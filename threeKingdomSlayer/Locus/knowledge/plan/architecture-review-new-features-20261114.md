---
id: kd_94e540e7-18c2-444c-a6c9-ffadbedc5d7b
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 架构体检：破防演出 / 投壶兵 / 两个技能（2026-11-14）

> 范围：2026-11-13 ~ 14 新增或改动的代码 —— 破防演出包（REQ-006）、投壶兵（1014）、技能1 抡斧、技能2 追命锁链、`ChainLinkRig`、`PlayerPoisonStatus`、以及给 `Enemy.TakeDamage` / `ShieldGuardEnemy.ProcessHit` 加的开关。
> 目的：**在下一轮继续之前把架构上的欠账列清楚**，避免继续往同一个方向堆。
> 参照：`design/architecture-decoupling-analysis.md`（既有分析）、`memory/asmdef-layering-pitfalls.md`。

---

## 0. 结论

**程序集分层没有被破坏** ✅ —— 本轮所有新代码都落在既有的层里、依赖都是**同层内**。
但**层内的组织**有 6 处可以优化，其中 2 处建议下一轮就做（成本低、收益直接），其余留给专门的重构批次。

| # | 发现 | 影响面 | 代价 | 建议时机 |
|---|---|---|---|---|
| ① | 「投射物能不能被打落」这条**规则放在了投射物身上** | `EnemyProjectile`（敌人层）反过来依赖 `AttackSystem`（玩家层） | 小 | **下一轮就做** |
| ② | `Enemy.TakeDamage` 参数已 **21 个** | 全工程唯一的伤害入口，加一个开关就加一个参数 | 中 | 下一轮做（先做无风险的分组） |
| ③ | `ActiveSkillRunner` 已 **617 行 / 10 个 switch 分支**，每个效果 1~2 个方法 | 上帝类趋势；每加一个技能要改枚举 + runner + 基类配置三处 | 中 | 攒够 2~3 个新技能再做 |
| ④ | `UpgradeDefinition`（基类）堆了 **15 个「按效果类型」的 List** | 基类被具体效果污染；新增技能要动基类 | 大 | 与 ③ 一起做（③ 是它的前提） |
| ⑤ | 弹簧骨链求解器**写了两遍**（`ChainLinkRig` vs `WeaponTasselDynamics`） | 两套参数、两套手感 | 中 | 下一轮做 |
| ⑥ | 链链的**魔法数字散在代码里**（节数 9 / 节长 0.95 / 淡出…） | 调参要改代码 | 小 | 随 ⑤ 一起做 |

---

## 1. 分层核查（结论：✅ 无跨层违规）

> ⚠ **本工程的程序集是按 `.asmref` 文件分配的**（不是「目录里有 asmdef」）：目录里放一个
> `Locus.Combat.asmref` 就把该目录并入 Locus.Combat。**只看 asmdef 会看错归属。**

| 程序集 | 覆盖目录 |
|---|---|
| `Locus.Data` | `Data/` |
| `Locus.Services` | `Services/` |
| `Locus.Combat` | `Attack/` `Core/` `DebugTools/` `Effect/` `Effects/` **`Enemy/`** `MoveSystem/` **`Player/Combat/`** `Prop/` `QTE/` **`Status/`** `Wave/` |
| `Locus.Flow` | `Managers/` `Player/` `Route/` `RouteFake/` `Utils/` |
| `Locus.UI` | `UI/` `Tutorial/` |
| `Locus.EditorTools` | `Editor/` |

本轮新增/改动的归属与依赖：

| 文件 | 程序集 | 依赖检查 |
|---|---|---|
| `Core/ActiveSkillRunner.cs` | Locus.Combat | 引用 `ChainLinkRig`/`MeshWeaponVisual`/`Enemy`/`AttackSystem` ⇒ **全在 Locus.Combat** ✅ |
| `Effects/ChainLinkRig.cs` | Locus.Combat | 只依赖 UnityEngine ✅ |
| `Status/PlayerPoisonStatus.cs` | Locus.Combat | 引用 `PlayerState`（同层）✅ 与既有 `ReflectShieldStatus` 同构 ✅ |
| `Player/Combat/{PlayerState,PlayerGuard,PlayerWeaponStance,AttackSystem}.cs` | Locus.Combat | ✅ |
| `Enemy/{EnemyProjectile,Enemy,Enemy.Combat,ShieldGuardEnemy}.cs` | Locus.Combat | ✅（但见 ①） |
| `Data/{UpgradeDefinition,ActiveSkillDefinition}.cs` | Locus.Data | 新配置只含 `GameObject`/`Sprite`/`float` ✅ 不反向依赖 Combat ✅ |

⇒ **没有出现「低层引用高层」**。`Locus.Data` 里新增的效果配置结构体只引用 UnityEngine 类型 ✅。

---

## 2. 逐条发现

### ① 规则放错边：投射物自己去问「玩家是不是在挥枪」

**现象**
`EnemyProjectile.UpdateFlight()` 里加了 `TryShootDown()`，内部问 `AttackSystem.Instance.IsActionPlaying` 与
`AttackSystem.Instance.ProjectileShootDownRadius` ⇒ **敌人层的飞行物反向依赖玩家层的攻击系统**。

**为什么是问题**
- 方向反了：招架反弹（`DeflectProjectilesNear`）的规则**本来就在攻击侧**（`AttackSystem`），
  “打落”却是同一条规则，却写在了投射物侧 ⇒ 两条同类规则分居两地。
- 后果：以后若要做「只有某类攻击能打落」「命中框外不打落」，得再往投射物里加条件。

**建议改法（下一轮做，成本小）**
把判断挪到攻击侧，与反弹并列：

```csharp
// AttackSystem：与 DeflectProjectilesNear 并列
private int ShootDownProjectilesNear(Vector3 playerPos, float range) { ... p.ShootDown(); ... }
// 在攻击命中帧调用它；EnemyProjectile 只保留 ShootDown() 这个被动方法
```
⇒ 投射物不再认识攻击系统；规则与「反弹」同源、同位置。

---

### ② `Enemy.TakeDamage` 参数已达 21 个

**现象**
签名里现在混着：伤害/伤害类型/数字颜色/是否可打断霸体/是否招架中断/是否计连击/是否可打断攻击/是否播受击动画/
是否忽略伤害修正/反馈来源/反馈强度/命中位置/命中方向/是否带疾病/是否打断骑兵/受击方向/是否抑制受击动画/是否重击/破防值/招架破防值/
**是否无视盾牌减伤**。

**为什么是问题**
- 再加 2~3 个开关（例如「无视霸体」「算处决」）就会到 24+ ⇒ 可读性与出错率同步上升；
- 新加的 `ignoreShieldBlock` 已经是**第 21 个布尔**，调用点靠命名参数维护，极易漏传/传错。

**建议改法（下一轮做，分两步）**
1. **先分组**（不改行为）：把「反馈类」参数收成一个 `HitFeedbackOptions`（含 feedbackSource / feedbackStrength /
   impactPosition / impactDirection / hitReactionDirection / suppressHitReactionAnimation），
   「行为类」收成一个 `HitOptions`（含 canInterruptCFrame / isParryInterrupt / countsForCombo / canInterruptAttack /
   triggerHitAnimation / ignoreDamageModifiers / ignoreShieldBlock / interruptsCavalryCharge / diseaseStabHit /
   isHeavyAttack / poiseDamage / parryPoiseDamage）。
   ⚠ 改动面大（调用点多）⇒ 建议**只给新代码用**，旧调用点保持原样逐步迁移，别一次性全改。
2. 长期：破盾/破防类开关应进**“命中修饰”小结构体**，而不是顶层参数。

---

### ③ `ActiveSkillRunner` 变成上帝类

**现象**
617 行；`switch (definition.activeEffectType)` 有 **10 个分支**；每个效果 1~2 个私有方法；
本轮新增的两个效果各带一段 **协程表现**（`RunAxeSmash` / `RunChainPull`）⇒ 表现、选目标、位移、结算全挤在一个类里。

**建议改法（攒够 2~3 个新技能再做）**
```csharp
public interface IActiveSkillEffect {
    bool Activate(ActiveSkillDefinition def, int level);   // 入口
}
// 每个效果一个文件：Effects/AxeSmashEffect.cs、ChainPullEffect.cs、…
// ActiveSkillRunner 只做：查表 → 调 effect.Activate
```
- 好处：加技能不再改 runner；表现协程有地方放；可单测。
- 代价：一次性重构所有既有效果 ⇒ **不要和做新功能混在一起**。

---

### ④ 基类被效果类型污染

**现象**
`UpgradeDefinition`（所有升级的基类）里有 **15 个** `List<XxxLevelConfig>`，每个对应一种效果类型
（`waveLevels` / `diseaseLevels` / `cycloneLevels` / `chargeAttackShockwaveLevels` / **本轮新增的
`axeSmashLevels` / `chainPullLevels`** ……）。

**为什么是问题**
- 基类每加一个技能就多两个字段（配置 + 模型引用）⇒ 基类持续膨胀，且**数据层知道太多具体效果**；
- 同一份资产里 15 组只有一组有效，其余是空列表（序列化噪音）。

**建议改法（与 ③ 一起）**
把「效果专属配置」下沉到效果自己的 ScriptableObject：
`ActiveSkillDefinition` 只留 `activeEffectType` + 冷却表 + `List<UpgradeDefinition>` 引用；
每个效果一个 `AxeSmashDefinition : ScriptableObject`（含分级配置、模型引用、链节贴图）挂在 `ActiveSkillDefinition` 上。
⇒ 数据层不再随技能数量线性膨胀。

---

### ⑤ 弹簧骨链求解器写了两遍

**现象**
- `Effects/ChainLinkRig.cs`（本轮新增）：距离约束 + 位置弹簧 + 重力下垂，**自己一套**；
- `Effects/WeaponTasselDynamics.cs`（既有，红缨）：**另一套**弹簧骨链（位置/速度/刚度/阻尼/重力/静止朝向/瞬移重置）。

**建议改法（下一轮做）**
抽 `Effects/SpringBoneChain.cs`（共用求解器 + 参数），两者各自只保留「锚点怎么接」。
- 好处：手感参数一处可调；链与红缨的物理表现一致。
- 注意：两者语义不同（红缨有**静止朝向**与**瞬移重置**），抽公共部分时别把这两条混进去。

---

### ⑥ 魔法数字散在代码里

**现象**
链链的节数（9）、节长（0.95）、拖影淡出等写在 `ActiveSkillRunner` 与 `ChainLinkRig` 的默认值里，
**不在配置资产里** ⇒ 调手感要改代码 + 重编译。

**建议改法**
- 节数/节长/链环缩放 ⇒ 进 `ChainPullLevelConfig`；
- 表现节奏（甩出 0.25s / 回收 0.2s）⇒ 进配置或常量集中处。

---

### ⑦ 玩家「直接扣血」的通道要显式命名

**现象**
`PlayerState.TakeDamageUnblockable(float)` —— 这条**刻意绕过防御与反射盾**的通道是毒/灼烧这类持续伤害需要的，
但名字不自我解释，容易被误用（例如给普通伤害用）。

**建议改法**
- 改名/加注释说明「仅供持续伤害（DoT）使用」；
- 或把调用点收进状态类内部（毒状态自己调），`PlayerState` 只暴露 `ApplyDirectDamage(...)` 且标注用途。

---

### ⑧ 加一个技能要改三个地方（枚举 + switch + 基类配置）

**现象**
新增一个主动技能 = ① `ActiveSkillEffectType` 加枚举值 ② `ActiveSkillRunner` 加 switch 分支 + 实现
③ `UpgradeDefinition` 加配置 List ④ 建资产并登记进 `ActiveSkillPoolConfig`。

**建议改法**
与 ③/④ 一起解决：改成**注册表**（`Dictionary<ActiveSkillEffectType, IActiveSkillEffect>`）
⇒ 加技能只写新文件 + 建资产。

---

## 3. 本轮**不建议**现在动的

- **全局 `timeScale` 相关的任何重构**：破防演出已经删掉了全局慢放（`GuardBreakTimeFx` 已删），
  现在只有敌人侧的**局部**慢放（`EnemyHitFeedback`）⇒ 无全局时间副作用，不必再动。
- **`Enemy` 的伤害管线整体重写**：影响面覆盖全工程（敌人、被动、三选一、教程）⇒ 留到有整块时间时做。
- **`ChainLinkRig` 提前做成通用链组件**：等正式链节模型到位、知道最终要几条骨之后再抽象。

---

## 4. 建议的执行顺序（下一轮）

1. ① 打落规则挪到攻击侧（小、纯重构、可用机器人回归）。
2. ⑤ + ⑥ 抽公共弹簧骨链 + 参数进配置。
3. ② 只给**新代码**引入分组参数（`HitOptions`），旧调用点逐步迁移。
4. ③④⑧ 等技能再攒 2~3 个一起做（避免重构与新功能混在同一批）。
5. ⑦ 改个名 + 注释（一行）。
