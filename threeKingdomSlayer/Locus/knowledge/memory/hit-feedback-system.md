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
updatedAt: 1785833345626
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
- 当前卡肉时长已增强为 Light 45ms / Standard 90ms / Heavy 140ms；Standard 命中运行时约 90ms 后恢复。用于让单目标基础命中更接近 Slash 多目标连续命中的体感。

## 后续注意

- 当前 Hit Stop 仅冻结受击敌人 Animator，攻击波、DOTween 视觉和其他战斗对象仍按既有时间规则运行；后续若需要更强的全局卡肉，应另行设计战斗时间控制器。
- 命中强度默认主要由调用方显式标注；新增伤害入口必须选择合适的 `HitFeedbackSource` / `HitFeedbackStrength`，不可只靠伤害数值猜测表现等级。
<!-- locus:body:end -->
