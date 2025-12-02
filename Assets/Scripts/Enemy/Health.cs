using UnityEngine;

[DisallowMultipleComponent]
public class Health : MonoBehaviour
{
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float currentHP;

    public float MaxHP => maxHP;
    public float CurrentHP => currentHP;
    public bool IsDead => currentHP <= 0f;

    private void Awake()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, transform.position + Vector3.up * 1.5f);
    }
    public void TakeDamage(float amount, Vector3 hitPoint)
    {
        if (IsDead || amount <= 0f) return;

        currentHP -= amount;

        var flash = GetComponent<FlashOnHit>();
        if (flash != null)
        {
            flash.Trigger();
        }

        // 在命中点附近生成伤害数字
        DamagePopupManager.Show(amount, hitPoint + Vector3.up * 0.2f);

        if (currentHP <= 0f)
        {
            currentHP = 0f;
            HandleDeath();
        }
    }
    private void HandleDeath()
    {
        // 这版原型先简单粗暴：直接 Destroy
        // 以后可以换成 播放动画 / Ragdoll / SetActive(false) 等
        Destroy(gameObject);
    }
}
