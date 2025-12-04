using UnityEngine;

[DisallowMultipleComponent]
public class PlayerSwordProceduralAnimator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerCombatController combat;

    // 攻击数据
    private AttackData currentAttack;
    private SwordMotionConfig motion;

    private float timer;
    private bool playing;

    // 角度都是“相对 baseYaw 的偏移”，单位：度
    private float _baseLocalYaw;       // 初始 localEulerAngles.y
    private float _currentAngle;       // 当前相对角
    private float _fromAngle;          // 本次攻击开始时的角度
    private float _startAngle;
    private float _endAngle;
    private float _anticipationAngle;
    private float _overshootAngle;
    private float _idleAngle;          // 来自 motion.idleAngle

    // 回 idle 的状态
    private bool _returningToIdle;
    private float _idleReturnTimer;
    [SerializeField] private float idleReturnDuration = 0.25f; // 自己可调

    private void OnEnable()
    {
        if (combat == null)
            combat = GetComponentInParent<PlayerCombatController>();

        if (combat != null)
        {
            combat.OnAttackStarted += HandleAttackStarted;
            combat.OnAttackEnded += HandleAttackEnded;
        }
    }

    private void OnDisable()
    {
        if (combat != null)
        {
            combat.OnAttackStarted -= HandleAttackStarted;
            combat.OnAttackEnded -= HandleAttackEnded;
        }
    }

    private void Start()
    {
        _baseLocalYaw = transform.localEulerAngles.y;
        _currentAngle = 0f; // 初始认为 idleAngle = 0
        ApplyAngle(_currentAngle);
    }

    private void HandleAttackStarted(AttackData data)
    {
        currentAttack = data;
        motion = data != null ? data.swordMotion : null;

        if (motion == null)
        {
            playing = false;
            return;
        }

        // 本次攻击从当前角度开始（支持 combo 平滑衔接）
        _fromAngle = _currentAngle;
        _startAngle = motion.startAngle;
        _endAngle = motion.endAngle;
        _anticipationAngle = motion.startAngle + motion.anticipationOffset;
        _overshootAngle = motion.endAngle + motion.overshootOffset;
        _idleAngle = motion.idleAngle;

        timer = 0f;
        playing = true;

        // 如果正在回 idle，而此时又开始新攻击，就打断 idle 回归
        _returningToIdle = false;
    }

    private void HandleAttackEnded(AttackData data)
    {
        // 没有 combo 的情况才会进来，这时 _currentAngle ≈ overshootAngle
        if (motion != null)
        {
            _returningToIdle = true;
            _idleReturnTimer = 0f;
        }

        playing = false;
        currentAttack = null;
        motion = null;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (playing && currentAttack != null && motion != null)
        {
            timer += dt;

            float s = currentAttack.startup;
            float a = currentAttack.active;
            float r = currentAttack.recovery;
            float total = s + a + r;
            float t = timer;

            if (t < s)
            {
                // STARTUP：fromAngle → anticipationAngle → startAngle
                float nt = s > 0f ? t / s : 1f;
                UpdateStartup(nt);
            }
            else if (t < s + a)
            {
                // ACTIVE：anticipationAngle → endAngle（Slow In）
                float nt = a > 0f ? (t - s) / a : 1f;
                UpdateActive(nt);
            }
            else if (t < total)
            {
                // RECOVERY：endAngle → overshootAngle（Slow Out）
                float nt = r > 0f ? (t - s - a) / r : 1f;
                UpdateRecovery(nt);
            }
            else
            {
                // 理论上 AttackState 会在 recovery 结束时调用 OnAttackEnded，
                // 这里兜底一下
                if (!_returningToIdle)
                    HandleAttackEnded(currentAttack);
            }
        }
        else if (_returningToIdle)
        {
            // 没有新攻击，并且需要回 idle
            _idleReturnTimer += dt;
            float nt = Mathf.Clamp01(_idleReturnTimer / idleReturnDuration);

            float angle = Mathf.Lerp(_currentAngle, _idleAngle, EaseOutQuad(nt));
            ApplyAngle(angle);

            if (nt >= 1f)
                _returningToIdle = false;
        }
    }

    // -------- 各阶段曲线 --------

    private void UpdateStartup(float t)
    {
        // Startup 整个阶段：fromAngle -> anticipationAngle（抬手）
        // t: 0~1，使用 EaseOutQuad 让起刀越来越快
        float eased = EaseOutQuad(t);
        float angle = Mathf.Lerp(_fromAngle, _anticipationAngle, eased);
        ApplyAngle(angle);
    }

    private void UpdateActive(float t)
    {
        // Active: anticipationAngle -> endAngle（Slow In）
        float eased = EaseInQuad(t); // 前半慢，后半快
        float angle = Mathf.Lerp(_anticipationAngle, _endAngle, eased);
        ApplyAngle(angle);
    }

    private void UpdateRecovery(float t)
    {
        float eased = EaseOutQuad(t);    // 一开始快，后面慢慢停
        float angle = Mathf.Lerp(_endAngle, _overshootAngle, eased);
        ApplyAngle(angle);
    }

    // -------- 实际写入 Transform --------

    private void ApplyAngle(float attackAngle)
    {
        _currentAngle = attackAngle;
        Vector3 euler = transform.localEulerAngles;
        euler.y = _baseLocalYaw + attackAngle;
        transform.localEulerAngles = euler;
    }

    // 曲线工具：Slow In / Slow Out / EaseInOut
    private float EaseInQuad(float x) => x * x;
    private float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x);
}
