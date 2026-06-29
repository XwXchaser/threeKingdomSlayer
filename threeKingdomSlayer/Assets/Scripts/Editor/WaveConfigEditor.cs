using UnityEditor;
using UnityEngine;

/// <summary>
/// RowConfig 的自定义 Inspector 绘制器
/// 将 enemyIds 数组显示为清晰的列标签：列0(最左) ~ 列4(最右)
/// </summary>
[CustomPropertyDrawer(typeof(RowConfig))]
public class RowConfigDrawer : PropertyDrawer
{
    private const float LineHeight = 18f;
    private const float Spacing = 2f;
    private static readonly string[] ColLabels = { "列0 (最左)", "列1", "列2 (中)", "列3", "列4 (最右)" };

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty enemyIdsProp = property.FindPropertyRelative("enemyIds");

        float y = position.y;
        float width = position.width;

        // 标题行 + 列布局示意
        Rect headerRect = new Rect(position.x, y, width, LineHeight);
        EditorGUI.LabelField(headerRect, label, EditorStyles.boldLabel);
        y += LineHeight + Spacing;

        // 列编号提示
        Rect hintRect = new Rect(position.x, y, width, LineHeight * 0.8f);
        EditorGUI.LabelField(hintRect, "enemyIds[0]=列0  [1]=列1  [2]=列2  [3]=列3  [4]=列4", EditorStyles.miniLabel);
        y += LineHeight * 0.8f + Spacing;

        // 绘制 enemyIds 数组（每列带有中文标签和颜色背景）
        if (enemyIdsProp.arraySize != 5)
            enemyIdsProp.arraySize = 5;

        float colWidth = width / 5f;
        Color oldBg = GUI.backgroundColor;

        for (int i = 0; i < 5; i++)
        {
            Rect colRect = new Rect(position.x + i * colWidth, y, colWidth - 2, LineHeight);
            SerializedProperty elem = enemyIdsProp.GetArrayElementAtIndex(i);
            GUI.backgroundColor = i switch { 0 => Color.cyan, 2 => Color.green, 4 => new Color(1f, 0.6f, 0.6f), _ => Color.grey };
            EditorGUI.PropertyField(colRect, elem, new GUIContent(ColLabels[i]));
        }
        GUI.backgroundColor = oldBg;
        y += LineHeight + Spacing;

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return 3 * LineHeight + 2 * Spacing + LineHeight * 0.8f;
    }
}

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
/// 将每排的 5 列敌人 ID 平铺为一行，带清晰的列标签和排编号。
/// 在 Waves 列表中添加新 element 时自动分配 WaveId。
/// </summary>
[CustomEditor(typeof(StageConfig))]
public class StageConfigEditor : Editor
{
    private SerializedProperty wavesProp;
    private static readonly string[] ColLabels = { "列0(左)", "列1", "列2(中)", "列3", "列4(右)" };

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

        // 波次数
        int listSize = wavesProp.arraySize;
        int newSize = EditorGUILayout.IntField("波次数", listSize);
        if (newSize != listSize)
        {
            int oldSize = listSize;
            wavesProp.arraySize = newSize;
            // 新增 element 时自动分配 WaveId
            for (int i = oldSize; i < newSize; i++)
            {
                int maxId = 0;
                for (int j = 0; j < i; j++)
                {
                    int existingId = wavesProp.GetArrayElementAtIndex(j).FindPropertyRelative("waveId").intValue;
                    if (existingId > maxId) maxId = existingId;
                }
                wavesProp.GetArrayElementAtIndex(i).FindPropertyRelative("waveId").intValue = maxId + 1;
            }
        }

        // 逐波绘制
        for (int wi = 0; wi < wavesProp.arraySize; wi++)
        {
            SerializedProperty wave = wavesProp.GetArrayElementAtIndex(wi);
            SerializedProperty waveIdProp = wave.FindPropertyRelative("waveId");
            SerializedProperty bossProp = wave.FindPropertyRelative("isBossWave");
            SerializedProperty rowsProp = wave.FindPropertyRelative("rows");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 波次标题行：波次 N  +  BOSS 开关  +  排数(+/-)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"波次 {waveIdProp.intValue}", EditorStyles.boldLabel, GUILayout.Width(70));
            bossProp.boolValue = EditorGUILayout.ToggleLeft("BOSS波次", bossProp.boolValue, GUILayout.Width(80));
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("排数", GUILayout.Width(30));
            int rowCount = rowsProp.arraySize;
            // - 按钮
            GUI.enabled = rowCount > 0;
            if (GUILayout.Button("-", GUILayout.Width(22), GUILayout.Height(16)))
            {
                if (rowCount > 0)
                {
                    rowsProp.arraySize = rowCount - 1;
                    rowCount = rowsProp.arraySize;
                }
            }
            GUI.enabled = true;
            // 数字显示
            EditorGUILayout.LabelField(rowCount.ToString(), GUILayout.Width(20));
            // + 按钮
            if (GUILayout.Button("+", GUILayout.Width(22), GUILayout.Height(16)))
            {
                int newRowCount = rowCount + 1;
                rowsProp.arraySize = newRowCount;
                // 确保新排 enemyIds 长度为 5
                var newRowProp = rowsProp.GetArrayElementAtIndex(newRowCount - 1);
                var newEnemyIds = newRowProp.FindPropertyRelative("enemyIds");
                if (newEnemyIds.arraySize != 5)
                    newEnemyIds.arraySize = 5;
                rowCount = newRowCount;
            }
            EditorGUILayout.EndHorizontal();

            // 波次敌人强化
            SerializedProperty hpMultProp = wave.FindPropertyRelative("healthMultiplier");
            SerializedProperty atkSpdMultProp = wave.FindPropertyRelative("attackSpeedMultiplier");
            SerializedProperty dmgMultProp = wave.FindPropertyRelative("damageMultiplier");
            SerializedProperty tintProp = wave.FindPropertyRelative("waveTintColor");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("血量倍率", GUILayout.Width(60));
            hpMultProp.floatValue = EditorGUILayout.Slider(hpMultProp.floatValue, 0.1f, 5f, GUILayout.Width(140));
            GUILayout.Space(10);
            EditorGUILayout.LabelField("染色", GUILayout.Width(30));
            tintProp.colorValue = EditorGUILayout.ColorField(tintProp.colorValue, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("攻速倍率", GUILayout.Width(60));
            atkSpdMultProp.floatValue = EditorGUILayout.Slider(atkSpdMultProp.floatValue, 0.1f, 3f, GUILayout.Width(140));
            GUILayout.Space(10);
            EditorGUILayout.LabelField("伤害倍率", GUILayout.Width(60));
            dmgMultProp.floatValue = EditorGUILayout.Slider(dmgMultProp.floatValue, 0.1f, 5f, GUILayout.Width(140));
            EditorGUILayout.EndHorizontal();

            // 动态补齐（本波次独立配置）
            SerializedProperty dynamicRushProp = wave.FindPropertyRelative("enableDynamicRush");
            SerializedProperty rushDelayProp = wave.FindPropertyRelative("rushMoveDelay");
            SerializedProperty rushDelayMinProp = wave.FindPropertyRelative("rushMoveDelayMin");
            EditorGUILayout.BeginHorizontal();
            dynamicRushProp.boolValue = EditorGUILayout.ToggleLeft("动态补齐加速", dynamicRushProp.boolValue, GUILayout.Width(100));
            if (dynamicRushProp.boolValue)
            {
                EditorGUILayout.LabelField("延迟", GUILayout.Width(30));
                rushDelayProp.floatValue = EditorGUILayout.FloatField(rushDelayProp.floatValue, GUILayout.Width(50));
                EditorGUILayout.LabelField("最低", GUILayout.Width(30));
                rushDelayMinProp.floatValue = EditorGUILayout.FloatField(rushDelayMinProp.floatValue, GUILayout.Width(50));
            }
            EditorGUILayout.EndHorizontal();

            // 列标头（仅当有排时显示）
            if (rowsProp.arraySize > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("", GUILayout.Width(45)); // 排编号占位
                Color oldBg = GUI.backgroundColor;
                for (int c = 0; c < 5; c++)
                {
                    // 用颜色区分列：左=蓝、中=绿、右=红
                    GUI.backgroundColor = c switch { 0 => Color.cyan, 2 => Color.green, 4 => new Color(1f, 0.6f, 0.6f), _ => Color.grey };
                    EditorGUILayout.LabelField(ColLabels[c], EditorStyles.centeredGreyMiniLabel, GUILayout.Width(52), GUILayout.Height(16));
                }
                GUI.backgroundColor = oldBg;
                EditorGUILayout.EndHorizontal();
            }

            // 逐排绘制 enemyIds
            for (int r = 0; r < rowsProp.arraySize; r++)
            {
                var rowProp = rowsProp.GetArrayElementAtIndex(r);
                var enemyIdsProp = rowProp.FindPropertyRelative("enemyIds");
                if (enemyIdsProp.arraySize != 5)
                    enemyIdsProp.arraySize = 5;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"第{r}排", GUILayout.Width(45));
                Color oldBg = GUI.backgroundColor;
                for (int c = 0; c < 5; c++)
                {
                    var elem = enemyIdsProp.GetArrayElementAtIndex(c);
                    GUI.backgroundColor = c switch { 0 => Color.cyan, 2 => Color.green, 4 => new Color(1f, 0.6f, 0.6f), _ => Color.grey };
                    elem.intValue = EditorGUILayout.IntField(elem.intValue, GUILayout.Width(52));
                }
                GUI.backgroundColor = oldBg;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
