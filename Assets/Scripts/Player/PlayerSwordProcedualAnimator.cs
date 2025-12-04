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

    // ------------ Yaw（水平方向，绕 Y） ------------
    private float _baseLocalYaw;
    private float _currentYaw;
    private float _fromYaw;
    private float _startYaw;
    private float _endYaw;
    private float _anticipationYaw;
    private float _overshootYaw;
    private float _idleYaw;

    // ------------ Pitch（垂直方向，绕 X） ------------
    private float _baseLocalPitch;
    private float _currentPitch;
    private float _fromPitch;
    private float _startPitch;
    private float _endPitch;
    private float _anticipationPitch;
    private float _overshootPitch;
    private float _idlePitch;

    // ------------ Roll（扭转，绕 Z） ------------
    private float _baseLocalRoll;
    private float _currentRoll;
    private float _fromRoll;
    private float _startRoll;
    private float _endRoll;
    private float _anticipationRoll;
    private float _overshootRoll;
    private float _idleRoll;

    // 回 idle 的状态
    private bool _returningToIdle;
    private float _idleReturnTimer;
    [SerializeField] private float idleReturnDuration = 0.25f;

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
        Vector3 e = transform.localEulerAngles;
        _baseLocalYaw = e.y;
        _baseLocalPitch = e.x;
        _baseLocalRoll = e.z;

        _currentYaw = 0f;
        _currentPitch = 0f;
        _currentRoll = 0f;

        ApplyAngles(_currentYaw, _currentPitch, _currentRoll);
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

        // ---- Yaw ----
        _fromYaw = _currentYaw;
        _startYaw = motion.startAngle;
        _endYaw = motion.endAngle;
        _anticipationYaw = motion.startAngle + motion.anticipationOffset;
        _overshootYaw = motion.endAngle + motion.overshootOffset;
        _idleYaw = motion.idleAngle;

        // ---- Pitch ----
        _fromPitch = _currentPitch;
        _startPitch = motion.startPitch;
        _endPitch = motion.endPitch;
        _anticipationPitch = motion.startPitch + motion.anticipationPitchOffset;
        _overshootPitch = motion.endPitch + motion.overshootPitchOffset;
        _idlePitch = motion.idlePitch;

        // ---- Roll ----
        _fromRoll = _currentRoll;
        _startRoll = motion.startRoll;
        _endRoll = motion.endRoll;
        _anticipationRoll = motion.startRoll + motion.anticipationRollOffset;
        _overshootRoll = motion.endRoll + motion.overshootRollOffset;
        _idleRoll = motion.idleRoll;

        timer = 0f;
        playing = true;

        _returningToIdle = false;
    }

    private void HandleAttackEnded(AttackData data)
    {
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
                float nt = s > 0f ? t / s : 1f;
                UpdateStartup(nt);
            }
            else if (t < s + a)
            {
                float nt = a > 0f ? (t - s) / a : 1f;
                UpdateActive(nt);
            }
            else if (t < total)
            {
                float nt = r > 0f ? (t - s - a) / r : 1f;
                UpdateRecovery(nt);
            }
            else
            {
                if (!_returningToIdle)
                    HandleAttackEnded(currentAttack);
            }
        }
        else if (_returningToIdle)
        {
            _idleReturnTimer += dt;
            float nt = Mathf.Clamp01(_idleReturnTimer / idleReturnDuration);

            float yaw = Mathf.Lerp(_currentYaw, _idleYaw, EaseOutQuad(nt));
            float pitch = Mathf.Lerp(_currentPitch, _idlePitch, EaseOutQuad(nt));
            float roll = Mathf.Lerp(_currentRoll, _idleRoll, EaseOutQuad(nt));

            ApplyAngles(yaw, pitch, roll);

            if (nt >= 1f)
                _returningToIdle = false;
        }
    }

    // -------- 各阶段曲线 --------

    private void UpdateStartup(float t)
    {
        float eased = EaseOutQuad(t);

        float yaw = Mathf.Lerp(_fromYaw, _anticipationYaw, eased);
        float pitch = Mathf.Lerp(_fromPitch, _anticipationPitch, eased);
        float roll = Mathf.Lerp(_fromRoll, _anticipationRoll, eased);

        ApplyAngles(yaw, pitch, roll);
    }

    private void UpdateActive(float t)
    {
        float eased = EaseInQuad(t);

        float yaw = Mathf.Lerp(_anticipationYaw, _endYaw, eased);
        float pitch = Mathf.Lerp(_anticipationPitch, _endPitch, eased);
        float roll = Mathf.Lerp(_anticipationRoll, _endRoll, eased);

        ApplyAngles(yaw, pitch, roll);
    }

    private void UpdateRecovery(float t)
    {
        float eased = EaseOutQuad(t);

        float yaw = Mathf.Lerp(_endYaw, _overshootYaw, eased);
        float pitch = Mathf.Lerp(_endPitch, _overshootPitch, eased);
        float roll = Mathf.Lerp(_endRoll, _overshootRoll, eased);

        ApplyAngles(yaw, pitch, roll);
    }

    // -------- 实际写入 Transform --------

    private void ApplyAngles(float yaw, float pitch, float roll)
    {
        _currentYaw = yaw;
        _currentPitch = pitch;
        _currentRoll = roll;

        Vector3 e = transform.localEulerAngles;
        e.y = _baseLocalYaw + yaw;   // 水平
        e.x = _baseLocalPitch + pitch; // 垂直
        e.z = _baseLocalRoll + roll;  // 扭转
        transform.localEulerAngles = e;
    }

    // 曲线工具：Slow In / Slow Out
    private float EaseInQuad(float x) => x * x;
    private float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x);
}
