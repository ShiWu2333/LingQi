using UnityEngine;

public enum ImpactGrade
{
    Small,   // 小型冲击力：短剑、小法术
    Medium,  // 中型冲击力：长剑、正常重击
    Large    // 大型冲击力：巨剑、蓄力、大招
}

public enum ImpactReaction
{
    None,          // 完全不硬直（霸体）
    LightStagger,  // 轻硬直
    MediumStagger, // 中硬直
    HeavyStagger,  // 大硬直
}

public enum ImpactContext
{
    Default,        // 默认：站立、走位等
    LightWeapon,    // 使用轻武器攻击中（整段攻击都算）
    MediumWeapon,   // 中型武器攻击中
    HeavyWeapon,    // 重武器攻击中
    DashAttack,     // Dash 攻击中（可选）
    Dodge,          // 翻滚非 i-frame 部分（要不要吃硬直）
    Broken          // 削韧状态（一般任何冲击都保持硬直）
}

public enum HitboxShape
{
    Sphere,   // 球形（默认）
    Box,      // 盒形（长方体）
    Capsule   // 胶囊体
}