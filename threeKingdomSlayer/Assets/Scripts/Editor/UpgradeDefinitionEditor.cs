using UnityEditor;
using UnityEngine;

/// <summary>
/// UpgradeDefinition 自定义 Inspector
///
/// 架构原则：
/// - 效果为主：effectType + 效果每级参数始终可见
/// - 触发为辅：category（AttackPassive / TimedPassive）决定触发字段，不丢数据
/// - 触发字段内联到每级效果 box 末尾
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
    private SerializedProperty numericLevelsProp;
    private SerializedProperty floatValueProp;
    private SerializedProperty intValueProp;
    private SerializedProperty secondaryIntValueProp;
    private SerializedProperty stringValueProp;
    private SerializedProperty baseAttackConfigProp;
    private SerializedProperty iconProp;

    // ── 每级配置列表 ──
    private SerializedProperty phantomLevelsProp;
    private SerializedProperty timedAoeLevelsProp;
    private SerializedProperty timedArrowLevelsProp;
    private SerializedProperty returnWaveLevelsProp;
    private SerializedProperty chainBounceLevelsProp;
    private SerializedProperty cycloneLevelsProp;
    private SerializedProperty arrowVolleyLevelsProp;
    private SerializedProperty reflectShieldLevelsProp;
    private SerializedProperty chargeShockwaveLevelsProp;
    private SerializedProperty chargeAttackShockwaveLevelsProp;
    private SerializedProperty chargeHitShockwaveLevelsProp;

    // ── 道具型 ──
    private SerializedProperty useCountProp;
    private SerializedProperty gestureIdProp;
    private SerializedProperty cycloneItemConfigProp;

    // ── 其他 ──
    private SerializedProperty prerequisitesProp;

    private void OnEnable()
    {
        categoryProp = serializedObject.FindProperty("category");
        upgradeIdProp = serializedObject.FindProperty("upgradeId");
        displayNameProp = serializedObject.FindProperty("displayName");
        descriptionTemplateProp = serializedObject.FindProperty("descriptionTemplate");
        rarityProp = serializedObject.FindProperty("rarity");
        maxLevelProp = serializedObject.FindProperty("maxLevel");
        effectTypeProp = serializedObject.FindProperty("effectType");
        numericLevelsProp = serializedObject.FindProperty("numericLevels");
        floatValueProp = serializedObject.FindProperty("floatValue");
        intValueProp = serializedObject.FindProperty("intValue");
        secondaryIntValueProp = serializedObject.FindProperty("secondaryIntValue");
        stringValueProp = serializedObject.FindProperty("stringValue");
        baseAttackConfigProp = serializedObject.FindProperty("baseAttackConfig");
        iconProp = serializedObject.FindProperty("icon");

        phantomLevelsProp = serializedObject.FindProperty("phantomLevels");
        timedAoeLevelsProp = serializedObject.FindProperty("timedAoeLevels");
        timedArrowLevelsProp = serializedObject.FindProperty("timedArrowLevels");
        returnWaveLevelsProp = serializedObject.FindProperty("returnWaveLevels");
        chainBounceLevelsProp = serializedObject.FindProperty("chainBounceLevels");
        cycloneLevelsProp = serializedObject.FindProperty("cycloneLevels");
        arrowVolleyLevelsProp = serializedObject.FindProperty("arrowVolleyLevels");
        reflectShieldLevelsProp = serializedObject.FindProperty("reflectShieldLevels");
        chargeShockwaveLevelsProp = serializedObject.FindProperty("chargeShockwaveLevels");
        chargeHitShockwaveLevelsProp = serializedObject.FindProperty("chargeHitShockwaveLevels");

        useCountProp = serializedObject.FindProperty("useCount");
        gestureIdProp = serializedObject.FindProperty("gestureId");
        cycloneItemConfigProp = serializedObject.FindProperty("cycloneItemConfig");
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
        // 被动攻击型
        // ══════════════════════════════════════
        if (category == UpgradeCategory.AttackPassive || category == UpgradeCategory.TimedPassive)
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
        else if (category == UpgradeCategory.ActiveSkill)
        {
            EditorGUILayout.HelpBox("主动技能请创建 ActiveSkillDefinition 资产，并配置独立冷却与每级效果。", MessageType.Info);
            DrawPassiveSection();
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
        bool isTimed = categoryProp.enumValueIndex == (int)UpgradeCategory.TimedPassive;

        // ── 效果每级配置（效果字段始终可见 + 触发字段按 category 内联）──
        switch (effectType)
        {
            case "passive_phantom_weapon":
                DrawEffectLevelList(phantomLevelsProp, "phantomSteps", true, isTimed, "triggerParam", "intervalSeconds");
                break;
            case "passive_timed_aoe":
                DrawEffectLevelList(timedAoeLevelsProp, "columns", false, isTimed, "triggerThreshold", "intervalSeconds");
                break;
            case "passive_timed_arrow":
                DrawEffectLevelList(timedArrowLevelsProp, null, false, isTimed, "triggerThreshold", "intervalSeconds");
                break;
            case "passive_return_wave":
                DrawEffectLevelList(returnWaveLevelsProp, null, false, isTimed, "triggerThreshold", "intervalSeconds");
                break;
            case "passive_chain_bounce":
                DrawEffectLevelList(chainBounceLevelsProp, null, false, isTimed, "triggerThreshold", "intervalSeconds");
                break;
            case "passive_timed_cyclone":
                DrawCycloneSection();
                break;
            case "passive_arrow_volley":
                DrawArrowVolleySection();
                break;
            case "charge_shockwave":
                DrawChargeShockwaveSection();
                break;
            case "charge_hit_shockwave":
                DrawChargeHitShockwaveSection();
                break;
            default:
                EditorGUILayout.HelpBox($"未知的被动 effectType: {effectType}", MessageType.Warning);
                break;
        }
    }

    // ══════════════════════════════════════════
    // 箭矢齐射绘制
    // ══════════════════════════════════════════

    private void DrawArrowVolleySection()
    {
        if (arrowVolleyLevelsProp == null) return;

        EditorGUILayout.PropertyField(arrowVolleyLevelsProp.FindPropertyRelative("Array.size"));

        for (int i = 0; i < arrowVolleyLevelsProp.arraySize; i++)
        {
            var elem = arrowVolleyLevelsProp.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Lv.{i + 1}", EditorStyles.miniBoldLabel);

            EditorGUILayout.PropertyField(elem.FindPropertyRelative("triggerThreshold"), new GUIContent("阈值(次)"));
            EditorGUILayout.PropertyField(elem.FindPropertyRelative("targetCount"), new GUIContent("敌人数"));
            EditorGUILayout.PropertyField(elem.FindPropertyRelative("arrowCount"), new GUIContent("每敌箭矢数"));
            EditorGUILayout.PropertyField(elem.FindPropertyRelative("baseDamage"), new GUIContent("基础伤害"));

            EditorGUILayout.EndVertical();
        }
    }

    // ══════════════════════════════════════════
    // 旋风绘制
    // ══════════════════════════════════════════

    private void DrawCycloneSection()
    {
        if (cycloneLevelsProp == null) return;

        EditorGUILayout.PropertyField(cycloneLevelsProp.FindPropertyRelative("Array.size"));

        for (int i = 0; i < cycloneLevelsProp.arraySize; i++)
        {
            var elem = cycloneLevelsProp.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Lv.{i + 1}", EditorStyles.miniBoldLabel);

            EditorGUILayout.PropertyField(elem.FindPropertyRelative("intervalSeconds"), new GUIContent("间隔(秒)"));
            EditorGUILayout.PropertyField(elem.FindPropertyRelative("enemyCount"), new GUIContent("敌人数"));
            EditorGUILayout.PropertyField(elem.FindPropertyRelative("knockupDuration"), new GUIContent("击飞时长(秒)"));
            EditorGUILayout.PropertyField(elem.FindPropertyRelative("damage"), new GUIContent("击飞伤害"));
            EditorGUILayout.PropertyField(elem.FindPropertyRelative("landingDamagePercent"), new GUIContent("落地伤害%"));

            EditorGUILayout.EndVertical();
        }
    }

    // ══════════════════════════════════════════
    // 数值型绘制
    // ══════════════════════════════════════════

    private void DrawNumericSection()
    {
        EditorGUILayout.LabelField("效果", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(effectTypeProp);

        string effectType = effectTypeProp.stringValue;

        if (effectType == "charge_reflect_shield")
            DrawReflectShieldSection();
        else
            DrawNumericLevelsSection();
    }

    private void DrawNumericLevelsSection()
    {
        if (numericLevelsProp == null) return;

        EditorGUILayout.PropertyField(numericLevelsProp.FindPropertyRelative("Array.size"), new GUIContent("等级数"));

        string effectType = effectTypeProp.stringValue;

        for (int i = 0; i < numericLevelsProp.arraySize; i++)
        {
            var elem = numericLevelsProp.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Lv.{i + 1}", EditorStyles.miniBoldLabel);

            if (effectType == "spike_trap")
            {
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("floatValue"), new GUIContent("伤害"));
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("intValue"), new GUIContent("行 (row)"));
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("secondaryIntValue"), new GUIContent("列 (col)"));
            }
            else if (effectType == "stab_range_boost" || effectType == "sweep_range_boost")
            {
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("intValue"), new GUIContent("范围加成"));
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("secondaryIntValue"), new GUIContent("伤害惩罚(%)"));
            }
            else if (effectType == "push_wave" || effectType == "convergence_wave")
            {
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("intValue"), new GUIContent("排数/格数"));
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("floatValue"), new GUIContent("百分比"));
            }
            else if (effectType == "exp_multiplier")
            {
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("floatValue"), new GUIContent("经验倍率增量"));
            }
            else if (effectType == "damage_multiplier")
            {
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("floatValue"), new GUIContent("伤害倍率增量"));
            }
            else if (effectType == "attack_speed")
            {
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("floatValue"), new GUIContent("攻速增量"));
            }
            else if (effectType == "move_speed")
            {
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("floatValue"), new GUIContent("移速增量"));
            }
            else
            {
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("floatValue"));
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("intValue"));
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("secondaryIntValue"));
            }

            EditorGUILayout.EndVertical();
        }
    }

    private void DrawReflectShieldSection()
    {
        if (reflectShieldLevelsProp == null) return;

        EditorGUILayout.PropertyField(reflectShieldLevelsProp.FindPropertyRelative("Array.size"), new GUIContent("等级数"));

        for (int i = 0; i < reflectShieldLevelsProp.arraySize; i++)
        {
            var elem = reflectShieldLevelsProp.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Lv.{i + 1}", EditorStyles.miniBoldLabel);

            EditorGUILayout.PropertyField(elem.FindPropertyRelative("intervalSeconds"), new GUIContent("CD间隔(秒)"));
            EditorGUILayout.PropertyField(elem.FindPropertyRelative("shieldAmount"), new GUIContent("护盾值"));

            var enableBonusProp = elem.FindPropertyRelative("enableBonus");
            EditorGUILayout.PropertyField(enableBonusProp, new GUIContent("启用额外效果"));
            if (enableBonusProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("bonusReflectPercent"), new GUIContent("反伤加成(%)"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }
    }

    // ══════════════════════════════════════════
    // 蓄力冲击波绘制
    // ══════════════════════════════════════════

    private void DrawChargeShockwaveSection()
    {
        if (chargeShockwaveLevelsProp == null) return;

        EditorGUILayout.PropertyField(chargeShockwaveLevelsProp.FindPropertyRelative("Array.size"));

        for (int i = 0; i < chargeShockwaveLevelsProp.arraySize; i++)
        {
            var elem = chargeShockwaveLevelsProp.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Lv.{i + 1}", EditorStyles.miniBoldLabel);

            EditorGUILayout.PropertyField(elem.FindPropertyRelative("intervalSeconds"), new GUIContent("间隔(秒)"));
            EditorGUILayout.PropertyField(elem.FindPropertyRelative("shockwaveCount"), new GUIContent("每次波数"));
            EditorGUILayout.PropertyField(elem.FindPropertyRelative("rangeRows"), new GUIContent("射程排数"));
            EditorGUILayout.PropertyField(elem.FindPropertyRelative("baseDamage"), new GUIContent("基础伤害"));
            EditorGUILayout.PropertyField(elem.FindPropertyRelative("stackDamageBonus"), new GUIContent("每层增伤(小数)"));
            EditorGUILayout.PropertyField(elem.FindPropertyRelative("waveDelay"), new GUIContent("波间延迟(秒)"));

            EditorGUILayout.EndVertical();
        }
    }

    private void DrawChargeHitShockwaveSection()
    {
        if (chargeHitShockwaveLevelsProp == null) return;
        EditorGUILayout.PropertyField(chargeHitShockwaveLevelsProp.FindPropertyRelative("Array.size"));
        for (int i = 0; i < chargeHitShockwaveLevelsProp.arraySize; i++)
        {
            var elem = chargeHitShockwaveLevelsProp.GetArrayElementAtIndex(i);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Lv.{i + 1}", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(elem.FindPropertyRelative("shockwaveCount"), new GUIContent("冲击波数量"));
            EditorGUILayout.PropertyField(elem.FindPropertyRelative("baseDamage"), new GUIContent("基础伤害"));
            EditorGUILayout.PropertyField(elem.FindPropertyRelative("rangeRows"), new GUIContent("范围排数"));
            EditorGUILayout.PropertyField(elem.FindPropertyRelative("damageBonusPerHit"), new GUIContent("每次受击增伤(小数)"));
            EditorGUILayout.EndVertical();
        }
    }

    // ══════════════════════════════════════════
    // 道具型绘制
    // ══════════════════════════════════════════

    private void DrawItemSection()
    {
        EditorGUILayout.LabelField("效果", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(effectTypeProp);
        if (effectTypeProp.stringValue == "item_cyclone")
            EditorGUILayout.PropertyField(cycloneItemConfigProp, true);
        else
        {
            EditorGUILayout.PropertyField(floatValueProp);
            EditorGUILayout.PropertyField(intValueProp);
            EditorGUILayout.PropertyField(secondaryIntValueProp);
            EditorGUILayout.PropertyField(baseAttackConfigProp);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("道具参数", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(useCountProp);
        EditorGUILayout.PropertyField(gestureIdProp);
    }

    // ══════════════════════════════════════════
    // 每级列表绘制辅助
    // ══════════════════════════════════════════

    /// <summary>绘制效果每级列表。效果字段始终可见；触发字段按 category 内联</summary>
    private void DrawEffectLevelList(SerializedProperty listProp, string nestedListName, bool isPhantom,
        bool isTimed, string triggerFieldName, string intervalFieldName)
    {
        if (listProp == null) return;

        EditorGUILayout.PropertyField(listProp.FindPropertyRelative("Array.size"));

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
