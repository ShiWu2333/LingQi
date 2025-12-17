using UnityEngine;

[DisallowMultipleComponent]
public class PlayerAnimatorBridge : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerCombatController combat;
    [SerializeField] private PlayerDodgeController dodge;
    [SerializeField] private PlayerResources resources;
    [SerializeField] private Animator animator;

    // Animator 参数 Hash
    private static readonly int HashMoveSpeed = Animator.StringToHash("MoveSpeed");
    private static readonly int HashAttackSpeed = Animator.StringToHash("AttackSpeed");
    private static readonly int HashIsCharging = Animator.StringToHash("IsCharging");
    private static readonly int HashCharge01 = Animator.StringToHash("Charge01");

    [Header("State Names")]
    [SerializeField] private string locomotionStateName = "Locomotion";
    [SerializeField] private string rollStateName = "Roll";      // 和 Animator 里状态名一致
    [SerializeField] private string hitReactStateName = "HitReact";  // 同上
    [Header("Charge State Names")]
    [SerializeField] private string chargeStartStateName = "ChargeStart";
    [SerializeField] private string chargeLoopStateName = "ChargeLoop";
    [Header("Cross Fade Times")]
    [SerializeField] private float locomotionCrossFade = 0.1f;
    [SerializeField] private float attackCrossFade = 0.05f;
    [SerializeField] private float rollCrossFade = 0.05f;
    [SerializeField] private float hitCrossFade = 0.03f;
    [SerializeField] private float moveDirDamp = 0.10f; // 0.08~0.15 都行
    [SerializeField] private float moveSpeedDamp = 0.06f; // 0.04~0.10
    [SerializeField] private float moveSpeedMax = 5f;     // 你的最大移动速度，用来归一化

    // ------------ 自动抓引用 ------------
    private void Reset()
    {
        if (!movement) movement = this.GetInParent<PlayerMovement>();
        if (!combat) combat = this.GetInParent<PlayerCombatController>();
        if (!dodge) dodge = this.GetComponent<PlayerDodgeController>();
        if (!resources) resources = this.GetComponent<PlayerResources>();
        if (!animator) animator = this.GetInChildren<Animator>();
    }

    private void OnValidate()
    {
        if (!movement) movement = this.GetInParent<PlayerMovement>();
        if (!combat) combat = this.GetInParent<PlayerCombatController>();
        if (!dodge) dodge = this.GetComponent<PlayerDodgeController>();
        if (!resources) resources = this.GetComponent<PlayerResources>();
        if (!animator) animator = this.GetInChildren<Animator>();
    }

    // ------------ 事件订阅 ------------
    private void OnEnable()
    {
        if (combat != null)
        {
            combat.OnAttackStarted += HandleAttackStarted;
            combat.OnAttackEnded += HandleAttackEnded;

            combat.OnHeavyChargeStarted += HandleHeavyChargeStarted;
            combat.OnHeavyChargeUpdated += HandleHeavyChargeUpdated;
            combat.OnHeavyChargeCanceled += HandleHeavyChargeCanceled;
            combat.OnHeavyChargeReleased += HandleHeavyChargeReleased;
        }

        if (dodge != null)
        {
            dodge.OnDodgeStarted += HandleDodgeStarted;
            dodge.OnDodgeEnded += HandleDodgeEnded;
        }

        if (resources != null)
        {
            resources.OnDamaged += HandleDamaged;
        }
    }

    private void OnDisable()
    {
        if (combat != null)
        {
            combat.OnAttackStarted -= HandleAttackStarted;
            combat.OnAttackEnded -= HandleAttackEnded;

            combat.OnHeavyChargeStarted -= HandleHeavyChargeStarted;
            combat.OnHeavyChargeUpdated -= HandleHeavyChargeUpdated;
            combat.OnHeavyChargeCanceled -= HandleHeavyChargeCanceled;
            combat.OnHeavyChargeReleased -= HandleHeavyChargeReleased;
        }

        if (dodge != null)
        {
            dodge.OnDodgeStarted -= HandleDodgeStarted;
            dodge.OnDodgeEnded -= HandleDodgeEnded;
        }

        if (resources != null)
        {
            resources.OnDamaged -= HandleDamaged;
        }
    }


    // ------------ 移动动画 ------------
    private void Update()
    {
        if (movement == null || animator == null) return;

        if (movement.isMovementLocked)
        {
            animator.SetFloat("MoveX", 0f, moveDirDamp, Time.deltaTime);
            animator.SetFloat("MoveY", 0f, moveDirDamp, Time.deltaTime);

            animator.SetFloat(HashMoveSpeed, 0f, moveSpeedDamp, Time.deltaTime);
            return;
        }

        // 1) 更新方向参数（MoveX/MoveY）
        UpdateMoveDirection();

        // 2) 更新速度参数（MoveSpeed 归一化 + 平滑）
        float speed01 = 0f;
        if (moveSpeedMax > 0.0001f)
            speed01 = Mathf.Clamp01(movement.CurrentPlanarSpeed / moveSpeedMax);

        animator.SetFloat(HashMoveSpeed, speed01, moveSpeedDamp, Time.deltaTime);
    }



    // ------------ 攻击动画（数据驱动） ------------
    private void HandleAttackStarted(AttackData data)
    {
        if (animator == null || data == null) return;

        var clip = data.animationClip;
        var stateName = data.animatorStateName;
        if (clip == null || string.IsNullOrEmpty(stateName))
        {
            Debug.LogWarning($"[PlayerAnimatorBridge] Attack {data.attackId} has no animation bound", this);
            return;
        }

        float timeline = data.startup + data.active + data.recovery;
        if (timeline <= 0f)
            timeline = clip.length;

        // AttackSpeed = clipLength / timeline → 实际播放时长 = timeline
        float speedMultiplier = clip.length / timeline;

        animator.SetFloat(HashAttackSpeed, speedMultiplier);
        animator.CrossFadeInFixedTime(stateName, attackCrossFade);
    }

    private void HandleAttackEnded(AttackData data)
    {
        if (animator == null) return;

        animator.SetFloat(HashAttackSpeed, 1f);
        animator.CrossFadeInFixedTime(locomotionStateName, locomotionCrossFade);
    }

    // ------------ 翻滚动画（事件驱动） ------------
    private void HandleDodgeStarted()
    {
        if (animator == null) return;

        animator.CrossFadeInFixedTime(rollStateName, rollCrossFade);
    }

    private void HandleDodgeEnded()
    {
        if (animator == null) return;
        animator.CrossFadeInFixedTime(locomotionStateName, locomotionCrossFade);
    }

    // ------------ 受击动画（事件驱动） ------------
    private void HandleDamaged(float damage)
    {
        if (animator == null) return;

        // 简单版：只要受到伤害就播受击
        animator.CrossFadeInFixedTime(hitReactStateName, hitCrossFade);
    }

    private void HandleHeavyChargeStarted()
    {
        if (animator == null) return;

        animator.SetBool(HashIsCharging, true);
        animator.SetFloat(HashCharge01, 0f);

        // 先播一次 start（可在 Animator 里用 Exit Time 自动进 loop）
        if (!string.IsNullOrEmpty(chargeStartStateName))
            animator.CrossFadeInFixedTime(chargeStartStateName, 0.05f);
        else if (!string.IsNullOrEmpty(chargeLoopStateName))
            animator.CrossFadeInFixedTime(chargeLoopStateName, 0.05f);
    }

    private void HandleHeavyChargeUpdated(float c01)
    {
        if (animator == null) return;
        animator.SetFloat(HashCharge01, c01);
    }

    private void HandleHeavyChargeCanceled(float c01)
    {
        if (animator == null) return;

        animator.SetBool(HashIsCharging, false);
        animator.SetFloat(HashCharge01, c01);

        // 回 locomotion（如果你想更硬一点）
        animator.CrossFadeInFixedTime(locomotionStateName, locomotionCrossFade);
    }

    private void HandleHeavyChargeReleased(float c01)
    {
        if (animator == null) return;

        animator.SetBool(HashIsCharging, false);
        animator.SetFloat(HashCharge01, c01);

        // 不在这里播重击动画：重击动画由 OnAttackStarted(heavyAttack) 接管
    }

    private void UpdateMoveDirection()
    {
        // 如果几乎没动，直接归零（避免漂移）
        if (movement.CurrentPlanarSpeed < 0.05f)
        {
            animator.SetFloat("MoveX", 0f, moveDirDamp, Time.deltaTime);
            animator.SetFloat("MoveY", 0f, moveDirDamp, Time.deltaTime);
            return;
        }

        Vector3 worldMove = movement.LastMoveDirection; // 世界空间
        worldMove.y = 0f;
        if (worldMove.sqrMagnitude < 0.0001f)
        {
            animator.SetFloat("MoveX", 0f, moveDirDamp, Time.deltaTime);
            animator.SetFloat("MoveY", 0f, moveDirDamp, Time.deltaTime);
            return;
        }

        Vector3 local = transform.InverseTransformDirection(worldMove.normalized);

        animator.SetFloat("MoveX", Mathf.Clamp(local.x, -1f, 1f), moveDirDamp, Time.deltaTime);
        animator.SetFloat("MoveY", Mathf.Clamp(local.z, -1f, 1f), moveDirDamp, Time.deltaTime);
    }

}
