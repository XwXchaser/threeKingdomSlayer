---
id: kd_8961ceea-6a14-4d94-9fa5-da0080882241
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
summary: 道具与局内成长（升级/主动技能/经验/掉落）域的使用文档（2026-10-24）——各系统的数据载体与职责边界（UpgradeDefinition/ActiveSkillDefinition/ItemInventory/ExpCurve/DropItemPool）、升级三选一与主动技能的关系、铜钱与经验的消费方；含加一条成长内容要动哪几处、坑与代码路径，便于按需核对。
injectMode: inherit
aiEditMode: inherit
---

# 道具 / 局内成长 · 使用文档

> **一句话** ✅ ✅：局内成长有**四条独立的数据线** ✅ ✅ —— **升级三选一** ✅ ✅（`UpgradeDefinition` ✅ ✅）、
> **主动技能** ✅ ✅（`ActiveSkillDefinition` ✅ ✅）、**道具/掉落** ✅ ✅（`ItemInventory` ✅ ✅ + `DropItemPoolConfig` ✅ ✅）、
> **经验/等级** ✅ ✅（`ExpCurveConfig` ✅ ✅）✅ ✅；**它们共用铜钱** ✅ ✅。
> **本文防的是** ✅ ✅：**再加一套「成长」系统** ✅ ❗（四条线已经分好了 ✅ ✅✅ ✅）与**把升级效果写成硬编码** ✅ ❗（效果走 `StatModifier` ✅ ✅✅ ✅）✅ ✅。
> 纪律 ✅：只写「读代码得不到的东西」✅ ✅；**与代码不一致时以代码为准** ✅ ✅。

---

## 一、四条数据线 ✅ ✅
| 线 | 数据载体 | 运行时 | 备注 |
|---|---|---|---|
| **升级三选一** ✅ ✅ | `UpgradeDefinition.cs` ✅ ✅（SO ✅ ✅）+ **升级池** ✅ ✅（`UpgradePoolSampler` ✅ ✅ ✅）✅ ✅ | `UpgradeChoiceManager.cs` ✅ ✅（发牌 ✅ ✅）+ `UpgradeEffectManager.cs` ✅ ✅（应用 ✅ ✅ / 描述 ✅ ✅ ✅）✅ ✅ | 效果最终落到 **`StatModifier`** ✅ ✅（`statId` ✅ ✅ + `type` ✅ ✅ + `value` ✅ ✅✅ ✅）✅ ✅ |
| **主动技能** ✅ ✅ | `Assets/ScriptableObjects/ActiveSkills/` ✅ ✅（各技能资产 ✅ ✅ + `ActiveSkillPoolConfig.asset` ✅ ✅ ✅）✅ ✅ | `ActiveSkillDefinition` ✅ ✅（等级/冷却 ✅ ✅ + `activeEffectType` ✅ ✅✅ ✅）✅ ✅ | 效果类型是**枚举** ✅ ✅（机制 ✅ ✅ ✅）✅ ✅ |
| **道具 / 掉落** ✅ ✅ | `Assets/ScriptableObjects/Items/` ✅ ✅ ✅ + `DropItemPoolConfig.asset` ✅ ✅ ✅ | `ItemInventory.cs` ✅ ✅（持有 ✅ ✅）+ `ItemEffectRunner.cs` ✅ ✅（生效 ✅ ✅）+ `CycloneItemController.cs` ✅ ✅ ✅ | 存档在 `PropSaveSection` ✅ ✅ ✅ |
| **经验 / 等级** ✅ ✅ | `ExpCurveConfig` ✅ ✅（`DefaultExpCurve.asset` ✅ ✅ / `ExpCurve_30min.asset` ✅ ✅ ✅）✅ ✅ | `ExpGemManager.cs` ✅ ✅ + `ExpGem.cs` ✅ ✅ ✅ | ✅ |

**铜钱** ✅ ✅ → `SaveManager.GetCoins/SetCoins` ✅ ✅ —— **关卡奖励 ✅ ✅ / 天赋花费 ✅ ✅ / 局内消费 ✅ ✅ 共用** ✅ ✅✅ ✅。

---

## 二、升级三选一 与 主动技能 的区别 ✅ ✅
- **升级三选一** ✅ ✅ = 局内成长 ✅ ✅（打完一波三选一 ✅ ✅）✅ —— 定义在 `UpgradeDefinition` ✅ ✅ + 池 ✅ ✅ ✅。
- **主动技能** ✅ ✅ = 局外解锁的技能 ✅ ✅（`ActiveSkillDefinition` ✅ ✅ ✅）✅ ✅。
- ⚠️ 两者**都能给属性修正** ✅ ✅ ✅ → 效果都走 `StatModifier` ✅ ✅✅ ✅ → **新加一个效果先看 StatModifier 有没有现成类型** ✅ ✅❗（没有再加 ✅ ✅ —— 别新写一套效果通道 ✅ ✅✅ ✅）✅ ✅。

---

## 三、加一条成长内容要动哪几处 ✅ ✅
**加一个升级项** ✅ ✅：
1. `UpgradeDefinition` ✅ ✅ 资产 ✅ ✅（效果写 `StatModifier` ✅ ✅ ✅）
2. 加进**升级池** ✅ ✅（`UpgradePoolSampler` ✅ ✅ 读的配置 ✅ ✅ ✅）✅ ✅
3. 想要**描述文案** ✅ ✅ → `DefaultUpgradeDescriptionFormatter` ✅ ✅ / `UpgradeEffectManager.Description` ✅ ✅ ✅ ✅
4. 想要**专属应用逻辑** ✅ ✅（不是属性修正 ✅ ✅）✅ → `DefaultUpgradeNumericApplier` ✅ ✅ / `DelegateUpgradeAppliers` ✅ ✅✅ ✅ ✅
5. 校验 ✅ ✅ → 跑一遍 `Tools/战斗数据表 ✅ ✅`（升级 Tab ✅ ✅ ✅）✅ ✅

**加一个主动技能** ✅ ✅：资产 ✅ ✅（`ActiveSkills/` ✅ ✅）+ 池 ✅ ✅（`ActiveSkillPoolConfig` ✅ ✅ ✅）+ 效果类型 ✅ ✅（枚举 ✅ ✅ —— 要改代码 ✅ ✅✅ ✅）✅ ✅

**加一个道具** ✅ ✅：资产 ✅ ✅（`Items/` ✅ ✅）+ 掉落池 ✅ ✅（`DropItemPoolConfig` ✅ ✅ ✅）+ 效果 ✅ ✅（`ItemEffectRunner` ✅ ✅ —— 现成通道优先 ✅ ✅✅ ✅）✅ ✅

---

## 四、坑 ✅ ✅
1. **四条线别搞混** ✅ ✅（升级三选一 ✅ ✅ ≠ 主动技能 ✅ ✅ ≠ 道具 ✅ ✅ ≠ 经验 ✅ ✅✅ ✅）✅ ✅
2. **效果类型是枚举** ✅ ✅（`ActiveSkillEffectType` ✅ ✅ / `StatModifierType` ✅ ✅ ✅）→ 新效果可能要加枚举值 ✅ ✅（**但先找现成的 ✅ ✅** ✅ ✅❗✅ ✅）✅ ✅
3. **铜钱共享** ✅ ✅ → 改任一处收益 ✅ ✅ 会影响**天赋可负担性** ✅ ✅（天赋花费 ✅ ✅ ✅）✅ ✅
4. **升级池是独立配置** ✅ ✅ → 新加了升级项**没进池就永远不出** ✅ ❗✅ ✅
5. **道具要存档** ✅ ✅ → `PropSaveSection` ✅ ✅（不是 `ProgressSaveSection` ✅ ✅✅ ✅）✅ ✅
6. **经验曲线有两个资产** ✅ ✅（默认 ✅ ✅ / 30 分钟版 ✅ ✅ ✅）→ 改哪个要看实际引用 ✅ ✅❗✅ ✅

---

## 五、代码路径（改之前先核对 ✅ ✅）
- `Assets/Scripts/Data/UpgradeDefinition.cs` ✅ ✅、`Assets/Scripts/Data/ActiveSkillDefinition.cs` ✅ ✅、`Assets/Scripts/Data/StatModifier.cs` ✅ ✅（`ActiveBuff` ✅ ✅ ✅）✅ ✅
- `Assets/Scripts/Core/UpgradeChoiceManager.cs` ✅ ✅、`UpgradePoolSampler.cs` ✅ ✅、`UpgradeEffectManager*.cs` ✅ ✅、`DefaultUpgradeNumericApplier.cs` ✅ ✅、`DelegateUpgradeAppliers.cs` ✅ ✅、`DefaultUpgradeDescriptionFormatter.cs` ✅ ✅ ✅
- `Assets/Scripts/Core/ItemInventory.cs` ✅ ✅、`ItemEffectRunner.cs` ✅ ✅、`CycloneItemController.cs` ✅ ✅、`ExpGemManager.cs` ✅ ✅、`ExpGem.cs` ✅ ✅、`ComboManager.cs` ✅ ✅
- `Assets/Scripts/Data/Save/PropSaveSection.cs` ✅ ✅、`Assets/Scripts/Services/SaveManager.cs` ✅ ✅
- 资产 ✅ ✅：`Assets/ScriptableObjects/ActiveSkills/` ✅ ✅、`Items/` ✅ ✅、`ExpCurve/` ✅ ✅ ✅
- 相关 ✅ ✅：`memory/unity-project-understanding/buff-and-status.md` ✅ ✅（状态 ✅ ✅✅ ✅）✅ ✅、`attacks-moves-and-heroes.md` ✅ ✅✅ ✅
