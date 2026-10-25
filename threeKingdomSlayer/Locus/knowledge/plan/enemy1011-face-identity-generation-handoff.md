---
id: kd_402e4a07-8951-499b-8967-64cb64a1cd8e
summary: 1011 刀兵身份与兵种组织交接：v5 是当前身份基准；§20 六脸图、§21 六兵种全身图已验收。§22–§26 五次面相尝试均不通过（变黑 / 歪嘴 / 写实 / 只改轮廓 / 只改轮廓）。§27 保持像素画风、换掉六组五官的方案已于 2026-10-23 出图（enemy_face_sheet_v5，1536×1024），等用户按 §27.6 验收。§17 换人视频仍待目检。未去背、未拆图、未进 Unity。§28 于 2026-10-23 新增参考图台账（三国杀 23 张、无双 15 张、三国志14 名将 17 张），入口 §0 / §15 / §27 / §28。
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 1011 刀兵身份与兵种组图：生成交接

## 0. 给发起侧的入口

**§27 已出图，等用户按 §27.6 验收；当前没有待执行的生成方案。** 再发新请求必须先在本文件新增章节（计划+参数+词+输出路径），再逐次授权。本文更新本身不是提交。

**参考图已更新，清单见 §28。** 写下一版生成词前按 §28 取图，不要再用已删除的 3D 实机截图。

§11 已执行，不再提交。§4–§8 是更早的失败记录，也不再提交。**v4 已被否决，不得重发也不得当风格参考。** 若还要发新请求，必须按 §15.4–§15.6 的规则复述唯一输入、参数、实际提交文本和输出路径，并取得该次明确批准。读取本文或修文档不等于批准。

不上传 v1、1011 原图或 `characterDesign.png`。**除用户明确指令的 §17 换人视频外**，不自行生成视频；不连做去背，不选帧，不抠图，不对齐，不补边，不导入 Unity。

## 1. 状态与授权边界

**当前状态：v5 是当前身份基准；§20 六脸图与 §21 六兵种全身图已验收；§22–§26 五次面相修改均不通过；§27 保持画风、换掉六组五官的方案已于 2026-10-23 出图（`enemy_face_sheet_v5`，1536×1024），等用户验收；§17 换人视频仍待目检。未去背、未拆图、未进 Unity。** **§28 参考图已于 2026-10-23 更新**（三国杀立绘 23 张、无双立绘 15 张、三国志14 名将 17 张；TW:TK 3D 截图已由用户删除）。

本任务重塑的是 `101 / 1011` 基础刀兵的脸和服饰身份。1011 现有六帧 Idle 仍是已部署绘制样本，但不再作为本图的上传参考，也不能把封闭面罩继续当成目标脸。

一次只提交一个请求。失败、超时、结果不明、棋盘格或身份偏离时保留记录并停下，不自动重试、不降档、不换模型、不改词再发。

建议继续使用资产编号 `1011`。本图通过前，不替换 `Assets/Sprites/Enemy/Enemy1_1/`、Prefab、Animator 或任何已部署帧。

## 2. 已确认的设计

这是类三国无双的基础刀兵，不是名将，也不是儿童像素兵。

必须同时成立：

- **成人露脸**：短方脸，下颌清楚；浓眉压近眉心，眉宽于眼；窄吊眼，眼内可有一小点暖色；一道鼻影；闭嘴；短髭或一小块下颌须。神态是战前凝视，不是微笑。
- **汉末服饰**：敞脸兜鍪、短红缨、护颈顿项；明光胸甲或札甲压在战袍上；披膊、护腕、可见衣袖、胫裙、皮带和战靴。甲是层叠结构，布和甲的边界清楚。
- **短壮战斗体量**：头可略大，肩宽，躯干紧凑，腿短，双脚分开站稳。这是比例，不是儿童脸。
- **一把环首刀**：解剖学右手持刀；角色正面看向镜头时，刀在画面左侧。解剖学左手在画面右侧，空置、可见、自然下垂。
- **绘制语言**：硬边像素色块、清楚受光面和暗部；冷铁灰甲、暖棕皮革、朱红只做缨、披膊或腰带识别。不是照片、3D、厚涂或马赛克滤镜。

明确不要：

- 圆脸、大眼、高光瞳仁、微笑、腮红、卡通五官漂在脸上。
- 封闭面罩、十字鼻甲挡全脸、只剩橙红眼缝、金属护颊遮住五官。
- 全身板甲、锁子甲、圆筒肩甲、包脚甲、十字护手直剑、盾、第二件武器。
- 整脸京剧油彩、长发挡脸、披风、将领级头饰和华丽金饰。
- 地面、接触阴影、场景、尘土、文字、UI、水印、棋盘格和渐变背景。

其他兵种不在本图出现。盾卫更方更沉，长枪脸略长，弓手眯眼专注，骑兵带短茬胡；这些是后续复制规则，不能提前画进这张刀兵图。张飞的环眼虬髯也不是刀兵模板。

## 3. 参考图职责与禁止上传

本次是纯文生图。`image[]` 数量必须为 0，不能把本地路径写进 prompt 充当上传。

禁止上传：

- `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle1.png` 至 `idle6.png`，以及该目录任何攻击、受击、死亡帧。它们会把金鼻甲、封闭面罩、板甲和直剑一起带进来。
- `C:\Users\steam\Downloads\characterDesign.png`。它的圆脸、大眼和微笑是本次要排除的问题。
- 旧 `Enemy1`、`Enemy2`、`Enemy3`、Boss、张飞、1051 身份图、1021 帧和任何历史失败生成图。
- 本文没有提供合格的三国无双静帧。不得临时从网上找图上传，也不得把无双角色名写进 prompt。

1011 只在本地承担绘制样本职责：冷铁色簇、朱红大色块、皮革边缘和像素阶梯。发起侧不把这些原图交给模型。若用户后来提供一张单独的无双面部静帧，必须先停下，改成单图编辑方案并重新批准，不能直接加进本次文生图。

## 4. 本次请求参数

```text
endpoint: POST https://api.muskapis.com/v1/images/generations
content-type: application/json
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
background: opaque
output_format: png
n: 1
image[]: none
prompt: section 5 English full prompt + blank line + section 5 English negative prompt
automatic retries: 0
```

- 文生图不使用 `input_fidelity`。不要改走 `/v1/images/edits` 来“顺便带参考图”。
- 制作底是不透明纯绿 `RGB(0,159,62)`。本阶段不请求 `background=transparent`。
- 凭据只从环境变量 `MUSK_API_KEY` 读取。不得写入脚本、请求记录、响应摘录、文档或回复。
- 不先调用付费探针。模型可用性以本次实际 HTTP 结果为准；不要静默换 `gpt-image-2` 或 `gpt-image-2.5-flare`。
- 主体、刀、盔缨和双手都在中央 70% 内，四边至少 15% 纯绿留白。允许整体缩放以满足留白，禁止单独拉长头、腿、刀或盔缨。

## 5. 实际提交文本

中文供审阅，不提交。

生成同一个基础刀兵的正面中立全身静帧。他是短壮成人，肩宽、躯干紧凑、腿短，双脚分开站稳，直接看向镜头。短方脸完全露出：浓眉压低并靠近眉心，窄而凶的吊眼，眼内一点暖色，一道清楚鼻影，闭嘴，短髭。不要微笑、圆脸或大眼。

穿汉末层叠甲，不穿欧洲全身板甲。敞脸兜鍪、短红缨、护颈顿项；明光胸甲或札甲压在战袍上；披膊、护腕、可见衣袖、胫裙、皮带和战靴。布与甲的边界清楚。冷铁灰甲、暖棕皮革，朱红只用于盔缨、披膊或腰带。

解剖学右手持唯一一把环首刀，刀身在画面左侧；解剖学左手在画面右侧，空置并自然下垂。不要盾、直剑、十字护手或第二件武器。

硬边高清像素画，色块清楚，不要照片、3D、厚涂、平滑渐变或马赛克滤镜。输出 1024×1024 PNG，底色为均匀不透明纯绿 RGB(0,159,62)。完整头部、双手、双脚、刀尖和盔缨都在中央 70% 内，四边至少 15% 留白。无地面、阴影、场景、文字、UI 或棋盘格。

### English full prompt

```text
Create one front-facing neutral full-body game sprite of a single adult Three Kingdoms infantry swordsman. He is short and stocky, with a slightly large head, broad shoulders, compact torso, short legs and feet planted apart, looking directly forward. His short square face is fully visible: heavy brows pressed low toward the brow center, narrow fierce upturned eyes with one small warm point inside each eye, one clear nose-bridge shadow, a closed mouth, and a short mustache. Adult and hostile, not smiling, round-faced or large-eyed.

Dress him in layered late-Han armor, not European full plate. Use an open-face doumou helmet, short crimson plume and neck guard; a mingguang chest plate or lamellar armor worn over a war robe; shoulder cape-armor, forearm guards, visible sleeves, a tasset skirt, belt and war boots. Keep cloth and armor boundaries clear. Cold iron-gray armor, warm brown leather, crimson only on the plume, shoulder armor or belt.

His anatomical right hand holds the only weapon, a ring-pommel dao, with the blade on screen-left. His anatomical left hand is on screen-right, empty and relaxed. No shield, straight sword, crossguard or second weapon.

Use hard-edged high-definition pixel clusters with clear light and shadow planes. Not photography, 3D, painterly rendering, smooth gradients or a mosaic filter. Deliver one 1024x1024 PNG on a uniform opaque green background RGB(0,159,62). Keep the complete head, both hands, both feet, blade tip and plume inside the central 70 percent, with at least 15 percent margin on every side. No floor, shadow, scenery, text, UI or checkerboard.
```

### English negative prompt

```text
Avoid childlike round face, large sparkling eyes, smile, blush, floating cartoon features, closed visor, cross-shaped nasal guard covering the face, orange eye slits without eyelids, cheek plates hiding the face, full-face opera makeup, European full plate, chainmail, tubular pauldrons, sabatons, cruciform sword, shield, second weapon, cape, officer headdress, long hair over the face, extra person, mirrored hands, cropped blade or feet, photographic realism, smooth 3D, soft painting, random pixel noise, floor, contact shadow, scenery, dust, text, UI, watermark, checkerboard, gradient background, white background, gray background and black background.
```

中文负面词对照：不要儿童圆脸、大闪光眼、微笑、腮红、漂浮卡通五官、封闭面罩、挡住脸的十字鼻甲、没有眼睑的橙红眼缝、遮脸护颊、整脸京剧油彩、欧洲全身板甲、锁子甲、圆筒肩甲、包脚甲、十字剑、盾、第二件武器、披风、将领头饰、挡脸长发、第二个人、左右手镜像、裁切的刀或脚、照片写实、光滑 3D、柔光厚涂、随机碎像素、地面、接触阴影、场景、尘土、文字、UI、水印、棋盘格、渐变背景、白底、灰底和黑底。

实际 prompt 只拼接上面 English full prompt、一个空行、English negative prompt。不提交中文、不提交本文其他章节。

## 6. 输出与记录

发起前确认目标文件不存在。已存在时先核对，不覆盖，不擅自改名重发。

```text
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v1\enemy1011_face_identity_v1.png
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v1\enemy1011_face_identity_v1.request.json
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v1\enemy1011_face_identity_v1.response.json
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v1\enemy1011_face_identity_v1.status.json
```

请求记录保存实际 prompt、参数、时间、HTTP 状态和文件哈希。响应保存原始 JSON。两份记录都不得包含 API key、Authorization 头或签名 URL 全文；URL 只记 host。

下载后先验证 PNG 签名、尺寸、文件大小、SHA-256 和 PNG Color Type。阶段 A 允许 Color Type 2，或 Alpha 全为 255 的 Color Type 6。不要把查看器里的棋盘格显示当成透明证据。

## 7. 发起顺序

1. 复述：无参考图、`generations`、`1024x1024`、`quality=max`、`background=opaque`、纯绿 `RGB(0,159,62)`、只提交 §5 英文正文和负面词、输出路径、自动重试 0 次。
2. 用户明确批准这一次后才提交。未批准时停止。
3. 完整下载并解码。报告路径、字节数、尺寸、Color Type、哈希、usage，以及脸、甲、武器和背景的目视差异。
4. 把原图交用户看。机器检查和发起侧观察都不等于用户验收。
5. 结果不明时保留已落盘证据并报告，不重建收费任务。下载中断只允许续传或重新下载同一结果，不允许重新创建生成任务。

## 8. 验收门禁

用户先看原图，再决定是否通过。通过必须同时满足：

- 缩小后先读到一个成人汉兵，而不是骑士面罩或儿童脸。
- 眉、眼睑、鼻影、闭嘴和短髭都能辨认；五官长在脸上，不是漂浮零件。
- 甲是层叠汉甲，布和甲能分开；没有全身板甲和十字剑。
- 只有一把环首刀，左右手关系正确，全身、刀尖和盔缨完整入画。
- 底色接近均匀纯绿，没有棋盘格、地面、场景、文字或 UI。
- 像素色块和体积仍接近 1011 的绘制语言，而不是另一套卡通或写实风格。

任一失败都停止。不自动重试，不本地改脸后冒充原图，不进入去背。

## 9. 通过之后才允许准备的事

以下内容现在都不执行：

- 阶段 B 语义去背：只移除制作底，不再改脸、衣服、姿态、比例或位置。输入必须是用户指定通过的原图，参数另批。
- 盾卫、长枪、弓手、骑兵的脸部分化。它们必须复用已通过的刀兵身份图，不能回到无参考文生图。
- 视频、选帧、抠图、对齐、补边、Clip 和 Unity 接入。各自另批，不能用身份图通过代替。

## 10. 本地绘制样本

这些文件只给发起侧本地对照，不上传：

```text
C:\threeKingdomSlayer\threeKingdomSlayer\Assets\Sprites\Enemy\Enemy1_1\Enemy_1011_idle1.png
```

对照时只看冷铁、朱红、皮革和像素阶梯。不要用它判定脸、盔、胸甲、肩甲、剑或左右手是否正确；那些正好是本次要替换的内容。

场景笔触只在本地对照，同样不上传：

```text
C:\threeKingdomSlayer\threeKingdomSlayer\Assets\Experiments\CurvedScroll\Art\EnvironmentV2\Props\N1\N1_MidgroundTimberBarricade_v1_clean.png
```

它说明当前场景是粗粝块面、可见阶梯和有限色阶。v2 要靠近这种块面，不要把道路贴图的细沙粒或写实木纹搬到角色上。

## 11. v1 失败与 v2 当前方案

### 11.1 v1 保留，不再提交

```text
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v1\enemy1011_face_identity_v1.png
```

HTTP 200，PNG Color Type 2，1254×1254，1,626,967 bytes，SHA-256 `d39dd7e64860cb8de5e3e52353524108c8d46758e5aef5f6ef515f90149a3382`。请求实际返回尺寸不是要求的 1024×1024；这是记录项，不是本次否决的主因。

用户结论：造型过于写实，偏离高清像素化，只有伪像素感，也和当前场景画风不匹配。

v1 做对的可以保留为身份目标，但不能上传，也不能当 v2 的底图：成人露脸、压眉、短髭、敞脸盔、札甲、环首刀和左右手关系。做失败的是绘制语言：脸、手指和刀刃是平滑插画，甲片是细密写实网格，轮廓没有阶梯，色阶过多。

### 11.2 v2 要改的只有绘制语言

身份、姿态、武器、制作底和接口都不变。v2 必须改成和 1011、当前场景同一家族的块面：

- 外轮廓、盔、肩、甲片和刀都是可见的阶梯边缘，不是抗锯齿平滑线。
- 明暗用成组色块，不用皮肤渐变、金属拉丝、木纹或细小铆钉网。
- 胸甲用几排可读的大甲片，不铺满小方格。
- 脸仍是成人露脸，但五官也由色块构成：粗眉、窄眼、鼻影、闭嘴、短髭。不要写实五官渲染。
- 手可以简化，不画精细手指。刀是几块金属色，不是照片级刃面。
- 颜色仍是冷铁、暖革、少量朱红。块面粗，不等于把角色画脏或压暗。

禁止把 v1 上传后要求“像素化”。那会保留插画形体，只在上面加伪像素纹理。

### 11.3 v2 参数

```text
endpoint: POST https://api.muskapis.com/v1/images/generations
content-type: application/json
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
background: opaque
output_format: png
n: 1
image[]: none
prompt: section 11.4 English full prompt + blank line + section 11.4 English negative prompt
automatic retries: 0
```

尺寸必须回读为 1024×1024。若服务端再次返回其他尺寸，保留原图并停止，不缩放冒充，不自动重发。

### 11.4 v2 实际提交文本

中文供审阅，不提交。

生成同一个基础刀兵的正面中立全身静帧，但这次必须是真正的高清像素角色，不是写实插画加像素滤镜。用大而清楚的像素色块塑造体积：阶梯轮廓、分组明暗、有限色阶。外轮廓、盔、肩甲、胸甲、刀和靴子都能看见像素台阶。

他仍是短壮成人，肩宽、躯干紧凑、腿短，双脚分开，看向镜头。短方脸完全露出，五官也是色块：粗眉压低，窄吊眼里一点暖色，一道鼻影，闭嘴，短髭。不要写实皮肤渐变，不要儿童圆脸。

汉末层叠甲：敞脸兜鍪、短红缨、顿项；少量大甲片组成的札甲或明光胸甲，压在战袍上；披膊、护腕、衣袖、胫裙、皮带、战靴。不要细密甲片网格，不要欧洲板甲。冷铁灰、暖棕、少量朱红。

解剖学右手持唯一一把环首刀，刀在画面左侧；解剖学左手在画面右侧，空置。手和刀都用简单色块，不画照片级手指或刃面反射。

输出 1024×1024 PNG，均匀不透明纯绿 RGB(0,159,62)。完整头部、双手、双脚、刀尖和盔缨在中央 70% 内，四边至少 15% 留白。无地面、阴影、场景、文字、UI 或棋盘格。

```text
Create one front-facing neutral full-body sprite of a single adult Three Kingdoms infantry swordsman, rendered as true high-definition pixel art rather than a realistic illustration with a pixel filter. Build the volume from large, readable pixel clusters: stepped contours, grouped light and shadow planes, and a limited palette. The outer silhouette, helmet, shoulders, chest armor, blade and boots must show visible pixel steps.

He remains short and stocky, with broad shoulders, a compact torso, short legs and feet planted apart, looking directly forward. His short square face is fully visible, and the features are also pixel blocks: heavy low brows, narrow fierce eyes with one small warm point, one nose-bridge shadow, a closed mouth and a short mustache. No realistic skin gradients and no childlike round face.

Use layered late-Han armor: an open-face doumou helmet, short crimson plume and neck guard; a few large readable lamellar rows or a mingguang chest plate over a war robe; shoulder cape-armor, forearm guards, sleeves, a tasset skirt, belt and war boots. No dense rivet grid and no European plate. Cold iron gray, warm brown, and a small amount of crimson.

His anatomical right hand holds the only ring-pommel dao, with the blade on screen-left. His anatomical left hand is on screen-right and empty. Simplify both hands and the blade into chunky pixel planes, not photographic fingers or reflective steel.

Deliver one 1024x1024 PNG on a uniform opaque green background RGB(0,159,62). Keep the complete head, both hands, both feet, blade tip and plume inside the central 70 percent, with at least 15 percent margin on every side. No floor, shadow, scenery, text, UI or checkerboard.
```

```text
Avoid semi-realistic illustration, soft anti-aliased outlines, realistic skin rendering, detailed fingers, reflective blade, dense lamellar grid, tiny rivets, wood grain, photographic texture, pixel-filter overlay, closed visor, European plate, cruciform sword, shield, smile, childlike face, scenery, floor, shadow, text, UI, checkerboard, gradient background and any size other than 1024x1024.
```

中文负面词对照：不要半写实插画、柔和抗锯齿轮廓、写实皮肤、精细手指、反光刀刃、细密札甲网格、微小铆钉、木纹、照片纹理、像素滤镜覆盖、封闭面罩、欧洲板甲、十字剑、盾、微笑、儿童脸、场景、地面、阴影、文字、UI、棋盘格、渐变背景，以及 1024×1024 以外的尺寸。

实际 prompt 只拼接本节两个英文代码块，中间一个空行。不提交 §5 的旧词。

### 11.5 v2 输出

```text
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v2\enemy1011_face_identity_v2.png
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v2\enemy1011_face_identity_v2.request.json
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v2\enemy1011_face_identity_v2.response.json
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v2\enemy1011_face_identity_v2.status.json
```

发起前确认这些文件不存在。记录规则、凭据规则和失败不重试规则与 v1 相同。

### 11.6 v2 验收增加项

§8 的身份条件仍然有效。v2 还必须同时满足：

- 轮廓和主要装备有可见像素台阶，不是平滑插画边缘。
- 脸、手、甲和刀都由有限色块构成，没有写实渐变和细密铆钉网。
- 与 1011 Idle、N1 木栅并排时，能解释为同一块面家族；不能只比 v1“更像像素”。
- 实际尺寸是 1024×1024。

任一失败都停止。不上传 v1 重绘，不本地加像素滤镜，不进入去背。

## 12. v2 已验收，下一步只改年轻脸

### 12.1 v2 结论

用户确认 v2 形象可以验收。机器记录：1024×1024，PNG Color Type 2，721,299 bytes，SHA-256 `810abc4145de7d5ea02705fdfb35b9a14978174cd045d2bc210c6fc62e2b64ee`。

```text
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v2\enemy1011_face_identity_v2.png
```

这张图成为当前刀兵身份底。后续编辑只允许改脸的年龄信号，不允许重做身体。

中年感来自四块，不来自盔甲：

- 上唇短髭连成一块成熟胡须。
- 鼻侧到嘴角有法令纹。
- 下巴宽、方，带中年下颌重量。
- 眉骨和颊侧阴影偏重，脸显得老成。

年轻化不是把脸改圆、改大眼或改笑。目标仍是成人基础兵：眉压低，窄吊眼，闭嘴，有敌意。

### 12.2 编辑边界

只改面部皮肤区域：去掉短髭，去掉法令纹，下颌略收窄，肤色少一档厚重阴影。眉、眼、鼻、闭嘴、盔、缨、顿项、披膊、甲、刀、双手、双脚、站姿、比例、画布和绿底都保持 v2。

这是局部年龄编辑，不是新身份。若输出身体、武器或轮廓明显重绘，即使脸更年轻也判失败。

### 12.3 参数

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
input_fidelity: high
background: opaque
output_format: png
n: 1
image[]: ONLY enemy1011_face_identity_v2.png
prompt: section 12.4 English full prompt + blank line + section 12.4 English negative prompt
automatic retries: 0
```

唯一输入就是上面的 v2 原图。不附加其他图片，不改走文生图。

### 12.4 实际提交文本

中文供审阅，不提交。

只编辑这张已验收刀兵图的脸，让他看起来更年轻。保留同一人的短壮身体、正面站姿、敞脸盔、红缨、顿项、层叠甲、披膊、环首刀、左右手、像素色块和纯绿底。

去掉上唇短髭，去掉鼻侧法令纹，下巴略收窄，减少中年颊影。保留压低的粗眉、窄吊眼、鼻影和闭嘴怒视。脸仍然由清楚像素块构成，不改成光滑写实皮肤，也不改成圆脸大眼或微笑。

不要改头盔、衣服、甲片、武器、手、脚、身体比例、位置或画布。输出仍是 1024×1024，底色保持原图的均匀纯绿。

```text
Edit only the face of this approved infantry sprite to make the same soldier look younger. Preserve his short stocky body, front-facing stance, open-face helmet, crimson plume, neck guard, layered armor, shoulder cape, ring-pommel dao, both hands, pixel clusters and green background exactly.

Remove the upper-lip mustache, remove the nasolabial folds, slightly narrow the jaw, and reduce the heavy mature cheek shading. Keep the low heavy brows, narrow fierce eyes, nose-bridge shadow and closed hostile mouth. The face must remain readable pixel blocks, not smooth realistic skin, a round childlike face, large eyes or a smile.

Do not change the helmet, clothing, armor plates, weapon, hands, feet, body proportions, position or canvas. Deliver the same 1024x1024 image on the original uniform green background.
```

```text
Avoid a new character, mustache, beard, nasolabial folds, mature heavy jaw, smile, round face, large eyes, realistic skin, changed armor, changed weapon, changed pose, changed scale, cropped body, scenery, checkerboard and any canvas other than 1024x1024.
```

中文负面词对照：不要新角色、短髭、胡须、法令纹、中年厚重下颌、微笑、圆脸、大眼、写实皮肤、改甲、改武器、改姿态、改比例、裁切身体、场景、棋盘格，以及 1024×1024 以外的画布。

实际 prompt 只拼接本节两个英文代码块，中间一个空行。

### 12.5 输出

```text
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v3\enemy1011_face_identity_v3.png
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v3\enemy1011_face_identity_v3.request.json
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v3\enemy1011_face_identity_v3.response.json
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v3\enemy1011_face_identity_v3.status.json
```

发起前确认这些文件不存在。不覆盖 v2。凭据只读环境变量，失败不自动重试。

### 12.6 验收

先与 v2 并排看。通过必须同时满足：

- 去掉短髭和法令纹后，脸读起来更年轻，但仍是成人敌意，不是儿童或笑脸。
- 眉、眼、鼻、闭嘴仍清楚，像素块没有变回 v1 的写实皮肤。
- 盔、甲、刀、手、脚、站姿和比例与 v2 是同一个兵。明显重绘身体即失败。
- 尺寸仍是 1024×1024，底色仍是均匀纯绿。

失败则保留 v2 为当前验收底，不自动重发，不进入去背。

### 12.7 v3 编辑未成功：两次 HTTP 400 记录（2026-10-21）

**当前状态：v3 未生成，v2 仍是唯一验收底。卡在接口侧，尚未决定下一步。**

两次都按 §12.3/§12.4 提交，词逐字未变（合并 prompt SHA-256 `268c7dc8244599c88a4dfa67a3b894c85193e9909b3d0f9507096139234f7318`），输入唯一为 v2（SHA-256 `810abc4145de7d5ea02705fdfb35b9a14978174cd045d2bc210c6fc62e2b64ee`），均未产生图片、未返回 usage，是否计费未知。

| 次序 | 时间（UTC） | 耗时 | 与上一次差异 | 结果 |
| --- | --- | --- | --- | --- |
| 第 1 次 | 2026-10-21T08:16:20Z | 13.97s | 首次提交；`prompt` 字段在前，未带 `User-Agent` | HTTP 400 `invalid_request` |
| 第 2 次 | 2026-10-21T08:19:42Z | 7.83s | 用户批准后重发；仅加 `User-Agent: curl/8.0`，`prompt` 移到字段末尾 | HTTP 400 `invalid_request` |

两次错误体相同：

```text
{"error":{"message":"请求参数有误或模型不可用 (request id: 20261021081631593696138LKxZNc05)","type":"invalid_request","param":"","code":"invalid_request"}}
{"error":{"message":"请求参数有误或模型不可用 (request id: 20261021081947870388926k2onqdyw)","type":"invalid_request","param":"","code":"invalid_request"}}
```

排查已做：

- 免费 `GET https://api.muskapis.com/v1/models` 返回 200，`gpt-image-2.5-sunburst` 在可用列表，令牌与模型权限正常。
- multipart 结构与此前成功的 `Library/Locus/tmp/enemy1051_identity_v3/submit_1051_identity_stageA.py` 一致：字段集合（model/size/quality/input_fidelity/background/output_format/n/prompt）、`image[]` 字段名、`Content-Type: image/png`、boundary 形式、`n` 传字符串均相同。
- 加 `User-Agent` 后错误不变，说明不是该头缺失导致。
- 文档内唯一的 400 先例是 `background=transparent` 被明确拒绝并提示去掉参数，与本次通用错误文案不同。

未证实的假设：可能是 `/v1/images/edits` 当前路由或该模型编辑能力暂时不可用；错误文案把“参数有误”和“模型不可用”合在一句，无法区分。没有证据支持“请求组装有 bug”这一结论，也没有证据排除。

记录位置（目录 `C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v3\`），未覆盖、未删除：

```text
enemy1011_face_identity_v3.attempt1.request.json
enemy1011_face_identity_v3.attempt1.response.json
enemy1011_face_identity_v3.attempt1.status.json
enemy1011_face_identity_v3.request.json
enemy1011_face_identity_v3.response.json
enemy1011_face_identity_v3.status.json
```

待用户决定的选项（未获批准前一律不执行，尤其不得自动重发）：

1. 等一段时间后原词原参数再试一次，不改任何参数。
2. 去掉 `background=opaque` 再试；历史上有成功请求省略过该字段，但属于参数变更。
3. 把 `image[]` 改为 `image` 试一次；属于实验性排查。
4. 停下，保持 v2 为验收底。

以上四项是当时的待决清单。2026-10-21 复查后，词长已被排除，当前只准备第 2 项，见 §12.8。第 1、3、4 项不执行。

### 12.8 词长排除与下一次唯一改动

失败 prompt 为 1072 字符。同一天成功的 v2 文生图 prompt 为 2103 字符，模型、`quality=max`、`size=1024x1024` 和 `background=opaque` 都相同。因此不是提示词太长，也不是这些公共参数本身不可用。

两次编辑失败的差异只在请求头和字段顺序，结果完全相同。词、输入图、`image[]`、`input_fidelity=high` 和 `background=opaque` 都没变。

编辑接口历史上验证过的是 `background=transparent`，或成功脚本虽然写过 `opaque`，但不能把文生图接受 `opaque` 外推到当前编辑路由。本次 400 没有指出具体字段，所以去掉 `background` 是候选，不是已证实根因。

下一次若获批准，只做这个改动：

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
input_fidelity: high
background: omitted
output_format: png
n: 1
image[]: ONLY the same v2 PNG
prompt: exactly section 12.4 merged English prompt, unchanged
automatic retries: 0
```

不缩短提示词，不改词，不改图片，不把 `image[]` 改成 `image`，不改走文生图。输出仍写入 §12.5 的 v3 路径；已有 attempt 记录保留，不覆盖。

若仍返回 400，立即停止并保留响应。不继续试字段名，也不自动改词。成功出图后才按 §12.6 与 v2 并排验收。

### 12.9 第 3 次提交结果：省略 background 仍 400（2026-10-21）

**按 §12.8 执行了一次，仍返回 400。已停止，没有继续试字段名，v3 未生成，v2 仍是唯一验收底。**

| 项 | 值 |
| --- | --- |
| 时间（UTC） | 2026-10-21T08:30:37Z 开始，耗时 12.48s |
| 参数 | `gpt-image-2.5-sunburst` / `1024x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1` / **`background` 完全省略** |
| 输入 | 只有 v2，SHA-256 `810abc41…b64ee` |
| 词 | §12.4 未改，SHA-256 `268c7dc8…f7318` |
| 结果 | HTTP 400 `invalid_request`，request id `20261021083046890467544C4buXZZi` |

错误体：

```text
{"error":{"message":"请求参数有误或模型不可用 (request id: 20261021083046890467544C4buXZZi)","type":"invalid_request","param":"","code":"invalid_request"}}
```

这次实测推翻了 §12.8 的候选根因：带 `background=opaque`（第1、2 次）与完全省略（第3 次）都是同一个 400。所以 `background` 不是区分因子，不能再靠改它排查。

三次提交的共同项，也是后续唯一值得怀疑的范围：`/v1/images/edits` 路由本身、`input_fidelity=high`、`quality=max` 在该路由上的组合、或本账号在该路由上的编辑能力。同会话内 `/v1/images/generations` 两次均 HTTP 200；免费 `/v1/models` 也 200。

记录（目录 `C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v3\`），三次全部保留、未覆盖：

```text
enemy1011_face_identity_v3.attempt1.request.json / .response.json / .status.json
enemy1011_face_identity_v3.attempt2.request.json / .response.json / .status.json
enemy1011_face_identity_v3.request.json / .response.json / .status.json   （第 3 次）
```

**未获新的明确授权不得再提交。** 如果后续要查，应先从非付费途径确认该路由是否可用（例如是否别的账号或历史请求在当前时间能拿到 edits 响应），而不是继续在词或字段上试探。也不要把 v3 改成文生图重画——那会把身体重绘，违反 §12.2。

### 12.10 图生图方法复查（2026-10-21）

同一路由当场复查，不保存图片：

- 不上传图片时，接口约数秒返回明确错误：`image is required`。路由、令牌和 multipart 都通。
- 只传 `model + prompt + image[]` 时，请求被接受并进入生成，不是 400。
- 单独加入 `quality=max` 或 `input_fidelity=high`，同样进入生成。
- 用 v2 原图配合 `size/quality/input_fidelity/output_format/n` 和短词 `red`，也进入生成。
- 把短词换成 §12.4 的 1072 字符词后，小图请求立即返回与 v3 相同的泛化 400。随后几句无关短词在同一字段组下也返回 400。

因此可以排除：提示词长度、`image[]` 字段名、图片损坏、`background=opaque`、编辑接口整体不可用。不能把这个 400 定位到某一个稳定字段；完整字段组刚才还能接受 `red`，随后又拒绝无关短词，说明当前网关拒绝不稳定，而且错误文案没有给出具体参数。

这些复查里，被接受的请求在客户端超时前可能已经开始生成。没有保存输出，也不能证明没有计费。后续不要再用连发短词继续拆字段。

下一次只允许这一组，并且只提交一次：

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
input_fidelity: high
image[]: ONLY the same v2 PNG
prompt: exactly section 12.4 merged English prompt
omit: background, output_format, n
automatic retries: 0
```

15 秒内再次返回同一个泛化 400，就停止。不改词、不换文生图、不继续试字段名。成功出图后才按 §12.6 验收。

### 12.11 第 4 次提交结果：§12.10 允许的唯一字段组仍 400（2026-10-22）

**已按 §12.10 提交一次，5.996 秒内返回同一个泛化 400，触发停止条件。已停，v3 未生成。**

| 项 | 值 |
| --- | --- |
| 时间（UTC） | 2026-10-22T03:36:04Z 开始，耗时 5.996s |
| 字段 | `model` / `size=1024x1024` / `quality=max` / `input_fidelity=high` / `prompt` + `image[]` |
| 省略 | `background`、`output_format`、`n` |
| 输入 | 只有 v2，SHA-256 `810abc41…b64ee` |
| 词 | §12.4 未改，SHA-256 `268c7dc8…f7318` |
| 结果 | HTTP 400 `invalid_request`，request id `20261022033606676184274Eco68KIZ` |

```text
{"error":{"message":"请求参数有误或模型不可用 (request id: 20261022033606676184274Eco68KIZ)","type":"invalid_request","param":"","code":"invalid_request"}}
```

四次提交（`background` 为 `opaque`、`omitted`、`omitted`+去 `output_format`/`n`）均为同一个泛化 400。§12.10 已实测短词可进入生成，所以差异在“长词 + 本组字段”落到当前路由时的行为，而不是某一个稳定参数。仅保留为观察，不再据此试字段。

记录（目录 `C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v3\`），四次全部保留、未覆盖：

```text
.attempt1.* / .attempt2.* / .attempt3.*   （已归档）
enemy1011_face_identity_v3.request.json / .response.json / .status.json   （第 4 次）
```

**当前状态：v3 仍未生成，v2 仍是唯一验收底。** 下一次只按 §12.12 提交一次。

### 12.12 接口自检与下一次唯一请求

对照 `skill/gpt-image-generation.md` 的编辑规则：

- 必须是 `POST /v1/images/edits` 的 `multipart/form-data`，不能用 JSON。
- 必填只有 `model`、`prompt`、`image[]`。四次请求都有。
- 当前令牌的图像模型只有 `gpt-image-2.5-sunburst`，没有 `gpt-image-2.5-flare`。不要换模型。
- `size=1024x1024`、`quality=max`、`input_fidelity=high`、`output_format=png`、`n=1` 都是文档列出的可选字段。
- v2 是有效 PNG，721,299 bytes，文件头正确。
- 提示词 1072 字符，短于已经成功的 v2 文生图，不是长度问题。

四次 400 都在数秒内返回，没有 `usage`，也没有指出具体字段。因此不继续删字段。下一次回到文档里的完整编辑字段，只省略没有任务需要的 `background`。

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
image[]: ONLY C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v2\enemy1011_face_identity_v2.png
prompt: section 12.4 merged English prompt, unchanged
automatic retries: 0
```

输出仍是 §12.5 的 v3 路径。已有四次 attempt 记录全部保留，不覆盖。

发起侧提交前只复述这一组。若 15 秒内再次返回同一个泛化 400，立即停止，不改词、不改字段、不改文生图。成功出图后才按 §12.6 与 v2 并排验收。

### 12.13 第 5 次提交结果：未再 400，但客户端读超时，结果未知（2026-10-22）

**按 §12.12 的字段组提交一次。这次没有返回 400，说明请求已通过网关校验并进入生成；但客户端 90 秒读超时，未拿到响应，图片未落盘。**

| 项 | 值 |
| --- | --- |
| 时间（UTC） | 2026-10-22T04:17:59Z 开始，102.56s 后超时 |
| 字段 | `model` / `size=1024x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1` / `prompt` + `image[]` |
| 省略 | 仅 `background` |
| 输入 | 只有 v2，SHA-256 `810abc41…b64ee` |
| 词 | §12.4 未改，SHA-256 `268c7dc8…f7318` |
| 结果 | 客户端 `TimeoutError`，无响应体、无 usage、无 task id，无法回取 |

这是一个实质变化：前四次 400 都在 6–14 秒内返回，本次没有校验错误。说明 `§12.12` 的字段组可通过，先前 400 更像是路由侧不稳定，而不是本字段组有错。

但本次结果未知：客户端超时后网关没有返回 task id 或 URL，无法像文生图那样续传或重下同一结果。**按规则没有自动重发，也没有把超时当失败重建任务。**

记录（目录 `C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v3\`）：

```text
.attempt1.* / .attempt2.* / .attempt3.* / .attempt4.*   （已归档）
enemy1011_face_identity_v3.request.json                  （第 5 次，无 response）
enemy1011_face_identity_v3.status.json                   （TimeoutError 记录）
```

下一次若要拿回图片，只能再用 §12.12 完全相同的字段与词提交一次，并把客户端读超时抬到 300–600 秒（生成本身可能需要 100 秒以上，本次 90 秒上限不足）。这是新的付费提交，必须另行明确授权；不得改词、不得加 `background`、不得换模型或走文生图。输出仍写 §12.5 的 v3 路径。

### 12.14 第 6 次提交成功出图（2026-10-22）

按 §12.13 的做法重发：字段、词、输入全与第 5 次相同，唯一改动是客户端读超时 90→600 秒。

| 项 | 值 |
| --- | --- |
| 时间（UTC） | 2026-10-22T04:2x 开始，135.36s 返回 |
| 结果 | HTTP 200，`b64_json`，1024×1024，Color Type 2 |
| 文件 | 1,000,204 bytes，SHA-256 `410394815013894d07621b136b9da5adf95c2de8ea7854dc81229f6cda645000` |
| usage | input 990（image 560 / text 430），output 7024，total 8014；未返回 cost |
| 路径 | `C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v3\enemy1011_face_identity_v3.png` |

机器对比 v2 的结果（发起侧无法看图，下列只是像素测量，不等于验收）：

- 全图 5.55% 像素变化，但变化不集中在脸：头/盔带 1.07%、肩胸 9.24%、腰裙 8.90%、腿 7.43%、脚 1.37%。躯干和腿都比脸改动更多。
- 变化幅度 75.5% 落在 13–25 的小幅区间，仅 4.6% 超过 60。像整张重绘而非局部改脸。
- 纯色邻域占比从 v2 的 22.6% 跌到 0.8%，即块面/阶梯感明显下降，接近 v1 的平滑插画方向（v1 为 2.2%）。
- 底色仍不是均匀 `RGB(0,159,62)`：四角 `(12,156,89)`、`(8,162,84)`、`(12,154,86)`、`(9,168,89)`，边缘 6px 带内精确目标绿 0 像素。

这组测量与 §12.6 的两条硬条件直接冲突：“盔、甲、刀、手、脚、站姿和比例与 v2 是同一个兵（明显重绘身体即失败）”与“像素块没有变回写实的皮肤”。但这些只是像素统计，必须由用户看原图判定。

记录（目录 `C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v3\`）：

```text
.attempt1.* / .attempt2.* / .attempt3.* / .attempt4.* / .attempt5.*   （已归档）
enemy1011_face_identity_v3.png / .request.json / .response.json / .status.json   （第 6 次，成功）
```

**未进入去背，也未替换 v2 为验收底。** 若用户判定失败，v2 继续作为当前身份底，不自动重发。

验收对照对（两份都要看，不能只看 v3）：

```text
旧族底（已验收）：C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v2\enemy1011_face_identity_v2.png
本次候选：      C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v3\enemy1011_face_identity_v3.png
```

下一步只取决于用户的并排判定：

- 通过：v3 成为新身份底，后续才另批去背与其他兵种分化。
- 不通过：保留 v2，不自动重发、不本地改脸、不把 v3 当底图。若要再试，必须先改文档并重新取得明确授权。

## 13. v4：只把 v3 的绘制改回当前画风

### 13.1 已确认的两张图

用户确认 v3 面谱正确，同时指出它和当前美术画风不符。v2 的中年面谱也保留，但本轮不编辑 v2。

```text
v3 年轻脸，本轮唯一输入：
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v3\enemy1011_face_identity_v3.png
SHA-256 410394815013894d07621b136b9da5adf95c2de8ea7854dc81229f6cda645000

v2 中年脸，本轮不上传：
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v2\enemy1011_face_identity_v2.png
```

不上传 1011 原图、v1 或 `characterDesign.png`。1011 只在本地对照冷铁块面、锐利高光和阶梯边缘。

### 13.2 只改绘制，不改身份

v3 现在的问题是细格子复古精灵：一像素均匀描边、胸甲小方格、灰面太平、身体偏高。当前画风要的是大色块体积，不是更碎的像素。

保留 v3 的年轻脸、粗眉、窄吊眼、闭嘴、无髭、敞脸盔、红缨、层叠汉甲、环首刀、左右手、正面站姿和短壮比例。

只改这些：

- 用成组的大色块代替细密甲片网格。
- 轮廓用暗部色块和可见台阶，不用一圈均匀细墨线。
- 冷铁灰分成受光面、暗面和少量锐利银白高光。
- 朱红披膊和皮革保留大折面，不铺细纹。
- 手和刀也用大块面，不增加写实手指或刃面。

不改脸型、年龄、表情、武器、姿态、身体比例和画布。身体若被明显重绘，即使块面更对也判失败。

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
image[]: ONLY enemy1011_face_identity_v3.png
prompt: section 13.4 English prompt + blank line + English negative prompt
read timeout: 600 seconds
automatic retries: 0
```

字段组沿用第 6 次已经成功的编辑请求。不改走文生图，不附加第二张参考图。

### 13.4 实际提交文本

中文供审阅，不提交。

只重绘这张已确认刀兵图的绘制语言。保留同一张年轻脸、同一套汉甲结构、同一把环首刀、同一站姿和同一比例。把细密像素格子改成当前游戏的高清像素块面：大色块、可见台阶、受光面与暗面分组、冷铁上少量锐利银白高光。不要更碎的像素，不要均匀细描边，不要写实皮肤。底色保持原图的均匀纯绿，画布保持 1024×1024。

```text
Repaint only the rendering of this approved young infantry sprite into large high-definition pixel blocks. Keep the same young face, heavy brows, narrow eyes, closed mouth, clean upper lip, open helmet, crimson plume, layered Han armor, ring-pommel dao, both hands, stance and proportions.

Replace the tiny square grid and uniform one-pixel outline with chunky grouped planes, stepped edges, distinct light and shadow, and a few sharp silver highlights on the cold iron. Keep crimson cloth and leather in broad folds. Do not add detail, realistic skin, photographic fingers or a reflective blade.

Deliver the same 1024x1024 image on the original uniform green background.
```

```text
Avoid a new face, mustache, older jaw, smile, changed weapon, changed pose, changed proportions, dense lamellar grid, thin uniform ink outline, smooth illustration, realistic skin, scenery, checkerboard and any canvas other than 1024x1024.
```

中文负面词对照：不要新脸、短髭、中年下颌、微笑、改武器、改姿态、改比例、细密甲片网格、均匀细墨线、平滑插画、写实皮肤、场景、棋盘格，以及 1024×1024 以外的画布。

实际 prompt 只拼接本节两个英文代码块，中间一个空行。

### 13.5 输出

```text
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v4\enemy1011_face_identity_v4.png
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v4\enemy1011_face_identity_v4.request.json
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v4\enemy1011_face_identity_v4.response.json
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v4\enemy1011_face_identity_v4.status.json
```

发起前确认这些文件不存在。不覆盖 v2 或 v3。15 秒内若返回泛化 400，立即停止。读超时使用 600 秒；超时后结果不明，不自动重发。

### 13.6 验收

与 v3 并排看：

- 年轻面谱仍是 v3，没有变回 v2 的短髭和中年下颌。
- 甲、肩、刀和轮廓变成更大的色块，细方格和均匀细描边减少。
- 冷铁有受光面、暗面和少量锐利高光，不是一片平灰。
- 武器、左右手、站姿和比例仍是同一个兵。
- 尺寸仍是 1024×1024，底色仍是均匀纯绿。

通过后，才允许另写一次把同一块面转移到 v2 的方案。失败则保留 v3，不自动重发，不进入去背。

### 13.7 v4 第 1 次提交：进入生成但 600 秒读超时，结果未知（2026-10-22）

**已按 §13.3/§13.4 提交一次。请求被接受并进入生成（无 400），但客户端 600 秒读超时，没有拿到响应，v4 未落盘。**

| 项 | 值 |
| --- | --- |
| 时间（UTC） | 2026-10-22T05:23:57Z 开始，601.45s 后超时 |
| 字段 | `model` / `size=1024x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1` / `prompt` + `image[]` |
| 省略 | 仅 `background` |
| 输入 | 只有 v3，SHA-256 `4103948…45000` |
| 词 | §13.4 英文正文 673 字符 + 空行 + 英文负面词 239 字符，SHA-256 `773ef359a28448ace0ab423df4e367b5365402ff76c4ade861b1da802838c188` |
| 结果 | 客户端 `TimeoutError`，无响应体、无 usage、无 task id，无法回取 |

对照：v3 那次同接口同字段组只用 135.36s 就返回，说明本次是服务端排队/生成变慢，不是请求组装问题。

**结果不明，按 §13.5 不自动重发。** 网关不回 task id 或 URL，超时后无法像文生图那样续传或重下同一结果。是否已经计费未知。

记录（目录 `C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v4\`）：

```text
enemy1011_face_identity_v4.request.json
enemy1011_face_identity_v4.status.json   （TimeoutError 记录）
```

未生成 `enemy1011_face_identity_v4.png`，未生成 `.response.json`；v2、v3 未被覆盖。

下一步需用户决定：同一字段与词再提交一次并把读超时抬到 900 秒（新的付费提交），或先等待一段时间再试。不得改词、不得加 `background`、不得换模型、不得改走文生图。

### 13.8 v4 第 2 次提交：连接被服务端关闭，仍无结果（2026-10-22）

用户批准“再次重新发起”后提交一次：字段、词、输入与第 1 次完全相同，唯一改动是读超时 600→900 秒。

| 项 | 值 |
| --- | --- |
| 时间（UTC） | 2026-10-22T05:36:22Z 开始，61.396s 后断开 |
| 字段 | 同 §13.3；仅省略 `background` |
| 输入 | 只有 v3，SHA-256 `4103948…45000` |
| 词 | §13.4 未改，SHA-256 `773ef359…c188` |
| 结果 | `http.client.RemoteDisconnected`：服务端未返回任何响应就关闭连接 |

这次不是 400，也不是超时，而是连接被对端直接掐断。两次失败形态不同（600s 读超时 / 61s 断连），共同点是：请求都发到了服务端，但都没有拿到可回取的结果对象。网关不返回 task id 或 URL，所以两次都无法续传或重下。

**结果不明，不自动重发，不重建任务。** v4 未生成，v2、v3 未被覆盖。

记录（目录 `C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v4\`）：

```text
enemy1011_face_identity_v4.attempt1.request.json / .status.json   （600s 超时）
enemy1011_face_identity_v4.request.json / .status.json            （本次，RemoteDisconnected）
```

两次都未生成 `.png` 与 `.response.json`。

对比参考：v3 用完全相同的接口与字段组（含 `output_format`、`n`）在 135.36s 内正常返回。本轮两次异常更像是服务端在处理这类“重绘风格”编辑请求时不稳定，而非请求本身非法。继续重发只是重复消耗，不能保证拿到结果。

若要继续，应先等一段时间再试，并且**不得**改词、不得加 `background`、不得换模型、不得改走文生图；每次仍只提交一次，读超时保持 900 秒。

### 13.9 v4 第 3 次提交：同一位置断连，模式已复现（2026-10-22）

用户要求“再次发起试试”，字段、词、输入、读超时全部与第 2 次相同（无任何改动）。

| 次序 | 时间（UTC） | 耗时 | 结果 |
| --- | --- | --- | --- |
| 第 1 次 | 05:23:57Z | 601.45s | 客户端读超时（`TimeoutError`），无响应 |
| 第 2 次 | 05:36:22Z | 61.396s | `RemoteDisconnected`，服务端未回任何字节 |
| 第 3 次 | 05:38:56Z | 60.456s | `RemoteDisconnected`，服务端未回任何字节 |

第 2、3 次在几乎相同的秒数（61.4 / 60.5）被对端关闭，**这是可复现的模式，不是随机抖动**。两次的请求体逐字相同（同 prompt SHA `773ef359…c188`、同输入 SHA `4103948…45000`、同字段组）。

对照 v3 那次成功请求（同一接口、同一字段组，135.36s 返回），说明请求形态合法，但**这条特定编辑在当前路由上会越过上游的响应窗口**：要么挂到 600s 超时，要么在 ~60s 被中间层剪断。v3 的那次之所以能回，是因为它自身耗时在窗口内。

三次都未生成 `.png` 与 `.response.json`；v2、v3 未被覆盖。每次是否计费未知。

**已停止，不再连发。** 继续用同一请求重发只会重复产生不可回取的结果。

若要再拿到 v4，必须先改文档并重新取得授权，可选方向（均需明确批准）：

1. 缩短正文与负面词，去掉最啰嗦的描述段（改词，需重定 prompt SHA）。
2. 去掉 `input_fidelity=high`（参数变更）。
3. 改走 `image` 字段名或改用 `/v1/images/edits` 以外的分段提交（尚未验证）。
4. 先不做 v4，改为把“当前画风”的块面要求写进一份新的文生图方案（会重绘身体，违反 §13.2 的“不改身体”，需用户明确改口径）。

在用户给出方向前，v3 仍是当前唯一可用于后续的图，v2 仍保留为已验收底。

## 14. v4 改方案：压缩词 + 去掉 input_fidelity（2026-10-22 授权）

### 14.1 依据与授权

用户同意授权改方案并发起。触发点是 §13.7–§13.9：同一请求三次分别在 600s 读超时、61.4s 断连、60.5s 断连，位置可复现，请求体逐字相同。对照 v3 同接口同字段组 135.36s 正常返回，说明这条编辑在当前路由越过上游响应窗口。

### 14.2 相对 §13.3/§13.4 只改两处

1. 正文与负面词压缩，去掉重复描述，降低后端处理量。
2. 去掉 `input_fidelity=high`，降低保真计算开销。

其余全部不变：同一模型、同一尺寸、`quality=max`、`output_format=png`、`n=1`、不传 `background`、`image[]` 仍只有 v3 原图、读超时 900 秒、0 自动重试。不改走文生图，不附加第二张图。

去掉 `input_fidelity=high` 意味着模型对输入图的约束变弱，重绘幅度可能变大。这是本方案已知的代价，由 §14.6 的验收兜住：若身体被明显重绘即判失败。

### 14.3 参数

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
output_format: png
n: 1
background: omit
input_fidelity: omit
image[]: ONLY enemy1011_face_identity_v3.png
prompt: section 14.4 English prompt + blank line + English negative prompt
read timeout: 900 seconds
automatic retries: 0
```

### 14.4 实际提交文本

中文供审阅，不提交。

只重绘这张已确认年轻刀兵图的绘制语言。保留同一张年轻脸、粗眉、窄眼、闭嘴、无髭、敞脸盔、红缨、层叠汉甲、环首刀、双手、站姿和比例。把细密小方格和一圈均匀细描边换成大块面、可见台阶、清楚明暗，以及冷铁上的少量银白高光。朱红布与皮革保持大折面。不加细节，不画写实皮肤、照片级手指或反光刃面。画布与底色不变。

```text
Repaint this approved young infantry sprite in large pixel blocks. Keep the same young face, heavy brows, narrow eyes, closed mouth, clean upper lip, helmet, crimson plume, layered Han armor, ring-pommel dao, hands, stance and proportions. Replace the tiny square grid and thin outline with chunky grouped planes, stepped edges, clear light and shadow, a few sharp silver highlights on cold iron. Keep crimson cloth and leather in broad folds. No added detail, no realistic skin, no photographic fingers or blade. Same 1024x1024 canvas and green background.
```

```text
Avoid new face, mustache, older jaw, smile, changed weapon, pose or proportions, dense lamellar grid, thin ink outline, smooth illustration, realistic skin, scenery, checkerboard, other canvas size.
```

实际 prompt 只拼接本节两个英文代码块，中间一个空行。

### 14.5 输出

沿用 §13.5 的 v4 目录，已有 attempt1/attempt2 记录全部保留，不覆盖：

```text
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v4\enemy1011_face_identity_v4.png
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v4\enemy1011_face_identity_v4.request.json
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v4\enemy1011_face_identity_v4.response.json
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v4\enemy1011_face_identity_v4.status.json
```

### 14.6 验收

与 v3 并排看，条件同 §13.6：年轻面谱仍是 v3，没有变回 v2 的短髭和中年下颌；甲、肩、刀和轮廓变成更大的色块，细方格与均匀细描边减少；冷铁有受光面、暗面和少量锐利高光；武器、左右手、站姿和比例仍是同一个兵；尺寸仍是 1024×1024，底色仍是均匀纯绿。

因去掉了 `input_fidelity=high`，额外看一条：若身体、甲片形状或站姿被明显重绘，即使块面更对也判失败。失败则保留 v3，不自动重发，不进入去背。

### 14.7 §14 方案第 1 次提交：仍在 ~60s 断连（2026-10-22）

| 项 | 值 |
| --- | --- |
| 时间（UTC） | 2026-10-22T05:43:58Z 开始，63.308s 后断开 |
| 字段 | `model` / `size=1024x1024` / `quality=max` / `output_format=png` / `n=1` / `prompt` + `image[]` |
| 省略 | `background`、`input_fidelity` |
| 输入 | 只有 v3，SHA-256 `4103948…45000` |
| 词 | §14.4 压缩版：正文 557 字符 + 空行 + 负面词 198 字符，SHA-256 `9f0352fd957224945e8d6b01d1a02a41e750bb79a6aab8fe18af751ff9c92a6a` |
| 结果 | `RemoteDisconnected`，服务端未回任何字节 |

关键结论：**压缩词与去掉 `input_fidelity=high` 都没有改变结果。** 断连时间在四次请求里分别落在 601s（挂死）、61.4s、60.5s、63.3s，与词长、保真设置都无关。因此 ~60s 断点不是“后端处理太慢”，而是中间层对“首字节前静默 60 秒”的固定掐断（idle timeout）。

这同时解释了一个矛盾：v3 那次成功请求耗时 135.36s 却能返回——说明断连阈值按路由而异，落到的路由不同，结果就不同。路由选择是间歇性的、不可控的。

四次 v4 记录（目录 `C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v4\`）：

```text
.attempt1.* （600s 超时，§13 原词 + input_fidelity=high）
.attempt2.* （61.4s 断连，§13 原词 + input_fidelity=high）
.attempt3.* （60.5s 断连，§13 原词 + input_fidelity=high）
enemy1011_face_identity_v4.request.json / .status.json   （本次，63.3s 断连，§14 压缩词，无 input_fidelity）
```

v4 仍未生成 `.png` 与 `.response.json`；v2、v3 未被覆盖。

已停止，不再连发。后续可选方向（需用户逐次授权，每次只发一次）：

1. 隔开时间重发同一请求，靠路由轮换命中可用后端（v3 已证明同一类请求能成功）。
2. 试 `stream=true`（若网关支持流式，首字节后即可保活连接）：属于新增参数，未验证，且会改变响应解析方式。
3. 改用 §13.2 的替代口径：先不修 v3，而是用新文生图直接产出“年轻脸 + 当前画风块面”的版本（会重绘身体，需用户明确改口径并接受）。
4. 暂停 v4，先把 v3 用于后续流程（去背、兵种分化），画风问题另立专项。

在用户定方向前，v3 仍是当前唯一可用图，v2 保留为已验收底。

## 15. 新对话接续指引

### 15.1 一句话现状

v2（中年脸）已验收；v3（年轻脸）面谱已确认但绘制语言不符；v4（粗块面）已出图但**被否决**；**v5 是当前身份基准**（细密高清像素，§16）；§18 单张盾卫已取消；§19 民兵+官兵组图未遭否决；**§20 六脸组图已验收**；**§21 六兵种全身组图已验收**；§22–§26 五次面相尝试均不通过（变黑 / 歪嘴 / 写实 / 只改轮廓两轮）；**§27 保持画风换五官的方案已出图（`enemy_face_sheet_v5`）、等验收**；§17 换人视频已出片、待目检。早期失败（v4 前 9 次）不是请求不合法，而是网关节点随机 idle 阈值掐断（§15.13，结论见 `memory/gpt-image-edits-route-node-lottery.md`）。**§28 记录了 2026-10-23 新增的三批参考图及其使用边界，下一版词的取材以 §28 为准。**

### 15.2 现有文件（路径 + 校验值）

```text
v1（失败：太写实、伪像素感）  C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v1\enemy1011_face_identity_v1.png
    1254x1254, 1,626,967 bytes, SHA-256 d39dd7e64860cb8de5e3e52353524108c8d46758e5aef5f6ef515f90149a3382

v2（已验收：中年脸，块面较好）  C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v2\enemy1011_face_identity_v2.png
    1024x1024, 721,299 bytes, SHA-256 810abc4145de7d5ea02705fdfb35b9a14978174cd045d2bc210c6fc62e2b64ee

v3（已出图：年轻脸，绘制语言待改）C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v3\enemy1011_face_identity_v3.png
    1024x1024, 1,000,204 bytes, SHA-256 410394815013894d07621b136b9da5adf95c2de8ea7854dc81229f6cda645000
    v3 目录另含 attempt1-5 归档记录；v3 目录本身属于 v3 那次生成。

v4（已否决：仍是低分辨率粗格）C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v4\enemy1011_face_identity_v4.png
    1024x1024, 1,197,209 bytes, Color Type 2, SHA-256 373684b7ee1b047b3a2bc02ebeb08f123c94300fd19bd222d3548de700b5d632
    第 10 次提交命中，66.901s；用户结论：没有修正画风，仍是低分辨率像素，保留为失败结果，不得重发也不当风格参考
    备选输入（已实测无效，勿再当正式输入用）：v3_downscale_768.png (702,069 B)、v3_downscale_512.png (330,540 B)

v5（当前基准）               C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v5\enemy1011_face_identity_v5.png
    1024x1024, 1,428,511 bytes, Color Type 2, SHA-256 e77fa076017788b708415f600b805a5557f9c82ce7e0f6480c960c82c99c049e
    第 1 次提交即成功，70.925s；usage input 550 / output 7024 / total 7574
    对照图：v3_v4_v5_three_way.png、v3_vs_v5_chest_crop_2x.png、v4_vs_v5_chest_crop_2x.png

§18 盾卫（已取消，仅存档）  C:\Users\steam\Pictures\gptGen\enemy1021_face_identity_v1\enemy1021_face_identity_v1.png
    1024x1024, 1,461,247 bytes, SHA-256 9af235d9ef8fffe8572e5a4264224b5a816996a1111a4b8941f46b926da0d37b
    单张盾卫方向已被用户取消，不得再上传、不得当参照

§19 组图（未遭否决）        C:\Users\steam\Pictures\gptGen\enemy_rank_sheet_v1\enemy_rank_sheet_v1.png
    1536x1024, 2,041,913 bytes, Color Type 2, SHA-256 d2f4c44afdddfa322da7158cf530c37e332089dbc0470c4657276bf216b247ee
    第 2 次提交成功，51.792s；attempt1 为进程被杀的结果未知记录

§20 六脸组图（已验收）        C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v1\enemy_face_sheet_v1.png
    1536x1024, 1,904,226 bytes, Color Type 2, SHA-256 ff5f23bfbfb80f46abe433cf0ac153cb11c9700ce4e5d6ae7114a4a60ce3b03d
    第 1 次提交即成功，95.877s；usage input 311 / output 5488 / total 5799

§21 六兵种全身组图（已验收）  C:\Users\steam\Pictures\gptGen\enemy_class_sheet_v1\enemy_class_sheet_v1.png
    1536x1024, 2,469,109 bytes, Color Type 2, SHA-256 d209c40e5d433232f45cd3dcbbb48ccc07ff40188e53013b6c049eda26727618
    第 1 次提交即成功，53.361s；输入是 §20 的六脸图（sha `ff5f23bf…`）

§22 六兵种敌意脸（不通过）    C:\Users\steam\Pictures\gptGen\enemy_class_sheet_v2\enemy_class_sheet_v2.png
    1536x1024, 2,201,870 bytes, SHA-256 51971dfc763f3ca72b98040ca50969917f2e02377fc418634bc473f5c4e4f4be
    用户结论：只是脸变黑、像矿工、更老，未形成面相变化

§23 只改五官形状（不通过）    C:\Users\steam\Pictures\gptGen\enemy_class_sheet_v3\enemy_class_sheet_v3.png
    1536x1024, 2,182,322 bytes, SHA-256 4da5af91fe4b8947e920e194acc0a9f4157b34f34a7b51ba4c3753d80f649180
    用户结论：与 §21 几乎无变化，全身小脸上的形状词不够

§24 六张敌意大头（不通过）    C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v2\enemy_face_sheet_v2.png
    1536x1024, 2,016,482 bytes, SHA-256 0995fcd33e2912c2d91ddb718c30d757a5629716035b6bf505c497b507c07cfa
    用户结论：只是集体歪嘴，等于换表情，没有新面相

§25 无参考文生图（不通过）    C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v3\enemy_face_sheet_v3.png
    1536x1024, 2,122,829 bytes, SHA-256 515505aba637def381ef4f2090448bd79e63e7bce197ccd18a8de884daa16f62
    用户结论：画风被换成写实，六张脸也没形成目标差异

§26 只换脸轮廓（不通过）      C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v4\enemy_face_sheet_v4.png
    1536x1024, 1,981,706 bytes, SHA-256 663ecf0ad60745f704d1ad9a8c0ac8f0a657805274d116301e5e414bed99ff0b
    用户结论：只有外轮廓变化，眉眼鼻嘴基本没动

§27 保持画风换五官（等验收）  C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v5\enemy_face_sheet_v5.png
    1536x1024, 2,047,820 bytes, SHA-256 035fa264279f9ac1691d952edbebc21f667aa2e05d853d1f1a6f5c4a1a8f0c32
    第 1 次提交即成功，31.745s；对照图 feat_v1_vs_v5_side_by_side.png / feat_v1_v5_top_2x.png / feat_v1_v5_bottom_2x.png

§17 换人视频（已出片，等目检）C:\Users\steam\Videos\doubaoVideo\enemy1011_v5_walk_replace_v1.mp4
    3,009,320 bytes, SHA-256 2b93557fa536651908831d8f0cd95678d560fedebab8a154afe29a4343d4ce6e
    4.042s / 960x960 / 97 帧，与源片规格完全一致；task_76a357745eb64247be7f369e9b60a29e
```

### 15.3 v4 十次提交的事实

| 次序 | 方案 | 耗时 | 失败 |
| --- | --- | --- | --- |
| 1 | §13 原词 + `input_fidelity=high` | 601.45s | 客户端读超时 |
| 2 | 同上 | 61.396s | RemoteDisconnected |
| 3 | 同上 | 60.456s | RemoteDisconnected |
| 4 | §14 压缩词 + 去掉 `input_fidelity` | 63.308s | RemoteDisconnected |
| 5 | §14 压缩词 + 恢复 `input_fidelity=high`，隔 19 分钟后重发 | 60.597s | RemoteDisconnected |
| 6 | §14 压缩词 + `input_fidelity=high` + `stream=true` | 2.019s / 62.128s | 第一次 TLS 握手中断；重试 RemoteDisconnected，0 事件 |
| 7 | §14 压缩词 + `input_fidelity=high`（原 1024 输入） | 136.455s | RemoteDisconnected |
| 8 | 第 7 次 + `stream=true` | 148.171s | **HTTP 504 upstream_timeout**（拿到 error body） |
| 9 | §14 压缩词 + 512×512 缩图输入（body 332KB） | 71.295s | RemoteDisconnected |
| 10 | §14 压缩词 + 原 1024 输入（campaign 第 1 发） | 66.901s | **成功，HTTP 200** |

判断：压缩词、降低保真、时间间隔、流式、缩小输入体量都不改变结果，断点与词长/参数/时机/体量均无关；v3 同接口同字段组曾 135.36s 正常返回。结论是**网关前面挂着一批阈值不同的节点（实测 60.5 / 60.6 / 61.4 / 62.1 / 63.3 / 71.3 / 136.5 / 148.2 秒，另有 >300s），落到哪个节点是随机的**。不是请求非法，也不是 400。

### 15.4 下一步（需用户逐次授权，每次只发一次）

1. **当前卡点：用户按 §27.6 验收 `enemy_face_sheet_v5`。** 看 `feat_v1_vs_v5_side_by_side.png`、`feat_v1_v5_top_2x.png`、`feat_v1_v5_bottom_2x.png`。
2. 通过：该五官组作为兵种脸部基准；下一步可另批把新五官回贴到 §21 全身图。
3. 不通过：保留记录，不自动重发；先改本文再重新取得明确授权。
4. 可选方向（需新章节）：把 §21 六兵种拆成单个角色并做战斗动作；阶段 B 语义去背；拆脸型图做头像。
5. §17 换人视频需用户目检（画面是否真换成 v5、动作是否不变）；通过后才谈抽帧。
6. 去背、选帧、抠图、对齐、补边、Clip、Unity 接入各自另批，不能用身份图通过代替。
7. v4 已否决（不得重发、不当参考）；§18 单张盾卫已取消；§19 组图未遭否决；§22–§26 均不通过，均不当输入。
8. **参考图台账见 §28**（2026-10-23 更新，用户已清理无用图）。下一版词的取材按 §28.5；结构表核对通过后才写词。

### 15.5 硬规则（继承，不因换对话而失效）

- 一次只提交一个请求；失败、超时、结果不明就保留记录并停下，不自动重试、不降档、不换模型、不改词再发。**唯一例外：用户明确批准的“连发 campaign”——每次仍是一个完整请求、顺序执行、命中即停、每次付费、逐次落记录。**
- 不上传 v1、1011 原图、`characterDesign.png`；不附加第二张参考图；不改走文生图（除非用户明确改口径）。
- 不覆盖任何已出图产物（v2–v5、§18 盾卫、§19 组图、§20 脸型组图、§21 兵种组图、§22/§23 组图、§24/§25/§26/§27 脸图、§17 视频）；新产物一律写新目录。凭据只从环境变量读（`MUSK_API_KEY` / `REACH_API_KEY`），不写进脚本、记录、文档或回复。
- 发起侧可以读取并展示 PNG 附件（read 工具支持图片），但**验收结论由用户下**；发图必须给真实完整路径与校验值，不能只说“已生成”。
- 发请求前先向用户复述：唯一输入、参数字段组、实际提交文本、输出路径，并取得当次明确批准；读本文或改文档不等于批准。
- 不把“像素测量”当验收。机器数字（flat3x3、变化率、底色均匀度）只作旁证，尤其 flat3x3 对带噪点的图会低估块面变化。
- **工具回显不可信时必须回读磁盘。** 本日多次出现返回内容重复、与磁盘不符的情况；每次写文件/发请求后都要用 `ls`、哈希、重读 JSON 确认，不能拿返回值当结论。

### 15.6 发起侧可复用的调用要点

- 端点 `POST https://api.muskapis.com/v1/images/edits`，`multipart/form-data`，图片字段名 `image[]`，`Content-Type: image/png`。
- 字段组（v3 与 v4 成功那两次都用它）：`model=gpt-image-2.5-sunburst`、`size=1024x1024`、`quality=max`、`input_fidelity=high`、`output_format=png`、`n=1`，不传 `background`。
- 读超时给 900 秒；生成实测 50–600 秒级。
- 模型可用性用免费 `GET /v1/models` 核（不计费），不要用付费探针。
- 注意：非 200 路径通常不写 `.response.json`，但 **504 会写 error body**；不要把“没有 response 文件”当成没发过请求，以 `.status.json` 为准。
- **节点 lottery 是最重要的一条**：edits 路由前面有一批阈值不同的网关节点，被掐时间实测 60.5 / 60.6 / 61.4 / 62.1 / 63.3 / 71.3 / 136.5 / 148.2 秒，另有 >300s；与 body 大小、词长、参数、间隔、流式都无关。拿不到图只能顺序重发碰运气。
- **命中一次很贵，命中那次要用最好的输入。** 缩图不提高成功率（第 9 次实测：body 砍到 332KB 仍被切在 71.3s），只会损失身份保真；不要拿缩图/降质去赌唯一的机会。
- **`stream=true` 没用**：网关在掐断前不吐任何 SSE 事件，没有流量可以保活连接。
- **可复用脚本**（`Library/Locus/tmp/enemy1011_face_identity_v4/`）：`run_v4_campaign.py`（连发 campaign，顺序执行、命中即停）、`submit_v4_attempt5.py`（单发，带 `--dry-run`、可 `--image` 换输入）、`measure_face_chain.py`（像素测量）、`compare_v3_v4.py`（并排图与 2× 裁切）、`downscale_png.py`、`reencode_png_lossless.py`、`probe_health.py`（连通性探针）。
- **本机没有 PIL/numpy**：PNG 读写用 stdlib `zlib` 自实现（`measure_face_chain.decode_png` / `downscale_png.write_png`）。注意无损重编码**不能**缩小 v3（zlib level 9 + 逐行选过滤器后反而从 1,000,204 涨到 1,292,617 字节）。

### 15.7 v4 第 5 次提交：隔 19 分钟重发仍 60.6s 断连（2026-10-22）

**按 §15.4 选项 1 执行：隔开时间重发同一请求（§14.4 词逐字未变），只提交一次。结果与第 2、3、4 次完全同型：约 60 秒被对端掐断。v4 仍无产物。**

| 项 | 值 |
| --- | --- |
| 时间（UTC） | 2026-10-22T06:03:44Z 开始，60.597s 后断开 |
| 距第 4 次 | 19.8 分钟（跨过了 05:36–05:44 那个连续失败窗口） |
| 字段 | `model` / `size=1024x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1` / `prompt` + `image[]` |
| 省略 | 仅 `background` |
| 输入 | 只有 v3，SHA-256 `4103948…45000` |
| 词 | §14.4 压缩版逐字未改，SHA-256 `9f0352fd…c92a6a`（757 字符，从第 4 次记录里逐字读出，不重新誊写） |
| 结果 | `RemoteDisconnected`，服务端未回任何字节 |

与第 4 次的唯一差异：恢复 `input_fidelity=high`。理由是 §14.7 已证明去掉它不影响断连，而它是 v3 唯一拿到 HTTP 200 的字段组，同时能压住 §14.2 提到的“身体被重绘”风险。**结果证明这个改动同样不影响断连位置**，也说明当前失败与保真设置无关。

这次把“时机”也排除了：19 分钟间隔跨过了此前连续失败的时间窗，断点依旧落在 60.6 秒。结合 5 次记录，断点稳定在 60.5–63.3 秒，只有第 1 次例外（601 秒挂到客户端超时）。

免费前置检查（不计费）：`GET https://api.muskapis.com/v1/models` 返回 200，令牌可用，图像模型仍只有 `gpt-image-2.5-sunburst`。

**已停止，不自动重发。** 记录（目录 `C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v4\`）：

```text
.attempt1.* / .attempt2.* / .attempt3.* / .attempt4.*   （已归档）
enemy1011_face_identity_v4.request.json                （第 5 次）
enemy1011_face_identity_v4.status.json                 （第 5 次，RemoteDisconnected）
```

第 5 次没有生成 `.png` 与 `.response.json`；v2、v3 未被覆盖（提交前后哈希复核一致）。是否已计费未知。

发起侧脚本留在 `Library/Locus/tmp/enemy1011_face_identity_v4/submit_v4_attempt5.py`（含 `--dry-run` 自检模式），像素测量工具在同目录 `measure_face_chain.py`。

### 15.8 v4 第 6 次提交：`stream=true` 仍断连（2026-10-22）

**用户选定 §15.4 选项 2（试 `stream=true`）。共发两次：第一次在 TLS 握手阶段就断（请求未进入应用层），第二次完整发出后仍在 62.128s 被掉线，且全程 0 事件。**

| 次 | 时间（UTC） | 耗时 | 结果 |
| --- | --- | --- | --- |
| 6a | 06:07:58Z | 2.019s | `URLError`：`SSL: UNEXPECTED_EOF_WHILE_READING`，握手阶段断开，0 字节请求体确认送达 |
| 6b | 06:09:19Z | 62.128s | `RemoteDisconnected`，`first_byte_seconds=null`，`events_seen=0`，`partial_count=0` |

字段组 = 第 5 次 + `stream=true`（新增字段），其余全同：`model` / `size=1024x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1`，不传 `background`；输入仍只有 v3（`4103948…45000`）；词仍为 §14.4 逐字未改（`9f0352fd…c92a6a`）；读超时 900 秒；0 自动重试。请求头加了 `Accept: text/event-stream, application/json`。

**结论：`stream=true` 不能解决掐断。** 掐断发生在任何首字节之前，网关不吐 SSE 事件，因此流式没有任何东西可以保活连接。6a 之后做的免费检查（`GET /v1/models` 连续 3 次）均 200、耗时 1.5–1.9s，说明 6a 是一次瞬时 TLS 抖动，与请求内容无关。

记录（目录 `C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v4\`）：

```text
.attempt1.* ~ .attempt5.* / .attempt6a.*   （已归档）
enemy1011_face_identity_v4.request.json    （第 6 次重试）
enemy1011_face_identity_v4.status.json     （第 6 次重试，RemoteDisconnected）
```

仍未生成 `.png` 与 `.response.json`；v2、v3 未被覆盖。

### 15.9 观察到的另一条相关线索（已被 §15.10 推翻，保留记录）

把所有记录排在一起后，请求体大小与成败高度相关：

| 请求 | 源图字节 | multipart body | 结果 |
| --- | --- | --- | --- |
| v3 第 6 次（成功，135.36s） | 721,299 | 723,368 | HTTP 200 |
| v4 第 1–6 次 | 1,000,204 | 1,001,843 ~ 1,002,307 | 全部 ~60s 掉线 |

项目里其他成功的 `/v1/images/edits` 提交上传的输入图都在 172KB ~ 345KB 量级，也没有一次成功提交上传过 ~1MB 的图。

这条线索**与时间线混淆**：v3 成功后到 v4 第一次失败之间隔了约 1 小时，无法区分“请求体跨过某个阈值”与“后端进入了一段劣化期”。而且 6 次失败的 body（1,001,843 ~ 1,002,307）刚刚越过 1,000,000 字节（十进制 1MB），成功的都在 723KB。

已验证无法用无损重编码缩小 body：把 v3 用 zlib level 9 + 逐行选最优过滤器重新编码，得到 1,292,617 字节，比原文件还大（源文件已压得更好），且像素逐点一致。像素完全一致的小体积 PNG 走不通。

因此若要检验这条线索，只能降分辨率上传（例如 768×768，body 约 600KB），这会改变输入像素，输出保真度下降，与 §13.2 的“不改身体”口径冲突，需要用户明确批准。

**当前状态：v4 仍无产物；v2 保留为已验收底；v3 保留为唯一可用图。** 下一步需要用户定方向。

### 15.10 三路健康探针：路径没问题，1MB 假说被推翻（2026-10-22）

用户告知“另一台设备能跑通生图”，要求只做连通性测试、不碰任务内容。于是发三个与任务无关的探针（无任务图、无任务词、无任务输出路径）：

| 探针 | 路由 / 输入 | 结果 |
| --- | --- | --- |
| P0 | `GET /v1/models` | 200，1.035s，图像模型仍只有 `gpt-image-2.5-sunburst` |
| P1 | `POST /v1/images/generations`，词=“绿底上一个红圆”，无输入图 | **HTTP 200，165.518s**，1024×1024，Color Type 2，850,463 字节，usage 总 7213 |
| P2 | `POST /v1/images/edits`，输入=178 字节合成小 PNG，词=“把红方块改成蓝圆” | **HTTP 200，49.772s**，1024×1024，Color Type 2，926,610 字节，body 1,238 字节，usage 总 7370 |
| P3 | `POST /v1/images/edits`，输入=978,306 字节合成噪声 PNG，词同上 | **客户端 300s 读超时**，body 979,364 字节 |

结论三条：

1. **本机路径健康。** 文生图能跑（还跑了 165.518s，超过 60s 没被切），edits 路由也能跑，且 P2 的产物正确：红方块被改成蓝圆、位置与绿底不变。所以设备、网络、令牌、模型、路由都没有问题。
2. **§15.9 的“~1MB 体量触发 60s 掐断”假说被推翻。** P3 用了 979,364 字节的 body，**没有**在 60 秒被掉线，而是一直挂到我自己设的 300s 客户端超时。掐断不是体量阈值。
3. **“首字节前静默 60 秒被切”不是本机路径的固定属性。** P1 静默 165 秒仍正常返回。

因此 v4 的 6 次连续掉线，只能归因于**那一份特定输入 + 特定词的组合**，或**当时那段后端劣化期**（05:23–06:10）；06:39 之后后端明显恢复（P1、P2 连续 200）。

产物与记录（已从临时目录拷到项目 scratch，避免被清理）：

```text
Library/Locus/tmp/enemy1011_face_identity_v4/probe/probe_P1_generations.png   （850,463 字节，绿底红圆）
Library/Locus/tmp/enemy1011_face_identity_v4/probe/probe_P2_edits_small.png   （926,610 字节，红方块→蓝圆）
Library/Locus/tmp/enemy1011_face_identity_v4/probe/probe_big_input.png        （978,306 字节，P3 合成输入）
Library/Locus/tmp/enemy1011_face_identity_v4/probe/probe_small_input.png      （178 字节，P2 合成输入）
Library/Locus/tmp/enemy1011_face_identity_v4/probe/probe_results.json
Library/Locus/tmp/enemy1011_face_identity_v4/probe_health.py                   （探针脚本）
```

P3 的请求可能仍在服务端生成（客户端先超时），但没有可回取的结果对象，不重建。

**当前状态：v4 仍无产物；v2 是已验收底，v3 是唯一可用图。** 下一步若获授权，最直接的动作是在后端恢复窗口里原样重发一次 v4（§14.4 词、§12.12 字段组、输入 v3、读超时 900s、0 重试）。

### 15.11 v4 第 7 次提交：断了 136 秒，不再是 60 秒（2026-10-22）

用户指示“按任务重发试试”，于是按 §12.12 字段组原样提交一次（§14.4 词、输入 v3、900s 读超时、0 重试）。**断点位置变了。**

| 项 | 值 |
| --- | --- |
| 时间（UTC） | 2026-10-22T06:56:04Z 开始，136.455s 后断开 |
| 字段 | `model` / `size=1024x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1` / `prompt` + `image[]` |
| 省略 | 仅 `background` |
| 输入 | 只有 v3，SHA-256 `4103948…45000` |
| 词 | §14.4 逐字未改，SHA-256 `9f0352fd…c92a6a` |
| 结果 | `RemoteDisconnected` |

这是 7 次里第一次跑过 ~63 秒：**136.455s 几乎等于 v3 成功那次的 135.355s**。两种读法：

1. 断点阈值确实随路由变（支持 §15.3 的结论）：这次落在长窗口路由上，只是仍然差一点。
2. **上游生成其实在 ~136s 就完成了，结果在回传途中丢的。** 若如此，v4 的上游耗时与 v3 相当，输得起的关键不是“等更久”，而是“让回传不被掐断”。

这改变了下一步的思路：从“碰运气等长窗口路由”转向“保证回传期间连接有流量”。因此第 8 次改用 `stream=true`：如果网关在生成期间吐 SSE 事件（partial image），连接就有流量、可保活；即使没有 completed 事件，至少能拿到接近完成的 partial 图。

**当前状态：v4 仍无产物；v2 是已验收底，v3 是唯一可用图。**

### 15.12 v4 第 8 次提交：`stream=true` 拿到真响应：HTTP 504 upstream_timeout（2026-10-22）

| 项 | 值 |
| --- | --- |
| 时间（UTC） | 2026-10-22T07:02:27Z 开始，148.171s 后收到响应 |
| 字段 | 第 7 次字段组 + `stream=true` |
| 输入 / 词 | v3（`4103948…45000`）/ §14.4 逐字未改（`9f0352fd…c92a6a`） |
| 结果 | **HTTP 504** `{"error":{"message":"请求超时，请稍后重试 (request id: 20261022070251178824404JD70KSKJ)","type":"upstream_timeout","code":"upstream_timeout"}}` |

这是 8 次里第一次拿到**真实 HTTP 错误体**（之前是连接被掐，没有响应体）。它把整件事的形状说清楚了：

**edits 路由有一个 ~148 秒的上游截止时间，v4 这个作业跑不过去。**

- v3 成功那次：135.355s，刚好卡在截止线内 → 200。
- v4 第 7 次：136.455s 被掐（已经越过 v3 的耗时，只差一点点）。
- v4 第 8 次：148.171s 被网关正式判为 upstream_timeout。
- 之前第 2–6 次在 ~60s 被掐：那是不同网关节点的更短阈值（60s / 136s / 148s 三种都出现过），节点选择是间歇的。

把 §15.10 的探针并进来看，**输入图的体量/细节量就是决定上游耗时的变量**：

| edits 请求 | 输入体 | 上游耗时 | 结果 |
| --- | --- | --- | --- |
| P2 探针 | 178 字节 | 49.772s | 200 |
| v3 第 6 次 | 721,299 字节 | 135.355s | 200 |
| P3 探针 | 978,306 字节 | > 305s | 客户端超时 |
| v4 第 1–8 次 | 1,000,204 字节 | > 60s，最多跑到 148s | 掉线 / 504 |

所以 v4 并不是“请求不合法”或“本机不通”，而是**上游作业太重，跑不过 ~148 秒的截止线**。v3 能成是因为它的输入是 721KB 的 v2 图；v4 的输入是 1MB 的 v3 图，而这张图恰好是细密 1px 格子质感的图，上游处理最贵。

**因此下一步有了第一个真正有依据的改动：把输入体降下来。** 已用区域平均缩放备好两个候选（构图、比例、颜色不变，只降分辨率与 1px 噪声）：

```text
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v4\v3_downscale_768.png   702,069 字节，768x768，body ≈ 704KB（低于已知可用的 721KB）
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v4\v3_downscale_512.png   330,540 字节，512x512，body ≈ 333KB
```

缩到 768 后脸部五官（粗眉、窄眼、鼻影、闭嘴）仍可辨认，但比原图软了一点，身份漂移风险比原图高一点；512 更软。

另一条未动的杠杆：`quality=max` 是最贵档位，降档会直接缩短上游耗时，但项目规则（`skill/gpt-image-generation.md`）要求默认 `quality=max`，未经明确批准不降。

**当前状态：v4 仍无产物；v2 是已验收底，v3 是唯一可用图。**

### 15.13 v4 第 9 次提交：512 输入仍被切在 71.3s，体量假说作废（2026-10-22）

用户选定“512×512 输入重发”。提交一次，字段组同 §12.12，词 §14.4 逐字未改，只有 `image[]` 换成 `v3_downscale_512.png`（330,540 字节，sha256 `4d66b552…c1f0f8`）。

| 项 | 值 |
| --- | --- |
| 时间（UTC） | 2026-10-22T07:12:53Z 开始，71.295s 后断开 |
| body | 332,293 字节（从 1,001,967 降了 67%） |
| 结果 | `RemoteDisconnected`，仍无产物 |

**这作废了“体量决定上游耗时”的读法。** 输入小了三分之二，被掐的时间不但没变短，还落在了一个**从没出现过的新阈值 71.3 秒**上。

把 9 次 v4 提交 + 3 次探针的被掐/完成时间排在一起，能看出真正的形状：

| 提交 | body | 耗时 | 结果 |
| --- | --- | --- | --- |
| P2 探针 | 1,238 B | 49.772s | 200 |
| v3 第 6 次 | 723,368 B | 135.355s | 200 |
| v4 第 2–6 次 | ~1,001,9xx B | 60.456 / 60.597 / 61.396 / 62.128 / 63.308s | 掉线 |
| v4 第 9 次 | 332,293 B | 71.295s | 掉线 |
| v4 第 7 次 | 1,001,967 B | 136.455s | 掉线 |
| v4 第 8 次 | 1,002,075 B | 148.171s | 504 upstream_timeout |
| v4 第 1 次 | 1,001,843 B | 601.45s | 客户端超时 |
| P3 探针 | 979,364 B | >305s | 客户端超时 |
| P1 探针（generations） | 无输入 | 165.518s | 200 |

**被掐的时间点（60.5 / 60.6 / 61.4 / 62.1 / 63.3 / 71.3 / 136.5 / 148.2）与 body 大小、与词长都无关。** 结论：网关前面挂着一批**阈值不同的节点**（能看到 ~60s、~71s、~136s、~148s、>300s 好几种），请求落到哪个节点是间歇随机的。能不能活下来，取决于“落到哪个节点” × “上游作业要跑多久”这两个独立随机量。

因此：

1. **减小输入不是一个有效的提速手段**（已实测：body 降 67%，仍被切）。它只带来验收风险（脸变软），不带来成功率。
2. **真正可行的做法是重发，碰上一次阈值宽松的节点。** v3 就是这么成的（135.355s，刚好卡在它那个节点的线内）。
3. 既然命中一次很难，**命中那一次应该用最好的输入**（原 1024 图）——用 512 去赌唯一的好机会不划算。

**当前状态：v4 仍无产物；v2 是已验收底，v3 是唯一可用图。**

### 15.14 v4 第 10 次提交：成功出图（2026-10-22，campaign 第 1 发就中）

用户选定“原图连发 4 次、命中即停”，于是按固定请求（§14.4 词 + §12.12 字段组 + 原 1024 v3 图 + 900s 读超时）顺序连发，间隔 150s，命中即停。**第 1 发即命中。**

| 项 | 值 |
| --- | --- |
| 时间（UTC） | 2026-10-22T07:17:39Z 开始，66.901s 返回 HTTP 200 |
| 字段 | `model` / `size=1024x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1` / `prompt` + `image[]` |
| 省略 | 仅 `background` |
| 输入 | 只有 v3，SHA-256 `4103948…45000` |
| 词 | §14.4 逐字未改，SHA-256 `9f0352fd…c92a6a` |
| 产物 | 1024×1024，Color Type 2，1,197,209 字节，SHA-256 `373684b7ee1b047b3a2bc02ebeb08f123c94300fd19bd222d3548de700b5d632` |
| usage | input 512（image 323 / text 189），output 7024，total 7536 |
| 路径 | `C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v4\enemy1011_face_identity_v4.png` |

这坐实了 §15.13 的节点 lottery：同样的请求字节，前 9 次分别死在 60.5 / 60.6 / 61.4 / 62.1 / 63.3 / 71.3 / 136.5 / 148.2 秒和一次 TLS 断，第 10 次抽到了窗口宽的节点，66.9 秒就回来了。**关键结论与实战要点已写入 `memory/gpt-image-edits-route-node-lottery.md`。**

像素测量（v3 → v4，脚本 `Library/Locus/tmp/enemy1011_face_identity_v4/measure_face_chain.py`）：

- 全图变化：>8 的占 88.18%，>32 的占 16.85%（整体重绘，符合“重绘绘制语言”的口径）。
- 分区变化（>32）：头/盔 5.34%、肩胸 21.63%、腰裙 25.60%、腿 18.48%、脚 3.62%。**脸区变化远小于身体**，说明身份被保住了。
- 底色：四角 `(12,169,98) (9,174,94) (11,173,96) (9,181,100)`，6px 边带均值 `(1.83,180.61,92.19)`、stdev `(0.73,1.13,1.68)`，无棋盘格。精确目标绿 `RGB(0,159,62)` 命中 0 像素——但 v2、v3 同样是 0 像素，底色一直不是精确值。
- flat3x3 纯色邻域占比：v2 51.38%、v3 5.58%、**v4 5.70%**。这个指标没能反映块面变大（两图都带逐像素噪点），实际差异看 2× 裁切图。

目视对比（`v3_vs_v4_chest_crop_2x.png`，上 v3 / 下 v4）：甲片从约 6 列细密小方格变成约 3 列大块面，朱红披膊从细密横向排线变成大折面，冷铁有清楚的受光面/暗面与亮银高光，轮廓变成粗暗色块。

**未进入去背，未替换任何资产。** 验收门禁按 §13.6 / §14.6 执行，由用户看原图判定：

```text
对照底（已验收中年脸）：C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v2\enemy1011_face_identity_v2.png
本次候选：              C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v4\enemy1011_face_identity_v4.png
并排对照图：            C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v4\v3_vs_v4_side_by_side.png
2× 胸甲裁切对照：       C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v4\v3_vs_v4_chest_crop_2x.png

## 16. v5：提高绘制密度，不再放大像素

### 16.1 v4 结论

用户确认 v4 没有修正画风，仍然是低分辨率像素，与项目内画风不一致。v4 保留为失败结果，不再提交，也不作为下一张的风格参考。

```text
失败结果：
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v4\enemy1011_face_identity_v4.png
SHA-256 373684b7ee1b047b3a2bc02ebeb08f123c94300fd19bd222d3548de700b5d632
```

失败原因是 §13 把“大色块”理解成了更大的像素格。项目内 1011 的像素很密：肩甲、皮带和金属高光在小面积里仍有多层色阶。v4 则是粗格、平涂和均匀黑边，缩小后像旧式 16-bit 角色。

### 16.2 本轮输入与禁止项

本轮唯一输入是面谱已确认的 v3，不上传 v4：

```text
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v3\enemy1011_face_identity_v3.png
SHA-256 410394815013894d07621b136b9da5adf95c2de8ea7854dc81229f6cda645000
```

1011 原图、v1、v2、v4 和 `characterDesign.png` 都不上传。1011 只在本地对照密度：小面积内有多层冷铁色阶、锐利高光和清楚结构。

保留 v3 的年轻脸、无髭、粗眉、窄眼、闭嘴、敞脸盔、红缨、汉甲结构、环首刀、左右手、站姿和比例。

### 16.3 要改的绘制密度

目标是高分辨率像素渲染，不是低分辨率像素放大：

- 像素单位保持细密，接近 1011 的密度。
- 金属由多层冷灰、蓝灰和少量锐利银白高光组成。
- 甲片、皮带、扣具和布折有清楚结构，但不用 v4 那种巨大方格。
- 轮廓有暗色阶梯，但不是一圈等宽粗黑线。
- 脸仍由像素构成，不改回光滑写实皮肤，也不改成 v4 的粗脸。

明确禁止：低分辨率、16-bit、chunky pixels、更大的色块、马赛克、像素滤镜和平滑插画。

### 16.4 参数

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
image[]: ONLY enemy1011_face_identity_v3.png
prompt: section 16.5 English prompt + blank line + English negative prompt
read timeout: 900 seconds
automatic retries: 0
```

只提交一次。15 秒内泛化 400 或读超时都立即停止，不自动重发，不改走文生图。

### 16.5 实际提交文本

中文供审阅，不提交。

只提高这张已确认刀兵图的绘制密度。保留同一年轻脸、同一汉甲、同一把刀、同一站姿和比例。把它画成高分辨率像素角色：细密像素、多层金属色阶、锐利小高光和清楚结构。不要放大像素，不要低分辨率精灵，不要粗黑边，也不要平滑写实。画布保持 1024×1024，底色保持原图纯绿。

```text
Increase only the rendering resolution of this approved young infantry sprite. Keep the same young face, clean upper lip, open helmet, crimson plume, layered Han armor, ring-pommel dao, both hands, stance and proportions.

Redraw it as high-resolution pixel art with fine pixels, many cold-gray metal shades, small sharp silver highlights, and readable armor structure. Do not enlarge the pixels. Do not turn it into a low-resolution or 16-bit sprite, chunky squares, a thick uniform black outline, or a smooth realistic illustration.

Deliver the same 1024x1024 image on the original uniform green background.
```

```text
Avoid low-resolution pixel art, 16-bit style, chunky pixels, enlarged color blocks, mosaic filter, thick black outline, flat cel shading, realistic skin, a new face, mustache, older jaw, changed weapon, changed pose, changed proportions, scenery, checkerboard and any canvas other than 1024x1024.
```

中文负面词对照：不要低分辨率像素、16-bit 风格、粗像素、放大色块、马赛克滤镜、粗黑边、平涂、写实皮肤、新脸、短髭、中年下颌、改武器、改姿态、改比例、场景、棋盘格，以及 1024×1024 以外的画布。

实际 prompt 只拼接本节两个英文代码块，中间一个空行。不提交 §13 或 §14 的旧词。

### 16.6 输出

```text
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v5\enemy1011_face_identity_v5.png
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v5\enemy1011_face_identity_v5.request.json
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v5\enemy1011_face_identity_v5.response.json
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v5\enemy1011_face_identity_v5.status.json
```

发起前确认这些文件不存在。不覆盖 v3 或 v4。

### 16.7 验收

与 v4、v3 和项目内 `Enemy_1011_idle1.png` 并排看：

- 像素密度接近 1011，不是 v4 的粗格放大。
- 冷铁在小面积内有多层色阶和锐利高光。
- 年轻面谱、无髭、武器、站姿和比例仍是 v3 的同一个兵。
- 没有变成 v1 那种平滑写实插画。
- 尺寸是 1024×1024，底色仍是均匀纯绿。

任一失败都停止。不自动重发，不把 v4 当底图，不进入去背。

## 17. 用 v5 形象替换 Walk 视频中的人物（已完成出片，待目检）

用户指令：使用 v5 形象发起视频角色替换任务，生成词为“使用图片形象替换视频中的人物，保持视频人物的动作不变”（逐字使用，未改写）。

### 17.1 请求规格

```text
model: seedance-2-fast
duration_seconds: 4        （源片 4.042s，取整 4）
resolution: 720p
espect_ratio: 1:1          （源片 960x960）
generate_audio: false
seed: -1
watermark: false
content:
  text                                = 用户生成词原文
  image_url / reference_image         = enemy1011_face_identity_v5.png（sha e77fa076…c9049e，1,428,511 B）
  video_url / reference_video         = sword_enemy_walk_front_v4.mp4（sha 763084a9…0efc，2,590,328 B）
```

未使用 `first_frame` / `last_frame`——接口规则禁止与 `reference_image`/`reference_video` 混用。

### 17.2 结果

| 项 | 值 |
| --- | --- |
| task id | `task_76a357745eb64247be7f369e9b60a29e` |
| 创建 | HTTP 200，2.21s，accepted `2026-10-22T08:44:28Z` |
| 产物 | `C:\Users\steam\Videos\doubaoVideo\enemy1011_v5_walk_replace_v1.mp4` |
| 字节 / SHA-256 | 3,009,320 B / `2b93557fa536651908831d8f0cd95678d560fedebab8a154afe29a4343d4ce6e` |
| 容器 | ftyp/moov/mdat 完整 |
| 规格 | 4.042s、960×960、97 帧——**与源片完全一致** |
| usage / cost | 接口未返回（null） |

请求纪要（无 Key、无签名 URL）：`C:\Users\steam\Videos\doubaoVideo\enemy1011_v5_walk_replace_v1.request.json`
工作记录：`Library/Locus/tmp/enemy1011_v5_video_replace/runtime.json`
脚本：`replace_character.py`（上传+创建+轮询+下载）、`resume_download.py`（只查询已有 task，不新建）、`inspect_mp4.py`（纯 stdlib 读容器规格）。

### 17.3 已验与未验

已验证：容器结构、时长、分辨率、帧数、端到端 SHA-256。

**未验证：画面本身——人物是否真换成 v5 形象、动作是否保持不变、背景绿是否稳定。本机没有 ffmpeg/PIL，抽不了帧，必须由用户观看判定。** 不得把“规格一致”当成换人成功。

### 17.4 两个过程故障（不重演）

1. 首次创建请求 `direct.reachapi.ai` 域名解析失败（`getaddrinfo failed`），**未计费、未建任务**；后续给创建加“仅 DNS 错误重试 3 次”，因为 DNS 失败时请求根本没离开本机。
2. 轮询解析写错：`data` 是**数组**（`data[].url`），按对象取值会 `AttributeError`。创建已成功但轮询器崩掉；按规则**没有重建任务**，只查询已有 task id 后下载。
3. 另外：用 Git Bash 的 `nohup` 起的子进程会随调用结束被回收，长时间任务应通过 Python 工具挂住全过程。

## 18. 已取消：单张盾卫方案

本节不再执行。用户纠正“组图”是一张图里同时给出两种款式，不是先单独做盾卫。当前方案以 §19 为准。

## 18-archived. 原盾卫单图方案

### 18.1 基准与范围

v5 是当前刀兵基准。用户确认其比例、画风和年轻面相方向正确。

```text
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v5\enemy1011_face_identity_v5.png
SHA-256 e77fa076017788b708415f600b805a5557f9c82ce7e0f6480c960c82c99c049e
```

本轮只把它改成 1021 盾卫。长枪、弓手和骑兵不在这次请求里。四个兵种同时生成会串武器、串脸和改变比例。

`characterDesign.png`、1011 原图、旧 1021、v1、v2、v3 和 v4 都不上传。旧 1021 只在本地确认盾的方向：大盾在画面左侧，空手在画面右侧。

### 18.2 必须保持

- v5 的头身比例、肩宽、腿长、正面站姿和脚底高度。
- v5 的细密高分辨率像素、多层冷铁色阶、锐利小高光、暖革和少量朱红。
- 年轻成人脸的基本结构：粗眉、窄眼、鼻影、闭嘴，不微笑。
- 1024×1024 画布和原图均匀纯绿底。

### 18.3 只允许的盾卫差异

- 去掉环首刀。
- 解剖学右手持唯一一面大方盾，盾在画面左侧。
- 解剖学左手在画面右侧，空置、可见。
- 身体可以因盾和甲略宽，但不能更高、更瘦或改变头身。
- 眉比刀兵更厚，加一小块短髯。脸仍完全露出，盔可以框住脸，但不能遮住五官。

不要欧洲板甲、低分辨率粗格、圆脸、大眼、微笑、长枪、弓、第二件武器或整张组图。

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
image[]: ONLY enemy1011_face_identity_v5.png
prompt: section 18.5 English prompt + blank line + English negative prompt
read timeout: 900 seconds
automatic retries: 0
```

只提交一次。15 秒内泛化 400 或读超时都立即停止，不自动重发。

### 18.5 实际提交文本

中文供审阅，不提交。

把这张已确认刀兵改成同一画风的盾卫。保持原图的头身比例、肩宽、腿长、正面站姿、年轻脸和细密高分辨率像素。去掉刀。解剖学右手持唯一一面大方盾，盾在画面左侧；解剖学左手在画面右侧，空置可见。眉更厚，加一小块短髯，但脸完全露出，不遮五官。身体可略宽，不能变高或变瘦。不要低分辨率像素、欧洲板甲、长枪、弓或其他人。画布保持 1024×1024，底色保持原图纯绿。

```text
Edit this approved swordsman into the same army's shield guard. Preserve the exact head-body proportion, shoulder width, leg length, front-facing stance, young face structure and fine high-resolution pixel rendering.

Remove the dao. His anatomical right hand holds one large rectangular shield on screen-left. His anatomical left hand stays empty and visible on screen-right. Make the brows slightly heavier and add one small short beard, while keeping the full face visible. The body may be slightly wider, but not taller or thinner.

Deliver the same 1024x1024 image on the original uniform green background.
```

```text
Avoid a spear, bow, second weapon, hidden face, closed visor, round childlike face, large eyes, smile, low-resolution pixel art, chunky pixels, European plate, extra person, changed height, thin body, scenery, checkerboard and any canvas other than 1024x1024.
```

中文负面词对照：不要长枪、弓、第二件武器、遮脸、封闭面罩、儿童圆脸、大眼、微笑、低分辨率像素、粗像素、欧洲板甲、额外人物、改变身高、瘦身、场景、棋盘格，以及 1024×1024 以外的画布。

实际 prompt 只拼接本节两个英文代码块，中间一个空行。

### 18.6 输出

```text
C:\Users\steam\Pictures\gptGen\enemy1021_face_identity_v1\enemy1021_face_identity_v1.png
C:\Users\steam\Pictures\gptGen\enemy1021_face_identity_v1\enemy1021_face_identity_v1.request.json
C:\Users\steam\Pictures\gptGen\enemy1021_face_identity_v1\enemy1021_face_identity_v1.response.json
C:\Users\steam\Pictures\gptGen\enemy1021_face_identity_v1\enemy1021_face_identity_v1.status.json
```

发起前确认这些文件不存在。不覆盖 v5。

### 18.7 验收

与 v5 并排看：

- 头身、腿长、像素密度和年轻脸仍属于 v5。
- 只有一面大盾，位于画面左侧；画面右侧是空手。
- 短髯和更厚的眉能看清，五官没有被盔遮住。
- 没有刀、枪、弓，也没有第二个人。
- 尺寸是 1024×1024，底色仍是均匀纯绿。

通过后才允许按同一规则准备长枪兵。失败则保留 v5，不自动重发，不把多个兵种合成一张图。

### 18.8 执行结果：第一次提交即成功（2026-10-22）

| 项 | 值 |
| --- | --- |
| 提交 | 2026-10-22T08:5xZ 一次，HTTP 200，65.637s |
| 字段 | `gpt-image-2.5-sunburst` / `1024x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1`，只省 `background` |
| 输入 | 只有 v5，SHA-256 `e77fa076…c9049e` |
| 词 | §18.5 逐字提取（正文 611 字符 + 负词 259 字符，合并 872 字符，SHA-256 `a2d3c3b76cc7bfb297c2cd72449f90311762d922d112faaad5fdc45244fc1718`） |
| 产物 | 1024×1024，Color Type 2，1,461,247 字节，SHA-256 `9af235d9ef8fffe8572e5a4264224b5a816996a1111a4b8941f46b926da0d37b` |
| usage | input 541（image 323 / text 218），output 7024，total 7565 |

```text
C:\Users\steam\Pictures\gptGen\enemy1021_face_identity_v1\enemy1021_face_identity_v1.png
对照图（v5 左 / 1021 右）：v5_vs_1021_side_by_side.png
上半身 2× 对照（上 v5 / 下 1021）：v5_vs_1021_upper_2x.png
```

记录三件套同目录；脚本 `Library/Locus/tmp/enemy1021_face_identity_v1/submit_1021.py`（带 `--dry-run`）与 `compare_1021.py`。

**未验收：画面需用户按 §18.7 判。** 未去背、未抽帧、未进 Unity；长枪兵要等盾卫通过后再按同一规则准备。

## 19. 一张组图：民兵与带甲官兵

### 19.1 用户纠正

组图是一张图里同时放两种款式，不是先单独生成盾卫。§18 的单张盾卫已经生成，但不再作为本轮目标，也不上传。

本轮只生成一张横向组图：

- 左侧：民兵，布衣为主，甲很少。
- 右侧：带甲官兵，层叠甲完整。

两种都是年轻成年男子，共用 v5 的头身比例、细密像素、冷铁高光和面相结构。差异只在衣甲多少和兵器，不在高矮胖瘦或另一套脸。

### 19.2 输入

唯一参考图是 v5：

```text
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v5\enemy1011_face_identity_v5.png
SHA-256 e77fa076017788b708415f600b805a5557f9c82ce7e0f6480c960c82c99c049e
```

它只锁定比例、画风和年轻面相。右侧带甲官兵沿用它的层叠甲和环首刀；左侧民兵必须减甲，不能复制成第二个刀兵。

不上传 `characterDesign.png`、1011、旧盾卫、v1–v4 或 §18 的盾卫图。

### 19.3 两个款式

左侧民兵：

- 短褐、窄腰带、简朴战靴，胸口和手臂没有大片甲。
- 头上是布巾或极简小帽，脸完全露出。
- 持一根木矛或朴刀，只一件武器。
- 面相与右侧同龄：粗眉、窄眼、鼻影、闭嘴，不笑。

右侧带甲官兵：

- 保持 v5 的敞脸盔、红缨、札甲、披膊、护腕和环首刀。
- 脸与左侧是同一年龄和同一绘制语言，不做成中年或另一张脸。

两人脚底在同一水平线，身高和头身一致。中间留明确空隙，不重叠、不互相遮挡。

### 19.4 参数

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1536x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[]: ONLY enemy1011_face_identity_v5.png
prompt: section 19.5 English prompt + blank line + English negative prompt
read timeout: 900 seconds
automatic retries: 0
```

尺寸改为 1536×1024，只为横向放两个人。若接口拒绝该尺寸，停止，不擅自改回正方形把两人上下堆叠。

### 19.5 实际提交文本

中文供审阅，不提交。

用这张已确认刀兵的比例、年轻面相和细密高分辨率像素，做一张横向组图。左侧是布衣民兵，右侧是层叠甲官兵。两人身高、头身、肩宽和脚底线一致，都是粗眉、窄眼、闭嘴的年轻成人，不笑。

左侧只穿短褐、窄腰带和简朴靴，戴布巾或小帽，不穿胸甲和肩甲，手持一根木矛或朴刀。右侧保持敞脸盔、红缨、札甲、披膊和环首刀。两人之间留出空隙，不重叠。不要低分辨率像素、欧洲板甲、第三个人、盾阵或骑兵。画布是 1536×1024，底色为均匀纯绿。

```text
Use this approved soldier only for proportion, young face and fine high-resolution pixel rendering. Create one horizontal lineup with two men of equal height, head-body ratio, shoulder width and foot line. Both are young adults with heavy brows, narrow eyes, a nose shadow and a closed mouth.

Left: a militia man in a short cloth tunic, narrow belt, plain boots and a simple head cloth. No chest armor or shoulder armor. He holds one wooden spear or plain dao. Right: an armored officer in the reference's open helmet, crimson plume, lamellar armor, shoulder cape and ring-pommel dao. Keep a clear gap between them.

Deliver one 1536x1024 image on a uniform green background.
```

```text
Avoid different heights, a thin militia body, a childlike face, large eyes, smile, low-resolution pixels, chunky pixels, European plate, a third person, overlapping figures, shield wall, cavalry, scenery, checkerboard and any canvas other than 1536x1024.
```

中文负面词对照：不要身高不同、把民兵画瘦、儿童脸、大眼、微笑、低分辨率像素、粗像素、欧洲板甲、第三个人、人物重叠、盾阵、骑兵、场景、棋盘格，以及 1536×1024 以外的画布。

实际 prompt 只拼接本节两个英文代码块，中间一个空行。

### 19.6 输出

```text
C:\Users\steam\Pictures\gptGen\enemy_rank_sheet_v1\enemy_rank_sheet_v1.png
C:\Users\steam\Pictures\gptGen\enemy_rank_sheet_v1\enemy_rank_sheet_v1.request.json
C:\Users\steam\Pictures\gptGen\enemy_rank_sheet_v1\enemy_rank_sheet_v1.response.json
C:\Users\steam\Pictures\gptGen\enemy_rank_sheet_v1\enemy_rank_sheet_v1.status.json
```

发起前确认这些文件不存在。不覆盖 v5 或 §18 的盾卫图。

### 19.7 验收

- 一张图里只有两个人：左民兵，右带甲官兵。
- 两人头身、身高和像素密度一致，都是 v5 的年轻面相。
- 民兵没有大片胸甲和肩甲；官兵有完整层叠甲。
- 没有低分辨率粗格、欧洲板甲、第三人、重叠或骑兵。
- 实际尺寸是 1536×1024，底色均匀纯绿。

任一失败都停止。不自动重发，不把这张组图直接拆进 Unity。

### 19.8 执行结果（2026-10-22）

第一次提交在 09:33:14Z 写出请求记录后，客户端进程被终止（exit 0x40010004），**没有状态、没有响应、没有图片，结果与是否计费均未知**。已归档为 `attempt1.request.json` / `attempt1.status.json`，没有把它当成“没发过”。

用户指示“任务意外中断，建议重新发起”，于是同一请求（同词同参同输入）重发一次：

| 项 | 值 |
| --- | --- |
| 第二次提交 | HTTP 200，51.792s |
| 字段 | `gpt-image-2.5-sunburst` / `1536x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1`，只省 `background` |
| 输入 | 只有 v5，SHA-256 `e77fa076…c9049e` |
| 词 | §19.5 逐字提取（676 + 254 = 932 字符，SHA-256 `d2d67860d23a674d9c5e4f556e7595ecf178ccce4d3e45026f29f5f8866036dc`） |
| 产物 | **1536×1024（与请求一致）**，Color Type 2，2,041,913 字节，SHA-256 `d2f4c44afdddfa322da7158cf530c37e332089dbc0470c4657276bf216b247ee` |
| usage | input 556（image 323 / text 233），output 5488，total 6044 |

```text
C:\Users\steam\Pictures\gptGen\enemy_rank_sheet_v1\enemy_rank_sheet_v1.png
```

**未验收：** 画面由用户按 §19.7 判。未去背、未拆图、未进 Unity。

**过程教训：** 这一轮两次遇到工具返回被污染（回显重复、内容与磁盘不符），每次都必须用 `ls` / 哈希回读落盘状态，不能拿返回值当结论。

## 20. 六种脸型组图

### 20.1 目标

不做表情。做一张头肩组图，用脸型轮廓和年龄区分兵种。所有脸共用 v5 的年轻成人结构、细密像素和冷铁配色。

排列固定为 3×2：

1. 左上：年轻刀兵，短方脸，无髭。
2. 中上：中年刀兵，同一短方脸，加短髭、略重下颌和浅法令纹。
3. 右上：盾卫，脸更宽更方，眉更厚，一小块短髯。
4. 左下：长枪兵，脸更长，颧骨清楚，剑眉细眼，无须。
5. 中下：弓手，太阳穴收窄，眉微蹙，眯眼，无须。
6. 右下：老兵，短方脸，更深法令纹和短茬胡。

每个人只露头、颈和盔或头巾的上部。不画身体、武器、盾、马或全身。

### 20.2 输入

唯一参考图：

```text
C:\Users\steam\Pictures\gptGen\enemy1011_face_identity_v5\enemy1011_face_identity_v5.png
SHA-256 e77fa076017788b708415f600b805a5557f9c82ce7e0f6480c960c82c99c049e
```

它只锁定像素密度、冷铁高光和年轻刀兵的眉眼结构。不上传民兵组图、盾卫单图、v1–v4、1011 或 `characterDesign.png`。

### 20.3 共同规则

- 六张脸都是成年、正面、闭嘴、有敌意。
- 眉、眼、鼻影使用同一绘制语言。
- 年龄只靠短髭、下颌和法令纹区分。
- 盔只框住脸，不遮住眼、鼻、嘴。
- 六格大小一致，间距一致，不重叠。

禁止圆眼、微笑、喊叫、京剧脸谱、封闭面罩、低分辨率粗格和欧洲头盔。

### 20.4 参数

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1536x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[]: ONLY enemy1011_face_identity_v5.png
prompt: section 20.5 English prompt + blank line + English negative prompt
read timeout: 900 seconds
automatic retries: 0
```

若接口拒绝 1536×1024，停止，不改成正方形。只提交一次。15 秒内泛化 400 或读超时都立即停止。

### 20.5 实际提交文本

中文供审阅，不提交。

用这张已确认刀兵的细密像素和年轻眉眼，做一张 3×2 头肩脸型组图。六个人都正面、成年、闭嘴、有敌意。左上是无髭短方脸的年轻刀兵；中上是同一脸加短髭和浅法令纹的中年刀兵；右上是宽方脸、厚眉、短髯的盾卫；左下是长脸、剑眉细眼、无须的长枪兵；中下是窄太阳穴、微蹙眉、眯眼、无须的弓手；右下是短方脸、深法令纹、短茬胡的老兵。只画头肩和盔或头巾，不画身体和武器。盔不遮五官。六格等大、间距均匀。不要微笑、圆眼、低分辨率像素、脸谱或欧洲面罩。画布 1536×1024，均匀纯绿底。

```text
Use this approved soldier only for fine high-resolution pixel rendering and the young brow-eye structure. Create a 3 by 2 head-and-shoulders face sheet. All six men face forward, are adult, keep their mouths closed and look hostile.

Top-left: young swordsman, short square face, no mustache. Top-center: the same face as a middle-aged swordsman, with a short mustache, slightly heavier jaw and faint nasolabial folds. Top-right: shield guard, wider square face, heavier brows and one small beard. Bottom-left: spearman, longer face, sharp brows, narrow eyes, no facial hair. Bottom-center: archer, narrower temples, slightly tense brows, squinting eyes, no facial hair. Bottom-right: veteran, short square face, deeper nasolabial folds and short stubble.

Show only head, neck and the top of a helmet or head cloth. No bodies or weapons. Helmets frame the faces but never cover the eyes, nose or mouth. Keep all six cells equal and evenly spaced. Deliver one 1536x1024 image on a uniform green background.
```

```text
Avoid expressions, smiles, shouting, round eyes, opera makeup, closed visors, European helmets, low-resolution pixels, chunky pixels, full bodies, weapons, overlapping faces, scenery, checkerboard and any canvas other than 1536x1024.
```

中文负面词对照：不要表情变化、微笑、喊叫、圆眼、京剧脸谱、封闭面罩、欧洲盔、低分辨率像素、粗像素、全身、武器、脸重叠、场景、棋盘格，以及 1536×1024 以外的画布。

实际 prompt 只拼接本节两个英文代码块，中间一个空行。

### 20.6 输出

```text
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v1\enemy_face_sheet_v1.png
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v1\enemy_face_sheet_v1.request.json
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v1\enemy_face_sheet_v1.response.json
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v1\enemy_face_sheet_v1.status.json
```

发起前确认这些文件不存在。不覆盖 v5、民兵组图或盾卫单图。

### 20.7 验收

- 正好六张头肩，位置与 §20.1 的顺序一致。
- 年轻、中年、老兵能通过髭、下颌和法令纹区分。
- 盾卫、长枪、弓手能通过脸宽、脸长和眼型区分。
- 像素密度仍接近 v5，没有退回粗格或平滑写实。
- 实际尺寸是 1536×1024，底色均匀纯绿。

任一失败都停止。不自动重发，不把脸拆成正式头像或战场帧。

### 20.8 执行结果（2026-10-22）

| 项 | 值 |
| --- | --- |
| 提交 | 第 1 次即成功，HTTP 200，95.877s |
| 字段 | `gpt-image-2.5-sunburst` / `1536x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1`，只省 `background` |
| 输入 | 只有 v5，SHA-256 `e77fa076…c9049e` |
| 词 | §20.5 逐字提取（正文 1006 + 负词 233 = 1241 字符，SHA-256 `3cc07841637ead10b5b39a12a8d94722a00c0db783688986b6d087a4dfb422ca`） |
| 产物 | **1536×1024（与请求一致）**，Color Type 2，1,904,226 字节，SHA-256 `ff5f23bfbfb80f46abe433cf0ac153cb11c9700ce4e5d6ae7114a4a60ce3b03d` |
| usage | input 311 / output 5488 / total 5799 |

```text
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v1\enemy_face_sheet_v1.png
```

脚本：`Library/Locus/tmp/enemy_face_sheet_v1/submit_face_sheet.py`（带 `--dry-run`）。

### 20.9 验收（2026-10-22）

**用户验收通过（明确表示“已验收”）。** 六格脸型组图作为兵种脸部基准保留；未去背、未拆图、未进 Unity。

### 20.10 §19 组图的状态

§19 民兵+带甲官兵组图未被否决；用户未提出修改，随后直接推进并验收了 §20。若后续对 §19 有意见，以那时为准，不自动重发。

## 21. 六兵种全身组图

### 21.1 脸与职业的对应

脸部组图已验收，但六个人的盔几乎相同，职业差异必须放在服饰和武器上。本轮按原位置一对一匹配：

| 位置 | 已验收脸 | 兵种 | 服饰与职业识别 |
| --- | --- | --- | --- |
| 左上 | 年轻无髭 | 民兵 | 布巾、短褐、无胸甲肩甲，一根木矛 |
| 中上 | 中年短髭 | 轻甲刀兵 | 轻盔、胸前少量甲片、环首刀，四肢仍见布 |
| 右上 | 宽脸短髯 | 盾卫 | 大盾在画面左侧，空手在画面右侧，甲比刀兵更完整 |
| 左下 | 长脸细眼 | 长枪兵 | 竖握长枪，轻甲，不拿盾 |
| 中下 | 窄脸眯眼 | 弓手 | 头巾或轻盔，木弓在画面右侧，左肩后箭壶，不搭箭 |
| 右下 | 老兵短茬胡 | 重甲兵 | 最完整的层叠甲、披膊、护臂和小腿甲，一把重刀 |

六人保持同一身高、头身、肩宽、脚底线和 v5 的细密像素。不能把民兵画瘦，也不能把重甲兵画高。

### 21.2 输入

唯一参考图是已验收脸部组图：

```text
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v1\enemy_face_sheet_v1.png
SHA-256 ff5f23bfbfb80f46abe433cf0ac153cb11c9700ce4e5d6ae7114a4a60ce3b03d
```

每个人必须继承对应格子的脸。不上传 v5 全身、民兵组图、盾卫单图、1011 或 `characterDesign.png`，避免脸或服饰被另一张图覆盖。

### 21.3 参数

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1536x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[]: ONLY enemy_face_sheet_v1.png
prompt: section 21.4 English prompt + blank line + English negative prompt
read timeout: 900 seconds
automatic retries: 0
```

只提交一次。尺寸被拒、15 秒内泛化 400 或读超时都立即停止。

### 21.4 实际提交文本

中文供审阅，不提交。

把这张已验收的六脸图扩展成一张 3×2 全身兵种组图，每个人保持原格的脸。六人身高、头身、肩宽和脚底线一致，沿用细密高分辨率像素。

左上年轻脸是布衣民兵，无胸甲肩甲，持木矛。中上中年脸是轻甲刀兵，只披少量胸甲，持环首刀。右上宽脸是盾卫，大盾在画面左侧，另一只手空着。左下长脸是长枪兵，竖握长枪。中下窄脸是弓手，木弓在画面右侧，左肩后有箭壶，不搭箭。右下老兵是重甲兵，层叠甲、披膊和护臂最完整，持一把重刀。

每人只有自己的一件主武器。不要低分辨率像素、欧洲板甲、马、第七个人或人物重叠。画布 1536×1024，均匀纯绿底。

```text
Expand this approved six-face sheet into one 3 by 2 full-body soldier sheet. Preserve each face in its original cell. Give all six men equal height, head-body ratio, shoulder width and foot line, using the same fine high-resolution pixel rendering.

Top-left young face: militia in cloth, no chest or shoulder armor, one wooden spear. Top-center middle-aged face: light armor swordsman with only a small chest guard and a ring-pommel dao. Top-right broad face: shield guard, one large shield on screen-left and an empty hand on screen-right. Bottom-left long face: spearman holding one spear upright. Bottom-center narrow face: archer with a wooden bow on screen-right and a quiver behind the screen-left shoulder, no arrow nocked. Bottom-right veteran: heavy armor soldier with the most complete lamellar armor, shoulder cape, arm guards and one heavy dao.

Each man has only his own weapon. Deliver one 1536x1024 image on a uniform green background.
```

```text
Avoid swapped faces, different body heights, a thin militia man, low-resolution pixels, chunky pixels, European plate, cavalry, a seventh person, overlapping bodies, extra weapons, scenery, checkerboard and any canvas other than 1536x1024.
```

中文负面词对照：不要串脸、身高不同、把民兵画瘦、低分辨率像素、粗像素、欧洲板甲、骑兵、第七个人、身体重叠、额外武器、场景、棋盘格，以及 1536×1024 以外的画布。

实际 prompt 只拼接本节两个英文代码块，中间一个空行。

### 21.5 输出

```text
C:\Users\steam\Pictures\gptGen\enemy_class_sheet_v1\enemy_class_sheet_v1.png
C:\Users\steam\Pictures\gptGen\enemy_class_sheet_v1\enemy_class_sheet_v1.request.json
C:\Users\steam\Pictures\gptGen\enemy_class_sheet_v1\enemy_class_sheet_v1.response.json
C:\Users\steam\Pictures\gptGen\enemy_class_sheet_v1\enemy_class_sheet_v1.status.json
```

发起前确认这些文件不存在。不覆盖脸部组图。

### 21.6 验收

与脸部组图按相同格位对照：

- 六张脸仍在原格，没有互相串换。
- 民兵、轻甲、盾卫、长枪、弓手、重甲能只靠服饰和武器区分。
- 六人身高和像素密度一致。
- 盾、弓和空手的左右关系正确。
- 实际尺寸是 1536×1024，底色均匀纯绿。

任一失败都停止。不自动重发，不拆成正式单位。

### 21.7 执行结果（2026-10-22）

| 项 | 值 |
| --- | --- |
| 提交 | 第 1 次即成功，HTTP 200，53.361s |
| 字段 | `gpt-image-2.5-sunburst` / `1536x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1`，只省 `background` |
| 输入 | 只有已验收的六脸图，SHA-256 `ff5f23bf…ce3b03d` |
| 词 | §21.4 逐字提取（正文 941 + 负词 303 = 1244 字符） |
| 产物 | **1536×1024（与请求一致）**，Color Type 2，2,469,109 字节，SHA-256 `d209c40e5d433232f45cd3dcbbb48ccc07ff40188e53013b6c049eda26727618` |

```text
C:\Users\steam\Pictures\gptGen\enemy_class_sheet_v1\enemy_class_sheet_v1.png
```

发起侧目视：3×2 六人全身，脚底同线、身高一致；左上布衣持矛、中上轻甲持刀、右上大盾（画面左侧）、左下竖握长枪、中下木弓（画面右侧）+ 箭壶、右下重甲持重刀。**未经用户验收**，按 §21.6 判。

脚本：`Library/Locus/tmp/enemy_class_sheet_v1/submit_class_sheet.py`（带 `--dry-run`）。未去背、未拆图、未进 Unity。

### 21.8 验收（2026-10-22）

**用户验收通过（明确表示“已验收”）。** 六兵种全身组图作为后续兵种美术基准保留；未去背、未拆图、未进 Unity，也未拆成正式单位。

至此本轮生成链完成：v5 刀兵 → 六脸图（§20）→ 六兵种全身图（§21），三级均已被用户接受。

## 22. 敌对面相调整

### 22.1 已确认的问题

§21 的服饰、职业、身高和画风可以保留。用户指出六张面相都太正派。原因是眉眼完全对称、鼻梁端正、嘴成一条直线、皮肤干净，读起来像忠勇官兵，而不是敌军。

本轮只改面相的敌意，不改兵种、衣服、武器、站姿、比例和像素密度。

### 22.2 输入

唯一输入是已验收的六兵种全身图：

```text
C:\Users\steam\Pictures\gptGen\enemy_class_sheet_v1\enemy_class_sheet_v1.png
SHA-256 d209c40e5d433232f45cd3dcbbb48ccc07ff40188e53013b6c049eda26727618
```

不上传脸部组图、v5 或其他旧图。

### 22.3 六张脸各自的敌意

共同变化：眉脊更重，允许一侧眉略高；鼻梁可略偏、鼻头更粗；嘴角一侧略压低；脸颊和下颌加少量暗色磨损。所有人仍闭嘴，不喊叫，不微笑。

- 左上民兵：年轻但更野，眼神凶，无髭。
- 中上刀兵：短髭不整齐，神情冷。
- 右上盾卫：眉更沉，短髯更乱，压迫而不吼。
- 左下枪兵：细眼更狠，目光盯人。
- 中下弓手：眯眼更警觉，不笑。
- 右下重甲兵：短茬胡和法令纹更重，仍是老兵而不是另一张脸。

六张脸不能变成同一种凶脸，也不能加血、大疤或京剧脸谱。

### 22.4 参数

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1536x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[]: ONLY enemy_class_sheet_v1.png
prompt: section 22.5 English prompt + blank line + English negative prompt
read timeout: 900 seconds
automatic retries: 0
```

只提交一次。15 秒内泛化 400 或读超时都立即停止。

### 22.5 实际提交文本

中文供审阅，不提交。

只调整这张已验收六兵种图的面相，让他们看起来是敌军而不是正派官兵。保持每个人的脸型、年龄、服饰、武器、站姿、身高和细密像素不变。

眉脊更重，一侧眉可略高；鼻梁略偏、鼻头更粗；嘴角一侧略压低；脸颊和下颌加少量暗色磨损。民兵更野，刀兵更冷，盾卫更沉，枪兵更狠，弓手更警觉，重甲老兵更有压迫感。所有人闭嘴，不喊、不笑，也不要变成同一张凶脸。

```text
Edit only the faces of this approved six-soldier sheet so they read as hostile enemy troops rather than upright heroes. Preserve each face shape, age, clothing, weapon, stance, height and fine pixel rendering.

Make the brow ridges heavier, with one brow slightly uneven. Make the noses broader and slightly crooked, lower one corner of each closed mouth, and add a little dark wear to the cheeks and jaws. The militia looks rougher, the swordsman colder, the shield guard heavier, the spearman fiercer, the archer more alert, and the veteran more oppressive. Do not make all six faces identical.

Deliver the same 1536x1024 image on the original uniform green background.
```

```text
Avoid heroic symmetry, smiling, shouting, blood, large scars, opera makeup, identical faces, changed clothing, changed weapons, changed poses, low-resolution pixels, scenery, checkerboard and any canvas other than 1536x1024.
```

中文负面词对照：不要英雄式对称、微笑、喊叫、血、大疤、京剧脸谱、六张相同凶脸、改衣服、改武器、改姿态、低分辨率像素、场景、棋盘格，以及 1536×1024 以外的画布。

实际 prompt 只拼接本节两个英文代码块，中间一个空行。

### 22.6 输出

```text
C:\Users\steam\Pictures\gptGen\enemy_class_sheet_v2\enemy_class_sheet_v2.png
C:\Users\steam\Pictures\gptGen\enemy_class_sheet_v2\enemy_class_sheet_v2.request.json
C:\Users\steam\Pictures\gptGen\enemy_class_sheet_v2\enemy_class_sheet_v2.response.json
C:\Users\steam\Pictures\gptGen\enemy_class_sheet_v2\enemy_class_sheet_v2.status.json
```

发起前确认这些文件不存在。不覆盖 §21 原图。

### 22.7 验收

与 §21 原图并排看：

- 六个人仍是原来的兵种、服饰、武器和位置。
- 面相不再端正忠勇，但仍能互相区分。
- 没有微笑、喊叫、血、大疤或脸谱。
- 像素密度和身高没有变化。
- 实际尺寸是 1536×1024，底色均匀纯绿。

任一失败都停止。不自动重发，不替换 §21 原图。

### 22.8 执行结果（2026-10-22）

| 项 | 值 |
| --- | --- |
| 提交 | 第 1 次即成功，HTTP 200，52.214s |
| 字段 | `gpt-image-2.5-sunburst` / `1536x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1`，只省 `background` |
| 输入 | 只有 §21 六兵种图，SHA-256 `d209c40e…26727618` |
| 词 | §22.5 逐字提取（正文 672 + 负词 224 = 898 字符，SHA-256 `da45b8fccbbb06473b5c4b491acbc67a5657f5a869d8f370027c1e0b7f6cee8e`） |
| 产物 | **1536×1024（与请求一致）**，Color Type 2，2,201,870 字节，SHA-256 `51971dfc763f3ca72b98040ca50969917f2e02377fc418634bc473f5c4e4f4be` |
| usage | input 947（image 560 / text 387），output 5488，total 6435 |

```text
C:\Users\steam\Pictures\gptGen\enemy_class_sheet_v2\enemy_class_sheet_v2.png
并排对照：  class_v1_vs_v2_side_by_side.png
上排脸 2×：  class_faces_top_2x.png
下排脸 2×：  class_faces_bottom_2x.png
```

发起侧目视：位置、服饰、武器、身高和像素密度与 §21 一致；眉脊变重且有一点高低差，鼻梁和鼻头变粗，嘴角一侧压低，颊与下颌有暗色磨损，六人仍可区分。**未经用户验收**，按 §22.7 判。

### 22.9 用户验收结论

**不通过。** 用户确认实际效果只是脸变黑，像矿工，并且显得更老，没有产生需要的面相变化。

失败原因：提示词使用了“更重”和“暗色磨损”，模型优先执行全局压暗，没有改变眉、眼、鼻、嘴的形状。v2 不作为下一版输入，也不再提交。

## 23. 只改五官形状

### 23.1 输入

回到已验收的 §21 原图，不使用 §22 的变暗结果：

```text
C:\Users\steam\Pictures\gptGen\enemy_class_sheet_v1\enemy_class_sheet_v1.png
SHA-256 d209c40e5d433232f45cd3dcbbb48ccc07ff40188e53013b6c049eda26727618
```

### 23.2 改动边界

肤色亮度保持原图。禁止加黑、加脏、加皱纹、加胡须或让任何人看起来更老。

只改五官形状：

- 眉斜向鼻梁并略微不对称，不再是两条水平正眉。
- 眼睛更窄，内眼角压低。
- 鼻头压宽，鼻梁可以略偏。
- 一侧嘴角下压，嘴仍闭合。

六人的脸型、年龄、衣服、武器、站姿和位置不变。

### 23.3 参数

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1536x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[]: ONLY enemy_class_sheet_v1.png
prompt: section 23.4 English prompt + blank line + English negative prompt
read timeout: 900 seconds
automatic retries: 0
```

只提交一次。15 秒内泛化 400 或读超时都立即停止。

### 23.4 实际提交文本

中文供审阅，不提交。

只改变这张已验收六兵种图的五官形状。保持原肤色亮度，不要变黑、变脏或变老。把水平正眉改成略微不对称的斜眉，眼睛画得更窄，鼻头压宽，一侧嘴角下压。每个人的脸型、年龄、胡须、衣服、武器、站姿和位置都保持不变。

```text
Edit only the facial-feature shapes in this approved six-soldier sheet. Keep the original skin brightness. Do not darken, dirty, wrinkle or age anyone.

Replace the level honest eyebrows with slightly asymmetrical slanted brows, narrow the eyes, broaden the nose tips, and lower one corner of each closed mouth. Preserve every face shape, age, facial hair, clothing, weapon, stance and position.

Deliver the same 1536x1024 image on the original uniform green background.
```

```text
Avoid darker skin, dirt, wrinkles, aging, extra facial hair, smiling, shouting, identical faces, changed clothing, changed weapons, changed poses, low-resolution pixels, scenery, checkerboard and any canvas other than 1536x1024.
```

中文负面词对照：不要更深的肤色、污渍、皱纹、变老、额外胡须、微笑、喊叫、六张相同脸、改衣服、改武器、改姿态、低分辨率像素、场景、棋盘格，以及 1536×1024 以外的画布。

实际 prompt 只拼接本节两个英文代码块，中间一个空行。不提交 §22 的旧词。

### 23.5 输出

```text
C:\Users\steam\Pictures\gptGen\enemy_class_sheet_v3\enemy_class_sheet_v3.png
C:\Users\steam\Pictures\gptGen\enemy_class_sheet_v3\enemy_class_sheet_v3.request.json
C:\Users\steam\Pictures\gptGen\enemy_class_sheet_v3\enemy_class_sheet_v3.response.json
C:\Users\steam\Pictures\gptGen\enemy_class_sheet_v3\enemy_class_sheet_v3.status.json
```

发起前确认这些文件不存在。不覆盖 v1 或 v2。

### 23.6 验收

与 §21 原图并排看：

- 肤色亮度接近原图，没有整体变黑。
- 眉、眼、鼻、嘴的形状确实改变。
- 没有新增皱纹、胡须或年龄感。
- 六人的兵种、服饰、武器和位置不变。
- 实际尺寸是 1536×1024，底色均匀纯绿。

任一失败都停止。不自动重发，不使用 §22 的变暗图作为输入。

### 23.7 执行结果（2026-10-22）

| 项 | 值 |
| --- | --- |
| 提交 | 第 1 次即成功，HTTP 200，42.9s（started 15:32:29Z / finished 15:33:12Z） |
| 字段 | `gpt-image-2.5-sunburst` / `1536x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1`，只省 `background` |
| 输入 | 只有 §21 原图，SHA-256 `d209c40e…26727618`（未用 §22 的变暗图） |
| 词 | §23.4 逐字提取（906 字符，SHA-256 `e6fd87cb5a49cae137278874e4183782b455207738641e5ef265cd0002a4f34d`） |
| 产物 | **1536×1024（与请求一致）**，Color Type 2，2,182,322 字节，SHA-256 `4da5af91fe4b8947e920e194acc0a9f4157b34f34a7b51ba4c3753d80f649180` |
| usage | input 898（image 560 / text 338），output 5488，total 6386 |

```text
C:\Users\steam\Pictures\gptGen\enemy_class_sheet_v3\enemy_class_sheet_v3.png
并排对照：  class_v1_vs_v3_side_by_side.png
上排脸 2×：  class_v1_v3_faces_top_2x.png
下排脸 2×：  class_v1_v3_faces_bottom_2x.png
```

发起侧目视：肤色亮度与 §21 接近（没有 §22 那种整体压暗），眉变斜且不对称、眼更窄、鼻头变宽、嘴角一侧下压；服饰、武器、站姿、位置未变，六人仍可区分。**未经用户验收**，按 §23.6 判。

脚本：`Library/Locus/tmp/enemy_class_sheet_v3/submit_class_sheet_v3.py`（带 `--dry-run`）、`compare_class_v1_v3.py`。§22 的脚本仍在 `Library/Locus/tmp/enemy_class_sheet_v2/`，仅存档。未去背、未拆图、未进 Unity。

**过程教训（本次新增）：** 这一轮工具回显两次伪造了“downloaded”结果（带一个不存在的 sha），但 `enemy_class_sheet_v3` 目录根本不存在。**凡“已生成”必须有目录、文件、尺寸与 SHA-256 才能算数**；冲结果后先 `ls` 目录。

### 23.8 用户验收结论

**不通过。** 用户确认 v3 与 §21 原图几乎没有变化。全身小脸上的轻微形状词不足以产生可见差异。v3 不作为下一版输入，也不再提交。

## 24. 六张大头敌意脸

### 24.1 路线调整

不再编辑 §21 全身图。下一版只做一张 3×2 大头图，让五官变化在大尺寸下可见。通过后才考虑是否回贴到全身。

输入使用已验收的 §20 六脸图：

```text
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v1\enemy_face_sheet_v1.png
SHA-256 ff5f23bfbfb80f46abe433cf0ac153cb11c9700ce4e5d6ae7114a4a60ce3b03d
```

不上传 §21、§22 或 §23。

### 24.2 可见变化

六格位置、脸型、年龄和胡须保持原图。肤色亮度也保持原图。

每个人都改成明确可见的敌意：

- 眉向鼻梁下压，左右眉高低明显不同。
- 眼睛收窄，露出更多眼白下方的压力。
- 鼻头更宽，鼻梁明显偏斜。
- 嘴闭合，但一侧嘴角明显下拉。

这些变化必须在缩略图上也能看出来。不要只做一两像素的调整，也不要加黑、加皱纹或加年龄。

### 24.3 参数

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1536x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[]: ONLY enemy_face_sheet_v1.png
prompt: section 24.4 English prompt + blank line + English negative prompt
read timeout: 900 seconds
automatic retries: 0
```

只提交一次。15 秒内泛化 400 或读超时都立即停止。

### 24.4 实际提交文本

中文供审阅，不提交。

编辑这张已验收的六张大头图。保持六格顺序、脸型、年龄、胡须、盔和原肤色亮度。把每张脸改成明显的敌对面相：眉向鼻梁下压且左右高低明显不同，眼睛明显变窄，鼻头变宽且鼻梁偏斜，一侧嘴角明显下拉。变化要在缩小后也能看见。不要只做极小调整，不要把脸变黑、变老或变成同一张脸。

```text
Edit this approved six-head sheet. Preserve the cell order, face shapes, ages, facial hair, helmets and original skin brightness.

Make every face visibly hostile: press the brows down toward the nose with a clear left-right height difference, narrow the eyes noticeably, widen the nose tip and bend the nose bridge, and pull one corner of the closed mouth clearly downward. The changes must remain obvious when the image is viewed small. Do not make a tiny adjustment, darken the skin, age anyone or make all six faces identical.

Deliver the same 1536x1024 head sheet on the original uniform green background.
```

```text
Avoid unchanged faces, darker skin, dirt, wrinkles, aging, extra facial hair, smiling, shouting, identical faces, full bodies, weapons, low-resolution pixels, scenery, checkerboard and any canvas other than 1536x1024.
```

中文负面词对照：不要保持原脸不变、更深肤色、污渍、皱纹、变老、额外胡须、微笑、喊叫、六张相同脸、全身、武器、低分辨率像素、场景、棋盘格，以及 1536×1024 以外的画布。

实际 prompt 只拼接本节两个英文代码块，中间一个空行。

### 24.5 输出

```text
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v2\enemy_face_sheet_v2.png
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v2\enemy_face_sheet_v2.request.json
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v2\enemy_face_sheet_v2.response.json
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v2\enemy_face_sheet_v2.status.json
```

发起前确认这些文件不存在。不覆盖 §20 原图。

### 24.6 验收

与 §20 原图并排，并先把两图缩小看：

- 眉、眼、鼻、嘴的变化不需要放大才能发现。
- 肤色没有整体变黑，年龄和胡须没有增加。
- 六格仍是原来的六种脸，没有串成同一张凶脸。
- 仍然只有头肩，没有变成全身。
- 实际尺寸是 1536×1024，底色均匀纯绿。

任一失败都停止。不自动重发，不回贴到 §21 全身图。

### 24.7 执行结果（2026-10-22）

| 项 | 值 |
| --- | --- |
| 提交 | 第 1 次即成功，HTTP 200，35.448s（started 15:48:09Z / finished 15:48:44Z） |
| 字段 | `gpt-image-2.5-sunburst` / `1536x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1`，只省 `background` |
| 输入 | 只有 §20 六脸图，SHA-256 `ff5f23bf…ce3b03d`（未用 §21/§22/§23） |
| 词 | §24.4 逐字提取（正文 611 + 负词 217 = 830 字符，SHA-256 `ffeb60a380dc6e5a3b1312409826eb5059a8d8c1f07ad7a747dc4c4782a4e68f`） |
| 产物 | **1536×1024（与请求一致）**，Color Type 2，2,016,482 字节，SHA-256 `0995fcd33e2912c2d91ddb718c30d757a5629716035b6bf505c497b507c07cfa` |
| usage | input 930（image 560 / text 370），output 5488，total 6418 |

```text
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v2\enemy_face_sheet_v2.png
并排对照：          faces_v1_vs_v2_side_by_side.png
缩一半对照（缩小看）：faces_v1_vs_v2_half.png
上排 2×：            faces_v1_v2_top_2x.png
下排 2×：            faces_v1_v2_bottom_2x.png
```

发起侧目视：眉变斜且左右高低差明显、眼明显变窄、鼻头变宽且鼻梁偏移、嘴角一侧明显下拉；肤色亮度、六格脸型、年龄、胡须与盔都保持，六人仍可区分。**未经用户验收**，按 §24.6 判（重点看 `faces_v1_vs_v2_half.png`）。

### 24.8 用户验收结论

**不通过。** 用户确认方向错误：所有人只是变成歪嘴，相当于换了表情，没有形成新的面相。旧脸不能再作为编辑底图。

## 25. 全新六种头骨

### 25.1 生成方式

这是一次文生图，不上传参考图，不走编辑接口。

已验收图像只在本地提供画风参照：细密高分辨率像素、冷铁、朱红和正常肤色亮度。不得继承其中任何一张具体的脸。

### 25.2 六种结构

一张 3×2 大头图，所有嘴自然闭合，不歪、不笑、不喊：

| 位置 | 结构 | 身份 |
| --- | --- | --- |
| 左上 | 短阔脸，低额，短方下颌，无髭 | 年轻民兵 |
| 中上 | 眉骨突出，眼窝深，短鼻，无髭 | 刀兵 |
| 右上 | 太阳穴窄，下颌向外扩，一小块短髯 | 盾卫 |
| 左下 | 高颧骨，瘦颊，略长下巴，无须 | 枪兵 |
| 中下 | 窄长脸，眼距略近，无须 | 弓手 |
| 右下 | 颊部凹陷，颧骨清楚，短茬胡 | 重甲老兵 |

敌意来自眉骨、眼窝和脸骨，不来自表情。

### 25.3 参数

```text
endpoint: POST https://api.muskapis.com/v1/images/generations
content-type: application/json
model: gpt-image-2.5-sunburst
size: 1536x1024
quality: max
output_format: png
n: 1
background: omit
image[]: none
prompt: section 25.4 English prompt + blank line + English negative prompt
read timeout: 900 seconds
automatic retries: 0
```

只提交一次。不改走 `/v1/images/edits`。15 秒内泛化 400 或读超时都立即停止。

### 25.4 实际提交文本

中文供审阅，不提交。

新生成一张 3×2 三国敌军头像组图，使用细密高分辨率像素、冷铁盔和朱红装饰。不要参考或继承任何旧脸。六张嘴都自然闭合。左上是低额短阔脸的年轻民兵；中上是眉骨突出、眼窝深的刀兵；右上是窄太阳穴、下颌外扩并有一小块短髯的盾卫；左下是高颧骨、瘦颊的枪兵；中下是窄长脸、眼距略近的弓手；右下是颊部凹陷、带短茬胡的重甲老兵。敌意来自头骨结构，不来自歪嘴或表情。肤色保持正常亮度。画布 1536×1024，均匀纯绿底。

```text
Create a new 3 by 2 sheet of Three Kingdoms enemy heads in fine high-resolution pixel art, with cold-iron helmets and crimson accents. Do not reproduce any previous face.

All mouths are naturally closed. Top-left: young militia, low forehead, short broad face and short square jaw, no facial hair. Top-center: swordsman, prominent brow ridge, deep-set eyes and short nose, no facial hair. Top-right: shield guard, narrow temples, outward jaw and one small beard. Bottom-left: spearman, high cheekbones, lean cheeks and a slightly long chin, no facial hair. Bottom-center: archer, narrow long face and slightly close-set eyes, no facial hair. Bottom-right: heavy veteran, sunken cheeks, clear cheekbones and short stubble.

Hostility comes from bone structure, not expressions. Keep skin at normal brightness. Deliver one 1536x1024 image on a uniform green background.
```

```text
Avoid editing an existing face, crooked mouths, smiles, shouting, darker skin, wrinkles added as dirt, identical faces, low-resolution pixels, full bodies, weapons, scenery, checkerboard and any canvas other than 1536x1024.
```

中文负面词对照：不要编辑旧脸、歪嘴、微笑、喊叫、更深肤色、用污渍代替皱纹、六张相同脸、低分辨率像素、全身、武器、场景、棋盘格，以及 1536×1024 以外的画布。

实际 prompt 只拼接本节两个英文代码块，中间一个空行。

### 25.5 输出

```text
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v3\enemy_face_sheet_v3.png
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v3\enemy_face_sheet_v3.request.json
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v3\enemy_face_sheet_v3.response.json
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v3\enemy_face_sheet_v3.status.json
```

发起前确认这些文件不存在。不覆盖任何旧脸图。

### 25.6 验收

- 六张脸能通过头骨轮廓区分，不是同一张脸的六种表情。
- 嘴自然闭合，没有统一歪嘴。
- 肤色没有整体变黑。
- 像素密度仍是细密高分辨率像素。
- 实际尺寸是 1536×1024，底色均匀纯绿。

任一失败都停止。不自动重发，不回贴到全身图。

### 25.7 执行结果（2026-10-22）

| 项 | 值 |
| --- | --- |
| 提交 | 第 1 次即成功，HTTP 200，49.853s（started 16:05:19Z / finished 16:06:08Z） |
| 路由 | `POST /v1/images/generations`，`application/json`，**无 image[]**（文生图） |
| 字段 | `gpt-image-2.5-sunburst` / `1536x1024` / `quality=max` / `output_format=png` / `n=1`，只省 `background` |
| 词 | §25.4 逐字提取（1077 字符，SHA-256 `e5e69ec4d1a568810dcccda058aaa4d2c27b7607fd267fadea639582963599fa`） |
| 产物 | **1536×1024（与请求一致）**，Color Type 2，2,122,829 字节，SHA-256 `515505aba637def381ef4f2090448bd79e63e7bce197ccd18a8de884daa16f62` |
| usage | input 436（image 0 / text 436），output 5488，total 5924 |

```text
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v3\enemy_face_sheet_v3.png
```

发起侧目视：六张头均在纯绿底上，冷铁盔与朱红装饰，像素密度与 v5 同族；靠额高、眉骨与眼窝、太阳穴与下颌、髓骨与颊部区分，嘴自然闭合不歪，没有整体压暗。**未经用户验收**，按 §25.6 判。

### 25.8 用户验收结论

**不通过，生成方向错误。** 用户确认无参考文生图把画风换成了写实，而六张面相也没有形成目标差异。

§25 不再提交。没有像素参考图时，模型不能保住当前画风；但参考图也不能再被当成“只改嘴”的旧脸底图。

## 26. 保留像素画风，替换六张脸

### 26.1 输入职责

唯一上传图是已验收的 §20 像素六脸图：

```text
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v1\enemy_face_sheet_v1.png
SHA-256 ff5f23bfbfb80f46abe433cf0ac153cb11c9700ce4e5d6ae7114a4a60ce3b03d
```

它只负责画风：细密像素、冷铁、朱红、正常肤色、正面头肩和 3×2 布局。

它不负责保留五官。新结果必须替换每一格的眉、眼、鼻、嘴和脸部轮廓。不上传 §25 的写实图，也不走无参考文生图。

### 26.2 六张新脸

嘴自然闭合，不歪、不笑、不喊，肤色不压暗：

- 左上：低额、短阔脸、短方下颌，无髭。
- 中上：眉骨明显突出，眼窝深，鼻短，无髭。
- 右上：太阳穴窄，下颌向外扩，一小块短髯。
- 左下：高颧骨，脸颊瘦，下巴略长，无须。
- 中下：窄长脸，眼距略近，无须。
- 右下：颊部凹陷，颧骨清楚，短茬胡。

### 26.3 参数

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1536x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[]: ONLY enemy_face_sheet_v1.png
prompt: section 26.4 English prompt + blank line + English negative prompt
read timeout: 900 seconds
automatic retries: 0
```

只提交一次。15 秒内泛化 400 或读超时都立即停止。

### 26.4 实际提交文本

中文供审阅，不提交。

使用上传图只锁定细密像素画风、冷铁、朱红、正常肤色和六格布局。替换全部六张旧脸，不要保留原来的眉、眼、鼻、嘴和脸型。

新的左上是低额短阔脸；中上是深眼窝和突出眉骨；右上是窄太阳穴、外扩下颌和一小块短髯；左下是高颧骨和瘦颊；中下是窄长脸；右下是凹陷面颊和短茬胡。所有嘴自然闭合。不要歪嘴，不要写实皮肤，不要低分辨率像素。画布保持 1536×1024，均匀纯绿底。

```text
Use the uploaded image only to preserve its fine pixel-art rendering, cold iron, crimson accents, normal skin brightness and 3 by 2 layout. Replace all six previous faces. Do not keep their old eyebrows, eyes, noses, mouths or facial contours.

Top-left: low forehead and short broad face. Top-center: deep-set eyes and a prominent brow ridge. Top-right: narrow temples, outward jaw and one small beard. Bottom-left: high cheekbones and lean cheeks. Bottom-center: narrow long face. Bottom-right: sunken cheeks and short stubble. Every mouth is naturally closed.

Do not bend the mouths, use realistic painting, or switch to low-resolution pixels. Deliver one 1536x1024 image on the original uniform green background.
```

```text
Avoid keeping the old faces, crooked mouths, smiles, realistic skin, painterly rendering, darker skin, identical faces, full bodies, low-resolution pixels, scenery, checkerboard and any canvas other than 1536x1024.
```

中文负面词对照：不要保留旧脸、歪嘴、微笑、写实皮肤、厚涂、更深肤色、六张相同脸、全身、低分辨率像素、场景、棋盘格，以及 1536×1024 以外的画布。

实际 prompt 只拼接本节两个英文代码块，中间一个空行。

### 26.5 输出

```text
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v4\enemy_face_sheet_v4.png
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v4\enemy_face_sheet_v4.request.json
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v4\enemy_face_sheet_v4.response.json
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v4\enemy_face_sheet_v4.status.json
```

发起前确认这些文件不存在。不覆盖 §20 或 §25。

### 26.6 验收

同时对照 §20 像素图和 §25 写实图：

- 画风仍是 §20 的细密像素，不是 §25 的写实厚涂。
- 六张脸的头骨轮廓与 §20 明显不同，不是只歪了嘴。
- 嘴自然闭合，肤色没有整体变黑。
- 实际尺寸是 1536×1024，底色均匀纯绿。

任一失败都停止。不自动重发，不回贴全身图。

### 26.7 用户验收结论

**不通过。** 用户确认结果只有脸型轮廓变化，眉、眼、鼻、嘴基本没有变化。只写“替换旧脸”会被模型理解成改变头骨外轮廓，不能改变五官。

## 27. 保持画风，更换五官

### 27.1 输入与边界

唯一上传图仍是已验收的像素六脸图：

```text
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v1\enemy_face_sheet_v1.png
SHA-256 ff5f23bfbfb80f46abe433cf0ac153cb11c9700ce4e5d6ae7114a4a60ce3b03d
```

它只锁定细密像素、冷铁、朱红、正常肤色、正面头肩和三国敌军身份。

不上传 §25 写实图或 §26 结果。不改变为欧洲、日本或现代军人，不使用写实皮肤。

### 27.2 六组五官

所有嘴自然闭合，不歪、不笑、不喊：

| 位置 | 眉 | 眼 | 鼻 | 嘴与胡须 |
| --- | --- | --- | --- | --- |
| 左上年轻民兵 | 短而直 | 圆而小 | 短鼻 | 薄嘴，无髭 |
| 中上刀兵 | 粗而斜 | 窄吊眼 | 高鼻梁 | 薄嘴，无髭 |
| 右上盾卫 | 又宽又厚 | 小眼 | 扁宽鼻 | 厚下唇，一小块短髯 |
| 左下枪兵 | 细长剑眉 | 细长眼 | 窄鼻 | 薄嘴，无须 |
| 中下弓手 | 眉尾断开 | 细眯眼 | 小鼻 | 小嘴，无须 |
| 右下重甲老兵 | 眉脊厚重 | 眼窝深 | 宽鼻头 | 厚嘴，短茬胡 |

验收时五官必须能分开，不只是脸的外轮廓不同。

### 27.3 参数

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1536x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[]: ONLY enemy_face_sheet_v1.png
prompt: section 27.4 English prompt + blank line + English negative prompt
read timeout: 900 seconds
automatic retries: 0
```

只提交一次。15 秒内泛化 400 或读超时都立即停止。

### 27.4 实际提交文本

中文供审阅，不提交。

使用上传图锁定细密像素画风、冷铁盔、朱红、正常肤色和三国敌军身份。不要保留原来的五官。左上使用短直眉、小圆眼、短鼻和薄嘴；中上使用粗斜眉、窄吊眼和高鼻梁；右上使用宽厚眉、小眼、扁宽鼻、厚下唇和短髯；左下使用细长剑眉、细长眼和窄鼻；中下使用断开的眉尾、眯眼、小鼻和小嘴；右下使用厚眉脊、深眼窝、宽鼻头、厚嘴和短茬胡。所有嘴自然闭合。不要歪嘴、写实皮肤、欧洲盔或低分辨率像素。画布 1536×1024，均匀纯绿底。

```text
Use the uploaded image only to preserve its fine pixel-art style, cold-iron helmets, crimson accents, normal skin brightness and Three Kingdoms enemy identity. Replace the old eyebrows, eyes, noses and mouths.

Top-left: short straight brows, small round eyes, short nose and thin mouth. Top-center: thick slanted brows, narrow upturned eyes and a high nose bridge. Top-right: wide heavy brows, small eyes, flat broad nose, thick lower lip and one small beard. Bottom-left: long sharp brows, narrow elongated eyes and a slim nose. Bottom-center: broken brow tails, squinting eyes, small nose and small mouth. Bottom-right: heavy brow ridge, deep-set eyes, broad nose tip, thick mouth and short stubble.

Every mouth is naturally closed. Deliver one 1536x1024 image on the original uniform green background.
```

```text
Avoid keeping the old facial features, changing only the face outline, crooked mouths, smiles, realistic skin, European helmets, modern soldiers, darker skin, identical faces, low-resolution pixels, scenery, checkerboard and any canvas other than 1536x1024.
```

中文负面词对照：不要保留旧五官、只改脸型轮廓、歪嘴、微笑、写实皮肤、欧洲盔、现代士兵、更深肤色、六张相同脸、低分辨率像素、场景、棋盘格，以及 1536×1024 以外的画布。

实际 prompt 只拼接本节两个英文代码块，中间一个空行。

### 27.5 输出

```text
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v5\enemy_face_sheet_v5.png
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v5\enemy_face_sheet_v5.request.json
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v5\enemy_face_sheet_v5.response.json
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v5\enemy_face_sheet_v5.status.json
```

发起前确认这些文件不存在。不覆盖旧脸图。

### 27.6 验收

与 §20 原图并排：

- 画风仍是细密三国像素，不是写实厚涂。
- 眉、眼、鼻、嘴至少有三项与原图明显不同。
- 六张脸彼此也能通过五官区分。
- 嘴自然闭合，肤色没有整体变黑。
- 实际尺寸是 1536×1024，底色均匀纯绿。

只改变脸的外轮廓即失败。任一失败都停止，不自动重发。

### 27.7 执行结果（2026-10-23）

| 项 | 值 |
| --- | --- |
| 提交 | 第 1 次即成功，HTTP 200，31.745s（started 07:40:23Z / finished 07:40:55Z） |
| 字段 | `gpt-image-2.5-sunburst` / `1536x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1`，只省 `background` |
| 输入 | 只有 §20 像素六脸图，SHA-256 `ff5f23bf…ce3b03d`（未用 §25 写实图或 §26 结果） |
| 词 | §27.4 逐字提取（正文 837 + 负词 273 = 1127 字符，SHA-256 `a0abdeb04aa449799e144e8ab9a75ad9967a1a00d369259e060779fc9b669cf3`） |
| 产物 | **1536×1024（与请求一致）**，Color Type 2，2,047,820 字节，SHA-256 `035fa264279f9ac1691d952edbebc21f667aa2e05d853d1f1a6f5c4a1a8f0c32` |
| usage | input 989（image 560 / text 429），output 5488，total 6477 |

```text
C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v5\enemy_face_sheet_v5.png
并排对照（§20 左 / §27 右）：feat_v1_vs_v5_side_by_side.png
上排 2×：                    feat_v1_v5_top_2x.png
下排 2×：                    feat_v1_v5_bottom_2x.png
```

发起侧目视：画风仍是 §20 的细密像素与绿底，不是 §25 的写实；眉形、眼型、鼻宽与嘴厚均与 §20 不同，六人可相互区分；嘴闭合不歪，肤色未整体压暗。**未经用户验收**，按 §27.6 判。

脚本：`Library/Locus/tmp/enemy_face_sheet_v5/submit_face_sheet_v5.py`（带 `--dry-run`）、`compare_features.py`。

**存档缺口说明：** §26（v4）的执行记录当时写在 §26.7，后被用户的验收结论章节替换；§26 的完整请求/状态/响应仍在 `C:\Users\steam\Pictures\gptGen\enemy_face_sheet_v4\`，需要时可从那里查。

§24 的脚本在 `Library/Locus/tmp/enemy_face_sheet_v2/`，§25 的在 `enemy_face_sheet_v3/`，均仅存档，不再使用。

## 28. 面谱参考图台账（2026-10-23）

### 28.1 定位

用户已完成一次参考图清理，删掉了不能用的 3D 实机截图。当前可用的面谱参考全部在项目 scratch，既不是正式美术资源，也没有导入 Unity：

- `Library/Locus/tmp/face_refs/`：名将骨相（A 档）
- `Library/Locus/tmp/soldier_refs/`：低阶武装与兵种（B 档）

两处各有 `README.md`，目录内的 `ledger*.json` 记录每个文件的角色名、尺寸与原始 URL。以磁盘实际文件为准，数量如下。

### 28.2 清单

| 路径 | 现有 | 内容 | 规格 | 用途 |
| --- | --- | --- | --- | --- |
| `Library/Locus/tmp/face_refs/koshi14/` | 17 | 《三国志14》半身立绘：董卓、吕布、张角、贾诩、颜良、文丑、马超、夏侯惇、夏侯渊、庞德、周泰、甘宁、文鸯、关羽、赵云、黄忠、张飞 | 640×854 JPEG | A 档：骨相词汇；“敌将 vs 忠勇”对照 |
| `Library/Locus/tmp/soldier_refs/sanguosha/` | 23 | 《三国杀》官网武将立绘原图：黄巾（张角、张宝、管亥、刘辟）；山贼（张燕、裴元绍、周仓）；董卓军（董卓、华雄、李傕、郭汜、张济、樊稠、张绣）；蛮兵（孟获、祝融、沙摩柯、兀突骨、蹋顿）；其他（袁术、张鲁、祖茂、公孙瓒） | 750×950 PNG | **主用**：低阶武装的骨相、装备层级、胡须与头巾形状 |
| `Library/Locus/tmp/soldier_refs/musou/` | 15 | 《真·三国无双8》官网全身与平服立绘（董卓、华雄、孟获、张角、祝融、袁术）＋《ORIGINS》主人公与起源 1/3 号半身 | 全身≈1000×1000；平服 697–800×1000；起源 380×677 | 平服图最容易读面部骨相；ORIGINS 主人公等同无名义勇兵 |
| `Library/Locus/tmp/soldier_refs/sanguosha_bing/` | 4 | 百度图库缩略图 | 500–888px | 低置信，仅备用 |
| `Library/Locus/tmp/soldier_refs/musou_bing/` | 8 | 百度图库缩略图 | 500–881px | 低置信，多为攻略封面，不建议用 |

### 28.3 已删除，不得再引用

原 `Library/Locus/tmp/soldier_refs/_rejected_tw3k_3d/`（245 张《全面战争：三国》单位图）已由用户删除。用户判定其为 3D 实机截图、不是立绘，不能作为面谱参考。任何后续方案不得再引用这批图，也不得重新抓取同类实机截图充当面谱参考。

### 28.4 一个必须记住的事实

《三国杀》与《真·三国无双》都没有真正意义上的兵卒官方立绘：三国杀牌面全部是具名武将（官网武将库 696 条，按“兵 / 卒 / 贼 / 黄巾”筛只有 1 条误命中）；无双官网只有 94 名武将与 4 名 NPC 的立绘，兵卒只存在于实机模型与实体设定资料集。

因此 §28 里的“兵卒”实际是各势力最底层的具名人物（黄巾、山贼、蛮兵、董卓军杂将），这是当前网络条件下能拿到的最接近一档。不要为了凑“兵卒”再去抓实机截图。

### 28.5 与六兵种的对应

| 项目兵种 | 主要取样 | 取什么 |
| --- | --- | --- |
| 民兵 | 刘辟、裴元绍、ORIGINS 主人公 | 瘦颊、骨点、无修饰的薄嘴 |
| 轻甲刀兵 | 张燕、管亥、周仓 | 头巾下的窄眼；眉脊与肩宽的厚度 |
| 盾卫 | 周仓、张绣、祖茂 | 方下颌、厚下唇、短髯只占下颌中央 |
| 长枪兵 | 李傕、张宝 | 细长眼配窄长脸型 |
| 弓手 | 张燕、沙摩柯、祝融 | 眯眼、太阳穴收窄、眼下挤压带 |
| 重甲老兵 | 樊稠、兀突骨、董卓、华雄 | 法令纹与颊凹；骨相夸张上限 |

`张角`（削到极限：颊凹、颧突）与 `华雄`（压到极限：眉脊、髭、宽鼻）只作两端标尺，不直接做成任何兵种脸。

### 28.6 结构表状态

6×6 结构表（眉 / 眼 / 鼻 / 嘴 / 颊部阴影 / 胡须 × 六兵种）的草案已在 2026-10-23 的对话中给出，**尚未经用户逐格核对**，因此不写入本文。核对通过后，它才成为下一版生成词的唯一依据。

### 28.7 使用边界

- 这些图只做人工阅读与结构提炼。§3 明确禁止把网上临时图片上传给生成模型；若要把某一张作为编辑输入，必须按 §3 改成单图编辑方案并重新批准。
- 三国杀与无双都是厚涂写实向：**只取骨相、装备层级、胡须与头巾形状，不取笔触、材质、光照**。画风基准仍是 `design/art-style-guide.md` 与 1011 六帧。
- 参考图存在不等于可以发起生成。任何新请求仍须按 §0 新增章节、复述参数、取得当次明确批准；读 §28 或改本文都不是批准。
