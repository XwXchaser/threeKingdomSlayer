---
id: kd_4ca270d5-f20a-45f1-a78c-57b691e65160
summary: 指向突刺（stabC2）教学拍第四版交接（2026-11-08，**Play Mode 实测跑通**）：冻结统一为与霸体防御拍/技能打断拍/时停招架拍**同一套** FreezeWorldForTutorial（不再自造 SuspendSource.Tutorial）、蓄满在前时停在后、必须落在可打断前摇、实测证据表、本轮修掉的 4 个 bug、待用户手按的瞄准验收、复跑方式与工作区红线。
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 指向突刺（stabC2）教学拍 —— 交接（第四版：与霸体教学同一套冻结，2026-11-08）

> **状态：Play Mode 实测跑通（机器人端到端）**，冻结方式与霸体防御拍 / 技能打断拍 / 时停招架拍**完全同一套代码**。
> 剩下**只有一件事**要用户手按验收：真实手指「按住蓄满 → 按在敌人身上松手」的瞄准手感。

---

## 1. 冻结方式：只有一套，不再造轮子

用户 2026-11-08 反馈：「**并没有实现与霸体教学一样的冻结 / 不要乱造轮子**」。

⇒ 之前那版把「只挂起敌人」（自己新加的 `Enemy.SuspendSource.Tutorial`）当冻结 ✗：
只停住了敌人、世界照旧跑 → ① 玩家在等抬手时照样挨打；② 和另外三个教学拍的观感/机制不一致。

**现在统一成 `TutorialStageDirector.FreezeWorldForTutorial()` / `UnfreezeWorldForTutorial()`**（`TutorialStageDirector.cs`），
四个拍都走它：

| 拍 | 冻结点 | 用法 |
|---|---|---|
| 霸体防御拍（`DefendAtCAttackRoutine`） | 敌人抬手到顶 | `FreezeWorldForTutorial()` |
| 技能打断拍（`SkillSlotInterruptRoutine`） | 敌人抬手到顶 | `FreezeWorldForTutorial()` |
| 时停招架拍（`FreezeParryRoutine`） | 命中帧那一刻 | `FreezeWorldForTutorial()` |
| **指向突刺拍（`ComboChargeRoutine`）** | **敌人抬手到顶 + 玩家已蓄满** | `FreezeWorldForTutorial()` |

helper 的四个动作（与原来三处内联代码逐行等价）：
```csharp
Time.timeScale = 0f;                 // 世界停住：敌人停在抬手那一帧，玩家也不挨打
_frozenByTutorial = true;
AttackSystem.Instance?.ClearActionLock();   // 冻结时动作锁走不完 → 不清就发不出招
SetSceneDim(true);                   // 只压暗场景
input.allowInputWhileTimeFrozen = true;     // 冻结中玩家的手势通道照常工作
```
（`Enemy.SuspendSource.Tutorial` 已删除；输入层的 `useUnscaledHoldTime` 仍保持回退状态。）

---

## 2. 这一拍现在的流程（已实测）

```
阶段 1  闸门 Tap → 玩家点一下，**必须命中目标**（1011）→ 命中才进阶段 2
阶段 2  ① 等接续窗口打开 → `machine.SetFrozen(true)` 把窗口撑开（玩家不用抢那 0.2s）
        ② 玩家按住蓄力（**必须先蓄满**：时停后蓄力条不再涨）
        ③ 敌人抡起霸体招、抬手到顶（可打断的前摇）→ **整局时停**（同一套冻结）
        ④ 玩家把手指按在 1011 身上松手 → 派生招式（指向突刺）打出 → **解冻** → 伤害落地 → **必须打断**
兜底    8 秒没打断 → 解冻 + 回阶段 1（不卡死）；目标被打死 → 换目标
```

**实测证据（正式 N1，row0 = 1011，机器人驱动）**：

| 检查项 | 结果 |
|---|---|
| 阶段 1 命中 | hp 200→170，phase 1→2，闸门 Tap→ComboHold ✓ |
| 接续窗口 | `windowOpen=True`、`machineFrozen=True` ✓ |
| 时停 | `Time.timeScale=0`、敌人 `toEnd=1.50`/`windup=True`/`cFrame=True`（停在抬手最后一帧）、**玩家血量不掉** ✓ |
| 松手 | 派生招式打出（`fired=True`）→ 自动解冻（`timeScale=1`）✓ |
| 打断 | `[Enemy] 打断攻击成功: #1(1011), col=2` ✓ |
| 收拍 | `手势拍完成：Stab col=2`、`招式解锁引导完成：talent_move_stabc2`（存档标记已教过）✓ |
| 收尾 | `timeScale=1`、`frozenByTutorial=False`、`allowInputWhileTimeFrozen=False`、`machineFrozen=False` ✓ |

---

## 3. 这一轮修掉的 4 个 bug（实测撞出来的）

| # | Bug | 现象 | 修法 |
|---|---|---|---|
| 1 | 冻结阈值 0.35s | 冻在**出伤/收招段**（`isAttackDrawPhase` → 一律打不断）→ 打中了却「没打断」→ 永远过不去 | 阈值 1.5s（口径同霸体防御拍/技能打断拍）+ 必须 `IsInAttackWindup && !IsInStrikeWindow()` |
| 2 | 失败一轮后**招式状态机的时钟没解冻** | `SetFrozen(true)` 残留 → ① 接续窗口永不再开 ② **戳击视觉门永不过期** → 玩家下一记戳被吞 → 下一轮直接死 | 回阶段 1 的三条路径都补 `machine.SetFrozen(false)` |
| 3 | 阶段 2 认「任何一次命中」 | 多段攻击的前一段命中、或玩家抢跑打出来的那一下，都会把这一拍判成「没打断」→ 回阶段 1 | 阶段 2 只认**派生招式**（`_comboChargeAttackFired`）打出来的那一下 |
| 4 | `_comboChargePhase` 没初始化成 1 | 第一次命中掉进通用「打中即完成」→ 拍瞬间收掉、还被写成「已教过」 | 进连招拍时置 1 + 阶段守卫 |

（上一轮已修：阶段 1 必须命中目标、冻结用整局时停、示范轨迹改成「点/蓄」。）

---

## 4. 两个关键约束（别再改回去）

1. **蓄满在前、时停在后**：时停 = `timeScale=0` → 蓄力时长（`Time.time - segmentStartTime`）不再涨 →
   冻结时还没蓄满，玩家松手只能打出 0 级蓄力 → 派生招式出不来 → 死等。
   所以冻结条件里加了 `input.CurrentChargeLevel >= 1`。
2. **必须落在「可打断的前摇」**：`IsInAttackWindup`（不是收招段）且 `!IsInStrikeWindow()`（还没进出伤窗口）。

---

## 5. 还剩什么没验（要用户手按）

机器人只能合成手势，**做不出真实的「按住蓄满 → 按在敌人身上松手」**（它得手动摆 `isLongPress/segmentStartTime/comboChargeActive`）。
请在正式关实机走一遍：

1. 解锁指向突刺后进 N1 → 台词 → 点一下 1011（要打中它）。
2. 看到「他抡招了」→ **整局时停**（敌人举着刀停住、画面压暗、玩家不掉血）。
3. **手指按在 1011 身上按住** → 蓄满 → **松手**（手指落在 1011 身上 = 瞄准）→ 派生招式打断它 → 收拍。
4. 手指不在它身上松手 → 武器退回收蓄力漂浮位、要重新蓄力（招式自身既有行为，不是 bug）。

另外，**霸体防御拍 / 技能打断拍 / 时停招架拍**这次只做了「冻结代码收敛到同一个 helper」（逐行等价）+ helper 行为直接验证；
三拍**未做逐拍实机回归**（需要跑教学关 E0）—— 改动前它们也各自内联同一段代码，风险点是「调用点少了几行」，已逐个对照。

---

## 6. 工作区红线（全是用户并行 WIP，**禁止提交/回退**）

`Enemy_1011/1012/1013/105/106_Fixed/109.prefab`、`Formal_E0/N1/N2/N3_Battle.asset`、`guide_move_stabc2*.asset`、`tutorial_t0_*.asset`、`Zhangfei_Jab2/Jab3.asset`、`WaveMarchScheduler.cs`、`CavalryEnemy.cs`、`Enemy.Visuals.cs`、`design/cavalry-enemy.md`、`Editor/ConfigTable*.cs`、`config-tables.xlsx`、`Tools/build|apply_config_workbook.py`。

---

## 7. 复跑方式（Play Mode）

1. 存档前提：`unlockedTalents` 含 `talent_move_stabc2`、`taughtMoveGuides` 为空（正式 N1 的 row0 = 1011）。
2. Play Mode → `StageController.Instance.StartRouteBattle(Formal_N1_Battle)` → 敌人站定后导演自己起「招式解锁引导拍」。
3. 机器人手势：点一下用 `SubmitGestureForDebug(MoveGesture.Tap, 0, 目标列, 坐标)`；
   蓄力用每帧摆 `isLongPress=true` / `segmentStartTime=Time.time-1` / `hasTriggeredDuringHold=false` / `comboChargeActive=true`；
   松手用 `SubmitGestureForDebug(MoveGesture.Hold, 1, 目标列, 坐标)`。
4. 测完**还原存档**：`Library/Locus/tmp/player_save_backup_robot_20261107.txt` → `PlayerPrefs.SetString("player_save", json)`，
   并清 `SaveManager._cache`（反射置 null），否则读到旧缓存。
