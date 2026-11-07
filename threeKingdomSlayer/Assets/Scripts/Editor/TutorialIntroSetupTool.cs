using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 教学开场 UI 的一次性布线工具（2026-11-06）。
///
/// 干什么：
/// ① 在 <c>TutorialIntroOverlay</c> 下建 <c>IntroDim</c>（不透明黑图 + CanvasGroup）
///    → 教学「第一段文本」期间压暗画面（黑幕），按钮垫在它上面不被压暗；
/// ② 把 <c>TutorialInputBlocker._dim</c> 接到 IntroDim 的 CanvasGroup；
/// ③ 把 <c>TutorialContinueButton._placeAbove</c> 接到玩家的血条（HealthBar）
///    → 「点击继续」按钮摆在血条正上方；
/// ④ 把开幕台词 <c>tutorial_t0_opening</c> 改成 **非阻塞**
///    → 去掉教学关开始时那次的黑幕 + 暂停。
///
/// 幂等：重复跑不会重复建对象。
/// </summary>
public static class TutorialIntroSetupTool
{
    private const string OverlayName = "TutorialIntroOverlay";
    private const string DimCanvasName = "TutorialTeachingDim";
    private const string DimName = "IntroDim";
    private const string HealthBarName = "HealthBar";
    private const string OpeningDialogueName = "tutorial_t0_opening";

    /// <summary>
    /// 压暗层的层级：**必须低于台词气泡**（DialogueOverlay order=120），
    /// 否则那行教学文字自己也被压暗了；又要**高于战斗 HUD**（order=21）→ 110。
    /// </summary>
    private const int DimSortingOrder = 110;

    [MenuItem("Tools/三国杀戮/教学关/开场 UI 布线")]
    public static void RunMenu()
    {
        Run();
        EditorUtility.DisplayDialog("教学开场 UI 布线", "完成。", "好");
    }

    public static string Run()
    {
        var log = new System.Text.StringBuilder();

        var overlay = FindRect(OverlayName);
        var button = FindRect("TutorialContinueButton");
        var healthBar = FindRect(HealthBarName);
        var blocker = UnityEngine.Object.FindObjectOfType<TutorialInputBlocker>(true);
        var continueBtn = UnityEngine.Object.FindObjectOfType<TutorialContinueButton>(true);

        if (overlay == null || button == null || healthBar == null || blocker == null || continueBtn == null)
        {
            log.Append("[TutorialIntroSetup] 找不到目标对象（overlay/button/healthBar/blocker/continueBtn）→ 中止\n");
            Debug.Log(log.ToString());
            return log.ToString();
        }

        // ── ① 压暗层（独立的置顶 Canvas，order=110）────────────────
        //    先清掉旧位置的残留（之前版本放在「点击继续」那层里 → 会把台词气泡一起压暗）
        var stale = overlay.Find(DimName);
        if (stale != null)
        {
            UnityEngine.Object.DestroyImmediate(stale.gameObject);
            log.Append("[TutorialIntroSetup] 删掉了旧的压暗层残留\n");
        }

        //    不能挂在「点击继续」按钮的祖先上（CanvasGroup 会连按钮一起压暗），
        //    也不能放在 order≥120 的层里（会把台词气泡一起压暗）。
        var dimCanvasGo = GameObject.Find(DimCanvasName);
        if (dimCanvasGo == null)
        {
            dimCanvasGo = new GameObject(DimCanvasName);
            var canvas = dimCanvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = DimSortingOrder;
            dimCanvasGo.AddComponent<CanvasScaler>();
        }
        var dimRoot = dimCanvasGo.transform;
        var dim = dimRoot.Find(DimName) as RectTransform;
        if (dim == null)
        {
            var go = new GameObject(DimName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(dimRoot, false);
            dim = go.GetComponent<RectTransform>();
            dim.anchorMin = Vector2.zero;
            dim.anchorMax = Vector2.one;
            dim.offsetMin = Vector2.zero;
            dim.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 1f);   // 不透明黑；透明度交给 CanvasGroup.alpha
            img.raycastTarget = false;                // 射线由「点击继续」那层的遮罩负责

            var group = go.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            log.Append("[TutorialIntroSetup] 建了压暗层 " + DimCanvasName + "/" + DimName + "\n");
        }
        else
        {
            // 之前放在「点击继续」那层里 → 移出来（否则会把台词气泡压暗）
            if (dim.parent != dimRoot)
            {
                dim.SetParent(dimRoot, false);
                dim.anchorMin = Vector2.zero;
                dim.anchorMax = Vector2.one;
                dim.offsetMin = Vector2.zero;
                dim.offsetMax = Vector2.zero;
                log.Append("[TutorialIntroSetup] 压暗层移到独立 Canvas\n");
            }
            else
            {
                log.Append("[TutorialIntroSetup] 压暗层已就位\n");
            }
        }

        // ── ② 接线 ──────────────────────────────────────────────────
        var blockerSo = new SerializedObject(blocker);
        var dimProp = blockerSo.FindProperty("_dim");
        if (dimProp != null)
        {
            dimProp.objectReferenceValue = dim.GetComponent<CanvasGroup>();
            blockerSo.ApplyModifiedPropertiesWithoutUndo();
            log.Append("[TutorialIntroSetup] _dim 已接\n");
        }
        else
        {
            log.Append("[TutorialIntroSetup] !! 找不到 _dim 字段\n");
        }

        var buttonSo = new SerializedObject(continueBtn);
        var aboveProp = buttonSo.FindProperty("_placeAbove");
        if (aboveProp != null)
        {
            aboveProp.objectReferenceValue = healthBar;
            buttonSo.ApplyModifiedPropertiesWithoutUndo();
            log.Append("[TutorialIntroSetup] _placeAbove 已接（玩家血条）\n");
        }
        else
        {
            log.Append("[TutorialIntroSetup] !! 找不到 _placeAbove 字段\n");
        }

        // ── ③ 编辑器里先把按钮摆到血条上方（运行时还会再算一次）──
        Canvas.ForceUpdateCanvases();
        var corners = new Vector3[4];
        healthBar.GetWorldCorners(corners);
        var topCenter = (corners[1] + corners[2]) * 0.5f;
        var local = overlay.InverseTransformPoint(topCenter);
        float desiredCenterY = local.y + 24f + button.rect.height * 0.5f;
        float pivotY = desiredCenterY - (0.5f - button.pivot.y) * button.rect.height;
        var anchorPoint = overlay.rect.min + Vector2.Scale(button.anchorMin, overlay.rect.size);
        button.anchoredPosition = new Vector2(button.anchoredPosition.x, pivotY - anchorPoint.y);
        log.Append($"[TutorialIntroSetup] 按钮 anchoredPosition = {button.anchoredPosition}（血条顶边 local y = {local.y}）\n");

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);

        // ── ④ 开幕台词改非阻塞（去掉教学关开始时的黑幕 + 暂停）──
        var guids = AssetDatabase.FindAssets(OpeningDialogueName + " t:DialogueEventData");
        if (guids.Length > 0)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var data = AssetDatabase.LoadAssetAtPath<DialogueEventData>(path);
            if (data != null)
            {
                var so = new SerializedObject(data);
                var nb = so.FindProperty("nonBlocking");
                if (nb != null)
                {
                    nb.boolValue = true;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(data);
                    log.Append($"[TutorialIntroSetup] 开幕台词 {data.name} → nonBlocking = true\n");
                }
            }
        }
        else
        {
            log.Append("[TutorialIntroSetup] !! 没找到开幕台词资产\n");
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        log.Append("[TutorialIntroSetup] 场景与资产已保存\n");
        Debug.Log(log.ToString());
        return log.ToString();
    }

    private static RectTransform FindRect(string name)
    {
        foreach (var r in UnityEngine.Object.FindObjectsOfType<RectTransform>(true))
        {
            if (r.name == name) return r;
        }
        return null;
    }
}
