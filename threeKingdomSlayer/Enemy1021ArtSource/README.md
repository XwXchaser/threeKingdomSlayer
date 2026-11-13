# Enemy 1021 持盾兵 · 美术素材包（待另一台主机回填）

> **2026-11-09 更新：本机（steam）已入库 1021 的验收身份图与编辑底件，见下方「当前已入库内容」。**
> 下列“本机是空的”说的是另一台主机（Administrator）的状态，仍然成立。

- 本目录**在另一台开发主机（Administrator）上是空的**：1021 的源视频与关键姿态在开发主机（`steam`）的
  `C:/Users/steam/Videos/doubaoVideo/` 与 `C:/Users/steam/Pictures/gptGen/`，那台机器没有文件，无法代为拷贝。
- 目录结构与同步白名单已经在根 `.gitignore` 里预留，另一台主机按下面的约定放文件即可直接入库。

## 当前已入库内容（2026-11-09）

| 路径 | 内容 |
|---|---|
| `01_KeyPosePreparation/1021_idle_identity_greenscreen_1024.png` | **已验收身份图（当前定稿）**：1024² 纯绿幕 (0,159,62)、人物本体高 696、可见脚底 y=895、身体中心 x=474.5；盾在画面左侧由人物右手持握；表情＝“凶但克制”。用户 2026-11-09 验收通过 |
| `01_KeyPosePreparation/1021_idle_identity_prompt.md` | 该图的生成规格（中英提示词、负面词、参考图职责、参数、溯源链、被否版本、已知偏差） |
| `01_KeyPosePreparation/1021_base_crop_from_v3_lineup.png` | 1021 的编辑底件（从五人并排图 v3 裁切），所有 1021 单图编辑的输入基线 |
| `00_Index/identity-images.csv` | 身份图台账（variant / source_path / bytes / sha256 / note） |

> 源视频与动作帧仍未回填；`00_SourceVideos/` 与 `01_KeyPosePreparation/` 的动作部分目前是空的。

## 约定结构（与 1011 / 109 一致）

```text
Enemy1021ArtSource/
├─ README.md                 本说明（已入库）
├─ 00_Index/                 溯源表、视频清单 videos.csv（sha256/任务ID）
├─ 00_SourceVideos/          已验收源视频（原始文件名不改）+ MANIFEST.csv
└─ 01_KeyPosePreparation/    已验收关键姿态成图 + 双语 Prompt + request-spec（不含 review/ upload/）
```

- 入库：上面这三个子目录。
- 不入库：`02_RawFrames/`、`03_SelectedFrames/`、`04_DeliveredSprites/`（交付精灵已在 `Assets/Sprites/Enemy/Enemy1021/` 入库）、任何 `review*/`、`upload*/`、`diagnostic_frames/`。
- 新增其它已验收子目录时，必须在根 `.gitignore` 的「素材同步白名单」里放行，否则不会被提交。

## `MANIFEST.csv` 列（与 `Enemy109ArtSource/00_SourceVideos/MANIFEST.csv` 相同）

```text
enemy,action,file,bytes,sha256,task_id,acceptance,source_path,synced_at
```

`sha256` 用 `python Locus/tools/art-sync/verify_art_sync.py` 可以对回校验；`acceptance` 写验收依据（哪份 knowledge 文档的哪一节）。

## 待该机核对的 1021 素材（名称取自 `memory/video-generation-case-log.md`，本机无文件）

源视频候选：

```text
enemy1021_shield_idle_v1.mp4
enemy1021_shield_walk_v1.mp4
enemy1021_shield_attack_v1.mp4
enemy1021_shield_guard_hit_v1_6s.mp4
enemy1021_shield_break_v1_6s.mp4
enemy1021_hitflash_v1.mp4
enemy1021_hit_right_v1.mp4 / enemy1021_hit_right_v2.mp4
enemy1021_dead_v1.mp4 / enemy1021_dead_v2.mp4
enemy1021_coward_idle_v1.mp4
enemy1021_coward_hit_v1.mp4 ... enemy1021_coward_hit_v4.mp4
enemy1021_coward_launched_v1.mp4
enemy1021_coward_launch_fullcycle_v2_10s.mp4
enemy1021_coward_dead_v1_10s.mp4
```

关键姿态候选（`C:/Users/steam/Pictures/gptGen/`）：

```text
enemy1021_shield_hold_v3.png / enemy1021_shield_hold_v3_green_v1.png
enemy1021_shield_guard_contact_v1..v4.png
enemy1021_shield_guard_rebound_v1..v2.png
enemy1021_hit_front_keypose_v2.png
```

只把**用户已验收**的写进 `00_SourceVideos/` 与 `01_KeyPosePreparation/`；未验收/作废的留在该机本地，并在本 README 追加一节说明原因（参照 `Enemy109ArtSource/README.md` 第二节的写法）。

## 已知进度入口

- `Locus/knowledge/plan/enemy1021-minimal-playable-handoff.md`（Idle/Walk/Attack/ShieldGuardHit/HitFlash/Dead 已游戏验收；HitRight、ShieldBreak 已接入待验收）
- `Locus/knowledge/plan/enemy1021-art-production-handoff.md`（**§0.21 / §0.21.1–§0.21.5：三国风身份重构与 idle 形象定稿全过程**）
- `Locus/knowledge/plan/enemy1021-coward-dead-10s-video-preparation.md`
- `Locus/knowledge/memory/enemy-proportion-lineup-reference.md`（并排比例研究图的方法与经验）
