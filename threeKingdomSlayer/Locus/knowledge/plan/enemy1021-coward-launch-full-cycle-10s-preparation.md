---
id: kd_6c2953a4-8649-482a-9840-3cb984c37fa3
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# Enemy 1021 胆怯击飞完整动作：单图参考 / 10 秒视频生成方案

## 1. 本次目标与授权边界

用户本次明确要求：只上传 Idle 一张参考图，不上传任何额外姿态图；用文字描述，让视频自由生成完整的 **Idle → 被挑飞 → 下落与落地 → Getup → 回到同一 Idle**，时长 10 秒，由其他对话或工具发起生成。

- **状态（2026-10-18 更新）：已按用户指令提交一次并下载解码。客观实测为“姿态平台 + 单帧换图”的停格结构（241 帧中 174 帧静止在 3–10 帧的平台上，另有 33 次 30k–107k px 的单帧换图），按 §10.2 不成立。详见 §12。**
- 本次是新的完整动作实验，取代 `enemy1021-art-production-handoff.md` §0.18 对“本次视频只做击飞后保持、6 秒、起身另做”的限制；旧失败记录仍保留，不重跑旧任务。
- 只用现有的 **1021 无盾胆怯 Idle** 定义身份、画风、起始及最终站姿。不得上传 1011、持盾 Idle、失败的击飞图、CowardHit 图、倒姿图、视频或接触表。
- 不做新的空中尾帧图、落地尾帧图、起身尾帧图；不使用强制首尾帧模式。
- 10 秒是素材源片长度，不是游戏内 Clip 时长。本轮不改代码、状态机、Prefab、场景或动画资产，也不承诺整支视频可以原样接入游戏。

## 2. 唯一上传图

Windows 绝对路径：

```text
C:\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\enemy1021_coward_idle_peak_v1\01_coward_idle_peak_source_green_00B140_1024.png
```

| 项 | 已核对值 |
|---|---|
| 身份 | 1021 无盾、空手、双掌前挡的胆怯 Idle；不是持盾待机 |
| 文件 | RGB PNG，1024×1024，172,046 bytes |
| SHA-256 | `3fd64c935cd7b5adf562d2729899430e2156120edf0575e969e2faf95146a6c0` |
| 空背景 | 四角各 64×64，median `(0,177,64)`，各通道 std=0，范围恒定 |
| 接口职责 | 单张普通 `reference_image`，仅一次；不用 `first_frame` / `last_frame` |
| 作用 | 角色身份、装备与配色、绘制尺度、固定视角、初始/最终胆怯站姿、绿幕 |

本方案准备时已回读文件、解码、复核尺寸、字节数、哈希与上述四块背景统计；没有修改图片。`Library` 不随 Git 自动迁移，其他机器发起前必须确认同一文件可用。缺图时停止并索取原件，不自行重建、改图或换成其他 Idle。

## 3. 生成参数

| 项 | 值 |
|---|---|
| 模型 | `seedance-2-fast` |
| 时长 | **10 秒** |
| 分辨率 | `720p`（请求档位，不承诺实际像素尺寸） |
| 比例 | `1:1` |
| 音频 / 水印 | `false` / `false` |
| seed | `-1` |
| 参考素材 | 上述唯一一张普通 `reference_image` |
| 透明度 | 不透明绿幕视频；未请求、也不承诺原生 Alpha |
| 上传制作底 | RGB `(0,177,64)` / `#00B140` |
| 建议新输出 | `C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_launch_fullcycle_v2_10s.mp4` |
| 建议留档目录 | `Library/Locus/tmp/enemy1021_coward_launch_fullcycle_v2_10s/`，目前未创建 |
| 提交次数 | 获明确发起指令后只创建一次；失败、超时或结果不明均不自动重建任务 |

沿用当前已使用的模型与规格，仅把本轮时长改成 10 秒。项目接口说明支持该模型 4–15 秒，因此 10 秒在已记录范围内；本轮没有远程探测。不能自行改成 1080p，该模型在当前接口文档中不支持。

## 4. 自由度与硬要求

### 允许模型自行决定

- 手臂张开弧线、双腿错时、头与红缨的惯性细节。
- 轻微不对称、自然的胸腰后倒和透视缩短；不要求匹配任何预制空中图。
- 落地时在原阵位内的局部接触调整、用哪只手先支撑、哪条腿先收回。
- 起身经过低姿、半跪或蹲姿的自然连接，但不能停在英雄跪姿。
- 各时间段是宽松节奏，不要求逐秒踩点。动作连续与外力因果优先于严格时刻和某张中间姿势。

### 仍然必须保持

- 同一个 1021、空手无盾、活着且胆怯；装备不能变、不能出现剑。
- 固定正面观察机位，固定画布、绘制尺度与材质颜色；不是全程把头脸钉成 Idle 朝向。头和躯干可以随翻倒与起身自然变化。
- 同一阵位内完成完整动作；允许少量必要的腾空升降和接触调整，不能横向飞走、走去别处、靠近/远离镜头。
- 起始与结束回到同一站位/脚底基准。双脚腾空时当然可以离开基准，不沿用“所有帧脚底固定”的站立受击条款。
- 小幅离地足以表示腾空，建议相对初始站姿只升高约自身站立高度的 10%–20%；不是大幅飞到画布顶部。体态展开可以改变总包围框，但不能把头盔、胸甲等部件整体放大。
- 全部轮廓完整入框，四边至少约 12% 空边；不能中途缩放或移动镜头救边界。
- 纯色绿幕，没有地板、地平线、投影、攻击者或特效。接地由姿态和承重关系表达。
- 只有一次击飞、一次落地、一次起身；无死亡、无重播、无倒放、无硬切、无叠化换图、无长时间空中冻结。

## 5. 宽松的 10 秒节奏

| 约略时间 | 情景与动作 |
|---|---|
| 0.0–0.6s | 同一胆怯 Idle，短暂紧张呼吸，无预备下蹲或主动起跳 |
| 0.6–2.4s | 一次来自下方略偏正前方的冲量；起力短而突然，骨盆/下腹先被挑起，胸头与四肢随后失控展开，形成向后摊开的空中姿态 |
| 2.4–4.0s | 自然经过顶点并连续下落，不摆空中英雄 pose、不像站着升空；双腿和双手沿惯性继续变化 |
| 4.0–4.8s | 臀髋、下背与腿先后接触并吸收重量，上身/头/红缨跟随收稳，形成短暂可读的活人摔落姿态；不头先着地 |
| 4.8–8.6s | 手/前臂承重，膝与脚收回身下，重心经过低姿再推起，胸头最后连续回正；不瞬间站起，不把下落倒放成起身 |
| 8.6–10.0s | 双掌回到同一胆怯护身关系，双脚/身体回到起始站位，末约 1 秒保持温和呼吸 |

时间只是方向，不要要求关节分阶段机械排队；多个部位要有短暂错时并互相重叠。长时长用于给完整过渡留空间，不是站着等到最后一秒再一次跳完。

## 6. 提交用英文正文

以下英文正文与 §8 英文负面词合并为一个 text；不要把中文对照一起提交。

```text
Create one continuous 10-second 2D game-sprite animation using ONLY the single supplied image. This image is the exact identity, drawing-scale and idle reference for an unshielded, empty-handed armored soldier. Begin in this fearful idle and end back in the same idle. The complete action is: fearful idle -> one externally caused upward launch -> airborne backward recoil -> descent and landing -> physical get-up -> fearful idle. This is a living frightened soldier knocked off his feet, not a death, a voluntary jump or a heroic stunt.

IDENTITY AND ART:
Keep the same silver-gray enclosed helmet and visor with orange eyes, red helmet plume and scarf, steel armor, gold trim, brown gloves and boots, body proportions, hard pixel-block shading and dark stepped outlines from the image. Both hands are empty throughout. He has already lost his shield before the video begins. No sword, shield or other equipment may appear. Keep the reference material colors and stable lighting. This is the same character changing pose, not a redesigned airborne character.

MOTION FREEDOM:
Invent the intermediate poses naturally; there is no supplied airborne, landing or get-up target image to match. Prioritize convincing force, weight and continuous articulation over an exact intermediate silhouette or an exact timestamp. Choose natural hand paths, knee timing, mild asymmetry and support-hand order. The times below are loose pacing guides, not separate shots or still pictures. Let body-part reactions overlap with small delays rather than move in a robotic queue. Do not fill the duration with idle followed by an instant pose swap.

THE LAUNCH:
After a short nervous idle, an invisible strike from below and slightly in front abruptly catches the lower abdomen and pelvis. The initial impulse is short and sharp. He does not prepare, squat, push off or choose to jump. His planted support breaks; both boots release from the ground as a consequence of the blow. The pelvis is carried upward first, the chest opens and recoils backward, and the helmet and head follow a little late. The neck, waist and knees all participate; do not leave a rigid standing body and only tip the head.

The two open protective hands are pulled away from the chest by inertia. Shoulders and elbows open on slightly different arcs, with wrists and fingers following rather than forming a deliberate symmetrical display. The loose knees bend and the legs separate a little at different depths. Ankles relax, and the boots follow the loss of balance; a sole may naturally become visible. Avoid two long straight legs hanging under an upright torso or a single pointed boot forming a tiptoe levitation pose.

As the impulse continues, the chest and pelvis visibly lose the upright standing relationship. The body reclines into a clearly backward-sprawled airborne attitude, approaching a flat-on-the-back relationship rather than standing in the air. The torso, bent legs and thrown-open arms form one connected reaction. Keep the visor and front armor recognizable, but let the head tilt with the torso instead of pinning an idle-facing head onto a fallen body. The red plume and loose cloth lag behind and then follow through. He looks startled and helpless, never triumphant. Do not lock him into a static star pose or a raised-arm victory pose. Pass through the top of the short flight continuously, without a prolonged hover, somersault or full-body spin.

DESCENT AND LANDING:
The upward momentum diminishes and gravity brings the same body down within its original small formation slot. Continue the actual airborne pose into the descent; do not reset the legs, head or hands. The hips, lower back and lower legs take the landing, then the shoulders and head follow with a small delayed settling motion. Protect the head; do not land head-first. Show one readable weight-absorbing contact and a brief reclined grounded state. Loose cloth and the plume settle last. The contact is on an implied invisible support plane only: do not draw a floor, shadow, dust or impact effect. He is winded and frightened, still alive, and already beginning to recover rather than becoming a motionless corpse.

GET-UP AND RETURN:
Recover from the exact position produced by that landing, not from a new kneeling pose inserted by a cut. A palm or forearm finds support, elbows and shoulders accept weight, and knees and boots gather underneath the pelvis. Shift the weight through a believable low crouch or brief half-kneel, then press through the planted boots to raise the hips and armored chest. You may choose which hand or knee leads, but show the continuous transfer of support. Do not lift a rigid flat body straight into standing, rewind the fall, use a kip-up, or turn the recovery into a combat roll or heroic kneeling display. The head follows the rising torso and gradually returns toward the camera; hands and wrists change orientation continuously, never flipping or teleporting. As balance returns, withdraw the supporting hand and bring both open palms back into the original nervous protective position. Finish in the same idle stance, scale, facing and boot-contact region as the reference, with a small natural settling breath. Reach that stance through motion, not a final snap to a copy of the image.

LOOSE PACING:
0.0-0.6s: short fearful idle.
0.6-2.4s: sudden launch impulse and continuous backward airborne unfolding, not slow preparatory movement.
2.4-4.0s: pass through the apex and descend naturally, with no frozen airborne platform.
4.0-4.8s: one landing, weight absorption and brief grounded settling.
4.8-8.6s: supported recovery through a low pose into standing.
8.6-10.0s: settle into the original fearful idle; keep the final approximately one second calm with subtle breathing.
You may shift these boundaries slightly to preserve natural overlap. Complete all phases once within the ten seconds; do not skip the descent, landing contact or supported get-up.

FIXED SPRITE PRESENTATION:
Use one locked front-view camera and the same square canvas throughout, with no cuts, camera movement, zoom or reframing. A fixed camera does not mean a frozen front-facing head: natural body recline and head tilt are necessary, but no turn into a new side-view or back-view character. Keep the drawing scale, helmet size and armor-part sizes consistent; articulation and foreshortening may change the silhouette, not the character scale. Complete the action in the original central slot. Allow only the modest vertical lift and small local contact adjustments needed for the action, roughly 10-20 percent of the initial standing body height upward; do not send him high across the canvas or sideways across the screen. Start and finish at the same ground-anchor region; the boots must be allowed to leave it while airborne. Keep at least about 12 percent clear margin around the complete silhouette at all times. If a limb approaches an edge, use a more compact joint pose, never shrink the character or move the camera. Keep the flat chroma-green production background RGB(0,177,64) unchanged. Render no floor, horizon, scenery, shadow, attacker, props, particles, motion blur, text or UI. Animate the actual joint transitions; no hard pose replacement, crossfade, dissolved limbs or afterimages.
```

## 7. 完整中文对照（供用户审阅，不提交）

```text
仅使用提供的这一张图片，制作一支连续的10秒2D游戏精灵动画。这张图定义同一个无盾、空手盔甲士兵的身份、绘制尺度与胆怯待机。以这个胆怯Idle开始，也回到同一个Idle结束。完整动作顺序为：胆怯待机 → 一次外力向上挑飞 → 空中向后失控摊开 → 下落与落地 → 真实承重起身 → 胆怯待机。这是一个仍然活着、惊惧的士兵被打得失去支撑，不是死亡、主动起跳或英雄特技。

身份与画风：
保留图片里同一套银灰封闭头盔、橙色眼睛与面甲、红缨和围巾、钢铁护甲、暖金边、棕色手套与靴子、身体比例、硬边像素块明暗和深色阶梯轮廓。双手全程空手，视频开始前他就已经失去了盾牌。不得出现剑、盾或其他新装备。主体材质颜色与光照保持稳定。这是同一个角色改变姿态，不是重新设计一个空中角色。

动作自由度：
自行自然生成所有中间姿态；没有任何空中、落地或起身目标图需要匹配。受力、重量与关节连续性优先于精确中间剪影或精确时刻。手臂路径、膝盖先后、轻微不对称、先用哪只手支撑均可自然决定。下方时间只提供宽松节奏，不是多个镜头，也不是几张静态图依次展示。各部位有短暂错时并互相重叠，不要像机器人一样依次排队。不要前面长时间待机，随后一瞬间换成倒姿。

被挑飞：
短暂紧张待机后，一次不可见的攻击从下方略偏正前方突然挑中下腹与骨盆。最初冲量短而突然。角色不准备、不下蹲蓄力、不蹬地，也不是自己选择跳跃。原有站立支撑被破坏，双靴因冲击而离地。骨盆先被向上带起，胸腔打开并向后反冲，头盔与头部稍晚跟随。脖子、腰和膝盖都要参与，不能身体仍像直立站着、只把头往后仰。

原本护在胸前的双掌被惯性带开。双肩与双肘沿略不同的弧线张开，手腕和手指随后跟随，不是主动摆出对称展示动作。膝盖松开并自然弯曲，两腿在略不同深度上适当分开。脚踝放松，靴子跟随身体失衡，必要时可以自然露出鞋底。不要两条长直腿挂在直立躯干下面，也不要一只绷直向下的靴尖形成踮脚飞升的姿态。

随着冲量延续，胸腔与骨盆明显失去原来的直立站姿关系。身体向后摊成清晰的空中倒姿，接近背部朝下、仰倒展开的关系，而不是站在空气里。躯干、弯曲的腿和被甩开的手臂必须组成同一次连续反应。面甲与正面护甲仍可识别，但头部应随躯干自然后倾，不能给倒下的身体钉一个始终正面待机的头。红缨和松动布料滞后，再跟随甩动。角色应惊惧、无助，不能得意或英勇。不要固定成静态大字pose或一手高举的胜利姿势。连续经过这次短距离腾空的顶点，不长时间悬停、不空翻、不做全身旋转。

下落与落地：
上升动量衰减，重力把同一个身体带回原来的小阵位。直接延续实际空中姿势进入下落，不重置腿、头或手。臀髋、下背和腿先后承受落地，肩膀与头随后有小幅延迟收稳。头部受到保护，不头先着地。画出一次清晰的承重接触，以及短暂的后仰摔落在地状态。布料与红缨最后收稳。支撑面只通过接触与承重来暗示，不画地板、投影、尘土或落地特效。他仍然活着，摔得发懵、惊惧，并开始恢复，不变成静止尸体。

起身与回到待机：
从本次落地真正形成的位置继续恢复，不通过切镜插入一张新的跪姿。一只手掌或前臂寻找支撑，肘和肩接受重量，膝盖与靴子收回骨盆下方。重心自然经过低蹲或短暂半跪，再通过踩稳的靴子把髋与盔甲胸腔推起来。可以自行选择哪只手、哪条腿先动，但必须表现连续的承重转移。不要把平躺的僵直身体直接竖起来，不倒放下落，不鲤鱼打挺，不把恢复做成战术翻滚或英雄跪姿。头部跟随躯干升起，再逐渐回向镜头；手掌和手腕连续转向，不能翻面或瞬移。恢复平衡后收回支撑手，双掌回到原本紧张护身的位置。最终回到参考图相同的胆怯站姿、尺度、朝向和双靴接地区域，并用一次微小呼吸自然收稳。通过动作走到这个姿势，不在最后一帧突然替换成原图。

宽松节奏：
0.0–0.6秒：短暂胆怯待机。
0.6–2.4秒：突然的挑飞冲量，以及连续向后展开的空中动作，不做缓慢预备动作。
2.4–4.0秒：自然经过顶点并下落，不在空中冻结成平台。
4.0–4.8秒：一次落地、吸收重量、短暂在地收稳。
4.8–8.6秒：有支撑地经过低姿恢复站立。
8.6–10.0秒：收回原本胆怯Idle，最后约1秒平稳，只有细微呼吸。
为了动作重叠自然，可以小幅移动阶段边界。十秒内每个阶段只完成一次，不能省掉下落、落地接触或承重起身。

固定精灵呈现：
全程只有固定的正面观察机位与同一正方形画布，无切镜、镜头运动、缩放或重新构图。机位固定不等于把头固定朝前：允许必要的身体后倒与头部倾斜，但不能转成全新的侧面或背面角色。绘制尺度、头盔尺寸和护甲部件尺寸一致；关节展开与透视缩短可以改变剪影，但不改变角色整体尺度。整个动作在原来的中心阵位内完成，只允许表达动作所需的小幅离地升降与局部接触调整，上升约为初始站立身高的10%–20%；不要飞到画布高处或横向飞过屏幕。开始和结束使用同一地面锚定区域，腾空期间双靴必须可以离开这个区域。全部剪影与四边始终保留约12%以上空白。肢体靠近边缘时采用更紧凑的关节姿势，不能缩小人物或移动镜头。全程保持同一平坦绿幕RGB(0,177,64)。不画地板、地平线、场景、投影、攻击者、道具、粒子、运动模糊、文字或UI。实际生成关节过渡，不硬切换姿势、不叠化、不溶解肢体、不出现残影。
```

## 8. 英文负面词（并入同一个 text）

```text
NEGATIVE CONSTRAINTS: no additional reference images, no sword, no shield, no weapon, no new equipment, no attacker, no blood, no sparks, no smoke, no dust, no impact flash, no shockwave, no speed lines, no floor, no ground texture, no horizon, no cast shadow, no scenery, no text, no UI, no checkerboard. No voluntary jump, no preparatory jump squat, no upright toe-pointed levitation, no rigid standing body in the air, no triumphant pose, no prolonged frozen airborne pose, no somersault, no full-body spin, no second hit, no second launch, no repeated landing, no extra bounce, no death, no permanent corpse, no kip-up, no combat roll, no heroic kneeling display, no reversed fall used as get-up. No camera movement, no zoom, no pan, no tracking, no cuts, no reframing, no character redesign, no new side-view or back-view presentation, no mirroring, no scale pulse, no inflation, no shrink-to-fit, no cropped limbs, no missing hands or boots, no extra limbs, no disconnected joints, no palm flips, no head snap, no teleport to standing, no pose-swap animation, no crossfade, no melting or dissolved armor, no motion blur, no afterimages, no long idle delay followed by an instant action.
```

中文负面词对照：

```text
不追加参考图；无剑、盾、武器或新装备；无攻击者、血迹、火花、烟雾、尘土、命中闪光、冲击波、速度线；无地板、地面纹理、地平线、投影、场景、文字、UI或棋盘格。不要主动跳跃、起跳前下蹲、绷脚尖直立飞升、空中僵直站姿、胜利姿态、长时间冻结的空中姿势、空翻、全身旋转、第二次受击、第二次击飞、重复落地或额外弹跳。不要死亡、永久尸体、鲤鱼打挺、战术翻滚、英雄跪姿或倒放下落充当起身。不要镜头移动、缩放、平移、跟拍、切镜、重构图、重设计角色、新侧面或背面呈现、镜像、尺度脉冲、充气、缩小救边界、肢体裁切、缺手缺靴、多肢体、关节断开、手掌翻面、头部硬切、瞬移站起、换图式动画、叠化、护甲融化或溶解、运动模糊、残影、长时间待机后瞬间完成动作。
```

注意：不要复用旧版的 `no falling`、`no landing`、`no return to standing`、`no tucked knees`、`no raised arm pose` 或“所有帧双脚固定”。本轮必须下落、落地、收腿起身，也允许自然的手臂甩动。

## 9. 发起侧提交方式

- 只把唯一一张 Idle 上传一次。
- 一个 text 内容为 §6 英文正文 + 空行 + §8 英文负面词。
- 只用普通参考图，不附加 `first_frame` / `last_frame`、参考视频、其他图像。
- 当前 ReachAPI 使用 `input.content`，没有单独的旧版 `negative_prompt` 字段；负面词必须合并到 text。
- 不承诺固定帧率、原生透明或逐像素首尾匹配，不添加不存在的 API 参数。

请求结构示意（占位符由发起侧替换，不是已提交请求）：

```json
{
  "model": "seedance-2-fast",
  "input": {
    "content": [
      {
        "type": "text",
        "text": "<§6英文正文 + 两个换行 + §8英文负面词>"
      },
      {
        "type": "image_url",
        "role": "reference_image",
        "image_url": {
          "url": "<唯一Idle上传返回的data.url>"
        }
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

具体上传、限时查询和断点下载依照 `Locus/knowledge/skill/reachapi-seedance-video-generation-v2.md`。创建只执行一次；已拿到 task 的查询或下载故障不能重建任务。保存实际提交正文、参数、唯一参考图哈希、task id、真实 usage/cost 和输出文件哈希，不记录 Key。

## 10. 发起后的验收重点与限制

1. 必须有完整的 Idle → 被挑飞 → 连续下落 → 一次落地 → 承重起身 → 原Idle，不能只生成其中几段。
2. 逐帧能找到真实的翻倒中间姿态、接触压缩、支撑转移；不是站姿/倒姿/站姿三张图硬切或叠化。压缩噪声和融化不算动作。
3. 击飞是被动受力，不是脚尖向下的直立飞升、主动跳跃或抬手英雄pose；死亡与活人起身语义分明。
4. 无盾无剑、同一1021，头盔/胸甲/手靴没有中途增大、变形或换材质；不能只看总包围框判断尺度。
5. 所有时刻完整入框、固定镜头、同一阵位，最后回到相近起始脚底/尺度/朝向；首尾相近不等于已证明无缝循环。
6. 完整下载并解码，报告实测尺寸/fps/帧数/时长，不把720p请求档位当作实测结果。
7. 测量实际绿幕时间线和主体颜色；输入RGB恒定不保证输出恒定。色差单列待用户决定，不自动改色或重生成。
8. 单图与自由动作降低了“强行凑尾图”的压力，但不保证连续性；10秒仍可能跳帧、拖延或漂移。若整体动作不成立，不用多抽帧冒充成功。
9. 本轮只生产素材源片。当前1021控制器尚无完整胆怯落地/起身接线，后续是否拆Clip、处理源片局部升降与代码根位移、配置落地/起身状态属于独立任务；不自动部署或改机制。
10. 无论成片是否成功，本侧没有生成授权，不自动继续执行；发起侧也不因技术成功自行标记美术通过，由用户看视频验收。

## 11. 可复制给发起侧的说明

> 请读取 `Locus/knowledge/plan/enemy1021-coward-launch-full-cycle-10s-preparation.md`。这是1021无盾胆怯完整击飞链的新方案：只上传文档里的唯一Idle，`seedance-2-fast / 10s / 720p / 1:1 / 无音频无水印 / seed=-1`，普通 `reference_image`，不做任何尾帧图，不上传1011或其他姿态。提交词为§6英文正文与§8英文负面词合并。文档状态为未生成，需用户明确发起指令；获指令后只创建一次任务，失败或超时不自行重建。视频完整下载/解码后交用户看，不选帧、不抠图、不部署，不改Unity资产。不得把旧§0.18的“禁止落地/起身、6秒、只保持倒姿”重新合并进本轮词。

## 12. 执行结果（2026-10-18，已提交一次、下载并全帧解码；客观不成立）

### 12.1 授权与提交

- **授权**：用户 2026-10-18 “已更新交接文档，请根据交接文档发起视频生成”。
- **参数**：`seedance-2-fast` / 10 秒 / 720p / 1:1 / 无音频 / 无水印 / seed -1；普通 `reference_image` 仅一张。
- **实际提交词**：§6 英文正文（7,284 字符，SHA-256 `6cfea96868161e40c0c4b297e5286c7c8279d37dc545840ccd04753ad4766bc5`）＋ §8 负面词（1,192 字符，`68aad0e58d728a199ee874b2985910e3c18e3bec8882886d461d5fb17cb43507`），合并 `fccc7c2c40bffcc12486bc3beea72d14cfc58db9457dbaf4dc3d6a535ad2ec11`；中文对照未提交，无 `first_frame` / `last_frame`。
- **唯一参考图**：`Library/Locus/tmp/enemy1021_coward_idle_peak_v1/01_coward_idle_peak_source_green_00B140_1024.png`，172,046 bytes、SHA-256 `3fd64c935cd7b5adf562d2729899430e2156120edf0575e969e2faf95146a6c0`、1024×1024 RGB、四角 64×64 中位数均为 `(0,177,64)`。
- **Task**：`task_78a8dfe06b3849be8cbd429c8964b706`；上传 HTTP 200，create HTTP 200，`create_attempts=1`，无重试；usage completion 216,900，`cost.spend=1.21464`（币种未注明）。
- **成片**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_launch_fullcycle_v2_10s.mp4`，3,859,668 bytes、SHA-256 `cef48f8e0850385805975747260a374b634c8e646bbad02abf473de32bce9cd7`；960×960 / 24fps / 241 帧 / 容器 10.041667 秒 / 末帧 PTS 10.0 / H.264 / 无音轨；四段 Range 均 206 且 ETag 一致，MP4 盒结构完整，全帧解码通过。

### 12.2 客观实测

- **停格结构（关键）**：以“相邻帧改动 ≤1,500 px 视为静止”统计，**241 帧中 174 帧落在 3–10 帧长的静止平台上**；另有 **33 次 ≥30,000 px 的单帧换图**（最大 107,112 px，出现在 f079→f080 = 3.29→3.33 秒）。典型序列：f060–f063 静止（改动 541–973 px）→ f063→f064 单帧 71,346 px → f066–f068 再次静止（371–2,451 px）→ f070→f071 单帧 102,631 px。这不属于连续关节运动，而是逐个姿势替换，与 §4/§8 的 `no hard pose replacement` / `no pose-swap animation` 相扣。
- **阶段弧线（主体包围盒，960 空间）**：idle 299×410、底边 703（0–1.25 秒）；离地 f036 1.50 秒（底边 608）；上升至 f060 2.50 秒 474×444、底边 509；空中横置 f071 2.96 秒 524×287、底边 444；下落 f084 3.50 秒 408×297、底边 653；背部平躺 f094–f142 3.92–5.92 秒 619×156、底边 721（持续近 2 秒）；起身 f143–f181 5.96–7.54 秒；回到站姿 f190 起底边 703；末尾 f240 298×412、底边 703。
- **尺度**：首帧 299×408、末帧 298×412，与参考 idle（1021 无盾 298–299×410、底边 703）一致，未见整体放大或缩小。
- **背景色**：四角 16×16 均值约 `(1,160,61)`，与请求的 `(0,177,64)` 不同（最大通道偏差 21，全程稳定在 158–163 绿通道）；后续抠图必须自动取键，不能直接用 `(0,177,64)`。
- **主体颜色**：下采样主体均值从 `(104.7,62.3,61.6)` 漂移到最低 `(82.4,49.2,49.7)`，最大通道差 22.5；色差单列，未改色。

### 12.3 结论与留档

- **按 §10.2 不成立**：没有真实的接触压缩与支撑转移，只有姿势平台之间的单帧切换。
- **重要更正（用户 2026-10-18）**：用户看片后反馈“效果挺好啊”，并在此片基础上要求按 1011 三段式选帧。因此本片**不再归为废片**，而是已认可的动作素材源片；停格结构只用作选帧依据（取平台内部帧），不再作为否决理由。
- **检查材料**：`Library/Locus/tmp/enemy1021_coward_launch_fullcycle_v2_10s/` 的 `FULLCYCLE_V2_TIMELINE.png`（每 10 帧一格）、`FULLCYCLE_V2_SWAP_PAIRS.png`（33 组换图前后对照）、`objective_metrics.json`、`pose_plateaus.json`、`frames_all/`（241 张原帧）、`contact_sheet.png`、`runtime.json`、`download_manifest.json`。
- **脚本**：`build_job.py`、`generate_video.py`、`measure_video.py`、`find_swaps.py`（均已执行；不要重跑创建）。

## 13. 选帧结果（2026-10-18，按 1011 三段式切分；待用户验收）

用户 2026-10-18 “效果挺好…按照 1011 的切分方式找到对应动作的有效帧”。沿用 1011 的 `Launched_Rise / Launched_Fall / Launched_Getup` 三段结构，每段 6 帧，首帧为进入前状态、末帧为段终点。

**选帧依据**：源片是“姿势平台 + 单帧换图”结构，所以每一帧都取在平台内部（前后帧改动 ≤ 10k px，绝大多数 ≤ 2.5k），拿到的都是已停定的干净姿势，而不是换图时的混合帧。

| 段 | 精灵 | 源帧 | 时间 | 包围盒 | 底边 | 1011 对应节拍 |
|---|---|---|---|---|---|---|
| Rise | rise1 | f030 | 1.25s | 300×410 | 703 | 起飞前最后 Idle |
| Rise | rise2 | f035 | 1.46s | 372×417 | 608 | 挑飞开始 |
| Rise | rise3 | f040 | 1.67s | 371×422 | 579 | 早期离地 |
| Rise | rise4 | f047 | 1.96s | 472×441 | 534 | 空中展开 |
| Rise | rise5 | f067 | 2.79s | 502×444 | 507 | 继续升高 |
| Rise | rise6 | f073 | 3.04s | 524×287 | 444 | 高点 / Rise 末帧 |
| Fall | fall1 | f078 | 3.25s | 478×242 | 511 | 下降开始 |
| Fall | fall2 | f083 | 3.46s | 407×297 | 653 | 继续下降 |
| Fall | fall3 | f087 | 3.62s | 478×245 | 682 | 接触/压缩 |
| Fall | fall4 | f091 | 3.79s | 611×162 | 713 | 落地过渡 |
| Fall | fall5 | f100 | 4.17s | 619×160 | 721 | 倒地终态开始 |
| Fall | fall6 | f142 | 5.92s | 619×156 | 721 | 落地尾帧 |
| Getup | getup1 | f144 | 6.00s | 604×177 | 717 | 倒地起始 |
| Getup | getup2 | f149 | 6.21s | 497×226 | 713 | 手臂开始支撑 |
| Getup | getup3 | f155 | 6.46s | 391×274 | 711 | 膝盖/骨盆收回 |
| Getup | getup4 | f163 | 6.79s | 311×284 | 711 | 躯干抬起 |
| Getup | getup5 | f188 | 7.83s | 267×378 | 699 | 接近直立 |
| Getup | getup6 | f240 | 10.00s | 298×412 | 703 | Idle 末帧 |

- **去重校验**：15 组相邻帧差异 16,886–104,226 px，无重复帧；最小一组是 fall5→fall6（同为倒地保持，仅腿部微沉），与 1011 的 landing5/landing6 同为终态保持的性质一致。
- **交付目录**：`Library/Locus/tmp/enemy1021_coward_launch_fullcycle_v2_10s/selected_v2/`（`rise1..6.png`、`fall1..6.png`、`getup1..6.png`，均 960×960 RGB，未抠图、未缩放、未对齐、未补边）；清单 `selected_frames_v2.json`（含逐帧 SHA-256、包围盒、底边、稳定度）。
- **评审材料**：同工作目录的 `RISE_6_REVIEW_v2.png`、`FALL_6_REVIEW_v2.png`、`GETUP_6_REVIEW_v2.png`、`SELECTED_18_ALL_REVIEW_v2.png`；预览 GIF 为 `RISE_6_PREVIEW_v2.gif`、`FALL_6_PREVIEW_v2.gif`、`GETUP_6_PREVIEW_v2.gif`（6 帧 ×100ms，仅评审节奏，不定义 Unity Clip）。
- **未做**：未抠图、未建 Clip、未改控制器/Prefab/场景、未进入 Play。抠图与接入分别验收。

## 14. 抠图结果（2026-10-18，T=80；待用户验收）

用户 2026-10-18：“rise1 和 getup6 是不需要的，因为就是回到了 idle 帧；其他按照容差 80 开始抠图”。因此去掉起飞前待机（旧 f030）与回 idle（旧 f240），段内**重新编号为 rise1..5 / fall1..6 / getup1..5**，共 16 帧。

**配方**（与已验收的 CowardIdle 一致）：Sprite Video Lab Chroma、自动取键（实测键色 `(2,158,59)`）、**容差 80**、softness 16、halo 1、alpha-aware despill、半透明转不透明（cutoff 128）；无缩放、无对齐、无补边、无平滑。执行脚本 `Library/Locus/tmp/enemy1021_coward_launch_fullcycle_v2_10s/matte_t80.py`（须用 `C:/sprite-video-lab-models/venv/Scripts/python.exe`，Python 3.10）。

| 精灵 | 取自 selected_v2 | 包围盒（960） | 可见 px | 半透明 | 透明区非零RGB | 幕布残留 | gEx>8 | 内部改动 |
|---|---|---|---|---|---|---|---|---|
| rise1 | rise2.png (f035) | 336,195–705,607 | 62,757 | 0 | 0 | 0 | 351 | 0/50,926 |
| rise2 | rise3.png (f040) | 347,160–715,579 | 63,091 | 0 | 0 | 0 | 393 | 0/50,842 |
| rise3 | rise4.png (f047) | 296,95–765,533 | 65,085 | 0 | 0 | 0 | 448 | 2/51,896 (maxΔ7) |
| rise4 | rise5.png (f067) | 276,66–775,506 | 65,206 | 0 | 0 | 0 | 428 | 0/51,902 |
| rise5 | rise6.png (f073) | 258,160–779,443 | 57,845 | 0 | 0 | 0 | 397 | 0/46,611 |
| fall1 | fall1.png (f078) | 301,271–775,511 | 56,007 | 0 | 0 | 0 | 450 | 0/46,189 |
| fall2 | fall2.png (f083) | 355,359–759,652 | 56,268 | 0 | 0 | 0 | 524 | 0/45,792 |
| fall3 | fall3.png (f087) | 284,441–759,681 | 50,361 | 0 | 0 | 0 | 329 | 0/41,163 |
| fall4 | fall4.png (f091) | 216,553–825,713 | 51,960 | 0 | 0 | 0 | 306 | 0/43,801 |
| fall5 | fall5.png (f100) | 216,563–833,721 | 51,774 | 0 | 0 | 0 | 314 | 0/43,632 |
| fall6 | fall6.png (f142) | 217,567–833,720 | 51,233 | 0 | 0 | 0 | 317 | 0/43,132 |
| getup1 | getup1.png (f144) | 221,543–823,717 | 52,717 | 0 | 0 | 0 | 252 | 0/44,259 |
| getup2 | getup2.png (f149) | 253,489–745,713 | 53,663 | 0 | 0 | 0 | 197 | 0/44,668 |
| getup3 | getup3.png (f155) | 303,439–691,710 | 53,722 | 0 | 0 | 0 | 243 | 0/44,877 |
| getup4 | getup4.png (f163) | 353,429–659,710 | 51,385 | 0 | 0 | 0 | 206 | 4/43,162 (maxΔ4) |
| getup5 | getup5.png (f188) | 327,323–589,698 | 55,851 | 0 | 0 | 0 | 225 | 0/47,462 |

- **合计**：半透明 **0**、透明区非零 RGB **0**、幕布残留 **0**（距键色 ≤80 的可见像素为 0）、内部改动 **6 px**（maxΔ7）。
- **遗留绿像素**：可见区 `g−max(r,b)>8` 共 197–524 px/帧（合计 5,380），属 SVL 去溢色的既有边界，单帧约占可见像素 0.4–0.9%；白底/深底需用户目视确认。若确认可见淡绿，按既有规则把容差回抬 5–10 重跑即可。
- **交付目录**：`Library/Locus/tmp/enemy1021_coward_launch_fullcycle_v2_10s/keyed_t80_v1/`（16 张 960×960 RGBA：`rise1..5.png`、`fall1..6.png`、`getup1..5.png`）；报告 `matte_report_v1.json`（逐帧 SHA-256、自动键色、包围盒与全部指标）。
- **评审材料**：同工作目录的 `MATTE_16_WHITE_v1.png`、`MATTE_16_DARK_v1.png`、`MATTE_16_CHECKER_v1.png`；预览 `RISE_MATTED_PREVIEW_v1.gif`、`FALL_MATTED_PREVIEW_v1.gif`、`GETUP_MATTED_PREVIEW_v1.gif`（120ms/帧，仅评审）。
- **未做**：未对齐、未补边 1108、未建 Clip、未改控制器/Prefab/场景、未进入 Play。

## 15. 验收与交付快照（2026-10-18）

- **用户验收**：源片与 16 帧透明素材均“已验收”。首末两帧（起飞前待机、回 idle）不属于本动作，已按要求剔除并重编为 rise1..5 / getup1..5。
- **当前可交付**：`Library/Locus/tmp/enemy1021_coward_launch_fullcycle_v2_10s/keyed_t80_v1/`（16 张 960×960 RGBA）。
- **尚未做**（属接入侧）：补边 1108、3 个 Clip（Rise 5 帧 / Fall 6 帧 / Getup 5 帧，沿用 1011 的 30fps 口径）、控制器状态与过渡。目前控制器 `CowardLaunched → CowardIdle` 直连，落地/起身接线需单独确认；`HasGetupAnimatorState()` 只认 `Launched_Getup`。
- **可复用经验**：已写入 `memory/enemy1021-coward-launch-recipe.md`（单图+动作自由、先测平台再选帧、键色每片实测）。

### 15.1 消费端注意事项

- 16 帧是**停格源**（每个姿势取自源片的静止平台），直接以 30fps 连成 6 键 Clip 会有“逐帧跳”而不会自动变连续；若游戏里跳感明显，应补中间帧或缩短单段时长，不能靠插值拖平。
- 三段的底边并不统一（Rise 从 607 到 443，Fall 从 511 到 721，Getup 从 717 到 698），因为真实动作本身就有升降；真正的落地/浮空高度由 `Enemy.UpdateLaunch()` 的根节点位移承担，补边时不要逐帧拉齐脚底。
