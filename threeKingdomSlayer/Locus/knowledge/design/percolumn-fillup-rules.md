---
id: kd_05e6039b-c5f9-463c-87f9-59b43da2ce9a
type: design
path: percolumn-fillup-rules.md
title: percolumn-fillup-rules
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1783434126515
updatedAt: 1784124635818
---

# percolumn-fillup-rules

## Summary
PerColumn 补齐规则：三条铁律、实现约束及9个已修复坑点（最新：坑点9 PerRow 分支缺少 StartWaveMarch 调用）

## Content
# PerColumn 补齐规则

## 三条铁律

### 规则1：波次行军
波次生成后，所有敌人按配置排列向 row=0 推进。行军是**跨列整排**的——同一排的所有列敌人一起前进，阵型不变。

### 规则2：整排空出才补齐
仅当某排**所有列**的敌人全部空出（死亡或已前移，不包括 Launched），后排才整体前移一排。若某排仍有至少一个存活（非 Launched）敌人，后方排不前进。阵型空隙得以保留。

- "空出"的定义：该排所有列均无存活（非 Dead）且非 Launched 的敌人。
- 死亡是规则2的自然结果——全部死亡即整排空出。
- 攻击范围决定敌人何时停止行军进入攻击，与补齐规则无关。
- 行军终点为 row=0（最前排）。

### 规则3：仅击退触发列内紧凑
仅当敌人受到"击退"效果时，触发**列内**紧凑（`CompactByClearRows` / `CompactColumn`）。紧凑完成后，回到规则1/2的波次行军。

- 击退效果：`ApplyPushWave`、`ApplyDirectionalPush` 等位移技能。
- 击退 → `PostDisplacementFillUp` → `RowBasedFillUp` → 各列紧凑 → 各列 Rush 链 → 链全部结束后启动波次行军。

## Boss 排除
Boss 有独立的补齐规则（`BossPause`/`BossResume`/`IsRowClearForBoss`），不在以上三条规则讨论范围内。

## 击退与补齐的防重叠
当前击退机制已有防重叠保护：
1. `CanPushColumn` 三规则检查（尾部阻塞、目标排占用、Boss 墙壁）确保击退前目标位置无敌人。
2. `pushedToRow` 参数在 `CompactByClearRows` 中保护被推敌人不被立即紧凑回来。
3. `InsertEnemySorted` 有重叠检测（Warning 级别）。

重构后需确保：击退紧凑完成后启动的是**跨列波次行军**而非各列独立推进，避免不同列以不同速度前进导致重叠。

## 实现约束
- 旧 `Column` 层 per-column 链式自推进（`OnColumnRushMoveComplete` 末尾的自动重启逻辑，Column.cs:166-188）必须删除。
- `Column.StartRushMoveChain` 保留为底层机制，但链结束回调改为通知 `ColumnManager`，由 `ColumnManager` 统一决策下一步。
- `ColumnManager` 新增波次行军协调器：`StartWaveMarch`、`AbortWaveMarch`、`OnWaveMarchRushComplete`。
- `Column.RemoveEnemy` 不再做 compact（移除列表压缩逻辑），仅移除敌人。
- `Column.TriggerFillForward` 废弃，由 `ColumnManager.StartWaveMarch` 替代。
- `WaveSpawner` 波次生成后调用 `ColumnManager.StartWaveMarch()` 而非逐列 `TriggerFillForward`。

---

## 本次对话发现的坑点 (2025-01)

### 坑点1: TriggerFillForward 自循环导致刷波阵型全毁
- 症状：刷波时敌人从 row=2+ 被循环推进到 row=0，阵型完全破坏
- 根因：`TriggerFillForward` 的链回调 `OnFillForwardChainComplete` 自递归调用自身，刷波调用点（只需前进1排）和死亡压实后调用点（需要循环填补）语义不同
- 教训：**不要用一个自循环方法同时服务「单步前移」和「循环填补」两种语义**

### 坑点2: 移除前移调用而不提供替代 → 敌人永远不前进
- 症状：敌人出生在 row=2+ 后永远不向前移动，永远打不到
- 根因：刷波后敌人需要某种机制向前推进到攻击范围。`CompactColumn` 只在死亡时压实空隙，不处理初始推进
- 教训：**移除前移调用时必须同时提供替代的推进机制**

### 坑点3: StartWaveMarch 已实现但无调用点
- `ColumnManager.StartWaveMarch()` 及 `BeginWaveStep`、`OnWaveEnemyRushComplete` 级联逻辑已完整实现
- 但当前代码库中**无任何地方调用 `StartWaveMarch()`**
- 设计文档规定刷波后和击退紧凑后应调用它

### 坑点4: Boss 被 TriggerFillForward 推到前排过早进入 InCombat
- 旧版 `TriggerFillForward` 设置 `targetRow = 0` 强制 Boss 冲到最前排
- Boss 到达 row ≤ 1 后触发应战缓冲计时器 → InCombat → 可被伤害
- 根因：Boss 不应参与普通敌人的补齐/行军链

### 坑点5: 设计文档与用户指令的矛盾
- 本文档规定：波次行军是跨列整排的（StartWaveMarch），TriggerFillForward 已废弃
- 用户近期指令：每列独立，不跨列
- 这两个要求在当前实现中互斥——需要用户明确选择

### 坑点6: 击退后紧凑延迟位置错误 —— 距离=1 时无可见延迟（2025-01）
- 症状：击退距离=1，敌人被击退后立刻 Rush 回原位，无停顿
- 根因（两层）：
  1. `compactionWaveMarchDelay` 放在 `OnCompactionChainComplete` → `StartWaveMarch` 之间，但 Rush 补齐动画发生在紧凑链阶段（`StartAllCompactionChains`）。修复：将 `Invoke` 移到 `RowBasedFillUp` 之后
  2. `RecheckAttackRange`（由 `RecheckPushedEnemiesAttackRange` 调用）内部调用 `StartMoving(isRush: true)`，完全绕过了延迟。修复：移除 `RecheckAttackRange` else 分支的 `StartMoving`，仅设置 `targetRow` + `pendingRushMove`
- 教训：**补齐流程中任何方法若内部会 `StartMoving`，必须确认它不会绕过 ColumnManager 的延迟/链调度**

### 坑点7: Boss 补齐入口 TriggerAllBossFillForward 零调用者（2025-01）
- 症状：Boss 在远处不向前补齐
- 根因：重构时将 Boss 补齐触发从 `TriggerFillForward` 提取为独立方法 `TriggerBossFillForward`，但废弃旧入口后未为 `TriggerAllBossFillForward` 添加新调用点
- 教训：**提取方法到新入口时，grep 确认新入口有调用者，旧入口的每个调用点都已迁移**

### 坑点8: 多排秒杀后 _pendingWaveEnemies 死锁（2025-01）
- 症状：同时击杀多排敌人后，后排原地不动永不补齐
- 根因：阵亡敌人仍在 `_pendingWaveEnemies` 中，`OnWaveEnemyRushComplete` 永不触发 → `_pendingWaveEnemies.Count` 永远 > 0 → `_isWaveMarching` 永久 true → `StartWaveMarch` 死锁
- 修复：`RemoveEnemyFromColumn` 中清理死亡敌人出 `_pendingWaveEnemies`，并重置标记
- 教训：**任何持有敌人引用的集合，在敌人死亡时都必须同步清理，否则状态机永久卡死**

### 坑点9: PerRow 分支缺少 StartWaveMarch 调用和 _pendingWaveEnemies 清理（2025-01）
- 症状：PerRow 模式下单一排敌人被一次性击杀后，后排敌人不补齐
- 根因：PerRow 分支只调用 `RowBasedFillUp()`（数据压缩），未调用 `StartWaveMarch()`（启动移动），且缺少 `_pendingWaveEnemies` 清理（与坑点8同模式）
- 修复：将 `_pendingWaveEnemies` 清理和 `RemoveEnemy` 提取到两个分支共用，PerRow 分支在 `RowBasedFillUp()` 后调用 `StartWaveMarch()`
- 教训：**修改 PerColumn 分支时，必须同步检查 PerRow 分支是否需要相同修复**

### 坑点10：波次补齐中断攻击动作（2025-01）
- 症状：105/106 等远程敌人在蓄力或射箭时，前方整排空出会被立即打断并补位。
- 根因：`ColumnManager.BeginWaveStep()` 无条件调用 `ResetMovementState()`，其会 Kill `_attackTween`。
- 修复：对 `isAttackAnimating` 敌人仅标记 `targetRow` 和 `pendingRushMove`；攻击的正常完成回调再调用 `TryStartRushMove()`。攻击冷却仍允许取消并补齐；眩晕、击飞、死亡等打断沿既有状态路径处理。
- 教训：波次补齐只能延后攻击动作结束后的移动，不能取消已经开始的攻击表现或命中。
