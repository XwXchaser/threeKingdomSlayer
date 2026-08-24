---
id: kd_04b72df3-abb3-4ca2-941d-83941c56fa62
injectMode: inherit
summary: 第三期局内成长系统（经验三选一）开发状态 + 位移效果三选一BUG修复（击退波/聚拢波/回旋波/连锁弹射）+ 被动特效prefab部署 + 补齐打断攻击BUG修复 + C技霸体位移BUG修复 + BOSS墙壁保护 + RowBasedFillUp打断BOSS Rush修复 + Boss初始wave spawn不补齐死锁修复 (2025-12-19)
aiEditMode: auto
maintenanceRules: |-
  Keep only durable and reusable project memory
  Consolidate duplicates or conflicts into the latest conclusion
  Remove temporary context, one-off tasks, and unsupported guesses
---

# 第三期局内成长系统（经验三选一）开发状态

## 最新更新 (2025-12)

### 攻速效果 BuffIcon 百分比显示 + 图标部署

**状态**: ✅ 已完成

**变更**:
- `BuffIcon.cs` 新增 `_percentText` 字段 + `SetPercentText()` 方法，用于右上角显示累计加成百分比
- `BuffIcon.prefab` 新增 `PercentText` 子对象：右上角锚定、金色 10pt 粗体、默认隐藏
- `UpgradeEffectManager.cs` 新增 `GetAttackSpeedBonusPercent()` → `(speedMult-1)*100`
- `BuffDisplayPanel.cs` `OnUpgradeApplied` 中 `attack_speed` 分支调用 `icon.SetPercentText($"+{pct:F0}%")`
- `UpgradePoolConfig` 加入 AttackSpeed（commonPool，权重 10）
- 部署四个图标：地刺→icon_31_spikeTrap、旋风→icon_31_cyclone、箭矢齐射→icon_31_tripleArrow、疾风→icon_31_longer

**攻速效果数据流**:
- 动作模式：`_actionLockTimer = cooldown / speedMult`，视效 timeScale 同步
- 旧独立CD模式：攻速不生效（保留为未来"移除动作硬直"奖励）
- 每级 `numericLevels.floatValue` 独立可配，默认 10 级 × 0.15
- 累计显示：Lv.1→+15%、Lv.5→+75%

---

## 历史记录

### Bug: PushWave RecheckAttackRange 被 CompactByClearRows 覆写 → 击退后敌人不回到攻击范围

**状态**: ✅ 已修复 (2025-12)

**现象**: Stab PushWave 击退敌人后，第一个被击退的敌人不会 rush 回到攻击范围（row 0），而是留在被击退的位置。

**根因**: `ExecutePush` 中调用 `RecheckAttackRange` 设置了正确的 targetRow，但随后的 `PostDisplacementFillUp` → `CompactByClearRows` 重新分配 targetRow（按列紧凑），覆写了 RecheckAttackRange 的结果。

**修复**:
1. `ExecutePush` 中移除 `RecheckAttackRange` 调用
2. `ApplyPushWave` 新增 `pushedEnemiesOut` 参数，收集被推动的敌人列表
3. `ApplyStabPushWave` 中调整执行顺序：`ApplyPushWave` → `PostDisplacementFillUp` → `RecheckPushedEnemiesAttackRange`
4. `CompactByClearRows` 中 Boss 跳过（`if (e.isBoss) continue`），防止 Boss 被紧凑改变 targetRow

**文件**: `ColumnManager.cs`, `AttackSystem.cs`, `Column.cs`

### Bug: Rush 重叠检测导致无限重试循环

**状态**: ✅ 已修复 (2025-08-09)

**现象**: 聚拢波将敌人移到其他列后，`RecheckAttackRange` 命令 rush 到 row 0，但 row 0 已被该列原有敌人占据 → Rush 重叠检测触发回退 → 0.1s 后重试 → 无限循环。日志 `[RowTrace] #19(103) row 1→0` 反复出现。

**根因**: Rush 重叠检测中设置了 `pendingRushMove=true` + 0.1s 延迟重试，但目标行持续被占 → 死循环。

**修复**: Rush 重叠时不再设置重试，直接放弃。该敌人留在当前排位等待前方敌人死亡后由死亡链自然补齐。

### 位移效果架构重构 — 按攻击类型拆分 (2025-08-11)

**状态**: ✅ 已完成

**变更**:
- 删除 `ApplyDisplacementEffects(AttackType switch 路由)`
- `ExecuteStab` → `ApplyStabPushWave` → `ColumnManager.ApplyPushWave`
- `ExecuteSlash` → `ApplySlashDirectionalPush` → `ColumnManager.ApplyDirectionalPush`（新方法，按行分组朝 slash 方向推，不重叠）
- `ExecutePierce/Sweep/Launch` → 不再触发位移
- `ApplyConvergenceWave` 重写为按行槽位分配（始终朝 col=2，不越界），删除 `ResolveConvergenceConflicts`
- `UpgradeEffectManager` 新增 `_directionalPushStep`
- `DisplacementDebugTool` 新增 DirectionalPush 字段
- 位移执行顺序：每个攻击类型独立调用位移方法 → 各自 `PostDisplacementFillUp`

### Bug: C技霸体位移

**状态**: ✅ 已修复 (2025-08-10)

**现象**: 处于C技攻击步骤中的敌人被普攻(Stab/Slash/Pierce/Sweep)的击退/聚拢推动。

**根因**: `ApplyPushWave` / `ApplyConvergenceWave` 未检查 `e.isCFrame`。

**修复**: 位移方法新增 `canInterruptCFrame` 参数。普攻传 `false` → 跳过C技敌人；Launch(挑飞)传 `true` → 可破霸体。

### Bug: 普通敌人被击退到BOSS身后 → 卡关

**状态**: ✅ 已修复 (2025-08-10)

**现象**: 击退将普通敌人推到BOSS所在排之后，BOSS因BossPause无法前进且敌人被BOSS挡住。

**根因**: `CanPushColumn` / `ExecutePush` 跳过BOSS检查，未将BOSS排视为墙壁。

**修复**: 新增 `GetBossRowInColumn`；`CanPushColumn` 规则3阻止越界击退；`ExecutePush` 钳制上限 `bossRow - 1`。

### Bug: RowBasedFillUp 打断 BOSS Rush 移动 → 卡关

**状态**: ✅ 已修复 (2025-08-10)

**现象**: BOSS到达row=2触发BossPause后，前排敌人死亡 → BossResume → StartMoving，同帧位移PostDisplacementFillUp再次RowBasedFillUp → CompactByClearRows杀Moving状态。BOSS卡死在Idle无法前进。

**根因**: `CompactByClearRows` 保护名单遗漏 `Moving` 状态。

**修复**: 保护条件新增 `e.state == EnemyState.Moving`。
