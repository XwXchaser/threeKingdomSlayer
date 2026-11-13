---
id: kd_d5aa8b1b-7568-4f22-a613-c0f86d29aff7
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 1051 弓手 · 剩余动作美术生成方案（7 套 / 36 帧）

> **状态：方案稿，尚未提交任何付费生成（2026-11-09）。**
> **范围**：只覆盖**美术生产**——形象/姿势参考图 → 关键帧母片 → 视频 → 抠图 → 选帧 → 落位 → 台账。
> **Unity 接入（`.anim` / prefab / 波次 / Editor / Play Mode）不在本方案范围内**，本文件只把「Clip 需要多少帧、什么节奏」作为美术侧的交付规格给出。
> 上游文档：`plan/enemy1051-bandit-archer-redesign-handoff.md`（形象定稿 + 三套已验收动作）、`plan/enemy1051-archer-shooting-video-plan.md`（射箭路线与失败史）。

---

## 0. 结论摘要

1. 1051 的**形象 + idle / walk / attack 三套动作已验收**（18 帧），素材在 `Enemy1051ArtSource/`，尚未进 `Assets/`。
2. 要让 1051 成为一个完整可用的敌人，还缺 **7 套动作共 36 帧**：受击 3 套、击飞 3 套、死亡 1 套。
3. **全部走「关键帧母片」路线**，不走纯文字视频（纯文字已被证明会重新定义动作：射箭 v1 瞄准方向被改到画面右侧）；但**母片的姿势图只由身份图 + 文字生成**，不上传动作参考图（用户 2026-11-09 定）。
4. 姿态语义**只从工程内 1011/1012 的成品帧「读」出来写成词**——`memory/enemy1011-animation-pose-reference.md` 明确写这批动作是「所有敌人可复用的通用动作基线」，1012 也带齐了 `hitLeft/hitRight/dead/rise/landing/getup`。
5. **用户已拍板的三条资产规则**（2026-11-09）：① **Dead 弓脱手**；② **Rise 弓不脱手**；③ **箭只在射箭时存在**，受击/击飞/死亡时双手干净，画面左侧的手始终空着。
6. 每套 1 支视频 + 3~4 张姿势图；**合计 7 支视频、约 24 张姿势图**。
7. 每套 6 帧、1108² 透明底、本体高 605 / 脚底 897 / 中心 x 521 / pivot 0.5,0.5（PPU 16），半透明 0 / 幕布残留 0。

---

## 1. 不可动的基线（每一套生成都必须一致）

| 项 | 值 | 来源 |
|---|---|---|
| 形象 | 三国民兵弓手（带装备）：粗麻短衣 + 破旧深棕皮背心 + 皮护腕 + 束腰带铁扣 + 左腰带鞘短刀 + 暗红布条 + 深色布裤 + 腿绑 + 露趾绑脚鞋 | `Enemy1051ArtSource/README.md` |
| 头部 | 额头窄布带 + 头顶束发髻 + 颈后布尾 + **下半脸被布巾遮住（无嘴、无牙）** | 同上 |
| 脸 | **眉用力下压的怒视**（用户 2026-11-08 已定接受，不做眉眼精修）；双眼窄、正视玩家 | handoff §9.5 / shooting-plan §11.1 |
| 手持 | **弓在左手（画面右）**、右手空手（射箭时才握箭）；肩后箭袋 3 支白羽箭 | shooting-plan §3 |
| 体量 | 短壮、宽肩短腿；本体高 605（1108² 口径），与 1011 之比 0.992（同族成立） | handoff §9.3 |
| 落位 | 1108² / 透明 / 本体高 **605** / 可见脚底线 **897** / 身体中心 x **521** / pivot **0.5,0.5** / PPU 16 | 三套台账 |
| 画风 | 高清像素画风 + 脏灰麻 / 棕皮 / 一处暗红 / 少量冷铁的冷暖关系 | `design/art-style-guide.md` |

> **横向下包络**：1051 总宽 432 px = **1.58 个阵型列距**（弓握右手侧、箭袋在左肩）。所有会甩动身体的动作（受击、击飞、死亡）里，弓的摆幅必须受控，否则横向跨度会进一步变大。

---

## 2. 缺口清单（7 套 / 36 帧）

| # | 动作 | 帧 | 语义拍段 | 交付节奏（30fps） | 循环 |
|---|---|---|---|---|---|
| 1 | **HitFlash** 正面受击 | 6 | 进入 → 峰值 → 回收 → 回 idle | 0 / 0.15 / 0.3 / 0.45 / 0.6 / 0.75（总 0.9s） | 否 |
| 2 | **HitLeft** 左受击 | 6 | 画面左来力 → 上身向画面右反冲 → 回收 | 同上 | 否 |
| 3 | **HitRight** 右受击 | 6 | 画面右来力 → 上身向画面左反冲 → 回收 | 同上 | 否 |
| 4 | **Launched_Rise** 击飞上升 | 6 | 骨盆先被托起 → 空中峰值 | 0 / 0.1 / 0.2 / 0.3 / 0.4 / 0.5（总 0.6s） | 否 |
| 5 | **Launched_Fall** 落地尾段 | 6 | 接地 → 稳定倒地 | 同上 | **是** |
| 6 | **Launched_Getup** 起身 | 6 | 倒地 → 撑起 → 回正 | 同上 | 否 |
| 7 | **Dead** 死亡 | 6 | 失去支撑 → 连续倒下 → 保持终态 | 同上 | 否 |

节奏数值取自工程内 `Enemy_1012_*.anim` 的实测 clip（受击 0.15s/键 · stopTime 0.9；击飞与死亡 0.1s/键 · stopTime 0.6），这样美术产物的节奏与项目既有敌人一致。

**合计新增 36 帧；1051 总帧数 18 → 54。**

---

## 3. 姿态依据：只读参考，不上传参考图（用户 2026-11-09 定）

> **2026-11-09 晚更新**：本条对 **Dead 这一套已被用户新决定取代** —— Dead 改走「工程内已部署 Dead 帧 + 1013/1051 并排组图」路线（只有组图本体才上传动作参考），见 `plan/enemy-dead-group-reference-20261109.md`。**受击三套与击飞三支是否同样改走组图，仍待用户拍板**；在拍板前其余六套仍按本条执行。

**决定**：不把任何动作参考图喂给模型。生成时**每套只上传 1051 身份图**；1012 的成品帧与 1011 的动作基线**只用来读姿态、写提示词**。

`Assets/Sprites/Enemy/Enemy1012/` 已包含全套（对照图见 `Library/Locus/tmp/1051_plan/1012_ref_1.png` / `1012_ref_2.png`）——**仅作词义来源**：

| 需要的动作 | 1012 帧 | 读法（迁移到 1051 时要改什么） |
|---|---|---|
| HitLeft | `Enemy_1012_hitLeft1..6.png` | 画面左侧来力，受力肩被压低后撤、对侧肩抬起，上身绕固定骨盆向画面右后方折开；胸肩先动、头延后 |
| HitRight | `Enemy_1012_hitRight1..6.png` | 镜像受力链：受力右肩下压，上身向画面左后方反冲，头延后转向画面左 |
| Dead | `Enemy_1012_dead1..6.png` | 站立结构瓦解 → 重心下降 → 仰面倒地、双手张开；**1012 的剑在 dead3 起脱手飞出** |
| Rise | `Enemy_1012_rise1..6.png` | 骨盆与下腹先被托起，胸腔向上后打开，头延后仰，双臂双腿被惯性甩开；末帧是空中峰值 |
| Fall | `Enemy_1012_landing1..6.png` | 腿髋先接地吸收冲击，躯干头手连续沉下，进入稳定倒地姿势 |
| Getup | `Enemy_1012_getup1..6.png` | 前臂先支撑，膝髋收拢到身体下方，躯干抬起，接近站立后回正 |

**迁移到 1051 时的四条改写（只影响提示词措辞，不影响图像输入）**：

1. **1012 是刀兵，剑在画面左手**；1051 是弓手，**弓在左手（画面右）**。写提示词时只描述**身体姿态与重心分配**，武器一律写 1051 的弓与箭袋，绝不提剑。
2. **Dead：弓脱手**（用户 2026-11-09 定）——弓在倒地过程中脱手、落到身体旁的地面上，**完整留在画布内**，不飞出画外。
3. **Rise / Fall / Getup / 受击：弓不脱手**（用户 2026-11-09 定）——弓始终在左手，被动甩动但不换手、不掉地、不出画。
4. **箭只在射箭时存在**（用户 2026-11-09 定）——受击/击飞/死亡三套**手里没有箭**，画面左侧的手（角色右手）始终空着、干净；画面上不得出现散落的箭羽或被握在手里的箭，但**肩后箭袋与三支白羽箭照旧保留**（身份的一部分），不得散成一捆。

> **风险（路线变更带来的）**：不再有动作参考图兜底，**姿势正确性从"母片继承"前移到"生图阶段"**——gpt-image 要凭空画出倒地/被挑飞的姿态，比照抄参考帧更容易画歪（尤其"失去支撑、连续倒地"这种大姿态）。
> **缓解**：每套**先只出 1 张姿势图**、量测 + 人工过目，再决定是否批量出剩下的；姿态词要按 §6.1 五层骨架写，把"重心、支撑点、关节先后"写成可量描述。

---

## 4. 逐套方案

每套统一走六步：**① 读参考写姿态词 → ② gpt-image 只用身份图出新形象姿势图 → ③ 关键帧母片 → ④ seedance replace 视频 → ⑤ 抠图 → ⑥ 选帧 → ⑦ 落位 + 台账**（①②③ 本地/生图，④ 付费）。

### 4.1 HitFlash 正面受击（6 帧）

- **意图**：正面外力突然命中，上半身短促冲击后回位。摄像机正对角色，"后仰"不能靠整体缩小或左右斜倒表达。
- **姿态依据（只读）**：`memory/enemy1011-animation-pose-reference.md` §3.3 的正面受击读法（肩线上耸内收、颈压、胸腔收紧、膝微屈、手臂与武器被动滞后）及其可复用关键词。**不上传参考图**。
- **关键读点**：肩线向上耸并向内收 → 颈部压短 → 胸腔收紧 → 膝微屈 → 手臂与弓被动滞后 → 回正。
- **1051 专属**：弓在左手，被动甩动但**不换手、不掉地、不出画**；布巾不掀、无嘴形；峰值只能靠眉眼（压眉 + 眯眼）承担。
- **母片**：首帧 = idle 持弓站姿；末帧 = idle 持弓站姿（回位），中间做一次冲击 → 4.0s 关键帧母片，节奏"进入→峰值→回收"。
- **末帧处理**：末帧直接复用 `1051_idle_identity_greenscreen_1024.png` 的站姿，保证回收无缝。

### 4.2 HitLeft 画面左来力（6 帧）

- **意图**：力从画面左侧进入，打中左肩/上胸；上身向画面右后方折开。强调"被外力打过去"，不是主动侧身摆 pose。
- **关键读点**：受力肩压低后撤 → 对侧肩抬起 → 胸腔先动 → 头延后转向画面右并压入肩 → 回正。
- **1051 专属**：弓在画面右手（左手），受击时**弓会被甩向画面右后方**——这正是 1051 与 1012 最大的差异点，必须在提示词里写明弓的被动位移方向，否则模型会让弓穿过身体或换手。
- **母片**：首帧 = 持弓站姿 → 峰值姿势图 → 回站姿，4.0s。

### 4.3 HitRight 画面右来力（6 帧）

- 4.2 的镜像受力链，**但不能镜像所有约束**：弓在画面右手（左手），身体被推向弓侧，弓的摆幅与包络要单独写（左扩 ≤40px / 右扩 ≤60px 之类按 1051 实际比例换算，不照抄 1011 的像素数）。
- **不要用"把 HitLeft 图镜像"的提示词**——项目已有记录：直接镜像会导致武器换手、刃外甩。

### 4.4 Launched_Rise 击飞上升（6 帧）

- **意图**：外力从下方及略偏前方命中，骨盆与下腹先被托起，胸腔向上略后打开，头延后仰，双臂双腿连同弓与箭袋按惯性依次甩开。末帧是**空中峰值**，不是落地、不是死亡、不是回 idle。
- **关键读点**：脚离地 → 骨盆先升 → 胸腔打开 → 头延后 → 四肢甩开。
- **1051 专属**：**弓不脱手**（用户 2026-11-09 定）——弓始终在左手，可被动贴到身体右后方，但必须完整入框、不换手、不掉地；**手里没有箭**（箭只在射箭时存在），画面左侧的手空着；箭袋白羽保持 3 支、不散成一把。
- **母片**：首帧 = 持弓站姿 → 空中峰值姿势图，4.0s；末帧 = 峰值姿势（接 Fall 段）。
- **与 Fall 的接缝**：Rise 末帧与 Fall 首帧必须是**同一张姿势图**，这是 1011/1012 已验证的做法（`memory/enemy1011-animation-pose-reference.md` §3.8）。

### 4.5 Launched_Fall 落地尾段（6 帧）

- **意图**：从 Rise 的实际末帧继续下落，腿髋先接地吸收冲击，躯干头手连续沉下，进入稳定倒地姿势。**它是活着的击飞落地，不是 Dead**；不站起、不回 idle。
- **母片**：首帧 = Rise 的峰值姿势图（同一张）→ 接地压缩 → 稳定倒地，4.0s；末帧 = 稳定倒姿（与 Getup 首帧同源）。
- **1051 专属**：弓落地时贴着身体，不单独飞出。

### 4.6 Launched_Getup 起身（6 帧）

- **意图**：从倒姿开始，前臂与另一只手先提供支撑，膝与骨盆收拢到身体下方，躯干抬起，头部延后并朝上，接近站立后连续回正，**回到精确的 idle 持弓姿态**。
- **末帧**：直接用 `1051_idle_identity_greenscreen_1024.png` 的站姿 → 保证与 idle 无缝。
- **1051 专属**：起身时弓重新回到左手同一握持位置；箭若还在则仍是**一支**。

### 4.7 Dead 死亡（6 帧）—— 语义是「被打飞」，不是原地塌

> **用户 2026-11-09 纠正**：本项目所有 dead 动画的语义都是**被外力打飞后倒地**（只有 109 系列是原地倒地），不是站在原地软倒。1051 的 Dead 按这个语义做。

- **意图**：站立中挨到致命一击 → 身体被打得后仰离地 → 被打横、腾空 → 落地 → 躺平保持死亡。
- **工程实测（1011 / 1012 的 dead 帧；口径：alpha>128 的整幅前景，主轴为 PCA 主轴与**水平**的夹角）**：

| 帧 | 宽高比 | 填充率 | 主轴（与水平夹角） | 最低点 |
|---|---|---|---|---|
| 1011 idle | 0.92 | 0.47 | 61°（竖直） | 895 |
| 1011 dead1 | 1.06 | 0.40 | 48° | 895 |
| 1011 dead2 | 1.24 | 0.36 | **5°**（已被打横） | 895 |
| 1011 dead3 | 1.55 | 0.30 | 11°（腾空横躺） | **841**（抬离地线 54px） |
| 1011 dead4–6 | 1.44–1.54 | 0.30 | 11°–20° | 819–852 |
| 1012 dead1–6 | 1.18→1.57 | 0.35–0.41 | 7°–16° | 823–896 |

> 交叉验证：`memory/video-generation-case-log.md` 记 1011 dead3 / dead6 的身体主轴为 71.5° / 81.6°（**与竖直**夹角口径，与上表互补）；同一记录里 1021 持盾死亡 v2 的失败主因就是"垂头失重、身形站桩、主轴仅 28.4°（与竖直）"，用户批"动作毫无张力、没有被击飞到底的感觉"。**结论：僵硬不是视频词的锅，是关键姿势图的锅。**

- **1051 关键姿势图的量化目标**（照抄 1011/1012 的成片区间）：
  - 终态躺平帧：**宽高比 1.4–1.6**、**填充率 ≤0.36**、**主轴与水平夹角 ≤25°**、身体最低点贴地线
  - 腾空帧：同样的宽高比/主轴，且**身体最低点抬离地线 40–60px**（这是"被打飞"的读点）
  - 过渡帧（dead2 位）：宽高比 1.2–1.4，主轴已明显离开竖直
- **1051 专属**：**弓脱手**（用户 2026-11-09 定）——被打飞时弓从左手脱落、落在身侧地面上，**完整留在画布内**、不飞出画外、不遮脸；**手里没有箭**，画面左侧的手空着。
- **末帧保持**：末帧是死亡终态并保持（不是受击视频里那种无意的峰值平台）；末帧里弓躺在身侧的地面上。

#### 4.7.1 Dead 套执行卡（批次 A，可直接照做）

**母片需要 5 张姿势图**（P1 直接用身份图，不用生成）：

| 图 | 内容 | 量化目标 | 地面关系 |
|---|---|---|---|
| P1 | 持弓站姿（= 身份图原图） | 宽高比 0.92 / 填充 0.47 / 主轴 61° | 脚底线 895 |
| P2 | **挨到致命一击**：膝弯、重心塌、上身向后仰、头后甩、**弓仍在左手** | 宽高比 ≈1.2 / 主轴已离开竖直（≈40°） | 脚底线 895 |
| P3 | **被打飞**：身体离地、被打得接近水平、四肢被惯性甩开、**弓已脱手在身体右外侧的空中** | 宽高比 1.4–1.6 / 主轴 ≤25° / 填充 ≤0.36 | **最低点抬离地线 40–60px** |
| P4 | **落地**：身体拍在地上、腿还在弹、双臂张开、**弓落在身侧地面** | 宽高比 1.4–1.6 / 主轴 ≤25° | 最低点接近地线 |
| P5 | **终态**：仰面躺平、四肢摊开、**弓躺在身侧地面上**、闭眼 | 宽高比 1.4–1.6 / 填充 ≤0.36 / 主轴 ≤25° | 身体贴地，最低点 895 |

> **P3 是整套的关键帧**：它必须同时读出「身体已被打横、离地」与「弓已脱手在空中」。这一帧画歪（主轴还是竖直的、宽高比还是 0.9），成片就会变成"原地软倒"——1021 持盾死亡 v2 就是这么废掉的。
> **弓的处理**：把弓画成**独立的一个块**，与身体之间留出明显空隙（1021 案例量到的"身体块与盾块最小间距 14px = 脱手成立"就是这个判据）。若成片里弓的姿态始终不理想，还有兜底：**把弓从帧里裁出来做独立贴图单独控制**（用户 2026-11-09 认可）。弓的落点不重要（用户 2026-11-09）。

**通用前缀（每张姿势图都用，只换差异段）**：

```text
A single full-body 2D game sprite on a uniform green screen background (RGB 0,159,62). No scenery, no floor, no shadow, no text, no watermark.

The character is the same Cao army archer from image 1: the same short stocky build, the same wide shoulders and short legs, the same coarse grey tunic with the torn hem, the same worn dark brown leather vest, the same leather bracers, the same square belt buckle, the same short dagger at his belt, the same red cloth strip, the same dark brown cropped trousers, the same pale leg wraps, the same strapped sandals, the same grey headband and the topknot on top of his head, the same grey scarf over the lower half of his face, the same quiver with three white fletched arrows over his right shoulder, the same wooden bow with its wrappings and the red band on the grip. Exactly the same high-definition pixel-art rendering, the same colors and the same lighting as image 1. The same 1:1 canvas, the same body size, the same ground line and the same margins as image 1.

His head faces the viewer square-on and never turns: the headband, the topknot and the scarf over his lower face stay exactly as in image 1. His mouth stays covered by the scarf, so no mouth shape and no teeth are ever drawn under the cloth.

The hand on the left side of the frame is empty: there is no arrow in it, no arrow in either hand, and no loose fletching anywhere in the frame.
```

**P2 差异段（失去支撑）**：

```text
The only thing that has changed is his pose. He has lost his support and starts to collapse: his right knee buckles first, his weight sinks down and slightly toward the left side of the frame, his chest folds forward, his head tips down a little, his shoulders drop, and the bow is still in his left hand but the bow arm has begun to tilt and slip down toward his hip. His face is the same angry face as in image 1, the brows still pressed down, the eyes still narrow slits looking straight at the viewer.

He is not dead yet in this picture, he is only beginning to fall. His feet are still on the ground line. The bow never leaves his hand in this picture.
```

**P3 差异段（弓脱手）**：

```text
The only thing that has changed is his pose. He is collapsing backwards: his knees have folded, his hips are dropping, his torso is falling backward, his left arm has opened and the bow has just left his hand. The bow is now in the air beside him, on the right side of the frame, a little away from his body, tilted at about 45 degrees, still clearly a whole wooden bow with its wrappings and its red band on the grip. The bow is neither in his hand nor on the ground: it is between them, falling.

His right hand, the hand on the left side of the frame, is empty and open. His face is the same angry face as in image 1, the brows pressed down, the eyes narrow.
```

**P4 差异段（接地）**：

```text
The only thing that has changed is his pose. He has just hit the ground: his back and hips are on the ground line, his legs are still folded up and half in the air, his arms are thrown wide. The bow has landed and now lies on the ground beside him, on the right side of the frame, flat on the ground line, a whole wooden bow with its wrappings and its red band on the grip, clearly separated from his hand.

His face is the same angry face as in image 1, the brows pressed down, the eyes narrow. The scarf never moves and no mouth shape is ever drawn under the cloth.
```

**P5 差异段（终态）**：

```text
The only thing that has changed is his pose. He lies flat on his back on the ground line, completely down, his arms open at his sides and his legs stretched out. He is dead: his eyes are closed and lifeless, the brows have relaxed out of the angry shape, and the whole body has gone heavy and still. The scarf never moves and no mouth shape and no teeth are ever drawn under the cloth.

The bow lies on the ground beside him, on the right side of the frame, flat on the ground line next to his hip, a whole wooden bow with its wrappings and its red band on the grip, completely inside the frame and not touching his body.
```

> **P5 的负向词要改一条**：§6.2 的"永不疲惫、困倦、耷拉眼皮"在死亡终态**不适用**（闭眼是死的读点，不是困倦）。Dead 套把这一条换成 "his eyes are closed and lifeless, and the face is not sleeping, it is dead"。

**母片（本地）**：

```bash
python Tools/visual_brief/make_keyframe_clip.py \
  --clip Enemy1051ArtSource/01_SourceVideos/1051_dead_keyframes_6s.mp4 \
  --item "P1.png:1.0,P2.png:1.0,P3.png:1.2,P4.png:1.0,P5.png:1.8" \
  --size 1024x1024 \
  --json Enemy1051ArtSource/01_SourceVideos/1051_dead_keyframes_6s.json
```

> 每拍停留 ≥1.0s、总 6.0s —— 这是反"模型偷懒跳帧"的硬规矩，理由见 §4.8。

**视频提示词**（seedance 2 / 1080p / 1:1 / 6s / replace 路线只传 reference_video）：

```text
Animate this keyframe sequence of the Cao army archer into a smooth sprite animation. The character, his costume, his face, the bow, the quiver and his equipment stay exactly as they are in the sequence, and the animation passes through the same poses in the same order: standing with the bow at his side, struck hard from the front so that his knees buckle and his body snaps backwards, knocked off his feet and lifted sideways until his whole body is almost horizontal in the air, landing flat on his back, and lying dead on the ground. Add only the in-between motion.

The five poses are motion landmarks, NOT five still pictures to display in sequence. Use visible continuous in-between joint articulation between every pair of them. No pose snapping, no hard cuts, no crossfades and no morphing. The body rotates as one piece while it is knocked back: the knees fold first, the hips and the chest swing back next, the head follows a moment later, and the arms and the legs are thrown open by the force and trail behind the rotation. These reactions overlap, they do not all happen on the same frame, and no pose is held as a freeze frame: every intermediate pose stays readable only while it is still moving. The final pose settles with a small damped movement of the limbs and then stays still.

The bow leaves his left hand exactly once, while his body is already horizontal in the air, and it falls separately and ends up lying on the ground next to his hip. The bow never multiplies, never disappears, never flies out of the frame and never returns to his hand. The hand on the left side of the frame stays empty: there is no arrow in either hand and no loose fletching anywhere in the frame.

Locked camera, fixed framing, fixed position and fixed scale: the archer stays in exactly the same spot in the frame at exactly the same size, with his entire body, the entire bow and the entire quiver fully visible at all times and never touching the edge of the frame. His head faces the viewer square-on and never turns: the headband, the topknot and the scarf keep the same front-facing shape in every frame, the brows stay pressed down and the eyes stay narrow until he is lying down, when they close. His mouth stays covered by the scarf, so no mouth shape and no teeth are ever drawn under the cloth.

No second person, no second bow, no extra arrow, no spark, no dust, no glow, no motion blur and no combat effect. Uniform green screen background, no scenery, no floor, no shadow, no text, no labels, no UI, no watermark, no camera movement, no zoom and no cut.
```

**选帧预期**：6 帧应覆盖 P1（站立）→ P2（挨打后仰）→ **P3（腾空横躺 + 弓在空中）** → P4（落地）→ P5（躺平终态）。P3 是必取帧；末帧弓在地上。

**闸门**：① 打成"被打飞"而非"原地塌"（终态宽高比 1.4–1.6、主轴与水平夹角 ≤25°、填充率 ≤0.36）；② 腾空帧身体最低点抬离地线 40–60px；③ 弓有且仅有一次"脱手"（不能中途消失、不能变两把、不能飞出画外）；④ 终态明确是死亡并保持；⑤ 帧内无箭与散羽、无第二件武器。

---

## 4.8 母片反偷懒：为什么一上传关键帧模型就会跳帧，怎么破

> 用户 2026-11-09 提出的核心问题。本节结论全部来自项目实测（1051 射箭四轮 / 行走四轮、1012 失盾三图、1021 持盾死亡三轮）与 `skill/reachapi-seedance-video-generation-v2.md`、`skill/workflows/character-hit-animation-video-workflow.md` §10。

### 4.8.1 实测对照表

| take | 母片关键帧 | 提示词写法 | 结果 |
|---|---|---|---|
| **walk v1**（作废） | **2 张**（旧 105 的两帧走姿） | "the two poses in the sequence **alternate** and the motion between them is smooth" | ❌ **模型只在两个姿势间"啪"地切**，中间过程全丢 |
| **attack v3 / v4**（采用） | **4 张**，每张停 1.15 / 1.4 / 2.0 / 1.45 秒（总 6.0s） | "the animation **passes through the same poses in the same order**: standing… raising the bow and nocking the arrow, pulling the bowstring back… and then letting the arrow go. **Add only the in-between motion.**" | ✅ 平滑；idle→射箭衔接成立；145/145 帧角色一致 |
| **1012 失盾三图**（采用） | **3 张** 普通 reference_image | "The three reference images are **motion landmarks, NOT three still pictures to display in sequence**. Use visible **continuous in-between joint articulation**. **No pose snapping, hard cuts, crossfades or morphing.**" + 6 秒错时重叠节奏表 + "not a freeze frame" | ✅ 用户认可**保留三张图、只改词**就过 |
| **idle**（采用） | 无母片，首帧=末帧=同一张锚图 | 眉眼四拍**分段时间线** + 四条负向词 | ✅ 无缝循环（IoU 0.9965） |

### 4.8.2 根因（四条，全部实测）

1. **关键帧太少、彼此又太像** → 模型只需在两个近似姿态之间插值，"啪"地切最省事。walk v1 的两张是同族走姿，差异只在腿位。
2. **每帧停留太短** → 母片里没有足够时间让模型"演"这一段，只能靠切换交差。
3. **提示词把关键帧说成"要依次出现的静态画面"或"交替"** → 模型照做。walk v1 原文就是 "The two poses in the sequence alternate"，等于直接命令它轮播两张图。
4. **没给段内的运动路径与时间分配** → 模型不知道两帧之间该发生什么，最省事的解法就是切。

### 4.8.3 对策（按实测有效性排序，1051 Dead 逐条落实）

| # | 对策 | 出处 | 1051 Dead 的落实 |
|---|---|---|---|
| 1 | **关键帧 ≥4 张，且相邻两帧形态差异肉眼可见**（站立→挨打后仰→腾空横躺→落地→躺平） | attack 4 帧成功 vs walk 2 帧失败 | 母片 5 张 |
| 2 | **每张在母片里停 ≥1.0 秒** | attack 母片 1.15/1.4/2.0/1.45s | 1.0/1.0/1.2/1.0/1.8s |
| 3 | **提示词逐拍点名 + "passes through the same poses in the same order" + "Add only the in-between motion"** | shoot_v3/v4 原句（已验收） | §4.7.1 视频词 |
| 4 | **加 motion landmark 三句**："NOT … still pictures to display in sequence" / "continuous in-between joint articulation" / "No pose snapping, hard cuts, crossfades or morphing" | 1012 失盾三图已验收句 | 同上 |
| 5 | **禁用"交替 / 轮播"式措辞**，一律改成"顺序经过" | walk v1 反例 | 全词表统一 |
| 6 | **给因果链与错时重叠**：膝先折 → 髋胸后摆 → 头延后 → 四肢被甩开，**不同时发生** | 1012 失盾节奏表 | 视频词第二段 |
| 7 | **不许冻结**：中间姿态 "readable only while it is still moving" | 同上 | 同上 |
| 8 | **尾段阻尼收稳**（damped settling），不要硬停 | 同上 | P5 段 |
| 9 | **兜底路线**：母片仍跳帧 → 改走「首尾帧锚图 + 带数字的规则词」 | skill v6 路线实测：动作与角色**两者都锁得住**（145/145 帧角色一致） | 首=P1 站姿、末=P5 躺平；词里写死"主轴与水平夹角 ≤25°、宽高比 1.4–1.6"这类可量值 |

> **代价**：母片路线每一拍都要先出一张姿势图（生图不贵但要逐张过目）；首尾帧兜底路线省图，但动作幅度靠词约束，历史上出现过放大 4.5 倍的事故（1012 受击 v3）。**1051 Dead 优先母片，兜底留到最后，不并行烧两支。**

---

## 5. 通用生产流程与命令

### Step 1 — 读参考、写姿态词（本地，不计费）

不上传任何动作参考图。做法：

1. 看 `Assets/Sprites/Enemy/Enemy1012/Enemy_1012_{hitLeft,hitRight,dead,rise,landing,getup}*.png`（对照图 `Library/Locus/tmp/1051_plan/1012_ref_1.png` / `_2.png`）与 `memory/enemy1011-animation-pose-reference.md` §3.1–3.8，**只提取姿态语义**：关节读点、重心、支撑点、先后顺序、终态形态。
2. 按 §6.1 五层骨架 + §6.3 专属词表写成英文提示词，落到 `Tools/visual_brief/prompts/1051_<set>_<pose>.txt`（+ `.cn.txt`）。
3. 词里**只描述身体**，武器恒为 1051 的弓与箭袋（见 §3 的四条改写）。

### Step 2 — 新形象姿势图（gpt-image 2.6，**只上传身份图**）

- **唯一图像输入** = 形象真源 `1051_idle_identity_greenscreen_1024.png`
- 姿态完全由提示词承担
- refs 放 `Tools/visual_brief/prompts/refs_1051_<set>_<pose>.json`；提交脚本 `Tools/visual_brief/submit_1051_pose.py` 目前只认 `--version draw/release`，新增动作需要**加新的 version 分支**（本地小活）
- **两种走法（都只上传身份图）**：
  - **(a) 单图编辑**：以身份图 + "改成这个姿态"的指令做 edit。改动小时更稳。
  - **(b) 以身份图为外观参考的重新生成**：把身份图当 appearance reference，姿态完全由词决定。**大姿态（如"倒在地上"）通常走 (b) 更稳**——edit 会保留原站姿构图，改不动。
- **产物必须量测 + 人工过目后再烧视频**：姿势图画错 → 后面全错（射箭 v2 就是这么整支作废的）；本路线没有参考图兜底，这道闸门更不能省

### Step 3 — 关键帧母片（本地）

```bash
python Tools/visual_brief/make_keyframe_clip.py \
  --clip Enemy1051ArtSource/01_SourceVideos/1051_hitLeft_keyframes_4s.mp4 \
  --item "A.png:1.2,B.png:1.4,A.png:1.4" \
  --size 1024x1024 \
  --json Enemy1051ArtSource/01_SourceVideos/1051_hitLeft_keyframes_4s.json
```

### Step 4 — 视频（付费，一次机会，失败不自动重发）

```bash
python Tools/visual_brief/submit_seedance.py \
  --prompt-file Tools/visual_brief/prompts/1051_hitLeft_v1.txt \
  --reference-video Enemy1051ArtSource/01_SourceVideos/1051_hitLeft_keyframes_4s.mp4 \
  --workflow replace \
  --output "C:/Users/steam/Videos/doubaoVideo/1051_hitLeft_v1.mp4" \
  --duration 4 --resolution 1080p --aspect 1:1 --approve
```

**两条硬规矩**（射箭轮踩出来的）：

1. seedance 2 **不允许首尾帧与参考素材混用**（服务端直接 HTTP 400 拒绝，不建任务、不计费）→ 中间关键帧只能走 `reference_video`。
2. replace 路线**不要同时传 reference_image**——母片里的人已经是新 1051，再传身份图会把姿势拉回站姿（射箭 v3 之后修正过一次）。

参数沿用射箭轮已验证配置：seedance-2 / 1080p / 1:1 / 4~6s（1440×1440）。

### Step 5 — 抠图

```bash
python Tools/visual_brief/chroma_key.py \
  --video Enemy1051ArtSource/01_SourceVideos/1051_hitLeft_v1.mp4 \
  --out Library/Locus/tmp/1051_hitLeft_keyed \
  --tol 79 --halo-rounds 2
```

验收：**半透明 0 / 幕布残留 0**（1051 三套已交付帧全部达标）。

### Step 6 — 选帧

- 受击/死亡：按**拍段语义**选（进入 → 峰值 → 回收），参考 `Tools/visual_brief/pick_shoot_frames.py` 的 `--windows` 拍段定义法
- 击飞链：按**相位/阶段**选，参考 `pick_cycle_frames.py`
- **先整片分段看相位尺**再抽帧（walk v4 在 t≈1.0s 有一次步态倒退/重放，闪回被带进动画）

### Step 7 — 落位 + 台账

```bash
python Tools/visual_brief/land_1051_attack.py \
  --keyed Library/Locus/tmp/1051_hitLeft_keyed \
  --pick hitLeft1=f012,hitLeft2=f020,... \
  --out-dir Enemy1051ArtSource/06_HitLeftFrames \
  --json Enemy1051ArtSource/06_HitLeftFrames/1051_hitLeft_landing.json \
  --fixed-k <k> --fixed-offset
```

落位口径与三套已交付帧完全一致（1108² / 本体高 605 / 脚底 897 / 中心 x 521 / pivot 0.5,0.5 / PPU 16）。

---

## 6. 提示词写法（五层骨架 + 负向词四条）

### 6.1 五层骨架（沿用 1011 已验证结构）

```text
1 场景与用途：2D game sprite animation, not a cinematic video；固定正面相机、固定画幅、固定比例、固定脚底锚点
2 动作因果：谁主动发力 / 外力从哪来 / 单次还是循环 / 终态是回 idle 还是保持倒地
3 关节读点：先写 2~3 个能识别动作的核心读点，再写次级滞后
4 画布硬约束：比例 / 根部 / 脚底 / 装备侧 / 边距 / 外扩量；禁止相机运动、整体漂移、武器换侧
5 节奏与阶段：进入 → 峰值 → 回收 → 终态；受击快进快出，死亡保持终态
```

### 6.2 负向词只留四条（1012 轮定规）

1. 眼睛**永不发光**，不变成亮光；
2. 脸**永不变**友善/嬉笑/得意，也**永不**读成愤怒/怒吼/狰狞/苦笑；
3. **永不疲惫、困倦、耷拉眼皮**；
4. **嘴永不张开、永远不露牙、布巾下永不画嘴形**。

### 6.3 1051 专属词表（每套都要带上）

> HIS LEFT HAND KEEPS THE BOW — the bow stays in his left hand the whole time, upright and vertical, it never changes hands, never changes length, never changes shape and never leaves the frame.（**Dead 套除外**：那一套弓脱手落地，见 4.7）
> HIS RIGHT HAND IS EMPTY — the hand on the left side of the frame stays empty and open; there is no arrow in it and no arrow in either hand, and no loose fletching anywhere in the frame. The quiver on his shoulder keeps exactly the same three white fletched arrows it has in image 1.（**射箭套除外**）
> HIS HEAD NEVER TURNS — never tilts down, never looks at his own hands, never shows a three-quarter view.
> HIS MOUTH STAYS COVERED — the scarf never moves, no mouth shape and no teeth are ever drawn under the cloth.

---

## 7. 选帧规则（三原则 + 本轮补充）

| 原则 | 说明 | 1051 证据 |
|---|---|---|
| ① 固定 k + 统一偏移 | 逐帧归一化到本体高 605 会把"头上下浮动"翻译成"整体缩放" | idle k=0.96645、walk k=0.93871 都是固定 k |
| ② 按动作相位抽帧，不按时间等距 | 等距抽会全落在同一相位（walk 第一版六帧五个都在"并腿"） | walk v4 选 f005/f008/f010/f013/f015/f018 |
| ③ 选帧前先整片看相位尺 | 源片可能混入闪回帧 | walk v4 在 t≈1.0s 步态倒退 |

**补充 ④（走路才用）**：走路必须固定 k + 统一偏移（各帧中位数），逐帧对位会抹掉水平摆动。
**补充 ⑤（受击/击飞/死亡）**：末帧要明确是"回收回 idle"还是"保持终态"，不要把受击视频里无意的**峰值平台**当成可用的末帧（1011 左受击曾有 1.21s 的平台，剔除平台重排节奏才可用）。

---

## 8. 落位口径（与已交付三套完全一致）

| 项 | 值 |
|---|---|
| 画布 | 1108² 透明底 |
| 人物本体高 | 605 |
| 可见脚底线 | 897（PIL 的 bbox 下边是 exclusive，可见底比几何底高 1px） |
| 身体中心 x | 521 |
| pivot | 0.5, 0.5（PPU 16）——**idle / attack / walk / 新增套全部同一个 pivot**，否则切换跳位 |
| 半透明 / 幕布残留 | 0 / 0 |
| 配准 | attack 六张曾统一左移 **12px**（dx=−12，IoU 0.8866）以对齐 idle；新套若站姿与 idle 不重合，需同样做一次掩码 IoU 配准 |
| **倒地 / 腾空帧的锚点** | 躺下后"本体高 605"不再成立（身体变长）。**Dead / Launched_Fall 的锚点改为「身体最低点与地线的关系」**：落地帧最低点 = 897；腾空帧最低点**抬离地线 40–60px**（1011 dead3 实测最低点 841、地线 895，差 54px）。缩放仍用固定 k，不逐帧归一化 |

---

## 9. 验收闸门

### 9.1 通用（逐片量）

| 闸门 | 阈值 |
|---|---|
| 脚底漂移 | 全帧恒定，漂移 ≤2px |
| 画幅 | 1:1 / 1080p；**出框 0 帧**（弓尖、箭羽、头顶都不贴边） |
| 人物本体高 | 与锚图一致，变化 ≤3%（防深蹲/拉伸） |
| 角色一致性 | 全程是同一个新 1051，无回退、无第二个人 |
| 武器 | 弓在左手（**Dead 套允许脱手落地**）；**帧内不得出现刀/剑/第二件武器** |
| 箭 | 受击/击飞/死亡三套**手里没有箭、画面上没有散落的箭羽**（肩后箭袋与三支白羽箭照旧保留，不得散成一捆） |
| 绿幕 | 四角与边缘 std ≈ 0；抠图后半透明 0 / 幕布残留 0 |

### 9.2 每套专属

| 套 | 专属闸门 |
|---|---|
| HitFlash | 肩线上耸 → 胸腔收紧 → 回位三段可读；**回位末帧 ≈ idle**（IoU ≥0.9） |
| HitLeft | 上身确实被推向**画面右**；骨盆不动；受击肩被压低 |
| HitRight | 上身确实被推向**画面左**；弓不被甩到身体另一侧 |
| Rise | 末帧是空中峰值；脚离地；不落地、不回 idle |
| Fall | 接地压缩 → 稳定倒姿；末帧与 Getup 首帧同源 |
| Getup | 回到**精确的 idle 持弓姿态**（末帧 = 身份图站姿） |
| Dead | **必须是"被打飞"而不是"原地塌"**：终态宽高比 1.4–1.6、填充率 ≤0.36、身体主轴与水平夹角 ≤25°；腾空帧最低点抬离地线 40–60px；终态保持、不站起、不回 idle；**弓脱手躺在身侧地面上，完整入画、不飞出画外、不遮脸** |

### 9.3 视频级工具

`Tools/visual_brief/measure_clip.py`（量测）、`check_shoot_video.py`（逐帧检查）、`compare_video_takes.py`（多 take 对比）、`measure_expression_arc.py`（表情弧线）。

---

## 10. 风险与已知坑

### 10.1 1051 专属

| 风险 | 说明 | 缓解 |
|---|---|---|
| **姿势画歪**（路线变更后的最大风险） | 不上传参考图，姿态全靠提示词；大姿态（倒地/被挑飞）最容易画歪 | 每套先只出 **1 张**姿势图 → 量测 + 人工过目 → 再批量；姿态词写"重心、支撑点、关节先后"这类可量描述 |
| **武器污染** | 词表来自持刀的 1012，容易顺手写出剑 | 词表里只描述身体，武器一律写"弓与箭袋"；闸门"帧内无第二件武器" |
| **横向跨度 1.58 列** | 弓在画面右手侧、箭袋在左肩；受击/击飞甩动会进一步变宽 | 提示词写"弓的摆幅不超过 X"，并把包络数值按 1051 实测比例换算（不照抄 1011 的像素数） |
| **半蒙面无嘴通道** | 受击/死亡的"发力"只能走眉眼 | 提示词把脸写成镜头里的主动作；负向词保留"嘴永不张开" |
| **怒视脸** | 已定的脸是怒视，不是"奸邪" | 新套沿用同一张脸；若要改，得先做眉眼精修并重出 idle/attack（成本另算） |
| **idle 无胸肩起伏** | 已交付 idle 是"表情+轮廓微动" | 若要真呼吸，需重出 idle 一支并加"肩线起伏 ≥2px"闸门（低优先级，本方案不含） |

### 10.2 管线通用（射箭轮踩出来的）

1. **姿势图画错 → 整支作废**：措辞要照抄参考帧的**实际画法**（短、透视、垂直朝下），不要写"指向左下"这种方位描述。
2. **纯文字 = 重新定义动作**：动作幅度只能靠母片/姿势帧继承。
3. **喂两帧 = 让模型偷懒**：母片只有两个姿势时模型会直接跳过去。
4. **模型会"凭空生成"缺失的物体**：母片首帧没有箭 → 模型复制出一捆散羽（射箭 v3）；母片首帧改成"持箭待机"才解决（v4）。
5. **生成片可能混入闪回帧**：选帧前必须先整片分段看相位尺。
6. **每支只生成一次，失败不自动重发**：叠轮边缘锐度逐轮下降。
7. **seedance 2 拒绝首尾帧与参考素材混用**（HTTP 400，不计费）。

---

## 11. 执行顺序与预算（待用户批准后执行）

**用户 2026-11-09 定：一步步来，先做 Dead。**

| 批次 | 内容 | 视频 | 姿势图 | 帧产出 | 备注 |
|---|---|---|---|---|---|
| **批次 A（现在）** | **Dead** | 1 支 | ~3~4 张 | 6 帧 | 唯一有"弓脱手"特殊规则的套，单独跑通管线；母片需要 持弓站姿 → 失去支撑 → 弓脱手 → 倒地终态 四五个姿势图 |
| 批次 B | HitFlash + HitLeft + HitRight | 3 支 | ~9 张 | 18 帧 | 三套共享同一批"持弓站姿"母片起点，出错可早停 |
| 批次 C | Launched_Rise + Fall + Getup | 3 支 | ~9 张 | 18 帧 | 三段必须共用首尾姿势图，接缝优先于单段好看 |

每批先出**一支**给用户过目再继续，避免整批翻车；Dead 这批还要先过"单张姿势图"闸门再烧视频。

---

## 12. 决策记录与剩余待定

### 12.1 已定（用户 2026-11-09）

| # | 决定 | 影响 |
|---|---|---|
| 1 | **Dead 弓脱手** | Dead 套：弓在倒地过程中脱手，落在身侧地面，完整入画 |
| 2 | **Rise 弓不脱手** | 击飞三套：弓始终在左手，被动甩动但不换手、不掉地 |
| 3 | **箭只在射箭时才有** | 受击/击飞/死亡三套手里无箭；画面左侧的手保持空、干净 |
| 4 | **HitLeft / HitRight 要出** | 缺口仍是 7 套 36 帧，但顺序排在 Dead 与受击三套之后 |
| 5 | **先做 Dead，一步步来** | 批次 A = Dead 单套，先把管线跑通 |
| 6 | **不上传动作参考图，只上传身份照** | 姿态语义只从 1012 帧与 1011 基线"读"出来写成词，不作图像输入 |
| 7 | **Dead 的语义是"被打飞"**，不是原地塌（工程内除 109 外全部如此） | §4.7 的关键姿势图与量化闸门按此重写 |
| 8 | **弓的落点不重要**；实在不行可以把弓裁出来做独立贴图单独控制 | §4.7.1 的 P3/P5 允许弓姿态不理想；兜底 = 弓独立贴图 |
| 9 | **Dead 终态仰面躺平**（与 1011/1012 基线一致） | P5 的提示词按仰面写 |

#### 4.7.2 真三国无双实机参考（2026-11-09，用户要求先找参考）

> **来源**：B 站实机录像 `BV1QgB1Y7E4g`《这就是割草游戏的魅力吗？太爽了！》（真·三国无双 起源）**t=594.5s**，720p 原片抽帧。已裁成 `Enemy1051ArtSource/00_Reference/dead_launch/`（group_launch + body_a/b/c，**第三方素材只留本地、不入库**，来源写在该目录 `references.csv`）。

**从实机读到的「小兵被打飞」姿态（三条硬读点）**：

1. **双脚朝天**：小腿与脚跟竖起指向天空，膝盖略屈——人被掀翻时**腿先甩过头顶**，这是无双最强的「打飞」读点。
2. **躯干后弓成弧**：背弯成月牙，胸口打开，剪影**是一条弧线而不是一条直线**；头的位置在身体下侧（被掀翻导致头朝下/侧下）。
3. **手臂脱离躯干下垂/甩开**：两只手不再护身，随身体旋转被甩向体侧。

> 对照本轮两次失败：**v1 把身体画成一条水平直线（读成“躺卧”）、v2 画成斜立但仍是直线且脚未甩起**——两者都缺「**腿甩过头顶 + 躯干弓成弧**」这一对特征，所以都不像被打飞。

**落成 1051 的 P3 姿态规格（v3 词据此写）**：

| 读点 | 1051 怎么画 |
|---|---|
| 双脚朝天 | 两条腿甩到头的**上方**，脚尖朝上、小腿竖起、膝略屈；这是第一优先特征 |
| 躯干后弓 | 背弓成弧（胸口朝观众方向打开），整体剪影是弧线，**禁止水平直线** |
| 头在后下侧 | 头在身体的下侧、脖子后伸、下巴朝上（仍正面可见，保证痛苦脸能读） |
| 手臂甩开 | 两臂向体侧甩开、手张开，不护身 |
| 旋转量 | 身体整体**倒转约 110°–130°**（不是 180° 倒栽，也不是水平躺），保证正面仍可读 |
| 弓 | 已脱手，独立飘在体侧，与身体有可见间隙（1021 判据：主体分 2 个连通块） |
| 脸 | 双眼紧闭、眉心皱紧（痛苦只走眼与眉），面部几何冻结 |

#### 4.7.3 P3 姿势图 v1 / v2 结果（2026-11-09，均已生成；v1 被用户否）

| 项 | v1 | v2 | v3 | v4 |
|---|---|---|
| 提交 | `submit_1051_pose.py --version deadp3`；只上传身份图 1 张；gpt-image-2.6-sunburst / quality=max / input_fidelity=high / 1024² / png / n=1；HTTP 200 / 137.76s；usage in 833 / out 1756 / total 2589 | `--version deadp3v2`；HTTP 200 / 约 140s；usage in 1121 / out 1756 / total 2877 | `--version deadp3v3`；HTTP 200；usage in 1082 / out 1756 / total 2838；词 `1051_pose_deadp3v3.txt`（4,395 字符） | `--version deadp3v4`；HTTP 200 / 131.99s；usage in 1082 / out 1756；raw 959,290 B，sha `5dde5b65…052b9`；词 `1051_pose_deadp3v4.txt` |
| 词 | `Tools/visual_brief/prompts/1051_pose_deadp3.txt`（3,331 字符） | `Tools/visual_brief/prompts/1051_pose_deadp3v2.txt`（4,481 字符）+ `refs_1051_pose_deadp3v2.json` |
| 原图 | `1051_pose_deadp3_raw.png`（1024² RGBA，1,176,045 B，sha `71d84516…64881`） | `1051_pose_deadp3v2_raw.png`（1024² RGBA，1,169,124 B，sha `29d3ebff…8bfa45`）——同样直接返回原生透明底 |
| 绿幕合成 | `1051_pose_deadp3_greenscreen_1024.png`（四角 (0,159,62) std 0） | `1051_pose_deadp3v2_greenscreen_1024.png`（同口径） |
| 主体 bbox | (119,213)–(949,846) | (128,162)–(943,878) | (92,172)–(938,877) | **(95,276)–(968,713)** |
| 宽高比 / 填充率 | 1.311 / 0.324 | 1.138 / 0.271 | 1.200 / 0.277 | **1.995 / 0.330**（偏“平板”） |
| 主轴（与水平夹角） | 10.4° | 25.4°（身体斜立，不再是水平直线） | 1.4°（回到水平，被打横） | 8.3° |
| 最低点 / 抬离地线 | 846 / 49px | 878 / 17px | 877 / 18px | **713 / 182px**（家族 1011dead3 约 118px） |
| 长轴/站立身高 | 1.17（基线 1.40） | 1.15（基线 1.40） | 1.19（基线 1.40） | **1.23**（基线 1.40） |
| 头顶/鞋底在画布的高度 | — | — | 17% / 86% | **27% / 70%**（家族 1011dead3 = 27% / 76%） |
| 连通块 | 2（身体 / 弓）→ 脱手成立 | 2（身体 / 弓）→ 脱手成立 |
| **用户验收** | ❌ 否决两点：① **非正面、读成「躺卧」，没有向后倒的感觉**；② **没有受击痛苦感，毫无表情** | ❌ 用户：**完全没后倒感**，要求去找真三国无双实机参考 | ❌ 用户：**双手姿势很奇怪**（成两个大张开的蜘蛛爪、朝镜头伸）+ **构图要修**（斜穿对角线、头在右上角、边距太紧） | **待用户确认** |
| v2 的改法 | — | 依据 1011 dead2/dead3 成片 + `memory/enemy1011-animation-prompt-archive.md`：① 背弓成 C 形、胸口朝上打开、脖子后伸下巴朝上、四肢**不对称**甩开、剪影是弧线、胸口仍正面朝观众；② 痛苦**只走眼与眉**（蒙面无嘴），面部几何/尺寸/颜色冻结不重画 |
| v2 已达成 | — | 后倒弧线 ✓、头后仰 ✓、**双眼紧闭 + 眉心皱紧 = 痛苦脸 ✓**、胸口正面 ✓、弓脱手 ✓、两手空无箭 ✓、肩后三支白羽箭保留 ✓ |
| v2 残留偏差 | — | ① **离地抬升只剩 17px**（目标 40–60px 游戏口径），“被打飞”的腾空感比 v1 弱；② 摊开度仍不到基线（长轴/身高 1.15 vs 1.40）。两者是同一个取舍：身体越斜、离地越高，w/h 越低 |

#### 4.7.4 路线定案（2026-11-09 用户定，**已被 §4.7.6 推翻**）

用户定：**不采用无双的倒栽姿态**——工程内除 109 外全部 dead 都是「被打横」的家族风格（1011/1012 的 dead3 = 斜向、空中、背弓、屈膝翘脚），所以 **v3 = 在 v1 的构图上修姿势与表情**：

| 修什么 | v1（被否） | v3（定案方向） |
|---|---|---|
| 躯干 | 一条水平直线（读成“躺卧”） | **背弓成弧**，腹朝上、胸打开，剪影是新月 |
| 头 | 随身体直走 | **头后甩**、脖子后伸、下巴朝上 |
| 腿 | 随意伸直 | **双膝屈、小腿翘起（脚高于臀）**（1011 dead3 的读点） |
| 手臂 | 对称摊开 | **一臂上甩、一臂下甩**，不对称 |
| 脸 | 怒视窄缝，无表情 | **双眼紧闭 + 眉心皱紧**的痛苦脸（面部几何冻结） |
| 弓 | 脱手独立 ✓ | 保持（2 个连通块） |

v3 实测：bbox (92,172)–(938,877)，847×706，**w/h 1.200 / 填充 0.277 / 主轴 1.4°**（回到水平被打横，与家族一致），抬离地线 18px，连通块 2（身体 150,609 px / 弓 14,883 px → 脱手成立），绿幕角 (0,159,62) std 0。
**残留偏差**：长轴/身高 1.19（基线 1.40）、离地抬升 18px（目标 40–60px）——即「摊开度」与「腾空高度」仍低于基线，但剪影语言（弓弧 + 屈膝翘脚 + 头后甩 + 不对称四肢 + 痛苦脸）已经与 1011 dead3 同族。

#### 4.7.6 四轮全部验收不通过（2026-11-09）：根因与修正规格

> **本节是四版的结论**（v1/v2 见 §4.7.3、v3 见 §4.7.4–4.7.5、v4 明细见下方 §4.7.5 的 v4 小节）。四版原图已全部撤出素材包。

**结论**：1051 的 Dead 关键姿势图（P3）**四版全部作废**。已撤出 `Enemy1051ArtSource/02_IdentityImages/`，归档在 `C:/Users/steam/Pictures/gptGen/enemy1051_rejected/03_DeadPose_P3/`（v1/v2/v3/v4 的 raw + 绿幕合成图）。提示词 `Tools/visual_brief/prompts/1051_pose_deadp3*.txt` **已作废，不要直接用**。

> **2026-11-09 晚更新**：Dead 路线改为**并排组图**（用户定），主文档 `plan/enemy-dead-group-reference-20261109.md`。本节「四版全部作废 + 根因两条」的结论仍成立，但**本节的修正规格表（P1–P5 那几行）经复测发现自相矛盾**（P3 身体块底 ~778 与地线 895 相差 117px，与同节「离地 20–60px」冲突；R1「顶 27–31%」与同表 1012 dead6 实测 38% 冲突），**不能直接当验收线**，纠错详情见主文档第 2 节。另：v4 的「身体块宽高比 1.995」实为**人物 + 弓的整体包围框**，身体块本体为 1.652；「四版弓全在身体右下」也不准确——**v2 的弓在右上**（dy −84），v1/v3/v4 才是右下。**v2 弓位对了仍被否，正说明「弓位正确」不等于「动作成立」。**

**用户否决记录**：

| 版 | 用户原话 | 实际错在哪 |
|---|---|---|
| v1 | 「非正面，变成像旧版本那样的躺卧，没有向后倒的感觉」「完全没有表达出受击的痛苦感，毫无表情」 | 身体画成一条水平直线 → 读成躺卧；脸照抄身份图的怒视 |
| v2 | 「完全没有后倒感」 | 改成斜立，但仍是直线、脚没甩起；腾空感与构图一起跑偏 |
| （中途） | 「去找姿态参考」「真三国无双实机看看小兵被打飞是怎样的姿态」 | 走了外网检索（1013 侧结论：外网检索对本项目**基本无效**） |
| v3 | 「双手的姿势很奇怪啊，当前 dead 动画末尾帧不这样吧，还要修构图」 | 两个大张开的蜘蛛爪朝镜头伸；身体斜穿画布对角线、头顶只有 17%、边距太紧 |
| v4 | 「构图不正确，你和另一边做 1013 的 dead 动画一样问题，验收不合格！」 | 身体被拉成平板（w/h 1.995）、浮空 182px、弓飞到了身体**右下** |

**根因（两条，与 1013 侧栽的是同一条）**：
1. **构图口径没对齐工程内已部署的 dead 实帧**。我用的是自己拍的估计值（「头顶 1/4、鞋底 3/4」），没先量 1011/1012 已部署帧的实际几何 → 对应 1013 侧的「当前其他 dead 姿势是这样的吗？」。
2. **道具（弓）与人物的相对位置关系错了**。家族里脱手的武器**飞向自己那一侧的上方**；我的四版弓全落在身体**右下方**（dy 为正）、跟头挤在一起 → 对应 1013 侧的「连旗帜原本的位置都错误了」。

**工程内已部署 dead 的实测口径**（1108²，地面线 895；身体块 = 去掉武器后的连通块）：

| 帧 | 身体块 bbox | 身体块 w/h | 顶/底 | 离地 | 武器块相对身体中心 dx,dy |
|---|---|---|---|---|---|
| 1011 dead1（站立挨打） | 167–782 | 1.060 | 28%/81% | 0 | — |
| 1011 dead2（后仰） | 114–803 | 1.239 | 31%/81% | 0 | — |
| 1011 dead3（腾空被打横） | 234–956 | 1.339 | 27%/76% | 54px | **dx −341, dy −143** |
| 1011 dead6（躺平） | 224–963 | 1.348 | 27%/77% | 43px | **dx −338, dy −171** |
| 1012 dead3 | 252–950 | 1.302 | 30%/78% | 29px | **dx −338, dy −148** |
| 1012 dead6 | 227–942 | 1.550 | 38%/79% | 17px | **dx −313, dy −218** |

**由此推出的四条硬规则（1051 直接照抄）**：
- **R1** 身体块顶 **27–31%** / 底 **76–81%**（画布相对位置，与画布尺寸无关）。
- **R2** 身体块宽高比 **1.06–1.55**；「被打横」那一拍取 **1.30–1.39**。
- **R3** 离地 **17–54px**（1108²）；姿势图（1024²）里换算为 **底边离地线 895 约 20–60px**。
- **R4** **脱手的武器飞向「自己那一侧的上方」**：家族 dx ≈ −330（武器在画面左）/ dy ≈ −150~−220（**在身体中心上方**）。

**1051 的修正规格**（1024² 姿势图口径；身体中心 x=467、地线 895、站立体高 696）：

| 关键帧 | 身体块顶 | 身体块底 | 身体块 w/h | 弓 |
|---|---|---|---|---|
| P1 持弓站姿 | 199（19%） | 895（87%） | 0.70（= 身份图） | 左手 |
| P2 挨打后仰（脚仍着地） | ~277（27%） | 895（87%） | 1.06–1.24 | 仍在左手 |
| P3 腾空被打横 | ~277（27%） | ~778（76%） | 1.30–1.34 | 脱手，中心 ≈ 身体中心 **(+330, −170)**（画面右上） |
| P4 落地 | ~277 | ~780 | 1.30 | 落在右上地面 |
| P5 躺平终态 | ~277（27%） | ~790（77%） | 1.35 | 躺在身侧右上 |

**方向决定（需用户拍板）**：1011/1012 的剑在**画面左**，倒下时**头在画面右**、剑飞向**画面左上**。1051 的弓在**画面右**（角色左手），所以**镜像**：**头在画面左、脚在画面右、弓脱手飞向画面右上**——弓始终留在自己那一侧、不跨过身体，这正是 1013 侧「旗与杆的相对位置不可改」在 1051 上的对应物。**若坚持头在画面右（与 1011/1012 同向），弓就必须跑到画面左侧**，与「弓在左手」矛盾，等于把 1013 那个错再犯一次。

**下一版继续带的四点**（v3/v4 已验证有效，用户未否）：
- **手：小、自然半握、手指微张、不朝镜头伸**（v4）
- **躯干背弓成弧、头后甩下巴朝上**（v3/v4）
- **双膝屈、小腿翘起（脚高于臀）**（v3）
- **痛苦脸：双眼紧闭 + 眉心皱紧，面部几何冻结**（v3）

**外网参考的结论**：真三国无双实机（B 站 `BV1QgB1Y7E4g` t=594.5s）抽到的「小兵被打飞」是**双脚朝天、躯干后弓、手臂甩开**；它对「背弓 + 腿甩起」有用，但与本项目家族的「水平被打横」不是一回事，**不能当构图依据**。已留档 `00_Reference/dead_launch/`（第三方素材只留本地）。**唯一权威参考 = 工程内已部署帧**：1013 侧整理的 `Library/Locus/tmp/dead_ref/all_dead.png`（可由 `Assets/Sprites/Enemy/*/Enemy_*_dead*.png` 重建）。

#### 4.7.5 v4（2026-11-09）：修「手」+「构图」（**仍未通过**）

用户对 v3 的反馈：**双手姿势很奇怪**（成两个大张开的蜘蛛爪、朝镜头伸）、**构图要修**（斜穿画布对角线、头在右上角、边距太紧）。

**修法（依据工程内家族 1011 dead3 / dead6 / 1012 dead6 的实测框位与画法）**：

| 修什么 | v3 | v4 |
|---|---|---|
| 构图 | 斜穿对角线、头顶 17% 高、边距 92/85/172/146 | **中段一条水平带里的紧凑一团**：头顶 **27%**（与 1011 dead3 同值）、鞋底 70%（家族 76%）、四周留明显空绿、不贴边 |
| 手 | 两个大张开的蜘蛛爪、朝镜头伸 | **小、自然半握、手指微张**，与身份图同大小；小臂方向随上臂，不朝镜头伸 |
| 保留 | — | 背弓成弧、屈膝翘脚、头后甩、痛苦脸（双眼紧闭）、弓脱手独立 |

**v4 实测**：bbox (95,276)–(968,713)，874×438，w/h 1.995 / 填充 0.330 / 主轴 8.3°，抬离地线 182px（家族 1011dead3 约 118px），长轴/身高 1.23，连通块 2（身体 113,066 px / 弓 13,145 px → 脱手成立），绿幕角 (0,159,62) std 0。

**残留偏差**：① **身体被拉得过于水平（w/h 1.995 vs 家族 1.548）**，读起来有点“平板”，弓弧感比 v3 弱；② **浮得比家族高**（182px vs 118px）。两者都源于 v4 词里“almost horizontal, tilted only slightly”这句写得太死——**若再修，v5 只需把这一句改成“斜置约 15°–20°、胸口朝上打开、剪影保持弧线”，并把“鞋底在 3/4 高度”改成“鞋底在 3/4 高度、整个身体压在画布下半部”**，v4 的手与紧凑构图不要动。

### 12.2 剩余待定

1. **Dead 终态是仰面还是侧卧**：1011/1012 都是仰面（摊开躺平）。1051 侧卧会让弓的落点更自然，但仰面与既有基线一致——默认按仰面，需改再拍板。
2. **母片路线失败时的兜底**：是否直接切「首尾帧锚图 + 数字规则词」（省图但动作靠词）。建议先试母片，失败再切，**不并行烧两支**。
3. **是否补 idle 的胸肩呼吸**：需要重出 idle 一支视频 + 重新选帧落位。
4. **受击峰值是否允许露出下颌/半张脸**：目前一律"布巾不动"。

---

## 13. 相关文件索引

| 用途 | 路径 |
|---|---|
| 形象身份图 | `Enemy1051ArtSource/02_IdentityImages/1051_idle_identity_greenscreen_1024.png` |
| 视频锚图 | `Enemy1051ArtSource/02_IdentityImages/1051_cast_anchor_green_1080x1080.png` |
| 已交付三套 | `Enemy1051ArtSource/03_AttackFrames/` `04_IdleFrames/` `05_WalkFrames/` |
| 姿态语义参考（**只读，不上传**） | `Assets/Sprites/Enemy/Enemy1012/Enemy_1012_{hitLeft,hitRight,dead,rise,landing,getup}1..6.png`、对照图 `Library/Locus/tmp/1051_plan/1012_ref_1.png` / `_2.png` |
| 旧 105 单帧（只读参考） | `Assets/Sprites/Enemy/Enemy5/enemy5_{hitted,dead,launch}.png` |
| 工具 | `Tools/visual_brief/`（`make_pose_reference` `submit_1051_pose` `make_keyframe_clip` `submit_seedance` `chroma_key` `pick_shoot_frames` `pick_cycle_frames` `land_1051_attack` `measure_clip` `check_shoot_video` `compare_video_takes` `measure_expression_arc`） |
| 提示词 | `Tools/visual_brief/prompts/1051_*.txt`（`1051_walk_v4.txt` 是措辞库最全的一份，建议当模板） |
| 通用动作基线 | `memory/enemy1011-animation-pose-reference.md` |
| 管线经验 | `memory/enemy1012-art-replacement-pipeline.md`、`memory/enemy1011-animation-prompt-archive.md` |
| 上游交接 | `plan/enemy1051-bandit-archer-redesign-handoff.md`、`plan/enemy1051-archer-shooting-video-plan.md` |
