using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **遮罩规则**：一次「我要遮什么」的完整描述。
///
/// <para>设计意图（2026-11-07 用户拍板：「本质应该只有一套，只是按照不同规则去遮挡 UI 物体」）：
/// 以前每来一个需求就长一套遮罩（场景压暗 / 全屏 UI 遮罩 / 聚光遮罩 / 点击遮罩），
/// 层级、透明度、渲染可靠性各写各的。现在统一成：
/// <b>谁需要遮 → 交一条规则 → 由同一个遮罩实现落地</b>。</para>
///
/// <para><b>规则里能表达什么</b>（以后加需求通常只改这里，不用再写新组件）：
/// <list type="bullet">
/// <item>遮在哪一层：<see cref="Host"/>（它所在的 Canvas 决定实际层级）</item>
/// <item>遮多强：<see cref="Alpha"/></item>
/// <item>要不要挡点击：<see cref="BlockClicks"/></item>
/// <item>哪些 UI 要豁免（保持亮 + 保持可点）：<see cref="Exempt"/> / <see cref="ExemptFilter"/></item>
/// <item>要不要同时压暗世界背景：<see cref="DimWorld"/>（走 <see cref="IMaskWorldDim"/> 后端）</item>
/// <item>过渡时长：<see cref="FadeSeconds"/></item>
/// </list></para>
///
/// <para>⚠️ <b>层级铁律</b>：遮罩不能压在「要教学的东西」上面。
/// 要么把遮罩放在那一层<b>之下</b>，要么用 <see cref="Exempt"/> 把目标提到遮罩上层。</para>
/// </summary>
public sealed class MaskRule
{
    /// <summary>遮罩建在哪一层（父节点）。必须在一个 UI Canvas 下面。</summary>
    public RectTransform Host;

    /// <summary>压暗强度 0..1（0 = 不压暗）。</summary>
    public float Alpha = 0f;

    /// <summary>是否吃射线（挡住同层里其它 UI 的点击）。</summary>
    public bool BlockClicks;

    /// <summary>是否同时压暗<b>世界背景</b>（需要注册 <see cref="IMaskWorldDim"/> 后端）。</summary>
    public bool DimWorld;

    /// <summary>世界压暗后保留的亮度（1 = 不压暗，默认 0.42）。</summary>
    public float WorldBrightness = 0.42f;

    /// <summary>淡入淡出时长（秒，不受 timeScale 影响）。</summary>
    public float FadeSeconds = 0.12f;

    /// <summary>要保持亮 / 保持可点的 UI 物体（会被提到遮罩上层，关掉时放回原位）。</summary>
    public readonly List<RectTransform> Exempt = new List<RectTransform>();

    /// <summary>
    /// 未来的规则：按条件挑出要豁免的物体（例如「面板里的所有按钮」）。
    /// 与 <see cref="Exempt"/> 是并集关系。
    /// </summary>
    public Func<RectTransform, bool> ExemptFilter;

    /// <summary>深拷贝（服务内部保存规则快照，避免调用方后续改动影响已生效的遮罩）。</summary>
    public MaskRule Clone()
    {
        var copy = new MaskRule
        {
            Host = Host,
            Alpha = Alpha,
            BlockClicks = BlockClicks,
            DimWorld = DimWorld,
            WorldBrightness = WorldBrightness,
            FadeSeconds = FadeSeconds,
            ExemptFilter = ExemptFilter
        };
        copy.Exempt.AddRange(Exempt);
        return copy;
    }
}
