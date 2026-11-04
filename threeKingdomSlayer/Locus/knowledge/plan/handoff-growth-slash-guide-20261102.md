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

### 3.2 现有 Tap 教学是怎么做的（**照它做，不要造新表现**）

`TutorialGestureHint.Gate(GestureHintKind kind, Vector2 from, Vector2 to, Action onPass, int targetColumn = -1)`

- 注释原文：「**引导门槛：暂停游戏、循环播放手势示范，玩家真的做出这个手势才恢复并回调**」
- `GateLoop()` 是 `while(true)` → **示范循环播放**，不会播完就消失
- 每轮时长：Tap = `tapDuration+0.5` / Swipe = `swipeDuration+0.5`（1.1+0.5 = 1.6s）
- **台词是收拍后补播**（`TutorialGestureStep.afterDialogue`），不是和示范同时
- 门槛期间 `timeScale=0` + `GameplayInputGate.RequestBlock(TutorialGate)` + 遮罩
- 招式白名单：`ApplyAttackGate(teachAttack)` + `Rules.cs` 已有 `Swipe → AttackType.Slash` 映射

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

### T1 · 撤掉教学关那拍 Swipe（**已决策：撤**）

| 项 | 内容 |
|---|---|
| 位置 | `Assets/Scenes/Battle.scene` → `TutorialStageDirector.gestureSteps` |
| 动作 | 删掉「教学 N1 / wave1 / kind=Swipe」那一条；它的 `afterDialogue = tutorial_t0_wave2` 一并消失 |
| 保留 | 另外 3 处 `afterDialogue`（点戳 `tutorial_t0_wave1` / 防御键 `tutorial_t0_wave3` / 技能打断 `tutorial_t0_skill`）—— 那是教学关**原本该有、原本是 NULL** 的台词，与 slash 无关 |
| 验收 | `gestureSteps` 回到 5 条；教学 N1 wave1 只剩 TapCombo |
| 备注 | 用 `EditorSceneManager.SaveScene` 保存；**不要手改 YAML** |

### T2 · 解锁引导改用 `Gate`（**复用现有教学机制**）

| 项 | 内容 |
|---|---|
| 位置 | `Assets/Scripts/UI/MoveGuidePresenter.cs` |
| 动作 | `PlayGesture()` 里的 `gestureHint.PlaySwipe(from, to)` → 改为调用 **`gestureHint.Gate(kind, from, to, onPass)`**（门槛循环示范 + 等玩家真做出手势） |
| 关键 | 门槛期间 `Gate` 自己会：`timeScale=0` + `RequestBlock(TutorialGate)` + 遮罩 + **循环播示范** |
| 台词 | 沿用现有节奏：**收拍后补播**（和 `TutorialGestureStep.afterDialogue` 一致），不要与示范同时叠 |
| 压暗 | 只留一层：对话在播时不要再叠提示层压暗（`ShowDim` 要在**排完对话之后**再决定，之前的修法位置错了） |
| 标记教过 | 仍然只在**玩家真的做出手势**（`onPass` 回调）后才 `MarkTaught` |
| 验收 | 进关卡 → 看到**循环**的横划示范 + 一行文字 → 做出横划 → 放行开打；Console 有 `[MoveGuide] 已教：talent_move_slash（横扫）` |

### T3 · 机器人补「点掉对话气泡」的能力

| 项 | 内容 |
|---|---|
| 位置 | `Assets/Scripts/Utils/BattleAutoPlayer.cs` → `HandleBlockingUi()` |
| 动作 | 加一条：`CombatPresenters.Dialogue.IsBusy` 时调 `BattleDialogueView.Instance.Skip()`（现成 public 方法） |
| 为什么 | 机器人点不掉对话 → 引导/三选一对话会把它卡死 → **机器人应该能做游戏内的所有操作**，否则没法测 |
| 验收 | 机器人能跑完整条路线不被对话卡住 |

### T4 · Q4：同方向连斩过快（**等真人实测数据，不要猜**）

- 诊断日志已提交（`219c82d8`）：`[SlashDiag] 尝试/命中` 两行/次，含 `cancel` / `lockRemain` / `hit` / `dir`
- **要真人测**：同方向连划 5 下 + 左右交替 5 下，两组日志对比
- 重点看「尝试」那行的 `lockRemain` 是否 > 0（=0 说明没进动作锁判定）
- 我已改过一次（自连不再 `cancelCurrentMove`），**现象仍在 → 路径找错了**，别再猜
- 定位完删掉 `AttackSystem.SlashTimingDiag` 与两处调用

### T5 · 校正文档里的过时描述（**否则下一个对话会照错的方向做**）

| 文档 | 要改的地方 |
|---|---|
| `design/out-of-game-growth-v2-analysis.md` | §4A / §11 里把 Slash 教学写成「关卡波次教学」的地方 → 改成「解锁后首次进关引导」 |
| `plan/growth-stage-move-plan-20261102.md` | §0.5 批次表 + §7 任务对照表（编号已从 T-XX 变成 A/B/C/D） |
| `design/combo-zhangfei-slash-c2.md` | §4 表现表：补「解锁引导」现状 + 根因 + 修法（T2） |
| `design/in-run-growth-and-unlocks.md` | 补一节「招式解锁引导（MoveGuide）」：现状 + 活体证据 + 修法 |

### T6 · 关卡与数值（**依赖解锁链落地，现在做没意义**）

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
