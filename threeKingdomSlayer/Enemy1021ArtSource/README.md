# Enemy 1021 持盾兵 · 美术素材包

- 素材由持有原件的开发主机 `steam` 回填（2026-10-19），`Administrator` 机无需本地文件即可通过 `git pull` 拿到。
- 当前入库：**已验收源视频 10 支（22.96MB）+ 已验收关键姿态 10 张与 3 张 G1 绿幕副本（9.58MB）**，全部逐字节 sha256 与原件一致。
- 复核：`python Locus/tools/art-sync/verify_art_sync.py Enemy1021ArtSource`。

## 约定结构（与 1011 / 109 一致）

```text
Enemy1021ArtSource/
├─ README.md                 本说明（已入库）
├─ 00_Index/                 关键姿态索引 keyposes.csv（sha256/用途/验收依据）
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

## 已入库的源视频（10 支）

`00_SourceVideos/` + `MANIFEST.csv`；验收依据全文见 CSV 的 `acceptance` 列（均指向 `memory/video-generation-case-log.md`）。

| 动作 | 文件 | 验收结论 |
|---|---|---|
| Idle（持盾待机） | `enemy1021_shield_idle_v1.mp4` | 四帧已部署，游戏验收 |
| Walk（持盾前进） | `enemy1021_shield_walk_v1.mp4` | 用户确认验收，六帧已部署 |
| Attack（盾撞） | `enemy1021_shield_attack_v1.mp4` | 素材 v2 验收 + 游戏验收 |
| ShieldGuardHit（格挡受击） | `enemy1021_shield_guard_hit_v1_6s.mp4` | 视频「已验收」，六帧透明 v3 已接入 |
| ShieldBreak（破盾/失盾） | `enemy1021_shield_break_v1_6s.mp4` | 修订六帧已验收，已接入第一关 |
| HitFlash（正面受击） | `enemy1021_hitflash_v1.mp4` | 第一关游戏验收通过 |
| HitRight（反侧受击） | `enemy1021_hit_right_v2.mp4` | 动作已验收（灰底流程问题另记） |
| Dead（持盾死亡） | `enemy1021_dead_v2.mp4` | 视频 + 3 帧透明已验收并接入 |
| CowardIdle（无盾胆怯待机） | `enemy1021_coward_idle_v1.mp4` | 循环视频已验收 |
| CowardLaunchFullCycle（击飞/下落/起身） | `enemy1021_coward_launch_fullcycle_v2_10s.mp4` | 源片与 16 帧均已验收 |

## 已入库的关键姿态（`01_KeyPosePreparation/`，索引见 `00_Index/keyposes.csv`）

| 目录 | 文件 | 用途 |
|---|---|---|
| `Identity/` | `enemy1021_shield_hold_v3.png` | 身份定妆（所有动作的身份/装备/盾形锚点） |
| `ShieldGuardHit/` | `enemy1021_shield_guard_contact_v4.png` | K1 承力峰值 |
| `HitRight/` | `enemy1021_hit_right_keypose_v2.png` | 歪头 + 盾外敞受击峰值 |
| `ShieldBreak/` | `enemy1021_shield_break_detach_keypose_v3.png`<br>`enemy1021_shield_break_coward_target_keypose_v1.png` | 脱手瞬间 / 无盾胆怯终态 |
| `Coward/` | `enemy1021_coward_idle_peak_keypose_v1.png`<br>`enemy1021_coward_hit_peak_v8_spread.png` | 胆怯呼吸最高点 / 受击峰值（供 CowardHit v4 视频） |
| `Dead/` | `enemy1021_dead_airborne_keypose_v3.png` | 持盾死亡腾空末帧锚点 |
| `GreenPlate/` | `enemy1021_shield_hold_v3_green_v1.png`<br>`01_coward_idle_peak_source_green_00B140_1024.png`<br>`02_hitflash_v1_green_00B140.png` | G1 绿幕副本（派生，供已验收视频复现；非新生成） |
| `HitFlash/` | `enemy1021_hit_front_keypose_v1.png` | **姿态验收未通过**，仅作已验收 HitFlash 视频 v1 的图 2 输入留档，不得再当基准 |

每张成图旁保留同名 `.request.json` / `.result.json`（完整双语 Prompt 与生成参数，已确认不含 Key 或签名 URL）。

## 未入库（留在 `steam` 机本地，附原因）

源视频：

| 文件 | 原因 |
|---|---|
| `enemy1021_hit_right_v1.mp4` | 动作验收不通过（参考图让动作走错方向，改 v2） |
| `enemy1021_dead_v1.mp4` | 两轮验收不通过（无张力、像被催眠倒下） |
| `enemy1021_coward_hit_v1.mp4`<br>`enemy1021_coward_hit_v2.mp4`<br>`enemy1021_coward_hit_v3.mp4` | 三版全部验收不通过（无受击感／两段语义硬拼／捂胸无表情） |
| `enemy1021_coward_hit_v4.mp4` | 已生成下载并全帧解码，**待用户看片** |
| `enemy1021_coward_launched_v1.mp4` | 验收不通过（凭空出现 1011 的剑，参考图带武器） |
| `enemy1021_coward_dead_v1_10s.mp4` | 源片已下载，选帧待验收；视频本身尚未验收 |

关键姿态（均未通过或未验收，不作参考）：`enemy1021_idle_keypose_v1/v2.png`、`enemy1021_shield_hold_v1/v2.png`、`enemy1021_shield_hold_v3_green_v2.png`、`enemy1021_shield_guard_contact_v1..v3.png`、`enemy1021_shield_guard_rebound_v1/v2.png`（K2 从未通过）、`enemy1021_hit_right_keypose_v1.png`、`enemy1021_hit_front_keypose_v2..v4.png`、`enemy1021_shield_break_detach_keypose_v1/v2.png`、`enemy1021_coward_hit_peak_v4..v7.png`、`enemy1021_coward_launched_peak_keypose_v1.png`。

选帧、抠图帧与补边成品仍不入库：交付精灵已在 `Assets/Sprites/Enemy/Enemy1021/`，原始帧可由本目录视频重建。

## 已知进度入口

- `Locus/knowledge/plan/enemy1021-minimal-playable-handoff.md`（Idle/Walk/Attack/ShieldGuardHit/HitFlash/Dead 已游戏验收；HitRight、ShieldBreak 已接入待验收）
- `Locus/knowledge/plan/enemy1021-art-production-handoff.md`
- `Locus/knowledge/plan/enemy1021-coward-dead-10s-video-preparation.md`
