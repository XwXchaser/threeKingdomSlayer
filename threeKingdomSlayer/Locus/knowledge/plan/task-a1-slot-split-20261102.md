---
id: kd_b743707b-5999-40c1-8491-05750a0c2c44
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 任务卡 A1 · 槽位 3+3 拆分

> **状态**：✅ **已完成 2026-11-03** · **归属**：局内机制批次 A · **复杂度**：中
> 纪律见 `plan/growth-stage-move-plan-20261102.md` §0.6；执行顺序见同文档 §0.5。
> 必读：`design/in-run-growth-and-unlocks.md` §5。
>
> **落地摘要**：基础 **3 主动 + 3 被动 + 4 道具**。三个槽位拆成 `HeroConfig` 三个独立字段；
> `Hero_Zhangfei.asset` 已显式写入两个新字段（磁盘回读确认）；两处读取点均带 `Mathf.Max(1, v)` 兜底。
> **未做**：天赋渐进解锁槽位的读开关（B 批）、被动槽口径与 UI 不一致（A2 / Q6）、`itemSlotCount` 语义（不变）。


---

## 1. 目标

把「主动技能槽 / 被动技能槽」从**当前共用一个字段**的耦合状态拆开，基础值 **3 主动 + 3 被动**，
并为「天赋渐进解锁更多槽位」留出读开关的落点（开关本身属 B 批，**本任务不做**）。

---

## 2. 现状（实测，2026-11-02）

| 项 | 位置 | 现状 |
|---|---|---|
| 主动槽上限 | `Assets/Scripts/Core/ActiveSkillInventory.cs:56` | `Capacity => heroConfig.itemSlotCount`（=4） |
| 道具槽上限 | `Assets/Scripts/Core/ItemInventory.cs:22` | `Capacity => heroConfig.itemSlotCount`（**同一字段**） |
| 被动槽上限 | `Assets/Scripts/Core/UpgradeChoiceManager.cs:98` | 硬编码 `PassiveUpgradeCount >= 6` |
| 被动计数口径 | `Assets/Scripts/Core/UpgradeEffectManager.cs:166` | `PassiveUpgradeCount => _appliedUpgrades.Count` → **含 Numeric 类**（专注 / 智慧 / 疾风 / 铁壁 / 地刺 / 拔苗助长…） |
| 被动排 UI 口径 | `Assets/Scripts/UI/PassiveSkillRow.cs` | 只显示 `AttackPassive` + `TimedPassive` → **不含 Numeric** |
| 武将资产 | `Assets/ScriptableObjects/Warrior/Hero_Zhangfei.asset` | `itemSlotCount: 4` |

> ⚠️ **被动槽口径与 UI 不一致**是 A2 的问题（待 Q6 拍板），**A1 只做拆分，不改口径**。

---

## 3. 目标数据契约

| 字段 | 归属 | 值 | 谁读 |
|---|---|---|---|
| `activeSkillSlotCount` | `HeroConfig` | **3** | `ActiveSkillInventory.Capacity` |
| `passiveSlotCount` | `HeroConfig` | **3** | `UpgradeChoiceManager` 的被动上限判定 + 相关 UI |
| `itemSlotCount` | `HeroConfig`（**保留原义**） | **4**（不变） | `ItemInventory.Capacity` |

---

## 4. ⚠️ 序列化陷阱（动手前必读）

Unity **新增 int 字段在老资产里是 `0`，不是字段声明时的默认值**。

→ 只加字段不改资产 = 主动槽变 0 → **游戏直接坏**（一个技能都放不下）。

**必须三件事一起做**：

1. `HeroConfig` 加字段时给 `= 3` 的初始化值（只对**新建**资产生效）
2. **同时改 `Assets/ScriptableObjects/Warrior/Hero_Zhangfei.asset`**，把两个字段填成 3
3. **读取处加兜底**：`Mathf.Max(1, v)` —— 防老存档 / 漏改资产的资产再次归零

---

## 5. 涉及文件（开工前先 `grep -rn "itemSlotCount" Assets/Scripts` 确认全部读取点）

| 文件 | 改什么 |
|---|---|
| `Assets/Scripts/Data/HeroConfig.cs` | 加 `activeSkillSlotCount` / `passiveSlotCount` 两个字段 + 注释 |
| `Assets/ScriptableObjects/Warrior/Hero_Zhangfei.asset` | 两个新字段填 3 |
| `Assets/Scripts/Core/ActiveSkillInventory.cs:56` | `Capacity` 改读 `activeSkillSlotCount` |
| `Assets/Scripts/Core/UpgradeChoiceManager.cs:98` | 被动上限改读 `passiveSlotCount` |
| `Assets/Scripts/Core/ItemInventory.cs:22` | **不动**（仍读 `itemSlotCount`） |
| `Assets/Scripts/UI/BuffDisplayPanel.cs` / `PassiveSkillRow.cs` | **开工前 grep**：若有读 `itemSlotCount` 当主动槽用的，一并改 |

---

## 6. 明确不做（越界清单）

- ❌ 不改天赋树数据模型（`groupId` / 载荷方案 = **B 批**）
- ❌ 不做「天赋渐进解锁槽位」的 flag 消费点（**B 批之后**）
- ❌ 不改被动槽的口径（Q6 待拍板 = **A2**）
- ❌ 不改 `itemSlotCount` 的含义与数值
- ❌ 不动对方已实现的文件（`TalentTreePanel.cs` / `TalentTree.scene` / 4 个树资产 / `TalentCatalog` / `TalentNodeInfo` / `TalentPaginator` / `ProceduralUiSprites`）
- ❌ 不新建校验器脚本 / 测试工程 / 引入测试依赖

---

## 7. 自检（**只用工程已有工具**）

| # | 检查 | 手段 |
|---|---|---|
| 1 | 编译无错 | Unity Console **error 级 = 0** |
| 2 | 数据合法 | 菜单「Tools/数据校验/天赋树」**零问题** |
| 3 | 主动槽 = 3 | 实机看技能排 / 主动技能栏 |
| 4 | 被动槽 = 3 | 实机看被动排（**注意**：只到 3 个，Numeric 也占槽） |
| 5 | 道具槽仍是 4 | 实机看道具栏 |
| 6 | **旧档兼容** | 用老存档打开，槽位**不是 0**，能正常开局 |
| 7 | **存档往返** | 退出重进 → 槽位 / 已有技能仍在 |
| 8 | **存档污染** | `SaveManager` 直写 PlayerPrefs → **验前备份、验完还原** |
| 9 | 资产编辑纪律 | 改 prefab / asset 前**备份**，存盘后**回读磁盘** |
| 10 | 收尾总检 | 菜单「Tools/项目体检/运行全部检查」 |

---

## 8. 完成后交接格式

回一条，四个部分：

```
改了什么：  （文件 + 每处改动的目的）
怎么验的：  （上表哪几项 + 实机/菜单/脚本的实际结果）
验出什么问题：（包括「没验到」的部分，别装作都过了）
还差什么：  （本任务没做但相邻的部分，例如 A2 的被动排口径）
```

> 完成后同步更新 `design/in-run-growth-and-unlocks.md` §5（把「目标」改成「已实现」）。
