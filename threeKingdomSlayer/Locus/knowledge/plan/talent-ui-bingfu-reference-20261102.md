---
id: kd_31b4b5f6-5d83-4ad2-b744-b19ba5fa3e76
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: 天赋界面美术需求文档（2026-11-02 改版）：本轮 v1–v5 全部产物作废、无可用目标参考图，本文仅记载需求。含《上古卷轴：刀锋》整套参考资料（实机截图 10 张 + 官方 wiki 技能系统页 + 节点框原件 + 发行商游戏介绍，已搬入 _art_process/Authoring/blades_skill_ref/）、兵符造型规格、简洁白边材质方向（实测基准）、刀锋的版式与功能要求、内容物清单、负面清单、生图流程与接口约束（CDN 不可达须带 response_format=b64_json、锚图不得并列元素）、与当前实现的落差（行距 150→250 等）、未决问题。
---


# 天赋界面美术需求（2026-11-02）

> ⚠️ **本文已作废（2026-11-02 决策 A + B）。**
> 刀锋参考作为**画风锚**已作废（决策 B：三方旧口径一律不采用，重新定）。
>
> **仍可参考的部分**：刀锋素材的**结构观察**（节点框边宽比例、单色无渐变、剪影图标、细线）—— 这些是**比例与复杂度**的参考，不是**风格**的参考；新画风可以定成别的样子，但「不要堆装饰」这个教训成立。
>
> **新的美术依据文档：`design/talent-page-art-brief-20261102.md`**（美术侧唯一入口）。

> **一句话**：2026-11-02 这一轮（v1–v5）的全部参考图作废，本文改为**只记载需求**。
> 参考游戏《上古卷轴：刀锋》的完整资料（实机截图 / 官方 wiki / 节点框原件 / 发行商介绍）
> 已从临时区搬进 `_art_process/Authoring/blades_skill_ref/`，见 §1。

> **状态：当前没有任何可用的目标参考图。**
> 2026-11-02 这一轮（v1–v5）产出的全部参考图**一律作废**，不得作为造型 / 版式 / 材质 / 内容依据。
> 作废清单见 §6，文件已移入 `_art_process/Authoring/talent_ui/_void/`（带 `VOID_` 前缀）。
> 本文**只记载需求**，不记录已否决的方案、不提供任何「照着这个做」的图。

---

## 1. 参考游戏资料

**参考游戏**：《上古卷轴：刀锋》（The Elder Scrolls: Blades，Bethesda 出品，TouchArcade 发行的手机版）。
它是**技能树界面的直接原型** —— 我们要做的天赋页，交互结构（三大分类 / 前置链 / 消耗解锁 / 底部详情面板）就是它那套。

### 1.0.1 游戏介绍（摘自发行商商店页 `ta_game.html`）

> From Bethesda Game Studios, the award-winning creators of Skyrim, comes The Elder Scrolls: Blades – a classic dungeon crawler reimagined.
> The Blades, the Empire's top agents, are forced into exile. On the run, you return to your hometown to find it destroyed.
>
> CONQUER your friends and rivals in epic one-on-one Arena battles.
> CREATE any character you want and discover unique weapons, armor, and abilities.
> MASTER the never-ending Abyss with a cutting-edge combat system.

### 1.0.2 技能系统介绍（摘自官方 wiki `skills.wiki`）

- **Skills** 是玩家与战斗相关的能力，分成三段：**Spells / Perks / Abilities**（对应我们的 **基础数值 / 招式解锁 / 三选一解锁**）。
- **学一个技能需要三件事**：达到最低玩家等级 + 已学会它的前置技能（如果有） + 花费 Skill Point。
- **升级（ranking up）** 会提升效果；戒指 / 项链还能额外提供 rank。
- Spells 与 Abilities 必须**装备**后才能在战斗中使用；Spells 同时只能装 3 个。
- **节点构造**：wiki 的图表模板写作 `{{Image Overlay | BL-icon-Skill Frame.png | <技能图标>}}` ——
  即**节点 = 节点框 + 居中叠一个技能图标**。这直接对应我们要的「兵符 + 图标」。
- 技能页底部详情面板含：名称、元信息、描述、数值表、LEARN 按钮（消耗技能点）。

### 1.0.3 资料清单

全部位于 `C:\threeKingdomSlayer\threeKingdomSlayer\_art_process\Authoring\blades_skill_ref\`
（**已从 `Library/Locus/tmp/blades_ref/` 搬进来** —— 那份是临时区，Unity 会清）：

| 文件 | 是什么 | 权威级别 |
|---|---|---|
| `icons/BL-icon-Skill_Frame.png` | **节点框原件**（256×256，菱形，暖奶油面 + 深棕描边 + 中心圆孔） | ★实机资源，画法权威 |
| `icons/` 其余 12 个 | 实机技能图标、`Skill Point`、`SkillCategories-Fire`、`Required_Level`、`Side_Menu` | ★实机资源 |
| `skills.wiki` | 官方 wiki 技能系统页（含 Chart 节点图数据、技能描述、等级/数值表） | ★**功能与数据权威** |
| `ta_game.html` | 发行商商店页（游戏介绍原文） | ★官方介绍 |
| `shot_1.jpg` … `shot_10.jpg` | 商店页实机宣传图 | 实机，但**不含技能页** |
| `shots_contact_sheet.png` | 上面 10 张的索引图（方便一眼看完） | 导航辅助 |
| `blades_skills_portrait_rebuild.png` | 1080×1920 竖屏技能页 —— **上一轮用 wiki 的 Chart 数据用 Python 渲染的重建图** | 重建图，**不是实机** |
| `blades_skill_trees_overview.png` | 三分类竖排总览 —— 同上，重建图 | 重建图，**不是实机** |
| `render.py` | 生成上面两张重建图的脚本 | 工具 |

### 1.0.4 三条必须知道的事

1. **实机截图里没有技能页**。`shot_*.jpg` 十张全是商店页宣传图：
   `shot_1/4/7` 城镇与场景、`shot_3/5` **CHARACTER 面板**、`shot_6/8/9/10` 战斗。
   → 技能页的**图像**资料只有重建图；**功能**资料以 wiki 为准；**节点画法**以 `BL-icon-Skill_Frame.png` 为准。
2. **刀锋有两套 UI 语言，不能混**：
   - **技能页 = 近黑底 + 奶油色文字 + 细金线**（这才是我们要的「简洁白边」）
   - **CHARACTER 面板 = 浅色羊皮纸底 + 深色字 + 细金线**（`shot_3/shot_5`）
   → 我们要的是**前者**。
3. **重建图是二手资料**，可当版式参考，但材质与配色一律以实机资源和 wiki 为准。

---

## 2. 需求

### 2.1 节点造型：兵符

天赋节点的**剪影**用中国古代「兵符 / 虎符」形铜牌（已认可的部分，仅造型）。

| 项 | 要求 |
|---|---|
| 长宽比 | 高 : 宽 ≈ **1 : 1.55**（竖长） |
| 顶部 | 圆拱冠，冠顶正中**一个**圆形穿绳孔 |
| 牌身 | 直筒 |
| 底部 | 圆弧收口 |
| 描边 | **四边闭合**，不能像 HUD 占位件那样底边不闭合 |
| 内部 | 图标放在牌的下半部（深色凹槽区） |

造型参照实物：`Assets/Sprites/UI/Combat/hud_slot_token.png`（124×192，实测规格见 `memory/ui-art-style-anchor.md`）。

### 2.2 材质方向：简洁白边

**这是本轮被否多次的核心原因。** 兵符不能做成厚重金属。

- 牌面 = **平面近黑**（≈ `#1A1A1E`），无渐变、无反光、无纹理、无倒角、无锈迹
- 描边 = **米白细发丝线**（≈ `#F2EDE0`），厚度约牌宽 **3%**
- 穿绳孔 = 素圈孔（透出后面的暗底）
- **不发光、不光晕、不辉光、不做光源特效**
- **三态只改描边颜色，牌面始终是黑的**：
  已解锁 ≈ `#F0DFB4`（亮奶金）/ 可解锁 ≈ `#C9A97A`（暖铜）/ 锁定 ≈ `#8C8A86`（冷灰）

参考基准：战斗 HUD 的令牌 / 暂停键 / 头像框 / 血条框 / 等级球——**只有两种材质：近黑承托 + 一圈米白细描边**。

### 2.3 版式与功能：参考《上古卷轴：刀锋》技能页

参考资料见 §1（整套都在 `_art_process/Authoring/blades_skill_ref\`）。
**功能以 `skills.wiki` 为准，节点画法以 `icons/BL-icon-Skill_Frame.png` 为准，版式参考用重建图 `blades_skills_portrait_rebuild.png`。**

要的东西：

- 顶部：左上一个大标题；标题下**一条横贯的细线**；右上一个小徽记 + 数字位
- 标题下一行**三个分类页签**：**纯文字**（不要底板 / 牌子 / 金属块 / 框），
  选中项更亮 + 下方一条细下划线
- 节点树区：节点用细线相连，每节点下方**一行名称 + 一行更小的价格**
- 底部**大详情面板**：面板边框、左侧一个节点小缩略图、一个大标题、一行元信息、一段描述、
  状态行、花费行，底部一条**通栏大按钮**
- 整屏克制、留白、高对比

### 2.4 内容物

- 三个分类页签：**基础数值 / 招式解锁 / 三选一解锁**（默认选中第三个）
- 节点：我们的 **6 个天赋**（基础数值 5 / 招式解锁 3 / 三选一解锁 6，三棵树合计 14 个可见节点），
  排布按各树真实数据（详见 `Assets/ScriptableObjects/Talents/TalentTree_*.asset` 的 `treePosition` 与前置关系）
- 节点图标：项目真实图标（`Assets/Sprites/31Reward/icon/`、`Assets/Sprites/BatlleHUD/QTE_*.png`）；
  **缺图标的 3 个**（`ConvergenceWave` / `PushWave` / `OnAttackStab`）要补
- 铜钱用**方孔铜钱**（与落地页右上角一致）
- 面板里展示的字段：名称、描述、解锁状态、铜钱花费、前置、解锁按钮

### 2.5 本轮交付边界

- 只出**整体参考图**（整屏），**不拆块件**
- 参考图里**不画文字**：中文由后期用项目字体合成真实文案

---

## 3. 负面清单（明确不要）

**材质 / 质感**
- 厚重金属、黄铜 / 黄金 / 铜绿、残破锻铁板
- 笔刷笔触、绘画质感、噪点堆叠、马赛克滤镜
- 像素簇堆叠、铆钉、花纹、印章、雕花、卷轴、羊皮纸
- 渐变金属、镜面反射、发光 / 光晕 / 辉光 / 光源特效

**色彩**
- 全屏饱和色（**唯一例外**：通栏解锁按钮的朱红 `#9A0101`，落地页的「开始关卡」是同样用法）
- 大面积朱红 / 暖土 / 赭黄铺满

**版式**
- 菱形节点框（刀锋原图是菱形，但我们要兵符剪影）
- 金属页签底板
- 三个令牌并排这类**并列锚图**（并列会被模型当成「三列」的版式暗示）
- 通栏大按钮之外再加第二个强对比按钮

**流程**
- 自己先复刻一版「像参考图的界面」再拿去生图（复刻必是次品，且会把材质语言锚死在烂底图上）
- 拿**角色 / 战场**概念图当 UI 画风锚（`art-style-guide` §7：UI 不复用角色像素密度）

---

## 4. 流程与接口约束（踩过的坑）

| 约束 | 说明 |
|---|---|
| **画风锚只能用 UI 概念图或已部署 UI 实物** | 优先用已部署实物（如 `hud_slot_token.png`）拼的材质板，最准 |
| **锚图不要并列元素** | 并列 = 版式暗示 |
| **`response_format=b64_json` 必须带上** | 图片 CDN `img.huaruics.com`（154.44.9.199）从本机 TCP 443/80 全超时，接口默认返回 `url` 会下不到图 |
| **先落盘响应 JSON，再下载图片** | 反过来会在下载失败时把 url 一起丢掉（已因此白丢一次付费生成） |
| 其余参数 | `gpt-image-2.5-sunburst` / `edits`（multipart，字段 `image[]`）/ `quality=max` / `input_fidelity=high` / `1024x1536` / `png` / `n=1` / `background` 不设（整屏稿不需原生透明） |
| 生图前报批 | 中英文提示词 + 负面词 + 参考图职责 + 参数，必须先给用户批准 |

---

## 5. 与当前实现的落差（采纳本需求就要改实现）

| 项 | 当前 `TalentTreePanel` | 需求 |
|---|---|---|
| 节点形状 | 方形 132×132 | 竖长兵符 1:1.55 |
| 名称 / 价格位置 | 在框内（上 / 下沿） | 名称在框外下方，价格在名称下方 |
| 行距 | 150 | 兵符竖长 + 框外两行字 → 约 **250**（否则上下压 54px） |
| 底部 | 300px 紧凑卡 + 右侧小按钮 | 放大成**大面板** + **通栏按钮** |
| 页签 | 纯文字无底板 | 纯文字 + 选中一条细下划线（可维持） |
| 节点高亮 | 圆形 `ProceduralUiSprites.Ring` | 圆形套竖长牌会变怪椭圆 → 改成同形高亮 |
| 场景槽位 | `nodeLocked/Available/Unlocked` 接的是**上一版已否**的 `TT_Node_*`（因 `preferProceduralNodes=1` 运行时未显示） | 换素材时必须重新指向 |

---

## 6. 作废产物清单（**不得作为参考**）

隔离于 `_art_process/Authoring/talent_ui/_void/`（`VOID_` 前缀），目录内有 `README_作废产物.md`：

- `VOID_talent_ui_ref_v5_*.png` —— v5「成品参考图」，整体不可用
- `VOID_talent_ui_ref_v3_*.png` —— v3，厚重金属 / 残破材质
- `VOID_talent_ui_ref_v1_*.png` —— v1，占位兵符材质，过于简陋复古
- `VOID_talent_ui_art_raw_v1/v3/v4/v5.png` —— 各轮 AI 原始产出
- `VOID_talent_mock_base*.png` —— v1/v2 自制版式底图
- `VOID_hud_token_material_ref.png` —— 三令牌并排材质板（并列暗示三列）
- `VOID_hud_token_single_ref.png` —— 单兵符锚图
- `_void/scripts/` —— 本轮全部生图 / 合成脚本（脚本可复用，产出作废）

本会话之前、更早就已否的产物（同样不可用，未移入 `_void/`）：
`talent_flat_*.png`、`talent_page_v3/v4/v5_*.png`、`talent_node_crop_v4.png`
（那是「青铜铭牌 + 铆钉 + 朱红印章 + 笔刷纸纹」的过度设计版）。

原始记录（提示词全文 / 请求 / 响应 / 状态）：
`C:\Users\steam\Pictures\gptGen\talent_ui_v1\ … _v2\ … _v3\ … _v4\ … _v5\`

---

## 7. 未决问题

1. **兵符在战斗界面里到底该不该是大件？** HUD 里的令牌是 62×96 的小图标容器（技能槽），
   不是面板底座。天赋页的节点要大得多 —— 需要明确「同一个物件在两种尺寸下的视觉重量差多少可接受」。
2. **三个缺失图标**（聚拢波 / 击退波 / 枪突收尾）由谁出。
3. **落地页笔刷语言 vs HUD 简洁白边**：天赋页从落地页进入，但按需求走 HUD 那套 —— 两套语言在同一个流程里并存是否可接受。
4. 行距 / 节点尺寸 / 详情卡放大，**等参考图定稿后再动工程资产**（当前 `TalentTreePanel.cs`、`TalentTree.scene`、
   三个 `TalentTree_*.asset` 一行未改）。
