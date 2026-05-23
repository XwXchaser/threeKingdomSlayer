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
updatedAt: 1779554707616
---

# phase3-development-summary

## Summary
第三期局内成长系统（经验三选一）开发状态与技术总结 — 2025-07

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
  - 独立 Canvas，Sort Order 高于 BattleHUD
  - DOTween 淡入/淡出动画（使用 `SetUpdate(true)` 在暂停时播放）
  - 竖向堆叠 `UpgradeCard` 实例
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

### ✅ 配置数据
- `UpgradeDefinition` ScriptableObject（`Assets/ScriptableObjects/Upgrades/Definitions/`）
  - `DamagePlus.asset` — 造成伤害提升
  - `AttackSpeed.asset` — 攻击速度提升
  - `OnAttackStab.asset` — 每3次攻击触发戳击
  - `OnKillCoin.asset` — 击杀掉落铜钱
- `UpgradePoolConfig.asset` — 稀有度池+权重配置
- `UpgradeRarity` 枚举：Common / Rare / Legendary
- `UpgradePrerequisite` — 前置条件（可选）
- `ExpCurveConfig` — 经验曲线（`Assets/ScriptableObjects/ExpCurve/`）

### ✅ Prefab
- `Assets/Prefabs/ExpGem.prefab` — 屏幕空间 UI Image + ExpGem 组件，RectTransform (100×100)
- `Assets/Prefabs/UI/UpgradeCard.prefab` — 卡片 Prefab（UpgradeChoicePopup 实例化）

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

## 关键代码文件清单

| 文件 | 职责 |
|------|------|
| `Assets/Scripts/Core/UpgradeChoiceManager.cs` | 暂停/弹窗/连续升级流程 |
| `Assets/Scripts/Core/UpgradeEffectManager.cs` | 效果累积+行为分发 |
| `Assets/Scripts/Core/UpgradeDefinition.cs` | 升级定义 SO + 前置条件 |
| `Assets/Scripts/Core/UpgradePoolConfig.cs` | 稀有度池配置 SO |
| `Assets/Scripts/Core/ExpCurveConfig.cs` | 经验曲线配置 SO |
| `Assets/Scripts/Core/ExpGem.cs` | 屏幕空间 UI Image 飞行宝石 |
| `Assets/Scripts/Core/ExpGemManager.cs` | 宝石管理单例（飞行+收集+屏幕坐标） |
| `Assets/Scripts/Core/IEffectExecutor.cs` | 行为型效果接口 |
| `Assets/Scripts/UI/UpgradeChoicePopup.cs` | 三选一弹窗容器 |
| `Assets/Scripts/UI/UpgradeCard.cs` | 单个选项卡片 |
| `Assets/Scripts/UI/BattleHUD.cs` | 经验条+gemParent/expSlider 传递 |
| `Assets/Scripts/Player/PlayerState.cs` | 经验/等级/acquiredUpgrades |
| `Assets/Scripts/Player/InputManager.cs` | blockInputFrames 防误触 |
| `Assets/Scripts/Player/AttackSystem.cs` | GetDamageMultiplier 集成 |
| `Assets/Scripts/Enemy/Enemy.cs` | gemSprite + cachedSpriteController + useAttackFlip |
| `Assets/Scripts/Enemy/EnemySpriteController.cs` | 敌人类状态驱动精灵切换 |
| `Assets/Scripts/Managers/EnemyManager.cs` | SpawnGem 调用点 + gemSprite 传递 |

## 场景对象

| 对象 | 位置 | 说明 |
|------|------|------|
| `ExpGemManager` | Battle.scene 根级 | 单例，baseSpeed=800 |
| `UpgradeChoiceManager` | Battle.scene | 单例，poolConfig 已拖拽 |
| `UpgradeEffectManager` | Battle.scene | 单例 |
| `UpgradeChoicePopup` | Battle.scene 独立 Canvas | canvasGroup 淡入淡出 |
| `ExpBar Slider` | BattleHUD Canvas 下 | expSlider + expLevelText |

## 开发避坑规则（后续必须遵守）

1. 任何暂停/恢复游戏（修改 Time.timeScale）恢复时必须设 InputManager.blockInputFrames=2
2. timeScale=0 期间必须重置 InputManager 所有输入状态
3. 纯展示型 Slider/Button（不交互）设 `interactable=false` 时必须将其 ColorBlock.disabledColor 设为与 normalColor 一致
4. 空间切换：世界空间距离小（~10 单位），屏幕空间距离大（~1000 像素），切换坐标系时必须同步调整速度参数
5. 暂停期间动画需使用 SetUpdate(true)（DOTween）或 Time.unscaledDeltaTime
6. 新 .cs 文件后必须 unity_recompile 再 unity_execute
7. **读取 .meta 文件 GUID 不可用 bash `find` + `head` 直读**：.meta 文件的 GUID 可能被 Unity 内部重新映射，AssetDatabase 中的实际 GUID 可能与 .meta 文件内容不同。获取 GUID 必须通过 `AssetDatabase.FindAssets` / `AssetDatabase.GUIDToAssetPath`

## 精灵部署位置

| 用途 | 路径 |
|------|------|
| 经验宝石 | `Assets/Sprites/ExpGem_placeholder.png`（替换此文件） |
| 普通经验宝石 | `Assets/Sprites/ex_point_normal.png` |
| 稀有经验宝石 | `Assets/Sprites/ex_point_rare.png` |
| 敌人精灵 Enemy1 | `Assets/Sprites/Enemy/Enemy1/` (6 帧) |
| 敌人精灵 Enemy2 | `Assets/Sprites/Enemy/Enemy2/` (6 帧) |
| 卡片稀有度背景 | 待用户提供 — 需在 UpgradeCard Prefab 上拖拽 |
| 弹窗面板背景 | 待用户提供 — 需在 UpgradeChoicePopup 上拖拽 |

## 字体

- 中文字体：`Assets/Fonts/方正粗黑宋简体 SDF.asset`
- UpgradeCard 的 NameText / DescriptionText 使用此字体
<!-- locus:body:end -->
