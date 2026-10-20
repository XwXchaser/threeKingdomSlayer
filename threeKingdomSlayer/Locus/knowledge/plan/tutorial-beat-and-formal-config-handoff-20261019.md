---
id: kd_9dd547f5-1f0b-41cd-a91d-4939c8cef351
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 教学手势拍重构 + 正式关卡配置 交接（2026-10-19 晚）

> 接续阅读：`plan/tutorial-and-formal-stage-handoff-20261019.md`（当天白天的总交接）。
> 本文只记这一轮（当天下午到晚上）改了什么、验到什么程度、还剩什么没定。

## 0. 一句话现状

- **教学手势教学**改成「不暂停 + 敌人冻结 + 只压暗场景 + 玩家真出招收场」，旧的「暂停 + 影子手势 + 事后重放」整条链路已废弃。
- **109 出场只播台词，不再教上挑**（上挑只在 1021 那一波教一次）。
- **教学关战败不重来**，只能回主菜单。
- **正式关卡**已配好 1011 / 1021 / 109 的递进配置（N1→E0→N2→N3，共 22 敌）。
- **主菜单左上角**有临时三按钮：教学关 / 正式关卡（简单）/ 加铜钱 +500。
- ✅ **重击突刺击退已修**（2026-10-20 凌晨）：原「后排有人就推不动」与「默认」击退失效是三个叠加 bug 一起造成的。详见 §13。
- 📌 **主菜单/落地页大改已立项**：`plan/main-menu-hub-refactor-plan-20261020.md`（落地页 = Battle 场景、关卡内容进场景即加载、选关/天赋降级为二级 overlay、含包体实测与分阶段实施计划）。本文件只记本轮关卡/教学关的改动。

---

## 1. 教学手势拍（核心）

### 1.1 旧 → 新

| | 旧 | 新 |
|---|---|---|
| 暂停 | `Time.timeScale=0`，世界全冻 | 不暂停；只把**敌人**冻结（`Enemy.tutorialHold`） |
| 输入 | `gameplayInputEnabled=false`，用 `TutorialGestureHint` 自己轮询原始 Input | 输入不断，`InputManager.tutorialGestureTarget` 只放行这一拍的目标手势 |
| 收场 | 玩家做完手势 → `ReplayGesture` 重播一整招 | 监听 `InputManager.OnAttackExecuted`，玩家**真打出**对应招式就收场 |
| 遮罩 | 全屏 UI 黑幕（盖住角色/长枪） | 只压暗场景（`TutorialSceneDim`），角色/长枪/敌人保持原亮度 |

### 1.2 配置字段（`TutorialGestureStep`）

| 字段 | 作用 |
|---|---|
| `kind` | `Tap / Hold / Swipe / ChargedSwipe / **None**`。`None` = 这一波只播登场台词、不教手势 |
| `lineAtWaveStart` | 一开波就冻结敌人 + 暂停播完台词，然后放行 |
| `triggerHpPercent` | 等目标血量降到比例才开拍。**`lineAtWaveStart` 的步骤在代码里强制按 1 处理**（不等血量） |
| `playSeconds` | 示范一轮的时长。ChargedSwipe 在代码里强制 ≥2.5s（按住 1.2s + 上划 1.1s） |

### 1.3 目标手势映射

| kind | 允许的输入 | 收场招式 |
|---|---|---|
| Tap | 未滑动、未蓄力 | Stab |
| Swipe | 未蓄力横划 | Slash |
| ChargedSwipe | **已蓄力**竖划 | Launch |
| None | 不教，不收场 | — |

不允许的手势：不执行、不吃蓄力段（`TutorialAllowsTapGesture / TutorialAllowsSwipeGesture`）。

### 1.4 当前 `Battle.scene` 的五步

| # | 波次 | kind | lineAtWaveStart | 说明 |
|---|---|---|---|---|
| 0 | N1 wave0 | Tap | 0 | 点按戳 |
| 1 | N1 wave1 | Swipe | 0 | 横划斩 |
| 2 | E0 wave0（1021） | ChargedSwipe | 0 | **唯一一次上挑教学** |
| 3 | E0 wave1（109） | **None** | 1 | 只播「前面那骑将……」，不教手势 |
| 4 | 路线出口(-2) | Tap | 0 | 箭头光标（非门槛、不暂停、不压暗） |

### 1.5 收场/中止

- 玩家出对招 → `OnAttackExecuted` → `EndBeat()`：示范消失、压暗 0.12s 淡出、台词立即关掉、敌人解冻、`tutorialGestureTarget=None`。
- **舞台不再是 InProgress（阵亡/通关）**：等待循环和教学拍循环都会立即中止（`等待期间舞台已结束` / `教学拍期间舞台结束`）。这条是 109 那波「示范盖在战败面板上一直循环」的修复。
- `StopHint()`（换波/关场景）也会 `EndBeat()` 并恢复 `timeScale`。

### 1.6 实测数据

```
点按拍 / 横划拍：dim=True、敌人 held=True、无暂停 → 收场 dim=False
109 波：登场台词 → 「只播登场台词，不教手势」 → 12 秒观察内教学拍启动 0 次
1021 波（修改后）：登场台词结束 → 等待 1.4 秒 → 手势拍开始 wave=1（只一次）
```

---

## 2. 场景压暗（`Assets/Scripts/Tutorial/TutorialSceneDim.cs`）

只压线路场景（背景/地形/植被/墙体），角色与敌人不动：

- SpriteRenderer / MeshRenderer 统一走**材质属性**：优先 `_TutorialDim`（路面专用）、其次 `_Color`、`_BaseColor`；只有材质没有颜色属性时才回落到 `sprite.color`。
  - 关键坑：天空背景 `Skybox Background - editable` 用的是只读材质 `_Color` 的自定义 shader，**不读精灵自身颜色**，早期只设 `sr.color` 导致它永远不暗。
- 路面网格 `CurvedScroll/Y Ground J1 Branches` 原本**没有任何颜色属性**，已给 `YGroundJ1Branches.shader` 加 `_TutorialDim`（默认 1）。
- 天空是程序化 Skybox：压暗时临时换一份材质实例调 `_SkyTint`/`_GroundColor`，`RestoreNow/OnDisable` 还原引用。
- 亮度系数 `TutorialSceneDim.dimBrightness`（默认 0.42），由 `TutorialStageDirector.sceneDimBrightness` 下发，可在 Inspector 调。

实测（Play Mode 截图对比）：整屏平均亮度比 0.78；路面段 103.6 → 55.5（0.54）；角色段基本不变；敌人 `color` 保持 `1,1,1`。

---

## 3. 台词系统改动

- `DialogueEventData.nonBlocking`：不暂停、不压暗、不挡点击（路线指引、教学手势台词都用它）。
- `DialogueManager.QueueEventAndNotify(event, forceReplay)`：跳过「本局已播 / 存档已完成」，且**不写入完成记录**（教学台词每次进教学关都要重讲）。
- `BattleDialogueView`：`Show(..., blockClicks)`、`PointerAdvanceEnabled`（教学门槛期间不让点击把台词点掉）。
- `DialogueManager.OnDestroy` 兜底把 `timeScale` 从 0 恢复，避免回主菜单被冻住。

`tutorial_t0_wave1..4` 四条台词都标了 `nonBlocking=true`；`tutorial_t0_choice` 文案改为「继续前进」并 `nonBlocking`。

---

## 4. 战败规则

- `StageController.blockRestartOnDefeat`：教学关由 `TutorialStageDirector` 在路线就绪时置 true。
- `BattleHUD.ShowDefeat` 在该标记下**隐藏「重来」按钮**（按 `onClick` 绑定的 `OnRestartButton` 识别）；`OnRestartButton` 被误触也会转成回主菜单。
- `MainMenuUI.Start` 强制 `Time.timeScale = 1`，避免上一局的暂停把主菜单冻住。

实测：教学关阵亡 → 面板只剩「返回主菜单」；正式关阵亡 → 两个按钮都在。

---

## 5. 1021 盾兵

- `ShieldGuardEnemy.ClassifySide` 的 `default` 从 `Unspecified` 改为 **`Front`**：未声明方向（`None`/`Random`）一律按正面格挡。
- 实测：正面 30 伤害实扣 20（−10 固定减伤）；反侧 30 全额；`Launch` 仍无条件破盾（破盾后进 `Coward`）。
- 已同步 `design/enemy-1021-shield-guard.md`（§3.2、§4.1 表、读招循环）。

---

## 6. 冰系

- **「重击解冻」= 解冻 + 这一击伤害 ×2**（用户确认「重击解冻就是碎冰重击」，不能拆成两个）。
  - `IceBreakRuntime.ThawDamageMultiplier = 2f`，解冻时无条件乘；`TalentIdBreakDouble` 已删除。
  - 天赋树 `TalentTree_Main` 删掉 `talent_ice_break_double`，现在 **9 个节点**；「重击解冻」说明改为「…并且这一击伤害翻倍」。
- **破冰冲击碰撞伤害 15 → 50**：`StageConfig.collisionDamage` 默认值与全部 23 份关卡配置都改成 50。
- 冰雾掉落权重：`ActiveSkillPoolConfig` 稀有池 `active_ice_mist` 100 → **100000**（与「蛇形喷射」的调试权重同级，稀有池里约五五开）。

---

## 7. 正式关卡配置

新增四份战斗配置（`Assets/RouteData/ChangbanpoScroll/Battles/`），节点已指向它们：

| 节点 | 配置 | 敌人 | 血量/伤害 |
|---|---|---|---|
| N1 | `Formal_N1_Battle` | 1011 ×3 | ×0.70 / ×0.50 |
| E0 | `Formal_E0_Battle` | 1011 + 1021 + 1011 | ×0.75 / ×0.55 |
| J1 / EV | 无战斗 | — | — |
| N2 | `Formal_N2_Battle` | 波1 1011×3；波2 1021 + 1011 + 109 | ×0.85 / ×0.65 |
| N3 | `Formal_N3_Battle` | 波1 1011×5；波2 1021 + 109 + 1011 + 109 + 1021 | ×1.00 / ×0.75 |

- 挂接位置：`Assets/RouteData/ChangbanpoScroll/Nodes/Node_*.asset` → `battleEntries[0].battleConfig`。
- 旧预览配置（`N1_ArtPreview_Battle` / `E0_Battle_Preview` / `SmallBattle`）已不再被节点引用，资产保留。
- 敌人本体数值仍来自 `Assets/Resources/EnemyPrefabs/Enemy_1011|1021|109.prefab`（影响所有关卡，包括教学关）。

---

## 8. 主菜单临时入口（验收用）

`MainMenuUI.showDebugStageSelect`（默认 true）在左上角生成三个按钮：

- **教学关**：无视存档直接进 `Tutorial_Route`，清该路线快照。
- **正式关卡（简单）**：进 `Changbanpo_ScrollRoute`（stageId 1）。
- **加铜钱 +500**：`SaveManager.AddCoins(500)`，用来点天赋。

定稿前取消勾选即可隐藏。

---

## 9. 这轮踩到的坑（务必看）

1. **`unity_recompile` 假成功**：报 `compiled` 但 `Library/ScriptAssemblies/Assembly-CSharp.dll` 时间没变、运行中类型也没有新字段。判据只有两个：DLL mtime 晚于源码 / 用反射确认运行中类型有新成员。
   - 可靠手段：删掉 `Library/ScriptAssemblies/Assembly-CSharp.dll` 与 `Library/Bee/artifacts/*/Assembly-CSharp.dll`，再 `CompilationPipeline.RequestScriptCompilation()`（或直接 `unity_recompile`），等它真的重建。
   - 删除时 Editor 会短暂失去脚本程序集，`unity_execute` 里任何引用项目类型的代码都会编译失败，属正常，重建后恢复。
2. **场景 SerializedObject 改动不一定落盘**：`OpenScene(additive) → 改 → SaveScene → CloseScene` 之后**必须 grep 磁盘上的 `.scene`** 复核。本轮 `gestureSteps[3].triggerHpPercent=1` 就丢过一次，导致 109 那波仍在等 80% 血量、等了 29 秒。
3. **给枚举加新值后要先生效再写场景**：`GestureHintKind.None` 未编译前写 `enumValueIndex=4` 会被忽略（回读仍是 ChargedSwipe）。顺序必须是：加枚举 → 编译 → 改场景 → 回读磁盘。
4. **控制台被 `TimedPassiveDiag` 每帧刷屏**：已给它加 `TimedPassiveModule.logDiagnostics`（默认关）。找回日志时先确认这一项，否则 `ctx.GetConsoleLog` 里全是它。
5. **`Logs/Editor.log` 可能是旧文件**（本轮停在 03:15），别拿它当本轮现场；以 Console 缓冲为准。
6. **类型名**：`ElementalBuffController.cs` 里其实是 `PlayerElementalBuffs` 与 `IceBreakRuntime` 两个静态类，冰系常量在 `IceBreakRuntime`。

---

## 10. 未验证 / 待办

1. ~~**重击突刺击退（进行中，未结论）**~~ **已修（2026-10-20 凌晨）**，见 §13。
2. **真手指触摸路径**：编辑器无法伪造 `UnityEngine.Input`，三拍教学只能用 `ReplayGesture` 验证收场逻辑，实机手感需人工验收。
3. **教学关完整链路**（含路线出口、结算、回主菜单）本轮只分段验过，建议整条复跑。
4. **正式关卡数值/时长**没有做整体回归（22 敌约 4 分钟，看是否合意）。
5. **天赋树删除节点后的老存档**：已解锁 `talent_ice_break_double` 的存档会留一个无节点对应的 id，无副作用。

---

## 11. 验收步骤（建议顺序）

1. 主菜单点「加铜钱 +500」若干次 → 点「教学关」。
2. 教学关：N1 点按拍（手指示范 → 真点一下 → 立刻收场）→ N1 横划拍 → 到 1021 波：上挑教学（按住蓄力 → 上划 → 破盾）→ 109 波：只有台词，不再教手势。
3. 故意阵亡 → 战败面板应只有「返回主菜单」（无「重来」）。
4. 回主菜单 → 点「正式关卡（简单）」→ 走 N1（3×1011）→ E0（含 1021，正面砍不动、要上挑）→ N2（6 敌）→ N3（10 敌）。
5. 天赋树点「重击解冻」「破冰冲击」→ 用冰雾冻住敌人 → 重击解冻看伤害是否 ×2、碰撞伤害是否为 50。

---

## 13. 重击突刺击退修复（2026-10-20 凌晨）

> 这一节属于「接续前晚的修复记录」，与 §10.1 的「重击突刺击退」待办对应。**结论：三个 bug 叠加**，全部已修。

### 13.1 现象（用户最初报告）

> 重击突刺能将敌人击退一格 / 后排第三排有人时，重击突刺无法把敌人击退一格

用户最初只描述"没击退"，但后来又说"可以进入蓄力但无法打出攻击"——两轮描述不同，对应**两条不同的修复路径**。

### 13.2 三个 bug 的诊断顺序

| # | 症状 | 根因 | 修法 |
|---|---|---|---|
| Bug #1 | 推退 `pushAmount` 总比预期大 1 → Rule 1 栈式阻塞 | Battle.scene 里 `DisplacementDebugTool.enablePushWave=true` 默认勾上，每场开始偷偷给 `_pushWaveDistance += 1` | 1) 场景文件改 `false`；2) **`DisplacementDebugTool.cs` 三个 bool 字段默认值改 `false`**，防止 Editor 把场景脏状态又勾回去 |
| Bug #2 | 重击突刺（StabR2 / 蓄力横划）**完全打不出** | `InputManager.cs` line 649-651（用户自己的 pending 改动）加了 `if (gesture == Hold && restrictChargedToVertical) return false;`，把 C2 连段蓄力松手那一帧的 `Hold` 手势直接吞了 | line 651 加 `&& !comboChargeActive` 豁免——combo 链期间不拦，仅独立站桩蓄力还按原意拦截 |
| Bug #3 | StabR2 触发了、`damage=35` 也打了，**但推退完全没尝试**（`pushDist=0`） | `AttackSystem.cs` line 362 在 `TryExecuteAttack` 末尾立刻把 `_currentConfigOverride = null`，但 `StabSweepEffect` 的 hit 回调是异步的（射线穿过敌人才触发），等回调里调 `GetConfig(Stab)` 时已 fall back 到默认 `Zhangfei_Stab`（id=1，pushBackRows=0） | 1) line 362 / 366 的清零改成「**非 Stab 才清**」；2) Stab 的清零移到 `StabSweepEffect` 的 `onComplete` lambda 里（射线完全播完才清，hit 回调期间 override 还在） |

### 13.3 修改清单（全部未提交）

| 文件 | 行数/字段 | 改动 |
|---|---|---|
| `Assets/Scenes/Battle.scene` | `enablePushWave` 字段 | `1 → 0` |
| `Assets/Scripts/DebugTools/DisplacementDebugTool.cs` | line 13/16/20 三个 bool 字段 | 默认值改 `false`（加注释说明 Battle 场景的脏状态写回会让默认值复活） |
| `Assets/Scripts/Player/InputManager.cs` | line 651 早返回条件 | `Hold + restrictChargedToVertical` → `Hold + restrictChargedToVertical + !comboChargeActive` |
| `Assets/Scripts/Player/AttackSystem.cs` | line 362 + 366 | `_currentConfigOverride = null` 改成 `if (attackType != Stab) _currentConfigOverride = null` |
| `Assets/Scripts/Player/AttackSystem.cs` | line 478 StabSweepEffect onComplete lambda 内 | 追加 `_currentConfigOverride = null;`（hit 回调异步完成后再清） |

### 13.4 实测验证（Play Mode）

```
[DisplacementDebugTool.Update]
  enablePushWave=False, pushDistance=1, _pushWaveDistance=0   ← Bug #1 修好

[AttackSystem.TryCall(Stab, 2, charged=true, moveConfig=StabR2)]
  ← Bug #3 修好：override 不再被早清

[StabSweepEffect hit callback → ApplyMovePushBack(StabR2, front)]
  ← Bug #2 修好：combo 链的 Hold 手势能穿透

[PushReturn] step #1(1011) row=0→1    ← 实际推退 1 格
[Enemy] StartMoving: row=1, targetRow=0, isRush=True
[PushReturn] complete #1(1011) origin=(2,0) gen=1 order=1  ← 0.35s 后精确回位（设计行为）
```

结论：正式关卡 col=2，front row 0 + rear row 2 的布局下，触发 StabR2 后：
- 敌人从 row 0 → row 1（推退成功）
- 0.35s 后从 row 1 → row 0（PushReturn 自动回位）

### 13.5 用户验收

正式关卡 N1 → E0 → N2 → N3 任意一段触发 StabR2（按住蓄力 → 竖划），看 Console 应出现：
- `[AttackSystem] 戳击 列X 伤害:35 射程:2`（确认是 StabR2 不是默认 Stab 的 damage=20 射程=1）
- `[Displacement] ApplyPushWave: pushAmount=1`（确认是 1 不是 2）
- `[PushReturn] step ... row=X→X+1`（确认推退 + 回位）

若 pushAmount 还是 2：场景的 `DisplacementDebugTool.enablePushWave` 又被勾回去了，在 Inspector 里取消勾选即可。
若 `damage=20 射程=1`：bug #3 没生效，检查 AttackSystem.cs 是否正确编译。

### 13.6 教训

- **B 静态分析一定要追到运行时**：我在静态层面对 `ApplyPushWave(1, {front at row 0, rear at row 3})` 的测试只是说明**算法本身没问题**，但运行时被三个独立 bug 同时破坏。每一层都要单独验证。
- **异步回调的 timing 是隐性契约**：`_currentConfigOverride = null` 在同步代码段里"立即清"看起来很合理，但 `StabSweepEffect` 的 hit 回调是 DOTween `Sequence.AppendCallback` 在射线播完才触发——同步段的清理会把异步段的依赖清掉。这是 `StabSweepEffect` 唯一有 `onComplete` 回调的攻击路径，其他攻击路径都是同步的都没事。
- **场景文件的「脏状态」会自己复活默认值**：脚本字段默认值改了不代表 Battle.scene 上的实例会跟着改——下次 Editor 加载 Battle.scene 时把脏实例写回磁盘，默认值就被覆盖回去了。修法是**双修**（场景字段 + 脚本默认值），或把工具组件从场景里彻底移走。

---

## 12. 关键文件索引

| 内容 | 路径 |
|---|---|
| 教学导演（手势拍/登场台词/失败规则） | `Assets/Scripts/Tutorial/TutorialStageDirector.cs` |
| 场景压暗 | `Assets/Scripts/Tutorial/TutorialSceneDim.cs` |
| 地面调暗属性 | `Assets/Experiments/CurvedScroll/Shaders/YGroundJ1Branches.shader` |
| 手势示范组件 | `Assets/Scripts/UI/TutorialGestureHint.cs`、`Assets/Prefabs/UI/TutorialGestureHint.prefab` |
| 教学手势过滤 | `Assets/Scripts/Player/InputManager.cs`（`tutorialGestureTarget`、`restrictChargedToVertical`） |
| 敌人冻结 | `Assets/Scripts/Enemy/Enemy.cs`（`tutorialHold`） |
| 1021 盾判定 | `Assets/Scripts/Enemy/ShieldGuardEnemy.cs` |
| 冰系解冻/碰撞 | `Assets/Scripts/Core/ElementalBuffController.cs`（`IceBreakRuntime`）、`Assets/Scripts/Core/ColumnManager.cs` |
| 战败面板 | `Assets/Scripts/UI/BattleHUD.cs`、`Assets/Scripts/Managers/StageController.cs` |
| 主菜单临时入口 | `Assets/Scripts/UI/MainMenuUI.cs` |
| 天赋树 | `Assets/ScriptableObjects/Talents/TalentTree_Main.asset` |
| 主动技能池（冰雾权重） | `Assets/ScriptableObjects/ActiveSkills/ActiveSkillPoolConfig.asset` |
| 正式关卡战斗配置 | `Assets/RouteData/ChangbanpoScroll/Battles/Formal_*.asset` |
| 正式关卡节点 | `Assets/RouteData/ChangbanpoScroll/Nodes/Node_*.asset` |
| 教学关场景配置 | `Assets/Scenes/Battle.scene`（Tutorial Director、InputManager、ChargeIndicator） |

> 以上改动**全部未提交**，当前分支 `route-scroll-movement`。
