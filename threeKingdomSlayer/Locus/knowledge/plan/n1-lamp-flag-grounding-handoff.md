---
id: kd_b2972d2c-c7ed-4284-bc55-ae820dfb89bb
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# N1 路灯旗落地交接

> **状态：已完成（2026-10-16）**。启用旗已换 `YSampleScenery_E0Route` 材质并全部正缩放 + `flipX`；Y 场景已保存并关闭重载复核通过，Console 0/0。
> **当前权威数值以「当前状态」节为准（scenery 685、可见旗 1 面）**；下方 689 / 4 根启用旗等数字属历史快照。
>
> **后续调整（同日，用户要求）**：旗子过于高大，可见旗统一缩到原值 **70%**；画面左侧只保留 **1 面**旗（停用 `N1_LampFlag_Left_31`），右侧按用户指示不变（仍 2 面）。当时可见：左 1 面 `Left_24`、右 2 面 `Right_24`（1.505）与 `Right_31`（1.05）。（右侧两旗后被用户自行删除；`Left_24` 的 scale 随后在下一轮修复中由 −1.68 改为 1.68。）
>
> **部署修复（同日，用户报告 Scene/Game 方向相反）**：
>
> 1. `N1_LampFlag_Left_24` 与 `N1_LampFlag_Left_31` 的 `scale.x` 由负改正（−1.68→**1.68**、−1.7→**1.7**），`flipX` 保持 1。原因：路线 Shader 的投影分支用 `length()` 重建 XY，**丢弃负号**，而 Scene 编辑画面走非投影分支**保留负号** → 两条路径镜像（实测 `direct=122.91 / mirrored=0.00`）。改后 `direct=0.00` 完全一致，且**游戏画面像素零变化**（改前/改后 Game 路径 `0.00`）。
> 2. `scenery` 数组由 689 压缩到 **685**，清除用户删旗后留下的 4 个 null 槽。
>
> **用户已游戏验收：Scene 与游戏方向终于一致。此项关闭。**
>
> 详见 `memory/unity-project-understanding/route-projection-shader-constraints.md`。
>
> 以下「当前事实」「原因」「唯一部署操作」「验收」四节是**原始交接的历史记录**，其中「8 根旗」「scenery 689」「负 X Scale 是有意的、不要修正」等描述**已被上面的修复推翻**，不要按它们执行。当前权威状态见下方「当前状态」。
>
> 原交接「唯一部署操作：只改共享材质、保留缩放」按字面执行会**静默改变左旗朝向**：路线 Shader 用 `length()` 重建 XY，负 `scale.x` 的符号被丢弃。实测 `Default@-2.4` vs `E0Route@-2.4` 差异 122.44，`Default@-2.4` vs `E0Route@-2.4+flipX=1` 差异 0.00。详见 `memory/unity-project-understanding/route-projection-shader-constraints.md`。
>
> 另一处原文不准确：8 根旗共用同一个**场景内嵌**材质实例，改材质会 8 根全改（含 4 根停用）。因此按 Renderer 逐个指定 `YSampleScenery_E0Route.mat` 资源，停用旗保持原样。

## 当前状态（权威，2026-10-16 用户游戏验收后）

对象路径：

`Assets/Experiments/CurvedScroll/YJunctionSample.unity/Y Junction - select for preview/Opening Roadside Props/N1 Lamp Flags - Behind Battle`

目标行为：**Scene 编辑画面与 Game 实际画面方向一致**（本轮已由用户游戏验收确认）。

| 对象 | 启用 | Local Position | Local Scale | flipX | 材质 |
|---|---|---|---|---|---|
| N1_LampFlag_Left_24 | **是** | (-4.97, -0.85, 25.04) | (1.68, 1.68, 1.68) | true | E0Route |
| N1_LampFlag_Left_31 | 否 | (-7.2, 0.15, 36) | (1.7, 1.7, 1.7) | true | E0Route |
| N1_LampFlag_Left_39 | 否 | (-6.2, 2.8, 39) | (-0.95, 0.95, 0.95) | false | Sprites/Default |
| N1_LampFlag_Right_39 | 否 | (6.2, 2.8, 39) | (0.95, 0.95, 0.95) | false | Sprites/Default |
| N1_LampFlag_Left_48 | 否 | (-6.6, 3.4, 48) | (-0.75, 0.75, 0.75) | false | Sprites/Default |
| N1_LampFlag_Right_48 | 否 | (6.6, 3.4, 48) | (0.75, 0.75, 0.75) | false | Sprites/Default |

- `N1_LampFlag_Right_24` 与 `N1_LampFlag_Right_31` **已由用户从场景删除**，不再存在；
- 可见旗只有 **1 面**（`Left_24`，画面左侧）；
- `YScrollSample.scenery` 长度 **685**，空项 0，重复 0；
- 场景已保存（`isDirty=False`），关闭重载复核一致；Console `0/0`；
- 名称里的 24/31/39/48 是创建时的旧 Z，不是当前 Z，以表中 Position 为准。

### 维护规则

- **新加/修改路线投影材质上的景物：`scale.x` 一律用正值，左右朝向用 `flipX`。** 不要用负 `scale.x` 做镜像，否则 Scene 与 Game 会镜像（详见下方约束文档）。
- `Left_39` / `Left_48` 的负缩放是历史遗留。它们在内嵌 `Sprites/Default` 上（无投影分支）所以 Scene/Game 仍一致；但**若将来启用它们、或改为路线材质，必须先改成正缩放 + `flipX`**。
- 在 Hierarchy 里删除被 `scenery` 引用的对象会留下 null 空槽，记得压缩（只删 null，不要删非空项）。

---

## 以下为原始交接历史（部分结论已被推翻）

### 当前事实（历史）

Y 场景已保存这组旗帜，当前只加载 `Assets/Scenes/MainMenu.scene`，MainMenu/Battle 均不脏。不要把编辑态 Scene 视图中的插地外观当成 Game 效果。

对象位于：

`Assets/Experiments/CurvedScroll/YJunctionSample.unity/Y Junction - select for preview/Opening Roadside Props/N1 Lamp Flags - Behind Battle`

8 个 Renderer 已登记在 `YScrollSample.scenery` 的 681–688，数组总数 689，无空项、无重复。Sprite 都是 `N1_SmallBrokenFlag_v1`，Sorting 为 Default/0。8 个对象共用场景内嵌材质 `Sprites/Default`（场景 fileID `627715111`，Unity 内建 Shader），没有使用路线投影材质。

| 对象 | 启用 | Local Position | Local Scale |
|---|---|---|---|
| N1_LampFlag_Left_24 | 是 | (-6.6, -0.85, 24) | (-2.4, 2.4, 2.4) |
| N1_LampFlag_Right_24 | 是 | (6.8, -0.7, 28) | (2.15, 2.15, 2.15) |
| N1_LampFlag_Left_31 | 是 | (-7.2, 0.15, 36) | (-1.7, 1.7, 1.7) |
| N1_LampFlag_Right_31 | 是 | (7.4, 0.15, 42) | (1.5, 1.5, 1.5) |
| N1_LampFlag_Left_39 | 否 | (-6.2, 2.8, 39) | (-0.95, 0.95, 0.95) |
| N1_LampFlag_Right_39 | 否 | (6.2, 2.8, 39) | (0.95, 0.95, 0.95) |
| N1_LampFlag_Left_48 | 否 | (-6.6, 3.4, 48) | (-0.75, 0.75, 0.75) |
| N1_LampFlag_Right_48 | 否 | (6.6, 3.4, 48) | (0.75, 0.75, 0.75) |

负 X Scale 是有意让左侧旗面朝向道路，不要“修正”为正值。名称里的 24/31/39/48 是创建时的旧 Z，不是当前 Z；以表中 Position 为准。

> ⚠️ **此条已作废**：`Left_24` / `Left_31` 的负缩放已被认证为 **Scene/Game 镜像缺陷**，已改为正值 + `flipX`。仅 `Left_39` / `Left_48` 仍保持负值（停用、无投影材质）。详见开头「当前状态」。

### 原因（历史）

`viewCamera` 为空时，Scene/编辑画面不执行卷轴投影，旗帜看起来插在未下压的地面上。Play 时 `BattleYRouteHost` 绑定 Battle Main Camera，地面和普通景物从 depth 18 后按 `0.009 × (depth − 18)²` 下压。这 4 根启用旗不读 `_YSEnabled`，因此地面下降后旗杆留在原高度，Game 中飞到天上。

现有路线材质是 `Assets/Experiments/CurvedScroll/Authoring/YSampleScenery_E0Route.mat`，Shader 为 `CurvedScroll/Y Sample Scenery E0 Route`。地面与该材质使用同一下压公式。天空背景 `CurvedScroll/Y Sky Background` 明确不进 scenery、不读投影参数，且不得包含旗帜。

### 唯一部署操作（历史）

把 4 个启用 Renderer 的材质改为 `YSampleScenery_E0Route`，并给两根左旗加 `flipX=true`。保留父节点、位置、旋转、缩放、Sprite、Sorting、启用状态和 scenery 引用。

实测落地数据（`progress=0`、`right=false`、根临时 `Y=-1.8`、Battle Main Camera）：

| 对象 | Z | 换材质前 Y | 下压 | 下压后 Y | flipX |
|---|---|---|---|---|---|
| N1_LampFlag_Left_24 | 24 | −0.85 | 0.324 | −1.174 | true |
| N1_LampFlag_Right_24 | 28 | −0.70 | 0.900 | −1.600 | false |
| N1_LampFlag_Left_31 | 36 | 0.15 | 2.916 | −2.766 | true |
| N1_LampFlag_Right_31 | 42 | 0.15 | 5.184 | −5.034 | false |

旗顶仍明显高于左右残垣，中央道路未被旗杆侵入；对照破车未变。

不要：

- 启用 z=39/48 的 4 根停用旗；
- 改任何 Local Position 或 Scale，包括为了“落地”继续调 Y；
- 新建材质、改 Shader、把旗画进天空背景；
- 改 `JunctionDistance`、旅行距离、Battle 相机、战斗坐标或原有破车；
- 改动期间调用 `Tools/Curved Scroll/Open Battle Host`。

### 验收（历史）

加载 Battle，并 Additive 加载 Y。用 Battle Main Camera、progress 0、right false、路线根临时 Y=-1.8 渲染；结束后恢复 progress、right、viewCamera、根高度和 RenderTexture。不保存 Battle。

通过标准是 4 根基座随可见地面下压，旗顶仍明显高于左右残垣，中央道路不被旗杆侵入。对照物是原破车 `N1_BrokenCart_Left_01=(-3.6,-0.7,4.2)`、`N1_BrokenCart_Right_01=(3.7,-0.5,5.2)`，两者不得变化。

若旗顶被压到地平线以下，停止继续调 Y，回来重新决定缩放；不要在同一轮里改距离或投影公式。通过后只保存 Y 场景，关闭重载，确认 scenery 仍为 689、空项 0、重复 0，4 根启用旗的材质已变为 E0 Route，4 根停用旗仍停用。Console warning/error 应为 0。

> ⚠️ **数量类断言已过期**：现在 scenery 为 **685**、启用旗为 **1 根**（`Left_24`）。“689 / 4 根启用”属历史值。

## 验证结果

### 第三轮：部署修复（Scene/Game 方向一致 + 数组清空槽）

磁盘回读（关闭重载后复核，与内存一致）：

| 对象 | isActive | localScale.x | flipX | 材质 |
|---|---|---|---|---|
| N1_LampFlag_Left_24 | 1 | **1.680**（原 −1.68） | 1 | E0Route |
| N1_LampFlag_Left_31 | 0 | **1.700**（原 −1.7） | 1 | E0Route |
| N1_LampFlag_Left_39 | 0 | −0.95（未改） | 0 | Sprites/Default |
| N1_LampFlag_Right_39 | 0 | 0.95 | 0 | Sprites/Default |
| N1_LampFlag_Left_48 | 0 | −0.75（未改） | 0 | Sprites/Default |
| N1_LampFlag_Right_48 | 0 | 0.75 | 0 | Sprites/Default |

- `scenery` 长度 **689 → 685**，空项 **4 → 0**，重复 0；6 个剩余旗 Renderer 全部仍注册；
- **Scene 路径 vs Game 路径：`direct=0.00`，两侧渲染完全一致**（修复目标达成）；
- **Game 路径改前/改后 = `0.00`**：游戏内观感零变化；实际翻转的是 Scene 编辑画面（由镜像转为与游戏一致）；
- 真实 Battle Main Camera portrait 渲染：屏幕可见左 1 面（`Left_24`, viewportX=0.255）、右 0 面；
- Console 0/0；Battle/MainMenu 未保存，Y 已保存并关闭。
- **用户游戏验收通过：当前旗帜场景中与实际游戏中的方向终于一致。本轮修复关闭。**

`Left_39` / `Left_48` 仍为负缩放但**有意不动**：它们在内嵌 `Sprites/Default` 上（无投影分支），Scene 与 Game 本来一致；且均为停用对象。

### 第二轮：缩小到 70% + 左侧只留 1 面

磁盘回读（关闭重载后复核，与内存一致）：

| 对象 | isActive | localScale | flipX | 材质 |
|---|---|---|---|---|
| N1_LampFlag_Left_24 | 1 | −1.68 | 1 | E0Route |
| N1_LampFlag_Right_24 | 1 | 1.505 | 0 | E0Route |
| N1_LampFlag_Left_31 | 0 | −1.7（未改） | 1 | E0Route |
| N1_LampFlag_Right_31 | 1 | 1.05 | 0 | E0Route |
| N1_LampFlag_Left_39 | 0 | −0.95 | 0 | Sprites/Default |
| N1_LampFlag_Right_39 | 0 | 0.95 | 0 | Sprites/Default |
| N1_LampFlag_Left_48 | 0 | −0.75 | 0 | Sprites/Default |
| N1_LampFlag_Right_48 | 0 | 0.75 | 0 | Sprites/Default |

- 2.15 × 0.7 = 1.505、1.5 × 0.7 = 1.05，缩放数值精确；
- 停用的 `Left_31` **位置、缩放、flipX 均未动**，只改 `m_IsActive`；
- 屏幕可见计数（progress 0，真实 Game 机位 portrait 540×1080）：左侧 1 面、右侧 2 面；
- scenery 仍 `689 / 空项 0 / 重复 0`，Console 0/0；
- 两根停用的 39/48 仍停用且仍用无投影的内嵌材质（未启用，不影响画面）。

缩到 70% 后**基座仍贴地**：贴图 pivot 在底部上方 16px，缩小时可见底边会比原尺寸抬升约 `0.16 × scale`；但下沉量由 Z 决定、与 scale 无关，放大对比两档截图（`base_now_70pct.png` vs `base_before_100pct.png`）未出现新悬空。

### 第一轮：落地（材质替换）

静态与磁盘：

- E0Route guid 引用数 `379 → 383`（+4），内嵌 `Sprites/Default` 引用数 `8 → 4`（−4），其它引用未动；
- 关闭 Y 场景后从磁盘重载：4 根启用旗 `YSampleScenery_E0Route` + 左旗 `flipX=True`，4 根停用旗仍为内嵌 `Sprites/Default` 且 `activeSelf=False`；
- 位置/缩放/旋转/Sorting/Sprite 全部与交接表一致；scenery `689 / 空项 0 / 重复 0`；
- Console warning/error `0/0`；MainMenu 与 Battle 均未变脏，Battle 已关闭（未保存）。

像素级 before/after（同机位、同 `progress`，`cmp_before_*.png` vs `cmp_after_*.png`）：

- `progress 0.075`：改前旗杆与旗面向下延伸到**地平线下方**、基座悬浮；改后基座落在路面，与路侧岩石/枯草同层；
- `progress 0.115`：同上，左右两侧均落地；
- 中央道路在两帧中均未被旗杆侵入。

截图位于 `Library/Locus/tmp/`（临时验证用，非交付物），保留原始值以便复查。
