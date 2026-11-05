---
id: kd_3e79988f-557e-49b3-8a29-5ab6b1979324
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_feel_wp_20261105
summary: 手感优化的实施工作包（2026-11-05）：按 W0~W8 拆成观测/缓冲升级/缓冲反馈/承诺点/取消窗口与命中取消/空挥一致/三选一波末/教学推进与长按选择/难度重标九包，每包给前置、落点文件、改动要点、风险、验收口径与回滚方式；含 5 条用户反馈到工作包的映射表、依赖顺序与"不要做"清单。
---

# 手感优化实施工作包（2026-11-05）

> **状态：计划，未实施。** 规范见 `design/action-game-feel-and-input-buffering.md`（本文只管"怎么改、怎么验、怎么退"）。
> 代码行号对应 HEAD `8b9a3438` 附近；动手前先回读确认。

---

## 0. 与 5 条用户反馈的映射

| 用户反馈 | 工作包 | 说明 |
|---|---|---|
| ① 需要动作游戏的**预输入** | **W1**（缓冲升级）+ **W4**（取消窗口） | 预输入 = 记住 + 最早可执行帧兑现 + 可见反馈（W2） |
| ② 教程**不应点任意处都能继续** | **W7** | `BattleDialogueView` 裸输入路径 |
| ③ 招式**不跟手** | **W3**（按下即出）+ **W1** | 延迟的主因是承诺点太晚 + 输入被丢 |
| ④ 三选一**长按选择** | **W7** | `UpgradeCard` 改按住计时 |
| ⑤ 三选一**波次结束弹出** | **W6** | 架子已在 `StageRunFlow.OnWaveCleared` |

---

## 1. 依赖顺序

```
W0 观测与基线 ──┬─► W1 缓冲升级 ──► W2 缓冲反馈 ──┐
                │                                 ├─► W8 难度重标（跟随每一档）
                └─► W3 承诺点(按下即出) ──► W4 取消窗口/命中取消 ──► W5 空挥一致 ──┘
W6 三选一波末 ──►（独立，但同样触发 W8）
W7 教学推进 + 长按选择 ──►（独立 UI 线，不依赖上面）
```

**W0 必须先做**：没有基线，后面每一档都只能靠"感觉"验收，改动会互相盖住对方的收益。

---

## W0 观测与基线（F0）

| 项 | 内容 |
|---|---|
| **目标** | 把"手感"变成可测的 6 个指标，并拿到**改前基线** |
| **前置** | 无 |
| **落点** | `Assets/Scripts/Utils/LevelTelemetry.cs`（新增事件类型）<br>`Assets/Scripts/Player/InputManager.Pointer.cs`（记录按下/抬起时刻）<br>`Assets/Scripts/MoveSystem/PlayerMoveStateMachine.cs`（记录起手/缓冲写入/消费/过期）<br>`Tools/analyze_level_telemetry.py`（新增 `feel` 子命令） |
| **改动要点** | 新增事件：`input` / `input_committed` / `input_buffered` / `input_executed` / `input_expired` / `attack_start`；每条带 `t`（realtime）与 `frame` |
| **风险** | 埋点本身会拖慢热路径 → 只在 `LevelTelemetry.Enabled` 时记录；不改游戏逻辑 |
| **验收** | 跑一轮机器人（`BattleAutoPlayer`）+ 一轮真机手按，产出基线表：承诺延迟中位数、兑现延迟、丢弃计数、连段密度 |
| **回滚** | 纯新增，删掉埋点调用即可 |

---

## W1 缓冲升级（F1）★核心

| 项 | 内容 |
|---|---|
| **目标** | 合法输入**丢弃率 0**，兑现延迟 ≤ 0.25s，且**跨招式**兑现 |
| **前置** | W0（要有基线对比） |
| **落点** | `Assets/Scripts/MoveSystem/PlayerMoveStateMachine.cs`（主战场）<br>`Assets/Scripts/Player/Combat/AttackSystem.cs`（动作锁静默丢弃那一处）<br>`Assets/Scripts/Data/GameplayInputGate.cs`（闸门优先级） |
| **改动要点** | ① 缓冲加时间戳 + `bufferLifetime`（默认 0.25s，可配）<br>② **缓存入口放宽**：`CanResolve` 为假（无匹配边）时**也缓存**，兑现时再判能不能执行<br>③ **`ExitToNeutral` 不再清缓冲**（保留到过期）；中立态第一帧检查并兑现<br>④ 只保留最近一条（新的覆盖旧的）<br>⑤ **`chargeLevel >= 1` 不进缓冲**（蓄力要实时按住）<br>⑥ 防御键 / UI 点击 / QTE 输入 / 被教学闸门拒绝的手势不进缓冲<br>⑦ 每帧检查过期 → 丢弃 + 计数 |
| **风险** | ① 跨招式兑现会把"误触的滑动"也打出来 → 用 0.25s 有效期压住<br>② 缓冲跨过教学拍边界 → **教学拍切换必须清空缓冲**（`NotifyExternalReset` 扩展）<br>③ 缓冲跨过三选一面板 → 面板打开时 `GameplayInputGate` 拦截，面板关闭那一点击仍可能变成攻击 → 现有 `BlockInputForFrames(2)` 在 W3 之后要加长（按下即出后承诺发生在按下帧） |
| **验收** | ① 丢弃率 = 0（合法输入）<br>② 兑现延迟 ≤ 0.25s<br>③ **回归 `design/combo-move-state-machine.md` §12「最容易丢的七条」逐条过**<br>④ 教学关跑通（手势拍全部能收） |
| **回滚** | `bufferEnabled` 已有开关 → 置 false 即回到旧行为 |

---

## W2 缓冲可见反馈（F1b）

| 项 | 内容 |
|---|---|
| **目标** | 玩家看得见"我按下的那一下被存着了" |
| **前置** | W1 |
| **落点** | 优先复用 `Assets/Scripts/UI/...` 的 `AttackTapIndicator` / `ChargeIndicatorController`；如需新表现 → 美术依赖 |
| **改动要点** | 缓冲写入时给指示器一次短闪；**过期**可选给一次极轻的负反馈 |
| **风险** | 反馈太强会变成"每按一下都在闪" → 用最轻的一档 |
| **验收** | 实机：按早了能看见"记住了"，且不干扰读招 |
| **回滚** | 一个开关 |

---

## W3 承诺点：按下即出（F2）★手感主刀

| 项 | 内容 |
|---|---|
| **目标** | 承诺延迟 ≤ 1 帧（16.7ms） |
| **前置** | W0、W1（缓冲先到位，改写才不会丢输入） |
| **落点** | `Assets/Scripts/Player/InputManager.Pointer.cs`（按下帧提交）<br>`Assets/Scripts/Player/InputManager.Gesture.cs`（移动超阈值 → 改写）<br>`Assets/Scripts/Player/Combat/AttackSystem.cs`（`TryCancelLightAttackStartup()` **已有但零调用**）<br>`Assets/Scripts/Attack/StabSweepEffect.Hit.cs`（`TryCancelBeforeHit()` **已有但零调用**）<br>`Assets/Scripts/MoveSystem/PlayerMoveStateMachine.cs`（`NotifyExternalReset()` **已有但零调用**）<br>轻攻击资产（判定帧 12% → ~20%，见风险） |
| **改动要点** | ① pointer down → 提交 Tap 根招式 → 立刻进起手<br>② 移动超阈值且**未到判定** → 取消起手（`TryCancelLightAttackStartup`）→ 提交新手势<br>③ 判定已过 → 不改写（正常两招）<br>④ UI 之上的按下不提交（沿用 `IsPointerOverUIAt`）<br>⑤ 防御键优先级高于战斗手势 |
| **风险** | ① **改写窗口太短 → 留下误伤轻攻击**（判定帧 12% ≈ 0.054s，滑动识别要 50px/180px·s⁻¹）→ 建议把轻攻击判定帧后移到 ~20%，代价是命中晚 0.036s<br>② 中立态按住无意义（2026-11-05 已禁蓄力）→ **当年撤回按下即出的理由已消失**，但要确认没有别处依赖"按住 = 蓄力"<br>③ 机器人走 `SubmitGestureForDebug` → 同样受影响，难度基线要重取 |
| **验收** | ① 承诺延迟中位数 ≤ 16.7ms<br>② 快速点+划不产生额外轻攻击伤害（改写生效）<br>③ 教学手势拍全部仍能收<br>④ 回归 §12 七条 |
| **回滚** | 一个 Inspector 开关（沿用 `combo-move-state-machine.md` §10.2 的"运行时开关"方法） |

---

## W4 取消窗口 + 命中取消（F3）

| 项 | 内容 |
|---|---|
| **目标** | 打中后**同帧**可接下一招；空挥走完收尾（有重量） |
| **前置** | W3（承诺点先定，否则"接"的时机没意义） |
| **落点** | `Assets/Scripts/Data/AttackSkillConfig.cs`（取消窗口字段，或按判定结束派生）<br>`Assets/Scripts/MoveSystem/PlayerMoveStateMachine.cs`（取消窗口判定 + 中立兑现）<br>`Assets/Scripts/Player/Combat/AttackSystem.cs`（**暴露"本次是否命中"** —— 现在 `hitAny` 是局部变量，状态机拿不到）<br>`Assets/Scripts/Core/HitFeedbackManager.cs`（卡肉广播已有；**让 `AttackSystem` 的动作锁也随卡肉冻结**） |
| **改动要点** | ① 取消窗口锚**伤害判定结束**（比例），攻速变化自动跟随<br>② 命中取消：只有 `hitAny` 才开放取消窗口<br>③ 卡肉期间：动作锁**冻结**（现在继续走 → 与招式时钟错位，`combo-move-state-machine.md` §14 坑点 1）<br>④ 卡肉结束那帧兑现缓冲 |
| **风险** | ① 取消窗口提前 → 与 §11.8「0.35~0.95 实测出得太快」的旧结论冲突 → **不要照抄 0.35/0.65**，按判定结束派生再逐招调<br>② 命中取消可能让连段过于顺 → 用 `repeatSelf` 与串尾节点控制上限<br>③ 武器连续性（§19）：取消点不能早于武器到位 |
| **验收** | ① 命中后**同帧**可接下一招（埋点 `input_executed.path = cancelWindow`）<br>② 空挥不可取消<br>③ 武器不闪断（实机看） |
| **回滚** | 取消窗口开关 + 命中取消开关各一个 |

---

## W5 空挥一致性（F4）

| 项 | 内容 |
|---|---|
| **目标** | 打空与打中的节奏差异可解释（现在：打空无锁 → 无限快打） |
| **前置** | W4 |
| **落点** | `Assets/Scripts/Player/Combat/AttackSystem.cs`（空挥时也给动作锁，但不结算能量/被动/冷却 —— §12 ②必须保住） |
| **改动要点** | 明确"空挥 = 走完收尾"；或在空挥时给更短的锁 |
| **风险** | 会让"点空"变慢 → 玩家觉得迟钝；先量再定 |
| **验收** | 空挥/命中的节奏差符合预期；机器人难度表影响可控 |
| **回滚** | 开关 |

---

## W6 三选一改波次结束弹出（#5）

| 项 | 内容 |
|---|---|
| **目标** | 升级不再当场打断战斗；选择攒到波末 |
| **前置** | 无（独立） |
| **落点** | `Assets/Scripts/Core/UpgradeChoiceManager.cs`（`OnPlayerLevelUp` 只入队）<br>`Assets/Scripts/Managers/StageRunFlow.cs`（`OnWaveCleared` 统一 flush —— **等待逻辑已存在**）<br>`Assets/Scripts/UI/BattleHUD.cs`（升级即时反馈：HUD 等级数字已有，可加轻提示） |
| **改动要点** | ① `OnPlayerLevelUp` → 只 `_pendingLevelUps++` + 入队，**不弹窗、不暂停**<br>② `OnWaveCleared` → flush 队列 → 弹窗（此时暂停合理）→ 选完 → 出下一波（现成链路）<br>③ 物品三选一（Boss 锦囊）走同一队列<br>④ 关卡结算前必须再 flush 一次（否则升级丢失） |
| **风险** | ① **教学关的三选一拍**（`Choice3@0` + `forcedChoices`）目前可能靠"升级立刻弹"触发 → 必须逐拍回归<br>② 路线流程（`FakeRouteRuntime` 用 `StageController` 当宿主）要实跑确认波末回调到位<br>③ 一次波连升多级 → 波末连弹多次（`_pendingQueue` 已支持）<br>④ **战斗不再被打断 → 玩家 DPS 曲线变了 → 铁律八与难度表要重标**（并入 W8） |
| **验收** | ① 波内升级不再弹窗<br>② 波末弹出、选完出下一波<br>③ 教学关三选一拍仍能收（存档标记 `Choice3@0` 正确写入）<br>④ 一关下来不丢升级 |
| **回滚** | 一个开关（立即弹 / 波末弹） |

---

## W7 教学推进 + 三选一长按选择（#2 #4）

### W7a 教学台词推进（#2）

| 项 | 内容 |
|---|---|
| **落点** | `Assets/Scripts/UI/BattleDialogueView.cs` |
| **改动要点** | ① `Update()` 里 L80-86 的裸 `Input.GetMouseButton*` 路径：**要么删除，要么先做 UI 命中检查**（复用 `InputManager.IsPointerOverUIAt` 的同类判定）<br>② 推进只认：点气泡本体 / 点「继续」按钮 / 教学台词自动推进<br>③ 保留 `PointerAdvanceEnabled`（手势拍期间关闭） |
| **风险** | 新手不知道要点哪 → 气泡上要有"点击继续"提示（**依赖美术**）；教学逐拍回归 |
| **验收** | 战场上点敌人**不会**推进台词；点气泡/按钮会 |

### W7b 三选一长按选择（#4）

| 项 | 内容 |
|---|---|
| **落点** | `Assets/Scripts/UI/UpgradeCard.cs`（`Button` → 按住计时）<br>`Assets/Scripts/UI/UpgradeChoicePopup.cs`（`OnCardClicked` 两段式 → 改为长按确认）<br>`Assets/Scripts/Tutorial/TutorialStageDirector.cs`（`TryGetChoiceScreenPoint` / `SetChoiceSelectable` 附近，教学提示要表达"按住"） |
| **改动要点** | ① 按下开始计时（0.3–0.4s），按住期间画进度（环形/充能），到时直接应用<br>② 提前松手 = 取消（不选中）<br>③ 首次按下即高亮（"正在选"），松手未满回弹<br>④ 教学那一拍的手势提示要改成"按住这张卡" |
| **风险** | ① 每次选择多 0.3–0.4s（一关 10+ 次）→ **与 W6 合起来看**：波末弹 + 长按选择 = 波间停顿变长，考虑"连续两次选择是否只确认最后一次"<br>② 进度环**依赖美术**<br>③ 与机器人/自动化选择流程的兼容性（`BattleAutoPlayer` 怎么选卡？） |
| **验收** | 一次长按完成选择；松手取消；教学拍能收 |

---

## W8 难度重标（跟随 W1–W6）

| 项 | 内容 |
|---|---|
| **目标** | 手感改动后，难度表与铁律八重新标定 |
| **落点** | `Tools/build_first_level_difficulty_table.py`、`Tools/analyze_level_telemetry.py`、`Locus/knowledge/design/stage-configuration.md` §5.6.1 |
| **改动要点** | 每改一档手感（W1/W3/W4/W6）→ 重跑机器人 → 更新难度表 → 更新 §5.6.1 的实测数字 |
| **风险** | **不要把手感改动与数值改动混在一次交付里** → 否则无法归因 |
| **验收** | 难度表与实测一致；铁律八（普通波 30–55s）在新节奏下仍成立 |

---

## 2. "不要做"清单

1. **不要**把手感改动和数值/内容改动混在一次提交里（无法归因、无法回滚）。
2. **不要**跳过 W0 直接改 —— 没有基线就只剩"感觉"，而手感争论就是这么拖下去的。
3. **不要**用 `Time.timeScale = 0` 造缓冲/取消（`InputManager.IsPointerDown` 要求 `timeScale > 0`，见 `combo-move-state-machine.md` §11.3 注释）。
4. **不要**在缓冲里存**蓄力**手势（蓄力等级是实时算的，延迟兑现会失真）。
5. **不要**改 `Assets/**/*.meta`（Tuanjie GUID 加密）。
6. **不要**用 `git add -A` 提交（会把别人的手改卷进来）。
7. **不要**把"起手 → 判定"这段时长压短换手感（那是动作设计，压短会让打击感变轻）。

---

## 3. 每档交付的固定动作

1. 改代码 → `unity_recompile` → Console 0 error。
2. 跑机器人（`BattleAutoPlayer`）→ `Tools/analyze_level_telemetry.py` 出对比表。
3. **实机手按**（手感无法自动化，`combo-move-state-machine.md` §10.2）→ 记 6 个指标。
4. 更新 `design/action-game-feel-and-input-buffering.md` 的实测数字与 `design/combo-move-state-machine.md` §11/§14（实现状态与坑点）。
5. 提交（**显式路径**）→ 推送。

---

## 4. 相关文档

| 文档 | 关系 |
|---|---|
| `design/action-game-feel-and-input-buffering.md` | **规范**（本文的上位） |
| `design/combo-move-state-machine.md` | 招式表 / 窗口 / 缓冲现状 / 回归清单 / 已撤回方案 |
| `design/attack-cooldown.md` | 动作锁语义 |
| `plan/input-and-choice-ux-feedback-20261105.md` | 5 条反馈的根因分析 |
| `plan/handoff-next-session-20261105.md` | 下一会话入口（本文应并入其 §4 队列） |
| `plan/level-config-handoff-20261105.md` | 关卡编成与铁律（W8 要用） |
