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
updatedAt: 1779691799262
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

### ✅ 被动攻击奖励：虚幻武器 (PhantomWeapon) — Phase 2 完成
- `PassiveTriggerModule` — 攻击计数器+幻影触发逻辑
- `AttackSystem.ExecutePhantomAttack()` — 穿通执行攻击
- 延迟攻击（delaySeconds 可逐级配置）
- 蓝色伤害数字 (#4D7FFF) 以区分幻影伤害
- per-level 独立配置：TriggerParam / DamageRatio / Alpha / delaySeconds

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
2. ~~虚幻武器待验证~~ ✅ 已完成（延迟攻击、蓝色伤害数字、per-level 配置）
3. **cardPrefab 空引用偶现**（低优先级）：Editor 中已正确赋值，需确认 Play Mode 是否复现
<!-- locus:body:end -->
