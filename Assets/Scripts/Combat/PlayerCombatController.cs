using UnityEngine;

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

    private PlayerResources resources;
    private PlayerMovement movement;
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
    }

    private void Update()
    {
        if (resources != null && resources.IsDead)
            return;

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
            TryStartAttack(weaponConfig.lightAttack);
        }
        else if (Input.GetKeyDown(heavyKey))
        {
            TryStartAttack(weaponConfig.heavyAttack);
        }
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
        // 以后这里可以加：锁住旋转 / 锁定朝向等
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
                if (stateTimer >= currentAttack.startup)
                    EnterActive();
                break;

            case AttackState.Active:
                if (stateTimer >= currentAttack.active)
                    EnterRecovery();
                else
                    UpdateActive(dt); // 如果要在整段 active 期间持续判定，可以在这里做
                break;

            case AttackState.Recovery:
                if (stateTimer >= currentAttack.recovery)
                    EndAttack();
                break;
        }
    }

    // =============== 各阶段切换 ===============
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
        // 如果你想在 active 全程连续检测，可以把 DoHitbox 放在这里而不是 EnterActive
        // 例如：每帧检测一次 hitbox
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
        currentState = AttackState.Idle;
        currentAttack = null;
        stateTimer = 0f;

        ApplyMovementMultiplier(1f);
    }

    // =============== Hitbox 调用（Phase 4 会填充） ===============
    private void DoHitbox()
    {

        if (currentAttack == null)
        {
            Debug.LogWarning("[Combat] DoHitbox called but currentAttack is null.");
            return;
        }

        // 计算判定球心位置
        Vector3 origin = transform.position + Vector3.up * hitboxHeightOffset;
        Vector3 center = origin + transform.forward * currentAttack.hitRange;
        float radius = currentAttack.hitRadius;

        // 打印一下当前参数
        Debug.Log($"[Combat] DoHitbox: attack={currentAttack.attackId}, center={center}, radius={radius}");
        debugHitboxCenter = center;
        debugHitboxRadius = radius;
        // 检测敌人
        Collider[] hits = Physics.OverlapSphere(
            center,
            radius,
            enemyLayers,
            QueryTriggerInteraction.Collide   // 注意这里：允许 Trigger 也被打到
        );

        Debug.Log($"[Combat] OverlapSphere hit count = {hits.Length}");

        int damageCount = 0;

        foreach (var col in hits)
        {
            Debug.Log($"[Combat] Hit collider: {col.name}, layer={LayerMask.LayerToName(col.gameObject.layer)}");

            if (col.TryGetComponent(out Health health))
            {
                // 使用 Collider 提供的最近点作为命中点，更贴合模型表面
                Vector3 hitPoint = col.ClosestPoint(debugHitboxCenter);
                health.TakeDamage(currentAttack.damage, hitPoint);
                damageCount++;
            }
            else
            {
                Debug.Log($"[Combat] Collider {col.name} has no Health.");
            }
        }

        Debug.Log($"[Combat] Damage applied to {damageCount} targets.");
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
