using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// **Y 场景坐标读取器**（2026-10-23）：让 agent 能从参考图/场景里读出「沿路坐标」✅。
///
/// 场景是 **flat world** ✅：z 就是沿路米数 ✅，x 是横向偏移 ✅。
/// 路线在 190m 处分叉 ✅ → 采样时左右两支都算 ✅，取最近的那个 ✅。
///
/// 用法：选中场景里的一个物件 ✅ → 点「读取」✅ → 读出沿路米数 / 横向偏移 / 所属节点 ✅
/// → 点「复制摆放行」✅ → 直接粘进摆放表 JSON ✅。
/// </summary>
public class YSceneCoordinateWindow : EditorWindow
{
    private Vector2 _scroll;
    private string _result = "（还没读）";
    private string _placementLine = "";
    private Transform _picked;

    [MenuItem("Tools/Y 场景/坐标读取器")]
    private static void Open()
    {
        var w = GetWindow<YSceneCoordinateWindow>("Y 场景坐标");
        w.minSize = new Vector2(380, 260);
        w.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("选中一个场景物件，读出它的沿路坐标", EditorStyles.wordWrappedLabel);
        var sel = Selection.activeTransform;
        EditorGUILayout.LabelField("当前选中", sel == null ? "（无）" : sel.name);
        if (GUILayout.Button("读取坐标")) Read(sel);
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.TextArea(_result, GUILayout.Height(90));
        if (!string.IsNullOrEmpty(_placementLine))
        {
            EditorGUILayout.LabelField("摆放表行");
            EditorGUILayout.SelectableLabel(_placementLine, GUILayout.Height(40));
            if (GUILayout.Button("复制摆放行")) EditorGUIUtility.systemCopyBuffer = _placementLine;
        }
        EditorGUILayout.EndScrollView();
    }

    private void Read(Transform t)
    {
        if (t == null) { _result = "先在场景里选一个物件"; return; }
        _picked = t;
        var route = FindRoute(t);
        if (route == null) { _result = "场景里没找到 YScrollSample（路线）"; return; }

        Vector3 world = t.position;
        float best = float.MaxValue, bestLat = 0f, bestDist = 0f;
        bool bestRight = true;
        // 左右两支都采样 ✅（190m 处分叉 ✅）
        for (int branch = 0; branch < 2; branch++)
        {
            bool right = branch == 1;
            for (float d = 0f; d <= route.Length; d += 0.5f)
            {
                Vector3 p = YScrollSample.Evaluate(d, route.turnAngle, right, out _);
                float dz = p.z - world.z;
                float dx = p.x - world.x;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                if (dist < best)
                {
                    best = dist; bestDist = d; bestRight = right;
                    // 横向偏移：物件到路线中心线的垂直距离 ✅
                    Vector3 fwd = YScrollSample.Evaluate(Mathf.Min(route.Length, d + 1f), route.turnAngle, right, out float heading);
                    Vector3 f = fwd - p; f.y = 0f;
                    if (f.sqrMagnitude > 0.0001f) f.Normalize();
                    bestLat = dx * -f.z + dz * f.x;
                }
            }
        }
        string node = NearestNode(t);
        _result = $"沿路米数 = {bestDist:F1} m\n"
                 + $"横向偏移 = {bestLat:F2} m（正=右）\n"
                 + $"分支 = {(bestRight ? "右支" : "左支")}\n"
                 + $"到路线中心距离 = {best:F2} m\n"
                 + $"所属节点 = {node}\n"
                 + $"局部坐标 = ({t.localPosition.x:F2}, {t.localPosition.y:F2}, {t.localPosition.z:F2})";

        string id = t.name;
        string kind = "sprite";
        var sr = t.GetComponent<SpriteRenderer>();
        var mr = t.GetComponent<MeshRenderer>();
        if (sr == null && mr != null) kind = "mesh";
        string layer = t.parent != null ? t.parent.name : "Props";
        _placementLine = "    { \"id\": \"" + id + "\", \"kind\": \"" + kind + "\", \"at\": ["
            + (bestLat).ToString("F2") + ", " + (bestDist).ToString("F2") + "], \"layer\": \"" + layer + "\" }";
    }

    private static YScrollSample FindRoute(Transform t)
    {
        var roots = t.gameObject.scene.GetRootGameObjects();
        foreach (var r in roots)
        {
            var s = r.GetComponentInChildren<YScrollSample>(true);
            if (s != null) return s;
        }
        return null;
    }

    private static string NearestNode(Transform t)
    {
        string best = "（无）";
        float bestD = float.MaxValue;
        foreach (var p in UnityEngine.Object.FindObjectsOfType<ScrollRouteNodePresentation>(true))
        {
            float d = Vector3.Distance(p.transform.position, t.position);
            if (d < bestD) { bestD = d; best = p.gameObject.name; }
        }
        return best;
    }
}
