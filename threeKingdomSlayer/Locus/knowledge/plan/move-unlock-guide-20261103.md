---
id: kd_f9b1e608-b72b-4572-bd5b-e4e2dcf89e9f
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_move_unlock_guide_20261103
injectMode: inherit
aiEditMode: inherit
---

# 招式解锁引导（解锁后首次进关弹出）（2026-11-03）

> 需求（用户）：「建议做 slash 解锁后的相关教学，在玩家解锁 slash 后初次进入关卡弹出 slash 引导（这个引导之前就有）」。
>
> 裁决（用户 2026-11-03 选定）：
> ① **只教「横扫 Slash」**（`talent_move_slash`）；
> ② 形式 = **手势示范 + 一行文字**（复用现成件）；
> ③ 时机 = **波次开始前弹出，挡住开打**（玩家确认后才开波）。

---

## 1. 问题

招式被放到天赋节点后面（解锁才可用）→ 玩家解锁后**没人教手势**。
教学关的手势拍里本来有 `Swipe` 这一档（对应 `AttackType.Slash`），但**当前 5 个教学步里没有它**
（Tap / Tap Combo / Tap / Defend Button / Skill Slot）→ 正式关里玩家只能靠猜。

## 2. 做法（不新建 UI，复用两件现成的）

| 环节 | 复用什么 | 备注 |
|---|---|---|
| 手势示范 | 场景里的 `TutorialGestureHint`（手指 + 拖尾 + 压暗，全代码驱动） | 拖尾贴图 `tutorial_gesture_swipe_trail_ui` ✅ |
| 一行文字 | 现成对话层（`CombatPresenters.Dialogue` → `DialogueEventData`） | 自带压暗 + 点击继续 ✅；无 Boss 台词 → 走 TutorialBubble 版式 ✅ |
| 挡路 | 引导播完前不开波（`FakeRouteRuntime.RunBattleEntries` 里 `yield` 住） | 实测：引导期间场上敌人 = 0 ✅ |

**机制是通用的**：节点自己配了 `moveGuideGesture` 才要教（数据决定，不写死招式名）；
当前只有「横扫」节点配了 → 其余节点不受影响 ✅（以后加新招式只配数据）。

## 3. 改动清单

| 层 | 文件 | 做什么 |
|---|---|---|
| 数据 | `Assets/Scripts/Data/MoveGuideRequest.cs`（新） | `MoveGuideGesture` 枚举 + `MoveGuideRequest` |
| 数据 | `Assets/Scripts/Data/TalentTreeConfig.cs` | 节点加两个字段：`moveGuideGesture` / `moveGuideDialogue` |
| 数据 | `Assets/Scripts/Data/SaveData.cs` | 加 `taughtMoveGuides`（已教过的引导 id） |
| 数据 | `Assets/Scripts/Data/Save/MoveGuideSaveSection.cs`（新） | 存档分片（与 `TalentSaveSection` 同构）+ 登记进 `SaveSections` |
| 数据 | `Assets/Scripts/Data/CombatPresenters.cs` | 新端口 `IMoveGuidePresenter` + `CombatPresenters.MoveGuide` |
| 核心 | `Assets/Scripts/Core/MoveGuideService.cs`（新） | 收集待教 / 标记已教 / `ShowPending()` 协程 |
| 界面 | `Assets/Scripts/UI/MoveGuidePresenter.cs`（新） | 演手势 + 排队台词 + 等玩家确认 |
| 流程 | `Assets/Scripts/RouteFake/FakeRouteRuntime.Node.cs` | 路线节点**开打前** `yield return MoveGuideService.ShowPending()` |
| 流程 | `Assets/Scripts/Managers/StageRunFlow.cs` | 直连关卡（非路线）启动前同样弹 |
| 资产 | `Assets/ScriptableObjects/Dialogue/guide_move_slash.asset`（新） | 一行台词：「手指横着划过去 —— 横扫一排敌人！」 |
| 资产 | `Assets/ScriptableObjects/Talents/TalentTree_Moves.asset` | `talent_move_slash` 挂 `SwipeHorizontal` + 上面那句台词 |
| 场景 | `Assets/Scenes/Battle.scene` | 新增根对象 `Move Guide Presenter`（挂 presenter，引用场景里的手势提示件） |

> **没动对方的文件**：`TutorialGestureHint.cs` / `TutorialStageDirector*` / `DialogueManager` / `BattleDialogueView` /
> `TalentTreePanel.cs` / `TalentTree.scene` 全部只读不改 ✅。
> Battle 场景只**新增**了一个根对象（+65 行），顺带把两个早已失效的序列化字段
> （`enableDirectionalPush` / `directionalPushStep`，SlashC2 那轮已从脚本删掉）从场景里清掉了 ✅。

## 4. 口径

| 问题 | 答案 |
|---|---|
| 什么时候弹 | **解锁后第一次进关**（按存档判定：已解锁 && 没教过 → 进关时弹） |
| 弹几次 | **同一存档只弹一次**（`SaveData.taughtMoveGuides`） |
| 没界面怎么办 | 端口没登记 → 直接跳过，**且不写「已教过」** → 下次还教 ✅ |
| 玩家不点继续 | 20 秒兜底 → 收掉台词 → 继续开打（战斗不会被卡死 ✅） |
| 教过的判定 | 播完就记（不管玩家点没点）—— 记的是「这条提示演过了」 |

## 5. 实测（2026-11-03）

| # | 检查 | 结果 |
|---|---|---|
| 1 | 解锁横扫后首次进关 → 引导弹出 | ✔ 实测：`guide showing=True`，**场上敌人 = 0**（挡住开打 ✅） |
| 2 | 压暗 + 台词 | ✔ 截图：画面压暗 + 底部气泡「手指横着划过去 —— 横扫一排敌人！」+ 张飞头像 |
| 3 | 手势示范 | ✔ 拖尾布局正确：水平长 609×67、旋转 0°，手在左端（`from=(0.2,0.58) → to=(0.8,0.58)`） |
| 4 | 玩家点继续 → 引导结束 → 开波 | ✔ 实测：`Skip()` 后 `guide showing=False`、`enemies=3`、`phase=Battle` |
| 5 | 只教一次 | ✔ `CollectPending` 教过后 = 0；清掉存档记录后 = 1（反证） |
| 6 | 存档往返 | ✔ 存档 `taughtMoveGuides=[talent_move_slash]`；验前备份、验后逐字节还原 |
| 7 | 编译 / 校验器 | ✔ error/warn = 0；天赋树 4 棵 / 天赋目录 / 关卡清单 / 战斗状态定义 全通过 |

> 修过的一个问题：超时兜底原来**不收台词** → 气泡会残留盖在战斗上（实测截图）→ 已改成超时时 `BattleDialogueView.Skip()` ✅。

## 6. 还能调的地方

| 项 | 现状 | 可调 |
|---|---|---|
| 台词内容 | 「手指横着划过去 —— 横扫一排敌人！」 | 改 `guide_move_slash` 资产里的 text ✅ |
| 手势位置 | 从屏幕 20% 划到 80%（高度 58%） | `MoveGuidePresenter` 的 `swipeFrom01/swipeTo01` ✅ |
| 压暗强度 | 0.5 | `dimAlpha` ✅ |
| 等待上限 | 20 秒 | `maxWaitSeconds` ✅ |
| 教谁 | 只有「横扫」 | 给别的节点配 `moveGuideGesture` 即可 ✅（不用改代码） |

## 7. 还没做的

| # | 项 | 说明 |
|---|---|---|
| 1 | **教学关本身没有教横扫** | 教学关的 5 个手势步里没有 `Swipe` → 玩家在教学关也学不到横扫；补这一步要动 `TutorialStageDirector` 的场景配置（**对方那侧的文件**）→ 未做 |
| 2 | 斩击 C2 的引导 | C2 节点没配引导（用户裁定「只教横扫」）；等 C2 的美术动作到位后再补 ✅ |
| 3 | 引导只演不判定 | 不像教学关那样「必须打出这一招才算过」→ 玩家点继续就能打 ✅（有意为之） |
