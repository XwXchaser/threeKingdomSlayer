---
id: kd_20b4828f-0df9-4419-b64c-c56cd9529e0d
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# J1→EV 按 90 米重新部署

## 距离

当前三段对比：

- N1→E0：90 米
- E0→J1：100 米
- 现有 J1→EV：35 米，过短

改为：

- J1 保持 190 米。
- EV 村口改为 **280 米**。J1→EV 因此是 **90 米**。
- N3 战斗点改为 **325 米**。EV→N3 是 45 米，保留一段从村口走进村内的距离。
- N2 保持 266.116455 米，右路不延长。

旅行配置：

- `Travel_J1_EV.travelDistance`：75 → **90**
- `Travel_EV_N3.travelDistance`：1.116455 → **45**
- 两段 duration 仍为 3 秒。

## 路线长度

弯道在 212.116455 米结束。325 米需要尾段约 112.8835 米。

把 `YScrollSample.ExitTailDistance` 从当前 54 改为 **113**。45° 时 `Length` 约为 325.116455，足以覆盖 N3=325。不要改 `JunctionDistance` 或 `CurveRadius`。

先编译，再移动锚点。

## 锚点

只改两个 `worldDistance`，然后调用 `SyncTransformFromDistance()`：

- EV：当前值 → **280**
- N3：266.116455 → **325**

EV 的村口组是子对象，会自动回到新的村口锚点，不要手改它的内部坐标。

N3 的村庄背景不是 N3 锚点子对象，而是路线根下的 `N3 Village Rescue - Accepted Art and Grey Wall Study @ 266`。战斗点改到 325 后，必须把这个整组平移到新的 N3 锚点位置，保持组内相对布局不变。预期锚点约为 `(-88.0685, 0, 289.7325)`，朝向 -45°。移动后战斗背景与战斗点的相对关系应和原来一致。

## 过渡景物

停用 `EV Approach - Existing Shoulder`，因为它是按错误的 265 米锚点布置的；移除其 Renderer 引用并不留 null。

在路线根下新建 `J1 to EV - Village Approach`，不要挂到 EV 锚点下。左路坐标按路径距离 d 计算：

```text
arcEnd = 212.116455
tail = d - arcEnd
x = -8.247743 - 0.70710678 * tail
z = 209.911814 + 0.70710678 * tail
```

再沿路径左右法线偏移。局部 Y 用 -0.02，Scale 全为正数，材质用 `YSampleScenery_E0Route`。

| 对象 | 复制源 | 距离 | 侧向 | Scale |
|---|---|---:|---|---:|
| Approach_Fence_A | `J1_A04_BrokenLowFence_v1` | 225 | 左 5.2 | 0.42 |
| Approach_Rock_A | `e0_j1_r1_rubble_v1` | 232 | 右 5.4 | 0.24 |
| Approach_Cart | `J1_A05_CompleteWoodenCart_v1` | 240 | 右 6.0 | 0.34 |
| Approach_Fence_B | 破篱 | 248 | 左 5.4 | 0.40 |
| Interior_House_Left_A | `N3_H1_CompactHouse_v1` | 258 | 左 6.0 | 0.78 |
| Interior_House_Right_A | `N3_H2_LongHouse_v1` | 266 | 右 6.2 | 0.74 |
| Interior_House_Left_B | `N3_H3_DamagedHouse_v1` | 274 | 左 6.4 | 0.70 |

225–248 米只出现篱、石和空车，表示还没到村。258 米后才出现房屋，280 米到达现有村口，325 米进入已有村庄内部。所有内缘离开路中心至少 3.6 米。

## 验收

用 Battle Main Camera、路线根临时 Y=-1.8：

- 212 米仍是弯道末端。
- 240 米只有路肩，没有房屋。
- 280 米恢复 EV 村口构图。
- 325 米的 N3 战斗背景与战斗点对齐，不再停留在旧的 266 米位置。
- 右路 266 米的 N2 不变。

只保存 Y 场景、两份 Travel 资产和 `YScrollSample.cs`。不新增逻辑节点，不改 EV 村口和 N3 村庄的内部布局。
