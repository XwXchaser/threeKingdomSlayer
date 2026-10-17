---
id: kd_f6a8f076-fe3b-45b7-abc8-2e3bd926a8c0
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# N3 村庄建筑与生活设施 G1：透明组图生成准备

## 当前状态

用户提供了 `C:/Users/Administrator/Pictures/gptGen/4c7ef3f7-f89c-44e0-b008-725566fcb107.png`，说明曲率与要求不同但内容接近，要求先分析三国时代村庄景物，随后明确要求“按照之前的成功经验，直接生成美术素材组图？准备生成工作”。

用户随后明确批准“已确认，发起生成”。本批已按批准内容完成一次生成，HTTP200，原图与请求/响应已保存；透明检查和三底预览已完成，用户随后明确回复“已验收”。验收仅针对G1四件美术，不代表村庄素材齐备或布景通过；用户明确要求继续制作后续场景组件，现阶段不进入布景。实际状态`approved=true / remote_requests_submitted=1 / automatic_retries=0`；生成前`*.prepared.json`保留为历史草稿，不代表当前状态。未进入Play Mode、未修改Unity场景/资产、未切片。之前对N3完整场景图的授权不是本批授权，本批使用此次明确批准。

用户图作为**景物与材质参考**；不把它视为 N3 道路几何或整体构图已验收，不上传完整场景去让模型复绘道路。N3 仍用 N1 同机位直路与中央开放战斗区。

## 生成范围与分批

首批四件，单张2048×2048透明2×2组图：

| 格位 | 编号 | 素材 | 主要约束 |
|---|---|---|---|
| 左上 | H1 | 完整紧凑土墙茅屋 | 一門一窗、完整屋檐/墙脚、近正面略侧視 |
| 右上 | H2 | 完整长屋变体 | 一門两窗、不同轮廓/门窗、不能只镜像H1 |
| 左下 | H3 | 局部受损茅屋 | 与H1同家族；保留约2/3以上屋面/主体，局部塌顶/焦黑梁，不烘烟火 |
| 右下 | W1 | 朴素乡村井 | 低井栏、双木柱、木辘轳、绳与一桶，无亭顶；物理约半间房高 |

首批覆盖民居主体、战损和生活设施。范围准备过程中曾考虑把土院墙放第四格，最终定为井；院墙转后续批次。篱笆、木车、树草和碎石复用现有资源，不再生一遍；柴垛/陶罐/竹筐、院墙等可在首批验收后再讨论，不批量承诺。

三国表达采用简朴土墙、茅屋和粗木材料；禁止后世徽派白墙马头墙、宫殿/寺庙化与欧式半木风。不是考古级复原，也不把所有灰瓦顶/木构系统认定为时代错误。本批只选茅屋，不加入塔楼、城门和军旗。

## 参考图与版式

正式准备目录：

`C:/Users/Administrator/Pictures/gptGen/n3_village_assets_g1_v1_preparation/`

拟上传 `references/` 内三张，顺序固定：

1. `01_village_house_left_detail.png`，735×585 RGB：原图(0,515)-(245,710)裁取后3×Nearest；只取左茅屋轮廓/材料，原屋体局部裁断/脚点遮挡，要完整重建。前景旗杆/篱笆/院墙、树草等不得入成品。
2. `02_village_house_right_detail.png`，441×540 RGB：原图(740,500)-(887,680)裁取后3×Nearest；只取右屋屋檐/土墙/木门/略侧视，补全裁断，忽略院墙/树/车。
3. `03_original_three_kingdoms_art_style.png`，1024×1536 RGB：原样复制项目战场概念图14，只校准体块/像素色簇/材质，不复制人物军阵城门旗帜。

原用户图887×1774 RGB、3220192 bytes、SHA256 `07b52fdcd6263db53b62b4f7c0c2982d535b01869f7909ab9858fed1f76c553c`，原样归档到 `sources/user_village_content_reference.png`，不上传。井不是图中的现成物件，W1按文字新设计，不声称有形体参考。

四格各1024²，80px外边；安全区为(80,80)-(944,944)、(1104,80)-(1968,944)、(80,1104)-(944,1968)、(1104,1104)-(1968,1968)。中间横竖160px透明隔离。房屋放不下时等比缩小，不拉伸；井不能为填格放大到房屋体量。所有裁图均只是形体参考，不是Unity可用素材。

`review/reference_contact_sheet.png`已实际read附图并检查，只有最终三张参考，宽高比保持；`review/atlas_layout_review.svg`是版式审阅，不加入image[]。候选裁图试验归档到`review/rejected_crop_trials/`，不上传。

## 完整提示词与参数

生成目标、完整中英文正向词与负面词均已写入并回读：

- `n3_village_architecture_g1_v1.prompt.zh.txt`
- `n3_village_architecture_g1_v1.prompt.en.txt`
- `PREPARATION.md`：内容、参考、参数、生成后验收、边界
- `atlas_layout.prepared.json`
- `reference_manifest.prepared.json`
- `n3_village_architecture_g1_v1.request.prepared.json`
- `n3_village_architecture_g1_v1.status.json`
- `review/preflight_verification.json`

接口`POST https://api.muskapis.com/v1/images/edits`，multipart三张`image[]`，model=`gpt-image-2.5-sunburst`、size=`2048x2048`、quality=`max`、input_fidelity=`high`、**background=`transparent`**、output_format=`png`、n=1。已按此次批准提交一次、0自动重试；实际multipart字节已解析确认透明字段和PNG字段确实存在，三张image[]顺序与哈希匹配。未静默删透明参数或转白底再发。

拟输出`C:/Users/Administrator/Pictures/gptGen/n3_village_assets_g1_v1_preparation/n3_village_architecture_g1_v1.png`，准备阶段此图不存在。

前次N3场景请求曾报额度不足，这是历史状态；本批实际返回HTTP200并得到原图。未独立查询当前额度/扣费，不能从成功生成推断剩余余额。

## 依据与本次改进

已重读`plan/j1-group-generation-prompts.md`、`plan/j1-tree-grass-atlas-g2-v2-preparation.md`、透明Skill和参考图到组装经验。

- G1车/篱笆确实通过用户验收，但固定y=512切线未被完全遵守；实际透明带允许完整拆分。
- 树草最终RGBA去背版本已按最新交接部署，格位也并非严格服从。四件组图不等于四格切片必然可用。
- 上次四件树草请求的`background`实际遗漏，是脚本执行偏差；新草稿`fields`内显式含transparent与PNG。后续提交前必须校验真实multipart而非只信顶层记录。
- 回包实际PNG决定透明性，不能用CDN域名、是否回显background或token数代替Alpha验收。
- 若四件偏离格位但轮廓完整独立，可以按实际Alpha空隙拆分；跨格不等于必然粘连。若连在一起/共享底座/轮廓断裂则停，不强切。
- 如果只有透明失败而造型获认可，可按新授权做单图仅去背；这不是本次准备自动包含的第二个收费请求。

## 准备阶段验证

本地JSON均可解析；三张输入PNG完整解码；源图/概念图副本hash与原图一致；中英文词已回读，hash与请求草稿匹配；安全区在格内且不侵入透明隔离带；approved=false、请求数0、无生成输出。

由`Library/Locus/tmp/n3-reference-prep/prepare_village_atlas_bundle.py`完成本地准备，无网络。这个脚本不提交图像请求；预览文件不等于已生成素材。

验收以完整房屋/井形体、实际RGBA/Alpha、白/深/土底边缘与缩小可读性为先。颜色统计不能证明时代形制、轮廓完整或可切片。原图与三底预览已read附图，G1四件已获用户验收；不自动切片、不导入Unity。

## 2026-10-14 生成结果与交付

- 实际执行`Library/Locus/tmp/n3-reference-prep/submit_village_atlas_once.py`：一次POST、HTTP200、n=1返回1图，API约156.3s、下载完成总计163.5s。实际请求/原始响应/状态分别保存为`.request.json`、`.response.json`、`.status.json`，未覆盖生成前草稿。
- 原图`C:/Users/Administrator/Pictures/gptGen/n3_village_assets_g1_v1_preparation/n3_village_architecture_g1_v1.png`：2048×2048、8-bit RGBA、5740672 bytes；SHA256`2218ff9b40f75c32d1144f1d697149829ba70a13cf8c0297e40c053b45e9859a`。
- `validate_village_atlas.py`完成CRC/全解码/Alpha检查和本地三底合成预览；原图hash未变。Alpha0–254，四角0；2328949个完全透明像素，1715998个Alpha240–254近不透明像素。背景局部有Alpha1/2极低值残留，未阈值清理。
- 观察原图与白/深/土底：四件主体可辨，未见明显整块背景、绘入棋盘格或宽大彩色光晕；房屋门窗/墙脚完整可读，受损屋有塌顶与梁架，井有双柱/辘轳/绳/单桶。屋脊翘木较显眼，时代感/最终画风随后获用户验收，但未宣称严格历史复原。
- **格位偏差**：竖向160px隔离带、左右80px外圈未遵守；H3跨过x1024中线，不能固定1024²四格切。Alpha>16和>64均有四个主要独立连通主体，主要轮廓未触画布边，另有1个Alpha>64单像素小分量。实际轮廓范围(Alpha>64，右下exclusive)：H1(38,216)-(953,884)、H2(960,236)-(2032,888)、H3(55,1140)-(1113,1889)、W1(1303,1301)-(1850,1885)。这些是检查范围，不是切片框。
- **比例偏差**：井的图内高度约为紧凑完整屋0.87倍，偏离约半间房高的目标；若采纳应在后续授权的切片/部署时单独缩小。
- 本地交付`GENERATION_DELIVERY.md`记录原图、预览和偏差；`review/generation_verification.json`、`review/submission_preflight.json`可追溯。三底并排图`review/n3_village_architecture_g1_v1_three_backgrounds.png`为2336×768 RGB；白/深/土单底均为2048² RGB，只供预览。
- 用户已验收G1原图，并指出尚不足以布景，后续组件已补齐到N3首次布景。本轮已将G1四件按真实Alpha拆为8件正式Unity Sprite，接入N3村庄战斗组；原始图集未修改。N3部署交接见`plan/n3-village-deployment-handoff-20261014.md`。
