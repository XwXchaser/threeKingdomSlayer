---
id: kd_4528a6bd-d02f-4a7a-9ed3-56e5bb18fb14
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# GPT Image 生图工作流体检（2026-10-22）

一次性体检记录：确认 `/gpt-image` 两条链路（文生图、图生图）在当前令牌/路由下仍可用，并留下两条会重复付费或误判的坑。

## 结论

| 项目 | 结果 |
| --- | --- |
| `MUSK_API_KEY` | Locus 进程环境内存在（长度 51）；**Windows 用户级环境变量里没有**，新开的终端不会自动继承，需要 `setx` 才持久 |
| `GET /v1/models` | HTTP 200；图像模型只有 `gpt-image-2.5-sunburst`（`gpt-image-2.5-flare` 不在列表，与历史 403 一致）**（2026-10-22 快照；2026-11-06 复检后 2.5 与 2.6 同时可用，见文末）** |
| 文生图 `/v1/images/generations` | HTTP 200，122.46s，1024×1024 PNG（Color Type 2 RGB，1,276,445 bytes），画面符合 prompt |
| 图生图 `/v1/images/edits` | HTTP 200，123.68s，multipart `image[]` 正常，1024×1024 PNG（**Color Type 6 RGBA**，1,166,889 bytes） |

本次图生图落到的路由**执行了** `background=transparent`：响应回显 `background: transparent`，Alpha 0–254，四角 Alpha 0，808,476 全透明像素，219,501 像素 ≥128。按 `skill/gpt-image-generation.md` 的路由指纹规则（回显含 `background` → 原生 RGBA）判定为 G 型后端；**这不是永久能力，下一次仍必须实测 Alpha**。

产物目录：`C:/Users/Administrator/Pictures/gptGen/workflow-check-2026-10-22/`（含 `.request.json` / `.response.json` 原始记录）。

## 图生图验收实测数值（可作后续对比基线）

输入是文生图产出的灰底武将图，prompt 为语义去背（ONLY background removal）。

- 主体包围框：输出 394×991 vs 源 397×996（比值 0.992 / 0.995），底边差 −1px → **没有历史案例里的整体放大/位移**。
- 核心实心像素保留率（源主体逐像素比对）：±10 = 5.34%，±30 = 31.62% → 画面被重绘（去背必然重绘 matte），但远好于历史「全量重绘」失败样本（±30 仅 5.54%）。
- 边缘污染：边缘像素均值 RGB (96.2, 51.5, 45.1) vs 主体核心 (97.1, 51.5, 45.0)，只有 0.07% 边缘像素接近源底灰 (196,201,204) → 无灰边。
- 环亮度比（1–4px）0.90–0.94，高于已认可基线 0.58/0.79/0.86 → 边缘至少不脏于基线（该指标按本文口径近似计算，历史文档未给出精确定义）。
- 视觉：白底与深底合成都干净，无光晕、无棋盘格、无脏边。

## 两条会重复付费 / 误判的坑

1. **先落盘图片，再清洗响应 JSON。** 响应只有 `b64_json`（本次 1,705,884 字符）时，如果先把 `b64_json` 替换成 `<base64 omitted>` 再写文件，图片数据会被自己覆盖掉；服务端已经计费完成，只能重新提交一次（本次因此多付了一次文生图）。顺序必须是：`store_image()` → `save_response()`。
2. **Alpha 直方图求 max 时注意偏移。** `enumerate(reversed(hist))` 的下标 i 对应 alpha 值 `254 - i`，直接拿 i 当 alpha 会把 max 254 误报成 1，看起来像「整张图全透明」的假故障。正确写法：`next((254 - i for i, c in enumerate(reversed(hist)) if c), None)`。

## 体检脚本

本次临时脚本：`Library/Locus/tmp/image_workflow_check/gpt_image_call.py`（t2i / i2i 两种模式，自动保存请求 JSON、响应 JSON、图片并输出 PNG 签名 / IHDR / Alpha 统计）。`Library/` 会被 Unity 清理，只作为临时工具；可复用的是它的请求结构（multipart 字段名必须是 `image[]`）和验收步骤。

## 2026-11-08 复检：默认模型切到 `gpt-image-2.6-sunburst`

用户指定「后续都用 2.6 代替 2.5，其他参数不变，质量用 `max`」。切换前跑探针：

| 探测 | 结果 |
| --- | --- |
| `GET /v1/models` | HTTP 200；图像模型**只有 `gpt-image-2.6-sunburst`**（2.5 当天已不在列表）；其余为 `claude-haiku-5-5` / `gpt-6-astra` / `gpt-6-luna` / `gpt-6.1-sol` / `gpt-6.1-terra` / `grok-4.7` |
| `POST /v1/images/generations`，model=`gpt-image-2.6-sunburst` | **HTTP 200，87.66s**，1024×1024 PNG（Color Type 2 RGB，837,807 bytes），usage total 1777 |

- 结论：2.6 可用 → **默认模型从 2.5 改为 2.6**；参数只改模型名，`quality=max` 等其余参数一律不变。
- 探针脚本 `Library/Locus/tmp/1013art/probe_gpt_image_2_6.py`，留档 `C:/Users/steam/Pictures/gptGen/probe_gpt_image_2.6/probe_2_6.{png,result.json}`。
- 工作流文档 `skill/gpt-image-generation.md` 同步更新（含模型默认值变更记录章节）。
- 仓库里写死模型名的 **125 个 Python 脚本**已统一为 `gpt-image-2.6-sunburst`（含 `Tools/visual_brief/` 两个正式提交脚本与 `Library/Locus/tmp/` 下 117 个临时脚本），全部通过 `py_compile`。历史记录类文件（`*.request.json` / `*.response.json` / `*.status.json` / 交接 md）**保持原样不改**。
- 提醒（沿用 11-06 结论）：中转站会当天反复开关模型，**不要**把这次的「只有 2.6」写死为长期事实；批量生图前仍应先跑一次零成本探针。

## 2026-11-06 复检：2.6 与 2.5 同时可用，**默认用 2.5**（已被 2026-11-08 的切换取代）

中转站在 2026-11-06 一天内来回切过两次路由（下午先只开 2.6，几十分钟后又把 2.5 开回来）。当前 `GET /v1/models` 共 8 个模型，图像模型两个都在：

`gpt-image-2.5-sunburst`、`gpt-image-2.6-sunburst`

**选型结论：默认用 `gpt-image-2.5-sunburst`**（2.6 保留作备选）。理由是实测 2.5 明显更便宜：同一 prompt / 同一 `size=1024x1024` / 同一 `quality=high`，2.5 的 image token 为 **1756**，2.6 为 **7024**，约差 4 倍。

### 探测矩阵（2026-11-06 15:40 前后）

| 探测 | 结果 |
| --- | --- |
| `GET /v1/models` | HTTP 200；`gpt-image-2.5-sunburst` + `gpt-image-2.6-sunburst` 同时在列表 |
| `GET /v1/models/{id}` | 该子端点未实现，对列表内模型也回 `model_not_found`，**不可用作探针** |
| `POST /v1/images/generations`，model=`gpt-image-2.5-sunburst` | 模型校验通过；空 prompt + `size=1x1` 探针得到 HTTP 400 参数错 |
| `POST /v1/images/generations`，model=`gpt-image-2.6-sunburst` | 同上，HTTP 400 参数错 |
| `POST /v1/images/generations`，model=`gpt-image-2.6` | HTTP 403 `This token has no access to model gpt-image-2.6` |
| `POST /v1/images/generations`，model=`gpt-image-2.5` | HTTP 403 `This token has no access to model gpt-image-2.5` |
| `POST /v1/images/generations`，model=`gpt-image-2.6-fake-xyz` | HTTP 403（403 假名基线） |
| 真实文生图 `gpt-image-2.5-sunburst` | HTTP 200，47.06s，1024×1024 PNG（Color Type 2 RGB，884,878 bytes），画面符合 prompt；usage total 1769 |
| 真实文生图 `gpt-image-2.6-sunburst` | HTTP 200，1024×1024 PNG（828,295 bytes）；usage total 7037 |

两条硬结论：

1. **不带后缀的写法（`gpt-image-2.5` / `gpt-image-2.6`）一律不可用**，403 与假名完全同型。模型名必须带 `-sunburst` 后缀。
2. **中转站会当天反复开关模型**，不要根据上一次结果写死。上批量生图前先跑一次零成本探针（下面的 400/403 判别法），再用真实模型名。

### 零成本探针写法（可复用）

对 `/v1/images/generations` 发一个 `prompt=""` + `size="1x1"` 的请求：

- 令牌无权访问的模型 → **403 muskapis_error**（"This token has no access to model ..."）
- 模型有权但参数非法 → **400 invalid_request**（"请求参数有误或模型不可用"）

两种情况都在真正生成前失败，**不产生计费**。配合 `GET /v1/models` 就能确定当下该用哪个模型名。

### 影响面

仓库里 84 个写死模型名的 Python 脚本已统一为 `gpt-image-2.5-sunburst`（多数在 `Library/Locus/tmp/`，一份在 `_art_process/Authoring/talent_ui/scripts/`，后者内容与 HEAD 一致、git 状态干净），全部通过 `py_compile`。历史记录类文件（`request-spec.json` / `production_record.json` / 交接 md）**保持原样不改**，它们记录的是当时真实发出的请求。
