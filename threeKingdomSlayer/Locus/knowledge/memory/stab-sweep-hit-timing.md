---
id: kd_ee05f31e-c48e-4497-b777-8dc9e4165a03
type: memory
path: stab-sweep-hit-timing.md
title: stab-sweep-hit-timing
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1784022845319
updatedAt: 1784026276942
---

# stab-sweep-hit-timing

## Summary
Stab 空挥、枪尖时序命中、递减视觉超程与层级策略。

<!-- locus:body:start -->
- `AttackSystem.ExecuteStab` 现在始终生成 `StabSweepEffect`，所以无目标时也会播放戳击并进入原有动作锁定/冷却。
- `StabSweepEffect` 在刺出阶段，以 `Stab.prefab` 渲染器 bounds 的前端 Z 位置扫描指定列；只命中 `rangeRows`（含 Stab 范围升级）内、仍存活且已应战的敌人，每个敌人每次挥击最多一次。先缓存命中候选，再结算 `TakeDamage`，避免敌人死亡重排 `column.enemies` 时修改遍历集合。
- 刺击视觉终点额外向 -Z 延伸 `1 / rangeRows` 排：range 1 额外一排，随范围递减；命中范围仍严格保持原 rangeRows。
- `Stab.prefab` 实例化后将其 SpriteRenderer `sortingOrder` 设为 10，使枪在 Default Sorting Layer 中显示于敌人前方，不改变全局 Sorting Layer/Z 深度策略。
- 空挥不触发能量或 `OnAttackPerformed` 被动计数；首次实际命中时才触发二者。命中收招后才执行 Stab 击退波。
- Tap 输入无可选敌人时回退到固定五列屏幕映射；长按 Pierce 仍保持原来的目标依赖。
- 相关文件：`Assets/Scripts/Player/AttackSystem.cs`、`Assets/Scripts/Attack/StabSweepEffect.cs`、`Assets/Scripts/Player/InputManager.cs`。
<!-- locus:body:end -->
