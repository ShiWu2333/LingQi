using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInteractController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactLayers = ~0;

    [Header("UI")]
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private LootContainerUI lootContainerUI;

    [Header("Input")]
    [SerializeField] private KeyCode inventoryKey = KeyCode.B;
    [SerializeField] private KeyCode interactKey = KeyCode.F;

    [Header("Search")]
    [SerializeField] private bool useHoldToSearch = true;
    [SerializeField] private float minSearchHold = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool debugDrawRay = false;
    [SerializeField] private string currentFocusName;

    private ILootSource _currentFocus;
    private Coroutine _searchRoutine;
    private bool _isSearching;
    private float _searchProgress;

    public float SearchProgress => _searchProgress;
    public bool IsSearching => _isSearching;
    public ILootSource CurrentFocus => _currentFocus;

    private void Update()
    {
        HandleInventoryToggle();
        HandleEscapeClose();
        UpdateFocus();
        HandleInteract();
        HandleClosePanels();   // 新增
    }

    // ========= 背包开关 =========

    private void HandleClosePanels()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool anyClosed = false;

            if (lootContainerUI != null && lootContainerUI.IsVisible)
            {
                lootContainerUI.Hide();
                anyClosed = true;
            }

            // 如果打开箱子时你总是同时打开背包，这里也顺手关掉
            if (inventoryUI != null && inventoryUI.IsVisible && anyClosed)
            {
                inventoryUI.Hide();
            }
        }
    }

    private void HandleInventoryToggle()
    {
        if (Input.GetKeyDown(inventoryKey))
        {
            if (inventoryUI != null)
                inventoryUI.Toggle();
        }
    }

    // ========= Esc 关闭 LootContainer + 背包 =========

    private void HandleEscapeClose()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        bool anyClosed = false;

        if (lootContainerUI != null && lootContainerUI.IsVisible)
        {
            lootContainerUI.Hide();
            anyClosed = true;
        }

        if (inventoryUI != null && inventoryUI.IsVisible && anyClosed)
        {
            // 如果本来就是为搜箱子打开的背包，一起关掉
            inventoryUI.Hide();
        }
    }

    // ========= 交互目标检测 =========

    private void UpdateFocus()
    {
        _currentFocus = null;
        currentFocusName = "";

        Vector3 origin = transform.position + Vector3.up * 1.0f;
        Vector3 dir = transform.forward;
        Ray ray = new Ray(origin, dir);

        if (Physics.Raycast(ray, out var hit, interactDistance, interactLayers,
                QueryTriggerInteraction.Collide))
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

    // ========= F 键：搜索 / 打开容器 =========

    private void HandleInteract()
    {
        if (_currentFocus == null)
            return;

        if (_isSearching)
            return;

        if (useHoldToSearch)
        {
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
            if (Input.GetKeyDown(interactKey))
            {
                if (_searchRoutine == null)
                    _searchRoutine = StartCoroutine(CoSearchAndOpen(_currentFocus, true));
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

        if (lootContainerUI != null)
            lootContainerUI.Show(source);
    }
}
