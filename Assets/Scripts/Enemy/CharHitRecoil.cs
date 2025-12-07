using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class CharHitRecoil : MonoBehaviour
{
    [Header("Target (可选)")]
    [Tooltip("真正做抖动的模型节点，留空就用当前 transform。推荐挂在模型子物体上。")]
    [SerializeField] private Transform modelRoot;

    [Header("Recoil Angles (Degrees)")]
    public float maxBackwardAngle = 30f;
    public float maxSideAngle = 30f;

    [Header("Timings")]
    public float recoilInDuration = 0.08f;
    public float recoilOutDuration = 0.12f;
    public bool scaleByImpact = true;

    private Quaternion _baseLocalRot;
    private Coroutine _routine;

    private void Awake()
    {
        if (modelRoot == null)
            modelRoot = transform; // 推荐：直接把这个组件挂在模型节点上

        _baseLocalRot = modelRoot.localRotation;
    }

    public void PlayRecoil(Vector3 hitPoint, ImpactGrade impact)
    {
        if (!isActiveAndEnabled) return;
        if (!gameObject.activeInHierarchy) return;
        if (modelRoot == null) return;

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

        // ⭐ 每次受击时，以当前 localRotation 作为基准
        _baseLocalRot = modelRoot.localRotation;

        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(RecoilRoutine(hitPoint, strength));
    }

    private IEnumerator RecoilRoutine(Vector3 hitPoint, float strength)
    {
        // 命中方向（以模型位置为参考）
        Vector3 toHit = modelRoot.position - hitPoint;
        toHit.y = 0f;

        if (toHit.sqrMagnitude < 0.0001f)
            toHit = -modelRoot.forward; // 当成正面被打

        toHit.Normalize();

        // 前后：命中点在前面 → 向后仰
        float frontBack = Vector3.Dot(toHit, modelRoot.forward); // >0：前方

        float backSign = (frontBack >= 0f) ? 1f : -1f;
        float backAngle = maxBackwardAngle * strength * backSign;

        // 左右：命中点在右侧 → 向左倒一点（反方向）
        float side = Vector3.Dot(toHit, modelRoot.right); // >0：右侧

        float sideSign = Mathf.Sign(side);
        if (Mathf.Abs(sideSign) < 0.0001f)
            sideSign = 0f;

        float sideAngle = -maxSideAngle * strength * sideSign;

        Quaternion targetRot = Quaternion.Euler(backAngle, 0f, sideAngle) * _baseLocalRot;

        // 进入受击姿势
        float t = 0f;
        while (t < recoilInDuration)
        {
            float n = recoilInDuration > 0f ? t / recoilInDuration : 1f;
            n = 1f - (1f - n) * (1f - n); // EaseOut
            modelRoot.localRotation = Quaternion.Slerp(_baseLocalRot, targetRot, n);
            t += Time.deltaTime;
            yield return null;
        }

        modelRoot.localRotation = targetRot;

        // 回到站立
        t = 0f;
        while (t < recoilOutDuration)
        {
            float n = recoilOutDuration > 0f ? t / recoilOutDuration : 1f;
            n = n * n; // EaseIn
            modelRoot.localRotation = Quaternion.Slerp(targetRot, _baseLocalRot, n);
            t += Time.deltaTime;
            yield return null;
        }

        modelRoot.localRotation = _baseLocalRot;
        _routine = null;
    }
}
