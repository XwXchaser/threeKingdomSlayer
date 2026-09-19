---
id: kd_05fdb97a-d8dc-45f8-9e27-272984b65401
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 卷轴向开发交接：当前进度与后续方向

## 1. 下一对话先读这里

本文件是本次长对话结束时的交接入口。用户已对最后一轮修复回复“已确认”，要求整理进度后在其他对话继续。

- 当前分支：`experiment/curved-scroll-travel`，基于场景向旧 V2 基线。
- 当前主要测试场景：`Assets/Experiments/CurvedScroll/FakeRouteDataTrial.unity`。
- 实际启用的流程组件：`ScrollNodeFlowTrial`；不是同名的旧 `FakeRouteDataTrial`，也不是正式 FakeRouteRuntime。
- 旧 `FakeRouteDataTrial` 与 `ScrollCombatTrial` 在此场景禁用，不要重新同时启用。
- 目前成果是可运行的山谷卷轴＋统一节点测试，不是已完成的生产路线/存档系统。
- 本次未提交或推送；大量实验资产、RouteFake脚本和同步文档仍未跟踪。开始工作前重新检查Git状态与Editor现场，不覆盖用户修改。

旧交接存在历史过时或过度完成描述，以当前代码、最新用户确认和本文限定范围为准。特别不要把“测试Save/Get/Clear成功”当成完整恢复，也不要把“无效choiceId被拒绝”当成已验证回访限制。

## 2. 用户确认的方向

### 视觉

固定玩家和战斗坐标，通过卷轴环境迎面滚动表达旅行。近处保持平直，远处向下弯形成艺术化地平线；不是要求严格物理圆柱。

山谷场景：中央可调宽度道路，两侧独立地表与路肩，岩峰、岩石、树、灌木、草簇形成包围感，远景固定。景物同速随旅行进度移动，近远视差由透视产生，不让贴地景物各自滑动。

用户要求背景按2:3设计；历史Game截图多为1080×1920（9:16），已做800×1200离屏相机预览，但不等于完整2:3 Game/HUD适配验收。继续工作时必须核实实际画幅。

### 关卡与节点

1. 有分支，连接不同路线节点挑战。
2. 同一局正常沿路线不会返回已访问节点；合流允许，环路不允许。恢复当前存档节点不等于沿路线重访。
3. 每次抵达新节点，以及完成节点内容/奖励/选项确认时，应在安全边界保存。
4. 可有共同终点或多个不同终点，完成任一有效终点后整关通关。
5. 所有节点共用一种数据类型。战斗、对话、奖励都是可选内容；空battleEntries合法。不另造RewardNode/DialogueNode/JunctionNode或每节点Scene。
6. 战斗数据继续复用StageConfig和WaveConfig。旅行参数属于连接，环境不决定逻辑目标。
7. 节点有多场战斗时需在本次进入中完成编排，不能照搬视频分支“下次重访执行下一条”的语义。
8. 单出口也等待用户确认前进；战斗清空不等于奖励结束，不提前离开。

## 3. 当前资产与代码地图

### 实验场景

| 场景 | 用途 |
|---|---|
| `Assets/Experiments/CurvedScroll/CurvedScrollLab.unity` | 原始视觉对照：平直/曲率/远景 |
| `Assets/Experiments/CurvedScroll/ScrollCombatTrial.unity` | 早期单场战斗接入副本 |
| `Assets/Experiments/CurvedScroll/ValleyCombatTrial.unity` | 山谷美术接入及密度调整 |
| `Assets/Experiments/CurvedScroll/FakeRouteDataTrial.unity` | 当前统一节点流程入口 |

不要修改正式 `Assets/Scenes/Battle.scene` 来代替隔离实验修改。

### 表现层

- `Assets/Experiments/CurvedScroll/Scripts/CurvedScrollLab.cs`：CPU细分地面、下弯函数、旅行距离、调试模式。新增SetPresentationDistance用于外部流程精确驱动。
- `Assets/Experiments/CurvedScroll/Scripts/ScrollDepthLayers.cs`：早期几何占位分层；美术适配器运行时隐藏占位表现。
- `Assets/Experiments/CurvedScroll/Scripts/ValleyArtPresentation.cs`：图集装饰、路肩曲面网格、背景Canvas、地面材质参数。大景物32、小景物80；尺寸倍率1.3/.65；按Sprite宽度保留道路空间。
- `Assets/Experiments/CurvedScroll/Shaders/ValleyGround.shader`：道路/路侧两张纹理按世界尺度采样与边界过渡，宽度变化不应拉伸纹理。
- `Assets/Experiments/CurvedScroll/Scripts/ScrollNodeFlowTrial.cs`：当前统一试验流程，仍使用字符串Phase和调试IMGUI；禁用lab自身Update，旅行消费duration、travelDistance、curveStrength。speedCurve/backgroundOffset/skipAllowed尚未完整接入。

### 节点数据

`Assets/Scripts/RouteFake/` 下 Stage/Node/Choice/Travel各自是同名脚本的ScriptableObject。不要把多种资产类型合并回同一个.cs，已实际发生m_Script=None、出口丢失。

FakeRouteNodeConfig：nodeId、displayName、dialogues（DialogueEventData）、rewards（KillRewardEntry）、battleEntries（StageConfig引用）、completionPolicy、terminalPolicy、savePoint、outgoingChoices。

Choice目前是独立SO，已有资产引用；此前曾建议改成内嵌Serializable，但未实施，不要无迁移改变类型。completionPolicy/savePoint等字段尚不等于运行器完全消费其语义。

FakeRouteGraphValidator：ID、引用、连接和可达环路检查已存在。仍需补全非法死路、终点组合、快照与内容策略检查，并做真正负例测试。

## 4. 当前测试案例

资产目录：`Assets/Experiments/CurvedScroll/RouteTrialData/`。

```text
A_Start：真实战斗
├─ Left → B_Left：真实战斗 ─ Forward → D_End
└─ Right → C_Right：对话 → 领取30铜钱 ─ Forward → D_End
D_End：真实终点战斗，完成后仅显示Completed (test only)
```

- 战斗引用`SmallBattle.asset`：3名101、低伤害倍率、原DefaultFormation；不是共享TrialBattle的25敌人配置。
- 重要修复：WaveSpawner.ResolvedStageConfig优先自身序列化字段。它原先仍引用TrialBattle，导致节点数据虽指SmallBattle却实际生成25敌人。现在ScrollNodeFlowTrial在每次StartRouteBattle前同步waveSpawner.stageConfig。后续正式化应建立唯一战斗配置来源。
- C对话使用现有DialogueEventData，但当前显示在调试面板，不是正式BattleDialogueView。
- Coin节点奖励已测试+30；Heal有代码分支但缺完整验收。RandomUpgrade调用TriggerItemChoice受非战斗StageState限制，不能称已支持。
- 当前流程不写生产存档、不执行正式终点奖励/通关持久化。禁用的旧FakeRouteDataTrial曾写过正式SaveManager测试快照，历史测试不构成存档未污染证明。

### 用户操作

1. 打开主要测试场景，进入Play。
2. 击败A敌人并完成全部三选一/弃置，等待经验收集。
3. 看到屏幕中部大号“继续前进：Left/Right”。
4. Left验证下场战斗；Right验证对话确认→领取30铜钱→Forward。
5. D结束目前是测试完成提示。退出Play重新开始，勿依赖继承的正式菜单/重试按钮做存档测试。

## 5. 最后一轮修复与验证范围

### 已落地修改

- StageController：保留autoStartStage；_routeBattleRuntime直接发送完成事件。
- SetRouteRewardWaitState只停止生成，不立即回收死亡动画/奖励依赖；SetRouteTravelState执行现有节点清理，随后进入Starting。
- Enemy：死亡弹起/跌落及旋转Tween使用SetUpdate(true)，奖励timeScale=0时可收尾。
- SpikeTrapController：SetRouteVisibility(false)隐藏视觉、停止地刺协程/触发，保留技能参数；下一战StartRouteBattle恢复。不是ResetAll，避免抹掉地刺能力。
- UpgradeChoicePopup：跟踪淡入淡出Tween，替换前Kill；OnDestroy清理Tween与CanvasGroup绑定。针对已观察到的销毁后CanvasGroup访问进行了修复。
- 测试路线按钮由角落小字改为缩放后的中部大号按钮；Waiting rewards显示完成奖励提示。
- WaveSpawner同步当前节点StageConfig，避免25敌人和额外大量升级误入小型案例。

### 证据与限制

- 运行检查确实观察到Waiting rewards、三选一面板；确认完奖励后Choose route、StageState.Starting、IsRouteCombatActive=false、敌人0、地刺IsActive=false，并有Game截图。
- C对话→领奖→Choose route已用工具推进验证，局内铜钱精确+30；重复出口选择被拒绝。
- 诊断清场主要是TakeDamage高伤害与程序调用ConfirmChoice，不可扩展为全战斗自然操作已验收。
- 曾出现DOTween内部IndexOutOfRange、CanvasGroup MissingReference和残留奖励面板。Popup生命周期修复已编译，但这不足以证明所有Tween故障根因消除，仍需重新跑压力/实际点击测试。
- 用户最后回复“已确认”；按当前体验反馈记录为已确认，不扩展成Boss/QTE/存档等未测试项的认可。
- 地刺下沉曾被用户提出，但最后代码只新增可见性，没有明确的地刺yOffset/脚底定位修正证据；不要说地刺贴地已全面修复。实验地面整体Y=-1.8，地刺Sprite中心/底部空白与敌人不同，需要单独截图校准。
- 近期最后一次工具看到Completed (test only)，但未记录完整路径，不作为完整D结算回归证据。

## 6. 美术素材

已验收并导入 `Assets/Experiments/CurvedScroll/Art/`：
- ValleyProps.png：12个岩峰、树木、岩壁、岩石、灌木、草簇切片。
- ValleyShoulders.png：4条透明路肩。
- ValleyBackground.png：1024×1536竖版背景。
- ValleyRoad.png / ValleyRoadside.png：256px独立平铺底纹。
- ValleyGround.mat：实验道路材质。

透明合图原始Alpha上限253/254，导入副本已归一化，原图保留。切片按生成实际范围人工估计，所有变体边缘与底部锚点仍需逐项复查。背景lift=.4使山体露出地平线；不是重新生成背景。

原始图、请求与响应位于Library/Locus/tmp/valley-art，不宜唯一长期保存。主要批次：20260917_155010（装饰）、20260917_161428（验收背景/双底纹）、20260917_162648（路肩）。旧横背景未验收，勿误用。

## 7. 视频分支参考：复用什么，不照搬什么

参考 `origin/route-fake-movement`，最后fetch tip c31868a；实现主要来自b345fd7和c017a25。详细对照：`Locus/knowledge/plan/scroll-route-video-branch-review.md`。

优先复用：generation防旧回调、奖励等待与出发清理分开、正常死亡动画回池、旅行前补回收Dead敌人、Ultimate.Cancel链、各特效OnDestroy清理Tween、地刺入场杀敌补齐修复。

尚未完整迁移：EnemyManager补回收Dead敌人、Ultimate取消及Berserk清理、火焰/箭/旋风/被动箭销毁回调、QTE/蓄力完整取消、地刺补齐c017a25修复。

不得照搬：允许重访、每次进入只执行一个Entry；按Resources.FindObjectsOfTypeAll恢复升级；同名ID直接兼容视频/旧V2快照。当前需求优先于这些旧规则。

## 8. 存档是最高优先级未完成项

已有SaveData.fakeRouteSnapshots、DTO和SaveManager读写清理API，不等于已经有自动保存/完整恢复。正式FakeRouteRuntime尚未交付，曾写草稿已撤回。

### 需保存

- routeArchitectureId、snapshotVersion、routeId、stageId、configurationVersion、runId。
- 安全节点和阶段（刚抵达/内容完成/出口已确认）；已访问/完成节点、Entry进度、内容/奖励完成标记。
- 选择历史应包含sourceNodeId、choiceId、targetNodeId；若出口确认后保存，恢复必须保持锁定目标。
- 武将/规则版本身份；生命、复活、等级、经验、局内铜钱、击杀、大招能量。
- 被动升级、主动技能持有/等级/槽序；限次道具剩余次数/槽序；需要跨节点保持的Buff或资源需逐项盘点。
- 奖励已领/终点结算防重状态，避免恢复重复领取。

### 不保存

旅行帧、敌人/波次/投射物、攻击/QTE/连击、Tween、奖励弹窗中间态；普通/主动冷却与计时被动按明确节点规则重置。

### 恢复顺序

先验证完整快照、版本、节点历史和Inspector资源目录→停止旧流程并使token失效→清空运行态→重建Build→恢复生命等最终数值→恢复节点阶段并刷新UI。不能因恢复升级而重复回血/发物品/立即触发，主动技能不能同时通过upgrades和activeSkills应用两次。

不自动推断旧V2迁移。测试使用独立存储或精确备份还原，不直接修改用户player_save来验证接口。所有节点到达与内容完成均应保存，不因当前savePoint开关遗漏。

## 9. 推荐下一轮执行顺序

1. 读取本文和当前脚本，核对用户现场、配置和Git差异；避免再根据历史说明盲改。
2. 把试验字符串Phase整理为正式枚举状态和单一流程所有者；封装独立ScrollTravelPresenter，路线运行器不直接控制网格。
3. 优先补全视频分支清理依赖，验证Boss死亡奖励、地刺非战斗隐藏/下一战恢复、QTE/大招/DoT残留；单独校准地刺贴地。
4. 落地安全存档协议、玩家Build/道具恢复与幂等结算。节点无内容、纯对话、纯奖励、混合内容都用统一节点。
5. 保留小型A战斗→B战斗/C非战斗→D终点；再加不同终点样例。配置重导入后验证m_Script与全部引用。
6. 实际点击完成奖励→按钮→旅行→下一战；强退/失败恢复、双击、暂停、不同终点逐条记录。不能只看Phase值就宣布用户操作通过。

## 10. 其他现场注意

当前已跟踪修改包括SaveManager、SpikeTrapController、Enemy、StageController、UpgradeChoicePopup；实验目录和RouteFake目录仍有未跟踪文件。此前还观察到Enemy_101.prefab、DOTweenSettings、ProjectSettings和知识同步改动，未经核对不要覆盖或全仓库提交。

不要继续把旧README里“25敌人”“无真实战斗”“完整恢复已完成”等阶段描述作为当前事实。本文记录的是代码与证据边界；进入新对话后以实际读取为最终依据。
