---
id: kd_3385bbb5-e543-4605-ac63-6349c715c6b6
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
summary: Y 场景自动搭建管线的**摆放表格式 v2** 与规格条目化模型：两种坐标空间（route/local）、每个节点一条目、组只是排序桶、往返验证口径，以及五个踩过的坑（组变换覆盖、世界坐标换算浮点误差、多精灵图集按路径取首个子精灵等）。
---

# Y 场景规格 / 摆放表管线（2026-10-24 定稿）

> 入口：`plan/scene-pipeline-handoff-20261023.md`（进度与待拍板项 ✅）。
> 本文只记**可复用规则** ✅：格式 ✅、数据模型 ✅、不变量 ✅、坑 ✅。
> 代码：`Assets/Scripts/Editor/Experiments/YScenePlacementImporter.cs` ✅、
> `Assets/Scripts/Editor/Experiments/YSceneSpecTools.cs` ✅、
> `Assets/Experiments/CurvedScroll/Scripts/SceneSpec/YSceneSpec.cs` ✅。

## 一、摆放表（= 自动搭场景的输入格式 ✅）

JSON ✅，两种坐标空间由表级字段 `space` 决定 ✅：

| space | 谁用 | `at` 是什么 | `layer` 是什么 |
|---|---|---|---|
| `route`（默认 ✅） | agent 手写 ✅ | **沿路坐标** [x 横向, z 沿路米数] ✅（可写 2 个数，y=0 ✅） | 分组桶名 ✅（节点根下的一个容器 ✅） |
| `local` | 导出器反推 ✅ | **相对父容器**的局部变换 ✅ | 父容器路径 ✅（空 = 直接挂节点根下 ✅） |

其余字段：`kind` = sprite / mesh / group（只带变换的容器 ✅）；`asset` = 资产路径 ✅；
`spriteName` / `meshName` = 子资产名 ✅（多精灵图集必填 ✅）；`material` + `materialPath` ✅；
`sortingOrder` ✅；`origin` + `rootRotation` = 节点根在场景里的位置/朝向 ✅。

## 二、规格模型：**每个节点一条目** ✅

- `YPropEntry.path` = **完整相对路径**（相对节点根 ✅）—— 层级靠路径还原 ✅；
- 每个节点的变换（位置/旋转/缩放）由**它自己那条条目**带 ✅；
- `YSceneGroup` **只是排序桶** ✅（路径前缀 ✅ + 组基准 sortingOrder ✅），**不带变换** ✅；
- 纯容器（无渲染器）= `containerOnly` 条目 ✅（生成时不加工渲染器 ✅）；
- 有组件但无渲染器的物件 = `YFuncEntry` ✅（组件数据用 EditorJsonUtility 逐字段无损 ✅）。

**生成器不变量**：同一条 transform 只被写一次 ✅ —— 条目写自己的 ✅，组不写 ✅，
路径上的容器由条目路径自动补出来 ✅（`FindOrCreatePath` ✅，只建不覆盖 ✅）。
→ 组与条目各写一次 = 互相覆盖 = 变换被抹掉 ✅（见坑 1 ✅）。

## 三、往返验证（= 管线是否无损的口径 ✅）

`Tools/Y 场景规格/⑤ 摆放表往返验证` ✅（prefab → 摆放表 → 规格 → 生成 → 逐件比对 ✅）：
比对**相对根的路径 + 局部变换 + 精灵/网格/材质/sortingOrder/sortingLayer** ✅。
`Tools/Y 场景规格/④ 往返验证` ✅ 是场景切片那条（导出切片 → 生成 → 比对 ✅）。

**2026-10-24 实测 ✅**：`Authoring/Segments/` 下 **15 个段 prefab 共 764 件 → 逐件差异 0** ✅；
整场景（`Y Junction - select for preview` ✅，275 件 ✅）从规格重建 → **逐件差异 0** ✅。

## 四、五个坑（都踩过 ✅）

1. **组写变换会覆盖条目的变换** ✅：组和条目写同一个物件 ✅，谁后写谁赢 ✅。
   → 组不带变换 ✅（或保证组与条目不碰同一个节点 ✅）。
2. **世界坐标换算有 ~1e-5 浮点误差** ✅：根有 315° 朝向时 ✅，
   `InverseTransformPoint` 往返一次就会漂 ✅（38m 处差 1.5e-5 ✅）→ 往返比对会报「位置看着一样却不一致」✅。
   → **一律用局部变换**（`localPosition/localEulerAngles/localScale` ✅），别做世界换算 ✅。
3. **多精灵图集按路径取会取错** ✅：`LoadAssetAtPath<Sprite>(贴图路径)` 返回**第一个**子精灵 ✅（RockRound → BushCompact ✅）。
   → 必须带 `spriteName` ✅，用 `LoadAllAssetsAtPath` 按名字取 ✅。
4. **只导带渲染器的节点 → 容器变换丢失** ✅：容器没渲染器 ✅，位置在它自己身上 ✅。
   → **全部变换节点都进表** ✅（容器 = `kind: group` ✅）。
5. **组路径前缀只取第一层** ✅：按 `layer` 前缀拼组路径时会截断 ✅ → 深层容器拿不到自己的变换 ✅。
   → 层级一律靠**条目完整路径** ✅（`layer + "/" + id` ✅），别靠前缀推导 ✅。

## 六、节点 prefab 生产（2026-10-24 ✅）

**新方案第一次真正产出内容** ✅（此前只有管线与自检 ✅）。

工具：`Tools/Y 场景规格/⑥ 节点 prefab 生产` ✅
（场景组 → 摆放表 ✅ → 规格 ✅ → **prefab** ✅ → 与源逐件比对 ✅）。

**产物约定** ✅：

| 产物 | 路径 | 说明 |
|---|---|---|
| 节点 prefab | `Authoring/Nodes/{nodeId}.prefab` ✅ | 根 = 节点 id ✅；**不带位置** ✅（`localRoot = true` ✅ → 原点/朝向 = 单位变换 ✅）→ 位置由路线系统放在锚点上 ✅ |
| 节点规格 | `Assets/RouteData/YSceneSpecs/{nodeId}.asset` ✅ | `nodeId` **必须**与 `FakeRouteNodeConfig.nodeId` 对齐 ✅ |

**EV 首产实测** ✅：源 57 件 ✅ → prefab 57 个对象 ✅ → 逐件差异 **0** ✅；
放到锚点后 49 个渲染器的世界位置偏差 ≤ 3.05e-05 ✅（float 精度 ✅）、旋转 \|dot\|=1 ✅、
材质与 sortingOrder 全同 ✅；**当前场景零改动** ✅（`isDirty=false` ✅）。

**两个易踩的坑** ✅：

1. **不激活的父级要连子树一起不搬** ✅：父级关着 → 子级 `activeSelf` 仍是 true ✅ →
   只跳父级会把子级搬成「凭空多出来的容器」✅（比对时报「多出」✅）。
   → 导出要判**实际可见**（自身 + 所有祖先都激活 ✅，`IsEffectivelyActive` ✅）。
2. **比对时的四元数符号** ✅：`q` 与 `-q` 是同一个旋转 ✅ → `Quaternion.Dot` 会是 **-1** ✅
   → 别据此判「旋转不一致」✅（用 `Mathf.Abs(Dot)` ✅ 或比 euler ✅）。

**还没做** ✅：prefab 的运行时加载器 ✅（S5 ✅）→ 场景里仍是手摆版 ✅；其余 5 个节点未进规格 ✅。

## 六·一、N3：把已验收布景**表化** + 按参考图补 6 件（2026-10-24 ✅）

> ⚠️ **口径更正** ✅：这一项**不是**「从参考图重建」✅ —— 主布局的坐标来自**已部署 prefab 的数值** ✅（即上一轮在游戏里目视调出来的结果 ✅）✅，
> 只有 6 件是**看参考图补的** ✅。原因见下（该节点的「照图 1:1 反算」在数学上不成立 ✅）。

**为什么这个节点不能照图 1:1 搭**（2026-10-24 实测 ✅）：

| 查证 | 结果 |
|---|---|
| 素材来源图 | `n3_village_architecture_g1_v1.png` = **孤立物件表**（4 物件摆棕底 ✅）→ 素材**不携带布局** ✅ |
| 构图参考图 | `4c7ef3f7-...png` = 手绘 2.5D 街景 ✅：超广角低机位 ✅、石板路占画面一半 ✅、村庄压在灭点附近**仅 ~140px 高** ✅ |
| 脚点可读性 | 多数物件的落地线**被前面的墙/篱笆/草丛挡住** ✅ → 读不出 ✅ |
| 尺寸可读性 | 孤立表里的渲染尺寸是任意的 ✅ → 只能从街景视宽反推 ✅ → 只能得到「远、小」的村庄 ✅ |
| 游戏取景 | 卷轴是 **shader 折弯的 flat world** ✅（相机固定 (0,3,-10) ✅ 俯角 18° ✅）→ 与参考图机位**不是同一个** ✅ |

⇒ 能从图里可靠读出的只有**布置关系**（谁在左/谁在前/相对大小 ✅）——这正是已部署那版承载的 ✅。

**产物** ✅：

| 产物 | 路径 | 说明 |
|---|---|---|
| **摆放表**（可手改的输入 ✅） | `Authoring/Placement/N3_Village.json` ✅ | 86 条 ✅（local 空间 ✅，层 = 分组 ✅） |
| 节点规格 | `Assets/RouteData/YSceneSpecs/N3.asset` ✅ | 15 组 / 86 条目 ✅ |
| 节点 prefab | `Authoring/Nodes/N3.prefab` ✅ | 根 = `N3` ✅，**不带位置** ✅ |
| **段 prefab**（已进段表 ✅） | `Authoring/Segments/N3 Village - From Reference Table.prefab` ✅ | 根 = 世界位姿 (-88.07, 0, 289.73) rot 315° ✅ → 段表 323→328 ✅ |

**参考图带来的新增**（已部署版本里没有的 ✅，素材现成 ✅ —— 位置/比例是**看图估的** ✅）：
`Reference Extras - From N3 Concept` 层 6 件 ✅ —— 望楼 ✅、营门 ✅、军帐 ✅、破车 ✅、断旗 ✅、木栅 ✅
（图上能对上的是：望楼 ✅（右中高台）、营门 ✅（远处门楼）、破车 ✅（右下车轮）、木栅 ✅（左前木桩）✅；
**断旗**是宽泛对应（图上是大幅红旗 ✅，素材是小断旗 ✅）、**军帐**对应较弱（图上那个更像屋 ✅）✅ —— 待用户验收 ✅）。

**验证** ✅：与已部署版本共同 84 件 → 位置最大偏差 **6.4 mm** ✅（表里写的是 2 位小数 ✅）；
Play Mode 跑到 N3 ✅ → 段已实例化 ✅ → 游戏内截图与参考图构图一致 ✅（Console 0 error ✅）。

> ⚠️ 旧的段 prefab `N3 Village Rescue - Accepted Art and Grey Wall Study @ 266` ✅ **已删除** ✅
> （同一份内容的两份拷贝 ✅ → 违背「表是唯一来源」✅ → 已删 ✅；git 历史可找回 ✅）。
> ⇒ N3 段是**第一个跑通「表 → 规格 → prefab」全链路**的段 ✅（其余 15 个段仍是手摆 prefab ✅，待同法处理 ✅）。

## 七、全段表化（2026-10-24 ✅）与**验收手段**

`Tools/Y 场景规格/⑧ 全段表化` ✅：导表 ✅ → 导规格 ✅ → 生成到临时场景并与原 prefab **逐件比对** ✅
→ 渲染**改前/改后同机位对照图** ✅ → **回写同一 prefab 路径** ✅（GUID 不变 ✅ → 段表不用动 ✅）。
已有同名表的段跳过 ✅；空占位 prefab 跳过 ✅ → **幂等** ✅。

**结果** ✅：**15 个段、764 条** ✅ 全部表化 ✅，逐件比对**全 0** ✅；
prefab 原地回写 ✅，`Authoring/Segments/` 下不再有手摆副本 ✅；
规格落 `Assets/RouteData/YSceneSpecs/{段名}.asset` ✅；表落 `Authoring/Placement/{段名}.json` ✅。

**三个验收手段** ✅：

| 手段 | 怎么做 | 看什么 |
|---|---|---|
| **对照图** ✅ | `Library/Locus/tmp/segment_compare/side_by_side/*.png` ✅ | 左=旧手摆 prefab ✅、右=表重新生成 ✅ → 肉眼看是否一致 ✅ |
| **数值比对** ✅ | `Tools/Y 场景规格/⑤ 摆放表往返验证` ✅ | 逐件差异必须 **0** ✅（位置/旋转/缩放/精灵/网格/材质/sortingOrder ✅） |
| **编辑器预览** ✅ | 选中段 prefab → `Tools/Y 场景规格/⑦ 预览段 prefab` ✅ | 按它在游戏里的位姿摆进场景 ✅（`DontSave` ✅ 不留垃圾 ✅）+ 镜头对过去 ✅ |

**已剔除** ✅：作者态关掉的旧残留 ✅（导出默认跳过 ✅ —— 判定依据见 `memory/y-junction-scene-legacy-inventory.md` ✅）。

## 八、场景是 flat world ✅

z = 沿路米数 ✅，x = 横向偏移 ✅；190m 处分叉 ✅，左右两支采样取最近的那支 ✅。
段 prefab 可以带自己的朝向（如 315° 分支朝向 ✅）→ 沿路坐标 ↔ 局部坐标要按根朝向换算 ✅（`route` 空间已内置 ✅）。
