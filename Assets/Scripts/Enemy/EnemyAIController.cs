using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyResources))]
public class EnemyAIController : MonoBehaviour
{
    private enum EnemyState
    {
        Idle,
        Chase,
        Attacking,
        Cooldown
    }

    [Header("Config")]
    [SerializeField] private LayerMask playerLayer;   // 只检测玩家
    [SerializeField] private float hitboxRadius = 1.0f;
    [SerializeField] private float hitboxHeightOffset = 1.0f;
    [SerializeField] private bool debugDrawHitbox = false;

    [Header("Runtime (ReadOnly)")]
    [SerializeField] private EnemyState state = EnemyState.Idle;
    [SerializeField] private float stateTimer;

    private NavMeshAgent _agent;
    private EnemyResources _resources;
    private Transform _player;
    private AttackData _attackData;   // 目前只用一个默认攻击

    // 攻击生效阶段标记 + 最近一次 hitbox 数据
    private bool isAttackActive;
    private Vector3 lastHitboxCenter;
    private float lastHitboxRadius;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _resources = GetComponent<EnemyResources>();

        if (_resources.Stats == null)
        {
            Debug.LogError("[EnemyAI] EnemyStatsConfig is missing on EnemyResources.", this);
            enabled = false;
            return;
        }

        _attackData = _resources.Stats.defaultAttack;
        if (_attackData == null)
        {
            Debug.LogWarning("[EnemyAI] defaultAttack is not assigned in EnemyStatsConfig.", this);
        }

        // NavMeshAgent 基础参数从 stats 读
        _agent.speed = _resources.Stats.chaseSpeed;
        _agent.angularSpeed = _resources.Stats.rotateSpeed;
        _agent.stoppingDistance = _resources.Stats.attackRange * 0.8f; // 稍微早一点停下
    }

    private void Start()
    {
        // 自动找玩家（要求 Player 物体 Tag = "Player"）
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("[EnemyAI] No object with Tag=Player found.", this);
        }

        state = EnemyState.Idle;
        stateTimer = 0f;

        // 敌人死亡时禁用 AI
        _resources.OnDeath += HandleDeath;
    }

    private void OnDestroy()
    {
        if (_resources != null)
        {
            _resources.OnDeath -= HandleDeath;
        }
    }

    private void Update()
    {
        if (_player == null || !_agent.enabled) return;

        float dt = Time.deltaTime;
        stateTimer += dt;

        switch (state)
        {
            case EnemyState.Idle:
                TickIdle();
                break;
            case EnemyState.Chase:
                TickChase();
                break;
            case EnemyState.Attacking:
                // 攻击过程用协程控制，不在这里更新
                break;
            case EnemyState.Cooldown:
                TickCooldown();
                break;
        }
    }

    private void TickIdle()
    {
        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist <= _resources.Stats.detectionRange)
        {
            SwitchState(EnemyState.Chase);
        }
    }

    private void TickChase()
    {
        float dist = Vector3.Distance(transform.position, _player.position);

        if (dist > _resources.Stats.detectionRange * 1.5f)
        {
            // 跑远了，回 Idle
            _agent.isStopped = true;
            SwitchState(EnemyState.Idle);
            return;
        }

        // 追踪玩家
        _agent.isStopped = false;
        _agent.speed = _resources.Stats.chaseSpeed;
        _agent.SetDestination(_player.position);

        // 转向玩家
        Vector3 dir = (_player.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _resources.Stats.rotateSpeed * Time.deltaTime);
        }

        // 进入攻击范围
        if (dist <= _resources.Stats.attackRange && _attackData != null)
        {
            _agent.isStopped = true;
            StartCoroutine(CoDoAttack());
        }
    }

    private IEnumerator CoDoAttack()
    {
        SwitchState(EnemyState.Attacking);
        stateTimer = 0f;

        float startup = _attackData.startup;
        float active = _attackData.active;
        float recovery = _attackData.recovery;

        // Startup：前摇
        float t = 0f;
        while (t < startup)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // Active：出刀 + 伤害检测（这段时间 Gizmo 也显示）
        isAttackActive = true;
        DoHitbox();

        t = 0f;
        while (t < active)
        {
            t += Time.deltaTime;
            yield return null;
        }
        isAttackActive = false;

        // Recovery：后摇
        t = 0f;
        while (t < recovery)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // 攻击结束，进入冷却
        SwitchState(EnemyState.Cooldown);
        stateTimer = 0f;
    }

    private void TickCooldown()
    {
        if (stateTimer >= _resources.Stats.attackInterval)
        {
            // 冷却结束，重新判断是否追击/攻击
            SwitchState(EnemyState.Chase);
        }
    }

    private void DoHitbox()
    {
        // 命中球心位置
        Vector3 center = transform.position
                         + transform.forward * _resources.Stats.attackRange * 0.6f
                         + Vector3.up * hitboxHeightOffset;

        float radius = hitboxRadius;

        // 记录下来给 Gizmo 用
        lastHitboxCenter = center;
        lastHitboxRadius = radius;

        Collider[] hits = Physics.OverlapSphere(center, radius, playerLayer, QueryTriggerInteraction.Ignore);

        if (debugDrawHitbox)
        {
            Debug.Log($"[EnemyAI] DoHitbox: hits={hits.Length}");
        }

        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (col.TryGetComponent(out PlayerResources playerRes))
            {
                playerRes.TakeDamage(_attackData.damage);
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
        _agent.isStopped = true;
        _agent.enabled = false;
        enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!debugDrawHitbox) return;
        if (!isAttackActive) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(lastHitboxCenter, lastHitboxRadius);
    }
}
