---
id: kd_1ed8c882-4ea1-44f9-a45a-88971022a2a3
summary: 指向突刺（stabC2）教学拍第三版交接（2026-11-08，**Play Mode 实测跑通**）：只冻敌人不冻全局的原因、冻结必须落在可打断前摇（阈值 1.5s）、必须打断的判据、本轮修掉的 5 个实测 bug（阶段号未初始化/命中不看对象/收拍未登记/冻结阈值过晚/每帧回中立）、实测证据表、待用户手按的瞄准验收、复跑方式与工作区红线。
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 指向突刺（stabC2）教学拍 —— 交接（第三版已跑通，2026-11-08）

> **状态：Play Mode 实测跑通（机器人端到端）**。剩下**只有一件事**要用户手按验收：真实手指「按住蓄满 → 按在敌人身上松手」的瞄准手感。
> 落点：`TutorialStageDirector.Hints.cs`（拍流程）/ `.Dialogue.cs`（命中分流）/ `.MoveGuide.cs`（示范轨迹）/ `TutorialStageDirector.cs`（阈值）/ `Enemy.cs`（挂起来源）。

---

## 1. 这一拍现在长什么样（已实测）

```
阶段 1  闸门 Tap → 玩家点一下，**必须命中目标**（1011）→ 命中才进阶段 2
阶段 2  ① 等接续窗口打开（IsWindowOpenFor(Hold,1)）→ 冻结招式状态机的时钟（窗口常开）
        ② 等目标抡起霸体招、抬手到顶（可打断的前摇）→ **只冻敌人**
        ③ 玩家按住蓄满 → 把手指按在 1011 身上松手 → 派生招式（指向突刺）
        ④ **必须打断**（attackInterrupted=true）才算过；打中但没打断 → 回阶段 1 重来
兜底    8 秒内没打断 → 解冻 + 回阶段 1（不卡死）；目标被打死 → 换目标
```

**实测证据（正式 N1，row0 = 1011，机器人驱动）**：

| 检查项 | 结果 |
|---|---|
| 阶段 1 命中 | hp 200→170，phase 1→2，闸门 Tap→ComboHold ✓ |
| 接续窗口 | `machineFrozen=True`、`windowOpen=True` ✓ |
| 抬手冻结 | `suspended=True`、`attacking=True`、`windup=True`、`strikeWindow=False`、`draw=False`、`cFrame=True`、`toEnd=1.49` ✓ |
| 世界照常走 | `timeScale=1`（全程）✓ |
| 松手打断 | `[Enemy] 打断攻击成功: #1(1011)`，`interrupted=True`，hp→135 ✓ |
| 收拍 | `beatActive=False`、闸门复位、存档写入 `taughtMoveGuides=talent_move_stabc2` ✓ |
| 收尾 | 敌人 `suspended=False`（没有永久僵住）✓ |
| 超时兜底 | 8 秒没打断 → 解冻 → 回阶段 1 ✓（实测触发过一次，流程正常） |

---

## 2. 关键设计决定（别再改回去）

### 2.1 **只冻敌人，不冻整个游戏** ✗ 整局 `timeScale=0`

海浪/格挡拍用的是「整局冻结 + `allowInputWhileTimeFrozen`」，**这一拍不能照抄**：
戳击视觉门（`_stabVisualTimer`）、接续窗口（`NormalizedTime`）、蓄力条（`HoldDurationSeconds`）全走**缩放时间** → 冻住就永不过期 → 玩家第二下/松手出不来 → 整拍死等。
⇒ 世界照常走，只把敌人的动作停住：`Enemy.PushSuspend(SuspendSource.Tutorial)`（新增枚举值 4）→ 暂停攻击 tween + 停 Animator → 停在抬手那一帧；其余敌人 `tutorialHold` 按住。

### 2.2 冻结必须落在「**可打断的前摇**」里

判据（2026-11-08 实测修）：`target.IsInAttackWindup && !target.IsInStrikeWindow() && TimeToAttackEnd() <= comboWindupFreezeSeconds`。
- 阈值 **0.35s → 1.5s**（口径同霸体防御拍 `defendPromptToEndSeconds` / 技能打断拍 `skillPromptToEndSeconds` = 「武器举到位、刀还没落」）。
- 0.35s 会冻在**出伤/收招段**（`isAttackDrawPhase=true` → 那一段一律打不断）→ 玩家打中了却「没打断」→ 永远过不去。
- 加 `IsInAttackWindup` / `!IsInStrikeWindow()` 两个判据 = 保证冻结瞬间这一刀**还打得断**。

### 2.3 判据必须是「打断」

`CancelAttack()` 里的 `attackInterrupted=true` 是唯一可靠信号；「打中」不算。
打断规则（`Enemy.Combat.cs`）：`inArmorWindow`（`isCFrame`）时需要 `canInterruptCFrame || isHeavyAttack` → **指向突刺是重攻击** ✓ 所以能破霸体。

### 2.4 状态机的「回中立」每轮只做一次

`machine.NotifyExternalReset()` 会 `ExitToNeutral()` → `_currentMove=null`。
**每帧调**会把玩家刚打出的 Jab1 立刻清掉 → 连段没了 → 派生招式接不出来。只能在**进入阶段 1 时调一次**。

---

## 3. 本轮修掉的 5 个 bug（都是实测撞出来的）

| # | Bug | 现象 | 修法 |
|---|---|---|---|
| 1 | `_comboChargePhase` 没初始化成 1 | 第一次命中掉进通用「打中即完成」→ 拍瞬间收掉、还被写成「已教过」→ 之后再也不出现 | `HintRoutine` 进连招拍时置 1；`OnBeatEnemyDamaged` 加阶段守卫 |
| 2 | 阶段 1 命中不看对象 | 打到别的敌人也算「学会瞄准」 | 阶段 1 必须打在 `_beatTargetEnemy` 身上才推进 |
| 3 | 打断成功后没走收拍登记 | 存档不写「已教过」→ 每场都重教 | 阶段 2 打断成功 → `RegisterBeatHit()` → `_beatCompleted=true` → EndBeat 写存档 |
| 4 | 冻结阈值 0.35s | 冻在收招段 → 打不断 → 永远过不去 | 阈值 1.5s + 可打断前摇两个判据（见 §2.2） |
| 5 | 每帧 `NotifyExternalReset` | 连段被清空 → 派生招式出不来 | 每轮只做一次 |

附带：`GuideDemoPath` 给 `ComboCharge` 补了「点/蓄」轨迹（原来落到默认分支 → 示范画成横划，和这一拍要教的动作不符）。

---

## 4. 还剩什么没验（要用户手按）

机器人只能模拟「合成手势」，**做不出真实的「按住蓄满 → 按在敌人身上松手」**（它得手动补 `InputManager.comboChargeActive = true`，真实玩家按住时由状态机 `UpdatePointerHold` 置位）。
所以请用户在正式关实机走一遍：

1. 解锁指向突刺后进 N1 → 台词出现 → 点一下 1011（要打中它）。
2. 看到「他抡招了 → 冻结」：敌人举着刀停住、后排敌人不动 ✓。
3. **手指按在 1011 身上按住** → 蓄满 → **松手**（手指要落在 1011 身上 = 瞄准）→ 派生招式打断它 → 收拍。
4. 若手指不在敌人身上松手 → 武器退回收蓄力漂浮位、要重新蓄力（招式自身既有行为，不是 bug）。

---

## 5. 工作区红线（全是用户并行 WIP，**禁止提交/回退**）

`Enemy_1011/1012/1013/105/106_Fixed/109.prefab`、`Formal_E0/N1/N2/N3_Battle.asset`、`guide_move_stabc2*.asset`、`tutorial_t0_*.asset`、`Zhangfei_Jab2/Jab3.asset`、`WaveMarchScheduler.cs`、`CavalryEnemy.cs`、`Enemy.Visuals.cs`、`design/cavalry-enemy.md`、`Editor/ConfigTable*.cs`、`config-tables.xlsx`、`Tools/build|apply_config_workbook.py`。

（本轮只提交：`Assets/Scripts/Enemy/Enemy.cs`、`Assets/Scripts/Tutorial/TutorialStageDirector{,.Hints,.Dialogue,.MoveGuide}.cs`、`Assets/Scenes/Battle.scene`（只多序列化了几個新字段 + `comboWindupFreezeSeconds=1.5`）、本计划文档。）

---

## 6. 复跑方式（Play Mode）

1. 存档前提：`unlockedTalents` 含 `talent_move_stabc2`、`taughtMoveGuides` 为空（正式 N1 的 row0 = 1011）。
2. Play Mode → `StageController.Instance.StartRouteBattle(Formal_N1_Battle)` → 敌人站定后导演会自己起「招式解锁引导拍」。
3. 机器人手势：`InputManager.Instance.SubmitGestureForDebug(MoveGesture.Tap, 0, 目标列, 屏幕坐标)`；松手用 `MoveGesture.Hold, chargeLevel=1`，**并先手动置 `input.comboChargeActive = true`**。
4. 测完**还原存档**：`Library/Locus/tmp/player_save_backup_robot_20261107.txt` → `PlayerPrefs.SetString("player_save", json)` + 清掉 `SaveManager._cache`（否则读到旧缓存）。
