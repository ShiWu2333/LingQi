using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LevelUpUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject rootPanel;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI spiritText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI staminaText;
    [SerializeField] private TextMeshProUGUI manaText;
    [SerializeField] private TextMeshProUGUI physicalText;
    [SerializeField] private TextMeshProUGUI magicText;

    [Header("Buttons")]
    [SerializeField] private Button hpButton;
    [SerializeField] private Button staminaButton;
    [SerializeField] private Button manaButton;
    [SerializeField] private Button physicalButton;
    [SerializeField] private Button magicButton;

    private PlayerResources _resources;
    private PlayerMovement _movement;
    private PlayerCombatController _combat;
    private PlayerDodgeController _dodge;

    private bool _isOpen;

    private void Awake()
    {
        if (rootPanel == null)
            rootPanel = gameObject;

        if (hpButton != null) hpButton.onClick.AddListener(UpgradeHP);
        if (staminaButton != null) staminaButton.onClick.AddListener(UpgradeStamina);
        if (manaButton != null) manaButton.onClick.AddListener(UpgradeMana);
        if (physicalButton != null) physicalButton.onClick.AddListener(UpgradePhysical);
        if (magicButton != null) magicButton.onClick.AddListener(UpgradeMagic);

        Close();
    }

    private void Update()
    {
        if (!_isOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    public void Open(PlayerResources res)
    {
        if (res == null) return;

        _resources = res;
        if (_movement == null) _movement = res.GetComponent<PlayerMovement>();
        if (_combat == null) _combat = res.GetComponent<PlayerCombatController>();
        if (_dodge == null) _dodge = res.GetComponent<PlayerDodgeController>();

        // 锁定玩家操作（简单粗暴版）
        if (_movement != null) _movement.isMovementLocked = true;
        if (_combat != null) _combat.enabled = false;
        if (_dodge != null) _dodge.enabled = false;

        rootPanel.SetActive(true);
        _isOpen = true;

        RefreshAll();
    }

    public void Close()
    {
        _isOpen = false;
        rootPanel.SetActive(false);

        if (_movement != null) _movement.isMovementLocked = false;
        if (_combat != null) _combat.enabled = true;
        if (_dodge != null) _dodge.enabled = true;
    }

    private void RefreshAll()
    {
        if (_resources == null) return;

        int spirit = _resources.CurrentSpirit;
        if (spiritText != null)
            spiritText.text = $"灵力: {spirit}";

        if (hpText != null)
        {
            int lv = _resources.HPLevel;
            int cost = _resources.GetLevelUpCost(LevelUpStat.MaxHP);
            hpText.text = $"HP Lv.{lv}  Max:{_resources.MaxHP:0}  (Cost:{cost})";
        }

        if (staminaText != null)
        {
            int lv = _resources.StaminaLevel;
            int cost = _resources.GetLevelUpCost(LevelUpStat.MaxStamina);
            staminaText.text = $"ST Lv.{lv}  Max:{_resources.MaxStamina:0}  (Cost:{cost})";
        }

        if (manaText != null)
        {
            int lv = _resources.ManaLevel;
            int cost = _resources.GetLevelUpCost(LevelUpStat.MaxMana);
            manaText.text = $"MP Lv.{lv}  Max:{_resources.MaxMana:0}  (Cost:{cost})";
        }

        if (physicalText != null)
        {
            int lv = _resources.PhysicalAtkLevel;
            int cost = _resources.GetLevelUpCost(LevelUpStat.PhysicalAttack);
            float pct = _resources.PhysicalBonus * 100f;
            physicalText.text = $"Physical Lv.{lv}  +{pct:0.#}%  (Cost:{cost})";
        }

        if (magicText != null)
        {
            int lv = _resources.MagicAtkLevel;
            int cost = _resources.GetLevelUpCost(LevelUpStat.MagicAttack);
            float pct = _resources.MagicBonus * 100f;
            magicText.text = $"Magical Lv.{lv}  +{pct:0.#}%  (Cost:{cost})";
        }

        // 按钮是否可点（灵力不足时禁用）
        int curSpirit = _resources.CurrentSpirit;

        if (hpButton != null)
            hpButton.interactable = curSpirit >= _resources.GetLevelUpCost(LevelUpStat.MaxHP);

        if (staminaButton != null)
            staminaButton.interactable = curSpirit >= _resources.GetLevelUpCost(LevelUpStat.MaxStamina);

        if (manaButton != null)
            manaButton.interactable = curSpirit >= _resources.GetLevelUpCost(LevelUpStat.MaxMana);

        if (physicalButton != null)
            physicalButton.interactable = curSpirit >= _resources.GetLevelUpCost(LevelUpStat.PhysicalAttack);

        if (magicButton != null)
            magicButton.interactable = curSpirit >= _resources.GetLevelUpCost(LevelUpStat.MagicAttack);
    }

    private void UpgradeHP()
    {
        if (_resources != null && _resources.TryLevelUp(LevelUpStat.MaxHP))
            RefreshAll();
    }

    private void UpgradeStamina()
    {
        if (_resources != null && _resources.TryLevelUp(LevelUpStat.MaxStamina))
            RefreshAll();
    }

    private void UpgradeMana()
    {
        if (_resources != null && _resources.TryLevelUp(LevelUpStat.MaxMana))
            RefreshAll();
    }

    private void UpgradePhysical()
    {
        if (_resources != null && _resources.TryLevelUp(LevelUpStat.PhysicalAttack))
            RefreshAll();
    }

    private void UpgradeMagic()
    {
        if (_resources != null && _resources.TryLevelUp(LevelUpStat.MagicAttack))
            RefreshAll();
    }
}
