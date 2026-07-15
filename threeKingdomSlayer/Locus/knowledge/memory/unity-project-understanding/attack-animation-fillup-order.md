---
id: kd_b50736d7-6834-430f-8e21-e5e780c02a81
type: memory
path: unity-project-understanding/attack-animation-fillup-order.md
title: attack-animation-fillup-order
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1784124647579
updatedAt: 1784125212440
---

# attack-animation-fillup-order

## Summary
波次补齐与攻击动作的先后规则。

<!-- locus:body:start -->
## 波次补齐不得中断攻击动作
- `ColumnManager.BeginWaveStep()` 为波次补齐标记敌人的 `targetRow` / `pendingRushMove` 时，若 `isAttackAnimating=true`，不得调用 `ResetMovementState()`；后者会 Kill `_attackTween`，导致远程敌人蓄力/发射表现和攻击被取消。
- 正常攻击结束的回调已调用 `TryStartRushMove()`，可自然接入波次 `_pendingWaveEnemies` 屏障；整排后续行军继续等待该敌人完成补位。
- 仅攻击动画阶段延后补齐；攻击冷却可照旧被补齐打断。眩晕、击飞、死亡继续使用原有中断/恢复/移除链路。
- Rush 移动因目标排已被占用而回退时，必须清除本次 `pendingRushMove` / `targetRow` 并触发 `OnRushMoveComplete`；否则 `ColumnManager._pendingWaveEnemies` 或列链会永久等待。该敌人未前移，后续列结构变化仍会重新评估补齐。
<!-- locus:body:end -->
