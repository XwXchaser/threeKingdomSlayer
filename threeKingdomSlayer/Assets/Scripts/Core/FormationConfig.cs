using UnityEngine;

/// <summary>
/// 阵型与可见性配置 - ScriptableObject
/// 多关卡共享同一份配置，避免每个 StageConfig 重复设置
/// </summary>
[CreateAssetMenu(fileName = "NewFormationConfig", menuName = "一夫当关/阵型配置")]
public class FormationConfig : ScriptableObject
{
    [Header("排阵型配置（梯形/扇形扩散）")]
    [Tooltip("方案A：预设表。若设置则优先使用预设表，否则使用方案C公式计算")]
    public RowFormationPreset formationPreset;

    [Header("方案B：手动每排宽度（优先级最高）")]
    [Tooltip("手动指定每一排的半宽值，数组索引=排索引（0=最前排）。\n例如 [2.0, 2.5, 3.0, 3.5, 4.0] 表示第0排半宽2.0（窄）、第4排半宽4.0（宽），由窄变宽。\n设置此数组后，方案A预设表和方案C公式均被忽略。")]
    public float[] manualRowHalfWidths;

    [Header("方案C：公式参数（仅当未设置预设表和手动数组时生效）")]
    [Tooltip("前排（rowIndex=0）半宽（窄）。例如2.0表示最前排最左列X=-2.0")]
    public float formationMaxSpread = 4.0f;
    [Tooltip("后排半宽（宽）。例如4.0表示最后排最左列X=-4.0")]
    public float formationMinSpread = 0.5f;
    [Tooltip("扩散曲线指数。1.0=线性，>1.0=后排更快扩散")]
    public float formationPowerCurve = 1.2f;

    [Header("排间距与偏移")]
    [Tooltip("排间距（Z轴，世界单位）")]
    public float rowSpacing = 2.5f;
    [Tooltip("阵型整体Z轴偏移（正值=远离摄像机）")]
    public float formationOffsetZ = 0f;

    [Header("可见性")]
    [Tooltip("每排的透明度系数，索引0=最前排。例如 [1.0, 0.8, 0.6, 0.4, 0.2]")]
    public float[] rowAlphaFactors = new float[] { 1.0f, 0.8f, 0.6f, 0.4f, 0.2f };
    [Tooltip("玩家能看到的最大排数，超出此排数的敌人完全透明")]
    public int maxVisibleRows = 5;
}
