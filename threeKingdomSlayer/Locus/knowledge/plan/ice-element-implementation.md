---
id: kd_b1b74fff-723e-422f-b1d0-1b6520edef04
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 冰系切片落地计划（冻结 / 冻结概率 / 破冰推动）

> 规则依据：`design/ice-element-link.md`（冰系唯一规则源）。
> 本文只写"改哪里、怎么改、怎么验"，不含最终数值（数值全部走 Inspector）。
> **跨对话接手先读 `plan/ice-element-handoff-20261018.md`**（现状、待办、已修坑、工作区风险）。
> 术语：`P0…P6` 为可独立验收的阶段，前一阶段未过验收不进入下一阶段。

## 0. 范围

首切片只放开普通敌人（1011 优先，其后 103 / 105 / 106）。**101 不进关卡，不作为验证对象**。1021 盾卫、109 骑兵、BOSS 在 P2 通过后再逐个开，因为它们的冻结例外各自需要单独验收。

## 0.5 进度

| 阶段 | 状态 | 备注 |
|---|---|---|
| **P0 字段与数据层** | ✅ 已完成（2026-10-17） | 编译通过；10 个招式资产已填值；Console 0 error |
| **P1a 挂起令牌（卡肉改造）** | ✅ 已完成（2026-10-17） | `SuspendSource` + `PushSuspend`/`PopSuspend`，行为不变 |
| **P1b 冻结状态与概率** | ✅ 已完成（2026-10-17） | `Frozen` 状态、`Freeze` / `BreakFreeze`、`ApplyIceHit` 概率模型、染色占位、**1011 参数已配** |
| **P1c 在途位移落位** | ⬜ 未开始 | `CompletePendingMovement()`（需重构 `UpdateMovement` 的完成分支） |
| **P6a 冰雾接线（占位美术）** | ✅ 已完成（2026-10-17） | 可在游戏内直接测试：`IceMist` 分支 + 冰蓝染色占位 + `ActiveSkill_IceMist.asset` 入 Rare 池 + Battle 场景 `DEBUG_IceTest_Bootstrap` 开局注入 |
| **P3+P4 破冰预备 / 破冰结算** | ✅ 已完成（2026-10-17） | 一次性 BUFF 容器 + 攻击会话 + 双倍伤害 + 破冰动画（与 hitted 同步）+ BUFF 消费；Play Mode 实测通过 |
| **P3b 破冰预备 HUD** | ✅ 已完成（2026-10-18） | `ElementalBuffHUD`：零场景接线，首次获得时自动创建图标，消费后淡出；占位图标为程序化雪花 |
| **P7 美术接线** | ✅ 已完成（2026-10-18） | `SpriteSequenceEffect` 序列帧播放器；新增 `freezeMeltEffectPrefab`（自然解冻专用）；素材已部署（见下） |
| **P7b 素材部署** | ✅ 已完成（2026-10-18） | 素材在 `Assets/Sprites/EffectSprites/ice/`；三个 Prefab 统一挂在共享配置 `Assets/ScriptableObjects/Combat/FreezePresentation.asset`，**12 个敌人预制体全部引用它**（不再逐个挂） |
| **P5 推动与碰撞** | ✅ 已完成（2026-10-17） | `ApplyBreakIcePush`（逐排推进 / 撞停）+ 碰撞伤害 + 延迟回位；Play Mode 实测通过 |

P0 实际改动：

- `Assets/Scripts/Core/AttackSkillConfig.cs`：新增 `ColumnMode` 枚举 + `columnMode` / `isHeavy` / `breakIcePushRows`。
- `Assets/Scripts/Enemy/Enemy.cs`：`EnemyState` 追加 `Frozen` / `IceBreaking`；`DamageType` 追加 `Ice` / `Collision`；新增冻结参数（时长 / 概率上限 / 衰退延时与速率 / 前摇倍率 / 抗性 / 免疫 / 破冰动画时长 / 回位延时）。
- `Assets/Scripts/Core/HitFeedbackManager.cs`：`HitFeedbackSource` 追加 `Collision`。
- `Assets/Scripts/Core/ActiveSkillDefinition.cs`：`ActiveSkillEffectType` 追加 `IceMist`。
- `Assets/Scripts/Core/UpgradeDefinition.cs`：`TimedAoeLevelConfig` 新增 `iceFreezeChancePerHit`。
- `Assets/Scripts/Core/StageConfig.cs`：新增 `collisionDamage`。
- 10 个招式资产按代码实测填值（Stab / 枪突 1–3 / 指向突刺 = 单列轻；Pierce = 单列重；Slash = 多列轻；Sweep / Launch / 终结技 = 多列重）。

验收：`unity_recompile` 编译通过、Console 0 error；资产 YAML 已落盘（`columnMode` / `isHeavy` / `breakIcePushRows` 可直接读到）。

### P1b 已实现接口（供后续阶段调用）

| 接口 | 作用 |
|---|---|
| `enemy.Freeze(duration)` | 进入 `Frozen`（已冻结时只重置时长）；违反 `CanBeFrozen` 时返回 false |
| `enemy.BreakFreeze()` | 破冰解冻：取消被暂停的动作 → Idle（P4 在这里接破冰动画与双倍结算） |
| `enemy.ApplyIceHit(chancePerHit)` | 冰伤害入口：叠概率 → 前摇翻倍 → clamp → 按当前累计概率掷骰 |
| `enemy.IsFrozen` / `FreezeChance` / `IsInAttackWindup` / `CanBeFrozen` | 查询 |
| `enemy.PushSuspend/PopSuspend(SuspendSource)` | 挂起令牌（卡肉与冻结共用，互不干扰） |

**1011 手测步骤（Play Mode，长坂坡预览关 N1 同时有 1011）：**
1. 进战斗后暂停，在 Console 用 Locus 执行 `Freeze`（或等 P6 冰雾技能接好后用技能）。
2. 验：敌人定格、不推进、不出招；`Freeze` 日志打出原状态。
3. 冻结中再调一次 `Freeze(1.5f)`：只重置时长、不重播表现。
4. 等 4s：日志打出「自然解冻 → 原状态」，动画从暂停处继续。
5. 冻结中让它被打（卡肉）：HP 掉、跳字正常，卡肉结束后仍保持冻结（不会被卡肉解除）。

### P4 实测记录（2026-10-17，Play Mode，N1_ArtPreview_Battle）

| 用例 | 结果 |
|---|---|
| 冻结 1011 → 单列重攻击（Pierce）命中 | hp 200→160（**×2**）、状态转 `IceBreaking`、0.4s 后回正常状态机 |
| 冻结 1011 → 多列重攻击（Sweep）命中 | hp 160→155（**×1，不触发**）、仍为 `Frozen`、BUFF 保留 |
| 紧接着用 Pierce 命中 | hp 155→115（**×2**）、BUFF 被消费 |

当前破冰动画只有状态计时与 hitted，没有冰碎特效（`iceBreakEffectPrefab` 未配）。

### 已定：C2 纳入重攻击 + 推动优先级（2026-10-18）

- `Zhangfei_StabR2`（C2 指向突刺）：`isHeavy` → **1**；`pushBackRows` 保留 1。
- 规则：命中触发破冰时，该目标**跳过招式自带的普通击退**（`IceBreakRuntime.IsBrokenThisAttack` → `ApplyMovePushBack` 直接返回 false）；未触发则照常。
- 实测（N1，冻结 1011 吃 C2）：HP 200→130（**35×2=70**）、row 0→3、`brokenThisAttack=True`、`normalPushApplied=False`。

### P5 实测记录（2026-10-17，Play Mode）

| 用例 | 结果 |
|---|---|
| 冻结目标身后全空 → 破冰 | row 0 → **row 3**（当时 `breakIcePushRows=3`；已于 2026-10-18 改为 4，可顶到最后一排），HP -40 |
| 破冰动画结束 + 回位窗口 | 敌人回到原槽 row 0 |
| 目标身后有人（把 1021 移到同列 row1）→ 破冰 | 目标 **row 不动**（撞停）；目标 HP -55 = 40（双倍）+ 15（碰撞）；阻挡者 HP -15 |
| 多列重攻击（Sweep）命中冻结目标 | 不推动、不碰撞、不消费 BUFF（已有 P4 验证） |
| 推动补间 + 冰碎占位 | 命中瞬间位置仍在旧排，0.15s 内滑到新排（非瞬移）；`IceBreakFx` 已生成；3.5s 后回到原槽 |
| 冰块覆盖层 | 中心与敌人包围盒完全重合，尺寸 1.12×，sortingOrder 在敌人之上 |
| 破冰碎裂 | `IceBreakFx` 生成 **9 片**（同一张冰块贴图 3×3 切分） |

**已知边界（待观察）**：破冰发生在波次行军/入场期间时，回位交易可能被行军订单阻塞或被取消，敌人可能停在后排不归位。常规战斗（无行军在途）下已验证可正常回位。若实机出现“被推的敌人卡在后排”，下一步在 `RegisterBreakIceReturn` 里先释放敌人持有的非回位订单。

### 已修：破冰后被推的敌人不补齐回来（2026-10-18）

现象：冰冻目标被破冰击退后停在后排，不再回到战线。

两个原因：

1. 回位交易只在 `UpdateIceBreaking()`（破冰动画结束时）登记；一旦该状态被晕/击飞/取消打断，回调不会跑，交易永远不会建立。
2. `ApplyBreakIcePush` 调了 `ReleaseEnemyFromSchedulers`，它会 **取消回位交易并把这个敌人从波次行军链里摘出去**（严重时会打断整条补齐链）。

修法：

- 推完立即登记回位，到期时间 = `iceBreakAnimDuration + breakIceReturnDelay`（不再依赖动画结束回调）。
- 推前只取消它自己的在途移动（`IsRushMovementActive` → `CancelRushMoveOrder`），不再调 `ReleaseEnemyFromSchedulers`。

实测（Play Mode）：破冰瞬间 row 0→3、`tx=1`；t+2.0s row=2 开始行军；t+2.5s **row=0、`tx=0`**（回位完成）。

两条硬规则（后续不得破坏）：
1. `isHeavy` 与 `interruptsCavalryCharge` **并存不合并**：前者用于元素反应判定，后者用于打断骑兵 / 破盾。当前两者集合相同，但语义不同。
2. 枚举**只追加不插入**：`EnemyState` / `DamageType` / `HitFeedbackSource` / `ActiveSkillEffectType` 均可能被序列化，插入会漂移索引。

### 临时调试参数（验证完必须回收）

| 项 | 当前值 | 位置 | 回收时改回 |
|---|---|---|---|
| 冰雾池权重 | common 100000 / rare 100000 | `ActiveSkillPoolConfig.asset` | 只留 Rare，权重 100 |
| 每跳冻结概率 | 0.30 / 0.32 / 0.34 / 0.36 / 0.38 | `ActiveSkill_IceMist.asset` → `timedAoeLevels` | 0.10 附近 |
| 1011 概率上限 / 衰退 | max 0.9、delay 0.8s、rate 0.35/s | `Enemy_1011.prefab` | max 0.75、delay 0.6s、rate 0.5/s |
| 开局注入 | `DEBUG_IceTest_Bootstrap`（Battle 场景） | `ItemTestHelper` | 正式版删除该对象 |

这些值只为了“容易看到效果”，不是设计结论。

## 1. 现状勘察（证据表）

| 机制 | 现有落点 | 事实 | 能否复用 |
|---|---|---|---|
| 扇形喷流 | `Assets/Scripts/Effect/ShootFireEffect.cs` `PlaySweep` | `sweepAngle=35`、`sweepDuration=0.9`、`sweepHitInterval=0.2`（同一敌人可重复命中）、命中走 `TakeDamage(..., DamageType.Pierce, feedbackSource: Passive, Light)` | 复用结构，需要参数化"元素"与命中回调 |
| 扇形喷流调用方 | `TimedPassiveModule`（蛇形喷射）、`ItemEffectRunner` | 都是被动/道具；主动技能里目前只有 `FireAoe` 用 `Play`（直线），**没有主动用 `PlaySweep`** | 新增 `ActiveSkillEffectType.IceMist` 调 `PlaySweep` |
| 技能等级配置 | `TimedAoeLevelConfig`（`rangeRows` / `damage` / `columns` / 灼烧字段） | 字段足够承载冰雾；`design/active-skill-v2-rules.md` 明确允许复用通用数值结构 | 复用 + 加冰系字段 |
| 冻结所需"暂停" | `Enemy.ApplyHitStop` / `PauseAttackTweenForHitStop` / `ResumeAttackTweenAfterHitStop` | 用 `_animator.enabled=false` + `_attackTween.Pause()`，但标记是 `bool`（`_attackTweenPausedByHitStop`） | 改成令牌计数后复用 |
| 攻击动作载体 | `Enemy._attackTween`（DOTween Sequence）+ `attackTimer`（`-= Time.deltaTime`） | 冷却/前摇/收招都是 delta 计时或 tween | 冻结早退 + tween 暂停即可 |
| 前摇判定 | `Enemy.TryInterruptAttackByParry` | 条件 = `state==Attacking && isAttackAnimating && !isAttackDrawPhase` | 直接作为"前摇翻倍"口径 |
| 在途补齐移动 | `Enemy.UpdateMovement` | **不是 tween**：`moveProgress += dt/speed`，完成时 `rowIndex--` + `CompleteRushMoveOrder()` | 需要新增"立即完成"入口，且必须走完整完成分支 |
| 补齐订单 | `Enemy.HasRushMoveOrder` / `CompleteRushMoveOrder` / `OnRushMoveComplete`（owner + generation） | 漏回调会让整列停摆 | 冻结必须完成订单，不能挂起 |
| 绝对时间戳 | `Enemy._poiseRecoveryEndTime = Time.time + duration` | 全类唯一绝对时间 | 冻结期间顺延 |
| 敌人状态枚举 | `EnemyState`（Idle/Moving/Attacking/Stunned/Launched/Dead/QTEAttacking/GettingUp） | 无 Frozen | 新增 |
| 受击表现开关 | `Enemy.TakeDamage(triggerHitAnimation, suppressHitReactionAnimation, countsForCombo, feedbackSource, ...)` | 18 个参数，已经是"参数逐段透传"风格 | 冻结中受击直接复用开关 |
| 普通敌人自主前进 | `Enemy.StartMoving` | `if (!isBoss && !isRush) return;` — 普通敌人不会自己往前走，只吃补齐/行军订单 | 不可依赖 |
| 前排推进触发 | `ColumnManager.RequestLogicalLayoutReflow` / `TryBuildLogicalWaveMarchPlan` / `StartWaveMarch` | 只有**逻辑排无存活成员（死亡）**才重排；`RowBasedFillUp` / `CompactAllColumns` 已停用 | 破冰要用需新增入口 |
| 画面边界 | `Enemy.GetRowZ` | `(maxRow - row) * -spacing`，row 超过 `maxVisibleRows-1` 时 z 变正 → 落到画面外 | 推动排数必须 clamp |
| 回位延时 | `ColumnManager.pushReturnDelay = 0.35f` | 普通击退 0.35s 后就弹回原槽 | 破冰必须显式取消回位 |
| 攻击打断口径 | `Enemy.TakeDamage` 的 `canInterruptAttack` 分支 | 条件需要 `state == Attacking`，冻结态不会命中该分支 | 冻结中受击天然不取消暂停动作 |
| 击退 | `ColumnManager.ApplyPushWave` → `ExecutePush` | `CanPushColumn` 规则1/2：目标排有非命中者 → **整列不推**；推成功后 `RegisterPushReturn` 定时回原槽 | 破冰需要逐目标新通道 |
| 回位交易 | `ColumnManager.PushReturnTransaction{ originalColumn, originalRow, displacedRow, generation }` + `CancelPushReturn` | 已有交易会被继承为"更早的原槽" | "不回原槽"必须显式取消 |
| 撞停先例 | `Enemy/CavalryEnemy.cs` `CavalryPhase.Charging` | 逐排步进，前方有敌人 → `StrikeRoutine(blocker)`，停在它后一排 | 语义同构、代码不可复用，作为范式 |
| 伤害类型 | `DamageType`（Stab/Slash/Pierce/Sweep/Launch/Poise/Convergence） | `Convergence` 是"非攻击伤害"的先例 | 新增 `Ice`、`Collision` |
| 反馈来源 | `HitFeedbackSource`（BasicAttack/ActiveSkill/Passive/Item/Phantom/Dot/Ultimate/Displacement/SpikeTrap） | — | 新增 `Collision` |
| 敌人染色 | `Enemy` 的 `_tintPropBlock` + `SetPropertyBlock`（波次染色用） | 可在无美术时做冰蓝占位 | 冻结表现占位 |
| 元素 BUFF | `BuffManager` 只承载 `StatModifier`；`PlayerState.HasBuff(BuffType)` 是布尔枚举 | 都没有"一次性 + 作用域 + 可并存"语义 | 新建独立容器 |

## 2. 数据层改动

### 2.1 招式资产 `Assets/Scripts/Core/AttackSkillConfig.cs`

```csharp
public enum ColumnMode { SingleColumn, MultiColumn }

[Header("元素反应")]
public ColumnMode columnMode = ColumnMode.SingleColumn;
public bool isHeavy;                       // 与 interruptsCavalryCharge 并存，语义不同
public int breakIcePushRows = 4;           // 破冰推动的最大档排数
```

按代码实测填值（7 个资产）：

| 资产 | columnMode | isHeavy |
|---|---|---|
| `Zhangfei_Stab` / `Zhangfei_Jab1/2/3` / `Zhangfei_StabR2` | 单列 | 否 |
| `Zhangfei_Pierce` | 单列 | 是 |
| `Zhangfei_Slash` | 多列 | 否 |
| `Zhangfei_Sweep` / `Zhangfei_Launch` / `Zhangfei_LaunchFinisher` | 多列 | 是 |

### 2.2 敌人预制体 `Assets/Scripts/Enemy/Enemy.cs`

```csharp
[Header("冻结")]
public float freezeDuration = 4f;          // 4s：给玩家一次重攻击 + 收招的窗口
public float freezeChanceMax = 0.75f;
public float freezeChanceDecayDelay = 0.6f;
public float freezeChanceDecayPerSecond = 0.5f;
public float freezeWindupChanceMultiplier = 2f;
public bool freezePushImmune = false;
public float freezeResist = 0f;            // 减算到冻结概率；>= freezeChanceMax 等效免疫

[Header("破冰")]
public float iceBreakAnimDuration = 0.4f;   // 破冰动画时长（美术素材到位后再调）
public float breakIceReturnDelay = 1.2f;    // 破冰后的回位延时，从动画结束起算
public GameObject freezeOverlayPrefab;      // 冻结覆盖层（美术挂点，空则用 _Color 染色占位）
public GameObject iceBreakEffectPrefab;     // 破冰一次性特效（美术挂点）
```

### 2.3 全局战斗数值 `Assets/Scripts/Core/StageConfig.cs`

```csharp
[Tooltip("破冰推动被阻挡时的碰撞伤害固定值")]
public float collisionDamage = 15f;
```

### 2.4 技能配置 `Assets/Scripts/Core/UpgradeDefinition.cs` → `TimedAoeLevelConfig`

```csharp
[Header("冰系")]
[Tooltip("每次冰伤害叠加的冻结概率")]
public float iceFreezeChancePerHit;        // 火系留 0 即可
```

### 2.5 枚举扩展

- `DamageType` += `Ice`、`Collision`
- `HitFeedbackSource` += `Collision`
- `EnemyState` += `Frozen`、`IceBreaking`
- `ActiveSkillEffectType` += `IceMist`

### 2.6 元素 BUFF 容器（新建）

- `Assets/Scripts/Core/ElementalBuffDefinition.cs`（SO：元素类型 / 作用域 `ElementalScope` / 图标 / 描述模板）
- `Assets/Scripts/Core/PlayerElementalBuffs.cs`（运行时：可并存、无时限、`Grant` / `Consume` / `Query(scope, columnMode)`）
- 作用域枚举：`SingleColumnHeavy` / `AnyHeavy`

## 3. 系统层改动（6 个改动单元）

### 3.1 挂起令牌（P1 核心）

`Enemy.cs`：

```csharp
public enum SuspendSource { HitStop, Frozen }
private int _suspendCount;
public bool IsSuspended => _suspendCount > 0;
public void PushSuspend(SuspendSource src);  // 首次进入：_animator.enabled=false; _attackTween.Pause()
public void PopSuspend(SuspendSource src);   // 归零才恢复
```

要点：

- 卡肉改为 `PushSuspend(HitStop)` / 到点 `PopSuspend(HitStop)`，删除 `_attackTweenPausedByHitStop` 布尔。
- 冻结与卡肉可叠加；一方解除不会误恢复另一方暂停的资源。
- 同一来源重复 Push 必须在计数上配对，否则会永久定格。

### 3.2 冻结状态（P2）

```csharp
public void Freeze(float duration);   // 已冻结 → 只重置时长，不重复 Push
public bool IsFrozen => state == EnemyState.Frozen;
```

- `Update()` 在 `state == Frozen` 时：只跑冻结计时 + 冻结概率衰退 + 闪白/染色，其余（含 `switch` 状态机、命中帧结算、`rushMoveDelayTimer`、`_bossEngageTimer`）全部跳过。
- `TryStartRushMove` 的排除条件从 `Stunned`/`Launched` 扩到 `Frozen`（现有代码在 `Enemy.Update` 与 `Stun` 恢复路径都有调用点，需逐处检查）。
- 进入冻结前调用一次 `CompletePendingMovement()`（新增）：

```csharp
public void CompletePendingMovement();  // moveProgress=1 → 走 UpdateMovement 完成分支 → CompleteRushMoveOrder()
```

- 自然解冻：`PopSuspend(Frozen)` + 恢复 `_animator.enabled` + 继续计时。
- 破冰解冻：`PopSuspend(Frozen)` → 复用 Stun 那套"清理攻击状态"（`CancelAttack()` 同族逻辑）→ 播 hitted。
- `_poiseRecoveryEndTime` 在解冻时按冻结实际时长顺延。
- 排除：`Launched` / `GettingUp` / `QTEAttacking` / `isPhaseTransitioning` / `Dead` / `isBoss && bossState != InCombat`。
- `IceBreaking`（破冰动画期间）：仍算"冻结中"（位置静止、不可再次冻结、不叠概率），但 **hitted 与破冰动画同步播放**、可正常承受伤害；动画播完才 `PopSuspend(Frozen)` 并回正常状态机。

### 3.3 冻结概率（P2）

`Enemy.cs`：

```csharp
private float _freezeChance;        // 0..1
private float _lastIceHitTime;

public void ApplyIceHit(float chancePerHit);  // 叠加 → 前摇翻倍 → clamp → 重置衰退起点
public bool TryRollFreeze();                  // 掷骰，命中则 Freeze() 且 _freezeChance=0
```

- 衰退在 `Update()` 最前（不受冻结早退影响），用 `Time.deltaTime`。
- 冻结中：`ApplyIceHit` 不累加；`TryRollFreeze` 直接返回 false。
- 前摇翻倍口径复用 `TryInterruptAttackByParry` 的三条件判断（抽成 `IsInAttackWindup` 属性，两处共用同一实现，避免口径漂移）。
- 投掷时机：**每次冰伤害命中时**先叠加、再按当前累计概率立即投掷（不另设频率限制，命中本身就受 `sweepHitInterval = 0.2s` 约束）。
- 抗性：`effective = max(0, _freezeChance - freezeResist)`，`effective <= 0` 时直接不投掷。
- UI：**暂不可见**——不加状态条、不加描边指示，调试只用 Console 输出。

### 3.4 冰伤害入口（P2）

- 新增 `DamageType.Ice`；`Enemy.GetDamageMultiplier` 的 switch 走 default=1（需确认 default 分支）。
- 冰雾不再复用 `DamageType.Pierce`，改为 `Ice`，并在 `TakeDamage` 内对 `damageType == Ice` 的来源调用 `ApplyIceHit(chancePerHit)`——`chancePerHit` 通过透传参数带入（与 `interruptsCavalryCharge` 同风格），或由效果层在 `TakeDamage` 之前直接调用 `ApplyIceHit`（更简单，推荐后者，避免再加参数）。

### 3.5 破冰会话 + `TakeDamage` 前置块（P4）

- `Assets/Scripts/Player/AttackSystem.cs`：招式开始时 `IceBreakSession.Begin(cfg)`，动作结束时 `End()`（复用现有 `AttackReleaseTimeline` / 动作时长回调落点）。
- `IceBreakSession`：
  - `TryArmOnFirstHit()`：首次实际命中时校验 `isHeavy` + `columnMode` 与 BUFF 作用域 → `PlayerElementalBuffs.ConsumeMatching()` → `armed = true`。
  - `armed` 在本次攻击全程有效（满足多目标双倍）。
- `Enemy.TakeDamage` 前置块（在扣血之前）：

```
if (state == Frozen && session.armed)
{
    ① ColumnManager.ApplyBreakIcePush(...)   // 推动或撞停，返回是否被阻挡 + blocker
    ② if (blockedBy != null) 双方各结算 collisionDamage（DamageType.Collision / source Collision / countsForCombo:true / triggerHitAnimation:false）
    damage *= 2f;                            // ③
    ④ 播放破冰动画（IceBreaking）；动画结束时解除冻结并登记延迟回位（`breakIceReturnDelay` 从动画结束起算）
}
```

- 透传：`columnMode` / `isHeavy` 沿 `AttackSystem.Execute* → Effect.Create(...) → TakeDamage(...)` 追加参数，与现有 `interruptsCavalryCharge` / `hitReactionDirection` 同路径。
- 时间竞争：`UpdateFrozen` 里"本帧已被破冰"标记优先，跳过自然解冻。

### 3.6 破冰推动（P5）

`ColumnManager.cs` 新增：

```csharp
public struct BreakIcePushResult { public bool moved; public Enemy blockedBy; public int fromRow, toRow; }

public BreakIcePushResult ApplyBreakIcePush(Enemy frozen, int maxRows)
```

算法：

1. `if (frozen.isBoss || frozen.IsMountedCavalry || frozen.freezePushImmune) return 无位移`;
2. `maxRows = Mathf.Min(maxRows, GetMaxVisibleRows() - 1 - frozen.rowIndex)`：**必须 clamp**，否则 `Enemy.GetRowZ` 会让超出的排 z 变正、落到画面外；
3. `for (step = 1..maxRows)`：检查 `col.GetEnemyAtRow(row + step)`
   - 空 / 本次命中集合内的冻结目标 → 继续；
   - 非命中者 / 骑兵预留位 / BOSS → `blockedBy` = 该者，跳出；BOSS 与骑兵作为阻挡者**也吃碰撞伤害**（BOSS 未进战斗除外）；
4. 同列多个冻结目标由调用方**从后往前**逐个调用（row 大的先推），与 `ExecutePush` 的排序一致；被阻挡者停住后，其前面的目标不得越过它；
5. 推过至少 1 排才 `SetRowIndex` + `InsertEnemySorted`；
6. **照常登记** `RegisterPushReturn`（方案 A：不回原槽 → 延迟回位），原槽 = 命中前真实槽位、新槽 = 推动后槽位；`returnDueTime` 改为动画结束 + `breakIceReturnDelay`；已有旧交易走重注册路径；
7. 结束后 `frozen.RecheckAttackRange()`；撞停时 `blockedBy` 不移动、不进补齐。

### 3.7 冰雾主动技能（P6）

- `ActiveSkillRunner`：`case IceMist → ActivateIceMist`，取 `timedAoeLevels[level-1]` 的 `columns` / `rangeRows` / `damage` / `iceFreezeChancePerHit`。
- `ShootFireEffect`：新增 `public System.Action<Enemy> onSweepHit`，命中循环里回调；冰雾订阅 → `enemy.ApplyIceHit(chancePerHit)`。这样喷流类不掺元素逻辑。
- 视觉：复用现有 prefab，把 `fireSprites` 换成冰系精灵（美术后续给），染色偏冷。
- 资产：新建独立 `ActiveSkillDefinition`（`IceMist`），入 `ActiveSkillPoolConfig` 的 Rare/Legendary 池（`design/active-skill-v2-rules.md` 要求资产独立）。
- 玩家 BUFF：施放瞬间 `PlayerElementalBuffs.Grant(Ice, SingleColumnHeavy)`。
- HUD：`BuffDisplayPanel` / `BuffIcon` 显示一次性图标，消费后消失（现有组合 buff 图标体系可复用）。

## 4. 阶段计划

| 阶段 | 改动文件 | 验收步骤（Play Mode 手测） |
|---|---|---|
| **P0 字段与透传** | `AttackSkillConfig.cs`、`AttackSystem.cs`、`Attack/{StabSweepEffect,SweepEffect,AttackWave}.cs`、`Enemy.TakeDamage` 签名、7 个招式资产 | 打一遍全部招式；伤害/击退/破盾/连击/能量与改动前一致；Console 无异常 |
| **P1 挂起令牌** | `Enemy.cs`（`PushSuspend`/`PopSuspend`/`CompletePendingMovement`）、卡肉路径 | 冻结 **1011**：动画定格、攻击冷却不走、补齐中冻结先落位再定格；解冻后从暂停处继续；冻结中被打（卡肉）两者不互相解除 |
| **P2 冻结状态与概率** | `Enemy.cs`、`EnemyHealthBar`/`BossHealthUI`、预制体参数（**只配 1011**） | 连续冰伤害让概率上升、停手后下降；前摇中喷冰上升更快；概率满/掷中时进入冻结；冻结中不叠概率；重复冻结只重置时长；BOSS C 帧被冻 → 解冻后**续播到同一次 C 帧** |
| **P3 元素 BUFF 容器** | 新建 SO + 运行时 + HUD | 冰雾后图标出现；空挥重攻击图标不消失；打一次合规重攻击后图标消失 |
| **P4 破冰（不含推动）** | `IceBreakSession`、`TakeDamage` 前置块、`AttackSystem` 会话开关 | 单列 Pierce 打冻结目标：双倍跳字 + 解冻 + 播 hitted + BUFF 消失；多列 Sweep 打冻结目标：不触发不消耗、冻结保留；空挥不消耗 |
| **P5 推动与碰撞** | `ColumnManager.ApplyBreakIcePush`、`DamageType.Collision`、`HitFeedbackSource.Collision` | 冻结目标身后有人 → 停在阻挡者前一排、双方各扣一个固定值、连击数增加；身后全空 → 推满档且**不立即回原槽**；BOSS/骑乘骑兵/`freezePushImmune` 不被推 |
| **P6 冰雾技能** | `ActiveSkillRunner`、`ShootFireEffect`、新建 SO、池资产、prefab | 点技能 → 扇扫命中 → 概率叠加 → 冻结 + 玩家获得 BUFF；CD 与遮罩正常；换冰系贴图后无残留火焰色 |

## 5. 测试矩阵（对应 `design/ice-element-link.md` 验收）

| # | 用例 | 期望 |
|---|---|---|
| 1 | 冰雾命中 | 概率上升；命中时前摇中则上升翻倍 |
| 2 | 停手 0.6s 后 | 概率按秒下降、不低于 0 |
| 3 | 掷中概率 | 敌人冻结，概率归零 |
| 4 | 冻结中再受冰伤害 | 概率不涨、不重复冻结 |
| 5 | 冻结中普通攻击 | 不解冻、无 hitted；形变/卡肉/跳字正常；**被暂停的动作不被打断** |
| 6 | 单列重攻击命中冻结目标 | 推动 → 被阻挡则碰撞（碰撞跳字）→ 双倍（命中跳字）→ 破冰动画 + hitted → 动画完才解冻 → BUFF 消耗 |
| 6b | 破冰动画期间 | 位置静止；破冰动画与 hitted 同步播放；不能再被冻结、不叠概率；跳字正常 |
| 6c | 破冰动画结束 | 解除冻结、取消原动作、回正常状态机；随后 `breakIceReturnDelay` 到点回原槽 |
| 7 | 多列重攻击（单列版 BUFF） | 不触发、不消耗、冻结保留 |
| 8 | 空挥 | 不消耗 |
| 9 | 自然解冻 | 从暂停处续播（含前摇、冷却、收招） |
| 10 | 重复冻结 | 时长重置、进度保留 |
| 11 | 多次破冰 | 敌人被推远且不立即回槽；row 不超过 `maxVisibleRows-1`；不触发超范围攻击 |
| 11b | 破冰撞到 BOSS / 骑乘骑兵 / 预留位 | 双方各吃一次碰撞伤害（BOSS 未进战斗时它自己不吃） |
| 11c | 碰撞伤害与连击 | 连击数与伤害统计增加；大招能量与被动计数不变 |
| 11d | 冻结中被打死 | 直接播放死亡，无受击 |
| 11e | 冻结中 BOSS 跨阶段阈值 | 冰冻被解除并进入阶段演出 |
| 11f | 冻结中骑兵处于冲锋 | 解冻后进入返航，不续播冲锋 |
| 12 | 冻结/概率跨战斗 | 战斗结束、节点切换后清空 |
| 13 | 1021 / 109 / BOSS | 按规则文档第 4.3 / 5.4 节 |
| 14 | 概率抗性 | `freezeResist` 减算后为 0 时不投掷（等效免疫） |

## 6. 风险登记

| 风险 | 触发 | 规避 |
|---|---|---|
| 冻结与卡肉互抢 animator/tween | 冻结中被打 | P1 令牌计数，删除布尔标记 |
| 冻结挂住补齐订单 → 整列停摆 | 补齐移动中冻结 | `CompletePendingMovement()` 走完整完成分支（含 `CompleteRushMoveOrder`） |
| 破冰后被"偷偷"送回原槽 | 目标身上有未完成击退交易 | `CancelPushReturn` + 不注册新交易 |
| 概率永涨不冻或必冻 | 参数不当 | `freezeChanceMax` clamp + 掷中清零 + 冻结中不叠加 |
| 敌人被推远至不可攻击排 | 不回原槽 + 敌人不自主前进 | 推动上限 clamp 到 `maxVisibleRows-1`；加战线安全阀（无可攻击排存活敌人时强制重排）；`RecheckAttackRange()` |
| BOSS 霸体教学被洗掉 | 霸体期被冻 | 允许冻结但"暂停非取消"，解冻续播同一次 C 帧，P2 单独验 |
| QTE 动画被打断（历史 BUG） | QTE 期间被冻结 | 冻结入口显式排除 `QTEAttacking` 与 `isPhaseTransitioning` |

## 7. 待决项

1. `collisionDamage` 走 `StageConfig`（已定：它是"两个敌人相撞"的全局规则）。
2. 破冰后的回位方式：**已定方案 A**（延迟回位，`breakIceReturnDelay`，从破冰动画结束起算），不再需要战线安全阀。
3. 冰雾入池稀有度与是否允许 BOSS 掉落（V2 规则下 BOSS 只掉 Rare/Legendary 主动技能）。
4. 数值全部走 Inspector：冰雾伤害/列/排、`iceFreezeChancePerHit`、衰退、上限、冻结时长、`breakIcePushRows`、`collisionDamage`。
