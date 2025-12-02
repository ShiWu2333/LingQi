using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -8f);
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private bool enableDebugLogs = false;

    private Vector3 velocity;

    private void Start()
    {
        if (enableDebugLogs)
        {
            Debug.Log("Camera follow initialized.");
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        transform.LookAt(target.position);
    }
}
