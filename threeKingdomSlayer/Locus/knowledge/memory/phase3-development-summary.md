---
id: kd_04b72df3-abb3-4ca2-941d-83941c56fa62
type: memory
path: phase3-development-summary.md
title: phase3-development-summary
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1779520344306
updatedAt: 1779631193601
---

# phase3-development-summary

## Summary
第三期局内成长系统（经验三选一）开发状态 — 含虚幻武器被动奖励、UI弹窗重构、Bug修复记录

<!-- locus:body:start -->
# 第三期：局内成长系统（经验三选一）开发状态

## 架构概览

```
Enemy dies → EnemyManager.SpawnGem(世界坐标, expReward, enemy.gemSprite)
  → ExpGemManager 生成屏幕空间 UI Image（Canvas 子对象）
  → ExpGem.Update() 飞行（RectTransform.position 屏幕坐标）
  → 目标 = expSlider.fillRect 右边缘屏幕坐标（生成时锁定，不受升级 Fill 重置影响）
  → 到达收集点 → PlayerState.AddExp(expAmount)
  → 经验条更新 (BattleHUD.UpdateExpBar)
  → 经验满 → PlayerState.OnLevelUp 触发
  → UpgradeChoiceManager.StartChoiceFlow()
    → Time.timeScale=0, 随机抽取 choiceCount 个 UpgradeDefinition
    → UpgradeChoicePopup.ShowChoices() — 竖向堆叠卡片淡入
    → 玩家点击 UpgradeCard → ConfirmChoice(selected)
      → UpgradeEffectManager.ApplyUpgrade(def)
        → 数值累积（damage_multiplier/attack_speed/move_speed/exp）
        → IEffectExecutor 行为分发（on_attack_trigger/unlock_attack）
        → 同步到 PlayerState.acquiredUpgrades
      → 恢复 timeScale=1 + InputManager.blockInputFrames=2
      → 若还有 pendingLevelUps → 再次 ShowNextChoice（仍暂停）
```

## 已完成功能

### ✅ 经验值系统
- `PlayerState.currentExp` / `currentLevel` / `AddExp(float)` — 累加经验，溢出循环升级
- `ExpCurveConfig` ScriptableObject — 配置每级所需经验
- `PlayerState.OnExpChanged` / `OnLevelUp` 事件
- `BattleHUD.expSlider` / `expLevelText` — 经验条+等级显示

### ✅ 经验宝石飞行系统（2025-07-16 重构为 UI 屏幕空间）
- `ExpGem` — 屏幕空间 UI Image（RectTransform），从敌人屏幕坐标飞向 ExpBar Fill 右端
  - 渲染层级高于所有 UI（`SetAsLastSibling()`）
  - `raycastTarget=false` 不阻挡点击
  - Size Delta: 100×100（可通过 Inspector 调整）
- `ExpGemManager` — 单例管理飞行中宝石列表
  - `gemParent` (RectTransform) — 运行时由 BattleHUD 设置，指向 Canvas transform
  - `expSlider` (Slider) — 运行时由 BattleHUD 设置，宝石飞向其 Fill 右端
  - 目标在生成时锁定（`GetFillEndScreenPosition()` 获取 fillRect 右边缘屏幕坐标），避免升级时 Fill 重置导致飞行中宝石改变目标
  - `fallbackCollectPoint` (Transform) — expSlider 为空时备用
  - 防积压：飞行中宝石越多速度越快（baseSpeed × min(1+0.15×(n-1), 5)）
  - **baseSpeed=800**（屏幕空间像素/秒，参考分辨率 1080×1920）
- `EnemyManager.OnEnemyDied` → `ExpGemManager.SpawnGem()`
- `Enemy.gemSprite` (Sprite) — 每个敌人可配置不同的经验宝石精灵
  - EnemyManager 传 `enemy.gemSprite` 给 SpawnGem
  - SpawnGem 的 `overrideSprite` 参数传递给 `ExpGem.SetVisual()`
- `ExpGem.SetVisual(Sprite, Color)` — 设置 Image.sprite 和 Image.color
- 精灵文件：`Assets/Sprites/ExpGem_placeholder.png` + `ExpGem_placeholder.asset`（Sprite 资产）
  - `Assets/Sprites/ex_point_normal.png` / `ex_point_rare.png`（不同稀有度精灵）

### ✅ 三选一弹窗系统
- `UpgradeChoiceManager` — 暂停/弹窗/连续升级流程管理
  - 监听 `PlayerState.OnLevelUp`，累计 `_pendingLevelUps`
  - `ConfirmChoice()` 后检查 `_pendingLevelUps`，连续升级时刷新弹窗
  - 恢复时设置 `InputManager.blockInputFrames=2` 防止误触发攻击
- `UpgradeChoicePopup` — UI 容器
  - 独立 Canvas（UpgradePopupCanvas），Sort Order 高于 BattleHUD
  - DOTween 淡入/淡出动画（使用 `SetUpdate(true)` 在暂停时播放）
  - 竖向堆叠 `UpgradeCard` 实例（VerticalLayoutGroup + ContentSizeFitter）
  - `cardSpacing` 属性自动同步到 VerticalLayoutGroup.spacing
- `UpgradeCard` — 单个选项卡片
  - 显示 `displayName` + 动态描述 `GetDescription()`
  - 稀有度配色（common/rare/legendary）
  - 点击 → `UpgradeChoiceManager.ConfirmChoice()`
  - 字体：`方正粗黑宋简体 SDF`（支持中文）

### ✅ 效果系统
- `UpgradeEffectManager` — 效果累积与分发
  - 数值型累积：`damage_multiplier`, `attack_speed`, `move_speed`, `exp_multiplier`
  - 行为型注册表：`Dictionary<string, IEffectExecutor>` (effectType → executor)
  - `ApplyUpgrade()` → 等级追踪 + 数值叠加 + 行为分发 + PlayerState 同步
  - `GetDescription()` → 根据模板 + 当前等级生成描述文本
- `AttackSystem` 已集成 `GetDamageMultiplier()` 查询
  - 刺击伤害（264行）和技能伤害（331行）均已乘入 multiplier
- `IEffectExecutor` 接口 — 行为型效果扩展点
  - `Execute(UpgradeDefinition def, int level)`

### ✅ 被动攻击奖励：虚幻武器 (PhantomWeapon) — 2025-07-18
- `PassiveTriggerModule` — 攻击计数器+触发逻辑
  - 每第5次有效攻击触发（Slash/Stab/Sweep/Pierce/Launch 计入，Parry 和 Ult 不计）
  - 幻影攻击不算入累积次数
  - 框架支持多段幻影（不同 damageRatio/alpha）
- `AttackSystem.ExecutePhantomAttack()` — 穿通执行攻击（不耗冷却、不加能量、不计入计数器）
- `UpgradeDefinition` 新增：
  - `triggerCount` (int) — 触发所需攻击次数
  - `countedAttackTypes` (AttackType[]) — 计数的攻击类型
  - `phantomDamages` (PhantomDamageStep[]) — 多段幻影参数数组
  - `PhantomDamageStep` 结构体：`damageRatio` (float) + `alpha` (float)
- `UpgradeEffectManager.InitializePassive()` — 注册 PassiveTriggerModule
- `PhantomWeapon.asset` — 配置：triggerCount=5, 单段60%伤害+60%alpha
- `AttackWave.Create` / `SweepEffect.Create` — 新增 `alphaOverride` 参数

### ✅ UI 三选一弹窗重构（2025-07-18）
- **新 Prefab**：
  - `Assets/Prefabs/UI/UpgradePopup.prefab` — Image(Sliced, background_31_outside) + CanvasGroup + VLG + CSF
  - `Assets/Prefabs/UI/UpgradeCard.prefab` — Image(Sliced, background_31_inside) + Button + IconBg(background_31__select) + 文本
- **素材**：`Assets/Sprites/31Reward/` — 9-slice 底框图片
  - `background_31_outside.png` (512×171, border=35) — 大底框
  - `background_31_inside.png` (512×171, border=20) — 选项底框
  - `background_31__select.png` (512×512) — 选项图标底框
- **Battle.scene 结构**：
  - `UpgradePopupCanvas` (Canvas + CanvasScaler 1080×1920 + GraphicRaycaster, sortingOrder=100)
    - `UpgradePopup` (Image + CanvasGroup + UpgradeChoicePopup + VLG + CSF)
- `UpgradeChoicePopup.cardSpacing` → 属性自动同步 VLG.spacing

### ✅ 敌人精灵动画系统（2025-07-17，临时方案，后续改为 Animator）
- `EnemySpriteController` — 新组件，读取 `Enemy.state` 驱动 `SpriteRenderer.sprite` 切换
  - 职责分离，挂在 Enemy prefab 上，与 Enemy.cs 解耦
  - 精灵规则：
    - **Dead（最高优先级）**：立即切换 dead sprite，中断一切
    - **受击闪烁**：`TriggerHitFlash()` 由 `Enemy.TakeDamage` 调用，持续 0.3s，仅 Idle/Moving/Attacking冷却阶段 触发
    - **Attacking**：AttackSpawn 前半=attack1，后半+AttackDraw=attack2；冷却阶段=idle
    - **Launched**：knockUp sprite，直到落地或死亡
    - **QTEAttacking**：不干预（QTEController 自行管理）
    - **Idle/Moving/Stunned**：idle sprite
- `Enemy.cs` 改动：
  - 新增 `cachedSpriteController` 缓存，`TakeDamage` 中调用 `TriggerHitFlash()`
  - 新增 `useAttackFlip` bool（默认 true），关闭后攻击动画跳过 `DOScaleX` 镜像翻转
- Prefab 配置：`Enemy_101.prefab` / `Enemy_102.prefab` 已添加组件并配置 6 个精灵引用
- 精灵资源：
  - `Assets/Sprites/Enemy/Enemy1/` — Enemy_1(idle), attack1, attack2, dead, hitted, knockUp
  - `Assets/Sprites/Enemy/Enemy2/` — Enemy_2(idle), attack1, attack2, dead, hitted, knockUp
  - 旧 `Assets/Sprites/Enemy/Enemy_1.png` / `Enemy_2.png` 已删除（迁移到子文件夹）
- 已知限制：`Enemy_104.prefab` 尚未配置 EnemySpriteController（无对应精灵资源）

### ✅ 共享血量组系统（2025-07-17）
- **设计规则**：同行相邻同ID且 `shareHealthWithAdjacent=true` 的敌人自动共享一个血量池
  - 同行：`rowIndex` 相同
  - 相邻：`columnIndex` 连续（col, col+1, col+2, ...）
  - 同ID：`enemyId` 相同
  - 攻击任一成员 → 扣共享池；池归零 → 所有成员同时死亡（触发多次死亡事件）
  - Launched 状态不破坏组；PerColumn 补齐后不同行则解散；PerRow 补齐后始终同行不解散
- `SharedHealthGroup`（`Assets/Scripts/Enemy/SharedHealthGroup.cs`）— 数据类
  - `currentHealth` / `maxHealth` / `members` / `chainObjects`
  - `TakeDamage(rawDamage, damageType, hitMember)` — 扣除共享池，扣完触发 `KillAll()`
  - `KillAll()` — 每个成员独立调用 `Die()`（触发独立死亡事件/掉落）
  - `Disband()` — 剩余HP平分，解除共享关系
  - `SpawnChains()` / `UpdateAllChainPositions()` — 铁链视觉连接（chainPrefab 为空则跳过）
- `Enemy.cs` 改动：
  - 新增 `shareHealthWithAdjacent` bool（默认 false），Inspector 可配置
  - 新增 `sharedHealthGroup` (NonSerialized) — 运行时组引用
  - `TakeDamage()` — 有组时重定向到 `SharedHealthGroup.TakeDamage()`
  - `UpdateMovement()` — 移动完成后解散检查（**已修复**：补齐期间跳过解散，见下方 Bug 记录）
  - `ResetEnemy()` — 清理 `sharedHealthGroup = null`
- `EnemyManager.cs` 改动：
  - `sharedHealthChainPrefab` / `chainScale` / `chainYOffset` — 铁链配置
  - `RegisterGroup()` / `RemoveGroup()` — 组注册表管理
  - `LateUpdate()` — 每帧更新所有组的铁链位置
  - `ClearAllEnemies()` — 清理所有组的引用
- `WaveSpawner.cs` 改动：
  - `CreateSharedHealthGroups()` — 生成后扫描存活敌人，按 rowIndex 分组，同行相邻同ID建立组
  - 调用时机：`SpawnNextWave()` 中补齐前调用（UpdateMovement 的解散守卫保证存活）
- `Enemy_102.prefab` — `shareHealthWithAdjacent = true` 已配置
- **已知 Bug 修复（2025-07-17）**：
  - 问题：组在补齐前创建（row=2），`RowBasedFillUp()` 链式补齐期间，第一个成员移动完成 rowIndex-- 后第二个成员 rowIndex 尚未变化 → `UpdateMovement()` 解散检查触发 Disband() → 组秒解散
  - 修复：解散检查加前置守卫——若组内任何成员 `state==Moving` 或 `pendingRushMove==true`，跳过检查
- **待完成**：`EnemyManager.sharedHealthChainPrefab` 在 Battle.scene 中为 NULL，需准备铁链 Prefab 并拖入

## 最近 Bug 修复（2025-07-18）

### Bug: UpgradePopup/UpgradeCard 背景不显示
- **根因**：Battle.scene 中 `UpgradePopup` 是独立根对象（无 Canvas 父节点），Image 无法在屏幕空间渲染；`UpgradePopupCanvas` 错误挂载了 Image/VLG/CSF 等组件
- **修复**：清理 UpgradePopupCanvas 多余组件，将 UpgradePopup 移至其子节点下

### Bug: Stab/Slash Wave 颜色异常（绿色调）
- **根因**：之前 prefab 路径中从未调用 `material.color = color`（旧bug），上次修复补上后，`GetColor()` 饱和色直接叠加白色 sprite 上造成过强着色
- **修复**：prefab 路径用 `Color.Lerp(color, Color.white, 0.5f)` 淡化色调

## 关键代码文件清单

| 文件 | 职责 |
|------|------|
| `Assets/Scripts/Core/UpgradeChoiceManager.cs` | 暂停/弹窗/连续升级流程 |
| `Assets/Scripts/Core/UpgradeEffectManager.cs` | 效果累积+行为分发+被动初始化 |
| `Assets/Scripts/Core/UpgradeDefinition.cs` | 升级定义 SO + PhantomDamageStep |
| `Assets/Scripts/Core/UpgradePoolConfig.cs` | 稀有度池配置 SO |
| `Assets/Scripts/Core/ExpCurveConfig.cs` | 经验曲线配置 SO |
| `Assets/Scripts/Core/ExpGem.cs` | 屏幕空间 UI Image 飞行宝石 |
| `Assets/Scripts/Core/ExpGemManager.cs` | 宝石管理单例（飞行+收集+屏幕坐标） |
| `Assets/Scripts/Core/IEffectExecutor.cs` | 行为型效果接口 |
| `Assets/Scripts/Core/PassiveTriggerModule.cs` | 被动触发模块（攻击计数+幻影触发） |
| `Assets/Scripts/UI/UpgradeChoicePopup.cs` | 三选一弹窗容器 |
| `Assets/Scripts/UI/UpgradeCard.cs` | 单个选项卡片 |
| `Assets/Scripts/UI/BattleHUD.cs` | 经验条+gemParent/expSlider 传递 |
| `Assets/Scripts/Player/PlayerState.cs` | 经验/等级/acquiredUpgrades |
| `Assets/Scripts/Player/InputManager.cs` | blockInputFrames 防误触 |
| `Assets/Scripts/Player/AttackSystem.cs` | GetDamageMultiplier + ExecutePhantomAttack |
| `Assets/Scripts/Attack/AttackWave.cs` | AttackWave + alphaOverride 参数 |
| `Assets/Scripts/Attack/SweepEffect.cs` | SweepEffect + alphaOverride 参数 |
| `Assets/Scripts/Enemy/Enemy.cs` | gemSprite + cachedSpriteController + useAttackFlip + shareHealthWithAdjacent |
| `Assets/Scripts/Enemy/EnemySpriteController.cs` | 敌人类状态驱动精灵切换 |
| `Assets/Scripts/Enemy/SharedHealthGroup.cs` | 共享血量组 |
| `Assets/Scripts/Managers/EnemyManager.cs` | SpawnGem + sharedHealthChainPrefab + 组注册 |
| `Assets/Scripts/Wave/WaveSpawner.cs` | CreateSharedHealthGroups() |

## 场景对象

| 对象 | 位置 | 说明 |
|------|------|------|
| `ExpGemManager` | Battle.scene 根级 | 单例，baseSpeed=800 |
| `UpgradeChoiceManager` | Battle.scene | 单例，poolConfig 已拖拽 |
| `UpgradeEffectManager` | Battle.scene | 单例 |
| `UpgradePopupCanvas` | Battle.scene 根级 | Canvas + CanvasScaler(1080×1920) + GraphicRaycaster, sortingOrder=100 |
| `UpgradePopup` | UpgradePopupCanvas 子节点 | Image + CanvasGroup + UpgradeChoicePopup + VLG + CSF |
| `ExpBar Slider` | BattleHUD Canvas 下 | expSlider + expLevelText |

## 开发避坑规则（后续必须遵守）

1. 任何暂停/恢复游戏（修改 Time.timeScale）恢复时必须设 InputManager.blockInputFrames=2
2. timeScale=0 期间必须重置 InputManager 所有输入状态
3. 纯展示型 Slider/Button（不交互）设 `interactable=false` 时必须将其 ColorBlock.disabledColor 设为与 normalColor 一致
4. 空间切换：世界空间距离小（~10 单位），屏幕空间距离大（~1000 像素），切换坐标系时必须同步调整速度参数
5. 暂停期间动画需使用 SetUpdate(true)（DOTween）或 Time.unscaledDeltaTime
6. 新 .cs 文件后必须 unity_recompile 再 unity_execute
7. **读取 .meta 文件 GUID 不可用 bash `find` + `head` 直读**：.meta 文件的 GUID 可能被 Unity 内部重新映射，AssetDatabase 中的实际 GUID 可能与 .meta 文件内容不同。获取 GUID 必须通过 `AssetDatabase.FindAssets` / `AssetDatabase.GUIDToAssetPath`
8. **补齐期间 rowIndex 不稳定**：链式补齐（RushMove）期间，同组成员逐个完成移动，rowIndex 短暂不同步。对 rowIndex 有依赖的跨成员系统（如共享血量组）必须在成员稳定后（无 Moving/pendingRushMove）再执行一致性检查
9. **新建 Canvas 必须配置 CanvasScaler + GraphicRaycaster**，否则 UI 元素无法渲染/交互。Canvas 的 Image 和 CanvasGroup 应挂载在子节点上而非 Canvas 自身
10. **UI 元素必须在 Canvas 子节点下**才能屏幕空间渲染，独立根对象的 Image 组件无效

## 精灵部署位置

| 用途 | 路径 |
|------|------|
| 经验宝石 | `Assets/Sprites/ExpGem_placeholder.png`（替换此文件） |
| 普通经验宝石 | `Assets/Sprites/ex_point_normal.png` |
| 稀有经验宝石 | `Assets/Sprites/ex_point_rare.png` |
| 敌人精灵 Enemy1 | `Assets/Sprites/Enemy/Enemy1/` (6 帧) |
| 敌人精灵 Enemy2 | `Assets/Sprites/Enemy/Enemy2/` (6 帧) |
| 铁链 | `Assets/Sprites/Enemy/Enemy2/chain.png` |
| 三选一底框（大） | `Assets/Sprites/31Reward/background_31_outside.png` |
| 三选一底框（选项） | `Assets/Sprites/31Reward/background_31_inside.png` |
| 三选一图标框 | `Assets/Sprites/31Reward/background_31__select.png` |

## 字体

- 中文字体：`Assets/Fonts/方正粗黑宋简体 SDF.asset`
- UpgradeCard 的 NameText / DescriptionText 使用此字体
<!-- locus:body:end -->
