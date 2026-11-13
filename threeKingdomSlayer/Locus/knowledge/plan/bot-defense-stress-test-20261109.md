---
---
id: kd_b7f3c9d2-5a41-4e8b-9c07-2f6d1e4a8b30
summary: 机器人加防御能力并做格挡压测（2026-11-09）：BattleAutoPlayer 新增防御键长按/接管进行中战斗；实测走真实路线跑通 N1→E0→N3 与 N2，格挡全程 0 报错、无「防御锁死 Boss」，但暴露「受击挂起→攻击动画卡死自愈→敌人 4~5 秒不出手」与「防御与出招互斥→推进变慢」两条；BOSS 300 秒 0 伤害是机器人输出被防御拖慢所致。
injectMode: inherit
aiEditMode: inherit
---

# 机器人格挡压测（2026-11-09）

> 起因：用户要求「多跑点机器人测试，模拟玩家玩，减少攻击频率，多用防御键格挡，看看格挡会不会产生其他问题」。

## 1. 机器人新增能力（`Assets/Scripts/Utils/BattleAutoPlayer.cs`）

| 能力 | 说明 |
|---|---|
| `useDefense` / `defendChance` / `defendHoldSeconds` | 敌人进入攻击前摇时**按住防御键**（hold 语义） |
| 走**真实指针路径** | 按下/松开都用 `EventSystem` 派发 `IPointerDownHandler/IPointerUpHandler` → 按钮自己的静音 / 区域识别 / 预输入 逻辑照常被压到 |
| **不引用 UI 程序集** | `DefenseButton` 在 Locus.UI，机器人在 Locus.Flow → **Flow 不能依赖 UI**（分层铁律）→ 改成「按名字找物件 + EventSystem 接口派发」 |
| `DetectBattleInProgress()` | 机器人在**战斗开始之后**才被创建时，自动接管当前战斗（否则收不到出波事件 → 永远站着不动） |
| 防御失败原因统计 | `DebugStats` 输出：按下次数 / 生效次数 / 防御生效帧 / 失败原因分布 |

### 1.1 第二轮改造（2026-11-09 晚）：**真实模拟玩家 + 自查**

> 起因：用户指出「我看你跑的时候主界面那些 UI 都还没消失，你并没有真实的模拟玩家游玩的方法」。
> 根因确认：那几轮是用 `StageController.StartRouteBattle` 直开战斗的 → **路线停在 `Ready`（就绪态）**
> → `HubHudModeSwitch` 把 `Side / Bottom / Debug / Top`（落地页专属 UI）留在屏上、`GameplayInputGate` 也被关 →
> **那几轮根本不是关卡态**，测出来的数据不作数。

| 新能力 | 说明 |
|---|---|
| `clickHubStartButton` | 落地页就绪后**自己点「开始关卡」**（EventSystem 指针路径），不调路线 API |
| `assertInBattleState` | 每秒自查：**战斗中路线还停在就绪态** = 落地页 UI 一定还挂在屏上 → 报一条错（这正是上面那个坑的哨兵） |
| 卡死检测 | 进展签名（路线阶段/节点/波次/存活数/出招数/UI 点击数）不变超过 `stallSeconds` → 报一条 + **截图留证** |
| 截图留证 | `Library/Locus/tmp/bot/NN_*.png` |
| 运行报告 | `Library/Locus/tmp/bot/run-report.txt`：出招数 / UI 点击数 / 防御统计 / 卡死次数 / 不在关卡态次数 / 全部问题清单 |

**正确起跑顺序**（对比上一版）：
```
旧：rt.Begin(cfg) → 直接 sc.StartRouteBattle(cfg)   ✗ 路线还在就绪态
新：rt.Begin(cfg) → 等 rt.IsPrepared → rt.RunPrepared()  ✓ 等价于玩家点「开始关卡」
```
（`RunPrepared` = `FakeRouteRuntime.Entry.cs:188` 把 `_prepared` 置 false → HUD 进关卡态 + 输入门打开。）

**终验结果**（真实路线 N1→E0）：
```
[AutoPlayer] 点「开始关卡」（落地页 → 关卡态）
[AutoPlayer] 战斗开始：Formal_N1_Battle → 战斗结束：胜利=True → 战斗开始：Formal_E0_Battle
报告：出招=108  UI 点击=4  防御按下=99/生效=87（12 次进预输入）  卡死=0  不在关卡态=0  问题清单=0
```

## 2. 测试起跑方式（重要，之前踩过）

```csharp
var rt = RouteFlowHub.Current as FakeRouteRuntime;
rt.Begin(rt.RouteConfig);                    // 只是「准备落地页」→ HUD 仍是落地页模式
while (!rt.IsPrepared) await 帧;             // 等就绪
rt.RunPrepared();                            // ← 等价于玩家点「开始关卡」
```

- `Begin()` 之后不调 `RunPrepared()` → 路线停在 `Ready` → **战斗 HUD 被收成落地页模式**（防御键不可见、`GameplayInputGate` 被关）→ 机器人按防御按了 100+ 次也一次不生效。
- `GameplayInputGate` 是**按 owner 计数**的门；`PrepareForEntry` 协程在 `RunPrepared` 之后才把 `_prepared` 置 true（`FakeRouteRuntime.Entry.cs:164`）→ 真机上不冲突，但**手动起跑必须走 Begin→等就绪→RunPrepared**。

## 3. 实测结果（attackInterval=1.6s，defendChance=0.9，机器人血量拉满）

| 场景 | 结果 |
|---|---|
| 路线 N1 → E0（5 波） | **通关**；防御按下 286 次 / 生效 231 次；防御占约 54% 时间；**Console 0 报错** |
| E0 wave2（1051 弓手） | 「远程攻击被掐断，箭未发出 → 收尾兜底补发一次」多次触发 → **BUG B 的兜底补发在实战中持续救场**，一次都没真哑火 |
| N3（5 波，含 106 远程） | 同上；`106` 也会被掐断 → 兜底补发救回 |
| N2 wave2（104 Boss） | Boss 血量 **300 秒 0 伤害**、Boss **一次都没进入攻击态**（一直在「等前方排清空」） |

## 4. 结论：格挡本身没带来新问题，但暴露了两条既有机制

### 4.1 ✅ 没有出现「一直防御把霸体 Boss 锁死」

- **防御键不做招架判定**（`DefenseButton` 注释：**没有接触帧、不反弹飞行物、敌人不会被打断**）→ 防御不会打断敌人的攻击。
- 实测 Boss 虽未出手，但那是**它自己**在等前方排清空（`Boss等待前方排清空(row=N)`），与防御无关。
- 真正会锁死 Boss 的是**玩家普通攻击的连续打断**（`打断攻击成功`），不是防御键。

### 4.2 ⚠️ 防御键的「预输入」路径工作正常，但代价是**输出被拖慢**

- `defendChance=0.9` 时机器人有 **41 次**按到「出招动作锁（软门）→ 进预输入」→ 按钮记住这次按压、连段结束自动进防御 ✓ 机制正常。
- 但**防御与出招互斥**（`PlayerState.IsDefendingActive` → 不能出招）→ 机器人一看到敌人起手就防御 → 输出几乎减半 → Boss 300 秒没掉血。
- ⇒ **这是设计使然，不是 bug**；但它意味着「习惯性防御」的玩法会显著拖慢推进，值得在手感上留意。

### 4.3 ⚠️ 真正值得跟进的一条：**「攻击动画卡死自愈」频繁触发 → 敌人 4~5 秒不出手**

- 现象：日志里 `攻击动画卡死自愈: #NN, state=Attacking, tweenAlive=False` 反复出现；对应 `Enemy.Attack.cs` 的看门狗
  （`isAttackAnimating` 还挂着但攻击 tween 已死 → 超时后 `ResetStalledAttack`）。
- 成因：**受击硬直会把攻击 tween 挂起（Pause）**，玩家打得太密时 tween 长期挂着 → 看门狗到点自愈 → **敌人这 4~5 秒完全不出手**（`存活但无人攻击` 的最长连续 46s 就是这么来的）。
- 与既有记录一致：`memory/known-issues.md` 的「敌人受击频繁 → 攻击卡死自愈 → 一段时间不还手」；`memory/parry-window-design.md` 也记着同样的症状。
- ⚠️ 这条**不是本轮引入的**：2026-11-09 把攻击 tween 改成独立 target 之前，`transform.DOKill(transform)` 的连带杀是更主要的一根因；改完之后**连带杀没了，但受击挂起这条路径还在**。

### 4.4 顺带修掉的诊断噪声

DOTween **池化复用 `Sequence` 对象** → 被回收的对象仍带着旧的 OnKill 回调 → 近战敌人被杀一次能刷几十条 `ArrowDiag`。
→ `OnAttackTweenKilled` 改成**只在「箭还待发」时记录**（近战不归这条诊断管）。

## 5. 遗留 / 待用户拍板

1. **「攻击动画卡死自愈」导致敌人长时间不出手** —— 要不要把看门狗的 `guard` 收紧，或让受击挂起不累积到看门狗阈值？（本轮只记录，未改行为）
2. **防御期间敌人照打玩家** —— 本轮机器人血量被拉满，没测到「防御免伤」的真实数值；也没触发 `[Guard] 破防`（需要让玩家真的挨打）。
3. **`[Enemy] 命中帧已声明但解析不到攻击 clip（trigger=CAttack）`** —— 1011 的 CAttack clip 解析不到（既有警告，与格挡无关）。
