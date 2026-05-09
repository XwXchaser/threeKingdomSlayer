# 开发日志

## 2025-12-16 — 补齐链断裂修复 & 缩放残留修复

### 概述
修复 Launch（挑飞）攻击杀死敌人后补齐链中断，以及多敌人快速受击时缩放永久变形两个 Bug。

### Bug 1: 挑飞击杀后补齐链中断

**根因**：`AttackWave` 以 0.04s/排 stagger 命中敌人。前排敌人先死 → `RemoveEnemy()` 为后方敌人设 `targetRow`，但后方敌人尚未被命中。0.04s 后命中 → `Launch()` 无条件重置 `targetRow = -1`。落地时 `UpdateLaunch()` 查不到 targetRow，走自然移动而非补齐。

**修复**：`Launch()` 不再重置 `targetRow`。`RemoveEnemy()` 设置的值保留到落地。

### Bug 2: 多敌人快速受击时缩放永久变形

**根因**：`DOTween.Kill("punchScale")` 是全局 Kill，会误杀其他敌人正在运行的 punch tween。敌人 A 的 punch 被敌人 B 的受击 Kill 打断，scale 停在中间振动值无法恢复。`Launch()` 中同样存在全局 `DOTween.Kill("punchScale")` / `DOTween.Kill("rushBounce")` 误杀问题。

**修复**：
- `TakeDamage`：punch tween ID 改为 per-instance `$"punch_{GetInstanceID()}"`
- `Launch`：移除冗余的全局 `DOTween.Kill("punchScale")` / `DOTween.Kill("rushBounce")`（`transform.DOKill(false)` 已覆盖本对象）

### 涉及文件
- `Assets/Scripts/Enemy/Enemy.cs` — `Launch()` 不重置 targetRow；`TakeDamage()` per-instance punch ID；`Launch()` 移除全局 DOTween.Kill

### 概述
完善 Parry 攻击的触发、打断、架势判定、眩晕规则，重构敌人攻击与架势系统。

### 改动明细

**招架触发**
- `InputManager.ProcessGesture`：无充能快速滑动，方向与垂直轴夹角 < `verticalSwipeThreshold` 时映射为 Parry
- `HeroConfig` 新增 `parryRangeRows`（默认 1）、`parryCooldown`（默认 0.5s）
- `PlayerState.GetCooldownDuration(AttackType.Parry)` 读取 `heroConfig.parryCooldown`

**招架命中逻辑** (`AttackSystem.ExecuteParry`)
- 遍历 `parryRangeRows` 排内所有存活敌人，造成 `parryDamage`（DamageType.Stab）和 `parryPoiseDamage`
- 打断判定：`parryPoiseDamage >= enemy.config.maxPoise` 且敌人处于 AttackSpawn 阶段 → `CancelAttack()` 返回攻击冷却
- 不满足打断条件：仅造成伤害+架势伤害，不打断不眩晕
- 架势破碎不再眩晕：`TakePoiseDamage` 仅重置 `currentPoise`
- Boss 眩晕：`CheckParryStunThresholds` 仅在 `isBoss=true` 时按血量百分比阈值触发 `Stun()`
- 减伤代码已注释，留作日后角色专属技能

**敌人攻击三阶段** (`Enemy.cs`)
- AttackSpawn（前冲+翻转，`isAttackAnimating && !isAttackDrawPhase`）→ 可被招架打断
- AttackDraw（收招返回，`isAttackDrawPhase=true`）→ 不可打断
- 冷却 → 可被链式补齐中断
- `isAttackAnimating` / `isAttackDrawPhase` 改为 public，供 AttackSystem 读取

**受伤抖动隔离**
- `TakeDamage` 中 punch scale tween 使用 `DOTween.Kill("punchScale")` + `.SetId("punchScale")`
- 不再使用 `transform.DOKill(true)` 避免误杀攻击动画 tween

### 涉及文件
- `Assets/Scripts/Core/HeroConfig.cs` — 新增 `parryRangeRows`、`parryCooldown`
- `Assets/Scripts/Enemy/Enemy.cs` — 攻击三阶段、CancelAttack、TakePoiseDamage、CheckParryStunThresholds、punchScale 隔离
- `Assets/Scripts/Player/AttackSystem.cs` — ExecuteParry 重写
- `Assets/Scripts/Player/PlayerState.cs` — Parry 冷却读 HeroConfig
- `Assets/Scripts/Player/InputManager.cs` — Parry 手势分支（已存在）
- `_summaries/Enemy.summary.md` — 更新接口与规则
- `_summaries/Player.summary.md` — 更新接口与规则
- `_summaries/Core.summary.md` — 更新 HeroConfig 字段
