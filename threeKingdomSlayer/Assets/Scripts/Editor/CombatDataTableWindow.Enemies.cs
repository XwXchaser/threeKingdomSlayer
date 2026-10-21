using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


/// <summary>
/// 战斗数据表的**敌人 Tab**：敌人列表、表头与行绘制。
///
/// 按机制从 <c>CombatDataTableWindow.cs</c> 拆出（2026-10-22，partial 分文件）。
/// partial class 与 CombatDataTableWindow.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
/// </summary>

public sealed partial class CombatDataTableWindow : EditorWindow
{
    private void DrawEnemies()
    {
        DrawEnemyHeader();
        var entries = new List<EnemyEntry>(_enemies);
        entries.Sort(CompareEnemies);

        foreach (var entry in entries)
        {
            if (!Matches(entry.Enemy.enemyName, entry.Enemy.enemyId.ToString(), entry.Prefab.name))
                continue;
            DrawEnemyRow(entry);
        }
    }

    private void DrawEnemyHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        DrawSortButton("ID", EnemySort.Id, IdWidth);
        DrawSortButton("名称", EnemySort.Name, NameWidth);
        DrawSortButton("生命", EnemySort.Health, NumberWidth);
        DrawSortButton("伤害", EnemySort.Damage, NumberWidth);
        GUILayout.Label("攻速", GUILayout.Width(NumberWidth));
        GUILayout.Label("移速", GUILayout.Width(NumberWidth));
        GUILayout.Label("射程", GUILayout.Width(NumberWidth));
        GUILayout.Label("架势", GUILayout.Width(NumberWidth));
        GUILayout.Label("金币", GUILayout.Width(NumberWidth));
        GUILayout.Label("经验", GUILayout.Width(NumberWidth));
        GUILayout.Label("远程", GUILayout.Width(45f));
        GUILayout.Label("Boss", GUILayout.Width(40f));
        GUILayout.Label("详情", GUILayout.Width(ButtonWidth));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawEnemyRow(EnemyEntry entry)
    {
        var enemy = entry.Enemy;
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        int id = EditorGUILayout.IntField(enemy.enemyId, GUILayout.Width(IdWidth));
        string name = EditorGUILayout.TextField(enemy.enemyName, GUILayout.Width(NameWidth));
        float health = EditorGUILayout.FloatField(enemy.maxHealth, GUILayout.Width(NumberWidth));
        float damage = EditorGUILayout.FloatField(enemy.attackDamage, GUILayout.Width(NumberWidth));
        float speed = EditorGUILayout.FloatField(enemy.attackSpeed, GUILayout.Width(NumberWidth));
        float moveSpeed = EditorGUILayout.FloatField(enemy.moveSpeed, GUILayout.Width(NumberWidth));
        float range = EditorGUILayout.FloatField(enemy.attackRange, GUILayout.Width(NumberWidth));
        float poise = EditorGUILayout.FloatField(enemy.maxPoise, GUILayout.Width(NumberWidth));
        int coin = EditorGUILayout.IntField(enemy.coinReward, GUILayout.Width(NumberWidth));
        float exp = EditorGUILayout.FloatField(enemy.expReward, GUILayout.Width(NumberWidth));
        bool ranged = EditorGUILayout.Toggle(enemy.isRanged, GUILayout.Width(45f));
        bool boss = EditorGUILayout.Toggle(enemy.isBoss, GUILayout.Width(40f));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(enemy, "编辑敌人数值");
            enemy.enemyId = id;
            enemy.enemyName = name;
            enemy.maxHealth = health;
            enemy.attackDamage = damage;
            enemy.attackSpeed = speed;
            enemy.moveSpeed = moveSpeed;
            enemy.attackRange = range;
            enemy.maxPoise = poise;
            enemy.coinReward = coin;
            enemy.expReward = exp;
            enemy.isRanged = ranged;
            enemy.isBoss = boss;
            EditorUtility.SetDirty(enemy);
        }
        DrawDetailsButton(entry.Prefab);
        EditorGUILayout.EndHorizontal();
    }
}