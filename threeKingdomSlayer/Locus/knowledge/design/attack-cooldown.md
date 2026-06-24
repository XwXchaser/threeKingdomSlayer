---
id: kd_5f380275-6449-4752-a084-ff230b75b378
type: design
path: attack-cooldown.md
title: attack-cooldown
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1782310710086
updatedAt: 1782311628330
---

# attack-cooldown

## Summary
攻击冷却双模式设计：独立技能CD（保留）vs 动作锁定（新增），设计意图与实施细节

## Content
# 攻击冷却双模式设计

## 概述

攻击系统支持两种冷却模式，通过 `AttackSystem.useActionBasedCooldown` 切换。

---

## 模式一：独立技能CD（旧，默认关闭）

每招独立冷却计时器。Stab 冷却中仍可 Slash。允许快速交替连打。

**保留原因**：未来可能作为奖励效果——例如技能"移除攻击硬直"，将动作锁定降级为独立CD。

## 模式二：动作锁定（新）

每次攻击执行后，全局锁定所有攻击输入。

### 设计意图

- 攻击动作时长 = 该攻击的视觉动画/特效总时长
- 锁定期间玩家无法做任何其他攻击动作（包括 Parry）
- 玩家自然感知到"因为还在挥刀，所以不能做其他动作"
- 攻速 buff 直接缩短锁定时长 → 攻速价值线性、即时兑现

### 锁定时长计算

```
baseDuration = max(actionDuration, visualEffectMinDuration)
实际锁定时长 = baseDuration / attackSpeedMultiplier
```

- `actionDuration`: 该攻击的基准动画时长（ScriptableObject 配置）
- `visualEffectMinDuration`: 视觉特效保底时长，由 `GetVisualEffectMinDuration()` 按攻击类型硬编码
- 使用 `max()` 确保动作锁至少覆盖 prefab 完整播放，避免特效未消失即可发起下一次攻击

### 各攻击视觉保底时长

| 攻击 | visualEffectMinDuration | 对应特效 |
|------|------------------------|---------|
| Slash | `slashSweepDuration + 0.28s` | SweepEffect sweep + interval + fade |
| Sweep | `slashSweepDuration + 0.28s` | 同上 |
| Stab | 0.5s | AttackWave thrust(0.2s) + retract(0.3s) |
| Pierce | 0.6s | AttackWave travel + interval + fade |
| Launch | 0.5s | AttackWave Fixed mode |
| Parry | 0s | 无视觉特效 |

### 各攻击当前配置值

| 攻击 | actionDuration | cooldown (旧模式) |
|------|---------------|------------------|
| Stab | 0.45 | 1.2 |
| Slash | 0.50 | 1.0 |
| Pierce | 0.50 | 5.0 |
| Sweep | 0.55 | 1.5 |
| Launch | 0.60 | 1.0 |
| Parry | 0.20 | 0.5 |

---

## 实施

- `AttackSystem._actionLockTimer` 在 Update 中递减
- `TryExecuteAttack` 入口检查锁状态
- 命中后根据模式分流：新模式设 `_actionLockTimer`，旧模式调 `PlayerState.StartCooldown`
- `GetVisualEffectMinDuration()` 返回各攻击类型的视觉保底时长，与 `actionDuration` 取最大值
- `PlayerState` 旧冷却逻辑完整保留

## 已知限制

- DOTween 动画时长暂不随攻速缩放。高攻速时锁定时长缩短但视觉特效固定，可能出现锁已解除但视觉仍在播放的轻微不同步（Phase 2 可考虑对 DOTween 序列应用 `timeScale` 同步）
- 未命中敌人时不消耗冷却（两种模式均保留此规则）
