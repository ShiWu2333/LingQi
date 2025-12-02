using LingQi.Player;
using TMPro;
using UnityEngine;

namespace LingQi.Combat
{
    [DisallowMultipleComponent]
    public class DummyTarget : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 120f;
        [SerializeField] private float resetDelay = 1.5f;
        [SerializeField] private Color damageTextColor = Color.yellow;

        private float health;

        private void Awake()
        {
            health = maxHealth;
            EnsureCollider();
        }

        public void ApplyDamage(float amount, Vector3 hitPoint)
        {
            health = Mathf.Max(0f, health - Mathf.Max(0f, amount));
            SpawnDamageText(amount, hitPoint);

            if (health <= 0f)
            {
                Invoke(nameof(ResetHealth), resetDelay);
            }
        }

        private void ResetHealth()
        {
            health = maxHealth;
        }

        private void SpawnDamageText(float amount, Vector3 hitPoint)
        {
            GameObject textObject = new GameObject("DamageText");
            textObject.transform.position = hitPoint + Vector3.up * 0.25f;
            var text = textObject.AddComponent<TextMeshPro>();
            text.text = amount.ToString("0");
            text.fontSize = 3f;
            text.color = damageTextColor;
            text.alignment = TextAlignmentOptions.Center;

            var floating = textObject.AddComponent<FloatingDamageText>();
            floating.SetMotion(Vector3.up * 0.75f, 1.2f);
        }

        private void EnsureCollider()
        {
            if (!TryGetComponent<Collider>(out var collider))
            {
                collider = gameObject.AddComponent<CapsuleCollider>();
            }

            if (collider is CapsuleCollider capsule)
            {
                capsule.height = 2f;
                capsule.radius = 0.35f;
                capsule.center = new Vector3(0f, 1f, 0f);
            }
        }
    }
}
