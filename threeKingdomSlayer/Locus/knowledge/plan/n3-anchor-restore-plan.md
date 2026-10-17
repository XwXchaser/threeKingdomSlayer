---
id: kd_1ab2dedb-1149-40d4-a7f9-7e7bbf95cdd9
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 恢复 N3 原战斗锚点

> **状态：已执行完成（2026-10-17）**。N3 `worldDistance` 306.232910 → **266.116455**，世界坐标精确回到 `(-46.431503, 0, 248.09558)`、朝向 -45°；`ExitTailDistance` 94 → **54**，`Length` 回到 **266.1165**；`Travel_EV_N3.travelDistance` 41.116455 → **1.116455**。`scenery` 保持 **708**（未增删 Renderer）。已保存并关闭重载复核，Console 0/0。
>
> **一处前提需更正（不影响结论）**：本方案写「N3 下现有 70 个 Renderer，都挂在该锚点下」——**实测不成立**。70 个 Renderer 属于路线根下的独立组 `N3 Village Rescue - Accepted Art and Grey Wall Study @ 266`，该组**不是 N3 锚点的子对象**，且从未被移动过，至今仍在 `(-46.43, 0, 248.10)`。N3 锚点本身是**空节点**（`children=0`）。
>
> 因此上一轮的错误**不在村庄背景**，而在**逻辑战斗点**被推到了 306 m：玩家会在原 N3 村庄已经驶过之后才开打。本轮的修正仍然完全必要且正确。这也解释了为何恢复锚点后画面无需任何村庄子对象的补偿——它们本来就没动。

## 问题

当前逻辑节点仍是 N1、E0、J1、EV、N2、N3，没有新增节点。`EV Approach - Existing Shoulder` 只是 EV 下的 8 个景物。

错误是把 N3 从 266.116455 米挪到了 306.232910 米。世界坐标已从 (-46.4315, 0, 248.0956) 变为 (-74.80, 0, 276.46)，所以已验收的 N3 战斗背景相对战斗点发生了变化。N3 下现有 70 个 Renderer，都挂在该锚点下。

## 唯一修正

只改 N3 Presentation Anchor 的 `worldDistance`：

`306.232910 → 266.116455`

然后调用它的 `SyncTransformFromDistance()`。锚点和其全部村庄子对象会一起回到原位置。不要逐个改 N3 子对象，不要改 EV 的 265 米，也不要移动 `EV Approach`。

恢复后 N3 世界坐标必须重新为：

`(-46.431503, 0, 248.09558)`，朝向保持 -45°。

## 同步两项数据

- `Assets/RouteData/ChangbanpoScroll/Travels/Travel_EV_N3.asset` 的 `travelDistance`：41.116455 → **1.116455**。EV=265，N3=266.116455，差值就是 1.116455；duration 仍为 3。
- `Assets/Experiments/CurvedScroll/Scripts/YScrollSample.cs` 的 `ExitTailDistance`：94 → **54**。45° 路线长度必须回到 **266.116455**。保留此前 `Length + 0.2f` 的比较余量，不需要再改。

N2 保持 266.116455，不因恢复 N3 而改动。J1→EV 的 75 米旅行配置保持不变。

## 验收

编译后确认 `YScrollSample.Length == 266.116455`。用 Battle Main Camera、路线根临时 Y=-1.8，对比：

- 左路 265：现有 EV 村口和 8 件过渡景物仍在；
- 左路 266.116455：N3 回到原战斗位置，背景与延长前的 266 米画面一致；
- 右路 266.116455：N2 不变。

只保存 Y 场景、`Travel_EV_N3.asset` 和 `YScrollSample.cs`。scenery 数量保持当前 708，不增删 Renderer。

## 执行结果（2026-10-17）

| 项 | 执行前 | 执行后 |
|---|---|---|
| `N3.worldDistance` | 306.232910 | **266.116455** |
| N3 世界坐标 | (-74.80, 0, 276.46) | **(-46.431503, 0, 248.09558)** |
| N3 朝向 | Y=315° | Y=315°（保持）|
| `ExitTailDistance` | 94 | **54** |
| `YScrollSample.Length` | 306.1165 | **266.1165** |
| `Travel_EV_N3.travelDistance` | 41.116455 | **1.116455**（= 266.116455 − 265）|
| `Travel_EV_N3.duration` | 3 | 3（未动）|
| `Travel_J1_EV.travelDistance` | 75 | 75（未动）|
| `scenery` | 708 | **708**（未增删）|

`Length + 0.2f` 比较余量按方案保留，未再改。改常量后已 `unity_recompile` 完整重编，编译通过。

### 锚点全会校验

| 节点 | worldDistance | 世界坐标 | accepted |
|---|---|---|---|
| N1 | 0 | (0, 0, 0) | True |
| E0 | 90 | (0, 0, 90) | True |
| J1 | 190 | (0, 0, 190) | True |
| EV | **265（未动）** | (-45.64, 0, 247.31) | True |
| N2 | 266.1165（未动）| (46.43, 0, 248.10) | True |
| N3 | **266.1165** | (-46.43, 0, 248.10) | True |

### 画面验收（Battle Main Camera，根临时 Y=-1.8）

- **左路 265**：EV 村口与 8 件过渡景物仍在；
- **左路 266.116455**：屋顶/石墙/村道与 265 基本一致（相差仅 1.1 m），N3 战斗点现在就在村口；
- **右路 266.116455**：仍是荒野直道，**未串入任何左路村舍**，N2 未受影响。

### 其他确认

- 关闭重载复核：`Length=266.1165`、全部 6 个锚点数值与内存一致、`EV Approach - Existing Shoulder` 8 件完好、`scenery=708`；
- Console `0/0`；Battle 与 MainMenu 未保存；无探针残留。
