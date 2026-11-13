using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// **唯一的 UI 遮罩实现**（2026-11-07 统一遮罩系统）。
///
/// <para>它取代了以前并存的四套东西：<c>TutorialClickMask</c>（聚光）、
/// <c>TutorialInputBlocker</c>（点击遮罩）、<c>TutorialRouteBlackout</c> 的幕布、
/// 以及各处手写的全屏压暗图。差异只体现在 <see cref="MaskRule"/> 上。</para>
///
/// <para><b>三条硬约束（都是实测踩出来的）：</b></para>
/// <list type="number">
/// <item>底图<b>一律不透明</b>（透明交给 <see cref="CanvasGroup"/> 的 alpha）：
/// 本项目实测<b>半透明 Image 渲染不可靠</b>（运行时新建的半透明黑图整层画不出来 →
/// 「黑幕从来没显示过」的真正原因）。</item>
/// <item>遮罩是 <see cref="Host"/> 的<b>第一个子物体</b> → 同一 Canvas 里它在最底；
/// 要豁免的目标被提到 <see cref="Host"/> 的<b>最后</b> → 画在遮罩上层，且不受遮罩的
/// CanvasGroup 影响（不会被一起压暗）。</item>
/// <item>遮罩<b>不吃射线</b>除非规则要求 → 默认只压暗，不挡点击。</item>
/// </list>
///
/// <para>⚠️ 遮罩只能遮<b>UI</b>：世界里的人物（敌人 / 长枪）在所有 UI 之下，
/// 遮罩必然盖住它们 → 「背景压暗、人物保持亮」要走 <see cref="IMaskWorldDim"/> 后端。</para>
/// </summary>
public sealed class UiMask : MonoBehaviour
{
    private CanvasGroup _group;
    private Image _image;
    private RectTransform _host;

    private float _alpha;
    private float _target;
    private float _fadeSeconds = 0.12f;
    private Coroutine _fadeRoutine;

    // 豁免对象：原父节点 + 原序号（关掉时精确放回）
    private readonly List<RectTransform> _exempt = new List<RectTransform>();
    private readonly List<RectTransform> _exemptHome = new List<RectTransform>();
    private readonly List<int> _exemptIndex = new List<int>();

    /// <summary>当前透明度（0..1）。</summary>
    public float Alpha => _alpha;

    /// <summary>当前是否在挡点击。</summary>
    public bool Blocking => _image != null && _image.raycastTarget;

    /// <summary>在指定宿主层下建一块遮罩（幂等：已有则复用）。</summary>
    public static UiMask Create(RectTransform host)
    {
        if (host == null) return null;

        // 同一宿主层只留一块（避免重复 Apply 时叠出多块）
        for (int i = 0; i < host.childCount; i++)
        {
            var existing = host.GetChild(i).GetComponent<UiMask>();
            if (existing != null) return existing;
        }

        var go = new GameObject("UiMask", typeof(RectTransform), typeof(CanvasGroup), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(host, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var mask = go.AddComponent<UiMask>();
        mask._host = host;
        mask._group = go.GetComponent<CanvasGroup>();
        mask._image = go.GetComponent<Image>();
        mask._image.color = new Color(0f, 0f, 0f, 1f);   // 不透明黑（透明交给 CanvasGroup）
        mask._image.raycastTarget = false;                // 默认不挡点击
        mask._group.interactable = false;
        mask._group.blocksRaycasts = true;   // 自己的 Image 要能吃射线（是否吃由 Image.raycastTarget 决定）
        mask._alpha = 0f;
        mask._target = 0f;
        mask._group.alpha = 0f;
        // 垫在宿主层最底下 → 同层里它在最底
        rt.SetAsFirstSibling();
        return mask;
    }

    /// <summary>按规则配置这块遮罩。</summary>
    public void Configure(MaskRule rule)
    {
        if (rule == null) return;
        _fadeSeconds = Mathf.Max(0f, rule.FadeSeconds);

        // 吃射线？
        if (_image != null) _image.raycastTarget = rule.BlockClicks;
        if (_group != null)
        {
            // CanvasGroup 不参与交互判定（我们自己管射线）
            _group.interactable = false;
            _group.blocksRaycasts = true;    // 让自己的 Image 能吃射线（是否吃由 Image.raycastTarget 决定）
        }

        SetExempt(rule.Exempt, rule.ExemptFilter);
        FadeTo(rule.Alpha, _fadeSeconds);
    }

    /// <summary>设置要豁免（保持亮 + 保持可点）的 UI 物体。</summary>
    public void SetExempt(List<RectTransform> targets, Func<RectTransform, bool> filter)
    {
        // 先算出目标集合，再跟当前比：**集合没变就什么都不做**
        // （调用方常常每帧 Apply → 不比对的话会把目标反复重挂到宿主层，
        //   原始父节点就丢了 → 关掉时放不回原位。）
        var wanted = new List<RectTransform>();
        if (targets != null)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (t != null && !wanted.Contains(t)) wanted.Add(t);
            }
        }
        if (filter != null)
        {
            // 只在**当前宿主层的子树**里找（避免把别处的物体提上来）
            var all = _host != null ? _host.GetComponentsInChildren<RectTransform>(true) : null;
            if (all != null)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    var t = all[i];
                    if (t == transform as RectTransform) continue;
                    if (wanted.Contains(t)) continue;
                    if (filter(t)) wanted.Add(t);
                }
            }
        }

        if (SameSet(wanted)) return;

        RestoreExempt();
        for (int i = 0; i < wanted.Count; i++) Exempt(wanted[i]);
    }

    private bool SameSet(List<RectTransform> wanted)
    {
        if (wanted.Count != _exempt.Count) return false;
        for (int i = 0; i < wanted.Count; i++)
        {
            if (_exempt[i] != wanted[i]) return false;
        }
        return true;
    }

    private void Exempt(RectTransform target)
    {
        if (target == null || _host == null) return;
        if (target == transform as RectTransform) return;
        if (_exempt.Contains(target)) return;

        _exempt.Add(target);
        _exemptHome.Add(target.parent as RectTransform);
        _exemptIndex.Add(target.GetSiblingIndex());

        if (target.parent != _host)
        {
            target.SetParent(_host, true);   // 保留世界位置
        }
        target.SetAsLastSibling();            // → 画在遮罩上层
    }

    /// <summary>把豁免对象放回原来的父节点与序号。</summary>
    public void RestoreExempt()
    {
        for (int i = _exempt.Count - 1; i >= 0; i--)
        {
            var t = _exempt[i];
            var home = _exemptHome[i];
            if (t == null) continue;
            if (home != null && t.parent != home)
            {
                t.SetParent(home, true);
                int max = home.childCount - 1;
                t.SetSiblingIndex(Mathf.Clamp(_exemptIndex[i], 0, Mathf.Max(0, max)));
            }
        }
        _exempt.Clear();
        _exemptHome.Clear();
        _exemptIndex.Clear();
    }

    /// <summary>淡入 / 淡出到目标透明度。</summary>
    public void FadeTo(float alpha, float seconds)
    {
        alpha = Mathf.Clamp01(alpha);
        // 目标没变 → **不重启**淡入淡出（调用方常常每帧 Apply → 重启会让协程永远跑不完）
        if (Mathf.Approximately(_target, alpha)) return;

        _target = alpha;
        _fadeSeconds = Mathf.Max(0.001f, seconds);
        if (_fadeRoutine != null) { StopCoroutine(_fadeRoutine); _fadeRoutine = null; }

        if (seconds <= 0.001f)
        {
            _alpha = _target;
            Apply();
            return;
        }

        // ⚠️ **目标已失活时不能启协程**（2026-11-09 修）：
        //   遮罩宿主被禁用 / 场景卸载时，`Configure` → `FadeTo` 会走到这里 →
        //   `StartCoroutine` 报「Coroutine couldn't be started because the the game object 'UiMask' is inactive!」
        //   **而且协程不跑 → 淡出永远不执行**（alpha 卡在原值）。
        //   和 `TutorialSceneDim.FadeTo` 同一套处理：直接落终值。
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            _alpha = _target;
            Apply();
            return;
        }

        _fadeRoutine = StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        while (!Mathf.Approximately(_alpha, _target))
        {
            _alpha = Mathf.MoveTowards(_alpha, _target, Time.unscaledDeltaTime / _fadeSeconds);
            Apply();
            yield return null;
        }
        _alpha = _target;
        Apply();
        _fadeRoutine = null;
    }

    private void Apply()
    {
        if (_group != null) _group.alpha = _alpha;
    }

    /// <summary>关掉：撤掉豁免、淡出。</summary>
    public void Close()
    {
        RestoreExempt();
        FadeTo(0f, _fadeSeconds);
    }

    private void OnDestroy()
    {
        RestoreExempt();
    }
}
