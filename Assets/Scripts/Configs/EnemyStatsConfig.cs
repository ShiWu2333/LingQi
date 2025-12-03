using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyStatsConfig",
    menuName = "GameConfigs/Combat/Enemy Stats Config")]
public class EnemyStatsConfig : ScriptableObject
{
    [Header("=== 基础属性 ===")]
    public float maxHP = 50f;

    [Header("=== 韧性（Poise）===")]
    public float maxPoise = 40f;
    public float poiseRegenDelay = 1.5f;
    public float poiseRegenPerSecond = 20f;
    public float breakDuration = 1.0f;

    [Header("=== 行为属性（AI 用）===")]
    public float moveSpeed = 3f;
    public float chaseSpeed = 4f;
    public float rotateSpeed = 12f;

    [Tooltip("发现玩家的范围")]
    public float detectionRange = 10f;

    [Tooltip("攻击距离（AI 停下来攻击）")]
    public float attackRange = 1.8f;

    [Tooltip("每隔多久攻击一次")]
    public float attackInterval = 1.5f;

    [Header("=== 攻击数据 ===")]
    [Tooltip("敌人默认的攻击动作")]
    public AttackData defaultAttack;

    [Tooltip("更强的攻击（可选）")]
    public AttackData heavyAttack;

    [Header("=== 冲击力反应表 ===")]
    [Tooltip("不同状态下对不同冲击力的硬直反应")]
    public ImpactProfile impactProfile;

    [Header("=== 受击反馈（可选） ===")]
    public Color hitFlashColor = Color.white;
    public float hitFlashDuration = 0.1f;
}
