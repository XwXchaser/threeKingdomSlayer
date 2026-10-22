# 卷轴场景归档（Step 1）

2026-10-17 由 `plan/scroll-on-demand-loading-plan.md` 的 Step 1 执行：把 6 个已废弃的美术组从
`Assets/Experiments/CurvedScroll/YJunctionSample.unity` 中搬出为 prefab，**未删除任何内容**。

## 为什么搬出

这些组的子物体全部是 `activeSelf = false`（对渲染零贡献），但仍随场景反序列化，占用加载时间与常驻内存。
搬出后场景物体数 1483 → 856，renderer 1360 → 741，`YScrollSample.scenery` 790 → 665（同步剔除 125 项）。

## 组清单

| 组 | 物体 | renderer | 文件 | 归档时 localPosition |
|---|---|---|---|---|
| `Approach - Valley Road` | 483 | 482 | 1192 KB | (0.00, 0.00, 0.00) |
| `Junction Rock Barrier and Roadside Density` | 33 | 32 | 80 KB | (0.00, 0.00, 107.00) |
| `Left branch landmarks` | 31 | 30 | 74 KB | (0.00, 0.00, 107.00) |
| `Right branch landmarks` | 31 | 30 | 74 KB | (0.00, 0.00, 107.00) |
| `Left - Valley Encounter` | 28 | 26 | 65 KB | (0.00, 0.00, 107.00) |
| `Right - Camp Encounter` | 21 | 19 | 48 KB | (0.00, 0.00, 107.00) |

## 如何复原

1. 打开 `Assets/Experiments/CurvedScroll/YJunctionSample.unity`。
2. 把对应 prefab 拖到根节点 `Y Junction - select for preview` 下。
3. 用本目录 `archive-inventory.csv` 的 `localPosition` 列核对根 transform（多数为 `0 0 0`，4 个分支组为 `0 0 107`）。
4. 若该组原本参与 `YScrollSample.scenery`（左/右地标、坑谷、营垒、岩石屏障共 125 个 renderer），
   需要把它们重新加回该数组，否则着色器的曲率投影 bounds 行为与原先不同。

`archive-inventory.csv` 为逐物体清单（组名、prefab、父节点、根 localPosition、物体名、世界坐标、activeSelf），
可用于核对"场景有没有少东西"。
