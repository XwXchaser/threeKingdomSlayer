---
id: kd_e6815137-b73f-41d7-b6ad-d76834c73ab0
type: skill
path: gpt-image-generation.md
title: gpt-image-generation
inheritInjectMode: true
summaryEnabled: true
commandEnabled: true
readOnly: false
inheritAiConfig: true
skillEnabled: true
skillSurface: command
commandTrigger: /gpt-image
argumentHint: <prompt> [--image path ...] [--size 1024x1024|1024x1536|1536x1024] [--quality low|medium|high] [--out path]
createdAt: 1783568914335
updatedAt: 1783873168941
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

## 执行流程

1. 判断任务类型：文生图、单图编辑、多图融合。
2. 检查是否有 `MUSK_API_KEY`。
3. 没有指定输出路径时，保存到 `C:/Users/steam/Pictures/gptGen/`。
4. 用 Python 或 curl 调接口。
5. 接口返回后，读取 `data[0].url` 或 `data[0].b64_json`。
6. 下载或解码图片到本地。
7. 向用户返回保存路径；如果接口返回 URL，也一并返回。

## Python 模板：���生图

```python
import base64
import json
import os
import pathlib
import sys
import urllib.request

import requests

api_key = os.environ.get("MUSK_API_KEY")
if not api_key:
    raise SystemExit("Missing MUSK_API_KEY")

prompt = sys.argv[1]
out_path = pathlib.Path(sys.argv[2] if len(sys.argv) > 2 else "C:/Users/steam/Pictures/gptGen/gpt-image-result.png")
out_path.parent.mkdir(parents=True, exist_ok=True)

resp = requests.post(
    "https://api.muskapis.com/v1/images/generations",
    headers={"Authorization": f"Bearer {api_key}"},
    json={
        "model": "gpt-image-2",
        "prompt": prompt,
        "size": "1024x1024",
        "quality": "high",
    },
    timeout=300,
)
resp.raise_for_status()
item = resp.json()["data"][0]

if item.get("url"):
    urllib.request.urlretrieve(item["url"], out_path)
elif item.get("b64_json"):
    out_path.write_bytes(base64.b64decode(item["b64_json"]))
else:
    raise SystemExit(f"No image result found: {resp.text}")

print(json.dumps({"saved": str(out_path), "url": item.get("url")}, ensure_ascii=False))
```

## Python 模板：单图编辑 / 多图融合

```python
import base64
import json
import os
import pathlib
import sys
import urllib.request

import requests

api_key = os.environ.get("MUSK_API_KEY")
if not api_key:
    raise SystemExit("Missing MUSK_API_KEY")

# Usage: python script.py out.png prompt image1.png [image2.png ...]
out_path = pathlib.Path(sys.argv[1])
prompt = sys.argv[2]
image_paths = sys.argv[3:]
if not image_paths:
    raise SystemExit("At least one input image is required")
out_path.parent.mkdir(parents=True, exist_ok=True)

opened = []
try:
    files = []
    for path in image_paths:
        f = open(path, "rb")
        opened.append(f)
        files.append(("image[]", (pathlib.Path(path).name, f)))

    resp = requests.post(
        "https://api.muskapis.com/v1/images/edits",
        headers={"Authorization": f"Bearer {api_key}"},
        files=files,
        data={
            "model": "gpt-image-2",
            "prompt": prompt,
            "size": "1024x1024",
            "quality": "high",
            "input_fidelity": "high",
        },
        timeout=300,
    )
    resp.raise_for_status()
finally:
    for f in opened:
        f.close()

item = resp.json()["data"][0]
if item.get("url"):
    urllib.request.urlretrieve(item["url"], out_path)
elif item.get("b64_json"):
    out_path.write_bytes(base64.b64decode(item["b64_json"]))
else:
    raise SystemExit(f"No image result found: {resp.text}")

print(json.dumps({"saved": str(out_path), "url": item.get("url")}, ensure_ascii=False))
```

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
