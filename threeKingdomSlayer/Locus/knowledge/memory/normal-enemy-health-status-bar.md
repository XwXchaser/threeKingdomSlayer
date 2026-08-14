---
id: kd_ed59dc1a-374b-43d0-b50f-ec83c0fa3c72
injectMode: inherit
summary: 普通敌人生命/状态条统一由 EnemyHealthBar.barWidth 控制；当前验收宽度为1.0。
aiMaintained: inherit
---

普通敌人头顶生命条与其运行时生成的灼烧/染病状态条共用 `EnemyHealthBar.barWidth`。不要仅改脚本默认值：各敌人 Prefab 已序列化该字段，必须使用 Unity API 批量写入并复核。

已验收的当前规范：普通敌人 `Enemy_1`、`Enemy_101`、`Enemy_102`、`Enemy_103`、`Enemy_105`、`Enemy_106_Fixed` 的 `barWidth=1.0`；Boss `Enemy_104`、`Enemy_107`、`Enemy_108` 不改，仍由独立 `BossHealthUI` 管理。状态条的宽度、左对齐填充和疾病层数文本位置均会从 `barWidth` 自动推导。
