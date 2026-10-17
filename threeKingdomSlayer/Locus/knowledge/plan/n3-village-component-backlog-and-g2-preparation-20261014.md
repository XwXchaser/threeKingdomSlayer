---
id: kd_2459c777-e5d4-4d56-abf2-a6bf61c0d662
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# N3 村庄后续组件清单与G2准备（2026-10-14）

## 用户最新方向

**2026-10-15更新：用户已授权EV不出参考图，按三国时期村庄村口理解直接布景；首版EV外围组已用已验收素材完成、保存重载并获用户Game验收。交接：`plan/ev-village-outskirts-deployment-handoff-20261015.md`（含新对话接续清单）。首版无必须新生成素材；炊烟、陶罐/水缸、竹筐/粮袋/谷束属于可选后续，未生成、未准备请求。**

用户已明确验收G1/G2美术并要求继续组件。G1/G2现已按真实Alpha拆分并接入N3正式Y层，N3首轮布景已保存；用户随后已在Game中验收首轮效果。美术/首轮构图验收不等于完整自然流程验收。

G1原图与验收记录：`plan/n3-village-assets-g1-preparation-20261014.md`；原图未修改。切片与N3部署交接：`plan/n3-village-deployment-handoff-20261014.md`。

准备阶段没有远程生图、额度查询、Play/Unity写入。用户随后明确授权G2并完成一次HTTP200生成；G2美术已获用户验收。之后本轮完成切片、灰黑沿路墙原型和N3正式Y层部署；生成前*.prepared.json与preflight_verification.json保留历史。

## 当前缺口与分批

| 批次 | 组件 | 职责 | 状态 |
|---|---|---|---|
| G1 | H1紧凑茅屋/H2长屋/H3受损屋/W1井 | 建筑与生活设施主体 | 美术已验收，已按Alpha拆分并部署N3 |
| G2 | B1完整矮院墙/B2破损矮院墙/B3朴素院门/B4柴垛 | 村舍与路侧过渡、院落边界、生活生产 | 美术已验收，已拆分部署N3；灰黑墙为结构验证材质 |
| G3 | 粮袋组/陶罐组/竹筐组/成捆谷束 | 储粮、取水、收获等乡村生活 | 可选后续；EV首版未依赖，未写完整提示词/请求 |
| G4 | 两种淡烟；焦木残件/遗落布包候选 | 战乱气氛与搜救痕迹 | 可选后续；EV首版未依赖，待用户确定范围 |

按当前设计N3=村落边缘空地，左右或中远景放茅屋/木篱/残墙/淡烟，中央连续开阔。不追加军事塔楼、营帐、人物剧情动画来替代村庄组件；远景H1/H2可缩小疏密组合，暂不另生整片屋排。石板地面已部署，不再生成道路。

N3首轮布景及石墙Game效果已验收。EV外围组已于2026-10-15使用已有验收素材完成部署并获用户Game验收。G3生活小件与G4淡烟/战损未生成，属于可选补充；完整自然流程仍待确认。用户村庄图只提供景物/材质，曲率与要求不同；不把它的道路或G1单件通过当作完整构图批准。

## 可复用素材核对

实际list/read核对了下列文件；复用只是候选范围，未做Unity导入/摆放：

- J1完整/破损低篱：`Assets/Experiments/CurvedScroll/Art/EnvironmentV2/J1/J1_A03_IntactLowFence_v1.png`、`J1_A04_BrokenLowFence_v1.png`。用户已验收J1接入，可在村路两侧复用，不再生同类。
- J1树草：`Assets/Experiments/CurvedScroll/Art/EnvironmentV2/J1/Vegetation/`；树林/高草延续，保护已验收J1布局。
- 完整木车`Assets/Experiments/CurvedScroll/Art/EnvironmentV2/J1/J1_A05_CompleteWoodenCart_v1.png`与N1破车`Assets/Experiments/CurvedScroll/Art/EnvironmentV2/Props/N1/N1_BrokenCart_v1_grounded_v2.png`：有限用于村路生活运输/战乱，不重复收费生成。
- `Assets/Experiments/CurvedScroll/Art/ValleyProps.png`含灌木/碎石等技术候选，旧画风不反向定义新组件；以项目新画风与已验收G1为准。
- 已read E0候选路径的已验收B01：`Assets/Experiments/CurvedScroll/Art/EnvironmentV2/E0/Candidates/e0_b01_low_wall_right_v1.png`，形体是残城垣式土石墙，不能直接代替村庄粗石矮院墙。
- 已read旧`Props/Crates_Corrected.png`和`Props/Brazier_v1.png`：箱组/火盆不能代替村庄粮袋/陶罐/竹筐或淡烟。`Assets/Sprites/EffectSprites/burn/`只有战斗燃烧帧，不等于中远景环境烟。
- 文件名检查Assets内smoke/grain/sack/basket/pottery/village/firewood/straw未找到匹配PNG/PSD/JPG/TGA/Prefab。这不证明所有图内不存在相似物件，仅与已见环境目录合并支持缺口判断。

早期交接将J1 v3说成含村庄建筑，与其权威生成记录相矛盾；继续沿用本次用户图和已验收G1，不使用那条错误来源。大量用户Git改动保留，不操作FakeRouteDataTrial。

## G2准备包

Windows目录：

`C:\Users\Administrator\Pictures\gptGen\n3_village_assets_g2_v1_preparation\`

核心文件：

- `PREPARATION.md`：组件清单、参考职责、参数、验收与授权边界。
- `n3_village_boundaries_g2_v1.prompt.zh.txt`：完整中文正向/负面词。
- `n3_village_boundaries_g2_v1.prompt.en.txt`：拟提交完整英文正向/负面词。
- `n3_village_boundaries_g2_v1.request.prepared.json`、`.status.json`。
- `atlas_layout.prepared.json`、`reference_manifest.prepared.json`。
- `references/`：四张拟上传图片。
- `review/reference_contact_sheet.png`（1796×500）、`review/atlas_layout_review.svg`、`review/preflight_verification.json`。
- `sources/`：原用户图与已验收G1原图字节一致副本，只存档，不上传。

准备时拟输出`n3_village_boundaries_g2_v1.png`不存在；用户批准后已生成并保存。实际`.request.json`/`.response.json`/`.status.json`与`GENERATION_DELIVERY.md`已补入目录，未覆盖准备历史。提交脚本`Library/Locus/tmp/n3-reference-prep/submit_village_g2_once.py`仅发送一次，准备脚本本身仍不执行网络。

四输入顺序/职责：

1. 用户图右石院墙局部(765,615)-(887,747)，4×Nearest，488×528 RGB；石块、层叠与墙厚，补全被裁/遮挡部分。SHA256`06fcdbe13b8eb340f2447ce3a7139992c0e4a67a9da91d2a02735a756c653d5b`。
2. 用户图左木篱局部(0,632)-(107,780)，4×Nearest，428×592 RGB；仅粗木、绳扎连接，不是院门形体。SHA256`45d7a2c20abab5a019037cb0f3c919f088ec1a04a96a746ecb7480093cfcb025`。
3. G1已验收木门/墙/柱局部(405,586)-(740,875)，2×Nearest，670×578 RGB，中性底合成只供参考；仅校准材料/光照，不复制房屋剪影。SHA256`f8dfa6aa368f45584269c836a4f1563a539c36070b3f7d7e0b5008ba54558869`。
4. 概念图14原样复制1024×1536 RGB，只校准高清像素色簇/体积/冷暖。SHA256`643d50375a9fcdd2210aa40f43b5a70fb05099f01664de9ae54dfb0423016210`。

B2缺口、B3院门、B4柴垛按文字新设计，不虚称有原图形体。参考对照与G1裁图已read附图、原图哈希未改变。

## 参数与验证

拟POST `/v1/images/edits`，4个image[]，`gpt-image-2.5-sunburst / 2048x2048 / quality=max / input_fidelity=high / background=transparent / output_format=png / n=1`。批准后一次POST、0自动重试，不自动改参数或收费去背。

四格1024²，80px外圈和160px中间隔离带。安全区同G1；组件尺寸为低墙/院门/柴垛各自目标，不能为了填格拉高。有限完整墙段而非无缝纹理；院门无顶横额/屋顶；柴垛无棚架、车或底卡。物理高度目标：墙=屋总高30%–40%、院门50%–60%、柴垛25%–33%，非最终世界值。

本地`Library/Locus/tmp/n3-reference-prep/prepare_village_g2_bundle.py`已执行，无网络。四输入完整PNG解码、来源副本hash一致、双语文件hash匹配、prepared.fields.prompt与英文逐字一致、布局安全区有效；本地multipart样例已解析含透明/PNG字段与4个image[]顺序/哈希。**这不是实际提交证据**，批准执行时须重新检查真正发送的multipart。

英文SHA256`1cf9ad16be85277da265d5bb06a301b4652e9954a5bc342dbc3b6f54c21569ca`，中文`52d99b50e653e6060378b64408451e980b1003ee5b94cbb7edff5bbb63a4722b`。

生成后先查轮廓/用途与完整分离，再查Alpha与三底；G1经验决定不机械四格切片。烟的有意半透明要另设标准，不用实体近不透明阈值去清烟。

## 下一步与授权

用户已批准G2并完成一次生成，随后验收G2美术。2026-10-15用户进一步授权EV无需参考图、按三国村口理解布景；EV首版已复用现有素材，不需新图。此授权仅涵盖EV首版布景，不等于批准G3/G4生图；如后续确需新素材，先报具体清单/职责/数量并取得生成批准。

## G2生成结果（2026-10-14）

- 实际multipart重新解析验证了transparent/PNG/max、完整英文词hash与4个image[]顺序/hash；`review/submission_preflight.json`保留真实提交前检查，与本地准备样例分开。
- 一次POST、HTTP200、返回1图，API97.443s、下载完成总计113.671s，无重试。未查询余额/实际扣费。
- 原图`C:/Users/Administrator/Pictures/gptGen/n3_village_assets_g2_v1_preparation/n3_village_boundaries_g2_v1.png`为2048² RGBA/8-bit，4448835 bytes；SHA256`1ebff1bd97049697b8bcf36999c9f8aa6eb2e843e797387960ef205b7bd9c337`。
- `validate_village_g2.py`复用PNG解码/三底工具并检查G2专用轮廓；全解码/CRC通过、Alpha0–254、四角0、2655702个全透明像素、1238812个Alpha240–254近不透明像素。背景低Alpha残留保留，原图hash未变。
- 原图、白/深/土底并排与深底已read附图观察：两墙/开启院门/劈柴垛均可辨，端和脚点未见裁到画布，未见明显背景块/绘入棋盘格/宽大彩色光晕；用户随后已验收造型，但沿道路纵深的墙体表达未验证。
- Alpha>64有5个主要连通分量，院门左柱与右柱/门扇分离属于开启结构，不误判为第5件资产。四象限核心轮廓内包含各自组件，但80px边距/160px隔离带未严格遵守。Alpha>16辅助空带：竖x[990,1070)、横y[775,1068)，仍可能有低Alpha残留。
- 比例偏差：B1宽高约2.53、B2约2.49，较B1约3–3.8目标更短厚，已提示用户重点确认，未拉伸修正。图内相对尺寸不作为世界尺度。
- Alpha>16核心范围(右下exclusive)：B1(56,399)-(990,768)、B2(1070,403)-(1995,775)、B3整体(116,1068)-(952,1893)、B4(1087,1374)-(1999,1836)，仅检查范围非切片框。
- 三底单图均2048² RGB，并排`review/n3_village_boundaries_g2_v1_three_backgrounds.png`为2336×768 RGB；`GENERATION_DELIVERY.md`记录当前交付。用户验收后status=`generated_user_accepted_not_deployed`。
- 仅完成G2生成交图与美术验收，未切片、未导入Unity、未布景或提交额外收费请求。

## 道路平行围墙表达：只读分析

用户指出现有B1/B2主要是正面横墙形象。应将“沿路侧墙/纵深连续墙”列为独立待验证项，G2验收不覆盖这一用途。

已读`Assets/Experiments/CurvedScroll/Shaders/YScrollSceneryE0Route.shader`：普通景物按锚点重投影，顶点`anchor.xy += v.xy * scale`使卡片始终横向；单改Transform Y旋转不会得到沿道路纵深延伸的墙体。

已读`Assets/Experiments/CurvedScroll/Shaders/YOrientedSceneryE0Route.shader`：定向材质读取作者X轴并改变顶点横轴方向，有现成沿路栅栏实现。用户当前Play Mode只读现场看到`J1_Left_Fence_01`等实际接`J1_Fence_Oriented.mat`，作者X=(-0.26,0,0.97)、旋转Y255。未修改物件/材质/进度或切换Play Mode。既有篱笆证明存在定向渲染路径，不证明G2墙已适用。

建议先区分用途：B1/B2保留房前横向院墙；沿道路连续墙先验证B1/B2短段定向投影的近中远表现、顶部与接缝。新图并非绕过朝向设置的必需条件，但当前平面Sprite无法凭Shader补出实体厚度/顶面/转角，重复有限端头也可能暴露。若近景要求石墙体积，补墙面/窄顶面/端头（必要时转角）的独立模块，或明确选择只服务固定停点的左右侧向Sprite。避免把消失点与长透视整条烘进唯一墙片后再转向造成重复透视。

该建议仅记录分析结果，尚未对G2转向试摆/生成新图，未作最终方向表现验收；新增墙模块范围与生成提示词需后续任务确定。

用户随后要求结合卷轴场景构造深入思考生成方式，详见`plan/n3-road-parallel-wall-art-analysis-20261014.md`。只读代码/现场/数值分析确认：地面逐顶点投影，当前定向卡片朝向依赖观察方向且整片按锚点下沉，不宜当可连续接段的实体墙。推荐短简易立体墙段+正交长墙面A/B、窄顶面、短端面；现有G2保留横墙/零散残墙。新模块材质/网格尚未实现，先用本地占位或G2局部做后续技术验证再正式准备付费素材；本轮仍只有分析，无运行进度/场景/源码修改或新生图。
