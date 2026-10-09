---
id: kd_1a1a9602-d40f-4f86-a154-7ddcf01d6bbb
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 给另一台机器的自查清单：Battle.scene 差异与同步收尾（2026-10-09）

> **用途**：机器 B（本机）已迁移到重写后的仓库历史并把 6 个提交推送到 `origin/route-scroll-movement`。合并过程中 `Assets/Scenes/Battle.scene` 出现**唯一一处双向差异**，需要机器 A 自查后回复。读完本文即可执行，不需要额外上下文。
> **背景文档**：`plan/two-device-handoff-20261009.md`（双设备总入口）、`plan/git-sync-prep-20261009.md`（B 侧的迁移、提交与清理记录）。

## 0. 先同步（A 机）

```bash
cd <项目目录>
git fetch origin --prune
git status -sb
git merge --ff-only origin/route-scroll-movement   # 或 git pull --rebase
# 回到 Unity 让它重新导入被改动的资产
```

本次同步**没有删除任何仓库根目录文件**（早期计划过的“清理 21 个遗留条目”已取消，原因见下）。

- 背景：本仓库开启了 sparse-checkout（`core.sparseCheckout=true`，`.git/info/sparse-checkout` = `threeKingdomSlayer/*` 并排除 `Assets/Wwise/`、`Assets/StreamingAssets/`），所以仓库根的已跟踪文件本来就不在工作区里、也不出现在 `git status`，属**设计使然**而非遗留垃圾。
- 唯一相关改动：`threeKingdomSlayer/.gitignore` 新增了 `Enemy1011ArtSource/` 规则（B 机上的 518MB 美术素材总包，Unity 不导入、不入库）。
- 若 A 机看到 `.gitattributes` 出现在工作区（B 侧为让 SmartMerge 生效而显式落盘）且 A 侧没有，属预期：它不在 sparse 锥内，按需自行 `git update-index --no-skip-worktree .gitattributes && git checkout -- .gitattributes`。

## 1. 需要 A 机自查的问题

B 侧 `Battle.scene` 与远端旧版本的差异是 `+7 / -27`：

| 侧 | 内容 |
|---|---|
| **B 独有（已提交为 `656ff29c`）** | `BattleYRouteHost` 上 7 行 story-stop 字段：`showRouteChoice: 0`、`useStoryStops: 1`、`e0Distance: 0.3381978`、`e1Distance: 0.58620954`、`storyStopDuration: 3`、`storyStopWaiting: 0`、`storyStopLabel: "E0 战后余烬"` |
| **远端旧版本独有** | Main Camera 上多两个 `MonoBehaviour`（fileID `145927963`、`145927964`）+ GameObject 里两行组件引用；脚本 guid 分别为 `a007016540c477e409f2c9cfa64a23c6`、`18e0960af8ce17046bd942e113b91b64` |

B 机在本机 `Assets/` 内**找不到这两个 guid 的 .meta**，两侧提交树的 Assets 内容完全一致，因此 B 判断它们可能是**悬空引用**（脚本从未提交，或已删除）。
但 B 无法排除另一种可能：**A 机磁盘上存在这两个 `.cs`（未跟踪、因此没入库），场景仍然连着它们**。如果是后者，B 提交的版本会切掉 A 机正在用的组件，必须先补齐脚本再决定。

## 2. A 机怎么查

```bash
# (1) 已跟踪文件里是否有这两个脚本的 .meta
git grep -l "a007016540c477e409f2c9cfa64a23c6" -- '*.meta'
git grep -l "18e0960af8ce17046bd942e113b91b64" -- '*.meta'

# (2) 未跟踪的脚本也要查（git grep 默认只看已跟踪）
grep -rl "a007016540c477e409f2c9cfa64a23c6" threeKingdomSlayer/Assets --include=*.meta
grep -rl "18e0960af8ce17046bd942e113b91b64" threeKingdomSlayer/Assets --include=*.meta

# (3) 历史上是否存在过（可选）
git log --oneline -S"a007016540c477e409f2c9cfa64a23c6" -- '*.meta' | head
git log --oneline -S"18e0960af8ce17046bd942e113b91b64" -- '*.meta' | head
```

Unity 侧（更直观）：

1. 打开 `Assets/Scenes/Battle.scene`，层级里选中 `Main Camera`。
2. 看 Inspector 是否有两个 `Missing (Mono Script)` 组件 —— B 的版本里这两行已被移除。
3. 若有 Missing：看组件上显示的脚本名/或该脚本文件是否在磁盘上（`grep -rl <guid> threeKingdomSlayer/Assets --include=*.meta`）。
4. 旧版本场景可以这样对照：`git show 836411c0:threeKingdomSlayer/Assets/Scenes/Battle.scene | grep -n -A3 "145927963"`。

## 3. 判定与对应动作

| 情况 | 现象 | 动作 |
|---|---|---|
| 1 | A 磁盘上有这两个 `.cs`（未跟踪） | A 先提交脚本（`.cs` + 同名 `.cs.meta`）并推送，然后通知 B 把两个组件加回场景；B 会从 `836411c0` 取回那两行后重新提交 |
| 2 | A 也没有脚本，Unity 显示 `Missing (Mono Script)` | **保持 B 的版本**（组件已移除），A 无需动作；若能顺便确认组件名，B 会记录进文档 |
| 3 | 脚本存在但组件本就是历史残留、不再需要 | **保持 B 的版本**，A 无需动作 |

## 4. 回报模板

```text
自查结论：情况 <1/2/3>
guid a007016540c477e409f2c9cfa64a23c6 -> <脚本路径 / 不存在>
guid 18e0960af8ce17046bd942e113b91b64 -> <脚本路径 / 不存在>
Unity Main Camera：<有/无> Missing 组件，组件显示名：<...>
需要 B 动作：<是/否 + 具体>
```

## 5. 相关提交与文件

- B 侧迁移与清理记录：`plan/git-sync-prep-20261009.md`（§3.1 迁移结果 / §3.2 skip-worktree 遗留 / §3.3 提交记录 / §5 待决策项）
- 相关提交：`656ff29c`（含 `Battle.scene` 与 story-stop 字段）、`83eebeaa`（B 侧推送时远端 tip）
- 对照用旧场景版本：`git show 836411c0:threeKingdomSlayer/Assets/Scenes/Battle.scene`
- 若需 B 侧临时素材副本（不随 git 走）：`Library/Locus/tmp/git-sync-backup-20261009/`（含迁移前 patch、整树快照、`legacy-root-entries/`）

## 6. A 机自查回复（2026-10-09，A 机）

### 6.1 结论（按 §4 模板）

```text
自查结论：情况 2（A 也没有脚本；旧组件是悬空引用）
guid a007016540c477e409f2c9cfa64a23c6 -> 不存在（已跟踪 .meta、磁盘任意 .meta、AssetDatabase.GUIDToAssetPath 三处均为空）
guid 18e0960af8ce17046bd942e113b91b64 -> 不存在（同上）
Unity Main Camera：重载前有 2 个 Missing 组件，显示名不可读（m_Name / m_EditorClassIdentifier 均为空）；重载后 0 个
需要 B 动作：否
```

### 6.2 证据

1. 已跟踪文件：`git grep -l <guid> -- '*.meta'` 两个 guid 均无命中。
2. 磁盘（含未跟踪）：`grep -rl <guid> Assets --include=*.meta` 无命中；全目录任意文件也无命中。
3. Unity 侧：`AssetDatabase.GUIDToAssetPath(guid)` 两个都返回空（“no asset”）。
4. Unity 重载前，`Main Camera` 上确实挂着 2 个 `<Missing script component>`；两个组件在旧场景里除了 `m_Script` / `m_Name` / `m_EditorClassIdentifier` 外**没有任何序列化字段**（无自定义字段内容）。
5. 重载磁盘版本后，`Main Camera` 组件变为 `Transform / Camera / CameraManager / AudioListener / CameraFeedbackController`，Missing 数量为 0。

结论：两个组件是**历史遗留的空壳 MonoBehaviour**，脚本从未入库也不在 A 磁盘上，**保持 B 的版本（移除）是正确的**；A 无需回填脚本，也不需把组件加回场景。若 B 想记录，可按旧场景 block 位置记为“Main Camera 上两个无字段悬空组件（old fileID 145927963 / 145927964）”归档即可。

### 6.3 本次同步在 A 侧的核验记录

- `git fetch` 后 A 为 0 ahead / 8 behind，`git merge --ff-only` 后 HEAD = `475f0ea` = 远端，两边一致。
- 拉取期间遇到并按序处理的两件事，B 侧后续也可能碰到：
  - 首次 ff 合并超时被中断，留下 `.git/index.lock`；确认无 git 进程后 `rm -f .git/index.lock` 即可。
  - 部分克隆（`blob:none`）在合并/`git diff --stat` 时会逐个按需拉取缺失 blob，极慢；改为 `git -c remote.origin.partialclonefilter=blob:limit=200m fetch` 一次性取回新 blob 后再合并，快得多。
- 本次共 90 个文件进入工作区（21 PNG、3 Shader、3 材质、2 场景、2 脚本等）；Unity 导入后控制台 0 error / 0 warning。
- Unity 弹出“打开的场景已在外部被修改（YJunctionSample + Battle）”，已选择**重新加载**；重载后两场景 `dirty=false`、`YJunctionSample` 仍以附加方式加载，无未保存内容丢失（`Temp/__Backupscenes` 最后写入停在 18:19，两个场景当时均无未保存编辑）。

### 6.4 一处需要更正的结论：A 侧**没有**开启 sparse-checkout

B 侧 §0 的说明（仓库根已跟踪文件不在工作区、属稀疏锥设计）**只对 B 克隆成立**。A 侧实测：

```text
git config --get core.sparseCheckout   -> 空
cat .git/info/sparse-checkout          -> 不存在
git ls-files -v | grep -c '^S'         -> 0
git sparse-checkout list               -> fatal: this worktree is not sparse
```

且仓库根已跟踪文件在 A 侧是落盘的（`.gitattributes`、`.gitignore`、`BOSS的QTE攻击设计文档.txt`、`_summaries/*` 等）。因此：

- A 侧不需要 `git update-index --no-skip-worktree .gitattributes`，`.gitattributes` 本来就在工作区且已生效。
- “清理 21 个遗留条目”在 A 侧不成立（它们就是正常跟踪文件）；不要在 A 侧按“稀疏锥设计”去删这些文件。
- 若 B 后续要两边行为一致，建议由 B 侧自行决定是否保留 sparse-checkout；不要把它当成仓库级约定写进文档，避免另一方按不存在的前提操作。

### 6.5 下一步（A 侧视角）

A 侧已完全对齐 `475f0ea`，Unity 两个场景已重载为磁盘版本，可以继续推进。按 `plan/scroll-scene-current-todolist.md` §8，当前优先项是**用户目视验收正式 N1→E0 连续结构**，其后才是 E1/J1 专用美术、门内深度与自然奖励/确认/旅行全流程回归。

### 6.6 对 B 侧 `plan/git-sync-prep-20261009.md` §5 待决策项的回应（A 侧）

| # | B 侧的问题 | A 侧答复 |
|---|---|---|
| 3 | 旧对象回收（B 的 `.git` 仍 4.0GB） | A 侧已完成 `reflog expire + gc --prune=now`，`.git` 403MB。B 侧建议在做完旧 stash/其它分支的内容确认后再回收；回收前先做一次镜像（约 4GB） |
| 4 | `Enemy1011ArtSource/` 是否写进共享 `.gitignore` | **已在 B 侧 `3aea8ab` 落地**；A 侧拉取后确认 `.gitignore` 已含该规则，此项无需再决策 |
| 5 | Locus 状态文件是否 skip-worktree | 建议每台机器各自处理（本地配置，不入库）：`git update-index --skip-worktree threeKingdomSlayer/Locus/workspace-trees/default.json`；`Directory.Build.props` 是 Locus 生成物，同理。A 侧尚未执行，等用户确认 |
| 6 | 其它分支 / 旧 stash 是否也迁移 | A 侧：11 个远程分支已在重写时一并重写并强推；`docs-sync-route-fake-movement`、`docs-sync-route-scene-v2-baseline` 仅存于 A 本地（未推）。B 侧的 3 条 `main` 旧 stash 属于旧历史，建议先 `git stash list` / `git stash show -p` 确认内容是否还要，需要就导出成补丁，再决定丢弃 |
| 7 | `Battle.scene` 唯一差异 | **已查清，见本文 §6.1–6.2：情况 2**。两个 guid 在 A 磁盘完全不存在（已跟踪/未跟踪 `.meta` 均无命中、`AssetDatabase.GUIDToAssetPath` 返回无资产），Unity 重载前它们显示为 2 个无字段的 Missing 空壳组件；保持 B 的版本（移除）正确 |

补充一条 B 侧遗留事实供参考：B 的 `.git` 4.0GB 意味着重写前的旧历史仍在那台机器上；它不影响功能，但会拖慢该机的一切全库操作。
