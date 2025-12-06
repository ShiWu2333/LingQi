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

    [Header("Runtime (ReadOnly)")]
    [SerializeField] private EnemyState state = EnemyState.Idle;
    [SerializeField] private float stateTimer;

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
            case EnemyState.Attacking: /* 协程驱动 */   break;
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
        InterruptCurrentAction();

        _staggerDuration = Mathf.Max(0f, duration);

        if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            _agent.isStopped = true;

        SwitchState(EnemyState.Stagger);
    }

    private void TickStagger()
    {
        if (stateTimer >= _staggerDuration)
        {
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
                _agent.isStopped = false;

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

        // 关键点：这里不再 Reset Telegraph，避免同一段时间内多次起手导致 Hint 刷屏
        // if (_telegraph != null)
        //     _telegraph.ResetForNextAttack();

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
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh)
            return;

        float dist = Vector3.Distance(transform.position, _player.position);

        if (dist > _resources.Stats.detectionRange * 1.5f)
        {
            _agent.isStopped = true;
            SwitchState(EnemyState.Idle);
            return;
        }

        _agent.isStopped = false;
        _agent.speed = _resources.Stats.chaseSpeed;
        _agent.SetDestination(_player.position);

        Vector3 dir = _player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                _resources.Stats.rotateSpeed * Time.deltaTime);
        }

        if (dist <= _resources.Stats.attackRange && _attackData != null)
        {
            _agent.isStopped = true;

            if (_attackRoutine == null)
            {   
                _attackRoutine = StartCoroutine(CoDoAttack());
            }
        }
    }

    private IEnumerator CoDoAttack()
    {
        SwitchState(EnemyState.Attacking);
        stateTimer = 0f;

        // 一进入攻击协程就触发 Telegraph（内部还有 telegraphDelay）
        if (_telegraph != null)
        {
            Debug.Log("[EnemyAI] BeginAttackTelegraph()");
            _telegraph.BeginAttackTelegraph();
        }

        if (_attackData != null)
            OnAttackStarted?.Invoke(_attackData);

        float startup = _attackData.startup;
        float active = _attackData.active;
        float recovery = _attackData.recovery;

        // Startup
        float t = 0f;
        while (t < startup)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // Active：真正出刀
        isAttackActive = true;
        DoHitbox();

        t = 0f;
        while (t < active)
        {
            t += Time.deltaTime;
            yield return null;
        }
        isAttackActive = false;

        // Recovery
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

        SwitchState(EnemyState.Cooldown);
        stateTimer = 0f;

        _attackRoutine = null;
    }

    private void TickCooldown()
    {
        if (stateTimer >= _resources.Stats.attackInterval)
            SwitchState(EnemyState.Chase);
    }

    // ================== Hitbox ==================

    private void DoHitbox()
    {
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

                ImpactGrade impact = _attackData != null
                    ? _attackData.impact
                    : ImpactGrade.Medium;

                playerRes.TakeDamage(_attackData.damage, hitPoint, impact);
            }
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
        if (!debugDrawHitbox || !isAttackActive) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(lastHitboxCenter, lastHitboxRadius);
    }
}
