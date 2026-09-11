---
id: kd_e6815137-b73f-41d7-b6ad-d76834c73ab0
injectMode: inherit
aiEditMode: inherit
skillEnabled: true
skillSurface: command
commandTrigger: /gpt-image
argumentHint: <prompt> [--image path ...] [--size 1024x1024|1024x1536|1536x1024] [--quality low|medium|high] [--out path]
---

# gpt-image-generation

## Summary
脱离 Unity 使用 gpt-image-2 图像生成接口的 Skill 使用文档：说明调用方式、必需配置、默认输出目录、示例、接口规则和常见问题。

## Content
## 使用说明

`/gpt-image` 用于脱离 Unity Editor 调用 `gpt-image-2` 图像生成接口。Unity 断开、未启动、或项目不在 Play Mode 都不影响使用。

支持三类任务：

- 文生图：只提供文字 prompt，生成新图片。
- 单图编辑：提供 1 张参考图，要求替换背景、改风格、保留主体等。
- 多图融合：提供多张图片，把元素、风格或构图融合成一张图。

默认输出目录：

```text
C:/Users/steam/Pictures/gptGen/
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

- 输出尺寸：`1024x1024`、`1024x1536`、`1536x1024`
- 画质：`low`、`medium`、`high`
- 输出路径：通过 `--out` 指定
- 图生图保真度：`input_fidelity=high` 或 `low`

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

默认保存到：

```text
C:/Users/steam/Pictures/gptGen/
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
/gpt-image 生成一个像素风铜钱道具图标 --out C:/Users/steam/Pictures/gptGen/coin.png
```

### 单图编辑

```text
/gpt-image 参考 C:/Users/steam/Pictures/input/hero.png，保持人物不变，把背景改成三国战场夕阳氛围 --out C:/Users/steam/Pictures/gptGen/hero_battle.png
```

### 多图融合

```text
/gpt-image 融合 C:/a.png 和 C:/b.png，把第二张的武器自然加入第一张角色手中，光影统一 --out C:/Users/steam/Pictures/gptGen/fusion.png
```

## 接口规则

### 文生图

Endpoint:

```text
POST https://api.muskapis.com/v1/images/generations
```

协议：`application/json`

必填字段：

- `model`: `gpt-image-2`
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

- `model`: `gpt-image-2`
- `prompt`
- `image[]`

多图融合时重复传多个 `image[]`。

常用可选字段：

- `size`
- `quality`
- `input_fidelity`
- `output_format`

## 本项目已验证成功范例（2026-09）

本项目已成功调用 `gpt-image-2`，生成并落盘：

```text
C:/Users/steam/Pictures/gptGen/enemy_101_dialogue_portrait_v2.png
```

该次成功流程的可复用要点：

1. **先实际检查前置条件**：用 shell 检查 `MUSK_API_KEY`；不要只假设环境变量存在。此环境的 managed Python 没有安装 `requests`，不能照抄 `requests` 模板后宣称已调用。
2. **文生图可用标准库 `urllib.request`**：向 `/v1/images/generations` 发送 JSON，`Authorization: Bearer $MUSK_API_KEY`，模型固定 `gpt-image-2`。接口返回可能给 `data[0].url` 或 `data[0].b64_json`，两种都必须处理。
3. **图生图/多参考必须走 edits 接口**：向 `/v1/images/edits` 发 `multipart/form-data`，每张参考图都使用同名字段 `image[]`；不可把本地路径仅写进 prompt，也不可对 edits 接口发送 JSON。
4. **参考图职责分配**：第一张放“角色身份/造型”的严格参考，后续图放“头像构图/像素风格/UI语言”参考；prompt 中明确每张图的职责和优先级，禁止模型把 Boss 的华丽元素转移到普通敌人。
5. **输出路径必须显式指定**：每次使用固定的版本化路径，例如 `C:/Users/steam/Pictures/gptGen/<asset>_v1.png`；脚本先 `mkdir(parents=True, exist_ok=True)`。
6. **成功不能只看 HTTP 200**：写文件后检查存在、文件大小合理（至少大于 1KB）、PNG 签名 `\x89PNG\r\n\x1a\n`；再用图片读取工具人工检查是否符合美术需求。API 成功不等于美术可用。
7. **必须在回复中交付真实路径**：未实际调用、未落盘、未验证或没有完整路径时，绝不能说“已生成”。会话附件、prompt 文本或预期效果都不是交付物。

### 本次失败教训

- 内置会话生图附件不等于本项目的 `/gpt-image` 落盘工作流；用户要求本地验收时，必须调用本 Skill 描述的 API 并输出到 `C:/Users/steam/Pictures/gptGen/`。
- 不要因为工具列表没有名为 `gpt-image-generation` 的原生工具就判断 Skill 不可用。本 Skill 是命令/流程文档，实际执行应按其 API 说明通过 shell/Python 完成。
- 生成前必须先阅读相关角色精灵、已有头像、UI 框和项目美术文档。只用文字描述会产生题材正确但风格错误的图；例如 Enemy_101 曾被错误生成成写实厚涂成年士兵，不能导入。
- “透明背景”请求可能返回带可见棋盘格的 RGB 背景或不干净 Alpha；必须视觉检查，未通过时标记为待重做/待抠图，不能直接导入 Unity。

### 无 requests 的标准库调用骨架

```python
import base64, json, os, pathlib, urllib.request

api_key = os.environ["MUSK_API_KEY"]
out = pathlib.Path(r"C:/Users/steam/Pictures/gptGen/result.png")
out.parent.mkdir(parents=True, exist_ok=True)
request = urllib.request.Request(
    "https://api.muskapis.com/v1/images/generations",
    data=json.dumps({"model": "gpt-image-2", "prompt": "...", "size": "1024x1024", "quality": "high", "output_format": "png"}).encode("utf-8"),
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
3. 没有指定输出路径时，保存到 `C:/Users/steam/Pictures/gptGen/`。
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
out_path = pathlib.Path(sys.argv[2] if len(sys.argv) > 2 else "C:/Users/steam/Pictures/gptGen/gpt-image-result.png")
out_path.parent.mkdir(parents=True, exist_ok=True)

request = urllib.request.Request(
    "https://api.muskapis.com/v1/images/generations",
    data=json.dumps({
        "model": "gpt-image-2",
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

本环境没有 `requests`。编辑/多图融合必须以 Python 标准库手工组装 `multipart/form-data`：每张输入图使用同名字段 `image[]`，图片部分包含文件名与 `Content-Type`，并把 `model`、`prompt`、`size`、`quality`、`input_fidelity`、`output_format` 作为普通字段上传到 `/v1/images/edits`。

已验证的完整实现应复用本 Skill“本项目已验证成功范例”中所述流程：构造唯一 boundary、写入每个文本和图片 part、以 `urllib.request.Request` 发送请求，随后从 `data[0].url` 或 `data[0].b64_json` 保存到指定路径，并验证 PNG 签名。禁止使用以下当前环境不可用的 `requests.post(..., files=...)` 模板。

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
C:/Users/steam/Pictures/gptGen/result.png
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
- 若输出要纳入 Unity 项目，再指定保存到 `Assets/...`；普通测试图默认放 `C:/Users/steam/Pictures/gptGen/`。
