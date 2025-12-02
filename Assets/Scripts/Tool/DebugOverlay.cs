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

        // 找 Text （本物体上的）
        text = GetComponentInChildren<TextMeshProUGUI>();
        if (text == null)
            Debug.LogError("DebugOverlay: No TextMeshProUGUI in children.");

        // 找刚体（玩家）
        var player = FindAnyObjectByType<PlayerResources>();
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

        if (playerRb != null)
            s += $"\nVelocity: {playerRb.velocity.magnitude:0.00}";

        text.text = s;
    }
}
