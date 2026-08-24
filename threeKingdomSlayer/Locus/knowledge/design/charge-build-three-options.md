---
id: kd_cbd967f3-a57a-4000-8fb9-a14f5914a012
injectMode: inherit
summary: 蓄力Build三选一设计文档：蓄力减伤(Numeric)、顺序调转(AttackPassive)、冲击波跟随(TimedPassive)的设计目的、技术难点、管线细节与改动清单
aiEditMode: inherit
---

# 蓄力 Build 三选一设计

> **已实现**: 蓄力减伤 ✅ | 反伤盾 ✅ | 冲击波跟随 ✅ | 顺序调转 ⏳

## 一、设计目的与方向

### 背景

当前 commonPool 选项围绕**快攻-位移线**：通过高频攻击位移将敌人赶进伤害区（烈焰喷射/箭雨/地刺），攻速加速循环，攻击触发箭矢。

蓄力攻击（Pierce / Sweep）因天然需要蓄力窗口，在这个体系中是"纯代价"——更长的动作锁、更高的被打风险，但没有专属的 build 回报。

### 设计目标

新增一组围绕蓄力攻击特性的选项，将蓄力窗口从"代价"改造为"资产"，形成与快攻线并行的**蓄力-爆发线**：

```
快攻线：Stab/Slash → 高频位移 → 赶敌进伤害区 → 持续削血
蓄力线：Pierce/Sweep → 蓄力蓄能 → 叠波/调转/站桩 → 节奏爆发
```

两条线共享终点（伤害区收割），但路线不同。玩家看到选项就能理解 build 方向，不会冲突。

### 四个选项

| 选项 | 类型 | 效果 | 状态 |
|---|---|---|---|
| 蓄力减伤 | Numeric | 蓄力时获得 {0}% 减伤 | ✅ |
| 反伤盾 | Numeric | 每 {0}s 获得1层盾，蓄力受击时反弹 {1}% 伤害 | ✅ |
| 顺序调转 | AttackPassive | 每 {0} 次攻击后，下一次 Sweep 反转击中列内敌人前后顺序 | ⏳ |
| 冲击波跟随 | TimedPassive | 每 {0}s 攒 {1} 道冲击波（距离 {2}），蓄力攻击时一并释放，每叠 1 层伤害 +{3}% | ✅ |

---

## 二、核心难点

### 1. 执行顺序

蓄力攻击的完整管线必须明确规定反转、位移、伤害三者的先后。结论：

```
反转 → 位移 → 伤害
```

原因：
- 反转必须在伤害前：否则前排目标死后压缩，反转形同虚设
- 位移必须在反转后：推的是新排位上的敌人（原后排高危翻上来后被推入伤害区）
- 伤害在最后：保证所有前置布局（反转、位移、冲击波）都基于同一次攻击的最终目标快照

冲击波位置：在位移后、伤害前。基于位移后的新位置同时释放多波（并行命中），避免串行时前一波推走敌人导致后续波次打空。

### 2. 反伤盾的执行顺序

反伤盾在 TakeDamage 中的顺序：

```
反弹(基于原始伤害) → parry减伤 → 蓄力减伤
```

理由：反弹基于原始伤害，先于减伤；鼓励玩家用其他方式（反伤）代替 parry 应对敌人攻击。

### 3. 冲击波"生成 ≠ 释放"模式

现有的 TimedPassive 都是"计时器到 → 立刻生成特效"。冲击波需要新模式：计时器到 → 攒入队列，等待蓄力攻击时清空释放。

需要在 TimedPassiveModule 中新增"队列型"分支，同时保持现有"即时型"不受影响。

### 4. 顺序调转的触发方式

`每 {0} 次攻击后，下一次 Sweep 附带效果` —— 这是"计数 → 设标志 → 特定攻击消耗标志"的模式。

现有 PassiveTriggerModule 只支持"计数 → 立即生成特效"。需要新增 case：到达阈值时设全局标志，由 ExecuteSweep 消费。

### 5. Charge 事件的接入口

蓄力状态（isCharged）只在 InputManager 中存在，PlayerState 和 AttackSystem 都感知不到。蓄力减伤需要 PlayerState.TakeDamage 能判断"是否在蓄力"。

方案：利用 InputManager 已有的 `OnChargeBegan / OnChargeEnded` 事件，PlayerState 订阅设置内部标志。

---

## 三、细节要素

### 蓄力减伤

- effectType: `charge_damage_reduction`
- category: Numeric (0)
- 数值来源: `numericLevels.floatValue`（每级独立可配）
- 减伤在 `TakeDamage` 中应用，与 parry 减伤叠加
- 蓄力标志来源: `InputManager.OnChargeBegan / OnChargeEnded`
- 蓄力标志在 `pressDuration >= minChargeTime` 时才立起（非按下瞬间），当前 minChargeTime=1s

### 反伤盾

- effectType: `charge_reflect_shield`
- category: Numeric (0)
- 配置结构: `ReflectShieldLevelConfig`
  - `intervalSeconds` — {0} 护盾生成间隔（秒）
  - `reflectPercent` — {1} 反弹伤害比例（0.2=20%）
- Timer 逻辑: `UpgradeEffectManager.Update()` 倒计时，盾存在时停转，消耗后重启
- 盾状态: 单次次数盾，不可叠加，消耗后重新计时
- 反弹时机: `PlayerState.TakeDamage` 中，基于原始伤害先反弹再减伤
- 反弹目标: `source.TakeDamage(originalDamage * reflectPercent)`
- 蓄力条件: 仅 `_isCharging == true` 时触发
- 首次获得时立即给盾（`_hasReflectShield = true`）

### 顺序调转

- effectType: `sweep_reverse_order`
- category: AttackPassive (2)
- 触发阈值: `intValue`（每 X 次攻击）
- 反转逻辑: `ColumnManager.ReverseColumnOrder(List<Enemy> targets)`
  - 提取 targets 中不重复列索引
  - 每列 `enemies.Reverse()`
  - 重新分配 rowIndex（index 0 = row0, index 1 = row1...）
- 执行位置: ExecuteSweep 伤害前、位移前

### 冲击波跟随

- effectType: `charge_shockwave`
- category: TimedPassive (3)
- 配置结构: `ChargeShockwaveLevelConfig`
  - `intervalSeconds` — {0} 生成间隔
  - `shockwaveCount` — {1} 每轮生成数量
  - `rangeRows` — {2} 冲击波距离
  - `stackDamageBonus` — {3} 每层增伤比例
- 队列: `TimedPassiveModule` 维护 `Dictionary<string, int> _pendingShockwaveCounts`
- 释放: `ExecutePierce / ExecuteSweep` 位移后、伤害前调用 `ConsumeShockwaves(upgradeId)`
- 并行释放: 所有 pending 波次基于同帧目标快照，伤害逐一递增
- 冲击波形态: AttackWave(Travel)，从玩家位置沿 Z 飞出

### 完整 ExecuteSweep 管线

```
1. 快照目标（原排位）
2. [如有] 反转 → ColumnManager.ReverseColumnOrder
3. 快照目标（新排位）
4. [如有] 位移 → ApplySlashDirectionalPush
5. [如有] 冲击波释放 → 并行多波
6. 伤害 → AttackWave.Create
```

### ExecutePierce 管线（无反转/位移，仅冲击波）

```
1. 快照目标
2. [如有] 冲击波释放 → 并行多波
3. 伤害 → AttackWave.Create(Travel)
```

---

## 四、改动文件清单

| 文件 | 蓄力减伤 | 反伤盾 | 顺序调转 | 冲击波跟随 |
|---|---|---|---|---|
| `UpgradeEffectManager.cs` | ✓ | ✓ | ✓ (描述) | ✓ (描述) |
| `PlayerState.cs` | ✓ | ✓ | | |
| `UpgradeDefinition.cs` | | ✓ | | ✓ (新配置结构) |
| `EnemyProjectile.cs` | | ✓ (source参数) | | |
| `EnemyManager.cs` | | ✓ (source传递) | | |
| `Enemy.cs` | | ✓ (source传递) | | |
| `QTEController.cs` | | ✓ (null source) | | |
| `AttackSystem.cs` | | | ✓ (管线改造) | ✓ (管线改造) |
| `ColumnManager.cs` | | | ✓ (反转) | |
| `PassiveTriggerModule.cs` | | | ✓ (标志) | |
| `TimedPassiveModule.cs` | | | | ✓ (队列) |

新增文件: 0
新增 prefab / 特效: 冲击波可能复用现有 AttackWave prefab

---

## 五、待确认

- [x] 冲击波使用哪个 prefab？→ 复用 Sweep 的 attackWavePrefab
- [x] 冲击波的 baseDamage → 独立配置固定值（ChargeShockwaveLevelConfig.baseDamage）
- [ ] 顺序调转是仅 Sweep 还是 Pierce 也触发？（当前设计仅 Sweep）
- [ ] 蓄力减伤的蓄力标志：是否需要 `pressDuration` 渐变减伤，还是二值（蓄力中/不在蓄力）？
- [x] 反伤盾 icon 待配
