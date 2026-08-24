---
id: kd_28a19b01-e07f-49cd-9619-cd7c81a4635a
injectMode: inherit
summary: 精灵锚点不统一的视觉补偿方案：通过 Inspector 可配的 Y 偏移字段，让不同尺寸的立于地面角色和特效的脚底对齐，避免每个精灵都去 Sprite Editor 改锚点。
aiEditMode: inherit
---

## 实现

三个组件各加了一个 `[SerializeField]` Y 偏移字段：

### Enemy.cs — `visualYOffset`
- 位置：`[Header("视觉偏移")]` 下，紧接 `moveSpeed`
- 应用：`UpdateWorldPosition()` 中 `transform.localPosition = new Vector3(xPos, bounceYOffset + visualYOffset, zPos)`
- 默认值：0
- 配置：在每个 Enemy Prefab 的 Inspector 上配
- 已于 2026-07-08 在提交 `4220292` 的整批回退中被意外删除；2026-07-09 已仅恢复此独立功能，未带回 PushWave 等其它被回退逻辑。

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

## 当前 Enemy 配置

- `Enemy_104.prefab`: `visualYOffset = 0.34`
- `Enemy_1`, `Enemy_101`, `Enemy_102`, `Enemy_103`, `Enemy_105`: `visualYOffset = 0`
