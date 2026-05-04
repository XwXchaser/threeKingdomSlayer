# Bug：敌人补齐到最前排后，后一排的敌人依旧在向前补齐，导致不断重叠

## 问题描述

经过多轮击杀后（例如连续击杀前排 3 个敌人），列中剩余的所有敌人都汇聚到了 `rowIndex=0`（最前排），发生严重重叠。原本应当各占据不同排位置（pos=0→row=0，pos=1→row=1，pos=2→row=2），但实际上所有敌人都停在了 row=0。

## 根因分析

**核心问题在于 [`UpdateMovement()`](threeKingdomSlayer/Assets/Scripts/Enemy/Enemy.cs:350-384) 中决定"是否继续补齐"的退出条件仅依赖 `attackRange`，未考虑敌人的列表位置。**

### 详细触发路径

1. **`Column.RemoveEnemy()`**（[`Column.cs:84-94`](threeKingdomSlayer/Assets/Scripts/Core/Column.cs:84)）调用 `SetRowIndex(i+1)`，将剩余敌人的 `rowIndex` 设为 `listPosition + 1`：
   - 位置 0 的敌人 → `SetRowIndex(1)`（正确位置应是 row=0）
   - 位置 1 的敌人 → `SetRowIndex(2)`（正确位置应是 row=1）
   - 位置 2 的敌人 → `SetRowIndex(3)`（正确位置应是 row=2）

2. **链式触发**：第一个敌人开始补齐移动，`moveProgress >= 0.5` 时触发链式事件（[`Enemy.cs:324-329`](threeKingdomSlayer/Assets/Scripts/Enemy/Enemy.cs:324)），下一敌人开始移动。

3. **移动完成后的判定**（[`Enemy.cs:350-384`](threeKingdomSlayer/Assets/Scripts/Enemy/Enemy.cs:350)）：
   ```csharp
   rowIndex--;  // 位置0的敌人：rowIndex从1→0
   int attackRange = (int)Mathf.Max(1, config.attackRange);  // 通常为1
   bool reachedAttackRange = rowIndex < attackRange;  // 0 < 1 = true
   ```
   - rowIndex=0 `<` attackRange=1 → `reachedAttackRange=true` → **停止补齐，开始攻击** ✅ 位置0正确

4. **对于后续敌人**（经过 **`rushMoveDelay`** 后的第二轮补齐）：
   - 位置1的敌人当前 `rowIndex=1`（第一轮补齐后 `rowIndex--` 从 2→1）
   - `1 >= attackRange(1)` → `reachedAttackRange=false`
   - 启动 **`rushMoveDelay`** 计时器（[`Enemy.cs:372-374`](threeKingdomSlayer/Assets/Scripts/Enemy/Enemy.cs:372)）
   - 延迟结束后调用 `TryStartRushMove()` → 再前进一排（`rowIndex--` 从 1→0）
   - 此时 `0 < 1` → 停止 → **位置1的敌人也停在了 row=0** ❌ 与位置0的敌人重叠

5. **位置2及其他敌人同理**，经过多轮 `rushMoveDelay` + 补齐，最终全部汇聚到 `rowIndex=0`，形成完全重叠。

## `FillToFront` 行为的定量分析

敌人的补齐逻辑本质上是一个**逐排前进循环**，每次前进一排后判断是否继续：

```
while (rowIndex >= attackRange):
    补齐移动一排（rowIndex--）
    等待 rushMoveDelay(≈0.2s)
```

这个循环的退出条件只有 `rowIndex < attackRange`，**没有任何检查敌人的 `rowIndex` 是否已经等于其正确的列表位置（`listPosition`）**。因此：

| 敌人 | 列表位置 | SetRowIndex(i+1)后的 rowIndex | 第1轮补齐后 | 第N轮补齐后 | 停止位置 |
|------|---------|------------------------------|------------|------------|---------|
| E0   | 0       | 1                            | 0          | -          | row=0 ✅ |
| E1   | 1       | 2                            | 1          | 0          | row=0 ❌ (应为 row=1) |
| E2   | 2       | 3                            | 2          | 0          | row=0 ❌ (应为 row=2) |

## 修复方向

需要在 [`UpdateMovement()`](threeKingdomSlayer/Assets/Scripts/Enemy/Enemy.cs:350-384) 的补齐完成分支中，增加**第二个停止条件**：当 `rowIndex <= listPosition`（即敌人已到达其正确位置）时，即使 `rowIndex >= attackRange` 也不应继续补齐。

### 具体方案选项

**方案 A（推荐）：在 `Enemy` 类中新增 `int targetRow` 字段，记录当前补齐的目标排**

- `Column.RemoveEnemy()` 调用 `SetRowIndex(i+1)` 后，设置 `enemy.targetRow = i`（列表位置）
- `ColumnManager.UpdateEnemyRow()` 同理，设置 `e.targetRow = i`
- `UpdateMovement()` 在补齐完成分支中，判断 `rowIndex <= targetRow` 时停止补齐

**方案 B（简化版）：复用现有字段推断目标排**

- `SetRowIndex(row)` 时，如果 `pendingRushMove == true`，推断目标排为 `row - 1`
- 在 `UpdateMovement()` 的补齐完成逻辑中，检查 `rowIndex <= row - 1`

### 修改位置

- [`Enemy.cs`](threeKingdomSlayer/Assets/Scripts/Enemy/Enemy.cs) — 新增 `targetRow` 字段（或推断逻辑），修改 `UpdateMovement()` 中的补齐停止条件
- [`Column.cs`](threeKingdomSlayer/Assets/Scripts/Core/Column.cs:84-94) — `SetRowIndex(i+1)` 后设置 `enemy.targetRow = i`
- [`ColumnManager.cs`](threeKingdomSlayer/Assets/Scripts/Core/ColumnManager.cs:138-144) — `SetRowIndex(i+1)` 后设置 `e.targetRow = i`
