# Enemy 1021 持盾兵 · 美术素材包（待另一台主机回填）

- 本目录**在这台主机（Administrator）上是空的**：1021 的源视频与关键姿态在另一台开发主机（`steam`）的
  `C:/Users/steam/Videos/doubaoVideo/` 与 `C:/Users/steam/Pictures/gptGen/`，本机没有文件，无法代为拷贝。
- 目录结构与同步白名单已经在根 `.gitignore` 里预留，另一台主机按下面的约定放文件即可直接入库。

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
- `Locus/knowledge/plan/enemy1021-art-production-handoff.md`
- `Locus/knowledge/plan/enemy1021-coward-dead-10s-video-preparation.md`
