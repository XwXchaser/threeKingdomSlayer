---
id: kd_744aa7d9-8d09-4efb-a832-650e5fe1e011
type: design
path: ui-resolution-adaptation.md
title: ui-resolution-adaptation
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1782725196525
updatedAt: 1783054538884
---

# ui-resolution-adaptation

## Summary
竖屏 UI 分辨率适配方案：动态 CanvasScaler Match 调整 + UIResolutionHelper 统一缩放系数 + 代码中绝对像素值适配。已完成 StageProgressBar、MainMenuUI、CoinCounterUI、QTEDisplay、UpgradePopup、Victory panel 适配。

## Content
# UI 竖屏分辨率适配方案

## 问题背景

项目使用 Canvas Scaler `Scale With Screen Size`，参考分辨率 1080×1920，Match=1（高度驱动）。在比 16:9 更长的屏幕上（18:9、19.5:9、21:9 等），Canvas 有效宽度被压缩，导致使用绝对像素值的 UI 元素水平方向重叠。

| 屏幕比例 | 分辨率示例 | Match=1 时 Canvas 有效宽度 |
|---------|-----------|--------------------------|
| 16:9（参考） | 1080×1920 | 1080 |
| 18:9 | 1080×2160 | 960（窄 11%） |
| 19.5:9 | 1080×2340 | 886（窄 18%） |
| 21:9 | 1080×2520 | 823（窄 24%） |

## 已完成修改

### 1. CanvasScalerAdapter — 动态 Match 调整

**文件**：`Assets/Scripts/UI/CanvasScalerAdapter.cs`  
**挂载**：`Assets/Scenes/Battle.scene/BattleHUD(Canvas)`

**原理**：根据当前屏幕宽高比动态计算 `CanvasScaler.matchWidthOrHeight`，确保 Canvas 有效宽度不低于参考宽度的 90%（可配置 `_minWidthRatio`）。

- 16:9 屏幕：Match=1（无变化）
- 更长屏幕：自动降 Match，宽度锁定 ≥ 972px

**参数**：
- `_referenceWidth`: 1080
- `_referenceHeight`: 1920
- `_minWidthRatio`: 0.9

### 2. UIResolutionHelper — 统一缩放系数

**文件**：`Assets/Scripts/UI/UIResolutionHelper.cs`（新建）

静态工具类，提供 `UIScale` 属性：

```
UIScale = Min(1, (ScreenW / ScreenH) / (1080 / 1920))
```

参考分辨率下 = 1，窄屏时 < 1。所有代码中创建 UI 的绝对像素值乘此系数即可等比缩放。

| 屏幕比例 | 分辨率示例 | UIScale |
|---------|-----------|--------|
| 16:9 | 1080×1920 | 1.0 |
| 18:9 | 1080×2160 | 0.89 |
| 19.5:9 | 1080×2340 | 0.82 |
| 21:9 | 1080×2520 | 0.76 |

叠加 `CanvasScalerAdapter` 兜底：Canvas 有效宽度 ≥ 972px。

### 3. 代码中绝对像素值适配（已完成）

| 脚本 | 适配内容 |
|------|---------|
| `StageProgressBar.cs` | `dotSpacing`、`lineThickness`、`dotDiameter`、`playerDotDiameter` 乘 `_uiScale`；Edit Mode 预览对象加 `HideFlags.DontSave` |
| `MainMenuUI.cs` | `cellSize`、`spacing`、`fontSize` 乘 `_uiScale` |
| `CoinCounterUI.cs` | `floatTextFontSize`、`floatTextRectSize`、`floatUpDistance`、偏移量 乘 `_uiScale` |
| `QTEDisplay.cs` | `slideInOffsetY`、回退默认 600×150 乘 `_uiScale` |
| `CameraManager.cs` | `background` null 检查修复 |

### 4. UpgradePopup 添加 CanvasScaler

**文件**：`Assets/Prefabs/UI/UpgradePopup.prefab`

弹窗有自己的 Canvas 但之前没有 CanvasScaler，以恒定像素渲染，导致 Editor Game View 和真机表现不一致。

**配置**：Scale With Screen Size / 1080×1920 / Match=0.5

### 5. Victory Panel 添加 CanvasScaler

**文件**：`Assets/Scenes/Battle.scene` — `BattleHUD(Canvas)/Victory(panel)`

嵌套 Overlay Canvas 补全 CanvasScaler：ScaleWithScreenSize / 1080×1920 / Match=1。

## 待修复项

### 中等 — 部分适配但值写死

| 脚本 | 硬编码值 |
|------|---------|
| `QTEConfig.cs` | 默认 `indicatorSize=(200,200)`，注释写明"基于参考分辨率 1080×1920" |
| `GlobalKillDisplayConfig.cs` | 默认 `displaySize=(200,200)` |
| `KillRewardUI.cs` | `milestoneLabelOffsetX=-30`、`fontSize=18` |

### 低优先级 — 已有局部缩放

| 脚本 | 说明 |
|------|------|
| `SpriteNumberDisplay.cs` | `_digitSize=(16,20)`、`_spacing=1`；已有 `_displayScale` 可手动缩放 |
| `DamageNumber.cs` | World-space，不受 CanvasScaler 约束 |

## 策略

1. **Canvas 级别**：所有独立 Canvas 必须有 CanvasScaler（或继承父级缩放）
2. **组件级别**：代码中创建 UI 时，绝对像素值需乘 `UIResolutionHelper.UIScale`
3. **动态 CanvasScalerAdapter** 作为兜底，防止极端屏幕比例下宽度过度压缩
