---
id: kd_fd5bd710-1fbc-4ab3-84e5-d6cf88d23e18
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 本轮总交接：教学关手势引导 + 正式关卡进度（2026-10-19）

> 换对话接手先读这一页，再按 §7 的文件索引去看细节。
> 细节文档：`plan/tutorial-stage-handoff-20261018.md`（教学迁移全过程）、
> `memory/tutorial-gesture-gate-pitfalls.md`（坑点详版）、
> `plan/breakable-props-art-handoff-20261019.md`（可破坏道具，交给别的 AI 做美术）。

## 0. 一句话现状

**教学关已经能完整跑通并验收过前半段；正式关卡目前只有骨架（2~3 分钟内容），离 10 分钟差得远。**

## 1. 本轮已完成

### 1.1 教学关（可玩、已实机验收关键节点）

- 走正式卷轴路线：主菜单新档 → `Battle.scene` → 教学路线 → N1 → 出口旅行 → E0 → 结算 → 回主菜单。
- **手势引导系统**（本轮核心）：暂停 + 黑遮罩 + 手势示范（手指/拖尾，手指轴心=指尖）→ 玩家真做出手势 → 重放这一下（真的出招）→ 放完台词 → 继续。
- 台词：开幕 `tutorial_t0_open`、每波 `tutorial_t0_wave1..4`、出口箭头 `tutorial_t0_choice`；台词自动推进（不拦玩家），波次台词等敌人站定后再推。
- 5 个教学步骤（配置在 `Battle.scene` 的 `Tutorial Director.gestureSteps`）：

| # | 波次 | 手势 | 触发 |
|---|---|---|---|
| 0 | N1 第 1 波 | 点按 | 敌人站定 |
| 1 | N1 第 2 波 | 横向滑动 | 敌人站定 |
| 2 | E0 第 1 波（1021） | 蓄力上挑（无光圈） | 血量 ≤80% |
| 3 | E0 第 2 波（109） | 蓄力上挑（无光圈） | 血量 ≤80% |
| 4 | 路线出口 | 点按 | 箭头出现 |

- 教学数值：1011 = 100 血、1021 = 150、109 = 500（冲锋 20×格数、去掉 50% 常态减伤）。
- 教学期三选一固定给冰雾；E0 不放三选一。
- 阵亡 = 失败（弹失败面板，不再"按通过"）；E0 是存档点，输了从 E0 重来。
- 结算：教学完成标记 + 铜钱 250 + 击杀铜钱（实测一局 620）。
- 主菜单「继续」在教学未通关时优先回教学关（之前会掉进 YJ）。

### 1.2 局外成长 / 招式解锁

- C2（指向突刺）、C4（枪突收尾）**未解锁时连蓄力都进不去**，且不偷偷回落成别的招式。
- **Parry 本轮新改为解锁招式**：天赋节点 `talent_unlock_parry`（100 铜钱），未解锁时竖滑不蓄力不出招。
- 冰系三节点（解冻/击退/翻倍）按天赋开闸；冰雾在没点「重击解冻」时只冻结、不给破冰预备。

### 1.3 其他

- 蓄力招式全局收窄：只保留竖滑蓄力（上挑），站点蓄力（穿刺）与横滑蓄力（横扫）关闭，蓄力指示器与蓄力武器视觉不出现（`InputManager.restrictChargedToVertical`，开关式，后续做完可关）。
- 正式路线 J1 的右路出口已删除，现在 **N1 → E0 → J1 → EV → N3**；N2 不可达（资产保留）。

### 1.4 教学手势拍重构（取代旧的暂停门槛）

旧做法：`timeScale=0` + `gameplayInputEnabled=false`，玩家在冻结世界里做手势，松手后 `ReplayGesture` 重播一招。
后果：手势没有蓄力反馈（看起来慢一拍）、重播绕过招式状态机、完成时黑幕还要等台词，玩家看不到自己打出了什么。

新做法（用户拍板：敌人冻结、玩家只能做目标手势、只压暗场景、不提供跳过/失败提示）：

1. **开拍**：所有存活敌人置 `Enemy.tutorialHold = true`（不进攻、不移动，受击/动画照常）→ 场景压暗 → `InputManager.tutorialGestureTarget` 设为这一拍的目标手势 → 循环示范。
2. **输入**：不暂停、不断输入。`InputManager` 只放行目标手势，其它手势不成立、也不消耗蓄力段。
3. **收拍**：监听 `InputManager.OnAttackExecuted`，玩家真的打出对应招式（Tap→Stab / 横划→Slash / 蓄力竖划→Launch）立刻收场：示范消失、压暗 0.12s 淡出、台词立即关掉、敌人恢复行动。
4. **压暗实现**：`Assets/Scripts/Tutorial/TutorialSceneDim.cs`。只给路线场景（背景/地形/植被/墙体）乘亮度，角色、长枪、敌人一律不动：
   - SpriteRenderer 走 `color`；MeshRenderer 走 `MaterialPropertyBlock` 的 `_Color`；
   - 路面网格用的是无颜色属性的自定义 shader，已给 `YGroundJ1Branches.shader` 加 `_TutorialDim`（默认 1，只影响教学拍）；
   - 天空是程序化 Skybox，压暗时临时换成材质实例并调 `_SkyTint`/`_GroundColor`，结束时还原引用。
5. **出口箭头那一拍**（waveIndex<0）：仍未暂停门槛——台词 `nonBlocking`（不暂停/不压暗/不挡点击），只在真实路线按钮上循环点按光标，选完路自动收起。

实测（Play Mode，程序化驱动）：

```
点按拍 open/收场：dimming=True（地面 _TutorialDim=0.42、skyTint=0.21）→ 敌人 held=True 且 color=1,1,1
收场后：dimming=False、skyTint 回 0.50、地面 _TutorialDim 回 1.0、held=False、timeScale=1
亮度实测：整屏 0.78（路面段 103.6→55.5 = 0.54，角色段基本不变）
横划拍、上挑拍同样开/收正常；上挑后 1021 formState=Normal→Coward（破盾成功）
1021 正面 30 伤 → 实扣 20（−10 固定减伤）；反侧 30 → 全额 30
```

## 2. 已实测的数据（Play Mode，程序化驱动）

```
引导#1 点按 / 引导#2 横划 / 引导#3 血比=80% / 引导#4 血比=80%
引导中：遮罩=True、目标列=2、手指=显示
通过后：遮罩关闭、敌人 100→80（重放真的打中）、招式=Zhangfei_Stab
结算：路线=Completed、关卡=Victory、教学完成=True、铜钱=620
```

1021 上挑破盾已由用户实机确认可行。

## 3. 未验证 / 待做

1. **正式关卡内容量**：现在 N1/N2/N3 共用 `N1_ArtPreview_Battle`（1 波 3 敌），E0 用 `E0_Battle_Preview`（1 波 1 敌），J1/EV 无战斗 → 全程 2~3 分钟。目标 ≥10 分钟。
   建议配置（未实施，等拍板）：N1 4×5、E0 3×5、J1 4×6、EV 3×6、N2 4×6、N3 5×7 ≈ 136 敌 ≈ 9~10 分钟；一局约 700 铜钱，天赋 100~400 → 一局点 2~3 个节点。
2. **EV 可破坏补给（酒缸/木箱）**：美术交给别的 AI（见 `plan/breakable-props-art-handoff-20261019.md`）；`BreakableProp` 组件、摆位、高亮、奖励结算都还没写。
3. **正式关卡的教学**：蓄力、连段、Parry（解锁后第一次用）都还没有对应的教学节拍。
4. 出口箭头那句台词的实机可见性没单独确认过。
5. 教学完成 → 回主菜单点天赋 → 新游戏进 YJ 这条闭环没整条跑过。

## 4. 本轮最大的坑（详细版见 `memory/tutorial-gesture-gate-pitfalls.md`）

1. **`unity_recompile` 会假成功**：报 compiled，但 `Library/ScriptAssemblies/Assembly-CSharp.dll` 的 mtime 没变，游戏跑旧包。**唯一判据是 DLL mtime 晚于源码 mtime**；必要时先 touch 源文件再编译。本轮因此连续多次"修好了但没变化"。
2. **编辑工具会丢改动**：报成功、文件里却没有目标代码（遮罩 `SetActive(true)` 就丢过一次）。改完必须 grep 回读。
3. **编译错误会挡住 Play**：表现为"开不了游戏"；而且 Console 里的报错可能是上一轮的，要看 DLL 时间或重新编译。
4. **UI 轴心**：改图片 sprite pivot 不影响 UI 定位，**RectTransform 的 pivot 才是锚点**；要让手指指到哪，就把 RectTransform pivot 改到指尖。
5. **引导自己取消自己**：示范循环里调 `Cancel()` 会把引导一起取消 → 播放用 `StopRoutine()`。
6. **对话顶掉暂停**：`DialogueManager.Finish()` 无条件 `timeScale=1`，门槛期间要每帧压住 0。
7. **遮罩别叠加**：台词自己有压暗，台词在播时不要再叠我们的遮罩，否则后面几段明显更黑。
8. **重放要绕动作锁**：`timeScale=0` 时动作锁不走时，重放必须 `cancelCurrentMove:true`；列号直接用目标敌人的 `columnIndex`，别用屏幕坐标反算（透视会偏）。
9. **入场期间不能暂停**：敌人从后排走进来时暂停，敌人永远到不了前排；台词与引导都必须等敌人站定。
10. **美术风格**：本轮两次生图都因提示词写成"厚涂偏写实"而报废。本项目的画风词汇是"阶梯状轮廓 / 成簇明暗色块 / 暖土冷铁 / 边缘干净"，写提示词前先读 `design/art-style-guide.md`。

## 5. 建议的下一步顺序

1. **正式关卡配置**：按 §3.1 的波次表建 6 份 StageConfig，替换节点战斗配置，跑自动验证（总敌人数/时长/铜钱）。
2. **难度与养成闭环**：确认"第一次打不过、靠局外变强"所需的敌人血量倍率与铜钱产出。
3. **正式关卡教学**：蓄力 → 连段 → 解锁后的 C2/C4/Parry，各排一个首次教学节拍（可复用现有 `TutorialGestureStep` 机制）。
4. **可破坏补给**：等美术到位后写 `BreakableProp` + EV 摆位 + 高亮。
5. 补 §3.4 / §3.5 的两处验收。

## 6. 关键文件索引

| 内容 | 路径 |
|---|---|
| 教学导演 | `Assets/Scripts/Tutorial/TutorialStageDirector.cs` |
| 手势提示组件 / 预制体 | `Assets/Scripts/UI/TutorialGestureHint.cs`、`Assets/Prefabs/UI/TutorialGestureHint.prefab` |
| 手势图 | `Assets/Sprites/UI/Tutorial/tutorial_gesture_{finger,swipe_trail,hold_ring}_ui.png` |
| 教学路线资产 | `Assets/RouteData/Tutorial/Tutorial_Route.asset`（节点/战斗/出口同目录） |
| 教学台词 | `Assets/ScriptableObjects/Dialogue/tutorial_t0_{open,wave1..4,choice,death}.asset` |
| 天赋树 | `Assets/ScriptableObjects/Talents/TalentTree_Main.asset`（10 节点，含 C2/C4/Parry/冰系三项） |
| 正式路线 | `Assets/RouteData/ChangbanpoScroll/Changbanpo_ScrollRoute.asset` |
| 招式数值 | `Assets/Prefabs/UI/Skills/Zhangfei_*.asset`（大招充能已降到 1~3/击） |
| 敌人数值 | `Assets/Resources/EnemyPrefabs/Enemy_109.prefab` 等 |
