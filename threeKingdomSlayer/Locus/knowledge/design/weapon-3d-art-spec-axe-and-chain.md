---
id: kd_5edeaf78-c79b-4c52-85e5-2239a1bb8148
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 抡斧 / 追命锁链：四视图美术需求 + 3D 制作规范（2026-11-14）

> **任务来源**：用户 2026-11-14 ——「斧头和锁链暂时用这杆长枪代替」「等我模型到位，先更新交接文档，并准备这两者的四视图美术需求，我拿到四视图后会去制作 3D 模型，你要把制作规范告诉我」。
> **本文是这两个武器模型的唯一需求源**：美术需求（四视图）+ 制作规范（3D 落地）都在这里。
> 相关：`plan/week-20261114-new-enemy-two-skills-parry-counter.md`（本周计划）、`plan/bugs-and-requests.md` REQ-008 / REQ-009、
> `plan/stab-assembled-four-view-v06.md` + `plan/stab-five-view-modeling-reference.md`（长枪四/五视图的**既有范式，本文沿用其坐标、命名与交付口径**）、
> `memory/art-production-workflow-entry.md`（美术工作流入口）。

---

## 0. 范围与当前状态

| 物件 | 玩法定位 | 当前占位 | 交付目标 |
|---|---|---|---|
| **抡斧（巨斧）** | 技能1「抡斧劈砍」：抡起大斧劈砍，**无视盾牌减伤 + 必定破盾**（原型 = PBZ 巨凶斧 5 级「强行蓄力」满蓄力斩击 + 25 级「千钧之势」凌空一劈） | 玩家这杆长枪实例（缩放 0.45 当斧头） | 换成真正的**大斧**模型 |
| **追命锁链（锁镰）** | 技能2「追命锁链」：发射带铁链的镰刀，把**最远、最弱**的敌人拉到面前（原型 = PBZ 追命锁镰 5 级「如影附形」拖拽到面前） | 长枪实例当锁镰 + 2D 链环贴图（`Enemy2/chain.png`）+ 运行时生成的**弹簧骨链** | 换成真正的**锁链 + 镰刀**模型；链身继续由程序骨链驱动（见 §3.6） |

⚠ **两个模型都还没有美术**。用户拿到四视图后去做 3D；本项目这边**已预留插拔位**（§3.7），模型到位只需在资产里指定，不用改代码。

> ★ **PBZ 官方立绘参考已就位**（第三方素材，只留本地、不进仓库）：
> `SkillWeaponsArtSource/01_Reference/pbz/juxiongfu_axe.png` / `zhuiming_suolian.png`（各 2048²，来源与 sha256 见同目录 `references.csv`）。
> 概念图生成方案见 `plan/weapon-concept-image-plan-20261114.md`。

---

## 1. 统一观察坐标（沿用长枪口径，四视图必须一致）

- **+Y**：武器主轴，指向**刃口 / 尖端**（斧：斧刃方向；锁镰：镰刃方向）。
- **+X**：正面图的**画面右侧**。
- **+Z**：**朝正面观察者**一侧（前后厚度沿 Z）。

| 视图 | 观察者位置与朝向 | 画面右 | 画面上 |
|---|---|---|---|
| 正面 FRONT | 在 +Z，朝 −Z 看 | +X | +Y |
| 右侧 RIGHT | 在 +X，朝 −X 看 | −Z | +Y |
| 背面 BACK | 在 −Z，朝 +Z 看 | −X | +Y |
| 左侧 LEFT | 在 −X，朝 +X 看 | +Z | +Y |

- **正交投影**，不透视缩短、不倾斜；同一姿态画四张。
- 链子/铁链这类**柔性物**：四视图必须是**同一个静止姿态**（自然下垂或收拢），不得四张各给一种"风吹姿态"。

---

## 2. 交付物清单（美术侧）

### 2.1 四视图图片

| 交付 | 规格 | 说明 |
|---|---|---|
| 前／左／右／后**单图** | 各 **384×1024** 竖长条 | 武器竖直摆放，完整不出画，四边留白 ≥16px |
| **无字合图** | **1536×1024** | 顺序固定：**前／左／右／后** |
| **中文标注版** | **1856×1440** | 视图名 + 观察方向由本地排版加上（标注不烧进模型图） |
| 生成原图 | 1024×1536 纯白不透明 PNG（每张一次生成） | `quality=max`；正文/负面词见 §6 |
| ★ **五视图合板**（额外） | **1536×1024** 无字 + **1856×1440** 标注 | 左三格 384×1024 = 正／右／后，右列 384×1024 上下两格 384×512 = 顶视／底视。**建模用四视图即可** |

#### 2.1.1 ★ 实际产出（2026-11-14 已完成 · 抡斧）

`SkillWeaponsArtSource/03_FourView/`：

| 文件 | 内容 |
|---|---|
| `axe_four_view_v1_plain.png` / `_annotated.png` | 四视图无字合图 1536×1024 + 中文标注版 1856×1440 |
| `views/{front,left,right,back}.png` | 四张 384×1024 单图 |
| `axe_five_view_v1_plain.png` / `_annotated.png` | 五视图合板 + 标注版 |
| `axe_five_view_v1_raw.png` / `axe_left_view_v1_raw.png` | 生成原图（可追溯） |

**只花了 2 次付费生成**：① 一次出五视图合板（正/右/后/顶/底）② 用它当参考补一张左侧 ⇒ 本地切格合成四视图。
（沿用长枪 V05/V06 的成例。）

⚠ **顶视/底视给的信息有限**：生成器画的是**柄端的端面**（尖头 + 圆环），
**没给出斧头的 X 宽 / Z 深比例** ⇒ 建模时按 §3.1 的数字自行把握（刃宽 1.28 / 斧背厚 0.22 / 刃口 0.03）。
⚠ 四图是**各自生成**的，不是同一模型的几何投影 ⇒ 只能当造型与配色参考，**不能当比例验收**。

### 2.2 目录结构（实际布局）

```
SkillWeaponsArtSource/
  README.md
  00_Index/            执行规格与台账（axe / chain 各一份）
  01_Reference/        参考图（★第三方素材只留本地，不进仓库）
    pbz/               PBZ 官方立绘 + 斧头特写 + references.csv
  02_Concept/          概念图（v1..v6）+ 提示词留痕
  03_FourView/         正交视图交付物（原始图 + 切好的单图 + 无字合图 + 标注版 + 校验 JSON）
  04_Model/            3D 源文件（fbx / blend）与贴图（不入 Unity）
```

> ⚠ `04_Model/` 里的 3D 源文件**不入 Unity**（Unity 侧只放导出好的 FBX + 贴图 + Prefab），源文件归档在这里。
> ⚠ 第三方参考图（PBZ 等）按 `.gitignore` 白名单**只留本地**；只放行 `README.md` / `00_Index/` / `02_Concept/` / `03_FourView/`。

---

## 3. ★ 3D 制作规范（模型交付必须满足，否则接不进工程）

### 3.0 配色口径（★ 2026-11-14 实测修正，优先于本文其他描述）

本文 §4 / §5 早期写的「写实偏硬的金属 + **深色木** + **局部旧金**」与工程实际不符。
实测已部署的 3D 长枪 `Assets/Models/Weapons/FantasySpear/Textures/FantasySpear_BaseColor.png`：

| 角色 | 色 | 用途 |
|---|---|---|
| 主 | **鎏金 / 黄铜** | 金属配件、箍件、装饰 |
| 主 | **朱红** | 缠握布、缨带、流苏（**强识别色**） |
| 主 | **冷银灰 / 白** | 刃面、锋口高光 |
| 辅 | **暖棕 / 橙褐** | 木杆、皮革 |
| 点缀 | 少量翠绿 | 极小面积 |

⇒ **金是主金属色（不是“一点旧金”）、朱红是主识别色、木杆是暖棕橙（不是深色木）**。

#### ★★ 但这套配色**属于「主角主武器」档，不属于「技能道具」档**

上表是**张飞的长枪** —— 全游戏最贵、最华丽的那件资产。
**技能道具（抡斧 / 追命锁链）是砸出去、勾人用的消耗件**，照搬主武器的鎏金会让它们读成“高级货”，
而且违反 `design/art-style-guide.md` §5.3「**高饱和色不要平均撒满画面**」。

⇒ 分两档（2026-11-14 用户定）：

| 档 | 金属 | 缠握 | 木 | 强调色 | 用途 |
|---|---|---|---|---|---|
| **基础档（当前在做）** | **冷蓝灰的钢**（带一点蓝紫倾向，受光面亮、暗面深一档，边缘干净）；**无鎏金无黄铜** | **暖棕偏红褐的旧皮革** | **暖棕**旧木 | 无（唯一亮色是刃缘磨出的**亮银白钢口**） | 技能道具常态外观 |
| **高级档（以后）** | 鎏金 / 黄铜配件 | 朱红 | 暖棕橙木 | 朱红 | 升级 / 稀有度档 |

> ★★ **配色必须以游戏实机为准，不是以贴图猜**（2026-11-14 实测修正）：
> 游戏实机（`_art_process/VisualLibrary/shots/battle_enemy_1012.png`）里
> **金属是冷蓝灰的钢**（不是褐色锈铁）、**刃口是亮银白**、**木料是暖棕**（不是近黑）、
> **皮带是暖棕偏红褐**，整体**明亮高对比、色彩分区清楚**。
> ⇒ 已从实机截图裁出**色卡 + 材质参考**放在 `SkillWeaponsArtSource/01_Reference/style/`：
> `game_palette_board.png`（8 格实机色卡）、`game_wood_iron_ref.png`（木料+铁箍）、`game_saber_ref.png`（刀刃）。
> ⇒ **配色就以这三张为准**；「褐锈铁 + 近黑木 + 整体压暗」是错的（那是 v3~v6 的偏差）。

**与技能等级的挂钩（可选、现成结构）**：`AxeSmashLevelConfig` / `ChainPullLevelConfig` 本来就有 **L1/L2/L3 三级**
⇒ 每级一个 `weaponPrefab` 即可做 **L1 素铁 → L2 → L3 鎏金**，不需要改代码结构。

> ⇒ **本轮（基础档）的概念图与模型一律按基础档做**：金属件全部冷灰旧铁，缠握深棕旧皮革，木杆深冷褐。
> 鎏金整套留给将来的高级档。概念图生成方案见 `plan/weapon-concept-image-plan-20261114.md` §4.7。

### 3.1 尺度基准（先算清楚再建模）

- 相机：`Main Camera` 位于 `(0, 3, -10)`，垂直 fov 60 ⇒ **在玩家所在深度（z≈0）可视高度 ≈ 11.55 世界单位**。
- 现有长枪 `FantasySpear3D` 全长 **10.8**（几乎满屏）⇒ 这是"长杆兵器"的尺度上限。
- **本项目建议尺寸**（世界单位，1 unit ≈ 屏高 1/11.55）：

| 物件 | 全长 | 关键尺寸 | 备注 |
|---|---|---|---|
| **大斧** | **3.2** | 斧刃最大宽 **1.28**、刃厚 **0.22**、握把直径 **0.14** | ≈ 屏高 28%；刃宽:握把径 ≈ 9:1 ⇒ **巨斧体量**（v1 概念图做成 0.34 太窄、读成单手斧 ⇒ 按 PBZ 实测 0.40 上调） |
| **锁镰（镰头）** | **0.9** | 镰刃最大宽 **0.85**、刃厚 **0.16** | 镰头比斧小一号 |
| **链身** | **3.6**（8 节 × 0.38 + 镰头 0.9） | 链环外径 **0.26**、链节厚 **0.09** | 链节可平铺复用 |

> ⚠ **宁可略小也不要大**：模型是"玩家手持/飞出的道具"，过大会挡住战场读数。
> 最终落位可由 Prefab 缩放微调，但**请按上表出图**，不要用比例去凑。

### 3.2 轴向与原点（最容易做错的一项）

- 模型本地 **+Y = 刃口/尖端方向**（沿武器主轴）。
- **模型原点 = 握持点 / 连接点**：
  - 大斧 ⇒ 原点在**握把末端**（手握的位置）；
  - 锁镰 ⇒ 原点在**链环与链身的连接点**。
- ⚠ 长枪的 prefab 根节点在**杆的中段**（双手长杆的特殊处理），**斧/镰不要照抄**：单手武器的旋转支点就在手里，原点放握持点，旋转才自然。
- 正面观察方向 = 模型的 **+Z 面朝观察者**；左右由 ±X 决定。

### 3.3 网格与性能（移动端）

- **单材质、单贴图、单 draw call**；三角面 ≤ **8k**、顶点 ≤ **4k**（每件）。
- 不用 LOD、不用粒子系统、不带动画片段（动作由代码驱动，见 §3.5）。
- 贴图：**1024×1024 PNG + Alpha**（锁链环单独出 **256×256**），UV 一套 0..1 覆盖整把武器，**不要 UV 壳**。

### 3.4 材质与着色器（**必须用项目自带的**）

- 着色器：**`Assets/Shaders/WeaponColorUnlit.shader`（`Custom/WeaponColorUnlit`）**。
  - 单贴图 `_MainTex`（贴图自带全部明暗与涂装）+ `_Color`（染色/淡出）+ ★ **`_FlashStrength`**（破防闪白通道，2026-11-13 新增）。
- **交付时不要自带材质**（若带了，必须与上述着色器同名同参数），否则闪白通道会失效。

### 3.5 动画：**不需要骨骼动画**

- 两个武器的动作**全部由代码驱动**：
  - 斧：`ActiveSkillRunner.RunAxeSmash` 的**抡起（0.18s）→ 劈下（0.12s）**两段位移+旋转；
  - 锁镰：`RunChainPull` 的**甩出 → 绷紧拽人 → 回收**三段。
- ⇒ 交付**静态模型**即可（无需骨骼动画、无需 FBX 动画片段）。

### 3.6 链身的两条路线（**默认走 A**）

| 路线 | 交付物 | 成本 | 说明 |
|---|---|---|---|
| **A（默认，推荐）** | **单个链环的静态模型/贴图** | 低 | 运行时由 `ChainLinkRig` 生成 **9 节骨骼**（弹簧+距离约束），按骨链把链环摆到位 ⇒ **不需要绑定骨骼** |
| B（可选升级） | 带骨骼的蒙皮链（8 根骨） | 高 | 需要把 `ChainLinkRig` 换成驱动该蒙皮的版本；收益是链节能有厚度与扭转 |

> ⇒ **美术侧请按 A 出**：链环一个（可平铺），镰刀头一个。

### 3.7 工程插拔位（**模型到位只改资产，不改代码**）

| 物件 | 放哪 | 谁引用 |
|---|---|---|
| 大斧模型 | `Assets/Prefabs/SkillWeapons/Axe3D.prefab` | `ActiveSkill_AxeSmash.asset` → `axeSmashLevels[].weaponPrefab` |
| 锁镰模型 | `Assets/Prefabs/SkillWeapons/ChainSickle3D.prefab` | `ActiveSkill_ChainPull.asset` → `chainPullLevels[].weaponPrefab` |
| 链环贴图 | `Assets/Sprites/SkillWeapons/chain_link.png` | `chainPullLevels[].chainLinkSprite`（留空则用 Inspector 上的默认链节贴图） |

**Prefab 结构（照抄 `FantasySpear3D.prefab`）**：

```
Axe3D / ChainSickle3D（根）
 ├─ MeshWeaponVisual        ← 必须
 │    renderers[]           ← 全部 Renderer（含 SkinnedMeshRenderer）
 │    modelRoot             ← 模型根（可指向子节点）
 │    tipAnchor             ← 刃尖 / 镰尖
 │    tailAnchor            ← 握把末端 / 链尾端
 │    fadeMaterial          ← 淡出用材质槽
 └─ (可选) WeaponTasselDynamics ← 提供 Length（需 weaponTip / weaponTail 两个锚点）
```

⚠ **`tipAnchor` / `tailAnchor` 是硬要求**：姿态系统的"矛头指向 + 矛头落点"解算、`Length`、格挡判定都依赖它们。
⚠ 若不加 `WeaponTasselDynamics`，`MeshWeaponVisual.Length` 会退化为 0 ⇒ 姿态解算失效 ⇒ **要么加，要么在 Prefab 里补好两个锚点**。

---

## 4. 美术需求 A：抡斧（巨斧）

### 4.1 形态要求

> ★ **2026-11-14 实测修正（连修三次才对）**：本节初稿按「月牙形单刃」写 ⇒ 与 PBZ 官方立绘不符。
> 反复放大官方立绘的头部后，准确结论是：
> **PBZ 巨凶斧 = 一整块实心厚重的铁块，从柄端向一侧张开成宽扇形/倒三角；
> 刃口是一条斜着的长边（不是弧）；上侧是厚钝的斧背；靠柄处只有一个小浅内凹。**
> 参考图：`SkillWeaponsArtSource/01_Reference/pbz/juxiongfu_axe.png`（2048²）
> 与 **`juxiongfu_axe_head_zoom.png`（930×810，头部特写，★ 形状的唯一权威）**。
> 官方描述：「**斧身厚重**，一击可开山裂甲，破盾能力强」「曾为**斧老大彭猛**所用的厚刃巨斧」。

- **斧头是「一整块实心厚重的巨大钢板」**，从柄端**向一侧张开**：
  - **刃口是一条平滑连续的圆弧**（从上方那只角绕过右侧一路弯到下方的尖端）。
    ★ 要**平滑连贯**，中途不拐弯；不要直线斜边、不要折角、不要尖三角。
  - **圆弧以内是实心金属**（厚度可读、有分量）：**不是空心的月牙片**、不是薄钩刃、不是镂空。
    ⇒ 「圆弧」说的是**刃线的走向**，不是**外轮廓的挖空**。
  - 斧背厚钝；断面是**上厚下薄的楔形**（斧背厚重、刃口收成一条锋利的刃线）——**这是斧与铲的根本区别**，
    等厚的板状刃面会读成铲子。
  - ★★ **斧背保持干净**：只有钢面本身的锻打纹理与明暗分档。
    **不要**铆钉排、铆钉头、纹章、雕花、装饰铁片、附加铁条。
    皮革绑带与钢箍**只允许出现在柄与斧头的连接处**，**不横跨刃面或斧背**。
  - ★★ **绝对不要做成月牙形 / 弯月刃 / 镰刀 / 钩镰 / 薄片**。
    （v2~v6 反复做成「月牙铲」的根因：提示词里一直写「月牙形 / 凸弧 / 月牙缺口」，
    生成器就照着画弧 —— 是**措辞**的问题，不是参考图的问题。）
- **柄端下方单独垂着一根长尖喙刺**（像鸟喙 / 倒钩，PBZ 上约占头宽的一半）；
  **柄尾有一组小的倒钩尖刺**（像撬棍头）。
- **长柄**（柄约占全长 **58%**，斧头约 42%）：深色杆，近斧头处与杆身中段各有一道**缠握**。
  ⚠ 本项目做**长柄**（与 PBZ 一致，也与 §3.1 的尺度数字自洽）；初稿写的「不是长柄斧」已作废。
- 斧头与柄的连接处有**钢制环箍 + 皮革绑带**（只在连接处，不横跨刃面）。
- 刃面是锻打分面的冷蓝灰钢，**刃缘是亮银白**（沿那条圆弧的细窄刃线）。
- ★ **本项目的变体（与 PBZ 的唯一差异，已定）**：PBZ 的头**不对称**（一角厚钝斧背、另一角带喙刺）、
  刃口是偏直的斜边；本项目取**刃口为平滑圆弧**（用户 2026-11-14 定），
  理由是规格要求 **+Y 指向刃口方向**（与长枪的“尖端”同构），四视图与建模的朝向才没有歧义。
  ★★ **形状的唯一权威 = PBZ 头部特写**（本地 `juxiongfu_axe_head_zoom.png`）
  ⇒ **建模与出图以它为准；AI 概念图只负责材质与气质参考。**
- 风格按**基础档**（见 §3.0）：金属件**冷蓝灰的钢**、刃缘**亮银白**、缠握**暖棕旧皮革**、木杆**暖棕**；
  **不用鎏金**（留给高级档）；**不要褐锈铁 / 近黑木 / 整体压暗**（与实机不搭）。
- 静态姿态：**整把竖直正立、握把在下、斧头在上端并向 +X 一侧张开**，刃口那条圆弧朝右下。不要斜放。


### 4.2 物理合理性（模型侧）

- 重心靠近斧头（单手抡砍成立）；柄长 ≈ 斧刃宽的 2.5~3 倍。

---

## 5. 美术需求 B：追命锁链（锁链 + 镰刀）

### 5.1 形态要求

> ★ **2026-11-14 实测修正**：本节初稿按「单刃弯钩镰 + 长链」写，与 PBZ 官方立绘不符。
> 官方立绘 `SkillWeaponsArtSource/01_Reference/pbz/zhuiming_suolian.png`（2048²）。
> 官方描述：「曾为猎杀者追命所用的**一双**锁链镰刀」「盘绕、牵拽、收割」「利于中远距离攻击，也能钩住远处锚点进行移动」。

- **内侧开刃的弯钩镰**，镰尖指向 **+Y**；**镰刃根部一个带缺口的上扬爪形头**（像爪/兽头）。
- **分叉的握杆**，杆上缠**粗绳**（PBZ 是深红，我们按 §3.0 用**朱红**），绳上串几枚**小金属环与新月形小片**。
- 握杆**下端收细**、数道金属箍，**末端一个小挂环** = 链子挂点（模型原点见 §3.2）。
- **链身**：8~9 节**可辨识的链环**（每节是闭合椭圆环，有链节间的交叠关系），链环直径一致，**不是实心粗绳**。
- ⚠ **一件还是一对**：PBZ 是「一双」，但本项目的技能只抛**一条链**、生成**一个**镰头 ⇒ **本项目做一把**（见 `plan/weapon-concept-image-plan-20261114.md` §8 待拍板 2）。
- 风格按**基础档**（见 §3.0）：**冷蓝灰钢 + 亮银白刃缘 + 暖棕木与皮革**；**不用鎏金**（留给高级档）。
- 静态姿态：**链自然下垂**（略带弧度，不是笔直绷紧、也不是缠绕成一团）。

### 5.2 拆分交付

| 件 | 用途 | 尺寸 |
|---|---|---|
| **链环**（单个） | 运行时按骨链平铺 9 节 | 外径 0.26、厚 0.09 |
| **镰刀头**（带环） | 飞向目标、挂住敌人 | 长 0.9、刃宽 0.85 |

---

## 6. 四视图生图提示词（**待用户批准后提交**；也可直接给美术/建模师当参考）

> 通用负面词（双语）：
> `人物、手、场景、文字、标签、尺寸线、坐标轴、边框、光效、残影、多视图合集、透视缩短、倾斜、镜像复用、改变比例、悬空零件、塑料感、卡通描边、发光。`
> `characters, hands, scenery, text, labels, dimension lines, axes, borders, glow, motion trails, multi-view sheet, perspective foreshortening, tilt, mirrored reuse, changed proportions, floating parts, plastic look, cartoon outline, emissive.`

### 6.1 抡斧 · 四视图

**中文（逐视图执行，一次只出一张）**
```text
根据输入参考图，画同一把【单手大斧】的{正面/左侧/右侧/背面}正交单视图。输入图只用于锁定斧头造型、刃口曲率、木柄粗细、配色与静止姿态。输出只含一把完整大斧，不生成多视图，不重新设计造型。

定义 +Y 指向斧刃方向，+X 是正面图画面右，+Z 朝正面观察者。左侧观察者在 −X 沿 +X 看，画面右为 +Z、上为 +Y；右侧从 +X 沿 −X 看；背面从 −Z 朝 +Z 看。只画正交投影，不透视缩短、不倾斜。

保留：不对称月牙形单刃斧头（刃口朝上）、厚实铆钉斧背、金属箍、木柄与皮革缠握、柄尾穿孔挂环。保持总长比例与所有部件高度关系，不加不减零件。斧头略偏向 +X 一侧，保持静止，不做挥舞姿态。

输出一张 1024×1536 纯白不透明 PNG，只含一把完整竖直大斧，居中，斧刃、柄尾、挂环全部可见，四边留至少 64 像素。写实偏硬的旧铁、深色木、局部旧金，柔和中性光照、干净边缘。
```

**English**
```text
Use the input reference to draw ONE orthographic {FRONT/LEFT/RIGHT/BACK} view of the SAME single-handed battle axe. The input locks the head silhouette, blade curvature, haft thickness, palette and the fixed resting pose. Output only one complete axe, not a multi-view sheet, and do not redesign the weapon.

Define +Y toward the blade, +X as screen-right in the front view, and +Z toward the front observer. The LEFT observer is at -X looking along +X (screen-right +Z, screen-up +Y); the RIGHT observer is at +X looking along -X; the BACK observer is at -Z looking toward +Z. Orthographic projection only, no foreshortening, no tilt.

Preserve the asymmetric crescent single-bit head (edge upward), heavy riveted poll, metal socket, wooden haft with leather wrap, and the pierced ring at the butt. Keep the overall proportions and all component heights; add and remove nothing. Keep the head slightly biased toward +X in a fixed resting pose, never mid-swing.

Output one opaque pure-white 1024x1536 PNG containing exactly one complete upright axe, centered, blade, butt and ring fully visible, with at least 64 pixels of margin on every edge. Worn iron, dark wood and restrained old gold, soft neutral light, clean edges.
```

### 6.2 追命锁链 · 四视图

**中文**
```text
根据输入参考图，画同一条【锁链 + 镰刀】的{正面/左侧/右侧/背面}正交单视图。输入图只用于锁定链环造型、环径一致性、镰头轮廓与配色。输出只含一条完整锁链，不生成多视图。

定义 +Y 指向镰尖方向，+X 是正面图画面右，+Z 朝正面观察者。观察方向同上。链身自然下垂、略带弧度，四张必须是同一个静止姿态。

保留：8~9 节闭合椭圆链环（环径一致、节间有交叠，不是实心粗绳）、链尾大连接环、镰刀头根部环、内侧开刃的弯钩镰（镰尖朝上）、旧铁与深色缠握质感。不要缠绕成一团，不要笔直绷紧。

输出一张 1024×1536 纯白不透明 PNG，只含一条完整锁链竖直悬挂（镰尖朝上、链尾在下），居中，全部可见，四边留至少 64 像素。
```

**English**
```text
Use the input reference to draw ONE orthographic {FRONT/LEFT/RIGHT/BACK} view of the SAME chain-and-sickle. The input locks the link shape, consistent ring diameter, hook silhouette and palette. Output only one complete chain, not a multi-view sheet.

Define +Y toward the hook tip, +X as screen-right in the front view, +Z toward the front observer; observers as above. The chain hangs naturally with a slight arc and all four views must show the SAME resting pose.

Preserve 8-9 closed oval links of consistent diameter with visible interlock (not a solid rope), the large connector ring at the tail, the ring at the hook root, and the inward-edged curved sickle with the tip upward. Keep the worn-iron and dark-wrapped-handle material. Do not coil it up, do not stretch it taut.

Output one opaque pure-white 1024x1536 PNG containing exactly one complete chain hanging vertically (hook up, tail down), centered, fully visible, with at least 64 pixels of margin on every edge.
```

---

## 7. 验收清单

**美术侧（四视图）**
- [ ] 四张单图同一姿态、同一比例，部件高度一致；左/右不是水平镜像（链环交叠关系、斧背铆钉要能区分）
- [ ] 刃口方向一致（+Y）、无透视缩短、无倾斜、无人物/手/背景/文字
- [ ] 白底、四边留白足够、主体完整不出画
- [ ] 合图顺序 = 前／左／右／后；中文标注版标注视图名与观察方向
- [ ] 记录生成参数、参考图职责、来源与许可（放 `01_Reference/`）

**3D 侧（模型交付）**
- [ ] +Y = 刃口方向；原点 = 握持点／连接点
- [ ] 尺寸符合 §3.1（斧 3.2 / 锁镰 0.9 / 链节外径 0.26）
- [ ] 单材质单贴图（1024² PNG+Alpha），用 `Custom/WeaponColorUnlit`，含 `_FlashStrength` 通道
- [ ] 面数 ≤8k、顶点 ≤4k、单 draw call
- [ ] Prefab：`MeshWeaponVisual` 的 `renderers / modelRoot / tipAnchor / tailAnchor / fadeMaterial` 全部填好
- [ ] 落到 `Assets/Prefabs/SkillWeapons/`，并在技能资产里指定（不改代码）

**工程侧（收到模型后由本项目做）**
- [ ] 技能资产填 `weaponPrefab` / `chainLinkSprite` → 战斗内可见、可被排序压住敌人
- [ ] 斧的抡起/劈下两段轨迹与新模型的比例匹配（必要时调 `RunAxeSmash` 的三个点位）
- [ ] 链的甩出/绷紧/回收三段与新模型的连接点匹配
- [ ] 移动端帧率与包体（`memory/android-build-state.md` 清单）

---

## 8. 已知坑（从长枪那一轮继承下来的）

1. **四视图不是 3D 投影认证**：对齐不等于精确投影，建模时以**标注的轴向与尺寸**为准，必要时自己量。
2. **不要用镜像冒充左侧**：水平翻转会让链环交叠/斧背铆钉位置错误。
3. **链环不能建成实心粗绳**：有厚度、有交叠，但截面要细。
4. **`tipAnchor`/`tailAnchor` 缺了 ⇒ 姿态解算直接失效**（`Length` 退化为 0）。
5. **新字段只改 C# 默认值不生效**：域重载会保留 ScriptableObject 内存值 ⇒ 数值/引用必须写进 `.asset` 并 `SaveAssets()`。
6. **图生四视图要逐张生成**：一次出多视图会把比例和细节带歪；一张一次、失败先分析不重发。
