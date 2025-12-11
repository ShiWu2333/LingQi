using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panelRoot;          // 整个背包面板
    [SerializeField] private PlayerInventory inventory;     // 数据
    [SerializeField] private Transform slotsParent;         // GridLayout 容器
    [SerializeField] private GameObject slotPrefab;         // Prefab，上面挂着 InventorySlotUI

    private readonly List<InventorySlotUI> _slotUIs = new List<InventorySlotUI>();

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        BuildSlots();
        Refresh();
    }

    private void OnEnable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged += Refresh;
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= Refresh;
    }

    private void BuildSlots()
    {
        if (slotsParent == null || slotPrefab == null || inventory == null)
            return;

        foreach (Transform child in slotsParent)
            Destroy(child.gameObject);

        _slotUIs.Clear();

        int count = inventory.SlotCount;
        for (int i = 0; i < count; i++)
        {
            GameObject go = Instantiate(slotPrefab, slotsParent);
            var ui = go.GetComponent<InventorySlotUI>();
            if (ui == null)
            {
                Debug.LogError("[InventoryUI] slotPrefab 上缺少 InventorySlotUI 组件！");
                continue;
            }

            ui.InitForInventory(this, i);
            _slotUIs.Add(ui);
        }
    }

    public void Toggle()
    {
        if (panelRoot == null) return;

        bool newState = !panelRoot.activeSelf;
        panelRoot.SetActive(newState);

        if (newState)
        {
            BuildSlots();
            Refresh();
        }
    }

    public void Show()
    {
        if (panelRoot == null) return;

        panelRoot.SetActive(true);
        BuildSlots();
        Refresh();
    }

    public void Hide()
    {
        if (panelRoot == null) return;
        panelRoot.SetActive(false);
    }

    public void Refresh()
    {
        if (inventory == null || _slotUIs.Count == 0)
            return;

        int count = Mathf.Min(inventory.SlotCount, _slotUIs.Count);

        for (int i = 0; i < count; i++)
        {
            var stack = inventory.GetSlotStack(i);
            _slotUIs[i].SetItem(stack);
        }

        for (int i = count; i < _slotUIs.Count; i++)
            _slotUIs[i].SetEmpty();
    }

    // ===== 给 InventorySlotUI 调用的接口 =====

    public ItemStack GetStack(int index)
    {
        return inventory != null ? inventory.GetSlotStack(index) : null;
    }

    public void SetStack(int index, ItemStack stack)
    {
        inventory?.SetSlotStack(index, stack);
        Refresh();
    }
}
