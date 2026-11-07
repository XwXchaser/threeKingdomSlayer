using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **遮罩调度器**（统一遮罩系统的唯一入口，2026-11-07）。
///
/// <para>用法（三行）：</para>
/// <code>
/// MaskService.Apply(this, new MaskRule { Host = _layer, Alpha = 0.45f, BlockClicks = true, Exempt = { _btn } });
/// // ……
/// MaskService.Clear(this);
/// </code>
///
/// <para><b>设计意图</b>：以前每来一个需求就长一套遮罩组件（场景压暗 / 全屏 UI 遮罩 / 聚光遮罩 /
/// 点击遮罩），层级与透明度各写各的。现在只有：
/// <list type="bullet">
/// <item>一个 UI 遮罩实现：<see cref="UiMask"/></item>
/// <item>一个世界压暗后端接口：<see cref="IMaskWorldDim"/>（谁来实现都行，教学用 TutorialSceneDim）</item>
/// <item>一条规则格式：<see cref="MaskRule"/></item>
/// </list>
/// 差异全部体现在规则里。</para>
///
/// <para><b>多来源共存</b>：每个 owner（发起者）各持一块遮罩，互不干扰；
/// 世界压暗按 owner 计数（都清了才恢复）。</para>
///
/// <para>⚠️ <b>owner 要是会被销毁的对象</b>（MonoBehaviour / GameObject）：
/// 销毁后调用 <see cref="Clear"/> 会失效 → 服务在每次 <see cref="Apply"/> 时会顺手清掉已销毁的 owner。</para>
/// </summary>
public static class MaskService
{
    private static readonly Dictionary<object, UiMask> Masks = new Dictionary<object, UiMask>();
    private static readonly Dictionary<object, bool> WorldDimOwners = new Dictionary<object, bool>();

    /// <summary>世界侧压暗后端（由需要它的系统注册，例如教学注册 TutorialSceneDim）。</summary>
    public static IMaskWorldDim WorldDim { get; set; }

    /// <summary>当前是否有任何遮罩生效（调试用）。</summary>
    public static int ActiveMaskCount => Masks.Count;

    /// <summary>
    /// 申请一块遮罩（同一个 owner 重复调用 = 更新规则）。
    /// </summary>
    /// <param name="owner">发起者（一般是谁要遮就传谁，用于之后 Clear）。</param>
    /// <param name="rule">规则。</param>
    public static void Apply(object owner, MaskRule rule)
    {
        if (owner == null || rule == null) return;
        CleanupDestroyedOwners();

        // 只压世界背景、不需要 UI 遮罩 → 不建 UI 遮罩
        bool needUiMask = rule.Alpha > 0.001f || rule.BlockClicks;

        UiMask mask = null;
        Masks.TryGetValue(owner, out mask);
        if (mask == null && needUiMask)
        {
            mask = UiMask.Create(rule.Host);
            if (mask == null)
            {
                // 没有宿主层 → 至少把世界压暗做掉，别把规则整个丢掉
                ApplyWorldDim(owner, rule);
                return;
            }
            Masks[owner] = mask;
        }

        if (mask != null) mask.Configure(rule);
        else if (!needUiMask)
        {
            // 规则不再需要 UI 遮罩（例如开场的点击遮罩收掉）→ 把之前那块拆掉
            UiMask stale;
            if (Masks.TryGetValue(owner, out stale))
            {
                Masks.Remove(owner);
                if (stale != null)
                {
                    if (Application.isPlaying) Object.Destroy(stale.gameObject);
                    else Object.DestroyImmediate(stale.gameObject);
                }
            }
        }
        ApplyWorldDim(owner, rule);
    }

    /// <summary>收掉某个 owner 的遮罩。</summary>
    public static void Clear(object owner)
    {
        if (owner == null) return;
        UiMask mask;
        if (Masks.TryGetValue(owner, out mask))
        {
            Masks.Remove(owner);
            if (mask != null)
            {
                if (Application.isPlaying) Object.Destroy(mask.gameObject);
                else Object.DestroyImmediate(mask.gameObject);
            }
        }
        if (WorldDimOwners.ContainsKey(owner))
        {
            WorldDimOwners.Remove(owner);
            RefreshWorldDim();
        }
    }

    /// <summary>某个 owner 当前有没有遮罩。</summary>
    public static bool IsActive(object owner)
    {
        return owner != null && Masks.ContainsKey(owner);
    }

    /// <summary>全部收掉（场景切换 / 回到主菜单时的兜底）。</summary>
    public static void ClearAll()
    {
        var owners = new List<object>(Masks.Keys);
        for (int i = 0; i < owners.Count; i++) Clear(owners[i]);
        var worldOwners = new List<object>(WorldDimOwners.Keys);
        for (int i = 0; i < worldOwners.Count; i++) WorldDimOwners.Remove(worldOwners[i]);
        RefreshWorldDim();
    }

    // ── 世界侧压暗 ────────────────────────────────────────────
    private static void ApplyWorldDim(object owner, MaskRule rule)
    {
        bool want = rule.DimWorld;
        bool had = WorldDimOwners.ContainsKey(owner);
        if (want == had)
        {
            // 规则没变也要刷新亮度（可能被改了）
            if (want) WorldDim?.SetDimmed(true, rule.WorldBrightness);
            return;
        }
        if (want) WorldDimOwners[owner] = true;
        else WorldDimOwners.Remove(owner);
        RefreshWorldDim(rule.WorldBrightness);
    }

    private static void RefreshWorldDim(float brightness = 0.42f)
    {
        if (WorldDim == null) return;
        WorldDim.SetDimmed(WorldDimOwners.Count > 0, brightness);
    }

    // ── 清理已销毁的 owner ─────────────────────────────────────
    private static void CleanupDestroyedOwners()
    {
        if (Masks.Count == 0 && WorldDimOwners.Count == 0) return;
        var dead = new List<object>();
        foreach (var kv in Masks)
        {
            var uo = kv.Key as Object;
            if (uo == null) dead.Add(kv.Key);
        }
        foreach (var kv in WorldDimOwners)
        {
            var uo = kv.Key as Object;
            if (uo == null && !dead.Contains(kv.Key)) dead.Add(kv.Key);
        }
        for (int i = 0; i < dead.Count; i++)
        {
            if (Masks.ContainsKey(dead[i])) Masks.Remove(dead[i]);
            if (WorldDimOwners.ContainsKey(dead[i])) WorldDimOwners.Remove(dead[i]);
        }
        if (dead.Count > 0) RefreshWorldDim();
    }
}
