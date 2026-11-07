using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// **教学遮罩 / 遮挡审计**（2026-11-07）。
///
/// 干什么：跑一遍教学，在每个「暂停时刻」（选路牌 / 教学拍 / 三选一）做三件事：
///   ① 抓一张截图（抓给人和 AI 看画面）；
///   ② 在关键位置做 EventSystem 射线 → 记录**命中顺序**（谁在最上面）；
///   ③ 记录所有「压暗/遮罩」组件的当前状态。
/// 输出：<c>Library/Locus/tmp/mask-audit/</c> 下的 PNG + report.txt。
///
/// 用法（Play Mode 里）：
/// <c>Tools ▸ 三国杀戮 ▸ 教学关 ▸ 遮罩与遮挡审计</c>
/// 或让 Locus 调 <c>TutorialMaskAudit.Start()</c>（会自己拉起自动驱动）。
/// </summary>
public static class TutorialMaskAudit
{
    private const string OutDir = "Library/Locus/tmp/mask-audit";

    private static readonly StringBuilder Report = new StringBuilder();
    private static readonly HashSet<string> Captured = new HashSet<string>();
    private static bool _running;
    private static float _startTime;
    private static float _routeEnteredAt;
    private static int _shotIndex;
    private static int _pendingShots;
    private static string _pendingTag;

    public static void Start()
    {
        if (_running) return;
        Directory.CreateDirectory(OutDir);
        Report.Clear();
        Captured.Clear();
        _shotIndex = 0;
        _pendingShots = 0;
        _running = true;
        _startTime = Time.unscaledTime;
        EditorApplication.update += Tick;
        TutorialFlowDriver.StartInPlayMode();
        Debug.Log("[MaskAudit] 开始审计");
    }

    public static void Stop()
    {
        EditorApplication.update -= Tick;
        _running = false;
        File.WriteAllText(Path.Combine(OutDir, "report.txt"), Report.ToString());
        Debug.Log("[MaskAudit] 结束 → " + Path.Combine(OutDir, "report.txt"));
    }

    private static void Tick()
    {
        if (!_running) return;
        if (Time.unscaledTime - _startTime > 150f) { Stop(); return; }

        // 抓图是异步的：上一张写完再抓下一张
        if (_pendingShots > 0)
        {
            _pendingShots--;
            return;
        }

        var runtime = RouteFlowHub.Current as FakeRouteRuntime;
        if (runtime == null) return;

        if (runtime.Phase == FakeRoutePhase.ChoosingRoute)
        {
            if (_routeEnteredAt <= 0f) _routeEnteredAt = Time.unscaledTime;
        }
        else
        {
            _routeEnteredAt = 0f;
        }

        if (runtime.Phase == FakeRoutePhase.ChoosingRoute && Captured.Add("route"))
        {
            // 让驱动**别马上点路牌** → 否则 ChoosingRoute 只持续 0.1s，幕布还没升上来就切走了
            TutorialFlowDriver.skipRouteChoiceClick = true;
            Capture("route_choice");
            AuditRouteChoice();
        }
        else if (runtime.Phase == FakeRoutePhase.ChoosingRoute && Captured.Contains("route") && !Captured.Contains("route_settled"))
        {
            // 等幕布淡入到位（0.12s）再抓一张 + 重新量一次层级
            if (Time.unscaledTime - _routeEnteredAt > 0.5f)
            {
                Captured.Add("route_settled");
                Capture("route_choice_settled");
                AuditRouteChoice();
            }
        }

        var view = BattleDialogueView.Instance;
        if (view != null && view.IsShowing)
        {
            var dim = UnityEngine.Object.FindObjectOfType<TutorialSceneDim>(true);
            if (dim != null && dim.IsDimming && Captured.Add("beat"))
            {
                Capture("teaching_beat");
                AuditTeachingBeat();
            }
        }
    }

    // ── 抓图 ────────────────────────────────────────────────
    private static void Capture(string tag)
    {
        string path = Path.Combine(OutDir, $"{_shotIndex:00}_{tag}.png");
        _shotIndex++;
        _pendingShots = 3;
        ScreenCapture.CaptureScreenshot(path);
        Report.AppendLine($"[截图] {Path.GetFileName(path)}");
    }

    // ── 选路牌那一刻 ─────────────────────────────────────────
    private static void AuditRouteChoice()
    {
        Report.AppendLine();
        Report.AppendLine("======== 选路牌（ChoosingRoute）========");

        var curtain = FindCurtain();
        Report.AppendLine($"-- 幕布 Tutorial Blackout Curtain：{(curtain == null ? "不存在" : "存在")}");
        if (curtain != null)
        {
            var cg = curtain.GetComponent<CanvasGroup>();
            var canvas = curtain.GetComponentInParent<Canvas>();
            Report.AppendLine($"   canvas={canvas.name}(order {canvas.sortingOrder}) color={curtain.color} groupAlpha={(cg == null ? "-" : cg.alpha.ToString("F2"))} blocks={(cg == null ? "-" : cg.blocksRaycasts.ToString())} active={curtain.gameObject.activeInHierarchy} raycastTarget={curtain.raycastTarget}");
        }

        var btn = UnityEngine.Object.FindObjectOfType<RouteChoiceButton>();
        if (btn == null)
        {
            Report.AppendLine("-- 没找到 RouteChoiceButton");
            return;
        }
        var rt = btn.transform as RectTransform;
        var canvasBtn = rt.GetComponentInParent<Canvas>();
        Report.AppendLine($"-- 路牌按钮 canvas={canvasBtn.name}(order {canvasBtn.sortingOrder}) 位置={rt.anchoredPosition} 世界={rt.position}");
        RaycastAt(rt, "路牌按钮中心");
    }

    // ── 教学拍那一刻 ─────────────────────────────────────────
    private static void AuditTeachingBeat()
    {
        Report.AppendLine();
        Report.AppendLine("======== 教学拍（压暗中）========");

        var dim = UnityEngine.Object.FindObjectOfType<TutorialSceneDim>(true);
        Report.AppendLine($"-- TutorialSceneDim.IsDimming={(dim == null ? "组件缺失" : dim.IsDimming.ToString())} dimBrightness={(dim == null ? "-" : dim.dimBrightness.ToString())}");

        var sky = RenderSettings.skybox;
        if (sky != null && sky.HasProperty("_SkyTint")) Report.AppendLine($"-- 天空盒 {sky.name} _SkyTint={sky.GetColor("_SkyTint")}");

        // 敌人 / 武器 的屏幕位置与射线
        var alive = EnemyManager.Instance != null ? EnemyManager.Instance.GetAllAliveEnemies() : null;
        if (alive != null && alive.Count > 0)
        {
            var e = alive[0];
            var sr = e != null ? e.GetComponentInChildren<SpriteRenderer>() : null;
            if (sr != null) Report.AppendLine($"-- 敌人精灵亮度={sr.color.grayscale:0.00} sortingOrder={sr.sortingOrder}");
            // 敌人的屏幕矩形：确认它**在屏幕上**、且没有被 UI 挡住
            var cam = Camera.main;
            if (e != null && cam != null)
            {
                var b = sr.bounds;
                var sp0 = cam.WorldToScreenPoint(b.min);
                var sp2 = cam.WorldToScreenPoint(b.max);
                Report.AppendLine($"-- 敌人屏幕矩形 x {sp0.x:0}..{sp2.x:0}  y {sp0.y:0}..{sp2.y:0}（屏幕 1080x2160）");
                var mid = new Vector3((sp0.x + sp2.x) * 0.5f, (sp0.y + sp2.y) * 0.5f);
                RaycastAtScreen(mid, "敌人中心");
            }
        }
        var weapon = FindWeaponRenderer();
        if (weapon != null)
        {
            var t = weapon.transform;
            while (t != null && t.name != "Player") t = t.parent;
            var wrt = t as RectTransform;
            Report.AppendLine($"-- 武器渲染器 {weapon.name} 材质色={(weapon.sharedMaterial != null && weapon.sharedMaterial.HasProperty("_Color") ? weapon.sharedMaterial.GetColor("_Color").grayscale.ToString("0.00") : "-")}");
        }

        // 台词气泡
        var bubble = FindRect("TutorialBubble");
        if (bubble != null)
        {
            var c = bubble.GetComponentInParent<Canvas>();
            Report.AppendLine($"-- 台词气泡 canvas={c.name}(order {c.sortingOrder})");
            RaycastAt(bubble, "台词气泡中心");
        }
        var cont = UnityEngine.Object.FindObjectOfType<TutorialContinueButton>(true);
        if (cont != null && cont.IsShowing)
        {
            var c = ((RectTransform)cont.transform).GetComponentInParent<Canvas>();
            Report.AppendLine($"-- 继续按钮 canvas={c.name}(order {c.sortingOrder})");
            RaycastAt((RectTransform)cont.transform, "继续按钮中心");
        }
    }

    // ── 射线：记录命中顺序 ───────────────────────────────────
    private static void RaycastAtScreen(Vector2 pos, string label)
    {
        var es = EventSystem.current;
        if (es == null) { Report.AppendLine($"-- [{label}] 没有 EventSystem"); return; }
        var ped = new PointerEventData(es) { position = pos };
        var hits = new List<RaycastResult>();
        es.RaycastAll(ped, hits);
        Report.AppendLine($"-- [{label}] 屏幕坐标 {pos} 命中 {hits.Count} 个：");
        for (int i = 0; i < hits.Count && i < 6; i++)
        {
            var go = hits[i].gameObject;
            var c = go.GetComponentInParent<Canvas>();
            Report.AppendLine($"     #{i + 1} {go.name} (canvas {c.name}/{c.sortingOrder}) depth={hits[i].depth:0.00}");
        }
    }

    private static void RaycastAt(RectTransform target, string label)
    {
        var es = EventSystem.current;
        if (es == null) { Report.AppendLine($"-- [{label}] 没有 EventSystem"); return; }
        var canvas = target.GetComponentInParent<Canvas>();
        var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        var pos = RectTransformUtility.WorldToScreenPoint(cam, target.TransformPoint(target.rect.center));
        var ped = new PointerEventData(es) { position = pos };
        var hits = new List<RaycastResult>();
        es.RaycastAll(ped, hits);
        Report.AppendLine($"-- [{label}] 屏幕坐标 {pos} 命中 {hits.Count} 个：");
        for (int i = 0; i < hits.Count && i < 6; i++)
        {
            var go = hits[i].gameObject;
            var c = go.GetComponentInParent<Canvas>();
            var img = go.GetComponent<Graphic>();
            Report.AppendLine($"     #{i + 1} {go.name} (canvas {c.name}/{c.sortingOrder}) depth={hits[i].depth:0.00} graphic={(img == null ? "-" : img.GetType().Name)} raycastTarget={(img == null ? "-" : img.raycastTarget.ToString())}");
        }
    }

    // ── 工具 ────────────────────────────────────────────────
    private static Image FindCurtain()
    {
        foreach (var t in UnityEngine.Object.FindObjectsOfType<Transform>(true))
        {
            if (t.name == "Tutorial Blackout Curtain") return t.GetComponent<Image>();
        }
        return null;
    }

    private static RectTransform FindRect(string name)
    {
        foreach (var r in UnityEngine.Object.FindObjectsOfType<RectTransform>(true)) if (r.name == name) return r;
        return null;
    }

    private static MeshRenderer FindWeaponRenderer()
    {
        foreach (var mr in UnityEngine.Object.FindObjectsOfType<MeshRenderer>(true))
        {
            var t = mr.transform;
            while (t != null) { if (t.name == "Player") return mr; t = t.parent; }
        }
        return null;
    }
}
