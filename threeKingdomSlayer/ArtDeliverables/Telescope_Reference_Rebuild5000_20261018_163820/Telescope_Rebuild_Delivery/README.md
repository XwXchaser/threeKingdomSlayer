# 望远镜 · 全新重搭交付

## 制作依据与范围

- 唯一造型参考：用户提供的 `screenshot-20261018-150725.png`。
- 从零搭建网格，没有查看或复用项目原有生成模型。
- 按用户后续要求，以 **5000 面以内**替代最初的 1000 面限制。
- 原图为黑白线稿，配色为补充设计：黑灰金属、银色接缝、深蓝镀膜镜片。
- 本次仅交付模型与视图，不导入或改动现有 Unity 场景、Prefab、脚本。

## 模型

最终模型为 **4804 个三角面、2488 个网格顶点、43 个独立可编辑网格零件**。已显式三角化，无细分或增面修改器。GLB 与 FBX 回读均为 4804 三角面；各零件非流形边和退化面检查均为 0。不同材料或法线边界可能导致导入后的渲染顶点数增加，但不改变三角面数量。

包含主镜筒、前遮光罩与凹入式物镜、后端目镜、顶部寻星镜及双支架、调焦座与左右调焦轮、俯仰支架、转轴螺栓、云台、三条伸缩支腿、锁紧件、橡胶脚垫和折叠撑杆。

- `model/Telescope_Rebuild_Under5000.blend`：Blender 5.2.1 原生文件；独立新场景，包含材质、灯光与五台相机。
- `model/Telescope_Rebuild_Under5000.fbx`：只包含新模型及材质，不含渲染相机与灯光。
- `model/Telescope_Rebuild_Under5000.glb`：自包含 PBR 材质模型，不依赖外部纹理。
- 材质通过基础色、金属度、粗糙度及镀膜效果呈现，不含烘焙纹理图集。各零件提供基础 UV，但未制作无重叠的统一贴图 UV。
- 模型为静态装配，无骨骼或机构动画；网格零件之间可有装配交叠，不是单体焊接打印模型。
- Blender 文件坐标：Z 向上，主物镜方向为 -Y，目镜方向为 +Y。FBX/GLB 已执行相应坐标导出。

## 拆分视图

全部为 **2048×2048、透明背景 RGBA PNG**，同一模型、相同正交比例、相同灯光，四个方向分别渲染，不是将一张图镜像或裁切。

| 方向 | 文件 |
|---|---|
| 前：从主物镜端观察 | `views/01_Front_Objective.png` |
| 后：从目镜端观察 | `views/02_Rear_Eyepiece.png` |
| 左：从 Blender -X 观察 | `views/03_Left.png` |
| 右：从 Blender +X 观察 | `views/04_Right.png` |
| 附加立体预览 | `views/05_ThreeQuarter.png` |

`Telescope_FourViews_Overview.png` 是带方向标签的浅底总览；`Telescope_ThreeQuarter_Preview.png` 是浅底立体预览。拆分 PNG 本身没有标签或背景。

## 验证记录

- `model/mesh_audit.json`：每个零件的网格与拓扑统计。
- `model/export_verification.json`：GLB、FBX 回读面数与相机比例、画面边距。
- `model/image_verification.json`：独立视图、图像尺寸、Alpha 边界和 SHA-256。

`Telescope_Rebuild_Delivery.zip` 包含以上模型、拆分视图、预览与验证文件。
