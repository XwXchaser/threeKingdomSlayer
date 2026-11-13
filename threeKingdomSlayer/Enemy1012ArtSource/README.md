# Enemy 1012 美术素材总包（v11 刀兵形象 · 替换自 1011）

- 生成日期：2026-10-25
- 位置说明：本目录在 **Unity 项目根目录下、`Assets/` 之外**，Unity 不会导入，不产生 `.meta`，也不进 AssetDatabase / 打包。
- 用途：**这一套是「替换」产物** —— 造型基线沿用 1011 的 v11 定稿（`enemy1011_style_locked_dao_v11.png`），但作为**新角色编号 1012** 独立入库，1011 已还原为原本造型。
- 本包内容边界：**只含美术资源与溯源信息**。Prefab / 敌人配置 / 代码映射 / 场景接线由接入方按交接文档 `Locus/knowledge/plan/enemy1012-v11-art-handoff.md` 自行部署。

## 目录结构

```text
Enemy1012ArtSource/
├─ README.md                  本说明
├─ 00_Index/                  溯源表与对照图
│   ├─ videos.csv             9 支「动作源片」（旧美术的动作出处，也是 v11 替换的 reference_video）
│   ├─ replacement-videos.csv 10 支 v11 替换成片（task / sha256 / 费用）
│   ├─ identity-images.csv    3 张 v11 身份图（各动作组用的那张）
│   ├─ frame-provenance.csv   70 张交付精灵的逐张溯源
│   └─ contact_old1011_vs_1012.png  旧 1011 美术（上） vs 新 1012 美术（下）逐槽对照
├─ 01_SourceVideos/           9 支动作源片（已验收，字节与 videos.csv 一致）
└─ 02_IdentityImages/         3 张 v11 身份图（绿幕制作底，可直接复用于重生成）
```

## 十组动作与素材对应

| 组 | 精灵 | 动作源片（reference_video） | 替换成片 | 说明 |
|---|---|---|---|---|
| Idle | `Enemy_1012_idle1..6` | `sword_enemy_v4_combat_idle_loop_v1_720p_4s.mp4` | `dao_v11_idle_v1_4s_720p.mp4` | 6 帧循环 |
| Walk | `Enemy_1012_walk1..6` | `sword_enemy_walk_front_v4.mp4` | `dao_v11_walk_v1_4s_720p.mp4` | 6 帧循环 |
| Attack | `Enemy_1012_attack1..16` | `2b1888f6388ed56b14da731eb0aeb229.mp4` | `dao_v11_attack_v1_4s_720p.mp4` | 18 键（首尾接 idle1） |
| HitHeavy（正面受击） | `Enemy_1012_hitHeavy1..6` | **无源片（纯生成词）** | `dao_v11_hitfront_v1_6s_720p.mp4` | 新动作：旧正面受击幅度只有 6.6%，改走纯生成 |
| HitLeft | `Enemy_1012_hitLeft1..6` | `sword_enemy_hit_right_v1.mp4` | `dao_v11_hitright_v1_4s_720p.mp4` | **命名对调后**：旧美术的 hitRight 用的就是这一支 |
| HitRight | `Enemy_1012_hitRight1..6` | `sword_enemy_hit_left_v3.mp4` | `dao_v11_hitleft_v1_4s_720p.mp4` | **命名对调后**：旧美术的 hitLeft 用的就是这一支 |
| Dead | `Enemy_1012_dead1..6` | `sword_enemy_dead_front_v4.mp4` | `dao_v11_dead_v1_4s_720p.mp4` | 后仰倒地、刀脱手 |
| Launched_Rise | `Enemy_1012_rise1..6` | `enemy1011_launch_rise_v1.mp4` | `dao_v11_rise_v1_4s_720p.mp4` | 站立 → 被击飞腾空 |
| Launched_Fall | `Enemy_1012_landing1..6` | `enemy1011_launch_landing_v1.mp4` | `dao_v11_landing_v1_4s_720p.mp4` | 空中 → 下落触地躺定 |
| Launched_Getup | `Enemy_1012_getup1..6` | `enemy1011_launch_getup_v1.mp4` | `dao_v11_getup_v1_4s_720p.mp4` | 躺姿 → 起身站定 |

合计 **70 张精灵 / 10 个 clip / 1 个 controller**。

## 命名口径（重要）

项目的左右受击命名口径：**身体被击后倒向哪一侧 = 哪个动作**（`身体被击后倒向画面左 = HitLeft`）。
旧美术的源片文件名与该口径是相反的，因此 v11 替换时做了**对调**：HitLeft 用 `sword_enemy_hit_right_v1.mp4`、HitRight 用 `sword_enemy_hit_left_v3.mp4`。
同一口径已同步应用到 1021：其原 `hitRight` 素材实测上身相对脚底偏移 −40.7…+3.6 px（倒向画面左），已改名为 `hitLeft`（精灵 `Enemy_1021_hitLeft1..6.png`、clip `Enemy_1021_HitLeft.anim`、控制器状态 `HitLeft`）。

## 制作配方（可复现）

- 身份基线：`enemy1011_style_locked_dao_v11.png`（SHA `67da5838…c39`），已登记在 `design/art-style-guide.md`。
- 身份图三张（见 `00_Index/identity-images.csv`）：
  - `input_identity_v11_696.png` —— 主体高 696、脚底 896（idle / walk / attack / hitLeft / hitRight / hitHeavy）
  - `input_identity_v11_dead_A696_ground830.png` —— 主体高 696、接触线 830 = 站立地面线（dead / rise / getup）
  - `input_identity_v11_landing_A696_ground717.png` —— 主体高 696、接触线 717 = 躺姿地面线（landing）
- 视频参数：`seedance-2-fast` / 4 s（HitHeavy 为 6 s）/ 720p / 1:1 / 无音频 / 无水印 / seed −1 / 不传 background；一次授权只发一个任务；一个动作只允许生成一次（叠轮会让边缘锐度 3.71→3.33→3.14 逐轮下降）。
- 抠图：SVL Chroma **T=79 / softness 16 / halo 1 / 半透明转不透明**；逐帧实测键色（成片底色比请求键低 5–20，落在 G≈138–152）。
- 碎屑清理：alpha 连通域 <150 px 的岛丢弃（模型在起身段会喷红色尘点）。
- 落位：单一全局缩放 + 单一贴图偏移，1108×1108 画布；击飞链共用 **k=0.93077 / 偏移 (106,172)**；站立参考帧对齐 idle1（主体高 605、脚底 898、中心 x=521）。
- 导入设置：PPU 16 / alignment 0 / pivot 0.6539,0.5487（attack 与 dead 组为 0.5,0.5）/ maxTextureSize 2048 / Point / alphaIsTransparency / textureType Sprite / spriteMode Single。

## 已验收的量化结论

| 项 | 实测 |
|---|---|
| G2 背景片基 | 对基准键 (0,159,62)：ΔR 0…15、ΔG −22…0、ΔB −9…12（各组同量级） |
| 特效闸门 | 偏离键色 >25 的像素 ≤1500/帧（idle 122 / walk 553 / attack 237 / 本批最高 1709，起身段红色尘点，已由碎屑清理清零）；亮像素 0 |
| 抠图 | holes 0 / plate_leak 0 / semi_transparent 0 / green>8 <1% 可见像素 |
| 链连贯 | 旧源片 rise 末帧 ≡ landing 首帧（差 3.31/通道）、landing 末帧 ≡ getup 首帧（差 3.49）→ v11 替换天然连贯 |
| 链接口 | 新 rise6→landing1 缩 17.5%（旧链 12.6%）；新 landing6→getup1 缩 6.1%（旧链 13.3%）；getup 站立帧 610/898/521 ≈ idle1 605/898/521 |

## 版本库同步范围

本包默认整体被根 `.gitignore` 忽略（Unity 不导入、体积约 18MB）。为了让两台开发主机共用**已验收且不可重建**的素材，只把下面这些放进版本库：

| 已入库 | 内容 |
|---|---|
| `README.md` | 本说明 |
| `00_Index/` | 溯源表、对照图 |
| `01_SourceVideos/` | 9 支动作源片 |
| `02_IdentityImages/` | 3 张 v11 身份图 |

仍只留在本机、不进版本库：各动作的原始抽帧、选帧副本、抠图中间产物（都在 `Library/Locus/tmp/`，被 git 忽略）。

核对方式：`python Locus/tools/art-sync/verify_art_sync.py`（按 `videos.csv` 逐条 sha256）。


---

## 山贼版 v5 立绘替换（2026-11-04；attack 于 2026-11-05 重做）

已验收的山贼新形象（v5 立绘）替换了 v11 刀兵形象。已替换 **idle**、**attack**、**walk**、**hitRight**、**hitLeft**、**hitHeavy** 六个动作，其余动作仍是 v11 形象。

| 动作 | 新成片 | 身份图 | 选帧 | 落位 |
|---|---|---|---|---|
| idle | `01_SourceVideos/enemy1012_bandit_idle_v6.mp4` | `02_IdentityImages/input_identity_v12_bandit_idle_v6_1024.png` | f010 / f017 / f024 / f032 / f039 / f046 | k=0.92366 + 逐帧偏移 x=521-缩放后中心x、y=897-可见底行 |
| attack | `01_SourceVideos/enemy1012_bandit_attack_arc_E2.mp4` | 同上 | f004 / f007 / f010 / f013 / f016 / f050 / f054 / f058 / f062 / f066 / f070 / f074 / f078 / f082 / f086 / f090 | **与 idle 站姿帧配准**（dx=0 / dy=0，IoU=0.986）→ k=0.92366 + 固定偏移 (+109,+122)；脚底线 897（刀尖可低于脚） |
| attack 表情探针 | `01_SourceVideos/enemy1012_bandit_attack_arc_E1.mp4` | A6 头肩 `stageA6_wicked_grin_v1.png` | —（不入工程，仅验证表情弧线） | — |
| walk | `01_SourceVideos/enemy1012_bandit_walk_v1.mp4` | 同上 | f040 / f043 / f045 / f048 / f051 / f053（步态周期 16 帧内等距） | k=0.92366 + 固定偏移 (+53,+122)；脚底线 897（与 idle 同一偏移） |
| hitRight（向右倒的受击） | `01_SourceVideos/enemy1012_bandit_hitHeavy_v1.mp4` | 同上 | f012 / f026 / f044 / f076 / f086，末帧用 idle1 | k=0.92366 + 固定偏移 (+110,+122)；脚底线 897 |
| hitLeft（另一面的受击） | `01_SourceVideos/enemy1012_bandit_hitLeft_v1.mp4` | 同上 | f008 / f026 / f042 / f072 / f078，末帧用 idle1 | k=0.92366 + 固定偏移 (+110,+122)；脚底线 897 |
| hitHeavy（正面受击） | `01_SourceVideos/enemy1012_bandit_hitHeavy_v6.mp4` | `02_IdentityImages/anchor_bandit_960.png`（首尾帧） | f030 / f062 / f068 / f100 / f118 / f140 | k=0.92366 + 固定偏移 (+110,+122)；脚底线 897 |

- 视频接口：`POST https://direct.reachapi.ai/v1/vids/create`，`seedance-2-fast` / 4 s / 720p / 1:1 / 无音频 / 无水印 / seed −1；first_frame 与 last_frame 用同一张身份图。
- 抠图：SVL Chroma T=79 / softness 16 / halo 1 / 半透明转不透明（2026-11-05 起改用本地 `Tools/visual_brief/chroma_key.py`，与归档产物逐帧 alpha 一致率 99.75%，残留更少）。
- 身份图规格：1024×1024、绿 (0,159,62)、**主体高 696 / 脚底 895 / 中心 474.5**（与 v11 身份图同规格）。
- idle 选帧口径：周期 43 帧（= Enemy_1012_Idle.anim 的 1.8 s 键距 @24fps）内等距取 6 帧，起点取「首尾姿态差最小」处；该口径在旧源片上回归 **16/16 精确命中**（工具 `Tools/visual_brief/pick_attack_frames.py --validate-on`）。
- attack 选帧口径（2026-11-05 版）：**举刀段手选 + 劈砍段 DP 最大化最小相邻差**（排除 f052/f053 大回旋弧光）；旧的“峰值对齐”法在本片会把举刀段塔缩进静止保持区（s03–s05 剪影完全相同）。
- attack 表情口径：**方案 A —— 起手与收势 = idle 的半笑，峰值 = 劈砍瞬间露齿**；首帧与末帧同 idle1，idle↔attack 无跳变。

**已知偏差（山贼版源片固有，非选帧问题）**
- **idle**：当前部署的 idle 是 v6 成片，其提示词为“保持表情不变”写法 → 脸全程冻结浅笑，**没有当初验收过的表情弧线**；且选帧只取 f010–f046（43 帧 = 前 1.8 s），弧线的回落段（f060–f096）未被选中。
- attack 刀形在片内会变（起手/收势为直刃细刀，举刀段与劈砍段为环首弯刀）——生成模型的固有不一致。
- attack 劈砍段 f060–f072 为定格段，选出的 s09–s11 因此是同一姿势（落定停顿约 0.2 s）。
- attack 全片可分辨姿势约 14 个，16 键需复用。
- attack 劈砍帧的刀尖带白色拖尾（s07–s08），非大回旋弧光。
- walk 步态周期实测 **16 帧**（0.667 s），与游戏 clip 的 0.6 s 循环基本等速。
- walk 躯干在源片内有 **±19 px 左右摆动**（重心转移，旧 v11 为 ±21.5 px，同量级），原样保留。
- walk 源片刀形与身份图一致（直刃 + 金 T 护手 + 金环首）；**attack 片内刀形会变**（直刃 ↔ 弯刀），两者不一致。
- hitRight：表情为「痛苦龇牙惨叫」（眼挤紧 + 下巴抬起 + 露齿）→ 收回半笑；身体**向右倒**（对位 v11 hitRight 的方向约定：头明显偏右 +124~+203 px）。
- hitRight 源片在 **f018–f025 有刀脱手**（刀变成独立漂浮物体），选帧已全程避开；其余帧刀在手中。
- hitRight 后仰幅度偏大（源片最深约 45°）且保持约 2 s；工程里只取 5 帧 + idle1 收尾，实际只播 0.81 s。
- **hitHeavy（正面受击）已替换**：跨原型换角色（山贼版 ↔ v11 差异大）**必须走首尾帧锚图路线**（首尾帧 = `02_IdentityImages/anchor_bandit_960.png`，身份图缩到 960²，主体高 652/脚底 839/中心 445，与其余素材同构图）；**reference_video 路线会回退成参考片人物**（实测两次：v4 长词、v5 标准一句词，均在 f022–f030 起变回 v11 的宽方脸络腮胡）。
- hitHeavy 成片 QC：剑脱手 **0** 帧、武器形变 **0** 帧、脚底线恒定 839；头顶下沉 +64（母本 +27，工程现有 v11 为 9% 身高）；刀尖贴画面左缘（母本同阶段也是 0px 余量）。
- hitHeavy 选帧按**相位**：f030 刀被震起 → f062 下扫 → f068 蹲底 → f100 蹲底 → f118 起身 → f140 站定。
- **母本 v11 的脸是完全可见且表演很足的**（红布是束带不是遮脸）：f041 咬牙瞪眼、f051 张口惨叫、f067–f090 低头龇牙、f100 脸收回 → 走 reference_video 即连表演一起继承，**不需要另发表情片**。
- hitLeft：动势**照 v11 原设计**（原地站立不倒不蹲、刀自身侧抬起向左上、头微沉）；表情=痛苦龇牙惨叫 → 收回半笑；**剑全程在手**（连通域检测 0 帧脱手），已避开 f004-f005（刀形变木桩）。

**未替换**：dead / rise / landing / getup 仍是 v11 形象，与山贼版不一致，后续需按同一流程逐个替换。

---

## 山贼版比例修正 v5 的「正面受击」替换（2026-11-07，**候选片，未接入工程**）

> 触发：用户提供 `C:/Users/steam/Pictures/gptGen/enemy1012_proportion_fix/1012_fix_v5.png`（山贼版按 v11 比例修正后的第五轮立绘）作参考图，
> 要求把 `C:/Users/steam/Videos/doubaoVideo/dao_v11_hitfront_v1_6s_720p.mp4`（正面受击母本）里的人物换成它。

### 产出

| 项 | 值 |
|---|---|
| 成片 | `01_SourceVideos/enemy1012_bandit_v5fix_hitHeavy_replace_v1.mp4`（sha `0a714b098af343203942b405c8e3053da17d69cd27ec3b6fbd23ce6a55b79d78`，task `task_d94fc5b0031a4ccfa6c96f7863a1e92c`） |
| 路线 | **替换视频工作流**（`reference_image` = 新身份图，`reference_video` = 母本，`duration` = 6 s = 母本时长；提示词用已验收的标准一句词，26 字符） |
| 参考图 | `02_IdentityImages/input_identity_v12_bandit_v5fix_1024.png`（由 1012_fix_v5.png 抠图→缩放到主体高 696→贴 (175,200)→合成绿 0,159,62） |
| 备用锚图 | `02_IdentityImages/anchor_bandit_v5fix_960.png`（主体高 652 / 脚底 839 / 中心 445；**当前未被任何成片使用**，为备用） |
| 提示词 | `Tools/visual_brief/prompts/1012_hitHeavy_replace_v5fix_v1.txt` |

### QC 结论（证据在 `Library/Locus/tmp/1012_batches/12_hitHeavy_replace_v5fix/`）

- **动作继承极好**：与母本的包围盒轨迹相关 **左沿 0.975 / 上沿 0.993 / 高度 0.996**
- **角色只在开头 1.25 s 成立**：**f000–f029 是参考图角色，f030 起回退成母本里的 v11 刀兵**（宽檐斗笠盔 + 红发 + 络腮胡）
- 且开头这 1.25 s **没有动作**：主体高 650→646、盔顶行 227→232 —— 是参考图的静态站姿，母本此时的「举剑」没有继承
- 武器脱手 0 帧；四边全在框内 142 帧；片基 dG −33.2…−13.0
- → **本片无法用于游戏的正面受击**：新角色既没有拿到受击动作，也只覆盖了前 1/5 时长

> 结论与项目既有实测一致（`Locus/knowledge/skill/reachapi-seedance-video-generation-v2.md`）：
> **跨原型换角色时，`reference_video` 路线锁得住动作、锁不住角色**；本轮用新比例立绘复测，结论不变。
