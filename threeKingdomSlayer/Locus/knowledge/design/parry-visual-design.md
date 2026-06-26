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
updatedAt: 1782468531869
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

## 四、Launch 挑飞视觉（对称实现）

Launch 与 Parry 形成攻防对称：

| | Parry | Launch |
|---|---|---|
| 语义 | 防御格挡 | 攻击上挑 |
| 运动端 | 枪尾上挑 | 枪头上挑 |
| X/Y 轴 | (54, 270) | (35, 90) — 180°翻转 |
| Z 旋转 | 45°→145°（+100°） | 140°→40°（-90°） |
| 位移 | 无 | Y 上升 launchRiseHeight(1.0) |
| 时长 | 0.25s | 0.20s |
| 颜色 | 白 | 暖金 (1.0, 0.85, 0.3) |
| 位置 | 玩家身前 | 玩家身前 |

### Launch Config 字段

| 字段 | 默认 | 说明 |
|------|------|------|
| `launchFlickAngle` | 90 | Z 旋转幅度 |
| `launchFlickDuration` | 0.20 | 上挑时长 |
| `launchSpawnX/Y/ZOffset` | 0 / 1.5 / 0 | 生成位置偏移 |
| `launchAngleVariance` | 15 | Z 起始角随机偏差 |
| `launchRiseHeight` | 1.0 | Y 轴上升高度 |

---

## 五、stab_rotate 三帧动画

Launch 和 Slash 使用 stab_rotate1/2 素材做 flipbook 动画：

- 帧序：stab → rotate1 → rotate2（各 1/3 duration）
- AttackSystem 挂载 `_stabRotate1Sprite` / `_stabRotate2Sprite` 引用
- PlayLaunchVisual 和 SweepEffect.Create 中通过 seq.Insert 插入 sprite 切换
- Slash R→L 时 flipX 翻转素材，使枪头朝向运动方向

---

## 六、涉及文件

| 文件 | 改动 |
|------|------|
| `Assets/Scripts/Core/AttackSkillConfig.cs` | Parry 6 字段 + Launch 7 字段 |
| `Assets/Scripts/Player/AttackSystem.cs` | `PlayParryVisual()` + `PlayLaunchVisual()` + sprite 引用 |
| `Assets/Scripts/Attack/SweepEffect.cs` | `Create()` 新增 rotate sprite 参数 + flipX |
| `Assets/Scripts/Enemy/EnemyProjectile.cs` | `isQTEProjectile` 标记 |
| `Assets/Scripts/QTE/QTEController.cs` | QTE 箭矢设 `isQTEProjectile = true` |
| `Assets/Prefabs/UI/Skills/Zhangfei_Parry.asset` | 6 个 parry 参数 |
| `Assets/Prefabs/UI/Skills/Zhangfei_Launch.asset` | 7 个 launch 参数 + attackWavePrefab |
