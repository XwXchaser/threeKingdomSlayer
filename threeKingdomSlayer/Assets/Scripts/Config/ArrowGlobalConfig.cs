using UnityEngine;

/// <summary>
/// 全局箭矢飞行参数 — 所有敌人（普通/QTE）射出的箭矢共用此配置
/// 放入 Assets/ScriptableObjects/ 下创建 asset 后拖入 Enemy / QTEController 引用
/// </summary>
[CreateAssetMenu(fileName = "ArrowGlobalConfig", menuName = "Config/Arrow Global Config")]
public class ArrowGlobalConfig : ScriptableObject
{
    [Header("按排飞行倍率")]
    [Tooltip("飞行时长倍率：索引0=最前排；未配置或值≤0时使用1。")]
    public float[] perRowFlightDurationMultipliers = { 1f, 1f, 1f, 1f, 1f };
    [Tooltip("弧高倍率：索引0=最前排；未配置或值≤0时使用1。")]
    public float[] perRowArcHeightMultipliers = { 1f, 1f, 1f, 1f, 1f };

    [Header("箭矢朝向")]
    [Tooltip("箭矢下落阶段允许的最大俯角（度）；仅限制视觉旋转，不改变抛物线轨迹。")]
    [Range(0f, 89f)] public float maxDescentPitch = 35f;

    [Header("随机化")]
    [Tooltip("生成位置 XZ 随机偏移量")]
    public float randomPositionJitter = 0.3f;
    [Tooltip("飞行时间随机变化比例（±），0.1 = ±10%")]
    public float randomFlightVariation = 0.1f;
    [Tooltip("弧高随机变化比例（±），0.15 = ±15%")]
    public float randomArcVariation = 0.15f;
    [Tooltip("错开发射最大延迟（秒）")]
    public float staggerMax = 0.12f;

    public float GetFlightDurationMultiplierForRow(int row)
    {
        return GetRowMultiplier(perRowFlightDurationMultipliers, row);
    }

    public float GetArcHeightMultiplierForRow(int row)
    {
        return GetRowMultiplier(perRowArcHeightMultipliers, row);
    }

    private static float GetRowMultiplier(float[] multipliers, int row)
    {
        if (multipliers == null || multipliers.Length == 0) return 1f;
        float value = multipliers[Mathf.Clamp(row, 0, multipliers.Length - 1)];
        return value > 0f ? value : 1f;
    }
}
