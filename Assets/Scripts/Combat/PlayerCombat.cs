using LingQi.Player;
using UnityEngine;

namespace LingQi.Combat
{
    [RequireComponent(typeof(PlayerResources))]
    [DisallowMultipleComponent]
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private Weapon primaryWeapon;
        [SerializeField] private Weapon secondaryWeapon;
        [SerializeField] private LayerMask hitMask = Physics.DefaultRaycastLayers;
        [SerializeField] private float attackCooldown = 0.3f;

        private Weapon currentWeapon;
        private PlayerResources resources;
        private float cooldownTimer;
        private Camera cachedCamera;

        private void Awake()
        {
            resources = GetComponent<PlayerResources>();
        }

        private void Start()
        {
            cachedCamera = Camera.main;
            currentWeapon = primaryWeapon != null ? primaryWeapon : secondaryWeapon;
        }

        private void Update()
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1) && primaryWeapon != null)
            {
                currentWeapon = primaryWeapon;
            }

            if (Input.GetKeyDown(KeyCode.Alpha2) && secondaryWeapon != null)
            {
                currentWeapon = secondaryWeapon;
            }

            if (currentWeapon == null)
            {
                return;
            }

            if (cooldownTimer <= 0f)
            {
                if (Input.GetButtonDown("Fire1"))
                {
                    TryLightAttack();
                }
                else if (Input.GetButtonDown("Fire2"))
                {
                    TryHeavyAttack();
                }
            }
        }

        public void ConfigureWeapons(Weapon primary, Weapon secondary)
        {
            primaryWeapon = primary;
            secondaryWeapon = secondary;
            currentWeapon = primaryWeapon != null ? primaryWeapon : secondaryWeapon;
        }

        private void TryLightAttack()
        {
            if (currentWeapon == null)
            {
                return;
            }

            if (!resources.TrySpendStamina(currentWeapon.LightStaminaCost))
            {
                return;
            }

            PerformAttack(currentWeapon.LightDamage, currentWeapon.Range);
        }

        private void TryHeavyAttack()
        {
            if (currentWeapon == null)
            {
                return;
            }

            if (!resources.TrySpendMana(currentWeapon.HeavyManaCost))
            {
                return;
            }

            PerformAttack(currentWeapon.HeavyDamage, currentWeapon.Range + 0.5f);
        }

        private void PerformAttack(float damage, float range)
        {
            cooldownTimer = attackCooldown;
            Vector3 origin = cachedCamera != null ? cachedCamera.transform.position : transform.position + Vector3.up;
            Vector3 direction = cachedCamera != null ? cachedCamera.transform.forward : transform.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                DummyTarget target = hit.collider.GetComponentInParent<DummyTarget>();
                if (target != null)
                {
                    target.ApplyDamage(damage, hit.point);
                }
            }
        }
    }
}
