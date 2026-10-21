using UnityEngine;

/// <summary>
/// 冰系表现共享配置：所有敌人共用一份，避免每个预制体各配一套（换美术只改这里）。
/// Enemy 优先读本配置；本配置为空时才回落到 Enemy 自身的同名字段。
/// </summary>
[CreateAssetMenu(fileName = "FreezePresentationConfig", menuName = "一夫当关/冰系表现配置")]
public class FreezePresentationConfig : ScriptableObject
{
    [Header("预制体")]
    [Tooltip("冻结覆盖层（盖在敌人身上）")]
    public GameObject freezeOverlayPrefab;
    [Tooltip("破冰碎裂序列帧特效（重攻击打破冰冻）")]
    public GameObject iceBreakEffectPrefab;
    [Tooltip("自然解冻融化序列帧特效")]
    public GameObject freezeMeltEffectPrefab;

    [Header("尺寸")]
    [Tooltip("冰块高度 / 角色可见高度（等比缩放，保持素材长宽比）")]
    [Range(0.3f, 2.5f)] public float overlayScale = 0.825f;
    [Tooltip("冰块中心垂直偏移（相对角色可见高度，正 = 向上）")]
    [Range(-0.5f, 0.5f)] public float overlayYOffset = 0.1f;
}
