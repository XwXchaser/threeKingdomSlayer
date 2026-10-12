---
id: kd_0a431e09-a499-4b58-af2f-63191e0cfe9a
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# Enemy 109 抠图与 Unity 入库交接（2026-10-10）

> **本文件是 Enemy 109 当前交接的权威起点。** §0 / §11 记录本轮动画接线与运行时验证；§1–9 保留素材入库时的历史现场，§10 保留返航素材生产记录。历史描述不覆盖后续接线结果。
> **重要**：`plan/cavalry-enemy-visual-handoff.md` 顶部已增加转向本文件的说明，但其历史正文仍保留「等待动作帧验收，无去背、无 Unity 接线」等旧描述；**不能据此判断当前状态**，冲突时以本文件为准。
> §1–9 中的状态是入库时历史快照。素材生产记录见 §10；本轮已将 Windup、Charging、Attack、TurnToIdle 接入 109 Prefab，运行时验证及剩余缺口见 §11。TurnToIdle 透明版仍待美术目检，生成器主体轻微色差仍单独保留；动画接线不等于素材美术验收。

---

## 0. 一句话状态

Enemy 109 的骑乘主要动作现已接线：MountedIdle / Windup / Charging / 18帧 Attack / 24帧 TurnToIdle 均有专用 Clip 和 Inspector 引用，Controller 阶段由 `CavalryEnemy` 驱动。Attack 核心帧 `Enemy_109_Attack12` 对齐 `strikeWindup=0.18s` 的伤害提交点；命中、打断、返航受阻、落马与池复用已作运行时探针验证。背向返航跑步、成功/打断后的收枪回身尚缺素材，当前以静态背向姿势和 Charging 姿势作明确占位；TurnToIdle 透明版尚待美术目检。不得将占位误述为完整骑乘循环。T70/T79 与被替换素材验收记录冲突仍见 §5。

---

## 1. 当前现场（2026-10-10 18:04 只读复核）

| 项 | 值 |
|---|---|
| 工作目录 | `H:\Project\threeKingdomSlayer\threeKingdomSlayer` |
| Git 仓库根 | `H:\Project\threeKingdomSlayer`（工程是子目录 `threeKingdomSlayer/`） |
| 分支 | `route-scroll-movement` |
| Unity | 2022.3.62t7，Built-in RP，Legacy Input |
| Editor 状态 | `editing` |
| Active Scene | `Assets/Scenes/Battle.scene` |
| 已加载场景与内存状态 | Battle：`dirty=true`；`Assets/Experiments/CurvedScroll/YJunctionSample.unity`：`dirty=false` |
| 最近提交 | `e039961` (2026-10-10 17:11) docs: 手册补 1.0 节；`1c6a958` (13:53) docs(tools): 抠图工作流手册 v2 + 参数化脚本 |

**不要**切场景、保存场景、改玩法时序；尤其保留 Battle 当前未保存的内存改动。YJunctionSample 虽在 Editor 内不是 dirty，磁盘仍有相对 Git 的修改，两种状态不能混为一谈。前轮部署改动了 PNG 与素材源目录；**本次仅更新文档，未进入 Play Mode，也未保存任何场景**。

---

## 2. 素材制作与入库成果（部署记录 + 当前复核）

### 2.1 抠图（keyed_ui_t79）

- 产物目录：`Enemy109ArtSource/03_SelectedFrames/<Action>/keyed_ui_t79/`
- 动作与帧数：`MountedIdle` 6、`Windup` 6、`Charging` 6、`Attack` 18 → 共 36
- 报告：`Enemy109ArtSource/03_SelectedFrames/enemy109_keyed_ui_t79_report.json`（含每帧明细）
- 总览图：`Enemy109ArtSource/03_SelectedFrames/enemy109_keyed_ui_t79_overview.png`
- 源清单：`Enemy109ArtSource/03_SelectedFrames/enemy109_source_manifest.json`

**配方（现行）**：`Chroma` + 自动取背景色 + **容差按底材标定** + `softness 16` + `halo 1` + 只勾「半透明像素转不透明」。

**已弃用**：`softness 0`、`先做平滑处理`(Real-ESRGAN)、以及旧的脚本后处理链（描边环替换 / 删源绿 `d>8` / 中性化 / 硬绿保证）。完整依据见 `skill/workflows/svl-chroma-rim-fix.md`。

### 2.2 Unity 入库

- 脚本：`Library/Locus/tmp/svl_run/deploy_109_to_unity.py`
- 日志：`Library/Locus/tmp/svl_run/109_deploy.log`
- 部署报告：`Library/Locus/tmp/backup_enemy109_20261010-131343/deploy_report_109.json`
- 备份：`Library/Locus/tmp/backup_enemy109_20261010-131343/`（18 张替换前原件）
- 时间：2026-10-10 13:13
- 结果：`replaced 18, created 18`

| 目标 | 数量 | 源 |
|---|---|---|
| `Enemy_109_MountedIdle1..6.png` | 6 | `MountedIdle/keyed_ui_t79/mountedIdle*_f*.png` |
| `Enemy_109_Windup1..6.png` | 6 | `Windup/keyed_ui_t79/windup*_f*.png` |
| `Enemy_109_Charging1..6.png` | 6 | `Charging/keyed_ui_t79/charging*_f*.png` |
| `Enemy_109_Attack1..18.png` | 18 | `Attack/keyed_ui_t79/attack*_f*.png` |

**逐像素核对（本次扩大为全量）**：按 `deploy_report_109.json` 的 36 条记录逐一读取源输出与库内 PNG，**36/36 张完全相等，0 张失败**。1108×1108 画布中的源窗口为 `[y=120:1080, x=74:1034]`，即左上角偏移 `(x=74, y=120)`、960×960 区域；对应 PIL crop 为 `(74,120,1034,1080)`。这证明入库来源一致，不代替细部件目检或用户验收。

**v6 刺击 18 帧 → Attack 序**（`attack1..18_f055/059/063/067/071/074 · f075/076/078/080/081/082 · f083/086/089/092/097/103`）。

### 2.3 参数化脚本与上传包

- **仓库分发位置**：`Locus/tools/svl-matting/`，本次已核实包含 `README.md` 与 6 个脚本：`svl_common.py`、`svl_matte_batch.py`、`svl_scan_tolerance.py`、`svl_build_source.py`、`svl_deploy_to_unity.py`、`green_guarantee.py`。
- 本机工作副本：`Library/Locus/tmp/svl_run/`；前轮上传包记录：`Library/Locus/tmp/upload_ready/svl-matting-workflow/`。**Library 被忽略，不随 Git 同步，不能作为另一台主机的唯一恢复入口。**
- 前轮自检记录：`Library/Locus/tmp/svl_selftest/report.json`（Windup，T=79 / softness 16，`enclosed_holes_px = 0`）。本次没有重跑抠图或自检流程。

---

## 3. 本轮被推翻的中间结论（勿再沿用）

前轮按**旧配方**做过一版 `keyed_v5`，用户目检判定「抠太狠、角色出现破洞」。以下保留前轮诊断与用户反馈，**keyed_v5 不作为交付**；本次未重跑该诊断：

- 产物：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v6/keyed_v5/`（18 张，`<group>_fNNN_v5.png`，已**作废**）
- 诊断目录：`.../hole_census/`、`.../hole_diagnosis/`、`.../single_frame_f080/`

**已量化的根因**（脚本 `Library/Locus/tmp/svl_run/hole_enclosed_v6.py`、`hole_threshold_scan_v6.py`）：

1. 内部封闭空洞主要来自**第一步键控**（T=100 过大），不是后处理：T=100 时每帧封闭孔 916–18,580 px。
2. 空洞源像素是**高饱和绿**（sat≈0.92，接近键色 `(2,143,62)`），即**背景从缝隙透出**，不是角色材质。
3. **银色被误删的范围很小**：f080 约 113 px（lum≈207）、f055 约 66 px。此前「银色整片被扣掉」的推断**不成立**。
4. 降低 T 只能部分缓解（f074：T=100→15,862 孔；T=20→10,822 孔）→ 正解是**换配方**（softness 16 反混合 + 半透明转不透明），而非继续调 T。

**因此 §2.1 的 `T=79 / softness 16` 才是本轮交付配方。**

---

## 4. 入库时的部署快照（历史；本轮接线结果见 §11）

### 4.1 入库复核时的主要缺口（当时未执行接线）

- `Assets/Animations/` 中以 `Enemy_109` 命名的资产目前只有 `Assets/Animations/Enemy_109.controller` 与 `Assets/Animations/Enemy_109_MountedIdle.anim`（及其 `.meta`）。**Windup / Charging / Attack 专用 Clip 尚未创建。**
- Unity 当前实测：109 Controller 的 `Idle` motion 是 `Enemy_109_MountedIdle.anim`；`Attack` motion 仍是 `Enemy_101_Attack.anim`；`Dead / HitFlash / Walk / Launched_Rise / Launched_Fall` 也仍复用对应 101 Clip。
- **不能写成“没有 Unity 接线”**：MountedIdle 与模式切换基础已接入；准确缺口是这批 Windup / Charging / Attack 精灵尚未接入 Clip、阶段驱动与视觉路由。
- 返航、返航受阻等其他骑乘阶段在Unity中仍属占位表现。后续返航形象/视频生产已推进，详见 §10；它们尚无新增入库精灵或Clip，是否纳入接入范围需确认。
- 真实战斗中的蓄势→冲锋→刺击→返航、打断与落马流程尚未在本次验收。

### 4.2 当前导入规格（本次用 Unity API 全量复核）

36 张全部满足：`textureType=Sprite`、`PPU=16`、`Uncompressed`、`alphaIsTransparency=true`、`mipmap=false`、`npotScale=None`、`spriteMeshType=Tight`、`spriteAlignment=Center(0)`、有效 `pivot=(554,554)`、画布 `1108×1108`。

**但存在两处新建/替换差异，建议下一会话决定是否统一**：

| | 18 张替换（MountedIdle/Windup/Charging） | 18 张新建（Attack） |
|---|---|---|
| `wrapMode` | `Repeat` | `Clamp` |
| Importer 存储的 `spritePivot` | `(0.65, 0.55)` | `(0.5, 0.5)` |

两组均为 Center(0)，当前实际 Sprite pivot 都是 `(554,554)`，**不存在已证实的有效锚点差异**；wrap 与存储 pivot 不一致仍是待决定的资产规范问题。本次未改 `.meta` 或重新导入，也未将此认定为已发生的显示故障。

### 4.3 已部署垂距（本次全量复核，alpha>20 包围盒）

以下为画布左上原点的像素行坐标，顶边/底边均为实际非透明像素的包含端点；不是 Unity 世界坐标，也不是 PIL 包围盒的排他末端。

| 组 | 底边范围 | 顶边范围 |
|---|---|---|
| MountedIdle | 1050..1050（跨度 0） | 135..137 |
| Windup | 1050..1050（跨度 0） | 136..319 |
| Charging | 991..1048（跨度 **57**） | 267..285 |
| Attack | 993..1017（跨度 **24**） | 237..279 |

Charging/Attack 的底边跨度来自马腿步态，**是记录值不是锚点值**，未做逐帧钉脚。

### 4.4 已完成的 MountedIdle 动画接线

- `Assets/Animations/Enemy_109_MountedIdle.anim` 当前 **6 个 Sprite key、0 空引用**，依次引用 `Enemy_109_MountedIdle1..6.png`。
- 绑定是根节点 `UnityEngine.SpriteRenderer.m_Sprite`；key 时间为 `0 / 0.1 / 0.2 / 0.3 / 0.4 / 0.5s`，Clip `frameRate=30`、`length=0.6s`。
- `Assets/Animations/Enemy_109.controller` 的 `Idle` 使用该 Clip；`Assets/Resources/EnemyPrefabs/Enemy_109.prefab` 的根 Animator 使用该 Controller，`Apply Root Motion=false`。
- 109 Prefab 是 `Assets/Resources/EnemyPrefabs/Enemy_101.prefab` 的变体，后续修改应针对 109，不改写 101 源 Prefab 或共享 Clip。

### 4.5 Prefab 与代码当前视觉路由（静态核对，非 Play Mode 验收）

`Assets/Resources/EnemyPrefabs/Enemy_109.prefab` 的 `CavalryVisualController` 当前 Inspector 引用：

| 模式 | 进入条件 / 当前 controller | 表现 |
|---|---|---|
| `MountedCombinedIdle` | 已骑乘且 `Phase=MountedIdle`；`mountedCombinedAnimatorController` → `Assets/Animations/Enemy_109.controller` | 根 Renderer 显示人马合一 Idle；关闭旧坐骑层与骑手 Renderer |
| `MountedPlaceholder` | 其余骑乘阶段；`mountedPlaceholderAnimatorController` → `Assets/Animations/Enemy_101.controller` | 根 Renderer 隐藏；镜像 101 骑手，并用 105 坐骑静帧/两帧翻帧占位 |
| `Dismounted` | 不再骑乘；`dismountedAnimatorController` → `Assets/Animations/Enemy_1011.controller` | 根 Renderer 显示 1011；关闭旧占位层 |

`Assets/Scripts/Enemy/CavalryVisualController.cs` 的 `GetVisualMode()` 目前只让 MountedIdle 进入合成模式，其他骑乘阶段仍走占位模式。**因此仅把 109 Controller 的 Attack motion 换为新 Clip 不足以完成接入**：新动作还需要与骑兵阶段、controller 切换及播放时序衔接。以上是当前源代码与序列化引用的核对，本次未验证运行时切换表现。

### 4.6 现有玩法参数（后续动画适配它们，不反改玩法时长）

当前 109 Prefab 的 Inspector 实测值：

| 字段 | 值 |
|---|---:|
| `CavalryEnemy.chargeDamage` | 8 |
| `CavalryEnemy.windupDuration` | 0.3s |
| `CavalryEnemy.strikeWindup` | 0.18s |
| `CavalryEnemy.strikeRecover` | 0.22s |
| `CavalryEnemy.strikeLungeDistance` | 0.5 |
| `CavalryEnemy.interruptRecover` | 0.2s |
| `CavalryEnemy.chargeCooldown` | 1s |
| `CavalryEnemy.spawnEntrySettleDuration` | 0.35s |
| `Enemy.moveSpeed` | 0.2s/排 |

动作接入按 `design/cavalry-enemy-animation.md` 的时间对齐规则：玩法时长为权威、动画不带根位移、刺击命中帧同步 `strikeWindup`。旧动画设计中的分层制作建议不能覆盖本文件已核实的人马合一素材与现有三模式路由；本次不新增或变更设计决定。

---

## 5. 仍待确认的验收与元数据记录

### 5.1 T=79 / T=70：当前来源已确认，验收备注仍冲突

- **本次已确认**：`Enemy109ArtSource/03_SelectedFrames/enemy109_keyed_ui_t79_report.json` 四组均记录 `threshold=79`，36 张库内 PNG 全部与对应 `keyed_ui_t79` 输出相等。因此本交接以 **T=79 报告批次**作为当前在库来源。
- **仍未确认**：`enemy109_source_manifest.json` 与 `Attack/raw_manifest.json` 的验收 note 仍写「keyed at T=70 and accepted」。本次没有执行日志证明该备注与当前 79 批次是同一次验收，也未重新调用抠图流程核实参数。
- 不能默默把 70 改成 79 并宣称用户验收已同步；应确认备注是否笔误、或用户验收针对另一个版本，再调整 manifest。

### 5.2 manifest 状态仍落后于实际入库

当前 `Enemy109ArtSource/03_SelectedFrames/enemy109_source_manifest.json`：

| 动作 | 现存验收记录 | 当前事实与限制 |
|---|---|---|
| Attack | `keyed_and_accepted_by_user`，note=T70 | 精灵已入库；记录声称验收，但与当前 T79 批次的对应关系仍需确认 |
| Windup / Charging | `user_accepted_pending_cutout` | 已抠图且已入库；旧“待抠图”与 `next_step` 明显过时，不应据此再抠一遍 |
| MountedIdle | `deployed_reference_exists`，note=GPT cutouts | 当前在库字节已是 `keyed_ui_t79`，原 GPT 来源备注不能代表当前版本 |

**本次只修正文档，未回写上述 JSON。** 后续记录应分开表达“已抠图/已入库”和“该版本已获用户目检验收”，不能只用一个状态自动补齐两者。

### 5.3 被替换的 18 张是否重新验收仍未确认

MountedIdle / Windup / Charging 原先是已验收 GPT 产物，本次部署记录显示被 `keyed_ui_t79` 覆盖。前轮报告显示新旧版本差异明显（如 Charging：可见像素 204,379→193,139，被删主体指标 11.7%→16.8%）。**用户是否已知情并接受当前替换版仍未确认**；旧版验收不能自动继承。本次没有进行新的美术目检或用户验收，也没有据指标认定视觉质量通过。

---

## 6. 入库复核时的工作树快照（历史；既有改动必须保留）

本次 `git status --short --untracked-files=normal` 实测：**94 modified / 38 untracked 条目**；原始快照为 93 / 37。这里是 Git 条目计数，未跟踪目录被折叠为一项，不是递归文件数。

| 路径 | 状态 |
|---|---|
| `Assets/Sprites/Enemy/Enemy1_1/*.png` | 69 modified（1011 全量换装；前轮记录为已验收，未提交） |
| `Assets/Sprites/Enemy/Enemy109/` | 18 PNG modified；18 Attack PNG + 18 对应 `.meta` 共 36 untracked |
| `Enemy109ArtSource/` | 1 个 untracked 目录条目；递归 97 文件，55,968,131 字节（约 53.4 MiB） |
| `Assets/Scenes/Battle.scene`、`Assets/Experiments/CurvedScroll/YJunctionSample.unity` | 2 modified（用户改动） |
| `Locus/knowledge/plan/cavalry-enemy-visual-handoff.md` | modified，顶部已有转向本文件的提示 |
| `Locus/knowledge/plan/e0-approach-scene-refinement-handoff.md`、`Locus/knowledge/plan/handoff-other-device-selfcheck-20261009.md`、`Locus/knowledge/plan/scroll-scene-current-todolist.md` | 3 modified |
| `Locus/workspace-trees/default.json` | modified |
| `Locus/knowledge/plan/enemy109-matting-unity-handoff-20261010.md`（本文件） | untracked；本次仅更新此文件，不暂存或提交 |

**保留全部既有改动，不清理、不覆盖、不暂存、不提交。** 当前 `.gitignore`、109 动画/controller/Prefab 与相关脚本未出现在修改条目中；本次没有改动它们。

### 6.1 提交前必须处理的忽略状态问题（本次仍能复现）

- 工程根 `.gitignore` 实读为 35 行，**只有 `Enemy1011ArtSource/`，没有 `Enemy109ArtSource/` 规则**；第 32 行为空行。
- `git check-ignore -v Enemy109ArtSource/` 本次仍返回 `threeKingdomSlayer/.gitignore:32:` 命中与退出码 0，**与文件内容及其他 Git 查询相互矛盾，原因未查明**。不要只依据这一条输出宣称目录已安全忽略。
- 当前可暂存状态交叉核对：`git status` 显示 `?? Enemy109ArtSource/`；`git ls-files --others --ignored --exclude-standard Enemy109ArtSource/` 为 **0 文件**；`git ls-files --others --exclude-standard Enemy109ArtSource/` 为 **97 文件**；`git add --dry-run -- Enemy109ArtSource/` 为 **97 条拟加入记录**（未实际暂存）。

→ **提交风险仍在**：按当前枚举与 dry-run，广泛暂存会纳入这 97 个素材源文件（约 53.4 MiB）。若后续要提交，先决定源帧的提交/忽略策略，再核对拟暂存清单；不要直接 `git add -A`。`skill/workflows/svl-chroma-rim-fix.md` §1.0 建议只提交目录结构与 manifest、排除帧文件，本次未替用户选择策略或改忽略规则。

---

## 7. 外部残留（前轮记录，未清理，供追溯）

以下保留前轮外部任务与文件记录；本次未重新调用外部服务、生成视频或重发抠图请求，也未逐项重验外部目录。

- **已取消的 GPT 批处理**：`C:/Users/Administrator/Pictures/gptGen/cavalry_striking_v6_cutout_batch_v1/`
  - 成功 5 帧：`f055 / f059 / f063 / f067 / f080`（RGBA，**旧路线产物，未采用**）
  - `f071` 只有 `.request.json`、无响应 → **结果未知，禁止静默重发**
  - 进度：`cutout_progress.json`（`api_calls_total 5, aborted False`）
  - 脚本：`Library/Locus/tmp/cavalry_concept/run_cavalry_striking_v6_gpt_cutout_batch_v1.py`（已取消）
- **v6 源视频**：`C:/Users/Administrator/Videos/doubaoVideo/cavalry_lancer_striking_v6_720p_6s.mp4`，task `task_1bcb08e0d51d4df8bd09c9388f4b7d4b`，SHA-256 `119df1a087a34856c14b104e4a50607fd60cc6967a6ee1c0fb0981dbfadf415b`，**只创建 1 次，不重建**
- **18 张已验收源帧**：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v6/keyframes_6plus6plus6_v2/<group>/`（960×960 RGB）

---

## 8. 下一会话建议顺序

1. 从 §11 恢复已接入的动画结果，勿重复创建 Windup / Charging / Attack / TurnToIdle Clips，勿沿用旧占位 controller 路由。
2. 美术目检 TurnToIdle 当前24帧透明版与游戏速度、收尾色差；当前沿用原f001..f145帧序，未替换为其他候选端点。
3. 从已验收返航跑步视频完成生产选帧和抠图，替换 `Enemy_109_RetreatPose` 静态占位；另外制作成功/打断两条收枪回身入口、骑乘受击与死亡。
4. 在包含109的自然战斗配置中验收输入触发、移动/受阻、多骑兵、死亡及对象池交互；本轮探针覆盖不等于完整路线自然流程通过。
5. §5 的T70/T79与重新验收备注继续保留为未确认记录，不因接线自动补验收。
6. 获准提交时再处理素材源忽略策略与提交清单，不混入其他用户场景或并行改动。

---

## 9. 关键路径索引

**知识**
- `Locus/knowledge/skill/workflows/svl-chroma-rim-fix.md`（**主手册**，配方/验收/入库规格/踩坑）
- `Locus/knowledge/memory/sprite-video-lab-matting-evaluation.md`（完整迭代与实测台账）
- `Locus/knowledge/plan/cavalry-enemy-visual-handoff.md`（素材制作与失败案例历史，顶部已转向本文件；历史状态勿覆盖现状）
- `Locus/knowledge/design/cavalry-enemy.md`、`Locus/knowledge/design/cavalry-enemy-animation.md`（机制与动画约束；分层素材建议需与当前合成素材区分）

**产物**
- `Enemy109ArtSource/03_SelectedFrames/`（raw / keyed_ui_t79 / deployed_reference / 报告 / 总览图）
- `Assets/Sprites/Enemy/Enemy109/`（36 张，已入库）
- `Assets/Animations/Enemy_109_MountedIdle.anim`、`Assets/Animations/Enemy_109.controller`、`Assets/Resources/EnemyPrefabs/Enemy_109.prefab`
- `Assets/Scripts/Enemy/CavalryVisualController.cs`（三模式路由）、`Assets/Scripts/Enemy/CavalryEnemy.cs`（骑兵阶段）
- `Locus/tools/svl-matting/`（随仓库分发的参数化脚本）

**记录**
- `Library/Locus/tmp/svl_run/109_deploy.log`
- `Library/Locus/tmp/backup_enemy109_20261010-131343/`（18 张替换前备份 + `deploy_report_109.json`）
- `Library/Locus/tmp/svl_run/deploy_109_to_unity.py`、`enemy109_run_workflow.py`
- `Library/Locus/tmp/svl_selftest/report.json`、`Library/Locus/tmp/upload_ready/svl-matting-workflow/`

**本轮诊断（keyed_v5 作废依据）**
- `Library/Locus/tmp/svl_run/hole_enclosed_v6.py`、`hole_threshold_scan_v6.py`、`single_frame_f080_v6.py`
- `Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v6/hole_census/`

---

## 10. 返航与转回Idle视频生产更新（2026-10-11；生产记录，后续接线见 §11）

本节只同步已授权的素材生产结果，不重做 §2 的在库素材，也不重新验收 §5 的T70/T79批次。详细API、提示词与哈希台账见 `memory/video-generation-case-log.md`。

### 10.1 已获用户验收：背向形象与4秒奔跑视频

- 背向形象：`C:/Users/Administrator/Pictures/gptGen/enemy109_retreat_back_startframe_v1.png`，960×960 RGB；用户明确验收。
- 原奔跑视频：`C:/Users/Administrator/Videos/doubaoVideo/enemy109_retreat_run_loop_v1_720p_4s.mp4`，唯一task `task_767c173fa0ea4ff2a5869894ce316d9b`；960×960、24fps、97帧、约4.04s、无音轨，容器和全量解码通过。
- 用户已明确“已验收，先不抠图”。旧代理对尾部趋向站定的预审疑虑不覆盖用户验收；任意循环相位退出与游戏衔接仍未验证。

### 10.2 6秒转身回MountedIdle：视频与24帧已验收，色彩问题单独待处理

- 用户确认真实奔跑末帧为起点、对齐现有MountedIdle1为终点，路径 **背面→马头朝画面左侧的侧身→正面**；审阅6秒双语Prompt后明确“发起生产”。只创建一次 `task_75f597274c3a4dc28fb978b719692522`，最终success，不重新生成。
- 原视频：`C:/Users/Administrator/Videos/doubaoVideo/enemy109_retreat_to_idle_v1_720p_6s.mp4`，3,801,380 bytes；SHA-256 `c01c129c581e0b95df5fe7d427d15e7d18d47e8b2f6fd7b9077ed92127074b7a`。
- 请求 `seedance-2-fast / 6s / 720p / 1:1 / 无音频无水印 / seed=-1`，仅 first_frame + last_frame；实测H.264、960×960、24fps、145帧、约6.04s，末帧名义时间6.000s。MP4结构、FFmpeg全量解码与无音轨核验通过。
- 接触表呈现批准的左侧转向、人马同步转动、换蹄和单根长枪从画面右侧转至左侧；首尾姿态目检贴合参考。旧预审曾记约2.5–3.5s侧身停留较久；用户随后“已验收，请截取有效帧供我验收”，视频已通过，旧疑虑不覆盖验收；仍未宣称全帧解剖、无滑步或Unity衔接通过。
- 全145帧诊断未检测到4px边缘前景触碰，最小L/T/R/B余量28/28/40/65px；**18帧未达到拟定32px安全带**。这是“未检测到裁切”，不是全部构图约束通过；诊断绿色掩码不是Alpha抠图。
- 首帧输入/成片包围框左边只差1px，尾帧包围框相同，蹄底可见行均892；原输入与视频哈希保持。RGB编码像素有差异，源空间对齐不能当作最终Unity锚点/尺寸验收。

### 10.3 恢复路径与下一步边界

- Prompt、批准头尾帧、规格与生产brief：`Library/Locus/tmp/cavalry_concept/retreat_to_idle_v1/`；唯一任务运行记录：`Library/Locus/tmp/cavalry_concept/retreat_to_idle_v1.runtime.json`。
- 审阅：该目录下 `diagnostic_review/` 的全程、侧身、最后16帧、端点对照、145帧WebP、`validation_and_endpoint_report.json`与`findings.md`。WebP重复只用于审阅，原动作是非Loop转换。
- 视频已获用户验收；本地导出24张有效帧候选至 `Library/Locus/tmp/cavalry_concept/retreat_to_idle_v1/keyframes_v1_24/`，4+8+8+4分组，按动作节点去掉相似停顿，真实首尾保留；用户已明确“当前这批挑选帧已验收”，帧序**已验收**；色彩一致性问题单独待处理。原PNG960×960 RGB，24/24与解码像素一致，最低L/T/R/B=35/33/42/66px，均满足32px余量，未缩放/裁切/移位/调色。总览、两版24帧WebP、CSV/manifest及README同目录。
- **最新：用户已授权继续抠图，TurnToIdle24张T79透明PNG完成，透明版待用户验收。** 位置 `Enemy109ArtSource/03_SelectedFrames/TurnToIdle/keyed_ui_t79/`；原帧副本、`raw_manifest.json`、`matting_report.json`、CSV、README及白/灰/深底审阅在同动作目录。7张代表帧小样通过后批量，现行SVL配方逐帧auto_key/T79/softness16/halo1/半透明转不透明，无平滑或全图校色。
- 独立回读24/24：960×960 RGBA、Alpha0/255、核心掩码3257182像素次0删除/改色、raw哈希与来源一致、MP4不变；边缘必要反混合每帧1075–2295像素RGB变化单独留档，最小透明剪影余量37/36/43/67px。白/深底总览与细部源图对照未见明显新增断裂或缺块；末两帧与现有Idle1中性底对照已生成。
- **端点候选v2已按用户要求进行实际抠图与画面验证**：`Enemy109ArtSource/03_SelectedFrames/TurnToIdle/endpoint_review_v2/`，候选f002/f140/f141/f144和所有切换预览均基于T79 RGBA透明输出，未改已验收24帧或其透明版。预览帧像素回读通过；v1中两份绿底源RGB WebP不可作去背证据，v2已重做。
- 证据结论：f001/f002空背景G中位146/145，f144/f145为159/160，±1仍保留约14级跨度，不能解决绿底端点差。f140/f141约153且主体与f138相近，但与Idle1主体差距更大；四候选只是内部收尾与接Idle之间的色彩/动作取舍，尚未替换序列，待用户选择。没有收费API。
- 4秒返航奔跑没有已确认精灵选帧清单，本次未抽选该批。零新Unity导入、Clip或视觉路由接入，未补1108、改玩法参数、保存场景或Git暂存/提交。
- 新诊断：上传首/尾角落中位(0,152,63)/(0,168,60)，未统一输入底色是流程遗漏；生成/编码输出也发生时变，已选f138(0,153,59)→f145(0,160,57)。原始YUV与RGB均时变，不是仅显示问题；主体局部也变化，不能只改背景就承诺修好。诊断全文 `Library/Locus/tmp/cavalry_concept/retreat_to_idle_v1/diagnostic_review/background_color_diagnosis.md`。原片及24张已验收RGB哈希保持；本次完成去背但未修复主体RGB色差，透明结果不能当色差修复已验收。
- 6秒仅是生产源时长，不是游戏到位后的等待时间。后续动画接线见 §11：TurnToIdle已按0.8秒接入现有冷却；真实返航、受阻已用协程探针验证，任意奔跑循环相位退出仍未覆盖。

---

## 11. 本轮动画接线与运行时验证（用户请求「请接入未接入的动画」）

### 11.1 已落盘的资产

- 109专用Clips：`Assets/Animations/Enemy_109_Windup.anim`（6帧，0.3s，非循环）、`Enemy_109_Charging.anim`（6帧，0.2s，循环）、`Enemy_109_Attack.anim`（18帧，0.4s，非循环）、`Enemy_109_TurnToIdle.anim`（24帧，0.8s，非循环）。所有Clip只绑定根`SpriteRenderer.m_Sprite`，无根位移、无AnimationEvent、0空Sprite引用。
- 明确占位Clips：`Assets/Animations/Enemy_109_RetreatPose.anim`（TurnToIdle1背向静态姿势）与`Enemy_109_InterruptedPose.anim`（Charging1静态持枪姿势）；不是返航跑步或专用打断收招的正式动画。
- `Assets/Animations/Enemy_109.controller` 已绑定以上状态；Idle/Attack/Walk等骑乘状态不依赖exit-time跳转，由阶段事件直接驱动，避免通用触发器抢占。骑乘HitFlash/Dead motion保留合成Idle姿势；没有正式骑乘受击/死亡Clip。
- `Assets/Resources/EnemyPrefabs/Enemy_109.prefab` 已序列化六个Clip直接引用、`strikeHitNormalizedTime=0.45`、`turnToIdleDuration=0.8`，保留原GUID、变体关系与旧101/105回退层。未改101源Prefab或共享动画。
- 新增`Assets/Sprites/Enemy/Enemy109/Enemy_109_TurnToIdle1..24.png`及meta。使用既有T79透明帧按完整序号清单导入，未采用f002/f140等额外候选。源输入曾以0.9375倍、(12,20)偏移对齐Idle，因此部署统一Nearest16/15放大到1024、左上(61,99)置于1108画布，恢复现有Idle尺度/蹄底；未逐帧bbox fit、钉脚、改色或更换端点。PPU16、Point、Uncompressed、Center有效pivot(554,554)、无mipmap。

### 11.2 代码播放边界

- `CavalryEnemy.AnimationStage` 是视觉子阶段，玩法`CavalryPhase`与冲锋/返航规则不变。命中前探与收招分开播放：Attack12对应0.18s提交点，先同步采样命中帧再提交原有伤害；微小归一化偏移避免float边界采到前一Sprite。
- `CavalryVisualController` 使用合成人马根Renderer，按阶段调制Animator速率；排号更新不重播Charging。SpawnEntry移动用Charging，返航到位用TurnToIdle，在现有1s冷却中播放，播放时长自动限制不超过冷却，不增加玩法等待。
- `Enemy` 的Idle/骑兵Attack入口委派视觉控制器，普通受击不将人马切成101步兵；闪白、缩放和HitStop继续生效。骑兵协程在本地HitStop期间同Animator一起暂停移动/出伤，避免精灵冻结但逻辑继续。永久落马仍切1011全套。
- 中断刺击的收回从实际中断位置插值，避免先瞬移到完整前探位置再收回；落马时终止骑乘插值。现有数值字段未改。

### 11.3 实际验证及限制

- 完整编译和域重载通过；Clip与Prefab回读确认引用完整。
- Play Mode临时109 Prefab：Idle/Windup/Charging/Attack/Interrupted/Retreating/TurnToIdle实际Animator与Sprite匹配；骑乘受击保留109 Controller，落马切`Enemy_1011.controller`并隐藏旧层。
- 直接运行完整`CavalryCycle`：第3排发起、挡路者row0，3→2→1前移；唯一1次伤害32，目标HP10000→9968，伤害回调当帧Sprite为`Enemy_109_Attack12`；返航1→2→3，TurnToIdle完成后回MountedIdle。
- 前摇、Charging、Striking伤害提交前分别调用打断：三例0伤害、播放InterruptedPose、最终回发起row3。
- 独立返航受阻探针：row1遇row2阻塞，进入RetreatBlocked/背向姿势；移除阻塞并重试后回row3并播放TurnToIdle。落马→ResetEnemy→Initialize复用恢复MountedIdle、109 Controller、speed1。
- 最新Console warn/error=0。路线启动首轮出现过`ColumnManager.ClearAllColumns`初始化顺序异常，未修改该通用代码；当前自然路线的自动启动/转场会干扰孤立探针，因此本轮证据为临时Prefab及协程驱动，不宣称自然路线、真实手势、死亡/多骑兵/全波次回归通过。
- 临时探针对象已销毁，Editor回Edit Mode；本轮未保存场景。Battle及Y场景在探针期间曾显示dirty，随后回读均为clean；未主动保存或清理场景。检测到`BattleYRouteHost.cs`并行文案修改，未覆盖。

### 11.4 仍需后续制作或验收

- 本节为此前接线后的待办快照。成功攻击后的收枪回身已在§12完成f010_v2抠图与部署；背向跑步Loop及打断后的专用入口仍未部署，不倒播刺击冒充返航入口。
- TurnToIdle接线使用当前透明候选；透明版美术验收、上游主体色差和游戏尺度/速度目检仍待用户。4秒返航视频没有新增选帧或抠图；未调用收费API。
- 骑乘受击/死亡动画未制作，死亡仍沿用通用弹起旋转下落流程；后续独立替换，不把本轮视作完整109美术验收。
- 后续用户确认成功攻击后的转向内容，并在审阅头尾副本与词后明确“已确认，发起生成，时长为6秒”。唯一任务 `task_c67ba690deac46f2bb2cb17c31f0471f` 成片已下载/解码：`C:/Users/Administrator/Videos/doubaoVideo/enemy109_strike_to_retreat_v1_720p_6s.mp4`，实际960×960、24fps、145帧、约6.04秒。预审枪尖越框/站定记录保留；用户随后“已验收，请挑选12帧有效帧”，视频已验收。12张原始有效帧已导出 `Enemy109ArtSource/03_SelectedFrames/StrikeToRetreat_v1/raw/`（f001/010/027/035/039/043/072/075/079/087/104/145，真实首尾保留），像素重解码12/12一致，边距最小44/33/38/64px，帧序待用户查看；未抠图/Unity导入。候选、生产记录和预览位于 `Enemy109ArtSource/02_VideoPreparation/StrikeToRetreat_v1/`，恢复入口 `plan/enemy109-turn-to-retreat-video-preparation.md`。历史4秒准备提案已被本次6秒请求覆盖，不重新生成。

## 12. f010 共用 Strike 尾帧与 StrikeToRetreat 头帧（2026-10-11，用户授权抠图替换已完成）

- 用户指定 `StrikeToRetreat_v1/raw/strikeToRetreat02_f010_t0.375.png` 为新的转身头帧及Strike尾帧；本版去掉原f001的部署用途后为11帧（f010/027/035/039/043/072/075/079/087/104/145）。保留原12张RGB与MP4，未收费调用、重生成或补帧。
- 透明成品：`Enemy109ArtSource/03_SelectedFrames/StrikeToRetreat_f010_v2/keyed_ui_t79/`，960×960RGBA、Alpha0/255。SVL逐帧auto/T79/softness16/halo1/硬化，7帧小样及现有Idle同配方回归通过；1,359,179核心像素次0删除/改RGB，14,321边缘像素次反混合，幕布/隐藏RGB0，浅深底未见明显新增细部断裂。
- 部署新增11张 `Assets/Sprites/Enemy/Enemy109/Enemy_109_StrikeToRetreat1..11.png`；PNG为Nearest16/15→1024、置1108左上(61,57)，恢复原Strike源映射，PPU16/Point/Uncompressed/Center554。`Enemy_109_Attack18.png`覆盖为新第1帧，meta/GUID不变，两个PNG字节及像素完全一致；这仅保证两动作公共边界一致，旧Attack17→新18的生成材质/姿势变化和全动作跨色仍需目检。
- 新 `Assets/Animations/Enemy_109_StrikeToRetreat.anim`：11键、30fps、0.366667秒源Clip、非Loop，0空Sprite/位移曲线/事件。109 Controller新增同名状态，Prefab新增 `CavalryVisualController.strikeToRetreatAnimation` 直接引用，保留原Prefab/Controller GUID。`Enemy_109_RetreatPose.anim`改用新f145末帧。
- `CavalryVisualController`仅在成功StrikeRecover进入返航时播放一次新段，动画时长适配剩余返航排数×现有moveSpeed，阻塞/HitStop暂停并从原进度续播。打断收招不使用成功动画；落马/池复用清计时。未改CavalryEnemy移动/出伤代码和0.18/0.22/0.2s参数，不加玩法等待。
- 完整编译/域重载通过。真实StrikeRoutine唯一32伤害且回调当帧Attack12，收招到已替换Attack18；受阻第5帧暂停→恢复不重播→第11帧、提交前打断0伤害、HitStop、落马/复用通过。临时独立列管理器真实CavalryCycle row3→row1→成功转身11帧只进入1次→row3→TurnToIdle→Idle/speed1通过。
- 自然场景的第一次完整循环探针被UI timeScale0干扰超时，清理后隔离场景驱动重测成功；停用场景根导致1条DamageNumber inactive无法StartCoroutine测试环境日志，跳字实现未改，不写Console0。最终回Edit Mode，临时对象0、Battle/Y均clean，未保存场景或Git提交。
- `review/`有白/灰/深底总览、125ms逐帧WebP、Attack17→新18/新头→第2帧中性底对照；来源/抠图/部署/Unity回读与两份runtime报告同目录。透明版及自然游戏速度仍待用户目检，未测试真实手势、多骑兵和全波次，背向奔跑Loop/专用打断入口未部署。
- 旧PNG/meta和相关资产/代码快照：`Library/Locus/tmp/backup_enemy109_strike_to_retreat_f010_20261011-232431/`。最终素材范围和地址以v2 README为准。

