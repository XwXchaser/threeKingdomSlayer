---
id: kd_52210ae5-d98e-4831-8c4e-d5af84be3119
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
summary: Y 卷轴场景重建方案：把「手摆的大场景」改成「spec 数据 → 生成器 → 自检 → 预览」的工作流；含四层分层、spec 字段设计、sortBias 层级规则（修城门楼压全场的问题）、N3 立体围墙的可复用配方、从现状迁移的批次与验收口径、加载与包体的诚实结论。
injectMode: inherit
aiEditMode: inherit
---

# Y 卷轴场景重建方案（2026-10-23）

> 目标（用户原话）：**整个 Y 场景从零生成**，按参考图与美术素材部署节点场景内容；
> 做成**工作流**（能搭、能微调）；**只搭场景**，不负责节点内容/战斗内容部署，但会搭可交互物体（如箱子）；
> 要**自检** + **能直接预览**（倾向做「跳过节点战斗」的小按钮，方便打包到手机上测）。
> 现状问题：**E0 前方城楼压在所有场景物之上**（根因已定位 ✅）；**N3 立体沿路围墙是成功案例** ✅。
> 明确不做：**不碰贴图压缩** ✅（包体压缩另议 ✅）、不碰战斗内容 ✅、不删 Legacy ✅。

---

## 一、现状事实（实测口径，2026-10-23）

### 1.1 场景构成（`Assets/Experiments/CurvedScroll/YJunctionSample.unity`）

| 项 | 实测 |
|---|---|
| 根对象 | 2 个（`Y Junction - select for preview` ✅ + `Sample Camera` ✅） |
| 规模 | 276 Transform / 189 Renderer |
| 顶层分组 | 8 个：Terrain（1 mesh ✅）· Skybox ✅ · `N1 Far Landmarks` ✅ · `Route Nodes - N1 E0 J1 and Branches`（6 节点锚点 ✅ / 57 Renderer ✅）· `Route Connections`（5 条 ✅）· `Opening Roadside Props`（92 子 ✅）· `N1 to E0 - Shoulder Detail v1` ✅ · `Encounter Authoring`（战斗 ✅ 不在本次范围 ✅） |
| 节点 | 6 个锚点：N1 / E0 / J1 / EV / N2 / N3 ✅ |
| 连接 | 5 条：N1→E0 · E0→J1 · J1→EV(左) · J1→N2(右) · EV→N3 ✅ |
| 段 prefab | `Authoring/Segments/` 20 个 ✅；`YRouteSegments` 段表 **14 条** ✅（带 start/end 米数 ✅） |
| 立体围墙 | `EV_Wall_*_Segment_01/02` 等 **6 件静态对象** ✅（挂 EV 锚点下 ✅） |
| 地面 | **一个地形 mesh** ✅（`J1_Ground_Branches.mat` ✅，分叉画在贴图里 ✅），5 张贴图：`_LeftStoneRoad` / `_LeftStoneSides` / `_Outer` / `_Road` / `_Shoulder` ✅ + `_BranchMaterialBlend=0.6` ✅ |
| 交互物 | `N1_Prop_Crate_01`（`BreakableProp` ✅，挂 `N1 Encounter` 下 ✅） |

### 1.2 距离与路线的权威来源

- `YScrollSample`：`JunctionDistance = 190f` ✅（分叉在 190m ✅）、`CurveRadius` ✅、`ExitTailDistance = 113f` ✅、
  `Evaluate(distance, degrees, right, out heading)` ✅（唯一权威的「米数 → 位置」映射 ✅）。
- **节点米数不等距** ✅（`plan/scroll-on-demand-loading-plan.md` 明令「禁止按节点编号或等距推算」 ✅）→
  权威值在 `ScrollRouteNodePresentation.worldDistance` ✅ → **spec 必须引用它，不能自己算** ✅。
- 坐标是**展开态**（flat）✅ —— `ScrollWorldItem` 明确「Transform 永远是展开态的作者位」 ✅，弯曲由 shader 做 ✅。
  ⇒ **生成器写展开态坐标** ✅。

### 1.3 渲染层级（本次要修的真 bug）

- 全场布景精灵：`sortingOrder = 0` ✅、`sortingLayerID = 0` ✅（Default ✅）→ **同层** ✅；同层内按 **z** 排 ✅（flat world ✅，z 即纵深 ✅）。
- 精灵 shader：Transparent 队列 + **ZWrite Off** ✅ → 精灵之间**没有深度遮挡** ✅。
- 立体件 shader（`YVillageWallPrototype` ✅）：**Geometry+5 + ZWrite On** ✅ → **会写深度** ✅ → 能实打实遮住后面的一切 ✅。
- **BUG 现场**：`Authoring/Segments/E0 Integrated - Gate and Aftermath @ 90.prefab`
  → 子组名 `Gate Facade - Depth Sorted` ✅ → 城门楼 `order=1` ✅、两侧门墩 `order=2` ✅、低墙 `order=0` ✅。
  ⇒ Unity 里 **sortingOrder 越大越靠前** ✅ → 整组被抬到世界之上 ✅ → **城门楼压在所有场景物之上** ✅。
  ⇒ 根因：为了解决「同一 z 的城门楼与门墩分不出先后」✅，人工把 order 往上抬 ✅，**方向错了** ✅。
- **正确规则**：组内相对关系用**相对偏移**表达 ✅，整组基准必须**不高于世界基准** ✅（城门楼组应落在 −2/−1 ✅）。
- 同段的 `... - Approach` prefab 全是 order 0 ✅（无此问题 ✅）。

### 1.4 N3 立体围墙（成功案例，要复用的配方）

| 组成 | 路径 |
|---|---|
| 4m 段网格（4 变体：开口/收口） | `Authoring/N3Village/N3_Wall4m_{Open_Open, Open_End, Start_Open, Start_End}.asset` ✅ |
| 描边材质 | `Authoring/N3Village/N3_StoneWall_Outline.mat` / `N3_GreyWall_Outline.mat` ✅ → shader `YVillageWallPrototype.shader` ✅ |
| 贴图 + **机器可读切片清单** | `Art/EnvironmentV2/N3/Village/N3_WallSurface_G1_Atlas.png` ✅ + `slice_manifest.json` ✅（name / path / sha256 / 图集区域 / alpha 轮廓 bbox / crop_origin / size / padding / **pivot_normalized** ✅） |
| 场景用法 | 父组 + 2 段接龙 ✅（`EV_Wall_Left_Entry` → `Segment_01/02` ✅） |

⇒ **`slice_manifest.json` 就是「机器可读摆放数据」的现成范式** ✅ → 新 spec 直接沿用其字段 ✅。

### 1.5 加载与预载（已存在，不要重造）

- `YRouteStreamingWindow` ✅：沿路米数窗口 `[−28, +105]` ✅，只切 `Renderer.enabled` ✅；常驻窗口 `[−30, +120]` ✅；`alwaysResidentRoots` 6 项 ✅。
- `YRouteSegments` ✅：段 prefab **停留期按需实例化** ✅，预载提前量 = 最大节点间距 + 渲染窗口 + 10 ✅，离开即销毁 ✅。
- 路线场景 **additive 加载** ✅（`ScrollRoutePresenter.EnsureRoute` ✅）→ 启动不一次性全加载 ✅。

### 1.6 现有可复用工具（别重造 ✅）

`Tools/Curved Scroll/*` 11 项 ✅（世界生成 / 连接校验 / 世界预览 / 风动批量 ✅）·
`Tools/三国杀戮/战斗数值总表` ✅ · `Tools/Fake Route/Validate Assets` ✅ · `Tools/敌人/脚底对齐` ✅。

---

## 二、目标架构

```
┌─ 数据层（可编辑、可 diff、可校验）──────────────────────────┐
│ YSceneSpec (SO, 每条路线一份)                              │
│   ├─ terrain: 地形 mesh + 每条分支的地面材质（5 贴图+混合）  │
│   ├─ nodeSpecs[]:  nodeId / 锚点米数(引用 worldDistance) /    │
│   │                分组 / propEntries[] / interactives[]      │
│   ├─ segmentSpecs[]: 段 prefab + 装载窗口[start,end] + 常驻   │
│   └─ groundProfiles[]: 每条分支的地面材质                     │
└───────────────────────────────────────────────────────────┘
        │ 生成器（Editor，只写 spec 负责的条目，幂等）
        ▼
┌─ 产物层 ─────────────────────────────────────────────────┐
│ 场景：YJunctionSample（骨架 + 常驻层）                     │
│  prefab：每节点一个布景 prefab / 每段一个段 prefab          │
│  段表：写回 YRouteSegments.segments（与现有装载机制对接）    │
└───────────────────────────────────────────────────────────┘
        │ 校验器（生成后自动跑）
        ▼
   自检报告（错误阻断 / 警告列出）
```

**四层分层**（回答「节点 + 过道怎么区分」✅）：

| 层 | 内容 | 归属 | 常驻策略 |
|---|---|---|---|
| **L0 常驻** | 地形 / 天空盒 / 远景地标 / 路线逻辑（连接、锚点、采样器） | 场景里 | 永不卸载 ✅ |
| **L1 过道段** | 节点之间的沿路装饰（路肩、草、篱笆、石块） | 段 prefab | `YRouteSegments` 按需 ✅ |
| **L2 节点布景** | 节点周边景物（路牌、城门、车厢、村口） | 节点 prefab | 停留时装载 ✅（新增 ✅） |
| **L3 交互物** | 箱子等可交互物件 | 跟节点 prefab / 或独立条目 | 随节点 ✅ |

---

## 三、数据模型（字段表）

### 3.1 `YSceneSpec`（ScriptableObject ✅，不用 JSON ✅）

| 字段 | 类型 | 说明 |
|---|---|---|
| `routeId` / `stageId` | string / int | 与 `FakeRouteStageConfig` 对齐 ✅ |
| `terrainMesh` | GameObject | 地形（**给定输入** ✅，生成器不生成地形 ✅） |
| `groundProfiles[]` | `YGroundProfile` | 每条分支：地面材质 ✅ + 混合系数 ✅ + 贴图槽位（可选 ✅，缺省用材质自带 ✅） |
| `nodeSpecs[]` | `YNodeSceneSpec` | 见下 ✅ |
| `segmentSpecs[]` | `YSegmentSpec` | 段 prefab ✅ + `startDistance` / `endDistance` ✅ + `alwaysResident` ✅ |
| `renderWorldBaseline` | int | **世界基准 sortingOrder** ✅（默认 0 ✅）→ 校验器用它判「越界」✅ |

### 3.2 `YNodeSceneSpec`

| 字段 | 说明 |
|---|---|
| `nodeId` | 对齐 `FakeRouteNodeConfig.nodeId` ✅（校验器检查 ✅） |
| `anchorDistance` | **米数** ✅（必须等于 `ScrollRouteNodePresentation.worldDistance` ✅） |
| `groups[]` | 组名 + 基准 `sortBias` ✅（组的基准必须 ≤ `renderWorldBaseline` ✅） |
| `propEntries[]` | 见下 ✅ |
| `interactives[]` | 可交互物（`BreakableProp` prefab ✅ + 位置 ✅） |

### 3.3 `YPropEntry`（一条布景）

| 字段 | 说明 | 对应现有事实 |
|---|---|---|
| `prefab` | 精灵 prefab 或 3D 段 prefab ✅ | `Authoring/Segments/*` ✅ |
| `group` | 归入哪个组 ✅ | 场景里的分组名 ✅ |
| `localPosition` / `localRotation` | **展开态** ✅ 局部坐标 ✅ | 场景里的作者位 ✅ |
| `renderMode` | `Sprite` \| `Solid3D` ✅ | 精灵 = Transparent+ZWriteOff ✅ / 3D = Geometry+ZWriteOn ✅ |
| `sortBias` | **相对所属组的偏移** ✅（不是绝对值 ✅） | 修城门楼那个 bug 的关键 ✅ |
| `pivotMode` | `Foot`（脚点，贴地 ✅）\| `Center` ✅ | 现有导入约定：pivot 按 Alpha 底边 ✅（`slice_manifest.pivot_normalized` ✅） |
| `footOffsetY` | 脚点补偿 ✅ | J1 那轮的「Alpha 底边局部 Y 补偿」 ✅ |
| `windMeshOverride` | 风动网格（树/旗）✅ | `WindScenery` ✅ / `YSceneryWind` ✅ |

### 3.4 关键约定（写进 spec 注释里 ✅）

1. **组基准 ≤ 世界基准** ✅ → 任何组都不会整体压到世界之上 ✅（城门楼组用 −1 ✅）。
2. **同组内用 sortBias 表达前后** ✅；同 z 的构件靠 sortBias 稳定分层 ✅。
3. **`Solid3D` 的 z 必须大于它要遮挡的精灵 z** ✅（3D 写深度 ✅，会吃掉更远的东西 ✅）→ 校验器检查 ✅。
4. **`pivotMode=Foot` 的构件，其排序深度取脚点** ✅ —— 与 Unity 透明排序一致 ✅（精灵按 pivot 深度排 ✅）。
5. **坐标是展开态** ✅ —— 生成器不套用任何弯曲 ✅。

---

## 四、生成器（Editor 工具，放 `Locus.EditorTools` ✅）

### 4.1 职责

1. 读 spec → 在目标场景里**按命名规则找到已有组并更新** ✅（幂等 ✅）。
2. 产出三种产物：**节点 prefab** ✅、**段 prefab** ✅、**场景骨架 + 常驻层** ✅。
3. 写回 `YRouteSegments.segments` ✅（段表 ✅）与 `ScrollRouteNodePresentation` ✅（锚点 ✅）。
4. 生成后自动跑校验器 ✅。

### 4.2 幂等与微调保护（最容易翻车的地方 ✅）

- 命名规则：`{节点Id}/{组名}/{条目名}` ✅ → 生成器靠名字找回已有对象 ✅。
- 每条条目带 `generatedBy = <spec 指纹 + 条目 id>` ✅（存在条目上的隐藏组件/字段 ✅）。
- **你手改过的条目**打上 `userModified` ✅ → **再生成时跳过** ✅，只在自检里提示「有 N 条被手工改过，生成器已跳过」✅。
- 提供「强制重生成」开关 ✅（忽略 `userModified` ✅）与「导出当前场景为 spec」反向工具 ✅（把手摆的结果导成数据 ✅ → **这是迁移的关键工具 ✅**）。

### 4.3 产物：节点 prefab（解决「节点内容没法按需卸载」✅）

- 每个节点布景 → 一个 prefab ✅（含该节点的 propEntries + interactives ✅）。
- 运行时：新增一个轻量 `YNodeSceneLoader` ✅（挂在 `YScrollSample` ✅）—— 玩家靠近节点停留时实例化该节点 prefab ✅、离开后销毁 ✅（与 `YRouteSegments` 同一套节奏 ✅）。
- 节点 prefab 与段 prefab **互不重叠** ✅（一个按节点区间 ✅，一个按沿路区间 ✅）→ 自检要查重叠 ✅。

### 4.4 预览

- 复用 `ScrollWorldPreviewWindow` ✅（整条路线 ✅）。
- 新增**节点 prefab 单体预览** ✅（选中一个 nodeSpec → 单独渲染该节点 ✅，不用跑整条路线 ✅）。

---

## 五、自检规则（生成后自动跑 ✅，错误阻断 ✅）

| # | 规则 | 级别 |
|---|---|---|
| 1 | 节点米数**单调递增** ✅ 且与 `ScrollRouteNodePresentation.worldDistance` **一致** ✅ | 错误 ✅ |
| 2 | 段区间不重叠、不留空洞 ✅（除显式 `alwaysResident` ✅） | 错误 ✅ |
| 3 | 节点 prefab 与段 prefab 的**区间不重叠** ✅ | 错误 ✅ |
| 4 | 所有 prefab / 材质引用非空且存在 ✅ | 错误 ✅ |
| 5 | `nodeId` 与 `FakeRouteNodeConfig.nodeId` **对齐** ✅（不认识的节点报错 ✅） | 错误 ✅ |
| 6 | **组基准 ≤ 世界基准** ✅（城门楼那类问题 ✅） | 错误 ✅ |
| 7 | **同组内 sortBias 唯一/单调** ✅（避免同 z 互相穿插 ✅） | 警告 ✅ |
| 8 | **`Solid3D` 的 z > 它要遮挡的精灵 z** ✅ | 错误 ✅ |
| 9 | `pivotMode=Foot` 的精灵**贴地** ✅（脚点 y ≈ 0 ✅） | 警告 ✅ |
| 10 | 每节点 Renderer 预算 ✅（超阈值提示 ✅） | 警告 ✅ |
| 11 | 交互物（`BreakableProp`）都在**某个节点**内 ✅（不落进过道 ✅） | 警告 ✅ |
| 12 | `userModified` 条目统计 ✅ | 提示 ✅ |

> 规则 6/7/8 就是这次层级 bug 的**固化** ✅ —— 以后不会再有人手抬 order ✅。

---

## 六、从现状迁移（不推倒重来 ✅）

### 6.1 迁移工具：反向导出

「**把当前手摆的场景导出成 spec**」✅（`ScrollWorldAuthoringEditor` ✅ 已有同类工具 ✅ 可扩展 ✅）→
逐个对象读出：名字 ✅ / 父组 ✅ / 展开态坐标 ✅ / 旋转 ✅ / sortingOrder（换算成组基准 + sortBias ✅）/ pivot ✅ / 材质 ✅ /
sprite 的 pivot 与贴图 alpha 底边（对照 `slice_manifest.pivot_normalized` ✅ 校验 ✅）/ prefab 归属 ✅。

⇒ **迁移 = 导出 → 人工整理分组基准与 sortBias → 生成器重建 → 逐件比对** ✅。

### 6.2 分批推进（每批一个节点/一段 ✅，可回退 ✅）

| 批 | 范围 | 验收 |
|---|---|---|
| **S0** | 数据结构 + 导出工具 + 生成器骨架（输出到**副本场景** ✅） | 导出结果与现状**逐件一致** ✅ |
| **S1** | **E0 段**（城门楼 ✅ + Approach ✅ + 路肩 ✅ + 栅栏 ✅）—— 含排序规则修正 ✅ | 视觉与现状一致 ✅（差异只在城门楼层级 ✅）+ 自检 6/7/8 通过 ✅ |
| **S2** | N1 段 + 中央路牌 ✅ | 同上 ✅ |
| **S3** | J1 段（石路分支 ✅ 5 张地面贴图 ✅）+ 两侧植被 ✅ | 同上 ✅ |
| **S4** | EV 段 + **N3 立体围墙**（照 1.4 的配方 ✅） | 同上 ✅ |
| **S5** | 节点 prefab 化 + `YNodeSceneLoader` ✅ + 段表写回 ✅ | 加载行为与现状一致 ✅（`YRouteSegments` ✅） |
| **S6** | 交互物（箱子 ✅）+ 开发者「跳过本节点战斗」小按钮 ✅ | 按钮在手机构建里可用 ✅ |

---

## 七、加载与包体（诚实结论 ✅）

- **prefab 化不减包体** ✅（prefab 仍会进包 ✅）→ S5 的收益是**内存与首屏** ✅ + 结构 ✅。
- 包体的真正杠杆是**图集化**与**压缩** ✅ —— 按用户要求**本次不做** ✅，但 §1 的数据模型已为图集化留好接口 ✅
  （`pivotMode` ✅ + `slice_manifest` 字段一致 ✅ → 以后可直接由清单生成 spec ✅）。
- 常驻策略沿用现有机制 ✅：`YRouteStreamingWindow`（渲染窗口 ✅）+ `YRouteSegments`（段按需 ✅）+ 新增节点按需 ✅。
- **Y 场景仍需在 build settings 里** ✅（它是路线表现 ✅）—— 用户「重构跑通就不需要」的假设不成立 ✅：
  重构后它**仍要进包** ✅，只是变得**可重建 ✅、可校验 ✅**。**这点要提前说清** ✅。

---

## 八、开发者「跳过本节点战斗」小按钮（用户要的方向 ✅）

- 位置：落地页/关卡内的**屏幕角落小按钮** ✅（仅开发构建显示 ✅）→ 方便打到手机上测 ✅。
- 通路（现成 ✅）：`FakeRouteRuntime.MarkBattleEntryCompleted`（改 internal ✅）→
  `NotifyBattleCompleted()` ✅ → `StageController.CompleteCurrentCombatNode()` ✅（奖励门控 ✅）。
- 行为：标记当前节点所有战斗条目已完成 ✅ → 直接进奖励/下一节点 ✅ → **不碰战斗逻辑 ✅**。
- 附带收益：可作为**自动化冒烟**入口 ✅（自检脚本可用它跑全路线 ✅）。

---

## 九、风险与回退

| 风险 | 处置 |
|---|---|
| 生成器覆盖手工微调 | `userModified` 保护 ✅ + 「强制重生成」显式开关 ✅ |
| 场景数据丢失 | 已备份 ✅：git tag `backup/pre-y-scene-rebuild-20261023` ✅ + 场景/Authoring 副本 ✅ |
| 一次改动太大看不清 | 分节点推进 ✅（S1~S4 ✅）每批可单独回退 ✅ |
| 排序规则改完与现状不一致 | S1 只改 E0 ✅，其它段保持现状 ✅ → 差异可逐件解释 ✅ |
| 节点 prefab 化影响加载 | S5 单独做 ✅，且沿用 `YRouteSegments` 的节奏 ✅ |

---

## 十、明确不做

- 不碰贴图压缩/图集 ✅（另议 ✅）· 不生成美术 ✅ · 不部署战斗内容 ✅（敌人/波次/遭遇点 ✅）·
  不改 `YScrollSample.Evaluate/Length/JunctionDistance/turnAngle` ✅ · 不动战斗相机与战斗坐标 ✅ ·
  不删 Legacy ✅ · 不改 `FakeRouteNodeConfig` 的玩法语义 ✅（节点奖励是**另一件事** ✅，见 `design/gameplay-pillars.md` §5 ✅）。

---

## 十一、验收口径（每批都要过 ✅）

1. 生成器跑完无错误 ✅（自检 12 条 ✅）。
2. **逐件比对**：与迁移前导出的条目清单**逐条对齐** ✅（位置/旋转/分组/材质/pivot ✅）。
3. **画面比对**：同一相机位、同一米数，导出前后各截一张图 ✅（差异要能逐条解释 ✅）。
4. Play Mode：路线推进 ✅、节点到达 ✅、战斗进入 ✅、按需装载 ✅（段 + 节点 ✅）不卡顿 ✅；Console 0 error / 0 warn ✅。
5. 回归前备份 / 回归后还原玩家存档 ✅。

---

## 十二、相关文档

- `plan/scroll-on-demand-loading-plan.md` ✅（加载模型与不变量 ✅）
- `plan/j1-scene-deployment-handoff-20261011.md` ✅（人工部署流程与导入基线 ✅）
- `plan/architecture-optimization-handoff-20261020.md` ✅（分层与纪律 ✅）
- `design/anti-ghost-reference.md` ✅（数据可追踪 ✅）
- `memory/unity-project-understanding/asmdef-layering-pitfalls.md` ✅（Unity 工具链的坑 ✅）
