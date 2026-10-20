---
id: kd_2c714602-6061-4401-97c8-b57c3ab65250
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: PerColumn 补齐规则退役方案（2026-10-20）：实测现役内容 100% 用 PerRow，11 个 PerColumn 资产全是 AI 误部署/旧命名/未被引用；给出三步走（默认值翻转+导入期报错 → 清理资产 → 终态删枚举与字段，让 AI 无从选错），并说明与「补齐豁免扩展点」「ColumnManager 拆分」的先后顺序。
---

---
summary: PerColumn 退役方案（2026-10-20）：实测现役内容 100% 用 PerRow，11 个 PerColumn 资产全是 AI 误部署/旧命名/未被引用；给出三步走（默认值翻转+导入期报错 → 清理资产 → 终态删枚举与字段，让 AI 无从选错），并说明与「补齐豁免扩展点」「ColumnManager 拆分」的先后顺序。
---

# PerColumn 补齐规则退役方案

> 背景：用户指出 `PerColumn` 是**被错误部署**的默认值——枚举第 0 项是 PerColumn，
> 新建关卡与 AI 生成的资产都落在它上面；而**现役内容实际用的是 PerRow**
> （带 0 排占位符与 999 节奏门的那批）。目标：**让 AI 部署关卡时不再可能选错**。
> 配套阅读：`design/formation-fillup-mechanism-20261020.md`（机制地图）、
> `design/percolumn-fillup-rules.md`（规则权威版）、`design/immutable-constraints.md`。

---

## 0. 结论

| 项 | 结论 |
|---|---|
| 现役内容用哪条规则 | **100% PerRow**（教学 `Tutorial_N1/E0_Battle`、正式 `Formal_N1/E0_Battle` 全部 PerRow） |
| 11 个 PerColumn 资产 | 全是 **AI 误部署 / 旧命名 / 无人引用**——不是现役内容 |
| 能不能直接删 | **能**，但建议分三步：先让默认值与导入期校验拦住新错误 → 再清理存量资产 → 终态**删掉枚举与字段**，让这个选项根本不存在 |
| 与后续工作的顺序 | **先退役 PerColumn**（它会先让 `ColumnManager` 瘦一圈）→ 再做「补齐豁免扩展点」→ 最后做 `ColumnManager` 拆分 |

---

## 1. 现状证据

### 1.1 现役路线节点引用的战斗配置（Unity `AssetDatabase` 反查，非 grep）

| 路线节点 | 战斗配置 | 规则 |
|---|---|---|
| `Tutorial_Node_N1` | `Tutorial_N1_Battle` | **PerRow** |
| `Tutorial_Node_E0` | `Tutorial_E0_Battle` | **PerRow** |
| `Node_N1`（正式） | `Formal_N1_Battle` | **PerRow** |
| `Node_E0`（正式） | `Formal_E0_Battle` | **PerRow** |
| `Node_J1` | 无 battleEntries | — |

### 1.2 `Assets/Resources/StageConfigs/` 下的 6 个关卡

| 资产 | 规则 | 备注 |
|---|---|---|
| `QTE_V2_TestStage` / `Stage_BossRushTest` / `Stage_Trial` / `Stage_WaveMarchBugTest` / `testStage` | PerRow | — |
| `Stage_1` | **PerColumn** | **无任何引用**（Resources 里会进包，但没人用；代码里也没有按名字加载它） |

### 1.3 全部 11 个 PerColumn 资产

| 分类 | 资产 |
|---|---|
| 旧命名（已被 `Tutorial_*` / `Formal_*` 取代） | `RouteData/Stage01/N1_Battle`、`N0_Battle`、`J0_Battle`、`N1_ArtPreview_Battle`、`RouteData/ChangbanpoScroll/Battles/E0_Battle_Preview` |
| 试玩/实验数据 | `Experiments/CurvedScroll/TrialBattle` + `RouteTrialData/` 下 4 个 |
| Resources 里的死资产 | `Resources/StageConfigs/Stage_1` |

> ⚠️ 查引用**不能**用 grep 比对 guid：本项目 `.meta` 存 base64、资产引用存 hex（见主计划文档 §7.4）。用
> `AssetDatabase` / `AssetDatabase.GetDependencies` 反查。

### 1.4 代码里的默认与分支点

| 位置 | 现状 |
|---|---|
| `Assets/Scripts/Data/StageConfig.cs` | `public FillUpRule fillUpRule = FillUpRule.PerColumn;` ← **字段默认值＝枚举第 0 项** |
| `Assets/Scripts/Data/StageConfig.cs` | `enum FillUpRule { PerColumn, PerRow }` ← PerColumn 是第 0 项 |
| `Assets/Scripts/Data/StageConfig.cs` | `[CreateAssetMenu(menuName = "一夫当关/关卡配置")]` ← 新建资产走默认值 |
| `Assets/Scripts/Managers/StageFormation.cs:78` | `config != null ? config.fillUpRule : FillUpRule.PerColumn` ← 兜底也是 PerColumn |
| `Assets/Scripts/Wave/WaveSpawner.cs:176` | `ResolvedStageConfig?.fillUpRule ?? FillUpRule.PerColumn` |
| `Assets/Scripts/Wave/WaveSpawner.cs:256` | `SpawnRow(..., FillUpRule fillRule = FillUpRule.PerColumn, ...)` ← 参数默认值 |
| `Assets/Scripts/Enemy/Enemy.cs:924` | `... == FillUpRule.PerRow && ...`（PerRow 分支判断） |

**三个来源叠加**＝新建即 PerColumn、null 兜底 PerColumn、AI 复制旧资产 YAML 写死 `fillUpRule: 0`。

---

## 2. 三步方案

### 步骤 1：默认值翻转 + 显式废弃标注 + 导入期报错（零风险）✅ 已完成（`5769fb0`）

| 改动 | 位置 | 说明 |
|---|---|---|
| 字段默认值改 PerRow | `StageConfig.fillUpRule = FillUpRule.PerRow` | **只影响新建资产**；已有资产的序列化值不变 |
| 枚举项标注废弃 | `FillUpRule.PerColumn` 加 `[Obsolete(...)]` + 注释 | 写代码时会有编译警告，是显式信号 |
| 兜底默认改 PerRow | `StageFormation.GetFillUpRule`、`WaveSpawner` 两处 `??`、`SpawnRow` 参数默认值 | 配置缺失时按**现行规则**走 |
| **导入期报错** | `StageConfig.OnValidate()`：`fillUpRule == PerColumn` → `LogError`（带资产名 + 现行规则 + 修复指引） | AI 写完资产导入时立刻红字 |

**实测**：新建 StageConfig 默认 = PerRow；现有 PerColumn 资产 11 个（PerRow 12 个），强制导入后
每个都产生一条 `[StageConfig]` 红字；现役流程（教学关·初见 → 开打 → 出怪 → 攻击掉血）不受影响；
Play Mode 运行期 Console error = 0。

> 这 11 条红字是**预期的守门信号**，步骤 2 清理资产后消失。

### 步骤 2：清理 11 个存量 PerColumn 资产

| 分组 | 建议动作 |
|---|---|
| 旧命名 5 个（`Stage01/N1_Battle` 等） | 确认无引用后**删除**（已被 `Tutorial_*` / `Formal_*` 取代） |
| 试玩/实验 5 个 | 改为 PerRow（保留实验用途） |
| `Resources/StageConfigs/Stage_1` | 无引用但在 Resources（**进包**）→ 建议删除（顺带省包体）或改 PerRow |

**风险**：删除类操作，需用 `AssetDatabase` 反查引用后再删（别用 grep）。
**验收**：全项目 `fillUpRule` 分布只剩 PerRow；`OnValidate` 不再报红。

### 步骤 3（终态，根除）：删掉枚举与字段

| 改动 | 说明 |
|---|---|
| 删 `enum FillUpRule` 与 `StageConfig.fillUpRule` 字段 | **运行时只剩一种补齐语义** → AI 部署关卡时**没有这个选项可填**，从根上杜绝误配 |
| 简化 `WaveSpawner` | 两分支合一；`SpawnRow` 去掉 `fillRule` 参数 |
| 简化 `Enemy.cs:924` | 去掉 `== FillUpRule.PerRow` 判断 |
| 简化 `StageFormation.GetFillUpRule` | 或整体删除（若无外部消费者） |
| 删 `ColumnManager` 的 legacy 入口 | `RowBasedFillUp`（:1525 已 inert）、`CompactAllColumns`（:2100 已 inert）、`StartWaveMarch` 的 legacy 扫描分支 → **同时让 ColumnManager 变小，为后续拆分减负** |

**风险**：中。前提是步骤 2 已完成；且现役关卡要回归（整排清空推进、999 门开启、SpawnEntry 远处入场、地刺同步死亡——这些都在「待验收」清单里）。
**验收**：① 项目里搜不到 `FillUpRule`；② 教学/正式关卡流程与现在一致；③ AI 生成关卡资产时无需（也无法）填 `fillUpRule`。

---

## 3. 与后续两项工作的顺序

| 顺序 | 工作 | 为什么排这个位置 |
|---|---|---|
| ① **退役 PerColumn**（本方案） | 三步走 | 会先删掉 `ColumnManager` 的 legacy 分支 → 类变小 → 拆分更干净；且把「只有一条规则」这个前提固定下来 |
| ② **补齐豁免扩展点** | `Enemy.ignoreFillUpOrders` + 订单**执行**层跳过 | 必须在单一规则（PerRow）下设计；「豁免者算不算占位」的语义也要在简化后的逻辑队列上定 |
| ③ **ColumnManager 拆分** | 5 刀（逻辑队列 / 只读查询 / 入场 / 补齐调度 / 位移回位） | 队列与调度在 ① 之后已经只剩一套语义，拆分时不用同时处理两套路径 |

---

## 4. 实施清单（文件级）

**步骤 1**
- `Assets/Scripts/Data/StageConfig.cs`：枚举 `[Obsolete]` 标注；`fillUpRule` 默认值 → `PerRow`；新增 `OnValidate` 报错
- `Assets/Scripts/Managers/StageFormation.cs`：`GetFillUpRule` 兜底 → `PerRow`
- `Assets/Scripts/Wave/WaveSpawner.cs`：两处 `?? FillUpRule.PerColumn` → `PerRow`；`SpawnRow` 参数默认值 → `PerRow`

**步骤 2**
- 11 个资产（删除 6 / 改 5），逐个用 `AssetDatabase` 反查引用

**步骤 3**
- `Assets/Scripts/Data/StageConfig.cs`：删枚举 + 字段
- `Assets/Scripts/Wave/WaveSpawner.cs`、`Assets/Scripts/Enemy/Enemy.cs`、`Assets/Scripts/Managers/StageFormation.cs`：分支简化
- `Assets/Scripts/Core/ColumnManager.cs`：删 legacy 入口

---

## 5. 验收清单

| 项 | 期望 |
|---|---|
| 新建 StageConfig | 默认 **PerRow** |
| 任何资产被设为 PerColumn | 导入时 **红字报错** |
| 步骤 2 后 | 全项目 `fillUpRule` 只剩 PerRow |
| 步骤 3 后 | 项目里搜不到 `FillUpRule`；AI 生成的关卡资产无需填任何补齐规则 |
| 教学关 / 正式关卡 | 整排清空推进、999 节奏门、SpawnEntry 远处入场、地刺同步死亡 **与现在一致** |
| 遗留实验场景 | `Bisect.unity` 等缺失脚本仍为 0 |
