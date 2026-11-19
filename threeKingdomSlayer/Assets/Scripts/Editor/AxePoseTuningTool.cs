using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Locus.EditorTools
{
    /// <summary>
    /// 抡斧劈砍**位姿对照工具**（临时调校用）：
    /// 直接实例化 Axe3D.prefab（走真实 transform 链，不手工拼矩阵），
    /// 按运行时的 <see cref="AxeSmashPose"/> 数学摆位，并画出：
    ///   · 红框 = 第一排敌人的胸口落点
    ///   · 绿十字 = 斧头的 Tip 锚点（刃尖）
    /// 菜单：Tools/测试/抡斧/位姿对照图
    /// </summary>
    public static class AxePoseTuningTool
    {
        private const string PREFAB = "Assets/Prefabs/SkillWeapons/Axe3D.prefab";
        private const string OUT_DIR = "Library/Locus/tmp/axe-unity-check";
        private const int W = 489, H = 979;

        [MenuItem("Tools/测试/抡斧/位姿对照图", false, 30)]
        public static void BuildSheet()
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB);
            if (pf == null) { Debug.LogError("[AxePose] prefab missing"); return; }
            var cam = Camera.main;

            // ── 第一排敌人的基准（与运行时同一套算法）──
            Vector3 enemyBase = FirstRowEnemyPosition(out string info);
            Vector3 impactPoint = AxeSmashPose.ImpactPoint(enemyBase);
            Debug.Log($"[AxePose] {info} enemyBase={enemyBase} impactPoint={impactPoint} screen={ScreenOf(cam, impactPoint)}");

            // ── 实例化 prefab 读锚点 ──
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(pf);
            var vis = inst.GetComponentInChildren<MeshWeaponVisual>(true);
            Vector3 tipLocal = vis != null ? vis.TipPosition : Vector3.zero;
            Debug.Log($"[AxePose] 锚点(局部)：Tip={tipLocal}");

            // ── 三帧（美术 2026-11-15 摆定）× 两档缩放（运行时 2 / 旧版 1.28 对照）──
            var frames = new (string tag, Vector3 grip, Quaternion rot)[]
            {
                ("起手Start", AxeSmashPose.StartGrip(), AxeSmashPose.StartRot),
                ("最高Apex", AxeSmashPose.ApexGrip(), AxeSmashPose.ApexRot),
                ("命中End", AxeSmashPose.EndGrip(), AxeSmashPose.EndRot),
            };
            float[] scales = { AxeSmashPose.RuntimeScale, 1.28f };
            var sheet = new Texture2D(W * 3, H * 2, TextureFormat.RGBA32, false);
            for (int row = 0; row < scales.Length; row++)
            {
                for (int c = 0; c < 3; c++)
                {
                    var f = frames[c];
                    inst.transform.position = f.grip;
                    inst.transform.rotation = f.rot;
                    inst.transform.localScale = Vector3.one * scales[row];
                    RenderCell(cam, inst, impactPoint, tipLocal, sheet, c * W, row * H);
                    Vector3 tipWorld = inst.transform.TransformPoint(tipLocal);
                    Debug.Log($"[AxePose] {f.tag} scale={scales[row]} grip={f.grip} | 刃尖={tipWorld} screen={ScreenOf(cam, tipWorld)} | 离落点={Vector3.Distance(tipWorld, impactPoint):F2}");
                }
            }
            Object.DestroyImmediate(inst);

            Directory.CreateDirectory(OUT_DIR);
            File.WriteAllBytes(OUT_DIR + "/pose_sheet.png", sheet.EncodeToPNG());
            Debug.Log("[AxePose] wrote " + OUT_DIR + "/pose_sheet.png");
        }

        [MenuItem("Tools/测试/抡斧/动画分解图（逐帧）", false, 32)]
        public static void BuildAnimationStrip()
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB);
            if (pf == null) { Debug.LogError("[AxePose] prefab missing"); return; }
            var cam = Camera.main;
            Vector3 enemyBase = FirstRowEnemyPosition(out string info);
            Vector3 impactPoint = AxeSmashPose.ImpactPoint(enemyBase);

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(pf);
            var vis = inst.GetComponentInChildren<MeshWeaponVisual>(true);
            Vector3 tipLocal = vis != null ? vis.TipPosition : Vector3.zero;

            Vector3 startGrip = AxeSmashPose.StartGrip();
            Vector3 apexGrip = AxeSmashPose.ApexGrip();
            Vector3 endGrip = AxeSmashPose.EndGrip();
            Quaternion startRot = AxeSmashPose.StartRot;
            Quaternion apexRot = AxeSmashPose.ApexRot;
            Quaternion endRot = AxeSmashPose.EndRot;

            // 按运行时的五段时序采样：蓄势 → 绷住 → 劈下 → 停顿 → 渐变消失（原地）
            float wu = AxeSmashPose.WindUpSeconds;
            float hold = AxeSmashPose.ApexHoldSeconds;
            float chop = AxeSmashPose.ChopSeconds;
            float stop = AxeSmashPose.HitStopSeconds;
            float fol = AxeSmashPose.FollowThroughSeconds;
            float[] times = { 0f, wu * 0.5f, wu, wu + hold * 0.5f, wu + hold + chop * 0.5f,
                              wu + hold + chop, wu + hold + chop + stop, wu + hold + chop + stop + fol * 0.5f,
                              wu + hold + chop + stop + fol };
            int cols = times.Length;
            var sheet = new Texture2D(W * cols, H, TextureFormat.RGBA32, false);
            for (int i = 0; i < cols; i++)
            {
                float t = times[i];
                Vector3 grip; Quaternion rot;
                if (t <= wu)
                {
                    grip = Vector3.Lerp(startGrip, apexGrip, AxeSmashPose.EaseWindUpPos(t / wu));
                    rot = Quaternion.Slerp(startRot, apexRot, AxeSmashPose.EaseWindUpRot(t / wu));
                }
                else if (t <= wu + hold)
                {
                    grip = apexGrip; rot = apexRot;
                }
                else if (t <= wu + hold + chop)
                {
                    float k = (t - wu - hold) / chop;
                    grip = Vector3.Lerp(apexGrip, endGrip, AxeSmashPose.EaseChopPos(k));
                    rot = Quaternion.Slerp(apexRot, endRot, AxeSmashPose.EaseChopRot(k));
                }
                else if (t <= wu + hold + chop + stop)
                {
                    grip = endGrip; rot = endRot;
                }
                else
                {
                    // ⑤ 渐变消失：**原地不动**，只淡出
                    grip = endGrip; rot = endRot;
                }
                inst.transform.position = grip;
                inst.transform.rotation = rot;
                inst.transform.localScale = Vector3.one * AxeSmashPose.RuntimeScale;
                RenderCell(cam, inst, impactPoint, tipLocal, sheet, i * W, 0);
                Vector3 tipWorld = inst.transform.TransformPoint(tipLocal);
                Debug.Log($"[AxePose] 帧 t={t:F3}s grip={grip} tipScreen={ScreenOf(cam, tipWorld)}");
            }
            Object.DestroyImmediate(inst);
            Directory.CreateDirectory(OUT_DIR);
            File.WriteAllBytes(OUT_DIR + "/anim_strip.png", sheet.EncodeToPNG());
            Debug.Log("[AxePose] wrote " + OUT_DIR + "/anim_strip.png  " + info);
        }

        [MenuItem("Tools/测试/抡斧/重测斧刃入地点", false, 36)]
        public static void MeasureAxeLowestVertex()
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB);
            if (pf == null) { Debug.LogError("[AxePose] prefab missing"); return; }
            var model = pf.transform.Find("Model");
            var mf = model != null ? model.GetComponent<MeshFilter>() : null;
            if (mf == null || mf.sharedMesh == null) { Debug.LogError("[AxePose] 找不到 mesh"); return; }
            // Model → 斧根节点 的局部变换（预制体里二者在同一空间）
            Matrix4x4 toRoot = model.localToWorldMatrix * pf.transform.worldToLocalMatrix;
            float minY = float.MaxValue; Vector3 lowest = Vector3.zero;
            foreach (var v in mf.sharedMesh.vertices)
            {
                Vector3 p = toRoot.MultiplyPoint3x4(v);
                if (p.y < minY) { minY = p.y; lowest = p; }
            }
            Debug.Log($"[AxePose] 斧 mesh 最低点（斧根局部空间）= {lowest:F5}\n" +
                      $"  ⇒ 粘到 AxeSmashPose.AxeLowestVertexRootLocal：new Vector3({lowest.x:F5}f, {lowest.y:F5}f, {lowest.z:F5}f)");
            AxeSmashPose.AxeLowestVertexRootLocal = lowest;   // 本次会话内立即生效
        }

        private static Vector3 FirstRowEnemyPosition(out string info)
        {
            var sc = Object.FindObjectOfType<StageController>();
            var enemiesRoot = GameObject.Find("Enemies");
            float spacing = sc != null ? sc.GetRowSpacing() : 2.5f;
            int maxRows = sc != null ? sc.GetMaxVisibleRows() : 5;
            float offZ = sc != null ? sc.GetFormationOffsetZ() : 0f;
            float rootZ = enemiesRoot != null ? enemiesRoot.transform.position.z : 9f;
            float row0Z = rootZ + (maxRows - 1 - 0) * (-spacing) + offZ;
            float row0X = sc != null ? sc.GetFormationOffset(2, 0) : 0f;
            info = $"第一排 rootZ={rootZ} spacing={spacing} maxRows={maxRows} offsetZ={offZ}";
            Vector3 basePos = new Vector3(row0X, 0.14f, row0Z);
            if (enemiesRoot != null)
            {
                foreach (Transform c in enemiesRoot.transform)
                {
                    var comp = c.GetComponent<MonoBehaviour>();
                    if (comp == null) continue;
                    var so = new SerializedObject(comp);
                    var rp = so.FindProperty("rowIndex");
                    if (rp == null || rp.intValue != 0) continue;
                    basePos = c.position;
                    break;
                }
            }
            return basePos;
        }

        private static void RenderCell(Camera cam, GameObject inst, Vector3 impactPoint, Vector3 tipLocal,
                                       Texture2D sheet, int ox, int oy)
        {
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4; rt.Create();
            var prev = cam.targetTexture; cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
            RenderTexture.active = null;

            // 红框 = 落点；绿十字 = 刃尖锚点
            var px = ScreenPx(cam, impactPoint);
            var red = new Color(1f, 0.25f, 0.2f, 1f);
            for (int i = -60; i <= 60; i++) { SetPx(tex, px.x + i, px.y + 60, red); SetPx(tex, px.x + i, px.y - 60, red); }
            for (int i = -60; i <= 60; i++) { SetPx(tex, px.x + 60, px.y + i, red); SetPx(tex, px.x - 60, px.y + i, red); }
            var gx = ScreenPx(cam, inst.transform.TransformPoint(tipLocal));
            var green = new Color(0.2f, 1f, 0.35f, 1f);
            for (int i = -14; i <= 14; i++) { SetPx(tex, gx.x + i, gx.y, green); SetPx(tex, gx.x, gx.y + i, green); }

            // ★ RT 的 ReadPixels 第 0 行 = 图像底部；Texture2D 第 0 行也是底部 ⇒ 不要再翻 y
            //   （之前多翻了一次，所有对照图都是倒的）
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    sheet.SetPixel(ox + x, oy + y, tex.GetPixel(x, y));
            tex.Apply();
            Object.DestroyImmediate(tex);
            cam.targetTexture = null;      // ★ 先解绑再 Release，否则 Unity 报 "Releasing render texture that is set as Camera.targetTexture"
            rt.Release(); Object.DestroyImmediate(rt);
        }

        private static void SetPx(Texture2D t, int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= t.width || y >= t.height) return;
            t.SetPixel(x, y, c);
        }

        private static Vector2Int ScreenPx(Camera cam, Vector3 p)
        {
            var v = p - cam.transform.position;
            float depth = Vector3.Dot(v, cam.transform.forward);
            float vert = Vector3.Dot(v, cam.transform.up);
            float hor = Vector3.Dot(v, cam.transform.right);
            float tan = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float ndcY = depth > 0.01f ? vert / depth / tan : 99f;
            float ndcX = depth > 0.01f ? hor / depth / (tan * cam.aspect) : 99f;
            return new Vector2Int(Mathf.RoundToInt(W * (0.5f + 0.5f * ndcX)), Mathf.RoundToInt(H * (0.5f - 0.5f * ndcY)));
        }

        private static string ScreenOf(Camera cam, Vector3 p)
        {
            var s = ScreenPx(cam, p);
            return "(" + s.x + "," + s.y + ")";
        }
    }
}
