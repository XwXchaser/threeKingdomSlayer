// 本文件位于 Locus.Combat（运行时程序集）下的 Editor 子目录 → asmref 接管后 "Editor 文件夹" 规则失效，
// 编辑器脚本会被编进玩家程序集（打包报 "The type or namespace name 'Editor' could not be found"）。
// 用标准的 #if UNITY_EDITOR 包裹，运行时编译为空。
#if UNITY_EDITOR
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
#endif
