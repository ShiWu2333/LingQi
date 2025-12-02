using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshPro text;
    [SerializeField] private float lifeTime = 0.6f;
    [SerializeField] private float moveUpSpeed = 1.5f;
    [SerializeField] private float fadeSpeed = 3f;

    private float elapsed;
    private Color startColor;

    public void Init(float amount)
    {
        if (text == null)
            text = GetComponentInChildren<TextMeshPro>();

        if (text == null)
        {
            Debug.LogError("[DamagePopup] No TextMeshPro assigned or found in children.", this);
            return;
        }

        text.text = Mathf.RoundToInt(amount).ToString();
        startColor = text.color;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        // 往上移动
        transform.position += Vector3.up * moveUpSpeed * Time.deltaTime;

        // 后半段开始淡出
        if (elapsed > lifeTime * 0.5f)
        {
            float t = (elapsed - lifeTime * 0.5f) / (lifeTime * 0.5f);
            Color c = startColor;
            c.a = Mathf.Lerp(startColor.a, 0f, t);
            text.color = c;
        }

        if (elapsed >= lifeTime)
        {
            Destroy(gameObject);
        }
    }
}
