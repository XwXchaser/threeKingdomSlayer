---
id: kd_445654b7-c163-4993-9bb9-8c43800c50e5
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_tutorial_system_overview_20261107
summary: 教程系统权威总览（2026-11-07）：设计模型、教学链 R1~R10、六层架构与"新需求放哪层"、一次教学拍的完整时序、统一遮罩系统（MaskRule/MaskService/UiMask/IMaskWorldDim + 三条硬规矩）、输入拦截（GameplayInputGate + 各输入入口）、存档语义表、"要做什么改哪里"扩展指引、反模式清单（带实测原因）、三个 Editor 工具、已知问题与待办。目的：防止后续改动再造一套。
---

# 教程系统总览（权威参考 · 2026-11-07）

> **这份文档的用途**：新增教学需求前**先查这里**，按「要做什么 → 改哪里」的表接入，
> **不要另起一套**。历史上每个需求都长出过新组件（聚光遮罩 / 点击遮罩 / 全屏压暗 / 场景压暗…），
> 2026-11-07 已统一成一套（见 §4）。
>
> 相关：教学线的排期与待办 `plan/todolist-20261105-interaction-and-feel.md`；
> 架构梳理与缺口清单 `plan/tutorial-guidance-architecture-and-fix-plan-20261106.md`；
> 开场两段字幕的实施记录 `plan/tutorial-intro-flow-redesign-20261105.md`。

---

## 1. 设计模型（一句话）

**教学 = 第一关的内容**（不是弹窗教程）。玩家的自由度在「**怎么打**」，不在「**能不能不打**」。

| 类别 | 允许吗 | 代价 |
|---|---|---|
| 战斗操作（出招 / 技能 / 道具） | **只允许这一拍要的那个** | — |
| 其它 UI（道具栏 / 技能槽 / 防御键 / 头像…） | **锁**；只有左上角暂停键永远允许 | — |
| **走开**（退出教学 / 掉线 / 切后台） | **允许** | **从头再来** |

**只记两件事**：① 这条教学路线通没通（→ 解锁正式关）② 主菜单那一步做完没（→ 允许点「开始关卡」）。
**教学内部的拍不记进度** —— 中途退出 = 这一趟白走。

---

## 2. 教学链：需求 → 落点 → 强制手段

| # | 教学链一步 | 落点 | 强制手段（现实现） |
|---|---|---|---|
| R1 | 点一下触发攻击 | N1 w0（`Tap` 拍） | 手势闸门 `TutorialGestureHint.Gate()`（冻结 + `TutorialGate` + 遮罩）+ 招式白名单 `TutorialAttackGate` |
| R2 | 连点三下触发三连 | N1 w1（`TapCombo` 拍） | 同上 + `keepRingVisible` 常驻光圈 |
| R3 | 「多次攻击能打断」只出文字 | N1 w1 收拍后 `afterDialogue` | 非阻塞台词 |
| R4 | 节点移动要有黑幕 | N1→E0 转场 / 选路 | `TutorialRouteBlackout`（统一遮罩系统，order 19 → 在路牌面板之下） |
| R5 | 霸体敌人 → 教按住防御键 → 三选一 | E0 w0（`DefendButton` 拍） | 手势闸门（时停冻结）+ 防御键静音 `DefenseButton.SetMuted` + 三选一**只让选推荐卡** |
| R6 | 用技能打断霸体 | E0 w1（`SkillSlot` 拍） | `GameplayInputGate.AllowSkillActivationWhileBlocked` + 技能槽高亮 |
| R7 | 击杀节点二所有波次 → **结束初始教学** → 回主菜单 | 路线结算 | 置位时机 = 整条教学路线 `Completed`（**不是** N1 胜利） |
| R8 | 主菜单教玩家进天赋页点出蓄力 C2 | 落地页 + 天赋 overlay | 天赋页硬性教学（指节点 → 点解锁 → 真解锁才算完）+「开始关卡」在它走完前变灰不可点 |
| R9 | 点击引导玩家开始关卡 | 落地页 | `HubTutorialGuide`（手 + 文字 + 聚光遮罩）⚠️ **有旧 bug，见 §9** |
| R10 | 新招式（指向突刺）在正式关里怎么触发 | 正式关内 `ComboCharge` 引导拍 | 招式解锁引导拍 + `MoveGuideSaveSection.taughtMoveGuides` |
| — | **教学关不能被绕过**（主菜单 / 选关页） | `FakeRouteRuntime.PrepareForEntry` | 未完成教学 → 强制换成教学路线 |
| — | **中途退出 → 从头开始** | `PrepareForEntry` / `TutorialSaveSection` | 进教学关即清「已教过的教学拍」+ 教学路线存档点 |

---

## 3. 六层架构：每层放什么，新需求放哪层

```
L0 路线层   FakeRouteRuntime / RouteCatalog / FakeRouteStageConfig
            走哪条线、节点推进、解锁关系、存档点
            出口：PrepareForEntry(进关) / RunPrepared(点开始关卡) / FinishRoute / HandleStageDefeat

L1 教学编排层  TutorialStageDirector(.Beats/.Dialogue/.Hints/.Intro/.MoveGuide/.Rules/.Targets)
            把「拍」排进流程 —— 引导拍 / 教学拍 / 开场两段 / 收拍 / 台词 / 存档标记
            关键成员：gestureSteps（拍表）、n1Dialogues/e0Dialogues（波次台词）、
                      n1IntroDialogues/e0IntroDialogues（开场第一段）

L2 拍级门控层  TutorialGestureHint（手势闸门：冻结 + TutorialGate + 遮罩 + 示范）
            TutorialAttackGate（招式白名单：只让出该出的招）
            DefenseButton.SetAvailable/SetMuted（防御键：隐藏 / 压暗不可用 / 是否吃射线）
            UpgradeChoicePopup.SetChoiceSelectable（三选一：只让选推荐卡）

L3 输入层    InputManager（tutorialGestureTarget 手势白名单 / 蓄力 / 松手链）
            GameplayInputGate + InputBlockReason（全局输入门，按 owner 计数）
              owner：RouteTravel / ResultPanel / TutorialGate / TutorialIntro / GuardBroken / LegacyRoute
            触摸路径（HandleTouchInput）与鼠标路径（HandleMouseInput）分离

L4 UI 表现层  BattleDialogueView（气泡 / 继续键 / 全屏 ClickBlocker / 自动推进）
            TutorialSceneDim（**场景压暗**：只压背景，角色与敌人保持原亮度）
            TutorialRouteBlackout（选路/转场时机）+ TutorialBlackoutPointerTrigger
            TutorialGestureStyle（手 / 光圈素材）
            TutorialContinueButton（开场「点击继续」，自带 Canvas+GraphicRaycaster）
            HubTutorialGuide（主菜单引导：手 + 文字 + 聚光遮罩）
            TalentTreePanel.TryStartTeaching（天赋页硬性教学）
            **统一遮罩系统 MaskRule / MaskService / UiMask / IMaskWorldDim**（§4）

L5 进度存档层  SaveManager / SaveData
            tutorialCompleted              教学路线整条通关
            taughtTutorialBeats           「已教过的教学拍」逐拍标记（进教学关即清零）
            taughtMoveGuides              招式解锁引导拍已教
            taughtTalentPanel             天赋页教学已教
            taughtStartStage              开始关卡引导已教
            completedTutorialDialogueIds  已播过的教学对话
            clearedStageIds / fakeRouteSnapshots / unlockedTalents
```

### 3.1 一次「教学拍」的完整时序

```
WaveSpawner.OnWaveStarted
 └ OnWaveStartedRoutine(wave)
    ├ 招式解锁引导拍（若有）→ HintRoutine → PushBeatDialogue
    └ 教学拍 → StartHint → HintRoutine / DefendHoldRoutine / SkillBeatInterruptRoutine
       ├ ① 等敌人站定（“就位”）              ← 开场两段就在这一刻触发
       ├ ② PushBeatDialogue
       │    ├ 教学拍 + 有开场第一段 → PlayTutorialIntro：
       │    │    冻场(timeScale=0) + 输入门(TutorialIntro) + HUD失能 + 点击遮罩
       │    │    第一段文本 + 「点击继续」同时出现 → 等点击（无计时器）→ 点击后恢复可操作
       │    │    → 第二段文本上屏（不自动消失）
       │    └ 否则 → 单句台词（原有行为）
       ├ ③ BeginBeat（手势闸门 Gate()：冻结 + TutorialGate + 遮罩；招式白名单；防御键静音）
       ├ ④ 示范循环 + 等玩家做手势
       └ ⑤ EndBeat（清台词 + 恢复自动推进 + 恢复遮罩/闸门/白名单 + 撤压暗）
```

---

## 4. 统一遮罩系统（**唯一**的遮罩入口）

```
MaskRule  ← 规则：谁要遮、遮多强、哪些 UI 豁免、要不要压背景、过渡时长
   ↓
MaskService（唯一入口，按 owner 管理）
   ├─ UI 侧   → UiMask         （唯一的 UI 遮罩实现：不透明底图 + CanvasGroup.alpha）
   └─ 世界侧 → IMaskWorldDim   （教学用 TutorialSceneDim）
```

| 规则字段 | 作用 | 典型取值 |
|---|---|---|
| `Host` | 遮罩建在哪一层（它所在的 Canvas 决定实际层级） | 选路：`Tutorial Blackout`(19)；开场：`TutorialIntroOverlay`(121)；落地页：引导层(60)；天赋页：`TeachLayer`(41) |
| `Alpha` | 压暗强度 0..1 | 选路 0.55；聚光 0.45 |
| `BlockClicks` | 是否吃射线（挡住同层其它 UI 的点击） | 开场两段 = true；选路 = false（路牌要点得到） |
| `DimWorld` + `WorldBrightness` | 要不要同时压暗**世界背景** | 教学拍 = true / 0.42；其它 = false |
| `Exempt` / `ExemptFilter` | 哪些 UI 保持亮 + 保持可点（**提到遮罩上层**，关掉时放回原位） | 聚光 = 目标按钮 |
| `FadeSeconds` | 淡入淡出（不受 timeScale 影响） | 0.12 |

### 4.1 为什么分「UI 侧 / 世界侧」两个后端

战场是**透视相机 + 世界物件**（长枪是 `MeshRenderer`、敌人是精灵）→ **任何 Overlay 画布都必然盖在角色上面**，
层级无解 → 「背景压暗、敌人与武器保持亮」只能改背景渲染器的亮度（世界侧）。
**但**「遮哪些 UI」完全由规则描述 → 对外**只有一个 API**。

### 4.2 三条硬规矩（都来自实测，违反就出问题）

1. **UI 遮罩底图一律不透明**，透明度只由 `CanvasGroup.alpha` 控制。
   → 半透明 Image 在本项目**渲染不可靠**（运行时新建的半透明黑图整层画不出来）。
2. **遮罩不能压在要教学的东西上面**：要么压背景（世界侧），要么把目标提到遮罩上层（`Exempt`）。
3. **豁免对象只在集合变化时重挂** → 每帧重挂会丢原始父节点，关掉时放不回原位。

### 4.3 现有四类遮罩怎么用（不要再新增第五类）

| 用途 | 写法 |
|---|---|
| 压暗背景、角色保持亮（教学拍） | 规则 `DimWorld=true`（教学由 `TutorialSceneDim` 实现） |
| 全屏压暗 + 某些 UI 保持亮（选路） | 规则 `Alpha=0.55` + `Host` 放在路牌面板**之下** |
| 聚光一个 UI 目标（落地页 / 天赋页） | 规则 `Alpha=0.45, BlockClicks=true` + `Exempt` = 目标按钮 |
| 只挡点击不遮挡（开场两段） | 规则 `Alpha=0, BlockClicks=true`（透明遮罩） |

---

## 5. 输入拦截：三个入口，都要管

| 入口 | 拦截手段 | 注意 |
|---|---|---|
| **手势 / 出招** | `GameplayInputGate.RequestBlock(owner, reason)` + `InputManager.tutorialGestureTarget` | owner 规则：对象销毁时自动失效；**导演被禁用但没销毁**时要显式 `Release` |
| **UI 按钮 / 页签 / 关闭键** | 教学期把战斗 HUD（`HudCard`）整块失能（`CanvasGroup.interactable/blocksRaycasts=false`） | ⚠️ **失能范围不能是 `BattleHUD` 根**：它是暂停键与「点击继续」的祖先 → 一失能两个都点不动（2026-11-06 实测） |
| **道具 / 药水**（不走手势系统） | `HealthPotionManager.CanUsePotionNow()` + 道具栏入口整体拦下 | 教学「用技能打断」那一拍除外 |

`InputBlockReason`：`None / RouteTravel / ResultPanel / TutorialGate / TutorialIntro / GuardBroken / LegacyRoute`。

---

## 6. 存档语义表

| 字段 | 含义 | 什么时候写 | 注意 |
|---|---|---|---|
| `tutorialCompleted` | 教学路线**整条**通关 | 路线 `Completed` 时 | 决定能否进正式关；**不是** N1 胜利就置位 |
| `taughtTutorialBeats` | 「已教过的教学拍」逐拍标记 | 每教完一拍 | **进教学关即清零** → 中途退出 = 从头开始 |
| `taughtMoveGuides` | 招式解锁引导拍已教 | 真的打出去才写 | 引导拍在**正式关**，属于可选内容 → 可保留 |
| `taughtTalentPanel` | 天赋页教学已教 | **真解锁了 C2** 才算 | 不能「解锁了任意节点」就算 |
| `taughtStartStage` | 「开始关卡」引导已教 | 玩家点了开始关卡 | ⚠️ 见 §9 的旧 bug |
| `completedTutorialDialogueIds` | 已播过的教学对话 | 台词播完 | 强制重播的对话不写 |

---

## 7. 「要做什么 → 改哪里」扩展指引（防乱造轮子）

| 我要做… | 改哪里 | **不要** |
|---|---|---|
| 加一句教学台词 | 台词资产（`Assets/ScriptableObjects/Dialogue/`）+ 拍表里的 `beforeDialogue` | 在代码里硬写文本 |
| 加一个教学拍（新手势） | 拍表（`TutorialSetupTool` 生成的 `gestureSteps`）+ `TutorialStageDirector` 里已有的拍型 | 另写一套拍流程 |
| 某一拍多一句/少一句台词 | 拍表的 `beforeDialogue` / `comboPhase2Dialogue` / `afterDialogue` | 改 `BattleDialogueView` 的通用逻辑 |
| 让某个 UI 在教学期不可点 | 教学期把对应 CanvasGroup 失能（参考 `DisableHudInteraction`） | 新加一个遮罩图片挡 |
| 要压暗画面 / 聚光某个 UI | **一条 `MaskRule`**（§4） | 新写一个遮罩组件 |
| 要挡点击（教学期） | `GameplayInputGate` + CanvasGroup 失能 | 用半透明 Image 挡 |
| 要改教学关的敌人/波次 | `Assets/RouteData/Tutorial/Tutorial_*_Battle.asset`（走 `unity_execute` 改 SerializedProperty） | 手改 YAML |
| 要改教学拍表（几拍、顺序） | `TutorialSetupTool`（Tools ▸ 三国杀戮 ▸ 教学关） | 手改场景里的拍表组件 |
| 要给教学加新的**强制手段** | L2 拍级门控层（`TutorialAttackGate` / `DefenseButton` / `UpgradeChoicePopup` 的同类开关） | 在 L1 编排层里写 if |

---

## 8. 反模式清单（**都踩过**）

| 反模式 | 后果 | 正确做法 |
|---|---|---|
| 用**半透明 Image** 做遮罩 | 整层画不出来（逐像素比对才发现） | 不透明底图 + `CanvasGroup.alpha` |
| 用**全屏 UI 遮罩**压暗战场 | 敌人与武器被盖住 → 没法教 | 压背景走 `TutorialSceneDim`；UI 遮罩只遮 UI |
| 遮罩层级高于**要教学的东西** | 路牌按钮/文字被压暗、点不到 | 目标 `Exempt` 提到上层，或把遮罩放到它下面 |
| 锁 `BattleHUD` 根 CanvasGroup | 暂停键 + 「点击继续」一起点不动 | 只锁 `HudCard` |
| `CanvasGroup.interactable=false` 当作「隐藏」 | 子物体的 `Selectable` 连带不可交互 | 要挡点击用 `blocksRaycasts`，要禁按钮用 `interactable`，**两个分开** |
| 每帧重挂豁免对象 | 丢原始父节点 → 关掉时放不回原位 | 集合比对后再重挂 |
| 用 `timeScale=0` 表达「暂停」 | 指针状态判定 / 招架接触帧 / 手势时钟全受影响 | 冻场用 `SetEnemiesHold` + 输入门；真冻结只在开场两段用 |
| 台词自动推进当业务逻辑 | 玩家读不完就被推走 | 教学台词关掉自动推进（`autoAdvanceEnabled`），由拍的生命周期控制 |
| 在 `BattleDialogueView` 里塞教学专属逻辑 | 正式对话被牵连 | 教学差异放 `TutorialStageDirector` |
| 教学行为漏进正式关 | 正式关被锁招式/静音防御键 | 离开教学路线要复位（`ReleaseGlobalTutorialState` / `UpdateAttackGate`） |
| 自动化用 `onClick.Invoke()` 代替真实点击 | 「看着能点、真点不动」的洞全漏 | 用真实 UI 路径（`TutorialContinueButton.ClickViaUi` / 射线首命中 + `IsInteractable`） |
| 在 Locus 驱动的 Play 会话里判断「半透明 UI 画出来没」 | 误判成代码缺陷 | **用正常 Editor Play** 验证；且要**逐像素比对**，肉眼极易看漏 |
| 改教学关后不退出 Play 再重跑 | 同会话重跑污染采样 | 先退出 Play Mode 再进 |
| 跑自动化不备份存档 | 玩家存档被改 | 跑前备份 `player_save`（PlayerPrefs 键），跑完还原 |

---

## 9. 工具

| 工具 | 用途 |
|---|---|
| `Tools ▸ 三国杀戮 ▸ 教学关 ▸ 重建教学关配置`（`TutorialSetupTool`） | 幂等重建教学关的拍表/波次/台词接线（改拍表从这里走） |
| `Tools ▸ 三国杀戮 ▸ 教学关 ▸ 开场 UI 布线`（`TutorialIntroSetupTool`） | 开场两段的按钮位置 / 开幕台词非阻塞 |
| `Tools ▸ 三国杀戮 ▸ 教学关 ▸ 教学遮罩统一布线`（`TutorialMaskSetupTool`） | 遮罩宿主层与层级、清理旧实现残留 |
| `Tools ▸ 三国杀戮 ▸ 教学关 ▸ 引导期可交互元素审计`（`TutorialInteractionAudit`） | 枚举引导期**真能点到**的元素并逐个判定 |
| `Tools ▸ 三国杀戮 ▸ 教学关 ▸ 遮罩与遮挡审计`（`TutorialMaskAudit`） | 跑一遍教学，在暂停时刻抓图 + 打印射线命中顺序 + 各遮罩状态（输出 `Library/Locus/tmp/mask-audit/`） |
| `TutorialFlowDriver`（Play Mode 内调用） | 自动跑完整教学（点击走**真实 UI 路径**） |

---

## 10. 已知问题与待办

| # | 问题 | 状态 |
|---|---|---|
| 1 | **落地页「点开始关卡」引导几乎不会出现**：启动早期有一帧路线不在 `Ready` → `HubTutorialGuide.ResolveMode()` 把 `taughtStartStage` 标成已教 → 之后永远不再教 | **未修**（2026-11-07 审计发现） |
| 2 | 引导拍「松手不出招」（触摸路径抬手丢失 / 连段松手被静默吞） | 修复已落地，**真机未验**（Editor 走鼠标路径复现不了） |
| 3 | 真机（Android）整体验收：压暗、按钮位置、触摸、三选一 | 未做（adb 设备为空） |
| 4 | 场景里有 **2 个 EventSystem**（日志每次刷警告） | 未做 |
| 5 | 对话层的压暗（`DialogueDimOverlay`）与 `_clickBlocker` 未并入统一遮罩系统 | 未做（不属教学线；要并就按 §4 的规则写一条） |
| 6 | 三选一教学的波末弹出 / 顺序 / 卡面 | 用户指定 later |
| 7 | 开场第一段的 4 条字幕文案仍是草稿 | 待用户改 |
