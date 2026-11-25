using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ActiveSkillDefinition))]
public class ActiveSkillDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("category"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("upgradeId"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("kindId"),
            new GUIContent("同款身份 kindId", "留空 = 用 upgradeId。同一件装备的不同品质 / 等级变体填同一个 kindId。"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("descriptionTemplate"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("extraDescriptionTemplate"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("levelFeatureDescriptions"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rarity"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("quality"),
            new GUIContent("品质", "普通 / 精良 / 传说（玩家可见；与装备池内档位无关）。"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("visibleLevel"),
            new GUIContent("可见等级", "选中后按这一级生效（装备变体用）：不 +1、不继承旧等级。"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxLevel"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultLevelCap"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("statId"),
            new GUIContent("属性 statId", "影之武装留空；属性条目才填。"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("unlockedByDefault"),
            new GUIContent("初始即解锁", "demo 期现有内容全勾；长线改为掉落解锁。"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("prerequisites"), true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("主动技能", EditorStyles.boldLabel);
        var effectType = serializedObject.FindProperty("activeEffectType");
        EditorGUILayout.PropertyField(effectType);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cooldownLevels"), true);

        switch ((ActiveSkillEffectType)effectType.enumValueIndex)
        {
            case ActiveSkillEffectType.FireAoe:
            case ActiveSkillEffectType.FireLine:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("timedAoeLevels"), true);
                break;
            case ActiveSkillEffectType.ArrowRain:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("timedArrowLevels"), true);
                break;
            case ActiveSkillEffectType.Cyclone:
            case ActiveSkillEffectType.Wave:
                DrawWaveLevels(serializedObject.FindProperty("waveLevels"));
                break;
            case ActiveSkillEffectType.ChargeAttackShockwave:
                DrawChargeAttackShockwaveLevels(serializedObject.FindProperty("chargeAttackShockwaveLevels"));
                break;
            case ActiveSkillEffectType.Disease:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("diseaseLevels"), true);
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawWaveLevels(SerializedProperty levels)
    {
        if (levels == null) return;

        EditorGUILayout.PropertyField(levels.FindPropertyRelative("Array.size"), new GUIContent("等级数"));
        for (int i = 0; i < levels.arraySize; i++)
        {
            var level = levels.GetArrayElementAtIndex(i);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Lv.{i + 1}", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(level.FindPropertyRelative("rangeRows"), new GUIContent("范围排数"));
            EditorGUILayout.PropertyField(level.FindPropertyRelative("damage"), new GUIContent("伤害"));
            EditorGUILayout.PropertyField(level.FindPropertyRelative("bossPoiseDamagePercent"), new GUIContent("Boss最大架势削减比例"));
            EditorGUILayout.PropertyField(level.FindPropertyRelative("landingDamage"), new GUIContent("落地伤害（仅Cyclone）"));
            EditorGUILayout.PropertyField(level.FindPropertyRelative("cycloneDuration"), new GUIContent("旋风持续时间"));
            EditorGUILayout.EndVertical();
        }
    }

    private static void DrawChargeAttackShockwaveLevels(SerializedProperty levels)
    {
        if (levels == null) return;

        EditorGUILayout.PropertyField(levels.FindPropertyRelative("Array.size"), new GUIContent("等级数"));
        for (int i = 0; i < levels.arraySize; i++)
        {
            var level = levels.GetArrayElementAtIndex(i);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Lv.{i + 1}", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(level.FindPropertyRelative("rangeRows"), new GUIContent("范围排数"));
            EditorGUILayout.PropertyField(level.FindPropertyRelative("damage"), new GUIContent("伤害"));
            EditorGUILayout.EndVertical();
        }
    }
}
