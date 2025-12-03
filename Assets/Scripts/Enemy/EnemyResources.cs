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

    public EnemyStatsConfig Stats => stats;
    public float CurrentHP => currentHP;
    public float MaxHP => stats != null ? stats.maxHP : 0f;

    private bool _isDead;
    private FlashOnHit _flashOnHit;

    private void Awake()
    {
        if (stats == null)
        {
            Debug.LogError("[EnemyResources] EnemyStatsConfig is not assigned!", this);
        }

        _flashOnHit = GetComponent<FlashOnHit>();   // 别忘了缓存

        InitFromConfig();
    }

    public void InitFromConfig()
    {
        if (stats == null) return;

        currentHP = stats.maxHP;
        _isDead = false;
        OnHPChanged?.Invoke(currentHP, MaxHP);
    }

    // 不带命中点（备用）
    public void TakeDamage(float amount)
    {
        InternalTakeDamage(amount);
        PlayHitFeedback(transform.position, amount);
    }

    // 带命中点：玩家 Hitbox 调这个
    public void TakeDamage(float amount, Vector3 hitPoint)
    {
        InternalTakeDamage(amount);
        PlayHitFeedback(hitPoint, amount);
    }

    private void InternalTakeDamage(float amount)
    {
        if (_isDead) return;

        currentHP -= amount;
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

    private void PlayHitFeedback(Vector3 worldPos, float amount)
    {
        // 稍微往上抬一点，让数字不要埋进地板
        Vector3 popupPos = worldPos + Vector3.up * 0.8f;

        // 闪白
        if (_flashOnHit != null)
        {
            _flashOnHit.Trigger();
        }

        // 伤害数字
        DamagePopupManager.Show(amount, popupPos);
    }

    private void HandleDeath()
    {
        Debug.Log("[EnemyResources] Enemy died.", this);
        OnDeath?.Invoke();

        gameObject.SetActive(false);
    }
}
