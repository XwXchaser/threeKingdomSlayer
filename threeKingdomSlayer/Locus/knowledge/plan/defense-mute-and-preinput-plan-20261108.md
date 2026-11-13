---
id: kd_962700a0-5121-4016-85cd-cf63e2b7d6ad
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_defense_mute_preinput_20261108
summary: 防御键静音口径（除教防御外全静音）+ 防御键预输入（连段结束即转防御）的实施方案（2026-11-08，方案已出、代码未动，等落地指令）。含原始反馈记录的更正（2026-11-02「为什么会受击」是教学流程被误杀的 BUG，不是「这一拍必须能防御」的需求）、既有 mute 机制与攻击手势开关的实测清单、硬门/软门判定设计、改动清单与行号、验证口径、与隔壁 agent 改动的冲突面。
---

# 防御键：静音口径 + 预输入（方案稿 · 2026-11-08）

> 状态：**已落地**（2026-11-08，隔壁 agent 提交后动工）。
> 改动 4 个文件：`DefenseButton.cs` / `PlayerState.cs` / `TutorialStageDirector.cs` / `TutorialStageDirector.Hints.cs`。
> 编译通过（Console 0 error / 0 warn）；机制层已用 Play Mode 白盒验证（见 §8）。
> **未覆盖**：教学全流程里每一拍的静音表现 + 提前按住的手感 → 由用户实机验收（§8.3）。
> 冲突面：隔壁 agent 正在改 `TutorialStageDirector.Beats.cs / .Hints.cs / .cs`（指向突刺教学返工，删超时兜底）。
> 本方案把**需求 2（预输入）全部落在 `Assets/Scripts/UI/DefenseButton.cs`**（无人改动），
> 需求 1 的主体落在 `TutorialStageDirector.cs` 的 `BeatNeedsAttack`（隔壁未碰该函数），
> 只有「技能拍流程复核」一节必须等隔壁收工后再动。

---

## 0. 用户口径（本轮拍板）

| # | 口径 |
|---|---|
| 1 | **除教防御外全静音**（教学拍一律灰掉防御键，只有「教按防御键」那一拍可用） |
| 2 | 2026-11-02「为什么会受击」是**教学流程错误**的反馈，不是「这一拍必须能防御」的需求 → 旧记录里的缓解措施要撤销 |
| 3 | **三选一弹窗 + 游戏结束**时不能按防御（与攻击手势的开启关闭同一套口径） |
| 4 | 预输入：**玩家可以提前按住**；若正在进行的连段结束 → 转进防御（与既有预输入逻辑同族） |
| 5 | 预输入**只在松手 / 静音时取消**，不设超时 |

---

## 1. 记录更正：2026-11-02「为什么会受击」的真相

原文：`plan/tutorial-hint-handoff-20261102.md` §12 / §13。

- 用户原话是「**当前获得3选1技能后下一次敌人攻击没有冻结并提示我用技能打断**」+「**为什么会受击**」。
- 真凶（§12.2，堆栈取证）：`TutorialStageDirector.Update()` 的**出口光标清理**误杀教学拍
  → `StopHint()` 把当前拍一起 `StopCoroutine` 掉 → **冻结代码根本没执行**
  → 同时**出招被封 + 防御键被 `OnChoiceSelected` 锁住** → 玩家**双重锁死**、只能站着挨打。
- 当时做了两件事：
  1. **修误杀**（出口光标只收自己，不再调 `StopHint`）—— 这才是正事；
  2. **顺手把这一拍的防御键解锁**（§12.3「这一拍解锁防御键（等待期间能格挡）」）—— 这是**当时的缓解措施**，被写进了代码注释（`Beats.cs` 「⚠️ 这一拍不要让防御键一直静音（2026-11-02 用户反馈「为什么我会受击」）」）。

→ **口径 2 的结论**：撤销缓解措施 2（这一拍也静音），并且**必须保证这一拍里玩家不会被「锁死又挨打」**（见 §4.5）。

> 教训：代码注释里引用的用户反馈，只能当**线索**，不能当**需求**。
> 引用前必须回到原始记录（`plan/` 的逐轮记录）核对上下文。

---

## 2. 现状实测

### 2.1 防御键的既有开关（**复用，不造轮子**）

`Assets/Scripts/UI/DefenseButton.cs`：

| API | 语义 | 表现 |
|---|---|---|
| `SetAvailable(bool)` (`:98`) | **隐藏**（教学关还没教到格挡） | alpha=0 + blocksRaycasts=false → 看不见也点不到，也不吞手势 |
| `SetMuted(bool muted, bool swallowClicks=true)` (`:117`) | **压暗可见但无效**＝「灰掉」 | alpha=0.35 + interactable=false（按下去不派发）→ 不防御；`blocksRaycasts` 由调用方决定：true=吞掉这一下点击（不漏成手势），false=不吃射线（点在防御键那块仍能出招） |
| `OnPointerDown` (`:184`) | 按下 → `PlayerState.SetDefend(true)` | 静音/隐藏/点在活敌人身上（`ShouldYieldToCombat`）三种情况直接 return |
| `Release()` (`:201`) | 松手 / 静音 / 隐藏 / 禁用 | 收回长枪 |

编排层 `Assets/Scripts/Tutorial/TutorialStageDirector.cs`：

- 两个静音来源合并应用 → `ApplyDefenseMute()` (`:707`)：
  - `_defenseMutedByChoice`（**教学专用**）：三选一弹窗开 → 静音；选完 → 继续静音；技能拍开拍 → 解除；拍收尾 → 解除。
    离开教学路线有**每帧兜底自愈**（`:385` `ReleaseTutorialOnlyDefenseMuteIfOffTutorialRoute`）。
  - `_defenseMutedByBeat`（**任何关卡**）：`BeatNeedsAttack(kind)` 为真时静音；
    时机已按 BUG-009 修好——提前到「教学条件满足」那一刻（`Hints.cs:213`），开拍再确认（`:278`），
    收拍路径统一恢复（`Hints.cs:109/235/245/599/726`）。
- `RefreshDefenseButtonAvailability()` (`:401`)：教学路线里「没教到格挡」→ 整个键**隐藏**；
  `Hints.cs:39` 在 DefendButton 拍开始时置 `_defendTaught = true` → 这一拍按钮才转可见。

### 2.2 教学拍表与覆盖矩阵（实测资产，非文档）

拍表：`Assets/Scenes/Battle.scene` → `TutorialStageDirector.gestureSteps`（5 拍）。
引导拍：`Assets/ScriptableObjects/Talents/TalentTree_Moves.asset` → `talent_move_stabc2`（ComboCharge）、`talent_move_slash`（SwipeHorizontal）。

| 教学段 | 拍型 | 要出招 | 防御键现状 | 判定 |
|---|---|---|---|---|
| N1 w0 | Tap | 是 | 静音 ✅（且此刻按钮整体隐藏） | OK |
| N1 w1 | TapCombo | 是 | 同上 | OK |
| 路线出口(-2) | Tap | 是 | 静音 ✅（该阶段 HUD 另被锁） | OK |
| E0 w0 | DefendButton | 否（**教按防御**） | 不静音 ✅ | OK |
| E0 w1 | SkillSlot | 否（点技能槽） | **不静音**（=旧缓解措施，按口径 2 要撤销） | **要改** |
| 三选一弹窗（教学） | — | — | 静音 ✅ | OK |
| 三选一弹窗（**正式关**） | — | — | **不静音** ❌（`OnChoicesReady` 里 `if (!IsOnTutorialRoute()) return;` 早退在静音之前） | **要改** |
| 引导拍·指向突刺 | ComboCharge | 是 | 静音 ✅ | OK |
| 引导拍·**横扫** | Swipe | 是 | **不静音** ❌ → 玩家按住防御 → `AttackSystem` 硬闸「防御中，不能出招」→ 这一划打不出来 → 拍收不了 → **卡死**（与 BUG-009 同型） | **要改** |

### 2.3 攻击手势的开关清单（口径 3 的参照物）

手势通道被关掉，一共这 5 道（`Assets/Scripts/Player/InputManager*.cs`）：

| # | 开关 | 位置 | 何时为假 |
|---|---|---|---|
| 1 | `gameplayInputEnabled` = `!GameplayInputGate.IsBlocked` | `InputManager.cs:244` | 结算面板（`BattleHUD` ResultPanel）/ 路线旅行 / 破防硬直（`PlayerGuard`）/ 开场字幕（`TutorialIntro`） |
| 2 | `Time.timeScale == 0 && !allowInputWhileTimeFrozen` | `InputManager.cs:434` | **三选一弹窗**（弹窗自己把 timeScale 压成 0）/ 教学时停（放行） |
| 3 | `IsAnyQTEActive()` | 同上 | QTE 期间 |
| 4 | `IsInBattleState()` = `stageState == StageState.InProgress` | `InputManager.Gesture.cs:387` | **游戏结束** / 局外 |
| 5 | `blockInputFrames > 0` | `InputManager.cs:452` | 三选一选完的几帧（防误触） |

**防御键是 UI 按钮，上面 5 道一道都不经过** → 这就是口径 3 要补的洞。
另外 `AttackSystem.TryExecuteAttack` 还有一道硬闸 `playerState.stageState != StageState.InProgress`（`:421`）和「防御中，不能出招」（`:424`）。

---

## 3. 方案 A：防御键的「能不能防御」判定（硬门 / 软门）

核心：**把「拒绝按」和「先记着」分开**。

```
硬门（拒绝按，连预输入都不产生）
  • !_available            教学关还没教到格挡（隐藏）
  • _muted                 教学拍静音（口径 1）
  • stageState != InProgress        游戏结束（与 TryExecuteAttack 同口径）
  • GameplayInputGate.IsBlocked     结算面板 / 路线旅行 / 破防硬直 / 开场字幕
  • 三选一弹窗打开                   弹窗自己暂停了游戏，手势通道已关（口径 3）
  • 点在活敌人身上（ShouldYieldToCombat）  那是攻击，不是防御

软门（**先记着，等它结束再兑现** ← 口径 4 的预输入）
  • 出招动作锁中（AttackSystem.IsActionPlaying）
  • 连段进行中（PlayerMoveStateMachine.IsMoveActive）
```

落地位置（**不新增组件、不新增全局单例**）：

- `Assets/Scripts/Player/Combat/PlayerState.cs`（Locus.Combat）
  - 新增 `public bool CanAcceptDefendIntent()` —— 硬门（stageState / 输入门 / 三选一弹窗）。
    依赖检查：`GameplayInputGate` 在 `Locus.Data` ✅、`UpgradeChoiceManager` 在 `Locus.Combat`（`Assets/Scripts/Core/Locus.Combat.asmref`）✅。
  - 改 `CanStartDefend()`（`:152`）= `CanAcceptDefendIntent() && !AttackSystem.IsActionPlaying`。
    → `DefenseButton.RefreshAvailability()` 已经用它决定图标亮度 → **图标自动变暗**（现有视觉通道，零新增美术）。
    ⚠️ `CanStartDefend()` 全工程只有两处调用（`PlayerState.SetDefend:116` / `DefenseButton.RefreshAvailability:235`）→ 改它不会波及其它。
    ⚠️ **行为变化**：破防硬直期间（`PlayerGuard.Break` 会 `GameplayInputGate.RequestBlock(GuardBroken)`，约 1.5s）
    今天**可以**按防御（会把枪在破防动画里举起来），加硬门后**不能**按 —— 与手势通道一致，属于修正。
  - `SetDefend(true)` 继续用 `CanStartDefend()` 兜底（`:116`）。
- `Assets/Scripts/UI/DefenseButton.cs`（Locus.UI）
  - 软门（连段是否还在进行）需要 `PlayerMoveStateMachine`：`Locus.Combat` ✅。
    取法与工程现状一致：`TutorialStageDirector` 也是 `FindObjectOfType<PlayerMoveStateMachine>()`（`Hints.cs:385` / `Dialogue.cs:166`）；
    或 `InputManager.Instance.moveStateMachine`（`InputManager.cs:46`，Locus.Flow，Locus.UI 可见 ✅）。**取后者优先，取不到再 Find**。

---

## 4. 方案 B：预输入（口径 4 / 5）

### 4.1 语义

> 玩家提前按住防御键 → 现在不能防御（出招动作锁 / 连段未结束）→ **记住这个意图**
> → 连段结束的那一刻自动转进防御 → 一直防御到玩家松手。

### 4.2 兑现点 = 连段结束（不是动作锁结束）

| 时机 | 动作锁 | 连段（`IsMoveActive`） | 防御是否兑现 |
|---|---|---|---|
| 玩家在 Jab1 收招里按住防御 | 还在 | 还在（接续窗口开着） | ❌ 不兑现（否则会砍掉连段） |
| 连段走完回到中立 | 结束 | 结束 | ✅ **这一帧兑现** |

→ 兑现点 = `!AttackSystem.IsActionPlaying && !machine.IsMoveActive`。
→ 与既有预输入同族：`PlayerMoveStateMachine` 的跨招式缓冲就是在「回到中立」兑现的
（`PlayerMoveStateMachine.cs:146-151` `if (!_active) { TryExecuteBufferedInNeutral(); return; }`）。

### 4.3 落点（全部在 `DefenseButton.cs`）

```csharp
private bool _pressed;        // 既有：手指还按着
private bool _pendingDefend;  // 新增：按了但现在还不能防御 → 记住

OnPointerDown:
    if (!_available) return;                       // 隐藏
    if (_muted) return;                            // 静音（口径 1）
    if (ShouldYieldToCombat(pos)) return;          // 点在活敌人身上 → 那是攻击
    _pressed = true;
    var ps = PlayerState.Instance;
    if (ps == null || !ps.CanAcceptDefendIntent()) return;   // 硬门 → 拒绝，且不缓冲
    if (ps.CanStartDefend()) ps.SetDefend(true);              // 现在就能防御 → 立刻
    else _pendingDefend = true;                               // 软门 → 预输入

Update():
    RefreshAvailability();                        // 既有
    if (_pendingDefend && _pressed && CanDefendNow())   // 连段结束 → 兑现
    {
        PlayerState.Instance?.SetDefend(true);
        _pendingDefend = false;
    }

Release():   // OnPointerUp / SetMuted(true) / SetAvailable(false) / OnDisable
    _pressed = false;
    _pendingDefend = false;                       // ← 口径 5：只在松手/静音时取消
    PlayerState.Instance?.SetDefend(false);
```

**取消路径只有两条**（口径 5）：松手、`SetMuted` / `SetAvailable`（静音与隐藏都会调 `Release()`，已存在）。
**不设超时**（口径 5）。

### 4.4 与静音的耦合（口径 1 + 4 必须一起做）

`SetMuted(true)` → `Release()` → 清 `_pendingDefend` → **教学拍一开始，之前挂着的防御预输入被取消**。
否则：玩家在拍开始前按住防御 → 预输入挂起 → 某一拍的动作锁一结束 → 自动防御 → **把这一拍要教的招式又挡掉 → 制造新的卡死**。

### 4.5 教学拍静音口径改成黑名单（口径 1）

`TutorialStageDirector.BeatNeedsAttack(kind)`（`:790`）现在是**白名单**（Tap / TapCombo / ComboCharge）。
改成两个概念：

| 函数 | 口径 | 用途 |
|---|---|---|
| `BeatNeedsAttack(kind)` | 这一拍玩家要**出招**（Tap / TapCombo / ComboCharge / Swipe / Hold / ChargedSwipe / Parry / ParryHold） | 决定静音时**吃不吃射线**：出招拍必须**不吃**（否则玩家点在防御键那块 → 招式打不出来 → BUG-009 卡死） |
| `BeatAllowsDefend(kind)` | **只有 `DefendButton` 为真** | 口径 1：其余拍一律静音 |

调用点（`Hints.cs:213` / `:278`）：`if (!BeatAllowsDefend(step.kind)) SetDefenseMuteBeat(true, BeatNeedsAttack(step.kind));`
`ApplyDefenseMute()` 的 `swallowClicks` 改为由「当前拍是否需要出招」决定（出招拍不吞，其余吞）。

### 4.6 实测记录（**本次范围外，仅备查**）

以下是在读代码时看到的一条事实，**不属于本次需求，也没有做任何改动**：

技能打断拍要求 `skillReady = !IsSkillOnCooldown(...)` 才肯冻结（`Beats.cs`）。
若玩家提前把技能放了，这一拍会等技能冷却（海浪 10s）才冻结，期间敌人自由攻击。
是否要处理、怎么处理 —— **由用户另行决定**，本文不做建议。

---

## 5. 改动清单

| 文件 | 改动 | 冲突面 |
|---|---|---|
| `Assets/Scripts/UI/DefenseButton.cs` | `_pendingDefend` + `OnPointerDown` 分硬门/软门 + `Update` 兑现 + `Release` 清 pending | **无冲突**（无人改） |
| `Assets/Scripts/Player/Combat/PlayerState.cs` | 新增 `CanAcceptDefendIntent()`；`CanStartDefend()` 改为硬门+软门 | 无冲突 |
| `Assets/Scripts/Tutorial/TutorialStageDirector.cs` | `BeatNeedsAttack` 拆成「需要出招」+ 新增 `BeatAllowsDefend`；`ApplyDefenseMute` 的射线口径 | 隔壁只动了文件头注释，**未碰这两个函数** |
| `Assets/Scripts/Tutorial/TutorialStageDirector.Hints.cs` | **不动** —— 射线口径改成在 `ApplyDefenseMute()` 里从 `_currentStep.kind` 自己推导 | 隔壁在改同文件 → 避开 |
| `Assets/Scripts/Tutorial/TutorialStageDirector.Beats.cs` | **本次不动** | 隔壁正在改 → 等收工 |

---

## 5.1 复核记录（2026-11-08 逐条回代码核对）

> 这一节是「每条结论都能落到 file:line」的核对表。**上一轮有一处结论是错的，已改**（矩阵里“路线出口”那一格）。

| # | 结论 | 证据 | 状态 |
|---|---|---|---|
| 1 | 静音有两个现成 API（隐藏 / 灰掉） | `DefenseButton.cs:98` `SetAvailable`、`:117` `SetMuted(muted, swallowClicks)` | ✅ |
| 2 | 静音的两个来源合并应用 | `TutorialStageDirector.cs:707` `ApplyDefenseMute`（`_defenseMutedByChoice` / `_defenseMutedByBeat`） | ✅ |
| 3 | 拍静音提前到“条件满足”那一刻 | `Hints.cs:213`（BUG-009 的修法） | ✅ |
| 4 | **拍正常收尾会解除拍静音** | `Hints.cs:705` `EndBeat` → `:731` `SetDefenseMuteBeat(false)` | ✅（黑名单改造后不会残留） |
| 5 | **路线出口拍（wave -2）不走拍静音** | `Hints.cs:42` → `RouteHintRoutine`（`:126`）全程**不调** `SetDefenseMuteBeat` | ❌ **上一轮矩阵写错，已改**：那一格靠的是路线阶段 HUD 整体失能（`TutorialStageDirector.cs:356` `UpdateRouteTeachingLock`） |
| 6 | 拍表只有一份 + 运行时生成的引导拍 | `TutorialStageDirector.cs:121` `gestureSteps`（Battle.scene）+ `MoveGuide.cs:27` `BuildMoveGuideSteps`；全工程仅此两处 | ✅ |
| 7 | 引导拍只有两种手势 | `TalentTree_Moves.asset`：`talent_move_stabc2`=ComboCharge、`talent_move_slash`=SwipeHorizontal，其余节点 `moveGuideGesture: 0`(None) | ✅ |
| 8 | 横扫引导拍**没静音** → 会卡死 | `BeatNeedsAttack`（`TutorialStageDirector.cs:790`）只含 Tap/TapCombo/ComboCharge，**Swipe 不在内**；按住防御 → `AttackSystem.cs:424` 硬闸 → 招式打不出来 → 拍收不了 | ✅ 真缺口 |
| 9 | 正式关的三选一弹窗**没静音** | `TutorialStageDirector.cs:602` `if (!IsOnTutorialRoute()) return;` **早退在** `:646` `SetDefenseMuteChoice(true)` **之前** | ✅ 真缺口（由口径 3 的硬门统一解决） |
| 10 | 手势通道的 5 道闸 | `InputManager.cs:244`（输入门）、`:434`（timeScale）、`:136`（QTE）、`InputManager.Gesture.cs:387`（`stageState==InProgress`）、`:452`（blockInputFrames） | ✅ |
| 11 | 三选一弹窗的可靠信号 | `UpgradeChoiceManager.cs:285` `_isChoosing=true` / `:449` `=false`；`:86` `IsChoosing` | ✅ |
| 12 | 破防硬直期间今天**可以**按防御 | `PlayerGuard.cs:116` `RequestBlock(GuardBroken)` → 输入门拦手势，但**不拦 UI 按钮** | ✅（加硬门后行为会变，已在 §3 标注） |
| 13 | 教学拍期间输入门**没被拦**（否则“教防御”拍会自己把自己禁了） | `TutorialGestureHint.Gate()`（`:140`）**全工程无调用点** → `TutorialGate` 这个 owner 实际不持有拦截；`TutorialIntro` 只在开场字幕期间持有 | ✅ |
| 14 | 防御只有 UI 一个入口 | `PlayerState.ToggleDefend()`（`:108`）**无调用点**；无键盘/手柄入口 | ✅ |
| 15 | `CanStartDefend()` 改动面 | 全工程两处调用：`PlayerState.cs:116`、`DefenseButton.cs:235` | ✅ |
| 16 | 敌人出伤在攻击 tween 结束 | `Enemy.cs:305` 注释 + `TimeToAttackEnd()`（`:311`）= tween 剩余时间 | ✅ |
| 17 | 教学 E0 每波 1 只敌人 | `Tutorial_E0_Battle.asset` 两波各 1 个 `enemyIds`；`damageMultiplier: 0.3` | ✅ |
| 18 | 防御键极少落在敌人身上 | `EnemyManager.cs:510` `TryGetEnemyAtScreenPoint` → 敌人中心半径 `max(40px, 屏高11%)` 的核心圆 | ✅ |
| 19 | 拍静音的“吞不吞射线”两套口径都在 `ApplyDefenseMute` 里算 | `TutorialStageDirector.cs:714` `swallowClicks = !_defenseMutedByBeat \|\| _defenseMutedByChoice` | ✅（需拆成“当前拍是否要出招”） |

---

## 6. 验证口径

1. `Tools ▸ 三国杀戮 ▸ 教学关 ▸ 引导期可交互元素审计` → 打印「防御键：可用=…；静音=…」→ 逐拍核对。
2. 预输入手测：出招动作锁/连段期间按住防御键 → **连段结束那一帧**自动进防御位；松手立即退出。
3. 连段不被打断：Jab1→Jab2 连段期间按住防御 → 连段**不被砍**；连段走完才进防御。
4. 静音期间：教学拍开始时若玩家正按住防御 → **不自动防御**（pending 被静音取消）。
5. 口径 3：三选一弹窗打开 / 结算面板出现时 → 按防御键**无反应**，图标变暗。
6. 回归：解锁「横扫」后首次进关的引导拍 → 按住防御键划动 → 应被静音挡住（而不是卡死）。

---

## 8. 实施与验证（2026-11-08 已落地）

### 8.1 实际改动（4 个文件）

| 文件 | 改了什么 |
|---|---|
| `Assets/Scripts/Player/Combat/PlayerState.cs` | 新增 `CanAcceptDefendIntent()`（硬门：非战斗状态 / 输入门拦下 / 三选一弹窗）；`CanStartDefend()` = 硬门 + 动作锁；`SetDefend` 先查硬门再查动作锁 |
| `Assets/Scripts/UI/DefenseButton.cs` | 新增 `_pendingDefend`（预输入）；`OnPointerDown` 分硬门/软门；`Update` 里 `TryConsumePendingDefend()` 兑现；`Release()` 清 pending（松手/静音/隐藏/禁用四个入口共用） |
| `Assets/Scripts/Tutorial/TutorialStageDirector.cs` | `BeatNeedsAttack` → **拆成两个**：`BeatShouldMuteDefense`（黑名单：除 `DefendButton` 外全静音）+ `BeatRequiresPlayerAttack`（决定静音时吃不吃射线）；`ApplyDefenseMute` 的射线口径改由 `_currentStep.kind` 推导 |
| `Assets/Scripts/Tutorial/TutorialStageDirector.Hints.cs` | 两处静音判定改用 `BeatShouldMuteDefense`；**`BeginBeat` 里加一行** `SetDefenseMuteBeat(...)` → 一次覆盖**所有拍型**（招式拍 / 霸体防御拍 / 技能打断拍 / 时停招架拍），收拍由 `EndBeat` 统一恢复 |

**为什么静音加在 `BeginBeat`**：技能打断拍 / 霸体防御拍 / 时停招架拍走的是**各自的协程**，不经过 `HintRoutine` →
以前只在 `HintRoutine` 里静音，这几条路径根本没被盖到。放在 `BeginBeat` = 一个入口盖全部拍型。

### 8.2 判定表（反射实测，与设计一致）

| 拍型 | 灰掉防御键 | 要出招 | 吃射线 |
|---|---|---|---|
| Tap / Hold / Swipe / ChargedSwipe / Parry / ParryHold / TapCombo / ComboCharge | 是 | 是 | 否（保住 BUG-009 那条修复：点在防御键那块仍能出招） |
| None / SkillSlot | 是 | 否 | 是（点防御键不会顺手打出一记攻击） |
| **DefendButton** | **否** | 否 | — |

### 8.3 自动化验证结果（Play Mode 白盒，10 项全过）

| # | 验证项 | 结果 |
|---|---|---|
| 1 | 常态：硬门放行、防御键可用且未静音 | ✅ |
| 2 | 动作锁中按防御键 → 产生预输入，**当前不防御** | ✅ |
| 3 | 动作锁结束 → **那一帧自动进防御**，预输入清掉 | ✅ |
| 4 | 松手 → 退出防御 | ✅ |
| 5 | 静音中按防御键 → **不产生预输入** | ✅ |
| 6 | 按下→立刻松手 → 松手即取消预输入（口径 5，无超时） | ✅ |
| 7 | **连段进行中** → 动作锁过了也**不兑现**（不砍连段） | ✅ |
| 8 | **连段结束** → 自动兑现 | ✅ |
| 9 | 三选一弹窗打开 → 硬门拒绝（不防御、不缓冲）；关闭后恢复 | ✅ |
| 10 | **游戏结束**（实测战败）→ 防御被拒（输入门 ResultPanel + RouteTravel 拦着） | ✅ |

> 存档保护：每次跑测前 `PlayerPrefs.GetString("player_save")` 备份、跑完写回。
> 测试用白盒（反射改 `_actionLockTimer` / `_active` / `_duration` / `_isChoosing`），**没动任何游戏逻辑**。

### 8.4 未覆盖（需用户实机验收）

1. **教学全流程每拍的静音表现**（尤其：技能打断拍现在也灰掉了 —— 这是 2026-11-02 旧缓解措施的撤销，
   等敌人抡招那几秒玩家是「不能出招 + 不能防御」；冻结点在出伤前 1.5s，理论不挨打，但要人眼确认）。
2. **提前按住的手感**：预输入兑现的时机、图标反馈（目前「预输入中」和「出招中」共用同一个变暗色）。
3. 正式关「解锁横扫后首次引导拍」按住防御键划动 → 应被挡住而不是卡死（回归）。

### 8.5 已知副作用（与手势通道对齐的修正）

- **破防硬直期间**（`PlayerGuard.Break` → 输入门 GuardBroken，约 1.5s）现在**不能按防御**了
  （以前能按 → 会把枪在破防动画里举起来）。
- 教学「教按防御键」那一拍**不受影响**：输入门的 `TutorialGate` 实际从不持有拦截
  （`TutorialGestureHint.Gate()` 全工程无调用点），`TutorialIntro` 只在开场字幕期间持有。

---

## 9. 参考

- `design/tutorial-system.md` —— 教程六层架构；L2 拍级门控层就是防御键静音的落点。
- `plan/tutorial-hint-handoff-20261102.md` §12/§13 —— 2026-11-02 原始记录（本文 §1 更正的出处）。
- `plan/bugs-and-requests.md` BUG-009 —— 「防御键抢输入 → 教学卡死」与 `SetMuted(muted, swallowClicks)` 的由来。
- `design/action-game-feel-and-input-buffering.md` §4.3 —— 既有输入缓冲规范（防御预输入与其同族）。
