using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private bool useGravity = true;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private bool enableDebugLogs = false;

    private CharacterController controller;
    private float verticalVelocity;
    private bool isMoving;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical);
        Vector3 moveDir = inputDirection.sqrMagnitude > 1f ? inputDirection.normalized : inputDirection;

        HandleDebugLogging(moveDir);

        if (useGravity)
        {
            verticalVelocity += gravity * Time.deltaTime;

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
        }

        Vector3 move = moveDir * moveSpeed;

        if (useGravity)
        {
            move.y = verticalVelocity;
        }

        controller.Move(move * Time.deltaTime);

        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void HandleDebugLogging(Vector3 moveDir)
    {
        if (!enableDebugLogs)
        {
            return;
        }

        bool currentlyMoving = moveDir.sqrMagnitude > 0.0001f;

        if (currentlyMoving && !isMoving)
        {
            Debug.Log("Player started moving.");
        }
        else if (!currentlyMoving && isMoving)
        {
            Debug.Log("Player stopped moving.");
        }

        isMoving = currentlyMoving;
    }
}
