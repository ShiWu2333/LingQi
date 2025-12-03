using UnityEngine;

[CreateAssetMenu(
    fileName = "AttackData",
    menuName = "GameConfigs/Attack Data")]
public class AttackData : ScriptableObject
{
    [Header("Meta")]
    public string attackId = "light_1";

    [Header("Hitbox")]
    public float hitRange = 1.5f;
    public float hitRadius = 1.0f;

    [Header("Timing (seconds)")]
    [Tooltip("前摇时间")]
    public float startup = 0.15f;

    [Tooltip("有效判定时间")]
    public float active = 0.08f;

    [Tooltip("后摇时间")]
    public float recovery = 0.25f;

    [Header("Cost")]
    public float staminaCost = 0f;
    public float manaCost = 0f;

    [Header("Damage (Phase 4 才会真正用到)")]
    public float damage = 10f;

    [Header("Movement Multipliers")]
    [Tooltip("前摇阶段移动速度倍率")]
    public float moveMultiplierStartup = 0.4f;

    [Tooltip("Active 阶段移动速度倍率")]
    public float moveMultiplierActive = 0.2f;

    [Tooltip("后摇阶段移动速度倍率")]
    public float moveMultiplierRecovery = 0.7f;

    [Header("Step Forward (可选)")]
    [Tooltip("是否在攻击开始时向前小位移")]
    public bool stepForward = false;

    [Tooltip("向前位移距离")]
    public float stepDistance = 1.0f;

    [Tooltip("位移耗时（和 startup 叠加）")]
    public float stepDuration = 0.1f;

    [Header("Cancel / Buffer (以后可以细调)")]
    [Tooltip("从前摇开始多少秒后允许翻滚打断")]
    public float cancelableAfter = 0.1f;

    [Tooltip("在 Recovery 末尾多少秒可以预输入下一个攻击（连击用）")]
    public float bufferWindow = 0.15f;

    [Header("Poise / Impact")]
    public int poiseDamage = 0;          // 每次攻击削韧量
    public ImpactGrade impact = ImpactGrade.Small; // 冲击力等级

    [Header("Combo (Soft Combo)")]
    public string comboGroupId;          // 例如 "Sword_L", "Greatsword_L"
    public int comboStepIndex = 0;       // 0 = 第一段，1 = 第二段...

    [Tooltip("下一段连击的 AttackData（没有就留空）")]
    public AttackData nextCombo;         // 连段链表结构

    [Header("Combo Input Window (相对 Attack timeline 时间)")]
    public float comboInputOpenTime = 0.0f;   // 允许按键进入下一段连击的开始时间
    public float comboInputCloseTime = 0.0f;  // 结束时间

    [Header("动画绑定（表现层，但跟这招强绑定）")]
    public AnimationClip animationClip;      // 这一招用的动画
    public string animatorStateName;         // Animator 里对应的 state 名字，比如 "Sword_Light_1"


}
