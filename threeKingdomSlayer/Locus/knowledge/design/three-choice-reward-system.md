---
id: kd_88224985-75b2-4921-aff3-d5e2a3b1bd6f
type: design
path: three-choice-reward-system.md
title: three-choice-reward-system
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1779547549506
updatedAt: 1779548227456
---

# three-choice-reward-system

## Summary
三选一奖励系统的完整技术设计文档。涵盖数值buff型、道具型（大旋风+落雷）、被动攻击型三种类型的分类、数据流、UI布局、手势冲突矩阵、当前实现状态、待修复问题、配置资产清单和关键设计决策。贯穿整个游戏开发周期。

## Content
# 三选一奖励系统 — 技术设计文档

## 概述

经验升级后的三选一奖励，通过 `UpgradeCategory` 枚举分为三种类型：数值buff型、道具型、被动攻击型。
本文档是完整的技术分析，贯穿整个游戏开发周期，后续具体功能的实现对话均以此文档为设计依据。

---

## 一、三种类型分类与数据流

```csharp
public enum UpgradeCategory
{
    Numeric,   // 数值buff型：伤害/攻速/移速/经验倍率等永久加成
    Item,      // 道具型：手势触发的一次性/限次道具（大旋风、落雷等）
    Passive    // 被动攻击型：每N次攻击自动触发效果
}
```

```
UpgradeDefinition.category
├─ Numeric  ──────────→ UpgradeEffectManager (数值buff型)
│   └─ effectType: damage_multiplier / attack_speed / move_speed / exp_multiplier
│
├─ Item     ──────────→ ItemInventory (道具型)
│   ├─ gestureId="circle"               → WhirlwindController
│   └─ gestureId="long_press_swipe_down"→ ExecuteLightning (InputManager)
│
└─ Passive  ──────────→ PassiveTriggerModule (被动攻击型, 新建)
    └─ triggerParam = 触发阈值
```

| 类型 | 判断条件 | 存储位置 | 效果执行 | 叠加规则 |
|------|---------|---------|---------|---------|
| 数值buff | `category == Numeric` | `UpgradeEffectManager._appliedUpgrades` | `ApplyNumericEffect` 加法累加 | 再次获得升级，数值叠加 |
| 道具型 | `category == Item` | `ItemInventory._items` | `WhirlwindController` / `InputManager.ExecuteLightning` | 再次获得叠加 useCount |
| 被动攻击型 | `category == Passive` | `PassiveTriggerModule` (新建) | 独立 effectType 体系 | 再次获得升级，阈值减小(待确认) |

---

## 二、UpgradeDefinition 完整字段

```csharp
// 标识
public UpgradeCategory category;   // 奖励类型（显式枚举）
public string upgradeId;
public string displayName;
public string descriptionTemplate;
public UpgradeRarity rarity;
public int maxLevel = 10;

// 效果 (数值型 + 被动型共用)
public string effectType;
public float floatValue;
public int intValue;
public string stringValue;
public AttackSkillConfig baseAttackConfig;

// UI
public Sprite icon;

// 被动攻击型（category=Passive 时生效）
public int triggerParam;           // 触发阈值（每X次攻击触发一次效果）

// 道具型（category=Item 时生效）
public int useCount = 1;           // -1=无限次
public string gestureId;           // "circle" | "long_press_swipe_down"

// 前置条件
public List<UpgradePrerequisite> prerequisites;
```

---

## 三、道具型深度设计

### 3.1 大旋风（category=Item, gestureId="circle"）

**触发方式**：按住屏幕画圈（以屏幕中心为原点，不需闭合，累积 270° 即触发）

**实现文件**：`Assets/Scripts/Core/WhirlwindController.cs`

**核心参数**：
- `detectionAngle`：圈累积阈值 270°
- `minRadius` / `maxRadius`：有效半径范围（像素）
- `directionLockReverseThreshold`：反向检测阈值 30°
- `floatValue`：每秒伤害跳数（`tickInterval = 1f / floatValue`）
- `baseAttackConfig.rangeRows`：影响排数，基础为整排攻击
- `baseAttackConfig.damage`：每次伤害值（最终乘 `damageMultiplier`）

**运行时行为**：
1. 手指按下 → 每帧 `UpdateCircleDetection(fingerPos)` 累加角度
2. 累积 ≥ 270° → 触发激活，`ItemInventory.TryConsume("circle")`
3. 激活后 → `Update()` 每 tick 对 rangeRows 内敌人造成伤害（走 `AttackWave.Create`）
4. 激活后 → `TickActive(fingerPos)` 追踪角度，每 360° 击飞范围内所有敌人
5. 手指离开 → `Deactivate()`，圈检测重置

**设计决策**：
- 圈检测以屏幕中心为原点，不需要圈闭合
- 方向锁定后反向超过 30° 重置累积
- 半圈松手（未触发）走正常手势识别 → 可能触发普通攻击（符合直觉）
- 伤害走 `AttackWave.Create` 管道，保证统一伤害计算

### 3.2 落雷（category=Item, gestureId="long_press_swipe_down"）

**触发方式**：长按（≥ `longPressDuration`）+ 下滑（与下方向夹角 < `swipeDownAngleThreshold`）

**实现位置**：`InputManager.TryConsumeItemGesture()` + `ExecuteLightning()`

**核心参数**：
- `longPressDuration`：长按阈值（复用现有，默认 0.3s）
- `swipeDownAngleThreshold`：下滑角度阈值 30°
- `baseAttackConfig.damage`：基础伤害（最终乘 `damageMultiplier`）
- `baseAttackConfig.damageType`：伤害类型

**网格模型**：
- 5×5 切比雪夫网格，中心 (col=2, row=2)
- 切比雪夫距离 = max(|col-2|, |row-2|)，最大距离 2
- 共 25 格

**伤害衰减**（加法）：
| 距离 | 衰减 | 伤害比例 |
|------|------|---------|
| 0 | 0% | 100% |
| 1 | 10% | 90% |
| 2 | 20% | 80% |

**BOSS 规则**：无论 BOSS 处于 5×5 网格内任何位置，均承受全额伤害（不衰减）

**已知问题**：
- 落雷走 `enemy.TakeDamage()` 绕过 `AttackWave`，无视觉特效 — 后续可补充

### 3.3 道具库存（ItemInventory）

**实现文件**：`Assets/Scripts/Core/ItemInventory.cs`

**关键设计**：
- `gestureId` 作为唯一键：一个手势只对应一种道具
- 多次获得同一道具叠加 `useCount`
- `useCount = -1` = 无限次，`TryConsume` 永远返回 true 但不扣减
- `OnItemChanged(gestureId, remainingUses, wasRemoved)` 事件驱动 UI 刷新
- `ClearAll()` 新对局重置

---

## 四、被动攻击型深度设计

### 4.1 架构

**新建文件**：`Assets/Scripts/Core/PassiveTriggerModule.cs`

**职责**：
- 维护 `Dictionary<string, PassiveState>` (upgradeId → {counter, threshold, definition})
- 监听 `AttackSystem.OnAttackPerformed`（需在 AttackSystem 新增此事件）
- 每次攻击 counter++，到达 threshold 时执行效果 + 重置 counter
- 提供 `Register(UpgradeDefinition def)` / `Unregister(string upgradeId)` / `ResetAll()`

**数据模型**：
```csharp
struct PassiveState {
    public UpgradeDefinition definition;
    public int currentCount;
    public int threshold;       // = def.triggerParam
}
```

### 4.2 独立 effectType 体系

与数值型的 effectType 完全隔离，使用独立命名空间：

| effectType | 行为 | 参数来源 |
|------------|------|---------|
| `passive_extra_stab` | 对随机一列执行额外戳击 | `baseAttackConfig` |
| `passive_aoe_damage` | 对 rangeRows 内敌人造成伤害 | `baseAttackConfig` + `floatValue` |
| `passive_heal` | 回复自身血量 | `floatValue` |
| （后续扩展） | ... | ... |

### 4.3 关键设计约束

- **每种被动独立计数器**：获得时间不同、阈值不同
- **所有攻击类型都计数**：Stab/Slash/Pierce/Sweep/Launch/Parry
- **效果不写死**：根据配表 `effectType` 决定行为
- **AttackSystem 事件发射**：在 `TryExecuteAttack` 的 `hitAny==true` 后发射一次（非每敌人一次）

### 4.4 整合点

- `UpgradeEffectManager.ApplyUpgrade` → `category==Passive` 分支 → `PassiveTriggerModule.Register(def)`
- `UpgradeEffectManager.ResetAll` → `PassiveTriggerModule.ResetAll()`
- `AttackSystem.TryExecuteAttack` → `hitAny==true` → `OnAttackPerformed?.Invoke()`

### 4.5 UI 显示

- 图标 + 角标显示触发阈值数字（如 "5"）
- 不显示实时进度

---

## 五、UI 布局设计（BuffDisplayPanel）

### 5.1 布局规格

- 位置：屏幕左侧
- 两列纵向排列，水平并排
- Column A（左）：数值buff型 + 被动攻击型（持久显示）
- Column B（右）：道具型（消耗后消失，剩余上移补位）

### 5.2 层级结构

```
BuffDisplayPanel (Canvas, ScreenSpace-Overlay, 左侧锚定)
├─ ColumnA (VerticalLayoutGroup)
│   ├─ BuffIcon ("神力" damage+10%)     ← icon + "Lv.3"
│   ├─ BuffIcon ("疾风" attack speed)   ← icon + "Lv.1"
│   └─ BuffIcon ("反击" 每5次攻击)      ← icon + "5"
│
└─ ColumnB (VerticalLayoutGroup)
    ├─ BuffIcon ("大旋风" 画圈)          ← icon + "∞"
    ├─ BuffIcon ("落雷" 长按下滑)        ← icon + "2" → 用完后 fade out + 上移
    └─ (空位自动补位)
```

### 5.3 BuffIcon 组件规格

- `SetIcon(Sprite)` — 设置图标
- `SetBadge(string text)` — 角标文字（"Lv.3" / "5" / "∞" / "2"）
- `PlayConsumeAnimation()` — 道具消耗消失动画
- `PlayTriggerFlash()` — 被动触发闪烁

### 5.4 数据绑定

| 区域 | 数据源 | 事件 |
|------|--------|------|
| ColumnA（数值） | `UpgradeEffectManager._appliedUpgrades` | `OnUpgradeApplied` |
| ColumnA（被动） | `PassiveTriggerModule` | `OnPassiveRegistered` / `OnPassiveTriggered` |
| ColumnB（道具） | `ItemInventory._items` | `OnItemChanged(gestureId, remainingUses, wasRemoved)` |

### 5.5 道具消耗动画

- `wasRemoved==true` → 播放 fade out / scale down → 移除 GameObject → LayoutGroup 自动上移补位

---

## 六、手势优先级与冲突矩阵

### 6.1 优先级链

```
Update() 每帧:
  画圈检测（按住中持续，达到 270° → 激活 Whirlwind）

松开时:
  Whirlwind 激活中 → Deactivate（不触发任何攻击）
  Whirlwind 未激活 → ProcessGesture:
    QTE 拦截 (TryConsumeQTEInput)
    → 道具手势 (TryConsumeItemGesture)
    → 蓄力攻击 (pressDuration >= minChargeTime)
    → 普通攻击
```

### 6.2 冲突场景验证

| 场景 | 结果 | 正确性 |
|------|------|--------|
| 画圈 → 触发 → 继续画圈 → 松手 | Deactivate，不触发攻击 | ✅ |
| 画圈 → 未到 270° → 松手 | 走 ProcessGesture，可能触发划动攻击 | ✅ |
| 长按 0.3s + 下滑 ≥30° | 落雷触发，阻止攻击 | ✅ |
| 长按 + 下滑 <30° | 判定为长按 → 穿刺 | ⚠️ 边缘情况，角度阈值 30° 偏低风险 |
| 同时持有 circle + long_press_swipe_down | 画圈在 Update 优先，下滑在松开时 | ✅ 不冲突 |
| QTE 活跃 + 任何手势 | QTE 优先拦截 | ✅ |
| 暂停期间画圈 | `Time.timeScale==0` → Update 首行 return | ✅ 自动冻结 |
| 一只手画圈 + 另一只手触摸 | `Input.touchCount>0` 优先触摸，mouse 跳过 | ✅ |

### 6.3 一 gesture 一 item 保证

- `ItemInventory` 以 `gestureId` 为 key，一个手势只存一个 ItemStock
- 不存在同手势匹配两个不同道具的情况
- 多次获得同一道具 → useCount 叠加

---

## 七、当前实现状态

### 已完成

| 组件 | 文件 | 状态 |
|------|------|------|
| UpgradeCategory 枚举 + category 字段 | `Assets/Scripts/Core/UpgradeDefinition.cs` | ✅ |
| ItemInventory | `Assets/Scripts/Core/ItemInventory.cs` | ✅ 完整实现 |
| WhirlwindController | `Assets/Scripts/Core/WhirlwindController.cs` | ✅ 完整实现 |
| UpgradeEffectManager 道具路由 | `Assets/Scripts/Core/UpgradeEffectManager.cs` | ✅ 道具路由 + ResetAll |
| InputManager 画圈检测 | `Assets/Scripts/Player/InputManager.cs` | ✅ Update 中检测 + TickActive |
| InputManager 落雷框架 | `Assets/Scripts/Player/InputManager.cs` | ✅ 落雷完整实现 (GetCurrentRow 已修复) |

### 已知问题

| # | 位置 | 问题 | 严重度 |
|---|------|------|--------|
| 1 | `ExecuteLightning` | 走 `enemy.TakeDamage()` 绕过 AttackWave，无视觉特效 | 功能缺失 |

### 待实现

| 步骤 | 内容 | 依赖 |
|------|------|------|
| Step 5 | 落雷补充视觉特效 | 无 |
| Step 6 | AttackSystem.OnAttackPerformed 事件 + PassiveTriggerModule | 无 |
| Step 7 | BuffDisplayPanel + BuffIcon Prefab + UI 布局 | Step 6 |
| Step 8 | Scene 集成 + 配置资产创建 | Step 7 |
| Step 9 | 全流程测试 | Step 8 |

---

## 八、配置资产清单

需要策划创建的 `UpgradeDefinition` 和 `AttackSkillConfig` 资产：

### UpgradeDefinition

| 文件名 | category | 关键字段 |
|--------|----------|---------|
| `WhirlwindItem.asset` | Item | gestureId=circle, useCount=1/-1, floatValue=每秒N次, baseAttackConfig=大旋风 |
| `LightningItem.asset` | Item | gestureId=long_press_swipe_down, useCount=1/-1, baseAttackConfig=落雷 |
| `PassiveStab_5.asset` | Passive | triggerParam=5, effectType=passive_extra_stab |
| ... | ... | 后续扩展 |

### AttackSkillConfig

| 文件名 | 关键字段 |
|--------|---------|
| `WhirlwindAttack.asset` | rangeRows, damage, damageType, attackWavePrefab |
| `LightningAttack.asset` | damage, damageType |

### BuffIcon 精灵

| 用途 | 路径 |
|------|------|
| 数值buff图标 | 用户提供 → 配置到对应 UpgradeDefinition.icon |
| 道具图标 | 用户提供 → 配置到对应 UpgradeDefinition.icon |
| 被动图标 | 用户提供 → 配置到对应 UpgradeDefinition.icon |

---

## 九、关键设计决策汇总

1. **命名规范**：用 `UpgradeCategory` 枚举（Numeric/Item/Passive）统一称呼三种类型
2. **数值buff**：加法叠加，再次获得同名升级 = 等级+1
3. **道具 useCount**：-1 = 无限次，正数 = 使用次数
4. **大旋风 floatValue**：每秒伤害跳数（`1/floatValue` 秒一跳）
5. **大旋风 rangeRows**：从 `baseAttackConfig` 读取，基础为整排攻击
6. **落雷中心**：固定 (col=2, row=2)，5×5 切比雪夫扩散，加法衰减
7. **落雷 BOSS**：无论位置全额伤害
8. **手势优先级**：QTE > 道具 > 攻击
9. **手势唯一性**：一个 gestureId = 一种道具，不可能冲突
10. **被动计数器**：独立模块 `PassiveTriggerModule`，独立 effectType 体系
11. **被动计数范围**：所有攻击类型都计数
12. **UI 位置**：屏幕左侧，Column A (数值+被动) | Column B (道具)
13. **道具 UI**：消耗后消失，下方上移补位
14. **被动 UI**：只显示触发阈值数字，不显示进度
15. **图标**：由用户提供 sprite，通过 `UpgradeDefinition.icon` 配置

---

## 十、后续扩展预留

- `IGestureRecognizer` 接口：新道具手势可插拔注册（已在设计文档中预留）
- `PassiveTriggerModule` effectType 可扩展：新增枚举值 + switch case
- `BuffDisplayPanel` 可支持更多列（如 Column C 用于限时 Buff）
- `UpgradePoolConfig` 可配置道具型/被动型在随机池中的权重

---

## 参考文件

| 文件 | 职责 |
|------|------|
| `design/in-game-growth-system.md` | 局内成长三期总设计 |
| `design/game-mechanics.md` | 游戏完整机制介绍 |
| `memory/phase3-development-summary.md` | Phase 3 开发状态记录 |
| `Assets/Scripts/Core/UpgradeDefinition.cs` | 升级定义 SO |
| `Assets/Scripts/Core/ItemInventory.cs` | 道具库存 |
| `Assets/Scripts/Core/WhirlwindController.cs` | 大旋风控制器 |
| `Assets/Scripts/Core/UpgradeEffectManager.cs` | 效果管理器 |
| `Assets/Scripts/Player/InputManager.cs` | 输入管理器 |
| `Assets/Scripts/Player/AttackSystem.cs` | 攻击系统 |
| `Assets/Scripts/Enemy/Enemy.cs` | 敌人实体 |
