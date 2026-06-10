---
id: kd_6bd23eb4-c8b2-454e-a1dd-a6e8409ac519
type: design
path: boss-overhaul.md
title: boss-overhaul
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1781067207076
updatedAt: 1781106265395
---

# boss-overhaul

## Summary
BOSS机制统一重构：打断规则与普通敌人对齐、通用描边Shader、锁血转阶段、多阶段攻击配置。Phase 5 完成Poise规则重构。

## Content
# BOSS 机制统一重构设计文档

## 实施进度

- [x] **Phase 0**: 描边 Shader + 材质 + QTE蓝色描边 — 完成
- [x] **Phase 1**: 打断统一 + SuperArmor — 完成
- [x] **Phase 2**: 锁血 + 转阶段 — 完成
- [x] **Phase 3**: 多阶段配置 BossPhaseData SO — 完成
- [x] **Phase 4**: BOSS Idle调度 + QTE由Enemy驱动 + interruptibleOnStun — 完成
- [x] **Phase 5**: Poise伤害规则重构 + 弹刀移除 + Launch零Poise + QTE专用Poise — 完成
- [ ] **Phase 6**: Parry窗口收窄（待设计）
- [ ] **Phase 7**: 各阶段独立QTE数据（架构已支持，待配置）

---

## 核心架构变更

### BOSS Idle 调度系统

BOSS使用 `Idle → [Attack/CAttack/QTE] → Idle` 循环：
1. 进入 Idle → 设置 `actionCooldownTimer`（从BossPhaseData.actionInterval [min,max] 随机取值）
2. 倒计时结束 → `SelectBossAction()` 加权随机选择下一个行动
3. 行动完成 → 回到 Idle，重新开始冷却

**所有回到 Idle 的路径**：攻击动画OnComplete、CancelAttack()、Stun恢复、Launch落地、QTE退出、转阶段完成、Boss首次进入战斗

### QTE 由 Enemy 驱动

QTEController 状态机：`Idle → PerformingQTEAttack → QTEJudging → QTECompleted → Idle`
- Enemy.SelectBossAction() 选中QTE时调用 TriggerQTEAttack()
- QTE完成后使用 postQTECooldown 作为冷却

### Poise 伤害规则（Phase 5）

**TakePoiseDamage**（仅 Parry 调用，仅 Attacking 态生效）：
- `state != Attacking` → 拒绝
- `state == Stunned` → 拒绝
- Poise→0 → Stun

**TakeQTEPoiseDamage**（仅 QTEController 调用，仅 QTEAttacking 态生效）：
- `state != QTEAttacking` → 拒绝
- `interruptibleOnStun=true`: Poise→0 → Stun → QTEController 中止 QTE
- `interruptibleOnStun=false`: Poise→0 → 播放受击硬直动画，Poise重置为满，QTE继续

**Launch 永不造成 Poise 伤害**（通用规则）。

### 玩家攻击 × BOSS 状态完整矩阵

| BOSS状态 | 普攻 | Parry | Launch | Poise伤害来源 |
|---------|------|-------|--------|-------------|
| Idle | 伤害 | 伤害 | 伤害 | 无 |
| Attacking+普通窗口 | 伤害+打断 | 伤害+打断+Poise | 伤害+打断 | Parry |
| Attacking+CFrame/SuperArmor | 伤害(不打断) | 伤害+打断+Poise | 伤害+打断 | Parry |
| Attacking+AttackDraw | 伤害(不打断) | 伤害(不打断) | 伤害(不打断) | 无 |
| QTEAttacking | 伤害(不打断) | 伤害(不打断) | 伤害(不打断) | QTE成功 |
| Stunned | 伤害+可击飞 | 伤害 | 伤害+击飞 | 无 |
| Launched | 伤害×1.5+延长 | 伤害×1.5 | 伤害×1.5 | 无 |

### 已移除的机制
- **弹刀(Clank)**：从未设计，已删除 PlayClankEffect 及相关逻辑
- **CheckParryStunThresholds**：旧的血量百分比触发stun规则，已删除

---

## BossPhaseData 字段

```csharp
phaseIndex, phaseName, triggerHealthPercent
attackSequence, qteData
normalAttackWeight, cAttackWeight, qteWeight
actionInterval (Vector2 [min,max])
postQTECooldown
isSuperArmor
stab/slash/pierce/sweep/launch/poise multipliers
transitionTriggerName, transitionDuration
```

## 描边系统

- 红色 = C技（isCFrame=true 且 state=Attacking）
- 橙色 = 全程霸体（isSuperArmor=true）
- 蓝色 = QTE攻击中（state=QTEAttacking）
- 死亡或转阶段中不显示描边

全局默认 + 每个Enemy可覆盖（颜色/宽度独立调整）。

---

## 待解决问题

### Parry窗口过宽
整个AttackSpawn阶段都在parry可打断窗口，BOSS可能被连续parry锁死。
可能方案：收窄可打断窗口、增加parry抗性、仅C技窗口可parry打断。

### 各阶段独立QTE数据
架构已支持（BossPhaseData.qteData），待为各阶段创建独立QTE配置资产。
