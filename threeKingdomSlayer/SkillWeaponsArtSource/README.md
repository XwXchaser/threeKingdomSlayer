# SkillWeaponsArtSource —— 技能武器美术素材源

**参考类只放造型参考、画风参考；生成类只放已验收的成品（当前只有 `03_FourView/`）。**
被判定不合格的生成样本（曾 9 版概念图 + 3 版视图板）**仍不得恢复**。

## 目录

| 目录 | 内容 | 是否入库 |
|---|---|---|
| `01_Reference/pbz/` | PBZ《影之刃零》官方立绘（巨凶斧 / 追命锁镰）+ 斧头特写 + 来源台账 | ❌ 第三方素材，只留本地 |
| `01_Reference/style/` | 从本项目实机截图裁出的画风参考（色卡 / 木料铁箍 / 钢刃） | ✅ 自产，入库 |
| `03_FourView/` | **已验收**的抡斧正交四视图：单图 384×1024（front/left/right/back）+ 无字合图 1536×1024 + 中文标注版 1856×1440 + 生成留档 JSON | ✅ 入库 |
| ~~`03_FourView/chain_sickle/`~~ | **追命锁链**四视图：2026-11-15 生成，同日**被用户否决**，已撒出本包（归档在 `C:/Users/steam/Pictures/gptGen/chain_sickle_four_view_rejected_20261115/`），**不得再入库** | ❌ 已撒出 |
| `04_Model/` | **抡斧 3D 模型与贴图**（2026-11-15）：UV 修复后的 `.blend`（木色/钢色顶盖两版）+ `.fbx` + 2048² basecolor/normal PNG + 修复记录 JSON | ✅ 入库（不入 Unity） |
| `05_VfxConcept/` | **抡斧砸地裂纹特效 · 概念板**（2026-11-16）：① v1 动画分镜板（3×2 六格，**已被用户判为冲击力不足**）② v2 冲击语言 2×2 四格对照板（**已被判为 3D 地形断层画风**）③ **v3 战斗特效叠加概念板**（3×2 六格：纯裂纹 / +金白核 / +朱红 / +碎土 / +拖痕 / 综合版，**扁平叠在地面上的 VFX，当前待用户评审**）+ 中英提示词 + 负面词 + request/status 留档 + 逐格量测 JSON。**只用于评审，不是最终贴图，不得导入 Unity** | ⏳ 待用户验收 |

## 唯一需求源

`Locus/knowledge/design/weapon-3d-art-spec-axe-and-chain.md`
—— 交这份给制作方即可，不需要其它上下文。

## 约定

- `01_Reference/` 里的参考图**只保留上面这些**；不要再新增 AI 生成样本
  （本项目曾生成 9 版概念图 + 3 版视图板，全部判定不合格并已删除，
  见 `plan/weapon-concept-image-plan-20261114.md` §3）。
- `03_FourView/` 是 **2026-11-14 用户验收后**才建立的，存当前这一套四视图；
  ⚠ 四张是**各自独立重绘**，不是同一把武器的精确投影（斧头曲率、缠握位置、尾钩形状在视图间有出入），
  建模以需求书 §3/§4 为准，四视图只作朝向与体量参考（详见 plan §5.10）。
- **锁镰四视图已于 2026-11-15 被否决并撒出本包**（归档在本机 `gptGen/chain_sickle_four_view_rejected_20261115/`），
  锁镰造型待定、新方案由用户另处准备（详见 plan §5.17）。**不要重新入库。**
- `04_Model/` 里是**已修复贴图的抡斧模型**（用户修复版几何 + 顶盖 UV 重排 + 重绘贴图）；
  执行记录见 `plan/axe-model-texture-fix-20261115.md`。
  ⚠ 尚未满足需求书 §6（要 1024² PNG+Alpha、**一套 0..1 UV 覆盖整把武器、不要 UV 壳**、单贴图），
  也尚未按 §4.2/§4.3 调尺寸与轴向（当前 Z-up、高约 1 m）——工程接入前必须补。
- 第三方参考图的来源页 URL 与 sha256 记进 `01_Reference/pbz/references.csv`。

## `05_VfxConcept/`（2026-11-16，抡斧砸地裂纹 · 动画概念分镜板）

- **用途**：在生成最终透明贴图之前，先用一张 3×2 六格分镜板确认裂纹形状、径向扩散与淡出的读感。
- **产出**：`axe_crack_storyboard_v1.png`（1536×1024 RGB，sha256 `8dfd3b6b…4b85`）+ `*.prompt.txt` / `*.prompt_cn.txt` /
  `*.negative.txt` / `*.request.json` / `*.status.json` / `*.panels.json`（六格量测）。
- **提交参数**：`gpt-image-2.6-sunburst` / `/v1/images/edits`（4 张 `image[]`：两张实机截图 + 1011 精灵 + 抡斧四视图，6.14 MB）/
  `1536x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1` / 不传 background；HTTP 200，182.22 s。
- **量测结论**：六格序列与需求一致（0 → 47% 显现 → 100% → 101% 保持 → 淡出但仍可读 → 归零），
  像素统计见 `axe_crack_storyboard_v1.panels.json`。
- ⚠ **只用于评审**：不是最终贴图，**不得导入 Unity**；最终 `Assets/Sprites/EffectSprites/axeCrack/AxeGroundCrack.png`
  仍需另一次生成（1024²、真 Alpha、中心＝命中点）。

### v2：冲击语言 2×2 四格峰值对照板（2026-11-16，替代 v1）

- **产物**：`axe_impact_language_board_v2.png`（1536×1024 RGB，sha256 `277a4d45…3e99`）+ 同名 `.prompt*.txt` / `.request.json` /
  `.status.json` / `.panels.json`。
- **提交参数**：`gpt-image-2.6-sunburst` / `/v1/images/edits`（5 张 `image[]`，合计 1.86 MB）/ `1536x1024` /
  `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1`；HTTP 200，104.21 s，usage total 9475。
- **失败史**：前 4 次均被**上游故障**挡回（503 / 504，request id 见 `plan/axe-skill-integration-20261115.md` §6.15），
  第 5 次成功；**降体积并未解决问题**，原样重发 + 等待才是有效解。
- **量测**：四格中心均为实心破碎核（暗像素 0.34–0.44），地面中位色一致、外圈干净 ✔；
  偏差是 A/B 被画成较深的沟槽、冲击区占比 25%–37%（目标 55%–65%）。
- ⚠ 选定方向后还要做第二轮**时间分镜**（`0.00/0.02/0.06/0.16/0.28/0.40/0.50s`）；
  当前 shader 是单贴图径向遮罩 + 统一淡出，做不出瞬时爆核与碎屑，需另行讨论工程改动。

### v3：战斗特效叠加概念板（2026-11-16，按 §6.15 方案，替代 v1/v2）

- **产物**：`axe_combat_vfx_overlay_board_v3.png`（1536×1024 RGB，sha256 `670adb56…eee8`）+ 同名 `.prompt*.txt` / `.request.json` /
  `.status.json` / `.measure.json`。
- **提交参数**：`gpt-image-2.6-sunburst` / `/v1/images/edits`（5 张 `image[]`，1.86 MB）/ `1536x1024` /
  `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1`；**第 1 次即 HTTP 200**，112.26 s，usage total 9896。
- **量测**：六格地面中位色完全一致；效果覆盖 15.1%→21.3%、裂纹像素 10.6%→14.9%（逐格叠加关系成立）；
  金白核心仅占画面 ~1%；饱和朱红第 1、2 格为 0、第 3 格 2.03%。
- **相对 v2 的关键改善**：全部是**平铺在地面上的扁平图层**，竖直断面 / 坑壁 / 峡谷 / 岩板 / 3D 岩石全部消失。
- **残留偏差**：裂纹仍有 12 个角度扇区全部有质量（偏树根状，只做到"不完全均匀放射"）；金白核心是放射星芒；
  第 4 格碎土像带阴影小石子；第 5 格拖痕读成泥抹痕；特效占幅 45%–60%（略超 35%–50%）。
