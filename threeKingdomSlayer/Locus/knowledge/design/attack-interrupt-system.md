---
id: kd_c10dc057-dc52-4576-94f9-09869e1c6e5e
injectMode: inherit
summary: 敌人攻击打断系统设计文档：三级打断体系（普通攻击/C技/QTE）、霸体机制、与三选一奖励及Parry/Launch的交互关系、实现计划。已完成sharedHealthGroup打断修复和canInterruptCFrame参数传递，待实现P0-P2。
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
│ C技(蓄力技)│ 仅 Parry / Launch 打断 │ 需要正确对策     │
│ AttackDraw │ 不可打断               │ 收招保护         │
│ QTE攻击    │ 不可打断               │ BOSS终极技能     │
└─────────────────────────────────────────────────────┘
```

### 3.2 Boss敌人

Boss 的攻击打断规则与非Boss不同，核心原则：**Parry始终可打断Boss攻击，Launch仅在普通窗口可打断**。

```
┌──────────────────────────────────────────────────────┐
│ Boss窗口          │ 打断条件            │ 设计意图    │
├──────────────────────────────────────────────────────┤
│ 普通窗口          │ 所有攻击可打断       │ 压制窗口    │
│ (非CFrame非SA)    │                      │             │
│ CFrame (C技霸体)  │ 仅 Parry 可打断     │ Parry对策   │
│ SuperArmor (霸体) │ 仅 Parry 可打断     │ Parry对策   │
│ AttackDraw        │ 不可打断             │ 收招保护    │
│ QTE攻击           │ 不可打断             │ 独立系统    │
└──────────────────────────────────────────────────────┘
```

**设计理由**：
- Boss 的 CFrame/SuperArmor 窗口赋予 Parry 独特的战术价值——它是唯一能打断Boss霸体/蓄力攻击的手段
- Launch（挑飞）在 Boss CFrame/SuperArmor 窗口不能打断，避免 Launch 替代 Parry 的地位
- Boss 的普通窗口保持"所有攻击可打断"，给予玩家压制空间
- 不存在玩家用 Parry 无法打断的 Boss 攻击（QTE 属于独立演出系统，不在此规则内）

### 3.3 Level 1 — 普通攻击
- 所有敌人默认的攻击类型
- `isCFrame = false`, `isSuperArmor = false`
- 被任何 `TakeDamage()` 调用打断（`CancelAttack()`）
- **意义**：玩家积极进攻即可压制杂兵，类似无双割草

### 3.4 Level 2 — C技（蓄力技）
- 由 `cAttackProbability` 控制触发概率
- 前摇阶段 `isCFrame = true`（霸体窗口）
- 伤害帧（`PerformAttack()`）结束后 `isCFrame = false`
- 非Boss: Parry/Launch 伤害 → 打断成功
- Boss: **仅** Parry 伤害 → 打断成功
- 普通伤害 → 弹刀反馈（`PlayClankEffect()`：橙红闪烁+水平抖动）
- **意义**：为 Parry 赋予战术价值，避免它沦为"普通攻击的换皮"

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
            ├─ Parry：高风险高回报（精确时机 + 眩晕）
            ├─ Launch：挑空连段
            └─ 闪避：安全但损失输出窗口
         → BOSS 霸体/CFrame → 弹刀反馈 → 玩家必须：
            └─ Parry：唯一打断手段
         → BOSS QTE：强制演出，不可打断（Level 3）
```

## 5. 数据流

### 5.1 canInterruptCFrame 传递

```
伤害来源                      canInterruptCFrame   isParryInterrupt
──────────────────────────────────────────────────────────────────
Stab/Slash/Pierce/Sweep       false（默认）         false（默认）
Parry                          true                  true
Launch (via AttackWave)        true                  false
Launch (via WhirlwindController.ExecuteLaunch)  直接调用 enemy.Launch() —— 绕过 TakeDamage
Thunder (落雷)                 false（默认）         false（默认）
三选一全屏伤害                  false（默认）         false（默认）
Whirlwind tick damage          false（默认）         false（默认）
```

### 5.2 打断决策伪代码

```
if state == Attacking && isAttackAnimating && !isAttackDrawPhase:
    if isBoss:
        if isParryInterrupt:
            CancelAttack()    // Boss任何窗口：仅Parry可打断
        // else: 不打断（Launch等对其他窗口无效）
    else if !isSuperArmor && !isCFrame:
        CancelAttack()        // 非Boss普通窗口：所有攻击可打断
    else if canInterruptCFrame:
        CancelAttack()        // 非Boss CFrame：Parry/Launch可打断
```

### 5.3 关键传递路径
```
AttackSystem
  → AttackWave.Create(..., canInterruptCFrame, isParryInterrupt)
    → AttackWave.HitTarget()
      → Enemy.TakeDamage(damage, type, color, canInterruptCFrame, isParryInterrupt)
        → 打断检查
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
- Launch 命中 Boss CFrame → 应触发弹刀，不打断攻击
- Parry 命中 Boss CFrame → 应打断攻击 + 削Poise
- Parry 命中 Boss SuperArmor → 应打断攻击 + 削Poise
- Boss 普通窗口：所有攻击可打断
- 非Boss CFrame：Launch 仍可打断（行为不变）

## 10. Parry 架势边界修复（已实现）

- 普通敌人不参与 Parry 架势累计，不会因连续 Parry 进入 `Stunned`；Parry 对普通敌人的攻击打断规则保持不变。
- Boss 仅在 `BossState.InCombat`、`state == Attacking`、`isAttackAnimating == true` 且不处于 `AttackDraw` 时受到 Parry 架势伤害。
- `state == Attacking` 同时覆盖攻击冷却与攻击动画，不能单独作为架势伤害窗口；必须额外检查 `isAttackAnimating`。
- 修复原因：旧实现允许普通敌人累计架势并在归零后进入 Boss 专属眩晕流程，眩晕结束落入 `Idle` 后可能永久失去攻击调度。
