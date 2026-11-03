using System;

/// <summary>
/// **招式引导手势类型**（数据层）。
///
/// 「解锁招式后首次进关」的那一下提示要演什么手势。
/// 枚举放数据层：天赋节点资产要引用它 → 节点配置在 <c>Locus.Data</c> ✅。
/// </summary>
public enum MoveGuideGesture
{
    /// <summary>不弹引导（默认）。</summary>
    None = 0,
    /// <summary>横向划动（横扫类，如 Slash）。</summary>
    SwipeHorizontal = 1,
    /// <summary>纵向划动（上挑类）。</summary>
    SwipeVertical = 2,
    /// <summary>按住蓄力。</summary>
    Hold = 3,
    /// <summary>点按。</summary>
    Tap = 4
}

/// <summary>
/// 一条「招式解锁引导」的请求（纯数据，内核 → 界面层用）。
///
/// 来源：天赋节点上配的 <c>moveGuideGesture</c> + <c>moveGuideDialogue</c> ✅
/// （只有配了手势的节点才会进这个队列 ✅）。
/// </summary>
[Serializable]
public class MoveGuideRequest
{
    /// <summary>来源天赋节点 id（存档里记「已教过」用）。</summary>
    public string nodeId = "";

    /// <summary>招式名（展示用）。</summary>
    public string title = "";

    /// <summary>被解锁的招式。</summary>
    public AttackSkillConfig move;

    /// <summary>要演的手势。</summary>
    public MoveGuideGesture gesture = MoveGuideGesture.None;

    /// <summary>引导台词（一行文字；可为空 → 只演手势）。</summary>
    public DialogueEventData dialogue;
}
