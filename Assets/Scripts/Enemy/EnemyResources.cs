using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyResources : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private EnemyStatsConfig stats;

    [Header("Runtime (ReadOnly)")]
    [SerializeField] private float currentHP;

    public event Action<float, float> OnHPChanged;
    public event Action OnDeath;
    public event Action<float> OnDamaged;

    public EnemyStatsConfig Stats => stats;
    public float CurrentHP => currentHP;
    public float MaxHP => stats != null ? stats.maxHP : 0f;

    private bool _isDead;
    private FlashOnHit _flashOnHit;

    private void Awake()
    {
        if (stats == null)
            Debug.LogError("[EnemyResources] EnemyStatsConfig is not assigned!", this);

        _flashOnHit = GetComponent<FlashOnHit>();

        InitFromConfig();
    }

    public void InitFromConfig()
    {
        if (stats == null) return;

        currentHP = stats.maxHP;
        _isDead = false;
        OnHPChanged?.Invoke(currentHP, MaxHP);
    }

    // ===== 对外接口：不带命中点（旧代码可继续用） =====
    public void TakeDamage(float amount)
    {
        // 默认把命中点设在自己身上，冲击力给一个默认值
        TakeDamage(amount, transform.position, ImpactGrade.Small);
    }

    // ===== 对外接口：带命中点 + 冲击力（推荐） =====
    public void TakeDamage(float amount, Vector3 hitPoint, ImpactGrade impact)
    {
        if (_isDead || amount <= 0f) return;

        currentHP -= amount;
        OnDamaged?.Invoke(amount);

        // 受击反馈（闪白 + 后仰 + 痛感数字）
        PlayHitFeedback(hitPoint, amount, impact);

        if (currentHP <= 0f)
        {
            currentHP = 0f;
            _isDead = true;
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
        {
            _flashOnHit.Trigger(worldPos, impact);
        }

        DamagePopupManager.Show(amount, popupPos);
    }

    private void HandleDeath()
    {
        Debug.Log("[EnemyResources] Enemy died.", this);
        OnDeath?.Invoke();
        gameObject.SetActive(false);
    }
}
