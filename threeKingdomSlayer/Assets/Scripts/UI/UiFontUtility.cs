using TMPro;
using UnityEngine;

/// <summary>
/// **中文字体解析**（2026-11-16 新增）。
///
/// 为什么需要：<c>TMP_Settings.defaultFontAsset</c> 在本项目里是 **LiberationSans SDF**（拉丁字体），
/// 运行时用它建 TMP 文本 → **所有中文变成 □**（实测警告：
/// "The character with Unicode value 分 was not found in the [LiberationSans SDF] font asset"）。
///
/// 项目里真正带中文的 TMP 字体是 `Assets/Fonts/方正粗黑宋简体 SDF.asset`，
/// 但它**不在 Resources** 里 → 运行时不能按路径加载（也不能用 AssetDatabase，那是编辑器 API）。
///
/// ⇒ 做法：从**场景里已经在显示中文的 TMP 组件**上取它的字体资产（运行时安全、零新增引用）。
/// 取不到就退回默认字体（英文仍可读）。
/// </summary>
public static class UiFontUtility
{
    private static TMP_FontAsset _cached;

    /// <summary>取一个能显示中文的 TMP 字体（带缓存）。</summary>
    public static TMP_FontAsset ResolveCjkFont()
    {
        if (_cached != null) return _cached;

        // ① 优先：场景里已在用的、非 Liberation 的字体（那就是项目的中文字体）
        var texts = Object.FindObjectsOfType<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            var f = texts[i] != null ? texts[i].font : null;
            if (f == null) continue;
            if (f.name.IndexOf("Liberation", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
            _cached = f;
            return _cached;
        }

        // ② 退而求其次：场景里任何 TMP 文本的字体
        for (int i = 0; i < texts.Length; i++)
        {
            var f = texts[i] != null ? texts[i].font : null;
            if (f != null) { _cached = f; return _cached; }
        }

        // ③ 最后：TMP 默认字体
        _cached = TMP_Settings.defaultFontAsset;
        return _cached;
    }
}
