using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FlashOnHit : MonoBehaviour
{
    [Header("Flash")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.08f;

    [Header("Recoil (Optional)")]
    [Tooltip("可选：如果角色有 CharHitRecoil，将自动触发受击后仰效果")]
    [SerializeField] private CharHitRecoil recoil;

    private Renderer[] renderers;
    private Color[] originalColors;

    private void Awake()
    {
        // 找 Recoil（可选）
        if (recoil == null)
            recoil = GetComponentInChildren<CharHitRecoil>();

        // 收集可闪光材质
        var all = GetComponentsInChildren<Renderer>();
        var list = new List<Renderer>();
        var colors = new List<Color>();

        foreach (var r in all)
        {
            var mat = r.material;
            if (mat != null && mat.HasProperty("_Color"))
            {
                list.Add(r);
                colors.Add(mat.color);
            }
        }

        renderers = list.ToArray();
        originalColors = colors.ToArray();
    }

    /// <summary>
    /// 外部调用受击闪光 + 后仰
    /// </summary>
    public void Trigger(Vector3 hitPoint, ImpactGrade impact)
    {
        if (!isActiveAndEnabled) return;

        // ① 视觉闪光
        if (renderers != null && renderers.Length > 0)
        {
            StopAllCoroutines();
            StartCoroutine(FlashRoutine());
        }

        // ② 受击后仰（若角色有此组件）
        if (recoil != null)
            recoil.PlayRecoil(hitPoint, impact);
    }

    private IEnumerator FlashRoutine()
    {
        // 变亮
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].material.color = flashColor;
        }

        yield return new WaitForSeconds(flashDuration);

        // 还原
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].material.color = originalColors[i];
        }
    }
}
