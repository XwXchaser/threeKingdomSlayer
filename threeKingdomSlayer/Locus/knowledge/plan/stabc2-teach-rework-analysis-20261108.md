---
id: kd_4be17420-c71f-4586-bf3b-792ead88c349
summary: 指向突刺（stabC2）教学拍第五版交接（2026-11-08，**Play Mode 实测跑通**）：从真实玩家日志定位的根因（教学台词暂停游戏 → 缩放时间的蓄力不涨 + InputManager 在 timeScale=0 时取消按住 → 实按 0.7s 蓄力却为 0）、冻结统一为与霸体教学同一套 FreezeWorldForTutorial、蓄满在前时停在后、瞄准台词前移、五条硬约束、7 个已修 bug、实测证据日志、工作区红线与复跑方式。
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 指向突刺（stabC2）教学拍 —— 交接（第五版：日志驱动的修复，2026-11-08）

> **状态：Play Mode 实测跑通（机器人端到端）**；冻结方式与霸体防御拍 / 技能打断拍 / 时停招架拍**完全同一套代码**。
> 冻结**无条件触发**：敌人一抬手就暂停（玩家按不按都暂停 ✓）。
> 剩余：真实手指「按住蓄满 → 按在敌人身上松手」的瞄准手感要用户手按验收。

---

## 1. 本轮最重要的发现（从真实玩家日志里挖出来的）

用户实机跑了一次，日志（`%LOCALAPPDATA%/Tuanjie/Editor/Editor.log`）里两行是关键：

```
[InputManager] MouseUp frame=772 pressDuration=0.700 swipeDistance=0.0
[InputManager] 连段松手：提交 Hold(蓄力0) → 列2，执行=True
[AttackSystem] 戳击 列2 伤害:35 射程:2 视觉射程:2      ← 打出来的是普通「戳击」，不是指向突刺
[Tutorial] 连招蓄力拍：这一下没打断目标的霸体招 → 不算，回阶段 1 重来
```

⇒ **玩家实按了 0.7 秒，蓄力却是 0** → 派生招式根本没出来 → 判据判「没打断」→ 回阶段 1 永远过不去。

**根因（两条，都是「教学台词会暂停整个游戏」引起的）**：

1. `DialogueManager` 播教学台词时会 **暂停游戏**（`_pausedForDialogue` → `Time.timeScale = 0`）。
   蓄力时长走的是**缩放时间**（`CurrentChargeLevel` → `Time.time - segmentStartTime`）→ **暂停期间按住也不涨**。
2. `InputManager.Update` 在 `timeScale == 0 && !allowInputWhileTimeFrozen` 时会**直接把这次按住取消掉**
   （`isTouching / isLongPress / hasTriggeredDuringHold` 全清）→ 玩家在台词没关掉时按住 → **松手时蓄力已被清零**。

⇒ 修法：**阶段 2 的台词必须先播完（玩家点完「继续」），才放行玩家蓄力**。
（时停之后的「瞄准」台词同样挪到蓄力**之前**播 —— 否则玩家点「继续」那一下点击**就是松手**，位置还在按钮上 → 反而教歪。）

---

## 2. 这一拍现在的流程（已实测）

```
阶段 1  闸门 Tap → 玩家点一下，**必须命中目标**（1011）→ 命中才进阶段 2
阶段 2  ① 等接续窗口打开 → `machine.SetFrozen(true)` 把窗口撑开（玩家不用抢那 0.2s）
        ② 播「按住蓄满 → 松手」+「按在他身上别松手」两段台词 → **等台词播完**（游戏恢复运行）
        ③ 敌人抡起霸体招、抬手到顶（可打断的前摇）→ **整局时停**（同一套冻结）
           ⚠️ **无条件触发**：跟玩家按没按、按没按满**无关**（同霸体/技能拍）
        ④ 玩家在冻结里**按住蓄满**（按住时长走 unscaled 时钟 → 冻结中也攒得满）
        ⑤ 玩家把手指按在 1011 身上松手 → 派生招式（指向突刺）打出 → **解冻** → 伤害落地 → **必须打断**
兜底    8 秒没打断 → 解冻 + 回阶段 1（不卡死）；目标被打死 → 换目标
```

**实测证据（正式 N1，row0 = 1011，机器人驱动）**：
> 关键一条：**机器人完全不按**（不摆任何按住状态）→ 敌人抬手 → `timeScale=0`、敌人停在 `toEnd=1.50`、玩家血量不掉 ✓

```
[AttackSystem] 戳击 列2 伤害:30 射程:1 视觉射程:1          ← 阶段 1 的 Jab1
[Tutorial] 连招蓄力拍：接续窗口已打开 → 冻结招式推进
[Tutorial] 连招蓄力拍：阶段 2 台词结束 → 放行给玩家蓄力        ← 新增
[Tutorial] 连招蓄力拍：敌人抬起到顶（剩余 1.48s）→ 整局时停
[AttackSystem] 戳击 列2 伤害:35 射程:2 视觉射程:2          ← 派生招式（StabR2，射程 2）
[Tutorial] 连招蓄力拍：派生招式已打出 → 解冻，等伤害落地打断他
[Enemy] 打断攻击成功: #1(1011), col=2
[Tutorial] 手势拍完成：Stab col=2（第 1 次）
[Tutorial] 招式解锁引导完成：talent_move_stabc2            ← 存档标记已教过
```
状态核对：时停期间 `Time.timeScale=0`、敌人停在 `toEnd=1.48` 的前摇、**玩家血量不掉**；收尾 `timeScale=1 / frozenByTutorial=false / allowInput=false / machineFrozen=false`。

---

## 3. 冻结方式：只有一套，不再造轮子

用户 2026-11-08 反馈：「**并没有实现与霸体教学一样的冻结 / 不要乱造轮子**」。
⇒ 之前那版把「只挂起敌人」（自加 `Enemy.SuspendSource.Tutorial`）当冻结 ✗：只停住敌人、世界照旧跑 → 玩家等抬手时照样挨打，且与另外三拍机制不一致。

**现在统一成 `TutorialStageDirector.FreezeWorldForTutorial()` / `UnfreezeWorldForTutorial()`**（`TutorialStageDirector.cs`），四个拍都走它：

| 拍 | 冻结点 |
|---|---|
| 霸体防御拍 | 敌人抬手到顶 |
| 技能打断拍 | 敌人抬手到顶 |
| 时停招架拍 | 命中帧那一刻 |
| **指向突刺拍** | **敌人抬手到顶 + 玩家已蓄满** |

helper 的四个动作（与原三处内联代码逐行等价）：
```csharp
Time.timeScale = 0f;                       // 世界停住：敌人停在抬手那一帧，玩家也不挨打
_frozenByTutorial = true;
AttackSystem.Instance?.ClearActionLock();  // 冻结时动作锁走不完 → 不清就发不出招
SetSceneDim(true);                         // 只压暗场景
input.allowInputWhileTimeFrozen = true;    // 冻结中玩家的手势通道照常工作
```
（`Enemy.SuspendSource.Tutorial` 已删除；输入层没有新增任何开关 ✓。）

---

## 4. 五条硬约束（别再改回去）

1. **只冻敌人不行、只冻全局也不行**：只冻敌人 → 玩家照样挨打；只冻全局 → 玩家的连段（视觉门 / 接续窗口 / 蓄力条）全走缩放时间 → 冻住就永不过期 → 第二下/松手出不来。
   ⇒ 世界照常走 + 敌人那一刀停在抬手最后一帧（用项目既有机制：`timeScale=0` 的教学时停）。
2. **冻结无条件、蓄力在冻结里攒**：敌人抬手就暂停（跟玩家按没按无关 ✓）；
   玩家在冻结里按住 → 按住时长走 unscaled 时钟 → 照样蓄满 → 松手出派生招式 ✓。
3. **台词必须先播完再让玩家蓄力**：教学台词会暂停游戏 → ① 缩放时间的蓄力不涨 ② `InputManager` 直接取消这次按住（`timeScale==0 && !allowInputWhileTimeFrozen`）→ 蓄力永远是 0。
4. **「瞄准」台词不能在时停那一刻弹**：玩家点「继续」那一下点击就是松手 → 派生招式在按钮位置释放 → 教歪。⇒ 挪到蓄力之前。
5. **冻结必须落在「可打断的前摇」**：阈值 0.35s → 1.5s（口径同霸体/技能拍）+ 必须 `IsInAttackWindup && !IsInStrikeWindow()`
   （0.35s 会冻在出伤/收招段 → 那一段 `isAttackDrawPhase` → 一律打不断 → 打中了却「没打断」）。

---

## 5. 本轮修掉的 7 个 bug

| # | Bug | 现象 | 修法 |
|---|---|---|---|
| 1 | 冻结用自造的 `SuspendSource.Tutorial` | 只停敌人、玩家照样挨打、与别的拍不一致 | 统一到 `FreezeWorldForTutorial`（四拍共用） |
| 2 | 冻结阈值 0.35s | 冻在收招段 → 打不断 → 永远过不去 | 1.5s + `IsInAttackWindup && !IsInStrikeWindow()` |
| 3 | 失败一轮没解冻**招式状态机时钟** | `SetFrozen(true)` 残留 → 接续窗口永不再开 + **戳击视觉门永不过期** → 下一记戳被吞 → 下一轮死等 | 回阶段 1 的三条路径都补 `machine.SetFrozen(false)` |
| 4 | 阶段 2 认「任何一次命中」 | 多段攻击的前一段命中、玩家抢跑的那一下，都会把这一拍判成「没打断」 | 阶段 2 只认**派生招式**（`_comboChargeAttackFired`）的那一下 |
| 5 | `_comboChargePhase` 没初始化成 1 | 第一次命中掉进通用「打中即完成」→ 拍瞬间收掉、还被写成「已教过」 | 进连招拍时置 1 + 阶段守卫 |
| 6 | **台词暂停把按住取消掉**（本轮日志定位） | 玩家 0.7 秒实按住，蓄力却是 0 → 只打出普通戳 → 永远过不去 | 阶段 2 台词播完才放行蓄力 |
| 7 | 时停瞬间弹「瞄准」台词 | 点「继续」= 松手，位置在按钮上 → 教歪 | 台词挪到蓄力之前；时停期间每帧压住 `timeScale`（台词系统会把它顶回 1） |
| 8 | **冻结被绑在「玩家蓄满了没」上** | **玩家不按 → 敌人抬手也不暂停 → 这一拍既没教到、玩家还挨打**（用户：「我都不用按，你怎么解释敌人抬手后没有暂停？」） | 冻结判据去掉蓄力条件 → 无条件触发（同霸体/技能拍） |
| 9 | 冻结期间蓄力攒不满 | `CurrentChargeLevel` 写死缩放时间 → 冻结中永远是 0 级 → 松手出不来派生招式 | 改用 `SegmentDuration`（跟着 `useUnscaledHoldTime` 走 unscaled ✓） |
| 10 | 冻结期间状态机看不到「手指按着」 | `InputManager.IsPointerDown` 带 `Time.timeScale > 0f` → 冻结中恒 false → 连段蓄力标记置不上 | 冻结且放行输入时也算「按着」（与 `Update()` 的早退口径一致 ✓） |

---

## 6. 还剩什么没验

- 机器人做不出真实的「按住蓄满 → 按在敌人身上松手」（它得手动摆 `isLongPress / segmentStartTime / comboChargeActive`）。
  请在正式关实机走一遍：点一下 1011 → 读两句台词 → 点「继续」→ **手指按在 1011 身上按住** → 蓄满 → 他抡招时**整局时停**（敌人举着刀停住、玩家不掉血）→ **在 1011 身上松手** → 派生招式打断他 → 收拍。
- 霸体防御拍 / 技能打断拍 / 时停招架拍本次只做「冻结代码收敛到同一 helper」（逐行等价）+ helper 行为直接验证，
  **未做逐拍实机回归**（需跑教学关 E0；日志里技能打断拍的冻结实测正常：`③ 已冻结：敌人抬起到顶（C技）` ts=0.00 ✓）。

---

## 7. 工作区红线（全是用户并行 WIP，**禁止提交/回退**）

`Enemy_1011/1012/1013/105/106_Fixed/109.prefab`、`Formal_E0/N1/N2/N3_Battle.asset`、`guide_move_stabc2*.asset`、`tutorial_t0_*.asset`、`Zhangfei_Jab2/Jab3.asset`、`WaveMarchScheduler.cs`、`CavalryEnemy.cs`、`Enemy.Visuals.cs`、`design/cavalry-enemy.md`、`Editor/ConfigTable*.cs`、`config-tables.xlsx`、`Tools/build|apply_config_workbook.py`。

---

## 8. 复跑方式（Play Mode）

1. 存档前提：`unlockedTalents` 含 `talent_move_stabc2`、`taughtMoveGuides` **为空**（正式 N1 的 row0 = 1011）。
   每次测完若被标成「已教过」，引导拍就不会再出现 → 先还原存档。
2. Play Mode → `StageController.Instance.StartRouteBattle(Formal_N1_Battle)` → 敌人站定后导演自己起「招式解锁引导拍」。
3. 机器人手势：点一下用 `SubmitGestureForDebug(MoveGesture.Tap, 0, 目标列, 坐标)`；
   台词用 `ResolveDialogueView()` → `Skip()` 关掉（等价玩家点「继续」）；
   蓄力用每帧摆 `isLongPress=true` / `segmentStartTime=Time.time-1` / `hasTriggeredDuringHold=false` / `comboChargeActive=true`；
   松手用 `SubmitGestureForDebug(MoveGesture.Hold, 1, 目标列, 坐标)`。
4. 判「已时停」用 `_frozenByTutorial`（**不要用 `Time.timeScale==0`**：台词暂停也会是 0 → 误判 ✓）。
5. 测完**还原存档**：`Library/Locus/tmp/player_save_backup_robot_20261107.txt` → `PlayerPrefs.SetString("player_save", json)`，
   并清 `SaveManager._cache`（反射置 null），否则读到旧缓存。
