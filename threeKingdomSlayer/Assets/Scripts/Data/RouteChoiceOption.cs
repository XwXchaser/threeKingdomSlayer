using System;
using UnityEngine;

/// <summary>
/// 路线出口在面板上的**一个选项**（纯数据）。
///
/// 2026-10-22 从 <c>Assets/Scripts/Route/RouteChoicePanel.cs</c> 拆到 <c>Locus.Data</c>：
/// 出口的呈现与可选性都做成了扩展点，它们需要引用这个数据结构，
/// 放在叶子程序集里才能被下层引用。
/// </summary>
[Serializable]
public sealed class RouteChoiceOption
{
    /// <summary>出口 id（对应 <c>FakeRouteChoiceConfig.choiceId</c>）。</summary>
    public string id;

    /// <summary>主标题。</summary>
    public string label;

    /// <summary>副标题（为空则不显示）。</summary>
    public string description;

    /// <summary>图标（为空则用面板的默认图标）。</summary>
    public Sprite icon;

    /// <summary>按钮在面板中的位置与旋转（null = 默认排布）。</summary>
    public RouteChoiceLayout layout;

    /// <summary>是否锁定（不可点）。</summary>
    public bool locked;

    /// <summary>锁定原因（锁定时显示在副标题上）。</summary>
    public string lockedReason;
}
