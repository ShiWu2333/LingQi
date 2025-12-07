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

    [Header("Spirit / 灵力")]
    [SerializeField] private int currentSpirit = 0;   // Inspector 里只读查看就行

    // ================== Level Up：属性等级 & 加成 ==================

    [Header("Level Up - Runtime Levels")]
    [SerializeField] private int hpLevel;
    [SerializeField] private int staminaLevel;
    [SerializeField] private int manaLevel;
    [SerializeField] private int physicalAtkLevel;
    [SerializeField] private int magicAtkLevel;

    // 加成值（运行时）
    [SerializeField] private float bonusMaxHP;
    [SerializeField] private float bonusMaxStamina;
    [SerializeField] private float bonusMaxMana;
    [SerializeField] private float physicalBonus; // 0.15 = +15%
    [SerializeField] private float magicBonus;    // 0.20 = +20%

    public int HPLevel => hpLevel;
    public int StaminaLevel => staminaLevel;
    public int ManaLevel => manaLevel;
    public int PhysicalAtkLevel => physicalAtkLevel;
    public int MagicAtkLevel => magicAtkLevel;

    public float PhysicalBonus => physicalBonus;
    public float MagicBonus => magicBonus;

    // ================== Level Up - Config（Inspector 可调） ==================

    [Header("Level Up - Cost Config")]
    [SerializeField] private int hpBaseCost = 5;
    [SerializeField] private int hpCostPerLevel = 5;

    [SerializeField] private int staminaBaseCost = 4;
    [SerializeField] private int staminaCostPerLevel = 4;

    [SerializeField] private int manaBaseCost = 5;
    [SerializeField] private int manaCostPerLevel = 5;

    [SerializeField] private int physicalBaseCost = 6;
    [SerializeField] private int physicalCostPerLevel = 3;

    [SerializeField] private int magicBaseCost = 6;
    [SerializeField] private int magicCostPerLevel = 3;

    [Header("Level Up - Gain Config")]
    [SerializeField] private float hpPerLevel = 10f;
    [SerializeField] private float staminaPerLevel = 5f;
    [SerializeField] private float manaPerLevel = 10f;
    [SerializeField] private float physicalBonusPerLevel = 0.02f; // +2%
    [SerializeField] private float magicBonusPerLevel = 0.02f;    // +2%

    // ================== 内部状态 ==================

    private float staminaRegenDelayTimer;
    private PlayerDodgeController dodge;
    private FlashOnHit _flashOnHit;

    // 事件：以后 UI / 其他系统可以订阅
    public event Action<float, float> OnHPChanged;
    public event Action<float, float> OnStaminaChanged;
    public event Action<float, float> OnManaChanged;
    public event Action<float> OnDamaged;  // 受到了多少伤害（最终生效伤害）
    public event Action OnDeath;
    public event Action<int> OnSpiritChanged;

    public float CurrentHP => currentHP;
    public float CurrentStamina => currentStamina;
    public float CurrentMana => currentMana;

    // ⭐ Max 值要包含加点的 bonus
    public float MaxHP => (stats != null ? stats.maxHP : 0f) + bonusMaxHP;
    public float MaxStamina => (stats != null ? stats.maxStamina : 0f) + bonusMaxStamina;
    public float MaxMana => (stats != null ? stats.maxMana : 0f) + bonusMaxMana;

    public bool IsDead => currentHP <= 0f;
    public int CurrentSpirit => currentSpirit;

    private void Awake()
    {
        if (stats == null)
        {
            Debug.LogError("[PlayerResources] Stats config is not assigned!", this);
            return;
        }

        dodge = GetComponent<PlayerDodgeController>();
        _flashOnHit = GetComponentInChildren<FlashOnHit>();

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

        // 等级和加成默认从 0 开始（如果你希望可配置初始等级，可以在这里根据某个初始值重算 bonus）
        hpLevel = staminaLevel = manaLevel = 0;
        physicalAtkLevel = magicAtkLevel = 0;
        bonusMaxHP = bonusMaxStamina = bonusMaxMana = 0f;
        physicalBonus = magicBonus = 0f;

        RaiseAllChangedEvents();
    }

    private void RaiseAllChangedEvents()
    {
        OnHPChanged?.Invoke(currentHP, MaxHP);
        OnStaminaChanged?.Invoke(currentStamina, MaxStamina);
        OnManaChanged?.Invoke(currentMana, MaxMana);
        OnSpiritChanged?.Invoke(currentSpirit);
    }

    // ================== HP 操作 ==================

    public void TakeDamage(float amount, Vector3 hitPoint, ImpactGrade impact)
    {
        if (IsDead || amount <= 0f) return;

        // 闪避 i-frame：不受伤
        if (dodge != null && dodge.IsInvincible)
            return;

        currentHP -= amount;

        OnDamaged?.Invoke(amount);
        PlayHitFeedback(hitPoint, amount, impact);

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

    private void PlayHitFeedback(Vector3 worldPos, float amount, ImpactGrade impact)
    {
        Vector3 popupPos = worldPos + Vector3.up * 0.8f;

        if (_flashOnHit != null)
            _flashOnHit.Trigger(worldPos, impact);

        DamagePopupManager.Show(amount, popupPos);
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

    // ================== Stamina 操作 ==================

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

    // ================== Mana 操作 ==================

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

    // ================== 灵力对外接口 ==================

    /// <summary>击杀敌人等获得灵力。</summary>
    public void AddSpirit(int amount)
    {
        if (amount <= 0) return;

        currentSpirit += amount;
        OnSpiritChanged?.Invoke(currentSpirit);
    }

    /// <summary>消耗灵力（用于加点）。成功返回 true，失败 false。</summary>
    public bool TrySpendSpirit(int amount)
    {
        if (amount <= 0) return true;

        if (currentSpirit < amount)
            return false;

        currentSpirit -= amount;
        OnSpiritChanged?.Invoke(currentSpirit);
        return true;
    }

    // ================== Level Up：成本 & 升级逻辑 ==================

    public int GetLevelUpCost(LevelUpStat stat)
    {
        switch (stat)
        {
            case LevelUpStat.MaxHP:
                return hpBaseCost + hpLevel * hpCostPerLevel;
            case LevelUpStat.MaxStamina:
                return staminaBaseCost + staminaLevel * staminaCostPerLevel;
            case LevelUpStat.MaxMana:
                return manaBaseCost + manaLevel * manaCostPerLevel;
            case LevelUpStat.PhysicalAttack:
                return physicalBaseCost + physicalAtkLevel * physicalCostPerLevel;
            case LevelUpStat.MagicAttack:
                return magicBaseCost + magicAtkLevel * magicCostPerLevel;
            default:
                return int.MaxValue;
        }
    }

    public bool TryLevelUp(LevelUpStat stat)
    {
        int cost = GetLevelUpCost(stat);
        if (!TrySpendSpirit(cost))
            return false;

        switch (stat)
        {
            case LevelUpStat.MaxHP:
                hpLevel++;
                bonusMaxHP += hpPerLevel;
                currentHP += hpPerLevel; // 升级顺便回一点血
                OnHPChanged?.Invoke(currentHP, MaxHP);
                break;

            case LevelUpStat.MaxStamina:
                staminaLevel++;
                bonusMaxStamina += staminaPerLevel;
                currentStamina += staminaPerLevel;
                OnStaminaChanged?.Invoke(currentStamina, MaxStamina);
                break;

            case LevelUpStat.MaxMana:
                manaLevel++;
                bonusMaxMana += manaPerLevel;
                currentMana += manaPerLevel;
                OnManaChanged?.Invoke(currentMana, MaxMana);
                break;

            case LevelUpStat.PhysicalAttack:
                physicalAtkLevel++;
                physicalBonus += physicalBonusPerLevel;
                break;

            case LevelUpStat.MagicAttack:
                magicAtkLevel++;
                magicBonus += magicBonusPerLevel;
                break;
        }

        return true;
    }

    // 暴露一个获取配置的只读接口，给 Movement/Combat 用
    public PlayerStatsConfig StatsConfig => stats;
}
