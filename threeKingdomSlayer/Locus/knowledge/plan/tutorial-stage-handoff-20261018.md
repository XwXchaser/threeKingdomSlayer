---
id: kd_66dac704-1e39-4362-852c-fb2956ad141a
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 教学关（新手教学）· 交接（2026-10-18）

> 本文是**换一个对话接手**的入口。上一个会话在实现过程中反复走弯路，**先读 §6「走过的弯路」再动手**，
> 那里写着哪些现成系统不该重写、哪些做法已被用户否决。

## 0. 一句话现状

教学关**还没做完、不可验收**。已有：独立教学场景、4 波内容、N1→E0 的一段卷轴推进、禁充能、天赋树框架。
缺：**节点流程与结算**（应该直接复用正式关卡那套，而不是自己写）、阵型间距（用户认为仍不对）、
冰系机制拆天赋（代码已改未验证）、固定三选一（代码已改未验证）。

## 1. 任务目标（用户定义的完整教学流程）

```
删档 → 新游戏 → 教学关（只出现一次）
  ├ N1：两波敌人，教「点戳」「斩击」；获得一次升级，三选一固定给冰雾（只冻结，无解冻/击退/翻倍）
  ├ 走到 E0（有节点移动表现）
  ├ E0 波1：1021，教「蓄力上挑（launch）」破盾
  ├ E0 波2：109，冲锋高伤；可打赢（5 套枪突链）也可能被打死
  └ 结算（两个出口都过关）
→ 主菜单：给铜钱 → 天赋树点出「C2 蓄力戳击」「重击解冻」
→ 正式 YJ 关卡 N1：敌人部署与教学关第一波相同（不加台词），验证玩家会用新招式
```

- 教学只出一次（`SaveData.tutorialCompleted` 已实现）。
- 玩家教学期技能**固定**；正式游玩时三选一是随机的。
- 教学关里**无法完成充能**（所以教不到终结技），终结技在正式关卡教。
- C2、C4 两个连段在首轮**锁住**；`Launch`（上划挑飞，`AttackType.Launch`）**不是** C4，默认可用。
- N1–N3 正式关卡的数值/配置**另开议题**，不在本次范围。

## 2. 用户已拍板的数值（1011 = 100 血为基准）

| 项 | 值 | 依据 |
|---|---|---|
| 1011 血量 | 100 | 基准 |
| 1021 血量 | 150 | 1.5 × 1011（减伤未定） |
| 109 血量 | 500 | 5 套枪突链 |
| 枪突三连 | 30 / 33 / 37 | 合计 100，递增；三下刚好杀 1011 |
| 斩击 damage | 20 | 1011 的 20% |
| 终结技（`Zhangfei_LaunchFinisher`） | 1000 | 等于两个 109 的血 |
| 109 冲锋伤害 | 约 40~60 | 高但可救；导演里 `lethalChargeDamage = 20`（× 冲锋格数） |

已在本仓库资产里改好（见 §3 提交 `2b11bd9`）。其余招式伤害未动。

## 3. 已提交内容

| 提交 | 内容 |
|---|---|
| `8d5f77f` | 教学关雏形：`Assets/Scenes/Tutorial/Tutorial_YJ.unity`（YJ 拷贝 + 战斗栈）、`Tutorial_Battle`、张飞台词 5 条、109 演出、`tutorialCompleted` 收尾、主菜单入口 |
| `c478fa1` | 教学关专用阵型 `Formation_Tutorial`（**用户认为间距仍不对**） |
| `2b11bd9` | 数值重定、四波重排、N1→E0 推进、禁充能、109 改为真实敌人 |

## 4. 未提交的改动（可能未验证，接手先看再决定留不留）

| 文件 | 改了什么 | 状态 |
|---|---|---|
| `Assets/Scripts/Core/ElementalBuffController.cs` | 冰系破冰能力按天赋节点 id 开闸：`talent_ice_thaw`（解冻）/ `talent_ice_break_push`（击退+碰撞）/ `talent_ice_break_double`（翻倍） | 编译通过，**未实机验证** |
| `Assets/ScriptableObjects/Talents/TalentTree_Main.asset` | 新增上述 3 个节点（链式顺序插入在第 2~4 位） | 未验证 |
| `Assets/Scripts/Core/UpgradeChoiceManager.cs` | 新增 `forcedChoices`（固定候选集，填 1 个即强制） | 未验证 |
| `Assets/Scripts/Tutorial/TutorialStageDirector.cs` | 用 `forcedChoices` 替代 `suppressChoices`；新增 `tutorialIceSkill` 字段 | 未验证 |
| `Assets/Scenes/Tutorial/Tutorial_YJ.unity` | 导演接线：`tutorialIceSkill` = `ActiveSkill_IceMist` | 未验证 |

> 注意：一次「验证脚本空引用中断」后就没有再验过这五项，**不要当它们已经工作**。

## 5. 已实测通过 / 未验证

**已实测（Play Mode）**
- 教学关独立开局：`stageConfig = Tutorial_Battle`、状态 InProgress、第 1 波出怪
- 点戳能掉血（60 → 40，当时数值）
- 清完第 1、2 波 → 卷轴 progress 0 → 0.228 → 0.2768 → 第 3 波 1021（hp 150）在 E0 出生
- 台词自动弹出并暂停（`Time.timeScale = 0`）
- 三选一曾把波次卡死（已用开关绕开）

**未验证**
- 新数值下「三下杀 1011」「5 套杀 109」「1021 上挑破盾」
- 第 4 波 109 的完整战斗与两个出口
- 冰系天赋开闸（冻结后重击是否真的不解冻）
- 固定三选一是否真的只弹冰雾、选完是否拿到技能

## 6. 走过的弯路（**接手务必先读**）

1. **别自己写节点流程。** 我第一次把教学做成“独立场景 + 自己推卷轴 progress”。用户明确说：现有案例里**节点移动、间距、结算都是现成的**，不该新写。正确的是让教学关走 `FakeRouteRuntime` 那条流程（进入节点 → 战斗 → 奖励 → 旅行 → 结算），结算/存档/旅行全部现成。
2. **别用静态测量定阵型间距。** 我用精灵包围盒算宽度（含透明边，5.76）→ 定 2.6 列间距；用户仍认为“挤在一起”。视觉参数应该**照抄正式关卡正在用的阵型资产**，或由用户给参照，不要用几何量推算。
3. **别在没验证前叠功能。** 数值、阵型、行进、天赋、三选一一起改，出错时无法定位，用户每次看到的都是不同的问题。
4. **验证脚本要写成“不炸”的形式。** 临时脚本常因 `DialogueManager.Instance` / `EnemyManager.Instance` 在开战前为 null 而中断，一中断就以为完事。凡是跨帧读取，先判空再读。
5. **`SetState(同状态)` 必须仍然同步 playerState**（已在 `8d5f77f` 修）。否则玩家状态停在 `Starting`，攻击/技能全被挡。
6. **非路线关卡在 Battle.scene 里不会自动开战**（`FakeRouteRuntime.Awake` 会关掉 `autoStartStage`）。
7. **线性关卡启动要清 `routeStageConfigV2`**（已在 `8d5f77f` 修），否则被 V2 分支截走、关卡永远起不来。
8. **不要让三选一在教学关里暂停卡波**（已加 `suppressChoices` / `forcedChoices`）。

## 7. 下一步建议（按这个顺序，一次只做一件并验证）

1. **决定骨架**：教学关改成走正式路线流程（推荐）还是继续独立场景。
   - 若走正式路线：新建一条 `FakeRouteStageConfig`（教学专用），节点 N1、E0 指向教学战斗配置，旅行/选择/结算用现有资产。
   - 若保持独立场景：必须自己补「节点进入 → 完成判定 → 结算」，工作量大且与正式关卡分叉。
2. **阵型**：直接用正式关卡 N1 现在的 `formationConfig`；若它也不对，请用户给一个“看着对”的参照再填。
3. **冰系天赋开闸**：验证 §4 那三处改动（冻结后重击不解冻；点亮 `talent_ice_thaw` 后能解冻）。
4. **固定三选一**：验证只弹冰雾、选完 `ActiveSkillInventory` 里有冰雾、且冰雾只冻结不解冻。
5. **109 两个出口**：打赢 → 结算；被打死 → 不弹战败面板 → 同样结算。
6. **结算 → 铜钱 → 天赋树引导 → 回正式 N1 复习波**。

## 8. 文件索引

| 内容 | 路径 |
|---|---|
| 教学场景 | `Assets/Scenes/Tutorial/Tutorial_YJ.unity` |
| 教学战斗配置 | `Assets/RouteData/Tutorial/Tutorial_Battle.asset`（stageId 9000，4 波） |
| 教学阵型（用户认为不对） | `Assets/RouteData/Tutorial/Formation_Tutorial.asset` |
| 教学导演 | `Assets/Scripts/Tutorial/TutorialStageDirector.cs` |
| 台词 | `Assets/ScriptableObjects/Dialogue/tutorial_t0_wave1..4.asset`、`tutorial_t0_death.asset` |
| 天赋树框架 | `Assets/Scripts/Core/TalentTreeConfig.cs` / `TalentTreeState.cs` / `UI/TalentTreePanel.cs` |
| 天赋树配置 | `Assets/ScriptableObjects/Talents/TalentTree_Main.asset` |
| 冰系破冰 | `Assets/Scripts/Core/ElementalBuffController.cs`（`IceBreakRuntime`） |
| 1021 盾规则 | `Locus/knowledge/design/enemy-1021-shield-guard.md`（`DamageType.Launch` 无视盾侧直接破盾） |
| 招式资产 | `Assets/ScriptableObjects/Moves/Zhangfei/` + `Assets/Prefabs/UI/Skills/` |

## 9. 给接手者的一句提醒

这个会话里用户的耐心已经被消耗很多。汇报用三段：**做了什么 / 现在能不能玩 / 下一步做什么**，
不要讲类名和字段；有不确定就先问一句再动手；宁可少做一步，也不要叠着未验证的改动往前推。

## 10. 迁移进展（2026-10-19）：已按「走正式路线流程」改完并实测

§7 第 1 步的骨架已经拍定并落地：教学关走 `FakeRouteRuntime` 那条正式卷轴流程，`Tutorial_YJ.unity` 保留但不再使用。

**现在的流程**：主菜单新档 → `Battle.scene` → 教学路线 → N1（1011 两波）→ 出口旅行 → E0（1021 + 109，终点）→ 整关结算 → 回主菜单点天赋。

**新增资产**（`Assets/RouteData/Tutorial/`）

| 资产 | 内容 |
|---|---|
| `Tutorial_N1_Battle.asset` | stageId 9001，两波 1011（原 4 波配置的前两波） |
| `Tutorial_E0_Battle.asset` | stageId 9002，1021 + 109（原后两波） |
| `Tutorial_Node_N1.asset` / `Tutorial_Node_E0.asset` | nodeId 直接叫 `N1` / `E0`，复用 YJ 路线场景已有锚点；E0 为终点、只有一条战斗 |
| `Tutorial_Choice_N1_E0.asset` | 旅行表现沿用正式 `Choice_N1_E0` |
| `Tutorial_Route.asset` | routeId `tutorial_route`、stageId 9100、整关铜钱 250 |

阵型与补齐一律照抄正式 YJ 战：`DefaultFormation` + `Per Column`、`rushMoveDelay 0.05`；教学敌人排布（1011 列 0/2/4、1021 列 2、109 单只）保持原样未改。

**代码改动**

1. `YScrollSample.TryGetNodeProgress`：精确资产匹配失败时按 `nodeId` 兜底（场景锚点绑的是正式节点资产，新教学节点没有自己的锚点；同 id 多处仍判歧义）。
2. `TutorialStageDirector` 重写为 `Battle.scene` 上的常驻组件（对象名 `Tutorial Director`），只负责：按波推台词、教学期禁充能、N1 固定给冰雾（E0 不放三选一）、最后一波阵亡按通过收尾。
3. `StageController` 新增 `defeatCountsAsClear` 与 `CompleteRouteBattleAsScriptedEnding()`；脚本化阵亡不再调 `HandleStageDefeat`，路线继续走奖励等待与结算。
4. `MainMenuUI` 用 `tutorialRouteConfig` 取代旧的 `tutorialStageConfig` / `tutorialSceneName`（两者已从类里删除）。

**踩到的坑（接手别再踩）**：`PlayerState.HandleDeath` 会先把玩家状态置 `Defeat`，`StageController` 只镜像状态、不再发 `OnStageDefeat`，所以教学导演必须同时订阅 `PlayerState.OnPlayerDied`，否则收尾永远不跑。

**已实测（Play Mode，程序化驱动）**：新档 → N1 wave0（1011 hp100）→ wave1（1011×3）→ 三选一只有冰雾（选中 `active_ice_mist`）→ 选出口 → 旅行 → E0 wave0（1021 hp150）→ wave1（109 hp500，阵亡按通过=true）→ 玩家阵亡 → 收尾 → 路线 Completed / Victory / `tutorialCompleted=true`。禁充能在教学战斗里为 true；两场都是 DefaultFormation + Per Column。

**未验证 / 待做**

- 打赢 109 的出口没单独跑（和阵亡出口同走 `FinishRoute`）。
- 1021 上挑破盾（`Launch` 无视盾）这个教学点没验证。
- 冰系天赋三处开闸、固定三选一之后的「冰雾只冻结不解冻」仍未实机确认。
- C2/C4 锁定仍未实现：天赋树有节点，代码里没有查询点。
- `Tutorial_Battle.asset`（原 4 波配置）已被两份新配置取代，未删除，等用户决定。
- 教学结算铜钱：固定 250 + 击杀铜钱，实测到手 300。

## 11. 手势引导系统（2026-10-19 晚，已跑通）

> 本文 §10 的两条描述已被本轮推翻：**教学期不再禁充能**（禁充能会把上挑一起禁死），
> **阵亡也不再“按通过”**（阵亡就是失败，弹失败面板）。

### 11.1 现在的教学形态

台词自动推进 → 敌人站定（按需等血量）→ **暂停 + 黑遮罩 + 手势示范** → 玩家真做出该手势 → 重放这一下（真的出招）→ 放完台词 → 继续。

### 11.2 组件与资产

| 内容 | 路径 |
|---|---|
| 手势提示组件 | `Assets/Scripts/UI/TutorialGestureHint.cs`（手指/拖尾/光圈/遮罩四层 + 暂停门槛 + 原始输入判定 + 重放） |
| 提示预制体 | `Assets/Prefabs/UI/TutorialGestureHint.prefab`（自带 Canvas 60 层，全层 raycastTarget=false） |
| 三张图 | `Assets/Sprites/UI/Tutorial/tutorial_gesture_{finger,swipe_trail,hold_ring}_ui.png`（256²，手指的 **RectTransform pivot=(0.2637,0.8066)= 指尖**） |
| 步骤配置 | `Battle.scene` 的 `Tutorial Director.gestureSteps`（`TutorialGestureStep[]`） |
| 台词 | `tutorial_t0_open`（开幕）、`tutorial_t0_wave1/2/3/4`（每波）、`tutorial_t0_choice`（出口箭头） |

### 11.3 当前 5 个步骤（配置权威）

| # | 波次 | 手势 | 条件 | 备注 |
|---|---|---|---|---|
| 0 | N1 第 1 波 | Tap 点按 | 敌人站定 | 教点戳 |
| 1 | N1 第 2 波 | Swipe 横划（0.28→0.72） | 敌人站定 | 教「一刀扫多个」 |
| 2 | E0 第 1 波（1021） | ChargedSwipe 蓄力上挑 | **血量 ≤80%** | 不显示光圈 |
| 3 | E0 第 2 波（109） | ChargedSwipe | **血量 ≤80%** | 同上 |
| 4 | 路线出口（waveIndex=-2） | Tap | 箭头出现 | 教点箭头移动 |

### 11.4 实测数据（`unity_run_states` 程序化驱动）

```
引导#1 点按      引导#2 横划      引导#3 血比=80%    引导#4 血比=80%
引导中：遮罩=True、目标列=2、手指=显示
通过后：遮罩关上、hp 100→80（重放真的打中）、招式=Zhangfei_Stab
结束：路线=Completed、关卡=Victory、教学完成=True、铜钱=620（250+击杀）
```

### 11.5 本轮新增的坑（详细版见 `memory/tutorial-gesture-gate-pitfalls.md`）

1. **构建链路**：`unity_recompile` 报 compiled 但 DLL 没更新 → 游戏跑旧包。唯一判据是 DLL mtime 晚于源码 mtime。
2. **编辑丢行**：编辑工具报成功、目标代码却没进文件（遮罩 `SetActive(true)` 就这样丢过一次）。改完必须 grep 回读。
3. **UI 轴心**：改图片的 sprite pivot 不会影响 UI 定位，**RectTransform 的 pivot 才决定锚点**；手指要指哪就把 RectTransform pivot 改到指尖。
4. **自己取消自己**：示范循环每轮调 `Cancel()` 会把引导本身取消 → 播放用 `StopRoutine()`。
5. **对话顶掉暂停**：`DialogueManager.Finish()` 无条件 `timeScale=1`，门槛期间需每帧压住 0。
6. **遮罩叠加**：台词自己有一层压暗，台词在播时不要再叠我们的遮罩。
7. **重放要绕动作锁**：`timeScale=0` 时动作锁不走时，重放必须 `cancelCurrentMove:true`；列号直接用目标敌人的 `columnIndex`，别用屏幕坐标反算（透视会偏）。
8. **入场期间不能暂停**：敌人从后排走进来的时候暂停，敌人永远到不了前排；台词必须等敌人站定后再推。

> **本轮之后的总交接看 `plan/tutorial-and-formal-stage-handoff-20261019.md`**
> （包含完整进度、已实测数据、待做项、坑点与下一步顺序）。
