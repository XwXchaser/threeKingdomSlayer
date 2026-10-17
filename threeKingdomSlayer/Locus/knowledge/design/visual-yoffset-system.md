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

## 偏移值计算公式（2026-10-12 修正）

**旧公式 `visualYOffset = spriteHeight(px) / PPU / 2` 已废弃。**

精灵是 Tight 打包，`sprite.rect` 是 512 或 1108 的统一画布，含各不相同的透明边距。
半高等于**画布半高**而不是脚底到 pivot 的距离，因此对多数敌人偏大：

| Prefab | 旧公式算出 | 实际需要 | 误差 |
|---|---|---|---|
| Enemy_1011 | 1.801 | 0.139 | 约 13 倍 |
| Enemy_104 | 2.857 | 1.035 | 约 2.8 倍 |
| Enemy_109 | 2.386 | 0.794 | 约 3 倍 |

**现行公式**（在 Prefab 根节点自身 scaleY 下，Prefab 阶段节点 Y = 0）：

```
visualYOffset = groundY - silhouetteMinLocalY * rootScaleY
```

- `silhouetteMinLocalY`：`Sprite.vertices`（剪影网格）的最低 Y，不含透明边
- `groundY`：统一地面基准线世界 Y，当前定为 **-1.8**
- 注意 `localPosition` 不被自身 `localScale` 缩放，所以此处**不能**除以 `rootScaleY`

## 当前 Enemy 配置（2026-10-12，基准 -1.8）

| Prefab | visualYOffset | 对齐前剪影脚底 Y |
|---|---|---|
| Enemy_101 | 0.0113 | -1.811 |
| Enemy_1021 | -0.0024 | -1.798 |
| Enemy_1011 | 0.1389 | -1.939 |
| Enemy_1 | 0.2125 | -2.013 |
| Enemy_103 | 0.2138 | -2.014 |
| Enemy_102 | 0.2375 | -2.038 |
| Enemy_105 / 106_Fixed | 0.2625 | -2.063 |
| Enemy_109 | 0.7938 | -2.594 |
| Enemy_104 / 107 / 108 | 1.035 | -2.835 |

> 对齐前只有 101 和 1021 恰好在 -1.8 附近，其余敌人全部偏低，109 低 0.79 世界单位，
> 这是「高大单位陷到地里」的实测根因。

## 运行时自动对齐（EnemyGroundAligner）

`Assets/Scripts/Enemy/EnemyGroundAligner.cs` 按当前精灵剪影自动算偏移，写入
`Enemy.runtimeVisualYOffset`（不序列化），与上表的 `visualYOffset` 相加：

```
Enemy 世界 Y = bounceYOffset + visualYOffset + runtimeVisualYOffset
```

- 按 `Sprite → minLocalY` 静态字典缓存，逐帧路径上无几何计算
- **只在首次拿到有效精灵时对齐一次**。刻意不逐帧跟随：出招/死亡/QTE 帧的剪影会伸到
  地面线以下（104 的 `BOSS_QTE_sweep_hit1` 低到 -4.70），逐帧对齐会让单位上下浮动
- 多形态单位（109 落马后换成 `Enemy_1011.controller`）需在切换后调用 `Recapture()`
- 与工作区里 `BattleYRouteHost.applyEnemyVisualOffset` 互斥，不要同时开启

## 血条（EnemyHealthBar）

`yOffset <= 0` 时改用 `EnemyGroundAligner.TryGetSilhouetteHeight()` 取剪影高度，
不再用 `sprite.bounds.size.y`（画布半高）。Enemy_109 手填的 `yOffset: 35.28`
正是旧公式产物（1108/16/2 + 0.3），应改为 0 让它自动算。
