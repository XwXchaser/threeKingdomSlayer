---
id: kd_ee05f31e-c48e-4497-b777-8dc9e4165a03
injectMode: inherit
summary: Stab 空挥、枪尖时序命中、递减视觉超程与层级策略。
aiMaintained: inherit
---

- `AttackSystem.ExecuteStab` 现在始终生成 `StabSweepEffect`，所以无目标时也会播放戳击并进入原有动作锁定/冷却。
- `StabSweepEffect` 在刺出阶段，以 `Stab.prefab` 渲染器 bounds 的前端 Z 位置扫描指定列；只命中 `rangeRows`（含 Stab 范围升级）内、仍存活且已应战的敌人，每个敌人每次挥击最多一次。先缓存命中候选，再结算 `TakeDamage`，避免敌人死亡重排 `column.enemies` 时修改遍历集合。
- `stab_rotate1.png` / `stab_rotate2.png` 是 Slash/Sweep 横扫使用的弯曲运动帧，包含左右弯曲形变，不适用于直线 Stab；普通直刺当前没有专用的“直刺运动/冲击”美术帧。
- `stab.png` 是普通直刺使用的完整静态武器图；`stab_charge1/2` 主要用于蓄力视觉及 Launch 接管序列，不能直接视为普通直刺帧。
- 空挥不触发能量或 `OnAttackPerformed` 被动计数；首次实际命中时才触发二者。命中收招后才执行 Stab 击退波。
- Tap 输入无可选敌人时回退到固定五列屏幕映射；长按 Pierce 仍保持原来的目标依赖。
- 相关文件：`Assets/Scripts/Player/AttackSystem.cs`、`Assets/Scripts/Attack/StabSweepEffect.cs`、`Assets/Scripts/Player/InputManager.cs`。
