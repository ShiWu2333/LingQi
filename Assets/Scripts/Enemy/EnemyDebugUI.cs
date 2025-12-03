using UnityEngine;
using TMPro;

public class EnemyDebugUI : MonoBehaviour
{
    public TextMeshPro text;  // << 不再是 Canvas Text，而是普通 TMP
    public Vector3 offset = new Vector3(0, 2.2f, 0);

    private Camera cam;
    private EnemyResources resources;
    private EnemyPoiseController poise;
    private EnemyAIController ai;

    private void Start()
    {
        cam = Camera.main;

        resources = GetComponentInParent<EnemyResources>();
        poise = GetComponentInParent<EnemyPoiseController>();
        ai = GetComponentInParent<EnemyAIController>();

        if (text == null)
            text = GetComponent<TextMeshPro>();
    }

    private void Update()
    {
        if (cam == null || text == null) return;

        // 让文字跟着敌人头顶
        transform.position = resources.transform.position + offset;

        // 永远面向摄像机（billboard）
        transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);

        // 填充文本
        string hp = $"HP: {Mathf.RoundToInt(resources.CurrentHP)} / {Mathf.RoundToInt(resources.MaxHP)}";
        string poiseStr = $"Poise: {Mathf.RoundToInt(poise.CurrentPoise)} / {Mathf.RoundToInt(poise.MaxPoise)}";
        string state = $"State: {ai.CurrentDebugState}";
        string reaction = $"Reaction: {poise.LastReaction}";
        string lastImpact = $"Impact: {poise.LastImpactGrade}";

        text.text = $"{hp}\n{poiseStr}\n{state}\n{reaction}\n{lastImpact}";
    }
}
