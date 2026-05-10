# Player 模块

## 模块名称
Player（玩家系统）

## 主要职责
玩家端系统：手势到攻击的输入映射、含目标选择的攻击执行、玩家属性及冷却管理。

同时定义项目全局枚举：`AttackType`、`StageState`。

## 核心类

| 类 | 说明 |
|---|---|
| `AttackSystem` (MonoBehaviour, singleton) | 执行 6 种攻击。`TryExecuteAttack(AttackType, targetColumn)` 通过 PlayerState 检查冷却，委托给类型专用方法，触发 AttackWave。仅当至少命中一个敌人时才触发冷却。包含波位置计算和 UI 伤害查询。命中后调用 `UltimateSystem.AddEnergyForAttack(attackType)` 增加大招充能。 |
| `InputManager` (MonoBehaviour, singleton) | 统一鼠标 + 触摸输入。手势识别：点击 -> Stab，长按（>=`longPressDuration`）-> Pierce（若充能 >=`minChargeTime`），带角度分析的滑动 -> Launch（近垂直）、Sweep（近水平）、Slash（斜向/默认）。无充能快速滑动：近垂直（夹角 < `verticalSwipeThreshold`）-> Parry，其余 -> Slash。暴露 `OnChargeBegan/Updated/Ended` 供 UI 充能指示器（`ChargeIndicatorController`）使用。列映射：屏幕坐标投影到世界坐标，匹配最近存活敌人所在列。 |
| `PlayerState` (MonoBehaviour, singleton) | 玩家属性：血量、复活、击杀、金币、当前波次、关卡状态。6 个独立冷却计时器。`TakeDamage()` 处理死亡/复活。`ResetPlayer()` 用于关卡重开。全部属性变化均有事件。 |
| `AttackType` (enum) | Stab, Slash, Pierce, Sweep, Launch, Parry |
| `StageState` (enum) | None, Starting, InProgress, Victory, Defeat |

## 公开接口

**AttackSystem** (singleton)：
- `bool TryExecuteAttack(AttackType type, int targetColumn)` — 主入口；仅命中时返回 true
- `float GetAttackDamage(AttackType type)` — UI 展示用
- 属性：`columnManager`, `playerState`

**InputManager** (singleton)：
- 事件：`OnAttackExecuted(AttackType, int column)`, `OnChargeBegan(Vector2 screenPos)`, `OnChargeUpdated(Vector2, float progress)`, `OnChargeEnded()`
- 可配置：`longPressDuration`, `minChargeTime`, `swipeThreshold`, `verticalSwipeThreshold`, `horizontalSwipeThreshold`

**PlayerState** (singleton)：
- `TakeDamage(float damage)`, `ResetPlayer()`
- `IsAttackReady(AttackType)`, `StartCooldown(AttackType)`, `GetCooldownProgress(AttackType)`
- `AddKill()`, `AddCoins(int)`, `SetCurrentWave(int)`, `SetStageState(StageState)`
- 事件：`OnHealthChanged`, `OnReviveCountChanged`, `OnKillCountChanged`, `OnCoinChanged`, `OnWaveChanged`, `OnStageStateChanged`, `OnPlayerDied`

## 依赖模块

- **AttackSystem**：`ColumnManager` (Core), `PlayerState`, `HeroConfig` (Core), `AttackWave` (Attack), `Enemy`, `DamageType`, `UltimateSystem` (Core)
- **InputManager**：`AttackSystem`, `AttackType`, `Enemy`（列映射用）, Unity `Camera.main`, `Input` 系统
- **PlayerState**：`HeroConfig` (Core), `StageState` 枚举（自定）, `AttackType` 枚举（自定）

## 重要规则

- **"命中才冷却"规则**：`TryExecuteAttack` 仅在攻击实际命中敌人时才调用 `StartCooldown`，空挥攻击免费
- **充能门控**：仅 `pressDuration >= minChargeTime`（默认 0.5s）时滑动才解析为充能攻击（Pierce/Sweep/Launch）。低于此阈值的快速滑动一律映射为 Slash
- **角度手势**：纯方向判定 — 无屏幕区域划分。与垂直方向夹角 < 阈值 -> Launch；与水平方向夹角 < 阈值 -> Sweep；其余 -> Slash
- **含死区的列映射**：`GetColumnFromScreenPosition` 将敌人世界位置投影到屏幕空间，找最近列。若最近敌人距离超过半列宽则返回 -1（攻击被阻止）
- **Parry 参数现已从 HeroConfig 读取**：`parryDamage`、`parryPoiseDamage`、`parryRangeRows`、`parryCooldown` 均配置在 `HeroConfig.asset` 上，AttackSystem 自身的旧字段已弃用
- **Parry 使用 Stab 伤害类型**（非 Poise），避免 `poiseDamageMultiplier` 降低实际伤害
- **AttackSystem 仅命中时消耗冷却**；InputManager 是手势到攻击映射的守门人

## 扩展指南

- **新攻击类型**：添加到 `AttackType` 枚举，在 `AttackSystem.TryExecuteAttack()` 中添加 case，添加 Execute 方法，在 `PlayerState` 中添加冷却计时器，在 `HeroConfig` 中添加字段，在 `InputManager.ProcessGesture()` 中添加手势规则，在 `BattleHUD` 中添加 UI
- **新手势**：在 `ProcessGesture()` 中添加检测逻辑，在 InputManager Inspector 中添加阈值参数
- **新玩家属性**：在 `PlayerState` 中添加字段 + 事件，在 `BattleHUD` 中添加 UI 绑定
