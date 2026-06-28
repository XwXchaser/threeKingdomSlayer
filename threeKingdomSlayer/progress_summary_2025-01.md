# 蓄力Build三选一 — 开发进度总结

> 更新时间: 2025-01

## 整体进度

| 选项 | 类型 | 状态 |
|---|---|---|
| 蓄力减伤 (charge_damage_reduction) | Numeric | ✅ 已完成 |
| 反伤盾 (charge_reflect_shield) | Numeric | ✅ 已完成 |
| 冲击波跟随 (charge_shockwave) | TimedPassive | ✅ 已完成 |
| 顺序调转 (sweep_reverse_order) | AttackPassive | ⏳ 待实现 |

## 本次完成: 蓄力冲击波

### 机制设计

- **攒波模式**: 计时器每{0}s tick一次，不立即生成特效，而是积攒1层层数
- **蓄力叠加**: 蓄力状态下可无限叠层（冷却继续转）；非蓄力时层数上限=1
- **一并释放**: 蓄力攻击(Pierce/Sweep/Launch)命中时，消费所有层数并行释放
  - 每层 = {1}道冲击波
  - 每道伤害 = baseDamage × (1 + layer × stackDamageBonus)
- **视觉**: 复用 Sweep 的 attackWavePrefab

### 配置 (ChargeShockwaveLevelConfig)

| 参数 | 说明 |
|---|---|
| intervalSeconds | {0} 攒波间隔 |
| shockwaveCount | {1} 每次波数 |
| rangeRows | {2} 射程排数 |
| baseDamage | {3} 基础伤害 |
| stackDamageBonus | {4} 每层增伤% |

### 5级默认配置

| 等级 | 间隔 | 波数 | 排数 | 基础伤害 | 每层增伤 |
|---|---|---|---|---|---|
| Lv1 | 8s | 1 | 2 | 15 | 10% |
| Lv2 | 7s | 1 | 2 | 20 | 12% |
| Lv3 | 6s | 2 | 3 | 25 | 15% |
| Lv4 | 5s | 2 | 3 | 30 | 18% |
| Lv5 | 4s | 3 | 3 | 35 | 20% |

### 描述模板

`每{0}秒积攒{1}道射程{2}排造成{3}点伤害的冲击波，在蓄力攻击时一并释放。可在蓄力时叠加，每层伤害+{4}%`

### 改动文件清单

| 文件 | 改动内容 |
|---|---|
| `PlayerState.cs` | 新增 `public bool IsCharging` 属性 |
| `TimedPassiveModule.cs` | 新增 `_shockwaveLayers` 层数追踪 + `AccumulateShockwave()` + `ConsumeAllShockwaves()` API + Update中非蓄力时层数上限=1 + `ShockwaveConsumeResult` struct |
| `UpgradeEffectManager.cs` | `GetDescription` 新增 `charge_shockwave` 分支({0}~{4}) |
| `AttackSystem.cs` | 新增 `ReleaseChargeShockwaves()` 辅助方法，在 ExecutePierce/Sweep/Launch 伤害前调用 |
| `UpgradeDefinition.cs` | 新增 `ChargeShockwaveLevelConfig` struct + `chargeShockwaveLevels` 列表 + `GetTriggerInterval` case |
| `UpgradeDefinitionEditor.cs` | 绑定 `chargeShockwaveLevelsProp` + `DrawChargeShockwaveSection()` |
| `ChargeShockwave.asset` | 新建资产，5级配置 |
| `UpgradePoolConfig.asset` | 新增 charge_shockwave 到 commonPool (权重10) |

### 执行管线

```
蓄力攻击触发 → 快照目标 → 释放冲击波(多波并行) → 主攻击伤害
```

### 待配

- icon: 当前为 null

---

## 之前完成: 反伤盾

- effectType: `charge_reflect_shield`
- 独立配置结构 `ReflectShieldLevelConfig` (intervalSeconds + reflectPercent)
- Timer在 `UpgradeEffectManager.Update()` 中运行
- 单次次数盾，不可叠加，消耗后重新计时
- 反弹基于原始伤害，先于减伤 (反弹→parry减伤→蓄力减伤)
- 首次获得立即给盾
- 自定义Editor (`DrawReflectShieldSection`)

## 之前完成: 蓄力减伤

- effectType: `charge_damage_reduction`
- 使用 `numericLevels.floatValue`（每级独立可配）
- PlayerState订阅 `InputManager.OnChargeBegan/Ended` 控制 `_isCharging`
- 减伤在 `TakeDamage` 中应用，与 parry 减伤乘法叠加

---

## 新增美术素材

| 文件 | 用途 |
|---|---|
| `Assets/Sprites/31Reward/icon/icon_31_chargingArmor.png` | 蓄力减伤 icon |
| `Assets/Sprites/31Reward/icon/icon_31_thornArmor.png` | 反伤盾 icon |
| `Assets/Sprites/31Reward/icon/icon_31_timedFire.png` | 喷火 icon |
| `Assets/Sprites/EffectSprites/thornArmor/` | 反伤盾特效序列帧 |

---

## 待实现: 顺序调转

- effectType: `sweep_reverse_order`
- 类型: AttackPassive
- 每{0}次攻击后，下一次 Sweep 反转列内敌人前后顺序
- 执行顺序: 反转 → 位移 → 伤害
- 需改动: PassiveTriggerModule, ColumnManager, AttackSystem
