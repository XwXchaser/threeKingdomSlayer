---
id: kd_0441e4db-e43a-49b4-927f-546fc0b67b1e
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 连招登记表与命名规范

连招会越来越多，本文件是**新增连招的唯一登记入口 + 命名口径**。新增一条链：先在 §2 登记表加一行，再按 §3 建链文档与资产。

---

## 1. 命名规范

### 1.1 各层级命名

| 层级 | 规范 | 现有例子 |
|---|---|---|
| 连招链 | `<武将>·<链名>` | 张飞·枪突链 / 张飞·指向突刺链 |
| 链内段位 | `<链名>-<段号>`（末段用「终结」） | 枪突-1 / 枪突-2 / 枪突-3 / 枪突-终结 |
| 链文档 | `design/combo-<武将拼音>-<链名缩写>.md` | `design/combo-zhangfei-aim-stab.md`；枪突链待建 `design/combo-zhangfei-stab3-launch.md` |
| 实现/进度文档 | `plan/combo-<链名缩写>-impl.md` | `plan/stab-c2-charge-aim-release-impl.md` |
| 招式资产 | `Assets/ScriptableObjects/Moves/<武将>/<武将>_<动作><段号>[_<后缀>].asset` | `Zhangfei_Jab1/2/3`、`Zhangfei_LaunchFinisher`、`Zhangfei_StabR2` |
| 招式表 | `Assets/ScriptableObjects/Moves/MoveTable_<武将>.asset` | `MoveTable_Zhangfei` |

命名只描述**功能**（动作 + 段位），不写口号式后缀（不要 `..._v2` / `..._new` / `..._final`）；版本差异靠资产内容与 `id` 表达。

### 1.2 C 编号只作别名，不作正式名

无双口径：`C_n = (n-1) 次普攻 + 蓄力`。但口语里 "C1" 有时指「第一下普攻」、有时指「站桩蓄力技」，容易混。因此：

- **正式文档一律用「链名-段号」**（例如「枪突-3」），不单独用 C 编号。
- 需要与无双或旧文档对照时写成「枪突-3（别名 C4）」。
- 登记表的「别名」列专门放这类对照。

### 1.3 招式资产 `id` 区间（`AttackSkillConfig.id`）

| 区间 | 用途 | 现值 |
|---|---|---|
| 1–9 | 直通招式（按攻击类型） | 1 Stab / 2 Slash / 3 Pierce / 4 Sweep / 5 Launch / 6 Parry |
| 11–19 | 链 A：张飞·枪突链 | 11 Jab1 / 12 Jab2 / 13 Jab3 / 14 LaunchFinisher |
| 21–29 | 链 B：张飞·指向突刺链 | 22 StabR2（已从 15 迁到 22） |
| 30 起 | 每新增一条链给一个十位段，链内按段号递增 | — |

### 1.4 招式自身属性的归属（架构规则）

与攻击类型无关的招式属性**一律挂在招式资产 `AttackSkillConfig` 上**，执行层不按攻击类型写死：

| 属性 | 字段 | 消费方 |
|---|---|---|
| 命中击退 | `pushBackRows` | `AttackSystem.ApplyMovePushBack`（公共入口；升级 `push_wave` 只给「已带击退的招式」加成） |
| 蓄力指向偏摆 | `chargeHoldYawDegrees` / `chargeHoldYawSmoothSeconds` | `StabSweepEffect`（读**当前节点**） |
| 命中震动 | `hitShake*` | `StabSweepEffect`（读**释放招式**） |
| 释放归零过冲 | `stabRedirectSnapRatio` / `stabRedirectOvershootDegrees` | `StabSweepEffect` |
| 挑飞支点 / 额外自转 | `launchPivotFromTailRatio`（0 = 支点在枪尾）/ `launchSweepRollDegrees`（默认 0，旋转交给帧序列） | `LaunchVisualEffect`（终结技） |
| 终结技拖尾透明度 | `slashSweepAlpha`（1 = 与 slash 一样；小值 = 淡拖尾） | `AttackSystem.PlaySweepPresentation` → `SweepEffect` |
| 接续图 | `moveEdges` / `repeatSelf` / `overrideWindow` | `PlayerMoveStateMachine` |

---

## 2. 连招登记表

| 链 | 别名 | 输入 | 落点 / 位移 | 文档 | 资产 | 状态 |
|---|---|---|---|---|---|---|
| 张飞·枪突链 | 枪突-3 + 蓄力 = C4 | 点 → 点 → 点 → 按住 → 上划 | 全列 3 排 + 挑飞 | `design/combo-zhangfei-stab3-launch.md` | `Zhangfei_Jab1/2/3` + `Zhangfei_LaunchFinisher` | 已完成，实机可用 |
| 张飞·指向突刺链 | 口语 C1 → C2 | 点一下 → 按住（可横向改列）→ 松手 | 松手所指列的前 2 排；击退 1 格（升级加成后 2 格） | `design/combo-zhangfei-aim-stab.md` | `Zhangfei_Jab1` + `Zhangfei_StabR2` | 已完成，已实机确认 |

---

## 3. 新增一条连招的 checklist

1. **登记**：§2 加一行（链名、别名、输入、落点、文档、资产、状态）。
2. **链文档** `design/combo-<武将>-<链>.md`：输入模型（含释放手势）、节点与边（窗口 / `minChargeLevel` / `repeatSelf`）、落点与位移、表现（含与已有链的区分）、参数表、验收标准、已知副作用与待实机项。
3. **资产**：按 §1.1 命名；`id` 按 §1.3 取区间；后继一律用 `moveEdges` 直接引用（禁止字符串 id 查找，见 `design/anti-ghost-reference.md`）。
4. **参数归属**：表现/位移参数加在招式资产上（§1.4），不要写进执行层的攻击类型分支。
5. **输入语义**：若该链需要新的输入语义，写进链文档的输入节，并同步 `design/combo-move-state-machine.md` 的输入模型节与 §18 的共享规则。

---

## 4. 共享输入规则（所有链都受约束）

详见 `design/combo-move-state-machine.md` §18：

1. **一次按住只出一记蓄力招式**：出招后必须抬手重按才能重新进入蓄力状态（`hasTriggeredDuringHold` 参与蓄力累积）。
2. **「站桩蓄力 / 连段蓄力」的判定依据** = 是否有**招式表节点**在跑（`IsMoveActive && CurrentMove != null`），不是"手指是否按着"。
3. **蓄力等级 ≥1 的滑动**：连段蓄力按实际是否出招判定「已消费」（未出招不重置蓄力、不吞松手）；站桩蓄力一律算消费（防按住连刷）。
4. **招式自身属性走资产 + 公共入口**，不按攻击类型写死；新增属性时优先放 `AttackSkillConfig`。

### 戳击共用帧规则（本轮定稿）

- 残影帧 `stab_v13` 覆盖**整个「刺出段」**（`stabSpeedFrameStart01 = 0.1` → `stabSpeedFrameEnd01 = 1.0`）—— 不靠缩短窗口来控制。
- **不要再改成「命中那一刻就切清晰帧」**（`StabSweepEffect.CheckHits` 首击处切帧，已回退）：命中判定发生在刺出段约 70%（第 1 排）~87%（第 2 排）处，命中即切会把 v13 砍到只剩一小截（0.45s 动作里约 0.075s），实机几乎观测不到。
- 已知代价（接受）：命中卡肉（约 0.09s）会把画面冻在 v13 上；卡肉之后刺出段剩余部分仍是 v13，到「刺出段结束」才切清晰帧。
- 因此：刺出段 = 残影帧（含卡肉那一下）；穿入 / 回收 = 清晰帧。

### 终结技的旋转表达（本轮定稿）

- 支点在**枪尾**（`launchPivotFromTailRatio = 0`）；不要用 transform 去硬转大角度。
- 旋转姿态由**帧序列**表达：终结技三帧 = `stab_charge2`（起始蓄势姿）→ `stab_rotate1` → `stab_rotate2`，与 slash 的 `stab → rotate1 → rotate2` 同一个道理（像素图不靠 transform 硬转，否则会转成背面/糊掉）。
- 位移只保留很小的上抬 + 很小的左移；“向左上”的成分交给旋转与帧序，不再用斜向平移+一层 slash 来凑。
- 深度：**起手位保持不动**（与蓄力姿一致），只在「挑出」段沿镜头前方前移 `launchForwardShift = 2.0`（作用在 `apexPosition` 上，见 `LaunchVisualEffect`）。Battle 是透视相机（(0,3,-10)、18° 俯角），沿相机前方移动 = 屏幕位置不变、视觉变小；在枪体深度约 10.1 处，2.0 对应挑出终点约小 16~17%（0.6 只有约 6%，实测不够；3.0 约 23%）。
- 拖尾只留一层很淡的 slash 扫掠（`slashSweepAlpha = 0.25`）。
