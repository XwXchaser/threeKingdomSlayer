---
summary: 1012 山贼版 v5 立绘替换的完整交接（2026-11-04）：idle 全链路已完成并接入工程；attack 第一轮因"把脸钉死"的写法失败，已按 v4 两阶段法备好 E1'/E2' 两份提示词待授权生成；视觉库（实机截图+素材索引+选帧/量测/落位工具链）已建成。含文件路径索引、落位口径、闸门工具与坑清单。
id: kd_d16f485c-a2e1-4f15-90ed-233fafee9fba
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---


# 1012 山贼版立绘替换 · 交接（2026-11-04）

> 状态：**attack / walk / hitRight / hitLeft / hitHeavy 已完成并接入工程（2026-11-05）；idle 已接入但表情被后续重做覆盖成固定浅笑（见 §11）；其余 4 个动作（dead / rise / landing / getup）未动。**
> 上下文已满，本轮之后转新会话继续。

---

## 0. 一句话背景

1012（兜鍪贼兵）的新形象 v5 立绘已由用户验收，比例按 v11 身份图对齐。
要把游戏里 1012 的整套动作帧从"v11 刀兵形象"换成"山贼版 v5 形象"。
本轮已完成 **idle**；**attack** 第一轮失败（原因见 §4），重做方案已备好（§5）。

---

## 1. 已完成

### 1.1 v5 立绘 → 绿幕身份图 v6

```
C:\Users\steam\Pictures\gptGen\enemy1012_idle_v6\input_identity_v12_bandit_idle_v6_1024.png
1024×1024 / 绿 (0,159,62) / 四角 std 0 / 主体高 696 / 脚底 895 / 中心 474.5
```
规格与 v11 身份图 `enemy1011_attack_anim_v1/input_identity_v11_696.png` 完全一致（主体高 696 / 脚底 896 / 中心 475.5）。
来源：v5 立绘 → SVL 抠图 → 裁主体 → 缩放到主体高 696 → 贴 (180,200) → 合成绿幕。
副本已入库：`Enemy1012ArtSource/02_IdentityImages/input_identity_v12_bandit_idle_v6_1024.png`

### 1.2 idle 动画（已完成并接入工程）

| 项 | 值 |
|---|---|
| 视频 | `C:\Users\steam\Videos\doubaoVideo\enemy1012_bandit_idle_v6.mp4`，960×960/24fps/97 帧/4.042s，sha `78802b25…` |
| 任务 | `task_50d155b9b8154c8ebefcd71066ba83d5` |
| 选帧 | f010 / f017 / f024 / f032 / f039 / f046（周期 43 帧 = clip 的 1.8s 键距） |
| 抠图 | SVL Chroma T=79 / softness 16 / halo 1（半透明 0 / 幕布残留 0 / 孔 0） |
| 落位 | k=0.92366 + 逐帧偏移 x=521−缩放后中心x、y=897−可见底行 → 1108² |
| 接入 | 覆盖 `Assets/Sprites/Enemy/Enemy1012/Enemy_1012_idle1..6.png`（**保留 .meta**）；`.anim` / prefab 未动 |
| 校验 | 脚底 897 六帧全同、中心 520.5–521.5、高 597–614；首尾姿态差 0.0068 |
| 备份 | `Library/Locus/tmp/1012_batches/01_idle_v6/backup/idle_20261104-204026`（含落位正确前的黑底错版，勿用） |

### 1.3 视觉库（实机截图 + 美术素材索引 + 工具链）

- 库目录：`_art_process/VisualLibrary/`（`index.csv` 899 条 / `shot_defs.csv` / `shots/` / `bundles/`）
- 知识层入口：`Locus/knowledge/memory/visual-library.md`（**所有 AI 的取图入口**）
- 采集器：`Assets/Scripts/Editor/VisualLibrary/VisualLibraryCapture.cs`（Unity 内 Play Mode 批处理采集）
- 工具：`Tools/visual_brief/`（build_brief / claim_shots / index_art_assets / measure_1012_proportion / make_1012_proportion_sheet / pick_attack_frames / measure_expression_arc / submit_seedance / local_fix_1012_lowerbody）

### 1.4 溯源包已更新（`Enemy1012ArtSource/`，包校验 27 条一致 / 0 问题）

- `00_Index/replacement-videos.csv`：idle / attack 两行 → 山贼版成片
- `00_Index/identity-images.csv`：新增 `bandit_v6`
- `00_Index/frame-provenance.csv`：idle 6 行 + attack 16 行的来源帧号与落位参数
- `README.md`：追加「山贼版 v5 立绘替换」章节

---

## 2. 当前状态表

| 动作 | 精灵数 | 现状 |
|---|---|---|
| **idle** | 6 | ✅ **山贼版已接入**（用户已验收） |
| **attack** | 16 | ✅ **山贼版已接入**（2026-11-05，方案 A 表情：起手=半笑 → 劈砍露齿峰值 → 收势回半笑；源片 `enemy1012_bandit_attack_arc_E2.mp4`；详见 §5.2） |
| walk | 6 | ✅ **山贼版已接入**（2026-11-05；步态周期 16 帧，相位 f040，周期内等距取 6 帧；落位与 idle 同偏移） |
| hitHeavy | 6 | ✅ **山贼版已接入**（2026-11-05；替换视频工作流，动作与表情表演均由母本继承） |
| hitRight | 6 | ✅ **山贼版已接入**（2026-11-05；向右倒的受击反应，表情=痛苦龇牙惨叫 → 收回半笑） |
| hitLeft | 6 | ✅ **山贼版已接入**（2026-11-05；另一面的受击，动势照 v11 原设计：原地站立、刀自身侧抬起向左上） |
| dead | 6 | ❌ 同上 |
| rise / landing / getup | 6 / 6 / 6 | ❌ 同上（击飞链共用 k=0.93077 / 偏移 (106,172)） |

---

## 3. 裁剪动作的固定口径（已实测反推）

| 项 | 值 |
|---|---|
| clip 结构 | `Enemy_1012_Attack.anim`：3.1333s / 18 键；**t=0 与 t=3.1 同为 idle1**，t=0.3–1.5 = attack1..5，t=2.0–3.0 = attack6..16 |
| 旧片 attack1..16 ↔ 源片帧 | f010 f019 f027 f035 f043 f050 f052 f053 f055 f061 f065 f068 f070 f074 f078 f082 |
| 节拍 | 前 5 帧稀疏（步长 8–9，举刀过顶）→ 后 11 帧密集（步长 1–6，劈砍）→ attack16 回位 |
| 落位（attack） | 全局 k=0.944 + 固定偏移 (74,57)；**攻击帧脚底随斩击前冲变化**（896→933），不做统一脚底线 |
| 落位（idle） | k=0.92366 + 逐帧偏移（脚底 897 统一） |
| 导入设置 | PPU 16 / pivot：idle 用 0.6539,0.5487；**attack 与 dead 用 0.5,0.5** |

---

## 4. attack 第一轮为什么失败（**成功案例在同一个文档里**）

出处：`Locus/knowledge/plan/enemy1012-bandit-idle-animation-handoff.md` §0.3–0.4

| 轮次 | 写法 | 结果 |
|---|---|---|
| v3（失败） | 表情写成「**保持**图 1 的奸笑并让它活起来」+ 一串钉死脸的负向词 | 表情弧线**没有**实现 |
| **v4（成功）** | ①表情写成**分段时间线** ②负向词只留四条不可违反的 ③**把脸写成镜头里的主动作**、身体动作压到最低 | E1 头部片 / E2 全身片**弧线闸门均通过** |

**本次 attack 踩的就是 v3 的坑**：词里写「Keep his expression exactly as in image 1 and keep it cold and calculating **for the whole animation**」+「never look tired… never droop his eyelids」= 把脸钉死。

其余两个固有缺陷（新片本身，不是写法的错）：
- 起手 f007→f008 刀横向拉开的硬跳变（旧片是渐起）
- 劈砍后 f064–f072 三帧定格 → 选出的 s10–s12 是同一姿势（收势停顿约 0.3s）
- 全片只有约 13 个明显姿势，clip 要 16 键 → 必然复用

第一轮视频仍保留在 `C:\Users\steam\Videos\doubaoVideo\enemy1012_bandit_attack_v1.mp4`（未采用）。

---

## 5. attack 重做方案（**待授权，各 1 次**）

| 阶段 | 作用 | 参考图 | 提示词 |
|---|---|---|---|
| **E1'** | 头部表情弧线研究片（脸占画面最大 → 模型做表情可行性高；便宜、失败不污染工程） | `C:\Users\steam\Pictures\gptGen\enemy1012_bandit_face_v3\stageA6_wicked_grin_v1.png`（**已像素级比对：与验收的 v5/A6b 同一张脸**） | `Tools/visual_brief/prompts/1012_attack_arc_E1_head.txt`（2256 字符） |
| **E2'** | 全身 attack | `C:\Users\steam\Pictures\gptGen\enemy1012_idle_v6\input_identity_v12_bandit_idle_v6_1024.png` | **`Tools/visual_brief/prompts/1012_attack_arc_E2_full_v1beat.txt`（4149 字符；2026-11-05 修正三处「钉脸」缺陷，见 §5.1）** ← 按验收 take 的真实节拍重写；旧版 `1012_attack_arc_E2_full.txt` 已作废 |

E2'（v1beat 版）相对第一轮的四处改动：
1. 表情写成**时间线**（冷脸 → 蓄力时嘴角起 → 劈砍瞬间成坏笑露齿 → 收势收回）+ `the face must still travel through that arc even though the face is small in the frame`
2. 负向词只留四条：嘴不张开 / 不露下齿 / 不变友善笑 / 不疲惫困倦
3. **动作节拍按验收 take v1 的真实节拍重写**（§9）：起手改为**向上竖举**（`no sideways swing`）→ 斜劈下压 + 身体前俯 + 刀横在身前 waist 高 → **收势三段**（横刀身前 → 举回身侧 → 垂回身侧）
4. **删掉大回旋弧光**：只保留 `only a short, thin white trail flickers along the last part of the blade's path`，并明确 `no wide sweeping crescent arc and no full circular swing`

### 5.7 正面受击（hitHeavy）的参考视频定案（2026-11-05）

**参考片 = `sword_enemy_hit_front_v2.mp4`**（1011 已验收的正面受击源片）。
- 位置：`已验收源视频_20261019/1011_持剑兵/`，1011 包 `Enemy1011ArtSource/01_SourceVideos/` 同名同字节（1,989,310 bytes）
- **与工程当前来源的关系**：工程当前部署的 hitHeavy 来自 `dao_v11_hitfront_v1_6s_720p.mp4`（6s，v11 替换片）。实测**两者是同一个动作设计**（站立 → **垂直下沉成蹲姿** → 起身），只是 A 慢 B 快 —— 逐帧轨迹相关低是因为时长/相位不同，不是动作不同。
- **定案依据**：沿用 attack 当年的结论（§7）：**母本取用户验收的那一支**，节拍按 B 的真实节奏写。

**B 的真实节拍（抽帧实测）**：f000–f024 站立持刀（左上）→ f030–f072 下沉至蹲底 → f078–f096 起身回站。

**这直接修正了 v1 的错误**：v1 把正面受击写成「上身后仰」，生成出来是**向右倒**（那其实是 hitRight 的动势，已改归 hitRight）。正面受击的正确动势是**垂直下沉**，不是后仰也不是侧倒。

**新词 `Tools/visual_brief/prompts/1012_hitHeavy_v2.txt`（3548 字符）** 四拍：站立起手 → 下沉（脚不动、不滑、脸仍朝镜头）→ 蹲底（龇牙惨叫）→ 起身（收回半笑）；
负面词明确「不侧倒、不后仰、不滑步、不离开地面线」+ **「剑始终握在手里、绝不脱手漂浮」**。

### 5.7 正面受击（hitHeavy）的参考视频定案（2026-11-05）

> **流程性错误（本轮最大教训）**：本轮一直用「首尾帧工作流 + 文字描述动作」做「替换视频」这件事，等于每次把动作重新定义一遍 → 4 支全飘。正确做法见 memory 的 §9.2：**reference_video = 旧动作源片**，动作从母本继承，提示词只管形象与表情。

**母本 = `dao_v11_hitfront_v1_6s_720p.mp4`**（工程现有 hitHeavy 帧的来源，145 帧 / 6s）
- 位置：`C:\Users\steam\Videos\doubaoVideo\dao_v11_hitfront_v1_6s_720p.mp4`（3,067,539 bytes）
- **像素对拍确认**：部署的 hitHeavy1..6 全部出自它的 **f041/f051/f067/f079/f090/f100**（IoU 0.905–0.944）；已验收归档的 `sword_enemy_hit_front_v2.mp4` 仅 0.45–0.75。两者是同一动作设计（站立→垂直下沉→起身），仅时长不同（6s vs 4s）。
- **母本量化参数（验收标准）**：头顶下沉 **49px**（占身高 7%）；举刀只到**肩高**且在**身体左侧**、刀刃不出画；蹲底时刀**水平指左**；脚全程原地不动；6 来源帧的节拍如上。

**已生成的三支正面受击均为失败**（记录在此避免重演）：
- v1 `enemy1012_bandit_hitHeavy_v1.mp4`：向右倒（实际是 hitRight 动势）→ 已改归 hitRight
- v3 `enemy1012_bandit_hitHeavy_v3.mp4`：头顶下沉 226px（34%，几乎坐在地上）+ 举刀过顶（刀尖 top=0）+ 刀换到身体右侧
- v3 的失败根因：① 幅度词无上限（"sinks straight down toward the ground"）② 武器没锁手侧/高度/出画 ③ 母本参数没继承

**下一支（v4）必须走 reference_video 工作流**：`reference_video` = 母本 mp4（已验证可上传，HTTP 200）、`reference_image` = `input_identity_v12_bandit_idle_v6_1024.png`（**不能再当 first_frame/last_frame**）；提示词只写形象与表情，不写动作幅度。
（具体命令见本节末尾「下一支必须走替换视频工作流」；工作流定义见 skill 文档与 memory §9.2。）

**下一支必须走替换视频工作流**（定义见 skill 文档「## 替换视频工作流」与 memory §9.2）：

```
python Tools/visual_brief/submit_seedance.py --workflow replace \
  --prompt-file Tools/visual_brief/prompts/1012_hitHeavy_replace_v1.txt \
  --reference-image  C:/Users/steam/Pictures/gptGen/enemy1012_idle_v6/input_identity_v12_bandit_idle_v6_1024.png \
  --reference-video  C:/Users/steam/Videos/doubaoVideo/dao_v11_hitfront_v1_6s_720p.mp4   （母本，6s）
  --reference-video-extra C:/Users/steam/Videos/doubaoVideo/enemy1012_bandit_attack_arc_E1.mp4 （表情研究片）
  --duration 6 --output .../enemy1012_bandit_hitHeavy_v4.mp4 --approve
```
- 母本 mp4 已验证可上传（HTTP 200，2.9 MiB）；`--duration 6` 必须与母本时长一致，否则动作被压缩。
- 提示词 `1012_hitHeavy_replace_v1.txt` **不描述任何动作幅度**（幅度/节拍/持刀侧全部由母本继承），只写：身体动作照母本 + 脸的表情弧线（龇牙惨叫 → 收回半笑）。
- 工具已支持：`Tools/visual_brief/submit_seedance.py` 新增 `--workflow replace / --reference-image / --reference-video / --reference-video-extra / --duration`（frames 工作流原用法不变）。

### 5.7 正面受击（hitHeavy）：替换视频工作流（2026-11-05，已接入工程）

> **母本 = `C:\Users\steam\Videos\doubaoVideo\dao_v11_hitfront_v1_6s_720p.mp4`**（3,067,539 bytes，145 帧 / 6s）——就是工程现有 hitHeavy 帧的来源。
> 像素对拍：部署的 hitHeavy1..6 全部出自它的 **f041/f051/f067/f079/f090/f100**（IoU 0.905–0.944）；已验收归档的 `sword_enemy_hit_front_v2.mp4` 仅 0.45–0.75。
> **母本的脸完全可见且表演很足**（红布是束带不是遮脸）：f041 咬牙瞪眼、f051 张口惨叫、f067–f090 低头龇牙、f100 脸收回 → **表情随 reference_video 一起继承，不需要另发表情片**。

| 项 | 值 |
|---|---|
| 成片 | `enemy1012_bandit_hitHeavy_v4.mp4`（task `9fef6e65…`，3,413,412 bytes，sha `05c96f4f…`），145 帧 / 6s |
| 工作流 | **替换视频**：`reference_image`=身份图 v6、`reference_video`=母本 mp4、`duration=6`；提示词 `1012_hitHeavy_replace_v2.txt`（1265 字符，**不描述任何动作幅度**，只写「换成图 1 的人，其余照旧」） |
| **动作继承实测** | 包围盒左沿/上沿/高度轨迹相关 **0.975 / 0.919 / 0.940**；头部下沉 f067 +10 / f090 +36（母本 +27 / +46）；脚的落点逐帧差 1–2 px；构图与 idle 配准 dx=0/dy=0（IoU 0.9922） |
| 表情继承 | 六帧与母本逐帧同构：咬牙瞪眼 → 张口惨叫 → 低头龇牙 → 龇牙 → 头歪龇牙 → 脸收回 |
| 源片 QC | 剑脱手 **0** 帧；武器形变 **0** 帧（局部突刺检测）；脚下阴影残留 **0**；主体内绿味 32–102 px/帧（<1% 可见像素） |
| 落位 | k=0.92366 + 固定偏移 **(+110,+122)**（与 idle 同偏移；站姿帧配准 dx=0/dy=0） |
| 接入 | 覆盖 `Enemy_1012_hitHeavy1..6.png`（**.meta 全保留**）；回读：画布 1108²、脚底 885–899（与母本逐帧对应） |
| 备份 | `Library/Locus/tmp/1012_batches/08_hitHeavy_v4/backup/hitHeavy_v11_originals/` |

### 5.7 正面受击（hitHeavy）：**首尾帧锚图路线**（2026-11-05，已接入工程）

> **流程结论（本轮踩坑三次才找到）**：
> 1. **首尾帧 + 文字描述动作**（v3）→ 角色全程不跳 ✓，但幅度失控（头顶下沉 226px vs 母本 49px）
> 2. **reference_video + 长提示词**（v4）→ 动作继承 ✓，但形象随时间回退成参考片的 v11（f030 身形变宽、f090 脸变络腮胡）
> 3. **reference_video + 标准一句词**（v5，照已验收案例 `enemy1011_v5_video_replace`）→ **同样回退**（f000–f020 是山贼版，f022 起整个头变成 v11）
> → **跨原型换角色（山贼版 ↔ v11 差异大）必须走首尾帧锚图路线**；reference_video 只适用于同原型换细节。
> → **锁住角色的是「路线」不是「词」**：v3（首尾帧）全程 97 帧是山贼版；v4/v5（reference_video）都在 ~1 秒后变回参考片的人。

| 项 | 值 |
|---|---|
| 成片 | `enemy1012_bandit_hitHeavy_v6.mp4`（task `e2002ba3…`，3,661,367 bytes，sha `0a96f996…`），145 帧 / 6s |
| 首尾帧 | `02_IdentityImages/anchor_bandit_960.png`（身份图缩到 960²；主体高 652 / 脚底 839 / 中心 445 → 与 idle/walk 源片同构图，站姿帧与 idle 配准 dx=0/dy=0, IoU 0.990） |
| 词 | `Tools/visual_brief/prompts/1012_hitHeavy_anchor_v1.txt`：四拍 + **幅度写死**（“头比站立时最多低一个身高的十分之一，不蹲/不坐/不跪”）+ 武器红线（刀始终在手、同一手同一侧、不越身体另一侧、不高过肩线、不出画）+ 防超框（四边 ≥8% 空白、头顶与靴底同行） |
| 幅度实测 | 头顶下沉 **+64**（母本 +27；工程现有 v11 hitHeavy 为 9% 身高 → v6 约 10.4%，与既有观感一致） |
| 角色一致性 | **全程 97/145 帧均为山贼版**（窄脸+稀疏八字胡+麻衣+绳带+环首刀），无络腮胡/无红头巾/无 v11 圆盔 |
| 源片 QC | 剑脱手 **0** 帧；武器形变 **0** 帧；脚底线恒定 839；刀尖贴画面左缘（母本同阶段也是 0px 余量） |
| 选帧（按相位） | f030 刀被震起 → f062 下扫 → f068 蹲底 → f100 蹲底 → f118 起身 → f140 站定 |
| 落位 | k=0.92366 + 固定偏移 **(+110,+122)**（与 idle 同偏移） |
| 接入 | 覆盖 `Enemy_1012_hitHeavy1..6.png`（**.meta 全保留**）；回读：画布 1108²、脚底 897 |
| 备份 | `Library/Locus/tmp/1012_batches/10_hitHeavy_v6/backup/hitHeavy_v11_originals/` |

### 5.6 hitLeft 重做执行结果（2026-11-05，已接入工程）

> **先搞清「哪个视频需要被替换」**：台账里 hitLeft 槽位的 replacement_video 是 `dao_v11_hitright_v1_4s_720p.mp4`（名字写着 hitright，但因历史对调，它实际喂 hitLeft 槽位）。
> **像素对拍确认**（绿幕掩码 IoU 全帧扫描）：部署的 hitLeft1..6 全部匹配该片 **f020/f024/f033/f044/f049/f028**（IoU 0.968–0.975），另一支候选仅 0.55–0.70 → 台账的「视频」对、「帧号」不准。
> **v11 hitLeft 的动势实测**：头-躯干偏移稳定 -8~-10（**不侧倾**）、主体高 586–604（**不下蹲**）→ 就是「原地站着挨一下、刀斜举向左上、头微沉」。

| 项 | 值 |
|---|---|
| 成片 | `enemy1012_bandit_hitLeft_v1.mp4`（task `d05a5fce…`，2,373,477 bytes，sha `121adede…`），**一次成功** |
| 生成词 | `Tools/visual_brief/prompts/1012_hitLeft_v1.txt`（3324 字符）：动势照 v11 hitLeft（身体不倒不蹲）+ 表情=龇牙惨叫→收回半笑 + **「剑始终握在手里、绝不脱手漂浮」** |
| **剑离手检测** | 连通域检测：**0 帧脱手** ✓（对比 hitRight 那支的 f018-f025 有 8 帧脱手） |
| **武器形变检测** | 像素量相对基线突增 → **f004-f005**（刀变成木桩），选帧已避开；用宽度判定会误伤「刀举高」的正常帧 |
| 选帧 | DP 最大化最小相邻差 → **f008 / f026 / f042 / f072 / f078**（最小相邻差 0.453） |
| 落位 | k=0.92366 + 固定偏移 **(+110,+122)**；脚位段实测 (339-351)/(653-667) 与 idle 完全一致 |
| 接入 | 覆盖 `Enemy_1012_hitLeft1..6.png`（**.meta 全保留**）；回读：画布 1108²、脚底 897 |
| 备份 | `Library/Locus/tmp/1012_batches/06_hitLeft_v1/backup/hitLeft_v11_20261105-181904/` |

### 5.5 hitRight 重做执行结果（2026-11-05，已接入工程）

> **原计划做 hitHeavy（正面受击），用户验收时指出两个问题：① 剑莫名离手再归位；② 角色实际向画面右侧倾斜。**
> **实测：① 确有其事（f018–f025 刀变漂浮物）；② 整段反应是向右倒，不是后仰。**
> **按 v11 的方向约定（hitRight 的头明显偏右 +124~+203 px，hitLeft 基本不偏），本片属于 hitRight。**
> → 本片已改写入 **hitRight1..6**；hitHeavy 槽位已还原为 v11 原图，待另发。

| 项 | 值 |
|---|---|
| 成片 | `enemy1012_bandit_hitHeavy_v1.mp4`（task `cddf9def…`，2,750,990 bytes，sha `b0068a8f…`），**一次成功** |
| 生成词 | `Tools/visual_brief/prompts/1012_hitHeavy_v1.txt`（3938 字符） |
| **剑离手检测** | 连通域检测：主体以外出现 >150px 的独立块 → **f018–f025** 共 8 帧（刀浮空，~12,000px），其余帧剑在手中 |
| 选帧（重挑） | **f012 / f026 / f044 / f076 / f086**，全程在「剑不离手」窗口内；剑的朝向逐帧递进（水平 → 竖直 → 45°上 → 水平 → 下垂）；末帧用 idle1 |
| 落位 | k=0.92366 + 固定偏移 **(+110,+122)**（强制用 idle 偏移；按帧推算会被横伸的剑拉偏 12px） |
| 接入 | 覆盖 `Enemy_1012_hitRight1..6.png`（**.meta 全保留**，pivot 0.6539/0.5487 / PPU 16）；回读：画布 1108²、脚底 897–899 |
| 备份 | `Library/Locus/tmp/1012_batches/04_hitRight_v1/backup/hitRight_v11_20261105-165347/` |

**已知代价**：① 后仰幅度偏大（最深约 45°）且源片里保持约 2 s，工程只播 5 帧 + idle1（0.81 s）；② 冲击瞬间剑甩得很开（h01 的剑伸到画面左缘）。

### 5.4 hitHeavy（正面受击）的情景调研（2026-11-05，表情设计前）

**它是什么**：台账里的「HitHeavy（正面受击）」= Animator 的 **HitFlash** 状态，clip `Enemy_1012_HitFlash.anim`（**0.9 s**，6 键 @0.15 s）。另两组 HitLeft / HitRight 是左右方向（方向指「用哪个受击动画」，不是受力向量）。

**触发条件**：任何命中都触发，除非 C 技霸体帧 / 击飞 / 死亡 / QTE 攻击中 / 眩晕；攻击被打断时也转成 HitFlash（controller 里 Attack → HitFlash）。
方向为 Front → HitFlash；未指定方向 → **在 Front/Left/Right 三者中随机**（所以正面受击约占 1/3）。

**玩家实际感知的 0.81 s**（Animator 的 HitFlash 状态 exit time=0.9 → 0.81 s；最后自动回 Idle）

| 时刻 | 表现 | 脸能看见吗 |
|---|---|---|
| 0 – 0.045/0.09/0.14 s | **卡肉**冻结 Animator（轻/标准/重） | ❌ 同时在闪白 |
| 0 – 0.15 s | **闪白**：材质 (1,1,1,1) 纯白剪影 | ❌ |
| 0 – 0.06 s | **缩放冲击** 1.2×（重击 1.28×，霸体封顶 1.1×） | — |
| 0.15 – 0.24 s | **键 1 首次以彩色出现**（可见时长 = 卡肉时长 0.045–0.14 s，且此时缩放最大 → 脸最大最好读） | ✅ |
| 0.24 / 0.39 / 0.54 / 0.69 s | 键 2 / 3 / 4 / 5，每帧 0.15 s | ✅ |
| 0.84 – 0.90 s | clip 第 6 键 = **idle1**（与 Attack clip 的 t=0 同一 GUID） | ✅ |

→ **键 1 = 冲击帧（被卡肉+放大强调），键 2–5 = 后仰与回复，键 6 = idle1。**

**新美术的硬约束**
1. **头不能低下去** —— 低头就把脸藏了，表情白做；应该「上身后仰、下巴抬起」，脸始终朝镜头。
2. 最后一帧必须回到 **idle1 的姿态与表情**（clip 第 6 键就是 idle1），否则 0.84 s 处会跳。
3. 脸在屏幕上很小（角色高≈3.2 u，落位后脸宽≈60 px）→ 只能靠**大形状**：眉压、眼挤、牙关；嘴角细微差别读不出来。
4. 受击是**高频事件**（连打时每 0.3 s 一次）→ 表情不能夸张到烦人。
5. ~~现有 v11 版本是「蹲身低头」，且 v11 角色的脸被红围巾挡住，本来就没有表情空间 → 新版本必须换动势。~~
   **（2026-11-05 更正：此条错。红布是束带不是遮脸，v11 角色的脸完全可见且表演很足——母本 f041 咬牙瞪眼、f051 张口惨叫、f067–f090 低头龇牙、f100 脸收回。表情由 reference_video 继承即可，不需要另发表情片。）**
6. 现有 v11 版本是「蹲身低头」（头随身体沉下去）→ 正面受击应保持「下沉」动势，但**不低头**（否则脸不可见）。

### 5.3 walk 重做执行结果（2026-11-05，已接入工程）

| 项 | 值 |
|---|---|
| 成片 | `enemy1012_bandit_walk_v1.mp4`（task `8790cd63…`，2,987,323 bytes，sha `f24e4cfa…`），**一次成功** |
| 生成词 | `Tools/visual_brief/prompts/1012_walk_v1.txt`（3062 字符）：**正面原地踏步**（对齐旧 v11 walk 的前倾小跑）+ 表情=idle 同款半笑 + keep 列表逐项列举物理特征 + 四条面部负向词 + 禁止白弧光/残影/多肢 |
| 步态周期 | 实测 **16 帧**（配对距离 0.007）→ 相位起点 f040，周期内等距取 **f040 / f043 / f045 / f048 / f051 / f053**；相邻剪影差 0.335/0.156/0.216/0.326/0.150，无近重复 |
| 落位 | k=0.92366（与 idle 同缩放）+ 固定偏移 **(+53,+122)** → 脚底线 897（与 idle 同一偏移）；x 锚点用**躯干中心**（bbox 30%–55% 带的 x 中点）的中位数，不用 bbox 中心（刀伸出画面会拉偏 bbox） |
| 步态起伏 | 源片靴底行 827–848（周期起伏）；躯干左右摆幅 **38 px**（旧 v11 为 43 px，同量级）→ 原样保留，不逐帧对齐 |
| 接入 | 覆盖 `Assets/Sprites/Enemy/Enemy1012/Enemy_1012_walk1..6.png`（**.meta 全保留**）；回读：画布 1108²、高 614–628、脚底 897 |
| 备份 | `Library/Locus/tmp/1012_batches/03_walk_v1/backup/walk_20261105-151745/` |

**已知代价**：源片刀形与身份图一致（直刃 + 金 T 护手 + 金环首），**这点比 attack 稳**；但游戏 clip 是 0.6 s / 6 帧，步态周期 0.667 s → 几乎等速，步子偏快。

### 5.2 attack 重做执行结果（2026-11-05，已接入工程）

| 阶段 | 产物 | 结果 |
|---|---|---|
| **E1'** 头部表情探针 | `enemy1012_bandit_attack_arc_E1.mp4`（task `f8a44414…`，1,317,554 bytes，sha `c34251ab…`） | **一次成功**；弧线闸门 rise 3136 / Q3 215 < 峰值×0.6 / 末帧差 2 → **全过**；目视 f000 半笑 → f069 峰值露齿 → f096 回到起手 |
| **E2'** 全身 | `enemy1012_bandit_attack_arc_E2.mp4`（task `e0047d6c…`，2,406,499 bytes，sha `0e201f90…`） | **一次成功**；表情弧线成立（f000 冷笑 → f036/f048 满齿大笑 → f080 收 → f096 回起手） |

**表情方案 A（用户定）**：起手与收势 = **idle 的半笑**（与游戏内 idle 一致），峰值 = **劈砍瞬间露齿**。
（不采用方案 B「冷脸→坏笑→冷脸」的理由：与实际部署的 idle 表情对不上，idle↔attack 与 attack→idle 两头都会跳变。）

**落位（新口径，与 idle 完全一致）**：新片站姿帧 ↔ idle 站姿帧 掩码配准 → **dx=0 / dy=0, IoU=0.986** → `k=0.92366 + 固定偏移 (+109,+122)` → 1108²，脚底线 **897**（刀尖可低于脚，s07–s11 为刀尖）。
工具：`Tools/enemy1012_attack_land.py`（默认干跑，`--apply` 才写）。

**选帧**：举刀段手选（f004/007/010/013/016）+ 劈砍段 **DP 最大化最小相邻差**（f050…f090，排除 f052/f053 大回旋弧光）→ f004 007 010 013 016 050 054 058 062 066 070 074 078 082 086 090。

**已知代价（源片固有）**：
1. 刀形在片内会变（起手/收势直刃细刀，举刀/劈砍段环首弯刀）；
2. f060–f072 为定格段 → s09–s11 同一姿势（落定停顿约 0.2 s）；
3. 全片可分辨姿势约 14 个，16 键需复用。

**已完成**：16 张 `Enemy_1012_attack1..16.png` 覆盖写入（**.meta 全保留**，pivot 0.5/0.5 / PPU 16 不变）；回读校验：画布 1108²、脚底 897、s16 中心 x 521.5（= idle1 521）。
**溯源**：`Enemy1012ArtSource/00_Index/frame-provenance.csv`（attack 16 行）、`replacement-videos.csv`（attack 行 + 新增 attack_arc_probe_E1 行）、`01_SourceVideos/` 存入两支新片、`README.md` 山贼版章节已更新；`verify_art_sync.py` → **27 条一致 / 0 问题**。
**备份**：`Library/Locus/tmp/1012_batches/02_attack_arc_E2/backup/attack_20261105-143620/`（旧 v11 形象 16 张）。

### 5.1 E2' 提示词修正（2026-11-05，未发起任何生成）

把 E2' 逐句比对**已过闸门的 E2 原词**（`Library/Locus/tmp/1012_batches/01_idle_v6/scripts/enemy1012_E2_expression_arc.py`），发现 v1beat 版正文里实际留了**六条**面部禁令（§5 声称只留四条）：

| 缺陷 | 修正 |
|---|---|
| `and he must never droop his eyelids` | 删除（v4 成功词明确删掉此句） |
| keep 列表 `... the leather chin strap, his face, the hemp tunic, ...` | 改为逐项列举物理特征（窄长脸/高颧骨/凹颊/尖下巴/浓眉/病黄肤/稀疏八字胡/胡茬/肤色亮度） |
| 结尾 `no new equipment and no change to his face or helmet` | 改为 `no new equipment and no change to his helmet, his clothing or his weapon` |

修正后面部约束句与成功版**结构完全一致**；残留检查 0 命中，提示词 3942 → 4149 字符。
**E1' 头部片复查无缺陷**（负向词止于 `bored, sad or blank`，keep 列表逐项列举，结尾无面部禁令）→ 可直接提交。
修订记录：`Tools/visual_brief/prompts/CHANGES_1012_attack_prompts.md`

提交命令（通用提交器，提示词/参考图/输出全参数化）：
```bash
python Tools/visual_brief/submit_seedance.py \
  --prompt-file Tools/visual_brief/prompts/1012_attack_arc_E1_head.txt \
  --reference  "C:/Users/steam/Pictures/gptGen/enemy1012_bandit_face_v3/stageA6_wicked_grin_v1.png" \
  --output     "C:/Users/steam/Videos/doubaoVideo/enemy1012_bandit_attack_arc_E1.mp4" \
  --dry-run
# 实际提交加 --approve；任务已建但中断用 --resume（绝不重新提交）
```

**建议**：E1' 先发，看片通过再发 E2'（两发一起发 = 2×约 0.57）。

---

## 6. 工具清单

| 工具 | 用途 | 关键说明 |
|---|---|---|
| `Tools/visual_brief/submit_seedance.py` | 通用 Seedance 图生视频提交器 | `--dry-run` / `--approve` / `--resume`；`--resume` 只续等不重发 |
| `Tools/visual_brief/pick_attack_frames.py` | 挑攻击有效帧 | **峰值对齐**法；`--validate-on <旧片抠图目录>` 在旧源片回归 **16/16 精确命中** |
| `Tools/visual_brief/measure_expression_arc.py` | 表情弧线闸门 | 嘴部带相对首帧的逐像素变化量（>60 记 1），全身版先做头部垂直对齐。闸门 rise≥60 / Q3<峰值×0.6 / 末帧≈首帧 |
| `Tools/visual_brief/measure_1012_proportion.py` | 身材比例量测 | `--baseline v11` 可对 v11 身份图判定 |
| `Tools/visual_brief/compare_video_takes.py` | **判定某支视频是不是验收的那一支** | 绿幕键 → 裁内容 bbox → 归一化同尺寸 → 掩码 IoU；配 `--sprite` + `--frame` 用已部署精灵对拍候选片。**要扫全帧**排除帧号漂移（1011 attack 实测：v1 IoU 0.93–0.96，v2 最高 0.61） |
| `Tools/visual_brief/make_1012_proportion_sheet.py` | 并排对齐对照图 | 每 10% 身高参考网格 + 实测数值栏 |
| `Tools/enemy1012_idle_land.py` | idle 选帧 + 落位 + 写入 | 默认干跑，`--apply` 才写；自动备份 |
| `Tools/enemy1012_idle_video_v6.py` / `Tools/enemy1012_attack_video_v1.py` | 第一轮两支视频的提交脚本 | 已跑完，保留作记录 |

---

## 7. 坑清单（**别再踩**）

1. **表情不能写"保持/加深"**，必须写分段时间线；负向词里针对脸的"绝不"会把脸钉死。
2. **闸门在全身 attack 上会假阳性**：头顶对齐锚点是"身体最高像素"，attack 里最高点是举起的刀 → 量到的是刀的移动。全身版闸门只能参考，**必须配目视六帧对照**。
3. **写精灵必须保留 alpha**：曾用 `convert("RGB")` 落盘 → 透明区变黑底 → Unity 里是黑方块。**写完必须回读**。
4. **PNG 绿幕色不是标称值**：v5 立绘实测绿是 (2,165,82)，管线身份图用的是 (0,159,62)；抠图阈值按各自实测值走。
5. **`.meta` 里的 guid 是 base64、引用处是 hex**，无法本地反查映射 → 需要 Unity 才能解析（Editor 断线时只能用轮廓匹配等旁证）。
6. **本项目的 .meta 在 gitignore 白名单外**：`Enemy1012ArtSource/*` 被忽略，只放行 `README.md` / `00_Index/` / `01_SourceVideos/` / `02_IdentityImages/`；**新产物要放进这四个位置之一**。
7. **不要自己启 Unity**：`locus.ensure_unity_editor` 会真的拉起 GUI（本轮已因此越界过一次）。Editor 崩溃后由用户自己打开。
8. 视频接口 **不返回费用/usage**（历史 cost_spend 是记账值，不是接口返回）。
12. 一次动作只生成一次（叠轮边缘锐度 3.71→3.33→3.14 逐轮下降）。

---

## 8. 未完成 / 下一步

1. **Unity 实读复验**（meshMinY −21.708 / 脚底世界 Y −1.8044）——**已移交 Unity 侧，不在美术产出范围内**。
2. **attack 重做**：✅ **已完成并接入工程**（2026-11-05，详见 §5.2）。待办仅剩：Unity 侧复验（编辑器侧，不在美术范围内）。
3. **其余 5 个动作**（hitHeavy / dead / rise / landing / getup）仍是 v11 形象，与山贼版不一致。**下一步建议 hitHeavy（正面受击）**：需另发一支，约束 = 不向侧面倒（正面受击不能像 hitRight 那样向右倒）+ **剑全程在手中** + 表情沿用「龇牙惨叫 → 收回半笑」。dead / rise / landing / getup 另排（击飞链换新形象后需重测落位口径）。
4. `Enemy1012ArtSource/README.md` 的「已知偏差」段**已随 attack 重做更新**。
5. **当前部署的 idle 没有表情弧线**（详见 §11）——若要恢复当初验收过的冷脸→坏笑→冷脸弧线，需另发一支 idle 生成。

---

## 9. 动作母本用错 take（**2026-11-04 用户发现，已定位并更正**）

用户指出：真正验收的 1011 攻击动画是 `sword_enemy_attack_front_v1.mp4`，而本次生成参考的是 `dao_v11_attack_v1_4s_720p.mp4`（v11 替换成片）→ **用了错的母本，导致新片动作生硬**。

**实测比对（绿幕包围盒轨迹 + 中心轨迹相关系数）**

| 对比 | 相关系数 | 结论 |
|---|---|---|
| 用户 v1 ↔ `2b1888f6…`（包 videos.csv 记的源片） | **1.000** | **同一条 take**（不同编码） |
| `2b1888f6…` ↔ `sword_enemy_attack_front_v2.mp4`（1011 包） | 0.793 | 不同 take（v2 中段有巨大白色回旋弧光，宽度轨迹 541 407 **750** 573 542） |
| v1/`2b1888f6` ↔ `dao_v11_attack_v1_4s_720p.mp4`（D） | 0.980 | D 是照着另一支 take 做的 |
| **v1/`2b1888f6` ↔ 我生成的 `enemy1012_bandit_attack_v1.mp4`（E）** | **0.646** | **本次生成偏离验收版最多** |

**错链**：我的提示词以 D（v11 替换成片）为动作母本 → D 的节拍源自 v2 那支 → 继承了"大回旋弧光"，并额外带出"刀横拉"起手。

**验收版 v1 的真实节拍（13 帧采样实测）**
`垂刀站定 → 向上竖举过头（慢，f008–f032）→ 斜劈下压 + 身体前俯（f040–f064，仅 f056 一道短促小拖尾）→ 收势三段：横刀身前 → 举回身侧 → 垂回身侧（f072–f096）`

**已改**
- 新提示词 `Tools/visual_brief/prompts/1012_attack_arc_E2_full_v1beat.txt`：去掉横拉、去掉大回旋弧光（只留短促小拖尾）、收势改三段、表情峰值对齐第二拍
- `00_Index/frame-provenance.csv`：attack 的 origin_video 16 行 `sword_enemy_attack_front_v2.mp4` → `2b1888f6388ed56b14da731eb0aeb229.mp4`
- `00_Index/videos.csv`：attack 行 note 原写"与 v2 同动作"（**实测为错**）→ 已改为"与 sword_enemy_attack_front_v1.mp4 同一条 take"

**方法论（可复用）**：要确认"某支视频是不是验收的那一支"，用**绿幕包围盒轨迹 + 中心轨迹的相关系数**做两两比对，比对文件名的记载可靠。

**其余动作审计结果**：10 个动作里只有 attack 的 origin 列与 videos.csv 不一致（已更正）；hitLeft/hitRight 的对调是**历史既有**（README 已注明"命名对调后"），非本轮引入；其余 7 个动作两表一致。

---

## 9.1 实证：已部署的 1011 攻击精灵其实出自 **v1**，不是 v2

新工具 `Tools/visual_brief/compare_video_takes.py`（绿幕键 → 裁内容 bbox → 归一化同尺寸 → 掩码 IoU）。**先对记录的那一帧，再扫 f025–f094 全帧**（排除帧号漂移）：

| 已部署精灵（记录源帧，1-based） | v1 最佳匹配 | v1 IoU | v2 最佳匹配 | v2 IoU |
|---|---|---|---|---|
| `Enemy_1011_attack3.png` (f029) | f027/f028 | **0.957** | f026 | 0.609 |
| `Enemy_1011_attack6.png` (f049) | f048 | **0.956** | f026 | 0.616 |
| `Enemy_1011_attack10.png` (f055) | f054/f055 | **0.930** | f054 | 0.497 |

记录源帧是 **1-based**（差 2 帧），v1 的最佳匹配帧与记录逐条对得上；v2 则在**任何帧**都上不去 0.62。

**结论**：`Enemy1011ArtSource/01_SourceVideos/sword_enemy_attack_front_v2.mp4` **不是已部署精灵的来源**。台账（`memory/video-generation-case-log.md`）写“v2 已验收并部署”、provenance 的 video 列写 v2 —— **两处都错**；而同一行的 cutout 临时目录名反而是 `Library/Locus/tmp/sword_enemy_attack_front_v1/`，且 v1 的字节数 2,015,371 与台账 v1 行完全一致。**当年部署用的是 v1 那一支**（v1 = task_e92bbc89…，v2 = task_67c87dbe…）。

**推论（好消息）**：工程里已部署的 `Enemy_1011_attack1..16.png` **与验收的 v1 一致，无需重新接入**；错的只是源包留存的那支文件与两条记录。

## 9.2 源包替换（**已执行**）

| 动作 | 内容 |
|---|---|
| 删除 | `Enemy1011ArtSource/01_SourceVideos/sword_enemy_attack_front_v2.mp4`（错 take，中段带大回旋弧光） |
| 放入 | `Enemy1011ArtSource/01_SourceVideos/sword_enemy_attack_front_v1.mp4`（= 用户 `C:/Users/steam/Downloads/sword_enemy_attack_front_v1.mp4` 原片） |
| `00_Index/videos.csv` | attack 行 file / bytes / sha256 全部按新文件重算（2,015,371 / `bbc8f914…`）；task_id 由 v2 的 `task_67c87dbe…` 改为 v1 的 `task_e92bbc89…`；acceptance 注明「已更正，见 9.1」 |
| `00_Index/frame-provenance.csv` + `.md` | attack 16 行 video 列 → v1、task → v1；keying_route 由 `canonical v2` 改为 `canonical v1`；notes 补记更正依据 |
| `README.md` | Attack 行改指 v1；新增「攻击源片更正」说明 |
| 知识层副本 | `Locus/knowledge/plan/enemy1011-frame-provenance.csv` / `.md` 同步更正；`memory/video-generation-case-log.md` 的 Attack v1/v2 两行结论重写（v1 = 实际部署源，v2 = 未用于部署） |

**保持不动**：`Enemy1012ArtSource/00_Index/frame-provenance.csv` 的 attack origin 指向 `2b1888f6388ed56b14da731eb0aeb229.mp4`（与 v1 同 take，相关系数 1.000）——**该记录本身没错**，不必改。

**校验**：`python Locus/tools/art-sync/verify_art_sync.py` → 27 条一致 / 0 问题。

---

## 11. 实证：当前部署的 idle 没有表情弧线（2026-11-05 新发现）

### 11.1 溯源（像素级 IoU，`Tools/visual_brief/compare_video_takes.py` 全帧扫描）

| 部署精灵 | 最佳匹配 | IoU | 次高候选 |
|---|---|---|---|
| `Enemy_1012_idle1.png` | `enemy1012_bandit_idle_v6.mp4` f005 | **0.979** | 其他 idle 源 ~0.84 |
| `Enemy_1012_idle3.png` | 同上 f024 | **0.974** | ~0.83 |
| `Enemy_1012_idle6.png` | 同上 f000 | **0.976** | ~0.84 |

→ 部署源确认 = `enemy1012_bandit_idle_v6.mp4`（task `50d155b9…`），选帧 f010/f017/f024/f032/f039/f046。

### 11.2 问题一：源片提示词就是 v3 失败写法

`Tools/enemy1012_idle_video_v6.py` 的 PROMPT 写着 `Keep his expression exactly as in image 1 and keep it cold and calculating for the whole loop` → 脸被钉死（§4 的坑）。
嘴部变化信号呈**双峰起伏**（f28 / f76），不是一条干净的起—峰—落弧线；目视六帧（`Library/Locus/tmp/1012_batches/01_idle_v6/checks/DEPLOYED_sprites_idle1_6.png`）**几乎全程同一张浅笑脸**，无露齿、无冷脸起点。
（对比：失败片 `enemy1012_bandit_idle_v3.mp4` 实测 rise 仅 28、嘴部序列无趋势；v6 的 rise 369 属于「有起伏但很弱」。）

### 11.3 问题二：选帧把弧线截断了

源片 97 帧 = 4.04s；部署只取 **f010–f046**（43 帧 = 前 1.8s）→ 只覆盖第一段起伏，**第二段（f60–f096）与末尾回落根本没被选中**，循环也收不回同一表情。
对照当初验收过的成功片 `enemy1012_idle_arc_v4.mp4`：六帧取 f000/f016/f028/f048/f076/f096 = 冷脸 → 嘴角起 → 露齿窄笑 → 满口坏笑 → 收敛 → 回冷脸，**完整弧线**（目视证据 `Library/Locus/tmp/1012_batches/01_idle_v6/checks/RAW_arc_v4_SUCCESS.png`；闸门 rise 804 / Q3 1047 < 峰值×0.6）。

### 11.4 结论与修法（待授权）

游戏里现在的 1012 idle = **固定浅笑 + 呼吸**，不是当初验收过的表情弧线。
要恢复：① 再发一支 idle 生成，提示词**照抄已过闸门的 E2 原词**（`Library/Locus/tmp/1012_batches/01_idle_v6/scripts/enemy1012_E2_expression_arc.py` 的 PROMPT），身份图用 `input_identity_v12_bandit_idle_v6_1024.png`；② 选帧必须覆盖整段弧线（f000 / f016 / f028 / f048 / f076 / f096 口径），而不是均匀取 43 帧窗口。

---

## 12. 批次目录与「选中的帧在哪」（2026-11-05 重排）

**所有 1012 产出收在 `Library/Locus/tmp/1012_batches/` 下，一批次一目录，互不混放。** 索引：`Library/Locus/tmp/1012_batches/README.md`

| 批次目录 | 内容 | 落位成品帧 | 逐帧清单 |
|---|---|---|---|
| `01_idle_v6/` | idle（山贼版 v5） | `picked/landed/`（本批次 11-04 完成，未留暂存帧） | `picked/picked_frames.csv` |
| `02_attack_arc_E2/` | attack（方案A 表情） | `picked/landed/s01..s16.png` | `picked/picked_frames.csv` |
| `03_walk_v1/` | walk | `picked/landed/w01..w06.png` | `picked/picked_frames.csv` |
| `04_hitRight_v1/` | hitRight（向右倒的受击） | `picked/landed/h01..h06.png` | `picked/picked_frames.csv` |
| `05_hitHeavy_front/` | 正面受击（**未完成**） | `rejected/landed_first_attempt/`（被否的第一版） | — |
| `_ref_v11_sprites/` | v11 旧形象参考图（受击三组朝向对照） | — | — |
| `_tool_validation/` | 抠图器回归验证（与归档 SVL 对拍） | — | — |

**每个批次的 `keyed/` 目录里是抠图后的 97 帧原始素材，选中的那一帧就在其中**（文件名即源帧号，如 `04_hitRight_v1/keyed/f012.png`）。
`picked/picked_frames.csv` 逐行给出：精灵名 ← 源帧号 ← 源视频 ← task ← 抠图帧路径 ← 落位帧路径。

> 重排前的散目录（`enemy1012_attack_E2/`、`enemy1012_walk_v1/`、`enemy1012_hitHeavy_v1/`、`arc_measure/`）已全部收拢；
> 纯干跑用的暂存目录（stage_*）与被取代的偏移版本已删除（内容均在 picked/landed/ 里保留）。

---

## 10. 文件路径速查

### 10.0 源视频地址（外部盘，不在工程里）

**A. 已验收源视频归档（权威母本，用户在 Downloads 收的那批）**
```
C:\Users\steam\Downloads\threeKingdomSlayer_已验收源视频_20261019\
    索引.csv
    1011_持剑兵\   sword_enemy_attack_front_v2.mp4 / sword_enemy_hit_front_v2.mp4
                   sword_enemy_hit_left_v3.mp4 / sword_enemy_hit_right_v1.mp4
                   sword_enemy_dead_front_v4.mp4 / sword_enemy_walk_front_v4.mp4
                   sword_enemy_v4_combat_idle_loop_v1_720p_4s.mp4
                   enemy1011_launch_rise_v1 / landing_v1 / getup_v1.mp4
    1021_持盾兵\   10 个（coward / shield / hitflash / dead / walk …）
    109_枪骑兵\    8 个（rider_fall / retreat / charge …）
```

**B. v11 替换成片（生成时的产出；工程里**现有**那批帧的来源）**
```
C:\Users\steam\Videos\doubaoVideo\
    dao_v11_idle_v1_4s_720p.mp4      dao_v11_walk_v1_4s_720p.mp4
    dao_v11_attack_v1_4s_720p.mp4    dao_v11_hitfront_v1_6s_720p.mp4   ← 正面受击·当前帧来源
    dao_v11_hitleft_v1_4s_720p.mp4   dao_v11_hitright_v1_4s_720p.mp4
    dao_v11_dead_v1_4s_720p.mp4      dao_v11_rise_v1 / landing_v1 / getup_v1_4s_720p.mp4
```

**C. 本轮新生成的 1012 山贼版片**
```
C:\Users\steam\Videos\doubaoVideo\
    enemy1012_bandit_idle_v6.mp4                 （已接入）
    enemy1012_bandit_attack_arc_E1.mp4 / _E2.mp4（已接入）
    enemy1012_bandit_walk_v1.mp4                 （已接入）
    enemy1012_bandit_hitHeavy_v1.mp4             → 实际用于 **hitRight**（已接入）
    enemy1012_bandit_hitLeft_v1.mp4              （已接入）
    enemy1012_bandit_idle_v1/v2/v3.mp4、enemy1012_bandit_attack_v1.mp4（被否，勿用）
    enemy1012_expression_arc_v4.mp4 / enemy1012_idle_arc_v4.mp4（10-30 成功案例）
```

**D. 工程内归档**：`Enemy1011ArtSource/01_SourceVideos/`（1011 母本）、`Enemy1012ArtSource/01_SourceVideos/`（1012 母本 + 本轮新片），台账见 `Enemy1012ArtSource/00_Index/`。

| 内容 | 路径 |
|---|---|
| 方案（本轮主文档） | `_art_process/VisualLibrary/bundles/1012_proportion_fix/PLAN_1012修正方案.md` |
| 需求整合稿 | 同目录 `brief.md` |
| 素材包（9 张图） | 同目录 `images/` |
| 溯源包 | `Enemy1012ArtSource/`（README + 00_Index + 01_SourceVideos + 02_IdentityImages） |
| v5 立绘（已验收） | `C:\Users\steam\Pictures\gptGen\enemy1012_bandit_face_v3\stageA6b_fullbody_v1.png` |
| A6 头肩（E1' 用） | `C:\Users\steam\Pictures\gptGen\enemy1012_bandit_face_v3\stageA6_wicked_grin_v1.png` |
| 山贼版身份图 v6 | `C:\Users\steam\Pictures\gptGen\enemy1012_idle_v6\input_identity_v12_bandit_idle_v6_1024.png` |
| idle 成片 | `C:\Users\steam\Videos\doubaoVideo\enemy1012_bandit_idle_v6.mp4` |
| attack 第一轮（未采用） | `C:\Users\steam\Videos\doubaoVideo\enemy1012_bandit_attack_v1.mp4` |
| 视觉库 | `_art_process/VisualLibrary/` |
| 视觉库知识入口 | `Locus/knowledge/memory/visual-library.md` |
| 历史成功案例（表情弧线 v4） | `Locus/knowledge/plan/enemy1012-bandit-idle-animation-handoff.md` §0.3–0.4 |
| 成功案例原词（已过闸门） | `Library/Locus/tmp/1012_batches/01_idle_v6/scripts/enemy1012_E1_expression_arc.py` / `.../enemy1012_E2_expression_arc.py` |
| 提示词修订记录 | `Tools/visual_brief/prompts/CHANGES_1012_attack_prompts.md` |
| 目视证据（弧线 vs 固定浅笑） | `Library/Locus/tmp/1012_batches/01_idle_v6/checks/RAW_arc_v4_SUCCESS.png`、`RAW_idle_v6_DEPLOYED.png`、`DEPLOYED_sprites_idle1_6.png`、`mouth_3way.png` |
