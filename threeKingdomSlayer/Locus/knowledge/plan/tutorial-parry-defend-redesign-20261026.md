---
id: kd_564cca38-e16a-427c-b33a-6426aa1321f3
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 教学关流程改造：招架 / 防御 / 109 离场（2026-10-26 定稿）

> 本文件是**执行方案**，不是设计讨论记录。所有"用户口径"条目均为 2026-10-26 对话中用户明确拍板。
> 关联：`design/attack-interrupt-system.md`（§3 打断矩阵 / §4.6–4.8 招架与防御）、
> `plan/parry-guard-rework-handoff-20261026.md`（招架动作与防御姿态的现状）、
> `plan/enemy1012-asset-pull-20261026.md`（1012 资产来源与坑）、
> `plan/tutorial-and-formal-stage-handoff-20261019.md`（教学关既有流程）。
> **实现级方案见 `plan/tutorial-gesture-ui-implementation-20261026.md`**（含现有教学系统解剖 + 逐文件改动）。

---

## 0. 一句话

教学关从"教点戳 / 横划 / **蓄力上挑破盾** / 打 109"改成
"教点戳 / 横划 / **招架格开霸体攻击** / **招架后按住＝防御，扛住 109 冲锋**"，
109 不再被杀、而是**回退淡出离场**，教学关以**胜利**收尾回落地页；招架不再是局外天赋项。

---

## 1. 用户口径（定稿，逐条）

| # | 决定 | 落地含义 |
|---|---|---|
| 1 | **取消 `talent_unlock_parry` 天赋节点** | 招架变基础招式；教学关就能用；"本来就不应该在这里" |
| 2 | 招架拍用**完全冻结**（timeScale=0）教手势 | 沿用旧门槛 `TutorialGestureHint.Gate()` 的原始输入轮询思路 |
| 3 | **盾兵 1021 从教学关消失**（不是"被替换"，是那个波次位置空出来放 1012） | 破盾/上挑这条线教学关**完全不教** |
| 4 | 霸体教学敌人 = **教学专用 1012**（新形象，机制数值同 1011） | 1012 攻击步加 `isCAttack`；命中帧要修正（见 §5.2） |
| 5 | 防御拍**不存在失败**：玩家按住 → 教学**临时锁定防御** → 直到被 109 攻击并成功防御 → 之后清除锁定 | 需要"强制按住"开关；破防不教 |
| 6 | **不教破防**，只教"如何进入防御" | 破防条只作为吸收量的可视反馈 |
| 7 | 109 **不计击杀**；本轮只把教学关跑通，其它（铜钱总数等）先不算 | 离场走"非死亡移除"通路 |
| 8 | 正式关**不再教招架** | |
| 9 | 正式关教**连段 C2**（`Zhangfei_StabR2` 衍生）：教学关通关后玩家去**天赋树解锁**该招式 | 天赋节点 `talent_unlock_c2` 已在（100 铜钱） |
| 10 | 正式关**初次进入**出现教学：**一个拍**同时教「用重攻击打断霸体敌人的攻击」，重攻击即 C2 | 需要新的复合手势提示（Tap→按住→横划改列→松手） |

补充口径（对话中确认）：

- 冻结拍**没有失败态**：冻结 + 手势示范循环，玩家做到为止；"做不到"就是实现没卡好，不是设计里的失败分支。
- 正式关的霸体敌人**也用 1012**。

---

## 2. 现状事实（代码/资产实测，方案的事实底座）

### 2.1 教学关现状

| 项 | 实测值 | 位置 |
|---|---|---|
| 路线 | `Tutorial_Route`（routeId `tutorial_route` / stageId 9100 / clearCoinReward 250）→ 节点 N1 → 节点 E0（`isFinalNode=true`, `savePoint=true`） | `Assets/RouteData/Tutorial/Tutorial_Route.asset`、`Tutorial_Node_E0.asset` |
| N1 战斗 | `Tutorial_N1_Battle`：波0 = 1011×1（列2），波1 = 1011×3，`damageMultiplier=0.3` | `Tutorial_N1_Battle.asset` |
| E0 战斗 | `Tutorial_E0_Battle`：波0 = **1021**（列2，damageMult 0.3）；波1 = **109**（第 1 排，damageMult 1） | `Tutorial_E0_Battle.asset` |
| 手势拍 | ①N1波0 Tap ②N1波1 Swipe(横) ③**E0波0 ChargedSwipe(=Launch，破盾)** ④E0波1 `None`+登场台词 ⑤出口 Tap | `Battle.scene` → `Tutorial Director.gestureSteps` |
| 拍型机制 | 冻敌 `Enemy.tutorialHold` + 压暗场景 + 输入闸门只放行目标手势 + 示范循环；玩家真手势走 InputManager → `OnAttackExecuted` 收拍 | `TutorialStageDirector.Hints.cs:208` `BeginBeat/EndBeat` |
| 旧冻结门槛 | `TutorialGestureHint.Gate()`：timeScale=0 + 原始输入轮询 + 解冻后重放手势。**代码仍在，导演已不调用** | `TutorialGestureHint.cs:82` |
| 剧情杀 | **已不存在**：`StageController.suppressDefeatUI` 场景里为 false；`CavalryEnemy.OverrideChargeDamageForScript()` 与台词 `tutorial_t0_death` 均无引用（孤儿） | 实测 |
| 109 数值 | maxHealth 400、`chargeDamage=20`、windup 0.5、strikeWindup 0.5、strikeRecover 0.8、chargeCooldown 3、retreatStep 0.6、retreatTurn 0.4 | `Enemy_109.prefab` |
| 109 冲锋伤害 | `20 × 冲锋格数(发起排+1=2) × 波次倍率(1)` = **40/次**；玩家 100 血 | `CavalryEnemy.ChargeDamage` |
| 1012 数值 | maxHealth 100、attackDamage **3**、attackSpeed 0.5、spawnDuration 2、`parryStrikeWindow=0.15` | `Enemy_1012.prefab` |

### 2.2 招架 / 防御链路

- 招架 = **不蓄力竖滑** → `AttackType.Parry` → `Assets/Prefabs/UI/Skills/Zhangfei_Parry.asset`：
  `damage=0 / damageType=Poise / poiseDamage=30 / cooldown=0.5 / actionDuration=0.48 / parryWhiffDuration=0.9`；
  姿态：挥入 0.18 + 过冲 8° + 回弹 0.08 + 定格 0.10 →（按住）防御位 0.12 →（松手）收枪 0.12（格挡成功）/ 0.54。
- **招架被天赋锁着**：`AttackSystem.cs:314-324` → `TalentTreeState.IsMoveGatedAndLocked(tree, Zhangfei_Parry)` → 未买 `talent_unlock_parry` 则竖滑**直接不出招**。
- **招架只在出伤窗口生效**：`Enemy.IsInStrikeWindow()`（`Enemy.cs:267`）= 出伤前 `parryStrikeWindow`(0.15s) 内；窗口外招架**不断前摇**。命中窗口 → `CancelAttack()` → 该击不结算。
- **防御没有独立入口**：只能由招架派生（`PlayerWeaponStance.AfterSwingIn` → `EnterDefend`）。两种按法：
  ① 竖滑后**不松手**（滑动在按住中被识别 → 招架带着"手指仍按住"执行）；
  ② 竖滑松手后**在 0.36s 内重新按住**。
- 防御期间：免伤、伤害只涨破防值（阈值 120 / 每秒回落 30 / 破防硬直 1.5s）、**手势全吞**（要攻击必须先松手收枪）。HUD 已有破防条（`HeroHUD.GuardMeter` + `BattleHUD` 每帧轮询）。
- 109 冲锋**招架打不断**（骑兵不在 `EnemyState.Attacking`，不吃 `isParryInterrupt`；能断冲锋的只有 `interruptsCavalryCharge` 的重攻击）→ **防御是唯一解**。

### 2.3 时停（完全冻结）的可行性与硬约束

| 事实 | 后果 |
|---|---|
| `InputManager.Update()` 开头 `if (!gameplayInputEnabled \|\| Time.timeScale == 0f) return;` | **冻结期间整条游戏输入通道不响应** → 必须在冻结中用**原始输入轮询**（`TutorialGestureHint.PollRawInput`，读 unscaled 时间，冻结时照常工作） |
| 敌人攻击 tween 用 `DOTween.Sequence().SetUpdate(UpdateType.Normal)` | 冻结 → tween 停 → `IsInStrikeWindow()` 的判定值**冻结在原地** |
| `Enemy.CancelAttack()` 是即时清理（Kill tween + 播 Idle） | 冻结中取消攻击也成立 |
| `PlayerWeaponStance.Tick(Time.deltaTime, pointerHeld)` | 冻结时**玩家持枪动画也停**（挥入不会推进） |
| `GameplayPause` 体系 | timeScale=0 时 unscaled 的游戏内容 tween 也会被冻结，UI 照常 → 冻结期遮罩/手势提示可交互 |

### 2.4 1012 的已知问题（2026-10-26 已实测校验，详见 `plan/enemy1012-asset-pull-20261026.md` §8）

- 1012 是 2026-10-26 由 `Enemy_1011.prefab` 复制而来（美术/动画时序与 1011 完全一致，Attack clip 2.9333s / 18 帧）。
- **命中帧没跟着改**：prefab 里仍是 `spriteName: Enemy_1011_attack8`（1011 的精灵名）→ 1012 自己的 clip 是 `Enemy_1012_Attack.anim`（`Enemy_1012_attack1..16`）
  → 命中帧永远匹配不上 → 出伤被推到攻击动画末尾（`ResolvePendingHitFramesOnAttackEnd`）→ **招架窗口整体错位**。
  修法：改成 **`Enemy_1012_attack6`**（实测：1011 的大白弧峰值在 a8，**1012 在 a6**，整套 attack 比 1011 早 ~2 帧），
  keyIndex/hitTime 用 `Tools ▸ 三国杀戮 ▸ 校验攻击命中帧` 回填。
- **脚底会浮空 0.14 世界单位**：prefab 的 `visualYOffset` 是为 1011 的 idle1 调的（1011 idle1 脚底正好在地面线 −1.8），
  1012 用同值 → idle1 脚底 −1.660（≈14px 浮空）→ 改成 **0.001**（见 §5.2）。
- **单位尺寸没问题**：walk / 命中帧的世界身高与 1011 差 ≤0.2%；idle 矮 12% 是姿态差（1011 举剑）→ **不要缩放去对齐**。
- 1012 **尚未接入任何波次**（本轮首次接线）。
- 1012 的 controller 复制自 1011，**没有 `CAttack` 状态** → 霸体步的 `animationTrigger` 必须显式填 `"Attack"`
  （代码在 `animationTrigger` 为空时会去解析 `"CAttack"` trigger，解析不到 → 走空档 + 报错）。

### 2.5 "敌人不死亡地离场"目前没有通路

- `EnemyManager.allAliveEnemies` 只在 `OnEnemyDied` 里移除；波次完成判据是 `EnemyManager.IsAllEnemiesDead`（存活列表为空）。
- `EnemyPool.ReturnEnemy()` 会 `ResetEnemy()` + 挂回池；正常路径由死亡协程调用。
- → 需要新增"**非死亡移除**"：`DespawnEnemy(Enemy)`：移出存活列表 + 移出列（`skipChain: true`）+ **不触发** `OnAnyEnemyDied`（不计击杀 / 经验 / 铜钱 / 掉落）+ 回池。

---

## 3. 新流程

### 3.1 教学关（4 拍 → 6 拍）

```
N1 波0   1011      → ①教「点按」（不变）
N1 波1   1011×3    → ②教「横划」（不变）
E0 波0   1012(霸体) → ③教「招架」：时停 → 上划 → 格开霸体攻击
E0 波1   109       → ④教「防御」：上划并按住 → 扛住冲锋 → 109 淡出离场
终点结算 → 胜利 → 回落地页（Hub）
```

**③ 招架拍（完全冻结）**

1. 1012 走到前排起手；霸体步 `isCAttack` → 前摇有**红描边**（霸体可视信号），出伤窗口 0.15s。
2. 导演逐帧监视目标敌人；**当 `IsInStrikeWindow()` 变为真时**（窗口 0.15s ≈ 9 帧，逐帧轮询足够）→ **冻结** `Time.timeScale = 0`（并每帧压住，防 `DialogueManager.Finish()` 把 timeScale 顶回 1）。
   > 必须**在窗口打开的那一刻**冻结：窗口一旦打开就因冻结而**永不关闭** → 玩家在这段时间内任何一次上划都必然落在窗口里。若在窗口之前冻结，`timeToStrike` 会被冻在窗口外 → 永远格不开 → 死锁。
3. 弹出「上划」手势示范循环 + 遮罩（沿用 `TutorialGestureHint` 的图层与 `PollRawInput`）。
4. 玩家上划 → 原始输入捕获 → **解冻并同帧派发招架**：
   `AttackSystem.TryExecuteAttack(AttackType.Parry, -1, …, cancelCurrentMove: true)`
   - 同帧解冻+派发：解冻后 tween 每帧只推进 ~16ms，而剩余出伤时间 ≥ 0.15s → **无论 DOTween 更新在 MonoBehaviour 更新之前还是之后，招架都先于出伤生效**（余量 ≈ 9 帧）。
   - `cancelCurrentMove: true`：绕开动作锁（上一拍收尾可能还锁着）。
   - 派发后调 `InputManager.CancelCurrentGesture()`：冻结期间 `GetMouseButtonDown` 被错过，抬起不应再被判成手势。
5. 派发成功 → 1012 的攻击被 `CancelAttack()` → **这一击不结算伤害**（玩家 HP 不变）→ 1012 回到 Idle → 玩家用点戳收掉它（1012 100 血，教学波次建议 `healthMultiplier=0.4` → 40 血，两下点戳）。
6. 招架的挥枪动画在解冻后播完（0.36s 挥入 + 定格 + 加速收枪 0.12s）。

> 视觉备选（不做，先按上面）：若要"枪在冻结里就挥到位"，需给 `PlayerWeaponStance` 加一个"冻结期用 unscaled 计时"的开关。

**④ 防御拍（不冻结，109 必须真冲锋）**

1. 109 入场（第 1 排）→ 导演对它置 `tutorialHold`：**按住不动**（`CavalryEnemy.StepPaused()` → `_enemy.IsActionPaused` 含 `tutorialHold` → 冲锋计时不走）。
   > 注意：**必须在冲锋开始前**按住。冲锋开始后再上 `tutorialHold` 会被判为"冻结中冲锋"→ 冲锋作废直接返航。
2. 提示「上划并按住」（手势示范循环：手指快速上划 → 停在原地 + 光圈 = 别松手）。
   > 示范必须是**快速上划**（≈0.2s 内完成）：游戏里"按住中划动"的识别有 `minSwipeSpeed=180px/s` + `maxSwipeDuration=0.25s` 门限，慢拖会走"松手才出招"那条路。
   > 成功判据用 `PlayerWeaponStance.IsDefending`（与按法无关，两条按法都算过）。
3. 玩家上划并按住 → 招架执行（对 109 是空放：动作锁 0.9s，不影响防御）→ 挥入 0.36s → 落防御位。
4. 导演检测到 `IsDefending` → 打开**强制按住**（教学临时锁定）→ 放开 109 的 `tutorialHold`。
5. 109 冲锋（windup 0.5 + 前移 0.35 + strikeWindup 0.5 ≈ 1.4s）→ 命中玩家 → `PlayerGuard.TryAbsorbDamage(40)` 吃掉 → **玩家 HP 不变**，破防条 +40/120（不破防）。
6. 导演检测到"吸收了一次伤害" → **关掉强制按住** → 玩家可正常松手收枪。
7. 109 走它自己的返航流程（转身 0.4 + 每排 0.6）→ 返航途中**淡出**（马 + 骑手两个 renderer 的 alpha）→ 调 `EnemyManager.DespawnEnemy()` → 移出存活列表与列（不计击杀）→ 回池。
8. 存活列表清空 → 波次完成 → E0 是终点节点 → `FinishRoute` → 胜利结算（250 + 击杀铜钱）→ `AdvanceActiveStageAfterClear` → 回落地页。

### 3.2 正式关首次进入（一个拍，同时教 C2 与"重攻击破霸体"）

- 前置：教学关通关 → 落地页 → 天赋树买 `talent_unlock_c2`（100 铜钱，指向 `Zhangfei_StabR2`）。
- 正式关首入 → 一个拍：**1012 带霸体起手** → 提示 C2 的复合手势（点一下 → 按住蓄力 → 横划改列 → 松手）→ 打出 C2（`isHeavy=true`, `poiseDamage=30`, `rows=2`）→ **霸体被打断**（重攻击在霸体期也能打断）→ 削韧 30。
- 与教学关分开的两件事（工作量在正式关这一侧，不在教学关）：
  1. **手势提示要新增一种复合形态**：现有 `GestureHintKind` 只有 Tap / Hold / Swipe / ChargedSwipe，表达不了"点一下→按住→横划→松手"，需要新的示范动画与判定。
  2. **需要一个正式关的教学导演**：现在 `TutorialStageDirector` 只在 `routeConfig == tutorialRouteConfig` 时生效 → 要么给它加"按路线配置绑定"，要么加第二个组件。

---

## 4. 实施批次

| 批次 | 内容 | 风险 |
|---|---|---|
| **0 前置** | ① `TalentTree_Main.asset` 删 `talent_unlock_parry` 节点（9→8）② 1012 命中帧改 `Enemy_1012_attack6` 并用工具回填 keyIndex/hitTime ③ 1012 攻击步 `isCAttack: true` + `animationTrigger: "Attack"` ④ 1012 `visualYOffset` 改 0.001（防浮空）⑤ 落地页"清除存档"按钮与选关页标签换中文字体（教学关可进入性） | 低（资产层） |
| **1 机制底座** | ④ `Enemy.TimeToStrike()`（公开出伤倒计时，导演据此判窗口）⑤ `InputManager.forcePointerHeld`（`PointerHeld` 读它）⑥ `PlayerGuard` 暴露"吸收伤害"事件/计数 ⑦ `EnemyManager.DespawnEnemy(Enemy)` ⑧ `CavalryEnemy` 离场流程（返航→淡出→Despawn） | 中（战斗层公共代码） |
| **2 教学拍** | ⑨ `TutorialGestureHint` 新增 `Parry` / `ParryHold` 两种手势种类 + 示范动画 + 原始输入竖划判定 ⑩ `InputManager.tutorialGestureTarget` 新增 `ParryVertical`（只放行不蓄力竖滑，挡掉其它手势）⑪ `TutorialStageDirector` 新增两种拍型：时停招架拍 / 防御拍 | 中 |
| **3 资产与文案** | ⑫ `Tutorial_E0_Battle.asset`：波0 `1021→1012`、`healthMultiplier=0.4` ⑬ 台词重写：`tutorial_t0_wave3`（破盾→招架）、`tutorial_t0_wave4`（→防御）、`tutorial_t0_death`（孤儿→胜利收尾）⑭ `Battle.scene` 的 `gestureSteps` 重配 | 低 |
| **4 正式关首入** | ⑮ C2 复合手势提示 ⑯ 正式关教学拍（1012 霸体 + C2 破霸体）⑰ 教学导演泛化/第二个组件 | 高（工作量大，单独立项） |

---

## 5. 逐项改动说明

### 5.1 天赋门控（批次 0）

- 删 `Assets/ScriptableObjects/Talents/TalentTree_Main.asset` 的 `talent_unlock_parry` 节点 → `IsMoveGatedAndLocked` 找不到配对节点 → 招架恒放行。
- 旧存档里买过该节点的玩家：解锁标记成为孤儿，无副作用（节点不在树里，`TalentTreeState` 不会因此报错）。
- 文档同步：`design/attack-interrupt-system.md` §4.7.4、`design/out-of-game-growth-system.md`（"招架 = 局外天赋解锁项"的表述要改成"基础招式"）；
  `plan/parry-guard-rework-handoff-20261026.md` §7 的第 1 条要标注已被本轮推翻。

### 5.2 1012 霸体化（批次 0）

> 1012 的美术此前**未做规格校验**，2026-10-26 已按规范实测完毕，结论见
> `plan/enemy1012-asset-pull-20261026.md` §8（含“单位尺寸 / 脚底锚点 / 命中帧取哪一帧”的实测表）。

| 字段 | 现状 | 改为 | 原因 |
|---|---|---|---|
| `attackSequence[0].isCAttack` | false | **true** | 前摇进霸体窗口（`isCFrame`）+ 红描边 → "这敌人有霸体"的可视信号；也满足"普通攻击打不断他，招架才是解"的读法 |
| `attackSequence[0].animationTrigger` | 空 | **`"Attack"`** | 为空时代码会去解析 `"CAttack"` trigger（controller 无此状态）→ 解析失败走空档并报错；显式填 `Attack` 后 clip 正常解析 |
| `attackSequence[0].hitFrames[0].spriteName` | `Enemy_1011_attack8` | **`Enemy_1012_attack6`** | 1011 抄来的名字既错角色也错帧：1011 的大白弧峰值在 a8，**1012 的峰值在 a6**（整套 attack 比 1011 早 ~2 帧）；不改则命中帧永远匹配不上 → 出伤被推到动画末尾 → 招架窗口整体错位 |
| `hitFrames[0].keyIndex / hitTime` | 8 / 2 | 用 `Tools ▸ 三国杀戮 ▸ 校验攻击命中帧` 回填（预期 6 / 对应时刻） | 以工具读出为准 |
| `visualYOffset` | 0.13886152（从 1011 照抄） | **0.001** | 该值是为 **1011 的 idle1** 调的（1011 idle1 脚底正好在地面基准线 −1.8）；1012 用同值会**浮空 0.14 世界单位**（≈14px）。实测 `v = −1.8 − minY×rootScale = +0.00095` |
| `parryStrikeWindow` | 0.15 | **不动** | 冻结已等于"无限窗口"，不为教学去动平衡数值 |
| 其余数值 | 照抄 1011 | 不动 | 用户口径"机制和数值是一致的" |

> **单位尺寸结论：1012 不比 1011 大**。walk / 命中帧的世界身高与 1011 差 ≤0.2%（3.24u vs 3.24u / 3.76u vs 3.67u）；
> idle 矮 12% 是**姿态差**（1011 的 idle 把剑举高），不是画得小 → **不要用缩放去"对齐"**（会破坏与 1011 的一致性）。
> 1012 的 idle 脚底 6 帧完全锁定（画布像素全 = 898），比 1011 的 idle 更稳。
> `EnemyGroundAligner`（运行时自动对齐）**没挂在任何敌人 prefab 上** → prefab 手填值是唯一控制项。
> 注意：1012/1011 的 .meta 写的自定义 pivot 与 Unity 实际读出的画布中心 pivot **不一致**（导入产物陈旧）；
> 若有人删 .meta 强制重导入，两者 pivot 会一起变 → `visualYOffset` 需要重算。

### 5.2b 教学关可进入性（批次 0，与 1012 同批，测试前提）

| 问题 | 实测根因 | 修法 |
|---|---|---|
| 落地页左下角那个**乱码按钮** | 就是**清除存档**按钮（`HubDebugSaveButton`，父节点 "Debug"，位置 (27,27)）；乱码是因为 TMP 字体用了 `LiberationSans SDF`（无中文字形，`HasCharacter` 全 false，atlas 250 个拉丁字形） | 把该 TMP 的字体换成 `Assets/Fonts/方正粗黑宋简体 SDF.asset` |
| 教学关"进不去" | 存档里 `activeFakeRouteStageId = 1`（长坂坡·卷轴），而 `tutorialCompleted = false` → 落地页自举进的是正式关（`FakeRouteRuntime.ResolveActiveRouteFromSave`） | 两条路都通：①点清除存档 → 存档清空 → id = −1 → 落地页回退到 `routes[0]` = **教学关**；②落地页 → 选关 → 教学关（`alwaysUnlocked`，永远可选） |
| 选关页也读不懂 | `StageSelectUI.AddLabel` 用 `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` 写中文关卡名 → 同样无中文字形 | 选关页标签换 `方正粗黑宋简体`（TMP 或 legacy 均可） |

> 教学关接线后，**每次要重跑教学关**都用清除存档（会一并清掉天赋与铜钱，符合"新档进教学"的语义）；
> 若只想重置教学进度而不动天赋，需要单独一个"重置教学"入口（暂不做）。

> 1012 仍是**未提交**资产（`git status` 显示 prefab / controller / 9 个 anim / meta 均为 untracked），本轮接线后一并纳入版本管理。

### 5.3 新增机制（批次 1）

| # | 落点 | 内容 |
|---|---|---|
| ④ | `Assets/Scripts/Enemy/Enemy.cs` | 新增 `public float TimeToStrike()`：`_approachDuration - tween.Duration()*tween.ElapsedPercentage()`（与 `IsInStrikeWindow()` 同源），供导演判断"窗口是否已开"；必要时给 `IsInStrikeWindow()` 加一个"窗口已开但还没出伤"的宽判定 |
| ⑤ | `Assets/Scripts/Player/InputManager.cs` | 新增 `[System.NonSerialized] public bool forcePointerHeld;`；`PointerHeld => isTouching \|\| forcePointerHeld`；`TutorialStageDirector` 独占使用，防御拍结束时清零 |
| ⑥ | `Assets/Scripts/Player/Combat/PlayerGuard.cs` | `TryAbsorbDamage` 成功时广播（如 `event Action<float> OnDamageAbsorbed`），导演据此判定"防御成功"（比轮询 `AccumulatedDamage` 更明确） |
| ⑦ | `Assets/Scripts/Enemy/EnemyManager.cs` | 新增 `DespawnEnemy(Enemy)`：移出 `allAliveEnemies` + `columnManager.RemoveEnemyFromColumn(col, enemy, skipChain: true)` + **不触发** `OnAnyEnemyDied`（不计击杀/经验/铜钱/掉落）+ `EnemyPool.ReturnEnemy`；并在存活列表清空时照常触发 `OnAllEnemiesDied`（波次完成判据） |
| ⑧ | `Assets/Scripts/Enemy/CavalryEnemy*.cs` | 新增"离场"流程：停止冲锋循环 → 走返航 → 返航途中淡出（`CavalryVisualController` 的马 + 骑手 renderer alpha）→ 调 ⑦ → 回池。淡出期间不可被输入打断、不计伤害 |

### 5.4 教学拍（批次 2）

**手势种类扩展**

- `GestureHintKind` 增加 `Parry`（上划）与 `ParryHold`（上划后按住：手指走完上划段后停在原地 + 光圈不收）。
- `TutorialGestureTarget` 增加 `ParryVertical`：
  - `InputManager.TutorialAllowsSwipeGesture`：只放行**不蓄力竖滑**（`chargeLevel < 1 && directionGesture == MoveGesture.SwipeVertical`），其余手势照旧 drop。
  - `TutorialStageDirector.ExpectedAttackFor`：`ParryVertical → AttackType.Parry`（收拍判据）。
- `TutorialGestureHint`：
  - `Parry` 示范 = 一次快速上划（拖尾 + 手指），循环播放。
  - `ParryHold` 示范 = 快速上划 → 手指停在终点 + 光圈常亮（表达"别松手"）。
  - `PollRawInput` 增加竖划判定：`|dy| ≥ |dx|`、`|delta| ≥ swipeMinPixels`、`dy > 0`；`ParryHold` 额外要求**抬手前按住达到 `parryHoldSeconds`**。
  - 冻结期输入捕获沿用现有 `PollRawInput`（unscaled 时间，冻结时照常工作）。

**拍型扩展（`TutorialStageDirector`）**

- `FreezeParryBeat`（招架拍）：
  1. 目标敌人起手 → 等 `IsInStrikeWindow()` 为真 → `Time.timeScale = 0`（每帧压住）+ 遮罩 + 示范循环。
  2. 玩家上划 → 解冻并同帧 `TryExecuteAttack(Parry, …, cancelCurrentMove: true)` → `CancelCurrentGesture()`。
  3. 派发后校验"敌人攻击已被取消"→ 拍成功（敌人回 Idle，HP 不变）→ 收拍（关示范、恢复场景亮度、放开敌人）。
  4. 拍失败（派发时窗口已过 / 没识别到手势）→ 继续冻结等待，**不设超时**（用户口径：没有失败态）；仅保留一条"长时间无任何输入"的防卡死保险（不写进设计，只写进实现注释）。
- `DefendHoldBeat`（防御拍）：
  1. 目标 109 置 `tutorialHold`（按住不动）→ 提示 `ParryHold`。
  2. 每帧检查 `PlayerWeaponStance.IsDefending` → 命中则开 `forcePointerHeld` 并放开 109 的 `tutorialHold`。
  3. 监听 `PlayerGuard.OnDamageAbsorbed` → 命中则关 `forcePointerHeld` → 收拍。
  4. 109 返航 → 淡出 → 离场 → 波次清空 → 路线通关。

### 5.5 资产与文案（批次 3）

- `Assets/RouteData/Tutorial/Tutorial_E0_Battle.asset`
  - 波0 `enemyIds[2]`：`1021` → `1012`；`healthMultiplier` 0.4（1012 100 血 → 40，两下点戳收掉）。
  - 波1（109）不动。
- 台词（`Assets/ScriptableObjects/Dialogue/`）
  - `tutorial_t0_wave3`：现文案"按住不放蓄力，再往上划——上挑破盾！" → 改写为**招架**教学（他这一下带霸体，正面打不断；看准挥下的那一瞬**上划招架**）。
  - `tutorial_t0_wave4`：现文案"前面那骑将……来者不善，稳住阵脚。" → 改写为**防御**教学（**上划之后别松手**，把枪横过来挡住冲锋）。
  - `tutorial_t0_death`（孤儿资产，"（喘息）……长坂坡的路，还长着。"）→ 改写为**胜利收尾**台词并接到教学关结束。
- `Battle.scene` → `Tutorial Director.gestureSteps`
  - ①N1波0 Tap、②N1波1 Swipe 不变；
  - ③E0波0：`ChargedSwipe` → `Parry`（新拍型）；④E0波1：`None` → `ParryHold`（新拍型）；⑤出口 Tap 不变。

---

## 6. 验收口径（Play Mode 可断言）

| # | 用例 | 期望 |
|---|---|---|
| 1 | 全新存档进教学关，竖滑 | 招架**出招**（天赋节点已删，不被锁） |
| 2 | 1012 起手 | 前摇有**红描边**（霸体信号）；`isCFrame=true` |
| 3 | 招架拍冻结瞬间 | `Time.timeScale == 0`；`enemy.IsInStrikeWindow() == true` 且保持不变 |
| 4 | 冻结中上划 | 1012 的 `isAttackAnimating` 变 false（攻击被取消）；**玩家 HP 不变**；解冻后挥枪动画播完 |
| 5 | 招架拍整体 | 1012 血量掉到 0 → 死亡 → 波次 0 完成 |
| 6 | 防御拍开场 | 109 静止不动（`tutorialHold=true`，`rowIndex` 不变） |
| 7 | 玩家上划并按住 | `PlayerWeaponStance.IsDefending == true`；`InputManager.forcePointerHeld == true` |
| 8 | 109 冲锋命中 | **玩家 HP 不变**；`PlayerGuard.AccumulatedDamage` +40；破防条可见（1/3） |
| 9 | 防御成功后 | `forcePointerHeld == false`；109 返航 → 淡出 → 离场 |
| 10 | 109 离场后 | `EnemyManager.GetAllAliveEnemies().Count == 0`；**击杀数不增加**；波次 1 完成 → 路线 `Completed` → 关卡 `Victory` → 结算铜钱 → 回落地页 |
| 11 | 全程 Console | 0 error（`TutorialGestureHint` / `CavalryEnemy` / `EnemyManager` 无异常） |

---

## 7. 风险与已知坑

1. **冻结点必须落在"窗口已开"**（§3.1 ③-2）→ 早冻结会死锁（`timeToStrike` 冻在窗口外，永远格不开）。
2. **解冻与派发必须同帧**，且剩余出伤时间 ≥ 一帧（≥0.15s，余量 ≈9 帧）→ 帧序不敏感；仍需在实现里避免"先解冻、后等一帧再派发"。
3. **冻结期间 InputManager 不工作** → 必须用原始输入轮询；且解冻后要 `CancelCurrentGesture()`，否则抬指可能被误判。
4. **强制按住是教学特权** → 必须保证拍结束/被打断时清零（否则玩家永久无法攻击）。
5. **109 的 `tutorialHold` 只能在冲锋开始前上**（冲锋中上 = 冲锋作废）。
6. **1012 命中帧不修 → 一切错位**（出伤被推到动画末尾，招架窗口对不上）。
7. `tutorialHold` 期间敌人"受击/动画照常"，但**不调度新行动** → 招架拍的 1012 在冻结前必须已经起手（否则玩家格不到东西）。
8. 对象池：`DespawnEnemy` 走 `ReturnEnemy` 前必须停掉 109 的协程与动画状态，否则复用时残留（参考 `memory/enemy-wave-tint-pooling.md` 的池复位教训）。
9. 1012 资产目前**未提交**（untracked）→ 本轮要一并 `git add`。
10. 招架动作本身昨天刚重做（`parry-guard-rework-handoff-20261026.md` §6 待办 1：用户还没看过"沿枪轴推出"版）→ 教学关上线前用户最好先看一眼这一版招架动作。

---

## 8. 与既有设计文档的关系（需同步的表述）

| 文档 | 需改 |
|---|---|
| `design/attack-interrupt-system.md` §4.7.4 | "招架是局外天赋解锁项（`talent_unlock_parry`）" → 改为"基础招式" |
| `design/out-of-game-growth-system.md` | 9 节点 → 8 节点；"招架 = 独立旁支 100 铜钱"的表述作废 |
| `design/gameplay-pillars.md` / `design/game-mechanics.md` | 教学关内容表：点戳 / 横划 / 招架 / 防御（去掉上挑破盾） |
| `plan/parry-guard-rework-handoff-20261026.md` §7 | 标注"招架已不再是局外解锁项（2026-10-26 教学关改造）" |
| `plan/tutorial-and-formal-stage-handoff-20261019.md` | 教学关 5 步手势表已过时（现在 6 拍） |
