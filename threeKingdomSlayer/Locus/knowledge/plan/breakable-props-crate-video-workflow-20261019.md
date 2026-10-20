---
id: kd_89980c00-247b-4c9f-84c0-e56f747298eb
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 可破坏木箱 · 4秒炸裂视频工作流（2026-10-19）

> v1 已生成但因全片出框作废。当前只准备 v2 修正规则，不授权重新上传或创建任务。

## 0. v1 出框失败

`C:/Users/Administrator/Videos/doubaoVideo/breakable_crate_explosion_v1_720p_4s.mp4` 已生成，但用户判定完全出框，不能抽帧、不能抠图、不能作为 v2 输入。

实测 960×960、97 帧。首帧主体包围盒约 `(48,98)-(924,898)`，约占画面宽 91.3%、高 83.3%。从约 2.0 秒起包围盒达到 `(0,0)-(960,960)`，2 秒和末帧都有木板、铁件被四边裁切。

根因是工作流漏掉了项目的基本安全边规则：`design/pixel-character-and-enemy-animation-spec.md` 要求按最大展开范围预留至少 15% 边距，动作视频要求全帧四边至少 32px。v1 首帧没有先缩小，提示词又要求碎片向外飞，所以出框是可预见的。

v2 在创建前必须同时满足：

- 首帧木箱居中，最大边不超过画面的 46%，四边留白至少 27%。
- 炸裂后的所有木板、铁箍、角铁和底板全程留在画面内。
- 全 97 帧四边至少 32px 绿幕，4px 外圈不得出现主体。
- 任一帧低于 32px 即判整片失败，不抽帧、不抠图、不自动重生成。

## 0.1 当前结论

逐帧生图不稳定，停止继续生成酒缸和木箱的序列帧。

当前只准备木箱视频。酒缸视频不在本次范围内；酒缸的既有破坏图和 v1–v3 提示词均不作为木箱参考。

已验收木箱基图：

`C:/Users/Administrator/Pictures/gptGen/breakable_props_base_v1/crate/Prop_Crate_base_v1.png`

- SHA-256：`c6757ef3bfa794a30e89ba1f20149edfbacf25f2a16dd31a5fa57ffe96702d0a`
- 1024×1024，RGBA，PNG Color Type 6。
- 近立方军需箱，轻微四分之三视角；顶面三块木板，正面和右侧面清楚；两道水平冷铁灰铁箍，四角包铁，正面有一条旧竖裂。

已作废的木箱序列：

`C:/Users/Administrator/Pictures/gptGen/breakable_props_break_atlas_v1/crate/Prop_Crate_BreakAtlas_v1.png`

它的后段把底板留在原地，侧板向内倒下，读作坍塌，不得上传，也不得作为视频参考。

## 1. 先读文件

1. `skill/reachapi-seedance-video-generation-v2.md`：接口、首帧规则、轮询、下载和完成性边界。
2. `memory/video-generation-case-log.md`：同类任务的实际耗时、计费和验收记录方式。
3. `memory/user-preference.md`：生成前必须提交完整中英文词、参数和参考职责，等待明确批准。

## 2. 视频规格

```text
POST https://direct.reachapi.ai/v1/vids/create
model: seedance-2-fast
duration_seconds: 4
resolution: 720p
aspect_ratio: 1:1
generate_audio: false
watermark: false
seed: -1
```

只上传已验收木箱基图一次，并只使用一个 `first_frame`。

禁止同时加入：

- `last_frame`
- `reference_image`
- `reference_video`
- `reference_audio`

接口不允许首帧与这些角色混用。因此不能上传失败组图来“提示不要这样做”；失败原因只能写进提示词。

输出目标：

`C:/Users/Administrator/Videos/doubaoVideo/breakable_crate_explosion_v1_720p_4s.mp4`

Key 只从 `REACH_API_KEY` 读取，不写入文件、命令输出、日志或回复。

## 3. 上传前准备

原图是透明 PNG，不能假设视频接口会保留 Alpha。生成前另存一张不透明上传副本，不覆盖原图：

`C:/Users/Administrator/Videos/doubaoVideo/breakable_props_video_v1/crate/Prop_Crate_base_v1_first_frame_flat_white.png`

副本规则：

- 1024×1024 RGB PNG。
- 使用已验收木箱的 Alpha，把主体合成到纯白 `(255,255,255)`。
- 不缩放、不平移、不重绘、不调色。
- 四角和空背景必须精确为白；主体可见 RGB 与原图逐像素一致。
- 记录原图和副本的 SHA-256。

项目视频规范要求先做绿幕，不使用白底。依据是 `skill/reachapi-seedance-video-generation-v2.md` 的 G1–G3，以及 `skill/workflows/image-asset-generation.md` 的实测：绿幕输入后本地抠图的边缘亮度比约 0.63–0.67；近白底再抠会留下约 2.31–2.61 的亮边，而且底抠不净。

当前批次统一色键是 `(0,159,62)`。上传副本改为：

`C:/Users/Administrator/Videos/doubaoVideo/breakable_props_video_v1/crate/Prop_Crate_base_v1_first_frame_green_0_159_62.png`

副本规则：

- 1024×1024 RGB PNG。
- 使用已验收木箱的 Alpha，把主体合成到精确 `(0,159,62)`。
- 先按原始像素把木箱等比缩到最大边 470px，再居中放入 1024 画布。
- 不重绘、不调色、不改变木箱比例。
- 合成后实测四边留白，任何一边低于 27% 都停止，不得上传。
- 四角和所有确认无主体的空背景必须精确为 `(0,159,62)`，std 为 0。
- 主体核心 RGB 与原图逐像素一致；只允许 Alpha 边缘发生必要的绿幕合成变化。
- 原图和副本分别记录 SHA-256。

提示词中的背景描述也改为同一绿色，不再写白色背景。

绿幕输入不是输出恒色保证。下载后必须逐帧测量空背景，报告与 `(0,159,62)` 的 ΔR/ΔG/ΔB、范围、最差帧和首尾数值。颜色偏差升级为待决项，不自动改色或重新生成。

## 4. 动作设计

4 秒只有一个动作：木箱被从画面左侧水平冲击后炸裂。不要循环，不要恢复成原箱。

- `0.00–0.35s`：保持已验收木箱的身份、机位、比例和位置，仅有极短的受击前停顿。
- `0.35–1.10s`：左侧面和正面同时向外炸开。木板、铁箍、角铁和底板只在画面中心区域内短距离飞散，最远不超过首帧木箱尺寸的 35%。
- `1.10–2.40s`：所有主要部件继续离开原位置并旋转。底板必须离开地面并翻起，不能作为完整托盘留在原地。
- `2.40–4.00s`：碎片落成低矮、分散的残骸。板条、弯曲铁箍和角铁分开平放或低斜靠，不再组成箱子，也不再有完整底板。

炸裂方向必须可读：冲击来自画面左侧，碎片主要向右侧、左前侧和上方飞出。所有碎片、铁箍和角铁必须完整留在画面内，四边始终保留至少 32px 绿幕。不得让镜头推进、放大、横移或跟随碎片。不得画武器、枪尖或人物。

## 5. 中文提示词

以上传的木箱作为精确首帧。保持同一只三国军需木箱、同一四分之三机位、同一比例和同一精确 RGB(0,159,62) 绿幕背景。前 0.35 秒只保持原箱稳定。

随后木箱被一记来自画面左侧的水平冲击直接炸裂。正面、左侧面、顶板、两道冷铁灰铁箍、四角包铁和底板全部脱离原结构，只在画面中心短距离向右侧、左前侧和上方飞出并旋转，飞散范围不超过首帧木箱尺寸的 35%。底板必须翻起并离开地面，但不能飞出画面。铁箍弯曲弹开，角铁单独飞出。碎片在 4 秒结束前落成画面中下部的低矮分散残骸，不再有箱子形状，也不再有完整底板。

这是外力炸裂，不是受重力坍塌，不是箱体慢慢张开，也不是内部压力均匀自爆。保持固定镜头、固定比例和固定画面位置，不得推进、放大、横移或跟随碎片。每一块木板、铁箍、角铁和底板从始至终完整可见，距离四边至少 32px。保持暖木色、冷铁灰、阶梯状像素色块和清楚的受光暗部。背景全程保持同一精确 RGB(0,159,62) 绿幕，不出现地面、阴影、烟尘、火花、文字、武器或第二只箱子。

## 6. English prompt

Use the uploaded crate as the exact first frame. Keep the same Three Kingdoms supply crate, the same three-quarter camera angle, the same scale, and the same exact RGB(0,159,62) green-screen background. Hold the intact crate stable for the first 0.35 seconds.

Then a horizontal impact from screen left makes the crate explode. The front boards, left-side boards, top boards, both cool iron-gray bands, all corner plates, and the bottom panel detach, but they travel only a short distance toward the right, front-left, and upward, no farther than 35 percent of the original crate size. The bottom panel flips upward and leaves the ground, while remaining fully inside the frame. The iron bands bend and spring away, and the corner plates fly separately. By 4.00 seconds, the pieces settle into a low, scattered ruin in the lower center, with no crate shape and no intact bottom panel.

This is an external explosion, not a gravity collapse, not a slow opening, and not an even internal-pressure burst. Keep the camera fixed: no push-in, zoom, pan, tilt, or fragment tracking. Every board, iron band, corner plate, and the bottom panel remains fully visible throughout, at least 32 pixels inside every edge. Keep the warm timber, cool iron gray, stepped pixel clusters, and clear light and shadow. Keep the background on the same exact RGB(0,159,62) green screen throughout. Do not add ground, shadows, dust, smoke, sparks, text, a weapon, or a second crate.

## 7. 创建前核对

用户明确批准后才执行，顺序固定：

1. 核对原图哈希和白底副本。
2. 核对实际请求只含一个 text 和一个 `first_frame`。
3. 向用户复述模型、4 秒、720p、1:1、无音频、无水印、单张首帧和输出路径。
4. 只有收到本次“发起生成”后，才上传并创建一次任务。
5. 创建成功后保存 task ID。查询最多 12 次、总等待不超过 120 秒；未完成就停止并报告，不自动重发。
6. 下载先写 `.part`，验证 MP4 结构和可解码后再原子改名。

失败、超时、结果不明或观感不对，都不自动重试，不改词后重发。

## 8. 验收

技术验收和美术验收分开：

- 技术：MP4 可解码，记录实际分辨率、帧率、帧数、时长、文件大小、SHA-256、usage/cost。
- 首帧：与上传木箱的身份、机位和比例一致；允许视频编码差异，但不能换成另一只箱子。
- 动作：1 秒内必须看出炸裂，而不是箱体慢慢裂开或向下塌。
- 结构：底板、铁箍和角铁都离开原位置；结束时没有完整底板，也没有站立的箱框。
- 画风：暖木、冷铁灰和像素色块仍在；无地面、烟尘、武器、文字或第二只箱子。

视频通过后，才讨论抽帧。抽帧不是本次授权，也不能直接把视频帧当作透明素材。
