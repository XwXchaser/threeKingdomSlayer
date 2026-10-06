using UnityEngine;

/// <summary>
/// Scene-authoring record for one route encounter. This is editor data and preview only;
/// real enemies remain owned by Battle.scene/StageController.
/// </summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class BattleEncounterAuthoring : MonoBehaviour
{
    [Header("Identity")]
    public string nodeId = "Encounter";
    public string displayName = "Battle Encounter";
    public float worldDistance;

    [Header("Battle data")]
    public StageConfig[] battleConfigs = System.Array.Empty<StageConfig>();
    public bool enabledForRoute = true;
    public bool showAllWaves = true;
    public bool showLabels = true;

    [Header("Battle-space authoring")]
    public Transform battleAnchor;
    public Transform playerEntry;
    public Vector2 battleAreaSize = new Vector2(6f, 12f);
    public Color gizmoColor = new Color(1f, 0.55f, 0.1f, 0.9f);
    public Color enemyPreviewColor = new Color(1f, 0.25f, 0.1f, 0.9f);

    public StageConfig PrimaryConfig => battleConfigs != null && battleConfigs.Length > 0 ? battleConfigs[0] : null;

    void Reset()
    {
        nodeId = gameObject.name;
        displayName = gameObject.name;
    }

    void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(nodeId)) nodeId = gameObject.name;
        if (string.IsNullOrWhiteSpace(displayName)) displayName = gameObject.name;
        battleAreaSize.x = Mathf.Max(0.5f, battleAreaSize.x);
        battleAreaSize.y = Mathf.Max(0.5f, battleAreaSize.y);
    }

    void OnDrawGizmos()
    {
        DrawGizmos(false);
    }

    void OnDrawGizmosSelected()
    {
        DrawGizmos(true);
    }

    void DrawGizmos(bool selected)
    {
        if (!enabledForRoute) return;
        Transform anchor = battleAnchor != null ? battleAnchor : transform;
        Gizmos.color = selected ? gizmoColor : new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.45f);
        Gizmos.DrawWireCube(anchor.position, new Vector3(battleAreaSize.x, 0.1f, battleAreaSize.y));
        Gizmos.DrawLine(anchor.position, anchor.position + anchor.forward * 3f);
        if (playerEntry != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(playerEntry.position, 0.35f);
            Gizmos.DrawLine(playerEntry.position, playerEntry.position + playerEntry.forward * 1.5f);
        }
#if UNITY_EDITOR
        if (showLabels && selected)
        {
            UnityEditor.Handles.color = gizmoColor;
            UnityEditor.Handles.Label(anchor.position + Vector3.up * 0.7f,
                displayName + "  [" + nodeId + "]  d=" + worldDistance.ToString("F0"));
        }
#endif
        if (selected || showAllWaves) DrawEnemyPreview();
    }

    void DrawEnemyPreview()
    {
        if (battleConfigs == null) return;
        int count = showAllWaves ? battleConfigs.Length : Mathf.Min(1, battleConfigs.Length);
        for (int i = 0; i < count; i++)
        {
            StageConfig stage = battleConfigs[i];
            if (stage == null || stage.waves == null) continue;
            DrawStagePreview(stage);
        }
    }

    void DrawStagePreview(StageConfig stage)
    {
        FormationConfig formation = stage.formationConfig;
        int visibleRows = formation != null ? Mathf.Max(1, formation.maxVisibleRows) : 5;
        float spacing = formation != null ? formation.rowSpacing : 2.5f;
        float offsetZ = formation != null ? formation.formationOffsetZ : 0f;
        Transform anchor = battleAnchor != null ? battleAnchor : transform;
        int maxRow = 0;
        foreach (var wave in stage.waves)
            if (wave != null && wave.rows != null) maxRow = Mathf.Max(maxRow, wave.rows.Count - 1);
        for (int w = 0; w < stage.waves.Count; w++)
        {
            var wave = stage.waves[w];
            if (wave == null || wave.rows == null) continue;
            for (int row = 0; row < wave.rows.Count; row++)
            {
                var rowConfig = wave.rows[row];
                if (rowConfig == null || rowConfig.enemyIds == null || rowConfig.IsRhythmGate) continue;
                for (int col = 0; col < rowConfig.enemyIds.Length; col++)
                {
                    if (rowConfig.enemyIds[col] <= 0) continue;
                    float x = formation != null
                        ? RowFormation.GetColumnOffsetX(row + 2, col, Mathf.Max(1, maxRow), formation.manualRowHalfWidths,
                            formation.formationPreset, formation.formationMaxSpread, formation.formationMinSpread, formation.formationPowerCurve)
                        : (col - 2) * 2f;
                    float z = (visibleRows - 1 - (row + 2)) * (-spacing) + offsetZ;
                    Vector3 position = anchor.TransformPoint(new Vector3(x, 0.35f, z));
                    Gizmos.color = enemyPreviewColor;
                    Gizmos.DrawWireCube(position, new Vector3(0.8f, 1.5f, 0.45f));
#if UNITY_EDITOR
                    if (showLabels)
                    {
                        UnityEditor.Handles.color = enemyPreviewColor;
                        UnityEditor.Handles.Label(position + Vector3.up * 0.85f,
                            stage.name + " W" + (w + 1) + " R" + row + " C" + col + " id=" + rowConfig.enemyIds[col]);
                    }
#endif
                }
            }
        }
    }
}
