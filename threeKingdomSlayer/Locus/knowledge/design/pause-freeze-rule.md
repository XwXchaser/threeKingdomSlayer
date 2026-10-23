---
id: kd_453b3a9c-7f01-41c3-8459-5d72783bf955
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 暂停冻结规则：游戏内容冻结、UI 继续运行

用户确认的方向（2026-10-23 交接，2026-10-24 落地）：当 `Time.timeScale = 0`（升级三选一、暂停菜单、对话、教学引导、选关 overlay）时，
**任何游戏内容都应冻结**，只有 UI 保持运行、仍可交互。

## 为什么需要专门处理

- 普通 DOTween Tween（`SetUpdate(false)`）与普通 Animator（`updateMode = Normal`）本来就跟 `timeScale` 一起停，无需处理。
- 显式 `SetUpdate(true)`（unscaled）的 Tween **不会**跟着停：敌人死亡动画、伤害数字、镜头反馈、像素打击特效、攻击波飞行都会继续播放。
- 读 `Time.unscaledDeltaTime` / `WaitForSecondsRealtime` 的**游戏内容**协程也不会停：卡肉计时、骑兵死亡演出、109 战马逃逸、Pierce 系特效、序列帧特效。

实测（2026-10-24，Play Mode）：`timeScale = 0` 持续 60 帧，普通 Tween 位移 0.0000（已冻结），unscaled Tween 位移 1.0730（仍在跑）。

## 冻结 / 不冻结的分类

| 类别 | 处理 |
| --- | --- |
| 敌人死亡 Tween（`DeathBounceAndFall` / `LaunchDeathEffect`） | 冻结 |
| 攻击波飞行（`AttackWave.travelSeq`） | 冻结 |
| 伤害数字（`DamageNumber.animSeq`） | 冻结（是战斗反馈，不是面板 UI） |
| 镜头震动（`CameraFeedbackController`） | 冻结（三选一期间镜头不抖） |
| 像素打击特效（`PixelHitEffectManager`） | 冻结 |
| 序列帧特效淡出（`SpriteSequenceEffect`，`useUnscaledTime=true`） | 冻结 |
| 卡肉计时（`EnemyHitFeedback`）、攻击 trace 等待 | 冻结 |
| 骑兵 / 109 战马 / Pierce 系演出 | 冻结 |
| 三选一弹窗、HUD、QTE 面板、对话、教学手势提示、奖励弹窗 | **不冻结**，继续按 unscaled 走时，保证可交互 |

## 实现

集中到 `GameplayPause`（`Assets/Scripts/Core/GameplayPause.cs`），由 `GameplayPauseWatcher` 依据 `Time.timeScale` 自动同步，
调用点不需要各自维护暂停开关（避免漏改）。

- 游戏内容的 unscaled Tween 用 `GameplayPause.Bind(tween)` 登记，暂停时 `Pause()`，恢复时 `Play()`。
- 游戏内容的计时改读 `GameplayPause.UnscaledDelta`（暂停时为 0）与 `GameplayPause.UnscaledTime`（扣除暂停时长的单调时钟）。
- 需要「等一段时间但暂停时不走时」的协程用 `GameplayPausableWait` 代替 `WaitForSecondsRealtime`。
- 不要用 `DOTween.PauseAll()`：会把 UI 一起冻住。

## 验收口径

1. `timeScale = 0` 期间：敌人死亡动画的 Y 位移与旋转**完全不变**（实测 dY = 0.0000，rotZ 不变）。
2. 恢复 `timeScale = 1` 后：同一段死亡动画从原处继续（实测 Y 0.541 → 1.231）。
3. 同期未登记的 unscaled Tween（UI）仍继续推进（实测位移 1.0730）。
4. Console 无 error / warning。
