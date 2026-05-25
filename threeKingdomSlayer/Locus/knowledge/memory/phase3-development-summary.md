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
updatedAt: 1779682011022
---

# phase3-development-summary

## Summary
第三期局内成长系统（经验三选一）开发状态 — 含虚幻武器被动奖励、UI弹窗重构、Bug修复记录。7种奖励全部完成，3个未修复问题。

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
        → 数值累积（damage_multiplier/attack_speed/move_speed/exp/stabRange/sweepRange）
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
- 精灵文件：`Assets/Sprites/ExpGem_placeholder.png`、`ex_point_normal.png`、`ex_point_rare.png`

### ✅ 三选一弹窗系统
- `UpgradeChoiceManager` — 暂停/弹窗/连续升级流程管理
- `UpgradeChoicePopup` — UI 容器（9-slice Image + CanvasGroup + VLG + CSF）
- `UpgradeCard` — 单个选项卡片（9-slice Image + Button + 文本）
- 素材：`Assets/Sprites/31Reward/` — 9-slice 底框图片（border 已修复）
- 图标：`Assets/Sprites/31Reward/icon/icon_31_*.png`（6 个：exp/larger/longer/money/power/unrealWeapons）
- 字体：`Assets/Fonts/方正粗黑宋简体 SDF.asset`

### ✅ 效果系统（2025-07-18 更新）
- `UpgradeEffectManager` — 效果累积与分发
  - 数值型累积：`damage_multiplier`, `attack_speed`, `move_speed`, `exp_multiplier`
  - 新增范围加成：`stab_range_boost`（戳击范围+intValue排/级，伤害惩罚-secondaryIntValue%/级）
  - 新增范围加成：`sweep_range_boost`（横扫范围+intValue排/级，伤害惩罚-secondaryIntValue%/级）
  - 行为型注册表：`Dictionary<string, IEffectExecutor>`
- `UpgradeDefinition` 新增字段：`secondaryIntValue` — 第二整数加成（范围伤害惩罚%）
- `GetDescription()` — {0}=intValue*level, {1}=secondaryIntValue*level（范围类）或 floatValue*level（数值类）

### ✅ 当前三选一奖励选项（共7种）

| ID | 名称 | 类型 | 效果 | 稀有度 |
|----|------|------|------|--------|
| damage_plus | 神力 | Numeric | 伤害+5%/级 | Common |
| attack_speed | 急速 | Numeric | 攻速+5%/级 | Common |
| move_speed | 疾行 | Numeric | 移速+5%/级 | Common |
| wisdom | 智慧 | Numeric | 经验+5%/级 | Common |
| stab_range_boost | 延长 | Numeric | 戳击范围+1排/级，该范围伤害-1%/级 | Rare |
| sweep_range_boost | 波长 | Numeric | 横扫范围+1排/级，该范围伤害-1%/级 | Rare |
| phantom_weapon | 虚幻武器 | Passive | 每5次攻击触发1次30%伤害幻影 | Rare |

### ✅ 被动攻击奖励：虚幻武器 (PhantomWeapon)
- `PassiveTriggerModule` — 攻击计数器+幻影触发逻辑
- `AttackSystem.ExecutePhantomAttack()` — 穿通执行攻击
- 代码验证通过：订阅 ✅ | 奖池 ✅ | 路由 ✅
- ⚠️ 实际触发待 Play Mode 获取后验证

### ✅ UI 三选一弹窗重构（2025-07-18）
- 新 Prefab：`UpgradePopup.prefab`、`UpgradeCard.prefab`
- 9-slice 素材：`background_31_outside.png`(border=35)、`background_31_inside.png`(border=20)、`background_31__select.png`
- Battle.scene 结构：`UpgradePopupCanvas` → `UpgradePopup`（Image + CanvasGroup + VLG + CSF）

### ✅ Bug 修复（2025-07-18）
- 9-slice sprite border=0 → 修复为 (35,35,35,35) / (20,20,20,20)
- InputManager Debug.Log 帧刷屏 → 注释掉
- UpgradePopup/UpgradeCard 背景图不显示 → 更换素材解决

## 未修复问题（详见 Locus/knowledge/memory/unresolved-issues.md）

1. **QTE 无法触发**（高优先级）：QTEController._state 始终 Idle，OnBossEngaged 不触发
2. **虚幻武器待验证**（中优先级）：代码路径正确，需 Play Mode 实测
3. **cardPrefab 空引用偶现**（中优先级）：Editor 中已正确赋值，需确认 Play Mode 是否复现

## 关键代码文件清单

| 文件 | 职责 |
|------|------|
| `Assets/Scripts/Core/UpgradeChoiceManager.cs` | 暂停/弹窗/连续升级流程 |
| `Assets/Scripts/Core/UpgradeEffectManager.cs` | 效果累积+行为分发+被动初始化 |
| `Assets/Scripts/Core/UpgradeDefinition.cs` | 升级定义 SO + PhantomStep + secondaryIntValue |
| `Assets/Scripts/Core/UpgradePoolConfig.cs` | 稀有度池配置 SO |
| `Assets/Scripts/Core/ExpCurveConfig.cs` | 经验曲线配置 SO |
| `Assets/Scripts/Core/ExpGem.cs` | 屏幕空间 UI Image 飞行宝石 |
| `Assets/Scripts/Core/ExpGemManager.cs` | 宝石管理单例 |
| `Assets/Scripts/Core/IEffectExecutor.cs` | 行为型效果接口 |
| `Assets/Scripts/Core/PassiveTriggerModule.cs` | 被动触发模块 |
| `Assets/Scripts/UI/UpgradeChoicePopup.cs` | 三选一弹窗容器 |
| `Assets/Scripts/UI/UpgradeCard.cs` | 单个选项卡片 |
| `Assets/Scripts/UI/BattleHUD.cs` | 经验条+gemParent/expSlider 传递 |
| `Assets/Scripts/Player/PlayerState.cs` | 经验/等级/acquiredUpgrades |
| `Assets/Scripts/Player/InputManager.cs` | blockInputFrames 防误触 |
| `Assets/Scripts/Player/AttackSystem.cs` | GetDamageMultiplier + ExecutePhantomAttack |
| `Assets/Scripts/Attack/AttackWave.cs` | AttackWave + alphaOverride |
| `Assets/Scripts/Attack/SweepEffect.cs` | SweepEffect + alphaOverride |
| `Assets/Scripts/Enemy/Enemy.cs` | gemSprite + useAttackFlip |
| `Assets/Scripts/Enemy/EnemySpriteController.cs` | 敌人类状态驱动精灵切换 |
| `Assets/Scripts/Enemy/SharedHealthGroup.cs` | 共享血量组 |
| `Assets/Scripts/Managers/EnemyManager.cs` | SpawnGem + sharedHealthChainPrefab |
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
7. 读取 .meta 文件 GUID 不可用 bash `find` + `head` 直读，必须通过 AssetDatabase API
8. 补齐期间 rowIndex 不稳定：链式补齐（RushMove）期间，同组成员逐个完成移动，rowIndex 短暂不同步。对 rowIndex 有依赖的跨成员系统必须在成员稳定后再执行一致性检查
9. 新建 Canvas 必须配置 CanvasScaler + GraphicRaycaster，Canvas 的 Image 和 CanvasGroup 应挂载在子节点上
10. UI 元素必须在 Canvas 子节点下才能屏幕空间渲染
11. **9-slice Sliced Image 的 sprite border 必须非零**：border=0 时图片不拉伸，导入后必须在 Sprite Editor 设置 Border
12. **修改 prefab/scene/cs 后必须：保存 → unity_recompile → 确认编译通过 → 再进入 Play Mode**
13. **CanvasGroup.blocksRaycasts 必须在显隐逻辑中同步维护**：显示=true，隐藏=false

## 精灵部署位置

| 用途 | 路径 |
|------|------|
| 经验宝石 | `Assets/Sprites/ExpGem_placeholder.png`、`ex_point_normal.png`、`ex_point_rare.png` |
| 敌人精灵 Enemy1 | `Assets/Sprites/Enemy/Enemy1/` (6 帧) |
| 敌人精灵 Enemy2 | `Assets/Sprites/Enemy/Enemy2/` (6 帧) |
| 铁链 | `Assets/Sprites/Enemy/Enemy2/chain.png` |
| 三选一底框（大） | `Assets/Sprites/31Reward/background_31_outside.png` (border=35) |
| 三选一底框（选项） | `Assets/Sprites/31Reward/background_31_inside.png` (border=20) |
| 三选一图标框 | `Assets/Sprites/31Reward/background_31__select.png` |
| 奖励图标 | `Assets/Sprites/31Reward/icon/icon_31_*.png` (6 个) |
<!-- locus:body:end -->
