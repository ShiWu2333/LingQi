using UnityEngine;
using UnityEngine.AI;

public enum EnemyState
{
    Idle,
    Chasing,
    Attacking,
    Recovery
}

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAIController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private AttackData attackData;   // ★ 怪物攻击数据
    [SerializeField] private float detectRange = 12f; // 发现玩家距离
    [SerializeField] private float stopDistance = 2.2f; // 靠近到这个距离就不再继续贴脸推进
    [SerializeField] private float attackCooldown = 1.0f; // 每次攻击之后的额外冷却

    [Header("Hitbox")]
    [SerializeField] private LayerMask playerLayers;
    [SerializeField] private float hitboxHeightOffset = 1.0f;

    [Header("Debug (ReadOnly)")]
    [SerializeField] private EnemyState state;
    [SerializeField] private float stateTimer;
    [SerializeField] private bool isAttacking;
    [SerializeField] private bool debugDrawHitbox = true;

    private Transform player;
    private NavMeshAgent agent;
    private Health health;

    private float attackCooldownTimer;
    private bool hasDealtDamageThisAttack;

    // debug hitbox
    private Vector3 debugHitCenter;
    private float debugHitRadius;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();

        var pr = FindFirstObjectByType<PlayerResources>();
        if (pr != null)
        {
            player = pr.transform;
        }
        else
        {
            Debug.LogWarning("[EnemyAI] No PlayerResources found in scene.", this);
        }

        if (attackData == null)
        {
            Debug.LogError("[EnemyAI] AttackData not assigned!", this);
        }
    }

    private void Update()
    {
        if (health != null && health.IsDead)
        {
            agent.isStopped = true;
            return;
        }

        float dt = Time.deltaTime;
        stateTimer += dt;

        // 攻击冷却计时
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= dt;
            if (attackCooldownTimer < 0f) attackCooldownTimer = 0f;
        }

        switch (state)
        {
            case EnemyState.Idle:
                TickIdle();
                break;
            case EnemyState.Chasing:
                TickChasing();
                break;
            case EnemyState.Attacking:
                TickAttacking();
                break;
            case EnemyState.Recovery:
                TickRecovery();
                break;
        }
    }

    private void TickIdle()
    {
        if (!player) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= detectRange)
        {
            state = EnemyState.Chasing;
            stateTimer = 0f;
            agent.isStopped = false;
        }
    }

    private void TickChasing()
    {
        if (!player || attackData == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // 移动到玩家附近
        if (dist > stopDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            agent.isStopped = true;
        }

        // 距离足够近 + 攻击不在冷却中 → 开始攻击
        if (dist <= attackData.hitRange && attackCooldownTimer <= 0f)
        {
            BeginAttack();
        }
    }

    private void BeginAttack()
    {
        if (attackData == null) return;

        state = EnemyState.Attacking;
        stateTimer = 0f;
        isAttacking = true;
        hasDealtDamageThisAttack = false;

        // 停止 NavMesh 移动
        agent.isStopped = true;

        // 朝玩家转向（简单版）
        if (player)
        {
            Vector3 dir = player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }

    private void TickAttacking()
    {
        if (attackData == null) return;

        float windup = attackData.startup;
        float active = attackData.active;

        if (stateTimer < windup)
        {
            // 前摇阶段：啥也不做，让敌人定在那里
        }
        else if (stateTimer < windup + active)
        {
            // Active 阶段：只在第一次进入时打一次 hitbox
            if (!hasDealtDamageThisAttack)
            {
                DoHitbox();
                hasDealtDamageThisAttack = true;
            }
        }
        else
        {
            // 进入 Recovery
            state = EnemyState.Recovery;
            stateTimer = 0f;
        }
    }

    private void TickRecovery()
    {
        if (attackData == null) return;

        float recovery = attackData.recovery;

        if (stateTimer >= recovery)
        {
            isAttacking = false;
            agent.isStopped = false;

            // 设置攻击冷却
            attackCooldownTimer = attackCooldown;

            // 回到追击
            state = EnemyState.Chasing;
            stateTimer = 0f;
        }
    }

    private void DoHitbox()
    {
        if (attackData == null) return;

        Vector3 origin = transform.position + Vector3.up * hitboxHeightOffset;
        Vector3 center = origin + transform.forward * attackData.hitRange;
        float radius = attackData.hitRadius;

        debugHitCenter = center;
        debugHitRadius = radius;

        Collider[] hits = Physics.OverlapSphere(
            center,
            radius,
            playerLayers,
            QueryTriggerInteraction.Collide);

        int hitCount = 0;

        foreach (var col in hits)
        {
            if (col.TryGetComponent(out PlayerResources pr))
            {
                pr.TakeDamage(attackData.damage);
                hitCount++;
            }
        }

        // Debug.Log($"[EnemyAI] Attack hit {hitCount} targets.");
    }

    private void OnDrawGizmos()
    {
        if (!debugDrawHitbox) return;
        if (!Application.isPlaying) return;
        if (attackData == null) return;

        // 画当前 attackData 的判定范围（方便调）
        if (state == EnemyState.Attacking)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(debugHitCenter, debugHitRadius);
        }

        // 在场景中常驻显示“潜在攻击范围”
        Gizmos.color = new Color(1f, 0f, 1f, 0.2f);
        Vector3 origin = transform.position + Vector3.up * 1.0f;
        Vector3 idealCenter = origin + transform.forward * (attackData != null ? attackData.hitRange : 2f);
        Gizmos.DrawWireSphere(idealCenter, attackData != null ? attackData.hitRadius : 1.2f);
    }
}
