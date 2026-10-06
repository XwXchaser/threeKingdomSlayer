using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyAttackTelegraph))]
public sealed class EnemyAttackTelegraphEditor : Editor
{
    private static readonly string[] FrameLabels =
    {
        "Frame 1 - Diamond Point",
        "Frame 2 - Expanded Cross (75%)",
        "Frame 3 - Cross + Outer Ring"
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();

        var previewFrame = serializedObject.FindProperty("previewFrame");
        var showPreview = serializedObject.FindProperty("showPreview");
        var frames = serializedObject.FindProperty("telegraphFrames");

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Edit Mode Preview", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "在 Edit Mode 预览预警帧，然后移动父级 AttackTelegraphAnchor 调整敌人专属默认点位。预览不会触发攻击。",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            for (int i = 0; i < EnemyAttackTelegraph.FrameCount; i++)
            {
                bool isSelected = previewFrame.intValue == i;
                GUI.backgroundColor = isSelected ? new Color(0.45f, 0.85f, 1f) : Color.white;
                if (GUILayout.Button((i + 1).ToString(), GUILayout.Height(28f)))
                {
                    previewFrame.intValue = i;
                    showPreview.boolValue = true;
                }
            }
            GUI.backgroundColor = Color.white;
        }

        int selectedFrame = Mathf.Clamp(previewFrame.intValue, 0, EnemyAttackTelegraph.FrameCount - 1);
        EditorGUILayout.LabelField(FrameLabels[selectedFrame]);
        EditorGUILayout.ObjectField("Sprite", frames.GetArrayElementAtIndex(selectedFrame).objectReferenceValue, typeof(Sprite), false);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Show Selected Frame"))
                showPreview.boolValue = true;
            if (GUILayout.Button("Hide Preview"))
                showPreview.boolValue = false;
        }

        if (serializedObject.ApplyModifiedProperties())
        {
            var component = (EnemyAttackTelegraph)target;
            EditorUtility.SetDirty(component);
        }
    }
}
