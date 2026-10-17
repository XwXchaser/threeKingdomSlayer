using UnityEngine;

/// <summary>
/// V2 路线在 Battle 场景中的阶段目标点（首节点 Head / 战斗位 / Tail）。
/// 2026-10-15 从 RouteStageSceneEntryV2.cs 拆为独立文件：
/// 该类原本是文件内的"非主类"，导致场景里的 m_Script 引用被写成非规范形式
/// （只有 fileID、没有脚本 GUID），让玩家构建在预加载该场景时解析失败并 abort。
/// 独立成文件后组件引用为标准的 {fileID: 11500000, guid: ...} 形式。
/// </summary>
public sealed class RouteStageTargetsV2 : MonoBehaviour
{
    public Transform initialHeadTarget;
    public Transform combatTarget;
    public Transform tailTarget;
}
