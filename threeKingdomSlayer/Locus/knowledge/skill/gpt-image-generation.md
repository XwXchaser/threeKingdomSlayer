---
id: kd_e6815137-b73f-41d7-b6ad-d76834c73ab0
injectMode: inherit
summary: 需要通过 GPT Image 接口文生图、图生图、多图融合或语义去背并落盘交付时使用；覆盖模型、质量档位、透明参数、调用模板与排错，任务编排与透明验收见 workflow。
aiEditMode: inherit
skillEnabled: true
skillSurface: command
commandTrigger: /gpt-image
argumentHint: <prompt> [--image path ...] [--size 1024x1024|1024x1536|1536x1024] [--quality low|medium|high|xhigh|max|auto] [--out path]
---

# gpt-image-generation

脱离 Unity 使用 GPT Image 图像生成接口的使用文档：说明调用方式、模型与质量档位、必需配置、默认输出目录、透明参数、示例、接口规则和常见问题。

## 使用说明

`/gpt-image` 用于脱离 Unity Editor 调用 GPT Image 图像生成接口，当前优先使用 `gpt-image-2.5-sunburst`。Unity 断开、未启动、或项目不在 Play Mode 都不影响使用。

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

- `model`: 当前优先使用 `gpt-image-2.5-sunburst`；`gpt-image-2.5-flare` 也属于当前 GPT Image 模型，但具体路由/权限须实测
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

- `model`: 当前优先使用 `gpt-image-2.5-sunburst`；`gpt-image-2.5-flare` 的可用性须按当前路由实测
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

当前 `gpt-image-2.5-sunburst` 和 `gpt-image-2.5-flare` 文档列出的 `quality` 枚举为：

```text
low | medium | high | xhigh | max | auto
```

- `auto` 是模型默认档位，由模型自动选择。
- `low` 适合草稿，`medium` 适合普通候选，`high` 适合常规最终候选。
- `xhigh` 和 `max` 是当前 2.5 模型新增的更高质量档位；当前没有文档或实测证据表明存在高于 `max` 的档位。
- 质量档位不是姿态锁定或角色身份锁定开关。`max` 可能带来更丰富的重绘细节，也可能改变局部轮廓；参考图编辑仍需使用明确的不可改变约束和 `input_fidelity=high`。
- 早期 GPT Image 模型最高支持到 `high`，不能把 `max` 自动外推给旧模型。
- `size`、`quality` 和 `background` 在当前文档中支持 `auto`；透明输出应显式设置 `background=transparent`，并使用支持 Alpha 的 `png` 或 `webp`。
- `input_fidelity` 仅用于编辑/融合任务；它不能替代视觉验收。

### GPT Image 语义去背（复杂灰底/角色边缘）

当原图背景与角色灰色铠甲、剑刃或暗色轮廓相近，传统阈值抠图产生明显脏边时，可以使用 `/v1/images/edits` 单图编辑做语义去背，避免直接否定已验收视频或要求重新生成视频。

已实际验证的单帧参数组合：

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
- 本测试使用 `high` 而非 `max`；不据此声称 `high` 必然比 `max` 更少重绘。
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
- 可用模型可用**免费** `GET https://api.muskapis.com/v1/models` 查询（不生成图、不计费）：当前令牌只列出 `gpt-image-2.5-sunburst` 一个图像模型（其余为 `gpt-5.6-luna`、`gpt-6-astra`、`gpt-6.1-sol`）。请求 `gpt-image-2.5-flare` 返回 **HTTP 403 “This token has no access to model”**，已实测，不要再把它当可用候选。
- 结论：“透明能力”不取决于模型名，而是取决于**本次落到哪套后端**；同一 `sunburst` 在 G1/G2 上能给出原生 alpha，在 B 上一定不给。
- 机器验收第一步永远是读 PNG Color Type 或 IHDR：`2`=RGB、`6`=RGBA。查看器或预览图里显示的“透明格子”不能作为透明度证据。
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
    data=json.dumps({"model": "gpt-image-2.5-sunburst", "prompt": "...", "size": "1024x1024", "quality": "high", "output_format": "png"}).encode("utf-8"),
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
        "model": "gpt-image-2.5-sunburst",
        "prompt": prompt,
        "size": "1024x1024",
        "quality": "high",
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

### 没有生成文件

可能原因：返回结果没有 `url` 或 `b64_json`，或下载失败。

处理：输出原始响应片段，检查 `data[0]` 内容。

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

## 安全注意事项

- 不要泄露 `MUSK_API_KEY`。
- 不要把 Key 写进仓库、日志、文档或截图。
- 远程 URL 可能只是中间产物；需要长期保存时应下载到本地或转存到自己的存储。
- 若输出要纳入 Unity 项目，再指定保存到 `Assets/...`；普通测试图默认放到当前 Windows 用户的 `Pictures/gptGen/`。
