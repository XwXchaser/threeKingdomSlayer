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
updatedAt: 1781013425808
---

# three-choice-reward-system

## Summary
三选一奖励系统的完整技术设计文档。涵盖数值buff型、道具型（大旋风+落雷）、被动攻击型三种类型的分类、数据流、UI布局、手势冲突矩阵、当前实现状态、待修复问题、配置资产清单和关键设计决策。贯穿整个游戏开发周期。

## Content
# 三选一奖励系统 — 技术设计文档

## 概述

经验升级后的三选一奖励，通过 `UpgradeCategory` 枚举分为三种类型：数值buff型、道具型、被动攻击型。
本文档是完整的技术分析，贯穿整个游戏开发周期，后续具体功能的实现对话均以此文档为设计依据。

### 架构原则（v2 重构）

- **效果为主，触发为辅**：`effectType` 定义「做什么」，触发方式由 `category`（`AttackPassive` / `TimedPassive`）决定「何时做」，两者正交。
- **效果参数始终可见**：Inspector 中效果每级配置不被触发方式影响。
- **触发参数内联到效果 box**：触发字段（间隔/阈值）不设独立区域，直接放在每级效果 box 末尾，按 category 显示对应字段，切换 category 不丢数据。
- **效果自包含**：效果执行不再从攻击上下文借用数据（攻击类型、目标列等），所有参数由自身配置提供。

---

## 一、三种类型分类与数据流

```csharp
public enum UpgradeCategory
{
    Numeric,       // 数值buff型：伤害/攻速/移速/经验倍率等永久加成
    Item,          // 道具型：手势触发的一次性/限次道具（大旋风、落雷等）
    AttackPassive, // 攻击计数被动：每 N 次攻击触发 → PassiveTriggerModule
    TimedPassive   // 定时被动：每 N 秒触发 → TimedPassiveModule
}
```

```
UpgradeDefinition
├─ category=Numeric ──→ UpgradeEffectManager (数值buff型)
│   └─ effectType: damage_multiplier / attack_speed / move_speed / exp_multiplier
│
├─ category=Item ────→ ItemInventory (道具型)
│   ├─ gestureId="circle"                  → WhirlwindController
│   └─ gestureId="long_press_swipe_down"   → ExecuteLightning (InputManager)
│
└─ category=AttackPassive → PassiveTriggerModule（监听 OnAttackPerformed 计数）
    category=TimedPassive  → TimedPassiveModule（Update tick 计时）
```

| 类型 | 判断条件 | 路由模块 | 效果执行 | 叠加规则 |
|------|---------|---------|---------|---------|
| 数值buff | `category == Numeric` | `UpgradeEffectManager` | `ApplyNumericEffect` 加法累加 | 再次获得升级，数值叠加 |
| 道具型 | `category == Item` | `ItemInventory` | `WhirlwindController` / `InputManager.ExecuteLightning` | 再次获得叠加 useCount |
| 被动攻击型 | `category == AttackPassive \|\| TimedPassive` | `TimedPassive` → `TimedPassiveModule` / else → `PassiveTriggerModule` | 统一效果分发（全效果类型） | 再次获得升级，阈值/间隔按每级配置 |

---

## 二、UpgradeDefinition 完整字段

```csharp
// 标识
public UpgradeCategory category;
public string upgradeId;
public string displayName;
public string descriptionTemplate;
public UpgradeRarity rarity;
public int maxLevel = 10;

// 触发方式由 category 决定
// AttackPassive / TimedPassive — 无需额外 triggerMode 字段

// 效果
public string effectType;           // 效果类型标识
public float floatValue;            // 数值型每级浮点加成
public int intValue;                // 数值型每级整数加成
public int secondaryIntValue;       // 数值型第二整数加成
public string stringValue;
public AttackSkillConfig baseAttackConfig;

// UI
public Sprite icon;

// 被动攻击型 — 效果每级配置（始终可见，不随触发选项卡切换）
public List<PhantomLevelConfig> phantomLevels;       // 幻影武器
public List<TimedAoeLevelConfig> timedAoeLevels;     // 喷火
public List<TimedArrowLevelConfig> timedArrowLevels; // 箭雨
public List<ReturnWaveLevelConfig> returnWaveLevels; // 折返波
public List<ChainBounceLevelConfig> chainBounceLevels; // 连锁弹射

// 道具型
public int useCount = 1;
public string gestureId;

// 前置条件
public List<UpgradePrerequisite> prerequisites;
```

### 每级配置 struct（触发参数 + 效果参数分离）

```csharp
// 以喷火为例 — 所有效果 struct 遵循相同模式
[System.Serializable]
public struct TimedAoeLevelConfig
{
    // 触发参数（Inspector 中按 category 切换显示）
    public float intervalSeconds;    // TimedPassive：触发间隔
    public int triggerThreshold;     // AttackPassive：攻击计数阈值

    // 效果参数（始终可见）
    public int damage;
    public List<int> columns;
}

// 折返波 — 新增每级配置
[System.Serializable]
public struct ReturnWaveLevelConfig
{
    public float intervalSeconds;
    public int triggerThreshold;
    public int column;          // 目标列（效果自身参数）
    public int rangeRows;       // 波覆盖排数
    public float damageRatio;   // 折返伤害比例
}

// 连锁弹射 — 新增每级配置
[System.Serializable]
public struct ChainBounceLevelConfig
{
    public float intervalSeconds;
    public int triggerThreshold;
    public int column;
    public int maxBounces;
    public float damageRatio;
}

// 幻影武器 — 扩展了 attackType / targetColumn
[System.Serializable]
public struct PhantomLevelConfig
{
    public float intervalSeconds;
    public int triggerParam;        // AttackCount 阈值
    public AttackType attackType;   // 效果自身攻击类型
    public int targetColumn;        // 效果自身目标列
    public List<PhantomStep> phantomSteps;
}
```

---

## 四、被动攻击型深度设计（v2 重构）

### 4.0 设计理念

**触发方式与效果解耦**：
- `effectType` 定义效果行为（喷火/箭雨/幻影/折返波/连锁弹射/…）
- `category`（`AttackPassive` / `TimedPassive`）定义触发机制
- 一个效果 SO 可以自由切换 category，切换时不丢失数据

**效果自包含**：
- 幻影武器自带 `attackType` 和 `targetColumn`，不再从 `_lastAttackType` 等攻击上下文借用
- 折返波自带 `column`、`rangeRows`、`damageRatio`
- 连锁弹射自带 `column`、`maxBounces`、`damageRatio`
- 定时触发的折返波/连锁弹射使用默认 `AttackType.Pierce`，列号从配置读取

### 4.1 架构

两个触发模块各自处理所有效果类型：

| 模块 | 触发机制 | 处理的效果类型 |
|------|---------|--------------|
| `PassiveTriggerModule` | 监听 `OnAttackPerformed`，攻击计数 | 全部：幻影/折返波/连锁弹射/喷火/箭雨 |
| `TimedPassiveModule` | `Update()` 计时器 | 全部：同上 |

路由逻辑（`UpgradeEffectManager.ApplyUpgrade`）：
```csharp
if (def.category == UpgradeCategory.TimedPassive)
    TimedPassiveModule.Instance.Register(def, newLevel);
else
    PassiveTriggerModule.Instance.Register(def, newLevel);
```

### 4.2 effectType 列表

| effectType | 效果 | 每级配置来源 |
|------------|------|------------|
| `passive_phantom_weapon` | 幻影攻击 | `phantomLevels` |
| `passive_return_wave` | 折返波 | `returnWaveLevels` |
| `passive_chain_bounce` | 连锁弹射 | `chainBounceLevels` |
| `passive_timed_aoe` | 喷火 | `timedAoeLevels` |
| `passive_timed_arrow` | 箭雨 | `timedArrowLevels` |

### 4.3 TimedAoeLevelConfig 定时AOE被动

**数据结构**（定义在 `UpgradeDefinition.cs`）：
```csharp
[System.Serializable]
public struct TimedAoeLevelConfig
{
    public float intervalSeconds;   // 触发间隔（秒）— Timed 模式
    public int triggerThreshold;    // 攻击计数阈值 — AttackCount 模式
    public int damage;              // 单次伤害
    public List<int> columns;       // 受影响列
}
```

**运行时模块**：
- `TimedPassiveModule` — 定时触发版本
- `PassiveTriggerModule` — 攻击计数触发版本（新增 `ExecuteFire` / `ExecuteArrow` 方法）
- 两模块均引用 `fireEffectPrefab` / `arrowEffectPrefab`

### 4.4 整合点

- `UpgradeEffectManager.ApplyUpgrade` → `category` 路由 → `TimedPassiveModule.Register` 或 `PassiveTriggerModule.Register`
- `UpgradeEffectManager.ResetAll` → 重置两模块

### 4.5 UI 显示

- 图标 + 角标显示触发信息（计时类显示间隔秒数，计数类显示阈值）
- **计时被动冷却显示**：图标上叠加 Radial360 顺时针冷却填充 + 右上角倒计时数字
  - 由 `BuffDisplayPanel.Update()` 每帧驱动，读取 `TimedPassiveModule` 公开 API

### 4.6 Inspector 自定 Editor

`UpgradeDefinitionEditor.cs`（`Assets/Scripts/Editor/`）：
- **category 下拉**：选择 `AttackPassive` 或 `TimedPassive` 决定触发方式
- **每级效果 box**（始终可见）：根据 `effectType` 显示效果参数（damage / columns / rowCount / attackType 等）
- **触发字段内联**：每级 box 末尾按 category 动态追加一行 `间隔(秒)` 或 `阈值(次)`，切换 category 即时生效、数据不丢失

---

## 七、当前实现状态

### 已完成

| 组件 | 文件 | 状态 |
|------|------|------|
| UpgradeCategory / TriggerMode 枚举 | `Assets/Scripts/Core/UpgradeDefinition.cs` | ✅ AttackPassive + TimedPassive |
| 触发-效果解耦数据层 | `Assets/Scripts/Core/UpgradeDefinition.cs` | ✅ 每级配置含双触发参数 |
| 自定义 Inspector 选项卡 | `Assets/Scripts/Editor/UpgradeDefinitionEditor.cs` | ✅ |
| PassiveTriggerModule（全效果） | `Assets/Scripts/Core/PassiveTriggerModule.cs` | ✅ 攻击计数触发所有效果类型 |
| TimedPassiveModule（全效果） | `Assets/Scripts/Core/TimedPassiveModule.cs` | ✅ 定时触发所有效果类型 |
| UpgradeEffectManager triggerMode 路由 | `Assets/Scripts/Core/UpgradeEffectManager.cs` | ✅ |
| ItemInventory | `Assets/Scripts/Core/ItemInventory.cs` | ✅ |
| WhirlwindController | `Assets/Scripts/Core/WhirlwindController.cs` | ✅ |
| ShootFireEffect + TimedArrowEffect | `Assets/Scripts/Effect/` | ✅ |
| BuffIcon 冷却显示 | `Assets/Scripts/UI/BuffIcon.cs` | ✅ |
| BuffDisplayPanel 冷却驱动 | `Assets/Scripts/UI/BuffDisplayPanel.cs` | ✅ |
| 现有 SO 资产迁移 | `Assets/ScriptableObjects/Upgrades/Definitions/` | ✅ |

### 已知问题

| # | 位置 | 问题 | 严重度 |
|---|------|------|--------|
| 1 | `ExecuteLightning` | 走 `enemy.TakeDamage()` 绕过 AttackWave，无视觉特效 | 功能缺失 |

---

## 九、关键设计决策汇总

1. **命名规范**：用 `UpgradeCategory`（Numeric/Item/Passive）+ `TriggerMode`（AttackCount/Timed）统一分类
2. **效果为主**：effectType 定义效果，category（AttackPassive/TimedPassive）定义触发，两者正交
3. **效果自包含**：不再从攻击上下文借用数据，所有参数由 SO 自身提供
4. **Inspector**：触发字段内联到效果每级 box 内，按 category 显示对应触发字段，效果字段始终可见，切换 category 不丢数据
5. **数值buff**：加法叠加，再次获得同名升级 = 等级+1
6. **道具 useCount**：-1 = 无限次，正数 = 使用次数
7. **大旋风 floatValue**：每秒伤害跳数
8. **大旋风 rangeRows**：从 `baseAttackConfig` 读取
9. **落雷中心**：固定 (col=2, row=2)，5×5 切比雪夫扩散，加法衰减
10. **落雷 BOSS**：无论位置全额伤害
11. **手势优先级**：QTE > 道具 > 攻击
12. **手势唯一性**：一个 gestureId = 一种道具，不可能冲突
13. **被动计数范围**：所有攻击类型都计数
14. **UI 位置**：屏幕左侧，Column A (数值+被动) | Column B (道具)
15. **道具 UI**：消耗后消失，下方上移补位
16. **被动 UI**：显示触发阈值数字（计数类）或冷却倒计时（计时类）
17. **图标**：由用户提供 sprite，通过 `UpgradeDefinition.icon` 配置
