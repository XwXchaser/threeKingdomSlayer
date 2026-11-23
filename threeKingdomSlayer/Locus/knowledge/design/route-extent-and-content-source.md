---
id: kd_e01ce694-bd41-4ee9-985f-930d0a47332e
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 路线架构：长度 × 内容来源（无限/有限 × 随机/非随机）

> **需求（2026-11-21 用户下达）**：新架构必须同时兼容
> **无限 / 有限** 两种关卡长度，以及 **随机 / 非随机** 两种内容来源 —— 四种组合都要能跑。
>
> 本文定义这套两轴模型、四格的现状与落点、以及不变式。**它取代**任何"只能做无限关"或"只能手工摆"的隐含假设。
> 前置口径（已确认，勿再翻）：循环 = **神殿逃亡式无限前进 + 素材复用，玩家永不回头**
> （见 `design/endless-mode-design.md` §2.2；**不做几何闭环**）。

---

## 0. 一句话

**长度**和**内容来源**是**两个正交的轴**，不是一件事。
今天两条轴都各做了一半，而且都被写死在"某一种组合"里；新架构就是把它们拆开，让四格共用同一套滚动与接缝标准。

> **四格全做（2026-11-21 用户定）**：**不做"只做无限 + 随机"这个简化**。
> **有限是默认，无限是它的扩展**（见 §3-6）—— 剧情关是**主路径**，不是"四格之一"。

| | **非随机（手工）** | **随机（程序）** |
|---|---|---|
| **有限** | ✅ **现状**：剧情关（progress 封顶、节点/段/波次全手工） | ⚠ **半格**：波次能生成，**节点序列与路线内容仍是手工** |
| **无限** | ✅ **现状**：无尽关（progress 不封顶、手工节点环绕 + 圈帧复用） | ⚠ **半格**：同上，只有波次能生成 |

> 结论：**"随机"这一列缺的不是数值，是"节点序列"和"路线内容"两层的生成入口。**

---

## 1. 现状：两条轴分别卡在哪（带证据）

### 1.1 长度轴（有限 / 无限）—— 已支持，但表达方式很隐含

| 载体 | 现状 |
|---|---|
| `YScrollSample.infiniteScroll` + `flatCycleLength` | 一个 bool + 一个米数决定一切：封不封顶、`Length` 取哪个、内容是否按圈展开 |
| `YScrollSample.Length` | 无限时 = `flatCycleLength`；有限时 = `GetRouteLength(turnAngle)`（≈325） |
| `progress` | 有限时夹在 1.0；无限时不封顶（一圈 = 1.0，第 k 圈 = k） |
| 内容按圈展开 | `YRouteSegments.Drive`：`startDistance + l × 一圈长度`；`YLapFollower` 按 `GetLapFrame(lap)` 摆放 |

⇒ **能跑，但"有限/无限"是从 bool 推出来的副作用，不是一个显式的类型。**
⇒ 判据散落在 6 个文件里（`YScrollSample` / `YRouteSegments` / `YLapFollower` / `YRouteStreamingWindow` / `ScrollRoutePresenter` / `EndlessRunController`），**加第三种长度模型会很痛**。

### 1.2 内容来源轴（随机 / 非随机）—— 只有"波次"这一层支持

| 层 | 非随机 | 随机 | 证据 |
|---|---|---|---|
| **波次（敌人编队）** | ✅ `BuildAuthoredWaves()` | ✅ `BuildGeneratedWaves()`（`EndlessWaveGenerator`） | `EndlessRunController.useAuthoredLevels` 一个开关切两路 |
| **节点序列（走哪些节点、什么顺序）** | ✅ 读 `sourceRoute.nodes` 的出口 + 硬编码回程边 | ❌ **没有入口** | `EndlessRunController.EnsureGraph()` 直接遍历资产并 `AddEdge` |
| **路线内容（铺哪些段 / 哪块地面）** | ✅ `YRouteSegments.segments`（声明区间 + 圈展开） | ❌ **没有入口** | 段表是 Inspector 静态列表 |
| **奖励池** | — | ✅ `UpgradePoolSampler` | 已随机 |

⇒ **"随机关卡"今天只能随机敌人，不能随机"路怎么走、路两边长什么样"。**

### 1.3 一个已经躺着的半成品：`YRouteSlice`

`Assets/Scripts/Data/YRouteSlice.cs` 定义了切片：`length` / `segmentPrefabs[]` / `node` / `nodeDistance` / **`groundPrefab`**；
`YRouteSliceSequence` 用 `slices[]` + `TotalLength` 把它们串起来。

现状：**只有 `Assets/Resources/LoopTest/` 里 2 片测试（N1 90m + E0 100m），两片 `groundPrefab` 都是空的，序列没被任何场景引用。**

⇒ 这套结构**正好是"随机内容"需要的单元**（决定"这一段路有多长、铺什么、地面是哪块、挂不挂节点"），
本架构把它**扶正为"生成侧"的基础单元**，而不是丢掉。

### 1.4 已踩到的失效样本："搬家型内容"跨接缝必瞬移

实测（2026-11-21，`Opening Roadside Props` 79 件）：

| 相机沿路距离 | 圈 | 前方 0~60m 已开启物件数 |
|---|---|---|
| 376.0 | 0 | 0 |
| 379.8 | 0 | 2 |
| 380.0 | 1 | 2 |
| **380.5** | 1 | **79** |

**一帧内 2 → 79** —— 玩家看到的就是"村庄瞬间切到 N1 黄土路 + N1 全套内容"。

**根因**：那 79 件只有**一个实例**，摆在 `GetLapFrame(当前圈)` 上。圈 0 时它在 6~72m（在身后 300 米），
圈 1 时才被整体搬到 386~452m（前方 6 米）—— **它在结构上不可能提前出现在前方**。

**对照**：段 prefab 没这个问题（按距离调度，圈 1 的副本在相机 376m 时已进窗口）。

⇒ 这条样本直接推出了 §3-6 / §3-7 与 §4 的"逐帧连续性"判据。

---

## 2. 目标抽象（四个部件，每个只做一件事）

```
                      ┌─────────────────────────────┐
                      │  RouteExtent（长度）         │  Finite | Infinite
                      │  只管：progress 封不封顶、   │
                      │       Length 取哪个、        │
                      │       内容按不按圈展开       │
                      └──────────────┬──────────────┘
                                     │
                      ┌──────────────▼──────────────┐
                      │  IRoutePlan（节点序列）      │  AuthoredRoutePlan
                      │  产出：一串 RouteStep        │  GeneratedRoutePlan
                      │  （节点 / 旅行秒数 / 圈号）  │
                      └──────────────┬──────────────┘
                                     │
                      ┌──────────────▼──────────────┐
                      │  IRouteContentSource（内容） │  SegmentTableSource（现状）
                      │  产出：某区间该铺哪些段/地面 │  SliceSequenceSource（生成）
                      └─────────────────────────────┘
```

### 2.1 `RouteExtent` —— 把 bool 变成类型

| 值 | progress | `Length` | 内容 | 用在哪 |
|---|---|---|---|---|
| `Finite` | 夹在 1.0 | 路径形状长度 | 只铺一次 | 剧情关 |
| `Infinite` | 不封顶 | 一圈长度 | 按圈展开（`startDistance + k×一圈`） | 无尽关 |

- **只做这一件事**：`Infinite` ⇔ 今天 `infiniteScroll = true` 的全套行为。`Finite` ⇔ 为 false 的全套行为。
- 落点：`YScrollSample` 增加 `RouteExtent extent`（`infiniteScroll` 保留为它的 `on/off` 视图，先不改序列化字段，避免场景迁移）。
- ⚠ **`Finite` 语义必须与今天逐像素一致**（剧情关红线）。

### 2.2 `IRoutePlan` —— 节点序列从哪来

```csharp
public struct RouteStep { public FakeRouteNodeConfig node; public float travelSeconds; public int lap; }
public interface IRoutePlan { IReadOnlyList<RouteStep> BuildForLap(int lap); }
```

| 实现 | 行为 | 状态 |
|---|---|---|
| `AuthoredRoutePlan` | 读 `FakeRouteStageConfig.nodes` + 出口 + 回程边，**照抄今天 `EnsureGraph()` 的语义** | 需抽出（今天是写死在 `EndlessRunController` 里） |
| `GeneratedRoutePlan` | 按规则从节点池挑顺序：圈 1 强制教学序（N1→E0→J1），圈 2+ 按权重抽支路与插入节点；同一 seed 同一局 | **新建** |

- 落点：`EndlessRunController.EnsureGraph()` 降级为 `AuthoredRoutePlan` 的实现细节，控制器只消费 `IRoutePlan`。
- ⚠ **有限关也走这个接口**（剧情关用 `AuthoredRoutePlan`，只产一圈）。

**终点用参数表达，不写第二个生成器**（否则"有限+随机"会分叉成两套代码）：

| `termination` | 行为 | 用于 |
|---|---|---|
| `Never` | 成环，永不结束 | 无限 + 随机 |
| `AtStepCount(n)` | 走 n 步结束 | 有限 + 随机（按节点数；剧情关就是这个语义） |
| `AtLength(m)` | 累计路线长度到 m 结束 | 有限 + 随机（按长度） |

### 2.3 `IRouteContentSource` —— 路两边长什么样

```csharp
public interface IRouteContentSource { float CycleLength { get; } void Apply(YScrollSample route, YRouteSegments segments); }
```

| 实现 | 行为 | 状态 |
|---|---|---|
| `SegmentTableSource` | 今天这套：静态段表 + 声明区间 + 圈展开 | ✅ 已有，原样保留 |
| `SliceSequenceSource` | 把 `YRouteSlice` 序列**按序平铺**：第 k 片从 `Σ前序 length` 起，自带 `groundPrefab`；无限时整序列成环，有限时截断 | **新建（扶正 `YRouteSlice`）** |

**为什么生成侧要用切片而不是沿用"整图 + 圈帧"**：
每片自带 `groundPrefab` ⇒ **接缝由切片边界决定**，可以做到"每片的地面首尾材质一致" ⇒ 生成出来的路**天然无跳变**，不需要为生成内容去改 shader 或补过渡片。

### 2.4 四格怎么落

| 组合 | 长度 | 节点序列 | 路线内容 | 备注 |
|---|---|---|---|---|
| **有限 + 非随机** | `Finite` | `AuthoredRoutePlan` | `SegmentTableSource` | = **剧情关，必须零变化** |
| **有限 + 随机** | `Finite` | `GeneratedRoutePlan`（限 N 步） | `SliceSequenceSource`（限总长） | 新玩法：随机小关 |
| **无限 + 非随机** | `Infinite` | `AuthoredRoutePlan` | `SegmentTableSource` | = **今天的无尽关** |
| **无限 + 随机** | `Infinite` | `GeneratedRoutePlan`（不封顶） | `SliceSequenceSource`（成环） | 目标形态：随机无尽 |

---

## 3. 不变式（四格共用，不许分叉）

1. **不回头**：路径是无限/有限延伸的开路径，位置是沿路距离的连续函数；**不取模、不平移、不闭合、不回主干**。
2. **接缝标准**：任何"内容切回开头"的位置，都要满足"看上去是连续的正常路面"（内容密度 + 材质；见 §4 验收）。
3. **剧情关零变化**：`Finite + AuthoredRoutePlan + SegmentTableSource` 这条路径必须逐像素不变。
4. **相机空间投影不变**：滚动仍是"视点固定、世界迎面来"；不为任何一格改投影模型。
5. **存档**：局内进度不存；只存最佳分 / 最高圈。
6. ★ **有限是默认，无限是扩展**（2026-11-21 新增）。
   `Finite`：progress 封顶、内容只铺一次、`Length` = 路径形状长度（剧情关 ≈325）。
   `Infinite`：progress 解封顶、内容按一圈长度复用、`Length` = 一圈长度（当前 380）。
   ⇒ "剧情关不变"是**默认行为**，不是靠补丁守的红线。
   （`RouteExtent` 仍必须是**显式输入** —— 两者的 `Length` 语义本来就不同，不能从"计划有没有结束"推出来。）
7. ★ **内容一律按距离调度**（2026-11-21 新增）。可见内容必须是"按沿路距离实例化、能提前出现在相机前方、能被距离窗口裁掉"的单元。
   **禁止"按圈搬家"**（单实例跟随当前圈）——它在结构上**不可能**在接缝前出现在前方，必然瞬移（见 §1.4）。
   推论：`YLapFollower` 的搬家模式只允许用于**无渲染的纯锚点**；凡是有渲染的内容，一律走 `IRouteContentSource`。
8. ★ **地面副本的边界必须在"材质上首尾一致"**（2026-11-21 新增，踩过）。
   地面是按圈复制的、每份只画 `[0, 一圈长度]`（flat z），两份的边界在世界空间里是**同一个点**（实测相差 0.00000 m）。
   但材质（石板入场 / 石板收尾 / UV）是按**每份副本自己的 flat z** 算的
   ⇒ **任何材质 band 都不能跨副本边界**，否则边界两边各算一套，会出现一条**横切路面的硬边**。
   实测教训：把区间改成 `[-14, 380-14]` 后，边界处一边读 `map.y=366`（石板 0.50）、一边读 `map.y=-14`（土路）⇒ 路面硬跳。
   当前正确取值：`clip(min(i.map.y, _YSCycleLength - i.map.y))`；镜头身后由**上一圈的副本**负责（`copyLapRadius = 1`）。
9. ★ **`i.map.y` 是 flat z，不是路程**（2026-11-21 新增，同一个坑踩了三次）。
   分叉之后两者就不再相等：**尾段每前进 1 米路程，flat z 只走 cos45° ≈ 0.707 米**。
   换算（左支）：`flat.z ≈ 209.9 + 0.707 × (路程 − 212.1)`；反推：`路程 ≈ 212.1 + (flat.z − 209.9) / 0.707`。
   ⇒ 圈边界（路程 380）处的 flat z 只有 **328.6**。
   ⚠ 任何用 `i.map.y` 写的材质/裁剪 band，都必须用 **flat z** 算位置，不能用路程直接填。
   实测教训：石板收尾写成 `358→374`（当时以为是路程）⇒ 圈边界时 flat z 才 328.6，band 根本还没开始
   ⇒ 接缝处仍是村庄青石路。改成 `300→328` 后，路程 375 之后已是土路，边界两侧一致。

---

## 4. 验收判据（每格都要过）

| 判据 | 怎么量 |
|---|---|
| 相机路径连续 | 跨接缝前后 `travelPos` 位移 ≈ 里程差、朝向不变 |
| **前向扫描 0 空洞** | 每 10m 采样，"前方 40m 内已开启 Renderer == 0"的采样点必须为 0 |
| 内容集合可复现 | 同 seed、同位置 ⇒ 已开启物件集合完全一致 |
| **逐帧连续性（新增）** | 跨接缝时，"前方 0~60m 已开启物件数"的**相邻帧变化 ≤ 5**；且任何物件不得在 1 帧内让观察距离跳变 ≥50m。⚠ 空间扫描放过了 §1.4 的 2→79，必须用时间判据 |
| **泄漏（新增）** | 跑 **10 圈**后，窗口登记数 / 活跃实例数回到首圈量级（±10）。朴素"按圈复制"每圈会多留 109 个登记 |
| 物件朝向稳定 | 每圈同物件世界旋转一致 |
| 剧情关逐像素不变 | 同 progress 下新旧实现 0 像素差 |
| 生成侧可复现 | 同 seed 两局 ⇒ 节点序列 + 段序列 + 波次完全一致 |

> ⚠️ **不要用 `Renderer.isVisible` 当"在屏幕上"**：被撑过包围盒的渲染器 `isVisible` **恒为 true**（§1.4 与 N1 装饰修复都踩过）。
> 要看"是否真的在画面里"，用逐帧连续性判据或实际截图。

---

## 5. 分期（每期都能单独验收，不推倒现状）

| 期 | 内容 | 验收 |
|---|---|---|
| **P0** | 接缝质感：内容补满一圈 + 石板在接缝前收回土路 | 跨接缝逐像素连续；剧情关 0 像素差 |
| **P1** | `RouteExtent` 显式化（`Finite` 为默认），`infiniteScroll` 变成它的视图 | 剧情关 & 无尽关表现均与今天逐像素一致 |
| **P2** | `IRoutePlan` 抽出，`EndlessRunController` 改为消费它；`AuthoredRoutePlan` + `termination` | 无尽关表现与今天一致（同一局同流程）；剧情关 `AtStepCount` 等价 |
| **P3** | `SliceSequenceSource` + 把 `YRouteSlice` 扶正（**每片配 `groundPrefab`**）；把"搬家型内容"迁到调度侧（见 §6.1） | 用切片拼出与今天段表等价的路线；前向扫描 0 空洞；**逐帧连续性**；跑 10 圈无泄漏 |
| **P4** | `GeneratedRoutePlan`（`termination = Never`）+ `SliceSequenceSource` 成环 ⇒ 无限 + 随机 | 同 seed 可复现；四格取三格通过 §4 |
| **P5** | `termination = AtStepCount / AtLength` ⇒ **有限 + 随机** | 复用 P4，只换终止条件；结束时机与剧情关同语义 |

> P3 先用"手工切片序列复现今天的路线"做等价性验证，**再**接生成器 —— 否则生成器出问题时分不清是生成规则错还是切片装配错。

---

## 6. 风险 / 待拍板

### 6.1 必须在 P3 迁走的"搬家型内容"（**否则跨接缝必瞬移**，见 §1.4）

| 组 | 渲染器数 | 现状 | 迁到哪 |
|---|---|---|---|
| `Opening Roadside Props` | 79 | 搬家型（单实例跟圈） | 段表或切片（prefab 已存在于 `Authoring/Segments/`） |
| `N1 to E0 - Shoulder Detail v1` | 30 | 同上 | 同上 |

> 迁完后 `YLapFollower` 只服务两类：**地面（复制）** 与 **无渲染的纯锚点（搬家）**。

### 6.2 待拍板

| # | 项 | 现状 |
|---|---|---|
| 1 | ~~有限 + 随机是否真要做~~ | **已定：做**（四格全做） |
| 2 | `GeneratedRoutePlan` 的"可读性规则"（不能连续两个 Boss、支路必须成对出现…） | 未设计 |
| 3 | 切片的 `groundPrefab` 从哪来：把现有整图按片切，还是每片单独做地面 | 未定（影响美术工作量） |
| 4 | 生成侧是否需要"圈主题"（每圈换一套切片池） | 未定（`design/in-run-build-design.md` §3 提到过） |
| 5 | 左右支路的选择在有限关里怎么表达（剧情关只有左路资产接线齐全） | 见 `endless-mode-design.md` §2.1 |
