using UnityEditor;
using UnityEngine;

/// <summary>
/// WaveConfig 的自定义 Inspector 编辑器
/// 在新建 element 时自动递增 WaveId
/// </summary>
[CustomPropertyDrawer(typeof(WaveConfig))]
public class WaveConfigDrawer : PropertyDrawer
{
    private const float LineHeight = 18f;
    private const float Spacing = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // 获取属性
        SerializedProperty waveIdProp = property.FindPropertyRelative("waveId");
        SerializedProperty isBossWaveProp = property.FindPropertyRelative("isBossWave");
        SerializedProperty rowsProp = property.FindPropertyRelative("rows");

        // 计算布局
        float y = position.y;
        float width = position.width;

        // 标题行
        Rect headerRect = new Rect(position.x, y, width, LineHeight);
        string headerText = string.IsNullOrEmpty(label.text) ? $"波次 {waveIdProp.intValue}" : label.text;
        EditorGUI.LabelField(headerRect, headerText, EditorStyles.boldLabel);
        y += LineHeight + Spacing;

        // WaveId 显示（只读，自动管理）
        Rect idLabelRect = new Rect(position.x, y, width * 0.3f, LineHeight);
        EditorGUI.LabelField(idLabelRect, "Wave ID");
        Rect idValueRect = new Rect(position.x + width * 0.3f, y, width * 0.2f, LineHeight);
        EditorGUI.LabelField(idValueRect, waveIdProp.intValue.ToString());
        y += LineHeight + Spacing;

        // IsBossWave
        Rect bossRect = new Rect(position.x, y, width, LineHeight);
        isBossWaveProp.boolValue = EditorGUI.Toggle(bossRect, "BOSS波次", isBossWaveProp.boolValue);
        y += LineHeight + Spacing;

        // Rows 列表
        Rect rowsRect = new Rect(position.x, y, width, LineHeight);
        EditorGUI.PropertyField(rowsRect, rowsProp, new GUIContent("敌人排配置"), true);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty rowsProp = property.FindPropertyRelative("rows");
        float rowsHeight = EditorGUI.GetPropertyHeight(rowsProp, true);
        return 3 * (LineHeight + Spacing) + rowsHeight + 10f;
    }
}

/// <summary>
/// StageConfig 的自定义 Inspector 编辑器
/// 在 Waves 列表中添加新 element 时自动分配 WaveId
/// </summary>
[CustomEditor(typeof(StageConfig))]
public class StageConfigEditor : Editor
{
    private SerializedProperty wavesProp;

    private void OnEnable()
    {
        wavesProp = serializedObject.FindProperty("waves");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 绘制默认 Inspector（排除 waves 属性）
        DrawPropertiesExcluding(serializedObject, "waves");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("波次配置", EditorStyles.boldLabel);

        // 手动绘制 waves 列表
        int listSize = wavesProp.arraySize;
        int newSize = EditorGUILayout.IntField("波次数", listSize);

        // 检测是否新增了 element
        if (newSize > listSize)
        {
            // 新增了 element，自动分配 WaveId
            wavesProp.arraySize = newSize;
            for (int i = listSize; i < newSize; i++)
            {
                SerializedProperty newWave = wavesProp.GetArrayElementAtIndex(i);
                SerializedProperty waveIdProp = newWave.FindPropertyRelative("waveId");
                // 自动分配 ID = 当前最大 ID + 1
                int maxId = 0;
                for (int j = 0; j < i; j++)
                {
                    SerializedProperty existingWave = wavesProp.GetArrayElementAtIndex(j);
                    int existingId = existingWave.FindPropertyRelative("waveId").intValue;
                    if (existingId > maxId) maxId = existingId;
                }
                waveIdProp.intValue = maxId + 1;
            }
        }
        else if (newSize < listSize)
        {
            // 删除了 element
            wavesProp.arraySize = newSize;
        }

        EditorGUI.indentLevel++;
        for (int i = 0; i < wavesProp.arraySize; i++)
        {
            SerializedProperty wave = wavesProp.GetArrayElementAtIndex(i);
            SerializedProperty waveIdProp = wave.FindPropertyRelative("waveId");
            SerializedProperty rowsProp = wave.FindPropertyRelative("rows");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"波次 {waveIdProp.intValue}", EditorStyles.boldLabel);

            // WaveId（只读显示）
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("Wave ID", waveIdProp.intValue);
            EditorGUI.EndDisabledGroup();

            // BUG FIX: WaveConfig 没有 nextWaveDelay 字段，已移除
            SerializedProperty bossProp = wave.FindPropertyRelative("isBossWave");
            bossProp.boolValue = EditorGUILayout.Toggle("BOSS波次", bossProp.boolValue);

            EditorGUILayout.PropertyField(rowsProp, new GUIContent("敌人排配置"), true);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        EditorGUI.indentLevel--;

        serializedObject.ApplyModifiedProperties();
    }
}
