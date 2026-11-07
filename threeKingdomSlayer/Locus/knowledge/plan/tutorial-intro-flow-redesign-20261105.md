---
id: kd_3004767b-bed7-4e95-9cba-7d26c2625d29
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_tutorial_intro_20261105
summary: 教程开场流程重做（2026-11-05 用户定，尚未实施）：暂停 + 第一段字幕（玩家零交互，仅暂停键可用）→ 出现「点击继续」按钮 → 第二段字幕 → 正式教学（与当前一致）。含目标流程、现状代码事实（台词资产每波仅 1 行 / DialogueLayer 20 与 PauseLayer 30 的层级结论 / GameplayInputGate / 现有 DialogueAdvanceButton）、实施要点、5 个待拍板决策点与验收清单。
---

# 教程开场流程重做（2026-11-05 用户定 · **2026-11-06 按反馈重做**）

> **状态：代码已实施（2026-11-06），Play Mode 自动化验收通过；**第一段字幕文案待用户改**、**真机手感待验收**。
> 用户原话：「玩家在游戏内的游玩内容是不断的点击屏幕，所以教程应该改为：
> **暂停一段时间，期间播放第一段字幕，玩家无法进行任何交互（除了按左上角暂停键）**；
> **然后再出现点击按钮处继续**；**然后播第二段字幕正式开始教学（与当前一致）**。」

---

## 1. 目标流程（每波开场）—— **2026-11-06 用户口述定稿，需求原文**

> 用户原话（逐字，2026-11-06）：
> **「就位，暂停，播放第一段文本同时启用按键(不等待时间)；
>   玩家点击按键；
>   出现第二段文本和对应手势，直到玩家做出对应手势前文本和教学各项内容都不会消失；
>   完成教学，退出 回到正常游戏；
>   不存在任何计时器直接跳过，从来没有这样的需求。」**

```
[波次开始] → 敌人进场 → **就位**（敌人站定 = 原本那行台词出现的时刻）
   │
   ├─ ① 暂停：冻场 + 锁输入 + 挡住其它 UI
   │        玩家此时点屏幕**没有任何反应**；只有左上角暂停键可按
   │
   ├─ ② 播【第一段文本】**同时**亮出「点击继续」按键（**不等任何计时器**）
   │        → 文本不会自己消失，玩家可以慢慢读
   │
   ├─ ③ 玩家点击按键
   │
   ├─ ④ 出现【第二段文本】**与对应手势示范**（同时出现）
   │        → 在玩家做出对应手势之前，**文本与教学各项内容都不会消失**
   │
   └─ ⑤ 做出手势 → 教学完成 → 退出暂停 → 回到正常游戏
```

### 由这段描述直接定死的几条

| # | 规则 | 含义 |
|---|---|---|
| 1 | **没有任何计时器** | 不存在“等 N 秒自动推进/自动跳过/超时放行”。第一段文本不会自己消失；第二段文本也不会按秒消失 |
| 2 | 第一段文本与按键**同时**出现 | 不是“文本先出 → 文本播完 → 按钮才出” |
| 3 | 第二段文本与手势**同时**出现 | 且两者**一起留到玩家做出手势** |
| 4 | 唯一的推进力 = 玩家的动作 | 一次点击（③）+ 一个手势（④） |
| 5 | 开场阶段**不需要任何压暗/黑幕/高亮** | 需求里没写 → 按工作流规则不得自主添加（见 `memory/user-preference.md`） |
| 6 | 暂停的**位置** = 就位那一刻 | 2026-11-06 用户确认：“当前暂停打点正确，但是需要优化” |

> **⚠️ 2026-11-06 用户反馈后修正**：之前把流程理解成“第一段自动播完 → 按钮出现 → 第二段播完 → 进教学”，并自行追加了压暗/黑幕、12 秒超时、台词自动推进三条需求。
> 上表是**唯一**依据；本表以外的行为一律属于“未描述”，只能提建议。

**为什么**：玩家的默认行为是**不停点屏幕**。当前教程里每一次点击都可能被当成"推进台词 / 出招"，
把教学节拍打乱（这正是 `plan/bugs-and-requests.md` BUG-009 那类问题的温床）。
开场做成**显式三段**，玩家立刻知道"现在该看字 / 现在该点一下 / 现在该打"。

---

## 1.1 当前实现 vs 定稿的差距（**已按定稿改完，2026-11-06**）

> 下面只列**定稿要求**与现有实现的差额（2026-11-06 定稿后）。不包含任何“需求外”的项。

| # | 定稿要求 | 现有实现（我写的） | 需要的改动 |
|---|---|---|---|
| 1 | 第一段文本**不会自己消失** | 对话视图对 `DialogueEventType.Tutorial` 自动推进（`max(2.2s, 字数×0.16s)`） | 开场期间**关掉自动推进**（建议：对话视图加一个开关，由导演在开场期间关掉、结束时恢复；不动现有台词资产） |
| 2 | 第一段文本与按键**同时**出现 | 先等第一段自动推进完，**才**亮出按钮 | 改成同一帧：文本上屏的同时亮出「点击继续」 |
| 3 | **不存在任何计时器自动跳过** | 有 `introContinueTimeoutSeconds = 12`（超时自动继续） | **删掉这个超时**（以及它的计时逻辑） |
| 4 | 第二段文本与手势**同时**出现，且**都留到玩家做出手势** | 先等第二段自动推进完，**才**开始手势示范 | 第二段上屏与 `BeginBeat`（手势示范）同时；文本同样不自动消失；收拍时 `EndBeat` 已有的 `view.Skip()` 会清掉文本 ✓ |
| 5 | 做出手势 → 教学完成 → **退出暂停**回正常游戏 | 第二段播完就解锁 | 解锁时机 = 拍完成（`EndBeat`）之后，而不是第二段播完 |
| 6 | 暂停位置 = 就位那一刻 | ✅ 已经是（2026-11-06 用户确认“当前暂停打点正确”） | 不动 |
| 7 | 引导期只有暂停键与「点击继续」可点 | ✅ 已经是（HUD 整块失能 + 点击遮罩） | 不动 |
| 8 | 开场无压暗/黑幕/高亮 | ✅ 本来就没有 | 不动（**也不许再加**） |

---

## 2. 现状（代码事实，2026-11-05 实测）

| 环节 | 现在在哪 | 事实 / 备注 |
|---|---|---|
| 波次开始的编排 | `TutorialStageDirector.OnWaveStarted` → `OnWaveStartedRoutine` | 教学台词在这里**只挂** `_pendingWaveDialogue`，**不推**（入场期 timeScale=0 会让敌人走不动） |
| 波次台词实际弹出 | `HintRoutine`（`TutorialStageDirector.Hints.cs` L199-217）：等敌人站定 → 推台词 → 等它显示 → 再开拍 | 现在台词与手势引导**同时**出现 |
| 台词资产 | `Assets/ScriptableObjects/Dialogue/tutorial_t0_*.asset` —— **每个只有 1 行**（`tutorial_t0_opening` / `wave1..4` / `open` / `death` / `interrupt` / `combo` / `skill*` / `choice` / `route_choice`） | 新的"两段字幕"要**加内容**（见 §4.3） |
| 台词显示 | `BattleDialogueView.Show(dialogueEvent, onFinished, blockClicks)`；教学类台词**自动推进**（`_tutorialAutoAdvanceMinSeconds=2.2` + 每字 0.16s） | 要在两句之间插「点击继续」→ 需要"停在某一行等点击"的能力 |
| 「继续」按钮 | `BattleDialogueView._advanceButton`（场景对象 `DialogueAdvanceButton`），`blockClicks=true` 时显示 → `onClick → Advance()` | **现成按钮与美术都在**；但它现在只负责"下一句台词" |
| 输入锁 | `GameplayInputGate.RequestBlock(owner, reason)` / `Release(owner)`；`InputBlockReason` 已有 `TutorialGate` | 建议新增 `TutorialIntro` 原因，便于区分排查 |
| **暂停键层级** | `PauseLayer` sortingOrder = **30**；`DialogueLayer` = **20** | ✅ **暂停键在对话遮罩之上** → 「除了暂停键都能点」这条**天然满足**，不用改层级 |
| 对话遮罩 | `BattleDialogueView._clickBlocker`（960×1920 全屏，射线开） | 遮罩期间点屏幕不会漏成游戏手势 ✓（BUG-009 的教训：静音态的防御键**也要**放行射线） |
| 时间冻结 | 项目里 `timeScale=0` 有已知坑：`InputManager.IsPointerDown` 要求 `timeScale > 0`；招架接触帧走 unscaled；`AttackSystem.ClearActionLock()` 是给时停教学用的 | 见 §5 决策点 1 |

> **顺带**：`TutorialStageDirector` 在正式关也在跑（招式解锁引导拍）→ 改动要注意**别影响正式关**。

---

## 3. 实施要点

### 3.1 新增「开场流程」协程（插在 `OnWaveStartedRoutine` 最前面）

```
OnWaveStartedRoutine(waveIndex)
  └─ yield return TutorialIntroRoutine(waveIndex);   ← 新增（在招式解锁引导拍之前）
       ├─ GameplayInputGate.RequestBlock(this, InputBlockReason.TutorialIntro)
       ├─ 冻场（敌人不动）+ 播【第一段字幕】（见 §3.2）
       ├─ 等第一段字幕播完 → 显示「点击继续」→ 等点击（**unscaled 计时**）
       ├─ 点击 → 播【第二段字幕】→ 等它结束
       ├─ GameplayInputGate.Release(this)   ← 这一刻起玩家才能操作
       └─ 继续现有流程（引导拍 → 教学拍）
```

### 3.2 两段字幕怎么播

- 现有 `BattleDialogueView` 一次 `Show()` 会把整个 `DialogueEventData` 的多行按**自动推进**播完。
- 需要**"先播第 1 行 → 停下等点击 → 再播第 2 行"**。
  - 最小改动方向：给 `BattleDialogueView` 加一个"**播到第 N 行就停、显示继续按钮**"的能力
    （或加一个 `ShowLines(event, fromLine, toLine)` 的入口）。
  - 另一种做法：**两句用两个台词资产**（第一段一个 asset、第二段一个 asset）→ 视图完全不用改 ✓（更省事）。
  - → **建议：用两个资产**（第一段新增 `tutorial_t0_intro_*.asset`），视图零改动，风险最低。

### 3.3 「点击继续」按钮

- **优先复用** `BattleDialogueView` 的 `_advanceButton`（现成、美术已就位、位置在气泡下方）。
  - 但它现在绑定的是 `Advance()`（下一句台词）→ 要新增一个"点击 → 触发回调"的用法。
  - ⚠️ 按钮**必须吃射线**（玩家点它不能漏成游戏手势）→ 静音态按钮的教训（BUG-009）。
- 若要独立于台词（第一段字幕播完后台词已消失），则在 `DialogueLayer` 下新建一个按钮对象（美术可复用现有按钮图）。
  - ⚠️ 层级必须在 `DialogueLayer`（order 20）内或之上；**不能低于暂停键的层级需求**（暂停键 order 30 不受影响 ✓）。

### 3.4 台词内容

- 现在每波台词**只有 1 行** → 新的两段字幕需要内容：
  - **第一段（暂停期间）**：这一波/这一段要做什么的一句话铺垫（承接上一波）。
  - **第二段（点击继续之后）**：现在那行台词（它的位置就是"正式教学前的一句"）。
- → 内容侧工作量：教学 N1 / E0 各 2 波 → **约 3~4 条新台词**（N1 第一波、E0 第一波可复用"开场"）。

---

## 4. 决策点（要拍板 / 实测确认）

| # | 问题 | 选项 / 建议 |
|---|---|---|
| 1 | 「暂停一段时间」= 真冻结时间（`timeScale=0`）还是**只锁输入 + 冻敌人**？ | 建议**只锁输入 + 冻敌人**：项目里 `timeScale=0` 有已知坑（指针状态判定、招架接触帧），而"玩家不能操作"的观感靠**输入锁 + 字幕 + 敌人不动**已经能达成；若要更强的"暂停感"，再评估 timeScale |
| 2 | 这个开场**每一波**都走，还是**只有第一波**？ | 每波都走 = 节奏统一但啰嗦；只第一波 = 干净。**建议：只有教学的第一波走完整流程，后续波次走简化版（锁输入 + 字幕 + 点继续）** |
| 3 | 正式关的**招式解锁引导拍**（ComboCharge / 指向突刺）要不要也套「暂停 → 点继续」？ | 用户反馈里这条线也出过事（BUG-009）→ **建议要**（至少给"点继续"这一步） |
| 4 | 「点击继续」按钮的形态：复用台词区的 `DialogueAdvanceButton`，还是独立一个居中大按钮？ | 建议**独立居中大按钮**（玩家一眼看到"点这里"），美术复用现有按钮图 |
| 5 | 第一段字幕播完后按钮**自动出现**，还是**字幕播完停住等玩家点**？ | 建议自动出现（字幕有固定停留时长，玩家看完自然看到按钮） |
| 6 | 「点击继续」能不能跳过？ | 建议**不能**（这一下点击就是教学的一部分：让玩家学会"教程会等我的点击"）。**已实施为：不能跳，但 30s 超时自动继续**（`introContinueTimeoutSeconds`，防卡死） |

---

## 5. 验收清单（实施后逐条手测）

1. 进教学 N1 → 波次开始 → **画面停住、第一段字幕出现**；此时**点屏幕任何位置都没反应**。
2. **左上角暂停键仍然能按**（层级已验证：PauseLayer 30 > DialogueLayer 20 ✅，但要实测确认没有被输入锁连带拦掉）。
3. 第一段字幕播完 → **出现「点击继续」** → 点击 → **第二段字幕** → 字幕结束 → **手势引导出现**，这时点屏幕才有反应。
4. 正式教学**与当前一致**：手势示范、防御键静音、拍能收（回归 `design/combo-move-state-machine.md` §11.11 的 5 条回归重点）。
5. 中途退出 / 暂停 / 切场景 → **输入锁必须回收**（`GameplayInputGate` 的 owner 规则会在对象销毁时自动失效 ✅，但要实测）。
6. 正式关的招式解锁引导拍不受影响（除非按决策点 3 一起改）。

---

## 6. 实施记录（2026-11-06）

### 6.1 实际改了什么

| 文件 | 改动 |
|---|---|
| `Assets/Scripts/Tutorial/TutorialStageDirector.Intro.cs`（新） | `TutorialIntroRoutine(waveIndex)`：锁输入 → 冻场（timeScale=0）→ 第一段字幕 → 「点击继续」→ 第二段字幕 → 解锁。`try/finally` 保证被打断也把状态还回去 |
| `Assets/Scripts/Tutorial/TutorialStageDirector.cs` | 新字段 `n1IntroDialogues` / `e0IntroDialogues` / `continueButton` / `introContinueTimeoutSeconds`；`OnWaveStartedRoutine` 里调用开场流程；`ReleaseGlobalTutorialState()` 补 `GameplayInputGate.Release(this)` + 收按钮 |
| `Assets/Scripts/UI/TutorialContinueButton.cs`（新） | 「点击继续」按钮组件：自带 Canvas(121)+GraphicRaycaster、显示/隐藏+回调、`SimulateClick()`（自动化用） |
| `Assets/Scripts/Data/GameplayInputGate.cs` | 新增 `InputBlockReason.TutorialIntro`（诊断用） |
| `Assets/Scripts/Editor/TutorialSetupTool.cs` | 新增 `EnsureIntroDialogue()`（**已存在的资产不改文案**）+ 接线两个 intro 数组 |
| `Assets/Scripts/Editor/TutorialFlowDriver.cs` | 开场期间等「点击继续」并点它（且不许点按） |
| `Assets/Scripts/Editor/TutorialInteractionAudit.cs`（新） | **引导期可交互元素审计**（菜单：Tools ▸ 三国杀戮 ▸ 教学关 ▸ 引导期可交互元素审计）：枚举屏上真正能被点到的东西并逐个判定「点了会发生什么」 |
| `Assets/Scripts/Core/HealthPotionManager.cs` | 喝药补门控：`CanUsePotionNow()`（时停 / 输入门拦下时不能喝） |
| `Assets/Scripts/UI/FrontItemBar.cs` / `BuffDisplayPanel.cs` | 道具栏点击整体拦下（教学「用技能打断」那一拍除外） |
| `Assets/Scenes/Battle.scene` | 新增 `TutorialContinueButton`（复制自 `DialogueAdvanceButton` + Canvas121 + GraphicRaycaster + 文案「点击继续」）+ 导演四个字段接线 |
| `Assets/ScriptableObjects/Dialogue/tutorial_t0_intro_n1_w0.asset` 等 4 个（新） | 第一段字幕（**文案是草稿，待用户改**） |

### 6.2 关键实现选择（与原方案的差异）

- **零交互靠「timeScale=0 + 输入闸门」，不靠点击遮罩**：
  实测发现 `DialogueOverlay`（order=120 的嵌套 Canvas）**没有 GraphicRaycaster** → 里面的全屏 `ClickBlocker` **根本没在挡点击**；
  真让它生效又会盖住暂停键（order=30）→ 改用冻结（`InputManager.Update` 在 `timeScale==0` 时直接吞掉所有手势）+ `GameplayInputGate`。
- **「点击继续」不复用 `DialogueAdvanceButton`**：它在 `DialogueOverlay` 下 → 收不到射线 → 点不动。
  新按钮**自带 Canvas + GraphicRaycaster**（照抄场景里 `DialogueSkipButton` 的做法），order=121。
- **台词用 `nonBlocking=true`**：不暂停、不压暗、不挡点击 → 暂停键照常可按；冻结由开场流程自己管。
- **`DialogueManager.AllowDialogueWhilePaused`** 在开场期间打开（冻结中 `IsBlockedByOtherUI()` 会拦掉台词队列），收场复原。

### 6.3 实测结果（Play Mode，2026-11-06）

| 验收项 | 结果 |
|---|---|
| 四波都走开场 | ✅ 驱动日志：13.7s / 21.1s / 35.5s / 52.9s 各点了一次「点击继续」（N1 w0 / N1 w1 / E0 w0 / E0 w1） |
| 开场期间零交互 | ✅ 快照：`timeScale=0`、`IsPointerDown=False`、`gameplayInputEnabled=False` |
| 输入锁的 owner 与原因 | ✅ `输入门：已拦下，持有者 1 个 → Tutorial Director(TutorialIntro)` |
| 输入锁回收 | ✅ 开场结束后持有者只剩 RouteFlow/BattleHUD（不是我们）；退出后无残留 |
| 冻结回收 | ✅ 开场结束后 `timeScale=1` |
| 「点击继续」能被点 | ✅ 射线首命中 = `TutorialContinueButton`（ord=121） |
| 暂停键在开场期间可用 | ✅ 暂停键位置射线首命中 = `PauseIcon`（ord=30），未被对话层遮挡 |
| 正式教学未被破坏 | ✅ 三选一、霸体防御拍、技能打断拍、结算全部跑通；Console 0 error / 0 warning |

### 6.5 ⚠️ 真机「一上来就卡死」——根因与修复（2026-11-06，用户实测发现）

**现象**：玩家进教学关立刻卡死（画面冻住、点「点击继续」没反应），只能等 30 秒超时。

**根因**：新做的「点击继续」按钮原本挂在 `DialogueOverlay` 下，而 **`DialogueOverlay` 上挂着一个 CanvasGroup：
`m_Interactable: 0`、`m_BlocksRaycasts: 0`**（场景里写死的）→ 它的**所有子物体既不可交互、也不吃射线**。
- `Button.OnPointerClick` 先判 `IsInteractable()` → false → **点击被直接丢弃**（按钮看着能点，真点没反应）。
- 同一个 CanvasGroup 的 `blocksRaycasts=0` 也解释了**为什么全屏 `ClickBlocker` 一直没挡住点击**。

**为什么我的测试没发现**：教学驱动 `TutorialFlowDriver` 调的是按钮的 **C# 回调**（`SimulateClick()`），
**绕过了整个 UI 判定层**；而机器人出招走 `InputManager.SubmitGestureForDebug`，**不查输入门、也不查 timeScale**
→ 机器人在我冻结的场子里照样能动，**根本感觉不到「卡」**。

**修复**：
1. 把按钮从 `DialogueOverlay` **移到 `DialogueLayer`**（其祖先 CanvasGroup `BattleHUD(Canvas)` 是 interactable=true）
   → 按钮自带 Canvas(121)+GraphicRaycaster 不变。
2. `TutorialContinueButton.ClickViaUi()`：**真实 UI 点击路径**（射线 + EventSystem 派发 pointerClick）
   → 驱动改用它，以后自动测试不会再漏这一类洞。
3. `TutorialInteractionAudit` 的「真能点到」判据补上 **`Button.IsInteractable()`**（射线命中 ≠ 点得动）。
4. `EndIntro()` 改成**先解冻再收别的**、每步 try/catch（任何一步抛异常都不能把游戏留在 timeScale=0）。
5. 「点击继续」超时 30s → **12s**（万一再坏，不该是"卡死 30 秒"）。

**复测（Play Mode，驱动走真实 UI 路径）**：17.5s / 25.0s / 39.5s / 52.9s 四次「点「点击继续」（真实 UI 路径）」，
**无超时告警**，教学 66 秒跑完，结束态 `timeScale=1`、输入门只剩 RouteFlow。

### 6.6 引导期「玩家还能碰到别的功能」自查（2026-11-06）

新增自检脚本 `Assets/Scripts/Editor/TutorialInteractionAudit.cs`（Play Mode 里跑），
口径是**在元素屏幕中心做一次 EventSystem 射线，首命中是不是它自己** → 比“按钮亮着”准。

| 采样时机 | 真正能点到的 | 判定 |
|---|---|---|
| 开场流程中（timeScale=0，输入门=TutorialIntro） | **只有 PauseButton** | ✅ 符合设计（只有暂停键可按） |
| 教学拍进行中（Tap@0） | PauseButton + DevSkipBattleButton | ✅ DevSkip 是 `#if DEVELOPMENT_BUILD` 的开发按钮，正式包没有 |
| 三选一 / 结算态 | PauseButton、MainMenuButton、ItemSlot_0(技能槽) | ✅ 技能槽点了没反应（被输入门拦） |

**查出一个真实的洞并已修**：
- **药水/道具路径不走手势系统** → `GameplayInputGate` 拦不到它 →
  `HealthPotionManager.TryUsePotion()` 只查「有没有药水」，**没有** timeScale / 输入门判断
  → 引导期（尤其开场等「点击继续」那段**没有台词在播**）点道具栏会**真的喝掉**。
  修法：`CanUsePotionNow()` + 两个道具栏入口整体拦下（教学「用技能打断」那一拍除外）。
  实测：开场时 `TryUsePotion()=False`、输入门放开时 `TryUsePotion()=True`（药水 1→0）→ 没把正常玩法堵死。
  ⚠️ 现状 `HealthPotionManager.potionDefinition` **是空的** → 药水图标其实进不了道具栏 → 这个洞目前**够不到**，但已按类修好。

### 6.7 ⚠️ 2026-11-06 用户反馈后的重做（暂停时机 / 两段间隔 / 别的 UI 能点）

**反馈三条**：
1. “局内第一段开始暂停的时间完全错误，其应该是原本开始的时间” → 之前把开场做成了**波次开始**就暂停（敌人还在走）。
2. “这两段怎么能分开暂停呢？中间怎么会允许存在间隔？” → 两段之间不能有缝。
3. “当前引导期间还是能点别的 UI”。

**重做后的实现**：

| # | 改动 | 位置 |
|---|---|---|
| 1 | 开场从「波次开始的独立前步骤」**改到「原本推台词的那个位置」** → 统一入口 `PushBeatDialogue()`，所有拍型（点一下 / 连点 / 防御 / 霸体时停 / 技能打断）都在自己原本念台词的那一刻开场 | `TutorialStageDirector.Intro.cs` + `Hints.cs` / `Beats.cs` |
| 2 | 第一段**等它播完** → 立刻出按钮 → 点击 → 立刻播第二段 → 第二段播完 → **立刻进手势教学**（无间隔） | `PlayIntroLine()` 等 `IsShowing` 变 false |
| 3 | 引导期**整个战斗 HUD 失能**：HUD 根 CanvasGroup 置 `interactable=false` + `blocksRaycasts=false`（道具栏 / 技能槽 / 防御键 / 头像全不可点，也不会有按下高亮）；暂停层与开场遮罩层挂 `ignoreParentGroups` 的 CanvasGroup → 不受影响 | `DisableHudInteraction()` / `TutorialInputBlocker` |
| 4 | 「点击继续」+ 引导期点击遮罩放进**同一个 Canvas**（`TutorialIntroOverlay`，order 121，自带 GraphicRaycaster）→ 遮罩深度 -1、按钮深度 0 → 射线优先命中按钮 | 场景 |
| 5 | 暂停层 order 30 → **130**（在遮罩之上 → 暂停键与暂停面板照常可用） | 场景 |

**实测（Play Mode，驱动走真实 UI 点击路径）**：

| 波 | 暂停开始 | 第一段 | 按钮 | 第二段（原本那行） | 进手势教学 |
|---|---|---|---|---|---|
| N1 w0 | 5.32s | 5.33s 「看好了——先别动手…」 | 8.37s | 8.41s 「燕人张翼德在此！点一…」 | 12.57s |
| N1 w1 | 14.29s | 14.31s 「这一波人更多…」 | 16.71s | 16.74s 「连点三下！一枪接一枪…」 | 19.46s |
| E0 w0 | 27.60s | 27.62s 「这回不一样。对面那个…」 | 30.97s | 31.00s 「他这一下带着霸体…」 | 36.27s |
| E0 w1 | 在「技能打断拍」念台词那一刻（那一拍要先等三选一选完） | — | — | — | — |

- 引导期审计（`TutorialInteractionAudit`）：**能点到的只有**「点击继续」+「暂停键」+（开发构建专属的 `SkipBattleButton`）；
  防御键 / 头像 / 道具栏 / 技能槽 **全部命中不到** ✓
- 四波共 4 次真实 UI 点击，**0 次超时放行**；教学 81s 跑完（4 次点击里 E0 w1 那次因驱动在等三选一时把敌人打死、该拍被取消而未发生）。

### 6.8 还没做的

1. **第一段字幕的 4 条文案是草稿** → 待用户改（`tutorial_t0_intro_*` 四个资产）。
2. **真机手感验收**（本轮只在 Editor Play Mode 验过）。
3. **E0 wave1 没有第二段字幕**：该波原本就没有波次台词（`e0Dialogues[1]` 是 null，台词由「技能打断拍」自己给）→ 该波只有第一段 + 「点击继续」。
4. **正式关的招式解锁引导拍没有套这套流程**（决策点 3 → 本轮不做）。
5. **对话层整体不可交互**（`DialogueOverlay` 的 CanvasGroup `interactable=0/blocksRaycasts=0` + 没有 GraphicRaycaster）：
   台词气泡 / 原 `DialogueAdvanceButton` / 点击遮罩依旧点不到 → 2026-11-05 那次「删掉点任意处继续 → 改成点气泡/点继续」实际上**没生效**。
   本轮绕开了它（按钮移出去了），但**建议单独开一轮**修：
   给 `DialogueOverlay` 补 GraphicRaycaster + 处理 CanvasGroup，并把 `PauseLayer` 提到 120 以上（否则遮罩一真生效就会盖住暂停键）。

---

## 6.8 按定稿重做的实施记录（2026-11-06）

### 改了什么

| # | 文件 | 改动 |
|---|---|---|
| 1 | `Assets/Scripts/UI/BattleDialogueView.cs` | 新增 `autoAdvanceEnabled` 开关（开场期间关掉 → 文本不会自己消失） |
| 2 | `Assets/Scripts/Tutorial/TutorialStageDirector.Intro.cs`（重写） | 开场两段：**第一段 + 按键同时出** → 等点击（**无任何计时器**）→ 点击后**立即恢复可操作** → 第二段上屏（不自动消失） |
| 3 | `Assets/Scripts/Tutorial/TutorialStageDirector.Hints.cs` | `EndBeat` 收尾时清文本 + 恢复自动推进；`StopHint` 打断时收尾 |
| 4 | `Assets/Scripts/Tutorial/TutorialStageDirector.cs` | 删掉 `introContinueTimeoutSeconds`（**需求里没有计时器**）；新增 `UpdateRouteTeachingLock()`（路线选择期锁 HUD） |
| 5 | `Assets/Scripts/Tutorial/TutorialStageDirector.Beats.cs` | 所有拍型统一走 `PushBeatDialogue()` → 开场发生在“原本那行台词出现的那一刻” |
| 6 | `Assets/Scripts/Player/InputManager.Pointer.cs` | **触摸路径补“抬手丢失”兼底**（`touchCount==0` 但在跟踪手指时自己补一次抬手，与手势提示层同一处补丁一致）+ 松手日志 |
| 7 | `Assets/Scripts/Player/InputManager.Gesture.cs` | `ProcessComboBranchGate` **不再静默吞松手**：提交不了就交回默认路径 + 打日志 |
| 8 | `Assets/Scripts/Tutorial/TutorialRouteBlackout.cs` | 黑幕**在 ChoosingRoute（等玩家选路牌）时就亮**（之前只在“点完按钮→开始移动”那一小段） |
| 9 | `Assets/Scripts/Route/RouteChoicePanel.cs` | 路牌面板挂 `ignoreParentGroups` 的 CanvasGroup → HUD 失能时路牌仍可点 |
| 10 | `Assets/Scenes/Battle.scene` | HudCard 上新增 CanvasGroup 作为**引导期失能范围**（**不能是 BattleHUD 根**：它会把暂停键和「点击继续」一起锁死）；导演指向它 |

### 实测（Play Mode，驱动走真实 UI 点击路径）

| 波 | 暂停 | 第一段 + 按键 | 点击 | 第二段 + 手势 | 拍完成 |
|---|---|---|---|---|---|
| N1 w0 | 5.29s | 5.30s | 5.42s（立即 ts=1） | 5.44s | 6.10s |
| N1 w1 | 7.61s | 7.63s | 7.96s | 7.98s | 9.04s |
| E0 w0 | 16.59s | 16.60s | 16.80s | 16.82s | 20.56s |

- 第二段文本在 E0 w0 那拍**停留 3.7 秒**直到手势完成 → 确认“不自动消失” ✓
- 引导期可点元素：**只有「点击继续」+ 暂停键**（+ 开发构建的跳过按钮）✓
- 路线选择期：HUD 组 `interactable=False blocks=False` ✓、黑幕亮起 ✓、手势提示层（高亮）showing ✓、路牌仍可点 ✓

### 过程中改掉的两个自造问题

1. **HUD 失能范围取错**：原先锁的是 `BattleHUD(Canvas)` 根 CanvasGroup → 它是**暂停键**和**「点击继续」**的祖先 → `Selectable.IsInteractable()` 被连带置 false → **两个都点不动**（暂停键废掉、继续键点不动 → 只能等超时）。改为锁 **HudCard**（战斗 HUD 本体：防御键 / 道具栏 / 技能槽 / 头像）。
2. **第一段文本与按键不同步**：上一条台词（引导拍尾巴）还留在屏上时，“等文本上屏”会误判 → 按钮先亮、文本后到。改为：先 `view.Skip()` 收掉旧台词，再上第一段。

### 仍需真机确认

- 问题2（蓄力松手）：触摸路径的“抬手丢失”兼底 + 不再静默吞松手 **已落地**，但 Editor 用的是鼠标路径（`Input.GetMouseButtonUp` 是保留状态，天然收得到）→ **必须真机跑一次**确认；真机日志会出现 `[InputManager] TouchRelease ...` 与 `[InputManager] 连段松手：...`。

### 6.9 教学不能被绕过（2026-11-06 用户反馈：「在主菜单不点教学引导直接点开始游戏」）

**两个根因**（R7/D5 在 `plan/tutorial-and-hint-overhaul-plan-20261101.md` 里就记过，一直没修）：

| # | 根因 | 位置 | 修法 |
|---|---|---|---|
| A | 落地页选路线**完全不看 `tutorialCompleted`** → 教学没打完也能直接进正式关 | `FakeRouteRuntime.ResolveActiveRouteFromSave()` → `PrepareForEntry()` | 在 `PrepareForEntry` **入口处**统一拦（主菜单「开始关卡」/ 选关页切关 / 落地页自举都走它）→ 教学未完成则强制换成教学路线 |
| B | `tutorialCompleted` **在第一场战斗（N1）胜利时就置位** → 打完 N1 就被当成“教学完成” → E0 那半段（防御拍 / 三选一 / 技能打断）再进就被跳过 | `TutorialStageDirector.OnStageVictory()` | 置位时机改为**整条教学路线跑完**（`FakeRoutePhase.Completed`）→ `UpdateTutorialRouteCompletion()` |
| C | 教学打完回主菜单，存档的当前关卡可能还指着教学路线 → 又被拉回教学 | `ResolveActiveRouteFromSave()` | 教学已完成时**跳过教学路线**、回到第一条正式路线 |
| D | 主菜单那段教学（进天赋页解锁 C2，R8）只是**提示不拦路** → 玩家可以直接点「开始关卡」跳过 | `BattleStartStageButton.ApplyFromState()` | 「开始关卡」在天赋页教学（`taughtTalentPanel`）走完前**可见但变灰、不可点**（同项目里「三选一其它卡变灰」的做法）；教学关没打完时不算锁（落地页准备的就是教学路线） |
| E | **教学进度被存档记住** → 中途退出后重进直接跳过那些拍（玩家根本没学过） | `TutorialSaveSection` / `FakeRouteRuntime.PrepareForEntry()` | 进教学关时**进度清零**（清「已教过的教学拍」+ 教学路线的存档点）→ **中途退出 = 从头开始**；只保留「整条教学通关」这一个完成标记 |

**实测**（存档：tutorialCompleted=false、当前关卡=正式关#1）：
- 落地页自举 → 准备的是 **教学关·初见#9100** ✓
- 选关页切「长坂坡·卷轴」→ 实际准备 **教学关·初见** ✓
- 教学跑完时间线：N1 胜利（ChoosingRoute）时 `tutorialCompleted=False` → 路线 `Completed` 后才 **True** ✓
- 主菜单「开始关卡」：天赋页教学未走完 → `interactable=False`、alpha 0.45、**真人点击被拦** ✓；走完后可点 ✓
- 中途退出：进场前 `taughtTutorialBeats=[Tap@0,TapCombo@1]` → 进教学关后 **[]**（从头开始）✓；`tutorialCompleted` 仍为 False ✓

> R8 这条链**能走通**（前提已实测）：天赋节点 `talent_move_stabc2` 现在是 `hidden=0` + `coinCost=1` + 有 `moveConfig` → 玩家花 1 铜钱解锁「指向突刺」→ 天赋页教学完成 → 才能点「开始关卡」。

---

## 6.10 2026-11-06 第二轮修改：教学文本的黑幕 + 继续按钮位置（用户定）

> 用户当天的新要求（逐字）：
> ① 「当前教学关开始时有**没必要的黑幕暂停**」；
> ② 「**所有的局内第一段文本教学都没有黑幕，需要补上**，并把**继续按钮挪到玩家血条上方**」。
> 追加拍板：**黑幕盖所有局内教学文本**、**一直压到手势做完**。

### 6.10.1 教学关开始的黑幕暂停 → 去掉

- 根因：开幕台词 `tutorial_t0_opening`（「燕人张翼德在此！」）是 **blocking** 台词 →
  `DialogueManager.TryPlayNext` 里 `_pausedForDialogue = !nonBlocking` → **冻结游戏 + 拉起 `DialogueDimOverlay`（0.55 黑幕）**。
- 改法：把该资产的 `nonBlocking` 改成 `1` → 不暂停、不压暗、不挡点击，台词照播（自动推进）。
- 实测：开场台词上屏那一帧 `timeScale=1.00`、压暗 `alpha=0.00` ✅。

> 连带更正：`plan/tutorial-guidance-architecture-and-fix-plan-20261106.md` §9.2 里
> 「`DialogueDimOverlay` / `Tutorial Blackout` 幕布**从来没显示过**」的说法**不成立** ——
> `DialogueDimOverlay` 就是用户看到的那个黑幕（它会显示）。`Tutorial Blackout` 幕布仍待单独验。

### 6.10.2 局内教学文本统一压暗（黑幕）

| 项 | 内容 |
|---|---|
| 实现 | 不透明黑 `Image` + `CanvasGroup.alpha`（0.55）——**不用半透明 Image**（本项目实测半透明 Image 会画不出来） |
| 层级 | **order=110** 的独立根 Canvas `TutorialTeachingDim`：在台词气泡（DialogueOverlay 120）**之下** → 那行字不被压暗；在「点击继续」按钮（121）**之下** → 按钮不被压暗；在战斗 HUD（21）**之上** → 场景被压暗 |
| 开关点 | 教学文本出现 → `SetTeachingDim(true)`（`PushBeatDialogue` / 开场两段 / 技能拍台词 / 冻结台词）；手势做完 → `EndBeat` → `SetTeachingDim(false)`；被打断 / 离开教学 / 不教手势的波次 → 也撤 |
| **不压的** | 三选一教学台词、路线选择台词、开幕台词 —— 压暗层 order=110 **高于**三选一弹窗 order=100，压上去会把弹窗一起变暗 |

### 6.10.3 「点击继续」按钮 → 玩家血条上方

- 不写死坐标：`TutorialContinueButton._placeAbove` 接战斗 HUD 的 `HealthBar`，
  `Show()` 时按**血条顶边中点（世界坐标）→ 父层局部坐标**换算按钮的 `anchoredPosition`。
- 实测：按钮底边 y=205.9 / 血条顶边 y=178.9 → **在血条上方约 27px**；按钮中心 x=540 = 血条中心（血条跨 114~965）。

### 6.10.4 本轮改动清单

| 文件 | 改动 |
|---|---|
| `Assets/Scripts/UI/TutorialInputBlocker.cs` | 新增 `_dim` + 公开 `SetDim(bool)`；压暗与点击遮罩**解耦**（点完「点击继续」不能撤压暗） |
| `Assets/Scripts/UI/TutorialContinueButton.cs` | 新增 `_placeAbove` + `PlaceAboveAnchor()`（跨 Canvas 换算） |
| `Assets/Scripts/Tutorial/TutorialStageDirector.Rules.cs` | 新增 `SetTeachingDim(bool)` |
| `Assets/Scripts/Tutorial/TutorialStageDirector.Hints.cs` | 开拍处不压暗、收拍（`EndBeat`）撤压暗；`StopHint` / 不教手势的波次 / 拍没开成 也撤 |
| `Assets/Scripts/Tutorial/TutorialStageDirector.Beats.cs` | `PushBeatDialogue` / 技能拍台词 / 冻结台词 → 开压暗 |
| `Assets/Scripts/Tutorial/TutorialStageDirector.Intro.cs` | 开场两段的第一段 → 开压暗 |
| `Assets/Scripts/Tutorial/TutorialStageDirector.cs` | `ReleaseGlobalTutorialState` → 撤压暗 |
| `Assets/Scenes/Battle.scene` | 新增 `TutorialTeachingDim/IntroDim`；`TutorialContinueButton._placeAbove` / `TutorialInputBlocker._dim` 接线；按钮位置 |
| `Assets/ScriptableObjects/Dialogue/tutorial_t0_opening.asset` | `nonBlocking: 0 → 1` |
| `Assets/Scripts/Editor/TutorialIntroSetupTool.cs`（新） | 幂等接线工具：`Tools ▸ 三国杀戮 ▸ 教学关 ▸ 开场 UI 布线` |

### 6.10.5 还没做的

1. **第一段字幕 4 条文案仍是草稿** → 待用户改。
2. **真机表现待验**（压暗、按钮位置、触摸松手）。
3. **三选一教学台词要不要也压暗** → 悬着（压了会把弹窗变暗）。

---

## 7. 相关文档

| 文档 | 关系 |
|---|---|
| `plan/bugs-and-requests.md` BUG-009 | 同一区域的问题（防御键静音 / 点击被吞） |
| `plan/todolist-20261105-interaction-and-feel.md` | 本工作流的待办与验收清单（本文是其中第 1 项） |
| `design/combo-move-state-machine.md` §11.11 | 手感改造后的教学回归重点 |
| `plan/tutorial-hint-overhaul-plan-20261101.md` | 教学/引导层的整体方案（背景） |
| `plan/tutorial-hint-handoff-20261102.md` | 教学线交接（背景） |
| `memory/known-issues.md` §22 | 防御键静音的空窗问题 |
