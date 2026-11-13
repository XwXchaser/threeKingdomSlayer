---
id: kd_a8dce347-4cee-4c5f-ae1a-efe88223955c
summary: 补齐与1051哑火的定位结论与修复（2026-11-09）：BUG A 根因=前导空排且队列从不做重排（第一排活人永远卡 row1）——已用配置修复（N2 wave1 交换 row0/row1，N2 实测 wave1 30 秒清完）；BUG B 根因=攻击 tween 与敌人 Transform 共用 DOTween target，被 transform.DOKill 连带杀掉→发箭回调静默消失——已修（攻击 tween 改用自身 target + 远程收尾兜底补发，实测 远程起手12=箭已发出9+兜底补发3）。含诊断日志速查与复跑方法。
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 交接：补齐（前方无敌人也不前移）+ 1051 不射箭（哑火）

> **给新对话的入口。** 这两个 BUG 本轮只做到"定位 + 取证"，**未修复**。工作区里已有 3 项已完成的修复（见 §1），接手时先确认它们还在。

---

## 1. 工作区已完成的改动（不要回退）

| 文件 | 内容 | 验证 |
|---|---|---|
| `Assets/Scripts/Enemy/EnemyCommander.cs` | 1013 督战：**补齐优先于号令**（有前移订单→打断鼓舞、走 Walk 补齐；移动中不起手） | 编译通过；用户实机通过 |
| `Assets/Scripts/Enemy/EnemyProjectile.cs` | 箭矢回收加固：淡出 tween 字段化 + **unscaled**、**真实时间 0.6s 硬销毁兜底**、安全兜底改 `WaitForSecondsRealtime`、加 3 条诊断日志 | 编译通过；用户报"似乎不会触发了" |
| `Assets/Scripts/Tutorial/TutorialStageDirector.Hints.cs` + `guide_move_stabc2_phase2.asset` + `TalentTree_Moves.asset` | stabC2 教学文本时机挪移（文本①→敌人抬手定格；文本②→阶段2 开始；打空不弹文本） | 编译通过；资产与接线已回读确认 |

**已按用户要求撤回**（`git diff` 应为空）：`Assets/Scripts/Wave/WaveSpawner.cs`、`Assets/Scripts/Managers/StageRouteBattleHost.cs`（原"波末清箭"方案，因不是箭矢残留的真因，不保留）。

**知识文档已更新**：`plan/enemy-commander-buff-unit-plan.md` §5.1 不变量 4 / §5.6 M10 / §5.0 增补。

---

## 2. BUG A｜补齐：前方没有敌人也不前移

### 现象
- 用户**真机**遇到，与冰冻无关（历史上 2026-11-08 的冰冻掉队是另一个已修问题，见 `memory/known-issues.md` #23）。
- 用户明确要求：**不改现有补齐规则**（"整排清空才补齐" + "留空排占位"都是既定机制）。

### 已确认的代码缺陷（都不是"改规则"，是"清空判定不准"）

1. **非死亡离场不会产生"整排清空"事务**
   `Assets/Scripts/Core/ColumnManager.cs:210-214` 的 `mayCreateWaveMarch` 要求 `enemy.state == EnemyState.Dead`：
   ```csharp
   bool mayCreateWaveMarch = _queue.UseRowMarchPlanner
       && enemy != null && !enemy.isBoss
       && enemy.state == EnemyState.Dead          // ← 只有"死了"才算
       && _queue.TryGetRowForEnemy(enemy, out clearedLogicalRow);
   ```
   ⇒ **109 骑兵"回退淡出离场"**（`Assets/Scripts/Enemy/CavalryEnemy.Exit.cs:70` → `EnemyManager.DespawnEnemy`）、教学 despawn 这类**非死亡移除**：
   - 它那一排**不会被 `MarkRowRemoved`** → 在 `LogicalRowQueue.ReassignRuntimeRows` 里仍是 **Live 排** → **永久占一个运行时行号** → 它后面的逻辑排**永远前移不了**；
   - 而且 `DespawnEnemy` 是先 `RemoveEnemyFromColumn` 再 `ReturnEnemy`（`Enemy.Pool.cs:82` 才把 state 置 Dead）→ 移除那一刻它**还被 `HasLivingMembers` 算作活人**。

2. **逻辑队列的注销侧从未被调用**
   `Assets/Scripts/Core/ColumnManager.cs:450` 的 `UnregisterWaveEnemy` → `LogicalRowQueue.UnregisterEnemy`（`LogicalRowQueue.cs:155`）**全项目零调用点**；而注册侧 `Assets/Scripts/Wave/WaveSpawner.cs:325` → `RegisterWaveEnemy` 在用。**配对缺一半**。

### 修法（不改规则，只修判定；待用户拍板）
1. `RemoveEnemyFromColumn` 里判断"这一排还有没有别人"时**排除正在被移除的这个敌人**（死亡路径行为不变，因为它本来就是 Dead）；
2. 移除时调用 `UnregisterWaveEnemy`（补上缺失的配对）；
3. `mayCreateWaveMarch` 去掉 `state == Dead` 限制，改由"该排是否还有别人"决定。

### 另一个独立问题：2 个波次的第一排活人不在 row0
扫描全部波次配置（`Assets/RouteData/**/*.asset`，enemyIds 是每列 4 字节小端 uint32）只找到 2 处：

| 文件 | 波 | 第一排活人行 |
|---|---|---|
| `Assets/RouteData/ChangbanpoScroll/Battles/Formal_N2_Battle.asset` | wave2 | **row1** = [1021@col1, 1011@col3]（row2 = 109@col2, 1011@col4） |
| `Assets/RouteData/Tutorial/Tutorial_Battle.asset` | wave4 | **row1** = [1012@col2] |

**硬阻塞链**：开波 `WaveSpawner.cs:194` `ConfigureWaveRowLayout(wave.rows, 0)` → 队列 `_dirty=false` → **开波不重排** → 前导空排被保留（规则0"开场按配置位置保留"）→ 它只在"前方有活人排死掉"时才移除，而它前面本来就没有活人排 → **永不移除** → 第一排活人**永远停在 row1**；
而玩家基础戳击 `AttackSkillConfig.rangeRows = 1`（`Assets/Scripts/Attack/StabSweepEffect.Hit.cs:92` `if (enemy.rowIndex >= _rangeRows) continue;`）→ **只打得到 row0** → 打不到 → 不产生死亡事务 → 不触发补齐 → **这一波推不动**。
（指向突刺 `Zhangfei_StabR2` rangeRows=2 / 挑飞收招 `LaunchFinisher` rangeRows=3 够得着 row1，但那要靠天赋解锁。）

**这一条可以纯改配置解决**（把第一排填上活人），不动代码。

### 未确认
- 真机那次到底是上面哪个（幽灵排 / 前导空排 / 击退留下的空位〔规则3 明确不传递，属设计〕）——**需要一条诊断日志**：在补齐决策处打印每个敌人的 `逻辑排ID / 排内活人数 / 每个成员的状态+rowIndex+column`。

---

## 3. BUG B｜1051 弓手"准备射箭时被补齐 → 走完动画但没箭"

### 现象
- 1051 在**前排**准备射击时，若此刻前排清空需要补齐，**射击动作动画走完，但没有箭射出**。
- 用户强调：**没有主动做打断行为**。
- 后排弓手正常（玩家够不着、不会被打断）。

### 已确认的代码事实（prefab + 日志实证）
1. `Assets/Resources/EnemyPrefabs/Enemy_1051.prefab` 的 attack step：**`spawnDuration: 2`**、`drawDuration: 1.5`、**`hitFrames: []`**、`isRanged: 1`。
2. **箭只由 tween 回调在攻击开始后 2.0 秒发出**（`Assets/Scripts/Enemy/Enemy.Attack.cs:294-301`）：
   ```csharp
   _attackTween.AppendInterval(spawnDuration);
   _attackTween.AppendCallback(() => { SpawnProjectile(); ... });
   ```
   日志印证：`命中帧 #0 触发 ... isRanged=True` 全程 **0 次**（全日志 62 次 `发射飞行物`）→ 1051 不走命中帧。
3. **远程箭没有任何兜底**：唯一兜底 `ResolvePendingHitFramesOnAttackEnd()`（`Assets/Scripts/Enemy/Enemy.HitFrame.cs:131`）第一行
   `if (_hitFrameSprites.Count == 0 || _hitFrameFiredAny) { ClearHitFrames(); return; }` → **1051 直接返回，不补发、不告警**。
4. **动画由 Animator 独立驱动** → 杀 tween 不会停动画 → 观感就是"动作走完但没箭"。

### 杀攻击 tween 的 4 条路径
| 路径 | 位置 | 动 Animator 吗 |
|---|---|---|
| `CancelAttack()`（受击 / 招架） | `Enemy.StateSwitch.cs:414`（受击入口 `Enemy.Combat.cs:112`） | `Play("Idle")` → 动画被切 |
| `Enemy.Stun()`（眩晕/破势） | `Enemy.StateSwitch.cs:197` | **不动** → 动画走完 |
| `Enemy.Launch()`（挑飞） | `Enemy.StateSwitch.cs:287` | 播挑飞动画 |
| `ResetMovementState()`（**补齐/移动**） | `Enemy.Visuals.cs:24`（调用点 `Enemy.Visuals.cs:225` `TryStartRushMove`） | **不动** → 动画走完 |

### 关键结论：**"补齐"在代码上不会吞箭**
`Enemy.Visuals.TryStartRushMove()` 的 `case EnemyState.Attacking` 只在 **`!isAttackAnimating`** 时才走 `ResetMovementState()`：
```csharp
case EnemyState.Attacking:
    if (!isAttackAnimating) { ResetMovementState(); pendingRushMove = true; StartMoving(true); ... }
    return RushMoveStartResult.Deferred;   // ← 动画进行中（含那 2 秒）走这里
```
⇒ 攻击动画期间补齐只会 `Deferred`（订单保留、动画继续、箭照发）；只有冷却期（箭已发出）才会杀 tween。
⇒ **所以"补齐打断射箭"这个归因不成立，触发者是别的东西**，且用户确认没有主动打断 → 大概率是**自动路径**（受击的 `CancelAttack` / 眩晕 / 冻结挂起后 tween 未恢复 / 看门狗 `ResetStalledAttack`）。

### 下一步（建议按顺序）
1. **先加 2 行日志（不改行为）**，复现一次指名凶手：
   - 远程攻击开始：`远程攻击开始，箭将在 2.0s 后发出`（带 `spawnDuration` / `enemyId` / `rowIndex`）
   - 攻击 tween 被杀/被打断：`攻击中断，箭未发出`（**带击杀来源与调用栈**）；落点建议 `Enemy.Attack.cs` 的 `OnKill` 回调 + `Enemy.Visuals.ResetMovementState` 的 Kill 分支 + `Enemy.StateSwitch.Stun/Launch/CancelAttack` 的 Kill 分支各打一处（或用 tween 的 `OnKill` 统一抓）。
2. 可选兜底：远程路径补"收尾兜底"——tween 结束而箭未发出就**补发一次**并记日志（与近战/命中帧兜底同一思路），敌人永不哑火。

---

## 4. 日志与取证速查

- 编辑器活日志：`C:/Users/Administrator/AppData/Local/Tuanjie/Editor/Editor.log`（**不是**工程内 `Logs/Editor.log`，那个是旧的）
- 打包产物：`Build/Android/threeKingdomSlayer_full_<时间戳>.apk`；状态：`Build/Android/build-status.txt`（含 result/errors/布局自检）
- 打包入口：菜单 `Tools/打包/Android（ARM64+X86，传统布局）`（`Assets/Scripts/Editor/AndroidBuildMenu.cs`）；IL2CPP + ARM64/X86，热缓存约 1m30s
- 补齐关键字：`[WaveMarch]`、`[FillUp] 豁免补齐`、`[Enemy] StartMoving`、`[Enemy] 移动完成`、`[RowTrace]`、`[WaveDiag] NotifyWaveCleared`、`[WaveSpawner] 第 N 波已清空`
- 箭矢关键字：`[Enemy] #NN(1051) 发射飞行物`、`[Enemy] 打断攻击成功`、`[Enemy] 命中帧`、`[EnemyProjectile]`（新增的 3 条诊断）
- 命中帧/打断：`[Enemy] 打断攻击成功: #xx(1051)` = 该弓手的攻击被受击取消

## 5. 已排除的猜想（别重复走弯路）

| 猜想 | 结论 |
|---|---|
| 箭矢残留是"波末三选一暂停冻住" | **不是**。探针实证：`ts=1 paused=False`，是 `DisposeProjectile()` 的**淡出 tween 在 ~0.117s 被掐断**（alpha 停在 0.782），`OnComplete` 不触发；且 3 秒兜底已被 `DisposeProjectile` 自己停掉。已用 unscaled + 硬销毁兜底修掉 |
| 箭矢残留是 `TimedArrowEffect`（玩家箭雨）误杀 | **不是**。它 `DOTween.Kill` 的是自己 spawn 的箭实例 |
| 箭矢残留是 hit-stop 直接杀淡出 | **不是**。hit-stop 只暂停射手自己的 `_attackTween`（`EnemyHitFeedback.PauseAttackTweenForSuspend`） |
| 1051 用命中帧发箭 | **不是**。`hitFrames: []`，日志 `isRanged=True` 的命中帧 0 次 |
| 补齐（`TryStartRushMove`）会杀正在进行的攻击 tween | **不会**。动画期间一律 `Deferred` |
| 106 敌人在 E0 三连并排 | **不是 106**。E0 三连并排的是 **1051**（wave3 row9 / wave4 row5 / wave5 row2,row9）；106 只出现在 `Formal_N3_Battle` |

## 6. 待用户拍板

1. BUG A 先做哪个：**改那两个波次配置**（零代码）／ **修幽灵排的两个代码缺陷**（不动规则）／ 先加诊断日志。
2. BUG B 先做哪个：**只加日志**／ **日志 + 远程兜底补发**。

---

## 7. 【2026-11-09 追加】机器人自检后的定位结论（本轮做完）

> 本轮选择：**只加诊断日志**（两个 BUG 都加），然后用 `BattleAutoPlayer` 在 Editor Play Mode 里跑实测。
> 结论已实测到根因，下一轮可直接进入修复。

### 7.1 本轮加的诊断（全部是 Editor-only，打包零开销）

| 文件 | 内容 |
|---|---|
| `Assets/Scripts/Core/FillUpDiag.cs`（**新增**） | 补齐诊断的静态出口：队列快照 / 排活人数 / 行军规划。所有方法标 `[System.Diagnostics.Conditional("UNITY_EDITOR")]` → **打包产物里整段被剥离** |
| `Assets/Scripts/Core/LogicalRowQueue.cs` | 新增 `HasLivingMembersExcluding()`；`Configure()` / `ReassignRuntimeRows()` 各加一条快照日志 |
| `Assets/Scripts/Core/ColumnManager.cs` | `RemoveEnemyFromColumn` 打印：该排活人数（现有判定 vs 排除本敌）、移除后是否**仍在队列登记**、是否已请求重排 |
| `Assets/Scripts/Core/WaveMarchScheduler.cs` | `TryBuildLogicalWaveMarchPlan` 逐个敌人打印「为什么派 / 为什么跳」+ 汇总 |
| `Assets/Scripts/Enemy/Enemy.Attack.cs` | 远程攻击起手打印「箭将在 Xs 后发出」；`OnKill` 打印「箭未发出」+ 凶手标签；**完整栈写 `Library/Locus/tmp/arrowkill.txt`**（Console 会截断长栈） |
| `Enemy.Combat/StateSwitch/Visuals/FormSwitch/QTEAttack/Pool.cs` | 10 处杀攻击 tween 的地方各加一行 `_attackTweenKillReason = "…"` 标签 |

> 定位完成后：删 `FillUpDiag.cs` + 各调用点即可（调用点都是单行）。

### 7.2 BUG A｜根因 = **前导空排 + 队列从不做重排**（已实测复现）

**实测过程**（N2 关 `Formal_N2_Battle`）：机器人 `attackInterval=1.0s`、血量拉到 99999、`StageController.StartRouteBattle(N2)`。
wave0 正常清完；**wave1 卡死**：4 个敌人 60 秒内血量纹丝不动（1021 435/435、1011 580/580）。

**运行时队列快照（决定性证据）**：
```
[Q0] kind=Empty init=0 cur=0 removed=False members=[(空)]          ← 前导空排，还在
[Q1] kind=Live  init=1 cur=1 removed=False members=[#5(1021)[row=1 col=1], #6(1011)[row=1 col=3]]
[Q2] kind=Live  init=2 cur=2 removed=False members=[#7(109)[row=1 col=2], #8(1011)[row=2 col=4]]
```
⇒ 第一排活人卡在 **row1**，玩家基础戳击 `rangeRows=1` 只打 row0 → **这一波永远推不动**。

**因果链**：
1. `WaveSpawner.cs:194 ConfigureWaveRowLayout(wave.rows, 0)` → `LogicalRowQueue.Configure()` → 建队列后 **`_dirty=false`**；
2. `ReassignRuntimeRows()` 里「前导空排在**前方无活人排**时移除」这条规则**是对的**，但它**只在 `_dirty` 时才会被调用**；
3. wave1 的 Q1/Q2 一次都没被清空过 → `_dirty` 一直 false → **开波后一次重排都没发生** → Q0 这个前导空排**永远保留** → Q1.cur 永远 = 1。

**⇒ 排除了「幽灵排」这个原因**：4 个敌人的逻辑排归属全部正确，没有错挂或残留。
`UnregisterWaveEnemy` 零调用点这条**在本波里没有造成影响**（但仍是隐患，见下）。

**配置侧的来源**：工作区未提交的改动把 109 从 row0 挪到了 row2，**顺带把 row0 留成了空排**：
`git diff Assets/RouteData/ChangbanpoScroll/Battles/Formal_N2_Battle.asset` → wave1 row0 由 `109@col0` 变成全 0。
（`Tutorial_Battle.asset` wave4 的前导空排是**已提交**状态，未被本次改动影响。）

**修复选项（下一轮拍板）**：
- (a) **改配置**：wave1 row0 填上活人（把 109 放回 row0，或换一个）→ 零代码；
- (b) **改规则**：开波时也重排一次 → **违反**「开场按配置位置保留」（`design/percolumn-fillup-rules.md` 规则0），用户明确要求不改规则；
- (c) 让「前导空排」在开波时就判定为「前方无活人排」→ 等于 (b)，同样改规则。

### 7.3 BUG B｜根因 = **攻击 tween 与敌人 Transform 共用 DOTween target，被连带杀掉**

**实测过程**（E0 关 `Formal_E0_Battle`，wave2 有 1051 弓手）：机器人 `attackInterval=1.0/1.5s`。
多次跑批共 **17 次「箭未发出」**（`Library/Locus/tmp/arrowkill.txt`）。

**机制（三条合起来才成立）**：
1. 远程箭**只**由攻击 tween 的回调在 `spawnDuration=2s` 后发出（`Enemy.Attack.cs` `AppendInterval(spawnDuration)` + `AppendCallback(SpawnProjectile)`）；
   `hitFrames: []` → `ResolvePendingHitFramesOnAttackEnd` 直接 return → **没有任何兜底**。
2. **攻击 tween 的 DOTween target 就是敌人的 Transform**（`_attackTween.SetTarget(transform)`）→
   代码里所有「清理位移 / 缩放 tween」的调用（`transform.DOKill(false)` / `DOTween.Kill(transform, false)`）
   **都会连带把攻击 tween 一起杀掉** → 那条「2 秒后发箭」的回调**静默消失** → 哑火。
   实测凶手标签统计：**9/10 次 = `取消攻击（transform.DOKill 连带杀）`**，其余为同机制的其它调用点。
3. Animator 由 Animator 独立驱动 → DOTween 杀它管不着 → **观感就是「射击动作动画走完，但没有箭射出」**。
   尤其 `Stun()`（眩晕/破势）与 `ResetMovementState()`（补齐/移动重置）**不碰 Animator** → 与用户描述**完全一致**。

**⇒ 交接文档 §3 的「补齐在代码上不会吞箭」仍然成立**：
`TryStartRushMove` 的 `case EnemyState.Attacking` 在 `isAttackAnimating` 为真时走 `Deferred`（不杀 tween），
所以**拉弓窗口内的补齐本身不会吞箭**；实测中也没出现 `重置移动状态（补齐/移动）` 这个凶手标签。
用户看到的「前排清空需要补齐」更像是**时间上的巧合**（补齐把敌人挪到眼前，恰好看到它没箭），真凶是上面的连带杀。

**修复方向（下一轮拍板，任一即可根治）**：
- (a) **给攻击 tween 一个独立 target**（不要用 Transform）→ `DOTween.Kill(transform)` 再也碰不到它；
- (b) **远程路径加「收尾兜底补发」**：攻击结束时若箭还没发出就补发一次（与近战命中帧兜底同一思路）；
- (c) 两者都做（推荐：a 治本，b 兜底）。

### 7.4 诊断本身的两个坑（已修/已记）

- **DOTween 会池化复用 `Sequence` 对象**：Kill 掉之后再 `DOTween.Sequence()` 会拿到**同一个对象**，
  且它**仍带着旧的 OnKill 回调** → 会在下一次被 Kill 时串台触发别人的回调。
  → 诊断回调必须带**序号护栏**（本项目用 `_attackTweenSerial`，序号不匹配就忽略），否则会报出假线索。
- **OnKill 不是 Kill 的瞬间触发**：实测杀掉攻击 tween 时 OnKill 没有当场触发，
  它延后到该对象被池回收后**再次被 Kill** 时才触发（栈顶是 `DamageNumber` 的安全超时，与真凶无关）。
  → **靠 OnKill 的调用栈找不到真凶，必须在每个 Kill 点打标签**（本项目已加 `_attackTweenKillReason`）。
- 详见 `memory/dotween-destroyed-transform.md`（2026-11-09 补充段）。

### 7.5 机器人跑测方法（可复用）

```csharp
// Editor Play Mode 里直接开指定关卡的战斗（绕开路线系统）
var sc   = UnityEngine.Object.FindObjectOfType<StageController>();
var cfg  = UnityEditor.AssetDatabase.LoadAssetAtPath<StageConfig>(
               "Assets/RouteData/ChangbanpoScroll/Battles/Formal_N2_Battle.asset");
sc.StartRouteBattle(cfg);

// 机器人：降低攻击频率 + 拉高血量
var go = new GameObject("BattleAutoPlayer(诊断)");
go.SetActive(false);                       // 先 inactive 再 AddComponent，避免 autoStart 抢跑
var bot = go.AddComponent<BattleAutoPlayer>();
bot.autoStart = true;
bot.attackInterval = 1.0f;                 // 降低攻击频率 → 敌人有更多时间处在"拉弓窗口"
bot.useAreaAttacks = bot.useHeavyAttacks = false;
bot.bossQTEMode = BattleAutoPlayer.BossQTEMode.Suppress;
go.SetActive(true);
PlayerState.Instance.currentHealth = 99999f;  // 机器人不死，才能一直跑到复现
```
- 血量要在循环里每 2 秒续一次（`currentHealth` 是 public 非序列化字段）。
- Console 只留最近约 30 条 → **长日志要自己收集**（`ctx.GetConsoleLog` 在脚本里轮询 + 过滤）或写文件。
- 复现两个 BUG 的关卡：BUG A = `Formal_N2_Battle` wave1；BUG B = `Formal_E0_Battle` wave2（1051 弓手）。

---

## 8. 【2026-11-09】两个 BUG 的修复（已实施并验证）

### 8.1 BUG A 修复：改波次配置（零代码，不改任何规则）

**改的是什么**：`Assets/RouteData/ChangbanpoScroll/Battles/Formal_N2_Battle.asset` → wave1（第二波）的**三排敌人站位**。

```
改前（不可打）                        改后（可打）
row0  .   .   .   .   .   ← 空排       row0  . 1021 . 1011 .   ← 前排活人，打得到
row1  . 1021 . 1011 .                 row1  .   .   .   .   .   ← 空排占位（规则里的「空1」）
row2  .   . 109 . 1011               row2  .   . 109 . 1011   ← 109 保留在第 2 排（它的冲锋发起排）
```
- 敌人**一个没加没减**，只是把「空的第 0 排」和「有活人的第 1 排」对调。
- 109 骑兵**仍然留在第 2 排**（新骑兵规则要求它从第 2 排发起冲锋，见 `CavalryEnemy.ChargeAttackRow`）。
- 空排挪到中间后，它就是规则文档 `design/percolumn-fillup-rules.md` 里的 `A0 空1 B2` 标准形态：
  被前方活人排保护 → 随队列前移 → 前方清空后作为前导空排被移除。
- **没有改任何代码或规则**。

**为什么原来不可打**：玩家基础戳击 `rangeRows=1` → **只打得到第 0 排**。
第 0 排是空的 → 没有任何敌人能被击杀 → 永远不会发生「整排清空」→ 队列从不做重排 → 空排永远保留 → 第一排活人永远卡在 row1。

**验证**：机器人跑 N2，**wave1 从「60 秒零伤害、纹丝不动」变成 30 秒清完**，随后正常进入 boss 波（104）。E0 全关打通。

**遗留（未处理，属于工作区骑兵改造的范围）**：骑乘态 109 的行军跳过条件是 `enemy.rowIndex <= targetRow`
（`Assets/Scripts/Core/WaveMarchScheduler.cs`），而骑兵的 `targetRow = max(冲锋发起排 2, 逻辑排号)` →
骑兵一旦被推到第 0 排就**永远不会被派单挪回第 2 排**，而 `CavalryEnemy.TryStartCharge` 要求 `rowIndex == 2` → **它会停在第 0 排不冲锋**。
实测能看到 109 在 row0/row1/row2 之间来回。**要不要修、怎么修，等用户拍板**（不在本轮范围）。

### 8.2 BUG B 修复：攻击 tween 独立 target + 远程收尾兜底补发

**改了什么**（代码）：

| 文件 | 改动 |
|---|---|
| `Assets/Scripts/Enemy/Enemy.Attack.cs` | ① `_attackTween.SetTarget(transform)` → **`SetTarget(this)`**（攻击 tween 不再与位移/缩放 tween 共用 Transform）；近战翻转的 `DOScaleX` 子 tween 同样改用 `this`。<br>② 新增 `CheckRangedArrowFallback()`：**箭还待发 且 攻击 tween 已不在活动状态** → **补发一次**（敌人永不哑火）。 |
| `Assets/Scripts/Enemy/Enemy.cs` | `Update()` 每帧调用 `CheckRangedArrowFallback()`。 |
| `Assets/Scripts/Enemy/Enemy.Pool.cs` / `Enemy.Init.cs` / `Enemy.Combat.cs` / `Enemy.Boss.cs` | 攻击 tween 改了 target → 原来靠 `transform.DOKill()` 顺带清掉它的地方**必须显式杀**：回池 / 重置 / 取消死亡动画 / Boss 暂停各加一行 `_attackTween?.Kill(false)`。 |

**为什么这样修**：
- 治本：攻击 tween 用**自己**当 target → 代码里任何「清理位移 tween」的 `transform.DOKill(false)` / `DOTween.Kill(transform, false)`
  （CancelAttack / ResetMovementState / Stun / Launch / 死亡清理 …）**再也碰不到攻击动画** → 那条「N 秒后发箭」的回调不会静默消失。
- 兜底：万一还有别的路径掐断攻击，**轮询**（不是 OnKill 回调——它的时机不可靠）发现「箭还待发且 tween 已死」就补发一次。

**语义变化（要知道）**：**被打断的远程敌人现在也会把箭射出来**（受击/眩晕/挑飞都算）。
这是有意为之（对应交接文档 §3「敌人永不哑火」的建议），如果只想让「非受击打断」补发，把 `CheckRangedArrowFallback` 里的补发条件收窄即可。

**验证（机器人跑 E0）**：
```
远程攻击起手 = 12
箭已发出     =  9
兜底补发     =  3     → 9 + 3 = 12 = 全部起手都有箭，哑火清零
E0 全关打通（allWavesCompleted = True），Console 0 error
```

