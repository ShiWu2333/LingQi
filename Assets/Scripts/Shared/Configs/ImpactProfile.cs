using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ImpactProfile",
    menuName = "GameConfigs/Combat/Impact Profile")]
public class ImpactProfile : ScriptableObject
{
    [Tooltip("不同状态下，对应不同冲击力等级的硬直反应表")]
    public List<StateImpactRule> rules = new List<StateImpactRule>();
}

[Serializable]
public class StateImpactRule
{
    [Tooltip("这个规则对应的战斗状态，例如 LightAttack_Active 等")]
    public ImpactContext state;

    [Header("不同冲击力等级的反应")]
    public ImpactReaction smallImpactReaction = ImpactReaction.LightStagger;
    public ImpactReaction mediumImpactReaction = ImpactReaction.MediumStagger;
    public ImpactReaction largeImpactReaction = ImpactReaction.HeavyStagger;
}
