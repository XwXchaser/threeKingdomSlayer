---
id: kd_fb96a7e6-c460-4e62-b7ef-d0881d692594
type: memory
path: unresolved-issues.md
title: unresolved-issues
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1779681797075
updatedAt: 1779682031835
---

# unresolved-issues

## Summary
当前 3 个未修复问题：QTE 无法触发、虚幻武器待验证、cardPrefab 偶现空引用 — 含调试步骤和关键文件

<!-- locus:body:start -->
# 未修复问题 TODO（2025-07-18）

> 开启新对话时可直接将此列表作为任务起点。
> 调试入口统一使用 Play Mode + Console 断点。

---

## TODO-1 [高] QTE 无法触发 — QTEController 始终 Idle

**症状**: Boss 进入战斗后 QTE 反击从不触发，`QTEController._state` 始终为 `Idle`

**已排除**:
- CanvasGroup 阻挡输入（QTE Canvas 无 CanvasGroup）
- QTE 配置缺失（BossQTEData_104 有 2 个攻击，QTEConfig 均有 prefab）
- InputManager 阻挡（skillInputEnabled=true, blockInputFrames=0）
- QTE Canvas 配置错误（ScreenSpaceCamera, worldCamera 已设）
- QTEController.enemy 引用缺失（Awake 中自动补全）

**调试步骤**:
1. Play Mode → 等待 Boss 进入战斗
2. 检查 `QTEController._state` 是否从 Idle 变为 CoolingDown
3. 检查 `QTEController._qtePhaseStarted` 是否为 true
4. 若 _state 仍为 Idle → 检查 `OnBossEngaged` 事件是否触发（在 QTEController.OnBossEngaged 加 Debug.Log）
5. 若 OnBossEngaged 不触发 → 检查 Boss 的 Enemy 组件是否正确触发 InCombat 事件

**关键文件**:
- `Assets/Scripts/QTE/QTEController.cs` — `OnBossEngaged()`, `Update()`, `StartQTE()`
- `Assets/Scripts/Enemy/Enemy.cs` — 搜索 `OnBossEngaged` 事件声明和触发点

---

## TODO-2 [中] 虚幻武器触发验证

**症状**: 用户反馈「虚幻武器无法触发」

**代码验证已通过**:
- `PassiveTriggerModule` 订阅 `AttackSystem.OnAttackPerformed` ✅
- `PhantomWeapon.asset` 在 commonPool ✅
- `UpgradeEffectManager.ApplyUpgrade` → `PassiveTriggerModule.Register` 路由 ✅

**调试步骤**:
1. Play Mode → 升级获取「虚幻武器」
2. 执行攻击 5 次 → 观察第 5 次是否触发幻影攻击
3. 在 `PassiveTriggerModule.OnAttackPerformed` 中检查计数器是否正确累加
4. 在 `AttackSystem.ExecutePhantomAttack` 中检查是否被调用

**关键文件**:
- `Assets/Scripts/Core/PassiveTriggerModule.cs`
- `Assets/Scripts/Player/AttackSystem.cs` — `ExecutePhantomAttack()`

---

## TODO-3 [低] cardPrefab 空引用偶现确认

**症状**: 历史曾报 `UnassignedReferenceException: cardPrefab of UpgradeChoicePopup has not been assigned`

**当前状态**: Editor 中 cardPrefab 已正确赋值（UpgradePopup prefab 和 scene 实例均确认）

**调试步骤**:
1. 进入 Play Mode
2. 击杀敌人获取经验 → 触发等级提升 → 弹出三选一
3. 确认是否出现 NullReferenceException
4. 若再现 → 在 `UpgradeChoicePopup.ShowChoices` 中打印 cardPrefab / cardsParent 是否为 null

**关键文件**:
- `Assets/Scripts/UI/UpgradeChoicePopup.cs:87` — `Instantiate(cardPrefab, cardsParent)`
<!-- locus:body:end -->
