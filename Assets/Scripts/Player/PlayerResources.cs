using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerResources : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private PlayerStatsConfig stats;

    [Header("Runtime Values (ReadOnly in Inspector)")]
    [SerializeField] private float currentHP;
    [SerializeField] private float currentStamina;
    [SerializeField] private float currentMana;

    private float staminaRegenDelayTimer;
    private PlayerDodgeController dodge;
    private FlashOnHit _flashOnHit;

    // 事件：以后 UI / 其他系统可以订阅
    public event Action<float, float> OnHPChanged;
    public event Action<float, float> OnStaminaChanged;
    public event Action<float, float> OnManaChanged;
    public event Action<float> OnDamaged;  // 受到了多少伤害（最终生效伤害）
    public event Action OnDeath;

    public float CurrentHP => currentHP;
    public float CurrentStamina => currentStamina;
    public float CurrentMana => currentMana;

    public float MaxHP => stats != null ? stats.maxHP : 0f;
    public float MaxStamina => stats != null ? stats.maxStamina : 0f;
    public float MaxMana => stats != null ? stats.maxMana : 0f;

    public bool IsDead => currentHP <= 0f;

    private void Awake()
    {
        if (stats == null)
        {
            Debug.LogError("[PlayerResources] Stats config is not assigned!", this);
            return;
        }
        dodge = GetComponent<PlayerDodgeController>();
        _flashOnHit = GetComponentInChildren<FlashOnHit>();   // 新增
        InitFromConfig();
    }

    private void Update()
    {
        if (stats == null) return;

        float dt = Time.deltaTime;
        TickStaminaRegen(dt);
        TickManaRegen(dt);
    }

    // 初始化
    public void InitFromConfig()
    {
        if (stats == null) return;

        currentHP = stats.maxHP;
        currentStamina = stats.maxStamina;
        currentMana = stats.maxMana;
        staminaRegenDelayTimer = 0f;

        RaiseAllChangedEvents();
    }

    private void RaiseAllChangedEvents()
    {
        OnHPChanged?.Invoke(currentHP, MaxHP);
        OnStaminaChanged?.Invoke(currentStamina, MaxStamina);
        OnManaChanged?.Invoke(currentMana, MaxMana);
    }

    // HP 操作
    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        // 闪避 i-frame：不受伤
        if (dodge != null && dodge.IsInvincible)
        {
            // Debug.Log("[PlayerResources] Damage ignored due to i-frame.");
            return;
        }

        currentHP -= amount;

        // 🔴 新增：真正扣血才广播受伤事件
        OnDamaged?.Invoke(amount);

        if (_flashOnHit != null && amount > 0f)
        {
            _flashOnHit.Trigger();
        }

        if (currentHP <= 0f)
        {
            currentHP = 0f;
            OnHPChanged?.Invoke(currentHP, MaxHP);
            HandleDeath();
        }
        else
        {
            OnHPChanged?.Invoke(currentHP, MaxHP);
        }
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f || MaxHP <= 0f) return;

        currentHP = Mathf.Min(currentHP + amount, MaxHP);
        OnHPChanged?.Invoke(currentHP, MaxHP);
    }

    private void HandleDeath()
    {
        Debug.Log("[PlayerResources] Player died.", this);
        OnDeath?.Invoke();
        // 复活逻辑之后再加
    }

    // Stamina 操作
    public bool TrySpendStamina(float amount)
    {
        if (amount <= 0f) return true;

        if (currentStamina < amount)
            return false;

        currentStamina -= amount;
        if (currentStamina < 0f) currentStamina = 0f;

        staminaRegenDelayTimer = stats.staminaRegenDelayAfterUse;

        OnStaminaChanged?.Invoke(currentStamina, MaxStamina);
        return true;
    }

    private void TickStaminaRegen(float dt)
    {
        if (currentStamina >= MaxStamina || stats.staminaRegenPerSecond <= 0f)
            return;

        if (staminaRegenDelayTimer > 0f)
        {
            staminaRegenDelayTimer -= dt;
            return;
        }

        currentStamina += stats.staminaRegenPerSecond * dt;
        if (currentStamina > MaxStamina) currentStamina = MaxStamina;

        OnStaminaChanged?.Invoke(currentStamina, MaxStamina);
    }

    // Mana 操作
    public bool TrySpendMana(float amount)
    {
        if (amount <= 0f) return true;

        if (currentMana < amount)
            return false;

        currentMana -= amount;
        if (currentMana < 0f) currentMana = 0f;

        OnManaChanged?.Invoke(currentMana, MaxMana);
        return true;
    }

    private void TickManaRegen(float dt)
    {
        if (currentMana >= MaxMana || stats.manaRegenPerSecond <= 0f)
            return;

        currentMana += stats.manaRegenPerSecond * dt;
        if (currentMana > MaxMana) currentMana = MaxMana;

        OnManaChanged?.Invoke(currentMana, MaxMana);
    }

    // 暴露一个获取配置的只读接口，给 Movement/Combat 用
    public PlayerStatsConfig StatsConfig => stats;
}
