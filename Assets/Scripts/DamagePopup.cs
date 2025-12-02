using System.Collections;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float lifetime = 1.25f;
    [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private TextMesh textMesh;
    private Color baseColor = Color.white;

    public static void Spawn(Vector3 worldPosition, float amount, Color tint)
    {
        GameObject obj = new GameObject("DamagePopup");
        obj.transform.position = worldPosition;
        var popup = obj.AddComponent<DamagePopup>();
        popup.Initialize(amount, tint);
    }

    public void Initialize(float amount, Color tint)
    {
        textMesh = gameObject.AddComponent<TextMesh>();
        textMesh.text = Mathf.RoundToInt(amount).ToString();
        textMesh.fontSize = 64;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.color = tint;
        baseColor = tint;

        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        float elapsed = 0f;
        Vector3 direction = new Vector3(0.35f, 1f, 0f);

        while (elapsed < lifetime)
        {
            float progress = elapsed / lifetime;
            transform.position += direction * floatSpeed * Time.deltaTime;
            Color c = baseColor;
            c.a = alphaCurve.Evaluate(progress);
            if (textMesh != null)
            {
                textMesh.color = c;
                transform.LookAt(Camera.main != null ? Camera.main.transform : transform.position + Vector3.forward);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}
