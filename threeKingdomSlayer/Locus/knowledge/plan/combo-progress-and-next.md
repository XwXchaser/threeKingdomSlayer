---
id: kd_8b9a6f42-f1e9-4f1e-bb81-c84ff3d80c4f
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 连招系统：当前进度与下一步任务

本文档用于跨会话交接。读完这一份 + `design/combo-move-state-machine.md`，即可继续开发，不需要回看历史对话。

## 1. 阅读顺序

1. 本文件（进度、当前数值、下一步任务）
2. `design/combo-move-state-machine.md`（系统权威设计与实现状态，§11.5–11.8 为落地记录）
3. 需要改素材/参考图时：`plan/stab-combo-video-reference-workflow.md`

## 2. 一句话现状

招式状态机（招式图、接续窗口、输入缓冲、蓄力分级、串尾终止）已完成并在实机可用；张飞"三下枪突"连段已完成并调过角度与力度；下一步要做"三下枪突 → 蓄力上挑"，并有一个已识别的时序冲突需要先决策（本轮已选定并实现，见 §7）。

## 3. 已落地的代码

| 文件 | 内容 |
|------|------|
| `Assets/Scripts/MoveSystem/PlayerMoveStateMachine.cs` | 招式状态机：直通模式 / 招式表模式、接续窗口、输入缓冲、串尾、阶段时钟、卡肉冻结、调试面板（`showDebugPanel`） |
| `Assets/Scripts/MoveSystem/MoveTableConfig.cs` | 每武将一份招式表：中立入口、状态表、默认窗口派生 |
| `Assets/Scripts/MoveSystem/MoveGesture.cs` | 手势枚举、`GestureInput`、手势→攻击类型映射（分叉轴 = 是否蓄力） |
| `Assets/Scripts/Attack/StabMotionParams.cs` | 戳击动作参数与"交接姿态"结构 |
| `Assets/Scripts/Attack/StabSweepEffect.cs` | 刺出时间线（起手/刺出/穿入/回收）、高速帧替换、模糊、长轴自转、交接 |
| `Assets/Scripts/Player/AttackSystem.cs` | 招式执行、动作锁、招式资产取用、连段交接、命中判定 |
| `Assets/Scripts/Player/InputManager.cs` | 手势识别、蓄力分级、六处出招点收敛为 `SubmitGesture` |

关键机制说明：

- **招式资产即节点**（一层结构）：窗口 / `repeatSelf` / `moveEdges` 直接写在 `AttackSkillConfig` 上；`MoveDefinition` / `MoveEdge` 已删除。
- **取配置**：状态机把解析出的招式资产传给 `AttackSystem.TryExecuteAttack(..., moveConfig)`；`GetConfig(type)` 优先用它，否则回落到 `HeroConfig.skillConfigs`。**直通模式（无招式表）行为与改造前一致。**
- **连段交接**：新一段从上一段末态起步（位置/朝向继承），上一段改为"隐藏 + 关闭命中"，由它自己的时间线跑完再销毁（不 Kill，避免销毁级联带走同帧新建的下一段）。
- **长轴自转**：`stabRollDegrees` 落在形变节点的本地 Y（精灵长轴）上，刺出段 0→设定值，回收段转回 0。

## 4. 三段枪突当前数值

| 参数 | Jab1 | Jab2 | Jab3 |
|------|------|------|------|
| 伤害 / actionDuration | 20 / 0.45 | 26 / 0.40 | 34 / 0.60 |
| 后继 | 点击 → Jab2 | 点击 → Jab3 | 无（串尾） |
| 接续窗口（占整段） | 0.65–0.95 | 0.65–0.95 | — |
| 预备 / 刺出比例 | 0.12 / 0.28 | 0.15 / 0.25 | 0.20 / 0.20（含 0.05s 蓄势停顿） |
| 枪尾偏移 右/上/前 | 0 / 0 / 0 | −0.9 / +0.7 / −0.3 | +0.9 / −0.6 / −0.5 |
| 画面内姿态倾角 | 0° | 4° | 5° |
| 长轴自转 | 0° | **−25°** | −4° |
| 拉伸 / 模糊倍率 | 1.18 / 1.0 | 1.22 / 1.15 | 1.30 / 1.35 |
| 前伸 / 卡肉 | 2.0 / Standard | 2.8 / Standard | 3.6 / Heavy |

连段蓄力保持参数（写在 Jab3 上，仅当节点存在 `minChargeLevel≥1` 的边时生效）：

| 参数 | 现值 | 含义 |
|------|------|------|
| `chargeHoldRetractRatio` | 0.75 | 按下后枪体被拉回到回收段的哪个位置（位移即蓄力条） |
| `chargeHoldPullSeconds` | 0.3 | 拉满蓄势位所需按住时长，与一级蓄力门槛对齐 |
| `chargeHoldPitchDegrees` | 8 | 拉回时枪身向上预压的仰角 |
| `chargeHoldShakeAmplitude` / `chargeHoldShakeFrequency` | 0.06 / 14Hz | 蓄势位微颤（幅度随拉回进度增大） |
| `minChargeLevel`（边） | 1 | 该边要求的最低蓄力等级，未达标不参与匹配 |
| `launchSkipWindup`（终结技） | true | 上挑跳过预备段，直接从蓄势位起 |

资产路径：`Assets/ScriptableObjects/Moves/Zhangfei/Zhangfei_Jab*.asset`；招式表 `Assets/ScriptableObjects/Moves/MoveTable_Zhangfei.asset`（已挂到 `Hero_Zhangfei.moveTable`）。

参数含义与调法：

- **枪尾偏移**决定路径朝向（推导出偏航/俯仰），是"斜插方向"的主控；枪尖仍落在本列，所以怎么斜都不会打空。
- **画面内姿态倾角**只改持枪姿态，不改轨迹。
- **长轴自转**是"枪身侧过来一点"的立体感；正值与负值方向相反（当前 Jab2 用负值才是想要的方向）。
- 2D 精灵绕长轴自转会视觉变薄；如需更大角度，优先考虑换 3D 模型（见第 6 节）。

## 5. 已验证 / 未验证

已验证（Play Mode 实测）：三段连段贯通、第三段不接受任何输入（串尾）、早于窗口的投递只进缓冲、每段各自使用自己的招式资产（伤害 20/26/34、时长 0.45/0.40/0.60）、画面里始终只有一把可见的枪、每段朝向与起点逐段承接、长轴自转 0→20° 渐变（实测中段 11°、末段 20°）。

未验证或未做：

- 有敌人时的命中表现（本轮测试场地无敌人，命中只有推导，没有实机打击感验收）。
- 三段素材层面的视觉差异（当前三段共用 `Stab.prefab` 与 `stab_v13.png`）。
- 招架成功态、局外解锁、局内三选一改写招式表：均未开始。

## 6. 已定的后续方向

- **枪体将换成 3D 模型**（用户准备素材）。替换后：长轴自转变成真正的绕轴旋转，不再有精灵变薄的副作用；高速帧那套精灵替换（`stab_v13`）可以退休，刺出速度感改用其它手段表达。接手时要先确认模型的枪身长轴是否为其本地 Y，以便 `stabRollDegrees` 直接沿用。
- 招式动作的表现力提升（弧线轨迹、五段节奏、镜头跟随动作方向）已讨论但未实施。

## 7. 已实现：三下枪突 → 蓄力上挑（本轮）

原需求与三个候选方案保留在 §7A（决策记录）。最终按「放宽窗口 + 按住宽限 + 连段蓄力动作语言」实现，并做过 Play Mode 状态机级实测。

### 7.1 玩法

点、点、点（Jab1→Jab2→Jab3）之后**按住不放**：第三击刺出后枪体沿回收路径被「拉回」，拉回的过程就是蓄力条（0.3s 拉满，与一级蓄力门槛对齐）；拉满即进入蓄势位（枪尾收到玩家侧、枪尖退到中景、枪身向上预压 8°，再向后一顿后开始沿枪轴前后微颤）。此时**上划**即释放收尾：**保留挑飞（击飞）效果**，动作语言换成「一记大幅朝左上的扫击」——枪体从蓄势位先压一下再一甩扫出，同时叠一层 slash 的扫掠观感（扇形 110° + 拖尾 + 右→左 + 路径斜度 −22°）；伤害与击飞仍由挑飞结算。

### 7.2 动作语言（只借素材帧，不搬蓄力表现）

| 阶段 | 表现 |
|------|------|
| 按下（第三击刺出后） | 停止原回收节奏，沿回收路径受控拉回；素材帧切 `chargeSprite2` |
| 拉满（0.3s） | 停在蓄势位（回收 75%）；随后**再向后一顿**（`chargeHoldSettleSeconds` 0.12s、`chargeHoldSettleRatio` +0.10 → 共回收 85%），一顿结束后才开始**沿枪身长轴前后**微颤；素材帧切 `chargeSprite1`（静态档位，不闪） |
| 上划释放 | **挑飞（击飞）保留**；枪体从蓄势位先做一小段预备再向左上扫出（`launchSweepMode`：终点枪尖方向/终点位移直接在相机平面给出，不受挑飞角度钳制；扫击模式旋转与位移同曲线一甩到底）；同时叠一层 slash 扫掠表现层（`SweepEffect`，目标为空 → 不判定、不伤害） |
| 未蓄满松手 | 由 `StabSweepEffect` 自己把回收段剩余部分走完（不 seek 时间线），第三击正常收尾 |

不做的事：不复用蓄力视觉的进出场/跟手/帧闪；不新建第二把蓄力枪。

### 7.3 改动清单

| 位置 | 改动 |
|------|------|
| `AttackSkillConfig` | 新增（边）`minChargeLevel`；（节点）`chargeHoldRetractRatio` / `chargeHoldPullSeconds` / `chargeHoldPitchDegrees` / `chargeHoldSettleSeconds` / `chargeHoldSettleRatio` / `chargeHoldShakeAmplitude` / `chargeHoldShakeFrequency`；（横扫）`slashSweepDirection` / `slashMovementTiltDegrees` / `slashOverrideVisualTilt` / `slashVisualTiltDegrees` |
| `PlayerMoveStateMachine` | 三处按 `chargeLevel` 过滤边（`CanResolve` / `GetWindowFor` / `ResolveFromCurrent`）；新增「按住不成时本段先不结束」的驻留（含 1.2s 上限）；驻留时仍消费缓冲 |
| `StabSweepEffect` | 蓄力拉回接管：`BeginChargeHold` / `SetChargeHoldHeldSeconds` / `TryGetChargeHoldPose` / `ReleaseAfterChargeHold` / `EndChargeHold` + 自己续完回收 |
| `LaunchVisualEffect` | 支持显式起手姿态（枪体中心 + 世界旋转 + 世界缩放）与跳过预备 |
| `AttackSystem` | `BeginComboChargeHold` / `UpdateComboChargeHold` / `EndComboChargeHold`；非戳击招式接手时释放蓄势枪体（`ReleaseHeldStabVisual`）；横扫的方向/倾斜可被招式资产覆盖；`poiseDamage>0` 的横扫命中时削韧 |
| `ChargeStabVisual` | 连段蓄力期间抑制进出场蓄力视觉（`SuppressForComboCharge`，指针抬起自动解除） |
| `InputManager` | `IsPointerDown`（含 QTE 优先级）、`HoldDurationSeconds` |
| 资产 | 三段窗口末端 0.95→1.0（Jab3 起点 0.65→0.60）；Jab3 新增 `SwipeVertical + minChargeLevel=1` 边指向新资产；新建 `Zhangfei_LaunchFinisher.asset`（Slash 类型：damage 60 / poise 80 / rows 3 / 半宽 4.5 / 扇形 110° / 路径斜度 −22° / 视觉倾斜 20° / 动作锁 0.6s） |

### 7.4 实测（Play Mode，状态机级）

已验证：三段连打（窗口内 t≈0.70 接续）；按下后枪体确实被拉回到玩家侧（枪体中心 z≈−5.1，原刺出位 z≈+1.9）；素材帧档位 charge2→charge1；抖动纯沿枪轴前后（垂直分量 0.000），且到位后再向后一顿（比例 0.75→0.85）才开始抖；未蓄力竖滑被拒（「无匹配边」）；蓄力竖滑在窗口开放当帧被消费并解析为 `Zhangfei_LaunchFinisher`、成功执行（动作锁 0.6s）；横斩扫掠从 (4.17,−1.99) 走到 (−4.10,+1.36)：方向=右→左、向上位移 ✓（即「朝左上」）；蓄势位那把枪已交掉（场上无第二把枪）；未蓄满松手时回收平滑续完（每帧 0.10–0.17 世界单位，无突跳）且无残留；驻留超过 1.2s 上限后正常收尾；普通斩击的方向/倾斜仍跟手势（回归通过）。

本轮修掉的两个真 bug：① 驻留时「推进时钟前就 return」导致窗口开放前到达的蓄力上划永远不被消费（刚蓄满就上划出不来）；② 松手时用 DOTween `Goto` seek 序列导致跳位（改为自己续完回收）。

### 7.5 仍需实机验收（无法自动验证）

- 手感与读数：拉回—停顿—上挑的节奏是否顺、微颤幅度是否够、预压 8° 是否合适。
- 打击与击退：横斩命中时的伤害/击退表现（60 伤害 / 3 排 / poise 80）与镜头反馈；扫掠的「幅度感」是否够（现在靠扇形 + 拖尾，没有额外的弧面素材）。
- 不可调项提示：`launchFlickAngle` 在视觉里被钳制到 45–70、`launchRiseHeight × 0.49` 再钳制 0.40–0.56、模糊强度写死在 `LaunchVisualEffect` ——「把角度/高度拉满」不会继续变强，要更强必须改视觉代码。

### 7.6 主要可调旋钮

| 旋钮 | 位置 | 现值 |
|------|------|------|
| 拉回量 | `Zhangfei_Jab3.chargeHoldRetractRatio` | 0.75 |
| 拉满时长 | `chargeHoldPullSeconds` | 0.3 |
| 预压仰角 | `chargeHoldPitchDegrees` | 8 |
| 微颤 | `chargeHoldShakeAmplitude` / `Frequency` | 0.06 / 14Hz（沿枪轴前后） |
| 向后一顿 | `chargeHoldSettleSeconds` / `chargeHoldSettleRatio` | 0.12s / +0.10 |
| 驻留上限 | `PlayerMoveStateMachine.holdWaitMaxSeconds` | 1.2 |
| 三段窗口 | `Zhangfei_Jab1/2/3.windowEnd01` | 1.0（Jab3 起点 0.60） |
| 终结技（挑飞） | `Zhangfei_LaunchFinisher.asset` | damage 60 / rows 3 / 击飞 1.2s / 扫击时长 0.26s（动作锁 0.6s） |
| 扫击终点姿态/位移 | `launchSweepUp` / `Left` / `Forward` / `Rise` / `LeftShift` | 1 / 0.7 / 0.35 / 1.0 / 1.8 |
| 扫击预备 | `launchWindupDuration` / `launchWindupDistance` | 0.06s / 0.12 |
| 扫掠表现层 | `slashSweepHalfWidth` / `Angle` / `Direction` / `MovementTilt` / `VisualTilt` / `Duration` | 4.5 / 110° / 右→左 / −22° / 20° / 0.36s |

### 7.7 待改进（本轮实测发现，未实施）

1. **蓄力指示器仍会与 C4 同时出现**（用户反馈“偶尔”）
   - 现象：连招蓄力期间（按住 ≥0.3s）屏幕上的**蓄力指示器**仍会出现，与连招的枪体同时存在。
   - 原因：蓄力事件链有 5 个订阅方，本轮只让 `ChargeStabVisual` 与 `PierceAimIndicator` 让位；`ChargeIndicatorController`（`Assets/Scripts/UI/ChargeIndicatorController.cs`，`appearThreshold = 0.3`）没让位。
   - 修法：照 `PierceAimIndicator` 同一模式，在 `OnChargeUpdated` 开头加 `if (InputManager.Instance != null && InputManager.Instance.comboChargeActive) { indicatorRoot.SetActive(false); return; }`，`OnChargeBegan` 里同样判一次。
   - 需一并决定：`ThornArmorEffect`（蓄力护盾视觉）与 `PlayerState` 的蓄力减伤护盾，是否也在连段蓄力中让位/生效。

2. **C4 收尾的斜率太低：应更像“上挑”，不是横扫**
   - 现状：`launchSweepUp/Left/Forward = 1/0.7/0.35`、`launchSweepRise/LeftShift = 1.0/1.8` → 偏左的斜扫；扫掠表现层 `halfWidth 4.5 / 扇形 110° / 路径斜度 −22°` 也偏“横向大扫”。
   - 目标：**正上方偏左**——上挑为主，只带一点左。
   - 建议初值（改 `Assets/ScriptableObjects/Moves/Zhangfei/Zhangfei_LaunchFinisher.asset`）：
     - `launchSweepUp / Left / Forward` → 1 / 0.25 / 0.3
     - `launchSweepRise / LeftShift` → 1.5 / 0.35
     - `slashSweepHalfWidth` → 2.2（缩短横向行程）、`slashMovementTiltDegrees` → −50（扫掠线更陡）、`slashSweepAngle` → 70、`slashVisualTiltDegrees` → 35（把“向上的抬升量”补回来：该层的抬升 = `halfWidth × tan(tilt)`）

3. **C4 的卡肉要更强**
   - 现状：挑飞的伤害走透明 `AttackWave`，其 `HitTarget` 里 `damageType == Launch → HitFeedbackStrength.Heavy`（0.14s，已是三档里最强）；所以“更强”需要超出 Heavy。
   - 建议：① 在 `HitFeedbackManager` 增加一档（如 `HeavyPlus` ≈ 0.20s）；② `AttackWave.Create` 增加可选参数 `feedbackStrengthOverride`（`HitTarget` 优先用它），`AttackSkillConfig` 增加 `hitFeedbackStrength` 字段，终结技设新档位；③ 同一档位顺带提升另外两层反馈（`design/combat-hit-feedback-direction.md`：受击者 / 攻击点 / 镜头）——`PixelHitEffectManager` 的 Launch 爆点尺寸与 `CameraFeedbackController.RequestHit` 强度，让“更重”在三层上都读得出来。

## 7A. 历史：原「下一步任务」的决策记录

（下列内容为本轮实施前的原始方案与三个候选，保留作决策记录；结论见 §7。）

## 7. 下一步任务（历史）：三下枪突 → 蓄力上挑

**需求**：三下点击（Jab1→Jab2→Jab3）之后，玩家用"蓄力 + 竖滑"（挑飞的输入语言）触发收尾，释放一个强力挑飞，作为四连的终结技。

**实现位置（历史方案）**：纯数据即可完成大半——在 `Zhangfei_Jab3.asset` 上加一条边：手势 = 竖滑、需蓄力 → 后续节点 = 新建的强力挑飞资产（复用现有挑飞的参数组：`launchFlickAngle` / `launchFlickDuration` / `launchWindup*` / `launchRiseHeight` / `launchSideTilt`，把角度、上升高度、模糊、卡肉、伤害拉高，`moveEdges` 留空即串尾）。

**需要先做的小扩展**：`AttackMoveEdge` 目前只有"手势 + 窗口"，没有"要不要蓄力"这一维。要支持"蓄力竖滑"，需要给边加上最低蓄力等级（例如 `minChargeLevel`，0 = 任意），并在状态机匹配边时用 `GestureInput.chargeLevel` 过滤。这是几行的改动。

**必须先决策的时序冲突（重要）**：

蓄力需要玩家按住 0.3 秒以上，而 Jab3 当前的接续窗口只有 0.65–0.95（0.60s 的段 → 实际 0.39–0.57s，约 180ms）。也就是说玩家必须在这 180ms 内完成"按住 0.3 秒再竖滑"，物理上做不到。可选方案：

1. **放宽 Jab3 的窗口**：例如把起点提前到 0.3、终点放到 1.0 甚至允许在收尾结束后仍保留一小段"可接续余韵"。改动最小，但窗口整体变宽，也让"点一下继续点"更容易误触发。
2. **蓄力手势单独给宽限期**：当检测到玩家"按住不动"时，暂停/延长当前节点的接续窗口（把窗口计时冻结），松手或滑动后再继续判断。手感最好，但需要在状态机里加一条"等待输入"的状态。
3. **改输入语言**：这个终结技不用"蓄力竖滑"，改用其它不冲突的输入（例如三下之后的长按不移动，或直接第四下点击）。最省事，但削弱"蓄力"的表达。

建议先讨论并选定其中一条，再动手；否则做完会卡在这个冲突上。

**验收标准**：三下点击后能用该输入触发强力挑飞；触发时武器从 Jab3 的末态自然接上（不出现第二把枪）；不触发时 Jab3 正常收尾回中立；连段全程画面里只有一把可见武器。

## 8. 提交状态

本文件所在提交之前，代码与资产改动均已提交并推送到 `route-fake-movement` 分支（见最近提交信息）。本轮（三下枪突 → 蓄力上挑，见 §7）的代码与资产改动**尚未提交**：

- 代码：`AttackSkillConfig.cs` / `StabMotionParams.cs` / `AttackSystem.cs` / `InputManager.cs` / `ChargeStabVisual.cs` / `LaunchVisualEffect.cs` / `StabSweepEffect.cs` / `PlayerMoveStateMachine.cs`
- 资产：`Zhangfei_Jab1/2/3.asset`（窗口）、`Zhangfei_LaunchFinisher.asset`（新建）

---

## 9. 新增链：张飞·指向突刺链（stab → 蓄力指向释放）

- 权威文档：`design/combo-zhangfei-aim-stab.md`；命名规范与连招登记表：`design/combo-registry.md`。
- 玩法：点一下 → 按住蓄力（可横向移动改目标列，枪身朝所指列偏摆）→ 松手 → 在松手所指列打一记 range 2 的单列戳击，命中击退 1 格（选了升级 `push_wave` 后 2 格），然后串尾结束。
- 资产：`Zhangfei_Jab1`（起步节点，新增 `Hold + minChargeLevel 1` 边与蓄力保持/指向偏摆参数）+ `Zhangfei_StabR2`（指向戳，当前 id 15，建议按规范迁到 22）。
- 与本文件 §7 的枪突链（别名 C4）共用同一套底层（边 + `minChargeLevel` + 按住驻留 + 枪体交接）。
- 本轮同时确立了四条共享输入规则（一次按住一记蓄力招式、站桩/连段的判定依据、滑动消费语义、招式属性归属），见 `design/combo-move-state-machine.md` §18。
