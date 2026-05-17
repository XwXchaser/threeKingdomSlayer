# Enemy 模块

## 模块名称
Enemy（敌人实体与对象池）

## 主要职责
敌人实体的完整生命周期（状态机、移动、攻击、受伤、死亡及 DOTween 动画），以及按 enemyId 索引的对象池。

同时定义项目全局枚举：`EnemyState`、`DamageType`。

## 核心类

| 类 | 说明 |
|---|---|
| `Enemy` (MonoBehaviour) | 完整敌人实体。所有属性通过 `[SerializeField]` 直接序列化在预制体上（enemyName、enemyId、maxHealth、occupySlots、attackSpeed/Damage/Range/SpawnDuration/DrawDuration、moveSpeed、maxPoise、stunDuration、launchDuration/YHeight、launchedDamageTakenMultiplier/HitExtendDuration、coinReward、isBoss、6种伤害倍率、parryStunThresholds）。运行时每实例分配唯一 `instanceId`，`DebugTag` 属性（格式 `#3(101)`）用于调试日志区分同预制体不同实例。状态机：Idle/Moving/Attacking/Stunned/Launched/Dead。处理：逐排移动（DOTween 弹跳）、三阶段攻击动画（AttackSpawn 前冲+翻转可打断 → AttackDraw 收招不可打断 → 冷却）、带闪烁效果的受伤反馈（材质实例方案 + 隔离式 punch scale tween）、死亡序列（弹跳 + 旋转 + 重力坠落）、按排透明度。链式补齐系统：`pendingRushMove`、`targetRow`、`rushMoveDelayTimer`、`OnRushMoveComplete` 事件。内置 `EnemyHealthBar` 组件管理头顶血条。
| `EnemyState` (enum) | Idle, Moving, Attacking, QTEAttacking, Stunned, Launched, Dead |
| `DamageType` (enum) | Stab, Slash, Pierce, Sweep, Launch, Poise |
| `EnemyPool` (MonoBehaviour, singleton) | 按 enemyId 索引的对象池。自动从 `Resources/EnemyPrefabs/` 注册预制体（命名规则：`Enemy_{id}.prefab`）。若未找到预制体则回退为动态创建红色 Cube。支持 `RegisterPrefab()`、`PrewarmPool()`、`GetEnemy()`、`ReturnEnemy()`、`ClearAllPools()`、`GetEnemyOccupySlots(int enemyId)`（从预制体读取占位数，无需实例化）。为池根节点和运行时敌人根节点维护不同的父 Transform。 |

## 公开接口

**Enemy**：
- `Initialize(int col, int row)` — 从对象池激活（分配唯一 instanceId，使用直接序列化字段，无需传入 config）
- `ParryStunThreshold` (struct) — 招架后血量百分比眩晕阈值，定义于 Enemy.cs
- `TakeDamage(float damage, DamageType type)` — 施加伤害（含弱点倍率、闪烁、弹缩放）
- `TakePoiseDamage(float poiseDamage)` — 架势击破时仅重置架势值，不再造成眩晕
- `CancelAttack()` — 招架打断攻击动作，返回攻击冷却阶段（非眩晕）
- `EnterQTEAttack()` / `ExitQTEAttack()` — Boss QTE 攻击专用：进入/退出 QTE 攻击状态，阻止普通攻击循环
- `CheckParryStunThresholds()` — 仅 Boss（`isBoss=true`）在血量低于配置百分比时触发眩晕
- `Die()` — 启动死亡协程
- `StartMoving(bool isRush)` — 开始前进一排
- `StartAttacking()` — 进入攻击状态（先冷却后动画）
- `Stun(float duration)` — 眩晕（击飞中仅重置Poise不进眩晕；眩晕时Kill DOTween清理移动状态；结束后恢复 pendingRushMove 链）
- `Launch(float duration)` — 挑飞
- `ResetMovementState()` — 为链式补齐重新触发重置状态
- `TryStartRushMove()` — 基于当前状态尝试链式补齐；返回 bool
- `ResetEnemy()` — 为回池做完整重置
- `DebugTag` (string) — 调试标签属性，格式 `#instanceId(enemyId)`，如 `#3(101)`
- 事件：`OnDeath`, `OnDamageTaken`, `OnRushMoveComplete`

**EnemyPool** (singleton)：
- `RegisterPrefab(int enemyId, GameObject prefab)`
- `PrewarmPool(int enemyId, int count)`
- `GetEnemy(int enemyId)` — 返回已激活的 Enemy
- `ReturnEnemy(Enemy enemy)` — 停用并回队
- `ClearAllPools()`

## 依赖模块

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
- **Stun 安全规则**：击飞中（Launched）不进眩晕，仅重置 Poise 防止敌人冻结半空。眩晕进入时 Kill 全部 DOTween + 清理移动状态（`isMovingToNextRow`/`isRushMove`/`moveProgress`）+ `UpdateWorldPosition()` 锁定位置。眩晕结束后恢复 `pendingRushMove` 链
- **DebugTag 调试**：所有 Enemy/Column/ColumnManager 的 Debug.Log 统一使用 `DebugTag`（`#instanceId(enemyId)`）而非裸 `enemyId`，方便区分同预制体不同实例

## 扩展指南

- **新敌人状态**：添加到 `EnemyState` 枚举，在 `Update()` switch 中添加 case，创建 `UpdateXxx()` 方法
- **新伤害类型行为**：在 `GetDamageMultiplier()` 中添加 case，在 Enemy 组件上添加对应的弱点倍率字段
- **新死亡动画**：修改 `DeathBounceAndFall()` 协程或提供可配置的 DOTween 模板
