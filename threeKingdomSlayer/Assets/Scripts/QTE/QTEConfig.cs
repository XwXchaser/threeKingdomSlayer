using UnityEngine;

/// <summary>
/// QTE 类型
/// </summary>
public enum QTEType
{
    Click,  // 点击型 QTE：在判定窗口内点击指定区域
    Swipe   // 划动型 QTE：在判定窗口内沿指定方向划动
}

/// <summary>
/// 单个 QTE 行为的完整配置
/// 包含类型、判定参数、成功/失败效果数值
/// </summary>
[CreateAssetMenu(fileName = "QTEConfig", menuName = "QTE/QTE Config")]
public class QTEConfig : ScriptableObject
{
    [Header("基本设置")]
    public QTEType qteType = QTEType.Click;

    [Header("时机参数")]
    [Tooltip("预警时长（秒）：QTE 图标出现后、判定窗口开启前的视觉提示时间")]
    public float warningDuration = 0.5f;
    [Tooltip("判定窗口时长（秒）：玩家可进行 QTE 输入的有效时间")]
    public float judgeWindow = 1.5f;

    [Header("划动型参数（仅 Swipe 类型生效）")]
    [Tooltip("划动方向（角度制，0=右, 90=上, 180=左, 270=下）")]
    [Range(0f, 360f)] public float swipeDirection = 90f;
    [Tooltip("角度容差（±度），划动方向与目标方向夹角小于此值判定成功")]
    [Range(5f, 90f)] public float swipeAngleTolerance = 60f;
    [Tooltip("划动最小速度（像素/秒）")]
    public float swipeMinSpeed = 200f;

    [Header("成功效果")]
    [Tooltip("架势伤害")]
    public float poiseDamage = 20f;
    [Tooltip("大招充能值")]
    public int ultimateEnergyGain = 10;

    [Header("失败效果")]
    [Tooltip("玩家承受伤害")]
    public float failureDamage = 15f;

    [Header("视觉")]
    [Tooltip("QTE 指示器 prefab（由美术提供，含精灵图片和范围设定）")]
    public GameObject qteIndicatorPrefab;
    [Tooltip("QTE 指示器在屏幕上的归一化坐标 (0~1)")]
    public Vector2 screenPosition = new Vector2(0.5f, 0.6f);
    [Tooltip("QTE 指示器在 Canvas 上的尺寸（像素，基于参考分辨率 1080×1920）")]
    public Vector2 indicatorSize = new Vector2(200f, 200f);
}
