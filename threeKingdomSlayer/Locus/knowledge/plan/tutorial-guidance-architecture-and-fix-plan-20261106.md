---
id: kd_c70f8bf8-0281-4391-a01a-96992f92ee32
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_tutorial_arch_20261106
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 引导教学设计架构梳理 + 修复方案（2026-11-06）

> 本文只做**架构梳理与修复方案**（不动代码）。
> 关联：`plan/tutorial-and-hint-overhaul-plan-20261101.md`（需求 R1~R10 / D1~D6 的原始拆解）、
> `plan/tutorial-intro-flow-redesign-20261105.md`（开场两段字幕的实施记录）、
> `plan/handoff-next-session-20261105.md`（跨会话交接）。
> 规则：需求没描述的实现一律不落地（`memory/user-preference.md`）。

---

## 0. 设计模型（一句话）

**教学 = 第一关的内容**（不是弹窗教程）。玩家的自由度在「怎么打」，不在「能不能不打」。

| 类别 | 允许吗 | 代价 |
|---|---|---|
| 战斗操作（出招 / 技能 / 道具） | **只允许这一拍要的那个** | — |
| 其它 UI（道具栏 / 技能槽 / 防御键 / 头像…） | **锁**；只有左上角暂停键永远允许 | — |
| **走开**（退出教学 / 掉线 / 切后台） | **允许** | **从头再来** |

**只记两件事**：① 这条教学路线通没通（→ 解锁正式关）② 主菜单那一步做完没（→ 允许点「开始关卡」）。
**教学内部的拍不记进度** —— 中途退出 = 这一趟白走。

> 对照其它游戏的四种范式：① 教学=第一关（Hyperlight Drifter / 空洞骑士 / 奥日）✅ 本作走这条；
> ② 强制首关+关卡解锁（银河战士 / 塞尔达）✅ 本作已有（`prerequisiteStageIds`）；
> ③ 分段强制+蒙层高亮（手游通用）✅ 主菜单那段用这个；
> ④ 软引导·只提示不拦路（欧美常见）❌ **本作原来的问题就是这条**。

---

## 1. 教学链 × 强制手段 × 现状

需求原文见 `plan/tutorial-and-hint-overhaul-plan-20261101.md` §一（R1~R10）。

| # | 教学链一步 | 落点 | 强制手段（现实现） | 状态 |
|---|---|---|---|---|
| R1 | 节点一 内容1：点一下触发攻击 | N1 w0（`Tap` 拍） | 手势闸门 `TutorialGestureHint.Gate()`（冻结 + `TutorialGate` + 遮罩）+ 招式白名单 `TutorialAttackGate` | ✅ |
| R2 | 节点一 内容2：连点三下触发三连 | N1 w1（`TapCombo` 拍） | 同上 + `keepRingVisible` 常驻光圈 | ✅ |
| R3 | 「多次攻击能打断」只出文字 | N1 w1 收拍后 `afterDialogue` | 非阻塞台词 | ✅ |
| R4 | 节点移动要有黑幕 | N1→E0 转场 | `TutorialRouteBlackout`（幕布 alpha 0.55，不拦射线）+ `TutorialBlackoutPointerTrigger`（按下路牌即黑） | ✅ 本轮把 ChoosingRoute 也纳入 |
| R5 | 节点二 内容1：霸体敌人 → 暂停教按住防御键 → 三选一 | E0 w0（`DefendButton` 拍） | 手势闸门（时停冻结在蓄势完成）+ 防御键静音 `DefenseButton.SetMuted` + 三选一**只让选推荐卡** `forceRecommendedChoiceOnly` | ✅ |
| R6 | 节点二 内容2：用技能打断霸体 | E0 w1（`SkillSlot` 拍） | `GameplayInputGate.AllowSkillActivationWhileBlocked`（拦截期间允许放技能）+ 技能槽高亮 | ✅ |
| R7 | 击杀节点二所有波次 → **结束初始教学** → 回主菜单 | 路线结算 | 置位时机必须是**整条路线 `Completed`** | ✅ 本轮修（原为「N1 胜利就置位」） |
| R8 | **主菜单教玩家进天赋页点出蓄力 C2 stab** | 落地页 + 天赋 overlay | 天赋页硬性教学 `TalentTreePanel.TryStartTeaching`（步骤0 指节点 → 步骤1 指「解锁」→ 真解锁才算完）+ **「开始关卡」在它走完前变灰不可点** | ✅ 本轮补了「开始关卡」的门 |
| R9 | 点击引导玩家开始关卡 | 落地页 | `HubTutorialGuide`（手 + 文字，Mode.Stage） | ✅（提示层，不拦路是设计） |
| R10 | 新招式（指向突刺）在正式关里怎么触发 | 正式关内 `ComboCharge` 引导拍 | 招式解锁引导拍（`BuildMoveGuideSteps`）+ `MoveGuideSaveSection.taughtMoveGuides` | ✅（但见 G4） |
| — | **教学关不能被绕过**（主菜单 / 选关页） | `FakeRouteRuntime.PrepareForEntry` | 未完成教学 → 强制换成教学路线 | ✅ 本轮修 |
| — | **中途退出 → 从头开始** | `PrepareForEntry` / `TutorialSaveSection` | 进教学关即清「已教过的教学拍」+ 路线存档点 | ✅ 本轮修 |

---

## 2. 架构分层（谁负责哪一层）

```
L0 路线层   FakeRouteRuntime / RouteCatalog / FakeRouteStageConfig / RouteCatalogData
            职责：走哪条线、节点推进、解锁关系、存档点
            状态：Ready → EnteringNode → Battle → ChoosingRoute → FakeMoving → WaitingReward → Completed / Defeated
            出口：PrepareForEntry(进关) / RunPrepared(点开始关卡) / FinishRoute(结算) / HandleStageDefeat(退出或战败)

L1 教学编排层  TutorialStageDirector(.Beats/.Dialogue/.Hints/.Intro/.MoveGuide/.Rules/.Targets)
            职责：把「拍」排进流程 —— 引导拍 / 教学拍 / 开场两段 / 收拍 / 台词 / 存档标记
            关键成员：gestureSteps（拍表）、n1Dialogues/e0Dialogues（波次台词）、n1IntroDialogues/e0IntroDialogues（开场第一段）

L2 拍级门控层  TutorialGestureHint（手势闸门：冻结 + TutorialGate + 遮罩 + 示范）
            TutorialAttackGate（招式白名单：只让出该出的招）
            DefenseButton.SetAvailable/SetMuted（防御键：隐藏 / 压暗不可用 / 是否吃射线）
            UpgradeChoicePopup.SetChoiceSelectable（三选一：只让选推荐卡）

L3 输入层    InputManager（tutorialGestureTarget 手势白名单 / 蓄力 / 松手链）
            GameplayInputGate + InputBlockReason（全局输入门，按 owner 计数）
              owner：RouteTravel / ResultPanel / TutorialGate / TutorialIntro / GuardBroken / LegacyRoute
            触摸路径（HandleTouchInput）与鼠标路径（HandleMouseInput）分离

L4 UI 表现层  BattleDialogueView（气泡 / 继续键 / 全屏 ClickBlocker / 自动推进）
            TutorialSceneDim（场景压暗，角色不压暗）
            TutorialRouteBlackout（幕布）+ TutorialBlackoutPointerTrigger
            TutorialGestureStyle（手 / 光圈素材）
            TutorialContinueButton + TutorialInputBlocker（本轮新增：开场两段用）
            HubTutorialGuide（主菜单引导：手 + 文字，Mode.Talent / Mode.Stage）
            TalentTreePanel.TryStartTeaching（天赋页硬性教学）

L5 进度存档层  SaveManager / SaveData
            tutorialCompleted              教学路线整条通关
            taughtTutorialBeats           「已教过的教学拍」逐拍标记
            taughtMoveGuides              招式解锁引导拍已教
            taughtTalentPanel             天赋页教学已教
            taughtStartStage              开始关卡引导已教
            completedTutorialDialogueIds  已播过的教学对话
            clearedStageIds / fakeRouteSnapshots / unlockedTalents
```

### 2.1 一次「教学拍」的完整时序（L1↔L2↔L3↔L4）

```
WaveSpawner.OnWaveStarted
 └ OnWaveStartedRoutine(wave)
    ├ 招式解锁引导拍（若有）→ HintRoutine → PushBeatDialogue → (L1)
    └ 教学拍 → StartHint → HintRoutine / DefendHoldRoutine / SkillSlotInterruptRoutine
       ├ ① 等敌人站定（“就位”）                          ← 开场两段就在这一刻触发
       ├ ② PushBeatDialogue
       │    ├ 教学拍 + 有开场第一段 → PlayTutorialIntro：
       │    │    冻结(timeScale=0) + 输入门(TutorialIntro) + HUD失能 + 点击遮罩 + 关台词自动推进
       │    │    第一段文本 + 「点击继续」同时出现 → 等点击（无计时器）→ 点击后恢复可操作 → 第二段文本上屏
       │    └ 否则 → 单句台词（原有行为）
       ├ ③ BeginBeat（手势闸门 Gate()：冻结 + TutorialGate + 遮罩；招式白名单；防御键静音）
       ├ ④ 示范循环 + 等玩家做手势
       └ ⑤ EndBeat（清台词 + 恢复自动推进 + 恢复遮罩/闸门/白名单）
```

---

## 3. 进度数据：现在 vs 应该

| 存档位 | 现在的语义 | 问题 | 应有语义 |
|---|---|---|---|
| `tutorialCompleted` | 路线整条通关时置位（本轮已修） | — | ✅ 保持 |
| `taughtTutorialBeats` | **每教完一拍就写** | 中途退出后重进会跳过那些拍 → 玩家没真学过 | **进教学关即清零**（本轮已改）→ 只在本次进关内有效 |
| `taughtMoveGuides` | 引导拍教完就写 | 引导拍在**正式关**里，属于可选内容 → 可保留 | ✅ 保持 |
| `taughtTalentPanel` | 天赋页教学真解锁了才写 | 现在只要求「解锁了任意一个节点」→ 可能没解锁 C2 也算完成 | 建议：**要求解锁的正是 C2**（`talent_move_stabc2`）→ 见 G3 |
| `taughtStartStage` | 进过战斗就写 | 只用于不再显示“点开始关卡”的指引 | ✅ 保持 |
| `completedTutorialDialogueIds` | 播过的教学对话 | 强制重播的对话不写 → 无影响 | ✅ 保持 |

---

## 4. 缺口清单（按严重度）

| # | 现象 | 根因 | 涉及文件 | 修复方向 |
|---|---|---|---|---|
| **G1** | 引导期玩家还能点别的 UI（道具栏 / 技能槽 / 头像 / 防御键） | 教学期的 UI 失能只做了「手势层」+「招式白名单」，**UI 层没有统一锁**；对话层自己的 `ClickBlocker` 因 `DialogueOverlay` 缺 `GraphicRaycaster` 且其 `CanvasGroup(interactable=0,blocksRaycasts=0)` 而**完全失效** | `BattleDialogueView` / `DialogueOverlay` 的 CanvasGroup / 各 UI 入口 | ① 教学期给**战斗 HUD（`HudCard`）**整块失能（已做，开场段）→ 推广到**所有教学拍**；② 给 `DialogueOverlay` 补 `GraphicRaycaster` 并处理其 `CanvasGroup`，让 `ClickBlocker` 真正生效（**注意：必须同时把 `PauseLayer` 提到遮罩之上，否则遮罩会吃掉暂停键**） |
| **G2** | 「点击继续」按钮曾经点不动 | `DialogueOverlay` 的 `CanvasGroup.interactable=0` → 子物体的 `Selectable.IsInteractable()` 连带为 false；且按钮自带 Canvas → 与遮罩分属两个 GraphicRaycaster，命中顺序不可控 | `DialogueOverlay` 的 CanvasGroup / 按钮的 Canvas | 已修（按钮移出该子树 + 自带 Canvas/GraphicRaycaster）；**G1 的第②项若实施，必须沿用同一套做法** |
| **G3** | 天赋页教学「解锁了任意节点」就算完成 → 可能没解锁 C2 就放行了 | `TalentTreePanel.FindTeachingTarget` 选的是「任意可解锁节点」（优先招式页） | `TalentTreePanel` | 教学目标**锁定到 C2**（`talent_move_stabc2`），其余节点在教学期间不可选（同三选一「只让选推荐卡」的做法） |
| **G4** | 指向突刺的引导拍：松开蓄力不出招，要再按一下 | 松手链（`InputManager.ProcessGesture` 的 5 道门）任一道都能**静默吞掉**松手；触摸路径在「抬手那帧 touchCount 归零」时会整帧丢掉松手 | `InputManager.Pointer.cs` / `InputManager.Gesture.cs` | 已补：触摸路径补抬手兜底 + 不再静默吞 + 日志；**真机验证仍缺**（Editor 走鼠标路径复现不了） |
| **G5** | 引导拍阶段 1 的判定（历史 bug P1-2） | 已由 phase 计数（`_comboChargePhase`）修正 → 阶段1 的命中只推进阶段、不收拍 | `TutorialStageDirector.Beats.cs` | ✅ 已修（待复核） |
| **G6** | 对话层整体不可交互（气泡 / 原「继续」键 / 全屏遮罩都点不到） | 同 G2 的 `CanvasGroup` + 缺 `GraphicRaycaster` | 同 G1② | 与 G1② 一起处理；**这是 2026-11-05「不点任意处继续」那次改动没生效的原因** |
| **G7** | 场景里 2 个 EventSystem（每次刷警告） | 重复的 EventSystem | 场景 | 清理一个（低优先，与教学无关） |
| **G8** | 3选1 教学（波末弹出 / 教学顺序 / 卡面） | — | `UpgradeChoiceManager` / `TutorialStageDirector.OnChoicesReady` | **用户指定：先修完上面三个再动** |

---

## 5. 修复方案（分批 · 含依赖 / 风险 / 验收）

> 每批都要求：**先写清验收口径 → 落地 → Play Mode 回归 → 用户验收**。

### P0 批次：教学期的统一「锁」（本轮已落地一半，需推广）

| 项 | 内容 | 风险 | 验收 |
|---|---|---|---|
| P0-1 ✅ | 开场两段：冻结 + 输入门 + **HUD（HudCard）整块失能** + 点击遮罩 + 关台词自动推进 | 已验 | 引导期可点元素只剩「点击继续」+ 暂停键（审计脚本实测） |
| P0-2 ✅ | 教学路线不可绕过（主菜单 / 选关页） | 已验 | 教学未完成 → 落地页与选关页都强制回教学关 |
| P0-3 ✅ | 中途退出 → 从头开始（进教学关清进度） | 已验 | 进场前 `[Tap@0,TapCombo@1]` → 进场后 `[]` |
| P0-4 ✅ | 主菜单「开始关卡」在天赋教学走完前变灰不可点 | 已验 | `interactable=False`、alpha 0.45、真人点击被拦 |
| **P0-5** | **把 HUD 失能推广到所有教学拍**（不只是开场两段） | 防御键在「教按防御键」那一拍必须可用 → 失能范围要与 `DefenseButton.Available` 协调 | 每一拍跑一遍：只允许该拍要求的那个操作 |

### P1 批次：对话层的交互层修好（G1② / G2 / G6）—— **已决策并落地（安全部分）**

| 步骤 | 内容 | 状态 |
|---|---|---|
| P1-1 | `DialogueOverlay` 补 `GraphicRaycaster`；其 `CanvasGroup` 改为 `interactable=1 / blocksRaycasts=1` | ✅ 已落地 |
| P1-2 | `BattleDialogueView.IsOnActiveBubble`：气泡**本体**也算“点在气泡上”（原来 `IsChildOf` 不含自身 → 点气泡框不推进） | ✅ 已落地 |
| P1-3 | `ClickBlocker`：**`raycastTarget = false`**（不再抢点击） | ✅ 已落地 |
| P1-4 | **不做**：让全屏遮罩真的挡住世界点击 | ❌ **已决策不做**（见下方影响分析） |

**P1-4 为何不做（影响分析）**：
- 遮罩一旦真生效，会**连带把暂停键吃掉** —— 实测：遮罩在 order 99、暂停键在 order 130，但**跨 GraphicRaycaster 的命中顺序不按 sortingOrder 走**（EventSystem 的合并对不同 module 返回 0，顺序不稳定）→ 实测遮罩赢了，暂停键点不到。
- 同样会**连带挡住三选一弹窗**（弹窗 order 100）→ 教学的三选一就点不动。
- 而“对话期间玩家不许干别的”这件事**已经由输入门 + HUD 失能覆盖**（教学拍实测：除该拍要的交互外，其余全不可点）。
- → 结论：**“挡住世界”由输入门负责，不靠全屏图片**；全屏图片只负责压暗视觉。

**P1 验证结果（Play Mode）**：
- 气泡位置射线：`Text(120), TutorialBubble(120)` → **气泡能被射线命中** ✓
- 「继续」键射线：`DialogueAdvanceButton(120)` → **可点，且真实点击能推进台词** ✓
- 战场空处：`DialogueDimOverlay(20)` → 点击不漏到战场 ✓
- 遗留（**已知、待真机**）：**阻塞型台词期间暂停键会被对话层的压暗遮罩挡住**（order 20 抢在暂停层 130 前）—— 这是**改造前就有的**（对话层缺 GraphicRaycaster 时也一样被 `DialogueDimOverlay` 吃掉），要修得让压暗遮罩不吃射线（代价：Boss 台词期间玩家可以攻击战场）→ 列入待定。

### P2 批次：内容与手感（G3 / G4 / G8）
| 项 | 内容 | 状态 |
|---|---|---|
| P2-1 | **G3 = 需求已写明**：天赋页教学的目标就是**指向突刺（C2）**（R8「点出蓄力 C2 stab」）；解锁它之后，正式关里用**「连招蓄力」引导拍**教这个招（R10 / D4：点一下 → 按住蓄满 → 松手 → 打出 `Zhangfei_StabR2`）。实现：教学目标**锁定到 C2**（判据不写死 id：**教学手势 = ComboCharge** 的那个节点，与 `MoveGuideService` 同一套“数据决定”口径）＋ 教学期间**其它节点不可点**（同「三选一只让选推荐卡」的做法） | ✅ 已落地 |
| P2-2 | G4 真机验证（触摸松手） | ⚠️ **待真机**：真机日志会出现 `[InputManager] TouchRelease ...` / `[InputManager] 连段松手：...` |
| P2-3 | G8 三选1 教学调整 | 用户指定later |

### P1.5 批次：教学引导的**聚光遮罩**（2026-11-06 用户明确要求）

> 需求（用户原话）：
> ① 「主菜单你还是没有按照**不能点其他 UI**的需求去实现，**没有遮罩和对应高亮**」
> ② 「遮罩**不应该覆盖需要点击的按钮**，且**灰掉开始游戏按钮毫无意义**」
> ③ 「天赋页面内**没有遮罩和禁止其他操作**（这种教学引导是**通用准则**，怎么到别的界面就不遵守了？）」

| 项 | 内容 | 状态 |
|---|---|---|
| P1.5-1 | `TutorialClickMask` 改成**聚光型**：4 块面板围出一个**洞**，洞 = 当前高亮目标的屏幕矩形 → **目标不被覆盖、直接可点**（不需要“转发点击”那套） | ✅ 已落地 |
| P1.5-2 | 遮罩挂在**宿主层**下（落地页 = 引导层；天赋页 = 面板的 `TeachLayer`）→ 同一 Canvas 内用深度决定前后（跨 Canvas 的命中顺序不可靠） | ✅ 已落地 |
| P1.5-3 | 引导层补 `GraphicRaycaster`（没有它 → 遮罩收不到射线 = 等于没有） | ✅ 已落地 |
| P1.5-4 | **撤掉**「开始关卡按钮变灰」那套（用户：毫无意义）→ 强制力改由**遮罩**提供（指向天赋入口时，开始按钮被遮罩挡住 → 点不到） | ✅ 已落地 |
| P1.5-5 | **天赋页用同一块遮罩**（通用准则）：教学期间只允许点**被高亮指着的那个**（C2 节点 / 解锁按钮 / 返回按钮），页签、其它节点全部点不到 | ✅ 已落地 |
| P1.5-6 | 天赋页开着时**收起**落地页那条引导（否则那只手会浮在天赋面板上指着一个被挡住的东西） | ✅ 已落地 |
| P1.5-7 | 教学目标 = **指向突刺（C2）**；C2 已解锁时**不拿别的节点凑数** → 直接判定这段教学已达成 | ✅ 已落地 |
| P1.5-8 | ★ **不用“高亮材质”**（用户：「本质上来说确实不需要高亮材质，而是让对应按钮显示在遮罩上层即可」）→ **手 / 光圈不再画**，高亮效果 = **遮罩在目标按钮处开洞**（按钮原样亮着 = 显示在遮罩上层）；洞的范围取**可点的那个按钮**（不是它的图标） | ✅ 已落地 |

**实测**：
- 落地页：洞的目标 = `HubTalentEntry`（整个按钮）→ 天赋入口位置射线 `Plate(25)`（**洞没盖住**）；「开始关卡」位置 → `MaskLeft(60)` 在前（**被挡住**）；页签栏 → 被挡住 ✓；手/光圈 `active=False` ✓
- 天赋页：洞的目标 = `Node_talent_move_stabc2`（C2）→ 该位置射线 `Node_talent_move_stabc2(41)`（**洞没盖住**）；页签区 / 面板中央 → `MaskLeft/MaskRight(41)`（**被挡住**）✓；手/光圈 `active=False` ✓
- 点「开始关卡」（穿洞）→ 路线阶段 `Ready → Battle` ✓（目标本身可点）

### P3 批次：清理（G5 复核 / G7）

---

## 6. 本轮已落地（不再重复排期）

见 `plan/tutorial-intro-flow-redesign-20261105.md` §6.8 / §6.9；改动共 31 个文件，**未提交**。

---

## 7. 验证手段（可重复）

| 工具 | 用途 |
|---|---|
| `Assets/Scripts/Editor/TutorialInteractionAudit.cs`（Tools ▸ 三国杀戮 ▸ 教学关 ▸ 引导期可交互元素审计） | Play Mode 里枚举「真能点到」的元素并逐个判定（射线首命中 + `IsInteractable` + CanvasGroup 链） |
| `Assets/Scripts/Editor/TutorialFlowDriver.cs` | 自动跑完整教学（点击走**真实 UI 路径**，不是直接调回调） |
| `Assets/Scripts/Editor/TutorialSetupTool.cs`（Tools ▸ 三国杀戮 ▸ 教学关） | 重建教学关配置（幂等） |
| 存档备份 / 还原 | `Library/Locus/tmp/player_save_backup_user_c2.txt`（用户档）；每次测完必须还原 |

---

## 8. 已决策（不再作为疑问）

| # | 问题 | 决策 | 依据 |
|---|---|---|---|
| 1 | P1 批次要不要现在做 | **做**（安全部分 P1-1~P1-3）；**不做** P1-4（全屏遮罩真挡世界） | 11-05 需求「点气泡或点继续才推进」至今未生效；P1-4 的影响已实测（会连带挡暂停键/三选一） |
| 2 | P0-5 的锁粒度 | **不用新代码** —— 实测现有架构已按拍锁好（TapCombo 拍只剩暂停键；防御拍多一个防御键；技能槽由输入门放行、药水被拦） | Play Mode 审计实测 |
| 3 | G3 的口径 | **教学目标 = 指向突刺（C2）**（需求 R8/D3 写明）；后续教 = 正式关的「连招蓄力」引导拍（R10/D4） | 需求原文 |
| 4 | 用户当前存档是否清掉重来 | **不动**（存档保持原样）；需要重来时再说 | — |

---

## 9. P1.5 聚光遮罩：定稿方向（2026-11-06 用户口述改向）

### 9.1 用户定规（取代原「四块挖洞」方案）

> 「做的什么玩意儿？你就把**字幕板**和**对应按钮**层级**提到遮罩上**不就行了，还做手动切分遮罩？」

→ 遮罩 = **一整块**（铺满引导层）；**字幕板**与**要点的那个按钮**提到遮罩**上层** → 不被压暗、射线也先命中它；其余区域压暗且点不到。**不做**把遮罩切成四块挖洞。

**已落地（`Assets/Scripts/UI/TutorialClickMask.cs` 重写 + `HubTutorialGuide.cs`）**
- 遮罩 = 一整块全屏 `Image`（铺满宿主层，宿主层子对象**第一位**）。
- `SetTarget(目标)`：把目标**提到宿主层**（`SetParent(host, true)` 保留世界位置 + `SetAsLastSibling()`），关闭时**放回原父节点与原序号**。
- 字幕板本就在宿主层 → 自然在遮罩之上。
- **已实测**：目标按钮（天赋卡）**不被压暗、且可点** ✓。

### 9.2 这一轮踩到的坑（实测，2026-11-06）—— **结论已修正：遮罩本身是好的**

**排查过程中一度以为「运行时新建的半透明 UI 在本项目里画不出来」**：
- 现象：`Image` 的 `color.a = 0.45` → **逐像素比对等于没画**；改成 `a = 1` 却能正常显示（最容易误判成"遮罩根本没做"）。
- 逐项排查已排除：材质 / Shader / 顶点色 / Canvas / Mesh / 深度 / 尺寸 / 创建帧 / 启用重注册 / `ForceUpdateCanvases` / 场景预置对象 —— 全都不是原因。
- **最终定位（用户 2026-11-06 在正常 Editor Play 下复验）**：压暗**正常显示** ✓ →
  上面那个现象是 **Locus 驱动 Play 会话**的副作用（同一会话里用 `unity_execute` 片段当场建的同款面板能显示，
  游戏代码建的不能显示 → 差别在批次时序），**不是代码缺陷**。
  → 教训已写进 `memory/tool-result-verification.md`（末节）：**验证 UI 渲染要用正常 Editor Play，
  别在 Locus 会话里下"画不出来"的结论**。
- **实现上仍然保留的两条硬约束**（无副作用、且更稳）：
  1. 压暗用 **`CanvasGroup.alpha` + 不透明黑底图**（比半透明 Image 稳）。
  2. 遮罩在**玩家帧之外**创建（Editor: `EditorApplication.update`；真机: `WaitForEndOfFrame`）。
- 附带发现（**待单独查**）：场景里的 `DialogueDimOverlay`、`Tutorial Blackout` 幕布**也从没显示过**，
  可能同源（都是半透明 Image）→ 值得单独用正常 Editor Play 验一次。

### 9.3 状态与下一步

**状态（2026-11-06）：聚光遮罩已实现，Editor 里压暗正常 ✓（本轮收口，后续在别的对话继续）。**
- 落地页引导：遮罩 + 字幕板在遮罩上 + 目标按钮提到遮罩上（不被压暗、可点）✓。
- 改动文件：`Assets/Scripts/UI/TutorialClickMask.cs`（新）、`Assets/Scripts/UI/HubTutorialGuide.cs`、`Assets/Scripts/UI/TalentTreePanel.cs`。**未提交。**

**下一步**
1. 天赋页（`TalentTreePanel`）沿用同一套「提到遮罩上层」逻辑：`TeachLayer` 即宿主层；
   ⚠️ 注意天赋页的宿主层在**面板内部** → 遮罩要能盖住面板外的部分，层级顺序要单独确认。
2. 单独查 `DialogueDimOverlay` / `Tutorial Blackout` 幕布为什么不显示（同源疑点）。
3. 遮罩的**实机**表现（Android）待验。
