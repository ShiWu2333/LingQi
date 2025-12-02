using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class FlashOnHit : MonoBehaviour
{
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.08f;

    private Renderer[] renderers;
    private Color[] originalColors;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            // 注意：material 会实例化一份，对原型阶段来说可以接受
            originalColors[i] = renderers[i].material.color;
        }
    }

    public void Trigger()
    {
        if (renderers == null || renderers.Length == 0) return;
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // 变色
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = flashColor;
        }

        yield return new WaitForSeconds(flashDuration);

        // 还原
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].material.color = originalColors[i];
            }
        }
    }
}
