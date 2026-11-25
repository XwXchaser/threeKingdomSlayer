---
id: kd_43cd2f3c-d1ab-406d-8e57-02e14fb3cbb1
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 地面/路面接缝修复 —— 专项交接（2026-11-23）

> **这条线只做一件事**：让无尽卷轴的**地面路面**在任何位置、任何圈数都「看上去是连续的正常路面」。
> 本文只收地面相关（不含段调度、物件搬家、宝箱旋转等已于同轮修完的内容）。
> 上游交接：`plan/scroll-loop-handoff-20261123.md`（那一轮的全量改动）。本文是它的**地面切片 + 新增现状**。
>
> ⚠️ **2026-11-24 结案：本文部分假设已被推翻，勿照 §9 前两步施工。**
> 地面**就是多副本**（`copyLapRadius = 1`，相邻两份各负责接缝一侧），H1「多副本导致叠层」不成立；
> 真实原因是两份地面**都按整张地图绘制、缺少互补归属**。已用「本圈本地空间的互补半空间 + 走廊沿路 UV + 每圈相位」修复，
> **用户已实机验收**。现状口径 `design/endless-mode-design.md` §2.4；结案 `plan/endless-ground-continuity-repair-plan-20261123.md` §14。

---

## 0. 一句话现状

**N1 土路表现正常**；**村庄（左支石板路）出现严重叠层 —— 石板路与土路交错呈现**。
静态资产里已发现一处**与自家组件文档直接冲突**的配置（地面复制半径 = 1，组件 Tooltip 写明必须为 0），
这就是当前的首要嫌疑，但**尚未做运行时验证**（见 §3.1）。

---

## 1. 目标口径（不读会推错方向）

- 循环 = **神殿逃亡式无限前进 + 素材复用**，玩家永不回头，**没有环路、没有合流点**（2026-11-21 用户澄清）。
  `design/endless-mode-design.md` §1/§2.2 里旧的「真闭环 / 环路图」已作废，不要再按那个做。
- 唯一残留接缝 = **内容从「地图末尾」切回「地图开头」那一瞬**，要求只有一条：**看上去是连续的正常路面**。
  靠「内容密度 + 材质收尾」实现，**不靠几何闭合**。
- **剧情关（Finite + 手工）必须逐像素不变**。所有新逻辑用 `_YSCycleLength > 0` 之类开关隔离。
- 用户认可的基线形态：**土路贴图 UV = 地图空间**（不是沿路、不是世界 XZ）、**地面单份 + flat z 只封上界**。
  ⚠️ **后半句已过时（2026-11-24）**：现在是**两份地面互补归属**（半径 1），且**道路走廊**改用沿路坐标；只有路外保持地图空间。

---

## 2. 现场实读（2026-11-23，静态资产真值）

### 2.1 场景里 5 个 `YLapFollower`

`Assets/Experiments/CurvedScroll/YJunctionSample.unity` 实读：

| 物件 | `copyInsteadOfMove` | `copyLapRadius` |
|---|---|---|
| `Terrain - one mesh, fork painted in terrain`（**地面**） | **1（复制）** | **1** ⚠ |
| `N1 Far Landmarks` | 1（复制） | 1 |
| `Route Connections` | 0（搬家） | 0 |
| `Encounter Authoring` | 0（搬家） | 0 |
| `Supply Spawn Manager` | 0（搬家） | 0 |

⚠ **地面 `copyLapRadius = 1`，但 `YLapFollower` 自己的 Tooltip 写的是**：

> 「复制模式下**同时**保留前后各几圈的副本。⚠ **必须为 0**：shader 只有一个全局朝向角，只能把**一个朝向**的内容捋直。
> 前后圈的副本朝向不同，同时显示会出现一块**歪着的地面把正常内容盖掉**。」

按 `YLapFollower.EnsureCopies` 推得：`from = max(0, lap - radius)`、`to = lap + radius`
⇒ **开局 lap 0 时就有 lap0 + lap1 两份地面同时可见**（plus 原件被隐藏），其中 lap1 那份带 **+45° 累积旋转**。
这与「一块歪着的地面把正常内容盖掉」描述的是同一件事。**待运行时确认（§3.1）**。

### 2.2 shader 与材质实读

`Assets/Experiments/CurvedScroll/Shaders/YGroundJ1Branches.shader`（181 行）：

| 行 | 事实 |
|---|---|
| 68-75 | `o.map = i.vertex.xz * 世界缩放`（**副本局部**米）；`o.wuv = w.xz`（世界 XZ） |
| 129 | `clip(min(i.z + 14, maxZ - i.z))`：地面只画到「跟前固定距离」`_YSGroundMaxZ` / `_YSViewDistance` |
| 145 | **每圈裁剪只封上界**：`clip(_YSCycleLength - i.map.y)`，用的是**副本局部 flat z** |
| 156-158 | 土路/路肩/外侧贴图 UV = `i.map / 4`（地图空间，用户认可） |
| 163-169 | 石板入场 `entrance`（`i.map.y - _JunctionDistance`）、收尾 `stoneExit`（`i.map.y`）、`leftWeight = entrance * smoothstep(±_BranchMaterialBlend, rightDist-leftDist) * stoneExit` |
| 172-176 | 石板 UV = `i.map / _LeftStoneTileSize`，`leftGround` 与 `dirt` 按 `leftWeight` 插值 |

**两个必须知道的事实**：

1. **`o.wuv` 是死代码**：vert 里算好了「世界 XZ UV」（当初修「换圈重洗」的方案），
   **frag 从未使用**（全部贴图都吃 `i.map`）。回退时只改了 frag，vert 的注释与计算留了下来 —— 别被它误导。
2. **`i.map` 是副本自己的局部坐标** ⇒ 同一世界点在不同圈副本里 **UV 相位不同**。
   这正是「换圈那一瞬贴图重洗」的根因，也解释了为什么**多副本同时显示时会出现两套图案互相压**。

`Assets/Experiments/CurvedScroll/Authoring/J1_Ground_Branches.mat` 实读：

| 参数 | 当前值 | 备注 |
|---|---|---|
| `_StoneExitStart` / `_StoneExitFade` | **350 / 26** | 📌 `scroll-loop-handoff-20261123.md` §2.1 写的是 **300 / 28**，两者不符，需确认哪个是验收值 |
| `_LeftStoneTileSize` | 6 | 石板贴图世界尺寸 |
| `_LeftStoneRoadBlend` | 0.45 | 石板路→路肩过渡宽带 |
| `_StoneEntryLength` | 8 | 石板入场过渡长度 |
| `_BranchMaterialBlend` | 0.6 | 左右支材质混合带半宽 |
| `_RoadHalfWidth` / `_ShoulderWidth` | 2.45 / 1.5 | |
| `_JunctionDistance` / `_CurveRadius` | 190 / 28.159557 | 分叉点与弧半径 |
| `_YSGroundMaxZ` | 0（= 用 view distance） | 地面最远绘制距离 |
| `_LeftStoneTint` | (0.89, 1.04, 0.93) | |

> 该材质最后一次修改是 `6a355afb`（本轮卷轴提交），改动只加了 `_StoneExitStart/_StoneExitFade/_YSGroundMaxZ` 三项。

---

## 3. 当前问题：村庄路面严重叠层、与黄图路交错

**用户描述（2026-11-23）**：N1 黄图路看着正常；**村庄路面存在严重叠层问题，似乎与黄图路交错呈现**。

### 3.1 假设 H1（首要嫌疑，未验）：地面同时显示 2 份、朝向不同

- 证据：场景 `Terrain.copyLapRadius = 1`（§2.1）+ 组件 Tooltip 明写「必须为 0，否则一块歪着的地面盖掉正常内容」。
- 机理：两份地面各自套自己的圈帧（相差 45°），但 shader 只有**一个全局朝向角** `_YSPose/_YSAngle`；
  其中一份对齐、另一份是**歪的**，两份的 `_StoneExitStart`/`entrance` band 与 `i.map` UV 相位都在各自局部空间里算
  ⇒ 在村庄段（石板 band 边界清晰、图案对比高）表现为**石板与土路交错**；N1 土路图案均匀 ⇒ 不易看出来。
- **5 分钟判定**：Play Mode 里数 `Terrain - one mesh...` 的**活跃份数与众数朝向**：
  若出现 ≥2 份且 `rotY` 不同（如 0° 与 45°），H1 成立。
- 处置（若成立）：地面 `copyLapRadius` 改回 **0**；再回看接缝处是否露洞，露洞属「内容补满一圈」范畴，不是靠多副本解决。
- ⚠️ **已证伪（2026-11-24）**：多副本本身不是叠层原因；改回 0 会直接露洞。真实原因是两份都画整张地图、缺少互补归属。

### 3.2 假设 H2（若 H1 证伪）：单份地面 + 局部 UV 相位差

即 11-23 §3 已记录的那条：同一世界点在不同圈副本里 UV 差一个旋转 ⇒ 换圈「重洗一次牌」。
村庄石板因为 tile 尺寸 6m、纹理细节强，比土路更容易被眼睛抓到。

### 3.3 假设 H3：材质 band 跨越副本边界

`design/route-extent-and-content-source.md` §3 第 8 条：地面副本边界必须在**材质上首尾一致**，
**任何材质 band 都不能跨副本边界**，否则边界两边各算一套 ⇒ 出现一条**横切路面的硬边**。
`_StoneExitStart=350`、`_StoneExitFade=26` ⇒ 石板收尾区间 350→376，圈长 380，**band 完全在圈内**（当前配置本身不越界）；
但若把 start/fade 调大，就会跨边界 ⇒ 同一圈内石板半淡出又跳回 —— 复现时要盯这一点。

> 排查顺序：**先证伪 H1（改一个字段即可判）**，再谈 H2/H3。不要在未判 H1 的情况下去改 UV 方案。

---

## 4. 已修完（有实测数字，不要重做）

摘自 `plan/scroll-loop-handoff-20261123.md` §2.1：

| 项 | 验收数字 |
|---|---|
| 撑包围盒修视锥剔除（`YLapFollower` + `CreateCopy` 再撑一次） | N1 装饰 `isVisible` 圈1 **0 → 78**；地形副本 lap2 **0 → 1** |
| 路侧物件跨接缝瞬移 | 跨接缝前方物件数 **2 → 79**（一帧 Δ77）→ **Δ ≤ 2** |
| N1 远城墙跨接缝瞬移 | 整帧接缝帧间差 **79.5 → 31**（普通帧 32） |
| 空路段补满 | 新增 `EV to N3 - Roadside Approach [270,328]`、`N3 Village Exit - Seam Dressing [366,380]`，前向扫描圈0/圈1 **0 空洞** |
| N3 段声明范围 | `[323,328] → [322,376]`，相机不再在 358m 提前卸载村庄 |
| 接缝路面材质 | 接缝两侧 R−B = **119 / 113**（土路），村庄段 41（青石） |
| 无限关开局脚下地面空缺 | 裁剪只封上界后，开局底部中央 RGB **(194,141,80)** = 土路 |

---

## 5. 已撤回，**不要重做**（除非有新证据）

| 尝试 | 为什么撤 |
|---|---|
| 土路 UV 改**沿路坐标** `(along, dist)` | 「把路摊平」的映射，离路心越远畸变越大 ⇒ 路肩/外侧明显拉伸发歪 |
| 土路 UV 改**世界 XZ** | 世界固定而路面每圈转 45° ⇒ 贴图相对路面每圈转 45°（用户最早报的症状）。**注意：`o.wuv` 死代码就是它** |
| **沿路裁剪 + 半径 1** | 把 `map.y < 0` 那段裁掉 ⇒ **开局脚下地面完全错误** |
| 把路径改成环 / 环路图 / 合流点 | 与「无限前进」口径冲突 |

---

## 6. 测量纪律（必读，别重复踩）

1. **`Texture2D.GetPixels32()` 返回的数组会被复用** ⇒ 比较两帧必须显式 `Array.Copy` 深拷贝，
   并且**每次跑一个自检用例**（两个明显不同的画面必须给出大差异）。本文件作者曾因此误报「逐像素一致」。
2. **`Renderer.isVisible` 对被撑大包围盒的物件恒为 true** ⇒ 不能当「在屏幕上」用。
3. **`i.map.y` 是 flat z，不是路程**：分叉后每 1 米路程只推进 cos45° ≈ 0.707；圈边界（路程 380）处 flat z 只有 **328.6**。
4. **沿路距离必须「三候选取最近点」**（`YGroundJ1Branches.shader: AlongRoute()`），只用相对弧心的角度判会把尾段当弧上
   ⇒ 沿路距离卡在 ~210 不再增长（实测 d=250/300/380/600 全算出 ~208~211）。
5. 整帧上做「每列最强竖向跳变」拟合斜线，量到的是物件/路肩，**不是地面** ⇒ 测地面必须只留地面。
6. 直接改 `progress` 采样时，段调度要几帧才收敛，**向后跳不会卸载** ⇒ 验证要么连续向前走，要么重开 Play Mode。

---

## 7. 验收探针

| 判据 | 怎么量 |
|---|---|
| **单份地面** | Play Mode 数活跃 `Terrain - one mesh...` 副本数 = 1；众数朝向一致（H1 的证伪线） |
| 接缝逐帧连续 | 跨接缝相邻帧「前方 0~60m 已开启物件数」变化 ≤ 5；任何物件 1 帧内观察距离跳变 < 50m |
| 前向扫描 0 空洞 | 每 10m 采样，「前方 40m 内已开启 Renderer == 0」的采样点为 0 |
| 地面图案连续性 | 只留地面截两帧（跨接缝前后），`Array.Copy` 深拷贝后比对；自检用例必须先通过 |
| 村庄段材质归属 | 村庄段取样 R−B ≈ 41（青石）；土路段 ≈ 113~119；**不得在同一条横断面内左右跳变** |
| 剧情关 0 像素差 | 同 progress 下新旧实现逐像素比对 |

---

## 8. 关键文件索引

| 文件 | 作用 |
|---|---|
| `Assets/Experiments/CurvedScroll/Shaders/YGroundJ1Branches.shader` | 地面：弯曲投影 + 每圈裁剪 + 石板/土路材质（181 行，`AlongRoute()` 在 85-108） |
| `Assets/Experiments/CurvedScroll/Authoring/J1_Ground_Branches.mat` | 材质参数（§2.2 表） |
| `Assets/Experiments/CurvedScroll/Scripts/YLapFollower.cs` | 按圈摆放/复制；`copyLapRadius` Tooltip 在 33-36 |
| `Assets/Experiments/CurvedScroll/YJunctionSample.unity` | 场景：5 个 `YLapFollower`、段表、`Far Landmarks` |
| `Assets/Experiments/CurvedScroll/Scripts/YScrollSample.cs` | `EvaluateLap` / `GetLapFrame` / `Length` |
| `Assets/Experiments/CurvedScroll/Scripts/YRouteSegments.cs` | 段按沿路区间加载/卸载 |
| `design/route-extent-and-content-source.md` §3 | 不变式 8（材质 band 不得跨副本边界）、9（flat z ≠ 路程） |

---

## 9. 下一步顺序

> ⚠️ **2026-11-24：下列 1–2 步已作废，不要执行**（地面必须多副本；也不要加“半径 > 0 就报错”的校验）。
> 3–5 步已随 11-24 修复处理（H2 相位、材质参数核查、`o.wuv` 死代码已删）。原文保留供追溯。

1. **判 H1**（只读，5 分钟）：Play Mode 数地面活跃副本与朝向 ⇒ 若 >1 份不同朝向，先把 `copyLapRadius` 改 0 复测症状。
2. 若症状消失 ⇒ 记录为「多副本 + 单一全局朝向角」的必然结果，并把 `YLapFollower` 的 `copyLapRadius` 加**导入期/运行时校验**（>0 且是地面时报错），防止再被改回去。
3. 若症状仍在 ⇒ 转 H2：**开独立实验场景/开关**收敛，每次只改一项，用 §7 探针验收；**不要在 `YJunctionSample` 正式场景里试**。
4. 确认 `_StoneExitStart/_StoneExitFade` 的验收值到底是 **350/26**（资产真值）还是 **300/28**（文档值），并同步两处。
5. 顺手清理 `YGroundJ1Branches.shader` 的 `o.wuv` 死代码（或在注释里明确标「已废弃，勿用」），避免下一个人把它当成现役方案。

---

## 10. 待拍板

1. ~~地面到底允许几份副本？（当前场景 1，组件文档要求 0，shader 注释假设「同时只有一份」）~~
   ✅ **已定（2026-11-24）**：每圈一份、`copyLapRadius ≥ 1`，相邻两份共享接缝平面；旧 Tooltip「必须为 0」已改正。
2. ~~村庄石板的 `_StoneExitStart/_StoneExitFade` 验收值（350/26 vs 300/28）~~
   ✅ **已定（2026-11-24）**：材质保持 `_StoneExitStart=350`、`_StoneExitFade=26`；无限路径不再用它，而是按本圈 flat z 反算。
3. ~~若最终必须靠「过渡片」解决材质硬切，是否愿意为此做一张过渡贴图~~
   ✅ **不需要（2026-11-24 用户已验收）**：石板在圈界前淡回土路 + 走廊沿路 UV 已足够，本轮未新增任何美术素材。
