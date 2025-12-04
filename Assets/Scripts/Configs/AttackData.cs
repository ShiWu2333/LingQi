using UnityEngine;

[CreateAssetMenu(
    fileName = "AttackData",
    menuName = "GameConfigs/Attack Data")]
public class AttackData : ScriptableObject
{
    [Header("Meta")]
    [Tooltip("这一招的唯一 ID，调试和日志用")]
    public string attackId = "light_1";

    // ================== 命中与时间 ==================

    [Header("Hitbox")]
    [Tooltip("逻辑命中范围的前向距离（例如 OverlapSphere 的中心距离玩家多远）")]
    public float hitRange = 1.5f;

    [Tooltip("逻辑命中范围的半径（近似扇形/柱体的粗细）")]
    public float hitRadius = 1.0f;

    [Header("Timing (seconds)")]

    [Tooltip("前摇时间（Startup），此阶段一般不产生伤害")]
    public float startup = 0.15f;

    [Tooltip("有效判定阶段（Active）的总时长")]
    public float active = 0.08f;

    [Tooltip("后摇时间（Recovery），大多数字面上已出刀但动作还没完全结束")]
    public float recovery = 0.25f;

    [Header("Hit Window (相对 Attack timeline 的绝对时间，单位：秒)")]
    [Tooltip("从攻击开始后多少秒起，开启伤害判定；若为 0 且 hitEndTime <= 0，则默认整个 Active 段")]
    public float hitStartTime = 0.0f;

    [Tooltip("从攻击开始后多少秒，关闭伤害判定；若 <= 0 则默认在 startup+active 结束时关闭")]
    public float hitEndTime = 0.0f;

    // ================== 资源消耗与伤害 ==================

    [Header("Cost")]
    [Tooltip("每次攻击消耗的体力值")]
    public float staminaCost = 0f;

    [Tooltip("每次攻击消耗的法力值（近战通常为 0）")]
    public float manaCost = 0f;

    [Header("Damage (Phase 4 才会真正用到)")]
    [Tooltip("这一击的基础伤害（后续会叠加玩家攻力、敌人防御等）")]
    public float damage = 10f;

    // ================== 移动相关 ==================

    [Header("Movement Multipliers")]
    [Tooltip("前摇阶段玩家移动速度倍率")]
    public float moveMultiplierStartup = 0.4f;

    [Tooltip("Active 阶段玩家移动速度倍率")]
    public float moveMultiplierActive = 0.2f;

    [Tooltip("后摇阶段玩家移动速度倍率")]
    public float moveMultiplierRecovery = 0.7f;

    [Header("Step Forward (可选)")]
    [Tooltip("是否在攻击期间向前小位移（例如短剑前冲一步）")]
    public bool stepForward = false;

    [Tooltip("总前冲距离（世界空间）")]
    public float stepDistance = 1.0f;

    [Tooltip("前冲位移耗时（通常不超过 startup+active）")]
    public float stepDuration = 0.1f;

    // ================== 取消 / 预输入 ==================

    [Header("Cancel / Buffer")]
    [Tooltip("从攻击开始多少秒后，允许翻滚/下一招打断（不含 dashAttack 特例）")]
    public float cancelableAfter = 0.1f;


    // ================== 韧性 / 冲击力 ==================

    [Header("Poise / Impact")]
    [Tooltip("这一击对敌人韧性的削减量（用于打出硬直/击倒）")]
    public int poiseDamage = 0;

    [Tooltip("冲击力等级，用于控制 CameraShake / HitStop 强度等")]
    public ImpactGrade impact = ImpactGrade.Small;

    // ================== 连击配置 ==================

    [Header("Combo (Soft Combo)")]
    [Tooltip("用来区别不同武器连段的分组 ID，例如 \"ShortSword_L\"、\"Greatsword_L\"")]
    public string comboGroupId;

    [Tooltip("连段步数：0 = 第一段，1 = 第二段...")]
    public int comboStepIndex = 0;

    [Tooltip("下一段连击的 AttackData（没有就留空，表示这一段是连段终点）")]
    public AttackData nextCombo;

    [Header("Combo Input Window (相对 Attack timeline 时间，单位：秒)")]
    [Tooltip("从攻击开始后多少秒起，按键会被视为下一段连击的输入")]
    public float comboInputOpenTime = 0.0f;

    [Tooltip("从攻击开始后多少秒止，之后按键不再视为下一段连击的预输入")]
    public float comboInputCloseTime = 0.0f;

    // ================== 动画表现绑定 ==================

    [Header("动画绑定（表现层）")]
    [Tooltip("这一招默认使用的 AnimationClip（如果你之后回到 Animator 流程）")]
    public AnimationClip animationClip;

    [Tooltip("Animator Controller 里对应的 State 名称，例如 \"Sword_Light_1\"")]
    public string animatorStateName;

    // ================== 程序化短剑挥砍配置（当前 Prototype 用） ==================

    [Header("Procedural Sword Motion (Prototype)")]
    [Tooltip("当前攻击的挥砍轨迹配置（绕玩家旋转的角度/半径等）")]
    public SwordMotionConfig swordMotion;
}

public enum SwordMotionType
{
    Swing,      // 普通横斩：右→左 / 左→右
    DashSwing,  // 冲刺攻击那种前冲大挥砍（目前和 Swing 行为一致，主要是标签）
}

[System.Serializable]
public class SwordMotionConfig
{
    [Header("Swing Arc")]
    [Tooltip("Active 阶段开始时的角度（右正左负）")]
    public float startAngle = -60f;

    [Tooltip("Active 阶段结束时的角度")]
    public float endAngle = 60f;

    [Tooltip("目前暂时不用脚本控制半径，只当设计参考")]
    public float radius = 1.1f;

    [Header("Idle Pose")]
    [Tooltip("Idle 状态下 Pivot 的 Y 角度")]
    public float idleAngle = 0f;

    [Tooltip("Idle 状态下的参考半径")]
    public float idleRadius = 0.8f;

    [Header("Extra Motion")]
    [Tooltip("抬手时在 startAngle 方向再往后拉多少度（做预备动作）。例如 20 = 再往身后拉 20 度")]
    public float anticipationOffset = 20f;

    [Tooltip("收招时在 idleAngle 方向再甩出去多少度（做过冲）。例如 10 = 再甩 10 度再收回")]
    public float overshootOffset = 10f;
}
