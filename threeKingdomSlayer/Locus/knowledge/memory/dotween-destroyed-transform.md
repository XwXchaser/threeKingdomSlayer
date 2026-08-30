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
- 回调：`OnComplete` 可以销毁自然播放完成的对象；不要在 `OnKill` 中再次 `Destroy`，外部清理过程中会造成 DOTween 回收重入和 `IndexOutOfRangeException`。
- 战斗时序：三选一只暂停 `Time.timeScale`，已开始的视觉与 ULT 都应冻结并在恢复后继续；真正结束战斗才取消 ULT。路线奖励等待是软结束，普通死亡/火焰/箭雨等应自然结束；玩家确认离开节点、重开或退回菜单才允许硬清理。
