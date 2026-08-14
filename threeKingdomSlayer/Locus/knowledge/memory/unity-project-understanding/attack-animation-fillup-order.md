---
id: kd_b50736d7-6834-430f-8e21-e5e780c02a81
injectMode: inherit
summary: 补齐、攻击和位移的调度边界；同时记录当前代码与权威规则的已知偏差。
aiMaintained: inherit
---

## 权威调度边界

- 普通补齐只由整排清空触发；单列空槽和击退临时空槽不能创建普通补齐订单。
- 击退前必须记录被击退者的精确原槽；停顿后只由该敌人返回原槽。未受击敌人不移动。
- 横向位移只改变命中者的column，不执行向前rejoin。
- 攻击范围只决定攻击/等待，不创建移动。
- 受到命中的无霸体敌人在可打断攻击阶段会被打断；已经生成的飞射物独立继续。
- 未被命中的攻击中敌人若已有合法普通补齐订单，应等待攻击结束后重新校验订单，而不是被强制打断。
- `ColumnManager` 是订单唯一创建者；订单使用owner、generation和精确目标槽。

## 当前实现入口

- `ColumnManager.StartWaveMarch` 扫描全局空排并创建跨列 WaveMarch 步骤；`BeginWaveStep` 不再按 `attackRange` 排除源排成员。
- 合法步骤中的攻击动画敌人先保留订单并等待；`Enemy.PlayAttackAnimationTween` 完成回调通过 `TryStartRushMove` 恢复已有订单。
- `Enemy.UpdateAttack` 在 HitStop 或 HitFlash 有效期间暂停攻击冷却，避免受击动画与 Attack Trigger 竞争；反馈结束后继续原冷却。
- `ColumnManager.ExecutePush` 在修改 row 前注册或续期 `PushReturnTransaction`；连续击退保留第一次 `(column,row)` 原点。
- `ColumnManager.PostDisplacementFillUp(IEnumerable<Enemy>)` 只为实际被后推的敌人开启 0.35 秒后回位；不扫描或移动其他敌人。
- `RushMoveOrderOwner.PushReturn` 每次只在下一排空闲时前进一步，最终只在精确原槽完成；阻塞时由 `ColumnManager.Update` 和拓扑变化继续重试。击退前已有的 WaveMarch 步骤会保存成员和目标槽，回位结束后重新校验该原订单；仅真实拓扑变化才允许重新扫描。
- `Enemy.Die`、`Enemy.OnDisable`、`Enemy.ResetEnemy` 与列移除/清空路径会取消击退事务，generation 使旧完成回调失效。
- `AttackSystem.ApplySlashDirectionalPush` 不再调用 `PostDisplacementFillUp`；`WaveManager` 仍聚合后推目标后统一开启回位。
- 旧 `PrepareDisplacementCompaction`、`RowBasedFillUp`、`CompactAllColumns` 兼容入口为 inert，运行时后推路径不再使用 compaction/rejoin。
- `TryResumePausedWaveAfterPushReturns` 不再检查 `IsRowFullyVacated(targetRow)`，无条件恢复暂存波次。`BeginWaveStep` 内部每敌人校验（rowIndex 过滤、pushReturnTransactions 过滤、AssignRushMoveOrder 拒绝）已足够避免重复发放订单。

## 已验证缺陷

### 缺陷1：105 攻击范围过滤 (已修复)
- 105远程敌人曾因 `BeginWaveStep` 的 `rowIndex < attackRange` 过滤而拿不到 WaveMarch 订单，表现为前排清空后仍持续远程攻击。
- 修复原则是删除订单创建阶段的攻击范围过滤，而不是让 `Enemy` 在攻击结束时自行创建移动；这样保持 `ColumnManager` 的唯一调度所有权。

### 缺陷2：行军中被击退导致单兵永久卡死 (已修复)
- 行军阶段被 `AbortWaveMarch` 中断，`TryResumePausedWaveAfterPushReturns` 因 `IsRowFullyVacated(targetRow)=false` 丢弃暂存波次→丢失 WaveMarch 订单→Idle 状态永久不恢复。
- 修复：移除 `IsRowFullyVacated` 前置条件，无条件恢复暂存波次。

### 缺陷3：受击后攻击伤害存在但动画缺失 (已修复)
- HitStop 禁用 Animator 时仍设置 Hit Trigger，且攻击冷却继续推进；恢复 Animator 的同一帧触发 Attack 后，HitFlash 抢占视觉，但 DOTween 仍在之后结算伤害。
- 修复：HitStop/HitFlash 期间暂停 `UpdateAttack` 的冷却推进，反馈结束后再启动攻击；用户复测暂未复发。
