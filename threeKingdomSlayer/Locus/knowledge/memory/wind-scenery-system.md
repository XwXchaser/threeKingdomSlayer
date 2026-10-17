---
id: kd_4c7f3ca0-c117-489d-a840-32c8a4c12304
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
summary: 树木/灌木/旗帜顶点风动系统：YSceneryWind Shader + WindSceneryTool 工具、内容框权重三模式、flip 必须镜像 UV 与偏移而非负缩放、风动写在 _YSEnabled 分支外，含 Phase 1 小样验证数据与当前部署状态。
---

# 树木/灌木/旗帜风动（YSceneryWind）

## 组件

- Shader：`Assets/Experiments/CurvedScroll/Shaders/YSceneryWind.shader`（`CurvedScroll/Y Scenery Wind`）。立卡投影逻辑复制自 `YScrollSceneryE0Route`，风动只在顶点阶段做沿立卡右向的位移。
- 工具：`Assets/Experiments/CurvedScroll/Scripts/Editor/WindSceneryTool.cs`，菜单 `Tools/Curved Scroll/Wind/Apply Wind To Selected Scenery`、`Remove Wind From Selected Scenery`；静态入口 `WindSceneryTool.Apply(IReadOnlyList<SpriteRenderer>, Mode?)`。
- 资产：网格 `WindSceneryGrid[_MirrorX|_MirrorY|_MirrorXY].asset`（13×21 顶点，x/y 均 -0.5..0.5），材质按 sprite 各一份，全部在 `Assets/Experiments/CurvedScroll/Authoring/WindScenery/`。

## 硬约束（踩过的坑）

- 不能沿用草的 `texcoord.y` 当高度：`TreePine`/`BushWide` 在 `ValleyProps` 图集里，v 不是 0–1。
- SpriteRenderer 的 flipX 由 `UnityFlipSprite` 在 shader 里绕**物体原点**镜像，所以带偏移 pivot 的精灵翻转后位置会换边。风动网格因此必须：子节点 `localPosition = (xmin+xmax)/2 * (±1)`，网格几何保持对称，**只镜像 UV**（`WindSceneryGrid_MirrorX`）。不要用负 localScale——shader 里用的是 `length()` 取模，符号会丢。
- 风动位移必须在 `_YSEnabled` 分支之外计算：Edit Mode 与 `viewCamera` 未绑定时 `_YSEnabled=0`，写在分支里会导致作者态完全看不到风。
- 网格 y 必须居中（-0.5..0.5），因为子节点按 sprite 四边形中心定位、scale 用 sprite 真实单位尺寸。
- 不要改共享材质 `YSampleScenery_E0Route` / `YSampleScenery_v2`（石头、帐篷、建筑共用 400+ 渲染器）。
- 部署沿用草的约定：原 SpriteRenderer 保留为布局源但 `enabled=false`，子物体 `Wind Mesh`（MeshFilter+MeshRenderer）实际渲染，并注册进 `YScrollSample.scenery`。

## 权重模型（材质参数，无需重新烘焙）

`_WindMode` 0=树、1=灌木、2=旗；`_ContentRect` 由工具按 sprite alpha 自动求可见内容框：

- 树：`smoothstep(_WindStart, _WindEnd, yN) * lerp(.55,1,yN)`，树干在冠层以下权重 0。
- 灌木：`pow(yN, _WindPower)`，根部固定，整丛弯（等同草的曲线）。
- 旗：`smoothstep(_WindStart, _WindStart+_WindEnd, xN) * lerp(.85,1,yN)`，`_WindStart` 由工具探测“贯穿全高的旗杆列”自动生成，杆件权重 0。

## 已验证（2026-10-17 Phase 1 小样）

对象：`J1_Tree_Broad_Main`、`J1_Left_EdgeTree_02`、`J1_Left_EdgeBush_01`、`J1_Right_PlainFlag_01`、`N1_LampFlag_Left_24`（flipX）。

- 把 `_WindStrength` 置 0 后与原生 SpriteRenderer 逐像素对比：IoU=1.000（5/5），几何完全一致。
- 13 个风动时间点：树冠顶部横向位移约 2%–3.8% 树高，树干/下部 0；旗帜布面 100% 像素变化、旗杆列 0–5%；灌木整体变化、右侧稀疏边缘 0。
- Console error/warn=0；`scenery` 由 707 增至 712。

## 当前状态

Phase 1 小样（树/灌木/旗各一 + 镜像旗）已验收（用户“观感可以”）并随场景保存。

Phase 2 已落地并保存、重载回读通过（2026-10-17）：当前激活的 22 棵树（`J1_Tree_Broad_v2` 11 / `J1_Tree_Willow_v2` 11）、53 丛灌木（`BushCompact` 16 / `BushWide` 37）、8 面旗（`N1_SmallBrokenFlag_v1`，含 flipX）全部接入，共 83 个 `Wind Mesh`、22,659 顶点；`YScrollSample.scenery` 712→790（无空项/无重复）；原 SpriteRenderer 全部 disabled，Console error/warn=0。

验证证据：83/83 对象在 `_WindStrength=0` 时与新网格逐像素 IoU≥0.998（与原生 SpriteRenderer 几何一致）；J1 与村口两处 512×512 广角前后帧差异约 300px/262144（仅边缘抗锯齿），说明排序、尺度、位置无回归。

未做：`TreePine`(160)、`Banner_v1`(4)、`BannerLarge`(2) 目前全部 inactive，未预生成；若后续节点启用它们，选中后跑一次菜单工具即可。
