using UnityEngine;

/// <summary>
/// **品质的显示名与配色**（2026-11-25）——卡片 / 换装列表共用一份，避免两处各写一套。
/// 颜色只表达品质（普通 / 精良 / 传说），不改变任何数值。
/// </summary>
public static class UpgradeQualityStyle
{
    public static string Name(UpgradeQuality quality)
    {
        switch (quality)
        {
            case UpgradeQuality.Fine: return "精良";
            case UpgradeQuality.Legendary: return "传说";
            default: return "普通";
        }
    }

    public static Color Color(UpgradeQuality quality)
    {
        switch (quality)
        {
            case UpgradeQuality.Fine: return new Color(0.44f, 0.71f, 1f, 1f);      // #70B5FF
            case UpgradeQuality.Legendary: return new Color(0.96f, 0.77f, 0.26f, 1f); // #F5C442
            default: return new Color(0.91f, 0.89f, 0.82f, 1f);                     // #E8E3D1
        }
    }

    /// <summary>「传说 Lv.1」这种玩家可见的标识。</summary>
    public static string QualityAndLevel(UpgradeQuality quality, int level)
    {
        return Name(quality) + " Lv." + Mathf.Max(1, level);
    }
}
