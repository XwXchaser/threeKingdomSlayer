# UI 竖屏分辨率适配 — 验收备忘

> 状态: ✅ 代码已改完，⏳ 待验收调参

---

## 一、改动总览

| # | 项 | 文件 | 改动 |
|---|-----|------|------|
| 1 | Victory panel CanvasScaler | `Assets/Scenes/Battle.scene` — `BattleHUD(Canvas)/Victory(panel)` | 添加 CanvasScaler: ScaleWithScreenSize / 1080×1920 / Match=1 |
| 2 | UIResolutionHelper | `Assets/Scripts/UI/UIResolutionHelper.cs` (新建) | 静态工具类，提供 `UIScale` 缩放系数 |
| 3 | StageProgressBar | `Assets/Scripts/UI/StageProgressBar.cs` | Awake 中 dotSpacing / lineThickness / dotDiameter / playerDotDiameter 乘系数 |
| 4 | MainMenuUI | `Assets/Scripts/UI/MainMenuUI.cs` | cellSize / spacing / fontSize 乘系数 |
| 5 | CoinCounterUI | `Assets/Scripts/UI/CoinCounterUI.cs` | floatTextFontSize / floatTextRectSize / floatUpDistance / 偏移 乘系数 |
| 6 | QTEDisplay | `Assets/Scripts/QTE/QTEDisplay.cs` | slideInOffsetY / fallback 600×150 乘系数 |

---

## 二、缩放系数公式

```
UIScale = Min(1, (ScreenW / ScreenH) / (1080 / 1920))
```

| 屏幕比例 | 分辨率示例 | UIScale | 效果 |
|---------|-----------|---------|------|
| 16:9 (参考) | 1080×1920 | 1.0 | 不变 |
| 18:9 | 1080×2160 | 0.89 | 元素缩小 11% |
| 19.5:9 | 1080×2340 | 0.82 | 元素缩小 18% |
| 21:9 | 1080×2520 | 0.76 | 元素缩小 24% |

叠加 `CanvasScalerAdapter`（BattleHUD Canvas 上已有）兜底：Canvas 有效宽度 ≥ 972px。

---

## 三、验收测试步骤

### 3.1 Editor Game View 测试

1. Game View 左下角选择 **Free Aspect** → 点击 **+** 创建自定义分辨率
2. 依次测试: `1080 × 1920`、`1080 × 2160`、`1080 × 2340`
3. 每个分辨率下检查以下项目

### 3.2 检查清单

- [ ] **Victory panel** — 通关触发，弹窗文字/按钮不变形、不被裁剪
- [ ] **波次进度条** — 节点 + 白线不超出边界，滚动动画正常
- [ ] **主菜单关卡网格** — cell 和间距不过宽导致换行异常
- [ ] **铜钱飘字** — 字体不过大，偏移不过远，动画不怪异
- [ ] **QTE 指示器** — 入场下滑起始位置合理，退场到位

### 3.3 调参指引

所有序列化字段在 **Inspector 中保持参考分辨率原值**，运行时由代码自动乘系数缩放。如需微调：

- **StageProgressBar**: Inspector 中调整 `dotSpacing`(150) / `lineThickness`(6) / `dotDiameter`(20) / `playerDotDiameter`(28)
- **MainMenuUI**: 改代码中 `CreateStageGrid()` 里的 `200`, `80`, `12`, `16` 基础值
- **CoinCounterUI**: Inspector 中调整序列化字段即可
- **QTEDisplay**: Inspector 中调整 `slideInOffsetY`(200)

---

## 四、关联文件

| 文件 | 角色 |
|------|------|
| `Assets/Scripts/UI/CanvasScalerAdapter.cs` | BattleHUD Canvas 动态 Match 调整 |
| `Assets/Scripts/UI/UIResolutionHelper.cs` | 统一缩放系数 |
| `Assets/Scripts/UI/StageProgressBar.cs` | 波次进度条缩放 |
| `Assets/Scripts/UI/MainMenuUI.cs` | 主菜单缩放 |
| `Assets/Scripts/UI/CoinCounterUI.cs` | 铜钱飘字缩放 |
| `Assets/Scripts/QTE/QTEDisplay.cs` | QTE 缩放 |
| `Assets/Scenes/Battle.scene` | Victory panel CanvasScaler |
