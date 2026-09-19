# 统一节点测试：当前检查点

## 入口

`Assets/Experiments/CurvedScroll/FakeRouteDataTrial.unity`。只有 ScrollNodeFlowTrial 启用；旧 FakeRouteDataTrial、ScrollCombatTrial 已禁用，不要手工重新启用。

所有节点仍是 FakeRouteNodeConfig；可选 dialogues 直接引用现有 DialogueEventData，rewards 复用 KillRewardEntry，battleEntries 引用 StageConfig，没有新增战斗/奖励/对话节点类型。

## 人工测试步骤

1. Play 后 A 自动开始真实战斗。SmallBattle.asset 配置3名101，伤害倍率0.05，沿用 DefaultFormation；共享TrialBattle及Enemy_101用户改动不覆盖。
2. 击败A的敌人，完成全部经验升级/弃置选择。出现 Left/Right 分支之前，阶段应为Waiting rewards。
3. Left进入B，执行同一小规模战斗，完成后选择Forward进入D。
4. Right进入C：不生成敌人；点击Continue dialogue，再点击Claim node reward，铜钱增加30，再显示Forward。
5. D执行战斗；结束显示Completed (test only)，不发放永久通关/铜钱结算，不写生产路线存档。
6. 旅行期间按钮不可重复选择；暂停时间缩放时旅行不推进。退出Play重置本次测试。

## 已实测

- 完整编译通过。
- A真实敌人死亡（诊断高伤害）后，通过UpgradeChoiceManager.ConfirmChoice逐个完成现有奖励，进入Choose route。
- A→C到达Dialogue；确认后Claim reward；领取后Choose route，局内铜钱精确+30。
- C→D首次选择成功，重复选择被拒绝。后续异步检查只观察到Travel随后Battle，没有完整验收D结算。
- 诊断中使用了临时无敌/高伤害，不等于自然操作战斗体验验收。

## 清理边界变化

StageController.SetRouteRewardWaitState不再立即ClearAllEnemies，仅停止生成，保留死亡动画及延迟奖励回调。SetRouteTravelState执行现有节点切换清理再进入非战斗。

仍未完成视频分支的全部迁移：死亡Tween无缩放时间、死亡敌人补充回池、Ultimate取消、特效Tween集中收尾、地刺非战斗隐藏与次战恢复、QTE取消。不能宣称这些已解决。

## 后续缺口

本测试组件是统一节点流程的隔离验证，不是完整正式FakeRouteRuntime。未实现检查点保存/恢复、角色/Build/物品恢复、不同终点样例、正式路线UI。对话复用数据但显示为调试面板，未接正式BattleDialogueView。

旅行已消费duration、travelDistance与curveStrength；speedCurve/backgroundOffset/skipAllowed尚未实现。当前RandomUpgrade奖励调用受StageController非战斗状态限制，不应配置为已支持的非战斗奖励；本例仅用Coin。
