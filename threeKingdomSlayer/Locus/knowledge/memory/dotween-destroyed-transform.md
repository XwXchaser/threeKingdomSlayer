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
- **`Component.DOKill()` 在目标已销毁时会因 Unity 假 null 直接 return，静默失败**；要杀这类 Tween 必须用原始托管引用调 `DOTween.Kill(targetRef, false)`（实测：对已销毁 Transform 调用能杀掉 1 个 Tween，且 `Kill(null)` 安全返回 0）。
- 本项目已按此改造：`TimedArrowEffect`（追踪生成的箭矢子对象，OnDestroy 逐个 `DOTween.Kill`）、`StabSweepEffect.Feedback`、`SweepEffect`（OnDestroy 改用原始引用 Kill）。
- 回调：`OnComplete` 可以销毁自然播放完成的对象；不要在 `OnKill` 中再次 `Destroy`，外部清理过程中会造成 DOTween 回收重入和 `IndexOutOfRangeException`。
- 战斗时序：三选一只暂停 `Time.timeScale`，已开始的视觉与 ULT 都应冻结并在恢复后继续；真正结束战斗才取消 ULT。路线奖励等待是软结束，普通死亡/火焰/箭雨等应自然结束；玩家确认离开节点、重开或退回菜单才允许硬清理。
- 暂停冻结的落地实现见 `memory/pause-gate-implementation.md`，规则见 `design/pause-freeze-rule.md`。
