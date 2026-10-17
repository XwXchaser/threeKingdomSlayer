---
id: kd_617bcb3b-887b-40fe-8f46-5e1ebada9852
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 敌人脚底对齐（Y 轴）体系

## 根因

所有敌人精灵都是 **Tight 打包、pivot 在画布中心（0.5, 0.5）**，但画布尺寸不同：
普通敌人 `rect` = 512×512，109/1011/1021 = 1108×1108。

因此**脚底到根节点原点的距离因敌人而异**，而根节点 Y 一律为 0：

| 敌人 | 剪影脚底 Y（未补正） |
|---|---|
| Enemy_1021 | -1.798 |
| Enemy_101 | -1.811 |
| Enemy_1011 | -1.939 |
| Enemy_1 | -2.013 |
| Enemy_103 | -2.014 |
| Enemy_102 | -2.038 |
| Enemy_105 / 106 | -2.063 |
| **Enemy_109** | **-2.594** |
| Enemy_104 / 107 / 108 | -2.835 |

对齐前只有 101 和 1021 恰好在 -1.8 附近，其余全部偏低。**109 低 0.79 世界单位**
（前排 z=0 折合约 34 屏幕像素，后排 z=-10 更大），这就是"高大单位陷到地里"的实测根因。

## 旧公式为什么错

`Locus/knowledge/design/visual-yoffset-system.md` 原公式 `精灵高度(px)/PPU/2` 用的是
`sprite.rect` 尺寸，即**画布半高**，而非脚底到 pivot 的距离：

| Prefab | 旧公式 | 实际需要 | 误差 |
|---|---|---|---|
| Enemy_1011 | 1.801 | 0.139 | 约 13 倍 |
| Enemy_104 | 2.857 | 1.035 | 约 2.8 倍 |
| Enemy_109 | 2.386 | 0.794 | 约 3 倍 |

只有 1011 这种剪影几乎撑满画布的精灵才接近成立。Enemy_109 手填的
`EnemyHealthBar.yOffset: 35.28` 也是同一公式的产物（1108/16/2 + 0.3）。

## 现行基准与公式

地面基准线 **groundY = -1.8**（与 `BattleYRouteHost.enemyFootGroundY` 一致）。
Prefab 根节点自身 scaleY 下，Prefab 阶段节点 Y = 0：

```
visualYOffset = groundY - silhouetteMinLocalY * rootScaleY
```

- `silhouetteMinLocalY` = `Sprite.vertices`（Tight 剪影网格）的最低 Y，不含透明边
- **不能除以 rootScaleY**：`localPosition` 位于父节点空间，不被自身 `localScale` 缩放

## 已落盘的 offset（基准 -1.8）

| Prefab | visualYOffset |
|---|---|
| Enemy_101 | 0.0113 |
| Enemy_1021 | -0.0024 |
| Enemy_1011 | 0.1389 |
| Enemy_1 | 0.2125 |
| Enemy_103 | 0.2138 |
| Enemy_102 | 0.2375 |
| Enemy_105 / 106_Fixed | 0.2625 |
| Enemy_109 | 0.7938 |
| Enemy_104 / 107 / 108 | 1.035 |

Enemy_109 是 **Prefab Variant**（源 `Enemy_101.prefab`），该字段以 override 形式存储。
Enemy_109 的骑乘动画帧剪影一致（`MountedIdle1` -2.595、`Windup2` -2.595、
`TurnToIdle21` -2.600），静态单值对这些帧成立。

## 三个组件的关系

1. **`Enemy.visualYOffset`**（序列化）：美术基准，Inspector 可手调。
2. **`Enemy.runtimeVisualYOffset`**（`[NonSerialized]`）：`EnemyGroundAligner` 写入。
   两者相加 = `Enemy.EffectiveVisualYOffset`，互不覆盖。
3. **`EnemyGroundAligner`**：按 `Sprite → minLocalY` 静态字典缓存，**只在首次拿到有效
   精灵时对齐一次**。刻意不逐帧跟随——出招/死亡/QTE 帧剪影会伸到地面线以下
   （104 的 `BOSS_QTE_sweep_hit1` 比 Idle 低约 1.9），逐帧对齐会让单位上下浮动。
   多形态单位（109 落马换 controller）需调用 `Recapture()`。

`BattleYRouteHost.applyEnemyVisualOffset` 是同一目的的第二条通道（当前关闭），
**不要与 EnemyGroundAligner 同时开启**，且它累加 `visualYOffset` 会覆盖美术基准。

## 已知未收口项

- **1021 的本地偏移**：`Enemy_1021.prefab` 的根节点 localScale 与 1011 的 0.0832
  不同（0.0902），实测剪影脚底偏高约 0.25，是本地缩放差异导致，尚未处理。
- **109 落马**：落马后换成 `RiderAnchor` + `Enemy_1011.controller`，剪影基准不同
  （-1.939 系），需要接 `Recapture()` 与专用偏移。
- **104/107/108 暂未验证视觉**：offset 从 0.36 提到 1.035，改动量最大。

## 相关文件

- `Assets/Scripts/Enemy/EnemyGroundAligner.cs` — 运行时自动对齐
- `Assets/Scripts/Editor/EnemyGroundAlignTool.cs` — 菜单 `Tools/敌人/脚底对齐 (Ground Align)`
- `Assets/Scripts/Enemy/Enemy.cs` — `EffectiveVisualYOffset` / `ApplyVisualOffsetNow`
- `Assets/Scripts/UI/EnemyHealthBar.cs` — 头顶高度改用剪影最高点

## 验证方式

Play Mode 下读每个敌人的 `Enemy.EffectiveVisualYOffset`，与
`transform.position.y + sprite.vertices 最低点 * lossyScale` 之和对比，应等于 -1.8。
2026-10-12 实测：109 = -1.800（误差 0），跨 180 帧稳定。
