---
id: kd_e530a240-6c69-4460-ba60-c230761fb9de
summary: 指向突刺（stabC2）教学拍交接（2026-11-08，**Play Mode 实测跑通，含「打偏→重打」路径**）：冻结统一为与霸体教学同一套 FreezeWorldForTutorial 且无条件触发、蓄满在前时停在后、必须落在可打断前摇、打偏那一下照常放出去后提示重打 C1→C2、**全项目删除所有「到点自动结束教学」的兜底**（指向突刺/霸体防御/技能打断/时停招架）、教学关整条路线仍跑通、教学关既有模式对照表、硬约束与复跑方式。
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 指向突刺（stabC2）教学拍 —— 交接（2026-11-08，已跑通 + 已删兜底）

> **状态：Play Mode 实测跑通（机器人端到端，含「打偏 → 重打」路径）**。
> 冻结方式与霸体防御拍 / 技能打断拍 / 时停招架拍**完全同一套代码**；冻结**无条件触发**（敌人一抬手就暂停）。
> **全项目已无「到点自动结束教学」的兜底** → 教学一律等玩家操作。
> 剩余：真实手指「按住蓄满 → 按在敌人身上松手」的瞄准手感要用户手按验收。

---

## 1. 这一拍现在的流程（已实测）

```
阶段 1  闸门 Tap → 玩家点一下，**必须命中目标**（1011）→ 命中才进阶段 2
阶段 2  ① 等接续窗口打开 → `machine.SetFrozen(true)` 把窗口撑开（玩家不用抢那 0.2s）
        ② 播「按住蓄满 → 松手」+「按在他身上别松手」两句台词（**nonBlocking，不暂停游戏**）
        ③ 敌人抡起霸体招、抬手到顶（可打断的前摇）→ **整局时停**（同一套冻结）
           ⚠️ **无条件触发**：跟玩家按没按、按没按满**无关**（同霸体/技能拍）
        ④ 玩家在冻结里**按住蓄满**（按住时长走 unscaled 时钟 → 冻结中也攒得满）
        ⑤ 玩家把手指按在 1011 身上松手 → 派生招式（指向突刺）打出 → **解冻** → 伤害落地 → **必须打断**
打偏了  这一下**照常放出去**（不拦玩家的操作 ✓）→ 没打中目标 → **这一拍不结束**
        → 提示玩家把这一段连招（C1→C2）**重打一遍**（状态机打回中立，下一记从 Jab1 起）
无兜底  **没有任何「到点自动结束」**：没蓄满 / 没瞄准 / 没打断 → 一直等玩家操作
```

**实测证据（正式 N1，row0 = 1011，机器人驱动）**：

```
[AttackSystem] 戳击 列2 伤害:30 射程:1                ← 阶段 1 的 Jab1（打中目标）
[Tutorial] 连招蓄力拍：接续窗口已打开 → 冻结招式推进
[Tutorial] 连招蓄力拍：阶段 2 台词结束 → 放行给玩家蓄力
[Tutorial] 连招蓄力拍：敌人抬起到顶（剩余 1.48s）→ 整局时停     ← 冻结（timeScale=0，玩家不掉血）
[AttackSystem] 戳击 列3 伤害:35 射程:2                ← **故意打偏**：这一下照常放出去
[Tutorial] 派生招式已打出 → 解冻，等伤害落地打断他（列=3 目标列=2）
[Tutorial] 这一下没朝目标打出去 → 提示玩家把这一段连招重打一遍   ← 拍没结束（phase=1，闸门回到 Tap）
[AttackSystem] 戳击 列2 伤害:30 射程:1                ← 重打：C1
[Tutorial] 敌人抬起到顶（剩余 1.49s）→ 整局时停          ← 再次冻结
[AttackSystem] 戳击 列2 伤害:35 射程:2                ← 重打：C2，打在目标列上
[Enemy] 打断攻击成功: #1(1011), col=2
[Tutorial] 手势拍完成：Stab col=2（第 1 次）
[Tutorial] 招式解锁引导完成：talent_move_stabc2         ← 存档标记已教过
```

状态核对：时停期间 `Time.timeScale=0`、敌人停在 `toEnd=1.48` 的前摇、**玩家血量不掉**；
收尾 `timeScale=1 / frozenByTutorial=false / allowInput=false / useUnscaled=false / machineFrozen=false`。

---

## 2. 已删掉的兜底（用户口径：「严禁任何兜底提前结束的方案」）

| 位置 | 原来的兜底 | 现状 |
|---|---|---|
| 指向突刺拍 | 阶段 2 超 8 秒 → 解冻 + 回阶段 1（`comboChargeTimeoutSeconds`） | **删**（字段与场景序列化值一并删）→ 没打断就一直等 |
| 霸体防御拍 | 冻结超 `freezeTimeoutSeconds`(20s) → `EndBeat()` 提前结束 | **删** → 玩家不按防御键就一直等（敌人定格、玩家安全） |
| 技能打断拍 | 冻结超 `skillBeatTimeoutSeconds`(30s) → 放行结束 | **删** |
| 技能打断拍 | 技能放出去后等 `skillResultWaitSeconds`(3s) 没打断 → 也收拍 | **删** → 等玩家再放一次技能 |
| 时停招架拍 | 冻结超 `freezeTimeoutSeconds`(20s) → 放行结束 | **删** |
| 技能打断拍 | 等三选一选完超 `skillChoiceWaitSeconds`(90s) → 「继续」（不等玩家选就开拍） | **删** → 等玩家自己选完 |
| 技能打断拍 | 等技能图标进注册表超 `skillSlotWaitSeconds` → 用「按槽位序号取位置」兜底 | **保留**（只是提示手画在哪，不代替玩家操作） |

⇒ 删完后**教学关整条路线仍可跑通**（`TutorialFlowDriver` 实测：`phase → Completed` ✓）。
⇒ 删完后**没有超时**：玩家不操作就一直等（敌人定格、玩家安全）；唯一脱困出口是玩家自己按暂停菜单退出 ✓。

---

## 3. 冻结方式：只有一套，不再造轮子

用户 2026-11-08：「**并没有实现与霸体教学一样的冻结 / 不要乱造轮子**」。
⇒ 之前把「只挂起敌人」（自加 `Enemy.SuspendSource.Tutorial`）当冻结 ✗ → 只停住敌人、世界照旧跑 → 玩家等抬手时照样挨打，且与另外三拍机制不一致。

**现在统一成 `TutorialStageDirector.FreezeWorldForTutorial()` / `UnfreezeWorldForTutorial()`**，四个拍都走它（霸体防御 / 技能打断 / 时停招架 / 指向突刺）：

```csharp
Time.timeScale = 0f;                       // 世界停住：敌人停在抬手那一帧，玩家也不挨打
_frozenByTutorial = true;
AttackSystem.Instance?.ClearActionLock();  // 冻结时动作锁走不完 → 不清就发不出招
SetSceneDim(true);                         // 只压暗场景
input.allowInputWhileTimeFrozen = true;    // 冻结中玩家的手势通道照常工作
input.useUnscaledHoldTime = true;          // 按住时长走 unscaled → 冻结里也能蓄满
```

**冻结点**（与霸体/技能拍同口径）：`target.isAttackAnimating && target.IsInAttackWindup && !target.IsInStrikeWindow() && 0 <= TimeToAttackEnd() <= 1.5f`。
（`IsInAttackWindup`/`!IsInStrikeWindow()` 是为了保证**冻结瞬间这一刀还打得断** —— 收招段 `isAttackDrawPhase` 一律打不断 ✗。）

**解冻出口**：① 派生招式打出（让伤害落地）② 目标死了 ③ 这一拍被打断（`EndBeat`）④ 导演被禁用/关场景。**没有「到点自动解冻收拍」**。

---

## 4. 教学关的既有模式（我们照抄的那套）

| | 霸体防御拍 | 技能打断拍 | 时停招架拍 |
|---|---|---|---|
| 冻结点 | C 技抬手到顶 `toAttackEnd <= 1.5` | 同口径 + 技能已就绪 | 命中帧那一刻 `TimeToStrike <= 0.03` |
| 冻结期间玩家做什么 | 按住防御键（状态）；每帧 `DriveStanceUnscaled()` 推进持枪姿态 | 点技能槽（UI 点击） | 上划（一次手势） |
| 解冻条件 | 玩家进入防御位 → 解冻 → 让敌人这一击落下来 | 技能释放 → 解冻 → 效果结算 → 霸体攻击被打断 | 格开 / 这一击没打中 |
| 台词 | `PushBeatDialogue` 只排队 + 等上屏（1.5s），**不等播完**；台词资产 nonBlocking → 不暂停、不压暗、不挡点击 | 同左 | 同左 |
| 每帧压住 timeScale | 有 | 有 | 有 |

**要点**：教学关的台词**全是 `nonBlocking: 1`**（21 个资产）→ 从不暂停游戏 → 唯一会暂停的是开场流程 `PlayTutorialIntro`（拍开始之前的「点击继续」）。
⇒ 所以「台词和冻结有冲突」这个说法不成立；我们这一拍也用同一套（非阻塞台词 + 每帧压 timeScale ✓）。

---

## 5. 硬约束（别再改回去）

1. **只冻敌人不行、只冻全局也不行**：只冻敌人 → 玩家照样挨打；只冻全局 → 玩家的连段（视觉门 / 接续窗口 / 蓄力条）全走缩放时间 → 冻住就永不过期 → 第二下/松手出不来。
   ⇒ 世界照常走 + 敌人那一刀停在抬手最后一帧（用项目既有机制：教学时停）。
2. **冻结无条件**：敌人抬手到顶就停（跟玩家按没按无关 ✓）；玩家在冻结里按住蓄满（按住时长走 unscaled ✓）。
3. **必须在可打断的前摇里冻结**：阈值 1.5s + `IsInAttackWindup && !IsInStrikeWindow()`。
4. **打偏的那一下照常放出去**，事后判定有没有打中目标；没打中 → 提示重打这一段连招，**不结束这一拍**。
5. **不允许任何「到点自动结束」**：没蓄满 / 没瞄准 / 没打断 → 一直等玩家操作。

---

## 6. 本轮修掉的 bug（累计）

| # | Bug | 现象 | 修法 |
|---|---|---|---|
| 1 | 冻结用自造的 `SuspendSource.Tutorial` | 只停敌人、玩家照样挨打 | 统一到 `FreezeWorldForTutorial`（四拍共用） |
| 2 | 冻结阈值 0.35s | 冻在收招段 → 打不断 → 永远过不去 | 1.5s + 可打断前摇两个判据 |
| 3 | 失败一轮没解冻**招式状态机时钟** | 接续窗口永不再开 + 戳击视觉门永不过期 → 下一轮死等 | 回阶段 1 的路径补 `machine.SetFrozen(false)` |
| 4 | 阶段 2 认「任何一次命中」 | 多段攻击前一段命中、玩家抢跑的那一下，都会误判 | 只认**派生招式**（`_comboChargeAttackFired`）的那一下 |
| 5 | `_comboChargePhase` 没初始化成 1 | 第一次命中掉进「打中即完成」→ 拍瞬间收掉 | 进连招拍时置 1 + 阶段守卫 |
| 6 | 冻结被绑在「玩家已蓄满」上 | **玩家不按 → 敌人抬手也不暂停** | 去掉该条件 → 无条件触发 |
| 7 | 冻结期间蓄力攒不满 / 状态机看不到手指 | `CurrentChargeLevel` 写死缩放时间；`IsPointerDown` 带 `timeScale > 0f` | 前者改走 `SegmentDuration`；后者「时停且放行输入时也算按着」 |
| 8 | 8 秒兜底 + 别的三拍的兜底 | 到点自动结束教学 | 全删（见 §2） |

---

## 7. 还剩什么没验

- 真实手指「按住蓄满 → 按在敌人身上松手」的瞄准手感（机器人做不出真蓄力）。
  实机走一遍：点一下 1011 → 读两句台词（不暂停，可直接按住）→ **他抡招时整局停住**（举着刀定格、你不掉血）→ **按住 1011 蓄满** → **在他身上松手** → 打断 → 收拍；
  若松偏了 → 招式照常放出 → 会提示「再打一遍这个连段」→ 重打即可。
- 霸体防御拍 / 技能打断拍 / 时停招架拍：删兜底后**教学关整条路线仍跑通** ✓（driver 实测 `phase → Completed`），但没有逐拍的手感复验。

---

## 8. 工作区红线（全是用户并行 WIP，**禁止提交/回退**）

`Enemy_1011/1012/1013/105/106_Fixed/109.prefab`、`Formal_E0/N1/N2/N3_Battle.asset`、`guide_move_stabc2*.asset`、`tutorial_t0_*.asset`、`Zhangfei_Jab2/Jab3.asset`、`WaveMarchScheduler.cs`、`CavalryEnemy.cs`、`Enemy.Visuals.cs`、`design/cavalry-enemy.md`、`Editor/ConfigTable*.cs`、`config-tables.xlsx`、`Tools/build|apply_config_workbook.py`。

---

## 9. 复跑方式（Play Mode）

1. 存档前提：`unlockedTalents` 含 `talent_move_stabc2`、`taughtMoveGuides` **为空**（正式 N1 的 row0 = 1011）。
2. Play Mode → `StageController.Instance.StartRouteBattle(Formal_N1_Battle)` → 敌人站定后导演自己起「招式解锁引导拍」。
3. 机器人手势：点一下用 `SubmitGestureForDebug(MoveGesture.Tap, 0, 目标列, 坐标)`；
   台词用 `ResolveDialogueView()` → `Skip()`（教学台词 nonBlocking，其实不关也行）；
   蓄力用每帧摆 `isLongPress=true` / `segmentStartTime=Time.time-1` / `hasTriggeredDuringHold=false` / `comboChargeActive=true`；
   松手用 `SubmitGestureForDebug(MoveGesture.Hold, 1, 列, 坐标)`。
4. 判「已时停」用 `_frozenByTutorial`（**不要用 `Time.timeScale==0`**：别的暂停也会是 0 → 误判）。
5. 测完**还原存档**：`Library/Locus/tmp/player_save_backup_robot_20261107.txt` → `PlayerPrefs.SetString("player_save", json)`，
   并清 `SaveManager._cache`（反射置 null），否则读到旧缓存。
