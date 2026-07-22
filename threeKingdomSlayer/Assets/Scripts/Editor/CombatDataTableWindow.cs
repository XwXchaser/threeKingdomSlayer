using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class CombatDataTableWindow : EditorWindow
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

    private void OnGUI()
    {
        DrawToolbar();
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        if (_tab == Tab.Enemies)
            DrawEnemies();
        else if (_tab == Tab.Skills)
            DrawSkills();
        else
            DrawUpgrades();
        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        var nextTab = (Tab)GUILayout.Toolbar((int)_tab, new[] { "敌人", "技能 / 大招", "三选一升级" }, EditorStyles.toolbarButton, GUILayout.Width(270f));
        if (nextTab != _tab)
        {
            _tab = nextTab;
            _scroll = Vector2.zero;
        }

        GUILayout.FlexibleSpace();
        GUILayout.Label("搜索", EditorStyles.miniLabel);
        string nextSearch = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.Width(220f));
        if (nextSearch != _search)
            _search = nextSearch;
        if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(48f)))
            Refresh();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawUpgrades()
    {
        DrawUpgradeDefinitions();
        EditorGUILayout.Space(14f);
        DrawUpgradePools();
    }

    private void DrawUpgradeDefinitions()
    {
        EditorGUILayout.LabelField("升级定义", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        GUILayout.Label("名称", GUILayout.Width(CompactTextWidth));
        GUILayout.Label("ID", GUILayout.Width(CompactTextWidth));
        GUILayout.Label("类型", GUILayout.Width(100f));
        GUILayout.Label("稀有度", GUILayout.Width(90f));
        GUILayout.Label("最高等级", GUILayout.Width(70f));
        GUILayout.Label("效果类型", GUILayout.Width(120f));
        GUILayout.Label("展开", GUILayout.Width(48f));
        EditorGUILayout.EndHorizontal();

        foreach (var upgrade in _upgrades)
        {
            if (!Matches(upgrade.displayName, upgrade.upgradeId, upgrade.effectType, upgrade.category.ToString()))
                continue;

            EditorGUILayout.BeginHorizontal();
            string controlPrefix = "Upgrade_" + upgrade.GetInstanceID();
            EditorGUI.BeginChangeCheck();
            string displayName = DrawCompactTextField(controlPrefix + "_Name", upgrade.displayName);
            string upgradeId = DrawCompactTextField(controlPrefix + "_Id", upgrade.upgradeId);
            var category = (UpgradeCategory)EditorGUILayout.EnumPopup(upgrade.category, GUILayout.Width(100f));
            var rarity = (UpgradeRarity)EditorGUILayout.EnumPopup(upgrade.rarity, GUILayout.Width(90f));
            int maxLevel = EditorGUILayout.IntField(upgrade.maxLevel, GUILayout.Width(70f));
            string effectType = DrawCompactTextField(controlPrefix + "_Effect", upgrade.effectType, 120f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(upgrade, "编辑升级定义");
                upgrade.displayName = displayName;
                upgrade.upgradeId = upgradeId;
                upgrade.category = category;
                upgrade.rarity = rarity;
                upgrade.maxLevel = maxLevel;
                upgrade.effectType = effectType;
                EditorUtility.SetDirty(upgrade);
            }
            bool expanded = GetUpgradeDetailsExpanded(upgrade);
            bool nextExpanded = GUILayout.Toggle(expanded, expanded ? "收起" : "展开", EditorStyles.miniButton, GUILayout.Width(48f));
            if (nextExpanded != expanded)
                _upgradeDetailsExpanded[upgrade.GetInstanceID()] = nextExpanded;
            EditorGUILayout.EndHorizontal();
            if (nextExpanded)
                DrawUpgradeEffectDetails(upgrade);
        }
    }

    private void DrawUpgradeEffectDetails(UpgradeDefinition upgrade)
    {
        var serializedUpgrade = new SerializedObject(upgrade);
        serializedUpgrade.Update();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("描述与效果配置", EditorStyles.miniBoldLabel);
        var description = serializedUpgrade.FindProperty("descriptionTemplate");
        var extraDescription = serializedUpgrade.FindProperty("extraDescriptionTemplate");
        EditorGUILayout.LabelField("主说明", EditorStyles.miniLabel);
        description.stringValue = DrawCompactTextField("Upgrade_" + upgrade.GetInstanceID() + "_Description", description.stringValue, 260f);
        EditorGUILayout.LabelField("补充说明", EditorStyles.miniLabel);
        extraDescription.stringValue = DrawCompactTextField("Upgrade_" + upgrade.GetInstanceID() + "_ExtraDescription", extraDescription.stringValue, 260f);

        var effectProperty = GetEffectProperty(serializedUpgrade, upgrade);
        if (effectProperty != null)
            DrawCompactEffectLevels(effectProperty, upgrade);
        else
        {
            EditorGUILayout.HelpBox("该效果使用基础字段或专属资产，请在详情 Inspector 中编辑。", MessageType.None);
        }

        serializedUpgrade.ApplyModifiedProperties();
        EditorGUILayout.EndVertical();
    }

    private void DrawCompactEffectLevels(SerializedProperty property, UpgradeDefinition upgrade)
    {
        string effectType = upgrade.effectType;
        EditorGUILayout.LabelField(GetEffectLabel(effectType), EditorStyles.miniBoldLabel);
        if (!property.isArray)
        {
            DrawEffectFields(property, upgrade);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("+ 等级", GUILayout.Width(56f)))
            property.InsertArrayElementAtIndex(property.arraySize);
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < property.arraySize; i++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Lv." + (i + 1), GUILayout.Width(34f));
            DrawEffectFields(property.GetArrayElementAtIndex(i), upgrade);
            if (GUILayout.Button("-", GUILayout.Width(22f)))
            {
                property.DeleteArrayElementAtIndex(i);
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawEffectFields(SerializedProperty level, UpgradeDefinition upgrade)
    {
        string effectType = upgrade.effectType;
        switch (effectType)
        {
            case "passive_phantom_weapon":
                DrawField(level, upgrade, "intervalSeconds", "间隔");
                DrawField(level, upgrade, "triggerParam", "阈值");
                DrawField(level, upgrade, "attackType", "攻击");
                DrawField(level, upgrade, "targetColumn", "目标列");
                DrawComplexField(level, upgrade, "phantomSteps", "段数");
                break;
            case "passive_timed_aoe":
                DrawField(level, upgrade, "intervalSeconds", "间隔");
                DrawField(level, upgrade, "triggerThreshold", "阈值");
                DrawField(level, upgrade, "damage", "伤害");
                DrawComplexField(level, upgrade, "columns", "列");
                break;
            case "passive_timed_arrow":
                DrawField(level, upgrade, "intervalSeconds", "间隔");
                DrawField(level, upgrade, "triggerThreshold", "阈值");
                DrawField(level, upgrade, "rowCount", "排数");
                DrawField(level, upgrade, "arrowCount", "箭数");
                DrawField(level, upgrade, "damage", "伤害");
                break;
            case "passive_return_wave":
            case "passive_chain_bounce":
                DrawField(level, upgrade, "intervalSeconds", "间隔");
                DrawField(level, upgrade, "triggerThreshold", "阈值");
                DrawField(level, upgrade, "column", "列");
                DrawField(level, upgrade, effectType == "passive_return_wave" ? "rangeRows" : "maxBounces", effectType == "passive_return_wave" ? "排数" : "弹射");
                DrawField(level, upgrade, "damageRatio", "伤害比");
                break;
            case "passive_timed_cyclone":
                DrawField(level, upgrade, "intervalSeconds", "间隔");
                DrawField(level, upgrade, "enemyCount", "敌数");
                DrawField(level, upgrade, "knockupDuration", "击飞");
                DrawField(level, upgrade, "damage", "伤害");
                DrawField(level, upgrade, "landingDamagePercent", "落地%");
                break;
            case "passive_arrow_volley":
                DrawField(level, upgrade, "triggerThreshold", "阈值");
                DrawField(level, upgrade, "targetCount", "目标");
                DrawField(level, upgrade, "arrowCount", "箭数");
                DrawField(level, upgrade, "baseDamage", "伤害");
                break;
            case "charge_reflect_shield":
                DrawField(level, upgrade, "intervalSeconds", "间隔");
                DrawField(level, upgrade, "shieldAmount", "护盾");
                DrawField(level, upgrade, "enableBonus", "额外");
                DrawField(level, upgrade, "bonusReflectPercent", "反伤%");
                break;
            case "charge_shockwave":
                DrawField(level, upgrade, "intervalSeconds", "间隔");
                DrawField(level, upgrade, "shockwaveCount", "波数");
                DrawField(level, upgrade, "rangeRows", "排数");
                DrawField(level, upgrade, "baseDamage", "伤害");
                DrawField(level, upgrade, "stackDamageBonus", "层加成");
                DrawField(level, upgrade, "waveDelay", "波延迟");
                break;
            case "charge_hit_shockwave":
                DrawField(level, upgrade, "shockwaveCount", "波数");
                DrawField(level, upgrade, "baseDamage", "伤害");
                DrawField(level, upgrade, "rangeRows", "排数");
                DrawField(level, upgrade, "damageBonusPerHit", "受击加成");
                break;
            case "item_cyclone":
                DrawField(level, upgrade, "durationSeconds", "持续");
                DrawField(level, upgrade, "intervalSeconds", "间隔");
                DrawField(level, upgrade, "cooldownSeconds", "冷却");
                DrawField(level, upgrade, "rowCount", "排数");
                DrawField(level, upgrade, "initialDamage", "伤害");
                DrawField(level, upgrade, "landingDamagePercent", "落地%");
                break;
            default:
                DrawField(level, upgrade, "floatValue", "浮点");
                DrawField(level, upgrade, "intValue", "整数");
                DrawField(level, upgrade, "secondaryIntValue", "整数2");
                break;
        }
    }

    private void DrawField(SerializedProperty parent, UpgradeDefinition upgrade, string name, string label)
    {
        if (!IsFieldMentioned(upgrade, name)) return;
        var property = parent.FindPropertyRelative(name);
        if (property == null) return;

        EditorGUILayout.BeginVertical(GUILayout.Width(72f));
        GUILayout.Label(label, EditorStyles.miniLabel);
        EditorGUILayout.PropertyField(property, GUIContent.none, GUILayout.Width(72f));
        EditorGUILayout.EndVertical();
    }

    private void DrawComplexField(SerializedProperty parent, UpgradeDefinition upgrade, string name, string label)
    {
        if (!IsFieldMentioned(upgrade, name)) return;
        var property = parent.FindPropertyRelative(name);
        if (property == null) return;
        EditorGUILayout.LabelField(label + ": " + property.arraySize, GUILayout.Width(58f));
    }
    private bool IsFieldMentioned(UpgradeDefinition upgrade, string fieldName)
    {
        int placeholder = GetPlaceholderIndex(upgrade.effectType, fieldName);
        if (placeholder < 0) return false;
        string token = "{" + placeholder + "}";
        return (upgrade.descriptionTemplate != null && upgrade.descriptionTemplate.Contains(token))
            || (upgrade.extraDescriptionTemplate != null && upgrade.extraDescriptionTemplate.Contains(token));
    }

    private int GetPlaceholderIndex(string effectType, string fieldName)
    {
        switch (effectType)
        {
            case "passive_phantom_weapon":
                return fieldName == "intervalSeconds" || fieldName == "triggerParam" ? 0 : fieldName == "phantomSteps" ? 1 : -1;
            case "passive_timed_aoe":
                return fieldName == "intervalSeconds" || fieldName == "triggerThreshold" ? 0 : fieldName == "damage" ? 1 : -1;
            case "passive_timed_arrow":
                return fieldName == "intervalSeconds" || fieldName == "triggerThreshold" ? 0 : fieldName == "rowCount" ? 1 : fieldName == "arrowCount" ? 2 : fieldName == "damage" ? 3 : -1;
            case "passive_return_wave":
                return fieldName == "intervalSeconds" || fieldName == "triggerThreshold" ? 0 : fieldName == "damageRatio" ? 1 : -1;
            case "passive_chain_bounce":
                return fieldName == "intervalSeconds" || fieldName == "triggerThreshold" ? 0 : fieldName == "maxBounces" ? 1 : fieldName == "damageRatio" ? 2 : -1;
            case "passive_timed_cyclone":
                return fieldName == "intervalSeconds" ? 0 : fieldName == "enemyCount" ? 1 : fieldName == "knockupDuration" ? 2 : -1;
            case "passive_arrow_volley":
                return fieldName == "triggerThreshold" ? 0 : fieldName == "targetCount" ? 1 : fieldName == "arrowCount" ? 2 : -1;
            case "charge_reflect_shield":
                return fieldName == "intervalSeconds" ? 0 : fieldName == "shieldAmount" ? 1 : (fieldName == "enableBonus" || fieldName == "bonusReflectPercent") ? 2 : -1;
            case "charge_shockwave":
                return fieldName == "intervalSeconds" ? 0 : fieldName == "shockwaveCount" ? 1 : fieldName == "rangeRows" ? 2 : fieldName == "baseDamage" ? 3 : fieldName == "stackDamageBonus" ? 4 : -1;
            case "charge_hit_shockwave":
                return fieldName == "shockwaveCount" ? 0 : fieldName == "baseDamage" ? 1 : fieldName == "rangeRows" ? 2 : fieldName == "damageBonusPerHit" ? 3 : -1;
            case "item_cyclone":
                return fieldName == "durationSeconds" ? 0 : fieldName == "intervalSeconds" ? 1 : fieldName == "rowCount" ? 2 : -1;
            case "stab_range_boost":
            case "sweep_range_boost":
                return fieldName == "intValue" ? 0 : fieldName == "secondaryIntValue" ? 1 : -1;
            case "push_wave":
            case "convergence_wave":
                return fieldName == "intValue" ? 0 : fieldName == "floatValue" ? 1 : -1;
            case "charge_damage_reduction":
                return fieldName == "floatValue" ? 0 : -1;
            case "spike_trap":
                return fieldName == "floatValue" ? 0 : fieldName == "intValue" ? 1 : fieldName == "secondaryIntValue" ? 2 : -1;
            default:
                return fieldName == "floatValue" ? 0 : fieldName == "intValue" ? 1 : -1;
        }
    }
    private bool GetUpgradeDetailsExpanded(UpgradeDefinition upgrade)
    {
        int id = upgrade.GetInstanceID();
        if (!_upgradeDetailsExpanded.TryGetValue(id, out bool expanded))
        {
            expanded = true;
            _upgradeDetailsExpanded.Add(id, true);
        }
        return expanded;
    }

    private string DrawCompactTextField(string controlName, string value, float compactWidth = CompactTextWidth)
    {
        bool focused = GUI.GetNameOfFocusedControl() == controlName;
        GUI.SetNextControlName(controlName);
        return EditorGUILayout.TextField(value, GUILayout.Width(focused ? ExpandedTextWidth : compactWidth));
    }
    private SerializedProperty GetEffectProperty(SerializedObject serializedUpgrade, UpgradeDefinition upgrade)
    {
        SerializedProperty property = null;
        switch (upgrade.effectType)
        {
            case "passive_phantom_weapon": property = serializedUpgrade.FindProperty("phantomLevels"); break;
            case "passive_timed_aoe": property = serializedUpgrade.FindProperty("timedAoeLevels"); break;
            case "passive_timed_arrow": property = serializedUpgrade.FindProperty("timedArrowLevels"); break;
            case "passive_return_wave": property = serializedUpgrade.FindProperty("returnWaveLevels"); break;
            case "passive_chain_bounce": property = serializedUpgrade.FindProperty("chainBounceLevels"); break;
            case "passive_timed_cyclone": property = serializedUpgrade.FindProperty("cycloneLevels"); break;
            case "passive_arrow_volley": property = serializedUpgrade.FindProperty("arrowVolleyLevels"); break;
            case "charge_reflect_shield": property = serializedUpgrade.FindProperty("reflectShieldLevels"); break;
            case "charge_shockwave": property = serializedUpgrade.FindProperty("chargeShockwaveLevels"); break;
            case "charge_hit_shockwave": property = serializedUpgrade.FindProperty("chargeHitShockwaveLevels"); break;
            case "item_cyclone": property = serializedUpgrade.FindProperty("cycloneItemConfig"); break;
        }

        var numericLevels = serializedUpgrade.FindProperty("numericLevels");
        if (upgrade.category == UpgradeCategory.Numeric || (property != null && property.isArray && property.arraySize == 0 && numericLevels.arraySize > 0))
            return numericLevels;
        return property;
    }

    private string GetEffectLabel(string effectType)
    {
        return string.IsNullOrEmpty(effectType) ? "效果配置" : "效果配置（" + effectType + "）";
    }
    private void DrawUpgradePools()
    {
        EditorGUILayout.LabelField("三选一升级池", EditorStyles.boldLabel);
        if (_upgradePools.Count == 0)
        {
            EditorGUILayout.HelpBox("未找到 UpgradePoolConfig。", MessageType.Info);
            return;
        }

        string[] names = new string[_upgradePools.Count];
        for (int i = 0; i < names.Length; i++) names[i] = _upgradePools[i].name;
        _selectedPoolIndex = Mathf.Clamp(_selectedPoolIndex, 0, _upgradePools.Count - 1);
        _selectedPoolIndex = EditorGUILayout.Popup("配置资产", _selectedPoolIndex, names);
        var pool = _upgradePools[_selectedPoolIndex];

        EditorGUI.BeginChangeCheck();
        float commonWeight = EditorGUILayout.FloatField("普通出现权重", pool.commonWeight);
        float rareWeight = EditorGUILayout.FloatField("稀有出现权重", pool.rareWeight);
        float legendaryWeight = EditorGUILayout.FloatField("传说出现权重", pool.legendaryWeight);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(pool, "编辑升级池稀有度权重");
            pool.commonWeight = commonWeight;
            pool.rareWeight = rareWeight;
            pool.legendaryWeight = legendaryWeight;
            EditorUtility.SetDirty(pool);
        }

        DrawPoolList(pool, "普通池", "commonPool");
        DrawPoolList(pool, "稀有池", "rarePool");
        DrawPoolList(pool, "传说池", "legendaryPool");
        DrawDetailsButton(pool);
    }

    private void DrawPoolList(UpgradePoolConfig pool, string label, string propertyPath)
    {
        var serializedPool = new SerializedObject(pool);
        var entries = serializedPool.FindProperty(propertyPath);
        EditorGUILayout.Space(5f);
        EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
        for (int i = 0; i < entries.arraySize; i++)
        {
            var entry = entries.GetArrayElementAtIndex(i);
            var upgrade = entry.FindPropertyRelative("upgrade");
            var weight = entry.FindPropertyRelative("weight");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(upgrade, GUIContent.none, GUILayout.MinWidth(240f));
            weight.intValue = EditorGUILayout.IntField(weight.intValue, GUILayout.Width(70f));
            if (GUILayout.Button("移除", GUILayout.Width(ButtonWidth)))
            {
                entries.DeleteArrayElementAtIndex(i);
                serializedPool.ApplyModifiedProperties();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("添加到 " + label, GUILayout.Width(110f)))
        {
            entries.InsertArrayElementAtIndex(entries.arraySize);
            var entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            entry.FindPropertyRelative("upgrade").objectReferenceValue = null;
            entry.FindPropertyRelative("weight").intValue = 1;
        }
        serializedPool.ApplyModifiedProperties();
    }
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
