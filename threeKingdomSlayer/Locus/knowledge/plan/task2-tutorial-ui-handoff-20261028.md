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

---

# 2026-10-29 复核：4 条勘误 + 1 个新问题（开工前必读）

> 本文是 10-28 的交接。10-29 开工前把交接里的每条主张对着**当前代码 / 资产 / 场景 / Editor 实测**复核了一遍，
> 结论：交接的**方向全对**，但有 4 条细节与现状不符（其中 1 条照抄会直接做不出来），另有 1 个新问题（提示落点偏移）。
> 以下勘误**以本文为准**，与上文冲突时以本节为准。

## 8.1 勘误 1【会直接做不出来】教学关里**没有 1021**，蓄力上挑破盾那一拍不存在

- 交接 §2/T1 表第 3 行写「`Tutorial_Battle` wave 2 → ChargedSwipe（打 1021）」。**这条按字面做不出来**：
  - 实测（`AssetDatabase` 扫全部 scene/prefab + 代码搜 `9000/9001/9002`）：`Tutorial_Battle`(stageId 9000)
    **入向引用 = 0**（场景/预制体/代码都没有）→ 是**孤儿资产**。
  - 教学路线实际用的（`Tutorial_Route` → `Tutorial_Node_N1` → `Tutorial_Node_E0`）：
    | 节点 | 战斗资产 | 波次内容 |
    |---|---|---|
    | N1 | `Tutorial_N1_Battle`(9001) | 波0 = 1011；波1 = 1011×3 |
    | E0 | `Tutorial_E0_Battle`(9002) | 波0 = **1012**(hp×0.4)；波1 = **109** 骑兵 |
  - 1021 **只存在于那个孤儿资产**里。10-26 的设计决定已明确写过：
    「1021 移出教学关（没有 Launch 就破不了盾）」、「教学里**不教蓄力上挑**，它在整个教学关一直是锁的」。
- **用户 10-29 拍板：不加这一拍，教程关内容待定。**
  → 教学拍序固定为 **点按 → 横划 → 招架 → 防御** 四拍；破盾留给正式关的即时提示（另立项）。

## 8.2 勘误 2【T1 的真实缺口】E0 两拍不是"没配"，是**被任务 1 改场景时弄丢的**

- 提交史实测（`git show <commit>:Assets/Scenes/Battle.scene`）：
  | 提交 | 日期 | gestureSteps 条数 |
  |---|---|---|
  | `558dd16` / `f43d912` | 10-25 | **5 条**（N1波0 Tap / N1波1 Swipe / **E0波0 kind3** / **E0波1 kind4** / 路线出口） |
  | `457230b`（任务 1 战斗 UI 改造） | 10-29 | **3 条**（E0 两条消失） |
  | `619061c`（现状 HEAD） | 10-29 | 3 条 |
- 也就是说：**10-25 配过、10-26 改成了招架/防御（`plan/tutorial-parry-defend-implementation-20261026.md` §2 记录了最终 5 拍且自测全绿），
  但那版配置从未进过提交**，随后 10-29 任务 1 大改场景时被覆盖 → 现在只剩 3 条。**
- → **T1 的工作量 = 恢复 2 条配置（纯 Inspector/Unity API 写场景）+ 加一条防回归自检**，不是"新写拍型"；
  代码侧（`TutorialStageDirector.Beats.cs` 的 `FreezeParryRoutine` / `DefendHoldRoutine`）**已存在且 10-26 自测通过**。
- ⚠️ **波次索引口径**：`WaveSpawner.OnWaveStarted` 传的是 **列表序号** `currentWaveIndex`（`Assets/Scripts/Wave/WaveSpawner.cs:170`），
  **不是 waveId** → E0 两波要填 **waveIndex 0 / 1**（它的 waveId 是 2 / 3）。

## 8.3 勘误 3 T2 不能用固定 2.2 秒

- 实测台词长度（`DialogueEventData`）：4 条波次台词 **18~33 字**，气泡只有 **316×150 canvas**。
  2.2 秒读完 33 个字不可能 → 固定 2.2s 会把"卡住等点击"换成"来不及看"。
- → T2 应该是**按字数计时**（`max(tutorialLineSeconds, 字数 × 0.16s)`）或**打字机逐字 + 读完再停一拍**。
- 顺带修正「台词不会自动推进」的影响面：实测 `nonBlocking`——
  `tutorial_t0_opening` = **false（真阻塞**：timeScale=0 + 全屏压暗 + 点击挡，不点就卡住）；
  4 条波次台词 = **true（不阻塞，但气泡赖着不走）**。
  → 真正"卡住游戏"的是**开场那一句**。

## 8.4 勘误 4 T4「纯白方框 TMP」不准确 + 教学关没有收尾台词

- 实测气泡层级（`BattleHUD/Combat/Overlay/Dialogue/DialogueLayer/DialogueOverlay`）：
  `TutorialBubble` / `PlayerBubble` / `BossBubble` **都已经用 `dialogue_bubble_9slice` 九宫格 + `dialogue_tail` 尾巴**，
  字体是 `方正粗黑宋简体 SDF`。不是"无边框白方框"，是**纯白底、无头像、无打字机**。
- 真正缺的：①教学气泡**没有头像槽**（Boss/Player 有 `BossPortrait`/`BossPortraitFrame`）②没有打字机 ③底色纯白，没有水墨感。
- 另：**教学关没有收尾台词** —— `tutorial_t0_death`（已按 10-26 改写成胜利收尾）是**孤儿资产**，导演没引用它。

## 8.5 新问题 D1：手势提示**没有落在敌人身上，有偏移**（用户 10-29 指出，已量化）

- 根因（实测）：提示的锚点是**敌人根节点 = 脚底**，不是可见本体中心。
  - `Assets/Scripts/Tutorial/TutorialStageDirector.Targets.cs:70` → `cam.WorldToScreenPoint(best.transform.position)`（根节点）。
  - 实测敌人几何（prefab）：1011/1012 精灵 `size=32` × `lossyScale 0.08` → **本体高 2.66 世界单位**，
    **根节点(脚底) → 本体中心 = 1.33 世界单位（本体高度的 50%）**。
  - 相机是**透视**（Main Camera，fov 60、z=-10、敌人 z≈0）→ 1.33 世界单位 ≈ **250 px**（1080×2160 下）。
  - 而提示图层 `Finger=220 px`、`Ring=240 px` → **巨大的白手 + 光圈落在敌人的小腿/脚边**，不是身上 → 读起来就是"偏了"。
  - 叠加问题：`Swipe` 拍的 `from01/to01` 是屏幕归一化坐标（`(0.5,0.7)→(0.18,0.7)`），
    换算后**在脚底高度上画一条 346 px 的水平拖尾** → 视觉上更贴地、更不像"指着他"。
- 附带确认（好消息）：`tutorialHold` **同时停移动与停攻击**（`Enemy.Movement.cs:18` / `Enemy.Attack.cs:18`）
  → 提示锚点**不会因为敌人继续走位而失效**；偏移纯粹是"锚点选错"。

## 8.6 参照《上古卷轴：刀锋》重想的提示分工（10-29 用户要求：参考它的指引）

实测结论（依据 `plan/weekly-plan-combat-ui-tutorial-20261027.md` §2.2 用户提供的实机截图 + §2.5 三条总纲）：

| 刀锋的做法 | 对我们的含义 |
|---|---|
| HUD 只有 5 个元素，**中央是战场**、信息贴边缘 | 提示不该堆在玩家操作区，也不该压住战场中央 |
| 中央那个**盾牌图标是"状态显示"**（你正在格挡），不是输入按钮 | 「我做这个动作」的表达 ≠「我有一个按钮」 |
| **没有教程关**，靠**动作第一次变得重要时才提示**（敌人一出现 → 合适时机 → 光圈） | 正式关的即时提示必须**落在敌人身上**（这是刀锋的原做法） |
| 提示 = **一个光圈 + 一条轨迹 + 可选一行短字** | 我们的白手 220 px / 光圈 240 px **太大**，要缩到"描边光圈"级别 |

→ **两层分工**（这是本轮 T3 的方向，待用户点头）：

1. **「打谁」= 世界内**：目标敌人身上一个**小描边光圈**（跟着敌人走、修掉脚底锚点 → 锚到本体中心）。
   刀锋式；正式关的即时提示也复用这一层。
2. **「怎么做」= 屏幕下方**：一条**独立教学提示带**（不占用技能格，位置在底排右侧空位），
   内容 = **小尺寸手势演示 + 一行短字**（点一下 / 横着划 / 上划招架 / 上划后别松手）。
   刀锋把"法术"放在轮盘里调，我们把"手法"放在同一条边缘信息带里 —— 边缘=操作，中央=战斗。

> 待拍板：这条"两层分工"要不要照此落地（以及提示带是否就放在底排右侧空位：屏幕 px x≈590..1060 / y≈200..360）。

---

# 10. 2026-10-29 用户新提的四条方向 → 功能定位与需求分析（待拍板）

> 用户原话要点：
> 1. **不要出现“点一下”这类文字**（文本量暴增、画面变杂）→ 提示只用手势动画。
> 2. 手势引导三个问题：**太小**；**tap 只有光圈、手没有动作**（要几帧手的动作帧）；**锚点该是手指尖，现在手与光圈错位**。
> 3. **引入防御按键、取消精防**：敌人不会因为 parry 被打断攻击动作（省大量动画）；
>    按下防御 → 长枪出现 → 复用「进入 parry 再到 defend」的表现（无精防判定）；
>    若在 parry 表现期间受击 → 直接进 defend 的受击表现（允许武器跳位）。
> 4. 教学要教：**点击 / 划动 / 防御按键**。
>
> ⚠️ **本文推翻 §8.6 的“提示带 + 一行短字”方案**（需求 1 不要文字）→ 提示带改成**纯动画带**。

## 10.1 需求 2：提示 UI —— 定位到 3 个**具体缺陷**（不是感觉问题）

**相关文件**：`Assets/Scripts/UI/TutorialGestureHint.cs`、`Assets/Prefabs/UI/TutorialGestureHint.prefab`、
`Assets/Scripts/Tutorial/TutorialStageDirector.Hints.cs`（演示落点）、`TutorialStageDirector.Targets.cs:70`（锚点来源）。
素材：`Assets/Sprites/UI/Tutorial/tutorial_gesture_{finger,hold_ring,swipe_trail}_ui.png`（各 256×256 **单帧**）。

| # | 缺陷 | 实测证据 | 性质 |
|---|---|---|---|
| **A** | **Tap 的光圈从没被摆位** | `PlayTap()` 只 `ShowLayers(ring:false,…)`（先藏光圈），`TapRoutine()` 里 `ring.SetActive(true)` + 缩放，**全程没有 `Place(ring…)`** → 光圈停在 `anchoredPosition(0,0)` = **屏幕正中** | **代码 bug**（这就是“手与光圈错位”+“tap 只有光圈”的真凶） |
| **B** | **世界锚点是敌人的脚** | `TryGetTargetEnemyScreenPosition` 用 `enemy.transform.position`（根节点=脚底）。实测 1011/1012 本体高 2.66 世界单位，脚底→本体中心 1.33 世界单位 ≈ **250 px**（透视相机 fov60 / z-10 / 1080×2160） | **代码 bug**（与 §8.5 同一个根因） |
| **C** | **尺寸偏小且写死** | `fingerSize=220 / ringSize=240`（`Place()` 每次用组件字段覆盖 `sizeDelta`）→ 1080 宽屏上 ≈ 屏幕宽的 20% | 调参（预制体 + 场景实例两边都要改，任务 1 的老坑） |

**已验证的好消息（不用再改）**：
- 手部贴图的 pivot **已经正好在指尖**：贴图不透明区 (62..193, 48..206)，指尖归一化 ≈ (0.262, 0.813)，prefab 的 `Finger.pivot = (0.26, 0.81)` ✅。
- 光圈与拖尾贴图的视觉中心 ≈ (0.498, 0.502)，`pivot = (0.5,0.5)` ✅。
→ 所以“锚点该是手指尖”这件事**预制体里已经是对的**；真正错的是 **A（光圈没摆位）** 与 **B（锚到了脚底）**。

**tap 手部动作帧**：现有 `TapRoutine` 只是把手**整体缩放** 1→0.88→1（以指尖为轴）+ 光圈扩散 → 读不出“点”。
要做帧 = **新美术产出**（建议 3~4 帧：下压 / 触底 / 回弹），项目已有抠图与切图流水线，但这是排期项，不是代码项。

## 10.2 需求 3：防御按键 + 取消精防 —— 现有“招架/防御”整条链的地图

| 层 | 文件 | 关键内容（取消精防时哪些要拿掉） |
|---|---|---|
| 输入 | `Assets/Scripts/Player/InputManager*.cs`（`Mapping.cs:148`） | 竖划（不蓄力）→ `AttackType.Parry`；教学闸门只放行“未蓄力竖滑” |
| 判定 | `Player/Combat/AttackSystem.Types.cs:449 ExecuteParry` → `:499 ResolveParryContact` | 主动帧模型：输入只起手，**接触帧**判定 `enemy.TryDeflectByParry(window, poiseDamage)`；`HasDeflectTarget` 只决定长短后摇；`_lastParryWhiff` = 空放长后摇 |
| 敌人侧 | `Enemy/Enemy.Combat.cs:277 TryDeflectByParry` | **取消攻击 + 削韧 + 闪白/受击动画** ← 这就是“敌人被 parry 打断”的动画成本来源 |
| 敌人参数 | `Enemy/Enemy.cs:95 parryStrikeWindow`（+ `IsInStrikeWindow()` / `TimeToStrike()`） | 每个敌人的招架窗口 |
| 姿态链 | `Player/Combat/PlayerWeaponStance.cs` | **已经抽好了**：`Idle → SwingIn(挥入) → Defending(按住) → Exiting(收枪出画)`；`PlayParry(cfg)` / `SetParryResult(bool)` / `Recoil(cfg)` / `Tick(dt, pointerHeld)` |
| 伤害 | `Player/Combat/PlayerGuard.cs` | 防御期**免伤**只涨破防值；满 → 破防硬直；`OnDamageAbsorbed`（教学防御拍靠它判成功） |
| 入口 | `Player/Combat/PlayerState.cs:241` | **只有 `WeaponStance.IsDefending`** 才走 `Guard.TryAbsorbDamage` |
| 数据 | `Data/AttackSkillConfig.cs`（`parry*` 30 个字段） | 姿态角度/时长、破防阈值、枪 prefab |
| 遗留 | `Effects/ParryVisualEffect.cs` | 只在 `AttackSystem.Types.cs:684` 用（旧的“自播自毁”招架特效），与 `PlayerWeaponStance` 并存 |

**需求差距**：
1. 现在**没有任何防御按钮**；防御只能靠“上划后按住”（教学拍 `DefendHoldRoutine`）。
2. “取消精防”要拿掉：`ExecuteParry` 接触帧判定、`HasDeflectTarget` 预测、`_lastParryWhiff` 空放后摇、
   `Enemy.TryDeflectByParry`（连带敌人“被格开”的取消攻击/削韧/受击动画）、`parryStrikeWindow`、`LastParryBlocked` 及其“格挡成功→收枪加速”。
3. 要保留：`PlayerWeaponStance` 的挥入→防御→收枪链、`PlayerGuard` 的免伤+破防值、受击后退 `Recoil`。
4. 新增：“**挥入（parry 表现）期间受击 → 直接进 defend 受击表现（允许武器跳位）**”。
   现状：挥入中 `IsDefending=false` → **不吸收，照常掉血**，也没有跳位逻辑。

**建议的剥离/抽取方案（4 刀）**：
1. **防御意图从“手势→招式”里摘出来**：新增极小的 `DefendIntent`（`PlayerState` 持一个 `defendHeld` 布尔）；
   防御按钮（UI）只负责产出这个意图；`PlayerWeaponStance.Tick(dt, pointerHeld)` 的“按住”来源改为 `pointerHeld || defendHeld`。
2. **按钮不走 `AttackType.Parry` 管线**：按下直接 `WeaponStance.PlayParry(cfg)`（复用挥入），**不经过 `AttackSystem.ExecuteParry`** → 天然没有判定、没有打断。
3. **招架判定整块下线**：上表“判定/敌人侧/敌人参数”那几行删除或加开关（`AttackSkillConfig` 加 `parryJudgmentEnabled` 默认 false），
   同时 `Enemy.parryStrikeWindow` 与 `IsInStrikeWindow/TimeToStrike` 的招架用途退役（`TimeToStrike` 教学时停拍还在用，**不能删**）。
4. **挥入期受击跳位**：`PlayerWeaponStance` 加一个 `InterruptToDefend()`（取消挥入 → 落防御位 → 起 `Recoil`），
   由 `PlayerState.TakeDamage` 在 `Phase==SwingIn` 时调用。

**输入层的硬约束（必须先拍板）**：
- 现在**只跟踪一根手指**：`InputManager.Pointer.cs:115` 用 `Input.GetTouch(0)` → 多指时“哪根是游戏指针”不确定。
- 好消息：UI 点击**不会**变成游戏手势（`IsPointerOverUIAt` 当场射线检测，不是 EventSystem 的上一帧缓存）→ **防御按钮不会抢走点击/划动手势** ✅。
- 但“**按住防御键**同时还要划动”在手机上做不到（按住的那根手指会占住 touch[0]）。
  → **A. 点按切换（toggle）**：零输入改造，最稳；**B. 按住维持**：需改输入层为“多指时忽略落在 UI 上的那根”。

## 10.3 需求 4：教学要教 点击 / 划动 / 防御按键

- 现有拍型：`GestureHintKind {Tap, Hold, Swipe, ChargedSwipe, None, Parry, ParryHold}`；
  `StartHint` 按 `freezeOnGesture` / `holdAfterGesture` 分流；闸门 = `InputManager.tutorialGestureTarget`（**只过滤手势，不影响按钮** ✅）。
- 现状拍序：N1 波0 Tap / N1 波1 Swipe / E0 波0 Parry / E0 波1 ParryHold。
- 新拍序：**Tap → Swipe → 防御按键**（E0 波0 = 按下防御键挡 1012；E0 波1 = 保持防御挡 109 冲锋）。
- 需要新增 `GestureHintKind.DefendButton` + “**等按钮被按**”的判定（不能再等手势）→ 按钮组件要暴露 `OnPressed` 事件给导演。
- 提示表达：手部 tap 动作帧可**复用**成“按下按钮”的动画（手移到按钮上 → 按下）→ 正好同时满足需求 1（无文字）与 2.2（手部动作帧）。
- 位置：任务 1 已在底排中间留了**防御按钮占位**（草图：canvas x 341..586 / y 370..792 的黑色盾牌位）✅ 可直接用。

## 10.4 待拍板清单

1. **防御键形态**：点按切换（推荐，零输入改造）还是按住维持（需改输入层多指）？
2. **挥入期受击**：免伤（走防御期口径）还是照常掉血？“武器跳位”只位移还是同时改姿态？
3. **手势招架（竖划）是否彻底下线**（“取消精防”= 只取消判定，还是连手势入口一起取消）？
4. **手部 tap 动作帧**：谁出图（建议 3~4 帧）、放大到多少（现 220px）？
5. **防御键在正式关是否常驻**？

---

# 11. 实施记录（2026-10-29）：五条拍板后的第一轮落地

> 用户 10-29 拍板：① 防御键=点按切换、点击即发动 ② 挥入期受击**免伤** ③ 手势招架**彻底下线但代码全保留**
> ④ tap 手部动作帧由我做生成方案、尺寸放大 1.4 倍 ⑤ 教学关就要教防御按键。

## 11.1 改了什么（代码 / 资产 / 场景）

| # | 需求 | 实现 | 文件 |
|---|---|---|---|
| 1 | **tap 光圈没摆位**（缺陷 A） | `PlayTap` 里补 `Place(ring, screenPos, ringSize)` → 光圈与指尖**同心** | `Assets/Scripts/UI/TutorialGestureHint.cs` |
| 2 | **锚点锚在敌人脚底**（缺陷 B） | `TryGetTargetEnemyScreenPosition` 改用 **SpriteRenderer.bounds.center**（本体中心），无渲染器时用 1.33 世界单位兜底 | `Assets/Scripts/Tutorial/TutorialStageDirector.Targets.cs` |
| 3 | **提示放大 1.4 倍** | `fingerSize 220→308`、`ringSize 240→336`、`trailThickness 48→67`（预制体 + 场景实例两边都改了） | `TutorialGestureHint.cs` + prefab + 场景实例 |
| 4 | **tap 手部动作帧** | 代码先支持多帧：新增 `tapFrames[]` / `tapFrameInterval`；**无帧时改成绕指尖 pivot 旋转 14° 的程序化按压**（pivot 本来就在指尖 → 指尖不动、只有手在压）。帧素材方案见 `plan/tutorial-tap-hand-frames-generation-20261029.md`（**待批准**） | `TutorialGestureHint.cs` |
| 5 | **防御意图与按键** | 新增 `PlayerState.DefendHeld` + `OnDefendChanged` + `ToggleDefend/SetDefend`；`SetDefend(true)` **立即** `WeaponStance.PlayDefend(cfg)`（复用挥入表现）+ 播 `Player_Parry` 音；姿态链的“按住”来源改为 `pointerHeld \|\| DefendHeld` | `Assets/Scripts/Player/Combat/PlayerState.cs` |
| 6 | **挥入期免伤 + 武器跳位** | `TakeDamage` 的吸收条件加上 `IsSwingingIn`（挥入期也免伤、只涨破防值）；命中后调 `WeaponStance.InterruptToDefend(cfg)`（取消挥入 → 0.06s 跳位进防御位 + 起 `Recoil`） | `PlayerState.cs` + `PlayerWeaponStance.cs` |
| 7 | **手势招架彻底下线（代码保留）** | `AttackSkillConfig.parryGestureEnabled`（默认 **false**）；`AttackSystem.TryExecuteAttack` 开头拦截：关掉时竖划（不蓄力）直接 return false（**不消耗动作锁**）。**判定/敌人打断/空放后摇等全部代码原封不动**，勾上开关即可重启 | `Assets/Scripts/Data/AttackSkillConfig.cs` + `Assets/Scripts/Player/Combat/AttackSystem.cs` |
| 8 | **防御键 UI** | 新增 `Assets/Scripts/UI/DefenseButton.cs`（点按切换 + 配色切换）；`HeroHudLayoutTool` 新增 `ApplyDefenseButtonOnly()`（**只加防御键，不动其它数值**——避免覆盖用户正在调的底排）与 `ApplyDefenseButton()`；盾面片 `Assets/Sprites/UI/hud_defend_shield.png`（程序化生成 256×256，导入设置对齐 `hud_orb_circle.png`） | `DefenseButton.cs` / `HeroHudLayoutTool.cs` / `hud_defend_shield.png` |
| 9 | **教学新拍型** | `GestureHintKind` 新增 `DefendButton`；`StartHint` 新分支 → 走 `DefendHoldRoutine`（闸门改为**不限手势**、不教招式）；提示落点改成**防御按钮的屏幕坐标**，`PlayTapLoop` 循环点它；拍成功按 `despawnTargetOnSuccess` 决定目标是否离场；拍结束自动 `SetDefend(false)` 收枪 | `TutorialStageDirector.cs` / `.Hints.cs` / `.Beats.cs` |
| 10 | **场景与台词** | `gestureSteps` 改为 5 条：N1波0 Tap / N1波1 Swipe / **E0波0 DefendButton** / **E0波1 DefendButton(despawn=true)** / 路线出口；台词 `tutorial_t0_wave3` “按下防御键…” / `tutorial_t0_wave4` “按住防御键别松手…” | `Assets/Scenes/Battle.scene` + `Assets/ScriptableObjects/Dialogue/tutorial_t0_wave{3,4}.asset` |

## 11.2 Play Mode 实测（全部通过，Console 0 error / 0 warning）

| 项 | 结果 |
|---|---|
| 点按防御键 | `DefendHeld=True` → 姿态 `SwingIn`（**立即发动**）→ 0.8s 后 `Defending` ✅ |
| 再点一下 | `DefendHeld=False` → 收枪 → `Idle` ✅ |
| **挥入期受击** | HP **100 → 100（免伤）**；姿态 `SwingIn → Defending`（跳位）；破防值 +23.6 ✅ |
| 手势招架下线 | 竖划(不蓄力) `SubmitGestureForDebug` 返回 **false**（不再出招）✅ |
| tap 提示 | 手指与光圈 `anchoredPosition` **完全相同**（同心）✅；手 308 / 光圈 336 ✅；按压时 rot -13.5°、scale 0.94，结束归位 ✅ |

## 11.3 遗留 / 下一步

1. **tap 手部帧**：方案已写（§4 生成方案文档），**待批准后生成**；代码已支持 `tapFrames`，到位即接管。
2. **防御键美术**：现在是程序化盾牌面片，等正式美术替换。
3. **招架旧资产**：`AttackSkillConfig.parryWhiffDuration` / `parryStrikeWindow` / `Enemy.TryDeflectByParry` 等**全部保留但已不再被触发**（敌人不再被 parry 打断 → 敌人侧那套“被格开”动画不用做了 ✅）。
4. **教学时长**：E0 两波现在要玩家先按防御键 → 演示节奏变了，需实机跑一遍确认 3 分钟口径。
5. **正式关**：防御键是通用机制（正式关也有），教学关负责教 ✅。



---

# 9. 攻击提示 + 攻击命中提示：设计思考（2026-10-29，待拍板）

> 用户 10-29 追加：「我们需要做攻击提示和攻击命中的提示，思考刀锋是怎么做的」。
> 本文 = 参照《刀锋》的**设计结论**（含我们能证实与只能推论的边界）+ 对照本项目现状的**缺口表** + 建议方案。

## 9.1 刀锋是怎么做的（已证实 / 推论，分开标注）

**已证实**（用户 10-27 提供的实机截图 412×810，竖屏；以及联网核实的官方资料）：

| 维度 | 刀锋的做法 | 对我们的含义 |
|---|---|---|
| **攻击提示·输入语义** | 点按=攻击、滑动=格挡、按住=蓄力 —— **没有"招式按钮排"** | 手势语义少而固定时不需要提示图标；**我们的手势词汇表更宽**（点/横划/竖划/蓄力竖划/长按…）→ 这层缺口是我们自己的，不能照抄"它不需要" |
| **攻击提示·多选项** | 法术同时最多装 3 个，通过**轮盘**呼出；HUD 上**只常驻 1 个"大招就绪"指示**（左下蓝色菱形） | 「多选项用轮盘，不占常驻位」→ 我们的黄色棱形排（主动技能）已经是这个位置；**招式提示不该再占一排图标** |
| **攻击提示·状态 vs 输入** | 屏幕中央那个米白色盾牌图标 = **"你正在格挡"的状态显示**，临时出现、无按钮底 | 「状态显示」与「输入提示」是两种东西，**不能混**：我们"我格开了"目前只有镜头震动，没有状态符号 |
| **攻击提示·何时出现** | **没有教程关**；靠「敌人一出现 → 合适时机 → 光圈引导点击」（用户原话） | 提示只在**动作第一次变得重要**时出现；一次性、不打断 |
| **命中提示·落在哪里** | 反馈**放进世界**：命中点伤害数字；精英敌人顶部命名血条（左灰白残影=已受伤害 / 红=当前 / 黑=已失去） | 我们 weekly plan §2.5 C 已定为参考项；**不要把命中反馈堆进底排 HUD** |
| **命中提示·不怎么做** | 不做全屏闪光 / 不做长镜头抖动（它的战斗本身就被评"触屏不精确、割草"） | 反馈要**克制**；噪音叠上去只会更糟 |

**只能推论**（没有证据，别当事实用）：
⚠️ 我们**没有**刀锋的战斗录屏（weekly plan §2.6 明确记着"录屏 ❌ 仍缺"），所以
「刀锋命中时的卡肉时长 / 是否有命中顿帧 / 音效分层」**全部是推论**，不可直接引用。

## 9.2 本项目现状盘点（实测）

### 攻击提示侧

| 已有 | 说明 |
|---|---|
| `ChargeIndicatorController` | 蓄力环，**跟随手指**（项目里现成的正确写法） |
| `PierceAimIndicator` | 穿刺瞄准指示 |
| `EnemyAttackTelegraph` | 敌人起手三帧预警（棱形点→十字星→十字星+外环），挂在敌人身上 |
| 黄色棱形排（任务 1） | 主动技能 + 道具，按获得顺序左排 |
| 招式映射数据 | `MoveTableConfig`（手势→招式**数据驱动**）；默认落点表：Tap→Stab、Hold→Pierce、竖滑→Parry(不蓄力)/Launch(蓄力)、横滑→Slash(不蓄力)/Sweep(蓄力) |

### 缺口

| # | 缺口 | 说明 |
|---|---|---|
| A1 | **手势→招式的映射没有 UI** | 任务 1 已把 6 个招式冷却图标退役（照刀锋"不按招式组织 HUD"的方向），但**替代物没做** → 玩家不知道自己这一划打出了什么 |
| A2 | **"此刻该用什么招"的取舍没有表达** | 对 109 该招架、对 1021 该上挑、对霸体该重击 —— 全靠玩家自己试 |
| A3 | **没有"状态符号"** | 格挡成功 / 处于防御 / 可取消 —— 只有镜头与特效，没有一个"我刚才做对了"的符号 |
| A4 | 提示带与手势带的概念还没分家 | 教学提示带（怎么做）vs 正式关对策提示（对谁用） |

### 攻击命中提示侧

| 已有 | 说明 |
|---|---|
| 局部卡肉 | Light 45ms / Standard 90ms / Heavy 140ms（`HitFeedbackManager`，只冻受击者 Animator，**不动全局 timeScale**） |
| 受击闪白 + 受击缩放 | `EnemyHitFeedback`（强度分级 Light/Standard/Heavy，霸体时封顶 1.1） |
| 命中点特效 | `PixelHitEffectManager`（像素十字 / 斩口，Standard/Heavy/Slash 三套参数 + 对象池） |
| 伤害数字 | `DamageNumber`/`DamageNumberManager`（按强度缩放，Heavy 额外放大；**盾格挡有专属颜色**） |
| 镜头 | `CameraFeedbackController`（`heavyOnlyCameraShake` + 限频 0.12s）；格挡另有 `RequestGuardBlock`；招架另有 `RequestParryImpactFeedback` |
| 格挡吸收 | `PlayerGuard.OnDamageAbsorbed`（教学防御拍靠它判成功） |

### 缺口

| # | 缺口 | 说明 |
|---|---|---|
| B1 | **命中没有"语义"，只有强度** | 打在盾上 / 打在肉上 / 格开 / 破势 / 击飞，视觉上只差一个颜色或大小；玩家不知道为什么"没打进去" |
| B2 | **打空没有反馈** | 挥空时没有任何东西告诉玩家"这一下空了" |
| B3 | **普通敌人血条常显**（U4 未做） | 世界内的血量信息把命中反馈挤掉；且与刀锋"只有精英有命名血条"的结构相反 |
| B4 | 受击缩放仍写在**敌人根 Transform** | 与位移/翻面/击飞 Tween 抢同一个 Transform（`design/combat-hit-feedback-direction.md` 已登记该风险） |

## 9.3 结论：两类提示必须**分开落点**（这是刀锋最大的贡献）

```
屏幕下方 / 边缘  =  「怎么做」   → 操作提示（手势带、提示带、状态点）
世界内 / 目标身上 =  「打谁 / 打到了吗」 → 对策提示 + 命中反馈
```

刀锋的 HUD 只有 5 个元素、中央永远是战场 —— 它把**操作**放边缘、**结果**放世界。
我们照抄这个分区，但不抄它的"只有 5 个元素"（我们的玩法信息量更大）。

## 9.4 攻击提示：三类，落点与生命周期各不相同

| 类型 | 回答 | 落点 | 触发 | 生命周期 | 刀锋对应 |
|---|---|---|---|---|---|
| **A. 操作教学提示** | 这一下怎么做 | 屏幕下方**独立提示带**（底排右侧空位 x≈590..1060 / y≈200..360） | 教学拍 | 教学期 | 刀锋没有（我们独有） |
| **B. 对策提示**（正式关） | 对**这个**敌人用什么招 | 目标敌人身上**小描边光圈** + 一行短字 | 首次遇到该机制 | **一次性** + 冷却 | 刀锋的"光圈引导点击"（U5）→ **用户 10-29 定：本轮先不做** |
| **C. 状态提示** | 我现在是什么状态 | 中央临时图标（仿刀锋中央盾牌） | 状态变化 | 状态持续期间 | 刀锋中央盾牌 |

**本轮只做 A**，但 A 的组件要写成 B/C 能复用的形态（刀锋式"一个光圈 + 一条轨迹 + 可选一行短字"）。

**A 的具体形态（建议）**：
- 一行短字（点一下 / 横着划 / 上划招架 / 上划后别松手）+ 一个**小尺寸**手势图标原地循环。
- **缩掉现在的白手**：现状 Finger 220 px / Ring 240 px 相对敌人（本体高 2.66 世界单位 ≈ 500 px）过大 →
  改成描边光圈级别（约 1/2）。
- 不阻塞：不暂停游戏、不抢输入；出招中 / 对话中 / 三选一中排队。

## 9.5 命中提示：把"强度"升级成"语义"（低成本、高收益）

现有反馈分级回答的是"**打得多重**"，不回答"**打出了什么**"。建议在命中点加**语义符号**（程序化几何即可，不等美术）：

| 情形 | 现在 | 建议 |
|---|---|---|
| 打在护盾上 | 只有跳字换色 | 跳字 + **盾形符号**（灰白） |
| 招架成功 | 镜头重击级震动 | 镜头 + **环形冲击** + 专用符号（已有 `RequestParryImpactFeedback`，补符号） |
| 破势 / 破盾 | 无区分 | **碎裂符号**（金） |
| 挥空 | **无** | **落空符号**（灰）+ 短促音效 |
| 重击 / 击飞 | 数字放大 | 已有（Heavy 放大）→ 再加**数字描边变金** |

**与 U4 的耦合**：普通敌人血条若改为"受击后短暂闪现"（weekly plan §10 的退路方案），
世界内就腾出了"命中反馈"的位置 —— **建议 U4 与命中符号一起做**，否则符号会被常驻血条挤掉。

## 9.6 排期建议与风险

| 项 | 本轮（10-29） | 之后 |
|---|---|---|
| 攻击提示 | **A（教学提示带：短字 + 小手势图标）** = 任务 2 的 T3 | B（对策提示/光圈引导，另立项）、C（状态符号） |
| 命中提示 | **B1/B2 的语义符号**（纯表现，复用 `PixelHitEffectManager` 的池，低风险） | U4 血条改造（与命中反馈一起做）、B4 缩放所有权 |

风险：
1. **反馈噪音**：刀锋的战斗本身就被评"割草、触屏不精确"；提示与命中反馈必须限频、不叠加、不阻塞。
2. **坐标空间**：提示图层 `Tutorial Gesture Hint` 的 CanvasScaler 是 ScaleWithScreenSize(1080×1920, match 0.5) → 1 单位 ≈ 1 像素；
   `BattleHUD` 是 1.125 → 两边坐标不能直接抄，必须用屏幕像素换算。
3. **不要恢复 6 个招式图标排**（与任务 1 已定方向冲突）。

---

# 12. 修：教学跑不通 + 防御键吞手势（2026-10-29 用户实机反馈）

> 反馈：「开局的点击会点到防御键」「手势输入似乎被防御按键吞掉」。

## 12.1 根因（按输入映射规则核对）

输入规则在 `Assets/Scripts/Player/InputManager.Pointer.cs:20`：
`IsPointerOverUIAt()` **当场射线检测 EventSystem** → 命中任何 `blocksRaycasts` 的 UI → **这次输入不算游戏手势**
（`isTouching=false` 直接 return）→ 变成一次 UI 点击（例外：QTE 活跃时）。

→ 所以**任何带 raycastTarget 的 UI 都会吞掉它下面的手势**；防御键是 Image+Button（raycastTarget 默认开）→ 符合规则地吞手势。
→ 根子不在“按钮位置”，而在**缺少区域位置识别**：规则只分“UI / 非 UI”，
  没有“这次点击该给 UI 还是给世界”的判断 → 压在敌人上的动作按钮就把攻击手势吃掉了。

## 12.2 改了三处（**没有改按钮位置**）

| # | 改法 | 实现 |
|---|---|---|
| 1 | **区域位置识别**（核心） | 新增 `Assets/Scripts/Data/IActionButton.cs`（数据层共享契约，UI 层与输入层都能看见）→<br>• 输入层 `InputManager.Pointer.cs`：新增 `GetTopUIAt()` + `ShouldKeepGestureOverUI()` —— 命中的是**动作按钮**（`IActionButton`）且它判定“让给战斗” → **放行手势**；**模态 UI**（三选一/对话/结算）仍照旧吞掉点击。<br>• 界面层 `DefenseButton`：实现 `IActionButton.ShouldYieldToCombat()` → `EnemyManager.TryGetEnemyAtScreenPoint()`（新增：把敌人精灵包围盒投影到屏幕做点命中）→ 点下面有活敌人 = 那是攻击，按钮让位；`OnPointerClick` 同规则 → 不切换防御。 |
| 2 | **教学关教到之前锁住** | `DefenseButton.SetAvailable(bool)`：锁住 = `CanvasGroup.alpha=0` + `blocksRaycasts=false` + `interactable=false`（**CanvasGroup 与 Image 同节点** → 射线不命中 → 既不会被点到，也不吞手势）。`TutorialStageDirector.RefreshDefenseButtonAvailability()` 每帧同步：教学路线且未完成且还没教到 → 锁住；教到 `DefendButton` 拍（`StartHint` 里 `_defendTaught=true`）→ 开放；正式关/教学已完成 → 一直开放 |
| 3 | **位置保持草图预留位**（还原） | `HeroHudLayoutTool.ApplyDefenseButton`：`SetRect(..., anchor(0.5,0), pivot(0.5,0), pos(0,440), size(190,190))` → canvas x 385..575 / y 440..630（px 433..647 / 495..709），对应草图「屏幕中央黑盾牌」预留位（canvas x 341..586 / y 370..792，中心 463,581）。**不擅改位置。** |

> ⚠️ 中途我曾擅自把按钮挪到底排右端（想绕开敌人区）—— **已还原**。绕不开也不该绕：
> 正解是**区域位置识别**（按钮让位给战斗），不是搬家。

## 12.3 实测（Play Mode）

| 项 | 结果 |
|---|---|
| 开场：防御键 | `Available=False`、alpha=0、blocksRaycasts=false → 射线**完全不命中**它 → 开局的点击不会被它吞 ✅ |
| 敌人本体中心 `IsPointerOverUIAt` | false → 点敌人照常出招 ✅ |
| 解锁后：按钮中心射线命中 | `DefenseButton`（`IActionButton` = true）✅ |
| **敌人挪到按钮区内** | `ShouldKeepGestureOverUI = true` → **手势放行 → 那一下是攻击，按钮让位** ✅ |
| 点按钮（下面有敌人） | `DefendHeld` 不变 ✅（不误切防御） |
| 落地页「开始关卡」按钮 | 战斗态下 `Hub/Bottom` 整组失活 → 不再吃战斗里的点击 ✅（顺带确认） |
| Console | 0 error / 0 warning ✅ |

> 顺带澄清一条误判：中途我一度以为“落地页的开始关卡按钮在战斗里仍吃点击”，那是我测试流程错了
> （在路由就绪前就调了 `RunPrepared`，落地页与战斗同时在场）。正确流程下战斗态 `Hub/Bottom` 是失活的。

## 12.4 分层注意（本次踩到）

`Assets/Scripts/Player/` 属于 **Locus.Flow**（asmref），**看不到 Locus.UI** → 输入层不能直接引用 `DefenseButton`。
→ 共享契约必须放**数据层**（`Locus.Data`，UI/Flow 都能引），这就是 `IActionButton` 放数据层的原因。

## 12.5 遗留

- 防御键在草图位（底排上沿居中）✅ 已还原；**需要用户看一眼真机是否顺手**（不擅自再动）。
- 正式关里防御键一直开放；若某个敌人正好站在按钮后面，点它会是攻击（按钮让位）→ 这是区域识别的既定规则。

---

# 13. 2026-10-29 收尾：只补功能缺口，画风统一顺延

> 用户定：**「算了先不出新款式的UI，只做还没有的，后面再做画风统一」**
> → 美术类（tap 手部动作帧、防御键图标的画风打磨）**全部顺延**；本轮只做功能上还没有的。

## 13.1 本轮补掉的功能缺口：教学台词自动推进（T2）

- 缺口：`tutorialLineSeconds`（2.2）**全库零引用** → 教学台词一直卡到玩家点。
- 实现：`Assets/Scripts/UI/BattleDialogueView.cs`
  - 新增 `_tutorialAutoAdvanceMinSeconds = 2.2f` + `_tutorialSecondsPerChar = 0.16f`；
  - **只有 `DialogueEventType.Tutorial` 自动推进**（正式对话不自动）；
  - 时长 = `max(2.2, 字数 × 0.16)`（中文读速 ≈ 6 字/秒）——**固定 2.2 秒读不完 20~30 字的教学台词**；
  - 玩家点**仍可立即跳过**；用 `unscaledDeltaTime`（暂停时也走）。
- 实测：24 字台词自动关闭 **3.8 s**（= 24×0.16 ✅），26 字 **4.1 s** ✅。

## 13.2 教学全链路实跑（Play Mode，程序化驱动）

| 段 | 结果 |
|---|---|
| 落地页 → 点「开始关卡」 | 路线进入 Battle ✅ |
| 开场台词 | **自动**推进（不再等玩家点）✅ |
| N1 波0 点拍 | 提交点按 → 敌人 100 → **70** ✅ |
| N1 两波清完 | 波浪/三选一（冰雾，每波再给一次）/ 经验宝石收集 ✅ |
| 选路 → E0 | `[FakeRoute] battle entry completed node=E0 index=0` ✅ |
| E0 两波（1012 → 109） | ✅；**109 走 `CavalryEnemy/<TutorialExitRoutine>` 离场**（= 防御拍吸收了冲锋）✅ |
| 结算 | `通关推进当前关卡: 教学关·初见 → 长坂坡·卷轴`、`铜钱 +300`、`教学关已完成并记档` ✅ |

→ **教学关当前可以完整跑通并通关**（这是用户 10-29 报的「教程跑不通」的验证）。

## 13.3 顺延清单（美术 / 画风统一，下一阶段）

| 项 | 状态 | 备注 |
|---|---|---|
| tap 手部动作帧 | **顺延** | 代码已支持多帧（`TutorialGestureHint.tapFrames`），无帧时用**绕指尖 pivot 旋转 14°** 的程序化按压；帧素材待「画风统一」后一起出 |
| 防御键图标画风 | **顺延** | 现有可用（扁平对分纹章盾，219×256，比例 0.86，导入设置已对齐）；正式画风随统一一起做 |
| 气泡换样式（头像 / 打字机 / 水墨底） | **顺延**（打字机是功能项，可单独做） | |
| 局内即时提示（T5） | **不做**（用户 10-29 定） | |
| U4 普通敌人血条 | 另立项 | 与命中反馈语义符号一起做 |

---

# 14. 防御按键：hold 语义 + 亮度联动 + 长按冲突（2026-10-29 用户驳回后重做）

> 用户驳回：「格挡图标的显示逻辑与交互逻辑有误」
> ① 应该为 **hold 长按保持防御，松开离开防御**（不是点按切换）；
> ② **能防御时图标亮，不能防御时图标不亮**；
> ③ hold 时要注意**蓄力等长按**问题。

## 14.1 三条怎么落地的

| 条 | 实现 |
|---|---|
| ① hold 语义 | `DefenseButton` 改实现 `IPointerDownHandler` / `IPointerUpHandler`：`OnPointerDown` → `SetDefend(true)`；`OnPointerUp` → `SetDefend(false)`；`OnDisable`（HUD 隐藏）也释放 → 不会卡在防御中。**不再用 onClick**（onClick 一律清空，避免双触发） |
| ② 亮度 = 能不能防御 | `DefenseButton.Update()` 每帧算：`player.IsDefendingActive || player.CanStartDefend()` → 亮（白）；否则暗（60% 灰）。`CanStartDefend()` = **动作锁未结束**（出招中不能防御）→ **出招时图标变暗**，一眼看出“现在防不了” |
| ③ 长按/蓄力冲突 | `InputManager.Pointer.cs` 新增 `PickGameplayTouch()`：**游戏指针只取“不在 UI 上”的手指**（已跟踪的手指优先）→ 按住防御键的那根手指**永远不会**成为游戏指针 → 不会误触发蓄力 / 长按 / 点按。鼠标路径本来就只按下/抬起时判定，天然不冲突 |

## 14.2 顺带修掉的一个真 bug：区域位置识别过宽

- 旧实现用**敌人世界包围盒投影后的 AABB** 判“点下面有没有敌人” →
  透视下那个框大得离谱（实测 1011 投影出 X=-163..951 / Y=594..1692）→ **几乎全屏都算“敌人身上”**
  → 防御键**永远点不动**（按了没反应）。
- 新实现：命中范围 = 敌人**胸口的核心圆**（半径 ≈ 敌人屏高的 22%，实测 ≈132 px）
  → 点**敌人胸口** = 攻击；点**按钮** = 防御；点敌人边缘但正好压在按钮上 → 以按钮为准。

## 14.3 实测（Play Mode）

| 项 | 结果 |
|---|---|
| 按钮中心 (540,600) 是否命中敌人 | **false**（敌人的胸口在 y 1089~1292）→ 按钮可用 ✅ |
| **按住** | `DefendHeld=True`、姿态 `Defending`、图标 **亮** ✅ |
| **松开** | `DefendHeld=False`、姿态 `Idle` ✅ |
| **出招中** | 图标变 **60% 灰**、`CanStartDefend=False` ✅ |
| 出招中按防御键 | 被拒绝（`DefendHeld` 不变）✅ |
| Console | 0 error / 0 warning ✅ |

> 多指场景（一只手按住防御键、另一只手划屏幕）无法在编辑器里模拟 → 需真机验证；
> 逻辑已改在 `PickGameplayTouch()`（游戏指针只取不在 UI 上的手指）。

---

# 15. 修：旧的“按住任意处也能回防御”残留（2026-10-29 用户指出）

> 用户反馈：「玩家松手时应该按回防御键才能继续防御，当前存在旧的输入逻辑残留，
> 玩家可以在进入防御后长按任意处而不是按钮位置回到防御 → 玩家想做别的指令，
> 按理来说应该是锁住玩家的点击攻击，但变成了回到防御状态。」

## 15.1 根因

姿态链的“按住”来源还挂着**游戏指针**：
`PlayerState.Update` → `WeaponStance.Tick(dt, pointerHeld || DefendHeld)`，
其中 `pointerHeld = InputManager.PointerHeld`（**屏幕任意处按住**）。
而 `PlayerWeaponStance.Tick` 里有一条：
`if (Phase == Exiting && _holding) EnterDefend();` → **收枪动画途中只要屏幕还按着，就又回到防御位** ❗
→ 玩家在屏幕任何地方长按（想蓄力/想点别的）都会被拖回防御状态。

## 15.2 改法

| 处 | 改前 | 改后 |
|---|---|---|
| `PlayerState.Update` | `WeaponStance.Tick(dt, pointerHeld \|\| DefendHeld)` | **`WeaponStance.Tick(dt, DefendHeld)`** —— 防御**只认防御键的 hold** |
| `InputManager.PointerHeld` | `isTouching \|\| forcePointerHeld` | `isTouching` |
| `InputManager.forcePointerHeld` | 教学用的“强制按住”开关 | **删除**（字段 + 教学导演里的 4 处引用全清） |
| `AttackSystem.TryExecuteAttack` | — | 不变：防御中出招仍被**锁住**（用户期望的“锁住点击攻击” ✅） |

## 15.3 实测（Play Mode）

| 项 | 结果 |
|---|---|
| 按住防御键 | `DefendHeld=True`、姿态 `Defending` ✅ |
| 松手 | `DefendHeld=False`、姿态 `Idle` ✅ |
| 再按回防御键 | 姿态回到 `Defending` ✅（必须按回按钮才能继续防御） |
| 防御未开时屏幕按住（喂 `pointerHeld=true`） | 姿态 `Exiting → Idle`，**不会回到防御** ✅ |
| Console | 0 error / 0 warning ✅ |

> 语义至此完全对齐：**防御 = 按住防御键**；松手即退出；防御中攻击被锁住；
> 屏幕其它地方的长按 = 游戏自己的蓄力，不会再把玩家拖回防御。

---

# 16. 被动技能排：底排右侧，从右往左（2026-10-29 用户提）

> 用户：「我把被动技能给忘了 / 将其放到右侧排列如何，从右往左，参考刀锋的另一种技能的摆放。」

## 16.1 现状与模型

- 被动技能共 **7 种**（`UpgradeCategory.AttackPassive` 4 个 + `TimedPassive` 3 个），
  都有 `UpgradeDefinition.icon` ✅（如 连锁弹射 `icon_31_shandian` / 旋风 `icon_31_cyclone`）。
- 局内持有在 `PlayerState.acquiredUpgrades`（`List<UpgradeAcquired>`：definition + currentLevel）✅。
- **但局内 HUD 里一直没有任何地方展示它们** —— 左侧黄色棱形排只装**主动技能 / 道具**（可点的），
  `BuffDisplayPanel` 展示的是**状态 buff**，不是被动技能 ❗（这就是“忘了”的那部分）。

## 16.2 实现

| 项 | 做法 |
|---|---|
| 位置 | `HudCard/PassiveSkills`：**底排右端**，canvas x 587..920、**与黄色棱形排完全同一水平线**（y 183..322）→ 左侧“能点的”、右侧“被动生效的”，两排分开 |
| 形态 | **与黄色棱形槽同款**：直接复制 `FrontItemBar/ItemSlot_0` 的结构（`hud_slot_diamond` 底框 + ButtonFace + Icon + 角标），尺寸同为 **72×139**；被动不可点 → 去掉 Button；冷却遮罩与中间大数字关掉 |
| 排列 | **最右侧往左排**：索引 0 = 最右 = 最早获得，新获得往左长（Slot_0 在 x 848..920 … Slot_3 在 x 587..659，间距 87 = 72+15，同棱形排） |
| 数量 | 4 个槽位 + 超出显示 “+N”（标签贴在被动排最左 x 507..567） |
| 内容 | 升级自带图标 + 角标等级（1 级不标数字） |
| 新增 | `Assets/Scripts/UI/PassiveSkillRow.cs`（数据源 = `PlayerState.acquiredUpgrades`，按内容签名变化才重排） |
| 两个必踩的坑 | ① 复制来的槽位里 `Icon` 是**禁用**的（空槽惯例）→ 填内容时要 `icon.enabled = true`；② 槽内的 `Frame`（不透明黄菱形）会**盖住图标** → 被动槽要关掉它 |

实测（Play Mode，注入 连锁弹射 Lv3 / 虚幻武器 Lv1 / 旋风 Lv2 / 回旋镖 Lv5）：
Slot_0=连锁弹射(3)、Slot_1=虚幻武器、Slot_2=旋风(2)、Slot_3=回旋镖(5) ✅（从右往左 ✅）。
截图：`Library/Locus/Screenshots/locus_game_20261029_092152_193.png`。

## 16.3 ⚠️ 布局工具已删除（2026-10-29 用户定）

- `Assets/Scripts/Editor/HeroHudLayoutTool.cs` **已删除**（连 .meta ✅）——用户要求“直接移除相关部分，否则后续总是会有 agent 用这个东西”。
- 背景：项目 **2026-10-24 已决定“不做自定义布局工具”**（`plan/hub-layout-rework-20261024.md` → “手动调整位置与尺寸（**不用额外工具**）”），
  理由：Unity 原生就能做（Scene 视图 RectTransform 工具 / Inspector 填数值 / Play Mode 预览），自定义工具反而带来偏差。
- **后续改 HUD 布局一律：在 prefab 或场景里直接改数值** ✅，不要再写工具。
  - 场景 HUD 是 prefab 实例（`Assets/Prefabs/UI/HeroHUD_Zhangfei.prefab`）→ 在 prefab 里改，场景实例自动跟随；
    但**同名节点的增删**要小心：实例里若有一个与 prefab 同名的 **added 子节点**，删掉它会被 Unity 记成“移除 prefab 同名子节点”的 override（实例就不继承了）→ 这种情况直接在场景里建一份即可。
- 保留的：`Tools/落地页/搭建落地页 UI`（`HubSceneSetup.cs`）—— 10-24 决定保留（幂等重建，非交互工具）。









