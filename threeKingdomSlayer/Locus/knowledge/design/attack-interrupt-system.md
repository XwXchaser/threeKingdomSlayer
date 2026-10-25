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
│ C技(蓄力技)│ 招架 / 任何重攻击      │ 需要正确对策     │
│ AttackDraw │ 不可打断               │ 收招保护         │
│ QTE攻击    │ 不可打断               │ BOSS终极技能     │
└─────────────────────────────────────────────────────┘
```

> **重攻击** = 招式资产 `isHeavy = true`（穿刺 / 横扫 / 挑飞 / 枪突终结技 / 指向突刺 C2）。
> 重攻击打断霸体的**同时**按资产 `poiseDamage` 削韧（见 §4.5 破势规则）。

### 3.2 Boss敌人

Boss 的攻击打断规则与非Boss不同，核心原则：**招架始终可打断Boss攻击；重攻击可打断霸体并削韧；Launch仅在普通窗口可打断**。

```
┌──────────────────────────────────────────────────────┐
│ Boss窗口          │ 打断条件            │ 设计意图    │
├──────────────────────────────────────────────────────┤
│ 普通窗口          │ 所有攻击可打断       │ 压制窗口    │
│ (非CFrame非SA)    │                      │             │
│ CFrame (C技霸体)  │ 招架 或 重攻击       │ 招架快/重攻击慢 │
│ SuperArmor (霸体) │ 招架 或 重攻击       │ 同上        │
│ AttackDraw        │ 不可打断             │ 收招保护    │
│ QTE攻击           │ 不可打断             │ 独立系统    │
└──────────────────────────────────────────────────────┘
```

**设计理由**：
- 招架仍然是**最快**的反制（立即打断 + 削韧），保持它的战术价值与「精准时机」定位
- 重攻击提供**第二条路**：慢但稳（能打断、能削韧、削满破势），BOSS 后半场不再只有招架一个答案
- Launch（挑飞）在 Boss 普通窗口可打断；不把 Launch 单独排除在霸体反制之外，是因其本身就是重攻击
- 不存在玩家用招架无法打断的 Boss 攻击（QTE 属于独立演出系统，不在此规则内）

### 3.3 Level 1 — 普通攻击
- 所有敌人默认的攻击类型
- `isCFrame = false`, `isSuperArmor = false`
- 被任何 `TakeDamage()` 调用打断（`CancelAttack()`）
- **意义**：玩家积极进攻即可压制杂兵，类似无双割草

### 3.4 Level 2 — C技（蓄力技）
- 由 `cAttackProbability` 控制触发概率
- 前摇阶段 `isCFrame = true`（霸体窗口）
- 伤害帧（`PerformAttack()`）结束后 `isCFrame = false`
- 非Boss: 招架 / 任何重攻击 伤害 → 打断成功（重攻击同时削韧）
- Boss: **招架 或 重攻击** 伤害 → 打断成功（重攻击同时削韧）
- 普通伤害 → 弹刀反馈（`PlayClankEffect()`：橙红闪烁+水平抖动）
- **意义**：为 Parry 赋予战术价值，避免它沦为"普通攻击的换皮"；重攻击提供慢但可靠的第二条路

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

### 4.7 防御姿态与破防（2026-10-25 定）

**入口**：只在**招架后摇**（招架的动作锁窗口）期间长按进入；进入后只要一直按着就维持，松手退出。
选择“只在招架后摇”是因为本项目的**长按已经是蓄力**（`chargeLevel` 0.3/0.6/1.0，蓄力门 1.0s，连段 C2 还占用“按住→松手”），占用任意时刻的“按住不动”会与蓄力整套重排。

**防御期间**：

- **免伤**：受到的伤害不扣血，只累积「破防值」（`AccumulatedDamage`）。
- 蓄力等级恒 0（不出现蓄力视觉），蓄力指示器让位。
- **滑动不出招**：必须先松手退出防御，再重新操作。

**破防**：

```
破防值 ≥ parryGuardBreakThreshold（默认 120）
  → 进入 Broken：GameplayInputGate 申请 GuardBroken 拦截
    → 不能行动、不能放技能、不能做手势输入
  → 持续 parryGuardBreakDuration（默认 1.5s）后自动恢复
```

破防值在停止挨打 `parryGuardDecayDelay`（默认 3s）后按 `parryGuardDecayPerSecond`（默认 30/秒）衰减。

**归属与数值来源**：`PlayerGuard`（`Assets/Scripts/Player/Combat/PlayerGuard.cs`）由 `PlayerState` 持有（与 Enemy → EnemyPoise 同做法，不新增场景组件）；全部数值与武器 Prefab 挂在**招架招式资产**的 `parryGuard*` 字段上（Inspector 可调、无幽灵引用）。

**实现注意**：武器销毁前必须先 `DOKill`（否则后摇 tween 会写已销毁的 Transform → MissingReferenceException）。

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
