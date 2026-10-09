---
id: wf_5c2e2f94-8f71-4cd8-9f41-1f4c4d5f5c0a
injectMode: inherit
summary: 需要通过 GPT Image 生成、编辑、融合图片或对视频原始帧进行语义去背时使用；含质量档位、原生透明、后处理抠图、浅深底验收与动画一致性检查。
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
7. 选择版本化输出路径；默认按当前运行主机的 Windows 用户目录解析为 `<当前用户目录>/Pictures/gptGen/`，不要把另一台主机的用户名硬编码为全局默认。若用户明确指定目录，优先使用用户指定目录。
8. 实际调用 API，保存原始响应 JSON 和图片文件；如工作流跨主机运行，按当前主机实际路径另存或复制，并在交付中列出最终真实路径。
9. 进行文件格式、尺寸、文件大小和 Alpha 等机器验证。
10. 使用 `read` 回读本轮最关键的成品图或主候选，在当前对话中实际附图，同时进行视觉检查，判断是否可直接使用、需重做或需后处理；不能只输出路径或文字描述。
11. 只有 API 成功、文件落盘、验证通过并完成视觉检查后，才能向用户报告“已生成”；“用户已看到/已验收”必须有用户反馈，不能从工具附件回执推断。
12. 回复中同时交付关键图和完整真实文件路径（不用省略号缩写），并说明验证结果和剩余限制，具体见下文“关键成品图的对话交付”。若本次产出是后续收费任务（如视频生成）的输入，必须等用户对该产出给出验收结论后，才能进入下一步；不得因“它不会被上传”而把两步合并提交。

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

### 请求参数选择与透明能力

当前 `gpt-image-2.5-sunburst` 和 `gpt-image-2.5-flare` 文档列出的质量档位为：

```text
low | medium | high | xhigh | max | auto
```

- 草稿或低成本试探使用 `low` / `medium`。
- 常规最终候选使用 `high`。
- 需要最高当前模型质量时可试 `xhigh` 或 `max`；`max` 是当前已知最高档位，没有证据表明存在更高档位。
- 参考图编辑通常同时使用 `input_fidelity=high`，但它只提高参考保真倾向，不能锁死局部重绘。
- `max` 可能带来更丰富的细节，也可能重绘更多局部；必须比较角色身份、姿态、边缘和素材可用性，不能只看文件更大。

原生透明请求应同时指定：

```text
background: "transparent"
output_format: "png" 或 "webp"
```

只在 prompt 中写 `transparent`，或只指定 PNG，都不能证明输出存在有效 Alpha。透明能力必须按“模型 + endpoint + 当前路由/令牌 + 参数组合”实测，不能从文生图成功外推到编辑接口，也不能从一次失败永久推断接口永远不支持。

历史观察：同名模型的编辑接口曾对 `background=transparent` 返回 HTTP 400 `Transparent background is not supported for this model.`；去掉参数后编辑成功，但曾得到 RGB 背景或绘入的棋盘格。该记录保留用于解释旧结果，不覆盖后续成功实测。

当前已验证的编辑透明分支：`gpt-image-2.5-sunburst` + `/v1/images/edits` + `quality=max` + `input_fidelity=high` + `size=1024x1024` + `output_format=png` + `background=transparent` 返回 HTTP 200，并得到 RGBA PNG。

```json
{
  "prompt": "A single red apple with a small green leaf, centered, isolated product cutout, no shadow, transparent background",
  "model": "gpt-image-2.5-sunburst",
  "size": "1024x1024",
  "quality": "max",
  "background": "transparent",
  "output_format": "png",
  "n": 1
}
```

参考图编辑的已验证参数模板：

```text
model: gpt-image-2.5-sunburst
quality: max
input_fidelity: high
size: 1024x1024
output_format: png
background: transparent
```

提交前仍要确认本次收费规格；不要把模板本身当作已执行请求。

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
- PNG 实际包含 Alpha：优先确认 `RGBA` 模式或 PNG Color Type `6`，不能只看扩展名。
- Alpha 存在有效范围：至少有透明像素或边缘透明像素；记录 `alpha_min`、`alpha_max`、透明/半透明像素数量。
- 四角或边缘存在透明像素。
- 主体区域不能因为模型生成的氛围光、背景渐变或错误抠图而整体变成半透明。
- 图片尺寸符合任务要求。

注意：Alpha 最大值不必达到 `255` 才能通过透明检查。`253/254` 仍接近完全不透明；不能仅因非零 Alpha 都小于 255 就标记主体透明度异常。应检查主体区域的 Alpha 分布与浅/深底合成效果，确认没有明显透底或低 Alpha 光晕；确需修正时另存后处理版本，不自动覆盖 API 原始输出。

#### 二级：干净透明抠图

通过图片预览检查：

- 主体外区域视觉上确实为纯透明。
- 没有棋盘格被模型实际绘入图片。
- 没有红色、绿色或其他彩色光晕。
- 没有渐变背景、地面阴影或环境色溢出。
- 主体边缘没有明显脏边或过度半透明。

判定“画入棋盘格”的方法（不能只靠肉眼看查看器）：先取背景区域一行的明暗游程，看是否周期性交替（本案例约 16/17 px）；再查背景灰度直方图是否只有两级（本案例 245/254）。命中即为模型把棋盘格画进像素，此时颜色类型通常是 `2`(RGB)。查看器里显示的“透明格子”不能作为透明度证据，必须读 PNG Color Type 或 IHDR。

边缘与光晕必须与已认可基线比对，不要使用绝对阈值：本案例已认可的 f000 结果为 1 px 环亮度比 0.58、2 px 0.79、3 px 0.86，后续输出只要同量级即可，不能因为不等于 1.0 就判不合格。

几何漂移是独立的固定验收项：记录输出前景框与原帧主体框的宽高差与底边差（本案例放大 26%–32%、底边下移 93–98px），判断能否直接当动画帧。

只有通过二级验收，才可直接作为 Unity 角色、道具或 UI 素材使用。一级通过、二级失败时，应标记为“带透明通道但需抠图/后处理”，不能直接导入。

### 已验证案例

#### 文生图透明测试

`gpt-image-2.5-sunburst` 已实际成功生成透明 PNG：HTTP `200`、`1024x1024`、RGBA、约 `1.76 MB`，Alpha 范围 `0–254`，约 49 万个完全透明像素。

测试文件：

```text
C:/Users/steam/Pictures/gptGen/gpt-image-2.5-sunburst-transparent-test.png
```

该案例通过了一级 Alpha 验收，但主体周围有明显红色/绿色光晕，未通过二级干净抠图验收。这是本工作流的重要边界案例：API 成功和 Alpha 有效，不等于素材可直接使用。

#### 参考图编辑透明测试：持剑敌军 combat idle v4

本次用户已确认形象方向，并实际验证当前路由可以在编辑接口返回原生透明 PNG：

```text
model: gpt-image-2.5-sunburst
endpoint: /v1/images/edits
quality: max
input_fidelity: high
size: 1024x1024
output_format: png
background: transparent
HTTP: 200
PNG: RGBA / Color Type 6
四角 Alpha: 0
Alpha 范围: 0–254
完全透明像素: 701,027
文件大小: 1,276,936 bytes
```

输出文件：

```text
C:/Users/Administrator/Pictures/gptGen/sword_enemy_combat_idle_v4_max_transparent.png
```

原始请求参数和响应：

```text
C:/Users/Administrator/Pictures/gptGen/sword_enemy_combat_idle_v4_max_transparent.request.json
C:/Users/Administrator/Pictures/gptGen/sword_enemy_combat_idle_v4_max_transparent.response.json
```

该结果证明当前模型、endpoint、令牌和参数组合的原生透明编辑可用；不能保证其他账号、模型路由、时间或输入图片相同。视觉上角色细节和边缘比 v3 更丰富，但 `max` 对参考图进行了明显重绘，仍需单独验收角色身份、姿态、透明边缘和半透明光晕。它没有自动替换 v3，也没有导入 Unity。

### GPT Image 语义去背（复杂灰底/视频原始帧）

当角色与背景在颜色上高度接近，例如银灰剑刃/铠甲叠在暗灰视频底上，传统颜色阈值或边缘洪泛可能留下明显灰边。此时可以把从视频解码得到的**原始 RGB 帧**作为单图编辑输入，让 GPT Image 按角色语义进行去背和 Alpha matte 清理。

#### 已验证参数与成功结论

持剑敌军 Idle 的原始视频首帧使用以下组合返回 HTTP `200` 和原生 RGBA PNG，用户明确评价“抠得很好”，认可单帧去背的视觉质量。浅色背景上的灰边较先前手动候选明显改善；这证明语义去背是可采用的分支，不证明每次都优于传统方法。

```text
model: gpt-image-2.5-sunburst
endpoint: /v1/images/edits
quality: high
input_fidelity: high
size: 与输入原始帧相同（已实测 960x960）
background: transparent
output_format: png
image[]: 未标注、未去背的实际视频 RGB 帧
```

本测试没有把 `high` 与 `max` 做对照，不能声称降低质量档位必然消除重绘。一次性输入/输出路径、完整请求与响应、Alpha 数值及位置变化记录在 `memory/video-generation-case-log.md` 的“持剑敌军 v4 Idle 与 GPT Image 单帧去背”条目，工作流只保留可复用规则。

#### 执行规则

1. 先从 MP4 解码并保存无文字、未抠图、未缩放的原始 RGB 帧；不要把已有脏边的透明候选作为唯一输入。
2. 只测试一帧，保存版本化输出、完整 Prompt 和原始响应；用户认可后再逐帧处理其余帧，不要直接批量收费试错。
3. Prompt 明确任务是 `ONLY background removal and alpha-matte cleanup`，同时锁定原画布、角色位置、绘制比例、脚底、剑尖、双手、双脚、颜色、亮度和金属高光；禁止增强、重绘、换姿态和换背景。
4. 机器检查 PNG、RGBA、尺寸、Alpha、四角透明和透明/半透明像素数量；至少在浅色/白底和深色背景上各做一次视觉检查。浅色背景是发现灰边的主验收背景，深色背景不能单独证明去背干净。
5. 对动画帧还必须比较 Alpha 阈值下的前景包围框、脚底坐标、剑尖位置和角色比例。GPT Image 即使返回相同画布，也可能重绘、放大或移动角色；同画布不代表锚点不变。若发生变化，单独记录几何差异并验收对齐副本，不把位置变化等同于去背视觉失败，也不直接替换正式 Sprite 序列。
5b. **动画专用：必须先做「帧间倍率一致性」择优（2026-10-04 实测新增）**。实测结论：**模型不重画姿势，但会把整幅画随机放大**——用高度归一化的形状比对（对缩放不变）可确认同姿势（shapeIoU 0.946–0.976），而每帧“需要的回缩倍率”会散得很开（同一批 6 帧实测 0.740–1.000；同一帧不同次调用：0.820 / 0.917 / 0.958，另一帧 0.791 / 0.933 / 0.740）。因此**单帧视觉通过 ≠ 动画可用**。做法：①逐帧算形状 IoU 与所需倍率；②倍率偏离 1.0 超过 ~3–5% 的帧换时间/新路由**重试**（保留 v1/v2/v3 全部候选，不堆提示词）；③最终只对 outlier 帧做整幅缩放，其余零重采样；④平移用**单一全局偏移**，**禁止逐帧锚脚/逐帧包围框归一化**（会把循环动作的腾空/起伏相位抹掉）。
6. 单帧“去背视觉通过”和“动画锚点/帧间一致性通过”是两个独立结论。若模型改变了位置或比例，输出可作为静态透明候选、抠图参考或后续对齐参考，不能直接部署为动画序列。

推荐 Prompt 核心：

```text
Remove the dark gray background from the provided existing game animation frame. This is ONLY background removal and alpha-matte cleanup, not image generation, not an enhancement, not a redesign, and not a new pose.
Preserve the original canvas size, character location, drawing scale, foot positions, silhouette, sword, hands, armor geometry, colors, brightness, metallic highlights and pixel-block details exactly. Do not move, recenter, crop, trim, resize, rotate, mirror, redraw or polish the character.
Produce a clean true RGBA transparent PNG. Remove gray-background contamination from the contour so it is clean on a pure white background as well as a dark background. No gray halo, black matte fringe, light halo, colored fringe, blur, semi-transparent armor, checkerboard or replacement background.
```

该分支不能替代 Alpha 与视觉验收，也不能保证模型保持逐像素坐标；动画任务必须保留原视频帧作为锚点基准。

#### 路由决定原生透明能力（本案例实测）

同一份请求（`/v1/images/edits` + multipart `image[]` + `background=transparent` + `output_format=png` + 同一参数组 + 逐字相同 Prompt）在同一环境的多次调用分别命中了不同图片路由：部分返回原生 RGBA，另一条返回 `color_type=2`(RGB) 并把棋盘格画入像素；对失败路由追加“禁止画棋盘格、必须真实 alpha”的 Prompt 条款没有改变结果。

- 原生透明失败时，先做机器检查再决定动作；**优先换时间/等路由变化重试，不要靠不断堆 Prompt**。
- 每次请求记录路由指纹：`data[0].url` 的 host、响应顶层/回显字段与 usage 用量（只记 host，不保存签名 URL 全文）。累计证据：回显含 `background` → 原生 RGBA 3/3；回显不含 `background` → RGB + 画入棋盘格 3/3；无回显 → 原生 RGBA 1/1；失败路由用量稳定偏大（input 1445 / output 6732）对比成功路由（870 / 1683）。样本仍少，作为强线索而不是定论，不能据此跳过实际 Alpha 检查。
- 不要因一次透明失败就否定已验收视频或要求重新生成视频，也不要在同一失败路由上连续付费试错。

### 白底编辑结果的后处理与验收

- 保留 `_raw.png` 和原始响应；后处理文件另存，明确它是抠图 Alpha，不是 API 原生透明。原生透明输出也应保留原始响应和版本化图片，不要只保留最终导入文件。
- 使用 Pillow 等成熟图像库；先读实际PNG格式、尺寸和Alpha。不要仅凭文件后缀或响应字段判断。
- 对不透明像素角色，可从边缘洪泛移除连通近白底，避免全局 `white→transparent` 删除盔甲/眼睛高光。检查封闭空隙是否残留背景，以及边缘白边。
- 若返回的“背景”实际是模型画入的规则图案（棋盘格等），只做边缘洪泛会漏掉被四肢、剑与身体包住、与边界不连通的那部分；此时应按图案灰阶判定并一并抠除（本案例实测遗漏区域 2,073 px），再对剪影边缘做颜色去污染（用内侧邻近色替换环上颜色，保留 alpha 与剪影）。
- **绿幕优于近白底（实测）**：当生成输入本身就是绿幕（为抠图而选）时，直接对**原始帧**做“绿优势阈值 + 绿通道封顶 + 1px 环颜色替换”，实测 1px 环亮度比 **0.63–0.67**，与 GPT 原生 alpha 基线 **0.61** 同级，且几何无需重采样；而把模型输出的**近白底**再键得到 **2.31–2.61** 的亮白边、底抠不净、且逐帧尺寸漂移 ±7%。推论：要抠就尽量在**输入阶段**用绿幕，别指望把模型画的浅色底事后抠干净。
- 在深色/棋盘预览上检查抠图；统计透明像素不能证明动作或边缘质量合格。`read` 可直接查看PNG，不要用ASCII/颜色直方图代替视觉验收。
- 渐隐盾牌、烟雾、半透明像素不适用这种硬阈值抠图，需要单独处理透明度与背景颜色污染。
- 角色身份与受力姿态分别验收；不强行复用失败生成图。已验收姿态可以作为明确分工的动作参考，用户认可部分应保留，只调整未通过部分。

### 透明素材部署进 Unity 的尺寸与密度约定

新单位的透明帧落地时，**不许改图片的导入设置**（PPU、Pivot、Filter、Compression、MeshType 全项目统一），密度差一律放到**单位 transform 的 scale** 上。

**密度关系**：世界尺寸 = 素材像素高 ÷ PPU × 单位scale。本项目敌人基准（enemy101）：角色 280px、PPU 16、Prefab scale 0.18 → 角色世界高 3.15。项目现有敌人本来就使用 0.18 / 0.20 两种单位 scale，所以“按单位缩放”是既有做法。

**高分辨率素材（如视频抽帧，角色 606px）的落地公式**：

1. 素材**按原生像素补边**，不重采样：画布高 = 角色px × 512/280；脚底自底 = 画布高 × 97/512；**横向不要重锚定——把源画布居中放入目标画布**（本案例 960→1108 即 x+74）。**不要按“脚心对齐画布中心”**：源素材的脚心未必在源画布中心（本案例脚心在 x=623、画布中心 480），强行对齐会把整幅画推偏（本案例左推 **0.74 世界单位**，列距 1.0 → 看起来正好差一列，且与邻兵重叠）。
2. 导入设置保持与基准逐项相同：PPU 16、Center pivot、Point、Uncompressed、Tight、maxSize 2048。
3. 单位 scale = 3.15 × 16 ÷ 角色px（即 50.4 ÷ 角色px）。
4. 碰撞盒 local size 与 center = 基准值 × (0.18 ÷ scale)，保证世界碰撞盒与基准一致，否则缩放会让受击范围变小。
5. `EnemyHealthBar.yOffset` = 16.3 × (0.18 ÷ scale)（基准的推导值），保证血条世界高度一致。

**为什么不能采用其它做法**：

- “PPU 34.695 + scale 0.18” 与 “PPU 16 + scale 0.083168” 世界结果完全等价；PPU 是全项目共享的密度标准（每张图都要写），scale 只属于单个单位，所以密度差必须落在 scale。
- **不要按左边缘对齐**：角色左边缘会随武器/姿态变化（同一单位不同帧相差可达 1.5 倍），按左边缘对齐会把身体推离站位。角色一律按**脚底**对齐。
- **不要把渲染器挪到子物体**：`Enemy.cs` 与 `EnemyHealthBar` 都用 `GetComponent<SpriteRenderer>()` 取**根节点**渲染器（受击闪色、描边/穿透高亮），挪到子物体会让这些功能静默失效。
- **不要改基准单位** 的 scale、碰撞盒或图片设置；只在新单位上做补偿。

**部署后的机器验收**（与基准单位逐项比对，缺一项不能算完成）：

- 精灵世界框高度（`Renderer.bounds`）与基准一致（本项目 5.76）
- 脚底世界 y 与基准一致（本项目 −1.789）
- 碰撞盒世界尺寸与中心（`Collider.bounds`）与基准一致（本项目 0.502 × 1.287 × 0.036，中心 y 0.0246）
- 血条世界高度（`yOffset × lossyScale`）与基准一致（本项目 2.934）
- 角色可见高允许随姿态小幅变化（本项目六帧 3.07–3.21，均值 3.15）
- **角色质量中心（alpha 加权）相对变换原点的横向偏移与基准同量级**：本项目基准 101 = **−0.04 单位**；本案例被错锚时是 **−0.85 单位**（看起来差一列）。必须用源 PNG 的 alpha 算，不能肉眼或只看包围框判断。

## 文件保存与验证

- 输出目录自动创建。Windows 主机默认使用当前用户目录：`<当前用户目录>/Pictures/gptGen/`，例如 `C:/Users/steam/Pictures/gptGen/` 或 `C:/Users/Administrator/Pictures/gptGen/`。
- 不同主机可能使用不同 Windows 用户名；禁止仅凭历史路径判断当前输出位置。调用前先检查/创建当前主机的目标目录，调用后再检查文件是否真实落盘。
- 用户显式指定输出目录时，使用用户指定目录；本次任务的指定目录为 `C:/Users/Administrator/Pictures/gptGen/`。
- 每次使用版本化文件名，例如 `<asset>_v1.png`。
- 同时保存原始响应，例如 `<asset>_v1.response.json`。
- 若生成发生在另一台主机或另一用户目录，需将图片和原始响应复制到当前用户指定的交付目录；复制后重新检查目标文件存在性、大小和格式，不得只报告源文件路径。
- 对 `data[0]` 同时兼容 `b64_json` 和 `url`：前者 base64 解码，后者下载到本地。
- PNG 至少检查文件存在、大小大于 1KB、PNG 签名 `\\x89PNG\\r\\n\\x1a\\n`、尺寸和可读性。
- 不要把 curl 直接输出的 JSON 文件误当成图片；先解析响应再解码或下载。
- 如使用 Unity，测试图默认不要写入项目；只有用户明确要求纳入项目时，才输出到 `Assets/...`，并继续执行 Unity 资产导入与检查。

## 关键成品图的对话交付

本次R1参考图实践中，图片已生成、落盘并验证，但用户仍反馈“没有看到图片”；重新对同一PNG调用 `read` 后，图片再次以附件提供。可复用经验是：**生成成功、已发出附件、用户实际看到、用户目视验收是四个不同状态。**

1. **默认直接附图，不等待用户追问。** 每次生图交付，至少选择本轮最关键的成品或需要用户评审的主候选，生成结束并验证后，对其最终真实路径调用 `read`。仅输出API结果JSON、Python文件信息、下载链接或文字路径不能替代图片附件。
2. **图与路径一同交付。** 在同一次交付中实际附图，并在回复中明确列出该图的完整真实本地文件路径、版本、尺寸和必要限制。可以附加打开链接，但链接不能替代图片，也不能代替完整路径。多图任务优先展示最关键的一张，按评审需要再附其他图，避免大量过程图淹没主结果。
3. **没有看到时，重新附同一文件，不重新生图。** 用户反馈附件不可见，先核对原图存在且可读，再直接使用 `read` 重新附加已落盘的同一版本，并重复提供完整路径。不修改Prompt、不切换模型、不再次调用收费接口；附件显示问题不等于生成失败。
4. **无法附图时明确说明。** 若附件工具报错或当前渠道不支持展示，报告该限制，提供完整路径或可用打开方式，不声称用户已经看到。查不到原图或文件损坏时先诊断，确需重新生成仍须获得授权，不能自动付费补生。
5. **保留事实与验收边界。** 工具返回 `image: attached` 只证明工具已附加图片，不保证用户界面已显示；用户反馈不可见时以反馈为准。技术验证和AI视觉检查不等于用户审美验收；没有用户认可，不自动推进依赖该图的后续收费生成或场景部署。

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
- 本次持剑敌人概念图生成验证：当前令牌调用 `gpt-image-2` 返回 `403`（无模型权限），不能据此继续声称该模型可用；`gpt-image-2.5-sunburst` 的第一次多图编辑请求曾被服务端断开，重试后返回 `200` 并成功产出 PNG。模型可用性应以本次实际 HTTP 返回为准。
- 多图融合时，必须在 prompt 中分别声明参考图职责，并重复关键左右关系；本次要求“角色解剖学右手持唯一一把剑、画面左侧出剑，解剖学左手清晰可见且完全空置”，同时明确禁止盾牌、其他副武器和整图镜像，降低左右手与装备错误风险。
- API 成功后要同时保留图片和原始 JSON 响应。本次输出按用户指定主机目录保存到 `C:/Users/Administrator/Pictures/gptGen/`；最终交付应以该目标目录中的文件为准。
- 不能因为工具结果曾经返回图片附件、或请求已经发出，就声称“已生成”；必须确认 API 成功、目标文件落盘、目标文件可读，并在需要时回读图片完成视觉检查。

## 交付清单

回复用户前逐项确认：

- [ ] 本轮确实执行了 API，而不是只读取文档或准备 prompt。
- [ ] API 返回成功且包含图片结果。
- [ ] 图片已保存到真实路径。
- [ ] 原始响应已保留。
- [ ] 文件格式、大小、尺寸和必要的 Alpha 检查已完成。
- [ ] 已进行视觉检查，并明确说明是否有光晕、脏边或其他限制。
- [ ] 回复中的完整真实文件路径与实际交付图片一致，包含正确版本，不以省略路径或只有链接代替。
- [ ] 若发生跨主机/跨用户目录复制，已验证最终交付目录中的图片和原始响应，而不是只验证源目录。
- [ ] 本轮最关键的成品图或主候选已通过 `read` 实际附加到当前对话，不等待用户另行要求看图；无法附图时已说明限制并提供完整路径。
- [ ] 若用户反馈看不到图片，已核对并重新附加同一已落盘文件，未把附件显示问题误判为生成失败或擅自再次收费生成。
- [ ] 区分“已生成”“工具已附图”“用户已看到”“用户已验收”，不从工具回执推断后两项。

事实边界：没有实际调用、落盘和验证，就不能说“已生成”“已完成”或“图片就在上方”。

## 安全与成本注意事项

- 不要泄露 `MUSK_API_KEY`。
- 不要把 Key 写入仓库、知识库、日志或截图。
- 超时后不要立即重复提交；先确认原请求状态。
- 远程 URL 是中间产物，需要长期保存时必须下载到本地或自有存储。
