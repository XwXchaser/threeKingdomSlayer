---
id: kd_11befadd-ae26-4037-9d28-9a1b9f84303c
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: 1021胆怯死亡视频待发起方案：仅一张现有无盾Idle，seedance-2-fast、10秒、720p、1:1；完整中英文词描述致命冲击打散防御、全身展开后松脱，保持无支撑死亡倒姿，不落地起身。只完成交接，尚未生成。
---

# Enemy 1021 胆怯死亡：单图参考 / 10 秒视频生成交接

## 1. 当前状态与本次任务

**状态：已按用户指令提交一次并下载解码（2026-10-18）。客观实测：idle 保持到约 1.9 秒才起力，f157–f240（6.54–10.00 秒）是完全冻结的死亡保持（607×526，底边 787）；背景与项目基色 (0,159,62) 最大偏差 4.8，主体颜色漂移仅 5.4。待用户看片。详见 §11。**

用户先要求思考胆怯 `Dead` 的动作内容，随后明确“准备视频生成方案交接”。本次授权只覆盖方案与交接文档，不包含调用收费生成、制备新参考图、选帧、抠图、对齐、补边或 Unity 接入。

本方案延续刚获用户验收的胆怯击飞源片的制作组合：**只给一张无盾胆怯 Idle + 10 秒源片 + 用文字说明受力情景 + 放开中间姿态**。10 秒方案已产出可用的完整动作与 16 帧透明素材，但时长、参考图数量和动作约束同时变化，不能把本次成功写成“只要延长时长就保证成功”。本轮继续采用这个已有效的组合，不退回“先造峰值图，再逼视频凑到图2”的路线。

### 本支动作

**胆怯 Idle → 一次致命重击冲散双掌防御 → 全身反冲、短暂离地并向后展开 → 头颈与四肢失去主动控制 → 无支撑的死亡倒姿保持。**

- 先有猛烈冲击，再有死重；不是慢慢泄气、被催眠倒下，也不是已经僵硬的尸体突然换图。
- 胆怯由初始的双掌护身及防御被打散来体现；不另演求饶、捂胸、后退、跪地或挣扎。
- **本次是死亡，不是“仍活着”的击飞链**：不落地、不撑地、不起身、不回 Idle，不再进行第二次受击。
- 最终是便于引擎继续旋转、下坠的无盾死亡姿态，不是地板上的留尸画面。
- 文本只给动作因果与主要读点，不指定某只手的精确角度、不锁定尾姿图、不约束最终包围盒为某个比例。

## 2. 唯一上传图（已经存在，无须重新制作）

```text
C:\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\enemy1021_coward_idle_peak_v1\01_coward_idle_peak_source_green_00B140_1024.png
```

| 项 | 本次回读核验结果 |
|---|---|
| 身份 | 1021 无盾、空手、双掌前挡的胆怯 Idle |
| 格式 / 尺寸 | RGB PNG，1024×1024 |
| 字节数 | 172,046 |
| SHA-256 | `3fd64c935cd7b5adf562d2729899430e2156120edf0575e969e2faf95146a6c0` |
| 空背景 ROI | 四角各 64×64：左上 `(0,0,64,64)`，右上 `(960,0,1024,64)`，左下 `(0,960,64,1024)`，右下 `(960,960,1024,1024)`；坐标右下为开区间 |
| ROI RGB | 四块 median 均 `(0,177,64)`，各通道 std=0，min=max=对应色值 |
| 提交职责 | 只上传一次，单张普通 `reference_image` |
| 定义内容 | 同一角色身份、材质、部件尺度、像素画风、起始站姿、绿幕 |
| 不定义内容 | 空中峰值、死亡终态、精确关节路径；这些由视频依据文字自行生成 |

本轮已实际查看图像并复核上述文件数据；未改图片。此文件位于 `Library`，不随 Git 自动迁移；发起机器缺文件时应索取同一原件，不自行换图、重建或重新收费生成。

**禁止追加：** 1011 的任何帧、持盾 Idle、持盾死亡、CowardHit 峰值、胆怯击飞/落地帧、之前失败的关键图、接触表、参考视频。不要把唯一 Idle 同时设为尾帧，否则会把死亡引导回站姿。

## 3. 参数、路径与提交边界

| 项 | 取值 |
|---|---|
| 模型 | `seedance-2-fast` |
| 时长 | **10 秒** |
| 分辨率 / 比例 | `720p` / `1:1` |
| 音频 / 水印 | `false` / `false` |
| seed | `-1` |
| 图像模式 | 唯一一张普通 `reference_image`；不用 `first_frame` / `last_frame` |
| 制作底 | 不透明纯绿幕 RGB `(0,177,64)` / `#00B140`；不承诺 Alpha 视频 |
| 建议输出 | `C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_dead_v1_10s.mp4` |
| 建议项目留档目录 | `Library/Locus/tmp/enemy1021_coward_dead_video_v1_10s/` |
| 当前文件状态 | 上述输出和留档目录在准备时均不存在；本次未创建视频或留档目录 |
| 任务次数 | 用户另行明确发起后只创建一次；失败、超时或创建结果不明均不自动重建 |

10 秒是素材源片时长，不是游戏内死亡时长。沿用现有接口记录的规格：`seedance-2-fast` 支持 4–15 秒和 `720p`，本轮没有远程探测，不自行改模型、降规格或改成不支持的 `1080p`。

### 宽松节奏（不是逐帧强制时刻）

| 约略时间 | 动作职责 |
|---|---|
| 0.0–0.6s | 短暂胆怯 Idle；没有主动蓄力、下蹲起跳或慢慢倒下的前摇 |
| 0.6–2.0s | 突然的一次致命冲量打在胸口/上腹；双掌防御被冲开，胸肩反折，头稍晚被甩开，支撑被打断 |
| 2.0–4.5s | 沿同一次惯性向后展开，胸、髋、双臂和双腿形成真正的中间姿态；不二次发力、不主动蹬腿 |
| 4.5–7.0s | 冲击绷紧逐渐变为死重；头颈、肘腕、膝踝松脱，仍保留清楚的展开剪影，不缩回护身姿态 |
| 7.0–10.0s | 余势收完，保持稳定、无支撑的死亡终态；无呼吸、无回头、无起身或重新护脸 |

起力本身必须短而硬，上表较长的段用于容纳后续关节变化，不是让“致命一击”缓慢持续数秒。可浮动时间边界，但动作顺序只完成一次；中间动作优先于精确踩点。

## 4. 完整英文正文（与 §6 英文负面词合并提交）

```text
Create one continuous 10-second 2D battle-game sprite animation using ONLY the single supplied image. It defines the exact unshielded, empty-handed armored soldier, his fearful idle, drawing scale and pixel-art identity. Begin in that nervous two-palm protective stance. One heavy invisible killing blow destroys his defense, jolts him off his feet and opens his body backward; then all active resistance is lost and he becomes limp dead weight. End in a stable unsupported airborne death pose, not back in idle. This is one forceful death, not a survivable launch followed by recovery, not a voluntary jump and not a slow faint.

IDENTITY AND RENDERING:
Preserve the same silver-gray enclosed helmet and visor, orange eyes, red helmet plume and scarf, steel armor, warm gold trim, brown gloves and boots, proportions, hard pixel-block shading and dark stepped outlines shown in the image. Both hands are empty. His shield was lost before this clip begins; no shield, sword, weapon or new equipment may appear. The helmet and armor stay attached and intact. Keep the subject's material palette and lighting stable. Do not use a sudden color change, eye-glow effect or disappearing eyes as a substitute for the physical death reaction.

FREEDOM AND CONTINUITY:
Invent natural intermediate poses; there is no supplied impact, airborne or corpse target image to match. Prioritize a convincing external force, connected body mechanics and the change from impact tension to dead weight over a prescribed silhouette or exact timestamp. Choose the arm arcs, small asymmetry, knee offsets and head-tilt direction naturally. Let chest, pelvis, head, limbs and loose cloth react with short overlapping delays, not as a robotic queue. The fixed camera does not freeze the joints or require the head to keep looking forward. Do not wait in idle for most of the video and then swap to a corpse picture. Animate the transition, not a crossfade or a melting transformation.

ONE KILLING IMPACT:
After a brief fearful idle, a heavy unseen strike from straight in front, with a small upward component, hits the chest and upper abdomen. The impulse itself is sudden and sharp. He has no warning, no attack wind-up and no deliberate jump squat. His open protective hands are violently separated by inertia as the shoulders and chest recoil. The waist participates and the chest is driven back relative to the pelvis; do not leave a rigid upright body with only a head flinch. The helmet and head follow a fraction late, thrown backward and slightly to one side. The knees lose their supporting tension and both boots release from the standing support because of the blow, not because he pushes off.

The initial reaction must have force before it becomes limpness. Do not start with a calm slow sag, hands clutching the chest, begging, a theatrical backward walk or kneeling. Fear belongs to his starting defense being broken, not to a second acted gesture after the hit. The attacker, weapon and all impact effects remain absent.

UNCONTROLLED BACKWARD OPENING:
Carry the same impulse through the whole body. The shoulders and elbows are pulled apart along slightly different arcs, the wrists and fingers follow, and the legs separate loosely with unequal knee bends. One leg may lag behind the other; neither performs an intentional kick. The torso reclines clearly away from the original standing relationship, toward a diagonally sprawled or nearly horizontal attitude. The chest, pelvis, head and limbs must change together as one connected body, not as a rigid idle cutout rotating as a single card.

The arms and legs can open strongly enough to make the death readable, but they never straighten into a triumphant symmetrical star or a raised-arm victory pose. Avoid two straight dangling legs under an upright torso, pointed toes suggesting levitation, or boots still bracing against an imaginary ground. Let the plume and loose red cloth whip a little late and then follow the motion. Keep all body parts connected, with consistent armor construction and natural joint overlap.

LOSS OF CONTROL - THE DEATH READ:
After the sharp recoil, show the difference between a living airborne soldier and a corpse. The spine releases from the impact arch instead of actively holding it. Shoulders, elbows, wrists, hips, knees and ankles stop resisting. The head and neck lose support and remain at a slack backward or sideways angle; the visor no longer actively tracks the viewer. Hands relax, fingers loosen, and the protective gesture does not return. The knees do not gather under the pelvis to catch a landing or prepare a get-up. There is no searching for support, reaching for the ground, breathing, renewed struggle or second flinch.

Keep the opened silhouette readable while the joints soften: do not pull all four limbs back to the chest or erase the body into a compact ball. A loosely offset arm and unequal bent legs are preferable to a deliberately posed symmetrical display. Let the small overlapping inertial motions die away into a clear limp, unsupported death attitude. Do not force a specific final arm angle, exact body-axis angle or exact width-to-height ratio; the generated pose must follow from the actual motion and remain recognizably the same unshielded soldier.

END STATE AND GAME-MOTION BOUNDARY:
By the final part, settle into a stable fully visible airborne corpse pose and hold it for extraction. There is no ground contact, landing compression, kneel, prone-on-floor scene, supporting palm, stand-up or return to idle. The game engine will supply the large flight, rotation and downward exit later. Here, express the loss of footing and the changing body pose with only a modest local lift, about 10-20 percent of the initial standing body height, not a high flight through the canvas. Keep the body in the original central safe area, with no horizontal travel, approaching the camera or receding from it. The held tail is a production sprite pose, not a new magical levitation action. The body remains opaque and present; do not fade it out, dissolve it or let it leave the frame. Once the final pose settles, there is no new voluntary movement.

LOOSE TEN-SECOND PACING:
0.0-0.6s: brief fearful idle, without preparatory movement.
0.6-2.0s: one abrupt killing impact, broken palm defense, connected chest recoil and loss of standing support; the impulse is quick, not a slow push lasting this whole interval.
2.0-4.5s: continuous inertial backward opening with readable intermediate head, torso, arm and leg relationships.
4.5-7.0s: the impact tension drains into limp dead weight; joints, head and loose cloth finish their delayed settling.
7.0-10.0s: hold the completed unsupported death pose steadily, without breathing, recovery or a second action.
These are broad pacing guides, not separate shots or mandatory pose deadlines. You may shift boundaries slightly to keep the action connected. Use the longer source duration to develop the actual transition, not to add more hits, several deaths or a long idle delay.

SPRITE SAFETY AND PRESENTATION:
Use one locked front-view camera and the same square canvas throughout. No cuts, zoom, pan, tracking, camera rotation, shake or reframing. Natural torso recline, local joint rotation, head tilt and foreshortening are allowed and necessary; do not turn the character into a new side-view or back-view design, perform a somersault, or spin the whole drawing as a flat card. Keep the drawing scale and helmet and armor-part sizes consistent. Limb opening may widen the silhouette without enlarging the character. Boots may leave the initial ground anchor; do not pin them in place while airborne. Keep the complete helmet, plume, hands, legs, boots and cloth at least about 12 percent away from every canvas edge. If a pose approaches an edge, bend the joints more compactly; never shrink the character, move the camera or crop a limb. Keep the same flat chroma-green production background RGB(0,177,64) in all frames, without gradients, texture, scenery, floor, shadow, props, text or UI. Keep clean hard pixel edges, without motion blur, afterimages, double bodies or dissolved anatomy.
```

## 5. 完整中文对照（供审阅，不与英文一起提交）

```text
仅使用提供的这一张图片，制作一支连续10秒的2D战斗游戏精灵动画。该图定义同一个无盾、空手的盔甲士兵、他的胆怯Idle、绘制尺度和像素画风。以这个紧张的双掌护身站姿开始。一记沉重、不可见的致命打击摧毁防御，将他打得失去支撑、向后展开；随后一切主动抵抗消失，身体成为松脱的死重。最终停在稳定、无支撑的空中死亡姿态，不回Idle。这是一次猛烈死亡，不是仍然活着、随后恢复的击飞，不是主动跳跃，也不是慢慢晕倒。

身份与绘制：
保留图片中同一套银灰封闭头盔与面甲、橙色眼睛、红缨和围巾、钢铁护甲、暖金边、棕色手套与靴子、身体比例、硬边像素块明暗和深色阶梯轮廓。双手空手。视频开始前盾已经丢失，不得出现盾、剑、武器或新装备。头盔和护甲始终完整并留在身体上。主体材质颜色与光照稳定。不通过突然变色、眼光特效或眼睛凭空消失来代替真正的死亡身体反应。

自由度与连续性：
自然生成全部中间姿态；没有受击、空中或尸体目标图需要匹配。外力可信、身体连贯、从冲击绷紧转为死重，优先于规定剪影和精确时刻。手臂弧线、轻微不对称、双膝错位和头部偏向可自然决定。胸腔、骨盆、头、四肢和松动布料有短暂错时并互相重叠，不像机器人一样排队。机位固定不代表关节不动，也不要求头始终朝前。不要大部分时间待机，最后突然替换成尸体图。生成实际过渡，不叠化，不把身体融化成另一种形状。

一次致命冲击：
短暂胆怯Idle后，一记来自正前方、带少量向上分量的沉重无形攻击击中胸口和上腹。冲量短而突然。没有预警、攻击前摇或主动下蹲起跳。双肩与胸腔反冲时，原本张开的护身双掌被惯性猛然分开。腰部参与，胸口相对骨盆被打向后方；不能身体仍僵直站着、只有头轻微受击。头盔与头部稍晚被甩向后方并略偏一侧。双膝失去承重张力，双靴因为冲击而脱离原本支撑，不是主动蹬地。

最初必须有冲击力，然后才失力。不要一开始就平静地慢慢瘫软、捂胸、求饶、戏剧性后退或跪下。胆怯来自开头的防御被打散，不是在受击后再主动演第二个姿势。攻击者、武器和所有命中特效都不入画。

失控向后展开：
让同一次冲量贯穿全身。双肩与双肘沿略不同的弧线被带开，手腕和手指随后跟随，两腿松动分开、膝盖弯曲程度不同。一条腿可以比另一条稍晚，但都不主动踢腿。躯干明显离开原本直立的关系，向后展开成斜横或接近水平的失控倒姿。胸腔、骨盆、头和四肢作为相连的身体共同改变，不是把整张僵直Idle贴图像卡片一样旋转。

手脚可以充分展开，让死亡一眼可读，但不能主动绷成对称胜利大字，也不能摆一手高举的英雄姿势。不要两条直腿挂在直立躯干下面，不要绷脚尖飞升，也不要靴子仍像踩着不存在的地面。红缨和松动红布稍晚甩动，再沿惯性跟随。所有部件相连，护甲结构一致，关节遮挡自然。

失去控制——死亡的核心：
猛烈反冲后，表现出活人浮空与尸体的区别。脊柱从冲击反弓中松开，不再主动维持弓形。肩、肘、腕、髋、膝和踝停止抵抗。头颈失去支撑，保持向后或一侧松垂的角度，面甲不再主动追着玩家看。手掌松弛、手指放松，不恢复护身动作。双膝不收回骨盆下方准备落地或起身。不寻找支撑、不伸手撑地、不呼吸、不重新挣扎，也不再受击一下。

关节松开时仍保留清晰的展开剪影，不要把四肢全部收回胸前，也不要缩成一团。松弛且错位的手臂、弯曲程度不同的双腿，比刻意对称展示更合适。让小幅错时惯性逐渐消退，形成明确、无支撑的松脱死亡姿态。不强制最终手臂角度、身体轴线角度或包围盒宽高比；终态应自然来自本次动作，并始终是同一个无盾士兵。

终态与游戏位移边界：
后段收稳为完整可见的空中尸体姿态，并保持供选帧。没有接地、落地压缩、跪地、趴在地板上的场景、撑地手掌、起身或回Idle。后续的大幅飞行、旋转和向下出画由游戏引擎提供。本支只通过原地附近的小幅离地与姿态变化表达失去支撑，离地约初始站立身高的10%–20%，不飞到画布高处。身体始终留在原本中心安全区域，不横向飞走、不靠近镜头、不远离镜头。末段保持是精灵素材的制作定格，不是新增一个魔法悬浮动作。身体保持不透明、完整存在，不淡出、不溶解、不离开画面。终态收稳后不再有任何主动动作。

宽松10秒节奏：
0.0–0.6秒：短暂胆怯Idle，没有预备动作。
0.6–2.0秒：一次突然的致命冲击，双掌防御被冲散，胸腔连续反冲并失去站立支撑；冲量本身很快，不是整段都在缓慢推他。
2.0–4.5秒：沿惯性连续向后展开，能看见头、躯干、双臂和双腿关系变化的中间姿态。
4.5–7.0秒：冲击张力消退为死重，关节、头和布料完成延迟收稳。
7.0–10.0秒：稳定保持完成的无支撑死亡倒姿，不呼吸、不恢复、不再进行第二个动作。
这些只是宽松节奏，不是分镜或强制姿态截止时刻。可以小幅移动边界以保持动作相连。更长的源片用于发展真实过渡，不是增加受击次数、演多次死亡或长时间待机。

精灵安全与呈现：
全程只有固定正面观察机位和同一正方形画布。无切镜、缩放、平移、跟拍、镜头旋转、震动或重新构图。允许且需要自然的躯干后倒、局部关节转动、头部倾斜和透视缩短；不能换成新的侧面或背面角色设计，不能空翻，也不能把整幅图像平面卡片一样旋转。绘制尺度、头盔和护甲部件尺寸一致。手脚展开可以加宽剪影，但不是放大人物。腾空时双靴可以离开初始地面锚点，不能把脚钉死。头盔、红缨、双手、双腿、双靴和布料始终完整可见，四边保留约12%以上空白。姿态靠近边缘时弯曲关节、使姿势紧凑，不缩小人物、不移动镜头、不裁切肢体。全程保持同一平坦绿幕RGB(0,177,64)，没有渐变、纹理、场景、地板、投影、道具、文字或UI。保留清晰硬边像素轮廓，无运动模糊、残影、双人重影或肢体溶解。
```

## 6. 英文负面词（追加到 §4，作为同一个 text）

```text
NEGATIVE CONSTRAINTS: no sword, no shield, no weapon, no new equipment, no attacker, no blood, no gore, no dismemberment, no detached helmet, no sparks, no smoke, no dust, no impact flash, no speed lines, no floor, no shadow, no scenery, no text, no UI, no checkerboard. No voluntary jump, no preparatory squat, no slow faint, no begging, no chest-clutching performance, no theatrical backward steps, no heroic star pose, no raised-arm victory pose, no upright toe-pointed levitation, no rigid standing body in the air, no active kicking, no second hit, no repeated death. After the loss of footing, no landing or renewed ground contact. No kneeling, no supporting palm, no get-up, no return to idle, no breathing after death, no renewed defensive gesture. No full-body spin, no somersault, no rigid flat-card rotation, no camera movement, no cuts, no zoom, no reframing, no new side-view or back-view character, no mirroring, no scale pulse, no inflation, no shrink-to-fit, no cropped limbs, no extra limbs, no missing hands or boots, no disconnected joints, no head snap, no pose-swap animation, no crossfade, no melting anatomy, no motion blur, no afterimages, no fade-out, no disappearing corpse, no movement out of frame, no long idle delay followed by an instant final pose.
```

### 中文负面词对照

```text
无剑、盾、武器、新装备、攻击者、血迹、血腥、肢解、脱落头盔、火花、烟雾、尘土、命中闪光或速度线；无地板、投影、场景、文字、UI或棋盘格。不要主动跳跃、预备下蹲、慢慢晕倒、求饶、捂胸表演、戏剧性后退、英雄大字、举手胜利姿势、绷脚尖直立飞升、空中僵直站姿、主动踢腿、第二次受击或重复死亡。失去支撑后不要落地或重新接地。不要跪地、手掌撑地、起身、回Idle、死亡后呼吸或重新护身。不要全身旋转、空翻、僵直卡片旋转、镜头运动、切镜、缩放、重构图、新侧面或背面角色、镜像、尺度脉冲、充气、缩小救边界、肢体裁切、多肢体、缺手缺靴、关节断开、头部硬切、换图式动画、叠化、身体融化、运动模糊、残影、淡出、尸体消失、移出画面，或长时间待机后瞬间替换终态。
```

**禁止复用击飞完整链的 `no death`、`no corpse`、落地/起身时间线和“末尾回到同一Idle”。** 本次恰好需要死亡和尸体终态。也不要把“不要空中冻结”照搬过来：本次末段需要静态保持供选帧，只是不允许身体仍直立地飞升。

## 7. 提交结构与发起侧纪律

1. 读取本文件，不重跑已执行的击飞脚本，也不提交主交接历史 §0.18 的词。
2. 用户明确“按本方案发起”之前，不调用上传或收费生成接口。本轮只准备文档，不构成生成批准。
3. 获明确发起指令后，复核唯一 Idle 的路径/哈希/尺寸/背景，复述本次模型、10秒、720p、1:1、音频关闭及输出路径；已有同一规格的明确批准可复用。
4. 只上传该 Idle 一次，使用一个普通 `reference_image`；不另建尾帧图，不附加视频或其他图片。
5. 提交 text **只包含 §4 英文正文 + 两个换行 + §6 英文负面词**。不提交中文对照或整份文档，也不把负面词放进废弃的 `negative_prompt` 字段。
6. 创建任务只执行一次。创建超时/结果不明先检查同一请求的留档，不盲目重复收费；已拿到 task 的查询和下载失败不能重建任务。
7. 按 `Locus/knowledge/skill/reachapi-seedance-video-generation-v2.md` 限时查询、下载、完整解码并回读文件；保存实际提交词、参数、参考图哈希、task、usage/cost、输出哈希，绝不记录 Key。
8. 完成后先交用户看视频。此生成任务不自动包含正式选帧、抠图、对齐、补边、Clip、控制器接入或 Play 自检。
9. 如用户否决，只记录原因并停止；不自行改词、换图、降规格或重发。后续实质改动应重新提交完整双语词与参数供审。

请求结构示意（占位符供发起侧替换；没有发出请求）：

```json
{
  "model": "seedance-2-fast",
  "input": {
    "content": [
      {
        "type": "text",
        "text": "<§4英文正文 + 两个换行 + §6英文负面词>"
      },
      {
        "type": "image_url",
        "role": "reference_image",
        "image_url": { "url": "<唯一Idle上传返回的data.url>" }
      }
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

## 8. 验收重点：先看动作是否有用，不凭平台结构否决

- **情景**：确实从胆怯双掌护身中受到一次致命外力，而不是自己摆大字、主动起跳、慢慢晕倒或跪地求饶。
- **先硬后松**：冲击有胸肩与全身张力，随后看得见头颈/肘腕/膝踝松脱；不能全程保持刚性的受击峰值，也不能从头到尾软绵绵。
- **死亡与击飞可区分**：身体不再主动保护、撑地或收腿准备恢复；后段不活过来、不回Idle。
- **终态**：无支撑的无盾倒姿，足以作为后续引擎旋转下坠的精灵；不锁死角度或包围盒指标，不靠熄灭眼睛判断死亡。
- **身份与安全**：始终是1021，空手无盾，部件尺度/结构一致，完整入框，无新道具、地板或特效；固定镜头，不自动缩放救边界。
- **真实可选姿态**：回看冲击、展开、松脱三个过程，确认有可用且无融化/重影的实际姿态。记录姿势平台与换图位置；平台本身不等于废片，不能重复把已获用户认可的击飞源片判废。
- **技术与美术分开**：下载成功、全帧解码、时长达到10秒不代表动作验收；记录实际像素尺寸/fps/帧数/时长和源文件哈希，由用户决定画面通过与否。
- **颜色**：输入恒色不保证输出恒色；逐帧测量空背景 ROI，分别报告相对上传键 `(0,177,64)` 和既有片基基准 `(0,159,62)` 的差值，并检查已配准主体材质内部的颜色。不能拿未经配准的整图均值直接认定主体变色。偏差明确列为用户待决项，不自动校色或重新生成。
- **如果后续授权选帧**：从真正受力后的有效姿态开始，不把纯Idle计入死亡帧数；在平台内部选干净姿态，避免混合帧；按实际不同姿态决定帧数，不为凑3帧或6帧重复取近似图。帧序、抠图及接入分别验收。

单图/10秒/动作自由是当前已有成功先例的选择，不是连续性、姿态或颜色的保证；发起侧应报告真实结果而非替用户验收。

## 9. 游戏端边界（只供接入侧理解，本轮不执行）

已核对 `Assets/Scripts/Enemy/Enemy.cs`：

- `PlayDeadVisual()` 在 `IsCoward` 或 `ShieldBroken` 时按名播放 `CowardDead`。
- 地面死亡立即启动 `DeathBounceAndFall()`：0.3s 向上弹起 + 0.8s 下落，同时随机 X/Z 旋转；大幅位移和旋转不是 Sprite 源片职责。
- 浮空死亡立即启动 `LaunchDeathEffect()`：从当下位置旋转下落约0.8s，不再弹起。
- 协程不等待10秒源片或死亡Clip完整播放才运动；因此不能把慢跪倒、手掌撑地或数秒挣扎设计成必看内容。
- 既有计划的 `CowardDead` 为约0.6s 非循环、末帧保持；这里只作为后续压缩选帧的接入参考，不在生成侧按秒照搬，也不在本轮改时长或状态机。
- 后续需单独评估“站立死亡”和“浮空中死亡”的起点衔接；本源片以Idle开头，不自动证明两种运行时入口都无闪帧。未经用户明确授权不新增分支Clip或改代码。

本轮没有进行任何运行或画面自检，也没有修改资产。

## 10. 给发起侧的可复制说明

> 请读取 `Locus/knowledge/plan/enemy1021-coward-dead-10s-video-preparation.md` 与主交接 §0.20。此为1021胆怯死亡新方案：唯一参考是现有无盾胆怯Idle；`seedance-2-fast / 10s / 720p / 1:1 / 无音频无水印 / seed=-1`；单张普通 `reference_image`，不制作或上传任何峰值/尾帧/其他角色图。提交词为§4英文正文与§6英文负面词合并：双掌防御被一次致命打击冲散 → 全身反冲展开 → 头颈四肢松脱 → 无支撑死亡终态保持。不落地、不起身、不回Idle。文档仅是准备完成，须用户另行明确发起；获指令后只创建一次任务，不自动重试。完整下载/解码后交用户看视频，不自动选帧、抠图或接入Unity。姿势平台仅用于后续选帧分析，不能代替用户美术验收。建议输出 `C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_dead_v1_10s.mp4`。

## 11. 执行结果（2026-10-18，已提交一次、下载并全帧解码；待用户看片）

- **授权**：用户 2026-10-18 “已更新交接文档，请按照文档内容发起dead动画的生成”。
- **实际提交词**：§4 英文正文（8,157 字符，SHA-256 `1868d8f6…0b2b`）×§6 英文负面词（1,280 字符，`12fcd757…a741`），合并 `9930871c…d564`；中文对照未提交，未使用 `first_frame` / `last_frame`。
- **唯一参考图**：`Library/Locus/tmp/enemy1021_coward_idle_peak_v1/01_coward_idle_peak_source_green_00B140_1024.png`，172,046 bytes、SHA-256 `3fd64c93…a6c0`、1024×1024 RGB、四角 64×64 中位均为 `(0,177,64)`。
- **Task**：`task_64938fa46e21459eace6e2109afc62ce`；上传/创建均 HTTP 200，`create_attempts=1`，无重试；usage completion 216,900，`cost.spend=1.21464`。
- **成片**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_dead_v1_10s.mp4`，4,864,912 bytes、SHA-256 `5c9d7355741ce48a0983ad0314a7ee505c857c73b04b42b0417c0df3579cd28a`；960×960 / 24fps / 241 帧 / 容器 10.041667 秒 / 末帧 PTS 10.0 / H.264 / 无音轨；四段 Range 与全帧解码通过。

### 11.1 客观实测

- **节奏**：0–1.88 秒为空闲 idle（多个 3–4 帧平台，299–300×410，底边 703，与参考 Idle 一致）；**约 f046–f048（1.92–2.00 秒）才真正起力**，比方案 0.6–2.0 秒的窗口晚到末尾。
- **运动连续性**：f001–f156 逐帧帧间改动中位数 **3,284 px**（最大 90,438），不是击飞那支的“静止平台 + 单帧换图”形态；仍有 69 帧 ≥30k px，属于快速展开阶段，不能据此单独判断动作好坏。
- **死亡保持（可直接取帧）**：**f157–f240（6.54–10.00 秒）共 84 帧完全冻结**，包围盒恒为 607×526、底边 787，无呼吸、无回摆。
- **升降方向**：底边从 idle 的 703 单调降到 787（低 84 px），**全程没有向上腾空**；与方案“小幅离地 10–20% 身高”不一致，而是向下摊开。是否算“落地/不落地”需用户看片判定，本轮不自行结论。
- **背景色（双基准）**：四角均值 `(0.0–2.4, 157.6–163.0, 58.5–64.2)`；相对请求键 `(0,177,64)` 最大偏差 **20.8**（主要是绿通道 158–163 vs 177），相对项目基色 `(0,159,62)` 最大偏差仅 **4.8**。抠图时必须自动取键。
- **主体颜色**：逐帧主体均值漂移最大 **5.4**，远小于击飞那支的 22.5，未出现整体变色。

### 11.2 留档与边界

- **材料**：`Library/Locus/tmp/enemy1021_coward_dead_video_v1_10s/` 的 `DEAD_V1_TIMELINE.png`、`objective_metrics.json`（逐帧包围盒/底边/角点均值/主体均值/平台与换图表）、`frames_all/`（241 张原帧）、`contact_sheet.png`、`runtime.json`、`download_manifest.json`、`job.json`。
- **脚本**：`build_job.py`、`generate_video.py`、`measure_video.py`（均已执行；**不要重跑创建**）。
- **未做**：未选帧、未抠图、未对齐、未补边、未建 Clip、未改任何 Unity 资产、未进入 Play。

## 12. 选帧结果（2026-10-18，待用户验收）

用户 2026-10-18 “有效帧似乎不多，请挑选有效帧”。实测确实如此：**f000–f047（0–1.96 秒）是纯 idle**（与 Idle 参考同尺寸、同底边，不可当死亡帧）；**f156–f240（6.50–10.00 秒）是 85 帧完全相同的冻结终态**（只能取 1 帧）；真正可用的不同姿态只在 **f048–f155** 这 108 帧连续动作里。因此按“只取受力后真实不同姿态、不凑固定帧数、不重复取近似图”取 **8 帧**：

| 精灵 | 源帧 | 时间 | 包围盒 | 底边 | 节拍 |
|---|---|---|---|---|---|
| dead1 | f049 | 2.04s | 311×418 | 705 | 冲击第一拍 |
| dead2 | f057 | 2.38s | 388×454 | 711 | 胸肩反冲、双臂被冲开 |
| dead3 | f072 | 3.00s | 435×492 | 715 | 惯性展开 |
| dead4 | f089 | 3.71s | 474×514 | 713 | 张开到位 |
| dead5 | f104 | 4.33s | 472×507 | 708 | 张力转松 |
| dead6 | f133 | 5.54s | 568×524 | 739 | 下肢摊开下落 |
| dead7 | f146 | 6.08s | 624×530 | 775 | 展开最宽处 |
| dead8 | f160 | 6.67s | 607×526 | 787 | 死亡终态（冻结段代表帧） |

- **去重校验**：7 组相邻帧差异 80,365–160,246 px，无重复帧；冻结段只取 f160 一张，不把 85 帧重复姿态计入。
- **交付目录**：`Library/Locus/tmp/enemy1021_coward_dead_video_v1_10s/selected_dead_v1/`（`dead1..8.png`，960×960 RGB，未抠图、未缩放、未对齐、未补边）；清单 `selected_dead_v1.json`（逐帧 SHA-256、包围盒、底边、帧间差异）。
- **评审材料**：同目录的 `DEAD_SELECTED_REVIEW_v1.png`、`DEAD_SELECTED_PREVIEW_v1.gif`（140ms/帧，仅评审）。
- **可选**：若觉得最后一拍跨度大，f146→f160 之间还可补 f152（6.33 秒，616×528、底边 783，与 dead7 差 121,749 px、与 dead8 差 103,534 px），不自动加。
- **未做**：未抠图、未补边、未建 Clip、未改 Unity。

## 13. 抠图与尺寸对齐（2026-10-18，待用户验收）

用户：舍弃头两帧（原 dead1 f049、dead2 f057）→ 剩 6 帧重编 dead1..6 = f072 / f089 / f104 / f133 / f146 / f160。

- **抠图**：SVL 自动取键 `(2,160,61)`、容差 80 / softness 16 / halo 1 / despill / 半透明转不透明（cutoff 128）；六帧均半透明 0、透明区非零 RGB 0，可见区 `g−max(r,b)>8` 为 610–899 px/帧。原始结果在 `keyed_t80_v1/`。
- **尺寸对齐（作废的第一次）**：用“水平 run p30（肢体厚度）”作代理，在摊开姿态上失真（p30 48→36），把最后一帧误放大 1.333 倍（805×697、底边 888），已删除不得使用。
- **最终手法**：以剪影面积作代理，`scale = sqrt(中位面积 108424 / 本帧面积)`，属有界对称修正，不是分割级测量；预乘 RGBa LANCZOS、以 alpha 质心为锚、重采样后 cutoff 128 重新硬化。

| 精灵 | 源帧 | 缩放 | 对齐前→后 | 底边前→后 |
|---|---|---|---|---|
| dead1 | f072 | 1.140 | 432×487 → 492×554 | 713→745 |
| dead2 | f089 | 1.084 | 471×511 → 511×554 | 711→732 |
| dead3 | f104 | 1.046 | 470×504 → 491×527 | 706→719 |
| dead4 | f133 | 0.959 | 565×520 → 542×499 | 738→726 |
| dead5 | f146 | 0.929 | 619×526 → 575×488 | 772→750 |
| dead6 | f160 | 0.917 | 604×523 → 554×479 | 785→759 |

- 对齐后面积统一 107,566–107,812，最小留边 ≥169，半透明 0。
- **备用口径**：若前几帧被放得过大（面积代理把“四肢摊开”算成尺度），可改成只缩不放，把 1.140/1.084/1.046 置 1.0 重跑。
- **交付**：`keyed_t80_aligned_v1/`（dead1..6，960×960 RGBA）；原始抠图 `keyed_t80_v1/`；报告 `matte_align_report_v1.json`；评审 `DEAD_MATTED_ALIGNED_6_WHITE/DARK/CHECKER_v1.png` 与 `DEAD_MATTED_ALIGNED_6_PREVIEW_v1.gif`。
- **未做**：未补边 1108、未建 Clip、未改 Unity。
