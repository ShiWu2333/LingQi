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
    [SerializeField] private float hitboxRadius = 1.0f;
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

    [Header("Spacing / Strafe")]
    [SerializeField] private float preferredMinDistFactor = 0.6f;  // attackRange * 这个 = 太近，下撤
    [SerializeField] private float preferredMaxDistFactor = 0.9f;  // attackRange * 这个 = 舒服的中距离
    [SerializeField] private float strafeDistance = 1.5f;          // 绕圈时侧移距离

    [Header("Attack Tracking (吸附)")]
    [SerializeField] private float attackTrackSpeed = 2.0f;       // 抬手阶段的慢速移动速度
    [SerializeField] private float attackTrackStopDistance = 0.8f;// 想要站在 attackRange*这个 的位置出刀

    [Header("Runtime (ReadOnly)")]
    [SerializeField] private EnemyState state = EnemyState.Idle;
    [SerializeField] private float stateTimer;
    [SerializeField] private float timeInAttackZone;

    [Header("Projectile")]
    [SerializeField] private Transform projectileSpawnPoint;  // 敌人手上 / 枪口位置

    // 运行时（和 PlayerCombatController 类似）
    private int projectilesFired;
    private float projectileElapsed;
    private float projectileNextFireTime;

    private NavMeshAgent _agent;
    private EnemyResources _resources;
    private EnemyPoiseController _poise;
    private Transform _player;
    private AttackData _attackData;

    [Header("Telegraph")]
    [SerializeField] private AttackTelegraph _telegraph;

    public string CurrentDebugState => state.ToString();

    // 攻击相关
    private bool isAttackActive;
    private Vector3 lastHitboxCenter;
    private float lastHitboxRadius;
    private Coroutine _attackRoutine;

    // 硬直
    private float _staggerDuration;

    public event Action<AttackData> OnAttackStarted;
    public event Action<AttackData> OnAttackEnded;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _resources = GetComponent<EnemyResources>();
        _poise = GetComponent<EnemyPoiseController>();

        // 旋转由我们自己控制，避免 NavMeshAgent 抢方向
        _agent.updateRotation = false;

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
        {
            // 被打断时可以重置一下 Telegraph，避免一直残留
            _telegraph.ResetForNextAttack();
        }

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

        float dist = Vector3.Distance(transform.position, _player.position);

        // ================== 1）脱战判断 ==================
        if (dist > _resources.Stats.detectionRange * 1.5f)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            timeInAttackZone = 0f;
            SwitchState(EnemyState.Idle);
            return;
        }

        float attackRange = _resources.Stats.attackRange;
        float attackZone = attackRange + attackRangeBuffer;
        bool inAttackZone = dist <= attackZone;

        // ================== 2）更新 timeInAttackZone ==================
        if (inAttackZone)
            timeInAttackZone += Time.deltaTime;
        else
            timeInAttackZone = 0f;

        // ================== 3）朝向玩家（统一放最前） ==================
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
                _resources.Stats.rotateSpeed * Time.deltaTime);
        }

        // ================== 4）移动逻辑 ==================
        _agent.isStopped = false;

        float preferredMin = attackRange * preferredMinDistFactor;  // e.g. 0.6
        float preferredMax = attackRange * preferredMaxDistFactor;  // e.g. 0.9

        if (!inAttackZone)
        {
            // (A) 在进攻区外 → 纯追击
            _agent.speed = _resources.Stats.chaseSpeed;
            _agent.SetDestination(_player.position);
        }
        else
        {
            // (B) 在进攻区内 → 拉距 / 绕圈
            _agent.speed = _resources.Stats.chaseSpeed * 0.7f; // 稍微慢一点

            if (dist < preferredMin)
            {
                // 太近 → 后撤到一个舒服的位置
                Vector3 backDir = (transform.position - _player.position).normalized;
                Vector3 targetPos = _player.position + backDir * preferredMax;
                _agent.SetDestination(targetPos);
            }
            else if (dist > preferredMax)
            {
                // 稍微远一点 → 向前补一小步
                Vector3 toPlayer = (_player.position - transform.position).normalized;
                Vector3 targetPos = _player.position - toPlayer * preferredMax;
                _agent.SetDestination(targetPos);
            }
            else
            {
                // 在一个“舒服距离”上 → 轻微绕圈
                Vector3 toPlayer = (_player.position - transform.position).normalized;
                Vector3 sideDir = Vector3.Cross(Vector3.up, toPlayer); // 左侧
                float sideSign = (UnityEngine.Random.value > 0.5f) ? 1f : -1f;
                sideDir *= sideSign;

                Vector3 strafeTarget = transform.position + sideDir * strafeDistance;
                _agent.SetDestination(strafeTarget);
            }
        }

        // ================== 5）攻击决策 ==================
        if (_attackData != null &&
            inAttackZone &&
            _attackRoutine == null &&
            state != EnemyState.Attacking &&
            state != EnemyState.Stagger &&
            state != EnemyState.Cooldown)
        {
            // 5.1 需要在进攻区域“观察”至少 attackReactionTime
            if (timeInAttackZone < attackReactionTime)
                return;

            // 5.2 朝向玩家角度要比较正
            if (dirSq > 0.001f)
            {
                float dot = Vector3.Dot(transform.forward, dir);
                if (dot < attackAngleThreshold)
                    return;
            }

            // 5.3 再加一点随机概率，避免节奏太机械
            if (UnityEngine.Random.value > attackChance)
                return;

            // 启动攻击协程（Startup 里会做吸附 + 刹车）
            _attackRoutine = StartCoroutine(CoDoAttack());
        }
    }

    private IEnumerator CoDoAttack()
    {
        SwitchState(EnemyState.Attacking);
        stateTimer = 0f;

        if (_telegraph != null)
        {
            _telegraph.BeginAttackTelegraph();
        }

        if (_attackData != null)
            OnAttackStarted?.Invoke(_attackData);

        float startup = _attackData != null ? _attackData.startup : 0.2f;
        float active = _attackData != null ? _attackData.active : 0.1f;
        float recovery = _attackData != null ? _attackData.recovery : 0.3f;

        // ⭐ Projectile 计时重置
        projectilesFired = 0;
        projectileElapsed = 0f;
        projectileNextFireTime = _attackData != null ? _attackData.hitStartTime : 0f;

        // 记住原始 NavMesh 设置
        float originalSpeed = _agent.speed;
        bool originalStopped = _agent.isStopped;

        // ========== STARTUP：带吸附的抬手阶段 ==========
        float t = 0f;
        while (t < startup)
        {
            t += Time.deltaTime;

            if (_agent != null && _agent.enabled && _agent.isOnNavMesh && _player != null)
            {
                // 抬手期间允许缓慢移动
                _agent.speed = attackTrackSpeed;
                _agent.isStopped = false;

                Vector3 toPlayer = _player.position - transform.position;
                toPlayer.y = 0f;
                float dist = toPlayer.magnitude;

                if (dist > 0.001f)
                {
                    // 朝向玩家
                    Vector3 toPlayerDir = toPlayer / dist;
                    Quaternion targetRot = Quaternion.LookRotation(toPlayerDir, Vector3.up);
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRot,
                        _resources.Stats.rotateSpeed * Time.deltaTime);

                    // 控制吸附距离
                    float desiredDist = _resources.Stats.attackRange * attackTrackStopDistance;
                    if (dist > desiredDist)
                    {
                        // 稍微向前靠近
                        _agent.SetDestination(_player.position);
                    }
                    else
                    {
                        // 到了理想距离附近 → 刹车，不再向前滑
                        _agent.ResetPath();
                        _agent.isStopped = true;
                        _agent.velocity = Vector3.zero;
                    }
                }
            }

            yield return null;
        }

        // ========= ACTIVE：攻击阶段 =========
        if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
            _agent.ResetPath();
        }

        isAttackActive = true;

        if (_attackData != null && _attackData.isProjectileAttack)
        {
            // 远程：在 Active 持续时间内按节奏发射投射物
            yield return DoProjectileActive(active);
        }
        else
        {
            // 近战：一刀打一次 hitbox
            DoHitbox();

            float tActive = 0f;
            while (tActive < active)
            {
                tActive += Time.deltaTime;
                yield return null;
            }
        }

        isAttackActive = false;


        // ========= RECOVERY：后摇，原地 =========
        t = 0f;
        while (t < recovery)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (_attackData != null)
            OnAttackEnded?.Invoke(_attackData);

        if (_telegraph != null)
            _telegraph.ResetForNextAttack();

        // 回到冷却
        SwitchState(EnemyState.Cooldown);
        stateTimer = 0f;

        // 冷却阶段先保持停住；回到 Chase 后由 TickChase 再打开移动
        _agent.speed = originalSpeed;
        _agent.isStopped = originalStopped;

        _attackRoutine = null;
    }

    private IEnumerator DoProjectileActive(float activeDuration)
    {
        float t = 0f;
        int total = Mathf.Max(1, _attackData.projectileCount);
        float interval = Mathf.Max(0.01f, _attackData.projectileInterval);

        while (t < activeDuration)
        {
            t += Time.deltaTime;
            projectileElapsed += Time.deltaTime;

            if (projectilesFired < total && projectileElapsed >= projectileNextFireTime)
            {
                FireProjectile();
                projectilesFired++;
                projectileNextFireTime += interval;
            }

            yield return null;
        }
    }

    private void FireProjectile()
    {
        if (_attackData == null || _attackData.projectilePrefab == null)
            return;

        // 计算发射点
        Vector3 spawnPos;
        if (projectileSpawnPoint != null)
            spawnPos = projectileSpawnPoint.position;
        else
        {
            // 没指定就从敌人前方一点的位置发
            spawnPos = transform.position
                       + transform.forward * (_resources.Stats.attackRange * 0.5f)
                       + Vector3.up * hitboxHeightOffset;
        }

        // 朝向玩家
        Vector3 dir = _player != null
            ? (_player.position + Vector3.up * 0.8f - spawnPos).normalized
            : transform.forward;

        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

        GameObject go = GameObject.Instantiate(_attackData.projectilePrefab, spawnPos, rot);

        var proj = go.GetComponent<Projectile>();
        if (proj != null)
        {
            Transform homingTarget = null;
            if (_attackData.projectileHoming && _player != null)
                homingTarget = _player;

            // 敌人作为 attackSource，命中层仍然是 playerLayer
            proj.Init(_attackData, playerLayer, this, homingTarget);
        }
    }


    private void TickCooldown()
    {
        if (stateTimer >= _resources.Stats.attackInterval)
        {
            SwitchState(EnemyState.Chase);
            // 回到追击时再让 TickChase 自己控制 isStopped / speed
        }
    }

    // ================== Hitbox ==================

    private void DoHitbox()
    {
        if (_attackData == null || _player == null)
            return;

        Vector3 center = transform.position
                         + transform.forward * _resources.Stats.attackRange * 0.6f
                         + Vector3.up * hitboxHeightOffset;

        float radius = hitboxRadius;

        lastHitboxCenter = center;
        lastHitboxRadius = radius;

        Collider[] hits = Physics.OverlapSphere(
            center,
            radius,
            playerLayer,
            QueryTriggerInteraction.Ignore);

        if (debugDrawHitbox)
            Debug.Log($"[EnemyAI] DoHitbox: hits={hits.Length}");

        foreach (var col in hits)
        {
            if (col == null) continue;

            if (col.TryGetComponent(out PlayerResources playerRes))
            {
                Vector3 hitPoint = col.ClosestPoint(center);

                ImpactGrade impact = _attackData.impact;
                float damage = _attackData.damage; // 如果以后要算伤害加成，可以在这里统一处理

                playerRes.TakeDamage(damage, hitPoint, impact);
            }
        }
    }

    private void SwitchState(EnemyState newState)
    {
        state = newState;
        stateTimer = 0f;

        // 离开追击/攻击时，重置一下攻击区计时，避免状态切换后立即乱出手
        if (newState != EnemyState.Chase)
            timeInAttackZone = 0f;
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
        if (!debugDrawHitbox || !isAttackActive) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(lastHitboxCenter, lastHitboxRadius);
    }
}
