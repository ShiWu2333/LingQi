using UnityEngine;

namespace LingQi.Combat
{
    [DisallowMultipleComponent]
    public class Weapon : MonoBehaviour
    {
        [SerializeField] private string weaponName = "Weapon";
        [SerializeField] private float lightDamage = 12f;
        [SerializeField] private float heavyDamage = 24f;
        [SerializeField] private float range = 2.75f;
        [SerializeField] private float lightStaminaCost = 10f;
        [SerializeField] private float heavyManaCost = 8f;

        public string WeaponName => weaponName;
        public float LightDamage => lightDamage;
        public float HeavyDamage => heavyDamage;
        public float Range => range;
        public float LightStaminaCost => lightStaminaCost;
        public float HeavyManaCost => heavyManaCost;
    }
}
