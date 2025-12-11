using UnityEngine;

[CreateAssetMenu(
    fileName = "CombatBalanceConfig",
    menuName = "GameConfigs/Combat/Combat Balance Config")]
public class CombatBalanceConfig : ScriptableObject
{
    [Header("Hit Stop")]
    public float defaultHitStop = 0.05f;

    [Header("Stagger Durations")]
    public float lightStaggerDuration = 0.1f;
    public float mediumStaggerDuration = 0.35f;
    public float heavyStaggerDuration = 0.6f;
}
