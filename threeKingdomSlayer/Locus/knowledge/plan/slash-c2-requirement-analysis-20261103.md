---
id: kd_fa0760c6-0d8e-4fd2-95ed-791b35a4da67
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_slash_c2_analysis_20261103
injectMode: inherit
aiEditMode: inherit
---

# 需求分析 · Slash 去位移 + 新增 SlashC2（2026-11-03）

> 需求原文：「slash 移除左右位移效果，新增 slashC2，任意方向 slash 后蓄力松开发动，释放一次与上次 slash 的强化 slash，带有位移效果，
> slashC2 需要解锁且需要设计蓄力动作和招式动作。先分析需求，本质只是做拆分。」
>
> 本文是**分析 + 落地方案**；实现按「不需要肉眼验收」的部分先做，数值与美术部分标注为待确认。

---

## 1. 需求拆成三句话

| # | 需求 | 落到工程上是什么 |
|---|---|---|
| ① | **Slash 移除左右位移** | 删掉 Slash 命中后的「左右位移」通道（`ApplySlashDirectionalPush` 及其 debug 驱动） |
| ② | **新增 SlashC2**：任意方向 Slash → 蓄力松手 → 释放「与上次 Slash 同方向的强化 Slash」，**带位移** | 一个**新的招式资产** + 一条**接续边** + 位移能力从 Slash 挪到它身上 |
| ③ | **SlashC2 需要解锁**（+ 蓄力/招式动作要设计） | 天赋树 Moves 页加一个 `moveConfig` 指向 SlashC2 的节点（解锁判定**已有现成机制**）；蓄力/招式动作 = **美术**，需要出资源 |

> **「本质只是做拆分」的准确含义**：②③ 不用新写战斗系统 —— **枪突链的 C2（`Zhangfei_StabR2`）就是现成模板**，
> SlashC2 照抄它的接续方式即可（**新资产 + 一条边 + 一个解锁节点**）。

---

## 2. 现状实测（关键事实）

### 2.1 Slash 的左右位移：其实是**只有 debug 能开**的死通道

| 位置 | 事实 |
|---|---|
| `AttackSystem.Types.cs:171` | `ExecuteSlash` 的 `onAllHit: () => ApplySlashDirectionalPush(hitTargets, leftToRight)` |
| `AttackSystem.Utils.cs:206` | `ApplySlashDirectionalPush` → `step = UpgradeEffectManager.GetDirectionalPushStep()` → **`step <= 0` 直接 return** |
| `UpgradeEffectManager.cs:43/398` | `_directionalPushStep` 只有 `DebugSetDirectionalPush()` 能写（`DisplacementDebugTool`） |

→ 结论：**正常游戏里 Slash 的左右位移从未发生**（step 恒为 0）。所以「移除」= 删掉这条**从未生效**的通道 ✓ 无行为回归风险。
→ 而位移能力本身（`ColumnManager.ApplyDirectionalPush(targets, step, leftToRight, canInterruptCFrame)`）是现成的 ✓ → **给 SlashC2 复用**（能力搬家，不新建系统 ✓）。

### 2.2 C2 的现成模板（照抄对象）

`Zhangfei_StabR2`（id 22）——「指向突刺 C2」：

| 字段 | 值 | 含义 |
|---|---|---|
| `attackType` | Stab（**与母招同类型** ✓） | → **不需要新增 AttackType 枚举** ✓ |
| `damage` / `poiseDamage` | 35 / 30（母招 Stab 是 20 / 0） | 强化 |
| `isHeavy` | 1（母招 0） | → 重攻击 ✓（破霸体 / 破盾的关键 ✓） |
| `columnMode` / `rangeRows` | SingleColumn / 2 | 覆盖更远 |
| `pushBackRows` | 1 | 命中击退 |
| `windowStart01/End01` | 0.65 / 1 | 接续窗口 |
| `repeatSelf` | false | 串尾终止 |
| `moveEdges` | 空 | 由**上一段**指向它 |

### 2.3 接续边怎么配（枪突链实测）

```
MoveTable_Zhangfei.roots:  Tap → Zhangfei_Jab1
Zhangfei_Jab1.moveEdges:
    ① gesture=Tap,  minCharge=0 → Zhangfei_Jab2
    ② gesture=Hold, minCharge=1 → Zhangfei_StabR2   ← ★「蓄力松手 → C2」的写法
Zhangfei_Jab2.moveEdges:    Tap, minCharge=0 → Zhangfei_Jab3
Zhangfei_Jab3.moveEdges:    SwipeVertical, minCharge=1 → Zhangfei_LaunchFinisher
```

→ **SlashC2 的接续边就是 ② 的翻版**：`gesture=Hold, minCharge=1 → SlashC2` ✓

### 2.4 「蓄力松手」这条输入是怎么活起来的

| 环节 | 事实 |
|---|---|
| 手势分类 | 长按不动 = `MoveGesture.Hold`；横滑/斜滑 = `SwipeHorizontal` / `SwipeDiagonal` ✓（`MoveGesture.cs`） |
| 蓄力门槛 | `chargeLevel >= 1`（按住时长分级，`InputManager.CurrentChargeLevel`）✓ |
| 连段蓄力放行 | `InputManager.Gesture.cs:284`：`gesture == Hold && restrictChargedToVertical && !comboChargeActive` → **拦截**；`comboChargeActive` 为真时放行 ✓ |
| `comboChargeActive` 谁置真 | `PlayerMoveStateMachine.BeginMove`：节点有 `minChargeLevel>=1` 的边且**手指还按着** → 置真 + `BeginComboChargeHold()`（枪体停住等蓄力 ✓）✓ |

→ 所以 SlashC2 的蓄力表现**复用现有的连段蓄力机制**（枪体拉回 + 蓄力条 ✓），**不需要新输入系统** ✓。

### 2.5 ⚠️ 一个必须处理的冲突：Slash 目前**不在招式表里**

| 事实 | 后果 |
|---|---|
| `MoveTable_Zhangfei.roots` 只有 `Tap → Jab1` ✓ | Slash / Sweep / Launch / Parry 都走**直通模式**（`ExecuteDirect`，`_currentMove = null`）✓ |
| `ResolveFromCurrent` 的边匹配要求 `_currentMove != null`（`PlayerMoveStateMachine.cs:287`） | → **Slash 想当节点接续，就必须进招式表当 root** ✓ |
| `MoveTableConfig.FindRoot(gesture)` **只按手势匹配，不看蓄力等级** | → 把 Slash 加成 `SwipeHorizontal` 的 root 后，**「横滑+蓄力 → Sweep」会被抢走变成 Slash** ✗✗ |

→ **必须给 `MoveEntry` 补一个 `minChargeLevel`**（与 `AttackMoveEdge.minChargeLevel` 同概念 ✓✓），
root 匹配改成「手势 + 蓄力等级」✓。这是一处**很小、很通用**的扩展 ✓。

### 2.6 「与上次 Slash 同方向」怎么传

- 方向来自输入：`GestureInput.slashLeftToRight`（`InputManager` 按滑动方向给 ✓）→ **Slash 那一下有方向 ✓**；
- 但 SlashC2 的输入是 **Hold**（长按）→ **没有方向** ✗ → 必须由**上一段**继承。
- 状态机目前只记了 `_entryGesture`（`PlayerMoveStateMachine.cs:250`）**没记方向** ✗ → 需要补一个 `_entrySlashLeftToRight` ✓。

---

## 3. 落地方案（8 步）

| # | 步骤 | 落点 | 需要谁定 |
|---|---|---|---|
| ① | **删** Slash 的左右位移 | `AttackSystem.Types.cs:171` 去掉 `onAllHit` 的 push；`AttackSystem.Utils.cs` 删 `ApplySlashDirectionalPush`；`UpgradeEffectManager` 删 `_directionalPushStep` / `GetDirectionalPushStep` / `DebugSetDirectionalPush`；`DisplacementDebugTool` 删对应字段 | —（需求已定） |
| ② | **位移能力搬家** | `AttackSkillConfig` 加 `slashDirectionalPushRows`（默认 0 = 不位移）；`ExecuteSlash` 改成「按配置值决定是否位移」→ **同一个执行器同时服务 Slash(0) 与 SlashC2(N)** ✓ | 位移几排：**我先按 1 排**（可调） |
| ③ | **新招式资产** | `Assets/ScriptableObjects/Moves/Zhangfei/Zhangfei_SlashC2.asset`（照 StabR2 的字段结构 ✓）：`id` 新号、`attackType=Slash`、`damage` / `poiseDamage` / `isHeavy` / `rangeRows` / `window` / `slashDirectionalPushRows` | 伤害/架势/行数：**我先按 StabR2 的比例给**（Slash 20 → 35，架势 0 → 30，isHeavy 1，rangeRows 2，窗口 0.65–1）——**都在资产里，随时能调** ✓ |
| ④ | **招式表 root 支持蓄力等级** | `MoveEntry` 加 `minChargeLevel`；`MoveTableConfig.FindRoot(gesture, chargeLevel)` 改成「手势匹配 + `chargeLevel >= minChargeLevel`」✓ | —（通用扩展 ✓） |
| ⑤ | **Slash 进表 + 保留 Sweep** | `MoveTable_Zhangfei.roots` 加：`SwipeHorizontal(minCharge=0) → Slash`、`SwipeHorizontal(minCharge=1) → Sweep`、`SwipeDiagonal(minCharge=0) → Slash`（斜滑无论蓄力都还是 Slash ✓ 与现有默认映射一致 ✓） | — |
| ⑥ | **接续边** | `Zhangfei_Slash.moveEdges += { gesture=Hold, minCharge=1, next=SlashC2, overrideWindow=true, window=[0.55,1] }`（照 Jab1→StabR2 ✓） | 窗口：**我先按 0.55–1**（可调） |
| ⑦ | **解锁节点** | `TalentTree_Moves` 加节点 `talent_move_slashc2`（`moveConfig = SlashC2`，`coinCost` 沿用占位 1 ✓，描述写清效果）→ **解锁判定零代码**（`PlayerMoveStateMachine.IsMoveLocked` 已按 `hero.talentTree` 查 ✓，Moves 页正是它 ✓） | —（沿用占位价 ✓） |
| ⑧ | **方向继承** | 状态机记 `_entrySlashLeftToRight`；`ResolveFromCurrent` 执行后继时，若后继是 `AttackType.Slash` → 用**上一段的方向**而不是本次 Hold 的方向 ✓ | — |

> ①②③④⑤⑥⑦⑧ 里 **只有 ③ 的数值**和 **窗口**是设计数值（都在资产里，可随时调 ✓）；
> **⑨ 美术**（下一步单独列 ✓）。

---

## 4. 需要美术/前端提供的资源（我不能做）

| # | 资源 | 现状 | 说明 |
|---|---|---|---|
| ① | **SlashC2 的招式动作**（斩击表现） | 暂无 | 需要一套「强化 Slash」的表现：建议沿用 Slash 的横扫贴图序列 + 加宽/加亮拖尾 |
| ② | **SlashC2 的蓄力动作**（连段蓄力时枪体的姿态） | 现有蓄力视觉是 **Stab 专用**（`ChargeStabVisual`）✓ | Slash 的蓄力姿态需要单独一张（收枪 + 蓄力条对齐） |
| ③ | **位移反馈**（敌人在被左右推时的表现） | `ColumnManager.ApplyDirectionalPush` 只改列号 ✓ 无表现 | 位移若要有「被推开」的观感，需要位移表现层（可先不做，逻辑先通） |
| ④ | **天赋页图标** | SlashC2 节点需要图标 | 现有 `TT_Icon_*` 是旧美术占位，天赋页美术整体重做时一起出 |

> 前端侧：天赋页**不需要改代码**就能显示新节点（面板按 `moveConfig` 自动取图标与文案 ✓）；
> 但**面板按 group 画泳道**仍是对方未做的部分（`groupId` 我已填好 ✓）。

---

## 5. 风险与注意事项

| # | 风险 | 处理 |
|---|---|---|
| 1 | Slash 进表后，**「横滑+蓄力 → Sweep」被抢**（root 只按手势匹配 ✗） | ④ 的 `minChargeLevel` 扩展 ✓（不扩展就会破坏横扫 ✗✗） |
| 2 | **斜滑+蓄力** 现在会落到 Slash（现有默认映射如此 ✓） | 保持不变 ✓（root 用 minCharge=0 ✓） |
| 3 | 长按蓄力在**教学关**被 `restrictChargedToVertical` 拦住 | `comboChargeActive` 会放行 ✓（与 C2 同机制 ✓）；教学关若要教 SlashC2，需对方补一段教学 |
| 4 | SlashC2 的**伤害/架势/位移排数**是我提的初值 | 全部在资产里，随时能调 ✓（不动代码） |
| 5 | Slash 的左右位移**本来就从未生效**（step 恒 0） | ① 的删除**无行为回归** ✓ |

---

## 6. 状态（2026-11-03 已落地）

| 步骤 | 状态 | 备注 |
|---|---|---|
| ① 移除 Slash 左右位移 | ✅ 已删 | `ApplySlashDirectionalPush` 改为**按资产配置**驱动；debug 的 `_directionalPushStep` / `DisplacementDebugTool` 入口一并删除（本来就只有 debug 能开） |
| ② 位移能力搬家 | ✅ | 新增 `AttackSkillConfig.slashDirectionalPushRows`（0 = 不位移）；普通 Slash = 0，SlashC2 = 1 |
| ③ SlashC2 资产 | ✅ 已建（数值待定稿） | `Assets/ScriptableObjects/Moves/Zhangfei/Zhangfei_SlashC2.asset`（id 30） |
| ④ root 支持蓄力等级 | ✅ | `MoveEntry.minChargeLevel` + `FindRoot(gesture, chargeLevel)`（同手势取**蓄力要求最高且已满足**的入口） |
| ⑤ Slash 进表 | ✅ | roots：横滑(c0)→Slash、横滑(c1)→Sweep、斜滑(c0)→Slash |
| ⑥ 接续边 | ✅ | Slash.moveEdges：Hold(minCharge=1) → SlashC2，窗口 0.55–1 |
| ⑦ 解锁节点 | ✅ | `TalentTree_Moves` → `talent_move_slashc2`（占位价 1，解锁判定零代码） |
| ⑧ 方向继承 | ✅ | 状态机记 `_entrySlashLeftToRight`；后继是 Slash 系时继承上一段方向 |
| ⑨ 美术 | ⏸ **等美术/前端** | 蓄力动作 + 招式动作 + 位移反馈 + 天赋页图标 |

**实机验收全过**：Slash→蓄力松手→打出 SlashC2（伤害 35 ✔）｜带左右位移且方向继承上一段 ✔｜
未解锁时打不出来 ✔｜普通 Slash 不再位移 ✔｜「横滑+蓄力→Sweep」不被抢走 ✔｜天赋树校验零问题 ✔。

连招文档：`design/combo-zhangfei-slash-c2.md`（已按 `design/combo-registry.md` §3 的 checklist 写）✔
登记表已加一行 ✔（`design/combo-registry.md` §2）。

> 验证口径：① 删干净（不再有 `ApplySlashDirectionalPush` 调用链 ✓）；④ 改完「横滑+蓄力 → Sweep」仍成立 ✓；
> ⑦ SlashC2 在 Moves 页可见且**未解锁时打不出来**（`IsMoveLocked` ✓）、解锁后能打出来 ✓。
