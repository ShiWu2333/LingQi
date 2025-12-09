using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyStatsConfig",
    menuName = "GameConfigs/Combat/Enemy Stats Config")]
public class EnemyStatsConfig : ScriptableObject
{
    [Header("===  ===")]
    public float maxHP = 50f;

    [Header("=== ԣPoise===")]
    public float maxPoise = 40f;
    public float poiseRegenDelay = 1.5f;
    public float poiseRegenPerSecond = 20f;
    public float breakDuration = 1.0f;

    [Header("=== ΪԣAI ã===")]
    public float moveSpeed = 3f;
    public float chaseSpeed = 4f;
    public float rotateSpeed = 12f;

    [Tooltip("ҵķΧ")]
    public float detectionRange = 10f;

    [Tooltip("루AI ͣ")]
    public float attackRange = 1.8f;

    [Tooltip("ÿùһ")]
    public float attackInterval = 1.5f;

    [Header("=== AI Behaviour ===")]
    [Tooltip(" hitbox ҵƫ")]
    public float hitboxHeightOffset = 1.0f;

    [Tooltip("Զ档 ڽӵ attack ֮ǰٵʱ")]
    public float attackReactionTime = 0.4f;
    [Tooltip("dot 阈ֵ，大概 36° 内才出手")]
    public float attackAngleThreshold = 0.8f;
    [Tooltip("满足条件时本次是否出手的概率")]
    public float attackChance = 0.7f;
    [Tooltip("攻击范围的缓冲区（稍微远一点也算进攻区域）")]
    public float attackRangeBuffer = 0.3f;

    [Tooltip("是否允许敌人随机使用第二种攻击")]
    public bool enableSecondAttack = false;
    [Range(0f, 1f)]
    [Tooltip("当允许第二种攻击时，本次出手改用 heavyAttack 的概率")]
    public float secondAttackChance = 0.4f;

    [Tooltip("attackRange * 这个 = 太近，下撤")]
    public float preferredMinDistFactor = 0.6f;
    [Tooltip("attackRange * 这个 = 舒服的中距离")]
    public float preferredMaxDistFactor = 0.9f;
    [Tooltip("绕圈时侧移距离")]
    public float strafeDistance = 1.5f;

    [Tooltip("抬手阶段的慢速移动速度")]
    public float attackTrackSpeed = 2.0f;
    [Tooltip("想要站在 attackRange*这个 的位置出刀")]
    public float attackTrackStopDistance = 0.8f;

    [Header("===  ===")]
    [Tooltip("ĬϵĹ")]
    public AttackData defaultAttack;

    [Tooltip("ǿĹѡ")]
    public AttackData heavyAttack;

    [Header("=== Ӧ ===")]
    [Tooltip("ͬ״̬¶ԲͬӲֱӦ")]
    public ImpactProfile impactProfile;

    [Header("=== ܻѡ ===")]
    public Color hitFlashColor = Color.white;
    public float hitFlashDuration = 0.1f;

    [Header("===  ===")]
    [Tooltip("击杀后给予玩家的灵力数量")]
    public int spiritReward = 1;
}
