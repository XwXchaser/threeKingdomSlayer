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
updatedAt: 1779895286146
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
    → Instantiate(popupPrefab) 动态生成弹窗（不在场景中预置）
    → UpgradeChoicePopup.ShowChoices() — 填充3张预置卡片内容 + 淡入
    → 玩家点击 UpgradeCard → ConfirmChoice(selected)
      → UpgradeEffectManager.ApplyUpgrade(def)
        → 数值累积（damage_multiplier/attack_speed/move_speed/exp/stabRange/sweepRange）
        → IEffectExecutor 行为分发（on_attack_trigger/unlock_attack）
        → 同步到 PlayerState.acquiredUpgrades
      → 恢复 timeScale=1 + InputManager.blockInputFrames=2
      → 若还有 pendingLevelUps → 再次 ShowNextChoice（仍暂停）
```

## 三选一UI架构（2025-07-20 重构）

### 设计原则
- **所有布局由用户在 Prefab 中手动调整**，代码绝不覆写 Inspector 值
- **卡片不单独为 prefab**，而是 UpgradePopup.prefab 内的普通 GameObject
- **弹窗由 UpgradeChoiceManager 动态 Instantiate/Destroy**，不在场景中预置
- 代码只负责：填充数据（文本+图标）+ 淡入淡出动画

### Prefab 结构

```
UpgradePopup.prefab
├─ FrameBg (Image)          ← 用户拖入 outsideframe sprite
└─ Content (VerticalLayoutGroup)
   ├─ InsideFrame (Image)   ← 用户拖入 insideframe sprite
   ├─ Card1                 ← 普通GameObject，用户手动调整位置/大小
   ├─ Card2
   └─ Card3
       每张Card: Image(背景) + Button + CanvasGroup + UpgradeCard + LayoutElement
         ├─ IconBg/Icon     ← 用户拖入 iconframe sprite
         ├─ NameText        ← 方正粗黑宋简体 SDF
         └─ DescriptionText ← 方正粗黑宋简体 SDF
```

### 关键脚本

**UpgradeChoicePopup.cs** (~65行)
- 3个 public 字段：`card1/card2/card3`（Inspector中串接）
- `ShowChoices()`: 按选项数显隐卡片 + 调用 Setup() 填充
- `Dismiss()`: 淡出后 Destroy
- **不再包含**: 动态生成、spacing覆写、padding覆写、contentRect

**UpgradeCard.cs** (~45行)
- 5个 public 字段：`backgroundImage/iconImage/nameText/descriptionText/button`
- `Setup(def)`: 填充 nameText.text、descriptionText.text、iconImage.sprite
- `OnClicked()`: 调用 `UpgradeChoiceManager.ConfirmChoice(_upgradeDef)`
- **不再包含**: 稀有度颜色覆写（GetRarityColor已删除）

**UpgradeChoiceManager.cs** — 不变
- 动态 Instantiate(popupPrefab) / Dismiss 后 Destroy
- 暂停/连续升级流程管理

### 已修复的UI问题
- ✅ 字段未串接 → 代码创建后手动wire
- ✅ 中文字体 → 方正粗黑宋简体 SDF
- ✅ 图标不显示 → 新增 iconImage 字段 + Setup中赋值
- ✅ 背景颜色染色 → 移除 GetRarityColor()
- ✅ Missing Prefab → 删除残留 + 重建为普通GameObject
- ✅ 代码覆写Inspector → 移除所有动态生成和padding/spacing覆写

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

### ✅ 被动攻击奖励：虚幻武器 (PhantomWeapon) — Phase 2 完成
- `PassiveTriggerModule` — 攻击计数器+幻影触发逻辑
- `AttackSystem.ExecutePhantomAttack()` — 穿通执行攻击
- 延迟攻击（delaySeconds 可逐级配置）
- 蓝色伤害数字 (#4D7FFF) 以区分幻影伤害
- per-level 独立配置：TriggerParam / DamageRatio / Alpha / delaySeconds

### ✅ 玩家受击反馈 PlayerHitFeedback（2025-07-19）
- `PlayerHitFeedback` 组件挂载在 Player GameObject 上
- 监听 `PlayerState.OnHealthChanged`，检测伤害（current < _lastHealth）
- **hitted 边框图**：全屏 Image（HittedOverlay）瞬间全白 → 停留 hittedDuration → 淡出 hittedFadeDuration
  - 放在 `BattleHUD(Canvas)/HittedOverlay` 下，sprite=hitted.png，初始 alpha=0
  - 防输入拦截：`raycastTarget = false`（Start 中强制设置）
- **镜头抖动**：`Camera.main.DOShakePosition(shakeDuration, shakeIntensity, shakeVibrato)`
  - 与 CameraManager.OnRenderImage 模糊后处理兼容（抖动在模糊之前）
- Inspector 可调参数：
  - hittedImage（Image 引用）、hittedDuration（默认 0.3s）、hittedFadeDuration（默认 0.1s）
  - shakeDuration（默认 0.2s）、shakeIntensity（默认 0.3）、shakeVibrato（默认 20）
- 风险防范：全屏 Image 默认 raycastTarget=true 会拦截输入，Start() 中强制设为 false
- 文件：`Assets/Scripts/Player/PlayerHitFeedback.cs`

### ✅ Bug 修复
- 9-slice sprite border=0 → 修复为 (35,35,35,35) / (20,20,20,20)
- InputManager Debug.Log 帧刷屏 → 注释掉
- UpgradePopup/UpgradeCard 背景图不显示 → 更换素材解决
- UpgradeCard 字段未串接 → 代码串接
- 中文不显示 → 方正粗黑宋简体 SDF
- 图标不显示 → iconImage 字段 + Setup 赋值
- 背景染色 → 移除 GetRarityColor
- Missing Prefab 残留 → 重建为普通GameObject
- 代码覆写Inspector → 移除所有动态生成和布局覆写
<!-- locus:body:end -->
