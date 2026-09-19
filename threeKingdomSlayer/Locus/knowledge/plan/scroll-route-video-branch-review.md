---
id: kd_557e8789-77a2-4036-abe5-f8da68b81119
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 卷轴路线：视频分支对照与修复准备

## 来源与本轮边界

已 fetch `origin/route-fake-movement`，当次 tip 为 `c31868a`，运行时代码主要来自 `d1b68c7`、`e080b6b`、`b345fd7`、`c017a25`。仅阅读分支代码，没有 merge/cherry-pick 视频场景与资源。原始对照副本在 Library/Locus/tmp 下，不是项目代码。

本轮以调查和修复准备为最终交付。调查中尝试的未接线 FakeRouteRuntime 草稿已撤下，StageController 恢复本轮前的改动：autoStartStage开关与 _routeBattleRuntime 完成事件。Editor最后报告 up_to_date。当前没有正式 FakeRouteRuntime，缺按钮问题尚未修复。

## 缺少前进按钮的实际根因

Edit Mode 实查 `Assets/Experiments/CurvedScroll/FakeRouteDataTrial.unity`：
- FakeRouteDataTrial存在，是手动“Complete current content”模拟器，不订阅StageController战斗完成事件。
- Combat Scroll Environment 上 ScrollCombatTrial仍 enabled且active，是另外一套真实战斗试验流程。
- StageController.autoStartStage=false，stageConfig=TrialBattle，V2配置为空。
- A_Start、B_Left、C_Right、D_End四个逻辑节点的battleEntries全部为0。

因此真实战斗属于ScrollCombatTrial，手动路线属于FakeRouteDataTrial；二者同时驱动同一个scrollLab，但没有完成/选项联动。这不是按钮材质或布局问题。先前“无真实战斗”“已验证回访拒绝”的汇报不准确：场景确实启动了另一套真实战斗；在B上选择不存在的Choice_0只验证了无效ID拒绝，不证明已访问目标拒绝。

现有FakeRouteDataTrial还存在：旅行期间未锁定选择；以unscaled时间等待且未匹配卷轴实际抵达；未消费travelDistance/curveStrength/backgroundOffset；clearOnStart默认清除快照；直接使用正式SaveManager写测试快照。测试快照清理也不等于原始player_save字节未改变。后续测试必须独立存储或原件备份/精确还原。

## 视频分支实际可复用部分

### 状态机与选择

FakeRouteRuntime：进入节点→真实StartRouteBattle→监听OnRouteBattleCompleted→WaitingReward→等待UpgradeChoiceManager.IsChoosing、ItemDiscardPopup.IsShowing、ExpGemManager.IsCollecting→标记Entry→ChoosingRoute。只有ChoosingRoute绘制选择，点击立即变FakeMoving，使用generation防旧协程回调；播放结束才提交目标节点。

当前卷轴StageController已无条件对_routeBattleRuntime发送OnRouteBattleCompleted，不需要再恢复“FindObjectOfType某运行器才发事件”的耦合。

### 奖励等待与出发清理是不同阶段

视频StageController.SetRouteRewardWaitState仅EndCombatForRouteReward：取消大招、停止生成。不立即ClearAllEnemies，不提前销毁死亡表现/经验链路。

点击出口进入SetRouteTravelState才CleanupCombatForRouteTravel：销毁AttackWave、StabSweepEffect、SweepEffect，清理敌人/敌方弹体、连击、血包、节点攻击冷却、主动冷却、计时被动。StartRouteBattle也调用该清理保证上一节点没有残留。

### 死亡动画与对象池

Enemy死亡弹起/跌落Tween采用SetUpdate(true)，保证奖励暂停时死亡表现仍能收尾。正常动画完成再回池。

新增CancelDeathAnimationAndReturnToPool：停止死亡协程、Kill变换Tween、回池。EnemyManager.ClearAllEnemies除了存活列表，还遍历活动Dead敌人并执行此清理，避免死者已从存活列表移除而漏回收。

Boss奖励需要保留死亡动画完成回调，不能通过提前ClearAllEnemies跳过。

### 特效与地刺

- UltimateEffect新增Cancel虚方法；UltimateSystem跟踪活动大招；Berserk取消时停止协程并恢复玩家/HUD状态。
- ShootFireEffect、TimedArrowEffect、CycloneEffect及被动箭补Tween销毁清理；不是只Destroy根对象即可。
- TimedPassiveModule已按IsRouteCombatActive阻止非战斗推进，保留Build注册、重置计时和清理已生成效果；本地大部分已存在，视频差异主要是去日志。
- c017a25修复地刺杀死补齐敌人：Enemy在CheckAndTrigger后检测Dead立即退出；ColumnManager等待入场批次结束再执行挂起的重排。
- 所查远端SpikeTrapController与本地没有实质差异，没有发现独立的非战斗隐藏/恢复接口。不能宣称“所有常驻地刺生命周期都已解决”。ResetAll会同时清空伤害参数，不能在每次出发盲目调用而不重建。需要区分技能持有与场上表现，并验证下次战斗重新生效。
- QTE/蓄力的完整取消链路仍需专项核对，不把设计清单当作代码已覆盖证据。

## 存档缺口与不可照搬项

视频快照额外保存击杀数、局内铜钱、大招能量；choiceHistory记录sourceNodeId/choiceId/targetNodeId。当前卷轴简化快照缺这些字段与checkpoint阶段。

后续需保存：架构/版本/routeId/stageId/runId、到达或节点完成阶段、visited/completed/entry进度、锁定出口（若确认后存档）、角色配置身份、生命/复活/等级/经验、局内铜钱/击杀/大招能量、被动与主动持有/等级/槽序、限次物品剩余次数/槽序；检查击杀奖励已领状态、永久结算防重标记。仅保存完成前/后的安全状态，不保存敌人/波次/Tween/旅行帧。

先完整验证快照、资源目录与历史，再清理当前运行态，再恢复Build，再恢复生命等数值，再刷新UI；避免升级重放导致重复回血/物品/立即触发，也避免主动技能在upgrades与activeSkills中重复应用。资源从Inspector显式目录解析存档ID，不沿用Resources.FindObjectsOfTypeAll查找已加载对象。

视频分支允许重访且一次进入只执行一条Entry，本需求禁止同局回访；因此必须明确一个节点在本次进入执行完其全部内容。存档恢复当前节点属于恢复，不算再次沿边访问。所有节点到达/完成都保存，不只savePoint=true。共同终点与不同终点均需测试。

## 修复顺序及测试门槛

1. 备份实验场景与测试存档；移除/禁用互相竞争的两个试验驱动，仅一个路线运行器控制战斗与旅行。
2. 按依赖迁移清理：Ultimate取消链→Enemy死亡回池→EnemyManager收尾→StageController分阶段清理→特效Tween清理→地刺补齐修复。检查每个相关脚本和用户差异后局部编辑，不整文件覆盖。
3. 正式路线运行器接真实StageConfig与奖励等待，单出口也有按钮；全程枚举状态与generation保护。
4. ScrollPresenter消费连接配置，暂停一致、准确抵达、结束后冻结；相机/敌人逻辑不跟随旅行。
5. 独立快照协议、恢复、最终结算幂等，之后接UI。
6. 测试：首战自然击杀→奖励结束出现选项；Boss死亡奖励；三选一暂停期间死亡收尾；双击出口一次提交；左右两支/合流/不同终点；旅行暂停/失败/恢复；地刺下节点有效；火焰/箭/旋风/大招不残留；完成节点恢复不重复战斗/奖励；JSON/资产重导入有效。仅工具高伤害清场不能替代用户战斗体验验收。
