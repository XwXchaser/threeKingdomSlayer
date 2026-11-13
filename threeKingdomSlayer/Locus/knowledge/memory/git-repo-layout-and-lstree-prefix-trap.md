---
id: kd_862ab4b3-f7fb-4cff-8cc0-1261963c576f
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 本机 git 仓库布局 + `ls-tree` 前缀过滤陷阱（2026-11-13 踩坑记录）

## 布局事实

- **git 仓库根目录 = `C:\threeKingdomSlayer`（Unity 工程的父目录）**，不是工程目录本身。
- Unity 工程在仓库里的路径前缀是 **`threeKingdomSlayer/`**
  （例：`threeKingdomSlayer/Assets/...`、`threeKingdomSlayer/Locus/knowledge/...`）。
- 仓库根目录还散落着一些不入工程的东西：`Locus/`、`plans/`、`_summaries/`、`gen_report.py`、
  `temp_tree.txt`、`一夫当关.txt` 等（历史遗留，多数未跟踪）。
- 远端 `origin` = `git@github.com:XwXchaser/threeKingdomSlayer.git`，**blob:none 部分克隆**
  ⇒ 第一次读某个历史 blob 会去远端拉，**`git diff --stat` 这类要 blob 的命令可能卡到超时**
  （实测 `git diff --stat HEAD origin/...` 120s 不够）。只比路径的话用 `--name-only --no-renames`。
- 常规分支：`feat/stage-select-talent-overlays`（主线）、`dev`、`main`、`test`、
  `art/1012-v11-resources`、`experiment/curved-scroll-travel` 等。

## ★ 陷阱：`git ls-tree` 的输出不是「树根」

在工程目录（= 仓库子目录）里执行 `git ls-tree --name-only HEAD`，git 会**把当前目录当成隐式前缀过滤器**，
于是输出的是 `threeKingdomSlayer/` 子树的内容，看起来像「仓库根就是工程目录」——
和 `git cat-file -p HEAD^{tree}`（真实树根，含 `threeKingdomSlayer` 这一项）**对不上**。
同理 `git ls-tree --name-only origin/xxx:threeKingdomSlayer` 会返回空（路径已经在前缀里了）。

⇒ 判断真实树结构一律用：

```bash
git cat-file -p HEAD^{tree}          # 真实树根条目
git ls-tree --name-only $(git rev-parse HEAD^{tree}) --full-tree   # 不受 cwd 前缀影响
```

同理 `git diff` 的路径是**相对仓库根**的（`threeKingdomSlayer/...`），而 `git status --short`
在子目录里是**相对当前目录**的 ⇒ 两边做交集比较时要统一口径，否则会误判「无重叠」。

## ★★ 同类陷阱：`.meta` 里的 GUID 与 Unity 实际用的 GUID 不是同一个字符串

- 本仓库**大多数脚本的 `.meta` 里 `guid:` 是 64 位 base64 串**（如
  `Assets/Scripts/Core/CameraFeedbackController.cs.meta` → `Wy8Wsn6kB3hXFHLy73RocikpeEnjj5NfZt4nUFFcYTz5JgjADhANO4c=`），
  但 **Unity 的 AssetDatabase 与所有场景/预制体 YAML 用的都是 32 位小写 hex**
  （同一脚本在 Unity 里是 `de97d9d3a33207d4097eb36e9d1b0bd6`；场景里 `m_Script: {fileID:11500000, guid:de97d9d3a33207d4097eb36e9d1b0bd6}`）。
  ⇒ **拿 `.meta` 里的字符串去 grep 场景/预制体，一定搜不到 ⇒ 会得出「组件没落盘/是死代码」的错误结论**
  （2026-11-13 就因此误判 `CameraFeedbackController` 没挂进 Battle.scene，实际它是挂着的且 `m_Enabled: 1`）。
- ★ 正确查法：
  ```bash
  # ① 取 Unity 认的 GUID（hex）
  #    Editor: UnityEditor.AssetDatabase.AssetPathToGUID("Assets/Scripts/xxx.cs")
  # ② 再用这个 hex 去 grep 场景/预制体
  grep -n "<hex32>" Assets/Scenes/Battle.scene
  ```
  或直接用 `unity_execute` 读活场景（`FindObjectsOfType<T>(true)` + 序列化字段）—— 活场景是权威。
- 顺带：**场景 YAML 里搜组件类名是没用的**，MonoBehaviour 一律按 script GUID 引用。
