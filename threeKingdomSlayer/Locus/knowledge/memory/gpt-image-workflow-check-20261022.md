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
| `GET /v1/models` | HTTP 200；图像模型只有 `gpt-image-2.5-sunburst`（`gpt-image-2.5-flare` 不在列表，与历史 403 一致） |
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
