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
    public HitboxShape hitboxShape = HitboxShape.Sphere;

    [Tooltip("相对于 meleeHitboxPivot / Player 的本地偏移")]
    public Vector3 hitboxLocalOffset = new Vector3(0f, 0f, 1f);

    // Sphere 专用
    [Min(0f)]
    public float hitboxRadius = 1.0f;

    // Box 专用（半尺寸）
    public Vector3 hitboxBoxHalfExtents = new Vector3(0.5f, 0.5f, 1.0f);

    // Capsule 专用
    [Min(0f)]
    public float hitboxCapsuleRadius = 0.7f;
    [Min(0f)]
    public float hitboxCapsuleHeight = 2.0f;

    [Tooltip("胶囊朝向，本地空间方向，一般用 (0,0,1) 表示前后，或 (0,1,0) 表示上下")]
    public Vector3 hitboxCapsuleDirection = Vector3.forward;

    [Header("Timing (seconds)")]
    [Tooltip("前摇时间（Startup），此阶段一般不产生伤害")]
    public float startup = 0.15f;

    [Tooltip("有效判定阶段（Active）的总时长")]
    public float active = 0.08f;

    [Tooltip("后摇时间（Recovery），大多数字面上已出刀但动作还没完全结束")]
    public float recovery = 0.25f;

    [Header("Hit Window (相对 Attack timeline 的绝对时间，单位：秒)")]
    [Tooltip("从攻击开始后多少秒起，开启伤害判定；若为 0 且 hitEndTime <= 0，则默认进入 Active 时判一次")]
    public float hitStartTime = 0.0f;

    [Tooltip("从攻击开始后多少秒，关闭伤害判定；若 <= 0 则不额外关闭，由逻辑控制")]
    public float hitEndTime = 0.0f;

    // ================== 资源消耗与伤害 ==================

    [Header("Cost")]
    [Tooltip("每次攻击消耗的体力值")]
    public float staminaCost = 0f;

    [Tooltip("每次攻击消耗的法力值（近战通常为 0）")]
    public float manaCost = 0f;

    [Header("Damage")]
    [Tooltip("这一击的基础伤害（后续会叠加玩家攻力、敌人防御等）")]
    public float damage = 10f;

    [Tooltip("物理伤害系数（1 = 100% 参与玩家物攻加成）")]
    public float physicalCoef = 1f;

    [Tooltip("法术伤害系数（1 = 100% 参与玩家法攻加成）")]
    public float magicCoef = 0f;

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


    [Header("VFX")]
    [Tooltip("挥砍弧线特效（跟随 SlashSocket 旋转的那条 arc）")]
    public GameObject slashVFXPrefab;

    [Tooltip("起手瞬间的爆光 / 溅射，不跟随剑旋转")]
    public GameObject splashVFXPrefab;

    [Tooltip("命中时在 hitPoint 生成的特效")]
    public GameObject hitVFXPrefab;

    [Tooltip("相对于 SlashSocket 的本地偏移")]
    public Vector3 slashLocalOffset = Vector3.zero;

    [Tooltip("相对于 SlashSocket 的本地额外旋转（度数）")]
    public Vector3 slashLocalEulerOffset = Vector3.zero;

    [Tooltip("slash 弧线整体缩放")]
    public float slashScale = 1f;

    [Tooltip("slash 原始时长（Prefab 主粒子 Duration）")]
    public float vfxBaseDuration = 1.0f;

    [Tooltip("希望这一招的 slash 在多少秒内播完（Active 匹配用）")]
    public float vfxLifetime = 0.5f;

    // ================== Projectile（远程攻击支持） ==================
    [Header("Projectile Attack (Optional)")]
    public bool isProjectileAttack = false;

    [Tooltip("发射物 prefab（必须包含 Projectile 组件）")]
    public GameObject projectilePrefab;

    [Tooltip("发射点相对玩家坐标偏移（若未提供 SpawnPoint）")]
    public Vector3 projectileSpawnOffset = new Vector3(0, 1.0f, 0.6f);

    [Tooltip("发射速度（m/s）")]
    public float projectileSpeed = 16f;

    [Tooltip("投射物最大生存时间")]
    public float projectileLifeTime = 3f;

    [Tooltip("发射数量（轻击 = 1，重击 = 3）")]
    public int projectileCount = 1;

    [Tooltip("多发之间的间隔（重击三连发）")]
    public float projectileInterval = 0.12f;

    [Header("Projectile Homing (Optional)")]
    [Tooltip("是否对锁定目标进行软追踪")]
    public bool projectileHoming = false;

    [Tooltip("追踪转向速度（度/秒），例如 360 = 1 秒最多转 360 度")]
    public float projectileHomingTurnSpeedDeg = 360f;

    [Tooltip("瞄准目标时的高度偏移，比如瞄准敌人胸口而不是脚")]
    public float projectileHomingHeightOffset = 0.8f;

}

public enum SwordMotionType
{
    Swing,      // 普通横斩：右→左 / 左→右
    DashSwing,  // 冲刺攻击那种前冲大挥砍（目前和 Swing 行为一致，主要是标签）
}

[System.Serializable]
public class SwordMotionConfig
{
    [Header("Swing Arc (Yaw 水平挥砍)")]
    [Tooltip("Active 阶段开始时的角度（绕 Y 轴，右正左负）")]
    public float startAngle = -60f;

    [Tooltip("Active 阶段结束时的角度")]
    public float endAngle = 60f;

    [Tooltip("目前暂时不用脚本控制半径，只当设计参考")]
    public float radius = 1.1f;

    [Header("Idle Pose (Yaw)")]
    [Tooltip("Idle 状态下 Pivot 的 Y 角度")]
    public float idleAngle = 0f;

    [Tooltip("Idle 状态下的参考半径")]
    public float idleRadius = 0.8f;

    [Header("Extra Motion (Yaw)")]
    [Tooltip("抬手时在 startAngle 方向再往后拉多少度")]
    public float anticipationOffset = 20f;

    [Tooltip("收招时在 idleAngle 方向再甩出去多少度")]
    public float overshootOffset = 10f;


    // ================== Pitch：垂直挥砍 ==================
    [Header("Vertical Arc (Pitch 垂直挥砍，绕 X)")]
    [Tooltip("Active 开始时的垂直角，正为抬刀，负为向下劈")]
    public float startPitch = 0f;

    [Tooltip("Active 结束时的垂直角")]
    public float endPitch = 0f;

    [Header("Idle Pose (Pitch)")]
    [Tooltip("Idle 状态下 Pivot 的 X 角度")]
    public float idlePitch = 0f;

    [Header("Extra Motion (Pitch)")]
    [Tooltip("抬手时在 startPitch 方向再拉多少度")]
    public float anticipationPitchOffset = 0f;

    [Tooltip("收招时在 idlePitch 方向再甩多少度")]
    public float overshootPitchOffset = 0f;


    // ================== Roll：绕剑身扭转 ==================
    [Header("Twist Arc (Roll 绕 Z 轴扭转)")]
    [Tooltip("Active 开始时的 Roll 角度，正为顺时针扭转")]
    public float startRoll = 0f;

    [Tooltip("Active 结束时的 Roll 角度")]
    public float endRoll = 0f;

    [Header("Idle Pose (Roll)")]
    [Tooltip("Idle 状态下 Pivot 的 Z 角度")]
    public float idleRoll = 0f;

    [Header("Extra Motion (Roll)")]
    [Tooltip("抬手时在 startRoll 方向再拉多少度")]
    public float anticipationRollOffset = 0f;

    [Tooltip("收招时在 idleRoll 方向再甩多少度")]
    public float overshootRollOffset = 0f;
}


// 如果你没在别处定义 HitboxShape，就在这里放一个；
// 若已经在 CombatEnums 里有，删掉这个避免重复定义。
