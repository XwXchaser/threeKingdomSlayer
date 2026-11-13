# Enemy 1051 素材源包（曹军弓手 · 形象制作）

> **用途**：1051 弓手的**形象制作**素材与台账。
> **状态（2026-11-08）：形象 + idle / walk / attack 三套动作均已验收**；12 张动作帧在 `03_AttackFrames/` `04_IdleFrames/` `05_WalkFrames/`，
> **尚未进 `Assets/`**（1051 还没有 prefab）。完整交接见方案文档 §10。
> **⚠ Dead 套（P3 关键姿势图）四轮全部验收不通过，已作废**：四版原图已撤出本包，归档在 `C:/Users/steam/Pictures/gptGen/enemy1051_rejected/03_DeadPose_P3/`，**不得当素材用**。根因与修正后的量化规格见 `plan/enemy1051-remaining-animation-art-plan.md` §4.7.6。
> **共用规格**：1108² 透明底 / 人物本体高 605 / 可见脚底线 897 / 身体中心 x 521 / pivot 0.5,0.5（PPU 16）；半透明 0、幕布残留 0。
> **落位三原则**：固定 k + 统一偏移（不逐帧归一化）/ 按动作相位抽帧（不按时间等距）/ 选帧前先整片看相位尺（避开闪回）。
> **形象来源**：四兵种并排比例组图 `C:/Users/steam/Pictures/gptGen/enemy_proportion_lineup_v1/enemy_proportion_lineup_v1.png`
> （最右一位，SHA-256 `e08eeb0eefb495b4f785886dba1ab206b5e08cdd0ea1c1eca1ab6d0a610c96fe`）的裁切。
> **方案文档**：`Locus/knowledge/plan/enemy1051-bandit-archer-redesign-handoff.md`（含 T1–T7 形象研发史与「奸邪面相」规格）。
> **剩余动作方案**：`Locus/knowledge/plan/enemy1051-remaining-animation-art-plan.md`（受击 3 套 / 击飞 3 套 / 死亡 1 套，共 36 帧；已定：Dead 弓脱手、Rise 弓不脱手、箭只在射箭时存在、不上传动作参考图只上传身份照；批次 A 先做 Dead）。
> **管线经验**：`Locus/knowledge/memory/enemy1012-art-replacement-pipeline.md`、`Locus/knowledge/memory/enemy-proportion-lineup-reference.md`。
> **现行画风**：`Locus/knowledge/design/art-style-guide.md`。

## 目录

| 目录 | 内容 |
|---|---|
| `00_Index/` | 台账：`videos.csv`（11 支源视频的 sha256 + 字节数，`verify_art_sync.py` 可校验）、`identity-images.csv`（形象基准与锚图） |
| `01_SourceVideos/` | 射箭动作成片（含被否的 v1/v2 存档）与关键帧母片 |
| `02_IdentityImages/` | 身份图 / 绿幕锚图 / 姿势图 / **旧 105 动作帧姿势参考**（**视频生成输入**，不是游戏素材） |
| `03_AttackFrames/` | **attack 射箭动作帧**（1108² 落位成品 + 对照图 + 源帧 + 台账）—— **已验收** |
| `04_IdleFrames/` | **idle 待机动作帧**（1108² 落位成品 + 对照图 + 循环 GIF + 源帧 + 台账）—— **已验收** |
| `05_WalkFrames/` | **walk 行走动作帧**（1108² 落位成品 + 对照图 + 循环 GIF + 源帧 + 台账）—— **待验收** |

## 02_IdentityImages 现有文件（2026-11-08）

| 文件 | 规格 | 用途 |
|---|---|---|
| `1051_lineup_crop_reference_rgba.png` | 334×479 RGBA，alpha 二值 | 并排组图的独立裁切（研究参考 + 后续加工源） |
| `1051_idle_identity_greenscreen_1024.png` | 1024² / 绿 `RGB(0,159,62)` std 0 / 人物本体高 **696** / 脚底 **895** / 身体中心 **474.5** | **idle 形象**（视频生成参考图） |
| `1051_cast_anchor_green_1080x1080.png` | 1080² / 绿 `RGB(0,159,62)` std 0 / 人物本体高 **470** / 脚底 **1000** / 身体居中 **540** | 视频**首尾帧锚图**（seedance frames 工作流） |
| `1051_vs_1013_proportion_sheet.png` | 脚底对齐 | 人工复核体型用 |
| `1051_lineup_crop_measure.json` | 量测记录 | 脚本产出，含逐列最高像素剖面 |

## 量测摘要（并排图 → 裁切）

| 项 | 值 |
|---|---|
| 人物本体高（头顶发髻→脚底） | 468 px（并排图坐标 379→846） |
| 身体中心列 | 1327.5（并排图坐标） |
| 总高 / 总宽（含弓与箭袋） | 479 / 334 px |
| 弓顶高出头顶 | 11 px |
| 与同图 1011 人物本体高之比 | 468 / 472 = 0.992（同族成立） |
| 换算到 1108² 游戏精灵（普通兵 605 px） | 身体 605 / 总高 619 / 总宽 **432 px** |
| 五列阵型列距 273 px → 主体横向跨度 | **1.58 列**（弓与箭袋会压到邻列，与 1013 旗面同性质的问题） |

## 复现命令

```bash
python Tools/visual_brief/crop_lineup_1051.py \
  --src "C:/Users/steam/Pictures/gptGen/enemy_proportion_lineup_v1/enemy_proportion_lineup_v1.png" \
  --out-dir "Enemy1051ArtSource/02_IdentityImages" \
  --ref-1013 "C:/Users/steam/Pictures/gptGen/enemy1013_idle_ref_v1/1013_lineup_crop_rgba.png"
```

## 硬规矩（摘自 1012 管线经验）

1. **抠图**：源图是直通道 alpha，阈值 128 二值化即可，不会带白边；**连通域必须四邻接**（见下条坑）。
2. **连通域**：并排图里相邻角色会粘连，且 x 区间重叠，**不能靠 x 窗口选人**，要靠连通域整体。
3. **头顶 ≠ 最高像素**：弓顶比头顶高 11 px，头顶取「身体中心列 ±60px 内的最高像素」。
4. **落位**：按**可见底行**对齐（不是几何底）；同一动作共用一组 k 与偏移。
5. **写精灵**：必须保留 alpha；只覆盖 PNG 字节、保留 `.meta`；写完回读核对。

## 已知待办

- **脸读成「凶」而非「奸邪」**：本组图的弓手双眉用力下压成怒视，与 T3/T5 定稿的「左挑右压、眼神算计」不一致（见 `Locus/knowledge/plan/enemy1051-bandit-archer-redesign-handoff.md` §5.5/§5.7）。半蒙面拿掉「笑」的通道后，这条线必须靠眉眼修正才能立住。→ **2026-11-08 用户已定：接受怒视脸**，不做眉眼精修。
- **横向跨度 1.58 列**：弓握在身体右侧、箭袋在左肩，进五列阵型会压邻列，需在接入时决定握弓侧或压缩弓的横向占位。

## 射箭动作（2026-11-08）
| 文件 | 说明 |
|---|---|
| `01_SourceVideos/1051_shoot_v4_6s.mp4` | **最新成片**：持箭待机→搭箭→满弓→放箭；手里只有一支箭、头全程正对镜头 |
| `01_SourceVideos/1051_shoot_keyframes_v4_6s.mp4` | v4 关键帧母片（持箭待机/搭箭/满弓/放箭 四张姿势图排成 6.000s） |
| `01_SourceVideos/1051_shoot_v3_6s.mp4` | 作废：搭箭时箭被画成一捆散羽 + 头转侧脸 |
| `01_SourceVideos/1051_shoot_v2_6s.mp4` | 作废：缺 idle 衔接，箭被拉成长斜箭 |
| `01_SourceVideos/1051_shoot_keyframes_6s.mp4` | 关键帧母片（idle/搭箭/满弓/放箭 四张姿势图排成 6.000s） |
| `01_SourceVideos/1051_shoot_v2_6s.mp4` | 作废：缺 idle 衔接，箭被拉成长斜箭 |
| `01_SourceVideos/1051_shoot_v1_6s_REJECTED_aim_sideways.mp4` | 作废：纯文字描述动作，模型把瞄准方向改到画面右侧 |
| `02_IdentityImages/1051_pose_draw_greenscreen_1024.png` | 满弓姿势（射箭峰值）绿幕图，**箭头短且垂直朝下** |
| `02_IdentityImages/1051_pose_nock_greenscreen_1024.png` | 搭箭姿势绿幕图 v1（作废：握杆中段） |
| `02_IdentityImages/1051_pose_nock2_greenscreen_1024.png` | **拉弓开始·搭箭姿势绿幕图 v2**（手捏箭尾、箭抵弓弦）—— attack4 用 |
| `02_IdentityImages/1051_pose_draw2_greenscreen_1024.png` | 满弓静帧 v2（作废：箭没靠弓） |
| `02_IdentityImages/1051_pose_ready_greenscreen_1024.png` | 持箭待机姿势绿幕图（v4 母片第一帧：右手垂握一支箭、头正对） |
| `02_IdentityImages/1051_pose_release_greenscreen_1024.png` | 放箭姿势绿幕图 |
| `02_IdentityImages/1051_pose_draw_v1_*_diagonalarrow.png` | 作废存档：箭头被画成 45° 长斜箭 |
| `02_IdentityImages/1051_old105_attack{1,2,3}_pose_reference.png` | 旧 105 动作帧做成的同构图姿势参考（生图与视频的姿势真源） |

复现与验收细节见 `Locus/knowledge/plan/enemy1051-archer-shooting-video-plan.md` §7。

## 03_AttackFrames —— attack 射箭（2026-11-08 用户选帧 + 握法修正，6 关键帧）**已验收**

| 文件 | 拍 | 源 | t(30fps) |
|---|---|---|---|
| `03_AttackFrames/Enemy_1051_attack1.png` | 抽箭·抬手 | v3 `f020` | 0.00 |
| `03_AttackFrames/Enemy_1051_attack2.png` | 抽箭·箭到手 | v3 `f026` | 0.35 |
| `03_AttackFrames/Enemy_1051_attack3.png` | 举弓到身前 | v3 `f027` | 0.70 |
| `03_AttackFrames/Enemy_1051_attack4.png` | **拉弓开始·搭箭（手捏箭尾、箭抵弓弦）** | 姿势图 `1051_pose_nock2`（替掉原 f045） | 1.05 |
| `03_AttackFrames/Enemy_1051_attack5.png` | 满弓定住（峰值） | v3 `f069` | 1.50 |
| `03_AttackFrames/Enemy_1051_attack6.png` | 放箭·收势 | v3 `f113` | 2.50 |
| `03_AttackFrames/1051_attack_landing.json` | 落位量测 + 6 键时间轴 + pivot | |

规格：1108² / 透明 / 人物本体高 605 / 可见脚底线 897 / 身体中心 521 / pivot 0.5,0.5（PPU 16）；六张均 **半透明 0 / 幕布残留 0**。

**总长 2.767s**（= 旧 105 `Enemy_105_Attack.anim` 的 StopTime），满弓峰值停 1.0s。

**搭箭帧重出**：用户定的 f045 握法错（右手握在**箭杆中段**、白羽戳在手上方）。按用户指正，目标不是「满弓」而是**「拉弓开始」**（姿势参考 = 旧 105 的 `enemy5_attack1`），并同时修两处：① 右手**捏箭尾**、白羽在拳里、箭头紧贴拳下；② **箭必须抵在弓弦与弓身上**、弦从弓顶拉到同一只手，不许箭飘在弓外。生成物 `1051_pose_nock2`，已与旧 105 attack1 放大三方比对一致。
**作废**：`1051_pose_draw2`（满弓静帧，拉满时箭没靠弓）。

**attack ↔ idle 配准**：站姿帧掩码 IoU 搜索 → attack1 对 idle1 的 IoU 0.8814、偏移 **dx=−12** → attack 六张统一左移 12 px，对齐后 attack1/2/3 的脚位与 idle 完全一致（x 335–681）。

## 04_IdleFrames —— idle 待机（2026-11-08）**待验收，未获用户确认**

| 文件 | 源 |
|---|---|
| `04_IdleFrames/Enemy_1051_idle1..6.png` | idle 视频 `1051_idle_v1_4s.mp4` 的 f000/f008/f016/f024/f032/f040 |
| `04_IdleFrames/1051_idle_landing.json` | idle 落位台账 + 时间轴 + pivot |
| `04_IdleFrames/idle1-6_循环预览.gif` | 循环预览（另有 `_旧选_…_循环预览.gif` 作对比） |

周期 **48 帧 = 1.98 s**；时间轴 t = 0 / 0.33 / 0.66 / 0.99 / 1.32 / 1.65；pivot 与 attack 同为 **0.5,0.5**（PPU 16），保证 idle↔attack 切换不跳位。

**idle 踩到的两个坑（重要）**：
1. **落位必须固定 k**（k=0.96645）：逐帧归一化到本体高 605 会把「头上下浮动」翻译成「整体缩放」→ 看着像在缩放、反倒没呼吸感。
2. **选帧不能取模绕回**：旧选法「相位起点 + k·周期」再对总帧数取模，最后一步变成 19→77（58 帧而非一个周期）→ **循环接缝跳一大截**。改为谷底起步、整段取帧不绕回。

**已知**：源片里模型**没做胸肩起伏**（全身高/头顶y/脚底y 全程恒定），帧间变化是重绘噪点 + 表情弧线；所以这六帧是「表情+轮廓微动」的待机，不是胸肩呼吸。

## 05_WalkFrames —— walk 行走（2026-11-08）**待验收**

| 文件 | 说明 |
|---|---|
| `05_WalkFrames/Enemy_1051_walk1..6.png` | 六帧，源 `1051_walk_v4_4s.mp4` 的 f005/f008/f010/f013/f015/f018（取自**闪回之前**的干净对侧交替段 f00–f18） |
| `05_WalkFrames/1051_walk_landing.json` | 落位台账 + 时间轴 + pivot |
| `05_WalkFrames/walk1-6_对照图.png`、`walk1-6_循环预览.gif` | 复核 |

时间轴 t = 0 / 0.1 / 0.2 / 0.3 / 0.4 / 0.5（循环 **0.6s**，与 1012 的 walk 动画同规格）；pivot 0.5,0.5（PPU 16）；**固定 k + 统一偏移**（走路不能逐帧对位，否则会抹掉水平摆动）。

**两条路线的教训**：
- ❌ 母片路线（把旧 105 的两帧走姿喂给模型）→ 模型只在两个姿势间跳帧，完全不流畅。**喂两帧 = 让模型偷懒**。
- ✅ 文字路线（从身份锚图让模型自己走）→ 步态连续；源片结构是「静止 20 帧 → 走 30 帧 → 静止 18 帧 → 走 28 帧」，步态周期 23–24 帧。

**选帧的教训**：
- ❌ **按时间等距抽**（第一版 f017/021/025/029/033/037）→ 六个相位值 0.74/0.72/0.73/0.65/0.05/0.76，五个落在同一相位，循环像“并腿→并腿→并腿”。
- ❌ **不先看整片就抽**（第二版 f012/015/017/022/025/028）→ 跨过了源片 **t≈1.0s 的闪回**（步态倒退/重放），闪回被带进动画。
- ✅ **先整片分段看相位尺，再按步态相位抽**：f00–f18 是干净的对侧交替（f05 右脚落地 → f08 左脚抬起 → f10 并腿 → f13 左脚落地 → f15 并腿 → f18 右脚落地），**walk6 与 walk1 同相位** → 接缝顺。f19–f30 是闪回区、f31 之后退化为单腿踏步，都不能用。

**闸门**：逐步腿部差 0.343/0.348/0.363/0.376/0.412/0.285（接缝；已入库 1012 walk 为 0.583/0.201/0.297/0.557/0.180/0.235）→ 每步都有真实变化且更均匀；六张均 半透明 0 / 幕布残留 0。

**文字路线也有两个坑（都踩过）**：
- v2：腿连续了但**躯干在扭转** → 看着像斜着走 → 提示词要写「肩膀/胸/胯始终正对镜头，只有腿和手臂动」。
- v3：写了 “marches straight **at the viewer**” → 他**真的朝镜头走来**（脚底线漂 103px、身体变大）→ 必须写「**原地踏步**、不靠近镜头、尺寸逐帧不变」。
- v4（采用）：躯干中心极差 17px、脚底极差 16px、主体高极差 3.3% → 原地踏步且躯干不扭。

**注意**：v3 视频里的「抽箭」帧，箭仍偏“一捆散羽”（v3 固有瑕疵）；v4 那版把箭修成一支但**丢了抽箭动作**，两者都留档，用户选了 v3 的动作。

**未做**：12 张 PNG 暂未进 `Assets/`（1051 还没有 prefab，入 Assets 会变成无引用资产）；`.anim` / prefab / 波次接入需要 Unity Editor（**当前 crashed**）且需单独授权。

> **idle 尚未验收**：attack 六帧由用户于 2026-11-08 确认通过；idle 六帧为同批产出，**用户尚未确认**，不要当作已验收资产使用。
