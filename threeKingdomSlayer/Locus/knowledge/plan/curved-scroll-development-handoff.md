---
id: kd_05fdb97a-d8dc-45f8-9e27-272984b65401
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
summary: Y分叉最新交接：用户确认54株草摇曳已实现；细分网格与自动风动接线正确，当前Battle和Y均dirty，风动保存重载收尾及自然路线回归待确认。
---

# 卷轴向开发交接：当前进度与后续方向

## 1. 下一对话先读这里

**当前场景搭建以第14节“2026-10-01 当前Y分叉场景交接”为最新入口，覆盖下方旧实验入口说明。**本节下面至第12节的大部分内容记录旧直线卷轴实验，不能用来决定当前部署目标。当前只在 `Assets/Scenes/Battle.scene` + Additive `Assets/Experiments/CurvedScroll/YJunctionSample.unity` 工作；`FakeRouteDataTrial.unity` 不得打开或保存。

### 历史入口（仅保留背景，不是当前操作步骤）
本文件早期记录用户对当时修复回复“已确认”；这不构成最新草簇、背景或完整战斗流程的最终验收。

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

## 11. 连续卷轴场景任务交接（最新）

### 下一对话必须先读
本节覆盖本轮连续2.5D卷轴场景改造的当前事实。当前用户反馈明确表示：

1. 完成节点后点击“继续前进”仍会出现“场景跳跃”，上一轮修复未解决，不能宣称已修复。
2. 敌人受击时会出现整个场景/镜头整体震动，需确认是否是错误的全局镜头反馈，当前未验收。
3. 当前实验功能仍不能视为完成；先修复稳定性和视觉连续性，再继续扩展布局。

### 当前目标与设计方向
- 卷轴是连续的2.5D面片化场景，不是按节点切换背景或Unity Scene。
- 玩家沿 presentation distance 前进，实时看到山谷、营地入口、营地内部等环境变化。
- 节点只负责战斗、对话、奖励和路线出口；节点进入不应直接切景。
- 战斗坐标、敌人阵型、攻击、QTE和投射物不受卷轴道路宽度与视觉环境影响。
- 场景由背景、地面、道路、侧景、前景和过渡面片组成；未来可补充素材，但当前优先修引用、布局和生命周期。

### 当前实验入口
- 场景：`Assets/Experiments/CurvedScroll/FakeRouteDataTrial.unity`
- 流程组件：`Unified Node Flow Trial/ScrollNodeFlowTrial`
- 表现组件：`Combat Scroll Environment/ValleyArtPresentation`
- 卷轴核心：`Assets/Experiments/CurvedScroll/Scripts/CurvedScrollLab.cs`
- 连续序列：`Assets/Experiments/CurvedScroll/Scripts/ScrollWorldSequence.cs`
- 预览组件：`Assets/Experiments/CurvedScroll/Scripts/ScrollWorldPreviewTrial.cs`
- 测试序列：`Assets/Experiments/CurvedScroll/RouteTrialData/ScrollWorldSequence_Trial.asset`
- 视觉Profile目录：`Assets/Experiments/CurvedScroll/RouteTrialData/`

### 当前连续序列配置
`ScrollWorldSequence_Trial.asset`已回读确认：
- `0–60`：Valley，使用 `VisualProfile_Narrow_01`
- `60–105`：CampApproach，使用 `VisualProfile_StrongholdEntrance`
- `105–175`：CampInterior，使用 `VisualProfile_StrongholdInterior`

`ScrollNodeFlowTrial.worldSequence`已绑定该序列。`Travel()`仍固定3秒，并在旅行帧调用 `ValleyArtPresentation.ApplyWorldDistance()`。

### 已完成的实现
- 已创建三份独立多波次单列战斗资产：`NarrowRoadBattle_01~03.asset`及对应窄路阵型。
- A/B/D节点已分别绑定三份窄路战斗资产；C仍为非战斗对话/奖励节点。
- 旅行资产duration已统一为3秒；运行代码使用`FixedTravelDuration = 3f`。
- 已建立`ScrollWorldSequence`和环境区段数据结构。
- 已建立Valley/CampApproach/CampInterior测试Profile。
- 已将山谷和营地素材写入固定布局`ScrollPropPlacement`，不再仅依赖随机小物件池。
- 已加入卷轴预览对象`ScrollWorldPreviewTrial`，Play Mode按F8打开，可拖动距离查看整段序列；尚未完成完整验收。
- 已为运行时对象销毁增加部分DOTween清理；Unity最近一次完整编译通过。

### 当前已知问题（未解决）
#### BUG-Scroll-01：点击前进后场景跳跃
现象：节点战斗/奖励结束后，点击路线前进，场景出现明显跳跃，当前修复后仍复现。

已排查但不能视为解决：
- 曾发现`TryGetSegment()`原逻辑从前往后返回第一个满足条件的区段，已改为返回最后一个`startDistance <= distance`的区段，并验证距离0/59/60/80/104/105/120/175对应区段正确。
- 曾移除旅行过程中在50%处调用`ApplyProfile(next.profile)`的逻辑。
- 曾避免第一次`ApplyWorldDistance`时重新套用当前Profile。

仍需重点排查：
- `ValleyArtPresentation.ApplyProfile()`是否在其他生命周期/初始执行路径中重设背景、道路、对象池或固定布局。
- `ApplyWorldDistance()`只对背景和道路宽度做连续插值，但Profile中的道路开关、路肩、固定物件和材质仍不是连续混合。
- `appliedSegment/appliedProfile`状态与`Start()`初始化顺序是否造成一次旧Profile/新Profile重排。
- `ScrollNodeFlowTrial.Travel()`结束后`Enter(targetNode)`、`stage.SetRouteTravelState()`和下一战启动是否触发其他场景/背景重置。
- `CurvedScrollLab.SetPresentationDistance()`与`ValleyArtPresentation.LateUpdate()`是否在同一帧产生距离/布局重复更新。
- 必须用逐帧日志或断点记录：点击时的lab.Distance、current segment、appliedProfile、roadWidth、background引用、fixedProps数组长度，以及所有ApplyProfile调用来源。

修复标准：点击前进后3秒旅行内不得发生整批布景销毁/创建、背景瞬时替换、道路瞬时重建或距离回退；环境变化必须由连续距离驱动。

#### BUG-Scroll-02：敌人受击时整个场景震动
现象：敌人受击时观察到整个场景整体震动，当前未确认这是预期的局部命中反馈还是错误的全局镜头反馈。

代码证据：
- `Assets/Scripts/Core/CameraFeedbackController.cs`的`RequestHit()`会调用`PlayFeedback()`。
- `PlayFeedback()`对Camera Transform执行`DOLocalMove`和`DOShakeRotation`。
- `LateUpdate()`又按cameraDelta把`worldBackground`移动`cameraDelta * 135f`，因此背景会被大幅带动。
- 当前`CameraFeedbackController`仍是全局相机反馈入口，不能直接假设应删除；需要先确认用户要求的是局部受击反馈而非全场景震动。

排查/修复标准：
- 明确敌人受击是否应只做敌人局部HitStop/缩放/闪白，还是允许极轻微镜头反馈。
- 若保留镜头反馈，需限制来源、强度、频率和屏幕位移，避免道路、背景、营地布景整体明显跳动。
- 分离`worldBackground`的视觉视差反馈，不应把背景大倍率移动误认为场景连续卷轴运动。
- 用无攻击、单次命中、连续命中、DoT四组测试确认：普通受击、重击、DoT是否分别触发镜头和背景位移。

### 相关稳定性问题
曾观察到DOTween的`MissingReferenceException`和内部`IndexOutOfRangeException`：
- 销毁后的Transform/SpriteRenderer仍被Tween访问。
- 相关Console路径包含`CameraFeedbackController.PlayFeedback`、`SpriteRenderer.DOFade`和若干攻击/特效对象。
- `ValleyArtPresentation.OnDestroy()`与`CameraFeedbackController.OnDestroy()`已增加部分Kill清理，Unity编译通过；但尚未重新进行重复进入/退出Play Mode及完整路线压力验收。
- 下一对话必须清空Console后重新测试，不能把旧错误与新错误混为一谈。

### 当前待验收情况
未通过：
- 点击前进后的场景连续性。
- Valley→CampApproach→CampInterior的实际视觉过渡。
- 山谷大型侧景是否形成包围感。
- 营地固定布局是否规整且中央战斗区无遮挡。
- 营地背景、营地地面、前景装饰职责是否正确分离；当前`VisualProfile_StrongholdInterior.groundMaterial`仍引用`ValleyGround.mat`，需修正/验收。
- 敌人受击时是否存在不应有的整体场景震动。
- 反复进入/退出Play Mode时DOTween错误是否清零。
- F8整关预览是否不启动战斗且能正确拖动全序列。

已完成但仍需回归：
- Unity编译。
- 连续序列资产回读。
- A节点读取3波、总敌人数6的基础验证。
- worldSequence场景绑定回读。
- 分段选择函数的距离映射验证。

### 下一对话推荐执行顺序
1. 先清空Console并保持Edit Mode，读取当前脚本和场景引用；不要继续生成素材。
2. 给`ApplyProfile`、`ApplyWorldDistance`、`SetPresentationDistance`、`Enter`、`Travel`增加可关闭的逐帧/调用来源诊断，定位BUG-Scroll-01的真实跳变来源。
3. 临时禁用固定布局重排、Profile材质/背景替换和节点visualProfile兼容分支，逐项恢复以确定跳跃触发点。
4. 单独隔离CameraFeedbackController：记录敌人受击时camera localPosition、rotation、worldBackground position和触发来源，定位BUG-Scroll-02。
5. 修复后先做纯预览距离拖动，再做A→选择前进→3秒旅行→下一战，最后做敌人受击回归。
6. 只有Console无错误、场景不跳、战斗坐标未变化、用户视觉验收通过后，才继续扩展场景预览和更多环境布局。

### 修改边界
- 不修改正式`Assets/Scenes/Battle.scene`来替代实验验证。
- 不删除用户现有未提交改动。
- 不把节点Profile绑定当作最终连续世界方案。
- 不把“编译通过”或“分段函数返回正确”表述为视觉问题已修复。

## 12. 可编辑场景重构（2026-09-26，优先于上面的历史状态）

### 用户最新要求
- 所有部署物件必须是 Hierarchy 中能选择、保存和修改的真实场景对象。
- Scene 窗口同时展示平铺的整条环境；不接受仅观察某个距离的临时预览。
- 根组件距离滑条控制 Game 窗口中的实际卷轴效果；编辑结果直接用于游戏。

### 本轮实现
- 测试场景新增 `Scroll World - Editable Layout`，挂载 `ScrollWorldAuthoring`。
- `Layout - edit objects here` 下分 `00_Valley`、`01_CampApproach`、`02_CampInterior`，每段包含 Ground、Landmarks、Roadside、Background；148 个带 ScrollWorldItem 的真实可保存对象（142 个侧景、3 个地面、3 个背景）。
- Profile 仅作为一次性迁移来源。现有场景对象的 Transform/SpriteRenderer/材质是布局权威来源；运行时不再根据 Profile 重建或覆盖布局。不应再次执行导入来覆盖用户手动编辑。
- Scene 中物件沿世界距离平铺；仅指定 Game 相机渲染时，专用 shader 执行距离偏移、远处下弯与视距裁剪。渲染结束恢复 shader 开关及 bounds，物件 Transform 全程保持平铺位置。
- ScrollWorldAuthoring 自定义 Inspector：`Game 预览距离` 滑条 0–175，`Scene 聚焦整条平铺路线` 按钮。支持直接移动/缩放/翻转/换图/复制/删除已有场景物件。新增侧景可复制已有物件，保留专用材质和 ScrollWorldItem。
- 菜单 `Tools/Curved Scroll/Select Editable World` 选中根节点；旧 World Preview 菜单转向该入口，不再生成临时树石。旧 F8 控制已停用。
- ScrollNodeFlowTrial 绑定 authoredWorld；开局0→20、A→B/C为20→75、B/C→D为75→145，各旅行8秒，退出Play后编辑滑条恢复。旧 Lab/ValleyArt/ScrollDepth 组件保留但禁用，不再生成对象。
- 一次性迁移侧景时按 sprite 包围盒保留中间通道（不在运行时钳制位置）；原 x=0 的营地主帐篷移至侧面，用户可在 Scene 直接再布局。
- 文件：ScrollWorldAuthoring.cs、ScrollWorldItem.cs、Editor/ScrollWorldAuthoringEditor.cs、Shaders/AuthoringScroll*.shader、Shaders/ScrollProjection.cginc；生成的永久材质/地面网格位于 `Assets/Experiments/CurvedScroll/Authoring/`。

### 验证与边界
- 编译通过，三个 shader 无编译消息；采样时 Console error/warn=0。
- 保存并重开场景：148 个对象、148 注册项，authoredWorld引用保持，旧runtime roots=0，真实布局对象不带DontSave。
- 临时移动真实侧景做渲染对比，Game截图像素差8297；测试后恢复原位。滑条20→145未改变任何布局Transform；渲染后shader开关归零，bounds全部恢复。
- Edit Mode Scene平铺与Game在20/75/145的显示均已截图；进入Play不继承编辑滑条75，A战斗距离为20，148对象不重复生成。
- 隔离调用旅行协程验证20→75和75→145均8.00秒，未改变布局Transform；这不等同完整自然战斗/奖励操作回归。
- 用户已验收前次首次前进不闪现及清除误保存树石；本轮新布局仍待用户编辑体验与美术验收。

### 本轮分叉卷轴与Battle宿主交接（2026-09-27）

- Y形卷轴样例 `Assets/Experiments/CurvedScroll/YJunctionSample.unity` 已验收视觉转向：单一连续地板、Left/Right两分支、Progress拖动、-45°/+45°方向、左右环境与展示敌人均可观察。
- 当前正式测试结构：`Assets/Scenes/Battle.scene` 是唯一战斗宿主；通过 `BattleYRouteHost` Additive加载Y路线层，绑定Battle Main Camera，禁止Y样例反向加载Battle。
- 已验证流程：Battle启动→Y层加载→开局移动→真实StageController战斗→奖励/三选一阻塞完成→Left/Right选择→路线Progress实际移动→抵达后再次StartRouteBattle生成真实敌人。`Time.timeScale=0`奖励期间路线使用unscaled时间仍能移动。
- 已禁用旧 `RouteStageRuntimeV2`，避免重复路由系统；Build Settings已包含Y样例。
- 展示敌人仅为SpriteRenderer，不挂Enemy脚本；真实敌人由Battle.scene对象池/StageController生成。
- 最近错误的Enemy visualYOffset抬高逻辑已撤销；当前 `BattleYRouteHost.applyEnemyVisualOffset=false`，Enemy rootY/visualYOffset恢复原始值。视觉地面与Battle战斗地面高度仍需后续最终统一，不得宣称下沉完全解决。
- Edit Mode工具：`Tools/Curved Scroll/Open Battle Host` 同时打开Battle和Y路线层，Scene/Game可联合观察；`Open Y Junction Sample`用于独立布局编辑。
- 当前仍是测试Host，不是正式RouteBranchRuntime；B/C→D合流、正式存档、正式节点快照、自然完整左右长流程仍未交付。
- 安全边界：不要重新启用RouteStageRuntimeV2；不要使用全局Shader接管Battle对象；不要让展示敌人承担战斗逻辑；不要把单次方法调用测试表述为自然操作验收。

- 尚待后续：完整A→B/C→D战斗/奖励回归、Boss/QTE/主动技能清理、正式存档。不要把隔离演出测试当作完整路线验收。

## 14. 2026-10-01 当前Y分叉场景交接（新对话先读）

### 当前现场
- 当前分支已是 `port/combat-layer-from-video-branch`，HEAD 为 `07970e52 feat: 移植视频分支战斗层 — 招式状态机/张飞连招 + 骑兵109与102胆怯形态`。工作区还有视频战斗层和场景搭建未提交文件，禁止全仓库回退、覆盖或清理。
- Active Scene：`Assets/Scenes/Battle.scene`，Battle dirty=false；Additive 已加载 `Assets/Experiments/CurvedScroll/YJunctionSample.unity`，当前回读 dirty=false。只加载这两个场景，未处于 Play Mode。
- 最终根节点 `Y Junction - select for preview` 位于 `(0,0,0)`，预览 `progress=0.5`、`right=true`、`turnAngle=45`，`scenery`=151；其中54个是草、引用无空项或重复，天空不在该列表中。
- `FakeRouteDataTrial.unity` 未加载；严禁打开、保存、回退或向其部署素材。
- Console 最近回读 error/warning=0。Battle.scene、`YScrollSample.cs`、`BattleYRouteHost.cs`、共享 `YScrollGround.shader/YScrollScenery.shader` 相对当前 HEAD 没有工作区 diff。**分支已包含新的战斗层基线，不能拿旧14c30d0的Battle文件覆盖它。**
- 场景搭建本轮没有 commit/push；新环境图、材质、YGrass等Shader、Encounter脚本大量为 `??` 未跟踪，Y场景和文档为未提交修改。其他Enemy动画、骑兵/MoveSystem文件、HTML、`Locus/workspace-trees/default.json` 均保留，不在本任务清理范围。
- `FakeRouteDataTrial.unity` 虽未加载，磁盘仍带此前误编辑的 diff；已明确保留，不继续修复或整文件回退。

### 已落盘的当前场景结果
- Y Terrain 使用 `Assets/Experiments/CurvedScroll/Authoring/YSampleGround_ThreeLayer_Candidate.mat`：`_Road=BattleRoad_v2`、`_Shoulder=BattleShoulder_v3_candidate`、`_Outer=BattleRoadside_v2`。已拍开局/Left/Right候选图；它改善两侧砂石与中央道路的硬切，但仍是平面三层 Shader，不是真实高低差 Mesh。
- 天空使用 `YSampleScenerySkybox_v2.mat` + `YSkyBackground.shader`，已从 `YScrollSample.scenery` 移除；不再遮挡道路，竖版图 UV方向已修正。
- 当前保存54株草簇：`GrassOpeningTrial_00~05` 6株、`GrassPatch_Opening_00~13` 14株、`GrassPatch_Left_00~16` 17株、`GrassPatch_Right_00~16` 17株；均是真实 Hierarchy对象并加入 `YScrollSample.scenery`。前6株虽然名称含Trial，也是当前保留布局，不能按名字当临时对象删除。
- 草簇使用独立 `Assets/Experiments/CurvedScroll/Authoring/YGrass.mat` + `Assets/Experiments/CurvedScroll/Shaders/YGrass.shader`，不再使用共享树木/建筑材质；保存重开后回读54株、54个 scenery 引用、专用 Shader正确。
- 草簇当前已压低：回读世界高度约 `0.4836–0.993`；宽度保留；每株有约 ±25° Y旋转。开关对照曾产生10906个变化像素，说明朝向参与渲染。
- 三个 `BattleEncounterAuthoring` 仍存在并分别引用 `NarrowRoadBattle_01/02/03`，但 `BattleYRouteHost` 实查开局/左/右仍全部为 `SmallBattle`，并且运行时把路线根Y置为-1.8。Encounter预览不是实际生成配置来源，正式运行接入仍是待办。
- 本轮建筑/节点布景修正：两座瞭望塔已缩至0.4并按左右终点镜像放到远景（终点后27、路侧±6.2）；门楼放到右终点后24、道路中心，主帐篷放到右终点后22、左侧-5.6。仍保留建筑原Sprite中心Pivot，场景Y补偿已扣除16px透明留白，实际Alpha脚点贴地；未修改共享Importer、Shader或移动核心。旧重复 `Right - Camp Encounter/CommandTent - beside road` 已禁用但保留对象。
- 悬空帐篷根因：旧 `SmallTentA/SmallTentB` 的Rect同时截入上下两排帐篷，旧 `CommandTent` 切片也带邻物残段；单改Y无法修正。保留原图集不动，新增单主体正确裁切的 `SmallTentA_Corrected.png`、`SmallTentB_Corrected.png`、`Crates_Corrected.png`，并导入已验收的 `Palisade_v1.png`、`Banner_v1.png`、`Brazier_v1.png`，统一位于 `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/Props/`，仅在Y场景部署，无新生图。
- 左谷战斗节点12个树岩/悬崖按相同路径深度与相反侧向偏移镜像重排；56个旧ValleyProps树石按各自7–17px透明底边修正Y。右营地帐篷、栅栏、旗、火盆、箱子成对布置到可见视野，开局战斗点新增6个镜像木栅/军旗/火盆；54株草布局、比例、朝向和专用Shader未改。
- `YRouteSurfaceAuthoring.cs`、`YRouteSurface.shader` 为早先误诊时留下的未跟踪草稿，Y场景附着数量=0。现有Terrain已经覆盖分支；不要部署这批草稿。组件含 `OnEnable/OnValidate` 重建及未隔离的 `UnityEditor` 调用，与安全协议相冲突，后续若整理须先核对使用并单独审批删除或修复。

### 8. 当前用户提出的下一轮环境目标（2026-10-02）
- 用户指出：当前场景装饰普遍偏矮，人物与树/栅栏的比例不可信；整体环境仍稀疏。
- 设计问题：分叉路口正前方过于空旷，没有山石、城墙或道路标识形成阻挡/引导，无法从画面语义解释为什么道路必须分叉。
- 下一轮目标：以人物高度为参照重新校准树木、栅栏和关键营地装饰；补足开局直路、分叉口和分支前段的近/中/远景密度；在不侵入战斗通道、不改移动核心的前提下，用自然障碍/地标构成“主路被地形或设施截断，左右绕行”的视觉逻辑。
- 当前证据：Y场景当前只读回读 scenery=151；主要父节点数量为 `Approach - Valley Road=32`、`Left branch landmarks=30`、`Right branch landmarks=30`、`Left - Valley Encounter=20`、`Opening Roadside Props=14`、`Right - Camp Encounter=12`。场景对象的局部scale中，Opening栅栏约0.62、Roadside树约0.7、分支树约0.9、Palisade约0.28–0.34；这些数值只能作为初始核对，最终以Battle Main Camera下Game画面为准。
- 实施约束：仅修改Y路线层和其景观对象/资源；保护Battle.scene战斗坐标、Player/Enemy/Camera/StageController、YScrollSample.Evaluate、路径点、转角和草风动系统。不得打开、保存或回退 `Assets/Experiments/CurvedScroll/FakeRouteDataTrial.unity`。

### 资源与验证证据
- 道路候选参数：`_RoadHalfWidth=2.45`、`_ShoulderWidth=1.5`、`_RoadBlend=1.1`、`_OuterBlend=1.3`；OuterTint=`(1.18,1.11,1.02,1)`。没有启用新的道路Mesh，也没有重写Y移动。
- 现有草图为 `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/Grass/GrassSmall_v1.png`、`GrassWide_v1.png`、`GrassMedium_v1.png`、`GrassTall_v1.png`；本轮使用前三种，不需要新生图。原图/抠图/请求记录在 `C:/Users/Administrator/Pictures/gptGen/camp_grass_20260928_v1/`。
- 已验收小物件原始交付在 `C:/Users/Administrator/Pictures/gptGen/camp_props_20260928_v1/`（木栅/军旗/火盆）；大型建筑在 `camp_architecture_20260928_v1/`；环境v2在 `camp_environment_20260928_v2/`；肩部候选在 `camp_environment_20260928_v3/shoulder/`。后续导入前检查真实路径，保留原图，不自动重生成付费请求。
- 最新前后截图在 `Library/Locus/tmp/grass-yaw/`：`before_opening.png`、`before_left.png`、`before_right.png`、`after_opening.png`、`after_left.png`、`after_right.png`、`after_left_end.png`、`after_right_end.png`。它们是临时验证证据，不是永久美术交付；对象引用和正文事实已经记录，不能只依赖Library保存。
- 本轮确实查看了开局0、实际开局战斗点 `18/Length≈0.26035`、Left/Right 0.5/0.75/1 的放大Game图。截图预览临时把路线根Y设为Host实际的-1.8，结束恢复0；保持Battle Camera原位。已保存并只重载Y层，回读151景物/54草/无缺失Sprite和材质，两场景dirty=false，Console error/warning=0。
- 最新可复核图：`Library/Locus/Screenshots/locus_game_20261001_081649_060.png`（开局战斗两侧装饰）；`locus_game_20261001_083756_472.png`/`locus_game_20261001_083921_519.png`（左右0.75）；`locus_game_20261001_084026_548.png`（重载左谷终点）；`locus_game_20261001_084429_691.png`（重载右营地终点）；`locus_game_20261001_085429_632.png`、`locus_game_20261001_085536_394.png`、`locus_game_20261001_085715_743.png`（开局/右营地/左谷Play暂停取景）。这些Library图是临时证据，不是永久美术交付。
- Play验证边界：Host实际进入开局Battle、主相机正确绑定、路线根Y=-1.8、展示敌人自动隐藏；为了干净截图暂时关闭受击Overlay并在Play暂停时切换route.right/progress查看两分支环境，未推进自然奖励/选择/旅行、未改Battle坐标，退出Play恢复。两次Play进入/退出未发现Console error/warning，但不等于自然完整路线、Boss/QTE或10次生命周期验收。

### 已验证移动基线
```text
Left  p=0.00 (0,0,0) angle=0
Left  p=0.25 (0,0,17.28) angle=0
Left  p=0.50 (-5.58,0,33.03) angle=-45
Left  p=0.75 (-17.80,0,45.25) angle=-45
Left  p=1.00 (-30.02,0,57.48) angle=-45
Right p=0.00 (0,0,0) angle=0
Right p=0.25 (0,0,17.28) angle=0
Right p=0.50 (5.58,0,33.03) angle=45
Right p=0.75 (17.80,0,45.25) angle=45
Right p=1.00 (30.02,0,57.48) angle=45
```

移动核心未改。**交接实查 `YScrollSample.viewCamera=None` 且Sample Camera disabled，Edit Mode下会呈现未投影画面，不能误判为路线丢失。**跨场景拖拽引用不能可靠保存；Play时Host会绑定Battle Main Camera。已加载Battle+Y时直接会话绑定并在渲染后恢复参数即可，不保存Battle。`Tools/Curved Scroll/Open Battle Host` 虽可绑定，但菜单会先Single重开Battle、再Additive开Y，且 `EnsureSaved()` 只检查Active Scene dirty；有Y未保存编辑时不要调用菜单，否则可能丢失布局。持久安全的Editor绑定入口仍待修复。

### 草簇风动当前交接（2026-10-01，用户已确认效果已实现）

- 用户当前确认：现有草已实现可观察的摇曳效果。本结论更新了此前“仅完成草Shader/细分部署、风动尚未完成”的旧描述。
- 实现文件：`Assets/Experiments/CurvedScroll/Shaders/YGrass.shader`、`Assets/Experiments/CurvedScroll/Authoring/GrassWindMeshes/`，以及对应的草材质资源。
- Shader行为：使用真实高度细分网格；根部固定，上部按 `texcoord.y²` 增加弯曲；风动由自动 `_Time.y` 驱动，并按草的作者位置生成相位差；保留Y路线投影与草自身朝向处理。
- 已记录的部署结果：54株原始草保留为布局源并禁用原 `SpriteRenderer`，每株有一个 `Wind Mesh` 子节点；细分网格为21顶点/24三角形；`YScrollSample.scenery`已从重复的205项修正为151项，其中54项为唯一风动渲染引用；三份风动材质参数为强度0.28、速度2.1、自动时间（`_WindTimeOverride=-1`）。
- 本轮只读现场核对再次确认上述数量、网格和材质参数；scenery空项=0、重复=0，三份材质Shader supported=true，Console error/warning=0。当前Active为Battle，仅加载Battle+Y，双方dirty=true；Y根位置(0,-1.8,0)、right=false、progress=0.260352、viewCamera=None。这覆盖前面旧交接中的dirty=false和已恢复预览值，不代表已丢失或损坏。
- 本轮仅写文档，不保存或重开场景。后续持久化收尾应先确认dirty内容及用户并发编辑，恢复经确认的临时预览值，再仅保存授权Y层和草资产并重载回读；禁止Save All或为清理dirty整场景回退。
- 验收边界：用户确认当前效果已实现，但本轮文档更新没有重新拍摄连续帧，也没有重新加载场景做持久化回读；因此不扩展为“本轮重载后完整验收”。
- 保护边界：Battle移动核心、战斗坐标、树木、道路和Y路线采样不属于草风动改动范围。`Assets/Experiments/CurvedScroll/FakeRouteDataTrial.unity` 当前在Git中存在未提交修改，禁止在本交接中打开、保存、回退或归因给草风动。

### 下一对话建议顺序
1. 先读 `Locus/knowledge/memory/y-junction-scene-authoring-workflow.md`、`Locus/knowledge/memory/project-mistake-note.md`、`Locus/knowledge/plan/latest-todolist.md` 的错误复盘协议和本节。
2. 复核 Active Battle + Additive Y、FakeRoute未加载、Git status、Console=0；不要碰视频战斗层未提交文件。
3. 用户已确认草摇曳实现，不再自动重复实现或调整草高度。恢复工作时先处理风动持久化收尾，核对并保护双方dirty内容；随后按需要做会话相机绑定和开局/Left/Right/终点画面回归。保留54株、现有布局和路缘侵入，不重新生图或删除草。
4. 先请用户复核本轮开局/左谷/右营地战斗构图与参考素材的一致性；有新问题继续按真实镜头、Alpha主体和路径位置小组修正。已验收木栅/火盆/军旗与正确裁切帐篷现在有场景引用，不要重复生图或再用旧错误切片。
5. 当前三层候选及曲率/移动核心保持；实际对照已证明此前道路空带来自天空遮挡，不是矩形网格必然不够。高度差道路Mesh只是曾讨论的备选，不是已授权的下一步必要任务。
6. 完成 Encounter 数据消费和最新战斗层下Left/Right自然Play回归前，不得宣称分叉正式流程完成。相机重开问题也需后续提供安全的Editor绑定入口。

