using System.Collections.Generic;
using UnityEngine;

public interface ILootSource
{
    bool IsSearched { get; }
    float GetSearchTime();
    List<ItemStack> TakeAllLoot();
    int SlotCount { get; }
    ItemStack GetSlotStack(int index);
    void SetSlotStack(int index, ItemStack stack);
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class LootContainer : MonoBehaviour, ILootSource, IRunResettable
{
    [Header("Loot Content")]
    [Tooltip("这就是箱子/尸体里实际存的东西，可以在 Inspector 里直接填。")]
    [SerializeField] private List<ItemStack> loot = new List<ItemStack>();

    [Header("Search Settings")]
    [SerializeField] private float searchTimeMultiplier = 1.0f;
    [SerializeField] private float minSearchTime = 0.3f;
    [SerializeField] private float maxSearchTime = 5.0f;

    [Header("Flags")]
    [SerializeField] private bool consumeOnSearch = true;
    [SerializeField] private bool destroyWhenEmpty = false;
    [SerializeField] private bool isSearched = false;

    [Header("Run Reset")]
    [Tooltip("勾选：该容器是“场景预设容器”，ResetRun 时会恢复到初始状态。\n不勾选：该容器通常是运行时生成（例如尸体箱子），不参与恢复，只会被 RunManager Destroy（需 RegisterRuntimeObject）。")]
    [SerializeField] private bool participateInRunReset = true;

    // ===== 初始快照（只对 participateInRunReset=true 有意义）=====
    private List<ItemStack> _initialLootSnapshot;
    private bool _initialIsSearched;

    public bool IsSearched => isSearched && (loot == null || loot.Count == 0);
    public int SlotCount => loot != null ? loot.Count : 0;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void Awake()
    {
        if (participateInRunReset)
        {
            CaptureInitialState();
            RunManager.Instance?.RegisterResettable(this);
        }
    }

    private void OnDestroy()
    {
        // 防御性：场景卸载/销毁时把自己从列表移除，避免残留引用
        if (participateInRunReset)
            RunManager.Instance?.UnregisterResettable(this);
    }

    private void CaptureInitialState()
    {
        _initialLootSnapshot = new List<ItemStack>();

        if (loot != null)
        {
            foreach (var s in loot)
            {
                if (s == null || s.item == null || s.count <= 0)
                    _initialLootSnapshot.Add(null);
                else
                    _initialLootSnapshot.Add(new ItemStack(s.item, s.count));
            }
        }

        _initialIsSearched = isSearched;
    }

    // ================= IRunResettable =================
    public void ResetToDefault()
    {
        if (!participateInRunReset)
            return;

        if (loot == null)
            loot = new List<ItemStack>();
        else
            loot.Clear();

        if (_initialLootSnapshot != null)
        {
            foreach (var s in _initialLootSnapshot)
            {
                if (s == null)
                    loot.Add(null);
                else
                    loot.Add(new ItemStack(s.item, s.count));
            }
        }

        isSearched = _initialIsSearched;
    }

    // ============== 外部配置接口（给 EnemyLootDropper 用） ==============
    public void SetParticipateInRunReset(bool value)
    {
        participateInRunReset = value;
    }

    public void SetDestroyWhenEmpty(bool value)
    {
        destroyWhenEmpty = value;
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

        return s;
    }

    public void SetSlotStack(int index, ItemStack stack)
    {
        if (loot == null)
            loot = new List<ItemStack>();

        while (loot.Count <= index)
            loot.Add(null);

        if (stack == null || stack.item == null || stack.count <= 0)
        {
            loot[index] = null;
        }
        else
        {
            loot[index] = new ItemStack(stack.item, stack.count);
        }

        isSearched = false;
    }

    // ================= ILootSource：全部拿走 =================
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

    // ============== 辅助接口（敌人死亡时填战利品） ==============
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
