---
id: kd_f5d147a6-617e-4c1a-9096-78b47fb734ee
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# J1→EV 90 米、EV→N3 45 米部署交接

## 当前事实

2026-10-17 只读核对，Active Scene 是 `Assets/Experiments/CurvedScroll/YJunctionSample.unity`：

- scenery=704，空项 0。
- `ExitTailDistance=54`，Length=266.116500。
- J1=190，EV=225，N2=266.116500，N3=266.116500。
- `Travel_J1_EV.travelDistance=35`，`Travel_EV_N3.travelDistance=41.116455`，两段 duration=3。
- `EV Approach - Existing Shoulder` 已停用，仍有 8 个 Renderer。
- N3 背景组 `N3 Village Rescue - Accepted Art and Grey Wall Study @ 266` 在路线根下，不在 N3 锚点下，当前世界坐标与 N3 锚点同为 (-46.43150, 0, 248.09560)。

目标距离：

- N1→E0=90，E0→J1=100，保持不变。
- J1=190，EV=**280**，因此 J1→EV=**90**。
- N3=**325**，因此 EV→N3=**45**。
- N2 保持 266.116500，右路不延长。

## 1. 先改代码并编译

`Assets/Experiments/CurvedScroll/Scripts/YScrollSample.cs`

- `ExitTailDistance`：54 → **113**
- 45° Length 必须约为 **325.116500**
- 不改 `JunctionDistance=190`、`CurveRadius`。
- 保留现有 `worldDistance > Length + 0.2f` 判断。

编译完成前不要改 280/325 锚点，否则 `TryGetNodeProgress` 会拒绝超长节点。

## 2. 改两份旅行资产

- `Assets/RouteData/ChangbanpoScroll/Travels/Travel_J1_EV.asset`：`travelDistance` 35 → **90**
- `Assets/RouteData/ChangbanpoScroll/Travels/Travel_EV_N3.asset`：`travelDistance` 41.116455 → **45**
- duration 都保持 3。

## 3. 移动两个锚点

路径：

`Assets/Experiments/CurvedScroll/YJunctionSample.unity/Y Junction - select for preview/Route Nodes - N1 E0 J1 and Branches`

- EV Presentation Anchor：`worldDistance` 225 → **280**，调用 `SyncTransformFromDistance()`。
- N3 Presentation Anchor：`worldDistance` 266.116500 → **325**，调用 `SyncTransformFromDistance()`。

预期世界坐标：

- EV：(-56.24865, 0, 257.91272)，朝向 -45°。
- N3：(-88.06846, 0, 289.73253)，朝向 -45°。

EV 村口组是 EV 子对象，会自动到新村口。不要改它的 4 个内部子组。

## 4. 平移整个 N3 背景

选择路线根下的整组：

`N3 Village Rescue - Accepted Art and Grey Wall Study @ 266`

当前 (-46.43150, 0, 248.09560)。整体增加：

`(-41.63695, 0, 41.63695)`

结果应为 (-88.06845, 0, 289.73255)。只改这个组的 Transform，不改 70 个内部 Renderer 的局部坐标、缩放或材质。平移后它应与新的 N3 锚点重合。

## 5. 停用旧过渡引用

`EV Approach - Existing Shoulder` 已经 inactive。从 `YScrollSample.scenery` 移除它的 8 个 Renderer 引用并压缩数组，不删除对象，不留 null。704 减去 8 后基线为 **696**。

## 6. 新建直路景物

在路线根下新建 `J1 to EV - Village Approach`，不要挂到 EV 或 N3 锚点。复制现有对象，材质保持 `YSampleScenery_E0Route`，局部 Y=-0.02，Scale 三个分量均为正数。转向只用 `flipX`。

坐标已经转换成路线根世界坐标：

| 对象 | 复制源 | Position | Scale |
|---|---|---|---:|
| Approach_Fence_A | `J1_A04_BrokenLowFence_v1` | (-21.035, -0.02, 215.345) | 0.42 |
| Approach_Rock_A | `e0_j1_r1_rubble_v1` | (-18.489, -0.02, 227.790) | 0.24 |
| Approach_Cart | `J1_A05_CompleteWoodenCart_v1` | (-23.722, -0.02, 233.871) | 0.34 |
| Approach_Fence_B | `J1_A04_BrokenLowFence_v1` | (-37.440, -0.02, 231.467) | 0.40 |
| Interior_House_Left_A | `N3_H1_CompactHouse_v1` | (-44.935, -0.02, 238.114) | 0.78 |
| Interior_House_Right_A | `N3_H2_LongHouse_v1` | (-41.965, -0.02, 252.397) | 0.74 |
| Interior_House_Left_B | `N3_H3_DamagedHouse_v1` | (-56.531, -0.02, 249.145) | 0.70 |

225–248 米只有篱、石和空车；258 米后才出现房屋。摆完检查每件可见内缘离路径中心至少 3.6 米，中央道路不得被房屋或车侵入。

7 个 Renderer 追加到 scenery。最终应为 **703**，空项 0、重复 0。

## 7. 验收

用 Battle Main Camera，路线根临时 Y=-1.8，结束恢复。检查左路 212、240、280、300、325，以及右路 266。

通过标准：

- 212 仍是弯道末端。
- 240 没有房屋。
- 280 是完整 EV 村口，不包含已停用的 `EV Approach`。
- 325 的 N3 锚点与整个 N3 背景组重合。
- N2 仍在 (46.43150, 0, 248.09560)。

只保存 Y 场景、两份 Travel 资产和 `YScrollSample.cs`。不新增逻辑节点，不改 EV 村口内部、N3 背景内部、道路 Shader、共享材质或右路。

## 执行状态（2026-10-17 已落地，待用户开游戏验收）

七步全部执行，只写 Y 场景、两份 Travel 与 `YScrollSample.cs`。

- `ExitTailDistance` 54 → **113**，`GetRouteLength(45)` = **325.116500**；`JunctionDistance`、`CurveRadius`、`worldDistance > Length + 0.2f` 判断均未动。先改代码并 `unity_recompile` 完整编译通过，才改 280/325 锚点。
- `Travel_J1_EV` 90、`Travel_EV_N3` 45，duration 均 3。
- EV `worldDistance` 225 → **280**，世界坐标 (-56.24865, 0, 257.91270)；N3 **325**，(-88.06846, 0, 289.73250)；两者均 -45°。
- `N3 Village Rescue` 背景整组平移 (-41.63695, 0, 41.63695) → (-88.06845, 0, 289.73250)，与 N3 锚点重合；70 个内部 Renderer 未动。N2 仍在 (46.43150, 0, 248.09560)，J1 仍 190。
- 新建 `J1 to EV - Village Approach`（路线根下，局部 0），7 件均为复制体，材质 `YSampleScenery_E0Route`，Y=-0.02，正 Scale，`flipX=false`。实测路径距离 225 / 232 / 240 / 248（篱、石、车）+ 258 / 266 / 274（房屋），符合「225–248 只有篱石车、258 后才出现房屋」。

### 与方案不符之处（已按用户确认处理）

1. **第 5 步是空操作**：`EV Approach - Existing Shoulder` 的 8 个 Renderer 在上一轮部署时已从 `scenery` 移出，本轮开始时 `scenery` 已是 704 而非 712，所以「704−8=696」无法复现。
2. **`EV to N3 - Village Interior` 旧组**（4 座房屋在 236/242/250/256 米）会违反「225–248 米只有篱石车」「240 没有房屋」两条标准。已按用户选择整组 `SetActive(false)` 并从 `scenery` 移除 4 个 Renderer，对象保留可回看。
3. 因此最终 `scenery` = 704 − 4 + 7 = **707**（方案预估 703），空项 0、重复 0。

### 内缘校验

按地面 Shader 的垂直距离口径（45° 段法线 (-0.7071, -0.7071)，用 alpha 可见边界）：篱 4.285、石 4.893、车 4.622、篱 B 4.529、房屋 4.123 / 4.945 / 4.562 米，全部 ≥ 3.6，方案给的坐标未做调整。

### 距离分段实测

`J1 to EV - Village Approach` [223.9, 277.5]、`EV Village Outskirts` [276.1, 321.4]、`N3 Village Rescue` 背景组 [327.7, 377.2]。EV 村口组与新组末端在 276–277 米仅有包围盒相邻；EV 村口组止于 321.4、N3 背景组从 327.7 起，N3 战斗锚点 325 落在这段约 6 米空隙中。

未做：未跑截图自检，按用户偏好由用户开游戏验收。
