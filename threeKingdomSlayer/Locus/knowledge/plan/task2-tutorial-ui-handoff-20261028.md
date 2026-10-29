---
id: kd_965193ac-6bec-41f8-9630-47deff8b960d
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_task2_tutorial_ui_handoff_20261028
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: 任务 2（教学关优化 + 提示 UI 改造）的开工交接 —— 已实测的 5 个缺口与根因、改法、代码位置、坑、验收口径；新对话可直接照此开工。
---

# 任务 2 交接：教学关优化 + 提示 UI 改造（2026-10-28）

> **状态**：任务 1（战斗 UI 改造）**已验收**，提交 `619061c`，已 push 到
> `origin/feat/stage-select-talent-overlays`（`031d603..619061c`）。
> 本文件是**新对话直接开工的入口**，不需要回读上一段对话。
>
> 上游文档：`plan/weekly-plan-combat-ui-tutorial-20261027.md`
> （§5 战斗 UI 方案、§6 教学关方案、§13 实施记录与踩坑）。
> 参考项目理解：同文档 §2（已联网核实 + 用户提供实机截图）。

---

## 0. 任务定义（用户原话）

> 本周任务 2：**教学关的优化和提示 UI 改造**（配合任务 1），参考《上古卷轴：刀锋》手机版。
> 10-29 前要完成 1、2 两项，**让玩家能直观地了解怎么玩这个游戏**，优化战斗局内 UI 和引导，以及表现。

**验收口径（建议按这个定）**：一个没看过说明的人，打开游戏 **3 分钟内能自己打出一场完整战斗**。

---

## 1. 任务 1 已交付的现状（任务 2 在这之上做，别推翻）

| 已就位 | 说明 |
|---|---|
| 底排结构 | 头像 + 等级球 + 黄色棱形技能排 + 通栏红血条，**已按用户草图逐像素对齐**（误差 ≤3 canvas 单位） |
| 可重跑改造工具 | `Assets/Scripts/Editor/HeroHudLayoutTool.cs`（菜单 `三国杀戮 ▸ 战斗UI ▸ 应用新布局`）。场景实例与 prefab 模板用同一套函数，**全程幂等** |
| 血条 | 纯红左右镜像填充 →「两侧向中间收缩」；`100/100` 居中 |
| 等级球 | 浅底深字，叠在头像左上角，球内竖向「水上涨」= 经验比例；经验宝石改为飞向球 |
| 黄色棱形 | 竖长 72×139，按获得顺序从左往右排，空槽隐藏，点击即释放 |

**改造工具的三个用法要点**（新对话必须知道）：
1. 场景里的 HUD 是 **prefab 实例且有 157 处手改 override** → 改 prefab **不会**同步到场景，必须两边都跑。
2. 在 prefab 实例里"删了重建"会产生 added override 并逐次叠加 → 工具已改成幂等（有就复用）。
3. `anchoredPosition` 相对的是**锚点参考点**不是枢轴；`anchoredPosition.y` 对锚点 (0,0) 的元素 = **离父级下边缘多高**。
4. canvas 高是 **1920** 不是 2160（CanvasScaler ref 1080×1920 + match height，scaleFactor 1.125）。

---

## 2. 任务 2 的五个子项（按优先级 + 已实测根因）

### T1【必修】教学关 `gestureSteps` 只有 3 条，新拍型一条都没配

**实测**（Play Mode 反查 `TutorialStageDirector.gestureSteps`）：

| # | battle | waveIndex | kind | 含义 |
|---|---|---|---|---|
| 0 | `Tutorial_N1_Battle` | 0 | `Tap` | 点一下 |
| 1 | `Tutorial_N1_Battle` | 1 | `Swipe` | 横划 |
| 2 | *(null)* | −2 | `Tap` | 路线出口（点箭头） |

**缺**：`ChargedSwipe`（蓄力上挑破盾）、`Parry`（时停招架）、`ParryHold`（上划按住防御）；
**且没有任何一条属于 `Tutorial_E0_Battle`** → E0 两波完全没手势教学。

**关键事实**：代码侧**已经写好**但**场景没配**，所以根本不会触发：
- `Assets/Scripts/Tutorial/TutorialStageDirector.Beats.cs`（**未提交**）→ `FreezeParryRoutine` / `DefendHoldRoutine`
- `Assets/Scripts/Data/TutorialAttackGate.cs`（**未提交**）→ 招式白名单「已教完 ∪ 当前这一拍要教的」

**要补的步骤**（波号需在 Play Mode 实测对齐 —— `StartHint` 匹配的是 `waveIndex`，
而 `Tutorial_E0_Battle` 的 `waveId` 是 2/3，**不要照抄**）：

| 序 | battle | wave | kind | 教什么 | 备注 |
|---|---|---|---|---|---|
| 3 | `Tutorial_Battle` | 2 | `ChargedSwipe` | 蓄力上挑破盾（打 1021） | `pauseUntilPerformed=true` |
| 4 | `Tutorial_E0_Battle` | 0 | `Parry` | 时停招架（对 1012 霸体） | `freezeOnGesture=true` |
| 5 | `Tutorial_E0_Battle` | 1 | `ParryHold` | 上划按住防御（扛 109 冲锋） | `holdAfterGesture=true` |

**路由**（`TutorialStageDirector.Hints.cs:10-45`）：`waveIndex<0` → 路线提示；`freezeOnGesture` → 时停拍；
`holdAfterGesture` → 防御拍；否则普通拍。

### T2【必修】教学台词不会自动推进

`tutorialLineSeconds = 2.2`（`TutorialStageDirector.cs:47`）**全库零引用** → 教学台词一直卡到玩家点击。

**改法**：接进 `BattleDialogueView`（教学期生效）——每行自动停 2.2s 推进，**玩家点击可立即跳过**；
非教学场景行为不变。真正的门槛仍然是手势，不是台词。

### T3【必修】引导提示的表达方式太弱

**实测现状**：`TutorialGestureHint` 能力其实很够（ring 光圈 / finger 手指 / trail 轨迹 / dimOverlay 压暗 /
`Gate()` 闸门 / 循环示范 / `ReplayPlayerGesture` 回放玩家手势 / 六种动作动画
`PlayTap·PlaySwipe·PlayHold·PlayChargedSwipe·PlayParry·PlayParryHold`）。

**问题在用法不在能力**：
1. 提示出现在**敌人身上**（截图里是白手 + 金色圆环指着敌人）→ 玩家不知道"我要在哪划"；
2. 没有文字说明 → 玩家不知道这是"点一下 / 横划 / 上划招架"；
3. 没有和任务 1 做出来的**手势带/技能排**联动。

**改法**：
- 教学提示的落点从"敌人身上"改为**屏幕下方技能排对应格子**（任务 1 已把那排做成「技能棱形」，
  正好承载"当前该按哪个手势"）；
- 每个动作配一行短文字（点一下 / 按住蓄力 / 上挑破盾 / 横划 / 上划招架 / 上划按住）；
- 闸门与提示**共用同一套视觉**，不要两套并存。

### T4【建议】教学气泡换样式

截图实测：开场台词是**居中的纯白方框 TMP**（`TutorialBubble` 356×169，
canvas x 144..500、y 306..475），无边框、无头像、无打字机，和项目的木���/水墨语言完全不搭。

**改法**：复用 `BattleDialogueView` 已有气泡骨架（`PlayerBubble`/`BossBubble`/`TutorialBubble` + 尾巴贴图），
换水墨底 + 角色小头像（已有 `PlayerDialoguePortrait`/`BossDialoguePortrait`）+ 打字机。

### T5【可选，本项目已立项未做】正式关的「局内即时提示」

**用户 10-27 的原话**：「其游戏中只要有敌人出现，在合适的时机就会出现光圈引导玩家点击」。

这正是项目里**早已立项但没做**的那一项 ——
`plan/tutorial-gesture-ui-implementation-20261026.md` 的「不做（另立项）」列写着
「**正式关首入拍**（C2 复合手势提示 + 破霸体教学）」。

**设计原则**（参考《刀锋》的上下文提示：动作第一次变得重要时才提示）：
1. **一次性**：学会后永不再弹 → 需要一个"已学会哪些动作"的存档位（可复用天赋存档写法）；
2. **不打断**：出招中 / 对话中 / 三选一中 → 排队；
3. **不遮挡**：落在敌人身上但用描边光圈，不占玩家操作区。

**触发建议**：首次遇到 109 骑兵 → 上划招架；首次遇到 1021 盾卫 → 蓄力上挑破盾；
首次遇到远程敌人 → 闪避（依赖任务 3）；首次遇到霸体敌人 → 重攻击破势（依赖任务 3）。

---

## 3. 教学关的战斗内容（改提示时要对齐）

| 资产 | 波次 | 内容 |
|---|---|---|
| `Tutorial_Battle`(9000) | 4 波 | 1011 → 1011×3 → **1021**（盾卫）→ **1012**（霸体教学用） |
| `Tutorial_E0_Battle`(9002) | 2 波 | **1012**（`healthMultiplier=0.4`）→ **109**（骑兵冲锋） |

> 教学关资产齐全、`TutorialStageDirector` 已挂在 `Assets/Scenes/Battle.scene` 的「Tutorial Director」上。

---

## 4. 局内即时提示与任务 1 的耦合

任务 1 做出来的**黄色棱形排**（主动技能 + 道具，按获得顺序左排）目前**只有 1 个**（教学给的冰雾）。
如果 T3 要在"技能排对应格子"上做手势提示，需要**把手势提示和技能排分开两个概念**：
- 技能排 = **已获得的主动技能**（点击释放）；
- 手势提示 = **当前该按的手势**（不是技能，是操作教学）。
→ 建议手势提示做成**独立的一条提示带**（可放在技能排上方或替代它的位置），不要混进技能格。

---

## 5. 坑（都是这次踩过的）

1. **Editor 崩过一次**：2026-10-27 分析过程中原生崩溃（SIGSEGV @ Tuanjie `Application::TickTimer`），
   Locus bridge 随之断开。崩前没保存的内容会丢 → **重要节点先存盘**。
2. **prefab 实例的 added override 会叠加**：在实例里删了重建 → 会出现 3 个同名节点。
   → 改造工具必须幂等（复用优先于新建）。
3. **prefab 与场景会漂移**：场景 HUD 有 157 处手改 override，改 prefab 不同步 → 工具两边各跑一次。
4. **`HeroHUD` prefab 里的 `HudCard` 自带一个 `Canvas`（order 21）+ `GraphicRaycaster`** → 加东西时注意层级。
5. **`ExpGemManager.expSlider` 现在是 null**（等级球取代了经验条），收集点走 `collectTarget`；
   `GetFillEndScreenPosition()` 已改为优先 `collectTarget`。
6. **「点击任意处继续」（`DialogueAdvanceButton`）原本压在屏幕最底正中**，正好盖住血条数字
   → 已在任务 1 里挪到画面中部（canvas y 869..1020）。任务 2 改气泡时要记住它现在的位置。
7. **教学对话的 `DialogueDimOverlay` / `ClickBlocker` 是全屏的**，改气泡位置要一并看。

---

## 6. 开工前建议先做

1. **提交一批**：工作区还有 **51 个已跟踪改动 + 40 个未跟踪文件**未提交，其中包含任务 2 要用的
   `TutorialStageDirector.Beats.cs`、`TutorialAttackGate.cs`、`CavalryEnemy.Exit.cs`、`HubUiRestructure.cs`
   以及整套 `Enemy_1012` 素材。**这些不提交，新对话改起东西来无法区分新旧**。
2. 读一遍 `plan/tutorial-parry-defend-implementation-20261026.md`（教学关招架/防御那一轮的实现记录 + 自测方式）
   与 `plan/tutorial-gesture-ui-implementation-20261026.md`（现有教学手势系统的解剖）。
3. 确认要不要做 T5（局内即时提示）—— 它工作量最大，但和用户「光圈引导」的诉求最贴近。

---

## 7. 本周剩余任务（本周只做到任务 2 为止）

| # | 任务 | 状态 |
|---|---|---|
| 1 | 战斗 UI 改造 | ✅ **已验收**（`619061c`） |
| 2 | 教学关优化 + 提示 UI 改造 | ⬜ **本次要做** |
| 3 | 新敌人开发 | ⬜ 方案已出（`weekly-plan-...md` §7）；P6 已定「本周做 1 个到可玩」 |
| 4 | 正式关卡配置 | ⬜ 方案已出（§8）；**前置**：22 波 vs 6 波的文档口径差异要先回写 |
| 5 | 敌人形象重塑（1012 敌化） | ⬜ 方案已出（§9） |
