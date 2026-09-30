---
id: kd_cbd002a0-2908-4479-a850-5aabdc38bed0
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 骑兵敌人（109）机制设计：骑乘冲锋循环

## 1. 文档定位与当前状态

本文件是 109 骑兵的权威规则记录。规则已由用户逐条确认，已按此实现并在真实节点验证；视觉仍是占位。

- 已确认并实现：骑乘期间**只有冲锋循环**（判定 → 前摇 → 前移 → 命中/被打断 → 原路返航），没有任何普通攻击分支。
- 已确认并实现：冲锋可从任意排直接发起（row ≥ 1），不需要先回到固定排；伤害 = `chargeDamage × (发起排 + 1) × 波次攻击倍率`。
- 已确认并实现：冲锋路径上出现敌人就停在它后一排前探打它，不越过、不并排；前方全空则冲到 row=0 打玩家。
- 已确认并实现：只有被标记为“可打断骑兵冲锋”的攻击能打断（当前 = 蓄力攻击）；Parry 不打断；被打断时本次不结算任何伤害，收招后原路返航。
- 已确认并实现：返航目标是本次冲锋的发起排；途中受阻就停在阻塞者前等待，状态机继续重试。
- 已确认并实现：骑乘期间免疫眩晕与位移（击退/横推），落马后恢复普通受控；真正进入 `Enemy.Launch()` 永久落马，之后按普通 101 逻辑行动（含普通攻击与普通 Per Row 补齐）。

### 1.1 代码与资产落点

- `Assets/Scripts/Enemy/CavalryEnemy.cs`：骑乘态状态机、逐排位移、代际校验、免控与落马入口。
- `Assets/Scripts/Enemy/Enemy.cs`：Idle 委派 `CavalryEnemy.Tick()`、骑乘期禁止普攻、眩晕免疫、冲锋命中表现。
- `Assets/Scripts/Core/ColumnManager.cs`：骑乘期免疫击退/横移、槽位预留、下马后重排。
- `Assets/Scripts/Core/AttackSkillConfig.cs`、`AttackWave`/`SweepEffect`/`StabSweepEffect`、`AttackSystem`：`interruptsCavalryCharge` 标记从配置穿到命中判定。
- `Assets/Resources/EnemyPrefabs/Enemy_109.prefab`（101 骑手 + 105 坐骑占位，非正式美术）与 `Assets/Scripts/Enemy/CavalryVisualController.cs`（占位视觉与调试标签）。

状态口径：

- 「已验证」= 在真实节点 `Assets/RouteData/FakeStage01/Battle_FakeStage01_N1_1.asset` 的完整链路（真 `SpawnEntry` → 真 `StartRouteBattle`）里观测到对应日志，见第 13 节。
- 「待验收」= 机制时序已通过，但手感、视觉与数值需用户实机确认，见第 16 节。

## 2. 规则依据

- `design/percolumn-fillup-rules.md`：普通补齐、逻辑配置排、SpawnEntry、999 节奏门和精确击退回位。
- `design/immutable-constraints.md`：不重叠、未受位移者不跟随移动、调度所有权。
- `design/pixel-character-and-enemy-animation-spec.md`：正面像素角色、动作语义、统一锚点。
- `design/visual-yoffset-system.md`：角色脚底补偿方案。

骑兵冲锋是敌人特殊战斗移动，不是普通补齐：需要独立 owner/事务，不能通过攻击范围或计时器创建 WaveMarch，也不能借用 PushReturn 模拟返航。

## 3. 用户确认的规则（权威）

### 3.1 骑乘形态

1. 只要还在马上，这个单位的唯一攻击行为就是冲锋；**不存在普通攻击分支**，也不需要“先回到某一排才能发起”。
2. 冲锋可以从它当前所在的任意排发起；row=0 没有冲锋距离，必须先返航。
3. 冲锋循环：判定 → 预备（前摇）→ 前移 → 命中或被打断 → 原路返航 → 结束。
4. 返航目标是**本次冲锋的发起排**。
5. 骑乘期间不主动移动其他敌人，也不把自身空位传递给同列后排。

### 3.2 冲锋伤害

- `最终伤害 = chargeDamage × (发起排 + 1) × (attackDamage / BaseAttackDamage)`。
- 即“冲锋距离格数 + 1”：row1 发起 = 2 格，row2 = 3 格，row3 = 4 格，row4 = 5 格。
- 打玩家与打敌人用同一个数值；数值在发起冲锋时锁定，不因中途被打断而重算（被打断则不结算）。

### 3.3 前方出现敌人

- 冲锋路径上（含 row=0）只要出现存活敌人，就停在它后面那一排，像近战一样“往前挪一下再回来”打它，然后进入返航；**不继续冲到底**。
- 该次冲锋到此即视为结束（“完成/中断”），打完后不再结算 row=0 的玩家伤害。
- 移动途中新出现的敌人（例如被玩家位移推进来）同样适用：骑兵回到自己那一排再打它。

### 3.4 打断

- **只有被标记为“可打断骑兵冲锋”的攻击能打断**，判定只有两个来源，**不维护代码里的攻击类型白名单**：① 攻击资产勾选了 `AttackSkillConfig.interruptsCavalryCharge`（把某种攻击视为“蓄力 / 重攻击”就在资产上声明）；② 本次输入为蓄力（`InputManager.isCharged` → `TryExecuteAttack(..., chargedAttack)`，用于斩击这种普通与蓄力共用的动作）。**禁用 `PlayerState.IsCharging` 判定**：它只表示“按键按住”，点击松手时仍可能为真，会把普通戳击误判成蓄力攻击。**玩家输入窗口不受影响**：`InputManager` 只是把已有的 `isCharged` 多传了一个只读参数，`minChargeTime`/长按时长/手势分支一行未改。
- Parry 暂不打断（只造成架势与伤害）。
- 可打断窗口：前摇 + 前移 + 命中伤害提交之前；伤害提交后不可再取消。
- 被打断 = 直接进入本次冲锋的收招（后摇），然后开始返航；**本次不结算任何伤害**。
- 没有“两次有效受击”这类计数阈值；不需要霸体机制。

### 3.5 免控与落马

- 骑乘期间：免疫眩晕、免疫击退与位移（含横向推拉）。这条**不使用**项目里 `isSuperArmor` 那套全局机制表达（那套只做“攻击不打断 + 红描边 + 不播受击动画”），而是骑兵自己的显式规则。
- 真正进入 `Enemy.Launch()`（含旋风、玩家挑飞等所有实际调用来源）→ 永久落马；仅造成 Launch 类型伤害但未实际击飞不落马。
- 落马后恢复普通受控（可被眩晕/击退）与普通补齐。

### 3.6 占位与补齐

- 骑乘期间仍占据其物理排；被排除在普通 WaveMarch 之外，同列后排不会越过它补齐。
- 下马后保留自己的逻辑排身份，重新参与普通 Per Row 补齐（整排清空才前移）；落点不因落地而补位、也不被拉回。
- 骑兵移动不删除逻辑配置排，不因临时空位开启 999 节奏门。

## 4. 术语与参数

### 4.1 冲锋格数

发起冲锋时锁定：`格数 = 发起排 + 1`（包含起点排与目标 row0）。它同时决定伤害与返航目标。

### 4.2 三种排概念

- 配置逻辑排身份：由 `ColumnManager` 登记（`_waveEnemyLogicalRows`），骑兵移动不改变该身份。
- 物理占位排：`Enemy.rowIndex`，用于格子占位、攻击筛选、技能目标查询；逐格提交。
- 视觉位置：连续插值中的渲染坐标，可在两排之间；不直接用于逻辑判定。

### 4.3 可打断标记

`AttackSkillConfig.interruptsCavalryCharge` + `Enemy.TakeDamage(..., interruptsCavalryCharge)`。标记必须**在攻击发起时固化到局部变量并随伤害调用传递**：穿刺/横扫/挑飞的伤害是在释放视觉或时间轴回调里才创建攻击波，如果在回调里再读“当前攻击是否蓄力”，标记会已经丢失（本轮实测踩过这个坑）。

### 4.4 阶段与时间尺度

| 阶段 | 时长（当前参数） | 说明 |
|---|---|---|
| Windup | `windupDuration = 0.3s` | 前摇，不位移，可打断 |
| Charging | 每排 `Enemy.moveSpeed`（109 = 0.2s/排） | 逐排提交物理排；被打断则放弃未提交位移 |
| Striking | `strikeWindup 0.18s` + `strikeRecover 0.22s` | 前探 → 提交伤害 → 收招；伤害提交后不可取消 |
| Retreating | 每排 `moveSpeed` | 原路返航到发起排 |
| 冷却 | `chargeCooldown = 1s` | 返航完成后可再次发起 |

### 4.5 Inspector 参数与已作废字段

当前序列化字段（`Assets/Resources/EnemyPrefabs/Enemy_109.prefab` 的 `CavalryEnemy`）：`cavalryEnemyId`、`chargeDamage`、`windupDuration`、`strikeWindup`、`strikeRecover`、`strikeLungeDistance`、`interruptRecover`、`chargeCooldown`、`spawnEntrySettleDuration`。

已移除、不再使用的旧字段：`chargeHitThreshold`、`chargeRangeRows`、`retreatMaxRow`、`retreatAfterCharge`。

### 4.6 “蓄力 / 重攻击”声明与扩展协议

骑兵打断的声明式入口是 `AttackSkillConfig.interruptsCavalryCharge`，当前数据：

| 攻击资产 | attackType | 勾选 | 说明 |
|---|---|---|---|
| `Assets/Prefabs/UI/Skills/Zhangfei_Pierce.asset` | Pierce | 是 | 蓄力专属动作 |
| `Assets/Prefabs/UI/Skills/Zhangfei_Sweep.asset` | Sweep | 是 | 蓄力专属动作 |
| `Assets/Prefabs/UI/Skills/Zhangfei_Launch.asset` | Launch | 是 | 蓄力专属动作 |
| `Assets/Prefabs/UI/Skills/Zhangfei_Slash.asset` | Slash | 否 | 普通/蓄力共用，由输入分支 `isCharged` 判定 |
| `Assets/Prefabs/UI/Skills/Zhangfei_Stab.asset` | Stab | 否 | 普通攻击 |
| `Assets/ScriptableObjects/Upgrades/Definitions/PhantomWeapon.asset` | Stab | 否 | 幻影分身不走该链路，不打断 |

将来把某类攻击视为“重攻击”时：

1. 在它的攻击资产上勾选 `interruptsCavalryCharge` —— 不改代码、不用动骑兵。
2. 若新攻击由**新的输入分支**触发，只需在该分支传入 `chargedAttack: true`。
3. **不要**在骑兵或 `AttackSystem` 里再维护攻击类型白名单（本轮已删除 `IsChargeOnlyAttack`）：那种写法在重命名或新增攻击类型时会静默失效。

## 5. 状态机与事件

| 阶段 | 进入条件 | 行为 | 退出 |
|---|---|---|---|
| MountedIdle | 初始化、冷却结束 | 判定是否可发起 | Windup / 等待 |
| Windup | 判定通过 | 前摇，不位移 | Charging / 被打断 |
| Charging | 前摇结束 | 逐排前移并提交物理排 | Striking / 被打断 / Retreating（路径硬阻挡） |
| Striking | 前方有敌人或抵达 row0 | 前探 → 提交一次伤害 → 收招 | Retreating |
| Retreating | 命中结束或被打断 | 逐排退回发起排 | MountedIdle / RetreatBlocked |
| RetreatBlocked | 返航目标排被占 | 停在阻塞前等待，`Tick()` 每帧重试 | Retreating / MountedIdle |
| Dismounted | 真正 `Enemy.Launch()` | 取消骑兵动作与未结算伤害，交回普通逻辑 | 普通 101 生命周期 |
| Dead | HP 归零 | 停止动作，按既有死亡流程回收 | 对象池 |

事件：`OnStateChanged(CavalryEnemy)` 供占位视觉与调试标签订阅。伤害提交点是“前探结束”；提交后不可取消。

## 6. 实现要点与约束

### 6.1 单一 Transform 写入者

冲锋/返航期间只有 `CavalryEnemy` 的协程写根 `transform.localPosition`；`Enemy.UpdateWorldPosition()` 只在它自己提交物理排（`SetRowIndex`）时同步。骑乘期间不再有击退/横移写入者，因为 `ColumnManager` 对骑乘骑兵免疫位移。

### 6.2 槽位预留

每段移动前 `TryReserveCavalrySlot`、提交后 `ReleaseCavalrySlot`；WaveMarch / SpawnEntry / 击退 / 横移 / `CanContinuousWaveEnterRow` 都检查该预留。骑乘骑兵被排除在普通 WaveMarch 计划之外。

### 6.3 代际校验的边界

`_generation` 只用于让“可打断阶段的等待与插值”立即失效；**返航与收招不可打断，必须使用当前代际**。若把失效代际传进返航，返航第一步会立即自我中止（本轮实测两次踩到：收招返航路径与命中前探被打断后的主循环路径），勿回归。

此外 `Tick()` 带自愈：骑乘且 Idle 且处在 row0、但还欠一次返航时（`_chargeOriginRow > rowIndex`），会自动补一次返航，避免任何中断路径把它永久留在 row0。

### 6.4 外部订单接管

SpawnEntry / WaveMarch 需要接管移动时调用 `AbortForExternalOrder(reason)`：停止协程、释放预留、把视觉位置交还给物理排。

### 6.5 免控落点

- 眩晕：`Enemy.Stun()` 对骑乘态直接返回。
- 击退/横移：`ColumnManager.ApplyPushWave`、`ExecutePush`、`MoveEnemyToColumnAtRow` 跳过 `Enemy.IsMountedCavalry`。
- 击飞：不免疫，走 `Enemy.Launch()` → 落马。

### 6.6 与普通补齐的并发

骑兵动作不创建普通订单；已有合法订单优先。下马后 `RequestCavalryDismountReflow()` 置脏并请求重排；若当时正处 SpawnEntry/PushReturn/WaveMarch，会延后到本步收尾，不会丢。

### 6.7 敌人侧判定与玩家输入解耦

骑兵打断判定只作用于敌人侧这一条链路：`AttackSystem.InterruptsCavalryCharge` → 特效/攻击波参数 → `Enemy.TakeDamage(..., interruptsCavalryCharge)` → `CavalryEnemy.OnInterruptingHit`。它不参与玩家输入窗口、手势分支或冷却。改判定时只动这条链路，不要改 `InputManager` 的 `minChargeTime`、`longPressDuration`、`isCharged` 计算与分支条件。

## 7. 占位视觉方案（101 骑手 + 105 坐骑）

暂不合图、不生成收费素材：用两个 SpriteRenderer 组合，101 表示骑手、105 仅代表坐骑；两套视觉独立切动作，落马时隐藏 105、保留 101 作为普通士兵。

不能嵌套两个完整敌人 Prefab：105 节点不含 Enemy、EnemyProjectile、Collider、HealthBar、掉落、对象池注册或发箭逻辑。

Prefab 结构（已部署）：

```text
Enemy_109
├─ Enemy / CavalryEnemy / CavalryVisualController
├─ BoxCollider / EnemyHealthBar
└─ CavalryVisualController
   ├─ mountAnchor → MountVisual（105 精灵）
   ├─ riderAnchor → RiderAnchor（101 精灵镜像 sourceRenderer）
   └─ debugLabel（世界空间 TMP，验收用）
```

- 根 Transform 保持逻辑占位与单一移动写入者；根 101 Animator 仍是精灵数据来源，根 Renderer 隐藏后由子骑手显示层镜像。
- 参数全部 Inspector 可追踪：不新增 Resources.Load 或字符串资源查找。
- 骑手与坐骑接受一致的排透明度、波次染色、闪白与描边；不污染共享材质。

## 8. 阶段到视觉的映射（占位）

> 动画制作交接（状态 × 动画矩阵、素材清单、Animator 结构、时间对齐规则、验收清单）见 `design/cavalry-enemy-animation.md`；本节保留占位视觉的当前映射。

| 阶段 | 105 坐骑 | 101 骑手 | 调试标签提示 |
|---|---|---|---|
| MountedIdle | Idle 轻微起伏 | Idle，位于骑乘偏移 | 骑乘等待 / 冷却 |
| Windup | 轻微压低 | 101 攻击准备帧 | 黄色：冲锋准备（可打断） |
| Charging | Walk 加速循环 | 攻击准备姿态 | 橙色：冲锋中（可打断） |
| Striking | 接触停顿 | 101 攻击命中帧（前探→命中→收招） | 命中敌人/玩家；伤害已提交 |
| Retreating | Walk 表现后退 | Idle | 蓝色：返航 |
| RetreatBlocked | Idle | Idle | 返航受阻，等待 |
| 落马过渡（Dismount 执行中） | 侧后偏移、淡出 | 从当前位置脱离座位，切 101 击飞表现 | 红色：落马 |
| Dismounted | 隐藏 | 101 完整普通动作 | 下马 + Enemy 状态 |
| Dead | 若骑乘则一起淡出 | 101 死亡表现 | 标签隐藏 |

正面像素风保持不变；返航不把角色旋转 180 度露背。

## 9. 落马表现

1. 逻辑层先锁定永久下马（`Dismount`），取消未提交的冲锋伤害与动作、释放槽位预留。
2. 骑手由 `mountedRiderOffset` 过渡到 `dismountedRiderOffset`；105 侧移淡出后隐藏。
3. 若同时实际进入 Launched，由 `Enemy` 既有重力/浮空流程负责根高度，骑手局部过渡只处理离座偏移，避免两套重力叠加。
4. 落地后 105 保持隐藏，101 恢复脚底、Idle/Walk/Attack/Hit/Dead，不恢复骑乘。
5. 落马本身不结算击杀、经验、铜钱，不生成第二个敌人，不改变敌人 ID 与逻辑排身份。

## 10. 与 Enemy 视觉实现的接线风险

`Enemy` 缓存根 Renderer/Animator 与原始缩放，受击、死亡、血条都依赖该结构；不能仅把 Renderer/Animator 搬到子节点就算完成。

- 骑兵必须明确绑定 primaryRenderer/primaryAnimator（骑手），普通敌人继续根组件回退；不改动 101/102 Prefab。
- 材质实例创建/销毁、`UpdateAlpha`、波次染色、描边、HitStop、`TriggerHitFlash` 必须覆盖正确的两个视觉对象。
- Rider 只有一个精灵写入者，不能同时由 `EnemySpriteController` 与 Animator 竞争写 sprite。
- 不把 105 完整 controller 的发箭/攻击事件引入坐骑。

## 11. 调试标签与可观察性

标签是只读观察器，由 `debugLabel` + `showDebugState` 控制，正式版本关闭。

```text
109 #实例ID 阶段
发起排 / 格数 / 本次冲锋伤害
物理row | 原因：路径阻塞 / 被打断返航 / 返航受阻 / 击飞落马
```

- phase、落马原因、阻塞原因变化时更新，不在每帧拼接字符串。
- 标签跟随位置可用 LateUpdate，文本只更新脏数据。
- 逻辑事件驱动标签；动画结束不能当作伤害结算信号。

## 12. 生命周期与清理

- Initialize：`Enemy.Initialize` 调用 `CavalryEnemy.ResetCavalry()`，重置骑乘、代际、伤害提交标记、发起排、冷却、入场 settle 与 phase。
- ResetEnemy / OnDisable：停止协程、释放槽位预留、重置视觉与 Animator。
- OnDeath：停止骑兵动作；死亡奖励与回收仍走既有单次流程。
- 落马：`RequestCavalryDismountReflow()` 让逻辑排重排，恢复到普通补齐。
- 三选一暂停：已开始动作随 `Time.timeScale` 冻结；节点切换/重开时硬取消特殊移动，避免旧回调作用于复用实例。

## 13. 本轮已验证记录（真实节点 N1）

链路：`StageController.StartRouteBattle(Battle_FakeStage01_N1_1)` → `WaveSpawner` → `ColumnManager.StartSpawnEntry` → 骑兵循环。以下均为 `[CavalryDiag]` 日志实测。

| 规则 | 观测证据 |
|---|---|
| 入场不先回后排，直接在当前排发起 | `SPAWN_ENTRY_COMPLETE row=2` 后直接 `CYCLE_START originRow=2 grids=3`，中间无返航 |
| 伤害 = chargeDamage × (发起排+1) | 每次命中 `damage=24.0`（8×3） |
| 冲到 row0 打玩家后原路返航 | `2→1→0` → `STRIKE_PLAYER 24` → `RETREAT_STEP_COMMIT 0→1→2` → `RETREAT_DONE` → `CYCLE_END row=2` |
| 标记攻击命中即打断，本次不结算 | `INTERRUPT row=1` → `RECOVERY_RETREAT fromRow=1` → 返航 `1→2`；该循环无任何 `STRIKE_*` |
| 打断后下次冲锋仍从发起排开始 | 下一轮 `origin=2 grids=3 damage=24`，并再次命中玩家 |
| 普通攻击不打断 | 无标记攻击命中后 `interrupt=False`，冲锋照常打到玩家 |
| 前方出现敌人则打它并返航 | `CHARGE_CONTACT_ENEMY blocker=#13(101)` → `STRIKE_ENEMY damage=24` → 直接返航 |
| 返航受阻停在阻塞前并重试 | `RETREAT_BLOCKED from=1 nextRow=2 target=2` → 路径清空后 `RETREAT_STEP_COMMIT row=2` |
| 击飞落马并恢复受控 | `Launch()` → `DISMOUNT mounted=False phase=Dismounted`；落地后 `Stun` 生效、`MoveEnemyToColumnAtRow` 成功 |
| 骑乘期间免控 | 同一实例落马前 `MoveEnemyToColumnAtRow` 返回 false、`Stun` 不生效 |
| 点击戳击（普通攻击）命中冲锋中的骑兵 | 不打断，冲锋照常打到玩家（`interrupt=False`） |
| 蓄力穿刺命中冲锋中的骑兵 | 打断，收招后返航回发起排，本次不结算；打在命中前探阶段同样生效 |
| 命中前探（Striking）阶段被打断 | 取消未提交伤害，返航回发起排，不会停在 row0 |
| 穿刺走“非蓄力输入分支”（仅靠资产声明 `interruptsCavalryCharge`） | 仍然打断并返航回发起排，证明数据声明独立于输入分支生效 |

验证过程中修复的缺陷（均复发过，勿回归）：

1. 被打断进入返航时误用了失效代际 → 返航第一步立即中止；后来在“命中前探被打断”路径上再次出现，表现为骑兵停在该排既不返航也不重新发起。现返航内部固定取当前代际，并加了 row0 欠返航自愈。
2. `interruptsCavalryCharge` 用 `PlayerState.IsCharging` 实时判定 → 普通点击戳击也打断冲锋；但改成字段标记后，穿刺在释放回调里读字段时已被重置，标记又丢失。现改为“攻击类型判定 + 发起时固化到局部变量”双层。

验证限制：用的是“带/不带标记的攻击波”直接命中，覆盖了 `特效 → Enemy.TakeDamage → 骑兵打断` 整段；`AttackSystem` 判定“蓄力攻击”这一小段为编译与代码审查确认，尚未用真实手指蓄力操作实机复现。

用户实机反馈（修补后）：点击戳击不再打断、被打断后正常返航；本轮未再发现问题。第 16 节其余项仍待继续覆盖。

## 14. 已作废规则（历史，仅供追溯）

以下规则在本轮被用户明确取代，不再实施：

- 已作废：“冲锋中累计 2 次有效受击才打断”（阈值计数）→ 改为标记攻击命中即打断，`chargeHitThreshold` 已移除。
- 已作废：“先回到最后排（row=4）才能发起冲锋” → 改为任意排直接发起，`retreatMaxRow`、`chargeRangeRows` 已移除。
- 已作废：“返航（回撤）目标是可见最后排” → 改为回到本次冲锋的发起排。
- 已作废：“打断时按移动进度过半提交目标排（可出现停在 row=0 再退回）” → 改为只按实际到达的排，未提交位移一律放弃。
- 已作废：“row=0 且回撤受阻时使用 101 普通攻击”“骑乘态存在普通攻击分支” → 骑乘期完全没有普通攻击。
- 已作废：“Parry 可打断冲锋” → 暂时取消。
- 已作废：“全程红色霸体（`isSuperArmor` 表达）+ 两次受击落马” → 改为显式免控规则 + 标记攻击打断。
- 已作废：“row=0 只有视觉前探、物理排不提交” → 改为物理排可以真正到 row0。

## 15. 验收矩阵

| 场景 | 必须观察到的结果 |
|---|---|
| 骑兵处于任意排（row ≥ 1）且冷却就绪 | 直接发起冲锋，不需要先返航 |
| row0 前方全空 | 冲到 row0，打玩家一次 `chargeDamage × (发起排+1)`，随后原路返航 |
| row0 有 101 | 停在其后一排前探打它，玩家不受伤；不产生同排重叠 |
| 冲锋中前方被塞入敌人（位移） | 回到本排打该敌人，然后返航，不继续冲到底 |
| 进入冲锋后被标记攻击（蓄力）命中 | 打断，收招后返航到发起排；本次不结算任何伤害 |
| 进入冲锋后被普通攻击命中 | 不打断，照常冲到 row0 或打到挡路者 |
| 前摇期间被标记攻击命中 | 同样打断（此时无位移，直接收招） |
| 命中伤害已提交后再被打断 | 不可取消，伤害保持已结算 |
| 返航途中目标排被占 | 停在阻塞者前等待；路径清空后继续返航到发起排 |
| 冲锋/返航中受到眩晕或击退 | 均无效（骑乘期免控） |
| 真正 `Enemy.Launch()`（含旋风） | 永久落马，落地后按 101 逻辑；可被眩晕/击退 |
| 仅受 Launch 类型伤害未击飞 | 保持骑乘 |
| 下马后所在排 | 参与普通 Per Row 补齐，落点不被拉回、不触发别人补位 |
| 冲锋期间同列后排敌人 | 不越过骑兵补齐 |
| 冲锋/落马中死亡 | 只有一次死亡结算与回收，无迟到伤害 |
| 对象池复用 | 新实例恢复骑乘、代际与计数归零 |

## 16. 待用户验收 / 未覆盖

1. 手感与时长：`windupDuration`、`strikeWindup`、`strikeRecover`、`interruptRecover`、`chargeCooldown` 的数值与整体节奏需要实机确认。
2. 实机蓄力攻击：用真实手指蓄力（穿刺/横扫/挑飞）打正在冲锋的骑兵，确认打断生效；同时确认快速点击（非蓄力）不打断。
3. 视觉：101+105 占位组合的比例、遮挡、脚底与血条位置；正式骑兵美术替换。
4. 伤害位置依赖：骑乘期免位移且不参与补齐，骑兵实际钉在入场逻辑排上；N1 每下固定 24。若希望更快成长，需要后续机制把它推到更靠后的排。
5. 多骑兵同列、Boss 墙、999 节奏门、地刺同步死亡、长时间节点切换与并发补齐仍未覆盖。

## 17. 变更日志

- 本轮：按用户最终确认的规则重写为“骑乘冲锋循环”；移除两次受击阈值、固定返航排、过半提交、Parry 打断、骑乘普攻；新增任意排发起、格数伤害、打挡路者、标记攻击打断、骑乘免控、返航到发起排。真实节点 N1 全链路验证通过。
- 修补（实机反馈）：普通点击戳击误打断 + 打断后停在原地不返航。原因分别是 `PlayerState.IsCharging` 实时判定与穿刺延迟伤害丢失标记、以及返航使用失效代际；现已改为输入侧 `isCharged` 标记 + 发起时固化，且返航代际自取当前值并带 row0 自愈。
- 判定去白名单：删除 `AttackSystem` 里写死的“蓄力专属类型”列表，改为“资产勾选 `interruptsCavalryCharge` + 本次输入为蓄力”两个来源；并在 `Zhangfei_Pierce/Sweep/Launch` 三个资产上勾选，使“重攻击”类声明纯数据化。
