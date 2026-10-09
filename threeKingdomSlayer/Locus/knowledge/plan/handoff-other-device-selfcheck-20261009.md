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
