# Enemy 1014 投壶兵 · 美术素材包

> **需求源**：`Locus/knowledge/design/enemy-1014-art-brief.md`（唯一需求源）
> **生成方案与闸门**：`Locus/knowledge/plan/session-handoff-20261114.md` §7
> **当前状态（2026-11-16）**：★ **阶段 A v2 的 1014 形象已由用户验收通过**（造型方向成立：基层民兵 + 单手小水壶）；裁切件与脸谱对照表已入库；★ **待修项 = 脸部表情**（当前读成「义愤/老卒」，缺 1012 山贼的奸诈感，方案见需求书 §5.5 待拍板）。阶段 B（1014 单角色身份图）、阶段 C（陶壶独立道具图）**未开始**；**本包不含任何 Unity 资产改动**。
>
> ⚠ **v1 已否决**（造型方向错：重甲 + 双手抱大陶壶）；v2 按 `session-handoff-20261114.md` §7.8 重做，**造型方向已对，画风与背景仍未达标**（见下）。
> ⚠ **需求书口径冲突**：`design/enemy-1014-art-brief.md` §0/§7.1 写的是「本任务按人工制作执行，不再走生成接口」；本包 v1/v2 是生成接口产出的**比例研究图**，两者口径需用户拍板。

## 当前已入库内容

| 路径 | 内容 |
|---|---|
| `03_Lineup/enemy_proportion_lineup_1014_v1.png` | 阶段 A 六人并排比例组图 v1（1536×1024 RGBA，sha256 `8b246a2b…06dd`）——**待用户验收** |
| `03_Lineup/enemy_proportion_lineup_1014_v1.annotated.png` | v1 的量测标注图（六连通域框 / 头顶 / 脚底线） |
| `03_Lineup/enemy_proportion_lineup_1014_v1.measure.json` | v1 的量测原始数据（`Tools/visual_brief/measure_lineup_figures.py`） |
| `03_Lineup/enemy_proportion_lineup_1014_v1.prompt.txt` / `.prompt_cn.txt` / `.negative.txt` | 实际提交的提示词正文（中英）与负面词（**唯一真源在 `Tools/visual_brief/prompts/enemy_proportion_lineup_1014.*`**） |
| `03_Lineup/enemy_proportion_lineup_1014_v1.request.json` / `.status.json` | 请求留档（endpoint / 参数 / 五张 `image[]` 的 sha256 / usage / HTTP 结果） |
| `00_Index/lineup-images.csv` | 台账（variant / source_path / bytes / sha256 / note） |
| `03_Lineup/enemy_proportion_lineup_1014_v2.png` | 阶段 A 六人并排比例组图 **v2**（1536×1024 RGBA，sha256 `216e1f8d…0f65`）——**待用户验收** |
| `03_Lineup/enemy_proportion_lineup_1014_v2.annotated.png` | v2 的量测标注图（六连通域框 / 头顶 / 脚底线） |
| `03_Lineup/enemy_proportion_lineup_1014_v2.measure.json` | v2 的量测原始数据（`Tools/visual_brief/measure_lineup_figures.py`） |
| `03_Lineup/enemy_proportion_lineup_1014_v2.prompt.txt` / `.prompt_cn.txt` / `.negative.txt` | v2 实际提交的提示词正文（中英）与负面词（**唯一真源在 `Tools/visual_brief/prompts/enemy_proportion_lineup_1014_v2.*`**） |
| `03_Lineup/enemy_proportion_lineup_1014_v2.request.json` / `.response.json` / `.status.json` | v2 请求留档（endpoint / 参数 / 五张 `image[]` 的 sha256 / usage / HTTP 结果） |
| `02_IdentityImages/1014_lineup_crop_reference_rgba.png` | ★ **1014 单独裁切件**（214×364 RGBA，从 v2 组图按量测 bbox 裁出，+8 px 边距）——**当前的形象基准** |
| `02_IdentityImages/1014_face_reference_chart.png` | ★ **六个兵种脸谱对照表**（1011 兜鍪 / 1012 山贼 / 1013 旗手 / 1051 弓手 / 1021 楯兵 / 1014 新形象）——修脸时的参考 |
| `02_IdentityImages/1014_v2_base_greenscreen_1024.png` | 修脸底件（裁切件放大重贴：1024² 纯绿、主体高 696 / 脚底 895 / 中心 474.0） |
| `02_IdentityImages/1014_face_grin_v1.png` | ★ **新脸谱概念稿（方案 C：1012 式咧嘴奸诈笑）**——待用户验收 |
| `02_IdentityImages/1014_face_before_after.png` | 改脸前后对照图 |
| `02_IdentityImages/1014_face_grin_v1.*`（prompt / request / response / status） | 修脸这一轮的提交留档 |
| `02_IdentityImages/1014_face_shape_v2.png` | ★ **脸谱第二轮（收窄脸型 + 双眼正视前方）原始输出** |
| `02_IdentityImages/1014_face_shape_v2_greenscreen_1024.png` | ★ **同一张图的归一化版**（主体高 696 / 脚底 895 / 中心 474.0 / 绿幕纯净）——**当前最接近身份图口径的产物** |
| `02_IdentityImages/1014_face_three_way.png` | 三代对照（底件 → grin v1 → shape v2） |
| `02_IdentityImages/1014_face_shape_v2.*`（prompt / request / response / status） | 第二轮提交留档 |
| `03_Lineup/enemy_proportion_lineup_1014_v3.png` | ★ **阶段 A 组图 v3**（1536×1024，sha256 `31eaaa6d…aba6`）——**改回组图方案后的锁比例版本，待用户验收** |
| `03_Lineup/enemy_proportion_lineup_1014_v3.*`（measure / annotated / prompt / request / response / status） | v3 组图的量测与提交留档 |
| `02_IdentityImages/1014_v3_lineup_greenscreen_1024.png` | ★ **从 v3 组图裁出并归一化的 1014 身份底件**（主体高 696 / 脚底 895 / 中心 474.0，比例由家族锁死）——**当前的形象基准** |
| `00_Reference/idle_loop_reference_1012_1051_1021.png` | ★ **家族 idle 循环参考**（1012 六帧 / 1051 六帧 / 1021 四帧）——做 1014 idle 时照这个节奏 |
| `02_IdentityImages/1014_idle_toss_v1.png` / `1014_idle_toss_v1_greenscreen_1024.png` | ★ **idle 静帧概念稿 v1「轻抛小水壶把玩」**（原始 / 归一化落位：主体高 695 / 脚底 894 / 中心 474.0）——待用户验收 |
| `02_IdentityImages/1014_idle_pose_compare.png` | 军姿 vs 新 idle 对照图 |
| `02_IdentityImages/1014_idle_toss_v1.*`（prompt / request / response / status） | idle 概念稿的提交留档 |

> 组图**只是比例与画风研究参考**，**不可直接裁切为 Unity Sprite**（见 `memory/enemy-proportion-lineup-reference.md` 的裁切实验数据）。

## v1 的量化结果与已知偏差

**达标项**

- 恰好六人，顺序正确：1011 / 1012 / 1013 / 1051 / 1021 / 1014；六人脚底同基线（`foot_bottom_y` 全部 = 700）。
- 1014 身份读法正确：**露脸、闭嘴、无武器**，双手在胸腹前抱住素陶粗壶；壶的鼓腹与短流口可辨；无釉 / 无花纹 / 无金边 / 无绿光。
- 陶壶在**画面左侧**（躯干带质心 x=1379.9 < 头带质心 x=1395.5）⇒ 对应角色右手，与设定一致。

**未达标 / 偏差项**

| 项 | 实测 | 目标 |
|---|---|---|
| **渲染画风** | 六人整体是**平滑厚涂插画**（渐变、柔和高光），不是工程的成簇色块像素画 | 匹配 1011 已验收像素风 |
| **背景** | 透明底 + 人物周围暖色光晕/渐变（alpha 17–127 占 4.74%），**不是单一中性纯色** | 单一中性纯色背景 |
| **1014 身高比** | 0.912（1011=341 px，1014=311 px） | 0.94–0.99 |
| **1013 旗面** | 又自发长出**金色回纹边框**、旗色偏鲜红 | 无金边、偏暗赭红 |

- 画风与背景两项是**同一条根因**：本次五张 `image[]` 全部是工程内已部署的像素精灵帧，模型把它们当"姿态/形体参考"重绘成了平滑厚涂
  （与 `memory/enemy-proportion-lineup-reference.md`「教训八」同一现象）。
- 身高比 0.912 落在本图**实际家族带**（0.912–0.962）内，只是整体略低于需求书的 0.94–0.99。

## v2（2026-11-16，已出图）的量化结果与已知偏差

**v2 相对 v1 的两处改动**：① `image[]` 从「已部署精灵帧」换成**像素风锚图**（1011 绿幕锚图 + 1012/1013/1051/1021 锚图件，即 v4 组图那套）；② 1014 按 §7.8 改成**与 1012 同源的基层民兵 + 小型单手水壶**。

**达标项**

- 恰好六人、顺序正确，脚底同基线（`foot_bottom_y` = 718–719）。
- **1014 造型方向已对**：露脸、闭嘴、压眉；灰米交领粗布衣 + 暗棕皮革肩带/腰带 + 小布包 + 暗棕裤 + 灰白绑腿 + 旧布鞋；**无甲片、无兜鍪、无武器**；水壶**在画面左＝人物右手**，单手可握，不遮胸腹。裁切放大复核：`Library/Locus/tmp/1014lineup/v2_1014_zoom.png`。
- 1011 / 1012 / 1051 / 1021 的身份与装备保持稳定；1021 的盾**仍在画面左**（人物右手），未被镜像。

**未达标 / 偏差项**

| 项 | v2 实测 | 目标 |
|---|---|---|
| **渲染画风** | 六人整体**仍是平滑厚涂插画**（渐变、柔和高光）——**换像素风锚图作 `image[]` 也没有解决** | 匹配 1011 已验收像素风 |
| **背景** | 透明底（alpha=0 占 72.60%）+ 人物周围**暖色光晕/渐变**（alpha 1–127 占 5.54%），不是单一中性纯色 | 单一中性纯色背景 |
| **1014 身高比** | **0.920**（1011=376 px，1014=346 px） | 0.94–0.99（落在本图实际家族带 0.920–0.963 内） |
| **1013 旗面** | 再次自发长出**金色回纹边框**（跨轮复现的已知偏差） | 无金边、偏暗赭红 |
| **水壶尺寸** | 壶高约为人物本体的 **1/5**，比设定（1/8–1/10）偏大 | 一只手可握、不遮胸腹 |

> 画风与背景这两项**已换过一轮输入仍复现** ⇒ 判定为该出图链路的**结构性表现**（整排全身人物一律被渲染成插画），不是输入选错。继续加负面词不会解决；按需求书口径，**1014 的正式形象应走人工制作**，本图只作比例与设计研究参考。

## 下一步

1. **用户验收 v2**：① 1014 的民兵造型与小水壶方向是否可用；② 画风/背景未达标是否接受「这张只当研究图」；③ **需求书「人工制作」与本包生成图的口径冲突**需要拍板。
2. 若 v2 造型方向通过 ⇒ 把它连同本页的比例数据（1014 本体高 0.920×1011 ⇒ 折算 1108² 约 **628–661 px**，取家族带中值 ≈ **645 px**）一起交给人工制作方。
3. 阶段 B（1014 单角色身份图，1024² 纯绿幕 `RGB(0,159,62)`、主体高 696、脚底 895、中心 474.5）与阶段 C（陶壶道具图）在**人工制作**路线下是制作方的交付物；不再由生成接口出。

### 2026-11-16 后续：形象已验收 → 裁切件已出 → 脸谱修正项待拍板

- **验收结论**：v2 的 1014 造型（基层民兵 + 单手小水壶）**通过**。
- **裁切件**：`02_IdentityImages/1014_lineup_crop_reference_rgba.png`（214×364 RGBA；按量测 bbox `[1295,370,1493,718]` 裁出、四周 +8 px）。
- **脸谱对照表**：`02_IdentityImages/1014_face_reference_chart.png`（六个兵种的头部并排）。
- **★ 待修项（需求书 §5.5）**：1014 当前脸 = 压眉 + 闭嘴 + 方脸 ⇒ 读成「义愤/风霜老卒」，**与 1021 撞档**，缺 1012 山贼的**奸诈感**。
  - 技术结论：**奸诈感的技术核心是「不对称」**（一边嘴角上挑 + 视线略斜 + 眉内端下压），不必露齿。
  - 候选：**A 闭嘴单侧挑唇（推荐）** / B 阴鸷眯缝斜视 / C 1012 式咧嘴（与 1012 撞脸，不推荐）。
- **执行路线二选一（待用户拍板）**：① **人工制作**（需求书 §0 口径）——把脸谱 + 对照表 + 裁切件交给制作方；② **生成接口出概念稿**——单图编辑路线，底件 = 裁切件放大重贴成 1024² 绿幕底件，提示词只写「改脸、其余不动」。

### 2026-11-16 再续：新脸谱（方案 C）概念稿已出

用户拍板：**脸 = 方案 C（1012 式“笑着的凶”·咧嘴露齿）**、**路线 = 生成接口出概念稿**。

- 产出：`02_IdentityImages/1014_face_grin_v1.png`（+ `1014_face_before_after.png` 对照图）。
- 参数：gpt-image-2.6-sunburst / edits / **单张 image[]** / 1024×1024 / quality=max / input_fidelity=high / png / n=1；HTTP 200，59.13s，sha256 `6518306d…6505`。
- **保真度**：剪影一致率 **IoU = 0.9867**，落位不变（主体高 696 / 脚底 895 / 中心 474→475）⇒ “只改脸、其余不动”成立。
- **⚠ 附带偏差**：模型顺手加了**一圈黑色胡茬**（底件没有）⇒ 与 1013/1021 有混淆风险；**待拍板**：保留 / 再编辑去掉 / 改浅灰短茬。

### 2026-11-16 再再续：脸型 + 视线第二轮（shape v2）

用户反馈：**脸型没变**、**没正视前方** ⇒ 只改这两处（保留咧嘴笑与胡茬）。

- 产出：`02_IdentityImages/1014_face_shape_v2.png`（原始）+ `1014_face_shape_v2_greenscreen_1024.png`（**归一化版**）+ `1014_face_three_way.png`（三代对照）。
- 过程：首次提交 **HTTP 503 `upstream_unavailable`**（上游抖动，非内容拦截）→ 75s 后重试 → HTTP 200，201.75s，sha256 `44feb7f8…2c90`。
- **接口原图不能直接当身份图**：脚底被画到 884（口径 895）、整个人重渲染（剪影 IoU 0.9867 → **0.9001**）⇒ 新工具 `Tools/visual_brief/normalize_greenscreen_layout.py` 做本地归一化 ⇒ **主体高 696 / 脚底 895 / 中心 474.0 / 绿幕纯净**。
- **达标**：脸型收窄（颊内收、颧骨成棱、下颌略尖、额头与鼻梁收窄）；**双眼正视前方**；咧嘴笑与胡茬保留；身体/衣服/水壶未变。

### 2026-11-16 再再再续：改回组图方案（v3）

用户反馈：**比例不对了**（量化：肩线从体高 15.1% 被拖到 30.5%、头高占比 0.095→0.105）⇒ 单图编辑不能用来“修比例”，改回六人并排组图。

- 产出：`03_Lineup/enemy_proportion_lineup_1014_v3.*` + `02_IdentityImages/1014_v3_lineup_greenscreen_1024.png`（从组图裁出并归一化的身份底件）。
- 方案：前五张锚图不变 + **第 6 张 = 已验收的 1014 身份底件**（锁造型与脸）；正文追加「第六位身材 = 前五位身材，不许瘦长溜肩」与「六人都直视玩家」。
- 过程：两次 **503 `upstream_unavailable`**（上游抖动）⇒ 按经验减载荷（参考图长边降到 640，2.46 MB → 0.34 MB）⇒ 一次成功：HTTP 200，289.78s，sha256 `31eaaa6d…aba6`。
- **量测**：恰好 6 人、同基线 y=744–745；**1014/1011 = 0.936**（v2 为 0.912）；**背景首次是平匀中性灰 (113,114,115)**（v1/v2 是透明底+暖光晕）。

### 2026-11-16 收尾：v3 已验收，idle 姿态成为唯一未决项

- **用户验收**：v3 组图通过（「可以，但是需要修 idle 姿态」）⇒ 形象 / 脸 / 比例**锁定**。
- **下一项任务书**：`Locus/knowledge/plan/session-handoff-20261114.md` **§7.14**（已定项 / 三种读法 / 工程实测约束 / 可用资产 / 交付物 / 坑 / 待拍板）——**用户交由另一个 AI 出方案**。
- 新增参考资产：`00_Reference/idle_loop_reference_1012_1051_1021.png`（家族 idle 逐帧对照）。

## 相关文档

- `Locus/knowledge/design/enemy-1014-art-brief.md`（需求书）
- `Locus/knowledge/design/enemy-1014-javelin-thrower-mechanics.md`（机制）
- `Locus/knowledge/plan/session-handoff-20261114.md` §7（生成方案与闸门）
- `Locus/knowledge/memory/enemy-proportion-lineup-reference.md`（组图方法与历史教训）
- `Locus/knowledge/memory/art-production-workflow-entry.md`（S0–S5 阶段闸门）
- 提交脚本：`Tools/visual_brief/submit_enemy_proportion_lineup_1014.py`（v1）/ `Tools/visual_brief/submit_enemy_proportion_lineup_1014_v2.py`（v2）
