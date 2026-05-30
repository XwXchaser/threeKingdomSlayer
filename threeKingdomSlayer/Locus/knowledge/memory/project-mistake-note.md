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
updatedAt: 1780165889786
---

# project-mistake-note

## Summary
更新至 2025-08-07 — 新增 Animator AnyState 转移错误打断动画规则 + 敌人动画状态机设计规范 + EnemySpriteController 空实现问题

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
- 当前状态：**已废弃 — cardPrefab 字段已删除，改为预置3张卡片**
- 文件：`Assets/Scripts/UI/UpgradeChoicePopup.cs:87`

### UpgradePopup/UpgradeCard 背景图不显示 ✅ 已修复（2025-07-18）
- 症状：Image 组件已拖入 source image，但游戏中完全不显示
- 根因：素材图片尺寸过大（或格式问题），Unity 无法正确渲染
- 修复：用户更换图片素材后问题解决
- 预防规则：新导入的 UI 图片建议控制在 2048×2048 以内，优先使用 PNG 格式

### IsPointerOverGameObject 拦截 UI-based 游戏交互 🔁 新类别（2025-07-19）
- 症状：QTE 指示器正常显示，但玩家点击/滑动永远无法命中判定。Console 无任何 QTE 成功的日志，也无 InputManager 的 MouseDown/MouseUp 日志
- 根因：QTE 指示器是 Canvas 下的 UI 元素（Graphic 有 RaycastTarget）。`InputManager.HandleMouseInput()` 在 `MouseDown` 时调用 `EventSystem.current.IsPointerOverGameObject()`，检测到指示器后判定为"点击了 UI"，直接 `return` 丢弃输入。`isTouching` 保持 false，后续 `MouseUp` 也不会触发 `ProcessGesture`，QTE 输入被完全阻断
- 修复：在 overUI 检查中增加 `!IsAnyQTEActive()` 条件。QTE 活跃时放行 UI 上的点击/触摸，让输入经 `ProcessGesture` → `TryConsumeQTEInput` 进入 QTE 判定。同理修复触摸输入（`HandleTouchInput` 的 TouchBegan）
- 预防规则：**当 UI 元素本身就是游戏交互目标（非菜单/按钮），且通过 `ProcessGesture` 路由输入时，overUI 检查必须为这些元素放行**。不要仅凭 `IsPointerOverGameObject` 就丢弃输入，要考虑"当前是否有 UI-based 的游戏交互在等待输入"
- 文件：`Assets/Scripts/Player/InputManager.cs` (HandleMouseInput line ~164, HandleTouchInput line ~254, IsAnyQTEActive)

### ⚠️ 核心规则：代码不得覆写用户在 Inspector 中手动设置的值 🔁 反复出现（2025-07-19，更新于 2025-07-20）
- 症状：用户在 Prefab 中手动调整了 Content 的 RectTransform、子节点的 rotation/scale/sprite 等属性，但运行时 / Awake / OnValidate 中被代码重置为硬编码值
- 案例 1：`FlexibleFrame.EnsureChildren()` 对已存在的子节点仍覆写 anchorMin/Max、pivot、sizeDelta、anchoredPosition → **✅ 已修复：所有 RectTransform 赋值移入 `if (!existed)` 块内，已存在节点完全不触碰**
- 案例 2：`UpgradeChoicePopup.ApplyPadding()` 硬编码覆写 `contentRect.anchoredPosition` 和 `contentRect.sizeDelta` → **✅ 已修复：删除 contentRect 覆写代码及 _padding* 字段，移除 ApplyPadding() 方法**
- 案例 3：`UpgradeChoicePopup.ApplySpacing()` 覆写 VerticalLayoutGroup.spacing → **✅ 已修复：改为预置3张卡片，删除所有动态生成和spacing覆写**
- 预防规则：
  1. **Awake/Start/OnValidate 中只能读取 Inspector 值，不得写回硬编码默认值**
  2. 如需初始化 RectTransform 布局，在 Editor 脚本或 Prefab 阶段做，不在运行时做
  3. 任何对 `anchorMin/anchorMax/pivot/localRotation/localScale/anchoredPosition/sizeDelta` 的赋值都是高危操作，必须先确认是否会覆盖用户手动调整
  4. 子节点的创建/销毁必须在 Editor 时完成（Prefab 阶段），运行时只能使用已有节点
  5. 设计组件时遵循「配置与布局分离」：配置（Inspector 序列化字段）由用户控制，布局（运行时计算）仅读取配置来调整派生值
  6. `OnValidate()` 中不得调用会覆写 Inspector 序列化字段的方法
- 文件：`Assets/Scripts/UI/FlexibleFrame.cs`（已删除）、`Assets/Scripts/UI/UpgradeChoicePopup.cs`

### 代码创建GameObject未串接组件字段 🔁 新类别（2025-07-20）
- 症状：通过 unity_execute 代码创建 Card1/2/3 的完整GameObject结构后，UpgradeCard 组件的 public 字段（backgroundImage/nameText/descriptionText/button/iconImage）全部为 None，Setup() 中 `if (xx != null)` 全部跳过，UI 无任何数据显示
- 根因：代码创建 GameObject 并 AddComponent 后，未将子节点的组件引用赋值到脚本的序列化字段。Unity 不会自动串接这些引用
- 修复：创建完成后用代码逐一串接：`uc.backgroundImage = cardT.GetComponent<Image>()` 等
- 预防规则：**代码创建含脚本组件的 GameObject 后，必须检查该脚本所有 `public` / `[SerializeField]` 字段是否需要手动串接子节点/同级组件引用**
- 文件：`Assets/Prefabs/UI/UpgradePopup.prefab`、`Assets/Scripts/UI/UpgradeCard.cs`

### TMP 字体不支持中文（2025-07-20）
- 症状：3选1卡片的 NameText 和 DescriptionText 无法显示中文，只显示方块或空白
- 根因：代码创建的 TextMeshProUGUI 默认使用 LiberationSans SDF（西文字体），不包含中文字形
- 修复：将字体改为 `Assets/Fonts/方正粗黑宋简体 SDF.asset`（项目中已有的中文字体）
- 预防规则：创建含中文文本的 TMP 组件时，必须显式设置 font 为中文字体

### Image.color 覆写导致 Sprite 被染色（2025-07-20）
- 症状：用户已在 prefab 中为卡片设置好背景 sprite，但运行时背景颜色和其他卡片不一致（更深/更蓝/更金）
- 根因：`UpgradeCard.Setup()` 调用 `GetRarityColor(def.rarity)` 覆写 `backgroundImage.color`（Common灰/Rare蓝/Legendary金），染色了用户设置好的 sprite
- 修复：移除 `GetRarityColor()` 调用及相关字段（commonColor/rareColor/legendaryColor），不再覆写 backgroundImage.color
- 预防规则：**如果用户已通过 sprite 控制UI外观，代码不得额外覆写 Image.color**
- 文件：`Assets/Scripts/UI/UpgradeCard.cs`

### UpgradeCard.prefab 删除后 Missing Prefab 残留（2025-07-20）
- 症状：用户删除 UpgradeCard.prefab 后，UpgradePopup.prefab 中的 Card1/2/3 变为 Missing Prefab，且无法 Unpack
- 根因：PrefabUtility.UnpackPrefabInstance 在源 prefab 已删除时无法正常工作
- 修复：删除 Missing Prefab 实例，用代码重建完整 GameObject 结构（Image+Button+CanvasGroup+UpgradeCard+LayoutElement 及子节点）
- 预防规则：删除 prefab 前先确保没有其他 prefab/scene 通过 prefab instance 引用它

### ItemInventory 组件未挂载到场景 ✅ 已修复（2025-08-06）
- 症状：道具选择后 BuffDisplayPanel 不显示图标，ItemInventory.AddItem 调用无效果（Instance 为 null）
- 根因：ItemInventory.cs 依赖 Awake() 中的 Instance 赋值，但没有任何 GameObject 挂载该组件。整个道具存储/消耗/事件链路完全断裂
- 修复：通过 unity_execute 将 ItemInventory 组件添加到 Manager GameObject（与 UpgradeEffectManager 同级）
- 预防规则：任何单例组件必须确认场景中至少有一个 GameObject 挂载了它。创建新单例时同步在场景/初始化流程中挂载
- 文件：`Assets/Scripts/Core/ItemInventory.cs`、`Assets/Scenes/Battle.scene`

### BuffIcon._button 未在 Prefab 中串接 ✅ 已修复（2025-08-06）
- 症状：TestDamageBoost 道具图标显示正常，但点击无响应（OnItemIconClicked 不触发），图标不消失，伤害加成不生效
- 根因：BuffIcon.prefab 中 `_button` 字段未在 Inspector 中拖入 Button 组件引用。Setup() 中 `if (_button != null)` 跳过，Button.onClick 从未绑定
- 修复：在 Prefab 中将子节点 Button 组件拖入 BuffIcon 的 `_button` 字段
- 预防规则：**预制体中的 `[SerializeField]` 字段（尤其是 Button、Image、TMP 等 UI 组件引用）创建后必须逐个确认已串接**。与「代码创建GameObject未串接组件字段」同源
- 文件：`Assets/Prefabs/UI/BuffIcon.prefab`、`Assets/Scripts/UI/BuffIcon.cs`

### Slider 填充条透明度问题 🔁 反复出现（2025-08-06）
- 症状：Slider 的 Fill Image 在游戏中始终半透明/透明，即使 Image.color.a=1、CanvasGroup.alpha=1、Sprite 已拖入
- 根因（两层）：
  1. **Fill Image type 用了 Filled**：Unity Slider 对 Simple 和 Filled 类型的 fillRect 处理机制不同。Simple 通过 RectTransform 裁剪控制填充，Filled 通过 fillAmount 控制。两者同时存在时冲突，fillAmount=0 导致 Image 渲染为空。**项目中已验证的正确模式：fillRect Image 必须用 Simple 类型**（参考 HeroHUD_Zhangfei health bar）
  2. **Sprite 导入设置 alphaIsTransparency=False**：UI Sprite 必须设 `alphaIsTransparency=True`，否则 Unity 不将 alpha 通道用于透明度混合，导致渲染异常。**任何用作 UI Image 的 sprite，导入后必须确认 alphaIsTransparency=True**
- 修复：① ProgressSlider Fill Image type=Filled→Simple；② `slider_stage_siller.png` alphaIsTransparency=False→True
- 预防规则：**新建 Slider 填充条时：① fillRect Image type 必须为 Simple（不是 Filled）；② fill sprite 的 alphaIsTransparency 必须为 True**
- 文件：`Assets/Scenes/Battle.scene` (ProgressSlider)、`Assets/Sprites/BatlleHUD/slider_stage_siller.png`

### Animator AnyState 转移导致动画被错误打断 ✅ 已修复（2025-08-07）
- 症状：敌人被击飞后 Launched 精灵只闪现一瞬间就回到 Idle；敌人在 Launched 状态下受击会切换到 HitFlash（受击动画）而非保持 Launched 动画
- 根因：三个敌人 Animator Controller 中均配置了 `AnyState → HitFlash`（Hit trigger 条件）。AnyState 转移的优先级高于当前状态的转移，当 `SetTrigger("Hit")` 在 Launched 期间触发时，Animator 直接从 Launched 跳到 HitFlash，0.9s 后回到 Idle。Launched 动画被打断，表现为"敌人还没落地就站起来了"
- 修复：
  1. 移除所有控制器的 AnyState→HitFlash
  2. 改为从**可被打断的状态**添加显式 Hit trigger 转移：
     - 101/102：Idle→HitFlash、Attack→HitFlash
     - 103：Idle→HitFlash、CAttack1→HitFlash、CAttack2→HitFlash、CAttack3→HitFlash
  3. Launched、Dead 状态不添加 HitFlash 转移，从 Animator 层面阻止受击动画偷走击飞动画
  4. 代码侧 `HitFlashRoutine()` 已有守卫（Launched/Dead/QTEAttacking/isCFrame 不 SetTrigger），形成 Animator + 代码双重保险
- 预防规则：
  1. **AnyState 转移只能用于真正"从任何状态都应该触发"的场景（如 Dead）**。对于条件性的打断（如受击动画），必须用显式状态转移
  2. 设计 Animator 时先列出所有状态，明确哪些状态之间可以互相转移，再逐条添加。不要用 AnyState 偷懒
  3. 当"某些状态不应该触发某个 Trigger"时，必须同时在 Animator 层（不添加对应转移）和代码层（SetTrigger 前检查状态）做双重守卫
  4. 修改 Animator Controller 后必须在 Play Mode 中验证：击飞→空中受击→落地恢复 全流程
- 文件：`Assets/Animations/Enemy_101.controller`、`Enemy_102.controller`、`Enemy_103.controller`、`Assets/Scripts/Enemy/Enemy.cs` (HitFlashRoutine)

### 敌人动画状态机设计规范（2025-08-07）
- 所有普通敌人（非Boss）共用同一套 Animator 状态机逻辑：
  - **Idle**（默认状态）：循环播放 Idle 动画
  - **Attack**（101/102）：Trigger 进入，HasExitTime 自动回 Idle。Attack 动画是单次播放的非循环 clip（如2帧精灵序列）
  - **CAttack1→CAttack2→CAttack3→Idle**（103 C技链）：HasExitTime 链式转移，每一步播完自动进入下一步，最后回 Idle。CAttack 由代码通过 CAttack trigger 触发，代码在 CA 结束后设 variant 回 0
  - **Launched**：AnyState→Launched（Launch trigger），无自动回 Idle 的转移。代码 `UpdateLaunch()` 控制落地时机，落地时 `animator.Play("Idle")` 切回
  - **Dead**：AnyState→Dead（Dead trigger），终端状态无离开转移
  - **HitFlash**：从 Idle/Attack/CAttack1-3 通过 Hit trigger 转移进入，HasExitTime(0.9s)→Idle。不在击飞/死亡时触发
  - **Stunned**：代码层状态，Animator 保持 Idle。代码控制眩晕计时和退出
- 时序关系：spawnDuration（前摇）= 攻击判定窗口开始前的时间；drawDuration（收招）= 攻击判定结束后的等待时间。Attack/CAttack 动画应在 spawnDuration 内播完，drawDuration 期间保持最后一帧，然后回 Idle
- 文件：`Assets/Animations/Enemy_101.controller`、`Enemy_102.controller`、`Enemy_103.controller`、`Assets/Scripts/Enemy/Enemy.cs`

### EnemySpriteController.TriggerHitFlash() 为空实现（2025-08-07）
- 症状：Enemy.cs 的 `TriggerHitFlash()` 原本委托给 `_spriteCtrl?.TriggerHitFlash()`，但 EnemySpriteController 从未挂载到任何敌人 Prefab 上，调用始终为空操作。受击闪烁实际上只靠材质 flash（白色闪白），没有精灵替换的红色闪烁效果
- 根因：EnemySpriteController 组件从未被添加到 101/102/103 的 Prefab 中
- 修复：将 `TriggerHitFlash()` 改为启动 `HitFlashRoutine` 协程，通过 Animator 的 HitFlash 状态播放受击动画（HitFlash clip 为单帧精灵短暂显示）。EnemySpriteController.TriggerHitFlash() 保留但当前未使用
- 预防规则：添加新组件时同步确认所有需要它的 Prefab 已挂载该组件
- 文件：`Assets/Scripts/Enemy/Enemy.cs`、`Assets/Scripts/Enemy/EnemySpriteController.cs`
<!-- locus:body:end -->
