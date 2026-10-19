# Enemy 109 骑乘枪骑 · 美术素材包（含版本库同步白名单）

- 位置：Unity 项目根目录下、`Assets/` 之外。Unity 不导入、不产生 `.meta`、不进 AssetDatabase、不打包。
- 内容边界：本包内**一律是拷贝**。原视频仍在 `C:/Users/Administrator/Videos/doubaoVideo/`，原成图在 `C:/Users/Administrator/Pictures/gptGen/`，抽帧/抠图中转在 `Library/Locus/tmp/`（临时目录，会被清理）。
- 交付精灵在 `Assets/Sprites/Enemy/Enemy109/`，动画在 `Assets/Animations/`，这两处才进 AssetDatabase。
- 动作与验收进度：`Locus/knowledge/plan/enemy109-matting-unity-handoff-20261010.md`、`plan/enemy109-charge-interrupted-keypose-preparation.md`、`plan/enemy109-rider-fall-mount-escape-handoff-20261015.md`、`plan/cavalry-enemy-visual-handoff.md`。

## 一、版本库同步白名单（2026-10-19）

本包整体约 406MB，默认被根 `.gitignore` 忽略；只有「已验收 + 不可重建」的部分进版本库，供两台开发主机对齐。

| 入库 | 内容 | 体积 |
|---|---|---|
| `README.md` | 本说明 | 小 |
| `00_SourceVideos/` | 8 支已验收源视频 + `MANIFEST.csv`（字节数/sha256/任务ID/验收依据） | 22.8MB |
| `01_KeyPosePreparation/` | 供视频用的关键姿态成图、双语 Prompt、request-spec、生产记录（不含 `review/`、`upload/`） | 15.5MB |
| `02_VideoPreparation/` | 每支视频的首尾帧输入与生产记录（不含 `review/`、`review_final/`、`upload/`、`upload_final/`、`generated_review_*/`、`diagnostic_review/`） | 13.0MB |
| `04_MountEscape/` | 无骑手马（`01_RiderlessHorse/`）+ 视频②生产记录；不含 `03_SelectedFrames/` 与各 `review/`、`upload/` | 1.6MB |

**不入库（只留本机）：**

- `03_SelectedFrames/`（约 194MB）：13 组选帧 `raw/` 与抠图帧 `keyed_ui_t79|t80/`。已在 Unity 里的交付精灵就是这些帧的成品；需要重抠时按各组 `selected_frames_manifest.json` 从源视频重新选帧即可。
- `05_ColorCorrection/`（约 19MB）：已验收帧的备份与预览副本。
- `04_MountEscape/03_SelectedFrames/`（约 12MB）：无骑手马 14 帧。

新增素材时，必须在根 `.gitignore` 的「素材同步白名单」里放行对应子目录，否则 `git add` 不会带上它。

## 二、已验收源视频（`00_SourceVideos/`）

| 动作 | 文件 | 任务 ID | 验收依据 |
|---|---|---|---|
| 蓄势冲锋 | `cavalry_lancer_windup_charge_v1_720p_4s.mp4` | `task_b388d78b3a784172bcf8fc7dc3b04618` | `plan/cavalry-enemy-visual-handoff.md` §5.3 |
| 背向返航循环 | `enemy109_retreat_run_loop_v1_720p_4s.mp4` | `task_767c173fa0ea4ff2a5869894ce316d9b` | `plan/enemy109-matting-unity-handoff-20261010.md` |
| 刺击转返航 | `enemy109_strike_to_retreat_v1_720p_6s.mp4` | `task_c67ba690deac46f2bb2cb17c31f0471f` | 同上（另已选 12 帧并验收） |
| 转身回 Idle | `enemy109_retreat_to_idle_v1_720p_6s.mp4` | `task_75f597274c3a4dc28fb978b719692522` | 同上（24 帧已验收） |
| 冲锋被打断 | `enemy109_charge_interrupted_v1_720p_4s.mp4` | `task_2db36264422f43af9add2480770f4e6c` | `memory/video-generation-case-log.md` |
| 打断转返航 | `enemy109_interrupted_to_retreat_f063_v1_720p_4s.mp4` | `task_9af5590e1ede438fa2ca54e6210aa74c` | `plan/enemy109-charge-interrupted-keypose-preparation.md` |
| 骑手受击摔落（视频①） | `enemy109_rider_fall_v1_720p_6s.mp4` | `task_393dfe84427642639b163b4d499cfa26` | `plan/enemy109-rider-fall-mount-escape-handoff-20261015.md` |
| 无骑手马逃逸（视频②） | `enemy109_riderless_escape_v1_720p_4s.mp4` | `task_aceac9346c3f47f1a7fb42d522ed84e7` | 同上 |

逐条 sha256 与源路径见 `00_SourceVideos/MANIFEST.csv`。

### 暂不入库的 109 视频（在 `doubaoVideo/`，理由如下）

| 文件 | 状态 |
|---|---|
| `cavalry_lancer_idle_v1_720p_4s_retry1.mp4` | 无验收记录：正式 `MountedIdle` 六帧来自关键姿态成图与 GPT 去背，不是这支视频的抽帧 |
| `cavalry_lancer_striking_v1..v4` | 历史迭代，未验收 |
| `cavalry_lancer_striking_v5_720p_6s.mp4` | 曾获「一般视频验收」，但刺击中段越界，已被 v6 取代 |
| `cavalry_lancer_striking_v6_720p_6s.mp4` | 边界检查通过，但动作帧仍待用户验收；验收后再回填本目录并补 `MANIFEST.csv` |

另：`breakable_crate_explosion_v1_720p_4s.mp4`（可破坏木箱）判定四边出框、整片作废，不属已验收素材。

## 三、核对与合并

```bash
# 在 Unity 项目根目录执行：按 MANIFEST.csv 逐条校验 sha256，报告缺失/不一致
python Locus/tools/art-sync/verify_art_sync.py
```

另一台主机上合并步骤：

1. `git pull` 后确认 `Enemy109ArtSource/00_SourceVideos/` 里 8 支视频 + `MANIFEST.csv` 存在；
2. 运行上面的脚本，全部为 `OK` 即可直接用这些视频重抽帧/重抠图；
3. 本机新增的已验收视频请沿用同样结构（`00_SourceVideos/<原始文件名>` + `MANIFEST.csv` 追加一行）再提交，不要改文件名，便于两台机器比对。
