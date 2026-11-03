---
id: kd_398ff36c-f7e5-4e15-95b5-007b2cdf9810
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_slash_c2_art_request_20261103
injectMode: inherit
aiEditMode: inherit
---

# 美术资源清单 · 张飞·斩击 C2（SlashC2）（2026-11-03）

> 需求来源：`design/combo-zhangfei-slash-c2.md`（连招文档 §4「表现」待补项）。
> 画风基准：`design/art-style-guide.md`（三国战场 / 笔触金属 UI / 猛烈战斗特效）。
> 图标规范：`design/skill-item-icon-art-guideline.md`。

---

## 0. 现状（2026-11-03 实测，美术改前必读）

**斩击（Slash）现在的表现链**：

```
AttackSystem.ExecuteSlash
  └─ SweepEffect.Create(prefab: cfg.attackWavePrefab, rotateSprite1/2, useEnhancedSlashMotion: true, enableWeaponAfterimage: true)
       ├─ 武器模型：Assets/Prefabs/FantasySpear3D.prefab（MeshWeaponVisual，根 scale 11.05，Model 子物体 localRot X 270°）
       └─ 旋转拖尾：2 张贴图（原本是**全局**的，见下）
```

| 东西 | 现在在哪 | 尺寸（磁盘 / 运行时） | 备注 |
|---|---|---|---|
| 旋转拖尾 A/B | `Assets/Sprites/zhangfei/stab_rotate1.png`、`stab_rotate2.png` | 1280×1280 → 导入 512×512，PPU 6.4（世界宽 80 单位） | 挂在场景 `AttackSystem` 的 `_stabRotate1Sprite/_stabRotate2Sprite`（**全局**，Slash 与终结技共用） |
| 蓄力指示 5 张 | `stab_charge1/2`、`stab_charge_ready`、`stab_charge_loop1/2`（`Assets/Sprites/zhangfei/`） | 1280×1280 → 512×512，PPU 6.4 | 挂在场景 Player 上的 `ChargeStabVisual`；跟随手指，**突刺风格** |
| 蓄力武器模型 | `ChargeStabVisual.stabPrefab` = `FantasySpear3D.prefab` | — | 3D 枪，蓄满时后拉 0.45 + 暖金染色 (1.55, 1.25, 0.5) |
| 扫掠扇形贴图 | `Assets/Sprites/zhangfei/slash.png`（`sweep.prefab`） | 1280×1280 → 512×512 | **当前 Slash 不用它**（Slash 用 3D 枪模型扫掠），别照它出图 |

**两个美术必须知道的前提**：

1. **斩击没有「枪体停住等蓄力」**：连段蓄力的枪体定格是 **Stab 专属**
   （`AttackSystem.BeginComboChargeHold()` 依赖 `_lastStabEffect`，上一段不是 Stab 就直接返回）。
   所以「Slash → 蓄力」期间玩家看到的是**跟随手指的蓄力指示**（`ChargeStabVisual`），场景里没有停住的枪。
   → SlashC2 的蓄力姿态要靠**这套跟随指示**表现；「枪体停住」是另一件事（见 §4 待定项）。
2. **拖尾贴图已改成 per-move**：`AttackSkillConfig.slashRotateSprite1/2`（我已加），
   留空 = 回退全局。→ B1 出图后直接填进 `Zhangfei_SlashC2` 资产即可，不用改场景。

---

## 1. 资源清单

导入设置（**A/B/C/D 全部统一**，与现有 `Assets/Sprites/zhangfei/` 同族素材一致）：
`Texture Type = Sprite (2D and UI)`｜`Sprite Mode = Single`｜`Pixels Per Unit = 16`｜`Max Texture Size = 512`｜`Filter Mode = Point`｜`Alpha Is Transparency = ✔`｜`Mesh Type = Tight`｜压缩 = Normal｜不生成 MipMap。

> 出图请按 **1280×1280** 交付（与同族素材一致，导入后自动缩到 512）。
> 命名一律小写下划线，与现有素材同族：`slash_c2_*.png` / `slash_charge*.png`。

| # | 资源 | 什么时候出现 | 出图尺寸 | 文件名 | 落点（谁来挂） |
|---|---|---|---|---|---|
| **A1** | 斩击蓄力指示 · 蓄力中（进度 < 65%） | Slash 之后按住蓄力 | 1280×1280 | `slash_charge1.png` | `ChargeStabVisual`（需要「按招式切样式」的代码钩子，见 §4） |
| **A2** | 斩击蓄力指示 · 蓄力中（进度 65%→100%） | 同上 | 1280×1280 | `slash_charge2.png` | 同上 |
| **A3** | 斩击蓄力指示 · **蓄满** | 松手前一刻 | 1280×1280 | `slash_charge_ready.png` | 同上 |
| **A4** | 斩击蓄力指示 · 蓄满后循环（2 张交替） | 蓄满后等待松手 | 1280×1280 ×2 | `slash_charge_loop1.png`、`slash_charge_loop2.png` | 同上（循环间隔 0.3s） |
| **B1** | **强化斩击拖尾**（旋转残影 A/B） | SlashC2 挥砍时 | 1280×1280 ×2 | `slash_c2_rotate1.png`、`slash_c2_rotate2.png` | `Zhangfei_SlashC2.slashRotateSprite1/2`（字段已加 ✔） |
| **B2** | 斩击弧光（宽刀光，**可选**） | SlashC2 命中瞬间 | 1280×1280 | `slash_c2_arc.png` | 扫掠路径上（`SweepEffect` 的 visualPath），弧长对齐「覆盖 2 排」 |
| **C1** | 推挤撞击反馈 | 敌人被左右推开 1 排的瞬间 | 1280×1280 | `slash_push_impact.png` | 敌人脚下，朝推开方向偏移（朝向由代码给，贴图本身单一朝向即可） |
| **D1** | 天赋页图标 | `talent_move_slashc2` 节点 | **1024×1024**（按图标规范） | `QTE_SlashC2.png` → `Assets/Sprites/BatlleHUD/` | `TalentTree_Moves` → `talent_move_slashc2.icon`（拖进去即可） |

### 各资源的画面要求

- **A1~A4（斩击蓄力）**：与现有 `stab_charge*.png` **同构图、同挂点**（沿枪轴前 1/3），
  但视觉语言改成**斩击**：金属刃口反光 + 弧形能量线，而不是枪尖聚气点。
  颜色：未蓄满用冷铜金，蓄满（A3/A4）用**高饱和赤金**（要与突刺蓄力一眼区分）。
- **B1（强化斩击拖尾）**：比 `stab_rotate*` 更**宽、更亮**的弧形残影；
  世界尺寸与 `stab_rotate*` 对齐（80 单位宽），否则会看到「缩放跳变」。
  A→B 两帧的旋转相位差沿用现有两张 rotate 贴图的节奏。
- **B2（弧光）**：一道横向宽刀光，**中间厚两端薄**，中心与枪尖连线对齐；
  半透明，alpha 峰值 ≤ 0.85（现有 `slashSweepAlpha` 通道会再压一次）。
- **C1（推挤反馈）**：脚下短促撞击（尘 + 火星），**不做长拖尾**（位移只有 1 排，快）。
- **D1（图标）**：与 `QTE_Slash`（600×200）、`QTE_Stab`（200×200）同族构图；
  主体 = 一次**更宽的横斩 + 位移箭头**，一眼能看出「斩击的强化段」。

---

## 2. 验收口径

| # | 检查 | 怎么看 |
|---|---|---|
| 1 | 蓄力三态可辨 | 按住不放：< 65% / 65~100% / 蓄满 三段画面明显不同 |
| 2 | 蓄满后不抖 | 蓄满状态循环帧 0.3s 交替，不闪烁 |
| 3 | 斩击拖尾比母招更强 | SlashC2 的残影宽度/亮度明显高于普通 Slash |
| 4 | 拖尾不跳缩放 | 切换到 SlashC2 时残影世界尺寸与普通 Slash 一致（80 单位宽） |
| 5 | 位移有反馈 | 敌人被推开时有撞击表现，且不遮挡敌人血条 |
| 6 | 图标可辨认 | 天赋节点框内（~64px）能看出「斩击 C2」 |

---

## 3. 不需要美术出的（我这边做）

- 刀光宽度/覆盖排数：**2 排**（已在 `Zhangfei_SlashC2.rangeRows`）。
- 位移方向：跟随上一段 Slash 的方向（向左/向右），由代码给。
- 数值：伤害 35 / 架势 30 / 重攻击 / 窗口 0.55–1（资产里可调）。
- 蓄力时长与松手判定：现有连段蓄力机制，不改。

---

## 4. 我这边要补的代码钩子（等图到位一起做，或先做）

| # | 钩子 | 状态 |
|---|---|---|
| 1 | `AttackSkillConfig.slashRotateSprite1/2`（per-move 拖尾贴图） | ✅ **已加**（留空回退全局） |
| 2 | `ChargeStabVisual` 按招式切蓄力贴图（Slash 用斩击系、Stab 用突刺系） | ⚠️ 待加（出 A1~A4 时一起做） |
| 3 | 斩击后的「枪体停住等蓄力」 | ⚠️ **待你拍板**：现在只有 Stab 有；给 Slash 也做一套要改状态机（不是纯美术） |
| 4 | 位移撞击反馈的挂点与朝向 | ⚠️ 待加（C1 出图时一起做） |
