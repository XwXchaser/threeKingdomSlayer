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
