---
id: kd_a4d7e047-878e-4a54-9f47-d32bcff7ad82
type: memory
path: unity-project-understanding/boss-player-attack-range.md
title: boss-player-attack-range
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1784125979010
updatedAt: 1784125979011
---

# boss-player-attack-range

## Summary
Stab 与 Slash/Sweep 的战斗中 Boss 范围例外保持一致。

<!-- locus:body:start -->
## 战斗中 Boss 的玩家攻击范围例外
- `ColumnManager.GetEnemiesInRange` / `GetAllEnemiesInRange` 已允许 `BossState.InCombat` 的 Boss 无视普通 `rangeRows`，供 Slash/Sweep 使用。
- Stab 需同步此规则，且不能只放宽伤害：`AttackSystem.ExecuteStab()` 必须把射线视觉长度延长至目标列 InCombat Boss 的 `rowIndex + 1`；`StabSweepEffect` 用普通 `rangeRows` 过滤普通敌人，但允许 InCombat Boss，并用延长的视觉排数换算命中距离。
- 非战斗 Boss 继续不可命中；普通敌人仍严格受 Stab range 限制。
<!-- locus:body:end -->
