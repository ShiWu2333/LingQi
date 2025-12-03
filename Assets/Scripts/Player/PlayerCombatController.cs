using UnityEngine;
using System;

[DisallowMultipleComponent]
public class PlayerCombatController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private WeaponConfig weaponConfig;

    [Header("Input")]
    [SerializeField] private KeyCode lightKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode heavyKey = KeyCode.Mouse1;

    [Header("Hitbox")]
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private float hitboxHeightOffset = 1f; // 从角色中心往上抬一点，防止贴地

    [Header("Debug")]
    [SerializeField] private bool debugDrawHitbox = true;

    [Header("Debug (ReadOnly)")]
    [SerializeField] private AttackState currentState = AttackState.Idle;
    [SerializeField] private AttackData currentAttack;
    [SerializeField] private float stateTimer;

    public event Action<AttackData> OnAttackStarted;
    public event Action<AttackData> OnAttackEnded;

    private PlayerResources resources;
    private PlayerMovement movement;
    private PlayerDodgeController dodge;          // ⭐ 新增：用来判断 DashAttack 窗口
    private CharacterController controller;       // ⭐ 用于 stepForward 位移
    private bool dashAttackQueued;   // ⭐ 记录“闪避中按了轻攻，要在结束后放 DashAttack”

    private Vector3 debugHitboxCenter;
    private float debugHitboxRadius;

    // 对外只读属性，给 DebugOverlay 用
    public AttackState CurrentState => currentState;
    public float CurrentStateTimer => stateTimer;
    public AttackData CurrentAttack => currentAttack;

    private void Awake()
    {
        resources = GetComponent<PlayerResources>();
        movement = GetComponent<PlayerMovement>();
        dodge = GetComponent<PlayerDodgeController>();   // ⭐
        controller = GetComponent<CharacterController>();     // ⭐

        if (weaponConfig == null)
        {
            Debug.LogError("[PlayerCombatController] WeaponConfig not assigned!", this);
        }

        if (resources == null)
        {
            Debug.LogError("[PlayerCombatController] PlayerResources not found!", this);
        }

        if (movement == null)
        {
            Debug.LogError("[PlayerCombatController] PlayerMovement not found!", this);
        }

        if (controller == null)
        {
            Debug.LogWarning("[PlayerCombatController] CharacterController not found, stepForward will be disabled.", this);
        }
    }

    private void Update()
    {
        if (resources != null && resources.IsDead)
            return;

        TryConsumeQueuedDashAttack();   // ⭐ 先看看要不要在闪避结束后放 DashAttack
        HandleAttackInput();
        TickAttackState(Time.deltaTime);
    }

    // =============== 输入处理 ===============
    private void HandleAttackInput()
    {
        if (weaponConfig == null || resources == null)
            return;

        if (currentState != AttackState.Idle)
            return; // 简单版：攻击中不接受新输入（之后可以加 buffer）

        if (Input.GetKeyDown(lightKey))
        {
            AttackData attackToUse = null;

            bool hasDashAttack = (weaponConfig.dashAttack != null && dodge != null);

            if (hasDashAttack)
            {
                // 闪避过程中按下：排队，等结束后再放
                if (dodge.IsDodging)
                {
                    dashAttackQueued = true;
                    return;
                }

                // 闪避刚结束、还在 dash 窗口内：直接放 dashAttack
                if (dodge.IsInDashAttackWindow)
                {
                    attackToUse = weaponConfig.dashAttack;
                }
            }

            // 不在窗口或没 dashAttack：普通 lightAttack
            if (attackToUse == null)
                attackToUse = weaponConfig.lightAttack;

            TryStartAttack(attackToUse);
        }
        else if (Input.GetKeyDown(heavyKey))
        {
            TryStartAttack(weaponConfig.heavyAttack);
        }
    }

    private void TryConsumeQueuedDashAttack()
    {
        if (!dashAttackQueued)
            return;
        if (weaponConfig == null || weaponConfig.dashAttack == null)
        {
            dashAttackQueued = false;
            return;
        }
        if (dodge == null)
        {
            dashAttackQueued = false;
            return;
        }

        // 还在闪避中 → 不能起手
        if (dodge.IsDodging)
            return;

        // 窗口已经过了 → 直接丢弃这次请求
        if (!dodge.IsInDashAttackWindow)
        {
            dashAttackQueued = false;
            return;
        }

        // 只能在攻击 Idle 时起手
        if (currentState != AttackState.Idle)
            return;

        dashAttackQueued = false;
        TryStartAttack(weaponConfig.dashAttack);
    }

    private void TryStartAttack(AttackData attack)
    {
        if (attack == null)
            return;

        // 资源检查
        if (!resources.TrySpendStamina(attack.staminaCost))
        {
            Debug.Log("[PlayerCombatController] Not enough stamina for attack: " + attack.attackId);
            return;
        }

        if (!resources.TrySpendMana(attack.manaCost))
        {
            Debug.Log("[PlayerCombatController] Not enough mana for attack: " + attack.attackId);
            // 体力已经扣了，要不要退回？看你设计，这版先不退
            // 可以改成：先检查 Mana，再扣 Stamina
            return;
        }

        // 真正开始攻击
        currentAttack = attack;
        currentState = AttackState.Startup;
        stateTimer = 0f;

        ApplyMovementMultiplier(currentAttack.moveMultiplierStartup);

        // 通知表现层：某个 AttackData 正在启动
        OnAttackStarted?.Invoke(currentAttack);
    }

    // =============== 状态机主循环 ===============
    private void TickAttackState(float dt)
    {
        if (currentState == AttackState.Idle || currentAttack == null)
            return;

        stateTimer += dt;

        switch (currentState)
        {
            case AttackState.Startup:
                UpdateStartup(dt);   // ⭐ 这里实现 stepForward
                if (stateTimer >= currentAttack.startup)
                    EnterActive();
                break;

            case AttackState.Active:
                if (stateTimer >= currentAttack.active)
                    EnterRecovery();
                else
                    UpdateActive(dt);
                break;

            case AttackState.Recovery:
                if (stateTimer >= currentAttack.recovery)
                    EndAttack();
                break;
        }
    }

    // =============== 各阶段 ===============
    private void UpdateStartup(float dt)
    {
        // ⭐ 通用的 stepForward 逻辑：任何设置了 stepForward 的攻击都会在前 stepDuration 秒向前冲
        if (currentAttack == null || controller == null)
            return;

        if (!currentAttack.stepForward)
            return;

        if (currentAttack.stepDuration <= 0f || currentAttack.stepDistance <= 0f)
            return;

        // 在 Startup 的前 stepDuration 秒做位移
        if (stateTimer <= currentAttack.stepDuration)
        {
            float speed = currentAttack.stepDistance / currentAttack.stepDuration;
            Vector3 dir = transform.forward;
            dir.y = 0f;
            dir.Normalize();

            controller.Move(dir * speed * dt);
        }
    }

    private void EnterActive()
    {
        currentState = AttackState.Active;
        stateTimer = 0f;

        ApplyMovementMultiplier(currentAttack.moveMultiplierActive);

        // 进入 Active 瞬间做一次判定
        DoHitbox();
    }

    private void UpdateActive(float dt)
    {
        // 如果你想在 active 全程连续检测，可以把 DoHitbox 放在这里
        // DoHitbox();
    }

    private void EnterRecovery()
    {
        currentState = AttackState.Recovery;
        stateTimer = 0f;

        ApplyMovementMultiplier(currentAttack.moveMultiplierRecovery);
    }

    public void EndAttack()
    {
        var finishedAttack = currentAttack;

        currentState = AttackState.Idle;
        currentAttack = null;
        stateTimer = 0f;

        ApplyMovementMultiplier(1f);

        // 通知表现层：这个 Attack 走完了
        OnAttackEnded?.Invoke(finishedAttack);
    }

    // =============== Hitbox ===============
    private void DoHitbox()
    {
        if (currentAttack == null)
        {
            Debug.LogWarning("[Combat] DoHitbox called but currentAttack is null.");
            return;
        }

        Vector3 origin = transform.position + Vector3.up * hitboxHeightOffset;
        Vector3 center = origin + transform.forward * currentAttack.hitRange;
        float radius = currentAttack.hitRadius;

        debugHitboxCenter = center;
        debugHitboxRadius = radius;

        Collider[] hits = Physics.OverlapSphere(
            center,
            radius,
            enemyLayers,
            QueryTriggerInteraction.Collide
        );

        int damageCount = 0;

        foreach (var col in hits)
        {
            if (col.TryGetComponent(out EnemyResources enemyRes))
            {
                Vector3 hitPoint = col.ClosestPoint(debugHitboxCenter);

                enemyRes.TakeDamage(currentAttack.damage, hitPoint);

                if (col.TryGetComponent(out EnemyPoiseController poise))
                {
                    var reaction = poise.ApplyHit(currentAttack, ImpactContext.Default);
                }

                damageCount++;
            }
        }
        // Debug.Log($"[Combat] Damage applied to {damageCount} targets.");
    }

    // =============== 工具方法 ===============
    private void ApplyMovementMultiplier(float multiplier)
    {
        if (movement == null) return;
        movement.moveSpeedMultiplier = multiplier;
    }

    private void OnDrawGizmos()
    {
        if (!debugDrawHitbox) return;
        if (!Application.isPlaying) return;
        if (currentState != AttackState.Active) return;
        if (currentAttack == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(debugHitboxCenter, debugHitboxRadius);
    }

    public bool CanDodgeNow()
    {
        // Idle：随时可以闪避
        if (currentState == AttackState.Idle)
            return true;

        // Startup / Recovery：允许闪避，用来做 cancel
        if (currentState == AttackState.Startup || currentState == AttackState.Recovery)
            return true;

        // Active：不允许闪避
        return false;
    }
}
