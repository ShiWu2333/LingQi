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

    [Header("Lock-On")]
    [SerializeField] private KeyCode lockOnKey = KeyCode.Q;
    [SerializeField] private float lockOnMaxDistance = 15f;
    [SerializeField] private LayerMask lockOnEnemyLayers = ~0;   // 可以指定 Enemy 层
    [SerializeField] private bool debugLockOn = false;

    private CharacterController controller;
    private PlayerResources resources;
    private float verticalVelocity;
    private bool isMoving;

    [HideInInspector] public float moveSpeedMultiplier = 1f; // 外部控制移速
    [HideInInspector] public bool isMovementLocked = false;  // 闪避等锁移动
    public float CurrentPlanarSpeed { get; private set; }

    // ===== 攻击期间锁朝向 =====
    private bool isFacingLocked = false;
    private Quaternion lockedRotation;

    // ===== 锁定敌人 =====
    private Transform lockOnTarget;
    public bool IsLockOnActive => lockOnTarget != null;
    public Transform CurrentLockOnTarget => lockOnTarget;

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
            return;

        HandleLockOnInput();
        ValidateLockOnTarget();

        if (isMovementLocked)
        {
            // 移动被锁（比如闪避），但朝向是否锁定由 isFacingLocked / 锁定系统决定
            if (isFacingLocked)
                transform.rotation = lockedRotation;
            else if (lockOnTarget != null)
                RotateTowardsWorldPosition(lockOnTarget.position);
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

        Vector3 planar = new Vector3(move.x, 0f, move.z);
        CurrentPlanarSpeed = planar.magnitude;

        if (useGravity)
        {
            move.y = verticalVelocity;
        }

        controller.Move(move * Time.deltaTime);

        // ===== 朝向逻辑优先级 =====
        // 1）攻击锁朝向
        // 2）锁定敌人
        // 3）正常鼠标朝向
        if (isFacingLocked)
        {
            transform.rotation = lockedRotation;
        }
        else if (lockOnTarget != null)
        {
            RotateTowardsWorldPosition(lockOnTarget.position);
        }
        else
        {
            RotateTowardsMouse(moveDir);
        }
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

    // =========================================================
    //  锁定系统：Q 键切换锁定最近敌人
    // =========================================================
    private void HandleLockOnInput()
    {
        if (Input.GetKeyDown(lockOnKey))
        {
            if (lockOnTarget != null)
            {
                // 已经锁定 → 取消锁定
                if (debugLockOn) Debug.Log("[LockOn] Unlock");
                lockOnTarget = null;
            }
            else
            {
                // 尝试获取新的锁定目标
                lockOnTarget = FindBestLockOnTarget();
                if (debugLockOn)
                {
                    if (lockOnTarget != null)
                        Debug.Log("[LockOn] Locked on: " + lockOnTarget.name);
                    else
                        Debug.Log("[LockOn] No valid target.");
                }
            }
        }
    }

    private void ValidateLockOnTarget()
    {
        if (lockOnTarget == null)
            return;

        // 目标死了/被禁用/离得太远，就取消锁定
        EnemyResources er = lockOnTarget.GetComponent<EnemyResources>();
        if (er == null || !lockOnTarget.gameObject.activeInHierarchy || er.IsDead)
        {
            lockOnTarget = null;
            return;
        }

        float distSqr = (lockOnTarget.position - transform.position).sqrMagnitude;
        if (distSqr > lockOnMaxDistance * lockOnMaxDistance)
        {
            lockOnTarget = null;
        }
    }

    /// <summary>
    /// 在 lockOnMaxDistance 范围内，选择“屏幕上离鼠标最近”的敌人。
    /// </summary>
    private Transform FindBestLockOnTarget()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return null;

        EnemyResources[] enemies = GameObject.FindObjectsOfType<EnemyResources>();
        if (enemies == null || enemies.Length == 0)
            return null;

        Vector2 mousePos = Input.mousePosition;
        float bestScore = float.MaxValue;
        Transform bestTarget = null;

        foreach (var e in enemies)
        {
            if (e == null || e.IsDead)
                continue;

            Transform t = e.transform;

            // 距离限制
            Vector3 toEnemy = t.position - transform.position;
            float distSqr = toEnemy.sqrMagnitude;
            if (distSqr > lockOnMaxDistance * lockOnMaxDistance)
                continue;

            // 屏幕空间位置
            Vector3 screenPos = cam.WorldToScreenPoint(t.position);
            if (screenPos.z <= 0f)
                continue; // 在摄像机背后

            Vector2 screen2D = new Vector2(screenPos.x, screenPos.y);
            float cursorDistSqr = (screen2D - mousePos).sqrMagnitude;

            // 以“鼠标距离”为主排序（越近越优先）
            float score = cursorDistSqr;

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = t;
            }
        }

        return bestTarget;
    }

    // =========================================================
    //  旋转逻辑
    // =========================================================
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

    private void RotateTowardsWorldPosition(Vector3 worldPos)
    {
        Vector3 dir = worldPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude <= 0.0001f)
            return;

        Quaternion target = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            target,
            rotationSpeed * Time.deltaTime);
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

    // =========================================================
    //  给 Combat / 其他系统调用的接口
    // =========================================================

    /// <summary>攻击开始时锁定当前朝向（Combat 在 Active 开始时调用）。</summary>
    public void LockFacing()
    {
        isFacingLocked = true;
        lockedRotation = transform.rotation;
    }

    /// <summary>攻击结束时解除朝向锁定。</summary>
    public void UnlockFacing()
    {
        isFacingLocked = false;
    }
}
