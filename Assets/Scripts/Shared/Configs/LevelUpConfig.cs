using UnityEngine;

[CreateAssetMenu(
    fileName = "LevelUpConfig",
    menuName = "GameConfigs/Player/Level Up Config")]
public class LevelUpConfig : ScriptableObject
{
    [Header("Level Up - Cost Config")]
    public int hpBaseCost = 5;
    public int hpCostPerLevel = 5;

    public int staminaBaseCost = 4;
    public int staminaCostPerLevel = 4;

    public int manaBaseCost = 5;
    public int manaCostPerLevel = 5;

    public int physicalBaseCost = 6;
    public int physicalCostPerLevel = 3;

    public int magicBaseCost = 6;
    public int magicCostPerLevel = 3;

    [Header("Level Up - Gain Config")]
    public float hpPerLevel = 10f;
    public float staminaPerLevel = 5f;
    public float manaPerLevel = 10f;
    public float physicalBonusPerLevel = 0.02f; // +2%
    public float magicBonusPerLevel = 0.02f;    // +2%
}
