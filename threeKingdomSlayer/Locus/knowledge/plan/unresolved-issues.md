---
id: kd_fb96a7e6-c460-4e62-b7ef-d0881d692594
injectMode: inherit
summary: 带调试步骤与关键文件的未修复问题 TODO（QTE 无法触发、cardPrefab 空引用、路线计时被动跨节点时序）；教学/引导提示这条线的六项已全部修复并验收（TODO-5/6/7 关闭）；现象层清单见 memory/known-issues.md。
aiEditMode: inherit
---

# 未修复问题 TODO（2025-07-18）

> 开启新对话时可直接将此列表作为任务起点。
> 调试入口统一使用 Play Mode + Console 断点。
> 现象与根因清单见 `memory/known-issues.md`；本文件只保留带调试步骤的待办。

---

## TODO-1 [已验证不复现，2026-10-21] QTE 无法触发 — QTEController 始终 Idle

> **2026-10-21 实测结论：已不复现，本条关闭。**
> 方法：Play Mode 里 `Instantiate(Resources.Load<GameObject>("EnemyPrefabs/Enemy_104"))`，
> 把 `bossState` 置 `InCombat` 后首次调用 `Enemy.SelectBossAction()`，
> 立即得到 `QTEController._state = PerformingQTEAttack`、`attackIndex=0`、`enemy.state = QTEAttacking`。
> 同期静态核对：三个 Boss prefab（104/107/108）均有 `QTEController` 组件，
> `qteData = BossQTEData_104`（2 个攻击），`bossPhases` 各阶段 `qteWeight = 2 / 0.5`（不为 0）。
> 另：原文说的排查方向「检查 OnBossEngaged 是否触发」是**错的** ——
> `QTEController` 从不订阅 `OnBossEngaged`，它的启动链是
> `Enemy.Update`（Idle + `bossState==InCombat`）→ `actionCooldownTimer` 归零 →
> `SelectBossAction()` 加权随机 → `TriggerQTEAttack()`。
> 下方历史排查记录保留，供“同类现象再次出现”时用。

**症状**: Boss 进入战斗后 QTE 反击从不触发，`QTEController._state` 始终为 `Idle`

**已排除**:
- CanvasGroup 阻挡输入（QTE Canvas 无 CanvasGroup）
- QTE 配置缺失（BossQTEData_104 有 2 个攻击，QTEConfig 均有 prefab）
- InputManager 阻挡（skillInputEnabled=true, blockInputFrames=0）
- QTE Canvas 配置错误（ScreenSpaceCamera, worldCamera 已设）
- QTEController.enemy 引用缺失（Awake 中自动补全）

**调试步骤**:
1. Play Mode → 等待 Boss 进入战斗
2. 检查 `QTEController._state` 是否从 Idle 变为 CoolingDown
3. 检查 `QTEController._qtePhaseStarted` 是否为 true
4. 若 _state 仍为 Idle → 检查 `OnBossEngaged` 事件是否触发（在 QTEController.OnBossEngaged 加 Debug.Log）
5. 若 OnBossEngaged 不触发 → 检查 Boss 的 Enemy 组件是否正确触发 InCombat 事件

**关键文件**:
- `Assets/Scripts/QTE/QTEController.cs` — `OnBossEngaged()`, `Update()`, `StartQTE()`
- `Assets/Scripts/Enemy/Enemy.cs` — 搜索 `OnBossEngaged` 事件声明和触发点

---

## TODO-2 [已完成] 虚幻武器触发验证 ✅

Phase 2 已完成：延迟攻击、蓝色伤害数字、per-level 配置均已实现。

---

## TODO-3 [低] cardPrefab 空引用偶现确认

**症状**: 历史曾报 `UnassignedReferenceException: cardPrefab of UpgradeChoicePopup has not been assigned`

**当前状态**: Editor 中 cardPrefab 已正确赋值（UpgradePopup prefab 和 scene 实例均确认）

**调试步骤**:
1. 进入 Play Mode
2. 击杀敌人获取经验 → 触发等级提升 → 弹出三选一
3. 确认是否出现 NullReferenceException
4. 若再现 → 在 `UpgradeChoicePopup.ShowChoices` 中打印 cardPrefab / cardsParent 是否为 null

**关键文件**:

## TODO-4 [高] 场景化路线计时被动跨节点时序

**当前状态**：待持续观测，未判定修复。现象有两类：Head→Combat 期间疑似提前触发；计时器进入冷却但没有可见效果。添加大量日志后问题曾暂时消失，需警惕日志改变 Editor 时序。

**关键日志**：`[TimedPassiveDiag]`、`[RouteDiag]`、`BurnTick`、`DiseaseTick`。

**验收重点**：
- Head→Combat 不应出现 `TimerExpired`、`BurnTick` 或首次计时被动触发；
- `StartRouteBattle` 后应先确认敌人已生成，再尝试首次效果；
- 效果创建失败不得进入冷却；
- 旧节点 DoT、效果对象和协程不得污染新节点。

**关键文件**：
- `Assets/Scripts/Core/TimedPassiveModule.cs`
- `Assets/Scripts/Core/UpgradeEffectManager.cs`
- `Assets/Scripts/Managers/StageController.cs`
- `Assets/Scripts/RouteV2/RouteStageRuntimeV2.cs`

## TODO-5 [已修复 2026-11-02] 常态点击引导：手与光圈对不齐 / 圈过大 / 无敌人处出现光圈

> **2026-11-02 已修复并自测通过，本条关闭。**
> 根因：① 上一轮那批编辑（pivot 改掌心 + 位置补偿）**整批回滚**（`Edit 5 of 5 failed`）→ 实际只改了 pivot、没加补偿 → 指尖偏左上 ≈200px；
> ② `TipUV/PalmUV` 是从 prefab pivot 反推的，不是量图得来的（实测指尖 (0.273,0.801)、掌心 (0.48,0.48)）；
> ③ 光圈用固定设计像素，而两个画布缩放不同 → 正式关里过大；
> ④ unit 持有上一帧的敌人引用 → 敌人死/被回收后光圈停在原地。
> 修复：尺寸改为**相对敌人屏幕高度**（圈 = 敌人高 × 0.3）；坐标常量换成像素实测值 + 位置补偿（指尖→圈心偏差 **14.2px**）；
> 目标存活双重校验 → 敌人清空后 0.6s 内无残留光圈。
> 详见 `plan/tutorial-and-hint-overhaul-plan-20261101.md` §0.14。
> 以下为原调试步骤（保留备查）。

**当前状态**：**已修复**（2026-11-02）。**背景**：教学关改造新增的「常驻点击特效」`AttackTapIndicator`（最前排敌人身上的手 + 光圈）。

**症状**：
1. 正式关卡里引导的**手指与光圈位置对不上**（目视）；光圈**明显偏大**。
2. **没有敌人的位置也出现光圈**。

**已做但无效**：
- 坐标/尺寸抽成唯一设置处 `Assets/Scripts/UI/TutorialGestureStyle.cs`（指尖 UV、掌心 UV、基准尺寸、放大倍数）；
- 两边 RectTransform 的 pivot 都改到掌心；位置补「掌心−指尖」偏移；
- 运行时实测：常态引导 手 616 / 圈 480，指尖到圈中心偏差 **19.5px**（按压弧线）。

**调试步骤**：
1. Play Mode → 正式关（或教学关）→ 看前排敌人身上的手与光圈是否同心，量屏幕像素。
2. **不要再靠 pivot 数学推**：用图片处理量两张 UI 图的实际内容边界
   - `Assets/Sprites/UI/Tutorial/tutorial_gesture_finger_ui.png` → 指尖像素、掌心像素；
   - `Assets/Sprites/UI/Tutorial/tutorial_gesture_hold_ring_ui.png` → 视觉中心是否 = pivot 中心；
   把量出来的值填回 `TutorialGestureStyle.TipUV / PalmUV`。
3. **尺寸改成相对敌人屏幕尺寸**（而不是固定设计像素）→ 消除两个画布缩放不同（教学提示 1080×1920 match 0.5 vs 战斗 HUD 1080×2160）导致的“偏大”。
4. 现象 B：在 `AttackTapIndicator.Refresh()` 打印 `targets.Count` 与每个目标的 `DebugTag / currentHealth / state / rowIndex`；
   击杀前排敌人后立刻看光圈是否留在尸体位置；核对 `EnemyManager.GetAllAliveEnemies()` 是否包含 `state == Dead` 或已被对象池回收的敌人；
   确认 `unit.enemy` 失效（池化回收）时是否立即隐藏该 unit。

**关键文件**：
- `Assets/Scripts/UI/AttackTapIndicator.cs`
- `Assets/Scripts/UI/TutorialGestureStyle.cs`
- `Assets/Scripts/UI/TutorialGestureHint.cs`
- `Assets/Scripts/Enemy/EnemyManager.cs`


## TODO-6 [已修复 2026-11-02] 正式关内**常驻引导**（手 + 光圈）的对位

> **背景**：用户 2026-11-02 第三轮反馈「正式关里引导的光圈和手指还是错位」。
> 其中「教学提示层漏到正式关」这一类**已修**（根因 = `TutorialGestureHint.Hide()` 不停循环协程 → 光圈下一帧又被画回来，
> 见 `plan/tutorial-and-hint-overhaul-plan-20261101.md` §0.15 与 `memory/known-issues.md` 第 11 条）。
> **本条剩的是**：正式关里**常驻引导** `AttackTapIndicator`（前排敌人身上的手 + 光圈）的对位，需要在**正式关内**复测。

> **2026-11-02 已修并实测通过，本条关闭。**
> 根因（实测定位）：`AttackTapIndicator` 的**手是 TapHint 根节点的子物体**（根节点已摆在敌人屏幕点上），
> 但手的 `anchoredPosition` 写成了**画布绝对坐标** → **位置算两遍**，指尖落到 `2×目标点**；
> 敌人离画面中心越远偏得越多（教学关敌人居中 → 被掩盖）。
> 正式关实测：指尖→圈心 247px / 151px；修复后 6 秒 240 次采样**恒为 0.0px**。
> 附带：按压动画绕掌心转 + 缩放会让指尖划出 ≈50px 的弧 → 新增 `TutorialGestureStyle.RotatedTipOffset()`
> 做反向补偿（常驻引导 + 教学手势层都接上）。
> 详见 `plan/tutorial-and-hint-overhaul-plan-20261101.md` §0.15、`memory/known-issues.md` 第 12 条。

**为什么不能拿现有数据当结论**：TODO-5 修完后的 14.2px 偏差是**教学镜头**下量的；
正式关镜头/画布缩放不同（战斗 HUD 1080×2160 vs 教学提示 1080×1920 match 0.5），尺寸与偏移不能直接搬。

**复测步骤**：
1. Play Mode → 落地页进任意**正式关**（不是教学关）→ 站到有前排敌人的位置；
2. 量指尖 → 圈心的屏幕像素偏差（教学关基准：14.2px），并看圈直径是否仍等于敌人屏幕高度 × 0.3；
3. 若偏差明显：对比 `AttackTapIndicator` 与 `TutorialGestureHint` 的画布 `CanvasScaler`（matchWidthOrHeight / referenceResolution），
   以及 `TutorialGestureStyle.HandPositionOffset(size)` 是否按**实际 canvas 缩放**换算过。

**关键文件**：
- `Assets/Scripts/UI/AttackTapIndicator.cs`
- `Assets/Scripts/UI/TutorialGestureStyle.cs`
- `Assets/Scripts/UI/TutorialGestureHint.cs`

---

## TODO-7 [已修复并验收 2026-11-02] 教学关「用技能打断霸体」整段教学不发生

> **2026-11-02 用户验收通过，本条关闭。**
> 现象：玩家选完三选一 → 敌人攻击 → **不冻结、没有「点技能」的手势、玩家只能站着挨打**。
>
> **真凶（三个，都实测取证）**：
> 1. **出口光标清理误杀教学拍**：`TutorialStageDirector.Update()` 里的路线光标清理调了 `StopHint()`，
>    而 `StopHint()` 会 `StopCoroutine(_hintRoutine)` → **把当前教学拍一起停掉**
>    （`_routeHintActive` 从不复位）。用户那次它刚好在拍开拍的同一帧触发 →
>    冻结那段代码根本没执行（日志里「等敌人抡起霸体技」一次都没出现）。
>    → 修：路线光标单独用 `_routeHintRoutine` 管，清理只收它自己。
> 2. **场景里有两个 `FrontItemBar`**：`Instance` 拿到的是**没配槽位**那个 →
>    技能槽查找全查空表 → **手势指引出不来**（诊断日志：`entries=1 图标已注册=0 槽位数=0`）。
>    → 修：`Awake()` 让**有槽位的优先当 Instance** + `ResolveForSkills()`；教学拍逐个 bar 试。
> 3. **成功判据被卡肉误触发**（`IsSuspended` 含 `SuspendSource.HitStop`，任何一次命中都成立）
>    + **等技能槽要等满 60 秒** → 整段教学静默消失。
>    → 修：判据必须先 `skillUsed`；等槽位 2 秒后走「按槽位序号取位置」兜底；定位不到也照样冻结+台词。
>
> **验收数据**：模拟真人（停驱动等冻结）→ 冻结中 ts=0、敌人举刀定格+红框、**手势压在技能槽上（偏差 0.6px）**、
> **槽位图标脉冲高亮 1.000~1.120** → 点技能 → `打断=True` → 收拍 → 出招闸门恢复 ✓。
> 截图 `Library/Locus/Screenshots/locus_game_20261102_075809_274.png`。
>
> **顺带**：这一拍**解锁防御键**（`OnChoiceSelected` 锁的）→ 等待抡招期间玩家能格挡，不白挨打；
> 冻结判据改成「敌人的**下一次攻击**」（不挑 C 技 —— 1012 的普通招/C 技是**序列轮换**的）。
>
> **关键文件**：
> - `Assets/Scripts/Tutorial/TutorialStageDirector.Beats.cs`（技能拍）
> - `Assets/Scripts/Tutorial/TutorialStageDirector.cs`（Update 里的路线光标清理）
> - `Assets/Scripts/UI/FrontItemBar.cs`（Instance 选取 / 技能槽定位）
> - 详见 `plan/tutorial-hint-handoff-20261102.md` §12、§13。
