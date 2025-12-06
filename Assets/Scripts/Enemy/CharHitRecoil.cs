using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class CharHitRecoil : MonoBehaviour
{
    [Header("Recoil Angles (Degrees)")]
    [Tooltip("正面被打时，身体向后仰的最大角度")]
    public float maxBackwardAngle = 30f;

    [Tooltip("侧面被打时，身体向一侧扭的最大角度")]
    public float maxSideAngle = 30f;

    [Header("Timings")]
    [Tooltip("从正常姿势过渡到受击姿势的时间")]
    public float recoilInDuration = 0.08f;

    [Tooltip("从受击姿势恢复到正常姿势的时间")]
    public float recoilOutDuration = 0.12f;

    [Tooltip("是否按 ImpactGrade 缩放强度")]
    public bool scaleByImpact = true;

    private Quaternion _baseLocalRot;
    private Coroutine _routine;

    private void Awake()
    {
        _baseLocalRot = transform.localRotation;
    }

    public void PlayRecoil(Vector3 hitPoint, ImpactGrade impact)
    {
        if (!isActiveAndEnabled) return;
        if (!gameObject.activeInHierarchy) return;

        float strength = 1f;
        if (scaleByImpact)
        {
            switch (impact)
            {
                case ImpactGrade.Small: strength = 0.4f; break;
                case ImpactGrade.Medium: strength = 0.7f; break;
                case ImpactGrade.Large: strength = 1.0f; break;
                default: strength = 0.8f; break;
            }
        }

        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(RecoilRoutine(hitPoint, strength));
    }

    private IEnumerator RecoilRoutine(Vector3 hitPoint, float strength)
    {
        // 命中点转到角色本地空间
        Vector3 localHit = transform.InverseTransformPoint(hitPoint);
        localHit.y = 0f;

        if (localHit.sqrMagnitude < 0.0001f)
            localHit = Vector3.forward;      // 极端情况兜底：当成正面

        // localHit.z > 0 说明命中点在角色前方 → 向后仰
        // localHit.z < 0 命中点在角色背后 → 向前倾一点
        float backSign = (localHit.z >= 0f) ? 1f : -1f;

        // localHit.x > 0 命中点在角色右侧； < 0 在左侧
        // 我们希望“被打的那一侧抬起来、身体往对侧倒一点”
        float sideSign = Mathf.Sign(localHit.x);
        if (Mathf.Abs(sideSign) < 0.0001f)
            sideSign = 0f;

        float backAngle = maxBackwardAngle * strength * backSign;
        // 这里取负号：右侧被打 → 向左倒，左侧被打 → 向右倒
        float sideAngle = -maxSideAngle * strength * sideSign;

        Quaternion targetRot =
            Quaternion.Euler(backAngle, 0f, sideAngle) * _baseLocalRot;

        // 进入受击姿势
        float t = 0f;
        while (t < recoilInDuration)
        {
            float n = recoilInDuration > 0f ? t / recoilInDuration : 1f;
            n = 1f - (1f - n) * (1f - n); // EaseOut
            transform.localRotation = Quaternion.Slerp(_baseLocalRot, targetRot, n);
            t += Time.deltaTime;
            yield return null;
        }

        transform.localRotation = targetRot;

        // 回到站立
        t = 0f;
        while (t < recoilOutDuration)
        {
            float n = recoilOutDuration > 0f ? t / recoilOutDuration : 1f;
            n = n * n; // EaseIn
            transform.localRotation = Quaternion.Slerp(targetRot, _baseLocalRot, n);
            t += Time.deltaTime;
            yield return null;
        }

        transform.localRotation = _baseLocalRot;
        _routine = null;
    }
}
