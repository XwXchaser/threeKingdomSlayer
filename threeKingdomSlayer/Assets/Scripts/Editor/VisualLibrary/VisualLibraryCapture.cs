using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 视觉库采集器 —— 按 <c>_art_process/VisualLibrary/capture_flow.md</c> 的七步流程批量抓实机截图。
///
/// 为什么要单独一个文件：采集是「跨域重载」的活（进 Play Mode 会重载程序集），
/// 所以任务状态必须落盘（JobFile），跑批逻辑必须能在重载后自动接上（VisualLibraryCaptureRunner）。
///
/// 入口
///   VisualLibraryCapture.Start("shot_a,shot_b")        // 开始采集（会切场景 + 进 Play Mode）
///   VisualLibraryCapture.ReadReport()                   // 读结果报告
///
/// 产物
///   _art_process/VisualLibrary/shots/&lt;shot_id&gt;.png
///   Library/Locus/tmp/visual_library_captured.json       // 待登记清单（交给 claim_shots.py 写 index.csv）
///   Library/Locus/tmp/visual_library_report.json          // 本次运行报告
///
/// 铁律
///   · 一个镜头 = shot_defs.csv 里的一行，临时描述不算镜头。
///   · 动作码未实现 / 抓出纯色图 → 判失败并写明原因，**不产出半成品**。
///   · 不自动跑：必须由人显式调用 Start()。
/// </summary>
public static class VisualLibraryCapture
{
    public const string LibraryRoot = "_art_process/VisualLibrary";
    public const string ShotDefsPath = "_art_process/VisualLibrary/shot_defs.csv";
    public const string ShotsDir = "_art_process/VisualLibrary/shots";
    public const string JobFile = "Library/Locus/tmp/visual_library_job.json";
    public const string CapturedFile = "Library/Locus/tmp/visual_library_captured.json";
    public const string ReportFile = "Library/Locus/tmp/visual_library_report.json";

    public const int CaptureWidth = 1080;
    public const int CaptureHeight = 1920;
    public const int SettleFrames = 60;      // S2 场景就绪
    public const int SetupWaitFrames = 20;   // S3 摆场后
    public const int CameraWaitFrames = 2;   // S4 相机
    public const int CaptureWaitFrames = 3;  // S5 定帧后
    public const int AfterCaptureFrames = 2; // S6→S7

    // ── 声明行 ────────────────────────────────────────────────────────────
    [Serializable]
    public class ShotSpec
    {
        public string shotId = "";
        public string title = "";
        public string scene = "";
        public string camera = "MainCamera";
        public string setup = "none";
        public string capture = "game";
        public string time = "";
        public string tags = "";
        public string crop = "";
        public int maxLongEdge = 1080;
        public string status = "planned";
        public string notes = "";

        public string ScenePath => ProjectRoot() + "/" + scene;

        private static string ProjectRoot()
        {
            var dir = new DirectoryInfo(Application.dataPath);
            return dir.Parent.FullName.Replace("\\", "/");
        }
    }

    // ── 落盘的任务状态（跨域重载） ────────────────────────────────────────
    [Serializable]
    public class JobState
    {
        public string[] shotIds = new string[0];
        public string[] scenes = new string[0];
        public int cursor = 0;
        public string state = "pending";   // pending | running | switch_scene | done
        public int width = CaptureWidth;
        public int height = CaptureHeight;
        public string lastError = "";
    }

    [Serializable]
    public class CaptureRecord
    {
        public string shotId = "";
        public string title = "";
        public string status = "";        // ok | failed
        public string message = "";
        public string file = "";          // 相对工程根
        public string method = "";        // rt | screen
        public int width = 0;
        public int height = 0;
    }

    // ─────────────────────────────────────────────────────────────────────
    // 入口
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>开始采集。shotIdsCsv 为空表示拍 shot_defs.csv 里全部 planned 的镜头。</summary>
    public static string Start(string shotIdsCsv, int width, int height)
    {
        var specs = LoadSpecs();
        if (specs.Count == 0) return "错误：shot_defs.csv 没有可用镜头（" + ShotDefsPath + "）";

        var wanted = (shotIdsCsv ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        if (wanted.Count > 0)
        {
            specs = specs.Where(s => wanted.Contains(s.shotId)).ToList();
            if (specs.Count == 0) return "错误：指定的镜头 id 不存在：" + shotIdsCsv;
        }
        else
        {
            specs = specs.Where(s => s.status != "deprecated").ToList();
        }

        var job = new JobState
        {
            shotIds = specs.Select(s => s.shotId).ToArray(),
            scenes = specs.Select(s => s.scene).ToArray(),
            cursor = 0,
            state = "pending",
            width = width > 0 ? width : CaptureWidth,
            height = height > 0 ? height : CaptureHeight,
        };
        WriteJob(job);

        // 切到第一个镜头的场景（必须在 Edit Mode 做）
        var firstScene = Abs(job.scenes[0]);
        if (!File.Exists(firstScene)) return "错误：场景文件不存在 " + firstScene;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return "已取消：用户未保存当前场景";
        EditorSceneManager.OpenScene(firstScene, OpenSceneMode.Single);

        EditorApplication.EnterPlaymode();
        return "已启动采集：" + string.Join(", ", job.shotIds) + "（场景 " + job.scenes[0] + "）";
    }

    public static string ReadReport()
    {
        var p = Abs(CapturedFile);
        if (!File.Exists(p)) return "尚无采集结果（" + CapturedFile + " 不存在）";
        return File.ReadAllText(p, System.Text.Encoding.UTF8);
    }

    // ─────────────────────────────────────────────────────────────────────
    // 声明表解析
    // ─────────────────────────────────────────────────────────────────────

    public static List<ShotSpec> LoadSpecs()
    {
        var list = new List<ShotSpec>();
        var path = Abs(ShotDefsPath);
        if (!File.Exists(path)) return list;
        var rows = ParseCsv(File.ReadAllText(path, System.Text.Encoding.UTF8));
        foreach (var r in rows)
        {
            if (!r.TryGetValue("shot_id", out var id) || string.IsNullOrWhiteSpace(id)) continue;
            int edge;
            if (!int.TryParse(Get(r, "max_long_edge", "1080"), out edge) || edge <= 0) edge = 1080;
            list.Add(new ShotSpec
            {
                shotId = id.Trim(),
                title = Get(r, "title", ""),
                scene = Get(r, "scene", ""),
                camera = Get(r, "camera", "MainCamera"),
                setup = Get(r, "setup", "none"),
                capture = Get(r, "capture", "game"),
                time = Get(r, "time", ""),
                tags = Get(r, "tags", ""),
                crop = Get(r, "crop", ""),
                maxLongEdge = edge,
                status = Get(r, "status", "planned"),
                notes = Get(r, "notes", ""),
            });
        }
        return list;
    }

    // ─────────────────────────────────────────────────────────────────────
    // 运行器（域重载后自动接上）
    // ─────────────────────────────────────────────────────────────────────

    [InitializeOnLoad]
    public static class Runner
    {
        private static int _phase;             // 0 稳定 1 摆场等待 2 相机等待 3 抓取等待 4 抓取后
        private static int _phaseStartFrame;
        private static int _lastGameFrame = -1;
        private static int _playEnteredFrame = -1;
        private static bool _fallbackPending;
        private static string _fallbackShotId = "";
        private static Camera _shotCamera;
        private static Vector3 _camPos;
        private static Quaternion _camRot;

        static Runner()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                _phase = 0;
                _phaseStartFrame = Time.frameCount;
                _playEnteredFrame = Time.frameCount;
                _lastGameFrame = -1;
            }
            else if (change == PlayModeStateChange.ExitingPlayMode)
            {
                var job = ReadJob();
                if (job == null) return;
                if (job.state == "switch_scene")
                {
                    // 换场景继续下一组
                    job.cursor = NextSceneCursor(job);
                    if (job.cursor < job.shotIds.Length)
                    {
                        job.state = "pending";
                        WriteJob(job);
                        var scene = Abs(job.scenes[job.cursor]);
                        if (File.Exists(scene))
                        {
                            EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
                            EditorApplication.EnterPlaymode();
                        }
                        else
                        {
                            job.state = "done";
                            job.lastError = "场景不存在：" + job.scenes[job.cursor];
                            WriteJob(job);
                            Finish(job);
                        }
                    }
                    else
                    {
                        job.state = "done";
                        WriteJob(job);
                        Finish(job);
                    }
                }
                else if (job.state == "done")
                {
                    Finish(job);
                }
            }
        }

        /// <summary>推进到下一个镜头；遇到换场景则退 Play Mode 再进。</summary>
        private static void Advance(JobState job)
        {
            job.cursor++;
            WriteJob(job);
            if (job.cursor >= job.shotIds.Length)
            {
                job.state = "done";
                WriteJob(job);
                EditorApplication.ExitPlaymode();
                return;
            }
            if (job.scenes[job.cursor] != job.scenes[job.cursor - 1])
            {
                job.state = "switch_scene";
                WriteJob(job);
                EditorApplication.ExitPlaymode();
            }
        }

        private static int NextSceneCursor(JobState job)
        {
            var cur = job.scenes[job.cursor];
            for (int i = job.cursor + 1; i < job.scenes.Length; i++)
                if (job.scenes[i] != cur) return i;
            return job.shotIds.Length;
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            var job = ReadJob();
            if (job == null || job.state == "done") return;
            if (job.state == "pending") { job.state = "running"; WriteJob(job); }

            if (Time.frameCount == _lastGameFrame) return;   // 一游戏帧处理一次
            _lastGameFrame = Time.frameCount;

            var specs = LoadSpecs();
            if (job.cursor >= job.shotIds.Length)
            {
                job.state = "done";
                WriteJob(job);
                EditorApplication.ExitPlaymode();
                return;
            }
            var spec = specs.FirstOrDefault(s => s.shotId == job.shotIds[job.cursor]);
            if (spec == null)
            {
                AddRecord(new CaptureRecord { shotId = job.shotIds[job.cursor], status = "failed", message = "shot_defs.csv 里找不到该镜头" });
                job.cursor++;
                WriteJob(job);
                return;
            }

            int frames = Time.frameCount - _phaseStartFrame;

            switch (_phase)
            {
                case 0:
                    if (Time.frameCount - _playEnteredFrame < SettleFrames) return;
                    {
                        VisualLibraryCapture.ResetShotState();
                        var err = VisualLibraryCapture.ApplySetup(spec.setup);
                        if (err != null)
                        {
                            AddRecord(new CaptureRecord { shotId = spec.shotId, title = spec.title, status = "failed", message = err });
                            Advance(job);
                            return;
                        }
                        _shotCamera = VisualLibraryCapture.ResolveCamera(spec.camera);
                        if (_shotCamera != null)
                        {
                            _camPos = _shotCamera.transform.position;
                            _camRot = _shotCamera.transform.rotation;
                        }
                        _phase = 1;
                        _phaseStartFrame = Time.frameCount;
                    }
                    break;

                case 1:
                    if (frames < SetupWaitFrames) return;
                    VisualLibraryCapture.ApplyCamera(spec.camera, _shotCamera, ref _camPos, ref _camRot);
                    // 抓前冻结时间 → 画面确定，不会边抓边动
                    Time.timeScale = 0f;
                    _phase = 2;
                    _phaseStartFrame = Time.frameCount;
                    break;

                case 2:
                    if (frames < CameraWaitFrames) return;
                    _phase = 3;
                    _phaseStartFrame = Time.frameCount;
                    break;

                case 3:
                    if (frames < CaptureWaitFrames) return;
                    {
                        var rec = VisualLibraryCapture.CaptureShot(spec, _shotCamera, job.width, job.height);
                        if (rec.status == "failed" && rec.method == "rt")
                        {
                            // RT 抓出纯色 → 下帧改用 ScreenCapture 兜底
                            _fallbackPending = true;
                            _fallbackShotId = spec.shotId;
                            _phase = 5;
                            _phaseStartFrame = Time.frameCount;
                            return;
                        }
                        AddRecord(rec);
                        Advance(job);
                        _phase = 0;
                        _phaseStartFrame = Time.frameCount;
                    }
                    break;

                case 5:
                    if (frames < 2) return;
                    {
                        var spec2 = specs.FirstOrDefault(s => s.shotId == _fallbackShotId);
                        var rec = spec2 != null
                            ? VisualLibraryCapture.CaptureShotScreen(spec2, job.width, job.height)
                            : new CaptureRecord { shotId = _fallbackShotId, status = "failed", message = "兜底重抓时找不到镜头声明" };
                        AddRecord(rec);
                        Advance(job);
                        _fallbackPending = false;
                        _phase = 0;
                        _phaseStartFrame = Time.frameCount;
                    }
                    break;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // 镜头间重置
    // ─────────────────────────────────────────────────────────────────────

    private static readonly List<GameObject> _created = new List<GameObject>();

    /// <summary>
    /// 每个镜头开始前重置现场：销毁上一个镜头留下的对象（含游戏自己弹的三选一）并恢复时间流速。
    /// 不做这一步，上一个镜头的弹窗会泄到下一个镜头里（实测踩过）。
    /// </summary>
    public static void ResetShotState()
    {
        Time.timeScale = 1f;

        foreach (var go in _created)
        {
            if (go != null) UnityEngine.Object.Destroy(go);
        }
        _created.Clear();

        foreach (var p in UnityEngine.Object.FindObjectsOfType<UpgradeChoicePopup>(true))
        {
            if (p != null) UnityEngine.Object.Destroy(p.gameObject);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // 动作码
    // ─────────────────────────────────────────────────────────────────────

    public static string ApplySetup(string setup)
    {
        if (string.IsNullOrWhiteSpace(setup) || setup.Trim() == "none") return null;
        foreach (var raw in setup.Split('+'))
        {
            var act = raw.Trim();
            if (act.Length == 0) continue;
            var err = ApplyAction(act);
            if (err != null) return err;
        }
        return null;
    }

    private static string ApplyAction(string act)
    {
        var parts = act.Split(new[] { ':' }, 2);
        var code = parts[0].Trim();
        var arg = parts.Length > 1 ? parts[1].Trim() : "";

        switch (code)
        {
            case "none":
                return null;

            case "clear":
                ClearEnemies();
                return null;

            case "freeze":
                Time.timeScale = 0f;
                return null;

            case "unfreeze":
                Time.timeScale = 1f;
                return null;

            case "spawn":
                return SpawnEnemy(arg);

            case "upgrade_choice":
                return ShowUpgradeChoice(arg);

            default:
                return "动作码未实现：" + code;
        }
    }

    private static void ClearEnemies()
    {
        foreach (var e in UnityEngine.Object.FindObjectsOfType<Enemy>(true))
            UnityEngine.Object.Destroy(e.gameObject);
    }

    private static string SpawnEnemy(string name)
    {
        if (string.IsNullOrEmpty(name)) return "spawn 缺少参数";
        var path = "Assets/Resources/EnemyPrefabs/" + name + ".prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return "找不到敌人 prefab：" + path;
        var cam = Camera.main;
        var pos = cam != null ? cam.transform.position + cam.transform.forward * 6f : Vector3.zero;
        var go = UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity);
        _created.Add(go);
        return null;
    }

    /// <summary>弹三选一。参数形如 active_cyclone|active_arrow_rain|active_wave；留空则挑 3 个带图标的定义。</summary>
    private static string ShowUpgradeChoice(string arg)
    {
        var mgr = UnityEngine.Object.FindObjectOfType<UpgradeChoiceManager>();
        if (mgr == null) return "场景里没有 UpgradeChoiceManager";
        if (mgr.popupPrefab == null) return "UpgradeChoiceManager.popupPrefab 未配置";

        var defs = ResolveDefinitions(arg);
        if (defs.Count == 0) return "找不到 UpgradeDefinition：" + (string.IsNullOrEmpty(arg) ? "(自动挑选也没找到带图标的)" : arg);

        var go = UnityEngine.Object.Instantiate(mgr.popupPrefab);
        _created.Add(go);
        var view = go.GetComponent<IUpgradeChoiceView>();
        if (view == null)
        {
            UnityEngine.Object.Destroy(go);
            return "popupPrefab 上找不到 IUpgradeChoiceView 实现";
        }
        view.ShowChoices(defs);
        return null;
    }

    private static List<UpgradeDefinition> ResolveDefinitions(string arg)
    {
        var all = new List<UpgradeDefinition>();
        foreach (var g in AssetDatabase.FindAssets("t:UpgradeDefinition"))
        {
            var def = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(AssetDatabase.GUIDToAssetPath(g));
            if (def != null) all.Add(def);
        }

        var result = new List<UpgradeDefinition>();
        if (!string.IsNullOrWhiteSpace(arg))
        {
            foreach (var id in arg.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var d = all.FirstOrDefault(x => x.upgradeId == id.Trim());
                if (d != null) result.Add(d);
            }
            return result;
        }

        // 自动挑：优先主动技能（active_*），其次任意带图标的
        foreach (var d in all.Where(x => x.upgradeId.StartsWith("active_") && x.icon != null).OrderBy(x => x.upgradeId))
            result.Add(d);
        if (result.Count < 3)
        {
            foreach (var d in all.Where(x => x.icon != null && !result.Contains(x)).OrderBy(x => x.upgradeId))
                result.Add(d);
        }
        return result.Take(3).ToList();
    }

    // ─────────────────────────────────────────────────────────────────────
    // 相机
    // ─────────────────────────────────────────────────────────────────────

    public static Camera ResolveCamera(string spec)
    {
        var name = string.IsNullOrWhiteSpace(spec) ? "MainCamera" : spec.Split(new[] { ':' })[0].Trim();
        if (name == "MainCamera")
        {
            var main = Camera.main;
            if (main != null) return main;
            // 纯 UI 场景（TalentTree / StageSelect）的 Main Camera 在场景里是关着的，
            // Camera.main 拿不到 → 找任意相机并临时激活，抓完还原。
            var any = UnityEngine.Object.FindObjectsOfType<Camera>(true).FirstOrDefault();
            if (any != null)
            {
                _activatedCamera = any;
                _cameraWasActive = any.gameObject.activeSelf;
                if (!_cameraWasActive)
                {
                    any.gameObject.SetActive(true);
                    // 场景里本来就没开相机（纯 UI 场景），玩家实际看到的是黑的 → 强制黑底
                    _forceBlackBackground = true;
                }
            }
            return any;
        }
        var named = UnityEngine.Object.FindObjectsOfType<Camera>(true).FirstOrDefault(c => c.name == name);
        if (named != null && !named.gameObject.activeSelf)
        {
            _activatedCamera = named;
            _cameraWasActive = false;
            named.gameObject.SetActive(true);
            _forceBlackBackground = true;
        }
        return named;
    }

    private static Camera _activatedCamera;
    private static bool _cameraWasActive;

    /// <summary>相机是不是我们临时激活的（这种相机的清屏色不代表玩家实际看到的画面）。</summary>
    private static bool _forceBlackBackground;

    /// <summary>抓完还原被临时激活的相机。</summary>
    public static void RestoreCameraState()
    {
        if (_activatedCamera == null) return;
        if (!_cameraWasActive) _activatedCamera.gameObject.SetActive(false);
        _activatedCamera = null;
        _forceBlackBackground = false;
    }

    public static void ApplyCamera(string spec, Camera cam, ref Vector3 pos, ref Quaternion rot)
    {
        if (cam == null) return;
        cam.transform.position = pos;
        cam.transform.rotation = rot;
    }

    // ─────────────────────────────────────────────────────────────────────
    // 抓图
    // ─────────────────────────────────────────────────────────────────────

    public static CaptureRecord CaptureShot(ShotSpec spec, Camera cam, int width, int height)
    {
        var rec = new CaptureRecord { shotId = spec.shotId, title = spec.title };
        if (cam == null)
        {
            rec.status = "failed";
            rec.message = "找不到相机：" + spec.camera;
            return rec;
        }

        Texture2D tex = null;
        string method = "rt";
        var why = "";
        try
        {
            tex = CaptureViaRenderTexture(cam, width, height);
        }
        catch (Exception e)
        {
            rec.message = "RT 抓取异常：" + e.Message;
        }

        if (tex == null || IsUniform(tex, out why))
        {
            if (tex != null) UnityEngine.Object.DestroyImmediate(tex);
            rec.status = "failed";
            rec.method = "rt";
            rec.message = string.IsNullOrEmpty(rec.message) ? ("抓出纯色图（" + why + "），需改用 ScreenCapture 兜底") : rec.message;
            RestoreCameraState();
            return rec;
        }

        var path = Abs(LibraryRoot + "/shots/" + spec.shotId + ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, tex.EncodeToPNG());
        rec.status = "ok";
        rec.method = method;
        rec.file = LibraryRoot + "/shots/" + spec.shotId + ".png";
        rec.width = tex.width;
        rec.height = tex.height;
        UnityEngine.Object.DestroyImmediate(tex);
        RestoreCameraState();
        return rec;
    }

    public static CaptureRecord CaptureShotScreen(ShotSpec spec, int width, int height)
    {
        var rec = new CaptureRecord { shotId = spec.shotId, title = spec.title, method = "screen" };
        Texture2D tex = null;
        try
        {
            tex = ScreenCapture.CaptureScreenshotAsTexture();
        }
        catch (Exception e)
        {
            rec.status = "failed";
            rec.message = "ScreenCapture 异常：" + e.Message;
            RestoreCameraState();
            return rec;
        }
        if (tex == null)
        {
            rec.status = "failed";
            rec.message = "ScreenCapture 返回空";
            RestoreCameraState();
            return rec;
        }
        if (IsUniform(tex, out var why))
        {
            rec.status = "failed";
            rec.message = "ScreenCapture 也抓出纯色图（" + why + "）";
            UnityEngine.Object.DestroyImmediate(tex);
            RestoreCameraState();
            return rec;
        }
        var path = Abs(LibraryRoot + "/shots/" + spec.shotId + ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, tex.EncodeToPNG());
        rec.status = "ok";
        rec.file = LibraryRoot + "/shots/" + spec.shotId + ".png";
        rec.width = tex.width;
        rec.height = tex.height;
        UnityEngine.Object.DestroyImmediate(tex);
        return rec;
    }

    private struct CanvasState
    {
        public Canvas canvas;
        public RenderMode mode;
        public Camera cam;
        public float distance;
    }

    /// <summary>相机 → RenderTexture。Overlay 的 Canvas 临时切成 ScreenSpaceCamera，抓完还原。</summary>
    private static Texture2D CaptureViaRenderTexture(Camera cam, int width, int height)
    {
        var saved = new List<CanvasState>();
        foreach (var c in UnityEngine.Object.FindObjectsOfType<Canvas>(true))
        {
            if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            saved.Add(new CanvasState { canvas = c, mode = c.renderMode, cam = c.worldCamera, distance = c.planeDistance });
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = cam;
            c.planeDistance = Mathf.Max(0.35f, cam.nearClipPlane + 0.05f);
        }

        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
        var prevTarget = cam.targetTexture;
        var prevActive = RenderTexture.active;
        var prevFlags = cam.clearFlags;
        var prevBg = cam.backgroundColor;
        Texture2D tex = null;
        try
        {
            if (_forceBlackBackground)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
            }
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            RenderTexture.active = rt;
            tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
        }
        finally
        {
            RenderTexture.active = prevActive;
            cam.targetTexture = prevTarget;
            cam.clearFlags = prevFlags;
            cam.backgroundColor = prevBg;
            foreach (var s in saved)
            {
                s.canvas.renderMode = s.mode;
                s.canvas.worldCamera = s.cam;
                s.canvas.planeDistance = s.distance;
            }
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }
        return tex;
    }

    /// <summary>纯色检测：抽样求像素极差，低于阈值判定为"没抓到东西"。</summary>
    private static bool IsUniform(Texture2D tex, out string why)
    {
        why = "";
        var px = tex.GetPixels32();
        int step = Math.Max(1, Math.Min(tex.width, tex.height) / 64);
        byte min = 255, max = 0;
        for (int y = 0; y < tex.height; y += step)
        {
            for (int x = 0; x < tex.width; x += step)
            {
                var p = px[y * tex.width + x];
                byte l = (byte)Math.Min(255, (p.r * 299 + p.g * 587 + p.b * 114) / 1000);
                if (l < min) min = l;
                if (l > max) max = l;
            }
        }
        if (max - min < 6)
        {
            why = string.Format("亮度极差仅 {0}", max - min);
            return true;
        }
        return false;
    }

    // ─────────────────────────────────────────────────────────────────────
    // 落盘
    // ─────────────────────────────────────────────────────────────────────

    [Serializable]
    public class CaptureList
    {
        public CaptureRecord[] items = new CaptureRecord[0];
    }

    public static void AddRecord(CaptureRecord rec)
    {
        var list = ReadCaptured();
        list.RemoveAll(r => r.shotId == rec.shotId);
        list.Add(rec);
        var wrapper = new CaptureList { items = list.ToArray() };
        File.WriteAllText(Abs(CapturedFile), JsonUtility.ToJson(wrapper, true), System.Text.Encoding.UTF8);
    }

    public static List<CaptureRecord> ReadCaptured()
    {
        var p = Abs(CapturedFile);
        if (!File.Exists(p)) return new List<CaptureRecord>();
        try
        {
            var wrapper = JsonUtility.FromJson<CaptureList>(File.ReadAllText(p, System.Text.Encoding.UTF8));
            return wrapper != null && wrapper.items != null ? wrapper.items.ToList() : new List<CaptureRecord>();
        }
        catch { return new List<CaptureRecord>(); }
    }

    public static void WriteJob(JobState job)
    {
        File.WriteAllText(Abs(JobFile), JsonUtility.ToJson(job, true), System.Text.Encoding.UTF8);
    }

    public static JobState ReadJob()
    {
        var p = Abs(JobFile);
        if (!File.Exists(p)) return null;
        try { return JsonUtility.FromJson<JobState>(File.ReadAllText(p, System.Text.Encoding.UTF8)); }
        catch { return null; }
    }

    private static void Finish(JobState job)
    {
        var recs = ReadCaptured();
        var wrapper = new CaptureList { items = recs.ToArray() };
        File.WriteAllText(Abs(ReportFile), JsonUtility.ToJson(wrapper, true), System.Text.Encoding.UTF8);
        Debug.Log("[VisualLibrary] 采集结束：共 " + recs.Count + " 条结果 → " + CapturedFile);
    }

    // ─────────────────────────────────────────────────────────────────────
    // 小工具
    // ─────────────────────────────────────────────────────────────────────

    public static string Abs(string rel)
    {
        var root = new DirectoryInfo(Application.dataPath).Parent.FullName.Replace("\\", "/");
        return rel.StartsWith("/") ? root + rel : root + "/" + rel;
    }

    private static string Get(Dictionary<string, string> row, string key, string fallback)
    {
        string v;
        return row.TryGetValue(key, out v) && !string.IsNullOrEmpty(v) ? v : fallback;
    }

    public static List<Dictionary<string, string>> ParseCsv(string text)
    {
        var rows = new List<Dictionary<string, string>>();
        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        List<string> header = null;
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cells = SplitCsvLine(line);
            if (header == null) { header = cells; continue; }
            var row = new Dictionary<string, string>();
            for (int i = 0; i < header.Count; i++)
                row[header[i]] = i < cells.Count ? cells[i] : "";
            rows.Add(row);
        }
        return rows;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var cells = new List<string>();
        var cur = new System.Text.StringBuilder();
        bool inQuote = false;
        for (int i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (inQuote)
            {
                if (ch == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; }
                    else inQuote = false;
                }
                else cur.Append(ch);
            }
            else if (ch == '"') inQuote = true;
            else if (ch == ',') { cells.Add(cur.ToString()); cur.Clear(); }
            else cur.Append(ch);
        }
        cells.Add(cur.ToString());
        return cells;
    }
}
