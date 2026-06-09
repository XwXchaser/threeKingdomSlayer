using UnityEditor;
using UnityEngine;

/// <summary>
/// UpgradeDefinition 自定义 Inspector
///
/// 架构原则：
/// - 效果为主：effectType + 效果每级参数始终可见
/// - 触发为辅：triggerMode 通过选项卡切换，仅显示对应触发字段
/// - 切换选项卡不丢失数据（所有字段始终序列化）
/// </summary>
[CustomEditor(typeof(UpgradeDefinition))]
public class UpgradeDefinitionEditor : Editor
{
    // ── 顶层属性 ──
    private SerializedProperty categoryProp;
    private SerializedProperty upgradeIdProp;
    private SerializedProperty displayNameProp;
    private SerializedProperty descriptionTemplateProp;
    private SerializedProperty rarityProp;
    private SerializedProperty maxLevelProp;
    private SerializedProperty effectTypeProp;
    private SerializedProperty floatValueProp;
    private SerializedProperty intValueProp;
    private SerializedProperty secondaryIntValueProp;
    private SerializedProperty stringValueProp;
    private SerializedProperty baseAttackConfigProp;
    private SerializedProperty iconProp;
    private SerializedProperty triggerModeProp;

    // ── 每级配置列表 ──
    private SerializedProperty phantomLevelsProp;
    private SerializedProperty timedAoeLevelsProp;
    private SerializedProperty timedArrowLevelsProp;
    private SerializedProperty returnWaveLevelsProp;
    private SerializedProperty chainBounceLevelsProp;

    // ── 道具型 ──
    private SerializedProperty useCountProp;
    private SerializedProperty gestureIdProp;

    // ── 其他 ──
    private SerializedProperty prerequisitesProp;

    // ── 选项卡状态 ──
    private int _triggerTab;

    private void OnEnable()
    {
        categoryProp = serializedObject.FindProperty("category");
        upgradeIdProp = serializedObject.FindProperty("upgradeId");
        displayNameProp = serializedObject.FindProperty("displayName");
        descriptionTemplateProp = serializedObject.FindProperty("descriptionTemplate");
        rarityProp = serializedObject.FindProperty("rarity");
        maxLevelProp = serializedObject.FindProperty("maxLevel");
        effectTypeProp = serializedObject.FindProperty("effectType");
        floatValueProp = serializedObject.FindProperty("floatValue");
        intValueProp = serializedObject.FindProperty("intValue");
        secondaryIntValueProp = serializedObject.FindProperty("secondaryIntValue");
        stringValueProp = serializedObject.FindProperty("stringValue");
        baseAttackConfigProp = serializedObject.FindProperty("baseAttackConfig");
        iconProp = serializedObject.FindProperty("icon");
        triggerModeProp = serializedObject.FindProperty("triggerMode");

        phantomLevelsProp = serializedObject.FindProperty("phantomLevels");
        timedAoeLevelsProp = serializedObject.FindProperty("timedAoeLevels");
        timedArrowLevelsProp = serializedObject.FindProperty("timedArrowLevels");
        returnWaveLevelsProp = serializedObject.FindProperty("returnWaveLevels");
        chainBounceLevelsProp = serializedObject.FindProperty("chainBounceLevels");

        useCountProp = serializedObject.FindProperty("useCount");
        gestureIdProp = serializedObject.FindProperty("gestureId");
        prerequisitesProp = serializedObject.FindProperty("prerequisites");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ── 标识 ──
        EditorGUILayout.LabelField("标识", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(categoryProp);
        EditorGUILayout.PropertyField(upgradeIdProp);

        EditorGUILayout.Space();

        // ── 显示 ──
        EditorGUILayout.LabelField("显示", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(displayNameProp);
        EditorGUILayout.PropertyField(descriptionTemplateProp);
        EditorGUILayout.PropertyField(iconProp);

        EditorGUILayout.Space();

        // ── 稀有度 ──
        EditorGUILayout.LabelField("稀有度与等级", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(rarityProp);
        EditorGUILayout.PropertyField(maxLevelProp);

        EditorGUILayout.Space();

        var category = (UpgradeCategory)categoryProp.enumValueIndex;

        // ══════════════════════════════════════
        // 被动攻击型 — 效果 + 触发选项卡
        // ══════════════════════════════════════
        if (category == UpgradeCategory.Passive)
        {
            DrawPassiveSection();
        }
        // ══════════════════════════════════════
        // 数值型
        // ══════════════════════════════════════
        else if (category == UpgradeCategory.Numeric)
        {
            DrawNumericSection();
        }
        // ══════════════════════════════════════
        // 道具型
        // ══════════════════════════════════════
        else if (category == UpgradeCategory.Item)
        {
            DrawItemSection();
        }

        EditorGUILayout.Space();

        // ── 前置条件 ──
        EditorGUILayout.PropertyField(prerequisitesProp);

        serializedObject.ApplyModifiedProperties();
    }

    // ══════════════════════════════════════════
    // 被动攻击型绘制
    // ══════════════════════════════════════════

    private void DrawPassiveSection()
    {
        EditorGUILayout.PropertyField(effectTypeProp);

        string effectType = effectTypeProp.stringValue;
        string triggerField = null;
        string intervalField = null;

        // 根据 effectType 确定触发字段名，统一传给 DrawEffectLevelList
        switch (effectType)
        {
            case "passive_phantom_weapon":
                triggerField = "triggerParam";
                intervalField = "intervalSeconds";
                break;
            case "passive_timed_aoe":
                triggerField = "triggerThreshold";
                intervalField = "intervalSeconds";
                break;
            case "passive_timed_arrow":
                triggerField = "triggerThreshold";
                intervalField = "intervalSeconds";
                break;
            case "passive_return_wave":
                triggerField = "triggerThreshold";
                intervalField = "intervalSeconds";
                break;
            case "passive_chain_bounce":
                triggerField = "triggerThreshold";
                intervalField = "intervalSeconds";
                break;
        }

        // ── 触发方式选项卡（放在效果列表上方，切换即时生效）──
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("触发方式", EditorStyles.boldLabel, GUILayout.Width(56));
        _triggerTab = GUILayout.Toolbar(_triggerTab, new[] { "攻击计数", "定时触发" });
        EditorGUILayout.EndHorizontal();
        triggerModeProp.enumValueIndex = _triggerTab;

        EditorGUILayout.Space();

        // ── 效果每级配置（效果字段始终可见 + 触发字段按选项卡内联）──
        switch (effectType)
        {
            case "passive_phantom_weapon":
                DrawEffectLevelList(phantomLevelsProp, "phantomSteps", true, triggerField, intervalField);
                break;
            case "passive_timed_aoe":
                DrawEffectLevelList(timedAoeLevelsProp, "columns", false, triggerField, intervalField);
                break;
            case "passive_timed_arrow":
                DrawEffectLevelList(timedArrowLevelsProp, null, false, triggerField, intervalField);
                break;
            case "passive_return_wave":
                DrawEffectLevelList(returnWaveLevelsProp, null, false, triggerField, intervalField);
                break;
            case "passive_chain_bounce":
                DrawEffectLevelList(chainBounceLevelsProp, null, false, triggerField, intervalField);
                break;
            default:
                EditorGUILayout.HelpBox($"未知的被动 effectType: {effectType}", MessageType.Warning);
                break;
        }
    }

    // ══════════════════════════════════════════
    // 数值型绘制
    // ══════════════════════════════════════════

    private void DrawNumericSection()
    {
        EditorGUILayout.LabelField("效果", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(effectTypeProp);
        EditorGUILayout.PropertyField(floatValueProp);
        EditorGUILayout.PropertyField(intValueProp);
        EditorGUILayout.PropertyField(secondaryIntValueProp);
        EditorGUILayout.PropertyField(stringValueProp);
        EditorGUILayout.PropertyField(baseAttackConfigProp);
    }

    // ══════════════════════════════════════════
    // 道具型绘制
    // ══════════════════════════════════════════

    private void DrawItemSection()
    {
        EditorGUILayout.LabelField("效果", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(effectTypeProp);
        EditorGUILayout.PropertyField(floatValueProp);
        EditorGUILayout.PropertyField(intValueProp);
        EditorGUILayout.PropertyField(baseAttackConfigProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("道具参数", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(useCountProp);
        EditorGUILayout.PropertyField(gestureIdProp);
    }

    // ══════════════════════════════════════════
    // 每级列表绘制辅助
    // ══════════════════════════════════════════

    /// <summary>绘制效果每级列表。效果字段始终可见；触发字段按 triggerMode 选项卡内联</summary>
    private void DrawEffectLevelList(SerializedProperty listProp, string nestedListName, bool isPhantom,
        string triggerFieldName, string intervalFieldName)
    {
        if (listProp == null) return;

        EditorGUILayout.PropertyField(listProp.FindPropertyRelative("Array.size"));

        bool isTimed = triggerModeProp.enumValueIndex == 1;
        string activeTriggerField = isTimed ? intervalFieldName : triggerFieldName;
        string triggerLabel = isTimed ? "间隔(秒)" : "阈值(次)";

        for (int i = 0; i < listProp.arraySize; i++)
        {
            var elem = listProp.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Lv.{i + 1}", EditorStyles.miniBoldLabel);

            if (isPhantom)
            {
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("attackType"));
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("targetColumn"));
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("phantomSteps"), true);
            }
            else if (nestedListName == "columns")
            {
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("damage"));
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("columns"));
            }
            else if (listProp.name.StartsWith("timedArrow"))
            {
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("rowCount"));
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("arrowCount"));
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("damage"));
            }
            else if (listProp.name.StartsWith("returnWave"))
            {
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("column"));
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("rangeRows"));
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("damageRatio"));
            }
            else if (listProp.name.StartsWith("chainBounce"))
            {
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("column"));
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("maxBounces"));
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("damageRatio"));
            }

            // 触发字段按选项卡内联（始终序列化，仅显示切换）
            if (!string.IsNullOrEmpty(activeTriggerField))
            {
                EditorGUILayout.PropertyField(elem.FindPropertyRelative(activeTriggerField),
                    new GUIContent(triggerLabel));
            }

            EditorGUILayout.EndVertical();
        }
    }

}
