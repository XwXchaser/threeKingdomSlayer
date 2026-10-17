---
id: kd_bd8538b4-0bfd-4dd0-a081-5511e34c55fb
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# N3 石墙面图接入交接（2026-10-15）

## 结果与授权

用户先批准四面组图生成，一次请求 HTTP200；随后明确“已验收，请核对透明底部并接入到对应场景位置”。本轮已把验收图的派生色图接入正式 Y 场景 N3 的4条墙链、12个4m墙段，并保存、关闭/重开核对引用。原始生成图未改，灰色原型材质保留。

正式场景：`Assets/Scenes/Battle.scene` + Additive `Assets/Experiments/CurvedScroll/YJunctionSample.unity`。

墙组：`Assets/Experiments/CurvedScroll/YJunctionSample.unity/Y Junction - select for preview/N3 Village Rescue - Accepted Art and Grey Wall Study @ 266/Road Parallel Walls - Grey Black Outline`。历史“Grey”层级名保留以免破坏识别/布局；当前12段已经使用石材。

## 美术来源和透明底部

原始文件：

`C:/Users/Administrator/Pictures/gptGen/n3_village_wall_surfaces_g1_v1_preparation/n3_wall_surface_g1_v1.png`

原始 SHA256：`f845120da8b1e061d8626fe5992729b6cb88dfafdb3db8922ac78864600651c9`。

- 原图2048² RGBA、Alpha0–254，4个主体面区；底部68行Alpha最大1，其中155像素Alpha1，其余139109像素Alpha0，无实际石材内容。未修改原图或覆盖生成记录。
- 模型没有严格按规划坐标排版，不能使用原格位直接采样。正式Atlas从已审阅的内部安全区域提取：Side A `(256,305)-(1456,869)`、B `(256,1064)-(1456,1628)`、Top `(256,1777)-(1456,1942)`、End `(1780,305)-(1945,869)`，右/下exclusive。
- 各源裁图共同300px/m且Alpha≥250；统一4/3等比例最近邻缩放到400px/m，RGB保留，仅把近不透明Alpha设为255。未按方格非等比拉伸石块。
- 正式Atlas：Side A/B1600×752、Top1600×220、End220×752；四面各有8px挤边。面区和底行全部Alpha255；面区与padding之外Alpha0，最底60行Alpha0。不会把原图底部透明当墙面的一部分。

## 项目资产

素材目录（可粘贴资源管理器）：

`H:\Project\threeKingdomSlayer\threeKingdomSlayer\Assets\Experiments\CurvedScroll\Art\EnvironmentV2\N3\Village\`

- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/N3/Village/N3_WallSurface_G1_Source.png`：原始生成图的字节一致复制，用于追溯，不用于墙材质采样。
- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/N3/Village/N3_WallSurface_G1_Atlas.png`：生产派生Atlas，SHA256 `7ab79b02103623dcaf88f2f460529118e3d439e99e1d4c444deabc32f6bc3393`。
- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/N3/Village/N3_WallSurface_G1_Atlas.manifest.json`：作者来源/裁图/区域契约，不在运行时加载。
- `Assets/Experiments/CurvedScroll/Authoring/N3Village/N3_StoneWall_Outline.mat`：新石材材质，Tint白色、描边色0.025、宽0.045。
- `Assets/Experiments/CurvedScroll/Shaders/YVillageWallPrototype.shader`：保持原路径/GUID，新增Inspector显式Atlas和各面区域采样；原灰材质仍以默认白贴图得到原灰色结果。

贴图Importer：Default、sRGB、RGBA FromInput、Bilinear、Clamp、Uncompressed、mipmap off、2048、NPOT None；Alpha Is Transparency=false，避免Importer对色图RGB作透明挤色。Atlas已显式挤边且采样半像素内缩。

## Shader/几何边界

- 长侧面用现有uv2 `(4,1.88)`识别，Mesh本地x≤0采Side A、x>0采Side B；两侧的序列化法线均为−X，因此不能用法线正负选面。
- 顶面uv2 `(4,.55)`采Top；端面`(.55,1.88)`采End，开口Mesh无端帽则不会多画端面。
- 保留单submesh、两侧/顶/端顶点色、所有Mesh与Transform。原uv0只在采样时映射到Atlas，描边仍用原uv0和真实面尺寸。
- 保留原路线逐顶点投影、下弯、距离裁剪；仍Opaque、ZWrite On、Cull Off，不对实体墙使用Alpha Blend。
- 无Resources.Load、字符串运行查找或静态贴图缓存；Renderer→Material→Atlas/Shader在Inspector可追踪。

## 验证结果

证据目录：

`H:\Project\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\n3-wall-surface-integration-20261015\`

- `scene-before.json` / `scene-after.json` / `scene-reloaded.json`：1304个Transform精确序列化一致；Renderer数量1194不变，只12个N3墙材质引用不同，Mesh引用不变。
- `static-verification.json`：四面/底行Alpha255、底部60行Alpha0、区外Alpha0，原始哈希未变。
- `unity-integration.json`：保存重载成功、12材质引用持久化、scenery636/null0/duplicate0、progress0/rightfalse、根Y0；Shader supported、编译消息0。
- `protected-before-preview.json`：13个被保护文件在编辑态预览前后哈希一致，包括Battle/MainMenu/FakeRoute、路线与Host代码、共享地面/景物Shader、灰墙材质和4个墙Mesh。该哈希基线采自赋新材质后，用于证明后续预览未改动保护对象；本轮整体布局证明来自赋材质前的场景快照。
- 编辑态正式Battle Camera渲染：512×1024，Main Camera `(0,3,-10)`、pitch18°、FOV60；只在预览时使用Host groundHeight−1.8，结束恢复。临时隐藏Battle已有5个战斗Renderer防止武器特效混入，全部恢复，未保存Battle。
- 干净对照：`previews/left_255.0_stone_clean.png`、`previews/left_266.1_stone_clean.png` 与相同距离`_grey_clean.png`。已回读两处石材图：两侧灰墙换为石墙，黑边、厚度、脚点和中央净空保留。
- Unity Console warn/error0。用户已在Game中确认新石墙效果并完成本轮验收；未启动额外的完整Play流程、真实战斗、奖励或退出回归。未声称石块逐块跨段无缝对齐。

## 最终现场

石材接入收尾时曾恢复为Edit Mode、仅加载 `Assets/Scenes/MainMenu.scene` 且dirty=false；随后用户自行进入Battle Play Mode完成Game验收。当前会话状态以最新Unity Editor Status为准：Play Mode，Active Scene=`Assets/Scenes/Battle.scene`。未把progress/运行时根高度/Camera临时引用保存，未操作FakeRouteDataTrial场景，未修改Battle、路线、Camera、房屋/树草/井门柴垛布局。

G3/G4、EV独立村落入口、完整自然路线/真实战斗回归依然未完成，不属于此次面材质替换。
