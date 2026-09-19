# 实际操作复现与修复检查点

## 用户报告对应的根因

实测原场景的状态确实是：战斗清空后进入 `Waiting rewards`，而不是直接进入路线选择。此时屏幕中央有升级/道具三选一面板，玩家必须完成全部选择；这不是按钮缺失。此前调试工具用代码自动 `ConfirmChoice`，因此错误地把“工具状态跑通”报告成“用户实际流程跑通”。

另外，死亡对象在奖励等待阶段仍处于 `Dead + active` 是设计上的死亡动画窗口；奖励完成后才由死亡协程回池。旅行前清理存活/死亡残留。地刺在这次实测中 `IsActive=false`，未残留；已加入非战斗可见性开关和战斗开始恢复。

## 已完成的修正

- 测试场景只启用 `ScrollNodeFlowTrial`；`FakeRouteDataTrial` 和 `ScrollCombatTrial` 禁用。
- A节点真实战斗 →奖励等待→路线选项按钮。
- 节点按钮改为大尺寸“继续前进：Left/Right”，放在三选一面板区域之上/之后，避免小按钮和HUD遮挡。
- `FakeRouteNodeConfig` 保持统一节点结构，增加可选 `dialogues` 与 `rewards`；battleEntries为空仍是合法非战斗节点。
- A、B、D绑定小型真实战斗；C为对话+30铜钱非战斗节点。
- Enemy死亡动画Tween改为不受Time.timeScale暂停影响（SetUpdate(true)），确保奖励暂停期间死亡动画可以完成并回池。
- 地刺增加 `SetRouteVisibility(bool)`；离开战斗隐藏，进入下一场战斗恢复；不清除技能持有数据。
- StageController奖励等待阶段只停止继续生成，保留死亡动画/奖励依赖；进入旅行时才执行节点清理。

## 实际验证

- Play Mode真实生成25个敌人。
- 诊断击杀后显示三选一面板，状态为 `Waiting rewards`，不会误显示前进按钮。
- 自动完成全部三选一后，状态变为 `Choose route`，StageState为Starting，敌人数量为0，地刺IsActive=false。
- 截图已检查：竖屏画面中显示两条大号继续前进按钮：Left→B、Right→C。
- Console无错误。

## 用户操作

1. Play `Assets/Experiments/CurvedScroll/FakeRouteDataTrial.unity`。
2. 正常击败A敌人。
3. 完成所有升级/主动技能选择；若有弃置弹窗也完成。
4. 奖励面板关闭后，屏幕中部会出现“继续前进：Left/Right”。
5. 选择C可验证纯对话→领取30铜钱→Forward；选择B可验证下一场真实战斗。

仍未完成：自然操作而非诊断击杀的完整验收、正式路线运行器迁移、生产存档恢复、死亡对象池长期压力、Boss奖励、QTE/大招/地刺全组合回归。