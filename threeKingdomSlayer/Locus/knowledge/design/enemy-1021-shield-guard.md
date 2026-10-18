---
id: kd_06bec1a4-aa64-4b6c-a746-63f05b4ffbaf
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 盾兵敌人（1021）机制设计：盾侧格挡 / 拆盾 / 胆怯

## 1. 文档定位与当前状态

本文件是 1021 盾兵的权威记录（已确认规则 + 代码资产落点 + 实现约束 + 验证记录）。同级实验品 102 保留不动，二者的关系见 §10.1。

- **已确认并实现（最小可玩版）**：四值方向分类（盾侧 / 反侧 / 正面 / 未声明）、固定数值减伤（下限 1）、破盾 → 掉盾动画 → 胆怯（增伤 1.5×、不再攻击）、对象池复位、波次接入。Play Mode 实测见 §11。
- **已确认、尚未实现**：跟手精确攻击（连段蓄力派生戳 `Zhangfei_StabR2`）与命中部位（盾区）判定；`breaksGuard` / `isPreciseAim` 独立资产字段（见 §4.4）。
- **当前美术进度（2026-10-14）**：正式Idle四帧、Walk六帧、Attack十二帧、ShieldGuardHit六帧、HitFlash六帧已接入第一关并获用户游戏验收；HitRight六帧（用户自抠）已接入、待游戏验收。破盾（`ShieldBreak`）与独立盾掉落接管已于 2026-10-15 实现并接入，待用户游戏验收；死亡、击飞、无盾胆怯家族仍使用 102 占位（见§7、§12）。动画验收不代表下文机制未覆盖项已完成；交接与剩余清单见 `plan/enemy1021-art-production-handoff.md` §0.2。

### 1.1 代码与资产落点

| 位置 | 角色 |
|---|---|
| `Assets/Scripts/Enemy/ShieldGuardEnemy.cs` | 1021 的盾牌判定组件：侧判定、固定减伤、破盾流程、格挡表现、池复位（新增） |
| `Assets/Scripts/Enemy/Enemy.cs` | `EnemyFormState.ShieldBroken`、`_shieldGuard` 引用、`TakeDamage` 盾判定钩子、两条破盾/胆怯入口、破盾期抑制受击动画、攻击守卫、`ResetEnemy` 复位 |
| `Assets/Animations/Enemy_1021.controller` | 复制自 `Enemy_102.controller`，新增 `ShieldBreak`、`ShieldGuardHit`（新增） |
| `Assets/Resources/EnemyPrefabs/Enemy_1021.prefab` | 复制自 `Enemy_102.prefab`，`enemyId=1021`、`enemyName=盾卫`、`maxHealth=40`、旧百分比减伤置 0、关闭共享血量、Animator→1021、挂 `ShieldGuardEnemy`（新增） |
| `Assets/RouteData/Stage01/N1_ArtPreview_Battle.asset` | 当前路线节点 A 的实际战斗配置；第一波第一排 `[0,0,1011,0,0]` → `[1021,0,1011,0,0]`（实验改动，可一键回退） |
| `Assets/Resources/EnemyPrefabs/Enemy_102.prefab`、`Assets/Animations/Enemy_102.controller`、`Enemy_102_Coward*.anim`、`Assets/Sprites/Enemy/Enemy2/` | 102 实验品资产，1021 复用其中的美术与 clip；**102 本身未做任何修改** |

`EnemyPool` 按文件名自动注册（`Enemy_{id}.prefab`），新增预制体无需改代码。

### 1.2 状态口径

- **已验证** = 在本机 Play Mode 的真实路线节点（`Stage01_RouteV2` → 节点 A → `N1_ArtPreview_Battle`）中实测到对应数值/形态/动画状态，见 §11。
- **待验收** = 机制时序已通，但手感、数值与视觉需用户实机确认，见 §12。

## 2. 规则依据

- `design/attack-interrupt-system.md`：三级打断体系；本敌人的"重攻击"沿用同一套声明式思路。
- `design/combo-move-state-machine.md`、`memory/unity-project-understanding/input-gesture-system.md`：连段表与手势 → 招式映射（决定玩家能施加哪种"来源方向"）。
- `plan/enemy1011-animation-deployment-handoff.md` §4：`HitReactionDirection` 的既有语义（`Left` = 力来自画面左 = 被从左侧打），本敌人直接沿用该词汇。
- `design/pixel-character-and-enemy-animation-spec.md`、`design/visual-yoffset-system.md`：正面像素角色、动作语义、锚点与脚底补偿。
- `design/percolumn-fillup-rules.md`、`design/immutable-constraints.md`：胆怯仍然前进所涉及的补齐/位移边界。
- `memory/enemy-wave-tint-pooling.md`：对象池复用残留的教训，对应本敌人的"池复位"要求。

盾牌判定是**伤害结算前的敌人侧规则**，与玩家输入窗口、手势识别、冷却无关；不要为了它改动 `InputManager` 的分支或 `AttackSystem` 的伤害倍率链路。

## 3. 用户确认的规则（权威）

### 3.1 盾与方向词汇

- 敌人**右手持盾** → 盾在**画面左侧** → `Left`（力来自画面左）= **盾侧**。
- 方向沿用工程既有 `HitReactionDirection` 的三值含义（`Left` / `Right` / `Front`），并**新增第四类"未声明"**（见 §4.1）。
- "左侧 / 右侧 / 正面"一律指**画面左右与正面**，不引入敌人自身坐标系的镜像。

### 3.2 减伤（盾侧与正面）

1. 来源侧为**盾侧（Left）**或**正面（Front）**的普通攻击 → **格挡**：`实际伤害 = max(minDamage, 伤害 − 固定减伤)`。
2. **减伤是固定数值**，不是百分比。
3. **DoT 不参与**盾的任何判定（走既有 `ignoreDamageModifiers`，整段跳过）。
4. **未声明方向**的伤害（`None` / `Random`）**不进盾判定**，按全额结算。
   - 注意：`未声明 ≠ 正面`。被动烈焰/箭雨/落雷/道具/DoT 与未填方向的招式都落在"未声明"，若把它们归入"正面"，这些伤害会突然被格挡，与规则 4 冲突。实现上必须是**四值分类**，不允许把 `None` 映射为 `Front`。
5. 最少造成 `minDamage`（默认 1），避免低伤多段攻击被完全免疫。

### 3.3 破盾

1. **反侧（Right）+ 重攻击** → 打掉护盾。
2. **盾侧/正面的重攻击不破盾**，只按格挡结算。
3. **精确攻击**（见 §4.4）命中**盾区** → 破盾；命中身体 → 全额。
4. 破盾是**一次性**的，破盾后不再触发（不恢复）。
5. 未来新增的破盾手段一律走**数据声明**（§4.2），不维护攻击类型白名单。

### 3.4 掉盾时序

1. 破盾**瞬间**减伤立即失效（同一次命中即按全额结算）。
2. 随后进入破盾表现期（`EnemyFormState.ShieldBroken`）播放掉盾动画；表现期内**普通受击不打断**，**死亡与击飞可打断**。
3. 掉盾动画**播完**才进入胆怯（`Coward`）—— 即"增伤"比"减伤失效"晚一个动画时长（当前 0.46s）。

### 3.5 胆怯

- 进入胆怯后：受到伤害 **×1.5**（与击飞倍率取较大者，不叠乘）、**不再攻击**、动画走无盾的胆怯家族。
- 胆怯**仍然前进**（沿用 102 现状，`UpdateMovement` 不判形态），只是不还手。
- 胆怯是单向的，直到死亡或回池复位。

### 3.6 与 102 的关系

- 1021 与 102 **并存**，102 是实验品，**不修改、不移除**。
- 102 的胆怯入口是"被挑飞"（`EnterCowardFormFromPlayerLaunch`，硬编码 `enemyId == 102`）；1021 的入口是"破盾"。两条入口在代码里是分开的，互不影响。

## 4. 术语与参数

### 4.1 四值方向分类

| 分类 | 触发来源 | 结果 |
|---|---|---|
| 盾侧 ShieldSide | `HitReactionDirection.Left`（力来自画面左） | 格挡（重攻击也不破盾） |
| 反侧 OffSide | `HitReactionDirection.Right` | 普通全额；重攻击破盾 |
| 正面 Front | `HitReactionDirection.Front` | 格挡 |
| 未声明 Unspecified | `None` / `Random`（含 DoT 与 `ignoreDamageModifiers`） | 不进盾判定，全额 |

**判定必须读"原始声明值"**：`Enemy.ResolveHitReactionDirection()` 会把未声明招式**随机**成 Front/Left/Right（那是受击动画用）；若拿解析后的值做玩法判定，未声明招式会变成随机格挡。

现役玩家招式方向实况（实测资产数据）：

| 招式 | 类型 | 声明方向 | 备注 |
|---|---|---|---|
| `Zhangfei_Jab1`（招式表 Tap 根节点） | Stab | Front | `--Hold,minCharge≥1-->` `Zhangfei_StabR2` |
| `Zhangfei_Jab2` | Stab | Left（盾侧） | `--Tap-->` Jab3 |
| `Zhangfei_Jab3` | Stab | Right（反侧） | `--SwipeVertical,minCharge≥1-->` `LaunchFinisher`（AoE，排除） |
| `Zhangfei_Slash` | Slash | **几何覆盖**：左→右划 = Left，右→左划 = Right | `AttackSystem.ExecuteSlash` |
| `Zhangfei_StabR2` | Stab, rows=2 | 未声明（精确攻击载体，玩法只看命中部位） | 串尾 |
| `Zhangfei_Stab / Pierce / Sweep / Launch / LaunchFinisher / Parry / Jab*` 其余 | — | 未声明（Random） | 需要参与盾判定时在资产填方向即可，无需改代码 |

由此得到本敌人的**读招循环**：连段第 1/2 段（正面/盾侧）打不动 → 第 3 段（反侧）才是全额；想更快处决就用**反侧重攻击**或**精确攻击拆盾** → 胆怯增伤。

### 4.2 "重攻击"声明与扩展协议

当前实现**借用**既有的 `AttackSkillConfig.interruptsCavalryCharge`（其 Tooltip 语义本就是"把某种攻击视为蓄力/重攻击就在资产上声明"），该标记已由 `AttackSystem` 透传到 `Enemy.TakeDamage(..., interruptsCavalryCharge)`：

| 攻击资产 | 勾选 | 说明 |
|---|---|---|
| `Zhangfei_Pierce` / `Zhangfei_Sweep` / `Zhangfei_Launch` | 是 | 蓄力专属动作（但方向为"未声明"，目前不构成破盾路径） |
| `Zhangfei_LaunchFinisher` | 是 | 连段 AoE 终结技 |
| `Zhangfei_Slash` | 否 | **普通与蓄力共用**：勾选为 0，但 `AttackSystem.InterruptsCavalryCharge(cfg)` 会把"本次输入为蓄力"也算作重攻击 |
| `Zhangfei_Stab` / `Zhangfei_Parry` / `Jab1-3` / `StabR2` | 否 | 普通攻击 |

**当前唯一可达的破盾路径 = 右→左划的蓄力斩击**（`ExecuteSlash` 同时传几何方向 Right 与重攻击标记）。

扩展协议：

1. 目标形态是新增独立的 `AttackSkillConfig.breaksGuard`（与骑兵语义解耦）；在字段落地前，新招式先按上表"勾 `interruptsCavalryCharge` + 方向声明"组合。
2. 新增攻击若走新的输入分支，只需在该分支传入 `chargedAttack: true`，不需要改盾兵代码。
3. **不要**在 `ShieldGuardEnemy` / `AttackSystem` 里维护攻击类型白名单。

### 4.3 Inspector 参数（`Enemy_1021.prefab`）

`ShieldGuardEnemy` 组件：

| 字段 | 默认 | 说明 |
|---|---|---|
| `flatDamageReduction` | 10 | 格挡时扣减的固定伤害 |
| `minDamage` | 1 | 格挡后的最低伤害 |
| `heavyFromShieldSideBreaks` | false | 盾侧/正面的重攻击是否也破盾（默认按规则 3.3 不破） |
| `breakAnimationDuration` | 0.46 | 掉盾动画时长（与复用的 `Enemy_102_CowardTransition` stopTime 对齐） |
| `breakAnimatorState` | `ShieldBreak` | 掉盾动画状态名 |
| `cowardDamageTakenMultiplier` | 1.5 | 胆怯形态受伤倍率（与击飞倍率取较大者） |
| `blockDamageNumberColor` | (0.62, 0.70, 0.86) | 格挡命中的跳字颜色（可读性） |
| `blockAnimatorState` | `ShieldGuardHit` | 格挡专属状态名（缺失时回落 `HitFlash`） |
| `logShieldEvents` | true | 破盾/胆怯日志（调试用） |

`Enemy` 组件上须保持：`normalFormDamageReductionPercent = 0`（否则与固定减伤叠加）、`shareHealthWithAdjacent = false`（见 §10.4）。

### 4.4 盾区与跟手（设计已定、尚未实现）

- 载体：连段 `Jab1 --Hold,minCharge≥1--> StabR2`（rows=2、dmg 35、串尾）。
- 行为：抬手时以手指位置为瞄准点，枪尖与命中点跟手横移（列内），命中部位决定结果（盾区 → 破盾；身体 → 全额）。
- 待接线点（全部已定位）：
  1. `InputManager` Hold 释放分支现在只解析列（`GetStabColumnFromScreenPosition(releasePos)`）→ 需把抬手点作为瞄准负载传下去；
  2. `GestureInput`（`Assets/Scripts/MoveSystem/MoveGesture.cs`）需增加瞄准字段；
  3. 逐段透传 `PlayerMoveStateMachine` → `AttackSystem.TryExecuteAttack` → `ExecuteStab` → `StabSweepEffect`；
  4. `ExecuteStab` 现在落点只由列偏航角（`GetStabRayYaw`）+ 命中排距离决定。
- 约束：命中**集合**仍按列取（保证跟手不会打空），横向偏移只驱动**视觉枪尖与 `impactPosition`**（两者必须同源）；该招式上关闭 `stabVisualTargetRandomRadius`（纯视觉随机）；偏移上限 ±半列宽；部位判定只作用于枪尖对准的那个目标。
- 盾区参数（`shieldZoneMinX / shieldZoneMaxX`，相对敌人局部 X）与 Gizmo 可视化一并实现，保证"美术画的盾"与"判定的盾区"一致。

## 5. 形态状态机与事件

```
Normal（持盾：固定减伤生效）
  └破盾→ ShieldBroken（减伤已失效、增伤未生效、播放 ShieldBreak）
            └breakAnimationDuration 后→ Coward（增伤生效、不再攻击；CowardIdle）
```

| 形态 | 进入条件 | 行为 | 退出 |
|---|---|---|---|
| `Normal` | `Initialize`、回池复位 | 盾判定生效；正常攻击/移动/受击 | 被破盾 → `ShieldBroken`；死亡 |
| `ShieldBroken` | `ShieldGuardEnemy.OnShieldBroken()`（反侧重攻击 / 精确命中盾区） | 减伤失效；`CancelAttack()` 清理攻击动作；播放 `ShieldBreak`；普通受击不打断（不播受击动画，但保留闪白/掉血/跳字）；不再开始新攻击 | 计时结束 → `Coward`；死亡 → `Dead`；被击飞 → 立即转 `Coward` 并走击飞 |
| `Coward` | 掉盾动画播完（`CompleteShieldBrokenToCoward`） | 受伤 ×1.5；不再攻击（仍前进）；动画走无盾胆怯家族 | 死亡；回池复位 |
| `Dead` | HP 归零 | 既有死亡流程（当前用带盾的 `Dead` 帧，属美术待修） | 对象池 |

事件与外部入口：

- `Enemy.EnterShieldBrokenForm()`、`Enemy.CompleteShieldBrokenToCoward()`、`Enemy.EnterCowardFormFromShieldBreak()`（供未来的破盾来源直接进胆怯）。
- `ShieldGuardEnemy.ProcessHit(...)` → `HitResult { finalDamage, blocked, broke }`：`Enemy.TakeDamage` 在伤害结算前调用，结算后按 `blocked` / `broke` 触发表现。
- `ShieldGuardEnemy.ResetShieldGuard()`：`Enemy.Initialize` 与 `Enemy.ResetEnemy` 调用，中断未完成的破盾计时。

## 6. 实现要点与约束

### 6.1 唯一的伤害拦截点

盾判定只发生在 `Enemy.TakeDamage` 的"应用弱点倍率"之后、"扣血"之前（`multiplier` 计算完、`finalDamage` 确定时），并且**跳过 `ignoreDamageModifiers`**。不要在 `GetDamageMultiplier` 里做固定减伤：它按倍率返回，无法表达减法，且被转阶段伤害预测与 `SharedHealthGroup` 复用。

### 6.2 命中卡肉会让受击动画晚几帧

`HitFeedbackManager.Trigger` → `Enemy.ApplyHitStop` 会临时 `_animator.enabled = false`（Standard 0.09s / Light 0.045s），prefab 又设 `keepAnimatorStateOnDisable = 0`，因此**受击/格挡/破盾动画要在卡肉结束后才出现在 Animator 状态里**。这是既有机制（101 的受击动画同理）；写验证脚本时不能用"命中后 1 帧"读状态，否则会误判为"动画没播"。格挡走 `Light`，所以格挡动画比普通受击更早出现。

### 6.3 两条胆怯入口必须隔离

102 的入口（`EnterCowardFormFromPlayerLaunch`，保留原有 `enemyId != 102` 判定）与 1021 的入口（破盾流程）分开，公共表现体抽成 `EnterCowardFormVisual()`。102 的判定与行为不得因 1021 而改变。

### 6.4 破盾期抑制受击动画的落点

`allowHitReactionAnimation` 增加 `formState != EnemyFormState.ShieldBroken`：不播受击动画，但保留闪白、掉血、跳字。格挡表现（`PlayBlockVisual`）在受击动画分支之后播放，因此能覆盖普通受击动画。

### 6.5 攻击守卫

`StartAttacking()` 与 `UpdateAttack()` 增加 `formState == ShieldBroken` 守卫；`Coward` 由既有 `IsCoward` 守卫覆盖。**已知遗留**：非 Boss 的攻击循环语义下，破盾后 `state` 会停在 `Attacking`（不再推进、也不再攻击），未被强制改成 `Idle`；若未来有逻辑读 `state == Attacking`，需要单独处理。

### 6.6 池复位

`Initialize`/`ResetEnemy` 会 `StoStop` 破盾计时并把 `formState` 复位为 `Normal`（由 `Enemy` 既有复位与 `ResetShieldGuard()` 共同完成）。盾的**视觉残留**（`ShieldVisual` 子物体被 clip 写过的 sprite/位置）目前没有复位代码——见 §12，属美术重做时一并解决。

### 6.7 与补齐/位移无关

盾判定只改伤害数值与表现，不改排位、不占额外槽位、不参与补齐调度；胆怯仍按普通敌人前进。

## 7. 动画资产与播放契约

> 动画制作输入（逐状态 × 动画矩阵、素材清单与规格、Animator 待补状态、时间对齐规则、盾的表现方案、验收清单）见 `design/enemy-1021-animation.md`；本节只记录代码侧的播放契约。

1021已把 Idle/Walk/Attack/ShieldGuardHit/HitFlash/HitRight/ShieldBreak 换成正式 clip，`CowardIdle` 暂用静态兜底 clip；其余状态仍复用 102 的 clip（`Assets/Animations/Enemy_102_*.anim`）与精灵（`Assets/Sprites/Enemy/Enemy2/`）。

`Enemy_1021.controller`（复制自 102）已新增 `ShieldBreak`、`ShieldGuardHit`、`HitRight` 三个状态（其中 `ShieldBreak` 与 `HitRight` 无入边，只由代码 `Play`）：

| 状态 | motion | 出边 | 说明 |
|---|---|---|---|
| `ShieldBreak` | `Enemy_1021_ShieldBreak.anim` | 无（0.46s 结束事件触发进入胆怯） | 掉盾表现（50fps、0.46s 非循环）；0.12s 事件生成独立盾、0.46s 事件收尾；`ShieldVisual` 0 秒 null 键。已接入、待游戏验收 |
| `ShieldGuardHit` | `Enemy_1021_ShieldGuardHit.anim` | exit time 1 → `Idle` | 格挡表现（正式6帧、0.30s非循环，已获用户游戏验收：盾臂局部承力、盾牌小幅震荡、上身相对骨盆向画面右侧轻微倾斜；不后仰、无特效） |

状态名契约（代码按名 `Play`，缺失时回落）：

```csharp
// Enemy.cs
PlayIdleVisual()  => _animator.Play(IsCoward ? "CowardIdle" : "Idle");
PlayHitVisual(d)  => IsCoward ? "CowardHit" : (Left/Right => "HitLeft"/"HitRight"，缺失回落 "HitFlash")
PlayLaunchVisual()=> IsCoward ? "CowardLaunched" : "Launched_Rise";
PlayDeadVisual()  => IsCoward ? "CowardDead" : "Dead";
// Enemy.EnterShieldBrokenForm()
_animator.Play("ShieldBreak")   // 仅当 Base Layer.ShieldBreak 存在
// ShieldGuardEnemy.PlayBlockVisual()
_animator.Play("ShieldGuardHit") // 缺失回落 "HitFlash"
```

- 1021 的 controller **没有** `HitLeft` / `HitRight`（102 只有 `HitFlash`）→ 方向受击表现目前退化为正面闪白。正式版需要补 `HitRight`（反侧 = 全额，必须看得出没被挡）；`HitLeft` **不做**——当前规则下盾侧攻击必然被格挡，该状态在持盾形态不可达（详见 `design/enemy-1021-animation.md` §7）。
- 102 的 controller **没有** `Launched_Getup` → `HasGetupAnimatorState()` 为 false，落地后直接回 `Idle`/`CowardIdle`，没有起身表现；`PlayGetupVisual()` 里的 `"CowardGetup"` 在整个工程不存在（靠前置守卫未触发）。
- 破盾表现期被击飞时：会用带盾的 `Launched_Rise` 帧（美术错配），逻辑上已立即转 `Coward`。

## 8. 表现与可读性（机制的一部分）

- **格挡三件套**：专属跳字颜色（`blockDamageNumberColor`）、弱化卡肉（`HitFeedbackStrength.Light`）、`ShieldGuardHit` 状态。缺了跳字颜色，玩家会把"减伤"读成"没打中"。
- **破盾**：已改接正式 `Enemy_1021_ShieldBreak.anim`（50fps/0.46s）。盾在角色本体帧内直到脱手帧，0.12s 事件后由引擎驱动独立世界对象（`Enemy_1021_ShieldDrop.prefab`）接手下落；旧 `ShieldVisual` 分离层已停用并在池复位清空。已接入、待用户游戏验收。
- **定妆形象（单帧已验收，未部署）**：用户已确认 `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3.png` 的 Idle 形象通过。人物直接以 1011 为基底改剑为盾，保留其体型、头盔、甲胄与色彩；盾在画面左侧、胸腹前方，持盾手/前臂藏在盾后，画面右侧左手保持空垂。实际生成参考为 1011 Idle1 + 用户盾牌局部材质/构造图，后续以 v3 成图为身份锚点；Idle 动画、透明处理、对齐和 Unity 部署尚未开始，详见 `plan/enemy1021-art-production-handoff.md`。本轮未更改机制与数值。
- **胆怯**：无盾、缩身的胆怯家族；增伤可由跳字颜色/大小体现。
- **教学顺序**：正面（Jab1）与盾侧（Jab2）都打不动 → 用 Jab3 / 右→左划从反侧打全额 → 用反侧重攻击或精确攻击拆盾 → 胆怯处决。

## 9. 数值（当前默认，可调）

| 项 | 1021 | 对照 102 |
|---|---|---|
| `maxHealth` | 40 | 30（含 50% 常态减伤） |
| 减伤方式 | 固定 10（最低 1），仅盾侧/正面 | 百分比 50%，全向 |
| `attackDamage` / `attackSpeed` / `moveSpeed` | 10 / 0.2 / 0.3 | 同 |
| 胆怯增伤 | ×1.5（不叠乘击飞 1.5×） | 仅"失去减伤" |
| `coinReward` / `expReward` | 沿用 102 | 10 / 50 |

低伤招式（斩击 8、横扫 5）打在盾侧/正面只能造成 1 点 —— 这是有意的"从这侧打不动"教学；若体感拖沓，优先调 `minDamage` 与 `flatDamageReduction`。

## 10. 与其他系统的交互

### 10.1 与 102（实验品）

102 一行未改：仍是"常态 50% 减伤 + 被挑飞进胆怯 + 共享血量 + 仅 HitFlash + 无起身"。两者可在同波次并存（当前实验波次就是 1021 与 1011 并存）。

### 10.2 打断 / 霸体

1021 不设霸体，普通攻击可打断其攻击动作（与 102 一致）。破盾表现期由形态守卫拦截新攻击，不依赖霸体。

### 10.3 击飞

破盾表现期被击飞 → 立即完成破盾（`Coward`）并走击飞流程；落地后按胆怯表现（无盾）。盾的掉落表现（复用 clip 内曲线）不受影响。

### 10.4 共享血量

1021 **关闭** `shareHealthWithAdjacent`。原因：`SharedHealthGroup.TakeDamage` 用"被命中成员"的倍率算整池伤害，而盾/破盾是**实例级**状态——共享会带来"减伤泄漏到全组、只有被命中的那只破盾"的不一致。若未来要开，需把盾/破盾提升为组级。

### 10.5 DoT 与无方向伤害源

DoT（`ignoreDamageModifiers`）、被动烈焰/箭雨/旋风/陷阱/落雷/道具/连锁弹射、经 `AttackWave` 的穿刺与挑飞、以及未填方向的招式，一律**不进盾判定**（全额）。与 102 的旧"全向 50% 减伤"相比手感差别明显，配置波次时要预期这一点。

### 10.6 未覆盖的系统

QTE / 招架对盾兵的表现、Boss 混编、同列多只 1021、移动端触屏瞄准（跟手未实现）均未覆盖。

## 11. 本轮已验证记录（Play Mode，路线节点 A / `N1_ArtPreview_Battle`）

链路：`Battle.scene` → `Stage01_RouteV2` → 节点 A 战斗配置 `N1_ArtPreview_Battle` → 第一排 `[1021, 0, 1011, 0, 0]`。

| 用例 | 期望 | 实测 |
|---|---|---|
| 正面（`Front`）普通 20 | 格挡 → 10 | 10 ✓ |
| 盾侧（`Left`）普通 20 | 格挡 → 10 | 10 ✓ |
| 反侧（`Right`）普通 20 | 全额 20 | 20 ✓ |
| 未声明（`None`）20 | 全额 20 | 20 ✓ |
| `ignoreDamageModifiers`（DoT 语义）20 | 全额 20 | 20 ✓ |
| 盾侧 + 重攻击 | 格挡 10，**不破盾** | 10、`form=Normal` ✓ |
| **真实招式** `Zhangfei_Jab2`（Left，26 伤） | 26 → 16 | 16 ✓（资产 → `ExecuteStab` → `StabSweepEffect` → `TakeDamage` 全链路） |
| 反侧 + 重攻击 | 破盾 + 全额 20 | 20、`form=ShieldBroken` ✓ |
| 破盾动画期间普通受击 | 全额且不打断掉盾动画 | 20、动画保持 `ShieldBreak` ✓ |
| 掉盾动画播完 | 进胆怯 + `CowardIdle` | 0.46s 后 `form=Coward`、`CowardIdle` ✓ |
| 胆怯形态受击 20 | 30（×1.5） | 30 ✓ |
| 破盾动画期间被击飞 | 立即进胆怯 + 走击飞 | `form=Coward`、`state=Launched` ✓ |
| 对象池复用 | 复用后回到持盾态 | 池取新实例 `form=Normal`、盾生效 ✓ |
| 波次接入 | 路线节点刷出 1021 | 第一排列 0 刷出 `盾卫(1021)`，列 2 为 `1011` ✓ |
| 动画状态 | 三个状态按契约出现 | `ShieldGuardHit`（格挡）、`ShieldBreak`（破盾）、`CowardIdle`（胆怯）✓ |

验证过程中的两个坑（勿回归）：

1. **"动画没播"是误判**：命中卡肉期间 Animator 被禁用（§6.2），必须等卡肉结束再读状态。
2. **测试脚本的 HP 记账**：破盾与胆怯两段之间若不复位血量，第二次受击会把敌人打死，导致"没有进入胆怯"的假失败。

验证限制：破盾触发用的是"直接传入重攻击标记 + 反侧方向"的命中，覆盖了 `TakeDamage → 盾判定 → 破盾表现` 整段；"右→左划的蓄力斩击"这条真实按键路径为代码审查确认（`ExecuteSlash` 同时传几何方向与 `InterruptsCavalryCharge`），**尚未用真实手指蓄力实机复现**。

实拍截图（`Library/Locus/Screenshots/`，`Library/` 下可能被清理）：持盾 `locus_game_20261009_141350_926.png`、破盾动画中 `locus_game_20261009_141358_552.png`、胆怯 `locus_game_20261009_141404_038.png`。

## 12. 待验收 / 未覆盖

1. **数值与手感**：固定减伤 10、生命 40、胆怯 1.5×；以及"低伤招式在盾侧只打 1 点"是否过于拖沓。
2. **实机蓄力斩击破盾**：用真实手指右→左划 + 蓄力打盾兵，确认破盾路径成立。
3. **跟手精确攻击 + 盾区判定**（§4.4）：尚未实现，是"正面位置判定"的唯一落地方式。
4. **盾与格挡的正式美术**：持盾Idle形象已验收，Idle/Walk/Attack/ShieldGuardHit/HitFlash正式素材已接入且游戏验收通过。正式 `ShieldBreak` clip、盾的世界掉落对象与脱手接管**已于 2026-10-15 实现并接入，待游戏验收**；格挡火花与金属音效仍未做。
   - 旧分离层的三个已知问题已随接入处理：`ShieldVisual` 上的 sprite 已清空且 transform 已复位；不再由任何 clip 驱动该分离层；池复位（`ResetShieldGuard`）会显式清空它。独立盾对象改为继承本体的层序与透明度来源，不再依赖固定 `sortingOrder`。
5. **方向受击表现**：`HitRight` 已接入（`Enemy_1021_HitRight.anim`，0.9s非循环，待用户游戏验收）；`HitLeft` 不做（盾侧表现并入 `ShieldGuardHit`，见 `design/enemy-1021-animation.md` §7）。
6. **破盾后美术错配（部分缓解）**：破盾后死亡/击飞已改为路由到无盾占位（`CowardDead`/`CowardLaunched`），但两者仍是 102 占位、不是 1021 正式素材；持盾形态的 `Dead`/`Launched_Rise` 仍用带盾帧。`state` 停在 `Attacking`（§6.5）。
7. **字段正名**：`breaksGuard` / `isPreciseAim` 落地后与 `interruptsCavalryCharge` 解耦。
8. **未覆盖场景**：QTE/招架交互、同列多只 1021、共享血量、Boss 混编、移动端手感、长时间节点切换与并发补齐。

## 13. 变更日志

- 本轮（最小可玩）：按用户确认的四条修正（固定数值减伤 + DoT 不计；未声明方向不进判定；1021 与 102 并存；"正面普通/重攻击同样被格挡"）整合机制与实现。新增 `ShieldGuardEnemy`、`ShieldBroken` 形态、两条隔离的胆怯入口、`Enemy_1021` 预制体/控制器，接入路线实验波次；Play Mode 全项验证通过。
- 设计确认记录：盾画在画面左侧（敌人右手持盾）→ `Left` = 盾侧；盾侧重攻击不破盾；AoE 仍按声明方向分类；破盾瞬间减伤失效、掉盾动画期间除死亡外可被击飞打断、普通受击不打断；掉盾动画播完进胆怯。
- 早期版本（已被本轮取代）：曾把"未声明方向"记作"正面 → 不减伤"，与"正面会被格挡"直接冲突，现拆为四值分类。
- 早期仅同步用户确认的美术生产理解：A 型宽矩形木盾、空手副手、固定持盾；盾在本体帧内，破盾动画脱手后由引擎驱动掉落。当时正式素材及掉落接管未实现，机制/数值不变。
- 本轮 Idle 定妆验收：用户确认 `enemy1021_shield_hold_v3.png` 的持盾Idle单帧形象通过；当时正式动画、透明帧、对齐、Unity部署和掉落接管未完成，机制/数值不变。
- 2026-10-11 动画交接同步：Idle/Walk/Attack已接入，Walk与Attack已获用户验收。Attack为十二帧、2秒非循环、1秒接触姿态键；本轮只更新进度记录，机制规则、攻击数值和代码不变。跟手精确攻击、独立声明字段、盾区判定及独立掉盾对象仍未完成；后续在其他对话继续。默认部署完成后由用户开游戏验收，未明确要求不运行自检。
- 2026-10-14 进度同步：ShieldGuardHit六帧与HitFlash六帧已接入第一关并获用户游戏验收；本文§1当前进度、§7播放契约与§12待验收项同步为实际 clip（`Enemy_1021_ShieldGuardHit.anim`、`Enemy_1021_HitFlash.anim`）。HitRight当时仍待透明验收、暂不部署（同日稍后用户自行抠图并授权接入，见下条）；`ShieldBreak`、盾掉落独立对象与脱手接管仍未实现。仅同步事实状态，未改代码、机制规则或数值。
- 2026-10-14 HitRight接入：用户自行抠图并授权接入，六张补边为1108×1108，新建 `Enemy_1021_HitRight.anim`（0.9s非循环）并在控制器新增 `HitRight` 状态与回 Idle 转场；本文§1进度与§12第5项同步为已接入、待游戏验收。盾掉落独立对象与脱手接管仍未实现。
- 2026-10-15 ShieldBreak 接入与独立盾接管：新建 `Enemy_1021_ShieldBreak.anim`（50fps/0.46s，0.12s `OnShieldReleaseFrame`＋0.46s `OnShieldBreakAnimationComplete`）、`Enemy_1021_CowardIdle_Hold.anim`（`CowardIdle` 静态兜底）、`Assets/Prefabs/Effects/Enemy_1021_ShieldDrop.prefab` 与 `ShieldDropEffect`；`Enemy.cs`/`ShieldGuardEnemy.cs` 补脱手、打断（未到脱手帧取消生成、已脱手自行完成）与池复位；旧 `ShieldVisual` 清空。本文§7播放契约、§8破盾表现与§12第4/6项同步为已实现、待游戏验收。
