using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed partial class CombatDataTableWindow : EditorWindow
{
    private enum Tab { Enemies, Skills, Upgrades }
    private enum EnemySort { Id, Name, Health, Damage, MoveSpeed }
    private enum SkillSort { Id, Type, Damage, Cooldown }

    private const float ButtonWidth = 52f;
    private const float IdWidth = 42f;
    private const float NameWidth = 120f;
    private const float NumberWidth = 68f;
    private const float CompactTextWidth = 92f;
    private const float ExpandedTextWidth = 220f;

    private readonly List<EnemyEntry> _enemies = new List<EnemyEntry>();
    private readonly List<AttackSkillConfig> _attackSkills = new List<AttackSkillConfig>();
    private readonly List<UltimateSkillConfig> _ultimateSkills = new List<UltimateSkillConfig>();
    private readonly List<UpgradeDefinition> _upgrades = new List<UpgradeDefinition>();
    private readonly List<UpgradePoolConfig> _upgradePools = new List<UpgradePoolConfig>();
    private readonly Dictionary<int, bool> _upgradeDetailsExpanded = new Dictionary<int, bool>();

    private Tab _tab;
    private EnemySort _enemySort;
    private SkillSort _skillSort;
    private bool _descending;
    private string _search = string.Empty;
    private Vector2 _scroll;
    private int _selectedPoolIndex;

    [MenuItem("Tools/三国杀戮/战斗数值总表")]
    private static void Open()
    {
        var window = GetWindow<CombatDataTableWindow>("战斗数值总表");
        window.minSize = new Vector2(900f, 360f);
        window.Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }



    private void DrawDetailsButton(UnityEngine.Object asset)
    {
        if (GUILayout.Button("详情", GUILayout.Width(ButtonWidth)))
        {
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }

    private void DrawSortButton(string label, EnemySort sort, float width)
    {
        if (GUILayout.Button(label + SortMark(_enemySort == sort), EditorStyles.label, GUILayout.Width(width)))
        {
            if (_enemySort == sort) _descending = !_descending;
            else { _enemySort = sort; _descending = false; }
        }
    }

    private void DrawSortButton(string label, SkillSort sort, float width)
    {
        if (GUILayout.Button(label + SortMark(_skillSort == sort), EditorStyles.label, GUILayout.Width(width)))
        {
            if (_skillSort == sort) _descending = !_descending;
            else { _skillSort = sort; _descending = false; }
        }
    }

    private string SortMark(bool selected)
    {
        return selected ? (_descending ? " ▼" : " ▲") : string.Empty;
    }

    private int CompareEnemies(EnemyEntry a, EnemyEntry b)
    {
        int result;
        switch (_enemySort)
        {
            case EnemySort.Name: result = string.Compare(a.Enemy.enemyName, b.Enemy.enemyName, StringComparison.OrdinalIgnoreCase); break;
            case EnemySort.Health: result = a.Enemy.maxHealth.CompareTo(b.Enemy.maxHealth); break;
            case EnemySort.Damage: result = a.Enemy.attackDamage.CompareTo(b.Enemy.attackDamage); break;
            case EnemySort.MoveSpeed: result = a.Enemy.moveSpeed.CompareTo(b.Enemy.moveSpeed); break;
            default: result = a.Enemy.enemyId.CompareTo(b.Enemy.enemyId); break;
        }
        return _descending ? -result : result;
    }

    private int CompareAttackSkills(AttackSkillConfig a, AttackSkillConfig b)
    {
        int result;
        switch (_skillSort)
        {
            case SkillSort.Type: result = a.attackType.CompareTo(b.attackType); break;
            case SkillSort.Damage: result = a.damage.CompareTo(b.damage); break;
            case SkillSort.Cooldown: result = a.cooldown.CompareTo(b.cooldown); break;
            default: result = a.id.CompareTo(b.id); break;
        }
        return _descending ? -result : result;
    }

    private bool Matches(params string[] values)
    {
        if (string.IsNullOrWhiteSpace(_search)) return true;
        foreach (string value in values)
        {
            if (!string.IsNullOrEmpty(value) && value.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    private void Refresh()
    {
        _enemies.Clear();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/EnemyPrefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var enemy = prefab != null ? prefab.GetComponent<Enemy>() : null;
            if (enemy != null)
                _enemies.Add(new EnemyEntry { Prefab = prefab, Enemy = enemy });
        }

        _attackSkills.Clear();
        foreach (string guid in AssetDatabase.FindAssets("t:AttackSkillConfig"))
        {
            var asset = AssetDatabase.LoadAssetAtPath<AttackSkillConfig>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) _attackSkills.Add(asset);
        }

        _ultimateSkills.Clear();
        foreach (string guid in AssetDatabase.FindAssets("t:UltimateSkillConfig"))
        {
            var asset = AssetDatabase.LoadAssetAtPath<UltimateSkillConfig>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) _ultimateSkills.Add(asset);
        }

        _upgrades.Clear();
        foreach (string guid in AssetDatabase.FindAssets("t:UpgradeDefinition"))
        {
            var asset = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) _upgrades.Add(asset);
        }

        _upgradePools.Clear();
        foreach (string guid in AssetDatabase.FindAssets("t:UpgradePoolConfig"))
        {
            var asset = AssetDatabase.LoadAssetAtPath<UpgradePoolConfig>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) _upgradePools.Add(asset);
        }

        Repaint();
    }

    private struct EnemyEntry
    {
        public GameObject Prefab;
        public Enemy Enemy;
    }
}
