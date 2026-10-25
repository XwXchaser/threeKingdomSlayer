---
id: kd_c10dc057-dc52-4576-94f9-09869e1c6e5e
injectMode: inherit
summary: 敌人攻击打断系统设计文档：三级打断体系（普通攻击/C技/QTE）、霸体机制、与三选一奖励及Parry/Launch的交互关系、实现计划。2026-10-25 霸体反制规则重定：重攻击可打断霸体并削韧破势、招架空放长后摇。
aiEditMode: inherit
---

# 攻击打断系统设计

## 1. 设计目标

玩家的所有直接攻击（Stab/Slash/Pierce/Sweep + 三选一奖励技能伤害）都能打断敌人正在进行的普通攻击，提供《真三国无双》式的割草压制感。

**不包括**：DOT 持续伤害（尚未实现）。

## 2. 参考游戏分析

### 鬼泣（DMC）
- 精英敌人有霸体窗口，普通攻击无法打断
- 需要特定招式（如 Stinger 破霸体、High Time 挑空）来应对
- 核心：**"识别威胁 → 选择正确对策"** 的博弈感

### 真三国无双
- 杂兵攻击可被任意攻击打断
- 敌将蓄力技（C技）有明显前摇，可用无双/特殊技打断
- 核心：**割草的爽快感 + 对敌将的关键应对**

### 本项目的融合
> "杂兵像无双一样被压制，精英像鬼泣一样需要正确对策"

## 3. 三级打断体系

### 3.1 非Boss敌人

```
┌─────────────────────────────────────────────────────┐
│ 窗口       │ 打断条件              │ 设计意图         │
├─────────────────────────────────────────────────────┤
│ 普通攻击   │ 任何直接伤害即可打断   │ 割草压制         │
│ C技(蓄力技)│ 重攻击可打断；招架仅在出伤窗口 │ 前摇留给重攻击拆 │
│ AttackDraw │ 不可打断               │ 收招保护         │
│ QTE攻击    │ 不可打断               │ BOSS终极技能     │
└─────────────────────────────────────────────────────┘
```

> **重攻击** = 招式资产 `isHeavy = true`（穿刺 / 横扫 / 挑飞 / 枪突终结技 / 指向突刺 C2）。
> 重攻击打断霸体的**同时**按资产 `poiseDamage` 削韧（见 §4.5 破势规则）。

### 3.2 Boss敌人

Boss 的攻击打断规则与非Boss不同，核心原则：**招架可在出伤窗口格开Boss攻击；重攻击可打断前摇与霸体**。

```
┌──────────────────────────────────────────────────────┐
│ Boss窗口          │ 打断条件            │ 设计意图    │
├──────────────────────────────────────────────────────┤
│ 普通窗口          │ 所有攻击可打断       │ 压制窗口    │
│ (非CFrame非SA)    │                      │             │
│ CFrame (C技霸体)  │ 重攻击可打断         │ 重攻击拆前摇     │
│ SuperArmor (霸体) │ 重攻击可打断         │ 同上             │
│ 出伤窗口          │ 招架可格开（仅招架） │ 招架的专属时机   │
│ AttackDraw        │ 不可打断             │ 收招保护    │
│ QTE攻击           │ 不可打断             │ 独立系统    │
└──────────────────────────────────────────────────────┘
```

**设计理由**：
- 招架与重攻击**各司其职**（2026-10-26 重定）：
  · **重攻击 = 拆前摇**：慢、稳、提前打断（霸体期也能）；
  · **招架 = 格出伤**：快、准，**只能在挥击即将落下的那一下**（前摇打不断）。
- 招架仍是唯一能在**霸体期**格开攻击的手段（Boss 的霸体整段常开 → 必须精准到出伤窗口）。
- 不存在玩家用招架无法应对的 Boss 攻击（QTE 属于独立演出系统，不在此规则内）

### 3.3 Level 1 — 普通攻击
- 所有敌人默认的攻击类型
- `isCFrame = false`, `isSuperArmor = false`
- 被任何 `TakeDamage()` 调用打断（`CancelAttack()`）
- **意义**：玩家积极进攻即可压制杂兵，类似无双割草

### 3.4 Level 2 — C技（蓄力技）
- 由 `cAttackProbability` 控制触发概率
- 前摇阶段 `isCFrame = true`（霸体窗口）
- 伤害帧（`PerformAttack()`）结束后 `isCFrame = false`
- 非Boss: **重攻击**伤害 → 打断成功（同时削韧）；**招架**只在出伤窗口内能格开
- Boss: **重攻击**伤害 → 打断成功（同时削韧）；**招架**只在出伤窗口内能格开
- 普通伤害 → 弹刀反馈（`PlayClankEffect()`：橙红闪烁+水平抖动）
- **意义**：前摇留给重攻击拆；招架是“格出伤”的精准手段（详见 §3.1 / §3.2 / §4.8）

### 3.5 Level 3 — QTE攻击（BOSS专属）
- `state == EnemyState.QTEAttacking`
- `TakeDamage()` 可正常造成 HP 伤害，但不会触发打断（不播 HitFlash 动画，保留闪白+Scale 效果）
- 玩家在 QTE期间可正常发动攻击，但攻击动作未结束时无法与 QTE 指示器交互（`IsActionPlaying` 守卫）
- QTE提前输入不再判定失败，未命中指示器的手势穿透为普通攻击
- 由 `QTEController` 驱动，独立于普通攻击状态机

## 4. 霸体（Super Armor）设计

### 4.1 非Boss
C技的前摇阶段即为"霸体窗口"：
- `isCFrame = true` 期间，非 Parry/Launch 伤害只触发弹刀
- 弹刀反馈：橙红闪白 + 水平抖动，区别于普通受击的白色闪白 + 缩放抖动
- 敌人不会因弹刀而停止攻击

`isSuperArmor` 在非Boss中始终为 `false`（代码中无赋值路径）。

### 4.2 Boss
Boss 可通过 `isSuperArmor`（BossPhase配置）和 `isCFrame`（C技步骤）两种方式进入霸体状态。两种窗口下仅 Parry 可打断。

`isSuperArmor` 仅通过 Boss 转阶段时赋值：`isSuperArmor = nextPhase.isSuperArmor`。若 Phase 未配置，默认为 `false`。

### 4.3 设计考量

| 维度 | 非Boss | Boss |
|------|--------|------|
| 触发条件 | 仅 C技 前摇阶段 | C技前摇 + BossPhase SuperArmor |
| 玩家反制 | Parry、Launch | **仅 Parry** |
| 视觉反馈 | 弹刀特效（橙红闪烁+水平抖动） | 同左 |
| 节奏影响 | 玩家需要识别 C技 前摇并选择对策 | Boss 强制玩家使用 Parry |

### 4.4 与战斗循环的关系

```
玩家进攻 → 杂兵被压制（普通窗口打断）
         → 遇到 C技敌人 → 弹刀反馈 → 玩家选择：
            ├─ 招架：高风险高回报（精确时机 + 立即打断 + 削韧）
            ├─ 重攻击：慢但稳（打断 + 削韧，削满破势）
            └─ 闪避：安全但损失输出窗口
         → BOSS 霸体/CFrame → 弹刀反馈 → 玩家选择：
            ├─ 招架：立即打断（唯一即时选项）
            └─ 重攻击：打断 + 削韧，削满 → 破势眩晕
         → BOSS QTE：强制演出，不可打断（Level 3）
```

### 4.5 破势削韧（2026-10-25 定）

**削韧来源**：招架（既有，`poiseDamage`）＋ **任何重攻击**（`isHeavy = true` 的招式，削韧值取同一字段）。

**生效范围**：削韧**只在霸体窗口内**生效——C 技霸体（`isCFrame`）或阶段霸体（`isSuperArmor`）。
窗口外的敌人不吃这份削韧：**破势是“破霸体”的手段，不是通用输出窗**。

**结算**：

```
① 重攻击命中处于霸体窗口的敌人 → 按资产 poiseDamage 削韧
② 韧性归零 → 破势：进入 Stunned（眩晕输出窗），同时解除霸体
③ 眩晕结束 → 韧性回满（Enemy.StateUpdate.BeginStunExit）
```

**边界**：

- 只有玩家攻击本体算数（`HitFeedbackSource.BasicAttack`）：DoT / 被动 / 道具 / 碰撞 / 幻影不削韧（与冰系“破冰只认玩家本体”同口径）。
- 破势眩晕时长：普通敌人用 `poiseStunDuration`（默认 1.5s）；BOSS 沿用 `stunDuration`（默认 5s）。
- 非 BOSS 敌人的 `maxPoise` / `poiseStunDuration` 此前是死值（只有 BOSS 会被削韧），本规则后它们**第一次生效**。
- 招式资产 `poiseDamage` 一字段两用：招架用它是“削韧量”；挑飞/终结技用它是“挑飞所需的架势阈值”（`CanBeLaunched`）——**改这两个资产的 `poiseDamage` 会同时改挑飞阈值**。
- 当前数值：挑飞 50 / 枪突终结技 80 / 指向突刺 C2 30 / 穿刺 30 / 横扫 25（未挂载）。

### 4.6 招架后摇（2026-10-25 定）

**空放 = 没格挡到任何攻击**：既没弹开敌方飞行物，也没格挡到正在攻击的敌人（`state == Attacking && isAttackAnimating && !isAttackDrawPhase`）。

- 空放 → 动作锁用 `AttackSkillConfig.parryWhiffDuration`（默认 **0.8s**），而不是格挡成功的 `actionDuration`（0.2s）→ **招架不能连发**。
- 空放不给大招能量（否则可空刷）。
- 动作锁模式下生效；独立 CD 模式仍走每招独立冷却（`cooldown`）。
- 后续扩展：长按可在招架后摇期间保持防御姿态（见 §4.7）。

### 4.7 防御姿态与破防（2026-10-25 定 / 2026-10-26 重做）

#### 4.7.1 持枪实体与动作链（2026-10-26）

**为什么要有常驻实体**：原实现是“招架 = 自播自毁的独立特效（ParryVisualEffect）”+“防御 = 另起一个实例”，于是场上同时有两把枪、两套深度、两套姿态基准 → parry 与 defend 之间是跳变。现在改成 **PlayerWeaponStance**（`Assets/Scripts/Player/Combat/PlayerWeaponStance.cs`）常驻玩家身上，持有**一把**枪，播一组动作。

**动作链（用户定的规则，2026-10-26 修正并重做）**：

```
招架 → **挥入**（从屏外上方 77° 斜劈入画 → 斜举位 42°；0.18s ease-out）
        → 过冲 +8° → 回弹 0.08s → **定格 0.10s**（格住的那一下）
          ├─ 玩家按住（挥入前/挥入中按都算）→ 立刻接**防御位**（角度同为 42°、**只用位置**下沉 0.25）→ 防御态
          │     ├─ 松手 → **收枪**：沿原路挥出画面 → 出画后才销毁
          │     │           挥出途中按住 → 接住 → 回防御位
          │     └─ 破防 → 枪被打脱手，同样要挥出画面
          └─ 玩家没按 → **收枪**：沿原路挥出画面 → 出画后销毁
```

**收枪速度**：成功格挡 **0.12s**（加速）/ 未格挡 **0.54s**（正常）——沿用旧版招架的规则。

**预输入的正确语义（2026-10-26 用户纠正，之前的实现是 BUG）**：
> 预输入只是“提前按住”，**本质仍然是先播完 parry，再回到防御位的角度**。
> “挥入中一按住就切到防御位”= **跳过了上扬、动作不完整**，是实现缺陷，不是机制。
> 正确做法：挥入阶段只**记住**“玩家按着”，等招架动作完整播完（到斜举位）再接防御位。

**收枪 = 防御结束**，且**武器必须挥出画面**才能销毁——不允许在画面里原地消失
（底层准则见 `design/combo-move-state-machine.md` §19「武器动作准则：进入与离开」）。

**动作锁必须覆盖“武器在画面上的全长”**：否则玩家能在枪还没出画时就出招 → 两把枪同屏。
现值：`actionDuration` 0.48（= 挥入 0.18 + 回弹 0.08 + 定格 0.10 + 加速收尾 0.12），
空放侧由 `parryWhiffDuration` 0.9 覆盖（= 0.18 + 0.08 + 0.10 + 0.54）。
**防御态期间不允许出招**（手势被吞）→ 想攻击必须先松手收枪（与旧版“parry 时不能攻击”一致）。

#### 4.7.2 姿态的表达方式（重要）

**不用手调欧拉角**（枪 prefab 自带 (90,0,0)，不同代码路径下含义还不一样），而是「**矛头指向 + 矛头落点**」：

```
rot = Quaternion.FromToRotation(枪身本地 +Y, 目标世界方向)
根节点 = 矛头 − (Length/2)·方向        // 根节点在枪身中段，Length = MeshWeaponVisual.Length = 10.8
```

> **两个容易搞错的事实**（都是实测踩过的坑）：
> 1. **根节点在枪身中段，不在枪尾**。依据：Stab 视觉用 `Length*0.5` 把模型往后推，让矛头落在射线末端。
> 2. **不能拿包围盒/BakeMesh 顶点量矛头偏移**：模型带 100×11.05 的缩放链，
>    SkinnedMeshRenderer 的世界包围盒虚高 ~20%，BakeMesh 的顶点又落在骨骼局部空间 → 两者都不可用。

**参考图实测（《起源》第三人称，背面视角 = 我们相机视角）**：

| 项 | 值 |
|---|---|
| 枪尖指向 | 屏幕右上 **≈42°**（正面图 48°） |
| 手部 | 右手握枪身中上段（肩胸位），左手握靠枪尾的下段（重心偏角色右手 = +X 侧） |
| 防反（defend_attack1~3） | 从斜举**上撩到竖直**（80° → 90°），终态竖直 |

**坐标对照**：相机 `(0,3,-10)` 朝 +Z、18° 俯角 → **屏幕右 = +X**；玩家面朝 +Z → 角色右手在 +X 侧 → 参考图与我们的视角**同侧，不需要镜像**。

**实测的三个旧姿态**（避免后人重踩）：

| 姿态 | 枪尖方向 | 结论 |
|---|---|---|
| 现招架格挡 `Euler(54,270,0)` | 屏幕**左上** 36° | 与参考**镜像** |
| 上一版防御姿态 | 水平 0° | 就是用户说的“太平行” |
| 本轮 | 屏幕**右上** 42° | 与参考一致 |

#### 4.7.2.1 空间层：武器视觉一律在“前景带”

相机 z=-10、玩家 z=0、敌人根 z=+9。本游戏所有武器视觉都在**相机与玩家之间**：

| 武器视觉 | 落点 |
|---|---|
| Stab | 射线起点 z=-5.5（硬编码），y=-0.6 |
| Slash | 生成点 z=-3.6 |
| 原版 Parry | 枢轴 z=-6，y=1.5 |
| ChargeStabVisual | 跟随指针世界坐标（位置不固定） |
| **持枪实体（现在）** | 矛头锚点 z=-4.5，scale 0.55 |

**踩过的坑**：曾把持枪实体放在**玩家所在的深度**（z=+0.4、y=0.9）→ 投影落在屏幕中线偏上约 20%，
正是敌人 row0 的高度带 → 读出来就是“一根枪飞到敌人面前”。**持枪必须落在前景带**，
且矛头要在**敌人线以下**（现值：屏幕“右 20% / 上 -6%”，0% = 画面中心，敌人在上 ~17%）。

**尺度**：前景带里用全尺寸（1.0）会变成“巨剑怼脸”；现用 0.55 → 画面里是矛头 + 红缨 + 一段杆身斜跨右下，
与参考图 idle 同一读法。

#### 4.7.3 破防值（真衰减条）

- **B 方案**：每次受击 **+伤害**，**每帧按固定速率回落**（不再有“停手 N 秒才开始降”的阶段）→ 不挨打就自然回落。
- **阈值 / 回落速率 = 自然的记忆窗口长度**（默认 120 / 30 每秒 ≈ 4 秒）。
- 归零 → **破防**：GameplayInputGate 申请 `GuardBroken` 拦截 → 不能行动 / 不能放技能 / 不能做手势输入，持续 `parryGuardBreakDuration`（默认 1.5s）后自动恢复。
- **显示**：玩家血条上方一条 `GuardMeter`（HeroHUD prefab 的 `Health(Slider)` 下方新增，Filled/Horizontal，复用 `zhangfei_HP_filler`）+ `HeroHUD.SetGuardMeter(ratio, visible)` + `BattleHUD` 每帧轮询。

#### 4.7.4 其它边界
- 防御期间**免伤**、蓄力等级恒 0（不出现蓄力视觉）、蓄力指示器让位。
- **滑动不出招**：必须先松手退出防御（持枪姿态进行中时手势全部吞掉）。
- **两杆长枪的根因**：站桩蓄力武器 `ChargeStabVisual` 是**跟随指针世界坐标**的，按住 ≥0.3s 就出现，
  而招架正是“按住时划一下”→ 两者必然同屏。现在蓄力视觉 / 蓄力等级 / 蓄力指示器
  一律对“持枪姿态进行中”（Raising / Returning / Settled）让位（`IPlayerInput.IsGuarding`）。
- **前置条件**：招架本身是**局外天赋解锁项**（`talent_unlock_parry`，100 铜钱，独立旁支、无前置）。没解锁就既不能招架、也进不了防御姿态。2026-10-25 曾因天赋树的“空列表 = 取列表上一项”旧规则把它挂在 1850 铜钱单链末端，导致实际买不到 → 已修（见 `design/out-of-game-growth-system.md`）。
- **归属**：`PlayerWeaponStance` + `PlayerGuard` 都由 `PlayerState` 持有（与 Enemy → EnemyPoise 同做法，不新增场景组件）；全部数值挂在**招架招式资产**的 `parryGuard*` / `parryStance*` 字段上（Inspector 可调、无幽灵引用）。
- **实现注意**：武器销毁前必须先 `DOKill`（否则后摇 tween 会写已销毁的 Transform → MissingReferenceException）。

#### 4.7.5 待做（防反）

- 防反（defend_attack）规则用户明确“之后再说”；本轮只**预留竖直终态姿态**（参考 defend_attack3），未实现动作。

### 4.8 招架时机重定（2026-10-26）

#### 4.8.1 为什么要改

原来的招架能打断敌人的**前摇**（出伤那一下反而打不断，因为出伤瞬间 `isAttackDrawPhase` 就被置 true）。
这让“招架”变成了一个**提前取消**的工具，而不是“格开”的工具，且与重攻击的定位重叠。

**新定位：招架与重攻击各司其职**

| | 前摇 | 出伤窗口（挥击即将落下） | 收招 |
|---|---|---|---|
| 轻攻击 | 打断（普通窗口；霸体期无效） | 打断 | 不可 |
| **重攻击** | 打断（霸体期也能）→ 专职“拆前摇” | 打断 | 不可 |
| **招架** | **不断** | **格开（取消攻击 → 出伤不结算）** | 不可 |

#### 4.8.2 出伤窗口（strike window）

敌人的出伤在当前数据里是**一帧**（1011 有命中帧但只 1 帧；其余走无命中帧兜底：前摇结束那一帧 `PerformAttack()`），
所以“出伤段”需要一个**人为窗口**：

- `Enemy.parryStrikeWindow`（默认 **0.15s**，Inspector 可调）：出伤时刻前这么多秒内，招架能格开这次攻击。
- 出伤时刻 = 本次攻击的**前摇结束时刻**（已按命中帧修正；远程 = 发射瞬间）。
- 实现用攻击 tween 的**已播比例**换算（`Duration * ElapsedPercentage`）→ 卡肉/冻结暂停时判定不错位。

#### 4.8.3 削韧跟着“判定成功”

- 招架的削韧**只在真的格开了一次时**发生（与打断成功同点结算，`parryPoiseDamage` 参数透传）。
- 旧行为是“对范围内每个敌人都无条件 `TakePoiseDamage`”——空挥也在磨 BOSS 的架势，已废除。
- 数值：`Zhangfei_Parry.poiseDamage` 10 → **30**（BOSS `maxPoise` = 100 → 3~4 次成功格开破一次势）。

#### 4.8.4 预输入进防御（无空档）

- 挥入过程中就持续采样 hold：一旦检测到按住 → **记住**（**不切断动作**）；等招架动作完整播完 → 立刻接到防御位。
- 于是“parry 判定段就开始长按” → 直接进防御姿态，中间没有“举着枪但还没防御”的空档（那段既不免疫也不计破防）。
- 挥入 0.8s → **0.18s**（要“迅速格开”的脆度）；收枪（挥出画面）0.54s / 0.12s（成功格挡则加速）。
- **防御位与斜举位用同一个角度**（42°），只用**位置**下沉 0.25 → 枪尖上扬角每次读得一样（不再出现“每次角度不同”）。
- 红缨（`WeaponTasselDynamics`，速度驱动的弹簧骨链）在持枪实例上**收敛**（阻尼 12→26、摆幅 38→14）→ 枪头附近的视觉抖动变小。

## 5. 数据流

### 5.1 canInterruptCFrame 传递

```
伤害来源                      canInterruptCFrame   isParryInterrupt   isHeavyAttack
──────────────────────────────────────────────────────────────────────────────────
Stab/Slash/Pierce/Sweep       false（默认）         false（默认）       false（默认）
Parry                          true                  true                false
Launch (via AttackWave)        true                  false               true（资产 isHeavy）
枪突终结技 (via AttackWave)    true                  false               true
指向突刺 C2 (via StabSweep)    false                 false               true
Thunder (落雷)                 false（默认）         false（默认）       false（默认）
三选一全屏伤害                  false（默认）         false（默认）       false（默认）
Whirlwind tick damage          false（默认）         false（默认）       false（默认）
```

> `isHeavyAttack` 与 `poiseDamage` 一路透传到 `Enemy.TakeDamage`（沿用 `interruptsCavalryCharge` 的逐段透传风格）。

### 5.2 打断决策伪代码

```
if state == Attacking && isAttackAnimating && !isAttackDrawPhase:
    if isBoss:
        if !inArmorWindow:
            CancelAttack()    # Boss普通窗口：所有攻击可打断
        elif isParryInterrupt or isHeavyAttack:
            CancelAttack()    # Boss霸体/CFrame：招架或重攻击可打断
    else:
        if !inArmorWindow:
            CancelAttack()    # 非Boss普通窗口：所有攻击可打断
        elif canInterruptCFrame or isHeavyAttack:
            CancelAttack()    # 非Boss CFrame：招架/打断标记/重攻击可打断

# 削韧（破势）：只在霸体窗口内，且只认玩家攻击本体
if poiseDamage > 0 and inArmorWindow and source == BasicAttack:
    currentPoise -= poiseDamage
    if currentPoise <= 0: Stun()                # 破势：眩晕 + 解除霸体
```

### 5.3 关键传递路径
```
AttackSystem
  → AttackWave.Create(..., canInterruptCFrame, isParryInterrupt, isHeavyAttack, poiseDamage)
    → AttackWave.HitTarget()
      → Enemy.TakeDamage(damage, type, color, canInterruptCFrame, isParryInterrupt,
                          isHeavyAttack, poiseDamage)
        → 霸体窗口捕获 → 破势削韧 → 打断检查
```

## 6. 已完成的改动

### 6.1 sharedHealthGroup 打断修复
- **问题**：共享血量敌人（如 Enemy_101）的 `TakeDamage()` 在打断检查之前就 return 了
- **修复**：将打断逻辑移到 `sharedHealthGroup.TakeDamage()` 调用之前
- **文件**：`Assets/Scripts/Enemy/Enemy.cs` — `TakeDamage()` 方法

### 6.2 `canInterruptCFrame` 参数传递
- `AttackWave.Create()` 接受 `canInterruptCFrame` 参数，默认 `false`
- `SweepEffect` 同样支持
- Parry/Launch 传入 `true`

## 7. 待实现

### 7.1 Boss CFrame/SuperArmor 仅 Parry 打断（P0）
**问题**：Launch 也能打断 Boss 的 CFrame/SuperArmor 攻击，违反设计意图。
**方案**：`TakeDamage` 增加 `isParryInterrupt` 参数，Boss 分支仅 `isParryInterrupt` 时执行 `CancelAttack`。

### 7.2 共享血量组打断传递（P1）
**问题**：当共享血量组中成员A被命中但不在攻击中，成员B正在攻击中时，B的攻击不会被中断。
**方案**：在 `SharedHealthGroup.TakeDamage()` 中遍历成员，对正在攻击的成员调用打断逻辑。

### 7.3 WhirlwindController.ExecuteLaunch() 绕过 TakeDamage（P1）
**问题**：漩涡击飞直接调用 `enemy.Launch()`，不经过 `TakeDamage()`，跳过了打断检查。
**影响**：漩涡可以打断 C技（可能是有意的，需确认）。

### 7.4 旅行波到达时序优化（P2）
**问题**：Pierce/Sweep 的波飞行时间可能超过敌人的 AttackSpawn 窗口（尤其在 spawnDuration 较短时），导致波到达时敌人已进入 AttackDraw。
**方案**：
- 方案A：增大默认 `attackSpawnDuration`（让窗口更长）
- 方案B：旅行波命中时无视 `isAttackDrawPhase`（让整个攻击动画都可打断）
- 方案C：在波创建时快照目标敌人的状态，到达时使用快照状态判断（不推荐，过于复杂）

### 7.5 三选一技能伤害接入 `canInterruptCFrame`（P2）
需逐个审查三选一技能，确认哪些应该能打断 C技。

## 8. 动画架构讨论（仅讨论，未实现）

- 当前使用 DOTween 驱动攻击动效（位移+翻转）
- 未来转为 AnimationClip + Animator
- `EnemySpriteController` 可演化为动画控制器
- 攻速匹配：通过 `animator.speed = baseSpeed * (attackData.duration / animationClip.length)` 在 Inspector 调整 `attackSpawnDuration` 自动匹配动画速度

## 9. 测试验证

### 已验证
- Parry 成功打断 C技（日志确认：`isCFrame=True, canInterruptC=True → CancelAttack`）
- Stab 命中 C技敌人触发弹刀反馈（日志确认：`isCFrame=True, canInterruptC=False → PlayClankEffect`）
- 非攻击状态敌人不受影响（日志确认：`state=Idle → 条件不满足`）

### 待验证
- 破冰推动与破势眩晕叠加时的表现（冰系 + 霸体同时成立）
- 挑飞打断霸体后的挑飞阈值是否被破势改写（`CanBeLaunched` 读 `currentPoise`）
- 招架空放在独立 CD 模式下的表现
- 弹刀反馈仍未实现（设计 §3.4 写了 `PlayClankEffect`，代码里不存在）

### 2026-10-25 霸体反制重定 · Play Mode 实测（Battle 场景，运行时构造 1011 / 104）

| 用例 | 结果 |
|---|---|
| 重攻击打 C 技霸体 | 打断 ✓；架势 50 → 20 ✓ |
| 再一击重攻击（同一目标） | 架势归零 → **破势 Stunned** ✓；`isCFrame` 清除（霸体解除）✓ |
| 非霸体窗口打重攻击 | 打断 ✓；**架势不变**（窗口外不削韧）✓ |
| BOSS 阶段霸体 + 重攻击 | **打断 ✓**（新规则，旧设计只有招架能断）；架势 100 → 70 ✓ |
| BOSS 阶段霸体 + 轻攻击 | 不断打 ✓；不削韧 ✓ |
| 招架空放 | 动作锁 **0.8s**（`parryWhiffDuration`）✓ |
| 招架格挡到正在攻击的敌人 | 动作锁 0.2s ✓；目标被打断 ✓ |
| 空放后立刻再招架 | 被动作锁挡住 ✓ |

## 10. Parry 架势边界修复（已实现）

- 普通敌人不参与 Parry 架势累计，不会因连续 Parry 进入 `Stunned`；Parry 对普通敌人的攻击打断规则保持不变。
- Boss 仅在 `BossState.InCombat`、`state == Attacking`、`isAttackAnimating == true` 且不处于 `AttackDraw` 时受到 Parry 架势伤害。
- `state == Attacking` 同时覆盖攻击冷却与攻击动画，不能单独作为架势伤害窗口；必须额外检查 `isAttackAnimating`。
- 修复原因：旧实现允许普通敌人累计架势并在归零后进入 Boss 专属眩晕流程，眩晕结束落入 `Idle` 后可能永久失去攻击调度。
