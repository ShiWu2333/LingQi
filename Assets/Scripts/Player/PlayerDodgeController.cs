using UnityEngine;
using System;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class PlayerDodgeController : MonoBehaviour
{
    [Header("Dash Attack Window")]
    [SerializeField] private float dashAttackWindow = 0.25f;   // 闪避结束后还能接 Dash 攻击的时间

    private float postDodgeTimer;

    // ⭐ 给 Combat 查询用：只在闪避结束后的窗口期内为 true
    public bool IsInDashAttackWindow => postDodgeTimer > 0f;

    [Header("Input")]
    [SerializeField] private KeyCode dodgeKey = KeyCode.Space;

    [Header("Debug (ReadOnly)")]
    [SerializeField] private bool isDodging;
    [SerializeField] private bool isInvincible;
    [SerializeField] private float currentCooldown;
    [SerializeField] private Vector3 lastDodgeDirection;

    private CharacterController controller;
    private PlayerResources resources;
    private PlayerMovement movement;
    private Renderer[] renderers;
    private Color[] originalColors;
    [SerializeField] private Color iFrameColor = new Color(0.6f, 0f, 1f, 1f); // 紫色

    
    // 从 Stats 读的配置
    private float dodgeDistance;
    private float dodgeDuration;
    private float dodgeStaminaCost;
    private float dodgeCooldown;
    private float dodgeIFrameStart;
    private float dodgeIFrameEnd;

    private float dodgeTimer;
    private float dodgeSpeed;   // distance / duration
    private PlayerCombatController combat;

    // 对外暴露给 DebugOverlay 用
    public bool IsDodging => isDodging;
    public bool IsInvincible => isInvincible;
    public float CurrentCooldown => currentCooldown;
    public event Action OnDodgeStarted;
    public event Action OnDodgeEnded;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        resources = GetComponent<PlayerResources>();
        movement = GetComponent<PlayerMovement>();
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        combat = GetComponent<PlayerCombatController>();

        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
        }
        if (controller == null)
            Debug.LogError("[PlayerDodgeController] CharacterController not found!", this);
        if (resources == null)
            Debug.LogError("[PlayerDodgeController] PlayerResources not found!", this);
        if (movement == null)
            Debug.LogError("[PlayerDodgeController] PlayerMovement not found!", this);

        LoadConfigFromStats();
    }

    private void LoadConfigFromStats()
    {
        if (resources == null || resources.StatsConfig == null)
        {
            Debug.LogWarning("[PlayerDodgeController] No StatsConfig, using fallback values.", this);
            dodgeDistance = 3f;
            dodgeDuration = 0.22f;
            dodgeStaminaCost = 20f;
            dodgeCooldown = 0.7f;
            dodgeIFrameStart = 0.05f;
            dodgeIFrameEnd = 0.20f;
            return;
        }

        var s = resources.StatsConfig;
        dodgeDistance = s.dodgeDistance;
        dodgeDuration = s.dodgeDuration;
        dodgeStaminaCost = s.dodgeStaminaCost;
        dodgeCooldown = s.dodgeCooldown;
        dodgeIFrameStart = s.dodgeIFrameStart;
        dodgeIFrameEnd = s.dodgeIFrameEnd;
    }

    private void Update()
    {
        if (resources != null && resources.IsDead)
            return;

        float dt = Time.deltaTime;

        TickCooldown(dt);
        HandleInput();
        TickDodge(dt);
        TickPostDodgeWindow(dt);   // ⭐ 新增
    }

    // 冷却计时
    private void TickCooldown(float dt)
    {
        if (currentCooldown > 0f)
        {
            currentCooldown -= dt;
            if (currentCooldown < 0f) currentCooldown = 0f;
        }
    }

    private void HandleInput()
    {
        if (isDodging) return;
        if (currentCooldown > 0f) return;
        if (resources == null) return;

        // 🔴 攻击状态限制：只允许 Idle / Startup / Recovery 闪避
        if (combat != null && !combat.CanDodgeNow())
        {
            // Debug.Log($"[Dodge] Cannot dodge during {combat.CurrentState}");
            return;
        }

        if (Input.GetKeyDown(dodgeKey))
        {
            TryStartDodge();
        }
    }


    private void TryStartDodge()
    {
        // 体力检查
        if (!resources.TrySpendStamina(dodgeStaminaCost))
        {
            Debug.Log("[Dodge] Not enough stamina.");
            return;
        }

        // 如果在攻击中（Startup / Recovery），先打断攻击
        if (combat != null && combat.CurrentState != AttackState.Idle)
        {
            combat.EndAttack();
        }

        // 确定闪避方向：优先输入方向，否则面向方向
        Vector3 dir = GetInputDirection();
        if (dir.sqrMagnitude < 0.01f)
        {
            dir = transform.forward;
        }
        dir.y = 0f;
        dir.Normalize();

        lastDodgeDirection = dir;
        dodgeSpeed = (dodgeDuration > 0f) ? (dodgeDistance / dodgeDuration) : 0f;

        isDodging = true;
        isInvincible = false;
        dodgeTimer = 0f;

        if (movement != null)
        {
            movement.isMovementLocked = true;
        }

        // 🔴 新增：广播开始闪避事件
        OnDodgeStarted?.Invoke();
    }

    private Vector3 GetInputDirection()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0f, v);
        if (input.sqrMagnitude > 1f)
            input.Normalize();
        return input;
    }

    private void TickDodge(float dt)
    {
        if (!isDodging) return;

        dodgeTimer += dt;

        bool nowInvincible = (dodgeTimer >= dodgeIFrameStart && dodgeTimer <= dodgeIFrameEnd);

        // 如果进入或退出无敌状态 → 更新颜色
        if (nowInvincible != isInvincible)
        {
            isInvincible = nowInvincible;

            if (isInvincible)
                SetIFrameColor();
            else
                RestoreOriginalColors();
        }

        // 实际位移
        Vector3 move = lastDodgeDirection * dodgeSpeed;
        // 不处理重力，闪避过程中保持高度（足够用了）
        controller.Move(move * dt);

        // 结束闪避
        if (dodgeTimer >= dodgeDuration)
        {
            EndDodge();
        }
    }

    private void EndDodge()
    {
        isDodging = false;
        isInvincible = false;
        dodgeTimer = 0f;

        currentCooldown = dodgeCooldown;

        if (movement != null)
        {
            movement.isMovementLocked = false;
        }
        postDodgeTimer = dashAttackWindow;
        // 🔴 新增：广播结束闪避事件
        OnDodgeEnded?.Invoke();
    }

    private void TickPostDodgeWindow(float dt)
    {
        if (postDodgeTimer > 0f)
        {
            postDodgeTimer -= dt;
            if (postDodgeTimer < 0f)
                postDodgeTimer = 0f;
        }
    }

    private void SetIFrameColor()
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = iFrameColor;
        }
    }

    private void RestoreOriginalColors()
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].material.color = originalColors[i];
        }
    }

}
