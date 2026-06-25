---
id: kd_e9071eeb-e037-4bf8-87f1-627e0927e3ee
type: design
path: parry-visual-design.md
title: parry-visual-design
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1782387109895
updatedAt: 1782392200123
---

# parry-visual-design

## Summary
Parry 格挡视觉实现：纯 Z 轴旋转（54,270,45°→145°），以玩家位置为基准生成，反弹飞行物和伤害敌人两个分支都播放视觉。QTE 飞行物通过 isQTEProjectile 标记隔离，不被常规 Parry 反弹。

## Content
# Parry 格挡视觉实现方案（v2 — 纯 Z 轴旋转）

## 需求

用 Stab prefab 做**纯 Z 轴旋转**：从 `(54, 270, 45°)` 扫到 `(54, 270, 145°)`，即枪尾从下往上挑 ~100°，表现用枪尾格挡敌人攻击。不移动位置，仅旋转。每次 Parry 的 Z 起始角有 ±15° 随机偏差。

---

## 一、核心设计

### 旋转轴

- X=54, Y=270（prefab 在场景中的实测有效朝向）
- Z 轴旋转：起点 45°，终点 145°（扫掠 100°）
- 每次随机偏移 Z 起始角 ±`parryAngleVariance`（默认 15°）

### 位置

- 生成位置 = **玩家位置** + `parrySpawnXOffset`(默认0) + `parrySpawnYOffset`(默认1.5) + `parrySpawnZOffset`(默认0)
- 不做 DOMove，纯旋转
- 反弹飞行物和伤害敌人两个分支**都播放视觉**

### DOTween 序列

```
scale-in(0→targetScale, 0.05s)  ← 并行
Sequence:
  DORotate(endEuler, parrySweepDuration)  ← 纯 Z 旋转
  AppendInterval(0.03s)
  DOFade(0, 0.15s)
OnComplete/OnKill: Destroy
```

---

## 二、Inspector 参数（AttackSkillConfig）

| 字段 | 范围 | 默认 | 说明 |
|------|------|------|------|
| `parrySweepAngle` | 30-180 | 100 | Z 轴旋转幅度（度）|
| `parrySweepDuration` | 0.1-0.5 | 0.25 | 扫掠时长（秒）|
| `parrySpawnXOffset` | — | 0 | 生成位置 X 偏移（相对玩家）|
| `parrySpawnYOffset` | — | 1.5 | 生成位置 Y 偏移（相对玩家）|
| `parrySpawnZOffset` | — | 0 | 生成位置 Z 偏移（相对玩家）|
| `parryAngleVariance` | 0-30 | 15 | Z 起始角随机偏移范围 |

---

## 三、时序分析

```
Parry cooldown: 0.50s
actionDuration: 0.20s (无实际动画，仅做锁定参考)

Visual timeline:
  t=0.00s  scale-in 开始 (0.05s)
  t=0.00s  Z旋转开始 (parrySweepDuration = 0.25s)
  t=0.25s  旋转结束
  t=0.28s  interval 结束
  t=0.43s  fade-out 结束, Destroy

Visual total: ~0.43s < cooldown 0.50s ✓
```

---

## 四、涉及文件

| 文件 | 改动 |
|------|------|
| `Assets/Scripts/Core/AttackSkillConfig.cs` | 新增 6 个 parry 字段（`[Header("招架扫掠")]`）|
| `Assets/Scripts/Player/AttackSystem.cs` | `PlayParryVisual()` 纯旋转 DOTween；`ExecuteParry()` 两分支都播视觉 + 跳过 QTE 飞行物 |
| `Assets/Scripts/Enemy/EnemyProjectile.cs` | 新增 `isQTEProjectile` 标记 |
| `Assets/Scripts/QTE/QTEController.cs` | 创建 QTE 箭矢时设 `isQTEProjectile = true` |
| `Assets/Prefabs/UI/Skills/Zhangfei_Parry.asset` | 设置 6 个 parry 参数 |

---

## 五、潜在问题

### 5.1 attackWavePrefab GUID 已损坏

GUID `3ebe15c25c9ebda4fb9cd0b5ea7df39c` 在所有 Zhangfei skill config 中均损坏。Parry/Slash/Stab 均 fallback 到 Quad。Quad 无方向性，枪尾/枪尖区分暂不适用。

### 5.2 Z 旋转方向感

Quad 是对称矩形，旋转时两种方向看起来一样。当 prefab 恢复后，需要用非对称 sprite 区分头尾。

### 5.3 连续 Parry

Parry cooldown 0.50s，visual ~0.43s。连续 Parry 时前一个 visual 会自然销毁，不会残留。OnKill 兜底。

### 5.4 反弹飞行物也有视觉

两个分支都调用 `PlayParryVisual(cfg, playerPos)`，反弹箭矢时同样在玩家身前播放格挡特效。

### 5.5 QTE 飞行物不被 Parry 反弹

`EnemyProjectile.isQTEProjectile` 标记由 `QTEController` 在创建 QTE 箭矢时设为 `true`。`ExecuteParry()` 的 `FindObjectsOfType<EnemyProjectile>()` 遍历中跳过 QTE 飞行物。QTE 箭矢由独立的 `QTEController.DeflectArrowWave()` 系统处理。

### 5.6 无 Animator

Player 无 Animator 组件，所有动作表现均为 DOTween 特效驱动。Parry 的 `actionDuration=0.20s` 在视觉系统中无直接用途，锁定由 `cooldown=0.50s` 控制。

### 5.7 Time.timeScale

`seq.SetUpdate(true)` 忽略 timeScale，三选一暂停期间不受影响。

---

## 六、测试清单

- [ ] 单次 Parry：白色 Quad 在**玩家位置**做 Z 轴旋转（45°→145°），不位移
- [ ] 多次 Parry：每次起始角度有可见偏差（±15°）
- [ ] 连续 Parry：快速连续执行，无特效残留
- [ ] 反弹普通飞行物：同时播放格挡视觉
- [ ] 无目标：不播放特效
- [ ] QTE 箭矢：Parry 不反弹 QTE 飞行物，箭矢正常飞向玩家
- [ ] 三选一暂停：不影响（暂停期间不能触发攻击）
- [ ] Inspector：Zhangfei_Parry.asset 中 6 个 parry 参数可调
