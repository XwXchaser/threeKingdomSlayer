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
- 营地当前素材/地面沿用原资产，仍可见原来的地面质感不统一和背景素材职责问题；本轮重点是编辑架构，不宣称营地美术已完善。
- 尚待后续：完整A→B/C→D战斗/奖励回归、Boss/QTE/主动技能清理、正式存档。不要把隔离演出测试当作完整路线验收。

