using UnityEngine;

/// <summary>
/// 全局箭矢飞行参数 — 所有敌人（普通/QTE）射出的箭矢共用此配置
/// 放入 Assets/ScriptableObjects/ 下创建 asset 后拖入 Enemy / QTEController 引用
/// </summary>
[CreateAssetMenu(fileName = "ArrowGlobalConfig", menuName = "Config/Arrow Global Config")]
public class ArrowGlobalConfig : ScriptableObject
{
    [Header("俯仰角（按排）")]
    [Tooltip("默认俯仰角（度），perRow 未配置时回退到此值")]
    public float defaultPitchAngle = 12f;
    [Tooltip("按排俯仰角：索引0=最前排(row0)，索引5=最后排(row5)，-1表示使用defaultPitchAngle")]
    public float[] perRowPitchAngles = new float[6] { -1f, -1f, -1f, -1f, -1f, 20f };

    [Header("俯仰角比例")]
    [Tooltip("下降段俯仰角 = 上升段 × 此比例")]
    public float descentPitchRatio = 0.75f;

    [Header("随机化")]
    [Tooltip("生成位置 XZ 随机偏移量")]
    public float randomPositionJitter = 0.3f;
    [Tooltip("飞行时间随机变化比例（±），0.1 = ±10%")]
    public float randomFlightVariation = 0.1f;
    [Tooltip("弧高随机变化比例（±），0.15 = ±15%")]
    public float randomArcVariation = 0.15f;
    [Tooltip("错开发射最大延迟（秒）")]
    public float staggerMax = 0.12f;

    /// <summary>
    /// 获取指定排的俯仰角，未配置则回退到 defaultPitchAngle
    /// </summary>
    public float GetPitchAngleForRow(int row)
    {
        if (perRowPitchAngles != null && row >= 0 && row < perRowPitchAngles.Length)
        {
            float v = perRowPitchAngles[row];
            if (v >= 0f) return v;
        }
        return defaultPitchAngle;
    }
}
