---
id: kd_a01d540b-2822-4970-9c21-5fbed6a73304
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 连招系统：招式转移图与动作状态机

连招系统的权威设计文档。模型为「招式转移图」（动作状态机），取代早期的「前置段数 × 终结手势」矩阵设想；涵盖三阶段招式、接续窗口、上下文状态、输入模型 A/B 对比、局内外解锁、测试与验收要求。

## 概述

连招系统的定位不是「附加功能」，而是**战斗主干**。当前局内三选一（召唤物、落雷、喷火等）与被动数值过强，玩家的最优解偏向站桩放技能，动作表达被架空。改为招式驱动后：

- 玩家成长的主体 = **出招表能被用出来多少**（节点、边、窗口、属性挂点）
- 局内三选一 = **改写出招表规则**（属性挂边、窗口放宽、收尾缩短、节点替换），而不是加数值
- 局外天赋 = **解锁边或节点**，是「高段位招式」的天然门槛

---

## 1. 术语

| 术语 | 含义 | 与现有概念的关系 |
|------|------|------------------|
| 连招 / 招式转移图 | 输入序列 → 后继招式的有向图 | 本设计 |
| 节点 / 招式（Move） | 一个可执行的招式，内部有既有的 起手→命中→收尾 视觉序列 | 对应现有 `AttackSkillConfig`，需扩展 |
| 边（Edge） | （输入手势 + 有效窗口）→ 后继节点 | 新增 |
| 伤害判定窗口 | 视觉效果命中目标的时段，由特效组件按时序驱动 | **已存在**（`StabSweepEffect` / `SweepEffect`） |
| 动作锁 | 玩家不能进行其他动作的时段，等于整条序列长度 | **已存在**（`AttackSystem._actionLockTimer`） |
| 接续窗口 / 连招输入窗口 | 允许接下一招的时段，锚定在同一条序列的百分比区间上 | **新增**（本设计） |
| 上下文状态 | 招架成功、击飞等临时状态，替换/追加解析表 | 新增 |
| **连击（Combo）** | 命中计数与阈值 Buff | `ComboManager` 现有系统，**与本设计无关** |
| 出招表 | 转移图的策划可读视图（接续表） | Editor 工具 |

命名注意：`Assets/ScriptableObjects/Combo/` 当前已被命中连击的 `ComboBuffConfig` 占用；新资产请另建目录，避免混用。

---

## 2. 现有实现实测基线

（读取自磁盘 YAML 与源码，用于说明问题量级）

| 项目 | 值 | 来源 |
|------|-----|------|
| 攻击类型 | `Stab / Slash / Pierce / Sweep / Launch / Parry` + `Ultimate` | `Assets/Scripts/Player/PlayerState.cs` `AttackType` |
| 输入映射 | 点击→Stab；长按蓄力→Pierce；竖滑→Launch；横滑→Sweep；斜滑→Slash；按住竖滑→Parry | `Assets/Scripts/Player/InputManager.cs` |
| 点击手势判定时机 | 抬手时（`TouchPhase.Ended`） | `InputManager.cs` `ProcessGesture` 调用点 |
| 滑动招式前置条件 | 必须先按住蓄力门（场景实测 1.0s，见 2.1） | `InputManager.cs` `TryConsumeLiveGesture` |
| 未蓄满的快速滑动 | 不产生任何攻击（抬手分支只处理「蓄满长按」与「未滑动的点击」） | `InputManager.cs` `ProcessGesture` |
| 滑动参数 | 阈值 DPI 自适应（场景实测 50px，钳制 30–150）、须 0.25s 内完成、最低速度 180px/s、重新识别间隔 0.1s | `InputManager.cs` |
| 动作锁 | Stab `actionDuration` 0.45s、Slash 0.5s；场景中 `useActionBasedCooldown = true`（动作锁为**当前实际运行模式**），锁期间 `TryExecuteAttack` 直接返回 false，**输入被丢弃且无缓冲** | `Assets/Scripts/Player/AttackSystem.cs`、`Zhangfei_Stab/Slash.asset` |
| 伤害结算方式 | **时间分辨**：由特效组件按视觉时序逐个目标结算，不是一帧全结算 | `Attack/StabSweepEffect.cs` `CheckHits`、`Attack/SweepEffect.cs` `CheckHitThresholds` |
| 动作锁与序列的关系 | `GetAttackDuration(cfg)` 同时作为 `targetDuration` 传入特效，**动作锁与视觉序列 1:1 对齐** | `AttackSystem.cs` `ExecuteStab` / `ExecuteSlash` |
| 命中卡肉 | 只暂停特效序列（`seq.Pause()` + `WaitForSecondsRealtime`），**不改 `Time.timeScale`**，因此动作锁会先于视觉走完 | `StabSweepEffect.cs` / `SweepEffect.cs` `PauseSequenceForHitStop` |
| 招式配置归属 | `HeroConfig.skillConfigs` 与 `AttackSkillConfig`，**同一个 `AttackType` 只能有一个配置** | `Assets/Scripts/Core/HeroConfig.cs` |
| 现有武将配置 | 仅张飞一个（`Assets/ScriptableObjects/Warrior/Hero_Zhangfei.asset`） | — |
| 数值观察 | 张飞 Slash 伤害 8，低于 Stab 伤害 20 | `Zhangfei_Slash.asset` / `Zhangfei_Stab.asset` |

### 2.1 实时核实（编辑器内读取，覆盖磁盘默认值）

以下为在 Unity 编辑器内读取**场景实例实际值**的结果；与 C# 字段默认值不一致时，以本节为准。

| 项 | 实测值 | C# 默认值 | 说明 |
|----|--------|-----------|------|
| `InputManager.minChargeTime` | **1.0s** | 0.5s | 蓄力门比默认严一倍，连招节奏问题更严重 |
| `InputManager.longPressDuration` | 0.3s | 0.3s | 一致 |
| `InputManager.swipeThreshold` | **50px**（DPI 钳制后） | 30px | `Awake` 按 DPI 缩放并钳制到 30–150 |
| `AttackSystem.useActionBasedCooldown` | **true** | false | 动作锁为当前实际运行模式 |
| `AttackSystem.parryProjectileRange` | 10 | 4 | 场景覆盖 |
| `AttackSystem.pierceTimeScale` | 1 | 3 | 观察慢放已调回原速 |

张飞实际装配的招式配置（`Hero_Zhangfei.asset`）：

| 招式 | damage | cooldown（独立CD） | actionDuration | rangeRows |
|------|--------|--------------------|----------------|-----------|
| Stab | 20 | 0.3 | 0.45 | 1 |
| Slash | 8 | 0.65 | 0.50 | 1 |
| Pierce | 20 | 3.0 | 0.50 | 3 |
| Sweep | 5 | 2.0 | 0.55 | 3 |
| Launch | 2 | 0.5 | 0.60 | 1 |
| Parry | 0 | 0.5 | 0.20 | 1 |

大招配置为 `Zhangfei_BerserkUlt.asset`。这些配置资产放在 `Assets/Prefabs/UI/Skills/`，目录名有误导，里面实际是 `AttackSkillConfig`。

两个数值观察：Slash（8）低于 Stab（20），Launch 只有 2，Parry 为 0 —— 与「段位越深越强」的设计意图明显不符，需在内容阶段一并重做。

**反向依赖已核实**：这 6 个配置资产**只被 `Hero_Zhangfei.asset` 引用**，Prefab / Scene / HUD 均无引用（已用 `AssetDatabase.GetDependencies` 全量扫描 210 个资产确认）。因此「取配置 key 从 `attackType` 改为 `moveId`」只影响 §14 列出的 4 个代码调用点，UI 侧无额外耦合。

**结论**：现状做一次「点击→点击→斜滑」需约 2.7s 以上（两次点击各含抬手延迟，且滑动需额外按住 1.0s），且过程中任何提前输入被静默丢弃。这是连招手感无法成立的直接原因。

---

## 3. 核心模型：招式转移图

```
节点 = 招式（内部已有 起手 → 命中 → 收尾 的视觉序列）
边   = （输入手势 + 接续窗口）→ 后继招式
```

### 3.1 窗口划分：复用已有序列阶段，只新增输入窗口

游戏里存在三个不同的「窗口」，此前文档把它们混称「判定帧」，造成歧义。三者必须分开：

| 窗口 | 层面 | 现状 |
|------|------|------|
| 伤害判定窗口 | 命中 | **已存在**，由特效组件按视觉时序驱动 |
| 动作锁 | 玩家操作 | **已存在**，时长 = 整条序列长度 |
| 连招输入窗口 | 输入接续 | **不存在**，本设计要新增的就是它 |

**伤害判定窗口已经存在，且已经是时序化的：**

- `StabSweepEffect` 在刺出/穿入阶段按**枪尖推进距离**逐个命中（`CheckHits` 比较枪尖投影距离与敌人排距离，容差 0.35）。
- `SweepEffect` 在横扫推进过程中按每个敌人的 `xThreshold` 逐个命中（`CheckHitThresholds` 在移动 tween 的 `OnUpdate` 里调用）。
- 两者均在首次命中时暂停序列做卡肉。

**动作锁与视觉序列已经 1:1 对齐：** `GetAttackDuration(cfg)`（= `actionDuration / 攻速`）既作为动作锁时长，又作为 `targetDuration` 传入特效组件，特效按固定比例切分自己的内部阶段：

```
Stab 序列： 起手 12% → 刺出 28% → 穿入 8% → 回收 52%
Slash 序列：挥砍（总长 - 惯性 - 淡出）→ 惯性 → 淡出
```

因此**不需要把 `actionDuration` 拆成「起手 + 收尾」，也不需要新建判定帧**——起手与收尾在特效序列里早就存在（只是叫 windup/retract）。要新增的只有「连招输入窗口」，且它应锚定到同一条序列的百分比区间上。

### 3.2 边与接续窗口

- 边只在**接续窗口**内有效。窗口以**同一条视觉序列的百分比区间**配置（例如 Stab 的「回收段 10%–80%」），不使用独立秒数。
- 这样做的三个好处：① 窗口天然与视效同步；② 攻速提升会压缩整条序列，窗口按比例自动跟随；③ 命中卡肉暂停序列时窗口可一起冻结，不会错位。区间需要逐招式慢慢微调，不追求一次到位。
- 输入到达时的处理顺序：① 当前节点的显式边 → ② 节点的默认重复规则 → ③ **什么都不发生**（不回落出中立招式），当前招式走完收尾回到中立。
- 窗口内的提前输入进入**输入缓冲**，窗口打开时生效；超出窗口的丢弃。

### 3.3 段数上限与循环

**不设全局上限。** 链长 = 图深度 = 配置的节点数量，由策划决定。代码只提供容器与编辑器校验。

**允许成环**，当前连续 Stab 本质就是一条自环。为保持改造前后行为等价，每个节点默认保留「同手势重复」规则：

- 节点记录 `entryInput`（进入本节点所用的手势）；默认该手势可重复，即 `Stab → Stab → Stab`。
- 默认重复**只作用于 `entryInput`**，其他输入必须显式配边，避免未解锁的边被自环误命中。
- 想实现无双式「串尾终止」，在该节点关掉重复开关即可，不需要额外机制。

### 3.4 终止条件

当前节点没有任何可进入的后继时，招式自然收尾回到中立——即「动作状态机一直走下去，直到没有动作进入收尾」。

串尾不需要额外的惩罚机制：关掉重复开关后，末段节点的收尾本身就是终结硬直；若希望「反无脑连打」的力度更强，把末段节点的收尾配长即可。

### 3.5 示例形态（张飞）

| 当前节点 | 有效窗口 | 输入 | 后继 |
|----------|----------|------|------|
| 中立 | — | 点击 | 枪突1 |
| 中立 | — | 斜滑 | 斩击（现 C1） |
| 枪突1 | 判定后段 ~ 收尾 | 点击 | 枪突2（动作/伤害/位移独立配置） |
| 枪突1 | 同上 | 斜滑 | 斩·二连 |
| 枪突1 | 同上 | 竖滑 | 挑·二连 |
| 枪突2 | 同上 | 点击 | 枪突3 |
| 枪突2 | 同上 | 斜滑 | 斩·三连 |
| 枪突3 | — | （无配置） | 收尾 → 中立 |
| 招架成功态 | 3s | 斜滑 | 招架反击斩 |
| 招架成功态 | 3s | 点击 | 招架回刺 |

### 3.6 全招式可作前置

任何招式都能作前置（不限于轻攻击），因此每个节点理论上可配置 6 输入 × 6 后继。配置量必须靠工具控制，不能靠手填引用：

- Editor 呈现为**接续表**：每行一个节点，列出其边（输入 / 窗口 / 后继），空 = 不可接续。
- 校验规则：允许成环（例如显式配置的无限连段），但对「存在不可终止子图」必须警告。

### 3.7 接续窗口的默认派生与手动覆盖

窗口**既可手动配置，也在创建节点时给一个合理默认值**，不强制手填。默认值只作起点，最终以实机手感逐招式微调。

默认派生（按各招式现有特效序列的阶段比例自动计算）：

| 招式 | 现有序列阶段 | 默认窗口 | 依据 |
|------|--------------|----------|------|
| Stab | 起手 12% / 刺出 28% / 穿入 8% / 回收 52% | 48%–95% | 命中窗口结束时开窗（枪尖已到位），留 5% 余量避免窗口跨进下一招起手 |
| Slash | 挥砍 ≈70% / 惯性 ≈12% / 淡出 ≈18% | 65%–92% | 横扫后段开窗，覆盖惯性段，止于淡出中段 |
| Pierce / Sweep | 出手视觉 + 离手飞行物 | 60%–95% | 通用兜底 |
| Launch | 蓄势 + 上挑 | 70%–95% | 上挑判定完成后开窗 |
| Parry | 扫掠 | 60%–95% | 本身是防御动作，窗口主要服务「招架成功态」入口（第 6 节） |

通用兜底公式：起 = 命中窗口结束比例 × 0.8，终 = 95%。

三条约束：

- **窗口起点默认不早于命中窗口结束**，即默认不允许「取消掉还没落地的伤害」；需要时显式放开。
- **最小绝对时长下限**：窗口按百分比随攻速缩放，需保证 ≥ 0.06s，否则高攻速下窗口趋近 0，连招变成不可能。
- 默认值随节点创建时自动生成，后续可逐招式覆盖；两者共存，不做成「必须手填」。

---

## 4. 与真·三国无双 3–5 的对照

| 无双 3–5 | 可学点 | 本项目对应 |
|----------|--------|------------|
| 出招表是独立格子，每格一套数据 | 招式即自包含数据包 | `MoveDefinition` |
| `C_n = (n-1)方 + 蓄力` | 终结技身份由前置决定 | 图上的边 |
| C 技语义分工（挑空/多段/范围/浮空） | 每格有明确功能定位 | Launch=挑空、Sweep=范围、Slash=斩击、Pierce=贯穿 |
| 普攻串不循环，收尾硬直后回起始态 | 用动作成本而非数值惩罚无脑连打 | 无配置后继 → 收尾 → 中立 |
| 高段位 C 技需要武器进化 | 门槛式解锁 | 局外天赋解锁边/节点 |
| 每段普攻动画/伤害/位移独立 | 才有「每下 stab 不一样」 | 枪突1/2/3 拆为独立节点 |
| 取消窗口 + 预输入 | 手感命脉 | 接续窗口 + 输入缓冲 |
| 空挥也推进段数（输入驱动） | 否则面向空列永远连不上 | 段位推进与「命中才消耗冷却」解耦 |
| 属性只挂特定段位 | 精确的强度分配 | 三选一效果挂边 |
| （本轮排除）无双槽、跳跃攻击、骑乘、空中连段 | — | 大招能量已存在；空中连段留扩展位 |

注意：无双是双按键、零延迟按钮输入；本项目是手势输入，识别本身有延迟与歧义，因此第 5 节的输入模型是**本项目独有**的设计问题。

---

## 5. 输入模型 A / B

### 5.1 A（抬手判定，现状）

招式发生时刻 = 手指离开屏幕。抬手时才知道这次是点击还是滑动，因此按下瞬间无法出招。

- 轻攻击固有延迟 ≈ 80–150ms（一个完整点击周期）。
- 连打必须由玩家自行卡节奏，动作锁期间输入被静默丢弃。
- 优点：手势语义清晰，不需要「先出招再改写」的补救逻辑。

### 5.2 B（按下即出轻攻击 + 起手取消窗口）

招式发生时刻 = 手指按下，立即进入轻攻击起手。

- 起手结束（判定开始）之前，手指移动超过阈值 → 把这次输入**改写**为手势招式，不会留下一记误伤的轻攻击。
- 未移动 → 起手走完 → 判定 → 收尾，正常出招。
- 延迟 ≈ 0–16ms，与按钮游戏同级；天然形成「点一下 → 紧接着滑动」的连招节奏，且那一下轻攻击是真实存在的。

### 5.3 三个必须分离的轴

手感问题由三个独立原因造成，A/B 只是其中之一：

| 轴 | 现状 | 影响 |
|----|------|------|
| 1. 判定时机 | 抬手 | 轻攻击延迟、连打节奏 |
| 2. 蓄力门 | 所有滑动招式需按住 1.0s（场景实测） | 终结手势的节奏被打断 |
| 3. 输入缓冲与取消窗口 | 动作锁期间输入被丢弃 | 连打掉输入、无法提前接招 |

只改轴 1 而保留轴 2，终结手势仍要按住 1.0s，连招节奏依旧断裂。三者需要**独立开关**，以便分别测量手感贡献。

### 5.4 结论

采用 B，但**分两步**：

1. 招式分阶段（起手/判定/收尾）+ 接续窗口 + 输入缓冲。此步在 A 模型下即可运行完整状态机。
2. 轻攻击改为按下即出 + 起手取消，同时处理蓄力门问题。

---

## 6. 上下文状态

第一版只做**招架成功态**。击飞/空中态不是本期需求，仅保留机制上的可扩展性，不预先设计。

```
Parry 判定成功
  → 进入「招架成功态」（持续 N 秒）
  → 该状态下中立节点的解析表被替换 / 追加
  → 专属连招的边仅在此状态有效
  → 超时或受击 → 退出状态
```

前置需求：

- 需要 `OnParrySuccess` 事件。现有 `ExecuteParry` 在「反弹飞行物」与「打到敌人」两种情况下都返回成功，而「格挡成功」应更严格地指挡住敌方攻击或飞行物，需要区分「招架命中敌人」与「招架住攻击」。
- 同一机制将来可复用于其他状态（例如击飞），但不在本期范围内。

---

## 7. 数据与配置结构

```
MoveTable（每武将一份）
 ├─ nodes: [MoveDefinition ...]        每个节点自带后继边列表
 ├─ window: 接续窗口起止百分比
 └─ states: 上下文状态 → 解析表覆盖

MoveDefinition（可插入任意位置的招式包）
 ├─ 表现：出手视觉 + 飞行物预制体 + 卡肉 / 镜头 / 音效
 ├─ 判定：伤害 / 削韧 / 范围 / 伤害类型
 ├─ 位移：自身位移或击退
 ├─ 时长：起手 / 判定 / 收尾
 └─ edges: [（输入手势, 窗口, 后继节点）...]
```

两条约束：

1. **招式内不含触发条件**。招式不知道自己是第几段，只接收「目标列 / 朝向 / 上下文」。
2. **必需的结构改动**：现在 `AttackSystem.TryExecuteAttack(attackType)` 用 `switch(attackType)` 硬分发，且 `HeroConfig.GetSkillConfig(attackType)` 按类型取配置——**同一 `AttackType` 只能有一个配置**。要实现「每下 stab 不一样」「同一手势在不同段位不同招式」，取配置的 key 必须从 `attackType` 改为 `moveId`。现有 `_unlockedAttacks`（`Dictionary<string, AttackSkillConfig>`，当前无调用方）是该注册表的雏形，可改造复用。

---

## 8. 局外解锁

- **解锁粒度为边或节点**：天赋节点解锁「某个后继招式可用」。
- 未解锁时该边不存在；由于默认重复规则只作用于 `entryInput`，该输入既不匹配边也不匹配默认规则 → 什么都不发生，**不需要额外的回落逻辑**，行为天然一致。
- 需要持久化已解锁的边/节点 id 集合；`SaveData`（`Assets/Scripts/Core/SaveManager.cs`）需新增字段并沿用现有 `MigrateIfNeeded` 迁移模式。
- 天赋树本身尚未实现（`design/out-of-game-growth-system.md` 中为 P3），因此连招系统必须自带可切换的解锁来源，避免被阻塞。

---

## 9. 局内升级的改写点

| 改写类型 | 说明 |
|----------|------|
| 属性挂边 | 「斩·三连附带火」——属性只在该边生效 |
| 窗口改写 | 接续窗口放宽、收尾缩短 |
| 节点替换 | 把某条边指向强化版节点 |
| 新增边 | 解锁新的后继分支 |

与 `design/out-of-game-growth-system.md` 的「禁止纯数值提升」约束一致：改的是机制与规则，不是伤害数字。

---

## 10. 测试、调试与验收

### 10.1 第一步改造（零新内容）如何算「跑通」

不配置任何边时，每个招式退化为「中立 → 招式 → 收尾 → 中立」的单节点图，行为应**与现状等价**。验收判据：

1. **回归等价**：改造前后用同一套脚本化输入回放，记录「触发的招式序列 + 每招命中/伤害/冷却起始时刻」，逐条比对，差异为 0（我们有意变更的项除外）。
2. **阶段可观测**：调试 HUD 显示当前节点、所处阶段、阶段进度、窗口剩余。能看到「起手 0–0.2s / 判定 0.2–0.3s / 收尾 0.3–0.45s」即为分阶段生效。
3. **接续表为空时行为不变**：包括连续 Stab 连打（默认自环）在内的现状行为保持一致。
4. **配置一条边即生效**：例如 `枪突1 --点击--> 枪突2`，能观察到第二下点击接在收尾内且不再是同一个枪突1。
5. **输入缓冲可观测**：窗口内的提前输入被记录为「已缓冲、窗口打开时执行」，而不是被丢弃。

### 10.2 手感如何验证

手感无法自动化，因此需要**运行时开关**（Inspector 字段或调试热键），可现场切换：

- 轴 1：抬手判定 ↔ 按下即出
- 轴 2：蓄力门开 ↔ 关
- 轴 3：输入缓冲开 ↔ 关

三个开关的任意组合都要能跑。这样「哪种手感更好」由实机试按决定，而不是靠讨论。

### 10.3 工具

- **状态机调试器**：当前节点 / 阶段 / 可用边 / 窗口剩余；支持从任意节点起跑、一键执行任意边。
- **窗口对齐视图**：在动画/特效时间轴上叠加显示接续窗口区间，供逐招式微调（见 3.2）。
- **接续表 Editor**：每行一个节点，列出输入 / 窗口 / 后继；空置校验与不可终止子图警告。
- 项目已有「战斗数值总表 EditorWindow」可扩展承载接续表。

---

## 11. 实现方案（P1：零内容等价改造）

目标：装上状态机、窗口、输入缓冲与调试工具，**不配置任何边**，行为与现状等价。

### 11.1 新增类型

| 类型 | 职责 |
|------|------|
| `MoveDefinition`（ScriptableObject） | 招式节点：`moveId`、显示名、`AttackSkillConfig` 引用（复用现有资产）、窗口覆盖、`repeatSelf` 开关、后继边清单 |
| `MoveEdge`（Serializable） | `gesture`（点击/斜滑/横滑/竖滑/长按）、`windowStart01` / `windowEnd01`、`next`（`MoveDefinition` 引用） |
| `MoveTableConfig`（ScriptableObject） | 每武将一份：中立态可用的招式（roots）、上下文状态表、默认派生参数 |
| `PlayerMoveStateMachine`（MonoBehaviour） | 当前节点、阶段时钟、窗口判定、边解析、输入缓冲、默认重复规则；对外提供 `NotifyInput(...)` 与状态事件 |
| `GestureInput`（struct） | 手势类型 + 目标列 + 朝向 + tilt + charged + 时间戳 |

### 11.2 改造点

| 位置 | 改动 |
|------|------|
| `InputManager` | 6 个 `TryExecuteAttack` 调用点全部收敛为「向状态机投递手势」；手势识别逻辑本身不动，保证 P1 等价 |
| `AttackSystem` | `TryExecuteAttack` 保留为底层执行接口（状态机、QTE、解锁攻击、道具共用），P1 不改其内部分支 |
| `HeroConfig` | 保留 `GetSkillConfig(attackType)`；新增按 `moveId` 解析的路径 |
| `PlayerState` | 冷却语义 P1 不动；仅将动作锁剩余暴露给状态机 |
| 调试面板 | 当前节点 / 阶段百分比 / 窗口区间 / 缓冲内容 |

### 11.3 阶段时钟与卡肉（已确认可改 `HitFeedbackManager`）

现状：动作锁用 `Time.deltaTime` 递减；特效序列是 DOTween，卡肉时被 `seq.Pause()`。

- 状态机阶段进度优先读特效序列进度，读不到时回落到 `actionDuration` 派生。
- 在 `HitFeedbackManager.Trigger` 增加卡肉广播（唯一入口），供状态机冻结阶段时钟与窗口计时。广播语义：
  - 带 `duration`（由 `GetHitStopDuration(strength)` 得出）。
  - `HitFeedbackStrength.None`（DoT 等）不广播，避免持续伤害把连招时钟冻住。
  - 连续命中可叠加延长（取剩余时间与新时长的较大值）。
  - 状态机按「当前卡肉剩余时间」冻结阶段时钟；不改变敌人与特效自身的卡肉实现。

### 11.4 P1 明确不做

不配置任何边；不新增招式；不改手势识别与蓄力门；不改冷却、伤害与数值；不改输入模型（仍为 A）。

---

## 12. 改造前必须保住的现有功能（回归清单）

原则：状态机只接管「输入的解析与段位推进」，**不接管**下面这些既有职责。重构后逐条回归。

### 12.1 输入层

| 功能 | 证据 |
|------|------|
| 六种手势映射（点击 / 长按 / 竖滑 / 横滑 / 斜滑 / 按住竖滑） | `InputManager.ProcessGesture` / `ProcessSwipeGesture` / `TryConsumeLiveGesture` |
| 蓄力门（0.3s 长按判定、0.5s 蓄力满）与蓄力姿态接管 | `InputManager.longPressDuration` / `minChargeTime`、`ChargeStabVisual` |
| 蓄力事件链有 **5 个订阅方**，通道不能断 | `ChargeStabVisual` / `PierceAimIndicator` / `ThornArmorEffect` / `PlayerState` / `ChargeIndicatorController` |
| `OnAttackExecuted` 只有 **1 个订阅方**（蓄力姿态复位） | `InputManager` 事件、`ChargeStabVisual.OnAttackExecuted` |
| 蓄力减伤护盾（每次蓄力仅一次） | `PlayerState.OnChargeUpdated` → `TryGrantShield` |
| 三道输入门：`skillInputEnabled` / `gameplayInputEnabled` / `blockInputFrames` | `InputManager.Update` 早退 |
| UI 之上的触摸不处理 | `EventSystem.IsPointerOverGameObject` |
| 防连触发与分段重置 | `swipeRearmDelay` / `hasTriggeredDuringHold` / `ResetSegment` / `CancelChargeAndResetSegment` |
| 屏幕坐标 → 列映射与兜底 | `GetColumnFromScreenPosition` / `FallbackGetColumn` |
| QTE 优先消费输入（Strict / 普通两套规则） | `TryConsumeStrictQTEInput` / `TryConsumeQTEInput` |
| Stab 额外的视觉门 | `AttackSystem`：`_stabVisualTimer` |

### 12.2 招式执行与资源结算

| 功能 | 证据 |
|------|------|
| 空挥也播特效，但**不消耗冷却、不给能量、不触发被动** | `StabSweepEffect` 无目标也生成；首次命中才 `AddEnergyForAttack` / `OnAttackPerformed` |
| 首次实际命中才结算能量与被动计数 | `AttackSystem.TryExecuteAttack`、`ExecuteStab` 的 `onFirstHit` |
| `LastStabTargetEnemy`（被动定位用） | `PassiveTriggerModule` 读取 |
| 伤害公式：`GetFinalDamage`（× 伤害倍率）与 `GetAttackRangeDamagePenalty`（射程衰减） | `AttackSystem` |
| 打断参数透传：`interruptsCavalryCharge`（含 `isCharged` 分支）/ `canInterruptCFrame` / `isParryInterrupt` | `InterruptsCavalryCharge`、各 `Execute*` |
| 位移与补齐通道：戳击击退波、斩击方向性击退、`ApplyPushWave`、`PostDisplacementFillUp` | `AttackSystem.ApplyStabPushWave` / `ApplySlashDirectionalPush` |
| 幻影攻击 / 折返波 / 连锁弹射 | `ExecutePhantomAttack` / `ExecuteReturnWave` / `ExecuteChainBounce` |
| 解锁攻击注册表（现无调用方但已实现） | `_unlockedAttacks` / `TryExecuteUnlockedAttack` |
| 强制戳击接口 | `ForceExecuteStab` |

### 12.3 表现与打击反馈

| 功能 | 证据 |
|------|------|
| 每招独立特效组件（序列内命中、卡肉、命中脉冲、运动模糊、拖尾） | `StabSweepEffect` / `SweepEffect` / `AttackWave` / `LaunchVisualEffect` / `ParryVisualEffect` / `PierceReleaseVisual` / `ChargeStabVisual` |
| 卡肉分级：Light 0.045s / Standard 0.09s / Heavy 0.14s | `HitFeedbackManager` |
| 卡肉作用域：敌人动画 + 特效序列 + 飞行物；**DoT 不卡肉** | `Enemy.ApplyHitStop`、各特效 `PauseForHitStop`、DoT 用 `HitFeedbackStrength.None` |
| 镜头与像素特效反馈 | `CameraFeedbackController.RequestHit`、`PixelHitEffectManager.RequestHit` / `AttachSlashTrail` |
| 音效事件 | `Player_Attack` / `Stab_Hit` / `Slash_Hit` / `Player_Parry` |

### 12.4 系统联动

| 功能 | 证据 |
|------|------|
| 大招能量（按招式配置 `ultimateEnergyGain`）与演出期间禁用输入 | `UltimateSystem.AddEnergyForAttack`、`UltimateEffect_Berserk` |
| 被动触发计数（监听 `OnAttackPerformed`） | `PassiveTriggerModule` |
| 命中连击与连击 Buff（与攻击类型无关） | `ComboManager` 订阅 `Enemy.OnDamageTaken` |
| 伤害 / 攻速 / 移速 / 经验倍率 | `BuffManager` + `UpgradeEffectManager.GetDamageMultiplier` / `GetAttackSpeedMultiplier` |
| 攻速缩放动作时长（因此也缩放整条序列） | `AttackSystem.GetAttackDuration` |
| 骑兵 109 冲锋打断 | `CavalryEnemy` / `InterruptsCavalryCharge` |
| BOSS 覆盖列、`bossState`、架势与 CFrame | `ColumnManager.GetCombatBossCoveringColumn`、`TakePoiseDamage` |
| 列与阵型 API | `GetEnemiesInRange` / `GetAllEnemiesInRange` / `ApplyPushWave` / `PostDisplacementFillUp` |
| 疾病弹消耗（Stab 命中时） | `ActiveSkillRunner.ConsumeArmedDisease` |
| 旋风等主动技能与道具 | `WhirlwindController`、`ItemEffectRunner` |
| QTE 直接取 Slash 配置 | `QTEController` |
| 路线系统控制输入开关与节点切换重置 | `FakeRouteRuntime` / `RouteStageRuntimeV2` / `ResetCooldownsForNodeTransition` |
| 武将装配 | `Hero_Zhangfei.asset` 装配 6 个 `AttackSkillConfig`（`Assets/Prefabs/UI/Skills/Zhangfei_*.asset`）+ `Zhangfei_BerserkUlt.asset` |

### 12.5 最容易丢的七条

1. **`OnAttackExecuted` 只有 `ChargeStabVisual` 一个订阅方**，用于蓄力姿态复位。InputManager 改造后若不再派发该事件，蓄力姿态会残留。
2. **空挥不消耗冷却、不给能量、不触发被动** — 状态机按输入推进段位，但资源结算必须继续按命中。
3. **蓄力事件链有 5 个订阅方**，改造时不能断链。
4. **攻速缩放的是整条序列**（动作时长与视觉一起变），不只是动作锁。
5. **DoT 不卡肉，卡肉也不冻结玩家**；卡肉广播必须带 strength 过滤。
6. **位移与补齐必须走既有通道**（`ApplyPushWave` / `PostDisplacementFillUp`），不得新建路径。
7. **QTE 与路线系统的输入门优先级高于状态机**；QTE 期间状态机必须停手并清空缓冲。

---

## 13. 注意事项

- **等价优先**：P1 的目标函数是「零差异」，任何数值或手感变化都视为失败。
- **计数与结算分离**：段位推进按输入；大招能量、被动 `OnAttackPerformed`、击退与补齐仍按命中（现有 `StabSweepEffect` 空挥不触发能量与被动，这条不能破坏），否则空挥会刷满能量。
- **位移与补齐走既有通道**：连招段带位移/击退时，必须继续走 `ColumnManager.ApplyPushWave` / `PostDisplacementFillUp`，不得绕开（`design/immutable-constraints.md`）。
- **打断参数逐段透传**：`canInterruptCFrame`、`interruptsCavalryCharge` 必须逐段传递（`design/attack-interrupt-system.md`）。
- **引用 Inspector 可追踪**：节点之间用直接引用，禁止字符串 id 查找（`design/anti-ghost-reference.md`）。
- **状态复位点**：节点切换、死亡/复活、暂停、对话、QTE、大招演出、升级三选一面板期间，状态机停止推进并清空缓冲。
- **不要复用 `_unlockedAttacks`**：那是局内解锁攻击的死代码，新注册表另起，避免语义混淆。
- **能量防护**：段数变多后每段都给能量会让连招变成刷能量机器，需要独立配置或按段递减。
- **窗口下限**：窗口按百分比随攻速缩放，必须有最小绝对时长保护（建议 ≥ 0.06s）。
- **HUD 数据源**：当前读 `PlayerState` 独立 CD，动作锁模式下显示不准（`design/attack-cooldown.md` 已记录），连招上线后需统一。

---

## 14. 当前坑点（现存实现风险）

| # | 坑点 | 证据 | 影响 |
|---|------|------|------|
| 1 | 卡肉只冻结敌人与特效序列，玩家动作锁继续走 | `HitFeedbackManager` 仅 `enemy.ApplyHitStop`；`StabSweepEffect` / `SweepEffect` / `AttackWave` 各自 `seq.Pause()`；全项目只有暂停/对话/三选一动 `Time.timeScale` | 动作锁先于视觉结束；窗口若按绝对秒会错位 |
| 2 | 所有滑动招式需按住 1.0s（场景实测，C# 默认 0.5s） | `InputManager.TryConsumeLiveGesture` 要求 `isLongPress && isCharged`；`minChargeTime` 被场景覆盖为 1.0 | 终结手势几乎不可能在连招窗口内完成 |
| 3 | 未蓄满的快速滑动在抬手时不产生任何动作 | `InputManager.ProcessGesture` 只处理「蓄满长按」与「未滑动的点击」 | 滑动输入被静默吞掉，且最可能发生在连招窗口内 |
| 4 | 点击在抬手才判定 | `ProcessGesture` 调用点位于 `TouchPhase.Ended` | 轻攻击固有延迟，连打节奏发糊 |
| 5 | 动作锁期间输入被直接丢弃、无缓冲 | `AttackSystem.TryExecuteAttack` 冷却检查早退 | 掉输入；接续窗口的前提缺失 |
| 6 | Stab 有独立于动作锁的第二道门 | `AttackSystem`：`if (attackType == Stab && _stabVisualTimer > 0f) return false;` | Stab 实际可用间隔 = 视觉时长，与动作锁重复；状态机接管后需统一 |
| 7 | 一个 `AttackType` 只能有一个配置 | `HeroConfig.GetSkillConfig` 按 `attackType` 唯一匹配 | 无法实现「每下 stab 不一样」；取配置 key 必须换成 `moveId` |
| 8 | `GetSkillConfig` 有 4 个调用点 | `AttackSystem.GetConfig`、`PlayerState`（冷却时长）、`UltimateSystem`（能量）、`QTEController`（QTE 取 Slash 配置） | 换 key 需一并处理；QTE 绕过输入直接取配置 |
| 9 | 手势角度边界互相干扰 | `ProcessSwipeGesture` 斜滑为兜底；`design/attack-cooldown.md` 已记录向左横扫不稳 | 连招中 Sweep/Slash/Parry 容易互相误判 |
| 10 | 卡肉会拉长整套连招的实际墙钟时间 | 每段首次命中都暂停序列（Light 0.045s / Standard 0.09s / Heavy 0.14s） | 百分比窗口不受影响，但总时长变长，节奏与数值评估要用实机时间 |
| 11 | 数值倒挂 | `Zhangfei_Slash.damage = 8` < `Zhangfei_Stab.damage = 20` | 终结技弱于轻攻击，与「段位越深越强」矛盾 |
| 12 | 新增输入缓冲后无天然上限 | 现状无缓冲，故无此问题 | 连点会堆积，需要「只保留最近一条」策略 |

---

## 15. 分期计划

| 阶段 | 内容 | 验收 |
|------|------|------|
| P1 | 招式状态机 + 接续窗口 + 输入缓冲 + 状态机调试器；不配置任何边 | 回归等价 + 单条边可生效 + 三轴开关可测 |
| P2 | 轻攻击按下即出 + 起手取消；蓄力门与滑动手势解耦 | 实机手感对比 |
| P3 | 上下文状态：招架成功态 + 专属连招 | 格挡成功后可进入专属分支 |
| P4 | 局外解锁接入边/节点；局内三选一改写边与窗口 | 与天赋树对接 |
| P5 | 新招式与新连段内容生产（枪突1/2/3 差异化、斩·二连/三连等） | 内容验收 |

---

## 16. 风险与未决项

**风险**

- 手势识别：滑动在连打中容易与点击、Sweep、Parry 混淆，需要实机调参而非纸面推演。
- 配置量：全招式可作前置后，节点 × 输入的边组合迅速膨胀，必须依赖接续表工具。
- 数值倒挂：张飞 Slash 伤害（8）低于 Stab（20），与「段位越深越强」的设计意图相反，需要一并校正。
- 现有被动/主动过强：若不同步收敛，招表系统上线后仍会被站桩技能绕过。

**未决项**

- 接续窗口的每招式起止百分比：以视效为基准逐招式微调（见 3.2）。
- 招架成功态的持续时长、可否叠加等细节：推迟到需要时再设计。
- 阶段时钟与命中卡肉的冻结关系：动作锁与连招窗口是否随卡肉一起暂停（建议暂停，以保持与视觉同步）。

---

## 17. 参考文件

| 文件 | 职责 |
|------|------|
| `Assets/Scripts/Player/InputManager.cs` | 手势识别、蓄力门、输入门控 |
| `Assets/Scripts/Player/AttackSystem.cs` | 招式执行、动作锁、解锁攻击注册表 |
| `Assets/Scripts/Player/PlayerState.cs` | `AttackType`、独立技能冷却 |
| `Assets/Scripts/Core/HeroConfig.cs` | 武将技能配置装配 |
| `Assets/Scripts/Core/AttackSkillConfig.cs` | 招式配置资产 |
| `Assets/Scripts/Core/ComboManager.cs` | 命中连击（区别于本设计） |
| `design/attack-cooldown.md` | 出手视觉与飞行物的职责划分 |
| `design/launch-strategy-system.md` | 击飞与空中连段的既有设想（本设计未纳入，仅作机制可扩展性参考） |
| `design/out-of-game-growth-system.md` | 局外天赋树与「禁止纯数值」约束 |
| `design/anti-ghost-reference.md` | 所有引用必须 Inspector 可追踪 |
