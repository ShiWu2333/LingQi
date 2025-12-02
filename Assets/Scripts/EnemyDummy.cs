using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyDummy : MonoBehaviour
{
    [SerializeField] private float maxHealth = 200f;
    [SerializeField] private Color popupColor = Color.yellow;

    public float CurrentHealth => currentHealth;
    public Color PopupColor => popupColor;

    private float currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void ApplyDamage(float amount)
    {
        currentHealth = Mathf.Max(0f, currentHealth - Mathf.Max(0f, amount));
        DamagePopup.Spawn(transform.position + Vector3.up * 1.5f, amount, popupColor);

        if (currentHealth <= 0.01f)
        {
            currentHealth = maxHealth;
        }
    }
}
