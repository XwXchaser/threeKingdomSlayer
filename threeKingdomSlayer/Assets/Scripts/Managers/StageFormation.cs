using UnityEngine;

/// <summary>
/// 关卡**阵型**计算：梯形/扇形阵型下每列每排的 X 偏移、排间距、Z 偏移、可见排数、
/// 补齐规则、各排透明度系数。
///
/// 从 <c>StageController</c> 拆出：这些全是**纯计算**（输入 StageConfig，输出数值），
/// 不需要任何单例或场景引用，因此既能从关卡门面上摘掉，也能脱离 Unity 场景单测。
/// 原实现散在 StageController 的「查询」区段，这里保持算法与默认值一字未改。
/// </summary>
public sealed class StageFormation
{
    /// <summary>默认排间距（没有 formationConfig 时的兜底）。</summary>
    public const float DefaultRowSpacing = 2.5f;

    /// <summary>默认可见排数。</summary>
    public const int DefaultMaxVisibleRows = 5;

    /// <summary>
    /// 获取指定列和排的 X 轴偏移量（梯形/扇形阵型）。
    ///
    /// BUG FIX（搬迁前就有）：使用 maxVisibleRows 作为固定的最大排数基准，而非动态的 cachedMaxRow。
    /// 之前使用动态 cachedMaxRow 导致当某列敌人减少时，maxRow 变小，
    /// 阵型公式中的 normalizedRow 变大，所有列向中间收缩。
    /// 现在使用固定的 maxVisibleRows，确保阵型位置始终稳定。
    /// </summary>
    public float GetFormationOffset(StageConfig config, int columnIndex, int rowIndex)
    {
        var fc = config != null ? config.formationConfig : null;
        if (fc == null) return (columnIndex - 2) * 2.0f;
        int maxRow = Mathf.Max(fc.maxVisibleRows - 1, 0);

        return RowFormation.GetColumnOffsetX(
            rowIndex, columnIndex, maxRow,
            fc.manualRowHalfWidths,
            fc.formationPreset,
            fc.formationMaxSpread,
            fc.formationMinSpread,
            fc.formationPowerCurve
        );
    }

    /// <summary>排间距。</summary>
    public float GetRowSpacing(StageConfig config)
    {
        return config != null && config.formationConfig != null
            ? config.formationConfig.rowSpacing
            : DefaultRowSpacing;
    }

    /// <summary>阵型整体 Z 轴偏移（用于把敌人生成位置推离/靠近摄像机）。</summary>
    public float GetFormationOffsetZ(StageConfig config)
    {
        return config != null && config.formationConfig != null
            ? config.formationConfig.formationOffsetZ
            : 0f;
    }

    /// <summary>可见排数上限。</summary>
    public int GetMaxVisibleRows(StageConfig config)
    {
        return config != null && config.formationConfig != null
            ? config.formationConfig.maxVisibleRows
            : DefaultMaxVisibleRows;
    }

    /// <summary>各排的透明度系数（可为 null = 全体不透明）。</summary>
    public float[] GetRowAlphaFactors(StageConfig config)
    {
        return config != null && config.formationConfig != null
            ? config.formationConfig.rowAlphaFactors
            : null;
    }

}
