using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// **教学遮罩统一布线**（2026-11-07）。
///
/// 为什么有这个工具：项目里先后长出了好几套「遮罩/压暗」实现（场景压暗、全屏 UI 遮罩、
/// 聚光遮罩、点击遮罩），每套各写各的 → 层级、透明度、渲染可靠性都不一致。
///
/// 现在**统一成一套规则系统**（`MaskRule` + `MaskService` + `UiMask`），
/// 遮罩由服务在运行时创建 → 场景里**不需要预置遮罩对象**，本工具只做「清残留 + 接线」。
///
/// | 遮罩 | 实现 | 宿主层 | 作用 |
/// |---|---|---|---|
/// | 教学拍压暗 | `TutorialSceneDim`（改背景渲染器亮度） | 世界侧 | 只压背景，角色/武器保持亮 |
/// | 选路黑幕 | `MaskService` → `UiMask` | `Tutorial Blackout`（order **19**） | 压暗战场；**路牌按钮与文字在它上面** |
/// | 开场点击遮罩 | `MaskService` → `UiMask` | `TutorialIntroOverlay`（order 121） | 挡点击，不遮挡 |
/// | 主菜单/天赋页聚光 | `MaskService` → `UiMask` | 引导层 / 面板教学层 | 目标豁免（提到上层） |
///
/// ⚠️ **两条硬规矩**（都来自实测）：
///   ① UI 遮罩的底图**一律不透明**，透明度由 `CanvasGroup.alpha` 控制
///      —— 半透明 Image 在本项目渲染不可靠（运行时新建的半透明黑图整层画不出来）。
///   ② 遮罩层级**不能压在要教学的东西上面**：要么压背景（世界侧），要么把目标提到遮罩上层。
///
/// 幂等：重复跑不会重复建对象。
/// </summary>
public static class TutorialMaskSetupTool
{
    private const string BlackoutHostName = "Tutorial Blackout";
    private const string LegacyCurtainName = "Tutorial Blackout Curtain";
    private const string IntroOverlayName = "TutorialIntroOverlay";

    /// <summary>
    /// 黑幕 Canvas 的层级：**必须在路牌面板（BattleHUD root order=21）之下**
    /// → 路牌按钮与它的文字不被压暗；也在手势提示层（200）与暂停层（130）之下 ✓。
    /// </summary>
    private const int BlackoutSortingOrder = 19;

    [MenuItem("Tools/三国杀戮/教学关/教学遮罩统一布线")]
    public static void RunMenu()
    {
        Run();
        EditorUtility.DisplayDialog("教学遮罩统一布线", "完成。", "好");
    }

    public static string Run()
    {
        var log = new System.Text.StringBuilder();

        // ── 选路黑幕：场景预置的不透明底图 + CanvasGroup ──────────────
        var blackout = UnityEngine.Object.FindObjectOfType<TutorialRouteBlackout>(true);
        if (blackout == null)
        {
            log.Append("[MaskSetup] 没找到 TutorialRouteBlackout → 跳过黑幕布线\n");
            Debug.Log(log.ToString());
            return log.ToString();
        }

        var canvas = blackout.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            log.Append("[MaskSetup] 黑幕没有 Canvas → 跳过\n");
            Debug.Log(log.ToString());
            return log.ToString();
        }

        if (canvas.sortingOrder != BlackoutSortingOrder)
        {
            var before = canvas.sortingOrder;
            canvas.sortingOrder = BlackoutSortingOrder;
            log.Append($"[MaskSetup] 黑幕 Canvas 层级 {before} → {BlackoutSortingOrder}（路牌面板在 21，必须在它下面）\n");
        }

        // 遮罩本体由 MaskService 运行时创建（不再场景预置）→ 清掉旧版残留
        var legacy = GameObject.Find(LegacyCurtainName);
        if (legacy != null)
        {
            UnityEngine.Object.DestroyImmediate(legacy);
            log.Append("[MaskSetup] 删掉了旧的场景预置幕布（改由 MaskService 创建）\n");
        }

        // 旧版：TutorialIntroOverlay 上的点击遮罩 Image + TutorialInputBlocker 组件
        var overlay = GameObject.Find(IntroOverlayName);
        if (overlay != null)
        {
            // 组件的脚本已被删（统一成 MaskService）→ 场景里会变成 missing script → 一次性清掉
            int missing = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(overlay);
            if (missing > 0) log.Append($"[MaskSetup] 清掉了 {missing} 个 missing script（旧的点击遮罩组件）\n");
            // 遮罩现在由 MaskService 建在 **TutorialIntroOverlay 下面**（第一个子物体）
            // → 原来那块 Image 必须去掉，否则它会抢走「点击继续」按钮的射线。
            var oldImage = overlay.GetComponent<Image>();
            if (oldImage != null)
            {
                UnityEngine.Object.DestroyImmediate(oldImage);
                log.Append("[MaskSetup] 删掉了 TutorialIntroOverlay 上旧的遮罩 Image\n");
            }
        }

        // 导演接线：开场点击遮罩的宿主层
        var director = UnityEngine.Object.FindObjectOfType<TutorialStageDirector>(true);
        if (director != null && overlay != null)
        {
            var dso = new SerializedObject(director);
            var prop = dso.FindProperty("introMaskHost");
            if (prop != null)
            {
                prop.objectReferenceValue = overlay.transform as RectTransform;
                dso.ApplyModifiedPropertiesWithoutUndo();
                log.Append("[MaskSetup] 导演 introMaskHost 已接到 " + IntroOverlayName + "\n");
            }
        }

        // 幕布渲染已交给 MaskService（运行时创建 UiMask）→ 组件里不再需要 curtain 字段
        log.Append("[MaskSetup] 幕布渲染已交给 MaskService\n");

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        log.Append("[MaskSetup] 场景已保存\n");
        Debug.Log(log.ToString());
        return log.ToString();
    }
}
