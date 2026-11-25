---
id: kd_f474a3c2-958b-47bf-bbb5-243fc7d23fea
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 教学专属状态的模式边界（2026-11-25）

> **状态：已实施并验证（2026-11-25）。** Play Mode 实测：无尽关防御键可用、攻击白名单解除、`BattleAutoPlayer` 正常清怪；教学关“未教到防御 → 隐藏”保持。实施结果见 §8。
>
> 本文回答两个问题：
> 1. 哪些状态是「教学专属」，只允许在教学关存在；
> 2. 无尽 / 正式关接管时，怎样保证它们**一个都不残留**。
>
> 已修的第一个症状见 `memory/known-issues.md` §28（无尽攻击手势被 `TutorialAttackGate` 拦下）。
> 本文覆盖同类问题的完整范围，防御键是其中第二个被用户抓到的症状。

## 0. 触发问题

- 无尽关攻击手势全部失效 → 已定位为教学招式白名单残留（§28）。
- 用户指出：**防御键也不该在无尽关被锁**；只有教学关「还没教到防御」时才能锁。
- 实测根因：`TutorialStageDirector.RefreshDefenseButtonAvailability` 只看
  `RouteConfig == tutorialRouteConfig`，不看这条路线**是否已被无尽挂起**
  ⇒ 无尽里 `_defendTaught = false` 时，防御键被当成“教学还没教到”而隐藏。
- 同类判定还有两处（强制三选一 / 防御静音还原），都只看 `RouteConfig` 差异，都不看挂起状态。

## 1. 需求口径（用户确认）

1. **防御键只在教学关、且尚未教到“按住防御键”那一拍时锁住**；教到后本局一直可用。
2. **离开教学关**（正式关 / 无尽关 / 落地页）→ 防御键必须可用；
   与 `_defendTaught`、上次教学进度、当前 `RouteConfig` 都无关。
3. **无尽关从第一帧起**：防御可用、攻击手势可用；不出现教学提示 / 遮罩 / 强制三选一 / 教学时停。
4. **破防锁仍然生效**（`PlayerGuard.IsDefenseLocked` → 防御键压暗、按不了）——
   这是玩法锁，不是教学锁，不在本次整改范围内。
5. 三选一弹窗 / 输入门拦下 / 结算面板期间，按既有硬门
   （`PlayerState.CanAcceptDefendIntent`）忽略按压；这也不属于教学锁。

## 2. 现状审计

| 教学专属状态 | 当前守卫 | 漏洞 |
|---|---|---|
| `TutorialAttackGate` 招式白名单 | `IsOnTutorialRoute()`（已加 `IsSuspended`） | 已修；但启动顺序上仍需无尽接管时显式清一次 |
| `DefenseButton.Available`（隐藏锁） | `RefreshDefenseButtonAvailability` **自己算** `RouteConfig` | **不看挂起** → 无尽里被隐藏 |
| `DefenseButton.Muted`（静音） | `ReleaseTutorialOnlyDefenseMuteIfOffTutorialRoute` **自己算**“配置不同” | **不看挂起** → 无尽里可能残留压暗 / 无效 |
| `UpgradeChoiceManager.forcedChoices` / `suppressChoices` | `ReleaseTutorialOnlyChoicesIfOffTutorialRoute` **自己算**“配置不同” | **不看挂起** → 无尽里可能强制教学池 / 吞掉三选一 |
| `TutorialGestureHint` / `MaskService` / `TutorialSceneDim` | `ReleaseGlobalTutorialState`（OnDisable） | 模式接管时**没有被调用** |
| `InputManager.tutorialGestureTarget` / `allowInputWhileTimeFrozen` | 同上 | 同上 |
| `GameplayInputGate` 教学 owner / `Time.timeScale` 教学冻结 | 同上 | 同上 |
| `_defendTaught` | 只在教学防御拍开始时置 true，**从不重置** | 本会话学过就一直开放（现状；见 §5 待拍板） |

**共同根因**：没有“教学会话是否真的在进行”的单一判定；
每个功能各自复制一份 `RouteConfig` 比较，漏掉“路线已挂起”这一维。

## 3. 方案

### 3.1 单一判定：`IsTutorialSessionActive`

```
当前路线是 FakeRouteRuntime
&& !route.IsSuspended          // 已被无尽挂起 = 不在教学
&& tutorialRouteConfig != null
&& route.RouteConfig == tutorialRouteConfig
```

- 所有“教学专属”判定都改用它（`IsOnTutorialRoute()` 保留为它的别名，避免调用点散落）。
- **路线为 null 时不动**（保持现有“路线未就绪不要误清”的保护）。

### 3.2 防御键

- `RefreshDefenseButtonAvailability`：
  `shouldLock = IsTutorialSessionActive() && !_defendTaught`
- 非教学 → 每帧兜底 `SetAvailable(true)`（现有结构已经会做，只改判定）。
- `Muted` 由 `ReleaseTutorialOnlyDefenseMuteIfOffTutorialRoute` 在
  `!IsTutorialSessionActive()` 时清掉；同时 `SetAvailable(true)` 兜底。
- **不修改** `DefenseButton` 的玩法锁（破防 / 硬门 / 区域识别）。

### 3.3 统一回收入口

`TutorialStageDirector` 新增 public：

```
ReleaseTutorialOnlyStateForModeTakeover()
  ├─ StopHint()                      // 收掉正在跑的教学拍 / 手势提示
  ├─ ReleaseGlobalTutorialState()    // 白名单 / 教学三选一 / 输入 static / 教学时停 / 教学输入门 / 遮罩
  ├─ SetDefenseMuteChoice(false)
  ├─ SetDefenseMuteBeat(false)
  └─ DefenseButton.SetAvailable(true)
```

调用点：
- `EndlessRunController.StartEndlessRun()`：`SuspendForEndless()` 之后立即调用；
- `EndlessRunController.EndRun()`：结束本局时再调用一次（幂等）；
- 剧情正式关不需要额外调用：路线不是教学配置，`IsTutorialSessionActive` 已经为 false。

**分层注意**：`TutorialStageDirector` 在 `Locus.UI`（`Assets/Scripts/Tutorial/Locus.UI.asmref`），
`EndlessRunController` 在 `Locus.Flow`，**Flow 不能引用 UI**。
⇒ 新增 Data 层端口 `Assets/Scripts/Data/TutorialStatePort.cs`（`ITutorialStateGate` + 注册表），
导演在 `Awake` 注册、`OnDestroy` 注销；Flow 只调 `TutorialStatePort.ReleaseForModeTakeover()`。

### 3.4 其他重复判定

- `ReleaseTutorialOnlyChoicesIfOffTutorialRoute` → `!IsTutorialSessionActive()`
- `ReleaseTutorialOnlyDefenseMuteIfOffTutorialRoute` → `!IsTutorialSessionActive()`
- `UpdateRouteTeachingLock` / `UpdateAttackGate` → 已走 `IsOnTutorialRoute()`，自动跟随。
- 保留“`RouteFlowHub.Current == null` 时不动”的保护。

## 4. 验收矩阵

| 场景 | 防御键 | 攻击手势 | 教学提示 / 遮罩 | 三选一 |
|---|---|---|---|---|
| 教学 N1，未教到防御 | **隐藏** | 仅已教招式 | 按教学拍 | 教学强制池 |
| 教学，防御拍中 | **可见可用**（按住） | 按教学拍 | 防御拍提示 | — |
| 教学，教到之后 | 可见可用 | 已教招式 | 按教学拍 | 教学强制池 |
| 教学路线选择期 | 可见（HUD 整块失能，按既有规则） | 被输入门拦 | 路线光标 | — |
| 正式关 | **可见可用** | 全部 | 无 | 正常池 |
| 无尽战斗 | **可见可用** | 全部 | 无 | 正常池 |
| 无尽波末三选一 | 可见（硬门忽略按压） | 硬门拦 | 无 | 正常池 |
| 无尽路线 / 旅行 | 可见（硬门忽略按压） | 输入门拦 | 无 | — |
| 落地页 | HUD 隐藏 | — | 无 | — |
| 破防（所有模式） | **压暗、按不了** | 按破防规则 | — | — |

## 5. 待拍板（当前按现状保留）

1. 教学未教到防御时：**隐藏**（现状）还是可见但灰掉？
   - 现状理由：隐藏时它不会吞掉下方敌人的手势；可见灰掉会吃射线，需要额外处理。
2. `_defendTaught` 是否每次教学开局重置？
   - 现状：本会话学过就一直开放（不重置）；若要求“每局严格没教到不给看”，在 N1 第一波重置即可。
3. 教学路线选择期的 HUD 整块失能是否保留？（现状保留，只允许点路牌 / 暂停。）

## 6. 实施顺序

1. `IsTutorialSessionActive` 单一判定 + 防御键守卫改用它。
2. 静音 / 强制三选一还原改用它。
3. 新增统一回收入口，接进无尽开局 / 结束。
4. Play Mode 验收：无尽（防御 + 攻击 + 三选一）、教学防御拍、正式关、破防。
5. 更新 `memory/known-issues.md` 与 `plan/endless-demo-handoff-20261124.md`。

## 7. 非目标

- 不重做教学拍的冻结 / 提示 / 遮罩机制。
- 不改变破防锁、三选一暂停、输入门的既有规则。
- 不在本批新增/删除教学内容，不改教学节奏。

## 8. 实施结果（2026-11-25）

### 改动

| 文件 | 改动 |
|---|---|
| `Assets/Scripts/Data/TutorialStatePort.cs`（新） | `ITutorialStateGate` + 注册表；Flow 通过它请求回收，不反向引用 UI |
| `Assets/Scripts/Data/IRouteFlow.cs` | 新增 `IsSuspended`（让位给无尽后仍留在 `RouteFlowHub.Current` 里） |
| `Assets/Scripts/RouteFake/FakeRouteRuntime.Entry.cs` | 暴露 `IsSuspended` |
| `Assets/Scripts/Tutorial/TutorialStageDirector.cs` | `IsTutorialSessionActive()` 单一判定；防御键 / 静音 / 三选一 / 开场台词 / 路线光标 / 完成判定全部改用它；新增 `ReleaseTutorialOnlyStateForModeTakeover()` 并注册端口 |
| `Assets/Scripts/Tutorial/TutorialStageDirector.Rules.cs` | `UpdateAttackGate` 改用 `IsTutorialSessionActive()`（之前自己算 RouteConfig，会每帧把白名单重新打开） |
| `Assets/Scripts/Tutorial/TutorialStageDirector.Dialogue.cs` | 教学完成判定排除已挂起路线 |
| `Assets/Scripts/Tutorial/TutorialRouteBlackout.cs` | 已挂起路线不铺教学黑幕 |
| `Assets/Scripts/Managers/EndlessRunController.cs` | 开局 / 结束调用 `TutorialStatePort.ReleaseForModeTakeover()` + 白名单兜底 |

### 验证（Editor Play，实际执行）

- 进入无尽前：`IsTutorialSessionActive=True`、防御键 `Available=False`、白名单 LIMITED（教学状态正确）。
- 无尽开局后：`IsTutorialSessionActive=False`、防御键 `Available=True / Muted=False`、白名单 free、输入门 open、`timeScale=1`。
- 防御键真实指针路径：`OnPointerDown` → `IsDefendingActive=True` / `WeaponStance.SwingIn`；`OnPointerUp` → 退出防御。
- 攻击手势：`SubmitGestureForDebug(Tap)` 返回 true；`BattleAutoPlayer` 8 秒击杀 5 只。
- 编译通过、Console 0 error。

### 未改（按现状保留）

- 教学未教到防御时：**隐藏**（不是可见灰掉）。
- `_defendTaught` 不重置（本会话学过就开放）。
- 破防锁 / 三选一硬门 / 输入门行为不变。
