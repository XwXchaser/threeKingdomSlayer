---
id: kd_28a19b01-e07f-49cd-9619-cd7c81a4635a
type: design
path: visual-yoffset-system.md
title: visual-yoffset-system
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1783355398219
updatedAt: 1783355398221
---

# visual-yoffset-system

## Summary
精灵锚点不统一的视觉补偿方案：通过 Inspector 可配的 Y 偏移字段，让不同尺寸的立于地面角色和特效的脚底对齐，避免每个精灵都去 Sprite Editor 改锚点。

## Content
## 背景

项目中所有精灵锚点默认在中心 `(0.5, 0.5)`。不同尺寸的角色（32×32 vs 87×110）在同一 Y=0 平面上时，脚底位置不一致。

Sprite Editor 批量改锚点到底部是最彻底的方案，但会导致所有引用精灵的 GameObject 视觉偏移，需要大面积排查补偿逻辑。当前选择方案 B：不改锚点，在需要脚底对齐的组件上加 Y 偏移字段。

## 实现

三个组件各加了一个 `[SerializeField]` Y 偏移字段：

### Enemy.cs — `visualYOffset`
- 位置：`[Header("视觉偏移")]` 下，紧接 `moveSpeed`
- 应用：`UpdateWorldPosition()` 中 `transform.localPosition = new Vector3(xPos, bounceYOffset + visualYOffset, zPos)`
- 默认值：0
- 配置：在每个 Enemy Prefab 的 Inspector 上配

### CycloneEffect.cs — `yOffset`
- 位置：`[Header("视觉偏移")]` 下，紧接 `fadeOutDuration`
- 应用：`Setup()` 中 `pos.y = yOffset`（替代原来的 `pos.y = 0f`）
- 默认值：0
- 配置：在 `CycloneEffect.prefab` 的 Inspector 上配

### SpikeTrapController.cs — `yOffset`
- 位置：`[Header("位置与缩放")]` 下，与 `zOffset` 并列
- 应用：`GetLocalPosition()` 中 `y = yOffset`（替代原来的 `0f`）
- 默认值：0
- 配置：在 Battle.scene 的 `Manager` GameObject 上配

## 偏移值计算公式

```
visualYOffset = spriteHeight(px) / PPU / 2
```

例：32×32 精灵 @ PPU 16 → 32/16/2 = 1.0

## 适用场景

- 立于地面的角色精灵（Enemy、Player 等）
- 地面特效（地刺、旋风等）
- 不需要改锚点的其他地面物件

## 相关文件

- `Assets/Scripts/Enemy/Enemy.cs` — UpdateWorldPosition
- `Assets/Scripts/Effect/CycloneEffect.cs` — Setup
- `Assets/Scripts/Core/SpikeTrapController.cs` — GetLocalPosition
