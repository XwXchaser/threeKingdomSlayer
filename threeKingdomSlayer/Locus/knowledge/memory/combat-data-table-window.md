---
id: kd_82958919-59f4-477f-848b-d0e0a342ac41
injectMode: inherit
summary: 战斗数值总表 EditorWindow 已覆盖敌人、技能/大招、三选一升级定义与升级池；升级补充说明决定逐级数值表可见字段。
aiEditMode: auto
maintenanceRules: Keep only durable and reusable project memory
---

战斗数值总表已实现：`Assets/Scripts/Editor/CombatDataTableWindow.cs`。

入口：Tools > 三国杀戮 > 战斗数值总表。

提供敌人（`Assets/Resources/EnemyPrefabs`）、技能/大招（全项目 AttackSkillConfig / UltimateSkillConfig）和三选一升级三页：
- 敌人与技能：搜索、排序、直接数值编辑、Undo、详情定位原资产。
- 三选一升级：UpgradeDefinition 有主说明 `descriptionTemplate` 和补充说明 `extraDescriptionTemplate`；两者支持同一组占位符，`UpgradeEffectManager.GetDescription()` 会合并并替换，三选一卡片因此显示完整效果文本。总表中逐级效果只显示主说明或补充说明中实际引用占位符对应的字段，采用单等级单行紧凑布局；未提及的内部参数不显示。复杂数组只显示摘要，仍可通过详情 Inspector 编辑。
- 升级池：UpgradePoolConfig 的三档稀有度出现权重，以及普通/稀有/传说池的定义引用、单项权重、添加和移除。

已编译验证：7 个敌人、6 个普通技能、1 个大招、25 个升级定义、1 个升级池。
