using UnityEngine;

public class PlayerResourceDebugUI : MonoBehaviour
{
    [SerializeField] private PlayerResources playerResources;
    [SerializeField] private Vector2 screenOffset = new Vector2(10f, 10f);
    [SerializeField] private Vector2 boxSize = new Vector2(240f, 80f);

    private GUIStyle boxStyle;

    private void Awake()
    {
        if (playerResources == null)
        {
            playerResources = GetComponent<PlayerResources>();
        }

        boxStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 14,
            alignment = TextAnchor.UpperLeft,
            fontStyle = FontStyle.Bold
        };
    }

    private void OnGUI()
    {
        if (playerResources == null)
        {
            return;
        }

        string content = $"HP: {playerResources.CurrentHealth:F0} / {playerResources.MaxHealth:F0}\n" +
                         $"Stamina: {playerResources.CurrentStamina:F0} / {playerResources.MaxStamina:F0}\n" +
                         $"Mana: {playerResources.CurrentMana:F0} / {playerResources.MaxMana:F0}";

        Rect rect = new Rect(screenOffset.x, screenOffset.y, boxSize.x, boxSize.y);
        GUI.Box(rect, content, boxStyle);
    }
}
