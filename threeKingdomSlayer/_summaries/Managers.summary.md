# Managers 模块

## 模块名称
Managers（管理器层）

## 主要职责
编排层：伤害跳字显示、敌人生命周期协调、关卡流程控制（开始/胜利/失败/重开）。

## 核心类

| 类 | 说明 |
|---|---|
| `DamageNumberManager` (MonoBehaviour, singleton) | `DamageNumber` TextMeshPro 对象的对象池。`Spawn(Vector3 worldPos, float damage)` 在敌人上方显示红色浮动伤害数字，带随机 X 抖动。 |
| `EnemyManager` (MonoBehaviour, singleton) | 全部存活敌人的中央注册表。处理 `RegisterEnemy()`、死亡回调（`OnEnemyDied` — 从 Column 移除、回池、触发事件）、前移转发（`OnEnemyMovedForward`）、攻击转发（`OnEnemyAttackPlayer`）和 `ClearAllEnemies()`。事件：`OnAnyEnemyDied`, `OnAllEnemiesDied`。 |
| `StageController` (MonoBehaviour, singleton) | 顶层关卡流程。Start 时自动开始关卡。管理 `StageState` 转换（None -> InProgress -> Victory/Defeat）。连线事件：`WaveSpawner.OnAllWavesCompleted` -> Victory；`PlayerState.OnPlayerDied` -> Defeat；`EnemyManager.OnAnyEnemyDied` -> 加击杀/金币。暴露阵型参数。处理 MainMenu 和 Battle 场景加载。`StartStage()` 中调用 `UltimateSystem.ResetEnergy()` 重置大招充能，从 `SaveManager` 恢复铜钱。Victory 时调用 `SaveManager.MarkStageCleared()` / `SetCoins()` 自动存档。`PendingStageConfig` 静态变量接收 MainMenu 传入的关卡配置，Awake 中消费。 |
| `StageState` (enum) | None, Starting, InProgress, Victory, Defeat |

## 公开接口

**DamageNumberManager** (singleton)：
- `Spawn(Vector3 enemyWorldPos, float damage)`

**EnemyManager** (singleton)：
- `RegisterEnemy(Enemy)`, `RegisterEnemies(List<Enemy>)`
- `OnEnemyMovedForward(Enemy)`, `OnEnemyAttackPlayer(Enemy)` — 由 Enemy 调用
- `GetAllAliveEnemies()`, `GetEnemiesInColumn(int)`, `GetFrontEnemyInColumn(int)`
- `AliveEnemyCount`, `IsAllEnemiesDead`
- `ClearAllEnemies()`
- 事件：`OnAnyEnemyDied`, `OnAllEnemiesDied`

**StageController** (singleton)：
- `StartStage()`, `RestartStage()`
- `GoToMainMenu()`, `GoToBattleScene()`
- `GetFormationOffset(int column, int row)`, `GetRowSpacing()`, `GetFormationOffsetZ()`, `GetMaxVisibleRows()`, `GetRushMoveDelay()`
- 属性：`CurrentState`, `IsStageInProgress`, `IsStageVictory`, `IsStageDefeat`
- 事件：`OnStageStateChanged`, `OnStageVictory`, `OnStageDefeat`

## 依赖模块

- **DamageNumberManager**：`DamageNumber` (UI), TextMeshPro
- **EnemyManager**：`ColumnManager` (Core), `EnemyPool` (Enemy), `PlayerState` (Player), `Enemy` (Enemy)
- **StageController**：`StageConfig` (Core), `RowFormation` (Core), `WaveSpawner` (Wave), `EnemyManager`, `PlayerState`, `EnemyPool`, `Enemy`, `UltimateSystem` (Core), `SaveManager` (Core) + Unity `SceneManager`

## 重要规则

- **StageController 自动开始**：`Start()` 中调用 `Invoke(nameof(StartStage), 0.1f)` 延迟一帧，确保所有组件初始化完毕
- **EnemyManager 死亡流程**：`OnEnemyDied` -> 从存活列表移除 -> 取消订阅事件 -> `ColumnManager.RemoveEnemyFromColumn` -> `EnemyPool.ReturnEnemy` -> 触发 `OnAnyEnemyDied` -> 检查 `OnAllEnemiesDied`
- **关卡胜利触发**：`WaveSpawner.OnAllWavesCompleted` 触发 -> `SetState(Victory)` + 金币奖励
- **关卡失败触发**：`PlayerState.OnPlayerDied` 触发 -> `SetState(Defeat)`
- **重复状态保护**：`SetState()` 在应用前检查 `currentState == newState`

## 扩展指南

- **新管理器**：遵循单例模式（`Instance`, Awake/OnDestroy 中 null 检查）。在 `StageController.Start()` 中注册以自动发现
- **新关卡状态**：添加到 `StageState` 枚举，在 `StageController` 中添加转换逻辑，在 `BattleHUD.OnStageStateChanged` 中添加 UI 响应
