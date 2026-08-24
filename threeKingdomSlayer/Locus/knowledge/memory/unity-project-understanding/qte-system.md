---
id: kd_6dc15d1a-77b7-4a98-a7b2-4bdb38d7d679
injectMode: inherit
aiEditMode: inherit
---

# QTE System

## Overview
QTE 系统由 `QTEController`（挂载在 Boss prefab 上）、`QTEDisplay`（挂载在 Canvas 上）、`QTEConfig`/`QTEAttackConfig`/`BossQTEData` ScriptableObject 组成。

## Key Components
- **QTEController**: 状态机 Idle→CoolingDown→WaitingForAttackFinish→PerformingQTEAttack→QTEJudging→QTECompleted
- **QTEDisplay**: 管理 QTE 指示器 prefab 的生成/销毁/动画
- **InputManager.TryConsumeQTEInput**: 对手势进行 QTE 判定

## Critical Configuration
- Canvas 是 `ScreenSpaceCamera` 模式，render camera 必须正确设置
- `RectTransformUtility.RectangleContainsScreenPoint` 和 `GetWorldCorners` 需要传入 camera 参数才能在 ScreenSpaceCamera 模式下正确工作
- QTE 指示器 prefab 位于 `Assets/Prefabs/QTE/`，config 位于 `Assets/ScriptableObjects/QTE/`

## QTE 输入交互规则
- **LegacyPassThrough（V1）**：QTE 优先尝试匹配；未命中手势穿透为普通攻击。攻击动作冷却期间禁止 QTE 交互（`AttackSystem.IsActionPlaying` 守卫）。
- **Strict（V2）**：`QTEController._inputRule` 控制。整个 `IsQTEActive` 生命周期（Performing、Judging、Ending）都必须消费战斗手势；即使当前 slot 已结算或正在收尾，也绝不能回退到普通攻击。提前做出当前 slot 所需手势、段间等待时做出所需手势、判定期的错误手势均判当前 slot 失败。
- Strict 生命周期：触发时冻结 `ComboManager`（恢复时补偿 `_lastHitTime`，保留剩余断连时间），灰显并禁用 `BuffDisplayPanel` 射线，阻止大招；完成、中止、数据切换、对象销毁均解除。
- `Assets/Resources/EnemyPrefabs/Enemy_104.prefab` 当前配置为 `Strict`；Inspector 可改回 `LegacyPassThrough`，运行时可调 `SetInputRule`。

## Strict 进入提示
- `QTEDisplay.ShowStrictModePrompt()` 复用 `HeroHUD.strictModePrompt`，触发 0.45 秒淡出。
- 提示节点为 `Assets/Prefabs/UI/HeroHUD_Zhangfei.prefab/HeroHUD_Zhangfei/HudCard/FlipPanel/BackFace/QTEFrame/StrictModePrompt`；Boss 交战时看板已在 BackFace，不额外翻牌。

## BOSS 免疫位移规则 (2025-07)
- BOSS始终免疫PushWave/DirectionalPush位移（`ApplyPushWave`/`ApplyDirectionalPush` 内部过滤 `isBoss`）
- PushWave调用处仅在有敌人被实际推动时才执行 `PostDisplacementFillUp`，防止无条件填充触发BOSS状态重置
- `Column.CompactByClearRows` 守卫列表包含 `QTEAttacking`，防止压缩时 `ResetMovementState` 中止QTE

## Known Fix: QTE 无法交互 (2024)
- **根因**: Canvas 为 ScreenSpaceCamera 模式，`IsClickInQTEArea` 调用 `RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos)` 未传 camera，始终返回 false
- **修复**: 新增 `GetQTECanvasCamera()` 获取 Canvas.worldCamera，传入 `RectangleContainsScreenPoint` 和 `GetWorldCorners`→`WorldToScreenPoint`

## Known Fix: Parry/Swipe 无法命中 BOSS (2024)
- **根因**: `ColumnManager.GetEnemiesInRange(rangeRows)` 仅按 rowIndex < rangeRows 过滤，BOSS 位于后排时被排除
- **修复**: 新增条件 `|| (e.isBoss && e.bossState == BossState.InCombat)` 始终包含已应战的 BOSS

## QTE 飞行物保护 (2025)
- `EnemyProjectile.isQTEProjectile`: QTE 箭矢标记，由 `QTEController` 在 `SpawnArrowWave()` 创建时设为 `true`
- `AttackSystem.ExecuteParry()`: 遍历 `FindObjectsOfType<EnemyProjectile>()` 时跳过 `isQTEProjectile` 为 true 的飞行物
- QTE 箭矢走独立的 `QTEController.DeflectArrowWave()` 反弹通道，不经过常规 Parry

## Single-Slot Sequential QTE (2026-07)
- `QTEController` now processes `QTEAttackConfig.qteSlots` as a sequential queue. Only `_currentQTEIndex` is instantiated and evaluated; after a resolved slot, the next slot starts after its own `QTESlot.delay` interval.
- `QTEConfig.screenPosition` is no longer used by `QTEDisplay` for battle QTE layout. Indicators always enter and stop at the center of `HeroHUD/QTEFrame`; the legacy field remains serialized for compatibility.
- Arrow waves are spawned only when their current slot appears, so defensive TripleStab waves no longer overlap before the player reaches later prompts.
- Verified in Play Mode against Boss 104 TripleStab: one indicator at a time; three programmatic valid clicks advanced indices 0→1→2→3 and returned the controller to Idle.
- `QTESlot.delay` inspector text now means first-slot initial delay / later-slot post-resolution interval, rather than an absolute attack-relative spawn time.

## Known Fix: Strict 提前输入导致 QTE 卡死 (2026-07)
- 根因：Strict 可在动画前摇结束前结算当前 slot 并推进 `_currentQTEIndex`，随后 `StartQTEPhase()` 又把索引重置为 0；已结算 slot 被重新选中后既不会再次超时，也无法推进，永久停在 `QTEJudging`。
- 修复：索引只在 `TriggerQTEAttack()` 初始化、只在 `ResolveQTE()` 递增；`StartQTEPhase()` 不再写索引，而是按当前索引设置下一段时间。
- 生命周期防护：QTE 使用 generation 隔离旧飞行物与延迟动画回调；数据切换在活跃 QTE 时先中止；禁用/回收对象时解除 Strict 锁、清除指示器/飞行物/箭矢并恢复 Animator speed。
- 该索引卡死主要由 V2 Strict 提前结算暴露；回收、数据切换及旧回调风险属于 V1/V2 共用状态机。

## QTE V2 验收与生命周期记录 (2026-07)
- Strict 输入、三联顺序槽位与完整生命周期清理已完成；提前失败后的整轮攻击演出/伤害仍需用户实机复验。
- TripleStab 的“判定反馈”和“攻击段完成”必须分离：提前输入或错误点击会立即显示 FAIL 并让当前指示器退场，但 slot 仍等到原 `judgeEndTime` 才调用 `ResolveQTE`，避免提前推进或提前触发 `QTEEnd`。
- `QTEAttackConfig_TripleStab.fixedQteDuration=4.5s` 是整轮 QTE phase 的最短演出时长；所有 slot 提前结算后仍不得在该时长前进入 Ending。防御型 QTE 还需等已发射箭矢/错峰发射回调完成后才允许统一清理，防止吞掉末段箭矢伤害。
- 三个点击指示器统一使用 `Assets/Sprites/BatlleHUD/QTE_Stab.png`；测试过程中产生的未追踪旧 Circle Clone 已确认并从运行现场清除。
- Boss 104 的 `launchDuration` 从 5 秒调整为 1.5 秒，使其正常进入浮空衰减/加速下落阶段。
- QTE 单段结果使用 `Assets/Sprites/BatlleHUD/QTEResults/QTE_SUCCESS.png` / `QTE_FAIL.png`，显示于 QTEFrame 右侧 `(175, 0)`；当前运行时尺寸为 510×255px，0.08 秒弹入 + 约 0.22 秒停留 + 0.12 秒淡出，不拦截输入并使用 unscaled 时间。
