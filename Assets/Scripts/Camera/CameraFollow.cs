using UnityEngine;

namespace LingQi.CameraSystem
{
    [DisallowMultipleComponent]
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 3f, -6f);
        public float followSmoothing = 8f;
        public bool lookAtTarget = true;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSmoothing * Time.deltaTime);

            if (lookAtTarget)
            {
                transform.LookAt(target);
            }
        }
    }
}
