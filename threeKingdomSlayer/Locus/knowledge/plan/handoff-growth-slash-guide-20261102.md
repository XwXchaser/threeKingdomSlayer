---
id: kd_ee2a72b2-c8a3-41ac-9491-27ef2bcfe9a6
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 交接 todolist · 局外成长 / 天赋页 / SlashC2 表现（2026-11-02 收尾）

> **给下一个对话**：先读本文 §1（现状快照）与 §3（已确认事实），再按 §5 的顺序做。
> 配套文档：`design/talent-tree-architecture.md` · `design/talent-page-ui.md` · `design/in-run-growth-and-unlocks.md` ·
> `design/stage-configuration.md` · `design/move-split-and-derivatives.md` · `design/combo-zhangfei-slash-c2.md` ·
> `design/art-asset-brief-20261102.md`（**已作废**，见 §3.6）
> 裁决与差异：`plan/talent-scheme-alignment-20261102.md`

---

## 0. 📍 下一轮从这里开始（2026-11-04 收工状态）

> **本轮的完整交付物：设计文档 `design/tutorial-guides-spec-20261104.md`（所有引导的唯一口径）。**
> **改任何引导之前先读它** —— 本文 §3.2 曾经写错过教学机制（说 Tap 教学用 `Gate`），照着做返工了两轮。

### 0.1 本轮做完的（**全部未提交**，工作区有改动）

| 线 | 结果 |
|---|---|
| **教学→天赋→招式教学 全链路** | ✅ 一次连续跑通：教学关(≈140s) → 主菜单引导 → 天赋页教学 → 解锁指向突刺 → 正式关「连招蓄力」教学拍 → 打出 `Zhangfei_StabR2` → 标记已教 |
| **解锁引导改成教学拍** | ✅ 与 Stab 教学**完全同一条机制**（等敌人到位 → 冻结 → 示范 → 玩家做出手势 → **招式打中敌人** → 收拍 → 补台词） |
| **主菜单引导** | ✅ 新组件 `HubTutorialGuide`：教学关打完→指「天赋」卡；天赋教完→指「开始关卡」；进战斗后不再出现（存档标记 `taughtStartStage`） |
| **天赋页教学** | ✅ 面板自绘（手+光圈+文案）：指节点图标 → 指确认按钮 → **指「返回」**，点返回才算教完（存档标记 `taughtTalentPanel`） |
| **连招蓄力拍的冻结** | ✅ 窗口打开时冻结**招式状态机时钟** + 冻结持枪姿态 + 每帧重声明 `BeginComboChargeHold()`（长枪一直举在蓄势位） |
| **报错** | ✅ 修 `PixelHitEffectManager` 回收已销毁对象 + `GameplayPause` 一条坏 Tween 中断整批恢复 |
| **正式关不教三选一** | ✅ `OnChoicesReady` 加 `IsOnTutorialRoute()` 门槛 |

### 0.2 下一轮的三件事（按优先级）

| # | 事项 | 说明 |
|---|---|---|
| **1** | **T7 音效（唯一还开着的功能缺口）** | ① 敌人攻击的 `_enemyAttackTelegraphClip` 现在指到 `QTE_block1`（QTE UI 音）→ 需要一个**「挥武器」音效**（库里没有 → 要新素材）；② `PlayerState.SetDefend` 里 `PostEvent("Player_Parry")` → 启用防御不该播招架音。**用户 2026-11-04 明确要求放到 todolist** |
| **2** | **正式关数值（用户说「先别管」）** | 已查清：单只敌人硬度是教学关的 **4.7~6.1 倍**（倍率按「中期玩家」推的，实际开局玩家没有那些招式）；铁律八（波次 30~55s）**实测 30.9s 合规**。建议方案 D：波次总血量不变，把血量从「集中在 1~3 只」摊到「更多只」 |
| **3** | **T4 Q4（等真人实测）** | 同方向连斩过快 —— 交接文档已写明「不要猜」，等真人测两组数据（`[SlashDiag]` 日志已就位） |

### 0.3 下一轮要注意的坑（都已踩过）

1. **别再信「交接文档里的机制描述」** —— §3.2 写错过（教学拍**不用** `Gate`）。以 `design/tutorial-guides-spec-20261104.md` 为准。
2. **调试注入会绕过教学闸门**（`SubmitGestureForDebug`）→ 机器人会在教学拍里乱出招 → 已在 `InputManager` 里补上闸门校验。
3. **教学导演常驻 + 切场景重建** → 「只跑一次」的标志位会复位 → 教学行为必须**自己判路线**。
4. **TMP 文字要有带中文字形的字体**（`TMP_Settings.defaultFontAsset` 是 LiberationSans → 中文空白）。
5. **引导锚点用 `rect.center`（视觉中心）**，不要用 `rectTransform.position`（那是 pivot，很多 UI 的 pivot 在角上）。
6. **带旋转动画的手，位置必须每帧反算**，否则指尖会甩离目标。

---

## 1. 现状快照

| 项 | 值 |
|---|---|
| 分支 | `feat/stage-select-talent-overlays`（本地 = 远端 = `e2426257` 之后陆续有提交，**开工前先 `git fetch` 看最新**） |
| 工作区 | 干净（提交前跑过 `git status`） |
| 场景 | `Assets/Scenes/Battle.scene`（含**常驻**的 `TutorialStageDirector` 与 `MoveGuidePresenter`，正式关也在场） |
| 天赋数据 | 4 棵树：`TalentTree_Stats`(13) / `TalentTree_Moves`(4) / `TalentTree_Pools`(9) + 旧的 `TalentTree_Main`(2，**已无人引用**） |
| `HeroConfig.talentTree` | `TalentTree_Moves`（**只这一棵** → 所以我给 `HeroConfig` 加了 `talentCatalog` 字段并指向 `TalentCatalog_Main`） |
| 存档 | `PlayerPrefs`；`unlockedTalents` / `taughtMoveGuides` 两个列表 |
| 玩家存档现状 | `unlockedTalents = [talent_move_stabc2]`、`taughtMoveGuides = []`（跑测后的还原态） |

---

## 2. 本轮做完的（都在远端）

| 提交 | 内容 |
|---|---|
| `9bb6fddd` | 修 4 个实测问题：SlashC2 射程 2→1 排；**中立态 roots 路径漏了天赋锁**（没解锁也能横滑出招）；教学专用 `forcedChoices` 离开教学后残留 → 正式关三选一被锁死；同向连斩自连绕过动作锁 |
| `219c82d8` | 临时诊断日志 `AttackSystem.SlashTimingDiag`（**定位 Q4 用，定位完删掉**） |
| `33f4c0aa` | 解锁引导 3 处修：等示范真正播完 / 不叠双层压暗 / 只在完整演完时标记「已教过」；`CollectPending` 遍历全部树；`HeroConfig` 加 `talentCatalog` |
| `71ee3054` | 教学关补 `gestureSteps` 的 Swipe 一拍 + 接上 4 处缺失的 `afterDialogue` ← **其中 Swipe 那一拍已确认要撤，见 §5-T1** |

---

## 3. 已确认的事实（**别再重新推导**）

### 3.1 「slash 该在什么时候教」的正确答案

> **玩家首次解锁 slash 后，第一次进入战斗时，弹一次「手势示范 + 一行文字」。与关卡节点完全无关。**

依据：`MoveGuideService.cs` 自己的规则注释（「解锁后第一次进关时，在波次开始前弹一次」）。

→ **教学关不该有 Slash 教学。**（我之前在教学 N1 wave1 加的那拍 Swipe 是多余的 → T1）

### 3.2 现有 Tap 教学是怎么做的（❗**2026-11-04 实测修正：这条原来写错了**）

> ❌ **原文写的是「Tap 教学用 `TutorialGestureHint.Gate`」——这是错的**（害我按它做了一版解锁引导，结果和教学拍不一致）。
> 真相：**教学拍根本不用 `Gate`**。全工程搜 `\.Gate(` → 除我那版引导外**无人调用**；`Gate` 是一套没人用的旧机制。

**教学拍（`TutorialStageDirector.Hints.cs` 的 `HintRoutine`）实际是这样**：

| 环节 | 实现 |
|---|---|
| 示范 | `PlayGestureAt(kind, from, to)` **循环播**（不是 Gate 的 `GateLoop`）——TapCombo 用 `PlayTapLoop`（单次点按会把提示层收起来 → 只剩光圈） |
| 暂停 | **不暂停整个游戏**。只是：冻住敌人（`SetEnemiesHold`）+ 压暗场景（`SetSceneDim`）+ **手势闸门只认这一招**（`input.tutorialGestureTarget`） |
| 完成判据 | **招式真的打出去并命中**（`OnTutorialAttackExecuted` → `_beatHitCount` ≥ `requiredHits`）→ **不是**「玩家做了手势」 |
| 收拍 | `EndBeat()` → `gestureHint.Hide()` → 手势层收掉；这一招转「已教」（`_taughtAttacks`） |
| 台词 | 收拍后补播（`TutorialGestureStep.afterDialogue`），**教学类台词自动推进**（`DialogueEventType.Tutorial`） |
| 白名单 | `ApplyAttackGate(teachAttack)` → 白名单 = 已教完 ∪ 当前这一拍要教的 |

> → **要「教一个招式」，就把它当一拍拍出来**（这正是 2026-11-04 T2 的最终做法）。

### 3.3 解锁引导为什么「黑屏 + 暂停 + 没演出」（**活体组件给的证据**）

用机器人跑正式关（slash 已解锁、`taughtMoveGuides` 已清）后读到：

```
MoveGuidePresenter: IsShowing=True  CompletedNormally=False  gestureHint.IsShowing=False
Dialogue: IsBusy=True      Time.timeScale=0
```

| 现象 | 根因 |
|---|---|
| 敌人还没到位 | `ShowPending()` 在**波次开始前** → 战斗还没开始（设计，不是 bug） |
| 黑屏 + 莫名暂停 | 提示层 `ShowDim(0.5)` **叠加**对话层压暗；对话是「点击继续」式 → `IsBusy=true` → `timeScale=0` |
| **完全没有教学的演出** | `MoveGuidePresenter` 用 `PlaySwipe()`（**播一次**）→ 1.1s 后 `Finish()` → `HideLayers()` **自己藏了**；而它等的是 `minGestureSeconds=1.2s` → 示范在玩家读台词期间就消失了 |

**核心**：`MoveGuidePresenter` 复用了**「演示一次」**（`Play*`），而教学用的是**「门槛循环演示」**（`Gate`）→ **应该复用 `Gate`，不该自己播一次**。

### 3.4 机器人（`BattleAutoPlayer`）怎么用

**没有预置挂点**，用法（`plan/bot-and-level-tuning-round2-20261025.md` §1.4）：

```csharp
var go = new GameObject("BattleAutoPlayer(测量)");
var bot = go.AddComponent<BattleAutoPlayer>();
bot.autoStart = true; bot.useAreaAttacks = false;
bot.bossQTEMode = BattleAutoPlayer.BossQTEMode.Suppress;
bot.preferredRouteChoiceId = "";
var route = AssetDatabase.LoadAssetAtPath<FakeRouteStageConfig>(
    "Assets/RouteData/ChangbanpoScroll/changbanpo_ScrollRoute.asset");
FakeRouteRuntime.Instance.Begin(route);   // 直接开跑，不用点落地页
```

- **必须 Play 模式里跑**（Edit 模式调 `Begin` 无效；且 Edit 模式 `new GameObject` 会**污染场景**）
- 前置：**先解锁 `talent_move_stabc2`**（指向突刺），否则卡死在 E0 的 1012（C 技霸体只有重攻击能破）
- 埋点 → `Library/Locus/telemetry/run-*.jsonl`；分析 `Tools/analyze_level_telemetry.py`
- 机器人**已经会点 UI**：`HandleBlockingUi()` 会点三选一（`um.ConfirmChoice`）和路线选择（`btn.onClick.Invoke()`）——**但点不掉对话气泡**（那不是 Button，是区域点击）

### 3.5 存档纪律

`SaveManager` 直写 PlayerPrefs → **验前备份、验完逐字节还原**。备份/还原用 `data.unlockedTalents` / `data.taughtMoveGuides` 两个列表直接存取（`PlayerPrefs.GetAllKeys()` 在这个环境不可用，别用）。

### 3.6 美术文档现状

- 我写的 `design/art-asset-brief-20261102.md`（47 件全做）→ **已作废**。对方已交 `ae533120 assets(art): 天赋页 V6 面板素材包 + 美术文档交接`，美术交接文档是 `plan/talent-ui-art-handoff-20261101.md`。
- 决策 **A（美术全部重做）/ B（画风全部作废重新定）** 仍然有效，但**执行已由对方接手**，以他们的交接文档为准。
- 我那份里**只有** §0.5 抗变更约定 / §0.6 变更影响矩阵值得留用。

---

## 4. 本周的工作性质（纪律）

> **不开新功能。** 本周大部分需求都是在**已有功能上做解耦与配置**。

| 做 | 不做 |
|---|---|
| 改现有判定分支 | 新建子系统 / 新存档体系 / 新 UI 框架 / 新掉落系统 |
| 拆耦合的字段 | 引入新依赖 / 新测试框架 / 新事件总线 |
| **复用现有教学机制**（`Gate`） | **造新的手势表现** |
| 让机器人补齐操作能力（点对话） | 新建校验器脚本 |
| 改资产数据（配表） | 改玩法数值 |

---

## 5. 待做清单（**按依赖顺序**）

### T1 · 撤掉教学关那拍 Swipe（✅ 2026-11-04 已完成，**未提交**）

| 项 | 内容 |
|---|---|
| 位置 | `Assets/Scenes/Battle.scene` → `TutorialStageDirector.gestureSteps` |
| 动作 | 删掉「教学 N1 / wave1 / kind=Swipe」那一条；它的 `afterDialogue = tutorial_t0_wave2` 一并消失 |
| 保留 | 另外 3 处 `afterDialogue`（点戳 `tutorial_t0_wave1` / 防御键 `tutorial_t0_wave3` / 技能打断 `tutorial_t0_skill`）—— 那是教学关**原本该有、原本是 NULL** 的台词，与 slash 无关 |
| 验收 | `gestureSteps` 回到 5 条；教学 N1 wave1 只剩 TapCombo |
| 备注 | 用 `EditorSceneManager.SaveScene` 保存；**不要手改 YAML** |

**实际结果（改动前后各跑一次教学，Play Mode + `TutorialFlowDriver`）**：

| | 改动前（6 拍） | 改动后（5 拍） |
|---|---|---|
| 拍序列 | Tap/0 → **Swipe/1** → Tap/-2 → DefendButton/0 → SkillSlot/1 | Tap/0 → **TapCombo/1** → Tap/-2 → DefendButton/0 → SkillSlot/1 |
| Swipe 那一拍 | **从未完成**（门槛拍要真划，自动化只点按） | 已删除 |
| 「连点三下」那一课 | **从来没教**（Swipe 占了 wave1） | 进度 1/3 → 2/3 → **完成（第 3 次）** → 补播 `tutorial_t0_interrupt` |

> **附带发现（比原任务描述更重要）**：`StartHint()` **每波只取第一个匹配项** → 同一波里的第二拍永远不会跑。
> 所以那拍 Swipe 不只是「多余」，它**把 TapCombo（连点三下 / 「多次攻击可以打断」）整拍挤掉了** —— 撤掉 Swipe 同时补回了这一课。
>
> 改动清单：`Assets/Scenes/Battle.scene`（−21 行，只有 Swipe 那一块）+ `TutorialStageDirector.cs`（`n1Battle` 字段 Tooltip「教点戳 / 斩击」→「教点戳 / 连点三下」）。
> 存档已逐字节还原（`player_save`，len 1417 / fnv1a 2AEB5744）；Console error/warn = 0。
> 遗留（本次不做）：`TutorialSetupTool` 生成的 5 拍**不含**那 3 处 afterDialogue → 谁再跑一次「Tools ▸ 三国杀戮 ▸ 教学关 ▸ 应用教学关新配置」会把台词静默抹掉。

### T2 · 解锁引导（❌ 方案作废，2026-11-04 改成「教学拍」，**已完成**）

> **最终方案见 `design/tutorial-guides-spec-20261104.md`**（§2 教学拍标准形态 / §3.1 细则 / §4.1 全链路实测）。

> **最终结论：不用 `Gate`，把解锁引导拍成「教学拍」**（和教点戳同一条机制）。
>
> **为什么不用 Gate（实测三条理由）**：
> 1. **玩家划的那一下打不出去** —— 引导跑在战斗开始之前（`ShowPending()` 在 `StartRouteBattle()` 之前）→ `AttackSystem.TryExecuteAttack` 先卡 `playerState.stageState != StageState.InProgress` 就 return 了 → 玩家学不到「这一划会横扫一排」。**与 Stab 流程不一致的根因就是它。**
> 2. **和教学拍抢同一个提示层** → `TutorialGestureHint.Hide()` 不会停 `Gate` 的示范循环 → 教学拍收拍后**手势反复冒出来**（用户报的「stab 教学时玩家交互后手势没消失」）。
> 3. **暂停整个游戏**（`timeScale=0` + 拦输入），而教学拍是「冻敌人 + 压暗 + 限手势」→ 体感也不一致。
>
> → 交接 §3.2 说「照 Tap 教学做」是对的，但 §3.2 对 Tap 教学的**描述是错的**（见上面 §3.2 的修正）。

| 项 | 内容（**已按新方案重写**） |
|---|---|
| 位置 | `Assets/Scripts/Tutorial/TutorialStageDirector.MoveGuide.cs`（**新文件**） |
| 动作 | 「已解锁但还没教过」的招式 → **变成一拍拍出来**（`TutorialGestureStep` + `guideNodeId`/`guideAttackType`）→ 走教学拍那条机制 |
| 时机 | **开波时**（`OnWaveStarted`）→ 战斗已开始 → 玩家划的那一下**真的打出去**（和教点戳一致） |
| 台词 | 收拍后补播（教学类台词自动推进）→ `guide_move_slash` 的 `eventType` 已从 `Conversation` 改成 `Tutorial`（否则游戏一直暂停等点击） |
| 标记教过 | **只有招式真的打中敌人**才算教完 → `MoveGuideService.MarkTaught`（没打中下次还会教） |
| 验收 | 进关卡 → 看到横划示范 → 划一下 → **横扫真的打中敌人** → 收拍 → 一行文字 → Console `[Tutorial] 招式解锁引导完成：talent_move_slash` |
| **规格文档** | ✅ **`design/tutorial-guides-spec-20261104.md`（所有引导的唯一口径）** —— 教学拍标准形态 / 7 类引导 / P1–P5 原则 |

**实际改了这些（复用教学拍机制，没新造表现）**：

| # | 改动 | 文件 |
|---|---|---|
| 1 | 新增「招式解锁引导拍」：收集待教招式 → 转成教学拍 | `TutorialStageDirector.MoveGuide.cs`（新） |
| 2 | `OnWaveStarted` 改成协程：**先拍引导拍 → 再拍教学关自己的拍** | `TutorialStageDirector.cs` |
| 3 | `TutorialGestureStep` 加 `guideNodeId` / `guideAttackType` 两个字段 | 同上 |
| 4 | 引导拍用的招式 = 天赋节点配的（`guideAttackType`），白名单照样放行 → 玩家真能打出来 | `TutorialStageDirector.Rules.cs` |
| 5 | 引导拍收拍时写存档「已教过」（**只有打出去才算**） | `TutorialStageDirector.Hints.cs` |
| 6 | 删掉旧路：`MoveGuidePresenter`（已删）+ `IMoveGuidePresenter` 端口 + `CombatPresenters.MoveGuide` + `MoveGuideService.ShowPending` + 两处流程层调用点 | 多处 |
| 7 | `guide_move_slash` 的 `eventType`：Conversation → **Tutorial**（自动推进，不把游戏暂停等点击） | `Assets/ScriptableObjects/Dialogue/guide_move_slash.asset` |
| 8 | 🆕 **所有招式教学的完成判据 = 「招式打出去」+「真的打中敌人」**（用户 2026-11-04 定）→ 空挥（`col=-1`）不再收拍 | `TutorialStageDirector.Dialogue.cs` / `.Rules.cs` / `.Hints.cs` |
| 9 | 🆕 **正式关不教三选1**：`OnChoicesReady` 加 `IsOnTutorialRoute()` 门槛（不念台词 / 不指卡 / 不限只能选推荐项） | `TutorialStageDirector.cs` |

**实测结果（Play Mode 跑过 4 轮）**：

| 场景 | 结果 |
|---|---|
| 正式关 + 机器人 + 横扫已解锁 | 引导拍在 wave0 拍出 → 机器人划 → `[AttackSystem] 斩击 方向:L→R 伤害:20 目标数:1`（**真的打出来了**）+ `[SlashDiag] 命中 hit=True` → `[Tutorial] 招式解锁引导完成：talent_move_slash` → `taughtMoveGuides=[talent_move_slash]` ✅ |
| 教学关（横扫已解锁） | 引导拍 → 划 → 收拍 → 教学拍继续（Tap/0 → TapCombo/1 → 路线 → DefendButton/0 → SkillSlot/1）**全程跑通** ✅ |
| 教学关回归（改判据后） | Tap/0 → TapCombo/1（1/3→2/3→第3次）→ 路线 → DefendButton/0 → SkillSlot/1 **全部收拍正常**（新判据 = 必须打中）✅ |
| 引导拍收拍链 | `手势拍开始 kind=Swipe` → `斩击 方向:L→R 伤害:20 目标数:1` → `SlashDiag 命中 hit=True` → `手势拍完成：Slash` → `招式解锁引导完成` ✅ |
| 游戏是否被暂停 | **没有**（`timeScale=1`）—— 与 Stab 教学一致（教学拍是冻敌人+压暗+限手势，不是暂停全局）✅ |
| Console | error = 0，warn = 0 ✅ |

> **架构约束（实现时踩到的）**：`BattleAutoPlayer` 属于 **Locus.Flow**（`Assets/Scripts/Utils/Locus.Flow.asmref`）→ **不能引用 UI 层**（`TutorialStageDirector` 在 Locus.UI）。
> → 机器人改读 `InputManager.tutorialGestureTarget`（`TutorialGestureTarget` 定义在 Locus.Flow）→ 知道「这一拍要划」。
> `IDialogueModal.Skip()` 是 T3 加的（对话气泡）——**这条留着有用**。

**遗留**：
- `tutorial_t0_wave2`（「横着划一刀——一条横线扫过去…」）现在**没人引用**了 → 引导用的是天赋节点自带的 `guide_move_slash`。可删可留（它仍在 `DialogueDatabase.asset` 里）。
- `TutorialGestureHint.Gate`（门槛机制）现在**全工程无人调用** → 是一套没人用的旧机制（**§3.2 原来误以为它在用**）。没删（留着当标准件），但**别再拿它当教学机制**。
- **手势示范 / 台词 / 压暗的视觉仍需真人验收**（自动化只能验状态与日志）。

### T3 · 机器人补「点掉对话气泡」的能力（✅ 2026-11-04 已完成，**未提交**）

| 项 | 内容 |
|---|---|
| 位置 | `Assets/Scripts/Utils/BattleAutoPlayer.cs` → `HandleBlockingUi()` |
| 动作 | 加一条：`CombatPresenters.Dialogue.IsBusy` 时调 `BattleDialogueView.Instance.Skip()`（现成 public 方法） |
| 为什么 | 机器人点不掉对话 → 引导/三选一对话会把它卡死 → **机器人应该能做游戏内的所有操作**，否则没法测 |
| 验收 | 机器人能跑完整条路线不被对话卡住 |

**实际改了三处**：
1. `IDialogueModal` 加 `Skip()`（`DialogueManager` 实现）→ 机器人走端口调 `dialogue.Skip()`（不能直接用 `BattleDialogueView`，见上面的架构约束）。
2. 机器人这一拍要**划**的时候改用横划（读 `InputManager.tutorialGestureTarget`）→ 招式解锁引导拍能被机器人完成。
3. 🆕 **修了一个既有死锁**（不是本次引入，但会让 T3 验收直接卡住）：三选一面板会 `Time.timeScale = 0`（`UpgradeChoiceManager.cs:245`）→ **缩放时间 `Time.time` 冻结** → 机器人的 UI 计时器 `if (Time.time < _nextUiClickTime) return;` **永远成立** → 机器人**永远点不了三选一** → 卡死。
   → 改用 `Time.unscaledTime`（UI 点击节流本来就该用真实时间）。同时把「点对话」分支挪到「选三选一」**后面**（三选一更重要，且它会暂停游戏）。
4. `TutorialFlowDriver`（教学关驱动）也补了「这一拍要划就划」→ 教学关里的引导拍机器人/驱动能过。

> 这个死锁是**既有问题**：交接 §3.4 写的「机器人已经会点三选一」在**升级弹窗**这个场景下并**不成立**（只在弹窗恰好没暂停时才成立）。
> 以后跑机器人前先知道：它以前会在每次升级三选一上卡死。

### T4 · Q4：同方向连斩过快（**等真人实测数据，不要猜**）

- 诊断日志已提交（`219c82d8`）：`[SlashDiag] 尝试/命中` 两行/次，含 `cancel` / `lockRemain` / `hit` / `dir`
- **要真人测**：同方向连划 5 下 + 左右交替 5 下，两组日志对比
- 重点看「尝试」那行的 `lockRemain` 是否 > 0（=0 说明没进动作锁判定）
- 我已改过一次（自连不再 `cancelCurrentMove`），**现象仍在 → 路径找错了**，别再猜
- 定位完删掉 `AttackSystem.SlashTimingDiag` 与两处调用

### T5 · 校正文档里的过时描述（**否则下一个对话会照错的方向做**）

> ✅ 已做：`design/tutorial-guides-spec-20261104.md`（**所有引导的唯一口径**，含 §4.1 全链路实测与 5 个真 bug 的根因）。
> ⬜ 剩余：`design/out-of-game-growth-v2-analysis.md`、`plan/growth-stage-move-plan-20261102.md`（§0.5 批次表）、`design/combo-zhangfei-slash-c2.md`、`design/in-run-growth-and-unlocks.md` 里关于「Slash 教学」的描述尚未同步。

| 文档 | 要改的地方 |
|---|---|
| **`design/tutorial-guides-spec-20261104.md`（新建）** | ✅ **所有引导的唯一口径**（教学拍标准形态 / 7 类引导的时机与形式 / P1–P5 原则）→ 改引导前先看它 |
| `design/out-of-game-growth-v2-analysis.md` | §4A / §11 里把 Slash 教学写成「关卡波次教学」的地方 → 改成「解锁后首次进关引导」 |
| `plan/growth-stage-move-plan-20261102.md` | §0.5 批次表 + §7 任务对照表（编号已从 T-XX 变成 A/B/C/D） |
| `design/combo-zhangfei-slash-c2.md` | §4 表现表：补「解锁引导」现状 + 根因 + 修法（T2） |
| `design/in-run-growth-and-unlocks.md` | 补一节「招式解锁引导（MoveGuide）」：现状 + 活体证据 + 修法 |

### T7 · 敌人攻击音效（**需要新素材，用户 2026-11-04 定的口径**）—— ⬜ **下一轮第一个做**

> ① 敌人攻击的 `_enemyAttackTelegraphClip` 现在指到 `QTE_block1`（**QTE UI 音**被敌人攻击借用了）→ 需要一个**「挥武器」音效**（库里没有 `guard_block/QTE_block/parry/enemy_hitted` 之外的素材 → 要新出）。
> ② `PlayerState.SetDefend(true)` 里 `PostEvent("Player_Parry")` → 启用防御**不该播招架音**（它是「复用招架的挥入表现」时连音效一起复用的）。
> ③ 用户口径：**「玩家的 defend 音效被当成了敌人攻击的音效」** → 敌人攻击要有自己的挥武器音。
> ④ 玩家格挡（`PlayerGuard` → `Guard_Block`）现在的音效是对的，**不动**。

| 项 | 内容 |
|---|---|
| 现状 | ① `AudioManager._enemyAttackTelegraphClip` 指到 **`QTE_block1`**（QTE 答题 UI 音被敌人攻击借用了）→ 敌人起手播的是 UI 音 ✗；② 玩家**启用防御**时 `PlayerState.SetDefend(true)` 里 `PostEvent("Player_Parry")` → 启用防御播的是**招架音** ✗（它是「复用招架的挥入表现」时连音效一起复用的） |
| 用户口径 | “敌人的攻击”应该有自己的**挥舞武器**音效；玩家**防御格挡**的音是另一回事（那是格挡音）。**敌人攻击音需要新找素材** → 先记在待办里 |
| 素材盘点 | 库里**没有**独立的敌人攻击音（只有 `guard_block1..6` / `QTE_block1..2` / `parry` / `enemy_hitted1..2` / `launch_hit` / `slash_hit` / `stab_hit` / `playerattackvoice`）→ **需要美术/音频出「敌人挥武器」音效** |
| 待办 | ① 出「敌人挥武器」音效资产；② 把 `_enemyAttackTelegraphClip` 从 `QTE_block1` 换成它（在此之前**先清空**，不要让敌人攻击播 UI 音）；③ 删掉 `PlayerState.SetDefend` 里的 `PostEvent("Player_Parry")`（启用防御不播招架音） |

### T6 · 关卡与数值（**用户 2026-11-04 说「数值方面先别管」**）—— 暂缓

> 已查清的事实（下一轮要动数值时直接用）：
> - **铁律八（普通波 30–55s）实测合规**：正式关 N1 第一波 = **30.9s**（刚打完教学关的真实状态）。
> - **单只敌人的硬度是教学关的 4.7~6.1 倍**（倍率按「中期玩家（3 个招式节点 + 190 HP）」推的，实际开局玩家没有那些招式）→ 这就是用户说的「生命值远超原教学关设定」。
> - **建议方案 D**：波次总血量不变（时长不动）→ 把血量从「集中在 1~3 只」摊到「更多只」→ 单只硬度回到教学关量级。
> - 附带：各波的 `waveId` **全是 0**（运行时按列表下标走，暂不影响战斗，但数据不卫生）。
> - 其它已知未解：XP 缺口（整条路线 Lv10 vs 设计 Lv40）、满槽补偿不触发、N2 是孤儿节点、104 对不操作玩家容错为零。

| 项 | 状态 |
|---|---|
| 关卡编队 N1 1波 / E0 3波 / N3 5波 + 104 两阶段 | ✅ 对方已做（`159af158`） |
| 铜钱闭环 | ✅ 已做（`6d9950b4`），但**通关奖在路线流程里没被付**（`clearCoinReward` 配了不生效）→ 仍需确认 |
| 待拍板 | 通关收入目标 600–800 / XP 缺口 196% / 满槽补偿当前不触发 / N2 是孤儿节点 / 铁律八不达标 / 104 对不操作玩家容错为零 |

---

## 6. 每个任务的自检（**只用工程已有工具，不新建**）

| # | 手段 |
|---|---|
| 编译 | Unity Console **error 级 = 0** |
| 数据 | 菜单「Tools/数据校验/天赋树 ✅」「关卡清单 ✅」「战斗状态定义 ✅」 |
| 数值核对 | 菜单「Tools/三国杀戮/战斗数值总表」 |
| 命中帧 | 菜单「Tools/三国杀戮/校验攻击命中帧」 |
| 总检 | 菜单「Tools/项目体检/运行全部检查」 |
| 关卡 | `WaveConfigEditor`（Inspector 可视化编队） |
| 跑测 | `BattleAutoPlayer`（Play 模式）+ `Tools/analyze_level_telemetry.py` |
| 难度表 | `python Tools/build_first_level_difficulty_table.py`（⚠️ **基线已漂移**：脚本内置的敌人数值与波次现状停在 2026-10-25，与当前资产不符 → 重跑前先对资产） |

**项目内无任何自动化测试**（全项目无 `UnityEngine.TestTools` 引用）→ 自检 = 上面这些 + 实机 + Console。

---

## 7. 需要人验收的（**我做不了，也别让我代劳**）

| 项 | 谁看 |
|---|---|
| 手势示范 / 台词 / 遮罩的**视觉** | 玩家（真人） |
| 教学节奏与体感 | 玩家（真人） |
| 美术造型 | 美术 |
| Q4 的手感 | 玩家（真人） |

---

## 8. 纪律（这几轮踩过的坑，别再踩）

1. **动手前先查现有实现**。我在这上面连错三次：① 把「解锁后首次进关引导」当成「教学关某一拍」② 拿 `n1Battle` 的 Tooltip（一句没实现的意图注释）当需求依据 ③ 把「参考已有的点击教学去做」读成「照抄 Tap 的字段结构」
2. **不改代码前先拿活体证据**。跑一次机器人、读活着的组件状态，比读代码推断可靠（这一轮根因就是靠 `MoveGuidePresenter.IsShowing / CompletedNormally / gestureHint.IsShowing` + `Dialogue.IsBusy` 读出来的）
3. **引用二手文档前回原始出处**。「必须先给天赋节点」那条我转述自 `growth-week-handoff-20261103.md`，原始出处是 `bot-and-level-tuning-handoff-20261025.md` / `round2`
4. **开局先 `git fetch`**。我漏拉过一次，导致基于旧状态判断了整轮
5. **Play 模式里跑，别在 Edit 模式建对象**。我在 Edit 模式 `new GameObject` 污染过场景
6. **存档验前备份、验完还原**
7. **一次只推进一个可验证的小步**，每步跑校验器 + 机器人
