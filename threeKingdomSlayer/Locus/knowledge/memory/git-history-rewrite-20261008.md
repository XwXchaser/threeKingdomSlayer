---
id: kd_ccd86951-7a5b-4bcc-a3e9-81cf38c1bd50
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
summary: 2026-10-08 用 git filter-repo 重写仓库历史剔除废弃 Wwise 内容与 apk（.git 932MB→402MB）；含重演远程提交、部分克隆、备份镜像位置与「旧克隆必须重新克隆」的后果。
---

# 2026-10-08 历史重写与仓库瘦身

## 结论

仓库历史已用 `git filter-repo` 重写，剔除了已废弃的 Wwise 内容与构建产物。`.git` 从 932 MB 降到 402 MB。**所有 commit hash 已改变，旧克隆不能 `git pull`，必须重新克隆。**

## 做了什么

- 剔除路径：`Assets/Wwise/**`、`Assets/Wwise.meta`、`threeKingdomSlayer_WwiseProject/**`、`Assets/Scripts/Managers/WwiseAudioManager.cs(.meta)`、`threeKingdomSlayer.apk`。
- 重写前唯一 blob 共 1891 MB，其中 Wwise 占 1266 MB、插件二进制 895 MB、apk 40 MB。运行时仍在用因而保留：`BOSS.psd`（42.9 MB 源美术）、字体 ttf 与 TMP SDF。
- 13 个本地分支全部重写；11 个远程分支 `git push --force` 成功。推送时刻远程上有来自另一台机器的两个提交（`68f7843b`、`a08bc487`），已用 `git diff` + `git checkout <ref> -- <paths>` 逐文件重放到重写后的历史上，内容逐文件核对一致。
- 备份镜像：`C:/threeKingdomSlayer-history-backup-20261008.git`（硬链接，含重写前全部 68 个引用）。

## 复现与操作要点

- `git filter-repo` 会**删除 origin 远程**、清空远程跟踪引用并执行 `reset --hard` + `gc --prune=now`，所以本机未提交的改动会丢失 —— 动手前把改动快照到 `Library/Locus/tmp/`。
- filter-repo 只重写当时存在的引用。若某分支落后远程，必须先把远程提交纳入（或按 `git diff` + `git checkout` 重放），否则强推会丢掉那些提交。本次就是这样救回两个提交的。
- 本机现在是部分克隆（`remote.origin.promisor=true`、`partialclonefilter=blob:none`）；对历史做全量 `rev-list --objects --all` 这类遍历会触发按需拉取而极慢，统计体积改用 `git count-objects -vH`。

## 教训

- `.gitignore` 里写 `.apk` 只匹配名为 `.apk` 的文件，`*.apk` 才匹配扩展名。移除跟踪后文件立刻会以未跟踪状态重新出现。
- 第三方插件二进制（Wwise 的 `.pdb` / `.dSYM` / `.so`）和构建产物（apk）一旦入库，即使后来删除也永久占用历史与克隆时间；这类文件应在首次提交前就写进忽略规则。
- 两台机器协作时，历史重写必须趁第二台机器首次克隆之前做，并且要确认对方已推送完全部工作。

## 相关文档

- `plan/two-character-parallel-production.md`：双角色并行制作契约，第 7 节记录本次重写的实测数据与对两台机器的后果。
