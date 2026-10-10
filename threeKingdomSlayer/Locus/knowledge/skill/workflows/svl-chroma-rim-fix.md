---
id: kd_ed6c0171-4e5c-48d3-8586-f5836ee7f099
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
skillEnabled: true
skillSurface: command
summary: 'Sprite Video Lab 抠图工作流从零部署与使用手册（2026-10-10 用户验收，含 109 实装）。配方固定为 Chroma + 自动取背景色 + 容差按底材标定（彩色幕布 79 / 中性底 14，或中性底 6+幕布清理） + softness 16 + halo 1 + 只勾半透明像素转不透明；已弃用先做平滑处理（Real-ESRGAN 会重绘细节）与 softness 0。含新主机从零部署与判据、网页版操作、批量脚本清单与要改常量、新角色素材源文件夹约定与 raw 校验、四项目检验收、Unity 入库规格（替换与新建两条路径）与逐角色放置核对法，以及十三类踩坑根因。'
---

# Sprite Video Lab 抠图工作流：从零部署与使用手册

> **用途**：在任意 Windows 主机上，从零把 AI 生成的纯色底视频帧抠成透明精灵，并入库 Unity。本文件是**可独立使用的部署 + 操作手册**（含路径、命令、判据、踩坑），另一台没装过的主机照做即可跑通。
> **当前配方（2026-10-10 用户验收）**：`Chroma` + `自动取背景色` + **容差（按底材标定）** + `softness 16` + `halo 1` + **只勾「半透明像素转不透明」**；**不要勾「先做平滑处理」**。
> **维护方式**：改动必须附「实测数值 + 已验收参照物对照」，不接受只凭描述的结论。
> **相关**：`skill/workflows/local-green-screen-cutout.md`（旧的自有 numpy 路线，保留作对照）；`memory/sprite-video-lab-matting-evaluation.md`（本流程的完整迭代与实测台账）。

---

## 第 0 章 适用范围与一句话配方

**适用**：源素材是**纯色平底**（绿幕/蓝幕/深灰/白底），底色均匀（边框环 std ≤ 2）。角色由 AI 视频生成，常带一层「深色/偏绿描边」。
**不适用**：底不是纯色（真实或 AI 生成的复杂背景）→ 需要语义模型（`BiRefNet`/`CorridorKey`，见第 7.4 节）；需要保留角色本体上的**绿色部件**。

**一句话配方**：`Chroma` → `自动取背景色` → 容差（**按底材标定**）→ 只勾 `半透明像素转不透明`。
`softness` 保持默认 **16**，`halo` 保持默认 **1**。

---

## 第 1 章 从零部署（新主机照做）

### 1.1 前置条件

| 项 | 要求 | 说明 |
|---|---|---|
| 操作系统 | Windows | 只在 Windows 上验证过 |
| Python | **3.10**（3.11/3.12 可用；**3.13+ 不可用**） | `server.py` 顶层 `import cgi`，该模块在 3.13 被移除 |
| git | 任意版本 | 取代码用；也可直接拷目录 |
| ffmpeg / ffprobe | **可选** | 仅在需要**视频/GIF 输入**或导出**透明 MOV / GIF** 时才要；只处理 PNG 帧序列不需要 |

### 1.2 取代码

```powershell
git clone https://github.com/sparklecatta-lang/sprite-video-lab E:\sprite-video-lab
```
若 GitHub 直连不通：换可用的镜像/代理，或直接从有该目录的主机拷贝整个 checkout（见 1.7）。

### 1.3 建运行时

```powershell
py -3.10 -m venv E:\sprite-video-lab-models\venv
E:\sprite-video-lab-models\venv\Scripts\python.exe -m pip install --upgrade pip
E:\sprite-video-lab-models\venv\Scripts\python.exe -m pip install -r E:\sprite-video-lab\requirements.txt
# 可选：让仓库自带单元测试全绿（否则 2 个 AI 下载测试会报 ModuleNotFoundError）
E:\sprite-video-lab-models\venv\Scripts\python.exe -m pip install huggingface_hub
```
**判据**：`...\venv\Scripts\python.exe -c "import cgi, PIL; print('ok')"` → 输出 `ok`。
> 基础依赖只有 Pillow；torch 等 AI 依赖**本工作流不需要**，不要装。

### 1.4 工作目录与启动

约定路径（与启动器默认一致，放在 E: 是为了不占项目盘）：

| 用途 | 路径 |
|---|---|
| 代码 checkout | `E:\sprite-video-lab` |
| 运行时（venv） | `E:\sprite-video-lab-models\venv` |
| 工作目录（上传/任务/预览/导出） | `E:\sprite-video-lab-work` |
| 端口 | `127.0.0.1:8894` |

**启动（推荐）**：双击 `E:\sprite-video-lab\start_sprite_video_lab.bat`
它会：杀掉旧 server → 用 E: 的 venv 起 `server.py --serve` → 打开浏览器。

**启动（手动，想看日志时）**：
```powershell
cd /d E:\sprite-video-lab
set SPRITE_VIDEO_LAB_WORK_DIR=E:\sprite-video-lab-work
E:\sprite-video-lab-models\venv\Scripts\python.exe server.py --serve --host 127.0.0.1 --port 8894
```
**判据**：浏览器打开 `http://127.0.0.1:8894/`，出现三段式面板，页脚状态为「等待导入素材」。
> **必须设 `SPRITE_VIDEO_LAB_WORK_DIR`**：不设时 App 会在 `E:\sprite-video-lab\work` 找工具与产物（.bat 已经设好，手动启动要自己设）。

### 1.5 首次自检清单

| 检查 | 命令 / 动作 | 期望 |
|---|---|---|
| 服务在听 | 浏览器开 `http://127.0.0.1:8894/` | 200 + 三段式页面 |
| 运行时正确 | 页面里 `runtime-info`（或看启动窗口） | `work_dir = E:\sprite-video-lab-work` |
| 无重复实例 | `Get-CimInstance Win32_Process \| Where-Object { $_.Name -like 'python*' -and $_.CommandLine -like '*--serve*' }` | 只有 1 个 |
| 单测 | `...\venv\Scripts\python.exe -m unittest tests.test_ai_matte_sizing`（在 checkout 下） | 63 passed（装了 huggingface_hub） |

### 1.6 可选组件（本工作流**不需要**）

- **Real-ESRGAN（"先做平滑处理"）→ 已弃用**：它是生成式重建，会把角色细节重绘（实测每帧约 8.5 万像素颜色被改），仅在想要"极致干净的边缘"时短期使用。**不要装、不要勾**。
- **BiRefNet / CorridorKey**：仅在**底不是纯色**时才需要，要额外装 torch（约 2.5 GB）+ 各自权重，且 CorridorKey 有许可边界（见 7.3）。

### 1.7 从旧机器要带来的东西（可选）

1. **本手册**（`Locus/knowledge/skill/workflows/svl-chroma-rim-fix.md`）。
2. **批量/度量脚本**：**就用仓库里的 `Locus/tools/svl-matting/`**（已提交：`svl_common.py` / `svl_matte_batch.py` / `svl_scan_tolerance.py` / `svl_build_source.py` / `svl_deploy_to_unity.py` / `green_guarantee.py` + `README.md`）。**新主机只要拉这个仓库就有，不需要改代码**。
   ⚠️ 本机的工作副本在 `Library/Locus/tmp/svl_run/`，而 **`Library/` 被 .gitignore 忽略、不会随仓库同步**——所以别把它当成分发位置。项目内旧的一次性脚本（如 `enemy1011_run_new.py`）也在那儿，仅作参考。
3. **度量脚本的依赖**：脚本里的 `ero/dil/reach_border` 等 helper 是从
   `Library/Locus/tmp/sword_enemy_attack_front_v1/matting_from_raw_v2.py` 里 **exec 前缀**取用的；另一台机器要么一起拷这个文件，要么用第 4.3 节的替代口径。

---

## 第 2 章 网页版操作（主路径，零脚本）

### 2.1 导入

把帧拖进第一段「**导入源素材**」的拖放区（**多选文件**；点它也可以打开文件选择框多选）。
⚠️ **不能拖文件夹本身**——浏览器会静默忽略（`dataTransfer.files` 为空），状态栏一直停在"等待导入素材"。
⚠️ 第三段「帧检查与导出」里的「导入帧序列 / 导入文件夹」是**另一条路径**：它按设计跳过抠图（只用于检查/缩放/导出已抠好的帧），**不会解锁「去背景」面板**。

### 2.2 参数

| 参数 | 取值 | 说明 |
|---|---|---|
| 处理方式 | **Chroma** | 纯色底（绿/蓝/灰/白）都用它 |
| 背景色 | **自动取背景色** | 取画面边缘主色；不准时改"手动指定颜色"，用"从画面添加"在亮部/暗部连续取色（≤12 个色样） |
| 容差 | **按底材标定**（见 2.3） | 唯一的必调项 |
| `softness` | **16（默认，别改）** | 软边才有中间 alpha；改成 0 会关掉反混合（见第 6 章） |
| `halo_pixels` | **1（默认）** | 加大等于从外向内削剪影，会伤细部件 |
| 后处理 | **只勾「半透明像素转不透明」** | 其余三个开关都不要（背景残留转黑 / 饱和度归零 / 半透明转黑 都不需要） |
| 「先做平滑处理」 | **不要勾** | 已弃用（重绘细节） |

### 2.3 容差怎么定（**按底材标定**，不能全源统一）

先在网页里用「**预览当前帧**」+ 右侧即时预览（Chroma 在浏览器本地算，改参数立刻刷新）对**一帧**扫容差，定下来后用同一值处理整段。

| 底材 | 典型容差 | 实测依据 |
|---|---|---|
| **绿幕 / 蓝幕**（std ≈ 1.5） | **70–80** | 1011 用 79、骑兵用 70；绿幕对容差不敏感（T=12 与 T=100 的可见像素只差 2%），取大值能多清幕布族 |
| **深灰 / 白底**（std ≈ 1） | **10–20（从 14 起试）** | 1011 Idle（灰底 (44,43,48)）：T=79 → **被删主体 66.6%（角色被吃掉）**；T=14 → 干净且保形最多（可见 124k，幕布残留 0）；T=11 会留 180 个幕布灰点。**注意**：T=14 是“幕布绝对干净”的解，但**被删主体仍有 ~13%**；若要更保形，用 **T=6 + 中性幕布清理**（可见像素中 `饱和度 (max−min)/max < 0.10` 且通道值在幕布值 ±14 内 → 透明），实测 1011 Idle 被删降到 **3.4%** 且幕布残留 0 |

**判定标准（预览时就该满足）**：
1. 背景干净（看不到幕布色残留/灰点）；
2. 边缘没有明显绿边；
3. **细部件连续**（马腿、长枪、缰绳、衣摆不被切出缺口）。

### 2.4 处理与导出

- 定好参数后：选片段区间 → 点「**开始处理区间**」（写盘到 `E:\sprite-video-lab-work\jobs\<job>\processed\`）。
- 到第三段「帧检查与导出」：挑帧（全选/奇偶/反选/按选序）→「直接导出」→ 选 **Frames**（PNG 序列 + `frames.json`，记录逐帧时长）。
  （`Spritesheet` 也纯本地；`透明 MOV` / `GIF` 需要 ffmpeg。）

### 2.5 两个产物坑

1. **「清空 WebApp 内部文件」会 `rmtree` 掉 `exports / jobs / uploads / previews`** → 要留的产物**必须另存到项目内目录**。
2. 导出完成后它只会打开对应文件夹；本工作流的习惯是**把成品拷到素材源目录下的 `keyed_*/`**（见第 5 章）。

---

## 第 3 章 无头批量（可选）

### 3.1 为什么需要
一组动作几十帧时，网页点一次处理即可；但如果要**同参数批量跑多组**、或要**量化指标**，用脚本更快更可复核。

### 3.2 命令（本项目示例）

```powershell
# 绿幕批量（配方 = Chroma 自动取色 + T + softness16 + halo1 + 半透明转不透明）
E:\sprite-video-lab-models\venv\Scripts\python.exe Library\Locus\tmp\svl_run\cav_run_B.py
# 灰底单独标定（先扫容差，再出成品）
E:\sprite-video-lab-models\venv\Scripts\python.exe Library\Locus\tmp\svl_run\idle_esr_calibrate.py
# 单点去绿工具（对任意帧目录，不改 alpha，带 --dry-run 与备份）
E:\sprite-video-lab-models\venv\Scripts\python.exe Library\Locus\tmp\svl_run\green_guarantee.py <目录> --dry-run
# 按角色整套（自动选容差 + 出报告 + 总览图）
E:\sprite-video-lab-models\venv\Scripts\python.exe Library\Locus\tmp\svl_run\enemy109_run_workflow.py
E:\sprite-video-lab-models\venv\Scripts\python.exe Library\Locus\tmp\svl_run\enemy1011_run_new.py
```
> 跑脚本时**同样要设** `SPRITE_VIDEO_LAB_WORK_DIR=E:\sprite-video-lab-work`（否则找不到组件/产物目录）。
>
> **解释器**：批量/度量脚本里已内置 `sys.modules.setdefault("cgi", ...)` 兼容处理，**用 3.10 venv 或 3.13+ 都能跑**；但**服务端（`server.py`）必须 3.10**。
> **依赖**：脚本用 `numpy`（度量）+ `Pillow`；两者都不在 `requirements.txt` 里时要手动装：`pip install numpy pillow`。

#### 3.2.1 通用参数化脚本（推荐，零改代码）

`Library/Locus/tmp/svl_run/svl_*.py`（也随上传包分发）：全部走命令行参数，**不需要改代码**。

```powershell
$PY  = "E:\sprite-video-lab-models\venv\Scripts\python.exe"
$RUN = "Locus\tools\svl-matting"      # 随仓库分发（受版本控制）；工作副本在 Library\Locus\tmp\svl_run（gitignore，不入库）

# 批量抠图：整个素材根（每个动作一个输出目录），自动按底材选容差
$PY $RUN\svl_matte_batch.py --src "<ArtSource>\03_SelectedFrames"
# 单个帧目录 + 自定义输出 + 总览图
$PY $RUN\svl_matte_batch.py --src "<某帧目录>" --label Attack --out-root "<输出根>" --sheet
# 扫容差并给推荐值（彩色幕布会给默认 79 + 权衡；中性底会算“幕布残留=0 且保形最多”的 T）
$PY $RUN\svl_scan_tolerance.py --src "<某动作目录>" --tolerances 55,70,79
# 建新角色的素材源文件夹（重命名 + sha256 清单 + 拷已部署参照）
$PY $RUN\svl_build_source.py --src "<已验收 raw 目录>" --dest "<ArtSource>\03_SelectedFrames" `
    --action Attack --deployed-reference "<Assets 精灵目录>"
# 入库（**默认干跑**，只打印逐帧所需偏移及其跨度；确认后加 --apply 才写入）
$PY $RUN\svl_deploy_to_unity.py --src "<ArtSource>\03_SelectedFrames" --dest "<工程>\Assets\Sprites\Enemy\EnemyXXX" `
    --name-template "Enemy_XXX_{Action}{i}.png"
$PY $RUN\svl_deploy_to_unity.py ... --apply
# 单点去绿（不改 alpha，带备份与报告）
$PY $RUN\green_guarantee.py <目录> --dry-run
```

要点：
- `svl_matte_batch.py`：自动按底材选容差（**彩色幕布 79 / 中性底 14**），可用 `--tolerance N` 覆盖、`--keep-semi` 不硬化、`--only A,B` 限动作、`--sheet` 出图、`--report` 指定报告。
- `svl_deploy_to_unity.py`：**默认干跑**，会打印“逐帧所需偏移及跨度”（即 5.2 的核对法）；`--align-deployed` / `--foot-line N` 处理变位组；替换会自动备份到 **Assets 之外**；新建与替换会自动区分。
- 共性：`SPRITE_VIDEO_LAB_ROOT`（默认 `E:\sprite-video-lab`）指定 checkout；度量 helper（`ero/dil/reach_border`）**已内联在 `svl_common.py`**，不再依赖任何项目脚本。

### 3.3 换素材/换机器要改的常量（仅适用于项目内旧脚本）

| 常量 | 含义 |
|---|---|
| `TASK` / `SRC` / `ART` | 源帧目录（或素材根目录） |
| `OUT` | 成品输出目录 |
| `THRESHOLD` | 容差（按底材，见 2.3） |
| `INTERMEDIATE` | 中间产物目录（用 ESR 时才有） |
| 命名函数 `out_stem()` | 输出命名规则（本项目：`<prefix>_f<NNN>_<版本>.png`） |

> 上面这些常量只用于项目内那些**早前写的一次性脚本**；**通用脚本（3.2.1）不需要改任何常量**——新主机优先用它们（位置：`Locus/tools/svl-matting/`，随仓库分发）。

**现有关键脚本（本项目，`Library/Locus/tmp/svl_run/`）**：

| 脚本 | 作用 |
|---|---|
| `cav_run_B.py` | 绿幕批量（指定容差；骑兵/109 类源用） |
| `enemy1011_run_new.py` / `enemy109_run_workflow.py` | 按角色批量：按底材自动选容差（彩色 79 / 中性 14）并出报告与总览图 |
| `enemy109_build_source.py` | **建新角色的素材源文件夹**：从任务清单拷入已验收 raw（1011 风格命名）+ sha256 校验 + 拷已部署参照 |
| `idle_esr_calibrate.py` | 中性底容差标定（扫多个 T，选“幕布残留=0 且保形最多”的） |
| `green_guarantee.py` | 单点去绿工具（不改 alpha，带 --dry-run 与备份） |
| `deploy_v5_to_unity.py` / `deploy_109_to_unity.py` | 入库：替换（保留 .meta）或新建精灵 |
| `cav_variant_compare.py` | 多参数变体对照（出图 + 指标表，用于选参） |

### 3.4 新角色的素材源文件夹约定（每角色一份）

`<角色>ArtSource/03_SelectedFrames/<动作>/`：

| 子项 | 内容 |
|---|---|
| `raw/` | **已验收的原始帧**，命名 `<动作小驼峰><序号>_f<源帧号>.png`（如 `attack1_f055.png`、`mountedIdle3_f020.png`） |
| `raw_manifest.json` | 逐帧来源路径 + sha256 + 来源帧号 + 幕布色/建议容差 |
| `deployed_reference/` | 该动作**当前已部署**的精灵（作为验收对照基线，替换前拷一份） |
| `keyed_ui_t<容差>/` | 本工作流产物（与 `raw/` 同名） |
| `<动作>Keyed 报告 / 总览图` | 放在 `03_SelectedFrames/` 根下（如 `enemy109_keyed_ui_t79_report.json`） |

**由脚本实现**：`svl_build_source.py --src <已验收 raw 目录> --dest <ArtSource>\03_SelectedFrames --action <名字>`。

**开工前两道核对**（都做过，代价很低）：
1. **raw 真的是未动的解码帧**：与解码目录（如 `_decoded_source/frame_055.png`）逐数组比对，必须完全一致（否则可能拿的是已处理图）。
2. **sha256 对得上来源清单**：拷入时逐帧校验，失败即为选错帧/文件损坏。

**可用角色目录**：`Enemy1011ArtSource/03_SelectedFrames/`（10 动作 69 帧，已验收）、`Enemy109ArtSource/03_SelectedFrames/`（Attack 18 + Charging/Windup/MountedIdle 各 6）。

---

## 第 4 章 验收口径

### 4.1 硬门（必须为 0）
- **半透明像素 = 0**（`0 < α < 255`）。勾了「半透明像素转不透明」就必然满足。

### 4.2 主验收（目检，四条）
1. 无可见绿边 / 绿点；
2. 无「缺口 / 奶酪感」（细部件连续不断）；
3. 角色自身颜色与纹理**没有被改淡/改灰**；
4. 与源帧对照，造型没有被重绘（若用了平滑处理，重点看这条）。

### 4.3 参考量化（非硬门，用于对比不同参数）

| 指标 | 参考量级（1011 绿幕组 / 骑兵组） | 说明 |
|---|---|---|
| 白底可见绿像素 `gEx>0` | 数百 ~ 数千 | `gEx>8` 通常 ≤ 2 千，肉眼不可见 |
| 被删主体占比 | 1011 各组 10–27%；**109 各组 8–17%**；骑兵（用绿幕判据口径）4–5% | 口径：**离幕布色 ≤14 视为背景**；骑兵那组用的是另一套绿幕判据，**不与之直接比较** |
| 封闭孔像素 | ≤ ~750 | 全透明且不与画面边缘相连的洞 |
| 薄带占比 / 周长面积比 | 0.026 / 0.052 | 越低越"实"、越少缺口 |

> 量化脚本见 3.2；不同底材/不同口径的绝对值**不可直接跨轮比较**，只比同一轮内的不同参数。

---

## 第 5 章 入库 Unity

### 5.1 精灵规格（由 `.meta` 持有，**不要改**）

| 项 | 值 |
|---|---|
| 画布 | **1108×1108** |
| pivot | 画布中心 **(554, 554)** |
| PPU | **16** |
| 压缩 / Alpha | Uncompressed / `AlphaIsTransparency = true` |
| Sprite Mode | Single；Filter = Point |

### 5.2 放置规则

- **默认**：960×960 的成品**原尺寸**贴到 1108 画布的 **(74, 120)**。
- ⚠️ **这个偏移是每个角色各自的历史约定，不要跨角色照搬**：先用下面的核对法实测确认，再入库。已实测：**1011 = (74,120)，脚线 ≈896/897**；**109 = (74,120)，但脚线 ≈1051**（骑乘角色占满画面，脚底位置不同）。
- **核对法**（入库前跑，必做）：对该角色每个已有的已部署精灵，算 `所需偏移 = 已部署 alpha 包围盒最小角 − 成品帧 alpha 包围盒最小角`；若同一动作内各帧偏移**跨度 ≤ ~6px** → 用该固定偏移；若跨度很大（如 1011 的 getup 曾跨 150px）→ 说明这组历史产物做过额外裁切/重排，需逐帧对齐或单独处理。
- **需要变位的组（如 getup）**：裁剪到主体（上下各留 8px）后贴到
  `(原部署 x_min − 帧 bbox x_min, 脚线 − 裁剪后主体底边)`，**脚线 = idle 精灵 alpha 底边（实测 896）**。目的：Idle→Getup 切换不跳。

### 5.3 替换方式（关键）

**只覆盖 PNG 字节，保留 `.meta`** → GUID、pivot、PPU、导入设置全部不变 → 动画/预制体引用自动照旧。
⚠️ 不要删除重建素材文件（会换 GUID、断引用）。

**新建精灵（该动作还没有资产时，如 109 的 Attack）**：
1. 先把 1108 画布的 PNG 写到目标目录（此时还没有 `.meta`）；
2. 让 Unity 导入（`AssetDatabase.Refresh`），再**把导入设置镜像到该角色已有精灵**：
   `textureType=Sprite`、`spriteImportMode=Single`、`spritePixelsPerUnit=16`、`filterMode=Point`、`textureCompression=Uncompressed`、`alphaIsTransparency=true`、`mipmapEnabled=false`、`maxTextureSize=2048`、`spriteMeshType=Tight`、`spriteAlignment=Center`；
3. `ti.SaveAndReimport()`（18 张约 15–20 秒，单次 `unity_execute` 不要超过 30s，必要时分批）；
4. 核验：新精灵也是 1108×1108 / PPU 16 / pivot (554,554) / Point / Uncompressed。

> **新建精灵会拿到新的 GUID**（因为此前无人引用，安全）；但**它还没有任何动画 clip 引用它**——接线是另一件事（见 7.3）。

### 5.4 部署后必做的 Unity 验证（分两次 `unity_execute` 调用，单次 ≤30s）

1. 只对 mtime 变化的文件：`AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate)`；
2. 核两件事：① 全部精灵 1108×1108 / pivot (554,554) / ppu 16；② 遍历 `Enemy_*` 的 clip
   `AnimationUtility.GetObjectReferenceCurveBindings/Curve` → **空引用必须为 0**；
3. 若要知道“哪些 clip 引用了这个角色的精灵”（本次 109 用它确认了 `Enemy_109_MountedIdle.anim` 6 键 0 空引用）：扫全部 `AnimationClip`，对每个 sprite 键取
   `AssetDatabase.GetAssetPath(k.value)` 并过滤路径含该角色目录的；同时统计 `k.value == null` 的数量。

### 5.5 备份与回退

- 覆盖前把原 PNG 复制到 `Library/Locus/tmp/backup_<对象>_<时间戳>/`，并写 `deploy_report.json`。
- 由于素材在 git 里，整体回退可用：`git checkout -- Assets/Sprites/<目录>/`。

---

## 第 6 章 踩坑与失败模式（都实测过）

| 现象 | 根因 | 对策 |
|---|---|---|
| **角色正常颜色被刷掉/暗部消失** | ①`softness=0` 关掉了反混合；②"描边修复"用相对判据（内侧越暗/越绿阈值越松）| 用本手册配方（softness 16 + 半透明转不透明）；不要用相对判据的环替换 |
| **灰/白底角色被吃掉大半** | 用绿幕容差（T=79）→ 键球罩住暗部角色色 | 按底材标定（灰底 T≈14） |
| 细部件被切出缺口（奶酪感） | `softness=0` 时混色过渡带只能"删掉"或"留绿" | 保持 softness 16 |
| 边缘变青绿 | 用 `g ≤ max(r,b)` 做夹紧（绿幕 b≫r 时落到 g=b>r） | 不要做这个夹紧 |
| 造型/细节与原片不同 | **先用平滑处理（Real-ESRGAN）**：它是生成式重建 | **不要勾** |
| 拖文件夹没反应 | 浏览器忽略文件夹拖放（files 为空），且不报错 | 在资源管理器里 `Ctrl+A` 选文件后拖入 |
| 导入后没有"去背景"面板 | 用了第三段的「导入帧序列/文件夹」（按设计跳过抠图） | 用第一段拖放区导入 |
| 产物莫名消失 | UI「清空 WebApp 内部文件」会删 exports/jobs/uploads/previews | 成品另存到项目内目录 |
| 界面行为诡异/状态不对 | 端口 8894 **重复监听**（两个 server 实例） | 按 1.5 的查询命令查重，只留一个 |
| App 找不到某组件 / 找不到产物 | 没设 `SPRITE_VIDEO_LAB_WORK_DIR` | 启动前设置（或直接用 .bat） |
| GitHub 拉不动 / 模型下不来 | 本机直连 github.com 不通（curl 返回 000） | 用镜像/代理取包，手动放到期望目录 |
| Python 3.13+ 起不来 | `cgi` 模块已被移除 | 用 3.10 |
| **新建的精灵在游戏里不出现/不动** | 新精灵是新增资产，**没有任何 clip 引用它**（本工作流不接线） | 单独做动画部署（见 7.3） |

### 附：已弃用的做法（不要重复尝试）

- ❌ `softness = 0`（硬边）：会关掉反混合（`C'=(C−(1−α)B)/α` **只在 0<α<254 生效**）。
- ❌ 「先做平滑处理」（Real-ESRGAN）：重绘细节。
- ❌ 脚本后处理链（描边环替换 / 删源绿 `d>8` / 中性化 / 硬绿保证）：默认关闭，仅在**确认**存在生成器窄描边且做过小样诊断（外圈命中率 ≤30%）时才考虑。
- ❌ 全源统一容差。

---

## 第 7 章 附录

### 7.1 关键路径速查

```
E:\sprite-video-lab\                          checkout
E:\sprite-video-lab-models\venv\               运行时（Python 3.10）
E:\sprite-video-lab-work\                      工作目录（uploads/jobs/exports/previews）
http://127.0.0.1:8894/                         网页版
E:\sprite-video-lab\app\index.html             隐藏参数在第 407–410 行（softness/despill/halo/bifrefnet shrink）

素材源（每个角色一份，在 Unity 工程内但不在 Assets 下）：
  Enemy1011ArtSource\03_SelectedFrames\<动作>\   raw / raw_manifest.json / keyed_*/ / deployed_reference
  Enemy109ArtSource\03_SelectedFrames\<动作>\    同上（Attack/Charging/MountedIdle/Windup）

入库目标：
  Assets\Sprites\Enemy\Enemy1_1\                 1011 精灵（69，已验收，未改动）
  Assets\Sprites\Enemy\Enemy109\                 109 精灵（36：18 替换 + 18 新建）

备份（入库前原图 + deploy_report*.json）：
  Library\Locus\tmp\backup_<对象>_<时间戳>\

脚本（随仓库分发，推荐用它们）：
  Locus\tools\svl-matting\                     通用参数化脚本 + README（受版本控制）
    （本机工作副本在 Library\Locus\tmp\svl_run\，但 Library/ 被 gitignore、不随仓库同步）
```

### 7.2 历史：旧的四步链（已被取代，保留作参考）

2026-10-09 为 1011 设计过一条 `softness=0` + 「描边环替换 / 删源绿 / 中性化 / 硬绿保证」的链，它能把"白底绿像素"做到严格 0，但代价是**被删主体多 1–2 个百分点**（骑兵上甚至是 7.6% vs 4.2%），并且在暗色细结构主体上会被读成"角色被啃掉"。2026-10-10 用户验收后改用本手册的网页版路径。
详细的迭代过程、每一步的实测数字与四变体对照（A/B/C/D）见 `memory/sprite-video-lab-matting-evaluation.md`。

### 7.3 不在本工作流范围内

- **动画 clip / Animator controller 的创建与接线**：本手册只负责“把帧变成透明精灵并入库”，不建 clip、不改 controller。入库后 sprite 是孤立的（未被任何 clip 引用），需要单独做动画部署。
- **具体已知情况（2026-10-10）**：1011 的 10 个 clip 均已存在；109 只有 `Enemy_109_MountedIdle.anim` 引用了 109 的精灵，`Enemy_109.controller` 里 Attack/Dead/HitFlash/Walk/Launched_* 仍指向 `Enemy_101_*` 的 clip（占位）。
- 项目里存在与本工作流无关的既有问题：11 个空 sprite 引用（`Boss_104_QTE_Sweep_Start` 2、`Boss_104_QTE_Swipe` 5、`Enemy_102_CowardDead/Hit/Idle/Launched` 各 1）。

### 7.4 非纯色底（可选，未在本流程使用）

底不是纯色时 Chroma 不适用，可考虑 `BiRefNet`（语义分割）或 `CorridorKey`（绿幕重建+去溢色）：
- 需额外装 torch（约 2.5 GB）与各自权重；UI 选中时会弹确认后下载。
- **CorridorKey 许可**：CC BY-NC-SA 4.0 + Corridor Digital 附加条款——允许"作为商业项目的一部分处理图像"，但禁止重打包/转售、禁止付费推理服务、嵌入商业软件包需单独协议。
- 本机实测 github.com 直连不通，HuggingFace 下载可能也需要镜像。
