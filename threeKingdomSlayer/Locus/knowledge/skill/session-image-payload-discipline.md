---
id: kd_dd396324-0690-4f14-b787-4eefb81c5054
summary: 会话体积纪律：单次请求体上限 128 MB（超限返回 400 request body exceeds 128 MB，重试无效、历史图片无法回收）。规定截图分级预算（480/640/原图）、优先写盘截图+脚本统计只回数字、必须人眼判断时才出图且不重复读，含 400/413 判定表与撞线后的新会话续跑流程。
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
skillEnabled: true
skillSurface: command
---

# 会话体积纪律：截图与图像的取证方式

> 起因：2026-11-24 修无尽关地面时，单会话累计 17 张 1080×2160 Game 截图（46 MB PNG），
> 请求体越过上游 **128 MB** 上限，上游连续返回 `400 Bad Request: request body exceeds 128 MB`。
> 该错误**重试无效**，历史里的图片也**无法回收**，唯一出路是新开会话。

---

## 0. 硬事实（先记住这四条）

1. **单次请求体上限 = 128 MB**，超限返回：
   `400 Bad Request` + `"request body exceeds 128 MB: request body too large"`。
2. **base64 ≈ 原始字节 × 1.37**。图片进历史后一直按 base64 计。
3. **每一轮请求都重发全部历史** ⇒ 历史里每张图都是**常驻成本**，不是一次性成本。
4. 因此**图片总量**（不是单张大小）决定会不会撞线。

实测换算（本项目）：

| 资产 | 原始 | base64 |
|---|---|---|
| 1080×2160 Game 截图 1 张 | 2.5–2.7 MB | ≈ 3.4–3.7 MB |
| 同图降到 640 长边 | 60–120 KB | ≈ 90–165 KB |
| 同图降到 480 长边 | 30–50 KB | ≈ 45–70 KB |
| 512² 贴图 PNG | 0.5 MB | ≈ 0.7 MB |

⇒ **17 张原图就能吃掉 63 MB base64**，再叠加文本与其它图片即撞线。

---

## 1. 预算（按此规划，不要事后补救）

| 用途 | 长边 | 单张 | 建议上限 |
|---|---|---|---|
| 覆盖 / 几何 / 颜色统计 | 480 | ~40 KB | 20 张 |
| 观感速览 | 640 | ~90 KB | 10 张 |
| 交付给用户看图 | 0（原图） | ~2.5 MB | 2–3 张 |

**单会话累计图片原始字节控制在 30 MB 以内**（≈40 MB base64），留出文本与重试余量。

---

## 2. 默认流程：能不能用数字回答？

按顺序问自己：

1. **这个结论能不能只靠数字得出？**（覆盖率、帧差、相位残差、Draw 次数、投影坐标）
   → 能，就**不要出图**。见 §3。
2. **必须看图才能判断？**（构图、观感、艺术性、UI 排版）
   → 用 `unity_capture_viewport(max_long_edge=640)`，**一次一张**，看完即写结论，不复读。
3. **需要用户人眼验收？**
   → 图写进 `Library/Locus/tmp/…`，在回复里给路径，**由用户在本地看**，不要让模型 `read` 回来。

---

## 3. 写盘截图 + 脚本统计（首选，不进上下文）

`unity_capture_viewport` 会把 PNG 作为图像附件放进对话；**写盘截图不会**。

```csharp
// unity_execute（Play Mode）
ScreenCapture.CaptureScreenshot("Library/Locus/tmp/<task>/probe_d355.png");
await ctx.WaitFrames(3);
print("exists=" + System.IO.File.Exists(path) + " bytes=" + new System.IO.FileInfo(path).Length);
```

```bash
# 只统计，不返回图像
python - <<'PY'
from PIL import Image; import numpy as np
a = np.array(Image.open(p).convert("RGB"))
soil = (a[:,:,0].astype(int)-a[:,:,2].astype(int) > 22) & (a[:,:,0].astype(int)-a[:,:,1].astype(int) > 8)
print("soil_ratio=%.4f" % soil.mean())
PY
```

**必须做的有效性自检**：文件存在、字节数非零、且与另一个已知不同状态的图**差异非零**。
否则可能拿到空帧/缓存帧（历史上踩过）。

⚠ `ScreenCapture.CaptureScreenshotAsTexture()` 在 async 片段里可能返回 null；只用**写盘版**。
首次在新环境使用时先验证一次写盘是否成功再依赖。

---

## 4. 允许的例外

- 用户明确要求"给我看这张图"→ 出原图，但**同一会话不再重复**。
- 需要判定极细像素问题（1–2 px 破绽）→ 先裁剪出目标区域另存小图再 `read`，把 2.5 MB 压到 100 KB 量级。
- 文档/交接里**只写路径与结论数字**，不嵌图。

---

## 5. 诊断 400 / 413 的判定表

| 错误文本 | 含义 | 处置 |
|---|---|---|
| `request body exceeds 128 MB` | 图片/正文体积超限 | 新开会话；本会话不可继续 |
| `estimated_input_tokens` 逼近模型上限 | token 超限 | 压缩上下文或新会话 |
| `413 Payload Too Large` | 上游 nginx 层拦截 | 同上，体积问题 |

**排查命令**（Locus 会话日志）：

```bash
L="C:/Users/Administrator/AppData/Roaming/Locus/logs/sessions/<latest>.log"
grep -n "exceeds 128 MB" "$L" | cut -c1-200
```

**量化本会话图片体积**：

```bash
du -cb Library/Locus/Screenshots/*.png | tail -1
```

---

## 6. 已经撞线怎么办

1. 历史里的图**删不掉**，本会话**无法减肥**；重试只会再撞一次。
2. **新开会话**，开场先读交接文档（不要先读大图）。
3. 交接文档必须写清：已改哪些文件、已验证哪些数字、未解释的疑点、下一步顺序。
4. 若新会话一开场就 400，说明它恢复（restore）了旧历史 → 必须开一个**不带历史**的干净会话。

---

## 7. 反面清单（本会话踩过的）

- ❌ 同一机位连续拍 5–7 张原图做"基线/候选"对照 —— 应改为写盘 + 差异统计。
- ❌ `read` 2.5 MB 的 Game 截图只为"看一眼对不对" —— 应改为掩码/帧差数字。
- ❌ `read` 1.3 MB 历史截图做横向对照 —— 应改为引用已有结论 + 必要时小图。
- ❌ 在对话里贴贴图原文（512² PNG）判断纹理方向 —— 应改为读贴图的像素梯度或直接看渲染结果统计。
- ❌ 期望"重开会话"能自动瘦身 —— 恢复的历史同样占体积。
