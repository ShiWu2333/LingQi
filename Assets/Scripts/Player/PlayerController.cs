using UnityEngine;

namespace LingQi.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerResources))]
    [DisallowMultipleComponent]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float rotationSpeed = 120f;
        [SerializeField] private float gravity = -20f;

        [Header("Dodge")] 
        [SerializeField] private float dodgeDistance = 6f;
        [SerializeField] private float dodgeDuration = 0.25f;
        [SerializeField] private float dodgeStaminaCost = 25f;

        private CharacterController characterController;
        private PlayerResources resources;
        private Vector3 verticalVelocity;
        private bool isDodging;
        private Vector3 dodgeVelocity;
        private float dodgeTimer;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            resources = GetComponent<PlayerResources>();
            EnsureCharacterControllerDefaults();
        }

        private void Update()
        {
            HandleRotation();
            HandleMovement();
            ApplyGravity();
        }

        private void HandleMovement()
        {
            if (isDodging)
            {
                dodgeTimer -= Time.deltaTime;
                characterController.Move((dodgeVelocity + verticalVelocity) * Time.deltaTime);

                if (dodgeTimer <= 0f)
                {
                    isDodging = false;
                }

                return;
            }

            Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            input = Vector3.ClampMagnitude(input, 1f);
            Vector3 worldMove = transform.TransformDirection(input) * moveSpeed;
            characterController.Move((worldMove + verticalVelocity) * Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.Space))
            {
                TryDodge(worldMove);
            }
        }

        private void HandleRotation()
        {
            float yaw = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            transform.Rotate(0f, yaw, 0f);
        }

        private void ApplyGravity()
        {
            if (characterController.isGrounded && verticalVelocity.y < 0f)
            {
                verticalVelocity.y = -2f;
            }
            else
            {
                verticalVelocity.y += gravity * Time.deltaTime;
            }
        }

        private void TryDodge(Vector3 currentVelocity)
        {
            if (isDodging)
            {
                return;
            }

            Vector3 moveDirection = currentVelocity.normalized;
            if (moveDirection.sqrMagnitude <= 0.01f)
            {
                moveDirection = transform.forward;
            }

            if (!resources.TrySpendStamina(dodgeStaminaCost))
            {
                return;
            }

            isDodging = true;
            dodgeTimer = dodgeDuration;
            dodgeVelocity = moveDirection * (dodgeDistance / Mathf.Max(dodgeDuration, 0.01f));
        }

        private void EnsureCharacterControllerDefaults()
        {
            if (characterController == null)
            {
                characterController = gameObject.AddComponent<CharacterController>();
            }

            characterController.height = 2f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 1f, 0f);
        }
    }
}
