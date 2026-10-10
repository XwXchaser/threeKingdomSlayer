---
id: kd_87e60f85-f6fd-4a10-b5cc-eee0b4fd274f
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
summary: 'Sprite Video Lab 部署与迭代全记录。最终结论（2026-10-10 用户验收）：Chroma + 描边修复配方成立并成为项目首选抠图工作流，1011 的 69 个精灵已全量实装（含额外把 GPT 产物换掉）。本文件前部的“Chroma 不可用”属 v2 阶段中间结论，已被 v5 推翻并保留作演进记录。含部署命令、运维坑（清空按钮会删 exports、端口 8894 重复监听、灰底需小容差、getup 变位需脚底锚点）与两轮备份位置。'
---

## 目标与边界（2026-10-09 制定）

用 `sparklecatta-lang/sprite-video-lab`（下称 SVL）自带抠图流水线，对现有绿幕源帧独立跑一遍，产出能与现有 canonical 脚本直接对比的产物；用现有 `rim_metrics` 口径量化对比后，再决定"搭配优化"走哪条路。

边界：
- 不改 `Assets/**`；不改 `Library/Locus/tmp/matting_green_screen_v2_CANONICAL.py`；不改既有工作目录里的任何脚本或产物；不向项目 git 提交任何内容。
- SVL checkout、Python 运行时、模型缓存、工作目录全部落在项目外（E:）。

> **跨机器注意（2026-10-10 追加）**：本文件是**机器 A 的部署记录**，里面的 `E:\...` 路径只是那台机器的选择，**没有一处是必须的**。要在另一台机器（例如只有 C 盘）装：照 `skill/workflows/svl-chroma-rim-fix.md` 第 1 章做 —— 路径任意，用 `SPRITE_VIDEO_LAB_PYTHON` / `SPRITE_VIDEO_LAB_WORK_DIR` / `SPRITE_VIDEO_LAB_ROOT` 指向你的位置即可。
- 本轮不需要 Unity 编译、不需要 Play Mode、不需要编辑器连接。

## 预检结论（本机实测）

| 项 | 实测 | 影响 |
|---|---|---|
| Python 3.10 + pip 23.0.1（`py -3.10`） | 可用，`py_compile server.py` 通过 | SVL 运行时基础 |
| Python 3.13.12（PATH 默认 `python`） | **缺 `cgi`**（3.13 已移除），但带 numpy 2.5.3 + Pillow 12.3.0 | 不能跑 SVL 服务；用作**对比指标脚本**的解释器 |
| Python 3.14.4（`py -3`） | 缺 `cgi` | 同上，且是 `setup_ai_runtime.bat` 的默认建库版本（陷阱，见 §2） |
| PyPI | HTTP 200 | 可安装依赖 |
| 端口 8894 | 空闲 | SVL 默认端口可直接用 |
| E: | 377G 可用、可写 | 部署根；正好命中仓库脚本的 E: 默认 |
| H: | 58G 可用（97% 占用） | 不放 venv / 模型 / 工作目录 |
| GPU | RTX 4070 12G CUDA | 第二阶段 AI 路线可用 |
| ffmpeg / ffprobe | **缺失**（PATH、项目内、`I:\FF\...` 回退路径均无） | 视频输入 / 透明 MOV / GIF 不可用；**帧序列输入 + Frames 导出不受影响** |

## 部署形态与路径

```
E:\sprite-video-lab\                            SVL checkout（浅克隆 main）
E:\sprite-video-lab-models\venv\                Python 3.10 运行时（基础 + 未来 AI 依赖共用）
E:\sprite-video-lab-models\huggingface\         BiRefNet 模型缓存（第二阶段）
E:\sprite-video-lab-models\EZ-CorridorKey\      第二阶段 CorridorKey 源码与 checkpoint
E:\sprite-video-lab-work\                       uploads / jobs / exports / previews
```

理由：
- 仓库两个启动脚本在检测到 `E:\` 存在时会**自动**采用 `E:\sprite-video-lab-work` 与 `E:\sprite-video-lab-models\venv`；按上面布局放好后，**两个 .bat 都不需要任何环境变量**。
- venv 放在 `...\models\venv` 而非 checkout 内，是为了让第二阶段的 `setup_ai_runtime.bat` 直接复用同一个运行时。
- 与 Unity 项目零耦合：不写 `Assets`、不进 `Library`（避免被 Unity 清理）、不进 git。

**陷阱（顺序不能错）**：`setup_ai_runtime.bat` 在 venv 不存在时会执行 `py -3 -m venv`，而本机 `py -3` 解析到 **3.14** → 造出跑不了 `server.py` 的运行时。必须**先用 `py -3.10` 手动创建 venv**，让脚本走"已存在则跳过"分支。`start_sprite_video_lab.bat` 的查找顺序是 `SPRITE_VIDEO_LAB_PYTHON` → `E:\sprite-video-lab-models\venv\Scripts\python.exe` → PATH 的 `python`（=3.13，坏），所以 venv 建在上述位置即可自动命中。

## 首轮试跑素材（现有，不需重新生成）

| 用例 | 源（960×960 RGB 绿幕，97 帧） | 已有参照产物 | 用途 |
|---|---|---|---|
| A 攻击 | `Library/Locus/tmp/sword_enemy_attack_front_v1/clean_raw/` | 同目录 `matted_final/`（16 帧 canonical 本地键控） | 你们记录中"本地键控出现可见绿边"的难例 |
| B 跑动 | `Library/Locus/tmp/sword_enemy_walk_v1/raw_frames_v4/` | `matted_walk/`（6 帧 canonical 本地键控）+ `gpt_final_1108/`（6 帧 GPT 语义去背，已验收） | 三方对比：SVL vs 本地键控 vs GPT |

两套源均为 97 帧同尺寸；`raw_frames_v4/f001` 实测幕布绿为 `(0,168,60)`（用 SVL 的 `auto_key_color` 得到）。

## 部署步骤（含判据）

S1 克隆
```
git clone https://github.com/sparklecatta-lang/sprite-video-lab E:\sprite-video-lab
cd /d E:\sprite-video-lab && type VERSION        → 0.2.0
```

S2 运行时
```
py -3.10 -m venv E:\sprite-video-lab-models\venv
E:\sprite-video-lab-models\venv\Scripts\python.exe -m pip install --upgrade pip
E:\sprite-video-lab-models\venv\Scripts\python.exe -m pip install -r E:\sprite-video-lab\requirements.txt
```
判据：`...\venv\Scripts\python.exe -c "import cgi, PIL; print('ok')"` → `ok`

S3 冒烟
```
E:\sprite-video-lab-models\venv\Scripts\python.exe -m py_compile E:\sprite-video-lab\server.py
cd /d E:\sprite-video-lab
E:\sprite-video-lab-models\venv\Scripts\python.exe -m unittest tests.test_ai_matte_sizing
```
判据：编译通过 + 测试全绿（`tests` 在本机以 namespace package 方式可导入，已实测）。

S4 启动
```
E:\sprite-video-lab\start_sprite_video_lab.bat
```
该脚本自动：杀旧 server 进程 → 用 E: venv 起 `server.py --serve` → 打开浏览器。
判据：`http://127.0.0.1:8894/` 出现三段式页面（导入素材 / 去背景与取样 / 帧检查与导出），页脚状态栏为"等待导入素材"。

S5（可选，仅当要走视频输入）ffmpeg
```
winget install --id Gyan.FFmpeg -e
```
或便携包 + `set SPRITE_VIDEO_LAB_FFMPEG_DIR=<含 ffmpeg.exe 与 ffprobe.exe 的目录>`（解析顺序：PATH → 该环境变量）。
判据：`ffmpeg -version`、`ffprobe -version` 有输出。

## 首轮跑法（两条跑道 + 参数覆写）

`softness / despill / halo` 在 UI 里是隐藏项（默认 `16 / 0.6 / 1`），且 `halo=1` 会对 alpha 做 1px 腐蚀——**与"平滑 5–6px 羽化剖面"的验收形状天然冲突**。因此分两条跑道：

- **跑道 A「出厂默认」**：`halo=1`，不改隐藏值 → 回答"这工具开箱给什么"。
- **跑道 B「按验收口径」**：把 `app/index.html` 中 `haloInput` 的 value 由 `1` 改 `0`（只改 E: 里的副本）→ 回答"按你们口径能调到多好"。

操作要点：
1. 用"导入帧序列/导入文件夹"喂源帧（`/api/import-path` 只接受单个媒体文件，不接受帧目录）。
2. 处理方式选 `Chroma`，背景色选"手动指定颜色"，色样用"从画面添加"取亮部/暗部（自动取色结果为 `(0,168,60)`）。
3. 先"每 N 帧保留一帧 + 区间截取"只跑 1 帧做即时预览，扫容差 20/40/60/80 四档定边缘；再整段跑。
4. 导出只用 **Frames**（不需要 ffmpeg），输出目录指向 `E:\sprite-video-lab-work\exports\`。

时间预算：实测 960×960 单帧 `chroma_key_frame` 1.03 s + `alpha_aware_despill_frame` 0.17 s ≈ **1.2 s/帧 → 97 帧约 2 分钟/跑道**（纯 CPU 单线程）。

## 对比口径（复用现有度量，不新造指标）

用 `Library/Locus/tmp/sword_enemy_attack_front_v1/matting_from_raw_v2.py` 中的 `gex()` / `rim_metrics()`（numpy 实现，用现有 3.13 解释器执行）对 SVL 输出目录逐帧计算：

`visible_px / opaque_px / green_vis_px(white) / ring_alpha / ring_compRGB / ring_compGEx / inner_band_gEx / core_gEx / outline_ratio / holes_filled_px`

三方对齐：SVL 输出 vs `matted_final`(attack) / `matted_walk`(walk) vs `gpt_final_1108`(walk)。判据沿用既有验收：白底合成 0 可见绿像素 / 环不绿于内侧 / 羽化剖面（外 1–5px alpha 均值）平滑衰减。

> 已做的**单帧 smoke 仅作量级参照，不构成结论**：walk v4 `f001` 在阈值 40 / softness 16 / halo 1 下 → auto key `(0,168,60)`、visible 150,046 px（同量级参照：idle 155k / v4 噪声底修正后 146k）、`0<α<254` 边缘像素 1,474 个、其直通 RGB 平均绿优势 6.19。注意 6.19 与 `ring_compGEx` 不是同一量，正式结论必须走上面口径。

## 部署执行结果（2026-10-09 完成）

| 步骤 | 结果 |
|---|---|
| S1 克隆 `E:\sprite-video-lab` | ✅ VERSION 0.2.0，HEAD `01603e8` |
| S2 `py -3.10` venv `E:\sprite-video-lab-models\venv` | ✅ Python 3.10.11 + Pillow 12.3.0 + pip 26.2.1，`import cgi, PIL` 通过 |
| S3 冒烟 | ✅ `py_compile` 通过；单元测试 **63/63 全绿**（需额外装 `huggingface_hub`，否则 2 个 AI 下载测试报 ModuleNotFoundError） |
| S4 服务 | ✅ `127.0.0.1:8894` 返回 200；`/api/runtime-info` 确认 work_dir=`E:\sprite-video-lab-work`，模型缓存与 CorridorKey 根都在 E: |
| S5 ffmpeg | 未装（首轮不需要） |

**服务进程生命周期**：由 Locus 会话启动的 server 会随该次工具调用结束被回收。日常使用请双击 `E:\sprite-video-lab\start_sprite_video_lab.bat`（自身 detach 并开浏览器）；批量跑用 `Library/Locus/tmp/svl_run/run_all.py` 这种"脚本自带 server 生命周期"的方式。

## 首轮实测结果

跑法：仓库自己的 HTTP 接口（`/api/upload` 帧序列 → `/api/preview-frame` 容差扫描 → `/api/process` → `/api/export` Frames），驱动脚本 `svl_client.py` + `run_all.py`；度量 `svl_metrics.py` 复用 `matting_from_raw_v2.py` 的 `rim_metrics` **原文实现**。4 条跑道各 97 帧、960×960（源画布保留）、单条约 210 s（≈2.2 s/帧）。

匹配帧子集（攻击 = canonical 的 16 帧 f012…f081；跑动 = f023/25/27/29/30/32）中位数：

| 指标 | SVL 跑道A(halo=1) | SVL 跑道B(halo=0) | canonical 本地键控 | GPT(已验收) |
|---|---|---|---|---|
| 白底可见绿像素 | 2,140 | 5,577 | **0** | 181 |
| 其中 gEx>2 | 1,619 | 5,037 | **0** | 5.5 |
| 羽化剖面 外1/2/3/4/5px α | 0.1/0/0/0/0 | 0.1/0/0/0/0 | 0.9/0.8/0.6/0.5/0.3 | 1.0/0.9/0.8/0.7/0.6 |
| 外1px 环 α | 0.51 | 0.51 | 0.89 | 0.96 |
| 环 compGEx | −10.4 | −11.4 | −20.3 | −49.6 |
| 内侧带 gEx | −38.8 | −20.2 | −34.9 | −51.9 |
| 主体可见像素 | 148,430 | 153,462 | 169,991 | 146,816 |
| 实心像素(α≥0.99) | 147,053 | 152,049 | 98,889 | 114,048 |

跑动源结论同向。像素级核实（`svl_verify.py`）：SVL 残留的是 **α=255 的实心偏绿像素**（样例 `(99,160,132) gEx=28`、`(10,73,33) gEx=40`、`(87,108,58) gEx=21`）；canonical 两套均为 0；GPT 的 181 个是 α=254 的近白像素（`(247,248,247)` gEx=1）噪声。

## 结论：Chroma 路线在本批素材上不可用，且不是参数问题

根因（实现取舍，调参救不了）：
1. `alpha_aware_despill_frame` 对 **α≥254 的像素逐字复制**，而 Chroma 的硬阈值恰好把边缘污染像素定成 255 → 绿边被"保护"下来。
2. 缺 canonical 的三项（噪声底 / enclosed-hole fill / 环颜色外扩）；`halo_pixels` 腐蚀是唯一能压绿边的旋钮，但它同时削平羽化（跑道 A 绿少但更硬、跑道 B 羽化略好但绿更多）→ **"无绿边"与"保羽化"在该实现里互斥**，与 canonical 文档 §4/§6 记录的取舍同向，只是 SVL 更极端。

## 修订后的优化判断

- **P0 已被实测否掉**：`tools/apply_alpha_aware_despill.py` 在 canonical 攻击产物上 `--dry-run` → 幕布色被估成**蓝色 `(92,39,243)`**（canonical 已清零/替换透明区 RGB，取不到真实幕布绿），`edge_green_excess` 由 **0.0 → 2.48**，即把干净的产物弄脏。该工具只适用于"透明区仍保留幕布色"的素材。
- **P1（把 SVL 缺的三支柱补进 SVL）失去意义**：补齐后等于重写现有 canonical，没有理由引入 SVL 本体。
- **P2 降级为可选借鉴**：α 置信度加权的线性光反混合思路，目标是跑动源 `inner_band_gEx = −31`（描边偏绿，低于已验收参照 −54/−67）这个真实短板；需自建脚本，SVL 的现成工具因幕布色估计问题不可直接用。
- **尚未评估、仍可能有用**：(a) SVL 的两条 AI 路线（CorridorKey / BiRefNet）——真正的模型抠图，可能替代脆弱的 GPT 路线（注意 CorridorKey 许可边界）；(b) 与抠图质量解耦的能力——**方形底部对齐画布**（可直接替代手工 Y 偏移）、premultiplied-alpha 缩放、帧筛选/按选序、四种导出。这两类在首轮里未被证否。

## 复现与产物

- 驱动与度量：`Library/Locus/tmp/svl_run/{svl_client.py,run_all.py,svl_metrics.py,svl_verify.py}`
- 数据：`Library/Locus/tmp/svl_run/comparison.json`、`run_*.json`、`sweep_*.json`、`server.log`、`p0_despill_attack.json`
- 对照图（白底 1:1 + 3x 边缘放大）：`Library/Locus/tmp/svl_run/preview/sheet_attack_f012.png`、`sheet_walk_f023.png`
- SVL 产物：`E:\sprite-video-lab-work\exports\<时间戳>-export\frames\`（4 套 × 97 帧）

## keyed_v2 交付（2026-10-09，用户指定任务）

`Enemy1011ArtSource/03_SelectedFrames/Attack` 下已新建 **`keyed_v2/`**，16 帧 RGBA 960×960，命名沿用同层 `keyed/` 约定（`attack_f{NNN}_final.png`）。

- 源：同目录 `raw/`（已核实与 `sword_enemy_attack_front_v1/clean_raw` 逐字节相同）；同层 `keyed/` 与 `matted_final` 逐字节相同 → 本目录是同一批素材的整理版。
- 参数：`chroma` + 自动取色（首帧 f012 → `#019F40`）+ 容差 80 + softness 16 + halo 1，画布保留 960×960，无后处理。
- 脚本：`Library/Locus/tmp/svl_run/run_keyed_v2.py`（自带 server 生命周期）；核验 `verify_keyed_v2.py`；记录 `run_attack_keyed_v2.json`。
- 对照图：`Library/Locus/tmp/svl_run/preview/attack_f{012,081}_raw_keyed_keyedv2.png`。

同 16 帧中位数对比：

| 指标 | keyed_v2 (chroma) | keyed (canonical) |
|---|---|---|
| 白底绿像素(α>5) | 1,817（单帧 932–6,159） | **0** |
| 羽化 外1/2/3px α | 0.14 / 0.02 / 0 | 0.88 / 0.81 / 0.63 |
| 环 compGEx | −11.2 | −20.25 |
| 主体可见像素 | 147,823 | 169,990 |
| 实心像素 | 146,448 | 98,889 |

结论不变：keyed_v2 可作为"另一种效果"的对照版本使用，但按现有验收口径（白底 0 绿像素 + 平滑羽化）不达标，**不要直接进 Assets**。注意：本次自动取色来自 f012（`#019F40`），与首轮 97 帧跑（首帧 f001 → `#00A73C`）不同，故同一帧的绿像素数略低（如 f012：1,625 对 1,978）。

## keyed_v3（修正后的优化方案，2026-10-09）

用户更正：**头顶飘带本体是红色，绿色是残留污染** → "加大 halo 腐蚀"作废（实测 halo=2 会把细飘带的红色像素削掉 −5.4%）。

绿的深度实测（基线 t80/s16/h1，从透明区向内逐px）：**63% 在第 1px 环 / 16% 第 2px / 7% 第 3px（86% 在3px内），但 4% 在体内更深处** → 腐蚀只能治外圈且伤飘带，正确工具是颜色规则。

**已交付 `keyed_v3/`**（16 帧 RGBA 960×960，命名同 keyed/）：
1. SVL Chroma：容差 80 / **softness 0** / halo 1
2. 本地后处理 **绿保证**（= canonical 第 6 步，可见像素强制 `g ≤ max(r,b)`）——SVL 内核永远做不了（despill 逐字复制 α≥254，`is_background_residue_pixel` 对 α≥255 返回 False）

| 指标 | keyed_v3 | keyed_v2 | keyed (canonical) |
|---|---|---|---|
| 白底绿像素 | **0** | 1,817 | 0 |
| 半透明像素 | **0** | 1,572 | 73,432 |
| 主体可见像素 | 147,854 | 147,823 | 169,990 |
| inner_band_gEx | −38.25 | −40.25 | −34.85 |
| outline_ratio | 0.828 | 0.851 | 0.883 |

飘带红色像素 f051 5,871→5,891 / f053 8,433→8,470（完好）；16 帧共修 50,735 个绿像素。代价：完全硬边（canonical 是 73k 半透明像素的宽羽化）；描边环略亮。若想保留羽化：`softness 16 + 绿保证`（绿仍 0、半透明 1,572）。

脚本：`Library/Locus/tmp/svl_run/green_guarantee.py`（可复用 CLI）+ `run_keyed_v3.py`；对照图 `preview/head_ribbon_f051_v2_vs_v3.png`。**这两个问题都不需要 AI 模型。**

## keyed_v4（修正 v3 两处缺陷，2026-10-09 交付）

用户目检 v3 发现：①仍有些许绿边；②没有半透明了，但仍有浅绿/深绿"毛刺"贴在角色上。诊断结论（量化）：

- **v3 的 `g ≤ max(r,b)` 在本幕布色下把残绿变成青绿**：幕布 `(1,159,64)` 的 `b=64≫r=1`，clamp 落在 `g=b>r` → 产出 teal；v3 每帧有 2,867 个像素 g 高于 (r+b)/2。
- **v3 的删除步骤空转**：clamp 已抹平绿优势信号（d≤0），按 `d>24` 删不到像素。正确做法是从**源帧**读 `d_src = g − max(r,b)`（幕布≈98、角色≈−46）；v3 每帧残留 572 个 `d_src>20` 的可见像素，且全部落在 2px 腐蚀存不下来的细结构上（毛刺==残留）。

**配方**（已交付 `keyed_v4/`）：Chroma `t100 / softness 0 / halo 1` → 删 `d_src>20` → 中性化 `g ← min(g,(r+b)/2)`。

| 配置 | 可见像素 | 毛刺 | 绿/青绿 cast |
|---|---|---|---|
| v3 | 147,854 | 572 | 2,867 |
| t80 D20+中性化 | 147,340 (−0.35%) | 0 | 0 |
| **t100 D20+中性化（交付）** | 145,883 (−1.33%) | 0 | 0 |
| t110 D20+中性化 | 144,522 (−2.25%) | 0 | 0 |

飘带完好（头带红色像素各变体均在 9.5k 量级；伤飘带的是 halo=2 → 9,007）。脚本 `svl_run/{keyed_v4.py,keyed_v4_final.py}`；对照图 `preview/head_f051_v3_vs_v4.png`、`head_f053_v3_vs_v4.png`。

**遗留**：我没有图像视觉，`d_src≤20` 的灰绿残留和主观观感需用户目检确认；通过后可将该后处理固化为步骤。

## 两条运维坑（本次实测，会反复踩）

1. **UI 的「清空 WebApp 内部文件」会删掉 exports/jobs/uploads/previews**（`clear_managed_runtime_files` 直接 `rmtree` `MANAGED_RUNTIME_DIRS`，含 `EXPORTS_DIR`）。首轮 4 套 97 帧产物就是这样丢的。→ **需要留存的产物一律写到项目内的目标目录**（像本次 `keyed_v2/`），不要只指望 `E:\sprite-video-lab-work\exports`。
2. **8894 会出现重复监听**：`start_sprite_video_lab.bat` 被杀旧进程的 PowerShell 段未必能清干净（本次实测存在 venv 版与系统 Python310 版两个 server 同时存活），请求会随机落到其中一个。表现就是"导入后状态不对/素材未导入"这类无解异象。→ 异常时先查重：
   ```powershell
   Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -like '*server.py*' } | Select-Object ProcessId, CreationDate
   ```
   只留一个实例。

## 风险与回退

- 许可：CorridorKey（代码+权重）为 CC BY-NC-SA 4.0 + 附加条款，允许"作为商业项目的一部分处理图像"，禁止重打包/转售、禁止付费推理服务、嵌入商业软件包需单独书面协议 → 只在第二阶段评估；首轮 Chroma 不涉及。
- BiRefNet 通过 `trust_remote_code=True` 加载 HF 仓库代码（revision 已 pin），属第三方代码执行。
- 所有产物在 E:，Unity 项目零改动 → 回退＝停服务并删除 E: 相关目录。
- 已知不可用：视频输入 / 透明 MOV / GIF（需 ffmpeg）；帧序列必须走浏览器入口。
- UI 不暴露画布布局：`square_bottom / square_center` 与 `reduce_px` 只存在于 `/api/process` 载荷，UI 硬编码为 `auto / 0` → 想用"底部对齐的方形画布"必须走 API 或改 `app.js`。
- SVL 的 `despill_strength` 在主管线被硬编码为 `0.0`，`despill_alpha_edges` / `restore_source_colors_after_matte` 是死代码；Chroma 的颜色清理完全由 `alpha_aware_despill_frame` 承担。不要指望调那个参数。
- **解释器纪律**：SVL 服务固定用 `E:\sprite-video-lab-models\venv`（3.10）；对比/度量脚本用系统 3.13（带 numpy 2.5.3）。不要把 3.13/3.14 给 SVL。
- venv 里额外装了 `huggingface_hub`（仅为了跑绿仓库自带单元测试，未启用 torch/AI 推理）。
- 仓库自带的 `I:\FF\Flowframes\...` ffmpeg 回退路径在本机不存在（无害，只是不要依赖它）。

## 后续可选方向（待用户选择）

1. **评估 AI 路线**：`setup_ai_runtime.bat`（torch cu128 + EZ-CorridorKey pin `35b6b750`）后跑 CorridorKey（绿/蓝幕）与 BiRefNet，用同一套 `rim_metrics` 与 canonical/GPT 对比。注：CorridorKey 缓存与工作目录已按 E: 布局就绪。
2. **评估非抠图能力**：`square_bottom` 方形画布（底部对齐，替代手工 Y 偏移）+ premultiplied-alpha 缩放 + 帧筛选 + 图集导出。需走 `/api/process` 或改 `app.js`（UI 未暴露）。
3. **归档停用**：保留 E: 部署供人工预览，不再深入；结论已入库。
