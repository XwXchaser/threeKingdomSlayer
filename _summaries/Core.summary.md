# Core 模块

## 模块名称
Core（核心数据结构与配置）

## 主要职责
游戏阵型系统、敌人/武将数值、关卡布局的数据结构与配置。定义 5 列网格模型、基于排的梯形阵型定位，以及策划工作流用的 ScriptableObject 配置资产。包含存档系统和关卡配置管理器。

## 核心类

| 类 | 说明 |
|---|---|
| `Column` (Serializable) | 管理单列敌人列表。Index 0 = 最前排。处理 `RemoveEnemy()`（链式触发补齐前移）、`TriggerFillForward()`（生成后初始压缩）和链式回调 `OnColumnRushMoveComplete`。 |
| `ColumnManager` (MonoBehaviour) | 5 个 Column 实例的单例式管理器。提供按列增删改查、范围查询（`GetEnemiesInRange`、`GetAllEnemiesInRange`、`GetEnemiesByRowLimit`）和非死亡前移的 `UpdateEnemyRow()` 链式逻辑。 |
| `EnemyConfig` (ScriptableObject) | 敌人数值：`enemyName`、血量、`occupySlots`(1-5)、攻速/攻击/射程、移速、最大韧性、眩晕/击飞时长、金币奖励、`isBoss` 开关、6 种伤害类型弱点倍率、`parryStunThresholds`（Boss 招架血量百分比眩晕阈值数组）。菜单：`Assets > Create > 一夫当关/敌人配置` |
| `HeroConfig` (ScriptableObject) | 武将数值：`heroId`、血量、复活次数、复活血量百分比。6 种攻击独立字段，其中招架含 `parryDamage/parryPoiseDamage/parryRangeRows/parryCooldown`。全局 `damageBonusPercent`。菜单：`Assets > Create > 一夫当关/武将配置` |
| `RowFormationPreset` (ScriptableObject) | 每排 X 偏移预设表。含 `List<RowData>`，每个 `RowData` 存 `float[5] columnOffsets`。菜单：`Assets > Create > 一夫当关/排阵型预设` |
| `RowFormation` (static class) | 静态 X 轴列偏移计算器。三套优先级方案：**A**（手动 `float[] manualRowHalfWidths`，最高优先级，数组索引=排索引）、**B**（`RowFormationPreset` ScriptableObject 预设表）、**C**（公式：`Lerp(maxSpread, minSpread, t^powerCurve)`，`t = rowIndex/(maxVisibleRows-1)`）。提供 `DrawFormationGizmos()` |
| `StageConfig` (ScriptableObject) | 关卡配置：`stageId`/`stageName`、`List<WaveConfig> waves`、`killStreakThresholds` 连杀阈值、`clearCoinReward` 通关金币、`rowAlphaFactors` 排透明度因子、`maxVisibleRows` 最大可见排数、`rushMoveDelay` 补齐移动间隔。阵型参数：`formationPreset`（预设表，优先级 B）、`manualRowHalfWidths`（手动每排半宽数组，优先级 A 最高）、`formationMaxSpread`/`formationMinSpread`/`formationPowerCurve`（公式参数，优先级 C）、`rowSpacing` 排间距、`formationOffsetZ` 整体 Z 偏移。菜单：`Assets > Create > 一夫当关/关卡配置` |
| `WaveConfig` (Serializable) | 单波：waveId、isBossWave、`List<RowConfig> rows` |
| `RowConfig` (Serializable) | 单排：`int[5] enemyIds`（每列槽位一个） |
| `StageConfigManager` (MonoBehaviour, singleton) | 关卡配置管理器。挂载在 MainMenu 场景的独立 GameObject 上，Inspector 中拖入 StageConfig 资产并排序。列表顺序决定关卡解锁顺序，为关卡列表的唯一来源，不再自动扫描 Resources 文件夹。提供 `GetStages()`、`GetStageById(int)` |
| `StageRegistry` (ScriptableObject) | 关卡注册表。存放所有 StageConfig 的引用列表，供 StageController 跨场景按 stageId 查找。菜单：`Assets > Create > 一夫当关/关卡注册表` |
| `SaveManager` (static class) | 存档管理器。基于 `PlayerPrefs` + JSON 序列化。存储已通关关卡 ID 列表、铜钱数。`HasSave` / `Load()` / `Save()` / `Delete()` / `MarkStageCleared(int)` / `SetCoins(int)` / `GetNextAvailableStageId()` / `IsStageCleared(int)` |
| `SaveData` (Serializable) | 存档数据结构：`List<int> clearedStageIds`、`int coinCount`、`bool tutorialCompleted` |

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

**StageConfigManager** (singleton)：
- `Instance` — 静态单例
- `stages` — `List<StageConfig>` 序列化字段（Inspector 可编辑）
- `GetStages()` — 返回所有关卡
- `GetStageById(int stageId)` — 按 ID 查找

**SaveManager** (static)：
- `HasSave` — 是否存在存档
- `Load()` — 加载存档（带缓存）
- `Save()` / `Save(SaveData)` — 保存存档
- `Delete()` — 删除存档
- `MarkStageCleared(int stageId)` — 标记关卡已通关
- `SetCoins(int amount)` — 保存铜钱数
- `GetNextAvailableStageId()` — 最大已通关 ID + 1
- `IsStageCleared(int stageId)` — 检查关卡是否已通关

## 依赖模块

- `Column` 依赖 `Enemy`（读 state, rowIndex, columnIndex，调用方法）
- `ColumnManager` 依赖 `Column`, `Enemy`
- `EnemyConfig`, `HeroConfig`, `RowFormationPreset`, `StageConfig`, `StageRegistry` 均为 Unity ScriptableObject，无代码依赖
- `RowFormation` 为纯静态工具类，无 MonoBehaviour 依赖
- `WaveConfig`, `RowConfig`, `SaveData` 为纯可序列化数据类
- `SaveManager` 为纯静态工具类，依赖 Unity `PlayerPrefs` + `JsonUtility`
- `StageConfigManager` 依赖 `StageConfig`

## 重要规则

- **链式补齐**：敌人死亡填补时仅启动第一个存活敌人移动，后续敌人通过 `OnRushMoveComplete` 事件链式触发 — 每个必须完全完成后一个才启动
- **禁止 SetRowIndex 瞬移**：`RemoveEnemy()` 和 `UpdateEnemyRow()` 不再调用 `SetRowIndex()`，改为设置 `targetRow` 让 `UpdateMovement()` 逐步前进
- **Dead 敌人过滤**：两方法在重排时均过滤 Dead 状态敌人，防止链中断
- **阵型优先级**：`manualRowHalfWidths`（最高）> `formationPreset`（预设表）> 公式（`Lerp(maxSpread, minSpread, t^powerCurve)`）。`maxVisibleRows` 作固定分母（非动态 maxRow），保证列失去敌人时位置稳定。`t = rowIndex / (maxVisibleRows - 1)`
- **rowIndex 语义**：0 = 最前排（最靠近玩家，Z 最小），越大越远
- **关卡配置来源**：MainMenuUI 从 `StageConfigManager.stages` 获取列表（Inspector 配置），Battle 场景通过 `StageController.PendingStageConfig` 静态变量跨场景接收
- **存档时机**：通关时自动调用 `SaveManager.MarkStageCleared()` + `SetCoins()`；Battle 场景加载时恢复铜钱数

## 扩展指南

- **新敌人类型**：通过 Create 菜单创建 `EnemyConfig` 资产，设置 enemyId、数值、弱点倍率
- **新武将**：创建 `HeroConfig` 资产，赋给 `PlayerState.heroConfig`
- **新阵型**：创建 `RowFormationPreset` 资产，配置每排偏移，赋给 `StageConfig.formationPreset`
- **新关卡**：
  1. `Assets > Create > 一夫当关 > 关卡配置` 创建 StageConfig 资产，配置 waves/rows
  2. 将新 StageConfig 拖入 MainMenu 场景中 `StageConfigManager` 的 `Stages` 列表，拖拽排序
  3. 同时拖入 `Assets/Resources/StageRegistry.asset` 的 `stages` 列表以保持同步（StageController 跨场景查找用）
