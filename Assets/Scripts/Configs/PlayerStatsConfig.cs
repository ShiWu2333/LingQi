using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerStatsConfig",
    menuName = "GameConfigs/Player Stats Config")]
public class PlayerStatsConfig : ScriptableObject
{
    [Header("HP")]
    public float maxHP = 100f;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float rotationSpeed = 12f;
    public float lockOnMaxDistance = 15f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaRegenPerSecond = 15f;
    public float staminaRegenDelayAfterUse = 0.4f;

    [Header("Mana")]
    public float maxMana = 50f;
    public float manaRegenPerSecond = 3f;

    [Header("Poise / ")]
    public float maxPoise = 100f;
    public float poiseRegenDelay = 2f;
    public float poiseRegenPerSecond = 30f;
    public float breakDuration = 2f;

    [Header("Impact / Ӧ")]
    public ImpactProfile impactProfile;

    [Header("Dodge")]
    public float dodgeDistance = 3f;
    public float dodgeDuration = 0.22f;
    public float dodgeStaminaCost = 20f;
    public float dodgeCooldown = 0.7f;
    public float dashAttackWindow = 0.25f;

    [Tooltip("ܿʼ i-frame")]
    public float dodgeIFrameStart = 0.05f;

    [Tooltip("ܿʼi-frame ")]
    public float dodgeIFrameEnd = 0.20f;
}
