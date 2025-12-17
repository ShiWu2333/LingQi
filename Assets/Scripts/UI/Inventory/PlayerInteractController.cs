using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInteractController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactLayers = ~0;  // 只检测 LootContainer 的层

    [Header("UI")]
    [SerializeField] private InventoryUI inventoryUI;         // 玩家背包 UI
    [SerializeField] private InventoryUI campStorageUI;       // 营地仓库 UI（也是一个 InventoryUI）
    [SerializeField] private LootContainerUI lootContainerUI; // 物资箱 / 尸体 UI

    [Header("Input")]
    [SerializeField] private KeyCode inventoryKey = KeyCode.B;
    [SerializeField] private KeyCode interactKey = KeyCode.F;

    [Header("Search")]
    [SerializeField] private bool useHoldToSearch = true;   // true = 按住 F 完成搜索
    [SerializeField] private float minSearchHold = 0.1f;    // 防止误触

    [Header("Debug")]
    [SerializeField] private bool debugDrawRay = false;
    [SerializeField] private string currentFocusName;

    private ILootSource _currentFocus;
    private Coroutine _searchRoutine;
    private bool _isSearching;
    private float _searchProgress; // 0~1，之后可以暴露给进度条 UI

    // 营地状态
    [SerializeField] private bool _isInCamp = false;

    public float SearchProgress => _searchProgress;
    public bool IsSearching => _isSearching;
    public ILootSource CurrentFocus => _currentFocus;

    /// <summary>给营地区域触发器调用，设置玩家是否在营地。</summary>
    public void SetInCamp(bool value)
    {
        _isInCamp = value;
    }

    private void Update()
    {
        HandleInventoryToggle();
        UpdateFocus();
        HandleInteract();
        HandleCloseLootContainer();
    }

    // ================== 背包 / 仓库 开关 ==================

    private void HandleInventoryToggle()
    {
        if (!Input.GetKeyDown(inventoryKey))
            return;

        // 当前想要的最终状态：反转玩家背包当前状态
        bool newVisible = !(inventoryUI != null && inventoryUI.IsVisible);

        // 玩家背包
        if (inventoryUI != null)
        {
            if (newVisible) inventoryUI.Show();
            else inventoryUI.Hide();
        }

        // 在营地才联动仓库
        if (campStorageUI != null && _isInCamp)
        {
            if (newVisible) campStorageUI.Show();
            else campStorageUI.Hide();
        }
        else
        {
            // 不在营地，确保仓库是关着的
            if (campStorageUI != null && campStorageUI.IsVisible)
                campStorageUI.Hide();
        }
    }

    // ================== 交互目标检测 ==================

    private void UpdateFocus()
    {
        _currentFocus = null;
        currentFocusName = "";

        Vector3 origin = transform.position + Vector3.up * 1.0f;
        Vector3 dir = transform.forward;

        Ray ray = new Ray(origin, dir);

        if (Physics.Raycast(ray, out var hit, interactDistance, interactLayers, QueryTriggerInteraction.Collide))
        {
            var loot = hit.collider.GetComponentInParent<ILootSource>();
            if (loot != null)
            {
                _currentFocus = loot;
                currentFocusName = hit.collider.name;
            }
        }

        if (debugDrawRay)
        {
            Debug.DrawRay(origin, dir * interactDistance,
                _currentFocus != null ? Color.green : Color.red);
        }
    }

    // ================== F 键：搜索 / 打开容器 ==================

    private void HandleInteract()
    {
        if (_currentFocus == null)
            return;

        if (_isSearching)
            return;

        if (useHoldToSearch)
        {
            // 长按模式：按下开始，松开中断
            if (Input.GetKeyDown(interactKey))
            {
                _searchRoutine = StartCoroutine(CoSearchAndOpen(_currentFocus));
            }

            if (Input.GetKeyUp(interactKey))
            {
                if (_isSearching && _searchRoutine != null)
                {
                    StopCoroutine(_searchRoutine);
                    _searchRoutine = null;
                    _isSearching = false;
                    _searchProgress = 0f;
                }
            }
        }
        else
        {
            // 单击模式：直接完成搜索并打开
            if (Input.GetKeyDown(interactKey))
            {
                if (_searchRoutine == null)
                    _searchRoutine = StartCoroutine(CoSearchAndOpen(_currentFocus, ignoreHold: true));
            }
        }
    }

    private IEnumerator CoSearchAndOpen(ILootSource source, bool ignoreHold = false)
    {
        if (source == null)
            yield break;

        _isSearching = true;
        _searchProgress = 0f;

        float duration = Mathf.Max(0.01f, source.GetSearchTime());
        float t = 0f;

        while (t < duration)
        {
            float dt = Time.deltaTime;

            // 若是长按模式且松开了键，则取消
            if (!ignoreHold && !Input.GetKey(interactKey) && t > minSearchHold)
            {
                _isSearching = false;
                _searchProgress = 0f;
                _searchRoutine = null;
                yield break;
            }

            t += dt;
            _searchProgress = Mathf.Clamp01(t / duration);

            yield return null;
        }

        _isSearching = false;
        _searchProgress = 1f;
        _searchRoutine = null;

        // 搜索完成：打开背包 + 容器 UI
        if (inventoryUI != null && !inventoryUI.IsVisible)
            inventoryUI.Show();

        if (campStorageUI != null && _isInCamp && !campStorageUI.IsVisible)
            campStorageUI.Show();

        if (lootContainerUI != null)
            lootContainerUI.Show(source);
    }

    // ================== ESC / F 关闭 LootContainer ==================

    private void HandleCloseLootContainer()
    {
        if (lootContainerUI == null || !lootContainerUI.IsVisible)
            return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(interactKey))
        {
            lootContainerUI.Hide();

            // 这里根据你喜好：搜刮结束时顺便关掉背包
            if (inventoryUI != null && inventoryUI.IsVisible)
                inventoryUI.Hide();

            if (campStorageUI != null && campStorageUI.IsVisible && _isInCamp)
                campStorageUI.Hide();
        }
    }
}
