using UnityEngine;

[DisallowMultipleComponent]
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;   // 可选手动指定，没指定就自动找 Player

    [Header("Follow Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -8f);
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private bool enableDebugLogs = false;

    private Vector3 _velocity;

    private void Awake()
    {
        // 自动寻找目标：优先找 PlayerResources，再退回 tag
        if (target == null)
        {
            var player = FindFirstObjectByType<PlayerResources>();
            if (player != null)
            {
                target = player.transform;
                if (enableDebugLogs)
                    Debug.Log("[CameraFollow] Auto-bound target to PlayerResources.", this);
            }
            else
            {
                var tagged = GameObject.FindWithTag("Player");
                if (tagged != null)
                {
                    target = tagged.transform;
                    if (enableDebugLogs)
                        Debug.Log("[CameraFollow] Auto-bound target via 'Player' tag.", this);
                }
            }
        }

        if (target == null && enableDebugLogs)
        {
            Debug.LogWarning("[CameraFollow] No target found. Camera will not move.", this);
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref _velocity,
            smoothTime);

        transform.LookAt(target.position);
    }
}
