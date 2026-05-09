# Editor 模块

## 模块名称
Editor（编辑器工具）

## 主要职责
为 `StageConfig` 和 `WaveConfig` 提供自定义 Unity Inspector 绘制，提供策划友好的编辑体验，包含自动分配 waveId。

## 核心类

| 类 | 说明 |
|---|---|
| `WaveConfigDrawer` (PropertyDrawer) | `WaveConfig` 的自定义属性绘制器。显示 waveId（只读）、isBossWave 开关和 rows 列表。 |
| `StageConfigEditor` (Editor) | `StageConfig` 的自定义 Inspector。位于 `Editor/WaveConfigEditor.cs`。绘制除 `waves` 外的所有默认属性，然后手动渲染 waves 列表，新增元素自动递增 waveId。每波以 helpBox 粗体标题展示。 |

## 公开接口

无运行时接口 — 仅 Editor 工具。

## 依赖模块

- `UnityEditor`
- `StageConfig`, `WaveConfig`（Core 模块）

## 重要规则

- 仅编译时/编辑器使用，不包含在运行时构建中
- waveId 由 `StageConfigEditor` 自动管理，无需手动维护

## 扩展指南

- 为 `WaveConfig` 添加新字段：在 `WaveConfigDrawer.OnGUI()` 布局中添加，同时调整 `GetPropertyHeight()`
- 为 `StageConfig` 添加新字段：通过 `DrawPropertiesExcluding` 自动绘制；仅 `waves` 列表需要特殊处理
