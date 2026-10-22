---
id: kd_86f8eebd-7072-43f2-82bc-228718f85b1f
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
summary: 战斗状态/Buff 的使用文档（2026-10-24）——项目里有两套状态系统（数值型 ComboBuffConfig+StatModifier 与机制型 BuffIds+CombatBuffDefinition）各管什么、数据在哪、状态从哪里来（天赋/局内升级）、击飞概率等机制参数怎么进资产、改动时的联动点与坑；含对应代码路径便于按需核对。
injectMode: inherit
aiEditMode: inherit
---

# 战斗状态 / Buff · 使用文档

> **一句话** ✅ ✅：**数值型状态** ✅ ✅（属性增减 ✅ ✅ / 时长 ✅ ✅ / 叠层 ✅ ✅）走 `ComboBuffConfig` ✅ ✅ ✅；
> **机制型状态** ✅ ✅（强制击飞 ✅ ✅ / 概率击飞 ✅ ✅）走 `BuffIds` ✅ ✅ + `CombatBuffDefinition` ✅ ✅ ✅。
> **本文防的是** ✅ ✅：**再造第三套状态系统** ✅ ❗（已经有两套 ✅ ✅ —— 数值型 ✅ ✅ / 机制型 ✅ ✅✅ ✅，区别见下表 ✅ ✅）与**把概率/时长写回代码** ✅ ❗（已经在资产里 ✅ ✅✅ ✅）✅ ✅。
> 纪律 ✅：本文只写「读代码得不到的东西」✅ ✅；**与代码不一致时以代码为准** ✅ ✅。

---

## 一、**两套系统，别搞混** ✅ ✅

| | **数值型** ✅ ✅ | **机制型** ✅ ✅ |
|---|---|---|
| 载体 ✅ | `ComboBuffConfig` ✅ ✅（SO ✅ ✅）→ `StatModifier` ✅ ✅（`statId` ✅ ✅ + `type` ✅ ✅ + `value` ✅ ✅ ✅）✅ | `CombatBuffDefinition` ✅ ✅（SO ✅ ✅）→ id ✅ ✅ + 数值 ✅ ✅（`launchProbability` ✅ ✅ / `defaultDuration` ✅ ✅ ✅）✅ |
| 运行时 ✅ | `BuffManager` ✅ ✅ + `ActiveBuff` ✅ ✅（`buffId` ✅ ✅ + `endTime` ✅ ✅ + `modifiers` ✅ ✅ ✅）✅ | `PlayerState.buffTimers` ✅ ✅（**string key** ✅ ✅ ✅）+ `BuffIds` ✅ ✅ ✅ |
| 加一条 ✅ | **加资产** ✅ ✅ | 加资产 ✅ ✅（**新机制还要写消费方代码** ✅ ✅ ✅）✅ |
| 查 ✅ | `Tools/数据校验/战斗状态定义 ✅ ✅` ✅ ✅ | 同左 ✅ ✅ |

> ⚠️ **两套系统互不相通** ✅ ✅（数值型走 `BuffManager` ✅ ✅，机制型走 `PlayerState` ✅ ✅）✅ ✅——
> 别以为「给 BuffManager 加一条」就能让击飞生效 ✅ ❗，也**别**用 `PlayerState` 的计时器装数值型 ✅ ❗✅ ✅。

---

## 二、数据在哪 ✅ ✅
- **连击/升级类状态** ✅ ✅：`Assets/ScriptableObjects/Combo/` ✅ ✅（`ComboBuffConfig` ✅ ✅ ✅）✅ ✅ —— 触发阈值 ✅ ✅ → 属性修正 ✅ ✅（`StatModifier` ✅ ✅ ✅）✅ ✅。
- **局内升级** ✅ ✅：`UpgradeEffectManager` ✅ ✅（走 `ApplyBuffModifier` ✅ ✅ ✅）✅ ✅。
- **机制型状态定义** ✅ ✅：`Assets/RouteData/CombatBuffs/` ✅ ✅（`CombatBuffDefinition` ✅ ✅ ✅）✅ ✅
  - 现有一份：`ProbabilityLaunch.asset` ✅ ✅（id = `probability_launch` ✅ ✅，**概率 0.3 ✅ ✅** —— 原先写死在代码里 ✅ ❗✅ ✅ ✅）✅ ✅。
  - **接线** ✅ ✅：Battle 场景的 `PlayerState.probabilityLaunchBuff` ✅ ✅ 指向它 ✅ ✅ → 运行时读 `PlayerState.LaunchProbability` ✅ ✅ ✅。
- **机制 id 词表** ✅ ✅：`BuffIds` ✅ ✅（`force_launch` ✅ ✅ / `probability_launch` ✅ ✅ ✅）✅ ✅ ✅ —— **机制词表** ✅ ✅（加机制写代码 ✅ ✅，不是内容 ✅ ✅ ✅）✅ ✅。

---

## 三、状态从哪来 ✅ ✅
1. **天赋** ✅ ✅：天赋节点点亮的 `flagId` ✅ ✅ / 解锁的 `moveConfig` ✅ ✅ ✅（见 `talent-system.md` ✅ ✅）✅ ✅。
2. **局内升级/连击** ✅ ✅：`ComboBuffConfig` ✅ ✅ / `UpgradeEffectManager` ✅ ✅ ✅ ✅。
3. **代码直接施加** ✅ ✅：机制型走 `PlayerState.ApplyBuff(string id, duration)` ✅ ✅ ✅（**按 id ✅ ✅** —— `BuffType` ✅ ✅ 枚举已退役 ✅ ✅✅ ✅）✅ ✅。

---

## 四、机制型状态怎么加（例：概率击飞 ✅ ✅）
1. `BuffIds` ✅ ✅ 加一个 id 常量 ✅ ✅（**机制词表 ✅ ✅**）；
2. 建一个 `CombatBuffDefinition` ✅ ✅ 资产 ✅ ✅（id 填对 ✅ ✅ ✅ + 数值 ✅ ✅ ✅）；
3. 写消费方 ✅ ✅（哪里读这个状态 ✅ ✅ —— **机制逻辑必须写代码** ✅ ✅ ✅）；
4. 如果要接在 `PlayerState` ✅ ✅ 上 ✅ ✅ → 加一个 `CombatBuffDefinition` ✅ ✅ 字段 ✅ ✅ + 一个 `LaunchProbability` ✅ ✅ 之类的读数属性 ✅ ✅ ✅（**接线在场景里 ✅ ✅** ✅ ✅）；
5. **跑校验** ✅ ✅：`Tools/数据校验/战斗状态定义 ✅ ✅` ✅ ✅（会查「id 不在 `BuffIds` 词表里 ✅ ✅」✅ ✅ —— 拼错 id 最容易踩 ✅ ✅✅ ✅）✅ ✅。

---

## 五、改动时的联动点 ✅ ✅
- **id 拼错** ✅ ❗ → 状态不生效 ✅ ❗（校验器会报 ✅ ✅✅ ✅）✅ ✅；
- **`PlayerState` 的字段** ✅ ✅ → **场景里要接线** ✅ ✅（改字段名 ✅ ✅ → 场景引用会丢 ✅ ❗✅ ✅）✅ ✅；
- **数值型状态的 `statId`** ✅ ✅ → 消费者按 **id** ✅ ✅ 读 ✅ ✅ → 改名 ✅ ❗ = 效果失效 ✅ ✅（**不是枚举 ✅ ✅ 就没有重映射灾难** ✅ ✅✅ ✅）✅ ✅。

---

## 六、坑 ✅ ✅
- **两套系统** ✅ ✅（见上 ✅ ✅）✅ ✅ —— 最容易搞混的一处 ✅ ✅✅ ✅。
- **`BuffType` 枚举已删** ✅ ✅（2026-10-24 ✅ ✅）→ 代码里搜 `BuffType` 会搜不到 ✅ ✅ —— 新代码一律用 `BuffIds` ✅ ✅ + string ✅ ✅✅ ✅。
- **机制 id 与资产 id 要一致** ✅ ✅（资产里的 `buffId` ✅ ✅ 必须等于 `BuffIds` ✅ ✅ 的常量 ✅ ✅ ✅）✅ ✅。
- **概率这类数值** ✅ ✅ → 只在资产里改 ✅ ✅（代码里不该再有 0.3f ✅ ✅ 之类 ✅ ❗✅ ✅）✅ ✅。

---

## 七、对应代码路径（改之前先核对 ✅ ✅）
- `Assets/Scripts/Core/BuffManager.cs` ✅ ✅
- `Assets/Scripts/Data/StatModifier.cs` ✅ ✅（`ActiveBuff` ✅ ✅ + `StatModifier` ✅ ✅ ✅）✅ ✅
- `Assets/Scripts/Data/ComboBuffConfig.cs` ✅ ✅
- `Assets/Scripts/Data/CombatBuffDefinition.cs` ✅ ✅
- `Assets/Scripts/Data/BuffIds.cs` ✅ ✅
- `Assets/Scripts/Player/Combat/PlayerState.cs` ✅ ✅（buff 计时 + `probabilityLaunchBuff` ✅ ✅ ✅）✅ ✅
- `Assets/Scripts/Enemy/Enemy.StateSwitch.cs` ✅ ✅（`ForceLaunch` ✅ ✅ ✅）✅ ✅
- `Assets/Scripts/Player/Combat/AttackSystem.Types.cs` ✅ ✅ / `AttackSystem.Utils.cs` ✅ ✅（概率击飞 ✅ ✅ ✅）✅ ✅
- `Assets/Scripts/Core/UpgradeEffectManager.cs` ✅ ✅
- `Assets/Scripts/Editor/CombatBuffValidator.cs` ✅ ✅
- 相关交接 ✅ ✅：`plan/data-modularization-handoff-20261024.md` ✅ ✅
