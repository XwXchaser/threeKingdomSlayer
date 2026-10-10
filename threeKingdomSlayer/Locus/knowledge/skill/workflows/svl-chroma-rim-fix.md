---
id: kd_ed6c0171-4e5c-48d3-8586-f5836ee7f099
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
skillEnabled: true
skillSurface: command
summary: 'Sprite Video Lab 抠图工作流从零部署与使用手册（2026-10-10 用户验收，含 1021 Idle 4 帧实跑）。配方固定为 Chroma + 自动取背景色 + 容差按底材标定（彩色幕布 70-90 / 中性底 14，或中性底 6+幕布清理） + softness 16 + halo 1 + 只勾半透明像素转不透明；已弃用先做平滑处理（Real-ESRGAN 会重绘细节）与 softness 0。部署全程盘符无关（占位符 <SVL_ROOT>/<SVL_MODELS>/<SVL_WORK>，仅 C 盘可跑），【强制】部署位置先问用户、禁止自动安装 Python/venv/setup_ai_runtime.bat；含拉取工具本体的 git 地址与 HTTPS 443 间歇阻断时改走 SSH、环境变量绕过启动器硬编码的优先级与验证法、服务必须由用户双击启动（agent 起的进程会被回收）、视频抽帧用 ffmpeg 及零安装来源、网页版操作、批量脚本清单、素材源文件夹约定、四项目检验收、Unity 入库规格与逐角色放置核对法，以及踩坑根因表。'
---

# Sprite Video Lab 抠图工作流：从零部署与使用手册

> **用途**：在任意 Windows 主机上，从零把 AI 生成的纯色底视频帧抠成透明精灵，并入库 Unity。本文件是**可独立使用的部署 + 操作手册**（含路径、命令、判据、踩坑），另一台没装过的主机照做即可跑通。
> **当前配方（2026-10-10 用户验收）**：`Chroma` + `自动取背景色` + **容差（按底材标定）** + `softness 16` + `halo 1` + **只勾「半透明像素转不透明」**；**不要勾「先做平滑处理」**。
> **维护方式**：改动必须附「实测数值 + 已验收参照物对照」，不接受只凭描述的结论。
> **相关**：`skill/workflows/local-green-screen-cutout.md`（旧的自有 numpy 路线，保留作对照）；`memory/sprite-video-lab-matting-evaluation.md`（本流程的完整迭代与实测台账）。

---

## 第 0 章 适用范围与一句话配方

**适用**：源素材是**纯色平底**（绿幕/蓝幕/深灰/白底），底色均匀（边框环 std ≤ 2）。角色由 AI 视频生成，常带一层「深色/偏绿描边」。
**不适用**：底不是纯色（真实或 AI 生成的复杂背景）→ 需要语义模型（`BiRefNet`/`CorridorKey`，见第 7.5 节）；需要保留角色本体上的**绿色部件**。

**一句话配方**：`Chroma` → `自动取背景色` → 容差（**按底材标定**）→ 只勾 `半透明像素转不透明`。
`softness` 保持默认 **16**，`halo` 保持默认 **1**。

---

## 第 1 章 从零部署（新主机照做；**先问用户，不要自动装**）

> **本章铁律（2026-10-10 勘误，用户明确要求）**
> 1. 抠图内核 `sprite-video-lab` 是**独立的第三方仓库**，不在本项目里；**拉取地址见 1.0.1**。只拉本项目拿不到工具。
> 2. **装哪个盘、要不要装 Python、要不要建 venv，必须先问用户并得到答复**，再动手。**禁止**自行 `winget install`、自行建 venv、自行跑 `setup_ai_runtime.bat`。
> 3. **禁止自动安装**第 1.6 节的任何可选组件（torch / Real-ESRGAN / BiRefNet / CorridorKey，动辄 2.5–7 GB）。
> 4. 服务**必须由用户双击启动**（原因见 1.4 的“agent 起的进程会被回收”）。

### 1.0 【第一步】先问用户：装哪个盘 / 要不要装 Python

**拿到任务后的第一个动作是向用户提问，不是敲命令。** 至少确认：

| 问题 | 为什么必须问 |
|---|---|
| 部署到哪个盘/目录？ | 文档里的 `E:\...` 只是某台主机的选择。**很多机器根本没有 E 盘**，照抄会撞上 1.4 的全部硬编码陷阱 |
| 本机有没有可用的 Python 3.10？没有的话允不允许我装？ | 装 Python 是**系统级改动**，不应默许。也不要用 3.13/3.14 凑合（见 1.4）|
| 允许在哪个位置建 venv？ | 与上面同一盘最省事 |
| 有没有 ffmpeg？（视频输入时） | 没有就可能要装；但**先找剪映自带**（见 1.4.3），往往零安装就能解决 |

> **实测实例（2026-10-10，本机仅 C 盘）**：用户答复“装到 C 盘，动手”后，实际使用
> `<SVL_ROOT>`=`C:\sprite-video-lab`、`<SVL_MODELS>`=`C:\sprite-video-lab-models`、`<SVL_WORK>`=`C:\sprite-video-lab-work`。
> 这三个名字**只是为了和启动器的 fallback 一致、便于记忆**，不是硬性要求。

### 1.0.1 工具本体（第三方仓库，唯一权威来源）

```
https://github.com/sparklecatta-lang/sprite-video-lab
```

**HTTPS 可能间歇不通**（实测同一地址首次成功、随后连续三次 `Failed to connect to github.com port 443`，`curl` 返回 `000`）；
但 **SSH(22) 通道通常仍可用**。**先探再选，不要因 HTTPS 失败就断定“拉不下来”或去重下**：

```bash
# HTTPS 探针
git ls-remote https://github.com/sparklecatta-lang/sprite-video-lab HEAD
# SSH 探针（可用则改用它克隆）
ssh -o ConnectTimeout=12 -T git@github.com
# SSH 克隆（HTTPS 失败时的替代）
git clone --depth 1 --progress git@github.com:sparklecatta-lang/sprite-video-lab.git <SVL_ROOT>
```

拉取后自检：

```bash
ls <SVL_ROOT>                             # 应看到 server.py / requirements.txt / start_sprite_video_lab.bat / app / tools
cat <SVL_ROOT>/VERSION                    # 实测 0.2.0（HEAD 01603e8）
grep -n "^import cgi" <SVL_ROOT>/server.py   # 应为第 4 行；这是必须 3.10 的根据
```

#### 1.0.2 本项目侧（手册与包装脚本）

本工作流的**手册与全部脚本都随本项目仓库分发**（`Locus/knowledge/skill/workflows/svl-chroma-rim-fix.md` 与 `Locus/tools/svl-matting/`），新主机只需拉取本项目。

```bash
cd <项目目录>                                      # 已有克隆时
git status                                         # 有未提交改动先 commit/stash
git pull origin route-scroll-movement            # 本分支未设 upstream，必须写全（直接 git pull 会报错）
git log -1 --oneline                               # 应看到 docs(tools): 抠图工作流手册 v2 + 参数化脚本…
```

还没有克隆时，按 `plan/two-device-handoff-20261009.md` 第 0 节做（**部分克隆**：`git clone --filter=blob:none git@github.com:XwXchaser/threeKingdomSlayer.git`），**不要手工拷贝 `Assets/`**。

拉取后自检：

```bash
ls Locus/tools/svl-matting/                                  # 应看到 6 个 .py + README.md
python -m py_compile Locus/tools/svl-matting/*.py             # 语法自检（可选）
```

> ⚠️ 本机工作用的脚本副本在 `Library/Locus/tmp/svl_run/`，但 **`Library/` 被 .gitignore 忽略、不随仓库同步**；分发位置只有 `Locus/tools/svl-matting/`。
> ⚠️ **素材与精灵不在仓库里**：`Enemy1011ArtSource/`、`Enemy109ArtSource/`（raw/keyed/已部署参照）与 `Assets/Sprites/...` 下的精灵替换**尚未提交**。要让另一台机器拿到**同样的素材/精灵**，需先在本机提交（素材源目录建议只提交目录结构与 `raw_manifest.json`，帧用 .gitignore 排除），或让那台机器从自己的原始素材重跑一遍。

### 1.0.3 【第一步】先问用户：装哪个盘 / 要不要装 Python

**拿到任务后的第一个动作是向用户提问，不是敲命令。** 至少确认：

| 问题 | 为什么必须问 |
|---|---|
| 部署到哪个盘/目录？ | 文档里的 `E:\...` 只是某台主机的选择。**很多机器根本没有 E 盘**，照抄会撞上 1.4 的全部硬编码陷阱 |
| 本机有没有可用的 Python 3.10？没有的话允不允许我装？ | 装 Python 是**系统级改动**，不应默许。也不要用 3.13/3.14 凑合 |
| 允许在哪个位置建 venv？ | 与上面同一盘最省事 |
| 有没有 ffmpeg？（视频输入时） | 没有就可能要装；但**先找剪映自带**（见 1.4.3），往往零安装就能解决 |

> **实测实例（2026-10-10，本机仅 C 盘）**：用户答复“装到 C 盘，动手”后，实际使用
> `<SVL_ROOT>`=`C:\sprite-video-lab`、`<SVL_MODELS>`=`C:\sprite-video-lab-models`、`<SVL_WORK>`=`C:\sprite-video-lab-work`。
> 这三个名字**只是为了和启动器的 fallback 一致、便于记忆**，不是硬性要求。

### 1.1 前置条件

| 项 | 要求 | 说明 |
|---|---|---|
| 操作系统 | Windows | 只在 Windows 上验证过 |
| Python | **3.10**（3.11/3.12 可用；**3.13+ 不可用**） | `server.py` 顶层 `import cgi`，该模块在 3.13 被移除 |
| git | 任意版本 | 取代码用；也可直接拷目录 |
| ffmpeg / ffprobe | **抽帧必需**（视频→PNG）；导出透明 MOV / GIF 也要 | **不在 PATH 上不等于没有**，先看 1.4.3 的零安装来源 |
| 磁盘 / 路径 | 纯 Chroma 约 **100 MB**；**任意盘，不需要 E:** | 代码 ~9 MB + venv ~87 MB（**AI 路线要再 +2.5–7 GB**，见 1.6）。本手册里的 `<SVL_ROOT>` / `<SVL_MODELS>` / `<SVL_WORK>` 都是**占位符**，换成你机器上的真实路径（例：`C:\sprite-video-lab`）。**只有 C 盘也能完全跑通**，见 1.4 的"C 盘单盘示例" |

### 1.2 取代码

```powershell
# <SVL_ROOT> = 你要放工具的位置，任意盘。例：C:\sprite-video-lab 或 E:\sprite-video-lab
git clone https://github.com/sparklecatta-lang/sprite-video-lab <SVL_ROOT>
```
若 GitHub 直连不通：换可用的镜像/代理，或直接从有该目录的主机拷贝整个 checkout（见 1.7）。

> **本手册里的 `<SVL_ROOT>` / `<SVL_MODELS>` / `<SVL_WORK>` 都是占位符**，替换成你机器上的真实路径即可；**与磁盘盘符无关，只有 C 盘也能跑**（见 1.4 的 C 盘单盘示例）。

### 1.3 建运行时

```powershell
py -3.10 -m venv <SVL_MODELS>\venv
<SVL_MODELS>\venv\Scripts\python.exe -m pip install --upgrade pip
<SVL_MODELS>\venv\Scripts\python.exe -m pip install -r <SVL_ROOT>\requirements.txt
# 可选：让仓库自带单元测试全绿（否则 2 个 AI 下载测试会报 ModuleNotFoundError）
<SVL_MODELS>\venv\Scripts\python.exe -m pip install huggingface_hub
```
**判据**：`<SVL_MODELS>\venv\Scripts\python.exe -c "import cgi, PIL; print('ok')"` → 输出 `ok`。
> 基础依赖只有 Pillow；torch 等 AI 依赖**本工作流不需要**，不要装。

### 1.4 工作目录与启动

**路径全部可自定，不依赖任何特定盘符**（下面用变量表示，换成你的真实路径即可）：

| 用途 | 变量 | 示例（C 盘单盘） |
|---|---|---|
| 代码 checkout | `<SVL_ROOT>` | `C:\sprite-video-lab` |
| 运行时（venv） | `<SVL_MODELS>\venv` | `C:\sprite-video-lab-models\venv` |
| 工作目录（上传/任务/预览/导出） | `<SVL_WORK>` | `C:\sprite-video-lab-work` |
| 端口 | — | `127.0.0.1:8894` |

> 启动器 `start_sprite_video_lab.bat` 只在**检测到 `E:\` 存在**时才使用 `E:\sprite-video-lab-work` / `E:\sprite-video-lab-models\venv`；**没有 E 盘的机器上它不会用 E 盘**，但也不会自动找到你自建的 venv——所以必须设 `SPRITE_VIDEO_LAB_PYTHON`（下面）。

**启动（推荐）：先设两个环境变量，再双击启动器**

```powershell
set SPRITE_VIDEO_LAB_PYTHON=<SVL_MODELS>\venv\Scripts\python.exe   # 关键：告诉启动器用哪个 Python（必须 3.10）
set SPRITE_VIDEO_LAB_WORK_DIR=<SVL_WORK>                            # 可选；不设则用 <SVL_ROOT>\work
set SPRITE_VIDEO_LAB_ROOT=<SVL_ROOT>                                # 给批量脚本用（第 3 章）；设了就必须指向含 server.py 的目录，否则脚本会直接报错
```
> 三个变量的优先级：`SPRITE_VIDEO_LAB_ROOT` 设了就以它为准；不设时脚本按 `E:\sprite-video-lab` → `C:\sprite-video-lab` → 当前目录及其上级 依次探测第一个含 `server.py` 的目录。

双击 `<SVL_ROOT>\start_sprite_video_lab.bat`
它会：杀掉旧 server → 用 `SPRITE_VIDEO_LAB_PYTHON` 起 `server.py --serve` → 打开浏览器。
> ⚠️ **不设 `SPRITE_VIDEO_LAB_PYTHON` 时**，启动器会退到 PATH 里的 `python`/`py`——若那是 3.13+，服务会因 `cgi` 缺失而起不来（这是新主机最常见的坑）。

#### 1.4.1 【强制】服务必须由**用户**双击启动

> ⚠️ **实测限制（2026-10-10）**：agent 的工具调用结束时会**连带回收它拉起的进程**。
> 无论用 `start` / `&` / 后台任务，agent 起的 server 在工具返回后就死掉（实测 `curl` 回 `000`、端口空闲）。
> **所以：agent 不要尝试替用户启动服务，把启动交给用户。**

用户启动后，agent 可以做“启后验证”（读的是已存在的进程）：

```bash
curl -s -o /dev/null -w "%{http_code}\n" http://127.0.0.1:8894/          # 期望 200
curl -s http://127.0.0.1:8894/api/runtime-info | head -c 400
```

期望 `runtime-info` 里：`python_executable` 指向你建的 3.10 venv，`work_dir` = 你在上面设的 `<SVL_WORK>`，`torch.installed` = **false**（false 是**正确**的）。

#### 1.4.2 环境变量怎么写得可靠

- **不要用 `setx ... "C:\path"`**：实测两次分别写入了**字面引号**（`"C:\..."`）和被 shell 吃掉反斜杠（`C:sprite-video-lab`），两次都是错的。要么用无引号且路径无空格的 `setx`，要么用 Python `winreg` 直接写 HKCU\Environment（最稳，附 WM_SETTINGCHANGE 广播）。
- **验证必须用新开的 cmd 读**（当前 shell 不会自动刷新）：

```bat
@echo off
if not "%SPRITE_VIDEO_LAB_PYTHON%"=="" if exist "%SPRITE_VIDEO_LAB_PYTHON%" (
  echo LAUNCHER-WOULD-USE: %SPRITE_VIDEO_LAB_PYTHON%
  "%SPRITE_VIDEO_LAB_PYTHON%" -V
  "%SPRITE_VIDEO_LAB_PYTHON%" -c "import cgi; print('cgi ok')"
  goto :done
)
echo LAUNCHER-WOULD-FALLTHROUGH-TO: where python  ^<-- BAD
:done
```

期望：打印 3.10.x + `cgi ok`，**走不到** `<-- BAD` 那行。

#### 1.4.3 抽帧用的 ffmpeg（视频输入时必需）

**ffmpeg 不在 PATH 上，不等于本机没有。** 实测可用的零安装来源：

| 来源 | 路径形态 | 备注 |
|---|---|---|
| 剪映（JianyingPro）自带 | `%LOCALAPPDATA%\JianyingPro\Apps\<ver>\ffmpeg.exe` | 实测可直接抽帧；但该构建 **`--disable-ffprobe`**，没有 ffprobe |
| 其它剪辑/转码软件 | — | 同样先找找 |
| 用户显式指定的目录 | 环境变量 `SPRITE_VIDEO_LAB_FFMPEG_DIR` | SVL 自己的解析顺序见 `server.py: resolve_ffmpeg_binary` |

```bash
# 找一找（很快）
find "$LOCALAPPDATA/JianyingPro" -name "ffmpeg.exe" 2>/dev/null | head -1
# 验证能用
"<found ffmpeg>" -hide_banner -i "<video>" 2>&1 | grep -iE "Duration|Stream"
# 抽全部帧（不重采样、不缩放）
"<found ffmpeg>" -hide_banner -v error -i "<video>" -vsync 0 "<out>/clean_raw/f%03d.png"
```

> 若确实没有 ffmpeg：**先问用户**是否允许安装或由用户指定已有路径；不要自行下载。

**C 盘单盘示例（可直接照抄）**

```powershell
py -3.10 -m venv C:\sprite-video-lab-models\venv
C:\sprite-video-lab-models\venv\Scripts\python.exe -m pip install -r C:\sprite-video-lab\requirements.txt
set SPRITE_VIDEO_LAB_PYTHON=C:\sprite-video-lab-models\venv\Scripts\python.exe
set SPRITE_VIDEO_LAB_WORK_DIR=C:\sprite-video-lab-work
set SPRITE_VIDEO_LAB_ROOT=C:\sprite-video-lab
cd /d C:\sprite-video-lab
start_sprite_video_lab.bat
```

**启动（手动，想看日志时）**：
```powershell
cd /d <SVL_ROOT>
set SPRITE_VIDEO_LAB_WORK_DIR=<SVL_WORK>
<SVL_MODELS>\venv\Scripts\python.exe server.py --serve --host 127.0.0.1 --port 8894
```
**判据**：浏览器打开 `http://127.0.0.1:8894/`，出现三段式面板，页脚状态为「等待导入素材」。
> 手动启动时可以**不设** `SPRITE_VIDEO_LAB_WORK_DIR`，此时工作目录默认为 `<SVL_ROOT>\work`（能用，只是产物在 checkout 里）。

### 1.5 首次自检清单

| 检查 | 命令 / 动作 | 期望 |
|---|---|---|
| 服务在听 | 浏览器开 `http://127.0.0.1:8894/` | 200 + 三段式页面 |
| 运行时正确 | 页面里 `runtime-info`（或看启动窗口） | `work_dir` = 你在 1.4 里设的 `<SVL_WORK>`（未设时为 `<SVL_ROOT>\work`） |
| 无重复实例 | `Get-CimInstance Win32_Process \| Where-Object { $_.Name -like 'python*' -and $_.CommandLine -like '*--serve*' }` | 只有 1 个 |
| 单测 | `<SVL_MODELS>\venv\Scripts\python.exe -m unittest tests.test_ai_matte_sizing`（在 checkout 下） | 63 passed（装了 huggingface_hub） |

### 1.6 可选组件（本工作流**不需要**；**禁止自动安装**）

- **Real-ESRGAN（"先做平滑处理"）→ 已弃用**：它是生成式重建，会把角色细节重绘（实测每帧约 8.5 万像素颜色被改），仅在想要"极致干净的边缘"时短期使用。**不要装、不要勾**。
- **BiRefNet / CorridorKey**：仅在**底不是纯色**时才需要，要额外装 torch（约 2.5 GB）+ 各自权重，且 CorridorKey 有许可边界（见 7.5）。
- **`setup_ai_runtime.bat` → 默认不要跑。** 实测（2026-10-10）两个致命问题：
  1. 它的 `AI_ROOT` 在无 E 盘时 fallback 到 **`<SVL_ROOT>\work\models`**，会建**第二个 venv**，且用的是 `where py` 找到的 **3.14**——**连 `cgi` 都没有，跑不了服务端**。
  2. `requirements-ai.txt` 里的 `numpy<2` 需要现场编译（实测失败：`Compiler cl cannot compile programs`），后面的 scipy / transformers / opencv 全没装上，但它仍会打印 **“AI runtime is ready”**（**不检查 pip 退出码**）。实测白吃 **约 6.9 GB**。
  3. 该脚本卡在末尾时会**持续锁住 checkout 目录**（进程 cwd 在那里），导致删目录报“另一个程序正在使用此文件”——需先结束该 `cmd.exe` 进程。
- **若用户确实需要 AI 路线**：先把 `SPRITE_VIDEO_LAB_AI_MODEL_CACHE` 等变量指到用户同意的位置，并向用户确认磁盘代价后再执行；不要默许。

### 1.7 从旧机器要带来的东西（可选）

1. **本手册**（`Locus/knowledge/skill/workflows/svl-chroma-rim-fix.md`）。
2. **批量/度量脚本**：**就用仓库里的 `Locus/tools/svl-matting/`**（已提交：`svl_common.py` / `svl_matte_batch.py` / `svl_scan_tolerance.py` / `svl_build_source.py` / `svl_deploy_to_unity.py` / `green_guarantee.py` + `README.md`）。**新主机只要拉这个仓库就有，不需要改代码**。
   ⚠️ 本机的工作副本在 `Library/Locus/tmp/svl_run/`，而 **`Library/` 被 .gitignore 忽略、不会随仓库同步**——所以别把它当成分发位置。项目内旧的一次性脚本（如 `enemy1011_run_new.py`）也在那儿，仅作参考。
3. **度量脚本的依赖**：度量 helper（`ero/dil/reach_border` 等）**已内联在 `Locus/tools/svl-matting/svl_common.py`**，不依赖任何项目内旧脚本。脚本额外要 `numpy` + `Pillow`（不在基础 venv 里时 `pip install numpy pillow`）。
4. **SVL 源码怎么到新机（仅 C 盘时）**：若 GitHub 直连不通，直接把机器 A 的整个 `sprite-video-lab` 目录拷过去（只需代码，**别拷 `work/`**）；**venv 不要拷**（`pyvenv.cfg`/Scripts 里写死了绝对路径），到新机用 `py -3.10 -m venv` 重建。

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

- 定好参数后：选片段区间 → 点「**开始处理区间**」（写盘到 `<SVL_WORK>\jobs\<job>\processed\`）。
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
# 先定义两个变量（换成你机器上的真实路径）
$PY  = "<SVL_MODELS>\venv\Scripts\python.exe"     # 例 C:\sprite-video-lab-models\venv\Scripts\python.exe
$RUN = "Locus\tools\svl-matting"                 # 通用脚本（随仓库分发）

# 绿幕批量（配方 = Chroma 自动取色 + T + softness16 + halo1 + 半透明转不透明）
$PY $RUN\svl_matte_batch.py --src "<某帧目录或素材根>"
# 容差扫描与推荐
$PY $RUN\svl_scan_tolerance.py --src "<某动作目录>" --tolerances 55,70,79
# 单点去绿工具（不改 alpha，带 --dry-run 与备份）
$PY $RUN\green_guarantee.py <目录> --dry-run
```
> 跑脚本时按需设 `SPRITE_VIDEO_LAB_ROOT=<SVL_ROOT>`（不设则**自动探测**，见 3.3）与 `SPRITE_VIDEO_LAB_WORK_DIR=<SVL_WORK>`。
>
> **解释器**：批量/度量脚本里已内置 `sys.modules.setdefault("cgi", ...)` 兼容处理，**用 3.10 venv 或 3.13+ 都能跑**；但**服务端（`server.py`）必须 3.10**。
> **依赖**：脚本用 `numpy`（度量）+ `Pillow`；两者都不在 `requirements.txt` 里时要手动装：`pip install numpy pillow`。

#### 3.2.1 通用参数化脚本（推荐，零改代码）

`Locus/tools/svl-matting/svl_*.py`（随仓库分发，受版本控制）：全部走命令行参数，**不需要改代码**。

```powershell
$PY  = "<SVL_MODELS>\venv\Scripts\python.exe"    # 例 C:\sprite-video-lab-models\venv\Scripts\python.exe
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
- 共性：`SPRITE_VIDEO_LAB_ROOT` 指定 checkout（**不设则自动探测**：`E:\sprite-video-lab` → `C:\sprite-video-lab` → 当前目录）；度量 helper（`ero/dil/reach_border`）**已内联在 `svl_common.py`**，不再依赖任何项目脚本。

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

> **新建精灵会拿到新的 GUID**（因为此前无人引用，安全）；但**它还没有任何动画 clip 引用它**——接线是另一件事（见 7.4）。

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
| GitHub 拉不动 | **HTTPS 443 会被间歇阻断**（实测同一地址首次成功、随后连续三次 `Failed to connect to github.com port 443`，`curl` 返回 `000`） | **先探再选**：`git ls-remote <https> HEAD` 对比 `ssh -o ConnectTimeout=12 -T git@github.com`；**SSH(22) 通常仍通**，改用 `git@github.com:...` 克隆即可。不要因 HTTPS 失败就断定拉不下来、也不要重复下载 |
| Python 3.13+ / 3.14 起不来 | `cgi` 模块已被移除 | 用 **3.10** |
| **双击 .bat 后服务没起来** | 启动器第 22 行的 E: 探测失效 → 回退到 `where python`，选中了 PATH 上无 `cgi` 的 3.13/3.14 | 设 `SPRITE_VIDEO_LAB_PYTHON` 指向 3.10 venv（见 1.4）|
| **agent 起的服务马上死掉** | agent 工具调用结束时会回收其拉起的进程树（`start` / `&` / 后台任务都一样）| **服务必须由用户双击启动**；agent 只做“启后验证”（见 1.4.1）|
| **`setup_ai_runtime.bat` 白吃约 6.9 GB 且装坏了** | ①无 E 盘时 `AI_ROOT` fallback 到 `<SVL_ROOT>\work\models` 且用 3.14（无 `cgi`）；②`numpy<2` 需现场编译失败，但它**不检查 pip 退出码**，仍打印 “AI runtime is ready”；③卡住时会锁住 checkout 目录 | **默认不要跑**（见 1.6）；要跑先征得用户同意并指好目录 |
| `setx` 写环境变量写坏 | `setx VAR "C:\path"` 会写入**字面引号**；经 shell 转递时反斜杠被吃掉（`C:sprite-video-lab`）| 用 Python `winreg` 写；或用无引号且路径无空格的 `setx`（见 1.4.2）|
| 视频抽帧没工具 | 本机 PATH 没有 ffmpeg，但**不等于没有** | 先找剪映自带（`%LOCALAPPDATA%\JianyingPro\Apps\<ver>\ffmpeg.exe`，实测可用）；确实没有再问用户（见 1.4.3）|
| **新建的精灵在游戏里不出现/不动** | 新精灵是新增资产，**没有任何 clip 引用它**（本工作流不接线） | 单独做动画部署（见 7.4） |

### 附：已弃用的做法（不要重复尝试）

- ❌ `softness = 0`（硬边）：会关掉反混合（`C'=(C−(1−α)B)/α` **只在 0<α<254 生效**）。
- ❌ 「先做平滑处理」（Real-ESRGAN）：重绘细节。
- ❌ 脚本后处理链（描边环替换 / 删源绿 `d>8` / 中性化 / 硬绿保证）：默认关闭，仅在**确认**存在生成器窄描边且做过小样诊断（外圈命中率 ≤30%）时才考虑。
- ❌ 全源统一容差。

---

## 第 7 章 附录

### 7.1 关键路径速查

```
<SVL_ROOT>\                          checkout（任意盘；例 C:\sprite-video-lab）
<SVL_MODELS>\venv\                   运行时（Python 3.10；例 C:\sprite-video-lab-models\venv）
<SVL_WORK>\                          工作目录（uploads/jobs/exports/previews；未设则为 <SVL_ROOT>\work）
http://127.0.0.1:8894/               网页版
<SVL_ROOT>\app\index.html             隐藏参数在第 407–410 行（softness/despill/halo/bifrefnet shrink）

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

### 7.2 本次端到端实例（1021 Idle 4 帧，2026-10-10 用户验收）

**这是本工作流在一台全新主机（仅 C 盘）上跑通的完整链路**，可作为下一次的参照模板。

| 步 | 实际做了什么 | 结果 |
|---|---|---|
| 1 | 问用户部署位置 → 答“C 盘” | `<SVL_ROOT>`=`C:\sprite-video-lab`，`<SVL_MODELS>`=`C:\sprite-video-lab-models`，`<SVL_WORK>`=`C:\sprite-video-lab-work` |
| 2 | HTTPS 克隆失败 → 改 SSH | `git clone git@github.com:sparklecatta-lang/sprite-video-lab.git`，HEAD `01603e8`、VERSION 0.2.0 |
| 3 | 装 Python 3.10.11 + venv（Pillow 12.3.0 + numpy 2.2.6） | `import cgi` → ok |
| 4 | 写 4 个环境变量（winreg） | 启动器解析测试：选中 3.10.11 且 `cgi ok`，未走 `where python` 回退 |
| 5 | **用户双击** `start_sprite_video_lab.bat` | HTTP 200、`work_dir` 正确；`torch.installed=false`（正确） |
| 6 | 抽帧（剪映自带 ffmpeg，零安装） | 97 帧 `960×960 / 24fps / 4.04s`，完整解码无错 |
| 7 | 量化选帧（头顶 ymin 作为呼吸信号） | f001=230 → f038–62=208（峰值平台）→ f097=230；**整段即一个完整呼吸周期**，首尾闭合 |
| 8 | 取 4 帧（四分之一相位） | **f001 / f025 / f049 / f073** |
| 9 | `svl_scan_tolerance.py` | 绿幕 `(0,172,57)`，彩色幕布，T 不敏感；取 T=79 |
| 10 | `svl_matte_batch.py --tolerance 79 --sheet` | 4 帧 RGBA 960×960 |

**验收数据（实测）**：

| 指标 | 结果 |
|---|---|
| 半透明像素（硬门） | **0** |
| 幕布残留 / 封闭孔 | 0 / 0 |
| 被删主体 | 11.4%（1011 基线 10–27%） |
| 白底可见绿 gEx>8 | 661（1011 基线 ≤2k） |
| 薄带占比 / 周长面积比 | 0.026 / 0.052（与 1011 同量级） |
| **脚线漂移（4 帧）** | **0 px**（均为 779；源帧 781） |
| x 中心跨度 | 1.5 px |

**选帧方法可复用**：用「头顶 ymin（或主体像素数）逐帧曲线」定位呼吸周期与峰值平台，再按四分之一相位取帧；**不要对整段机械等距取帧**。

**产物路径**：
```
Library\Locus\tmp\enemy1021_idle_v1_frames\keyed_ui_t79\idle1_f001.png … idle4_f073.png
Library\Locus\tmp\enemy1021_idle_v1_frames\DELIVERABLE_1021_idle_4frames.png   （上排白底查灰边 / 下排棋盘格查 alpha）
Library\Locus\tmp\enemy1021_idle_v1_frames\clean_raw\f001.png … f097.png       （原始帧，未裁切）
```

### 7.3 历史：旧的四步链（已被取代，保留作参考）

2026-10-09 为 1011 设计过一条 `softness=0` + 「描边环替换 / 删源绿 / 中性化 / 硬绿保证」的链，它能把"白底绿像素"做到严格 0，但代价是**被删主体多 1–2 个百分点**（骑兵上甚至是 7.6% vs 4.2%），并且在暗色细结构主体上会被读成"角色被啃掉"。2026-10-10 用户验收后改用本手册的网页版路径。
详细的迭代过程、每一步的实测数字与四变体对照（A/B/C/D）见 `memory/sprite-video-lab-matting-evaluation.md`。

### 7.4 不在本工作流范围内

- **动画 clip / Animator controller 的创建与接线**：本手册只负责“把帧变成透明精灵并入库”，不建 clip、不改 controller。入库后 sprite 是孤立的（未被任何 clip 引用），需要单独做动画部署。
- **具体已知情况（2026-10-10）**：1011 的 10 个 clip 均已存在；109 只有 `Enemy_109_MountedIdle.anim` 引用了 109 的精灵，`Enemy_109.controller` 里 Attack/Dead/HitFlash/Walk/Launched_* 仍指向 `Enemy_101_*` 的 clip（占位）。
- 项目里存在与本工作流无关的既有问题：11 个空 sprite 引用（`Boss_104_QTE_Sweep_Start` 2、`Boss_104_QTE_Swipe` 5、`Enemy_102_CowardDead/Hit/Idle/Launched` 各 1）。

### 7.5 非纯色底（可选，未在本流程使用）

底不是纯色时 Chroma 不适用，可考虑 `BiRefNet`（语义分割）或 `CorridorKey`（绿幕重建+去溢色）：
- 需额外装 torch（约 2.5 GB）与各自权重；UI 选中时会弹确认后下载。
- **CorridorKey 许可**：CC BY-NC-SA 4.0 + Corridor Digital 附加条款——允许"作为商业项目的一部分处理图像"，但禁止重打包/转售、禁止付费推理服务、嵌入商业软件包需单独协议。
- 本机实测 github.com 直连不通，HuggingFace 下载可能也需要镜像。
