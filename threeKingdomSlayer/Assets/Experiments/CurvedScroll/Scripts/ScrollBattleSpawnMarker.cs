using System.Collections.Generic;
using UnityEngine;

/// <summary>Saved Scene marker for a route node's actual enemy spawn layout.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class ScrollBattleSpawnMarker : MonoBehaviour
{
    public FakeRouteNodeConfig node;
    public float worldDistance;
    public StageConfig[] battleConfigs = System.Array.Empty<StageConfig>();
    public float markerHeight = 0.35f;
    public bool showAllWaves = true;
    public bool showLabels = true;
    public Color markerColor = new Color(1f, 0.35f, 0.15f, 0.9f);

    void OnValidate()
    {
        transform.localPosition = new Vector3(0f, 0f, worldDistance);
        gameObject.name = (node != null ? node.nodeId : "Node") + " - Battle Spawn @ " + worldDistance.ToString("F0");
    }

    void OnDrawGizmos()
    {
        if (battleConfigs == null || battleConfigs.Length == 0) return;
        var world = GetComponentInParent<ScrollWorldAuthoring>();
        var formation = battleConfigs[0] != null ? battleConfigs[0].formationConfig : null;
        int visibleRows = formation != null ? Mathf.Max(1, formation.maxVisibleRows) : 5;
        float spacing = formation != null ? formation.rowSpacing : 2.5f;
        float offsetZ = formation != null ? formation.formationOffsetZ : 0f;
        int configCount = showAllWaves ? battleConfigs.Length : Mathf.Min(1, battleConfigs.Length);
        for (int c = 0; c < configCount; c++)
        {
            var stage = battleConfigs[c];
            if (stage == null || stage.waves == null) continue;
            for (int w = 0; w < stage.waves.Count; w++)
            {
                var wave = stage.waves[w];
                if (wave == null || wave.rows == null) continue;
                for (int row = 0; row < wave.rows.Count; row++)
                {
                    var rowConfig = wave.rows[row];
                    if (rowConfig == null || rowConfig.enemyIds == null || rowConfig.IsRhythmGate) continue;
                    int runtimeRow = row + 2;
                    for (int col = 0; col < rowConfig.enemyIds.Length; col++)
                    {
                        if (rowConfig.enemyIds[col] <= 0) continue;
                        float x = GetFormationX(formation, runtimeRow, col, Mathf.Max(1, wave.rows.Count - 1));
                        float z = GetRowZ(runtimeRow, visibleRows, spacing, offsetZ);
                        Vector3 local = new Vector3(x, markerHeight, z);
                        Vector3 position = transform.TransformPoint(local);
                        Gizmos.color = Color.Lerp(markerColor, Color.white, Mathf.Clamp01(row * 0.08f));
                        Gizmos.DrawWireCube(position, new Vector3(0.8f, 1.5f, 0.45f));
                        Gizmos.DrawLine(position + Vector3.down * 0.75f, position + Vector3.up * 0.75f);
#if UNITY_EDITOR
                        if (showLabels)
                        {
                            UnityEditor.Handles.color = Gizmos.color;
                            string label = node != null ? node.nodeId : "Node";
                            label += "  W" + (w + 1) + " R" + row + " C" + col + "  id=" + rowConfig.enemyIds[col];
                            UnityEditor.Handles.Label(position + Vector3.up * 0.9f, label);
                        }
#endif
                    }
                }
            }
        }
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f, 0.45f);
#if UNITY_EDITOR
        if (showLabels) UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, (node != null ? node.displayName : "Battle") + "  distance=" + worldDistance.ToString("F0"));
#endif
    }

    static float GetFormationX(FormationConfig config, int row, int col, int maxRow)
    {
        if (config == null) return (col - 2) * 2f;
        return RowFormation.GetColumnOffsetX(row, col, maxRow, config.manualRowHalfWidths, config.formationPreset,
            config.formationMaxSpread, config.formationMinSpread, config.formationPowerCurve);
    }
    static float GetRowZ(int row, int visibleRows, float spacing, float offset)
    {
        return (visibleRows - 1 - row) * (-spacing) + offset;
    }
}
