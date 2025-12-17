using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Loot Table", fileName = "LootTable_")]
public class LootTableConfig : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public ItemConfig item;
        [Min(0f)] public float weight = 1f;   // 权重（不是百分比）
        [Min(1)] public int minCount = 1;
        [Min(1)] public int maxCount = 1;
    }

    [Header("How many rolls per container")]
    [Min(0)] public int rolls = 2;

    [Header("Unique item constraint (optional)")]
    public bool uniqueItems = true;

    [Header("Entries")]
    public List<Entry> entries = new List<Entry>();
}
