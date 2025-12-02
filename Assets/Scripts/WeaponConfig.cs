using UnityEngine;

[System.Serializable]
public class WeaponConfig : MonoBehaviour
{
    [SerializeField] private string displayName = "Weapon";
    [SerializeField] private float damage = 10f;
    [SerializeField] private float range = 3f;
    [SerializeField] private float staminaCost = 10f;
    [SerializeField] private float manaCost = 0f;

    public string DisplayName => displayName;
    public float Damage => damage;
    public float Range => range;
    public float StaminaCost => staminaCost;
    public float ManaCost => manaCost;

    public static WeaponConfig CreateFallback(string name, float damage, float range, float staminaCost, float manaCost)
    {
        var temp = new GameObject("TempWeaponConfig").AddComponent<WeaponConfig>();
        temp.hideFlags = HideFlags.HideAndDontSave;
        temp.displayName = name;
        temp.damage = damage;
        temp.range = range;
        temp.staminaCost = staminaCost;
        temp.manaCost = manaCost;
        return temp;
    }
}
