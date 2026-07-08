---
id: kd_4a9116b1-c70a-4de3-8eeb-801deb71c4fe
type: memory
path: project-mistake-note.md
title: project-mistake-note
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1778764012219
updatedAt: 1783497897934
---

# project-mistake-note

## Summary
更新至 2025-12 — 新增 Cyclone 遮挡修复：战斗 2.5D 排序应优先用 Z 偏移，不要用高 sortingOrder 覆盖跨排深度

<!-- locus:body:start -->
### 自定义Editor中新增struct List字段不显示 Inspector 配置 ✅ 已修复（2025-06-27）
- 症状：`UpgradeDefinitionEditor` 中新加的 `DrawCycloneSection()` 已验证代码路径和数据均正确（`cycloneLevels` 5个元素、`effectType` 匹配、`FindProperty` 非null），但 Inspector 中不显示 Lv.1–Lv.5 配置
- 根因：Editor 脚本与源文件虽时间戳同步，但 Unity 未触发 domain reload / 重新编译，导致 Inspector 使用了旧版 Editor DLL（旧版无 `case "passive_timed_cyclone"` 分支）
- 修复：强制 `unity_recompile` 后恢复正常
- 预防规则：**新增 `[CustomEditor]` 分支或修改 Editor 脚本后，若 Inspector 不生效，先执行 `unity_recompile` 排除 DLL 过期问题，不要先怀疑代码逻辑**
- 文件：`Assets/Scripts/Editor/UpgradeDefinitionEditor.cs` (DrawCycloneSection)

### BuffIcon raycastTarget=False 导致 UI 点击穿透 ✅ 已修复（2025-08-10）
- 症状：点击血包 BuffIcon 时 `overUI=False`，InputManager 将点击降级为游戏 stab 攻击，血包无法使用
- 根因：BuffIcon 的 `Icon` / `Frame` 子级 Image 的 `raycastTarget` 在 Inspector 中设为 `false`。GraphicRaycaster 扫描时跳过这些 Graphic，`IsPointerOverGameObject()` 返回 `false`
- 修复：`BuffIcon.Setup()` 中对 `UpgradeCategory.Item` 类型显式设置 `_iconImage.raycastTarget = true`
- 预防规则：**所有需要点击交互的 UI 元素，其 Image 组件的 `raycastTarget` 必须为 `true`**。这是 GraphicRaycaster 命中检测的必要条件。Button 的 `targetGraphic` 仅影响按钮视觉过渡，不影响射线检测
- 文件：`Assets/Scripts/UI/BuffIcon.cs` (Setup)

### 新增 TimedPassiveModule 子类字段未在 Inspector 暴露 ✅ 已修复（2025-12）
- 症状：`CycloneEffect` 等 `TimedPassiveModule` 子类的新增 `[SerializeField]` 字段在 Inspector 中完全不显示，无法配置每级参数
- 根因：`TimedPassiveModule` 使用自定义 Editor (`TimedPassiveModuleEditor`)，该 Editor 通过 `SerializedProperty` 显式绘制已知字段。新增子类字段不会自动出现在 Editor 中，需在 Editor 脚本中添加对应的 `PropertyField` 绘制逻辑
- 修复：在 `TimedPassiveModuleEditor` 中添加 cyclone 相关字段的绘制分支
- 预防规则：**为使用自定义 Editor 的基类添加子类时，必须同步更新 Editor 脚本以暴露新字段。不要假设 `[SerializeField]` 会自动出现在自定义 Inspector 中**
- 文件：`Assets/Scripts/Editor/TimedPassiveModuleEditor.cs`

### 箭矢齐射 ArrowVolley 引用错误的 arrow prefab ✅ 已修复（2025-12）
- 症状：实现 ArrowVolley 时不确定该用哪个 arrow prefab，误用了 `ArrowRainEffect/ArrowTemplate` 的旋转值做额外修正
- 根因：项目中有两个 arrow prefab，职责不同：
  - `Assets/Prefabs/arrow.prefab` → 挂 `EnemyProjectile` 组件，QTE 系统专用（敌方弹丸），包含 arrowPart1/arrowPart2 子精灵
  - `Assets/Prefabs/Effects/ArrowRainEffect.prefab/ArrowTemplate` → 纯 `SpriteRenderer`，玩家侧箭雨/齐射特效视觉模板，`TimedPassiveModule.arrowEffectPrefab` 和 `PassiveTriggerModule.arrowEffectPrefab` 引用它
- 修复：ArrowVolley 使用 `_arrowVolleyTemplate`（SpriteRenderer）引用 `ArrowTemplate`，不修改其 prefab 自带旋转 `(270,0,0)`，直飞即可
- 预防规则：**使用 arrow 资源前先确认是玩家侧还是敌人侧。玩家侧特效用 `ArrowRainEffect/ArrowTemplate`（纯视觉），敌人侧弹丸用 `arrow.prefab`（带 EnemyProjectile 逻辑）。不要对 ArrowTemplate 做额外旋转修正，prefab 自带朝向已是正确方向**
- 文件：`Assets/Scripts/Core/PassiveTriggerModule.cs` (ExecuteArrowVolley / FireArrow)

### AttackWave/SweepEffect 默认 alpha=0.85 + Color.Lerp 洗白 + GetColor 染色导致 prefab 精灵颜色异常 ✅ 已修复（2025-12）
- 症状：所有玩家攻击特效（Stab/Slash/Pierce/Sweep/Launch/Parry）的 prefab 精灵显示偏色和半透明
- 根因：三处叠加——(1) `CreateInternal` 默认 `alphaOverride ?? 0.85f`，(2) 正常路径 `Color.Lerp(color, Color.white, 0.5f)` 洗白，(3) `GetColor(damageType)` 将类型颜色直接乘到材质。此外 `PlayLaunchVisual`/`PlayParryVisual` 硬编码了 `launchColor`/`parryColor`
- 修复：默认 alpha → 1.0f，移除 Lerp 洗白，prefab 路径统一 `Color.white`
- 预防规则：**对精灵 prefab 应用材质颜色会做乘法混合，要显示原图必须用 `Color.white`。不要在无 prefab 的 quad 和精灵 prefab 间共用同一套颜色逻辑**
- 文件：`AttackWave.cs`, `SweepEffect.cs`, `AttackSystem.cs` (PlayLaunchVisual/PlayParryVisual)

### Enemy Launch 后 Hit trigger 竞态导致落地播放 HitFlash ✅ 已修复（2025-12）
- 症状：Enemy_101 击飞落地后播放 HitFlash 动画而非直接回到 Idle
- 根因：`AttackWave.HitTarget()` 先调 `TakeDamage()`（设置 Hit trigger，此时 state 仍为 Stunned 而非 Launched），再调 `Launch()`。`Launch()` 的 `_animator.Play("Launched_Rise")` 不会清除已设置的 Hit trigger。落地切回 Idle 后，Idle→HitFlash 过渡（HasExitTime=False, If=Hit）立即捕获该遗留 trigger
- 修复：`Enemy.Launch()` 中 `_animator.Play("Launched_Rise")` 之前加 `_animator.ResetTrigger("Hit")`
- 预防规则：**动画状态切换前清理可能竞态的 trigger**，尤其是 `TakeDamage` 和 `Launch` 这种同一帧内先后调用的场景
- 文件：`Assets/Scripts/Enemy/Enemy.cs` (Launch)

### Stab PushWave RecheckAttackRange 被 CompactByClearRows 覆写导致击退后不补齐 ✅ 已修复（2025-12）
- 症状：Stab PushWave 击退敌人后，第一个被击退的敌人不 rush 回攻击范围（row 0），留在被击退的位置
- 根因：`ExecutePush` 中调用 `RecheckAttackRange` 设置了 targetRow，但后续 `PostDisplacementFillUp` → `CompactByClearRows` 按列紧凑重新分配 targetRow，覆写了 RecheckAttackRange 的结果
- 修复：将 RecheckAttackRange 移到 PostDisplacementFillUp 之后执行：`ApplyPushWave` → `PostDisplacementFillUp` → `RecheckPushedEnemiesAttackRange`。同时 `CompactByClearRows` 中 Boss 跳过（`if (e.isBoss) continue`）
- 预防规则：**位移系统中任何在紧凑（CompactByClearRows）之前设置的 targetRow 都会被紧凑覆写。需要基于攻击范围重新计算 targetRow 的逻辑必须在紧凑之后执行**
- 文件：`ColumnManager.cs` (ExecutePush / ApplyPushWave / RecheckPushedEnemiesAttackRange), `AttackSystem.cs` (ApplyStabPushWave), `Column.cs` (CompactByClearRows)

### Stab PushWave → RecheckAttackRange 覆写 Launched 状态导致浮空敌人瞬间落地 ✅ 已修复（2025-12）
- 症状：Launch 击飞敌人后，Stab 攻击浮空敌人时敌人被瞬间击退到后排并落地，无法维持浮空状态进行连段。Slash/Pierce/Sweep 无此问题
- 根因：Stab 的位移链 `ApplyStabPushWave` → `ExecutePush` → `RecheckAttackRange()` 无条件覆写 `state`：`rowIndex >= atkRange` 分支将 `Launched` 覆写为 `Idle` 再 `StartMoving` → `Moving`，`UpdateLaunch` 物理循环检测到 state 不再是 `Launched` 后立即停止，敌人落地。Slash 的 `ApplyDirectionalPush` → `MoveEnemyToColumnAtRow` 不调用 `RecheckAttackRange`，故不受影响
- 修复：`Enemy.RecheckAttackRange()` 顶部加 `if (state == EnemyState.Launched) return;`，浮空敌人位移后留在新位置继续浮空直到自然落地
- 预防规则：**任何可能被位移系统调用的状态变更方法（尤其是 `RecheckAttackRange`）必须显式守卫特殊状态（`Launched`、`Stunned` 等），不应假设当前 state 一定是常规战斗状态。所有新位移方法若内部调用 `RecheckAttackRange` 将自动受此守卫保护**
- 文件：`Assets/Scripts/Enemy/Enemy.cs` (RecheckAttackRange)

### PerColumn 击退补齐延迟位置错误导致距离=1 无延迟 ✅ 已修复（2025-01）
- 症状：击退距离=1 时，敌人被击退后立刻播放 Rush 补齐动画返回原位，无可见延迟
- 根因（两层）：
  1. 第一层：0.35s 延迟放在 `OnCompactionChainComplete`（紧凑链完成 → 波次行军之间），但玩家的 Rush 补齐感知发生在**紧凑链阶段**（`StartAllCompactionChains`）。修复：将 `Invoke` 移到 `RowBasedFillUp` 和 `StartAllCompactionChains` 之间
  2. 第二层（真正根因）：`RecheckAttackRange`（由 `RecheckPushedEnemiesAttackRange` 调用）在 `else` 分支中调用了 `StartMoving(isRush: true)`，**完全绕过了延迟**。修复：从 `RecheckAttackRange` 的 `else` 分支移除 `StartMoving`，仅设置 `targetRow` + `pendingRushMove`，由延迟后的紧凑链统一启动 Rush
- 预防规则：**任何在补齐流程中可能被调用的方法，如果内部会 `StartMoving`，必须确认它不会绕过 ColumnManager 的延迟/链调度**
- 文件：`ColumnManager.cs` (PostDisplacementFillUp), `Enemy.cs` (RecheckAttackRange)

### Boss 补齐入口 TriggerAllBossFillForward 零调用者导致 Boss 永远不补齐 ✅ 已修复（2025-01）
- 症状：Boss 停在远处不向前补齐，始终不与玩家交战
- 根因：PerColumn 重构时，Boss 补齐触发代码从 `Column.TriggerFillForward()` 内联提取为独立方法 `Column.TriggerBossFillForward()`，由 `ColumnManager.TriggerAllBossFillForward()` 统一调用。但重构废弃 `TriggerFillForward` 的原有调用点后，`TriggerAllBossFillForward()` 未被任何代码调用（零调用者）。Boss 永远得不到 `pendingRushMove=true`
- 修复：在 `WaveSpawner.SpawnNextWave()` 和 `StageController.OnChoicesDoneSpawnNextWave()` 中 `StartWaveMarch()` 之后添加 `TriggerAllBossFillForward()` 调用
- 预防规则：**提取方法到新入口时，必须同步确认所有调用点已正确迁移。废弃旧入口前 grep 确认无遗漏调用者**
- 文件：`ColumnManager.cs` (TriggerAllBossFillForward), `WaveSpawner.cs` (SpawnNextWave), `StageController.cs` (OnChoicesDoneSpawnNextWave)

### PerColumn 多排秒杀后 _pendingWaveEnemies 死锁导致后排永不补齐 ✅ 已修复（2025-01）
- 症状：玩家同时击杀多排敌人后，后排敌人原地不动永不补齐
- 根因：波次行军期间，已阵亡的敌人从 `_pendingWaveEnemies` 中移除但未触发 `OnWaveEnemyRushComplete`，导致 `_pendingWaveEnemies.Count` 永远不为 0，`_isWaveMarching` 永久为 true。后续 `StartWaveMarch()` 因 `_isWaveMarching` 守卫直接 return，波次行军彻底死锁
- 修复：`RemoveEnemyFromColumn` 中检测被移除敌人是否在 `_pendingWaveEnemies` 中，若在则清理订阅并重置 `_isWaveMarching` / `_currentWaveSourceRow`
- 预防规则：**任何持有敌人引用的状态集合（HashSet/List），在敌人死亡移除时必须清理该集合中的引用，否则状态机可能永久卡死**
- 文件：`ColumnManager.cs` (RemoveEnemyFromColumn)

### PerRow 单排秒杀后无补齐：RemoveEnemyFromColumn 缺少 StartWaveMarch + _pendingWaveEnemies 清理 ✅ 已修复（2025-01）
- 症状：PerRow 模式下，单一排敌人被一次性全部击杀后，后排敌人不向前补齐
- 根因（两层）：
  1. PerRow 分支只调用 `RowBasedFillUp()`（数据模型压缩，设置 targetRow + pendingRushMove），但从未调用 `StartWaveMarch()` 启动实际 Rush 移动。`CompactByClearRows` 注释明确写了"链式补齐由 ColumnManager 统一启动"，但 ColumnManager 的 PerRow 分支没有启动
  2. PerRow 分支缺少 `_pendingWaveEnemies` 清理（与坑点8相同模式），若死亡敌人正在波次行军中会导致 `_isWaveMarching` 死锁
- 修复：将 `_pendingWaveEnemies` 清理和 `RemoveEnemy` 提取到两个分支共用，PerRow 分支在 `RowBasedFillUp()` 后调用 `StartWaveMarch()`，与 PerColumn 分支结构对齐
- 预防规则：**修改 PerColumn 分支的死锁/补齐修复时，必须同步检查 PerRow 分支是否需要相同修复。两个分支共享 `_pendingWaveEnemies`、`_isWaveMarching` 等状态，但触发补齐的方式不同**
- 文件：`ColumnManager.cs` (RemoveEnemyFromColumn)

### Inspector 参数位置描述不清导致用户找不到配置位置 ✅ 规则纠正（2025-12）
- 症状：告知用户"在 Inspector 中调整 visualScale"但未说明是哪个 GameObject 的哪个组件，用户无法定位
- 预防规则：**每当提示用户在 Inspector 中调整参数时，必须完整指明路径：Hierarchy 中选中哪个 GameObject → Inspector 中找到哪个组件 → 调整哪个字段。例："在 Hierarchy 中选中 `Player`，然后在 Inspector 中找 `ChargeStabVisual` 组件，调整 `Visual Scale` 字段"。不能只说"在 Inspector 调整 X"**

### Cyclone 遮挡修复误用 sortingOrder 导致跨排遮挡 ✅ 已修复（2025-12）
- 症状：CycloneEffect 生效时会错误遮挡敌人；先尝试用 `sortingOrder = 50 - z * 10`，又降到 `10 - z`，但 row=0/1 等靠前排仍有遮挡异常
- 根因：项目战斗场景是透视相机 + Z 深度的 2.5D 排序体系，敌人和地面特效主要保持 `sortingOrder=0`，通过 Z 位置决定前后关系。给 Cyclone 设置全局高于敌人的 sortingOrder 会绕过 Z 深度，让后排 Cyclone 也压到前排敌人之上
- 正确修复：Cyclone 保持 `_sr.sortingOrder = 0`，生成位置使用目标敌人脚下坐标并 `pos.z -= 0.2f`，像 SpikeTrap 的 `zOffset=-0.2` 一样靠 Z 轻微前移显示在目标敌人身前，同时保留跨排深度关系
- 调试辅助：临时/保留 `DebugLog.Info($"[CycloneEffect] target={_target.DebugTag} row={_target.rowIndex} z={pos.z:F2}")` 可确认生成 row 和 Z
- 预防规则：**在本项目战斗内修复敌人/地面特效遮挡时，优先检查现有 2.5D Z 排序体系；不要先用大 sortingOrder 覆盖。只有纯 overlay/描边/UI 类视觉才适合高 sortingOrder**
- 文件：`Assets/Scripts/Effect/CycloneEffect.cs` (Setup), `Assets/Scripts/Core/SpikeTrapController.cs` (zOffset / baseOrder=0)
<!-- locus:body:end -->
