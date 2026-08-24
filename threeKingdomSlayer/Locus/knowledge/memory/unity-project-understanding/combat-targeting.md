---
id: kd_a9b75e0f-5859-4368-9879-06246e0dbd46
injectMode: inherit
aiEditMode: inherit
---

# Combat Targeting

## ColumnManager.GetEnemiesInRange
- 核心方法：`GetEnemiesInRange(int columnIndex, int rangeRows)`
- 正常敌人按 `rowIndex < rangeRows` 过滤
- BOSS 敌人始终包含（`e.isBoss && e.bossState == BossState.InCombat`），不受 rangeRows 限制
- 确保 Parry/Swipe 等技能能命中已应战的 BOSS
