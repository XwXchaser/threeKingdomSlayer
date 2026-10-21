using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


/// <summary>
/// 战斗数据表的**技能 Tab**：招式与终极技能列表。
///
/// 按机制从 <c>CombatDataTableWindow.cs</c> 拆出（2026-10-22，partial 分文件）。
/// partial class 与 CombatDataTableWindow.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
/// </summary>

public sealed partial class CombatDataTableWindow : EditorWindow
{
    private void DrawSkills()
    {
        DrawSkillHeader();
        var attacks = new List<AttackSkillConfig>(_attackSkills);
        attacks.Sort(CompareAttackSkills);
        foreach (var skill in attacks)
        {
            if (Matches(skill.name, skill.id.ToString(), skill.attackType.ToString()))
                DrawAttackSkillRow(skill);
        }

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("大招", EditorStyles.boldLabel);
        foreach (var skill in _ultimateSkills)
        {
            if (Matches(skill.name, skill.id.ToString(), skill.damageType.ToString()))
                DrawUltimateSkillRow(skill);
        }
    }

    private void DrawSkillHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        GUILayout.Label("类型", GUILayout.Width(80f));
        GUILayout.Label("资产", GUILayout.Width(150f));
        GUILayout.Label("ID", GUILayout.Width(IdWidth));
        GUILayout.Label("伤害类型", GUILayout.Width(90f));
        GUILayout.Label("伤害", GUILayout.Width(NumberWidth));
        GUILayout.Label("架势伤害", GUILayout.Width(NumberWidth + 20f));
        GUILayout.Label("范围", GUILayout.Width(NumberWidth));
        DrawSortButton("冷却", SkillSort.Cooldown, NumberWidth);
        GUILayout.Label("动作时长", GUILayout.Width(NumberWidth + 10f));
        GUILayout.Label("能量", GUILayout.Width(NumberWidth));
        GUILayout.Label("详情", GUILayout.Width(ButtonWidth));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawAttackSkillRow(AttackSkillConfig skill)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(skill.attackType.ToString(), GUILayout.Width(80f));
        GUILayout.Label(skill.name, GUILayout.Width(150f));
        EditorGUI.BeginChangeCheck();
        int id = EditorGUILayout.IntField(skill.id, GUILayout.Width(IdWidth));
        var damageType = (DamageType)EditorGUILayout.EnumPopup(skill.damageType, GUILayout.Width(90f));
        float damage = EditorGUILayout.FloatField(skill.damage, GUILayout.Width(NumberWidth));
        float poise = EditorGUILayout.FloatField(skill.poiseDamage, GUILayout.Width(NumberWidth + 20f));
        int range = EditorGUILayout.IntField(skill.rangeRows, GUILayout.Width(NumberWidth));
        float cooldown = EditorGUILayout.FloatField(skill.cooldown, GUILayout.Width(NumberWidth));
        float actionDuration = EditorGUILayout.FloatField(skill.actionDuration, GUILayout.Width(NumberWidth + 10f));
        int energy = EditorGUILayout.IntField(skill.ultimateEnergyGain, GUILayout.Width(NumberWidth));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(skill, "编辑技能数值");
            skill.id = id;
            skill.damageType = damageType;
            skill.damage = damage;
            skill.poiseDamage = poise;
            skill.rangeRows = range;
            skill.cooldown = cooldown;
            skill.actionDuration = actionDuration;
            skill.ultimateEnergyGain = energy;
            EditorUtility.SetDirty(skill);
        }
        DrawDetailsButton(skill);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawUltimateSkillRow(UltimateSkillConfig skill)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("大招", GUILayout.Width(80f));
        GUILayout.Label(skill.name, GUILayout.Width(150f));
        EditorGUI.BeginChangeCheck();
        int id = EditorGUILayout.IntField(skill.id, GUILayout.Width(IdWidth));
        var damageType = (DamageType)EditorGUILayout.EnumPopup(skill.damageType, GUILayout.Width(90f));
        float damage = EditorGUILayout.FloatField(skill.damage, GUILayout.Width(NumberWidth));
        GUILayout.Label("-", GUILayout.Width(NumberWidth + 20f));
        GUILayout.Label("-", GUILayout.Width(NumberWidth));
        float cooldown = EditorGUILayout.FloatField(skill.cooldown, GUILayout.Width(NumberWidth));
        GUILayout.Label("-", GUILayout.Width(NumberWidth + 10f));
        int energy = EditorGUILayout.IntField(skill.energyCost, GUILayout.Width(NumberWidth));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(skill, "编辑大招数值");
            skill.id = id;
            skill.damageType = damageType;
            skill.damage = damage;
            skill.cooldown = cooldown;
            skill.energyCost = energy;
            EditorUtility.SetDirty(skill);
        }
        DrawDetailsButton(skill);
        EditorGUILayout.EndHorizontal();
    }
}