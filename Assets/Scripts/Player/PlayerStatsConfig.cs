using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStatsConfig", menuName = "Configs/Player Stats Config")]
public class PlayerStatsConfig : ScriptableObject
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float maxStamina = 50f;
    [SerializeField] private float maxMana = 30f;
    [SerializeField] private float staminaRegenPerSecond = 6f;
    [SerializeField] private float manaRegenPerSecond = 4f;

    public float MaxHealth => Mathf.Max(1f, maxHealth);
    public float MaxStamina => Mathf.Max(0f, maxStamina);
    public float MaxMana => Mathf.Max(0f, maxMana);
    public float StaminaRegenPerSecond => Mathf.Max(0f, staminaRegenPerSecond);
    public float ManaRegenPerSecond => Mathf.Max(0f, manaRegenPerSecond);
}
