---
id: kd_ea04fcf7-9cb3-4bd7-9439-7549525ca59a
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 连招新增：stab → 蓄力指向释放（本列前 2 排戳击）

**当前状态：设计已定稿，代码与资产尚未实现。** 本轮只产出本文档（用户明确要求"先只落设计文档"）。实现清单见 §7，验收见 §9。

相关文档：`design/combo-move-state-machine.md`（招式转移图与状态机权威设计）、`plan/combo-progress-and-next.md`（三段枪突与 C4 的进度与数值）。

---

## 1. 需求

已有连段：**stab → stab → stab →（按住蓄力）→ 上挑收尾**（`Jab1 → Jab2 → Jab3 → Zhangfei_LaunchFinisher`，早期文档按 `C_n = (n-1)方 + 蓄力` 称其为 **C4**）。

新增连段：**C1（第一下 stab）→（按住蓄力）→ 松手释放 → 在松手时手指所指的那一列，打一记纵深 2 排的戳击（C2）→ 串尾结束**。

**命名口径（以用户口径为准）**：新链只有两段——**C1 = 第一下 stab**，**C2 = 蓄力指向释放的那一记 range 2 单列戳击**，且 **C2 打完即串尾、不再接续**。本文档中的 C1/C2 均指此口径；早期文档里的 C4 只用来指代已有连段的收尾。

用户确认的口径（2026 本轮对话）：

| 项 | 值 |
|---|---|
| 锚点节点 | 第一下 stab（`Zhangfei_Jab1`） |
| 释放手势 | **松手**（不是滑动） |
| 目标列 | **松手时手指位置**所映射的列 |
| 打击范围 | **本列**（单列）+ **前 2 排**（`rangeRows = 2`） |
| 显示效果 | 由 Agent 新设计（见 §4），与 C4 必须在"看"上可区分 |
| 中途改列 | **允许**：按住期间可横向移动手指改目标列（用户选定） |
| 段数与收尾 | **两段**：C1（stab）→ C2（蓄力释放），**C2 串尾**（`moveEdges` 留空） |
| C2 命中表现 | 枪体**轻微震动**表达冲击力（本轮新增要求，见 §4.4） |
| C2 命中位移 | **击退 1 格**（用户选定，见 §4.5）；位移做成**每招可配**，后续会统一改动 |

---

## 2. 与已有 C4 的差异化定位

| | 已有：三下枪突 → 蓄力上挑（C4） | 新增：C1 stab → C2 蓄力指向释放 |
|---|---|---|
| 输入 | 点、点、点 → 按住 → 上划 | 点一下 → 按住 → 松手 |
| 蓄力动作语言 | 纵向预压 + 枪轴前后微颤 | **横向偏摆"指向"** + 沿指向小幅推拉 |
| 瞄准提示 | 无 | **两排"钉"标记 + 流光 + 进度环** |
| 释放表现 | 扇形上挑扫掠（叠 slash 表现层）+ 挑飞 | **直线定点突刺**，穿透两排 |
| 落点 | 全列 3 排 | **本列前 2 排** |
| 玩法意图 | 收尾爆发 / 挑空 | 定点重刺、可"指哪打哪"（含改列） |
| 命中位移 | 无（普攻击退只在局内升级后出现） | **招式自带击退 1 格**（`pushBackRows`，见 §4.5） |

两条链共用同一套底层：招式状态机的边 + `minChargeLevel` 过滤 + 按住驻留 + 枪体交接（`CapturePose` / `HandOff`）。

---

## 3. 机制落地（现状已具备 vs 需改动）

### 3.1 直接复用（不需改代码）

| 环节 | 依据 |
|---|---|
| 边的蓄力维度 | `AttackMoveEdge.minChargeLevel`；状态机 `CanResolve` / `GetWindowFor` / `ResolveFromCurrent` 三处均按 `chargeLevel` 过滤 |
| 按住驻留 | `PlayerMoveStateMachine.IsWaitingForHold()`：指针按住且节点有后继时，时钟停在「最早窗口起点」，上限 `holdWaitMaxSeconds`（1.2s） |
| 蓄力动作语言 | `Jab1` 本身是戳击 → 新边带 `minChargeLevel ≥ 1` 后 `AttackSkillConfig.HasChargeContinuation()` 为真 → 按下即 `AttackSystem.BeginComboChargeHold()`，枪体沿回收路径拉回（位移即蓄力条）+ 预压 + 微颤 |
| 松手位置 → 列 | `InputManager.ProcessLongPressGesture` 已是 `GetStabColumnFromScreenPosition(releasePos)`，并带"最近匹配超过半列宽则返回 -1"的空列保护 |
| 单列 + 纵深 N 排 | `AttackSystem.ExecuteStab` → `StabSweepEffect.CheckHits` 只遍历本列，命中 `rowIndex < rangeRows`；`ColumnManager.GetEnemiesInRange(column, rows)` 同口径。**故 `rangeRows = 2` 是纯数据** |
| 连段交接（始终只有一把枪） | `StabSweepEffect.CapturePose()` / `HandOff()`，新段用 `StabStartPose` 从上一段末态起步 |
| 窗口百分比 / 攻速缩放 / 输入缓冲 | 既有机制 |

### 3.2 必须改的两处输入层

**改动 1 — 松手释放现在被显式吞掉。**

`InputManager.ProcessGesture`：

```csharp
// 现状：连招蓄力中已蓄成的那一下松手不再触发站桩蓄力
if (comboChargeActive && CurrentChargeLevel >= 1) return;
```

改为先投递、由状态机裁定：

```csharp
if (comboChargeActive && CurrentChargeLevel >= 1)
{
    SubmitGesture(MoveGesture.Hold, CurrentChargeLevel, column is from release position);
    return;
}
```

安全性：`PlayerMoveStateMachine.SubmitInput` 在 `_active` 分支下**没有匹配边时不会回落直通**（只返回 false，`_lastResolution` 为"无匹配边"）。因此没有 Hold 边的节点行为与今天完全一致——松手仍然不会误出站桩蓄力（穿刺）。

**改动 2 — 允许按住期间横向改列。**

现在 `isLongPress` 成立的条件是「距按下点欧氏距离 ≤ `chargeMovementTolerance`（默认 20px）」，而竖屏一列宽约 200px：手指移到别列会让 `CurrentChargeLevel` 直接掉回 0，蓄力失效。

改法：把该条件放宽为「**纵向抖动 ≤ 20px；横向按列宽计**」（即横向位移不再清零蓄力）。

不需要额外处理的部分：`TryDetectHoldSwipe` 只在**瞬时速度 ≥ `minSwipeSpeed`** 时才开始追踪横向滑动，因此「慢慢挪手指指向」天然不会进入滑动路径，也不会触发 `ResetSegment` 清空蓄力计时；高速滑动仍走原手势路径（`Jab1` 上未配对应边时会被"无匹配边"拒绝，不会误出招）。

### 3.3 数据流（列指向）

```
按下（Jab1 进行中）
  → comboChargeActive = true，Jab1 枪体进入蓄力保持
每帧：当前指针位置 → 目标列（复用 GetPierceColumnFromScreenPosition / GetStabColumnFromScreenPosition 口径）
  → 广播给指向标记（OnChargeUpdated 已有该事件，5 个订阅方之一）
松手：最后一次位置的列 → SubmitGesture(Hold, level, column)
  → 状态机解析 Hold + minChargeLevel → ExecuteMove(新资产, cancelCurrentMove: true)
  → ExecuteStab(该列)，从蓄势位姿态起步
```

---

## 4. 显示效果设计（新设计）

原则：与 C4 在视觉上完全区分；**不新增美术资产**（标记与速度带用程序化几何 + 现有贴图/材质），先保证可读性。

### 4.1 蓄力阶段（按住第一下 stab 之后）

- **0–0.15s 静止期**：枪体先不动（避免按下即抖）。
- **枪体**：拉回（`chargeHoldRetractRatio` 0.85）+ 预压（`chargeHoldPitchDegrees` 10°）+ 指向偏摆 `chargeHoldYawDegrees`（朝手指所指列，上限 ±10°）+ 平滑 `chargeHoldYawSmoothSeconds`（0.12s，避免换列时跳变）→ 身体语言读出"要打那边"。
- **微颤差异化**：C4 为沿枪轴前后抖；本链为沿指向方向的小幅推拉（沿用资产的 0.06 / 14Hz）。
- **指向标记：已移除（用户实机否决）。** 本节曾设计 `StabAimMarker`（目标列两排菱形 + 流光 + 进度环），用户明确表示这条链**不需要瞄准标记**；组件、场景对象与仅供它使用的 `AttackSystem.TryGetStabIndicatorPath` 已全部删除。
- **让位关系**：`ChargeStabVisual`、`PierceAimIndicator` 继续让位（现有条件不变）；`ChargeIndicatorController` 已纳入本链让位（顺手修掉 `plan/combo-progress-and-next.md` §7.7-1 的既有问题）。**仍待决定**：`ThornArmorEffect`（反伤盾视觉）没有让位，仅当玩家持有"反伤盾"升级时才会在连段蓄力中亮起。

### 4.2 释放瞬间（松手）

- **甩出段（刺出前 20%）**：偏摆从 ±10° **归零并过冲 −3°**（新字段 `stabRedirectSnapRatio` 控制该比例），枪尾同时沿指向方向做一小段位移 → 读作"甩过去打那边"。
- **主表现**：沿用 `StabSweepEffect` 既有时间线（起手→刺出→穿入→回收），按"重刺"取上档参数：`stabThrustLengthScale 1.22~1.30`、`stabThrustWidthScale 0.86`、模糊 28/14/18、高速帧区间启用、`stabRollDegrees 3~6°` 补立体感。
- **方向层**：一条**直线速度带**（蓄势位 → 枪尖，宽约 0.15，随时间收窄）；**不使用 slash 扫掠层**（那是 C4 的语言）。优先复用 `StabSweepEffect` 内已有轨迹/拖尾机制。
- **指向轨迹天然成立**：`ExecuteStab` 本就按「枪尾（玩家侧）→ 目标列目标点」重算射线（`GetStabRayYaw(列)`），改列时轨迹自动是"改向刺入"。

### 4.3 命中（两排分层，读出"穿透"）

| 排 | 卡肉 | 像素爆点 | 镜头 |
|---|---|---|---|
| 第 1 排 | `Heavy`（蓄力释放更重） | 中档尺寸 | 单次中档震动 |
| 第 2 排 | `Light` | 0.7× 缩小 | **不叠加震动** |

第二排"更轻、不叠震"是刻意的：读作"穿过去了"，而不是"打了两次"。第 2 排当时无敌人时，标记照常显示（预告），只是不出反馈。

### 4.4 C2 命中瞬间的枪体震动（力量感，本轮新增要求）

用户观察：**当前 stab 三连击读起来只是"戳/刺"，没有力量感**。C2 作为蓄力释放，用"命中时枪体自身震动"把冲击力读出来（三连击的既有手感本轮不动，见 §8）。

**震动设计（只抖玩家的枪，不动敌人）**

| 分量 | 表现 | 建议初值 |
|---|---|---|
| 轴向回弹 | 命中瞬间枪体沿刺出方向**反推**一小段再弹回（衰减振动） | 幅度 0.06 世界单位 |
| 横向抖动 | 垂直于枪轴的极小幅抖动 | 幅度 0.02 |
| 滚转颤 | 绕枪身长轴的短促滚转（与 `stabRollDegrees` 同轴叠加） | ±3° |
| 俯仰点头 | 枪尖短促下压回弹（"顶住了"） | ±1.5° |
| 频率 / 时长 / 衰减 | 4–6 次指数衰减振动 | 约 18Hz、总时长 0.08–0.12s |

**三条硬约束**

1. **只抖枪体，不动敌人**：敌人受击动画、卡肉、击退链路照旧；本项纯属玩家侧表现，不新增任何位移（不触碰 `design/immutable-constraints.md` 的位移规则）。
2. **不能被卡肉冻住**：现有卡肉是 `HitFeedbackManager` 让特效序列 `seq.Pause()`（不改 `Time.timeScale`）；若把震动做进同一条 DOTween 序列会被一起冻住。震动必须**独立计时驱动**（`SetUpdate(true)` 的独立 tween，或按 `Time.unscaledDeltaTime` 自驱动），让"先停住（卡肉）→ 枪身震颤 → 恢复行进"读成同一件事。
3. **只有真实命中才抖**：空挥不抖（与"空挥不给能量、不触发被动"的既有口径一致）。

**两排分层**：第 1 排全量；第 2 排按 0.6 倍触发（与 §4.3 一致，避免读成"打了两次"）。

**力量感是四层叠加的结果**，不是单靠震动：卡肉（Heavy）+ 枪体震动 + 命中爆点/镜头（§4.3）+ 释放期的直线速度带（§4.2）。缺任一层都容易退化成"戳一下"。

**与 C4 的区别**：C4 是上挑扫击，不使用本震动；本震动是 C2 的专属语言。

### 4.5 C2 命中位移：击退 1 格（用户选定）

**现有通道（不改）**：击退由 `ColumnManager.ApplyPushWave(hitEnemies, pushAmount, canInterruptCFrame, pushedEnemies)` 执行，随后 `PostDisplacementFillUp(pushedEnemies)` 负责回位；通道内部已有 owner/generation 校验、栈式阻塞与"回原槽"语义。今天这条通道**只由局内升级驱动**（`UpgradeEffectManager.GetPushWaveDistance()`，在 `AttackSystem.ExecuteStab` 的命中回调里读取），招式本身不带位移。

**本轮设计：位移挂在招式上**

- `AttackSkillConfig` 新增 **`pushBackRows`（int，0 = 不击退）**；C2 该值为 **1**。
- 语义：**每次命中结算时，对本次判中的敌人施加 `pushBackRows` 排击退**（沿 row 增大方向后移，停顿后回原位）。
- 生效流程沿用既有通道：命中 → `ApplyPushWave(命中列表, pushBackRows, canInterruptCFrame: false, pushedEnemies)` → 序列结束 → `PostDisplacementFillUp`。
- **两排不分层**：位移是"每个被命中目标"的事——第 1、2 排被命中的敌人都吃 1 格；只有震动与打击反馈按排分层（§4.3 / §4.4）。

**口径与既有约束（不得越过）**

| 项 | 口径 |
|---|---|
| 霸体 / C技（CFrame） | `canInterruptCFrame = false` → 与普通戳击一致，**不被推动**（若要能推，需单独设计并回归打断体系） |
| Boss / 骑乘骑兵 109 | `ApplyPushWave` 内部已免疫（`isBoss` / `IsMountedCavalry`），无需新代码 |
| 不重叠 | 由 `ApplyPushWave` 的栈式阻塞检查保证；目标槽被未参与者占据时按既有裁决处理 |
| 只影响被命中者 | 不得触发列内压实、不得带动未命中敌人（`design/immutable-constraints.md` §2） |
| 回位 | 只允许被击退者返回**自己的原槽**，不得改用最近空位（同文 §4） |
| 补齐 | 击退产生的临时空槽**不触发普通补齐**、也不开启节奏门（`design/percolumn-fillup-rules.md` 规则 1） |

**与升级"击退波"的关系：待你定（这就是你后面要做的"招式位移统一改动"的接口）**

- **A（本轮先按此实现，规则最简单）：取较大值** —— `effective = max(move.pushBackRows, UpgradeEffectManager.GetPushWaveDistance())`。避免"招式 1 格 + 升级 2 格"叠成 3 格。
- **B：招式优先** —— `move.pushBackRows > 0` 时用招式值，否则回落升级值；表达"这一招固定推多远，升级不再影响它"。

**不在本轮范围**：把 Slash 方向性击退（`directionalPushStep`）、Launch 击飞、聚拢/牵引**也统一收到招式配置**上（你已声明后续要做的那次变动）。本轮只新增 `pushBackRows` 并只给 C2 用。

---

## 5. 参数清单

| 位置 | 字段 | 说明 / 建议值 |
|---|---|---|
| 新资产（`Zhangfei_StabR2`） | `attackType = Stab`、`rangeRows = 2` | 单列前 2 排 |
| | `damage` / `actionDuration` / `cooldown` | 段位建议 30~40 / 0.45 / 0.3（待定） |
| | `attackWavePrefab` | **必须有值**（`ExecuteStab` 缺它直接返回 false） |
| | `stabThrustLengthScale` / `WidthScale` / `stabBlur*` / `stabRollDegrees` | 重刺档 |
| | `stabFirstHitStrength` | `Heavy` |
| | `stabRedirectSnapRatio`（**新增字段**） | 释放前归零 + 过冲的比例，建议 0.2 |
| | `hitShakeAmplitude` / `hitShakeLateral` / `hitShakeRollDegrees` / `hitShakePitchDegrees` / `hitShakeDuration` / `hitShakeFrequency` / `hitShakeDecay` / `hitShakeSecondRowScale`（**均为新增字段**） | §4.4 命中枪体震动；第二排按倍率减弱 |
| | `pushBackRows`（**新增字段**） | C2 = **1**（击退 1 格）；与升级击退波的组合规则见 §4.5（暂取较大值） |
| | `interruptsCavalryCharge` / `poiseDamage` / `ultimateEnergyGain` / `chargeLevelDamageMultipliers` | 逐段透传；能量建议低值或 0（避免多段刷能量） |
| | `moveEdges` | 留空 = 串尾 |
| 写在 `Zhangfei_Jab1` | 新边：`gesture = Hold`、`minChargeLevel = 1`、`next = Zhangfei_StabR2`、`overrideWindow`（建议 0.55–1.0） | 与 C4 的边写法同构 |
| | `chargeHoldRetractRatio` / `PullSeconds` / `PitchDegrees` / `Settle*` / `Shake*` | 复用 C4 那套 |
| | `chargeHoldYawDegrees`（**新增字段**） | 指向偏摆上限，建议 10 |
| | `chargeHoldYawSmoothSeconds`（**新增字段**） | 指向偏摆平滑时间，0.12s（0 = 不平滑） |
| ~~新组件 `StabAimMarker`~~ | 已按用户要求移除（见 §4.1） | — |

---

## 6. 与既有输入的边界（不得破坏）

- `Tap → Jab2 → Jab3 → C4` 链路只新增边，不改已有边；未蓄满松手仍走 `Tap` 分支（连段衔接不变）。
- 未配置 Hold 边的其他节点：`comboChargeActive` 下的松手继续被视为"无匹配边"，不回落站桩蓄力。
- 蓄力事件链 5 个订阅方不断链；DoT 不卡肉、卡肉不冻结玩家等既有约束照旧。
- QTE / 路线转场 / 三选一面板期间状态机停手并清缓冲（既有机制）。

---

## 7. 实现清单（待授权后执行）

| 文件 | 改动 |
|---|---|
| `Assets/ScriptableObjects/Moves/Zhangfei/Zhangfei_StabR2.asset` | 新建（§5） |
| `Assets/ScriptableObjects/Moves/Zhangfei/Zhangfei_Jab1.asset` | 加 Hold 边 + 蓄力保持与指向偏摆参数 |
| `Assets/Scripts/Player/InputManager.cs` | 改动 1（松手投递）、改动 2（横向改列不清蓄力） |
| `Assets/Scripts/Player/AttackSystem.cs` | 新增"按传入 config 生成指示路径"的 API（现 `TryGetPierceIndicatorPath` 写死取 Pierce 配置）；`ExecuteStab` 的命中击退改为读招式 `pushBackRows`（§4.5，暂取与升级值的较大者） |
| ~~`Assets/Scripts/Effects/StabAimMarker.cs`~~ | 已删除（用户否决，见 §6） |
| `Assets/Scripts/Attack/StabSweepEffect.cs` | 蓄力姿态加"指向偏摆 + 沿指向推拉"；释放段加"归零过冲"；命中时触发**独立计时**的枪体震动（§4.4，不被卡肉冻结） |
| `Assets/Scripts/Attack/StabMotionParams.cs` | 透传震动参数（`StabMotionParams.FromConfig` 增字段） |
| `Assets/Scripts/Core/AttackSkillConfig.cs` | 新增 `chargeHoldYawDegrees`、`chargeHoldYawSmoothSeconds`、`stabRedirectSnapRatio`、§4.4 的震动参数组、`pushBackRows` |

不改动：命中判定、手势识别主流程、`ColumnManager` 阵型/位移 API、既有 C4 资产，以及其他招式已有的位移逻辑（Slash 方向性击退、Launch 击飞、聚拢/牵引）。

---

## 8. 有意的省略与待确认项

- **释放只支持松手**（本次选定）。是否也要允许用滑动释放（那会给戳击带来"列号 = -1"的问题，需额外补列）暂不做。
- **目标列空列时**：`GetStabColumnFromScreenPosition` 的"超过半列宽返回 -1"保护会使相邻列取不到列；标记是否需要"吸附到最近列"待实机确认手感后再定。
- **驻留上限**：`holdWaitMaxSeconds = 1.2s` 与"一直按住蓄力"的手感关系未实测。
- **两排命中反馈强度**是否够/过头，需实机。
- **震动幅度/频率/时长**必须实机对着战斗画面调：手机上像素级抖动很容易过头，也可能因为 PPU/缩放看不见。
- **C2 击退与升级"击退波"的组合规则**（§4.5 的 A/B）待你确认；未定前按 A（取较大值）。
- **"招式附带位移"的统一改动**是你已声明的后续计划（把各招式位移收到配置上）；本轮只加 `pushBackRows` 并只给 C2 用，**不重构**其他招式，避免顺手改掉既有手感。
- **三连击（C1 的 Jab1→Jab2→Jab3）缺力量感**是用户本轮提出的观察，但**不在本链范围内**：本轮只给 C2 加震动，不顺手改既有手感。若要一并处理，另开一轮（例如每段加更短更轻的命中震动 + 分段递增）。

---

## 9. 验收标准

可自动 / Play Mode 状态机级：

1. `Jab1` 进行中按住 ≥ 0.3s → 枪体拉回蓄势位（状态机调试面板显示"蓄力保持 = 是"），松开后节点时钟继续。
2. 松手 → 解析到 `Zhangfei_StabR2`（`LastMoveConfig.name`），日志出现 `戳击 列N 射程:2`。
3. 松手列 = 按住期间最后指向的列（改变指向后松手，`N` 随之改变）。
4. 未蓄满松手 → 不触发新招式；窗口内点击仍正常接 `Jab2`（回归）。
5. 释放瞬间场上只有一把枪（无第二把蓄力枪、无残留）。
6. 命中第 1 排触发枪体震动，且**不被卡肉冻住**（震动应在卡肉窗口内跑完，随后枪体继续穿入/回收）；第 2 排以 0.6 倍触发；**空挥无震动**。
7. C2 命中后：被击退敌人沿 row 后退 1 格，停顿后**回到自己的原槽**；Boss / 骑乘骑兵不动；处于 C技霸体的敌人不动；击退造成的临时空槽**不触发普通补齐**。

必须实机试按：

- 蓄力—指向—释放的整体节奏与读数；横向改列的灵敏度（改动 2 的阈值）。
- 两排命中的"穿透感"与卡肉/镜头力度；标记在正常游戏比例下是否清晰。
- C2 命中震动的"力量感"是否够、是否过头（要与三连击对比着看）。
- C2 的击退 1 格与卡肉/震动的组合读感（"顶退"是否成立），以及两排同时命中时是否出现跨排或叠推异常。
- 动作节奏（见 §6）：拉回 → 戳出 → 慢慢收回 的对比是否读得出来。

---

## 6. 实机反馈后的修订（本轮）

用户实机验收后的三处改动：

1. **移除瞄准标记**：`StabAimMarker` 组件、场景对象、`AttackSystem.TryGetStabIndicatorPath` 全部删除。
2. **蓄力可以按多久就等多久**：`PlayerMoveStateMachine.IsWaitingForHold` 的驻留上限（1.2s）现在**只针对「没有蓄力后继」的节点**；有蓄力后继（本链的 C1，以及既有的 C4 三下→上挑）时不再有上限。原因：超过上限会让本段先收尾回中立，松手时连招直接丢失、什么都不发生（用户实测反馈，已复现：节点已结束时松手 `executedCount 1 → 1`；对照节点进行中松手 `1 → 2` 且出 `Zhangfei_StabR2`）。实测：Jab1 停驻 2000 次不触发上限；Jab2（无蓄力后继）仍在 1.2s 触发上限；C4 链路回归通过（`执行 Zhangfei_LaunchFinisher`）。
3. **C2 动作节奏**：`actionDuration` 0.5 → **0.66**；`stabWindupRatio` 0.18 → **0.26**；`stabWindupHoldSeconds` 0.05 → **0.08**；`stabThrustRatio` 0.26 → **0.15**；`stabPenetrationRatio` 0.08 → **0.05**。实际分段：拉回 0.172s → 蓄势停顿 0.08s → 戳出 0.099s → 穿入 0.033s → 收回 0.276s（戳出:收回 = 1:2.8）。同时 Jab1 蓄力：拉回 0.75 → **0.85**、拉满时长 0.3 → **0.4s**、预压 8° → **10°**。

4. **横移换列不再吞掉松手**：`InputManager.TryConsumeLiveGesture` 以前在蓄力等级 ≥1 时把滑动**无条件当作「已消费」**（`ProcessSwipeGesture(...)` 后直接 `executed = true`），而 `ProcessSwipeGesture` 不看状态机是否真的出招。于是按住蓄力时快速横划（≥50px、0.25s 内）被判成滑动招式 → `ResetSegment` 清掉蓄力分段并置 `hasTriggeredDuringHold` → 松手被 `if (hasTriggeredDuringHold) return;` 吞掉（或降到「未蓄力滑动」而无匹配边）→ 什么都不发生。现在改为 `executed = ProcessSwipeGesture(...)`（该节点没有滑动边就不算消费）；`ProcessSwipeGesture` 改成返回 bool。实测：横划后 `isLongPress=True`、`triggered=False`，松手 `executedCount 1 → 2` 出 `Zhangfei_StabR2`；C4 的「蓄力竖划终结技」不受影响（实测仍出 `Zhangfei_LaunchFinisher`，`triggered=True`）。
5. **指向偏摆平滑**：新增 `chargeHoldYawSmoothSeconds`（Jab1 = 0.12s），偏摆从「瞬间跳到新角度」改为按时间常数逐步逼近（实测 1.35° → 5.81° → 7.28° → 7.99°），消除换列时的跳变。根因：目标列是按存活敌人屏幕投影量化出来的，边界处会在相邻列之间跳。

**仍未定**：`ThornArmorEffect` 是否在连段蓄力中让位（蓄力减伤/反伤盾的授予逻辑在 `PlayerState`，本轮未改动）。
