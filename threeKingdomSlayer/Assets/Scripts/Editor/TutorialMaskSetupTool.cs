using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// **教学遮罩统一布线**（2026-11-07）。
///
/// 为什么有这个工具：项目里先后长出了好几套「遮罩/压暗」实现（场景压暗、全屏 UI 遮罩、
/// 聚光遮罩、点击遮罩），每套各写各的 → 层级、透明度、渲染可靠性都不一致。
/// 本工具把**场景侧的遮罩**收成一套：
///
/// | 遮罩 | 实现 | 层级 | 作用 |
/// |---|---|---|---|
/// | 教学拍压暗 | `TutorialSceneDim`（改背景渲染器亮度） | 世界侧 | 只压背景，角色/武器保持亮 |
/// | 选路黑幕 | `TutorialRouteBlackout` + 场景预置的不透明底图 | order **19** | 压暗战场；**路牌按钮与文字在它上面** |
/// | 开场点击遮罩 | `TutorialInputBlocker`（透明 Image，只吃射线） | order 121 | 挡点击，不遮挡 |
/// | 聚光遮罩 | `TutorialClickMask`（不透明底图 + 目标提层） | 宿主层内 | 主菜单 / 天赋页 |
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
    private const string CurtainName = "Tutorial Blackout Curtain";

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
            canvas.sortingOrder = BlackoutSortingOrder;
            log.Append($"[MaskSetup] 黑幕 Canvas 层级 {canvas.sortingOrder} → {BlackoutSortingOrder}（路牌面板在 21，必须在它下面）\n");
        }

        var curtain = canvas.transform.Find(CurtainName) as RectTransform;
        if (curtain == null)
        {
            var go = new GameObject(CurtainName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(canvas.transform, false);
            curtain = go.GetComponent<RectTransform>();
            log.Append("[MaskSetup] 建了场景预置的幕布\n");
        }
        else
        {
            log.Append("[MaskSetup] 幕布已存在\n");
        }

        // 铺满宿主层
        curtain.anchorMin = Vector2.zero;
        curtain.anchorMax = Vector2.one;
        curtain.offsetMin = Vector2.zero;
        curtain.offsetMax = Vector2.zero;

        // 底图**不透明**（透明交给 CanvasGroup.alpha）
        var img = curtain.GetComponent<Image>();
        if (img == null) img = curtain.gameObject.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 1f);
        img.raycastTarget = false;                 // 不拦点击（路牌按钮要点得到）

        var group = curtain.GetComponent<CanvasGroup>();
        if (group == null) group = curtain.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;                          // 默认不黑
        group.interactable = false;
        group.blocksRaycasts = false;              // 黑幕不拦点击

        blackout.curtain = img;
        log.Append("[MaskSetup] 幕布已接到 TutorialRouteBlackout\n");

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        log.Append("[MaskSetup] 场景已保存\n");
        Debug.Log(log.ToString());
        return log.ToString();
    }
}
