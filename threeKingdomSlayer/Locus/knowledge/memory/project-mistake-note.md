---
id: kd_4a9116b1-c70a-4de3-8eeb-801deb71c4fe
injectMode: inherit
summary: 更新至 2026-03 — 新增 TimedArrow 命中与视觉生命周期、随机轨迹、DOTween 清理、受击缩放、序列化迁移、视觉/伤害解耦及 Time 被动类型区分经验
aiMaintained: true
maintenanceRules: |-
  - Keep only durable and reusable project memory
  - Consolidate duplicates or conflicts into the latest conclusion
  - Remove temporary context, one-off tasks, and unsupported guesses
---

### Parry 架势规则误恢复导致普通敌人连续招架后停止攻击 ✅ 已修复
- 症状：连续 Parry 普通敌人多次后，敌人架势从 50 逐步降到 0，进入 `Stunned`；眩晕结束后普通敌人回到 `Idle`，位于 row=0 时没有移动事件重新调用 `StartAttacking()`，之后不再攻击。
- 根因：`Enemy.TakePoiseDamage()` 只检查 `state == Attacking` 和 `isAttackDrawPhase`，没有限制 `isBoss`，导致已经停用的"普通敌人架势/眩晕"机制重新生效；同时没有要求 `isAttackAnimating`，所以普通敌人在攻击冷却阶段也能被 Parry 持续削架势。
- 修复：
  1. `TakePoiseDamage()` 首先拒绝非 Boss；
  2. 仅在 Boss `InCombat`、`state == Attacking`、`isAttackAnimating == true` 且不处于 `AttackDraw` 时削架势；
  3. 普通敌人保留原有 Parry 攻击打断，但不再因 Parry 累计架势或进入 Stunned。
- 预防规则：**架势/眩晕是 Boss 专属机制，任何通用 Enemy 方法都必须明确区分 `isBoss`；Parry 架势伤害必须要求实际攻击动画前摇状态，不能只依赖 `state == Attacking`，因为该状态覆盖攻击冷却阶段。**
- 文件：`Assets/Scripts/Enemy/Enemy.cs`

### PlayLaunchVisual 变量声明顺序错误导致编译失败 ✅ 已修复
- 症状：`windupDistance`、`sideRatio`、`riseDistance` 在声明前被使用（CS0841），导致 `PlayLaunchVisual` 无法编译。
- 根因：重构枪尾支点模型时，将变量计算行放在 camera 向量行之后，但 windupPos/apexPos 计算仍未迁移，留在声明前引用。
- 修复：将 `windupDistance`/`sideRatio`/`riseDistance` 声明移到 camera 向量和轨迹计算之前。
- 文件：`Assets/Scripts/Player/AttackSystem.cs`

### Launch 蓄势偶发诡异绕转 ✅ 已修复并验收
- 症状：Launch 发动时，接管蓄力武器 pose 后，Windup 阶段低概率出现不符合既定动作的绕远或翻转；连续测试后修复版本暂未复现。
- 根因：目标姿态先通过 `.eulerAngles` 拆成欧拉角，再交给 `DOLocalRotate(..., RotateMode.Fast)` 插值。实时蓄力 pose 可能接近欧拉角环绕或非唯一表示区间，同一四元数会被拆成差异很大的欧拉角组合；随机倾角完整参与 Windup 又放大了异常。
- 修复：
  1. Windup 改为自定义 DOTween 进度，由 `Quaternion.SlerpUnclamped(startRotation, windupRotation, t)` 直接插值；
  2. Windup 位移与旋转由同一进度同步驱动；
  3. 随机倾角在 Windup 仅应用 12%，完整随机倾角延后到上挑终态。
- 预防规则：**从实时世界 pose 接管的武器动画不得将目标 Quaternion 转为 Euler 后做 Tween；跨对象/跨坐标系旋转衔接优先使用 Quaternion Slerp，并限制随机姿态在过渡前段的参与量。**
- 文件：`Assets/Scripts/Effects/LaunchVisualEffect.cs`
