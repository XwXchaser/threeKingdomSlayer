---
id: kd_5b449304-df41-4583-90fa-211c05f7ee1e
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# N3「村落搜救」布景参考图 — 生图准备稿（2026-10-14，待批准）

> **状态：已于 2026-10-14 提交一次，服务端因额度耗尽拒绝（HTTP 403 `insufficient_user_quota`），未生成图片、无重试，详见第 14 节。** 未修改场景、未改变工作区。Image 2（N3 位置纯场景图）已在本地截取完成，见第 5 节。
> 参考对象：**N1 与 E0 已验收参考图的实际请求记录**（本文件第 2 节，逐条来自落盘的 `.request.json`），并按该项目做法重写了关键词与参考图组合。
> 上游：`plan/ev-n3-village-composition-plan-20261014.md`（EV/N3 构图与显露节奏方案）。画风：`design/art-style-guide.md`。工作流：`skill/workflows/image-asset-generation.md`、`skill/gpt-image-generation.md`。

---

## 1. 任务定位

N3 = 村落搜救战斗节点，背景语义**村落边缘空地**。用户已明确：**N3 是直路，构图与 N1 一致**；**必须是三国风**。

与几何一致：转弯在 route 212.118 结束，N3(266.1165) 位于转完后的 −45° 直线尾段末端；相机是固定机位，`Main Camera` 在 (0,3,−10)、pitch 18°、fov 60、竖屏 aspect≈0.5。**同一台相机 → N1 的取景就是 N3 的取景**，这是"构图与 N1 一致"的物理依据。

## 2. 已验收参考图的实际做法（本轮依据，全部来自落盘请求记录）

### 2.1 逐次记录

| 产物 | 交付/验收 | 上传的参考图（按顺序） | 参数 |
|---|---|---|---|
| **N1 母图** `C:/Users/Administrator/Pictures/gptGen/n1_battlefield_reference_retry_20261007_011121.png` | 当前构图锚点 | 1、2 = **两张实际 Unity Game 截图**（`C:/Users/Administrator/Downloads/screenshot-20261007-005516.png`、`...005539.png`）。**这两张源截图现已不在磁盘上**（已核对） | sunburst / **high** / input_fidelity=high / 1024x2048 / png / n=1 |
| **E0 R1 v2** `changbanpo_e0_reference_workflow/r1_n1_to_e0_approach_v2.png` | 构图获用户认可 | 1 = **N1 母图**；2 = 三国概念图14 | 同上 |
| **E0 R1 v3** `..._v3.png` = `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/E0/References/E0_ApprovedSceneReference_v3.png` | **用户已验收** | 单图 = v2（聚焦修订） | 同上 |
| **J1 v1** `changbanpo_j1_reference_workflow/j1_after_e0_reference_v1.png` | 用户否决 | 1 = `E0_ApprovedSceneReference_v3.png`；2 = `Library/Locus/Screenshots/locus_game_20261010_101301_247.png`（489×979 实际 Game 截图）；3 = 概念图14 | high |
| **J1 v2** `..._v2.png` | 因"村庄出现过早"需修订 | 1 = **几何分析图**（只锁左右圆弧与路牌落点）；2 = `E0_ApprovedSceneReference_v3.png`；3 = 概念图14 | high |
| **J1 v3** `..._v3.png` | **用户已验收** | 单图 = v2（聚焦修订：移除左侧村庄） | **max** |

### 2.2 可复用的结构规律

1. **新节点 = 3 张图**：① **前节点已验收参考图**（构图/连续性/配色，priority 最高）＋② **实际部署 Game 截图**（真实机位、地平线、地面与天空占比、中央净空）＋③ **三国概念图14**（只有绘制语言）。J1 v1 就是这个配方。
2. **下一轮修订 = 单图**：只上传上一版，声明"整体构图已验收，只改 X"。E0 v3、J1 v3 都是这样通过的。
3. **提示词固定骨架**（E0 v2 / J1 v1 / J1 v2 一致）：
   `不是自由重设计相机 / 不是 2:3 海报` → `REFERENCE ROLES:` 逐图编号＋逐条"不要复制什么" → 连续性声明 → 内容描述 → 交付条款（完整不透明 PNG、1:2 竖屏、低机位、不是 Sprite/图集/抠图） → `NEGATIVE CONSTRAINTS:` 一整段逗号分隔。
4. **请求记录登记法**：逐张 `order / path / upload_filename / bytes / sha256`（J1 v1/v2 还带 `role`），另存 `prompt_sha256`、`authorization`、`planned_submission_count`、`no_automatic_retry`，中文提示词另存 `approved_chinese_prompt` / `approved_chinese_negative` 供评审。
5. **质量档位**：N1/E0/J1 v1–v2 用 `high`，自 J1 v3 起改为 `max`；当前项目统一 **`max`**。
6. **尺寸恒为 1024×2048**（1:2 竖屏）。概念图14 是 1024×1536（2:3），所以 N1/E0 的提示词都专门写了"约 1:2 竖屏、不是电影感 2:3 海报"来抵消它。
7. **N1 的原始做法无法复刻**（两张源 Game 截图已丢失）。N1 母图本身是唯一幸存的构图锚点，后续节点都以它为 Image 1——E0 与 J1 就是这么做的。
8. **已验收提示词的两条通用条款值得直接沿用**：`Favor composition fidelity over decorative detail.`（N1）、`Deliver one complete opaque RGB scene reference image for composition review. This is not a transparent sprite, not a sprite atlas and not a Unity cutout.`（E0 v2）。

### 2.3 关键可复用措辞（重要）

J1 v2 的**村庄段落实际已经画出过村庄**，而用户当时唯一的反对意见是**时机/位置**（村庄在 J1 就出现），**不是村庄的画法**。因此这段已被接受过的措辞可以直接作为 N3 的词汇基础：

- 英文原句（J1 v2，已实际产出村庄）：
  `The LEFT branch leads to the VILLAGE and is a GREEN STONE ROAD. Show actual blue-gray and muted gray-green irregular stone paving with visible stone edges, a little moss or grass in the joints and low vegetation along the shoulder. ... a village of small thatched houses, low fences, earth-and-stone wall remnants and sparse trees placed outside the lane. ... This is a lightly war-damaged village in the same battlefield world, not a modern park, fairy-tale garden, neon-green surface or dark forest. Only restrained damage, thin smoke and a few remnants.`
- 中文对应句同样存在于 `j1_after_e0_reference_v2.request.json` 的 `approved_chinese_prompt`。

**N3 只需要把其中的"向左远处退去""分叉"语义改成"直路两侧与中远景"**，其余建材、色调、规模、破损程度的表述保持不变。这比另起一套村舍描述更可靠：同一批词汇已经通过过一次生成。

## 3. 必须先说明的一处文档勘误

`plan/j1-vegetation-deployment-handoff-20261014.md` 第 156 行写「已验收的 `j1_after_e0_reference_v3.png`（该版本曾在 J1 左侧绘制村庄建筑…）」。**这句与权威记录相反。** `plan/j1-scene-reference-v3-left-road-refinement.md` 是 v3 的正式记录：v3 的唯一改动就是**移除左侧全部村庄建筑**（近景茅屋、沿石路房屋、远处屋群、建筑式瞭望塔），替换为篱笆/草簇/灌木/碎石/土肩，用户据此验收。

结论：**v3 只能作为「云、远山、石板路、路侧物件、日照」参考，不能作为村舍形态参考。** 本项目目前没有已验收的村舍参考图；村舍靠第 2.3 节那段已产出过村庄的措辞 + 第 6 节三国禁列来约束，而不是靠某张村舍图。

## 4. 实测构图数值（N1 母图，作为唯一构图基准）

对 `n1_battlefield_reference_retry_20261007_011121.png`（1024×2048、RGB、不透明）逐行解码：

| 区域 | 纵向占比 | 实测 |
|---|---|---|
| 天空 | 0.00–0.33 | 蓝天（行均值约 89–169 / 142–188 / 203–212）＋浅云 |
| 远山灰雾 | 0.24–0.33 | 冷灰蓝低对比带 |
| **地平线** | **≈0.35** | 全宽暖色带 |
| 地面 | 0.35–1.00（**65%**） | (156,110,64) → (206,146,79)，越近越亮越暖 |
| 中景侧带 | 0.33–0.55 | 暗物件只在左右两侧，中央是明亮开阔走廊 |
| **最下方约 30%** | 0.70–1.00 | **完全空的地面，零物件** |

**与游戏渲染的对应（按 Shader 同一套投影复算）**：真实相机几何地平线在 yFrac 0.219，但卷轴地面在 depth>18 后按 `0.009·(depth−18)²` 下弯，地面在 depth 30–40 处落到 yFrac 0.32–0.35。**参考图的地平线 0.35 正好对应游戏里地面实际消失的位置**——所以提示词必须写"地平线约在上方 35%"，不能按真实几何画到 22%。

## 5. 要上传的参考图（按已验收配方，3 张）

| 顺序 | 文件（已核对存在可读） | 规格 | 职责 |
|---|---|---|---|
| **Image 1** | `C:/Users/Administrator/Pictures/gptGen/n1_battlefield_reference_retry_20261007_011121.png` | 1024×2048 RGB，3432777 bytes | **首要且具约束力的构图基准**：竖屏比例、机位高度、地平线高度、相机朝向、地面占比、道路消失点、中央战斗通道宽度、左右边缘侵入量、蓝天/浅云/冷灰蓝远山/暖黄土色/明亮日间光照。**唯一例外：不继承它的暖黄土路面材质**（N3 是村落石板路） |
| **Image 2** | `Library/Locus/tmp/n3-reference-prep/n3_left_266_10.png`（本地新截，见下方） | 489×979 RGB，859648 bytes | **真实机位、地平线、地面与天空占比、中央净空、已部署石板路材质**的核对参考；不照抄道具与 HUD，也不取它的景物密度与空地比例 |
| **Image 3** | `C:/Users/Administrator/Downloads/微信图片_20260928101137_14_455.png` | 1024×1536 RGB | **只有高清像素化三国战场的绘制语言**：大体块、锐利色簇、厚重土石木材、冷暖对比。忽略人物、武器、战斗特效、旗帜符号、完整城门构图 |

**Image 2 = 本地新截的 N3/左支位置纯场景图（已按用户选择完成）**

- 文件：`Library/Locus/tmp/n3-reference-prep/n3_left_266_10.png`；489×979、RGB、859648 bytes。
- 机位：Battle Main Camera，Y 路线 pose=266.1165（= N3 停点，progress=1.0、right=false），aspect 0.4995，与既往部署截图同规格。
- 客观内容（逐行解码实测）：天空占 yFrac 0.00–0.29；可视化地平线 ≈0.30；其下 70% 全部是**已部署的村落石板路与路肩**（行均值约 (127,126,81) 的灰橄榄色，中部最暗、两侧略暖），带石板纹理与零星苔草绿；**没有村落、没有分叉、没有城门，路侧物件极少**（只有边缘处少量贴脸物）。
- 采集方式：**全程未写入任何场景序列化字段**。投影用自建的 `_YS*` 全局量实现，UI 用临时相机的 cullingMask 排除，玩家光环用 `Renderer.forceRenderingOff`（运行时属性），裁剪包围盒用 `Renderer.localBounds`（运行时属性）；临时相机带 `HideFlags.HideAndDontSave` 并渲染到 RenderTexture，取像素后立即销毁。
- 采集前后核对：`Battle:False YJunctionSample:False`（dirty 未变）、无遗留对象（`LOCUS_N3_CAPTURE` 不存在）、`viewCamera=None / progress=0 / right=False` 保持原样。
- 同批另存两张对照（**不上传，仅供核对**）：`ev_left_225_00.png`（EV 停点，能看到左支路侧物件带）、`j1_stop_190_00.png`（已验收的 J1 停点画面，下半幅 65% 为暖黄土道路、0.15–0.30 是路侧物件带）。
- **使用限制（已写进第 7/8 节提示词）**：图2 只提供真实机位、地平线、地面与天空占比、中央净空和已部署石板路材质；**不得从它取道路的屏幕宽度、景物密度或空地比例**——它是村落建成前的地面近景，N3 的目标构图仍以图1（N1 母图）为准。

**不上传**：J1 v3（分叉图、且其村舍已被移除，只会把分叉画进来）、v6（待验收、残留桶箱）、v2（整体构图已被否决）、E0 券拱参考、任何带标注框的分析图。

## 6. 任务类型与请求参数

```text
endpoint       POST https://api.muskapis.com/v1/images/edits        (multipart/form-data，字段名 image[])
model          gpt-image-2.5-sunburst
size           1024x2048
quality        max              (项目统一档位；J1 v3 起已用 max)
input_fidelity high
output_format  png
n              1
background     不上传（完整不透明场景参考，不是可部署 Sprite）
```

- 输出目录：`C:/Users/Administrator/Pictures/gptGen/n3_village_scene_reference_v1/`
- 文件：`n3_village_scene_reference_v1.png` + 同基名 `.request.json` / `.response.json` / `.status.json`
- 请求记录按第 2.2 节第 4 条登记：`reference_images`（order/path/upload_filename/bytes/sha256/role）、`prompt_sha256`、`authorization`、`planned_submission_count=1`、`no_automatic_retry`、`approved_chinese_prompt`、`approved_chinese_negative`。
- **只提交一次，不自动重试。**

## 7. 完整中文正向提示词

这是一次多图参考编辑，不是自由重设计相机，也不是标准的 2:3 海报构图。为三国战场连续卷轴游戏制作一张 N3「村落搜救」节点的完整环境参考图：汉末三国时期被战火波及的村落边缘空地。竖屏 1:2，1024×2048，完整不透明 PNG。

参考图分工：图1 是首要且具约束力的构图基准，严格锁定它的竖屏比例、机位高度、地平线高度、相机朝向、地面占比、道路消失点、中央战斗通道宽度和左右边缘的侵入量，并继承它的蓝天、浅云、冷灰蓝远山、暖黄土色与明亮日间光照；不要把图1 与其它图平均成一台新相机，不要重设计透视关系。**唯一不继承的是图1 的路面材质**：图1 的暖黄土路在本图中替换为下方的村落石板路。图2 是当前实际部署游戏在 N3 位置的真实画面（空场加上已部署的村落石板路），只作为真实机位、地平线、地面与天空占比、中央净空和已部署石板路材质的核对参考；不要从图2 取道路的屏幕宽度、景物密度或空地比例（它是村落建成前的地面近景），也不要照抄其中的道具与 HUD。图3 只提供高清像素化三国战场的绘制语言：大体块、锐利像素色簇、厚重土石木材、冷暖对比与战争气氛；忽略其中的人物、武器、战斗特效、旗帜符号和完整城门构图。

构图与骨架：画面上方约三分之一是蓝天与浅云，地平线附近是低对比的冷灰蓝远山和淡黄土雾；地平线以下约三分之二是连续地面。一条直路从画面下方中央连续延伸到画面中央远方的消失点，路幅稳定，不转弯、不分叉、不在半途消失。道路占据画面横向大约中间一半，左右各留约四分之一宽度给中景侧景。地平线以下最下方约三分之一必须保持完全连续、无遮挡的空地，用于玩家与敌人的战斗表现。追求构图保真优先于装饰细节。

道路材质：村落石板路。清楚可见的青灰与灰绿色不规则石板铺面，石块边缘明确，石缝里嵌少量苔草和压实的暖土，路肩逐渐长出低矮草和灌木；近处石块更大更清楚，向远处并入暖黄土色。道路两侧以夯土路肩过渡到草地地面；路面保持清楚、开阔、可行走，不变成封闭走廊、高墙夹道或阴暗森林。

村落内容只出现在道路左右两侧与中远景，不进入中央道路与下半幅：低矮的汉末三国民居，夯土或土坯墙配茅草屋顶的单层矮屋，屋顶低平朴素、屋脊简单，门窗为朴素木框；可有一两处被战火损毁的破屋，露出烧黑的木梁与塌落的茅草。沿路布置粗木与竹编的矮篱笆、断裂栅条、能看到夯层与断面的低土埂和少量残墙。院落附近有木井台、柴垛、竹筐、陶罐、麻布粮袋和捆束木料。植被为中低高度的柳形或阔叶树冠、灌木与草簇，高度低于建筑，不成排、不连成遮挡中央的树墙。远处村落只用低对比、低细节的屋顶轮廓和树线表示。这是同一战场世界中受战乱影响的村庄，不是现代园林、童话花园、荧光绿路面或阴暗森林；只有少量破损、细而稀疏的淡烟和极弱的余烬，烟不得遮挡空间。

三国风要求：严格汉末三国民间村落形制，只使用夯土、土坯、茅草、粗木、竹、石、陶、麻布等朴素材料，屋顶低矮朴素。不出现飞檐、斗拱、琉璃瓦、脊兽、灯笼、牌坊、朱漆大门、青砖白墙的徽派院落、寺塔、城楼、城门、城墙、望楼；不出现欧洲中世纪半木结构、石板城堡、尖顶钟楼或奇幻塔楼；旗帜或布幡上不出现文字、数字、徽记或后世阵营标识。

画风：延续高清像素化三国战场美术，以大体块、成簇的硬边光影和受控的土石竹木纹理表现材质；冷铁灰与暖土色对比，朱红只作极少量破布点缀，金白高光只用于受光边缘。粗粝厚重但不昏暗，明亮日间光照，光照方向与图1 一致。不是照片，不是光滑 3D，不是萌系绘本，不是普通插画加马赛克滤镜，不是低分辨率复古像素。

交付一张 1024×2048 的完整、不透明 PNG 场景参考图，用于构图评审。它不是透明 Sprite，不是 Sprite 图集，也不是 Unity 抠图素材。只画静态环境：无玩家、无士兵、无百姓、无伤员、无尸体、无手、无手臂、无武器、无敌人阵型、无斩击特效、无 HUD、无文字、无字幕、无箭头、无水印、无 logo、无黑边。

## 8. English positive prompt

This is a multi-image reference edit, not a free camera redesign and not a standard 2:3 poster composition. Create one complete vertical environment reference image for the "N3 village rescue" node of a scroll-based Three Kingdoms battlefield action game: the village-edge clearing of a late Han Chinese village touched by war. Vertical 1:2, 1024x2048, complete opaque PNG.

REFERENCE ROLES: Image 1 is the primary and binding composition baseline. Strictly lock its portrait aspect ratio, camera height, horizon height, camera direction, ground coverage, road vanishing point, central combat-lane width and left/right edge intrusion, and inherit its blue sky, pale clouds, cool gray-blue distant mountains, warm ochre soil and bright daytime lighting. Do not average Image 1 with the other images into a new camera and do not redesign the perspective. The ONLY thing not inherited from Image 1 is its road surface material: its warm ochre dirt road is replaced in this image by the village stone lane described below. Image 2 is the currently deployed gameplay frame at the N3 position (bare ground plus the already deployed village stone lane) and serves only as a corroborating reference for the real camera, horizon, ground-to-sky proportion, central clearance and the already deployed stone-lane material; do not take the road's on-screen width, scenery density or empty-area ratio from Image 2 (it is a ground close-up from before the village is built), and do not copy its props or HUD. Image 3 supplies only the high-definition pixel-cluster Three Kingdoms battlefield drawing language: strong material masses, deliberate sharp pixel clusters, heavy earth, stone and timber, warm/cool contrast and war atmosphere. Ignore its people, weapons, combat effects, banner symbols and complete fortified-gate composition.

COMPOSITION: the upper third of the frame is blue sky with pale clouds; near the horizon sits a low-contrast band of cool gray-blue distant mountains and pale ochre haze. Below the horizon about two thirds of the frame is continuous ground. One straight road runs from the lower center continuously to a single vanishing point in the middle distance; the width stays stable and the road neither curves, forks, nor disappears mid-way. The road occupies roughly the middle half of the frame width, leaving about one quarter of the width on each side for midground side scenery. The lowest third of the ground below the horizon MUST remain completely continuous, unobstructed empty ground reserved for player-versus-enemy combat. Favor composition fidelity over decorative detail.

ROAD SURFACE: a village stone lane. Clearly readable blue-gray and muted gray-green irregular stone paving with distinct stone edges, a little moss or grass and compacted warm soil in the joints, and low grass and shrubs growing along the shoulders; stones are larger and clearer near the camera and merge into warm ochre soil in the distance. The lane transitions through compacted earth shoulders into grassy ground on both sides. Keep the walking surface clear, open and walkable; do not turn it into an enclosed corridor, a walled alley or a dark forest.

VILLAGE CONTENT on the left and right sides and in the middle-to-far distance ONLY, never on the central road or in the lower third: low single-storey late-Han Chinese dwellings with rammed-earth or adobe walls and thatched roofs, low plain roof ridges and simple wooden-framed doors and windows; one or two war-damaged dwellings with exposed charred timber beams and collapsed thatch. Along the lane use low rough wooden and bamboo woven fences, broken fence slats, low earth berms showing visible rammed-earth layers and a few ruined wall stubs. Near the courtyards include a wooden well curb, firewood stacks, bamboo baskets, clay jars, hemp sacks of grain and tied bundles of timber. Vegetation is middle-to-low willow-shaped or broadleaf canopies, shrubs and grass clumps, all shorter than the buildings, not in rows and never forming a tree wall that blocks the center. Show distant village form only as low-contrast, low-detail roof silhouettes and a tree line. This is a lightly war-damaged village in the same battlefield world, not a modern park, fairy-tale garden, neon-green surface or dark forest; only restrained damage, thin sparse pale smoke and very weak embers. Smoke must not block the space.

THREE KINGDOMS STYLE REQUIREMENTS: strictly late-Han Three Kingdoms vernacular village forms. Use only rammed earth, adobe, thatch, rough timber, bamboo, stone, clay and hemp cloth, with low plain roofs. No upturned eaves, no bracket sets, no glazed roof tiles, no roof ridge beasts, no lanterns, no memorial archways, no vermilion gates, no whitewashed-brick courtyard architecture, no pagodas, no gate towers, no city walls, no city gates, no watchtowers; no European half-timber buildings, stone castles, spired bell towers or fantasy towers; no lettering, numbers, heraldry or later-dynasty faction symbols on flags or cloth banners.

ART STYLE: continue the high-definition pixelated Three Kingdoms battlefield art language, expressing earthen walls, thatch, rough timber, bamboo, stone paving and pottery with strong masses, clustered hard-edged shading and controlled earth/stone/wood textures. Contrast cool iron-gray with warm earth tones; use vermilion only as a very small accent of torn cloth and gold-white highlights only on lit edges. Heavy and rough but never dark; bright daytime lighting in the same direction as Image 1. Not a photograph, not glossy 3D, not cute storybook art, not an ordinary illustration with a mosaic filter, not low-resolution retro pixel art.

Deliver one complete opaque PNG scene reference image at 1024x2048 for composition review. This is not a transparent sprite, not a sprite atlas and not a Unity cutout. Static environment only: no protagonist, soldiers, civilians, wounded people, bodies, hands, arms, weapons, enemy formations, slash effects, HUD, text, subtitles, arrows, logo, watermark or black borders.

## 9. 负面约束（随主提示词提交，一整段）

中文：道路转弯、道路分叉、第二个出口、第三条道路、T 字死路、道路断开、道路半途消失或消失点偏移、地平线偏移、变成 2:3 电影海报比例、倾斜机位、俯视或等距视角；横跨中央道路的房屋、高墙、拒马、巨车或成排屋舍；中央下半幅出现任何物件、把道路夹成封闭走廊或高墙夹道；把村落画到道路上或路口；青砖白墙徽派院落、飞檐、斗拱、琉璃瓦、脊兽、灯笼、牌坊、朱漆大门、寺塔、城楼、城门、城墙、望楼；欧洲中世纪半木结构、石板城堡、尖顶钟楼、奇幻塔楼；复制图2 的分叉与路牌、复制图2 的道具与 HUD、复制图3 的人物/武器/战斗特效/军阵/城门/旗帜符号；荧光绿路面、现代柏油、现代交通设施；大面积浓烟遮挡空间；人物、士兵、百姓、伤员、尸体、玩家身体、手、手臂、武器、敌阵、斩击、HUD、文字、字幕、箭头、logo、水印、黑边；黄昏、夜景、阴暗森林、悬崖谷地突变；照片写实、光滑 3D、萌系绘本、普通插画加马赛克、低分辨率复古像素、模糊软边、拼贴、随机噪点。

English: curving road, forking road, a second exit, a third road, dead-end T junction, disconnected road surface, road disappearing mid-way or a shifted vanishing point, shifted horizon, 2:3 cinematic poster ratio, tilted camera, bird's-eye or isometric view; houses, high walls, barricades, large carts or a continuous row of buildings spanning the central road; any object in the lower central third, the road pinched into an enclosed corridor or a walled alley; village content placed on the road or at a junction; whitewashed-brick courtyard architecture, upturned eaves, bracket sets, glazed roof tiles, roof ridge beasts, lanterns, memorial archways, vermilion gates, pagodas, gate towers, city walls, city gates, watchtowers; European half-timber buildings, stone castles, spired bell towers, fantasy towers; copying Image 2's fork and signpost, copying Image 2's props and HUD, copying Image 3's people, weapons, combat effects, battle formations, city gate or banner symbols; neon-green road surface, modern asphalt, modern traffic furniture; large amounts of dense smoke hiding the space; people, soldiers, civilians, wounded, bodies, protagonist body, hands, arms, weapons, enemy formations, slashes, HUD, text, subtitles, arrows, logo, watermark, black borders; sudden sunset, night, dark forest or cliff valley; photorealism, glossy 3D, cute storybook art, mosaic-filtered illustration, low-resolution retro pixel art, blurry soft edges, collage, random noise.

## 10. 色彩锚点（生成后客观比对用，不写进提示词）

| 项 | 目标 | 来源 |
|---|---|---|
| 天空 | 约 (89–169, 142–188, 203–212) | N1 母图实测 |
| 地面（近地平线 → 前景） | 约 (156,110,64) → (206,146,79) | N1 母图实测 |
| 村落石板路（路面 / 路侧） | 约 (140,120,87) / (160,121,70) | 已部署 `J1_StoneRoad_Center_v1` / `_Sides_v1` 实测均值 |
| 道路半宽 / 路肩宽 | 世界 2.45 / 1.5（石板路宽约 4.9） | `J1_Ground_Branches.mat` 实测 |
| 远山 | 冷灰蓝低对比 | N1 母图 + `N1_FarBackground_v1`（均值约 (142,152,166)） |
| 篱笆 / 树冠（已部署） | (90,62,41) / (87,79,39) | `J1_A03_IntactLowFence_v1`、`J1_Tree_Broad_v2` 实测均值 |

## 11. 生成后验收清单

1. **机器检查**：PNG 可读、1024×2048、不透明 RGB、大小合理；`.request.json` / `.response.json` / `.status.json` 落盘且参数与本稿一致；单次提交、零自动重试。
2. **构图检查**（用 `Library/Locus/tmp/n3-reference-prep/pnggrid.py` 逐行解码取数，不依赖目视）：地平线 yFrac 0.32–0.38；天空占比 0.30–0.38；地面 ≥0.62；**yFrac 0.70 以下暗色物件计数为 0**；中央横向 0.30–0.80 的走廊在 yFrac 0.35–0.70 内以暖色为主、无暗块横穿；无分叉（不存在两条同时离开下缘的道路）。
   注意：**这些阈值按图1（N1 母图）的目标结构判定，不是照抄当前 N3 画面的空场状态**（当前 N3 位置只有地面与石板路）。生成图必须补上两侧与中远景的村落内容，同时保持最下方约 30% 连续空地、中央走廊净空、以及道路为石板而非纯黄土。
3. **内容检查**：无城门/城墙/塔楼/牌坊/飞檐/灯笼/青砖白墙院落/欧洲半木屋；无人物/军阵/HUD/文字；路面为青灰灰绿石板而非纯黄土或纯绿块；村落只出现在两侧与中远景。
4. **连续性检查**：蓝天、远山、土色、日照方向与图1 一致；中央下半幅连续开阔。
5. **交付**：原图、与 N1 母图的并排对照、完整真实路径一并交给用户。**技术通过不等于用户审美验收**；未获用户验收前不拆件、不部署、不发起下一笔收费生成。

## 12. 通过之后的关系

参考图验收通过后才进入拆件：按 `plan/ev-n3-village-composition-plan-20261014.md` §9 的缺口清单（茅屋/破屋/屋顶剪影/院墙/破门/烟柱/生活小件）生成**独立透明素材**，再按该方案 §6 三条规则与 §8 层级命名部署到 Y 层的 EV/N3 两个新组。参考图本身不裁成 Sprite 直接部署。

## 13. 未决项

- **Image 2 已确定为本地新截的 N3 位置纯场景图**（`Library/Locus/tmp/n3-reference-prep/n3_left_266_10.png`），规格与采集方式见第 5 节；不再需要 a/b 选择。
- `plan/j1-vegetation-deployment-handoff-20261014.md` 第 156 行关于 v3 含村庄建筑的表述与 v3 正式记录矛盾（见第 3 节）。本轮未修改该文件，是否更正由你决定。
- 本图不涉及 EV 节点；EV 是否需要独立的「村落外围」参考图待 N3 验收后再议。
- 授权范围：本稿只等"批准提交一次"，不构成拆件、收费批处理或场景部署的授权。

## 14. 提交记录（2026-10-14，用户批准后执行）

用户明确批准后，按第 5/6 节的三张参考图与参数提交了一次。结果：

- **HTTP 403，`insufficient_user_quota`**：服务端原文 `预扣费额度失败, 用户剩余可用额度: $0.00, 需要预扣费额度: $0.00 (request id: 20261014094054344645110Ifzg0CUd)`。耗时 2.552 秒。
- `submission_count=1`、`automatic_retries=0`；**没有生成图片**（无 `.png`），本次未产生生图费用；按项目规则未重试。
- 落盘记录（无图片那一份）：
  - `C:/Users/Administrator/Pictures/gptGen/n3_village_scene_reference_v1/n3_village_scene_reference_v1.request.json`（14894 bytes）
  - `C:/Users/Administrator/Pictures/gptGen/n3_village_scene_reference_v1/n3_village_scene_reference_v1.status.json`（362 bytes）
- 请求记录里的可核对指纹：`prompt_sha256=dfb623cafbc4a417ab9859116d78aa264d8ad72438b037c0614f9d19204a9f70`；参考图逐张 `order/bytes/png尺寸/sha256`：
  1. N1 母图 3432777 bytes、1024×2048 colorType 2、sha256 `11c0b0b90aecb7cd…`
  2. 本地 N3 现场图 859648 bytes、489×979 colorType 2、sha256 `23e867f60667623c…`
  3. 概念图14 2596005 bytes、1024×1536 colorType 2、sha256 `643d50375a9fcdd2…`
- 中英文提示词与负面词原文已完整写入该 `request.json`（`prompt` / `approved_chinese_prompt` / `approved_chinese_negative`），重提时逐字复用即可，不需要重写。
- **结论**：`403 insufficient_user_quota` 是账号额度问题，与提示词、参考图、参数无关；不要按「堆 prompt」或「换模型」处理，也不要连续重试。恢复额度后只需用同一份 `submit.py` 与同一份记录再提交一次。
