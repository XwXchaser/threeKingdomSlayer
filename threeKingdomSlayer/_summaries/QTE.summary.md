# QTE 模块

## 模块名称
QTE（Boss Quick Time Event 攻击系统）

## 主要职责
Boss 专属 QTE 攻击的完整生命周期管理：配置驱动的多段 QTE 编排（点击/划动）、状态机驱动、输入优先拦截、Canvas UI 指示器生成/销毁、结果判定与效果触发。

## 核心类

| 类 | 说明 |
|---|---|
| `QTEConfig` (ScriptableObject) | 单个 QTE 行为配置：类型（Click/Swipe）、时机（warningDuration/judgeWindow）、划动参数（方向/角度容差/最小速度）、效果数值（架势伤害/大招充能/失败伤害）、视觉（指示器 prefab/屏幕归一化坐标） |
| `QTESlot` (struct) | QTE 攻击中的单次 QTE 判断槽：config 引用 + delay（相对于攻击开始的延迟秒数） |
| `QTEAttackConfig` (ScriptableObject) | 一次 QTE 攻击的完整配置：QTESlot 列表、BOSS 动画参数（Trigger 名/animationLeadTime）、可选飞行物（prefab/flightTime/targetZ）、攻击后冷却。`TotalDuration` 属性自动计算最晚 QTE 结束时间 |
| `BossQTEData` (ScriptableObject) | Boss QTE 根数据：QTE 攻击列表（按序循环）、loopAttacks 开关、首次冷却/基础冷却 |
| `QTEController` (MonoBehaviour) | QTE 状态机核心。Phase-based 驱动：Idle → CoolingDown → WaitingForAttackFinish → PerformingQTEAttack → QTEJudging → QTECompleted。按 slot delay 独立计算 warning→judge→resolve 时间线。`TryConsumeClick(Vector2)` / `TryQTESwipe(Vector2, float, float)` 输入接口供 InputManager 调用。成功/失败各自触发架势伤害/玩家伤害。事件：OnQTESuccess/OnQTEFailure/OnQTECompleted |
| `QTEInstance` (class) | 运行时 QTE 实例数据：config 引用、各阶段时间戳、indicator GameObject、resolved/success 标志 |
| `QTEDisplay` (MonoBehaviour) | Canvas UI 管理器。`SpawnIndicator(QTEConfig)` — Instantiate 指示器 prefab → 设置 anchor 到位 → DOTween Scale 脉冲预警动画。`ShowQTEResult(indicator, success)` — 结果特效 + 指示器缩小消失。`ClearAllIndicators()` — 清理全部活跃指示器 |
| `QTEProjectile` (MonoBehaviour) | DOTween 飞行物。`Initialize(flightTime, targetPos, onReachTarget)` 飞向目标 → 回调通知 QTE 阶段开始。`ContinuePassThrough(time, onPassThrough)` QTE 失败后穿过摄像机。`DestroyOnSuccess()` QTE 成功后立即销毁 |

## 公开接口

**QTEController**：
- `TryConsumeClick(Vector2 screenPos)` — 点击型 QTE 输入，返回是否消费
- `TryQTESwipe(Vector2 direction, float distance, float speed)` — 划动型 QTE 输入，返回是否消费
- `OnEnemyAttackComplete()` — 敌人攻击动画完成回调（WaitingForAttackFinish → PerformingQTEAttack）
- `StartQTESequence()` — 手动启动 QTE 序列（冷却结束后调用）
- 事件：`OnQTESuccess` / `OnQTEFailure` / `OnQTECompleted`
- 序列化字段：`qteData`（BossQTEData）、`enemy`（Enemy 引用）

**QTEDisplay**：
- `SpawnIndicator(QTEConfig)` → GameObject — 生成指示器并启动预警动画
- `ShowQTEResult(GameObject indicator, bool success)` — 显示判定结果
- `ClearAllIndicators()` — 清除所有指示灯

## 状态机

```
Idle
  │ (StartQTESequence 或首次触发)
  ▼
CoolingDown (cooldownTimer 倒计时)
  │ (cooldownTimer <= 0)
  ▼
WaitingForAttackFinish (等待 enemy.isAttacking 结束)
  │ (enemy.OnAttackComplete → OnEnemyAttackComplete)
  ▼
PerformingQTEAttack (animationLeadTime 倒计时)
  │ (animationLeadTime <= 0 → QTE 阶段开始)
  ▼
QTEJudging (按 slot delay 独立 spawning→warning→judging→resolved)
  │ (全部 slot resolved)
  ▼
QTECompleted (统计结果 → 冷却 → 下一攻击 或 完成)
```

## 输入拦截机制

`InputManager.ProcessGesture` 顶部优先调用 `TryConsumeQTEInput`：
1. 查找 `QTEController`（`FindObjectOfType`）
2. 非划动输入 → `TryConsumeClick(screenPos)`
3. 划动输入 → `TryQTESwipe(direction, distance, speed)`
4. 命中 → return（短路后续攻击逻辑）
5. 未命中 → 继续正常攻击流程

## QTE 判定逻辑

**Click 判定**：`RectTransformUtility.RectangleContainsScreenPoint` 检测点击位置是否在指示器 RectTransform 范围内。

**Swipe 判定**：双阈值
- 方向阈值：`Vector2.Angle(swipeDirection, targetDirection) <= angleTolerance`
- 速度阈值：`distance / pressDuration >= swipeMinSpeed`（px/s）

## 配置资产

所有 QTE 配置资产位于 `Assets/ScriptableObjects/QTE/`：
- `BossQTEData_104.asset` — Enemy_104 的 QTE 根数据
- `QTEAttackConfig_TripleClick.asset` — 三连点击攻击
- `QTEAttackConfig_Swipe.asset` — 单次划动攻击
- `QTEConfig_Click_1/2/3.asset` — 三个屏幕位置的点击配置
- `QTEConfig_Swipe.asset` — 划动配置（向右划）

## Canvas UI Prefab

位于 `Assets/Prefabs/QTE/`：
- `QTE_Click_Indicator_1/2/3.prefab` — Image (200×200, circle 1 精灵)
- `QTE_Swipe_Indicator.prefab` — Image (400×60, PoiseBar 精灵)

全部使用 RectTransform + Image + CanvasRenderer，锚点居中，Raycast Target 开启。

## 依赖

- **DOTween**：指示器缩放动画、飞行物路径动画
- **InputManager**：通过 `FindObjectOfType<QTEController>()` 松散耦合
- **Enemy**：`QTEAttacking` 状态、`EnterQTEAttack`/`ExitQTEAttack` 方法
- **PlayerState**：失败伤害 `TakeDamage`
- **UltimateSystem**：成功充能 `AddEnergy`
