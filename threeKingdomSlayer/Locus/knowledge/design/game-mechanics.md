---
id: kd_6c6ceb9f-a422-4524-969d-06f8a432857d
injectMode: inherit
summary: 系统实现与资产附录（玩法文档请看 design/gameplay-pillars.md）：以工程资产/代码实测为准，列出核心循环、卷轴路线节点结构、5列阵型与补齐/位移规则、手势与招式数值表、敌人与BOSS图鉴、大招、打击反馈、局内成长（击杀里程碑/连击/经验三选一/主动技能）、铜钱经济、UI与配置资产地图、实现状态清单、已知问题与硬约束。
aiEditMode: inherit
---

# 三国无双割草（一夫当关）系统实现总览

> **玩法层面的文档入口是 `design/gameplay-pillars.md`（五大支柱：轻重攻击 / 连招 / 技能组合 / 敌人机制 / 路线分支与节点奖励）。本文是系统实现与资产附录**：UI 清单、技术架构、配置资产地图、实现状态清单、已知问题与硬约束。

> 本文写的是当前工程里**真实存在**的机制与数值，用于讨论新机制时避免与既有系统冲突。
> 数值来自工程实测（Enemy/AttackSkillConfig/UpgradeDefinition 等资产与 `Battle.scene`、`MainMenu.scene` 的场景实例值），不是设计愿景。

## 0. 口径与状态标记

| 标记 | 含义 |
|---|---|
| ✅ **已部署** | 在 `Assets/Scenes/Battle.scene` / `MainMenu.scene` 的真实流程中运行 |
| 🟡 **已实现未接入** | 代码/资产存在，仅用于测试关、或未挂进主菜单入口 |
| ⛔ **设计中未实现** | 知识库有设计文档（多为 30 分钟数值方案、局外成长、条件系统），工程未落地 |

一句话定位：竖屏手机、单手**手势**操作的一夫当关型三国割草。玩家固定操作张飞，站在 5 列阵型的前方，敌军以「排」为单位纵队推进；玩家用手势出招把敌人打退、打飞、打死，靠**局内三选一**成长，沿**卷轴节点路线**推进，节点尽头打 BOSS。

---

## 1. 核心循环

```
战斗（波次推进，5 列阵型）
 → 击杀敌人：得铜钱 / 经验宝石 / 推进击杀里程碑 / 累积连击
 → 经验满级：暂停 → 三选一（被动/主动技能）→ 玩家确认 → 继续
 → 波次清空：等待奖励结算 → 节点战斗结束
 → 节点出口：点击唯一出口或多出口二选一 → 卷轴旅行演出
 → 下一节点（战斗 / 情景 / 分支）
 → 终点节点：终局战 → 终点演出 → 整关结算（铜钱入存档）
```

设计侧目标（⛔ 未实现）：一局约 20–30 分钟、约 15 波、每 3 波一个 BOSS、约 42 次升级机会、玩家最多持有 6 被动 + 4 主动。当前**没有任何关卡**按该目标配置完成。

---

## 2. 局结构与路线系统

### 2.1 场景职责

| 场景 | 职责 |
|---|---|
| `Assets/Scenes/MainMenu.scene` | 开始/继续/删除存档、选关网格、铜钱显示、音量设置 |
| `Assets/Scenes/Battle.scene` | **唯一战斗运行场景**：玩家、敌人、波次、HUD、路线流程宿主、QTE、三选一 |
| `Assets/Experiments/CurvedScroll/YJunctionSample.unity` | 卷轴视觉层（Additive 加载），只提供沿途环境与分支道路 |

节点**不是**独立 Unity 场景，而是 ScriptableObject 逻辑资产（`FakeRouteNodeConfig` / `RouteNodeConfigV2`）。

### 2.2 主菜单实际入口（✅ 已部署）

`MainMenu.scene` 的 `StageConfigManager` 当前只配了一个**卷轴路线**：`Assets/RouteData/ChangbanpoScroll/Changbanpo_ScrollRoute.asset`。线性关卡列表与 `routeStagesV2` 列表均为空。

`StageController` 支持 4 种模式，优先级为：卷轴路线（FakeRoute）> RouteStageV2 > 旧路线（RouteStage）> 普通线性关卡。旧路线入口点击后只打印「旧路线入口已停用」。

### 2.3 当前卷轴路线拓扑（✅ 已部署，内容是**流程预览**）

路线 `routeId=changbanpo_scroll_preview`、`stageId=1`、起始 `startNode=N1`：

| 节点 | 名称 | 类型 | 战斗配置 | 终点 | 存档点 |
|---|---|---|---|---|---|
| N1 | 当阳乱军 | 战斗 | `N1_ArtPreview_Battle`：一排 `[1021, 0, 1011, 0, 109]` | — | 是 |
| E0 | 残门垣前 | 战斗 | `E0_Battle_Preview`：一排 `[0, 0, 1011, 0, 0]` | — | 是 |
| J1 | 城郊分叉 | 分支（无战斗） | — | — | 是 |
| EV | 村落外围 | 情景（无战斗） | — | — | 是 |
| N3 | 村落搜救 | 战斗 | 复用 `SmallBattle`（1×1011） | ✅ | 是 |
| N2 | 官道断后 | 战斗 | 复用 `SmallBattle`（1×1011） | ✅ | 是 |

流程：`N1 → E0 → J1 →{ 左：EV → N3 ｜ 右：N2 }`。

> 说明：**当前路线是美术/流程验证用的线性骨架**（每场战斗 1 排 1–3 个敌人、`clearCoinReward=0`、`fillUpRule=PerColumn`），不是正式数值关。正式「长坂坡 8–10 分钟」六战役节点 + 情景节点 + 分支拓扑只有设计文档（`design/changbanpo-six-node-level-design.md`）。

### 2.4 节点资产字段与运行规则

```
FakeRouteNodeConfig
├─ nodeId / displayName
├─ battleEntries[]   每项 = battleConfig + conditionEnabled
├─ outgoingChoices[] 出口（0/1/多个）
├─ isFinalNode       终点：无出口，完成后直接结算
├─ savePoint         抵达即写快照（当前 6 个节点全为真）
├─ completionPolicy / terminalPolicy
└─ entries: dialogues[] / rewards[] / entryDialogue / postBattleDialogue / postBattlePresentation / visualProfile
```

统一流程（所有节点同构）：**抵达节点 → 播放节点内容 → 显示出口 → 玩家点击出口 → 播放该边旅行演出 → 提交目标节点**。单出口也必须点击确认，不会自动前进（终点除外）。每次进入节点最多挑战 1 个未完成 BattleEntry，已完成的条目不再重复。

### 2.5 旅行演出与视觉

- 旅行由 `FakeRouteTravelPresentation` / `ScrollRoutePresenter` 驱动，读 `routeSceneName`（= `YJunctionSample.unity`），`groundHeight = -1.8`，使用 Battle 相机。
- 视觉是一条约 160 世界单位长的 2.5D 分叉卷轴（直路 → Y 分叉 +45°/-45° 两支），近中远景面片随进度前进掠过；战斗坐标（玩家/敌人/阵型/QTE/投射物）**不随卷轴移动**。
- 已验收基线：节点间距 80 单位、单段旅行时长 3 秒、`Length=160`、分叉半径 ≈28.16、转角 45°、54 株风动草与 148+ 可编辑侧景。

### 2.6 存档点与失败恢复（✅ 已部署）

- 存档点 = 节点**到达 Head 时刻**写快照（路线、节点/BattleEntry 完成状态、玩家生命、等级、已获升级与等级、主动技能、大招能量、击杀数、局内铜钱、路线选择历史）。
- 不保存：连击数、当前攻击/蓄力/QTE、临时 Buff、场上敌人与投射物。
- 失败后「重新开始」= 执行运行时 `ResetAll` → 载入最近存档点快照 → 从**该存档节点自身**的入口重新进入。
- 主菜单「继续游戏」= 从最后未完成的路线关卡 `startNode` 重新开始，**不**读失败恢复快照。

---

## 3. 战斗空间与波次规则

### 3.1 阵型与坐标

- 战场固定 **5 列**（col 0 最左 … col 4 最右）；排（row）表示纵深，**row 0 最靠近玩家**，row 越大越远。
- 波次配置 `WaveConfig.rows: List<RowConfig>`，每排 `enemyIds: int[5]`（按列给出敌人 ID）。
  - `0` = 空槽；整排 `(0,0,0,0,0)` = 普通空排占位符；任意槽位 `999` = **节奏门**（整排不生成敌人，前方敌群清空前禁止后方跨越）。
- 站位宽度来自 `FormationConfig`（`DefaultFormation`：逐排半宽 2 / 2.5 / 3 / 4 / 4.5，`rowSpacing=2.5`，`maxVisibleRows=5`，逐排透明度 1/0.9/0.8/0.75/0.75）。
- 波次可配 `healthMultiplier` / `bossHealthMultiplier` / `attackSpeedMultiplier` / `damageMultiplier` / `waveTintColor`（波次染色）/ 补齐延迟。

### 3.2 补齐（唯一合法移动来源）

| 现象 | 规则 |
|---|---|
| 普通补齐 | 只有**某排在全部列都清空**才触发，后方排整体前移一排，由 `ColumnManager` 统一调度 |
| 单列死亡 / 单列空槽 / 击退留空 | 不触发补齐 |
| `Launched`（被挑飞） | 仍占位，该排不算清空 |
| 新波次入场 | 敌人先生成在逻辑目标排**后方两排**，再由 `SpawnEntry` 连续入场；入场不修改逻辑排队列、不开启节奏门 |
| 击退 | 只移动被击退者，停顿时长后**返回自己原槽**；不影响其他敌人 |
| 横向位移（推/聚拢） | 只改列，不引发行补齐，不共用击退回位逻辑 |
| BOSS | 独立推进（`Approaching → 暂停在 row2 → 前两排全清 → row1 → 1 秒无敌缓冲 → InCombat`），不参与普通补齐，并作为**墙壁**阻挡敌人被击退到身后 |

补齐规则有两种模式：`FillUpRule.PerRow`（整排清空触发，正式规则）与 `FillUpRule.PerColumn`（历史兼容；当前卷轴预览关用的是它，`Stage_Trial` 用 PerRow）。

### 3.3 不可变约束（新机制必须先满足，来自 `design/immutable-constraints.md`）

1. 位移不可重叠：任何位移前必须验证目标槽，Boss 墙不可越过。
2. 未受位移者保持原位：位移只改变实际命中的敌人。
3. 普通补齐唯一触发条件 = 整排全清，调度权在 `ColumnManager`。
4. 击退回位必须返回**精确原槽**，不得改为最近空位或列压实。
5. 打断边界：无霸体敌人在可打断窗口被打断；已生成飞射物独立存在；未被命中者不因他人位移被中断。
6. 各类位移/补齐订单必须区分语义（普通补齐 / 击退回位 / 横向位移 / Boss 推进 / SpawnEntry），订单带 owner + generation。
7. 敌人**不得**因攻击范围检查、计时器、眩晕恢复或落地自行创建移动。
8. 聚拢类效果使用自己的冲突裁决，不借用击退回位或普通补齐。

---

## 4. 玩家操作、招式与连招

### 4.1 玩家属性

| 项 | 值 |
|---|---|
| 英雄 | 张飞（`Hero_Zhangfei`，唯一武将） |
| 生命 | 100 |
| 复活次数 | 0（`reviveHealthPercent=0.5` 已配但复活次数为 0） |
| 主动技能槽 | `itemSlotCount = 4` |
| 被动技能上限 | 6（`UpgradeChoiceManager` 硬编码） |

### 4.2 手势 → 招式映射（✅ 已部署）

判定都在**抬手**时刻，滑动阈值 DPI 自适应（场景值 50px），蓄力分级门槛 `0.3 / 0.6 / 1.0` 秒：

| 输入 | 蓄力等级 0 | 蓄力等级 ≥1 |
|---|---|---|
| 点击（未滑动） | 戳击 Stab（按列） | — |
| 长按后松手（未滑动） | — | 穿刺 Pierce |
| 竖滑 | 招架 Parry | 挑飞 Launch |
| 横滑 | 斩击 Slash | 横扫 Sweep |
| 斜滑 | 斩击 Slash | 斩击 Slash |

附加规则：**一次按住只出一记蓄力招式**（出招后必须抬手重按）；「连段蓄力」与「站桩蓄力」的判定依据是**是否有招式表节点在跑**；连段中有「松手释放」的边时，该次松手交给招式状态机裁定。

### 4.3 招式状态机与连招图（✅ 已部署）

`PlayerMoveStateMachine` 挂在 Player 上（`bufferEnabled=1`、`holdWaitEnabled=1`、`holdWaitMaxSeconds=1.2`，招式表默认取 `Hero_Zhangfei.moveTable`）。输入不再直接映射到攻击类型，而是投递给状态机：

```
节点 = 一个招式（AttackSkillConfig 资产，含视觉/判定/位移/时长）
边   = (手势 + 窗口 + 最小蓄力等级) → 后继招式资产
窗口 = 同一条视觉序列的百分比区间（如 0.65~1.0），默认按命中窗口自动派生，最小 0.06s
无匹配边 → 不响应（当前招式收尾回中立）
```

当前已实装的链：

| 链 | 输入 | 资产 | 落点/位移 |
|---|---|---|---|
| 张飞·枪突链 | 点 → 点 → 点 →（按住蓄力）上划 | `Zhangfei_Jab1/2/3` + `Zhangfei_LaunchFinisher` | 末段全列 3 排 + 挑飞（poise 80，可打断骑兵冲锋） |
| 张飞·指向突刺链 | 点一下 → 按住（可横向改目标列）→ 松手 | `Zhangfei_Jab1` + `Zhangfei_StabR2` | 松手所指列前 2 排，击退 1 排 |

### 4.4 招式数值（`AttackSkillConfig`，工程实测）

| 招式 | id | 伤害 | CD（独立CD模式） | 动作时长 | 覆盖排 | 架势伤害 | 大招能量 | 备注 |
|---|---|---|---|---|---|---|---|---|
| Stab 戳击 | 1 | 20 | 0.30 | 0.45 | 1 | 0 | 10 | 主力平A；命中产生击退波（招式 `pushBackRows` 决定） |
| Slash 斩击 | 2 | 8 | 0.65 | 0.50 | 1 | 0 | 10 | 方向性侧推 |
| Pierce 穿刺 | 3 | 20 | 3.0 | 0.50 | 3 | 0 | 15 | 蓄力招式，可打断骑兵冲锋 |
| Sweep 横扫 | 4 | 5 | 2.0 | 0.55 | 3 | 0 | 12 | 蓄力招式 |
| Launch 挑飞 | 5 | 2 | 0.50 | 0.60 | 1 | 50 | 8 | 削韧 + 浮空 |
| Parry 招架 | 6 | 0 | 0.50 | 0.20 | 1 | 10 | 5 | 双模式：弹反飞行物 / 近距范围削韧 |
| 枪突-1 | 11 | 20 | 0.30 | 0.45 | 1 | — | 10 | 后继：点击→枪突-2；按住(≥1级)→指向突刺 |
| 枪突-2 | 12 | 26 | 0.30 | 0.40 | 1 | — | 10 | 后继：点击→枪突-3 |
| 枪突-3 | 13 | 34 | 0.30 | 0.60 | 1 | — | 10 | 后继：按住(≥1级)→终结技 |
| 枪突-终结 | 14 | 60 | 0.30 | 0.60 | 3 | 80 | 8 | 挑飞 + 大幅扫击表现 |
| 指向突刺 | 22 | 35 | 0.30 | 0.66 | 2 | 0 | 5 | 击退 1 排，串尾 |

### 4.5 伤害与冷却模型

- 伤害 = `cfg.damage × UpgradeEffectManager.GetDamageMultiplier() × 射程衰减惩罚`。
- **命中是时间分辨的**：`StabSweepEffect` 按枪尖推进距离逐个命中（容差 0.35），`SweepEffect` 按敌人 x 阈值在移动 tween 中逐个命中；不是一帧全结算。
- **空挥**照常播特效，但不结算冷却、不给大招能量、不触发被动计数（首次实际命中才结算）。
- 冷却两种模式：场景当前为**动作锁模式**（`useActionBasedCooldown=true`），攻击动画期间锁住所有攻击输入，**不查每招独立 CD**；独立 CD 模式保留为未来奖励效果。
- 攻速提升会**同时压缩动作时长与整条视觉序列**（连招窗口按百分比自动跟随）。

### 4.6 打击反馈（✅ 已部署）

| 层 | 内容 |
|---|---|
| 局部卡肉 | `HitFeedbackManager` 三档：Light 0.045s / Standard 0.09s / Heavy 0.14s；只冻结敌人动画 + 特效序列 + 飞行物；**DoT 不卡肉** |
| 敌人表现 | 闪白、受击缩放/抖动、受击动画方向、伤害数字按强度分级 |
| 镜头 | `CameraFeedbackController` 命中震动 |
| 像素特效 | `PixelHitEffectManager` 命中火花、斩痕拖尾 |
| 声音 | `AudioManager` + AudioMixer 三档（总/音乐/音效），原生 AudioSource |

### 4.7 大招（狂怒，`UltimateSkillConfig`）

| 项 | 值 |
|---|---|
| 能量上限 / 消耗 | 100 |
| 冷却 | 10s |
| 持续 | `berserkDuration = 5s` |
| 效果 | 玩家无敌；按 `berserkStabCooldown=0.1s` 自动轮转对所有列戳击；期间禁止手动技能输入；血条变橙 |
| 伤害 | `damage=10` ×（武将加成）× `berserkDamageMultiplier` |
| 能量来源 | 招式命中（表中 `ultimateEnergyGain`）、击杀普通敌人 +5、击杀 BOSS +25 |

---

## 5. 敌人与 BOSS

### 5.1 图鉴（`Assets/Resources/EnemyPrefabs/` 预制体实测；预制体即唯一数值源）

| ID | 名称 | HP | 攻击力 | 攻击间隔 | 射程(排) | 移速 | 架势 | 眩晕 | 经验 | 铜钱 | 特殊机制 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 101 | 持剑杂兵 | 200 | 3 | 2.0s | 1 | 0.3 | 50 | 5s | 80 | 10 | 基础割草单位（单步普攻） |
| 1011 | 持剑杂兵_1011 | 200 | 3 | 2.0s | 1 | 0.3 | 50 | 5s | 80 | 10 | 101 的新画风替身（独立动画/锚点） |
| 102 | 盾兵 | 30 | 10 | 5.0s | 1 | 0.3 | 50 | 1.5s | 50 | 10 | 常态减伤 50%；**共享血量**（相邻共享）；实验品 |
| 1021 | 盾卫 | 40 | 10 | 5.0s | 1 | 0.3 | 50 | 1.5s | 50 | 10 | 盾侧格挡 / 破盾 / 胆怯（`ShieldGuardEnemy`） |
| 103 | 长枪兵 | 30 | 15 | 5.0s | 1 | 0.2 | 50 | 3s | 50 | 10 | **C 技霸体**（`isCAttack=true`，触发 `CAttack`，前摇 3s / 收招 1.5s） |
| 104 | BOSS | 500 | 10 | 5.0s | 2 | 0.3 | 100 | 5s | 400 | 10 | 占 5 格；3 阶段 + QTE + 霸体 |
| 105 | 弓箭手 | 8 | 5 | 3.33s | 3 | 0.2 | 40 | 2s | 50 | 10 | 远程，落点随机散布（±1.2） |
| 106 | 固定落点弓箭手 | 16 | 5 | 3.33s | 3 | 0.2 | 40 | 2s | 50 | 10 | 远程，落点按列固定（-2…+2，专用箭矢飞行曲线资产） |
| 107 | boss_wave3 | 500 | 10 | 5.0s | 2 | 0.3 | 100 | 5s | 200 | 10 | 占 5 格；**1 阶段** + QTE |
| 108 | boss_wave6 | 500 | 10 | 5.0s | 2 | 0.3 | 100 | 5s | 300 | 10 | 占 5 格；**2 阶段**（70% 起霸体）+ QTE |
| 109 | 骑兵 | 20 | 10 | 2.0s | 1 | 0.2 | 50 | 5s | 100 | 10 | 骑乘冲锋循环；骑乘免疫眩晕/位移；仅蓄力攻击可打断 |
| 1 | 骷髅兵 | 15 | 10 | 2.0s | 1 | 0.3 | 50 | 5s | 30 | 10 | 历史资产，未进任何波次 |

所有敌人共有的通用字段：受击倍率 1（stab/slash/pierce/sweep/launch/poise 各乘区均为 1）、浮空受伤 ×1.5、击飞参数（重力 20 / 反弹速度 8 / 高度 2.8–3.2）、招架眩晕阈值 `[70% HP → 5s]`（104 为 70%→5s 与 30%→5s 两档）。命中帧在预制体上为空数组，由动画事件/`AttackHitFrameResolver` 驱动。

### 5.2 敌人攻击流程

```
冷却（攻击间隔）→ AttackSpawn 前摇（前移 + 翻面，可被打断）
 → 造成伤害 → AttackDraw 收招（退回 + 翻回，不可打断）→ 冷却
```
敌人只有在 `row < attackRange` 时才进入攻击；进入攻击范围不会自行取消已持有的补齐订单（攻击结束后执行）。

### 5.3 打断体系（三级，来自 `design/attack-interrupt-system.md`）

| 窗口 | 普通敌人 | BOSS |
|---|---|---|
| 普通攻击前摇 | 任何直接伤害可打断 | 任何直接伤害可打断 |
| C 技霸体（CFrame） | 仅 Parry / Launch 可打断 | **仅 Parry** 可打断并削架势（普通伤害为弹刀反馈） |
| 阶段霸体（SuperArmor） | — | **仅 Parry** 可打断并削架势 |
| 收招（AttackDraw） | 不可打断 | 不可打断 |
| QTE 攻击 | 不可打断 | 不可打断（独立系统） |

### 5.4 特殊敌人机制

**103 长枪兵（霸体教学）**：唯一带 `isCAttack` 的普通敌人，前摇期间 `isCFrame=true`（红色描边），只有招架/挑飞能打断。

**105 / 106 远程**：发射 `arrow.prefab`；106 使用逐排飞行曲线资产（`Enemy_106_RowFlight`，逐排时长 0.75–1.35 倍、弧高 0.65–1.2 倍）与固定列落点；105 落点半宽 1.2 随机。远程敌人**也会补齐**（不再被攻击范围否决补齐订单）。

**102 共享血量组**：`shareHealthWithAdjacent=true`，相邻敌人共享血池（`SharedHealthGroup`），打断/伤害需整组结算。

**1021 盾卫（`ShieldGuardEnemy`，最小可玩版）**：
- 按攻击来源方向分四类：盾侧 / 反侧 / 正面 / 未声明。
- 盾侧与正面普通攻击 → **格挡**：固定减伤 10（下限 1 点伤害），播放 `ShieldGuardHit`，伤害数字为蓝灰色，不产生受击动画。
- 可破盾的机制（重攻击）→ 播放 `ShieldBreak`（0.46s）→ 进入**胆怯**形态：受伤 ×1.5、不再攻击，但仍会前进。
- 未实现：跟手精确攻击（连段蓄力派生 `Zhangfei_StabR2`）与命中部位（盾区）判定、独立盾掉落对象。

**109 骑兵（`CavalryEnemy`）**：
- 骑乘期间**只有冲锋循环**：判定 → 前摇（0.3s）→ 前移 → 命中（0.18s）/被打断 → 原路返航（0.22s）。无普通攻击分支。
- 冲锋可从任意排（row ≥ 1）发起；伤害 = `chargeDamage(8) × (发起排 + 1) × 波次攻击倍率`。
- 冲锋路径上遇到敌人 → 停在它后一排前探打击；前方全空 → 冲到 row 0 打玩家。
- 只有标记 `interruptsCavalryCharge` 的攻击（当前 = 蓄力攻击）能打断；**Parry 不打断**；被打断则本次不结算任何伤害。
- 骑乘期间免疫眩晕与位移（击退/横推）；真正被挑飞落入 `Launch()` 即**永久落马**，之后按普通 101 逻辑行动。

### 5.5 BOSS 机制

- **身份**：`Enemy.isBoss=true`，同一套 `Enemy` 框架；属性直接序列化在预制体上。
- **推进**：从后排（row≥3）沿补齐链前移 → 到达 row2 暂停 → 前两排（row0/1）跨全部列清空 → row1 → 1 秒**无敌**缓冲 → `InCombat`。不上 row0。
- **架势（Poise）**：`maxPoise`（104 为 100）；只有挑飞、招架造成架势伤害；架势不自动恢复，**眩晕结束后重置满值**；眩晕期间免疫额外架势伤害。
- **眩晕两条路径**：① 架势归零 → 眩晕 `stunDuration`；② 招架命中且当前血量百分比低于 `parryStunThresholds` 的某一档 → 取最严格档位眩晕（104：70%→5s、30%→5s）。
- **阶段（104 三阶段）**：

| 阶段 | 触发血量 | 霸体 | QTE 权重 | 转阶段 |
|---|---|---|---|---|
| Phase 1 | 100% | 无 | 2 | — |
| Phase 2 | 70% | 有 | 0.5 | 动画触发器 `Phase2`，1.5s |
| Phase 3 | 30% | 有 | 0.5 | 动画触发器 `Phase3`，1.5s |

107 = Phase1（单阶段）；108 = Phase1→Phase2。每阶段可独立配攻击序列、攻击倍率乘区、`actionInterval`、`postQTECooldown`（104 各阶段为 5s）。

### 5.6 QTE 系统（BOSS 专属，✅ 已部署）

组合资产 `BossQTEData`：104 = `[TripleStab, Sweep]`（循环）；Phase1/2/3 各自有 QTE 数据（首 QTE 冷却 1s；基础冷却 Phase1 1s、Phase2/3 3s）。

| QTE 组合 | 槽位 | 类型 | 判定窗 | 成功收益 | 失败代价 | 动画 |
|---|---|---|---|---|---|---|
| TripleStab（防反） | 3 个点击槽 @0 / 0.3 / 0.6s | 点击 | 1s（警示 2s） | 第 1 槽 poise 0、后两槽 poise 20，各 +10 能量 | 15 伤害 | Start / Loop / End，`fixedQteDuration=4.5`，7 箭/波，`cooldownAfterQTE=4` |
| Sweep（横斩格挡） | 1 个滑动槽 | 滑动 180°（容差 180°） | 1.2s（警示 0.8s） | poise 40，+20 能量 | 20 伤害 | Start / Loop / `Blocked`，前进时长 1.4167s，`cooldownAfterQTE=3` |
| Swipe | 1 个滑动槽 | 滑动 0°（容差 60°） | 1.2s | poise 40，+20 能量 | 20 伤害 | 单 clip |

- 104 的 `QTEController` 使用 **Strict 输入规则**：QTE 生命周期内吞掉战斗输入，提前做出的手势判失败，并冻结连击、锁定主动技能栏与大招。
- 防御型 QTE（TripleStab）成功时播放玩家举矛格挡表现（`stabBlockEffectPrefab` 等），失败则受击。
- QTE 结束后回归 Phase 权重循环；`EnemyState.QTEAttacking` 期间可正常造成 HP 伤害但不打断。

---

## 6. 局内成长系统

三套独立奖励系统共存（`design/in-game-growth-system.md`）：

| 系统 | 触发 | 奖励 | 持久化 | 暂停 |
|---|---|---|---|---|
| 总击杀里程碑 | 累计击杀数 | 一次性铜钱/回血 | 铜钱写存档 | 否 |
| 连击 Buff | 连续命中达阈值 | 限时属性 Buff | 否 | 否 |
| 经验三选一 | 经验满级 | 本局永久技能/升级 | 否（失败清空） | 是 |

### 6.1 总击杀里程碑（`KillRewardManager` + `StageConfig.killMilestones`）

`Stage_1` 的配置示例：击杀 5 → 30 铜钱；击杀 20 → 50 铜钱；击杀 50 → 100 铜钱。奖励类型枚举为 `Coin / Heal / RandomUpgrade`，其中 `RandomUpgrade` **未实现**（只打日志）。当前卷轴预览关与 `Stage_Trial` 均无里程碑配置。

### 6.2 连击 Buff（`ComboManager` + `ComboBuffConfig`）

`resetDelay = 3s`、计数模式 `Per Hit`、阈值 **20 连击** → Buff `combo_damage`：`StatModifier{ statId=atk, type=Multiply, value=0.1 }`（伤害 +10%）；同类刷新时长、异类共存，断连统一移除，图标显示在玩家头像左下方。

### 6.3 经验三选一（✅ 已部署）

- **经验来源**：击杀敌人获得 `expReward`（宝石飞向玩家，`ExpGemManager`）。
- **曲线**：`PlayerState.expCurveConfig = ExpCurve_30min`（**60 级**，Lv1 需 200 经验，逐级约 +2%；另有 `DefaultExpCurve` 15 级、100→550）。当前 `currentLevel` 从 0 开始，超出曲线长度即不再升级。
- **触发**：`PlayerState.OnLevelUp` → `UpgradeChoiceManager`：`Time.timeScale = 0` → 抽 3 张卡（`choiceCount=3`）→ 玩家**两次点击同一张卡**确认 → 应用效果 → 若还有待处理升级则**保持暂停继续弹窗**（支持一次经验多级）→ 全部完成恢复 `timeScale=1`。
- **抽取**：`UpgradeChoiceManager.activeSkillPoolConfig`（V2 统一池），按稀有度权重 `Common 0.60 / Rare 0.35 / Legendary 0.05` 掷稀有度，再在同稀有度候选内按权重抽；同一次抽卡不重复；候选不足时降级到其他稀有度补足。
- **过滤**：已达 `maxLevel` 排除；前置条件（`prerequisites`）不满足排除；**被动已满 6 个时不再出现新被动**；主动技能走 `ActiveSkillInventory.CanAcquire`（未持有且槽位未满，或已持有且未满级）。
- **BOSS 掉落**：BOSS 死亡触发 `TriggerItemChoice()`，V2 规则下**只从 Rare + Legendary 的主动技能**中抽 3 张（不含 Common）。

### 6.4 当前 V2 池内容（工程实测，⚠️ 含调试权重）

| 稀有度 | 条目（权重） |
|---|---|
| Common | 专注 100、智慧 100、铁壁 100、地刺 100、火龙舌（主动）100、**染病（主动）300** |
| Rare | 震荡 100、主动冲击波 100、箭雨（主动）100、**蛇形喷射 100000**、疾风 100 |
| Legendary | 旋风（主动）100、拔苗助长 100、海浪（主动）100 |

> ⚠️ **池里存在调试权重**（染病 300、蛇形喷射 100000），且与设计文档的稀有度分配不完全一致（例如设计稿把火龙舌列为 Rare、把蛇形喷射与疾风列为 Common）。讨论新机制前建议先确认这些权重是否为临时值。

### 6.5 被动/数值类条目逐级数值（工程实测）

| 技能 | 稀有度 | 级数 | 逐级数值 |
|---|---|---|---|
| 专注 `cooldown_reduction` | Common | 5 | 主动技能 CD −8/10/10/12/12%（累计 −52%） |
| 智慧 `exp_multiplier` | Common | 5 | 经验 +8/10/12/12/12%（累计 +54%） |
| 铁壁 `charge_damage_reduction` | Common | 5 | 蓄力减伤 15/13/12/10/10%（累计 60%） |
| 地刺 `spike_trap` | Common | 5 | 陷阱伤害 20/35/55/80/110 |
| 疾风 `attack_speed` | Common | 5 | 攻速 +10/12/14/16/18%（累计 +70%） |
| 拔苗助长 `attack_range_boost` | Rare | 2 | 攻击射程 +1/+2 排，该范围伤害惩罚 −30%/−25% |
| 震荡 `charge_hit_shockwave` | Common* | 5 | 蓄力命中附带 1 道/10 伤/2 排 → 3 道/55 伤/5 排；蓄力期间每受 1 次伤害该波伤害 +15%→+25% |
| 蛇形喷射 `passive_timed_aoe` | Common* | 5 | 间隔 15→10s，射程 1→3 排，单股 2→6 伤，灼烧 12→36 总伤 / 6s |

（* 资产内 `rarity` 字段与所在池的稀有度分层不完全一致。）已入池技能均含面板描述模板，UI 会显示逐级效果文本与冷却环。

### 6.6 主动技能（`ActiveSkillDefinition`，✅ 已部署）

| 技能 | 稀有度 | 级数 | 冷却（Lv1→满级） | 效果（逐级） |
|---|---|---|---|---|
| 火龙舌 `FireLine` | Rare* | 5 | 20→10s | 直线火焰 20/30/40/50/50 伤，5 排贯穿；对**灼烧中**目标必定暴击（200%） |
| 箭雨 `ArrowRain` | Rare | 5 | 20→12s | 前方 1→5 排、每波 4→8 箭、每箭 12→28 伤（伤害箭对落点范围内所有敌人分别结算） |
| 主动冲击波 `ChargeAttackShockwave` | Rare | 5 | 12→8s | 点击获得 1 层充能（可叠加）；下一次蓄力攻击每层释放 1 道 2→4 排、8→28 伤的冲击波并消耗全部层数 |
| 海浪 `Wave` | Rare | 3 | 10→6s | 零 HP 伤害击退控场：覆盖前 1/2/3 排，击退固定 1 排；对**未眩晕 BOSS** 改为削 8/10/12% 最大架势并打断 |
| 旋风 `Cyclone` | Rare* | 5 | 10→6s | 零 HP 伤害吹飞 1→3 排；Lv3 起解锁落地伤害 8/12/16；对未眩晕 BOSS 削 12→20% 最大架势 |
| 染病 `Disease` | Rare | 5 | 6→3s | 下一次戳击使目标染病：4–6s 内共 8/16/20/30/36 伤，目标死亡向相邻传播（Lv3 起为智能传播，优先同行最近敌人） |
| 烈焰喷射 `FireAoe` | Rare | 5 | 12→8s | 主动版扇形喷火（10→30 伤，间隔 3→1.5s）；**未入池** |

技能来源：三选一（全池）或 BOSS 掉落（仅 Rare/Legendary 主动）。升级 = 再次选中同技能 +1 级，CD 与效果按等级表刷新。激活：点击 HUD 主动技能槽 → 检查冷却 / `timeScale` / 对话与路线阻塞。

---

## 7. 铜钱经济（道具系统）

### 7.1 道具系统

铜钱是道具系统（`PropSystem`）的第一个道具类型。`PropType` 枚举定义所有道具类型：

```csharp
public enum PropType { Coin = 0, /* 后续扩展: Key, Gem, ... */ }
```

`PropData` 为「类型 + 数量」结构，`SaveData.props` 列表统一管理所有道具。

### 7.2 铜钱获取

- 击杀敌人获得铜钱（Enemy 组件 `coinReward` 字段）
- 通关时获得通关奖励（`StageConfig.clearCoinReward`）

### 7.3 铜钱结算

- 本局铜钱仅记录在 `PlayerState.coinCount`，新关卡从 0 开始
- **结算时机**（`StageController.SettleCoins()`）：
  - **通关胜利**：全部波次清空后结算
  - **返回主菜单**：点击「返回主菜单」按钮时结算（含失败后返回）
- `_coinsSettled` 标记保证同一局只结算一次（幂等）
- 「重新开始」不结算（铜钱丢失）
- 强退游戏（Alt+F4、杀进程）无法触发结算，铜钱丢失

### 7.4 存档

- `SaveData.props` 列表存储所有道具（`List<PropData>`）
- `coinCount` 字段保留向后兼容，`MigrateIfNeeded()` 自动将旧存档迁移到 props 列表
- `SaveManager` 提供通用道具 API：`GetProp(type)` / `SetProp(type, amount)` / `AddProp(type, delta)`
- 便利方法：`GetCoins()` / `SetCoins(amount)` / `AddCoins(delta)` 直接操作 PropType.Coin

### 7.5 数据流

```
Enemy死亡 → PlayerState.AddCoins(enemy.coinReward) → UI更新
通关      → PlayerState.AddCoins(clearCoinReward) → SettleCoins() → SaveManager.SetCoins()
返回主菜单 → SettleCoins() → SaveManager.SetCoins()
```

### 7.6 UI

- Battle 场景显示本局铜钱（`CoinCounterUI`，含跳动+金色飘字动画）
- MainMenu 显示总持有铜钱（`MainMenuUI.coinText`，调用 `SaveManager.GetCoins()`）
- 胜负面板显示本局获得铜钱（`BattleHUD.resultCoinText`）

---

## 8. UI 与 HUD 清单（`Battle.scene`）

| 模块 | 脚本 | 作用 |
|---|---|---|
| HeroHUD（张飞） | `HeroHUD` / `HeroHUDSkin` / `HeroHUDFlipCard` | 血条、经验条、等级、普通攻击按钮与冷却、双面翻牌（正面 HUD / 背面 QTE 看板） |
| 主动技能栏 | `FrontItemBar` | 最多 4 个主动技能槽，图标 + 冷却环 + 倒计时 |
| 被动/Buff 栏 | `BuffDisplayPanel` / `BuffIcon` | 被动图标、计时被动的径向冷却填充与倒计时、DoT 状态 |
| 连击 | `ComboDisplayUI` / `ComboBuffHUD` | 连击数显示与连击 Buff 图标 |
| 铜钱 / 击杀 | `CoinCounterUI` / `KillMilestoneDisplay` / `KillRewardUI` | 本局铜钱、里程碑进度、全屏 KO_50 类战绩图（`GlobalKillDisplayConfig`） |
| 敌人状态 | `EnemyHealthBar` / `BossHealthUI` | 普通敌人血条与 DoT 状态条（宽度 `barWidth=1.0`）、BOSS 血条 + 架势条 + 状态条 |
| QTE | `QTEDisplay` + `QTEIndicators` | 老虎机式滑动入场、填充、放大闪白、判定、退场；成功/失败符号 |
| 伤害数字 | `DamageNumberManager` / `DamageNumber` | 对象池化 TMP 跳字，按强度分级、DoT 单独颜色 |
| 关卡/路线 | `StageProgressBar` / `RouteChoicePanel` / `RouteChoiceButton` | 进度条（含对话入口预留）、路线二选一预览与确认 |
| 弹窗 | `UpgradeChoicePopup` / `UpgradeCard` / `ItemDiscardPopup` | 三选一卡牌、道具栏满时弃置/替换 |
| 菜单 | `PauseMenuUI` / `MainMenuUI` | 三档音量、继续/重开/返回主菜单、选关网格 |
| 对话 | `DialogueManager` / `BattleDialogueView` / `HeroHUDDialogue` | 阻塞式台词、Boss 头像/气泡、双面看板翻转 |

屏幕适配：竖屏，`CanvasScalerAdapter` + `SafeAreaAdapter` + `UIResolutionHelper`（动态 Match + 绝对像素缩放）。

---

## 9. 技术架构与配置资产地图（调参入口）

### 9.1 关键脚本

| 模块 | 文件 |
|---|---|
| 流程 | `Managers/StageController.cs`、`Managers/EnemyManager.cs`、`Wave/WaveSpawner.cs` |
| 阵型/移动 | `Core/ColumnManager.cs`、`Core/Column.cs`、`Core/RowFormation.cs` |
| 玩家 | `Player/InputManager.cs`、`Player/AttackSystem.cs`、`Player/PlayerState.cs`、`MoveSystem/PlayerMoveStateMachine.cs` |
| 特效/判定 | `Attack/StabSweepEffect.cs`、`Attack/SweepEffect.cs`、`Attack/AttackWave.cs`、`Effects/*` |
| 成长 | `Core/UpgradeChoiceManager.cs`、`Core/UpgradeEffectManager.cs`、`Core/ActiveSkill{Definition,Inventory,Runner}.cs`、`Core/ComboManager.cs`、`Core/KillRewardManager.cs` |
| 敌人 | `Enemy/Enemy.cs`（约 4300 行，普通+BOSS 共用）、`Enemy/CavalryEnemy.cs`、`Enemy/ShieldGuardEnemy.cs`、`Enemy/SharedHealthGroup.cs`、`Enemy/BossPhaseData.cs` |
| QTE | `QTE/QTEController.cs`、`QTE/QTEDisplay.cs`、`QTE/QTEAttackConfig.cs`、`QTE/QTEConfig.cs` |
| 路线 | `RouteFake/FakeRouteRuntime.cs`、`RouteFake/FakeRouteTravelPresentation.cs`、`RouteV2/RouteStageRuntimeV2.cs` |
| 存档/道具 | `Core/SaveManager.cs`、`Core/PropSystem.cs` |

### 9.2 配置资产

| 内容 | 路径 |
|---|---|
| 招式/连招 | `Assets/Prefabs/UI/Skills/Zhangfei_*.asset`（基础 6 招）、`Assets/ScriptableObjects/Moves/Zhangfei/*.asset`（链内招式）、`Moves/MoveTable_Zhangfei.asset` |
| 武将 | `Assets/ScriptableObjects/Warrior/Hero_Zhangfei.asset` |
| 大招 | `Assets/Prefabs/UI/Skills/Zhangfei_BerserkUlt.asset` |
| 敌人 | `Assets/Resources/EnemyPrefabs/Enemy_{id}.prefab`（`EnemyPool` 按文件名自动注册，新敌人只需新建预制体） |
| BOSS 阶段 / QTE | `Assets/ScriptableObjects/BossPhases/*.asset`、`Assets/ScriptableObjects/QTE/*.asset` |
| 技能池 / 技能定义 | `Assets/ScriptableObjects/ActiveSkills/ActiveSkillPoolConfig.asset`、`ActiveSkill_*.asset`、`Assets/ScriptableObjects/Upgrades/Definitions/*.asset` |
| 经验曲线 | `Assets/ScriptableObjects/ExpCurve/ExpCurve_30min.asset`（已装配）、`DefaultExpCurve.asset` |
| 连击 | `Assets/ScriptableObjects/Combo/ComboBuffConfig.asset` |
| 关卡/波次 | `Assets/Resources/StageConfigs/*.asset`、`Assets/RouteData/**/Battles/*.asset` |
| 路线 | `Assets/RouteData/ChangbanpoScroll/**`（已部署）、`Assets/RouteData/Stage01V2/**`（RouteStageV2） |
| 阵型 | `Assets/ScriptableObjects/StageFor/DefaultFormation.asset` |
| 箭矢 | `Assets/ScriptableObjects/ArrowConfigs/*.asset`、`ArrowGlobalConfig.asset` |
| 数值总览工具 | `Assets/Scripts/Editor/CombatDataTableWindow.cs`（战斗数值总表 EditorWindow） |

---

## 10. 实现状态清单（讨论新机制时的边界）

### ✅ 已部署并参与正式流程

- 5 列阵型战斗、波次生成、远处入场、整排清空补齐、击退回位、节奏门 999、Boss 墙
- 6 种基础攻击 + 手势识别 + 蓄力分级 + 招式状态机与两条连招链
- 打击反馈（卡肉/镜头/像素特效/伤害数字）、大招狂怒
- 敌人 101/1011/102/1021/103/104/105/106/107/108/109 与 BOSS 框架（阶段/架势/眩晕/打断/QTE）
- 局内成长：经验三选一（V2 统一池 14 个条目）、主动技能（含冷却与专注减 CD）、连击 Buff
- 卷轴节点路线（长坂坡预览）、节点出口确认、旅行演出、存档点与失败恢复
- 铜钱统计与结算、存档迁移、音频三档音量、暂停/返回菜单

### 🟡 已实现但未接入主流程

- `Stage_Trial`（试炼之路）：**唯一的 15 波、PerRow、带递进倍率与 4 个 Boss 波（第 5/8/11/14 波）的测试关**，`clearCoinReward=300`；未挂在主菜单入口上
- `RouteStageV2`：`Assets/RouteData/Stage01V2/Stage01_RouteV2.asset`（A→B/C/D）+ 场景 `Stage01_RouteV2.unity`；主菜单未挂载
- 旧路线系统（`RouteProgressionController` + `Stage01_Route`）与旧路线校验工具：入口已停用
- V1 道具体系：`ItemPoolConfig`、`DropItemPoolConfig`、`ItemInventory`、`ItemEffectRunner`、`Item_HealthPotion` 等（V2 下药水与掉落被跳过）
- 未入池的升级资产：神力、反伤、谋略、捡漏、敛财、连刺、虚幻武器、回旋波、连锁弹射、箭矢齐射、测试资产等
- `Enemy_1`（骷髅兵）、`Enemy_102`（盾兵实验品）

### ⛔ 设计已定但未实现

- 30 分钟正式数值方案（15 波、每波倍率、Boss 血量标尺、技能逐级数值）——`design/numerical-balance-draft.md`
- 长坂坡正式关卡（6 战斗节点 + 情景节点 + 分支 + 10 分钟内节奏）
- 局外成长与天赋树、可存档 Build 方向（`design/out-of-game-growth-system.md`）
- BattleEntry 条件系统、剧情/剧情选项与快照、跳过演出
- `KillRewardType.RandomUpgrade`、道具池的 Boss 锦囊（V1 遗留）
- 招式图后续内容：招架成功态专属连招（P3）、局外解锁边/节点（P4）
- Boss 内容补齐：107/108 独立外观与阶段演出、Boss 专属技能演出

---

## 11. 已知问题与风险

### 现象层（`memory/known-issues.md`）

1. BOSS 在 QTE 动画期间受到道具等伤害会播放受击动画，打断 QTE 动画（可能引发问题 2）。
2. BOSS 连续触发 QTE 但不播放对应动画（疑似由问题 1 引起）。
3. 攻击特效/伤害数字残留：未消失时进入 BOSS 战会残留。
4. BOSS 墙壁保护不完全：普通敌人可能被击退到 BOSS 身后，导致玩家打不到而卡关。
5. 被击退超出攻击范围的敌人仍在超范围攻击且不补齐。

### 数值/一致性观察

- 基础招式数值存在**倒挂**：Slash（8）低于 Stab（20），Launch 仅 2，与「段位越深越强」意图相反。
- 101 血量 200 + 攻击力 3 与早期文档（HP 16 / 攻击 10）完全不同：**预制体是唯一事实来源**，旧文档数字不可引用。
- 池内调试权重（染病 300、蛇形喷射 100000）会强烈扭曲三选一体感。
- HUD 冷却显示在动作锁模式下不准（读的是独立 CD 口径）。
- `WaveConfig.isBossWave` 字段存在但运行时未使用；104/107/108 的差异实际来自预制体本身。

### 手感/表现层（待实机验收）

- 连招手感（窗口起止、卡肉对墙钟时长的影响）、招架/挑飞时序、骑兵冲锋观感等需要实机试按确认。
- 卷轴旅行的连续环境过渡（分段映射）尚不能宣称完成。

---

## 12. 设计约束与扩展点（新机制讨论用）

### 12.1 新机制必须遵守的硬约束

1. **位移只能走既有通道**：`ColumnManager.ApplyPushWave` / `PostDisplacementFillUp`，并满足不重叠、未受位移者不动、原槽回位、订单 owner/generation。
2. **不要新增独立移动路径**：任何敌人位置变化必须属于补齐 / 击退回位 / 横向位移 / Boss 推进 / SpawnEntry 之一。
3. **空挥不结算资源**：新增招式/被动不能改变「空挥不消耗冷却、不给大招能量、不触发攻击计数被动」的现有契约。
4. **伤害与打断参数逐段透传**：`canInterruptCFrame`、`interruptsCavalryCharge`、`poiseDamage` 必须从招式资产传到命中判定。
5. **引用必须 Inspector 可追踪**：禁止字符串 id 查找、静态缓存、`Resources.Load` 取运行时数据（见 `design/anti-ghost-reference.md`）。
6. **DoT 不卡肉**，且卡肉不冻结玩家（只有暂停/QTE/三选一/对话会动 `timeScale`）。
7. **局内三选一改规则、不改纯数值**；局外成长禁止纯数值提升。
8. 节点切换、死亡、暂停、对话、QTE、大招演出、三选一弹窗期间，招式状态机停止推进并清空输入缓冲。

### 12.2 已预留的扩展点

| 想加的东西 | 现有接入口 |
|---|---|
| 新招式/新连招 | 新建 `AttackSkillConfig` 资产 + `moveEdges` 引用 + 登记到 `design/combo-registry.md`（命名与 id 区间有规范） |
| 新敌方单位 | `Assets/Resources/EnemyPrefabs/Enemy_{id}.prefab`（自动注册）+ 可选专用组件（如 `CavalryEnemy` / `ShieldGuardEnemy`）+ 波次 `enemyIds` 填 id |
| 新主动技能 | `ActiveSkillEffectType` 枚举 + `ActiveSkillRunner` 分支 + `ActiveSkillDefinition` 资产 + 入池 |
| 新被动/升级 | `UpgradeDefinition`（数值型 / AttackPassive / TimedPassive / ActiveSkill）+ 前置条件 + 池权重 |
| 新 BOSS 阶段 | `BossPhaseData` + `BossQTEData` 资产，预制体上按序引用 |
| 新 QTE | `QTEAttackConfig`（多槽 + 动画三件套 Start/Loop/End/Blocked）+ `QTEConfig`（点击/滑动、判定窗、成败效果） |
| 新节点/分支 | `FakeRouteNodeConfig` + `FakeRouteChoiceConfig` + `Travel` 资产；节点一律「抵达 → 内容 → 出口确认 → 旅行 → 提交」 |
| 新关卡数值 | `StageConfig` + `WaveConfig.rows`（`0` 空槽 / `999` 节奏门）+ 逐波倍率 |

### 12.3 一句话提醒

当前游戏的**内容量远小于系统能力**：系统（阵型/连招/BOSS/QTE/主动技能/路线）已经能跑完整链路，但正式关卡只有「预览脚手架」（每战 1 排 1–3 个敌人）。与新机制对话时，优先确认三件事：这机制是否属于既有五类位移/打断/成长通道；它的数值是否与「101 HP200 / Stab 20」这一当前量级一致；它是否要求改动上述不可变约束。

