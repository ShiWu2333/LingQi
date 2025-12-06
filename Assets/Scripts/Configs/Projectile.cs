using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public class Projectile : MonoBehaviour
{
    private AttackData _attack;
    private LayerMask _enemyLayers;
    private PlayerCombatController _owner;

    private float _speed;
    private float _lifeTime;
    private float _timer;

    // homing
    private Transform _homingTarget;
    private float _homingTurnSpeedRad;
    private float _homingHeightOffset;

    private void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        var rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    /// <summary>
    /// 初始化投射物
    /// </summary>
    public void Init(
        AttackData attack,
        LayerMask enemyLayers,
        PlayerCombatController owner,
        Transform homingTarget = null)
    {
        _attack = attack;
        _enemyLayers = enemyLayers;
        _owner = owner;

        _speed = attack.projectileSpeed;
        _lifeTime = attack.projectileLifeTime;

        if (attack.projectileHoming && homingTarget != null)
        {
            _homingTarget = homingTarget;
            _homingTurnSpeedRad =
                attack.projectileHomingTurnSpeedDeg * Mathf.Deg2Rad;
            _homingHeightOffset = attack.projectileHomingHeightOffset;
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 软追踪
        if (_homingTarget != null)
        {
            Vector3 targetPos = _homingTarget.position
                                + Vector3.up * _homingHeightOffset;
            Vector3 toTarget = targetPos - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude > 0.0001f)
            {
                Vector3 currentDir = transform.forward;
                currentDir.y = 0f;

                Vector3 newDir = Vector3.RotateTowards(
                    currentDir,
                    toTarget.normalized,
                    _homingTurnSpeedRad * dt,
                    0f);

                transform.rotation =
                    Quaternion.LookRotation(newDir, Vector3.up);
            }
        }

        // 位移
        transform.position += transform.forward * _speed * dt;

        // 寿命
        _timer += dt;
        if (_timer >= _lifeTime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Layer 过滤
        if (((1 << other.gameObject.layer) & _enemyLayers) == 0)
            return;

        if (!other.TryGetComponent(out EnemyResources enemy))
            return;

        Vector3 hitPoint = other.ClosestPoint(transform.position);

        float damage = _attack != null ? _attack.damage : 0f;
        ImpactGrade impact =
            _attack != null ? _attack.impact : ImpactGrade.Small;

        if (damage > 0f)
            enemy.TakeDamage(damage, hitPoint, impact);

        // 播放命中 VFX（复用近战逻辑，不依赖 currentAttack）
        if (_owner != null)
            _owner.PlayHitVfx(hitPoint, _attack);

        Destroy(gameObject);
    }
}
