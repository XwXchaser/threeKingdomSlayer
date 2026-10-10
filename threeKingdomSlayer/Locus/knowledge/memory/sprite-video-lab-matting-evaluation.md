---
id: kd_1182a721-ef19-46ed-8577-3defa28e7b7d
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
summary: 'Sprite Video Lab 抠图工作流实测与全量实装记录（最终版，2026-10-10 已验收）。首轮曾判 Chroma 不可用，经 v2→v5 迭代后成立并成为首选工作流：softness 0 消半透明、halo 固定 1、容差按底材标定（绿幕 100 / 灰白底 6 + 中性幕布清理）、相对判据描边环替换、源帧去绿、中性化。1011 的 69 个精灵已全量替换并通过 Unity 验证（10 clip / 72 key / 0 空引用），getup 用脚底锚点对齐 idle 脚线 896；含旧 GPT 路线结论与两次踩坑记录。'
---

## 结论（2026-10-09 首轮实测）

`sprite-video-lab`（SVL）的 **Chroma 抠图路线在本项目的绿幕素材上不可用**，且不是参数问题。已按计划做完部署 + 四条跑道 + 三方量化对比，结论已量化，不必重跑。

## 部署事实（复用即可）

- checkout：`E:\sprite-video-lab`（v0.2.0，HEAD `01603e8`）
- 运行时：`E:\sprite-video-lab-models\venv`（**Python 3.10**，Pillow 12.3.0；额外装了 `huggingface_hub` 仅为跑绿仓库自带单元测试）
- 工作目录：`E:\sprite-video-lab-work`（uploads/jobs/exports/previews）
- 启动：双击 `E:\sprite-video-lab\start_sprite_video_lab.bat`（自 detach + 开浏览器，127.0.0.1:8894）
- **盘符无关（2026-10-10 事故教训）**：上面这些 `E:\...` **只是机器 A 的选择，不是要求**。任意盘（**含只有 C 盘的主机**）都能装：把 checkout/venv/工作目录放哪儿都行，只要用 `SPRITE_VIDEO_LAB_PYTHON`（启动器用哪个解释器）/ `SPRITE_VIDEO_LAB_WORK_DIR` / `SPRITE_VIDEO_LAB_ROOT`（脚本用）三个环境变量指过去；可复制步骤见 `skill/workflows/svl-chroma-rim-fix.md` §1.4。
- **启动器陷阱**：`start_sprite_video_lab.bat` 只在**检测到 `E:\` 存在**时才用 `E:\` 的 venv/工作目录；没有 E 盘时它退到 PATH 里的 `python` —— 若那是 3.13+，服务会因 `cgi` 缺失而起不来。所以新主机**必须先设 `SPRITE_VIDEO_LAB_PYTHON`**。
- **解释器纪律**：SVL 只能用 3.10（`server.py` 顶层 `import cgi`，3.13+ 已移除）；numpy 度量脚本用系统 3.13。
- 由 Locus 会话启动的 server 会随工具调用结束被回收 → 批量跑要用"脚本自带 server 生命周期"的写法（见 `Library/Locus/tmp/svl_run/run_all.py`）。

## 量化结论（匹配帧子集中位数）

| 指标 | SVL(halo=1) | SVL(halo=0) | canonical 本地键控 | GPT(已验收) |
|---|---|---|---|---|
| 白底可见绿像素 | 2,140 | 5,577 | **0** | 181 |
| 其中 gEx>2 | 1,619 | 5,037 | **0** | 5.5 |
| 羽化剖面 外1–5px α | 0.1/0/0/0/0 | 0.1/0/0/0/0 | 0.9/0.8/0.6/0.5/0.3 | 1.0/0.9/0.8/0.7/0.6 |
| 主体可见像素 | 148,430 | 153,462 | 169,991 | 146,816 |

（攻击 16 帧与跑动 6 帧两套源结论同向；单条 97 帧跑道约 210 s。）

## 根因（两条实现取舍，调参无效）

1. `alpha_aware_despill_frame` 对 **α≥254 的像素逐字复制**；Chroma 的硬阈值恰好把边缘污染像素定成 255 → 绿边被"保护"下来。像素级取样证实 SVL 残留绿是 **α=255 的实心偏绿像素**（如 `(99,160,132) gEx=28`、`(10,73,33) gEx=40`），而 GPT 那 181 个是 α=254 近白像素的 ±1 噪声（`(247,248,247) gEx=1`）。
2. 缺 canonical 的三项（**噪声底 / enclosed-hole fill / 环颜色外扩**）；唯一能压绿边的 `halo_pixels` 腐蚀会同时削平羽化 → **"无绿边"与"保羽化"在该实现里互斥**。

## 已否掉的优化路线

- **P0 否掉**：`tools/apply_alpha_aware_despill.py` 作用在 canonical 攻击产物上（`--dry-run`）：幕布色被估成蓝色 `(92,39,243)`（canonical 已清零/替换透明区 RGB，取不到真实幕布绿），`edge_green_excess` 由 **0.0 → 2.48**，把干净产物弄脏。该工具只适用于"透明区仍保留幕布色"的素材。
- **P1 失去意义**：把 SVL 缺的三支柱补进 SVL ≈ 重写现有 canonical，没有理由引入 SVL 本体。
- **P2 仅存借鉴价值**：α 置信度加权的线性光反混合思路，目标是对付跑动源 `inner_band_gEx = −31`（描边偏绿，低于已验收参照 −54/−67）的真实短板；需自建脚本。

## 尚未证否、仍可能有用的部分

- SVL 的两条 **AI 路线**（CorridorKey 绿/蓝幕、BiRefNet）：真正的模型抠图，可能替代脆弱的 GPT 路线。CorridorKey 许可为 CC BY-NC-SA 4.0 + 附加条款（允许"作为商业项目的一部分处理图像"，禁止重打包/转售、禁止付费推理服务、嵌入商业软件需单独协议）。
- 与抠图质量解耦的能力：**方形底部对齐画布**（可直接替代手工 Y 偏移，对应 `memory/unity-project-understanding` 与 `design/visual-yoffset-system` 的痛点）、premultiplied-alpha 缩放、帧筛选/按选序、四种导出。注意 UI 未暴露画布布局，须走 `/api/process` 或改 `app.js`。

## 复现入口

- 方案与完整记录：`plan/sprite-video-lab-deployment.md`
- 驱动/度量脚本：`Library/Locus/tmp/svl_run/{svl_client.py,run_all.py,svl_metrics.py,svl_verify.py}`
- 数据：`Library/Locus/tmp/svl_run/comparison.json` 等；对照图 `Library/Locus/tmp/svl_run/preview/sheet_attack_f012.png`、`sheet_walk_f023.png`

## 运维坑（实测，会反复踩）

1. **UI 的「清空 WebApp 内部文件」会 rmtree `exports/jobs/uploads/previews`**（`MANAGED_RUNTIME_DIRS` 含 `EXPORTS_DIR`）——首轮 4 套 97 帧产物就是这样丢的。需要留存的产物必须写到项目内目标目录（如 `Enemy1011ArtSource/.../keyed_v2/`），不能只放在 `E:\sprite-video-lab-work\exports`。
2. **端口 8894 会重复监听**：实测同时存在 venv 版与系统 Python310 版两个 server，请求随机落点 → 界面表现为"素材未导入/步骤没解锁"。异常时先查重：`Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -like '*server.py*' }`。
3. **两个"导入"入口不要混**：第一段拖放区（多选 PNG，走 `/api/upload`）才会解锁抠图；第三段「导入帧序列/导入文件夹」走 `/api/import-animation`，代码里直接 `state.upload=null` 并隐藏 `processPanel`，只用于已抠好帧的检查/缩放/导出。拖"文件夹"到拖放区会被静默忽略（`dataTransfer.files` 为空）。

**已用修正配方重跑（2026-10-10，用户要求）**：输出 `cavalry_striking_video_v6/keyed_v5_fixed/`（18 帧 + `keyed_v5_fixed_report.json` + `preview/ab_f*.png` 对照图）。**5 项无条件门全过**（绿 0 / 半透明 0 / 毛刺 0 / 内侧未改 0 / 覆盖 ≤2%）；**颜色改动从每帧 11,104 px（5.4%）降到 548–1,529 px（0.2–0.7%）**。

gate4/5 仍不过，但**不应强过**：它们是 1011 的"窄描边"经验值，而该源外圈天生于深于/绿于内侧（深色主体 + 固有深轮廓，内侧亮度 66）。实测替代方案"局部判据 + 最外 1px"：rimΔlum −32.9→−30.6、rimΔcast +9.9→+9.6（几乎不动），而颜色改动 548→989（翻倍，2px 则 1394）→ 已把这条写进 skill 的 §3 并把 gate4/5 标为 **N/A**。

该次重跑**未导入 Unity**，未动任何 Unity 资产。

## 事故与修正（2026-10-10，骑乘素材颜色被刷掉）

另一个会话照 `skill/workflows/svl-chroma-rim-fix.md` 的旧版做了 `cavalry_striking_video_v6` 的 18 帧，结果**角色正常颜色被刷掉**。已复现并定位：

- 复现：按其报告的参数重跑，逐值一致（visible 204,213、实际颜色改动 15,406 px、meanΔRGB (21.1,8.8,11.7)）。
- **根因是第 2 步“描边修复”的判据被写成相对值**：`d_src > 内侧均值 + 6` 与 `亮度 < 内侧均值 − 25`。骑乘源内侧 `d=−49.1`、`亮度=66.2` → 阈值变 `d>−43.1`、`亮度<41`，**命中 81% 的外圈**（1011 是 61%），把正常暖色/棕色与马匹甲胄的正常暗部当成描边、用内侧色替换。
- 消融（cavalry f055，可见 204,213）：**旧配方 11,104 px 改动（5.4%）→ 不启用第 2 步仅 133 px（0.1%）→ 绝对 `d>4` 且关暗分支仅 527 px（0.3%）**。第 3 步（删源绿）只删 813 px，无问题。
- 另一个次要问题：v4/v5 把 v3 的**硬绿保证**去掉了，所以 gate1 严格不为 0（1011 实装帧仍有 gEx>0 的 1,134–1,598 px，但**最大只 4.0**，肉眼不可见）。

**已修文档（skill/workflows/svl-chroma-rim-fix.md）**：第 2 步改为**绝对判据 `d>4`、暗分支默认关闭（若启用只能与内侧 1px 邻域比）**、新增**开工前小样诊断（命中率须 ≤30%）**、新增**第 5 步硬绿保证收尾**、§3 新增**“任一门不过不许交付”的阻塞规则与消融流程**、§0 补上不适用条件、§5 写入本次反例。

## 平滑处理（Real-ESRGAN）已弃用 + 1011 全量复核（2026-10-10）

用户目检发现「先做平滑处理」会**重绘细节**（它是生成式重建：实测每帧约 8.5 万像素的颜色与源帧不同，≈可见像素的 28%）→ **弃用该工序**，已从工作流文档移除。已装好的 Real-ESRGAN 仍保留在 `E:\sprite-video-lab-work\tools\realesrgan-ncnn-vulkan\`（供其它用途），但标准流程不再使用。

同一轮用“T79 + 平滑”复核了 1011 全部 10 组 69 帧（产物 `Enemy1011ArtSource/03_SelectedFrames/<组>/keyed_ui_esr_t79/`）：
- **9 个绿幕组**：比旧 keyed_v5 **少删 1.1–1.9 个百分点**主体（Attack 12.4% vs 13.7%、Walk 26.1% vs 27.4%），半透明 0、绿残留≈ 0 → 新工作流一致更优。
- **Idle（灰底）例外**：自动取色取到灰 (44,43,48) + T=79 → **被删主体 66.6%**（角色被吃掉）。单独标定后取 **T=14**（可见 124k、绿 0、幕布残留 0），产物 `Idle/keyed_ui_esr_t14/`；灰底路线固有地比旧 GPT 版少约 20% 可见像素。
- 报告 `keyed_ui_esr_t79_report.json`，总览 `keyed_ui_esr_t79_overview.png`。

**工作流文档已重写**为“从零部署与使用手册”（`skill/workflows/svl-chroma-rim-fix.md`）：新主机照做即可跑通（前置/取码/venv/启动/自检/容差标定/批量脚本/验收/Unity 入库规格/踩坑）。

## 最终采用的工作流（2026-10-10 用户确认，取代前述配方）
**在网页版定容差，其余全默认**：Chroma + 自动取背景色 + softness 16 + halo 1 + **只勾「半透明像素转不透明」**；描边修复/删源绿/中性化/硬绿保证全部不用，softness **不得**改成 0。

- 骑兵素材容差：用户在网页预览定为 **70**。已出整套：`cavalry_striking_video_v6/keyed_v5_ui_t70/`（18 帧 + `keyed_v5_ui_report.json` + `preview/keyed_v5_ui_18frames_white.png`）。
- 实测（T=70，18 帧中位）：被删主体 **4.95%**（范围 4.18–5.25%）、半透明 **0**、封闭孔 ≤747（中位 244）、白底绿像素 gEx>0 ≈ 4,791 / **gEx>8 ≈ 1,924**（因比 T=80 少扣所以多留绿）。
- 为何优于我的旧配方：`softness=0` 在键控阶段就杀掉中间 alpha → 反混合（只在 0<α<254 生效）永不运行 → 那圈混色像素只能“删掉（切细部件/缺口）”或“留绿”；softness=16 则先反混合还原颜色、再由「半透明转不透明」硬化 → **半透明=0 且不丢形**。
- 同一源实测对比（f059/f075/f083 中位）：旧配方被删主体 **7.1–7.6%**、薄带占比 0.036、周长/面积 0.072；新工作流（T70）**4.2–5.3%**、0.026、0.052。
- 容差权衡：T=70 → 被删 4.2–5.3% / 绿 gEx>8 ≈ 1.3–2.3k；T=80 → 被删 5.4–5.7% / 绿更少。**若入库后发现淡绿可见，把容差回抬 5–10 重跑即可。**
- 对比素材（四个变体 + 对照图）：`cavalry_striking_video_v6/variant_compare/`；脚本 `svl_run/{cav_variant_compare.py,cav_run_B.py}`。

## 全量部署完成（2026-10-10，用户定调：今后优先此工作流）

用户要求 GPT 产物也一并替换，并确定**今后抠图工作流优先使用本路线**。已完成：

- **次轮 18 个**：idle 6 + walk 6 + getup 6，至此 `Assets/Sprites/Enemy/Enemy1_1/` 下 **69 个精灵全部换成本配方产物**。
- **Idle（灰底）专用配方**：T=6 + 中性幕布清理（低饱和且值接近幕布 ±14）→ 背景干净且保留更多暗部（不能靠大容差，幕布噪点上限 10.4 会逼到 T≈11）。
- **getup 锚点修正**：原部署的垂直布局需 222px 偏移（超出 1108 画布的 148px 上限）→ 改为裁剪到主体后按**脚底锚点**贴到 idle 脚线；实测 idle 与 getup 底边均为 **896**，完全对齐；X 沿用原部署位置。
- Unity 验证：69/69 精灵正常（1108×1108 / pivot (554,554) / ppu 16），10 个 Enemy_1011 clip / 72 个 sprite key / **0 空引用**；只改 PNG 字节、不动 `.meta` 与场景/prefab/clip。

备份：`Library/Locus/tmp/backup_1011_sprites_20261010-003156/`（首轮 57）与 `backup_1011_sprites_r2_20261010-004351/`（次轮 18）；脚本 `svl_run/deploy_v5_to_unity.py`、`deploy_v5_round2.py`。

**优先级变更**：`skill/workflows/svl-chroma-rim-fix.md` 已升为**首选**抠图流程；`skill/workflows/local-green-screen-cutout.md` 与 GPT 语义去背降为**备选/对照**（非纯色底或需语义判断时才用）。

## 部署到 Unity（2026-10-10，已执行）

用户验收 v5 后要求替换 1011 美术素材。已替换 **51 个精灵**（attack 16 / dead 6 / hit 5 / hitLeft 6 / hitRight 6 / landing 6 / rise 6），**只改 PNG 字节、不动 `.meta`** → GUID/pivot/PPU/导入设置不变；Unity 重导入后验证：69 个精灵全部 1108×1108 / pivot (554,554) / ppu 16；**10 个 Enemy_1011 clip、72 个 sprite key、0 空引用**（场景/prefab/clip 未动）。

放置沿用现有约定（960→1108 贴到 (74,120)），逐帧 bbox 偏差 +3~+5 / −2~−5 px（即被裁掉的 1-3px 描边/羽化），无跳位。实心像素大幅上升（attack 98,888 → 145,473）：旧的宽羽化（~64k 半透明）变成硬边，半透明归 0。

**三组故意未换（证据）**：
- **Idle / Walk 各 6 帧**：实测它们与 GPT 参考 **IoU = 1.000**（GPT 产物），本地路线替换会降级（walk 剪影差 ~9%；Idle 灰底最佳容差 T=6 仍少 ~18% 暗部内容）→ 需用户明确同意。
- **getup 6 帧**：现有部署需要的垂直偏移达 **222px**，超出 1108 画布可容纳范围（≤148）→ 说明该组源帧做过额外垂直裁切/重排；已按 md5 校验回滚，未动。

备份（57 个替换前 PNG + `deploy_report.json`）：`Library/Locus/tmp/backup_1011_sprites_20261010-003156/`；脚本 `svl_run/deploy_v5_to_unity.py`；`git status` 显示 51 个 PNG 已修改（`git checkout -- Assets/Sprites/Enemy/Enemy1_1/` 可全部回退）。

## 成功配方已固化为 Skill

→ `skill/workflows/svl-chroma-rim-fix.md`（工具路线：Chroma softness 0/halo 1 + 容差按底色标定 + 相对判据描边环替换 + 源帧 d_src 删绿 + 中性化；含七项验收与七类失败根因）。本文件的段落仅保留演进过程与实测数据。

## 全量交付（2026-10-09）

`Enemy1011ArtSource/03_SelectedFrames/` 下 **10 组动作共 69 帧**均已出 `keyed_v5/`（RGBA 960×960，命名 `<prefix>_f<NNN>_v5.png`）：Attack 16 / Dead 6 / HitFront 5 / HitLeft 6 / HitRight 6 / Idle 6 / Launch_Fall 6 / Launch_Getup 6 / Launch_Rise 6 / Walk 6。脚本 `svl_run/keyed_v5_all.py`；总览图 `preview/all_folders_keyed_v5_overview.png`；数据 `keyed_v5_all_folders.json`。

**容差必须按底色标定（新增关键教训）**：绿幕（plate std ≈ 1.5）T=12 与 T=100 只差 2%，用 100；**深灰底 Idle（std=1.1）T=100 → 可见 39k，T=20 → 116k**（参考 GPT 产物 155k），大容差键球会把暗部角色一起吃掉。

**遗留**：圈绿向偏置在 Dead/HitLeft/HitRight/Launch_*/Walk 仍为 +0.3~+3.1（Attack/HitFront 已达标 ≤ 0），需要更彻底时把环替换带宽从 3px 提到 5px（Attack 实测 −4.0/−23.3）；描边修复会改到每帧约 20% 可见像素，细部件（剑锋/飘带）必须目检。

## keyed_v5：深绿描边（生成端注入）的根治思路（2026-10-09）

**用户找到的根因**：视频生成时若要求绿底，生成器会**在角色轮廓上烙一层深绿色描边**；抠图时它既不是背景色、也不是角色真色，于是被当成角色保留。

**量化结果（16 帧，输出侧指标）**：这层边**不是绝对偏绿**（`d_src = g − max(r,b)` 中位 −37），而是**相对内侧**：

| 指标 | v4 | R1 环替换3px | R1b 环替换5px | R3 删环2px |
|---|---|---|---|---|
| 外 2px 圈亮度 − 内侧亮度 | **−14.0** | −8.5 | **−4.0** | −9.5 |
| 外 2px 圈绿向偏置 | **−14.8** | −22.8 | **−23.3** | −23.4 |
| 内侧（ero 5）绿向偏置 | −21.2 | −21.3 | −21.3 | −21.4 |
| 主体可见像素 | 146,007 | 145,473 (−0.37%) | 145,473 (−0.37%) | 137,689 (**−5.7%**) |
| 内侧像素改动 | – | 0 | 0 | 0 |

读法：v4 的外圈比内侧**暗 14 luma 且绿 6.4 个单位**（−14.8 对 −21.2）→ 这正是肉眼看成"深绿描边"的东西；绝对绿判据（d>0 / d>20）永远看不到它。

**有效修法（相对判据）**：外圈像素若 `d_src > 内侧均值 + 6` 或 `亮度 < 内侧均值 − 25` → 判为描边；**从内侧 1px 处取色替换**（保剪影）而不是删。实测 3px 替换把亮度差 −14→−8.5、绿偏 −14.8→**−22.8（已比内侧更不绿）**；5px 替换直接收敛到 −4.0 / −23.3（基本不可辨），且**覆盖不损失**。删环也能治，但要 −5.7% 剪影。

**交付**：`keyed_v5/` = Chroma t100/s0/h1 + 相对判据环替换 3px + 删绝对 d>8 + 中性化。脚本 `svl_run/rim_fix2.py`、`rim_output_metrics.py`；对照图 `preview/head_f051_v4_vs_v5b.png`、`head_f055_v4_vs_v5b.png`。

**风险**：5px 替换会把任何细于 5px 的部件（如飘带）压成单一颜色——需目检；3px 较安全。

## 源头建议（最高杠杆）

描边来自生成端，事后修补总会有代价。优先级：
1. **提示词层面**：明确要求"无深色描边 / 不要绿色轮廓线"，A/B 对比同一提示词的输出。
2. **take 择优自动化**：抽 3 帧算「外圈比内侧暗多少/绿多少」的分数，差 take 直接淘汰（本次脚本可直接改造）。
3. 换非绿色底 + BiRefNet 语义抠图（不吃颜色）——但白底抠图历史上更差，需实测。

## keyed_v4：修正 v3 的两处缺陷（2026-10-09）

**v3 的两个问题不是"绿像素=0 就没事了"**，我的 v3 量化口径太窄：

1. **v3 的规则 `g ≤ max(r,b)` 在本幕布色下会把残绿变成青绿**。幕布是 `(1,159,64)`——`b=64 ≫ r=1`，所以 clamp 落在 `g = b > r`，即产出一圈 **teal/cyan**。实测 v3 每帧有 **2,867 个像素** g 高于 (r+b)/2（肉眼读作浅绿/青绿边）。
2. **v3 的删除步骤空转**：clamp 已经把"绿优势"信号抹平（d≤0），所以再按 `d>24` 删除删不到任何像素（实测 0）。正确做法是**从源帧**读绿优势：源帧 `d_src = g − max(r,b)`，幕布 ≈ 98、角色 ≈ −46。v3 实测每帧残留 **572 个 `d_src>20` 的可见像素**，且它们**全部位于 2px 腐蚀不能存活的细结构上**（`burr==residue`）→ 这就是用户看到的"毛刺粘在角色身上"。

**keyed_v4 配方**（已交付 `keyed_v4/`，16 帧）：SVL Chroma `t100 / softness 0 / halo 1` → ① 删除可见且**源帧** `d_src > 20` 的像素 → ② 中性化：`g ← min(g, (r+b)/2)`。

| 配置 | 主体可见像素 | 毛刺(`d_src>20`) | 绿/青绿 cast |
|---|---|---|---|
| v3 (t80 + 旧 clamp) | 147,854 | 572 | 2,867 |
| t80 D40 + 中性化 | 147,807 | 520 | 0 |
| t80 D20 + 中性化 | 147,340 (−0.35%) | **0** | 0 |
| **t100 D20 + 中性化（交付）** | 145,883 (−1.33%) | **0** | 0 |
| t110 D20 + 中性化 | 144,522 (−2.25%) | **0** | 0 |

头带红色像素在所有变体都在 9.5k 量级（飘带完好；真正会伤飘带的是 halo=2，实测降到 9,007）。脚本：`Library/Locus/tmp/svl_run/{keyed_v4.py,keyed_v4_final.py}`；对照图 `preview/head_f051_v3_vs_v4.png`、`head_f053_v3_vs_v4.png`。

**尚未验证（我没有图像视觉）**：`d_src ≤ 20` 的灰绿残留、以及主观观感，仍需用户目检。

## keyed_v3：正确的优化方案（2026-10-09，后续被 v4 修正）

**用户更正：角色头顶飘带本体是红色，绿色是污染。** 这直接废掉了"加大 halo 腐蚀"的方案（腐蚀 2px 会把细飘带的红色像素削掉 −5.4%）。

**绿的深度分布（基线 t80/s16/h1 实测，从透明区向内逐px）**：63% 落在第 1px 环、16% 第 2px、7% 第 3px（86% 在 3px 内），但仍有 **4% 在体内更深处**（f051 191px / f053 212px）。→ 腐蚀只能治外圈且代价大；正确工具是颜色规则。

**配方（已交付 `keyed_v3/`，16 帧 RGBA 960×960）**：

1. SVL Chroma：`容差 80 / softness 0 / halo 1`（softness=0 是唯一能把半透明彻底归零的参数；halo 保持 1 以免削飘带）
2. 本地后处理 **绿保证**（= 现有 canonical 第 6 步）：可见像素强制 `g ≤ max(r,b)`。SVL 内核永远做不了这一步（despill 逐字复制 α≥254、`is_background_residue_pixel` 对 α≥255 返回 False）

**同 16 帧中位数**：

| 指标 | keyed_v3 | keyed_v2 | keyed (canonical) |
|---|---|---|---|
| 白底绿像素 | **0** | 1,817 | 0 |
| 半透明像素 | **0** | 1,572 | 73,432 |
| 主体可见像素 | 147,854 | 147,823 | 169,990 |
| inner_band_gEx | −38.25 | −40.25 | −34.85 |
| outline_ratio | 0.828 | 0.851 | 0.883 |

头部带红色像素（飘带存活）f051 5,871→5,891、f053 8,433→8,470 → 飘带完好。16 帧共修掉 50,735 个绿像素。

**代价/取舍**：硬边（完全没有羽化，canonical 是 73k 半透明像素的宽羽化）；`outline_ratio` 0.828 对 0.851（绿保证会把暗暖色描边的 g 抬到 max(r,b)，边缘环路偏亮）。若想保留羽化：`softness 16 + 绿保证` → 绿仍为 0、半透明 1,572。

**脚本**：`Library/Locus/tmp/svl_run/green_guarantee.py`（可复用 CLI，带 --dry-run/--backup-dir）、`run_keyed_v3.py`；对照图 `preview/head_ribbon_f051_v2_vs_v3.png`。

结论：**这两个问题不需要任何 AI 模型**，纯色键控 + 颜色保证就够了。

## keyed_v2 交付（2026-10-09）

`Enemy1011ArtSource/03_SelectedFrames/Attack/keyed_v2/`：16 帧 RGBA 960×960，命名同 `keyed/`（`attack_f{NNN}_final.png`）。源=`raw/`（与 `clean_raw` 逐字节相同），参数 = `chroma` + 自动取色 `#019F40` + 容差 80 / softness 16 / halo 1 + 源画布，无后处理。脚本 `Library/Locus/tmp/svl_run/run_keyed_v2.py`。同 16 帧中位数：白底绿像素 **1,817**（单帧 932–6,159）对 canonical **0**；羽化 0.14/0.02/0 对 0.88/0.81/0.63 → 可作对照版本，但按验收口径不达标，**不要直接进 Assets**。
