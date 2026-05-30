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
updatedAt: 1780166172310
---

# phase3-development-summary

## Summary
第三期局内成长系统（经验三选一）+ 击杀进度条 & 击杀里程碑闪现 + 敌人动画状态机重构（2025-08-07）

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
        → 道具型 → ItemInventory.AddItem(def) → BuffDisplayPanel 点亮 ColumnB 图标
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
  - 新增 `AddDamageBonus(float)` — 供道具型测试等直接叠加伤害倍率
- `UpgradeDefinition` 新增字段：`secondaryIntValue` — 第二整数加成（范围伤害惩罚%）
- `GetDescription()` — {0}=intValue*level, {1}=secondaryIntValue*level（范围类）或 floatValue*level（数值类）

### ✅ 当前三选一奖励选项（共8种）

| ID | 名称 | 类型 | 效果 | 稀有度 |
|----|------|------|------|--------|
| damage_plus | 神力 | Numeric | 伤害+5%/级 | Common |
| attack_speed | 急速 | Numeric | 攻速+5%/级 | Common |
| move_speed | 疾行 | Numeric | 移速+5%/级 | Common |
| wisdom | 智慧 | Numeric | 经验+5%/级 | Common |
| stab_range_boost | 延长 | Numeric | 戳击范围+1排/级，该范围伤害-1%/级 | Rare |
| sweep_range_boost | 波长 | Numeric | 横扫范围+1排/级，该范围伤害-1%/级 | Rare |
| phantom_weapon | 虚幻武器 | Passive | 每5次攻击触发1次30%伤害幻影 | Rare |
| test_damage_boost | 伤害加成 | Item | 使用后获得100%伤害加成（1次） | Common |

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

### ✅ 攻击打断系统（2025-07-21）

**三级打断体系**（详见 `design/attack-interrupt-system.md`）：

| 层级 | 攻击类型 | 打断条件 | 视觉反馈 |
|------|---------|---------|---------|
| Level 1 | 普通攻击 | 任何直接伤害即可打断 | 白色闪白 + 缩放抖动 |
| Level 2 | C技(蓄力技) | 仅 Parry / Launch 打断 | 橙红弹刀闪烁 + 水平抖动 |
| Level 3 | QTE攻击(BOSS) | 不可打断 | N/A |

**C技 霸体机制**：
- `isCFrame = true` 窗口内（PerformAttack 之前），非 Parry/Launch 伤害只触发弹刀反馈
- Parry/Launch 作为唯一能打断 C技 的手段，获得战术价值
- `canInterruptCFrame` 参数沿 AttackWave → TakeDamage 完整传递

**sharedHealthGroup 打断修复**：
- **问题**：共享血量敌人（如 Enemy_101）的 TakeDamage 在打断检查之前就通过 sharedHealthGroup.TakeDamage() return
- **修复**：将打断逻辑块移到 sharedHealthGroup 调用之前（`Enemy.cs` 第1138行）
- Enemy_101.prefab: `cAttackProbability` 设为 0（仅使用普通攻击，便于测试）

**已验证**：
- Parry 成功打断 C技：`isCFrame=True, canInterruptC=True → CancelAttack`
- Stab 命中 C技敌人触发弹刀：`isCFrame=True, canInterruptC=False → PlayClankEffect`
- 非攻击状态不受影响：`state=Idle → 条件不满足`
- 收招阶段不可打断：`isDraw=True → 条件不满足`

**待实现**：
- P0: sharedHealthGroup 遍历打断传递
- P1: WhirlwindController.ExecuteLaunch() 绕过 TakeDamage 需确认
- P2: 旅行波到达时序 vs spawnDuration 窗口期优化
- P2: 三选一技能伤害接入 canInterruptCFrame

### ✅ 左侧 Buff 图标面板 BuffDisplayPanel（2025-08-06）

**设计原则**：
- 槽位制：Inspector 中 `_columnASlots` / `_columnBSlots` 列表预置 BuffIcon 实例，代码按序分配
- 手动布局：与 UpgradePopup 设计一致，无 VerticalLayoutGroup，用户手动摆放图标位置
- 代码不创建/销毁实例，只 SetActive 切换显隐

**两根柱子**：

| 列 | 存放内容 | 生命周期 | 显示逻辑 |
|----|---------|---------|---------|
| ColumnA | 数值型(Numeric) + 被动型(Passive) | 持久，永不删除 | 再次获得仅更新角标（Lv.N / 阈值数） |
| ColumnB | 道具型(Item) | 可消耗，消耗后槽位前移补位 | 点击图标触发效果 → TryConsume → 图标消失+CompactColumnB |

**双列联动**：同一 UpgradeDefinition 可能同时出现在 ColumnA（被动注册）和 ColumnB（道具库存），由 `PassiveTriggerModule.OnPassiveRegistered` 和 `ItemInventory.OnItemChanged` 分别驱动。

**关键脚本**：

**BuffDisplayPanel.cs** (~212行)
- `_columnASlots` / `_columnBSlots` — `List<BuffIcon>`，Inspector 中增删
- `_upgradeIcons` — upgradeId → BuffIcon 映射（ColumnA）
- `_itemIcons` — gestureId → BuffIcon 映射（ColumnB）
- 监听三个事件：`UpgradeEffectManager.OnUpgradeApplied`、`ItemInventory.OnItemChanged`、`PassiveTriggerModule.OnPassiveRegistered`
- 首次收到升级时淡入面板（CanvasGroup alpha 0→1）
- CompactColumnB：被消耗图标之后的槽位依次前移补位

**BuffIcon.cs** (~69行)
- `_iconImage` / `_badgeText` / `_button` — 80×80 Image + Badge TMP + Button
- `Setup()` — 设置图标、角标、按钮状态（Item 型可点击，其他型不可交互）
- `ResetSlot()` — 清空所有数据并 SetActive(false)
- `OnClicked` — 道具点击回调（仅 Item 型绑定）

**道具点击分发流程**（BuffDisplayPanel.OnItemIconClicked）：
```
点击 BuffIcon → TryConsume(gestureId)
  → 成功 → 按 gestureId 分发：
    "circle"         → WhirlwindController.Activate(def)    // 大旋风自动运转
    "long_press_swipe_down" → InputManager.ExecuteLightning(def)  // 落雷
    "damage_boost"   → UpgradeEffectManager.AddDamageBonus(def.floatValue)  // 伤害加成
  → TryConsume 触发 OnItemChanged → BuffDisplayPanel 移除图标 + CompactColumnB
```

**ItemInventory 挂载修复**：
- ItemInventory 组件未在场景任何 GameObject 上，导致道具存储/消耗链路完全断裂
- 已添加到 Manager GameObject（与 UpgradeEffectManager 同级）

**WhirlwindController 重构**：
- 从画圈手势驱动改为点击 BuffIcon 激活 → 自动运转
- 移除：画圈检测（~80行）、手指追踪角度累积、TickActive
- 新增：`autoDuration`（默认5s）、`autoSpinSpeed`（默认180°/s）
- Update() 自动倒计时 → 持续伤害 → 定时击飞

**InputManager 简化**：
- 移除：画圈检测代码（~35行，Update 中的每帧检测+MouseUp/TouchEnd 中的 Deactivate 分支）
- 移除：`TryConsumeItemGesture`（长按下滑检测，~35行）
- `ExecuteLightning` 改为 public（供 BuffDisplayPanel 直接调用）

**场景结构**：
```
BattleHUD(Canvas)/BuffDisplayPanel [CanvasGroup, BuffDisplayPanel]
├─ ColumnA (6 BuffIcon slots: A_0~A_5, 默认disabled)
└─ ColumnB (4 BuffIcon slots: B_0~B_4, 默认disabled)
```

**新增文件**：
- `Assets/Prefabs/UI/BuffIcon.prefab` — 80×80 Image+Button+Badge
- `Assets/Scripts/UI/BuffDisplayPanel.cs`
- `Assets/Scripts/UI/BuffIcon.cs`
- `Assets/ScriptableObjects/Upgrades/Definitions/TestDamageBoost.asset` — 测试道具（gestureId=dmg_boost, floatValue=1.0）

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
- **ItemInventory 未挂载** → 添加到 Manager GameObject
- **TestDamageBoost 点击无响应** → BuffIcon._button 字段未在 Prefab 中串接，修复后图标消失+伤害加成正常
- **Animator AnyState→HitFlash 抢走 Launched 动画** → 改为显式状态转移（见下）

---

## 击杀进度条 & 击杀里程碑闪现（2025-08-06）

### ✅ 击杀进度条（KillRewardUI.ProgressSlider）

**功能**：右侧纵向从下往上，显示关卡击杀进度 currentKill/totalEnemyCount。

**场景配置**：
```
KillRewardUI
├─ ProgressSlider (Slider, direction=BottomToTop)
│  ├─ Background → 底框 sprite
│  ├─ Fill Area/Fill → slider_stage_siller.png (Image type=Simple)
│  └─ MilestoneLabel × N → 左侧自动生成
```

**里程碑标签**（KillRewardUI.BuildMilestoneLabels）：
- 从 StageConfig.killMilestones 自动读取阈值
- 按比例放置在 Slider 左侧：`anchoredPosition.y = sliderHeight * ratio + offsetY`
- Inspector 可调：`milestoneLabelColor`、`milestoneLabelFontSize`、`milestoneLabelOffsetX`、`milestoneLabelOffsetY`
- 预制体：`Assets/Prefabs/UI/MilestoneLabel.prefab`（TMP_Text，方正粗黑宋简体 SDF，fontSize=18，白色）

**填充条透明度修复（两层根因）**：
1. Fill Image type: Filled→Simple（与 HeroHUD health bar 一致，Slider 通过 RectTransform 裁剪控制填充）
2. sprite alphaIsTransparency: False→True（UI Sprite 必须为 True，否则 alpha 通道不被 UI shader 正确处理）

### ✅ 击杀里程碑闪现（KillMilestoneDisplay）

**功能**：全局击杀数到达阈值时，显示对应 sprite 并闪烁后消失。与关卡配置解耦。

**配置**：`GlobalKillDisplayConfig` ScriptableObject（`Assets/ScriptableObjects/GlobalKillDisplayConfig.asset`）
- `entries: List<KillDisplayEntry>` — 每项含 killThreshold、displaySprite、displayDuration、displaySize、displayPosition

**效果**：入场闪烁 → 停留 → 淡出消失

**关键脚本**：
- `KillMilestoneDisplay.cs` — 监听 PlayerState.OnKillCountChanged，到达阈值时触发协程
- `GlobalKillDisplayConfig.cs` — ScriptableObject 配置类

**场景结构**：
```
BattleHUD(Canvas)/KillMilestoneDisplay [KillMilestoneDisplay]
└─ KillDisplay_X × N → 代码动态创建 Image，alpha 控制显隐
```

**新增文件**：
- `Assets/Scripts/UI/KillMilestoneDisplay.cs`
- `Assets/Scripts/Core/GlobalKillDisplayConfig.cs`
- `Assets/ScriptableObjects/GlobalKillDisplayConfig.asset`
- `Assets/Prefabs/UI/MilestoneLabel.prefab`

---

## 敌人动画状态机统一（2025-08-07）

### ✅ 攻击序列改造

**目标**：将所有敌人的攻击从概率驱动改为序列驱动（类似 BOSS 逻辑）。

**数据结构**：
```csharp
[System.Serializable]
public struct AttackStep
{
    public bool isCAttack;
    public float spawnDuration;   // 前摇（秒）
    public float drawDuration;    // 收招（秒）
    public float extraCooldown;   // 额外冷却
    public bool useFlip;          // 攻击时左右翻转
}
// Enemy 中: public List<AttackStep> attackSequence;
```

**已移除的废弃字段**（统一使用 attackSequence）：
- `cAttackProbability` — C技概率（改为 sequence 中 isCAttack 控制）
- `cAttackSpawnDuration` — CA 前摇（改为 AttackStep.spawnDuration）

**保留但含义已变的字段**：
- `attackSpeed` — 保留在 Inspector 但当前未使用，留待后续接入 sequence 节奏控制

**执行流程**：
```
UpdateAttack() → _currentAttackStep 自增（循环）
  → 读取 attackSequence[_currentAttackStep]
  → PlayAttackAnimationTween():
      spawnDuration 内播完攻击动画（Attack/CAttack trigger）
      drawDuration 保持最后一帧 → 回 Idle
  → PerformAttack() 在 spawnDuration 结束时触发 AttackWave
  → cooldown = drawDuration + extraCooldown
```

### ✅ 敌人动画状态机规范

所有普通敌人（101/102/103）共用统一的 Animator 逻辑：

| 状态 | 进入方式 | 退出方式 | 循环 |
|------|---------|---------|------|
| Idle | 默认状态 | Trigger Attack/CAttack/Launch/Dead | 循环 |
| Attack (101/102) | Attack trigger | HasExitTime→Idle | 单次 |
| CAttack1-3 (103) | CAttack trigger→CAttack1 | HasExitTime 链: 1→2→3→Idle | 单次 |
| Launched | AnyState + Launch trigger | 代码控制落地→Play("Idle") | 单次 |
| Dead | AnyState + Dead trigger | 终端状态 | 单次 |
| HitFlash | Hit trigger（仅从 Idle/Attack/CAttack1-3） | HasExitTime(0.9s)→Idle | 单次 |
| Stunned | 代码层状态，Animator 保持 Idle | stunTimer到期→Play("Idle") | 不适用 |

**关键设计决策**：
- Stunned 不是 Animator 状态：眩晕期间敌人原地不动，代码控制计时和退出
- Launched 不进 HitFlash：击飞后空中受击保持 Launched 动画，不切受击闪烁
- CAttack 链通过 HasExitTime 自动推进：代码只需触发 CAttack trigger，Animator 自动 CA1→CA2→CA3→Idle

### ✅ Animator AnyState→HitFlash 修复

**症状**：敌人击飞后 Launched 动画只闪现一瞬间就回 Idle

**根因**：三个控制器均有 `AnyState → HitFlash (Hit trigger)`，在 Launched 期间若收到 Hit trigger 会抢走动画

**修复**：
- 移除 AnyState→HitFlash
- 改为显式转移：从 Idle、Attack、CAttack1-3 添加 Hit trigger→HitFlash
- Launched 和 Dead 不添加，代码侧 `HitFlashRoutine()` 已有守卫（Launched/Dead/QTEAttacking/isCFrame 不 SetTrigger）
- HitFlash→Idle (HasExitTime=0.9)

### ✅ Enemy_103 部署

**动画**：`Assets/Sprites/Enemy/Enemy3/` — Idle、CAttack1-3、Dead、Launched、HitFlash 各 anim 文件

**Animator**：`Assets/Animations/Enemy_103.controller` — CAttack1→CAttack2→CAttack3 链式转移

**Prefab**：`Assets/Resources/EnemyPrefabs/Enemy_103.prefab`

**攻击序列配置**：
- `isCAttack=true, spawnDuration=0.3, drawDuration=0.3, extraCooldown=0.5`（3步相同）
- CA1→CA2→CA3 组成一个完整攻击动作的3帧动画

### ✅ 已知问题与注意事项
- **EnemySpriteController.TriggerHitFlash()** 为空实现 — 组件从未挂载到任何敌人 Prefab，敌人受击闪烁实际依赖材质 flash。保留方法但不建议在新敌人中使用
- `attackSpeed` 字段含义待明确 — 当前未接入序列节奏控制，计划与 AttackStep 各阶段的整体速度倍率挂钩
- 101/102 的 Launched 动画仍是精灵替换方案（EnemySpriteController），未走 Animator。这两者与 103 的 Launched 实现方式不同，后续应统一为 Animator 驱动
<!-- locus:body:end -->
