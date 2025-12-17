using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class LootContainerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform slotsParent;
    [SerializeField] private GameObject slotPrefab;

    [Header("Config")]
    [SerializeField] private int fixedSlots = 4;

    private readonly List<InventorySlotUI> _slotUIs = new List<InventorySlotUI>();

    private ILootSource _currentSource;

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;
    public ILootSource CurrentSource => _currentSource;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        BuildSlots();
    }

    private void BuildSlots()
    {
        if (slotsParent == null || slotPrefab == null)
            return;

        foreach (Transform child in slotsParent)
            Destroy(child.gameObject);

        _slotUIs.Clear();

        int n = Mathf.Max(1, fixedSlots);
        for (int i = 0; i < n; i++)
        {
            GameObject go = Instantiate(slotPrefab, slotsParent);
            var ui = go.GetComponent<InventorySlotUI>();
            if (ui == null)
            {
                Debug.LogError("[LootContainerUI] slotPrefab 上缺少 InventorySlotUI 组件！");
                continue;
            }

            ui.InitForLoot(this, i);
            _slotUIs.Add(ui);
        }
    }

    public void Show(ILootSource source)
    {
        _currentSource = source;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (_slotUIs.Count == 0)
            BuildSlots();

        Refresh();
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        _currentSource = null;
    }

    public void Refresh()
    {
        if (_slotUIs.Count == 0)
            return;

        for (int i = 0; i < _slotUIs.Count; i++)
        {
            ItemStack stack = (_currentSource != null && i < _currentSource.SlotCount)
                ? _currentSource.GetSlotStack(i)
                : null;

            _slotUIs[i].SetItem(stack);
        }
    }

    // ===== 给 InventorySlotUI 调用的接口 =====

    public ItemStack GetStack(int index)
    {
        if (_currentSource == null || index < 0 || index >= _currentSource.SlotCount)
            return null;

        return _currentSource.GetSlotStack(index);
    }

    public void SetStack(int index, ItemStack stack)
    {
        if (_currentSource == null)
            return;

        _currentSource.SetSlotStack(index, stack);
        Refresh();
    }
}
