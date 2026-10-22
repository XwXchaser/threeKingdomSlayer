---
id: kd_d652de3e-5f30-4e63-8801-f998ecb36d49
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
summary: YJunctionSample（Y 卷轴场景）的**旧残留清单**：21 个作者态关闭、运行时永不激活的物件 + 被运行时禁用的旧路线机制；含「拿场景当参考重搭时不要搬过去」的执行规则（导出默认已剔除 ✅）。
---

# Y 场景旧残留清单（2026-10-24 实测）

> 入口：`plan/scene-pipeline-handoff-20261023.md`（进度 ✅）；管线规则见 `memory/y-scene-spec-pipeline.md` ✅。
> 场景：`Assets/Experiments/CurvedScroll/YJunctionSample.unity` ✅（根对象 `Y Junction - select for preview` ✅，275 个 Transform ✅）。

## 一、场景顶层结构（8 组 ✅）

| 组 | 规模 | 性质 |
|---|---|---|
| `Terrain - one mesh, fork painted in terrain` | 1 | 地形（**给定输入** ✅，常驻 ✅） |
| `Skybox Background - editable` | 1 | 背景（常驻 ✅） |
| `N1 Far Landmarks` | 2 | 远景地标（常驻 ✅）—— **唯一的渲染器是关着的** ⚠️ |
| `Route Nodes - N1 E0 J1 and Branches` | 73 | 6 个节点锚点（N1/E0/J1/EV/N2/N3 ✅）+ 节点周边布景 ✅ |
| `Route Connections` | 49 | 4 条连接（纯逻辑 ✅，无渲染器 ✅） |
| `Opening Roadside Props` | 100 | **N1 沿路布景** ✅（不是整组旧的 ✅）—— 里面混着 14 个旧道具 ⚠️ |
| `N1 to E0 - Shoulder Detail v1` | 34 | S1 切片 ✅（规格已入库 ✅） |
| `Encounter Authoring` | 14 | 战斗锚点 ✅（N1/E0/N2/N3 ✅ + 箱子 ✅） |

## 二、运行时**永不激活**的物件（21 个 ✅）

判据：`YRouteStreamingWindow` 只切 `Renderer.enabled` ✅，**从不 `SetActive(true)`** ✅
→ 作者态 `activeSelf = false` 的物件在正式游戏里永远不显示 ✅。

| 位置 | 物件 | 数量 |
|---|---|---|
| `Opening Roadside Props` | `Fence_Left_08` / `Fence_Right_08` / `Fence_Left_16` / `Fence_Right_16` / `Banner_Left_12` / `Banner_Right_12` / `Brazier_Left_18` / `Brazier_Right_18` / `Palisade_Left_18` / `Palisade_Right_18` / `Banner_Left_21` / `Banner_Right_21` / `Brazier_Left_24` / `Brazier_Right_24` | 14 |
| `Opening Roadside Props/N1 Lamp Flags - Behind Battle` | `N1_LampFlag_Left_31` / `Left_39` / `Right_39` / `Left_48` / `Right_48` | 5 |
| `N1 Far Landmarks` | `N1 Distant City Wall Landmark`（**该组唯一的渲染器** ⚠️ → 整组不显示 ✅） | 1 |
| `Route Nodes/…/EV - Presentation Anchor` | `EV Approach - Existing Shoulder` | 1 |

## 三、旧路线机制残留（不是物件 ✅，但也别照抄 ✅）

- `YSampleBattleHost`（挂在场景根 ✅）：旧假移动路线的战斗宿主 ✅ →
  **运行时被 `ScrollRoutePresenter` 禁用** ✅（`enabled = false` ✅ + 停协程 ✅）；`battleConfig = null` ✅。
- `YScrollSample` 的旧字段：`junctionVisuals` / `leftBranchVisuals` / `rightBranchVisuals` /
  `rightBranchEncounter` / `travelMarker` / `battleHost` **全是 null** ✅，`animate = false` ✅。
- 名字含 `Display Enemies - no combat` 的物件：运行时被 `SetActive(false)` ✅（场景里已无 ✅）。

## 四、执行规则（已落进代码 ✅）

1. **拿场景当参考重搭 → 不搬旧残留** ✅：
   `YScenePlacementImporter.ExportPlacementTable` ✅ 与 `YSceneSpecTools.BuildSpecFromSlice` ✅
   **默认跳过作者态关闭的物件** ✅（`includeInactive = false` ✅）→ 日志会报「跳过 N 个不激活物件」 ✅。
2. **比对要同步跳过** ✅：往返验证用 `CompareTrees(..., ignoreInactive: true)` ✅
   → 否则会把被剔除的物件误报成「缺失」 ✅。
3. 场景数据仍以**场景本身**为准 ✅；规格资产只是派生数据 ✅ → 物件真要重新启用 ✅，
   在场景里打开它 ✅，再重新导出 ✅。

## 五、残留**资产**（不是场景物件 ✅，同样别当现役内容 ✅）

| 资产 | 状态 | 说明 |
|---|---|---|
| `Authoring/Segments/J1 Side Vegetation and Right Road Detail.prefab`（197 件） | **切分前的原件** ✅，未进段表 ✅ | `plan/scroll-on-demand-loading-plan.md` 第 141 行 ✅：单批 197 件实例化摊不平 ✅ → 已拆成 `J1 Side Vegetation A`（50 件 ✅ 174→211 ✅）+ `J1 Side Vegetation B`（147 件 ✅ 209→271 ✅）✅。**不进段表是对的** ✅，但它会误导后来人 ✅。 |
| `Authoring/Segments/J1 Integrated - Official Road and Stone Lane @ 190.prefab` | **空壳** ✅（只有 1 个 Transform ✅） | 路面是画在地形贴图里的 ✅ → 不需要进段表 ✅ |

## 六、实测口径（2026-10-24 ✅）

- 整场景规格：剔除前 **189 条目 + 85 功能件** ✅ → 剔除后 **161 + 84** ✅（差 21 ✅ + 1 容器 ✅）；
  重建逐件差异 **0** ✅。
- `Authoring/Segments/` 16 个段 prefab 共 762 件 → 逐件差异 **0** ✅。
- N1→E0 切片往返 → 逐件差异 **0** ✅。

> ⚠️ 数量会随场景改动变 ✅ —— 判定口径是「`activeSelf == false` 且运行时没人会打开它」✅，
> 不是某个固定数字 ✅。复查方法：编辑器里跑一次「不激活物件」清单 ✅。
