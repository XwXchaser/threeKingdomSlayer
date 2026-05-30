using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QTEConfig))]
public class QTEConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var qteConfig = (QTEConfig)target;

        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck())
        {
            // Find all QTEAttackConfigs that reference this QTEConfig and refresh their clips
            PropagateToAttackConfigs(qteConfig);
        }
    }

    private static void PropagateToAttackConfigs(QTEConfig changedConfig)
    {
        var guids = AssetDatabase.FindAssets("t:QTEAttackConfig");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var attackConfig = AssetDatabase.LoadAssetAtPath<QTEAttackConfig>(path);
            if (attackConfig == null) continue;

            bool references = false;
            foreach (var slot in attackConfig.qteSlots)
            {
                if (slot.config == changedConfig)
                { references = true; break; }
            }

            if (references)
            {
                QTEAttackConfigEditor.ForceApplyClipDuration(attackConfig);
            }
        }
    }
}
