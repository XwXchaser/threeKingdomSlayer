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

# 教程开场流程重做（2026-11-05 用户定 · 待实施）

> **状态：设计已定，代码未动。** 下一个对话实施。
> 用户原话：「玩家在游戏内的游玩内容是不断的点击屏幕，所以教程应该改为：
> **暂停一段时间，期间播放第一段字幕，玩家无法进行任何交互（除了按左上角暂停键）**；
> **然后再出现点击按钮处继续**；**然后播第二段字幕正式开始教学（与当前一致）**。」

---

## 1. 目标流程（每波开场）

```
[波次开始]
   │
   ├─ ① 暂停：锁输入 + 冻场 ──► 播【第一段字幕】（自动播完，玩家不需要做任何操作）
   │                            玩家此时点屏幕**没有任何反应**；只有左上角暂停键可按
   ├─ ② 「点击继续」按钮出现 ──► 玩家点击（这是此刻唯一能做的事）
   │
   ├─ ③ 播【第二段字幕】─► 字幕结束 → 解除输入锁
   │
   └─ ④ 正式教学：招式解锁引导拍 → 教学拍（**与当前一致，不改**）
```

**为什么**：玩家的默认行为是**不停点屏幕**。当前教程里每一次点击都可能被当成"推进台词 / 出招"，
把教学节拍打乱（这正是 `plan/bugs-and-requests.md` BUG-009 那类问题的温床）。
开场做成**显式三段**，玩家立刻知道"现在该看字 / 现在该点一下 / 现在该打"。

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
| 6 | 「点击继续」能不能跳过？ | 建议**不能**（这一下点击就是教学的一部分：让玩家学会"教程会等我的点击"） |

---

## 5. 验收清单（实施后逐条手测）

1. 进教学 N1 → 波次开始 → **画面停住、第一段字幕出现**；此时**点屏幕任何位置都没反应**。
2. **左上角暂停键仍然能按**（层级已验证：PauseLayer 30 > DialogueLayer 20 ✅，但要实测确认没有被输入锁连带拦掉）。
3. 第一段字幕播完 → **出现「点击继续」** → 点击 → **第二段字幕** → 字幕结束 → **手势引导出现**，这时点屏幕才有反应。
4. 正式教学**与当前一致**：手势示范、防御键静音、拍能收（回归 `design/combo-move-state-machine.md` §11.11 的 5 条回归重点）。
5. 中途退出 / 暂停 / 切场景 → **输入锁必须回收**（`GameplayInputGate` 的 owner 规则会在对象销毁时自动失效 ✅，但要实测）。
6. 正式关的招式解锁引导拍不受影响（除非按决策点 3 一起改）。

---

## 6. 相关文档

| 文档 | 关系 |
|---|---|
| `plan/bugs-and-requests.md` BUG-009 | 同一区域的问题（防御键静音 / 点击被吞） |
| `plan/todolist-20261105-interaction-and-feel.md` | 本工作流的待办与验收清单（本文是其中第 1 项） |
| `design/combo-move-state-machine.md` §11.11 | 手感改造后的教学回归重点 |
| `plan/tutorial-hint-overhaul-plan-20261101.md` | 教学/引导层的整体方案（背景） |
| `plan/tutorial-hint-handoff-20261102.md` | 教学线交接（背景） |
| `memory/known-issues.md` §22 | 防御键静音的空窗问题 |
