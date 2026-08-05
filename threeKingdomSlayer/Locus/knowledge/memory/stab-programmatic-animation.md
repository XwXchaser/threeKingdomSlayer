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
updatedAt: 1785937968903
---

# stab-programmatic-animation

## Summary
普通 Stab 已加入程序化蓄势、加速刺入、轻微穿入、缩放形变和回收动画，不复用 Slash 的弯曲帧素材。

<!-- locus:body:start -->
## 普通 Stab 程序动画增强

- 普通直刺默认使用 `Assets/Prefabs/Stab.prefab` 的 `Assets/Sprites/zhangfei/stab.png`；`stab_rotate1/2` 属于 Slash/Sweep 横扫弯曲形变，不用于普通 Stab。
- 普通 Stab 已接入高速中间帧 `Assets/Sprites/zhangfei/stab_v13.png`，引用保存在 Battle 场景 `AttackSystem._stabSpeedSprite`；用户已验收视觉效果。
- `StabSweepEffect` 仅增强运行时 `StabVisual`，不改共享 Prefab、射线、伤害或命中判定。
- 当前视觉节奏：12% 蓄势后撤、32% 加速刺入、8% 目标后轻微穿入、48% 回收；高速中间帧仅在刺入阶段约 34%～82% 区间显示，随后恢复原始 Stab。
- 换帧实现必须是纯 `SpriteRenderer.sprite` 替换；禁止在运行时为不同 Sprite 额外修改 `Transform.localPosition` 或 `localScale`。素材尺寸、可见宽度和枪尖锚点必须通过导入配置/画布处理预先对齐。
- 早期错误实现按整张 Sprite bounds 计算运行时缩放和局部 Y 位移，因原 Stab 含大量透明边缘，导致 v13 被放大约数倍并出现在画面顶部；删除全部运行时换帧位移/缩放后修复。
- 换帧回调必须插入已经建立的刺出时间线：`windupDuration + thrustDuration * ratio`。如果在追加刺出 Tween 前用 `_sequence.Duration()` 计算，可能把回调错误插入蓄力/起始阶段。
- 蓄势/刺入/命中仍使用 `_deformRoot` 的程序缩放，模拟压缩、拉伸和命中姿态；这与子 Sprite 的纯换帧职责分离。
- 高速帧显示期间将动态模糊降为辅助强度，原始帧恢复后继续使用既有模糊流程。
- 首次命中仍按现有流程触发伤害、卡肉、能量、疾病附着和击退；命中暂停期间攻击序列保持暂停。

## 素材导入规则

- `stab_v13.png` 保留用户处理后的透明 PNG；使用 Point、无 Mipmap、无压缩、RGBA32。
- 新动画帧与原 Sprite 画布尺寸不同是允许的，但必须在导入前通过 PPU、透明留白与 Pivot 使同一视觉锚点一致；运行时不要用 bounds 临时补偿。
- 对已有透明边缘较大的 Sprite，整张 `sprite.bounds.size` 不等于有效武器轮廓尺寸，不能用来推导换帧缩放比例。
- 不应把 v13 强制裁成与原图相同高度；额外长度属于高速延展，只要纯换帧时视觉位置稳定即可。

## 已验收结论

- 起始阶段保持原 `stab.png`；v13 只在刺出中段出现。
- v13 使用纯 Sprite 换帧后，不再出现画面顶部的异常巨大 Stab，用户已验收效果不错。
- 若后续更换或重做 Stab 动画帧，优先在素材侧完成尺寸/Pivot校准，保持运行时代码只负责帧时序。
<!-- locus:body:end -->
