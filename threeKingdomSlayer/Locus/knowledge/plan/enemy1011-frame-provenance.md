---
id: kd_f945fbd8-fa6d-47c7-b281-e193d128080d
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# Enemy 1011 已部署动画帧 → 原生视频帧/视频 溯源表

> **用途**：把 `Assets/Sprites/Enemy/Enemy1_1/` 里已部署的 69 张 1011 精灵逐张对回它的**源视频、源视频帧序号与抠图产物**，供后续返工、重抠、重排节奏时直接定位"这一帧到底是哪一帧视频"。
> **口径**：结论分三级——`逐像素一致`（把源图按实测补边偏移贴进 1108 画布后与部署资产完全相同）、`轮廓对齐`（同姿势但经过缩放）、`仅记录`（只有部署记录/命名为证，未做像素校验）。本表逐条标注所属级别，不把"命名像"当"已证明"。
> **机器可读版**：`plan/enemy1011-frame-provenance.csv`（69 行，含完整路径）。可视对照图：`Library/Locus/tmp/1011_frame_provenance/contact_deployed_vs_source.png`（左=部署精灵，右=源视频帧，统一像素比例）。

## 0. 共同参数与前提

- 所有源视频都是 `seedance-2-fast`、请求 4s / 720p / 1:1、无音频无水印、`seed=-1`；**实际输出均为 960×960 / 24fps / 97 帧 / 约 4.04s**。因此视频时间 = 帧序号 ÷ 24（同一支视频内）。
- 两个**不同的帧命名基准**，换算时必须区分：
  - Idle 抽帧是 **0-based**（`clean_raw/f000.png`–`f048.png`，只抽了前 49 帧）；
  - 其余全部是 **1-based**（`f001.png`–`f097.png`）。CSV 的 `video_time_s` 已按各自基准算好（idle 用 `n/24`，其余用 `(n-1)/24`）。
- 部署画布固定为 **1108×1108**（960 画布补边），导入设置 PPU 16 / Center pivot (554,554) / Point / Uncompressed。常规补边偏移是 **(74,120)**。
- 视频文件都在 `C:/Users/Administrator/Videos/doubaoVideo/`；GPT 抠图产物在 `C:/Users/Administrator/Pictures/gptGen/`；抽帧与抠图中转在 `Library/Locus/tmp/`（临时目录，可从视频重建）。

## 1. 十条动画 → 源视频一览

| 动画 | 源视频（`C:/Users/Administrator/Videos/doubaoVideo/`） | task id | 抽帧目录（`Library/Locus/tmp/`） |
|---|---|---|---|
| Idle | `sword_enemy_v4_combat_idle_loop_v1_720p_4s.mp4` | `task_688460292bfc499f9f1f217b9a4c329c` | `sword_enemy_v4_idle_cycle_v1/clean_raw`（f000–f048） |
| 正面受击 | `sword_enemy_hit_front_v2.mp4` | `task_39f03a1065f5447bb302ab4e81bfe03b` | `sword_enemy_hit_front_v2/`（f000–f096 每 8 帧 + f059）与 `sword_enemy_hit_front_v2/selected/raw_fNNN.png` |
| Attack | `sword_enemy_attack_front_v2.mp4` | `task_67c87dbe7385459cb37f16aa958bee7e` | `sword_enemy_attack_front_v1/clean_raw` |
| Dead | `sword_enemy_dead_front_v4.mp4` | `task_e3d84ebe3403436192b5c1d3697488aa` | `sword_enemy_dead_v1/clean_raw_v4` |
| Walk | `sword_enemy_walk_front_v4.mp4` | `task_18e87a74532b4f1f86161a1b00b318cf` | `sword_enemy_walk_v1/raw_frames_v4` |
| 左受击 | `sword_enemy_hit_left_v3.mp4` | `task_3992f2cba10041af902a0aff47a43e05` | `sword_enemy_hit_left_v3/frames` |
| 右受击 | `sword_enemy_hit_right_v1.mp4` | `task_71ccbe537c0548d9af0b9a5c0dea6613` | `sword_enemy_hit_right_v1/frames` |
| 击飞 Rise | `enemy1011_launch_rise_v1.mp4` | `task_1c5d1f90fb0549f2b649110eda38208d` | `sword_enemy_launch_v1/frames_rise_v1` |
| 落地 Fall | `enemy1011_launch_landing_v1.mp4` | `task_9f5c4081f24c4206afeae33c071e19c3` | `sword_enemy_launch_v1/frames_landing_v1` |
| 起身 Getup | `enemy1011_launch_getup_v1.mp4` | `task_b226b12b3dc24b9aae00e0ad63480260` | `sword_enemy_launch_v1/frames_getup_v1` |

左/右受击与四条 Launch 视频的首尾帧都是 `Library/Locus/tmp/sword_enemy_hit_front_v2/anchor_green.png`（960²，背景中位 RGB(0,177,64)）。

## 2. 逐帧映射

符号：`源帧` 是视频帧序号（`fNNN`），`补边` 是实测的画布偏移；未标"缩放"的都通过"按该偏移贴入后与部署资产逐像素一致"校验。

### Idle（0-based，前 49 帧）
| 部署精灵 | 源帧 | 时间(s) | 抠图产物 | 补边 |
|---|---|---|---|---|
| `Enemy_1011_idle1.png` | f000 | 0.000 | `gptGen/sword_enemy_v4_idle_gpt_cutout_test/idle_f000_gpt_cutout_v1_aligned.png` | 74,120 |
| `Enemy_1011_idle2.png` | f008 | 0.333 | 同目录 `idle_f008_gpt_cutout_v3_aligned.png` | 74,120 |
| `Enemy_1011_idle3.png` | f016 | 0.667 | 同目录 `idle_f016_gpt_cutout_v1_aligned.png` | 74,120 |
| `Enemy_1011_idle4.png` | f024 | 1.000 | 同目录 `idle_f024_gpt_cutout_v1_aligned.png` | 74,120 |
| `Enemy_1011_idle5.png` | f032 | 1.333 | 同目录 `idle_f032_gpt_cutout_v1_aligned.png` | 74,120 |
| `Enemy_1011_idle6.png` | f040 | 1.667 | 同目录 `idle_f040_gpt_cutout_v5_aligned.png` | 74,120 |

### 正面受击（末键引用 `idle1`）
| 部署精灵 | 源帧 | 时间(s) | 抠图产物（`gptGen/sword_enemy_hit_front_keyed_A2_unmix/`） | 补边 |
|---|---|---|---|---|
| `Enemy_1011_hit1.png` | f016 | 0.625 | `hit_f016_A2.png` | 74,120 |
| `Enemy_1011_hit2.png` | f048 | 1.958 | `hit_f048_A2.png` | 74,120 |
| `Enemy_1011_hit3.png` | f076 | 3.125 | `hit_f076_A2.png` | 74,120 |
| `Enemy_1011_hit4.png` | f080 | 3.292 | `hit_f080_A2.png` | 74,120 |
| `Enemy_1011_hit5.png` | f084 | 3.458 | `hit_f084_A2.png` | 74,120 |

### Attack
| 部署精灵 | 源帧 | 时间(s) | 抠图产物（`sword_enemy_attack_front_v1/matted_final/`） | 补边 |
|---|---|---|---|---|
| `attack1` | f012 | 0.458 | `attack_f012_final.png` | 74,120 |
| `attack2` | f020 | 0.792 | `attack_f020_final.png` | 74,120 |
| `attack3` | f029 | 1.167 | `attack_f029_final.png` | 74,120 |
| `attack4` | f036 | 1.458 | `attack_f036_final.png` | 74,120 |
| `attack5` | f048 | 1.958 | `attack_f048_final.png` | 74,120 |
| `attack6` | f049 | 2.000 | `attack_f049_final.png` | 74,120 |
| `attack7` | f050 | 2.042 | `attack_f050_final.png` | 74,120 |
| `attack8` | f051 | 2.083 | `attack_f051_final.png` | 74,120 |
| `attack9` | f053 | 2.167 | `attack_f053_final.png` | 74,120 |
| `attack10` | f055 | 2.250 | `attack_f055_final.png` | 74,120 |
| `attack11` | f057 | 2.333 | `attack_f057_final.png` | 74,120 |
| `attack12` | f061 | 2.500 | `attack_f061_final.png` | 74,120 |
| `attack13` | f070 | 2.875 | `attack_f070_final.png` | 74,120 |
| `attack14` | f075 | 3.083 | `attack_f075_final.png` | 74,120 |
| `attack15` | f077 | 3.167 | `attack_f077_final.png` | 74,120 |
| `attack16` | f081 | 3.333 | `attack_f081_final.png` | 74,120 |

Clip 的命中键落在 2.000s，正好对应源帧 f049（`attack6`）。

### Dead
| 部署精灵 | 源帧 | 时间(s) | 抠图产物（`sword_enemy_dead_v1/matted_dead/`） | 补边 |
|---|---|---|---|---|
| `dead1` | f023 | 0.917 | `dead_f023_final.png` | 74,120 |
| `dead2` | f026 | 1.042 | `dead_f026_final.png` | 74,120 |
| `dead3` | f050 | 2.042 | `dead_f050_final.png` | 74,120 |
| `dead4` | f052 | 2.125 | `dead_f052_final.png` | 74,120 |
| `dead5` | f071 | 2.917 | `dead_f071_final.png` | 74,120 |
| `dead6` | f078 | 3.208 | `dead_f078_final.png` | 74,120 |

### Walk（原地冲锋跑）
| 部署精灵 | 源帧 | 时间(s) | 抠图产物（`gptGen/sword_enemy_walk_gpt_cutout/`） | 补边 |
|---|---|---|---|---|
| `walk1` | f023 | 0.917 | `walk_f023_cutout_v1.png` | 74,116 |
| `walk2` | f025 | 1.000 | `walk_f025_cutout_v1.png` | 74,116 |
| `walk3` | f027 | 1.083 | `walk_f027_cutout_v3.png` | **缩放 0.9582 后补边** |
| `walk4` | f029 | 1.167 | `walk_f029_cutout_v1.png` | 74,116 |
| `walk5` | f030 | 1.208 | `walk_f030_cutout_v2.png` | 74,116 |
| `walk6` | f032 | 1.292 | `walk_f032_cutout_v2.png` | **缩放 0.9333 后补边** |

### 左受击
| 部署精灵 | 源帧 | 时间(s) | 抠图产物（`sword_enemy_hit_left_v3/local_key_v2_final/`） | 补边 |
|---|---|---|---|---|
| `hitLeft1` | f024 | 0.958 | `hitLeft1_from_f024_final.png` | 74,120 |
| `hitLeft2` | f033 | 1.333 | `hitLeft2_from_f033_final.png` | 74,120 |
| `hitLeft3` | f069 | 2.833 | `hitLeft3_from_f069_final.png` | 74,120 |
| `hitLeft4` | f070 | 2.875 | `hitLeft4_from_f070_final.png` | 74,120 |
| `hitLeft5` | f071 | 2.917 | `hitLeft5_from_f071_final.png` | 74,120 |
| `hitLeft6` | f075 | 3.083 | `hitLeft6_from_f075_final.png` | 74,120 |

### 右受击
| 部署精灵 | 源帧 | 时间(s) | 抠图产物（源帧副本：`sword_enemy_hit_right_v1/select6_right_v1/`） | 补边 |
|---|---|---|---|---|
| `hitRight1` | f019 | 0.750 | `hitRight1_from_f019.png` | 74,120 |
| `hitRight2` | f030 | 1.208 | `hitRight2_from_f030.png` | 74,120 |
| `hitRight3` | f051 | 2.083 | `hitRight3_from_f051.png` | 74,120 |
| `hitRight4` | f054 | 2.208 | `hitRight4_from_f054.png` | 74,120 |
| `hitRight5` | f057 | 2.333 | `hitRight5_from_f057.png` | 74,120 |
| `hitRight6` | f075 | 3.083 | `hitRight6_from_f075.png` | 74,120 |

右受击没有单独落盘的中间抠图：部署时是**直接对 select6 原始绿幕帧跑本地绿幕键**并补边，证据是 `sword_enemy_hit_right_v1/local_key_deploy_report.json`（`method=canonical_local_green_key`、`pad=[74,120]`、逐帧 `bg_rgb≈(0,161,62)`、`green_visible_pixels=0`），并且 `select6_right_v1/*.png` 与 `frames/fNNN.png` **逐像素相同**（仅 PNG 重新编码导致字节不同）。

### 击飞 Rise / 落地 Fall / 起身 Getup
| 部署精灵 | 源帧 | 时间(s) | 抠图产物（`sword_enemy_launch_v1/local_key_<组>_v1/`） | 补边 |
|---|---|---|---|---|
| `rise1` | f049 | 2.000 | `rise1_from_f049_localkey.png` | 74,120 |
| `rise2` | f050 | 2.042 | `rise2_from_f050_localkey.png` | 74,120 |
| `rise3` | f052 | 2.125 | `rise3_from_f052_localkey.png` | 74,120 |
| `rise4` | f056 | 2.292 | `rise4_from_f056_localkey.png` | 74,120 |
| `rise5` | f070 | 2.875 | `rise5_from_f070_localkey.png` | 74,120 |
| `rise6` | f097 | 4.000 | `rise6_from_f097_localkey.png` | 74,120 |
| `landing1` | f001 | 0.000 | `landing1_from_f001_localkey.png` | 74,120 |
| `landing2` | f006 | 0.208 | `landing2_from_f006_localkey.png` | 74,120 |
| `landing3` | f010 | 0.375 | `landing3_from_f010_localkey.png` | 74,120 |
| `landing4` | f015 | 0.583 | `landing4_from_f015_localkey.png` | 74,120 |
| `landing5` | f020 | 0.792 | `landing5_from_f020_localkey.png` | 74,120 |
| `landing6` | f097 | 4.000 | `landing6_from_f097_localkey.png` | 74,120 |
| `getup1` | f001 | 0.000 | `getup1_from_f001_localkey.png` | **74,226（底部被裁）** |
| `getup2` | f016 | 0.625 | `getup2_from_f016_localkey.png` | **74,227（底部被裁）** |
| `getup3` | f031 | 1.250 | `getup3_from_f031_localkey.png` | **93,232（底部被裁）** |
| `getup4` | f046 | 1.875 | `getup4_from_f046_localkey.png` | **59,272（底部被裁）** |
| `getup5` | f061 | 2.500 | `getup5_from_f061_localkey.png` | **68,139** |
| `getup6` | f097 | 4.000 | `getup6_from_f097_localkey.png` | 74,120 |

## 3. 实测偏差与待处理项

以下都是本次核对中量出来的事实，不是推测：

1. **Getup 段画布偏移不统一**：`getup1–5` 不是常规 `(74,120)`，而是 `(74,226)/(74,227)/(93,232)/(59,272)/(68,139)`；`getup1–4` 的源画布底部被裁掉（偏移 +960 超出 1108）。相对统一画布，这五帧在画布内的垂直位置比常规最多低 **+152px（约 0.79 世界单位）**，横向最多差 **19px**。凡跨 clip 比较绝对位置（落地→起身的连贯性）都必须考虑这个差异。
2. **Walk 段脚底整体高于 idle 地面线**：`deploy_pad_walk_report.txt` 记录 walk1–6 的源底行 765/791/769/769/789/777，对应输出底边 885/911/889/889/909/897，而 idle 基准地面线是 **897**，即整批高 **27–53px**；同时 walk 用的补边是 `(74,116)` 而非 `(74,120)`，比常规再高 4px。
3. **Walk 有两帧经过缩放**：`walk3`（源 f027，用 GPT cutout v3 ×0.9582）与 `walk6`（源 f032，用 GPT cutout v2 ×0.9333）是全部 69 帧中**唯二**做过整幅重采样的帧；其余 67 帧都是 1:1 贴图。
4. **正面受击的原生帧已不在抽帧缓存里**：源帧 f076 与 f084 只存在于 `sword_enemy_hit_front_v2/selected/raw_fNNN.png`，`sword_enemy_hit_front_v2/` 下只剩每 8 帧（f000…f096）加 f059。要重抠这两帧（或它们的邻帧）必须先回视频重抽。
5. **右受击 hitRight1 的资产与部署记录不一致**：`local_key_deploy_report.json` 记 `bbox_alpha20=(154,312,853,897)`，当前 `Enemy_1011_hitRight1.png` 实测 `alpha>20` 为 `(154,312,795,897)`——右缘差 **58px**；同一报告里 hitRight2–6 的四个边界与当前资产**完全一致**。需要目检 hitRight1（是不是后来重存过、或报告取值取自另一版本）。
6. **Attack 的部署报告会误导**：`deploy_pad_report.txt` 里打印的 `srcBBox/shift`（如 `shift=+(96,124)`）与实际像素放置不符，16 帧实测全部是固定 `(74,120)` 且与 `matted_final` 逐像素一致。该报告的 `srcBBox` 是绿幕底的前景估计，不能当补边依据。
7. **Idle 六帧是六次独立 GPT 调用**：模型自带缩放比在 **0.756–0.940** 之间浮动，已按"单一缩放 + 脚底锚点"对齐回原帧；f040 到第 5 次调用才命中原生 alpha 路由。逐帧残差、以及"对齐后是否还有帧间抖动"属于未验收项。
8. **正面受击的抠图路线是 A2 本地反混合**（`*_A2.png`），不是 GPT 原生 alpha；这一点与 `design/new-style-character-redesign.md` 里"透明帧走 GPT 语义去背"的默认描述不同，重做时别选错来源。
9. **边缘残留（已记录、未修）**：`hitLeft3`（源 f069）左缘有 6×6px 剑尖实心残片（源视频画框切到剑尖），`hitLeft1` 左缘有 12px 淡痕；两者在补边后分别落在画布 x=74 附近。
10. **时间轴口径**：idle 的 `f000` 是第 0 帧，其余动画的 `f001` 是第 0 帧；把 idle 与其他动画放在同一条时间线上对比时，必须差 1 帧（1/24s）。

## 4. 复现与再抽帧

- 定位/复核脚本（只读资产，可重复运行）：
  - `Library/Locus/tmp/1011_frame_provenance/match_frames.py`：把每张部署精灵按轮廓回匹配源产物，输出 `match_report.json`。
  - `Library/Locus/tmp/1011_frame_provenance/build_deliverable.py`：重算补边偏移、重写 CSV 与对照图（本次产出 `plan/enemy1011-frame-provenance.csv`、`contact_deployed_vs_source.png`）。
- 校验口径：以"源产物按实测偏移贴入 1108 画布后与部署资产逐像素一致（RGB 每通道差 ≤2）"为准；不一致就是经过缩放或重绘，需单独说明。
- 重抽帧工具链：本次在当前 shell 的 PATH 上 `which ffmpeg` **无结果**（项目此前用 FFmpeg/PyAV 在其它环境解码并落盘到上面各 `tmp/*/frames*` 目录）。重抽前先确认可用的 ffmpeg 或 PyAV 解释器，再按"1-based、24fps、960×960"落盘，保持与原目录同名。
- 视频本体都在 `C:/Users/Administrator/Videos/doubaoVideo/`，没有被改动；`Library/Locus/tmp/` 下的抽帧与抠图中转都可由视频重建，不视为永久资产。

## 5. 未做与待确认

- 未做：任何 `Assets/`、Animator、Prefab 修改；未重抠任何帧；未重抽任何视频帧；未创建任何收费任务。
- 待确认（需你裁定后再动）：
  1. Getup 段非统一补边要不要统一回 `(74,120)`（会改变起身段在画面里的绝对位置）。
  2. Walk 段脚底高度差要不要按 idle 地面线 897 重排。
  3. `walk3`/`walk6` 的缩放帧要不要用 1:1 的候选重做。
  4. `hitRight1` 的右缘差异是记录过期还是资产被改过。
  5. `hit1–5` 是否需要换成 GPT 原生 alpha 路线重抠（现在是 A2 本地键）。
