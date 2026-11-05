---
id: kd_385e0cca-8f00-43e7-a7f6-69ede6fdabc2
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: 教学关 + 提示 UI 交接（2026-11-02，第九轮收尾，**用户已验收**）——九轮反馈全部修完并留证：引导残留（Hide 停协程 + 循环示范自检 + 宿主看门狗）；霸体防御拍「抬手做完再冻结」；常驻引导错位（手改相对偏移）；按压手势分工（指尖=定位锚点、掌心=旋转支点，删逐帧补偿）；常驻指引随机 1 只 + 可配闪烁；「用技能打断」整段教学（出口光标清理误杀教学拍 + 场景里两个 FrontItemBar + 技能槽定位兜底 + 槽位高亮脉冲）。含逐轮根因与证据、复现步骤、测试台坑、自查日志格式。
---

# 教学关 + 提示 UI 交接（2026-11-02，第十一轮验收后）

> 状态：**九轮反馈全部修完，2026-11-02 用户验收通过**；**尚未提交**（工作区有 80+ 个改动文件，见 §6）。
> 逐轮记录：§1（第一轮三条根因）→ §8（残留专项）→ §9（问题2/3/4）→ §10（技能打断教学缺失）→
> §11（查 Editor.log）→ §12（真凶=出口光标清理误杀 + 两个 FrontItemBar）→ **§13 验收结论**。
> **§5 的「手在飘」已闭环**（第四轮按「指尖=定位锚点 / 掌心=旋转支点」重做，用户确认修复）。
> 本文是教学关 + 提示 UI 这条线的**当前入口**。方案与逐轮记录见 `plan/tutorial-and-hint-overhaul-plan-20261101.md`（§0.15 = 本轮）；
> 问题清单见 `memory/known-issues.md`（第 11、12 条）；遗留项见 `plan/unresolved-issues.md`。
>
> **新对话接手必读**：§1（三条根因 + 证据）、§4（测试台坑，不看会误判回归）、§5（还要人眼确认的）。

## 0. 一句话

用户第三轮反馈的三条——「引导完成不消失」「一标红就开教学」「正式关手和光圈错位」——都已定位到**具体代码根因**并修复，
Play Mode 全流程 + 正式关实测通过（教学全流程无残留、冻结发生在抬手完成后、正式关指尖→圈心恒为 0px）。

## 1. 本轮三件事

### ① 引导完成后不消失（光圈/手指残留）

- **根因**：`TutorialGestureHint.Hide()` 只调 `HideLayers()`（`SetActive(false)`）**不停正在跑的协程**；
  而循环示范（`PlayTapLoop` 连点 / `PlayPressHoldLoop` 按住）是**无限循环**，每轮都会 `ring.gameObject.SetActive(true)`
  → **下一帧光圈又被画回来**。教学关的残留提示因此还会**跟着流进正式关**。
- **修复**：`Hide()` = `StopRoutine()` + `HideLayers()`；`HideLayers()` 里顺手把手恢复静止姿态（否则下次示范从「按下/转过」的姿态起手）。
  协程自己播完的路径走 `Finish()`（仍只 `HideLayers()`），不受影响。
- **证据**（Play Mode，每拍结束后 0.6s 采样）：
  点按教学第一拍结束 False/False；选【海浪】+0.5s False/False；技能打断拍结束 False/False；全流程结束 False/False。

### ② 一标红就开教学（霸体防御拍时机）

- **根因**：冻结判据是 `isAttackAnimating && isCFrame` 刚成立 → 那是**霸体刚起手**（敌人刚变红），语义不明。
- **实测**：1012 的 C 技攻击 tween 全长 **3.13s**，`isCFrame` 在**剩 1.15s** 时解除（= 出伤）；
  攻击动画 `Enemy_1012_Attack.anim` 关键帧 0/0.3/0.6/0.9/1.2/1.5/2.0…，出伤帧是 `Enemy_1012_attack6`。
- **修复**：冻结判据改为**攻击剩余时长** `Enemy.TimeToAttackEnd() <= defendPromptToEndSeconds`（默认 **1.5s**，可配）且仍在 `isCFrame` 内。
- **顺带修的两个真问题**（都是实测暴露，不是推测）：
  1. **整拍被跳过**：E0 第一波 1012 只有 150 血，玩家约 1.6s 就点死它 → 霸体技没起手人没了 → 格挡教学根本没发生
     （日志里 `拍 → DefendButton/0` 之后直接 `拍 → SkillSlot/1`）。→ 改成**拍一开始就禁出招**（`BeginBeat(BlockAll)`），
     台词已交代「它会出红技、按住防御键」；**压暗留到冻结那一刻**（别在敌人抬手时把画面压黑）。
  2. **目标死了就丢拍** → 改为**换下一只**继续等霸体技。
- **证据**：冻结瞬间敌人显示帧 **`Enemy_1012_attack5`**（举刀姿态，出伤帧 attack6 未出现）、攻击剩余 **1.48s**、`cFrame=True`、满血 150；
  玩家按住防御后解冻，这一击**被吸收 1 次**（教程里 1012 伤害 ×0.3 = 1 点）。

### ③ 正式关里手和光圈错位

- **根因**：`AttackTapIndicator` 的**手是 TapHint 根节点的子物体**（根节点已摆在敌人屏幕点上），
  但手的 `anchoredPosition` 写成了**画布绝对坐标**（`local + 偏移`）→ **位置被算两遍**，指尖落到 `2 × 目标点**。
  敌人离画面中心越远偏得越多 → **教学关敌人居中所以看不出来**，正式关偏 ±176 画布单位 → 偏 ~200px。
- **第二个来源**：按压动画绕**掌心**旋转 + 缩放 → 指尖划出 ≈50px 的弧，看着也像「手和圈对不上」。
- **修复**：① 手的 `anchoredPosition` 改为**相对根节点的偏移**；② 新增 `TutorialGestureStyle.RotatedTipOffset(size, angle, scale)`，
  按压每帧把位置**反向补偿**回去，指尖始终压在圈心（常驻引导 + 教学手势层都接上）。
- **证据**（正式关「长坂坡·卷轴」N1，1080×2160）：

  | | 修复前 | 修复后 |
  |---|---|---|
  | 正式关 常驻引导 指尖→圈心 | 247px / 151px | **0.0px**（6s / 240 次采样恒为 0） |
  | 教学关 常驻引导 | 0~50px | **0.0px**（100 次采样） |
  | 教学关 教学手势层 | 0~50px | **0.0px**（30 次采样） |

  光圈尺寸复核：可见直径 319px / 敌人屏高 1098px = **0.29**（配置 `ringHeightRatio = 0.3`）✓ 不算过大。
- 注：教学手势层（`TutorialGestureHint`）的 finger/ring/trail 都是 `ScreenSpace` 的**同父**子物体 → 坐标系一致，本来没这个 bug。

## 2. 代码改动清单

| 文件 | 改动 | 关键位置 |
|---|---|---|
| `Assets/Scripts/UI/TutorialGestureHint.cs` | `Hide()` 停协程；`HideLayers()` 恢复手姿态；按压时指尖锁点 | `Hide()` ~378、`HideLayers()` ~800、`TapRoutine` ~600 |
| `Assets/Scripts/UI/AttackTapIndicator.cs` | 手的 anchoredPosition 改相对根节点；按压指尖锁点 | `Refresh()` ~173、`Animate()` ~367 |
| `Assets/Scripts/UI/TutorialGestureStyle.cs` | 新增 `TipOffset(size)` / `RotatedTipOffset(size, angleDeg, scale)` | 文末 |
| `Assets/Scripts/Tutorial/TutorialStageDirector.Beats.cs` | 霸体防御拍：抬手做完再冻结 + 拍开始禁出招 + 目标死亡换下一只 | `DefendAtCAttackRoutine` ~281、新字段 `defendPromptToEndSeconds` ~24 |
| `Assets/Scripts/Editor/TutorialFlowDriver.cs`（**未提交的新文件**） | 自动驱动：只在**冻结时**才按防御键（模拟真实玩家） | `Drive()` ~162 |

> `Assets/Scripts/UI/AttackTapIndicator.cs`、`TutorialGestureStyle.cs`、`TutorialFlowDriver.cs` 是**本轮新增、尚未入库**的文件（git 里是 `??`）。

## 3. 怎么复现 / 验收（Play Mode）

### 3.1 教学关全流程（驱动）

```
unity_set_play_mode(play)
unity_execute:
  var save = SaveManager.Load(); save.tutorialCompleted = false;
  save.clearedStageIds.RemoveAll(id => id == 9100); SaveManager.Save(save);
  TutorialFlowDriver.StartInPlayMode();
```
驱动会自动：点按教学 → 三连 → 路线选择 → 黑幕 → E0 w0 霸体防御拍 → 升级三选一 → E0 w1 技能打断 → 结算 250 铜钱。
三选一默认 **20 秒后**才自动选第一个选项（要看引导手指卡就先等）；要立刻选就自己 `UpgradeChoiceManager.Instance.ConfirmChoice(def)`。

### 3.2 正式关对位测量（指尖→圈心）

正式关 = 落地页默认那条 **「长坂坡·卷轴」**（stageId 1，`RouteFlowHub.Current` 启动时已 Prepared，阶段 `Ready`）。
进关：`RouteFlowHub.Current.RunPrepared()` → 等前排敌人出现 → 取 BattleHUD 画布下名为 `TapHint` 的子节点：

```
Vector2 ringCenter = RectTransformUtility.WorldToScreenPoint(null, ring.position);
Vector3 tipLocal = new Vector3(
    (TutorialGestureStyle.TipUV.x - TutorialGestureStyle.PalmUV.x) * hand.sizeDelta.x,
    (TutorialGestureStyle.TipUV.y - TutorialGestureStyle.PalmUV.y) * hand.sizeDelta.y, 0f);
Vector2 tip = RectTransformUtility.WorldToScreenPoint(null, hand.TransformPoint(tipLocal));
float dev = Vector2.Distance(tip, ringCenter);   // 期望 ≈ 0
```

### 3.3 存档保护（自动流程会改存档）

存档是 PlayerPrefs 键 `player_save`。跑自动化前**备份**、跑完**还原**：

```
string backup = PlayerPrefs.GetString("player_save", "");
... 跑 ...
PlayerPrefs.SetString("player_save", backup); PlayerPrefs.Save(); SaveManager._cache = null;
```
不还原的后果：`tutorialCompleted=true` + `clearedStageIds` 含 9100 → 下次教学直接判定通关，铜钱 +250/次。

### 3.4 本轮留下的截图（我这边看不了画面，需要人眼）

`Library/Locus/tmp/` 下：`final_defend_freeze.png`（防御拍冻结瞬间）、`formal_tap_indicator_fixed.png`、
`formal_tap_indicator_final.png`、`tutorial_tap_indicator.png`、`tutorial_hint_alignment.png`、`verify_A~E_*.png`。

## 4. 测试台的坑（先看，否则会误判成回归）

1. **重跑教学必须先退出再进 Play Mode**。同一会话里再 `StartInPlayMode()` 会 `PrepareForEntry` 重启路线，
   拍序列会从 `SkillSlot/1` 跳回 `Tap/0`，采样全被污染（我踩过一次，误以为残留回归）。
2. **`TutorialFlowDriver` 驱动的是教学路线**；正式关要用 `RouteFlowHub.Current.RunPrepared()`，两者别混。
3. **驱动注入的 `SubmitGestureForDebug` 会绕过 `tutorialGestureTarget` 闸门** → 玩家在拍期间照样能打死敌人；
   真实玩家被 `BeginBeat(BlockAll)` 挡住。所以「拍被跳过」这类问题在自动流程里更容易复现。
4. **`ScreenCapture.CaptureScreenshot` 在 Locus 侧拿不到画面**（只能读文件），所以观感类验收必须交给用户。
5. `unity_execute` 片段是**方法体**：结尾要 `return null;`（不是 `return;`）；`ctx.GetConsoleLog` 返回的是
   `ConsoleLogResult` 不是 List（要过滤日志直接 `unity_get_console_log` 后在 bash 里 grep 落盘文件更快）。
6. 敌人 prefab / 场景在 **Tuanjie 版 Unity** 里 `.meta` 的 GUID 是 base64，YAML 引用却是 hex →
   `grep guid` 找资产经常落空，改用 `AssetDatabase.FindAssets` / `LoadAssetAtPath` 查。

## 5. 还需要人眼/用户确认的 —— ✅ 全部闭环（2026-11-02 验收）
1. **霸体防御拍冻结那一刻的观感**：敌人举着刀停住 + 提示手落在防御键上；台词「他这一下带着霸体，正面硬碰打断不掉——按下防御键，把枪举起来挡下来！」。
2. **正式关手压圈心的手感**：现在指尖恒定压在圈心（0px），按压时手身绕掌心转——看是否还像「手在飘」。**（2026-11-02 晚：仍未处理，待用户定方向——绕指尖转 / 取消旋转只缩放 / 保留但减小幅度）**
3. **第一拍开始禁出招**那段（敌人起手到抬手完成之间玩家只能看）是否太久/太突兀；台词已解释，但可调
   `Tutorial_E0_Battle.asset` 的 1012 血量或改 `defendPromptToEndSeconds`。

## 6. 工作区状态 / 提交注意

- 分支 `feat/stage-select-talent-overlays`，HEAD `ca51b68`。
- `git status` 有 **88 项**改动（截至第九轮），其中大部分是**更早几轮**的教学改造（Enemy prefab、Dialogue asset、
  ScriptableObjects、`Battle.scene`、多个 `TutorialStageDirector.*` 等），**不是最近几轮产生的**。
- **本条线（教学 + 引导提示）累计动过的文件**：
  `Assets/Scripts/UI/TutorialGestureHint.cs`、`TutorialGestureStyle.cs`、`AttackTapIndicator.cs`、`FrontItemBar.cs`、
  `Assets/Scripts/Tutorial/TutorialStageDirector.cs` + `.Beats.cs` + `.Hints.cs` + `.Dialogue.cs` + `.Rules.cs` + `.Targets.cs`、
  `Assets/Scripts/Core/ActiveSkillInventory.cs`、`Assets/Scripts/Data/GameplayInputGate.cs`、
  `Assets/Scripts/Editor/TutorialFlowDriver.cs`、`Assets/Scenes/Battle.scene`。
- 本轮新增未入库：`Assets/Scripts/UI/AttackTapIndicator.cs(+meta)`、`Assets/Scripts/UI/TutorialGestureStyle.cs(+meta)`、
  `Assets/Scripts/Editor/TutorialFlowDriver.cs(+meta)`。
- 知识文档改动：`Locus/knowledge/plan/tutorial-and-hint-overhaul-plan-20261101.md`（§0.15）、
  `memory/known-issues.md`（第 11、12、13、14 条）、`plan/unresolved-issues.md`（TODO-5/6/7 关闭）、
  本文件（§8~§13）。
- 临时调试日志（`Enemy.Attack.cs` / `Enemy.Combat.cs`）已全部撤掉，那两个文件的 diff 为空。
- **存档已被自动化改过并还原**（跑完 `player_save` 恢复原串）。

## 7. 下一轮可以接着做（2026-11-02 验收后仍可做的）
1. **提交**：本条线的工作区仍有 80+ 个改动文件未入库（见 §6），需要一次整理提交。
2. **表现打磨**（不影响功能）：常驻引导的光圈比例 / 闪烁节奏（`AttackTapIndicator` 上可配）；
   技能槽高亮现在是缩放脉冲，可换成更显眼的金框或发光。
3. **流程打磨**：「用技能打断」这一拍等待敌人抡招的那几秒（最长一个攻击周期 ≈4.5s），
   可以再加一句提示（例如「盯住他的刀，等它举到顶」），减少玩家「怎么没反应」的困惑。
4. 若要动常驻引导：`AttackTapIndicator` 的尺寸参数在 `Battle.scene` 的 `Attack Tap Indicator` 组件上
   （`ringHeightRatio=0.3`、`handToRingRatio=1.2833`、`showDuringTeaching=0`、`tapPressAngle=8`）。

## 8. 第二轮（2026-11-02 晚）：「残留不消失」专项

> 用户当时的原话：**「那我先测试一下，你先了解相关功能」**，优先级选 **「残留不消失最急」**。

### 8.1 先确认了「不是错位」

| 对象 | 测量 | 结果 |
|---|---|---|
| 正式关常驻引导 `AttackTapIndicator` | 指尖→圈心（同一画布内比较） | **0.00px**（两个前排敌人） |
| 教学手势层 `TutorialGestureHint` | 指尖→圈心（画布局部坐标，417 次活动示范采样） | **0.00**（含按压动画全程） |

→ 交接文档 §1 ③ 的错位确实已经修好；本轮只处理**残留**。

### 8.2 残留的 4 个洞（第一轮只堵住了 1 个）

1. **循环示范没有「主人」概念**：`PlayTapLoop` / `PlayPressHoldLoop` / `PlayParry` 都是**无限协程**。
   上一轮只改了 `Hide()`（停协程），但**没堵住「根本没人来调 Hide()」的路** → 示范永远画下去。
2. **技能打断拍提前退出**：`TutorialStageDirector.Beats.cs` 的 `SkillSlotInterruptRoutine` 在
   「舞台结束 / 目标死了」时 `yield break` → **跳过收尾** → 此时「循环点技能槽」的示范已在跑 → 永久残留。
3. **三选一的那只手不属于任何拍**：`ShowChoiceHint()` 直接 `PlayTapLoop(卡片坐标)`，
   只有 `OnChoiceSelected` 会收它；弹窗以别的方式关掉 → 无限协程留着 → **跟着流进正式关**。
4. **`keepRingVisible` 从不复位**：连点教学的光圈常驻标志会污染后面的循环示范。

### 8.3 改了什么（4 个文件）

| 文件 | 改动 |
|---|---|
| `Assets/Scripts/UI/TutorialGestureHint.cs` | ① 新增单例 `Instance` + `IsShowing`（供外部做残留兜底）；② 循环示范每轮问宿主 `HostAllowsDemo()`（= 导演的 `BeatActive \|\| ChoiceHintActive \|\| RouteHintActive`），**主人没了就自己 `Finish()` 收摊**；③ `Hide()`/`Cancel()` 复位 `keepRingVisible` |
| `Assets/Scripts/Tutorial/TutorialStageDirector.cs` | ① 新增 `ChoiceHintActive` / `RouteHintActive` 只读属性；② `ShowChoiceHint`/`OnChoiceSelected` 维护 `ChoiceHintActive`；③ `Update()` 兜底：三选一弹窗已关 → 收手；**不在教学路线上** → 一律收手 |
| `Assets/Scripts/Tutorial/TutorialStageDirector.Beats.cs` | `SkillSlotInterruptRoutine` 的两处 `yield break` → `break`（必定走到收尾：解冻 → `EndBeat()` → `Hide()`） |
| `Assets/Scripts/UI/AttackTapIndicator.cs` | 教学手势提示**只要还在屏上**就不挂常驻引导（之前只看 `BeatActive`）→ 顺带修掉「教学关同时出现两套手+光圈」 |

### 8.4 验证（Play Mode，全流程驱动）

- 420 次采样 / 85 秒：提示层在屏 112 次（说明示范真的在播，不是空跑）
  → **无主残留 = 0**、**漏到教学路线外 = 0**、**两套提示同时在屏 = 0**；结束时提示层为空。
- 单测：`PlayTapLoop` / `PlayPressHoldLoop` / `PlayParry` 在「无主人」时启动 → **立即自收**（`IsShowing=False`）。
- 正式关回归：常驻引导 2 个可见、指尖→圈心 0.00px、教学手势不在屏（截图已留）。

### 8.5 还没做的

- **「手在飘」**（§5.2）：按压动画绕掌心转，手身相对圈心摆 ≈48px（指尖锁死）。
  三个方向待用户选：① 改成绕**指尖**转（像真手指往下压）；② 取消旋转、只做缩放；
  ③ 保留但减小幅度/缩短按压行程。

---

## 9. 第三轮（2026-11-02 晚）：用户反馈 4 条 → 问题2/3/4 已修并实测，问题1 第四轮定稿

> 用户反馈：① 手和圈错位「已修复」✓；② 手是**绕手指尖**在转，不像点击；③ 常态攻击指引应该**随机给 1 个**；
> ④ 常态点击引导应该**闪烁**而不是常驻；⑤ 三选一后的「暂停→点技能打断 C 技」教学**缺失**。

### 9.1 问题1（手指/掌心分工）—— 只做了分析，未改代码

| 角色 | 点 | 含义 |
|---|---|---|
| **定位锚点** | **指尖** | 手的位置由它决定 → 静止时指尖精确落在圈心 |
| **旋转支点** | **掌心** | 按压动画绕它转 → 掌心在按压过程中**不移动** |

**代码里 pivot 确实 = 掌心**（`TutorialGestureStyle.HandPivot` = PalmUV 0.48/0.48）。
问题在**多出来的那条逐帧位置补偿**：把指尖每帧钉回圈心（`AttackTapIndicator.cs` / `TutorialGestureHint.cs`）。
「绕掌心转 + 钉死指尖」数学上等价于「绕指尖转」→ 掌心被迫绕指尖走 → **视觉上就是「整只手以指尖为圆心转」**。

实测（正式关，一个完整按压循环）：指尖→圈心 0.0~10.1px（10px 是采样滞后一帧）、掌心→圈心 192.1~204.3px（半径不变、切向摆 ≈30px）。

**代价量化**：删掉补偿后，指尖离圈心最大距离 = 2×|掌心→指尖|·sin(θ/2)。
常驻引导的手 475 单位（|掌心→指尖|=181）：θ=14°→44 单位≈47px（光圈半径 185 的 24%）、θ=8°→25 单位≈27px（14%）、θ=6°→19 单位≈20px（10%）。

**方案（待用户选）**：
- **A（建议）**：保留钉指尖、**去掉旋转** → 只留轻微缩放 → 零摆幅、零偏移，指尖恒 0px。
- **B**：保留绕掌心转、**去掉钉指尖** → 掌心不动、指尖下沉一小弧（θ=8° 时 ≈27px，仍在光圈内）。
- **C**：折中 —— 绕掌心 6° + 保留钉指尖 → 掌心几乎不动，手仍然「动一下」。

### 9.2 问题2（随机给 1 个）✅ 已修
- 根因：`CollectTargets()` 收集所有 `rowIndex == 0` 的敌人 → `Refresh()` 每个建一个单元（实测前排 2 只 → 2 个提示）。
- 修：`PickTargets()` 只挑**随机 1 只**，且**稳定**（还在候选里就继续用，死了/换人才重抽，否则每 0.1s 重抽会乱跳）。
- 验证：正式关前排 2 只 → 常驻引导可见单元 **1**。

### 9.3 问题3（闪烁）✅ 已修
- 根因：`Animate()` 手 alpha 恒 `handAlpha`、光圈 alpha 恒 0.63~0.9，全程不灭。
- 修：新增可配闪烁 `blinkEnabled` / `blinkPeriod=1.8` / `blinkOnRatio=0.55` / `blinkFadeSeconds=0.22`（手+光圈+波纹一起淡入淡出）。
- 验证：实测光圈 alpha 0.00~0.93、手 alpha 0.00~0.95（会灭到 0 ✓）。

### 9.4 问题4（技能打断教学缺失）✅ 已修（5 条根因）
1. **成功判据被卡肉误触发**：`IsSuspended` 含 `SuspendSource.HitStop` → 任何一次命中都成立 → 拍瞬间收拍（旧日志「打断=False 挂起=True → 收拍」）。
   → 改为 `target.attackInterrupted || (target.IsSuspended && !target.IsHitStopped)`。
2. **时停期间放不出技能**：`ActiveSkillInventory.TryActivate` 的 `if (Time.timeScale <= 0f) return false;`
   → 新增教学开关 `GameplayInputGate.AllowSkillActivationWhileFrozen`，技能拍冻结期间为 true。
3. **台词在播时点击被吞**（`IsInteractionBlocked`）→ 冻结那一刻先 Skip 台词。
4. **拍开始不拦出招** → 改成拍一开始 `BeginBeat(BlockAll)`（与霸体防御拍一致），目标死了**换下一只**。
5. **等技能槽 15 秒就跳过整拍** → 上限改 60 秒且不再跳过教学。
6. 顺带：冻结时机从「C 技刚起手」改成「抬手做完」（`skillPromptToEndSeconds=1.5`，与防御拍同口径）。
- 验证：驱动全流程 → 39.74s 冻结 → 同帧放技能 → `attackInterrupted=True` → 拍正常收 → 教学通关（驱动日志 phase → Completed）。
- **第四轮补测「真人点击」路径**（停掉驱动、只让 UI 走）：冻结瞬间截图确认 —— 敌人举刀定格 + 画面压暗 + **手指精准落在技能槽上**（指尖屏幕坐标 241.88,283.50 = 技能槽坐标）；用 EventSystem/Button 模拟真人点击技能槽 → **技能真的释放**（冷却置上）✓。
  → 该教学在第四轮的代码下是**通的**（前一轮「点技能没反应」是 `ActiveSkillInventory` 的 timeScale 闸门 + 台词阻塞所致，已修）。

### 9.5 遗留

- **问题1 未动**（等用户选 A/B/C 与 θ 默认值）→ 第四轮已实施（见 9.1）。
- 自动化流程的已知局限：驱动注入的 `SubmitGestureForDebug` **绕过** `tutorialGestureTarget` 闸门 → 仍能把教学目标打死（真实玩家被 `BeginBeat(BlockAll)` 挡住）。

---

## 10. 第五轮（2026-11-02 深夜）：问题2「技能打断教学完全没有实现」（第七/八轮补完根因）

> 用户原话：「我要的效果是：玩家点击选择3选1奖励后，敌人进入C技攻击，像防御教程那样暂停，触发手势引导让玩家点击技能释放，然后变回正常流程。
> 当前这部分是完全丢失了，玩家点击技能后也无法回到正常流程攻击敌人，变成只能用防御和技能。」

### 10.1 根因（实测取证）

1. **拍的顺序错了** —— 拍在**波次开始**就开，而三选一弹窗恰好也是这一刻弹出：
   实测 **22.38s 冻结 / 22.40s 弹窗** → 冻结画面和「点技能」手势**整个被弹窗盖住**，
   玩家根本看不到这一步；等弹窗关掉，拍已经接近结束 → **教学等于没发生**。
2. **点完技能拍不收** → `InputManager.tutorialGestureTarget` 一直停在 `BlockAll`
   → 玩家只剩防御键和技能能用（正好是用户说的「变成只能用防御和技能」）。
   根因是成功判据 `target.IsSuspended` 会被**普通命中的卡肉**触发（`SuspendSource.HitStop`），
   以及「技能放出后没打断就不收拍」—— 两边都会让收拍时机错位。

### 10.2 修法

- `SkillSlotInterruptRoutine` **先等玩家把三选一选完**（`ChosenSkill != null`，上限 `skillChoiceWaitSeconds=90`），
  这一段**不开拍、不拦出招**；选完之后才正式开拍（`BeginBeat(BlockAll)`）→ 敌人再抡 C 技 → 抬手做完 → 冻结 → 手势落在技能槽。
- 成功判据改成 **必须先 `skillUsed`（技能真的放出去了）**；技能放出后最多再等 `skillResultWaitSeconds=3`，
  **没打断也收拍** → 出招闸门一定会恢复，玩家不会被卡成「只能防御和技能」。
- 驱动（测试工具）同步修：技能拍期间**不出招**（否则会把教学目标打死）、只在冻结时放技能。

### 10.3 实测（停驱动 = 模拟真人）

| 时刻 | 事件 |
|---|---|
| 21.66s | 技能拍开始（等选卡，**不开拍**，玩家仍能出招） |
| 22.40s | 玩家选【海浪】 |
| 22.52s | 正式开拍 → 出招闸门 BlockAll（防教学目标被打死） |
| 25.36s | 敌人 C 技抬手完成 → **冻结 + 画面压暗 + 手势落在技能槽**（指尖与技能槽偏差 0.2px） |
| 点击 | 技能槽 → 技能释放（CD=10）→ 0.05s 后：解冻、拍收、**闸门恢复 None（玩家能正常出招）** ✓ |

截图：`Library/Locus/Screenshots/locus_game_20261102_063039_191.png`（冻结瞬间：敌人举刀定格 + 手指着技能槽）。

---

## 11. 第六轮（2026-11-02 深夜）：查 Editor.log 后的结论与加固

> 用户：「当前我测试中与修复前完全没有变化…当前获得3选1技能后下一次敌人攻击没有冻结并提示我用技能打断。请检查日志思考问题所在」。

### 11.1 日志在哪（踩过的坑）

- **真日志**：`%LOCALAPPDATA%/Tuanjie/Editor/Editor.log`（50MB，滚动）。
- **项目里的 `Logs/Editor.log` 是 10 月 27 日的旧文件** → 查它什么都看不到（第一轮就因此空转了一阵）。
- 过滤技巧：`grep -a "玩家选中技能\|技能打断拍\|幕台词" Editor.log`（Unity 的堆栈行会淹没结果，只取 `^[` 开头的行）。

### 11.2 日志给出的事实

- 用户最近 3 次教学会话里，技能打断拍**都成功跑通**了：
  `命中手势步骤 4 wave=1 kind=SkillSlot` → `三选1 回调` → `玩家选中技能：海浪`
  → `技能打断拍：敌人抬手完成（剩余 1.48~1.50s）→ 已锁住动作，等待玩家点技能槽`
  → `Activated 海浪 Lv.1` → `收拍（打断=True 挂起=True 卡肉=True 已放技能=True）`。
- 用户**最后一次**会话的日志**停在「玩家选中技能：海浪」**，之后敌人还没抡招就退出了 Play Mode
  → 那次是「等不及」，不是功能没生效。

### 11.3 仍然存在的真隐患（已修）

**玩家选完卡就手痒先点了技能**（进入冷却）→ 旧写法把这个「提前释放」当成「教学已完成」→
`skillUsed=true` → **冻结分支被跳过** → 敌人之后再怎么抡招都不会冻结 → 这一段教学静默消失。
这与用户描述的「下一次敌人攻击没有冻结」完全吻合。

修法：
1. **冻结必须等技能冷却结束**（`!IsSkillOnCooldown(wantedId)`）—— 否则玩家在冻结时点技能没反应，教学卡死 30 秒；
2. `skillUsed` **只认冻结之后的释放**（`freezeHappened` 才算）。

实测：选完卡立刻放技能 → 等冷却 10s → 仍然正常冻结 → 放技能 → 收拍 ✓。

---

## 12. 第八/九轮（2026-11-02 深夜）：「用技能打断」这一拍的真凶 = 出口光标清理误杀教学拍 + 两个 FrontItemBar

> 用户：「当前获得3选1技能后下一次敌人攻击没有冻结并提示我用技能打断」+「为什么会受击」。
> 用户要求：**自己复述正确的流程**（不要老问他方向）。

### 12.1 正确流程（玩家拿到三选一之后）

1. 玩家点完三选一 → **教学立刻进入下一课**，玩家不需要额外操作。
2. 敌人抡起霸体招。
3. **敌人抬手到顶那一刻游戏暂停**（画面压暗、敌人定格）—— 与霸体防御教学同一套。
4. **技能槽上出现手势**（指尖压在技能图标上）。
5. 玩家点技能 → 技能放出 → 敌人的这一下被打断。
6. **立刻恢复正常战斗**：出招闸门解除、防御键可用、敌人回到正常节奏。

等待期间玩家不能是「站着挨打」的状态：至少要能防御。

### 12.2 真凶（堆栈取证）

`TutorialStageDirector.Update()` 里的**出口光标清理**：

```csharp
if (_routeHintActive && (!onTutorialRoute || routeRuntime.Phase != FakeRoutePhase.ChoosingRoute))
{
    StopHint();      // ← 会 StopCoroutine(_hintRoutine) → 把当前教学拍一起掐掉
}
```

`_routeHintActive`（路线光标标记）**从不复位** → 这段清理会在某个时机触发，
而 `StopHint()` 会把**正在跑的教学拍**一起停掉 → 用户那次刚好在「用技能打断」这一拍开拍的同一帧触发 →
**冻结那段代码根本没执行**（日志里「等敌人抡起霸体技」一次都没出现），
同时出招被封 + 防御键被 `OnChoiceSelected` 锁住 → 玩家只能站着挨打。

**修**：那段清理**只收路线光标自己**（新增 `_routeHintRoutine` 单独持有那个协程），不再调 `StopHint()`。

### 12.3 同一轮一起修的

- 找技能槽不再阻塞（查一次 → 2 秒 → 按槽位序号兜底 → 定位不到也照样冻结+台词）；
- 这一拍解锁防御键（等待期间能格挡）；
- `StopHint()` / `OnDisable()` 加堆栈日志（下次日志直接告诉你是谁掐的）。

### 12.4 阶段日志（下次自查用）

### 12.4b 第九轮补充：手势指引不出 = 场景里有**两个 FrontItemBar**

诊断日志（`[Tutorial][技能拍] 技能槽定位：…`）直接给出：

```
技能槽定位：兜底也失败 | entries=1 图标已注册=0 槽位数=0 capacity=4 用主动技能规则=True | #0 active_wave(id=1,已注册=False)
```

→ `FrontItemBar.Instance` 指向的是**根节点上那个没配槽位**的组件（`HudCard` 下那个才有 4 个槽位）→
所有技能槽查找都查空表 → **手势指引出不来**。

修法：① `Awake()` 让**有槽位的那个优先当 Instance**，并加 `ResolveForSkills()`；
② 教学拍查槽位时逐个 FrontItemBar 试；③ 冻结期间给技能槽图标**脉冲缩放**（高亮），收拍复位。

验证（模拟真人）：冻结中 ts=0、手势在屏、**指尖→技能槽 0.6px**、**图标脉冲 1.000~1.120**、
放技能 → 打断 → 收拍 ✓。截图 `Library/Locus/Screenshots/locus_game_20261102_075809_274.png`。

`Enemy.Attack.cs` 的 `PlayAttackAnimationTween()`：

```csharp
var step = attackSequence[_currentAttackStep];
_currentAttackStep = (_currentAttackStep + 1) % attackSequence.Count;
bool isCAttack = step.isCAttack;
...
isCFrame = isCAttack;     // ← 只有 C 技才是霸体帧
```

→ **1012 的普通攻击和 C 技是轮流上的**。
而这一拍的冻结判据原来要求 `isCFrame` → **敌人的「下一次攻击」如果不是 C 技，就永远等不到** →
教学静默消失（用户那次会话：1012 连续两次普通攻击，一次都没冻上，日志里连一行「技能打断拍」都没有）。

**修法**：冻结判据改成 **「敌人的下一次攻击」**（不再要求 C 技），抬起到顶（剩余 ≤ 1.5s）就冻；
日志会标明这次是「C技」还是「普通招」。

### 11.5 流程坑（也是「和修复前完全没变化」的原因之一）

**我在改代码时反复 `unity_recompile` → 域重载会把用户的 Play Mode 重置**
（Editor.log 里能看到 `Start importing …StageController.cs` → `场景加载完成，自动开始关卡` → 教程被重开）。

→ 约定：**改完代码先告诉用户、等用户测完再编译**；不要在用户测试期间动工程。

### 11.6 阶段日志（下次自查用）

`[Tutorial][技能拍]` 前缀，四行看懂这一拍走到哪：

```
① 等玩家选完技能：等了 0.6s，选中=active_wave
② 开拍（暂时禁出招，等它抡霸体招）
WAIT-C 敌人=Enemy_1012(Clone)/Attacking 抡招=True C技=True 剩余=1.49s 阈值=1.50s 技能可用=True   ← 每秒一条
③ 已冻结：敌人抬起到顶（C技）→ 点技能槽打断它
```

若看不到 ① → 玩家没选卡；看不到 ② → 拍没开；`WAIT-C` 里「剩余」一直 > 阈值 → 敌人的抡招时序没到；
「技能可用=False」→ 玩家提前放了技能，等冷却即可。

---

## 13. 验收结论（2026-11-02，用户已验收）

### 13.1 最终流程（玩家拿到三选一之后）

1. 玩家点完三选一 → **教学立刻进入下一课**，玩家不需要额外操作。
2. 敌人抡起霸体招。
3. **敌人抬手到顶那一刻游戏暂停**（画面压暗、敌人定格、红框霸体高亮）—— 与霸体防御教学同一套。
4. **技能槽上出现手势**（指尖压在技能图标上，实测偏差 0.6px）+ **技能槽图标脉冲高亮**（1.000↔1.120）。
5. 玩家点技能 → 技能放出 → 敌人的这一下被打断（`attackInterrupted=True`）。
6. **立刻恢复正常战斗**：出招闸门解除、防御键可用、敌人回正常节奏。

等待抡招期间玩家不是「站着挨打」的状态：这一拍**解锁防御键**，能格挡。

### 13.2 六块改动的最终状态

| 诉求 | 状态 | 关键数据 |
|---|---|---|
| 引导完成后不消失（残留） | ✅ | 循环示范每轮问宿主要不要继续；`Hide()` 停协程；420 次采样残留 0 |
| 霸体防御拍时机 | ✅ | 抬手做完（剩 ≤1.5s）才冻结，冻结在举刀帧，伤害被吸收 1 次 |
| 常驻引导与光圈错位 | ✅ | 指尖→圈心 0px（改相对偏移，删逐帧补偿） |
| 按压手势分工 | ✅ | 指尖=定位锚点、掌心=旋转支点；掌心 124.8px 恒定不动 |
| 常驻指引随机 1 只 + 闪烁 | ✅ | 前排 2 只 → 只给 1 只；alpha 0.00~0.93（会灭） |
| 「用技能打断」整段教学 | ✅ | 冻结 → 手势压槽 0.6px → 槽位高亮 → 打断 True → 闸门恢复 |

### 13.3 这一轮踩到的三个坑（写给下一个人）

1. **场景里有两个 `FrontItemBar`**：`Instance` 是「先 Awake 的赢」→ 拿到的是没配槽位的那个 →
   技能槽查找全查空表 → 手势指引出不来。修法：`Awake()` 让**有槽位的优先当 Instance** + `ResolveForSkills()`。
2. **清理逻辑不要用 `StopHint()`**：`StopHint()` 会把当前教学拍一起 `StopCoroutine` 掉。
   「出口光标清理」原来调它 → 「用技能打断」那一拍在开拍同一帧被掐掉 → 冻结代码根本没执行。
   修法：路线光标单独用 `_routeHintRoutine` 管，清理只收它自己。
3. **真日志不在项目里**：`%LOCALAPPDATA%/Tuanjie/Editor/Editor.log`（50MB 滚动）；
   项目里的 `Logs/Editor.log` 是旧文件，查它会白忙。

### 13.4 自查日志格式（下次出问题先看这几行）

```
[Tutorial][技能拍] ① 等玩家选完技能：等了 2.0s，选中=active_wave
[Tutorial][技能拍] ② 开拍（暂时禁出招，等它抡霸体招）
[Tutorial][技能拍] WAIT-C 敌人=…/Attacking 抡招=True C技=True 剩余=1.50s 阈值=1.50s 技能可用=True
[Tutorial][技能拍] ③ 已冻结：敌人抬起到顶（C技）→ 点技能槽打断它
[Tutorial] 技能打断拍：敌人抬手完成（剩余 1.50s，C技）→ 已锁住动作，等待玩家点技能槽
[Tutorial] 技能打断拍：收拍（打断=True 挂起=True 卡肉=True 已放技能=True）
```

看不到 ① = 玩家没选卡；没有 ② = 拍没开；`WAIT-C` 的「剩余」一直 > 阈值 = 敌人还没抡到点；
「技能可用=False」= 玩家提前放了技能，等冷却即可；
另外 `[Tutorial] StopHint：中断当前手势拍` / `OnDisable：教学导演被禁用` 后面带**完整调用栈**——谁掐的一目了然。

### 13.5 提交注意

- 本条线所有改动**未提交**；`Battle.scene` 也被改过（`tapPressAngle` 8、组件字段）。
- 自动化跑测会改存档：跑前备份 `player_save`（`Library/Locus/tmp/player_save_backup.txt`），跑完还原（本轮已还原）。
- 驱动 `TutorialFlowDriver` 的约定：技能拍期间不出招、只在冻结时放技能；重跑教学必须**退出再进 Play Mode**。

---

## 14. 第十二轮（2026-11-05）：「拍弹出」修复 + **退出教学关回主菜单的架构补漏**

> 用户反馈：「有报错」→ `MissingReferenceException: TutorialGestureHint has been destroyed`。
> 用户判断：「似乎没有考虑到教程时退出返回主菜单的问题，架构考虑的不周到」→ 本节记录那半边架构。

### 14.1 本轮做的两件事

| # | 事项 | 结果 |
|---|---|---|
| **A** | **「用技能打断」这一拍一句台词都没有** | ✅ 新增 `TutorialGestureStep.freezeDialogue`（**冻结瞬间**的台词，与冻结时的手势示范同时上屏）；`afterDialogue` 改到 `EndBeat()` 统一播（每一拍都能播）；冻结时临时开 `DialogueManager.AllowDialogueWhilePaused`（`timeScale=0` 时对话层会拦队列）；台词在播时不再挡技能槽点击 |
| **B** | **退出教学关回主菜单必崩 + 状态泄漏** | ✅ 见 §14.2 / §14.3 |

### 14.2 根因（两层，都不是「少写一行」）

1. **`?.` 用在 `UnityEngine.Object` 上是错的**：`gestureHint?.Hide()` 走**引用 null** 判断，
   而 Unity 的「假 null」（对象已 Destroy、引用还在）挡不住 → `LoadScene` 重载时提示层先被销毁 →
   照样调 → 内部 `StopCoroutine` 抛异常。
   → **凡是 Unity 对象都要用 `if (obj != null)`**（Unity 语义）或专门的 safe 方法。
2. **清理是一条没有容错的直线**：`OnDisable` 里「退订 → 收拍 → 清白名单/三选一开关/输入闸门」，
   任何一步抛异常 → 后面全不跑。而漏掉的全是**活得比场景长**的状态（static / 全局）：
   - `TutorialAttackGate` 白名单 → **正式关里玩家一个招式都放不出**（白名单 = 已教完的招式）
   - `GameplayInputGate.AllowSkillActivationWhileBlocked / ...WhileFrozen`（static）
   - `DialogueManager.AllowDialogueWhilePaused`（static）
   - `Time.timeScale` → 三选一面板开着退出时残留 0 → **主菜单点不动**
   - `UpgradeChoiceManager.suppressChoices / forcedChoices` → 正式关的三选一被锁死在教学池

### 14.3 修法（架构级）

- 导演新增 **`ReleaseGlobalTutorialState()`** —— 教学占用过的**全局/静态状态唯一出口**（幂等）。
- `OnDisable` 重排为 **① 回收全局状态 → ② 退订事件 → ③ 收拍**，每步独立 `try/catch`（`SafeStep`）：
  **任何一步抛异常都不许挡住后面的步骤**；全局状态排在最前，因为它最危险（static）。
- `TutorialGestureHint.StopRoutine / Cancel`：已销毁时不再调 `StopCoroutine`。
- `UpgradeChoiceManager`：记 `_pausedByUs` → `OnDestroy` 把**自己**暂停的时间还回去。

### 14.4 验证口径（Play Mode 实跑）

| 场景 | 期望 | 实测 |
|---|---|---|
| 教学关跑通 | 全流程 + 冻结台词上屏 | ✅ 台词顺序全对，冻结瞬间气泡出「用技能打断它——点下面的技能图标！」 |
| 跑到「白名单=Stab」→ 中途回主菜单 | 白名单/开关/timeScale 全干净 | ✅ 白名单随路线切换正确放开（切正式关 = `不限制`），四个 static 开关全 False，timeScale=1 |
| **三选一面板开着**回主菜单 | 主菜单不被冻住 | ✅ timeScale=1 |
| Console | 0 error | ✅ 0 error / 0 warn |

### 14.5 写给下一个人的三条

1. **`?.` 不能用在 `UnityEngine.Object` 上**（含「已 Destroy 但引用还在」的假 null）。
2. **凡是 static / 单例上的状态，退出路径必须有一个「唯一回收口」**，回收要**幂等 + 分步容错**。
3. **场景重载时对象销毁顺序不定** → 清理代码不能假设「我引用的对象还活着」。

> 细节与完整根因见 `memory/known-issues.md` 第 13 条。

---

## 15. 第十三轮（2026-11-05）：「按防御 → 教程卡死」+ 每教完一拍就写存档

> 用户反馈两条：
> ① 「出现按防御会导致教程卡死的问题（不是说要教程 mute 到防御按键吗？）」
> ② 「我已完成点戳的教程，就应该在存档里标记完成，而不是根据关卡通过来判断（教程关除外）；
>    所有非教程关教程都应该这样」

### 15.1 卡死的根因与修法

**根因**：`AttackSystem` 有硬闸「**防御中，不能出招**」→ 玩家在**需要出招的教学拍**里按住防御
→ 招式永远打不出来 → **那一拍收不了 → 教程卡死**。

**修法**：新增 `BeatNeedsAttack(kind)` —— **需要出招的拍**（Tap / TapCombo / ComboCharge）
期间**静音防御键**；例外不静音：
- `DefendButton` 拍 = 教玩家按防御键 → 必须可用
- `SkillSlot` 拍 = 玩家点技能槽，不靠出招；且等敌人抡招时玩家要能防御（2026-11-02 反馈）

> 之前的静音只盖了 ComboCharge 的**阶段 2**（蓄力窗口），阶段 1 没盖 → 阶段 1 按防御一样卡死。
> 现在盖**整拍**（教学拍期间敌人是冻住的 → 不会让玩家挨打）。
> 静音源从 2 个变成 2 个（`_defenseMutedByChoice` 教学专用 / `_defenseMutedByBeat` 出招拍），
> `EndBeat` 会解除后者（否则两拍之间防御键一直是死的）。

### 15.2 每教完一拍就写存档（不看关卡通不通）

- 新增存档字段 `SaveData.taughtTutorialBeats`（List<string>，旧档由 `TutorialSaveSection.Migrate` 补空列表）。
- 教学关的每一拍教完就写 id（形态 `kind@waveIndex`），3选1 教学用 `Choice3@0`；
  **只有玩家真的做到才写**（`_beatCompleted`，防御拍在 `OnGuardAbsorbed` 里置位）。
- 再进教学关时，**已教过的拍直接跳过**。
  ⚠️ 跳过时必须把「这一拍教出来的东西」补上（`ApplyBeatOutcome`：招式白名单 + 防御键开放），
  否则教学关里玩家打不出招 → 教学关直接卡住。
- 引导拍（招式解锁）另有存档位 `MoveGuideSaveSection.taughtMoveGuides`（本来就是 per-tutorial ✓）。

### 15.3 实测（三轮）

| 轮次 | 结果 |
|---|---|
| 第 1 轮（从零教） | 写入 5 个标记：`Tap@0` / `TapCombo@1` / `DefendButton@0` / `Choice3@0` / `SkillSlot@1`（引导拍 `talent_move_stabc2` 也写入了 `taughtMoveGuides`） |
| 第 2 轮（再进） | 4 个拍全部「已教过（存档标记）→ 跳过」；3选1 教学也跳过（但**三选一照常弹**）；**教学关仍然通关**；招式白名单 = `Stab`（跳过时补齐 ✓） |
| 静音行为 | Tap / TapCombo / ComboCharge 进行中 → `Muted=True`；DefendButton / SkillSlot 进行中 → `Muted=False`；拍与拍之间 → 自动恢复 |

Console 0 error / 0 warn；存档已逐字节还原。

