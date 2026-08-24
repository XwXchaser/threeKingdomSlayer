---
id: kd_a4d7e047-878e-4a54-9f47-d32bcff7ad82
injectMode: inherit
summary: Stab 与 Slash/Sweep 的战斗中 Boss 范围例外保持一致。
aiEditMode: inherit
---

## 战斗中 Boss 的玩家攻击范围例外
- `ColumnManager.GetEnemiesInRange` / `GetAllEnemiesInRange` 已允许 `BossState.InCombat` 的 Boss 无视普通 `rangeRows`，供 Slash/Sweep 使用。
- Stab 需同步此规则，且不能只放宽伤害：`AttackSystem.ExecuteStab()` 必须把射线视觉长度延长至目标列 InCombat Boss 的 `rowIndex + 1`；`StabSweepEffect` 用普通 `rangeRows` 过滤普通敌人，但允许 InCombat Boss，并用延长的视觉排数换算命中距离。
- 非战斗 Boss 继续不可命中；普通敌人仍严格受 Stab range 限制。

## Boss horizontal Stab coverage (2026-07)
- `Enemy.occupySlots` is the serialized horizontal footprint value. Boss 104 is configured to `5`, but it remains stored only in its center formation column; spawning, movement, fill-up, and other column logic remain unchanged.
- `ColumnManager.GetCombatBossCoveringColumn(column)` derives the InCombat Boss footprint from its center column and `occupySlots`, clamped inside the five-column board.
- `AttackSystem.ExecuteStab` queries that coverage for the tapped column and passes the Boss as an explicit extra target to `StabSweepEffect`; the ray extends to the Boss row for visual reach.
- `StabSweepEffect` retains normal column scanning for ordinary enemies, then appends the covered Boss once its ray reaches the end. Its existing HashSet prevents duplicate damage in a single thrust.
- Verified Play Mode with Boss 104 set to InCombat: each of columns 0–4 resolves exactly one 10-damage Stab hit on the same Boss.
