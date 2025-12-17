using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public class Projectile : MonoBehaviour
{
    private AttackData _attack;
    private LayerMask _targetLayers;
    private MonoBehaviour _owner;   // 可以是 PlayerCombatController / EnemyAIController / 其它

    private float _speed;
    private float _lifeTime;
    private float _timer;

    // homing
    private Transform _homingTarget;
    private float _homingTurnSpeedRad;
    private float _homingHeightOffset;

    [Header("Initial Spread")]
    [Tooltip("发射瞬间，在当前朝向附近的最大偏移角度（度）。0 表示不散射")]
    [SerializeField] private float initialSpreadAngleDeg = 8f;

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
        LayerMask targetLayers,
        MonoBehaviour owner,
        Transform homingTarget = null)
    {
        _attack = attack;
        _targetLayers = targetLayers;
        _owner = owner;

        _speed = attack.projectileSpeed;
        _lifeTime = attack.projectileLifeTime;

        // ===== 首帧散射：在当前朝向附近随机一个角度 =====
        // 这里只在水平面(Y 轴)上散射，适合顶视 / 第三人称类魂
        if (initialSpreadAngleDeg > 0f)
        {
            float angle = Random.Range(-initialSpreadAngleDeg, initialSpreadAngleDeg);
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.up) * transform.rotation;
        }

        // ===== 追踪参数 =====
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
        // 软追踪（3D 版）
        if (_homingTarget != null)
        {
            Vector3 targetPos = _homingTarget.position
                                + Vector3.up * _homingHeightOffset;
            Vector3 toTarget = targetPos - transform.position;

            if (toTarget.sqrMagnitude > 0.0001f)
            {
                Vector3 currentDir = transform.forward;

                Vector3 newDir = Vector3.RotateTowards(
                    currentDir,
                    toTarget.normalized,
                    _homingTurnSpeedRad * dt,
                    0f);

                transform.rotation = Quaternion.LookRotation(newDir, Vector3.up);
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
        if (((1 << other.gameObject.layer) & _targetLayers) == 0)
            return;

        Vector3 hitPoint = other.ClosestPoint(transform.position);

        // 命中冲击等级
        ImpactGrade impact =
            _attack != null ? _attack.impact : ImpactGrade.Small;

        // ============ 情况 1：玩家发出的子弹，打敌人 ============
        if (_owner is PlayerCombatController playerOwner)
        {
            if (!other.TryGetComponent(out EnemyResources enemy))
                return;

            float damage = 0f;
            if (_attack != null)
                damage = playerOwner.GetFinalDamage(_attack);

            if (damage > 0f)
                enemy.TakeDamage(damage, hitPoint, impact);

            // 播放命中 VFX
            playerOwner.PlayHitVfx(hitPoint, _attack);

            Destroy(gameObject);
            return;
        }

        // ============ 情况 2：敌人发出的子弹，打玩家 ============
        if (_owner is EnemyAIController enemyOwner)
        {
            if (!other.TryGetComponent(out PlayerResources playerRes))
                return;

            float damage = (_attack != null) ? _attack.damage : 0f;
            if (damage > 0f)
                playerRes.TakeDamage(damage, hitPoint, impact);

            // 如果你未来想让敌人也有命中特效，可以：
            // enemyOwner.PlayHitVfx(hitPoint, _attack);  // 先在 EnemyAIController 里做个空实现

            Destroy(gameObject);
            return;
        }

        // ============ 情况 3：其他 owner（兜底） ============
        float fallbackDamage = (_attack != null) ? _attack.damage : 0f;
        if (fallbackDamage <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // 尝试打敌人
        if (other.TryGetComponent(out EnemyResources enemyFallback))
        {
            enemyFallback.TakeDamage(fallbackDamage, hitPoint, impact);
        }
        // 或者打玩家
        else if (other.TryGetComponent(out PlayerResources playerFallback))
        {
            playerFallback.TakeDamage(fallbackDamage, hitPoint, impact);
        }

        Destroy(gameObject);
    }
}
