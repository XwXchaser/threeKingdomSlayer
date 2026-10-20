using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 排阵型配置（预设表 - 方案A）
/// 策划可在Unity编辑器中创建和微调每排的精确X偏移
/// </summary>
[CreateAssetMenu(fileName = "NewRowFormationPreset", menuName = "一夫当关/排阵型预设")]
public class RowFormationPreset : ScriptableObject
{
    [System.Serializable]
    public struct RowData
    {
        [Tooltip("该排5列的X偏移值（世界单位），长度必须为5")]
        public float[] columnOffsets;
    }

    [Tooltip("rows[0]对应最前排（排索引0），按顺序配置")]
    public List<RowData> rows;

    /// <summary>
    /// 获取指定排索引的列偏移数组
    /// 如果排索引超出预设长度，返回最后一排的数据
    /// </summary>
    public float[] GetColumnOffsets(int rowIndex)
    {
        if (rows == null || rows.Count == 0)
            return null;

        int clampedIndex = Mathf.Min(rowIndex, rows.Count - 1);
        return rows[clampedIndex].columnOffsets;
    }
}

/// <summary>
/// 排阵型计算器
/// 支持三种方案（优先级从高到低）：
///   方案B：手动每排半宽数组（float[] manualRowHalfWidths）
///   方案A：预设表（RowFormationPreset ScriptableObject）
///   方案C：公式计算（动态生成，参数化调整）
/// 优先使用方案B，若未设置则使用方案A，均未设置则使用方案C
/// </summary>
public static class RowFormation
{
    /// <summary>
    /// 获取指定排、指定列的X轴偏移值
    /// </summary>
    /// <param name="rowIndex">排索引（0=最前排）</param>
    /// <param name="columnIndex">列索引（0~4）</param>
    /// <param name="maxRow">当前最大排索引（用于公式计算）</param>
    /// <param name="manualRowHalfWidths">手动每排半宽数组（方案B，最高优先级，可选）</param>
    /// <param name="preset">预设表（方案A，可选）</param>
    /// <param name="maxSpread">后排半宽（方案C参数，Lerp终点，后排宽）</param>
    /// <param name="minSpread">前排半宽（方案C参数，Lerp起点，前排窄）</param>
    /// <param name="powerCurve">扩散曲线指数（方案C参数，1=线性）</param>
    /// <returns>X轴偏移值（世界单位）</returns>
    public static float GetColumnOffsetX(
        int rowIndex,
        int columnIndex,
        int maxRow,
        float[] manualRowHalfWidths = null,
        RowFormationPreset preset = null,
        float maxSpread = 4.0f,
        float minSpread = 0.5f,
        float powerCurve = 1.2f)
    {
        // 方案B（最高优先级）：手动每排半宽数组
        if (manualRowHalfWidths != null && manualRowHalfWidths.Length > 0)
        {
            // 如果 rowIndex 超出数组长度，使用最后一个值
            int clampedIndex = Mathf.Min(rowIndex, manualRowHalfWidths.Length - 1);
            float spread = manualRowHalfWidths[clampedIndex];
            // 列索引0~4映射到 -spread ~ +spread
            // 列0在最左，列2在中心，列4在最右
            return (columnIndex - 2) * (spread * 2f / 4f);
        }

        // 方案A：使用预设表
        if (preset != null)
        {
            float[] offsets = preset.GetColumnOffsets(rowIndex);
            if (offsets != null && columnIndex >= 0 && columnIndex < offsets.Length)
            {
                return offsets[columnIndex];
            }
        }

        // 方案C：公式计算
        return CalculateOffsetByFormula(rowIndex, columnIndex, maxRow, maxSpread, minSpread, powerCurve);
    }

    /// <summary>
    /// 公式计算X偏移（方案C）
    /// 公式：currentSpread = Lerp(minSpread, maxSpread, t)
    ///       其中 t = Pow(rowIndex / maxRow, powerCurve)
    ///       列偏移 = (columnIndex - 2) * (currentSpread * 2 / 4)
    ///
    /// 前排窄、后排宽（由窄变宽），形成向远处扩散的视觉效果。
    /// </summary>
    private static float CalculateOffsetByFormula(
        int rowIndex,
        int columnIndex,
        int maxRow,
        float maxSpread,
        float minSpread,
        float powerCurve)
    {
        // 防止除以零
        float normalizedRow = maxRow > 0 ? Mathf.Clamp01((float)rowIndex / maxRow) : 0f;

        // 曲线插值
        float t = Mathf.Pow(normalizedRow, powerCurve);

        // 前排窄、后排宽（由窄变宽），向远处扩散
        float currentSpread = Mathf.Lerp(minSpread, maxSpread, t);

        // 列索引0~4映射到 -currentSpread ~ +currentSpread
        // 列0在最左，列2在中心，列4在最右
        float columnOffset = (columnIndex - 2) * (currentSpread * 2f / 4f);

        return columnOffset;
    }

    /// <summary>
    /// 获取指定排的5列完整X偏移数组（用于Gizmos调试）
    /// </summary>
    public static float[] GetRowOffsets(
        int rowIndex,
        int maxRow,
        float[] manualRowHalfWidths = null,
        RowFormationPreset preset = null,
        float maxSpread = 4.0f,
        float minSpread = 0.5f,
        float powerCurve = 1.2f)
    {
        float[] offsets = new float[5];
        for (int col = 0; col < 5; col++)
        {
            offsets[col] = GetColumnOffsetX(rowIndex, col, maxRow, manualRowHalfWidths, preset, maxSpread, minSpread, powerCurve);
        }
        return offsets;
    }

    /// <summary>
    /// 在Scene视图中绘制Gizmos显示阵型位置点
    /// </summary>
    public static void DrawFormationGizmos(
        int maxVisibleRows,
        float rowSpacing,
        float[] manualRowHalfWidths = null,
        RowFormationPreset preset = null,
        float maxSpread = 4.0f,
        float minSpread = 0.5f,
        float powerCurve = 1.2f)
    {
        Gizmos.color = Color.yellow;
        for (int row = 0; row < maxVisibleRows; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                float x = GetColumnOffsetX(row, col, maxVisibleRows - 1, manualRowHalfWidths, preset, maxSpread, minSpread, powerCurve);
                float z = -(row * rowSpacing);
                Gizmos.DrawSphere(new Vector3(x, 0f, z), 0.15f);
            }
        }
    }
}
