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
updatedAt: 1780166282770
---

# phase3-development-summary

## Summary
第三期局内成长系统 + 击杀进度条 + 敌人动画状态机统一，已完成。更新至 2025-08-07 — 最终确认敌人动画规范 + 新建敌人清单。

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

---

## 敌人动画状态机 — 最终确认规范（2025-08-07）

### Animator 参数
所有敌人控制器统一使用 4 个 Trigger：`Attack`、`CAttack`、`Hit`、`Launch`、`Dead`

### 状态转移规则

| 状态 | 进入 | 退出 | 循环 |
|------|------|------|------|
| **Idle** | 默认 | Attack/CAttack/Hit/Launch/Dead trigger | 循环 |
| **Attack** (101/102) | Idle + Attack trigger | HasExitTime→Idle | 单次 |
| **CAttack1→2→3→Idle** (103) | Idle + CAttack trigger→CAttack1 | HasExitTime 链: 1→2→3→Idle | 单次 |
| **HitFlash** | Idle/Attack/CAttack1-3 + Hit trigger | HasExitTime→Idle | 单次 |
| **Launched** | AnyState + Launch trigger | 代码落地→Play("Idle") | 单次 |
| **Dead** | AnyState + Dead trigger | 终端 | 单次 |

### 关键守则
- **AnyState 仅用于 Dead 和 Launched**：这两种是"无论什么状态都应立刻触发"的
- **Hit 不用 AnyState**：从可打断的 Idle/Attack/CAttack 显式转移。Launched/Dead 不添加 Hit 转移
- **代码 + Animator 双重守卫**：`HitFlashRoutine()` 中检查 Launched/Dead/QTEAttacking/isCFrame 不 SetTrigger(Hit)
- **CAttack 链由 Animator HasExitTime 自动推进**：代码只需触发一次 CAttack trigger

---

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

---

## 已完成功能清单

### ✅ 经验值系统
- `PlayerState.currentExp` / `currentLevel` / `AddExp(float)`
- `ExpCurveConfig` ScriptableObject
- `BattleHUD.expSlider` / `expLevelText`

### ✅ 经验宝石飞行系统
- `ExpGem` — 屏幕空间 UI Image，飞向 ExpBar Fill 右端
- `ExpGemManager` — 单例管理，防积压加速

### ✅ 效果系统
- `UpgradeEffectManager` — 数值累积 + 行为注册表
- 范围加成：stab_range_boost / sweep_range_boost

### ✅ 三选一奖励（8种）
| ID | 名称 | 类型 | 效果 |
|----|------|------|------|
| damage_plus | 神力 | Numeric | 伤害+5%/级 |
| attack_speed | 急速 | Numeric | 攻速+5%/级 |
| move_speed | 疾行 | Numeric | 移速+5%/级 |
| wisdom | 智慧 | Numeric | 经验+5%/级 |
| stab_range_boost | 延长 | Numeric | 戳击范围+1排/级 |
| sweep_range_boost | 波长 | Numeric | 横扫范围+1排/级 |
| phantom_weapon | 虚幻武器 | Passive | 每5次攻击触发幻影 |
| test_damage_boost | 伤害加成 | Item | 使用后+100%伤害 |

### ✅ 虚幻武器被动
- `PassiveTriggerModule` — 攻击计数器
- 延迟攻击 + 蓝色伤害数字

### ✅ 玩家受击反馈
- 全屏 hitted 边框闪白 + 镜头抖动
- `PlayerHitFeedback` 组件

### ✅ 攻击打断系统
- 三级打断：普通/C技/QTE
- C技霸体窗口 + Parry/Launch 打断

### ✅ 左侧 Buff 图标面板
- 双列（ColumnA 被动/数值，ColumnB 道具）
- 道具点击触发效果

### ✅ 击杀进度条 & 击杀里程碑闪现
- 纵向填充条 + 里程碑标签
- 全局击杀闪光

### ✅ 敌人攻击序列改造
- 概率驱动 → 序列驱动（`attackSequence: List<AttackStep>`）
- 废弃字段移除：`cAttackProbability`

### ✅ Enemy_103 部署
- CAttack1→2→3 链式动画
- Animator: `Assets/Animations/Enemy_103.controller`
- Prefab: `Assets/Resources/EnemyPrefabs/Enemy_103.prefab`
- 素材：`Assets/Sprites/Enemy/Enemy3/`

### ✅ Animator 统一修复
- 移除 AnyState→HitFlash，改为显式转移
- 101/102/103 共用规范（见上文状态转移表）

---

## 新建敌人所需清单

制作新敌人需要准备以下内容：

1. **精灵序列图**（放入 `Assets/Sprites/Enemy/`）
   - Idle（循环）
   - Attack（普通攻击，1个或多个精灵帧，单次播放）— 如果是C技型：CAttack1/CAttack2/CAttack3 各1帧
   - HitFlash（受击闪烁，单帧，短暂显示）
   - Dead（死亡）
   - Launched（击飞/浮空）

2. **Animation Clip**（从精灵图创建，放入 `Assets/Animations/`）
   - Enemy_XXX_Idle.anim
   - Enemy_XXX_Attack.anim（或 CAttack1/2/3.anim）
   - Enemy_XXX_HitFlash.anim
   - Enemy_XXX_Dead.anim
   - Enemy_XXX_Launched.anim

3. **Animator Controller**（参考现有 101/102/103 的转移规则创建 `.controller`）

4. **Enemy Prefab**（放入 `Assets/Resources/EnemyPrefabs/`）
   - Enemy.cs 组件 + Animator + SpriteRenderer + Collider
   - 配置 `attackSequence` 列表

5. **StageConfig 注册** — 在 `testStage.asset` 中添加 spawn 条目

---

## 已修复的Bug总览
- 9-slice sprite border=0 → 设置非零 Border
- InputManager Debug.Log 帧刷屏 → 注释
- 中文字体 → 方正粗黑宋简体 SDF
- CanvasGroup.blocksRaycasts 拦截输入 → 显隐时切换
- 全屏 Image.raycastTarget 拦截输入 → 设为 false
- 代码覆写Inspector值 → 删除所有硬编码 RectTransform 赋值
- Missing Prefab 残留 → 重建为普通GameObject
- ItemInventory 未挂载 → 添加到 Manager
- BuffIcon._button 未串接 → Prefab 中拖入
- Slider Fill 透明度 → Image type=Simple + alphaIsTransparency=True
- Animator AnyState→HitFlash 打断 Launched → 改为显式转移
- Stab Wave 视觉方向错误 → 改用最远目标方向
- sharedHealthGroup 打断失效 → 打断逻辑移到 sharedHealthGroup 前
<!-- locus:body:end -->
