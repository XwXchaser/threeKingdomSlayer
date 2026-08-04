---
id: kd_27ea3f61-811b-4edc-8db6-9e312373fa37
type: memory
path: stab-programmatic-animation.md
title: stab-programmatic-animation
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1785835591484
updatedAt: 1785835591484
---

# stab-programmatic-animation

## Summary
普通 Stab 已加入程序化蓄势、加速刺入、轻微穿入、缩放形变和回收动画，不复用 Slash 的弯曲帧素材。

<!-- locus:body:start -->
## 普通 Stab 程序动画增强

- 普通直刺使用 `Assets/Prefabs/Stab.prefab` 的静态 `Assets/Sprites/zhangfei/stab.png`；`stab_rotate1/2` 属于 Slash/Sweep 横扫弯曲形变，不用于普通 Stab。
- `StabSweepEffect` 仅增强运行时 `StabVisual`，不改共享 Prefab 和命中判定。
- 当前视觉节奏：12% 蓄势后撤、32% 加速刺入、8% 目标后轻微穿入、48% 回收。
- 蓄势/刺入/命中使用不同局部 X/Y 缩放，模拟武器压缩、拉伸和命中姿态；刺入改用 `Ease.InCubic`，回收使用 `Ease.OutCubic`。
- 首次命中仍按现有流程触发伤害、卡肉、能量、疾病附着和击退；命中暂停期间攻击序列保持暂停。
- Battle 运行验证确认 StabRay 和 StabVisual 的位置/缩放随阶段变化；未引入新火花素材。

## 已知验收注意

- 运行时调试若发现 `Time.timeScale=0`，可能是测试过程中暂停面板/其他阻塞系统残留，不代表 Stab 动画本身失效；验证前需恢复正常时间尺度。
- 该版本只改变程序动画，不改变美术资源语义；后续需由用户实机判断后撤、加速、穿入和回收的力度是否合适。
<!-- locus:body:end -->
