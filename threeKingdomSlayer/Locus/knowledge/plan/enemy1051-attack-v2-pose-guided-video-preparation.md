---
id: kd_b4ae73ce-9a99-4a05-86e0-dcb39287678d
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: 1051旧身份射箭v2方案已准备未提交：1051 v3-A加旧105 Attack1/2/3三张真实姿态参考，10秒聚焦拉弓/松弦，不演取箭。2026-10-25新画风入口见plan/enemy1051-v11-style-idle-replacement-handoff.md；新身份定稿后应保留这三张动作图，更新图1及身份/画法/尺度文本再审阅，不能直接按旧版发起。
---

# 1051射箭视频v2：1051身份图＋旧105 Attack三图姿态引导

**2026-10-25 接续提示：105 新画风制作见 `plan/enemy1051-v11-style-idle-replacement-handoff.md`。** 本文件保留旧身份的未提交方案；制作新画风射箭时，待新身份定稿后更新图 1 和正文身份／画法／尺度说明，并重新审阅四张输入。图 2–4 的旧 105 实际姿态参考仍须保留，不能又改成只传 Idle。本提示未创建视频任务，也未恢复任何去背。

## 1. 当前目标与边界

用户指出旧105已有可用的完整拉弓射击组图，不能看过后只转写成泛化文字、让生成器从Idle重新猜姿态，随后要求准备新的视频生成方案。

**状态：新方案与三张旧105动作绿幕副本已准备，未上传、未调用视频接口、未创建task或成片。** 本文件是可审阅交接，不构成收费生成批准；用户确认四张输入、完整双语词及参数后，另行明确发起才提交一次。

- **实际纳入四张普通参考图**：图1为1051身份与画风，图2/3/4分别来自旧105 Attack1/2/3，明确作为有序动作姿态参考。
- **聚焦攻击关键段**：从已经搭箭的Attack1关系开始，连续拉到Attack2，快速松弦到Attack3，再自然收回1051 Idle。**不再要求先演完整取箭、转箭、搭箭流程**，不把取箭当核心验收目标，不额外制作新峰值图。
- 保留单支**10秒**源片规格；时间用于连接已有动作关系，不要求游戏Clip长10秒。v1已经证实延长时长不能代替射箭几何控制，本版主要变化是实际姿态输入和明确手/弓/箭关系。
- 不上传v1失败成片帧，不复制横弓、换手、拿反箭等错误。不生成新身份图，不改旧105 Sprite/Clip/Controller/Prefab/脚本，不进入Play。
- 1051形象、6秒Idle源片及六帧候选已经验收，保持原状；静帧去背与Idle六帧抠图仍按用户要求暂缓，不借攻击任务恢复。

## 2. 相比失败v1，具体改什么

### 2.1 需要纠正的方案取舍

v1虽然本地查看了旧105三帧，但实际只上传1051 Idle。这保护了新身份，却没有将旧105的具体射箭姿态交给模型，偏离用户“参考已有105”的重点。新方案不再用“怕复制旧脸/旧甲”作为省略动作图的理由，而是把图的职责和身份风险明确交给用户审阅。

### 2.2 射箭结构优先级

1. **1051身份不变**：图1决定身体、遮面、轻装、深色布料、比例和弓的材质身份；不把角色变成图2–4的旧105。
2. **旧105姿态关系不丢**：按图2→图3→图4继承正面竖弓、双手位置、推弓/拉弦、箭的透视、松弦后的手形。不是只继承“会射箭”的抽象语义。
3. **动作连接要成立**：三张是运动路标，不是三个静态画面硬切；允许肩肘腕小幅自然调整，不要求旧角色的部件大小替代1051。
4. **竖弓明说**：整个搭箭受力、满弓和松弦阶段，上弓梢在下弓梢上方，不能将弓横成胸前的一根水平物件。
5. **双手明说**：画面右侧手持续持弓；画面左侧手持续拉弦/松弦，不交换、不镜像。
6. **箭的前后端明说**：箭尾槽接弦、羽毛端靠人物和拉弦手；金属箭头在前、穿过弓的支承关系朝玩家。不能把羽毛朝玩家、箭头握在后手边，也不能沿画面左右横放在弓身上。

v1原文确实写过“nock at the center of the bowstring”，因此不能归因为“完全没写箭尾接弦”。这次需要的是**真实姿态图＋可读的前后关系＋竖弓约束**，不是再追加一句旧要求就声称有保证。

## 3. 四张实际输入与上传顺序（待用户审阅）

统一要求：四张皆为**1024×1024不透明RGB PNG、背景RGB(0,159,62)**；各自四角64×64已回读为该精确色、std=0。四张分别上传一次，按以下顺序放进 `input.content`，角色全部使用 `reference_image`。不用 `first_frame` / `last_frame`，不混参考视频。

### 图1：唯一身份/画风/装备/尺度基准

```text
C:\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\enemy1051_idle_video_v1_6s\01_idle_v3A_green_009F3E_1024.png
```

- 445,447 bytes；SHA-256 `64a0f77765cc4a19fe35e1d0d6b1bca7c063883f97379e741c35cc0fd476a4fc`。
- 已验收1051 v3-A造型的绿幕上传副本；本轮未改。
- 定义蒙面、不露脸、深灰褐/橄榄布衣、露出手臂小腿、短壮身体、木弓与箭壶身份和像素画风。
- 当前源图约12%上下留边已被接受，不能读旧文15%条款后偷偷缩图。视频动作要求至少10%安全边。

### 图2：旧105 Attack1——进入拉弓的姿态关系

```text
C:\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\enemy1051_attack_video_v2_10s\02_old105_attack1_pose_green_009F3E_1024.png
```

- 126,497 bytes；SHA-256 `f7f14a4ceddb96859c93b233ce2c2818fd4e416aaa986942e5e5938d8bba4c96`。
- 来源 `Assets/Sprites/Enemy/Enemy5/enemy5_attack1.png`，源SHA-256 `ec8f9e06071e8464a5ff28c99acab43e42fcf01a72cc3a781b94ae53bf2a9785`。
- 只定正面构图、竖弓、左右手和已经搭箭的空间关系，不复制旧脸、甲、红巾或画风。

### 图3：旧105 Attack2——进一步拉弓/满弓的姿态关系

```text
C:\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\enemy1051_attack_video_v2_10s\03_old105_attack2_pose_green_009F3E_1024.png
```

- 127,449 bytes；SHA-256 `c250042fcc4b0d52a17d6ae5d32f2a7f7beeb7bab371081ca6510ded41e7ccb7`。
- 来源 `Assets/Sprites/Enemy/Enemy5/enemy5_attack2.png`，源SHA-256 `0c1027f9781f8d9e16e411725bd1b1e90da245352f326ed6af0bdb9e7863b49b`。
- 只定推弓手/拉弦手对向受力、脸旁拉弦关系、箭朝玩家的透视缩短及满弓构图，不另发明横弓或侧射。

### 图4：旧105 Attack3——松弦/放箭后的姿态关系

```text
C:\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\enemy1051_attack_video_v2_10s\04_old105_attack3_pose_green_009F3E_1024.png
```

- 132,151 bytes；SHA-256 `99bc3f6052c4d266b481200457a63069132063af674a2b582f6610129b58fbad`。
- 来源 `Assets/Sprites/Enemy/Enemy5/enemy5_attack3.png`，源SHA-256 `66adac7b9a42c6dbe0873005c57f84eb0f489e5e3cd07f570164998c22e8a4bc`。
- 只定已经松指、箭已离弓、持弓手继续稳住和拉弦手放松后的关系。
- 旧图外侧浅色放箭装饰已仅在本地副本清理；不把旧特效上传成1051的新特效。

### 准备方法与限制

- 三张旧图源文件有真实Alpha，均将**完整512×512源画布用同一2倍Nearest变换**变为1024×1024，再按原Alpha合到绿幕；没有逐图trim/fit、没有平移重锚、没有分开拉伸武器或重绘人物。
- 图2/3保留源Alpha。图4保留源图 `Alpha≥128` 的最大连通主体（34,576px）及外扩2px轮廓内原Alpha，外侧409px的Alpha被清除；源RGB未改、主体不重绘，源Assets文件全部未改。
- 不透明源主体RGB经合成后变化0；半透明边缘合到绿幕必然产生必要RGB变化，不能称为全图逐像素不变。
- 旧105与1051体型/画布占比不同，旧图只定姿态关系，不是1051尺寸标尺；输出尺度固定服从图1。不通过逐帧归一包围框抹掉实际动作。
- **风险仍在**：图2–4仍看得见旧脸、发髻、棕甲和红巾。明确角色分工能降低歧义，但不保证模型不复制身份。发起前须看图审阅，不自动承诺四图方案必然成功；若用户不接受这些可见身份内容，先停下另定参考制作方式，不能自行换成单图又称“已用旧105组图”。

### 审阅图与准备留档（不能上传审阅组图）

```text
C:\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\enemy1051_attack_video_v2_10s\FOUR_INPUTS_REVIEW.png
C:\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\enemy1051_attack_video_v2_10s\OLD105_ATTACK3_ALPHA_REVIEW.png
```

第一张为四输入总览；第二张左为原Attack3灰底预览、右为清理副本灰底预览。实际只上传前述四张无文字PNG，不上传总览或灰底预览。

准备记录：同目录 `pose_upload_preparation.json` 与已执行的 `prepare_pose_uploads.py`。脚本不是生成脚本，不覆盖重跑。Library目录不随Git迁移，异机发起时须传递四张同一PNG并核对哈希。

## 4. 动作与时间（一次拉弓射击，不是取箭全流程）

建议继续**10秒 / seedance-2-fast / 720p / 1:1 / 无音频无水印 / seed=-1**，仅本次新方案待审，不沿用v1的收费授权。

| 约略时间 | 主要动作 |
|---|---|
| 0–0.6s | 已经是1051自己的图2攻击关系，竖弓、箭已搭上；不是先播大段Idle |
| 0.6–3s | 连续肩肘腕动作，从图2关系拉至图3满弓关系 |
| 3–4s | 满弓短暂稳定、可读，身体保有受力，不反复抽拉 |
| 约4–4.3s | **一次快速松指/松弦**，从图3进入图4，箭离开弓 |
| 4.3–5.5s | 图4释放关系和短促后随，持弓手稳住 |
| 5.5–8.5s | 自然降手、收弓回图1自己的Idle，不倒放拉弓 |
| 8.5–10s | 原Idle收稳，不再取箭或射第二发 |

时间可略浮动，优先真实运动路标和关节连接。不需要再演一套取箭、翻转箭、绕头搭箭；起始已经搭好箭，不是箭在镜头内凭空变出。只射一箭。

## 5. 完整英文正文（实际提交时与§7英文负面词合并）

```text
Create one continuous 10-second 2D game-sprite bow attack using all FOUR supplied images in the stated roles.

IMAGE 1 is the ONLY character-identity and rendering reference: animate this exact masked 1051 archer, his dark olive-gray/brown cloth, bare limbs, short stocky proportions, orange eye slit, wooden bow, quiver, drawing scale and hard pixel-cluster style. Keep his face covered. IMAGES 2, 3 and 4 are the old 105 Attack1, Attack2 and Attack3 sequence. Use them as ORDERED MOTION LANDMARKS for the front-facing upright-bow shooting pose: body-to-bow layout, hand positions, draw geometry, arrow foreshortening and released fingers. Do not copy their old soldier, exposed face, hair, red headband, armor, costume colors, rendering style, body size or effects. Retarget their action onto IMAGE 1's character; do not morph the character into the old soldier.

Start already in the nocked attack relationship of IMAGE 2, drawn as the 1051 archer. Continue naturally into the stronger drawn/aimed relationship of IMAGE 3, release ONCE into the hand-and-bow relationship of IMAGE 4, then lower the hands and bow back into IMAGE 1's idle. This clip focuses on drawing and releasing the bow, not fetching an arrow from the quiver. No separate arrow-fetch performance or additional shot.

CRITICAL GEOMETRY:
The bow stays UPRIGHT throughout preparation, full draw and release: upper bow tip above lower bow tip, as in IMAGES 2-4. It may follow the small angles in those references, but never rotates into a horizontal bar across the chest. The hand on SCREEN-RIGHT holds IMAGE 1's same bow grip throughout; the hand on SCREEN-LEFT draws and releases the string. Never exchange or mirror those hand roles. Keep the front-facing body and forward shooting axis of the old Attack sequence, not a side-on archery pose.

The arrow's NOCKED TAIL and FLETCHING belong behind the bow, near the character and the drawing fingers. The tail nock is attached to the drawn string. The metal ARROWHEAD belongs ahead of the bow, pointing toward the player/camera. The arrow is strongly foreshortened as in the old attack references: the viewer sees the point end, not a feathered tail presented to the target. Do not lay a long sideways shaft along a horizontal bow, hold the metal head in the drawing hand, aim the feathers forward, reverse the arrow, or disconnect its nock from the string. Match the reference forward geometry using a proportionate arrowhead, not a screen-filling projectile.

BUILD AND RELEASE TENSION:
From IMAGE 2's relationship, the bow hand pushes forward steadily while the drawing elbow opens and the drawing fingers pull the nock/string toward the covered cheek. Reach IMAGE 3's relative wrist, elbow, string and bow arrangement through visible partial-draw positions. The taut string follows the drawing fingers and the bow limbs flex as the same wooden bow, without changing length, grip or identity. Keep the full draw readable briefly. Then the drawing fingers open in ONE quick release; the string returns and the bow recovers elastically. Use IMAGE 4's released-hand relationship, with a short natural arm follow-through, not a hand throwing an arrow forward. The last drawn pose and first released pose must be distinct. After release no arrow remains nocked. Do not add a long flying-arrow sequence, giant arrow zooming at the lens, projectile trail, flash or release effect; the game engine will supply the actual traveling projectile.

CONTINUITY AND PACING:
These three action images are movement landmarks, NOT three still pictures to display in sequence. Overlap shoulder, elbow, wrist and finger motion naturally; keep connected anatomy and real intermediate poses. No hard pose swaps, crossfades, melting, rigid-card rotation or multi-second static aiming. Loose timing: 0-0.6s already nocked in IMAGE 2's relationship; 0.6-3s draw continuously toward IMAGE 3; 3-4s readable full draw; around 4-4.3s one fast release toward IMAGE 4; 4.3-5.5s follow-through; 5.5-8.5s lower into IMAGE 1's idle; 8.5-10s quiet idle. Do not rewind the shot, fetch another arrow or start another attack.

SPRITE PRESENTATION:
Keep IMAGE 1's character, bow design, drawing scale, palette and lighting, one locked front-view camera and square canvas. Shoes, pelvis and formation slot stay planted; local shoulders and arms articulate without a whole-body turn or foot slide. The whole bow, string, hands, shoes, feather ends and short cloth tips remain inside the frame with at least ten percent clear margin. Use compact joint paths rather than shrinking or reframing the figure midway. Keep the same flat chroma-green background RGB(0,159,62) as all four prepared images. No floor, shadow, scenery, second character, new equipment, particles, text or UI.
```

## 6. 完整中文对照（供审阅，不提交）

```text
使用提供的全部四张图，按各自职责制作一支连续10秒的2D游戏精灵弓箭攻击。

图1是唯一的角色身份与绘制基准：动画中必须是这个确切的蒙面1051弓手，保留暗橄榄灰褐布衣、露出的四肢、短壮比例、暖橙窄眼缝、木弓、箭壶、绘制尺度和硬边像素块画风，全程遮面。图2、图3、图4是旧105的Attack1、Attack2、Attack3顺序组图，只作为有序动作路标，定义正面竖弓射箭的身体与弓布局、手位、拉弦几何、箭的透视缩短和松弦手指。不要复制旧士兵、露脸、头发、红头巾、护甲、服装颜色、旧画风、身体尺寸或特效。把旧动作移到图1角色身上，不把角色变形成旧士兵。

起始已经是图2“箭已搭好”的攻击关系，但画成1051。自然拉到图3更充分的拉弓/瞄准关系，再只释放一次进入图4的手与弓关系，随后降手收弓回图1的Idle。本支重点是拉弓和释放，不是从箭壶取箭；不另演取箭流程，不追加第二发。

关键几何：
搭箭受力、满弓和释放期间，弓始终竖立，上弓梢高于下弓梢，如图2–4。可以有参考图里的小角度，但不能横成胸前的一根水平物件。画面右侧手始终握图1同一把弓的同一握点；画面左侧手负责拉弦与释放，不交换、不镜像。继承旧Attack组图的正面身体和向前射击轴线，不改成横版侧面射箭。

箭尾槽和羽毛端在弓后、靠近人物与拉弦手指；箭尾槽连接拉开的弓弦。金属箭头在弓前，指向玩家/摄像机。箭如旧图一样明显透视缩短：玩家看到的是尖端，不是对着目标的羽毛尾。不能把一根长箭沿横弓侧放，不能拉弦手握金属箭头，不能羽毛向前、箭矢反向或箭尾离开弦。按参考图的向前几何绘制比例合理的箭头，不画占满屏幕的飞行物。

建立和释放张力：
从图2关系开始，持弓手稳定向前推，拉弦肘打开，手指把箭尾和弦拉向被遮住的脸颊旁。通过看得见的半拉弓中间姿态到达图3的手腕、肘、弦和弓的相对关系。绷紧弦随手指移动，弓臂作为同一把木弓受力弯曲，不改变长度、握点或身份。满弓短暂可读后，拉弦手指只快速松开一次，弦回弹、弓弹性恢复。采用图4的释放手形与短促自然后随，不是用手把箭扔向前。最后满弓和第一张释放姿态要清楚不同。释放后弓上不再搭着箭。不演长距离飞箭、巨箭冲镜头、箭轨、闪光或放箭特效，真正移动的箭后续由游戏引擎生成。

连接与节奏：
三张攻击图是动作路标，不是依次展示的三张静止画。肩、肘、腕、指有自然重叠，解剖相连，并生成真实中间姿态。不硬切、不叠化、不融化、不把贴图当卡片旋转，不满弓冻结数秒。宽松节奏：0–0.6秒已经搭箭、处于图2关系；0.6–3秒连续拉到图3；3–4秒满弓可读；约4–4.3秒快速释放到图4；4.3–5.5秒后随；5.5–8.5秒降手收弓回图1Idle；8.5–10秒安静待机。不倒放射击、不再取箭、不开始第二发。

精灵呈现：
保持图1身份、弓的设计、绘制尺度、配色与光照，固定正面机位和正方形画布。双鞋、骨盆、阵位稳定，上身肩臂可以动，但不全身转向、不滑脚。整把弓、弦、双手、鞋、羽毛和短巾尾始终完整在框内，四边至少10%空白。关节路径紧凑，不在中途缩小人物或重新构图。保持与四张上传副本一致的平坦绿幕RGB(0,159,62)。无地板、投影、场景、第二个人、新装备、粒子、文字或UI。
```

## 7. 英文负面词与中文对照

实际将以下英文负面词接在§5正文后，合并同一个text：

```text
Avoid horizontal or sideways bow rotation, crossbow posture, reversed arrow, feathers aimed at the viewer, metal arrowhead held by the drawing hand, swapped hands, mirrored equipment, disconnected nock or string, duplicate strings, sideways shooting, full side turn, copying the old exposed face, hair, red headband, armor or cartoon style, giant projectile, release flash, extra shot, arrow-fetch performance, long frozen aim, hard pose swaps, crossfade, melting limbs, whole-body scaling, foot sliding, camera movement, cropping, floor, shadow, scenery, particles, text and UI.
```

中文对照：不要横弓或弓横转、弩姿、箭矢反向、羽毛对玩家、拉弦手握金属箭头、双手交换、装备镜像、箭尾或弦脱开、多弦、侧向射击、完全侧身；不要复制旧脸、发型、红巾、护甲或卡通画风；不要巨型飞行物、放箭闪光、第二发、额外取箭表演、长时间冻结瞄准、硬切姿势、叠化、肢体融化、全身缩放、滑脚、镜头运动、裁切、地板、投影、场景、粒子、文字或UI。

不要附加Idle的“禁止拉弦/弓形不许变化”等词。本支需要受力弯曲和松弦，保持的是同一弓身份和尺寸。

## 8. 请求参数、输出与发起侧纪律

| 项 | v2方案 |
|---|---|
| 模型 | `seedance-2-fast` |
| 时长 | 10秒 |
| 分辨率 / 比例 | `720p` / `1:1` |
| 音频 / 水印 | `false` / `false` |
| seed | `-1` |
| 图像输入 | §3四张独立PNG，按图1/2/3/4顺序，全部普通 `reference_image` |
| 背景 | 不透明绿幕RGB(0,159,62)，不承诺Alpha视频 |
| 提交次数 | 用户明确发起后仅一次，失败/超时不自动重建 |

建议输出（目前不存在）：

```text
C:\Users\steam\Videos\doubaoVideo\enemy1051_attack_v2_10s.mp4
```

留档目录：

```text
C:\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\enemy1051_attack_video_v2_10s\
```

当前该目录只有本地参考图准备文件、报告和审阅图，不代表发过请求。

1. 发起前向用户呈现四张输入及职责、完整中英文词/负面词、10秒规格和新输出路径，取得明确批准。用户本轮“准备新的视频生成方案”不等于已经授权生成。
2. 复核四张PNG各自哈希、模式、尺寸与四角绿幕。四张各上传一次，不拿总览图代替，不漏旧Attack三图再声称实际用了动作组图。
3. text仅为§5英文正文＋两个换行＋§7英文负面词。不上传失败v1帧，不拼入v1的取箭流程，也不添加first_frame/last_frame。
4. 创建一次；超时/未知结果先调查同一请求，不重复收费。查询/下载失败不重建task。不得直接运行已执行的v1提交脚本。
5. 按 `skill/reachapi-seedance-video-generation-v2.md` 有限查询、断点下载、完整解码与回读，保存真实输入清单、词、参数、task、usage/cost、成片哈希，不保存凭据。
6. 完成后先交用户看片，不自动正式选帧、抠图、补边、建Clip、配置发箭键或运行Unity自检。所有已验收Idle素材与旧105资产保留。
7. 本版的角色/姿态分工只是控制目标，不是技术保证。若复制旧脸/甲、仍横弓/反箭或转成侧射，保留失败结果报告，不自改重发。

请求示意（尚未发出；不提交占位符）：

```json
{
  "model": "seedance-2-fast",
  "input": {
    "content": [
      { "type": "text", "text": "<§5英文正文 + 两个换行 + §7英文负面词>" },
      { "type": "image_url", "role": "reference_image", "image_url": { "url": "<图1 URL>" } },
      { "type": "image_url", "role": "reference_image", "image_url": { "url": "<图2 URL>" } },
      { "type": "image_url", "role": "reference_image", "image_url": { "url": "<图3 URL>" } },
      { "type": "image_url", "role": "reference_image", "image_url": { "url": "<图4 URL>" } }
    ],
    "duration_seconds": 10,
    "resolution": "720p",
    "aspect_ratio": "1:1",
    "generate_audio": false,
    "seed": -1,
    "watermark": false
  }
}
```

## 9. 验收重点与后续边界

### 本次视频优先验收

1. **身份**：从头到尾是图1蒙面轻装1051，没有换成图2–4旧105；不露鼻嘴，不长棕甲、红头巾或发髻，画风、颜色和尺度稳定。
2. **姿态依据**：把满弓和释放画面与旧Attack组图并排，检查竖弓、手位、箭的透视/前后端和躯干构图，不用“看起来会射箭”替代姿态比对。
3. **不可丢的几何**：右侧手持弓、左侧手拉弦；上弓梢在上、下弓梢在下；箭尾/羽毛靠人物与弦、箭尖朝玩家；不能再次变成横弓、弩式端持、换手或反箭。
4. **动作**：从图2拉到图3，能看见中间受力，再有明确的一次松弦到图4、短促后随和自然回Idle。只匹配首尾姿态或逐图硬切不代表动作完成。
5. **完整性**：弓梢、拉弦肘、手、羽毛和巾尾不触边，动作期间至少10%安全边。不自动缩放补救后宣称原片没有越界。
6. **特效边界**：不复制旧浅色闪光、不演长距离巨箭飞行；释放后没有持续搭箭或第二发。

### 技术/颜色与素材选择

- 完整解码并报告实际尺寸/fps/帧数/时长；720p是档位，不假定一定720×720或固定24fps。
- 背景逐帧量测确认无人空区，记录相对上传色RGB(0,159,62)的范围、首尾及最差帧；配色与同一身份图、已验收Idle另作稳定材质区比较，不能拿全图均值直接判主体变色。
- 平台/单帧换图作为选帧结构依据，不自动否决；用户判断动作是否有用。不得用噪声、融化或重复平台帧凑姿态数。
- 视频通过后再授权选帧，优先保留半拉、满弓、最后蓄力、第一松弦和后随；纯Idle不占攻击帧数。去背、对齐、接入与游戏验收另行授权。
- 旧105的2s计时、exit0.75等静态风险仅供后续接入参考；不按旧时序强迫源片，不在本轮修代码。实际箭Prefab、起点和击发时刻由接入侧后续处理。

## 10. 可复制给发起侧

> 请读取 `Locus/knowledge/plan/enemy1051-attack-v2-pose-guided-video-preparation.md`。这是纠正v1未实际使用旧105动作图的新方案：§3四张PNG都要上传，图1定1051蒙面轻装身份，图2/3/4定旧105 Attack1→Attack2→Attack3姿态。四张全部普通reference_image，10s/720p/1:1/seedance-2-fast/无音频无水印。重点是竖弓、右侧持弓左侧拉弦、箭尾羽毛靠人物与弦、箭头朝玩家；从已搭箭攻击姿态开始拉弓松弦，不另演完整取箭，不用失败v1帧。text为§5英文正文＋§7英文负面词。四输入和双语词须先经用户审阅，明确发起后只创建一次；下载解码后交用户看片，不自动抠图/接入，不恢复Idle去背。旧105露脸和护甲仍在参考图上，需报告身份泄漏风险而非保证能完全隔离。
