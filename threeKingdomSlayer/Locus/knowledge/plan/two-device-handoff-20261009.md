---
id: kd_6021c98b-44e8-44b7-939e-82f46cbe7b67
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
summary: 双设备项目交接入口（2026-10-09）：仓库历史已重写（Wwise/apk 剔除，.git 932MB→403MB），含另一台主机的迁移命令、SmartMerge 驱动一次性配置、日常提交流程、角色并行制作契约摘要与相关文档索引。
---

# 双设备项目交接（2026-10-09）

> **用途**：从另一台主机接手/恢复本项目的入口文档。读完本文就能知道工程当前处在什么状态、必须做哪些一次性动作、之后怎么和另一台机器往返同步。
>
> **权威边界**：本文是"当前状况 + 必须动作"的入口摘要；角色制作契约细节以 `plan/two-character-parallel-production.md` 为准，历史重写细节以 `memory/git-history-rewrite-20261008.md` 为准。

## 0. 如果你在另一台主机上（先执行这段）

本工程的历史已在 2026-10-08 被重写（剔除废弃 Wwise 与构建产物），**所有 commit hash 已改变**。旧克隆不能 `git pull`——拉取只会把旧历史混回来。要拿到本文内容，必须先迁到新历史：

```bash
# 旧克隆里有未推送的提交？先导出（否则迁移后无法恢复）
git format-patch origin/route-scroll-movement -o ../patches
git diff > ../local-work.patch

# 迁移（二选一）
# A. 原地切换
git fetch origin --prune                                   # 输出里的 forced update 就是信号
git reset --hard origin/route-scroll-movement
git reflog expire --expire=now --all && git gc --prune=now  # 丢掉约 500MB 旧对象
# B. 重新克隆（更干净）
git clone --filter=blob:none git@github.com:XwXchaser/threeKingdomSlayer.git threeKingdomSlayer

# 需要时重放补丁
git am ../patches/*.patch
```

迁完后执行第 3 节的一次性配置，再按第 2 节工作。**任何情况下都不要 `git push --force` 旧历史，也不要手工拷贝 `Assets/` 目录。**

## 1. 当前权威状态（2026-10-09）

| 项 | 值 |
|---|---|
| 开发分支 | `route-scroll-movement`（两台设备都用这一条） |
| 本机 HEAD | `57959a6` |
| 远程 tip | `57959a67d6b8378b48270d12b113ee4c671a6330`（与本机一致） |
| 引擎 | Tuanjie 2022.3.62t7（Unity 2022.3 系）。**两台必须完全一致**，否则触发全量重导与序列化漂移 |
| 仓库体积 | `.git` 403 MB（重写前 932 MB） |
| 本机克隆方式 | 部分克隆（`remote.origin.promisor=true`、`partialclonefilter=blob:none`），历史里的大文件按需拉取 |
| 备份镜像 | `C:/threeKingdomSlayer-history-backup-20261008.git`（926 MB，含重写前全部 68 个引用，确认无误后可删） |
| 唯一待办 | `plan/two-character-parallel-production.md` 第 2 节的 enemyId 登记表（用户自行登记） |

## 2. 双设备日常同步 SOP

```bash
# ---- 在 B（提交并上传）----
cd <项目目录>                 # 等 Unity 导入完成，避免刚生成的 .meta 还在写
git status                    # 确认没有遗留冲突
git pull --rebase             # 先对齐远程，避免 push 被拒
git add -A
git commit -m "feat(敌人10xx): ..."
git push

# ---- 在 A（同步 B 的改动）----
git fetch origin --prune
git status -sb
git merge --ff-only origin/route-scroll-movement   # 或 git pull --rebase
# 回到 Unity 让它重新导入被改动的资产
```

规则：同一条分支；用 `pull --rebase` 保持线性历史；**禁止 `push --force`**；不手工拷贝 `Assets/`（`.meta`/GUID 会造成"同名不同 GUID"或"同 GUID 两份资产"）；冲突时 Unity 资产已配 SmartMerge（远距离改动自动合并，真冲突标 `UU` 并打印字段路径，改用 Locus 结构化合并或人工裁决）。

## 3. 一次性：每台机器的本地配置

```bash
TOOL="C:/Program Files/Tuanjie/Hub/Editor/2022.3.62t7/Editor/Data/Tools/TuanjieYAMLMerge.exe"
git config merge.tuanjieyamlmerge.name   "Tuanjie SmartMerge"
git config merge.tuanjieyamlmerge.driver "\"$TOOL\" merge -p --force %O %B %A %A"
```

`--force` 不能省：Git 传给驱动的临时文件名形如 `<file>.prefab_BASE_<n>`，而 `TuanjieYAMLMerge.exe` **按扩展名派发处理器**，扩展名被解析成 `prefab_BASE_n` 后直接失败。加 `--force` 后实测 `.prefab/.scene/.asset/.controller/.anim/.mat` 的远距离改动都能自动合并；同行真冲突返回非 0，Git 标记 `UU`。

## 4. 本轮已完成的仓库优化（不影响写代码，但必须知道）

- **历史重写**：剔除 `Assets/Wwise/**`、`threeKingdomSlayer_WwiseProject/**`、`Assets/Scripts/Managers/WwiseAudioManager.cs`、`threeKingdomSlayer.apk`。唯一 blob 从 1891 MB 降到约 560 MB，`.git` 从 932 MB 降到 403 MB。保留 `BOSS.psd`（源美术）与字体。
- **新增 `.gitattributes`**（提交 `bd7866a`、`32941a6`）：`* text=auto`（索引统一 LF，本身无重规范化改动）、二进制标记（png/psd/wav/mp4/ttf/fbx/dll/apk 等）、Unity 文本资产指向 `tuanjieyamlmerge`。
- **`.gitignore` 修正**：原规则写的是 `.apk`（只匹配名为 `.apk` 的文件，实际不生效），已改为 `*.apk` / `*.aab`。
- **LFS 暂不启用**：仓库体积已可控，拉取成本由部分克隆覆盖；GitHub 免费额度（1GB 存储 / 每月 1GB 流量）超限会阻塞推送。重启条件与迁移步骤见契约文档第 7 节。
- **未清理项（有意保留）**：`Assets/Library/Locus/**` 是 Locus 自己的知识索引缓存（内部文件已被 `.gitignore` 的 `Library/` 规则忽略），`Assets/Library.meta` 是它的文件夹 meta、未跟踪。不要删——删了会重建，还可能中断正在运行的索引。

## 5. 角色并行制作契约摘要（详细见 `plan/two-character-parallel-production.md`）

- **新增角色 = 纯新增文件**：`EnemyPool` 在 Awake 扫描 `Assets/Resources/EnemyPrefabs`，按文件名 `Enemy_<数字>` 自动登记 enemyId，不需要改任何中心注册表；编辑器工具（战斗数值总表、命中帧校验）同样扫目录。
- **目录与命名**：`Assets/Sprites/Enemy/<角色>/`、`Assets/Animations/Enemy_<id>_<Action>.anim`、`Assets/Animations/Enemy_<id>.controller`、`Assets/Resources/EnemyPrefabs/Enemy_<id>.prefab`、角色专属脚本 `Assets/Scripts/Enemy/<角色><功能>.cs`。
- **已占用 enemyId**：1、101、102（惧战兵，专属状态集）、103–108、109（骑兵，专属机制组件）、1011。基础角色建议用 110 / 120，变体沿用 `101 → 1011` 的加位惯例。
- **Prefab 必需组件**（对齐 `Enemy_1011.prefab`）：`Enemy`、`SpriteRenderer`、`BoxCollider`、`EnemyHealthBar`、`Animator`、攻击预警锚点。
- **导入参数**：PPU 16、Point、Uncompressed、alphaIsTransparency、Sprite/Single、Mipmaps 关。画布与 pivot **同角色内统一**（pivot 取该角色 idle 首帧；实测 `Enemy_1011` = 1108×1108 + pivot 0.6539/0.5487，`Enemy2` = 512×512 + pivot 0.5/0.5），跨角色不统一。
- **Animator 公共状态名是硬契约，不得改名或挪用**：`Idle`、`Attack`、`HitFlash`、`HitLeft`、`HitRight`、`Dead`、`Walk`、`Launched_Rise`、`Launched_Fall`、`Launched_Getup`。缺 `HitLeft/HitRight` 会自动回退 `HitFlash`（安全）；缺 `Launched_Getup` 会让击飞落地退化。
- **角色独有状态与机制**：用角色前缀命名（如 `RaiderIdle`），由角色专属脚本驱动，照既有先例 `CavalryVisualController`（多个 `RuntimeAnimatorController` 序列化字段按机制阶段路由，不动 `Enemy.cs`）或 102 的 `IsCoward` 专属状态集。改 `Enemy.cs` 公共播放逻辑时集中一人、单次提交、在契约文档第 9 节登记。
- **测试入口隔离**：每个角色用自己的测试场景与测试关卡资产，不共用 `Assets/Scenes/Battle.scene` 与 `Assets/Experiments/CurvedScroll/RouteTrialData/SmallBattle.asset`；接线由装配人（同一人）一次性完成。
- **验收清单**：导入设置回读、Clip 帧数/fps/stopTime/Loop、Controller 状态接线、运行时（出场/受击方向/攻击命中帧/死亡/击飞 Rise→Fall→落地→Getup）、独有机制触发与打断恢复、脚底锚点与碰撞盒血条。全表在契约文档第 6 节。

## 6. 相关文档（按顺序读）

1. `plan/two-character-parallel-production.md` — 并行制作与汇合契约主文档（含 ID 登记表、目录契约、验收清单、体积与迁移章节）。
2. `memory/git-history-rewrite-20261008.md` — 历史重写的后果、操作要点与教训。
3. `plan/enemy1011-animation-deployment-handoff.md` — 角色动画交付文档模板（资产表 / Clip 参数 / 状态表 / 验证记录）。
4. `design/pixel-character-and-enemy-animation-spec.md` — 像素角色美术与动画硬性规范。
5. `skill/workflows/character-hit-animation-video-workflow.md` — 动作素材（视频→选帧→去背→部署）生产流程。

## 7. 本机（机器 A）工作区现状与已知小坑

- 工作区只剩 Locus 自生成文件处于修改态（`threeKingdomSlayer/Directory.Build.props`、`Locus/workspace-trees/default.json`），属机器本地状态，**不要提交**。
- Unity 重写序列化后偶尔出现"stat 脏但内容一致"的文件：`git status` 显示 ` M`、`git diff` 为空。`git add <file>` 即可清掉标记，不要把它当成改动提交。
- 场景扩展名是 `.scene`（3 个），另有 `.unity`（6 个），`.gitattributes` 两种都已覆盖。
