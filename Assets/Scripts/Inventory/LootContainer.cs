using System.Collections.Generic;
using UnityEngine;

public interface ILootSource
{
    bool IsSearched { get; }

    /// <summary>本次搜索需要的时间（秒）。</summary>
    float GetSearchTime();

    /// <summary>仅用于“全部拿走”之类的逻辑，暂时不用。</summary>
    List<ItemStack> TakeAllLoot();

    /// <summary>容器槽位数量。</summary>
    int SlotCount { get; }

    /// <summary>按索引读取一个格子的物品（可能为 null）。</summary>
    ItemStack GetSlotStack(int index);

    /// <summary>按索引写入一个格子的物品（null 表示清空）。</summary>
    void SetSlotStack(int index, ItemStack stack);
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class LootContainer : MonoBehaviour, ILootSource
{
    [Header("Loot Content")]
    [Tooltip("这就是箱子/尸体里实际存的东西，可以在 Inspector 里直接填。")]
    [SerializeField] private List<ItemStack> loot = new List<ItemStack>();

    [Header("Search Settings")]
    [Tooltip("搜索时间系数：最终时间 = max(baseSearchTime) * multiplier")]
    [SerializeField] private float searchTimeMultiplier = 1.0f;

    [Tooltip("最低搜索时间（避免 0）")]
    [SerializeField] private float minSearchTime = 0.3f;

    [Tooltip("最高搜索时间（保险，不让太夸张）")]
    [SerializeField] private float maxSearchTime = 5.0f;

    [Header("Flags")]
    [Tooltip("是否一旦搜刮就清空内容（true = 一次性容器）")]
    [SerializeField] private bool consumeOnSearch = true;

    [Tooltip("当容器被清空且 consumeOnSearch=true 时，是否销毁这个物体")]
    [SerializeField] private bool destroyWhenEmpty = false;

    [SerializeField] private bool isSearched = false;

    public bool IsSearched => isSearched && (loot == null || loot.Count == 0);

    public int SlotCount => loot != null ? loot.Count : 0;

    private void Reset()
    {
        // 默认把 Collider 设置为 Trigger，方便用 Trigger 做交互范围
        var col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    // ================= ILootSource：搜索时间 =================

    public float GetSearchTime()
    {
        if (loot == null || loot.Count == 0)
            return minSearchTime;

        float maxBase = 0f;

        foreach (var stack in loot)
        {
            if (stack == null || stack.item == null || stack.count <= 0)
                continue;

            float t = stack.item.baseSearchTime;
            if (t > maxBase)
                maxBase = t;
        }

        if (maxBase <= 0f)
            maxBase = minSearchTime;

        float finalTime = maxBase * Mathf.Max(0.01f, searchTimeMultiplier);
        finalTime = Mathf.Clamp(finalTime, minSearchTime, maxSearchTime);
        return finalTime;
    }

    // ================= ILootSource：按槽位访问 =================

    public ItemStack GetSlotStack(int index)
    {
        if (loot == null || index < 0 || index >= loot.Count)
            return null;

        var s = loot[index];
        if (s == null || s.item == null || s.count <= 0)
            return null;

        // 直接返回引用即可，UI 会用 SetSlotStack 写回去覆盖
        return s;
    }

    public void SetSlotStack(int index, ItemStack stack)
    {
        if (loot == null)
            loot = new List<ItemStack>();

        // 保证列表长度够
        while (loot.Count <= index)
            loot.Add(null);

        if (stack == null || stack.item == null || stack.count <= 0)
        {
            loot[index] = null;
        }
        else
        {
            // 存一份新的，避免外部继续持有引用乱改
            loot[index] = new ItemStack(stack.item, stack.count);
        }

        // 只要还有东西，就认为没彻底搜完
        isSearched = false;
    }

    // ================= ILootSource：全部拿走（暂时只留接口） =================

    public List<ItemStack> TakeAllLoot()
    {
        var result = new List<ItemStack>();

        if (loot != null)
        {
            foreach (var s in loot)
            {
                if (s == null || s.item == null || s.count <= 0)
                    continue;

                result.Add(new ItemStack(s.item, s.count));
            }
        }

        if (consumeOnSearch)
        {
            loot.Clear();
            isSearched = true;

            if (destroyWhenEmpty && result.Count > 0)
                Destroy(gameObject);
        }

        return result;
    }

    // ============== 辅助接口（比如敌人死亡时填战利品） ==============

    public void SetLoot(List<ItemStack> newLoot)
    {
        if (loot == null)
            loot = new List<ItemStack>();
        else
            loot.Clear();

        if (newLoot != null)
        {
            foreach (var s in newLoot)
            {
                if (s == null || s.item == null || s.count <= 0)
                    continue;

                loot.Add(new ItemStack(s.item, s.count));
            }
        }

        isSearched = false;
    }
}
