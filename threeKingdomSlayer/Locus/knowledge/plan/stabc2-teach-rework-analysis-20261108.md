---
id: kd_9e25fb21-6756-4eac-a5f0-837b79d3fabe
summary: 指向突刺（stabC2）教学拍交接（2026-11-08 收尾版）：已回滚到 git a2fa2f82 的实现——冻结=挂起敌人（不动 timeScale），并说明“把和霸体教学一样当整局时停”是本轮改烂的根因（三套缩放时间机制停摆）；含需求 7 条、当前拍的阶段0/1/2 顺序、保留/回滚清单、未解决的“蓄力恒 0”与 WAIT/CHARGE 探针读法、机器人测试不可信的验证纪律、工作区红线与复跑步骤。
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 指向突刺（stabC2）教学拍 —— 交接（2026-11-08 收尾版：已回滚到 git 实现）

> **状态：原本的流程已跑通**（用户确认）。剩余一个已知问题：**蓄力等级上不去 → 打不出 stabC2**，已加探针待下次实机定位。
> **这一拍的冻结 = 「挂起敌人」，不是整局 `timeScale=0`** —— 这是本轮最大的教训，见 §1。

---

## 1. 最重要的一条：这拍的「冻结」到底是什么

**git 里 `a2fa2f82`（用户拍板的版本）的实现：**

```csharp
private void FreezeAllEnemiesForTutorial()
{
    foreach (var e in alive) e.PushSuspend(Enemy.SuspendSource.Tutorial);  // 只挂起敌人
    SetEnemiesHold(true);
}
```

⇒ **只挂起敌人，完全不动 `Time.timeScale`**：目标定格在抬手最后一帧，**世界照常跑**。

**这四轮把它改烂的根因**：用户说「要和霸体教学一样的冻结」，我理解成 `Time.timeScale = 0`（整局时停）。
整局停住后，这一拍依赖的三套**缩放时间**机制全部停摆 →

| 机制 | 停住后的表现 |
|---|---|
| 招式状态机时钟（接续窗口 `NormalizedTime`） | 窗口永远打不开 → 派生招式接不出来 |
| 蓄力等级（`Time.time - segmentStartTime`） | 蓄力恒 0 |
| 戳击视觉门 `_stabVisualTimer` | 永不过期 → 之后每次点按都被吞 |

→ 我为此连加三套 unscaled 补丁，每补一处又冒出新卡点。**这些补丁在本轮全部回滚了**。

**结论（写死在注释里）**：霸体防御拍/技能打断拍/时停招架拍用整局时停，是因为它们的玩家动作是
**状态（按住防御键）/ UI 点击 / 一次划动**；而这一拍的玩家动作是**连段手势链**（点 → 按住蓄满 → 松手），
依赖缩放时间的机制 —— 所以它只能「挂起敌人」，不能停整个世界。

---

## 2. 需求口径（用户原话归纳，7 条）

1. 敌人**抬手到顶就定格**，与玩家此刻做了什么**完全无关**。
2. 点一下后**武器悬停**着等玩家蓄力；枪和敌人一起停在那。
3. 这一段教学内容是**一段**，在**敌人抬手之后**弹出。
4. 指向突刺必须真**打断**他的霸体招，**之后**敌人才解冻。
5. 打空的那一下**照常放出去**，然后提示玩家把这一段连招（C1→C2）重打一遍。
6. **严禁任何兜底提前结束**；教学只能靠玩家操作推进。
7. 参考霸体教学的既有实现，不自造机制、不复用无关流程。

---

## 3. 现在的实现（`291ca941` 回滚 + `8a0f539d` 增补）

### 拍的顺序

```
阶段 0（新加，与 C1 无关）：routine 最前面等目标
        isAttackAnimating && isCFrame && TimeToAttackEnd() <= 1.5s
        → FreezeAllEnemiesForTutorial()   // 挂起敌人；世界照常跑
        → 弹这一段合并后的台词（nonBlocking）
                 ↓
阶段 1：闸门 Tap，枪悬停（PlayerState.freezeWeaponStance = true）
        玩家点一下 → 打中目标 → 进阶段 2
                 ↓
阶段 2：闸门 ComboHold；等接续窗口（≤3s）→ machine.SetFrozen(true)（窗口常开）
        每帧 BeginComboChargeHold()（枪一直举着）
        玩家按住蓄满 → 在目标身上松手 → 派生招式打出
        → 伤害落地 → 打断（attackInterrupted）→ 收拍 → 解冻敌人
```

### 保留的增补（都是用户明确要求过的）

| 项 | 说明 |
|---|---|
| 冻结判据 | `isCFrame`（与霸体防御拍 `Beats.cs:420` 逐字一致） |
| 兜底 | **全部删除**（指向突刺 8s / 霸体 20s / 技能 30s / 技能结果 3s / 招架 20s / 等三选一 90s） |
| 台词 | `phase2` + `phase3` 合并成 `guide_move_stabc2_hold.asset`（nonBlocking），**只在敌人抬手那一刻弹一次** |
| 打空 | 这一招的列 ≠ 目标列 → 提示重打这一段连招，**拍不结束** |
| 按住谁 | `holdEnemies: false`（目标必须能起新的霸体攻击）+ `SetEnemiesHoldExcept(target)` 按住其它敌人 |
| 收尾 | `EndBeat` 里补 `EndComboChargeHold()` + `freezeWeaponStance = false`（否则拍被打断后枪悬在空中） |

### 本轮回滚掉的东西

- 整局 `timeScale=0` 冻结（`FreezeWorldForTutorial` 不再被这一拍使用，其它三拍仍用）
- `PlayerMoveStateMachine.unscaledClockWhileWorldFrozen`（连同 `_elapsed` 分支）
- `AttackSystem` 戳击视觉门的 unscaled 递减
- `Dialogue.cs` 的「阶段 1 打出即过」推进补丁
- 阶段 0 的旧版（整局时停那版）、阶段 2 里的旧冻结检查

---

## 4. 未解决的问题（下次从这里继续）

**现象**：蓄力后即使瞄准正确，也打不出 stabC2。

**日志证据**（回滚版首次实机）：
```
阶段 1 命中目标(敌人#1011) → 进入阶段 2
接续窗口已打开 → 冻结招式推进
目标已抬手 → 冻结全场
MouseUp frame=834 pressDuration=5.038 swipeDistance=71.9   ← 按住 5 秒，但位移 71.9px
（没有任何「连段松手」记录；随后一串 0.05~0.13s 小点按 charge 0 全被闸门挡掉）
```

**已加探针**（每秒一行，下次实机会打出来）：

```
[Tutorial][连招拍] WAIT   抡招=? C技=? 剩余=?s 阈值=1.50s 目标=1011/Attacking
[Tutorial][连招拍] CHARGE 等级=? 连段蓄力=? 长按=? 指针按下=? 状态机活跃=? 时钟冻=? ts=? 阶段=?
```

`CHARGE` 五个字段分别对应 `CurrentChargeLevel` 的五道门：
`comboChargeActive`（状态机按下边沿置位）/ `isLongPress` / `hasTriggeredDuringHold` / `IsPointerDown` / 按住时长。
哪一项不成立，蓄力就是 0 —— 这行日志会直接指出来。

**另一个可疑点**：`swipeDistance=71.9` → 那一下被判成**划动**而不是「按住」，走了划动分支，压根没进连段松手。
下次按住时手指尽量别横向移动（横向位移本来只用于改目标列，但超过阈值会变成划动手势）。

---

## 5. 验证纪律（本轮血的教训）

**机器人测试对这一拍不可信**，原因三条：
1. 它直接 `SubmitGestureForDebug` 并**手工摆 `comboChargeActive = true`** —— 绕过了「按下边沿」这个真实前置；
2. 它用 `Time.timeScale == 0` 判「已冻结」—— 台词暂停/开场暂停也是 0，会把暂停误判成冻结；
3. 它用 `Skip()` 秒关台词 —— 恰好跳掉过真凶（「等台词播完」堵死阶段 2）。

⇒ 这一拍**只以用户实机日志为准**。判「已冻结」用 `_frozenByTutorial` 或 `Enemy.IsSuspended`，不要用 `timeScale`。

**另一条**：改动写完必须回读磁盘确认。本轮有一次以为把 `unscaledClockWhileWorldFrozen` 写进文件，
实际没落盘（python 脚本中途 assert 失败但输出被我误读），直到编译报错才发现。

---

## 6. 工作区红线（用户并行 WIP，**禁止提交/回退**）

`Enemy_*` prefab（1/101/1011/1012/1013/102/1021/103/104/105/106/107/108/109）、`Formal_E0/N1/N2/N3_Battle.asset`、
`guide_move_stabc2.asset`、`tutorial_t0_*.asset`、`Zhangfei_Jab2/Jab3.asset`、`WaveMarchScheduler.cs`、`CavalryEnemy.cs`、
`Enemy.Visuals.cs`、`design/cavalry-enemy.md`、`Editor/ConfigTable*.cs`、`config-tables.xlsx`、`Tools/build|apply_config_workbook.py`。

---

## 7. 复跑

1. 存档前提：`unlockedTalents` 含 `talent_move_stabc2`、`taughtMoveGuides` 为空（正式 N1 的 row0 = 1011）。
2. Play Mode → 进正式关 N1（或 `StageController.Instance.StartRouteBattle(Formal_N1_Battle)`）。
3. 实机流程：点一下 1011（打中）→ 枪悬停 → 按住蓄满（**手指别横向乱动**）→ 他抡招时**定格** → 在他身上松手 → 打断 → 收拍。
4. 出问题时看 `WAIT` / `CHARGE` / `连段松手：提交 Hold(蓄力N)` 三行日志。
5. 测完还原存档：`Library/Locus/tmp/player_save_backup_robot_20261107.txt` → `PlayerPrefs.SetString("player_save", json)` + 清 `SaveManager._cache`。
