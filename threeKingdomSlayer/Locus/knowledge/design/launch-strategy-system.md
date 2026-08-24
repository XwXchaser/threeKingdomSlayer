---
id: kd_ef0164bf-5c2b-4fc7-91ba-0cce6713fb22
injectMode: inherit
summary: 以击飞为核心机制的3选1策略升级体系设计方案，覆盖触发、增强、连锁三个维度共5个升级提案。
aiEditMode: inherit
---

# 击飞 3 选 1 策略体系 — 设计方案

## 概述

以"击飞（Launch）"为核心机制，设计一套覆盖触发、增强、连锁三个维度的 3 选 1 升级体系，让玩家可以构建击飞流派。

## 当前击飞系统基础

| 属性 | 默认值 | 说明 |
|------|--------|------|
| 时长 | 2s | `launchDuration`，可通过 `Launch(customDuration)` 覆盖 |
| 随机高度 | 1.5~4.5 | `launchYHeightMin/Max` |
| 重力 | 20 | `launchGravity` |
| 反弹速度 | 8 | 空中受击时的反弹速度 |
| 空中受击延长 | +0.5s | `launchedHitExtendDuration` |
| 空中伤害倍率 | 1.5x | `launchedDamageTakenMultiplier` |
| 落地眩晕保留 | 自动 | 击飞前剩余 stun 时间保存/恢复 |

### 击飞触发入口

| 入口 | 类型 | 经 TakeDamage |
|------|------|--------------|
| 玩家挑飞攻击 | AttackSystem.ExecuteLaunch | ✅ |
| 漩涡自动击飞 | WhirlwindController | ❌ (直接 Launch) |
| 旋风被动 | TimedPassiveModule.SpawnCyclone | 先 TakeDamage 再 Launch |
| 幻影挑飞 | AttackSystem.ExecutePhantomAttack | ✅ |

### 击飞门槛 (CanBeLaunched)

- ForceLaunch Buff 活跃 → 无条件
- Boss → 仅眩晕中
- 普通敌人 → `currentPoise ≤ poiseDamage`

---

## 升级方案

### A. 触发维度 — 怎么触发击飞

#### ① ForceLaunch Buff — "霸王之力"
- **类型**：TimedPassive
- **效果**：获得 ForceLaunch Buff，持续 X 秒。期间无视 CanBeLaunched 门槛，可无条件击飞 Boss
- **等级成长**：
  | Lv | 持续(s) |
  |----|---------|
  | 1 | 5 |
  | 2 | 7 |
  | 3 | 9 |
  | 4 | 11 |
  | 5 | 15 |
- **设计意图**：Boss 战突破口。平时 Boss 必须眩晕才能击飞，此升级可主动挑飞 Boss 创造空中输出窗口

#### ② ProbabilityLaunch 实现 — "破军之势"
- **类型**：Numeric（被动概率）
- **效果**：攻击无法击飞的敌人时，有 X% 概率强制眩晕并击飞
- **等级成长**：
  | Lv | 概率 |
  |----|------|
  | 1 | 15% |
  | 2 | 20% |
  | 3 | 25% |
  | 4 | 30% |
  | 5 | 40% |
- **设计意图**：让非挑飞攻击也有概率触发击飞，提供不确定性击飞来源

### B. 增强维度 — 击飞后发生什么

#### ③ Aerial Hunter — "空中追击"
- **类型**：Numeric
- **效果**：击飞中敌人受到的伤害倍率从默认 1.5x 提升至 X 倍
- **等级成长**：
  | Lv | 倍率 |
  |----|------|
  | 1 | 1.8x |
  | 2 | 2.1x |
  | 3 | 2.5x |
  | 4 | 3.0x |
  | 5 | 4.0x |
- **设计意图**：让击飞成为爆发窗口，鼓励击飞→空中连段打法

### C. 连锁维度 — 击飞与其他系统联动

#### ④ Landing Impact — "陨石坠落"
- **类型**：AttackPassive（监听 OnLaunchedLanded）
- **效果**：被击飞的敌人落地时，对落地位置周围 Y 格内的敌人造成击飞伤害的 X%
- **等级成长**：
  | Lv | AOE 伤害% |
  |----|-----------|
  | 1 | 30% |
  | 2 | 40% |
  | 3 | 50% |
  | 4 | 60% |
  | 5 | 80% |
- **设计意图**：让击飞产生群伤。密集敌人击飞→落地互相伤害→链式反应

#### ⑤ Launch Chain — "连环击飞"
- **类型**：AttackPassive
- **效果**：击飞敌人时，若该敌人周围 X 格内有其他可击飞敌人，有 Y% 概率一并击飞
- **等级成长**：
  | Lv | 范围(格) | 概率 |
  |----|----------|------|
  | 1 | 1 | 40% |
  | 2 | 1 | 60% |
  | 3 | 2 | 60% |
  | 4 | 2 | 80% |
  | 5 | 3 | 100% |
- **设计意图**：一次性击飞大片敌人，配合空中追击+落地AOE形成清场 combo

---

## Build 路线

| 路线 | 升级组合 | 打法 |
|------|---------|------|
| Boss 杀手 | ①+③ | 主动击飞Boss→4x空中爆发 |
| 清场 | ⑤+④ | 一片击飞→落地全场AOE |
| 混合 | ②+③ | 随缘击飞→高额追击 |

---

## 实现状态

- [ ] ① 霸王之力 (ForceLaunch Buff)
- [ ] ② 破军之势 (ProbabilityLaunch)
- [ ] ③ 空中追击 (Aerial Hunter)
- [ ] ④ 陨石坠落 (Landing Impact)
- [ ] ⑤ 连环击飞 (Launch Chain)

## 已有击飞升级

- ✅ Cyclone (旋风) — TimedPassive，周期性随机击飞+落地伤害
