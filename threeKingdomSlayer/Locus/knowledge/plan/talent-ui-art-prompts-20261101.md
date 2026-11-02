---
id: kd_716a94f1-f6b7-4566-bd5e-2db993a0a1a3
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: 天赋界面新参考图与拆件生成词（计划稿）：把刀锋的结构换成「军帐铜牌」造型——墨色笔刷底 + 青铜包边 + 朱红强调，含整屏主参考图中英文提示词、7 类拆件（节点三态/页签两态/页托板/详情卡/按钮/铜钉/连线）生成词、色板与九宫格接入尺寸对应表。
---

# 天赋界面（三分类 × 二维分页）美术参考图与拆件生成词

> 用途：给"天赋"页出一张**新参考图**，以及后续拆件（页签/节点框/托板/详情卡/按钮/页码点/连线）的生成词。
> 风格口径来自 `design/art-style-guide.md`（墨色笔刷 + 金属边缘 + 高可读字体）与 `design/skill-item-icon-art-guideline.md`（三国像素街机图标）。
> 界面骨架已经实现并验收：三分类页签 + 左右切页 + 类内上下翻页 + 点击节点弹详情卡 + 铜钱解锁。本次只换皮，**不改布局骨架**。

---

## 1. 先定"长什么样"

### 1.1 一句话定位
**军帐里的青铜军令牌**：墨色笔刷底当宣纸/皮革，节点是被铜钉钉在托板上的青铜铭牌，选中和可解锁用朱红提亮，未解锁是冷铁灰。

### 1.2 与刀锋（Blades）的对应
| Blades | 我们 |
|---|---|
| 深色石质底 + 细金属描边 | 墨色笔刷底（带宣纸颗粒）+ 青铜/锻铁包边 |
| 方形节点框 + 内凹面板 + 蚀刻图标 | 青铜铆钉方牌 + 内凹墨底 + 蚀刻图标 |
| 顶部三页签 + 下划线 | 三块青铜铭牌，选中牌面前压 + 朱红描边 + 底部朱印条 |
| 底部详情卡（图标+描述+等级表+按钮） | 展开的军令牌：左图标铜框 / 中效果文本 / 右下朱红按钮 |
| 页码点 | 铜钉点（当前=朱红发亮，其它=暗铜） |
| 线（暗/亮两态） | 墨线（暗）/ 亮铜描边线（亮） |

### 1.3 布局分区（沿用现有骨架，1080×1920 设计像素）
```
┌───────────────────────────────┐
│ ［返回］        天  赋      铜钱 │  ← 顶部：笔刷横幅标题 + 朱印 + 右上钱袋
│  ┌─────┐ ┌─────┐ ┌─────┐      │  ← 页签：三块青铜铭牌（基础数值/招式解锁/三选一）
│  │基础 │ │招式 │ │三选一│      │
│  └─────┘ └─────┘ └─────┘      │
│ ┌───────────────────────────┐ │
│ │   ○───○        ○          │ │  ← 当前分类的一页（青铜托板托住整片节点区）
│ │   │╲  │        │          │ │
│ │   ○───○───○    ○          │ │     节点=青铜方牌，连线=墨线
│ │                           │ │     右侧竖排铜钉=类内页码
│ └───────────────────────────┘ │
│ ┌───────────────────────────┐ │
│ │[图标] 枪突收尾              │ │  ← 详情卡：展开的军令牌
│ │  伤害 30 · 冷却 0.5s·范围 1排│ │
│ │  花费 1 铜钱      [ 解 锁 ] │ │
│ └───────────────────────────┘ │
└───────────────────────────────┘
```

### 1.4 色板（写进生成词，避免跑偏）
- 墨底：`#14110F` → `#2A2320`（带笔刷颗粒，不要纯黑）
- 青铜（主金属）：`#C8A25A` / `#8C6B32` / 高光 `#F0D79A`
- 冷铁（未解锁）：`#6E6A63` / `#3A3733`
- 朱红（选中/强调/可买）：`#B4342A` / 高光 `#E0594A`
- 宣纸/米白（正文）：`#EFE3CB`
- 暖土（背景点缀）：`#7A5B3A`

### 1.5 三条硬约束
1. **不用萌系/Q版/仙侠水墨/照片写实**（风格指南明令禁止）。
2. 节点、页签、按钮的**外轮廓必须有清楚的明暗分层**，因为要在 132px 大小下被一眼分辨。
3. 所有拆件交付**透明背景 PNG**，界面外框由 UI 拼，不画整屏 UI 之外的场景内容。

---

## 2. 主参考图生成词（整屏，1 张）

**尺寸**：1080×1920（9:16 竖版）。**用途**：只做视觉方向参考，不直接切图。

### 中文
> 竖屏手机游戏界面设计稿，三国题材，高清像素化的动作游戏 UI。整体是一块深墨色的军帐皮革与宣纸拼接底板，边缘包青铜与锻铁，四角有小铜钉，表面有笔刷纹理与轻微磨损。
> 顶部居中是笔刷横幅标题「天赋」，横幅两端压着朱红印章；
> 标题下方横排三块青铜铭牌作为页签，文字为「基础数值」「招式解锁」「三选一解锁」，第一块铭牌被选中：牌面前压、朱红描边、下方一条朱印色横条；
> 中央是一块更大的青铜托板，托板上 6 到 8 个方形节点牌以三列错落排布，用细墨线与亮铜描边的曲线连接，节点牌是青铜边 + 内凹墨底 + 蚀刻的技能图标（剑、枪、旋风、浪头、箭雨、甲胄），牌子四角有小铆钉；已解锁的牌子偏亮金并有血色描边，可解锁的是暖青铜，未解锁的是冷铁灰；
> 中央区右侧竖排三颗铜钉作为页码指示，最上一颗朱红发亮；
> 底部是一块展开的军令牌，左侧铜框内是一枚技能图标，中段是米白色的效果文字两行，右下角一枚朱红色按钮写着「解锁」；
> 右上角一个钱袋图标和数字。整体厚重、凶悍、明亮暖色点缀，冷铁与暖铜强对比，硬边像素块面，清晰分层，不写实、不萌、不水墨仙侠。

### English
> Vertical mobile game UI screen, Three Kingdoms theme, high-resolution pixel-art action game interface. A dark ink-and-leather war-tent panel with bronze and forged-iron framing, small copper studs at the corners, visible brush texture and light wear. Centered top: a brush-stroke banner title with red seals at both ends. Below it, three bronze nameplates as tabs, the first one selected — pushed forward, red outline, red seal bar underneath. Center: a large bronze tray holding six to eight square skill plates arranged in three loose columns, connected by thin ink lines with bright bronze highlights; each plate has a bronze rim, sunken ink-black face and an etched skill icon (sword, spear, cyclone, wave, arrow rain, armor), with tiny rivets at the corners. Unlocked plates are bright gold with a blood-red rim, available ones warm bronze, locked ones cold gray iron. Right edge of the tray: three vertical copper studs as page dots, the top one glowing red. Bottom: an unfolded military token card — bronze-framed icon on the left, two lines of parchment-colored effect text, a red "unlock" button at the lower right. Top right corner: a coin pouch icon with a number. Heavy, fierce, warm accents against cold iron, hard-edged pixel clusters, crisp value separation; not photoreal, not cute, not ink-wash fantasy.

**负面词**：`cute, chibi, kawaii, soft gradients, airbrush, watercolor, ink-wash xianxia, gothic dark, sci-fi, neon, photorealistic, 3D render, glossy plastic, blurred, low contrast, western knight, anime moe, cluttered, tiny unreadable text, wrong perspective`

---

## 3. 拆件生成词（透明 PNG，逐个出）

> 通用尾巴（附在每个拆件词后面）：
> `isolated game UI element, transparent background, no text, no characters, hard-edged pixel clusters, clear rim light, 2x resolution, centered, generous safe margin`
> 负面词同上。

### 3.1 节点牌三态（对应 Inspector：`nodeLocked` / `nodeAvailable` / `nodeUnlocked`）
```
square skill plate, bronze riveted frame, sunken ink-black recessed face, etched icon area in the middle, empty name band along the bottom edge, small price band along the top edge, 128x128
```
- 未解锁：`cold gray iron frame, dim, low key`
- 可解锁：`warm bronze frame, faint red inner glow`
- 已解锁：`bright gold frame with blood-red rim highlight, inner face slightly lighter`

### 3.2 页签（`tabNormal` / `tabActive`）
```
rectangular bronze nameplate tab, beveled edge, brush-textured face, 9-slice friendly, 320x96
```
- 常态：`dark bronze, recessed`
- 选中：`pressed forward, red outline, red seal bar along the bottom`

### 3.3 页面托板（`pagePlate`）
```
large bronze-framed tray plate, ink-black leather center with paper grain, copper studs at four corners, thin double rim, 9-slice friendly
```

### 3.4 详情卡（`detailPlate`）
```
unfolded military token card, parchment center, bronze border with corner brackets, subtle red seal watermark in one corner, 9-slice friendly
```

### 3.5 按钮（`buttonNormal` / 禁用态）
```
rectangular stone-and-bronze button, beveled, red seal glyph area in the middle left blank
```
- 常态：`red lacquer face with bronze rim, slight top rim light`
- 禁用：`cold gray iron, matte, no highlight`

### 3.6 页码点（`dotOff` / `dotOn`）
```
small copper stud dot, 24x24
```
- 常态：`dark copper, matte`
- 当前：`glowing red copper, bright rim`

### 3.7 连线（`connector`）
```
thin ink line with bronze edging, 1:1 horizontal strip, tileable
```
- 亮态：`bright bronze, inner glow`
- 暗态：`dark ink, low contrast`

### 3.8 技能图标（节点内蚀刻图标，可复用 `31Reward/icon/`）
```
etched metal skill icon on dark plate, single subject, strong silhouette, 三国机关风格
```
（现有 `Assets/Sprites/31Reward/icon/` 里的图标已经是这套风格，优先复用；缺的 3 个：聚拢波 / 击退波 / 连刺）

---

## 4. 交付与接入规范

| 拆件 | 交付尺寸 | Inspector 槽位 |
|---|---|---|
| 节点三态 | 256×256（带九宫格边 40） | `nodeLocked` / `nodeAvailable` / `nodeUnlocked` |
| 页签两态 | 320×96（九宫格边 24） | `tabNormal` / `tabActive` |
| 页面托板 | 512×512（九宫格边 48） | `pagePlate` |
| 详情卡 | 512×256（九宫格边 40） | `detailPlate` |
| 按钮两态 | 256×96（九宫格边 24） | `buttonNormal`（禁用态用染色） |
| 页码点 | 48×48 / 48×48 | `dotOff` / `dotOn` |
| 连线 | 8×64 | `connector` |

- 必须**透明背景 PNG**，不要白底、不要 UI 外框之外的内容；
- 九宫格边距按上表留，避免拉伸糊角；
- 接入方式：把图拖到 `TalentTreePanel` 对应槽位即可，**不改代码**；槽位留空时仍用程序化垫底。
- 程序化垫底件的当前参数（可对着调）：节点框 128²、边框 11%、圆角 10%；页托板圆角 26 / 描边 4；页签圆角 18；按钮圆角 16。

---

## 5. 出图顺序建议

1. 先出**主参考图**（整屏），确认"军令铜牌 + 墨底"这个方向对不对；
2. 方向对了再出**节点牌三态**（决定界面的识别度）和**页签两态**；
3. 最后出托板/详情卡/按钮/铜钉这类"托底件"。
