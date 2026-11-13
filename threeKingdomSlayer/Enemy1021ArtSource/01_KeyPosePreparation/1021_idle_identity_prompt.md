# 1021 idle 身份图 · 生成规格（已验收）

> **验收状态：用户 2026-11-09 验收通过。**
> 本文件是这张图的唯一规格说明（提示词、参考图职责、参数、溯源、被否版本）。
> 正式台账见 `Enemy1021ArtSource/00_Index/identity-images.csv`。

## 产物

| 项 | 值 |
|---|---|
| 入库文件 | `Enemy1021ArtSource/01_KeyPosePreparation/1021_idle_identity_greenscreen_1024.png` |
| 尺寸 / 格式 | 1024×1024 RGB（纯绿幕，alpha=0 的透明版已合成为绿底） |
| 字节 / SHA-256 | 461,911 B / `49634fdcac936a66dbc5cb29af3af4f48c2d03e0fdf5dca346d4c6e2d7b1a176` |
| 落位口径 | 绿幕 (0,159,62) std=0 ｜ **人物本体高 696** ｜ **可见脚底 y=895** ｜ **身体中心 x=474.5** ｜ 封闭孔 0 px |
| 盾侧 | **画面左侧**，由人物**右手**持握；空着的左手垂在画面右 |
| 身份 | 素灰深布帽＋发髻＋垂布带；浅灰胡茬；灰米交领布袍＋肩胸几块深色皮革/铁小甲片；深棕皮革腰带与厚护腕；深棕裤、灰白绑腿、厚底战靴；木胎盾＋深色铁包边铆钉 |

## 溯源链

1. **五人并排比例研究图 v3**（本地：`C:/Users/steam/Pictures/gptGen/enemy_proportion_lineup_v3_1021/`）定下体型家族、画风、盾侧与身份；1021 在该图里本体高 406 px＝0.962×1011，五人脚底线同为 y=781。
2. 从 v3 组图裁出 1021 并重贴为 1024² 绿幕底件（`1021_base_v3_green_1024.png`，本体 698/脚底 895/中心 474.5）——**这是所有 1021 单图编辑的底件**。
3. 在该底件上做**单图编辑**，只改表情，三档迭代：
   - `idle_fierce_v1` —— 龇牙露齿、瞪眼 → **用户判「用力过猛、太浮夸」，作废**
   - `idle_menace_v1` —— 压眉＋收窄直视＋闭嘴硬线 → **用户 2026-11-09 验收通过**
4. 原生出图（原生透明 RGBA）由 `Tools/visual_brief/compose_identity_from_alpha.py` 按 alpha≥128 二值化→填封闭孔→缩放到本体高 696→贴 (474.5-w/2, 895-h)→合成纯绿 (0,159,62)。

## 接口与参数（实际提交）

| 项 | 值 |
|---|---|
| 接口 | `POST https://api.muskapis.com/v1/images/edits`，multipart **一张 `image[]`** |
| 模型 | `gpt-image-2.6-sunburst` |
| 参数 | `size=1024x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1` / **不传 background** |
| 参考图 | `C:/Users/steam/Pictures/gptGen/enemy1021_identity_v2/1021_base_v3_green_1024.png`（sha256 `3a60eab0ba8ff4c…`）——从 v3 组图裁出重贴的 1021 底件；提供身体/站姿/比例/装备/盾侧/布帽/胡茬，**只允许改表情** |
| 回执 | HTTP 200 / 135.35s / usage 2512 / 输出 sha256 `8849ba43e554f3ce45fa7d73fe71442dd5195eaa912c135a42439ceaba879d9e`（原图） |
| 纪律 | 一次提交、不自动重试；提示词真源 `Tools/visual_brief/prompts/1021_idle_menace_v1.*` |

> 服务端提示：五图重请求（~2.2 MB）曾连续两次上游超时（读超时 901s 丢回执；HTTP 504 upstream_timeout，request id `202611090630048287691621m2pVYcD`），**图像接口无取回端点，丢回执＝丢产物**；轻量单图请求（538 KB）正常。

## 提交用英文提示词（正文 + 负面词）

```text
Edit image 1 into a single front-facing 2D game-sprite identity image of a Han-era Cao army shield-bearer. Keep everything about the character exactly as it is in image 1 — the body, the stance, the head-to-body proportions, the plain dark cloth cap with its trailing tie, the grey-beige cross-collar cloth tunic with only a few small dark leather-and-iron armor plates over the shoulders and chest, the brown leather belt and sturdy bracers, the brown trousers, the grey-white leg wraps, the heavy boots, and above all THE SHIELD: the same broad rectangular timber shield with dark iron bands and studs, held upright in front of his body on the LEFT side of the picture, in front of his right arm, with his right arm hidden behind it and his left arm hanging free at his side. Do not move the shield to the other side of the picture, do not swap which arm holds it, and do not mirror the character.

CHANGE ONE THING ONLY — HIS EXPRESSION, and make it MENACING BUT RESTRAINED. He is a disciplined veteran on guard, not a berserker. His brows are low and angled slightly inward in a hard frown; the crease between them is shallow, not deeply knotted; his eyes are narrowed to a hard, unwavering stare straight out of the picture at the player, the upper lids pressed down but his eyes not bulging and no white showing; his nostrils are slightly flared; his mouth stays CLOSED, the lips pressed together in a firm, tight line with the corners pulled slightly downward; his jaw and cheek muscles are set hard. He is angry and dangerous, but controlled — the menace of a professional soldier who is done talking. His grey stubble, his cap and his whole outfit stay exactly as they are.

Keep the flat chroma-green background (RGB 0,159,62), the same full-body framing and the same drawing size. High-definition pixel-art Three Kingdoms action-game rendering: clean dark contours, clustered light and shadow planes, solid cool iron and warm cloth and leather volumes. Stern and dangerous, not cute.

Negative constraints: bared teeth, showing teeth, snarling, snarl, roaring, roar, screaming, scream, yelling, shout, open mouth, mouth wide open, bulging eyes, wide open eyes, exaggerated grimace, exaggerated fury, comic rage, overacting, exaggerated expression, deep knotted brow crease, snarling mouth, calm expression, neutral expression, relaxed expression, tired or weary eyes, sleepy eyes, gentle gaze, soft expression, kind face, kindly face, smile, smiling, smirk, sad face, worried expression, full black beard, long beard, braided beard, goatee, metal helmet, iron helmet, closed helmet, visor, glowing eyes, red plume, red scarf, shield on the right side of the picture, shield held in the left hand, mirrored character, sword, spear, bow, drum, second person, extra limbs, cropped limb, cropped head, hidden face, face covered, side view, three-quarter view, sitting, kneeling, western fantasy knight, anime moe, photorealism, 3D render, watercolor, blur, scenery, ground, shadow, text, UI, watermark, checkerboard
```

## 中文提示词（存档）

```text
把图一改成一张汉末曹军持盾兵的正面全身 2D 游戏精灵身份图。图一里的角色**其它一切保持原样**：身体、站姿、头身比例、素色深布帽与垂下的布带、灰米色交领布袍（肩胸只覆几块深色皮革与铁的小甲片）、深棕皮革腰带与厚护腕、深棕裤、灰白绑腿、厚底战靴——以及最重要的**那面盾**：同一面宽矩形木胎盾，深色铁包边带铆钉，竖直举在身前、位于**画面的左侧**，在人物**右臂**前面，右臂藏在盾后，左手空垂在身侧。不要把盾移到画面另一边，不要换手，不要镜像这个角色。

**只改一样——表情，而且要「凶但克制」**。他是一个沉着的老兵在戒备，不是一个狂暴的疯子：眉毛压低、略向内斜，形成一道硬 frown；眉心只是浅浅一道纹，不要拧成深褶；眼睛收窄成一道锐利、不眨的直视，目光钉在玩家身上，上眼睑下压但**不瞪大、不露眼白**；鼻翼略微张开；**嘴必须闭上**，双唇紧抿成一条硬线，嘴角略向下压；下颌与颊肌绷紧。他的情绪是愤怒且危险，但克制——像一个不再废话的职业士兵的威势。他的浅灰胡茬、布帽与整套装备完全不变。

保持纯色绿幕背景（RGB 0,159,62）、同样的全身构图和同样的绘制尺寸。高清像素化三国动作游戏美术：清楚的深色轮廓、成簇的明暗色块，以及有体积的冷铁、布料与暖棕皮革；严肃、有压迫感，不做软萌卡通。
```

## 已知偏差（未解决，如实记录）

- 脸部比 v3 组图另外四位略"干净/细腻"——单图 1024² 渲染 vs 组图小尺寸渲染的固有差异；线稿/配色/材质处理同源。**零漂移版本**需等服务恢复后重发五图组图（`Tools/visual_brief/prompts/enemy_proportion_lineup_v4_1021.*`，把第 5 位输入换成这张身份图），再从组图裁出。**词已备好，未提交。**
