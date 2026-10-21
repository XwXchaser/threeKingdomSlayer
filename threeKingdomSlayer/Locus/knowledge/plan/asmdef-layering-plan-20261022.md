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
| **1** | `Locus.Data` 扩容（纯数据类型下沉）+ `Locus.Services` 诞生（`DebugLog` / `AudioManager`） | ✅ 本轮完成 |
| 2 | `Locus.Services` 补全：`SaveManager`（需先把 `FakeRouteStageSaveSnapshot` 与 `PropSaveEntry` 下沉到 L0）、`InputManager` 拆成「读输入」与「选目标」 | 待办 |
| 3 | **内核接口化**：把第 3 节的硬引用逐条改成接口 / 事件（每条一个接口 + 装配处注入） | 待办 |
| 4 | `Locus.Combat` 成立（Enemy / Core / Attack / Wave / QTE / Effect / Status / 战斗侧 Player） | 待办 |
| 5 | `Locus.Flow`（流程 + 路线）与 `Locus.UI`（界面）分离 | 待办 |
| 6 | `Locus.Editor` 独立成程序集（当前靠 `Editor` 特殊文件夹） | 待办 |

### 批次 1 做了什么

- 下沉到 `Locus.Data`：`EnemyTypes`（`EnemyState` / `BossState` / `RushMove*` / `AttackStep` …）、
  `FreezePresentationConfig`、`PropData`（`PropType` / `PropData` / `PropUtils`）、`ComboBuffConfig`、
  新增 `BuffType`、`ElementalScope`（从 `PlayerState` / `ElementalBuffController` 里**抽出来**）。
- 新建 `Assets/Scripts/Services/`：`DebugLog`、`AudioManager` + `Locus.Services.asmdef`（引用 `Locus.Data`）。
- 文件迁移一律 `git mv`（GUID 不变），脚本资产 / prefab 的序列化引用不受影响。

**实测效果**（重跑依赖工具）：`Core → Enemy` 22→18 文件、`Enemy → Core` 11→10、
`Enemy/Core → Services` 新增一条**朝下**的边、`Core/Enemy → Data` 明显变多（数据真的下沉了）。

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
