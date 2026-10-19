---
id: kd_9bbdc84d-86be-46d5-9b77-5ee166a0ca85
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 已验收美术素材跨主机同步（2026-10-19）

> 起因：两台开发主机（`Administrator` = 本机 / `steam` = 另一台）各自在本地跑视频与生图，验收过的视频和成图只落在各自机器的 `C:/Users/<用户>/Videos/doubaoVideo`、`Pictures/gptGen` 和项目根的 `*ArtSource/` 里，而 `*ArtSource/` 整体被 `.gitignore` 忽略，导致两边互相看不到对方验收过的素材。
> 本轮范围：只同步「已验收 + 不可重建」的源视频与索引/清单，不同步选帧、抠图帧与原始抽帧（用户 2026-10-19 选择「精简：只同步视频」）。

## 1. 现状（本机核查结果）

| 包 | 本机体积 | 本轮前入库情况 |
|---|---|---|
| `Enemy1011ArtSource/` | 593MB | **0 文件入库**（`.gitignore` + `.git/info/exclude` 双重排除） |
| `Enemy109ArtSource/` | 406MB | 286 个文件 / 142.5MB 入库（`d9758e8` 那次），但 2026-10-10 之后新增的 RiderFall / RiderlessEscape / ChargeInterrupted 等全部未入库 |
| `StabWeaponArtSource/` | 135MB | 148 个文件 / 36.8MB 入库 |
| `ArtDeliverables/` | 31MB | 未跟踪（望远镜交付包） |
| `Enemy1021ArtSource/` | — | **不存在**：1021 素材在 `steam` 机的 `C:/Users/steam/Videos/doubaoVideo`、`Pictures/gptGen` |

关键发现：`Enemy1011ArtSource/` 除了 `.gitignore`，还被 **`.git/info/exclude`**（2026-10-09 迁移准备时加的，只在本机生效）整目录排除。只改 `.gitignore` 白名单无法让 1011 入库，必须同时注释掉这条本地排除。

## 2. 本轮改动

1. **`.gitignore` 改成「素材同步白名单」**：先用 `<包>/*` 挡住整包，再逐个子目录 `!` 放行。放行清单：
   - 1011：`README.md`、`00_Index/`、`01_SourceVideos/`
   - 109：`README.md`、`00_SourceVideos/`、`01_KeyPosePreparation/`（去 `review/`、`upload/`）、`02_VideoPreparation/`（去 `review*/`、`upload*/`、`generated_review_*/`、`diagnostic_review/`）、`04_MountEscape/`（去 `03_SelectedFrames/`、`review/`、`upload/`）
   - 1021：结构预留（`README.md`、`00_Index/`、`00_SourceVideos/`、`01_KeyPosePreparation/`）
   - `StabWeaponArtSource/` 保持整体忽略（已入库的历史文件不受影响）
2. **`.git/info/exclude`**：注释掉 `Enemy1011ArtSource/` 与 `**/Enemy1011ArtSource/` 两行本地排除（改由共享 `.gitignore` 白名单控制）。
3. **109 已验收源视频回填**到新建的 `Enemy109ArtSource/00_SourceVideos/`，共 8 支 22.8MB，逐字节 sha256 与 `doubaoVideo` 原件一致；`MANIFEST.csv` 记录 bytes/sha256/任务ID/验收依据。其中 `retreat_to_idle`（`c01c129c…`）与 `charge_interrupted`（`d3df3049…`）的哈希与既有 knowledge 记录一致，可作交叉验证。
4. **1011 视频清单**：新增 `Enemy1011ArtSource/00_Index/videos.csv`（10 支视频 bytes/sha256/任务ID），并在 `Enemy1011ArtSource/README.md` 追加「版本库同步范围」一节。
5. **`Enemy109ArtSource/README.md`（新建）**：入库/不入库边界、8 支视频表、未入库视频及原因（`cavalry_lancer_idle` 无验收记录；`striking v1–v4` 未验收；`v5` 越界被 v6 取代；`v6` 待动作帧验收；木箱炸裂片整片作废）。
6. **`Enemy1021ArtSource/README.md`（新建）**：约定结构 + `MANIFEST.csv` 列定义 + 待 `steam` 机核对的视频/关键姿态候选清单（名称取自 `memory/video-generation-case-log.md`）。
7. **`Locus/tools/art-sync/verify_art_sync.py`（新建）**：按各清单逐条校验 bytes/sha256，报告 MISSING/SIZE/HASH/UNLISTED，退出码可用于脚本化。本机实测 18 条全部 `OK`。

本轮提交体积：183 个文件 / 103.8MB（1011 22.4MB + 109 50.9MB + 望远镜交付 30.6MB）。

## 3. 另一台主机（`steam`）的合并步骤

```bash
# 1. 先拉取；若 pull 后看不到 1011/109 的新文件，先排查本地排除
git pull
git check-ignore -v Enemy1011ArtSource/01_SourceVideos/sword_enemy_walk_front_v4.mp4
#    若有输出且来源是 .git/info/exclude，说明该机也有 10-09 那条本地排除，按本机做法注释掉再 pull

# 2. 校验 8 支 109 视频 + 10 支 1011 视频
python Locus/tools/art-sync/verify_art_sync.py     # 期望：18 条一致，0 条有问题

# 3. 回填 1021 素材（结构见 Enemy1021ArtSource/README.md）
#    C:/Users/steam/Videos/doubaoVideo/<已验收视频> -> Enemy1021ArtSource/00_SourceVideos/
#    关键姿态 -> Enemy1021ArtSource/01_KeyPosePreparation/
#    追加 MANIFEST.csv 后 git add/commit/push；本机再 pull 即可拿到
```

## 4. 明确没做（保持原状）

- 1011 的 `03_SelectedFrames/`（129MB）、109 的 `03_SelectedFrames/`（194MB）、`05_ColorCorrection/`、`02_RawFrames/`、`04_DeliveredSprites/` 仍只留本机。理由：交付精灵已在 `Assets/` 入库；选帧与抠图帧可由 `00_SourceVideos/` 里的视频 + 各组 `selected_frames_manifest.json` 重建。
- 109 的 `cavalry_lancer_striking_v6` 待用户动作帧验收，验收后再回填 `00_SourceVideos/` 并补 `MANIFEST.csv`。
- 1021 的素材本机没有，只能由 `steam` 机按 `Enemy1021ArtSource/README.md` 提交。
- `Locus/knowledge/memory/video-generation-case-log.md` 里提到的 `Library/Locus/tmp/cavalry_concept/`（750MB 中转目录）未做迁移；其中已验收内容的成品已在 `Enemy109ArtSource/03_SelectedFrames/TurnToIdle/` 等目录落盘，但中转目录被清理后其诊断/脚本会丢，如需长期保存另议。
