---
id: kd_e977e93b-23a8-4e08-bcc1-886cebd05c6a
type: design
path: boss-mechanics.md
title: boss-mechanics
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1779004290012
updatedAt: 1786456224378
---

# boss-mechanics

## Summary
BOSS 机制玩法设计文档 — 涵盖 BOSS 身份标识、架势/眩晕、招架、阶段推进、攻击模式、必杀技交互、列阵规则、UI 血条、QTE攻击系统、配置参数等完整系统

## Content
# BOSS 机制玩法设计文档

## 概述

BOSS 是普通敌人的变体，与普通敌人共享同一套 `Enemy` MonoBehaviour 框架（`Assets/Scripts/Enemy/Enemy.cs`，约 2800+ 行），通过 `isBoss = true` 标志启用额外行为。核心差异化体现在：架势→眩晕循环、招架血量阈值触发、分阶段推进、跨列清空规则、专属血条 UI 五个方面。

---

## 1. BOSS 身份标识

| 属性 | 说明 |
|------|------|
| `Enemy.isBoss` | `true` 时启用 BOSS 全部特殊逻辑 |
| `Enemy.bossState` | 运行时状态：`None`（未推进）→ `Approaching`（第3排待机）→ `InCombat`（进入战斗） |
| `Enemy.bossHealthBarPrefab` | 可选，BOSS 专属血条 Prefab；为 null 则用 `BattleHUD` 默认模板 |
| `WaveConfig.isBossWave` | 关卡配置中的 BOSS 波次标记（字段已定义，运行时尚未使用） |

**设计要点**：BOSS 不使用独立的类或 ScriptableObject，所有属性（血量、架势值、弱点倍率、攻击速度、眩晕时长、击飞参数等）直接序列化在 Enemy 预制体组件上。打开 `Assets/Resources/EnemyPrefabs/` 下的 BOSS 预制体即可在 Inspector 中修改。

---

## 2. 架势与眩晕

### 2.1 架势条（Poise）

- BOSS 拥有 `maxPoise`（最大架势值，如 50），初始满值
- 玩家通过 **挑飞（Launch）** 和 **招架（Parry）** 攻击造成架势伤害（`poiseDamage`）
- 架势值不会自动恢复；**眩晕结束后**一次性重置为满值
- 架势条实时反映在 `BossHealthUI` 的 `poiseFill` Image 上

### 2.2 眩晕触发

BOSS 进入眩晕有**两条路径**：

| 路径 | 触发条件 | 说明 |
|------|---------|------|
| **架势破碎** | `currentPoise ≤ 0` | 直接调用 `Stun(stunDuration)`，眩晕 `stunDuration` 秒 |
| **招架血量阈值** | 招架命中 + 当前血量百分比低于阈值 | 遍历 `parryStunThresholds[]`，取最严格匹配的阈值，眩晕对应时长 |

### 2.3 眩晕期间规则

- 免疫额外架势伤害（`TakePoiseDamage` 直接返回 false）
- BOSS 进入战斗后（`InCombat`），眩晕恢复后**不参与列内补齐**，原地恢复攻击
- 架势恢复进度通过 `stunRecoveryProgress`（0→1 时间比例）驱动 UI 动画，不受状态切换影响

### 2.4 架势机制边界

- 架势与眩晕是 Boss 专属机制；普通敌人不累计 Parry 架势，不因 Parry 归零进入 `Stunned`。
- Boss 架势伤害只在实际攻击动画前摇阶段生效：`BossState.InCombat`、`state == Attacking`、`isAttackAnimating == true` 且 `isAttackDrawPhase == false`。
- 不能仅使用 `state == Attacking` 作为 Parry 架势窗口，因为该状态也覆盖攻击冷却阶段。



---

## 3. 招架系统（Parry）

### 3.1 对 BOSS 的招架规则

Boss 攻击打断规则遵循 `design/attack-interrupt-system.md` 第3.2节：

| Boss窗口 | 打断条件 |
|-----------|---------|
| 普通窗口（非CFrame非SuperArmor）| 所有攻击可打断 |
| CFrame（C技霸体）| **仅 Parry** 可打断 + 削Poise |
| SuperArmor（Phase霸体）| **仅 Parry** 可打断 + 削Poise |
| AttackDraw（收招）| 不可打断 |

**核心原则**：不存在玩家用 Parry 无法打断的 Boss 攻击。Launch（挑飞）仅在普通窗口可打断Boss攻击，CFrame/SuperArmor窗口下无效。

**与普通敌人的核心区别**：

| 场景 | 普通敌人 | BOSS |
|------|---------|------|
| 普通窗口 AttackSpawn | 任何攻击打断 | 任何攻击打断 |
| CFrame/SuperArmor AttackSpawn | Parry/Launch 打断 | **仅 Parry** 打断 + 削Poise |
| AttackDraw（收招） | 不可打断 | 不可打断 |
| 不在攻击动画中 | 仅造成伤害 | 仅造成伤害 + 检查血量阈值眩晕 |

### 3.2 招架血量阈值（`parryStunThresholds`）

```csharp
[System.Serializable]
public struct ParryStunThreshold
{
    [Range(0f, 1f)] public float healthPercent;  // 血量百分比阈值
    public float stunDuration;                     // 达到阈值时眩晕秒数
}
```

- 配置在 Enemy 预制体上，可设多个阈值（如 75%=0.5s, 50%=1s, 25%=2s）
- 招架命中后自动检查，取 **healthPercent 最小**的匹配项（即最严格的阈值）
- 典型的 "BOSS 残血时招架更有效" 设计

---

## 4. BOSS 阶段推进

### 4.1 推进流程

```
波次生成 → BOSS 出现在后方排（rowIndex ≥ 3）
         → 参与正常补齐链（逐个向前移动）
         → 到达 rowIndex = 2（第3排）：BossPause()
              ├─ 停止移动，等待前两排（row 0, 1）跨所有列清空
              └─ 前两排全清 → BossResume()
         → 移动到 rowIndex = 1（第2排）：启动 1 秒无敌缓冲
         → 缓冲结束：BossState.InCombat，开始攻击
```

### 4.2 跨列清空规则

Boss 补齐时检查的是**整排**（所有 5 列）而非仅本列：`IsRowClearForBoss(row)` 遍历全部列，只要有一列在该排存在存活敌人，BOSS 就不前进。这确保 BOSS 的出场是"前排全灭"后的演出时刻。

### 4.3 列阵中的特殊处理

- BOSS **完全不参与列补齐链**：`Column.RemoveEnemy()`、`Column.CompactColumn()`、`Column.CompactByClearRows()` 和 `Column.TriggerFillForward()` 均对 `isBoss` 敌人跳过 `pendingRushMove` 标记，BOSS 移动完全由自身状态机（`BossPause`/`BossResume`/`TryStartRushMove`）控制
- **初始 wave spawn 的 Boss 独立补齐**：`TriggerFillForward` 在列链启动后，通过 `StartFillForwardDelay(0.5f)` 触发 Boss 独立补齐，`targetRow=0` 确保持续前移直到攻击范围。`TryStartRushMove` 在 `IsRowClearForBoss` 失败时同时设置 `rushMoveDelayTimer=0.3f` 作为定时器兜底，防止 rush 补齐链中 `OnColumnsModified` 不触发导致的死锁
- **row≤1 守卫**（已实现）：`TryStartRushMove` 在 `EnemyState.Idle` 下检查：若 `isBoss && rowIndex ≤ 1`，直接跳过补齐前移（`pendingRushMove = false; return false`），确保 Boss 不会从 row=1 冲到 row=0
- BOSS 进入 `InCombat` 或缓冲中后，眩晕恢复后直接 `SetBossActionCooldown()`，不处理 `pendingRushMove`
- BOSS 缓冲期间（`_bossEngageTimer > 0`）无敌，免疫伤害

---

## 5. 攻击模式

### 5.1 三段攻击循环

BOSS 与普通敌人共用同一攻击系统：

```
冷却阶段（attackTimer 倒计时，40% 攻击间隔）
  └→ AttackSpawn 前摇（DOTween：向前移动 + 镜像翻转，spawnDuration 秒）
       └→ 造成伤害（PerformAttack）→ 进入 AttackDraw
            └→ AttackDraw 收招（DOTween：后退原位 + 翻转回正，drawDuration 秒）
                 └→ 返回冷却阶段
```

### 5.2 可配置攻击参数

| 参数 | 说明 |
|------|------|
| `attackSpeed` | 攻击频率（次/秒），决定总间隔 = 1/attackSpeed |
| `attackDamage` | 每次攻击伤害值 |
| `attackRange` | 攻击距离（排数），rowIndex < attackRange 时进入攻击 |
| `attackSpawnDuration` | 前摇时长（秒）——此阶段可被招架打断 |
| `attackDrawDuration` | 收招时长（秒）——此阶段不可被招架打断 |

### 5.3 攻击与补齐的优先级

- 冷却期间若被标记 `pendingRushMove`：**先补齐再攻击**
- 动画期间：完成当前攻击后检查是否需要补齐

---

## 6. 必杀技交互

### 6.1 狂怒大招（UltimateEffect_Berserk）

当玩家释放狂怒大招时：

- **玩家无敌**：`PlayerState.isInvincible = true`（BOSS 攻击无效）
- **自动 Stab**：按 `berserkStabCooldown` 间隔（如 0.5s）对所有列轮转执行戳击
- **伤害计算**：`UltimateSkillConfig.damage × (1 + 武将加成) × berserkDamageMultiplier`
- **禁止技能输入**：`InputManager.skillInputEnabled = false`（玩家无法手动出招）
- **血条变色**：英雄血量条变为橙色作为视觉反馈
- **持续时长**：`berserkDuration` 秒（如 5s）

### 6.2 大招充能

- 能量通过命中敌人积攒（每个技能有 `ultimateEnergyGain`）
- BOSS 波通常血量高，是充能的关键阶段

---

## 7. UI 系统

### 7.1 BossHealthUI

文件：`Assets/Scripts/UI/BossHealthUI.cs`

- 由 `BattleHUD.OnBossEngaged` 事件触发动态实例化
- **每帧轮询** `Enemy.currentHealth` / `Enemy.currentPoise`（而非事件驱动，避免时序问题）
- 显示元素：BOSS 名称（`bossNameText`）、血量条（`healthFill`）、架势条（`poiseFill`）
- 架势条使用 `stunRecoveryProgress` 驱动恢复动画
- BOSS 死亡后 0.5 秒淡出并销毁

### 7.2 BattleHUD 管理

- `bossBarsParent`：BOSS 血条的父容器 RectTransform
- `maxBossBars`：同时显示上限（默认 5）
- 去重：已绑定的 BOSS 不会重复创建血条

---

## 8. 弱点与伤害倍率

BOSS 拥有完整的弱点倍率系统，每个伤害类型独立配置：

| 倍率字段 | 对应伤害类型 |
|---------|------------|
| `stabDamageMultiplier` | 戳击（Stab） |
| `slashDamageMultiplier` | 斩击（Slash） |
| `pierceDamageMultiplier` | 穿刺（Pierce） |
| `sweepDamageMultiplier` | 横扫（Sweep） |
| `launchDamageMultiplier` | 挑飞（Launch） |
| `poiseDamageMultiplier` | 架势伤害（Poise） |

外加：击飞中受伤倍率 `launchedDamageTakenMultiplier`（默认 1.5x）。

---

## 9. 击飞系统

BOSS 共享完整击飞系统：

- **击飞参数**：`launchDuration`、`launchGravity`、`launchYHeightMin/Max`（随机高度）
- **反弹速度**：`launchReboundVelocity`（空中受击时叠加）
- **延长浮空**：`launchedHitExtendDuration`（每命中延长）
- **着陆后**：BOSS 进入战斗后锁定位置恢复攻击；未进入战斗则参与补齐链

---

## 10. 视觉规则

### 10.1 敌人透明度（`GetAlphaForRow`）

`Enemy.GetAlphaForRow()` 控制各排敌人的透明度，实现远近层次感。两组规则叠加：

**基础规则**（所有敌人）：
| 排位 | alpha |
|------|-------|
| row 0 | 1.00 |
| row 1 | 0.75 |
| row 2 | 0.60 |
| row ≥ 3 | 0.45 |

**攻击范围覆盖**（已实现）：处于 attackRange 内的敌人强制 alpha=1.0，提高辨识度。逻辑：`int atkRange = (int)Mathf.Max(1, attackRange); if (row < atkRange) return 1f;`

### 10.2 闪白/弹刀反馈

见 `design/attack-interrupt-system.md` 第4节。

---

## 11. BOSS 死亡

- 死亡动效：弹起 + 随机旋转 + 重力掉落（协程）
- 击飞中死亡：从当前空中位置直接旋转坠落（更大角度）
- 死亡后回收至对象池（`EnemyPool.ReturnEnemy`）
- 铜钱奖励：`coinReward` 字段（通常比普通敌人高）

---

## 12. 可配置参数汇总

### Enemy 组件（直接序列化在预制体上）

```
基础：enemyName, enemyId
战斗：maxHealth, attackSpeed, attackDamage, attackRange, attackSpawnDuration, attackDrawDuration, moveSpeed
架势：maxPoise, stunDuration
击飞：launchDuration, launchGravity, launchReboundVelocity, launchYHeightMin/Max, launchedDamageTakenMultiplier, launchedHitExtendDuration
BOSS：isBoss, bossHealthBarPrefab
弱点：stab/slash/pierce/sweep/launch/poiseDamageMultiplier
奖励：coinReward
招架阈值：parryStunThresholds[]
```

### StageConfig / WaveConfig

```
WaveConfig.isBossWave  — BOSS 波次标记（已定义，运行时逻辑待实现）
StageConfig.rushMoveDelay — 补齐移动间隔
```

### UltimateSkillConfig

```
cooldown, energyCost, damage, berserkDuration, berserkStabCooldown, berserkDamageMultiplier
```

---

## 13. 架构关系图

```
StageConfig (ScriptableObject)
  └→ WaveConfig[].rows[].enemyIds[]
       └→ WaveSpawner 实例化 Enemy Prefab
            └→ Enemy.isBoss = true 时触发 BOSS 逻辑

Enemy (MonoBehaviour, ~2800行)
  ├─ BossState: None → Approaching → InCombat
  ├─ Column / ColumnManager (列阵规则，Approaching 时跳过)
  ├─ BattleHUD.OnBossEngaged → BossHealthUI 实例化
  └─ AttackSystem.ExecuteParry() (招架分支)

UltimateSystem
  └→ UltimateEffect_Berserk.Execute()
       └→ 无敌 + 自动 Stab + 血条变色
```

---

## 14. 已有扩展点与后续方向

### 已预留但未启用的机制

- `WaveConfig.isBossWave`：关卡数据中已标记 BOSS 波，但 `WaveSpawner` 尚未使用此字段做差异化生成逻辑
- `bossHealthBarPrefab`：每个 BOSS 可挂载独立血条 Prefab，目前多数 BOSS 使用 `BattleHUD` 默认模板

### 可能的玩法延展方向

1. **多阶段 BOSS**：利用 `BossState` 扩展新阶段（如 Phase2 切换攻击模式、改变弱点倍率）
2. **BOSS 怒气/狂暴**：血量低时自动触发类似 `Berserk` 的效果（提升攻击速度、伤害）
3. **部位破坏**：BOSS 占多槽位（`occupySlots > 1`），不同槽位对应不同弱点
4. **BOSS 召唤小兵**：BossPause 期间定时刷出护卫敌人
5. **QTE 处决**：眩晕期间触发特殊攻击
6. **环境交互**：BOSS 出场时触发场景变化（摄像机动画、粒子特效）
7. **BOSS 专属攻击技能**：扩展 `Enemy.UpdateAttack()` 支持多种攻击模式切换
8. **架势系统深化**：不同 BOSS 有不同的架势恢复规则（如受特定攻击才减架势）

---

## 更新记录

| 日期 | 内容 |
|------|------|
| 2025-01 | 初始版本 |
| 2025-07 | 补充 row≤1 守卫规则（4.3节）+ 透明度/攻击范围覆盖规则（10节） |
| 2025-12 | 修复 Boss 初始 wave spawn 不补齐死锁：TriggerFillForward 添加 Boss 守卫 + timer 兜底 |
