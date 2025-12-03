using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyResources))]
public class EnemyPoiseController : MonoBehaviour
{
    [Header("Runtime (ReadOnly)")]
    [SerializeField] private float currentPoise;
    [SerializeField] private bool isBroken;
    [SerializeField] private float breakTimer;
    [SerializeField] private float regenDelayTimer;

    [Header("Debug")]
    [SerializeField] private ImpactContext currentContext = ImpactContext.Default;

    private EnemyResources _resources;
    private EnemyStatsConfig _stats;

    public event Action<ImpactReaction> OnImpactReaction;

    public float CurrentPoise => currentPoise;
    public bool IsBroken => isBroken;
    public ImpactContext CurrentContext => currentContext;
    public ImpactReaction LastReaction { get; private set; } = ImpactReaction.None;
    public ImpactGrade LastImpactGrade { get; private set; } = ImpactGrade.Small;
    public float MaxPoise => _stats != null ? _stats.maxPoise : 0f;

    private void Awake()
    {
        _resources = GetComponent<EnemyResources>();
        _stats = _resources.Stats;

        if (_stats == null)
        {
            Debug.LogError("[EnemyPoise] EnemyStatsConfig is null.", this);
            enabled = false;
            return;
        }

        ResetPoise();
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 削韧状态计时
        if (isBroken)
        {
            breakTimer -= dt;
            if (breakTimer <= 0f)
            {
                ExitBreak();
            }
            return;
        }

        // 回复延迟计时
        if (regenDelayTimer > 0f)
        {
            regenDelayTimer -= dt;
            return;
        }

        // 韧性回复
        if (currentPoise < _stats.maxPoise && _stats.poiseRegenPerSecond > 0f)
        {
            currentPoise += _stats.poiseRegenPerSecond * dt;
            if (currentPoise > _stats.maxPoise)
                currentPoise = _stats.maxPoise;
        }
    }

    public void ResetPoise()
    {
        currentPoise = _stats.maxPoise;
        isBroken = false;
        breakTimer = 0f;
        regenDelayTimer = 0f;
        currentContext = ImpactContext.Default;
    }

    /// <summary>
    /// 敌人被一次 Attack 命中时调用。
    /// 负责扣韧性 + 查冲击力表 + 返回硬直反应。
    /// </summary>
    public ImpactReaction ApplyHit(AttackData attackData, ImpactContext contextOverride = ImpactContext.Default)
    {
        if (_stats == null || attackData == null)
            return ImpactReaction.None;

        if (isBroken)
        {
            var rBroken = ResolveReaction(attackData.impact, ImpactContext.Broken);
            LastReaction = rBroken;
            LastImpactGrade = attackData.impact;
            OnImpactReaction?.Invoke(rBroken);
            return rBroken;
        }

        // 1) 扣韧性
        float poiseDamage = attackData.poiseDamage;
        if (poiseDamage > 0f)
        {
            currentPoise -= poiseDamage;
            if (currentPoise < 0f) currentPoise = 0f;

            regenDelayTimer = _stats.poiseRegenDelay;
        }

        // 2) 若韧性归零 → 进入削韧状态
        if (currentPoise <= 0f)
        {
            EnterBreak();
            var rBreak = ResolveReaction(attackData.impact, ImpactContext.Broken);

            LastReaction = rBreak;
            LastImpactGrade = attackData.impact;
            OnImpactReaction?.Invoke(rBreak);
            return rBreak;
        }

        // 3) 正常情况下，根据当前上下文查表
        currentContext = contextOverride;
        var reaction = ResolveReaction(attackData.impact, currentContext);

        LastReaction = reaction;
        LastImpactGrade = attackData.impact;
        OnImpactReaction?.Invoke(reaction);
        return reaction;
    }


    private void EnterBreak()
    {
        isBroken = true;
        breakTimer = _stats.breakDuration;
        currentPoise = 0f;
        currentContext = ImpactContext.Broken;
        Debug.Log("[EnemyPoise] Enter BREAK state.", this);
    }

    private void ExitBreak()
    {
        isBroken = false;
        currentPoise = _stats.maxPoise;
        currentContext = ImpactContext.Default;
        Debug.Log("[EnemyPoise] Exit BREAK state.", this);
    }

    private ImpactReaction ResolveReaction(ImpactGrade grade, ImpactContext ctx)
    {
        var profile = _stats.impactProfile;
        if (profile == null || profile.rules == null || profile.rules.Count == 0)
            return ImpactReaction.None;

        StateImpactRule rule = null;

        // 找对应上下文
        for (int i = 0; i < profile.rules.Count; i++)
        {
            if (profile.rules[i].state == ctx)
            {
                rule = profile.rules[i];
                break;
            }
        }

        // 如果没配这个上下文，就尝试找 Default，当兜底
        if (rule == null)
        {
            for (int i = 0; i < profile.rules.Count; i++)
            {
                if (profile.rules[i].state == ImpactContext.Default)
                {
                    rule = profile.rules[i];
                    break;
                }
            }
        }

        if (rule == null)
            return ImpactReaction.None;

        switch (grade)
        {
            case ImpactGrade.Small:
                return rule.smallImpactReaction;
            case ImpactGrade.Medium:
                return rule.mediumImpactReaction;
            case ImpactGrade.Large:
                return rule.largeImpactReaction;
            default:
                return ImpactReaction.None;
        }
    }

    // 以后如果你想在 AI 里手动切换冲击力上下文（比如重攻击期间当成 HeavyWeapon），可以加个接口：
    public void SetContext(ImpactContext ctx)
    {
        currentContext = ctx;
    }
}
