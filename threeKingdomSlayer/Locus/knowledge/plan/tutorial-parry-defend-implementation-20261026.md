---
id: kd_ddc72026-48cc-418e-8b43-d8520147121b
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 改造文档 + 测试流程：教学关招架/防御改造（2026-10-26 已实现并自测通过）

> 配套：`plan/tutorial-parry-defend-redesign-20261026.md`（设计定稿）、`plan/tutorial-gesture-ui-implementation-20261026.md`（实现级方案 + 现有系统解剖）。
> 本文件记录**实际改了什么**、**怎么自测的**、**还剩什么**。

---

## 0. 结果速览

| 项 | 结果 |
|---|---|
| 教学关全链路 | ✅ 跑通：点按 → 横划 → 升级（冰雾）→ 选路 → **招架拍（时停格开霸体攻击）** → **防御拍（上划按住挡住 109 冲锋）** → 109 淡出离场 → 胜利结算 → 回落地页 |
| 自测方式 | Play Mode 程序化驱动（`unity_run_states` 状态机 + `InputManager.SubmitGestureForDebug` / `TutorialGestureHint.NotifyRawGesture`） |
| 实测数据 | 时停命中 `TimeToStrike=0.142`（窗口 0.15 内）；招架后**玩家 HP 100→100**；防御**吸收 40**、HP 100→100；**击杀数 5→5（109 不计击杀）**；终局：关卡 `Victory`、教程完成 `True`、铜钱 300、当前关卡推进到正式关 id=1 |
| 编译 | ✅ 无 error/warning；已按坑位核对 `Library/ScriptAssemblies/*.dll` mtime 晚于源码 |
| 顺带修掉的既有 bug | ①**战斗对话遮罩被落地页 UI 重构误关**（教学开场阻塞台词 → 游戏永久卡 timeScale=0，这就是"教学关进不去"的真因）②新敌人 1012 未登记进 `EnemyPool.enemyPrefabList`（波次刷不出）③落地页"清除存档"按钮中文乱码（字体无中文字形） |

---

## 1. 改造清单（按文件）

### 1.1 资产层

| # | 文件 | 改动 | 为什么 |
|---|---|---|---|
| 1 | `Assets/ScriptableObjects/Talents/TalentTree_Main.asset` | 删除 `talent_unlock_parry` 节点（9 → 8 节点） | 用户口径：招架是基础招式，不该锁在天赋后面。删后 `TalentTreeState.IsMoveGatedAndLocked` 找不到配对节点 → 恒放行 |
| 2 | `Assets/Resources/EnemyPrefabs/Enemy_1012.prefab` | 攻击步 `isCAttack: 0→1`；`animationTrigger: ""→"Attack"`；命中帧 `Enemy_1011_attack8→Enemy_1012_attack6`（keyIndex 6 / hitTime 1.8）；`visualYOffset 0.1389→0.001` | ①霸体前摇（红描边）= "这敌人有霸体"的可视信号 ②`animationTrigger` 为空时代码会去解析不存在的 `"CAttack"` trigger → 走空档+报错 ③**命中帧原来指的是 1011 的精灵名**，1012 自己的 clip 是 `Enemy_1012_Attack.anim`；且实测**大白弧峰值在 a6**（1011 在 a8，1012 整套 attack 早 ~2 帧）→ 不改则出伤被推到动画末尾、招架窗口整体错位 ④原值是为 **1011 的 idle1** 调的，1012 用同值会**浮空 0.14 世界单位**（≈14px），实测改 0.001 后脚底正好落在地面线 −1.8 |
| 3 | `Assets/RouteData/Tutorial/Tutorial_E0_Battle.asset` | 波0 `enemyIds[2]: 1021→1012`；`healthMultiplier 1→0.4` | 1021 盾兵移出教学关（没有 Launch 就破不了盾）；1012 血量 40，两下点戳收掉，这一拍收得快 |
| 4 | `Assets/ScriptableObjects/Dialogue/tutorial_t0_wave3.asset` | 改写为招架教学台词 | 原："按住不放蓄力，再往上划——上挑破盾！"（教的是已废弃的 Launch） |
| 5 | `Assets/ScriptableObjects/Dialogue/tutorial_t0_wave4.asset` | 改写为防御教学台词 | 原："前面那骑将……来者不善" |
| 6 | `Assets/ScriptableObjects/Dialogue/tutorial_t0_death.asset` | 改写为胜利收尾台词（原本是孤儿资产，无人引用） | 教学关以胜利收尾，不再是剧情杀 |
| 7 | `Assets/Scenes/Battle.scene` | ①`Tutorial Director.gestureSteps` 重配（见 §2）②**恢复战斗对话遮罩**（`DialogueLayer` / `DialogueOverlay` / `DialogueDimOverlay` 三个对象从 inactive 改回 active）③`EnemyPool.enemyPrefabList` **新增 `Enemy_1012`**（列表长度 12→13）④落地页"清除存档"按钮的 TMP 字体 → `方正粗黑宋简体 SDF` | ①教学拍重配 ②**落地页 UI 重构（`a6be228`, 10-25）误把对话遮罩关掉了**，此后所有战斗台词不可见，而教学开场是阻塞式台词 → 游戏永久停在 timeScale=0 → 就是"教学关进不去"的真因 ③敌人池用的是 **Inspector 列表**（不是 Resources 扫描）→ 新做的 1012 不登记就刷不出来（Console 报"未注册敌人ID 1012"）④`LiberationSans SDF` 没有中文字形 → 按钮显示为方块，玩家认不出那是"清除存档" |

### 1.2 代码层

| # | 文件 | 新增/改动 | 说明 |
|---|---|---|---|
| 8 | `Assets/Scripts/Enemy/Enemy.cs` | `public float TimeToStrike()` | 与 `IsInStrikeWindow()` 同源（tween 已播比例）→ 教学时停判断"窗口是否已开" |
| 9 | `Assets/Scripts/Enemy/EnemyManager.cs` | `public void DespawnEnemy(Enemy)` | **非死亡移除**：移出存活列表 + 移出列（skipChain），**不触发** `OnAnyEnemyDied` → 不计击杀/经验/铜钱/掉落；存活列表清空照常触发 `OnAllEnemiesDied`（波次完成判据） |
| 10 | `Assets/Scripts/Enemy/CavalryEnemy.Exit.cs`（新增） | `RequestTutorialExit()` / `IsExiting` | 109 离场：淡出（与返航并行，跟游戏时间）→ 等退场收尾 → `DespawnEnemy` |
| 11 | `Assets/Scripts/Enemy/CavalryEnemy.cs` | `Tick()` 里 `if (_exiting) return true;`；`ResetCavalry()` 复位 `_exiting` | **离场中不再发起新的冲锋**（否则 109 会在淡出途中再冲一次，实测会再打玩家 40 伤） |
| 12 | `Assets/Scripts/Enemy/CavalryVisualController.cs` | `CacheFadeColors()` / `SetFadeAlpha()` / `RestoreFadeAlpha()` | 马 + 骑手一起淡出；**回池前必须还原颜色**（否则下次复用隐形） |
| 13 | `Assets/Scripts/Player/InputManager.cs` | `forcePointerHeld` 字段；`PointerHeld => isTouching \|\| forcePointerHeld` | 教学临时锁定"手指仍按住"（防御拍：玩家进入防御后不许松手） |
| 14 | `Assets/Scripts/Player/InputManager.Mapping.cs` | `TutorialGestureTarget.ParryVertical` + 闸门放行规则 | 教学闸门只放行"不蓄力竖滑"（= 招架），蓄力竖滑（上挑）照旧不成立 |
| 15 | `Assets/Scripts/Player/Combat/PlayerGuard.cs` | `event Action<float> OnDamageAbsorbed` | 防御拍据此判定"玩家成功防住了一下" |
| 16 | `Assets/Scripts/UI/TutorialGestureHint.cs` | `GestureHintKind` 加 `Parry`/`ParryHold`；`StartRawWatch/StopRawWatch/NotifyRawGesture/IsRawWatching`；`PlayParry/PlayParryHold`（快速上划 0.22s；防御示范=上划后手指停住+光圈**脉动**不收缩）；`PollRawInput` 加竖划判定 | 冻结拍必须在 **timeScale=0 时**仍能收到输入 → 复用 `PollRawInput`（unscaled 时间）这条原始输入通道；提示组件**不碰 timeScale**，冻结由导演持有 |
| 17 | `Assets/Scripts/Tutorial/TutorialStageDirector.Beats.cs`（新增） | `FreezeParryRoutine` / `DefendHoldRoutine` / `OnRawGestureDetected` / `OnGuardAbsorbed` / `PushWaveDialogue` / `PlayerScreenPoint` | 两种新拍型（见 §3） |
| 18 | `Assets/Scripts/Tutorial/TutorialStageDirector.cs` | `GestureHintKind` 加两个值；`TutorialGestureStep` 加 4 个开关 | `freezeOnGesture` / `anchorToPlayer` / `holdAfterGesture` / `despawnTargetOnSuccess` |
| 19 | `Assets/Scripts/Tutorial/TutorialStageDirector.Targets.cs` | `ToGestureTarget` / `ExpectedAttackFor` 支持 `ParryVertical → AttackType.Parry` | 收拍判据 |
| 20 | `Assets/Scripts/Tutorial/TutorialStageDirector.Hints.cs` | `StartHint` 按拍型分流；`StopHint` 增加解冻 / 停原始监听 / 清强制按住 | 防止拍被打断后卡在 timeScale=0 或玩家永久无法出招 |
| 21 | `Assets/Scripts/Tutorial/TutorialStageDirector.Dialogue.cs` | `OnTutorialAttackExecuted` 尊重 `_beatEndsOnAttack` | 防御拍的成功判据是"进入防御姿态"而不是"出招" |

---

## 2. 教学关的最终拍序（场景 `gestureSteps` 为准）

| # | 战斗 | 波 | 手势 | 开关 | 效果 |
|---|---|---|---|---|---|
| 0 | Tutorial_N1_Battle | 0 | Tap | — | 教点戳（不变） |
| 1 | Tutorial_N1_Battle | 1 | Swipe（横） | — | 教横划（不变） |
| 2 | Tutorial_E0_Battle | 0 | **Parry** | `freezeOnGesture` `anchorToPlayer` | **时停招架拍**：1012 带霸体起手 → 出伤窗口一开就冻结 → 玩家上划 → 同帧解冻+派发招架 → 这一击被格开（敌人攻击取消、不结算伤害） |
| 3 | Tutorial_E0_Battle | 1 | **ParryHold** | `holdAfterGesture` `anchorToPlayer` `despawnTargetOnSuccess` | **防御拍**：109 被按住不动 → 玩家"上划后按住"进入防御 → 教学锁定防御 → 放 109 冲锋 → 伤害被吸收（HP 不变、破防条 +40/120）→ 109 返航淡出离场（**不计击杀**） |
| 4 | — | -2 | Tap | — | 出口点箭头（不变） |

台词：`tutorial_t0_wave3`（招架）/ `tutorial_t0_wave4`（防御）/ `tutorial_t0_death`（胜利收尾）。

---

## 3. 两种新拍型的实现要点

### 3.1 时停招架拍（`FreezeParryRoutine`）

```
① 等 1012 站定 → 推台词
② 逐帧查 target.IsInStrikeWindow()
   —— 窗口一开就冻结（timeScale=0，每帧压住，防 DialogueManager.Finish 顶回 1）
   —— **必须落在窗口内**：窗口前冻结 → timeToStrike 被冻在窗口外 → 招架永远判不中 → 死锁
③ 压暗场景（unscaled 淡入，冻结中也生效）；**不冻敌人**（它必须真的挥下来）
④ 提示组件开原始输入监听（StartRawWatch）+ 循环播「快速上划」示范
⑤ 玩家上划 → OnRawGestureDetected：
   同一帧 Time.timeScale=1 → AttackSystem.TryExecuteAttack(Parry, cancelCurrentMove:true)
   → InputManager.CancelCurrentGesture()（丢掉冻结期间错过的按下）
   —— 同帧解冻+派发是安全的：解冻后 tween 每帧只推进 ~16ms，剩余出伤 ≥0.15s（≈9 帧）→ 帧序不敏感
⑥ 收拍：撤压暗、停监听、闸门归零
```

### 3.2 防御拍（`DefendHoldRoutine`）

```
① 等 109 站定 → 登场台词（lineAtWaveStart）
② target.tutorialHold = true   ← 必须**在冲锋开始前**上；冲锋中上会被判"冻结中冲锋"→ 冲锋作废直接返航
③ BeginBeat(ParryVertical)：冻敌 + 压暗 + 输入闸门只放行未蓄力竖滑
④ 每帧查 PlayerState.WeaponStance.IsDefending
   → true：开 InputManager.forcePointerHeld（教学锁定） + 放开 109 的 tutorialHold（让它冲锋）
⑤ 109 冲锋命中 → PlayerGuard.TryAbsorbDamage(40) 吃掉 → OnDamageAbsorbed
   → 关掉 forcePointerHeld（玩家可正常松手收枪）+ EndBeat + 109 RequestTutorialExit()
⑥ 109 返航途中淡出 → DespawnEnemy（不计击杀）→ 存活列表空 → 波次完成 → 路线通关
```

---

## 4. 测试流程（可复跑）

### 4.1 前置

1. 编译：`unity_recompile`；**必须核对** `Library/ScriptAssemblies/Locus.Combat.dll` 等的 mtime 晚于源码 mtime（"报 compiled 但 DLL 没更新"是本项目的老坑）。
2. 清存档（否则落地页自举到正式关，进不了教学关）：
   `SaveManager.Delete();`（或点落地页左下角"清除存档"按钮，字体已修好可读）。
3. 确认 `Enemy_1012` 在 `Battle.scene` 的 `EnemyPool.enemyPrefabList` 里（否则报"未注册敌人ID 1012"）。

### 4.2 程序化自测（本次用的方式）

用 `unity_run_states`（Play Mode，request_editor_status=playing）跑一个状态机：

| 状态 | 做什么 | 关键断言 |
|---|---|---|
| boot | 等落地页就绪 → `RouteFlowHub.Current.RunPrepared()` | 路线 = 教学关·初见 |
| fight_n1 | 按闸门发手势（`SubmitGestureForDebug`），清场用 `TakeDamage(9999)`（**只打 1011**） | 闸门 Tap/Swipe 各出招一次；升级三选一用 `ConfirmChoice` |
| choice | `RouteChoiceButton` 的 `Button.onClick.Invoke()` | 出口 = tutorial_n1_e0 |
| e0w0 | 等 `hint.IsRawWatching` → `hint.NotifyRawGesture(GestureHintKind.Parry)` | 冻结时 `IsInStrikeWindow()==true`；派发后**玩家 HP 不变**；1012 攻击被取消 |
| e0w1 | 等闸门 `ParryVertical` → `SubmitGestureForDebug(SwipeVertical)` + `input.forcePointerHeld=true` | `IsDefending==true`；破防值 +40；**玩家 HP 不变**；存活列表归零；**击杀数不变** |
| done | 等路线 `Completed` | 关卡 `Victory`、`tutorialCompleted=True`、铜钱结算、当前关卡推进 |

**测试注意**：
- 枚举活着的敌人要**先拷贝** `new List<Enemy>(em.GetAllAliveEnemies())`，否则 `TakeDamage` 触发死亡会边遍历边改列表 → `InvalidOperationException`。
- 帧预算约 3600 帧（≈15s 墙钟）；用 `ctx.Sleep(n)` 省帧、用 `Time.timeScale=4~6` 压缩游戏时间（测试时加速，正式游戏是 1）。
- 结束一批后 Editor 仍留在 Play Mode，可以接着跑下一段（状态是连续的）。

### 4.3 人工验收清单（需要用户本人在真机/编辑器里做）

程序化测试驱动的是**等价 API 路径**（`SubmitGestureForDebug` / `NotifyRawGesture` / `forcePointerHeld`），**真实手指输入与视觉表现**需要人工过一遍：

| # | 检查项 | 期望 |
|---|---|---|
| 1 | 落地页左下角按钮 | 显示"清除存档"（不再是方块） |
| 2 | 点它 → 重载落地页 → 「开始关卡」 | 进的是**教学关·初见** |
| 3 | 教学开场台词 | **看得见**（气泡在屏幕上），点一下能推进 |
| 4 | N1 两波 | 点按 / 横划的手势提示与之前一致 |
| 5 | E0 波0 | 1012 出现（前摇**红描边**= 霸体）；看到"快速上划"示范（手指在**玩家**身上，不是敌人身上） |
| 6 | 玩家上划 | 游戏在**出伤窗口**瞬间定格（时停）；松手/上划后**这一击没打中**（敌人被格开、玩家零伤害） |
| 7 | 招架动作 | 枪从下方挑上来（昨天重做的那版） |
| 8 | E0 波1 | 109 入场（先播登场台词）→ 提示"上划后按住"（手指上划后**停住**、光圈脉动） |
| 9 | 玩家上划并按住 | 枪落防御位；109 冲锋命中 → **玩家不掉血**、血条上方破防条涨一截 |
| 10 | 109 离场 | 109 **一边后退一边淡出**，然后消失；**没有被击杀**（击杀数不变） |
| 11 | 收尾 | 胜利结算 → 铜钱 → 回落地页（当前关卡变成"长坂坡·卷轴"） |
| 12 | 正式关 | **暂未实现**首次进入的 C2 教学（见 §6） |

---

## 5. 自测数据（本次实测原文）

```
[BOOT] 落地页就绪：路线=教学关·初见 → 开始关卡
[选择] 确认升级：ActiveSkill_IceMist
[N1] 两波打完 → 选路
[选路] 点击出口 tutorial_n1_e0
[E0W0] ★ 时停拍冻结：timeScale=0 目标=Enemy_1012(Clone) IsInStrikeWindow=True TimeToStrike=0.142 玩家HP=100
[E0W0] → 已派发招架（解冻）
[E0W0] ★ 招架结果：timeScale=4 玩家HP 100→100（未受伤=True） 敌人血量=40 存活=1
[防御] ★ 已模拟「上划 + 按住不放」（HP=100 破防值=0 109存活=1）
[防御] 玩家已进入防御姿态（forcePointerHeld=True）→ 导演放 109 冲锋
[防御] ★ 防御成功：吸收 40 破防值=40/120 玩家HP 100→100（未受伤=True）
[防御] ★ 109 已离场：存活数=0 玩家HP=100（防御后未再受伤=True）击杀数 5→5（109 不计击杀=True）
[收尾] ★ 路线完成：关卡状态=Victory 教程完成=True 铜钱=300 击杀=5 HP=100 当前关卡id=1
```

---

## 8. 第二轮修复（用户实机反馈的两个问题，2026-10-26 晚）

### 8.1 问题 1：教学没到对应流程时，对应招式应该锁住

**现状**：教学只在"手势拍"进行时用 `InputManager.tutorialGestureTarget` 过滤**手势**；拍之外玩家可以随意使用任何招式
→ 玩家可以在招架拍之前就上划出招架（抢跑），或用蓄力上挑直接把教学敌人打死（跳过这一拍）。

**修法**：新增**招式白名单** `Assets/Scripts/Data/TutorialAttackGate.cs`（Data 层，战斗层与教学层都能用）：
- 白名单 = **已经教完的招式 ∪ 当前这一拍要教的招式**。
- 拍开始 → `ApplyAttackGate(这一拍的招式)`（这一拍里玩家必须能出它）。
- 拍真的完成（`OnTutorialAttackExecuted` 命中）→ 该招式转入"已教"。
- `AttackSystem.TryExecuteAttack` 开头校验白名单 → 不在名单里的招式**发不出来**（含大招、蓄力上挑等本教学不教的东西）。
- 只有教学路线生效；离开教学路线立刻放开（`TutorialStageDirector.OnDisable` + `UpdateAttackGate`）。

**两个坑**：
1. 路线切场时 `RouteFlowHub.Current` 会短暂为 null → 那��帧会被当成"离开教学"→ **清空已教招式** → 玩家学过的招式又锁回去。
   → 修法：重新启用时**不清**已教集合；已教集合的生命周期 = 一次教学流程（在 N1 第一波开始时重置）。
2. 防御拍的成功判据是"进入防御姿态"而不是"出招" → 它的 `EndBeat` 走的是 `OnGuardAbsorbed`，
   `_beatCompleted` 为 false → **招架不会因为防御拍而变成"已教"**（防御拍用的是招架，而招架在上一拍已经教过了，语义上不冲突）。

### 8.2 问题 2：时停里触发不了招架、遮罩结束后完全动不了

**根因（两个）**：
1. 第一版时停拍**自己写了一套原始输入轮询**（绕开 InputManager，因为 timeScale=0 时 InputManager 不处理输入）。
   那套轮询在**触摸抬手那一帧拿不到 `Ended` 相位**（`Input.touchCount` 已归零）→ 划动永远识别不到；
   识别不到就**永久冻结**（timeScale=0）→ 玩家"完全动不了"。
   → 顺带也修了 `TutorialGestureHint.PollRawInput` 的这个触摸抬起 bug（旧 `Gate()` 模式受益）。
2. 我上一轮的程序化自测**直接调 `NotifyRawGesture()`**，**绕过了真实输入检测** → 这条路径从没被验证过（我的测试盲区）。

**修法（方向反过来）**：不再自己写输入通道，而是**让游戏自己的手势通道在时停期间继续工作**：
- `InputManager.allowInputWhileTimeFrozen`：`timeScale == 0` 时不再提前 return（只有教学导演会开）。
- 时停拍改成标准拍：`BeginBeat(ParryVertical, AttackType.Parry, holdEnemies: false)`（**不冻敌人**）+ 闸门只放行"不蓄力竖滑"；
  玩家的竖滑走**游戏自己的识别**（阈值/方向/抬手全一致）→ 招架执行 → 命中窗口 → 敌人攻击被取消。
- 收拍（`EndBeat`）里解冻 + 关掉 `allowInputWhileTimeFrozen`。
- **防卡死保险**：`freezeTimeoutSeconds`（默认 20s，unscaled）——玩家一直没做出手势就放行并结束这一拍，
  避免"永远识别不到 → 永久冻结"把游戏卡死。

### 8.3 第二轮自测结果（真实输入通道 + 招式锁）

```
[N1] 进入 wave=0（白名单=）                              ← 起始：什么都不许发
[N1] 手势拍#1 出招 闸门=Tap 成功=True 白名单=Stab        ← 教了点按 → Stab 解锁
[N1] 进入 wave=1（白名单=Stab）                          ← 记住了 Stab
[N1] 手势拍#2 出招 闸门=SwipeHorizontal 成功=True 白名单=Stab/Slash  ← 教了横划 → Slash 解锁
[选路] 已进入 E0 战斗（白名单=Stab/Slash）               ← 路线切场后没丢
[E0W0] ★ 招式锁断言：招架拍之前上划 → 发出=False 目标HP 40→40（应不变）
[E0W0] ★ 已教招式断言：此时点按 → 发出=True（白名单=Stab/Slash）
[E0W0] ★ 时停拍冻结：timeScale=0 IsInStrikeWindow=True TimeToStrike=0.140
[E0W0] → 已在冻结中上划（游戏自己的手势通道）
[E0W0] ★ 招架结果：玩家HP 100→100（未受伤=True） 敌人攻击中=False   ← 攻击被取消、零伤害
[防御] ★ 防御成功：吸收 40 破防值=40/120 玩家HP 100→100（未受伤=True）
[防御] ★ 109 已离场：存活数=0 击杀数 5→5（109 不计击杀=True）
[DONE] 路线阶段=WaitingReward 铜钱=50 击杀=5 HP=100 招式白名单=Stab/Slash/Parry
```

### 8.4 第二轮新增/改动的文件

| # | 文件 | 改动 |
|---|---|---|
| 22 | `Assets/Scripts/Data/TutorialAttackGate.cs`（新增） | 招式白名单（Data 层） |
| 23 | `Assets/Scripts/Player/Combat/AttackSystem.cs` | `TryExecuteAttack` 开头校验白名单 |
| 24 | `Assets/Scripts/Player/InputManager.cs` | `allowInputWhileTimeFrozen`（时停期间仍处理输入） |
| 25 | `Assets/Scripts/Tutorial/TutorialStageDirector.Rules.cs` | `_taughtAttacks` / `_currentTeachAttack` / `_beatCompleted` / `AttackTypeFor` / `ApplyAttackGate` / `UpdateAttackGate` |
| 26 | `Assets/Scripts/Tutorial/TutorialStageDirector.Hints.cs` | `BeginBeat(target, teachAttack, holdEnemies)`；`EndBeat` 负责解冻 + 关输入例外 + 记录"已教" |
| 27 | `Assets/Scripts/Tutorial/TutorialStageDirector.Dialogue.cs` | 收拍时置 `_beatCompleted = true` |
| 28 | `Assets/Scripts/Tutorial/TutorialStageDirector.cs` | `Update()` 里驱动招式白名单；N1 第一波重置"已教"；`OnDisable` 放开白名单 |
| 29 | `Assets/Scripts/Tutorial/TutorialStageDirector.Beats.cs` | 时停拍改走**游戏自己的手势通道**（删掉自写输入轮询）+ 20s 防卡死保险 |
| 30 | `Assets/Scripts/UI/TutorialGestureHint.cs` | 删掉自写原始输入监听（`StartRawWatch`/`NotifyRawGesture` 等）；修**触摸抬手相位丢失**的 bug |

### 8.5 教学里"招式解锁"的最终口径

| 教学进度 | 可用招式 | 锁住 |
|---|---|---|
| 起始（N1 波0 拍之前） | 无 | 全部（含招架） |
| 点按拍进行中 | 点按 | 其余 |
| 点按拍完成后（N1 波1） | 点按 | 横划、招架、上挑… |
| 横划拍进行中 | 点按 + 横划 | 招架… |
| 横划拍完成后（E0 波0） | 点按 + 横划 | 招架、上挑… |
| 招架拍进行中 | 点按 + 横划 + 招架 | 上挑… |
| 招架拍完成后（E0 波1 防御拍） | 点按 + 横划 + 招架 | 上挑… |

> 教学里**不教蓄力上挑（Launch）**，因此它在整个教学关一直是锁的（连同大招、穿刺、横扫）。
> 正式关的白名单由教学导演在离开教学路线时放开。

---

## 9. 还没做的（按批次）

| # | 事项 | 状态 | 说明 |
|---|---|---|---|
| 1 | **正式关首次进入的 C2 教学** | 未做 | 用户口径 #9/#10：教学关通关后玩家去天赋树解锁 `talent_unlock_c2`；正式关首入用一个拍同时教「C2」与「重攻击打断霸体」。需要：①手势提示新增"C2 复合手势"（Tap→按住→横划改列→松手）②教学导演泛化到正式关（现在只认 `tutorialRouteConfig`）③正式关的霸体敌人（暂用 1012 还是另配未定） |
| 2 | 1012 的**美术验收** | 部分 | 尺寸/锚点/命中帧已实测（见 `plan/enemy1012-asset-pull-20261026.md` §8）；**画面观感**（站姿比 1011 矮 12% 是姿态差，红缨/描边语言是否一致）需用户实机看 |
| 3 | 招架动作观感 | 待用户 | 招架本身是昨天重做的"沿枪轴推出"版，用户还没在 Play Mode 看过（`plan/parry-guard-rework-handoff-20261026.md` §6 待办 1） |
| 4 | 选关页中文字体 | 未做 | 选关页用 legacy `Text` + `LegacyRuntime.ttf`；实测该字体**有**中文字形（`HasCharacter('教')=True`），故不算 bug；如要换统一字体再做 |
| 5 | 教学关铜钱总数 | 未算 | 用户口径 #7：先不计击杀，其他先不算；实测 300（250 路线 + 50 击杀铜钱） |
| 6 | `HubTabBar` 仍被关闭 | 未动 | 落地页重构遗留，属用户正在做的落地页 UI 范围，**本轮没碰** |

---

## 10. 第三轮修复（用户实机反馈的四个问题，2026-10-26 深夜）

### 10.1 问题 1：清空存档后「直接进关卡，没到主菜单」

**事实**：本项目**没有独立的主菜单场景** —— MainMenu 在落地页重构（`plan/main-menu-hub-refactor-plan-20261020.md`）里被删了，
入口改成**落地页**（Battle 场景里的一片 UI，含「开始关卡」大按钮 + 选关/天赋/家园 tab + 四宫格）。
清档后的流程是：**落地页自举 → 当前关卡 = 教学关·初见 → 点「开始关卡」→ 进教学关**（“新档先教学”是既定设计）。

**同时修掉的真缺陷**：对话的**「点击任意处继续」推进按钮**在**没有台词时也是激活的**（落地页上它压在底部四个按钮上，
看着像“点这里就能进关卡”）。根因：它的隐藏只发生在 `Show()`/`Clear()`，而落地页从没播过台词 → 场景里的初始激活态没人关。
→ 修法：`BattleDialogueView.Start()` 里若没在显示就 `Clear()` 一次。

**如果确实要一个“真·主菜单”**（新游戏 / 继续 / 设置）：最小改动是在落地页加一层门——
存档为空时先显示「新游戏」而不是直接进当前关卡（新游戏 → 若教学未完成 → 教学关；否则 → 当前关卡），不必重建 MainMenu 场景。

### 10.2 问题 2：招架太难 + 敌人攻击动画太快

**根因（实机截图定位）**：**冻结拍里根本没有手势提示** —— 示范循环只写在旧拍（`HintRoutine`）里，
新拍（`FreezeParryRoutine`）只冻结、没播示范 → 玩家看到的是“画面突然停住 + 一句台词”，**不知道要划**。
另外还有两个隐藏坑：冻结时 `Time.deltaTime = 0` → **上一拍的动作锁永远走不完** → 招架发不出来；以及 `EndBeat` 收拍后图层全隐藏。

**修法**：
| 项 | 做法 |
|---|---|
| 手势示范 | 冻结拍开始时 `PlayGestureAt(Parry, 玩家位置, 上移 260px)`；提示层的招架示范**自循环**（上↔下来回划），**不再隐藏图层**（手指常驻，玩家任何时候都知道要在哪儿划） |
| 动作锁 | 冻结时 `AttackSystem.ClearActionLock()`（否则上一拍的锁在冻结中永不走 → 招架永远发不出来） |
| 空放不收拍 | 冻结拍要求**真的格到了**（`AttackSystem.LastParryBlocked`）才算完成；格不到就继续冻结让玩家再试（教学不设失败），并顺手清掉空放招架的 0.9s 后摇 |
| 攻击太快 | 教学 1012：`attackSpeed 0.5 → 0.33`（攻击间隔 2s → 3s）、`parryStrikeWindow 0.15 → 0.4`（真实时间里的招架也更容易） |
| 压暗 | 冻结拍叠一层**提示层自己的压暗**（`TutorialGestureHint.ShowDim`），见 §10.5 |

### 10.3 问题 3：109 打到防御上，格挡没有震动反馈

**根因**：`PlayerGuard.TryAbsorbDamage` 只涨破防值 + 武器后退，**没有任何打击反馈**（镜头/卡肉都没有）。
而招架格开走的是 `AttackSystem.RequestParryImpactFeedback`（镜头重击级）→ 所以“招架有感、防御没感”。

**修法**：`PlayerGuard.TryAbsorbDamage(damage, source)`（新增 source 参数，由 `PlayerState.TakeDamage` 传入）：
- **攻击者被卡肉** 0.12s（`source.ApplyHitStop`）→ 那一击顿住；
- **镜头重击级震动**（`CameraFeedbackController.RequestGuardBlock`）。

### 10.4 问题 4：敌人受击时整个场景都在震（需求是敌人自己震）

**根因**：`CameraFeedbackController.RequestHit` 对**所有 Standard 及以上**的命中都震镜头，
冷却只有 0.04s → 普通攻击连打时镜头一直在震；而且震的是**相机 + 背景视差**（背景跟着位移 135×）→ 整个画面在抖。

**修法**：
- 新增 `heavyOnlyCameraShake = true`：**只有重击/格挡才震镜头**，普通命中不碰镜头（敌人自己的闪白 + 缩放抖动照旧 → “敌人震”）。
- 限频：`requestCooldown 0.04 → 0.12`。

### 10.5 附带修掉的两个渲染问题（实测定位）

| 问题 | 根因 | 修法 |
|---|---|---|
| 场景压暗在教学关**完全没效果** | ① 换节点后路线场景会换，但压暗只绑定一次 → 压的是上一个节点的场景；② `SpriteRenderer` 的 `_Color` 由渲染器自己接管，**MaterialPropertyBlock 改不动它**；③ 提示层的 `DimOverlay` **没给 sprite → Unity 的 Image 没 sprite 就完全不渲染**（连旧的黑遮罩门槛也一直是失效的） | ① 压暗按当前活动场景**重新绑定**；② 精灵一律走 `sprite.color`；③ `TutorialGestureHint.EnsureDimSprite()` 运行时补一张 4×4 白图（不新增资产），时停拍用提示层压暗（α0.45）保证画面真的暗下来 |

### 10.6 第三轮自测结果

```
[N1] wave=0（白名单=）                       ← 起始：什么都不许发
[N1] 手势拍#1 闸门=Tap 成功=True 白名单=Stab
[N1] 手势拍#2 闸门=SwipeHorizontal 成功=True 白名单=Stab/Slash
[选路] 已进入 E0 战斗（白名单=Stab/Slash）   ← 切场后没丢
[E0W0] ★ 招式锁：招架拍前上划 → 发出=False 目标HP 40→40（应不变）
[E0W0] ★ 已教招式：此时点按 → 发出=True（白名单=Stab/Slash）
[E0W0] ★ 定格：提示层 finger=True 压暗alpha=0.45     ← 手势提示与压暗都在
[E0W0] ★ 招架结果：玩家HP 100→100（未受伤=True） 敌人攻击中=False
[防御] ★ 防御成功：吸收 40 破防值=40/120 玩家HP 100→100（未受伤=True）
[防御] ★ 109 已离场：存活数=0 击杀数 5→5（109 不计击杀=True）
[DONE] 白名单=Stab/Slash/Parry
```

### 10.7 第三轮新增/改动的文件

| # | 文件 | 改动 |
|---|---|---|
| 31 | `Assets/Scripts/Core/CameraFeedbackController.cs` | `heavyOnlyCameraShake`（默认开）+ 冷却 0.12s + `RequestGuardBlock()` |
| 32 | `Assets/Scripts/Player/Combat/PlayerGuard.cs` | `TryAbsorbDamage(damage, source)`：格挡反馈（攻击者卡肉 + 镜头震动） |
| 33 | `Assets/Scripts/Player/Combat/PlayerState.cs` | 把攻击者传给 Guard |
| 34 | `Assets/Scripts/Player/Combat/AttackSystem.cs` | `ClearActionLock()` + `LastParryBlocked` |
| 35 | `Assets/Scripts/Player/Combat/AttackSystem.Types.cs` | 记录本次招架是否格到 |
| 36 | `Assets/Scripts/Tutorial/TutorialSceneDim.cs` | 精灵改走 `sprite.color`；压暗按场景重新绑定 |
| 37 | `Assets/Scripts/UI/TutorialGestureHint.cs` | 招架示范自循环 + 图层常驻 + `ShowDim/HideDim` + `EnsureDimSprite()` |
| 38 | `Assets/Scripts/UI/BattleDialogueView.cs` | 无台词时隐藏推进按钮（落地页不再有“点击任意处继续”） |
| 39 | `Assets/Scripts/Tutorial/TutorialStageDirector.Beats.cs` | 冻结拍：示范 + 清动作锁 + 空放不收拍 + 提示层压暗 |
| 40 | `Assets/Scripts/Tutorial/TutorialStageDirector.Rules.cs` | 压暗按当前场景重新绑定 |
| 41 | `Assets/Scripts/Tutorial/TutorialStageDirector.Dialogue.cs` | 空放招架不收拍（清锁让玩家再试） |
| 42 | `Assets/Resources/EnemyPrefabs/Enemy_1012.prefab` | `attackSpeed 0.5→0.33`、`parryStrikeWindow 0.15→0.4` |

---

## 12. 第四轮修复（用户实机反馈三个问题，2026-10-27 凌晨）

### 12.1 问题 1：点播放后，**关卡还没开始就弹台词 + 黑遮罩**

**根因（用户截图定位）**：教学导演的开场台词条件是「路线不是 None」→ **落地页的阶段就是 `Ready`**（不是 None）
→ 于是玩家还没点「开始关卡」，落地页上就弹了「燕人张飞在此！」，而且它是**阻塞式**（`nonBlocking=0`）
→ 整个游戏被 `timeScale=0` 卡住 + 对话压暗 → 看着像「直接进关卡」。

**修法**：开场台词只在**真正进关卡**时播（`FakeRoutePhase.EnteringNode` / `Battle`），落地页（`Ready`）不播。

> 顺带澄清（不是 bug）：本项目**没有独立的主菜单场景**（MainMenu 在落地页重构里被删了），
> 入口就是落地页（Battle 场景里的一片 UI）。清档后的流程 = 落地页自举到「教学关·初见」→ 点「开始关卡」→ 进教学关。
> 想要「真·主菜单（新游戏/继续）」需要另加一层门，已在 §10.1 记下。

### 12.2 问题 2：黑遮罩把**所有东西**都遮住了（前面的遮罩只遮场景）

**根因**：上一轮我为了保证「画面真的暗下来」，给时停拍加了一层**提示层自己的全屏遮罩**（`TutorialGestureHint.ShowDim`）
→ 它在 Canvas 60（最上层）→ **角色/敌人/UI 全被盖住**，与「遮罩只遮场景」的设计相反。

**修法**：去掉时停拍的全屏遮罩，只用**场景压暗**。而场景压暗在教学关一直不生效，挖出**三层根因**（全部实测）：

| # | 根因 | 修法 |
|---|---|---|
| ① | 节点切换会在**同一个场景里重建景物**（scene handle 不变，但旧渲染器已销毁）→ 绑一次就绑在死对象上 | **每次开压暗都重新绑定**（`SetSceneDim(true)` 里直接 `Bind`） |
| ② | Y 场景的精灵 shader（`CurvedScroll/Y Sample Scenery E0 Route`）**忽略顶点色**，而 `SpriteRenderer` 的 PropertyBlock 改不动 `_Color`（渲染器自己接管）→ `sprite.color` 与材质 `_Color` 两条路都不生效 | 改**共享材质的 `_Color`**（该材质确实有 `_Color`，且 shader 会乘） |
| ③ | Y 场景脚本（`ValleyArtPresentation.LateUpdate` / `ScrollWorldItem`）**每帧重写**材质颜色与 PropertyBlock → 只在淡入那 0.12s 压一次会被立刻改回去 | `TutorialSceneDim` 加 `[DefaultExecutionOrder(1000)]` + **每帧重压**（LateUpdate） |

**结果（截图验证）**：地面/拱门/城墙/箱子/天空全部变暗，**敌人与 UI 保持原亮度** ✓ —— 正是「遮罩只遮场景」。

### 12.3 问题 3：parry 时机太难；只有长枪伸出时才有判定；招架像在「打断抬手帧」

**根因**：冻结点选在**出伤窗口开启**（命中前 0.15s）→ 那一刻敌人**还在抬手** → 玩家看到的是「招架把抬手打断了」，
而且要给玩家 0.15s 内反应（几乎不可能）→ 体感就是「只有长枪伸到最前才判得到」。

**修法**：
| 项 | 改动 |
|---|---|
| 冻结点 | 从「窗口开启」改为**命中帧那一刻**（`TimeToStrike() <= 0.03`）→ 画面上武器已经挥到位 → 玩家的动作是「**格开这一击**」而不是「打断抬手」。冻结后 tween 停住 → 窗口永远开着 → 玩家什么时候划都仍然算格开 |
| 敌人节奏 | 教学 1012：`attackSpeed 0.5→0.33`（攻击间隔 2s→3s）、`parryStrikeWindow 0.15→0.4` |
| 可重试 | 这一下没格到就不收拍（继续冻结等玩家再试），并清掉空放招架的 0.9s 后摇（否则冻结中后摇不走 → 玩家再也划不出来） |
| 动作锁 | 冻结时清空动作锁（冻结中 `deltaTime=0` → 上一拍的锁永远走不完 → 招架发不出来） |

**实测**：定格时 `TimeToStrike = 0.000`（命中帧）→ 冻结中上划 → 敌人攻击被取消、玩家 HP 不变 ✓。

### 12.4 第四轮自测结果（全绿）

```
[BOOT] 落地页就绪（应无台词）→ 开始关卡          ← 落地页不再弹开场台词
[N1] 手势拍#1 闸门=Tap 成功=True 白名单=Stab
[N1] 手势拍#2 闸门=SwipeHorizontal 成功=True 白名单=Stab/Slash
[E0W0] ★ 招式锁：招架拍前上划 → 发出=False 目标HP 40→40（应不变）
[E0W0] ★ 已教招式：点按 → 发出=True（白名单=Stab/Slash）
[E0W0] ★ 定格：提示finger=True 场景压暗=True 敌人TimeToStrike=0.000（命中帧）
[E0W0] ★ 招架结果：玩家HP 100→100（未受伤=True） 敌人攻击中=False
[防御] ★ 防御成功：吸收 40 破防值=40/120 玩家HP 100→100（未受伤=True）
[防御] ★ 109 已离场：存活数=0 击杀数 5→5（109 不计击杀=True）
```

### 12.5 第四轮新增/改动的文件

| # | 文件 | 改动 |
|---|---|---|
| 43 | `Assets/Scripts/Tutorial/TutorialStageDirector.cs` | 开场台词只在真正进关卡时播（落地页不弹） |
| 44 | `Assets/Scripts/Tutorial/TutorialStageDirector.Beats.cs` | 去掉时停拍的全屏遮罩；冻结点改到命中帧 |
| 45 | `Assets/Scripts/Tutorial/TutorialStageDirector.Rules.cs` | 场景压暗每次重新绑定 |
| 46 | `Assets/Scripts/Tutorial/TutorialSceneDim.cs` | 精灵改走共享材质 `_Color` + 每帧重压 + 执行序 1000 |

---

## 13. 已知坑（这次踩到 / 规避）

1. **`unity_recompile` 报成功但 DLL 没更新** → 唯一判据是 DLL mtime 晚于源码 mtime（已按此核对）。
2. **编辑工具会丢改动** → 改完必须回读文件/资产确认（本轮每步都回读）。
3. **落地页 UI 重构把战斗对话遮罩关了**（10-25 `a6be228`）→ 阻塞式台词会让游戏永久停在 timeScale=0。**改落地页 UI 时要避开 `BattleHUD/Combat/Overlay/Dialogue/DialogueLayer` 下的三个对象**。
4. **敌人池是 Inspector 列表**，不是 Resources 扫描 → 新敌人预制体必须手工加进 `EnemyPool.enemyPrefabList`。
5. **TMP 的 SDF 字体没有中文字形** → 中文 UI 必须用 `方正粗黑宋简体 SDF`（`LiberationSans SDF` 只有拉丁字形）。
6. **timeScale=0 时 `InputManager` 完全不处理输入** → 冻结拍必须走原始输入轮询；解冻后要 `CancelCurrentGesture()`。
7. **冻结点必须落在"出伤窗口已开"**，否则死锁（窗口前冻结 → 永远格不开）。
8. **109 的 `tutorialHold` 只能在冲锋开始前上**；离场中必须禁止新冲锋（否则淡出途中再冲一次）。
9. **枚举存活敌人要先拷贝列表**（死亡会改列表）。
10. `.meta` 的 pivot 与 Unity 实际读出的 pivot 不一致（导入产物陈旧）→ 谁删 `.meta` 强制重导入，谁就要重算 `visualYOffset`。
