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

## 五、场景是 flat world ✅

z = 沿路米数 ✅，x = 横向偏移 ✅；190m 处分叉 ✅，左右两支采样取最近的那支 ✅。
段 prefab 可以带自己的朝向（如 315° 分支朝向 ✅）→ 沿路坐标 ↔ 局部坐标要按根朝向换算 ✅（`route` 空间已内置 ✅）。
