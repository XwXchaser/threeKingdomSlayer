#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Android 打包入口（显式 BuildOptions）。
///
/// 背景：Tuanjie 在 BuildOptions 里追加了 PackClustersIntoPlayer(=134217728) 资源集群打包。
/// 开启时游戏数据会被打成单个 UnityFS 集群包 assets/bin/Data/data.tj3d，而当前引擎的 Player
/// 会按传统分文件路径去读 assets/bin/Data/levelN，导致真机/模拟器上进入第二张场景时报
/// "The file .../level1 is corrupted! [Position out of bounds!]" 并 abort。
/// 本入口显式传 BuildOptions.None，并在打包后自检产物布局。
///
/// 用法：菜单 Tools/打包/Android（ARM64+X86，传统布局）
/// 产物：Build/Android/threeKingdomSlayer_&lt;时间戳&gt;.apk
/// 状态：Build/Android/build-status.txt（含 result / errors / 布局自检结论）
/// </summary>
public static class AndroidBuildMenu
{
    const string OutputDir = "Build/Android";

    [MenuItem("Tools/打包/Android（ARM64+X86，传统布局）")]
    public static void BuildAndroidTraditionalLayout()
    {
        var scenes = EditorBuildSettings.scenes
            .Where(s => s != null && s.enabled && !string.IsNullOrEmpty(s.path))
            .Select(s => s.path)
            .ToArray();
        if (scenes.Length == 0) { Debug.LogError("[打包] Build Settings 里没有启用的场景"); return; }
        RunBuild(scenes, "full");
    }

    /// <summary>
    /// 诊断：只把 Battle 作为唯一场景打包。用于判定崩溃是"引擎预加载 Battle 场景数据本身"
    /// 还是"作为第二场景加载"的问题 —— 打包期间临时改 Build Settings，结束后自动恢复。
    /// </summary>
    [MenuItem("Tools/打包/诊断：只打 Battle 单场景")]
    public static void DiagnosticBattleOnly()
    {
        var original = EditorBuildSettings.scenes;
        try
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Battle.scene", true) };
            RunBuild(new[] { "Assets/Scenes/Battle.scene" }, "diag_battleonly");
        }
        finally
        {
            EditorBuildSettings.scenes = original;
            AssetDatabase.SaveAssets();
            Debug.Log("[打包] Build Settings 已恢复为：" + string.Join(" | ", original.Select(s => s.path)));
        }
    }

    /// <summary>诊断：只打一个最小空场景，用于判定“引擎/机器”还是“工程内容”问题。</summary>
    [MenuItem("Tools/打包/诊断：只打空场景 EmptyDiag")]
    public static void DiagnosticEmptyScene()
    {
        const string scene = "Assets/Experiments/CurvedScroll/Diagnostics/EmptyDiag.unity";
        if (!System.IO.File.Exists(scene)) { Debug.LogError("[打包] 找不到空场景：" + scene); return; }
        var original = EditorBuildSettings.scenes;
        try
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scene, true) };
            RunBuild(new[] { scene }, "diag_empty");
        }
        finally
        {
            EditorBuildSettings.scenes = original;
            AssetDatabase.SaveAssets();
            Debug.Log("[打包] Build Settings 已恢复：" + string.Join(" | ", original.Select(s => s.path)));
        }
    }

    /// <summary>诊断：构建二分变体场景（由 Locus 侧生成 Assets/Experiments/CurvedScroll/Diagnostics/Bisect.unity）。</summary>
    [MenuItem("Tools/打包/诊断：构建 Bisect 变体")]
    public static void DiagnosticBisectVariant()
    {
        const string scene = "Assets/Experiments/CurvedScroll/Diagnostics/Bisect.unity";
        if (!System.IO.File.Exists(scene)) { Debug.LogError("[打包] 找不到二分变体场景：" + scene); return; }
        var original = EditorBuildSettings.scenes;
        try
        {
            // 诊断包必须同时包含 Y 卷轴场景（主流程会 additive 加载它）
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(scene, true),
                new EditorBuildSettingsScene("Assets/Experiments/CurvedScroll/YJunctionSample.unity", true)
            };
            RunBuild(new[] { scene, "Assets/Experiments/CurvedScroll/YJunctionSample.unity" }, "diag_bisect");
        }
        finally
        {
            EditorBuildSettings.scenes = original;
            AssetDatabase.SaveAssets();
            Debug.Log("[打包] Build Settings 已恢复：" + string.Join(" | ", original.Select(s => s.path)));
        }
    }

    static void RunBuild(string[] scenes, string tag)
    {
        Directory.CreateDirectory(OutputDir);
        string outPath = Path.Combine(OutputDir, "threeKingdomSlayer_" + tag + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".apk");
        string statusPath = Path.Combine(OutputDir, "build-status.txt");

        var log = new StringBuilder();
        log.AppendLine("started " + DateTime.Now.ToString("o"));
        log.AppendLine("target=" + EditorUserBuildSettings.activeBuildTarget + " arch=" + PlayerSettings.Android.targetArchitectures
            + " development=" + EditorUserBuildSettings.development + " appBundle=" + EditorUserBuildSettings.buildAppBundle);
        log.AppendLine("scenes=" + string.Join(",", scenes));
        log.AppendLine("options=BuildOptions.None（不含 PackClustersIntoPlayer=" + (int)BuildOptions.PackClustersIntoPlayer + "）");
        log.AppendLine("output=" + outPath);

        var opts = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(opts);
        BuildSummary sum = report.summary;
        log.AppendLine("RESULT=" + sum.result + " errors=" + sum.totalErrors + " warnings=" + sum.totalWarnings
            + " bytes=" + sum.totalSize + " time=" + sum.totalTime);

        foreach (BuildStep step in report.steps)
            foreach (BuildStepMessage msg in step.messages)
                if (msg.type == LogType.Error || msg.type == LogType.Exception)
                    log.AppendLine("[" + step.name + "] " + msg.content);

        if (sum.result == BuildResult.Succeeded && File.Exists(outPath))
            log.Append(CheckApkLayout(outPath));

        File.WriteAllText(statusPath, log.ToString());
        if (sum.result == BuildResult.Succeeded)
            Debug.Log("[打包] 完成：" + outPath + "\n" + log);
        else
            Debug.LogError("[打包] 失败，详见 " + statusPath + "\n" + log);
    }

    /// <summary>产物布局自检：传统 level* 分文件布局 = 可运行；只有 data.tj3d = 集群包（会崩）。</summary>
    static string CheckApkLayout(string apkPath)
    {
        var sb = new StringBuilder();
        try
        {
            using (var fs = File.OpenRead(apkPath))
            using (var za = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                int total = 0, levels = 0, managed = 0;
                bool hasTj3d = false;
                string abis = "";
                var abiSet = new System.Collections.Generic.SortedSet<string>();
                foreach (var e in za.Entries)
                {
                    total++;
                    string n = e.FullName.Replace('\\', '/');
                    if (n.StartsWith("lib/") && n.Count(c => c == '/') >= 2) abiSet.Add(n.Split('/')[1]);
                    if (n.StartsWith("assets/bin/Data/level")) levels++;
                    if (n.StartsWith("assets/bin/Data/Managed/")) managed++;
                    if (n.IndexOf("data.tj3d", StringComparison.OrdinalIgnoreCase) >= 0) hasTj3d = true;
                }
                abis = string.Join(",", abiSet);
                sb.AppendLine("APK 布局自检：条目=" + total + " level*=" + levels + " Managed=" + managed + " data.tj3d=" + hasTj3d + " ABI=" + abis);
                if (hasTj3d && levels == 0)
                    sb.AppendLine("布局结论=❌ 集群包（data.tj3d）：当前引擎 Player 读不了，装到设备上会在加载第二张场景时 abort");
                else if (levels > 0 && !hasTj3d)
                    sb.AppendLine("布局结论=✅ 传统分文件布局（含 level0..levelN）");
                else
                    sb.AppendLine("布局结论=⚠️ 未识别（既有 level* 又有 data.tj3d）");
            }
        }
        catch (Exception e)
        {
            sb.AppendLine("APK 布局自检失败：" + e.Message);
        }
        return sb.ToString();
    }
}
#endif
