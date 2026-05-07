# Core 模块

## 模块名称
Core（核心数据结构与配置）

## 主要职责
游戏阵型系统、敌人/武将数值、关卡布局的数据结构与配置。定义 5 列网格模型、基于排的梯形阵型定位，以及策划工作流用的 ScriptableObject 配置资产。

## 核心类

| 类 | 说明 |
|---|---|
| `Column` (Serializable) | 管理单列敌人列表。Index 0 = 最前排。处理 `RemoveEnemy()`（链式触发补齐前移）、`TriggerFillForward()`（生成后初始压缩）和链式回调 `OnColumnRushMoveComplete`。 |
| `ColumnManager` (MonoBehaviour) | 5 个 Column 实例的单例式管理器。提供按列增删改查、范围查询（`GetEnemiesInRange`、`GetAllEnemiesInRange`、`GetEnemiesByRowLimit`）和非死亡前移的 `UpdateEnemyRow()` 链式逻辑。 |
| `EnemyConfig` (ScriptableObject) | 敌人数值：血量、occupySlots(1-5)、攻速/攻击/射程、移速、最大韧性、眩晕/击飞时长、金币奖励、6 种伤害类型弱点倍率。菜单：`Assets > Create > 一夫当关/敌人配置` |
| `HeroConfig` (ScriptableObject) | 武将数值：血量、复活次数、复活血量百分比、6 种攻击的伤害/范围/冷却、格挡属性、伤害加成百分比。菜单：`Assets > Create > 一夫当关/武将配置` |
| `RowFormationPreset` (ScriptableObject) | 每排 X 偏移预设表。含 `List<RowData>`，每个 `RowData` 存 `float[5] columnOffsets`。菜单：`Assets > Create > 一夫当关/排阵型预设` |
| `RowFormation` (static class) | 静态 X 轴列偏移计算器。三套优先级方案：**B**（手动 `float[] manualRowHalfWidths`，最高优先级）、**A**（RowFormationPreset ScriptableObject）、**C**（公式：`Lerp(maxSpread, minSpread, t^powerCurve)`，`t = rowIndex/maxRow`）。提供 `DrawFormationGizmos()` |
| `StageConfig` (ScriptableObject) | 关卡配置：stageId/name、`List<WaveConfig> waves`、连杀阈值、通关金币、排透明度因子、最大可见排数、补齐延迟及全部阵型参数 |
| `WaveConfig` (Serializable) | 单波：waveId、isBossWave、`List<RowConfig> rows` |
| `RowConfig` (Serializable) | 单排：`int[5] enemyIds`（每列槽位一个） |

## 公开接口

**Column**：
- `GetFrontEnemy()`, `GetEnemyAtRow(rowIndex)`, `AddEnemy(Enemy)`, `RemoveEnemy(Enemy)`, `TriggerFillForward()`
- 属性：`EnemyCount`, `IsEmpty`

**ColumnManager**：
- `AddEnemyToColumn(col, enemy)`, `AddEnemiesToColumn(col, List<Enemy>)`, `AddEnemiesToAllColumns(List<Enemy>[])`
- `RemoveEnemyFromColumn(col, enemy)`, `ClearAllColumns()`, `ClearColumn(col)`
- `UpdateEnemyRow(col, enemy)`
- `GetColumn(col)`, `GetFrontEnemy(col)`, `GetEnemyAt(col, row)`, `GetEnemiesInColumn(col)`, `GetAllEnemies()`, `GetEnemiesInRange(col, rangeRows)`, `GetAllEnemiesInRange(rangeRows)`, `GetEnemiesByRowLimit(maxRowIndex)`

**RowFormation** (static)：
- `GetColumnOffsetX(rowIndex, columnIndex, maxRow, ...)` — 主偏移计算
- `GetRowOffsets(rowIndex, maxRow, ...)` — 返回单排 `float[5]`
- `DrawFormationGizmos(...)` — Scene 视图调试绘制

## 依赖模块

- `Column` 依赖 `Enemy`（读 state, rowIndex, columnIndex，调用方法）
- `ColumnManager` 依赖 `Column`, `Enemy`
- `EnemyConfig`, `HeroConfig`, `RowFormationPreset`, `StageConfig` 均为 Unity ScriptableObject，无代码依赖
- `RowFormation` 为纯静态工具类，无 MonoBehaviour 依赖
- `WaveConfig`, `RowConfig` 为纯可序列化数据类

## 重要规则

- **链式补齐**：敌人死亡填补时仅启动第一个存活敌人移动，后续敌人通过 `OnRushMoveComplete` 事件链式触发 — 每个必须完全完成后一个才启动
- **禁止 SetRowIndex 瞬移**：`RemoveEnemy()` 和 `UpdateEnemyRow()` 不再调用 `SetRowIndex()`，改为设置 `targetRow` 让 `UpdateMovement()` 逐步前进
- **Dead 敌人过滤**：两方法在重排时均过滤 Dead 状态敌人，防止链中断
- **阵型优先级**：手动 `float[]` > Preset ScriptableObject > 公式。`maxVisibleRows` 作固定分母（非动态 maxRow），保证列失去敌人时位置稳定
- **rowIndex 语义**：0 = 最前排（最靠近玩家，Z 最小），越大越远

## 扩展指南

- **新敌人类型**：通过 Create 菜单创建 `EnemyConfig` 资产，设置 enemyId、数值、弱点倍率
- **新武将**：创建 `HeroConfig` 资产，赋给 `PlayerState.heroConfig`
- **新阵型**：创建 `RowFormationPreset` 资产，配置每排偏移，赋给 `StageConfig.formationPreset`
- **新关卡**：创建 `StageConfig` 资产，配置 waves/rows，拖入 `StageController.stageConfig`
