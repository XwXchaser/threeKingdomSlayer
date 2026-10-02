---
id: kd_c8a85aec-124a-4370-af1a-30e14e63b4e0
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 张飞·枪突链（枪突-1/2/3 → 蓄力上挑收尾）

> **命名**：正式名「张飞·枪突链」；别名「枪突-3 + 蓄力 = **C4**」（无双口径）。命名规范与连招登记见 `design/combo-registry.md`。
> 本文档是本链的**权威记录**；`plan/combo-progress-and-next.md` §7 保留当时的实施记录与决策历史。数值以本文档（读自资产实测）为准。

**状态：已完成，实机可用。** 待改进见 §6。

相关文档：`design/combo-move-state-machine.md`（招式图与状态机总纲；§11.8–11.10 为落地记录，§18 为四条共享输入规则）、`design/combo-zhangfei-aim-stab.md`（另一条链：指向突刺链，与枪突-1 共用起步节点）、`plan/stab-combo-video-reference-workflow.md`（素材与参考图流程）。

---

## 1. 输入与链路

| 段 | 输入 | 资产（id） | 后继 |
|---|---|---|---|
| 枪突-1 | 点击 | `Zhangfei_Jab1`（11） | 点击 → 枪突-2；另有 `Hold + 蓄力` 边 → 指向突刺链 |
| 枪突-2 | 点击 | `Zhangfei_Jab2`（12） | 点击 → 枪突-3 |
| 枪突-3 | 点击 | `Zhangfei_Jab3`（13） | 串尾（不接受任何输入） |
| 枪突-终结 | 按住蓄力 → 上划（竖滑，需 `minChargeLevel 1`） | `Zhangfei_LaunchFinisher`（14） | 串尾 |

## 2. 节点与边

- 接续窗口：枪突-1/2 = **0.65–1.0**；枪突-3 = **0.60–1.0**（三段均开启 `overrideWindow`）。
- **枪突-1 同时是两条链的起步节点**：`Tap → 枪突-2`（本链）与 `Hold + minChargeLevel 1 → Zhangfei_StabR2`（指向突刺链）。
- 按住驻留：指针按住且本段有后继时，帧时钟停在最早窗口起点；**有蓄力后继的节点不受 1.2s 上限约束**（总纲 §18 规则 2）。
- 一次按住只出一记蓄力招式：终结技放出后必须抬手重按才能再蓄力（总纲 §18 规则 1）。

## 3. 落点与位移

- 终结技保留**挑飞**：击飞 1.2s + 全列 3 排 + `poiseDamage 80` + `interruptsCavalryCharge 1`（可打断骑兵 109 冲锋）。
- 枪突-1/2/3 与终结技**都不带自身击退**（`pushBackRows = 0`）；升级 `push_wave` 也不会给它们加击退（只给已带击退的招式加成，见 `design/combo-registry.md` §1.4）。

## 4. 表现

- 三段枪突走 `StabSweepEffect` 时间线（起手 → 刺出 → 穿入 → 回收），逐段动作参数见 §5；长轴自转落在精灵长轴。
- 连段交接：新段从上一段末态起步（位置/朝向继承），上一段改为隐藏 + 关闭命中，自行跑完销毁（不 Kill，避免销毁级联带走同帧新建的下一段）。
- 连段蓄力的枪体语言：按下后沿回收路径拉回（**位移即蓄力条**）→ 到位后再向后一顿 → 沿枪身长轴前后微颤；素材帧只做姿势档位（不复用蓄力视觉的进出场/跟手/帧闪）。
- 收尾：伤害与击飞仍由挑飞结算，动作语言换成「大幅朝左上的扫击」（`launchSweepMode`），并叠一层 slash 扫掠表现层（`SweepEffect`，目标为空 → 不判定、不伤害）。
- 让位：连段蓄力期间 `ChargeStabVisual` / `PierceAimIndicator` / `ChargeIndicatorController` 让位（`ThornArmorEffect` 尚未让位，见 §6）。

## 5. 参数表（读自资产实测）

| 参数 | 枪突-1 | 枪突-2 | 枪突-3 |
|---|---|---|---|
| `damage` / `actionDuration` | 20 / 0.45 | 26 / 0.40 | 34 / 0.60 |
| 接续窗口 `windowStart01–End01` | 0.65–1.0 | 0.65–1.0 | 0.60–1.0 |
| 预备 / 刺出 / 穿入比例 | 0.12 / 0.28 / 0.08 | 0.15 / 0.25 / 0.08 | 0.20 / 0.20 / 0.08 |
| 枪尾偏移 右/上/前 | 0 / 0 / 0 | −0.9 / +0.7 / −0.3 | +0.9 / −0.6 / −0.5 |
| 画面内姿态倾角 `stabVisualTiltDegrees` | 0° | 4° | 5° |
| 长轴自转 `stabRollDegrees` | 0° | −25° | −4° |
| 拉伸 `stabThrustLengthScale` / 模糊倍率 `stabMotionBlurScale` | 1.18 / 1.0 | 1.22 / 1.15 | 1.30 / 1.35 |
| 视觉前伸 `stabVisualReachOffset` | 2.0 | 2.8 | 3.6 |
| 首击卡肉 `stabFirstHitStrength` | 2（Standard） | 2（Standard） | 3（Heavy） |

连段蓄力参数（写在节点上）：

| 参数 | 枪突-1 | 枪突-3 |
|---|---|---|
| `chargeHoldRetractRatio` | 0.85 | 0.75 |
| `chargeHoldPullSeconds` | 0.4 | 0.3 |
| `chargeHoldPitchDegrees` | 10 | 8 |
| `chargeHoldSettleSeconds` / `SettleRatio` | 0.12 / 0.10 | 0.12 / 0.10 |
| `chargeHoldShakeAmplitude` / `Frequency` | 0.06 / 14Hz | 0.06 / 14Hz |
| `chargeHoldYawDegrees` / `YawSmoothSeconds` | 60 / 0.12（供指向突刺链） | — |

终结技 `Zhangfei_LaunchFinisher`：
`damage 60`、`poiseDamage 80`、`rangeRows 3`、`actionDuration 0.6`、`launchDuration 1.2`、`interruptsCavalryCharge 1`；
扫击模式 `launchSweepMode 1` + `Up/Left/Forward = 1 / 0.7 / 0.35` + `Rise/LeftShift = 1 / 1.8`；
`launchFlickAngle 70` / `launchFlickDuration 0.26` / `launchWindupDuration·Distance = 0.06 / 0.12` / `launchSkipWindup 0`；
`launchSideTilt 16`、`launchRiseHeight 1.1`、`launchAngleVariance 12`；
扫掠表现层 `slashSweepHalfWidth/Angle/Direction/MovementTilt/VisualTilt/Duration = 4.5 / 110° / 右→左 / −22° / 20° / 0.36s`。

## 6. 待改进（实机发现，未实施）

1. **终结技收尾斜率偏低**：应更像「上挑」（正上方偏左），现状偏横向大扫。建议值与推导见 `plan/combo-progress-and-next.md` §7.7-2。
2. **终结技卡肉要更强**：挑飞伤害走透明 `AttackWave`，其 `HitTarget` 已是最强的 `Heavy`（0.14s），要更强需新增一档并支持从招式资产透传力度（§7.7-3）。
3. **三段素材未分化**：三段共用 `Stab.prefab` / `stab_v13.png`；枪体换 3D 模型后 `stabRollDegrees` 可直接沿用，高速帧那套精灵替换可退休。
4. **`ThornArmorEffect`（反伤盾视觉）在连段蓄力中不让位**：仅当玩家持有该升级时才会看到它亮起（与另外三个蓄力视觉的让位策略不一致）。

## 7. 验收

- 已完成（实机可用）：点、点、点接续；第三段串尾不接受输入；蓄力拉回—停顿—微颤；上划释放出挑飞终结技；全程只有一把可见武器；**终结技放出后必须抬手重按才能再蓄力**。
- 待实机手感项：拉回—停顿—上挑的节奏、微颤幅度、预压角度、扫掠的"幅度感"、终结技的卡肉力度。
