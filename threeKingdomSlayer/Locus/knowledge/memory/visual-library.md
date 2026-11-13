---
id: kd_979830a1-2d29-49c9-9edd-be85390e30a4
summary: 视觉库入口：实机截图与美术素材的统一索引（_art_process/VisualLibrary/，899 条：897 素材 + 2 实机图）。要图先查 index.csv，不要满仓库翻图；采集器已实现（VisualLibraryCapture.Start，一条命令拍完镜头）；缺图按决策树走固定抓取流程（capture_flow.md，七步 + 固定动作码词表 + game/frame/sequence 三种抓取模式）；选图规则：需求问"实机/现状"时实机截图硬优先；硬规则：不许拿离线稿或旧图冒充实机现状，缺图要写缺图。
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 视觉库（实机截图 + 美术素材索引）

> **一句话**：要"实机画面/截图"或"项目里已有的美术素材"时，先查这个库，不要满仓库翻图。
> 详细规范在 `_art_process/VisualLibrary/`（README.md + capture_flow.md），本文只当路由。

## 0. 为什么有这个库（2026-11-03 实测）

`Library/Locus/Screenshots/` 是 Locus 的自动截图缓冲区：滚动淘汰 + 在 `.gitignore` 里。
实测：知识文档引用过的 **51 张实机截图已全部永久丢失**（全工程搜索无果），
`plan/slash-weapon-afterimage-handoff.md` 等文档里的证据链接现在指向空文件。
所以视觉库自建目录并进 git：`shots/` 与 `index.csv` 不再随缓存蒸发。

## 1. 库在哪

```
_art_process/VisualLibrary/
  index.csv          素材索引（唯一入口，897 条）
  shot_defs.csv      镜头声明表（要拍什么）
  capture_flow.md    固定抓取流程规范（要拍新图看这里）
  shots/             实机截图
  bundles/           打包产物（gitignore）
```

脚本在 `Tools/visual_brief/`：`index_art_assets.py`（索引素材）、`build_brief.py`（选图打包）、`claim_shots.py`（登记实机图）。

## 2. 取图决策树（**先查库**）

```
要"实机画面"
 ├─ index.csv 里 role=ingame → 按 path 取图，核对 status=fresh
 ├─ index.csv 里 role=preview/component（离线素材，非实机）
 │    → 可当设计稿用，但必须标明是素材不是实机；同时加镜头行补实机图
 └─ 没有 → §4 缺图处理

要"已有美术素材"（贴图/图标/特效序列）→ index.csv 里 role=sprite/component/source，按 path 取图
```

查法：`grep` `index.csv` 的 `title`/`tags`/`path` 列；或自动选图打包
`python Tools/visual_brief/build_brief.py --req <需求.md> --bundle <bundle_id>`

## 3. 固定抓取流程（拍新图时）

规范全文：`_art_process/VisualLibrary/capture_flow.md`。要点：

- **一个镜头 = `shot_defs.csv` 里的一行**，不许口头临时拍（行含 scene/camera/setup/capture/time/tags/crop/status）
- 七步：解析声明行 → 加载场景并进 Play Mode → 按 `setup` 动作码摆场 → 锁死相机 →
  按 `capture`/`time` 定采样时刻 → 抓图 → 登记索引 + 纯色校验（纯色图判失败丢弃）
- **动作码词表（固定，不得自造）**：`none` `hub` `talent_page` `stage_select` `clear`
  `spawn:<Enemy_1021>` `wave:<n>` `combo:<n>` `player_attack:<Stab|Slash|Pierce|Sweep|Launch|Parry>`
  `effect:<CycloneEffect>` `skill:<ActiveSkill_Cyclone>` `qte:<Sweep|Swipe|TripleStab>` `hit:<Enemy_1021>`
  `select_node:<talent_ice_thaw>`，用 `+` 串联
- **相机预设**：`MainCamera` / `follow:<Target>` / `closeup:<Target>@<dist>`
- **三种抓取模式**：`game`（即时状态）｜`frame`（`ClipName@0.42` 或 `ClipName#23`，**动作关键帧/特效某一瞬间**，
  用 `Animator.Play`+`Update(0)` 强制采样，不靠等帧碰运气）｜`sequence`（`0.10-0.80` 区间等距采 N 帧 + 横排帧条）
- 常见特殊需求：特效实机表现 = `clear+effect:<Prefab>` + `capture=frame`；招式关键帧 = `player_attack:<Type>` + `capture=frame`；
  命中瞬间 = `spawn+player_attack+hit`；只要 HUD 局部 = 用 `crop` 拍摄时裁

## 4. 缺图处理（页面/镜头不存在时）

```
库里没有要的实机画面
 ├─ 语义相近的条目有吗？
 │    ├─ role=ingame → 直接用
 │    ├─ role=preview/component（素材非实机）→ 用但标明类别；并加镜头行补实机图
 │    └─ 没有 → 下面
 ├─ 工程里能摆出来吗？
 │    ├─ 能 → shot_defs.csv 加一行 → 跑采集 → 登记 → 用
 │    └─ 不能（场景不存在/表现未实现/无法稳定复现）
 │          → status=unsupported + notes 写原因 → brief 里显式写"缺图：<原因>"，列入未决问题
 └─ 旧图能顶替吗？→ 默认不能。旧图标 role=legacy 并注明拍摄时间，
        不得当作"现状"喂给方案模型（会让方案基于过时现状）
```

## 5. 硬规则

1. 每张图必须可追溯：`index.csv` 有行、有 `path`、有 `captured_at`、有 `sha256`。
2. **不许拿离线稿或旧图冒充实机现状**；给方案的需求里每张图都要标类别（实机/素材/参考/旧版）。
3. **缺图就写缺图**，不许沉默省略——出方案的一方必须知道哪里信息不足。
4. `status=stale`（源资产变了没重拍）要么重拍，要么标"可能已过期"。
5. 采集失败不产出半成品，报失败原因。

## 6. 与"需求整合 → 网页 GPT 出方案"的衔接

`build_brief.py` 把需求文档 + 选中的图打成一个包：`brief.md`（**只放需求不出方案**）、`images/`（裁剪缩放后，编号即粘贴顺序）、
`contact_sheet.png`（多图合一，只贴一张时用）、`manifest.json`（选了哪些、为什么选）。

图进模型前先缩：Claude 视觉 token ≈ 宽×高÷750。竖屏 1080×1920 原图 ~1780 token/张，长边 1080 约 875，长边 720 约 389。
**优先裁剪，其次缩放。**

**选图规则：实机优先。** 需求里出现「实机/现状/游戏里/真实显示…」这类词时，
`role=ingame` 的实机截图 +30 并压倒离线稿（离线稿 -8）；否则整图/参考优先于零件。
这条对应硬规则第 2 条——**不许拿离线稿冒充实机现状**，不靠人盯，靠打分器强制。

## 7. 当前状态与缺口

| 部分 | 状态 |
|---|---|
| 美术素材索引 | ✅ 899 条（`_art_process` 105 + `Assets/Sprites` 792 + 2 张实机图） |
| 选图打包器 / 镜头声明表 | ✅ 可用；12 个镜头（10 个 `planned` + 2 个已拍） |
| 固定抓取流程规范 | ✅ `capture_flow.md`（含实现状态与已踩坑记录） |
| **采集器（Play Mode 批处理）** | ✅ **已实现**：`Assets/Scripts/Editor/VisualLibrary/VisualLibraryCapture.cs` |
| 实机截图 `shots/` | ✅ 2 张：`battle_skill_icons_choice`（三选一技能图标）、`battle_overall_scene`（开局整体） |

**被要求"抓一下 XX 的实机画面"时怎么办**：直接走采集器，不用翻目录——
`VisualLibraryCapture.Start("shot_a,shot_b", 1080, 1920)`（Unity Editor 内调用，
或由 Locus 经 `unity_execute` 触发；目前无菜单入口）。没有对应镜头就在 `shot_defs.csv` 加一行再拍。
已实测能出图的：`battle_overall_scene` / `battle_skill_icons_choice` / `talent_page` / `stage_select`。
可用动作码：`none` `clear` `freeze` `unfreeze` `spawn:<Enemy_1021>` `upgrade_choice:<id|id|id>`。
**抓不了**：`frame`（招式关键帧、特效某一瞬）与 `sequence`（帧条）模式未实现；
`wave:` `combo:` `player_attack:` `effect:` `skill:` `qte:` `hit:` `select_node:` 等动作码未实现
（会明确报错并拒绝产出半成品）。这类需求改用 Play Mode 下 `unity_capture_viewport` 手工抓。

**采集器怎么用**：Unity Editor 内调用 `VisualLibraryCapture.Start("shot_a,shot_b", 1080, 1920)`；
一次调用拍完所列镜头，自动切场景、进 Play Mode、抓完自动退 Play Mode。结果清单在
`Library/Locus/tmp/visual_library_captured.json`，再用
`python Tools/visual_brief/claim_shots.py --import <该文件>` 写进 `index.csv`。

已实现的动作码：`none` `clear` `freeze` `unfreeze` `spawn:<Enemy_1021>` `upgrade_choice:<id|id|id>`；
其余（`wave:` `player_attack:` `effect:` `qte:` `hit:` `select_node:` 等）遇到会报"动作码未实现"并拒绝产出半成品。

⚠️ 现阶段实机图只有 2 张，其余镜头仍是 `planned`。被问到"现在游戏里长什么样"时，
先查 `index.csv` 里 `role=ingame` 的条目；没有就按 §4 走缺图流程，不要拿离线素材冒充实机。
