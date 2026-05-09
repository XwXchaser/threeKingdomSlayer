# Enemy 模块

## 模块名称
Enemy（敌人实体与对象池）

## 主要职责
敌人实体的完整生命周期（状态机、移动、攻击、受伤、死亡及 DOTween 动画），以及按 enemyId 索引的对象池。

同时定义项目全局枚举：`EnemyState`、`DamageType`。

## 核心类

| 类 | 说明 |
|---|---|
| `Enemy` (MonoBehaviour) | 完整敌人实体。状态机：Idle/Moving/Attacking/Stunned/Launched/Dead。处理：逐排移动（DOTween 弹跳）、三阶段攻击动画（AttackSpawn 前冲+翻转可打断 → AttackDraw 收招不可打断 → 冷却）、带闪烁效果的受伤反馈（材质实例方案 + 隔离式 punch scale tween）、死亡序列（弹跳 + 旋转 + 重力坠落）、按排透明度。链式补齐系统：`pendingRushMove`、`targetRow`、`rushMoveDelayTimer`、`OnRushMoveComplete` 事件。`IsBoss` 通过 `config.isBoss` 访问。内置 `EnemyHealthBar` 组件管理头顶血条。 |
| `EnemyState` (enum) | Idle, Moving, Attacking, Stunned, Launched, Dead |
| `DamageType` (enum) | Stab, Slash, Pierce, Sweep, Launch, Poise |
| `EnemyPool` (MonoBehaviour, singleton) | 按 enemyId 索引的对象池。自动从 `Resources/EnemyPrefabs/` 注册预制体（命名规则：`Enemy_{id}.prefab`）。若未找到预制体则回退为动态创建红色 Cube。支持 `RegisterPrefab()`、`PrewarmPool()`、`GetEnemy()`、`ReturnEnemy()`、`ClearAllPools()`。为池根节点和运行时敌人根节点维护不同的父 Transform。 |

## 公开接口

**Enemy**：
- `Initialize(EnemyConfig cfg, int col, int row)` — 从对象池激活
- `TakeDamage(float damage, DamageType type)` — 施加伤害（含弱点倍率、闪烁、弹缩放）
- `TakePoiseDamage(float poiseDamage)` — 架势击破时仅重置架势值，不再造成眩晕
- `CancelAttack()` — 招架打断攻击动作，返回攻击冷却阶段（非眩晕）
- `CheckParryStunThresholds()` — 仅 Boss（`isBoss=true`）在血量低于配置百分比时触发眩晕
- `Die()` — 启动死亡协程
- `StartMoving(bool isRush)` — 开始前进一排
- `StartAttacking()` — 进入攻击状态（先冷却后动画）
- `Stun(float duration)`, `Launch(float duration)` — 控制效果
- `ResetMovementState()` — 为链式补齐重新触发重置状态
- `TryStartRushMove()` — 基于当前状态尝试链式补齐；返回 bool
- `ResetEnemy()` — 为回池做完整重置
- 事件：`OnDeath`, `OnDamageTaken`, `OnRushMoveComplete`

**EnemyPool** (singleton)：
- `RegisterPrefab(int enemyId, GameObject prefab)`
- `PrewarmPool(int enemyId, int count)`
- `GetEnemy(int enemyId)` — 返回已激活的 Enemy
- `ReturnEnemy(Enemy enemy)` — 停用并回队
- `ClearAllPools()`

## 依赖模块

- `EnemyConfig`（Core）
- `StageController`（Managers）— 读取 `GetRushMoveDelay()`, `GetMaxVisibleRows()`, `rowAlphaFactors`, `GetFormationOffset()`, `GetRowSpacing()`, `GetFormationOffsetZ()`
- `EnemyManager`（Managers）— 调用 `OnEnemyMovedForward()`, `OnEnemyAttackPlayer()`
- `DamageNumberManager`（Managers）— 受伤时调用 `Spawn()`
- `PlayerState`（Player）— 通过 `EnemyManager.OnEnemyAttackPlayer()` 间接引用
- `EnemyHealthBar`（UI）— 受击时调用 `Show(percent)`
- **DOTween**：移动弹跳、攻击动画、死亡序列、弹缩放

## 重要规则

- **攻击三阶段**：AttackSpawn（前冲+翻转，可被招架打断）→ AttackDraw（收招返回，不可打断）→ 冷却（attackTimer，间隔的 40%）。冷却可被链式补齐中断；AttackSpawn/AttackDraw 期间补齐等待动画完成。`isAttackAnimating` 和 `isAttackDrawPhase` 为 public，供 AttackSystem 读取判断打断窗口
- **材质实例闪烁**：每个敌人克隆其渲染器材质。`hitFlashTimer` 驱动颜色变白。`ApplyHitFlashImmediate()` 处理敌人同帧死亡的情况（无 Update 循环）
- **死亡异步**：`Die()` 设 state=Dead 并启动协程。`OnDeath` 事件仅在 DOTween 死亡序列完成后触发（弹跳 + 旋转 + 坠落），防止闪烁期间对象被停用
- **链式补齐规则**：`pendingRushMove` 标记 + `TryStartRushMove()`。Idle：立即启动。Attacking/冷却：中断。Attacking/动画：等待。Stunned/Launched：等待。用 `rushMoveDelayTimer` 实现"快移+暂停"节奏
- **对象池命名**：`Resources/EnemyPrefabs/` 中预制体必须命名为 `Enemy_{id}`（如 `Enemy_1.prefab`）
- **受伤抖动隔离**：`TakeDamage` 中的 punch scale tween 使用 per-instance ID（`$"punch_{GetInstanceID()}"`），避免全局 `DOTween.Kill("punchScale")` 误杀其他敌人的 punch tween 导致缩放停在中间值无法恢复
- **架势破碎不再眩晕**：`TakePoiseDamage` 击破架势时仅重置 `currentPoise`，不再调用 `Stun()`。眩晕仅通过 `CheckParryStunThresholds`（Boss 血量百分比阈值）触发
- **Parry 打断规则**：仅当 `parryPoiseDamage >= maxPoise` 且敌人处于 AttackSpawn 阶段时，`CancelAttack()` 打断攻击返回冷却；否则仅造成伤害+架势伤害，不打断不眩晕
- **targetRow 系统**：防止敌人全部挤到第 0 排，给每个敌人设定特定目标。移动到 `rowIndex <= targetRow` 时停止
- **Launch 不重置 targetRow**：`Launch()` 不再重置 `targetRow = -1`。当 Launch 攻击错开命中（AttackWave stagger 0.04s/排），前方敌人先死触发 `RemoveEnemy()` 设置后方敌人的 `targetRow`，若 `Launch()` 重置则落地后链式补齐中断

## 扩展指南

- **新敌人状态**：添加到 `EnemyState` 枚举，在 `Update()` switch 中添加 case，创建 `UpdateXxx()` 方法
- **新伤害类型行为**：在 `GetDamageMultiplier()` 中添加 case，在 `EnemyConfig` 中添加弱点字段
- **新死亡动画**：修改 `DeathBounceAndFall()` 协程或提供可配置的 DOTween 模板
