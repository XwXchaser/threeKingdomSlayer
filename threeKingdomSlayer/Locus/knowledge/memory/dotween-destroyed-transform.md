---
id: kd_a2c9dc76-e50e-44a4-9726-80ae2ab41b7d
injectMode: inherit
summary: 攻击视觉 Tween 访问已销毁 Transform 的根因与清理规则。
aiEditMode: inherit
---

- 症状：攻击视觉对象被销毁后，DOTween 仍在下一帧访问已销毁 Transform，Unity 抛出 MissingReferenceException，可能导致游戏被强制暂停。
- 根因：Stab 的命中脉冲 Tween 独立于主 Sequence，销毁时未显式 Kill；Sweep 的独立 scaleIn/视觉路径 Tween 也未统一清理。SetTarget 不会让 Unity Destroy 后的 Transform 自动安全。
- 修复：Stab 在 OnDestroy、Sequence OnKill、OnComplete 清理视觉 Tween；Sweep 缓存视觉 Transform 与视觉路径 Transform，在 OnDestroy 清理；scaleIn 显式设置 target。
- 规则：任何独立 Tween 若访问 Transform 或 SpriteRenderer，必须在对象销毁前按**创建时同一 target**显式 Kill；`SetTarget` 不会让 Unity Destroy 自动安全。
- 动态子特效：父对象销毁时必须枚举其动态子对象，并按子对象创建 Tween 时使用的 `GameObject` / `Transform` target 执行 `DOTween.Kill`；只调用 `childTransform.DOKill()` 无法终止 target 设为 `childGameObject` 的 Tween。
- **DOTween 不会自动杀掉目标已销毁的 Tween**（2026-10-24 实测：对象 Destroy 后 Tween 仍在下一帧访问已销毁 Transform，抛 MissingReferenceException）。
- **最小复现（2026-10-24）**：新建 GameObject → `t.DOMove(end, 3f)` → `Destroy(go)` → 下一帧必抛。
  同一机制的两种栈形态：动画**启动帧**是 `Transform.get_position`（`DOMove` 的 getter），
  动画**中途**是 `Transform.set_position`（setter）—— 看栈里是 get 还是 set 只代表出错的时刻，根因相同。
  对照组：先 `DOTween.Kill(t, false)`（用原始引用）再 Destroy → **不报错** ✅
- **`Component.DOKill()` 在目标已销毁时会因 Unity 假 null 直接 return，静默失败**；要杀这类 Tween 必须用原始托管引用调 `DOTween.Kill(targetRef, false)`（实测：对已销毁 Transform 调用能杀掉 1 个 Tween，且 `Kill(null)` 安全返回 0）。
- 本项目已按此改造：`TimedArrowEffect`（追踪生成的箭矢子对象，OnDestroy 逐个 `DOTween.Kill`）、`StabSweepEffect.Feedback`、`SweepEffect`（OnDestroy 改用原始引用 Kill）。
- **2026-10-24 补修四处**（本轮报错的排查结论）：
  1. `Enemy.OnDestroy`：`transform.DOKill(false)` → 改为 `DOTween.Kill(_destroySafeTransform, false)`
     （`_destroySafeTransform` 在 `Enemy.Init.Awake` 缓存的**原始引用**）。
     敌人的销毁（死亡/回收）走这条路径，**之前那次 Kill 一直是静默失败的** → 敌人身上任何还活着的
     Tween 都会在下一帧访问已销毁的 Transform → 正是用户看到的报错。
     第二波刷怪 + 补齐时敌人大量出生/前移/死亡 → 触发概率显著升高（与现场观察一致）。
  2. `EnemyFreezeVisual.SpawnProceduralIceBreakFx`：碎片/闪光的 Tween target 是**子 GameObject**，
     销毁 root 前先逐个 `DOTween.Kill(child.gameObject, false)`（`childTransform.DOKill()` 杀不掉）。
  3. `ShootFireEffect.OnDestroy`：粒子是子对象，其 Tween target 是粒子 GameObject → 销毁前逐个 Kill。
  4. `QTEController`：备用弹道（无 QTEProjectile 组件的 prefab）的 `DOMove` 显式 `SetTarget(transform)`，
     `KillProjectileSequence` 改为同时用 Transform 引用与 GameObject 引用 Kill。
- **审计结论**：其余 `DOMove` 调用点（`AttackWave` / `StabSweepEffect` / `SweepEffect` / `PierceReleaseVisual` /
  `DamageNumber` / `QTEProjectile` / `PassiveTriggerModule` / `TimedArrowEffect`）**都已有 destroy 时清理**。
- **验证方式（可复用）**：新建物体 → `t.DOMove(end, 3f)` → `Destroy(go)` → 下一帧必抛；
  先 `DOTween.Kill(t, false)`（原始引用）再销毁 → 不抛。用它对照即可确认某处是否真的漏了清理。
- 回调：`OnComplete` 可以销毁自然播放完成的对象；不要在 `OnKill` 中再次 `Destroy`，外部清理过程中会造成 DOTween 回收重入和 `IndexOutOfRangeException`。
- 战斗时序：三选一只暂停 `Time.timeScale`，已开始的视觉与 ULT 都应冻结并在恢复后继续；真正结束战斗才取消 ULT。路线奖励等待是软结束，普通死亡/火焰/箭雨等应自然结束；玩家确认离开节点、重开或退回菜单才允许硬清理。
- 暂停冻结的落地实现见 `memory/pause-gate-implementation.md`，规则见 `design/pause-freeze-rule.md`。
