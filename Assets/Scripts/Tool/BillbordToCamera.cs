using UnityEngine;

[DisallowMultipleComponent]
public class BillboardToCamera : MonoBehaviour
{
    [Tooltip("只绕 Y 轴转（适合地上图标 / 竖立牌子）")]
    [SerializeField] private bool onlyYaw = true;

    private Camera _cam;

    private void LateUpdate()
    {
        if (_cam == null)
            _cam = Camera.main;
        if (_cam == null) return;

        if (onlyYaw)
        {
            // 只在水平面上对齐
            Vector3 dir = _cam.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
        else
        {
            // 完全面向摄像机
            transform.rotation = Quaternion.LookRotation(
                _cam.transform.position - transform.position,
                Vector3.up);
        }
    }
}
