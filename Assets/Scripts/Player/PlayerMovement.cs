using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float fallbackMoveSpeed = 6f;   // 没有 stats 时用
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Gravity")]
    [SerializeField] private bool useGravity = true;
    [SerializeField] private float gravity = -9.81f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private CharacterController controller;
    private PlayerResources resources;
    private float verticalVelocity;
    private bool isMoving;

    [HideInInspector] public float moveSpeedMultiplier = 1f; // 新增字段，默认1
    [HideInInspector] public bool isMovementLocked = false;

    private float CurrentMoveSpeed
    {
        get
        {
            if (resources != null && resources.StatsConfig != null)
                return resources.StatsConfig.moveSpeed;

            return fallbackMoveSpeed;
        }
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        resources = GetComponent<PlayerResources>();

        if (controller == null)
            Debug.LogError("[PlayerMovement] CharacterController missing!", this);

        if (resources == null)
            Debug.LogWarning("[PlayerMovement] No PlayerResources found on player. Using fallbackMoveSpeed.", this);
    }

    private void Update()
    {
        if (resources != null && resources.IsDead)
        {
            // 死亡不允许移动
            return;
        }

        // 🔒 闪避等情况可以锁定普通移动
        if (isMovementLocked)
        {
            return;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical);
        Vector3 moveDir = inputDirection.sqrMagnitude > 1f
            ? inputDirection.normalized
            : inputDirection;

        HandleDebugLogging(moveDir);

        if (useGravity)
        {
            verticalVelocity += gravity * Time.deltaTime;

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
        }

        Vector3 move = moveDir * CurrentMoveSpeed * moveSpeedMultiplier;

        if (useGravity)
        {
            move.y = verticalVelocity;
        }

        controller.Move(move * Time.deltaTime);

        RotateTowardsMouse(moveDir);
    }

    private void HandleDebugLogging(Vector3 moveDir)
    {
        if (!enableDebugLogs)
            return;

        bool currentlyMoving = moveDir.sqrMagnitude > 0.0001f;

        if (currentlyMoving && !isMoving)
        {
            Debug.Log("[PlayerMovement] Player started moving.");
        }
        else if (!currentlyMoving && isMoving)
        {
            Debug.Log("[PlayerMovement] Player stopped moving.");
        }

        isMoving = currentlyMoving;
    }

    private void RotateTowardsMouse(Vector3 fallbackDirection)
    {
        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, transform.position);

            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 lookDirection = hitPoint - transform.position;
                lookDirection.y = 0f;

                if (lookDirection.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        rotationSpeed * Time.deltaTime);
                    return;
                }
            }
        }

        RotateTowardsDirection(fallbackDirection);
    }

    private void RotateTowardsDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);
    }
}
