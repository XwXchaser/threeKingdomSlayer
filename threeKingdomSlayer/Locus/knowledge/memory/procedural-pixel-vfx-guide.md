---
id: kd_f354dc78-27d8-4095-8abf-794f7f9b9cee
injectMode: inherit
summary: 程序化像素命中特效的完整制作经验：颜色分层、不规则尖刺布局、帧动画结构、DOTween 时序、随机化控制、像素美术约束，以及 8 条踩坑记录（多 SpriteRenderer 拼装、中心空心、造型过规则、PPU 尺寸、动画过快、坐标混淆、DOTween 生命周期、AI 生图误用）。
aiMaintained: inherit
---

## 整体架构

程序化像素命中特效的核心思路：**在 Awake 时用 Texture2D 逐像素生成 Sprite，运行时通过单一 SpriteRenderer + DOTween Sequence 播放帧动画**。不使用外部贴图，不拼装多个 GameObject。

关键文件：`Assets/Scripts/Core/PixelHitEffectManager.cs`（~840 行）

架构层级：
1. **对象池**：预创建 N 个 `EffectInstance`，每个含 1 个 center `SpriteRenderer` + 最多 12 个 ray `SpriteRenderer`
2. **Sprite 生成**：Awake 时调用 `CreateStabSprites()`，产出 4 variant × 3 frame = 12 张完整 Sprite
3. **运行时播放**：`BuildStabEffect()` 用 DOTween Sequence 控制缩放、帧切换、碎屑扩散
4. **回收**：`ReturnToPool()` 用 owner 校验收敛，避免旧 Sequence 误回收复用实例

## 颜色分层策略（从外到内）

```
outline:  RGB(53, 16, 8)    深棕描边
darkRed:  RGB(145, 24, 8)   暗红
red:      RGB(213, 42, 8)    红
orange:   RGB(255, 101, 7)   橙
yellow:   RGB(255, 211, 16)  金黄
white:    RGB(255, 253, 224)  暖白核心
```

绘制顺序：先画大半径的 outline 实心核心 → darkRed → orange → yellow → white（越内层越后画，覆盖外层）。每层用 `DrawSolidBurstCore()` 画菱形夹紧圆盘，再用 `DrawBurstLayer()` 画对应颜色的放射尖刺。

**关键教训**：如果只画 `DrawBurstLayer`（从 innerRadius 开始），中心会透明空心。必须先调用 `DrawSolidBurstCore` 填充中心。

## 形状设计

### 尖刺布局
- 13 个不规则方向角（不是均匀 360°/N 等分），制造爆炸的不规则感
- 方向角：`3°, 18°, 47°, 72°, 109°, 128°, 171°, 198°, 226°, 252°, 287°, 316°, 344°`
- 每条尖刺有独立 baseLength（0.83 / 0.98 / 1.15 三档），再加 ±7% 随机扰动
- 每个 variant 的尖刺角度额外 ±5.5° 小偏移

### 实心核心（DrawSolidBurstCore）
- 用菱形 + 方形双重裁剪：`diamond ≤ 1.28f && square ≤ 1f`
- 这样中心是类圆形，但边缘保留像素硬边，不会变成圆滑渐变的 blob

### 放射层（DrawBurstLayer）
- 从 innerRadius 到 length 的锥形尖刺，越往外越窄（taper = 1 - normalized）
- 最小半宽 clamp 到 0.5px 防止尖刺消失

### 碎屑（DrawDebris）
- 仅在 frame 2（最终帧）绘制
- 5 个小碎片沿尖刺方向外推，每个 3-6px 长，1-2px 宽
- 颜色交替使用 outline / darkRed

## 帧动画结构

4 个 variant，每个 3 帧：
- **Frame 0（接触帧）**：小型白色星芒 + outline 轮廓，表示命中瞬间
- **Frame 1（展开帧）**：78% 展开，完整色彩分层（outline → darkRed → orange → yellow → white）
- **Frame 2（爆发帧）**：100% 展开，加上碎屑

每次命中轮换 variantIndex，使连续命中看起来有细微差别。

## DOTween 动画时序

3 阶段，用 `InsertCallback` 切换 Sprite：

| 时间点 | 动作 |
|---|---|
| 0 | Frame 0，scale=0.34×peak，开始放大 |
| contactEnd (14%) | 切换到 Frame 1，scale=0.64×peak |
| burstEnd (54%) | 切换到 Frame 2，scale=peak（Ease.OutBack 回弹） |
| holdEnd (72%) | 轻微缩小到 0.9×peak |
| duration (100%) | center 禁用，回收 |

碎屑在 burstEnd 后延迟 76%-94% 出现，向外散射后淡出。

**时序参数**：
- 普通：当前验收值 `duration=0.28s`、峰值基础倍率 `2.4×1.344`
- 重击：当前验收值 `duration=0.34s`、峰值基础倍率 `2.4×1.536`
- Slash 普通完整命中约 `0.18s`，Stab 已缩短到接近其短促节奏

## 随机化控制

- 整体缩放 ±3%（`Next(0.97f, 1.03f)`）
- 整体旋转 ±5°
- 尖刺角度 per variant ±4.5°
- 尖刺长度按主刺/侧刺分级并加 ±6% 随机
- 核心形状、颜色分层、中心位置保持稳定

## 像素美术约束（来自 skill-item-icon-art-guideline）

- FilterMode.Point：硬边像素
- 有限色阶、深色描边、高对比分阶块面
- 禁止平滑渐变、柔焦、空气笔刷
- PPU 影响视觉大小：PPU=44 时特效太小 → 降到 30

## 踩坑记录

### 坑 1：多个 SpriteRenderer 拼装 vs 单张程序化 Sprite
- 失败方案：用 12 个独立 SpriteRenderer 拼装色块 → 效果像「白色块粘橙色叶片」
- 正确方案：在 Awake 时用 Texture2D 逐像素画完整 Sprite，运行时只需 1 个 center SpriteRenderer
- 原则：**程序化特效的「造型」由像素决定，不由 Transform 层级决定**

### 坑 2：中心透明空心
- 原因：`DrawBurstLayer` 只从 innerRadius 向外画，中心区域无像素
- 修复：新增 `DrawSolidBurstCore()`，在所有放射层之前填充中心

### 坑 3：太规则，不像爆炸
- 原因：放射方向过多且均匀，颜色层叠后形成刺团
- 修复：减少方向数量，使用分级长短和不规则角度，同时缩短外扩半径；中心核心必须保持主视觉占比

### 坑 4：PPU 导致特效太小
- 初始 PPU=44 → 改为 30
- 如果还不够大，继续降低 PPU 或增大 peakScale

### 坑 5：太快无法验收
- 初始 duration=0.17s → 曾延长到 0.45s/0.52s 便于验收
- 验收后 Stab 回调到 `0.28s/0.34s`，使其与 Slash 的短促节奏接近

### 坑 6：Slash 左右镜像坐标混淆（来自 mistake-note）
- 特效方向拆为三种语义：世界方向、相机屏幕方向、特效局部方向
- 根节点固定在命中点，`VisualRoot` 负责从起点飞到命中点
- 运动、旋转、分叉、SpriteRenderer.flipX 必须共用同一方向基准

### 坑 7：DOTween Tween 目标已被 Destroy
- MirrorReferenceException：Transform 已被销毁但 DOTween 仍在尝试设置 localScale
- 预防：回收前调用 `sequence.Kill(false)`；ReturnToPool 用 owner 校验收敛；创建 Sequence 时用 `.SetTarget()` 绑定生命周期

### 坑 8：AI 生图不能替代程序化特效实现
- 用户明确要求「用参考图、不用 AI 生成替代图」时，不要再调用 gpt-image Skill
- 参考图只用于造型参考，实现必须走代码

### 坑 9：Slash 旋转角与左右镜像必须分离
- 错误做法：判断左向后直接将局部方向取反，再用取反后的向量计算 `Atan2`；左右 Slash 会得到相同旋转角。
- 正确做法：保留局部方向的 X 符号，用 `Atan2(y, Abs(x))` 计算斜向倾角，再将左右符号独立用于 `SpriteRenderer.flipX`。这样旋转负责“斜角”，镜像负责“左右造型”。
- 经验：任何方向性命中特效都要把“角度”和“镜像”作为两个独立输出，先做数学验证，再做左右运行时对照验收。

### 疾病泡泡特效
- `Assets/Scripts/Effect/DiseaseBubbleEffect.cs` 采用运行时程序化 Texture2D Sprite，不依赖外部 PNG；生成4种80×80变体，PPU=30、FilterMode.Point。
- 造型结构：深紫断续外轮廓、紫色内弧、下/右侧紫色内腔块面、少量淡紫/白色像素高光；中心并非完全填充，运行时仍会自然淡出。
- 运行时挂载在染病敌人上，保持左侧偏移 `spawnOffsetX=-1.5`、上方偏移 `spawnOffsetY=4.0`；最多同时存在3个泡泡。
- 当前验收参数：发射间隔0.32秒，生命周期0.70–0.95秒，上升距离1.5–2.2，缩放1.2–2.0，横向散布±0.9。
- 经验：冒泡感主要由数量、间隔、上升距离和横向分散决定；泡泡可读性依赖外环高不透明紫色与内腔紫色块面，白色仅作少量高光。生图参考图不可直接替代程序化实现。
- 症状：关闭 `BuildStabEffect()` 的 `instance.rays` 后，Stab 画面上的外围碎片仍存在。
- 根因：Stab 的碎片是 `CreateStabFrameSprite()` 最终帧调用 `DrawStabDebris()` 直接写入 Texture2D 的像素，不属于运行时 `SpriteRenderer` 碎片。
- 规则：修改“图片内部”的视觉元素前，先追踪它是 Texture2D 像素生成、运行时 SpriteRenderer，还是 Prefab 子物体；只修改其真实来源。对于当前 Stab，短时长下保留内嵌碎片的观感已验收。
