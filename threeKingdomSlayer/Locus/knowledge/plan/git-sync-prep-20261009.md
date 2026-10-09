---
id: kd_c1a171da-5f8e-4366-9c46-ed14214c606a
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# Git 同步准备（2026-10-09，本机 / "另一台主机"侧）

> **用途**：把本机接入重写后的仓库历史并完成一次双向同步的**准备清单 + 可执行步骤**。远端入口文档：`plan/two-device-handoff-20261009.md`（本机此前没有，已从 `origin/route-scroll-movement` 读出并另存到 `Library/Locus/tmp/git-docs/`）。
> **当前边界**：本文件只记录准备动作与待决策项。**迁移（reset --hard）、旧对象回收（gc --prune）、推送都还没有执行**，都需要用户明确批准。

## 1. 本机现状 vs 远端交接文档的假设（实测）

| 项 | 文档假设（撰写机） | 本机实测 | 结论 |
|---|---|---|---|
| 克隆方式 | 部分克隆 `blob:none` | `remote.origin.promisor=true`、`partialclonefilter=blob:none` | 一致 |
| `.git` 体积 | 403MB（重写后） | **4.0GB** | 旧对象仍在本机，未回收 |
| 本机 HEAD / 远端 tip | 57959a6 / 相同 | `a08bc487` / `836411c0`，**ahead 202 / behind 207** | 本机仍是重写前历史，需按 §0 迁移 |
| 工作区 | 只剩 Locus 自生成文件 | **12 个已跟踪修改 + 51 个有效未跟踪条目** | 有本机专属真实工作，不能直接 `reset --hard` |
| 历史备份镜像 | `C:/threeKingdomSlayer-history-backup-20261008.git`（926MB） | **本机不存在** | 老历史目前只存在于本机 `.git` |
| SmartMerge 驱动 | 已配置 | **未配置** → 已补配置（见 §2） | 已解决 |
| `.gitattributes` | 已入库 | 本机 HEAD 没有（远端 tip 才有） | 迁移后自动就位 |

已提交内容两边基本对齐：本机 HEAD 与远端 tip 的**全仓树差异只有 6 个路径**（新增 `.gitattributes`、`.gitignore` 微调、被移除的 `threeKingdomSlayer.apk`、远端新增的 3 篇文档）。**风险全部在未提交部分**，且双向都有新内容：

- 本机比远端 tip 新：`memory/video-generation-case-log.md`(457 vs 341 行)、`plan/cavalry-enemy-visual-handoff.md`(850 vs 532)、`Scripts/Core/BattleYRouteHost.cs`(169 vs 135)、`YScrollSample.cs`、`YJunctionSample.unity`、`N1_ArtPreview_Battle.asset`、`ProjectSettings.asset` 等。
- 远端 tip 比本机新：`Assets/Scenes/Battle.scene`(17249 vs 17223 行) + 3 篇新文档（`two-device-handoff`、`two-character-parallel-production`、`memory/git-history-rewrite-20261008`）。

## 1.1 领先关系（**本机是领先版本**，但必须分清两处）

**结论：本机（机器 B）是本项目的领先版本**，最新开发成果都在这里。但 `ahead 202 / behind 207` 会把这件事读错，所以要按"已提交 / 未提交"分开记：

- **未提交部分是领先的，也是唯一真源**：`memory/video-generation-case-log.md`(457 行 vs 远端 341)、`plan/cavalry-enemy-visual-handoff.md`(850 vs 532)、`Scripts/Core/BattleYRouteHost.cs`(169 vs 135)、`YScrollSample.cs`、`YJunctionSample.unity`、`N1_ArtPreview_Battle.asset`、`ProjectSettings.asset`、`october-milestone-plan.md`、`image-asset-generation.md`；另有 51 个未跟踪条目（Enemy109 十二帧、E0 场景/材质/Shader、11 篇知识库文档、518MB 的 `Enemy1011ArtSource/`）**只存在于本机**。迁移必须原样保住这些，不接受被远端版本覆盖。
- **已提交部分不是领先的，也不需要是**：本机 HEAD 与远端 tip 的**全仓**树差异只有 6 个路径（新 `.gitattributes`、`.gitignore` 微调、远端 3 篇新文档、被远端移除的 apk）；本机 `a08bc487` 与新历史里消息相同的 `d501f581` 逐文件比对**只差 apk 一个二进制**。即 `ahead 202` 是"同内容不同 hash"的假象，不代表远端缺了本机 202 个提交。
- **唯一一处远端更新的是 `Assets/Scenes/Battle.scene`**（远端 17249 行 vs 本机 17223 行，差 26 行）。这一处不能默认本机领先：重放 patch 时**必须显式裁决**（保留本机版 / 采纳远端 +26 行 / 用 Locus 结构化合并），不允许静默覆盖任何一侧。

> 记这条的目的：迁移和后续提交都以"本机工作区内容为准"，但不能因此丢掉远端唯一更新的那个文件。

## 2. 已完成的准备（都是本地、可回退）

1. **安全备份** → `Library/Locus/tmp/git-sync-backup-20261009/`（225MB）
   - `local-work-20261009.patch`（1,444,011 字节）：12 个已跟踪修改文件的 `git diff --binary HEAD`，**排除 234MB 的 apk**。
   - `threeKingdomSlayer.apk`（234,221,849 字节）：它是被跟踪文件，而新历史已删除它 → `reset --hard` 会把它从磁盘删掉，故单独留一份。
   - `git-status-20261009.txt`(70 行)、`refs-20261009.txt`(231 行)、`stash-list.txt`(3 条旧 stash)、`local-docs/`（4 篇本机独有的知识文档副本）。
   - `migrate-in-place.sh`：迁移脚本，默认 dry-run。
   - 注意：该目录在 `Library/` 下（gitignore），**如果改用"重新克隆"方案会随之消失**，届时要先复制到仓库外。
2. **SmartMerge 一次性配置**（本机 repo 级，已回读确认）
   - `merge.tuanjieyamlmerge.name = Tuanjie SmartMerge`
   - `merge.tuanjieyamlmerge.driver = "C:/Program Files/Tuanjie/Hub/Editor/2022.3.62t7/Editor/Data/Tools/TuanjieYAMLMerge.exe" merge -p --force %O %B %A %A`
   - 工具本体已确认存在（982,360 字节）。`--force` 按文档必需。
3. **工作区整树快照（方案 A 的保险层）** → `Library/Locus/tmp/git-sync-backup-20261009/worktree-snapshot/`
   - 89 个文件 / 22.6MB：所有未跟踪的真实工作（Enemy109 精灵 11.3MB、E0 场景与材质 10.3MB、11 篇知识文档、`YJunctionSample.unity`/`Battle.scene` 等）+ 11 个已跟踪修改文件的**完整副本**（按仓库相对路径存放）。
   - 跳过：234MB apk（已单独备份）、518MB `Enemy1011ArtSource/`（整包另存于项目根，未被跟踪、不受迁移影响）。
   - 作用：即使 patch 重放失败或冲突，本机领先内容也能按文件原样取回。
4. **本机专属忽略规则** → 写入 `.git/info/exclude`（**只在本机生效，不动共享 `.gitignore`**），已用 `git check-ignore -v` 逐条验证：
   - `**/Enemy1011ArtSource/`（518MB 美术总包）、`**/threeKingdomSlayer_BurstDebugInformation_DoNotShip/`、`*.stackdump`、`**/NUL`、3 个 `pelican*.html`。
   - 效果：待处理条目从 70 降到 63（其余是真实工作 + 12 个已跟踪修改）。

## 3. 迁移步骤（未执行）

脚本：`bash Library/Locus/tmp/git-sync-backup-20261009/migrate-in-place.sh`（先 dry-run，加 `--go` 才执行；`--go --gc` 才回收旧对象）。等价手工命令：

```bash
cd H:/Project/threeKingdomSlayer
git fetch origin --prune
git reset --hard origin/route-scroll-movement                 # 切到重写后的历史
cp <备份>/threeKingdomSlayer.apk threeKingdomSlayer/threeKingdomSlayer.apk   # 新历史已删该文件
git apply --3way --exclude=threeKingdomSlayer/Locus/workspace-trees/default.json <备份>/local-work-20261009.patch
# 冲突时：Unity 资产用 Locus 结构化合并 / SmartMerge，禁止按行硬合
# 可选、不可逆：git reflog expire --expire=now --all && git gc --prune=now
```

**执行前必须做**：在 Unity 里保存并关闭工程（`Assets/Scenes/Battle.scene` 会被覆盖；迁移后重新打开会触发重导）。

### 3.1 迁移执行结果（2026-10-09，**已执行**，未跑 gc）

- `HEAD` = `836411c0`（重写后 tip），分支与 `origin/route-scroll-movement` **无 ahead/behind**；迁移前 HEAD `a08bc487` 已写入备份的 `pre-migration-head.txt`。
- **patch 重放 10 个文件全部 cleanly 应用**（无冲突、无 `.rej`），并用 `git hash-object` 逐个校验：**内容与本机迁移前快照逐字节相同**，即本机领先版原样保留 —— `YScrollSample.cs`、`YJunctionSample.unity`、`N1_ArtPreview_Battle.asset`、`Battle.scene`、`BattleYRouteHost.cs`、`video-generation-case-log.md`、`cavalry-enemy-visual-handoff.md`、`october-milestone-plan.md`、`image-asset-generation.md`、`ProjectSettings.asset`。
- `threeKingdomSlayer.apk` 已从备份恢复（234MB），未被跟踪且被 `*.apk` 忽略。
- 未跟踪真实工作原样保留 52 条（Enemy109 24 / E0 16 / 知识库 12）；`Enemy1011ArtSource/` 1070 个文件 518MB 完好；备份目录 89 个快照文件完好。
- Unity 侧：重编译通过（`status: compiled`、domain reload 完成）、Console error 数 0。
- **未执行**：`gc --prune=now`（本机旧历史仍在）；未提交、未推送。当前索引里 10 个 patch 文件已由 `git apply --3way` **暂存**，未暂存 1 个（workspace-trees）。

### 3.2 迁移中发现的两件事

**(1) 索引 skip-worktree 遗留（已处理一个）**

`git ls-files -v` 显示本机索引里有 **25 个仓库根目录条目带 `S`（skip-worktree）标志，且这些文件在磁盘上全部不存在**：`_summaries/*`、`plans/*`、`一夫当关*.txt`、`gen_report.py`、`gptPic.html`、`temp_tree.txt`、`BOSS的QTE攻击设计文档.txt`、`__pycache__/gpt_pic.cpython-313.pyc`、根 `.gitignore`。skip-worktree 使 git 既不还原它们、也不把缺失报成删除。

- 这些文件在工程目录下有**另一套同名且已被跟踪**的副本（如 `threeKingdomSlayer/_summaries/*`、`threeKingdomSlayer/plans/*`），但两组**内容不同**（抽样比对 blob 不一致）→ 是旧扁平布局的遗留，**建议保持现状、不要“恢复”**，否则仓库根会多出一套冗余副本。
- **已处理**：`.gitattributes` 也在这一组里，而它是 SmartMerge 的必需配置 → 已 `update-index --no-skip-worktree` + `checkout` 落盘，并确认索引 blob 与远端 tip 完全一致（`bbc4f775…`）。现在 `git check-attr` 正确返回 `Battle.scene`/`.anim` → `merge: tuanjieyamlmerge`。
- 根 `.gitignore` 缺失意味着它那套 `threeKingdomSlayer/Library/` 之类规则**当前不生效**；但工程目录内的 `threeKingdomSlayer/.gitignore` 已覆盖同样目标（`Library/`、`*.apk`、`*.csproj` 等），实测无副作用。

**(2) 唯一需要裁决的差异：`Battle.scene`（见 §5 第 7 条）**

## 4. 迁移后待提交清单（分类）

**A. 真实工作（应入库）**

| 组 | 内容 | 条目 |
|---|---|---|
| 敌人 109 美术 | `Assets/Sprites/Enemy/Enemy109/Enemy_109_Charging1..6`、`Windup1..6`（png+meta） + `memory/scene-art-production-pipeline.md` | 24+1 |
| E0 场景与材质 | `Assets/Experiments/CurvedScroll/`：`E0OuterGatePreview.unity`、`Art/EnvironmentV2/E0/`、3 个 `*_E0Route.mat`、3 个 `Y*E0Route.shader`（各含 meta） | 16 |
| Y 卷轴/战斗接线 | `Scripts/Core/BattleYRouteHost.cs`、`YScrollSample.cs`、`YJunctionSample.unity`、`RouteData/Stage01/N1_ArtPreview_Battle.asset`、`Scenes/Battle.scene`、`ProjectSettings.asset`（已跟踪修改） | 6 |
| 知识库文档 | 11 个未跟踪（E0/N1 提示词与交接 7 个、`scroll-scene-current-todolist.md`、`enemy1011-frame-provenance.md/.csv`）+ 3 个已跟踪修改（`video-generation-case-log.md`、`cavalry-enemy-visual-handoff.md`、`october-milestone-plan.md`、`image-asset-generation.md`） | 15 |

**B. 不入库（本机状态 / 已废弃产物）**

| 内容 | 处理 |
|---|---|
| `Locus/workspace-trees/default.json`（已跟踪、被改） | 按交接文档 §7 不提交；patch 重放时已显式排除 |
| `threeKingdomSlayer.apk`（234MB，已跟踪、被改） | 新历史已删除且 `*.apk` 已忽略；只在本机保留（已备份） |
| `Enemy1011ArtSource/`（518MB） | 已本地忽略；是否同时写进共享 `.gitignore` 待定（§5） |
| Burst 调试目录、`grep.exe.stackdump`、`NUL`、3 个 `pelican*.html` | 本机垃圾，已忽略；建议直接删除 |

建议提交分组（可合并）：① 敌人 109 美术 + 场景管线文档 → ② E0 场景/材质 → ③ Y 卷轴接线与场景 → ④ 知识库文档。**注意**：`git apply --3way` 已把 10 个文件放进暂存区，提交前先 `git status` 复核暂存内容。禁止 `push --force`，`push` 前需用户批准。

## 5. 待决策项

1. ~~迁移时机~~ → **已决定（方案 A）**：先做工作区整树快照（已完成）→ 更新本文档 → 执行迁移（`--go`，**不带 `--gc`**）。Unity 实测两场景 `dirty=False`，不存在未保存的内存状态会被写回。`Battle.scene` 的 26 行差异按 §1.1 显式裁决，不静默覆盖。
2. **apk**：确认 234MB 构建产物只需在本机保留、不入库（新历史已移除）。
3. **旧对象回收**：`gc --prune=now` 会把本机 4.0GB `.git` 里的**重写前旧历史**彻底删掉，而本机没有文档里提到的 926MB 镜像备份。要么先做一次镜像备份（约 4GB，磁盘当前剩 72GB），要么接受旧历史不可恢复。
4. **`Enemy1011ArtSource/` 的忽略规则**：是否把 `Enemy1011ArtSource/` 提升写进**共享 `.gitignore`**（两台机器一致），还是继续只在本机 `.git/info/exclude`。
5. **本机 Locus 状态文件**：`Locus/workspace-trees/default.json`（以及 `Directory.Build.props`）是否要 `git update-index --skip-worktree` 永久静默，避免每次 `git status` 都出现。
6. **其它本地分支/stash**：`dev`/`main`/`test`/`experiment/*` 都处于 ahead/behind（同样受历史重写影响），还有 3 条 `main` 上的旧 stash。本次只处理 `route-scroll-movement`；其它分支是否也要迁，请给范围。
7. **`Battle.scene` 的唯一差异（需你/另一台机器确认）**：合并后本机版相比远端 tip 是 `+7 / -27`。
   - **本机独有**：`BattleYRouteHost` 上的 7 行 story-stop 字段（`showRouteChoice: 0` + `useStoryStops: 1` + `e0Distance` / `e1Distance` / `storyStopDuration` / `storyStopWaiting` / `storyStopLabel: "E0 战后余烬"`），与本地 `BattleYRouteHost.cs`(169 行) 的新逻辑配套。
   - **远端独有**：Main Camera 上多两个 `MonoBehaviour`（fileID `145927963`、`145927964`）及 GameObject 里两行组件引用；它们的脚本 guid `a007016540c477e409f2c9cfa64a23c6`、`18e0960af8ce17046bd942e113b91b64` **在本机 Assets 内无任何 .meta 命中**，而两侧提交树的 Assets 内容完全一致 → 远端版本很可能是**悬空引用**（或对应脚本从未提交）。
   - 现状：本机版已不含这两个组件，`git status` 干净、Unity 重编译通过、Console 无错误。**请另一台机器确认那两个脚本是否还以未跟踪文件形式存在于那台机器**：若有，先把脚本提交再决定是否把组件加回场景；若无，保持本机版。
8. **根目录 24 个 skip-worktree 遗留条目**（见 §3.2）：保持现状不恢复，还是趁这次整理把它们 `git rm --cached` 从索引清掉（会同时影响另一台机器，建议单独立项）。

## 6. 参考

- `plan/two-device-handoff-20261009.md`（远端，本机副本：`Library/Locus/tmp/git-docs/two-device-handoff-20261009.md`）
- `plan/two-character-parallel-production.md`（远端 tip 才有的契约主文档；本机尚未落地，迁移后会出现在工作区）
- `memory/git-history-rewrite-20261008.md`（同上）
