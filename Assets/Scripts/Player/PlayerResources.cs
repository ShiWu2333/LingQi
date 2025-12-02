using UnityEngine;

namespace LingQi.Player
{
    [DisallowMultipleComponent]
    public class PlayerResources : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;

        [Header("Stamina")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float staminaRegenPerSecond = 15f;
        [SerializeField] private float staminaRegenDelay = 0.6f;

        [Header("Mana")]
        [SerializeField] private float maxMana = 50f;
        [SerializeField] private float manaRegenPerSecond = 10f;
        [SerializeField] private float manaRegenDelay = 0.6f;

        public float Health { get; private set; }
        public float Stamina { get; private set; }
        public float Mana { get; private set; }

        private float staminaCooldown;
        private float manaCooldown;

        private void Awake()
        {
            Health = maxHealth;
            Stamina = maxStamina;
            Mana = maxMana;
        }

        private void Update()
        {
            TickResource(ref Stamina, maxStamina, ref staminaCooldown, staminaRegenDelay, staminaRegenPerSecond);
            TickResource(ref Mana, maxMana, ref manaCooldown, manaRegenDelay, manaRegenPerSecond);
        }

        public bool TrySpendStamina(float amount)
        {
            if (amount <= 0f)
            {
                return true;
            }

            if (Stamina < amount)
            {
                return false;
            }

            Stamina -= amount;
            staminaCooldown = staminaRegenDelay;
            return true;
        }

        public bool TrySpendMana(float amount)
        {
            if (amount <= 0f)
            {
                return true;
            }

            if (Mana < amount)
            {
                return false;
            }

            Mana -= amount;
            manaCooldown = manaRegenDelay;
            return true;
        }

        public void ReceiveDamage(float amount)
        {
            Health = Mathf.Max(0f, Health - Mathf.Max(0f, amount));
        }

        private void TickResource(ref float current, float maximum, ref float cooldown, float delay, float regenPerSecond)
        {
            if (cooldown > 0f)
            {
                cooldown -= Time.deltaTime;
                return;
            }

            if (current < maximum)
            {
                current = Mathf.Min(maximum, current + regenPerSecond * Time.deltaTime);
            }
        }
    }
}
