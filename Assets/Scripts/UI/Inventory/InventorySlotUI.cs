using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InventorySlotUI : MonoBehaviour,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Image emptyMask;

    public enum OwnerType
    {
        Inventory,
        LootContainer
    }

    // 当前这个格子所属
    public OwnerType ownerType { get; private set; }
    private InventoryUI ownerInventory;
    private LootContainerUI ownerLoot;
    private int slotIndex;

    // 当前显示的数据（仅用来刷新 UI）
    private ItemStack _currentStack;

    // ===== 全局拖拽状态（数据）=====
    private static InventorySlotUI draggingFrom;
    private static ItemStack draggingStack;
    private static bool dropHandledThisDrag;

    // ===== 全局拖拽图标（视觉）=====
    private static Image draggingIcon;
    private static RectTransform draggingIconRect;

    private Canvas rootCanvas;

    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        SetEmpty();
    }

    // ========== 初始化 ==========

    public void InitForInventory(InventoryUI owner, int index)
    {
        ownerType = OwnerType.Inventory;
        ownerInventory = owner;
        ownerLoot = null;
        slotIndex = index;
        SetEmpty();
    }

    public void InitForLoot(LootContainerUI owner, int index)
    {
        ownerType = OwnerType.LootContainer;
        ownerLoot = owner;
        ownerInventory = null;
        slotIndex = index;
        SetEmpty();
    }

    // ========== UI 刷新 ==========

    public void SetEmpty()
    {
        _currentStack = null;

        if (iconImage != null)
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
        }

        if (countText != null)
            countText.text = "";

        if (emptyMask != null)
            emptyMask.enabled = true;
    }

    public void SetItem(ItemStack stack)
    {
        if (stack == null || stack.item == null || stack.count <= 0)
        {
            SetEmpty();
            return;
        }

        _currentStack = stack;

        if (iconImage != null)
        {
            iconImage.enabled = true;
            iconImage.sprite = stack.item.icon;
        }

        if (countText != null)
        {
            countText.text = stack.count > 1 ? stack.count.ToString() : "";
        }

        if (emptyMask != null)
            emptyMask.enabled = false;
    }

    // ========== 帮助函数：访问数据层 ==========

    private ItemStack GetStackFromOwner()
    {
        switch (ownerType)
        {
            case OwnerType.Inventory:
                return ownerInventory != null
                    ? ownerInventory.GetStack(slotIndex)
                    : null;

            case OwnerType.LootContainer:
                return ownerLoot != null
                    ? ownerLoot.GetStack(slotIndex)
                    : null;

            default:
                return null;
        }
    }

    private void SetStackToOwner(ItemStack stack)
    {
        switch (ownerType)
        {
            case OwnerType.Inventory:
                ownerInventory?.SetStack(slotIndex, stack);
                break;
            case OwnerType.LootContainer:
                ownerLoot?.SetStack(slotIndex, stack);
                break;
        }
    }

    // ========== 拖拽图标辅助 ==========

    private void ShowDraggingIcon(Sprite sprite)
    {
        if (sprite == null || rootCanvas == null)
            return;

        if (draggingIcon == null)
        {
            var go = new GameObject("DraggingIcon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            draggingIcon = go.GetComponent<Image>();
            draggingIconRect = go.GetComponent<RectTransform>();
            draggingIcon.raycastTarget = false;
        }

        draggingIcon.sprite = sprite;
        draggingIcon.enabled = true;
        draggingIcon.gameObject.SetActive(true);

        // 放到当前 Canvas 下面
        draggingIconRect.SetParent(rootCanvas.transform, false);

        // 和格子图标同尺寸
        if (iconImage != null)
            draggingIconRect.sizeDelta = iconImage.rectTransform.sizeDelta;
    }

    private void HideDraggingIcon()
    {
        if (draggingIcon != null)
            draggingIcon.gameObject.SetActive(false);
    }

    private void UpdateDraggingIconPosition(Vector2 screenPos)
    {
        if (draggingIconRect == null || rootCanvas == null || !draggingIcon.gameObject.activeSelf)
            return;

        // ScreenSpace-Overlay 直接赋值就行
        if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            draggingIconRect.position = screenPos;
        }
        else
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.transform as RectTransform,
                screenPos,
                rootCanvas.worldCamera,
                out var localPos);
            draggingIconRect.localPosition = localPos;
        }
    }

    // ========== 点击（以后可以做 Shift 快速拾取） ==========

    public void OnPointerClick(PointerEventData eventData)
    {
        // 暂时留空
    }

    // ========== 拖拽 ==========

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        var stack = GetStackFromOwner();
        if (stack == null || stack.item == null || stack.count <= 0)
            return;

        draggingFrom = this;
        // 拿一份数据副本出来
        draggingStack = new ItemStack(stack.item, stack.count);
        dropHandledThisDrag = false;

        // 清空源格子（数据层）
        SetStackToOwner(null);

        // 显示拖拽图标
        ShowDraggingIcon(stack.item.icon);
        UpdateDraggingIconPosition(eventData.position);

        // 刷新 UI
        ownerInventory?.Refresh();
        ownerLoot?.Refresh();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggingFrom != this || draggingStack == null)
            return;

        UpdateDraggingIconPosition(eventData.position);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (draggingFrom == null || draggingStack == null)
            return;

        dropHandledThisDrag = true;

        // 目标格子的当前物品
        ItemStack targetStack = GetStackFromOwner();

        if (targetStack == null || targetStack.item == null || targetStack.count <= 0)
        {
            // 目标空格：直接放下
            SetStackToOwner(draggingStack);
            draggingStack = null;
        }
        else if (targetStack.item == draggingStack.item)
        {
            // 同种物品：尝试合并
            int maxStack = Mathf.Max(1, targetStack.item.maxStack);
            int canMove = Mathf.Max(0, maxStack - targetStack.count);

            if (canMove > 0)
            {
                int move = Mathf.Min(canMove, draggingStack.count);
                targetStack.count += move;
                draggingStack.count -= move;

                // 写回目标格子
                SetStackToOwner(targetStack);

                if (draggingStack.count <= 0)
                    draggingStack = null;
            }
            else
            {
                // 已经满栈，直接交换
                SwapWithTarget(targetStack);
            }
        }
        else
        {
            // 不同物品：交换
            SwapWithTarget(targetStack);
        }

        // 两边 UI 刷新
        ownerInventory?.Refresh();
        ownerLoot?.Refresh();
        draggingFrom.ownerInventory?.Refresh();
        draggingFrom.ownerLoot?.Refresh();
    }

    private void SwapWithTarget(ItemStack targetStack)
    {
        // 把目标格子的东西搬回源格子
        if (draggingFrom != null)
        {
            draggingFrom.SetStackToOwner(targetStack);
        }

        // 当前格子放下拖拽的那一堆
        SetStackToOwner(draggingStack);
        draggingStack = null;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 拖拽结束，隐藏图标
        HideDraggingIcon();

        // 如果拖拽结束没有任何格子接收，就把东西放回原位
        if (!dropHandledThisDrag && draggingFrom == this && draggingStack != null)
        {
            SetStackToOwner(draggingStack);
        }

        // 刷新 UI
        ownerInventory?.Refresh();
        ownerLoot?.Refresh();

        draggingFrom = null;
        draggingStack = null;
        dropHandledThisDrag = false;
    }
}
