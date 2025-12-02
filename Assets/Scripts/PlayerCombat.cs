using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Weapons")]
    [SerializeField] private WeaponConfig lightWeapon;
    [SerializeField] private WeaponConfig heavyWeapon;
    [SerializeField] private Transform cameraTransform;

    [Header("Feedback")]
    [SerializeField] private LayerMask hitMask = ~0;

    private PlayerController resources;

    private void Awake()
    {
        resources = GetComponent<PlayerController>();
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryLightAttack();
        }

        if (Input.GetMouseButtonDown(1))
        {
            TryHeavyAttack();
        }
    }

    private void TryLightAttack()
    {
        WeaponConfig config = lightWeapon;
        if (config == null)
        {
            config = WeaponConfig.CreateFallback("Light Attack", 15f, 3f, 10f, 0f);
        }

        if (!resources.TryConsumeStamina(config.StaminaCost))
        {
            return;
        }

        PerformAttack(config);
    }

    private void TryHeavyAttack()
    {
        WeaponConfig config = heavyWeapon;
        if (config == null)
        {
            config = WeaponConfig.CreateFallback("Heavy Attack", 30f, 4.5f, 0f, 15f);
        }

        if (!resources.TryConsumeMana(config.ManaCost))
        {
            return;
        }

        PerformAttack(config);
    }

    private void PerformAttack(WeaponConfig config)
    {
        Transform aim = cameraTransform != null ? cameraTransform : transform;
        Ray ray = new Ray(aim.position, aim.forward);

        if (Physics.Raycast(ray, out RaycastHit hitInfo, config.Range, hitMask, QueryTriggerInteraction.Ignore))
        {
            var dummy = hitInfo.collider.GetComponentInParent<EnemyDummy>();
            if (dummy != null)
            {
                dummy.ApplyDamage(config.Damage);
                DamagePopup.Spawn(hitInfo.point + Vector3.up * 0.5f, config.Damage, dummy.PopupColor);
            }
        }
    }
}
