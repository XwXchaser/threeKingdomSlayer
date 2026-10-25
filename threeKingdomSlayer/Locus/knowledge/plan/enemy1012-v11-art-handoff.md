---
id: kd_556e68ee-333b-4e31-af53-a50d4fef68c6
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_1012_v11_art_handoff_2026_10_25
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: 1012 = v11 刀兵形象的新角色编号（替换自 1011 的 v11 一轮，1011 已还原原本造型）。本文件是 1012 美术资源的上传与部署交接：上传只含美术资源与溯源包，Prefab / 敌人配置 / 代码映射 / 场景接线由接入方按本文部署。
---

# 1012（v11 刀兵形象）美术资源交接

> **一句话**：1011 的 v11 一轮产物已整体改名为 **1012** 并独立入库；**1011 已还原为原本造型**（精灵与控制器都回到仓库 HEAD）。本次上传**只含美术资源**，部署由接入方做。

## 1. 上传范围（只含美术资源）

| 类别 | 内容 | 路径 |
|---|---|---|
| 精灵 | 70 张（10 组动作） | `Assets/Sprites/Enemy/Enemy1012/Enemy_1012_<组><i>.png` |
| 动画 clip | 10 个 | `Assets/Animations/Enemy_1012_<组>.anim` |
| 控制器 | 1 个（状态与 1011 同名，motion 指向 1012 的 clip） | `Assets/Animations/Enemy_1012.controller` |
| 素材包 | 动作源片 9 支 + 身份图 3 张 + 溯源表 + 对照图 | `Enemy1012ArtSource/` |
| 文档 | 本文件 + `Enemy1011ArtSource/` 的 1011 一轮记录 | `Locus/knowledge/plan/` |

**未上传（接入方自行处理）**：任何 Prefab、敌人配置 ScriptableObject、场景、代码、`ProjectSettings`。
`Assets/Resources/EnemyPrefabs/Enemy_1011.prefab` 里别人的 `maxHealth 100→1000` 改动**未进本次提交**。

## 2. 接入方要做的事

1. 新建 **1012** 的敌人配置与 Prefab（美术侧**没有**提供 Prefab）。
2. Prefab 的 Animator 指向 `Assets/Animations/Enemy_1012.controller`；或用这套 clip 重建自己的控制器（状态名见下表）。
3. 代码侧沿用工程既有语义：`HitReactionDirection` → Animator 状态名（`Front → HitFlash`、`Left → HitLeft`、`Right → HitRight`）。
   **注意 1012 的 `HitFlash` 状态放的是 `Enemy_1012_HitHeavy`（正面受击新动作，7 键）**，与 1011 旧美术的 `HitFlash`（5 张旧帧）不是同一套。
4. 1011 保持原样（原本造型 + 原控制器），不要再引用 `Enemy_1011_hitHeavy*`（已不存在）。

## 3. 资产清单

### 3.1 控制器状态 → clip

| 状态 | clip | 精灵 | 时长 / fps / 循环 | 键 |
|---|---|---|---|---|
| Idle | `Enemy_1012_Idle` | idle1..6 | 0.6 s / 30 / 是 | 6（0→0.5，间隔 0.1） |
| Walk | `Enemy_1012_Walk` | walk1..6 | 0.6 s / 30 / 是 | 6 |
| Attack | `Enemy_1012_Attack` | attack1..16（首尾键用 idle1） | 2.9333 s / 30 / 否 | 18 |
| HitFlash | `Enemy_1012_HitHeavy` | hitHeavy1..6（末键 idle1） | 1.05 s / 30 / 否 | 7 |
| HitLeft | `Enemy_1012_HitLeft` | hitLeft1..5（末键 idle1） | 0.9 s / 30 / 否 | 6 |
| HitRight | `Enemy_1012_HitRight` | hitRight1..5（末键 idle1） | 0.9 s / 30 / 否 | 6 |
| Dead | `Enemy_1012_Dead` | dead1..6 | 0.6 s / 30 / 否 | 6 |
| Launched_Rise | `Enemy_1012_Launched_Rise` | rise1..6 | 0.6 s / 30 / 否 | 6 |
| Launched_Fall | `Enemy_1012_Launched_Fall` | landing1..6 | 0.6 s / 30 / **是** | 6 |
| Launched_Getup | `Enemy_1012_Launched_Getup` | getup1..6 | 0.6 s / 30 / 否 | 6 |

### 3.2 导入设置（逐组核对过，70 张一致）

`Sprite / Single`、PPU **16**、alignment **0**、maxTextureSize **2048**、filterMode **Point**、alphaIsTransparency **开**、textureType **Sprite**；
pivot：除 **attack** 与 **dead** 组为 `(0.5, 0.5)` 外，其余组为 `(0.6539, 0.5487)`（与同角色已部署精灵一致）。
精灵 1108×1108 画布，Unity 导入后纹理 1108→1024、bounds extents 34.63。

## 4. 命名口径（易踩坑，必读）

工程的左右受击命名口径：**身体被击后倒向哪一侧 = 哪个动作**（倒向画面左 = `HitLeft`）。旧美术的源片文件名与该口径相反，所以 v11 替换时做了**对调**：

- 1012 的 `HitLeft` 用 `sword_enemy_hit_right_v1.mp4`，`HitRight` 用 `sword_enemy_hit_left_v3.mp4`；
- 同一口径已同步到 **1021**：其原 `hitRight` 素材实测「上身相对脚底」偏移 −40.7…+3.6 px（倒向画面左），因此
  - 精灵 `Enemy_1021_hitRight1..6.png` → **`Enemy_1021_hitLeft1..6.png`**（GUID 不变）
  - clip `Enemy_1021_HitRight.anim` → **`Enemy_1021_HitLeft.anim`**（GUID 不变）
  - 1021 控制器状态 `HitRight` → **`HitLeft`**
  - **后果**：1021 现在没有 `HitRight` 状态，右侧受击按工程既有回落逻辑走 `HitFlash`；若要补一条右侧专属动作，可按 §5 的配方做（≈0.57321）。

## 5. 制作配方（可复现，见 `Enemy1012ArtSource/README.md`）

- 身份基线：`enemy1011_style_locked_dao_v11.png`（SHA `67da5838…c39`，已登记进 `design/art-style-guide.md`）。
- 身份图三张：`input_identity_v11_696.png`（主体高 696/脚底 896）、`input_identity_v11_dead_A696_ground830.png`（接触线 830 = 站立地面线）、`input_identity_v11_landing_A696_ground717.png`（接触线 717 = 躺姿地面线）。
- 视频参数：`seedance-2-fast` / 4 s（HitHeavy 6 s）/ 720p / 1:1 / 无音频 / 无水印 / seed −1；**一个动作只允许生成一次**（叠轮重画会让边缘锐度 3.71→3.33→3.14 逐轮下降）。
- 抠图：SVL Chroma **T=79 / softness 16 / halo 1 / 半透明转不透明**；逐帧实测键色（成片底色比请求键低 5–20）。
- 碎屑清理：alpha 连通域 <150 px 的岛丢弃（模型在起身段会喷红色尘点）。
- 落位：单一全局缩放 + 单一贴图偏移，1108×1108；击飞链共用 **k=0.93077 / 偏移 (106,172)**；站立参考帧对齐 idle1（主体高 605、脚底 898、中心 x=521）。

## 6. 已验收数值（可作为接入后的验收基线）

| 项 | 实测 |
|---|---|
| G2 背景片基 | 对基准键 (0,159,62)：ΔR 0…15、ΔG −22…0、ΔB −9…12 |
| 特效闸门 | 偏离 >25 的像素 ≤1500/帧（干净基线 idle 122 / walk 553 / attack 237）；亮像素 0 |
| 抠图 | holes 0 / plate_leak 0 / semi_transparent 0 / green>8 <1% 可见像素 |
| 链连贯 | 源片 rise 末帧 ≡ landing 首帧（差 3.31/通道）、landing 末帧 ≡ getup 首帧（差 3.49） |
| 链接口 | rise6→landing1 缩 17.5%（旧链 12.6%）；landing6→getup1 缩 6.1%（旧链 13.3%）；getup 站立帧 610/898/521 ≈ idle1 605/898/521 |

## 7. 已知偏差与遗留

1. **脸部**：dead / rise / landing / getup 的模型把嘴画成大张嘴（词里写的是闭嘴不露齿），用户已验收接受；walk/attack 的脸为冷脸。
2. **击飞链末段**：v11 的 rise→landing 之间刀已脱手飞在空中，landing 末段模型把刀放回手里（与 rise 的“刀已脱手”有轻微不一致，用户验收接受）。
3. **hitLeft6 / hitRight6** 不被任何 clip 引用（老问题）。
4. **1021 的右侧专属受击动作**缺一条（见 §4 后果）。
5. 1012 的 **Prefab / 敌人配置 / 关卡接线** 尚未做，等接入方按本文部署。
