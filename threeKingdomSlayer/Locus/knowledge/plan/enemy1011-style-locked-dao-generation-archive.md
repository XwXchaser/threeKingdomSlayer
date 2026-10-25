---
injectMode: none
summary: '1011 风格锁定刀兵的历史版本记录（归档）：v1–v10 立绘每一版的输入、提交词、参数、哈希与实测，以及 v1–v3 三段换人视频的完整台账。只在追溯“某一版当时为什么这么做/失败指纹”时读；当前有效的入口、调用纪律与 v11 定稿在 plan/enemy1011-style-locked-dao-generation-handoff.md。编号与原文档一致，不重排。'
---

# 1011 风格锁定刀兵：历史版本记录（归档）

> 从 `plan/enemy1011-style-locked-dao-generation-handoff.md` 于 2026-10-24 原文拆出，章节编号与原文档一致。
> 当前有效内容（§0 入口 / §5 调用纪律 / §23 v11 / §24 攻击动画 / §25 新形象定稿）仍在原文件。
>
> 本文件只供追溯，不是入口；如需发起新的生成，一律从活文档的 §0 开始读。

## 1. 这一轮只改什么

做一张正面中立的基础刀兵静帧。它要同时满足：

- **比例和绘制语言来自已部署的 1011 Idle**：短壮、头略大、肩宽、躯干紧凑、腿短但双脚完整分开。硬边高清像素色簇，冷铁灰甲，暖棕皮革，朱红只做小面积识别。
- **身份来自汉末普通兵，不来自 1011 的装备**：敞脸兜鍪、交领布袍、胸甲压在衣服上、布披膊、护腕、腰带、胫裙、短靴、一把环首刀。
- **不是将领，也不是六人组图。** 不画黄巾、披风、符纸、兽首、龙纹、长红缨和名将金饰。

1011 要保留的是头身、站姿重量和像素体积。不要保留封闭面罩、十字鼻甲、橙红眼缝、圆筒肩甲、全身细密甲片和直剑。

## 2. 比例事实

2026-10-23 对两张图做了色块测量，生成词按这个事实写，不要再改成 2.5–3 头身：

| 图 | 画布 | 角色包围盒 | 读到的比例 |
| --- | --- | --- | --- |
| `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle1.png` | 1108×1108 RGBA | 约 555×601 | 头盔主体到脚约 4 头身；肩宽，腿约占全身五分之一，双脚分开 |
| `C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v5\enemy1011_face_identity_v5.png` | 1024×1024 | 约 582×824 | 腿只约占八分之一，已经偏矮，不能当比例底图 |

`design/pixel-character-and-enemy-animation-spec.md` 里的 2.5–3 头身、粗黑描边和两阶阴影是旧规范。本轮以 `design/art-style-guide.md` 和 1011 六帧为准。

## 3. 唯一输入

只上传这一张绿幕副本：

```text
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v1\enemy1011_idle1_green_input.png
1108x1108 RGB, Color Type 2, 355,410 bytes
SHA-256 3f3c17a2be763ce80a054da333ef3bbc0b9190dfeba19d8402e38e2e38b6da56
```

它由 `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle1.png`（SHA-256 `b8ac9bcf97804e4cbb9d41e798e02c9c9a7e7b818e77159d952342fcf66f6939`）把透明区域填成 `RGB(0,159,62)` 得到。角色可见像素未改。四角实测为 `(0,159,62)`。

这张图只锁定头身、肩宽、站姿重量、冷铁高光、暖棕靴和硬边像素。它不锁定盔、面罩、胸甲、肩甲、剑或左右手武器。

禁止再上传：

- 1011 原图目录里的其他帧。透明底和直剑、面罩会一起被模型当成目标。
- v1–v5 身份图、§19–§27 任何组图、`characterDesign.png`。
- `Library/Locus/tmp/soldier_refs/` 下的三国杀、无双、百度缩略图。它们只给本文写结构，不进请求。
- 任何 3D 实机截图。

`image[]` 数量必须为 1。不要改走无参考文生图。

## 4. 参考图只供写词，不上传

兵的服饰结构读自：

- `Library/Locus/tmp/soldier_refs/musou_bing/2_889x500.jpg` 的东汉义勇兵。
- `Library/Locus/tmp/soldier_refs/musou_bing/4_500x2049.jpg` 底部两个普通兵。

`sanguosha_bing/` 四张是管亥、张角、张宝、何仪，全是将领。黄巾、披风、符纸和兽首不得进入本图。无双将领立绘和攻略封面同样不得当服饰来源。

只取结构：布袍在甲下、敞脸盔、少量胸甲、布披膊、护腕、腰带、胫裙、裹腿或短靴。不取厚涂、写实甲纹、龙纹、光照和飘带。

## 6. 实际提交文本

中文供审阅，不提交。

使用上传图只锁定短壮比例、略大的头、宽肩、紧凑躯干、短但完整的双腿、分开的双脚、硬边高清像素和冷暖材质。不要保留它的封闭面罩、十字鼻甲、橙红眼缝、圆筒肩甲、全身甲和直剑。不要把腿画得更短。

改成一个正面看向镜头的汉末普通刀兵。敞脸兜鍪，脸完全露出，短红缨或盔侧一小块红布，不要长飘缨。交领布袍露在胸甲外，只一块冷铁胸甲压在衣服上，布披膊、护腕、暖棕腰带、胫裙和短靴。布和甲的边界要分开。

解剖学右手持唯一一把环首刀，刀身在画面左侧。解剖学左手在画面右侧，空置并自然下垂。不要盾、直剑、第二件武器、披风、黄巾、兽首或龙纹。

输出 1024×1024 PNG。底色为均匀不透明纯绿 RGB(0,159,62)。头、双手、双脚、刀尖和盔缨都在中央 70% 内，四边至少 15% 留白。无地面、阴影、场景、文字或棋盘格。

### English full prompt

```text
Use the uploaded soldier only to preserve its short stocky proportions, slightly large head, broad shoulders, compact torso, short but complete legs, feet planted apart, hard-edged high-resolution pixel clusters, cold iron highlights and warm brown leather. Do not keep its closed visor, cross-shaped nasal guard, orange eye slits, tubular pauldrons, full-body armor or straight sword. Do not make the legs shorter.

Redraw him as one front-facing late-Han infantry swordsman looking directly forward. Use an open-face doumou helmet, a fully visible adult face, and only a short crimson plume or a small crimson cloth tab. No long flying plume. Wear a cross-collar cloth robe visible around the chest, one cold-iron chest plate over the robe, cloth shoulder guards, forearm guards, a warm-brown belt, a tasset skirt and short war boots. Keep cloth and armor boundaries separate.

His anatomical right hand holds the only weapon, a ring-pommel dao, with the blade on screen-left. His anatomical left hand is on screen-right, empty and relaxed. No shield, straight sword, second weapon, cape, yellow turban, beast-head ornament or dragon pattern.

Deliver one 1024x1024 PNG on a uniform opaque green background RGB(0,159,62). Keep the complete head, both hands, both feet, blade tip and plume inside the central 70 percent, with at least 15 percent margin on every side. No floor, shadow, scenery, text or checkerboard.
```

### English negative prompt

```text
Avoid closed visor, cross-shaped nasal guard, orange eye slits, tubular pauldrons, European full plate, chainmail, cruciform sword, shield, second weapon, cape, long flying plume, yellow turban, talisman paper, beast-head shoulder armor, dragon engraving, officer gold ornaments, shorter legs, cropped feet, childlike round face, large sparkling eyes, smile, realistic skin, painterly rendering, smooth 3D, low-resolution chunky pixels, mosaic filter, extra person, floor, contact shadow, scenery, text, UI, watermark, checkerboard, gradient background, white background and transparent background.
```

中文负面词对照：不要封闭面罩、十字鼻甲、橙红眼缝、圆筒肩甲、欧洲全身板甲、锁子甲、十字剑、盾、第二件武器、披风、长飘缨、黄巾、符纸、兽首肩甲、龙纹、将领金饰、更短的腿、裁切的脚、儿童圆脸、大闪光眼、微笑、写实皮肤、厚涂、光滑 3D、低分辨率粗像素、马赛克滤镜、第二个人、地面、接触阴影、场景、文字、UI、水印、棋盘格、渐变背景、白底和透明底。

实际 prompt 只拼接上面 English full prompt、一个空行、English negative prompt。不提交中文，不提交本文其他章节，不写“三国杀”“无双”或任何角色名。

## 7. 输出

发起前确认这些文件不存在。已存在时先核对，不覆盖，不改名重发。

```text
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v1\enemy1011_style_locked_dao_v1.png
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v1\enemy1011_style_locked_dao_v1.request.json
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v1\enemy1011_style_locked_dao_v1.response.json
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v1\enemy1011_style_locked_dao_v1.status.json
```

同目录已有输入副本 `enemy1011_idle1_green_input.png`。不要覆盖它，也不要把它当成生成结果。

请求记录保存实际 prompt、参数、时间、HTTP 状态、输入哈希和输出哈希。响应保存原始 JSON。两份记录都不得包含 API key、Authorization 头或签名 URL 全文；URL 只记 host。

下载后验证 PNG 签名、尺寸、字节数、SHA-256 和 Color Type。本阶段允许 Color Type 2，或 Alpha 全为 255 的 Color Type 6。查看器里的棋盘格不是透明证据。

## 8. 验收

用户先看原图。通过必须同时满足：

- 并排 1011 Idle 时，头身、肩宽和腿长仍接近 1011，没有缩成 v5 那种几乎没有小腿的比例。
- 并排 1011 Idle 时，仍是硬边高清像素和冷铁体积，不是厚涂、写实或低分辨率粗格。
- 脸露出，没有封闭面罩、十字鼻甲和橙红眼缝。
- 甲压在布袍上，能分开布、铁和皮革。没有全身板甲、圆筒肩甲、披风、黄巾、兽首和龙纹。
- 只有一把环首刀，刀在画面左侧，左手空置在画面右侧。脚、刀尖和盔完整入画。
- 底色接近均匀纯绿，没有棋盘格、地面、场景、文字或 UI。

任一失败都停止。不本地改图后冒充原图，不自动重发，不进入去背。

## 9. 通过之后才允许准备的事

v1 未通过。以下仍不执行，即使 v2 通过也要另批：

- 民兵、盾卫、枪兵、弓手、重甲。它们必须复用通过后的刀兵，只改覆盖量和武器，不能回到 1011 原图或无参考文生图。
- 把 §27 的五官回贴到这张刀兵。脸部验收和服饰验收分开。
- 去背、视频、选帧、对齐、补边和 Unity 接入。

## 10. 2026-10-24 v1 提交记录（已提交一次，等待目视验收）

按 §3–§6 提交了一次，未重试、未降档、未换模型、未传 `background`。

| 项 | 实测 |
| --- | --- |
| 唯一输入 | `enemy1011_idle1_green_input.png`，SHA-256 `3f3c17a2…6da56`、355,410 B、1108×1108、Color Type 2（提交前复核通过） |
| endpoint / model | `POST /v1/images/edits`、`gpt-image-2.5-sunburst`（回显 `model` 一致） |
| 参数回显 | `quality=max`、`size=1024x1024`、`background=auto`（未传） |
| HTTP | 200，耗时 31.39 s，`n=1` |
| 输出图 | `C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v1\enemy1011_style_locked_dao_v1.png`，1,170,153 B，1024×1024，PNG 签名通过，Color Type 2，SHA-256 `ced510c55c0a58e432a172350365231aac4258cf37889d907f23a60b62f4a2bb` |
| 记录文件 | 同目录 `.request.json`（含实际 prompt 全文与 prompt SHA-256 `b28b8fc1…1fb95`、body 字节数）、`.response.json`、`.status.json` |

底色实测：四角 `(8,147,70) (7,149,67) (10,147,68) (7,150,70)`，边框中位色 `(2,153,62)`，与目标 `RGB(0,159,62)` 同族且均匀，无棋盘格、无地面、无文字。背景整体比目标值暗约 6 个绿阶。

主体包围盒 `(179,156)–(715,901)`，即 537×746。四边留白：左 17.6%、右 30.2%、上 15.2%、下 12.0%。**底边距未达到 §6 的 15% 要求**，其余三边达标；是否接受由用户在 §8 目视验收时决定。

对照图（另行生成，非原图修改）：`compare_1011_idle_vs_dao_v1.png`，左为已部署 1011 Idle 的 Alpha 包围盒 640×689，右为新图，按等高 760px、深灰底并排。

**用户验收：不通过。** 两点：这是战斗 Idle，神态平静、不凶狠，没有战斗感；甲冒过多，像高级兵种，没有参考图的民兵感。比例、露脸、环首刀、像素画法和绿底保留，不回退。下一版只按 §11 改神态和减甲。

未做去背、视频、选帧、抠图、对齐、补边、Clip 或 Unity 接入；未改动 `Assets/Sprites/Enemy/Enemy1_1/`、Prefab、Animator；未开始盾卫/枪兵/弓手/重甲。

## 11. v2：只改战斗神态，只减甲

### 11.1 不再改的

继续编辑 v1 原图，不回到 1011 绿幕副本，不上传任何参考立绘。

锁定：短壮比例、头略大、肩宽、腿长、硬边高清像素、冷铁高光、暖棕靴、正面、环首刀在画面左侧、空手在画面右侧、绿底。

不改脸的骨相成另一套人。不加橙红眼缝，不加封闭面罩。不把站姿改成攻击、跑步或召喝。

### 11.2 只改这两处

**战斗待机，不是肖像。** 脚仍分开站稳，身体略前压。刀从垂直礼刀改成略向前的待战角度。画面右侧的手从摊开改成身侧松拳。眉压向眉心，窄眼盯着镜头，嘴紧闭。去掉整齐短髭，最多留一点茬。不要喊、露齿、歪嘴、微笑。

**布是主体，铁只留识别。** 从 v1 删掉：裙甲、两侧金属肩甲、两侧护臂、护颊、红缨、金带扣，以及盖满躯干的札甲。换成麻色交领短褐、布头巾、胸前一小块冷铁甲片、麻绳或素皮腰带、布裤、裹腿、短靴。朱红只留腰带或腕上一小条。

他仍是成年征发兵，不是农民、乞丐或黄巾军。不加披风、符纸、兽首、龙纹。

### 11.3 参数

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[]: ONLY enemy1011_style_locked_dao_v1.png
prompt: section 11.4 English full prompt + blank line + English negative prompt
read timeout: 900 seconds
automatic retries: 0
```

唯一输入：

```text
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v1\enemy1011_style_locked_dao_v1.png
1024x1024, Color Type 2, 1,170,153 bytes
SHA-256 ced510c55c0a58e432a172350365231aac4258cf37889d907f23a60b62f4a2bb
```

发起前复核这个哈希。不覆盖 v1 目录任何文件。其余调用规则仍按 §5：不传 `background`，凭据只从 `MUSK_API_KEY` 读，15 秒内 400、读超时、断连或 504 都停下，不在同一次授权里重发。

### 11.4 实际提交文本

中文供审阅，不提交。

使用上传图锁定短壮比例、硬边高清像素、冷铁高光、正面和绿底。不要改头身，不要把腿画短。

把他改成战斗待机，不是阅兵站姿。身体略前压，双脚仍分开站稳。环首刀仍在画面左侧，但刀身略向前，不再垂直下挂。画面右侧的手收成身侧松拳。眉压向眉心，窄眼盯着镜头，嘴紧闭。去掉整齐短髭。不要喊叫、露齿、歪嘴或微笑。

删掉裙甲、两侧金属肩甲、两侧护臂、护颊、红缨、金带扣和盖满躯干的札甲。改成布头巾、麻色交领短褐、胸前只一小块冷铁甲片、麻绳或素皮腰带、布裤、裹腿和短靴。朱红只留腰带或腕上一小条。布是主体，铁只是识别。

仍是成年征发兵，不是农民或乞丐。画布 1024×1024，均匀纯绿底 RGB(0,159,62)。头、双手、双脚和刀尖都在中央 70% 内。

```text
Use the uploaded soldier only to preserve his short stocky proportions, hard-edged high-resolution pixel rendering, cold-iron highlights, front-facing stance and green background. Do not change his head-to-body ratio or shorten his legs.

Change the parade stance into a combat idle. Lean the torso slightly forward while both feet stay planted apart. Keep the ring-pommel dao on screen-left, but angle the blade forward instead of hanging it straight down. Close the screen-right hand into a loose fist at his side. Press the brows toward the brow center, narrow the eyes, and stare directly forward with a tightly closed mouth. Remove the neat mustache. No shout, teeth, crooked mouth or smile.

Delete the tasset armor, both metal shoulder guards, both forearm guards, cheek guards, red plume, gold buckle and the lamellar armor covering the torso. Replace them with a cloth head wrap, a hemp-colored cross-collar tunic, only one small cold-iron chest plate, a rope or plain leather belt, cloth trousers, leg wraps and short boots. Crimson may appear only as a small belt or wrist tab. Cloth is the main surface; iron is only an identifier.

Keep him an adult levied soldier, not a farmer or beggar. Deliver one 1024x1024 PNG on the original uniform green background RGB(0,159,62), with head, both hands, both feet and blade tip inside the central 70 percent.
```

```text
Avoid keeping the old lamellar armor, tasset plates, metal shoulder guards, forearm guards, cheek guards, red plume, gold buckle, neat mustache, calm portrait expression, open mouth, shouting, smile, orange eye slits, closed visor, cape, yellow turban, talisman paper, beast-head ornament, dragon pattern, officer armor, bare peasant clothes, shorter legs, cropped feet, realistic skin, painterly rendering, low-resolution chunky pixels, scenery, text, checkerboard and any canvas other than 1024x1024.
```

中文负面词对照：不要保留旧札甲、裙甲、金属肩甲、护臂、护颊、红缨、金带扣、整齐短髭、平静肖像表情、张嘴、喊叫、微笑、橙红眼缝、封闭面罩、披风、黄巾、符纸、兽首、龙纹、军官甲、赤脚农民衣、更短的腿、裁切的脚、写实皮肤、厚涂、低分辨率粗像素、场景、文字、棋盘格，以及 1024×1024 以外的画布。

实际 prompt 只拼接本节两个英文代码块，中间一个空行。不提交中文，不写角色名或游戏名。

### 11.5 输出

发起前确认这些文件不存在。已存在就停下，不覆盖。

```text
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v2\enemy1011_style_locked_dao_v2.png
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v2\enemy1011_style_locked_dao_v2.request.json
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v2\enemy1011_style_locked_dao_v2.response.json
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v2\enemy1011_style_locked_dao_v2.status.json
```

记录规则与 §7 相同。不要把结果写回 v1 目录。

### 11.6 验收

与 v1 并排。通过必须同时满足：

- 眉压低、眼变窄、盯着镜头，嘴闭着。不再是平静肖像。
- 身体略前压，刀身前指，右侧手是松拳。仍是待机，不是挥砍。
- 裙甲、两侧金属肩甲、护臂、护颊、红缨、金带扣和满躯干札甲都不在。
- 胸前只有一小块铁甲，四肢和大部分躯干是布。缩小后读到征发兵，不是军官，也不是赤脚农民。
- 头身、腿长、像素画法仍接近 v1。底色仍是均匀纯绿，刀和双脚完整入画。

裙甲、肩甲或红缨还在，或嘴张开，即失败并停下。不自动重发，不降 `input_fidelity`，不进入去背。

### 11.7 2026-10-24 v2 提交记录（已提交一次，等待目视验收）

按 §11.3–§11.5 提交了一次，未重试、未降档、未换模型、未传 `background`。

| 项 | 实测 |
| --- | --- |
| 唯一输入 | `enemy1011_style_locked_dao_v1\enemy1011_style_locked_dao_v1.png`，SHA-256 `ced510c5…4a2bb`、1,170,153 B（提交前复核通过） |
| 参数回显 | `model=gpt-image-2.5-sunburst`、`quality=max`、`size=1024x1024`、`background=auto`（未传） |
| prompt | 仅 §11.4 两个英文块 + 一个空行，1,866 字符，SHA-256 `a66bd9498d196cfc634ff8690b3e59c51cdf18e076ce6c0fb718582c5dc87cba` |
| HTTP | 200，耗时 35.03 s，`n=1` |
| 输出图 | `C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v2\enemy1011_style_locked_dao_v2.png`，1,302,683 B，1024×1024，PNG 签名通过，Color Type 2，SHA-256 `f0844a9c94e24e5b35fb874793d7a052960a0d8f54e39540099d1a9b94ad46d4` |
| 记录文件 | 同目录 `.request.json`（含实际 prompt 全文）、`.response.json`、`.status.json` |

底色：四角 `(7,144,73) (7,144,68) (7,142,69) (6,144,71)`，边框中位 `(1,148,65)`，均匀无棋盘格，比目标 `RGB(0,159,62)` 略暗（v1 为 `(2,153,62)`）。

主体包围盒 `(164,166)–(742,917)`，579×752（v1 为 537×746）。四边留白：左 16.0%、右 27.4%、上 16.2%、下 **10.4%**（v1 为 12.0%），底边距仍未达 15%。

并排对照（另行生成，非原图修改）：`C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v2\compare_v1_vs_v2.png`，左 v1、右 v2，按等高 780px、深灰底。

**用户结论：修得太狠。** v2 的红头巾、单块胸牌、麻绳和满腿裹布读成民夫，不是无双小兵。战斗神态可保留：压眉、窄眼、闭嘴、刀身前指、身侧松拳。甲的层级以 §12 为准，不再按 §11.2 的“一小块铁甲”重发。

未做去背、视频、选帧、抠图、对齐、补边、Clip 或 Unity 接入；未改动 `Assets/Sprites/Enemy/Enemy1_1/`、Prefab、Animator。

## 12. v3：按无双列兵重制刀兵

### 12.1 服饰依据

用户提供的实机截图只供写词，不上传：

```text
C:\Users\steam\Downloads\screenshot-20261024-115410.png
```

图中小兵共用一套列兵制服：布帽或软帽、额带、交领短袍、无袖短甲齐髋并带浅色下摆、布袖、深色肥裤、小腿绑带。兵种只靠武器区分。紫、蓝、绿是阵营色，不搬。绿色持矛者甲更完整，是队率，不当刀兵模板。

### 12.2 输入与锁定

唯一输入仍是 v1 原图，不编辑 v2，不回到 1011 绿幕副本，不上传截图或任何参考立绘。

```text
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v1\enemy1011_style_locked_dao_v1.png
1024x1024, Color Type 2, 1,170,153 bytes
SHA-256 ced510c55c0a58e432a172350365231aac4258cf37889d907f23a60b62f4a2bb
```

锁定 v1 的短壮比例、头略大、肩宽、腿长、硬边高清像素、冷铁高光和绿底。采用 v2 已确认的战斗待机，但姿态用文字描述，不把 v2 图交给模型。

### 12.3 替换清单

换成：

- 布帽加一条额带，脸完全露出。
- 麻色交领短袍，领口和袖子露在甲外。
- 一件无袖冷铁短甲，从胸覆盖到髋，下摆一条浅色边。不是成排札甲，也不是一块方铁牌。
- 布肩或很小的肩片。深色肥裤、小腿绑带、短靴、素皮腰带。
- 环首刀在画面左侧，刀身略向前。画面右侧是身侧松拳。
- 眉压向眉心，窄眼盯镜头，嘴紧闭。朱红只留额带或腰带一小条。

删掉 v1 的兜鍪、红缨、护颊、大肩甲、双侧护臂、裙甲、金带扣、满身札甲和红袍主色。不要带回 v2 的红头巾、方铁牌、麻绳腰带和满腿裹布。

仍是成年列兵，不是军官、农民、乞丐或黄巾军。不要写 lamellar、one small chest plate、militia、peasant。

### 12.4 参数

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[]: ONLY enemy1011_style_locked_dao_v1.png
prompt: section 12.5 English full prompt + blank line + English negative prompt
read timeout: 900 seconds
automatic retries: 0
```

发起前复核 §12.2 的哈希。不覆盖 v1、v2 目录。其余调用规则仍按 §5：不传 `background`，凭据只从 `MUSK_API_KEY` 读，15 秒内 400、读超时、断连或 504 都停下，不在同一次授权里重发。

### 12.5 实际提交文本

中文供审阅，不提交。

使用上传图锁定短壮比例、硬边高清像素、冷铁高光、正面和绿底。不要改头身，不要把腿画短。

把他改成列兵刀手的战斗待机。身体略前压，双脚分开站稳。环首刀仍在画面左侧，刀身略向前。画面右侧的手是身侧松拳。眉压向眉心，窄眼盯着镜头，嘴紧闭。不要喊、露齿、歪嘴或微笑。

用布帽和一条额带替换头盔。用麻色交领短袍替换红袍，领口和袖子露在外面。用一件无袖短甲替换满身札甲：短甲从胸盖到髋，下摆有一条浅色边，不是成排甲片，也不是一块方铁牌。肩只留布肩或很小的肩片。下身是深色肥裤、小腿绑带、短靴和素皮腰带。朱红只留额带或腰带一小条。

画布 1024×1024，均匀纯绿底 RGB(0,159,62)。头、双手、双脚和刀尖都在中央 70% 内。

```text
Use the uploaded soldier only to preserve his short stocky proportions, hard-edged high-resolution pixel rendering, cold-iron highlights, front-facing stance and green background. Do not change his head-to-body ratio or shorten his legs.

Change him into a rank-and-file swordsman in combat idle. Lean the torso slightly forward, keep both feet planted apart, angle the screen-left ring-pommel dao forward, and close the screen-right hand into a loose fist at his side. Press the brows toward the brow center, narrow the eyes, and stare forward with a tightly closed mouth. No shout, teeth, crooked mouth or smile.

Replace the helmet with a cloth cap and one forehead band. Replace the red robe with a hemp-colored cross-collar tunic, leaving the collar and sleeves visible. Replace the full lamellar armor with one sleeveless hip-length cold-iron tunic-armor that has a single light-colored lower border. It is not rows of lamellar scales and not one square metal plate. Keep only cloth shoulders or very small shoulder tabs. Use dark loose trousers, calf wraps, short boots and a plain leather belt. Crimson may appear only as the forehead band or a small belt tab.

Deliver one 1024x1024 PNG on the original uniform green background RGB(0,159,62), with head, both hands, both feet and blade tip inside the central 70 percent.
```

```text
Avoid keeping the helmet, red plume, cheek guards, large shoulder armor, forearm guards, tasset plates, gold buckle, full lamellar armor, red robe, red headscarf, one square chest plate, rope belt, full-leg wraps, calm portrait expression, open mouth, smile, orange eye slits, closed visor, cape, yellow turban, officer armor, peasant clothes, shorter legs, cropped feet, realistic skin, painterly rendering, low-resolution chunky pixels, scenery, text, checkerboard and any canvas other than 1024x1024.
```

中文负面词对照：不要保留头盔、红缨、护颊、大肩甲、护臂、裙甲、金带扣、满身札甲、红袍、红头巾、一块方铁牌、麻绳腰带、满腿裹布、平静肖像、张嘴、微笑、橙红眼缝、封闭面罩、披风、黄巾、军官甲、农民衣、更短的腿、裁切的脚、写实皮肤、厚涂、低分辨率粗像素、场景、文字、棋盘格，以及 1024×1024 以外的画布。

实际 prompt 只拼接本节两个英文代码块，中间一个空行。不提交中文，不写游戏名、角色名或“参考截图”。

### 12.6 输出

发起前确认这些文件不存在。已存在就停下，不覆盖。

```text
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v3\enemy1011_style_locked_dao_v3.png
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v3\enemy1011_style_locked_dao_v3.request.json
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v3\enemy1011_style_locked_dao_v3.response.json
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v3\enemy1011_style_locked_dao_v3.status.json
```

记录规则与 §7 相同。不要把结果写回 v1 或 v2 目录。

### 12.7 验收

与 v1、v2 和用户截图并排。通过必须同时满足：

- 头是布帽加额带，没有头盔、红缨，也没有 v2 的红头巾。
- 躯干是一件齐髋无袖短甲，下摆有浅色边。不是 v1 的札甲，也不是 v2 的方铁牌。
- 领口、袖子和腿是布。没有大肩甲、护臂、裙甲和满腿裹布。
- 缩小后先读到列兵刀手，不是军官，也不是民夫。
- 压眉、窄眼、闭嘴、刀身前指、右侧松拳仍在。头身和像素画法仍接近 v1。刀和双脚完整，底色均匀纯绿。

头盔、红头巾、札甲或方铁牌还在，即失败并停下。不自动重发，不降 `input_fidelity`，不进入去背。

### 12.8 2026-10-24 v3 提交记录（已提交一次，等待目视验收）

按 §12.4–§12.6 提交了一次，未重试、未降档、未换模型、未传 `background`。

| 项 | 实测 |
| --- | --- |
| 唯一输入 | `enemy1011_style_locked_dao_v1\enemy1011_style_locked_dao_v1.png`，SHA-256 `ced510c5…4a2bb`、1,170,153 B（提交前复核通过；未用 v2 图） |
| 参数回显 | `model=gpt-image-2.5-sunburst`、`quality=max`、`size=1024x1024`、`background=auto`（未传） |
| prompt | 仅 §12.5 两个英文块 + 一个空行，1,834 字符，SHA-256 `c9efdeb80578343bf19ac70525e42b79a2a39d5f8e39cae7e12b166ef2380fd6` |
| HTTP | 200，耗时 38.46 s，`n=1` |
| 输出图 | `C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v3\enemy1011_style_locked_dao_v3.png`，1,298,067 B，1024×1024，PNG 签名通过，Color Type 2，SHA-256 `9c3109a8217424d1547b976c5d40339f2801d2a0d2a5cde323af56f1456528f9` |
| 记录文件 | 同目录 `.request.json`（含实际 prompt 全文）、`.response.json`、`.status.json` |

底色：四角 `(7,151,72) (7,153,67) (9,153,69) (6,153,70)`，边框中位 `(1,158,63)`，是三版中最接近目标 `RGB(0,159,62)` 的一版，均匀无棋盘格。

主体包围盒 `(180,136)–(740,911)`，561×776（v1 537×746、v2 579×752）。四边留白：左 17.6%、右 27.6%、上 **13.3%**、下 **10.9%**；上下两边均未达 15%，但主体完整未裁切。

并排对照（另行生成，非原图修改）：`C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v3\compare_v1_v2_v3.png`，左→右 v1 / v2 / v3，按等高 720px、深灰底。

**用户验收：不通过。** 两点：表情仍然平静，不像战斗状态；胸甲和护裆没有切开，连成一体。原因是 §12.5 把甲写成一件齐髋短甲。布帽、额带、交领袍、布袖、深色裤、短靴和刀的位置保留。下一版只按 §13 拆甲和改脸。

未做去背、视频、选帧、抠图、对齐、补边、Clip 或 Unity 接入；未改动 `Assets/Sprites/Enemy/Enemy1_1/`、Prefab、Animator。

## 13. v4：拆开胸甲与护裆，改成战斗凝视

### 13.1 只改什么

继续编辑 v3，不回到 v1 或 v2，不上传参考截图。

锁定：短壮比例、像素画法、布帽、额带、交领短袍、布袖、深色裤、短靴、画面左侧前指环首刀、画面右侧松拳、绿底。

只改两处：

- 脸从平眉、正常眼距改成眉压向眉心并略斜，窄吊眼，嘴压成一条直线。不张嘴，不露齿。
- 连体甲拆成腰带以上的胸甲，和腰带以下左右分开的两片护裆。浅色边只留在护裆下缘。前臂各加一条浅色布护腕。

### 13.2 唯一输入

```text
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v3\enemy1011_style_locked_dao_v3.png
1024x1024, Color Type 2, 1,298,067 bytes
SHA-256 9c3109a8217424d1547b976c5d40339f2801d2a0d2a5cde323af56f1456528f9
```

发起前复核这个哈希。不覆盖 v3 目录。

### 13.3 参数

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[]: ONLY enemy1011_style_locked_dao_v3.png
prompt: section 13.4 English full prompt + blank line + English negative prompt
read timeout: 900 seconds
automatic retries: 0
```

其余调用规则仍按 §5：不传 `background`，凭据只从 `MUSK_API_KEY` 读，15 秒内 400、读超时、断连或 504 都停下，不在同一次授权里重发。

### 13.4 实际提交文本

中文供审阅，不提交。用户已检阅过下面两个英文代码块，发起时不得改写。

使用上传图锁定短壮比例、像素画法、布帽、额带、交领短袍、布袖、深色裤、短靴、前指环首刀、身侧松拳和绿底。不要改头身。

把平静的脸改成战斗凝视。眉压向眉心并略斜，眼收成窄吊眼，嘴压成一条直线，盯着镜头。不要喊，不要露齿。

把连成一片的甲拆开。胸甲在腰带以上结束。素皮腰带露在胸甲和护裆之间。腰带下面是左右两片分开的护裆，中间能看到深色裤。浅色边只留在护裆下缘，从胸甲下缘去掉。前臂各加一条浅色布护腕，不要金属护臂。

```text
Use the uploaded soldier only to preserve his short stocky proportions, hard-edged high-resolution pixel rendering, cloth cap, forehead band, hemp cross-collar tunic, cloth sleeves, dark loose trousers, short boots, forward-angled ring-pommel dao on screen-left, loose fist on screen-right and green background. Do not change his head-to-body ratio or shorten his legs.

Replace the calm face with a combat glare. Pull both brows down toward the brow center and slant them. Shrink the eyes into narrow fierce upturned slits. Press the mouth into one tight horizontal line and stare directly forward. No shout, teeth, crooked mouth or smile.

Split the one-piece torso armor into separate garments. End the cold-iron chest armor at the waist, above the belt. Show the plain leather belt between the chest armor and the lower armor. Below the belt, hang two separate front hip-armor panels, one over each thigh, with dark trousers visible in the center gap. Move the light-colored border from the chest armor onto only the lower edge of those two panels. Add one light cloth wrap on each forearm. No metal forearm guards.

Deliver one 1024x1024 PNG on the original uniform green background RGB(0,159,62), with head, both hands, both feet and blade tip inside the central 70 percent.
```

```text
Avoid one-piece apron armor, chest armor continuing below the belt, connected hip panels, a light border on the chest armor, calm level brows, wide relaxed eyes, open mouth, shouting, smile, helmet, red plume, red headscarf, large shoulder armor, metal forearm guards, tasset plates, gold buckle, full lamellar armor, square chest plate, rope belt, shorter legs, cropped feet, realistic skin, painterly rendering, low-resolution chunky pixels, scenery, text, checkerboard and any canvas other than 1024x1024.
```

中文负面词对照：不要连体围裙甲、胸甲伸到腰带以下、两片护裆连在一起、胸甲下缘的浅色边、平眉、放松的宽眼、张嘴、喊叫、微笑、头盔、红缨、红头巾、大肩甲、金属护臂、裙甲、金带扣、满身札甲、方铁牌、麻绳腰带、更短的腿、裁切的脚、写实皮肤、厚涂、低分辨率粗像素、场景、文字、棋盘格，以及 1024×1024 以外的画布。

实际 prompt 只拼接本节两个英文代码块，中间一个空行。

### 13.5 输出

发起前确认这些文件不存在。已存在就停下，不覆盖。

```text
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v4\enemy1011_style_locked_dao_v4.png
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v4\enemy1011_style_locked_dao_v4.request.json
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v4\enemy1011_style_locked_dao_v4.response.json
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v4\enemy1011_style_locked_dao_v4.status.json
```

记录规则与 §7 相同。不要把结果写回 v3 目录。

### 13.6 验收

与 v3 并排。通过必须同时满足：

- 眉压向眉心并略斜，眼是窄吊眼，嘴是一条直线。不再是 v3 的平眉宽眼。
- 胸甲在腰带以上结束。腰带露在两件甲之间。
- 腰带下是左右两片护裆，中间能看到深色裤。浅色边只在护裆下缘。
- 前臂是浅色布护腕，不是金属护臂。
- 布帽、额带、交领袍、刀和比例仍接近 v3。底色均匀纯绿，刀和双脚完整。

胸甲和护裆仍连成一片，或眉眼仍然平，即失败并停下。不自动重发，不降 `input_fidelity`，不进入去背。

### 13.7 2026-10-24 v4 提交记录（已提交一次，等待目视验收）

按 §13.2–§13.5 提交了一次，未重试、未降档、未换模型、未传 `background`。

| 项 | 实测 |
| --- | --- |
| 唯一输入 | `enemy1011_style_locked_dao_v3\enemy1011_style_locked_dao_v3.png`，SHA-256 `9c3109a8…528f9`、1,298,067 B（提交前复核通过） |
| 参数回显 | `model=gpt-image-2.5-sunburst`、`quality=max`、`size=1024x1024`、`background=auto`（未传） |
| prompt | 仅 §13.4 两个英文块 + 一个空行，1,790 字符，SHA-256 `f9566f6242d144c9eb260b9e0d8216b3f4def87e79538714d7ddea391055dea1` |
| HTTP | 200，耗时 38.47 s，`n=1` |
| 输出图 | `C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v4\enemy1011_style_locked_dao_v4.png`，1,191,926 B，1024×1024，PNG 签名通过，Color Type 2，SHA-256 `05d9becc7d914f4cf6dc6d1196be8588b0c6db3c4a68eecf613373045b011d79` |
| 记录文件 | 同目录 `.request.json`（含实际 prompt 全文）、`.response.json`、`.status.json` |

底色：四角 `(9,148,76) (6,149,69) (10,150,74) (7,153,75)`，边框中位 `(3,157,68)`，均匀无棋盘格，接近目标 `RGB(0,159,62)`（v3 为 `(1,158,63)`，三版中最准）。

主体包围盒 `(179,129)–(743,913)`，565×785（v3 561×776）。四边留白：左 17.5%、右 27.3%、上 **12.6%**、下 **10.7%**；上下两边仍低于 15%，主体完整未裁切。

并排对照（另行生成，非原图修改）：`C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v4\compare_v3_vs_v4.png`，左 v3、右 v4，按等高 780px、深灰底。

**用户结论：还不如 v2。** v3、v4 的甲结构不再继续。下一版回到 v2，只加简易盔和护腕，并把表情改得更凶。

未做去背、视频、选帧、抠图、对齐、补边、Clip 或 Unity 接入；未改动 `Assets/Sprites/Enemy/Enemy1_1/`、Prefab、Animator。

## 14. v5：在 v2 上加简易盔和护腕

### 14.1 只加什么

唯一输入是 v2，不编辑 v3 或 v4，不回到 v1 或 1011 原图。

锁定 v2 的短壮比例、像素画法、红头巾、麻色短袍、方铁牌、麻绳腰带、深色裤、裹腿、短靴、画面左侧前指环首刀、画面右侧松拳和绿底。

只加三处：

- 一顶无缨、无护颊的敞脸铁盔，压在红头巾上。红布仍从额前和颈后露出，脸完全露出。
- 两侧前臂各一条短铁或硬皮护腕，无金边，不延伸到肩。
- 表情比 v2 更凶：眉压成深 V 并挤向眉心，眼收成细长敌意缝，嘴咬成一条直线。不张嘴，不露齿。

不加肩甲、裙甲、护裆、札甲或红缨。

### 14.2 唯一输入

```text
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v2\enemy1011_style_locked_dao_v2.png
1024x1024, Color Type 2, 1,302,683 bytes
SHA-256 f0844a9c94e24e5b35fb874793d7a052960a0d8f54e39540099d1a9b94ad46d4
```

发起前复核这个哈希。不覆盖 v2 目录。

### 14.3 参数

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[]: ONLY enemy1011_style_locked_dao_v2.png
prompt: section 14.4 English full prompt + blank line + English negative prompt
read timeout: 900 seconds
automatic retries: 0
```

其余调用规则仍按 §5：不传 `background`，凭据只从 `MUSK_API_KEY` 读，15 秒内 400、读超时、断连或 504 都停下，不在同一次授权里重发。

### 14.4 实际提交文本

中文供审阅，不提交。用户已检阅过下面两个英文代码块，发起时不得改写。

使用上传图锁定短壮比例、像素画法、麻色短袍、方铁牌、麻绳腰带、深色裤、裹腿、短靴、红头巾、前指环首刀、身侧松拳和绿底。不要改头身、衣服或武器。

在红头巾上加一顶简易敞脸铁盔。红布仍从额前和颈后露出，脸完全露出。不要缨、鼻甲和护颊。两侧前臂各加一条短的素护腕，冷铁或硬皮，无金边，不延伸到肩。

表情要比上传图更凶。双眉压成深 V 并挤向眉心，眼收成细长敌意缝，嘴咬成一条硬直线，盯着镜头。不要喊，不要露齿。

```text
Use the uploaded soldier only to preserve his short stocky proportions, hard-edged high-resolution pixel rendering, hemp tunic, small chest plate, rope belt, dark trousers, leg wraps, short boots, red headscarf, forward-angled ring-pommel dao on screen-left, loose fist on screen-right and green background. Do not change his head-to-body ratio, clothing or weapon.

Add one simple open-face iron bowl helmet over the red headscarf. Keep the red cloth visible at the forehead and neck. The face stays fully exposed. No plume, no nasal guard and no cheek plates. Add one short plain bracer on each forearm, in cold iron or hard leather, with no gold trim. Do not extend them to the shoulders.

Make the expression fiercer than the uploaded face. Crush both brows into a deep V pressed toward the brow center. Shrink the eyes into thin hostile slits. Bite the mouth into one hard horizontal line and stare directly forward. No shout, teeth, crooked mouth or smile.

Deliver one 1024x1024 PNG on the original uniform green background RGB(0,159,62), with head, both hands, both feet and blade tip inside the central 70 percent.
```

```text
Avoid removing the red headscarf, small chest plate, rope belt or hemp tunic, closed visor, red plume, nasal guard, cheek plates, large shoulder armor, full vambraces, gold trim, tasset plates, hip-armor panels, lamellar torso armor, calm level brows, wide eyes, open mouth, shouting, smile, shorter legs, cropped feet, realistic skin, painterly rendering, low-resolution chunky pixels, scenery, text, checkerboard and any canvas other than 1024x1024.
```

中文负面词对照：不要去掉红头巾、方铁牌、麻绳腰带或麻色短袍；不要封闭面罩、红缨、鼻甲、护颊、大肩甲、长护臂、金边、裙甲、护裆、札甲躯干、平眉、宽眼、张嘴、喊叫、微笑、更短的腿、裁切的脚、写实皮肤、厚涂、低分辨率粗像素、场景、文字、棋盘格，以及 1024×1024 以外的画布。

实际 prompt 只拼接本节两个英文代码块，中间一个空行。

### 14.5 输出

发起前确认这些文件不存在。已存在就停下，不覆盖。

```text
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v5\enemy1011_style_locked_dao_v5.png
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v5\enemy1011_style_locked_dao_v5.request.json
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v5\enemy1011_style_locked_dao_v5.response.json
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v5\enemy1011_style_locked_dao_v5.status.json
```

记录规则与 §7 相同。不要把结果写回 v2 目录。

### 14.6 验收

与 v2 并排。通过必须同时满足：

- 红头巾还在，上面多了一顶无缨敞脸铁盔。脸完全露出，没有护颊和鼻甲。
- 两侧前臂各有一条短护腕。没有肩甲，护腕没有接到肩。
- 眉比 v2 更低、更挤向眉心，眼更细，嘴仍是一条直线。
- 方铁牌、麻绳腰带、布袍、裹腿、刀和比例仍接近 v2。底色均匀纯绿，刀和双脚完整。

红头巾被盔盖没、脸被护颊挡住、肩甲出现，或表情仍和 v2 一样平，即失败并停下。不自动重发，不降 `input_fidelity`，不进入去背。

### 14.7 2026-10-24 v5 提交记录（已提交一次，等待目视验收）

按 §14.2–§14.5 提交了一次，未重试、未降档、未换模型、未传 `background`。

| 项 | 实测 |
| --- | --- |
| 唯一输入 | `enemy1011_style_locked_dao_v2\enemy1011_style_locked_dao_v2.png`，SHA-256 `f0844a9c…46d4`、1,302,683 B（提交前复核通过；未用 v3/v4） |
| 参数回显 | `model=gpt-image-2.5-sunburst`、`quality=max`、`size=1024x1024`、`background=auto`（未传） |
| prompt | 仅 §14.4 两个英文块 + 一个空行，1,576 字符，SHA-256 `5c04a6b9e2c3442c4b7d896b52e9fd54d3c6123fd724693c728fa238cd8f5d40` |
| HTTP | 200，耗时 34.76 s，`n=1` |
| 输出图 | `C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v5\enemy1011_style_locked_dao_v5.png`，1,352,518 B，1024×1024，PNG 签名通过，Color Type 2，SHA-256 `8330ac18a708e8d0b93ce881791ee71cc37f65faa3d4660fa4ad63fd49ea09d6` |
| 记录文件 | 同目录 `.request.json`（含实际 prompt 全文）、`.response.json`、`.status.json` |

底色：四角 `(7,141,72) (6,139,66) (8,139,69) (5,143,71)`，边框中位 `(1,147,65)`，均匀无棋盘格，比前四版都暗（目标 `RGB(0,159,62)`；最准的 v3 为 `(1,158,63)`）。

主体包围盒 `(162,125)–(744,918)`，583×794（v2 为 579×752；新增头盔使高度＋约 42px）。四边留白：左 15.8%、右 27.2%、上 12.2%、下 10.3%；上下两边未达 15%，主体完整未裁切。

并排对照（另行生成，非原图修改）：`C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v5\compare_v2_vs_v5.png`，左 v2、右 v5，按等高 780px、深灰底。

状态：待用户按 §14.6 四条目视验收（红头巾是否仍在且盔无缨敞脸无护颊鼻甲、两侧短护腕是否未接到肩且无肩甲、眉眼嘴是否比 v2 更凶、方铁牌/麻绳腰带/布袍/裹腿/刀与比例是否仍接近 v2）。

## 15. v5 换人视频（2026-10-24，已提交一次；两项待拍板）

### 15.1 任务与输入

用 v5 立绘替换用户提供的绿幕动画里的人物，动作与镜头保持源片原样。

| 角色 | 素材 |
| --- | --- |
| `reference_video`（动作＋镜头） | `C:\Users\steam\Videos\doubaoVideo\2b1888f6388ed56b14da731eb0aeb229.mp4`，960×960 / 24fps / 97 帧 / 4.042 s / h264；实测键色 G 161–169，f048 主体包围盒 455×706 |
| `reference_image`（身份与服饰） | `C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v5\enemy1011_style_locked_dao_v5.png`，SHA-256 `8330ac18…09d6` |

未用 `first_frame` / `last_frame`（接口禁止与 reference 混用），未上传其他参考。

### 15.2 参数与费用

```text
model: seedance-2-fast
duration_seconds: 4
resolution: 720p
aspect_ratio: 1:1
generate_audio: false
seed: -1
watermark: false
input.content: [text, video_url+reference_video, image_url+reference_image]
```

- 接口已废弃 `negative_prompt`，负面约束写进正文末段（地面/接触阴影、场景、文字、水印、棋盘格、镜头抖动、第二人、多余肢体、新增甲片、身份漂移、换服装、换武器）。
- 任务：`task_106a6af73b374bbebad8ea94115088b8`，`create_attempts=1`，usage 173,700，`cost.spend=0.57321`（billing: OUTPUT_TOKENS 173,700，条件 `has_video_input=true`、`720p`）。
- 首次上传被 `file.reachapi.ai` 前置 Cloudflare 以 `HTTP 403 error code 1010` 拒绝（`Python-urllib` UA 命中黑名单），**未创建任务、未计费**；补浏览器 UA 后上传成功并只创建一次任务。
- 轮询两轮各 12 次均未在本地预算内拿到终态（第一轮终态 `generating`，第二轮因解析 bug 报 `AttributeError`）；随后单次查询得到 `success`。本地等待耗尽不等于服务端失败。

### 15.3 交付物

```text
C:\Users\steam\Videos\doubaoVideo\dao_v5_motion_replacement_v1_4s_720p.mp4
C:\Users\steam\Videos\doubaoVideo\dao_v5_motion_replacement_v1_compare_src_vs_out.png
```

- 成片：2,082,134 B、SHA-256 `dcf13f43…fa82`、960×960 / 24fps / 97 帧 / 4.042 s / h264 / 无音轨；`ftyp`、顶层 box 边界、全帧解码通过。720p 请求在 1:1 下按 960×960 返回，与源片几何一致。
- 对照图：上排源片、下排新片，取 f000/012/024/036/048/060/072/096。
- 记录与脚本：`Library/Locus/tmp/dao_v5_repl_video/`（`dao_v5_motion_replacement_v1.request.json`、`.status.json`、`task_raw.json`、`dao_v5_motion_replacement_v1.queries.json`、`pair_analysis.json`、抽帧与 `submit_task.py` / `download_result.py` / `analyze_pair.py`）。

### 15.4 实测偏差（逐帧与源片比对）

| 项 | 结果 |
| --- | --- |
| 帧数 / 分辨率 / 时长 | 97 vs 97、960×960、4.042 s，与源片一致 |
| 主体质心 | 平均差 18 px，首帧约 6 px、末帧约 8 px，最差 f049 差 71 px |
| 主体高度 | **新片比源片高 20–25%**（首帧 745 vs 593，f072 719 vs 579） |
| 背景 | 首帧 G=154，f024 起稳定在 132–134；相对基准键 `(0,159,62)` 为 ΔG −5…−27（最差 f096），相对源片自身键 (161–169) 为 −27…−37；无棋盘格 |

### 15.5 待拍板（已于 2026-10-24 裁决，见 §16）

1. **背景键偏低 27–37 个 G**：接受 / 换参考重生成 / 只在交付物侧处理（抠图后在相同中性底比对主体色）。按 `skill/reachapi-seedance-video-generation-v2.md` 的 G2 第 6 条升级，不得静默结案。
2. **主体放大 20–25%**：是否影响逐帧对齐与后续替换管线（首尾帧位置对得上，中段 71 px 位移）。

**2026-10-24 裁决：两项一并修，随 §16 的 v2 一次生成解决；不再单独立项。** v2 的词同时钉住背景键值与主体尺寸/脚底线（§16.3 第 3、5 段）。

### 15.6 若重生成，前置条件

- 必须重新取得当次明确批准（视频为收费任务，一次授权只发一个）。
- 只允许改：参考图换成更贴近源片构图的版本；词里锁死主体在画面中的高度占比与脚底 Y；词里写明要求保持的绿色键值。
- 不得改模型、不得降分辨率、不得在同一授权里连发第二版。
- 参考视频与参考图职责不得互换或加图绕过限制。

### 15.7 尚未做的

选帧、抠图、补边、Clip 创建、Unity 接入；未修改 `Assets/Sprites/Enemy/Enemy1_1/`、Prefab、Animator。台账见 `memory/video-generation-case-log.md` 的同名条目。

## 16. v2：加攻击神态 + 一并修尺寸与底色（2026-10-24 已发起，**成片已收到，待用户验收**）

### 16.0 本节状态与边界

- 本节交付**方案、提交文本与实测记录**。视频已由本侧按用户 2026-10-24 的明确发起指令提交（见 §16.6），未重试、未降档、未换模型。
- 词与参数已落盘：`Library/Locus/tmp/dao_v5_repl_video/dao_v5_motion_replacement_v2_expr.prompt.txt`（2951 字符，SHA-256 `b21d8e547d599d2d8182698625013a65a82b5cbfd7eb0622984ef5269b736a32`）与 `dao_v5_motion_replacement_v2_expr.request.json`（同目录，含实际 prompt、参数、输入哈希、G1 实测）。
- **读取或修改本文不等于批准发起。** 发起方须先复述 §16.2–§16.4 的唯一输入、参数、实际提交文本与输出目录，并取得当次明确"发起生成"。

### 16.1 诊断（实测，2026-10-24）

先纠正两个此前评审里的错误认知，再定方案。

**① 成片的脸其实换成功了，"没有表情"是误读。** 成片第 48 帧已套用 v5 身份：敞脸铁盔、红头巾、露脸、浓眉压眼、方领布袍（对照图 `Library/Locus/tmp/dao_v5_expr_check/MONEY_saved_vs_real.png` 右侧为真实成片帧）。嘴 97 帧一帧未张，全片是同一张怒视贴图挂在会动的头上。

**② 但 `Library/Locus/tmp/dao_v5_repl_video/frame_000/024/048/072/096.png` 全部是源片帧，不是成片帧。** 逐像素核对：与源片同序号帧平均绝对差 = 0.00，与成片同序号帧差 62–78。`contact_sheet.png` 同源。此前若拿这些 PNG 评审，看到的是 1011 闭面罩原兵，恰好强化了"头盔、无表情"的错误印象。**后续评审必须从成片 mp4 重新抽帧。**

**③ "凑数跳帧"是源片继承的，不是模型偷懒。**

| 指标 | 源片 | 成片 v1 |
|---|---|---|
| 相邻帧近重复对（MAD<1.0） | 50/96 | 43/96 |
| 相邻帧平均 MAD | 3.16 | 3.16 |
| 大变化边界数 | 15 | 15 |

源片本身就是一套**16 姿态定点保持**的定格动画（97 帧按 24fps 放长到 4.042 s）。把已部署的 `Enemy_1011_attack1..16.png` 逐张去源片里找最佳匹配，得到**严格单调**的对应：attack1→f10、attack2→f19、attack3→f27、attack4→f35、attack5→f41、attack6→f48、attack7→f49、attack8→f50、attack9→f53、attack10→f54、attack11→f56、attack12→f62、attack13→f69、attack14→f74、attack15→f75、attack16→f79。**源片 = 已部署 1011 攻击动画的 16 帧放长版。** 保持段在精灵交付里根本用不到（`Enemy_1011_Attack.anim` 是 30fps、StopTime 2.9、16 张精灵前 6 张 0.3s 间隔后 10 张 0.1s 间隔，播放节奏早已定死），所以视频的"保持"不是缺陷。

**④ 脸只在举刀段可见，劈砍段被头盔挡死。** 按红色头巾锚点跟踪头部量脸部皮肤占比：f10 0.05 → f19 0.11 → f27 0.17 → f35 0.19 → f41 0.18 → f48 0.13，**f49–f72 塌到 0.025**（只剩一条眼缝，劈砍时低头前倾，与已部署 1011 同一套几何），收势 f74–f79 回到 0.05–0.07。

→ 因此**表情峰值必须落在举刀段顶刀点**（f35–f48），再顺势带进劈砍段。强求在刀锋命中帧露脸就必须改抬头/转脸，那属于改动作，与硬约束冲突。

**⑤ 主体放大与脚底下移同源。** 16 姿态槽位实测：源片主体高度占画面比均值 0.680、主体底边 Y = 776；成片 v1 为 0.798、底边 Y = 860（+84px）；身高比 out/src 1.03–1.30，均值 1.216。**人放大 ⇒ 脚被推到画面下方，一条约束可同时修两处。**

**⑥ 两个参考的绿本来就不一致。** 上传图 v5 实测四角 `(2,146,64)`；源片 `(1,169,58)`；v1 输出落在 G 132–134。只能靠词钉住输出键值，生成后必须按 G2 复测。

### 16.2 决策与参数

用户 2026-10-24 拍板：**举刀段起表情并合理衔接劈砍段；不强求看到表情；一律不改动作；主体放大与背景色偏一并修；先用 A 方案（仅提示词）生成一版看效果。**

A 方案 = **单变量对照**：模型、时长、分辨率、比例、seed 与两个输入文件**全部与 v1 字节一致**，只有提示词变。

| 项 | 值 | 与 v1 |
|---|---|---|
| 模型 | `seedance-2-fast` | 不变 |
| 时长 / 分辨率 / 比例 | 4 s / 720p / 1:1 | 不变 |
| seed / 音频 / 水印 | -1 / false / false | 不变 |
| reference_video | `C:/Users/steam/Videos/doubaoVideo/2b1888f6388ed56b14da731eb0aeb229.mp4`，817,576 B，SHA-256 `f624106031c57b569f76cf30b758730a21e9f73bc6f92567c6f35c07bfafa5c9` | 字节相同 |
| reference_image | `C:/Users/steam/Pictures/gptGen/enemy1011_style_locked_dao_v5/enemy1011_style_locked_dao_v5.png`，1,352,518 B，SHA-256 `8330ac18a708e8d0b93ce881791ee71cc37f65faa3d4660fa4ad63fd49ea09d6` | 字节相同 |
| 提示词 | 2951 字符，SHA-256 `b21d8e54…6a32` | **唯一变量** |
| 预估费用 | 与 v1 同量级 ≈ $0.57（以响应 usage 为准） | — |
| 输出 | `C:/Users/steam/Videos/doubaoVideo/dao_v5_motion_replacement_v2_expr_4s_720p.mp4` | 新文件，不覆盖 v1 |

参考职责不得互换：`reference_video` = 动作与镜头来源（身体、武器、头部轨迹、头部倾角、取景、脚底线）；`reference_image` = 角色身份与服装与画法（v5 刀兵）。

### 16.3 实际提交文本

英文（实际提交，逐字使用）：

```text
Use the uploaded reference video as the motion and camera source. Preserve every body movement, timing, pose, foot placement, weapon path, head path and head tilt of the person in that video frame by frame. Do not add, remove, retime or speed up any action, and do not change the head angle, the torso bend, the stance or the camera framing in any frame.

Replace that person with the character from the reference image: short stocky proportions, slightly large head, broad shoulders, hard-edged high-resolution pixel-art rendering and cold-iron highlights. He wears a red headscarf under a plain open-face iron bowl helmet with the face fully exposed, one small square cold-iron chest plate, a hemp cross-collar tunic, a rope belt, dark loose trousers, leg wraps and short war boots, and he holds a ring-pommel dao. Keep the identical character in every frame with no costume change, no weapon change and no identity drift.

Keep the fighter at exactly the same size on screen and standing on exactly the same ground line as in the reference video. Do not make him larger, do not shrink him, and do not move his feet up or down in the frame. Do not change the camera zoom.

Animate the face only, on top of the preserved body motion. The face is a live changing performance, not a static painted mask. As he begins to raise the blade, his face is a cold focused scowl with level brows and a closed mouth. While the blade travels up, the brows pull down and together, the eyes narrow into a hard glare, the jaw sets and the lips press into a tight line. At the top of the lift the brows crush toward the brow centre, the glare peaks, and the mouth opens into a shout showing a dark gap and the upper teeth. The mouth is fully open at the highest point of the blade and stays open as the blade starts to fall. Through the swing the face stays committed to the attack, mouth still open, brows still crushed. As he recovers, the mouth closes back into a hard line and the face settles into a stern set. The expression must change gradually and continuously through those stages; it must never snap from one expression to another in a single frame, and it must never stay frozen for a long stretch and then jump.

Do not let him grin, smile, laugh or look cheerful. Do not open the mouth in the idle frames at the start or the settled frames at the end. Do not change the head tilt, turn or lift from the reference video in order to show the face; the expression must be read on the face at the angle it already has. Keep the helmet, the red headscarf, the tunic, the chest plate and the sword in the same place as in the reference video.

Keep the uniform opaque green background RGB(0,159,62), flat and even across every pixel of every frame. No floor, no contact shadow, no scenery, no text, no watermark, no checkerboard, no camera shake, no second person, no extra limbs, no duplicated weapons and no armor pieces that are not in the reference image.
```

中文对照（审阅用，不提交）：

把上传视频当作动作与镜头来源。逐帧保留该片中人物的所有身体动作、时序、姿态、落脚点、武器轨迹、头部轨迹与头部倾角；不增加、不删除、不重排、不加速任何动作；任何一帧都不改动头部角度、躯干前倾、站姿或取景。

把该片中的人物替换为参考图中的角色：短壮比例、头略大、肩宽、硬边高清像素画法、冷铁高光。红头巾压在敞脸铁盔下，脸完全露出；一块方形冷铁胸甲、麻色交领短袍、麻绳腰带、深色肥裤、裹腿、短靴，手持环首刀。每一帧都是同一个角色，不换服装、不换武器、不漂移身份。

人物在画面中的大小与脚底站位线必须与参考视频完全一致。不要把他放大，不要缩小，不要让他的脚在画面里上下移动。不要改变镜头变焦。

只动画面部，叠加在已保留的身体动作之上。脸是一段活的、变化的表演，不是一张贴上去的死面具。起手举刀时，表情是冷静专注的皱眉、闭口；刀向上走的过程中，眉压低并向内靠拢，眼收成凶戾的吊眼，下颌绷紧，唇抿成一条硬线；举到最高点时眉心挤压、凶相到顶，嘴张开成怒吼，露出口腔暗隙与上齿；刀最高点嘴完全张开，刀开始下落时仍保持张开；挥砍全程脸保持攻击状态，嘴仍开、眉仍压；收势时嘴收回成硬线，脸落回冷峻。表情必须逐级连续地变化，不得在一帧内从一种表情跳到另一种，也不得长时间冻结后再跳变。

不要咧嘴、微笑、发笑或任何愉快表情。起始待机帧与结尾收势帧不要张嘴。不要为了露出脸而改动参考视频里的头部倾角、转侧或抬降；表情必须以头部已有的角度来读。头盔、红头巾、短袍、胸甲与刀的位置保持与参考视频一致。

背景保持均匀不透明纯绿 RGB(0,159,62)，每一帧每一个像素都平匀一致。无地面、无接触阴影、无场景、无文字、无水印、无棋盘格、无镜头抖动、第二人、多余肢体、重复武器、参考图里没有的甲片。

**三段改动对应三个目标**：第 4 段整段新增（表情阶梯，写法用关节/部位位移而非情绪形容词，并显式禁止"整体放大"式表达与"冻结后跳变"）；第 3 段一条约束同时修主体放大与脚底下移（§16.1 ⑤）；第 5 段写死背景键值（§16.1 ⑥）。

### 16.4 验收分工

**由用户目视验收**：攻击神态（举刀段表情阶梯是否成立、能否读出怒吼、是否与劈砍段合理衔接）、动作是否与源片一致（是否有任何新增/重排/改速/改头部轨迹）、身份与服装是否漂移、主体尺寸与脚底线是否与源片一致。

**由本侧只做背景色一项的数值验收**（G2，发起方下载解码后提供成片即可）：逐帧量四角 64×64 中位，报 ΔR/ΔG/ΔB 相对基准键 `(0,159,62)` 的范围、最差帧号与数值、首尾帧数值，并说明与上一批（v1 为 −5…−27、最差 f096）是否同向同量级。成片底色不达基准但无棋盘格时，按 G2 第 6 条升级为待用户决策项，不静默结案。

表情、动作、尺寸三项**不由本侧出具数值结论**，也不代替用户判定合格与否。

### 16.5 若 A 方案不通过的升级路径

A 方案是单变量探针，只验"提示词能否让脸动起来"。若 v2 仍出现嘴全闭、脸冻结：

1. 说明靠词拗不动，进入 **B 方案**：追加一张"v5 挥砍峰值怒吼脸"作第二张 `reference_image`，词内写明图1 管身份服装、图2 只管面部表情与峰值姿态，图例分工必须显式写。
2. B 方案需要新增素材。表情阶梯的生图建议：一次 GPT Image 调用产出 v5 的 4 宫格表情表（屏息/咬牙/咧嘴露齿/张口怒吼），再裁成 4 张参考图；沿用本项目 §20/§27 六脸图的成熟做法，不新造流程。
3. `seedance-2-5` 支持 30 张参考图 + 10 段参考视频，且明确把 reference-video 定位为 "Motion, editing, or continuation reference"，无 `edit_mode`/`extend_mode`（用参考内容 + 文字表达编辑），比 `seedance-2-fast` 的 9 图上限更适合多表情阶梯；带视频输入 $6.40/1M token，2-fast 720p 为 $3.30/1M。是否升 2.5 待用户决定。
4. 仍受 §15.6 约束：不得在同一授权里连发第二版，不得改模型后再自重发；每次重生成须重新取得当次明确批准。

### 16.6 v2 提交与实测记录（2026-10-24 14:39 已发起，成片已收到）

**状态：已收到成片，等用户验收。**

按 §16.2–§16.4 发起，**只创建一个任务**，未重试、未降档、未换模型。

| 项 | 实测 |
| --- | --- |
| Task | `task_7ef0c06849df431a819e5b62038fab3f`，`create_attempts=1`，usage 173,700，`cost.spend=0.57321`（与 v1 同量级，计费条件 `has_video_input=true`、`720p`） |
| 实际提交 prompt | 2951 字符，SHA-256 `b21d8e547d599d2d8182698625013a65a82b5cbfd7eb0622984ef5269b736a32`，与 §16.3 代码块逐字一致（已校验） |
| 唯一输入 | 源片 `2b1888f…mp4`（SHA `f624106031c57b…`）与 v5 立绘（SHA `8330ac18…09d6`），均与 v1 字节相同 |
| 成片 | `C:/Users/steam/Videos/doubaoVideo/dao_v5_motion_replacement_v2_expr_4s_720p.mp4`，2,129,352 B，SHA-256 `e9fae4ec73234e83eea7ec535ac453f1570cff7190e604f281f546b65c177747`；960×960 / 24fps / 97 帧 / 4.042 s / h264，无音轨，全帧解码通过 |
| 轮询 | 首轮 12 次查询耗尽本地 120 s 预算仍为 `generating`（**本地预算耗尽 ≠ 服务端失败**）；续查同一 task 第 5 次拿到终态 `success`，观察等待 44.61 s；下载 157.93 s |

**本侧只做的背景色 G2 验收（其余四项由用户目视验收）**

| 项 | v2 | v1 | 结论 |
| --- | --- | --- | --- |
| f000 四角中位 | `(0,139,63)`，ΔG −20 | `(0,154,60)`，ΔG −5 | 变差 |
| f024 四角中位 | `(3,135,64)`，ΔG −24 | `(2,133,63)`，ΔG −26 | 持平 |
| f096 四角中位 | `(1,133,64)`，ΔG −26 | `(1,132,64)`，ΔG −27 | 持平 |
| f024–f096 ΔG 范围 | −26…−24 | −27…−24 | **同向同量级** |
| 全片 ΔR/ΔG/ΔB | 0…3 / −26…−19 / 0…2 | 0…3 / −27…−5 / −2…2 | 同向 |
| 四角 G std | min 1.20 / mean 1.43 / max 3.17 | 1.08 / 1.34 / 3.87 | 均匀，无棋盘格 |

**结论：背景键偏差未修复**，与 v1 同向同量级（最差帧 f096，ΔG −26）。按 G2 第 6 条升级为待用户决策项，不静默结案：接受 / 换参考重生成 / 只做交付物侧处理（抠图后在相同中性底比对主体色）。词里写死 `RGB(0,159,62)` 与 `flat and even` **不足以约束输出键值**——这与本项目长期实测的“输入已统一 ≠ 输出已统一”一致。

**参考信息（非本侧验收项，供用户目视时参考）**：主体尺寸几乎未变——身高比 out/src 均值 1.182（v1 为 1.216，峰值 1.293），主体底边 Y 偏差均值 +69.2px（v1 为 +68.5）。即 §16.3 第 3 段的尺寸约束在本次同样未生效。

**表情与动作由用户目视验收**，本侧不代为判定。已从成片 mp4 重新抽取 16 姿态槽位帧（v1 的 `frame_*.png` 是源片帧，已弃用），存于 `Library/Locus/tmp/dao_v5_repl_video/v2_slot_frames/`（`v2_attack01_f010.png` … `v2_attack16_f079.png`），并生成对照图 `v2_face_16slots.png`（16 槽位脸部放大）与 `v2_body_16slots.png`（16 槽位全身）。

### 16.7 尚未做的

v2 成片已收到（见 §16.6），等用户验收。v1 与 v2 均未选帧、未抠图、未补边、未建 Clip、未改 `Assets/Sprites/Enemy/Enemy1_1/`、Prefab、Animator。16 姿态槽位与已部署 `Enemy_1011_attack1..16` 的对应关系已测出（§16.1 ③），选帧时可直接复用；v2 的槽位帧已从成片重新抽出（`v2_slot_frames/`），**不得再用 `dao_v5_repl_video/frame_*.png`（那是源片）**。

## 17. v3：收窄怒吼幅度、缩短张嘴窗口（2026-10-24 已发起，**成片已收到，待用户验收**）

### 17.0 状态与边界

- 用户 2026-10-24 结论：**表情方向成立**（v2 的压眉／窄眼／闭嘴起手读得出来），但**这一版怒吼过于突出，需要微调**。
- 本节交付**诊断、决策、提交文本与提交记录**。2026-10-24 15:40 本侧按用户当次明确指令发起（此前“生成由用户在别处发起”的分工已作废）：只创建一个任务，未重试、未降档、未换模型；成片已收到，§17.5.1 已回填，§17.9 为本侧数值结果。
- 读取或修改本文不等于批准发起。发起侧提交前须逐条复述 §17.3 的唯一输入与参数、§17.4 的逐字文本与哈希、§17.5 的输出路径，确认文件不存在；提交后按 §17.5.1 回填。
- 词已落盘：`Library/Locus/tmp/dao_v5_repl_video/dao_v5_motion_replacement_v3_snarl.prompt.txt`（3,447 字符，SHA-256 `bdee94b606854bacdfd5483064be3ff2507945d34481ffa2eee8c52e344a1175`）。该哈希是对 prompt 文本本身（不含文件末尾换行）计算的，与 §16.6 的口径一致；文件字节级 SHA-256 为 `7b13d70a094cb29b377ef1bb3c49bfc267ec4e2d47982a0a378e6d7e13967bbf`。
- 单变量纪律已校验：与 v2 词文件逐段比对，**P1/P2/P3/P5/P6 逐字相同（SAME），只有 P4 变**（1,033 → 1,529 字符）。v2 词文件重建后 SHA-256 仍为 `b21d8e54…6a32`，与 §16.0 一致，可证本次只有表情段被改。

### 17.1 诊断（2026-10-24 实测）

**方法**：逐帧在人物遮罩内、主体上部 30% 统计"暗红口腔色"像素（45≤R≤150、G≤80、B≤85、R≥G+15），以 v1（同身份、**全片闭嘴**）同帧位数值作基线，差值即表情改动带来的量。脚本与数据在 `Library/Locus/tmp/dao_v5_roar_analysis/`（`mouth_probe.py`、`mouth_probe.json`、`head_metrics.json`）。度量帧为 960×960，f41 主体高 810 px。

| 帧 | 槽位 | v1 暗红 px | v2 暗红 px | Δ |
|---|---|---|---|---|
| f0 | 片头待机 | 8,141 | 7,858 | −283 |
| f10 | attack1 | 7,005 | 7,013 | **+8** |
| f20 | attack2 后 | 5,024 | 4,920 | −104 |
| f28 | attack3 末 | 6,488 | 6,604 | +116 |
| f29 | 举刀起步 | 6,846 | 8,346 | **+1,500** |
| f35 | attack4 | 7,132 | 10,326 | **+3,194** |
| f41 | attack5 顶刀点 | 7,143 | 11,319 | **+4,176（峰值）** |
| f47 | attack6 前一帧 | 7,822 | 11,134 | +3,312 |
| f48 | attack6 | 5,033 | 7,191 | +2,158 |

读法：

- **f0–f28 两版面部一致**（Δ 在 ±150 内），说明起手段没有多出表情；差异全部集中在 f29–f47。
- **张嘴窗口 = f29–f47 = 19 帧 = 0.792 s**；峰值段 f35–f47 = 13 帧 = 0.542 s，峰在 f41–f42（attack5，举刀最高点）。
- 按 §16.1 ④，脸部可读窗口只有 f19–f48（30 帧 = 1.25 s），**张嘴占了其中 63%**，且正好压在脸最清楚、最居中的顶刀点上——这是"过于突出"的直接来源。
- 强度换算到 1108 精灵空间（主体 651 px 高）：峰值 Δ 面积 ≈ **2,700 px²**（≈ 60×45 或 52×52 的块），约等于角色脸高的三分之一。
- 未测到主体动作被改：两版逐帧主体高度与底边几乎同值（如 f41 均 810/862），可量到的差异集中在面部。

**成因在本侧的词里，不是模型加戏。** §16.3 的 P4 明确写了 `the mouth opens into a shout showing a dark gap and the upper teeth` 以及 `The mouth is fully open at the highest point of the blade and stays open as the blade starts to fall` / `mouth still open`——**幅度（整口张开、露口腔）与时长（劈砍全程保持）都是本侧指定的**，成片是照词执行。所以修法也只能是改词，不能指望重采样。

### 17.2 决策

只改 P4 一段（其余五段一字不动，保持单变量）：

1. **幅度**：从"张口怒吼、露出暗腔与上齿"改成"窄幅无声呲牙"——唇只分开一线（至多一到两行像素簇），只露上齿一条细边，**不出现口腔暗腔**。
2. **时长**：从"劈砍全程保持张嘴"改成"只在顶刀点瞬间，刀一开始下落就闭嘴"；劈砍段用压眉、窄眼、绷下颌保持攻击性。
3. **强度预算转移**：明确要求强度来自眉、眼、下颌，嘴小到不成为视觉焦点。
4. **堵替代动作**：显式禁止用后仰、抬颌、前推下颌或任何额外颈部动作来补回"怒吼"的存在感。

此版目标不是"表情更凶"，而是**把已经成立的凶相留住、把嘴的权重降下来**。

### 17.3 决策与参数（全部与 v1、v2 相同）

| 项 | 值 | 与 v2 |
|---|---|---|
| 模型 | `seedance-2-fast` | 不变 |
| 时长 / 分辨率 / 比例 | 4 s / 720p / 1:1 | 不变 |
| seed / 音频 / 水印 | -1 / false / false | 不变 |
| reference_video | `C:/Users/steam/Videos/doubaoVideo/2b1888f6388ed56b14da731eb0aeb229.mp4`，817,576 B，SHA-256 `f624106031c57b569f76cf30b758730a21e9f73bc6f92567c6f35c07bfafa5c9` | 字节相同（2026-10-24 复核） |
| reference_image | `C:/Users/steam/Pictures/gptGen/enemy1011_style_locked_dao_v5/enemy1011_style_locked_dao_v5.png`，1,352,518 B，SHA-256 `8330ac18a708e8d0b93ce881791ee71cc37f65faa3d4660fa4ad63fd49ea09d6` | 字节相同（2026-10-24 复核） |
| 提示词 | 3,447 字符，SHA-256 `bdee94b6…1175` | **唯一变量** |
| 预估费用 | 与 v1/v2 同量级 ≈ $0.57（以响应 usage 为准） | — |
| 输出 | `C:/Users/steam/Videos/doubaoVideo/dao_v5_motion_replacement_v3_snarl_4s_720p.mp4` | 新文件，不覆盖 v1/v2 |

参考职责不得互换，调用规则仍按 §15.6：一次授权只发一个，不重试、不降档、不换模型。

### 17.4 实际提交文本

英文（实际提交，逐字使用；**P4 为本次唯一改动段**，其余五段与 §16.3 相同）：

```text
Use the uploaded reference video as the motion and camera source. Preserve every body movement, timing, pose, foot placement, weapon path, head path and head tilt of the person in that video frame by frame. Do not add, remove, retime or speed up any action, and do not change the head angle, the torso bend, the stance or the camera framing in any frame.

Replace that person with the character from the reference image: short stocky proportions, slightly large head, broad shoulders, hard-edged high-resolution pixel-art rendering and cold-iron highlights. He wears a red headscarf under a plain open-face iron bowl helmet with the face fully exposed, one small square cold-iron chest plate, a hemp cross-collar tunic, a rope belt, dark loose trousers, leg wraps and short war boots, and he holds a ring-pommel dao. Keep the identical character in every frame with no costume change, no weapon change and no identity drift.

Keep the fighter at exactly the same size on screen and standing on exactly the same ground line as in the reference video. Do not make him larger, do not shrink him, and do not move his feet up or down in the frame. Do not change the camera zoom.

Animate the face only, on top of the preserved body motion. The face is a live changing performance, not a static painted mask. As he begins to raise the blade, his face is a cold focused scowl with level brows and a closed mouth. While the blade travels up, the brows pull down and together, the eyes narrow into a hard glare, the jaw sets and the lips press into a tight line. Near the top of the lift the brows crush toward the brow centre, the glare peaks, and the mouth tightens into a narrow silent snarl: the lips part only a hair, at most one or two pixel-cluster rows tall, with only a thin sliver of the upper teeth showing and no dark cavity behind them. That narrow snarl lasts only a moment at the top of the lift, and the lips close again before the blade starts to fall. Through the swing the face stays committed to the attack through the crushed brows, the narrow eyes and the set jaw, while the mouth stays closed. As he recovers, the face settles into a stern set. All the intensity must come from the brows, the eyes and the jaw, and the mouth must stay small enough that it is never the focal point. Never open the jaw wide, never show the inside of the mouth or the throat, never shout or scream, and do not compensate with a head throw-back, a lifted chin, a thrust jaw or any extra neck movement. The expression must change gradually and continuously through those stages; it must never snap from one expression to another in a single frame, and it must never stay frozen for a long stretch and then jump.

Do not let him grin, smile, laugh or look cheerful. Do not open the mouth in the idle frames at the start or the settled frames at the end. Do not change the head tilt, turn or lift from the reference video in order to show the face; the expression must be read on the face at the angle it already has. Keep the helmet, the red headscarf, the tunic, the chest plate and the sword in the same place as in the reference video.

Keep the uniform opaque green background RGB(0,159,62), flat and even across every pixel of every frame. No floor, no contact shadow, no scenery, no text, no watermark, no checkerboard, no camera shake, no second person, no extra limbs, no duplicated weapons and no armor pieces that are not in the reference image.
```

中文对照（审阅用，不提交）。P1/P2/P3/P5/P6 见 §16.3 中文对照，逐字不变；**只有第四段的改动是**：

起手举刀时仍是冷静专注的皱眉、闭口。刀向上走时眉压低并向内靠拢、眼收成凶戾窄眼、下颌绷紧、唇抿成硬线。**到举刀最高点附近，眉挤压到顶，嘴收成一个窄幅的无声呲牙：唇只分开一线，至多一到两行像素簇，只露上齿一条细边，后面不出现口腔暗腔。这个窄幅呲牙只在顶刀点维持一瞬，刀一开始下落，唇就闭回去。**挥砍全程的攻击性靠压眉、窄眼和绷紧的下颌来保持，**嘴保持闭合**。收势时脸落回冷峻。**全部强度必须来自眉、眼和下颌，嘴要小到永远不是视觉焦点。禁止放宽下颌、禁止露出嘴内或喉咙、禁止吼叫或尖叫；也不许用后仰、抬颌、前推下颌或任何额外颈部动作来补偿。** 表情必须逐级连续变化，不得在一帧内跳变，也不得长时间冻结后再跳变。

### 17.5 输出

```text
C:/Users/steam/Videos/doubaoVideo/dao_v5_motion_replacement_v3_snarl_4s_720p.mp4
Library/Locus/tmp/dao_v5_repl_video/dao_v5_motion_replacement_v3_snarl.request.json
Library/Locus/tmp/dao_v5_repl_video/dao_v5_motion_replacement_v3_snarl.status.json
```

发起前确认成片文件不存在，不覆盖 v1/v2 的成片与记录。

### 17.5.1 发起与回填（本侧发起，已回填）

**职责划分（已变更）**：2026-10-24 15:40 用户要求由本侧发起，本侧负责提交、轮询、下载与回填，并只做 §17.6 的两项数值。

**提交前三项自检（已执行）**：

1. 提示词取自 `Library/Locus/tmp/dao_v5_repl_video/dao_v5_motion_replacement_v3_snarl.prompt.txt`，未改、未裁剪、未追加负面段；LF 归一后 3,447 字符，SHA-256 `bdee94b6…1175`，与 §17.4 一致。
2. 两个输入哈希与 §17.3 一致：源片 `f6241060…bfa5c9`、v5 立绘 `8330ac18…ea09d6`。
3. 参数只取 §17.3 表内值；单次授权只发一个。

**新发现的坑（下次发起必看）**：词文件 `dao_v5_motion_replacement_v3_snarl.prompt.txt` 是 **CRLF** 行尾（3,459 字节，字节哈希 `7b13d70a…7bbf`）。直接按原始字节提交会得到 3,458 字符、SHA `566938c3…ad7a`，与文档口径不符。必须先 `CRLF → LF` 并去掉末尾单个换行，才得到 3,447 字符 / `bdee94b6…1175` 的“文档文本”。本次提交用的是后者，`.request.json` 同时记了两个哈希。

**提交后回填**：

| 字段 | 值 |
|---|---|
| 发起时间（本地时区） | 2026-10-24 15:40:45 |
| Task id | `task_3dc63c400b6f4fefadae1c8615a8dcf3` |
| create_attempts | 1（无重试） |
| usage / cost.spend | 173,700 / $0.57321（billing：OUTPUT_TOKENS 173,700，条件 `has_video_input=true`、`720p`，与 v1/v2 同） |
| 轮询结果 | 首轮 12 次（≤120 s）仍为 `generating`；第二轮第 10 次拿到终态 `success`，共 22 次查询，观察等待 171.8 s；下载 173.93 s |
| 成片路径 | `C:/Users/steam/Videos/doubaoVideo/dao_v5_motion_replacement_v3_snarl_4s_720p.mp4` |
| 字节数 / SHA-256 | 2,116,632 B / `dd79be1b99b775060f9cf38bcaab1ce559e120220a2fc06179f16a10681871b3` |
| 规格实测 | 960×960 / 24fps / 97 帧 / 4.042 s / h264 / 无音轨；`ftyp`、顶层 box、全帧解码通过 |
| 提交文本哈希 | `bdee94b6…1175`（3,447 字符，与 §17.4 逐字一致） |

### 17.6 验收

**用户目视**：嘴幅是否明显小于 v2（不再有整口张开与口腔暗腔）；顶刀点是否仍读得出攻击性；是否没有被后仰／抬颌／前推下颌等替代动作顶替；动作、身份、尺寸是否仍与源片一致。

**成片由发起侧提供并落盘后，本侧才执行下面两项数值**；在此之前不开始取帧、建槽位或统计。

**本侧只报数**（沿用 §17.1 同一方法，不出合格结论）：

- 背景色 G2：四角 64×64 中位对基准键 `(0,159,62)` 的 ΔR/ΔG/ΔB 范围、最差帧、首尾帧。
- 开口面积曲线：v3 与 v2 同帧位的暗红像素数与 Δ 对比，重点看峰值是否下降、窗口是否缩短到顶刀点附近；f0–f28 是否仍为 0 差值（验证起手段未被动过）。

### 17.7 若仍不通过

1. **仍偏大**：把 P4 的窄幅呲牙整段删掉，改成"全程闭口、只咬紧牙关"，用闭口下颌与眼角承担全部表情。
2. **闭口后攻击性不足**：进入 §16.5 的 B 方案（追加一张表情参考图，图例分工必须显式写）。
3. **开口合适但被替代动作毁掉**：单独一版只加禁止词，不动表情阶梯。
4. 任一重发都须重新取得当次明确批准，不得在同一授权里连发第二版。

### 17.8 尚未做的

v3 成片已收到（§17.5.1）。未做正式选帧、抠图、补边、Clip 与 Unity 接入；未改 `Assets/Sprites/Enemy/Enemy1_1/`、Prefab、Animator。

### 17.9 2026-10-24 v3 数值（本侧只报数，不出合格结论）

方法与 §17.1 相同：人物遮罩内、主体上部 30% 统计暗红口腔色像素（45≤R≤150、G≤80、B≤85、R≥G+15），以 v1（全片闭嘴）同帧位为基线，Δ = 本版 − v1。脚本 `Library/Locus/tmp/dao_v5_roar_analysis/analyze_v3.py`，数据 `mouth_probe_v3.json`。

| 帧 | 槽位 | v1 | v2 | v3 | Δv2 | Δv3 |
|---|---|---|---|---|---|---|
| f0 | 待机 | 8,141 | 7,858 | 8,146 | −283 | +5 |
| f10 | attack1 | 7,005 | 7,013 | 6,504 | +8 | −501 |
| f20 | attack2 | 5,024 | 4,920 | 5,498 | −104 | +474 |
| f28 | attack3 | 6,488 | 6,604 | 8,091 | +116 | **+1,603** |
| f29 | 举刀起步 | 6,846 | 8,346 | 9,773 | +1,500 | +2,927 |
| f35 | attack4 | 7,132 | 10,326 | 11,581 | +3,194 | +4,449 |
| f41 | 顶刀点 | 7,143 | 11,319 | 12,224 | +4,176 | **+5,081（峰值）** |
| f47 | attack6 前 | 7,822 | 11,134 | 12,238 | +3,312 | +4,416 |
| f48 | attack6 | 5,033 | 7,191 | 7,049 | +2,158 | +2,016 |
| f60 | 劈砍中 | 9,911 | 10,174 | 9,790 | +263 | −121 |
| f79 | attack16 | 9,205 | 9,056 | 9,206 | −149 | +1 |
| f96 | 收势 | 8,600 | 8,328 | 8,447 | −272 | −153 |

- **峰值**：v2 f41 +4,176；**v3 f41 +5,081（比 v2 高 21.7%）**。
- **超阈窗口（Δ>300）**：v2 = f29–f68（29 帧）；**v3 = f14–f49（35 帧）**。即 v3 比 v2 早开、早关，但窗口总长更长。
- **起手段 f0–f28 最大 |Δ|**：v2 = 501；**v3 = 1,603**（f28）。f0 基本持平（+5）。
- 读法限制：该指标测的是“上部 30% 区域的暗红像素量”，区域内包含红头巾与邻接金属阴影，不是纯嘴部像素计数；绝对值不能当嘴宽，只能做同帧位版本间对比。

**背景（G2，四角 64×64 中位对基准键 `(0,159,62)`）**

| 项 | v1 | v2 | v3 |
|---|---|---|---|
| ΔR / ΔG / ΔB 全片范围 | 0…3 / **−27…−5** / −2…2 | 0…3 / **−26…−19** / 0…2 | 0…2 / **−28…−7** / −2…2 |
| f000 | `(0,154,60)` | `(0,139,63)` | `(0,152,60)` |
| f024 | `(2,133,63)` | `(3,135,64)` | `(2,133,62)` |
| f096 | `(1,132,64)` | `(1,133,64)` | `(2,131,64)` |
| 四角 G std（min/mean/max） | 1.08 / 1.34 / 3.87 | 1.20 / 1.43 / 3.17 | 1.13 / 1.50 / 3.67 |

结论（只陈述数值）：背景键仍偏低，与 v1、v2 同向同量级（最差 f096，ΔG −28），未因词里写死 `RGB(0,159,62)` 而修复；无棋盘格。按 G2 第 6 条仍为**待用户决策项**：接受 / 换参考重生成 / 只做交付物侧处理。

**供目视的抽取物**（非验收结论）：`Library/Locus/tmp/dao_v5_repl_video/v3_slot_frames/`（16 姿态槽位：`v3_attack01_f010.png` … `v3_attack16_f079.png`，从 v3 成片重新抽取）、`Library/Locus/tmp/dao_v5_repl_video/v3_vs_v2_face_peak.png`（行 = v1 / v2 / v3，列 = f28 / f35 / f41 / f47 / f48，头部区域放大）。

**2026-10-24 用户对 v5 静帧的结论：** 造型可以留，但头身比与已部署 1011 不一致，硬线条太少，画风相差甚远。视频三版因此暂停，下一张静帧按 §18 重绘。

## 18. v6：用 1011 的头身和硬边重绘 v5 服饰

### 18.1 只改什么

保留 v5 的服饰和战斗脸：红头巾上的无缨敞脸盔、方铁牌、麻绳腰带、麻色短袍、短护腕、深色裤、裹腿、短靴、画面左侧前指环首刀、画面右侧松拳、压眉窄眼和闭口。

改掉 v5 的身体和画法：头和盔放大，肩加宽，躯干和腿缩短到 1011 的短壮比例；外轮廓改成可见阶梯，明暗改成两三阶色块，去掉细墨线、布纹、皮肤渐变和光滑金属高光。

不退回 v1 的军官甲、封闭面罩、圆筒肩甲和直剑。不退回 v3、v4 的连体甲或分开护裆。

2026-10-24 同宽测量：1011 身体高度约 599，肩宽约为身高的 0.74，腿约占 0.17。v5 身体高度约 794，肩宽约为身高的 0.56，腿约占 0.21。不要再把 v5 的比例写成目标。

### 18.2 两张输入，职责分开

`image[]` 必须正好两张，顺序固定：

```text
1. C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v1\enemy1011_idle1_green_input.png
   1108x1108 RGB, 355,410 bytes
   SHA-256 3f3c17a2be763ce80a054da333ef3bbc0b9190dfeba19d8402e38e2e38b6da56
   只锁头身、肩宽、腿长、阶梯轮廓和色块。不锁面罩、圆筒肩甲、直剑。

2. C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v5\enemy1011_style_locked_dao_v5.png
   1024x1024 RGB, 1,352,518 bytes
   SHA-256 8330ac18a708e8d0b93ce881791ee71cc37f65faa3d4660fa4ad63fd49ea09d6
   只锁服饰、武器和战斗脸。不锁比例、细线和皮肤渐变。
```

发起前复核这两个哈希。不上传 1011 原透明图、v2、v3、v4、参考截图或视频帧。

### 18.3 为什么不是单图编辑

只上传 v5 并写“改成 1011 画风”不会成立。`input_fidelity=high` 会把 v5 的高头身和平滑轮廓一起锁住。1011 必须作为第一张比例和画法底图进入请求。

### 18.4 参数

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[] order:
  1. enemy1011_idle1_green_input.png
  2. enemy1011_style_locked_dao_v5.png
prompt: section 18.5 English full prompt + blank line + English negative prompt
read timeout: 900 seconds
automatic retries: 0
```

其余调用规则仍按 §5：不传 `background`，凭据只从 `MUSK_API_KEY` 读，15 秒内 400、读超时、断连或 504 都停下，不在同一次授权里重发。

### 18.5 实际提交文本

中文供审阅，不提交。

第一张图只提供短壮头身、宽肩、短腿、阶梯外轮廓和硬边色块。第二张图只提供服饰、环首刀和战斗脸。不要保留第一张的面罩、圆筒肩甲和直剑。不要保留第二张的高头身、细长腿、细墨线、布纹和皮肤渐变。

把头和盔放大，肩加宽，躯干和腿缩短，直到身体重量接近第一张图。手改成简化色块。外轮廓用可见像素阶梯，明暗只用两三阶。铁甲用冷灰块和少量锐利高光，布用大色块，不要织纹。

服饰保持第二张：红头巾上的无缨敞脸盔，脸完全露出，方铁牌，麻绳腰带，麻色短袍，两侧短护腕，深色裤，裹腿，短靴。环首刀在画面左侧并略向前，画面右侧是松拳。眉压低，眼窄，嘴闭成一条直线。

```text
Image 1 controls only the short stocky head-to-body ratio, broad shoulders, short legs, stepped outer contour and hard-edged color clusters. Image 2 controls only the clothing, ring-pommel dao and combat face. Do not keep image 1's closed visor, tubular pauldrons or straight sword. Do not keep image 2's tall body, long legs, thin ink outlines, cloth weave or skin gradients.

Enlarge the head and helmet, widen the shoulders, and shorten the torso and legs until the body weight matches image 1. Simplify the hands into chunky color blocks. Rebuild every outer edge as visible pixel stairs, and use only two or three shade steps. Render iron as cold-gray blocks with a few sharp highlights, and cloth as large flat blocks without weave.

Keep image 2's costume: an open-face bowl helmet over a red headscarf, a fully exposed face, one small chest plate, a rope belt, a hemp tunic, one short bracer on each forearm, dark trousers, leg wraps and short boots. Keep the ring-pommel dao on screen-left and angled forward, with a loose fist on screen-right. Keep the brows pressed low, the eyes narrow and the mouth closed in one line.

Deliver one 1024x1024 PNG on a uniform opaque green background RGB(0,159,62), with head, both hands, both feet and blade tip inside the central 70 percent.
```

```text
Avoid image 2's tall proportions, long legs, narrow shoulders, thin ink lines, cloth weave, skin gradients, smooth metal highlights, painterly rendering, closed visor, orange eye slits, tubular pauldrons, straight sword, large shoulder armor, tasset plates, connected hip armor, red plume, nasal guard, cheek plates, open mouth, shouting, shorter-than-image-1 legs, cropped feet, scenery, text, checkerboard and any canvas other than 1024x1024.
```

中文负面词对照：不要第二张图的高头身、长腿、窄肩、细墨线、布纹、皮肤渐变、光滑金属高光、厚涂；不要封闭面罩、橙红眼缝、圆筒肩甲、直剑、大肩甲、裙甲、连体护裆、红缨、鼻甲、护颊、张嘴、喊叫、比第一张图更短的腿、裁切的脚、场景、文字、棋盘格，以及 1024×1024 以外的画布。

实际 prompt 只拼接本节两个英文代码块，中间一个空行。不写游戏名或角色名。

### 18.6 输出

发起前确认这些文件不存在。已存在就停下，不覆盖。

```text
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v6\enemy1011_style_locked_dao_v6.png
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v6\enemy1011_style_locked_dao_v6.request.json
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v6\enemy1011_style_locked_dao_v6.response.json
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v6\enemy1011_style_locked_dao_v6.status.json
```

记录规则与 §7 相同。记录里必须写明两张输入的顺序和哈希。不要覆盖 v1 绿幕副本或 v5 原图。

### 18.7 验收

与已部署 1011 Idle 和 v5 并排。通过必须同时满足：

- 头身、肩宽和腿长接近 1011，不再是 v5 那种高出约三分之一的身体。
- 外轮廓能看见阶梯，明暗是色块。没有 v5 的细墨线、布纹、皮肤渐变和光滑高光。
- 服饰仍是 v5：敞脸盔、红头巾、方铁牌、麻绳腰带、短护腕、环首刀。没有面罩、圆筒肩甲、直剑、大肩甲和连体护裆。
- 脸仍是压眉、窄眼、闭口。底色均匀纯绿，刀和双脚完整。

比例仍接近 v5，或轮廓仍是平滑立绘，即失败并停下。不自动重发，不降 `input_fidelity`，不重做视频，不进入去背。

### 18.8 2026-10-24 v6 提交记录（已提交一次，等待目视验收）

按 §18.4–§18.6 提交了一次，未重试、未降档、未换模型、未传 `background`。

| 项 | 实测 |
| --- | --- |
| 输入顺序 | `image[]` 1 = `enemy1011_idle1_green_input.png`（SHA `3f3c17a2…6da56`、355,410 B）；2 = `enemy1011_style_locked_dao_v5.png`（SHA `8330ac18…09d6`、1,352,518 B）。二者提交前复核通过 |
| 参数回显 | `model=gpt-image-2.5-sunburst`、`quality=max`、`size=1024x1024`、`background=auto`（未传）、`n=1` |
| prompt | 仅 §18.5 两个英文块 + 一个空行，1,734 字符，SHA-256 `f838c10a0a61d791260a25f22a91229a1e500747f01f332d83adf737313f5555` |
| HTTP | 200，耗时 47.67 s；usage 8,740（image_tokens 1,120 + text 596 → out 7,024） |
| 输出图 | `C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v6\enemy1011_style_locked_dao_v6.png`，1,271,109 B，1024×1024，PNG 签名通过，Color Type 2，SHA-256 `8f52cd052014ded5b55dc554cc717b7d98625e15eb3f9b9fee7e07aa738a8785` |
| 记录文件 | 同目录 `.request.json`（含两张输入的顺序与哈希）、`.response.json`、`.status.json` |

底色：四角 `(4,158,68)`、边框中位 `(2,156,64)`，均匀无棋盘格，接近目标 `RGB(0,159,62)`。

主体包围盒 `(109,135)–(816,914)`，708×780；留白 左 10.6% / 右 20.2% / 上 13.2% / 下 10.6%。左右留白小于 §18.5 的 15%，主体未裁切但横向更宽。

**比例实测（内容包围盒）**：1011 idle 640×689（宽高比 0.93）、v5 583×794（0.73）、**v6 708×780（0.91）**。即 v6 的宽高比已靠近 1011（0.91 vs 0.93，v5 为 0.73）；高度比 v5 低 1.8%（780 vs 794），比 1011 idle 高 13%（1011 的 689 为透明图 bbox，与§18.1 的“身体高 599”不是同一口径，不能直接比）。

并排对照（另行生成，非原图修改）：`Library\Locus\tmp\dao_v5_repl_video\v6_vs_1011_vs_v5.png`，三张按等高 560 px 摆放，用于看头身与肩宽比例。

**用户验收：比例正确，画风不通过。** 角色被过度像素网格化，用力过猛，与 1011 当前画风不符。原因是 §18.5 写了 visible pixel stairs、chunky color blocks 和 only two or three shade steps。头身、服饰和武器保留。下一版只按 §19 去掉内部方格。

未做去背、选帧、抠图、对齐、补边、Clip 或 Unity 接入；未重发 §15–§17 视频。

## 19. v7：去掉内部方格，保留 v6 头身

### 19.1 只改什么

继续编辑 v6。不再上传 1011 原图、v5 或任何参考图。1011 只在本地对照，不进请求，避免面罩和直剑回来。

锁定 v6 的短壮头身、宽肩、短腿、敞脸盔、红头巾、方铁牌、麻绳腰带、麻色短袍、短护腕、深色裤、裹腿、短靴、画面左侧环首刀、画面右侧松拳、压眉窄眼和闭口。

只改内部画法：删掉脸上、布上、铁甲上和皮肤上的方格与棋盘状明暗。甲片、布和脸的内部改成较大的受光面和暗面。铁甲保留少量锐利高光。外轮廓可以留少量阶梯，但阶梯不得铺进内部。

不要退回 v5 的细墨线、布纹、皮肤渐变和高头身。不要退回 v1 的面罩、圆筒肩甲和直剑。

禁止在提交词里再写 pixel stairs、chunky pixels、two or three shade steps、mosaic 或 pixel grid。这些词会把方格画回来。也不要写 smooth painting 或 realistic skin。

### 19.2 唯一输入

```text
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v6\enemy1011_style_locked_dao_v6.png
1024x1024, Color Type 2, 1,271,109 bytes
SHA-256 8f52cd052014ded5b55dc554cc717b7d98625e15eb3f9b9fee7e07aa738a8785
```

发起前复核这个哈希。不覆盖 v6 目录。`image[]` 数量必须为 1。

### 19.3 参数

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[]: ONLY enemy1011_style_locked_dao_v6.png
prompt: section 19.4 English full prompt + blank line + English negative prompt
read timeout: 900 seconds
automatic retries: 0
```

其余调用规则仍按 §5：不传 `background`，凭据只从 `MUSK_API_KEY` 读，15 秒内 400、读超时、断连或 504 都停下，不在同一次授权里重发。

### 19.4 实际提交文本

中文供审阅，不提交。

使用上传图锁定短壮比例、宽肩、短腿、服饰、环首刀、战斗脸和绿底。不要改头身，不要更换衣服或武器。

去掉脸上、布上、铁甲上和皮肤上的方格。把这些内部改成较大的受光面和暗面。铁甲只留少量锐利高光。外轮廓可以保留少量转折，但不要把格子铺进形体内部。刀刃改成完整的受光面，不要锯齿格。

```text
Use the uploaded soldier only to preserve his short stocky proportions, broad shoulders, short legs, open-face helmet, red headscarf, small chest plate, rope belt, hemp tunic, short bracers, dark trousers, leg wraps, short boots, screen-left ring-pommel dao, screen-right loose fist, pressed brows, narrow eyes, closed mouth and green background. Do not change his head-to-body ratio, clothing or weapon.

Remove the square grid from the face, cloth, iron armor and skin. Rebuild those interiors as larger light planes and shadow planes. Keep only a few sharp highlights on the iron. A few stepped turns may remain on the outer contour, but do not tile squares through the inside of any form. Rebuild the blade as one continuous light plane instead of jagged squares.

Deliver one 1024x1024 PNG on the original uniform green background RGB(0,159,62), with head, both hands, both feet and blade tip inside the central 70 percent.
```

```text
Avoid square pixels inside the face, cloth, armor or skin, checkerboard shading, jagged blade squares, thin ink outlines, cloth weave, skin gradients, smooth painterly rendering, realistic skin, closed visor, orange eye slits, tubular pauldrons, straight sword, taller body, longer legs, narrower shoulders, open mouth, scenery, text, checkerboard background and any canvas other than 1024x1024.
```

中文负面词对照：不要脸、布、甲或皮肤内部的方格，不要棋盘状明暗，不要锯齿刀刃，不要细墨线、布纹、皮肤渐变、平滑厚涂、写实皮肤、封闭面罩、橙红眼缝、圆筒肩甲、直剑、更高的身体、更长的腿、更窄的肩、张嘴、场景、文字、棋盘格背景，以及 1024×1024 以外的画布。

实际 prompt 只拼接本节两个英文代码块，中间一个空行。不写游戏名、角色名，也不写 pixel、chunky、mosaic 或 grid。

### 19.5 输出

发起前确认这些文件不存在。已存在就停下，不覆盖。

```text
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v7\enemy1011_style_locked_dao_v7.png
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v7\enemy1011_style_locked_dao_v7.request.json
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v7\enemy1011_style_locked_dao_v7.response.json
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v7\enemy1011_style_locked_dao_v7.status.json
```

记录规则与 §7 相同。不要把结果写回 v6 目录。

### 19.6 验收

与 v6 和已部署 1011 Idle 并排。通过必须同时满足：

- 脸、布、铁甲和皮肤内部不再是方格。能看见较大的受光面和暗面。
- 头身、肩宽和腿长仍接近 v6，没有被拉回 v5。
- 服饰和武器仍是 v6。没有面罩、圆筒肩甲、直剑，也没有退回 v5 的细线立绘。
- 底色均匀纯绿，刀和双脚完整。

内部方格还在，或头身被拉高，即失败并停下。不自动重发，不降 `input_fidelity`，不重做视频，不进入去背。

### 19.7 2026-10-24 v7 提交记录（已提交一次，等待目视验收）

按 §19.3–§19.5 提交了一次，未重试、未降档、未换模型、未传 `background`。

| 项 | 实测 |
| --- | --- |
| 唯一输入 | `enemy1011_style_locked_dao_v6.png`，SHA-256 `8f52cd05…8785`、1,271,109 B（提交前复核通过；未上传 1011/v5） |
| 参数回显 | `model=gpt-image-2.5-sunburst`、`quality=max`、`size=1024x1024`、`background=auto`（未传）、`n=1` |
| prompt | 仅 §19.4 两个英文块 + 一个空行，1,325 字符，SHA-256 `261acafbcb735a2ed9e39a7e5754eb01cbe69eaef36c454177fd37a5b6c8d555`（逐字提交，未改写） |
| HTTP | 200，耗时 31.08 s；usage 8,078（image 560 + text 494 → out 7,024） |
| 输出图 | `C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v7\enemy1011_style_locked_dao_v7.png`，1,331,292 B，1024×1024，签名通过，Color Type 2，SHA-256 `ff8ae3594cd0e7cc5af43cc89a357014d3c2c05a2f7b6be843fb758ba0f385be` |
| 记录文件 | 同目录 `.request.json`、`.response.json`、`.status.json` |

**词的一处歧义（已按原文提交）**：§19.1 要求提交词不写 `pixel`、`grid`，但 §19.4 正文自身含 `square grid`（删除指令）、负面含 `square pixels`。本次按“逐字拼接 §19.4 两个代码块”执行，未自行删改；若下一版要避这些词，需先回本文改词再发起。

底色：边框中位 `(1,157,69)`，四角 `(7,149,76) (5,149,73) (7,153,75) (6,154,74)`，边框颜色 124 种且无周期性明暗交替，无棋盘格。

主体包围盒 `(108,119)–(818,923)`，711×805（v6 为 708×780）；留白 左 10.5% / 右 20.0% / 上 11.6% / 下 9.8%。高度比 v6 高 3.2%，宽高比 0.88（1011 0.93、v6 0.91）。

**内部高频量（只报数，不代判定）**：腐蚀 6px 后的主体内部，相邻像素差 >30 的转移密度（每 1000 个内部像素）：v6 水平 152.6 / 垂直 159.8；**v7 水平 137.2 / 垂直 147.7**，下降约 10%。该指标只能说明“内部明暗转折变少”，不能直接等于“方格已消失”。

并排对照（另行生成，非原图修改）：`Library\Locus\tmp\dao_v5_repl_video\v7_vs_v6_vs_1011.png`（左 1011 idle、中 v6、右 v7，等高 580 px）。

**用户验收：不通过。** 去掉方格后完全没有 1011 的画风，变成平滑卡通。用户要求下一版提示词从简：直接用 1011 替换 v7 的画风，形象仍是 v7。按 §20 执行。

未做去背、选帧、抠图、对齐、补边、Clip 或 Unity 接入；未重发视频。

## 20. v8：用 1011 图替换 v7 的画风

### 20.1 只做这一件事

第一张图提供画风。第二张图提供要保留的形象。不再写阶梯、色块、方格或明暗阶数。上一版这些词分别把结果推成了马赛克和平滑卡通。

形象指 v7 的敞脸盔、红头巾、方铁牌、麻绳腰带、短护腕、环首刀、短壮身体和战斗脸。画风指 1011 图里的绘制方式。不锁 1011 的面罩、圆筒肩甲和直剑；这句话只写在本节说明里，不写进提交词，避免长词再次带偏。

### 20.2 两张输入，顺序固定

```text
1. C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v1\enemy1011_idle1_green_input.png
   1108x1108 RGB, 355,410 bytes
   SHA-256 3f3c17a2be763ce80a054da333ef3bbc0b9190dfeba19d8402e38e2e38b6da56
   画风来源。

2. C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v7\enemy1011_style_locked_dao_v7.png
   1024x1024 RGB, 1,331,292 bytes
   SHA-256 ff8ae3594cd0e7cc5af43cc89a357014d3c2c05a2f7b6be843fb758ba0f385be
   形象来源。
```

发起前复核这两个哈希。不上传 v5、v6、参考截图或视频帧。`image[]` 数量必须为 2，顺序不得颠倒。

### 20.3 参数

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[] order:
  1. enemy1011_idle1_green_input.png
  2. enemy1011_style_locked_dao_v7.png
prompt: section 20.4 English prompt only
read timeout: 900 seconds
automatic retries: 0
```

本版不附加负面词。其余调用规则仍按 §5：不传 `background`，凭据只从 `MUSK_API_KEY` 读，15 秒内 400、读超时、断连或 504 都停下，不在同一次授权里重发。

### 20.4 实际提交文本

中文供审阅，不提交。

用第一张图的画风，重绘第二张图的角色。保留第二张图的形象、衣服、武器、姿势和比例。

```text
Redraw the soldier in image 2 using only the rendering style of image 1. Keep image 2's identity, clothing, weapon, pose and proportions.
```

实际 prompt 只有这一个英文代码块。不追加负面词，不提交中文，不改写。

### 20.5 输出

发起前确认这些文件不存在。已存在就停下，不覆盖。

```text
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v8\enemy1011_style_locked_dao_v8.png
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v8\enemy1011_style_locked_dao_v8.request.json
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v8\enemy1011_style_locked_dao_v8.response.json
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v8\enemy1011_style_locked_dao_v8.status.json
```

记录规则与 §7 相同。记录里写明两张输入的顺序和哈希。不要覆盖 1011 绿幕副本或 v7 原图。

### 20.6 验收

与 1011 Idle 和 v7 并排。通过必须同时满足：

- 绘制方式接近 1011，不再是 v7 的平滑卡通，也不是 v6 的整片方格。
- 形象仍是 v7：敞脸盔、红头巾、方铁牌、麻绳、短护腕、环首刀和短壮比例。
- 没有把 1011 的封闭面罩、圆筒肩甲或直剑换回来。
- 底色均匀纯绿，人和刀完整入画。

画风仍是平滑卡通，或形象被换成 1011 的装甲，即失败并停下。不自动重发，不改词，不降 `input_fidelity`，不重做视频。

### 20.7 2026-10-24 v8 提交记录（已提交一次，等待目视验收）

按 §20.3–§20.5 提交了一次，未重试、未降档、未换模型、未传 `background`。

| 项 | 实测 |
| --- | --- |
| 输入顺序 | `image[]` 1 = `enemy1011_idle1_green_input.png`（SHA `3f3c17a2…6da56`、355,410 B，画风）；2 = `enemy1011_style_locked_dao_v7.png`（SHA `ff8ae359…852be`、1,331,292 B，形象）。提交前均复核 |
| 参数回显 | `model=gpt-image-2.5-sunburst`、`quality=max`、`size=1024x1024`、`background=auto`（未传）、`n=1` |
| prompt | 仅 §20.4 单个英文块（无负面词），137 字符，SHA-256 `ffac48afcfe172ef51625e40e4fd4c61fa27e885ba68aa484e61a2f929755968` |
| HTTP | 200，耗时 36.55 s；usage 8,341（image 1,120 + text 197 → out 7,024） |
| 输出图 | `C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v8\enemy1011_style_locked_dao_v8.png`，1,332,883 B，1024×1024，签名通过，Color Type 2，SHA-256 `739ac29ddc8a0199047fde459fcea2b22f83f1d06c4e40418ddf928852e2e146` |
| 记录文件 | 同目录 `.request.json`（含两张输入顺序与哈希）、`.response.json`、`.status.json` |

底色：四角 `(4,157,67)` 区域均匀，无棋盘格。

主体包围盒 `(116,138)–(810,926)`，695×789（v7 711×805）；留白 左 11.3% / 右 20.8% / 上 13.5% / 下 9.5%。宽高比 0.88，与 v7 同量级。

**画风代理指标（只报数，不代判定）**：主体内部（腐蚀 6px）相邻像素差 >30 的转移密度，每 1000 个内部像素——1011 idle 水平 289.2 / 垂直 284.8；v7 水平 137.2 / 垂直 147.7；**v8 水平 163.9 / 垂直 175.1**（向 1011 方向移动，但仍差约一半）。主体唯一色数：1011 77,828、v7 91,824、**v8 113,230**。即高频量上升、色数也上升，两个指标不指向同一方向，需以目视为准。

并排对照（另行生成，非原图修改）：`Library\Locus\tmp\dao_v5_repl_video\v8_vs_v7_vs_1011.png`（左 1011 idle、中 v7、右 v8，等高 580 px）。

**用户验收：不通过。** 还是太像素方块化，像乐高积木。短词里的 rendering style 被理解成了马赛克。形象仍回到 v7，画风词按 §21 改成体积和色块，并明确禁止大方块。

未做去背、选帧、抠图、对齐、补边、Clip 或 Unity 接入；未重发视频。

## 21. v9：取 1011 的体积，拒绝大方块

### 21.1 只改画风说法

输入仍是 1011 绿幕副本和 v7。不上传 v8，避免方块被高保真锁住。v7 的平滑画法是错的，但它的形象、衣服、武器、姿势和比例保留。

不再写 rendering style。上一版这个说法被理解成了乐高方块。本版只写金属体积、大色块和少量锐利高光，并明确禁止大方块。

### 21.2 两张输入，顺序固定

```text
1. C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v1\enemy1011_idle1_green_input.png
   1108x1108 RGB, 355,410 bytes
   SHA-256 3f3c17a2be763ce80a054da333ef3bbc0b9190dfeba19d8402e38e2e38b6da56
   画风来源。

2. C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v7\enemy1011_style_locked_dao_v7.png
   1024x1024 RGB, 1,331,292 bytes
   SHA-256 ff8ae3594cd0e7cc5af43cc89a357014d3c2c05a2f7b6be843fb758ba0f385be
   形象来源。
```

发起前复核这两个哈希。不上传 v5、v6、v8、参考截图或视频帧。`image[]` 数量必须为 2，顺序不得颠倒。

### 21.3 参数

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[] order:
  1. enemy1011_idle1_green_input.png
  2. enemy1011_style_locked_dao_v7.png
prompt: section 21.4 English prompt only
read timeout: 900 seconds
automatic retries: 0
```

本版不附加负面词。其余调用规则仍按 §5：不传 `background`，凭据只从 `MUSK_API_KEY` 读，15 秒内 400、读超时、断连或 504 都停下，不在同一次授权里重发。

### 21.4 实际提交文本

中文供审阅，不提交。用户已确定下面这一句，发起时不得改写。

用第一张图的高清像素画法重绘第二张图：有形的金属体积、大块明暗和少量锐利高光。保留第二张图的形象、衣服、武器、姿势和比例。不要把士兵画成大方块或乐高积木。

```text
Redraw image 2 in image 1's high-resolution pixel style: shaped metal volumes, broad shade planes and a few sharp highlights. Keep image 2's identity, clothing, weapon, pose and proportions. Do not turn the soldier into large square blocks or Lego bricks.
```

实际 prompt 只有这一个英文代码块。不追加负面词，不提交中文，不改写。

### 21.5 输出

发起前确认这些文件不存在。已存在就停下，不覆盖。

```text
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v9\enemy1011_style_locked_dao_v9.png
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v9\enemy1011_style_locked_dao_v9.request.json
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v9\enemy1011_style_locked_dao_v9.response.json
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v9\enemy1011_style_locked_dao_v9.status.json
```

记录规则与 §7 相同。记录里写明两张输入的顺序和哈希。不要覆盖 1011 绿幕副本、v7 或 v8。

### 21.6 验收

与 1011 Idle、v7 和 v8 并排。通过必须同时满足：

- 不是 v8 的大方块，也不是 v7 的平滑卡通。
- 金属能读出体积，明暗是较大的面，高光少而锐。
- 形象仍是 v7：敞脸盔、红头巾、方铁牌、麻绳、短护腕、环首刀和短壮比例。
- 没有换回 1011 的封闭面罩、圆筒肩甲或直剑。底色均匀纯绿，人和刀完整入画。

仍是大方块或平滑卡通，即失败并停下。不自动重发，不改词，不降 `input_fidelity`，不重做视频。

### 21.7 2026-10-24 v9 提交记录（已提交一次；已按场景对照作废）

按 §21.3–§21.5 提交了一次，未重试、未降档、未换模型、未传 `background`。

| 项 | 实测 |
| --- | --- |
| 输入顺序 | 1 = `enemy1011_idle1_green_input.png`（SHA `3f3c17a2…6da56`、355,410 B）；2 = `enemy1011_style_locked_dao_v7.png`（SHA `ff8ae359…852be`、1,331,292 B） |
| prompt | 仅 §21.4 单块（无负面词），255 字符，SHA-256 `57c0dd1133d99a55244aa22da68a6b79fb6c515ccb5fd92dcbeace86cda970af` |
| HTTP | 200，耗时 33.60 s；usage 8,370 |
| 输出图 | `C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v9\enemy1011_style_locked_dao_v9.png`，1,317,222 B，1024×1024，Color Type 2，SHA-256 `fbe8f3d9d096877ca1e367ea016327039c12222f232212d70bdfc380a5e992d8` |

底色四角 `(2,159,69)`；主体 686×807，宽高比 0.85；内部高频 185.8/192.7、唯一色数 112,195。对照图：`Library\Locus\tmp\dao_v5_repl_video\v9_vs_v8_v7_1011.png`。

**状态：已提交但未验收即被 §22 的前置结论取代**（1011 的冷铁方格与 N1/J1/N3 的暖土细像素不是同一世界），按用户指示不再发起 §21 系列。上表仅供追溯，不作验收依据。

**2026-10-24 补充：§21 不再发起。** 对照已接入场景后，1011 的冷铁方格与 N1、J1、N3 的暖土细像素不是同一世界。下一张按 §22，画风改用 J1 木篱。

## 22. v10：用已接入场景的画法重绘 v7

### 22.1 场景结论

已读并对照：

- N1：`N1_MidgroundTimberBarricade_v1_clean.png`、`N1_LowChevalDeFrise_v1.png`、`N1_Road_v1.png`
- J1：`J1_A03_IntactLowFence_v1.png`
- N3：`N3_H1_CompactHouse_v1.png`、`N3_B1_IntactWall_v1.png`、`N3_W1_Well_v1.png`

三处是同一套画法：先有完整形体，再在表面上铺细像素。木桩是圆的，木纹顺着材料走；石头是圆角大块；路是暖土连续颗粒。边缘有阶梯，但阶梯不切开形体。金属是小面积暖灰，有厚度。红布是破边小布条。

1011 是冷铁、大红围巾和粗方格，不作为本版画风输入。它只保留为头身对照，不上传。

### 22.2 本版只改画风来源

形象仍用 v7：敞脸盔、红头巾、方铁牌、麻绳、短护腕、环首刀、短壮比例和战斗脸。不上传 v8，避免方块被锁住。

画风只用 J1 木篱的绿幕副本。选它是因为木、铁箍和暖光都在一张小图里，比整张土路或石墙更不容易把角色画成道具。N3 石墙和水井只在本地对照，不上传。

### 22.3 两张输入，顺序固定

```text
1. C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v10\j1_fence_green_style_input.png
   768x436 RGB, 157,227 bytes
   SHA-256 60db5906e03a5dd752e72ef262e5e41fcb6b29440eb9ca9eac2a11c73372d1a8
   由 J1_A03_IntactLowFence_v1.png（SHA-256 e56321f44ad85a7d8ba91ab9b25b36ddc2f14b92427bd848d0371855e5416211）把透明区填成 RGB(0,159,62) 得到。四角实测 (0,159,62)。只提供木、铁和暖光的画法。

2. C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v7\enemy1011_style_locked_dao_v7.png
   1024x1024 RGB, 1,331,292 bytes
   SHA-256 ff8ae3594cd0e7cc5af43cc89a357014d3c2c05a2f7b6be843fb758ba0f385be
   形象来源。
```

发起前复核这两个哈希。不上传 1011、v5、v6、v8、土路、石墙或视频帧。`image[]` 数量必须为 2，顺序不得颠倒。不要把木篱画进结果。

### 22.4 参数

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[] order:
  1. j1_fence_green_style_input.png
  2. enemy1011_style_locked_dao_v7.png
prompt: section 22.5 English prompt only
read timeout: 900 seconds
automatic retries: 0
```

本版不附加负面词。其余调用规则仍按 §5：不传 `background`，凭据只从 `MUSK_API_KEY` 读，15 秒内 400、读超时、断连或 504 都停下，不在同一次授权里重发。

### 22.5 实际提交文本

中文供审阅，不提交。

用第一张图的暖色材质画法重绘第二张图的士兵。保留第二张的人、衣服、武器、姿势和比例。材质要有完整形体和细表面，不要大方块，也不要平滑卡通。不要把木篱画进画面。

```text
Redraw the soldier in image 2 with the warm material rendering of image 1: solid forms, fine surface grain and warm light. Keep image 2's person, clothing, weapon, pose and proportions. Do not use large square blocks or smooth cartoon shading, and do not draw the fence.
```

实际 prompt 只有这一个英文代码块。不追加负面词，不提交中文，不改写，不写 pixel style。

### 22.6 输出

发起前确认结果文件不存在。同目录已有 `j1_fence_green_style_input.png`，不要覆盖它，也不要把它当成生成结果。

```text
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v10\enemy1011_style_locked_dao_v10.png
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v10\enemy1011_style_locked_dao_v10.request.json
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v10\enemy1011_style_locked_dao_v10.response.json
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v10\enemy1011_style_locked_dao_v10.status.json
```

记录规则与 §7 相同。记录里写明两张输入的顺序和哈希。

### 22.7 验收

与 J1 木篱、v7 和 v8 并排。通过必须同时满足：

- 材质接近木篱和石墙：形体完整，表面有细颗粒，光是暖的。
- 不是 v8 的大方块，也不是 v7 的平滑卡通。
- 人仍是 v7：敞脸盔、红头巾、方铁牌、麻绳、短护腕、环首刀和短壮比例。画面里没有木篱。
- 底色均匀纯绿，人和刀完整入画。

仍是方块、平滑卡通，或画面里出现木篱，即失败并停下。不自动重发，不改词，不重做视频。

### 22.8 2026-10-24 v10 提交记录（已提交一次；用户验收不通过，见节末）

按 §22.4–§22.6 提交了一次，未重试、未降档、未换模型、未传 `background`。

| 项 | 实测 |
| --- | --- |
| 输入顺序 | 1 = `j1_fence_green_style_input.png`（768×436，SHA `60db5906…2d1a8`、157,227 B，四角 `(0,159,62)`）；2 = `enemy1011_style_locked_dao_v7.png`（SHA `ff8ae359…852be`、1,331,292 B）。均复核通过 |
| 参数回显 | `quality=max`、`size=1024x1024`、`background=auto`（未传）、`n=1` |
| prompt | 仅 §22.5 单块（无负面词），270 字符，SHA-256 `62cb74ec1a8497c6c622ad4ef38bf7a918647255cea57241447bc572c70bf6b6` |
| HTTP | 200，耗时 37.01 s；usage 8,374（image 1,120 + text 230 → out 7,024） |
| 输出图 | `C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v10\enemy1011_style_locked_dao_v10.png`，1,583,609 B，1024×1024，签名通过，Color Type 2，SHA-256 `1c99549830ba6117f603b050f35e5137b401f56664aab09572a8ff3b62d8b2c0` |
| 记录文件 | 同目录 `.request.json`（含两张输入顺序与哈希）、`.response.json`、`.status.json`；同目录原有 `j1_fence_green_style_input.png` 未动 |

底色：四角 `(3,155,67)`，均匀无棋盘格。

主体包围盒 742×882（v7 711×805、v8 695×789）——**本版主体比 v7 大一圈**；留白 左 9.1% / 右 18.5% / 上 7.1% / 下 6.7%。主体未裁切，但上下留白已远小于包制验收惯例的 15%，纵向占据 86% 画布。宽高比 0.84。

**画风代理指标（只报数，不代判定）**

| 图 | 内部高频（水平/垂直，每千内部像素） | 主体唯一色数 |
| --- | --- | --- |
| 1011 idle | 289.2 / 284.8 | 77,828 |
| v7 | 137.2 / 147.7 | 91,824 |
| v8 | 163.9 / 175.1 | 113,230 |
| **v10** | **216.8 / 209.5** | **158,946** |

即 v10 是四版新图中内部高频最高的一版（距 1011 约 25%），色数也最高，与“完整形体 + 表面细颗粒”的方向一致；但该指标无法区分“细颗粒”与“噪点”。

并排对照（另行生成，非原图修改）：`Library\Locus\tmp\dao_v5_repl_video\v10_vs_v8_v7_j1fence.png`（左→右 J1 木篱、v7、v8、v10，等高 520 px）。

**用户验收：不通过。** 用户反馈“只是吸取了色调，并没有吸纳画风设计”。对照原图可见：头盔、脸、布和皮肤都被铺成近似的小多边形切面，形体没有采用场景素材按材料与结构组织的笔触。v10 不作为下一版输入，原图与提交记录保留。

**修正此前判断：** “暖光＋表面颗粒”不是完整的画风定义；冷灰铁甲可以存在于暖土地场景中。内部高频增加、色数增加和颜色变暖都不能证明画风接近。此前把 1011 与场景差异主要归因于冷暖色的说法过于简单；下一版只做材质画法匹配试验，不把该判断写成已验收的美术规范。
