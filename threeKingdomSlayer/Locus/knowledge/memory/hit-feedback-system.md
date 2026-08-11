---
id: kd_cef5972f-5ef5-40cd-8471-38580aa751e0
type: memory
path: hit-feedback-system.md
title: hit-feedback-system
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1785815668360
updatedAt: 1786343471259
---

# hit-feedback-system

## Summary
首批打击反馈已实现：局部 Animator Hit Stop、命中来源/强度上下文、受击缩放与伤害数字分级；DoT 不卡肉。

<!-- locus:body:start -->
## 已实现的首批打击反馈

- 新增 `Assets/Scripts/Core/HitFeedbackManager.cs`，定义命中来源、反馈强度和命中上下文。
- `Enemy.TakeDamage` 支持反馈来源/强度；`SharedHealthGroup` 只触发一次主反馈，同时保留成员闪白/缩放。
- Hit Stop 采用局部表现暂停，不修改全局 `Time.timeScale`：受击敌人 Animator 暂停；Stab/Slash/AttackWave 的本体攻击视觉在首个标准/重命中时同步暂停，避免出现“敌人停了但武器继续走”而无法感知卡肉。对象池回收、死亡和禁用时清理。
- 现有受击缩放按 Light/Standard/Heavy 分级，始终从原始 Scale 重启；伤害数字按强度缩放，Heavy 增加短促放大。
- DoT 显式为 None，不触发卡肉；幻影/被动/道具/箭雨/火焰为轻反馈；Parry、Launch、旋风、终极技能为重反馈；基础攻击首击标准、后续轻反馈。
- `Enemy.TakeDamage` 与 `SharedHealthGroup.TakeDamage` 的敌人受击音效都必须检查 `feedbackSource != HitFeedbackSource.Dot`；DoT 仍可扣血和更新血条，但不播放 `Enemy_Hit`。
- 仅在 `Enemy.TakeDamage` 入口屏蔽是不够的，共享血量路径会直接在 `SharedHealthGroup` 内播放音效；两条路径必须同时处理。

## 已修复：受击后攻击动画丢失

- 根因：敌人在攻击冷却态受击时，HitStop 禁用 Animator，但 `Hit` Trigger 已设置且攻击冷却仍继续；Animator 恢复的同一帧又触发 `Attack`，最终 HitFlash 抢占动画，而 DOTween 独立执行 `PerformAttack`，表现为攻击动画缺失但伤害存在。
- 修复：`Enemy.UpdateAttack` 在 `_hitStopRemaining > 0` 或 `_hitFlashRoutine != null` 时暂停攻击冷却；受击反馈完整结束后才允许启动攻击。
- 保留仅异常时输出的 `[ATTACK_ANIM_DIAG] TriggerNotEntered` 与 `DamageWithoutAttackAnimation` 警告，用于监测复发。
- 用户已复测开局受击场景，暂未再次出现该问题。

## 后续注意

- 当前 Hit Stop 仅冻结受击敌人 Animator，攻击波、DOTween 视觉和其他战斗对象仍按既有时间规则运行；后续若需要更强的全局卡肉，应另行设计战斗时间控制器。
- 命中强度默认主要由调用方显式标注；新增伤害入口必须选择合适的 `HitFeedbackSource` / `HitFeedbackStrength`，不可只靠伤害数值猜测表现等级。
<!-- locus:body:end -->
