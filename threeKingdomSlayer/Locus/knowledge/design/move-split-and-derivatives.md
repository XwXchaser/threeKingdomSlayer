---
id: kd_b8a52389-de48-4280-a876-7a8d9eaae879
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 招式衍生与拆分

> 2026-11 设计文档。属于需求 **R2**（招式解锁的标的物）+ **R11**（Slash 衍生方向）。
> 解锁机制见 `design/talent-tree-architecture.md`；关卡怎么考这些招式见 `design/stage-configuration.md`。
> 现状盘点与实测证据见 `design/out-of-game-growth-v2-analysis.md`（§2.2）。

---

## 1. 定位

本文管「**玩家有哪些招式、招式之间怎么连**」：招式图模型、当前两条链、点线/划线的拆分口径、以及后续衍生的扩展协议。

| 负责 | 不负责 |
|---|---|
| 招式资产、招式图（转移关系） | 招式数值（→ `design/numerical-balance-draft.md`） |
| 点线 / 划线的划分与命名 | 关卡配置 |
| 解锁哪些招式（归属天赋节点） | 天赋页结构 |
| 新招式的扩展协议与登记 | |

---

## 2. 招式图模型

### 2.1 三层资产

| 层 | 资产 / 类 | 职责 |
|---|---|---|
| **招式表** | `Assets/ScriptableObjects/Moves/MoveTable_Zhangfei.asset`（`MoveTableConfig`） | 中立态入口（手势 → 招式）+ 上下文状态覆盖 + 默认窗口参数 |
| **招式** | `Assets/ScriptableObjects/Moves/Zhangfei/*.asset`、`Assets/Prefabs/UI/Skills/*.asset`（`AttackSkillConfig`） | 表现 / 判定 / 位移 / 时长，全在一个资产里 |
| **状态机** | `Assets/Scripts/MoveSystem/PlayerMoveStateMachine.cs` | 解析输入 → 沿边转移 → 执行 |

### 2.2 边的语义

`AttackSkillConfig.moveEdges`（`List<AttackMoveEdge>`）：

| 字段 | 含义 |
|---|---|
| `gesture` | 接受的手势（`Tap` / `Hold` / `SwipeVertical` / `SwipeHorizontal` / `SwipeDiagonal`） |
| `minChargeLevel` | 最低蓄力等级：0 = 任意；1 = 必须达到一级蓄力 |
| `next` | 后继招式。**留空 = 串尾**，无法再被取消接续 |
| `overrideWindow` / `windowStart01` / `windowEnd01` | 覆盖接续窗口（百分比区间） |

### 2.3 窗口派生

未显式配置时按招式类型派生（`MoveTableConfig.GetDefaultWindow`）：

| 招式类型 | 窗口 |
|---|---|
| Stab | 0.48 – 0.95 |
| Slash | 0.65 – 0.92 |
| Pierce / Sweep | 0.60 – 0.95 |
| Launch | 0.70 – 0.95 |
| Parry / 兜底 | 0.60 – 0.95 |

- 最小绝对时长 `minWindowSeconds` = 0.06（防高攻速下窗口趋近 0）
- 输入在窗口内提前到达 → **进输入缓冲**；窗口外丢弃
- 按住不放时节点会**驻留**在最早窗口起点（上限 1.2s），以便「按住蓄力再滑出」的收尾招成立
- 卡肉会冻结阶段时钟 → 窗口与视觉始终对齐

### 2.4 手势 → 招式的默认映射

`MoveGestureDefaults.ResolveAttackType(gesture, charged)`：

```
Tap                              → Stab
Hold（长按不移动）                → Pierce
竖滑 + 蓄力                      → Launch
竖滑 + 未蓄力                    → Parry
横滑 + 蓄力                      → Sweep
横滑 + 未蓄力                    → Slash
斜滑（无论是否蓄力）              → Slash
```

> ⚠️ **方向不是分叉轴**。「未蓄力的横滑/斜滑都落到 Slash」是设计本身，**不得改成按方向分叉**。
> ⚠️ 手势招架已下线（`parryGestureEnabled = false`），防御改由**防御按键**发动；竖划不蓄力不再触发招架。

---

## 3. 当前两条链

### 3.1 枪突链（点线）

```
中立态 Tap → Zhangfei_Jab1 → Jab2 → Jab3 ──(蓄力上划)──> Zhangfei_LaunchFinisher  = C4
                        └─(Hold 蓄力, 窗口 0.55–1)──> Zhangfei_StabR2            = C2
```

| 招式 | 伤害 | 架势 | isHeavy | 排数 | pushBackRows | 时长 | 受击方向 |
|---|---|---|---|---|---|---|---|
| Jab1 | 30 | 0 | — | 1 | 0 | 0.45s | Front |
| Jab2 | 33 | 0 | — | 1 | 0 | 0.40s | Left（盾侧） |
| Jab3 | 37 | 0 | — | 1 | 0 | 0.60s | Right（反侧） |
| **C4** `LaunchFinisher` | **60** | **80** | ✅ | **3** | 0 | 0.60s | 挑飞 + `releaseParryEnabled` |
| **C2** `StabR2` | **35** | **30** | ✅ | **2** | **1** | 0.66s | 蓄力指向 + 命中震动 |

> 三段枪突**刻意给了三种受击方向**（正面 / 盾侧 / 反侧）→ 在 1021 盾卫身上形成张力：第 2 段被格挡、第 3 段全额伤害。
> C2 挂在 Jab1 的 **Hold 边**（窗口 0.55–1）→ 「点一下后按住可横向改列，松手戳出所指列前 2 排」。

### 3.2 手势回落

`MoveTable_Zhangfei.roots` **只配了 `Tap → Jab1`**。其余手势（横滑 / 竖滑 / 长按）走 `MoveTableConfig.FindRoot` 返回 null 时的**默认回落**（按手势的默认攻击类型）。

> → 想给 Slash 加衍生，**必须先在 `roots` 里显式配 `SwipeHorizontal / SwipeDiagonal → Slash`**，否则衍生边没有入口。

---

## 4. 点线 / 划线的拆分口径

### 4.1 划分依据：**起手手势**

| 线 | 起手 | 覆盖 | 招式 |
|---|---|---|---|
| **点线** | **点**（Tap） | 单列精准输出 + 蓄力指向 | Jab1 → Jab2 → Jab3 →（Hold）C2 →（蓄力上划）C4 |
| **划线** | **划**（横滑 / 斜滑） | 全列横扫 | Slash（→ 后续衍生） |

**归属确认（2026-11）**

| 项 | 归属 |
|---|---|
| **C2 与 C4 是同一条线** | 点线（起手为点），一个分支搞定 |
| **Slash 单独一条线** | 划线（起手为划），后续要改 Slash 并为 Slash 添加更多衍生 |

### 4.2 为什么这样划

| | 点线 | 划线 |
|---|---|---|
| 覆盖 | 单列（`ColumnMode.SingleColumn`） | 全列（`ColumnMode.MultiColumn`） |
| 定位 | 精准 / 打断 / 破盾 | 群伤 / 位移 / 清场 |
| 代价 | 蓄力时间 | 高 CD + 低单发 |
| 对应敌人 | 1011（霸体）/ 1021（盾卫）/ 109（骑兵） | 1012 群 / 105 远程压制 |

→ 两条线正好对应两类反制手段，**解锁顺序天然是「先点线、再划线」**。

### 4.3 点线（解锁顺序）

| 顺序 | 招式 | 天赋节点 | 解锁后玩家能做什么 |
|---|---|---|---|
| 1 | **C2** `Zhangfei_StabR2` | `talent_unlock_c2`（100 铜钱） | 重攻击 → **打碎霸体 / 破盾 / 打断骑兵** |
| 2 | **C4** `Zhangfei_LaunchFinisher` | `talent_unlock_c4`（180 铜钱） | 群挑飞 + 附带格挡 → **BOSS 拆前摇** |

> ⚠️ **C2 必须先于 C4**：C4 的重攻击价值依赖玩家已经理解「重攻击」这个概念；且 C4 的终结技视觉建立在枪突链的三段蓄力之上。

### 4.4 划线

| 招式 | 现状 | 本周 |
|---|---|---|
| **Slash** `Zhangfei_Slash` | dmg 20 / 全列 / `pushBackRows = 0` / CD 0.65 / action 0.5 | 天赋节点「横扫位移」解锁其**横向位移**能力 |

> ⚠️ **Slash 的横向位移当前是死的**：
> `AttackSystem.ApplySlashDirectionalPush` → `UpgradeEffectManager.GetDirectionalPushStep()` → `_directionalPushStep` 字段
> → **全项目只有 `DebugSetDirectionalPush`（调试方法）会写它**，没有任何升级或天赋驱动 → `step <= 0` → 直接 return。
>
> 引擎通道本身是通的（`ColumnManager.ApplyDirectionalPush` → `DisplacementMover.Push.cs:437`），**只差一个数据源**。
> → 本周需要新增驱动（一个升级效果或一个天赋 flag 消费点）。

---

## 5. Slash 衍生方向（R11）

> 需求：「后续要改 Slash 和为 Slash 添加更多衍生。」**具体内容本周不定义。**

### 5.1 前提：先给 Slash 建入口

`MoveTable_Zhangfei.roots` 当前只有 `Tap → Jab1`。要加 Slash 衍生，必须先显式配：

```
roots: SwipeHorizontal → Zhangfei_Slash
       SwipeDiagonal   → Zhangfei_Slash
```

否则默认回落只能拿到 Slash 本体，边无处可挂。

### 5.2 可用的衍生方向（**2026-11-03 已选：Slash C2**）

> ✅ 已选并实现：**Slash C2**（任意方向斩击 → 蓄力松手 → 强化斩击，带左右位移）。
> 详见 `design/combo-zhangfei-slash-c2.md`；下面的方向清单保留作后续备选。

| 方向 | 机制 | 对位 |
|---|---|---|
| **蓄力划** | 横滑 + 蓄力 → `Sweep`（横扫，3 排）——**现有资产已存在**（`Zhangfei_Sweep` 在 `Prefabs/UI/Skills/`），但**未入 `skillConfigs`** | 对应 `MoveGestureDefaults`：横滑 + 蓄力 → Sweep |
| **上划** | 竖滑 + 蓄力 → `Launch`（挑飞，架势 50）——**资产已在 `skillConfigs` 里**（`Zhangfei_Launch`），但 MoveTable 没配 root | 击飞 / 削韧 |
| **连续划** | Slash → Slash → …（`repeatSelf` 或显式边） | 多段横扫、位移连打 |
| **划 + 点** | Slash → 回 Tap 接枪突链 | 点线/划线混合 |
| **划 → 蓄力上挑** | Slash → LaunchFinisher | 全列 → 群挑飞 |

### 5.3 ⚠️ 竖滑招式的归属（暂缓）

`Zhangfei_Launch`（竖滑 + 蓄力）**已经在 `HeroConfig.skillConfigs` 里**（4 个技能配置：Stab / Slash / Launch / Parry），但：

- `MoveTable_Zhangfei.roots` **没配竖滑入口** → 实际靠默认回落
- `Zhangfei_Parry` 的 `parryGestureEnabled = false` → 手势招架已下线，防御改用**防御按键**

**本周决定（R12）**：**未规划入池的招式（竖滑蓄力招 Launch / Pierce / Sweep）不进划线分支**，本期不动。

→ 后果：划线分支**只有 Slash 一个节点**，是个"未长开的分支"。建议：
- 本周保留分支占位（`branchId = sweep`），标为「后续扩展」
- 等 R11 具体内容定了再补节点

---

## 6. 招式解锁与天赋节点的对应

| 招式 | 天赋节点 | 载荷 | 价 | 备注 |
|---|---|---|---|---|
| C2 `Zhangfei_StabR2` | `talent_move_stabc2`（Moves 页） | `moveConfig` | 占位 1 | ✅ 引用已修（活节点在 Moves 页，`PlayerMoveStateMachine.IsMoveLocked` 读的就是它） |
| C4 `Zhangfei_LaunchFinisher` | `talent_move_stabc4`（Moves 页） | `moveConfig` | 占位 1 | ✅ 前置 = `talent_move_stabc2`（**先有 C2 才能买 C4**，链完整） |
| 横扫 `Zhangfei_Slash` | `talent_move_slash`（Moves 页，划线分支占位） | `moveConfig` | 占位 1 | ✅ 划线分支当前只有这一个节点（R11/R12：Slash 衍生本期不定） |
| **Slash C2（划线）** | `talent_move_slashc2` | `moveConfig`（新） | 占位 1 | ✅ **2026-11-03 已实现并实机确认**：任意方向斩击 → 蓄力松手 → 强化斩击（全列 2 排 + 重攻击 + **左右位移 1 排**，方向继承上一段）；普通 Slash 的左右位移**已移除** → 见 `design/combo-zhangfei-slash-c2.md` |

### 6.1 两个必须处理的连带（✅ 2026-11-03 已处理）

| # | 问题 | 处理 | 结果 |
|---|---|---|---|
| 1 | `talent_unlock_c2` 的 `moveConfig` 被清空 → C2 开局就能用，且节点变成 hidden + 无载荷 + 无后继者的**孤儿节点** | 活节点迁到 Moves 页（`talent_move_stabc2` → `Zhangfei_StabR2`）；Main 里的旧副本删除 | ✅ 4 棵树校验零问题 |
| 2 | `talent_unlock_c4.prerequisiteIds = [talent_ice_break_push]`，而该节点要删 → C4 静默变成「无前置根节点」（白送） | C4 的活节点是 Moves 页的 `talent_move_stabc4`，前置指向同页的 `talent_move_stabc2`；Main 里的重复副本删除 | ✅ C2→C4 的购买链完整 |

> ⚠️ **为什么 C4 的前置不跨页**：天赋页是 3 棵树（Stats / Moves / Pools），`GetPrerequisites` **只在本树内**解析
> → 跨页前置会被判成「前置不存在」（校验器直接报错）。需要跨页依赖时用**招式自己的 moveEdges**（招式图），
> 不要用天赋节点的前置。

> 另：`talent_ice_break_push`（破冰顶开）已随 §8.5 迁移**删除**，功能改由秘籍 `manual_ice_break_push` 承担。

---

## 7. 招式数值一览（实测）

| 招式 | 资产 | 类型 | 伤害 | 架势 | 列覆盖 | 排数 | CD | 动作时长 |
|---|---|---|---|---|---|---|---|---|
| Stab | `Zhangfei_Stab` | Stab | 20 | 0 | 单列 | 1 | 0.3 | 0.45 |
| Slash | `Zhangfei_Slash` | Slash | 20 | 0 | **全列** | 1 | 0.65 | 0.5 |
| Launch | `Zhangfei_Launch` | Launch | 2 | 50 | 全列 | 1 | 0.5 | 0.6 |
| Parry | `Zhangfei_Parry` | Parry | 0 | 30 | 单列 | 1 | 0.5 | 0.48 |
| Jab1 | `Zhangfei_Jab1` | Stab | 30 | 0 | 单列 | 1 | 0.3 | 0.45 |
| Jab2 | `Zhangfei_Jab2` | Stab | 33 | 0 | 单列 | 1 | 0.3 | 0.40 |
| Jab3 | `Zhangfei_Jab3` | Stab | 37 | 0 | 单列 | 1 | 0.3 | 0.60 |
| **C2** | `Zhangfei_StabR2` | Stab | 35 | 30 | 单列 | 2 | 0.3 | 0.66 |
| **C4** | `Zhangfei_LaunchFinisher` | Launch | 60 | 80 | 全列 | 3 | 0.3 | 0.60 |

**重攻击标记**：`isHeavy` = 破霸体 / 削韧 / 破盾的判定依据。⚠️ `isHeavy` 与 `interruptsCavalryCharge` 在部分实现里语义混用（`design/gameplay-pillars.md` §6 已记录此耦合）。

---

## 8. 扩展协议（加新连招必须走）

来自 `design/gameplay-pillars.md` §2.5：

| 项 | 规则 |
|---|---|
| 资产路径 | `Assets/ScriptableObjects/Moves/Zhangfei/<武将>_<动作><段号>.asset` |
| id 区间 | 1–9 直通招式 · 11–19 枪突链 · 21–29 指向突刺链 · 30 起每条新链一个十位段 |
| 后继引用 | **一律用 `moveEdges` 直接引用**（禁止字符串查找） |
| 登记 | 登记到 `design/combo-registry.md` ⚠️ **该文档目前不存在** → 新增招式时需一并建 |

### 8.1 登记文档缺失

`design/gameplay-pillars.md` §2.5 要求「登记到 `design/combo-registry.md`」，但 `design/` 下**没有这个文件**。

→ 本周若新增招式（尤其 R11 的 Slash 衍生），**需要一并建立 `design/combo-registry.md`** 作为招式登记表。

---

## 9. 验收口径

| # | 检查 | 方式 |
|---|---|---|
| 1 | C2 在未解锁前**完全不可用**（招式图不响应、不回退成别的招式） | 实机 |
| 2 | C4 在未解锁前同样不可用；解锁 C2 后可解锁 C4 | 实机 |
| 3 | 解锁 C2 后，1012 的霸体可被重攻击打断 | 实机 |
| 4 | 解锁 C2 后，1021 可被破盾（注意方向 Random → 概率） | 实机 |
| 5 | 解锁 C2 后，109 的冲锋可被打断 | 实机 |
| 6 | 解锁「横扫位移」后，Slash 产生横向位移 | 实机 |
| 7 | 划线分支当前只有 Slash 一个节点，**不影响其它分支可用性** | 实机 |
| 8 | 天赋校验器零问题（尤其 C4 的前置） | 菜单「校验天赋树」 |

---

## 10. 参考

| 文件 | 职责 |
|---|---|
| `Assets/Scripts/Data/AttackSkillConfig.cs` | 招式资产字段（表现 / 判定 / 位移 / 时长） |
| `Assets/Scripts/Data/MoveTableConfig.cs` | 招式表（roots / states / 窗口派生） |
| `Assets/Scripts/Data/MoveGesture.cs` | 手势枚举 + 默认映射 |
| `Assets/Scripts/MoveSystem/PlayerMoveStateMachine.cs` | 招式图状态机（含天赋锁定判定） |
| `Assets/Scripts/Player/Combat/AttackSystem*.cs` | 招式执行（含 Slash 位移管线） |
| `Assets/ScriptableObjects/Moves/MoveTable_Zhangfei.asset` | 张飞招式表 |
| `design/gameplay-pillars.md` §2 | 招式图模型与扩展协议 |
| `design/attack-interrupt-system.md` | 霸体 / 打断 / 破势 |
| `design/talent-tree-architecture.md` | 招式解锁机制 |
| `design/out-of-game-growth-v2-analysis.md` | 现状盘点与实测证据 |
