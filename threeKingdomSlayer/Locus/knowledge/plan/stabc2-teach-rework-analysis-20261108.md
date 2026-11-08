---
id: kd_67d4a4ea-c2c9-4515-a969-025a8dcafd17
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_stabc2_teach_rework_20261108
summary: 指向突刺（stabC2）招式引导的教学流程改造分析（2026-11-08，**已实施**：等敌人抡起霸体招→冻结全场→必须打断→并新增「瞄准」教学）；含三条教学（霸体防御/技能打断/招式引导）的现状对比、三条硬约束（缩放时间 vs 蓄力、tutorialHold 挡不住进行中的攻击、判据要从「打中」改成「打断」）、分块改动方案、用户 5 条拍板与落地对照、验收口径与影响面。
---

# 指向突刺教学（招式引导）的流程改造分析（2026-11-08）

> **状态：已实施（2026-11-08，提交 `a2fa2f82`）**。
> 触发：用户反馈「这个教学其实存在问题，我已将正确的 1011 部署到 N1 开头」，并给出两条要求：
> ① 流程应与「海浪技能教学」「格挡技能教学」类似，**等敌人抬手后再进入教学流程**；
> ② **应该冻结敌人的动作**（当前没有冻结，后面的敌人还在正常出招/运动）。
> 随后用户对第 5 节的 5 条待拍板逐条拍板（见 §5 的「拍板结果」列）→ 按拍板实施。

---

## 1. 需求拆解（用户原话 → 可实现的条目）

| # | 需求 | 落地含义 |
|---|---|---|
| N1 | 这条教学要表达的是「**用指向突刺打断敌人的霸体攻击**」 | 教学对象 = 会打 C 技的敌人（1011 ✓）；判据 = **打断**了它的霸体攻击 ✓（不是「打中任意敌人」） |
| N2 | 流程应与「海浪技能教学」「格挡教学」类似，**等敌人抬手后再进入教学流程** | 开拍时机：等目标**抡起招、抬到顶**的那一刻 ✓（判据沿用技能打断拍那一套：`isAttackAnimating` + `TimeToAttackEnd() <= 抬手阈值` ✓） |
| N3 | **应该冻结敌人的动作**（目标停在抬手；后面的敌人不动、不出招） | 目标：**暂停进行中的攻击 tween** ✓；全场：`tutorialHold = true` ✓（这正是「按住敌人」那个开关 ✓） |

---

## 2. 现状对比：三条「打断类」教学

| | 霸体防御拍（E0 波0） | 技能打断拍（E0 波1 = 海浪/旋风） | **指向突刺教学（现在）** |
|---|---|---|---|
| 目标选择 | 会打 C 技的敌人 | 会打 C 技的敌人 | **最前排任意敌人**（`FindTargetEnemy` → 只要 row 0 ✓） |
| 开拍时机 | 等 C 技抬手 | ①等玩家选完三选一 ②等敌人抡招到顶 | **进关立刻开** ✗ |
| 冻结方式 | 整局 `timeScale = 0` | 整局 `timeScale = 0`（且 `holdEnemies: false` → 敌人照常动 ✓） | **只按住敌人**（`tutorialHold` ✓），**不冻时间** ✗、**不停进行中的攻击** ✗ |
| 玩家动作 | 按住防御键 | 点技能槽 | 点一下 → 蓄满 → 松手 |
| 完成判据 | 进入防御姿态 | 技能进冷却 | **派生招式打中任意敌人** ✗（打中杂兵也算「学会」） |
| 手势闸门 | BlockAll（不许出招） | BlockAll + 允许放技能 | Tap →（命中后）ComboHold |

**差在哪**：指向突刺这一条**既不等抬手、也不冻结、判据也不是「打断」** —— 它现在更像「随便找个敌人，把招式打出来就行」。

---

## 3. 三条硬约束（决定「不能照抄海浪/格挡」）

### 约束 1：整局冻结会让「按住蓄满」永远蓄不满 ✗

```csharp
// Assets/Scripts/Player/InputManager.cs:130
public float HoldDurationSeconds => isTouching ? Mathf.Max(0f, Time.time - segmentStartTime) : 0f;
```

`Time.time` 是**缩放时间** → `timeScale = 0` 时按住时长不涨 → **蓄力条永远涨不上去** → 指向突刺出不来。

⇒ **结论：这一拍不能像海浪/格挡那样冻整个游戏**，只能**冻敌人**（这正好对上需求 N3 ✓）。

### 约束 2：`tutorialHold` 挡不住「已经挥出去的那一刀」 ✗

```csharp
// Assets/Scripts/Enemy/Enemy.Attack.cs:18
if (tutorialHold && !isAttackAnimating) return;   // 只挡「新起的一刀」，已经挥出去的照常播完、照常出伤
```

⇒ 要做到「停在抬手瞬间」，需要**暂停进行中的攻击 tween**。工程里已有这套机制（目前给卡肉/冰冻用）：
`Enemy.PushSuspend(SuspendSource)` / `PopSuspend` ✓ → `PauseAttackTweenForSuspend()` ✓ → 建议新增一个来源 `SuspendSource.Tutorial` ✓（语义清晰，不污染卡肉逻辑 ✓）。

### 约束 3：判据要从「打中」改成「打断」

现在 `OnBeatEnemyDamaged`：**命中任意敌人**就收拍 ✓ → 玩家打中一只杂兵也算「学会指向突刺」✗ → 与教学意图不符。

⇒ 新判据建议：派生招式命中**目标那只** ✓ 且**它的霸体攻击被打断**（`CancelAttack` ✓ / 出伤没发生 ✓）。
（指向突刺 `Zhangfei_StabR2` 的 `poiseDamage = 30` ✓ → 具备破霸体的能力 ✓，但要实机验一次 ✓。）

---

## 4. 改动方案（分块，含落点）

### A. 拍流程改造 —— `Assets/Scripts/Tutorial/TutorialStageDirector.Hints.cs`（`ComboChargeRoutine`）

| 步 | 做什么 | 复用/新增 |
|---|---|---|
| A1 | 目标改为**会打 C 技的敌人**（`isCFrame` / `isCAttack` ✓）优先；找不到就退而求其次 | `Targets.cs` 新增一个「找会打霸体招的敌人」的选择器（**不要改 `FindTargetEnemy` 的通用语义** ✗） |
| A2 | 开拍时机：先**只等不教** → 等目标 `isAttackAnimating` ✓ 且 `TimeToAttackEnd() <= 抬手阈值` ✓ | 判据抄技能打断拍 ✓（`skillPromptToEndSeconds` ✓） |
| A3 | 抬手到顶 → **冻结敌人**：目标 `PushSuspend(Tutorial)` ✓；全场 `tutorialHold = true` ✓ | 新增 `SuspendSource.Tutorial` ✓（`Enemy.Combat.cs` ✓） |
| A4 | **不冻时间** ✓（约束 1 ✓）→ 玩家的蓄力照常 ✓ | — |
| A5 | 冻结后再播台词 + 示范 → 玩家点一下 → 蓄满 → 松手 ✓ | 现有 `guide_move_stabc2_phase2` ✓ |
| A6 | 判据：**打断了目标** ✓ → 收拍 ✓ → 解冻（`PopSuspend` ✓ + `tutorialHold = false` ✓） | `OnBeatEnemyDamaged` 要按拍型分流 ✓ |
| A7 | 没打断 → **允许重试** ✓（复用上一轮加的空挥兜底 ✓） | ✓ |

### B. 拍参数 —— `TutorialGestureStep`

工程里**已经有** `waitForCAttack` / `freezeOnCAttack` 两个字段 ✓（本项目自带的、给别的拍用的 ✓）→ 这一拍可以直接复用它们的语义 ✓，**不要新造字段** ✓。

### C. 与上一轮三处修复的连带（要一起改，否则互相打脸）

| 上一轮修复 | 冲突 | 建议 |
|---|---|---|
| ③「没有够得着的目标就**放开行军**」 | 与 N3「冻结敌人」**直接冲突** ✗ | 限定范围：只在**等抬手阶段**允许放开（好让目标走到位 ✓）；**进入冻结后不再放开** ✓ |
| ①「命中了才换闸门」 | 无冲突 ✓ | 保留 ✓ |
| ②「派生招式空挥 → 回阶段 1」 | 无冲突 ✓ | 保留 ✓（现在还兼做「没打断 → 重试」✓） |

---

## 5. 待拍板（**已拍板，2026-11-08**）

| # | 问题 | 拍板结果 | 落地 |
|---|---|---|---|
| 1 | 「等抬手」是否必须等 C 技？ | **跟之前的教学一样**（技能打断拍口径：**任何一次攻击**的抬起到顶都冻） | `comboWindupFreezeSeconds = 0.35f` + `TimeToAttackEnd()` 判据 ✓ |
| 2 | 冻结范围 | **冻全场** | `PushSuspend(Tutorial)` 全场 + `tutorialHold` ✓ |
| 3 | 判据 | **必须打断**；玩家必须**把手指按在敌人位置上**松手才释放，否则武器退回蓄力漂浮位、要重新蓄力 | 判据 = `_comboTargetWasAttacking && 目标.attackInterrupted` ✓；「瞄准」教学 = 新增第三条台词 `guide_move_stabc2_phase3` ✓（那条「退回收蓄力位」是招式自身已有的行为 ✓，不是新写的） |
| 4 | 等待抬手期间玩家能不能自由出招 | **不能**（那时玩家正在 stabC2 连段里） | 阶段 2 的闸门本就是 ComboHold（只允许蓄力松手）✓ |
| 5 | 玩家迟迟不操作怎么办？ | **不给兜底**（强制教学，学不会就不能过）；**要加「瞄准」教学**（攻击 / 蓄力 / 瞄准 三步） | 无跳过路径 ✓；新增第三条台词 ✓ |

---

## 6. 验收口径（Play Mode）

1. 进 N1 → 这一拍**不立刻开** ✓ → 1011 抡起 C 技 → **冻结** ✓（目标停在抬手 ✓；后面敌人不动、不出招 ✓）。
2. 玩家点一下 → 蓄满 → 松手 → **打断** C 技 ✓ → 收拍 ✓ → 战斗继续 ✓。
3. 蓄满但**打偏**（没打断）→ 允许重试 ✓ → 不卡死 ✓。
4. 目标在等待期间被打死 → 换目标继续等 ✓（或跳过这一拍 ✓）。
5. 不冻结整个游戏（`timeScale` 仍为 1 ✓）→ 蓄力能蓄满 ✓。

---

## 7. 影响面（预估）

| 文件 | 改什么 |
|---|---|
| `Assets/Scripts/Tutorial/TutorialStageDirector.Hints.cs` | `ComboChargeRoutine` 流程（A2~A7 ✓） |
| `Assets/Scripts/Tutorial/TutorialStageDirector.Targets.cs` | 新增「找会打霸体招的敌人」（A1 ✓） |
| `Assets/Scripts/Tutorial/TutorialStageDirector.Dialogue.cs` | 判据按拍型分流（A6 ✓） |
| `Assets/Scripts/Enemy/Enemy.Combat.cs` | `SuspendSource` 加一个 `Tutorial`（A3 ✓） |
| 台词资产 | 可能要加一条「他抡招了 → 用指向突刺打断」（文案进《教学字幕总表》✓） |
| 输入层 | **不需要改** ✓（按约束 1 用「只冻敌人」绕开 ✓） |
