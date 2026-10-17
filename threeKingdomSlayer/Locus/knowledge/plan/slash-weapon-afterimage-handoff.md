---
id: kd_09566067-ea9f-4219-95a5-bd3c95c56883
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 普通斩击武器残影（Afterimage）交接

## 0. 现场核对（本会话只读实测，2026-10-16）

按交接摘要逐条核对当前工作区，结论与摘要基本一致，另有两条需要更正：

- **方向收口确认**：`Assets/Scripts/Player/AttackSystem.cs::ExecuteSlash`（约 487 行）未传 `enableMangaSweep`；`Assets/Prefabs/FantasySpear3D.prefab` 上 `MeshWeaponMangaSweep` 为 `m_Enabled: 0`；`Assets/Scripts/Attack/SweepEffect.cs` 的 `mangaSweep` 字段与 `Begin / SetPaused / EndEmission / Clear` 接线完整保留。原型资产均在。
- **`MeshWeaponTrail` 现状与摘要一致**：`anchors [1] = WeaponTip`、`headColor #FFF2D9B2`（a=0.70）、`headWidthOfLength 0.045`、`tailWidthRatio 0.08`、`trailSeconds 0.08`、`sortingOrder 3`。
- **修正 1（第 3.1 节已过时）**：prefab 上 `MeshWeaponVisual.fadeMaterial` 现指向 `Assets/Models/Weapons/FantasySpear/StyleCandidates/UnlitV01/FantasySpear_UnlitV02_Fade.mat`，**不是**根目录的 `Assets/Models/Weapons/FantasySpear/FantasySpear_Fade.mat`。方案 A 若要复用「与武器同一份淡出材质」，应跟随 prefab 现值。
- **修正 2（第 3.1 节风险项已排除）**：蒙皮网格 2521 顶点 / 13 骨骼（rootBone `WeaponRoot`），`FantasySpear_Rigged.fbx` 导入为 `isReadable: 0`；但本机 Edit Mode 实测 `SkinnedMeshRenderer.BakeMesh` 对非 Read/Write 网格**不报错且返回有效顶点**（zeroCount=0，无 Console error）。即**不需要**为了方案 A 修改 FBX 导入设置。仍需在 Play Mode 动画中复验一次。
- **骨骼结构（方案 B 选第二 anchor 的依据）**：`WeaponTip` 是弯曲链 `Shaft_00 → Shaft_01 → Shaft_02` 的叶节点（随枪杆形变摆动）；`WeaponTail` 是 `WeaponRoot` 的刚性直接子节点，弯曲链不含它。**若加第二 anchor，`WeaponTail` 是唯一符合「刚性采样点」要求的候选**（`MeshWeaponTrail` 明确禁止用 ribbon 骨）。
- **工作区确认**：`Assets/Prefabs/FantasySpear3D.prefab` 处于 dirty（+341/−24），来自更早的 3D 长枪 / 路线工作，不是本任务产物；不要整份提交或回退。

> ## 交接摘要（新对话入口 — 2026-10-16）
>
> **用户已否决「整杆漫画扫掠」方向，本任务改为细微残影。** 上一方向的完整过程、几何/着色/取证记录见 `plan/stab-manga-sweep-runtime-implementation-plan.md`，只作历史参考，**不要在其基础上继续调参**。
>
> **本次已完成的方向收口**
> - `Assets/Scripts/Player/AttackSystem.cs::ExecuteSlash` 不再传 `enableMangaSweep: true`，正式斩击恢复原枪尖拖尾表现。
> - 原型资产全部保留、默认关闭，未删除：`Assets/Scripts/Effects/MeshWeaponMangaSweep.cs`、`Assets/Shaders/WeaponMangaSweep.shader`、`Assets/Models/Weapons/FantasySpear/Effects/FantasySpear_MangaSweep.mat`、`Assets/Models/Weapons/FantasySpear/Effects/Textures/FantasySpear_MangaSweepMask.png`。
> - `Assets/Prefabs/FantasySpear3D.prefab` 上的 `MeshWeaponMangaSweep` 组件保持 `m_Enabled: 0`，存在不影响任何招式。
> - `Assets/Scripts/Attack/SweepEffect.cs` 的 `enableMangaSweep` 选择参数与 `Begin / SetPaused / EndEmission / Clear` 接线保留（默认 false），可直接复用为残影开关。
>
> **现场状态**：Edit Mode、编译通过、Console 0 error。工作区有大量未提交改动，其中 `Assets/Scenes/Battle.scene`、`Assets/Scenes/MainMenu.scene`、`Assets/Scripts/Player/AttackSystem.cs` 的多数改动来自本分支更早的路线 / 3D 长枪工作，**不是本任务产物**，不要整份提交或整份回退。
>
> **下一步**：方案 A（稀疏采样残影）已落地，代码与接线见第 8 节；待用户进游戏判定观感，再按第 8.5 节的旋钮微调。
>
> **边界（继续保持）**：不改伤害、推退、命中判定、QTE、枪杆弯曲方向/幅度、红缨；不推广到 C4 / 幻影 / Stab / Launch / Parry；不提交或推送 Git。

## 1. 目标与验收口径

目标：普通左右斩击时，武器留下**细微**的残影，读起来是「枪的鬼影」或「很淡的一层尾」，而不是发光线条。

用户判断标准（必须先满足，再谈数值）：

- 只有 1 层清晰可读的残影，不出现多道平行抓痕。
- 残影跟随武器朝向，不出现「朝摄像机一侧的收束」。
- 幅度小：是增益，不是主体特效。宁可弱，不要抢武器本体。

## 2. 上一方向为什么被否决（避免重蹈）

三轮落地后，整杆扫掠在实机里始终读作「三道独立抓痕 / 一块丝绸」，与参考图的一道连续弧不符。已证伪的具体做法，不要重复：

1. **整根杆 × 全部历史姿态的曲面**：枪尾近似不动、枪尖扫出大弧，曲面必然从尾部的窄缝张成一片布，读作丝绸。
2. **用固定 screen-right 当宽度轴**：斜视角下整条弧被压成前后错开的细线，看起来像多道平行线。
3. **用 `camera.forward × 切线` 当宽度轴**：宽度被吃进相机深度方向，弧面在屏幕上丢失面积。
4. **笔触遮罩作用到弧面主体**：会把一条弧切出平行缺口，直接读作「爪痕」。
5. **把旧切片沿杆长或 UV 向枪尾收束**：会产生朝镜头方向的诡异收束。

结论：屏幕上「一道连续手绘弧」本质是 2D 笔触问题，用 3D 曲面近似代价过高、三轮未收敛。**不要再回到这五条路线。**

## 3. 实现选项

### 3.1 方案 A（推荐）：稀疏采样残影

原理：挥击过程中每隔一段时间把当前武器姿态「拍照」一次，生成一个半透明副本，副本停在原地随生命期淡出。观感是「枪的鬼影」，不是线条。

- **组件**：新增 `Assets/Scripts/Effects/MeshWeaponAfterimage.cs`，挂在 `Assets/Prefabs/FantasySpear3D.prefab` 根节点，默认 `m_Enabled: 0`，由斩击入口 `Begin` 启用。
- **依赖引用**（全部 Inspector 可追踪，禁止 `Resources.Load` / 字符串 / 静态缓存）：
  - `MeshWeaponVisual weaponVisual`：取它的 `renderers`（当前 prefab 上该数组只有 1 个蒙皮 Renderer）以及世界姿态；`tipAnchor / tailAnchor` 也可用。
  - `Material afterimageMaterial`：复用已存在的 `Assets/Models/Weapons/FantasySpear/FantasySpear_Fade.mat`（prefab 上 `MeshWeaponVisual.fadeMaterial` 已引用同一份）。
  - `Transform effectRoot`：由 `SweepEffect` 传入本次斩击根（与 `MeshWeaponMangaSweep` 相同做法）。副本挂在这里才不会继续跟随武器移动。
- **参数**（Inspector 可调）：`sampleInterval` 0.05–0.08s、`lifetime` 0.10–0.18s、`startAlpha` 0.25–0.40、`maxCopies` 2–3、`fadeCurve`。
- **采样时机**：`[DefaultExecutionOrder(1100)]` 的 LateUpdate，排在杆骨形变与红缨之后，取到的才是屏幕上真正显示的姿态。
- **每次采样**：对每个 source Renderer 调 `BakeMesh(pooledMesh)`（写入预建 Mesh，无每帧 GC），把副本的 world 矩阵固定为采样瞬间的值，alpha 用 `MaterialPropertyBlock` 写 `_Color`（`MeshWeaponVisual` 用的同一属性），随后按 `lifetime` 线性淡出并回池。
- **暂停契约**：局部卡肉、全局 `timeScale = 0`、编辑器暂停时，只冻结计时，不新增采样、不推进淡出，恢复后不跨停顿连线。与 `MeshWeaponMangaSweep` 的契约保持一致。
- **清理**：池内对象数 ≤ `maxCopies`；`Clear()` 幂等；结束 / 提前 Kill / 中断 / OnDestroy 后不留 Mesh 与 GameObject。
- **风险**：`BakeMesh` 是 CPU 蒙皮求值，成本随采样次数增长。**把 `sampleInterval` 调稀，不要每帧采样**；单次斩击预计 6–10 次采样 × 1 个 Renderer。落地时确认 `BakeMesh` 是否需要模型开启 Read/Write；**若需要，不要为此把 FBX 设为 Read/Write**（会增常驻内存），改用方案 B。

### 3.2 方案 B（低成本备选，建议先给用户看这一档）

只改 `Assets/Prefabs/FantasySpear3D.prefab` 上已存在的 `MeshWeaponTrail`（当前 1 个 anchor = 枪尖、`headColor.a` 0.7、`headWidthOfLength` 0.045、`trailSeconds` 0.08、`sortingOrder` 3）：

- 给 `anchors` 增加 1 个刚性点（如枪尾或 `Shaft_01`），并把 `headColor.a`、`headWidthOfLength`、`trailSeconds` 压得更轻。
- 优点：零新代码，组件已在 prefab 上验证可用。
- 风险：每个 anchor 是一条独立 ribbon，加多了就是「多道线」，会重演本轮被否决的观感。**最多 2 个 anchor，且必须非常淡。**

### 3.3 不建议

- 继续由整杆 mesh 拼「扫掠面」（第 2 节五条已证伪）。
- 逐帧 BakeMesh 做连续残影：成本与风险都高于按间隔精确采样。

## 4. 生命周期与接线

- `SweepEffect.Create` 已有选择参数 `enableMangaSweep`（默认 false，只有 `AttackSystem.ExecuteSlash` 传值），并已有 `mangaSweep` 字段与 `Begin / SetPaused / EndEmission / Clear` 调用点。建议按同一模式接残影组件：**只有普通斩击 opt-in，C4 / 幻影 / Stab / Launch / Parry 默认关闭**。
- A/B 开关沿用 `replaceTipTrail` 的思路，保留「残影替换枪尖拖尾」与「两者并存」两种模式，便于对照。
- 新组件默认 disabled，`Awake` 不分配 Mesh / 缓冲；参照 `MeshWeaponMangaSweep.Begin / Clear / SetPaused` 的写法。
- 命名若改用 `enableWeaponAfterimage` 等新参数名，属于新增序列化字段，不影响既有 prefab 数据；但**不要删除**既有 `enableMangaSweep` 字段，避免动到 prefab 序列化。

## 5. 验证流程与两个必须避开的坑

### 5.1 两个坑（本次已踩过）

1. **不要用 `MainMenuUI.OnNewGame()` 做验证。** 它内部执行 `SaveManager.Delete()` → `PlayerPrefs.DeleteKey(SaveKey)`，会清掉本机存档（铜钱、解锁、路线快照）。本次任务中已因此被清过一次。非破坏性进入战斗改用：

```csharp
StageController.PendingStageConfig = null;
RouteStageV2Launch.PendingConfig = null;
FakeRouteLaunch.StartFromCheckpoint = false;
FakeRouteLaunch.PendingConfig = StageConfigManager.Instance.fakeRouteStages[0];
UnityEngine.SceneManagement.SceneManager.LoadScene("Battle");
```

（这正是 `OnNewGame` 的配置部分，只是去掉了 `SaveManager.Delete()`。）

2. **不要在 Play 中 `OpenScene`，也不要用 `timeScale = 0` 冻结画面截图。** 进 Play 后 `OpenScene` 会抛异常；`timeScale = 0` 会触发本效果的暂停契约，几何被冻结或清空。要定格就用很小的 `timeScale`（如 0.0001），或直接用组件自身的暂停信号。

### 5.2 可直接复现的截图流程

1. Edit Mode 用 `OpenSceneMode.Single` 打开 `Assets/Scenes/MainMenu.scene`，确保没有残留 Additive 场景。
2. 进入 Play。
3. 用 5.1 的片段进入 Battle。
4. 等到：活动场景是 `Assets/Scenes/Battle.scene`、**只有一台启用且无 RenderTarget 的相机**、场景里存在 `Enemy`。
5. 触发一次普通斩击：真实输入，或反射调用 `AttackSystem.ExecuteSlash(bool leftToRight, float visualTilt)`。
6. 目标进入视口（`WorldToViewportPoint().z > 0` 且 x/y 在 0.1–0.9 之间）后再截图；用 `unity_capture_viewport(target="game")`，**以返回的图片内容为准**，不能只看「文件已生成」。
7. 退出 Play，**不保存场景**。

### 5.3 验收检查项

- 左右斩击各一次：只有 1 层清晰残影，无多道平行抓痕，无朝镜头的收束。
- 命中卡肉不新增残影；恢复后不残留跨帧连线。
- 斩击结束、提前 Kill、OnDestroy 后无 Mesh / GameObject 残留。
- C4、幻影、Stab、Launch、Parry 仍不产生残影。
- 原伤害、推退、目标顺序、动作时长不变。

## 6. 回退

- 第一步：在 `AttackSystem.ExecuteSlash` 摘掉残影选择参数（与本次摘掉 `enableMangaSweep: true` 完全相同的做法）。
- 残影组件默认 `m_Enabled: 0`，保留在 prefab 上不影响其他招式。
- 只通过 Unity API 增删组件与引用，不整份覆盖 prefab / 场景，保留既有 GUID 与并行工作。

## 7. 待决项

- **先做方案 B 还是方案 A**：用户已选**方案 A**（2026-10-16），已落地，见第 8 节。
- 移动端预算：单次斩击可接受的 BakeMesh 次数与峰值副本数尚未实测。
- 残影材质是否沿用 `FantasySpear_Fade.mat`，还是需要一份更暗/更偏蓝的专用材质。
- 是否复用 `enableMangaSweep` 参数名（复用最省事，改名需要动 prefab 序列化字段）。

## 8. 方案 A 落地记录（2026-10-16，已实现待验收）

### 8.1 改动清单

| 文件 | 改动 |
| --- | --- |
| `Assets/Scripts/Effects/MeshWeaponAfterimage.cs` | **新增**：间隔采样残影组件。`[DefaultExecutionOrder(1100)]`、`DisallowMultipleComponent`；`Begin / SetPaused / EndEmission / Clear` + `IsRunning / ReplaceTipTrail / LiveCopyCount`。 |
| `Assets/Scripts/Effects/MeshWeaponVisual.cs` | 只新增 `public Renderer[] Renderers => renderers;`（不新增序列化字段）。 |
| `Assets/Scripts/Attack/SweepEffect.cs` | `Create` 新增 `bool enableWeaponAfterimage = false`；新增 `afterimage` 字段与 `Begin / SetPaused / EndEmission / Clear` 调用点（与 `mangaSweep` 完全同构）。 |
| `Assets/Scripts/Player/AttackSystem.cs` | 只有 `ExecuteSlash`（约 505 行）传 `enableWeaponAfterimage: true`；C4 / 幻影 / Stab / Launch / Parry 调用点不动。 |
| `Assets/Prefabs/FantasySpear3D.prefab` | 根节点新增 `MeshWeaponAfterimage`，`m_Enabled: 0`，`afterimageMaterial` = `Assets/Models/Weapons/FantasySpear/StyleCandidates/UnlitV01/FantasySpear_UnlitV02_Fade.mat`。 |

### 8.2 实现要点（与 §3.1 的偏差与原因）

- **烘焙空间（实测）**：`SkinnedMeshRenderer.BakeMesh(mesh, true)` 返回**渲染器本地空间**几何（尺寸 = 世界长度 / 该渲染器 lossyScale≈1105）；`BakeMesh(mesh, false)` 才是被预乘缩放的一份。实现用 `true` + 把副本世界矩阵设为**源渲染器自身的 position/rotation/lossyScale**，与实时蒙皮结果 1:1。
- **副本落点**：副本池 `WeaponAfterimageGhosts` 建在**场景根**（不是 `effectRoot`），每帧写冻结的世界位姿。原因：`SweepEffect` 的 `effectRoot`（`motionRoot` / 武器实例）整个挥击期间都在被 DOTween 移动，挂上去反而会把残影带着跑；挂在场景根后无需每帧重投影，且 `Clear` 会连同池根一起销毁。
- **深度与排序**：材质沿用武器自己的 fade 材质（内含 `VisibleSurfaceDepth` 预通道）；副本 `sortingOrder=1`，低于武器在斩击时的 `SetSorting(2)`，因此武器总是压在残影之上。
- **卡顿保护只用游戏时间**：`maxSampleGap` 只比 `Time.deltaTime`。实测在编辑器中截图会稳定造成 ~0.15s **真实**停顿，若同时用真实时间判据会把整轮采样全部吃掉（早期版本即如此）。残影是独立快照、不是连续带，故真实时间判据无必要。
- **暂停契约**：`_locallyPaused` 或 `deltaTime <= 0` 时既不采样也不推进淡出；恢复时只刷新枪尖基准、重置采样计时，不跨停顿算速度尖峰。

### 8.3 默认参数（prefab 上的现值）

`sampleInterval 0.06` / `lifetime 0.14` / `fadeCurve` 线性 1→0 / `startAlpha 0.32` / `maxCopies 3` / `maxSampleGap 0.1` / `minTipSpeed 8` / `fullTipSpeed 45` / `minSpeedIntensity 0.5` / `sortingLayer Default` / `sortingOrder 1` / `ghostTint` 白 / `replaceTipTrail true`。

`minTipSpeed` 用于避免「枪几乎不动时生成一个与原枪几乎重合的副本」，即视觉上的双枪；`fullTipSpeed` 用于让残影在起步/收尾处自然变淡而不突现。

### 8.4 已核实的证据（Play Mode，Battle，Console 0 error / 0 warn）

- 斩击（L→R 与 R→L 各一次）：挥击中段峰值同时 **2–3 个**存活副本，alpha 呈清晰递减（实测 0.320 / 0.167 / 0.006），`isVisible=True`、`sortingOrder=1`、`layer=0`；副本位置沿挥击轨迹散开（例：x 1.32 / −0.91）。
- 不带 opt-in 的 `SweepEffect.Create`（即 Stab / 幻影 / Launch / Parry / C4 同类调用）：全程无副本、无池根。
- 卡肉：`_locallyPaused=True` 期间不采样、不推进淡出。
- 结束回收：池根、副本对象、池化 Mesh 全部为 0，无残留；SweepEffect 自身也已销毁。
- `BakeMesh` 在 `isReadable: 0` 的蒙皮网格上可用，**不需要改 FBX 导入设置**。

### 8.5 仍需人眼判断（本会话无法目视图片）

本会话的图片只能作为附件返回、无法目视，且实时战场存在全屏闪光导致像素级隔离测量不可用。所以以下只能由用户在游戏里判定：

- 残影是否读作「1 层细微鬼影」而不是「两把枪」；`maxCopies` / `startAlpha` / `minTipSpeed` 是主要旋钮。
- 移动端单次斩击 6–10 次 BakeMesh × 1 渲染器的实际开销。
- 参考图：`Library/Locus/tmp/afterimage/*.png`（本会话拍摄）与冻结中段实机截图 `Library/Locus/Screenshots/locus_game_20261016_154426_048.png`。

### 8.6 回退

1. 在 `AttackSystem.ExecuteSlash` 去掉 `enableWeaponAfterimage: true`（一行）。组件留在 prefab 上且默认 `m_Enabled: 0`，其他招式零影响。
2. 若整份撤掉：删除 `Assets/Scripts/Effects/MeshWeaponAfterimage.cs`、`SweepEffect` 的 `afterimage` 字段与调用点、`MeshWeaponVisual.Renderers`，并用 Unity API 从 prefab 移除组件。

### 8.7 首轮调参 + 本体涂抹（2026-10-16，用户反馈后）

用户反馈：**残影拖尾太长**、**武器本体需要一点动态模糊**。

**A. 收紧残影**（prefab 现值）：`lifetime 0.14 → 0.09`、`maxCopies 3 → 2`、`startAlpha 0.32 → 0.30`、`minTipSpeed 8 → 10`、`minSpeedIntensity 0.5 → 0.4`、`fadeCurve` 改为 3 键先快后慢（(0,1) (0.45,0.28) (1,0)）。

**B. 新增帧内本体涂抹（in-frame smear）**：`bodyBlur`（默认 true）/ `bodyBlurSamples 3` / `bodyBlurAlpha 0.3` / `bodyBlurSeconds 0.045`。同一帧只 `BakeMesh` 一次，3 份副本按「当前姿态往回 `bodyBlurSeconds`」分摊绘制（位置/旋转/缩放用 `LerpUnclamped`/`SlerpUnclamped` 外推），每份 alpha = `bodyBlurAlpha / K`，画在武器下层（`sortingOrder 1` < 武器 2），**不跨帧残留**（与残影的区别就在这）。

- **为什么按秒不按帧**：按「几帧」定义时，编辑器跑 ~120fps 实测涂抹只有 0.47 世界单位（约武器长 4%），低帧率设备上又会糊过头。改成秒后实测 **0.69 单位（约 6%）且与帧率无关**。
- **为什么不复用 Sprite 版 `WeaponMotionBlurController` 的 UV 方向模糊**：3D 网格的贴图 UV 不对应屏幕方向，偏移 UV 会变成贴图在模型面上扭曲，不是运动模糊。
- 复验：挥击峰值 ghosts=2（上限 2）、smear 3 份同时可见、结束后池/副本/Mesh 全清、Console 0 error / 0 warn。
- 新旋钮：`bodyBlurSeconds`（0.01–0.15，模糊长度）、`bodyBlurAlpha`（整体强度）、`bodyBlurSamples`（层数）；嫌糊就把 `bodyBlurSeconds` 降到 0.03 或 `bodyBlur` 关掉。
- 参考截图：`Library/Locus/Screenshots/locus_game_20261016_155207_756.png`。

### 8.8 扩到 Stab（2026-10-16）

**接线**：`Assets/Scripts/Attack/StabSweepEffect.cs` 新增 `_afterimage` 字段与 `enableWeaponAfterimage`（默认 false）参数，在 Initialize 末尾 `Begin(...)`；`HitStopRoutine` 加 `SetPaused`；回收段起点回调（`_retractStartTime`）调 `EndEmission`（与 `MeshWeaponTrail.retractSuppression` 同一意图，避免回收被读成拖泥带水）；`OnDestroy` 与 `HandOff` 调 `Clear`。只有 `AttackSystem.ExecuteStab` 传 `enableWeaponAfterimage: true`。

**组件新增** `MotionProfile { Swing, Thrust }`：`Begin(weaponVisual, profile)` 把对应一组参数解析成运行时字段，运行路径不再分支。

**为什么必须分开**：戳击实测枪尖峰值约 **127 u/s**，是斩击（~15 u/s）的近 10 倍。共用参数时实测残影拖尾 **15.2 世界单位**（>1.4 倍枪长）、本体涂抹 **2.25 单位**（21% 枪长），明显过糊。

**Thrust 现值**：`thrustSampleInterval 0.012` / `thrustLifetime 0.024` / `thrustStartAlpha 0.28` / `thrustMinTipSpeed 40` / `thrustFullTipSpeed 160` / `thrustMinSpeedIntensity 0.4` / `thrustBodyBlurSeconds 0.012` / `thrustBodyBlurAlpha 0.25`。

**复验**（Play Mode，Battle，Console 0 error / 0 warn）：

- STAB：峰值 2 个残影 + 3 份涂抹，残影拖尾（相对武器锚点）**1.29 单位 = 12% 枪长**；首次生成残影时的枪尖速度 39 u/s（门限 40）——起手不产生残影。
- SLASH 对照：拖尾 1.83 单位 = 17% 枪长。
- 两者结束后池根/副本/池化 Mesh 全为 0，无残留。

**注意**：戳击旧有的枪尖拖尾被残影取代（`replaceTipTrail`，与斩击一致）。想比较「两者并存」就把 prefab 上该开关关掉。

**截图**：`Library/Locus/Screenshots/locus_game_20261016_155818_021.png`（慢动作刺出中段）。

**相位收窄（同日用户更正：只要刺出段，回收段不要）**

根因：`EndEmission` 原本只停新残影，而**本体涂抹不受 `_emitting` 控制、只看速度**，所以往回拉的快动作照样在糊。

修法：

1. 组件新增 `SetEmission(bool)`（`EndEmission` 改为它的薄包装），并且**涂抹也归发射开关管**：`alpha = _emitting ? ... : 0`。
2. `StabSweepEffect` 用自身时间线显式括住刺出段，不再依赖速度阈值猜相位：`Begin` 后立即 `SetEmission(false)`；刺出 `DOMove(...).OnStart` 里 `SetEmission(true)`；`_retractStartTime` 回调 `EndEmission()`。

复验（Play Mode，Battle，Console 0 error / 0 warn）：

- 组件共存活 156 帧，**发射窗口 = 第 4–46 帧**（就是刺出段）。
- 起手（刺出前）：脏帧 0，残影 0，涂抹 0。
- 回收段：仍有可见东西的帧数 = **0**（连淡出尾巴也是 0：穿入段已经让最后几个残影先消散完）。
- 斩击回归：不受影响，峰值 2 残影 / 3 涂抹 / 干净回收。
