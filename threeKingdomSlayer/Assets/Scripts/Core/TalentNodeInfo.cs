using System.Text;
using UnityEngine;

/// <summary>
/// 天赋节点的「显示信息」解析：图标与详情文案**从它解锁的内容上取**，不手写、不复制数值。
///
/// · 三选一节点（<see cref="TalentNodeConfig.upgradeConfig"/>）：
///     图标 = UpgradeDefinition.icon；
///     文案 = **用游戏自己的描述格式化器按 1 级算**（占位符 {0}/{1}/{2} 由它填），
///            再补上 levelFeatureDescriptions[0]（1 级新增机制）。
///     好处：升级条目的数值改了，天赋页自动跟着变，不需要两边各写一份。
/// · 招式节点（<see cref="TalentNodeConfig.moveConfig"/>）：列出伤害/冷却/范围等 1 级数值。
/// · 其它节点：回退到手写的 description。
/// </summary>
public static class TalentNodeInfo
{
    /// <summary>节点图标：优先节点自己的 icon，其次它所解锁的升级条目自带的图标。</summary>
    public static Sprite ResolveIcon(TalentNodeConfig node)
    {
        if (node == null) return null;
        if (node.icon != null) return node.icon;
        if (node.upgradeConfig != null && node.upgradeConfig.icon != null) return node.upgradeConfig.icon;
        return null;
    }

    /// <summary>详情卡正文：引用解锁内容的 1 级效果；都没有才用节点自带的描述。</summary>
    public static string ResolveDescription(TalentNodeConfig node)
    {
        if (node == null) return "";

        if (node.upgradeConfig != null)
        {
            var def = node.upgradeConfig;
            string text = FormatUpgradeLevel1(def);
            if (!string.IsNullOrEmpty(text)) return text;
        }

        if (node.moveConfig != null)
        {
            var m = node.moveConfig;
            var sb = new StringBuilder();
            sb.Append($"伤害 {m.damage:0.#}（{TypeName(m.damageType)}）");
            if (m.poiseDamage > 0f) sb.Append($" · 架势 {m.poiseDamage:0.#}");
            sb.Append($" · 冷却 {m.cooldown:0.##}s");
            sb.Append($" · 范围 {m.rangeRows} 排");
            if (!string.IsNullOrEmpty(node.description)) sb.Append('\n').Append(node.description);
            return sb.ToString();
        }

        return node.description ?? "";
    }

    private static string FormatUpgradeLevel1(UpgradeDefinition def)
    {
        if (def == null) return "";
        string template = def.descriptionTemplate;
        string text = template;

        // 走项目自己的占位符填法（与战斗内升级卡逐字一致），等级传 1 = 只看 1 级效果
        try
        {
            var formatter = new DefaultUpgradeDescriptionFormatter();
            if (formatter.CanFormat(def, 1))
                text = formatter.Format(def, template, 1);
        }
        catch
        {
            // 格式化器依赖场景对象时退回模板原文，不让天赋页因为文案挂掉
            text = template;
        }

        if (def.levelFeatureDescriptions != null && def.levelFeatureDescriptions.Count > 0
            && !string.IsNullOrEmpty(def.levelFeatureDescriptions[0]))
        {
            text = (text ?? "") + "\n" + def.levelFeatureDescriptions[0];
        }
        return text;
    }

    private static string TypeName(DamageType t)
    {
        switch (t)
        {
            case DamageType.Stab: return "戳击";
            case DamageType.Slash: return "斩击";
            case DamageType.Pierce: return "穿刺";
            case DamageType.Sweep: return "横扫";
            case DamageType.Launch: return "击飞";
            case DamageType.Poise: return "架势";
            default: return t.ToString();
        }
    }
}
