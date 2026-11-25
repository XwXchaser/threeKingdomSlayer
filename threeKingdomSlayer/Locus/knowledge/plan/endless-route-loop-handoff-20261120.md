---
id: kd_77163fa4-9cbf-4627-998f-66f0661c4428
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 无尽关「地图循环」交接（2026-11-20）

> 本文只做一件事：把**用户需求**和**代码里真正落地了什么**分开写清楚，并标出哪些是实测过的、哪些没有。
> 下一步从这里接着做，不要依赖上一轮对话。

## 0. 一句话现状

循环**框架已经打通**（无尽关自动开无限卷轴、一圈 380 米、相机与内容同时跨圈）；
**三条缺陷里两条已彻底修完并实测通过**（新 N1 无场景物件、新 E0 宝箱位置错）；
剩下**新 N1 路面跳变**（缺陷 1）—— 用户 2026-11-20 决定**先不动**，等圈 2/3 实机验收后再说。

> ⚠️ **2026-11-24 更新：本文以下部分口径已过时。** 地面已改为「每圈副本 + 互补半空间归属 + 走廊沿路 UV + 每圈相位」，
> **`copyLapRadius` 必须 ≥ 1**（不再是 0）；`i.vertex.xz * 世界缩放` 也不再是现行基线。
> 现行口径以 `design/endless-mode-design.md` §2.4 为准；取证与结案见 `plan/endless-ground-continuity-repair-plan-20261123.md` §13–14。

## 1. 需求（用户口径，不要再改）

1. 无尽关要**真正循环**：玩家一直往前走，地图在转（不是人在转），**不黑幕、不瞬移、不倒退**。
2. **剧情关（第一关等）表现必须一点不变**。
3. 循环时每一圈的节点场景都要**完整重新出现**，不能缺物件。
4. 允许"过渡段也是一个切片"，但过渡必须看起来是正常路面。
5. 用户报的三条验收缺陷（2026-11-20 最后一条消息）：
   - 新 N1 路面**跳变**，不是正常拼接；
   - 新 N1 **没有任何场景物件**；
   - 新 E0 **宝箱位置错**。

## 2. 已落地代码（都是当前工作区未提交状态）

### 2.1 核心框架：圈框架（刚体帧）

- `Assets/Experiments/CurvedScroll/Scripts/YRouteFrame.cs`（新增）：`位置 + 朝向` 的刚体帧，`Apply / Inverse / Compose`。
- `YScrollSample.cs`：
  - 删掉写死的 `ContentPeriod = 445`、`GetLapOffset`、`LapLength`；
  - 新增 `GetLapFrame(lap)`（第 0 圈恒为恒等）、`LapOf(distance)`、`EvaluateRoute(distance, ...)`；
  - `ComputePosition()` 改成走圈框架；`_YSPose.w = 局部朝向 + frame.heading`；
  - `endlessLoop` 为真时 `progress` 不再夹在 1.0；
  - `TryGetNodeProgress` 改成「取第一个 ≥ 当前距离的出现」（原来 `+1` 会在 progress=0 时把当前节点算成下一圈）。
- 三处摆放全部改刚体帧（原来是纯 +z 平移）：
  - `YRouteSegments.Load`（段 prefab）；
  - `ScrollRouteNodePresentation.SyncTransformForLap`（节点锚点及其子物件）；
  - `YLapFollower`（地面 / Route Connections / Encounter Authoring / N1 Far Landmarks）。

### 2.2 地面

- `YGroundJ1Branches.shader`：路面图案 UV 从「绕路线根算」改成「**副本自身局部米**」`i.vertex.xz * 世界缩放`；新增 `_YSCycleLength` 把每份地面**裁到本圈那一段**。
  ⚠️ **2026-11-24 已推翻**：现行归属是本圈本地空间的互补半空间（起点 `map.y` 平面 + 终点 `lapEnd/lapForward` 平面），
  路外 UV 恒为 `i.map/4`，只有道路走廊用沿路坐标并带每圈相位。
- `YLapFollower`：地面走**复制模式**；~~`copyLapRadius` 必须是 0~~ ⇒ **现在必须 ≥ 1**（相邻两份各负责接缝一侧，半径 0 会露洞）。
- 关闭无限卷轴时原件自动恢复可见（否则第一关地面会消失）。

### 2.3 可见性窗口

- `YRouteStreamingWindow`：
  - 采样表按 `route.Length` 变化**自动重建**（`EnsurePath / RebuildDistances`）——原来只在 Awake 建一次，无限卷轴把一圈从 325 改大后，超出 325 的内容全被当成"在身后"隐藏；
  - 新增 `SetTrackedDistance(renderers, lap)`：按物件当前位置**幂等重算**距离（不再用增量 `ShiftTrackedDistance`，圈号跳变会永久错位）；
  - `AddEntry` 记录 `trackedLap`。

### 2.4 玩法物件

- `RoutePropFollower.ApplyProjection`：锚点先套**本圈刚体帧**再投影（补给箱 / 宝箱按圈重现）。第 0 圈恒等 ⇒ 旧表现不变。

### 2.5 无尽关接线（正式关真正的入口）

- `IRouteMotionPresenter` 新增 `SetEndlessScroll(bool)`；`ScrollRoutePresenter` 实现（路线场景还没加载时先记下意图，加载完套用）。
- `EndlessRunController.StartEndlessRun()` → `SetEndlessScroll(true)`；结束流程 → `SetEndlessScroll(false)`。
- **这是关键修复**：此前正式关里无限卷轴**从来没被打开过**（`YRouteSliceLoader` 那个「卷轴实验」按钮在游戏里是空转的——它没拿到切片序列就直接 return）。
- `YRouteSliceLoader.ApplyOn` 也补了：没切片序列也能开（用场景里配的一圈长度）。

## 3. 已验证（实测，可复现）

| 项 | 结论 |
|---|---|
| 剧情关表现不变 | 同一 progress 下 `infiniteScroll` 开/关 **0 像素差**（583200 采样点全同） |
| 正式关能开无限卷轴 | Battle 里 `StartEndlessRun()` → `infinite=True`、`Length=380`、地面 1 份副本、原件隐藏 |
| 相机与内容同源 | 圈 1 局部 z 与圈 0 局部 z 落在同一屏幕坐标（x=0，偏差 0.00） |
| 前向扫描 0~2 圈 | **已复查：圈 0 / 圈 1 都是 0 空洞**（N3 段范围修正 + 新增 roadside 段之后） |
| 圈 1/2/3 距离账 | `Opening Roadside Props` 子 Renderer 的 `trackedDistance` = 386/422/391.5、766/802/771.5、1146/1182/1151.5 ✅ |
| 圈 1/2/3 宝箱 | `E0_Prop_Crate_01` 世界坐标恒为 `(3.60, -62.32, 100.00)`，与圈 0 逐位一致 ✅ |
| 剧情关零变化 | 物件可见的每个采样点，新旧投影公式结果完全一致（横向差 0.00m）✅ |

## 4. 用户报的三条缺陷：定位进度

### 缺陷 1：新 N1 路面跳变（未修）

- 根因（内容层，不是代码）：接缝两侧路面**材质本来就不同**。左支（去 N3）是村庄石路，下一圈起点是主路土路。圈边界正好切在两种材质之间 ⇒ 必然跳一下。
- 这不是靠改代码能修的，需要**过渡片**（石路→土路的过渡地面），或者把圈边界挪到材质一致的位置。
- 注意：`flatCycleLength = 380` 时边界落在内容末端 375.5 之后 4.5 米，跳变来自材质而非伸缩。

### 缺陷 2：新 N1 没有任何场景物件（**已修完，已实测**）

- 上轮那版改法（`OnLapChanged` 里改用幂等的 `SetTrackedDistance`）**方向对、但还差一步**：
  `OnLapChanged` 是在 `YScrollSample.Update` 里触发的，而 `YLapFollower` 的摆放原本只在 **LateUpdate** 做
  ⇒ 触发那一刻 transform 还是**上一圈**的世界位置 ⇒ 反算出来的沿路距离全部塌成 0。
- **实测证据**：圈 1 时 79 个子 Renderer 的 `trackedDistance` 全是 **380 / 380 / 380**（应是 386 / 422 / 391.5）；
  圈 2 是 760 / 760 / 760（应是 766 / 802 / 771.5）。物件「看起来还在」只是因为它们本来就在圈头附近。
- **修法**：`YLapFollower.OnLapChanged` 里**先套本圈刚体帧、再算距离**（把摆放抽成 `ApplyLapPlacement(lap)`，
  Update 与 LateUpdate 共用）。
- **复测**：圈 1 = 386 / 422 / 391.5，圈 2 = 766 / 802 / 771.5，圈 3 = 1146 / 1182 / 1151.5 ✅ 全对。

### 缺陷 3：新 E0 宝箱位置错（**已修完，已实测**）

- 上轮那版改法（锚点套本圈刚体帧后再投影）**必要但不充分**：漏了一个**单位错**。
- **根因**：`RoutePropFollower.ApplyProjection` 把 `route.TravelAngle` 当**弧度**喂给 `Sin/Cos`，
  但 `YScrollSample.TravelAngle` 是**度**。圈 0 时航向恒为 0 所以看不出来；一圈过 J1 弯、或第 2 圈起航向 ≠ 0
  ⇒ 角度被当弧度算 ⇒ 物件横向偏出去几十米。
- **实测证据**：圈 1 宝箱世界坐标 `(26.53, -57.24, 96.49)`，应是 `(3.60, -62.32, 100.00)`（横向偏 **23 米**）；
  同一公式按「度→弧度」换算后**精确复现圈 0 的几何**。
- **修法**：`float angle = route.TravelAngle * Mathf.Deg2Rad;`（`YScrollSample.TravelAngle` 加了单位注释）。
- **复测**：圈 1 / 2 / 3 宝箱世界坐标都是 `(3.60, -62.32, 100.00)`，与圈 0 逐位一致 ✅。

### 4.5 前向扫描剩下的空洞：**已补**（新增段 `EV to N3 - Roadside Approach`）

- **扫描口径**：每 10 米采样，看前方 40 米内有没有**已开启**的 Renderer。
- 上轮结果：d=365 / 375（圈 0）、745 / 755（圈 1）—— **这四个已消**（N3 段范围修正后）。
- 仍然剩：d=280（圈 0）/ d=660（圈 1）—— **不是代码问题**：EV 村内景结束于 274m，N3 村起始于 329m，
  中间 55m 确实什么都没铺。
- **补法**（2026-11-20，按用户「补一段布景」）：新增段 `EV to N3 - Roadside Approach`，
  声明范围 **[270, 328]**、内容 35 件（7 棵树 + 13 丛草 + 8 块石头），**全部复用 N3 村自己的路边美术**
  （`J1_Tree_Broad/Willow_v2` + 风动网格、`YGrassMesh_GrassMedium_v1`、`ValleyProps/RockRound`），
  偏移与缩放**逐件照抄 N3 村的 roadside 模板** ⇒ 视觉上与村口连续。
- 产出：`Authoring/Placement/EV to N3 - Roadside Approach.json` → `YSceneSpec` →
  `Authoring/Segments/EV to N3 - Roadside Approach.prefab` → 挂进 `YRouteSegments`。
- **复测**：圈 0 与圈 1 的前向扫描都是 **0 空洞** ✅。

### 4.6 剧情关零变化：**已验**（单位修正的副作用说明）

- 单位修正只在**航向 ≠ 0** 时改变物件的 CPU 位置；而三个 `RoutePropFollower` 物件的锚点都在 11 / 14 / 100m，
  相机不到 113m 它们就已经被 `hideBeyondZ=82` 隐藏 ⇒ **任何一帧可见时航向都恒为 0**。
- 实测：逐个 progress 采样（0 / 0.2 / 0.4 / 0.5 / 0.55 / 0.6 / 0.62 / 0.7 / 0.85 / 1.0），
  **凡是物件可见的采样点，新旧公式结果完全一致**（横向差 0.00m）。
- 附带好处：旧公式在 J1 弯（progress≈0.62）会算出 az≈1.8m ⇒ 把**已经走过 190m 的起始段木箱**
  重新画到镜头正前方；新公式与 shader 一致，这个剧情关鬼影也没了。

### 4.7 用户二次反馈的三条问题（2026-11-20 已定位，**未修**）

> 用户原话：「下一循环的 n1 还是跳变且场景内容物加载不正确 / 后续场景加载正确，但箱子奖励的形象变形了」。

**A. 下一循环 N1 跳变 = 接缝处路面材质硬切（不是相机跳）**
- 相机路径跨接缝是**连续**的：d=379 → travelPos(-126.25, 0, 327.91) heading -45°；
  d=380 → (-126.96, 0, 328.62) heading -45°，相差 1m ✅。
- 跳的是**内容**：
  1. **材质硬切**：左支是石板路（`YGroundJ1Branches` 的 `leftWeight`），下一圈起点是主路土路
     ⇒ 地面副本按圈换（`copyLapRadius=0`）⇒ 接缝处石板直接变土路。截图 d=378 / d=383。
     **✅ 2026-11-24 已修**：石板在圈界前淡回土路，两侧都是土路（见 `design/endless-mode-design.md` §2.4）。
  2. **接缝两侧是空路**：圈 0 最后一件内容是 `N3_Grass_*` @375.5~376；圈 1 第一件是
     `N1_GrassMedium_Left_01` @383 / `N1_BrokenCart_Left_01` @384 ⇒ **375.5~383 约 7.5m 什么都没有**。
     实测：前方 40m 内已开启物件 d=375 只有 **2 件**，d=380 变成 **60 件**。

**B. 场景内容物加载不正确：未复现“内容缺失”，但查出一个真的记账缺陷**
- 同一相对位置，圈 0（d=75）与圈 1（d=455）**已开启物件集合完全相同**（49 件、同名）；
  真实速度连续跑 7 圈（2692m）**0 空洞**；90m 跳变后窗口内容 1 帧内补齐（6→17）✅。
- 但：**分叉另一条路（右路）的物件，沿路距离账错了最多 43.9m**
  （例：`J1_Right_SmallRock_06` 存 250.5 / 真值 206.6；`J1_Right_Fence_02` 存 237 / 真 204.9）。
  864 个已登记渲染器里 **151 个偏差 > 2m**，圈 0 / 圈 1 完全一致 ⇒ **既有缺陷**，不是无尽循环引起的。
- 根因：`YRouteStreamingWindow.ProjectDistance` 同时投影**两条支路**取较近者，而相机位置是沿**左路**量的
  ⇒ 右路物件被拿去跟另一条轴比距离 ⇒ 淡出时机不对。

**C. 箱子奖励“形象变形”= 路线宝箱每圈跟着节点锚点转 -45°**
- 实测：`E0_Prop_Crate_01` 的**世界旋转** lap0 = 0°、lap1 = **315°**、lap2 = **270°**。
- 根因：`RoutePropFollower.ApplyProjection` 只写世界**位置**、从不写世界**旋转**
  （该物件 `followHeading=false`）⇒ 物件继承父链旋转，而父链 `Encounter Authoring → E0 Encounter`
  是节点锚点，`ScrollRouteNodePresentation.SyncTransformForLap` 会按圈框架把锚点**转 -45°/圈**。
- 对比：shader 弯折的景物是**故意不跟着转**的（shader 注释：*"Upright paper props face the travel view"*），
  ⇒ 圈 ≥1 时**只有箱子在自转**，看上去就是歪了。
- 像素证据：同一个箱子，圈 0 是正面方箱、圈 1 是转了 45° 的方箱（70×70 区域平均像素差 30/255）。
- 附带：这两个箱子用 `YSamplePropOutline`（shader `Custom/EnemyOutline`）⇒ 身上有一圈**亮米色描边**，
  与其它景物不一致（看着像“被选中的敌人”）。
- 若用户说的是**精英掉落箱** `EquipmentChest`：它是 48×48 的 `EquipmentChest_placeholder` 平铺占位图；
  且 `EquipmentChestManager.SpawnChest` 把 `Camera.WorldToScreenPoint` 的**屏幕像素**直接写进
  Canvas（scaleFactor **1.125**）的 `RectTransform.position` ⇒ 落点偏 **12.5%**。

## 5. 数据现状（YJunctionSample 场景，已落盘）

| 项 | 值 | 备注 |
|---|---|---|
| 一圈长度 `flatCycleLength` | **380** | 想改成 376 的那次没落盘 |
| `infiniteScroll` | 0 | 运行时由无尽关自动打开，**不要**在场景里写 1 |
| 节点距离 | 0 / 90 / 190 / (右 266.116 / 左 280 / 325) | |
| 内容实际末端 | **375.5** | 沿路投影最远物件（N3_Grass_R_15） |
| N3 村庄段声明范围 | **[322, 376]** | 实际内容铺到 **375.5**；原来写 [323,328] ⇒ 相机过了 358 米就把它卸掉 ⇒ 358~375 空路 |
| 新增段 `EV to N3 - Roadside Approach` | **[270, 328]** | 补 274~325 那 55m 空路（§4.5） |
| 地形 | 复制模式，`copyLapRadius = 1`（2026-11-24 起；0 会露洞） | |
| `N1 Far Landmarks` | 已挂 `YLapFollower`（搬家模式） | |

## 6. 硬约束（改之前先读，踩过）

1. **shader 只有一个全局朝向角**，同一时刻只能渲染**一个朝向**的内容 ⇒ 所有按圈摆放的副本，`copyLapRadius` 只能 0。
   ⚠️ **地面已不受此限（2026-11-24）**：每副本有 `_YSLapBranchSign` + 本圈本地空间，半径改为 ≥ 1；
   至于其它「搬家型」景物，本文这条限制仍有效。
2. **先注册再套帧**：刚设完根 transform 的 `localPosition`，同一帧读子 Renderer 的 `position` 会拿到旧值 ⇒ 全部登记到距离 0。`YRouteSegments.Load` 已按这个顺序写。
3. 采样表 / 距离表都要跟着 `route.Length` 重建，否则超过旧长度的内容会被判"在身后"。
4. 圈号可能跳变（检查点、直接改 progress）⇒ 距离同步必须**幂等**，不能用增量。
5. 剧情关（`infiniteScroll = false`）路径上，第 0 圈帧必须恒等、地面原件必须可见。

## 7. 下一步建议顺序（2026-11-20 更新）

1. ~~补验证缺陷 2 / 3~~ → **已完成**（§4.2 / §4.3，含根因与复测数字）。
2. ~~修数据：N3 段范围~~ → **已完成**（[322,376]，已确认落盘）。
3. ~~复查前向扫描~~ → **已完成**：圈 0 / 圈 1 都是 0 空洞（§4.5）。
4. **【当前】实机验收圈 2 / 圈 3**（用户 2026-11-20 决定）：先由玩家实机跑两圈，
   重点看 ① 新 N1 有物件、② 新 E0 宝箱位置对、③ 274~325 这段路不再空。
5. **缺陷 1（路面跳变）**（用户决定**先不动**）：接缝正好切在「左支村庄石路」与「下一圈主路土路」之间。
   两条路：**做石路→土路的过渡地面切片**，或**把圈边界挪到两侧材质一致的地方**（后者要同步改
   `flatCycleLength` 380↔376 与段范围）。这是内容工作，等用户拍板。

## 8. 工作区提交提醒

当前工作区混着三条互不相关的线，交接前建议分开提交：

- 无限卷轴（本文档 §2，`Assets/Experiments/**`、`IRouteMotionPresenter`、`ScrollRoutePresenter`、`EndlessRunController`、`YJunctionSample.unity`）；
- 属性池/装备池改造（`UpgradeStatIds`、`IUpgradeStatSink`、6 个新属性资产、池重排）；
- `Battle.scene` 里难度系数被改成 1、`ActiveSkillPoolConfig` 池调整 —— 需确认是调试值还是新基准。

## 9. 已知未提交/未落盘的东西（2026-11-20 更新）

- **一圈 `flatCycleLength` 仍是 `380`**（没改成 376）—— 这是**有意保留**：改它会动圈框架几何，
  而缺陷 1 的接缝位置还没拍板，两件事要一起决定。
- N3 段范围 `[322,376]` 与新增段 `[270,328]` **已落盘**（`grep` 场景文件回读确认）。
- 本轮新增未提交文件：`Authoring/Placement/EV to N3 - Roadside Approach.json`、
  `Authoring/Segments/EV to N3 - Roadside Approach.prefab`、
  `RouteData/YSceneSpecs/EV to N3 - Roadside Approach.asset`。
- 本轮改动未提交：`YLapFollower.cs`（先套帧再算距离）、`RoutePropFollower.cs`（度→弧度）、
  `YScrollSample.cs`（TravelAngle 单位注释）、`YJunctionSample.unity`（段范围 + 新增段）。
