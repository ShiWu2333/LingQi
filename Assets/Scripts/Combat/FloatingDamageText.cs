using TMPro;
using UnityEngine;

namespace LingQi.Combat
{
    [DisallowMultipleComponent]
    public class FloatingDamageText : MonoBehaviour
    {
        private Vector3 velocity;
        private float lifetime = 1f;
        private float timer;
        private TextMeshPro text;

        private void Awake()
        {
            text = GetComponent<TextMeshPro>();
        }

        private void OnEnable()
        {
            timer = lifetime;
        }

        private void Update()
        {
            transform.position += velocity * Time.deltaTime;
            transform.LookAt(Camera.main ? Camera.main.transform : transform.position + Vector3.forward);
            transform.Rotate(0f, 180f, 0f);

            timer -= Time.deltaTime;
            if (text != null)
            {
                float t = Mathf.Clamp01(timer / lifetime);
                Color color = text.color;
                color.a = t;
                text.color = color;
            }

            if (timer <= 0f)
            {
                Destroy(gameObject);
            }
        }

        public void SetMotion(Vector3 moveVelocity, float totalLifetime)
        {
            velocity = moveVelocity;
            lifetime = totalLifetime;
            timer = lifetime;
        }
    }
}
