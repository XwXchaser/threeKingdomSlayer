---
id: kd_416e65b8-76c3-4276-9ca6-ef4e72dffd3d
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
summary: 搭场景的**方法与约束**（不是识图建议）：参考图只给「物件怎么搭配」，尺度一律按游戏自己的数据算；七条可执行的约束（路面半宽、路肩、建筑退让、墙体朝向与接龙、近场留空、只用本节点素材、缺口记账）与每条的数据来源、自检方式。
---

# 搭场景的方法与约束（2026-10-24）

> 本文只写**方法**（怎么搭 ✅）与**约束**（不能违反什么 ✅，每条都有数据来源与自检 ✅）。
> **不含识图建议** ✅ —— 参考图只提供「物件怎么搭配」 ✅（房屋与围墙的关系 ✅），
> 尺度一律按下面这些**游戏自己的数据**算 ✅（参考图里的路宽/比例不可信 ✅）。

## 方法（四步 ✅）

| 步 | 输入 | 产出 |
|---|---|---|
| ① 搭配 | 参考图 ✅（只读"哪些构件、怎么配对" ✅） | 构件清单 + 配对关系 ✅（例：沿街墙 ↔ 房屋背靠墙 ✅；井/柴垛贴墙根 ✅；门楼在街尾 ✅） |
| ② 摆位 | **下面的约束表** ✅（不量图 ✅） | 每个构件的坐标 ✅ |
| ③ 落地 | 摆放表 `Authoring/Placement/{名字}.json` ✅ | 规格 ✅ → prefab ✅（`Tools/Y 场景规格/⑥⑧` ✅） |
| ④ 验收 | 自检 ✅ + 街景机位渲染 ✅ | 逐件差异 0 ✅ + 不侵入道路 ✅ + 目视 ✅ |

## 约束表（七条 ✅，逐条有数据来源与自检 ✅）

| # | 约束 | 数据来源 | 自检 |
|---|---|---|---|
| 1 | **沿街构件不得侵入路面**：内边缘 ≥ 路面半宽 ✅ | 路面半宽 = `ScrollRouteRoadSurface.width / 2`（=2.5m ✅） | 摆放表填 `roadHalfWidth` ✅ → 导入时自动报出侵入的件 ✅ |
| 2 | **硬边构件（墙/栅栏）退到路肩外**：≥ 路面半宽 + 路肩宽 ✅（=3.5m ✅） | 路肩宽 = `ScrollRouteShoulderSurface.width`（=1m ✅） | 同上 ✅ |
| 3 | **建筑退让**：位置 = 路面半宽 + 间隙 + **素材自身可见半宽** ✅（含枢轴偏移 ✅） | 素材可见宽/枢轴 = `slice_manifest.json` 的 `visible_width_world_at_ppu100` ✅ + `pivot_normalized` ✅ + `contour_bbox_alpha_gt16` ✅ | 同上 ✅ |
| 4 | **墙体精灵按侧别配对** ✅：主体朝街外延伸 ✅（`N3_B1` 枢轴在右端 → 摆左侧 ✅；`N3_B2` 枢轴在左端 → 摆右侧 ✅） | 素材的 `pivot_normalized` ✅ | 规则 1 的自检会抓到摆反的 ✅ |
| 5 | **3D 墙成链**：4m 一段（`N3_Wall4m_*` 的物理长度 ✅）✅，变体顺序 Start→Open→Open→End ✅ | `N3_WallSurface_G1_Atlas.manifest.json` 的 `physical_size_m` ✅ | 链段位置自检 ✅ |
| 6 | **街道近场留空** ✅：前 ~8m 只有路 ✅（不摆构件 ✅） | 街区节奏 ✅（玩家要有落脚地 ✅） | 目视 ✅ |
| 7 | **只用该节点自己的素材集** ✅；素材缺 → 记 `skipped` ✅（不生成 ✅、不借用别处素材 ✅） | `Art/EnvironmentV2/<节点>/…` ✅ | 表里逐条有交代 ✅ |

> 非本节点素材若必须保留 → **单列一层并写明** ✅（例：`Existing Vegetation (NOT N3 art - kept from accepted build)` ✅）→ 不想要整层删掉即可 ✅。

## 表的字段 ✅

```jsonc
{
  "nodeId": "N3 From Image Reading", "space": "local",
  "origin": [0,0,0], "rootRotation": [0,0,0],   // 节点根位姿 ✅（段 prefab 靠它 ✅）
  "roadHalfWidth": 2.5,                            // → 规则 1/2/3 的自检 ✅
  "skipped": [ { "element": "近左木栅", "reason": "N3 素材集里没有木栅", "note": "补素材时按墙的规则位" } ],
  "items": [ { "id": "...", "layer": "...", "kind": "sprite|mesh|group",
               "spriteName": "...", "asset": "...", "material": "...", "materialPath": "...",
               "at": [x,y,z], "rotation": [0,0,0], "scale": [s,s,s], "sortingOrder": 0 } ]
}
```

`at` = **相对父容器**的局部坐标 ✅（`layer` 决定父容器 ✅）—— 局部值才精确 ✅。

## 工具 ✅

| 工具 | 作用 |
|---|---|
| `Tools/Y 场景规格/⑥ 节点 prefab 生产` | 表 → 规格 → prefab + 逐件比对 ✅ |
| `Tools/Y 场景规格/⑧ 全段表化` | 批量：导表 → 导规格 → 比对 → 回写 prefab ✅（幂等 ✅） |
| `Tools/Y 场景规格/⑦ 预览段 prefab` | 把段按游戏位姿摆进场景看 ✅（`DontSave` ✅） |
| 导入自检 | 打印缺口清单 ✅ + 报出侵入道路的件 ✅ |

## 样例 ✅

`Authoring/Placement/N3 Village - From Image Reading.json` ✅（81 条 ✅ 侵入道路 0 ✅）
预览：`Library/Locus/tmp/N3_rebuild_preview/` ✅（`0_reference_vs_rebuild.png` ✅ 为参考图 + 重搭并排 ✅）
