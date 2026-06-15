---
id: kd_af882051-7c31-436c-8995-455c11b68c2d
type: memory
path: ui-dev-patterns.md
title: ui-dev-patterns
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1781504246030
updatedAt: 1781504246031
---

# ui-dev-patterns

## Summary
程序化 UI 开发三原则：静态布局归 prefab、动态值用基准偏移、Edit Mode 预览只重建叶子。避免代码覆写设计师手动调整的布局参数。

<!-- locus:body:start -->
# 程序化 UI 开发原则

## 核心问题

代码在动态调整 UI 时，容易覆写设计师在 prefab 中手动调整的布局参数。
根因：没有区分「用户设置的静态布局」和「代码驱动的动态值」。

## 三条规则

### 1. 静态布局归 prefab，动态数据归代码

- 位置、大小、锚点 → 设计师在 prefab 中调整，代码**只读不写**
- 滚动偏移、动态数量、颜色变化 → 代码以 prefab 值为**基准线**，做相对计算
- 反例：`_contentRect.anchoredPosition = new Vector2(-windowStart * dotSpacing, y)` — 用绝对值覆写了设计师设置的 X

### 2. 如果代码必须控制位置（如滚动），用「基准偏移」模式

```csharp
// Awake/BuildVisuals 时：首次读取用户布局作为基准
_originX = rect.anchoredPosition.x;

// 运行时：基准 + 动态偏移
rect.anchoredPosition = new Vector2(_originX - scrollOffset, rect.anchoredPosition.y);
```

不要用 `Vector2.zero` 或硬编码绝对值作为初始位置。

### 3. Edit Mode 预览：只重建叶子，保留结构节点

- **结构节点**（Frame、Content 容器等）：首次创建后序列化到 prefab，后续 `BuildVisuals` 用 `transform.Find()` 复用，不修改其 RectTransform
- **叶子节点**（Line、Node、PlayerDot 等预览元素）：标记 `HideFlags.DontSave | HideFlags.NotEditable`，预览重建时销毁并重建
- **RectMask2D**：也给 `PreviewFlags`，不序列化
- 预览触发：用 `[ExecuteAlways]` + `Update` 脏标记兜底（`OnEnable` 在 Prefab Stage 打开时可能不触发）

## 实际案例

### StageProgressBar
- Content X 被 `UpdateContentPosition` 覆写为 0 → 引入 `_contentOriginX`，缓存 prefab 中设计师设置的 X 偏移
- Frame/Content 每次重建被销毁 → `DestroyEditModePreview` 只清理 PreviewFlags 子节点，BuildVisuals 用 Find 复用已有结构节点

## 反模式（应避免）

- `MainMenuUI`：StageGrid 完全用硬编码 anchor 构建
- `KillRewardUI`：实例化 prefab 后立即覆写其 anchor/position
- `QTEDisplay`：Ghost 子节点固定全拉伸，无设计师调整入口
<!-- locus:body:end -->
