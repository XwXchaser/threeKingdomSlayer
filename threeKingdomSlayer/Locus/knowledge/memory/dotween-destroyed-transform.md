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

## 2026-10-29 补充：场景重载后的残留 tween（实战排查）

- **现场**：玩家阵亡 → 点「返回主菜单」→ `StageController.GoToMainMenu()` 把 Battle 场景卸载并重载 →
  下一帧抛 `MissingReferenceException: The object of type 'Transform' has been destroyed`，
  栈固定为 `Sequence.ApplyInternalCycle → Vector3Plugin.EvaluateAndApply → Transform.set_localScale`。
  （日志位置：`C:/Users/Administrator/AppData/Local/Tuanjie/Editor/Editor.log`；Unity 的 Console 面板不显示这条。）
- **审计方法**：把全项目 `.DOScale(` 调用点列全（14 处文件），逐个查「谁创建 sequence / 谁在销毁时杀它」→ 发现两处**确凿泄漏**：
  1. `Assets/Scripts/UI/BattleHUD.cs` 的通关印章序列是**局部变量**（`var seq = DOTween.Sequence();`），`OnDestroy` 里一句 `Kill` 都没有 → 印章是运行时 `Instantiate` 到相机下的，场景重载时先被销毁 → 已修：改为字段 `_victorySequence` + `OnDestroy` 杀。
  2. `Assets/Scripts/QTE/QTEDisplay.cs` 的 `_resultFeedbackTween`（QTE 结果反馈，运行时 new 出来的对象挂在 HUD 的 QTE 指示区下）`OnDestroy` 不杀 → 已修。
- **兜底**：`Assets/Scripts/Core/TweenLeakGuard.cs`（2026-10-29 新建）——
  每帧在 **DOTween 求值之前**（`[DefaultExecutionOrder(-1000)]`）扫一遍活跃 tween，杀掉「目标已销毁」的；
  序列还要看**嵌套 tween 的 target**（序列自身的 target 常常是 null）；另外在 `SceneManager.sceneUnloaded` 再扫一次。
  命中时打印来源（目标类型 + 名字），下次再犯就能直接定位到是哪条链路漏了清理。
  验证：合成探针（`DOScale` / `Sequence+Append(DOScale)` → 销毁目标）→ 守卫杀掉后 **Console 0 error**。
- **本版 DOTween 的 API 实测（踩过的坑）**：
  - `Tween` 只暴露属性；`Kill(bool)` / `IsActive()` 是 **`TweenExtensions` 的扩展方法**（没有 `using DG.Tweening;` 的临时脚本里调不到）。
  - `Sequence.Append/Join` 在 **`DG.Tweening.TweenSettingsExtensions`**，不在 `TweenExtensions`。
  - 反射句柄：`DG.Tweening.Core.TweenManager._activeTweens`（`Tween[]`）、`TweenManager.totActiveTweens`（int）、`DG.Tweening.Sequence.sequencedTweens`（`List<Tween>`，private）。
  - `tween.target` 的静态类型是 `object` → **Unity 假 null 判断必须写成** `target is UnityEngine.Object uo && uo == null`，
    直接 `target == null` 是引用比较，**抓不到已销毁对象**。
- **复现手法（可复用）**：新建 `RectTransform` → `DOScale(rt, …)`（或 `Sequence` 里 `Append` 一个 `DOScale`）→
  `Destroy` 目标 → 下一帧必抛；想同一帧复现就用 `DestroyImmediate`。

## 2026-11-09 补充：target 共享 → 清理位移 tween 连带杀掉攻击 tween（1051 射箭哑火根因）

- **现场**：1051 弓手在「拉弓等待发箭」的 2 秒窗口里，箭**没发出来**，但**射击动作动画照常走完**（用户原话：「准备射箭时动画走完，但没有箭射出」）。
- **机制（三条合起来才成立）**：
  1. 远程箭**没有兜底**：它由攻击 tween 的回调在 `spawnDuration=2s` 后发出（`Assets/Scripts/Enemy/Enemy.Attack.cs` 的 `AppendInterval(spawnDuration)` + `AppendCallback(SpawnProjectile)`）。
     攻击 tween 一旦在这 2 秒内被杀掉，那条回调**静默消失** → 箭永远不会发（`Enemy.HitFrame.ResolvePendingHitFramesOnAttackEnd` 对空 `hitFrames` 直接 return，不补发）。
  2. **攻击 tween 的 DOTween target 就是敌人的 Transform**（`_attackTween.SetTarget(transform)`）→
     于是代码里所有「清理位移 / 缩放 tween」的调用 —— `transform.DOKill(false)` / `DOTween.Kill(transform, false)` ——
     **都会连带把攻击 tween 一起杀掉**。实测命中点：`CancelAttack()`（受击/招架）、`ResetMovementState()`（补齐/移动重置）、`Stun()`、`Launch()`、死亡清理。
     其中 **`Stun()` 与 `ResetMovementState()` 不碰 Animator** → **动画照常走完、箭没了**，与用户描述完全一致。
  3. Animator 由 Animator 独立驱动，DOTween 杀它管不着 → 所以观感是「动作走完但没箭」。
- **修复方向（任一即可根治）**：给攻击 tween 一个**独立 target**（不要用 Transform）→ `DOTween.Kill(transform)` 再也碰不到它；
  或给远程路径加「收尾兜底补发」（tween 结束而箭未发出就补发一次）。
- **✅ 已修（2026-11-09）**：两件都做了。`Assets/Scripts/Enemy/Enemy.Attack.cs` 里 `_attackTween.SetTarget(transform)` → **`SetTarget(this)`**
  （近战翻转的 `DOScaleX` 子 tween 同样改用 `this`），并新增 `CheckRangedArrowFallback()`（每帧轮询「箭还待发 且 攻击 tween 已死」→ 补发一次）。
  ⚠️ **改了 target 就必须补显式杀**：原来靠 `transform.DOKill()` 顺带清掉攻击 tween 的地方
  （`Enemy.Pool.cs` 回池、`Enemy.Init.cs` 重置、`Enemy.Combat.cs` 取消死亡动画、`Enemy.Boss.cs` Boss 暂停）
  都要加一行 `_attackTween?.Kill(false)`，否则攻击 tween 会漏进对象池。
  验证：机器人跑 E0，**远程起手 12 = 箭已发出 9 + 兜底补发 3**，全关打通，0 error。
- **诊断陷阱 ①：OnKill 回调不是 Kill 的瞬间触发**。实测 `transform.DOKill(false)` 杀掉攻击 tween 时，
  OnKill **没有当场触发**；它延后到该 Sequence 对象被 DOTween 池回收后**再次被 Kill** 时才触发
  （现场栈顶是 `DamageNumber.SafetyTimeoutRealtime → animSeq.Kill()`，与真实凶手无关）。
  → **靠 OnKill 的调用栈找不到真凶，必须在每个 Kill 点打标签**（本项目已加 `_attackTweenKillReason`）。
- **诊断陷阱 ②：DOTween 会池化复用 `Sequence` 对象**。实测：`DOTween.Sequence()` 两次不是同一对象，
  但 **Kill 掉之后再取就拿到同一个对象**（`ReferenceEquals == true`）→ 被回收的对象**仍带着旧的 OnKill 回调**，
  会在下一次被 Kill 时**串台**触发别人的回调。
  → 任何挂在 tween 上的诊断回调**必须带序号护栏**（本项目用 `_attackTweenSerial`，序号不匹配就忽略），
  否则会报出「凶手是 DamageNumber」这种假线索。
- **判定「tween 真的死了」的可靠办法**：不要靠 OnKill 的时机，靠**业务状态**（例如「箭是否已发出」标志位）配合**在 Kill 点打的标签**。

