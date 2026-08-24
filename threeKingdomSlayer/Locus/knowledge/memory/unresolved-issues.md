---
id: kd_fb96a7e6-c460-4e62-b7ef-d0881d692594
injectMode: inherit
summary: 当前 2 个未修复问题：QTE 无法触发、cardPrefab 偶现空引用 — 含调试步骤和关键文件。TODO-2（虚幻武器）已完成。
aiEditMode: inherit
---

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

## TODO-2 [已完成] 虚幻武器触发验证 ✅

Phase 2 已完成：延迟攻击、蓝色伤害数字、per-level 配置均已实现。

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

## TODO-4 [高] 场景化路线计时被动跨节点时序

**当前状态**：待持续观测，未判定修复。现象有两类：Head→Combat 期间疑似提前触发；计时器进入冷却但没有可见效果。添加大量日志后问题曾暂时消失，需警惕日志改变 Editor 时序。

**关键日志**：`[TimedPassiveDiag]`、`[RouteDiag]`、`BurnTick`、`DiseaseTick`。

**验收重点**：
- Head→Combat 不应出现 `TimerExpired`、`BurnTick` 或首次计时被动触发；
- `StartRouteBattle` 后应先确认敌人已生成，再尝试首次效果；
- 效果创建失败不得进入冷却；
- 旧节点 DoT、效果对象和协程不得污染新节点。

**关键文件**：
- `Assets/Scripts/Core/TimedPassiveModule.cs`
- `Assets/Scripts/Core/UpgradeEffectManager.cs`
- `Assets/Scripts/Managers/StageController.cs`
- `Assets/Scripts/RouteV2/RouteStageRuntimeV2.cs`

