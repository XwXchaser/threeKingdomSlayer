---
id: kd_6bd23eb4-c8b2-454e-a1dd-a6e8409ac519
injectMode: inherit
summary: BOSS机制统一重构：打断规则与普通敌人对齐、通用描边Shader、锁血转阶段、多阶段攻击配置。Phase 5 完成Poise规则重构。
aiMaintained: inherit
---

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

## BOSS 动画资源部署

### Idle 动画
- `Boss_104_Idle.anim`：双帧循环 `BOSS_idle1.png` → `BOSS_idle2.png`（0.6s/帧，1.2s 总长）
- 原始 `BOSS_idle.png` 不再使用

### HitFlash 动画
- `Boss_104_HitFlash.anim`：4帧序列 `BOSS_hitted1-4.png`（0.1s/帧，0.4s 总长，不循环）
- `HitFlashRoutine` 等待时间同步调整为 0.4s

### SuperArmor 受击反馈规则
SuperArmor（阶段级霸体）受击时：
- **阻断**受击动画（`HitFlashRoutine` 加 `!isSuperArmor` 守卫）—— 霸体不应呈现痛苦摇摆
- **保留**白闪反馈（瞬时 + 持续）—— 击中确认，玩家需要
- **减弱**抖动（`DOPunchScale` 从 0.2/0.15s/8 降为 0.1/0.1s/5）—— 不破坏霸体印象但保留打击感

### Boss Stun 动画资源部署
- `Boss_104.controller` 已使用 `BOSS_weak_*` 资源新增四段状态：`StunStart`（weak_start1-3，0.3s）→ `StunLoop`（weak_loop1-3，循环）→ `StunHit`（weak_hitted1-3，0.3s）→ `StunLoop`；`StunEnd`（weak_end1-3，0.3s）由代码在眩晕计时结束时直接播放。
- `Enemy` 中 Boss 进入 Stun 播放 Start；眩晕中的常规 `TakeDamage`（非 DOT、非击飞/死亡/QTE/霸体路径）播放 StunHit；正常结束先完整播放 End，再恢复 Idle 调度。
- Start、Loop、Hit、End 均可由击飞打断。击飞中仍计眩晕：若落地时仍有剩余则直接恢复 Loop；若已结束或 End 被打断则落地播放完整 End，随后才恢复 Idle。转阶段锁血无敌与 Stun 独立；若 Stun 中触发转阶段，先播放 End，再播放 Phase2/Phase3 转场。

### CancelAttack Animator 复位
`CancelAttack()` 中新增 `_animator?.Play("Idle", 0, 0f)`。此前 Animator 回 Idle 依赖 HitFlash 链路的副作用（Hit 触发→HitFlash 状态→自动回 Idle），`!isSuperArmor` 守卫暴露了该隐式依赖。现改为显式复位，与 `PlayAttackAnimationTween` OnComplete 的 `Play("Idle")` 对称。

---

## 待解决问题

### Parry窗口过宽
整个AttackSpawn阶段都在parry可打断窗口，BOSS可能被连续parry锁死。
可能方案：收窄可打断窗口、增加parry抗性、仅C技窗口可parry打断。

### 各阶段独立QTE数据
架构已支持（BossPhaseData.qteData），待为各阶段创建独立QTE配置资产。

---

## 近期变更

| 日期 | 内容 |
|------|------|
| 2025-07 | Phase 5: Poise伤害规则重构 + 弹刀移除 |
| 2025-07 | QTE攻击期间BOSS不再无敌，可正常造成HP伤害 |
| 2025-07 | QTE提前输入不再判定失败，未命中穿透为普通攻击 |
| 2025-07 | 攻击动作未结束时禁止QTE交互（IsActionPlaying守卫） |
| 2025-07 | BOSS免疫位移规则强化：PushWave跳过BOSS时不触发PostDisplacementFillUp |
| 2025-07 | Column.CompactByClearRows增加QTEAttacking状态守卫，防止压缩重置状态 |
| 2025-07 | 击飞落地恢复Stun时保留_appliedStunDuration，修复poise进度条跳变 |
