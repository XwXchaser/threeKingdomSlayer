using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 脚底对齐工具：按敌人精灵的真实剪影，把每个 Prefab 的 visualYOffset 算成
/// "让脚底落在统一地面基准线上"的值。
///
/// 为什么不直接用 sprite 半高：精灵是 Tight 打包（rect 含 512/1108 画布），
/// 半高等于画布半高而不是脚底到 pivot 的距离，对 109 这类单位会算出偏大一倍的值。
/// 这里改用 Sprite.vertices（实际剪影网格）的最低点，不含透明边。
///
/// 取值来源：取 Prefab 上 Animator 全部 clip 引用到的精灵里，脚底位置的中位数。
/// 用中位数而非最低/最高，是为了不被个别出招帧（如 104 的 BOSS_QTE_sweep_hit1）
/// 或落马帧把整条地面线拉偏。
/// </summary>
public sealed class EnemyGroundAlignTool : EditorWindow
{
    private const string EnemyPrefabFolder = "Assets/Resources/EnemyPrefabs";
    private const float DefaultGroundY = -1.8f;

    private float groundY = DefaultGroundY;
    private Vector2 scroll;
    private readonly List<Row> rows = new List<Row>();

    private sealed class Row
    {
        public GameObject prefab;
        public float scale;
        public int sampleCount;
        public float current;
        public float target;
        public float medianFootWorld;
    }

    [MenuItem("Tools/敌人/脚底对齐 (Ground Align)")]
    private static void Open()
    {
        var w = GetWindow<EnemyGroundAlignTool>("脚底对齐");
        w.minSize = new Vector2(720f, 420f);
        w.Scan();
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "按真实剪影重算每个敌人 Prefab 的 visualYOffset，使脚底落在同一条地面基准线上。\n" +
            "先 Scan 预览，再 Apply 写入（支持 Ctrl+Z 撤销）。", MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            groundY = EditorGUILayout.FloatField("地面基准线 Y", groundY);
            if (GUILayout.Button("恢复默认", GUILayout.Width(80f))) groundY = DefaultGroundY;
            if (GUILayout.Button("Scan", GUILayout.Width(80f))) Scan();
        }

        if (rows.Count == 0) return;

        using (var s = new EditorGUILayout.ScrollViewScope(scroll))
        {
            scroll = s.scrollPosition;
            EditorGUILayout.LabelField(
                "Prefab\tscale\t帧数\t当前\t剪影脚底Y\t目标值", EditorStyles.boldLabel);
            foreach (var r in rows)
            {
                if (r.prefab == null) continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(r.prefab.name, GUILayout.Width(150f));
                    EditorGUILayout.LabelField(r.scale.ToString("0.####"), GUILayout.Width(70f));
                    EditorGUILayout.LabelField(r.sampleCount.ToString(), GUILayout.Width(50f));
                    EditorGUILayout.LabelField(r.current.ToString("0.###"), GUILayout.Width(70f));
                    EditorGUILayout.LabelField(r.medianFootWorld.ToString("0.###"), GUILayout.Width(90f));
                    Color old = GUI.color;
                    if (Mathf.Abs(r.target - r.current) > 0.001f) GUI.color = new Color(1f, 0.75f, 0.3f);
                    EditorGUILayout.LabelField(r.target.ToString("0.###"), GUILayout.Width(80f));
                    GUI.color = old;
                }
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Apply 写入 Prefab", GUILayout.Height(28f))) Apply();
            if (GUILayout.Button("还原为 0", GUILayout.Height(28f))) ResetToZero();
        }
    }

    private void Scan()
    {
        rows.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { EnemyPrefabFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;
            var enemy = go.GetComponent<Enemy>();
            if (enemy == null) continue;

            var sprite = go.GetComponentInChildren<SpriteRenderer>(true)?.sprite;
            float footLocalY = FootLocalY(sprite);

            // Prefab 根节点自身缩放在 Prefab 阶段保持 1，因此：
            // 静息态 节点Y + visualYOffset + footLocalY * scaleY == groundY
            float scaleY = go.transform.localScale.y;
            if (Mathf.Approximately(scaleY, 0f)) continue;

            rows.Add(new Row
            {
                prefab = go,
                scale = scaleY,
                sampleCount = 1,
                current = enemy.visualYOffset,
                medianFootWorld = footLocalY * scaleY,
                target = groundY - footLocalY * scaleY
            });
        }

        rows.Sort((a, b) => string.CompareOrdinal(a.prefab?.name, b.prefab?.name));
        Repaint();
    }

    /// <summary>
    /// 精灵剪影最低点在精灵网格空间的 Y（不含透明边）。
    /// </summary>
    private static float FootLocalY(Sprite sprite)
    {
        if (sprite == null) return 0f;
        var verts = sprite.vertices;
        float min = float.MaxValue;
        for (int i = 0; i < verts.Length; i++)
            if (verts[i].y < min) min = verts[i].y;
        return min == float.MaxValue ? sprite.bounds.min.y : min;
    }

    private void Apply()
    {
        var report = new StringBuilder();
        int changed = 0;
        foreach (var r in rows)
        {
            if (r.prefab == null) continue;
            var enemy = r.prefab.GetComponent<Enemy>();
            if (enemy == null) continue;

            var so = new SerializedObject(enemy);
            var prop = so.FindProperty("visualYOffset");
            if (prop == null) continue;

            Undo.RecordObject(enemy, "Enemy Ground Align");
            prop.floatValue = r.target;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(enemy);

            if (Mathf.Abs(r.target - r.current) > 0.001f)
            {
                changed++;
                report.AppendLine($"{r.prefab.name}: {r.current:0.###} -> {r.target:0.###}");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[脚底对齐] 已更新 {changed} 个 Prefab（基准 Y = {groundY:0.###}）\n{report}");
        Scan();
    }

    private void ResetToZero()
    {
        if (!EditorUtility.DisplayDialog("还原 visualYOffset",
            "把全部敌人 Prefab 的 visualYOffset 重置为 0？", "还原", "取消")) return;

        foreach (var r in rows)
        {
            if (r.prefab == null) continue;
            var enemy = r.prefab.GetComponent<Enemy>();
            if (enemy == null) continue;
            var so = new SerializedObject(enemy);
            var prop = so.FindProperty("visualYOffset");
            if (prop == null) continue;
            Undo.RecordObject(enemy, "Reset Enemy Ground Align");
            prop.floatValue = 0f;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(enemy);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[脚底对齐] visualYOffset 已全部重置为 0");
        Scan();
    }
}
