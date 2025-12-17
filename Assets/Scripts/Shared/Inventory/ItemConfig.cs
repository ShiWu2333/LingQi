using UnityEngine;

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

[CreateAssetMenu(menuName = "GameConfigs/Item", fileName = "Item_")]
public class ItemConfig : ScriptableObject
{
    [Header("Basic Info")]
    public string itemId;          // 唯一 ID（自己保证不重复，用于存档等）
    public string displayName;
    public Sprite icon;

    [Header("Gameplay")]
    public ItemRarity rarity = ItemRarity.Common;

    [Tooltip("一个格子中此物品最多可堆叠数量")]
    public int maxStack = 20;

    [Tooltip("重量或负重用（现在可以先不用）")]
    public float weight = 1f;

    [Header("Search")]
    [Tooltip("搜索这个物品需要的基础时间（越值钱越久）")]
    public float baseSearchTime = 1.0f;
}

[System.Serializable]
public class ItemStack
{
    public ItemConfig item;
    public int count;

    public bool IsValid => item != null && count > 0;

    public ItemStack(ItemConfig item, int count)
    {
        this.item = item;
        this.count = count;
    }
}
