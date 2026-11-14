---
id: kd_7c1a5e93-6b4d-4f21-9c07-2ad3e5f6b1d8
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 抡斧 / 追命锁链：美术需求准备（2026-11-14）

> **状态：AI 生成造型这条路已放弃并全部清除。**
> 本项目之前用 AI 生成过 9 版概念图 + 3 版正交视图板，**全部判定不合格**，
> 样本、提示词、生成脚本与本机生成档案**已全部删除，不保留任何失败样本**。
> ⇒ **现在唯一的交付物是需求书**：`design/weapon-3d-art-spec-axe-and-chain.md`（已重写成可直接交给制作方的独立任务书）。
> ⇒ **造型参考与画风参考**见 §2。

---

## 1. 需求书（交付物）

`design/weapon-3d-art-spec-axe-and-chain.md` —— 重写为**独立任务书**，不依赖任何上下文，包含：

| 章节 | 内容 |
|---|---|
| §0 | 一句话任务 + 两件武器的玩法定位 |
| §1 | 交付清单（3D 模型放哪 / 四视图规格） |
| §2 | **必读参考**（3 组，各自只负责什么、不负责什么） |
| §3 | 造型规范（抡斧 / 锁链 / 链身走「单环平铺」路线） |
| §4 | 尺度与坐标（世界单位、尺寸表、轴向与原点、四视图观察方向） |
| §5 | 配色分档（基础档 / 高级档 / 基础档不要做的） |
| §6 | 材质与渲染（`Custom/WeaponColorUnlit`、`_FlashStrength`、面数、UV） |
| §7 | 不需要骨骼动画（动作由代码驱动） |
| §8 | Prefab 结构与工程接法（插拔位已预留） |
| §9 | **★ 红线（13 条不要做什么）** |
| §10 | 验收清单（美术侧 / 3D 侧 / 工程侧） |
| §11 | 已知坑（7 条） |

**形状描述的来源**：不是猜的，是从 **PBZ 官方立绘实测**得到的（见 §2.1）。

---

## 2. 保留的参考资产

`SkillWeaponsArtSource/01_Reference/`（**只有这两类，别再加别的**）：

### 2.1 造型参考（第三方素材，只留本地、不进仓库）

| 文件 | 尺寸 | 来源 | 负责什么 |
|---|---|---|---|
| `pbz/juxiongfu_axe_head_zoom.png` | 930×810 | PBZ《影之刃零》巨凶斧官方立绘的**头部裁切** | **大斧头的体量与结构**：实心厚钢板、厚钝斧背、柄端长尖喙刺、连接处的钢箍与绑带 |
| `pbz/juxiongfu_axe.png` | 2048×2048 | 同上整图 | 整斧比例与配色参考 |
| `pbz/zhuiming_suolian.png` | 2048×2048 | PBZ《影之刃零》追命锁镰官方立绘 | **锁链镰形制**：内侧开刃弯钩、爪形头、分叉握杆、缠绳串环、下端金属箍与末端挂环 |
| `pbz/references.csv` | — | — | 来源页 URL + sha256 台账 |

来源页：`wiki.biligame.com/yingzhiren/影之武装`（图鉴页）与其词条页 `/巨凶斧`、`/追命锁镰`。

### 2.2 画风参考（自产，**从仓库自己的实机截图裁出**，已入库）

| 文件 | 内容 |
|---|---|
| `style/game_palette_board.png` | 8 格实机色卡：冷蓝灰甲片 / 银白刀刃 / 朱红旗帜 / 米白布衣 / 暖棕皮带 / 木+铁箍 / 暖黄地面 / 亮蓝天空 |
| `style/game_wood_iron_ref.png` | 暖棕木板 + 深蓝灰铁箍 + 铆钉的材质做法 |
| `style/game_saber_ref.png` | 银白钢刃的明暗分档 |

来源：`_art_process/VisualLibrary/shots/battle_enemy_1012.png`（游戏实机截图）。

### 2.3 ★ 取参考的两条经验（下次直接照做）

1. **不要用图片搜索引擎找"某游戏的某件武器"** —— 实测 Bing 对中文词 **0 命中**、对英文词返回**同一批无关图**（手机壳 / 摩托 / 头盔）。
   ⇒ **直接去官方 wiki / 图鉴页拿立绘直链**：图鉴页的缩略图 URL 去掉 `/thumb/…/260px-xxx.png` 就是原图。
2. **画风参考要从本项目自己的实机截图裁**，不要靠读贴图猜颜色
   —— 读 `FantasySpear_BaseColor.png` 会得出"金 + 朱红 + 暖棕木"，而实机里是**冷蓝灰钢 + 亮银白 + 暖棕木**。

---

## 3. 已清除的内容（不要恢复）

| 已删 | 说明 |
|---|---|
| `SkillWeaponsArtSource/02_Concept/` | 9 版 AI 概念图（v1~v9），**全部不合格** |
| `SkillWeaponsArtSource/03_FourView/` | 3 版正交视图板 + 4 张单图 + 校验 JSON，**画风过时且顶/底视画错** |
| `Tools/visual_brief/submit_weapon_concept.py`<br>`Tools/visual_brief/submit_weapon_view.py`<br>`Tools/visual_brief/assemble_view_board.py` | 本轮为该任务新建的 AI 生成/拼板脚本 |
| `Tools/visual_brief/prompts/axe_*.txt`<br>`Tools/visual_brief/prompts/refs_axe_*.json` | 对应的提示词与参考图清单 |
| `C:\Users\steam\Pictures\gptGen\skill_weapon_concept\`<br>`C:\Users\steam\Pictures\gptGen\skill_weapon_view\` | 本机生成档案（原始响应、留痕） |
| `Library/Locus/tmp/` 下的对比图、抓取垃圾池 | 临时物 |

> ★ **不要重新跑这条流水线**：它产出的造型已被判定为垃圾，且成本不低（该任务共约 15 次付费生成）。
> 需要新图时**人工制作**，参考 §2。

---

## 4. 两条工程侧经验（避免以后重蹈）

1. **内容安全策略会拦「亮银白的大刃」**：旧画风（暗铁锈）的图当编辑参考能过，亮银白大刃的图一律 400；
   含「刀」的图也 400；正文里写「**刀背**」会直接触发（同段「斧背」不会）。
   ⇒ 若日后还要用 AI 出武器图：**用「斧背」不用「刀背」**，排除项统一放负面词文件（主提示词只留一句「不要…」）。
2. **道具类武器不要指望 AI 出正交视图**：生成器做不出大刃武器的端向投影（顶/底视一律画成刀／矛尖），
   一次出多视图还会把比例带歪。

---

## 5. 下一步

| # | 事项 | 状态 |
|---|---|---|
| 1 | **把需求书发给制作方** | ★ 现在就可以发：`design/weapon-3d-art-spec-axe-and-chain.md` |
| 2 | 模型到位后接进工程 | 资产里 `axeSmashLevels[].weaponPrefab` / `chainPullLevels[].weaponPrefab` / `chainLinkSprite` **已预留**，只改资产、不改代码 |
| 3 | **REQ-010 防御→攻击衔接** | 用户已拍板：方案 A（同一把枪连续运动）+ 出招即脱离防御态 |
| 4 | 锁链的概念图 | **不再用 AI 做**（同 §3）；随需求书一起由制作方出 |

## 6. 相关文档

- **需求书**：`design/weapon-3d-art-spec-axe-and-chain.md` ★
- PBZ 调研（玩法数据）：`design/pbz-reference-analysis-20261109.md`
- 本周计划：`plan/week-20261114-new-enemy-two-skills-parry-counter.md`
- 画风总纲：`design/art-style-guide.md`
- 流程铁律：`memory/art-production-workflow-entry.md`
- 视觉库索引：`memory/visual-library.md`
