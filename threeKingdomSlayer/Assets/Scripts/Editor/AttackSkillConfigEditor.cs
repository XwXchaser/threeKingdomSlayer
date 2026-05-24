using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AttackSkillConfig))]
public class AttackSkillConfigEditor : Editor
{
    private SerializedProperty idProp;
    private SerializedProperty attackTypeProp;
    private SerializedProperty damageTypeProp;
    private SerializedProperty damageProp;
    private SerializedProperty poiseDamageProp;
    private SerializedProperty rangeRowsProp;
    private SerializedProperty cooldownProp;
    private SerializedProperty launchDurationProp;
    private SerializedProperty attackWavePrefabProp;
    private SerializedProperty stabSpawnYOffsetProp;
    private SerializedProperty stabSpawnZOffsetProp;
    private SerializedProperty slashSweepHalfWidthProp;
    private SerializedProperty slashSweepAngleProp;
    private SerializedProperty slashSweepDurationProp;
    private SerializedProperty slashSpawnYOffsetProp;
    private SerializedProperty slashSpawnZOffsetProp;
    private SerializedProperty ultimateEnergyGainProp;

    private void OnEnable()
    {
        idProp = serializedObject.FindProperty("id");
        attackTypeProp = serializedObject.FindProperty("attackType");
        damageTypeProp = serializedObject.FindProperty("damageType");
        damageProp = serializedObject.FindProperty("damage");
        poiseDamageProp = serializedObject.FindProperty("poiseDamage");
        rangeRowsProp = serializedObject.FindProperty("rangeRows");
        cooldownProp = serializedObject.FindProperty("cooldown");
        launchDurationProp = serializedObject.FindProperty("launchDuration");
        attackWavePrefabProp = serializedObject.FindProperty("attackWavePrefab");
        stabSpawnYOffsetProp = serializedObject.FindProperty("stabSpawnYOffset");
        stabSpawnZOffsetProp = serializedObject.FindProperty("stabSpawnZOffset");
        slashSweepHalfWidthProp = serializedObject.FindProperty("slashSweepHalfWidth");
        slashSweepAngleProp = serializedObject.FindProperty("slashSweepAngle");
        slashSweepDurationProp = serializedObject.FindProperty("slashSweepDuration");
        slashSpawnYOffsetProp = serializedObject.FindProperty("slashSpawnYOffset");
        slashSpawnZOffsetProp = serializedObject.FindProperty("slashSpawnZOffset");
        ultimateEnergyGainProp = serializedObject.FindProperty("ultimateEnergyGain");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 基本信息
        EditorGUILayout.LabelField("基本信息", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(idProp);
        EditorGUILayout.PropertyField(attackTypeProp);
        EditorGUILayout.PropertyField(damageTypeProp);
        EditorGUILayout.Space();

        // 伤害
        EditorGUILayout.LabelField("伤害", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(damageProp);
        EditorGUILayout.PropertyField(poiseDamageProp);
        EditorGUILayout.Space();

        // 范围与冷却
        EditorGUILayout.LabelField("范围与冷却", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(rangeRowsProp);
        EditorGUILayout.PropertyField(cooldownProp);
        EditorGUILayout.Space();

        // 特效
        EditorGUILayout.LabelField("特效", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(attackWavePrefabProp);
        EditorGUILayout.Space();

        // 按 attackType 显示专属参数
        AttackType at = (AttackType)attackTypeProp.enumValueIndex;
        switch (at)
        {
            case AttackType.Launch:
                EditorGUILayout.LabelField("挑飞特殊参数", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(launchDurationProp);
                break;

            case AttackType.Stab:
                EditorGUILayout.LabelField("戳击偏移", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(stabSpawnYOffsetProp);
                EditorGUILayout.PropertyField(stabSpawnZOffsetProp);
                break;

            case AttackType.Slash:
                EditorGUILayout.LabelField("斩击扇形扫掠", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(slashSweepHalfWidthProp);
                EditorGUILayout.PropertyField(slashSweepAngleProp);
                EditorGUILayout.PropertyField(slashSweepDurationProp);
                EditorGUILayout.PropertyField(slashSpawnYOffsetProp);
                EditorGUILayout.PropertyField(slashSpawnZOffsetProp);
                break;
        }

        EditorGUILayout.Space();

        // 大招
        EditorGUILayout.LabelField("大招", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(ultimateEnergyGainProp);

        serializedObject.ApplyModifiedProperties();
    }
}
