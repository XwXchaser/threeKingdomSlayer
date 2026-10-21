---
id: kd_cacf9532-1997-4a7c-ba8f-1069ff9f47db
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_9f4c2d81-5a7e-4b3c-8d16-2e7f4a9c6b52
injectMode: inherit
summary: asmdef 分层方案：实测依赖基线、目标层次（Data/Services/Combat/Flow/UI/Editor）、必须先切掉的硬引用清单、分批执行顺序与验收口径。
aiEditMode: inherit
---

# asmdef 分层方案（2026-10-22 起）

> 目标：把「架构分层」从**约定**变成**编译期约束**——新机制只能落在正确的层里。
> 判据三条：① 依赖方向单向（编译器可验）；② 每个 asmdef 程序集**自闭合**（asmdef 程序集看不到 `Assembly-CSharp`）；
> ③ 每一刀都能回归（§15.5 基线逐位一致 + Console 0/0）。

## 0. 当前进度（2026-10-22）

| 批次 | 状态 | 成果 |
|---|---|---|
| 1 · `Locus.Data` 扩容 + `Locus.Services` 诞生 | ✅ | 纯数据类型下沉；`DebugLog` / `AudioManager` 进服务层 |
| 2 · 存档下沉 L0 + `SaveManager` 进服务层 | ✅ | **`Locus.Services` 出边只剩 `Locus.Data`** ✅ **`Locus.Data` 出边为空** ✅ |
| 3 · 扩展点契约下沉 L0 | ✅ | 7 个扩展点接口进契约层；`Core→RouteFake/Attack/Effect` 三条边**归零** ✅ |
| 4 · 内核 → UI 的接口化（第一批） | ✅ | 三个呈现层端口（`ICombatHud` / `IDialogueModal` / `IItemDiscardPrompt`）+ 装配点 `CombatPresenters`；`Core→UI` 5→1 文件 ✅ |
| 5 · 工厂端口（内核自己创建 UI 组件那类） | ✅ | `IUpgradeChoiceView` / `IDamageNumberPresenter` / `ISceneTransitionCamera`；`DamageNumberManager` 移到界面层；**`Core→UI` / `Managers→UI` / `Wave→UI` 全部归零** ✅ |
| 6 · 血条工厂端口 | ✅ | `IEnemyHealthBarView` + `IEnemyHealthBarFactory`；**`Enemy→UI` 1→0** ✅ —— 内核（Core/Enemy/Managers/Wave/Player/Status）**一条 UI 引用都不剩** ✅ |
| 7 · QTE 的三个 UI 触点 | ✅ | `IQteHud` + `IQteInputLockTarget` + `QteInputLockTargets`（登记表✅）；`UIResolutionHelper` 下沉 L0（纯计算✅）；**`QTE→UI` 3→0** ✅ —— **战斗内核一条 UI 引用都不剩** ✅ |
| 8 · 内核不再点名流程类 | ✅ | `StageRoles` 装配点（`BattleState` / `Formation`）+ `IPropDestroyNotifier`；17 处 `StageController.Instance` 改走装配点 ✅；`StageFormation`（纯计算✅）/ `ArrowGlobalConfig` 下沉 L0 ✅；内核对 `Managers` 只剩 `EnemyManager` ✅ |
| 9a · 输入端口 | ✅ | `IPlayerInput`（状态 + 事件✅，**不含手势识别**✅）+ `PlayerInputHub`；内核 6 个文件改走端口 ✅；**内核不再引用 `InputManager` 类型** ✅；依据与扩展路径见 `design/input-gesture-pipeline.md` ✅ |
| 9b · **`Locus.Combat` 成立** | ✅ | 战斗内核独立成程序集（662KB ✅）；用 `.asmref` 挂目录，**250 个文件一个没搬** ✅；内核只朝下依赖 `Locus.Data` / `Locus.Services` ✅；**编译即验收** ✅ |
| 10+ · `Locus.Flow` / `Locus.UI` / `Locus.Editor` | ⏳ | 上层（`Assembly-CSharp` ✅）里还有流程 / 界面 / 编辑器工具；可按需再切 `Locus.Flow`（路线 + 流程 ✅）与 `Locus.UI`（界面 ✅）；`Locus.Editor` 独立成程序集 ✅ |

**现在的分层实况**（2026-10-22）：
| 程序集 | 内容 | 依赖 |
|---|---|---|
| `Locus.Data` | 配置 + 契约 + 纯数据类型（61 文件 / 3597 行） | 无 ✅ |
| `Locus.Services` | 全局服务（`DebugLog` / `AudioManager` / `SaveManager`） | Data ✅ |
| `Locus.Combat` | **战斗内核**（Enemy / Core / Attack / Wave / QTE / Effect / Status / MoveSystem / Prop / 战斗侧 Player） | Data + Services + 第三方 ✅ |
| `Assembly-CSharp` | 上层：流程 / 界面 / 输入适配器 / 编辑器工具 | 以上全部 ✅ |

⇒ **内核 → 界面 归零** ✅、**内核 → 流程 归零** ✅、**内核 → 输入类型 归零** ✅
—— 这三条边界现在是**编译器保证**的 ✅（写错层直接编译失败 ✅）。

**内核 → 界面已全部归零** ✅ ✅。内核现在只朝下依赖 `Locus.Data` / `Locus.Services` ✅，
跨目录依赖只剩两处 ✅（这就是 `Locus.Combat` 的最后一道门槛 ✅）：

| 内核 → | 用到的类型 | 归属建议 |
|---|---|---|
| `Player` | `PlayerState` / `AttackSystem` / `IceBreakSession` / `UpgradeAcquired` | **属于内核** ✅（玩家侧战斗状态 ✅）—— 归位到内核目录 ✅ |
| `Player` | ~~`InputManager`~~ | **已解决** ✅：内核改走 `IPlayerInput` 端口 ✅（识别逻辑留在上层 ✅） |
| `Managers` | `EnemyManager` | **属于内核** ✅（敌人注册表 ✅）—— 归位到内核目录 ✅ |

> 顺带一条：内核读流程的 **17 处** 引用**本来就已经在用 L0 接口** ✅（`IStageBattleState` / `IStageFormation` ✅），
> 只是「取实例」点名了流程类 ✅ —— 改成 `StageRoles` 装配点就干净了 ✅。

## 1. 实测基线（不是猜的）

工具：`Tools/asmdef-survey/`（`dep_survey.py` 目录级图 / `dep_detail.py` 跨目录类型明细 / `file_deps.py` 单文件闭合性）。
本轮实测：脚本 **285** 个、目录 **24** 个、`Locus.Data` 45 文件。

**唯一干净的叶子**：`Locus.Data` **没有任何出边** ✓（配置 + 契约 + 纯数据）。

**枢纽类型**（被多少个目录依赖）：`PlayerState` 15、`Enemy` 14、`EnemyState` 11、`StageController` 11、
`DebugLog` 10、`EnemyManager` 10、`UpgradeDefinition` 10、`AttackType` 10、`StageConfig` 10、`StageState` 9 …

**目录级环**（实测存在 dozens 条，摘几条短的）：
`Core → Attack → Core`、`Attack → Effects → Core → Attack`、`Enemy → QTE → UI → Tutorial → Wave → Attack`、
`Enemy → Managers → RouteFake → Prop → Core → Attack`。
⇒ **目录不是程序集边界**：绝大多数目录两两互环，必须按**类型**切开。

## 2. 目标层次

| 层 | 程序集 | 内容 | 允许依赖 |
|---|---|---|---|
| L0 | `Locus.Data` | 配置、契约接口、纯数据类型（枚举 / 结构体 / ScriptableObject） | 无 |
| L1 | `Locus.Services` | 全局服务：日志、音频、存档、输入 | L0 |
| L2 | `Locus.Combat` | 战斗内核：`Enemy` / `ColumnManager` / `AttackSystem` / `Wave` / `QTE` / `Effect` / `Status` | L0 + L1 |
| L3 | `Locus.Flow` | 关卡流程与路线：`StageController` / `RouteFake` / `Tutorial` / 场景装配 | L0~L2 |
| L4 | `Locus.UI` | 界面 | L0~L3 |
| L5 | `Locus.Editor` | 编辑器工具（当前在 `Assembly-CSharp-Editor`） | 全部 |

方向**只能向下**。`Locus.UI` 不得被 `Locus.Combat` 引用。

## 3. 硬骨头：必须先切掉的「内核 → 上层」直接引用

`Locus.Combat` 要成立，下列硬引用必须先改成接口 / 事件，或把被引用类型下沉：

| 来源 | 被引用的上层类型 | 说明 |
|---|---|---|
| `Core` | `BattleHUD` / `DialogueManager` / `ItemDiscardPopup` / `UpgradeChoicePopup` | 内核直接弹 UI |
| `Enemy` | `EnemyHealthBar` | 敌人直接操作血条 UI |
| `QTE` | `HeroHUD` / `BuffDisplayPanel` / `UIResolutionHelper` | QTE 直接摸 HUD |
| `Core` / `Enemy` | `StageController` | 内核回调关卡流程（已有 `IStageBattleState` 可下沉到 L0） |
| `Core` / `Enemy` | `PlayerState` | 内核读玩家状态（`PlayerState` 自身又依赖内核 → 双向） |
| `Core` / `Enemy` | `InputManager` | 输入管理同时做「选目标敌人」→ 是玩法不是服务 |
| `Wave` / `Effect` | `DamageNumber` | 战斗特效直接生成伤害数字 |
| `Managers` | `DamageNumberManager` | 同上 |

**结论**：`Locus.Combat` 这一刀不是「加个 asmdef」，而是**先把这些调用改成接口**（每处一个接口 + 装配处注入），
否则内核程序集无法自闭合。顺序上必须先做接口化，再切程序集。

## 4. 分批执行（每批 = 一次提交 + 一次回归）

| 批次 | 内容 | 状态 |
|---|---|---|
| **1** | `Locus.Data` 扩容（纯数据类型下沉）+ `Locus.Services` 诞生（`DebugLog` / `AudioManager`） | ✅ 完成 |
| **2** | `Locus.Services` 补全：`SaveManager`（含存档数据模型下沉到 L0）+ `BreakableProp.PropSaveEntry` 提成顶层类型 | ✅ 完成 |
| **3** | **扩展点契约下沉 L0**：`IStatusEffect` / `IEffectExecutor` / `IUpgradeStatSink` / `IUpgradeNumericApplier` / `IUpgradeDescriptionFormatter` / `IRouteMotionPresenter` / `IStatModifierApplier` | ✅ 完成 |
| 4 | `Locus.Services` 补全剩下的 `InputManager` —— **实测做不到**：它带**序列化的玩法工具引用**（`AttackSystem` / `WhirlwindController` / `PlayerMoveStateMachine`），而 Unity 序列化不认接口类型的字段；它也带自己的「落雷」技能逻辑。⇒ **它不属于服务层**，而是**内核之上的输入适配器**（内核反过来读它的蓄力状态）。它的归宿是上层的 `Locus.App` / `Locus.Input`，不是 `Locus.Services` | 结论已定，改为「归上层」 |
| 4' | **内核 → UI 的接口化（第一批）**：内核不再直接引用 UI 单例，改经 L0 契约接口 | 进行中 |
| 5 | **内核接口化（第二批）**：剩下的「内核自己创建 UI 组件」类引用——`UpgradeChoicePopup`（内核创建三选一弹窗）、`EnemyHealthBar`（敌人身上挂血条）、`CameraManager` / `DamageNumber` → 要靠**工厂端口**处理（内核只问「给我一个血条视图」，创建动作在界面层做 ✅） | 待办 |
| 6 | **内核接口化（第三批）**：内核读 `InputManager` / `PlayerState` 的地方改成 L0 接口 + 装配处注入 | 待办 |
| 6 | `Locus.Combat` 成立（Enemy / Core / Attack / Wave / QTE / Effect / Status / 战斗侧 Player） | 待办 |
| 7 | `Locus.Flow`（流程 + 路线）与 `Locus.UI`（界面）分离 | 待办 |
| 8 | `Locus.Editor` 独立成程序集（当前靠 `Editor` 特殊文件夹） | 待办 |

### 批次 1 做了什么

- 下沉到 `Locus.Data`：`EnemyTypes`（`EnemyState` / `BossState` / `RushMove*` / `AttackStep` …）、
  `FreezePresentationConfig`、`PropData`（`PropType` / `PropData` / `PropUtils`）、`ComboBuffConfig`、
  新增 `BuffType`、`ElementalScope`（从 `PlayerState` / `ElementalBuffController` 里**抽出来**）。
- 新建 `Assets/Scripts/Services/`：`DebugLog`、`AudioManager` + `Locus.Services.asmdef`（引用 `Locus.Data`）。
- 文件迁移一律 `git mv`（GUID 不变），脚本资产 / prefab 的序列化引用不受影响。

**实测效果**（重跑依赖工具）：`Core → Enemy` 22→18 文件、`Enemy → Core` 11→10、
`Enemy/Core → Services` 新增一条**朝下**的边、`Core/Enemy → Data` 明显变多（数据真的下沉了）。

### 批次 2 做了什么（`SaveManager` 进服务层）

- 存档**数据模型**下沉到 L0：`SaveData` / `RouteStageSaveSnapshot` / `RouteNodeBattleSaveState` /
  `RouteUpgradeSaveState`（从 `Core/SaveManager.cs` 拆出 → `Data/SaveData.cs`）；
  `FakeRouteStageSaveSnapshot` 等 5 个快照类型（`RouteFake/` → `Data/`）。
- `BreakableProp.PropSaveEntry`（嵌套在 MonoBehaviour 里的结构体）**提成顶层类型** → `Data/PropSaveEntry.cs`
  —— 存档条目要塞进快照，嵌套类型就下不去；序列化字段名不变 → **旧存档照读** ✓。
- `Core/SaveManager.cs` → `Services/SaveManager.cs`（服务只管「读 / 写 / 迁移」）。

**实测结果**（重跑依赖工具）：**`Locus.Services` 的出边只有 `Locus.Data`** ✓，
**`Locus.Data` 出边为空** ✓ —— 两层边界现在是**编译器保证**的，不再是口头约定。

**验证**（Play Mode）：存档往返（`ToJson` → `FromJson`）字段逐项一致（天赋 3 / 假路线快照 2 / 道具 1 / 铜钱 2630）✓；
快照里的 `propStates` 正确读出（`N1_Prop_Crate_01 destroyed=True coins=100`）✓；旧档 `coinCount` 迁移正常 ✓；
§15.5 基线逐位一致 ✓；Console 0/0 ✓。

### 批次 3 做了什么（扩展点契约下沉 L0）

搬的是**扩展点接口**——它们就是「为新机制留接口」的载体，理应住在最底层的契约层：

| 接口 | 原位置 | 用途 |
|---|---|---|
| `IStatusEffect` | `Status/` | 状态机制（新状态类住哪一层都行） |
| `IEffectExecutor` | `Core/` | 局内 Build 的行为型效果 |
| `IUpgradeStatSink` / `IUpgradeNumericApplier` / `IUpgradeDescriptionFormatter` | `Core/` | 升级数值 / 文案扩展点 |
| `IStatModifierApplier` | `Core/BuffManager.cs` 内嵌 | 属性修正应用器 |
| `IRouteMotionPresenter` | `RouteFake/` | 路线表现 |

**留在原处**（实测引用了上层类型，下不去）：`IDamageReactionHandler`（带 `Enemy` 参数）、
`IEnemyStatusProvider`（带 `Enemy` 参数）、`IStageRoleContext`（依赖 `StageRouteBattleHost`）。

**实测效果**：`Core → RouteFake` / `Core → Attack` / `Core → Effect` 三条边**归零** ✓；
`Enemy → Status` 4→1 文件 ✓；`Status/` 目录出边只剩 `Data` ✓；`Locus.Data` 仍是纯叶子 ✓。

> 顺带一个判据工具：`Tools/asmdef-survey/list_interfaces.py` —— 列出全工程接口 + 被多少目录使用，
> 用来判断「哪些接口该下沉到 L0」。

### 批次 4 做了什么（内核不再直接引用 UI 单例）

内核要「问界面挡不挡路 / 弹个窗 / 改血条颜色」时，以前直接摸界面单例 ✅
（`DialogueManager.Instance` / `ItemDiscardPopup.Show` / `FindObjectOfType<BattleHUD>()`）——
这是**内核 → 界面**的反向依赖 ✅，也是 `Locus.Combat` 成立的最大障碍 ✅。

改成**契约端口 + 呈现层装配点**：

| 契约（L0） | 谁实现 | 内核原来的用法 |
|---|---|---|
| `ICombatHud` | `BattleHUD`（Awake 登记） | `FindObjectOfType<BattleHUD>()` 改血条颜色 ✅ |
| `IDialogueModal` | `DialogueManager`（Awake 登记） | 问「对话是否挡路」✅ |
| `IItemDiscardPrompt` | `ItemDiscardPromptAdapter`（`RuntimeInitializeOnLoadMethod` 自动登记 ✅） | 弹道具弃置窗 ✅ |

配套两个数据类型下沉：`ItemEntry`（从 `ItemInventory.ItemEntry` 提成顶层 ✅）、
`ItemDiscardResult`（从 `ItemDiscardPopup.Result` 下沉 ✅）—— 否则界面层与内核只能互相看见对方的嵌套类型 ✅。

装配点 `CombatPresenters` 的规则：**上层登记、下层使用** ✅；取不到实现时按「没有界面」安全降级 ✅。

**实测效果**：`Core → UI` 5→1 文件 ✅、`Managers → UI` 3→2 ✅；
剩下的都是「内核自己创建 UI 组件」那类 ✅（`UpgradeChoicePopup` / `EnemyHealthBar` / `CameraManager` / `DamageNumber`）✅，
要靠**工厂端口**处理 ✅（下一批 ✅）。

### 批次 5 做了什么（工厂端口：内核不再自己创建 UI 组件）

上一批端口化的是「问状态 / 弹窗」；这一批处理**内核自己创建 UI 组件**那类 ✅：

| 契约（L0） | 谁实现 | 内核原来的用法 |
|---|---|---|
| `IUpgradeChoiceView` | `UpgradeChoicePopup` | `GetComponent<UpgradeChoicePopup>()` ✅ |
| `IDamageNumberPresenter` | `DamageNumberManager`（**从 `Managers/` 移到 `UI/`** ✅ —— 它管的是跳字对象池 ✅） | `DamageNumberManager.Instance.Spawn(...)` ✅ |
| `ISceneTransitionCamera` | `CameraManager` | `FindObjectOfType<CameraManager>()` ✅ |

`DamageNumberManager` 还接管了「清理残留跳字」（`ClearAll()`）✅ —— 原来由 `WaveSpawner` 直接
`FindObjectsOfType<DamageNumber>()` 扫 ✅，现在收进界面层自己管 ✅。

**实测效果**：`Core → UI` **1→0** ✅、`Managers → UI` **2→0** ✅、`Wave → UI` **1→0** ✅。
内核 → 界面现在只剩两处：`Enemy → EnemyHealthBar`（敌人身上挂血条 ✅，按约定单独一刀 ✅）
与 `QTE → HeroHUD / BuffDisplayPanel / UIResolutionHelper` ✅（下一批 ✅）。

> **Unity 坑**：`FindObjectOfType<T>()` 要求 `T : Object` ✅，**不接受接口** ✅ ——
> 所以「按接口找对象」得扫一遍 `MonoBehaviour` 再按接口筛 ✅（`SceneFlowManager` 就是这么做的 ✅）。

### 批次 6 做了什么（血条工厂端口）

内核 → 界面最后一条硬引用：`Enemy → EnemyHealthBar` ✅ —— 敌人受击时自己
`GetComponent<EnemyHealthBar>()` / `AddComponent<EnemyHealthBar>()` ✅，共享血量组也照做一遍 ✅
（内核在**创建 UI 组件** ✅）。

改成工厂端口 ✅：`IEnemyHealthBarView`（Show / Hide ✅）+ `IEnemyHealthBarFactory`（GetOrCreate ✅）；
`EnemyHealthBar` 实现视图端口 ✅；新增 `EnemyHealthBarFactory`（创建动作在界面层做 ✅）。

**实测效果**：`Enemy → UI` **1→0** ✅ —— 内核（`Core` / `Enemy` / `Managers` / `Wave` / `Player` / `Status`）
**一条 UI 引用都不剩** ✅。

**验证**：受击前血条隐藏 ✅ → 受击后血条出现 ✅、掉血 10 ✅ → 死亡后隐藏 ✅；§15.5 基线逐位一致 ✅；Console 0/0 ✅。
**验证缺口**：Trial 波次没有共享血量组 ✅，`SharedHealthGroup` 里那条「给成员建血条」的路径本轮**没跑到** ✅
（代码结构与敌人那条一致 ✅，但没实测 ✅）。

## 5. 每批验收口径

1. 一次编译通过（`unity_recompile`）。
2. **§15.5 基线逐位一致**：入场完成 `col1/r1 · col1/r3 · col2/r0 · col2/r4 · col3/r5`、
   整排清空后 `col1/r0 · col1/r2 · col2/r3 · col3/r4`。
3. Console **0 error / 0 warn**。
4. 重跑依赖工具：新程序集的**出边不得指向更低的层**（`Locus.Data` 出边必须为空）。

> Play Mode 回归的两个已知坑（沿用）：
> ① 落地页有一段**阻塞对话**会把 `timeScale` 置 0 → 测试脚本要先 `BattleDialogueView.Skip()`；
> ② 不点「开始关卡」时 `Time.timeScale` 也是 0 → 等待循环里要每帧兜底 `Time.timeScale = 1f`。

## 6. 迁移注意（Unity 特有）

- asmdef 程序集**看不到 `Assembly-CSharp`** → 新程序集必须「自闭合」；`Assembly-CSharp` 引用 asmdef 没问题（`autoReferenced: true`）。
- 一个 asmdef 会把**整个子目录树**圈进去 → 目录里若有 Editor 脚本（`using UnityEditor`）必须单独给一个 `includePlatforms: ["Editor"]` 的 asmdef。
- 搬文件用 `git mv`（`.meta` 跟着走，GUID 不变）→ prefab / ScriptableObject 的序列化引用不会断。
- 类型跨程序集后**不能用 `internal`**：要共享就得 `public`（或加 `InternalsVisibleTo`）。
