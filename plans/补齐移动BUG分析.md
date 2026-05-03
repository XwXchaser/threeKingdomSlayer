# 补齐移动 BUG 分析

## 背景

项目使用 5 列行列战斗系统，敌人按列排列，每列中 index 0 = 最前排（靠近玩家）。当最前排敌人被击杀后，后方所有敌人需要向前补齐一排。

## 核心文件

- [`Enemy.cs`](threeKingdomSlayer/Assets/Scripts/Enemy/Enemy.cs) — 敌人实体，管理状态机、移动、攻击
- [`Column.cs`](threeKingdomSlayer/Assets/Scripts/Core/Column.cs) — 单列敌人管理
- [`ColumnManager.cs`](threeKingdomSlayer/Assets/Scripts/Core/ColumnManager.cs) — 列管理器
- [`StageController.cs`](threeKingdomSlayer/Assets/Scripts/Managers/StageController.cs) — 关卡控制器（单例）
- [`RowFormation.cs`](threeKingdomSlayer/Assets/Scripts/Core/RowFormation.cs) — 排阵型计算器
- [`StageConfig.cs`](threeKingdomSlayer/Assets/Scripts/Core/StageConfig.cs) — 关卡配置

## 关键技术概念

- **Unity 2022.3.62t7（团结引擎 1.8.5）**：3D 项目，内置渲染管线
- **5 列行列战斗系统**：Column/ColumnManager 管理敌人位置，index 0 = 最前排
- **敌人状态机**：Idle/Moving/Attacking/Stunned/Launched/Dead
- **localPosition 定位**：敌人使用局部坐标
- **-Z 轴是前进方向**（朝向玩家），+Z 轴是后退方向（远离玩家）
- **rowSpacing = 2.5**：排间距
- **formationOffsetZ = 0**：阵型整体 Z 轴偏移（默认值）
- **isMovingToNextRow**：控制 `UpdateMovement()` 是否执行的关键标志
- **moveSpeed = 1f**：秒/排
- **maxVisibleRows = 5**：最大可见排数

## BUG 1：无限补齐（上一版本）

### 现象
击杀敌人后，后方敌人持续打印 Moving 日志，progress 值递增但永远无法到达 1，无限循环。

### 根因分析
Z 轴方向搞反了。在 Unity 中，**-Z 轴是前进方向**（朝向玩家），**+Z 轴是后退方向**（远离玩家）。

旧公式 `zPos = -(rowIndex * rowSpacing)`：
- `rowIndex=0` → `z=0`（最前排）
- `rowIndex=4` → `z=-10`（最后排）

但正确的应该是：
- `rowIndex=0`（最前排）在 `z=-10`（最靠近玩家）
- `rowIndex=4`（最后排）在 `z=0`（最远离玩家）

因为 Z 轴方向反了，`Lerp` 从旧排向新排移动时方向错误，敌人永远无法到达目标位置。`moveProgress` 到达 1 后位置不对，然后 `UpdateEnemyRow()` 又被调用，重置 `moveProgress=0`，无限循环。

### 修复
将 Z 轴公式改为 `(maxVisibleRows - 1 - rowIndex) * (-rowSpacing) + offsetZ`，并新增 `GetRowZ()` 辅助方法统一计算。

### 当前状态
✅ 已修复

---

## BUG 2：梯形向内聚拢（当前版本引入）

### 现象
击杀敌人后，后方敌人向中间聚拢（X 轴偏移变化），看起来像梯形向内收缩。

### 根因分析
在 `UpdateWorldPosition()` 中：
```csharp
xPos = StageController.Instance.GetFormationOffset(columnIndex, rowIndex);
```

`GetFormationOffset` 使用 `rowIndex` 计算 X 轴偏移。在补齐移动过程中：

**当前版本（有 `isRushMoving`/`StartRushMoving()`）：**
1. `Column.RemoveEnemy()` 先调用 `StartRushMoving()`（保存旧 `rowIndex` 到 `rushStartRow`）
2. 然后调用 `SetRowIndex(i)` 更新 `rowIndex` 为新值
3. `SetRowIndex()` 内部调用 `UpdateWorldPosition()`
4. `UpdateWorldPosition()` 中 `isMovingToNextRow=true`，走移动分支
5. 移动分支中 Z 轴使用 `rushStartRow`（旧排位置）计算
6. **但 X 轴使用 `rowIndex`（新排位置）计算！**

所以 X 轴和 Z 轴不同步：Z 轴在旧排位置，X 轴在新排位置。这就是"梯形向内聚拢"的原因。

**上一版本（无 `isRushMoving`/`StartRushMoving()`）：**
1. `Column.RemoveEnemy()` 先调用 `ResetMovementState()`（重置 `state=Idle`）
2. 然后调用 `SetRowIndex(i)` 更新 `rowIndex`
3. `SetRowIndex()` 内部调用 `UpdateWorldPosition()`
4. `UpdateWorldPosition()` 中 `isMovingToNextRow=false`，走非移动分支
5. 非移动分支中 X 轴和 Z 轴都使用 `rowIndex`（新排位置）
6. 然后调用 `StartMoving()` 设置 `isMovingToNextRow=true`
7. 下一帧 `UpdateMovement()` 中 `UpdateWorldPosition()` 被调用
8. 此时 `isMovingToNextRow=true`，走移动分支
9. 移动分支中 X 轴和 Z 轴都使用 `rowIndex`（新排位置，尚未更新）

**所以上一版本中 X 轴和 Z 轴始终同步！** 这就是为什么上一版本没有"梯形向内聚拢"的问题。

### 当前状态
❌ 未修复 — 当前代码已恢复到上一版本逻辑（无 `isRushMoving`/`StartRushMoving()`），理论上应该没有此问题。但用户反馈"完全没有修复问题"，可能需要进一步排查。

---

## BUG 3：无限尝试向前补齐（当前版本引入）

### 现象
击杀敌人后，后方敌人无限尝试向前补齐，日志显示反复触发补齐移动。

### 根因分析
**当前版本（有 `isRushMoving`/`StartRushMoving()`）：**
1. 补齐移动完成 → `state=Idle`
2. `rowIndex < attackRange` 检查 → 如果 `rowIndex >= attackRange`，调用 `StartMoving()`
3. `StartMoving()` 中 `state==Moving` 保护检查通过（因为 `state=Idle`）
4. `moveProgress=0`，`isMovingToNextRow=true`
5. `UpdateMovement()` 执行，`moveProgress` 到达 1
6. `rowIndex--`，`OnEnemyMovedForward()` → `UpdateEnemyRow()`
7. `UpdateEnemyRow()` 中再次调用 `StartRushMoving()` + `SetRowIndex()`
8. 再次补齐移动
9. 补齐移动完成 → `state=Idle`
10. 回到步骤 2，无限循环！

**上一版本（无 `isRushMoving`/`StartRushMoving()`）：**
1. `Column.RemoveEnemy()` 中：`ResetMovementState()` → `SetRowIndex()` → `StartMoving()`
2. `StartMoving()` 中 `state==Moving` 保护检查跳过（因为 `ResetMovementState()` 设置了 `state=Idle`）
3. `moveProgress=0`，`isMovingToNextRow=true`
4. `UpdateMovement()` 执行，`moveProgress` 到达 1
5. `rowIndex--`，`OnEnemyMovedForward()` → `UpdateEnemyRow()`
6. `UpdateEnemyRow()` 中：`ResetMovementState()` → `SetRowIndex()` → `StartMoving()`
7. 后方敌人开始补齐移动
8. 补齐移动完成后 `rowIndex--`，再次触发 `UpdateEnemyRow()`
9. 无限循环！

**但用户说上一版本没有这个问题！** 所以要么：
- 上一版本的 `ResetMovementState()` 没有重置 `state=Idle`（所以 `StartMoving()` 中 `state==Moving` 保护检查跳过）
- 或者 `UpdateEnemyRow()` 在上一个版本中没有被调用
- 或者 `StartMoving()` 的保护检查在上一个版本中不同

**关键问题：** `UpdateMovement()` 中移动完成后调用了 `StartMoving()`，导致当前敌人继续向前移动，而 `UpdateEnemyRow()` 又触发后方敌人补齐移动，造成无限循环。

### 当前修复尝试
在 `UpdateMovement()` 中移动完成后**不再调用 `StartMoving()`**，避免无限循环。

### 当前状态
❌ 未确认修复 — 用户反馈"完全没有修复问题"

---

## BUG 4：敌人后退（上一版本）

### 现象
击杀敌人后，后方敌人往后退（表现层）且永远无法补齐到第一排。

### 根因
Z 轴方向搞反了（同 BUG 1）。旧公式 `zPos = -(rowIndex * rowSpacing)` 让 `rowIndex=0` 在 `z=0`，但 -Z 是前进方向，`rowIndex=0` 应该在 `z=-10`。

### 修复
将 Z 轴公式改为 `(maxVisibleRows - 1 - rowIndex) * (-rowSpacing) + offsetZ`。

### 当前状态
✅ 已修复

---

## 当前代码状态

### [`Enemy.cs`](threeKingdomSlayer/Assets/Scripts/Enemy/Enemy.cs)
- 已移除 `isRushMoving`、`rushStartRow`、`StartRushMoving()` 相关代码
- `ResetMovementState()` 恢复为重置 `state=Idle`、`isMovingToNextRow=false`、`moveProgress=0`
- `UpdateMovement()` 中移动完成后不再调用 `StartMoving()`
- 保留 `GetRowZ()` 方法（Z 轴修复）
- `UpdateWorldPosition()` 中移动时 X 轴使用 `rowIndex`（旧排位置）

### [`Column.cs`](threeKingdomSlayer/Assets/Scripts/Core/Column.cs)
- `RemoveEnemy()` 使用 `ResetMovementState()` → `SetRowIndex()` → `StartMoving()` 顺序

### [`ColumnManager.cs`](threeKingdomSlayer/Assets/Scripts/Core/ColumnManager.cs)
- `UpdateEnemyRow()` 使用 `ResetMovementState()` → `SetRowIndex()` → `StartMoving()` 顺序

## 待排查问题

1. **"完全没有修复问题"** — 用户反馈当前代码仍然有问题。需要进一步排查：
   - 是否 `ResetMovementState()` 重置 `state=Idle` 后，`StartMoving()` 中 `state==Moving` 保护检查通过，导致 `moveProgress` 被重置为 0？
   - 是否 `UpdateMovement()` 中移动完成后不调用 `StartMoving()` 导致敌人卡在 `Idle` 状态？
   - 是否 `OnEnemyMovedForward()` → `UpdateEnemyRow()` 的调用链有问题？

2. **建议的调试方法**：
   - 在 `UpdateMovement()` 中添加日志，记录 `moveProgress`、`rowIndex`、`state`、`isMovingToNextRow`
   - 在 `Column.RemoveEnemy()` 和 `ColumnManager.UpdateEnemyRow()` 中添加日志，记录每个敌人的 `rowIndex` 变化
   - 在 `StartMoving()` 中添加日志，记录是否被保护检查跳过
   - 在 `ResetMovementState()` 中添加日志，记录重置前的状态
   - 检查 `OnEnemyMovedForward()` 是否被正确调用
   - 检查 `EnemyManager.Instance` 是否为 null

3. **可能的根因**：
   - `ResetMovementState()` 重置 `state=Idle` 后，`StartMoving()` 中 `state==Moving` 保护检查通过，`moveProgress` 被重置为 0。但 `SetRowIndex()` 已经更新了 `rowIndex`，所以 `Lerp` 从新位置向更前一排移动。这不是无限循环，而是正常的补齐移动。
   - **但问题在于：** `UpdateMovement()` 中移动完成后不调用 `StartMoving()`，所以当前敌人移动完成后停在 `Idle` 状态。但 `OnEnemyMovedForward()` → `UpdateEnemyRow()` 触发了后方敌人的补齐移动。后方敌人补齐移动完成后，`rowIndex--`，再次触发 `OnEnemyMovedForward()` → `UpdateEnemyRow()`，再次补齐移动。**这才是真正的无限循环！**
   - **修复方案：** 在 `UpdateMovement()` 中移动完成后，如果 `rowIndex >= attackRange`，调用 `StartMoving()` 继续移动。但这样又会导致 BUG 3 的无限循环。
   - **根本问题：** `UpdateMovement()` 中移动完成后调用 `StartMoving()` 会导致当前敌人继续移动，而 `UpdateEnemyRow()` 又触发后方敌人补齐移动，造成无限循环。不调用 `StartMoving()` 又会导致敌人卡在 `Idle` 状态。
   - **正确的修复：** 在 `UpdateMovement()` 中移动完成后，如果 `rowIndex >= attackRange`，调用 `StartMoving()` 继续移动。**但 `UpdateEnemyRow()` 中只处理后方敌人，不处理当前敌人。** 当前敌人已经在 `UpdateMovement()` 中自己更新了 `rowIndex`，不需要 `UpdateEnemyRow()` 再处理。
   - **但 `UpdateEnemyRow()` 中 `ResetMovementState()` 重置了后方敌人的 `moveProgress=0`，导致后方敌人回到起点。** 如果后方敌人正在移动中（`moveProgress` 已经到 0.5），重置为 0 会导致它们回到起点。
   - **所以正确的修复是：** `UpdateEnemyRow()` 中不调用 `ResetMovementState()`，只调用 `SetRowIndex()` 和 `StartMoving()`。但 `StartMoving()` 中 `state==Moving` 保护检查会跳过。
   - **所以需要：** 在 `UpdateEnemyRow()` 中，先设置 `state=Idle`，再调用 `StartMoving()`。但这样会破坏 `StartMoving()` 的保护检查。
   - **最终方案：** 移除 `StartMoving()` 中的 `state==Moving` 保护检查，改为检查 `isMovingToNextRow`。如果 `isMovingToNextRow==true`，说明正在移动中，不重置 `moveProgress`。如果 `isMovingToNextRow==false`，说明移动已完成或未开始，可以重置 `moveProgress`。
