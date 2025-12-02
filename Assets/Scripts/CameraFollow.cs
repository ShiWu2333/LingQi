using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 6f, -8f);
    [SerializeField] private float smoothTime = 0.15f;

    private Vector3 velocity;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desired = target.position + target.rotation * offset;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
