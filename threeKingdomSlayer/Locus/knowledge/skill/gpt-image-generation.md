---
id: kd_e6815137-b73f-41d7-b6ad-d76834c73ab0
injectMode: inherit
summary: 需要通过 GPT Image 接口文生图、图生图、多图融合或语义去背并落盘交付时使用；覆盖模型、质量档位、透明参数、调用模板与排错，任务编排与透明验收见 workflow。
aiEditMode: inherit
skillEnabled: true
skillSurface: command
commandTrigger: /gpt-image
argumentHint: "<prompt> [--image path ...] [--size 1024x1024|1024x1536|1536x1024] [--quality low|medium|high|xhigh|max|auto (project default: max)] [--out path]"
---

# gpt-image-generation

脱离 Unity 使用 GPT Image 图像生成接口的使用文档：说明调用方式、模型与质量档位、必需配置、默认输出目录、透明参数、示例、接口规则和常见问题。

## 使用说明

`/gpt-image` 用于脱离 Unity Editor 调用 GPT Image 图像生成接口，**当前默认使用 `gpt-image-2.6-sunburst`**（2026-11-08 起；探针实测 2.6 可用）。Unity 断开、未启动、或项目不在 Play Mode 都不影响使用。

支持三类任务：

- 文生图：只提供文字 prompt，生成新图片。
- 单图编辑：提供 1 张参考图，要求替换背景、改风格、保留主体等。
- 多图融合：提供多张图片，把元素、风格或构图融合成一张图。

默认输出目录：

```text
<当前 Windows 用户目录>/Pictures/gptGen/
```

例如：

```text
C:/Users/Administrator/Pictures/gptGen/
```

如果用户显式提供 `--out`，优先保存到指定路径。

## 使用者需要提供什么

### 必需

1. 生成需求 / prompt
   - 描述要生成或编辑的画面。
   - 可以直接写中文。
   - 如果需要图中文字，把文字内容明确写进 prompt。

2. API Key
   - 环境变量名必须是：`MUSK_API_KEY`
   - 不要把 Key 写进项目文件、聊天总结、知识库或提交到 Git。

### 图生图 / 多图融合额外需要

- 输入图片路径。
- 单图编辑提供 1 张图片。
- 多图融合提供 2 张或更多图片。

### 可选

- 输出尺寸：`1024x1024`、`1024x1536`、`1536x1024`；当前模型还可按接口能力使用满足约束的自定义尺寸
- 画质：`low`、`medium`、`high`、`xhigh`、`max`、`auto`
- 输出路径：通过 `--out` 指定
- 图生图保真度：`input_fidelity=high` 或 `low`
- 透明背景：对已验证支持的模型/接口使用 `background=transparent`，并同时使用 `output_format=png` 或 `webp`

## 如何设置

### 1. 设置 API Key

Windows PowerShell：

```powershell
setx MUSK_API_KEY "你的key"
```

设置后需要重新打开终端 / Locus，使新环境变量生效。

临时只在当前终端使用：

```powershell
$env:MUSK_API_KEY="你的key"
```

Git Bash：

```sh
export MUSK_API_KEY="你的key"
```

### 2. 确认默认输出目录

默认保存到当前 Windows 用户的 `Pictures/gptGen/` 目录；本次任务按用户指定路径保存到：

```text
C:/Users/Administrator/Pictures/gptGen/
```

执行时会自动创建目录。若要换目录，在请求里说明，或使用：

```text
--out C:/Users/steam/Desktop/result.png
```

## 调用示例

### 文生图

```text
/gpt-image 生成一张三国武将卡牌立绘，红黑配色，厚涂风格，金色边框，1024x1536
```

### 指定输出路径

```text
/gpt-image 生成一个像素风铜钱道具图标 --out C:/Users/Administrator/Pictures/gptGen/coin.png
```

### 单图编辑

```text
/gpt-image 参考 C:/Users/Administrator/Pictures/input/hero.png，保持人物不变，把背景改成三国战场夕阳氛围 --out C:/Users/Administrator/Pictures/gptGen/hero_battle.png
```

### 多图融合

```text
/gpt-image 融合 C:/a.png 和 C:/b.png，把第二张的武器自然加入第一张角色手中，光影统一 --out C:/Users/Administrator/Pictures/gptGen/fusion.png
```

## 接口规则

### 文生图

Endpoint:

```text
POST https://api.muskapis.com/v1/images/generations
```

协议：`application/json`

必填字段：

- `model`: **当前默认 `gpt-image-2.6-sunburst`**（2026-11-08 用户指定并探针实测）；模型名必须带 `-sunburst` 后缀，不带后缀的写法一律 403
- `prompt`

常用可选字段：

- `size`
- `quality`
- `output_format`
- `output_compression`

### 单图编辑 / 多图融合

Endpoint:

```text
POST https://api.muskapis.com/v1/images/edits
```

协议：`multipart/form-data`，不能用 JSON。

必填字段：

- `model`: **当前默认 `gpt-image-2.6-sunburst`**（2026-11-08 起）；`gpt-image-2.5-sunburst` 的可用性随路由变化，用前须实测
- `prompt`
- `image[]`

多图融合时重复传多个 `image[]`。

常用可选字段：

- `size`
- `quality`
- `input_fidelity`
- `background`
- `output_format`
- `n`

### 质量档位与透明参数

当前 GPT Image 模型（`gpt-image-2.6-sunburst` 等）文档列出的 `quality` 枚举为：

```text
low | medium | high | xhigh | max | auto
```

- `auto` 是模型默认档位，由模型自动选择。
- 当前项目的 GPT Image 请求（文生图、单图编辑、多图融合、去背）一律使用 `quality=max`，包括候选和探针；未经用户明确要求，不降档到 `high`、`medium` 或 `low`。
- `xhigh` 和 `max` 是当前模型较高的质量档位；项目默认固定采用最高已知档 `max`（切到 2.6 后 `quality=max` 保持不变，其余参数也保持不变）。
- 质量档位不是姿态锁定或角色身份锁定开关。`max` 可能带来更丰富的重绘细节，也可能改变局部轮廓；参考图编辑仍需使用明确的不可改变约束和 `input_fidelity=high`。`quality=max` 与 `input_fidelity=high` 是两个独立参数，可同时使用。
- 早期 GPT Image 模型最高支持到 `high`，不能把 `max` 自动外推给旧模型。
- `size`、`quality` 和 `background` 在当前文档中支持 `auto`；透明输出应显式设置 `background=transparent`，并使用支持 Alpha 的 `png` 或 `webp`。
- `input_fidelity` 仅用于编辑/融合任务；它不能替代视觉验收。

### GPT Image 语义去背（复杂灰底/角色边缘）

当原图背景与角色灰色铠甲、剑刃或暗色轮廓相近，传统阈值抠图产生明显脏边时，可以使用 `/v1/images/edits` 单图编辑做语义去背，避免直接否定已验收视频或要求重新生成视频。

已实际验证的历史单帧参数组合（当时使用 `high`；仅记录历史事实，不作为当前项目默认）：

```text
model: gpt-image-2.5-sunburst
quality: high
input_fidelity: high
size: 与原帧相同（本次为 960x960）
background: transparent
output_format: png
image[]: 从视频解码得到的原始 RGB 帧
```

- 本次 `f000` 测试返回 HTTP 200 和 960×960 RGBA，用户明确认可去背效果；浅色背景上的灰色脏边明显少于先前的手动去背结果。
- 本测试历史上使用 `high`。后续 GPT Image 请求（包括语义去背探针及批处理）使用 `quality=max`；不把历史参数当作降档依据。
- Prompt 必须限定为 `ONLY background removal and alpha-matte cleanup`，要求保留画布、角色位置、绘制比例、剑/手/脚坐标、配色、亮度和高光；禁止换姿态、增强材质和重新构图。
- 只上传无标注、未抠图的实际帧；不要把含棋盘格的预览或已经有脏边的去背结果作为唯一输入。
- 同尺寸输出不等于同像素坐标。模型仍可能放大、移位或重绘，`input_fidelity=high` 不构成硬性像素锁定。
- 用户认可单帧去背质量是成功结论；动画锚点、逐帧比例和循环连续性是另一项验收，不能混为“抠图失败”，也不能自动认为整套动画已经通过。
- Alpha 最大值 254、没有 Alpha=255，不应直接判为失败。Alpha 253/254 对应约 99.2%/99.6% 不透明度；需要查看主体 Alpha 分布和浅/深底合成效果，而不是把所有小于 255 的像素一概当作明显半透明。真正需要排查的是低 Alpha 主体、外部光晕和边缘污染，不擅自做 Alpha 归一化。
- 先测试一帧，保存版本化图片、完整 Prompt 和原始响应，获认可后再处理其余帧；不得直接批量收费试错。
- 原生透明能力随路由变化：同一份提交（`/v1/images/edits` + multipart `image[]` + `background=transparent` + `output_format=png` + 同一参数组 + 逐字相同 Prompt）实测可分别得到原生 RGBA 与 `color_type=2`(RGB)、棋盘格被画进像素两种结果。对失败结果追加“禁止画棋盘格、必须真实 alpha”的 Prompt 条款经验证无效，透明失败应优先换时间/等路由变化重试，而不是堆 Prompt。
- 该接口是**多后端路由**：实测至少 3 套后端，各有自己的 CDN host、响应字段与 token 记账 — G1（`cdn.jd23kjs.work`、无回显、in 833 / out 4153）、G2（`img.zxai.us`、回显含 `background`、in 870 / out 1683）、B（`r2.52image.xyz`、回显不含 `background`、in 1445–1513 / out 6732）。**G1/G2 都返回原生 RGBA，B 一定返回无 alpha 的 RGB**（累计 G 6/6、B 10/10，无例外）。
- 已用 PNG 容器证据排除“CDN 事后把 alpha 压平”：好/坏两种输出都带生成器特征辅助 chunk `caBX`（若经中间层重编码该 chunk 会丢失），且坏输出的背景是**带噪声的近白**（边框值集中在 240–254，恰好 255 仅占 4.75%–12.45%）→ 是模型自己画了底，不是透明被填白。
- B 后端的行为更像**降级回退**（首选后端忙/不可用时顶上）：G/B 会成段交错出现（相隔 32 秒即可能切换），与请求速率无关，失败时仍能出图，只是不执行 `background=transparent`。
- 实操：请求后先看回显与 token 指纹判断拿到哪套后端；拿到 B 时**不要连发**（同一时段大概率仍是 B），隔一段时间再试。
- 可用模型可用**免费** `GET https://api.muskapis.com/v1/models` 查询（不生成图、不计费）：**2026-11-08 探针时令牌只列出 `gpt-image-2.6-sunburst` 一个图像模型**（其余为 `claude-haiku-5-5`、`gpt-6-astra`、`gpt-6-luna`、`gpt-6.1-sol`、`gpt-6.1-terra`、`grok-4.7`）；`gpt-image-2.5-sunburst` 当天已不在列表。请求无 `-sunburst` 后缀或 `gpt-image-2.5-flare` 一类名字返回 **HTTP 403 “This token has no access to model”**，已实测，不要再把它们当可用候选。
- 结论：“透明能力”不取决于模型名，而是取决于**本次落到哪套后端**；同一 `sunburst` 在 G1/G2 上能给出原生 alpha，在 B 上一定不给。
- 机器验收第一步永远是读 PNG Color Type 或 IHDR：`2`=RGB、`6`=RGBA。查看器或预览图里显示的“透明格子”不能作为透明度证据。
- 用 Alpha 直方图求最大值时注意偏移：`enumerate(reversed(hist))` 的下标 `i` 对应 Alpha 值 `254 - i`，直接取 `i` 会把 max=254 误报成 1，看起来像“整张全透明”的假故障。正确写法：`next((254 - i for i, c in enumerate(reversed(hist)) if c), None)`。
- 棋盘格判定方法：取背景区域一行的明暗游程看是否周期性交替（本案例约 16/17px），再查背景灰度直方图是否只有两级（本案例 245/254）。命中即判为画入棋盘格，属于二级验收明令禁止的失败模式。
- 边缘与光晕比对已认可基线而非绝对值：本案例已认可的 f000 为 1px 环亮度比 0.58、2px 0.79、3px 0.86，后续输出只要同量级即可，不能因为不等于 1.0 判不合格。
- 几何漂移必须记录并与原帧比较：本案例输出相对原帧主体放大 26%–32%、底边下移 93–98px，且不同帧幅度不同；单帧可用不等于动画序列可用。

配套流程和提示词模板见 `skill/workflows/image-asset-generation.md` 的“GPT Image 语义去背”分支；透明帧落地进 Unity 的尺寸/密度约定（PPU 16 全项目统一、按原生像素补边 + 单位 scale 补偿）见同一文档的「透明素材部署进 Unity 的尺寸与密度约定」。一次性文件、数值和验收结论记录在 `memory/video-generation-case-log.md`。

## 配套工作流

完整的任务编排、透明背景验收、失败处理和交付清单请读取：

`skill/workflows/image-asset-generation.md`

本 Skill 仅负责 API 能力、参数规则和调用模板；不会因为读取文档而自动调用 API，必须由当前对话根据用户授权实际执行。

### 能力边界补充：透明和模型能力必须按本次路由实测

不能只凭模型名、PNG 后缀或历史失败结果判断能力。此前同模型编辑接口曾在 `background=transparent` 下返回 HTTP 400，但本次当前路由已实际验证成功：

- 模型：`gpt-image-2.5-sunburst`
- endpoint：`POST /v1/images/edits`
- 参数：`quality=max`、`input_fidelity=high`、`size=1024x1024`、`output_format=png`、`background=transparent`
- 结果：HTTP `200`，图片实际为 `1024×1024 RGBA`，PNG Color Type `6`，四角 Alpha 为 `0`
- 实际结果：`C:/Users/Administrator/Pictures/gptGen/sword_enemy_combat_idle_v4_max_transparent.png`

这证明当前模型、endpoint、令牌和参数组合可以返回原生 Alpha 编辑结果，但不保证其他账号、时间、模型路由或输入图也具有相同能力。每次仍需读取实际 PNG 并检查 Alpha；不要把旧的 HTTP 400 经验或本次成功无条件外推。

本次 v4 还观察到 Alpha 最大值为 `254`，没有 Alpha=`255` 的像素，且大量主体边缘为半透明。它通过了有效 Alpha 的一级检查，但是否适合作为干净 Unity 素材仍要看深色/浅色预览中的光晕、透明度衰减和主体完整性。

`gpt-image-2` 在当前令牌下曾返回无模型访问权限的 `403`；这只是当前令牌/路由观察，不能当作永久能力声明。保身份任务不得静默退回无参考纯文生图。API 超时或结果不明时，不可把本地查无文件说成已查询服务端；结果与费用未知，需用户确认后才重新提交。透明验收与后处理详见配套工作流。

### 无 requests 的标准库调用骨架

```python
import base64, json, os, pathlib, urllib.request

api_key = os.environ["MUSK_API_KEY"]
out = pathlib.Path.home() / "Pictures" / "gptGen" / "result.png"
out.parent.mkdir(parents=True, exist_ok=True)
request = urllib.request.Request(
    "https://api.muskapis.com/v1/images/generations",
    data=json.dumps({"model": "gpt-image-2.6-sunburst", "prompt": "...", "size": "1024x1024", "quality": "max", "output_format": "png"}).encode("utf-8"),
    headers={"Authorization": f"Bearer {api_key}", "Content-Type": "application/json"},
    method="POST")
with urllib.request.urlopen(request, timeout=300) as response:
    item = json.load(response)["data"][0]
if item.get("url"):
    urllib.request.urlretrieve(item["url"], out)
else:
    out.write_bytes(base64.b64decode(item["b64_json"]))
raw = out.read_bytes()
assert len(raw) > 1024 and raw.startswith(b"\x89PNG\r\n\x1a\n")
print(out)
```
## 执行流程

1. 判断任务类型：文生图、单图编辑、多图融合。
2. 检查是否有 `MUSK_API_KEY`。
3. 没有指定输出路径时，保存到当前 Windows 用户的 `Pictures/gptGen/`。
4. **实际调用生图 Skill/API**：不能把读取本 Skill、写 prompt、准备脚本或描述预期效果当作已经生成。
5. 接口返回后，读取 `data[0].url` 或 `data[0].b64_json`。
6. 下载或解码图片到本地。
7. **验证完成状态**：确认 API 请求实际返回成功，确认输出文件存在且文件头/格式可读；必要时用文件系统检查或读取图片确认。
8. **向用户交付地址**：回复中必须明确贴出真实、完整的本地输出路径；若有图片附件能力，同时附上图片。只生成但不提供地址，不算完成。
9. 只有完成第 4、6、7、8 步后，才可以向用户说“已生成/完成”。
10. 如果尚未调用 API、调用失败、超时、返回空结果、下载失败或文件验证失败，必须明确报告当前状态；不得把 prompt、计划、预期结果或概念描述当成已完成结果，也不得声称图片“就在上方”。

## 自我纠察清单

每次生图任务回复前逐项确认：

- [ ] 本轮确实执行了生图 Skill/API，而不是只读取了 Skill 文档。
- [ ] 工具返回成功，且没有把失败/空结果误判为成功。
- [ ] 输出文件存在、大小合理、文件格式可读。
- [ ] 回复中的路径与实际输出路径完全一致。
- [ ] 回复明确包含地址；没有地址不得宣称任务完成。
- [ ] 如用户要求看图，已实际附图或明确说明无法附图并仍提供地址。
- [ ] 若此前误报，先承认并更正，不延续错误状态。

**事实边界规则**：工具调用和验证结果是完成性表述的唯一依据。不要根据上下文推断工具已经执行；被用户追问时，先重新核查工具结果和文件状态。

## Python 模板：文生图（当前环境可用，无 `requests`）

```python
import base64
import json
import os
import pathlib
import sys
import urllib.error
import urllib.request

api_key = os.environ.get("MUSK_API_KEY")
if not api_key:
    raise SystemExit("Missing MUSK_API_KEY")

prompt = sys.argv[1]
out_path = pathlib.Path(sys.argv[2]) if len(sys.argv) > 2 else pathlib.Path.home() / "Pictures" / "gptGen" / "gpt-image-result.png"
out_path.parent.mkdir(parents=True, exist_ok=True)

request = urllib.request.Request(
    "https://api.muskapis.com/v1/images/generations",
    data=json.dumps({
        "model": "gpt-image-2.6-sunburst",
        "prompt": prompt,
        "size": "1024x1024",
        "quality": "max",
        "output_format": "png",
    }).encode("utf-8"),
    headers={"Authorization": f"Bearer {api_key}", "Content-Type": "application/json"},
    method="POST")

try:
    with urllib.request.urlopen(request, timeout=300) as response:
        item = json.load(response)["data"][0]
except urllib.error.HTTPError as exc:
    raise SystemExit(f"HTTP {exc.code}: {exc.read().decode('utf-8', errors='replace')}")

if item.get("url"):
    urllib.request.urlretrieve(item["url"], out_path)
elif item.get("b64_json"):
    out_path.write_bytes(base64.b64decode(item["b64_json"]))
else:
    raise SystemExit("No image result found")

raw = out_path.read_bytes()
if len(raw) <= 1024 or not raw.startswith(b"\x89PNG\r\n\x1a\n"):
    raise SystemExit("Output PNG verification failed")
print(json.dumps({"saved": str(out_path), "bytes": len(raw)}, ensure_ascii=False))
```

## Python 模板：单图编辑 / 多图融合

本环境没有 `requests`。编辑/多图融合必须以 Python 标准库手工组装 `multipart/form-data`：每张输入图使用同名字段 `image[]`，图片部分包含文件名与 `Content-Type`，并把 `model`、`prompt`、`size`、`quality`、`input_fidelity`、`background`、`output_format`、`n` 作为普通字段上传到 `/v1/images/edits`。透明编辑任务还必须检查响应图片的实际 Alpha，不能只看请求中是否发送了 `background=transparent`。

已验证的透明背景成例与两级验收见 `skill/workflows/image-asset-generation.md`；多图编辑的 multipart 组装按以下规则执行：构造唯一 boundary、写入每个文本和图片 part、以 `urllib.request.Request` 发送请求，随后从 `data[0].url` 或 `data[0].b64_json` 保存到指定路径，并验证 PNG 签名。禁止使用以下当前环境不可用的 `requests.post(..., files=...)` 模板。

## 可能遇到的问题

### Missing MUSK_API_KEY

原因：没有设置环境变量，或设置后当前进程未刷新。

处理：设置 `MUSK_API_KEY` 后重启终端 / Locus，再重试。

### 401 / 403

原因：Key 无效、过期、额度不足或服务端拒绝。

处理：确认 Key 是否正确，联系接口管理员检查权限或额度。

### 请求超时

原因：图生图、多图融合耗时较长，或网络不稳定。

处理：使用 300 秒超时；必要时重试。生产环境应对瞬时 5xx / 网络抖动做退避重试。

### edits 接口失败

常见原因：把图生图请求错误地按 JSON 发送。

处理：`/v1/images/edits` 必须使用 `multipart/form-data`，图片字段名必须是 `image[]`。

### content_policy_violation（HTTP 400）与“武器参考图”

2026-11-14 实测（本项目抡斧四视图）：**上传我们自己生成的武器概念图当设计锁**，或用
“复制这件武器 / 同一个已批准设计 / keep exactly the approved concept”这类口吻请求复现某件武器，
会被服务端以 `content_policy_violation`（400）拦下，**且不生成、不计费**。

被排除的嫌疑项（实测都不是原因）：参考图的高光亮度（压高光仍 400）、自定义尺寸（`384x1024` 与 `1024x1536` 同样 400）、
负面词里出现别的武器名（去掉 `knife/spear/sword` 仍 400）、提示词里完全不提武器名（仍 400）。

**可行替代**：不要上传生成图当锁，而是把「该武器的造型描述」写进提示词，配上一组**第三方参考图**
（本项目用 PBZ 斧头立绘 + 画风色卡），并要求“按指定观察方向出一张正交单视图”——实测 6 张全部 HTTP 200，
且因为参考图与描述一致，重绘出来的造型与已验收概念图高度接近。

另一个实操要点：**同一句措辞有时 200 有时 400**，该过滤对具体措辞敏感；被拦时先改写措辞再重发，
不要在同一句上连续重试。完整改动记录见 `plan/weapon-concept-image-plan-20261114.md` §5.10。

#### 2026-11-15 新增：被拦的可能是**参考图本身**，不是措辞（锁镰概念图实测）

追命锁镰概念图连续 3 次 400（request id `20261115101035172894036Cr4d9t3W` /
`20261115101430520743849R0YosNfV` / `20261115101610904186867CCcqIitf`），
改写措辞、缩短提示词都没用。用**隔离实验**定位（两次请求只差“有没有上传那张图”）：

- 382 字符短提示词 + PBZ《影之刃零》追命锁镰立绘 `zhuiming_suolian.png` → **400**
- **逐字相同**的提示词 + 项目自产色卡 / 木料铁箍参考图 → **200**（生成成功）

⇒ 该立绘只要作为 `image[]` 上传就被拦，**与提示词长短、负面词、武器名、参数无关**。
⇒ 排查顺序建议：**先做一次“同提示词、换参考图”的隔离实验**，再决定改词还是换图。
⇒ 400 是服务端拒绝、不建任务、不计费，探测成本低，可以放心做隔离实验。

#### 补充（2026-11-15 第二轮）：修正已通过的方案时，**别用新负面词和否定句**

锁镰 v5 在 v4 已通过的基础上追加了三条负面词（`chain attached to the grip`、
`chain hanging from the handle bottom`、`ring on the grip butt`）和一句 `No chain may touch the grip`，
结果直接 400（request id `20261115104200256054668h6Mc5ryi`，参考图与 v4 完全相同）。
把新增内容改成**中性描述句**（“链条由钩背外弧上一个明显高于握柄的受力连接环承载”）、
负面词**原封不动**沿用上一版，立刻 200。

⇒ 修正已通过方案时：**只加中性描述句，不加新的否定式负面词**；
“禁止 X / 不得 Y / chain attached to Z” 这类补强既容易被拦，也未必改善出图。

#### 2026-11-15 第三轮：**“制图术语”会把武器图请求拦成 400**

锁镰四视图阶段连续 3 次 400（`20261115105321690630634ZI47K5Ja` / `20261115105601937512355oKOiaokF` /
`20261115105652214693849lkZWWkRR`），对照组实验定位到触发点：

| 提示词 | 结果 |
|---|---|
| 造型描述原文 + “orthographic elevation sheet **for 3D modeling** … no perspective shortening” + 方向段 | ❌ 400 |
| 同上，换一种措辞 | ❌ 400 |
| 造型描述原文 + 极简正交句 + 一行方向句 | ❌ 400 |
| **造型描述原文 + 负面词**（= 之前成功出图那条，逐字一致） | ✅ 200 |
| **造型描述原文 + 一行坐标方向句**（无任何正交 / 建模措辞） | ✅ 200 |

⇒ **`orthographic projection` / `elevation sheet` / `for 3D modeling` / `no perspective shortening` 这类
“把武器画成制图/建模参考”的措辞会触发拦截**；一行坐标方向句（“FRONT view: the observer looks from +Z toward −Z;
screen right is +X, screen up is +Y.”）是安全的。
⇒ 需要“正对、平整”时改用**游戏素材语言**：`flat and centered like a game asset sheet`。
⇒ 通用排错顺序（本项目三次实战）：**先做“同图、同造型描述，只加减指令段”的隔离实验**，
把触发点定位到具体指令段，再决定改词——不要盲目重发整条提示词。
完整记录见 `plan/weapon-concept-image-plan-20261114.md` §5.15。

#### 2026-11-15 第四轮：多视图（正面 / 侧视）出图的两个坑

1. **“只要一件、且是边缘侧视”这句要求基本拿不到**：模型要么把侧视画成 3/4 宽面，
   要么干脆**并排画“正面 + 侧面”**。可行做法是**顺着它**——让它并排画，再**本地裁出**那张真正的边缘侧视。
2. **强化侧视的措辞容易撞内容安全**：像“宽面完全背对观察者、不可见”“钢板上切出窄槽”这类结构细节描述，
   实测连续 3 版被 400 拦掉（`20261115112537765430581bXI0xd8S` / `20261115112811829416452VR37xccT` /
   `20261115113026353173934ihRk382s`），而更温和的短句版本反而通过。
   ⇒ 侧视图的引导词要**短、视觉化、少用结构术语**（“slim vertical steel wedge”“a thin bright line”“alternating wide ovals and thin edges”）。

#### 2026-11-15 第六轮：**同一条提示词，早上能过、晚上被拦**（拦截状态会变）

> 背景：追命锁链按 plan §5.18 方案发起生成时，**连续 7 次 400**（edits 5 次 + generations 1 次，
> 外加 1 次 503 upstream_unavailable），其中包括**逐字重放当天 18:46 成功过的那条提示词**（v6）。

| 请求 | 通道 | 结果 |
|---|---|---|
| §5.18 正文（逐字） | edits | ❌ 400 |
| 同上，正文不变、负面词换中性最小集 | edits | ❌ 400（先遇一次 503） |
| 同上，否定句改中性描述 | edits | ❌ 400 |
| 同上，负面词去掉 `tilted weapon` | edits | ❌ 400 |
| 以**已验证可过的 v6 文案**为底、只改一个结构要素 | edits | ❌ 400 |
| **v6 原文逐字重放** | edits | ❌ 400 |
| §5.18 正文 | generations | ❌ 400 |
| 改写后的 v8 | generations | ✅ 200 |
| 同一段 v8 文字 | edits | ✅ 200 |

⇒ **拦截规则随路由/后端与时间变化**。同一条措辞在 A 时点可过、B 时点被拦。
⇒ 排查顺序要改：**任何 400 先怀疑通道状态**，做一次“已通过提示词的逐字重放”来判断是措辞还是通道；
若重放也被拦，**不要再改词**，直接换框架或换通道。

#### 2026-11-15 第七轮：能过审的写法是“**幻想游戏道具 + 纯几何描述**”

被拦的高风险成分是**叙事 + 否定句**：
`throwable` / `can fly forward` / `catch a distant enemy` / `pull the enemy back` /
`toward the player's hand` / `The spatial order is mandatory` /
`must not be near the hook head, hook back, hook root, or blade root`。

可行的改法（同一段结构要求、同一句信息量）：

1. 开头定位为“**为某类游戏设计一件幻想游戏道具武器的概念图**”；
2. **删掉全部叙事与否定句**，只用**几何结构**描述（谁在上、谁在中、谁在下、怎么连）；
3. 负面词只留**中性最小集**（`character, hand, person, scenery, text, watermark, logo, cropped weapon, multiple weapons`），
   **不列武器名、不列“X attached to Y”类否定式**；
4. 白底写进正文。

⇒ 同一段文字在 **edits 与 generations 两个通道都一次通过**。
⇒ 同时验证：**generations（无参考图）可以作为 edits 被拦时的正式通道**，
代价是失去画风参考（笔触由模型自选），所以**优选顺序：先试 edits（带画风参考），不行再走 generations**。

**补充（同日 §5.20 轮）**：连**需求文档里逐字写好的最终提示词**也会被拦（v9：502 后重试 400）。
高风险成分依旧是否定式补强：`not a closed circular grappling hook` /
`Do not close it into a circular C-shaped metal hook` /
`It is not near the sickle head, blade back, blade root, or cutting edge` /
`No chain links appear beside the sickle head` /
负面词里的 `chain attached to blade root` / `head-mounted chain` 这类条目。
⇒ **改法依旧是**：删掉否定句与叙事句，只用几何结构描述（谁在前、谁在中、谁在后、怎么连），
负面词只留中性最小集 —— 同一份结构要求改写后一次通过（v10）。
⇒ 结论可推广：**“否定式补强”是被拦的高风险成分；结构要求用正向几何描述表达更安全。**

#### 2026-11-15 第八轮：**上传自己的概念图当“设计锁” + 锁定措辞 = 被拦；同图换措辞 = 通过**

> 场景：需要以一张已生成的武器概念图为造型基准做定点编辑（把链条从头部迁到握柄尾部）。

| 请求 | 输入图 | 提示词 | 结果 |
|---|---|---|---|
| v11 | 概念图 + 3 张画风参考 | “The first image is the only shape authority. Preserve its … do not redesign or redraw the weapon.” | ❌ 400 |
| v12 | **完全相同的 4 张图** | 同一结构要求，改写成正向几何描述（“Use the sickle head of the first image as the shape basis: … keep their existing shape, proportions and placement.”） | ✅ 200 |

⇒ 被拦的是**“复制/锁定同一件已批准设计”的措辞 + 上传我们自己生成的武器图**这个组合，
**不是**“上传概念图当参考”这件事本身 —— 同一张图换个说法就通过了。
⇒ 实操：**先按原文试一次；被拦后不要降级输入图**（删掉参考图改用文字 = 丢掉造型基准），
而是**保留输入图、只改措辞**（改成正向几何描述 + 中性最小负面词）。
⇒ 判定“输入图是否真的用上了”要查请求留档里的 `image[]` sha256，不要只看提示词。

#### 2026-11-15 第九轮：**同一类措辞，当时能过 ⇒ 不要预设“必被拦”**

§5.24 的原文（含 `not a small sickle hanging from a long chain`、
`Do not scale the sickle head down to fit the chain`、
负面词里的 `chain attached to blade root` 等否定式补强）在 **edits 通道一次通过**，
而同类措辞在 §5.18 / §5.20 / §5.22 三轮里连续被拦。

⇒ 同一类措辞在**不同时点**结果相反 ⇒ **不要因为“上次这种写法被拦”就在提交前预先改写**。
⇒ 正确做法：**先按文档原文提交一次**（400 不计费），
被拦再按“保留输入图、只改措辞”处理；没被拦就直接拿到与文档一致的产物。

#### 2026-11-16 凌晨第十轮：**拦截是“通道 + 时段”的家族级状态**

一次“正式正面立绘”需求里，连续 4 次 400：

| 尝试 | 通道 | 输入图 | 结果 |
|---|---|---|---|
| v15 | edits | 自产武器图 v13 + v4 + 三张画风参考 | ❌ 400 |
| v16 | edits | 同上（只改措辞） | ❌ 400 |
| v17 | edits | v13 + 三张画风参考 | ❌ 400 |
| v18 | edits | **只留三张画风参考（一张武器图都不传）** | ❌ 400 |
| v18gen | **generations** | 无参考图 | ✅ 200 |

⇒ 关键：**连“不传任何自产武器图、只传项目自产画风参考”都被拦**，
而这三张画风参考在几个���小时前（v10 / v13）还成功过
⇒ 拦截是**通道 + 时段**级别的家族性收紧，不是某张图或某句词。
⇒ 排查顺序要改成：
① 先按文档原文提交一次；
② 被拦 → 改措辞再试一次（可能有效，也可能无效）；
③ 仍被拦 → **换通道**（generations，不带参考图），
并把**白底 + 配色 + 材质**全部写进正文（没有参考图时模型会自编背景与笔触）。
⇒ 400 不计费，所以这一串“逐级降级”值得走完再回报。

#### 2026-11-16 第十一轮：**造型漂移的根因是“构图竞争”——把大改动拆成多步最小编辑**

> 场景：已有���张已认可的武器概念图 A，要改它的一个部件（把链条从头部挪到尾部）。
> 直接提“在 A 的基础上把链改到尾部”⇒ 模型为了塞下链条重新平衡构图，**把主体（镰头）重画了**。

有效做法（实测）：

| 步骤 | 输入 | 只改一处 | 主体形状保持度 |
|---|---|---|---|
| 1 | A（**只 1 张**） | 去掉该部件，末端改成封死的帽 | **剪影一致率 99.6%** |
| 2 | 第 1 步的图（**只 1 张**） | 加上新部件 | 一致率 97.6%（较上一步） |

⇒ 三条硬规则：
1. **被改动的东西如果会在画幅里占很大面积，它就会和主体抢构图** ⇒ 先把它**拿掉**，再**加回来**。
2. **每步只上传上一张结果，且只传 1 张图**（多图融合会让模型重新拼装，直接破坏形状）。
3. 提示词**只写“改哪一处 + 其余保持原位原样”**，不重述整体造型、不写“重新设计”。

⇒ 验收用**剪影一致率**（二值化后逐像素比对）而不是目视，这样“有没有漂移”才有数字。

完整记录见 `plan/weapon-concept-image-plan-20261114.md` §5.19。

#### 2026-11-16 第十二轮：**503/504 是上游路由故障，不是内容拦截**（与 400 完全不同）

> 场景：抡斧砸地裂纹「冲击语言 2×2 四格对照板」。同一请求（提示词逐字未改）连续 4 次失败，第 5 次成功：

| 次 | 结果 | request id | 载荷 |
|---|---|---|---:|
| 1 | HTTP 503 upstream_unavailable | `20261116064647127370305wOO5KSYQ` | 6.61 MB |
| 2 | HTTP 504 upstream_timeout | `202611160650334442423328dxr2tJ4` | 6.61 MB |
| 3 | HTTP 503 upstream_unavailable | `202611160654344656332833FTGXbyt` | 1.86 MB（已降体积） |
| 4 | HTTP 503 upstream_unavailable | `20261116065830105525208gxH3gMxM` | 1.86 MB |
| 5 | ✅ HTTP 200，104.21 s | — | 1.86 MB |

⇒ **判据**：400 = 内容/策略拒绝（改措辞、换通道）；**503 / 504 = 上游出图后端故障** ⇒ **不要改提示词、不要改输入图，原样重发 + 间隔等待**。
⇒ 同期免费探针 `GET /v1/models` 返回 200 ⇒ 网关与 token 正常，故障在上游 ⇒ **别去查 key、别去换模型名**。
⇒ ⚠ **「重请求失败就降体积」这条经验在 503 场景不适用**：第 3、4 次已把载荷从 6.61 MB 降到 1.86 MB，**仍然 503** ⇒ 不要为此白改输入图或把参考图换成 JPEG/缩图（那会引入与 400 排查相冲突的噪音）。
⇒ 有效节奏：快失败（<70 s）的 503 直接等 5 分钟重发；超时型的 504 也可原样重发。
⇒ 每次失败都要把 **request id 报给用户**（便于找服务商查），并在留档 JSON 里写明“提示词未改”。

#### 2026-11-15 第五轮：**edits 通道拦的请求，generations 通道可能放行**

> ⚠ 背景：该轮锁镰概念图 v1–v6 与四视图**最终都被用户否决**，本条只作为**接口行为**的经验保留。

同一段提示词（武器造型正文 + 视图语义）在 `/v1/images/edits`（带参考图）连续 9 次被 400 拦掉，
而走 `/v1/images/generations`（不带参考图、JSON、`n` 必须是数字）**一次通过**。

⇒ 两个 endpoint 的审核策略**不是同一套**；带参考图的 edits 更严。
⇒ 需要“视图语义 / 正交 / 结构描述”这类容易触发拦截的内容时，**改走文生图通道**。
⇒ 没有参考图时模型会自己编背景（实测给锁链武器铺了黑底 + 辉光），必须把“白底”写死进正文。
⇒ `/v1/images/generations` 的 JSON 里 `n` 要写数字（写字符串会 500：
`json: cannot unmarshal string into Go struct field Alias.n of type uint`）。
⇒ 2026-11-15 晚补充：即使两边措辞完全相同，两边的拦截状态也会各自变化；
`generations` 不保证比 `edits` 松，只是**不同的一套规则**。

完整记录见 `plan/weapon-concept-image-plan-20261114.md` §5.12。

### 道具类武器的“正交侧视”出不来

生成器做不出大刃武器的端向/侧向投影：要求“侧视”时它会照抄正面的宽刃造型。
可行做法是把侧面**正面描述成窄楔**（例如「斧头在画面上只有正面宽度的五分之一，是一片侧对镜头的竖直钢楔」），
并把该描述放进提示词；这样能得到真正的边缘侧视。
另：不要一次出多视图合图，比例会被带歪；合图与中文标注全部本地排版。

### 没有生成文件

可能原因：返回结果没有 `url` 或 `b64_json`，或下载失败。

处理：输出原始响应片段，检查 `data[0]` 内容。

### 先落盘图片，再清洗响应 JSON

响应只带 `b64_json` 时（实测单张可达 170 万字符），如果为了存档先把 `data[0].b64_json` 替换成占位串再写文件，图片数据会被自己覆盖。服务端此时已计费完成，本地丢失只能重新提交，会重复付费。

正确顺序：先把 `b64_json` 解码写成 PNG（或下载 `url`），再把响应 JSON 里的 base64 换成占位符后落盘。2026-10-22 体检实测踩过这个坑，见 `memory/gpt-image-workflow-check-20261022.md`。

### ��径包含反斜杠问题

Windows 路径建议在脚本和 Skill 文档中使用正斜杠：

```text
C:/Users/Administrator/Pictures/gptGen/result.png
```

避免 `\` 被转义。

### curl -o 下载的文件是 JSON 而不是图片

原因：API 可能返回 `b64_json` 而非 `url`，`curl -o` 直接把 JSON 写入了目标文件。

诊断：用 `xxd 文件路径 | head -1` 检查文件头，若以 `{` 开头则是 JSON。

处理：用 Python 解码 base64：

```python
import json, base64, pathlib
raw = pathlib.Path('目标路径').read_text()
data = json.loads(raw)
b64 = data['data'][0]['b64_json']
png = base64.b64decode(b64)
pathlib.Path('目标路径').write_bytes(png)
```

### Windows 代理导致 Python requests 失败

原因：Windows 系统代理配置可能干扰 `requests` 库连接 `api.muskapis.com`。

处理：Python 方式 — 设置 `os.environ['NO_PROXY'] = '*'` 或使用 `session.trust_env = False`。更简单的方式：直接用 `curl --noproxy '*'` 替代 Python requests。

### 生成图不符合预期

处理：补充 prompt，明确风格、主体、背景、构图、文字、不要改变的部分。图生图时可提高 `input_fidelity`。

### 中文文字错误

该服务支持中文标题渲染，但复杂长文仍可能出错。

处理：减少文字长度，明确写：`顶部中文大字标题：「具体文字」`。

## 模型默认值变更记录

### 2026-11-08：默认模型切到 `gpt-image-2.6-sunburst`

用户指定「后续都用 2.6 代替 2.5，其他参数不变，质量用 `max`」。切换前先做探针：

| 探测 | 结果 |
| --- | --- |
| `GET /v1/models` | HTTP 200；图像模型只有 `gpt-image-2.6-sunburst`（`gpt-image-2.5-sunburst` 当天已不在列表） |
| `POST /v1/images/generations`，model=`gpt-image-2.6-sunburst`，`quality=max`、`size=1024x1024`、`output_format=png`、`n=1` | **HTTP 200，87.66s**，1024×1024 PNG（Color Type 2 RGB，837,807 bytes），usage total 1777 |

- 结论：**2.6 可用**，默认模型改为 `gpt-image-2.6-sunburst`。
- 参数口径**只改模型名**：`quality=max`、`input_fidelity=high`、`size`、`output_format=png`、`n=1`、是否传 `background` 等一律沿用原设定。
- 探针脚本：`Library/Locus/tmp/1013art/probe_gpt_image_2_6.py`；探针留档：`C:/Users/steam/Pictures/gptGen/probe_gpt_image_2.6/probe_2_6.{png,result.json}`。
- 成本差异（历史实测，仅供知悉）：2.5 与 2.6 在同一 `quality` 下的 image token 差距可达约 4 倍；本次切换是用户明确指定的版本升级，不以成本为依据自动回退。
- 切换后**所有生成脚本的 `MODEL` 常量统一改为 `gpt-image-2.6-sunburst`**；历史记录类文件（`*.request.json` / `*.response.json` / `*.status.json` / 交接 md 里的历史参数）保持原样不改，它们记录的是当时真实发出的请求。

## 安全注意事项

- 不要泄露 `MUSK_API_KEY`。
- 不要把 Key 写进仓库、日志、文档或截图。
- 远程 URL 可能只是中间产物；需要长期保存时应下载到本地或转存到自己的存储。
- 若输出要纳入 Unity 项目，再指定保存到 `Assets/...`；普通测试图默认放到当前 Windows 用户的 `Pictures/gptGen/`。
