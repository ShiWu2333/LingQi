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

    [Header("State Names")]
    [SerializeField] private string locomotionStateName = "Locomotion";
    [SerializeField] private string rollStateName = "Roll";      // 和 Animator 里状态名一致
    [SerializeField] private string hitReactStateName = "HitReact";  // 同上

    [Header("Cross Fade Times")]
    [SerializeField] private float locomotionCrossFade = 0.1f;
    [SerializeField] private float attackCrossFade = 0.05f;
    [SerializeField] private float rollCrossFade = 0.05f;
    [SerializeField] private float hitCrossFade = 0.03f;

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

        float speed = movement.CurrentPlanarSpeed;
        animator.SetFloat(HashMoveSpeed, speed);
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

    }

    // ------------ 受击动画（事件驱动） ------------
    private void HandleDamaged(float damage)
    {
        if (animator == null) return;

        // 简单版：只要受到伤害就播受击
        animator.CrossFadeInFixedTime(hitReactStateName, hitCrossFade);
    }
}
