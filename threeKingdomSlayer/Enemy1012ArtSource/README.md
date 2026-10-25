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
