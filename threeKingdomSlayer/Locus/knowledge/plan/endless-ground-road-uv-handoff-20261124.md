---
id: kd_9596eb61-15d9-4e3a-8728-56d3aabc73dc
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 无尽关路面纹理坐标修复 —— 进度交接（2026-11-24）

> 用户因会话 400 报错要求暂停。**当前只有 1 个文件有未提交改动**，场景/材质/存档均未被写。
> 本文是续跑入口：现状态、已测数字、未解释的疑点、下一步顺序。

---

## 0. 一句话现状（2026-11-24 已结案）

已实现**道路走廊沿路坐标 + 每圈纹理相位**的修复（仅无尽路径生效），
剧情关三处机位数值验证**逐像素不变**；§5 的覆盖疑点已查明为**远处投影回折**（非近景共面双绘，见 §10.1），
相位污染已修（§10.2），**观感已经用户实机验收**。

⇒ 本文保留作取证记录；**当前口径以 `design/endless-mode-design.md` §2.4 与
`plan/endless-ground-continuity-repair-plan-20261123.md` §14 为准。**

---

## 0.1 上一会话为什么中断（400 的真因，已用日志确认）

上游返回原文：

```
400 Bad Request
{"error":{"message":"Invalid request: Invalid request:
 request body exceeds 128 MB: request body too large","type":"muskapis_error"}}
```

- **不是**代码/Shader/场景故障，**也不是** token 撑爆；是**单次请求体积超过 128 MB**。
- 同一会话三次重试全部同一条错误，随后会话被判失败 ⇒ **重试无效**。
- 成因：本会话累计 17 张 1080×2160 Game 截图（**46.1 MB PNG**）+ 历史截图与贴图读取，
  base64 约 1.37× ⇒ 约 87–90 MB，叠加文本即越限。历史里的图**无法回收**。
- 规则已固化到 `skill/session-image-payload-discipline.md`：截图分级（480/640/原图）、
  优先「写盘截图 + 脚本统计，只回数字」，必须人眼判断时一次一张、看完即写结论不复读。

---

## 1. 目标（用户口径，不改）

- N2 官道土纹应与 N1 同方向平铺，而不是相对道路斜铺。
- 圈界前后路面应连续：不整屏换材质、不突然换纹理方向/相位、不重影游动。
- 左路村庄石路保持；右路始终土路。
- 剧情关现有表现不变；不做几何闭环、不加黑幕/整屏渐隐、不动相机与玩法。

---

## 2. 现场（暂停时）

| 项 | 状态 |
|---|---|
| 分支 / HEAD | `feat/stage-select-talent-overlays` / `465e8046` |
| 改动文件 | `Assets/Experiments/CurvedScroll/Shaders/YGroundJ1Branches.shader`（+60 −9，未提交） |
| 场景 | `Assets/Scenes/Battle.scene`、`YJunctionSample.unity` 均 `dirty=false`、时间戳未变 |
| 材质 | `J1_Ground_Branches.mat` 未改（时间戳仍 11-24 01:06） |
| 存档 | `player_save` SHA-256 全程 `ba53c213…a98ef`，未变；备份在 `Library/Locus/tmp/endless-ground-repair-20261124/player_save.backup.json` |
| Editor | Edit Mode；相机只有 `Main Camera`，灯光只有 `Directional Light`；Shader 编译 diagnostics = 0；Console 无 error |
| 运行时 | 本轮所有隔离实验（关渲染器、`timeScale=0`、临时改 `infiniteScroll`）都只存在于 Play 实例，退出后已消失 |

基线截图（可淘汰缓存，缺失就重拍）：
`Library/Locus/tmp/endless-ground-repair-20261124/baseline_*.png`、`candidate_*.png`。

---

## 3. 已实现的改动（仅 `YGroundJ1Branches.shader`）

1. **新增 `roadCoordinates(map, branchSign)`**：把平面点映射成「横向有符号距离 + 沿路米数」，
   三段（直段 / 圆弧 / 尾段）按最近候选选择。
   - 只用于**道路走廊**（`|lateral| ≤ _RoadHalfWidth+_ShoulderWidth` 再外扩 1.5m 过渡）。
   - 依据：弯道半径 28.16m 时，路缘 ±2.45m 的纵向尺度变化约 ±9%，路肩外缘 ±3.95m 约 ±14%，
     再往外会迅速变形甚至折叠 ⇒ **不能**推广到整张大地面（这也是历史上“沿路 UV”被撤回的原因）。
2. **走廊内改用走廊坐标采样** `_Road/_Shoulder/_Outer`，使纹理随道路方向转正；
   走廊外用旧地图坐标。两者按横向距离 `smoothstep` 混合。
3. **每圈相位** `phase = fmod(lap * fmod(cycleLength, 4), 4)`，让下一圈入口的相位接上前一圈终点
   （当前 `380 ÷ 4 = 95` 整除 ⇒ 实际恒为 0）。不使用无界的 `k*L`，避免高圈浮点精度下降。
4. **旧圈界双纹理混合**改为 `seamMix * (1 - corridorWeight)`：路心不再叠两套土纹，
   只在路外局部仍做圈界衔接。

---

## 4. 已验证（数值，非人眼）

测试方式：`Time.timeScale=0`、关掉全部 Canvas 与非地面材质渲染器、只留 `CurvedScroll/Y Ground J1 Branches`，
用现有 Battle 主相机 + `unity_capture_viewport` 定点截图，离线用 PIL 比对。

| 项 | 结果 |
|---|---|
| 剧情关右支 N2（266.1165m） | 改动前后 **整图 0 像素差**（bbox = None） |
| 剧情关 N1（0m） | 地面区域 **0 像素差**；整图仅右上 UI 小区域不同（非地面） |
| 剧情关左支村庄（325m） | 改动前后 **整图 0 像素差** |
| 无尽右支 N2 | 地面区域 mean abs diff ≈ **18.8**（确实改变，方向按预期由斜改平） |
| 接缝单份隔离（左支 355m） | 只留 c0 / 只留 c1 / 两份同时：按“土色特征掩码”统计，采样行带内 **neither = 0**（未发现整片无地面） |

**必须声明的限制：本轮截图都是图像附件，但会话通道未能把图像内容提供给我，**
因此**我并没有真正“看到”画面**。此前若有人据此认为“观感已确认由斜变平”，那是不成立的；
上述结论只到像素/掩码统计层面，**方向与观感必须由人眼在 Game 视图确认**。

---

## 5. 未解释的疑点（续跑第一件事）—— ✅ 已查明，见 §10.1

同一位置（左支 355m、同一相机）的两份地面合成图，底部行带出现约 **19%** 像素不满足土色掩码，
而“只留 c0”“只留 c1”两张图在同一行带内都是掩码全覆盖。

- 可能原因未定：两份共面时的深度竞争/近裁剪插值差、掩码对暗近景不敏感、或截图时刻状态差异。
- 与 §6.1 的投影高差（左接缝路心约 5cm、路肩约 10cm）可能是同一件事的不同表现。
- **不能用“单份都覆盖”推出“两份合成无缝”**。必须先判定这一点，再谈观感。

---

## 6. 已知未处理项

1. **相位污染外侧地面**：当前 `mapUV` 把 `phaseOffset` 也带给了走廊外的 `_Road/_Shoulder/_Outer`。
   圈长为 4 的整数倍（现行 380）时 `phase=0` 无影响；一旦改圈长就会让**路外纹理每圈平移**。
   ⇒ 应把相位限制在走廊与圈界局部，基线采样回到 `i.map/4`。
2. **投影后地面高差**：网格顶点 6m 间距 + 非线性下沉，相邻圈相对旋转 45° ⇒
   实测左接缝 `side=0` 高差 ≈0.053m、`side=±10` 达 0.10m 量级。是否形成可见缝/闪面未用 GPU 判定。
3. **每 8 圈同侧选择后圈帧几何回到原点附近**（RR…×8、LL…×8 实测距离 ≈6e-5m）。
   是否造成可见串景/回头未验证；不要在未复现前改路线拓扑。
4. **石板 UV 用 6m tile、未做相位对齐**；村庄石路在圈界前已由 `stoneExit` 淡出，理论上不跨接缝，
   但未单独验证跨圈混合支路时村庄是否始终为石路。
5. `YLapFollower` 副本只 `SetActive(false)` 不销毁（既有行为，每圈 +1 个 GameObject）。

---

## 7. 续跑顺序

1. **先判 §5**：同一位置分别拍 c0 / c1 / 合成，必要时用 Frame Debugger 看同一像素被画几次，
   区分「真有缝」与「掩码/状态噪声」。不要先调纹理。
2. 修 §6.1（相位只作用于走廊 + 圈界局部），复跑三处剧情关 0 像素差。
3. 人眼验收 N1 → J1 转弯 → N2 → 左支村庄 → 左右圈界（正常速度，连续行进），
   重点看：纹理方向、相位、重影、路肩拉伸。
4. 左右混圈（L→R、R→L）、同支跨 0→1、1→2、连续同侧多圈；查地面/布景/宝箱一致性。
5. 10 圈资源统计 + Shader 开销（走廊内多一次 `atan2` 分支）。
6. 全部通过后才谈提交，并把本文与 `endless-ground-continuity-repair-plan-20261123.md` §13 的旧结论对齐。

## 8. 不要做

- 不要 `git checkout / reset` 掉这份 Shader 改动（它是唯一已落盘的候选修复）。
- 不要把走廊坐标推广到整张地面（会拉伸/折叠）；不要用世界 XZ 或整图沿路 UV。
- 不要用黑幕、整屏渐隐、移动相机、取消村庄石板来遮问题。
- 不要把地面 `copyLapRadius` 改回 0（相邻两份各负责一侧，半径 1 是必要的）。
- 为截图不要新建相机/灯光、不要写 `RenderSettings`。
- **不要一上来连拍原图或回读大图**：按 `skill/session-image-payload-discipline.md` 的预算执行，
  否则会再次撞 128 MB 上限（见 §0.1）。

---

## 9. 新会话开场提示词（复制用）

> 请先读 `Locus/knowledge/plan/endless-ground-road-uv-handoff-20261124.md`（含 §0.1 会话中断原因）
> 与 `Locus/knowledge/skill/session-image-payload-discipline.md`，从**交接文档 §5** 接着做：
> 先判定左支 355m 处「只留 c0 / 只留 c1 / 两份合成」的覆盖差异是真缝还是测量噪声（用 Frame Debugger 数同一像素被画几次，
> 不要再用颜色掩码下结论），再按 §7 顺序修相位污染 → 人眼验收 → 左右混圈回归。
>
> 现场：工作区只有 `Assets/Experiments/CurvedScroll/Shaders/YGroundJ1Branches.shader` 一处未提交改动（候选修复，**不要回滚**）；
> Battle 与 YJunctionSample 均 `dirty=false`；`player_save` SHA-256 应为 `ba53c213…a98ef`。
>
> 取证纪律：默认 `unity_capture_viewport(max_long_edge=640)` 或写盘截图 + 脚本统计只回数字；
> 单会话累计图片原始字节 ≤30 MB；不要 `read` 2.5 MB 原图，不要在对话里贴贴图原文。

---

## 10. 续跑结果与结案（2026-11-24；用户已验收）

### 10.1 §5 真实原因：远处投影回折，不是近景共面双绘

- 本轮 **Unity 2022.3.62t7 / D3D11**，`Battle.scene` 的现有 Main Camera、左支 lap0 `d=355`、`Time.timeScale=0`。Frame Debugger 对原始 Shader 显示两次 Draw：事件 1 为 c0，事件 2 为 c1；两者 `_YSEnabled=1`、`_YSCycleLength=380`、各自 `_YSLapIndex=0/1`，原始深度状态为 `ZWrite On`、`LessEqual`。
- 临时**内存 Shader**保留原始顶点投影、裁剪和每副本 MPB，仅换成 `ZTest Always` / `ZWrite Off` / 加法计数（c0 红、c1 绿），不写项目资产。Frame Debugger 导出事件 2 的 RT，避免截图中后续 UI/文字污染。355 m 的 1080×2160 图，在像素行 768–2159 有 1,500,013 像素同时接收到两份片元；这不是土色掩码。
- 进一步用同样投影与深度状态输出插值坐标，证实 c1 远处下沉地面可回折投影到近景屏幕像素；`_YSGroundMaxZ=0` 时实为 `_YSViewDistance=380`，不能把“两次片元”直接解读成两块近处共面。实际 Shader 的 c0-only / c1-only / 合成四图对照：355 m 自 y=1200 起，合成 **1,036,800/1,036,800 像素等于 c0**，与背景相等 0。先前 19% 土色差不能证明露底，具体是暗近景/颜色分类还是其他噪声没有单独重建。
- **只用于诊断**将临时材质 `_YSGroundMaxZ=40`（未保存），排除回折：左支 `d=355,360,370,375,379.5,379.9,380,380.1,380.5,385`，在各自 RT 的 y=850–2159，**每点 1,414,800 像素均恰好一份，缺口/双份均 0**。按原始 `ZTest LessEqual` 单独输出最终归属，同位置 9 点与计数归属 0 差。
- LR 和 RL 混圈的第二次接缝（c1/c2，`d=735,745,755,759.5,759.9,760,760.1,760.5,765`）也各做同样的 40 m 诊断，所有点 y=850–2159 均缺口/双份 0。**这些仅是限制前方范围的投影定点，不能证明全视野/所有角度无缝，也没有修复原始材质的远端回折。**
- 原始材质、正常深度、正常 HUD/景物的不同单份和合成图只作补充；动态景物/后载段可能干扰截图，直接 RT 才是计数依据。Frame Debugger 的深度 RT 导出在 Unity 2022 超时，但事件及颜色 RT 导出可用；其 Skill 官方支持版本是 Unity 6.3+，因此还用原始 Shader A/B 核对，没有宣称官方支持此版本。

### 10.2 修复与有限关对照

- 仅在 `Assets/Experiments/CurvedScroll/Shaders/YGroundJ1Branches.shader` 追加改动：`mapUV` 恢复为 `i.map/4`；相位只保留在走廊采样及接缝局部的下一圈采样。已有 +60/−9 的走廊候选未回滚。
- 381 m（非 4 倍数）lap1、局部 100 m，临时内存 Shader 在同条件比较修复前/后：111,169 像素变化，主要在画面两侧；中央测试框 0 差。另以 GPU 输出的走廊权重为掩码，只改变相位而保持同一圈号：路外内缩区域 10,396 像素中 3 个通道量化误差，最大 1/255；走廊核心大幅变化。仅验证该测试点，不代表所有圈长与接缝视觉均通过。
- 现行 380 m、左支 `d=379.5` 同场景临时 Shader 的 old/fixed 对比：**整图 0 差**。剧情关右支 `266.1165` 和左村 `325` 修前/修后整图 0 差；N1 `0` 的 y≥1300 地面 0 差，右上有 13,263 个动态像素变化，不能称整图零差。
- 本轮小图只观察到正常 Game 画面。**观感已于同日由用户实机验收**：从真实无尽入口按正常速度看 N1 → J1 →
  左右支路 → 圈界，确认「已验收，似乎没有问题了」。设计口径已回写 `design/endless-mode-design.md` §2.4，
  结案记录见 `plan/endless-ground-continuity-repair-plan-20261123.md` §14。

### 10.3 左右流程和资源

- 真实 `StartEndlessRun()`，开发者跳过战斗、直接 `OnChoicePicked` 选出口，按已有旅行协程自然推进：**LR 和 RL 均到第 3 圈入口 d=760**；RL 第二圈到 N3 `d=705`，历史 `R,L`。这不是手动打怪/点击 UI 或对连续画面的人工验收。
- 两种定点测试的第二圈 J1 选择前后，已走帧 F0/F1、位置、角度的变化均为 0；副本帧与 lap MPB 对应，偏移为 0。
- 仅作调试跳距的 0–10 圈交替测试：地面副本活跃最多 3 个，第 10 圈字典留存 12 个（既有只关不销毁的增长）。没有涵盖道具、掉落和完整布景生命周期。
- PC Editor（D3D11，1080×2160，隔离地面、每组 120 帧）的**粗略 GPU 全帧计数**：无走廊版本均值 1.877 ms / p95 3.797 ms，当前候选 1.814 ms / p95 3.765 ms；只说明在该环境噪声内看不出增量，**不是 shader 单独耗时，也不能推断 Android 性能**。Profiler 原始结果位于 `Library/Locus/RunStates/`。

### 10.4 退出状态与下一步

- Edit Mode，`Assets/Scenes/Battle.scene` 已加载且 `dirty=false`，时间倍率恢复为 1，Frame Debugger 已关；只有 Main Camera / Directional Light。Shader supported、编译诊断 0、Console error 0。`player_save` SHA-256 始终 `ba53c213247658814f94f6305ae5ed758ec274b506357c7b67cc0f451f9a98ef`，没有调用会保存无尽最高分的 EndRun。
- 当前未提交内容不止 Shader：`memory/known-issues.md` 的会话限制补充，以及本交接和 `skill/session-image-payload-discipline.md` 是此前工作；都要保留。没有改场景/材质。
- **观感已验收**；仍可继续做的（不是本次范围）：未选分支副本重摆的肉眼对照、长圈串景、宝箱与高圈资源、真机性能。
  若后续又出现接缝观感问题，先复现并隔离原因（参考 `plan/endless-ground-continuity-repair-plan-20261123.md` §14.2），
  不要用定点数值直接否定或宣布通过。
