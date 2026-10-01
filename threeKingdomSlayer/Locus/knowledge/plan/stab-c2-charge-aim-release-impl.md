---
id: kd_bff529ad-5232-44ac-8dd0-7e68dbaa24de
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# C2 实现方案（可执行步骤）

配套设计：`design/stab-charge-aim-release.md`（§4.4 命中震动、§4.5 命中位移）。本文档只讲**怎么改、改哪些文件、每步怎么验、怎么回退**。

**当前状态：M1 + M2 已实现并提交，Step 8（场景接线与让位）也已随 M2 完成；剩余 = 实机验收与调参。**
提交：`1801c4cf`（M1 机制）、`1ca69299`（M2 表现）。

---

## 0. 范围与已定口径

- 目标：`C1（第一下 stab）→ 按住蓄力 → 松手 →` 在松手所指列打一记 `rangeRows = 2` 的单列戳击（**C2**），命中带**枪体震动**与**击退 1 格**，C2 **串尾**。
- 已定：释放=松手；允许按住期间横向改列；位移与升级"击退波"的组合按 **规则 A = 取较大值**（用户已确认，不再讨论）。
- 不做：不改命中判定、手势识别主流程、`ColumnManager` 位移 API；不重构 Slash 方向性击退 / Launch 击飞 / 聚拢；不动既有 C4 资产与既有 6 个技能资产；三连击缺力量感另开一轮。

---

## 1. 里程碑

| 里程碑 | 步骤 | 产出 | 验收 |
|---|---|---|---|
| **M1 机制** | Step 1–5 | 能蓄力、能松手出 C2、range 2、单列、击退 1 格（表现朴素） | Play Mode 状态机级断言全过 |
| **M2 表现** | Step 6–7 | 指向偏摆、两排标记、归零过冲、命中枪体震动 | 编译 + 状态机级 + 录屏目视 |
| **M3 接线调参** | Step 8 | 场景接线、让位修正、手感数值 | 你实机试按 |

---

## 2. 步骤明细

### Step 1 — `AttackSkillConfig` 新增字段（`Assets/Scripts/Core/AttackSkillConfig.cs`）

**回归铁律：全部新字段默认 0（= 不生效）**，只有新资产显式赋值。这样 Jab1/2/3、C4、6 个技能资产的行为零变化。

```csharp
[Header("蓄力指向（C2 前置节点用）")]
[Range(-20f, 20f)] public float chargeHoldYawDegrees = 0f;   // 蓄力时朝目标列的偏摆上限（写在【当前节点】上）
[Range(0f, 1f)]   public float stabRedirectSnapRatio = 0f;   // 释放段内偏摆归零+过冲所占比例（写在【C2】上）

[Header("命中震动（C2）")]
public float hitShakeAmplitude = 0f;      // 沿枪轴轴向回弹（世界单位）
public float hitShakeLateral = 0f;        // 垂直枪轴抖动
public float hitShakeRollDegrees = 0f;    // 绕枪身长轴滚转
public float hitShakePitchDegrees = 0f;   // 枪尖俯仰点头
public float hitShakeDuration = 0.1f;
public float hitShakeFrequency = 18f;
[Range(0f, 1f)] public float hitShakeSecondRowScale = 0.6f;

[Header("命中位移")]
[Min(0)] public int pushBackRows = 0;     // 每次命中施加的击退排数（0 = 不击退）
```

**参数归属**（容易放错，明确写死）：`chargeHoldYawDegrees` 属于**蓄力阶段**，写在做前置的 `Jab1` 上；`stabRedirectSnapRatio` / `hitShake*` / `pushBackRows` 属于**C2 自己的执行**，写在 C2 资产上。

### Step 2 — 新建 `Assets/ScriptableObjects/Moves/Zhangfei/Zhangfei_StabR2.asset`

复制 `Zhangfei_Jab1.asset` 作为模板（保持 `damageType` 与 `attackWavePrefab` 引用一致），按下表赋值：

| 字段 | 值 | 说明 |
|---|---|---|
| `id` | 15 | 现有最大 14（LaunchFinisher） |
| `attackType` | `Stab (0)` | 走 `ExecuteStab`，天然单列 |
| `rangeRows` | **2** | 本列前 2 排 |
| `damage` | 35 | 段位递进（20/26/34 → 35，可后调） |
| `cooldown` / `actionDuration` | 0.3 / **0.5** | 动作锁模式下以动作锁为准 |
| `attackWavePrefab` | 同 Jab1 | 缺它 `ExecuteStab` 直接返回 false |
| `stabSpawnYOffset` / `stabSpawnZOffset` | −0.6 / −5 | 同 Jab1 |
| `stabVisualReachOffset` / `stabVisualTargetRandomRadius` | 2 / 0.35 | 比 Jab1 的 0.5 收一点，重刺更准 |
| `stabWindupRatio` / `stabThrustRatio` / `stabPenetrationRatio` | 0.18 / 0.26 / 0.08 | 起手更明显、穿刺更快 |
| `stabThrustLengthScale` / `stabThrustWidthScale` | 1.26 / 0.88 | 力度上档 |
| `stabBlurThrust` / `SpeedFrame` / `Penetration` | 30 / 16 / 20 | 同量级加强 |
| `stabRollDegrees` | 4 | 立体感 |
| `stabFirstHitStrength` | **3 (Heavy)** | 蓄力释放更重（枚举：None0/Light1/Standard2/Heavy3） |
| `stabRedirectSnapRatio` | **0.2** | 释放段前 20% 偏摆归零 + 过冲 |
| `hitShake*` | 0.06 / 0.02 / 3 / 1.5 / 0.1 / 18 / 0.6 | §4.4 初值 |
| `pushBackRows` | **1** | 击退 1 格 |
| `chargeHold*` | 0 | C2 自身无蓄力后继 |
| `ultimateEnergyGain` | 5 | 压低，避免连段刷能量 |
| `poiseDamage` / `interruptsCavalryCharge` | 0 / false | 打断骑兵靠 `charged` 自动成立（见 Step 5） |
| `repeatSelf` / `moveEdges` | 0 / 空 | **串尾** |

### Step 3 — `Zhangfei_Jab1.asset` 加边 + 蓄力保持

| 字段 | 现状 | 改为 |
|---|---|---|
| `moveEdges`（新增一条，保留原 Tap 边） | 仅 Tap→Jab2 | 追加：`gesture = Hold (1)`、`minChargeLevel = 1`、`overrideWindow = 1`、`windowStart01 = 0.55`、`windowEnd01 = 1.0`、`next = Zhangfei_StabR2` |
| `chargeHoldRetractRatio` | 0.2 | **0.75**（否则按住时枪体几乎不回、读不出蓄力条） |
| `chargeHoldPullSeconds` / `PitchDegrees` | 0 / 0 | 0.3 / 8（与 Jab3 同值） |
| `chargeHoldSettleSeconds` / `SettleRatio` | 0 / 0 | 0.12 / 0.1 |
| `chargeHoldShakeAmplitude` / `Frequency` | 0 / 0 | 0.06 / 14 |
| `chargeHoldYawDegrees`（新增） | — | **10** |
| `repeatSelf` | 0 | 保持 0（不用默认自环，避免与显式边抢解析） |

### Step 4 — `InputManager.cs` 两处

**(a) 松手不再被吞**（`ProcessGesture`，现约 546–549 行）：

```csharp
if (comboChargeActive && CurrentChargeLevel >= 1)
{
    // 只在“招式仍在进行”时投递：节点已结束时维持原行为（绝不回落站桩蓄力）
    if (moveStateMachine != null && moveStateMachine.IsMoveActive)
    {
        int holdColumn = GetStabColumnFromScreenPosition(releasePos);
        if (holdColumn >= 0)
            SubmitGesture(MoveGesture.Hold, CurrentChargeLevel, holdColumn);
    }
    return;
}
```

- `IsMoveActive` 守卫是关键：驻留超上限（1.2s）后节点已收尾，松手仍然什么都不发生（与今天一致）。
- 顺带好处：蓄力后横向移动超过 `swipeThreshold` 再松手时，会走这条提前返回，不会被误判成"滑动"。

**(b) 允许按住期间横向改列**（两处同样的判定：`Update` 内约 235–239 行、`TryDetectHoldSwipe` 内约 496–498 行）：

```csharp
// 横向位移不再清零蓄力：只限制纵向抖动；横向移动视为“指向”（改列）
Vector2 delta = currentPointerPos - segmentStartPos;
bool withinChargeTolerance = Mathf.Abs(delta.y) <= chargeMovementTolerance;
```

- 其余门控不动：`TryDetectHoldSwipe` 仍需 `instantSpeed >= minSwipeSpeed` 才开始追踪横向滑动，所以"慢慢挪手指"不会进入滑动路径、也不会 `ResetSegment` 清空蓄力计时。
- 已知边界：若将来给 `Jab1` 也配滑动边，高速滑动会抢走这次输入（当前不会，因为只配了 Hold 边）。

### Step 5 — `AttackSystem.cs`

1. 新增位移取值（规则 A）：

```csharp
private int GetEffectivePushBack(AttackSkillConfig cfg)
{
    int movePush = cfg != null ? Mathf.Max(0, cfg.pushBackRows) : 0;
    int upgradePush = UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetPushWaveDistance() : 0;
    return Mathf.Max(movePush, upgradePush);   // 规则 A：取较大值（用户已定）
}
```

2. `ExecuteStab` 命中回调内的 `pushDist` 来源换成 `GetEffectivePushBack(cfg)`；`ApplyPushWave(..., canInterruptCFrame: false, pushedTargets)` 与 `PostDisplacementFillUp(pushedTargets)` 逻辑**原样不动**。`ApplyStabPushWave` 同样改用它，保持两条路径一致。
3. 新增指向路径 API（供标记用，现 `TryGetPierceIndicatorPath` 写死取 Pierce 配置）：

```csharp
public bool TryGetStabIndicatorPath(AttackSkillConfig cfg, int columnIndex,
    List<Vector3> pathPoints, out int effectiveRows)
```

- 算法照搬 `TryGetPierceIndicatorPath`，但用**传入的 cfg**、行数用 `GetEffectiveRangeRows(cfg)`；末点用 `cfg.stabVisualReachOffset` 算，不依赖 Pierce 专用视觉路径。
4. 打断口径无须改：`InterruptsCavalryCharge(cfg)` = `cfg.interruptsCavalryCharge || _attackIsCharged`，而 C2 由蓄力释放（`chargeLevel ≥ 1` → `charged = true`）→ **C2 自动能打断骑兵冲锋**（需实机确认一次）。

### Step 6 — `StabMotionParams.cs` + `StabSweepEffect.cs`

`StabMotionParams` 增字段并让 `FromConfig` 透传：`yawDegrees`、`redirectSnapRatio`、`shakeAmplitude`、`shakeLateral`、`shakeRollDegrees`、`shakePitchDegrees`、`shakeDuration`、`shakeFrequency`、`shakeSecondRowScale`。

`StabSweepEffect` 三处：

1. **蓄力姿态**：在现有 `chargeHoldPitchDegrees` 之外，按 `_column`（目标列）与玩家列的相对位置叠加 `chargeHoldYawDegrees` 的偏摆；微颤基向量从"枪身长轴"改为"指向方向"（其余不动，仍为纯前后、垂直分量 0）。
2. **释放段**：刺出段前 `redirectSnapRatio` 比例内把偏摆归零并过冲 −3°，枪尾同时沿指向方向做一小段位移。
3. **命中震动**（核心实现约定）：
   - 在 `CheckHits` 命中处调用 `TriggerHitShake(enemy.rowIndex == 0 ? 1f : shakeSecondRowScale)`（`PauseSequenceForHitStop` 的位置旁，同一处触发）。
   - 用**独立计时**驱动（`Time.unscaledTime` 或 `SetUpdate(true)` 的独立 tween），**不能挂进被 `seq.Pause()` 冻住的主序列**。
   - 每帧在 `LateUpdate` 用"**先减上一帧偏移、再加本帧偏移**"的方式写回 `_visualOffsetRoot` 的 `localPosition`/`localRotation`；**不动 scale**（避免与拉伸/形变冲突），旋转每帧从基准重算（不做累积）。
   - 衰减：4–6 次指数衰减，幅度按 `1 → 0` 随时间收敛。

### Step 7 — 新组件 `Assets/Scripts/Effects/StabAimMarker.cs`

| 项 | 约定 |
|---|---|
| 挂点 | 场景 `Player` 下新建空节点 `StabAimMarker`（Inspector 暴露参数） |
| 订阅 | `InputManager.OnChargeBegan / OnChargeUpdated / OnChargeEnded` |
| 可见条件 | `InputManager.comboChargeActive == true`（与 `ChargeStabVisual` / `PierceAimIndicator` 的让位条件正好互补） |
| 列映射 | `InputManager.GetPierceColumnFromScreenPosition(pos)`（已有 public API；列变化 → 标记 80ms 滑过去，旧的 60ms 淡出） |
| 位置 | `AttackSystem.TryGetStabIndicatorPath(c2Config, column, points, out rows)`，用 `points[1]` / `points[2]` 放第 1 / 第 2 排标记 |
| 视觉 | 两个程序化 Quad（菱形/十字）+ 第 2 排 0.6×/alpha 0.45；枪尖→第 1 排的细流光带（宽 0.08，随进度 0.4→1.0）；外圈进度环（蓄满转呼吸脉冲） |
| 排序 | 跟随现有像素特效排序层，避免被地面/敌人遮挡 |
| 素材 | 不新增美术：纯色 Quad + 现有材质；若要更好看再单独出图 |

### Step 8 — 场景接线与让位

1. `StabAimMarker` 组件挂好，Inspector 指定 C2 资产引用（或由 `PlayerState.heroConfig.moveTable` / 当前节点解析，二选一，倾向于显式引用，便于排查）。
2. `ChargeIndicatorController`（`Assets/Scripts/UI/ChargeIndicatorController.cs`）增加让位：在 `OnChargeBegan` / `OnChargeUpdated` 开头加 `if (InputManager.Instance != null && InputManager.Instance.comboChargeActive) { indicatorRoot.gameObject.SetActive(false); hasAppeared = false; return; }`（照 `PierceAimIndicator` 同款模式）——**顺手修掉 `plan/combo-progress-and-next.md` §7.7-1 的老问题**。
3. 场景保存（Edit Mode 内完成，Play Mode 改动不落盘）。

---

## 3. 验证

**编译层**：`unity_recompile` 无新增错误 + Console 0 error / 0 warn。

**Play Mode 状态机级**（用 `unity_execute` 脚本化，先不依赖真实触摸）：

| # | 断言 |
|---|---|
| 1 | 直接 `SubmitInput(Tap)` → `Jab1` 执行；随后 `SubmitInput(Hold, chargeLevel=1, column=3)` → `LastMoveConfig.name == Zhangfei_StabR2`，日志出现 `戳击 列3 射程:2` |
| 2 | 列取自传入手势：换成 `column=1` 时日志列随之变化 |
| 3 | 未蓄满（`chargeLevel=0`）投递 Hold → "无匹配边"，不执行、不改变节点 |
| 4 | 窗口内 `Tap` 仍解析到 `Jab2`（回归） |
| 5 | 释放瞬间场上只有一把戳击枪体（无残留、无第二把） |
| 6 | 蓄力保持：按住期间 `PlayerMoveStateMachine` 显示"蓄力保持=是"，枪体回收比例随按住时长增大 |
| 7 | 命中震动不被卡肉冻住（需要敌人在场；可在测试里临时生成 1 个敌人，观察 `_visualOffsetRoot` 在卡肉窗口内仍在振荡） |
| 8 | 击退：被命中敌人 `rowIndex +1`，停顿后回到原槽；`isBoss` / 骑乘骑兵不动；CFrame 敌人不动 |

**需你实机验收**：整体节奏（按下—指向—松手）、横向改列灵敏度、标记在真实战斗画面里的可读性、震动幅度是否过头、击退 1 格的"顶退"读感、两排同时命中有无异常。

---

## 4. 风险与回退

| 风险 | 应对 / 回退 |
|---|---|
| 放宽蓄力位移条件影响**站桩蓄力（穿刺）**手感 | 影响面 = "横向移动不再取消蓄力"；若实机不喜欢，把 (b) 改成"仅在 `comboChargeActive` 期间放宽"（一行 if），站桩蓄力恢复原判定 |
| 震动与 DOTween 抢同一 transform 导致抖动累积/突跳 | 固定"先减上一帧偏移再加本帧偏移"的写法；异常时把震动改为挂在独立子节点（回退方案 B） |
| `pushBackRows` 与升级击退波叠加 | 已定规则 A（取较大值），单点可控 |
| 蓄力超驻留上限（1.2s）后松手 | `IsMoveActive` 守卫保证不误出站桩蓄力（与今天一致） |
| 误伤既有连段 | 所有新字段默认 0，且 Jab2/Jab3/C4 资产不改；Jab1 只"新增一条边 + 填蓄力参数"，原 Tap 边不动 |
| 场景接线遗漏导致标记不显示 | 标记可见性只依赖 `comboChargeActive`，可用调试面板 + 日志确认事件链（5 个订阅方之一） |

---

## 5. 影响面与工作量估算

| 文件 | 类型 | 量级 |
|---|---|---|
| `Core/AttackSkillConfig.cs` | 改 | +18 行（字段 + Tooltip） |
| `ScriptableObjects/Moves/Zhangfei/Zhangfei_StabR2.asset` | 新建 | 1 个资产 |
| `ScriptableObjects/Moves/Zhangfei/Zhangfei_Jab1.asset` | 改 | 1 条边 + 6 个参数 |
| `Player/InputManager.cs` | 改 | +12 / ~6 行 |
| `Player/AttackSystem.cs` | 改 | +35 行（位移取值 + 路径 API） |
| `Attack/StabMotionParams.cs` | 改 | +12 行 |
| `Attack/StabSweepEffect.cs` | 改 | +90~120 行（偏摆/推拉/归零过冲/震动） |
| `Effects/StabAimMarker.cs` | 新建 | ~200 行（程序化标记 + 流光 + 进度 + 列切换） |
| `UI/ChargeIndicatorController.cs` | 改 | +6 行（让位） |
| `Assets/Scenes/Battle.scene` | 改 | 1 个节点 + 组件接线 |

合计：约 **400–500 行代码 + 1 个资产 + 1 处场景接线**，拆成 M1（机制）/ M2（表现）两次交付，每次都可单独验证与回退。

---

## 6. 进度与验证记录（滚动更新）

| 里程碑 | 状态 | 提交 |
|---|---|---|
| M1 机制 | ✅ 完成 | `1801c4cf` |
| M2 表现（含 Step 8 场景接线与让位） | ✅ 完成 | `1ca69299` |
| M3 实机调参 | ⏳ 待用户实机 | — |

### 已实测（Play Mode 状态机级 / 组件级）

| 项 | 结果 |
|---|---|
| C1 点击 → `Jab1` | ✅ |
| 蓄力投递（窗口外入缓冲、窗口内直接释放） | ✅ 两者都能到 `Zhangfei_StabR2`（`LastMoveDuration` 0.500） |
| 列号流入戳击执行 | ✅ `col=-1` 被拒；`col=2/4` 通过 |
| 未蓄满松手 | ✅ 无匹配边、不误出招 |
| 三连击回归（窗口内点击 → `Jab2`） | ✅ |
| 场上只有一把可见枪 | ✅ 2 实例 = 旧段隐藏（alpha 0）+ 新段可见 |
| 指向路径 API | ✅ rows=2、4 点（起点+两排+视觉终点）、按列变化 |
| 命中震动不被卡肉冻住 | ✅ 卡肉暂停序列期间 `ShakeRoot` 仍在振荡并衰减（独立 realtime 计时） |
| ShakeRoot 层级迁移 | ✅ `VisualOffsetRoot > ShakeRoot > DeformRoot`，无 world matrix 报错 |
| 蓄力指向偏摆 | ✅ 输入 8° 生效；输入 -20° 被资产上限 10° 夹到 -10°；回零正常；tilt 不受影响 |
| 瞄准标记 | ✅ 进度门槛后显示；Row0/Row1 与路径点一致；流光宽度随进度 0.06→0.08；换列 2→3 时平滑滑动；蓄力结束淡出隐藏 |
| 编译 / Console | ✅ 0 error / 0 warn |

### 实现期发现并修掉的问题

- **标记订阅时序**：`OnEnable` 可能早于 `InputManager.Instance` 就绪（`PierceAimIndicator` / `ChargeIndicatorController` 把订阅放在 `Start` 就是同一原因）→ 改为 `Start` + `Update` 重试的防御式订阅，已实测修复。

### 待实机（无法自动验证）

1. 真实触摸下的连段蓄力：枪体拉回 + 指向偏摆 + 标记 + 松手释放（含换列手感）。
2. 命中震动与击退 1 格（需敌人）：震动幅度是否过头、击退后是否精确回原槽、Boss/骑兵/霸体是否不动。
3. 数值调参：震动（0.06/0.02/3°/1.5°/0.1s/18Hz/0.6）、标记（0.6 尺寸 / 0.45 次排透明度 / 排序层）、偏摆上限 10°、C2 的 35 伤害 / 0.5s。
