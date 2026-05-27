---
id: kd_c10dc057-dc52-4576-94f9-09869e1c6e5e
type: design
path: attack-interrupt-system.md
title: attack-interrupt-system
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1779896984696
updatedAt: 1779897548899
---

# attack-interrupt-system

## Summary
敌人攻击打断系统设计文档：三级打断体系（普通攻击/C技/QTE）、霸体机制、与三选一奖励及Parry/Launch的交互关系、实现计划。已完成sharedHealthGroup打断修复和canInterruptCFrame参数传递，待实现P0-P2。

## Content
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

```
┌─────────────────────────────────────────────────────┐
│ 层级     │ 攻击类型    │ 打断条件              │ 设计意图       │
├─────────────────────────────────────────────────────┤
│ Level 1  │ 普通攻击    │ 任何直接伤害即可打断   │ 割草压制       │
│ Level 2  │ C技(蓄力技) │ 仅 Parry / Launch 打断 │ 需要正确对策   │
│ Level 3  │ QTE攻击    │ 不可打断               │ BOSS终极技能   │
└─────────────────────────────────────────────────────┘
```

### 3.1 Level 1 — 普通攻击
- 所有敌人默认的攻击类型
- `isCFrame = false`
- 被任何 `TakeDamage()` 调用打断（`CancelAttack()`）
- **意义**：玩家积极进攻即可压制杂兵，类似无双割草

### 3.2 Level 2 — C技（蓄力技）
- 由 `cAttackProbability` 控制触发概率
- 前摇阶段 `isCFrame = true`（霸体窗口）
- 伤害帧（`PerformAttack()`）结束后 `isCFrame = false`
- 普通伤害 → 弹刀反馈（`PlayClankEffect()`：橙红闪烁+水平抖动）
- Parry/Launch 伤害 → 打断成功
- **意义**：为 Parry 和 Launch 赋予战术价值，避免它们沦为"普通攻击的换皮"

### 3.3 Level 3 — QTE攻击（BOSS专属）
- `state == EnemyState.QTEAttacking`
- `TakeDamage()` 首行直接 return，完全跳过伤害和打断
- 由 `QTEController` 驱动，独立于普通攻击状态机

## 4. 霸体（Super Armor）设计

### 4.1 当前实现
C技的前摇阶段即为"霸体窗口"：
- `isCFrame = true` 期间，非 Parry/Launch 伤害只触发弹刀
- 弹刀反馈：橙红闪白 + 水平抖动，区别于普通受击的白色闪白 + 缩放抖动
- 敌人不会因弹刀而停止攻击

### 4.2 设计考量

霸体不是独立系统，而是 C技 的自然属性：

| 维度 | 设计 |
|------|------|
| 触发条件 | 仅 C技 前摇阶段 |
| 玩家反制 | Parry（格挡反击）、Launch（挑空） |
| 视觉反馈 | 弹刀特效（橙红闪烁+水平抖动） |
| 节奏影响 | 玩家无法无脑压制所有敌人，需要识别 C技 前摇并选择对策 |

### 4.3 与战斗循环的关系

```
玩家进攻 → 杂兵被压制（Level 1 打断）
         → 遇到 C技敌人 → 弹刀反馈 → 玩家选择：
            ├─ Parry：高风险高回报（精确时机 + 眩晕）
            ├─ Launch：挑空连段
            └─ 闪避：安全但损失输出窗口
         → BOSS QTE：强制演出，不可打断（Level 3）
```

**与三选一奖励的交互**：
- 三选一技能伤害（如落雷、全屏伤害）默认 `canInterruptCFrame = false`
- 它们能打断普通攻击，但不能打断 C技
- 保留了"三选一强力但不过度 trivialize C技博弈"的平衡

**Parry/Launch 的战术价值**：
- 在没有 C技霸体的世界里，Parry/Launch 只是"另一种造成伤害的方式"
- C技霸体赋予了它们独特的战术意义：**唯一能打断 C技 的手段**
- 这符合鬼泣的设计哲学：不同招式有不同的功能性用途

## 5. 数据流

```
伤害来源                      canInterruptCFrame
─────────────────────────────────────────────────
Stab/Slash/Pierce/Sweep       false（默认）
Parry                          true
Launch (via AttackWave)        true
Launch (via WhirlwindController.ExecuteLaunch)  直接调用 enemy.Launch() —— 绕过 TakeDamage
Thunder (落雷)                 false（默认）
三选一全屏伤害                  false（默认）
Whirlwind tick damage          false（默认）
```

### 5.1 关键传递路径
```
AttackSystem
  → AttackWave.Create(..., canInterruptCFrame)
    → AttackWave.HitTarget()
      → Enemy.TakeDamage(damage, type, color, canInterruptCFrame)
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

### 7.1 共享血量组打断传递（P0）
**问题**：当共享血量组中成员A被命中但不在攻击中，成员B正在攻击中时，B的攻击不会被中断。

**方案**：在 `SharedHealthGroup.TakeDamage()` 中遍历成员，对正在攻击的成员调用打断逻辑。

### 7.2 WhirlwindController.ExecuteLaunch() 绕过 TakeDamage（P1）
**问题**：漩涡击飞直接调用 `enemy.Launch()`，不经过 `TakeDamage()`，跳过了 C技 打断检查。
**影响**：漩涡可以打断 C技（可能是有意的，需确认）。

### 7.3 旅行波到达时序优化（P2）
**问题**：Pierce/Sweep 的波飞行时间可能超过敌人的 AttackSpawn 窗口（尤其在 spawnDuration 较短时），导致波到达时敌人已进入 AttackDraw。
**方案**：
- 方案A：增大默认 `attackSpawnDuration`（让窗口更长）
- 方案B：旅行波命中时无视 `isAttackDrawPhase`（让整个攻击动画都可打断）
- 方案C：在波创建时快照目标敌人的状态，到达时使用快照状态判断（不推荐，过于复杂）

### 7.4 三选一技能伤害接入 `canInterruptCFrame`（P2）
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
- 普通攻击（非C技）被 Stab/Slash 打断
- 共享血量组中非命中成员的攻击打断传递
- Pierce/Sweep 旅行波打断普通攻击的窗口期充足性
