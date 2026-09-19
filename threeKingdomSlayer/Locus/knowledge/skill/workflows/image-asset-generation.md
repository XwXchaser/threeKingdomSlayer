---
id: wf_5c2e2f94-8f71-4cd8-9f41-1f4c4d5f5c0a
injectMode: inherit
summary: 需要通过 GPT Image 接口生成、编辑或融合图片，并完成文件与 Alpha 验收后交付或导入 Unity 时使用；含透明背景两级验收、失败处理与交付清单。
skillEnabled: true
skillSurface: command
aiEditMode: inherit
---

# GPT Image 素材生成与验收工作流

## 目的

用于需要通过 GPT Image API 生成、编辑或融合图片，并将结果作为本地素材交付或导入 Unity 的任务。该工作流负责从需求确认到最终验收，不等同于 API 技术 Skill。

配套技术 Skill：`skill/gpt-image-generation.md`

## 何时使用

- 生成角色立绘、头像、道具图标、背景或 UI 素材。
- 对已有图片进行抠图、换背景、改风格或保留主体编辑。
- 融合多张参考图。
- 要求透明背景、PNG、Alpha 通道或 Unity 可导入素材。

## 工作流总览

1. 明确素材用途、主体、画风、构图、尺寸、背景和输出格式。
2. 判断任务类型：文生图、单图编辑或多图融合。
3. 读取相关项目美术规范、参考素材和已有资产，避免题材正确但风格不一致。
4. 检查 `MUSK_API_KEY` 是否存在；不要把 Key 写入项目、文档、日志或回复。
5. 读取 `skill/gpt-image-generation.md`，按任务类型选择 endpoint 和调用模板。
6. 组织 prompt，并明确参考图的职责、优先级和不可改变内容。
7. 选择版本化输出路径；未指定时使用 `C:/Users/steam/Pictures/gptGen/`。
8. 实际调用 API，保存原始响应 JSON 和图片文件。
9. 进行文件格式、尺寸、文件大小和 Alpha 等机器验证。
10. 读取图片进行视觉验收，判断是否可直接使用、需重做或需后处理。
11. 只有 API 成功、文件落盘、验证通过并完成视觉检查后，才能向用户报告“已生成”。
12. 回复中交付完整真实路径，并说明验证结果和剩余限制。

## 任务类型与接口选择

### 文生图

使用：

```text
POST https://api.muskapis.com/v1/images/generations
Content-Type: application/json
```

只发送文字 prompt 时使用此接口。

### 单图编辑 / 多图融合

使用：

```text
POST https://api.muskapis.com/v1/images/edits
Content-Type: multipart/form-data
```

本地图片必须通过同名字段 `image[]` 上传。多图融合时重复上传多个 `image[]`，不能把本地路径只写在 prompt 中，也不能将本地图片直接放进 JSON body。

参考图职责建议：第一张用于严格锁定角色身份和造型，后续图片用于构图、像素风格、UI 语言或其他指定参考。prompt 中要明确优先级，避免把 Boss 或其他参考图的华丽元素错误转移到普通角色。

## 透明背景专项工作流

### 推荐请求参数（已验证的文生图分支）

以下实测仅适用于 `gpt-image-2.5-sunburst` + `/v1/images/generations` 的透明请求，不可自动外推到编辑接口。能力需按服务商、模型、endpoint、参数组合核查。

本次 `/v1/images/edits` 使用同名模型并带 `background=transparent` 返回HTTP 400：`Transparent background is not supported for this model.` 去掉参数后编辑成功，但有RGB白底或被绘入棋盘格的结果；切换 `gpt-image-2` 返回当前令牌无权访问的403。这是当时接口/权限观察，不证明模型永久不支持编辑透明，也不证明换令牌必然支持。

不能因编辑接口拒绝透明就声称先前透明测试失败；也不能为透明通道而悄悄放弃必需角色参考图。需要保持角色身份时，先查服务文档/能力，或说明并采用“参考图编辑＋纯色临时底＋后处理”路线，不盲目付费试错。

```json
{
  "prompt": "A single red apple with a small green leaf, centered, isolated product cutout, no shadow, transparent background",
  "model": "gpt-image-2.5-sunburst",
  "size": "1024x1024",
  "quality": "high",
  "background": "transparent",
  "output_format": "png",
  "n": 1
}
```

对已支持原生透明的分支，请求同时指定：

```text
background: "transparent"
output_format: "png"
```

只在 prompt 中描述“透明”，或只指定 PNG，都不能证明输出具有透明像素。JPEG 无法保留 Alpha 通道。

### 推荐透明 prompt 补充

根据素材需求，可加入：

```text
pure transparent background, no halo, no glow, no environmental color spill, clean sharp cutout edges
```

这些文字不能替代验收；模型仍可能生成半透明光晕、渐变背景或环境色溢出。

### 两级验收标准

#### 一级：Alpha 通道有效

机器检查至少确认：

- 文件是可读取的 PNG。
- PNG 为 RGBA，或其他确实包含 Alpha 的格式。
- Alpha 最小值小于 255。
- 四角或边缘存在透明像素。
- 图片尺寸符合任务要求。

#### 二级：干净透明抠图

通过图片预览检查：

- 主体外区域视觉上确实为纯透明。
- 没有棋盘格被模型实际绘入图片。
- 没有红色、绿色或其他彩色光晕。
- 没有渐变背景、地面阴影或环境色溢出。
- 主体边缘没有明显脏边或过度半透明。

只有通过二级验收，才可直接作为 Unity 角色、道具或 UI 素材使用。一级通过、二级失败时，应标记为“带透明通道但需抠图/后处理”，不能直接导入。

### 已验证案例

`gpt-image-2.5-sunburst` 已实际成功生成透明 PNG：HTTP `200`、`1024x1024`、RGBA、约 `1.76 MB`，Alpha 范围 `0–254`，约 49 万个完全透明像素。

测试文件：

```text
C:/Users/steam/Pictures/gptGen/gpt-image-2.5-sunburst-transparent-test.png
```

该案例通过了一级 Alpha 验收，但主体周围有明显红色/绿色光晕，未通过二级干净抠图验收。这是本工作流的重要边界案例：API 成功和 Alpha 有效，不等于素材可直接使用。

### 白底编辑结果的后处理与验收

- 保留 `_raw.png` 和原始响应；后处理文件另存，明确它是抠图Alpha，不是API原生透明。
- 使用 Pillow 等成熟图像库；先读实际PNG格式、尺寸和Alpha。不要仅凭文件后缀或响应字段判断。
- 对不透明像素角色，可从边缘洪泛移除连通近白底，避免全局 `white→transparent` 删除盔甲/眼睛高光。检查封闭空隙是否残留背景，以及边缘白边。
- 在深色/棋盘预览上检查抠图；统计透明像素不能证明动作或边缘质量合格。`read` 可直接查看PNG，不要用ASCII/颜色直方图代替视觉验收。
- 渐隐盾牌、烟雾、半透明像素不适用这种硬阈值抠图，需要单独处理透明度与背景颜色污染。
- 角色身份与受力姿态分别验收；不强行复用失败生成图。已验收姿态可以作为明确分工的动作参考，用户认可部分应保留，只调整未通过部分。

## 文件保存与验证

- 输出目录自动创建；默认 `C:/Users/steam/Pictures/gptGen/`。
- 每次使用版本化文件名，例如 `<asset>_v1.png`。
- 同时保存原始响应，例如 `<asset>_v1.response.json`。
- 对 `data[0]` 同时兼容 `b64_json` 和 `url`：前者 base64 解码，后者下载到本地。
- PNG 至少检查文件存在、大小大于 1KB、PNG 签名 `\\x89PNG\\r\\n\\x1a\\n`、尺寸和可读性。
- 不要把 curl 直接输出的 JSON 文件误当成图片；先解析响应再解码或下载。
- 如使用 Unity，测试图默认不要写入项目；只有用户明确要求纳入项目时，才输出到 `Assets/...`，并继续执行 Unity 资产导入与检查。

## 失败处理

- `Missing MUSK_API_KEY`：检查当前进程环境变量，设置后重启终端或 Locus。
- `401/403`：检查 Key 有效期、权限和额度，不要把 Key 内容写入日志。
- `408`、网络断开或 `5xx`：先保留原始响应和请求记录，确认服务端是否可能已经开始生成；未经确认不要盲目重复提交，以免重复计费。
- `429`：降低请求频率，优先遵守 `Retry-After`。
- `edits` 失败：确认使用 multipart、字段名为 `image[]`，而不是 JSON。
- 没有 `url` 或 `b64_json`：保存并检查完整原始响应，不得宣称生成成功。
- 视觉效果不符合需求：补充主体、风格、构图、背景、文字和不可改变内容；编辑任务可按需提高 `input_fidelity`。
- 透明背景有光晕或脏边：先标记为不合格；尝试更严格 prompt，仍不合格时进入抠图/后处理或重做流程。

## 已积累的素材经验

- 为特定武器/特效制作形变帧前，先读 `memory/stab-vfx-generation.md`：内含可复用的提示词与参考图策略、失败模式（生成模型的“局部编辑”仍会重绘整图、矩形分段缩放会产生接缝），以及“当前工具链不适合可靠产出同源极限帧”的结论。

## 交付清单

回复用户前逐项确认：

- [ ] 本轮确实执行了 API，而不是只读取文档或准备 prompt。
- [ ] API 返回成功且包含图片结果。
- [ ] 图片已保存到真实路径。
- [ ] 原始响应已保留。
- [ ] 文件格式、大小、尺寸和必要的 Alpha 检查已完成。
- [ ] 已进行视觉检查，并明确说明是否有光晕、脏边或其他限制。
- [ ] 回复中的路径与实际文件路径完全一致。
- [ ] 用户要求看图时，已附图或说明无法附图但提供了路径。

事实边界：没有实际调用、落盘和验证，就不能说“已生成”“已完成”或“图片就在上方”。

## 安全与成本注意事项

- 不要泄露 `MUSK_API_KEY`。
- 不要把 Key 写入仓库、知识库、日志或截图。
- 超时后不要立即重复提交；先确认原请求状态。
- 远程 URL 是中间产物，需要长期保存时必须下载到本地或自有存储。
