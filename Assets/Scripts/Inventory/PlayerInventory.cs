using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInventory : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private int maxSlots = 16;

    [Header("Runtime (ReadOnly)")]
    [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

    public event Action OnInventoryChanged;

    [System.Serializable]
    public class InventorySlot
    {
        public ItemStack stack;

        public bool IsEmpty => stack == null || stack.item == null || stack.count <= 0;

        public ItemConfig Item => stack != null ? stack.item : null;
        public int Count => stack != null ? stack.count : 0;


        public void Clear()
        {
            stack = null;
        }
    }

    public IReadOnlyList<InventorySlot> Slots => slots;
    public int MaxSlots => maxSlots;

    private void Awake()
    {
        EnsureSlotCount();
    }

    private void OnValidate()
    {
        if (maxSlots < 1) maxSlots = 1;
        EnsureSlotCount();
    }

    private void EnsureSlotCount()
    {
        if (slots == null)
            slots = new List<InventorySlot>();

        // 扩容
        while (slots.Count < maxSlots)
        {
            slots.Add(new InventorySlot());
        }

        // 缩容（多余的直接裁掉）
        if (slots.Count > maxSlots)
        {
            slots.RemoveRange(maxSlots, slots.Count - maxSlots);
        }
    }

    // =========================================================
    //  对外接口
    // =========================================================

    /// <summary>
    /// 往背包里加入物品。返回剩余未放入数量（0 = 全部放进去了）。
    /// </summary>
    public int TryAddItem(ItemConfig item, int amount)
    {
        if (item == null || amount <= 0)
            return amount;

        int remaining = amount;

        // 1) 先填充已有的同种物品 Stack
        remaining = FillExistingStacks(item, remaining);

        // 2) 再找空格新开栈
        remaining = FillEmptySlots(item, remaining);

        if (remaining != amount)
            RaiseChanged();

        return remaining;
    }

    public bool TryRemoveItem(ItemConfig item, int amount)
    {
        if (item == null || amount <= 0)
            return true;

        int total = GetTotalCount(item);
        if (total < amount)
            return false;

        int remaining = amount;

        // 从前往后扣
        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            var slot = slots[i];
            if (slot.IsEmpty || slot.Item != item)
                continue;

            int take = Mathf.Min(slot.Count, remaining);
            slot.stack.count -= take;
            remaining -= take;

            if (slot.stack.count <= 0)
                slot.Clear();
        }

        RaiseChanged();
        return true;
    }

    public int GetTotalCount(ItemConfig item)
    {
        if (item == null) return 0;

        int total = 0;
        foreach (var slot in slots)
        {
            if (slot.IsEmpty) continue;
            if (slot.Item == item)
                total += slot.Count;
        }
        return total;
    }

    public bool HasItem(ItemConfig item, int requiredCount = 1)
    {
        return GetTotalCount(item) >= requiredCount;
    }

    // =========================================================
    //  内部：填充逻辑
    // =========================================================

    private int FillExistingStacks(ItemConfig item, int amount)
    {
        if (amount <= 0) return 0;
        int remaining = amount;

        int maxStack = Mathf.Max(1, item.maxStack);

        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            var slot = slots[i];
            if (slot.IsEmpty) continue;
            if (slot.Item != item) continue;

            int current = slot.Count;
            if (current >= maxStack) continue;

            int space = maxStack - current;
            int add = Mathf.Min(space, remaining);

            slot.stack.count += add;
            remaining -= add;
        }

        return remaining;
    }

    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= slots.Count) return null;
        return slots[index];
    }

    public void SwapSlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= slots.Count) return;
        if (indexB < 0 || indexB >= slots.Count) return;
        if (indexA == indexB) return;

        var tmp = slots[indexA].stack;
        slots[indexA].stack = slots[indexB].stack;
        slots[indexB].stack = tmp;

        RaiseChanged();
    }

    private int FillEmptySlots(ItemConfig item, int amount)
    {
        if (amount <= 0) return 0;

        int remaining = amount;
        int maxStack = Mathf.Max(1, item.maxStack);

        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            var slot = slots[i];
            if (!slot.IsEmpty) continue;

            int add = Mathf.Min(maxStack, remaining);
            slot.stack = new ItemStack(item, add);
            remaining -= add;
        }

        return remaining;
    }

    private void RaiseChanged()
    {
        OnInventoryChanged?.Invoke();
    }

    public int SlotCount => slots != null ? slots.Count : 0;

    public ItemStack GetSlotStack(int index)
    {
        if (slots == null || index < 0 || index >= slots.Count)
            return null;

        var slot = slots[index];
        return slot != null ? slot.stack : null;
    }

    public void SetSlotStack(int index, ItemStack stack)
    {
        if (slots == null || index < 0 || index >= slots.Count)
            return;

        var slot = slots[index];
        if (slot == null)
        {
            slot = new InventorySlot();
            slots[index] = slot;
        }

        slot.stack = (stack == null || stack.item == null || stack.count <= 0)
            ? null
            : new ItemStack(stack.item, stack.count);

        RaiseChanged();
    }
}
