---
id: kd_1571e5f3-0cc6-42ef-88b9-5237cee83a9e
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# J1→EV 过渡加长方案

> **状态：已部署完成（2026-10-17）**。`ExitTailDistance` 54→**94**，`Length` 266.1165→**306.1165**；EV 锚点 225→**265**、N3 锚点 266.116455→**306.232910**（均接受）；`Travel_J1_EV.travelDistance` 35→**75**；新建 `EV Approach - Existing Shoulder` 含 8 件。`scenery` **700 → 708**，空项 0、重复 0。已保存并关闭重载复核，Console 0/0。
>
> **一处对方案数值的必要修正**：`EVApproach_Grass_A` 的 X 由 −3.9 改为 **−4.05**（仅横向移 0.15）。按方案的 −3.9，可见（不透明）内缘只有 **3.50**，不满足方案自己要求的 `|x| >= 3.6`；−4.05 后为 **3.65**。其余 7 件的坐标、缩放、Z 全部按方案执行。详见下方执行结果。

## 目标

左弯结束后先走一段直路，再进入现有 EV 村口。弯道终点是 212.116455 米；EV 从 225 米改为 **265 米**，新增 40 米过渡。N3 保持在 EV 后 41.116455 米，改为 **306.232910 米**。N2 保持 266.116455 米，不延长右路。

每段旅行仍是 3 秒。只改 `Assets/RouteData/ChangbanpoScroll/Travels/Travel_J1_EV.asset` 的 `travelDistance`：35 → **75**。`Travel_EV_N3.travelDistance` 保持 41.116455。

## 必须先改路线长度

`YScrollSample.Length` 当前等于 `190 + 弧长 + ExitTailDistance(54)`，即 266.116455。`TryGetNodeProgress` 会拒绝大于 Length 的节点，所以不先加长就不能把 EV/N3 移到 265/306。

只把 `Assets/Experiments/CurvedScroll/Scripts/YScrollSample.cs` 的 `ExitTailDistance` 从 54 改为 **94**。不要改 `JunctionDistance` 或 `CurveRadius`。45° 时新长度应为 **306.116455**。

N3 的 306.232910 比这个长度多 0.116455。把比较改为允许 0.2 米余量：

```csharp
match.worldDistance > Length + 0.2f
```

不要为这 0.12 米再改半径或弧长公式。

地面 Shader 用各自的 `_JunctionDistance`，不读取 `ExitTailDistance`，所以地面几何不用改。改长度后要重新编译，再移动锚点。

## 移动锚点

只改两个 `ScrollRouteNodePresentation.worldDistance`：

- EV：225 → 265
- N3：266.116455 → 306.232910

调用各自的 `SyncTransformFromDistance()`。锚点下的 `EV Village Outskirts` 和 N3 村庄组是子对象，会自动跟着移动，禁止手改它们的世界坐标。J1、N2、E0、N1 不动。

改完后核对：

- EV 朝向约为 Y=-45°，N3 同样保持 -45°。
- N2 世界坐标和 266.116455 不变。
- 弯道终点仍是 212.116455；265 位于弯后直线上。

## 过渡景物

在 EV 锚点下新建 `EV Approach - Existing Shoulder`。组局部坐标沿锚点前方为 +Z、左为 -X。因为锚点在 265，目标路径距离 d 的局部 Z 是 `d - 265`。

全部复用现有对象，局部 Y=0，Scale 三个分量均为正数，路线材质用 `YSampleScenery_E0Route`，草用 `YGrass_E0Route`。转向只用 `flipX`。内缘保持 `|x| >= 3.6`。

| 对象 | 来源 | Local Position | Scale |
|---|---|---|---|
| EVApproach_Fence_A | `J1_A04_BrokenLowFence_v1` | (-5.4, 0, -48) | 0.46 |
| EVApproach_Fence_B | 同上 | (-5.8, 0, -36) | 0.42 |
| EVApproach_Fence_C | 同上 | (-5.2, 0, -24) | 0.38 |
| EVApproach_Rock_A | `e0_j1_r1_rubble_v1` | (5.2, 0, -44) | 0.24 |
| EVApproach_Rock_B | 同上 | (5.6, 0, -31) | 0.22 |
| EVApproach_Cart | `J1_A05_CompleteWoodenCart_v1` | (6.0, 0, -18) | 0.34 |
| EVApproach_Grass_A | 现有 EV 风动草 | (-3.9, 0, -41) | 0.22 |
| EVApproach_Grass_B | 同上 | (4.0, 0, -27) | 0.20 |

这些 Z 分别对应路径 217、229、241、221、234、247 米。不放房屋、柴门、新石墙或装货板车，避免把村口提前。

把 8 个 Renderer 追加到 scenery。当前基线先回读；只追加，不重排旧项。结果应比基线多 8，空项 0、重复 0。

## 验收与保护

先编译并确认新 Length=306.116455。用 Battle Main Camera、路线根临时 Y=-1.8，看左路 212、230、250、265、306，以及右路 236、266。

通过标准：212 仍是弯道末端，230–250 只有篱、石、空车和草，265 才出现现有村口，306 的 N3 构图不变；右路没有被拉长或串入左路物体。

只保存 Y 场景和 `Travel_J1_EV.asset`。`YScrollSample.cs` 是唯一代码改动。不改 J1、EV 村口内部、N3 村庄内部、道路 Shader、共享材质和 N2。

## 执行结果（2026-10-17）

### 代码与资产

| 项 | 原值 | 新值 |
|---|---|---|
| `YScrollSample.ExitTailDistance` | 54 | **94** |
| `YScrollSample.TryGetNodeProgress` 上限 | `Length` | **`Length + 0.2f`** |
| `YScrollSample.Length` | 266.1165 | **306.1165** |
| `Travel_J1_EV.travelDistance` | 35 | **75** |
| `Travel_EV_N3.travelDistance` | 41.116455 | 41.116455（未动）|

改常量后已 `unity_recompile` 完整重编（常量属静态初始化器，不支持热重载），编译通过、域重载完成。`JunctionDistance=190` 与 `CurveRadius=28.15956` 未动，弯道终点仍为 **212.1165**。

### 锚点

| 节点 | 原 worldDistance | 新 worldDistance | 新 worldPos | 朝向 |
|---|---|---|---|---|
| EV | 225 | **265** | (-45.64, 0, 247.31) | Y=315°（=-45°）|
| N3 | 266.116455 | **306.232910** | (-74.80, 0, 276.46) | Y=315° |
| N2 | 266.116455 | 266.116500（未动）| (46.43, 0, 248.10) | Y=45° |

两个锚点均用 `SyncTransformFromDistance()` 移动，村口/N3 村庄作为子对象自动跟随，未手改世界坐标。`nodePresentations` 共 6 项、无重复，全部 `TryGetNodeProgress` 通过（N3 的 progress 为 1.000380，即 0.2 m 容差生效的确切证据）。

### 过渡景物（8 件，`EV Approach - Existing Shoulder` 挂在 EV 锚点下）

| 对象 | Local Position | Scale | 内缘（可见）| 路径距离 |
|---|---|---|---|---|
| EVApproach_Fence_A | (-5.4, 0, -48) | 0.46 | 3.97 | 217 m |
| EVApproach_Fence_B | (-5.8, 0, -36) | 0.42 | 4.50 | 229 m |
| EVApproach_Fence_C | (-5.2, 0, -24) | 0.38 | 4.02 | 241 m |
| EVApproach_Rock_A | (5.2, 0, -44) | 0.24 | 4.48 | 221 m |
| EVApproach_Rock_B | (5.6, 0, -31) | 0.22 | 4.94 | 234 m |
| EVApproach_Cart | (6.0, 0, -18) | 0.34 | 4.06 | 247 m |
| EVApproach_Grass_A | (**-4.05**, 0, -41) | 0.22 | 3.65 | 224 m |
| EVApproach_Grass_B | (4.0, 0, -27) | 0.20 | 3.64 | 238 m |

局部 Y 均为 0、等比正缩放、`flipX` 全 false、Sorting Default/0；6 件用 `YSampleScenery_E0Route`、2 丛草用 `YGrass_E0Route`。

**内缘测量口径**：内缘必须按**贴图不透明像素**算，不能按 Sprite 矩形算。矩形带透明留白会虚假报警：例如 `J1_A05` 车图 1536 px 宽但只有 x=[260..1406] 有内容，按矩形算得 3.39（误判违规），按不透明范围算实为 **4.06**（合格）。只有 `EVApproach_Grass_A` 是真的越界（3.50 < 3.6），已横向修正。

**Z → 路径距离映射已验证**：8 件的世界坐标与 `Evaluate(265 + localZ)` 的距离恰好等于各自的 `|x|`（正交偏移），且 heading 均为 -45°，证明 -Z 确实是锚点前方。

### 验收实测

组 OFF vs ON 像素差（阈值 ≥8）：

| 路径距离 | 本组可见像素 | 画面 |
|---|---|---|
| 212 m | 3473 | 弯道末端，只有礁石与树木，无村口 |
| 230 m | 2678 | 篱、石、空车、草可见，无村口 |
| 250 m | 0 | 村口已出现，本组已驶过 |
| 265 m | 0 | EV 村口完整（左右屋舍、石墙）|
| 306 m | 0 | N3 构图 |
| 右路 236 m | **0** | 未串入 |
| 右路 266 m | **0** | 未串入 |

- **中带（屏幕中间 20%）在所有距离均为 0 像素**——无道路侵入；
- **右路完全未受影响**（两处均 0）；
- **村口没有提前**：212 m 画面只有弯道末端与路侧树石；250 m 才出现屋舍，265 m 完整。符合“不放房屋/柴门/新石墙”的要求。

本组可见窗口为 217–241 m（250 m 已过完）。与上一段 E0→J1 一致，驶过即离开视野，属几何必然。

### 其他确认

- 关闭重载复核：`Length=306.1165`、`scenery=708`、8 件位置/缩放/材质与内存一致；
- `PlayerSettings` 与其他场景未动；Battle 与 MainMenu 未保存；Console `0/0`。
