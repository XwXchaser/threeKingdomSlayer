# 视觉库（VisualLibrary）

存放**最新的实机截图 + 已登记的美术素材索引**，供 AI 按需取用，避免每次让模型满仓库翻图。

> 为什么建这个：`Library/Locus/Screenshots/` 是 Locus 的自动截图缓冲区，滚动淘汰且在 gitignore 里。
> 实测 2026-11-03：知识文档引用过的 **51 张实机截图已全部丢失**（全工程搜索无果），
> `plan/slash-weapon-afterimage-handoff.md` 等文档里的证据链接已指向空文件。
> 本目录是稳定副本，`shots/` 与 `index.csv` 都进 git。

## 目录结构

```
VisualLibrary/
  README.md        本文件
  capture_flow.md  固定抓取流程规范（七步 + 动作码词表 + 三种抓取模式 + 缺图决策树）
  index.csv        素材索引（唯一入口，机器可读）
  shot_defs.csv    镜头声明表：要拍什么、怎么拍、裁哪里
  shots/           实机截图（shots/<shot_id>.png，刷新时覆盖）
  bundles/         打包输出（不入 git）
```

## index.csv 字段

| 列 | 含义 |
|---|---|
| `id` | 稳定主键。美术素材 = `art_<路径slug>`；实机截图 = `shot_defs.csv` 里的 `shot_id` |
| `kind` | `art`（美术素材） / `shot`（实机截图） |
| `role` | `preview` 整图预览 / `reference` 参考 / `component` 零件件 / `sprite` 游戏内素材 / `source` 源件 / `ingame` 实机画面 / `void` 作废 / `archive` 归档 |
| `title` | 中文标题（人工可改，重跑索引不覆盖非空值） |
| `path` | 相对工程根的路径 |
| `tags` | 分号分隔标签，选图打分用 |
| `desc` | 自由说明 |
| `captured_at` | 入库时间（美术=文件 mtime，实机=拍摄时间） |
| `sha256` | 文件哈希，用于校验 |
| `width` / `height` | 像素尺寸 |
| `source_hash` | 被拍对象的哈希（仅实机截图）；源资产变了而没重拍 → `status=stale` |
| `git_commit` | 登记时的 commit |
| `status` | `active` 可用 / `fresh` 实机图且未过期 / `stale` 实机图已过期 / `missing` 文件已丢失 |

## 常用命令（工程根目录执行）

```bash
# 1) 刷新美术素材索引（新素材入库后跑）
python Tools/visual_brief/index_art_assets.py

# 2) 查库：只报告差异，不写文件
python Tools/visual_brief/index_art_assets.py --check

# 2b) 采集实机截图（Unity Editor 内调用，一条命令拍完）
#     VisualLibraryCapture.Start("battle_skill_icons_choice,battle_overall_scene", 1080, 1920)
#     结果 → Library/Locus/tmp/visual_library_captured.json
#     登记 → python Tools/visual_brief/claim_shots.py --import Library/Locus/tmp/visual_library_captured.json

# 3) 登记一张实机截图（复制进 shots/ 并写索引）
python Tools/visual_brief/claim_shots.py --src <png> --shot-id battle_hud_idle \
    --title "战斗 HUD 常态" --tags "combat;hud" --scene Assets/Scenes/Battle.scene

# 4) 按需求打包：选图 + 裁剪 + 缩放 + 生成 brief 骨架
python Tools/visual_brief/build_brief.py \
    --req Locus/knowledge/plan/<需求>.md \
    --bundle <bundle_id> \
    --max-images 6 --long-edge 1080

# 5) 只看选图结果，不写文件
python Tools/visual_brief/build_brief.py --req <需求>.md --bundle <bundle_id> --dry-run

# 6) 只贴一张图（多图合成一张带编号的拼图）
python Tools/visual_brief/build_brief.py --req <需求>.md --bundle <bundle_id> --mode montage
```

## 当前状态（2026-11-03）

| 部分 | 状态 |
|---|---|
| 美术素材索引 | ✅ 已建，901 条（897 素材 + 4 实机图） |
| 打包器（选图/裁剪/缩放/拼图/brief 骨架） | ✅ 可用 |
| `shot_defs.csv` 镜头声明 | ✅ 12 个镜头（4 个已拍出图） |
| 固定抓取流程规范 | ✅ `capture_flow.md`（七步 + 动作码词表 + 三种抓取模式 + 缺图决策树 + 实现状态） |
| **采集器（Play Mode 批处理）** | ✅ `Assets/Scripts/Editor/VisualLibrary/VisualLibraryCapture.cs` |
| 实机截图 `shots/` | ✅ 4 张（三选一技能图标 / 开局整体 / 天赋页 / 选关页） |
| 知识层入口 | ✅ `Locus/knowledge/memory/visual-library.md` |

## 美术工作怎么用（抓游戏素材/界面来备料）

### 三条入口

**A. 在对话里说一句话（最常用）**
> 「抓一下天赋页的实机画面」「把三选一弹窗抓下来」「抓开局整体画面」

我会自动走完：查索引 → 没有就在 `shot_defs.csv` 加一行 → 调采集器 → 登记索引 → 给你图片绝对路径。

**B. 直接调采集器（Unity Editor 内）**
```csharp
VisualLibraryCapture.Start("talent_page,battle_overall_scene", 1080, 1920)
```
- 自动切场景 → 进 Play Mode → 逐镜头摆场/定帧/抓图 → 登记 → 退 Play Mode
- 结果：`Library/Locus/tmp/visual_library_captured.json`（每条带 ok/failed 与失败原因）
- 登记索引：`python Tools/visual_brief/claim_shots.py --import Library/Locus/tmp/visual_library_captured.json`
- ⚠️ 目前**没有菜单入口**，只能由 Locus / Python SDK 调 `unity_execute` 触发。

**C. 手里已经有图（手工截的、美术给的），只是要入库**
```bash
python Tools/visual_brief/claim_shots.py --src <png> --shot-id <id> --title "标题" --tags "a;b"
```

### 新增一个镜头 = shot_defs.csv 加一行

```
shot_id,title,scene,camera,setup,capture,time,tags,crop,max_long_edge,status,notes
talent_page,天赋页总览,Assets/Scenes/TalentTree.scene,MainCamera,none,game,,talent;ui,,1080,planned,三分页 + 节点树现状
```
- `scene`：必须是工程里的场景路径
- `setup`：固定动作码，`+` 串联（见 `capture_flow.md` 词表）
- `crop`：归一化裁剪区，如 `"0,0,1,0.3"` 只取顶部 30%
- `status`：`planned` → `captured`；拍不了写 `unsupported` + notes 写原因

### 现在能抓 / 抓不了（实测结论）

| 能抓（已实测出图） | 说明 |
|---|---|
| `battle_overall_scene` | Battle 开局整体（含天空、HUD、开始关卡横幅） |
| `battle_skill_icons_choice` | 三选一弹窗（`upgrade_choice:active_cyclone\|active_arrow_rain\|active_wave`） |
| `talent_page` | 天赋页（TalentTree 场景，黑底=玩家实际所见） |
| `stage_select` | 选关页（StageSelect 场景；列表内容依赖运行时数据，静态帧可能是空的） |

可用动作码：`none` `clear` `freeze` `unfreeze` `spawn:<Enemy_1021>` `upgrade_choice:<id\|id\|id>`

| 抓不了 | 替代方案 |
|---|---|
| 招式动作关键帧、特效某一瞬（`frame` 模式未实现） | 我在 Play Mode 里用 Locus 的 `unity_capture_viewport` 手工抓一张；或先补动作码 |
| 连续帧条（`sequence` 模式未实现） | 同上 |
| `wave:` `combo:` `player_attack:` `effect:` `skill:` `qte:` `hit:` `select_node:` `hub` | 采集器会明确报"动作码未实现"并**拒绝产出半成品**；需先补动作码 |

### 采集后的收尾（每次都要做）

Unity 在实例化敌人 prefab 后会把它们的行尾重写一遍（LF vs CRLF），`git status` 里会多出一批 modified。
内容与 HEAD 逐字节相同，纯噪音，采集后还原即可：

```bash
git checkout -- Assets/Resources/EnemyPrefabs/
```

### 抓完怎么确认拿到东西了

```bash
# 看结果清单（每条带状态与原因）
cat Library/Locus/tmp/visual_library_captured.json

# 看库里有哪些实机图
grep ",shot," _art_process/VisualLibrary/index.csv

# 过期检测：源场景变了但没重拍 → status=stale
python Tools/visual_brief/index_art_assets.py --check
```

## 新增一个镜头

完整规范见 `capture_flow.md`，要点：

1. 在 `shot_defs.csv` 追加一行：
   `shot_id,title,scene,camera,setup,capture,time,tags,crop,max_long_edge,status,notes`
   - `setup`：固定动作码，`+` 串联。词表：`none` `hub` `talent_page` `stage_select` `clear`
     `spawn:<Enemy_1021>` `wave:<n>` `combo:<n>` `player_attack:<Stab|Slash|Pierce|Sweep|Launch|Parry>`
     `effect:<CycloneEffect>` `skill:<ActiveSkill_Cyclone>` `qte:<Sweep|Swipe|TripleStab>` `hit:<Enemy_1021>`
   - `capture`：`game`（即时状态）｜`frame`（`ClipName@0.42` 或 `ClipName#23`，动作关键帧/特效某一瞬间，
     用 `Animator.Play`+`Update(0)` 强制采样）｜`sequence`（`0.10-0.80` 区间等距采 N 帧 + 横排帧条）
   - `camera`：`MainCamera`｜`follow:<Target>`｜`closeup:<Target>@<dist>`
   - `crop`：归一化裁剪区 `左,上,右,下`，如 `"0,0,1,0.3"` 只取顶部 30%（像素量直接降 70%）
   - `status`：`planned` / `captured` / `unsupported`（拍不了，notes 写原因） / `deprecated`
2. 阶段 1 采集器按表批量拍摄 → 写入 `shots/<shot_id>.png` → 登记索引
3. 打包器会自动用 `shot_defs.csv` 里的 `crop` / `max_long_edge`

## 缺图了怎么办

见 `capture_flow.md` §7 的决策树。一句话版：
库里有 `role=ingame` 就用；只有离线素材就标明是素材并加镜头行补实机图；
工程里摆不出来就 `status=unsupported` + notes 写原因，并在 brief 里显式写"缺图：<原因>"。
**旧图默认不能顶替现状**（会让方案基于过时现状）。

## 给后续 AI 的使用约定

**查图前先查 `index.csv`，不要 `find` / `grep` 目录。**
需求文本 → `build_brief.py` 选图 → 按 `path` 取图 → 贴给用户或网页 GPT。
`bundles/` 里的 `brief.md` 只放**需求**，方案一律不出在这里。

> 注：`bundles/` 已在 `.gitignore` 里（打包产物是临时件）。

## 图的尺寸预算

Claude 视觉 token ≈ `宽×高÷750`（长边超 1568 会先缩放）。竖屏 1080×1920 原图约 1780 token/张。

| 长边 | 单张 token | 6 张 |
|---|---|---|
| 1920（原图） | ~1780 | ~10.7k |
| 1280 | ~1230 | ~7.4k |
| 1080（默认） | ~875 | ~5.2k |
| 720 | ~389 | ~2.3k |

所以：**优先裁剪（crop），其次缩放**；`--mode montage` 可把多张合成一张贴。
