using UnityEngine;

public class PlayerResources : MonoBehaviour
{
    [SerializeField] private PlayerStatsConfig statsConfig;
    [SerializeField] private bool startWithFullResources = true;

    private const float DefaultHealth = 100f;
    private const float DefaultStamina = 50f;
    private const float DefaultMana = 30f;
    private const float DefaultStaminaRegen = 6f;
    private const float DefaultManaRegen = 4f;

    public float CurrentHealth { get; private set; }
    public float CurrentStamina { get; private set; }
    public float CurrentMana { get; private set; }

    public float MaxHealth => statsConfig != null ? statsConfig.MaxHealth : DefaultHealth;
    public float MaxStamina => statsConfig != null ? statsConfig.MaxStamina : DefaultStamina;
    public float MaxMana => statsConfig != null ? statsConfig.MaxMana : DefaultMana;
    private float StaminaRegenPerSecond => statsConfig != null ? statsConfig.StaminaRegenPerSecond : DefaultStaminaRegen;
    private float ManaRegenPerSecond => statsConfig != null ? statsConfig.ManaRegenPerSecond : DefaultManaRegen;

    private void Awake()
    {
        InitializeResources();
    }

    private void Update()
    {
        RegenerateResources(Time.deltaTime);
    }

    public void InitializeResources()
    {
        if (startWithFullResources)
        {
            CurrentHealth = MaxHealth;
            CurrentStamina = MaxStamina;
            CurrentMana = MaxMana;
            return;
        }

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);
        CurrentStamina = Mathf.Clamp(CurrentStamina, 0f, MaxStamina);
        CurrentMana = Mathf.Clamp(CurrentMana, 0f, MaxMana);
    }

    public bool TryConsumeHealth(float amount)
    {
        if (amount <= 0f || CurrentHealth <= 0f)
        {
            return false;
        }

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        return true;
    }

    public bool TryConsumeStamina(float amount)
    {
        if (amount <= 0f || amount > CurrentStamina)
        {
            return false;
        }

        CurrentStamina -= amount;
        return true;
    }

    public bool TryConsumeMana(float amount)
    {
        if (amount <= 0f || amount > CurrentMana)
        {
            return false;
        }

        CurrentMana -= amount;
        return true;
    }

    public void RestoreHealth(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
    }

    public void RestoreStamina(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        CurrentStamina = Mathf.Min(MaxStamina, CurrentStamina + amount);
    }

    public void RestoreMana(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        CurrentMana = Mathf.Min(MaxMana, CurrentMana + amount);
    }

    private void RegenerateResources(float deltaTime)
    {
        if (StaminaRegenPerSecond > 0f)
        {
            CurrentStamina = Mathf.Min(MaxStamina, CurrentStamina + StaminaRegenPerSecond * deltaTime);
        }

        if (ManaRegenPerSecond > 0f)
        {
            CurrentMana = Mathf.Min(MaxMana, CurrentMana + ManaRegenPerSecond * deltaTime);
        }
    }
}
