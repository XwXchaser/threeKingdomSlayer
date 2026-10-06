---
id: kd_2810fd31-5518-4f7f-bd60-00a3030454f9
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 敌人攻击「命中帧 → 出伤」同步修复方案

> **触发问题**：1011 在攻击动画播完后才出伤。实测出伤在攻击开始后 **3.698s**，而 `Enemy_1011_Attack` 只有 2.933s，命中键 `attack8` 在 2.000s。
> **目标**：出伤（近战）/ 击发（远程）时刻与动画 clip 的指定「命中帧」严格同步；以后新敌人只需声明命中帧，不再靠手调 `spawnDuration` 去凑动画。
> **状态**：**P0 已实施并实测通过**（1011 出伤已与 `attack8` 帧同步）；P1 待各敌人命中帧数据。实施记录见 §9。

## 1. 已核实的事实

### 1.1 当前出伤机制
- 出伤由 DOTween 前摇回调触发，与动画无绑定：`Enemy.PlayAttackAnimationTween`（`Assets/Scripts/Enemy/Enemy.cs:1874-2011`），近战回调在 `Enemy.cs:1945-1951` 调 `PerformAttack()`，后者只做 `EnemyManager.OnEnemyAttackPlayer(this)`（`Enemy.cs:1803-1818` → `EnemyManager.cs:236-242` → `PlayerState.TakeDamage`，无延迟）。
- 远程（`isRanged`）在 `Enemy.cs:1923-1936` 用 `AppendInterval(spawnDuration)` 发射飞行物。
- 所有攻击 clip 均为**纯精灵曲线**（无 Transform 曲线）→ 位移归 DOTween、精灵归 Animator，二者可各自独立驱动，互不覆盖。
- 攻击 clip 无任何 AnimationEvent（全项目 0 个 `.anim` 含 `m_FunctionName`）。

### 1.2 根因（本次 1011 的偏移）
`Enemy.cs:1915-1920` 在**任何 `Append` 之前**执行：

```csharp
float telegraphStart = Mathf.Max(0f, spawnDuration - telegraphDuration); // 2.0 - 0.3 = 1.7
_attackTween.InsertCallback(telegraphStart, () => _attackTelegraph?.BeginWarning(telegraphDuration));
```

`InsertCallback(1.7)` 先把新建 Sequence 的时长撑到 1.7s，之后追加的元素整体后移 1.7s。
运行中实测（反射读取）：`duration = 4.7`、`lastTweenInsertTime = 3.7`、子 tween 时长 `2` 与 `1`。
→ 出伤 3.7s、收招 3.7–4.7s；动画 2.93s 就播完，所以「整段动画播完才出伤」。

运行中逐帧实测（一次完整攻击，t=0 为 tween/动画起点）：

| t | 事件 | 精灵 | Animator |
|---|---|---|---|
| 0.000 | tween 与动画同时开始 | idle1 | `Enemy_1011_Attack` |
| 2.002 | 命中键 attack8 出现 | attack8 | Attack（norm 0.690） |
| 2.902 | 攻击动画播完切回待机 | idle1 | Idle |
| **3.698** | **玩家扣血** | **idle6** | **Idle（norm 0.986）** |
| 4.698 | 收招结束 | idle1 | Idle |

预警（telegraph）在 +1.699s 出现、+1.999s 消失（0.3s 窗口），比实际出伤早 1.7s → 当前是假提示。

### 1.3 手填时长与 clip 本来就普遍不一致
| prefab | 步骤 | clip | clip 时长 | spawn / draw | 现状出伤 | 现状 tween 总长 |
|---|---|---|---|---|---|---|
| Enemy_1 | 普通 | Enemy_1_Attack（4 键） | 0.683 | 0.50 / 0.30 | 0.7 | 1.0 |
| Enemy_101 | 普通 | Enemy_101_Attack（4 键） | 2.533 | 2.00 / 1.00 | 3.7 | 4.7 |
| **Enemy_1011** | 普通 | **Enemy_1011_Attack（18 键）** | **2.933** | 2.00 / 1.00 | **3.7** | **4.7** |
| Enemy_102 | 普通 | Enemy_102_Attack（2 键） | 0.533 | 1.00 / 1.00 | 1.7 | 2.7 |
| Enemy_103 | C 技 | Enemy_103_CAttack1/2/3（各 2 键，三连） | 1.033×3 | 3.00 / 1.50 | 5.7 | 7.2 |
| Enemy_104 / 107 / 108 | 普通 / C 技 | Boss_104_Attack（5 键）/ Boss_104_CAttack（5 键） | 1.033 / 1.333 | 0.60 / 0.40、0.80 / 0.50 | 0.9 / 1.7 | 1.3 / 1.8 |
| Enemy_105 / 106 | 远程 | Enemy_105_Attack（3 键） | 2.767 | 2.00 / 1.50 | 3.7 | 5.2 |

现状出伤 = `spawnDuration + telegraphStart` = `2 × spawnDuration − 0.3`（`spawnDuration ≥ 0.3` 时）。
→ 光修 InsertCallback 顺序只能回到「出伤 = spawnDuration」，而 spawnDuration 与 clip 命中帧的对应关系全靠人工维护，仍会漂。

### 1.4 其它已核实的前提
- 全部 Attack / CAttack 状态 `speed = 1.0`；`Idle → 攻击` 过渡时长都是 0（1011 的 Idle 状态 speed 是 0.75，与本方案无关）。
- `ApplyHitStop` 用 `_animator.enabled = false` 冻结动画（`Enemy.cs:2260-2296`），`UpdateHitStop` 用 `Time.unscaledDeltaTime` 计时；DOTween 不受影响继续跑 → 卡肉期间「位移/出伤」与「精灵」会脱节（现状）。
- 受控验证（临时 Animator + `Enemy_1011.controller`）：Animator 被禁用期间 `GetCurrentAnimatorStateInfo(0)` 仍保留状态名、clip 与归一化时间 → 可以用动画时钟安全判定命中帧。
- 攻击状态的动画速度会被 Walk 补齐改动（`Enemy.cs:791`：`_animator.speed = Mathf.Max(1f, 0.6f/moveSpeed)`，1011 的 moveSpeed 0.3 → 2×），移动结束时复位（`Enemy.cs:1582`）。攻击开始时应显式归一化 `_animator.speed`，避免残留倍速影响命中帧判定。
- `currentStepSpawnDuration` 目前没有任何活着的消费者（仅 `EnemySpriteController` 这个遗留组件引用，而没有任何 prefab 挂它）；`_parryWindowStartTime` 等招架窗口字段从未被赋值（招架窗口当前是死代码）。

## 2. 机制选型（推荐 A）

| 方案 | 做法 | 优点 | 代价 |
|---|---|---|---|
| **A（推荐）动画时钟 + 命中帧声明** | `AttackStep` 声明命中帧（精灵名/键序号）；运行时按 Animator 的 `normalizedTime` 跨过命中帧触发结算；DOTween 时间线由命中帧推导 | 与视觉天然同步；卡肉自动一致（动画冻结→结算冻结）；不必改 clip；配置在 prefab 上可 Inspector 追溯（符合 `design/anti-ghost-reference.md`）；可静态校验 | 需要给每个敌人配一次命中帧；`AttackStep` 加字段 |
| B clip 内置 AnimationEvent | 在每个攻击 clip 的命中键位置加事件，回调 `Enemy` 结算 | 标记与帧同处，clip 重定时自动跟随 | 每个 clip 多一道工序（现有 clip 再生流程都要带）；骑兵挥击/阶段过渡/QTE 复用攻击动作时易误触，需守卫；目前项目无先例（QTE 是另一套用途） |

采用 A；B 作为后续可选升级（若某天 clip 全部走统一再生流程，可以把命中帧同时写进事件做双保险）。

## 3. 数据结构与时间推导

### 3.1 `AttackStep` 扩展（`Assets/Scripts/Enemy/Enemy.cs:63-77`）
```csharp
[System.Serializable]
public struct AttackHitFrame
{
    [Tooltip("命中帧的精灵名（如 Enemy_1011_attack8）；留空则用 keyIndex")]
    public string spriteName;
    [Tooltip("命中帧在 clip 关键帧中的序号（0 基）；spriteName 为空时生效")]
    public int keyIndex;
    [Tooltip("该命中帧是否结算一次伤害/发射（多段连击配多条目）")]
    public bool resolve;
}

public struct AttackStep
{
    // ... 现有字段保持不变 ...
    [Tooltip("命中帧列表；空 = 退回旧的 spawnDuration 行为")]
    public List<AttackHitFrame> hitFrames;
}
```

### 3.2 推导规则（`hitFrames` 非空时，clip 为唯一时间基准）

> 实现说明（P0 实际落地）：运行时**按精灵名匹配屏幕上真实显示的精灵**来判定命中帧，不读 clip 关键帧。
> 好处：不依赖 UnityEditor API（可进包）、对动画重定时/卡肉/变速/多段连招全都自动成立。
> `spawnDuration`（前冲时长）目前仍是手填值，应与命中帧时刻一致 —— 校验器会提示偏差。

- `hitTime`：命中帧在 clip 关键帧上的时刻（由校验器读出，人工核对；运行时不需要）。
- 出伤/击发：命中帧精灵出现在屏幕上的那一帧（`Enemy.UpdateAttackHitFrames`）。
- 前冲位移：`0 → spawnDuration`（应与 `hitTime` 一致，校验器提示偏差）。
- 收招回位：`spawnDuration → planClip.length`（**自动改写 `drawDuration`**），因此 tween 总长 = clip 时长。
- 预警：`BeginWarning(0.3)` 放在 `spawnDuration − 0.3`，且**先构建时间线再 `InsertCallback`**（修掉撑长问题）。
- `isAttackDrawPhase = true` 从命中帧那一刻开始（保持「命中后不可被招架打断」的语义）。
- 攻击冷却仍按 `attackSpeed`（`totalInterval * 0.4 + extraCooldown`）不变。
- 卡肉：`ApplyHitStop`/`UpdateHitStop` 里同时 `Pause()`/`Play()` `_attackTween`，保证精灵、位移、结算三者一起冻结/恢复。
- 兜底：命中帧整段没出现时（动画被受击打断或配置写错），收尾结算一次；配置错误会打印告警。

### 3.3 多段结算
`hitFrames` 里的多条目 = 多次 `PerformAttack()`。当前每个攻击步骤只结算一次伤害，**103 的三连 C 技（CAttack1→2→3）现在是 3 个 clip 但只出 1 次伤**（5.7s 处）。是否改成 3 次结算需要设计决定（见 §6）。

## 4. 代码改动清单

| # | 位置 | 改动 |
|---|---|---|
| 1 | `Enemy.cs:63-77` | 加 `AttackHitFrame` + `AttackStep.hitFrames` |
| 2 | `Enemy.cs:1915-1920` | 预警回调改为**时间线构建完成后**插入；起点改为 `hitTime − 0.3`（附带修掉 1.7s 偏移，所有敌人立即受益） |
| 3 | `Enemy.cs:1874-2011` `PlayAttackAnimationTween` | 解析命中帧 → 推导前冲/收招时长；攻击开始时归一化 `_animator.speed = 1f`；初始化本段运行时命中帧表（含归一化时间、是否已触发） |
| 4 | 新增 `Enemy.UpdateAttackHitFrames()`（由 `Update()` 在 `isAttackAnimating` 时调用） | 读当前 clip + `normalizedTime`；clip 变化（如 103 的 CAttack1→2→3）时按该 clip 重新解析命中帧；跨过命中帧 → 结算一次；守卫：仅在 `isAttackAnimating` 且状态属于攻击状态、且不在 `QTEAttacking`/`Stunned`/`Launched` |
| 5 | `Enemy.cs:1803-1818` `PerformAttack` | 拆出「单次结算」供命中帧调用，保留原有诊断日志 |
| 6 | `Enemy.cs:2260-2296` `ApplyHitStop` / `UpdateHitStop` | 卡肉期间同步暂停/恢复 `_attackTween` |
| 7 | `Enemy.cs:1923-1936` 远程分支 | 发射时刻同样用命中帧（击发帧），飞行时长不变 |
| 8 | 诊断 | 命中帧触发时校验「当前精灵 == 声明精灵」，不一致打 `[ATTACK_ANIM_DIAG]` 警告（延续现有诊断风格） |
| 9 | 编辑器校验器（可放在 `Assets/Scripts/Editor/`，或挂到既有「战斗数值总表」窗口） | 逐敌人/逐步骤输出：clip、命中帧 → 键序号/时刻/归一化值、推导出的前冲/收招/预警时刻；帧找不到、重复、落在 clip 之外 → 告警 |

回退路径：`hitFrames` 为空 → 完全按旧 `spawnDuration`/`drawDuration` 运行（但第 2 项的 1.7s 偏移必须修）。

## 5. 逐敌人迁移表（P1，需要你给命中帧）

| 敌人 | 攻击步骤 | clip | 键数 | 现状出伤 | 新出伤点（等你填） |
|---|---|---|---|---|---|
| 1011 | 普通 | `Enemy_1011_Attack` | 18 | 3.7 | **attack8 = 2.000s（已确认）** |
| 1 | 普通 | `Enemy_1_Attack` | 4 | 0.7 | ? |
| 101 / 109 | 普通 | `Enemy_101_Attack` | 4 | 3.7 | ? |
| 102 | 普通 | `Enemy_102_Attack` | 2 | 1.7 | ? |
| 103 | C 技（三连） | `Enemy_103_CAttack1/2/3` | 2 × 3 | 5.7（仅 1 次） | ? ×3（且是否各出一次伤） |
| 104 / 107 / 108 | 普通 | `Boss_104_Attack` | 5 | 0.9 | ? |
| 104 / 107 / 108 | C 技 | `Boss_104_CAttack` | 5 | 1.7 | ?（注意 §6.3：该 clip 目前根本播不到） |
| 105 / 106 | 远程 | `Enemy_105_Attack` | 3 | 3.7 | 击发帧 ? |

节奏影响（需你确认可接受）：**攻击周期**（现状 tween 总长 + 冷却 → clip 时长 + 冷却，冷却 = `1/attackSpeed × 0.4 + extraCooldown`）普遍变快：

| 敌人 | attackSpeed / 冷却 | 现状周期 | 新周期 |
|---|---|---|---|
| 1011 | 0.5 / 0.8s | 5.50s | 3.73s |
| 101 / 109 | 0.5 / 0.8s | 5.50s | 3.33s |
| 105 / 106 | 0.3 / 1.33s | 6.53s | 4.10s |
| 102 | 0.2 / 2.0s | 4.70s | 2.53s |
| 103 | 0.2 / 2.0s | 9.20s | 5.10s |
| 104 / 107 / 108 普通 | 0.2 / 2.0s | 3.30s | 3.03s |
| 104 / 107 / 108 C 技 | 0.2 / 2.2s | 4.00s | 3.53s |
| 1 | 0.5 / 0.8s | 1.80s | 1.48s |

## 6. 需要你决定的 4 件事

1. **命中帧数据**：§5 表里每个敌人/步骤的命中帧（精灵名即可，如 `attack8`）；1011 已确认。
2. **多段结算**：103 三连是否改成 3 次伤害（还是保留 1 次，只是时刻改为第一段命中帧）。
3. **Boss C 技 trigger**：104/107/108 的 C 技步骤 `animationTrigger` 为空 → 实际播的是普通 `Attack` 动作，`Boss_104_CAttack` 从未被播放（代码里没有任何地方 `SetTrigger("CAttack")`）。是否一并统一为「`isCAttack` 且 trigger 为空 → `CAttack`」。
4. **节奏变化接受度**：出伤普遍提前、敌人攻击周期缩短，是否需要在改完后做一次波次/数值回归（参考 `design/numerical-balance-draft.md`）。

## 7. 验证方法（改完后逐项跑）

- **静态**：编辑器校验器输出全部敌人/步骤的推导结果，无「帧找不到 / 落在 clip 外」告警。
- **运行时（逐帧采样，Locus Play Mode 探针）**：
  - 出伤瞬间精灵名 == 声明的命中帧精灵名（样例：1011 → `Enemy_1011_attack8`，容差 ±1 帧）。
  - 预警窗口 = 命中前 0.3s（1011：1.7–2.0s）。
  - `_attackTween` 总时长 == clip 时长（1011：2.933s），收招结束回到原位。
  - 卡肉期间：精灵冻结、位移冻结、不结算；解除后三者一起恢复。
- **回归**：每个敌人的出伤时刻与周期变化表 → 与用户确认是否可接受。

## 8. 相关文件

- `Assets/Scripts/Enemy/Enemy.cs`（攻击时序、命中帧判定、卡肉、命中结算）
- `Assets/Scripts/Enemy/AttackHitFrameResolver.cs`（新增：trigger → 攻击 clip 解析、精灵名匹配、多段家族计数）
- `Assets/Scripts/Editor/AttackHitFrameValidator.cs`（新增：`Tools/三国杀戮/校验攻击命中帧`）
- `Assets/Scripts/Enemy/EnemyAttackTelegraph.cs`（预警组件，`BeginWarning` 的语义是「命中前 N 秒」）
- `Assets/Scripts/Managers/EnemyManager.cs:236`（伤害投递，无延迟）
- `Assets/Scripts/Player/PlayerState.cs:203`（玩家扣血，无延迟）
- `Assets/Animations/Enemy_1011_Attack.anim`、`Assets/Animations/Enemy_1011.controller`、`Assets/Resources/EnemyPrefabs/Enemy_1011.prefab`
- `Assets/Animations/{Enemy_1,Enemy_101,Enemy_102,Enemy_103,Enemy_105,Boss_104}.controller`（各敌人攻击 clip 与状态接线）

## 9. 实施记录（P0 已完成，2026-10）

### 9.1 代码改动
| 文件 | 改动 |
|---|---|
| `Enemy.cs` | 新增 `AttackHitFrame` 结构 + `AttackStep.hitFrames`；`PlayAttackAnimationTween` 重排（预警回调改到时间线构建之后、按命中帧改写 drawDuration、攻击开始归一到 `_animator.speed = 1`）；新增 `UpdateAttackHitFrames`/`ResolveHitFrame`/`ResolvePendingHitFramesOnAttackEnd`/`SetupHitFrames`/`ClearHitFrames`；`Update()` 挂判定；`ApplyHitStop`/`UpdateHitStop`/`StopHitStop` 同步 Pause/Play 攻击位移 |
| `AttackHitFrameResolver.cs` | 新增（运行时安全）：`ResolveClipForTrigger`/`CountFamilyClips`/`IsAttackClipName`/`MatchesSprite` |
| `AttackHitFrameValidator.cs` | 新增编辑器校验器，覆盖全部敌人 prefab + BossPhaseData 序列；当前 20 个步骤、0 警告 |
| `Enemy_1011.prefab` | `attackSequence[0].hitFrames = [{ spriteName = Enemy_1011_attack8, keyIndex = 8, resolve = true }]` |

### 9.2 实测证据（Play Mode 逐帧）
- 攻击开始 → `attack8` 出现的那一帧（norm 0.690）**同时扣血**：`PLAYER DAMAGE | sprite=Enemy_1011_attack8`；实测攻击开始后约 2.00s（改前是 3.70s，且那时精灵已回到 idle6）。
- 预警窗口 0.30s，结束点与命中帧同帧：telegraph on 1.513s → off 1.814s → attack8 1.823s。
- 动作总长 = clip 时长：`ATTACK MOTION END` 在 2.745s（扣除探针 1 帧滞后约 0.19s ≈ 2.93s）。
- 收招从命中帧开始：命中那帧 `draw=True`，位移从 -10.500 回到 -10.000。
- 卡肉一致性：前摇中注入 `ApplyHitStop(0.4)` → 0.39s 内精灵停在 `attack2`、z 停在 -10.291；解除后继续，命中仍在 `attack8` 那一帧触发（整段顺延 0.4s）。

### 9.3 仍未做（P1/P2）
- 其余敌人的命中帧声明（见表 §5）；未声明者出伤回到 `spawnDuration`（1.7s 偏移已修，但与各自 clip 命中帧仍可能不齐）。
- `spawnDuration` 与命中帧时刻的偏差：校验器只提示、不自动改写；逐敌人给帧后需确认。
- 103 多段连招的整段时长与伤害次数；104/107/108 C 技 trigger（`Boss_104_CAttack` 仍然播不到）。
- 节奏变化回归（§5 表）。
