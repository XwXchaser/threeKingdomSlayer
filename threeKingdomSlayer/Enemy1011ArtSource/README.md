# Enemy 1011 美术素材总包（源视频 / 原裁切帧 / 挑选帧 / Unity 交付物）

- 生成日期：2026-02-19（由 `Library/Locus/tmp/1011_frame_provenance/gather_sources.py` 生成，可重复运行）
- 位置说明：本目录在 **Unity 项目根目录下、`Assets/` 之外**，Unity 不会导入，不产生 `.meta`，也不进 AssetDatabase / 打包。
- 内容边界：里面是**拷贝**。原视频仍保留在 `C:/Users/Administrator/Videos/doubaoVideo/`，抽帧与抠图中转仍保留在 `Library/Locus/tmp/`，两者都未被改动。
- 完整逐帧溯源（含像素校验口径与偏差量）：`00_Index/frame-provenance.md` 与 `00_Index/frame-provenance.csv`（工作副本：`Locus/knowledge/plan/enemy1011-frame-provenance.md` / `.csv`）。
- 可视对照图：`00_Index/contact_deployed_vs_source.png`（左=Unity 交付精灵，右=源视频帧，统一像素比例，69 组）。

## 目录结构

```text
Enemy1011ArtSource/
├─ 00_Index/                     溯源表、对照图
├─ 01_SourceVideos/              10 支源视频（seedance-2-fast、4s/720p/1:1、无音频无水印、seed -1）
│  └─ _first_last_frame_anchor/  8 支视频复用的首尾帧 anchor_green.png + Dead 尾帧参考
├─ 02_RawFrames/<动画>/          每支视频的**全部**原始抽帧（未抠图、未裁切、未缩放，960x960 / 24fps）
├─ 03_SelectedFrames/<动画>/
│  ├─ raw/                       被选中的原始帧（选帧副本）
│  └─ keyed/                     这些帧的抠图产物（本地绿幕键 / GPT 语义去背）
└─ 04_DeliveredSprites/          Unity 里正在用的 69 张 1108x1108 精灵（交付版）
   └─ _UnityAnimations/          对应的 10 个 .anim 与 1 个 .controller（附加参考，非本次整理要求项）
```

## 十条动画对应关系

| 目录 | 源视频 | task id | 交付精灵 | 抽帧的文件编号 |
|---|---|---|---|---|
| `Idle` | `sword_enemy_v4_combat_idle_loop_v1_720p_4s.mp4` | `task_688460292bfc499f9f1f217b9a4c329c` | `Enemy_1011_idle1..6` | f000–f048（**0-based**，前 49 帧） |
| `HitFront` | `sword_enemy_hit_front_v2.mp4` | `task_39f03a1065f5447bb302ab4e81bfe03b` | `Enemy_1011_hit1..5`（末键引用 idle1） | f000–f096 每 8 帧 + f059，另补 f076/f084 |
| `Attack` | `sword_enemy_attack_front_v2.mp4` | `task_67c87dbe7385459cb37f16aa958bee7e` | `Enemy_1011_attack1..16` | f001–f097（**1-based**） |
| `Dead` | `sword_enemy_dead_front_v4.mp4` | `task_e3d84ebe3403436192b5c1d3697488aa` | `Enemy_1011_dead1..6` | f001–f097 |
| `Walk` | `sword_enemy_walk_front_v4.mp4` | `task_18e87a74532b4f1f86161a1b00b318cf` | `Enemy_1011_walk1..6` | f001–f097 |
| `HitLeft` | `sword_enemy_hit_left_v3.mp4` | `task_3992f2cba10041af902a0aff47a43e05` | `Enemy_1011_hitLeft1..6` | f001–f097 |
| `HitRight` | `sword_enemy_hit_right_v1.mp4` | `task_71ccbe537c0548d9af0b9a5c0dea6613` | `Enemy_1011_hitRight1..6` | f001–f097 |
| `Launch_Rise` | `enemy1011_launch_rise_v1.mp4` | `task_1c5d1f90fb0549f2b649110eda38208d` | `Enemy_1011_rise1..6` | f001–f097 |
| `Launch_Fall` | `enemy1011_launch_landing_v1.mp4` | `task_9f5c4081f24c4206afeae07e19c3` | `Enemy_1011_landing1..6` | f001–f097 |
| `Launch_Getup` | `enemy1011_launch_getup_v1.mp4` | `task_b226b12b3dc24b9aae00e0ad63480260` | `Enemy_1011_getup1..6` | f001–f097 |

时间换算：全部 960x960 / 24fps / 97 帧。`Launch_*` 的 Rise/Fall/Getup 是同一任务下的三段独立视频（Fall 段来自 `enemy1011_launch_landing_v1.mp4`）。

## 命名规则

- `02_RawFrames`：沿用抽帧原名 `fNNN.png`（Idle 为 0-based，其余 1-based）。
- `03_SelectedFrames/*/raw/`：`<动作><序号>_f<源帧号>.png`（如 `attack6_f049.png`），序号即交付精灵的 `attack6` 的 6。
- `03_SelectedFrames/*/keyed/`：沿用抠图产物的原始文件名（`attack_f049_final.png`、`hit_f016_A2.png`、`idle_f000_gpt_cutout_v1_aligned.png`、`rise1_from_f049_localkey.png` 等），便于和流程文档/脚本对上。
- `HitRight/keyed/` 没有文件：右受击部署时是**直接对 raw 帧跑本地绿幕键**并补边，中间产物没有单独落盘，产物即 `04_DeliveredSprites/Enemy_1011_hitRight*.png`（见该目录 README.txt 与 `local_key_deploy_report.json`）。

## 校验结论（本次逐文件 sha256 已核对）

- 包内 1069 个文件与原始来源**逐字节一致**（含 10 支视频、839 张原裁切帧、106 张选帧/抠图、69 张交付精灵、11 个动画文件、3 个索引文件）。
- 溯源层面：69 张交付精灵中 **67 张是与抠图产物"按实测补边偏移贴入后逐像素一致"**，常规补边 `(74,120)`。
- 两个例外：`walk3`（源 f027，GPT cutout v3 **缩放 0.9582**）与 `walk6`（源 f032，v2 **缩放 0.9333**）。

## 已知偏差（详细数值见 `00_Index/frame-provenance.md` §3）

1. `Launch_Getup` 的 getup1–5 补边不是 `(74,120)`，而是 `(74,226)/(74,227)/(93,232)/(59,272)/(68,139)`，getup1–4 源画布底部被裁；相对统一画布垂直最多差 +152px。
2. `Walk` 整批脚底高于 idle 地面线 27–53px，且补边用 `(74,116)`。
3. `walk3`/`walk6` 是唯二重采样帧。
4. `HitFront` 的 f076/f084 原抽帧缓存已不存在，包里放的是保留下来的选帧副本（已按 `f076.png`/`f084.png` 补齐到 `02_RawFrames/HitFront/`）。
5. `HitRight1` 当前资产的 `alpha>20` 右缘 795，部署记录写 853（差 58px）——资产与记录不一致，待目检。
6. `HitFront` 的抠图路线是 A2 本地反混合，不是 GPT 原生 alpha。

## 版本库同步范围（2026-10-19）

本包默认整体被根 `.gitignore` 忽略（Unity 不导入、体积 593MB）。为了让两台开发主机共用**已验收且不可重建**的素材，只把下面这些放进版本库：

| 已入库 | 内容 | 体积 |
|---|---|---|
| `README.md` | 本说明 | 小 |
| `00_Index/` | 溯源表（`frame-provenance.md/.csv`）、69 组对照图、`videos.csv`（10 支源视频的字节数与 sha256） | 2.1MB |
| `01_SourceVideos/` | 10 支已验收源视频 + `_first_last_frame_anchor/` 首尾帧 | 20.3MB |

仍只留在本机、不进版本库：

- `02_RawFrames/`（410MB）——可由源视频按 `00_Index/frame-provenance.md` §4 重新抽帧。
- `03_SelectedFrames/`（129MB）——选帧与多个抠图版本；其中交付版已在 `Assets/Sprites/Enemy/Enemy1_1/` 入库。
- `04_DeliveredSprites/`（28MB）——与 `Assets/Sprites/Enemy/Enemy1_1/` 重复。

核对方式：`python Locus/tools/art-sync/verify_art_sync.py`（按 `videos.csv` 逐条 sha256）；本包 10 支视频与 `C:/Users/Administrator/Videos/doubaoVideo/` 原件逐字节一致。

新增已验收素材时，必须同时在根 `.gitignore` 的「素材同步白名单」里放行对应子目录，否则不会被提交。

## 重新生成本包

```bash
# 在项目根目录执行；只读原始位置，只写本包
python Library/Locus/tmp/1011_frame_provenance/gather_sources.py
```

脚本会重新拷贝并打印每个子目录的文件数/体积；若原始抽帧目录已被清理（`Library/Locus/tmp/` 属临时目录），需先按 `00_Index/frame-provenance.md` §4 用 ffmpeg/PyAV 重新抽帧，再运行本脚本。
