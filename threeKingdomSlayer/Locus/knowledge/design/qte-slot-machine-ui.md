---
id: kd_6229beba-8e69-41a3-90e8-c59e8debc69c
injectMode: inherit
summary: QTE老虎机UI动画重构的实现设计文档：滑动入场+填充+放大闪白+判定+滑动退场。涉及QTEDisplay重写、QTEController提早输入检测、QTE判定框场景配置。
aiMaintained: inherit
---

# QTE 老虎机 UI 重构设计

## 1. 需求概述

将 QTE UI 从简单的 DOTween scale pulse 改为老虎机风格的入场/填充/判定/退场动画：

```
slotidle（隐藏）
  → 图案从判定框上方下滑入框
  → 填充阶段：fillAmount 0→1（warningDuration 时长）
     → 提早输入：立即失败 → 下滑消失
  → 填充完成：放大 → 闪白 → 缩小（进入判定窗口）
  → 判定窗口：匹配手势 = 成功
  → 判定超时：下滑消失
  → 成功/失败后：下滑消失 + 特效
```

## 2. 改动范围

| 文件 | 改动程度 | 说明 |
|------|---------|------|
| `QTEDisplay.cs` | **重写** | 新增老虎机动画系统、填充驱动、frame 引用 |
| `QTEController.cs` | **中等** | 新增 standby 阶段提早输入失败检测；通知 display 阶段变化 |
| `InputManager.cs` | **不改** | 现有 TryConsumeQTEInput 已正确路由；QTE 屏蔽普通攻击已有 IsQTEActive |
| `QTEConfig.cs` | **不改** | warningDuration/judgeWindow 直接复用于填充+判定时长 |
| `QTEAttackConfig.cs` | **不改** | 无需变更 |
| `BossQTEData.cs` | **不改** | 无需变更 |
| `Enemy.cs` | **不改** | QTEAttacking 状态机不变 |
| `Battle.scene` | **小改** | 新增 QTE判定框 RectTransform（用户手动放） |

## 3. 核心架构

### 3.1 QTEDisplay 新 API

```csharp
// 取代 SpawnIndicator — 启动完整老虎机动画
public GameObject StartQTEIndicator(QTEConfig config, RectTransform frameRect);

// 通知进入判定窗口 — 触发放大闪白效果
public void OnJudgmentStart(GameObject indicator, QTEConfig config);

// 取代 ShowQTEResult — 成功/失败退场动画
public void ResolveIndicator(GameObject indicator, bool success, float slideOutDuration);

// 提前失败（提早输入）— 立即中断填充，下滑消失
public void CancelIndicatorEarly(GameObject indicator);
```

### 3.2 指示器实例数据结构

在 QTEDisplay 内部维护活跃指示器状态：

```csharp
private class IndicatorState
{
    public GameObject gameObject;
    public Image fillImage;       // 带 FillMethod 的 Image 组件
    public Sequence animationSeq; // DOTween Sequence（可 Kill）
    public bool fillComplete;     // 填充是否已完成
    public bool isCancelled;      // 是否被提前取消
}
```

### 3.3 动画序列（DOTween Sequence）

每个 QTE 指示器的完整动画序列：

```
1. 设置初始位置：indicator 在 frame 上方 slideDistance 处，alpha=0
2. Append: DOFade(1, slideInDuration) + DOLocalMoveY(targetY, slideInDuration)
   → 图案下滑入框（老虎机效果）
3. AppendCallback: 开始填充
4. Join: fillImage.DOFillAmount(1, warningDuration).SetEase(Ease.Linear)
   → 填充 0→1
5. AppendCallback: fillComplete=true; 通知 QTEController（进入判定）
6. Append: DOPunchScale(1.3, flashDuration) + DOColor(white, flashDuration/2) + DOColor(originalColor, flashDuration/2)
   → 放大闪白
7. [等待 QTEController 调用 ResolveIndicator 或 external Cancel]
```

退场动画（由 ResolveIndicator / CancelIndicatorEarly 触发）：
```
1. Kill 当前 Sequence
2. Sequence: DOLocalMoveY(frameBottom, slideOutDuration) + DOFade(0, slideOutDuration)
3. OnComplete: Destroy
```

### 3.4 填充方式选择

不新增 QTEConfig 字段。填充方式由指示器 prefab 上 Image 组件的 `FillMethod` 决定：

| QTE 类型 | 推荐 FillMethod | 说明 |
|---------|----------------|------|
| Click | Radial 360 | 圆形填充（现有 QTE_Click_Indicator 已配好） |
| Swipe | Horizontal | 横向填充条（需修改 QTE_Swipe_Indicator prefab） |

QTEDisplay 实例化 prefab 后读取 `Image.fillMethod`，按既有配置执行填充动画。

## 4. QTEController 改动细则

### 4.1 提早输入检测

在 `TryQTEClick()` / `TryQTESwipe()` 中新增 standby 阶段检测：

```csharp
// 伪代码
public bool TryQTEClick(Vector2 screenPos)
{
    if (_state != QTEState.QTEJudging && _state != QTEState.PerformingQTEAttack) return false;
    if (!_qtePhaseStarted) return false;

    foreach (var qte in _activeQTEs)
    {
        if (qte.resolved) continue;

        // NEW: standby 阶段提早输入 → 立即失败
        if (!qte.IsInJudgeWindow(_qtePhaseTimer))
        {
            ResolveQTE(qte, false);
            return true; // 消耗手势
        }

        // 原有判定逻辑...
    }
}
```

### 4.2 通知 QTEDisplay 阶段变化

在 `UpdatePerforming()` 中，QTE 进入判定窗口时通知 display：

```csharp
// 检查是否刚进入判定窗口
if (!qte._judgmentStarted && qte.IsInJudgeWindow(_qtePhaseTimer))
{
    qte._judgmentStarted = true;
    qteDisplay?.OnJudgmentStart(qte.indicator, qte.config);
}
```

`_judgmentStarted` 需要添加到 `QTEInstance` 中。

### 4.3 指示器生成

`SpawnQTEIndicator` 改为调用新 API：

```csharp
qte.indicator = qteDisplay.StartQTEIndicator(qte.config, qteFrameRect);
```

### 4.4 Resolve 流程

`ResolveQTE` 中结果特效改为调用新 API：

```csharp
qteDisplay.ResolveIndicator(qte.indicator, success, slideOutDuration: 0.3f);
```

注意：提早失败时，`CancelIndicatorEarly` 不需要显示成功/失败特效，直接下滑消失。

## 5. QTE 判定框（QTE Frame）

### 5.1 场景配置

在 `Battle.scene` 的 Canvas 下新增：

```
Canvas/
  ├─ QTEIndicators/        （现有，indicatorParent）
  └─ QTEFrame/             （新增）
       ├─ RectTransform     （判定框区域，含 RectMask2D 裁剪）
       └─ [可选] FrameImage （框体美术图，SpriteRenderer/Image）
```

### 5.2 QTEDisplay 新增字段

```csharp
[Header("老虎机动画")]
[Tooltip("QTE 判定框 RectTransform（定义可见区域 + 滑动起止范围）")]
public RectTransform qteFrameRect;
[Tooltip("下滑入场时长（秒）")]
public float slideInDuration = 0.25f;
[Tooltip("下滑退场时长（秒）")]
public float slideOutDuration = 0.3f;
[Tooltip("入场起始偏移（像素，frame 上方）")]
public float slideInOffsetY = 200f;
[Tooltip("放大闪白总时长（秒）")]
public float flashDuration = 0.3f;
```

### 5.3 坐标计算

```
slideStartY = frameRect.anchoredPosition.y + frameRect.rect.yMax + slideInOffsetY
targetY     = 0（frame 内居中）
slideEndY   = frameRect.anchoredPosition.y + frameRect.rect.yMin - slideInOffsetY
```

指示器的 anchor 设为 frame 中心，通过 `anchoredPosition.y` 控制垂直滑动。

## 6. 时序图

```
时间轴（以单个 QTE slot 为例）

t=0         t=slideIn   t=slideIn+warning    t=slideIn+warning+judge
| 入场下滑   |  填充 0→1  |  放大闪白 |  判定窗口              |  下滑退场

QTEController:  SpawnIndicator ────────────────── 输入判定 ────── ResolveIndicator
QTEDisplay:     slideIn → DOFill → flash → [等待] → slideOut
```

## 7. 边界情况

| 场景 | 处理 |
|------|------|
| 提早输入（填充中）| QTEController 检测 → ResolveQTE(false) → CancelIndicatorEarly |
| BOSS 死亡（QTE 中）| Enemy.Die() → AbortQTE() → ClearAllIndicators()（不变） |
| 转阶段（QTE 中）| Enemy → AbortQTE() → ClearAllIndicators()（不变） |
| Stun 打断 QTE | QTEController.Update() 检测 state≠QTEAttacking → AbortQTE()（不变） |
| 多 QTE 交错（TripleClick）| 每个 QTEInstance 独立 indicator + 独立 Sequence，互不干扰 |
| 填充中道具点击 | BuffDisplayPanel 独立于 InputManager，不受 QTE 屏蔽影响（不变） |

## 8. 向后兼容

- `QTEConfig` 字段不变，`warningDuration`/`judgeWindow` 语义不变
- `Enemy.cs` QTE 相关代码不变
- `InputManager.cs` 不变（TryConsumeQTEInput 接口不变）
- 旧 `SpawnIndicator` 方法保留但标记为 `[System.Obsolete]`，内部转发到新方法（降级路径）
- `ClearAllIndicators` 保留，增加对 `IndicatorState` 列表的清理

## 9. 实施步骤

1. **QTEDisplay 重写** — 新增 IndicatorState、老虎机动画序列、新 API（StartQTEIndicator / OnJudgmentStart / ResolveIndicator / CancelIndicatorEarly）
2. **QTEController 对接** — SpawnQTEIndicator 改用新 API；TryQTEClick/TryQTESwipe 新增 standby 提早输入检测；UpdatePerforming 新增 judgment start 通知
3. **QTEInstance 扩展** — 新增 `_judgmentStarted` 字段
4. **Battle.scene 连线** — 用户手动添加 QTEFrame RectTransform，QTEDisplay 拖入引用
5. **Swipe Indicator prefab** — 将 `QTE_Swipe_Indicator.prefab` 的 Image.FillMethod 改为 Horizontal
6. **测试验证** — 覆盖：正常成功、正常超时失败、提早输入失败、多QTE交错、BOSS死亡中断

## 10. 不改动的部分

- QTE 飞行物逻辑：不变
- QTE 动画（Animator Trigger）：不变
- QTE 架势伤害/充能/失败伤害数值：不变
- QTE 打断规则（interruptibleOnStun）：不变
- BOSS Idle 调度 QTE 触发：不变
- 转阶段 QTE 数据切换（SwitchQteData）：不变
