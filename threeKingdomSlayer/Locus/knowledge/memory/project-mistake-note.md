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
updatedAt: 1779692595852
---

# project-mistake-note

## Summary
更新至 2025-07-18 — 三选一系统错误汇总 + QTE/虚幻武器未修复问题

<!-- locus:body:start -->
### Stab Wave 视觉旅行方向错误 ✅ 已修复（2025-07-18）
- 症状：戳击 wave 视觉上穿过空排飞到错误位置，而非走到目标敌人处。当 row0 有敌人、rows 1-2 为空时，wave 反向飞向 row3 方向
- 根因：`AttackWave.SetupTravel` 中 stab 用 `closestZ` 判断方向，`endTravelZ = closestZ ± 2.5` 的设计假设 wave 在目标前方足够远处生成。但实际 wave 固定生成在 Z=0.5（prefab Z + zOffset），而敌人可能处于负 Z。当 startZ(0.5) > targetZ(-1.0) 时 `closestZ + 2.5 = 1.5`，DOTween 从 0.5→1.5 正方向移动，与目标方向相反
- 修复：stab 改为向 **最远目标**（furthestZ）方向旅行，`endTravelZ = furthestZ`，wave 从 player 侧直走到范围内最远敌人处再收回。当 rangeRows 增大（Buff）时自然走到新范围内最远排。非 stab（Pierce/Sweep）逻辑不变
- 预防规则：Travel 型 wave 的 `startZ`（固定 spawn 点）和 `endTravelZ` 必须确保在空间同一侧，否则 DOTween 移动方向与视觉预期相反
- 文件：`Assets/Scripts/Attack/AttackWave.cs` (SetupTravel)

### CanvasGroup.blocksRaycasts 导致全屏点击拦截 🔁 反复出现（2025-07-18）
- 症状：新建/替换 Canvas 后，游戏内所有交互失效（攻击、按钮等），看起来像输入系统挂了
- 根因：CanvasGroup 组件默认 `blocksRaycasts = true`。即使 Canvas 透明（alpha=0），只要 Canvas 覆盖全屏且 sortingOrder 高于其他 UI，就会吃掉所有点击事件
- 历史：暂停菜单出现过（commit `af1b4e1` — 点击穿透修复），三选一弹窗又出现一次
- 预防规则：**任何带 CanvasGroup 的全屏/覆盖式 Canvas，必须在初始化时同步设置 `blocksRaycasts = false`；显示时设为 `true`，隐藏时立即设为 `false`**。这包括：暂停菜单、升级弹窗、GameOver 面板、任何半屏以上覆盖层
- 检查方法：出问题时在 Inspector 中逐个关闭 Canvas（禁用 GameObject），确认交互恢复后检查该 Canvas 的 CanvasGroup.blocksRaycasts

### 全屏 Image.raycastTarget 同源问题（2025-07-19）
- 与 CanvasGroup.blocksRaycasts 同源：Unity Image 组件默认 `raycastTarget = true`，全屏覆盖 Image 同样会拦截所有 Raycast 输入
- PlayerHitFeedback 中的 HittedOverlay 已在 Start() 中显式设置 `raycastTarget = false`，且通过 unity_execute 创建时也设为 false
- 预防规则：任何全屏覆盖的纯视觉 UI 元素（边框、闪屏、暗幕），必须设 `raycastTarget = false`
- 文件：`Assets/Scripts/Player/PlayerHitFeedback.cs`

### 9-slice Sprite Border = 0 导致 Sliced Image 不拉伸 ✅ 已修复（2025-07-18）
- 症状：UpgradePopup/UpgradeCard 使用了 Image Type=Sliced，但背景图无论 ContentSizeFitter 如何调整，视觉上都不拉伸
- 根因：sprite 的 border 为 (0,0,0,0)。Unity 的 Sliced（9-slice）模式依赖 sprite border 定义中间可拉伸区域和四角保护区。border=0 时整张图被当作角部，不参与拉伸
- 修复：`background_31_outside.png` border 设为 (35,35,35,35)；`background_31_inside.png` border 设为 (20,20,20,20)。通过 TextureImporter.spriteBorder 设置并 Reimport
- 预防规则：任何用作 Sliced Image 的 sprite，导入后必须在 Sprite Editor 中设置非零 Border，或在导入脚本中自动设置
- 文件：`Assets/Sprites/31Reward/background_31_outside.png`、`background_31_inside.png`

### InputManager Debug.Log 帧刷屏 ✅ 已修复（2025-07-18）
- 症状：`[InputManager] Update frame=...` 每帧打印，淹没 Console 其他日志
- 修复：注释掉 `Assets/Scripts/Player/InputManager.cs:113` 的 Debug.Log
- 预防规则：高频日志（Update/FixedUpdate 中）应使用 `#if UNITY_EDITOR` 或条件编译开关，默认关闭

### UpgradeChoicePopup.cardPrefab UnassignedReferenceException ⚠️ 反复出现（2025-07-18）
- 症状：触发三选一时 `UnassignedReferenceException: The variable cardPrefab of UpgradeChoicePopup has not been assigned`，游戏卡死
- 根因分析：`cardPrefab` 在 Prefab 和 Scene 实例中均已正确赋值（当前 Editor 状态验证通过），但运行时偶现 null。可能原因：① 修改场景/Prefab 后未保存即进入 Play Mode；② 脚本修改后未 `unity_recompile` 导致旧代码读取新序列化数据失败；③ 存在多个 `UpgradeChoicePopup` 实例（一个正确、一个遗漏）
- 预防规则：每次修改 `.prefab` / `.unity` / `.cs` 后必须：保存场景 → `unity_recompile` → 确认编译通过 → 再进入 Play Mode
- 当前状态：Editor 中 cardPrefab 已正确赋值，需实际 Play Mode 验证
- 文件：`Assets/Scripts/UI/UpgradeChoicePopup.cs:87`

### UpgradePopup/UpgradeCard 背景图不显示 ✅ 已修复（2025-07-18）
- 症状：Image 组件已拖入 source image，但游戏中完全不显示
- 根因：素材图片尺寸过大（或格式问题），Unity 无法正确渲染
- 修复：用户更换图片素材后问题解决
- 预防规则：新导入的 UI 图片建议控制在 2048×2048 以内，优先使用 PNG 格式

### QTEController 始终 Idle / QTE 无法触发 ⚠️ 未修复（2025-07-18）
- 症状：QTE 反击无法交互，QTEController._state 始终为 Idle
- 已排除：CanvasGroup 阻挡（QTE Canvas 无 CanvasGroup）、QTE 配置缺失（qteData/prefabs 均已配置）、InputManager.skillInputEnabled/blockInputFrames 阻挡（均为正常值）
- 待查：Boss 是否进入 InCombat → OnBossEngaged 是否触发 → 冷却计时器是否启动
- 文件：`Assets/Scripts/QTE/QTEController.cs`

### 虚幻武器待验证（2025-07-18）
- 代码路径验证通过：PassiveTriggerModule 订阅 OnAttackPerformed ✅、PhantomWeapon 在奖池 ✅、ApplyUpgrade 路由正确 ✅
- 当前测试中玩家 Lv=0 未获取任何升级，无法实测触发。待玩家升级获取后验证

### QTE Prefab 交互被 Canvas 阻挡 🔁 与 CanvasGroup 同类问题（2025-07-18）
- 症状：QTE Prefab 无法点击/滑动交互
- 根因：同 CanvasGroup.blocksRaycasts 问题 — 其他 Canvas（如 HUD、弹窗）的 GraphicRaycaster 拦截了 QTE Canvas 的输入事件
- 排查：确认 QTE Canvas sortingOrder 是否大于所有常驻 Canvas；确认其他 Canvas 是否有未关闭的 CanvasGroup blocksRaycasts=true
- 预防规则：弹窗/暂停/HUD Canvas 的 blocksRaycasts 状态必须在显隐逻辑中同步维护
<!-- locus:body:end -->
