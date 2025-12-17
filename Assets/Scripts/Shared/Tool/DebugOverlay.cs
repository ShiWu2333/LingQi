using UnityEngine;
using TMPro;

public class DebugOverlay : MonoBehaviour
{
    private PlayerResources resources;
    private Rigidbody playerRb;
    private TextMeshProUGUI text;

    private void Awake()
    {
        // 自动查找 PlayerResources
        resources = FindAnyObjectByType<PlayerResources>();
        if (resources == null)
            Debug.LogError("DebugOverlay: No PlayerResources found in scene.");

        // 找 UI 文本
        text = GetComponentInChildren<TextMeshProUGUI>();
        if (text == null)
            Debug.LogError("DebugOverlay: No TextMeshProUGUI in children.");

        // 找玩家 Rigidbody（有些项目玩家没有 Rigidbody，这里容错）
        var player = resources != null ? resources.gameObject : null;
        if (player != null)
            playerRb = player.GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (resources == null || text == null)
            return;

        string s = "";

        s += $"HP: {resources.CurrentHP:0}/{resources.MaxHP}\n";
        s += $"ST: {resources.CurrentStamina:0}/{resources.MaxStamina}\n";
        s += $"MP: {resources.CurrentMana:0}/{resources.MaxMana}\n";
        s += $"SPIRIT: {resources.CurrentSpirit}\n";  // ⭐ 新增灵力
        s += $"PAtk: {resources.PhysicalBonus * 100f:0.#}%\n";
        s += $"MAtk: {resources.MagicBonus * 100f:0.#}%\n";

        if (playerRb != null)
            s += $"\nVelocity: {playerRb.velocity.magnitude:0.00}";

        text.text = s;
    }
}
