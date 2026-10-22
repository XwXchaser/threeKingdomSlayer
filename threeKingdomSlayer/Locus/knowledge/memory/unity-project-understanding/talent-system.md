---
id: kd_fc798378-53ea-450b-93b2-f933d61fd734
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
summary: 天赋树/天赋页的使用文档（2026-10-24）——天赋数据在哪个资产、前置关系与解锁载荷怎么写、面板连线与分叉、状态判定与解锁流程、存档按 nodeId 存的兼容边界、加一条天赋要连带检查的四处与坑；含对应代码路径便于按需核对。
injectMode: inherit
aiEditMode: inherit
---

# 天赋树 / 天赋页 · 使用文档

> **一句话** ✅ ✅：天赋是**内联数据** ✅ ✅（在天赋树资产里 ✅ ✅，不是一节点一资产 ✅ ✅），
> **前置关系显式** ✅ ✅（可分叉 ✅ ✅）、**解锁载荷可组合** ✅ ✅（招式 ✅ ✅ + 机械开关 ✅ ✅）✅ ✅。
> 纪律 ✅：本文只写「读代码得不到的东西」✅ ✅；**与代码不一致时以代码为准** ✅ ✅。

---

## 一、数据在哪 ✅ ✅
- **天赋树资产** ✅ ✅：`Assets/ScriptableObjects/Talents/TalentTree_Main.asset` ✅ ✅（`TalentTreeConfig` ✅ ✅，SO ✅ ✅）✅
  - `treeId` ✅ ✅（**写进存档做区分** ✅ ✅ —— 换一棵树要换 id ✅ ❗✅ ✅）
  - `nodes` ✅ ✅：**内联的节点列表** ✅ ✅（`TalentNodeConfig` ✅ ✅）✅
- **节点字段** ✅ ✅：`nodeId` ✅ ✅（**存档键 ✅ ❗ 不能改 ✅ ✅）/ `displayName` ✅ ✅ / `description` ✅ ✅ / `coinCost` ✅ ✅ / **`prerequisiteIds`** ✅ ✅ / **`moveConfig`** ✅ ✅ / **`flagId`** ✅ ✅ / `treePosition` ✅ ✅（面板位置 ✅ ✅，1080×1920 口径 ✅ ✅）/ `icon` ✅ ✅ ✅
- **英雄绑定** ✅ ✅：`HeroConfig.talentTree` ✅ ✅（哪个英雄用哪棵树 ✅ ✅）✅
- **面板** ✅ ✅：`TalentTreePanel` ✅ ✅（运行时**按数据生成** UI ✅ ✅ —— 没有美术也能跑 ✅ ✅ ✅）✅ ✅

---

## 二、前置关系（可分叉 ✅ ✅）
- `prerequisiteIds` ✅ ✅ = 前置节点的 **nodeId 列表** ✅ ✅（可多个 ✅ ✅ → **分叉/汇合都能做** ✅ ✅✅ ✅）✅ ✅。
- **留空 = 沿用旧写法** ✅ ✅（前置 = 列表里的**上一项** ✅ ✅）→ **老资产零改动可用** ✅ ✅✅ ✅。
- 判定：`TalentTreeConfig.GetPrerequisites(node)` ✅ ✅（显式优先 ✅ ✅，否则回退链式 ✅ ✅）✅ ✅
  → 消费方 ✅ ✅：`TalentTreeState.IsAvailable` ✅ ✅ / `TryUnlock` ✅ ✅ ✅ —— 语义是「**所有前置**都解锁 ✅ ✅」✅ ✅。
- **面板连线按边画** ✅ ✅（每个节点 → 它的每个前置 ✅ ✅）✅ ✅ → 树的形状由 `treePosition` ✅ ✅ 决定 ✅ ✅ ✅。

---

## 三、解锁载荷（组合 ✅ ✅，没有枚举了 ✅ ✅）
节点可以**同时**给多个载荷 ✅ ✅：
- `moveConfig` ✅ ✅（`AttackSkillConfig` ✅ ✅）→ **解锁招式** ✅ ✅（之后该招式才可出招 ✅ ✅）
- `flagId` ✅ ✅（字符串 ✅ ✅）→ **点亮机械开关** ✅ ✅（后续系统用 `TalentTreeState.IsFlagUnlocked` ✅ ✅ 读 ✅ ✅）
- **两个都空** ✅ ✅ = 无效载荷 ✅ ✅（自检会报 ✅ ✅）✅ ✅
- ⚠️ **没有「解锁类型」枚举了** ✅ ✅（`TalentUnlockKind` ✅ ✅ 已退役 ✅ ✅）→ **加第三种载荷 = 加一个可选字段** ✅ ✅（老资产继续有效 ✅ ✅✅ ✅ —— 枚举加值会逼所有资产重映射 ✅ ❗❗✅ ✅）✅ ✅。

---

## 四、状态与解锁流程 ✅ ✅
- `TalentTreeState` ✅ ✅（静态 ✅ ✅）：
  - `IsUnlocked` ✅ ✅（按 nodeId ✅ ✅ 查存档 ✅ ✅）
  - `IsAvailable` ✅ ✅（可点 = 没解锁 ✅ 且**所有前置**都解锁 ✅ ✅）
  - `TryUnlock` ✅ ✅ → **扣铜钱** ✅ ✅ → 写存档 ✅ ✅ → 广播 `OnChanged` ✅ ✅
  - `IsFlagUnlocked(tree, flagId)` ✅ ✅ / `IsMoveUnlocked(tree, move)` ✅ ✅ / `IsMoveGatedAndLocked(tree, move)` ✅ ✅
    （后者是「配了解锁节点但还没解锁」✅ ✅ —— **没配节点的招式不受天赋管** ✅ ✅，避免配置缺失把招式全锁死 ✅ ✅ ✅）✅ ✅
- 存档 ✅ ✅：`TalentSaveSection` ✅ ✅ → `SaveData.unlockedTalents` ✅ ✅（**nodeId 列表** ✅ ✅ ✅）
  → **改前置结构不会毁老档** ✅ ✅；**改 nodeId 会毁** ✅ ❗（该节点的解锁记录作废 ✅ ✅）✅ ✅。

---

## 五、加一条天赋要**连带检查**的四处 ✅ ✅
1. `nodes` ✅ ✅ 里加一条 ✅ ✅（`nodeId` 唯一 ✅ ✅）；
2. **前置** ✅ ✅（`prerequisiteIds` ✅ ✅ —— 留空 = 上一项 ✅ ✅）；
3. **解锁载荷** ✅ ✅（`moveConfig` ✅ ✅ / `flagId` ✅ ✅ —— 至少一个 ✅ ✅）；
4. `treePosition` ✅ ✅（面板位置 ✅ ✅）+ `icon` ✅ ✅；
5. **跑校验** ✅ ✅：`Tools/数据校验/天赋树 ✅ ✅` ✅ ✅（查前置 id 不存在 ✅ ✅ / 自指 ✅ ✅ / **成环** ✅ ✅ / nodeId 重复 ✅ ✅ ✅）✅ ✅✅ ✅。

---

## 六、坑 ✅ ✅
- **nodeId = 存档键** ✅ ❗ → 改名 = 老档丢失该节点 ✅ ❗✅ ✅。
- **面板连线** ✅ ✅ 改过（从"上一项"改成按边 ✅ ✅）→ **别改回去** ✅ ❗（那样就做不了分叉 ✅ ✅✅ ✅）✅ ✅。
- **链式兜底还在** ✅ ✅（`prerequisiteIds` 留空 = 上一项 ✅ ✅）→ 新写的内容**请显式填** ✅ ✅，别依赖兜底 ✅ ✅。
- **天赋页与战斗的耦合点** ✅ ✅：解锁的招式 ✅ ✅（`moveConfig` ✅ ✅）会被 `PlayerMoveStateMachine` ✅ ✅ 读 ✅ ✅（`IsMoveGatedAndLocked` ✅ ✅ ✅）✅ ✅。
- **铜钱是共享资源** ✅ ✅：天赋花费 ✅ ✅ 与关卡结算奖励 ✅ ✅ 都动它 ✅ ✅ → 改经济时要一起看 ✅ ✅。

---

## 七、对应代码路径（改之前先核对 ✅ ✅）
- `Assets/Scripts/Data/TalentTreeConfig.cs` ✅ ✅（`TalentTreeConfig` ✅ ✅ + `TalentNodeConfig` ✅ ✅ + `Validate` ✅ ✅ ✅）✅ ✅
- `Assets/Scripts/Core/TalentTreeState.cs` ✅ ✅
- `Assets/Scripts/UI/TalentTreePanel.cs` ✅ ✅
- `Assets/Scripts/Data/Save/TalentSaveSection.cs` ✅ ✅
- `Assets/Scripts/Data/HeroConfig.cs` ✅ ✅（`talentTree` ✅ ✅ ✅）✅ ✅
- `Assets/Scripts/MoveSystem/PlayerMoveStateMachine.cs` ✅ ✅（招式被天赋锁 ✅ ✅ ✅）✅ ✅
- `Assets/Scripts/Editor/TalentTreeValidator.cs` ✅ ✅
- 相关交接 ✅ ✅：`plan/data-modularization-handoff-20261024.md` ✅ ✅
