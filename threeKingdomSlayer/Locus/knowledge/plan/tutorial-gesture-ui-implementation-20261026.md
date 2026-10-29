---
id: kd_528ddd80-36db-486e-8bce-28544b46572a
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 实现方案：教学手势系统 + 招架/防御新拍型（2026-10-26）

> 配套设计：`plan/tutorial-parry-defend-redesign-20261026.md`（设计定稿：招架拍 / 防御拍 / 109 离场）。
> 本文件是**实现级方案**：先把现有教学系统解剖清楚（照抄现有约定，不推翻），再给逐文件的改动。
> 关联：`design/pixel-character-and-enemy-animation-spec.md`、`design/pause-freeze-rule.md`、
> `plan/parry-guard-rework-handoff-20261026.md`、`plan/enemy1012-asset-pull-20261026.md` §8。

---

## 1. 范围

| 做 | 不做（另立项） |
|---|---|
| 教学关：招架拍、防御拍、109 离场收尾 | 正式关首入拍（C2 复合手势提示 + 破霸体教学） |
| 手势提示新增 2 种手势（Parry / ParryHold）与其示范动画 | 手势提示的整体重构（现有分层/贴图/尺寸沿用） |
| 冻结拍（timeScale=0）+ 原始输入捕获 | 旧 `Gate()` 门槛的删除（先留着，新拍不依赖它） |
| 1012 霸体化 / 命中帧 / 脚底 | 1012 的美术重画（站姿与 1011 的观感差异是美术决策） |

---

## 2. 现有教学系统解剖（实现时照此约定）

### 2.1 手势 UI 的部署（场景层级）

`Assets/Prefabs/UI/TutorialGestureHint.prefab` → 场景里作为**独立根对象** `Tutorial Gesture Hint` 实例存在：

```
Tutorial Gesture Hint                      ← 独立 Canvas（sortingOrder 60 / ScreenSpace-Overlay）
└─ TutorialGestureHint (component)          ← 挂在根对象的子节点上
   └─ ScreenSpace (RectTransform, 0,0→1,1) ← 全屏容器，所有图层坐标换算的基准
      ├─ DimOverlay (Image, 全屏黑 α0.55, raycastTarget=false)  ← 只给旧 Gate 用
      ├─ Trail     (Image, 220×220, pivot 0.5)  tutorial_gesture_swipe_trail_ui.png
      ├─ Ring      (Image, 240×240)             tutorial_gesture_hold_ring_ui.png
      └─ Finger    (Image, 220×220, RT pivot (0.26,0.81))  tutorial_gesture_finger_ui.png
```

要点：

- **独立 Canvas + order 60** → 永远盖在战斗 HUD / 对话气泡之上；CanvasScaler = Scale With Screen Size，
  参考分辨率 1080×1920，match 0.5 → 竖屏各分辨率下坐标一致（屏幕坐标 → `RectTransformUtility.ScreenPointToLocalPointInRectangle`）。
- **手指的锚点是指尖**：`Finger` 的 **RectTransform pivot (0.26, 0.81)** 刻意对齐贴图自身的 pivot（67.5, 206.5）/256
  → `Place(finger, screenPos, size)` 传的是**指尖的屏幕位置**。
  （教训记录在 `memory/tutorial-gesture-gate-pitfalls.md`：改 sprite pivot 不影响定位，**RectTransform 的 pivot 才是锚点**。）
- **四个图层全部 `raycastTarget = false`** → 提示 UI 永远不挡玩家手指。
- 示范动画**全部由代码协程驱动**（无动画片段）：
  `PlayTap`（按下 + 涟漪）/ `PlaySwipe`（手指沿线段走、拖尾淡出）/ `PlayHold`（光圈原地收缩+旋转）/ `PlayChargedSwipe`（先蓄力后划）。
- 贴图 3 张 256²：`Assets/Sprites/UI/Tutorial/tutorial_gesture_{finger,swipe_trail,hold_ring}_ui.png`。
  → **新增手势不需要新贴图**（复用 finger / trail / ring 的组合表达）。

### 2.2 两套教学机制（并存，只有第二套在用）

| | 旧：`TutorialGestureHint.Gate()` | 现：`TutorialStageDirector` 的拍 |
|---|---|---|
| 冻结 | **全局 timeScale=0** + `GameplayInputGate.RequestBlock(TutorialGate)` | **只冻敌人**（`Enemy.tutorialHold`），游戏时钟照走 |
| 输入 | 自己轮询原始输入（`PollRawInput`，unscaled 时间） | 走**正常 InputManager 通道**，用 `tutorialGestureTarget` 过滤 |
| 判定 | 自己做（位移/时长/方向阈值） | 真的出招后由 `OnAttackExecuted(AttackType)` 判定 |
| 收场 | 解冻 + `ReplayGesture` 重放一次 | 结束拍、敌人恢复、场景恢复亮度 |
| 现状 | **导演已不再调用**（死代码，但可复用） | 在用（5 个步骤） |

> **关键约束（本次改造的核心矛盾）**：
> `InputManager.Update()` 开头 `if (!gameplayInputEnabled || Time.timeScale == 0f) return;`
> → **timeScale=0 时整条游戏输入通道不响应**。
> 所以"完全冻结 + 玩家真手势"**不能靠 InputManager**，只能靠 `PollRawInput` 那条原始输入通道。

### 2.3 现有一拍的完整时序（`TutorialStageDirector.Hints.cs`）

```
OnWaveStarted(waveIndex)
 └─ 记下这一波的台词（_pendingWaveDialogue）→ StartHint(waveIndex)
     └─ HintRoutine(step)
         ├─ delay（unscaled 等待）
         ├─ [lineAtWaveStart] PlayWaveIntroLine()：冻敌 + 播登场台词（timeScale=0 顶住）→ 台词结束放行
         ├─ kind == None → 直接结束（只播台词不教手势）
         ├─ 等条件：目标敌人站定（rowIndex==0 且不在 Moving）+ 血量 ≤ triggerHpPercent
         │           （登场台词拍用 hpThreshold=1，不等血量）超时按当前状态开拍
         ├─ 推台词（forceReplay）→ 等对话真的显示
         ├─ 等目标敌人当前那一刀收完（最多 2s，避免冻在半空）
         ├─ 示范位置：有敌人就贴到敌人身上（from=敌人屏幕位，to=敌人屏幕位+偏移）
         ├─ BeginBeat(target)：tutorialHold=true + 场景压暗 + 输入闸门=target + 订阅 OnAttackExecuted
         └─ 示范循环：每 playSeconds 播一次手势示范（ChargedSwipe 强制 ≥2.5s）
             └─ OnAttackExecuted(期望的 AttackType) → EndBeat()：解冻敌人 + 撤压暗 + 收掉台词
```

### 2.4 输入闸门（`InputManager`）

- `tutorialGestureTarget`（枚举 None / Tap / SwipeHorizontal / ChargedVertical）：
  - `TutorialAllowsTapGesture`：只教点按时，滑动/蓄力都不成立。
  - `TutorialAllowsSwipeGesture`：横滑教学只收**未蓄力**横滑；上挑教学只收**已蓄力**竖滑；其余 drop。
  - drop 的手势**不成立也不消费蓄力段**（玩家的蓄力保留给正确动作）。
- 手势→招式映射（`MoveGestureDefaults.ResolveAttackType`）：**未蓄力竖滑 = Parry**；蓄力竖滑 = Launch；横滑 = Slash；点按 = Stab。
- `TutorialStageDirector.ExpectedAttackFor(target)`：把目标手势映射回期望的 `AttackType`，用于收拍判定。

### 2.5 场景压暗（`TutorialSceneDim`）

- **不是 UI 遮罩**：战场是透视相机 + 世界物件（长枪是 MeshRenderer），任何 Overlay 画布都会盖在角色上。
  改成给**路线场景的渲染器**乘亮度（MaterialPropertyBlock 的 `_Color` / `_BaseColor`；地面自定义 shader 用 `_TutorialDim`），
  程序化天空盒则换一份材质实例调 `_SkyTint` / `_GroundColor`。
- **角色、长枪、敌人不压暗** → 玩家能看清"自己的武器 ↔ 面前的敌人"。
- 淡入淡出用 **unscaled** 时间（0.12s）→ **冻结时照样能淡**。
- 压暗值 0.42（场景 `Tutorial Director.sceneDimBrightness`）。

### 2.6 对话系统的配合

- `DialogueManager.QueueEventAndNotify(evt, forceReplay: true)`；`nonBlocking=true` 的台词不暂停、不压暗、不挡点击（教学关的每波台词都是 nonBlocking）。
- 阻塞式台词会 `Time.timeScale = 0`，且 `Finish()` **无条件把 timeScale 设回 1**
  → 冻结拍必须**每帧重新压住 timeScale=0**（旧 `Gate()` 就是这么做的）。

### 2.7 现状缺口清单（本次要处理/绕开的）

| # | 缺口 | 处置 |
|---|---|---|
| 1 | `GestureHintKind.Hold` 在 `ToGestureTarget` 里映射成 `None` → 用了就会"手势拍无法开始" | 本次新增的 `ParryHold` 走**新映射**，不碰 `Hold`；或顺手把 `Hold` 也映射上（本次不做） |
| 2 | `TutorialGestureStep.pauseUntilPerformed` 字段**没人读**（现拍不暂停） | 保留不动；冻结拍用新字段 `freezeOnGesture` |
| 3 | 示范位置**永远贴到目标敌人身上**（`from=敌人屏幕位`） | 招架/防御是**玩家侧**动作 → 需要新的定位模式（贴玩家） |
| 4 | 冻结与 InputManager 互斥 | 冻结拍走 `PollRawInput` 原始输入通道 + 手动派发 |
| 5 | `GestureHintKind.None` + `lineAtWaveStart` 那一拍（现在的 109 登场拍）不教手势 | 防御拍要替换它 |
| 6 | 手势提示的 `playSeconds` 默认 1.1s，**与游戏真实划动速度不符**（游戏：≥180px/s、≤0.25s） | 新手势用**快速示范**（0.22s） |

---

## 3. 新手势的设计

### 3.1 `GestureHintKind.Parry`（上划招架）

| 项 | 值 |
|---|---|
| 语义 | 从下往上快速划一下（**不蓄力**）→ 招架 |
| 示范 | 复用 `PlaySwipe` 机制，但**时长 0.22s**（游戏里的划动门限：`minSwipeSpeed=180px/s`、`maxSwipeDuration=0.25s`；示范画得慢会让玩家学错速度） |
| 位置 | **贴玩家**（招架是玩家自己的动作），而不是贴敌人 |
| 判定 | 竖直上划：`|dy| ≥ |dx|`、`dy > 0`、`|delta| ≥ swipeMinPixels(90)`；**不要求蓄力** |
| 输入通道 | 冻结拍 → 原始输入；非冻结拍 → 正常 InputManager（闸门只放行竖滑） |

### 3.2 `GestureHintKind.ParryHold`（上划后按住 = 防御）

| 项 | 值 |
|---|---|
| 语义 | 上划一下**并且别松手** → 招架挥入 → 落防御位 |
| 示范 | 快速上划（0.22s）→ 手指**停在终点** + 光圈出现并**轻微脉动**（不复用"收缩=蓄力"的语义，蓄力环会误导成"蓄力"） |
| 判定 | 竖直上划 + **抬手前已按住 ≥ 0.35s**（与游戏一致：招架挥入 0.18+0.08+0.10 = 0.36s 内按住即可落防御位） |
| 成功判据 | **不看手势，看状态**：`PlayerState.Instance.WeaponStance.IsDefending == true`（两种按法都算过） |
| 输入通道 | 正常 InputManager（不冻结：109 必须能真冲锋） |

### 3.3 冻结期的输入捕获

- 复用 `TutorialGestureHint.PollRawInput()`（读 `Input.touchCount/Input.mousePosition` + `Time.unscaledTime` → **冻结时照常工作**）。
- 现状它只在 `Gate()` 内部被调用 → 抽成可外部驱动的两段式：
  - `public void StartRawWatch(GestureHintKind kind, Vector2 from, Vector2 to)`：只开轮询 + 播示范，**不碰 timeScale**（冻结由导演管）。
  - `public event System.Action<GestureHintKind> OnRawGestureDetected`：识别到手势时回调（导演据此解冻 + 派发）。
  - `StopRawWatch()`：收场。
- 冻结由**导演**持有（`Time.timeScale = 0` + 每帧压住），**提示组件不碰 timeScale** → 职责清晰，也避免与 `DialogueManager.Finish()` 打架。

---

## 4. 两种新拍型

### 4.1 招架拍（`FreezeParryBeat`）

```
① 等目标敌人（1012）走到前排并起手（霸体前摇：红描边）
② 每帧查 enemy.IsInStrikeWindow()（出伤前 0.15s 内为真）
   —— 窗口一开就冻结（冻结后 tween 停 → 窗口永远开着）
③ Time.timeScale = 0（每帧压住）；场景压暗（unscaled 淡入，冻结中也生效）
④ gestureHint.StartRawWatch(Parry, from=玩家屏幕位, to=玩家屏幕位+上移)
   → 循环播「快速上划」示范
⑤ 玩家上划 → OnRawGestureDetected
   → 同一帧：Time.timeScale = 1（解冻）
            AttackSystem.TryExecuteAttack(Parry, -1, …, cancelCurrentMove: true)
            InputManager.CancelCurrentGesture()（丢掉冻结期间错过的按下）
   → 冻结时敌人 tween 停着 → IsInStrikeWindow() 仍为真 → 敌人攻击被 CancelAttack() → **这一击不结算**
⑥ 收拍：撤压暗、示范收起、敌人恢复（tutorialHold=false）
⑦ 玩家用点戳收掉 1012（教学波次 healthMultiplier 0.4 → 40 血）
```

**为什么"同帧解冻+派发"是安全的**：解冻后 tween 每帧只推进 ~16ms，而剩余出伤时间 ≥0.15s（≈9 帧）
→ 无论 DOTween 的更新在 MonoBehaviour 更新之前还是之后，**招架都先于出伤生效**。

**为什么必须"窗口开了才冻结"**：窗口前冻结 → `timeToStrike` 被冻在窗口外 → 招架永远判不中 → 死锁。

### 4.2 防御拍（`DefendHoldBeat`）

```
① 109 入场（第 1 排）→ 导演置 tutorialHold=true（StepPaused → 冲锋计时不走，109 原地不动）
② 提示「上划并按住」（ParryHold 示范循环，位置贴玩家）
③ 玩家上划并按住 → 正常 InputManager 通道（闸门只放行未蓄力竖滑）
   → 招架执行（对 109 是空放：动作锁 0.9s，不影响防御）→ 挥入 0.36s → 落防御位
④ 导演每帧查 PlayerState.Instance.WeaponStance.IsDefending
   → true：开 InputManager.forcePointerHeld = true（教学锁定，玩家松手也不掉防御）
            同时放开 109 的 tutorialHold → 109 开始冲锋
⑤ 109 冲锋（windup 0.5 + 前移 0.35 + strikeWindup 0.5 ≈ 1.4s）→ 命中
   → PlayerState.TakeDamage → Guard.TryAbsorbDamage(40) 吃掉 → **玩家 HP 不变**，破防条 +40/120
⑥ PlayerGuard 广播"吸收了伤害" → 导演关掉 forcePointerHeld → 收拍（玩家可正常松手收枪）
⑦ 109 走它自己的返航流程（转身 0.4 + 每排 0.6）→ 返航途中淡出 → DespawnEnemy → 回池
⑧ 存活列表清空 → 波次完成 → E0 是终点节点 → FinishRoute → 胜利结算 → 回落地页
```

**顺序铁律**：109 的 `tutorialHold` **必须在冲锋开始前上**。
冲锋开始后再上 → `StepPaused()` 会把 `_frozenDuringCharge` 置真 → 冲锋作废直接返航（109 不会来打玩家）。

---

## 5. 代码改动清单（按文件）

### 5.1 `Assets/Scripts/Enemy/Enemy.cs`

```csharp
/// <summary>本次攻击距离出伤还剩多久（秒）；不在攻击中返回 -1。用于教学时停判定。</summary>
public float TimeToStrike()
{
    if (state != EnemyState.Attacking || !isAttackAnimating) return -1f;
    if (_attackTween == null || !_attackTween.IsActive()) return -1f;
    if (_approachDuration <= 0f) return -1f;
    float elapsed = _attackTween.Duration() * _attackTween.ElapsedPercentage();
    return _approachDuration - elapsed;
}
```
> 与 `IsInStrikeWindow()` 同源（都用 tween 已播比例 → 卡肉/冻结时不错位）。
> 用途：导演可以在窗口**开之前**就准备（显示"准备"提示），但**冻结仍要等到窗口为真**。

### 5.2 `Assets/Scripts/Enemy/EnemyManager.cs`

```csharp
/// <summary>
/// 非死亡移除：把敌人从战场摘掉（移出存活列表与列），**不触发** OnAnyEnemyDied
/// → 不计击杀 / 经验 / 铜钱 / 掉落。用于"敌人撤退离场"类演出（如教学关 109）。
/// </summary>
public void DespawnEnemy(Enemy enemy)
{
    if (enemy == null) return;
    if (!allAliveEnemies.Remove(enemy)) return;
    columnManager?.RemoveEnemyFromColumn(enemy.columnIndex, enemy, skipChain: true);
    EnemyPool.Instance?.ReturnEnemy(enemy);
    if (allAliveEnemies.Count == 0) OnAllEnemiesDied?.Invoke();
}
```

### 5.3 `Assets/Scripts/Enemy/CavalryEnemy*.cs`

```csharp
/// <summary>教学用：停止冲锋循环 → 返航 → 淡出 → 离场（不计击杀）。</summary>
public void BeginTutorialExit() { … }
```
- 停止 `_cycleRoutine`、把 Phase 置 `Retreating`、沿返航路线逐排后退；
- 返航途中把**马 + 骑手**两个 SpriteRenderer 的 alpha 渐隐到 0（0.6s，unscaled）；
- 渐隐结束 → `EnemyManager.Instance.DespawnEnemy(_enemy)`；
- 离场期间：不可被输入打断、不结算伤害、不进击杀统计。

### 5.4 `Assets/Scripts/Player/InputManager.cs`

```csharp
/// <summary>教学临时锁定：强制"手指仍按住"（用于防御拍，防止玩家松手掉防御）。拍结束必须清零。</summary>
[System.NonSerialized] public bool forcePointerHeld;

public bool PointerHeld => isTouching || forcePointerHeld;

/// <summary>教学手势闸门新增目标：只放行"不蓄力的竖滑"（= 招架）。</summary>
public enum TutorialGestureTarget { None, Tap, SwipeHorizontal, ChargedVertical, ParryVertical }
```
- `InputManager.Mapping.cs` 的 `TutorialAllowsSwipeGesture` 增加：
  `case TutorialGestureTarget.ParryVertical: return chargeLevel < 1 && directionGesture == MoveGesture.SwipeVertical;`
- 注意：`MoveGestureDefaults.ResolveAttackType(SwipeVertical, charged:false) == AttackType.Parry` → 玩家的竖滑天然就是招架。

### 5.5 `Assets/Scripts/Player/Combat/PlayerGuard.cs`

```csharp
/// <summary>防御吸收了伤害时广播（教学拍据此判定"防御成功"）。</summary>
public event System.Action<float> OnDamageAbsorbed;
```
- 在 `TryAbsorbDamage` 成功吸收后 Invoke。

### 5.6 `Assets/Scripts/UI/TutorialGestureHint.cs`

新增（不破坏现有 Gate 逻辑）：

```csharp
public enum GestureHintKind { Tap, Hold, Swipe, ChargedSwipe, None, Parry, ParryHold }   // ← 加两个
```
> `GestureHintKind` 声明在 `TutorialStageDirector.cs`（不在 UI 脚本里）→ 改那一个文件。

```csharp
// ── 冻结拍用的原始输入监听（不碰 timeScale，冻结由导演持有）──
public event System.Action<GestureHintKind> OnRawGestureDetected;
public void StartRawWatch(GestureHintKind kind, Vector2 from, Vector2 to);
public void StopRawWatch();
```
- `StartRawWatch`：记录 kind/from/to、开始循环播示范、打开 `_rawWatch = true`。
- `Update()`：`_rawWatch` 时每帧调 `PollRawInput()`；`PollRawInput` 识别到符合 kind 的手势 → `OnRawGestureDetected?.Invoke(kind)`（**不自动重放**）。
- `PollRawInput` 新增两个分支：
  - `Parry`：竖直上划（`|dy| ≥ |dx|`、`dy > 0`、`|delta| ≥ swipeMinPixels`）→ 命中。
  - `ParryHold`：同上 **且** 抬手前已按住 ≥ `parryHoldSeconds`（新增字段，默认 0.35s）。
- 示范动画：
  - `PlayParry(from, to)`：复用 `SwipeRoutine` 的走线逻辑，但用**新的时长** `parrySwipeSeconds = 0.22f`（快速划动才符合游戏门限）。
  - `PlayParryHold(from, to)`：快速上划段 + 手指停在终点 + 光圈出现并**脉动**（不收缩，避免读成"蓄力"）。
    → 需要 Ring 的"脉动"表现：缩放 1.0↔1.12 缓慢呼吸（新增协程，不改 HoldRoutine 的蓄力语义）。

### 5.7 `Assets/Scripts/Tutorial/TutorialStageDirector*.cs`

- `GestureHintKind` 增加 `Parry` / `ParryHold`。
- `ToGestureTarget(kind)`：`Parry → TutorialGestureTarget.ParryVertical`；`ParryHold → ParryVertical`。
- `ExpectedAttackFor(target)`：`ParryVertical → AttackType.Parry`。
- `TutorialGestureStep` 增加：
  - `bool freezeOnGesture`：这一拍走**冻结拍**（招架拍）。
  - `bool anchorToPlayer`：示范贴玩家而不是贴敌人（招架/防御都是玩家侧动作）。
  - `bool holdAfterGesture`：这一拍的成功判据是 `IsDefending`（防御拍）而不是 `OnAttackExecuted`。
  - `bool despawnTargetOnSuccess`：成功后让目标敌人离场（109）。
- 导演新增两个拍型协程：`FreezeParryRoutine(step)` / `DefendHoldRoutine(step)`；
  `StartHint` 按 `step.freezeOnGesture / step.holdAfterGesture` 分流到对应的协程（其余走现有 `HintRoutine`）。
- 导演负责冻结：
  - 冻结前 `gestureHint.StartRawWatch(...)`；每帧 `if (Time.timeScale != 0f) Time.timeScale = 0f;`（压住 `DialogueManager.Finish()`）。
  - 收到 `OnRawGestureDetected` → **同一帧**：`Time.timeScale = 1` → `AttackSystem.Instance.TryExecuteAttack(AttackType.Parry, -1, …, cancelCurrentMove: true)` → `InputManager.Instance.CancelCurrentGesture()` → `gestureHint.StopRawWatch()`。
- 防御拍：
  - 开拍即 `targetEnemy.tutorialHold = true`；
  - 每帧 `if (PlayerState.Instance?.WeaponStance?.IsDefending == true && !_forcedHold) { InputManager.Instance.forcePointerHeld = true; targetEnemy.tutorialHold = false; }`
  - 订阅 `PlayerState.Instance.Guard.OnDamageAbsorbed` → 收拍：`forcePointerHeld = false` + `targetEnemy.BeginTutorialExit()`。
- `OnDisable` / `StopHint` 必须复位：`forcePointerHeld = false`、`Time.timeScale = 1`、`gestureHint.StopRawWatch()`、`tutorialHold = false`。

---

## 6. 资产与配置改动清单

| # | 对象 | 改动 |
|---|---|---|
| 1 | `Assets/ScriptableObjects/Talents/TalentTree_Main.asset` | 删 `talent_unlock_parry` 节点（9→8）→ 招架变基础招式 |
| 2 | `Assets/Resources/EnemyPrefabs/Enemy_1012.prefab` | 攻击步 `isCAttack: true` + `animationTrigger: "Attack"`；命中帧 `Enemy_1012_attack6`（keyIndex/hitTime 用 `校验攻击命中帧` 工具回填）；`visualYOffset` 0.1389 → **0.001** |
| 3 | `Assets/RouteData/Tutorial/Tutorial_E0_Battle.asset` | 波0 `enemyIds[2]` 1021 → 1012；`healthMultiplier` 0.4 |
| 4 | `Assets/ScriptableObjects/Dialogue/tutorial_t0_wave3.asset` | 改写为**招架**教学 |
| 5 | `Assets/ScriptableObjects/Dialogue/tutorial_t0_wave4.asset` | 改写为**防御**教学 |
| 6 | `Assets/ScriptableObjects/Dialogue/tutorial_t0_death.asset` | 孤儿资产 → 改写为**胜利收尾**台词并接线 |
| 7 | `Assets/Scenes/Battle.scene` | `Tutorial Director.gestureSteps` 重配（见 §7）；`Tutorial Gesture Hint` 的 TMP 字体（若该对象有中文） |
| 8 | 落地页 `HubDebugSaveButton` 的 TMP 字体 | `LiberationSans SDF` → `Assets/Fonts/方正粗黑宋简体 SDF.asset`（否则"清除存档"是乱码） |
| 9 | `Assets/Scripts/UI/StageSelectUI.cs` | 选关页标签的 legacy 字体 → 中文字体（否则关卡名是乱码） |
| 10 | 新增/改动脚本 | `Enemy.cs` / `EnemyManager.cs` / `CavalryEnemy*.cs` / `InputManager.cs` / `InputManager.Mapping.cs` / `PlayerGuard.cs` / `TutorialGestureHint.cs` / `TutorialStageDirector*.cs` |

---

## 7. 场景 `gestureSteps` 重配（配置权威在场景）

| # | battle | wave | kind | 新增开关 | 效果 |
|---|---|---|---|---|---|
| 0 | Tutorial_N1_Battle | 0 | Tap | — | 教点戳（不变） |
| 1 | Tutorial_N1_Battle | 1 | Swipe | — | 教横划（不变） |
| 2 | Tutorial_E0_Battle | 0 | **Parry** | `freezeOnGesture=true`、`anchorToPlayer=true` | 教招架（时停） |
| 3 | Tutorial_E0_Battle | 1 | **ParryHold** | `holdAfterGesture=true`、`anchorToPlayer=true`、`despawnTargetOnSuccess=true` | 教防御 + 109 离场 |
| 4 | — | -2 | Tap | — | 出口点箭头（不变） |

- step 2 的台词：`tutorial_t0_wave3`（"他这一下带霸体…看准挥下的那一瞬，上划招架！"）
- step 3 的台词：`tutorial_t0_wave4`（"上划之后别松手，把枪横过来挡住冲锋！"）
- step 2/3 的 `triggerHpPercent`：招架拍用 1（不等血量，敌人自己会起手）；防御拍用 1。

---

## 8. 验收

### 8.1 功能断言（Play Mode，可脚本化）

| # | 用例 | 期望 |
|---|---|---|
| 1 | 新档进教学关竖滑 | 出招架（天赋门控已删） |
| 2 | 1012 起手 | `isCFrame=true` + 红描边 |
| 3 | 招架拍冻结瞬间 | `Time.timeScale==0`；`enemy.IsInStrikeWindow()==true` 且保持不变 |
| 4 | 冻结中上划 | 同帧解冻；1012 `isAttackAnimating` 变 false；**玩家 HP 不变**；挥枪动画随后播完 |
| 5 | 招架拍收尾 | 1012 血量归零 → 死亡 → 波次 0 完成 |
| 6 | 防御拍开场 | 109 静止（`tutorialHold=true`，`rowIndex` 不变） |
| 7 | 玩家上划并按住 | `IsDefending==true`；`forcePointerHeld==true`；109 的 `tutorialHold=false` |
| 8 | 109 冲锋命中 | **玩家 HP 不变**；破防条 +40/120 |
| 9 | 防御成功后 | `forcePointerHeld==false`；109 返航 → 淡出 → `allAliveEnemies.Count==0`；**击杀数不增加** |
| 10 | 收尾 | 波次 1 完成 → 路线 `Completed` → 关卡 `Victory` → 结算 → 回落地页 |
| 11 | 全程 | Console 0 error |

### 8.2 视觉断言

- 招架拍：场景压暗到 0.42、角色/敌人/长枪不暗；手指在**玩家**身上快速上划。
- 防御拍：手指上划后**停住 + 光圈脉动**（不收缩）；109 返航时马与骑手一起淡出。
- 冻结期：示范动画用 unscaled 时间 → **冻结时照样在动**（`SwipeRoutine` 用 `Time.unscaledDeltaTime` ✓）。

### 8.3 回归

- 原 5 步（点按 / 横划 / 蓄力上挑 / 登场台词 / 出口）行为不变 → 改完后重跑 N1 波0/波1 与出口拍。
- 招架动作本身（昨天重做的"沿枪轴推出"版）用户还没看过 → 教学关上线前先看一眼。

---

## 9. 风险与坑

1. **冻结点必须落在"窗口已开"**（早冻结 → 死锁）。
2. **解冻与派发必须同帧**，且剩余出伤 ≥1 帧。
3. **冻结期 InputManager 不工作** → 必须用原始输入轮询；解冻后 `CancelCurrentGesture()`。
4. **强制按住是教学特权** → 拍结束/被打断/场景卸载必须清零，否则玩家永久无法攻击。
5. **109 的 tutorialHold 只能在冲锋前上**（冲锋中上 = 冲锋作废）。
6. **`DialogueManager.Finish()` 会把 timeScale 顶回 1** → 冻结拍每帧压住。
7. **示范速度必须快**（0.22s）：游戏划动门限 180px/s / ≤0.25s，示范画得慢 → 玩家学出来的手势在游戏里不成立。
8. **示范位置**必须贴玩家（招架/防御是玩家侧动作），否则会误导成"在敌人身上划"。
9. **1012 命中帧不修 → 出伤错位** → 招架窗口对不上（详见 `plan/enemy1012-asset-pull-20261026.md` §8.4）。
10. **1012 脚底浮空 0.14u** → `visualYOffset` 改 0.001。
11. 对象池：`DespawnEnemy` 前必须停掉 109 的协程/动画状态（`memory/enemy-wave-tint-pooling.md` 的池复位教训）。
12. 教学关每次重跑要**清存档**（落地页左下角按钮，字体先修好否则认不出）。

---

## 10. 实施顺序

| 批次 | 内容 | 产出 |
|---|---|---|
| **0** | 天赋节点 / 1012 prefab（霸体 + 命中帧 + visualYOffset）/ 两处中文字体 | 资产层，Play Mode 可直接看到 1012 与可读的按钮 |
| **1** | 机制底座：`Enemy.TimeToStrike` / `EnemyManager.DespawnEnemy` / `CavalryEnemy.BeginTutorialExit` / `InputManager.forcePointerHeld` / `PlayerGuard.OnDamageAbsorbed` | 纯新增 API，无行为变更 |
| **2** | 手势提示扩展：`GestureHintKind.Parry/ParryHold` + 示范动画 + 原始输入监听 + 输入闸门 `ParryVertical` | 可单独验证示范动画 |
| **3** | 导演新拍型：冻结招架拍 / 防御拍 + 场景配置 + 台词 | 教学关可跑通 |
| **4** | 正式关首入拍（C2 复合手势 + 破霸体） | 另立项 |
