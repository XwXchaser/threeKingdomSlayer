---
id: kd_1aab7a24-acd9-4097-8a93-e2442d2f50b9
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# Enemy 1021 v8 形象视频替换：交接与发起准备

## 0. 入口与当前状态

**任务**：1021 新画风身份（v8）已由用户于 2026-10-25 验收，本文件准备「用 v8 形象替换 1021 既有动作视频」这条线的全部前置工作：复盘 1011 的形象替换案例（重点是面部表情的塑造手法）、实测 1021 的源片与工程落点、准备唯一身份输入、并给出第一条任务（持盾 Idle）的完整提交规格。

**本轮已做**

- 找到并复盘 1011 的形象替换案例（v5→v1/v2/v3 三代换人视频，以及 v11 形象在 idle/walk/attack/dead/受击上的落点），提取可复用结论（§1）。
- 实测 1021 现有 10 段源视频的规格、底色、主体包围盒与运动节拍（§4、§5）。
- 实测 1021 现有精灵、Clip、Controller 状态与部署几何（§3）。
- 准备好 v8 归一化上传副本（唯一输入，§6）。
- 写好第一条任务的参数、参考职责、双语提交文本与验收闸门（§7）。
- **2026-10-25 用户拍板「按方案发起 Idle」，已按 §7 规格提交并落盘**：task `task_c9c2f36bb50542c7a80ec94351bbb1cb`，成片与实测见 §11。
- **2026-10-25 用户判定「角色形象（头身比例）仍是最主要问题」**，启动 v9 头体量修正：诊断见 §12.1，本地零成本草稿见 §12.3，已批准的出图因服务商余额耗尽未执行（**未扣费**），见 §12.2。
- **当前停点：2026-10-25 用户指示「先不生成了，搁置一下，等我充钱」**。本轮之后不再提交任何生成请求（出图与视频都不发），等余额恢复后按 §12.5 续接。

**本轮未做（明确边界）**

- 除 §11 记录的这一次持盾 Idle 任务外，**未再发起其他生成**；未选帧、未抠图、未对齐、未补边、未建 Clip、未改 `Assets/` 下任何资产、Prefab、Controller 或场景。
- 未对 v8 原图做任何裁切、改色或本地修饰（§6 的副本是**另存的新文件**，v8 原图字节未动）。

**分工边界（沿用用户既定约定）**

| 环节 | 负责方 |
| --- | --- |
| 方案、输入准备、生成词、发起与回填、选帧、抠图 | 本侧 |
| 尺寸/脚底对齐、补边、Clip 创建、Prefab/Controller 接线、第一关接入 | 接入侧 |
| 画面美术验收、表情是否成立 | 用户（代理不代为判定） |

**身份基准（下游唯一身份输入）**

```text
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v8\enemy1021_v11_style_idle_v8.png
1024x1024 RGB PNG, Color Type 2, 1,853,973 bytes
SHA-256 9b879ca2eddeebfe3e7b1ab56716df998f73bcf8cb6a39a12a550b3d4de99efe
```

v8 的不可变项（盔形、脸、服饰、盾、方向、画法、比例）见 `plan/enemy1021-v11-style-idle-replacement-handoff.md` §17.3，本文不重复抄写，只在需要提示词时引用。

---

## 1. 1011 形象替换案例复盘：六条可复用结论

案例出处：`plan/enemy1011-style-locked-dao-generation-archive.md` §15–§17（v5 换人视频 v1/v2/v3）、`plan/enemy1011-style-locked-dao-generation-handoff.md` §24–§26（v11 形象在 idle/walk/attack/dead/正面受击/左右受击的落点）、`skill/workflows/character-hit-animation-video-workflow.md` §13–§14。

| # | 结论 | 出处与实测 |
| --- | --- | --- |
| 1 | **`reference_video` 与 `reference_image` 职责不可互换**：视频＝动作＋镜头来源（身体、武器、头部轨迹、倾角、取景、脚底线）；图＝身份、服饰、画法 | 归档 §16.2；1011 attack/walk/dead/受击全部沿用同一分工 |
| 2 | **单变量纪律**：模型、时长、分辨率、比例、seed、两个输入文件全部与上一版字节一致，只改提示词 | 归档 §16.2（A 方案）、§17.2（v3 只改 P4 段，1,033→1,529 字符） |
| 3 | **表情必须写进第一次提交**：「只改脸／只改 X」这类增量指令会被当成「保持稳定」，结果是全程一张不变的死脸 | 技能 §13.3；1011 walk v1 死脸 → v2 张嘴 → v3 闭嘴仍死 → v4（一次性配方，逐帧脸确有差异）才成立 |
| 4 | **死脸判据**：把脸段遮住后，每一拍的脸必须有不同的答案；`may / slightly / by a hair` 这类弱量词一律不算数，事件必须可量测 | 技能 §14.1；attack v2 的五拍阶梯是可读正例（张口窗口 f29–f47，峰值 +4,176 口腔暗红 px） |
| 5 | **嘴的权重预算**：强度全部由眉、眼、下颌承担；嘴要小到永远不是视觉焦点；禁止用后仰/抬颌/前推下颌等替代动作补表情 | 归档 §17.2（v3 窄幅呲牙）、§17.7（仍偏大就整段删掉改成全程闭口） |
| 6 | **提交文本的哈希陷阱**：提示词文件常是 CRLF，直接按原始字节提交会得到与文档口径不同的字符数与哈希；必须先 CRLF→LF 并去掉末尾单个换行 | 归档 §17.5.1（v3：3,459 B 文件 vs 3,447 字符口径，SHA `bdee94b6…1175`） |

补充两条与 1021 直接相关的实测教训：

- **正文里写死的背景键值不能约束输出键值**：1011 v2 词里写死 `RGB(0,159,62)` 且 `flat and even`，成片 ΔG 仍是 −26…−19，与 v1 同向同量级（归档 §16.6）。所以键值只能写、不能信，生成后必须按 G2 复测并升级为待决项。
- **人放大 ⇒ 脚被推到画面下方**，一条约束可同时修两处（归档 §16.1 ⑤：源片主体占比 0.680 / 底边 776，成片 0.798 / 860）。

---

## 2. 面部表情的塑造方法（从 1011 案例提炼）

### 2.1 1011 三种表情写法的实测对照

| 案例 | 写法 | 结果 | 教训 |
| --- | --- | --- | --- |
| walk v1 | `Keep the face as a cold advance… jaw set, mouth closed in every frame. The expression may tighten slightly…` | 全片无变化（死脸） | 「状态保持」＋弱量词＝死脸 |
| walk v2 | 追加「下颌松开」 | 静立时张嘴，方向写反 | 呼吸／松弛类措辞会让模型去驱动嘴 |
| walk v3 | 只传视频 + 「嘴闭着」 | 闭嘴但仍无变化 | 增量指令无效 |
| walk v4 | 回原始源片＋原始身份图，一次性写入跟随步态节拍的表情阶梯 | 逐帧脸确有可见差异 | 唯一成立路径 |
| attack v2 | 五拍阶梯：冷脸闭嘴→刀上行走压眉窄眼绷颌→顶点张口怒吼→挥砍保持→收势闭回硬线 | 方向成立，但「过于突出」（张口占脸可读窗口 63%） | 幅度与时长都是词指定的，要改只能改词 |
| attack v3 | 只改 P4：幅度改「窄幅无声呲牙」（唇分开一线、无口腔暗腔）、时长改「只在顶刀点一瞬」、强度转移给眉眼颌 | 方向正确 | 单变量改一段，不动其余五段 |
| 正面受击（纯生成） | 受力瞬间眼缝挤紧、眉心下压、下颌咬死、嘴全程闭着；受力后眉松开一线 | 模型仍画成「大张嘴龇牙」，并把头抬起来 | 写死也可能被违背，验收要看头位而不只看脸 |
| dead（纯生成） | 写「嘴闭着、不露齿、不仰头」 | 模型画成大张嘴的痛呼（用户接受） | 死亡场景读作临死呼喊可接受 |

### 2.2 可直接套用的阶梯模板

```text
Animate the face only, on top of the preserved body motion. The face is a live changing performance, not a static painted mask.
<第1拍：起始状态，写具体部件状态>
<第2拍：随动作推进，写部件位移方向>
<第3拍：顶点，一次性可量测事件>
<第4拍：回落>
<第5拍：回到起始状态，保证循环闭合>
All the intensity must come from <眉/眼/颌>. Never open the jaw, never show the teeth, never show the inside of the mouth, never shout.
The expression must change gradually and continuously; it must never snap from one expression to another in a single frame, and it must never stay frozen for a long stretch and then jump.
```

### 2.3 落到 1021 盾兵守阵 idle 的设计

- 事件类型：**不是张口事件，而是「压眉＋收窄眼」的紧度事件**（§2.1 的教训：强度放眉眼颌，嘴永闭）。
- 绑定节拍：源片是一次慢呼吸循环，吸气峰值在 f37–f44（§5）。表情峰值必须落在这个吸气峰，而不是均匀分布。
- 嘴：全程闭紧，任何一帧都不张嘴、不露齿、不出现口腔暗腔（1011 v3 之后的口径）。
- 头位：禁止为了露脸而抬头／转脸；`the expression must be read on the face at the angle it already has`。
- 闭环：末帧脸回到首帧状态，保证 0.96 s 循环无跳变。

---

## 3. 1021 现状：资产、剪辑与部署几何（实测）

### 3.1 精灵分组（`Assets/Sprites/Enemy/Enemy1021/`，共 79 张 PNG）

| 组 | 张数 | 现状 |
| --- | --- | --- |
| `Enemy_1021_attack1..12` | 12 | 旧美术 |
| `Enemy_1021_walk1..6` | 6 | 旧美术 |
| `Enemy_1021_idle1..4` | 4 | 旧美术 |
| `Enemy_1021_shieldGuardHit1..6` | 6 | 旧美术 |
| `Enemy_1021_shieldBreak1..6` | 6 | 旧美术 |
| `Enemy_1021_hitFlash1..6` | 6 | 旧美术 |
| `Enemy_1021_hitRight1..6` | 6 | 旧美术 |
| `Enemy_1021_dead1..3` | 3 | 旧美术 |
| `Enemy_1021_cowardIdle1..6` / `cowardHit1..2` / `cowardFall1..6` / `cowardGetup1..5` / `cowardLaunched1..5` / `cowardDead1..6` / `shieldDrop` | 1–6 | 旧美术（无盾家族） |

### 3.2 Clip 与 Controller

- Clip：`Enemy_1021_Idle`（30 fps、stopTime 0.96、loop、4 个精灵键 0/0.24/0.48/0.72，末键重复 idle4，并带 `ShieldVisual` null 曲线）、`Walk`（0.6 s、6 帧）、`ShieldGuardHit`（0.3 s、6 帧）、`ShieldBreak`、`HitFlash`、`HitRight`、`Dead`、`Coward*` 系列。
- Controller 状态：`Idle / Walk / Attack / ShieldGuardHit / ShieldBreak / HitFlash / HitRight / Dead / CowardIdle / CowardHit / CowardDead / CowardGetup / CowardFall / CowardLaunched / CowardTransition / Launched_Rise / Launched_Fall`。
- **每个状态都带 `ShieldVisual` 曲线**：新帧接入时必须保留「持盾时显示、破盾后清空」的既有约定，不能只换 `m_Sprite` 曲线。

### 3.3 部署几何（实测，接入侧必须沿用）

| 项 | 实测值 |
| --- | --- |
| 源帧画布 | 960×960 RGBA（抠图后） |
| 补边规则 | **固定偏移 `(74, 91)` 零重采样**补到 1108×1108（`74+960=1034`，右侧余 74；`91+960=1051`，下方余 57） |
| 部署后主体高 | idle1–4 实测 **549 / 559 / 569 / 559** px |
| 部署后脚底行 | 实测 **870**（= 源片脚底 781 + 91 ≈ 872，差 1–2 px 属抗锯齿） |
| 导入设置（`Enemy_1021_idle1.png.meta`） | Single / PPU **16** / alignment 0 / pivot **(0.5, 0.5)** / maxTextureSize 2048 / filterMode Point / alphaIsTransparency / 无压缩 / 无 Mipmap |
| Prefab scale | 0.090161（1021 prefab） |

> 注意：1021 用**中心 pivot**，与 1011 的 pivot `(0.6539, 0.5487)` 不同。接入侧照 1011 的脚本写会错位。

---

## 4. 可用源片实测（10 段，960×960 / 24fps）

| 源片 | 帧 / 时长 | 底色实测 | 主体 bbox（f0） | 备注 |
| --- | --- | --- | --- | --- |
| `enemy1021_shield_idle_v1.mp4` | 97 / 4.042 s | 绿 `(0,172,57)` | x[269,681] y[230,781] h=552 | 持盾 idle，一次呼吸循环；旧美术自带红缨与闭面罩 |
| `enemy1021_shield_walk_v1.mp4` | 97 / 4.042 s | 绿 `(0,172,57)` | x[270,681] y[230,781] | 步态周期 ≈ 11 帧 |
| `enemy1021_shield_attack_v1.mp4` | 97 / 4.042 s | 绿 `(0,172,57)` | x[269,681] y[230,781] | 盾撞；主体高波动 504–666，x 最远到 823 |
| `enemy1021_shield_break_v1_6s.mp4` | 145 / 6.042 s | 绿 `(0,147,59)` | x[312,639] y[279,720] | 破盾，独立掉盾接管 |
| `enemy1021_hitflash_v1.mp4` | 97 / 4.042 s | 绿 `(0,159,60)` | x[270,681] y[230,781] | 正面受击 |
| `enemy1021_dead_v1.mp4` | 97 / 4.042 s | 绿 `(0,160,60)` | x[270,681] y[230,781] | 持盾死亡 |
| `enemy1021_dead_v2.mp4` | 97 / 4.042 s | 绿 `(0,159,58)` | x[270,681] y[230,781] | 备用 |
| `enemy1021_shield_guard_hit_v1_6s.mp4` | 145 / 6.042 s | **灰 `(223,224,224)`** | x[269,681] y[230,781] | **灰底**；受力峰值 f021–f026，回弹 f090–f101 |
| `enemy1021_hit_right_v1.mp4` | 97 / 4.042 s | **灰 `(223,223,226)`** | x[269,680] y[230,780] | **灰底**；且命名与游戏语义相反（见 §8） |
| `enemy1021_hit_right_v2.mp4` | 97 / 4.042 s | **灰 `(228,228,228)`** | x[270,680] y[230,781] | **灰底**；用户已把「角色视频用浅灰底」判为严重流程问题 |

文件 SHA-256 前 16 位（复核用）：

```text
enemy1021_shield_idle_v1.mp4        808593f9cbc5b913
enemy1021_shield_walk_v1.mp4        2d20f36dc068c73c
enemy1021_shield_attack_v1.mp4      ea41b8a47b01b4d9
enemy1021_shield_guard_hit_v1_6s.mp4 cefcc521c18651ca
enemy1021_shield_break_v1_6s.mp4    2ff04f2ee8d19c38
enemy1021_hit_right_v1.mp4          75a16f91bf6409f9
enemy1021_hit_right_v2.mp4          adbe1efbd2539b17
enemy1021_hitflash_v1.mp4           a36baeab0cf9fd2e
enemy1021_dead_v1.mp4               8e55e51942a09548
enemy1021_dead_v2.mp4               c2b444a358d246f2
```

---

## 5. 运动节拍实测（表情阶梯的绑定依据）

**持盾 idle（`enemy1021_shield_idle_v1.mp4`）**：脚底行 **781 全程 97 帧零漂移**；头顶行（= 胸肩起伏的代理）：

| 帧区间 | 头顶行 | 读法 |
| --- | --- | --- |
| f0–f2 | 230 | 起始静立（最低胸位） |
| f3–f36 | 230→210 | 吸气上升 |
| **f37–f44** | **208** | **吸气峰值（t ≈ 1.54–1.83 s）** |
| f45–f72 | 210→220 | 呼气下沉 |
| f73–f80 | 220 | 次级平台 |
| f81–f96 | 220→230 | 回位，末帧与首帧一致 |

既有部署槽位来自旧片 f001 / f025 / f049 / f073（1-based，即零基 0 / 24 / 48 / 72），对应「起始 / 上升中 / 下沉后 / 回位中」四个姿态。新片的选帧槽位应继续落在同样的相位上。

**持盾 walk**：步态周期 ≈ 11 帧（头顶行极小值出现在 f9–10、f20、f31、f42、f53、f64、f75、f87）；既有部署槽位取 f040–f050（1-based），即一个完整步幅内每 2 帧取一。

---

## 6. 已准备的唯一输入：v8 归一化上传副本

### 6.1 为什么需要归一化

1011 实测：参考图里主体占画布的比例会被原样带到成片（v5 立绘 0.78 → 成片 0.798，而源片是 0.68），所以**先把身份图缩到与源片一致的主体占比再上传**，避免"生成完才缩"的二次重采样（1011 §24.1）。1021 同理：

| 项 | 源片（idle f0） | 1024 画布上的等效值 | v8 原图 |
| --- | --- | --- | --- |
| 主体高 / 画布高 | 552 / 960 = **0.5750** | **589** | 915 / 1024 = 0.8936 |
| 脚底行 | 781 / 960 = 0.8135 | **833** | 980 |
| 主体中心 x | 475 / 960 = 0.4948 | **507** | 481 |

### 6.2 产物与实测

```text
C:\Users\steam\Pictures\gptGen\enemy1021_v11_video_v1\input_identity_v8_idle_589.png
1024x1024 RGB PNG, 350,038 bytes
SHA-256 6a3e1ef3542f8d831f9dae6060186308333985d43efa44eb12d174bce4526ae8
manifest: C:\Users\steam\Pictures\gptGen\enemy1021_v11_video_v1\input_identity_v8_idle_589.manifest.json
```

| 项 | 值 |
| --- | --- |
| 来源 | v8 原图裁切 `[100,66,862,980]` → LANCZOS 缩放 **0.64372** → 491×589 → 贴到 `(262,245)` |
| 副本实测主体 bbox | x[262,752] y[245,833]，**主体高 589、主体宽 491** |
| 主体占画布比 | **0.5752**（源片 0.5750，差 0.0002） |
| 脚底行 / 主体中心 x | **833 / 507**（与源片等比一致） |
| 背景键 | 统一为 **RGB(0,177,64)**（v8 原背景 G 138–160 不均匀，已用连通域法整体替换） |
| 替换像素 | 862,150 背景像素改为键色；186,426 像素原样保留（主体像素未改） |
| 四角采样 | `(0,177,64)` × 4，std = 0 |
| v8 原图 | **未改动**（哈希仍为 `9b879ca2…99efe`） |

### 6.3 预览（仅供审阅，绝不上传）

```text
C:\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\enemy1021_v11_video_v1\UPLOAD_REVIEW_three_up.png
    （左 v8 原图 / 中 上传副本 / 右 源片首帧）
C:\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\enemy1021_v11_video_v1\UPLOAD_REVIEW_equal_height_src_vs_v8.png
    （左 源片首帧主体 / 右 v8 主体，等高 420 px 对照）
```

等高对照可见的**必须在新词里禁掉的源片旧设计**：红色盔缨、闭面罩＋眼缝、亮银板甲、金边、红围巾。持盾方向两侧一致（解剖学右手持盾、盾在画面左），因此动作迁移方向正确。

### 6.4 背景键取值的理由

- 1021 历史批次用的是 `RGB(0,177,64)`（旧 `enemy1021_shield_hold_v3_green_v1.png` 即此键色），且用户已把 **RGB(0,177,64)** 记为 1021 角色视频的常用已验收色。
- 1021 三段绿幕源片实测键色为 `(0,172,57)`，与 `(0,177,64)` 仅差 G −5，因此副本键色与源片键色天然接近，不会出现「两个参考两个绿」。
- 1011 批次用的是 `(0,159,62)`；两批键色不同，**不跨批混用**。

---

## 7. 第一条任务：持盾 Idle v8（完整发起规格，待用户拍板）

### 7.1 目标与验收

用 v8 形象重做 1021 的持盾待机循环。成片必须：动作与源片一致（呼吸循环、脚底零漂移）、身份是 v8（敞脸、无缨、无银甲、无金边）、脸有随呼吸节拍的紧度变化且**全程闭嘴**、主体尺寸与脚底线与源片一致、背景为均匀绿。

### 7.2 输入与职责（不可互换）

| 角色 | 文件 | 职责 |
| --- | --- | --- |
| `reference_video` | `C:\Users\steam\Videos\doubaoVideo\enemy1021_shield_idle_v1.mp4`（2,000,007 B，SHA-256 `808593f9cbc5b913…`） | 只提供动作与镜头：呼吸循环的节拍、姿态、落脚点、头部轨迹与倾角、取景、脚底线 |
| `reference_image` | `C:\Users\steam\Pictures\gptGen\enemy1021_v11_video_v1\input_identity_v8_idle_589.png`（350,038 B，SHA-256 `6a3e1ef3…a569a`） | 只提供身份：v8 的脸、盔、服饰、盾、持盾方向、画法 |

不追加第三张图；不上传 v8 原图（旧比例会与副本竞争）、1011 v11、旧 1021 素材、场景图、v5–v7。

### 7.3 参数与费用

```text
endpoint: POST https://direct.reachapi.ai/v1/vids/create
model: seedance-2-fast
duration_seconds: 4
resolution: 720p
aspect_ratio: 1:1
generate_audio: false
seed: -1
watermark: false
input.content: [text, video_url+reference_video, image_url+reference_image]
上传接口: POST https://file.reachapi.ai/file/uploads（multipart/form-data，字段名 file）
轮询: GET https://direct.reachapi.ai/v1/tasks/{task_id}
```

- 预估费用：**≈ 0.57321**（1011 同参数同形态实测：`has_video_input=true` + 720p，`cost.spend=0.57321`；以本次响应 usage/cost 为准，不预填）。
- 凭据只从环境变量 `REACH_API_KEY` 读取；不得写入脚本、记录、文档或回复。
- 上传注意：1011 实测 `Python-urllib` 的 UA 会被 `file.reachapi.ai` 的 Cloudflare 以 **HTTP 403 / error code 1010** 拒绝；改用浏览器 UA 即可成功（该次未创建任务、未计费）。

### 7.4 实际提交文本

已落盘（LF 行尾、无 CR、无末尾换行）：

```text
Library/Locus/tmp/enemy1021_v11_video_v1/enemy1021_shield_idle_v8_v1.prompt.txt
5,734 字符
SHA-256 2510c48854fdd59e8927946594a3765f1ac5883ae8d2fc1aed60005dea1b579d
```

六段结构与 1011 归档 §16.3／§17.4 一致：**P1 动作锁定 → P2 身份替换 → P3 尺寸/地线/盾的刚性 → P4 表情阶梯 → P5 禁止项 → P6 背景键与特效禁令**。

英文逐字提交文本（内容见上面的 `.prompt.txt` 文件，提交时以该文件为准；此处按段列出，段落之间为**空行**）：

**P1**
```text
Use the uploaded reference video as the motion and camera source. Preserve every body movement, timing, pose, foot placement, head path and head tilt of the person in that video frame by frame. Do not add, remove, retime or speed up any action, and do not change the head angle, the torso bend, the stance or the camera framing in any frame. The reference video is only a motion source: do not copy its red plume, its closed faceplate with eye slits, its bright silver plate armour, its gold trim or its red scarf.
```

**P2**
```text
Replace that person with the character from the reference image: short stocky proportions, a large head on broad shoulders, a compact torso, short sturdy legs, thick hands and heavy boots. He wears a low segmented arched iron helmet with broad plate seams, a slightly raised central rib that ends at a narrow riveted brow band, short overlapping neck-guard plates behind the ears, and narrow side plates that cover both ears while leaving the whole face, both cheekbones and the jaw exposed. He has a dark indigo cross-collar tunic with a pale inner collar, a warm brown sleeveless leather cuirass that ends at the waist, a broad plain leather belt with a small iron buckle, short leather bracers, a separate cloth hem below the belt, dark loose trousers, calf wraps and sturdy short boots. He holds one tall rectangular wooden shield on screen-left, with vertical timber boards, a plain iron rim and a few rivets, gripped by the anatomical right hand; the anatomical left hand on screen-right is empty at his side. Keep the identical character in every frame with no costume change, no weapon change and no identity drift.
```

**P3**
```text
Keep the fighter at exactly the same size on screen and standing on exactly the same ground line as in the reference video. Do not make him larger, do not shrink him, and do not move his feet up or down in the frame. Do not change the camera zoom. Keep the shield at exactly the same position, angle and size relative to his body as in the reference video. The shield moves as one rigid object: its planks, iron rim, rivets, thickness, outline and orientation stay consistent, and only a tiny passive follow-through with the breathing shoulder and elbow is allowed. The gripping hand, fingers, forearm and handle stay concealed behind the shield throughout, and are never revealed to demonstrate the grip. No shield raising, lowering, swinging, bashing, blocking, releasing or side switching.
```

**P4（表情阶梯，绑定 §5 的呼吸节拍）**
```text
Animate the face only, on top of the preserved body motion. The reference video is one slow breathing cycle: the chest and shoulders rise from the opening frames to a peak around the middle of the first half of the clip, hold briefly, then sink back to the starting height by the closing frames. The face is a live changing performance, not a static painted mask. In the opening frames he holds a cold alert scowl: heavy straight brows pulled slightly down, small focused eyes fixed on the enemy in front of him, a firmly closed mouth and a set jaw. While the chest rises, the brows press down and a little closer together, the eyes narrow by a visible step and the jaw firms, while the lips stay pressed into a single closed line. At the peak of the inhale the brows reach their lowest point and the eyes reach their tightest; the mouth stays closed at that peak, the lips never part, no teeth and no dark gap behind the lips. As the chest sinks, the brows ease back up one step, the eyes open back to their opening-frame focus, the jaw relaxes one step, and the mouth stays closed. By the closing frames the face has returned exactly to its opening-frame state so the loop closes. All the intensity must come from the brows, the eyes and the jaw, and the mouth must stay closed and small enough that it is never the focal point. Never open the jaw, never show the teeth, never show the inside of the mouth, never shout, never hiss, and do not compensate with a head throw-back, a lifted chin, a thrust jaw or any extra neck movement. The expression must change gradually and continuously through those stages; it must never snap from one expression to another in a single frame, and it must never stay frozen for a long stretch and then jump.
```

**P5**
```text
Do not let him grin, smile, laugh, look cheerful or look relaxed. Do not let him snarl with an open mouth. Do not change the head tilt, turn or lift from the reference video in order to show the face; the expression must be read on the face at the angle it already has. Keep the helmet, the tunic, the cuirass, the belt, the bracers, the boots and the shield in the same position as in the reference video.
```

**P6**
```text
Keep the uniform opaque green background RGB(0,177,64), flat and even across every pixel of every frame. No floor, no contact shadow, no scenery, no text, no watermark, no checkerboard, no gradient, no vignette, no brighter patch behind him, no light falloff, no change of light and no change of colour in any part of the frame. No impact flash, no white flash, no shockwave, no impact lines, no speed lines, no motion blur streaks, no dust, no debris, no sparks, no smoke, no blood, no aura, no magic glow, no rim light, no bloom, no lens flare, no star glint, no sparkle on the iron, no light rays, no halo around the fighter. No camera zoom, pan, tracking, rotation, shake or reframing. No whole-character travel, no stepping, no foot sliding, no hopping, no weight transfer between the feet, no whole-body bobbing, no scaling, no inflation, no sideways leaning, no side view, no three-quarter view and no mirroring. No second person, no extra limbs, no duplicated weapons and no armour pieces that are not in the reference image. No freeze-frame animation, no rewind, no crossfade, no pose snapping, no particles, no dust, no text and no UI.
```

中文对照（审阅用，不提交）：

- **P1**：把上传视频当动作与镜头来源，逐帧保留身体动作、时序、姿态、落脚点、头部轨迹与头部倾角；不增加、不删除、不重排、不加速任何动作，任何一帧都不改头部角度、躯干前倾、站姿或取景。视频只当动作源：不复制它的红缨、闭面罩眼缝、亮银板甲、金边与红围巾。
- **P2**：把视频里的人换成参考图角色：短壮比例、大头宽肩、紧凑躯干、短腿、厚手厚靴；低矮分片拱顶铁盔（板材接缝、中央铁脊止于窄铆钉额箍、耳后短叠片护颈、两侧护片遮耳但脸/颧骨/下颌全露）；暗靛蓝交领布衣＋浅色内领；暖棕无袖皮甲止于腰；宽素皮腰带＋小铁扣；短皮护腕；腰带下独立布下摆；深色宽裤、绑腿、短靴；解剖学右手持一面高矩形木盾（竖木板、素铁边、少量铆钉）在画面左侧，画面右侧左手空置。每帧都是同一个角色，不换服装、不换武器、不漂移身份。
- **P3**：人物在画面中的大小与脚底线必须与参考视频完全一致：不放大、不缩小、不让脚在画面里上下移动、不改镜头变焦；盾相对身体的位置、角度、尺寸与源片一致；盾是刚体，板条/铁边/铆钉/厚度/轮廓/朝向保持不变，只允许随呼吸肩肘的极小被动跟随；握盾的手、手指、前臂与握把全程藏在盾后，不为展示握法而露出；不举盾、不落盾、不挥盾、不盾撞、不格挡、不松手、不换手。
- **P4**：只动画面部，叠加在已保留的身体动作之上。源片是一次慢呼吸循环：胸肩从开头帧上升，在前半段中段附近到峰值，短暂停留，随后在结尾帧前回落。脸是活的表演，不是贴上去的死面具：开头帧是冷峻警觉的皱眉——厚直眉略压，小眼盯住前方敌人，嘴紧闭，下颌绷住；胸上升时眉压低并略微靠拢，眼睛可见地收窄一线，下颌收紧，唇始终抿成一条闭线；吸气峰值处眉到最低点、眼收到最紧，此时**嘴仍然闭着**，唇绝不分开，不露齿，不出现口腔暗隙；胸下沉时眉松开一线，眼睛回到开头的聚焦，下颌放松一线，嘴仍闭着；结尾帧脸回到开头帧状态，循环闭合。强度全部来自眉、眼与下颌，嘴始终闭着且小到永不成为视觉焦点；绝不张大下颌、不露齿、不露口腔、不喊、不嘶，不用后仰/抬颌/前推下颌或任何额外颈部动作补偿。表情必须逐级连续变化，不得在一帧内跳变，也不得长时间冻结后再跳变。
- **P5**：不许咧嘴、微笑、发笑，不许轻松放松脸；不许张嘴龇牙；不得为了露脸改动参考视频的头部倾角/转侧/抬降，表情按头部既有角度读；盔、衣、甲、腰带、护腕、靴、盾的位置与源片一致。
- **P6**：背景保持均匀不透明纯绿 RGB(0,177,64)，逐帧逐像素平匀；无地面、无接触阴影、无场景、无文字、无水印、无棋盘格、无渐变、无暗角、无人物后方亮斑、无光衰减、无光照变化、无局部变色；特效禁令 26 项（冲击闪光、白闪、冲击波、速度线、动态模糊条、尘土、碎片、火花、烟、血、光环、魔法辉光、边缘光、泛光、镜头耀斑、星芒、铁件闪光、光线、人物光环）全部不出现；无镜头推拉摇移转震、无重新构图；人物不整体位移、不迈步、不滑脚、不跳、不换脚、不全身上下浮动、不缩放、不充气、不侧倾、不侧视、不四分之三视、不镜像；第二人、多余肢体、重复武器、参考图里没有的甲片一律不出现；无定格、无倒带、无叠化、无姿态跳变、无粒子、无尘土、无文字、无 UI。

### 7.5 输出与留档

```text
成片: C:\Users\steam\Videos\doubaoVideo\enemy1021_shield_idle_v8_v1_4s_720p.mp4
请求: Library/Locus/tmp/enemy1021_v11_video_v1/enemy1021_shield_idle_v8_v1.request.json
状态: Library/Locus/tmp/enemy1021_v11_video_v1/enemy1021_shield_idle_v8_v1.status.json
提示词: Library/Locus/tmp/enemy1021_v11_video_v1/enemy1021_shield_idle_v8_v1.prompt.txt
```

发起前确认：成片与两个记录文件都不存在（不覆盖旧 1021 任何成片）；两个输入的哈希与 §7.2 一致；提示词按 **LF、去末尾换行** 的口径取（§1 第 6 条）。下载后必须做：>1 KB、`ftyp`、顶层 box 边界、PyAV 全帧解码，再原子改名。

### 7.6 验收闸门

**代理侧只报数，不下美术结论：**

| 闸门 | 判据 | 基线 |
| --- | --- | --- |
| G1 输入片基（已测） | 上传副本四角中位 = `(0,177,64)`，std = 0；主体占画布 0.5752 ≈ 源片 0.5750 | 见 §6.2 |
| G2 输出片基 | 逐帧四角 64×64 中位对 `(0,177,64)` 的 ΔR/ΔG/ΔB 范围、最差帧号、首尾帧值；并与源片自身键 `(0,172,57)` 对比 | 本项目长期实测输出比请求键低 **5–20 G**；词里写死键值**不保证**输出一致，必须实测并升级为待决项 |
| 几何 | 主体高 out/src 比值（目标 ≈ 1.00）；脚底行漂移（源片为 0 漂移，成片应 ≤ ±20 px）；主体中心 x 漂移 | 1011 v1 曾出现主体放大 21.6%＋底边 +68 px，本轮要在词里钉死并复核 |
| 口腔代理 | 逐帧统计人物遮罩内上部 30% 的「暗红口腔色」像素（45≤R≤150、G≤80、B≤85、R≥G+15），与「全程闭嘴」基线比 | 本任务预期全程 ≈ 0 增量；若出现明显增量，说明嘴被画开 |
| 首尾闭合 | 首末帧 alpha 主体 bbox 是否一致 | 用于 0.96 s 循环闭合 |

**由用户目视判定（代理不代判）：** 脸是否有可见的紧度变化、是否全程闭嘴、是否仍是 v8（敞脸、无缨、无银甲、无金边、红围巾）、盾的方向与持法、动作是否与源片一致、主体大小与脚底线是否与源片一致。

### 7.7 若不通过（升级路径，来自 1011 案例）

1. 脸是死的 → 判定为提示词问题，**回原始源片＋原始身份图**重写一份完整提交，不是在成片上叠一轮（技能 §13.1）。
2. 嘴被画开 → 只改 P4 一段（其余五段逐字不动），把嘴的描述改成「闭紧、唇线绷紧、绝不分开」。
3. 身份被源片污染（红缨/银甲/金边回来） → 只改 P1/P2 两段并加强禁令。
4. 主体放大或脚底下移 → 只改 P3 的尺寸与地线段。
5. 背景键偏低 → 按 G2 升级为待决项（接受／换参考重生成／只在交付物侧处理），不静默结案。
6. 任何重发都要重新取得当次明确批准；一次授权只发一个任务，不重试、不降档、不换模型。

---

## 8. 后续动作顺序与各自的坑

建议顺序（与 1011 的 §25.4 同逻辑：先换敌人常态出现最多的动作）：**idle → walk → attack → shieldGuardHit → shieldBreak → hitFlash → hitRight → dead → 无盾胆怯家族**。

| 动作 | 源片 | 底色 | 已知坑 |
| --- | --- | --- | --- |
| walk | `enemy1021_shield_walk_v1.mp4` | 绿 `(0,172,57)` | 步幅周期 ≈11 帧，表情阶梯须绑步态节拍；1011 walk 的表情试过三轮都失败、一次性配方（v4）才成立 |
| attack | `enemy1021_shield_attack_v1.mp4` | 绿 `(0,172,57)` | 盾撞时主体前移与放大（源片本身主体高 504→666）；1011 attack 的表情峰值必须落在顶刀点这类脸可读窗口 |
| shieldGuardHit | `enemy1021_shield_guard_hit_v1_6s.mp4` | **灰 `(223,224,224)`** | 灰底成片已被用户判为流程问题；需先本地换绿或改用其他方案；受力峰值 f021–f026、回弹 f090–f101 |
| shieldBreak | `enemy1021_shield_break_v1_6s.mp4` | 绿 `(0,147,59)` | 盾淡出＋独立掉盾接管；渐隐帧的半透明边缘不能被硬阈值抠掉 |
| hitFlash | `enemy1021_hitflash_v1.mp4` | 绿 `(0,159,60)` | 正面受击；1011 的教训是「闭嘴咬牙」会被画成大张嘴龇牙 |
| hitRight | `enemy1021_hit_right_v1/v2.mp4` | **灰 `(223,223,226)` / `(228,228,228)`** | **命名与游戏语义相反**：该片里矛从画面右侧刺入、身体倒向画面左 → 按 1011 §26.12.2 的口径（身体倒向哪侧＝哪个动作）它其实对应游戏的 **HitLeft**；1021 的 `hitRight` 精灵正是从它抽的帧。动手前必须先核这条，别自行推导 |
| dead | `enemy1021_dead_v1/v2.mp4` | 绿 `(0,160,60)` / `(0,159,58)` | 死亡场景的「闭口」会被模型画成痛呼张口，1011 已被用户接受为可接受偏差 |
| 无盾胆怯家族 | `enemy1021_coward_*.mp4` | 绿/灰混 | 这批是旧美术下的独立资产链（cowardIdle/Hit/Fall/Getup/Launched/Dead），v8 身份下要不要整套重做，需用户拍板 |

另：**灰底源片（shieldGuardHit / hitRight v1 / v2）** 若要使用，须先做「保留原片 + 逐帧实测底色 + 本地分离背景换绿」的副本并交用户验收；不得在生成时直接用灰底（用户 2026-10-13 已明确）。

---

## 9. 后处理与接入边界

**本侧负责（成片之后）**

1. 落盘校验：>1 KB、`ftyp`、顶层 box、PyAV 全帧解码、哈希回读。
2. G2/G3 数值：逐帧背景 Δ、主体色配准比对、口腔探针、几何漂移。
3. 选帧：在成片自身取帧，按 §5 的相位与 §7.6 的闸门筛；**先出预览包给用户验收，未验收不得写入 `Assets/`**（1011 曾因此回滚过）。
4. 抠图：SVL Chroma，按本批实测键色标定容差（1021 历史用 T=79 / T=80，halo 1、softness 16、半透明转不透明），并核 holes / leak / semi / green>8 / 阴影残留。
5. 交付物：透明帧 + 预览包（`00.png…` 按播放顺序、`anim.gif` 按游戏内键间隔、`contact.png`、`manifest.json`）。

**接入侧负责**：尺寸/脚底对齐（1021 用单一全局 k + 钉脚底 870）、固定偏移 `(74,91)` 补边到 1108×1108、Clip 创建（保留 `ShieldVisual` 曲线）、Controller 状态接线、Prefab 与第一关接入。`.meta` 与 GUID 不动；1021 用中心 pivot (0.5,0.5)、PPU 16。

---

## 10. 纪律与待决项

**纪律**：一次授权一个任务；发起前复述唯一输入、参数、完整提交文本与输出路径并取得当次明确「发起生成」；失败/超时/结果不明保留记录后停止；不自动重试、不降档、不换模型、不叠轮重塑；凭据只从环境变量读取；不把成片当下一轮的 `reference_video`。

**待用户拍板（不自行决定）**

1. ~~是否按本文件 §7 的规格发起第一条任务~~ → **2026-10-25 已拍板「按方案发起 Idle」并完成提交，见 §11。**
2. 背景键确认用 `RGB(0,177,64)`（1021 批次已验收色，与源片实测键差 G −5）—— **已用于 Idle v1，成片实测键漂移见 §11.3，待用户验收时一并拍板。**
3. 表情强度口径：守阵 idle 采用「全程闭嘴、强度只由眉眼颌承担」的方案，还是允许在吸气峰值出现一次极窄幅呲牙（1011 v3 口径）—— **Idle v1 已按「全程闭嘴」提交，是否需要呲牙由用户在验收后定。**
4. 无盾胆怯家族（coward 系列）是否纳入本轮 v8 替换范围。
5. 灰底源片（shieldGuardHit / hitRight）改为本地换绿副本后再用，还是另找绿底源片。

---

## 11. 持盾 Idle v8：提交记录与实测（2026-10-25 已提交一次，待用户验收）

### 11.1 提交

- 授权指令：**2026-10-25 用户在拍板问句中选「按方案发起 Idle」**，即按本文件 §7 的唯一输入、参数、提交文本与输出路径执行，只发一个任务。
- Task：`task_c9c2f36bb50542c7a80ec94351bbb1cb`，`create_attempts = 1`（无重试、未改参、未换模型、未叠轮）。
- usage 173,700，`cost.spend = **0.57321**`（条件 `has_video_input=true` + 720p，与 1011 同形态一致）。
- 上传：`enemy1021_shield_idle_v1.mp4`（2,000,007 B）与 `input_identity_v8_idle_589.png`（350,038 B）均 HTTP 200；用了浏览器 UA（`Python-urllib` 默认 UA 会撞 `file.reachapi.ai` 的 Cloudflare 1010）。
- 轮询：第一轮 14 次 / 156.7 s 本地预算耗尽时仍为 `generating`（**本地预算耗尽 ≠ 服务端失败**）；续查同一 task 第 1 次即拿到终态 `success`。
- 下载：`.part` → 校验通过后原子改名；`ftyp` + 顶层 box（`ftyp/uuid/free/mdat/moov`）通过，PyAV 全帧解码 97/97。

```text
成片: C:\Users\steam\Videos\doubaoVideo\enemy1021_shield_idle_v8_v1_4s_720p.mp4
2,111,548 bytes
SHA-256 f6eb00dc46e1628751c986f5fae799d91d8933a596aa1173587dcf81125cc529
960x960 / 24fps / 97 帧 / 4.042 s / h264 / 无音轨
```

记录：`Library/Locus/tmp/enemy1021_v11_video_v1/` 的 `enemy1021_shield_idle_v8_v1.request.json` / `.status.json` / `.download.json`；提交脚本 `submit_idle_v8_v1.py`（已执行，勿重跑）；实测数据 `idle_v8_v1_g2_mouth.json`、`idle_v8_v1_face_motion.json`。

### 11.2 代理侧数值（只报数，不下美术结论）

| 项 | 实测 | 与 1011 基线对照 |
| --- | --- | --- |
| G2 背景键（四角 64×64 中位，逐帧） | 对请求键 `(0,177,64)`：ΔR 0、**ΔG −28…−14**、ΔB −10…−1；最差 f8 = `(0,149,57)`；f0 `(0,163,62)`、f96 `(0,151,56)` | 1011 v2 为 ΔG −26…−19，**同向同量级**；词里写死键值不保证输出一致（§1 补充条） |
| 对源片自身键 `(0,172,57)` | ΔG −23…−9 | 比 1011 v2（相对自身源片键 −27…−37）更贴 |
| 背景均匀度 | 四角空间 MAD 逐帧 min 1 / mean 2.0 / max 3；无渐变、无棋盘格 | 干净基线（idle max 122 / walk 553 / attack 237）远优于特效阈值 |
| 主体高 out/src | 比值均值 **0.996**（min 0.989 / max 1.005） | 1011 v1 曾 1.216（放大 21.6%），本版**未出现放大** |
| 脚底行漂移 | **2 px**（781→783） | 源片为 0 漂移；闸门 ≤±20 px，通过 |
| 主体中心 x 漂移 | 4 px | — |
| 主体宽度 | out 460 px vs 源 412 px | 符合预期：v8 盾更宽（v8 宽高比 0.834 vs 旧美术 0.746），**不是缩放错误** |
| 口腔暗红探针 | out 均值 6,068 / 源（旧闭面罩）均值 3,327 | **该指标在本批无效**：新角色调色板（棕皮甲、木盾、暖肤）大量落在探针色区间，增量来自皮革与木头而非张口。**张嘴与否必须目视判定**（技能 §14.3） |
| 脸部区域逐帧 mean&#124;Δ&#124; vs f0 | 均值 **22.6**、峰值 36.2 @f50 | 与源片（旧美术）35.6 同量级，说明脸部区域**不是冻结贴图**；但该值含头盔与盾缘位移，不能单独证明眉眼表演 |

### 11.3 目视核对（本侧只做身份与禁项核对，美术判断归用户）

- **身份成立**：v8 的分段铁盔（中央铁脊、窄铆钉额箍、两侧护片遮耳）、敞脸、厚眉小眼、闭口、下巴中央短髯、靛蓝交领布衣＋浅色内领、暖棕皮甲、竖木板木盾＋素铁边铆钉，全部到位。
- **旧设计未被带入**：源片的红缨、闭面罩眼缝、亮银板甲、金边、红围巾**均未出现**。
- **嘴全程闭合**：九个节拍（f0/12/24/36/44/56/72/88/96）逐帧看，脸部均为闭口，无口腔暗隙。
- **盾的关系**：解剖学右手持盾、盾在画面左侧、持盾手与前臂藏在盾后、画面右侧左手空置，全片保持。

### 11.4 审阅包（仅供审阅）

```text
C:\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\enemy1021_v11_video_v1\
  IDLE_V8_V1_CONTACT_src_vs_out.png      源片 f0 + 新片 f0/12/24/36/48/60/72/96
  IDLE_V8_V1_FACE_strip_full.png          新片脸部九连拍（按肤色定位自动裁切）
  IDLE_V8_V1_FACE_v8ref_vs_out_f0.png     v8 定妆脸 vs 新片 f0 脸 并排
  idle_v8_v1_g2_mouth.json                G2 与口腔探针原始数据
  idle_v8_v1_face_motion.json             脸部区域逐帧差
```

### 11.5 待用户验收的点

1. 表情是否读得出「随呼吸收紧再松开」，还是仍偏死脸（若偏死脸，按 §7.7 判定为提示词问题，**回原始源片＋原始身份图重写一份完整提交**，不在成片上叠轮）。
2. 盾更宽（460 vs 412 px）是否接受；接入侧按新宽度补边即可，不影响 PPU 与中心 pivot。
3. 背景键比请求值低 14–28 G（项目长期实测行为），按 G2 第 6 条**升级为待决项**：接受／换参考重生成／只在交付物侧处理（抠图后在中性底比对主体色）。本侧建议「接受 + 逐帧实测键色抠图」，与 1021 历史批次做法一致。
4. 通过后进入选帧 → 抠图（按本批实测键色标定容差）→ 交接入侧对齐/补边/Clip；**选帧必须先出预览给用户验收，未验收不得写入 `Assets/`**。

> **2026-10-25 追加：以上四项未逐项收口。** 用户看完成片后把问题定在**身份基准（头体量）**上，而不是本片的动作/几何/背景，因此本片**不进 `Assets/`**，不选帧、不抠图；已落盘的成片与数据保留备用（动作、几何、G2 结果都有效，身份修好后重发一次即可）。处置方案见 §12。

---

## 12. 头体量修正（v9）：诊断、本地草稿与暂停点

> **状态：2026-10-25 用户判定「角色形象依然是最大的问题——头身比例与新版 1011 不一致，头小身大」，要求调整基线图。用户批准了 v9 出图，但服务商余额耗尽（未扣费），随后用户指示「先不生成了，搁置一下，等我充钱」。本节把已做的工作、已备好的输入与恢复步骤固定下来。**

### 12.1 诊断（同一套量法，实测）

| 指标 | 1011 v11（基准） | 1021 v8（基线图） | 差异 |
| --- | --- | --- | --- |
| 头高 / 身高 | 0.4491 | 0.4492 | **一致** |
| 盔体高 / 身高 | 0.1731 | 0.1475 | **v8 −14.8%** |
| 头块宽 / 身高 | 0.7262 | 0.7814 | v8 +7.6%（宽度不缺） |
| 同屏主体高（1108 画布） | 605 px（头 272 px） | 554 px（头 249 px，新片） | v8 小 8.4% |

结论：**不是头身比例算错**，而是三件事叠加：① 1011 是高尖盔＋红头巾，头是一大块铁；v8 是贴脑低帽，盔体只占身高 0.148 → 头显小；② 盾宽 461 px（＝体高 83%）＋厚肩厚躯干，把躯干读数撑大；③ 同屏还比 1011 小一档。

对照图：`Library/Locus/tmp/enemy1021_v11_video_v1/HEADSIZE_1011deployed_vs_new1021_ingame.png`（在役 1011 idle1 vs 新 1021，同屏尺寸）。

### 12.2 已批准但被余额阻塞的出图（未扣费）

- 授权：2026-10-25 用户选「批准出 v9」。
- 词：§12.4，2,813 字符，SHA-256 `918c746b02a8d344bcd627af16f7b68c91f1fbe54cb1ead0b58b7c7025bc962e`（LF，已落盘）。
- 提交：`POST https://api.muskapis.com/v1/images/edits`，两张输入按序（v8 → 1011 v11）→ **HTTP 403 `insufficient_user_quota`：「余额及信控额度已用尽, 剩余可用额度: $0.00」**。任务未创建、**未扣费**。
- 记录（无 key、无签名 URL）：`C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v9\` 的 `.request.json` / `.response.json` / `.status.json`。
- 脚本：`Library/Locus/tmp/enemy1021_v11_video_v1/submit_v9_headmass.py`（**已执行过一次；再次运行前必须重新取得当次明确授权**）。

### 12.3 零成本本地草稿（已备好，可直接用或作为目标参照）

**原理**：v8 的盔冠上方全是背景，**只把「盔块（行 66..201）」做几何拉伸，脸与身体一个像素不动**，接缝落在眉线以下，不会破口。背景同时统一为 RGB(0,177,64)（原 v8 的 G 138–160 不均匀）。

| 变体 | 操作 | 盔体/身高 | 头块宽/身高 | 文件 |
| --- | --- | --- | --- | --- |
| A | 盔冠纵向 ×1.18 | 0.1693（对 1011 −2.2%） | 0.752（+3.5%） | `C:\Users\steam\Pictures\gptGen\enemy1021_v11_video_v1\enemy1021_v9_local_helmet_tall118.png`（788,203 B，SHA `2dcfa8fa3f0837de9510dc9444b33a66700685674ce4c7b058e98253cea01f06`） |
| B | 盔冠纵向 ×1.24 + 横向 ×1.06 | 0.1772（+2.4%） | 0.744（+2.4%） | `…\enemy1021_v9_local_helmet_tall124_wide106.png`（793,238 B，SHA `028aa960109d526fbf1e0da0a292cb7a5b5d566168824ca230a5d5a0f553443f`） |

**基线片（已按上传规格归一化：主体占画布 0.575、脚底 833、中心 507、底色 RGB(0,177,64)、四角实测 (0,177,64)、主体高 589）**

| 变体 | 基线片 | SHA-256 |
| --- | --- | --- |
| A | `C:\Users\steam\Pictures\gptGen\enemy1021_v11_video_v1\plate_A_helmet118.png`（344,106 B） | `53dfe15bf6d6d9e19915ad9128a41f5462548f6d3acdd0b49b04de7af5098639` |
| B | `…\plate_B_helmet124_wide106.png`（339,675 B） | `19d2e5a4b99fc2525b167e4ec6aaf9bc33e8305f0b74edda82b35eef004ef9ad` |

**副作用（需知悉）**：基线片归一化把总高固定 → 头变大的同时**躯干收窄约 2.5%**（这是头身比的净改善），**盾会跟着缩约 2.5%**。

对照图：`Library/Locus/tmp/enemy1021_v11_video_v1/V9_LOCAL_HEADMASS_4up.png`（1011 v11 / v8 / A / B 等高对照）、`V9_PLATES_vs_1011.png`（1011 与两个基线片）。

### 12.4 v9 出图词（已落盘，充值后逐字提交）

文件：`Library/Locus/tmp/enemy1021_v11_video_v1/enemy1021_v9_headmass_v1.prompt.txt`（2,813 字符，SHA `918c746b…bc962e`）。

```text
Enlarge the head and the helmet of the shield soldier in image 1, and change nothing else. The helmet in image 1 is a low cap that sits close to the skull, so the head reads far too small against the heavy body and the large wooden shield. Give him a taller, fuller iron cap with a raised crown and a broader skull-hugging silhouette, so the mass of the head reads the same way as the head of the soldier in image 2: a big, weighty block of iron above the face. Enlarge the visible face as well, keeping the same features, the same thick straight brows, the same small focused eyes, the same firmly closed mouth and the same short central chin beard, so the head reads bigger without becoming a different character. The figure's total height, the position of his boots in the frame, the position of the body, the stance and the camera framing must stay exactly as they are; the body must not shrink and the feet must not move.

Preserve the design of image 1 in every other respect: the segmented arched iron crown with its broad plate seams, the slightly raised central rib that ends at the narrow riveted brow band, the short overlapping neck-guard plates behind the ears and the narrow side plates that cover both ears while leaving the face, the cheekbones and the jaw fully exposed; the dark indigo cross-collar tunic with its pale inner collar, the warm brown sleeveless leather cuirass that ends at the waist, the broad plain leather belt with its small iron buckle, the short leather bracers, the separate cloth hem below the belt, the dark loose trousers, the calf wraps and the sturdy short boots; the one tall rectangular wooden shield with vertical timber boards, a plain iron rim and a few rivets, held on screen-left by the anatomical right hand with the gripping hand, fingers and forearm hidden behind the shield, and the empty anatomical left hand at his side on screen-right. Keep the same front-facing guarded stance, the same hard-edged directional brushwork with varied density and dark structural edges, the same distinct material treatment for iron, cloth, leather and wood, the same warm skin tone and the same flat uniform green background RGB(0,177,62).

Take only the head-to-body proportion and the amount of helmet mass from image 2. Do not copy anything else from image 2: no pointed helmet, no red headscarf, no flying red cloth tail, no round soft face, no full beard, no hemp-coloured tunic, no rope belt, no square chest plate, no sword and no ring-pommel dao. Do not add a plume, a face mask, a nasal guard, gold trim, silver plate armour, ornate officer equipment, a second shield, a spear or any other weapon. Do not crop, do not move the character in the frame and do not change the canvas size. One complete soldier, full body, boots and all shield corners inside the frame.
```

参数组（沿用本项目已执行字段）：`gpt-image-2.5-sunburst` / 1024×1024 / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1` / `background=omit` / `image[]` 顺序＝① `enemy1021_v11_style_idle_v8.png` ② `enemy1011_style_locked_dao_v11.png` / 读超时 900 s / 不自动重试。

### 12.5 恢复步骤（余额恢复后按序，一次一个授权）

1. **出 v9 图**（muskapis，1 次）。或跳过本步，直接用 §12.3 的 B 基线片（零成本，立即可用）。
2. **过 §12.6 闸门**；不过则记录原因停下，不自动重发。
3. **重做基线片**：用 v9 重出（同样归一化：主体 0.575、脚底 833、中心 507、底色 0,177,64）。
4. **重发 Idle 视频**（reachapi，1 次，≈0.57321）：源片仍是旧 idle `enemy1021_shield_idle_v1.mp4`；提示词在 §7.4 的 P2 段追加一句头身比锁定（head and helmet must occupy the same share of the body height as in the reference image; do not shrink the head）。
5. **选帧 → 抠图 → 交接入侧**（选帧先出预览给用户验收）。
6. **另可零成本做**：同屏尺寸对齐——把 1021 主体从 554 px 放大到与在役 1011 的 605 px 一致，Prefab scale 0.090161→约 0.0984，碰撞盒与血条同步（部署侧执行）。

### 12.6 v9 闸门（出图后本侧先量，不过就不往下走）

- `head_h_ratio ∈ [0.465, 0.495]`、`helmet_h_ratio ∈ [0.165, 0.185]`、脸宽高关系与 1011 v11 一致。
- 身份不可变项逐条对（盔结构、敞脸、服饰、盾、持盾方向、右手空置、无缨/面罩/银甲/金边）。
- 与 v8 的差异像素 ≥90% 落在 0–35% 高度带（只动头部）；躯干、腰带、手脚、盾被重排即算修改范围失控。
- 不合格：记录具体原因并停下，**不自动重发**（v3→v5→v6 的头身比反复就是栽在没有量化闸门）。

### 12.7 当前停点与未做项

- **停点**：2026-10-25 用户指示「先不生成了，搁置一下，等我充钱」。此后不提交任何出图或视频请求。
- **未做**：v9 出图（被余额阻塞）、Idle 视频重发、选帧、抠图、对齐、补边、Clip、Prefab、第一关接入、任何工程资产改动（git 核对：1021 精灵/Clip/Prefab 未变动）。
- **仍可用**：§12.3 的 A/B 基线片与 §12.4 的词，都是零成本且已就绪；§12.5 第 6 步的同屏尺寸对齐不花钱。
