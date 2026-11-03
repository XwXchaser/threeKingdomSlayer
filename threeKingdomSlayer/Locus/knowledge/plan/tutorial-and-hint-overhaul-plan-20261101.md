---
id: kd_0e7deae5-7d7f-4597-bd25-449b8e5e1e65
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 教学关 + 提示 UI 改造落地方案（2026-11-01）

> 来源：用户 2026-11-01 口述的改动内容与方向。
> 本文 = **需求拆解 + 现状实测 + 落地方案 + 拍板项**。**未改动任何工程代码/资产**（纯分析）。
> 上游：`plan/weekly-plan-combat-ui-tutorial-20261027.md`（任务 2 方案与实施记录）、
> `plan/task2-tutorial-ui-handoff-20261028.md`（任务 2 交接 + 攻击提示分析）、
> `design/attack-interrupt-system.md`（打断与霸体规则）、`memory/user-preference.md`（不自行追加需求）。
> 纪律：本文所有现状数字均为 **2026-11-01 在本工程实测**（Editor 反射 + 资产读 + 源码读）；
> 与代码不一致时以代码/资产为准。

---

## 〇、决策记录（2026-11-01 用户拍板）

| # | 问题 | 决定 | 对方案的影响 |
|---|---|---|---|
| D1 | 节点二内容2「用技能打断霸体」教什么 | **教三选1拿到的技能** | 内容2 = 新拍型「点技能槽 → 释放 → 霸体被打断」；三选1 必须包含能打断霸体的技能 |
| D2 | 三选1 的内容与时机 | **全程只有一次三选1**，就在节点二内容1之后；**节点一不给任何技能** | ① 节点一 `suppressChoices=true`（去掉现在的固定冰雾）；② 节点二内容1 结束后弹一次 3 选 1；③ 冰雾不再从三选1 来（但它本来就有，见 §0.1） |
| D3 | C2 是否锁到天赋 | **锁 + 老存档自动解锁一次** | `talent_unlock_c2.moveConfig = Zhangfei_StabR2`、`hidden=0`；`TalentSaveSection` 迁移时给老档补解锁 |
| D4 | 常驻点击特效范围 | **全局**（正式关也常驻） | `AttackTapIndicator` 全局启用，不限教学关 |

### 0.2 实施优先级（2026-11-01 用户定）

> 用户：「后续我可能会改天赋页面和分类，**所以先完善教学关卡**」

| 批次 | 内容 | 状态 |
|---|---|---|
| **第 1 批（已完成）** | **A 节点一**（点按 → 三连 → 打断说明）+ **B 节点移动黑幕** + **C 节点二**（霸体时停防御 + 血量配置 + 三选1 + 技能打断） | ✅ 2026-11-01 已实现并 Play Mode 跑通；⚠️ **常态点击引导（手/光圈）还有两个未修复问题 → §0.13** |
| 第 2 批 | **E 常驻点击特效**（全局，独立于教学，可随时做） | 待做 |
| 第 3 批（等天赋页面改完） | **D 元教程**（落地页 → 天赋 → C2 → 正式关内教 C2）+ C2 锁天赋 + 老存档自动解锁 | 暂缓 |

> D 暂缓的连带影响：C2 在此期间**仍然白送**（`talent_unlock_c2.moveConfig` 仍为空）；教学关部分不依赖它，已独立完成与验收。

### 0.14 第十轮：常驻引导的手/光圈【已修复】（2026-11-02，自测截图 + 像素测量）

> 用户 2026-11-02：「错位问题依然没有解决，且光圈当前非常大」+「明明对应位置没有敌人却有光圈提示」→ 本轮自测定位并修完。

#### 定位过程（自测证据）

1. **上一轮那批「pivot 改掌心 + 位置补偿」的编辑其实整批回滚了**（工具返回 `Edit 5 of 5 failed` → 整批未应用）。
   → 你截图时的状态是：**pivot 已改到掌心、但位置补偿没加** → 手整体压在目标点上、指尖偏到左上 ≈200px → 正是你看到的“错位”。
   → 补上补偿后，实测 **指尖→圈心偏差 14.2px**（= 按压弧线）。
2. **用像素量代替反推**（之前全是推的）：
   - 手指图 `tutorial_gesture_finger_ui.png`（256²）：alpha 内容 x=62..193 / y=49..207；**最上一行只有 6px 宽 → 指尖在 UV(0.273, 0.801)**；手掌主体在 y≈0.42~0.55 → **掌心 (0.48, 0.48)**。
   - 光圈图 `tutorial_gesture_hold_ring_ui.png`（256²）：内容 x=19..236 / y=21..234（占 85%），内容重心 UV(0.498, 0.489) ≈ pivot 中心 → **光圈是居中的**，可直用 pivot 对准。
   - 之前的 `TipUV/PalmUV` 是从 prefab 的 pivot 反推的，不是量图得来的 → 改成量出来的值。
3. **光圈过大**的根因：常态引导用的是**固定设计像素**（480），而战斗 HUD 画布（1080×2160）与教学提示画布（1080×1920）**缩放不同** → 同一个“设计像素”在两处实际屏幕尺寸不同 → 正式关里大得离谱。
   → 改成**相对敌人**：光圈直径 = 敌人屏幕高度 × 0.3（教学镜头下 ≈ 420px，正式关里随敌人变小而变小）。

#### 改动

| 项 | 改法 |
|---|---|
| 错位 | `TutorialGestureStyle` 的 `TipUV/PalmUV` 换成**实测值**；手的位置补「掌心−指尖」偏移（用实际尺寸算）→ 指尖落在光圈中心 |
| 圈太大 | 尺寸改成**相对敌人屏幕高度**（`ringHeightRatio = 0.3`），不再用固定设计像素 |
| 无敌人处的圈 | `CollectTargets` 排除死亡/禁用/血量≤0；`Refresh` 额外校验目标存活 → **敌人一死就撤圈**；池化回收导致 `unit.enemy` 失效也会隐藏 |

#### 自测结果

- 截图（`Library/Locus/Screenshots/locus_game_20261102_030947_530.png`）：N1 第一波，**手的光指尖落在光圈中心**，圈套在敌人胸口。
- 数值：指尖→圈心偏差 **14.2px**（按压弧线）；圈直径 **419px**（教学镜头，敌人约 1400px 高）。
- 敌人清空后 0.6s 内：提示单元 0 / 可见光圈 0 → **无残留光圈** ✓。

### 0.15 第十一轮：教学引导残留 + 霸体防御拍时机（2026-11-02，用户第三轮验收反馈）

> **本轮交接文档**：`plan/tutorial-hint-handoff-20261102.md`（新对话接手先读它）。

> 用户 2026-11-02 第三轮反馈（3 条）：
> ① 「有些引导完成后不消失」——典型：三选一选了【海浪】后光圈还在；开场点按教学同样收不掉；
> ② 「需要让敌人先完成抬手动作再暂停引导玩家格挡霸体攻击，**不能一标红就开教学**」；
> ③ 「正式关里引导的光圈和手指还是错位」。
>
> 本轮结论：① **已修并留证**；② **已修并留证**；③ 的**残留来源** = ① 的同一个根因（已修），
> 但**正式关里常驻引导（AttackTapIndicator）的对位仍需在正式关内复测**（见文末待办，未做）。

#### ① 根因：提示层 `Hide()` 只藏图层、**不停协程**

`TutorialGestureHint` 的循环示范（连点 `PlayTapLoop`、按住防御 `PlayPressHoldLoop`）是**无限循环**，
每一轮都会 `ring.gameObject.SetActive(true)`（`TapRoutine` 内）。
而 `Hide()` 只调 `HideLayers()`（`SetActive(false)`）→ **下一帧协程又把光圈 SetActive 回来**。

→ 表现：拍结束了、卡片选完了，光圈和手指还赖在屏幕上；教学关的残留引导也会**跟着流进正式关**（③ 的一个来源）。

**修复**：`Hide()` 改成 `StopRoutine() + HideLayers()`；`HideLayers()` 里顺手把手恢复静止姿态
（否则下一次示范会从「按下 / 转过」的姿态起手）。协程自己播完的路径走 `Finish()`（仍只 `HideLayers()`），不受影响。

**验证**（Play Mode 全流程自动驱动，每拍结束后 0.6s 采样）：

| 采样点 | 光圈 | 手指 | 结论 |
|---|---|---|---|
| 第一拍（点按教学）结束 +0.6s | False | False | ✅ 收干净 |
| 三选一弹窗（引导指着推荐卡） | True | True | ✅ 符合预期 |
| 选【海浪】+0.5s | False | False | ✅ **用户报的「选完还剩光圈」已修** |
| 技能打断拍结束 +0.6s | False | False | ✅ |
| 全流程结束 | False | False | ✅ |

#### ② 根因：霸体防御拍锁在「一标红」（起手），不是「抬手做完」

原判据 `target.isAttackAnimating && target.isCFrame` 一成立就冻结 → 那是**霸体刚起手**（敌人刚变红），
用户看到的教学瞬间是「敌人刚抬手就被冻住」，语义不明。

**实测**（Play Mode 反查 1012 的 C 技）：攻击 tween 全长 **3.13s**；`isCFrame`（霸体帧）在**剩 1.15s** 时解除（= 出伤）；
攻击动画 `Enemy_1012_Attack.anim` 的关键帧在 0/0.3/0.6/0.9/1.2/1.5/2.0…，出伤帧是 `Enemy_1012_attack6`。
→ 「抬手做完、刀还没落」≈ 攻击进行到 1.6~1.9s。

**改法**：冻结判据换成**攻击剩余时长** `Enemy.TimeToAttackEnd() <= defendPromptToEndSeconds`（默认 1.5s，可配），
仍要求 `isCFrame`（保证这一击还没落下去）。

**顺带修的两个真问题**（都是实测暴露的，不是推测）：

1. **整拍被跳过**：E0 第一波的 1012 只有 150 血，玩家约 1.6s 就能点死它 → 霸体技还没起手人就没了 →
   格挡教学整拍没发生（实测日志：`拍 → DefendButton/0` 之后直接 `拍 → SkillSlot/1`）。
   → 改成**拍一开始就禁出招**（`BeginBeat(BlockAll)`）；台词已经交代「它会出红技、按住防御键」，这段“只能看”合理；
   **压暗留到冻结那一刻**（别在敌人抬手时把画面压黑）。
2. **目标死了就丢拍**：目标死亡时不再 `break`，改为**换下一只**继续等霸体技。

**验证**（Play Mode）：

- 冻结瞬间：敌人显示帧 **`Enemy_1012_attack5`**（举刀姿态，出伤帧 attack6 还没出现）、攻击剩余 **1.48s**、`cFrame=True`、敌人满血 150 ✅
- 玩家按住防御 → 解冻 → 这一击落在防御上：**吸收 1 次**（教程里 1012 伤害 ×0.3 = 1 点）✅

#### ③ 正式关里的对位（本轮已修）

**根因（实测定位）**：常驻引导 `AttackTapIndicator` 的**手是 TapHint 根节点的子物体**，根节点已经摆在敌人屏幕点上，
但代码把手的 `anchoredPosition` 写成了**画布绝对坐标**（`local + 偏移`）→ **位置被算了两遍**：
指尖落到 `2 × 目标点`。敌人离画面中心越远偏得越多 → **教学关敌人居中看不出来，正式关偏 ±176 画布单位 → 偏 ~200px**。

实测对照（正式关「长坂坡·卷轴」N1，1080×2160）：

| | 修复前 | 修复后 |
|---|---|---|
| 正式关 指尖→圈心 | 247px / 151px | **0.0px**（6 秒 240 次采样恒为 0） |
| 教学关 常驻引导 | 0~50px | **0.0px**（100 次采样） |
| 教学关 教学手势层 | 0~50px | **0.0px**（30 次采样） |

**两处修复**：
1. `AttackTapIndicator`：手的 `anchoredPosition` 改成**相对根节点的偏移**（`TutorialGestureStyle.HandPositionOffset(handUnits)`）。
2. **指尖锁点**：按压动画是**绕掌心**转 + 缩放 → 指尖会划出一道 ≈50px 的弧（看着像“手和圈对不上”）。
   → 新增 `TutorialGestureStyle.RotatedTipOffset(size, angle, scale)`，按压每帧把位置**反向补偿**回去，指尖始终压在圈心
   （常驻引导与教学手势层都接上）。

> 注：教学手势层（`TutorialGestureHint`）的 finger/ring/trail 都是 `ScreenSpace` 的**同父**子物体 → 坐标系一致，本来就没有这个 bug。

### 0.13 【未修复】用户 2026-11-02 反馈的两个常态引导问题（先记录，本轮不修）

> 2026-11-02 补充：本节两个问题已在 §0.14 修复；另有一类「引导残留」问题（**教学提示层**的循环协程没停）根因不同 → §0.15。

> 用户原话：「错位问题依然没有解决，且光圈当前非常大」「明明对应位置没有敌人却有光圈提示的问题」
> → **本轮只记录，不修**。下面是我已经做到哪一步、测到哪些数、还剩哪些疑点，供下一轮接手。

#### A. 手与光圈仍然对不齐（且光圈过大）

**现象**：正式关卡里，引导的**手指和光圈位置对不上**；光圈**明显偏大**。

**已经做过的（无效）**：
- 把教学提示与常态引导的坐标/尺寸抽成唯一设置处 `TutorialGestureStyle`（指尖 UV、掌心 UV、基准尺寸、放大倍数、按压角）；
- 两边的 RectTransform **pivot 都改到掌心**（旋转绕掌心，不再绕指尖甩）；
- 位置统一补「掌心−指尖」偏移，意图是**指尖落在光圈中心**。

**实测到的数**（Play Mode，1080 画布）：常态引导 手 616 / 圈 480；**指尖到圈中心偏差 19.5px**（= 按压弧线，理论上可接受）。
→ 但**用户目视仍不对齐** → 说明偏差不止 pivot 数学这一处，**至少还有一个没对上的量**，我还没定位到。

**疑点（待查）**：
1. 圈用的图是 `tutorial_gesture_hold_ring_ui` —— 这张图的**视觉中心**未必等于它的 pivot 中心（可能是个偏心的「按住」光圈）；
2. 手图 `tutorial_gesture_finger_ui` 里**指尖的真实像素位置**未必等于我取的 `TipUV = (0.2637, 0.8066)`（那个值是从教学 prefab 的 pivot 反推的，不是量图得来的）；
3. 两张 UI 图所在的 **Canvas 缩放/参考分辨率不同**（教学提示画布 1080×1920 match 0.5；战斗 HUD 画布 1080×2160）→ 同一个“设计像素”在两个画布里**实际屏幕尺寸不同** → 就算坐标算对，圈在屏幕上也会比教学里更大（实测常态圈直径 480 → 1080 宽屏上占 44%）。

**光圈过大**：常态 = 教学 × 2（手 616 / 圈 480）。这个倍数是我按上一轮“至少放大 1 倍”的要求定的，**实测偏大**，需要按目视重新定。

**下一轮的做法（建议）**：不再靠 pivot 数学推，**直接按图量**：用图片处理算出两张 UI 图的实际内容边界（指尖像素、掌心像素、圈的视觉中心），把常量按测量值填进去；再把“常态引导”的尺寸改成**相对敌人屏幕尺寸**（而不是固定设计像素），避免两个画布缩放不同带来的放大。

#### B. 没有敌人的位置也出现光圈

**现象**：某个位置**明明没有敌人**，却挂着常驻点击提示（手 + 光圈）。

**已知线索**（未验证）：
- `AttackTapIndicator.Refresh()` 每 0.1s 按 `CollectTargets()` 重建列表 → 目标来自 `EnemyManager.GetAllAliveEnemies()` 中 `rowIndex == 0` 且不在向前移动的敌人；
- 每个 unit 持有**上一帧的敌人引用**（`unit.enemy`）→ 敌人死亡（死亡动画期间）/ 被对象池回收后，引用仍指向旧对象，`TryGetScreenPoint` 仍能算出屏幕坐标 → **光圈停在原地**；
- `Refresh()` 里只有在 `targets.Count` 变少时才销毁多余 unit；**目标数量不变但敌人换了**的位置刷新依赖 `unit.enemy = targets[i]`（看起来是对的，但没验证过池化时机）；
- 也可能是**死亡中的敌人仍在 `GetAllAliveEnemies()` 里**（死亡动画期间算“活着”）→ 光圈挂在尸体上。

**下一轮的调试步骤**：
1. Play Mode → 击杀前排敌人 → 立刻观察光圈是否还留在尸体位置；
2. 在 `AttackTapIndicator.Refresh()` 里打印 `targets.Count` 与每个目标的 `DebugTag / currentHealth / state / rowIndex`；
3. 核对 `EnemyManager.GetAllAliveEnemies()` 是否包含 `state == Dead` 或已回收的敌人；
4. 验证：`unit.enemy` 失效（池化回收）时是否应立即隐藏该 unit。

### 0.12 第九轮：手势统一设置 + 三连手指 + 三选1 改用对话（2026-11-02）

| # | 用户指出的问题 | 改法（已落地并实测） |
|---|---|---|
| 1 | 正式关卡里**手指和光圈位置不匹配**；教学与常态引导**各写一套坐标/尺寸** | 新增 **`TutorialGestureStyle`（唯一设置处）**：指尖 UV、掌心 UV、基准尺寸（教学手 308 / 圈 240）、常态放大倍数（×2）、按压角。**两边的 pivot 都设在掌心**（旋转绕掌心），位置统一补「掌心−指尖」偏移 → 理论上手指尖落在光圈中心（实测偏差 19.5px = 按压弧线）。⚠️ **但用户 2026-11-02 目视仍不对齐、且光圈偏大 → 本条不算修好，见 §0.13** |
| 2 | 三连刺教学**还是丢手指** | 根因：连点拍用的是**单次**点按 `PlayTap` → 播完 `Finish()` 把整个提示层收起来 → 只剩我加的常驻光圈。改成 `PlayTapLoop`（循环点按）+ 循环手势不再每轮重播/隐藏。实测：手指=True、光圈=True |
| 3 | 三选1 的教学文本**要用对话**，不要另起一块字幕 | 删掉 `TutorialTeachingBanner`（组件与文件都已移除）；三选1 弹出时用**对话系统**说。配套两处：① `DialogueManager.AllowDialogueWhilePaused` —— 弹窗会把游戏暂停，对话默认被“被别的 UI 挡住”拦下不播，这一拍开口；② 对话层 Canvas 排序 22 → **120**（弹窗是 100，否则对话被弹窗盖住）。实测：弹窗出现时 flag=True / isPlaying=True / 对话在显示=True |

### 0.11 第八轮：五条（2026-11-01 晚第七轮）

| # | 用户指出的问题 | 改法 |
|---|---|---|
| 1 | 1012 的 idle 要**换回旧版** | 从 git `870e3e6^` 取回 6 帧旧 idle（`git diff 870e3e6^ -- Enemy1012/` 已空 = 与旧版一致）。旧 idle = **旧武将**（与 1012 的攻击帧、1011 同一形象）→ 1012 现在从头到尾一只兵 |
| 2 | 三选1 → 教点技能之间**必须锁输入**（否则点防御让技能键点不动、长枪卡在空中） | ① 新增 `GameplayInputGate.AllowSkillActivationWhileBlocked`：`ActiveSkillRunner.TryActivate` 在拦截期间默认拒绝，教学技能拍里打开 → 玩家能真的点出技能；② 三选1 选完即**锁住防御键**并 `PlayerState.SetDefend(false)` 把长枪收回去；③ 技能拍结束恢复（技能可释放 + 防御键解锁） |
| 3 | 引导光圈**震动频率太高** | 呼吸 3.2Hz→**1.6Hz**、幅度 13%→**10%**、自转 42°/s→**16°/s**、点按周期 0.75s→**1.0s** |
| 4 | 三连刺引导**只剩光圈、丢了手指** | 根因：连点拍用的是**单次**点按（`PlayTap`），播完 `Finish()` 会把整个提示层收起来 → 只剩我加的常驻光圈。改成 `PlayTapLoop`（循环点按，手指一直在）+ 循环手势不再每轮重播 |
| 5 | N1 也配 1012 的话**敌人一直都有霸体** | N1 改回 **1011（无 C 技）** → 轻攻击能打断，「多次攻击可以打断敌人的攻击」这句才成立；E0 用 **1012（带 C 技）** 教霸体。因为 1012 的 idle 已回退成旧武将，**1011 与 1012 形象一致**，教学里不会出现两种兵 |

### 0.10 第七轮：手势提示三项调整 + 1012 形象实情（2026-11-01 晚第六轮）

**手势（问题2/3/4）**
| 项 | 改法 |
|---|---|
| 常态引导的圈和手太小 | 尺寸翻倍：手 220→**440**、光圈 240→**480**（配置工具每次应用时对齐，避开序列化旧值） |
| 手势绕指尖转 | 新增 `ApplyPalmRotation`：先绕指尖 pivot 转 θ，再把「指尖→掌心」的偏移按同角度转回去（补偿位移）→ **手绕掌心转、指尖划弧**。**教学提示 `TutorialGestureHint` 同样改了**（原来也是绕指尖） |
| 常态引导圈没动感 | 光圈 = **自转 42°/s + 呼吸 3.2Hz/±13%**；再加一圈**扩散波纹**（周期性外扩 + 淡出） |

**问题1：1012 的形象实情（实测截图对比）**
- `Enemy_1012_idle1..6` = **新贼兵**（麻布衣、皮甲、圆盔、细髭、短剑）
- `Enemy_1012_attack1..16` / walk / hit / dead = **旧武将**（全罩盔 + 红缨、黑络腮、长剑、肩甲）
→ **1012 一动起来就换人**（idle 与攻击帧是两个角色）。旧 idle 在 git 历史里（`870e3e6` 之前）。
→ `Enemy_1011` 全套（idle/walk/attack/hit/dead）都是**旧武将**，所以 1011 与 1012 不是同一种兵。

**待用户定方向（三选一）**
1. **把 1012 的 idle 换回旧版**（git 历史可取回）→ 1012 全套统一成旧武将，但丢掉已验收的新 idle；
2. **走美术流程**：把 1012 的攻击/行走/受击/死亡帧也按新形象重做（出中英文提示词 + 帧规格 + 落位规则，**等你批准再生成**）；
3. 暂时让 1012 的攻击/行走复用新 idle 帧（不换脸但动作缺失）—— 凑合，不推荐。

### 0.9 第六轮：教学关敌人形象统一为 1012（2026-11-01 晚第五轮）

**用户指出**：1012 用了**部分新的美术素材**（`Assets/Sprites/Enemy/Enemy1012/`：6 帧新 idle + 新 attack 帧 + `Enemy_1012` 动画组），
节点一原来用的 1011 还是**旧步兵形象**（`Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle1.png` + `Enemy_1011` 动画组）→ 教学里出现两种兵。

**改法**：`Tutorial_N1_Battle` 的两波敌人 1011 → **1012**（配置工具 `TutorialSetupTool` 每次应用时统一），
节点一 / 节点二**全部用 1012**。实测：运行时敌人贴图 = `Enemy_1012_walk3` ✓。

**文档依据**：`plan/enemy1012-bandit-idle-animation-handoff.md`（1012 新形象 · 6 帧 1.8s 周期 idle，已验收）、
`plan/enemy1012-bandit-face-v2-handoff.md`（敌方面相 v2：山贼感）。

### 0.8 第五轮：七条返工（2026-11-01 晚第四轮验收不通过）

| # | 用户指出的问题 | 改法（已落地并实测） |
|---|---|---|
| 1 | 常驻的**光圈和手指点凑成一起**，没按教学那样排布 | 手的锚点改成**教学提示同一个**（pivot 0.2637/0.8066、size 220）→ 指尖落在目标点上、手身往下右下展开；光圈居中（240）。不再两个图形重叠 |
| 2 | 教学时还显示常驻光圈 | `AttackTapIndicator` 读取 `TutorialStageDirector.BeatActive` → **教学拍进行中不显示**（教学自己就有手势提示） |
| 3 | 真的不能冻结敌人的动作吗？暂停不是能实现吗 | 能，而且已实现：内容1（防御）**敌人抡起霸体技的瞬间就冻结整个游戏**（timeScale=0）→ 它的蓄势动作被定住、玩家不会被砍、这一击一定落下来被防御吸收（实测玩家血量全程 100） |
| 4 | 节点移动时依然有黑屏；遮罩应该**在玩家点节点移动按钮时**出现 | 遮罩改为**由「按下路牌按钮」触发**（`TutorialBlackoutPointerTrigger` 挂在**可点的那个 Button** 的物体上 —— 之前挂在按钮对象上收不到按下事件），淡入 0.12s；**一进入移动（FakeMoving）就收掉** → 移动过程不再有遮罩 |
| 5 | 三选1 面板的点击教学有**奇怪的黑色遮罩**（乱加的字幕） | 教学文字横幅改成**正经的提示条**：深色条 + 底部金线 + 金色强调句，放在弹窗**上方**（topOffset 250 / 宽 900 / 高 150）；并修好两个真问题：① 横幅画布原本挂在导演对象下 → 继承了父级缩放（rect 变成 1018×2036，位置全偏）→ 改挂场景根；② 字体用 LiberationSans → **中文渲染成方块** → 改用场景同款中文字体「方正粗黑宋简体 SDF」 |
| 6 | 由于问题3，**完全无法在合适的时候暂停**教玩家用技能打断敌人 | 内容2（技能打断）现在**也冻结**：敌人抡起霸体技的瞬间冻结 → 提示手落在技能槽 → 玩家点技能（技能进入冷却=释放成功）→ 解冻 → 技能效果结算 → 敌人的霸体攻击被打断 → 收拍 |
| 7 | 教学关的敌人有时 1011 有时 1012？ | 是配置如此：**节点一 = 1011**（普通兵，教点击/三连）；**节点二 = 1012**（刀兵_1012，**唯一带 C 技霸体前摇的杂兵**，教霸体与打断）。要统一成一种跟我说用哪个 |

### 0.7 第四轮：七条返工（2026-11-01 晚第三轮验收不通过）

| # | 用户指出的问题 | 改法（已落地并实测） |
|---|---|---|
| 1 | **没有文字引导玩家选海浪** | 新增 `TutorialTeachingBanner`（自己建 sortingOrder=300 的 Overlay Canvas，**画在三选一弹窗之上**）；弹窗时显示两行：①「击杀敌人获得经验，经验满了就会升级，升级会弹出三选1奖励。」②「听我的，选【海浪】——它能把霸体敌人推回去，打断他的招。」实测：弹窗中横幅可见 |
| 2 | 升级弹三选1 时**先**告诉玩家经验→升级→三选1 | 同上（第一行就是这句） |
| 3 | 节点移动时**依然有黑幕** | 遮罩改为**只盖住「点完按钮 → 开始移动」这一小段**（`ShouldBeBlack` 只判 `RouteChoiceTransition`）；进入 `FakeMoving` 立刻收掉 → 移动过程看得见 |
| 4 | 常驻光圈提示**跟教学的完全不一样** | `AttackTapIndicator` 改成**手 + 光圈**（素材直接从教学提示 prefab 接：手 `tutorial_gesture_finger_ui`、光圈 `tutorial_gesture_hold_ring_ui`），手做「点一下」的按压循环、光圈呼吸 |
| 5 | 敌人**还在补齐**光圈就出现了 | 提示只给 **rowIndex == 0**（真正的前排）且 **不在向前移动**（`Enemy.IsMovingForward`）；另外暂停中（三选一/阻塞台词/教学冻结）不显示，非阻塞教学台词照常显示 |
| 6 | 提示玩家格挡时**要锁住敌人的攻击动作**，玩家被砍 | 根因：`TimeToStrike()` 按 `approachDuration=2s` 算，但 1012 的出伤在攻击 tween 结束/命中帧结算，tween 实测在起手 0.4s 后就没了 → 「等蓄势完成」永远等不到 → 冻结没触发 → 玩家被砍且不能还手。改为**敌人抡起霸体技的瞬间就冻结整个游戏**（锁住它的蓄势动作）+ 禁止出招 → 这一击一定落下来被防御吸收（实测玩家血量全程 100） |
| 7 | **还没教格挡就给格挡按键** | `RefreshDefenseButtonAvailability` 不再看存档的 `tutorialCompleted`：只要在教学路线上、还没教到防御，按钮一律锁住（`SetAvailable(false)` → 隐藏 + 不可点） |

### 0.6 第三轮：教学三选1 **只能选海浪**（2026-11-01 晚）

用户：「玩家怎么会选到它呢？你教程让他只能选海浪啊」

之前的做法只是「推荐」海浪（提示手指着），玩家其实还能点另外两张 → 选了打不断霸体的技能，内容2 的教学就教不下去。
现在改成**强制**：

| 项 | 改法 |
|---|---|
| 其它候选卡 | `UpgradeCard.SetSelectable(false)` → 按钮 `interactable = false` + 整张卡变暗（底图/图标/名称/描述 alpha 0.4） |
| 接口 | `UpgradeChoicePopup.SetChoiceSelectable(def, selectable)`；`UpgradeCard.Setup()` 每次重置回可点（非教学场景不受影响） |
| 开关 | `TutorialStageDirector.forceRecommendedChoiceOnly`（默认 true）——只对教学这一次三选1生效 |
| 实测 | 截图：三张卡里只有「海浪」是亮的、上面压着提示手；「旋风 / 火龙舌」变灰且点不动 |

### 0.5 七条返工（2026-11-01 晚第二轮，用户再次验收不通过）

| # | 用户指出的问题 | 改法（已落地并实测） |
|---|---|---|
| 1 | 三选一只配了海浪一个；**选项卡出现时没有引导选哪个** | 三选1 配成 **3 张卡**：海浪（推荐）/ 旋风 / 火龙舌；卡片弹出时**提示手落在推荐卡上**（`UpgradeChoicePopup.TryGetChoiceScreenPoint` + `TutorialGestureHint.PlayTapLoop`），台词点名「选【海浪】——它能把霸体敌人推回去，打断他的招。」 |
| 2 | 三连 stab 的教学要**保持有光圈提示** | `TutorialGestureHint.keepRingVisible`：TapCombo 拍里每次点完后光圈留在场上（不淡出），玩家一直看得见「点这里」 |
| 3 | 防御教学要**敌人先完成蓄势动作**才出提示 | 冻结点从「敌人起 C 技」改到**蓄势完成**（`TimeToStrike() <= 0.05`，与招架时停拍同一口径）；起手时只禁止出招（不提示），蓄势完成才冻结 + 提示按住防御键 |
| 4 | **常驻光圈提示**没做 | 新增 `AttackTapIndicator`：敌人出现且玩家可以攻击时，**只有最前排**（rowIndex == 0）的敌人身上出现呼吸光圈；对话/三选一/结算/招式冷却中不显示；纯视觉不拦点击 |
| 5 | 黑幕出现在**移动时**，应该出现在**点按键时** | 新增 `TutorialBlackoutPointerTrigger`（IPointerDownHandler）：路牌按钮被按下的那一帧就开始遮罩；提示层 Canvas 排序提到 200（否则会被三选一弹窗盖住） |
| 6 | 技能使用的引导**卡住不让我点** | 提示层的手/光圈/轨迹/遮罩全部 `raycastTarget = false` → 提示只做视觉，不再吃掉点击 |
| 7 | 结束面板的胜利按钮和返回主菜单按钮**要上移**（返回键被防御键压住点不到） | 胜利面板「返回」anchor y 0.25 → 0.40（画布 480 → 768）；暂停面板「继续游戏 / 返回」局部 -245/-350 → -140/-245。截图确认已避开底排防御键（防御键占画布 y 490..650） |

**实测截图依据**（`Library/Locus/Screenshots/`）：三选一弹窗上提示手落在「海浪」卡上、另外两张变灰不可点；弹窗上方有教学文字横幅两行；内容2 台词「它抡起霸体招了！用【海浪】把它推回去！」+ 提示手落在技能槽；前排敌人身上有常驻的「手 + 光圈」；胜利面板「返回」已高于防御键。

> ⚠️ **存档被自动化测试污染**：Play Mode 自动驱动每跑完一遍教学关，就会写一次存档（教学完成标记 + 250 铜钱）。
> 需要的话用 `HubDebugSaveButton`（落地页调试存档）重置，或我把存档还原。

### 0.4 六条返工（2026-11-01 晚，用户验收不通过后逐条改）

| # | 用户指出的问题 | 改法（已落地） |
|---|---|---|
| 1 | **敌人配置根本没有按新的做** | 节点二的「敌人配置」改成**一张明确的表**：`WaveConfig` 新增 **经验倍率** 字段；节点二两波都配成 1012（唯一带霸体前摇的杂兵），HP ×1.5 / 伤害 ×0.3 / 经验 ×3 |
| 2 | **测试用的冻结技能没移除，玩家开局就自带** | 场景里 `ActiveSkillInventory._grantDefaultSkills = false` 且 `_defaultSkills` 清空；`ActiveSkillPoolConfig` 三个池里的**冰雾也移除**；教学三选1 不再给冰雾（资产文件保留，只是不再进任何给玩家的入口） |
| 3 | **节点移动为什么会有黑屏效果** | 黑幕改成**半透明压暗 0.55**（画面在转场时仍然看得见），不再是全屏纯黑；实测 FakeMoving 时 alpha 升到 0.55、进战斗淡出到 0 |
| 4 | **没打败 E0 第一波就给技能；技能应该是击杀时经验给的；而且没有对应的教程** | ① 删掉自定义的「拍完就弹三选1」触发（`TriggerForcedChoices` 已移除）；② 三选1 改回**经验升级**触发：节点一经验倍率 0（不升级），节点二第一波经验 ×3（80×3=240 ≥ 升 1 级的 200）→ **那一杀正好升级 → 弹技能三选1**；③ 三选1 弹出时补一句教学台词（“选一个技能记住——它能打断霸体敌人的招”） |
| 5 | **让玩家选海浪，引导和对应技能呢** | 三选1 选完后**记下选中的技能**（`OnChoiceSelected`）；内容2 的拍**指着那个技能的槽位**，并按技能给对应台词（旋风/海浪/通用三份） |
| 6 | **骑兵敌人上文没说要配置** | 节点二的 109 骑兵波**删掉** → 节点二 = wave0（内容1）+ wave1（内容2），两波都是霸体敌人 |

**返工后的最终流程（Play Mode 自动驱动实测，Console 0 error）**
```
Tap/0（节点一 wave0：点击触发攻击）
→ TapCombo/1（节点一 wave1：连点三下 + 补一句「多次攻击可以打断敌人的攻击」）
→ 路线选择 → FakeMoving（半透明黑幕 alpha 0→0.55）
→ DefendButton/0（节点二 wave0：敌人起霸体技 → 冻结 → 教按住防御键 → 解冻 → 吸收伤害）
→ 玩家打死这一只 → 经验升级 → 三选1（旋风 / 海浪）→ 教学台词
→ SkillSlot/1（节点二 wave1：指着玩家选中的技能槽 → 释放 → 霸体攻击被打断=True → 收拍）
→ WaitingReward → Completed（结算 250 铜钱）
```

**遗留（需要用户定）**
- 三选1 只有 **2 张卡**（旋风 / 海浪）：工程里能打断霸体的主动技能目前就这两个。第三张卡要么用户指定，要么给某个技能（如「冲击波」）加打断能力。

### 0.3 第 1 批的实施结果（2026-11-01，已落地）

**新增代码**
- `Assets/Scripts/Tutorial/TutorialRouteBlackout.cs` —— 节点移动黑幕（监听路线阶段，淡入淡出用非缩放时间）
- `Assets/Scripts/Editor/TutorialSetupTool.cs` —— 教学关配置工具（幂等，菜单：Tools ▸ 三国杀戮 ▸ 教学关 ▸ 应用教学关新配置）
- `Assets/Scripts/Editor/TutorialFlowDriver.cs` —— Play Mode 自动驱动（验证用）

**改动的代码**
| 文件 | 改动 |
|---|---|
| `TutorialStageDirector.cs` | 新增拍型 `TapCombo` / `SkillSlot`；新字段 `requiredHits` / `freezeOnCAttack` / `waitForCAttack` / `skillUpgradeId` / `afterDialogue`；`tutorialChoicePool`；三选1 的触发（`GiveTutorialChoice`） |
| `TutorialStageDirector.Hints.cs` | 按新拍型分流；`_currentStep` 记录当前拍 |
| `TutorialStageDirector.Beats.cs` | 新增 `DefendAtCAttackRoutine`（霸体时停防御）/ `SkillSlotInterruptRoutine`（技能打断）；冻结期间用**非缩放时间**推进持枪姿态 |
| `TutorialStageDirector.Dialogue.cs` | 连点计数（`requiredHits`）；拍完成后补播 `afterDialogue` |
| `TutorialStageDirector.Rules.cs` | 教学全程 `suppressChoices = true`（随机三选一全部禁掉） |
| `TutorialStageDirector.Targets.cs` | 拍型 → 闸门映射（`SkillSlot` → `BlockAll`） |
| `InputManager.cs` | 新增闸门 `TutorialGestureTarget.BlockAll`（这一拍里不许出招，UI 点击不受影响） |
| `Enemy.cs` / `Enemy.StateSwitch.cs` / `Enemy.Init.cs` / `Enemy.Pool.cs` | 新增 `attackInterrupted` 标记（只有**真的被打断**才置位，区分「C 技自然打完」） |
| `DisplacementMover.Push.cs` | **海浪补打断**：允许打断的击退对 C 技敌人调 `CancelAttack()`（此前击退只把人推后一排，招照打） |
| `UpgradeChoiceManager.cs` | 新增 `TriggerForcedChoices`（脚本显式弹一次指定的三选1，不依赖升级事件；`suppressChoices` 时也能弹） |
| `FrontItemBar.cs` | 新增 `TryGetActiveSkillSlotScreenPoint`（教学要把提示手落在技能槽上） |

**资产/场景**
- `Tutorial_E0_Battle.asset`：节点二改成 **wave0 = 1012（hp×1.5 / dmg×0.3）→ wave1 = 1012（同上）→ wave2 = 109（原有骑兵波保留）**
- 新台词 3 份：`tutorial_t0_combo`（连点三下）/ `tutorial_t0_interrupt`（多次攻击可以打断，**非阻塞**）/ `tutorial_t0_skill`（用技能打断）
- `Battle.scene`：手势拍 6 条（含 `Tutorial Blackout` 物体）、台词数组、三选1 候选 `[旋风, 海浪, 冰雾]`

**Play Mode 实测（自动驱动跑通全流程，Console 0 error）**
```
Tap/0  →  TapCombo/1（三连） → 路线选择 → FakeMoving（黑幕 alpha 0→1.00）→ EnteringNode
→ DefendButton/0（内容1）：敌人起 C 技 → 冻结 → 玩家按住防御键 → 解冻 → 吸收伤害 1 → 收拍
→ 三选一弹窗（3 候选）→ 选择
→ SkillSlot/1（内容2）：释放技能 → 敌人的霸体攻击被压住 → 收拍
→ DefendButton/2（109 骑兵）→ WaitingReward → Completed（结算 250 铜钱）
```

**实施期发现并修掉的三个真 bug**
1. 霸体拍的循环用 `_beatActive` 当条件 → 拍还没开就秒退（修：用本地 `beatStarted`）。
2. 冻结时 `PlayerState` 用 `Time.deltaTime` 推进持枪姿态 → **防御位永远进不去**（修：冻结期间用 `unscaledDeltaTime` 额外驱动）。
3. 三选一在「波次清空」弹 → 下一波已经开跑，三选一反而排在内容2 之后（修：改成「内容1 的拍收完就弹」）。

> 仍按建议默认执行（不再单独问）：D5 霸体暂停 = **时停冻结**（冻结在 C 技起手瞬间）；D6 黑幕 = 选择路线时淡入黑 0.3s / 进战斗时淡出 0.2s。

### 0.1 三条实测事实（D2/D3 的前提，2026-11-01 运行时反查 + 源码链）

1. **冰雾本来就是默认技能**：`Battle.scene` 的 `ActiveSkillInventory` 配置为 **`_ruleVersion = V2_ActiveSkill`**、`_grantDefaultSkills = true`、`_defaultSkills = [ActiveSkill_IceMist]`
   → 玩家**每场战斗开局就带冰雾**。所以节点一去掉「固定冰雾三选一」**不会让玩家少任何东西**，反而更干净（少一次无意义的弹窗）。
   → V2 规则下主动技能走**技能槽**（`FrontItemBar`，4 个槽位，`itemSlotCount=4`），点击槽位 = `ActiveSkillInventory.TryActivate(entryId)`。
2. **海浪（ActiveSkill_Wave）的击退「不能」打断杂兵的攻击**（用户 11-01 提出的疑问，实测结论与其预期相反）：
   - `ApplyPushWave(..., canInterruptCFrame: true)` 里的这个标志**只决定「C 技敌人要不要被纳入击退集合」**（`DisplacementMover.Push.cs:389`），**不含任何打断逻辑**；
   - 击退执行 `SetRowIndex` → `UpdateWorldPosition()` → 敌人**瞬移**到后排，攻击 tween 不受影响（`Enemy.Visuals.cs:289-298`）；
   - `RecheckAttackRange()` 在 `isAttackAnimating` 时**直接 return**（`Enemy.Visuals.cs:325`）→ 不取消攻击；
   - 全工程 `CancelAttack()` 调用点只有 `Enemy.Combat.cs:107/112/113/291/439`、`Enemy.FormSwitch.cs:43/244`、`EnemyFreeze.cs:196/380` → **击退路径不在其中**。
   → 对比 **旋风（ActiveSkill_Cyclone）**：`CycloneZone.cs:167` 直接 `enemy.Launch()` → `Enemy.StateSwitch.cs:290-306` 杀掉攻击 tween、`isAttackAnimating=false`、`isCFrame=false`、状态转 `Launched` → **确实打断**。
   → **教学要让「无论玩家选哪个技能都能打断」，必须给海浪补打断**（与它自己的技能描述「打断攻击」一致的口径修正）；否则三选1 里只有旋风能完成教学。
3. **铜钱链已核实，买得起 C2**：教学路线 `Tutorial_Route.clearCoinReward = 250` → 整关结算 `StageRewardGate.CompleteFakeRouteStage(250)` → `PlayerState.AddCoins(250)` → `StageController.SettleCoins()` → `SaveManager.SetCoins(总铜钱)` **写进存档**（`StageController.cs:378-391`）。
   → 教学打完回到主菜单时存档里有 **+250 铜钱**，C2 售价 100 → **买得起**（教学期间没有其它消费点）。

**三选1 候选池**：`[旋风, 海浪, 冰雾(升级)]`（旋风必含；海浪需先补打断，见 §0.1-2）。

---

## 一、需求拆解（用户原话 → 落点）

| # | 用户原话 | 落点 | 现状 |
|---|---|---|---|
| R1 | 节点一 内容1：教玩家点击触发攻击 | N1 第 1 波 | ✅ **已有**（gestureStep[0] = Tap） |
| R2 | 节点一 内容2：多次点击触发三连 stab（同一节点两波敌人分别教学，与原有几乎一致） | N1 第 2 波 | 🟡 **需改**：当前第 2 波教的是**横划斩击**（Swipe），不是连点三下 |
| R3 | 多次攻击可以打断敌人的攻击（**仅文字**，玩家触发三连 stab 后播放，**不暂停**） | N1 第 2 波结束后 | ❌ 新增：一条非阻塞台词 |
| R4 | 节点移动（当前缺黑幕效果） | N1 → E0 转场 | ❌ 新增：节点切换无任何遮罩 |
| R5 | 节点二 内容1：配置霸体攻击敌人，在敌人霸体攻击时**暂停**教玩家按住防御键格挡（**要提高敌人血量**，否则玩家提前击杀）→ 然后获得 3 选 1 奖励 | E0 第 1 波 | 🟡 **半有**：E0 第 1 波已是 1012（**唯一带 C 技的杂兵**），但① 血量是**调低**的（×0.4）② 防御拍是「敌人站定就开始教」，不是「霸体攻击时暂停」③ E0 现在**不给**三选一（`suppressChoices=true`） |
| R6 | 节点二 内容2：教玩家用**技能**打断霸体敌人攻击 | E0 第 2 波 | ❌ 新增。**D1：教三选1拿到的技能** → 三选1 必须含能打断霸体的技能（旋风必含）；「海浪在杂兵上不真打断」已实测（§0.1） |
| R7 | 击杀节点二所有波次敌人 → 结束初始教学 → 回主菜单 | 路线结算 | 🟡 路线本身已是「N1 → 选择 → E0 → 结束」；但**「初始教学完成」现在在战斗胜利时就置位**，与 R8~R10 冲突（见 D5） |
| R8 | 主菜单教玩家进入天赋界面点出**蓄力 C2 stab** | 落地页 + 天赋 overlay | 🟡 天赋节点 `talent_unlock_c2`「指向突刺」**hidden=1 且 moveConfig 为空** → 现在 **C2 是白送的**，买天赋没有意义（见 D2） |
| R9 | 点击引导玩家开始关卡 | 落地页「开始关卡」按钮 | ✅ 按钮存在（`BattleStartStageButton`），但无引导 |
| R10 | 新关卡引导如何触发这个招式 | 正式关内 | ❌ 新增拍型（C2 = 点一下后按住蓄满松手） |
| R11 | **新改动**：敌人出现且玩家可以攻击时，常驻显示点击特效提示玩家点击触发攻击；**仅最前排**的敌人附带这个特效 | 全局战斗 | ❌ 全新功能（与教学无关，是常驻战斗提示） |

---

## 二、现状实测（可直接当依据用）

### 2.1 教学关的骨架

- 教学**不是独立场景**，是 `Assets/Scenes/Battle.scene` 常驻的 `TutorialStageDirector` 组件，走**正式卷轴路线**（`FakeRouteRuntime`）。
- 路线配置 `Assets/RouteData/Tutorial/Tutorial_Route.asset`：`stageId 9100`，`stageName 教学关·初见`，`startNode = Tutorial_Node_N1`，节点 **N1 →（选择）→ E0**，`clearCoinReward 250`，`prerequisiteStageIds` 空。
- 节点 `Tutorial_Node_N1`：`battleEntries = [Tutorial_N1_Battle]`，`completionPolicy "Battle And Reward"`，`outgoingChoices = [Tutorial_Choice_N1_E0]`。
- 战斗配置：
  - `Tutorial_N1_Battle`（stageId 9001）**2 波**：wave0 = 1×**1011**；wave1 = 3×**1011**；两波 `damageMultiplier 0.3`。
  - `Tutorial_E0_Battle`（stageId 9002）**2 波**：wave0 = 1×**1012**（`healthMultiplier 0.4`）；wave1 = 1×**109**（骑兵）；wave1 `damageMultiplier 1.0`。
- **实测的场景配置**（`Tutorial Director` 组件，`gestureSteps` 5 条）：

| # | battle | wave | kind | 关键开关 |
|---|---|---|---|---|
| 0 | Tutorial_N1_Battle | 0 | **Tap** | pause=true |
| 1 | Tutorial_N1_Battle | 1 | **Swipe** | lineAtWaveStart=true（登场台词） |
| 2 | ANY | -2 | Tap | 路线出口箭头（不暂停） |
| 3 | Tutorial_E0_Battle | 0 | **DefendButton** | — |
| 4 | Tutorial_E0_Battle | 1 | **DefendButton** | lineAtWaveStart=true, despawnTargetOnSuccess=true（109 冲锋后离场） |

- 其它接线：`tutorialIceSkill = ActiveSkill_IceMist`（冰雾）、`openingDialogue = tutorial_t0_opening`、`choiceDialogue = tutorial_t0_route_choice`、`n1Dialogues/e0Dialogues` 各 2 条。

### 2.2 教学拍的机制（复用点）

- `BeginBeat(target, teachAttack)`：冻住敌人（`tutorialHold`）+ **压暗路线场景**（`TutorialSceneDim`，只压背景/地形/植被，角色与敌人不压）+ **只放行目标手势**（`InputManager.tutorialGestureTarget`）+ 刷招式白名单（`TutorialAttackGate` = 已教 ∪ 当前这一拍）。
- 成功判据在 `OnTutorialAttackExecuted`：**一次出招就收拍**（防御拍例外，判据是 `PlayerState.WeaponStance.IsDefending`）。
- 招架拍是**时停**（`freezeOnGesture`）：`timeScale=0`，冻结在 `TimeToStrike()<=0.03`（命中帧那一刻），玩家在冻结中划动，`InputManager.allowInputWhileTimeFrozen` 让冻结中仍吃输入。
- 防御拍（`DefendHoldRoutine`）：按住敌人 → 教 → 玩家进入防御姿态 → 放开敌人让它出手 → `PlayerGuard.OnDamageAbsorbed` 判定成功 → 收拍。
- **招式白名单**：`AttackTypeFor(kind)` 目前只映射 Tap→Stab / Swipe→Slash / ChargedSwipe→Launch / Parry→Parry（`DefendButton` 不教招式）。生命周期 = 一次教学流程（`n1Battle` wave0 重置）。

### 2.3 霸体与打断（**回答用户「有啥技能能打断霸体」**）

判定代码：`Assets/Scripts/Enemy/Enemy.Combat.cs:96-115`

```
轻攻击：前摇可打断；**霸体期无效**
重攻击：前摇可打断，**霸体期也能**（同时按 poiseDamage 削韧）
招架  ：只在**出伤窗口**格开；前摇打不断
收招  ：一律不可打断
```
- 非 Boss 敌人的「霸体」= **C 技前摇**（`isCFrame`）。`isSuperArmor` 只有 BOSS 阶段配置有赋值路径（`Enemy.cs:173` 注释：非 Boss 无赋值路径）。
- 玩家侧能打断霸体的**全部手段**（实测）：

| 手段 | 能否打断霸体 | 依据 |
|---|---|---|
| **挑飞 Launch** | ✅ 且削韧 | `AttackSystem.Types.cs:427` `canInterruptCFrame: true`；`Zhangfei_Launch.asset` isHeavy=1 / poiseDamage=50 |
| **C2 指向突刺 `Zhangfei_StabR2`** | ✅ 且削韧 | isHeavy=1 / poiseDamage=30 / rangeRows=2 |
| **C4 枪突收尾 `Zhangfei_LaunchFinisher`** | ✅ | isHeavy=1 |
| 穿刺 Pierce / 横扫 Sweep（资产 isHeavy=1） | ✅ | 但**没接进 `HeroConfig.skillConfigs`**（只有 Stab/Slash/Launch/Parry）→ 玩家当前用不了 |
| **聚拢波 / 冲击波**（主动技能 Wave 系） | ✅ | `WaveEffectPlayer.cs:182` `ApplyPushWave(..., canInterruptCFrame: true)` |
| **旋风 Cyclone**（主动技能） | ✅（换了个方式：强制击飞） | `CycloneZone.cs:167` 直接 `enemy.Launch()`，状态被改 → 攻击取消 |
| 冰雾 IceMist | ❌ **冻结是「暂停」不是「取消」** | `EnemyFreeze.Freeze()` 只 `PushSuspend(Frozen)`；只有**破冰解冻**才 `CancelAttack()` |
| 火系 / 箭雨 / 疾病 / 冲击波伤害 | ❌ | 普攻级伤害，霸体期打不断 |
| 招架 Parry | ❌（只能格开出伤窗口） | `Enemy.Combat.cs:101` |

> **结论**：目前**没有**「三选一里随便给一个就能打断霸体」的常规主动技能。
> 三选一池（`ActiveSkillPoolConfig`）里能打断的只有 **旋风（传奇池）** 和 **聚拢波（Wave 系）**；
> common/rare 池的冰雾、火线、疾病、箭雨、冲击波**都打断不了**。
> **真正的「打断霸体」主力是重攻击招式**：挑飞（英雄自带）、C2、C4。

### 2.4 C2（蓄力指向突刺）现状 —— **一个必须先解决的问题**

- 招式链（`MoveTable_Zhangfei` + 移动资产）：`Tap → Jab1`；`Jab1 --Tap--> Jab2`；`Jab1 --Hold(minCharge=1)--> **StabR2（C2）**`；`Jab2 --Tap--> Jab3`；`Jab3 --SwipeVertical(minCharge=1)--> C4`。
  → **C2 手势 = 点一下后按住蓄满松手**（与用户说的「蓄力 C2 stab」一致）。
- `Zhangfei_StabR2`：`attackType Stab` / isHeavy=**1** / poiseDamage=30 / columnMode Single Column / rangeRows=2 / damage 35。
- 天赋节点 `talent_unlock_c2`（`TalentTree_Main.asset`）：displayName「指向突刺」，coinCost **100**，**`hidden: 1`**，**`moveConfig` 为空**，图标 `TT_Icon_UnlockC2.png` 已存在。
- 解锁判定 `PlayerMoveStateMachine.IsMoveLocked(move)`：**只有当某个天赋节点的 `moveConfig == 该招式` 时才会锁**。
  → 因为 `talent_unlock_c2.moveConfig` 是空的，**C2 现在完全没被锁，玩家白拿**（实测运行时 `Jab1 --Hold--> Zhangfei_StabR2`）。
  → 对照：`talent_unlock_c4.moveConfig = Zhangfei_LaunchFinisher` → C4 **是**锁着的。
- 铜钱：教学路线 `clearCoinReward = 250` ≥ C2 的 100 → 钱够（待实测确认到账路径 `StageRunFlow.SettleCoins`）。

### 2.5 主菜单 / 天赋 / 关卡流

- 「主菜单」= **Battle.scene 的落地页**（没有独立 MainMenu 场景）。
- 天赋入口：`HubTalentEntry` → `SceneFlowManager.SwitchOverlay("TalentTree")` → `Assets/Scenes/TalentTree.scene` 叠层；面板 `TalentTreePanel` 运行时按 `TalentTreeConfig` 生成节点；解锁 `TalentTreeState.TryUnlock`（`OnChanged` 事件）。
- 开始关卡：`BattleStartStageButton` → `IRouteFlow.RunPrepared()`；按钮显隐由 `HubStateSource`（唯一状态源）决定：`IsPrepared && !IsOverlayOpen`。
- 回主菜单 = **重载 Battle.scene**（`StageRunFlow.GoToMainMenu` → `LoadScene(battleSceneName)`）→ **跨场景的教程引导必须靠存档里的教学进度驱动**，不能靠内存状态。

### 2.6 「常驻点击特效」的可复用件

- 现有提示件：`Assets/Prefabs/UI/TutorialGestureHint.prefab`（Canvas 1080×1920 match 0.5，1 单位≈1 像素）：手指 308 / 光圈 336 / 轨迹 67 / `dimOverlayAlpha 0.45`。
- 但它是**单实例 + 教学闸门专用**（`Gate()` 会接管手势输入），不能直接当常驻战斗提示用 → 需要**独立组件/独立 prefab**（多实例或池化）。
- 前排定义：`rowIndex == 0`（教学的目标敌人选择器 `FindTargetEnemy` 就是这么取的）。
- 玩家可攻击范围：`Zhangfei_Stab/Slash/Launch` 的 `rangeRows` 都是 **1** → **基础招式只打得到前排** → 「仅前排带特效」与规则一致。
- 已有但**不能直接复用**的相关件：`EnemyAttackTelegraph`（敌人起手预警：菱形点→十字星→十字星+外环）——那是敌人侧的，方向相反。
- 历史结论见 `plan/task2-tutorial-ui-handoff-20261028.md` §9.4：白手 220/240 px 相对敌人（本体高 ≈500 px）过大，建议**缩到描边光圈级**；提示要「不阻塞、不抢输入」。

---

## 三、落地方案

### A. 节点一（N1）：点按 → 三连 → 打断说明

| 步骤 | 改动 | 落点 |
|---|---|---|
| A1 | 内容1 沿用现有 gestureStep[0]（N1 wave0 = Tap） | 不改 |
| A2 | **新增拍型 `TapCombo`**：`TutorialGestureStep.requiredHits`（默认 1）+ 成功判据改为「累计 N 次 Stab」；N1 wave1 改成 TapCombo(N=3) | `TutorialStageDirector.cs`（枚举 + 字段）、`Dialogue.cs`（`OnTutorialAttackExecuted` 计数）、`Targets.cs`（`ToGestureTarget`） |
| A3 | 「多次攻击可以打断敌人的攻击」= 一条**非阻塞**台词：三连完成后推，**不暂停**（`Blocking=false` + 按字数自动推进，与现有 `tutorialLineSeconds` 口径一致） | 新增 `DialogueEventData` 资产 + `n1Dialogues` 增加一条（wave1 之后的收尾句） |
| A4 | N1 wave1 敌人 3×1011 **保留**（与「同一节点两波敌人分别教学」一致）；wave1 现在教 Swipe → 改 TapCombo | `gestureSteps[1]`、`Tutorial_N1_Battle`（可不动） |

> 风险：Jab1→Jab2→Jab3 的接续窗口由 `moveEdges` 决定（Jab1 的 Tap 边 `overrideWindow=0` → 用默认 Stab 窗口 0.48~0.95）；玩家打空/慢点会断连。教学必须允许**重试**，不能因断连判失败。

### B. 节点移动黑幕

| 步骤 | 改动 | 落点 |
|---|---|---|
| B1 | 新增 `TutorialRouteBlackout`（Battle.scene 常驻，**全屏 UI Image**，unscaled 淡入淡出） | 新脚本 + 新 prefab/节点 |
| B2 | 触发点：路线阶段进入 `RouteChoiceTransition` / `FakeMoving` → 淡入黑（0.3s）；进入 `Battle` → 淡出（0.2s） | 监听 `RouteFlowHub.Current.Phase`（**不改路线系统本体**） |
| B3 | 为什么不能用现有 `TutorialSceneDim`：它的设计明确「不用全屏 UI 遮罩 —— 会把角色也遮住」；节点移动时没有角色在打，**这时用全屏黑幕才成立** | 文档化，避免后人误用 |

### C. 节点二（E0）：霸体 + 防御 + 三选一 + 打断教学

| 步骤 | 改动 | 落点 |
|---|---|---|
| C1 | 敌人用 **1012**（工程里唯一带 C 技的杂兵：`isCAttack:1`，前摇 2 秒 = 霸体窗口，命中帧 `Enemy_1012_attack6`@2.0s，maxHealth 100，maxPoise 50） | `Tutorial_E0_Battle` wave0（已是 1012） |
| C2 | **提高血量**：wave0 `healthMultiplier` 0.4 → **建议 1.5~2.0**（有效 HP 150~200）；`damageMultiplier` 保持 0.3（玩家挨打不疼） | `Tutorial_E0_Battle.asset` |
| C3 | **新增拍型「霸体时停防御」**：敌人 `isAttackAnimating && isCFrame`（C 技起手瞬间）→ **冻结**（timeScale=0）→ 教玩家按住防御键 → 玩家进入防御姿态 → **解冻** → 敌人继续挥下来 → `PlayerGuard.OnDamageAbsorbed` 吸收 → 收拍 | 复用 `FreezeParryRoutine` 的冻结骨架 + `DefendHoldRoutine` 的「进入防御才放行」+ `OnGuardAbsorbed` 成功判据 |
| C4 | 冻结点选在**起手瞬间**（不是命中帧）——否则敌人已经挥下来了，玩家没有反应时间；解冻后还剩约 2 秒才出伤，玩家需保持按住 | 同上（与招架拍不同，理由要写进代码注释） |
| C5 | **三选1（全程唯一一次，在节点二内容1 之后）**：E0 现在 `suppressChoices=true`（无三选一），N1 给固定冰雾。按 D2：**N1 改成 suppress（节点一不给任何技能）**；**E0 wave0 结束时给一次 forcedChoices** | `ApplyTutorialRules`（`TutorialStageDirector.Rules.cs`）：N1 → suppress；E0 → 出一次 forcedChoices |
| C6 | 三选1 候选池 = **`[旋风, 海浪, 冰雾(升级)]`**（旋风必含，它是唯一实测能真正打断杂兵 C 技的技能）；**前提**是给海浪补 `CancelAttack()`（§0.1），否则把海浪换成无关项 | `forcedChoices` |
| C7 | 内容2 新拍型「**点技能槽打断霸体**」：① 定位玩家刚拿到的技能槽（`FrontItemBar` 的槽位 → 屏幕坐标，参照 `TryGetDefendButtonScreenPoint` 的写法）；② 提示手落在槽位上；③ 玩家点击 → `ActiveSkillInventory.TryActivate` → 技能释放；④ **成功判据 = 目标敌人的 C 技被打断**（`isAttackAnimating == false` / `state != Attacking` / 出现破势） | 新增拍型 + 新增「技能槽屏幕坐标」查询 API（`FrontItemBar` 或 `ActiveSkillInventory` 侧） |
| C8 | 技能是 **UI 点击**，不经招式手势通道 → 教学拍打开手势闸门时**不会误触攻击**（`InputManager` 的 `IsPointerOverUIAt` 规则）→ 内容2 的拍**不需要新的手势闸门目标**；但要确认闸门打开时 UI 点击仍能穿透到技能槽按钮 | `TutorialStageDirector` + `FrontItemBar.OnItemIconClicked` |
| C9 | 白名单：内容2 不教招式（不把任何 AttackType 加进白名单）→ 玩家在内容2 期间仍只能出已教招式（点按/三连），**技能是另一条通道** | `Rules.cs`（无需改动，核对即可） |

### C0. 敌人配置怎么算（2026-11-01 计算，结论：两波 1012 都用 `healthMultiplier = 1.5`）

**输入数据（实测）**

| 项 | 值 | 出处 |
|---|---|---|
| 1012 基础血量 | 100 | `Enemy_1012.prefab` `maxHealth` |
| 1012 攻击伤害 | 3 | 同上 `attackDamage` |
| 1012 C 技 | `isCAttack:1`，前摇 **2.0s**（= 霸体窗口），命中帧 `Enemy_1012_attack6`@2.0s，收招 1.0s | 同上 `attackSequence` |
| 1012 攻击距离 | 1（到第 0 排即开打） | 同上 `attackRange` |
| 玩家点按输出 | Jab1 **30** / Jab2 **33** / Jab3 **37**（连点三段共 100），单段动作锁 0.40~0.60s | `Zhangfei_Jab1/2/3.asset` |
| 玩家攻击范围 | `rangeRows = 1` → **只能打前排** | `Zhangfei_Stab.asset` 等 |

**当前配置的问题**：`Tutorial_E0_Battle` wave0 `healthMultiplier = 0.4` → 有效 HP **40** → 玩家两段（30+33）就打死 → **1012 的 C 技根本打不出来** → 防御教学没有触发点。

**目标**：① 玩家在 C 技起手前/起手后都不能提前把它打死；② 教学结束后 4~6 段内能解决（教学节奏不拖沓）。

**做法**：教学拍期间**禁止玩家出招**（新增闸门 `TutorialGestureTarget.BlockAll` → 只挡攻击手势，不挡 UI 点击）。
于是敌人只需抗住「到达前排 → C 技起手」这一小段输出：敌人只有到第 0 排才可被攻击，窗口 ≈ 0.3~0.6s → 玩家最多 1~2 段 → **30~63 伤害**。

| 项 | 取值 | 理由 |
|---|---|---|
| wave0（内容1 霸体） | `healthMultiplier = **1.5**`（有效 **150**） | 扛得住起手前的 1~2 段；玩家 afterwards 约 5 段（≈2s 连点）解决 |
| wave1（内容2 技能打断） | `healthMultiplier = **1.5**`（有效 **150**） | 同上；拍内禁招 → 技能打断前不会被打死 |
| 两波 `damageMultiplier` | 保持 **0.3**（0.9 伤害/次） | 玩家 100 血，挨打不疼；保证「格挡成功」不会因为玩家已残血而看不出 |
| 109 骑兵波 | 保留为 wave2，不动 | 不删已有教学内容（要删由用户拍板） |

> 若实测发现玩家在 C 技起手前就能把它打死 → 把 `healthMultiplier` 提到 **2.0**（200）；反之若打得太久 → 降到 **1.2**（120）。

### D. 「初始教学 → 主菜单 → 天赋 → C2 → 新关卡」的跨场景流程

| 步骤 | 改动 | 落点 |
|---|---|---|
| D1 | **存档加教学进度**：`SaveData` 加 `tutorialStep`（枚举：`None / BattleDone / TalentDone / C2Taught / Done`），旧档迁移：`tutorialCompleted==true` → `Done` | `SaveData.cs`、`TutorialSaveSection.cs`（新增读写） |
| D2 | **让 C2 真的被锁住**：`talent_unlock_c2.moveConfig = Zhangfei_StabR2`、`hidden = 0`（图标已有）→ `IsMoveLocked` 生效，「买天赋解锁 C2」成立；**老存档自动解锁一次**（已有 C2 使用史的老档在迁移时直接标记为已解锁，避免体验回退） | `TalentTree_Main.asset`、`TalentSaveSection` |
| D3 | **元教程导演**（新组件，Battle.scene 常驻）：读 `tutorialStep` → 落地页指向天赋卡 → 检测 overlay 打开 → 指向 C2 节点 → 监听 `TalentTreeState.OnChanged` + `IsUnlocked("talent_unlock_c2")` → 引导关天赋 → 指向「开始关卡」→ 玩家点开始 → 进正式关 | 新脚本 `TutorialFlowDirector` |
| D4 | 正式关内**教 C2 手势**：新拍型（点一下 → 按住蓄满 → 松手）；白名单放行 Stab；成功判据 = 打出的招式是 `Zhangfei_StabR2`（`AttackSystem` 侧需要一个「本次执行的是哪个 move」的出口） | `AttackSystem`/`PlayerMoveStateMachine` 加一个「最近执行的 move」出口 + 新拍型 |
| D5 | **初始教学完成标记延后**：现在 `OnStageVictory` 立刻置 `tutorialCompleted=true`。新流程里战斗胜利只是 `step=BattleDone`；**C2 教完**才置 `Done` | `TutorialStageDirector.Dialogue.cs` 的 `OnStageVictory` |
| D6 | 铜钱确认：教学路线 `clearCoinReward 250` 是否在结算时真正进 `SaveManager`（`SettleCoins`）→ 实测确认；若不到账则调高或改发 | `StageRunFlow` |

### E. 常驻点击特效（新功能，与教学并行）

| 步骤 | 改动 | 落点 |
|---|---|---|
| E1 | 新增独立组件 `AttackTapIndicator`（**独立 prefab**，可多实例/池化），不复用 `TutorialGestureHint`（它单实例且接管手势输入） | 新脚本 + prefab |
| E2 | 出现条件：存在 `rowIndex==0` 的活敌人 **且** 玩家可攻击（无对话阻塞/无三选一/结算中/动作锁占用） | 新组件逻辑 |
| E3 | 生命周期：敌人出现 → 出现；敌人死亡/离场 → 消失；玩家出招期间**保留**（允许连点） | 同上 |
| E4 | 与教学**互斥**：教学拍进行中（手势闸门打开）时隐藏常驻特效，避免和手势示范叠在一起 | 新组件读 `InputManager.tutorialGestureTarget` |
| E5 | 尺寸：按 `task2-...handoff §9.4` 的结论**缩小**（白手 308 → 光圈描边级），先程序化/复用现有 Ring 素材 | 素材 |
| E6 | 范围：**全局**（D4 已定：正式关也常驻） | — |

---

## 四、拍板项：已全部定稿（见 §〇）

| # | 原问题 | 结论 |
|---|---|---|
| **Q1** | 内容2 教什么 | ✅ **教三选1拿到的技能**（D1） |
| **Q2** | 三选1 给什么 | ✅ **全程唯一一次，在节点二内容1 之后**；节点一不给技能；候选 `[旋风, 海浪, 冰雾升级]`，旋风必含（D2 + §0.1） |
| **Q3** | C2 锁不锁 | ✅ **锁 + 老存档自动解锁**（D3） |
| **Q4** | 点击特效范围 | ✅ **全局**（正式关也常驻，D4） |
| **Q5** | 霸体暂停形式 | ✅ **时停冻结**（冻结在 C 技起手瞬间） |
| **Q6** | 黑幕参数 | ✅ 选择路线时淡入 0.3s / 进战斗时淡出 0.2s |

**实施期仍需实测确认的两点**（不是需求问题，是实现事实）：
1. **海浪在杂兵身上到底断不打断** → 决定要不要给 `ApplyPushWave` 补一句 `CancelAttack()`（§0.1）。
2. **教学路线 `clearCoinReward 250` 是否真的进 `SaveManager` 铜钱** → 决定 100 铜钱的 C2 买不买得起。
3. **三选1 的弹窗时机**：现在三选1 由升级触发（`UpgradeChoiceManager`），要确认能精确落在「E0 wave0 结束」而不是别处。

---

## 五、改动清单（文件级，实施时按此执行）

**新增**
- `Assets/Scripts/Tutorial/TutorialRouteBlackout.cs`（节点移动黑幕）
- `Assets/Scripts/Tutorial/TutorialFlowDirector.cs`（跨场景元教程：落地页→天赋→C2→正式关）
- `Assets/Scripts/UI/AttackTapIndicator.cs` + prefab（常驻点击特效，**全局**）
- 新的 `DialogueEventData` 资产 ×2~3（三连提示 / 打断说明 / 天赋引导 / C2 引导）

**改**
- `Assets/Scripts/Tutorial/TutorialStageDirector.cs`（拍型枚举 + `requiredHits`）
- `.../TutorialStageDirector.Hints.cs`（TapCombo 拍循环；霸体时停防御拍）
- `.../TutorialStageDirector.Beats.cs`（霸体时停防御拍实现）
- `.../TutorialStageDirector.Dialogue.cs`（成功判据计数；`OnStageVictory` 改为置 step）
- `.../TutorialStageDirector.Rules.cs`（三选一移到节点二；白名单加 Launch）
- `.../TutorialStageDirector.Targets.cs`（新拍型 → 闸门映射）
- `Assets/Scripts/Player/InputManager.cs`（闸门枚举可能需要新增）
- `Assets/Scripts/Player/Combat/AttackSystem*.cs` / `PlayerMoveStateMachine.cs`（暴露「最近执行的 move」，供 C2 教学判据）
- `Assets/Scripts/Data/SaveData.cs` + `.../Save/TutorialSaveSection.cs`（教学进度；老档自动解锁 C2）
- `Assets/Scripts/Core/TalentTreeState.cs`（若需要「刚解锁某节点」事件）
- `Assets/Scripts/Core/DisplacementMover.Push.cs`（**可选**：海浪对 C 技敌人补 `CancelAttack()`，让「打断」口径对杂兵也成立）
- `Assets/Scripts/UI/FrontItemBar.cs`（暴露「某技能的槽位屏幕坐标」给教学拍用）
- `Assets/RouteData/Tutorial/Tutorial_E0_Battle.asset`（血量倍率）
- `Assets/ScriptableObjects/Talents/TalentTree_Main.asset`（C2 节点：moveConfig + hidden）
- `Assets/Scenes/Battle.scene`（新增组件与拍型配置）

**不动**
- 路线系统本体（`Assets/Scripts/RouteFake/*`）—— 黑幕靠旁挂组件监听，不改路线。
- `TutorialSceneDim`（场景压暗）—— 与黑幕是两件事，别混。

---

## 六、验收口径（建议）

1. **节点一**：wave0 点一下出招；wave1 连点三下出三连（断了能重来）；三连后自动出现「多次攻击可以打断敌人的攻击」文字且**不暂停**；点箭头前有黑幕过渡。
2. **节点二 wave0**：1012 起手（红描边/预警）→ 画面冻结 → 提示手落在防御键 → 玩家按住 → 解冻 → 敌人挥下来 → 血量不掉、破防值上涨 → 收拍；此时 1012 **还活着**（血量倍率生效）。
3. **节点二 wave0 结束**：弹一次三选一（含可打断霸体的技能）。
4. **节点二 wave1**：用所教手段打断 1012 的 C 技（Console 可验证 `CancelAttack` / 破势）。
5. **节点二清完** → 胜利 → 回落地页 → 出现「去天赋」引导 → 打开天赋 → 指向「指向突刺」→ 买下（扣 100 铜钱）→ 关掉天赋 → 出现「开始关卡」引导 → 进关 → 出现 C2 手势教学 → 打出 `Zhangfei_StabR2` → 教程标记置 Done（Console + 存档）。
6. **常驻点击特效**：任意正式关，前排敌人出现即有点击特效；打死后消失；后排敌人无特效；教学拍进行时不与手势示范重叠。
7. 全程 **Console 0 error**；连续跑通两遍（第二遍存档已跳过教学）。

---

## 七、风险与坑

1. **C2 锁解锁是行为变更**：不锁 → 教学无意义；锁 → 老玩家体验变化（Q3 已列缓解：老档自动解锁）。
2. **`AttackTypeFor` 只映射 4 种手势**：教主动技能（点 UI 槽）不在招式白名单体系里，要单独设计成功判据（技能是否释放成功 → `ActiveSkillInventory.OnSkillsChanged` / 冷却状态）。
3. **`TutorialGestureHint` 是单实例**：常驻点击特效必须独立组件，否则会和教学手势闸门互相打架（E1/E4）。
4. **1012 只有 1 个攻击步**（就是 C 技）：内容1 与内容2 若都要"霸体敌人"，需要两波 1012 或让同一波出两次 C 技（当前 `attackSequence` 只有 1 步）。
5. **时停拍的死锁风险**：冻结后玩家一直不按防御键 → 现有 `freezeTimeoutSeconds = 20s` 保险要沿用；`EndBeat` 必须恢复 timeScale。
6. **黑幕与 `TutorialSceneDim` 冲突**：黑幕是全屏 UI，会盖住角色 —— 只能在**没有战斗**的转场用（节点移动），别在战斗拍里用。
7. **跨场景**：回主菜单 = 重载 Battle.scene → 元教程状态**只能存存档**（D1）。
8. **白名单时机**：`ApplyTutorialRules` 目前按 battle 判定（n1/e0），新流程要在「波次」粒度上决定是否给三选一 → 需要给 StageConfig/波次加钩子，否则只能在 `ApplyTutorialRules` 里按 waveIndex 特判。
