# Attack 模块

## 模块名称
Attack（攻击波特效）

## 主要职责
创建并管理攻击波视觉特效。根据攻击类型生成沿 Z 轴移动（Travel 模式）或原地展现（Fixed 模式）的攻击波实体，按位置阈值命中敌人、造成伤害后自毁。

## 核心类

| 类 | 说明 |
|---|---|
| `AttackWave` (MonoBehaviour) | 自包含的攻击波实体。支持两种模式：**Travel**（Sweep/Pierce/Stab — 沿 Z 轴移动）和 **Fixed**（Slash/Launch — 原地按排错开命中）。DOTween 驱动移动/淡出，Update 驱动 Fixed 模式。 |
| `WaveMode` (enum, private) | Fixed, Travel |
| `TargetEntry` (struct, private) | 目标敌人引用 + 命中延迟或 Z 阈值 |

## 公开接口

- `static AttackWave Create(Vector3 position, DamageType damageType, float damage, List<Enemy> targets, Action<Enemy> onHit = null, GameObject prefab = null)` — 工厂方法。创建攻击波 GameObject、附加组件、排序目标、选择模式、启动 DOTween 序列。返回 AttackWave 实例。

## 依赖模块

- `Enemy`：调用 `TakeDamage()`，读取 `state != EnemyState.Dead`、`rowIndex`、`transform.position`
- `DamageType` 枚举（定义于 Enemy.cs）
- **DOTween** (`DG.Tweening`)：移动序列、淡出、缩放

## 重要规则

- Travel 波跟随 Z 轴：按 Z 排序目标，跨越 `zThreshold` 时命中
- Stab：刺出后收回；刺出瞬间立即命中所有目标（单排范围）
- Fixed 波：按 `rowIndex` 以伤害类型特定延迟错开命中（StabStagger=0.03s, SlashStagger=0.05s 等）
- 波颜色按类型编码（Stab=黄, Slash=蓝, Pierce=绿, Sweep=红, Launch=紫）
- 无存活目标时波在 0.2s 内自毁
- Travel 波 `CheckHitThresholds()` 做方向感知比较（+Z 或 -Z）

## 扩展指南

添加新伤害类型波：
1. 在 `GetColor()` 中添加新类型 case
2. 在 `targetScale` 的 `damageType switch` 中添加 case（无 prefab 时）
3. 在 `isTravel` 三元表达式中决定 Travel/Fixed 模式
4. 若为 Fixed 模式，添加错开常量
5. 创建对应 prefab 变体，通过 `AttackSystem` 的 `prefab` 参数传入
