using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
    /// UpgradeEffectManager 的**升级描述文本**：占位符填充与逐级文案。
    ///
    /// 按机制从 <c>UpgradeEffectManager.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 UpgradeEffectManager.cs **同类型**，行为完全一致。
    /// 扩展点：新的升级类型注册自己的描述格式化器，不要往格式化链里加分支。
    /// </summary>

public partial class UpgradeEffectManager : MonoBehaviour, IStatModifierApplier
{

    /// <summary>
    /// 根据描述模板和当前等级生成效果文本
    /// </summary>
    public string GetDescription(UpgradeDefinition def)
    {
        int level = GetDisplayLevel(def);
        return AppendFeatureDescriptions(GetDescriptionForLevel(def, level + 1), def, level + 1, false, level);
    }

    public string GetUpgradePreviewDescription(UpgradeDefinition def)
    {
        int currentLevel = GetDisplayLevel(def);
        int nextLevel = currentLevel + 1;
        string nextDescription = GetDescriptionForLevel(def, nextLevel);
        if (currentLevel <= 0)
            return AppendFeatureDescriptions(nextDescription, def, nextLevel, false, 0);

        string currentDescription = GetDescriptionForLevel(def, currentLevel);
        string comparison = BuildDescriptionComparison(currentDescription, nextDescription);
        return AppendFeatureDescriptions(comparison, def, nextLevel, true, currentLevel);
    }

    private int GetDisplayLevel(UpgradeDefinition def)
    {
        return def.category == UpgradeCategory.ActiveSkill && ActiveSkillInventory.Instance != null
            ? ActiveSkillInventory.Instance.GetLevel(def.upgradeId)
            : (_appliedUpgrades.TryGetValue(def.upgradeId, out int level) ? level : 0);
    }

    private static string AppendFeatureDescriptions(string description, UpgradeDefinition def, int level, bool highlightNew, int currentLevel)
    {
        if (def.levelFeatureDescriptions == null) return description;

        int upperBound = Mathf.Min(level, def.levelFeatureDescriptions.Count);
        for (int i = 0; i < upperBound; i++)
        {
            string feature = def.levelFeatureDescriptions[i];
            if (string.IsNullOrEmpty(feature)) continue;

            bool isNewFeature = highlightNew && i >= currentLevel;
            description += "\n" + (isNewFeature ? "<color=#F5C542>" + feature + "</color>" : feature);
        }
        return description;
    }

    private static string BuildDescriptionComparison(string currentDescription, string nextDescription)
    {
        var currentNumbers = Regex.Matches(currentDescription, @"\d+(?:\.\d+)?");
        var nextNumbers = Regex.Matches(nextDescription, @"\d+(?:\.\d+)?");
        if (currentNumbers.Count == 0 || currentNumbers.Count != nextNumbers.Count)
            return nextDescription;

        int numberIndex = 0;
        return Regex.Replace(nextDescription, @"\d+(?:\.\d+)?", match =>
        {
            string currentValue = currentNumbers[numberIndex].Value;
            string nextValue = match.Value;
            numberIndex++;
            return currentValue != nextValue
                ? currentValue + " → <color=#F5C542>" + nextValue + "</color>"
                : nextValue;
        });
    }

    private string GetDescriptionForLevel(UpgradeDefinition def, int nextLevel)
    {
        string desc = string.IsNullOrEmpty(def.extraDescriptionTemplate)
            ? def.descriptionTemplate
            : def.descriptionTemplate + "\n" + def.extraDescriptionTemplate;
        return FormatDescriptionForLevel(def, desc, nextLevel);
    }

    private string FormatDescriptionForLevel(UpgradeDefinition def, string desc, int nextLevel)
        {
            // 自定义格式化器优先；都没有命中时由内置兜底处理（文案与改造前一致）。
            for (int i = 0; i < _descriptionFormatters.Count; i++)
            {
                var formatter = _descriptionFormatters[i];
                if (formatter != null && formatter.CanFormat(def, nextLevel))
                    return formatter.Format(def, desc, nextLevel);
            }
            return _defaultDescriptionFormatter != null
                ? _defaultDescriptionFormatter.Format(def, desc, nextLevel)
                : desc;
        }
}