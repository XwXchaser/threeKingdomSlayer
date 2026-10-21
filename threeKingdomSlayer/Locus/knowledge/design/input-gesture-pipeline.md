---
id: kd_8cb4c248-969f-4947-81a0-113e60196ac6
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_5e1b7d34-9a2c-4f6e-b8d1-7c93e2f4a610
injectMode: inherit
summary: 玩家输入手势的完整链路（手指→手势→招式→出招）、手势分类与扩展路径（加新手势改哪几处）、以及内核与输入层之间的端口设计。
aiEditMode: inherit
---

# 玩家输入手势：链路、扩展路径与分层端口

> 本文是**动 `InputManager` 之前**的调研结论（2026-10-22，asmdef 分层第九批的前置）。
> 目标：把「输入」这件事的边界讲清楚 —— 哪些属于输入层、哪些属于战斗内核、
> **以后加新手势要改哪几处**。

## 1. 完整链路（手指 → 出招）

```
手指 / 鼠标
   ↓
InputManager.Update（每帧）
   ├─ 门控：GameplayInputGate（全局拦截）/ timeScale==0 / blockInputFrames
   ├─ 有触摸则走触摸，否则走鼠标（互斥，避免双触发）
   ├─ 按下：记录 segment（起点 / 起始时间）→ OnChargeBegan
   ├─ 按住中：
   │    • 长按判定：按住 ≥ longPressDuration 且纵向漂移 ≤ 容差 → isLongPress
   │    • 蓄力进度 = 按住时长 / minChargeTime → isCharged / CurrentChargeLevel / OnChargeUpdated
   │    • 速度门控的划动检测（TryDetectHoldSwipe）：瞬时速度达标才累计距离与时长
   │      → 达标即 TryConsumeLiveGesture（出招）
   └─ 松手：ProcessGesture —— 一条 **gate 链**（每条 gate 返回 true = 已消耗）
        ① 教学手势过滤（不是目标手势就 drop）
        ② QTE 输入消费（严格模式 / 普通 QTE，吃掉输入避免误触）
        ③ 本段按住期间已出过招（松手不再出招）
        ④ combo 链释放（连段蓄力由招式状态机裁定）
        ⑤ 默认路径：蓄力→长按穿刺 / 滑动→按角度分类 / 否则→戳击
             ↓ SubmitGesture(GestureInput)
        GestureInput { gesture, charged, chargeLevel, targetColumn, slashLeftToRight, slashVisualTilt, timestamp }
             ↓
        PlayerMoveStateMachine.SubmitInput（招式表模式）
             ├─ 直通模式：ExecuteDirect（行为与改造前一致）
             └─ 招式表模式：按「窗口 + 边」解析（MoveTableConfig.roots / 边 / 窗口）
             ↓ AttackType
        AttackSystem.TryExecuteAttack(...)
             ↓ 招式执行器注册表（RegisterAttackExecutor：Stab/Slash/Pierce/Sweep/Launch/Parry）
        实际出招 → OnAttackExecuted(attackType, column)
```

**关键分层**：
- **识别层**（上层）：把手指点分类成 `MoveGesture`、算蓄力、消费 QTE / 教学输入 ✅ —— 全在 `InputManager` 里 ✅。
- **消费层**（内核）：`PlayerMoveStateMachine`（招式表解析）+ `AttackSystem`（执行）✅。
- 两层之间只隔一个 **`GestureInput` 数据结构** ✅（在数据层 `Locus.Data` ✅）。

## 2. 手势分类与默认落点

`MoveGesture`（数据层）：`Tap` / `Hold` / `SwipeVertical` / `SwipeHorizontal` / `SwipeDiagonal` ✅。

`MoveGestureDefaults.ResolveAttackType(gesture, charged)`（数据层）✅：

| 手势 | 未蓄力 | 已蓄力 |
|---|---|---|
| Tap | Stab | Stab |
| Hold | Pierce | Pierce |
| 竖滑 | Parry | Launch |
| 横滑 | Slash | Sweep |
| 斜滑 | Slash | Slash |

> 设计约束：**分叉轴只有「是否蓄力」** ✅，方向只在蓄力时区分招式 ✅（注释里明确写了「不得改成按方向分叉」✅）。

## 3. 以后加新手势，改哪几处（5 处）

| # | 位置 | 改什么 | 层次 |
|---|---|---|---|
| 1 | `MoveGesture` 枚举 | 加一个值 | 数据层 ✅ |
| 2 | `InputManager.Gesture.cs` 的**三个产出点**：`ProcessTapGesture` / `ProcessLongPressGesture` / `ProcessSwipeGesture` | 识别出新手势并投出 `GestureInput` | 输入层 ✅ |
| 3 | `MoveGestureDefaults.ResolveAttackType` | 新手势的**默认**落点（招式表没配时的兜底） | 数据层 ✅ |
| 4 | `MoveTableConfig.roots` | 中立态入口招式（**数据驱动** ✅，不用改代码 ✅） | 数据层 ✅ |
| 5 | `TutorialGestureTarget` + `TutorialAllows*` | 若新手势也要能被教学关教 | 输入层 ✅ |

**要点**：
- **招式表是数据驱动的** ✅ —— 新手势通常只要加一条 `MoveEntry` ✅；
- **识别层在上层** ✅ —— 加新手势**不需要动战斗内核** ✅；
- 内核**从不消费 `MoveGesture`** ✅ —— 它只执行 `AttackType` ✅ ⇒
  **手势词汇表可以自由扩展** ✅，不会牵动内核 ✅。

## 4. 内核到底从输入层要什么（实测 34 处，5 个文件）

| 成员 | 谁用 | 读/写 | 性质 |
|---|---|---|---|
| `OnChargeBegan` / `OnChargeUpdated` / `OnChargeEnded` | `ChargeStabVisual`、`PierceAimIndicator`、`ThornArmorEffect` | 订阅 | 蓄力过程事件 |
| `OnAttackExecuted` | `ChargeStabVisual` | 订阅 | 出招事件 |
| `comboChargeActive` | `ChargeStabVisual`(3)、`PlayerMoveStateMachine`(读写) | 读+写 | 连段蓄力标记 |
| `restrictChargedToVertical` | `ChargeStabVisual`(3)、`PierceAimIndicator`(2) | 读 | 教学期开关 |
| `minChargeTime` / `longPressDuration` | `ChargeStabVisual` | 读 | 蓄力时长参数 |
| `skillInputEnabled` | `UltimateEffect_Berserk` | **写** | 狂化期间禁技能输入 |
| `blockInputFrames` | `UpgradeChoiceManager` | **写** | 三选一后挡 2 帧防误触 |
| `IsPointerDown` / `HoldDurationSeconds` / `CurrentPointerStabColumn` | `PlayerMoveStateMachine` | 读 | 连段蓄力保持 / 指向列 |

**结论**：内核要的是「**输入状态 + 输入事件**」✅，**不要「手势识别逻辑」** ✅
⇒ 分层不会把新手势挡在门外 ✅。

## 5. 分层端口设计（第九批实施依据）

原则：**端口是「意图」而不是「字段镜像」** ✅ —— 字段一改，端口不必跟着改 ✅。

```csharp
// 数据层（Locus.Data）
/// 内核看到的输入端口：只描述「输入状态 + 输入事件」，不含手势识别逻辑。
public interface IPlayerInput
{
    int CurrentChargeLevel { get; }
    bool IsPointerDown { get; }
    float HoldDurationSeconds { get; }
    int CurrentPointerStabColumn { get; }
    bool ComboChargeActive { get; set; }
    bool RestrictChargedToVertical { get; }
    float MinChargeTime { get; }
    float LongPressDuration { get; }
    bool SkillInputEnabled { get; set; }
    void BlockInputForFrames(int frames);
}
```

事件（蓄力 / 出招）也放数据层 ✅，由输入层 raise ✅、内核订阅 ✅
（与 `GameplayInputGate` 同一套「上层登记、下层使用」的路子 ✅）。

**为什么这样切**：
- 内核换成端口后，`InputManager` 整体留在**上层** ✅（它是输入适配器 ✅），
  它的 Inspector 阈值参数**完全不动** ✅（prefab / 场景零改动 ✅）；
- 内核只拿到「状态 + 事件」✅ → **新手势的扩展路径完全不受分层影响** ✅（第 3 节那 5 处 ✅）。
- 端口方法用**语义命名**（`BlockInputForFrames(int)` 而不是暴露一个计数器字段 ✅），
  将来输入层换实现（新手势识别器 / 摇杆 / 手柄 ✅）时，端口不用动 ✅。

**风险与验证点**：
- 三个表现组件（`ChargeStabVisual` / `PierceAimIndicator` / `ThornArmorEffect`）订阅的是**蓄力事件** ✅ —— 
  端口化后要确认「蓄力指示器 / 蓄力武器视觉 / 尖刺甲」的表现链路没断 ✅（⚠️ 这三处是**表现** ⚠️，Play Mode 难以自动断言 ⚠️ → 需人工过一遍 ✅）。
- `PlayerMoveStateMachine` 的 `comboChargeActive` 是**写**的 ✅ → 端口要保留写能力 ✅。
- `InputManager` 的 `Instance` 在多处被空判 ✅ → 端口解析不到时按「无输入」安全降级 ✅。

## 6. 实施结果（2026-10-22，第九批）

- 契约层新增 `IPlayerInput`（状态 + 事件 ✅）+ `PlayerInputHub`（持有者 ✅）。
- `InputManager`（仍在**上层** ✅，Inspector 参数一个没动 ✅）实现该端口 ✅、在 `Awake` 登记 ✅。
- 内核 6 个文件改成走端口 ✅：`ChargeStabVisual` / `PierceAimIndicator` / `ThornArmorEffect` /
  `PlayerState` / `UltimateEffect_Berserk` / `UpgradeChoiceManager` / `PlayerMoveStateMachine` ✅。
- **内核不再引用 `InputManager` 类型** ✅（只剩注释里的字样 ✅）。

**两个实现坑**：
1. C# 里**公开字段不能实现接口的 event / property** ✅ —— `InputManager` 的事件是公开委托字段 ✅，
   所以在类里写了**显式接口实现** ✅（转发到同一份委托 ✅，上层用公开字段、内核用接口 ✅，同一份 ✅）。
2. `PlayerMoveStateMachine` 的 `inputManager` 是**序列化字段** ✅ —— 改成接口类型 Unity 存不了 ✅；
   实际场景里它**从来没被赋过值** ✅（靠 `FindObjectOfType` 兜底 ✅），
   所以换成 `[NonSerialized]` 的端口字段 ✅（不丢任何东西 ✅）。

**验证**（Play Mode）：
- 端口已登记（`PlayerInputHub.Current = InputManager`）✅
- **蓄力事件经端口流到内核**：触发 `OnChargeBegan` → `PlayerState.IsCharging` 由 false 变 true ✅ →
  触发 `OnChargeEnded` → 变回 false ✅（这验的是「事件通道真的通」✅）
- 端口的写能力：`SkillInputEnabled=false` 输入层读到 false ✅；`BlockInputForFrames(2)` 输入层读到 2 ✅
- 端口的读能力：蓄力等级 / 按住中 / 最小蓄力 / 长按时长 / 只许竖滑 / 指向列 全部可读 ✅
- §15.5 基线逐位一致 ✅；Console 0/0 ✅

**待人工确认**（自动测不到 ✅）：蓄力指示器（`ChargeIndicatorController` ✅）、蓄力武器视觉（`ChargeStabVisual` ✅）、
穿刺猫准（`PierceAimIndicator` ✅）、尖刺甲（`ThornArmorEffect` ✅）这四个**表现**在新端口下是否照旧 ✅。
