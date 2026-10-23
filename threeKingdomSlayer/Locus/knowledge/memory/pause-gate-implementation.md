---
id: kd_743bdb6a-8fc9-4bcb-a18d-2dc306354474
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# GameplayPause 暂停门（实现备忘）

设计规则见 `design/pause-freeze-rule.md`。这里记录实现细节与踩过的坑。

## 三个文件

- `Assets/Scripts/Core/GameplayPause.cs`（Locus.Combat 层，静态类）
- `Assets/Scripts/Core/GameplayPauseWatcher.cs`（MonoBehaviour，运行时自建，DontDestroyOnLoad）
- `Assets/Scripts/Core/GameplayPausableWait.cs`（CustomYieldInstruction）

## API

- `GameplayPause.IsPaused` —— 当前是否 timeScale == 0。
- `GameplayPause.UnscaledDelta` —— 游戏内容用步进，暂停时为 0。
- `GameplayPause.UnscaledTime` —— 扣除所有暂停时长的单调时钟（等价 `Time.unscaledTime` 但不包含暂停段）。
- `GameplayPause.Bind(tween)` —— 登记**游戏内容**的 unscaled Tween；UI 不要登记。
- `GameplayPause.SetPaused(bool)` —— 由 watcher 调用，一般不手动调。
- `new GameplayPausableWait(seconds)` —— 暂停感知等待，替代游戏内容里的 `WaitForSecondsRealtime`。

## 坑

- **`CustomYieldInstruction` 没有 `OnUpdate()`**（Unity 2022.3 实测：只有 `keepWaiting` / `Current` / `MoveNext` / `Reset`）。
  所以 `GameplayPausableWait` 只能用 `keepWaiting` 属性，用 `GameplayPause.UnscaledTime` 做计时基准，不要试图 override `OnUpdate`。
- **DOTween 的 `Pause` / `Play` / `IsPlaying` / `IsActive` 都是扩展方法**（`DG.Tweening.TweenExtensions`），
  写在 `DG.Tweening` 命名空间下才能直接调用；用 unity_execute 写片段时要写全名 `DG.Tweening.TweenExtensions.IsPlaying(t)`。
- **DOTween 完成后 `IsActive()` 变 false**（实测），所以用 `IsActive()` 做登记列表的清理条件。
- 暂停门接管的是「自己 Pause 过的」Tween：卡肉等逻辑已经 Pause 的 Tween 不接管，恢复时交回原逻辑，避免双重恢复打架。
- 恢复时如果 Tween 的 target 已被销毁，先 `Kill` 再跳过，不要 `Play`（会访问已销毁对象）。
- watcher 用 `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` 自建，`Awake` 里 `GameplayPause.Reset()`，
  防止关掉 Domain Reload 时静态状态跨局残留。

## 已登记的 Tween / 已改计时的位置（2026-10-24）

Tween 登记：敌人死亡（`Enemy.Combat`）、攻击波飞行（`AttackWave` ×3）、伤害数字（`DamageNumber`）、
镜头反馈（`CameraFeedbackController`）、像素打击（`PixelHitEffectManager`）、序列帧淡出（`SpriteSequenceEffect`）。

计时改读 `GameplayPause`：`AttackWave`、`SweepEffect`、`StabSweepEffect`、`CameraFeedbackController`、
`EnemyHitFeedback`、`Enemy.Attack`（trace 等待）、`EnemyOutline`、`CavalryDeathSequence`、`MountEscapeVisual`、
`PierceAimIndicator`、`PierceVortexStreamVisual`、`PierceVortexVisual`、`SpriteSequenceEffect`、`BreakableProp`。

**故意没改**：UI 全部（三选一弹窗、HUD、QTE、对话、教学手势、ElementalBuffHUD、BuffIcon）、
`UI/CameraManager`（场景过场，不是战斗内容）、`MeshWeaponMangaSweep`（本来就靠 `Time.deltaTime <= 0` 冻结）。
