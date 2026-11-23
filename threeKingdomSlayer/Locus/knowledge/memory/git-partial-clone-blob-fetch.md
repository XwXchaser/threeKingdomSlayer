---
id: kd_b45c9e13-f4b0-4899-bc6d-540880a28114
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_git_partial_clone_blob_fetch_20261103
injectMode: inherit
aiEditMode: inherit
---

# 本仓库是 blobless partial clone：合并时批量取 blob 会断

**现象**（2026-11-03 实测）：`git fetch` 本身成功（提交与 tree 都拿到了），
但**一旦要写文件**（merge / checkout / `git show --stat`），git 会去远端**懒取 blob**，
然后报：

```
fetch-pack: unexpected disconnect while reading sideband packet
fatal: early EOF
fatal: index-pack failed
```

**根因**：`remote.origin.promisor = true`、`remote.origin.partialclonefilter = blob:none`
→ 图片等大 blob 不在本地；批量懒取时连接被服务端切断（`http.postBuffer` / `lowSpeedLimit` 已经调过也没用）。

**可用解法（按这个顺序）**：

1. **逐个预热 blob**（有效，本次 67 个 blob 用这个全部拿到）：
   ```bash
   for sha in $(git rev-list --objects --missing=print <远端commit> ^<共同祖先> | grep "^?" | cut -c2-); do
       git cat-file -s "$sha" >/dev/null 2>&1 || echo "FAIL $sha"   # 触发单对象懒取
   done
   ```
   大文件（>1MB）容易一次超时，**重试一次通常就好**。
2. 预热完再 `git merge` → 不再需要批量拉取 → 正常合并。
3. 单个文件想看内容时，路径要带仓库根前缀：
   `git show <commit>:threeKingdomSlayer/Locus/...`（仓库根下有一层 `threeKingdomSlayer/`）。

**注意**：`.meta` 里的 guid 是 **base64**、资产引用里是 **hex**，两者不是简单 base64/hex 互转，
**别指望用脚本从 `.meta` 反查场景里的引用**（`base64decode(meta_guid).hex()` 得到的串和场景里的对不上）。

## 直接 `git pull` 的操作配方（2026-11-19 实测，330 文件 / +26k 行）

1. **必须放后台任务跑**。`git pull --ff-only` 会先完成 ref 更新（秒级），**再回填 blob**；
   本次实测下载速度只有 **~55 KB/s**，`git status` 里 "behind 14" 一动不动就是这个阶段，
   整个过程 **约 14 分钟**。前台同步命令会超时并被中止 ⇒ 用 `async + notify` 的 bash 任务。
2. **只读命令同样会触发懒取**。`git diff --stat a..b` 在 blob 没回填时**会卡住**（本次 120s 超时），
   别误判成"仓库坏了"。等 pull 完成后同样的命令立刻就快。
3. **中止会留垃圾，必须手动清理**才能重试：
   - `<gitdir>/index.lock`（0 字节）——删掉才能再 pull；
   - `<gitdir>/objects/pack/tmp_pack_*`（中断时的半成品 pack，几十 MB）——删掉回收空间；
     `git gc` 也会清，但当场没必要跑。
   - 删除前先确认没有 git 进程在跑（`Get-Process git`）。
4. **网络**：SSH 通道可能不通（DNS/端口被挡）而 HTTPS 通 ⇒ `git fetch https://<host>/<repo>.git "+refs/heads/*:refs/remotes/origin/*"` 也能写回同样的 ref。
   `git fetch --all` 连不上时用 HTTPS 手动 fetch 一次即可。
5. **拉完后 Unity 会弹「打开的场景已在磁盘上被改变」模态框** → **选「重新加载」**，
   否则编辑器拿着旧场景实例，会误判"新增的 Prefab / 技能没注册进场景"。
   详见 `memory/project-mistake-note.md` 的「反向变体」条目。
