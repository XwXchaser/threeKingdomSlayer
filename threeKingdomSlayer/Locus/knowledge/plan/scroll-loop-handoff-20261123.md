---
id: kd_c82aa6e7-6967-4600-a8d4-629b63f1fa33
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 无尽卷轴 / 地面接缝 交接（2026-11-23）

> 覆盖 2026-11-20 ~ 11-23 这一轮的**全部改动**、**哪些验过 / 哪些没验**、**踩过的测量坑**、**已知未解决**、**提交建议**。
> 阅读顺序：§0 现状 → §1 目标口径（不读会推错方向）→ §2 已落地 → §3 未解决 → §4 测量工具（**本轮最大教训**）→ §5 提交建议。

---

## 0. 现状一句话

**内容/物件/远景/路面材质**四处跳变已修并有实测数字；
**地面贴图在换圈那一瞬仍会"重洗一次牌"**（图案变一下，不是拉伸、不是斜缝）；
我试过的三套修法（沿路 UV / 世界 XZ UV / 沿路裁剪+半径1）**全部撤回**——各自都带出更严重的问题。
当前地面 = **单份副本 + flat z 只封上界 + 地图空间 UV**，这是用户认可的基线。

---

## 1. 目标口径（必须先读）

- 循环 = **神殿逃亡式无限前进 + 素材复用**：玩家**永不回头**，**没有环路、没有合流点**。
- `design/endless-mode-design.md` §1 / §2.2 曾写"真闭环 / 环路图 / 合流点"——**已作废**（2026-11-21 用户澄清）。
  按旧口径推出的方案（改 `Evaluate` 成环路图）是错的，不要再做。
- 唯一残留接缝 = **内容从"地图末尾"切回"地图开头"那一瞬**；唯一要求是"看上去是连续的正常路面"。
- **剧情关（Finite + 手工）必须逐像素不变**。所有新逻辑都要用 `_YSCycleLength > 0` 之类的开关隔离。

---

## 2. 已落地（逐文件 / 逐条）

### 2.1 已验证（有实测数字）

| 改动 | 文件 | 验收数字 |
|---|---|---|
| **撑包围盒修视锥剔除** | `YLapFollower.cs`（新）+ `CreateCopy` 里再撑一次 | N1 装饰 `isVisible`：圈1 从 **0 → 78**，圈 0~4 全 78 |
| 地形副本被剔除 | 同上 | lap2 `isVisible` **0 → 1**，圈 0~3 全 1 |
| **宝箱每圈自转** | `RoutePropFollower.cs`（度→弧度 + 显式钉世界旋转） | `E0_Prop_Crate_01` 世界 rotY：`0/315/270` → **恒 0**；位置逐位一致 |
| **路侧物件跨接缝瞬移** | `YJunctionSample.unity` 段表 +2，删 2 个"搬家"实例 | 跨接缝前方物件数：`2 → 79`（一帧 Δ77）→ **Δ ≤ 2** |
| **N1 远城墙跨接缝瞬移** | `YJunctionSample.unity`（`Far Landmarks` 改按圈复制 radius 1） | 整帧接缝帧间差 **79.5 → 31**（普通帧 32） |
| **空路段** | 新增段 `EV to N3 - Roadside Approach [270,328]`、`N3 Village Exit - Seam Dressing [366,380]` | 前向扫描圈 0 / 圈 1 **0 空洞** |
| **N3 段声明范围与实际不符** | `YJunctionSample.unity` `[323,328] → [322,376]` | 相机不会再在 358m 提前卸载村庄 |
| **接缝处路面材质** | `YGroundJ1Branches.shader` + `J1_Ground_Branches.mat`（`_StoneExitStart=300 / _StoneExitFade=28`，flat z 口径） | 接缝两侧 R−B = **119 / 113**（土路），村庄段 41（青石） |
| **无限关开局脚下地面空缺** | 同上（裁剪只封上界 `clip(380 − map.y)`） | 开局底部中央 RGB (194,141,80) = 土路 |

### 2.2 撤回的尝试（**不要重做**，除非有新证据）

| 尝试 | 为什么撤 |
|---|---|
| 土路 UV 改**沿路坐标** `(along, dist)` | 它是"把路摊平"的映射：**离路心越远畸变越大** ⇒ 路肩/外侧明显拉伸发歪 |
| 土路 UV 改**世界 XZ** | 世界固定而路面每圈转 45° ⇒ **贴图相对路面每圈转 45°**（用户最早报的症状） |
| **沿路裁剪 + 半径 1** | 把 `map.y < 0` 那段（网格 −25 起）裁掉 ⇒ **开局脚下地面完全错误** |
| 环路图 / 合流点 | 与"无限前进"口径冲突（见 §1） |

---

## 3. 已知未解决（唯一一条）

**换圈那一瞬，地面贴图"重洗一次牌"**（图案变一下；不拉伸、不斜缝、不斜切）。

- 根因：每圈地面是**同一张地图旋转 45° 的副本**，贴图 UV 按各自地图坐标算 ⇒ 两份的图案相位差一个旋转。
- 我试的三条路都在 §2.2 里，各自带出更严重的问题。
- **建议做法**：**单开一个实验场景/开关**收敛，每次只改一项，用 §4 的可信探针验收；**不要直接在 `YJunctionSample` 正式场景里试**。

---

## 4. 测量工具的坑（本轮最大教训，务必读）

1. **`Texture2D.GetPixels32()` 返回的数组会被复用** ⇒ 拿它比较两帧会得到"完全相同"的**假结论**。
   必须显式 `Array.Copy` 深拷贝；**并且每次都跑一个自检用例**（两个明显不同的画面必须给出大差异）。
   本轮我因此误报过"无限关与剧情关逐像素一致（0.05 / 13 像素）"，实际是同一个缓冲区自己比自己。
2. **`Renderer.isVisible` 对被撑大包围盒的物件恒为 true** ⇒ 不能当"在屏幕上"用。
3. **`i.map.y` 是 flat z，不是路程**：分叉后每 1 米路程只推进 cos45° ≈ 0.707 米 flat z；
   **圈边界（路程 380）处 flat z 只有 328.6**。任何用 `i.map.y` 写的 band 都要按 flat z 算。
4. **沿路距离必须"三候选取最近点"**（直段 / 弧 / 尾段）。只用"相对弧心角度"判会把**尾段**当成弧上
   ⇒ 沿路距离卡在 ~210 不再增长（实测 d=250/300/380/600 全算出 ~208~211）。
   正确实现在 `YGroundJ1Branches.shader: AlongRoute()`：0~600m 误差 0.00，横向 ±100m 不变。
5. **整帧上做"每列最强竖向跳变"拟合斜线，量到的是物件/路肩**，不是地面 ⇒ 要测地面必须"**只留地面**"隔离。
6. 直接改 `progress` 采样时，段调度/窗口要几帧才收敛；**向后跳不会卸载**（`position - endDistance > behind` 才卸）
   ⇒ live 列表会被污染。验证要么连续向前走，要么重开 Play Mode。

---

## 5. 提交建议（工作区混着三条互不相关的线 + 1 处待确认）

### 5.1 建议分成 3 个提交

**① `feat(scroll): 无尽卷轴 —— 内容按距离调度 + 接缝修复`**
```
Assets/Experiments/CurvedScroll/**            （含新增 YLapFollower / YRouteFrame / YRouteSliceLoader）
Assets/Experiments/CurvedScroll/Authoring/Placement/EV to N3 - Roadside Approach.json
Assets/Experiments/CurvedScroll/Authoring/Placement/N3 Village Exit - Seam Dressing.json
Assets/Experiments/CurvedScroll/Authoring/Segments/EV to N3 - Roadside Approach.prefab
Assets/Experiments/CurvedScroll/Authoring/Segments/N3 Village Exit - Seam Dressing.prefab
Assets/RouteData/YSceneSpecs/EV to N3 - Roadside Approach.asset
Assets/RouteData/YSceneSpecs/N3 Village Exit - Seam Dressing.asset
Assets/Scripts/Data/IRouteMotionPresenter.cs
Assets/Scripts/Data/YRouteSlice.cs
Assets/Scripts/Managers/EndlessRunController.cs
Assets/Scripts/UI/EndlessHud.cs
```

**② `feat(build): 属性池 / 装备池两条来源线拆开`**
```
Assets/Scripts/Data/UpgradeStatIds.cs  IUpgradeStatSink.cs  UpgradeDefinition.cs  UpgradeRewardKind.cs
Assets/Scripts/Core/UpgradeEffectManager*.cs  UpgradePoolSampler.cs  DefaultUpgrade*.cs
Assets/ScriptableObjects/Upgrades/Definitions/*.asset        （13 改 + 6 新）
Assets/ScriptableObjects/ActiveSkills/ActiveSkillPoolConfig.asset
Assets/Scripts/Attack/*.cs  Assets/Scripts/Player/Combat/*.cs
Assets/Scripts/UI/BuffDisplayPanel.cs  PassiveSkillRow.cs  DevSkipBattleButton.cs
```

**③ `docs: 无尽卷轴口径修正 + 路线架构（长度×来源）+ 设计任务书`**
```
Locus/knowledge/design/endless-mode-design.md            （真闭环 → 无限前进）
Locus/knowledge/design/route-extent-and-content-source.md（新：两轴四格 + 不变量）
Locus/knowledge/design/in-run-build-design.md  weapon-system-design.md
Locus/knowledge/plan/weapon-endless-mode-plan-20261116.md
Locus/knowledge/plan/endless-route-loop-handoff-20261120.md（新）
Locus/knowledge/plan/build-progression-design-task-20261121.md（新）
Locus/knowledge/memory/git-partial-clone-blob-fetch.md
```

### 5.2 ⚠ 需要你确认后才能提交

1. **`Assets/Scenes/Battle.scene`（+7 −10）—— 不是我改的**。
   内容是**把无尽关难度系数全部抹平**：`startHpScale 0.55→1`、`startDamageScale 0.5→1`、
   `startAttackSpeedScale 0.8→1`、`hpGrowthPerCircle 1.6→1`、`bossHpGrowth 1.25→1`、
   `attackSpeedGrowth 1.06→1`、`damageGrowth 1.04→1`。
   ⇒ 若这是**调试值**，提交前应回滚；若是**新基准**，请说明后单独一个提交。
2. **`Assets/Resources/LoopTest/`（未跟踪）** —— 切片模型的 2 片测试资产，`groundPrefab` 为空、
   没有任何场景引用。它放在 `Resources/` 下只能用字符串路径加载，**违反项目"禁止幽灵引用"规范**。
   建议**不进版本库**（或移出 Resources 后再提交）。
3. **`Locus/knowledge/design/gameplay-progression-loop.md`（未跟踪）** —— 不是我创建的，请确认是否要提交。

---

## 6. 关键文件索引

| 文件 | 作用 |
|---|---|
| `Assets/Experiments/CurvedScroll/Scripts/YLapFollower.cs` | 按圈摆放/复制常驻物件（地面复制、纯锚点搬家）；撑包围盒防剔除 |
| `Assets/Experiments/CurvedScroll/Scripts/YRouteFrame.cs` | 圈刚体帧（位置+朝向），`GetLapFrame` 的基础 |
| `Assets/Experiments/CurvedScroll/Scripts/YScrollSample.cs` | 路径 `EvaluateLap`（尾段无限延伸）、`EvaluateRoute`、`GetLapFrame`、`TryGetNodeProgress` |
| `Assets/Experiments/CurvedScroll/Scripts/YRouteSegments.cs` | 段调度：按沿路区间加载/卸载 + 圈展开（`startDistance + k×一圈`） |
| `Assets/Experiments/CurvedScroll/Scripts/YRouteStreamingWindow.cs` | 可见性窗口（按沿路距离裁剪渲染器） |
| `Assets/Experiments/CurvedScroll/Shaders/YGroundJ1Branches.shader` | 地面：弯曲投影 + 每圈裁剪 + 石板/土路材质 |
| `design/endless-mode-design.md` §2.2 | **口径权威**：不做几何闭环 |
| `design/route-extent-and-content-source.md` | 两轴四格架构（有限/无限 × 随机/手工）、10 条不变量、P0~P5 分期 |
