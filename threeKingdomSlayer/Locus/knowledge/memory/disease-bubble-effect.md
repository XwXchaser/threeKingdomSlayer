---
id: kd_01096ae1-2924-4f6f-95c4-693a3bd99bd6
injectMode: inherit
summary: 疾病泡泡（DiseaseBubbleEffect）的实现方式、造型结构、挂载偏移与当前验收参数。
aiEditMode: inherit
---

# 疾病泡泡特效（DiseaseBubbleEffect）

- 实现：`Assets/Scripts/Effect/DiseaseBubbleEffect.cs` 采用运行时程序化 Texture2D Sprite，不依赖外部 PNG；生成 4 种 80×80 变体，PPU=30、FilterMode.Point。
- 造型结构：深紫断续外轮廓、紫色内弧、下/右侧紫色内腔块面、少量淡紫/白色像素高光；中心并非完全填充，运行时自然淡出。
- 挂载：运行时挂载在染病敌人上，左侧偏移 `spawnOffsetX=-1.5`、上方偏移 `spawnOffsetY=4.0`；最多同时存在 3 个泡泡。
- 当前验收参数：发射间隔 0.32 秒，生命周期 0.70–0.95 秒，上升距离 1.5–2.2，缩放 1.2–2.0，横向散布 ±0.9。
- 经验：冒泡感主要由数量、间隔、上升距离与横向分散决定；可读性依赖外环高不透明紫色与内腔紫色块面，白色仅作少量高光。AI 生图参考不可直接替代程序化实现。

制作方法与通用坑见 `skill/workflows/procedural-pixel-vfx-guide.md`。
