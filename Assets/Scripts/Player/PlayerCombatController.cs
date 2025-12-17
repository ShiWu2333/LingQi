using UnityEngine;
using System;

[DisallowMultipleComponent]
public class PlayerCombatController : MonoBehaviour, IAttackSource
{
    [Header("Config")]
    [SerializeField] private WeaponConfig weaponConfig;

    [Header("Weapon Presets (prototype)")]
    [SerializeField] private WeaponConfig weaponSlot1;    // 按 1 切换
    [SerializeField] private WeaponConfig weaponSlot2;    // 按 2 切换
    [SerializeField] private WeaponConfig weaponSlot3;    // 按 3 切换

    [SerializeField] private int currentWeaponIndex = 0;  // 0/1/2 对应上面三个

    [Header("VFX (Test)")]
    [SerializeField] private Transform slashSocket;

    [Header("Hit VFX")]
    [SerializeField] private float hitVfxHeightOffset = 0.4f;  // 命中特效往上抬一点，避免埋在胶囊里

    [Header("Projectile")]
    [SerializeField] private Transform projectileSpawnPoint;

    // Projectile 运行时
    private int projectilesFired;            // 当前 attack 已经发射了几发（用于三连发）
    private float projectileNextFireTime;    // 下一发的时间（相对 Active 起点）
    private float projectileElapsed;         // Active 阶段已经过去的时间（给投射物用）

    [Header("Input")]
    [SerializeField] private KeyCode lightKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode heavyKey = KeyCode.Mouse1;
    [SerializeField] private bool attackBuffered;
    private float lastAttackBufferedTime;

    [Header("Hitbox")]
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private float hitboxHeightOffset = 0f; // 如果你用 Pivot，就可以设为 0
    [SerializeField] private Transform meleeHitboxPivot;    // ⭐ 近战 hitbox 参考点（一般绑到剑/手上）

    [Header("Debug")]
    [SerializeField] private bool debugDrawHitbox = true;

    [Header("Debug (ReadOnly)")]
    [SerializeField] private AttackState currentState = AttackState.Idle;
    [SerializeField] private AttackData currentAttack;
    [SerializeField] private float stateTimer;

    [Header("Debug (Attack Runtime)")]
    [SerializeField] private float attackElapsed;
    [SerializeField] private bool hasHitThisAttack;   // 本招是否已经出过伤害

    // Debug hitbox（统一用球包一下）
    [SerializeField] private Vector3 debugHitboxCenter;
    [SerializeField] private float debugHitboxRadius;

    public event Action<AttackData> OnAttackStarted;
    public event Action<AttackData> OnAttackEnded;
    public event Action OnHeavyChargeStarted;
    public event Action<float> OnHeavyChargeUpdated;   // 0~1
    public event Action<float> OnHeavyChargeCanceled;  // 0~1
    public event Action<float> OnHeavyChargeReleased;  // 0~1

    [Header("Heavy Charge")]
    [SerializeField] private float heavyChargeTimeToFull = 0.75f; // 蓄满时间
    [SerializeField] private float heavyChargeMinToRelease = 0.05f; // 防止点一下也算“蓄力”
    [SerializeField] private bool lockMovementWhileCharging = false;

    [SerializeField] private bool isHeavyCharging;
    [SerializeField] private float heavyChargeTimer;
    public bool IsHeavyCharging => isHeavyCharging;
    public float HeavyCharge01 => Mathf.Clamp01(heavyChargeTimeToFull <= 0f ? 1f : heavyChargeTimer / heavyChargeTimeToFull);
    private float cachedMoveMulBeforeCharge = 1f;

    private PlayerResources resources;
    private PlayerMovement movement;
    private PlayerDodgeController dodge;
    private CharacterController controller;

    private bool dashAttackQueued;

    public AttackState CurrentState => currentState;
    public float CurrentStateTimer => stateTimer;
    public AttackData CurrentAttack => currentAttack;

    private void Awake()
    {
        resources = GetComponent<PlayerResources>();
        movement = GetComponent<PlayerMovement>();
        dodge = GetComponent<PlayerDodgeController>();
        controller = GetComponent<CharacterController>();

        if (weaponSlot1 != null)
        {
            weaponConfig = weaponSlot1;
            currentWeaponIndex = 0;
        }

    }

    private void Update()
    {
        if (resources != null && resources.IsDead)
            return;

        HandleWeaponSwitchInput();      // << 新增

        TryConsumeQueuedDashAttack();
        HandleAttackInput();
        TickAttackState(Time.deltaTime);
    }

    public float GetFinalDamage(AttackData attack)
    {
        if (attack == null)
            return 0f;

        float baseDamage = attack.damage;
        if (resources == null)
            return baseDamage;

        float pBonus = resources.PhysicalBonus; // 例如 0.15
        float mBonus = resources.MagicBonus;    // 例如 0.20

        float physCoef = attack.physicalCoef;
        float magicCoef = attack.magicCoef;

        float mul = 1f + pBonus * physCoef + mBonus * magicCoef;
        if (mul < 0f) mul = 0f;

        return baseDamage * mul;
    }


    // =========================================================
    // INPUT
    // =========================================================
    private void HandleAttackInput()
    {
        if (weaponConfig == null || resources == null)
            return;

        // 轻攻击（保持你原样）
        if (Input.GetKeyDown(lightKey))
        {
            attackBuffered = true;
            lastAttackBufferedTime = Time.time;

            if (currentState == AttackState.Idle && !isHeavyCharging)
            {
                AttackData attackToUse = null;

                bool hasDashAttack = (weaponConfig.dashAttack != null && dodge != null);
                if (hasDashAttack)
                {
                    if (dodge.IsDodging) { dashAttackQueued = true; return; }
                    if (dodge.IsInDashAttackWindow) attackToUse = weaponConfig.dashAttack;
                }

                if (attackToUse == null) attackToUse = weaponConfig.lightAttack;

                TryStartAttack(attackToUse);
                attackBuffered = false;
            }
        }

        // 右键：开始蓄力
        if (Input.GetKeyDown(heavyKey))
        {
            if (currentState == AttackState.Idle && !isHeavyCharging)
            {
                BeginHeavyCharge();
            }
        }

        // 右键按住：更新蓄力
        if (Input.GetKey(heavyKey))
        {
            if (isHeavyCharging)
            {
                TickHeavyCharge(Time.deltaTime);
            }
        }

        // 右键松开：释放重击
        if (Input.GetKeyUp(heavyKey))
        {
            if (isHeavyCharging)
            {
                ReleaseHeavyCharge();
            }
        }
    }
    private void BeginHeavyCharge()
    {
        isHeavyCharging = true;
        heavyChargeTimer = 0f;

        if (movement != null)
        {
            cachedMoveMulBeforeCharge = movement.moveSpeedMultiplier;

            // 用 heavyAttack 的某个倍率作为“蓄力减速”
            // 你提到想用 AttackData 的 MoveMultiplier：这里就直接取 heavyAttack.moveMultiplierStartup（或你认为更合适的那个）
            if (weaponConfig != null && weaponConfig.heavyAttack != null)
                movement.moveSpeedMultiplier = weaponConfig.heavyAttack.moveMultiplierStartup;
        }

        if (lockMovementWhileCharging && movement != null)
            movement.isMovementLocked = true;

        OnHeavyChargeStarted?.Invoke();
        OnHeavyChargeUpdated?.Invoke(0f);
    }

    private void TickHeavyCharge(float dt)
    {
        heavyChargeTimer += dt;
        OnHeavyChargeUpdated?.Invoke(HeavyCharge01);
    }

    public void CancelHeavyCharge()
    {
        if (!isHeavyCharging) return;

        float c01 = HeavyCharge01;

        isHeavyCharging = false;
        heavyChargeTimer = 0f;

        if (lockMovementWhileCharging && movement != null)
            movement.isMovementLocked = false;

        if (movement != null)
            movement.moveSpeedMultiplier = cachedMoveMulBeforeCharge;


        OnHeavyChargeCanceled?.Invoke(c01);
    }

    private void ReleaseHeavyCharge()
    {
        float c01 = HeavyCharge01;

        isHeavyCharging = false;
        heavyChargeTimer = 0f;

        if (lockMovementWhileCharging && movement != null)
            movement.isMovementLocked = false;

        if (movement != null)
            movement.moveSpeedMultiplier = cachedMoveMulBeforeCharge;

        OnHeavyChargeReleased?.Invoke(c01);

        // 太短的按键当作取消（避免误触）
        if (c01 < (heavyChargeMinToRelease <= 0f ? 0f : heavyChargeMinToRelease / Mathf.Max(0.0001f, heavyChargeTimeToFull)))
            return;

        // 释放时真正执行“重击 AttackData”
        if (currentState == AttackState.Idle)
            TryStartAttack(weaponConfig.heavyAttack);

    }

    // =========================================================
    //  Weapon Switching (prototype: 1/2/3)
    // =========================================================

    private void HandleWeaponSwitchInput()
    {
        // 只在 Idle 状态允许切换，避免半路换武器把当前 AttackData 搞乱
        if (currentState != AttackState.Idle)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SwitchWeapon(weaponSlot1, 0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SwitchWeapon(weaponSlot2, 1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SwitchWeapon(weaponSlot3, 2);
        }
    }

    private void SwitchWeapon(WeaponConfig cfg, int index)
    {
        if (cfg == null)
            return;
        if (cfg == weaponConfig)
            return; // 同一把，不必重复切

        weaponConfig = cfg;
        currentWeaponIndex = index;

        // 如果之后要做 UI 显示当前武器，可以在这里触发事件：
        // OnWeaponChanged?.Invoke(cfg);
    }


    // =========================================================
    // START ATTACK
    // =========================================================
    private void TryStartAttack(AttackData attack)
    {
        if (attack == null)
            return;

        if (!resources.TrySpendStamina(attack.staminaCost))
            return;

        if (!resources.TrySpendMana(attack.manaCost))
            return;

        // ⭐ 关键：开启一招新攻击（无论首段还是连段），
        // 先确保朝向是解锁的，这样这招的 Startup 可以重新调整方向
        if (movement != null)
            movement.UnlockFacing();

        currentAttack = attack;
        currentState = AttackState.Startup;
        stateTimer = 0f;
        attackElapsed = 0f;
        hasHitThisAttack = false;

        ApplyMovementMultiplier(attack.moveMultiplierStartup);

        OnAttackStarted?.Invoke(attack);

        // Projectile 计时重置
        projectilesFired = 0;
        projectileElapsed = 0f;
        projectileNextFireTime = attack != null ? attack.hitStartTime : 0f;
    }



    // =========================================================
    // STATE MACHINE
    // =========================================================
    private void TickAttackState(float dt)
    {
        if (currentState == AttackState.Idle || currentAttack == null)
            return;

        stateTimer += dt;
        attackElapsed += dt;

        TickHitWindow();   // 每帧检查 hit window（仅近战）

        switch (currentState)
        {
            case AttackState.Startup:
                UpdateStartup(dt);
                if (TryConsumeBufferedCombo()) return;
                if (stateTimer >= currentAttack.startup)
                    EnterActive();
                break;

            case AttackState.Active:
                if (TryConsumeBufferedCombo()) return;

                if (stateTimer >= currentAttack.active)
                    EnterRecovery();
                else
                    UpdateActive(dt);
                break;

            case AttackState.Recovery:
                if (TryConsumeBufferedCombo()) return;

                if (stateTimer >= currentAttack.recovery)
                    EndAttack();
                break;
        }
    }


    // =========================================================
    // HIT WINDOW 控制（只管近战）
    // =========================================================
    private void TickHitWindow()
    {
        if (currentAttack == null)
            return;

        if (currentAttack.isProjectileAttack)
            return; // projectile 走自己的 firing 系统，不走近战 hitbox

        // 已经打过一次，这一招就不再打
        if (hasHitThisAttack)
            return;

        // ⭐ 只在 Active 阶段才允许出伤害
        if (currentState != AttackState.Active)
            return;

        float hitStart = currentAttack.hitStartTime;
        float hitEnd = currentAttack.hitEndTime;

        // ========== 情况 1：没配 hitWindow，默认 Active 第一帧打一次 ==========
        if (hitStart <= 0f && hitEnd <= 0f)
        {
            // 进入 Active 的第一帧：stateTimer 从 0 开始累加，
            // 一帧内 stateTimer 通常 <= Time.deltaTime
            if (stateTimer <= Time.deltaTime)
            {
                DoHitbox();
                hasHitThisAttack = true;
            }
            return;
        }

        // ========== 情况 2：配了绝对 hitWindow ==========
        // t 是从整招开始算的绝对时间
        float t = attackElapsed;

        // 如果没填 hitEndTime，就默认到 startup+active 结束
        float endTime = hitEnd > 0f ? hitEnd : (currentAttack.startup + currentAttack.active);

        if (t >= hitStart && t <= endTime)
        {
            DoHitbox();
            hasHitThisAttack = true;
        }
    }



    // =========================================================
    // COMBO BUFFER
    // =========================================================
    private bool TryConsumeBufferedCombo()
    {
        if (!attackBuffered)
            return false;

        if (currentAttack == null)
            return false;

        if (currentAttack.nextCombo == null)
            return false;

        float open = currentAttack.comboInputOpenTime;
        float close = currentAttack.comboInputCloseTime;

        if (close <= open)
            return false;

        float t = attackElapsed;

        if (t < open || t > close)
            return false;

        attackBuffered = false;

        TryStartAttack(currentAttack.nextCombo);
        return true;
    }

    /// <summary>
    /// 命中 VFX（近战 / Projectile 共用）
    /// </summary>
    public void PlayHitVfx(Vector3 hitPoint, AttackData attack)
    {
        if (attack == null) return;
        if (attack.hitVFXPrefab == null) return;

        // 1. 把命中点稍微往上抬一点，避免埋在敌人胶囊里
        Vector3 pos = hitPoint + Vector3.up * hitVfxHeightOffset;

        // 2. 让特效大致朝向玩家（或者直接 Quaternion.identity 也行）
        Vector3 dir = (pos - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
            dir = transform.forward;

        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

        // 3. 实例化一次，不挂在任何父节点下面
        GameObject vfx = Instantiate(attack.hitVFXPrefab, pos, rot);

        // 4. 自动销毁，时间可以复用 vfxLifetime
        float life = attack.vfxLifetime > 0f ? attack.vfxLifetime : 0.6f;
        Destroy(vfx, life);
    }


    // =========================================================
    // STATE ENTER / EXIT
    // =========================================================
    private void EnterActive()
    {
        currentState = AttackState.Active;
        stateTimer = 0f;

        ApplyMovementMultiplier(currentAttack.moveMultiplierActive);

        // ⭐ 从 Startup → Active 的这一刻，锁定当前朝向
        if (movement != null)
            movement.LockFacing();

        // Projectile 的起手也可以沿用 Slash / Splash VFX
        SpawnSlashVfx();
        SpawnSplashVfx();

        // ⭐ Projectile 的计时从 Active 开始重新算，保证间隔和你设的一致
        projectileElapsed = 0f;
        projectileNextFireTime = currentAttack != null ? currentAttack.hitStartTime : 0f;
        projectilesFired = 0;
    }


    private void SpawnSlashVfx()
    {
        if (currentAttack == null) return;
        if (slashSocket == null) return;
        if (currentAttack.slashVFXPrefab == null) return;

        // 1. 位置 / 旋转 / 缩放
        Vector3 pos = slashSocket.TransformPoint(currentAttack.slashLocalOffset);
        Quaternion rot = slashSocket.rotation *
                         Quaternion.Euler(currentAttack.slashLocalEulerOffset);

        GameObject vfx = Instantiate(currentAttack.slashVFXPrefab, pos, rot);

        // 跟着剑旋转：挂在 SlashSocket 下面
        vfx.transform.SetParent(slashSocket, worldPositionStays: true);

        if (Mathf.Abs(currentAttack.slashScale - 1f) > 0.001f)
        {
            vfx.transform.localScale *= currentAttack.slashScale;
        }

        // 2. 根据 vfxLifetime 缩放播放速度
        float baseDur = Mathf.Max(0.0001f, currentAttack.vfxBaseDuration);
        float targetDur = currentAttack.vfxLifetime;

        float lifeTimeForDestroy;

        if (targetDur > 0f)
        {
            float speedMul = baseDur / targetDur;

            var systems = vfx.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                var main = ps.main;
                main.simulationSpeed *= speedMul;
            }

            lifeTimeForDestroy = targetDur;
        }
        else
        {
            lifeTimeForDestroy = baseDur + 0.5f;
        }

        Destroy(vfx, lifeTimeForDestroy);
    }

    private void SpawnSplashVfx()
    {
        if (currentAttack == null) return;
        if (currentAttack.splashVFXPrefab == null) return;
        if (slashSocket == null) return;

        // 用 SlashSocket 当前位置作为世界坐标（不 parent）
        Vector3 pos = slashSocket.position;
        // 朝向可以用玩家 forward，或者直接 identity，看具体 prefab
        Quaternion rot = Quaternion.LookRotation(transform.forward, Vector3.up);

        GameObject vfx = Instantiate(currentAttack.splashVFXPrefab, pos, rot);

        // 不设置 parent → 之后剑怎么旋转它都不跟
        float life = (currentAttack.vfxLifetime > 0f) ? currentAttack.vfxLifetime : 0.7f;
        Destroy(vfx, life);
    }

    private void EnterRecovery()
    {
        currentState = AttackState.Recovery;
        stateTimer = 0f;

        ApplyMovementMultiplier(currentAttack.moveMultiplierRecovery);
    }

    public void EndAttack()
    {
        var finished = currentAttack;

        currentState = AttackState.Idle;
        currentAttack = null;
        stateTimer = 0f;

        ApplyMovementMultiplier(1f);

        // ⭐ 攻击完全结束（含 recovery）后，解锁朝向
        if (movement != null)
            movement.UnlockFacing();

        OnAttackEnded?.Invoke(finished);
    }


    // =========================================================
    // PHASE LOGIC
    // =========================================================
    private void UpdateStartup(float dt)
    {
        if (currentAttack == null || controller == null)
            return;

        if (!currentAttack.stepForward)
            return;

        if (currentAttack.stepDuration <= 0f || currentAttack.stepDistance <= 0f)
            return;

        if (stateTimer <= currentAttack.stepDuration)
        {
            float speed = currentAttack.stepDistance / currentAttack.stepDuration;
            Vector3 dir = transform.forward;
            dir.y = 0f;
            dir.Normalize();

            controller.Move(dir * speed * dt);
        }
    }

    private void UpdateActive(float dt)
    {
        // 若要 continuous melee hit，可在这里加入 DoHitbox()

        // ⭐ projectile attack firing（从 Active 开始用自己的计时）
        if (currentAttack != null && currentAttack.isProjectileAttack)
        {
            TickProjectileAttack(dt);
        }
    }

    private void TickProjectileAttack(float dt)
    {
        if (currentAttack == null || !currentAttack.isProjectileAttack)
            return;

        if (currentState != AttackState.Active)
            return;

        projectileElapsed += dt;        // Active 内计时
        int total = Mathf.Max(1, currentAttack.projectileCount);
        if (projectilesFired >= total)
            return;

        float t = projectileElapsed;
        float interval = Mathf.Max(0.01f, currentAttack.projectileInterval);

        // 还没到下一发时间
        if (t < projectileNextFireTime)
            return;

        // 到了就发一发
        FireProjectile();
        projectilesFired++;

        // 预定下一发的时间（仍然是 Active 内的绝对时间）
        projectileNextFireTime += interval;
    }


    private void FireProjectile()
    {
        if (currentAttack == null || currentAttack.projectilePrefab == null)
            return;

        // 计算发射点
        Vector3 spawnPos;
        if (projectileSpawnPoint != null)
            spawnPos = projectileSpawnPoint.position;
        else
            spawnPos = transform.position +
                       transform.rotation * currentAttack.projectileSpawnOffset;

        // 初始朝向：锁定目标优先，其次鼠标
        Vector3 dir = transform.forward;
        if (movement != null)
            dir = movement.GetAimDirection();
        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

        GameObject go = Instantiate(currentAttack.projectilePrefab, spawnPos, rot);

        var proj = go.GetComponent<Projectile>();
        if (proj != null)
        {
            Transform homingTarget = null;
            if (currentAttack.projectileHoming &&
                movement != null &&
                movement.CurrentLockOnTarget != null)
            {
                homingTarget = movement.CurrentLockOnTarget;
            }

            proj.Init(currentAttack, enemyLayers, this, homingTarget);
        }
    }



    // =========================================================
    // HITBOX（固定在玩家身前，多形状）
    // =========================================================
    private void DoHitbox()
    {
        if (currentAttack == null)
            return;

        Vector3 localOffset = currentAttack.hitboxLocalOffset;
        localOffset.y += hitboxHeightOffset;

        Vector3 center = transform.position + transform.rotation * localOffset;

        debugHitboxCenter = center;
        float debugRadius = 0f;

        Collider[] hits = null;

        switch (currentAttack.hitboxShape)
        {
            case HitboxShape.Sphere:
                {
                    float radius = Mathf.Max(0f, currentAttack.hitboxRadius);
                    debugRadius = radius;

                    hits = Physics.OverlapSphere(center, radius, enemyLayers);
                    break;
                }

            case HitboxShape.Box:
                {
                    Vector3 halfExtents = currentAttack.hitboxBoxHalfExtents;
                    debugRadius = halfExtents.magnitude;      // 只是用来画近似球

                    Quaternion rot = transform.rotation;
                    hits = Physics.OverlapBox(center, halfExtents, rot, enemyLayers);
                    break;
                }

            case HitboxShape.Capsule:
                {
                    float radius = Mathf.Max(0f, currentAttack.hitboxCapsuleRadius);
                    float halfHeight = Mathf.Max(currentAttack.hitboxCapsuleHeight * 0.5f, radius);

                    Vector3 dirLocal = currentAttack.hitboxCapsuleDirection;
                    if (dirLocal.sqrMagnitude < 0.0001f)
                        dirLocal = Vector3.forward;

                    Vector3 dirWorld = transform.rotation * dirLocal.normalized;

                    Vector3 p1 = center + dirWorld * halfHeight;
                    Vector3 p2 = center - dirWorld * halfHeight;

                    debugRadius = halfHeight + radius;

                    hits = Physics.OverlapCapsule(p1, p2, radius, enemyLayers);
                    break;
                }

            default:
                return;
        }

        debugHitboxRadius = debugRadius;

        if (hits == null || hits.Length == 0)
            return;

        foreach (var col in hits)
        {
            if (col == null) continue;

            if (col.TryGetComponent(out EnemyResources enemyRes))
            {
                Vector3 hitPoint = col.ClosestPoint(center);
                float finalDamage = GetFinalDamage(currentAttack);
                enemyRes.TakeDamage(finalDamage, hitPoint, currentAttack.impact);

                // ⭐ 近战命中也用同一个 VFX 函数
                PlayHitVfx(hitPoint, currentAttack);
                Debug.Log($"Hit {enemyRes.name} at {hitPoint}");

                if (col.TryGetComponent(out EnemyPoiseController poise))
                    poise.ApplyHit(currentAttack, ImpactContext.Default);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!debugDrawHitbox)
            return;
        if (!Application.isPlaying)
            return;
        if (currentAttack == null)
            return;

        if (currentState != AttackState.Active)
            return;

        float hitStart = currentAttack.hitStartTime;
        float hitEnd = currentAttack.hitEndTime;

        float t = attackElapsed;
        float endTime = hitEnd > 0f ? hitEnd : (currentAttack.startup + currentAttack.active);

        if (t < hitStart || t > endTime)
            return;

        Gizmos.color = Color.yellow;

        Vector3 localOffset = currentAttack.hitboxLocalOffset;
        localOffset.y += hitboxHeightOffset;
        Vector3 center = transform.position + transform.rotation * localOffset;

        switch (currentAttack.hitboxShape)
        {
            case HitboxShape.Sphere:
                Gizmos.DrawWireSphere(center, currentAttack.hitboxRadius);
                break;

            case HitboxShape.Box:
                {
                    Vector3 half = currentAttack.hitboxBoxHalfExtents;
                    Matrix4x4 rotMatrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
                    Gizmos.matrix = rotMatrix;
                    Gizmos.DrawWireCube(Vector3.zero, half * 2f);
                    Gizmos.matrix = Matrix4x4.identity;
                    break;
                }

            case HitboxShape.Capsule:
                DrawWireCapsule(
                    center,
                    transform.rotation,
                    currentAttack.hitboxCapsuleRadius,
                    currentAttack.hitboxCapsuleHeight,
                    currentAttack.hitboxCapsuleDirection
                );
                break;
        }
    }

    private void DrawWireCapsule(Vector3 center, Quaternion rotation, float radius, float height, Vector3 localDir)
    {
        Vector3 dir = rotation * localDir.normalized;
        float half = Mathf.Max(0f, height * 0.5f);

        Vector3 p1 = center + dir * half;
        Vector3 p2 = center - dir * half;

        Gizmos.DrawWireSphere(p1, radius);
        Gizmos.DrawWireSphere(p2, radius);

        int segments = 16;
        for (int i = 0; i < segments; i++)
        {
            float a0 = (float)i / segments * Mathf.PI * 2f;
            float a1 = (float)(i + 1) / segments * Mathf.PI * 2f;

            Vector3 r0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * radius;
            Vector3 r1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * radius;

            Vector3 w0 = rotation * r0;
            Vector3 w1 = rotation * r1;

            Gizmos.DrawLine(p1 + w0, p1 + w1);
            Gizmos.DrawLine(p2 + w0, p2 + w1);
            Gizmos.DrawLine(p1 + w0, p2 + w0);
        }
    }


    // =========================================================
    // DODGE CANCEL
    // =========================================================
    private void TryConsumeQueuedDashAttack()
    {
        if (!dashAttackQueued)
            return;

        if (weaponConfig == null || weaponConfig.dashAttack == null)
        {
            dashAttackQueued = false;
            return;
        }
        if (dodge == null)
        {
            dashAttackQueued = false;
            return;
        }

        if (dodge.IsDodging)
            return;

        if (!dodge.IsInDashAttackWindow)
        {
            dashAttackQueued = false;
            return;
        }

        if (currentState != AttackState.Idle)
            return;

        dashAttackQueued = false;
        TryStartAttack(weaponConfig.dashAttack);
    }

    // =============== 工具方法 ===============
    private void ApplyMovementMultiplier(float multiplier)
    {
        if (movement == null) return;
        movement.moveSpeedMultiplier = multiplier;
    }

    public bool CanDodgeNow()
    {
        if (currentState == AttackState.Idle)
            return true;

        if (currentState == AttackState.Startup || currentState == AttackState.Recovery)
            return true;

        return false;
    }
}
