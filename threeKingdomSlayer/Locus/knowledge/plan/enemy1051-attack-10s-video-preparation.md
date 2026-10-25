---
id: kd_f10ee3b7-dfb3-42e1-b6c4-11cf4ede37ae
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: 1051射箭v1已验收不通过：横弓、换手、反箭，未落实旧105姿态。本文仅保留失败历史，不再提交；当前v2见plan/enemy1051-attack-v2-pose-guided-video-preparation.md，已准备1051身份图加旧105 Attack三图四参考方案，未发起。
---

# Enemy 1051：射箭视频v1失败记录 / 原10秒方案历史

## 1. 最新目标、已完成项与本次边界

用户已验收1051的新Idle源片与6帧循环候选，现要求：“下一个是拉弓射箭的关键动作，参考已有的105射箭动作去准备视频生成方案，写入交接文档”。

**当前停点：v1已提交一次并验收不通过——“箭矢拿反了”“射击姿态与105完全不一致”。本文件§2–10为当时单图方案历史，不再提交；其中“不上传旧105”和完整取箭流程已被新方案取代。** 用户指出旧105 Attack组图足以提供拉弓射击参考后，已准备 `Locus/knowledge/plan/enemy1051-attack-v2-pose-guided-video-preparation.md`：1051身份图＋旧105三张实际姿态输入，共四张普通参考；未提交v2。失败证据及方案错误见本文件§12，v2入口见§13。

- 身份：以已验收的1051 v3-A轻装形象和已验收Idle为准，暗褐灰橄榄头巾/面巾、灰褐短褂、露出四肢，**不露脸**。
- 参考旧105的**正面射箭方向、持弓/拉弦手关系、蓄力与松弦语义**，不复制其脸、重甲、红头巾、旧画风或放箭特效。
- 建议 **10秒源片，一次完整射击**：短Idle → 取一支箭 → 搭箭举弓 → 拉弓至满弓 → 一次松弦 → 跟随收势 → 回原Idle。用户此前6秒只指定Idle，本次完整动作较多，提出10秒供发起前确认，不把10秒当作用户已明确批准的请求。
- 不先做满弓或放箭关键图，不拆成多支视频；给自然关节过渡留空间，只上传1051自己的身份图。
- 当前Idle的6张原始候选保持不变，静帧阶段B与Idle帧去背均继续暂缓；本轮不抠图、不对齐补边、不建Clip、不替换旧105或Prefab、不进入Play。

## 2. 旧105已核对的实际证据（本地查看，不上传）

### 2.1 三张旧攻击图

| 资产 | 观察到的主要内容 | 新1051继承什么 / 不继承什么 |
|---|---|---|
| `Assets/Sprites/Enemy/Enemy5/enemy5_attack1.png` | 正面，弓从身体右侧移到身前，双手已参与搭箭/拉弦，箭头朝向玩家、透视缩短 | 继承正面朝玩家射箭，不复制裸露的五官、棕甲或巨大的装饰性箭头 |
| `Assets/Sprites/Enemy/Enemy5/enemy5_attack2.png` | 画面右侧手握弓，画面左侧手拉到脸旁，弦处于受力关系 | 继承弓手推弓、拉弦手向面部收回的受力对向关系 |
| `Assets/Sprites/Enemy/Enemy5/enemy5_attack3.png` | 画面左侧拉弦手张开、箭已离弦、弓回弹，旧图带浅色放箭效果 | 继承“手指松弦后”的可读姿态，不复制浅色特效、箭雨或新装备 |

另已查看 `Assets/Sprites/Enemy/Enemy5/enemy5_idle.png`。旧105只有这三张攻击姿态，不提供完整取箭、搭箭过渡和回Idle，因此“参考旧105”不等于原样重演三个静态图。

### 2.2 Clip、控制器与远程配置（本轮Edit Mode只读）

- `Assets/Animations/Enemy_105_Attack.anim`：30fps、length **2.766667s**、非循环、无AnimationEvent；Sprite键为 `enemy5_attack1 @0.0`、`enemy5_attack2 @1.5`、`enemy5_attack3 @2.733333`。
- `Assets/Animations/Enemy_105.controller`：Attack speed=1，`Attack → Idle` 无条件hasExitTime，exit=**0.75**、duration=0。
- `Assets/Resources/EnemyPrefabs/Enemy_105.prefab`：`isRanged=true`、`attackSequence[0]` 为 spawn=**2s** / draw=**1.5s** / useFlip=false / **hitFrames为空**；箭Prefab为 `Assets/Prefabs/arrow.prefab`，弧高3、飞行时长1、落点随机半宽1.2。
- 当前 `Assets/Scripts/Enemy/Enemy.cs` 的远程无命中帧分支：先等spawnDuration，再 `SpawnProjectile()`；若配置命中帧，则在对应Sprite实际显示时击发。`SpawnProjectile()` 从敌人Transform位置生成独立飞行物，不是从视频内画出的箭矢轨迹读取。
- `Assets/Scripts/Enemy/EnemyProjectile.cs` 负责独立抛物线、到达伤害、Parry与销毁，箭飞出后不跟随敌人死亡/受击。

**旧时序风险：** 按Clip长度×exitTime静态推算，旧Attack约 **2.075s** 即满足回Idle条件，而旧松弦图键在2.733s，放箭姿态可能被提前切走；当前2s计时发箭也不等于该旧Sprite出现的时刻。这是静态证据，**未进入Play验证，本轮不修复旧105**。新素材需要保留明确的松弦帧，后续接入时由接入侧重新对齐，不照抄旧退出比率和旧三键间隔。

## 3. 新射箭动作的情景与关键读点

### 3.1 方向与两只手

- **射击目标在摄像机/玩家方向**，不是横版朝画面右侧射出，也不是朝天射箭。
- 画面右侧手始终是持弓手，保持同一握持点。弓由待机右侧垂持位举到身前、中央偏右，朝玩家推出，不换手、不镜像。
- 画面左侧空手是取箭与拉弦手，先到左肩后的箭壶取一支箭，再带到弓前搭箭。搭好后向面巾旁拉回，肘向画面左侧打开。
- 正面机位不动；躯干可以有必要的轻微关节调整，不把角色转成完整侧身弓手，不重新画三分之四或背面造型。脚、骨盆、腰带根部和整体阵位稳定。

### 3.2 取箭、搭箭和受力

- 从现有空手Idle开始，只取**一支普通木杆、少量羽毛、普通金属箭头的箭**。手确实到箭壶，箭沿手的路径移动，不凭空出现在弓上。
- 搭箭时箭尾接上弓弦中心、箭杆与握把附近的支承关系成立，箭朝玩家，因正面透视可以明显缩短。只需小而可读的箭头，不能为“正面”生成占满胸口或屏幕的巨大箭头/矛。
- 推弓与拉弦同时建立张力：持弓手朝玩家方向稳定推出，拉弦手退到面巾旁、拉弦肘向外打开。弦由松弦时的直线变为朝拉弦手汇聚的**受力V形**，弓臂自然弹性弯曲，不能让手拉开但弦不动。
- 满弓短暂停留供看清预备，不做数秒冻结，也不不断抽拉。弓仍是同一把弓，长度、材料、握把不变，只有真实受力变形。

### 3.3 放箭、跟随和回位

- 关键是**手指松弦**，不是用手把箭扔出去：拉弦手张开/松指，沿原拉力方向有短促后随；弦迅速回到松弦关系，弓有很小的弹性回摆。
- 松弦后箭不再搭在弓上，空手无第二支箭。视频只需很短的近弓离弦读点，不生成长距离飞行、轨迹弧线、慢镜巨箭或向玩家扑来的放大箭。
- 实际飞行物由引擎负责，源片中不要留下独立飞箭让后续Sprite与Prefab形成双箭。明确区分最后一帧满弓与第一帧已松弦的身体关系。
- 随后拉弦手从脸旁自然降回左侧，持弓手将弓收回原来的右侧垂持位，回到原轻装Idle。不是倒放动作，不再取下一支箭，不进行第二次射击。
- 不强制箭壶射前射后每支箭羽完全一样：允许实际取走一支时局部遮挡/变化，但不能整壶跳动、增殖、换边或在收势阶段多取一支来凑相同尾图。

### 3.4 10秒源片的宽松节奏

| 时间 | 内容 |
|---|---|
| 0–0.5s | 短暂原Idle，无长等待 |
| 0.5–2s | 取一支箭，并举弓到身前 |
| 2–3s | 搭箭，弦与箭尾连接明确 |
| 3–5s | 连续推弓/拉弦，进入满弓，末端短暂可读 |
| 约5–5.5s | **仅一次快速松弦**，手指与弦释放，不把这一拍拖慢 |
| 5.5–7s | 弓/手臂短促后随与收稳 |
| 7–9s | 降手、降弓回Idle |
| 9–10s | 保持原警戒站姿，微弱呼吸，不开始下一次射箭 |

时间可自然浮动，以“取到箭—搭上—受力—松弦—收势”因果为先；不能等到最后一秒硬切完。10秒是源片，不是游戏攻击CD、前摇或Clip时长。

## 4. 唯一上传图与规格

### 4.1 复用已准备的1051身份绿幕副本

```text
C:\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\enemy1051_idle_video_v1_6s\01_idle_v3A_green_009F3E_1024.png
```

本次已回读/查看：RGB PNG / 1024×1024 / 445,447 bytes，SHA-256：

```text
64a0f77765cc4a19fe35e1d0d6b1bca7c063883f97379e741c35cc0fd476a4fc
```

四角各64×64均精确 `(0,159,62)`、std=0；文件未改。该副本来自已验收v3-A原图（SHA `553f30c66b2e91713670fdda8d69bf9d5869a2095a1c391fa3e694a2802288a0`），只归一原图外部背景、保护主体与边缘2px，无缩放/平移/Alpha生成，详见Idle方案§3。

- **本次使用单张普通 `reference_image`**，只定身份、起始/收势姿态、尺度、画风和绿幕；不是强制首尾帧。这样允许箭壶取箭后的局部变化，不为凑尾图让箭瞬移复原。
- **不上传旧105的三张图**：用户要求“参考”已落实为本地查看与动作文字，不把旧角色露脸、重甲或浅色特效输入生成器。发起侧不能自行追加旧105帧或其他动作参考；要改参考方式须先给用户审。
- 已验收Idle六帧是960画布、视频片基有偏差；本轮不改这六张，也不混用作另一张上传图。静帧基准到新视频输出的色彩/尺度衔接仍要单独比较，不宣称来源同一图就一定逐像素相同。
- 原图上下约12%留白已被接受，动作要求至少10%安全边；不为了旧15%指标擅自缩图。举弓/取箭时采用紧凑关节弧线，弓梢、手与羽毛不能出框。
- 文件在Library，不随Git迁移；其他机器发起前传递同一PNG并核对哈希，不能换成截图或对照图。

### 4.2 参数（待用户明确发起）

| 项 | 提案 |
|---|---|
| 模型 | `seedance-2-fast` |
| 时长 | **10秒**，本次完整动作源片建议值，需发起前确认 |
| 分辨率 / 比例 | `720p` / `1:1` |
| 音频 / 水印 | `false` / `false` |
| seed | `-1` |
| 输入 | 唯一一张普通 `reference_image`，无首尾帧、无参考视频 |
| 背景 | 不透明RGB `(0,159,62)`绿幕，不承诺Alpha视频 |
| 次数 | 用户明确发起后只创建一次，失败/超时不自行重建 |

建议输出，准备时不存在：

```text
C:\Users\steam\Videos\doubaoVideo\enemy1051_attack_v1_10s.mp4
```

建议请求/下载/检查留档目录，**本轮未创建**：

```text
C:\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\enemy1051_attack_video_v1_10s\
```

不重跑Idle生成，不覆盖已验收Idle的文件、候选帧、请求或报告。

## 5. 完整英文正文（实际提交）

```text
Create one continuous 10-second 2D game-sprite bow attack for the exact masked militia archer in the single supplied identity image. Start in his accepted empty-free-hand idle, perform ONE complete arrow shot toward the player/camera, then recover to the same relaxed bow-carrying idle. This is not a sideways archery scene, repeated firing loop or cinematic. The image defines identity and style, not a fixed drawn-bow target pose. Invent natural connecting motion without swapping between still poses.

Keep the same short stocky body, dark brown-gray olive head wrap and face veil, narrow orange eye slit, gray-brown light clothing, bare arms and lower legs, leather details, bow, quiver and hard pixel-cluster rendering. Keep the face covered throughout; do not expose a nose, mouth, chin or cheeks, restore metal armor, turn the cloth bright yellow or redesign the archer.

HAND ROLES AND DIRECTION:
The hand on SCREEN-RIGHT always holds the same wooden bow at the same grip. The free hand on SCREEN-LEFT fetches and draws the arrow. Preserve these roles, never mirror or swap them. The target is in front of the character, toward the player/camera, not off the right side of the screen. Bring the bow from its idle screen-right carrying position to a readable position in front of the chest, slightly right of center, and push it forward toward the player. Keep the front-view camera locked. Small natural shoulder and torso articulation is allowed, but do not turn into a new side-profile or back-view character.

FETCH AND NOCK:
After a brief idle, the screen-left hand reaches to the quiver behind the screen-left shoulder and takes ONE ordinary wooden arrow with feather fletching and a small metal head. Show the hand carrying that arrow from the quiver to the bow; do not conjure it on the string. Seat the nock at the center of the bowstring, with the shaft resting coherently near the bow grip. Aim toward the viewer. The arrow may be strongly foreshortened in this front view, but the shaft, nock and string must still form one understandable connection. Use a compact normal arrowhead, not a huge diamond, spear or blade filling the chest or canvas.

DRAW AND AIM:
Build tension continuously. The screen-right bow hand pushes forward and stabilizes while the screen-left drawing hand pulls the nock and string back beside the covered cheek, with the drawing elbow opening toward screen-left. The string changes from its undrawn straight line into a taut V meeting at the drawing hand. The bow limbs bend elastically as the same bow, without changing length, material, grip or identity. Show intermediate partial-draw positions; do not move the hands while leaving the string unchanged. Allow a short readable full-draw aim, not a multi-second freeze or several repeated tugs. Feet, pelvis, belt anchor and drawing scale stay stable; the tension is in the upper-body joints, not a whole-image stretch or a step.

RELEASE ONCE:
The drawing fingers let go of the string in ONE quick release. The drawing hand opens briefly and follows the released tension a short distance backward; it does not throw the arrow forward. The string springs back and the bow gives a small natural elastic recovery. The last full-draw pose and the first released pose must be visibly distinct. The arrow leaves the bow toward the player; it is no longer nocked after release. A very brief compact departure close to the bow is enough. Do not animate a long projectile flight, a giant arrow rushing into the lens, an arrow rain, a glowing trajectory or a second independent flying arrow hanging beside the character. The game engine supplies the actual projectile later; this source focuses on the archer's release and follow-through. No beam, shockwave, spark, flash, dust or motion-blur effect.

FOLLOW-THROUGH AND RETURN:
Keep the bow hand stable through the instant of release, let the arms absorb the small reaction, then relax. Lower the empty screen-left hand naturally from beside the face to its original idle position, and return the same bow to its original vertical screen-right carrying position. Recover through actual joint motion, not reversing the shot or snapping to the input picture. No second arrow is fetched and no second shot begins. Preserve the quiver; one arrow being taken may cause a natural local fletching change, but do not multiply arrows, empty the quiver, change its side or magically refill it to match a tail image. Finish in the same stance and equipment relationship, with only a quiet idle breath.

LOOSE PACING:
0-0.5s: brief idle.
0.5-2s: fetch one arrow and bring the bow forward.
2-3s: nock the arrow coherently.
3-5s: continuous draw, ending in a brief full-draw aim.
Around 5-5.5s: one fast finger-and-string release, not a slow push.
5.5-7s: natural small follow-through and settling.
7-9s: lower the hands and bow into idle.
9-10s: quiet idle, without starting another attack.
These are broad guides. Natural overlap and a readable connected shot matter more than exact timestamps; do not spend most of the video standing still and skip the important action at the end.

SPRITE PRESENTATION:
Keep one locked front-view camera, square canvas, original drawing scale, body identity and ground contacts. No zoom, pan, reframing, root travel, foot slide, body mirroring or full side turn. Both complete bow tips, string, hands, shoes, quiver, feather ends and short cloth tails remain inside the frame with at least ten percent safety space on every side. Use compact joint paths if a reach nears an edge; do not crop or shrink the whole character midway. Keep the flat chroma-green production background RGB(0,159,62), stable cloth/skin colors and unchanged lighting. No floor, shadow, scenery, extra person, new equipment, text or UI. Preserve connected fingers, wrists and string attachment points; no melting, double limbs, crossfades, pose teleportation or rigid-card rotation.
```

## 6. 完整中文对照（供审阅，不提交）

```text
为唯一身份图中这个确切的蒙面民兵弓手制作一支连续10秒的2D游戏精灵弓箭攻击。从已验收的空手持弓Idle开始，完成一次朝玩家/摄像机方向的完整射箭，再收回同一个放松垂持弓的Idle。这不是横向射箭场景、连发循环或电影。图片定义身份与画风，不是必须凑到的满弓目标图。自然生成连接动作，不用几张静态姿势互相替换。

保持同一个短壮身体、暗褐灰橄榄头巾与面巾、暖橙窄眼缝、灰褐轻装、露出的手臂与小腿、皮革细节、弓、箭壶和硬边像素块画风。面部全程遮住，不露鼻、嘴、下巴或脸颊，不恢复金属甲，不把布料变亮黄，不重新设计角色。

双手与方向：
画面右侧手始终在同一个握把位置持同一把木弓。画面左侧空手负责取箭和拉弦，不镜像、不换手。目标在角色正前方、玩家/摄像机方向，不在屏幕右侧。把弓从Idle的画面右侧垂持位举到胸前中央偏右，朝玩家方向推出，保持可读。正面机位不动。允许少量自然肩部/躯干关节调整，但不能转成新的纯侧面或背面角色。

取箭与搭箭：
短暂Idle后，画面左侧手伸到左肩后的箭壶，取出一支普通木箭，带羽毛和小金属箭头。手把这支箭从箭壶带到弓前，不让箭凭空出现在弦上。箭尾搭在弓弦中心，箭杆在握把附近有合理支承关系。箭朝向玩家，正面透视可以明显缩短，但箭杆、箭尾与弓弦仍应连接得清楚。使用普通、紧凑的箭头，不画占满胸口或画布的巨大菱形箭头、矛或刀刃。

拉弓与瞄准：
连续建立张力。画面右侧持弓手向前推稳，画面左侧拉弦手把箭尾与弦拉回遮住的脸颊旁，拉弦肘向画面左侧打开。弓弦从未拉开的直线变成朝拉弦手汇聚的绷紧V形。弓臂随受力弹性弯曲，但仍是同一把弓，不变长短、材料、握点或身份。要有半拉弓的中间姿态，不是双手已经分开而弦仍不动。满弓短暂停一下让人看清，不冻结数秒，也不反复抽拉。双脚、骨盆、腰带锚点和绘制尺度稳定，张力来自上身关节，不靠整张人物拉伸或迈步。

只松弦一次：
拉弦手指一次快速松开。拉弦手短暂张开，沿释放后的拉力方向小幅向后跟随，不把箭用手往前扔。弦迅速回弹，弓自然小幅恢复。最后满弓姿态与第一张已松弦姿态必须明显不同。箭朝玩家离开弓，释放后不再搭在弦上。只需很短、紧贴弓的离弦读点，不演长距离飞行、冲向镜头的巨大箭、箭雨、发光轨迹或在角色旁停着的第二支独立飞箭。真正的飞行物后续由游戏引擎提供，源片重点是人物松弦与后随。不加光束、冲击波、火花、闪光、尘土或运动模糊特效。

跟随与回位：
释放瞬间持弓手稳住，手臂吸收小反冲后松开。画面左侧空手从脸旁自然降回原Idle位置，弓回到原来的画面右侧垂持位。靠真实关节收势，不倒放射箭、不突然换回输入图。不取第二支箭，不开始第二发。箭壶保持同一身份；取走一支箭时可有自然的局部羽毛变化，但不能增殖箭、整壶清空、换边或为匹配尾图让箭凭空补回。最终回到相同站姿与装备关系，只保留微弱Idle呼吸。

宽松节奏：
0–0.5秒短Idle；0.5–2秒取一支箭并举弓；2–3秒合理搭箭；3–5秒连续拉弓、末端短暂满弓；约5–5.5秒一次快速手指松弦，不缓慢推箭；5.5–7秒小幅后随和收稳；7–9秒降手降弓回Idle；9–10秒安静待机，不开始下一次攻击。时间只是方向，自然重叠和完整可读的射击优先于精确时刻；不能大部分时间站着不动，最后跳过关键动作。

精灵呈现：
固定正面机位、正方形画布、原绘制尺度、角色身份与脚底接触。不推拉、摇移、重构图、移动根部、滑脚、镜像或完全侧转。两个弓梢、弦、双手、鞋、箭壶、羽毛和短巾尾始终完整入框，四边至少10%安全空白。取箭路径靠边时关节紧凑，不裁切、不在中途缩小人物。保持平坦绿幕RGB(0,159,62)、稳定布料/皮肤颜色与光照。没有地板、投影、场景、其他人物、新装备、文字或UI。手指、手腕和弦端连接正常，不融化、不多肢、不叠化、不瞬移换姿势、不像卡片一样整体转动。
```

## 7. 英文负面词（并入同一个text）

```text
Avoid sideways shooting, a full side-profile turn, swapping bow and drawing hands, mirrored equipment, an arrow appearing from nowhere, broken nock-to-string connection, disconnected or duplicate strings, a rigid unchanged string during draw, rubbery bow length changes, a giant arrowhead, throwing the arrow by hand, firing before full draw, multiple shots, a second arrow in recovery, arrow rain, long projectile flight, arrow zooming into the lens, beam or release flash, stepping, foot sliding, root movement, whole-body scaling, camera movement, cropped bow tips or hands, exposed facial features, new armor, bright yellow costume, melting anatomy, extra limbs, crossfade, pose snapping, rewind, long frozen aiming, scenery, floor, shadow, text and UI.
```

中文负面词对照：不要横向射击、完全侧身、持弓拉弦手交换、装备镜像、凭空出现箭、箭尾与弦脱开、断弦或多弦、拉弓时弦仍僵直不动、弓长像橡皮一样变化、巨大箭头、用手抛箭、未满弓就提前飞箭、多次射击、收势时第二支箭、箭雨、长距离飞箭、箭冲镜头放大、光束或放箭闪光、迈步、滑脚、移动根部、全身缩放、镜头运动、裁切弓梢或手、露脸、新增护甲、亮黄服装、肢体融化、多肢、叠化、换姿势硬切、倒放、长时间冻结瞄准、场景、地板、投影、文字或UI。

**不要带入Idle词里的 `no drawing the bow`、`no nocking`、`no shooting` 或“弓形永远不变”。** 本支恰好需要搭箭、拉弦、弹性弯弓和放箭；保持的是弓的身份与尺寸，不是所有受力形状冻结。

## 8. 发起侧请求与权限

1. 向用户呈现完整中英文词、负面词、唯一上传图职责、**建议10秒**及全部参数/输出路径；本轮只准备文档，须用户明确发起后才调用接口。
2. 复核§4文件与哈希，只上传该身份副本一次；使用单张普通 `reference_image`，不混首尾帧模式、不上传旧105图或峰值图。
3. 实际text为§5英文正文＋两个换行＋§7英文负面词；不提交中文、整份文档或Idle生成词。新版 `input.content` 无单独旧版 `negative_prompt` 字段。
4. 任务创建一次；失败、超时或结果不明先保留记录并报告，不盲目重建。已拿task后，查询或下载故障不创建第二个任务。
5. 按 `skill/reachapi-seedance-video-generation-v2.md` 有限查询、断点下载、完整解码并回读文件；保存真实词、参数、参考哈希、task、usage/cost、媒体信息与成片SHA-256，不记录Key。
6. 下载后交用户看片，不自动选正式生产帧、不抠图、补边、建Clip或改Unity。视频技术成功不等于动作验收，不自动重发收费任务。
7. 现有静帧阶段B和Idle六帧去背继续暂缓；射箭生成授权也不包含恢复这些任务。

请求示意（占位符，尚未发出请求）：

```json
{
  "model": "seedance-2-fast",
  "input": {
    "content": [
      { "type": "text", "text": "<§5英文正文 + 两个换行 + §7英文负面词>" },
      { "type": "image_url", "role": "reference_image", "image_url": { "url": "<唯一1051身份图上传返回的data.url>" } }
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

## 9. 验收与后续选帧/接入边界

### 画面与动作

- 看见一支箭的实际取出/搭上、半拉弓、满弓、松弦与收势，不只是Idle—满弓—Idle三张图硬切。
- 右侧持弓/左侧拉弦不变，朝玩家射，不成横版右射；机位与身体朝向分别检查，不能靠转镜头解决正面手部遮挡。
- **核心门槛**：拉弦手、V形弦、受力弓臂和箭尾连接一致；箭不是凭空出现、徒手投出或弦在手边脱开。最后满弓与第一张已松弦可明确区分。
- 普通小箭头，发射后没有持续搭弦箭或第二支独立飞箭；不画替代引擎弹道的长距离投射物特效。
- 回到同一轻装遮面Idle，无额外取箭或第二发；无需为了尾图精确还原箭壶像素而让箭再生。
- 整个人物、弓梢、手、羽毛与取箭路径完整入框，脚与尺度稳定，颜色/面巾/身体结构不变。
- 若视频有姿态平台，记录供选帧，不直接以平台结构否决；有用性由用户看动作判定。压缩噪声、融化或两图叠化不能算真实中间姿态。

### 技术与颜色

- 完整解码，记录真实尺寸/fps/帧数/时长，720p请求不等于实际720×720。
- 空背景ROI逐帧给RGB中位数、std、范围及相对上传键(0,159,62)的差值、首尾与最差帧；不沿用Idle视频实测键色当本片实际键。
- 主体配色与静帧/已验收Idle单独比较，用可信材质局部配准，不拿未经配准的全图均值推断变色。颜色异常列为用户待决项，不自动调色/重生。
- 请求成功、下载/解码成功、视频用户认可、帧序认可、抠图认可、接入与游戏验收分别记录。

### 只有后续明确授权才选帧/接入

- 按真实不同姿态选“取箭/搭箭/半拉弓/满弓/**最后蓄力**/**第一松弦**/跟随/回位”，不固定必须几帧；纯开场/结尾Idle不占生产攻击帧数。
- 释放前后最重要，不要只挑最大满弓姿态而漏掉松弦。候选先交用户看，不自动抠图。
- 后续新Attack Clip的实际时长、松弦键与箭Prefab发射时机必须一致；可利用现有命中帧声明机制按显示精灵击发，但是否配置由接入侧另获授权后决定。
- 旧105的exit0.75、无命中帧计时和三张帧间隔不作新Clip默认值。大幅飞行物仍用现有独立箭Prefab，源片不等于自动实现箭从弓口发出；当前从Transform生成的起点偏差另行评估，不在本轮顺手改代码。
- 本次只读旧105，没有修改Clip、Controller、Prefab、脚本、Sprite或场景，也没有运行攻击自检。

## 10. 历史v1发起说明（禁止重跑，当前v2见§13）

> 请读取 `Locus/knowledge/plan/enemy1051-attack-10s-video-preparation.md`。1051的6秒Idle源片和六帧已验收，去背仍暂缓。本次准备一次完整正面射箭：Idle→取一箭→搭箭→推弓拉弦至满弓→快速松弦→跟随→回Idle，参考旧105的手/弓方向但不上传旧图。唯一上传图是文档§4的1051归一绿幕身份副本，普通reference_image，不混首尾帧或峰值图。建议seedance-2-fast/10s/720p/1:1/无音频无水印/seed=-1；须先确认本次10秒和完整双语方案，用户明确发起后只创建一次。text为§5英文正文＋§7英文负面词。下载解码后交用户看，不自动抠图、接入Unity或恢复Idle去背，不把旧105静态时序风险当作本轮已修复。

## 11. v1执行结果（已提交一次、下载并全帧解码；验收不通过）

- **授权**：用户 2026-10-19“按照生成方案发起10秒射箭动作的生成”（10 秒与完整动作随之确认）。
- **输入**：唯一普通 `reference_image` = `01_idle_v3A_green_009F3E_1024.png`（445,447 bytes、SHA `64a0f777…a4fc`、四角精确 `(0,159,62)`），只上传一次；未用首尾帧模式，未上传旧 105 图。
- **实际词指纹更正**：本地 `Library/Locus/tmp/enemy1051_attack_video_v1_10s/runtime.json` 与本文件§5/§7回算一致：正文 **5,918字符**＋负面 **757字符**，合并UTF-8 SHA-256 **`e23a6c15d917be49072977f0e87e09bab2fc586cfa202b73b9902e1af5b8a9c1`**。此前记成5,084/578和`b0ba9f80…d11b`不符真实执行记录，此处纠正留痕；未更改已提交正文或收费请求。
- **Task**：`task_f344527f846d42488ec7b7e7e1a76b7b`；上传/创建均 HTTP 200，`create_attempts=1`，无重试；usage 216,900，`cost.spend=1.21464`。
- **成片**：`C:/Users/steam/Videos/doubaoVideo/enemy1051_attack_v1_10s.mp4`，4,360,193 bytes、SHA-256 `1e01ce16e71c233c59d68aac5a4df6ee68fb51c053e2cf5d17ed56e4bbfa0766`；960×960 / 24fps / 241 帧 / 容器 10.041667 秒 / 末帧 PTS 10.0 / H.264 / 无音轨，全帧解码通过。

### 11.1 客观实测

- **背景漂移**：四角均值首帧 `(0,147,62)` 一族、末帧 `(0,146,63)` 一族；相对上传键 `(0,159,62)` 最大通道偏差 **14.1**（平均 13.1，最差 f104），四角一致、无棋盘格。输出绿比上传暗约 13，是否接受由用户定，不自动校色。
- **几何越界风险（需要重点看）**：主体**顶边最低到 y=11**、底边稳定 841–842；宽度 494→**806**、高度 657–831、面积 157,054–193,780（±23.4%）。按 960 画布换算，极限帧的上边留白只有 **1.1%**、左右约 8%，**低于方案的 ≥10% 安全边**。举弓/取箭时弓梢或手可能贴边，请重点确认有没有裁切。
- **运动**：相邻帧改动中位数 1,414 px、最大 133,872 px；240 个相邻对里有 **61 对 <200 px**（近似静止平台）。>60k px 的剧烈变化集中在 f055–f057 与 f201–f203 两处（疑似松弦与收势转折），供选帧时参考。
- **材料**：`Library/Locus/tmp/enemy1051_attack_video_v1_10s/` 的 `contact_sheet.png`（帧 0/60/120/180/240）、`frames_all/`（241 张原帧）、`objective_metrics.json`、`runtime.json`、`download_manifest.json`、`submit_1051_attack_video.py`。

### 11.2 未做

未选帧、未抠图、未对齐补边、未建 Clip、未改旧 105/Prefab、未进 Unity、未进 Play；静帧阶段B 与 Idle 六帧去背继续暂缓。

## 12. v1不通过原因与本侧方案纠正

- **用户验收**：“箭矢拿反了”“射击姿态与105完全不一致”，本片不通过；不靠抽帧修补，不再原词重发。
- **实际帧证据**：`frames_all/f050.png`已出现横弓和手位交换；f060/f100/f140为横弓跨胸、画面左侧手持弓，羽毛端与拉弦处关系不成立；f180/f200继续是横弓结构，与旧105竖弓正面Attack不同。
- **不是没看组图**：准备v1时看过旧105 Attack1/2/3及Clip，但自行不上传，只转写为文字，未将用户指定的具体动作输入给生成器；把任务扩成完整取箭/搭箭流程也偏离关键拉弓松弦重点。旧105组图对该关键段已足够有参考价值。
- **不能误归因为完全没写箭尾接弦**：实际v1正文已写`Seat the nock at the center of the bowstring`和右持弓/左拉弦；结果没有遵守。新方案不是简单追加同一句要求，而是实际上传旧动作图、明确竖弓和箭前后端结构。
- **独立问题**：顶部留白最少约1.1%仍须约束；背景暗约13G另报。二者不解释反箭或换手。旧105控制器时序风险不是外部视频失败的原因，本轮未修代码。
- **当前授权**：用户要求准备新方案，不授权自动生成。旧图身份泄漏风险须在四参考审阅时说明，不再次自行删去动作图后声称“已参考”。

## 13. 当前v2方案入口（已准备，尚未提交）

- 完整双语新方案：`Locus/knowledge/plan/enemy1051-attack-v2-pose-guided-video-preparation.md`。
- 四输入：1051自己的身份绿幕图＋旧105 Attack1/2/3三个绿幕姿态副本，全部普通reference_image；源Assets不改，第三张仅在副本清理外侧浅色特效。
- 10秒只做已搭箭攻击关系→进一步拉弓/满弓→一次松弦→收势回Idle，不再先演取箭，不做新峰值图。
- 关键几何：竖弓、右侧手持弓左侧手拉弦；羽毛/箭尾靠人物与弦、箭尖朝玩家。四张图与完整中英文词需用户审阅，明确发起后只创建一次。
- v2建议输出 `C:/Users/steam/Videos/doubaoVideo/enemy1051_attack_v2_10s.mp4`，尚未生成；不能把本次参考图准备或交接写完当成视频已创建。
