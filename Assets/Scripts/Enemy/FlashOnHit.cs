using System.Collections;
using System.Collections.Generic;
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
        var all = GetComponentsInChildren<Renderer>();
        var list = new List<Renderer>();
        var colors = new List<Color>();

        foreach (var r in all)
        {
            var mat = r.material;
            // 只保留有 _Color 属性的材质，跳过 TMP 之类的
            if (mat != null && mat.HasProperty("_Color"))
            {
                list.Add(r);
                colors.Add(mat.color);
            }
        }

        renderers = list.ToArray();
        originalColors = colors.ToArray();
    }

    public void Trigger()
    {
        // 敌人已经被禁用时不要再开协程（下面解决第二个报错）
        if (!isActiveAndEnabled) return;
        if (renderers == null || renderers.Length == 0) return;

        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // 变色
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].material.color = flashColor;
            }
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
