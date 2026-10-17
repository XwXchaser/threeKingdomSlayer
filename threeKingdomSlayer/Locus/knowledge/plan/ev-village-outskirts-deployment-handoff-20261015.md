---
id: kd_1232891a-f5e8-48f2-803a-2e02e610a3a5
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# EV 村落外围布景交接（2026-10-15）

## 授权与结果

用户已验收 N3 石墙 Game 效果，随后授权：EV 不需要参考图，按三国时期村庄村口理解直接布景；如需要新的美术素材，先告知清单。本轮已使用现有验收素材完成 EV225 村口布景，未制作新参考图、未生成新美术、未调用远程生成接口。

**用户随后在 Game 中明确回复“已验收”，EV 首版布景已完成验收闭环。** 本轮只做编辑态渲染验证，没有进入 Play 或运行真实战斗/奖励流程；验收来自用户自行开游戏的主观判断，不等于完整自然流程已回归。

## 与新对话的接续

本批（N1→E0→J1→EV→N3 布景）到 EV 为止均已验收：J1、N3 灰黑墙、N3 石材墙面、EV 村口。新对话继续场景任务时的候选方向：

1. **N2（右官道）背景组**：`N2 - Presentation Anchor` 的 `backgroundAnchor` 至今为空，右支266米只有官道本体。需要另一份构图方案（+45° 分支，横向左右语义与 EV 相反），当前未做只读分析。
2. **EV/N3 可选生活补充件（G3/G4）**：陶罐/水缸、竹筐/粮袋/谷束、远景淡炊烟、焦木/搜救痕迹。首版两组都不依赖它们；若要做，先出清单与完整中英文提示词再取得生成批准。
3. **完整自然流程与真实战斗回归**：N1→E0→J1→(选支)→EV→N3 的自然推进、奖励阻塞、失败与退出尚未回归；真实战斗手感也未验收。
4. **EV 密度微调**：首版为稀疏村口，若 Game 中觉得偏空，可在不侵入中央净带（`|xn|≥0.72`）的前提下加密左右农户小院。

## 场景与新增层级

正式结构不变：`Assets/Scenes/Battle.scene` + Additive `Assets/Experiments/CurvedScroll/YJunctionSample.unity`。

新增组：

`Assets/Experiments/CurvedScroll/YJunctionSample.unity/Y Junction - select for preview/Route Nodes - N1 E0 J1 and Branches/EV - Presentation Anchor/EV Village Outskirts - Rural Entrance @ 225`

EV `ScrollRouteNodePresentation.backgroundAnchor` 已绑定到该组。组为 EV 锚点的局部原点，继承世界位置 `(-17.3577766,0,219.021851)`、Y315°；节点距离仍为225。未新增逻辑节点、未改路线长度或推进时序。

四个子组共45个实际Renderer、53个Transform：

| 子组 | 内容 | Renderer |
|---|---|---:|
| Rural Houses - Left Homesteads and Distant Roofs | 4处完整茅屋、2处较小远屋，复用H1/H2 | 6 |
| Village Entrance - Open Wicket Cart and Firewood | 开启柴门、木车、2柴垛、2院落横墙 | 6 |
| Short Courtyard Walls - Existing Stone Material | 3条短墙链，每链2段，复用验收石墙Mesh/材质 | 6 |
| Roadside Trees Grass and Stones | 4棵树、5处碎石、18株复用风动草 | 27 |

构图为左侧农户小院与柴门、路缘木车，右侧退后村舍和柳树呼应；中央石路开阔，不横跨道路设置城门或牌楼，不用营帐/军事塔楼替代村口。

## 复用和接线

- 屋舍/院落：`Assets/Experiments/CurvedScroll/Art/EnvironmentV2/N3/Village/` 的已验收Sprite；保留Importer/Pivot，仅补偿新实例的局部横向位置。
- 木车：`Assets/Experiments/CurvedScroll/Art/EnvironmentV2/J1/J1_A05_CompleteWoodenCart_v1.png`。
- 树：`Assets/Experiments/CurvedScroll/Art/EnvironmentV2/J1/Vegetation/`。
- 碎石与风动草：复用N3已有组件配置，Mesh、材质及风动参数不变。
- 石墙：复用 `Assets/Experiments/CurvedScroll/Authoring/N3Village/` 中的Mesh和 `N3_StoneWall_Outline.mat`，没有改共享材质、Shader或Mesh。
- 全部45个新Renderer已追加到 `YScrollSample.scenery`；636→681，null0、duplicate0。原636条引用顺序保留。

## 验证与保护结果

证据目录（仅预览/追溯，不是新美术素材目录）：

`H:\Project\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\ev-village-outskirts-20261015\`

- `baseline-scene.json` / `scene-after-reload.json`：原1304个Transform的位置/旋转/缩放/显隐精确一致；原1194个Renderer的启用、排序、材质与Mesh引用不变。新增后1357个Transform、1239个Renderer。
- `verification.json`：221个保护文件哈希未变化，涵盖Battle/MainMenu/FakeRoute、路线/Host脚本、共享材质/Shader、既有J1/N3素材与路线数据；只保存了目标Y场景。
- `reload-verification-final.json`：保存重载后EV锚点有效，45个Renderer全部在scenery，681/null0/duplicate0，progress0/rightfalse、路线根(0,0,0)。最后入口草修正也已保存并重载。
- 编辑态用正式Battle Camera渲染512×1024，采用Host groundHeight=-1.8作临时预览，结束恢复进度、左右路、Camera、根高度、Renderer显隐、RenderTexture。没有保存Battle或临时预览参数。
- 覆盖左路190、200、205、212.1、225、235、245、255、266.1；右路202、236、266.1。第二轮调整让柴门/木车在EV更可读，未动J1/N3。
- 初轮发现一株EV入口草在J1左缘露出，已把 `EV_Grass_Right_00` 从局部(5.43,-.04,-3)移到(5.2,-.04,4.2)并保存。最终J1图与EV关闭基线只差一个颜色通道1级，没有可见EV轮廓；N3与EV关闭基线逐像素一致。右路236初轮开/关EV亦逐像素一致，最终右路采样未见村落物件。
- Unity Console warn/error0。

最终代表图：

- 村口停点：`Library/Locus/tmp/ev-village-outskirts-20261015/final/left_225.0_ev_final.png`
- J1保护：`Library/Locus/tmp/ev-village-outskirts-20261015/final/j1-190-ev-group-on.png`
- 村落过渡：`Library/Locus/tmp/ev-village-outskirts-20261015/iteration2/left_245.0_ev.png`
- N3不变：`Library/Locus/tmp/ev-village-outskirts-20261015/iteration2/left_266.1_ev.png`

## 最终Editor现场

Edit Mode，仅加载 `Assets/Scenes/MainMenu.scene`，dirty=false（EV 验收后状态，由用户自行开游戏完成 Game 验收）。本轮临时打开的Battle/Y已关闭；Y新增布景已保存。MainMenu曾因预览留下dirty标记，收尾时用SaveScene(saveAsCopy=true)导出临时副本，与MainMenu磁盘文件63090字节逐字节一致后才清除脏标记；未保存或覆盖MainMenu。副本为 `Library/Locus/tmp/ev-village-outskirts-20261015/mainmenu-live-audit.unity`。

## 新素材需求与验收边界

**EV首版没有必须新生成的素材。** 如用户希望进一步丰富，可选：陶罐/水缸、竹筐/粮袋/谷束用于农户生活；淡炊烟用于远处屋顶。都不是本轮完成的前置条件，尚未准备提示词、未获新生图批准。G3/G4沿用原待办，不因本轮布景自动执行。

用户可从J1选左路，观察转弯后村舍显露、EV225停点柴门/木车与中央净空、再推进N3的衔接。EV Game观感、完整自然流程和真实战斗仍待用户确认；不把编辑态截图检查等同于Gameplay验收。
