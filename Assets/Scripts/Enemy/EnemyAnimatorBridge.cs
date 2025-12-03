using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class EnemyAnimatorBridge : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EnemyResources resources;
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;   // ⭐ 改成 NavMeshAgent
    [SerializeField] private EnemyAIController ai;


    private static readonly int HashMoveSpeed = Animator.StringToHash("MoveSpeed");
    private static readonly int HashAttackSpeed = Animator.StringToHash("AttackSpeed");

    [Header("State Names")]
    [SerializeField] private string locomotionStateName = "Locomotion";
    [SerializeField] private string hitReactStateName = "HitReact";

    [Header("MoveSpeed 归一化")]
    [SerializeField] private float maxSpeedForBlend = 5f;   // 敌人跑动时的大概最高速度

    [Header("Cross Fade Times")]
    [SerializeField] private float locomotionCrossFade = 0.1f;
    [SerializeField] private float hitCrossFade = 0.03f;

    private void Reset()
    {
        if (!resources) resources = GetComponent<EnemyResources>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!ai) ai = GetComponent<EnemyAIController>();
    }

    private void OnValidate()
    {
        if (!resources) resources = GetComponent<EnemyResources>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!ai) ai = GetComponent<EnemyAIController>();
    }

    private void OnEnable()
    {
        if (resources != null)
            resources.OnDamaged += HandleDamaged;

        if (ai != null)
        {
            ai.OnAttackStarted += HandleAttackStarted;
            ai.OnAttackEnded += HandleAttackEnded;
        }
    }

    private void OnDisable()
    {
        if (resources != null)
            resources.OnDamaged -= HandleDamaged;

        if (ai != null)
        {
            ai.OnAttackStarted -= HandleAttackStarted;
            ai.OnAttackEnded -= HandleAttackEnded;
        }
    }

    private void Update()
    {
        if (animator == null) return;

        float speedNorm = 0f;

        if (agent != null)
        {
            // NavMeshAgent 的速度，自带平滑
            Vector3 v = agent.velocity;
            v.y = 0f;
            float speed = v.magnitude;

            if (maxSpeedForBlend > 0f)
                speedNorm = Mathf.Clamp01(speed / maxSpeedForBlend);
            else
                speedNorm = speed;
        }

        animator.SetFloat(HashMoveSpeed, speedNorm);
    }

    // ===== 敌人受击动画 =====
    private void HandleDamaged(float damage)
    {
        if (animator == null) return;

        animator.CrossFadeInFixedTime(hitReactStateName, hitCrossFade);
    }

    // ===== 敌人攻击动画（AttackData 驱动） =====
    private void HandleAttackStarted(AttackData data)
    {
        if (animator == null || data == null) return;

        var clip = data.animationClip;
        var stateName = data.animatorStateName;
        if (clip == null || string.IsNullOrEmpty(stateName))
        {
            Debug.LogWarning($"[EnemyAnimatorBridge] Attack {data.attackId} has no animation bound", this);
            return;
        }

        float timeline = data.startup + data.active + data.recovery;
        if (timeline <= 0f)
            timeline = clip.length;

        // AttackSpeed = clipLength / timeline → 实际播放时间 = timeline
        float speedMultiplier = clip.length / timeline;

        animator.SetFloat(HashAttackSpeed, speedMultiplier);
        animator.CrossFadeInFixedTime(stateName, 0.05f);
    }

    private void HandleAttackEnded(AttackData data)
    {
        if (animator == null) return;

        animator.SetFloat(HashAttackSpeed, 1f);
        animator.CrossFadeInFixedTime(locomotionStateName, locomotionCrossFade);
    }


}
