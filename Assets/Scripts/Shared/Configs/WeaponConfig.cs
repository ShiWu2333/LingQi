using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponConfig",
    menuName = "GameConfigs/Weapon Config")]
public class WeaponConfig : ScriptableObject
{
    [Header("Basic Attacks")]
    public AttackData lightAttack;
    public AttackData heavyAttack;
    public AttackData dashAttack;
}
