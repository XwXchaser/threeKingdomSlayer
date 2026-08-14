---
id: kd_e9071eeb-e037-4bf8-87f1-627e0927e3ee
injectMode: inherit
summary: Parry 格挡视觉实现：纯 Z 轴旋转（54,270,45°→145°），以玩家位置为基准生成，反弹飞行物和伤害敌人两个分支都播放视觉。QTE 飞行物通过 isQTEProjectile 标记隔离，不被常规 Parry 反弹。v3 新增 QTE 防御格挡表现（摄像机空间矛举起+自转+三帧精灵）。
aiMaintained: inherit
---

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

## 六、QTE 防御格挡表现（新增）

### 触发时机

QTE 防御型攻击（`isDefensiveQTE = true`）中，玩家划动方向正确 → `DeflectArrowWave()` + `PlayBlockVisual()`

### 实现（QTEController.PlayBlockVisual）

以摄像机为父节点，创建两层结构：

- **父节点** (`QTE_BlockVFX`)：负责 X 轴旋转（举起动作 90°→0°），`DOLocalRotate` + `Ease.InOutQuad`
- **子节点**（实例化的 stab prefab）：负责 Z 轴自转（0°→900°，2.5 圈），`DOLocalRotate` + `Ease.Linear` + `FastBeyond360`
- **三帧精灵切换**：stab → stab_rotate1 → stab_rotate2，均匀分配在 duration 内
- **末尾 20% fadeout**：动画最后 20% 时间 sprite 透明度从 1 渐变至 0
- 动画结束后销毁父节点

### Inspector 参数（QTEController）

| 字段 | 默认 | 说明 |
|------|------|------|
| `stabBlockEffectPrefab` | — | 矛 prefab |
| `stabBlockSprite` | — | 格挡帧1 |
| `stabBlockRotateSprite1` | — | 格挡帧2 |
| `stabBlockRotateSprite2` | — | 格挡帧3 |
| `stabBlockDuration` | 0.5 | 总时长（秒）|
| `stabBlockDistance` | 3.0 | 摄像机前方距离 |
| `stabBlockScale` | (0.15, 0.15, 0.15) | 缩放 |

### QTE 箭矢着弹点

- `QTEAttackConfig.arrowTargetY`：箭矢到达时 Y 坐标，调整伤害触发位置贴近玩家
- `EnemyProjectile.Launch()` 新增 `endY` 参数，抛物线终点使用此值替代 `startPos.y`

---

## 七、涉及文件

| 文件 | 改动 |
|------|------|
| `Assets/Scripts/Core/AttackSkillConfig.cs` | Parry 6 字段 + Launch 7 字段 |
| `Assets/Scripts/Player/AttackSystem.cs` | `PlayParryVisual()` + `PlayLaunchVisual()` + sprite 引用 |
| `Assets/Scripts/Attack/SweepEffect.cs` | `Create()` 新增 rotate sprite 参数 + flipX |
| `Assets/Scripts/Enemy/EnemyProjectile.cs` | `isQTEProjectile` 标记 + `endY` 参数 |
| `Assets/Scripts/QTE/QTEController.cs` | QTE 箭矢设 `isQTEProjectile = true` + `PlayBlockVisual()` |
| `Assets/Scripts/QTE/QTEAttackConfig.cs` | `arrowTargetY` 字段 |
| `Assets/Prefabs/UI/Skills/Zhangfei_Parry.asset` | 6 个 parry 参数 |
| `Assets/Prefabs/UI/Skills/Zhangfei_Launch.asset` | 7 个 launch 参数 + attackWavePrefab |
