---
id: kd_e90d59f3-bb55-446d-9433-44ba7d591d6a
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_1012_bandit_idle_anim_2026_10_30
summary: 1012 新形象（兜鍪贼兵）的 idle 动画重做方案：实测现有 Idle clip/画布/脚底/prefab/controller 事实，选定「绿幕静帧→Seedance 视频→SVL 抠图→选帧→1108 落位→接入」路线，规格为 6 帧 0.6s 蓄势型交战 idle，含完整中英文视频词、参数、G1/G2/G3 颜色门槛与逐阶段验收；绿幕身份图已备好待用户批准付费生成。
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 1012 贼兵新形象 · idle 动画重做方案

> **状态：用户已验收（v4 表情合格、6 帧、1.8s 周期）。本轮工作完结，后续转 1051。**
>
> v1（疲惫脸）已被本轮替换；v1 成片保留在 `C:\Users\steam\Videos\doubaoVideo\enemy1012_bandit_idle_v1.mp4`。

---

## 0.1 v2 表情修正轮（用户反馈「视频里的角色看着疲惫」）

### 0.1.1 反馈与根因

用户验收 v1 idle：**「当前角色看着并不奸邪而是很疲惫的表情」**。

根因（两条，都在我自己的提示词里）：

1. A4 词里写了 `a slight shadow under the eyes` → 模型放大成眼袋/黑眼圈（疲惫主因）。
2. v1 视频词里写了 `dull heavy-lidded eyes` / `unfocused` / `dull` → 直接把脸锁成半睁困倦眼；模型还把静态里嘴角的上挑抹平成下撇的苦脸。

对照放大（静态 vs 视频帧）确认：视频里眉变平、眼睑下垂盖住虹膜、嘴角下撇、颊部张力丢失。

### 0.1.2 修正链（三步，用户逐次审批）

| 步骤 | 产物 | SHA-256 / 规格 | 结果 |
| --- | --- | --- | --- |
| A5 头肩表情修正 | `C:\Users\steam\Pictures\gptGen\enemy1012_bandit_face_v3\stageA5_expression_fix_v1.png` | `df3c00866d9a60a021121b894b493f6e49f2b2208c9aa45602dffa59c21acbec`，1024² RGB，1,203,588 bytes，HTTP 200 / 80.53s | 输入 = B3 头肩裁切 `stageA5_expression_input_v1.png`（SHA `28b3c8a4…c2d6`）。去眼下阴影与眼袋、下睑收紧上提、眉斜压外端上挑、嘴角单侧上挑、颊与下颌收紧；负面词加入 tired/sleepy/drowsy/weary/exhausted/drooping eyelids/under-eye bags/bored/sad/frown。**副作用**：肤色略亮略偏橙，蜡黄病色被冲淡（用户已知悉并接受） |
| A5b 回贴全身 | `C:\Users\steam\Pictures\gptGen\enemy1012_bandit_face_v3\stageA5b_fullbody_v1.png` | `279632ce4e4ed5b7fbdc3f9a4878537107875999c54dcc176725f9711816c813`，1024² RGB，1,229,726 bytes，HTTP 200 / 76.34s | image1 = B3 全身（权威，姿态原样不动），image2 = A5（只供脸）。body bbox 高 889 / 中心 x 444.5（B3 为 889 / 445）→ 姿态与几何保留 |
| 绿幕身份图 v5 | `C:\Users\steam\Pictures\gptGen\enemy1012_idle_v5\input_identity_v12_bandit_idle_v5_1024.png` | `f25fee4e3a7aa958a720356fbd214ca5b829688525753e9c248c9c9bc3ad19e6`，389,230 bytes | 1024²、绿 `(0,159,62)`、G1 四角中位数 `(0,159,62)` std 0、主体 bbox (180,200,771,895)、主体高 696、脚底 895、中心 475.5（与 v1 身份图同规格） |

### 0.1.3 v2 视频

| 项 | 值 |
| --- | --- |
| 任务 | `task_174012910b714452acf6b15b5e9ad284`，第 1 次即成功，HTTP 200；轮询 12 次/92s 仍 generating，第二轮单查即 success |
| 词 | 表情段改为用户选定的 **C 方向（冷而算计）**：`the face itself stays quiet and controlled, the eyes narrow and bright, angled slightly to one side as if he is studying the viewer, one brow a little higher than the other, the jaw set and the cheekbones sharp, the lips closed with one corner lifted just a little in a cold half-smirk that never reaches his eyes` + 硬约束 `must never look tired, sleepy, drowsy, weary, exhausted, bored, sad or blank … never droop his eyelids or turn his mouth corners downward`；删除了 `dull / heavy-lidded / unfocused` |
| 成片 | `C:\Users\steam\Videos\doubaoVideo\enemy1012_bandit_idle_v2.mp4`，960×960 / 24fps / 97 帧 / 4.042s / 1,770,401 bytes / SHA `85f3e9485da5d65ba22d3b18181cdcd9a32dcc503ed001a4e49165c307f5e8cb`；API 未返回 usage/cost |
| G2 | 背景绿全片 **G 140–143**（ΔG **−16…−19**；首帧 −16、末帧 −18、最差 f11 = −19）；每帧四角 std 0.2–1.7（帧内均匀）。与 v1 同规律，**仍为待用户决策项** |
| 主体稳定 | bbox min x 162–172、min y 184–192、max x 723、max y 839（恒定）→ 镜头锁定、无平移 |
| 目视 | 疲惫感消除：眼张开、虹膜可见、眉斜、嘴角单侧上挑；不再困倦 |

### 0.1.4 v2 抠图 / 选帧 / 落位 / 接入

- 抠图：SVL Chroma T=79 / softness 16 / halo 1，97 帧全过；半透明 0、幕布残留 0、孔 0
- 选帧：**f15 / f22 / f29 / f36 / f43 / f50**（帧间差 2.61–3.20、尾→首 2.49，最均匀）
- 落位：k=0.93125（960→894），单一偏移 x=106；入 1108² 后 **脚底 897 六帧全同**、中心 x 519–522.5、高 603–608
  - **踩过的坑**：首次落位用缩放前的中心算偏移（76）→ 整体左移 30px（cx 490），已修正为 106
- 接入：覆盖 `Assets/Sprites/Enemy/Enemy1012/Enemy_1012_idle1..6.png`（保留 .meta → GUID/导入设置不变）；`.anim` 无需修改
- Unity 实读：meshMinY 六帧全 **−21.708** → 脚底世界 Y **−1.8044**（地面 −1.8）；世界高 3.166–3.195u（旧 idle1 3.166u）；clip length **0.6**、6 键 t=0/0.1/…/0.5 → idle1..6；prefab 默认图 idle1、`visualYOffset = 0.001` 不变
- 备份：`Library/Locus/tmp/backup_Enemy1012_idle_v2_20261030-161932/`（落位错误版）与 `backup_Enemy1012_idle_v2b_20261030-162019/`（修正版覆盖前）
- 预览：`Library/Locus/tmp/enemy1012_idle_v5/idle_preview.gif`

## 0.3 v3 表情轮：奸诈笑容（露一线上齿）

用户反馈 v2 idle：「需要有奸诈的笑容表达情绪，当前表情变化不丰富导致表现力不足」；用户选择「露一线上齿的坏笑」。

### 0.3.1 资产链

| 步骤 | 产物 | SHA-256 / 规格 | 结果 |
| --- | --- | --- | --- |
| A6 头肩坏笑 | `C:\Users\steam\Pictures\gptGen\enemy1012_bandit_face_v3\stageA6_wicked_grin_v1.png` | `75c780df53c0517bb46b985c2b25ca5c53ac7061295b733d1f2707712c0844f1`，1024² RGB，1,195,134 bytes，HTTP 200 / 75.45s | 输入 = A5 头肩（已通过）。单侧嘴角明显上挑、上唇抬起露一线上齿、挑起侧颇部堆起苹果肌、眼眯成窄缝、眉外端上挑。负面词封掉 friendly smile / open mouth / full of teeth / lower teeth / symmetric grin / tired 等。**副作用**：上齿比“一线”略满（整齐一排），嘴未张开 |
| A6b 回贴全身 | `C:\Users\steam\Pictures\gptGen\enemy1012_bandit_face_v3\stageA6b_fullbody_v1.png` | `fcd54ab3640b52c578772c2935297dc87bebd61e234ba459bb0483ac73c8f53e`，1024² RGB，1,222,270 bytes，HTTP 200 / 81.64s | body bbox 高 889 / 中心 x 444.5（与 A5b、B3 完全一致）→ 姿态与几何保留。**流程失误**：image 1 误用 B3 而非 A5b；因 A6 覆盖整张脸且三者身体姿态/几何相同，结果未受影响 |
| 绿幕身份图 v6 | `C:\Users\steam\Pictures\gptGen\enemy1012_idle_v6\input_identity_v12_bandit_idle_v6_1024.png` | `aa5587299ef95256e2ba4b1f0aec5b213cf8144eb7fe40f0c978b0450f90c01e`，387,447 bytes | G1 四角中位数 `(0,159,62)` std 0；主体 bbox (180,200,771,895)、高 696、脚底 895、中心 475.5 |

### 0.3.2 v3 视频

| 项 | 值 |
| --- | --- |
| 任务 | `task_d3522f79488648408394378735bfa829`，一次创建成功（queued→generating→success） |
| 词 | 表情段改为「保持图 1 的奸笑并让它活起来：坏笑缓慢加深——挑起侧嘴角再抬一点、上齿一线保持可见、苹果肌笑纹加深、眯眼仍冷——末尾回到起始坏笑」；同时保留 never lose the grin / never open his mouth wide / never bare his lower teeth / never tired-sleepy-drowsy-weary-bored-sad-blank / never droop his eyelids |
| 成片 | `C:\Users\steam\Videos\doubaoVideo\enemy1012_bandit_idle_v3.mp4`，960×960 / 24fps / 97 帧 / 4.042s / 1,790,290 bytes / SHA `2efdbfe7411a31fdd974659b681410ee94cabd8e18e8a6d8905e545861e17179`；API 未返回 usage/cost |
| G2 | 背景绿全片 **G 125–139**，ΔG **−20…−34**（首帧 −20、末帧 −32、最差 f5 = −34，中位 −32）→ **比 v1/v2（−16…−19）明显更偏暗**，片基漂移升级，仍为待用户决策项 |
| 主体稳定 | 抠图后 bbox 恒定（max x 722、max y 838）；raw 的 f5 有一个孤立噪点（max x 887），已被抠图链路消除 |

### 0.3.3 关键实测：表情弧线**没有**实现

对 97 帧逐帧统计嘴部区域高亮（牙齿）像素数：**18–39 之间无趋势抖动，峰值在 f0**。即模型把坏笑**冻住**了，只有微抖；词里写的「缓慢加深再回落」没有落地。

→ 本轮实际交付的是「**固定坏笑 + 呼吸循环**」，不是「表情有起伏的循环」。若要真正的表情变化，下一轮需改写法：把表情写成**分段时间线**（第一秒闭嘴冷脸 → 中段嘴角上挑露齿成坏笑 → 末尾收回），而不是「保持并加深」。

### 0.3.4 抠图 / 选帧 / 落位 / 接入

- 抠图：SVL Chroma T=79 / softness 16 / halo 1，97 帧全过；半透明 0、幕布残留 0、孔 0
- 选帧：**f18 / f23 / f28 / f33 / f38 / f43**（约束：六帧均需露齿（teeth≥25）、帧间差 1.95–3.93、尾→首 1.46、牙齿像素 25→34→28 有一定表情差）
- 落位：k=0.92649（960→889），单一偏移 x=**107**（按**缩放后**的中心算，修正了上一轮用缩放前中心导致的 30px 左移）；入 1108² 后脚底 **897 全同**、中心 x 518–524、高 602–608
- 接入：覆盖 `Assets/Sprites/Enemy/Enemy1012/Enemy_1012_idle1..6.png`（.meta 保留 → GUID/导入设置不变）；`.anim` 无需改
- Unity 实读：meshMinY 六帧全 −21.708 → 脚底世界 Y **−1.8044**（地面 −1.8）；世界高 3.161–3.195u；clip length **0.6**、loop=true、6 键 t=0/0.1/…/0.5 → idle1..6
- 备份：`Library/Locus/tmp/backup_Enemy1012_idle_v3_20261030-174531/`
- 预览：`Library/Locus/tmp/enemy1012_idle_v6/idle_preview.gif`、`head_zoom.png`、`mouth_zoom.png`、`contact_sheet.png`
- 场景里的测试立牌 `Enemy_1012_IdleTest` 已自动指向新帧（默认图 idle1）

---

## 0.4 v4 轮：专攻表情起落（筹划，2026-10-30）

> **状态：方案已写好，等用户审批后提交。尚未创建任务。**

### 0.4.1 目标与可量化验收

目标：idle 循环里**脸要真的动起来**——从冷脸 → 嘴角上挑 → 坏笑露齿 → 回落，形成一个可读的表情弧线，而不是固定坏笑。

验收不再靠“看着像”，而是先定死**指标**（v3 就是靠这个指标判定失败的）：

| 指标 | 口径 | 通过条件 |
|---|---|---|
| 嘴部开口度 | 嘴部区域（y 285–325, x 395–545）内高亮像素数（上齿）逐帧序列 | 必须呈“起—峰—落”：前 1/4 的均值 +6 以上 < 中 1/2 峰值，且末 1/4 均值回落到 峰值 +4 以内 |
| 嘴角高度 | 嘴部外侧下缘像素的最小 y（越靠上=笑起越高）逐帧序列 | 与开口度指标同向（两者相关系数为正） |
| 主体稳定 | 抠图后 alpha bbox | max x / max y 恒定（±2px） |
| 疲惫回弹 | 眼区白像素占比 + 嘴角是否下撇 | 六帧内不得出现“眼半闭 + 嘴下撇”的组合 |

v3 实测：上齿序列 18–39 无趋势（峰值在 f0）→ **判定不通过**，这就是本轮要修的东西。

### 0.4.2 v3 为什么没成（推断）

1. **词里把脸钉死了**：`Keep his sly grinning expression from image 1` + `Keep his iron helmet … his face … exactly as they are in image 1` → 两处“完全保持”把脸锁成静态。
2. **负向词把脸也钉死了**：`never lose the grin / never open his mouth wide / never bare his lower teeth / never turn into a friendly smile / never droop his eyelids` —— 一串针对脸的“绝不”相当于告诉模型“脸不要动”。
3. **动作用完了**：`The motion is a restrained breathing idle cycle` + `Keep the whole motion subtle` → 运动预算全给了身体，脸被排到零。
4. **脸在画面里太小**：960 画布里脸宽约 200px，细微表情对模型本就难。

### 0.4.3 v4 的三个改法

| 改法 | 具体做法 |
|---|---|
| ① **表情写成时间线** | 不再写形容词，而是按秒写：“第一秒闭嘴冷脸 → 第二秒嘴角开始单侧上抬 → 中段成奸笑、露出一线上齿、颊部堆起笑纹、眼睛再眯一点 → 第三秒起回落 → 末尾回到起始冷脸”，并写明 `His face must visibly travel through this arc` |
| ② **减负负向词** | 只保留四条（嘴不张开、不露下齿、不变成友善笑、不疲惫困倦），**删掉** `never lose the grin`（与弧线矛盾）与 `never droop his eyelids`（改为“眼睛全程保持窄而冷”） |
| ③ **把脸写成镜头里的主动作** | `The expression is the main action of this shot and it changes over the four seconds`；并加一句躯体的微反应（肩与重心随表情轻微一动） |

代价：这是“允许脸变”换来的，模型有概率把脸做崩（过度变形、咧嘴、掉五官）→ 因此仍按头肩先验证、再回贴全身的流程，**不直接拿全身图冒险**。

### 0.4.4 执行流程（两阶段，每阶段审批）

```text
E1  头部表情弧线研究片（头肩专用，identity = A6 头肩）
    → 4s / 720p / 1:1 / seedance-2-fast / 1 次
    → 下载解码 → 逐帧量上齿序列与嘴角高度 → 出弧线曲线图
    → 判定：弧线成立？（不成立则不进入 E2）
E2  全身 idle（identity = A6b 全身绿幕图，词里同样用时间线）
    → 4s / 720p / 1:1 / seedance-2-fast / 1 次
    → 抠图 → 选帧（按弧线峰值/谷值挑，而非均匀取样）→ 1108 落位（脚底 897、中心 521）→ 替换工程 6 张 → Unity 实读复验
```

**E1 为什么先做头部**：脸占画面比例从 200px 提到 ~600px，模型做表情的可行性大幅提升；E1 便宜且失败不污染工程。E1 的成片还能当 E2 的 `reference_video`（表情表演参考），这是可选的加强项。

### 0.4.5 取帧与播放长度（交付口径）

- 源视频固定 4s（不变）。
- 选出帧后，**弧线峰/谷要落在关键帧上**（否则表情变化在游戏里看不到）。
- 播放长度两个口径：
  - **沿用现状**：6 帧 / 0.6s（工程与 controller 零改动）—— 4s 的弧线被压到 0.6s，表情会变成“坏笑一闪”；
  - **拉长**：12 帧 / 1.2s（弧线看得清）—— 会同时把 Idle→Attack 的 `exitTime 0.75` 触发从 0.45s 推到 0.9s（这是事实陈述，不是本轮争论点）。
  本轮先按 **6 帧 / 0.6s** 交付（不改任何工程时序），效果不够再谈拉长。

### 0.4.6 兜底（若 E1/E2 仍无弧线）

1. **多姿态表情表**：用静态图编辑做 3 个表情（冷脸 / 半笑 / 满坏笑），再在 sprite 帧层面按曲线替换嘴部区域（头部位移仅 ±5px，合成可行但需像素对齐验证）；
2. **接受固定坏笑**，把表现力转给武器与身体语言（刀光、衣摆、重心）；
3. **拉长源视频到 6–8s**，给弧线更多展开时间（接口允许 4–15s，费用需重新确认）。

### 0.4.7 E1 请求规格（待审批）

```text
endpoint: POST https://direct.reachapi.ai/v1/vids/create
model: seedance-2-fast
duration_seconds: 4
resolution: 720p
aspect_ratio: 1:1
generate_audio: false
watermark: false
seed: -1
input.content: [ text, image_url(role=reference_image) ]
reference_image: C:\Users\steam\Pictures\gptGen\enemy1012_bandit_face_v3\stageA6_wicked_grin_v1.png
    （A6 头肩，已验收；头与肩占满画面，脸占比最大）
创建次数: 1
输出: C:\Users\steam\Videos\doubaoVideo\enemy1012_expression_arc_v4.mp4
```

英文 prompt（实际提交）：

```text
Game sprite animation of the armored bandit swordsman shown in image 1, in the same high-definition pixel-art style and the same colors. Locked camera, fixed framing, fixed position and fixed scale: his head stays in exactly the same spot in the frame, at exactly the same size, at the same angle, with his whole head and shoulders fully visible at all times.

The expression is the main action of this shot and it changes over the four seconds. In the first second he wears a cold, closed-mouth stare, the lips pressed together and the narrowed eyes fixed on the viewer. Around the second second the corner of his mouth begins to lift on one side and the upper lip starts to rise. By the middle of the shot that lifted corner has pulled up into a sly, wicked grin, a thin line of his upper teeth shows between the slightly parted lips, the cheek on that side bunches into a hard apple-cheek crease, and his narrowed eyes squeeze a little tighter while staying cold and calculating. From the third second onward the grin slowly eases back down until his mouth is closed again and he is back to the cold stare he started with. His face must visibly travel through this arc, and the expression at the end must match the expression at the beginning.

His mouth stays nearly closed the whole time and never opens wide, he never shows his lower teeth, and he never turns into a friendly or warm smile. He must never look tired, sleepy, drowsy, weary, exhausted, bored, sad or blank. Keep everything else exactly as in image 1: the iron helmet with the short red plume, the forehead band, the neck guard, the leather chin strap, the narrow long bandit face, the high cheekbones, the hollow cheeks, the pointed forward chin, the heavy brows, the sallow yellow-olive skin, the sparse crooked wispy mustache, the stray chin stubble, the same skin brightness, the collar and the shoulders.

Uniform green screen background, no scenery, no floor, no shadow, no text, no other characters, no camera movement, no zoom, no cut, no dust and no new equipment. Deliver one 1024x1024 style full frame on the uniform green screen.
```

#### E1 执行结果（2026-10-30，1 次提交成功，**弧线闸门通过**）

| 项 | 值 |
| --- | --- |
| 任务 | `task_9f5129d339fa40089df6997acb2a579c`，一次创建成功（queued→generating→success） |
| 输入 | image = `stageA6_wicked_grin_v1.png`（A6 头肩，脸占画面最大） |
| 成片 | `C:\Users\steam\Videos\doubaoVideo\enemy1012_expression_arc_v4.mp4`，960×960 / 24fps / 97 帧 / 1,443,118 bytes / SHA `edfa21796431b92367b965bca25d0273ca85ac5c475a826def738f0743a99102` |
| 指标口径 | 嘴部区域（y 405–475, x 455–625）相对 **首帧** 的逐像素变化量（>60 记 1），避开静态高光干扰 |
| 序列 | f0–f17: 0→24；f18–f30: 449→3809；f31–f60: 4605→6360（峰值 f58）；f61–f80: 6344→170；f81–f96: 58 |
| 闸门 | 峰值−Q1 = **6176 ≥ 60** ✓；Q3 = **445 < 峰值×0.6 = 3816** ✓；末帧与首帧 diff = 0 ✓（无缝） |
| 对照 v3 | 同法测量为 18–39 无趋势、峰值在 f0 → **不通过** |
| 结论 | 三个改法（①表情分秒时间线 ②删掉钉死脸的负向词 ③把脸写成主动作）**有效**；弧线成立 |

产物：`Library/Locus/tmp/enemy1012_expression_arc_v4/`（`sheet6.png` 六帧对照、`raw_frames/`、`arc_metrics.json`）。

#### E2 规格（待审批）

- 输入：image = `stageA6b_fullbody_v1.png` 的绿幕版本 `C:\Users\steam\Pictures\gptGen\enemy1012_idle_v6\input_identity_v12_bandit_idle_v6_1024.png`
- 参数同 E1（seedance-2-fast / 4s / 720p / 1:1 / seed −1 / 无音频 / 1 次）
- 改动：**身体动作压到最低**（只留头部微点动与极轻重心移），把运动预算让给脸；并加一句 `the face must still travel through that arc even though the face is small in the frame`
- 失败时兜底：把 E1 头部片当 `reference_video` 传给全身任务（风险：模型可能连头部特写构图一起抄）

#### E2 执行结果（2026-10-30，1 次提交成功，**弧线闸门通过**）

| 项 | 值 |
| --- | --- |
| 任务 | `task_84cd881014814f01a5267a32245c3be7`，一次创建成功（排队较久：12 次轮询 92s 仍 queued，第二轮 85s 仍 queued，第三轮单查 success） |
| 输入 | image = `C:\Users\steam\Pictures\gptGen\enemy1012_idle_v6\input_identity_v12_bandit_idle_v6_1024.png`（A6b 全身绿幕图） |
| 词 | 同 E1 的时间线写法；**身体动作压到最低**（只留头部微点动 + 极轻重心移）；加 `the face must still travel through that arc even though the face is small in the frame` |
| 成片 | `C:\Users\steam\Videos\doubaoVideo\enemy1012_idle_arc_v4.mp4`，960×960 / 24fps / 97 帧 / 1,795,409 bytes / SHA `fca595881d3b5d39282a0e21736cb1c0619c897cc402a68cd3130a72d3e4cf11` |
| 指标 | 先用头盔带做**垂直对齐**（偏移 −5…+1），再量嘴部带（y 300–370, x 390–500）相对首帧的差异：Q1=784 → 中段 1279 → 峰值 f68=1782 → 末段 953；闸门 rise **998 ≥ 60** ✓、fall **953 < 1069** ✓；末帧与首帧差异 **0** ✓（无缝） |
| 主体稳定 | bbox minx 168–176、miny 182–188、maxx 723、maxy 839（恒定）✓ |
| G2 | 背景绿全片 G 140–143，ΔG −16…−18（与 v1/v2 同档，比 v3 好） |
| 目视 | 六帧对照确认：f0 闭嘴冷脸 → f16 嘴角起 → f32 成笑露齿 → f48/f64 满笑露齿 → f96 收回冷脸 |

#### E2 抠图 / 选帧 / 落位 / 接入

- 抠图：SVL Chroma T=79 / softness 16 / halo 1（输出目录与 E1 的 keyed 目录同名，E1 的抠图结果被覆盖；E1 原始帧仍在 `raw_frames/`）
- 选帧：**f0 / f16 / f32 / f48 / f64 / f96** —— 刻意把弧线的**冷脸起点、成形、峰值、回冷脸**四段都放进 6 帧；帧间差 2.17–4.25、尾→首 2.44
- 落位：k=0.92744（960→890），单一偏移 x=**106**；入 1108² 后脚底 **897 全同**、中心 x 518.5–522、高 603–609
- 接入：覆盖 `Assets/Sprites/Enemy/Enemy1012/Enemy_1012_idle1..6.png`（.meta 保留）；`.anim` 无需改
- Unity 实读：meshMinY 六帧全 −21.708 → 脚底世界 Y **−1.8044**；世界高 3.166–3.200u；clip length **0.6**、loop=true、6 键 t=0/0.1/…/0.5
- 备份：`Library/Locus/tmp/backup_Enemy1012_idle_v4_20261030-182736/`
- 预览：`Library/Locus/tmp/enemy1012_expression_arc_v4/idle_preview_v4.gif`、`deployed_heads.png`、`E2_head_sheet6.png`、`E2_expression_metrics.json`

#### 经验固化（可复用）

1. **表情要写成时间线**，不能写成"保持 + 加深"；
2. **负向词里针对脸的"绝不"会把脸钉死**，只保留不可违反的四条（嘴不张开 / 不露下齿 / 不变友善笑 / 不疲惫困倦）；
3. **把脸写成镜头里的主动作**，并把身体动作压到最低，把模型的运动预算让给脸；
4. **验收必须量化**：用"相对首帧的像素差异"量弧线，别用牙齿计数（会被静态高光淹没）；全身版还要先做**头部垂直对齐**再量，否则头部点动会污染信号。

#### v4 关键帧抽取（用户验收「v4 表情合格」后执行）

先出密集头部条（每 4 帧一张，25 张）读出弧线结构：**f0–f12 冷脸 → f16–f28 笑起露齿 → f32–f72 满笑 → f76–f88 收敛 → f92–f96 回冷脸**。

最终 6 帧（**覆盖六个互不重复的表情状态**，且末帧回到首帧状态以保证无缝）：

| 部署帧 | 源帧 | 表情状态 | 帧间差 |
| --- | --- | --- | --- |
| idle1 | f000 | 冷脸、嘴闭、眉压 | — |
| idle2 | f016 | 嘴角开始单侧上抬（唇缝初开） | 3.66 |
| idle3 | f028 | 窄笑、露出一线上齿 | 1.51 |
| idle4 | f048 | 满口坏笑（峰值，齿线最宽） | 3.41 |
| idle5 | f076 | 收敛中（牙线收窄、嘴角回落） | 2.03 |
| idle6 | f096 | 回到冷脸（与 idle1 同状态） | 1.77 / wrap 2.44 |

- 与上一版选帧（f0/f16/f32/f48/f64/f96）相比：去掉了重复的峰值帧（f48 与 f64 同为满笑），补进了 **f076 收敛帧**。
- 落位：k=0.92791（960→891），单一偏移 x=**107**；入 1108² 后脚底 **897 全同**、中心 x 520.5–523、高 604–607
- Unity 实读：meshMinY 六帧全 −21.708 → 脚底世界 Y **−1.8044**（地面 −1.8）；世界高 3.172–3.195u；clip length **0.6**、loop=true、6 键 t=0/0.1/…/0.5
- 备份：`Library/Locus/tmp/backup_Enemy1012_idle_v4_20261030-182736/`（第一版选帧）、`backup_Enemy1012_idle_v4b_20261030-183128/`（本版选帧，覆盖前）
- 预览：`Library/Locus/tmp/enemy1012_expression_arc_v4/idle_preview_v4.gif`、`deployed_heads.png`、`cand_new.png`、`head_strip_dense.png`

#### 播放速度：1.5× 放慢（用户要求）

用户反馈「播放速度太快了至少放慢1.5倍试试」。实现方式：**改 clip 的关键帧时间**（不重做素材、不换图）。

| 项 | 改前 | 改后 |
| --- | --- | --- |
| 关键帧时间 | 0 / 0.1 / 0.2 / 0.3 / 0.4 / 0.5 | 0 / 0.1733 / 0.3467 / 0.52 / 0.6933 / 0.8667 |
| Unity 读回 length | 0.6 | **0.9**（controller 里也读到 0.9） |
| loopTime | true | true |
| 帧内容 | 同一套 6 帧（冷→起→窄笑→满笑→收敛→冷） | 不变 |

**再放慢 2×（用户：「还是太快了，至少再放慢一倍」）**：同一手法再拉一次，周期 0.9s → **1.8s**，关键帧 0 / 0.3533 / 0.7067 / 1.06 / 1.4133 / 1.7667，Unity 与 controller 均读回 **1.8**、loop=true。

| 阶段 | 周期 | 每帧时长 | 相对最初 0.6s |
| --- | --- | --- | --- |
| 初始 | 0.6s | 0.1s | 1× |
| 1.5× 放慢 | 0.9s | 0.1733s | 1.5× |
| 再 2× 放慢（当前） | **1.8s** | 0.3533s | **3×** |

**代价（需知悉）**：6 个姿态摊到 1.8s → 每姿态停 0.35s，观感会偏“定格跳”，不如多帧顺滑。若还要更快地读出表情变化，下一步应改为**从源视频取 12–18 帧**（新增 `Enemy_1012_idle7..12.png` 等 + 12 键 clip），帧数上去了每步位移就小。

**坑（与 `plan/enemy1012-asset-pull-20261026.md` §4 同源）**：用 API 改完曲线并把 `stopTime` 设为 0.9 后，Unity 读回的 length 仍是 **0.7833**（= 末帧 0.75 + 1/30），也就是**导入产物按曲线末尾重算、不吃 stopTime**。解法：把末帧关键帧反推到 `目标周期 − 1/frameRate`（0.9 − 1/30 = 0.86667），六帧均分，Unity 读回即为 0.9。（**不要**删 .meta 强刷——那是给贴图用的，删 .anim 的 .meta 会重新生成 GUID 并打断 controller 引用。）

**连带影响**：`Enemy_1012.controller` 里 Idle→Attack 的 `exitTime 0.75` 不变，但 Idle 周期变长 → 攻击起手从 **0.45s 推到 0.675s**。

预览：`Library/Locus/tmp/enemy1012_expression_arc_v4/idle_preview_v4_slow.gif`（173ms/帧，与工程一致）。

## 8. 定稿结论与移交（2026-10-30）

**用户验收**：v4 表情合格；保持 6 帧；播放速度再放慢一倍（周期 0.9s → 1.8s）；1011 形象研发**暂停**；下一步转 1051 新形象。

**1012 当前定稿产物**：

| 项 | 值 |
| --- | --- |
| 静帧（形象） | `C:\Users\steam\Pictures\gptGen\enemy1012_bandit_face_v3\stageA6b_fullbody_v1.png`（SHA `fcd54ab3…c8f53e`） |
| 头肩基准 | `C:\Users\steam\Pictures\gptGen\enemy1012_bandit_face_v3\stageA6_wicked_grin_v1.png`（SHA `75c780df…844f1`） |
| idle 动画源片 | `C:\Users\steam\Videos\doubaoVideo\enemy1012_idle_arc_v4.mp4`（SHA `fca59588…3e4cf11`，任务 `task_84cd8810…`） |
| 头部表情研究片 | `C:\Users\steam\Videos\doubaoVideo\enemy1012_expression_arc_v4.mp4`（SHA `edfa2179…3a99102`，任务 `task_9f5129d3…`） |
| 工程内 idle 精灵 | `Assets/Sprites/Enemy/Enemy1012/Enemy_1012_idle1..6.png`（.meta 保留，GUID/导入设置不变） |
| clip | `Assets/Animations/Enemy_1012_Idle.anim`：6 键 0/0.3533/0.7067/1.06/1.4133/1.7667，Unity 读回 length **1.8**、loop=true |
| 脚底 | meshMinY 六帧全 −21.708 → 脚底世界 Y −1.8044（地面 −1.8），prefab `visualYOffset = 0.001` 不变 |
| 剩余遗留 | ① idle→attack 仍会有兜鍪↔旧宽檐盔跳变（其余 64 张动作帧未重做）；② 1012 仍未接入任何波次/关卡；③ 场景里的测试立牌 `Enemy_1012_IdleTest` 仍在（可删） |

**本轮方法论已写入本文 §0.4.9，可直接被 1051 复用**：表情写时间线、负向词只留 4 条硬约束、把脸写成主动作并压低身体动作、验收用相对首帧像素差异 + 头部垂直对齐。

---

## 0.2 测试单位接入（只循环 idle，用于试看）

用户验收 v2 后要求「接入，制作一个只有 idle 循环不会做其他动作的单位试看效果」。

### 0.2.1 新增资产

| 资产 | 内容 |
| --- | --- |
| `Assets/Animations/Enemy_1012_IdleTest.controller` | 只含 **1 个状态 Idle**（motion = `Enemy_1012_Idle`，speed 1，clip loopTime=true、length 0.6）；**anyStateTransitions = 0、entryTransitions = 0、parameters = 0** → 只会原地循环 idle，不会触发任何其他动作 |
| `Assets/Prefabs/Test/Enemy_1012_IdleTest.prefab` | 组件仅 Transform + SpriteRenderer + Animator（**不带 Enemy / BoxCollider / EnemyHealthBar**，因此不会被波次、战斗、伤害逻辑接管）；root localScale `(0.08316832, ×3)`，SpriteRenderer 默认图 `Enemy_1012_idle1`、sortingOrder 0，SR localPosition.y = **0.0044** |

### 0.2.2 场景实例

`Assets/Scenes/Battle.scene` 根下新增 `Enemy_1012_IdleTest`，position **(1.7, 0, 0.2)**，紧邻玩家右侧（相机 perspective / FOV 60 / pitch 18°，z≈0 处水平半宽约 2.9 单位，x=1.7 在画面内）。场景已保存。

### 0.2.3 实测

- 脚底世界 Y = **−1.8054**（敌人地面基准 −1.8，Δ 0.0054 ≈ 0.5px）→ 与其他敌人同一地线
- 世界高 3.1947u（与 6 帧新 idle 的 3.166–3.195 一致）
- 控制器：1 状态 / 0 过渡 / 0 参数 / clip 循环 ✓
- Game 视图截图已确认：单位在画面右侧正常站立、兜鍪与含胸持刀姿态可见（截图存于 `Library/Locus/Screenshots/`）

### 0.2.4 移除方式

在 Hierarchy 里删掉 `Enemy_1012_IdleTest`（或删掉 `Assets/Prefabs/Test/` 与 `Enemy_1012_IdleTest.controller`）即可；它不参与任何战斗逻辑。

### 0.2.5 工作区变更说明

本任务新增：上表两个资产 + Battle.scene 里一个根对象。其余工作区变更（14 个敌人 prefab 的纯行尾差异、`Battle.scene` 与 `HeroHUD*` / `HeroHudLayoutTool.cs` / `BuffIcon.cs` / `HeroHUD.cs` 的内容修改）**不是本任务产生的**，属用户正在进行的英雄 HUD 工作。


`git status` 另有一批变更：14 个敌人 prefab 为**纯行尾差异**（`git diff` 无内容差异）；`Assets/Scenes/Battle.scene`、`Assets/Prefabs/UI/HeroHUD_Zhangfei.prefab`、`Assets/ScriptableObjects/Warrior/HeroHUDSkin_Zhangfei.asset`、`Assets/Scripts/Editor/HeroHudLayoutTool.cs`、`Assets/Scripts/UI/BuffIcon.cs`、`Assets/Scripts/UI/HeroHUD.cs` 为用户正在进行的英雄 HUD 工作。**本任务一律未触碰**。


---

## 0. 执行结果摘要（2026-10-30）

| 阶段 | 结果 |
|---|---|
| Stage 1 视频 | 任务 `task_5ebe308d10c14a5faba029f5fb2d6f43`，queued → generating → success；成片 `C:\Users\steam\Videos\doubaoVideo\enemy1012_bandit_idle_v1.mp4`，960×960 / 24fps / 97 帧 / 4.042s / 1,775,120 bytes / SHA `be5f833296ae109aa9cda0ca626a33de3305840d3731ccaf8de48f1be24f2a30`；API 未返回 usage/cost |
| Stage 2 抠图 | SVL Chroma T=79 / softness 16 / halo 1，97 帧全抠；半透明 0、幕布残留 0、孔 0；主体 bbox 高 650–659、宽 544–553、中心 x 444.5–449.5、底 838（恒定） |
| Stage 2 选帧 | **f18 / f26 / f34 / f42 / f50 / f58**：覆盖一个完整呼吸周期（周期 ≈38 帧），帧间差异 2.5–3.8、尾→首 2.5（与帧间同量级，无缝） |
| Stage 2 落位 | 单一全局缩放 k=0.92508（960→888）+ 单一贴图偏移；入 1108² 后脚底 **897 全部一致**、中心 x 519–523.5、高 601–611（与旧帧 601–618 同区间） |
| Stage 3 接入 | 覆盖 `Assets/Sprites/Enemy/Enemy1012/Enemy_1012_idle1..6.png`（保留 .meta，GUID/pivot/PPU 不变）；**`.anim` 无需修改**（同一批 GUID）；Unity 实读 clip length **0.6**、6 键 t=0/0.1/…/0.5 → idle1..6 |
| 脚底验证 | 新帧 meshMinY = **−21.708**（6 帧全同）→ 脚底世界 Y = **−1.8044**（地面基准 −1.8，Δ=−0.0044）→ prefab `visualYOffset = 0.001` **仍然有效，无需改 prefab** |
| 高度 | 新 idle 世界高 3.155–3.206u（旧 idle1 3.166u，1011 idle1 3.605u）→ 与旧帧几乎一致，1012 仍比 1011 矮约 12%（沿用既有结论） |
| 备份 | `Library/Locus/tmp/backup_Enemy1012_20261030-153143/`（旧 6 帧 + deploy_report.json）；**脚本默认把备份写到 `Assets/` 内部，已立即移出并删除残留 `Assets/Library.meta`** |
| 预览 | `Library/Locus/tmp/enemy1012_idle_v4/idle_preview.gif`（6 帧 @10fps）、`contact_sheet.png`、`head_zoom.png` |
| 未做 | 未接任何波次/关卡（1012 至今未入关卡）；未跑 Play Mode 自检；其余 64 张动作帧未动 |

### 0.1 G2 输出片基实测（逐帧，四角 64×64）

- 背景绿中位数：全片 **G 139–143**（R≈0，B≈56–58），与基准键 `(0,159,62)` 的 **ΔG = −16 … −20**；首帧 G≈142–145、末帧 G≈139–141，最差帧 **f012（G=139，ΔG=−20）**。
- 每帧内部四角 std 仅 **0.2–1.7**（帧内均匀，无空间渐变）；时间方向 G 由 ~143 缓降到 ~139 后稳定。
- **与项目历史规律一致**：成片片基系统性低于输入绿 5–20 且随时间变化，输入端统一色键无法锁死。**此为待用户决策项**（接受 / 换参考重生成 / 只做交付物侧处理），未自动改色、未重生成。
- 主体位置稳定：bbox min x 166–178、min y 178–188、max x 721–722、max y 839（恒定）、高 651–661 → 镜头锁定、无整体平移。


---

## 1. 需求与范围（用户已定，2026-10-30）

| 项 | 决定 |
|---|---|
| 制作范围 | **只做 idle**（6 帧），先验证；其余 64 张（walk/attack/hit/dead/击飞链）暂不动 |
| 产出路线 | **路线 A：视频**（绿幕静帧 → Seedance → 抠图 → 选帧 → 落位 → 接入） |
| 分工 | **全链路由本侧完成**：视频词与提交、抠图、选帧、落位、接入、验收 |

### 1.1 为什么必须新做

- 旧 idle 来自 `sword_enemy_v4_combat_idle_loop_v1_720p_4s.mp4`（站姿、举剑剪影），与新定稿的「兜鍪 + 含胸蓄势 + 刀斜举 + 空手握拳」完全不同形。
- 旧 idle 的头部是宽檐钢盔，新形象是汉末兜鍪；即使只换盔，剪影也会跳。
- 新形象本身（头更小、躯干更宽、重心更低）已改变角色的整体剪影语言。

### 1.2 必须提前知晓的连带问题（本轮不解决）

`Assets/Animations/Enemy_1012_Attack.anim` 的**首帧与末帧都是 `Enemy_1012_idle1.png`**（同一 guid 在 attack 内出现 4 次）。因此替换 idle1 会同时改掉 attack 的入场/收势帧，而 attack 中间 16 张仍是旧盔旧站姿：

- **idle ↔ attack 切换会出现「兜鍪 ↔ 宽檐盔」跳变**，这是本轮范围选择的直接代价，必须让用户知悉；
- 1012 prefab 目前**未接入任何波次/关卡**，实机不可见，回归风险低；
- 后续若要彻底统一，仍需整批重做（另立交接）。

---

## 2. 现状实测（工程事实，2026-10-30 读取）

| 项 | 实测值 |
|---|---|
| Idle clip | `Assets/Animations/Enemy_1012_Idle.anim`，6 个 sprite 键 @ t=0/0.1/…/0.5，length 0.6s，loopTime=1，sampleRate 30（有效 10fps） |
| 画布与导入 | 1108×1108，PPU 16，Point，alphaIsTransparency，spriteMode Single，maxTextureSize 2048 |
| pivot | meta 写 (0.65392774, 0.5487343)，**Unity 实读为画布中心 (554,554)**（导入产物陈旧坑；删 meta 重导入会变，visualYOffset 需重算） |
| 脚底基准 | idle 6 帧画布内脚底全 = 898；prefab `visualYOffset = 0.001`（按 1012 idle1 修过，不是 1011 的 0.1388615） |
| 世界高度 | 1011 idle1 = 3.602u，1012 idle1 = 3.166u（−12%，因 1011 idle 举剑高） |
| Prefab | `Assets/Resources/EnemyPrefabs/Enemy_1012.prefab`，enemyId 1012，Animator = `Enemy_1012.controller`（10 状态） |
| Controller | Idle 的 transition exitTime 0.75 / 0.9 / 1，duration 0 / 0.1；**改 Idle 时长会改 idle→attack 的触发时刻** |
| 既有命中帧 | 1012 的命中帧是 `Enemy_1012_attack6`（不是 1011 的 attack8） |
| 制作配方 | `Enemy1012ArtSource/README.md`：视频 seedance-2-fast / 4s / 720p / 1:1 / 无音频 / seed −1；抠图 SVL Chroma T=79 / softness 16 / halo 1 / 半透明转不透明；落位 1108²、脚底 898、中心 x=521、pivot 0.6539/0.5487、PPU 16 Point |
| 历史坑 | 一个动作只允许生成一次（叠轮锐度 3.71→3.33→3.14 逐轮下降）；旧 idle 源片是「举剑站姿」，当动作参考会把模型拉回站姿 |
| 工具链 | SVL checkout `C:\sprite-video-lab` + venv python 3.10 可用；项目脚本 `Locus/tools/svl-matting/{svl_matte_batch,svl_scan_tolerance,svl_deploy_to_unity,green_guarantee}.py`；本机**无 ffmpeg**，视频解码用 PyAV 18.1.0 |
| 凭据 | `REACH_API_KEY`（视频）与 `MUSK_API_KEY`（图片）均已在环境变量，均未写入任何文件 |

### 2.1 规范依据

`design/pixel-character-and-enemy-animation-spec.md` §5–§9：固定画布、锁定镜头、脚底锚点不漂移、禁止整体平移、只允许局部姿态变化；Idle 语义为「双脚站稳、正面朝向玩家、武器自然持握，可有轻微呼吸或头巾变化，但不能迈步、攻击或产生尘土」。

---

## 3. 路线选择与判断

| 路线 | 判断 |
|---|---|
| A 绿幕静帧 → 视频 → 抠图 → 选帧 → 落位 → 接入 | **采用**。与 1012 现有 70 张同链路，衣物/刀的次级运动有真实细节 |
| B 程序化 idle（单张静帧 + 代码微动） | **兜底**。零视频成本、绝不改脸改盔，但无次级运动，像「会呼吸的立绘」。若 A 的成片改盔/改脸/改姿态，立即转 B，**不在视频上叠轮重生成** |
| C 图像模型逐帧生成 | 不采用。历史 §23 / §26 已多次证伪 |

### 3.1 关键取舍：动作参考视频用不用

1012 v11 的替换成片用的是「身份图 + **旧动作源片当 reference_video**」。但旧 idle 源片是**站姿举剑**，作为动作参考会把模型拉回站姿，与本轮要的含胸蓄势冲突。

**决定：只用新身份图作 reference_image，不传旧动作视频**，由提示词精确描述动作内容。代价是动作节奏由模型自行把握；收益是姿态不会被旧片带偏。若失败且用户批准，再考虑加旧片作动作参考重试（需新的付费授权）。

---

## 4. 规格

### 4.1 视频请求（Stage 1）

```text
model: seedance-2-fast
duration_seconds: 4
resolution: 720p
aspect_ratio: 1:1
generate_audio: false
watermark: false
seed: -1
input.content: [text, image_url(reference_image)]
read/upload limits: 单文件 ≤ 50 MiB
创建次数: 1（不自动重试；失败不重建收费任务）
```

参考素材（唯一）：

```text
C:\Users\steam\Pictures\gptGen\enemy1012_idle_v4\input_identity_v12_bandit_idle_1024.png
SHA-256 e97b50b46f2c27e03752e7b6e5a2ad339c3500cb7a35f1d518d64f8e64202643
1024×1024 RGB，绿幕 RGB(0,159,62)
```

G1 实测：四角 64×64 背景中位数 `(0,159,62)`，std `(0,0,0)`，min=max=`(0,159,62)`；主体 bbox (180,200,770,895)，主体高 696、脚底 895、中心 x=475.0（与旧身份图 `input_identity_v11_696.png` 的高 696 / 脚底 895 / 中心 475.5 同规格）。

### 4.2 动作内容（写进提示词）

允许：胸廓呼吸起伏 1–2px；重心在前后脚之间微移 1–2px；刀臂轻微颤动但刀不抬；红缨与腰间红布轻摆；头部极小幅度点动。

禁止：迈步、整体位移、镜头移动、抬高刀刃、攻击动作、尘土、背景变化、增加人物、变盔、变脸、变武器、变服装。

必须无缝循环：末帧回到与首帧相同的姿态。

### 4.3 抠图与落位（Stage 2）

```text
抠图: Locus/tools/svl-matting/svl_matte_batch.py --src <frames> --label Idle
      配方 Chroma + 自动背景色 + 容差 79 + softness 16 + halo 1 + 半透明转不透明
      禁止 softness=0、先平滑、全源统一容差
选帧: 6 帧，覆盖一个完整呼吸循环且首末衔接；避免选到运动模糊帧
落位: Locus/tools/svl-matting/svl_deploy_to_unity.py --foot-line 898（先干跑核对偏移跨度）
      1108×1108 / 脚底 898 / 中心 x=521 / PPU 16 / Point / alphaIsTransparency
```

### 4.4 Unity 接入（Stage 3）

1. 替换 `Assets/Sprites/Enemy/Enemy1012/Enemy_1012_idle1..6.png`（保留 .meta，GUID/pivot 不变；脚本自动备份到 Assets 之外）；
2. `Enemy_1012_Idle.anim` 的 sprite 引用换成新 6 帧，**关键帧时间保持 0/0.1/…/0.5 不动**；
3. 用 Unity 实读 clip length 验证（磁盘 yaml 不可信，见 §2 的导入产物陈旧坑）；
4. 按新帧脚底重算 prefab `visualYOffset`（蹲姿主体变矮，脚底世界 Y 会变）；
5. 实机验收：idle 循环无滑动、idle↔attack 跳变程度、与其他敌人同排高度。

---

## 5. 颜色门槛（G1/G2/G3）

- **G1 输入片基**：已过（四角 std=0，目标绿 `(0,159,62)`）。
- **G2 输出片基**：完整解码后逐帧量测四角 64×64 空背景（先确认该范围全程无主体），给出中位数、与基准键 `(0,159,62)` 的 ΔR/ΔG/ΔB、最差帧号、首尾帧数值、与上一批同项是否同向同量级。**已知历史规律：成片 G 系统性比目标低 5–20 且随时间变化**，这属于待用户决策项，不自动改色、不自动重生成。
- **G3 交付物颜色**：抠图后在中性底上与同批基准/身份参照并排比对主体颜色；未过不得写「颜色已修复」，也不得批量入库。

---

## 6. 失败处理

- 上传失败 / 创建失败 / 轮询超限 / 下载校验失败：**停止并报告**，不重建收费任务；
- 轮询上限：12 次、总等待 ≤ 120 秒、单次请求 ≤ 20 秒；
- 下载先写 `.part`，断点续传；校验 ftyp、顶层 box 边界、完整长度后才改名；
- 成片若改盔/改脸/改姿态：转路线 B，或在用户批准下另立版本重生成，**不叠轮**。

---

## 7. 阶段验收

| 阶段 | 闸门 |
|---|---|
| Stage 1 视频 | 技术成功（容器可解码、帧数/时长/尺寸符合）｜背景时序稳定（G2 数值）｜主体色彩连续｜用户美术验收（头形/脸/装备未变、姿态是含胸蓄势、动作幅度小、无缝循环）——四项分开报告 |
| Stage 2 抠图落位 | Alpha 干净、无半透明残留、幕布残留 0、毛刺 0；逐帧脚底 898±2、中心 x=521±4；与旧 idle 并排看尺度一致 |
| Stage 3 接入 | clip length 与帧键实测正确；prefab 脚底对齐；实机 idle 循环无滑动、无跳动；已知 idle↔attack 盔跳变被记录在案 |

---

## 8. 仍未授权

- 重做 walk/attack/hit/dead/击飞链；
- 修改 1011 或其他敌人；
- 改 `Enemy1012ArtSource/` 内的既有溯源文件；
- 未经用户验收就把新 idle 接入任何波次/关卡。
