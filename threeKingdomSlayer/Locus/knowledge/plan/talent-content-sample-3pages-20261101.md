---
id: kd_8525b1b4-87a5-4399-8540-93e39f00b9c7
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: 天赋三分类的解锁路线示例草案（只写文档、未写入资产）：页 2 招式（指向突刺/招架 → 枪突收尾的汇合示例）、页 1 基础数值（体魄链＋行囊，长边导致整页不可切）、页 3 三选一（14 个真实升级条目，三块独立子图演示自动切页回退到合法缝隙），含自动切页演算（rowsPerPage=8 → 第 1 页 6 行 / 第 2 页 3 行），并列出暴露的 3 个待办（talent_unlock_c2 缺载荷、页 3 载荷类型、页 1 读值消费点）。
---

# 天赋三类解锁路线 · 示例草案（供评审）

> ⚠️ **状态更新（2026-11-02）。**
> 本文的**方法**仍然有效：`nodeId` 跟内容走、行列 `col`/`row`、前置 AND、汇合示例（页 2 的「C4 需 C2 与招架都解锁」）、长边导致不可切页的例子、页 3 的自动切页演算。
> 但**具体节点内容会被替换**：最终信息架构是 **3 页 × 8 条分支**（能力页：战斗向 / 生存向 / 经济向；招式页：点线 / 划线；三选一页：蓄力流 / 位移流 / 火系），见 `plan/talent-scheme-alignment-20261102.md` §3。
>
> 另：本文已发现的两个问题**对方已修** —— `talent_unlock_c2` 的 `moveConfig` 已指向 `Zhangfei_StabR2`（现节点 id 为 `talent_move_stabc2`）。

> 状态：**草案**，只写文档，未写入任何资产。节点 id、成本、行列布局都是示例，可按你的意思改。
> 素材全部取自项目现有资产（招式资产、升级条目），不引入新玩法概念。
> 配套方案见 `plan/talent-3page-pager-plan-20261101.md`。

---

## 0. 约定

- 行列：`col` 0/1/2 = 左/中/右；`row` 0 = 最上一行（参考图里"根部在下、越往上越高级"，所以**前置在下、依赖在上**）
- 前置：`prerequisiteIds` 列表 = **AND**（列表里全部解锁才可点）。填 2 个就是"两边分支都点上才能开"
- 成本：`coinCost`（三类共用铜钱）
- 命名：`talent_<类>_<语义>`，跟内容走，不跟位置走

---

## 1. 页 2 · 招式解锁（最简单、现成资产直接能用）

| nodeId | 名称 | 载荷（真实资产） | 前置 | 铜钱 | 行/列 |
|---|---|---|---|---|---|
| `talent_move_c2` | 指向突刺 | `Zhangfei_StabR2` | — | 100 | 1,0 |
| `talent_move_parry` | 招架格挡 | `Zhangfei_Parry` | — | 100 | 1,2 |
| `talent_move_c4` | 枪突收尾 | `Zhangfei_LaunchFinisher` | `talent_move_c2` + `talent_move_parry` | 200 | 0,1 |

```
      [枪突收尾]              ← row0（两边都要点）
      ↙        ↘
 [指向突刺]   [招架格挡]        ← row1（两个独立根）
```

- 图里就有一次**汇合**：C4 需要 C2 与招架都解锁
- 只有 2 行 → 1 页装得下，不会触发分页
- ⚠ 现状缺陷：资产里 `talent_unlock_c2` 的 `moveConfig` 是空的（`HasValidUnlockPayload == false`，且 hidden）→ 这条要先修，否则它既买不到也不生效

---

## 2. 页 1 · 基础数值（需要新增数值载荷，B5 才落地）

| nodeId | 名称 | statId / 增量 | 前置 | 铜钱 | 行/列 |
|---|---|---|---|---|---|
| `talent_stat_hp1` | 体魄 I | `maxHealth` +50 | — | 100 | 2,0 |
| `talent_stat_hp2` | 体魄 II | `maxHealth` +50 | `talent_stat_hp1` | 150 | 1,0 |
| `talent_stat_revive` | 不屈之志 | `reviveCount` +1 | `talent_stat_hp2` | 250 | 0,0 |
| `talent_stat_slot` | 行囊 | `itemSlotCount` +1 | — | 200 | 2,2 |
| `talent_stat_hp3` | 钢筋铁骨 | `maxHealth` +100 | `talent_stat_revive` + `talent_stat_slot` | 300 | 0,1 |

```
   [不屈之志]      [钢筋铁骨]      ← row0
        │           ↗      ↑
   [体魄 II]              │        ← row1
        │                 │
   [体魄 I]          [行囊]        ← row2
```

- 这里有个长边（`钢筋铁骨` row0 ← `行囊` row2），它跨了 row1|row2 这条分界线 → **整页不能被切成两页**（正是"页必须前置闭合"的例子）
- 仍只占 3 行 → 1 页
- `maxHealth / reviveCount / itemSlotCount` 都是 `HeroConfig` 现有字段，但目前没有任何代码读天赋去改它们 → B5 要加一个读值消费点

---

## 3. 页 3 · 三选一解锁（14 个节点，用来演示自动切页）

节点名直接用现有升级条目的名字（33 个里先取 14 个作示例）。

| nodeId | 名称 | 解锁的升级（`upgradeId`） | 前置 | 铜钱 | 行/列 |
|---|---|---|---|---|---|
| `talent_pool_cyclone` | 旋风 | `cyclone` | 聚拢波 + 回旋波 | 400 | 0,1 |
| `talent_pool_convergence` | 聚拢波 | `convergence_wave` | 击退波 | 250 | 1,0 |
| `talent_pool_return` | 回旋波 | `return_wave` | 箭矢齐射 | 250 | 1,2 |
| `talent_pool_push` | 击退波 | `push_wave` | — | 200 | 2,0 |
| `talent_pool_arrowvolley` | 箭矢齐射 | `arrow_volley` | — | 200 | 2,2 |
| `talent_pool_chainbounce` | 连锁弹射 | `chain_bounce` | 神力 + 疾风 | 400 | 3,1 |
| `talent_pool_damage` | 神力 | `damage_plus` | 连刺 | 200 | 4,0 |
| `talent_pool_attackspeed` | 疾风 | `attack_speed` | 专注 | 200 | 4,2 |
| `talent_pool_stab` | 连刺 | `on_attack_stab` | — | 150 | 5,0 |
| `talent_pool_cooldown` | 专注 | `cooldown_reduction` | — | 150 | 5,2 |
| `talent_pool_phantom` | 虚幻武器 | `phantom_weapon` | 铁壁 + 反伤 | 350 | 6,1 |
| `talent_pool_charge` | 铁壁 | `charge_damage_reduction` | 海浪 | 200 | 7,0 |
| `talent_pool_reflect` | 反伤 | `charge_reflect_shield` | — | 200 | 7,2 |
| `talent_pool_wave` | 海浪 | `wave` | — | 150 | 8,0 |

```
  块A row0        [旋风]
              ↙          ↘
      row1  [聚拢波]      [回旋波]
              ↓              ↓
      row2  [击退波]      [箭矢齐射]
      ─────────── 页边界（合法缝隙，无连线跨越）───────────
  块B row3        [连锁弹射]
              ↙          ↘
      row4  [神力]        [疾风]
              ↓              ↓
      row5  [连刺]        [专注]
      ─────────── 页边界（合法缝隙）───────────
  块C row6        [虚幻武器]
              ↙          ↘
      row7  [铁壁]        [反伤]
              ↓
      row8  [海浪]
```

### 自动切页演算（`rowsPerPage = 8`）

1. 贪心装到第 8 行（rows 0–7）→ 会切在 row7|row8 之间
2. 但 `铁壁(row7) ← 海浪(row8)` 这条边跨界 → **非法断点**
3. 回退到最近的合法断点：row5|row6（块 B 与块 C 之间）
4. 结果：
   - **第 1 页 = rows 0–5**（10 个节点，只用了 6 行 → 底部留 2 行空白）
   - **第 2 页 = rows 6–8**（4 个节点）

这正是"数量不是驱动、缝隙才是"的实例：第 1 页明明还有 2 行空间，却不能把第 3 块的顶部塞进去——塞进去就断边了。

---

## 4. 三页合起来的整体形态

| 类 | 节点数 | 行数 | 页数 | 图示特征 |
|---|---|---|---|---|
| 基础数值 | 5 | 3 | 1 | 一条左列主链 + 右侧独立支，顶端汇合 |
| 招式解锁 | 3 | 2 | 1 | 汇合式（C4 要 C2 + 招架） |
| 三选一解锁 | 14 | 9 | 2 | 三块独立子图，两块缝里切页 |

合计 22 个节点、5 页内容（3 类 × 平均 1.7 页）。

---

## 5. 这份草案暴露的 3 个待办

1. **`talent_unlock_c2` 缺载荷**（`moveConfig` 空 + hidden）：页 2 落地前必须先修，否则"指向突刺"买不到
2. **页 3 节点的载荷类型**：`TalentTreeState.TryUnlock()` 要求 `HasValidUnlockPayload`（`moveConfig` 或 `flagId` 非空）。三选一节点是"给升级条目开门"，两种做法：
   - 复用 flagId：节点填 `flagId = pool_<upgradeId>`，同时把 `UpgradeDefinition.requiredTalentIds` 填成该 nodeId（33 个升级目前 0 个填了）
   - 或新增载荷类型（更干净，但要动 `TryUnlock` 的校验）
3. **页 1 的读值消费点**：`HeroConfig.maxHealth / reviveCount / itemSlotCount` 现在没人读天赋，B5 才做

---

## 6. 想改的话怎么改（不影响存档）

- 换位置（`col/row`）、调成本、增删前置 → **存档完全不受影响**（存档只存 nodeId）
- 换内容（比如把"旋风"那格换成"海浪"）→ **必须换新 nodeId**，否则老档会"匹配上"而白送新内容
- 节点总数变化导致页数变化 → 运行期重算 + 页码 clamp，同样不进存档

---

## 7. 实际配置结果（2026-11-01 已写入资产，与上文草案有差异）

用户定：招式只有 `slash / stabC2 / stabC4` 可配，`stabC2` 是第一个；铜钱全设 **1**。

| 资产 | treeId | 节点（费用均为 1） |
|---|---|---|
| `Assets/ScriptableObjects/Talents/TalentTree_Stats.asset` | `stats` | 体魄 I（根）→ 体魄 II → 不屈之志；行囊（根）；钢筋铁骨（需 不屈之志 + 行囊） |
| `TalentTree_Moves.asset` | `moves` | 指向突刺 = `Zhangfei_StabR2`（根）；横扫 = `Zhangfei_Slash`（根）；枪突收尾 = `Zhangfei_LaunchFinisher`（需 指向突刺） |
| `TalentTree_Pools.asset` | `pools` | 旋风（需 聚拢波 + 回旋波）；聚拢波（需 击退波）；回旋波（需 箭矢齐射）；击退波 / 箭矢齐射 / 海浪（根） |

接线：`TalentCatalog_Main` 槽位 0/1/2 = Stats/Moves/Pools；`Hero_Zhangfei.asset` 的 `talentTree` 由 `TalentTree_Main` 改为 `TalentTree_Moves`（招式门读这里）；`TalentTree.scene` 面板已挂 catalog（另有临时三个文字页签）。

**副作用（需知）**：招式门现在真的在拦——横扫 / 指向突刺未购买前不可出，开局只剩 MoveTable 的 Tap→Jab1。

**未接线**：页 3 节点目前只是 flag 占位，三选一池要实际生效需把 `UpgradeDefinition.requiredTalentIds` 填上对应 nodeId（33 个升级目前 0 个填）；页 1 的数值同样还没有读值点（B5）。
