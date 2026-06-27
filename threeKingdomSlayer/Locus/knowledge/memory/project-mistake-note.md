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
updatedAt: 1782549167866
---

# project-mistake-note

## Summary
更新至 2025-08-10 — 新增 BuffIcon raycastTarget=False 导致 UI 点击穿透

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
- 症状：Enemy_101 击飞落地后播放 HitFlash 动画而非直接回到 Idle
- 根因：`AttackWave.HitTarget()` 先调 `TakeDamage()`（设置 Hit trigger，此时 state 仍为 Stunned 而非 Launched），再调 `Launch()`。`Launch()` 的 `_animator.Play("Launched_Rise")` 不会清除已设置的 Hit trigger。落地切回 Idle 后，Idle→HitFlash 过渡（HasExitTime=False, If=Hit）立即捕获该遗留 trigger
- 修复：`Enemy.Launch()` 中 `_animator.Play("Launched_Rise")` 之前加 `_animator.ResetTrigger("Hit")`
- 预防规则：**动画状态切换前清理可能竞态的 trigger**，尤其是 `TakeDamage` 和 `Launch` 这种同一帧内先后调用的场景
- 文件：`Assets/Scripts/Enemy/Enemy.cs` (Launch)
<!-- locus:body:end -->
