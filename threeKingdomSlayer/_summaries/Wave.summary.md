# Wave 模块

## 模块名称
Wave（波次生成）

## 主要职责
按 `StageConfig` 波次生成敌人。读取排/敌人配置，从对象池实例化敌人，分配列/排位置，触发生成后初始阵型前压，监控波次完成。

## 核心类

| 类 | 说明 |
|---|---|
| `WaveSpawner` (MonoBehaviour, singleton) | 从 `StageConfig` 波次生成敌人。`SpawnNextWave()` 遍历波次排，调用 `SpawnRow()` 进行：通过 `enemyPool.GetEnemyOccupySlots()` 计算总槽位数以居中、决定共享 rowIndex（列内最大 + 可选偏移 + 非 boss 波 +2）、从对象池取敌人、直接 `Initialize(col, row)`（属性从预制体 Enemy 组件读取）、向 EnemyManager 注册。生成后对所有列触发 `TriggerFillForward()`。协程 `WaitForWaveClearAndNotify()` 每 0.5s 轮询 `EnemyManager.IsAllEnemiesDead` 后触发 `OnWaveCompleted`。事件：`OnWaveStarted`, `OnWaveCompleted`, `OnAllWavesCompleted`。 |

## 公开接口

- `StartWaveSpawning()` — 开始波次序列（由 StageController 调用）
- `SpawnNextWave()` — 前进到下一波（后续可能由"继续"按钮调用）
- 属性：`CurrentWaveIndex`, `IsSpawning`, `IsWaveComplete`, `IsAllWavesCompleted`, `TotalWaves`
- 事件：`OnWaveStarted(int waveIndex)`, `OnWaveCompleted(int waveIndex)`, `OnAllWavesCompleted`

## 依赖模块

- `StageConfig`, `WaveConfig`, `RowConfig`（Core）
- `EnemyPool`（Enemy）— `GetEnemy()`, `ReturnEnemy()`, `GetEnemyOccupySlots()`
- `ColumnManager`（Core）— `GetColumn()`, `GetColumnEnemyCount()`, `TriggerFillForward()`
- `EnemyManager`（Managers）— `RegisterEnemy()`, `IsAllEnemiesDead`
- `Enemy`（Enemy）— 调用 `Initialize()`
- `PlayerState` — 调用 `SetCurrentWave()`

## 重要规则

- **敌人属性从预制体读取**：`enemyPool.GetEnemyOccupySlots(enemyId)` 从预制体 Enemy 组件读取占位数，`Initialize(col, row)` 使用预制体上的直接序列化字段
- **Inspector 优先加载**：`enemyConfigs` Inspector 列表优先；回退到 `Resources.LoadAll<EnemyConfig>()`
- **敌人居中**：合计该排所有敌人的槽位数，`startColumn = (5 - totalSlots) / 2` 居中
- **rowIndex 偏移**：非 boss 波敌人生成在 `rowIndex + 2`（更靠后排，营造"推进"感）。Boss 波生成在 `rowIndex`（直接接敌）
- **生成后补齐**：生成后所有列触发 `TriggerFillForward()`，将敌人向队列前方压缩

## 扩展指南

- **新波次行为**：在 `WaveConfig` 中添加字段（如生成延迟、特殊规则），在 `SpawnRow()` 或新方法中实现
- **Boss 波特殊处理**：在 `WaveConfig` 中添加字段（如 boss enemyId、特殊攻击模式），在 `SpawnRow()` 中对 `isBossWave` 添加逻辑
- **多波自动推进**：当前等待手动"继续"。如需自动推进，在 `StageController` 中从 `OnWaveCompleted` 处理器调用 `SpawnNextWave()`
