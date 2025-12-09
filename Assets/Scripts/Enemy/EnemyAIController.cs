using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyResources))]
public class EnemyAIController : MonoBehaviour, IAttackSource
{
    private enum EnemyState
    {
        Idle,
        Chase,
        Attacking,
        Stagger,    // 硬直
        Cooldown
    }

    [Header("Config")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float hitboxHeightOffset = 1.0f;
    [SerializeField] private bool debugDrawHitbox = false;

    [Header("Stagger Config")]
    [SerializeField] private float lightStaggerTime = 0.1f;
    [SerializeField] private float mediumStaggerTime = 0.35f;
    [SerializeField] private float heavyStaggerTime = 0.6f;

    [Header("Attack Decision")]
    [SerializeField] private float attackReactionTime = 0.4f;   // 进入攻击区后，至少观察多久才可能出手
    [SerializeField] private float attackAngleThreshold = 0.8f; // dot 阈值，大概 36° 内才出手
    [SerializeField] private float attackChance = 0.7f;         // 满足条件时本次是否出手的概率
    [SerializeField] private float attackRangeBuffer = 0.3f;    // 攻击范围的缓冲区（稍微远一点也算进攻区域）

    [Header("Attack Variants")]
    [Tooltip("是否允许敌人随机使用第二种攻击（Stats.heavyAttack）")]
    [SerializeField] private bool enableSecondAttack = false;

    [Tooltip("当允许第二种攻击时，本次出手改用 heavyAttack 的概率")]
    [SerializeField][Range(0f, 1f)] private float secondAttackChance = 0.4f;

    [Header("Spacing / Strafe")]
    [SerializeField] private float preferredMinDistFactor = 0.6f;  // attackRange * 这个 = 太近，下撤
    [SerializeField] private float preferredMaxDistFactor = 0.9f;  // attackRange * 这个 = 舒服的中距离
    [SerializeField] private float strafeDistance = 1.5f;          // 绕圈时侧移距离

    [Header("Attack Tracking (吸附)")]
    [SerializeField] private float attackTrackSpeed = 2.0f;       // 抬手阶段的慢速移动速度
    [SerializeField] private float attackTrackStopDistance = 0.8f;// 想要站在 attackRange*这个 的位置出刀

    [Header("Projectile")]
    [SerializeField] private Transform projectileSpawnPoint;

    [Header("Runtime (ReadOnly)")]
    [SerializeField] private EnemyState state = EnemyState.Idle;
    [SerializeField] private float stateTimer;
    [SerializeField] private float timeInAttackZone;

    private NavMeshAgent _agent;
    private EnemyResources _resources;
    private EnemyPoiseController _poise;
    private Transform _player;
    private AttackData _attackData;

    [Header("Telegraph")]
    [SerializeField] private AttackTelegraph _telegraph;

    public string CurrentDebugState => state.ToString();

    // 攻击相关（近战 / 投射物）
    private bool isAttackActive;
    private Vector3 lastHitboxCenter;
    private float lastHitboxRadius;
    private Coroutine _attackRoutine;

    // Projectile 运行时
    private int projectilesFired;
    private float projectileElapsed;
    private float projectileNextFireTime;

    // 硬直
    private float _staggerDuration;

    public event Action<AttackData> OnAttackStarted;
    public event Action<AttackData> OnAttackEnded;

    // Gizmo 缓存
    private Vector3 gizmoCenter;
    private Quaternion gizmoRotation;
    private Vector3 gizmoBoxHalfExtents;
    private float gizmoSphereRadius;
    private Vector3 gizmoCapsuleP1;
    private Vector3 gizmoCapsuleP2;
    private float gizmoCapsuleRadius;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _resources = GetComponent<EnemyResources>();
        _poise = GetComponent<EnemyPoiseController>();

        if (_telegraph == null)
            _telegraph = GetComponentInChildren<AttackTelegraph>();

        if (_resources.Stats == null)
        {
            Debug.LogError("[EnemyAI] EnemyStatsConfig is missing on EnemyResources.", this);
            enabled = false;
            return;
        }

        if (_poise != null)
            _poise.OnImpactReaction += HandleImpactReaction;

        // 默认攻击（可以在 EnemyStatsConfig 里把 defaultAttack 配成 DashAttack）
        _attackData = _resources.Stats.defaultAttack;
        if (_attackData == null)
        {
            Debug.LogWarning("[EnemyAI] defaultAttack is not assigned in EnemyStatsConfig.", this);
        }

        // NavMeshAgent 参数
        _agent.speed = _resources.Stats.chaseSpeed;
        _agent.angularSpeed = _resources.Stats.rotateSpeed;
        _agent.stoppingDistance = _resources.Stats.attackRange * 0.8f;
    }

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;
        else
            Debug.LogWarning("[EnemyAI] No object with Tag=Player found.", this);

        state = EnemyState.Idle;
        stateTimer = 0f;

        _resources.OnDeath += HandleDeath;
    }

    private void OnDestroy()
    {
        if (_resources != null)
            _resources.OnDeath -= HandleDeath;

        if (_poise != null)
            _poise.OnImpactReaction -= HandleImpactReaction;
    }

    private void Update()
    {
        if (_player == null || !_agent.enabled) return;

        float dt = Time.deltaTime;
        stateTimer += dt;

        switch (state)
        {
            case EnemyState.Idle: TickIdle(); break;
            case EnemyState.Chase: TickChase(); break;
            case EnemyState.Attacking: /* 协程驱动 */ break;
            case EnemyState.Stagger: TickStagger(); break;
            case EnemyState.Cooldown: TickCooldown(); break;
        }
    }

    // ================== 受击 / 硬直 ==================

    private void HandleImpactReaction(ImpactReaction reaction)
    {
        switch (reaction)
        {
            case ImpactReaction.None:
                break;

            case ImpactReaction.LightStagger:
                EnterStagger(lightStaggerTime);
                PlaySmallStagger();
                break;

            case ImpactReaction.MediumStagger:
                EnterStagger(mediumStaggerTime);
                PlayMediumStagger();
                break;

            case ImpactReaction.HeavyStagger:
                EnterStagger(heavyStaggerTime);
                PlayLargeStagger();
                break;
        }
    }

    private void EnterStagger(float duration)
    {
        InterruptCurrentAction();  // 打断当前攻击

        _staggerDuration = Mathf.Max(0f, duration);

        if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero; // 刹死
            _agent.ResetPath();             // 清空当前路径
        }

        SwitchState(EnemyState.Stagger);
    }

    private void TickStagger()
    {
        if (stateTimer >= _staggerDuration)
        {
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                _agent.velocity = Vector3.zero;
            }

            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist <= _resources.Stats.detectionRange)
                SwitchState(EnemyState.Chase);
            else
                SwitchState(EnemyState.Idle);
        }
    }

    private void PlaySmallStagger() { }
    private void PlayMediumStagger() { }
    private void PlayLargeStagger() { }

    /// <summary>
    /// 打断当前攻击，不改变状态（由调用方决定进什么状态）
    /// </summary>
    private void InterruptCurrentAction()
    {
        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }

        isAttackActive = false;

        if (_telegraph != null)
            _telegraph.ResetForNextAttack();

        if (_attackData != null)
            OnAttackEnded?.Invoke(_attackData);
    }

    // ================== 状态逻辑 ==================

    private void TickIdle()
    {
        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist <= _resources.Stats.detectionRange)
            SwitchState(EnemyState.Chase);
    }

    private void TickChase()
    {
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh || _player == null)
            return;

        EnemyStatsConfig stats = _resources.Stats;

        float dist = Vector3.Distance(transform.position, _player.position);

        // 超过侦测范围很远：脱战
        if (dist > stats.detectionRange * 1.5f)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            timeInAttackZone = 0f;
            SwitchState(EnemyState.Idle);
            return;
        }

        // 计算“进攻区域”
        float attackRange = stats.attackRange;
        float attackZone = attackRange + attackRangeBuffer;
        bool inAttackZone = dist <= attackZone;

        if (inAttackZone)
            timeInAttackZone += Time.deltaTime;
        else
            timeInAttackZone = 0f;

        // 默认：向玩家追击
        _agent.isStopped = false;
        _agent.speed = stats.chaseSpeed;
        _agent.SetDestination(_player.position);

        // 朝向玩家
        Vector3 dir = _player.position - transform.position;
        dir.y = 0f;
        float dirSq = dir.sqrMagnitude;
        if (dirSq > 0.001f)
        {
            dir.Normalize();
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                stats.rotateSpeed * Time.deltaTime);
        }

        // 简单拉距 / 绕圈逻辑
        float preferredMin = attackRange * preferredMinDistFactor;
        float preferredMax = attackRange * preferredMaxDistFactor;

        if (dist < preferredMin)
        {
            // 太近 → 往后退一点
            Vector3 backDir = (transform.position - _player.position).normalized;
            Vector3 targetPos = _player.position + backDir * preferredMax;

            _agent.SetDestination(targetPos);
        }
        else if (dist < preferredMax)
        {
            // 中距离 → 轻微绕圈
            Vector3 toPlayer = (_player.position - transform.position).normalized;
            Vector3 sideDir = Vector3.Cross(Vector3.up, toPlayer); // 左侧
            float sideSign = (UnityEngine.Random.value > 0.5f) ? 1f : -1f;
            sideDir *= sideSign;

            Vector3 strafeTarget = transform.position + sideDir * strafeDistance;
            _agent.SetDestination(strafeTarget);
        }
        else
        {
            // 太远 → 正常追击，前面的 SetDestination(player) 已经生效
        }

        // ================== 攻击决策 ==================
        if (stats.defaultAttack == null)
            return;

        if (!inAttackZone ||
            _attackRoutine != null ||
            state == EnemyState.Attacking ||
            state == EnemyState.Stagger ||
            state == EnemyState.Cooldown)
            return;

        // 1）需要在进攻区域停留至少一段时间（反应时间）
        if (timeInAttackZone < attackReactionTime)
            return;

        // 2）朝向玩家角度要比较正
        if (dirSq > 0.001f)
        {
            float dot = Vector3.Dot(transform.forward, dir);
            if (dot < attackAngleThreshold)
                return;
        }

        // 3）再加一点随机概率，避免节奏太机械
        if (UnityEngine.Random.value > attackChance)
            return;

        // 4）到这里已经确定“要出手一次”，现在才随机选具体哪一招
        AttackData selected = stats.defaultAttack;

        if (enableSecondAttack && stats.heavyAttack != null)
        {
            // 随机决定本次是否用第二种攻击
            if (UnityEngine.Random.value < secondAttackChance)
                selected = stats.heavyAttack;
        }

        _attackData = selected;
        _attackRoutine = StartCoroutine(CoDoAttack());
    }

    private IEnumerator CoDoAttack()
    {
        if (_attackData == null)
            yield break;

        SwitchState(EnemyState.Attacking);
        stateTimer = 0f;

        if (_telegraph != null)
            _telegraph.BeginAttackTelegraph();

        OnAttackStarted?.Invoke(_attackData);

        float startup = _attackData.startup;
        float active = _attackData.active;
        float recovery = _attackData.recovery;

        // Projectile 计时（如果这是一招远程）
        projectilesFired = 0;
        projectileElapsed = 0f;
        projectileNextFireTime = _attackData.hitStartTime;

        float attackElapsed = 0f;      // 攻击总时间轴：0 → startup+active+recovery
        bool hasHitThisAttack = false;

        // 记住 NavMesh 原参数
        float originalSpeed = _agent.speed;
        bool originalStopped = _agent.isStopped;

        // ========== STARTUP：抬手 ==========
        float tStartup = 0f;
        while (tStartup < startup)
        {
            float dt = Time.deltaTime;
            tStartup += dt;
            attackElapsed += dt;

            if (_agent != null && _agent.enabled && _agent.isOnNavMesh && _player != null)
            {
                // 抬手期间朝向玩家
                Vector3 toPlayer = _player.position - transform.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.001f)
                {
                    Vector3 toPlayerDir = toPlayer.normalized;
                    Quaternion targetRot = Quaternion.LookRotation(toPlayerDir, Vector3.up);
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRot,
                        _resources.Stats.rotateSpeed * dt);
                }
            }

            yield return null;
        }

        // ========= ACTIVE：有 hit window / 或 Projectile =========
        if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
            _agent.ResetPath();
        }

        isAttackActive = true;

        if (_attackData.isProjectileAttack)
        {
            // 远程：在 Active 内按节奏发射投射物
            yield return DoProjectileActive(active);
        }
        else
        {
            // 近战：在 Active 内根据 hit window 执行 DoHitbox 一次 + 冲刺
            float tActive = 0f;
            while (tActive < active)
            {
                float dt = Time.deltaTime;
                tActive += dt;
                attackElapsed += dt;

                // ① 冲刺：只在 Active 前 stepDuration 秒内移动
                if (_attackData.stepForward &&
                    _attackData.stepDuration > 0f &&
                    _attackData.stepDistance > 0f &&
                    tActive <= _attackData.stepDuration)
                {
                    float speed = _attackData.stepDistance / _attackData.stepDuration;
                    Vector3 dashDir = transform.forward;
                    dashDir.y = 0f;
                    dashDir.Normalize();

                    if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
                        _agent.Move(dashDir * speed * dt);
                }

                // ② Hit Window：只出一次伤害
                if (!hasHitThisAttack)
                {
                    float hitStart = _attackData.hitStartTime;
                    float hitEnd = _attackData.hitEndTime;

                    if (hitStart <= 0f && hitEnd <= 0f)
                    {
                        // 没配 hitWindow：Active 第一帧出一次
                        if (tActive <= dt)
                        {
                            DoHitbox();
                            hasHitThisAttack = true;
                        }
                    }
                    else
                    {
                        float endTime = hitEnd > 0f ? hitEnd : (startup + active);
                        if (attackElapsed >= hitStart && attackElapsed <= endTime)
                        {
                            DoHitbox();
                            hasHitThisAttack = true;
                        }
                    }
                }

                yield return null;
            }
        }

        isAttackActive = false;

        // ========= RECOVERY：后摇 =========
        float tRecovery = 0f;
        while (tRecovery < recovery)
        {
            float dt = Time.deltaTime;
            tRecovery += dt;
            attackElapsed += dt;
            yield return null;
        }

        OnAttackEnded?.Invoke(_attackData);

        if (_telegraph != null)
            _telegraph.ResetForNextAttack();

        // 回到冷却
        SwitchState(EnemyState.Cooldown);
        stateTimer = 0f;

        _agent.speed = originalSpeed;
        _agent.isStopped = originalStopped;

        _attackRoutine = null;
    }

    // ========= 远程 Active 阶段 =========
    private IEnumerator DoProjectileActive(float activeDuration)
    {
        float t = 0f;
        int total = Mathf.Max(1, _attackData.projectileCount);
        float interval = Mathf.Max(0.01f, _attackData.projectileInterval);

        while (t < activeDuration)
        {
            float dt = Time.deltaTime;
            t += dt;

            projectileElapsed += dt;

            if (projectilesFired < total && projectileElapsed >= projectileNextFireTime)
            {
                FireProjectile();
                projectilesFired++;
                projectileNextFireTime += interval;
            }

            yield return null;
        }
    }

    private void TickCooldown()
    {
        if (stateTimer >= _resources.Stats.attackInterval)
        {
            SwitchState(EnemyState.Chase);
        }
    }

    // ================== Hitbox（近战） ==================

    private void DoHitbox()
    {
        if (_attackData == null)
            return;

        Vector3 localOffset = _attackData.hitboxLocalOffset;
        localOffset.y += hitboxHeightOffset;
        Vector3 center = transform.position + transform.rotation * localOffset;

        lastHitboxCenter = center;
        float debugRadius = 0f;

        Collider[] hits = null;

        gizmoCenter = center;
        switch (_attackData.hitboxShape)
        {
            case HitboxShape.Sphere:
                {
                    float radius = Mathf.Max(0f, _attackData.hitboxRadius);
                    debugRadius = radius;

                    hits = Physics.OverlapSphere(center, radius, playerLayer,
                        QueryTriggerInteraction.Ignore);

                    gizmoSphereRadius = radius;
                    break;
                }

            case HitboxShape.Box:
                {
                    Vector3 halfExtents = _attackData.hitboxBoxHalfExtents;
                    debugRadius = halfExtents.magnitude;

                    Quaternion rot = transform.rotation;
                    hits = Physics.OverlapBox(center, halfExtents, rot, playerLayer,
                        QueryTriggerInteraction.Ignore);

                    gizmoRotation = transform.rotation;
                    gizmoBoxHalfExtents = _attackData.hitboxBoxHalfExtents;
                    break;
                }

            case HitboxShape.Capsule:
                {
                    float radius = Mathf.Max(0f, _attackData.hitboxCapsuleRadius);
                    float halfHeight = Mathf.Max(_attackData.hitboxCapsuleHeight * 0.5f, radius);

                    Vector3 dirLocal = _attackData.hitboxCapsuleDirection;
                    if (dirLocal.sqrMagnitude < 0.0001f)
                        dirLocal = Vector3.forward;

                    Vector3 dirWorld = transform.rotation * dirLocal.normalized;

                    Vector3 p1 = center + dirWorld * halfHeight;
                    Vector3 p2 = center - dirWorld * halfHeight;

                    debugRadius = halfHeight + radius;

                    hits = Physics.OverlapCapsule(p1, p2, radius, playerLayer,
                        QueryTriggerInteraction.Ignore);

                    gizmoCapsuleRadius = radius;
                    gizmoCapsuleP1 = p1;
                    gizmoCapsuleP2 = p2;
                    break;
                }

            default:
                return;
        }

        lastHitboxRadius = debugRadius;

        if (hits == null || hits.Length == 0)
            return;

        foreach (var col in hits)
        {
            if (col == null) continue;

            if (col.TryGetComponent(out PlayerResources playerRes))
            {
                Vector3 hitPoint = col.ClosestPoint(center);

                ImpactGrade impact = _attackData.impact;
                float damage = _attackData.damage;

                playerRes.TakeDamage(damage, hitPoint, impact);
            }
        }
    }

    // ================== Projectile 发射 ==================

    private void FireProjectile()
    {
        if (_attackData == null || _attackData.projectilePrefab == null)
            return;

        Vector3 spawnPos;
        if (projectileSpawnPoint != null)
            spawnPos = projectileSpawnPoint.position;
        else
        {
            spawnPos = transform.position
                       + transform.forward * (_resources.Stats.attackRange * 0.5f)
                       + Vector3.up * hitboxHeightOffset;
        }

        Vector3 dir;
        if (_player != null)
            dir = (_player.position + Vector3.up * 0.8f - spawnPos).normalized;
        else
            dir = transform.forward;

        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

        GameObject go = Instantiate(_attackData.projectilePrefab, spawnPos, rot);

        var proj = go.GetComponent<Projectile>();
        if (proj != null)
        {
            Transform homingTarget = null;
            if (_attackData.projectileHoming && _player != null)
                homingTarget = _player;

            proj.Init(_attackData, playerLayer, this, homingTarget);
        }
    }

    private void SwitchState(EnemyState newState)
    {
        state = newState;
        stateTimer = 0f;
    }

    private void HandleDeath()
    {
        if (_telegraph != null)
            _telegraph.ResetForNextAttack();

        if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.enabled = false;
        }

        enabled = false;
    }

    private void OnDrawGizmos()
    {
        if (!debugDrawHitbox) return;
        if (!Application.isPlaying) return;
        if (!isAttackActive) return;
        if (_attackData == null) return;

        Gizmos.color = Color.yellow;

        switch (_attackData.hitboxShape)
        {
            case HitboxShape.Sphere:
                {
                    Gizmos.DrawWireSphere(gizmoCenter, gizmoSphereRadius);
                    break;
                }

            case HitboxShape.Box:
                {
                    Matrix4x4 matrix = Matrix4x4.TRS(gizmoCenter, gizmoRotation, Vector3.one);
                    Gizmos.matrix = matrix;
                    Gizmos.DrawWireCube(Vector3.zero, gizmoBoxHalfExtents * 2f);
                    Gizmos.matrix = Matrix4x4.identity;
                    break;
                }

            case HitboxShape.Capsule:
                {
                    DrawWireCapsule(gizmoCapsuleP1, gizmoCapsuleP2, gizmoCapsuleRadius);
                    break;
                }
        }
    }

    private void DrawWireCapsule(Vector3 p1, Vector3 p2, float radius)
    {
        Gizmos.DrawWireSphere(p1, radius);
        Gizmos.DrawWireSphere(p2, radius);

        int segments = 16;
        for (int i = 0; i < segments; i++)
        {
            float a0 = (float)i / segments * Mathf.PI * 2f;
            float a1 = (float)(i + 1) / segments * Mathf.PI * 2f;

            Vector3 r0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * radius;
            Vector3 r1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * radius;

            Gizmos.DrawLine(p1 + r0, p1 + r1);
            Gizmos.DrawLine(p2 + r0, p2 + r1);
            Gizmos.DrawLine(p1 + r0, p2 + r0);
        }
    }
}
