---
id: kd_a6b3e958-ee14-46fb-956a-17f03b2ee980
injectMode: inherit
injectAgents:
- '*'
summary: 架构解耦与模块化分析：实测 226 脚本/0 asmdef/36 单例/96 处 Find*，指出六个结构性问题（无程序集边界、隐式全局网、上帝类、三套路线驱动、层间倒置、状态聚合），给出 asmdef 分层/三个会话/事件化/IRouteFlow/Enemy 分批拆/输入门/存档分片的方案与 0-4 批实施顺序，含复测命令与基线。**输入门已落地（§4.6 / §5.4，2026-10-22）。**
aiEditMode: inherit
---

# 架构解耦与模块化分析（2026-10-20）

> **本文只做分析与方案，不含任何落地改动。** 落地按 §5 的批次单独排期。
> 测量基线与复测命令见 §7；与功能计划的关系见 §9。
> 测量时的 HEAD：`1b1c2e5`（`feat/stage-select-talent-overlays`）。
>
> 📌 **已落地**：批次 0 与批次 1 已在 2026-10-20 完成并 push（41 个提交，结束于 `afbd41e`）。
> 现状、验收清单与后续选项见 `plan/architecture-optimization-handoff-20261020.md`；本文保留作为
> 问题分析与方案依据。

---

## 0. 结论先行

玩法设计是清晰的（五大支柱 + 局外/落地页/关卡态三层状态），问题全在**执行层没有边界**：

1. **0 个 `.asmdef`** —— 226 个脚本同处 `Assembly-CSharp`，依赖方向无法被强制。
2. **36 个全局单例 + 96 处 `Find*OfType` + 192 个 `public static` 成员** —— 初始化顺序是隐藏契约。
3. **五个 1000 行以上的上帝类**：`Enemy` 5224、`ColumnManager` 2130、`AttackSystem` 1559、`QTEController` 1466、`UpgradeEffectManager` 1276；`StageController` 虽只 827 行但有 **50 个 public 成员**，是事实上的全局门面。
4. **三套路线驱动并存**（`Route` / `RouteFake` / `RouteV2`），运行期靠互相禁用来保证"同一时刻只有一个在跑"。
5. **层间倒置**：输入开关有 8 处写入、散在 5 个模块；`Core` 里反向依赖 UI/表现；`Core` 本身是杂物间。

最划算的四步（都不改玩法行为）：**拆 `Data` 程序集 → 给 `StageController` 加角色接口 → 路线驱动收敛为一套 → Hub 状态事件化**。

---

## 1. 坐标：目标玩法与运行阶段

### 1.1 五大支柱（权威文档：`design/gameplay-pillars.md`）

| 支柱 | 玩家决策 | 当前成熟度（该文档结论） |
|---|---|---|
| 轻重攻击 | 蓄力分级出哪种招 | 轻重由输入门槛划分；蓄力等级**无伤害收益** |
| 连招 | 招式转移图（枪突三连、终结技） | 两条链已实装；扩展要走"触发键"协议 |
| 技能组合 | 局内 Build（6 被动 / 4 主动上限） | 三条路线可辨识；扩展受 `UpgradeEffectManager` 限制 |
| 敌人机制 | 反制表（骑兵 / 1021 破盾 / 霸体 / QTE） | 1021 破盾是**概率**且与轻重攻击耦合 |
| 路线分支与节点奖励 | 分支取舍 | 奖励只有两层在跑，**分支无玩法差异**，节点奖励数据不存在 |

### 1.2 三层运行状态（本文的分析坐标系）

| 层 | 载体 | 关键状态 |
|---|---|---|
| 局外 | `SaveManager`（PlayerPrefs 单 blob）+ 天赋 | `unlockedTalents` / `clearedStageIds` / 铜钱 |
| 落地页（就绪态） | `Battle.scene` + `FakeRouteRuntime.PrepareForEntry` | `Phase=Ready`、`IsPrepared`、`activeFakeRouteStageId` |
| 关卡态 | `FakeRouteRuntime` 流程 + `StageController` 战斗 | `Phase`、`stageState`、checkpoint |

> 落地页的界面职责（四个 tab = 四个界面）见 `plan/main-menu-hub-refactor-plan-20261020.md` §4.3/§4.4。

---

## 2. 当前代码结构画像（实测）

| 指标 | 数值 |
|---|---|
| 脚本数 | 226 个（`Assets/Scripts` 下 `.cs`） |
| 程序集定义 | **0** |
| 全局单例（`static ... Instance`） | 36 个 |
| `public static` 成员 | 192 个 |
| `FindObjectOfType` / `FindObjects*` / `FindFirst*` | 96 处 |
| `Update` / `FixedUpdate` / `LateUpdate` | 75 个 |
| 事件声明 | `public event` 4 + `public static event` 4；`System.Action` 相关 109 处 |
| `Resources.Load` | 8 处 |
| `SaveManager.Save(data)` 整包写入点 | 4 处 |
| `PlayerPrefs` 出现 | 15 处（几乎都在 `SaveManager`） |

### 2.1 模块体量

| 模块 | 文件 | 行数 |
|---|---|---|
| `Assets/Scripts/Core` | 58 | 12 737 |
| `Assets/Scripts/UI` | 47 | 9 676 |
| `Assets/Scripts/Enemy` | 14 | 8 870 |
| `Assets/Scripts/Effects` | 20 | 5 662 |
| `Assets/Scripts/Player` | 4 | 3 181 |
| `Assets/Scripts/QTE` | 9 | 2 500 |
| `Assets/Scripts/Attack` | 4 | 2 385 |
| `Assets/Scripts/Effect` | 8 | 2 273 |
| `Assets/Scripts/Managers` | 5 | 1 921 |
| `Assets/Scripts/RouteFake` | 12 | 1 262 |
| `Assets/Scripts/Route` | 16 | 1 196 |
| `Assets/Scripts/RouteV2` | 7 | 862 |
| `Assets/Scripts/Tutorial` | 2 | 872 |
| `Assets/Scripts/MoveSystem` | 3 | 729 |
| `Assets/Scripts/Wave` | 1 | 466 |
| `Assets/Scripts/Prop` | 1 | 294 |

### 2.2 单例被引用次数（耦合强度）

| 单例 | 引用 | 单例 | 引用 |
|---|---|---|---|
| `PlayerState` | 157 | `ComboManager` | 17 |
| `StageController` | 125 | `TimedPassiveModule` | 17 |
| `InputManager` | 98 | `DialogueManager` | 16 |
| `UpgradeEffectManager` | 89 | `FakeRouteRuntime` | 12 |
| `EnemyManager` | 80 | `ExpGemManager` | 12 |
| `AttackSystem` | 54 | `SceneFlowManager` | 10 |
| `ActiveSkillInventory` | 47 | `WaveSpawner` | 7 |

### 2.3 超大文件

| 文件 | 行数 | 内部 region / 特征 |
|---|---|---|
| `Assets/Scripts/Enemy/Enemy.cs` | 5224 | 10 个 region：状态切换、状态更新、命中帧、伤害系统、冻结（冰系）、位置与透明度、Boss 分阶段推进、描边系统、BOSS 多阶段、对象池 |
| `Assets/Scripts/Core/ColumnManager.cs` | 2130 | 添加/移除/位移/查询/布局/骑兵槽位 |
| `Assets/Scripts/Player/AttackSystem.cs` | 1559 | 攻击类型实现、工具方法、解锁注册表 |
| `Assets/Scripts/QTE/QTEController.cs` | 1466 | — |
| `Assets/Scripts/Core/UpgradeEffectManager.cs` | 1276 | 局内 Build 的万能执行器（2026-10-22 已拆为 437/359/232/304，并把数值效果与描述文案改成注册表扩展点，见 §5.9） |
| `Assets/Scripts/RouteFake/FakeRouteRuntime.cs` | 909 | 流程 + checkpoint + 恢复 + 通关推进 |
| `Assets/Scripts/Managers/StageController.cs` | 827 | **50 个 public 成员** |

---

## 3. 六个结构性问题

### P1 没有程序集边界，依赖方向只能靠自觉

- 证据：`find Assets -name "*.asmdef"` = 0；226 个脚本同处 `Assembly-CSharp`。
- 后果：任何脚本可以引用任何脚本（含 UI ↔ 战斗内核互相引用）；改一个文件整体重编；**无法阻止耦合长回来**，是其余问题的放大器。
- 附带影响：包体与加载优化缺少"模块级"裁切点（场景/模块无法按程序集剥离）。

### P2 隐式全局网：单例 + 静态字段 + 运行时查找

- 证据：36 单例、192 个 `public static` 成员、96 处 `Find*OfType`。
- 后果：初始化顺序成为隐藏契约；状态的"唯一所有者"不存在。
- **实证（本仓库刚发生的三处 bug 全部同源）**：
  - `StageController.fakeRouteStageConfig` 在落地页自举模式下为 null → 结算 `CompleteFakeRouteStage` 静默 early-return（无 Victory / 无通关铜钱 / 不记已通关），战败重来与重来按钮同样走错分支。
  - `FakeRouteLaunch.StartFromCheckpoint` 是 static 旗标，进程冷启动默认 false → 落地页不接 checkpoint、丢血量与升级。
  - `InputManager.gameplayInputEnabled` 由 5 个模块共写，出现"点不动"时无法定位是谁关的。
- 这类字段不是"代码风格问题"，而是**隐式全局状态**：值决定了另一条流程走哪个分支，编译器与调用方都看不见。

### P3 上帝类把可分离的机制焊在一个生命周期里

- `Enemy`（5224）同时是状态机、命中帧判定、伤害结算、冻结、透明度/位置、Boss 阶段、描边、对象池。**冰系、描边、Boss 阶段已经是独立机制，却与核心循环共享一个类与一份生命周期。**
- `StageController`（50 public 成员）是关卡数据宿主 + 战斗编排 + 奖励门控 + 场景重载 + 结算 + 路线协调的合体，125 处引用都在依赖这个合体。
- `UpgradeEffectManager`（89 引用）是局内 Build 的唯一执行入口；支柱三要继续加触发键，会直接撞在它身上。
- `ColumnManager`（2130）把"阵型/位移/补齐/骑兵槽位/波次布局"混在一起，而 `design/immutable-constraints.md` 的位移三原则恰恰要求这块语义最稳定。

### P4 三套路线驱动并存，用运行期暴力维持唯一所有权

- 三套：`Assets/Scripts/Route`（16 文件）、`Assets/Scripts/RouteFake`（12）、`Assets/Scripts/RouteV2`（7）。
- `FakeRouteRuntime.AcquireExclusiveDriving()` 每次都会 `StopAllCoroutines()` + `enabled=false` 去关掉场景里的 `RouteStageRuntimeV2`；`Battle.scene` 里确实挂着一个 `RouteStageRuntimeV2`。
- **两条启动入口已是死路径**：全工程没有任何地方写入 `FakeRouteLaunch.PendingConfig` / `RouteStageV2Launch.PendingConfig`，只有 `StageController.Awake` 把它们读出来再置 null。旧 MainMenu 已删除（D4）。
- 后果：读代码时必须先判断"现在到底哪套在跑"；文档、编译、静态检查都在为死代码付费。

### P5 层间倒置：表现与输入被多方直接驱动

- 输入：`InputManager.gameplayInputEnabled` **8 处写入**，分布在 `FakeRouteRuntime`、`RouteStageRuntimeV2`、`BattleHUD`(2)、`TutorialGestureHint`(3)、`BattleYRouteHost`。
- 表现：13 个 `Core`/`Enemy`/`Player`/`Wave` 文件引用 `BattleHUD` / `DamageNumberManager` / `DialogueManager`；`ScrollRoutePresenter`（场景表现）住在 `Core` 里。
- `Core` 实际是杂物间：数据（`StageConfig`）、战斗规则（`ColumnManager`）、Build（`UpgradeEffectManager`、`TimedPassiveModule`）、表现（`ScrollRoutePresenter`、`PixelHitEffectManager`、`CameraFeedbackController`）、宿主（`BattleYRouteHost`）、存档（`SaveManager`）全在一层。
- 后果：无法做"只替换表现层"的试验（例如换一套打击反馈），也无法做"只替换规则层"的验证。

### P6 状态与配置的聚合点太少

- `SaveManager` 静态类 + 单一 `SaveData` blob：所有子系统共用一份 schema，迁移挤在一个 `MigrateIfNeeded()`；写入是整包重写（4 处 `Save(data)`）。
- `PlayerState` 一个单例承担血量 / 复活 / 等级 / 经验 / 铜钱 / 击杀 / 关卡状态 7 类状态（157 引用）。
- `StageConfig` 30 个字段 + `StageConfigManager` 同时承载"可选路线（`tutorialStage` + `fakeRouteStages`）"与"线性关卡（`stages`）"两种语义。
- 支柱五要落地（节点奖励 / 分支预览）时，`FakeRouteNodeConfig` 只承载对话/战斗/出口，**奖励与预览的数据结构尚不存在**；此时若直接加，会继续塞进 `FakeRouteRuntime`（已 909 行）。

---

## 4. 目标架构

### 4.1 程序集分层（依赖单向）

```
Locus.App      落地页 / 界面导航 / Hub 状态      ──► Locus.UI, Locus.Route
Locus.UI       HUD / 四个界面 / Display          ──► Locus.Combat, Locus.Route
Locus.Route    路线流程 / 节点 / 奖励门控        ──► Locus.Combat
Locus.Combat   战斗规则 / 敌人 / 攻击 / Build    ──► Locus.Data
Locus.Data     配置 SO / 存档 section / 枚举 / 事件契约   （叶子，不依赖上层）
Locus.Editor   工具窗口 / 导入器                 ──► 全部
```

**唯一硬规则：箭头不许反。** 契约（事件、接口、配置、存档 section 接口）放在 `Locus.Data`，下层不需要认识上层。

> **2026-10-22 进展**：`Locus.Data` 已补成完整的叶子层（**38 文件 / 2732 行**）——
> 配置（`StageConfig` / `HeroConfig` / `FakeRoute*` 路线配置 / `DialogueEventData` / `UltimateSkillConfig` …）
> 与契约（`StageState` + 5 个角色接口 + `IRouteStageHost` 合成端口）全在里面。
> 上层程序集（`Locus.Combat` / `Locus.Route` / `Locus.UI` / `Locus.App`）将来可以直接引用它们，
> 不必反向依赖 `Assembly-CSharp`。
> **剩下的卡点**：`Enemy` / `ColumnManager` / `AttackSystem` 等战斗内核自身还挂在 `Assembly-CSharp`，
> 且彼此依赖 `SaveManager` / `AudioManager` / `PlayerState` / `InputManager` 等全局服务；
> 要把它们拉进 `Locus.Combat`，得先给这些全局服务定程序集边界（体量远大于本轮）。

落地顺序建议：**先只拆 `Locus.Data`**——它是叶子，几乎没有调用方需要改；拿到边界后，再逐层把 `Combat` / `Route` / `App` 拉出去。

### 4.2 三个"会话"边界替代全局门面

| 会话 | 拥有 | 生命周期 | 对外暴露 |
|---|---|---|---|
| `AppSession`（落地页/界面） | 当前关卡、四个界面的导航、Hub 可见性 | 进程级 | `ShowInterface(kind)`、`StateChanged` |
| `RunSession`（一局） | route 阶段、节点推进、checkpoint、奖励门控 | 一次运行 | `Begin(runConfig)`、`Phase`、`PhaseChanged` |
| `CombatDomain`（一场战斗） | 敌人培育 / 列阵 / 波次 / 伤害结算 | 节点内 | `StartBattle(stageConfig)`、`BattleCleared` |

`StageController` 的 50 个 public 成员按这四类拆成接口：`IRunFlow` / `ICombatStaging` / `IRewardGate` / `ISceneHost`。

**落地图（2026-10-22，批次 1 尾项完成）**：

| 角色 | 接口 | 消费者 |
|---|---|---|
| 一局流程 | `IRunFlow` | `BattleHUD` / `PauseMenuUI` / `TutorialStageDirector` |
| 一场战斗的布场与开打 | `ICombatStaging` | 路线层（经 `IRouteStageHost`） |
| 奖励门控与结算 | `IRewardGate` | 路线层（经 `IRouteStageHost`） |
| 阵型只读 | `IStageFormation` | 12 个（战斗侧） |
| 战斗状态只读 + 事件 | `IStageBattleState` | 13 个（战斗侧 / UI） |
| 路线层合成端口 | `IRouteStageHost : IRunFlow, ICombatStaging, IRewardGate` | `FakeRouteRuntime` / Legacy `RouteStageRuntimeV2` |

> 手法：先"只加接口、实现仍由 `StageController` 承担、行为一行不改"，让调用方逐步改成依赖接口。这一步零行为风险，但立刻让"谁在依赖什么"变成可读信息。
> 已退役两个重叠的旧端口：`IStageFlowControl`（并入 `IRunFlow`）与 `IRouteBattleHost`（拆成上面三个窄角色，合成端口只做拼装）。
>
> **实现也搬出去了（2026-10-22 第二步）**：三个角色各有自己的实现类
> （`StageRunFlow` / `StageCombatStaging` / `StageRewardGate`），通过 `IStageRoleContext` 拿环境；
> `StageController` 退成薄宿主（Inspector 引用 + 状态 + 事件广播 + 协程宿主 + 薄转发）。

### 4.3 事件化：先把 Hub 的三处轮询合成一个状态源

现状：`BattleStartStageButton`、`BattleHubTabBar`、`HubHudModeSwitch` 三个组件各自每帧读 `IsPrepared` + `HasOverlay`（对应 75 个 `Update` 里的 3 个）。

目标：

```
HubStateSource（唯一计算 isPrepared / interfaceKind / overlayOpen）
        └─► 事件：HubStateChanged  ──► 各 UI 订阅（不再轮询）
```

收益不只是性能：**"谁改了状态"变得可追踪**。现在的显示异常只能去三个组件里逐个猜。

### 4.4 路线驱动收敛为一套

```csharp
public interface IRouteFlow
{
    FakeRoutePhase Phase { get; }
    void Prepare(FakeRouteStageConfig config);  // 落地页
    void Run();                                 // 开始关卡
    void Abort();
    event System.Action<FakeRoutePhase> PhaseChanged;
}
```

- `FakeRouteRuntime` 作为唯一实现（现有 `PrepareForEntry` / `RunPrepared` 就是这两段）。
- `RouteV2` 与 `Route` 原型移入 `Assets/Scripts/Legacy/`（**不删代码**，先移出编译与场景）。
- 删除 `Battle.scene` 上那个靠 `AcquireExclusiveDriving` 关掉的 `RouteStageRuntimeV2` 组件，以及 `AcquireExclusiveDriving` 本身的"关别人"逻辑。
- 删除两条死启动入口（`PendingConfig` 常量 + 读取分支）。

> 这一步直接消掉一整类调试成本："到底谁在驱动这一局"。

### 4.5 敌人：按"机制独立性"分批拆

| 批次 | 抽出 | 理由 |
|---|---|---|
| 1 | `EnemyOutline`（描边）、`EnemyFreeze`（冰系） | 引用面小、与核心循环耦合最低、可独立验收 |
| 2 | `EnemyBossPhases`（阶段推进 + 多阶段攻击） | Boss 专属，普通敌人不需要 |
| 3 | `EnemyHealth`（伤害 / 护盾 / 共享血量 / 打断判定） | 与 `SharedHealthGroup`、`poiseDamage`、`canInterruptCFrame` 契约强相关，需先定契约 |
| 4 | 余下 = `EnemyCore`（状态机 + 命中帧） | 最后才动核心 |

> 不要一次拆成 10 个文件：`伤害 / 打断 / 共享血量` 的契约还没抽出来，硬拆会造出互相反向引用的碎片。

### 4.6 输入：从布尔旗标到"输入意图门" ✅ 已落地

```csharp
enum InputBlockReason { None, RouteTravel, ResultPanel, TutorialGate, LegacyRoute, Other }
GameplayInputGate.RequestBlock(owner, reason);   // 每个 owner 最多持有一份
GameplayInputGate.Release(owner);
bool GameplayInputGate.IsBlocked { get; }        // 可查询
bool GameplayInputGate.IsBlockedBy(reason);      // 可按原因查
string GameplayInputGate.Describe();             // 谁在拦、因为什么拦
```

**落地形态（2026-10-22，批次 3 尾项）**：`Assets/Scripts/Data/GameplayInputGate.cs`（在 `Locus.Data` 程序集，
未来拆 `Locus.Combat` / `Route` / `UI` 时它仍是下层契约）。
全工程**任一 owner 持有拦截 → 输入被拦**；owner 被销毁时拦截自动失效（不会把输入永久锁死）。

- `InputManager.gameplayInputEnabled` 变成**只读属性**（`=> !GameplayInputGate.IsBlocked`）——
  编译器从此禁止任何模块直接写它，回归成本为零。
- 9 个写入点全部迁到门：路线层（`FakeRouteRuntime`）、结算面板（`BattleHUD`）、
  教学门槛（`TutorialGestureHint`）、Legacy 两套路线驱动、`Experiments` 两个试玩脚本。
- **唯一有意的行为差异**：教学门槛期间路线层的"解锁"不再踩掉教学门槛的拦截（旧行为是 bug）。

> 旧实现是"多个模块轮流写同一个全局布尔，后写者覆盖前写者"：
> `BattleHUD` 在胜利/战败写 `false` 后**从不自己撤销**，靠路线层在下一场战斗写 `true` 时顺带覆盖掉；
> 教学门槛的 `true` 同样会踩掉别人的拦截。这正是 P2 点名的隐式全局状态。

### 4.7 存档与配置

- **`ISaveSection`**：按模块分片（`TalentSection` / `RunSnapshotSection` / `TutorialSection` / `PropSection`），`SaveManager` 只负责读写 section，迁移逻辑跟着 section 走。
- `anti-ghost-reference.md` 违规项回归：`Resources.Load` 8 处（`EnemyPool`、`StageRegistry`、`ItemDiscardPopup`、`WeaponMotionBlurController`）改回 Inspector 引用。**这也是包体分析能生效的前提**：`EnemyPool` 按名字加载，导致依赖分析剪不动 prefab 包裹的贴图。
- 配置语义拆分：`StageConfigManager` 的"可选路线"与"线性关卡"分开；`StageConfig` 的 30 个字段按"波次 / 阵型 / 奖励 / 门控"分组并允许组合成子资产。
- **节点奖励数据**（支柱五前置）：`FakeRouteNodeConfig` 增加"奖励候选 / 预览摘要"，或独立 `NodeRewardTable` SO，避免继续膨胀 `FakeRouteRuntime`。

---

## 5. 改造批次与验收口径

| 批次 | 内容 | 风险 | 验收 |
|---|---|---|---|
| **0** | 拆 `Locus.Data` asmdef；删两条死启动入口与 `RouteStageRuntimeV2` 场景组件；Hub 三处轮询合并为一个状态源 | 低 | 编译通过；落地页一键脚本（Ready→选关→天赋→战斗→开打）；`Find*` 计数下降 |
| **1** | `StageController` 加四个角色接口（不改行为）；`Enemy` 抽出描边 + 冻结 | 低-中 | 战斗冒烟：骑兵冲锋 / 1021 破盾 / Boss 分阶段各一场 |
| **2** | 路线统一为 `IRouteFlow`；`Route`/`RouteV2` 移入 `Legacy/` | 中 | 节点全链路 N1→E0→J1→EV→N2→N3 + checkpoint 冷启动 |
| **3** | `EnemyBossPhases` + `EnemyHealth`；输入门替换 `gameplayInputEnabled` | 中 | 打断 / 霸体 / QTE / 盾兵回归清单 |
| **4** | 存档分片 + 节点奖励数据层 | 中 | 旧档迁移样本（迁移前后逐字段比对） |

**为什么是这个顺序**：先做"能自动阻止耦合回流"的（asmdef），再做"只改名不改行为"的（接口），最后才动热点（Enemy / 战斗）。反过来做，会在没有边界的情况下反复把耦合长回去。

### 5.1 批次 0 进展（2026-10-20）

| 子项 | 状态 | 提交 |
|---|---|---|
| 删两条死启动入口（实际是四条 Pending* 分支） | ✅ 代码已提交 | `f139212` |
| Hub 三处轮询合并为 `HubStateSource` | ✅ 代码已提交 + Play Mode 验证 | `31f0332` |
| `Locus.Data` asmdef | ✅ 代码已提交 + 编译/资产引用验证 | 本次提交 |
| Play Mode 回归 | ✅ 已补跑（4 个界面态 + 开打 + 强载恢复） | — |

**死路径清理的实际发现**：`StageController.Awake` 里四条 Pending* 分支（MainMenu 传 FakeRoute / 传 RouteV2 / 线性关卡 / 旧路线）**全部**无人写入，不只是文档里写的两条；两个 `public static` 字段也仅被自己置 null。已全部删除，Hub 自举成为唯一入口。

**Hub 事件化后的形态**：`HubStateSource`（挂在 `Battle.scene / BattleHUD(Canvas)`）是 `IsPrepared` 与 `Overlay` 的唯一计算源，变化时抛 `Changed`，首帧强制广播一次。三个组件改为 `OnEnable` 订阅 + `TrySubscribe()`（源可能晚 Awake，`Start` 再试一次），各自的 `Update` 与私有 `IsOverlayOpen()` 已删（`grep "void Update()"` 三文件均为空）。

**首次只能圈 7 个文件（不是先前估的 13 个）**：`ExpCurveConfig` / `FormationConfig` / `GlobalKillDisplayConfig` / `KillMilestoneConfig` / `RowFormation` / `StageConfig` / `StatModifier`。

**根因不是「配置引用了 PlayerState / UI」**（那是错误结论：`PlayerState` / `BuffDisplayPanel` / `Enemy` 在 `UpgradeDefinition` / `AttackSkillConfig` 里只出现在**注释与 Tooltip 文本**中，被朴素的 grep 当成了依赖）。实际根因是**三个纯枚举被埋在运行时大类里**：

| 枚举 | 原声明位置 | 被谁引用 |
|---|---|---|
| `AttackType` | `PlayerState.cs` 末尾 | `MoveGesture` / `UpgradeDefinition` / `AttackSkillConfig` 等配置 |
| `DamageType` | `Enemy.cs` 末尾（5224 行） | 几乎全部战斗代码 |
| `HitFeedbackSource` / `HitFeedbackStrength` | `HitFeedbackManager.cs` 顶部（与持有 `Enemy` 的 `HitFeedbackContext` 同居） | `AttackSkillConfig` 的反馈字段 |

把这些枚举提到 `Assets/Scripts/Data/CombatEnums.cs` 与 `HitFeedbackEnums.cs`（值不变，序列化按 int），并把本来就行干净的 `HitReactionDirection` / `MoveGesture` 一并搬入后，**闭包从 7 个扩到 19 个文件 / 1875 行**，`UpgradeDefinition`（581 行）也在其中。

→ **教训（已写进 `memory/tool-result-verification.md`）**：依赖分析必须先剥注释、字符串与 XML 文档注释；同时第一版闭包算法还有“把集合内文件误算成外部依赖”的 bug，两个错误叠在一起才会得出“配置反向依赖 PlayerState”这种错结论。

→ **批次 1 的实际形态**：先搬枚举（零行为风险），配置自然解封——**不需要**先做 PlayerState 注入化改造。后者仍值得做，但它不再是数据分层的前置条件。

**验证方式（本轮补足了 Play Mode）**：

| 项 | 实测 |
|---|---|
| 程序集 | `Locus.Data` 含 69 个类型（业务类型 60 个）；`Assembly-CSharp` 无同名残留且引用 `Locus.Data` |
| 资产引用 | `Stage_1.asset` / `Stage_Trial.asset` / `N1_Battle.asset` 均解析为 `StageConfig [Locus.Data]`（GUID 未变） |
| 界面态 A/B/C/D | 战斗 `overlay=null`、开始按钮与 tab 均 1.00；选关/天赋 `overlay` 正确、头像隐藏、开始按钮 0.00、tab 仍 1.00；返回后全部还原 |
| 开打 | 点开始关卡 → `phase=Battle`、`IsPrepared=false` → tab 与开始按钮同时归零、出怪 1 只 |
| 强载恢复 | overlay 开着时 `LoadScene("Battle")` → 栈被清空、`overlay=null`、hub 全部可见 |
| Console | warn/error = 0 |

**测试中发现并修掉的新缺陷**：`SceneFlowManager` 是 `DontDestroyOnLoad`，主场景单模重载后 `_overlayStack` 仍留旧条目 → `HasOverlay` 永真 → hub 内容永久隐藏且再点 tab 被「已在栈中」拦掉。已在 `sceneLoaded(Single)` 时清栈。

**开工前置已完成**：两条未提交流已分组落地（见 §5.2），工作区脏文件 92 → 0。

### 5.2 存量未提交流的落地分组（2026-10-20）

| 提交 | 内容 |
|---|---|
| `39ac81b` feat(tutorial) | 教学关导演重写 + 手势门槛/非阻断台词 + 招式解锁与冰系天赋（成组，含 `Enemy.tutorialHold`、`StageConfig.collisionDamage` 等编译依赖） |
| `e036ad2` feat(prop) | 可破坏道具；**同时修好 HEAD 缺失类型**：`AttackSystem.cs:322` 早已引用 `BreakableProp`，但 `Assets/Scripts/Prop/` 一直 untracked，clean clone 编译不过 |
| `99ba074` chore(balance) | 技能/关卡/敌人数值与 `TimedPassiveModule.logDiagnostics` |
| `65e1957` chore(scroll) | 卷轴试玩数据与 J1 地面着色 |
| `4eec32b` fix(combat) | 1021 盾侧/反侧/正面判定按实际来向 |
| `36b1a12` fix(talent) | 天赋页关闭按钮 raycast + 置顶 |
| `05ae55f` chore(locus) | 工作区工具配置 |

### 5.3 批次 1 进展（2026-10-20 夜，`8a3586c`）

| 子项 | 状态 |
|---|---|
| 提取纯枚举（AttackType / DamageType / HitFeedbackSource / HitFeedbackStrength / HitReactionDirection / MoveGesture） | ✅ |
| 8 个配置搬入 `Locus.Data`（含 `UpgradeDefinition` 581 行） | ✅ |
| 路线层端口接口 `IRouteBattleHost`（依赖倒置第一层） | ✅ `b2da9d2` |
| `Enemy` 拆 `EnemyOutline`（描边） | ✅ `eac2922`（5212 → 5132 行） |
| `Enemy` 拆 `EnemyFreezeVisual`（冰系视觉层） | ✅ `86c3fbd`（5145 → 4798 行，冰系区段 685 → 333 行） |
| `Enemy` 拆 `EnemyFreeze`（冰系状态与流程） | ✅ `34b6bc5`（4798 → 4496 行） |
| 阵型/补齐/位移机制地图 + `ColumnManager` 拆分方案（批次 2 前置） | ✅ 见 `design/formation-fillup-mechanism-20261020.md` |
| `PerColumn` 退役：步骤 1 默认值反转 + 导入期报错 | ✅ `5769fb0` |
| `PerColumn` 退役：步骤 2 清理 119 个无引用文件 | ✅ `dc23b6f` |
| 补齐豁免扩展点（`Enemy.ignoreFillUpOrders`） | ✅ `b3c382b`（A/B 实测通过） |
| `StageController` 按角色拆 `StageFormation` / `StageRouteBattleHost` | ✅ `f906501`（852 → 790 行） |
| `ColumnManager` 拆分（批次 2）：刀 1 `LogicalRowQueue` / 刀 2 `ColumnQueries` / 刀 3 `SpawnEntryMover` / 刀 4 `WaveMarchScheduler` / 刀 5 `DisplacementMover` / 刀 6 退役 `FillUpRule` | ✅ **批次 2 全部完成**（ColumnManager 2149 → **662 行**，−1487）：门面只剩列/骑兵槽位与薄转发；做法与验收见 `plan/columnmanager-split-plan-20261020.md` §0.8/§0.9 |
| 遗留路线实现收进 `Assets/Scripts/Legacy/`，Battle 场景去掉 V2 竞争驱动 | ✅ `55c216a` |
| Legacy 彻底隔离（现役代码不再引用遗留类型）+ 说明文档 | ✅ `a38ee66` |
| 剩余角色接口（`IRunFlow` / `ICombatStaging` / `IRewardGate`） | ✅ **已完成**（2026-10-22，`ee8d5a9`）：三个窄角色 + `IRouteStageHost` 合成端口；`IStageFlowControl` / `IRouteBattleHost` 同步退役。见 §5.5 |
| `IStageFormation`（阵型只读）+ `IStageFlowControl`（流程控制） | ✅ `Enemy` / `BattleHUD` 已迁移 |
| `AttackSystem` 迁移到 `IStageFormation` | ✅ `a0dccc9`（32 处调用收敛为一处） |
| 18 个消费者批量迁移（阵型 / 战斗状态 / 流程控制三角色） | ✅ `853d022`（门面引用 99 → 35 行） |
| A3：清掉最后几处门面**成员调用** | ✅ `d705596`（只剩惰性解析点） |
| `Enemy` 拆 `EnemyFreeze`（冰系，约 685 行） | ✅ `34b6bc5`（连视觉层 `EnemyFreezeVisual` 共拆两刀） |
| **ColumnManager 拆分操作手册**（写给后续接手的 AI） | ✅ `plan/columnmanager-split-plan-20261020.md`（刀 1 已完成 `facdbde`） |

**三处拆分后的验收证据**：

- 端口接口：`StageController is IRouteBattleHost`=true（显式实现三个原本是 public 字段的成员，
  字段与 Inspector 序列化未动）；路线层 `_battleHost` 已解析、`autoStartStage` 被置 false、
  `RouteConfig=教学关·初见`、完成事件订阅者 1、点开始关卡后 `phase=Battle` 出怪 1；
  `grep` 确认 `FakeRouteRuntime` 已无 `stageController.<成员>` 调用。
- 描边拆分：真实敌人 `Enemy_1011` 上 `_OutlineEnabled` 初始 0.00 → 穿刺高亮 0.71（白/宽 3）
  → 调试强制霸体 1.00（橙色/宽 8）→ 关高亮 0.00。序列化字段未搬动，prefab 无变更。
- 角色接口拆分：`StageController` 现实现 `IRouteBattleHost` / `IStageFormation` / `IStageFlowControl`
  三个角色；`Enemy`（14 处）与 `BattleHUD`（6 处）已全部改为走窄接口，两个文件里
  `StageController.Instance` 只剩延迟解析那一行；全项目门面直接引用从 125 降到 99。
  实测：敌人实际 x 与 `GetFormationOffset` 期望值差 0.00，阵型未错位；Console 0/0。
- `AttackSystem` 迁移：32 处阵型调用改为一份只读契约，接口与门面 `ReferenceEquals=true`
  （同一实例，行为零变化）；实跑 `TryExecuteAttack`：Stab 掉血 20、Slash 掉血 20、Launch 掉血 2。
  全项目 `StageController.Instance` 匹配行数 125 → 78。
- 18 个消费者批量迁移：新增 `IStageBattleState`（战斗状态只读 + 状态事件），
  `StageController` 现实现四个角色接口；阵型类 8 个文件、战斗状态类 10 个、流程类 1 个。
  验证：12 个在场景中的消费者，其角色接口属性全部解析为同一个 `StageController` 实例；
  三次攻击各掉血 20、阵型位置一致。门面直接引用 99 → 35 匹配行（107 → 36 次），
  剩下的基本是每个文件那一行惰性解析。
- A3 收尾：`FakeRouteRuntime` / `RouteStageRuntimeV2` / `TutorialStageDirector` 三处最后的
  门面**成员调用**也改为走接口（`IStageFlowControl.BlockRestartOnDefeat` 改为可写，
  `IRouteBattleHost` 增 `CompleteRouteStage`）。
  **结果：全项目再无消费者调用门面成员**；`StageController.Instance` 剩下的 30 处引用
  全部是「惰性解析单例」那一行（另加单例声明本身、2 行文档注释、1 处遗留原型
  `RouteProgressionController`）。这意味着 `StageController` 现在可以按四个角色拆成
  多个类，而消费者一行都不用改。
- 冰系视觉层拆分（`EnemyFreezeVisual`）：只搬「看起来冻住了」的部分（冰壳覆盖层、冰色着色、
  融化/碎冰特效、程序化贴图），冻结与破冰的**流程**仍留在 Enemy。环境依赖用 Context 结构体
  每次传入，类不持有 Enemy 引用。自检：冻结生成冰壳、呼吸动画 alpha 变化、破冰销毁覆盖层
  （子物体 2→1）、短冻结自然解冻；另起一局攻击命中链路完好（两次各掉 20）。
- `StageController` 按角色拆分（`f906501`）：`StageFormation`（阵型纯计算，可脱离场景单测）与
  `StageRouteBattleHost`（节点战斗的开打/旅行/等奖励状态与迁移，接管原 `_routeBattleRuntime` /
  `_routeRewardWaiting` 两个状态位）拆出，门面 852 → 790 行。
  自检：阵型数值与搬迁前算法**逐位一致**（5 列 × 全排，差异 < 0.0001）；旅行态/奖励等待/重新开打
  三个状态迁移正确且宿主与门面状态位同步；攻击两次各掉 20。
- 遗留路线收编（`55c216a`）：`RouteV2` 全套与 `Route` 里的 V1 原型移入
  `Assets/Scripts/Legacy/`（`git mv`，GUID 不变，试玩场景引用不受影响），Battle 场景里那个
  `RouteStageRuntimeV2` 组件删除，`FakeRouteRuntime.AcquireExclusiveDriving` 里
  「遍历停用 V2 运行时」的竞争逻辑随之移除——**「靠运行期暴力保证唯一驱动」这个历史包袱
  现在没有了**。  `Assets/Scripts/Route` 只留现役的 `RouteChoice*` 三件套。
- Legacy 彻底隔离（`a38ee66`）：现役代码不再引用任何 Legacy 类型——
  `StageController` 删掉 V1/V2 死分支与两个遗留字段（790 → 673 行），`StageConfigManager` 删死字段，
  `BattleHUD` 删 V2 旗标分支，`Battle.scene` 删掉挂着 Legacy 组件的残留对象，
  旧预演驱动 `BattleYRouteHost` 移入 Legacy（`YScrollSample.battleHost` 改 `Component` 类型）。
  **结论：Legacy 不能用 Editor-only asmdef 剔出包**——asmdef 程序集不能引用 Assembly-CSharp，
  而 Legacy 依赖仍在 Assembly-CSharp 的类型；真要剔除，前置是先把共享类型搬进独立程序集。
  详见 `Assets/Scripts/Legacy/README.md`。
- 冰系流程拆分（`34b6bc5`）：`EnemyFreeze` 持有冰系全部状态与流程（冻结/自然解冻/破冰推动与碰撞/
  破冰动画/冰伤害概率/回收清理），表现仍在 `EnemyFreezeVisual`，两者职责不重叠；
  `Enemy` 侧只留薄转发，通过 8 个 `internal` 成员访问，**不改任何 public API**。
  `Enemy` 4798 → 4496 行。

`Locus.Data` 现状：**19 文件 / 1875 行 / 69 类型**。

验证：枚举全项目各声明 1 次；`Assembly-CSharp` 无同名残留且引用 `Locus.Data`；
`TalentTree_Main`/`Zhangfei_Stab`/`Stage_1` 均解析到 `Locus.Data` 类型；枚举值与搬家前一致；
Play Mode 点开始关卡→`phase=Battle`→出怪 1、`ResolveAttackType` 映射不变、Console 0/0。

**一次真事故**：`HitFeedbackManager.cs` 的枚举删除第一次「回执已应用但未落盘」，导致项目里同时
存在两份枚举声明（重复类型，直接编译不过）。靠回读 `grep` 发现、重做并复核才避免。

### 5.4 批次 3 尾项进展：输入门 `GameplayInputGate`（2026-10-22）

> 提交 `4d1a52e`。批次 3 的 `EnemyHealth` 经实测决定不拆（`TakeDamage` 耦合太高），
> 本节是批次 3 剩下的最后一块：**输入门替换 `gameplayInputEnabled`**。

**为什么是它**：`design/architecture-decoupling-analysis.md` P5（层间倒置）与 P2（隐式全局网）都指向同一处 ——
`InputManager.gameplayInputEnabled` 是一个全局布尔，9 处写入点轮流写它，**谁最后写的决定结果**。
`BattleHUD` 在胜利/战败写 `false` 后从不自己撤销，靠路线层在下一场战斗写 `true` 时顺带覆盖掉；
教学门槛的 `true` 同样会踩掉别人的拦截。这就是本仓库之前那三处 bug 的同源形态。

**落地**：

| 项 | 结果 |
|---|---|
| 新增 | `Assets/Scripts/Data/GameplayInputGate.cs`（`Locus.Data` 程序集，未来拆上层程序集时它仍是下层契约） |
| 状态模型 | 按 owner 键保存「拦截 + `InputBlockReason` 原因」；全局被拦 = 任一 owner 持有 |
| 失效保护 | owner 被销毁（场景重载 / 对象回收）时拦截自动失效，不会把输入永久锁死 |
| 编译期护栏 | `InputManager.gameplayInputEnabled` 变**只读属性**，任何直接写都会编译失败 |
| 诊断 | `Describe()` 列出当前持有者与原因；拦截/放行翻转时打一条 `[InputGate]` 日志 |

**写入点迁移（9 处，全部）**：`FakeRouteRuntime`（路线层）、`BattleHUD`（结算面板）、
`TutorialGestureHint`（教学门槛）、Legacy `RouteV1/RouteV2`（两套遗留驱动）、
`Experiments` 的 `ScrollCombatTrial` / `ScrollNodeFlowTrial`（两个试玩脚本）。

**Play Mode 实测（Battle.scene）**：

| 场景 | 期望 | 实测 |
|---|---|---|
| 落地页就绪态 | 路线层持 `RouteTravel` 拦截 | ✅ `Route Flow (Scroll)(RouteTravel)` |
| 点「开始关卡」→ 进入节点战斗 | 路线层释放 → 可操作 | ✅ `blocked=False`、`enabled=True`、出怪 8 |
| 结算面板（Victory / Defeat） | 申请 `ResultPanel` | ✅ 持有者 2 个（路线 + HUD） |
| 下一场战斗（`StageState.InProgress`） | 结算面板**自己**释放 | ✅ `byResult=False` |
| 教学门槛 | `Gate` 申请、`Pass`/`Cancel` 释放 | ✅ `byTutorial` 随开随关 |
| owner 销毁 | 拦截自动失效 | ✅ 销毁后 `blockCount` 立即减 1 |
| 同 owner 重复申请 | 不叠加 | ✅ `blockCount` 不变 |

**唯一有意的行为差异**：教学门槛期间，路线层的"解锁"不再踩掉教学门槛的拦截（旧行为是 bug）。

### 5.5 批次 1 尾项：三个角色接口补齐（2026-10-22）

> 提交 `ee8d5a9`。至此 `design/architecture-decoupling-analysis.md` §5 的 0–3 批全部完成，
> 批次 1 遗留的最后一个 ⬜（三个角色接口）已清。

**为什么**：门面公开面此前散在 4 个接口里，其中 `IStageFlowControl`（战败/重来/返回）
与 `IRouteBattleHost`（路线宿主）职责交叉，后者还是个"什么都能干"的宽端口。
消费方拿到的不是"角色"，是"半个门面"。

**落地**：

| 角色 | 接口 | 消费者 |
|---|---|---|
| 一局流程 | `IRunFlow`（状态 / 开始 / 重开 / 退出 / 战败策略 / 路线配置 / 战败事件） | `BattleHUD` / `PauseMenuUI` / `TutorialStageDirector` |
| 一场战斗的布场与开打 | `ICombatStaging`（节点配置 / 开打 / 停战斗 / 旅行 / 切场景） | 路线层 |
| 奖励门控与结算 | `IRewardGate`（等奖励 / 节点完成 / 路线结算） | 路线层 |
| 路线层合成端口 | `IRouteStageHost : IRunFlow, ICombatStaging, IRewardGate`（0 自己的成员，只做拼装） | `FakeRouteRuntime` / Legacy `RouteStageRuntimeV2` |

**退役**：`IStageFlowControl`（能力并入 `IRunFlow`）、`IRouteBattleHost`（拆成三个窄角色）。

**验证**：

| 项 | 实测 |
|---|---|
| 编译 | 通过；Console 0 error / 0 warn |
| 角色覆盖 | 门面 41 个公开成员中 **36 个**落在角色接口后面；未覆盖的 5 个是 Inspector 协作者字段（`waveSpawner`/`enemyManager`/`playerState`/`enemyPool`/`battleSceneName`），外部读取 0 处，**故意不暴露** |
| 消费者解析 | `BattleHUD` / `PauseMenuUI` / 路线层拿到的都是**同一个** `StageController` 实例 |
| 主流程 | 点「开始关卡」→ 节点战斗（8 敌）→ Stab 掉血 20（总血 150→130） |
| 节点完成 | 清场 → `IRewardGate.IsRouteRewardWaiting=True`、`ICombatStaging.IsRouteCombatActive=False` |

> 附注：`Battle.scene` 里 `FakeRouteRuntime.stageController` 一直是 `{fileID: 0}`（空），
> 路线层一直靠 `StageController.Instance` 兜底解析宿主；本轮未改变这一事实（已核实）。

**下一刀（asmdef 分层的前置）**：门面的公开面已 100% 落在角色接口后面，
因此可以把三个角色的**实现**从 `StageController` 拆成 `StageRunFlow` / `StageCombatStaging` / `StageRewardGate`
三个类（照 `StageFormation` / `StageRouteBattleHost` 的先例），消费者一行都不用改。
→ ✅ **已完成（`43f58e3`）**，见 §5.6。

---

### 5.6 三个角色的实现拆出（2026-10-22）

> 提交 `43f58e3`。§5.5 的「下一刀」已落地。

| 新文件 | 行数 | 内容 |
|---|---|---|
| `Assets/Scripts/Managers/IStageRoleContext.cs` | 75 | 三个角色实现拿环境用的窄接口（继承 `StageRouteBattleHost.IContext`） |
| `Assets/Scripts/Managers/StageRunFlow.cs` | 277 | **一局流程**：开始 / 重开 / 退出 / 波次推进 / 胜负判定 |
| `Assets/Scripts/Managers/StageCombatStaging.cs` | 93 | **一场战斗的布场与开打**：节点配置 / 开打 / 停战斗 / 旅行 / 切场景 |
| `Assets/Scripts/Managers/StageRewardGate.cs` | 75 | **奖励门控与结算**：等奖励 / 节点完成 / 路线结算 |

`StageController` **718 → 519 行**，只保留：Inspector 引用、关卡状态（`currentState`）、
5 个 `public Action` 事件广播、协程宿主、角色接口的显式实现与薄转发。
**公开成员清单与拆分前逐条一致**（41 个），Inspector 序列化未动。

**验证（Play Mode，Battle.scene）**：

| 项 | 实测 |
|---|---|
| 编译 / Console | 通过；0 error / 0 warn |
| 路线流程 | 落地页 → 点「开始关卡」→ 节点战斗（8 敌）→ Stab 掉血 20（150→130）→ 清场 → `IsRouteRewardWaiting=True`、`IsRouteCombatActive=False` |
| 线性关卡 | `StartStage` 出怪 → 战败（`state=Defeat`）→ `RestartStage` → 波次推进 wave 0→5 |
| 胜利路径 | `state=Victory`、`OnStageVictory` 触发 1 次、铜钱结算 +1150 |
| 守卫 | `CompleteCurrentCombatNode` 非 InProgress 时不触发；奖励门控↔旅行状态互转正确 |
| **§15.5 回归基线** | **逐位一致**：入场完成 `col1/r1·col1/r3·col2/r0·col2/r4·col3/r5`；整排清空后 `col1/r0·col1/r2·col2/r3·col3/r4` |

**观察（非本次引入）**：玩家阵亡时门面的 `OnStageDefeat` 事件**不广播** ——
`PlayerState` 先发 `OnStageStateChanged(Defeat)` 再发 `OnPlayerDied`，门面状态已被同步为 `Defeat`，
`OnPlayerDefeated` 的早退守卫命中。战败面板走 `PlayerState` 自己的事件（显示正常）。拆分前后一致。

### 5.7 剩余上帝类：按 region partial 分文件（2026-10-22）

> 提交 `c3a9a5b`。批次 3 刀 C 的手法（partial class 分文件）用到两个从未被拆过的上帝类上：
> 同类型、零行为风险，只降单文件体量与阅读成本。

| 类 | 拆前 | 拆后 | 分片 |
|---|---|---|---|
| `AttackSystem` | 1568 | **386 / 591 / 537 / 98** | 主文件（字段+生命周期+招式分发）/ 攻击类型实现 / 工具方法 / 解锁攻击注册表 |
| `QTEController` | 1474 | **236 / 248 / 149 / 104 / 542 / 264** | 主文件（枚举+QTEInstance+字段+生命周期）/ 状态切换 / 箭矢波 / QTE 动画 / QTE 判定 / 收尾与表现 |
| `Enemy.State`（批次 2 补） | 1495 | **17 / 768 / 739** | 只留类声明与总览 / 状态切换 / 状态更新 |
| `InputManager`（批次 2 补） | 981 | **319 / 164 / 370 / 161** | 主文件（字段+生命周期）/ 指针输入 / 手势识别 / 坐标映射与输入过滤 |

**拆分前的机械校验（写入前在内存里跑，不通过就不落盘）**：

| 检查 | 结果 |
|---|---|
| 内容行多重集（拆分前后逐行比对，剔除空白/region 标记/类声明） | **丢失 = 0** |
| 新增行 | 只有文档注释 + 每个分片的 using 副本 |
| region 数 / 未闭合 region | 3→3、7→7；未闭合 0 |
| 花括号平衡 | 每个文件 `{` 与 `}` 数量相等 |
| 字段随 region 搬走 | 3 个实例字典初始化 + 2 个 `static readonly` 列表，彼此无依赖 → 初始化语义不变；字段名未变 → Unity 序列化不受影响 |

**验证（Play Mode，Battle.scene）**：编译通过、Console 0/0；Stab 掉血 20（150→130）、
Slash 掉血 18（126→108，前排两名）、Launch 掉血 4；Boss 104 首次 `SelectBossAction()`
即 `qteState=PerformingQTEAttack` / `enemyState=QTEAttacking`。

**当前最大文件**：`Enemy.cs` 933 / `DisplacementMover.cs` 894 / `StageProgressBar.cs` 864 / `CavalryEnemy.cs` 830 / `Enemy.Combat.cs` 828。

> 至此**全部文件 ≤ 933 行**（本轮把 `UpgradeEffectManager` 1284、`PixelHitEffectManager` 1227、
> `StabSweepEffect` 1012 也拆开了）。剩下的都是「机制本身就是这么大」的模块，
> 继续拆的收益递减；下一步的量级变化要靠**程序集分层**（把一整层搬出 `Assembly-CSharp`）。

> 剩下这三个（1284 / 1227 / 1012）**没有 region 结构**，要拆得先人工按方法语义分组，
> 属于「需判断」而非「机械搬移」，暂缓。
> 另一个坑（已踩两次）：分片文件必须**各自带齐 using**（主文件也要），否则编译报
> “MonoBehaviour / Vector2 等找不到”；`InputManager` 的分片还需 `using UnityEngine.EventSystems;`。

### 5.8 契约层与配置层下沉到 Locus.Data（2026-10-22）

> 提交 `c0ffb1d`。这是 §4.1 目标分层的第一步：把叶子层补完整。

**搬入 `Locus.Data`（19 个文件，git rename，GUID 全部保留）**：

| 类别 | 类型 |
|---|---|
| 契约 | `StageState`（从 `PlayerState.cs` 抽出）、`IStageFormation`、`IStageBattleState`、`IRunFlow`、`ICombatStaging`、`IRewardGate`、`IRouteStageHost` |
| 配置 | `FakeRouteStageConfig` / `NodeConfig` / `BattleEntry` / `ChoiceConfig` / `Presentation` / `TravelPresentation`、`HeroConfig`、`HeroHUDSkin`、`DialogueEventData`、`BossDialogueProfile`、`MoveTableConfig`、`RouteChoiceLayout`、`UltimateSkillConfig`、`ScrollRouteVisualProfile` |

**留在 `Assembly-CSharp`**：`IStageRoleContext`（继承 `StageRouteBattleHost.IContext`，
而后者依赖 `WaveSpawner`/`EnemyManager`/`PlayerState`/`EnemyPool` 等 MonoBehaviour）——
它是「门面 ↔ 角色实现」的内部接线契约，不是对外端口。

**方法论（值得记）**：**编译器就是闭包检查器**——`Locus.Data` 不能引用 `Assembly-CSharp`，
少搬一个依赖类型就会直接编译报错。做法：先用「只解析声明的成员类型」算闭包 → 搬 → 编译；
本次**一次编译通过**，说明闭包算准了。
（注意：闭包脚本不能用「扫任意大写词」当类型引用，会把方法体里的词误判 → 闭包爆炸到全工程。）

**验证**：反射核对类型归属全部正确；2 个路线配置资产 + 1 个 `HeroConfig` 资产均可加载；
Play Mode 落地页 → 战斗 → Stab 掉血 20 → **§15.5 基线逐位一致** → 路线正常推进；Console 0/0。

### 5.9 局内升级体系：把两处硬编码分派改成注册表（2026-10-22）

> 提交 `45f6f54`。P3 里点名的“下一条触发键会直接撞在 UpgradeEffectManager 身上”，本刀正面处理。

**改造前的两处硬编码**：
① `ApplyNumericEffect` 的 13 分支 `switch (def.effectType)`；② `FormatDescriptionForLevel` 的 215 行 if-else 链。
两者都是“新机制必改本体”的典型点。

**现在的形态**（沿用已有的 `IEffectExecutor` 注册表范式）：

| 扩展点 | 接口 | 注册入口 |
|---|---|---|
| 数值型升级效果 | `IUpgradeNumericApplier` | `RegisterNumericApplier(effectType, applier)`（含委托重载） |
| 升级描述文案 | `IUpgradeDescriptionFormatter` | `RegisterDescriptionFormatter(formatter)`（含委托重载） |
| 玩家数值面 | `IUpgradeStatSink` | 由管理器实现，应用器通过它写数值（`Add*` 累加 / `Set*` 覆盖） |

内置行为搬进 `DefaultUpgradeNumericApplier`（原 switch）与 `DefaultUpgradeDescriptionFormatter`（原 if-else 链，逐字保留），
它们在 `Awake` 时批量注册；**自定义优先于内置兼底**。

**用法（新增一种数值型升级）**：
```csharp
UpgradeEffectManager.Instance.RegisterNumericApplier("crit_rate",
    (def, level, cfg) => CritService.Instance.AddRate(cfg.floatValue));
```

**验证**：
- **升级快照逐行一致**：改造前/后各跑一遍「9 个升级定义 × 每级的描述 + 18 项数值」全量快照，diff 零差异。
- 扩展点可用：注册一个全新 effectType（委托版）+ 一个自定义描述格式化器，两者都按预期生效；内置类型文案未变。
- **§15.5 回归基线逐位一致**；Console 0/0。

### 5.10 路线节点：完成策略注册表（2026-10-22）

> 提交 `a00134e`。与 §5.9 同范式（升级体系），但对象是**路线节点**。

**问题**：数据模型早写了两个策略字段——`FakeRouteNodeConfig.completionPolicy`（4 种）与 `terminalPolicy`——
但运行时一直用固定规则（战斗条目跑完 = 完成；`isFinalNode` = 收尾整关），
两个字段**长期没有消费点**（`design/gameplay-pillars.md` 已标为 ❌）。
实测：长坂坡的 EV / J1 声明 `completionPolicy=ChoiceConfirmation`、N2 / N3 声明 `terminalPolicy=1`，
运行时却一律按旧规则走 → 资产表达的意图被顶掉了。

**同时拆分**：`FakeRouteRuntime` 914 → 主文件 294 / Entry 202 / Node 146 / Choice 75 / Checkpoint 210 / Finish 61。

**扩展点**：

| 接口 | 作用 |
|---|---|
| `IRouteNodeCompletionPolicy` | 某节点是否由本策略接管 / 内容是否完成 / 是否收尾整关 |
| `IRouteNodeProgressView` | 节点进度只读视图（条目数、条目完成、奖励是否待领） |
| `DefaultRouteNodeCompletionPolicy` | 内置兼底 = 改造前的固定规则（行为不变） |
| `DelegateRouteNodeCompletionPolicy` | 委托版，注册时直接传 lambda |

注册入口：`FakeRouteRuntime.RegisterNodeCompletionPolicy(...)`。

**验证**：
- **节点判定快照逐行一致**：改造前后各跑一遍「2 条路线 × 8 个节点」的字段 + 三项判定，diff 零差异。
- 快照第一次跑出差异，定位到内置策略在 `Awake` 里初始化（Edit Mode 不跑 Awake → 策略为 null → 判定失真）——
  已改为字段初始化，顺带消除「Awake 前调用就变行为」的隐患。
- 扩展点可用：注册只接管 N1 的策略 → N1 的判定改变，其它节点不受影响。
- **§15.5 回归基线逐位一致**；Console 0/0。

> **不改变任何节点的完成语义**（EV / J1 的 ChoiceConfirmation 仍按旧规则跑）——那是玩法决策，单独排期；
> 本次只把「可表达」的口子留出来。

### 5.11 三选一：抽池独立 + 两把扩展点（2026-10-22）

> 提交 `0b4e4c7`。需求：「后续可能会部分根据**天赋的解锁**才会入池，以及一些**额外效果**」。

**改造前**：`UpgradeChoiceManager` 567 行里，流程（触发→暂停→弹窗→确认→恢复）与抽池
（四个几乎重复的收集方法 + 两种抽取）混在一起；入池条件只有「需要另一个升级达到 N 级」一种硬编码。

**现在的形态**：

| 扩展点 | 接口 | 注册入口 |
|---|---|---|
| 入池条件（天赋解锁等） | `IUpgradeRequirementProvider`（+ `UpgradePoolKind`） | `UpgradeChoiceManager.RegisterRequirementProvider(...)` |
| 选中后的额外效果 | `IUpgradeSelectionEffect` | `UpgradeChoiceManager.RegisterSelectionEffect(...)` |
| 玩家数值面（上一刀） | `IUpgradeStatSink` | — |

- 抽池独立成 `UpgradePoolSampler`（纯 C#，依赖注入：当前等级 / 被动上限 / 主动技能可获得性），
  四个池的收集与两种抽取都在它里面；`UpgradeChoiceManager` 567 → **436 行**，只管流程。
- 内置条件源 `TalentUpgradeRequirementProvider`：按 `UpgradeDefinition.requiredTalentIds`
  判断（天赋状态以 `TalentTreeState` 为准，唯一权威是存档里的 `unlockedTalents`）。
- 两个扩展点都带**委托重载**（注册时直接传 lambda，不必写类）。

**用法**：
```csharp
// 按天赋解锁入池：升级定义里填 requiredTalentIds 即可，无需改代码
// 额外入池条件：
manager.RegisterRequirementProvider(new MyProvider());
// 选中后的额外效果：
manager.RegisterSelectionEffect(def => def.effectType == "crit_rate",
                                def => CritService.Instance.Grant(def.upgradeId));
```

**验证**：
- **四个池的候选集逐字一致**（改造前后快照 diff 零差异：id / 稀有度 / 权重全同）。
- 扩展点实测：加 `requiredTalentIds`（未解锁）→ 掉出池；解锁天赋（写存档）→ 回到池；
  注册选中效果 → `ConfirmChoice` 时触发 1 次且升级正常应用。
- **§15.5 回归基线逐位一致**；8 条升级描述文案未变；Console 0/0。

> 注：`IEffectExecutor`（持续型行为效果）与 `IUpgradeSelectionEffect`（一次性副作用）是两种东西，
> 前者已存在，本次补上后者。

### 5.12 分支出口：呈现与可选性两把扩展点（2026-10-22）

> 提交 `91915da`。非战斗侧最后一个缺扩展点的部分。

**改造前**：出口的「呈现」（标题拼法）与「可选性」（禁止回访）都写死在 `FakeRouteRuntime` 里；
新分支形态（带预览 / 带消耗 / 需要天赋解锁）必须改运行时。

**现在的形态**：

| 扩展点 | 接口 | 说明 |
|---|---|---|
| 出口呈现 | `IRouteChoiceFormatter` | 第一个 `CanHandle` 为真的格式化器完全接管该出口；内置兼底 = 「出口名 → 目标节点名」 |
| 出口可选性 | `IRewardGate`→`IRouteChoiceValidator` | 内置规则 = 禁止回访已访问节点；不可选时**锁定按钮并把原因显示在副标题上**（原来点不动但无提示） |

- `RouteChoiceOption` 下沉到 `Locus.Data`（扩展点要引用它），并加 `locked` / `lockedReason`。
- `FakeRouteRuntime` 新增 `RegisterChoiceFormatter` / `RegisterChoiceValidator`（均带委托重载）。
- 选项构造抽成 `BuildChoiceOptions(node)`，面板只管显示。
- `RouteChoiceButton` 尊重 `option.locked`（原来恒为可点）。

**验证**：出口选项快照逐字一致（2 路线 × 6 出口的 id + 文案，diff 零差异，当前路线无回访→无锁定）；
扩展点实测（自定义格式化器接管文案 / 自定义判定器锁定并带原因）；**§15.5 基线逐位一致**；Console 0/0。

> 至此**非战斗侧（落地页 / 四个界面 / 路线流程 / 三选一 / 分支选择）全部有扩展点**。

### 5.13 战斗侧两把扩展点（2026-10-22）

> 提交 `d39ddc8`。战斗侧之前没有「机制延伸」的口子。

**① 招式执行器注册表**
`AttackSystem.TryExecuteAttack` 里的 `switch (attackType)` 六个分支改成注册表
（`AttackType → 执行委托`），内置六个照旧注册。新增 `AttackExecutionContext`
（目标列 / 横扫方向 / 倾斜 / 蓄力）随执行器传入。
新招式 = `RegisterAttackExecutor(type, executor)`；同一类型后注册者覆盖先注册者；
未注册的招式不执行（与旧 switch 的默认分支一致）。

**② 敌人状态统一查询（`EnemyStatusRegistry`）**
状态效果此前散在三处、形状还不同：灼烧/疾病在 `UpgradeEffectManager`（玩家侧持有状态）、
冻结在敌人自己的状态机（`EnemyFreeze`）。于是「这个敌人是不是在燃烧」只能去问那一个系统。

现在每个状态注册一个 `IEnemyStatusEffect`（是否生效 / 进度 / 层数 / 优先级），
任何系统都能统一地问：
```csharp
if (EnemyStatusRegistry.IsActive(enemy, "poison")) { /* 新机制：中毒的敌人受到额外伤害 */ }
```
内置三个：灼烧 / 疾病 / 冻结。`EnemyHealthBar` 与 `BossHealthUI` 改为经注册表取状态
（显示槽位仍是疾病条/灼烧条，**视觉不变**）。

> 本接口只负责**查询与展示**；状态的施加与结算仍由各自的所有者负责
> （灼烧疾病在管理器、冻结在敌人状态机）——与 `EnemyFreeze` 的先例一致：
> 新状态自己写一个持有敌人侧状态的类。

**验证**：六个内置招式全部已注册执行器；Stab 经注册表执行掉血 20；
覆盖语义（注册自定义 Stab 执行器 → 被调用、返回 false、不再造成伤害）；
状态注册表与直接查询逐字一致；**§15.5 基线逐位一致**；Console 0/0。

---

## 6. 明确不做

| 不做 | 原因 |
|---|---|
| 整体换 ECS / DI 容器 | 226 脚本、36 单例，迁移成本远超收益 |
| 一次性把 `Enemy` 拆成 10 个文件 | 契约未抽先拆，只会产生碎片 |
| 为"可能存在的第二种路线实现"预埋抽象 | `Route` / `RouteV2` 两套预埋已经付出了成本 |
| 提前重写存档格式 | 与批次 4 绑定，先分片再谈格式 |
| 把 `PlayerState` 立刻砍成多个服务 | 157 处引用，且与伤害/奖励/存档强耦合，应最后动 |

---

## 7. 复测方法（命令口径）

```bash
# 程序集
find Assets -name "*.asmdef" | wc -l

# 脚本与体量
find Assets/Scripts -name "*.cs" | wc -l
find Assets/Scripts -name "*.cs" -exec wc -l {} + | sort -rn | sed -n '2,10p'

# 单例与静态面
grep -rn "public static [A-Za-z]* Instance" --include=*.cs Assets/Scripts | wc -l
grep -rnE "^\s*public static " --include=*.cs Assets/Scripts | wc -l

# 运行时查找（越大越差）
grep -rn "FindObjectOfType\|FindObjectsOfType\|FindFirstObjectByType\|FindObjectsByType\|FindAnyObjectByType" --include=*.cs Assets/Scripts | wc -l

# 每帧轮询
grep -rn "void Update()\|void FixedUpdate()\|void LateUpdate()" --include=*.cs Assets/Scripts | wc -l

# 违规引用（anti-ghost-reference.md）
grep -rn "Resources.Load" --include=*.cs Assets/Scripts | wc -l

# 隐式全局状态（示例：以输入旗标为指标）
grep -rn "gameplayInputEnabled\s*=" --include=*.cs Assets/Scripts | grep -v NonSerialized | wc -l
```

**当前基线（2026-10-22，HEAD `4d1a52e`）**：asmdef **1** / 脚本 **250** / 单例 **37** / `public static` **194** / `Find*` **92** / 每帧回调 **73** / `Resources.Load` **8** / **输入旗标写入 0**（旧基线：asmdef 0 / 脚本 226 / 单例 36 / `public static` 192 / `Find*` 96 / 每帧回调 75 / `Resources.Load` 8 / 输入旗标写入 8）。

> 尚未动的三项：`Resources.Load` 8 处（anti-ghost-reference 违规项，且 `EnemyPool` 按名加载导致包体依赖剪不掉）、
> 单例 37 / `public static` 194、批次 1 尾项的三个角色接口（`IRunFlow` / `ICombatStaging` / `IRewardGate`）。

---

## 8. 前置条件与风险

1. **工作区脏必须先收敛**。当前混着两条未提交的流：道具系统（`Assets/Scripts/Prop/` 整个目录 untracked + `BreakableProp` 相关改动）与教学关导演重写（`TutorialStageDirector.cs` +556/−229 + 其路由资产）。`Prop/` 一旦进入程序集边界，会立刻暴露它引用不到的类型——asmdef 拆分不能在这个状态下开工。
2. **场景侧耦合容易被忽略**。`Battle.scene` 有 21 个根对象 / 393 个 MonoBehaviour，其中挂着三套路线驱动里的两套；改代码边界时必须同步清场景，否则"代码已解耦、场景还在互相禁用"。
3. **存档兼容性**。批次 4 前不要动 `SaveData` 字段语义；`MigrateIfNeeded` 是唯一既有迁移入口，改动要点在文档里留样本。
4. **小程序包体约束**。asmdef 与场景依赖同时决定"能不能剥掉某些模块"；阶段 E（包体）的结论是敌人美术几乎全部在用，真正的裁切空间在模块级，而不是删贴图。

---

## 9. 与其他文档的关系

| 文档 | 关系 |
|---|---|
| `design/gameplay-pillars.md` | 玩法权威：本文 §1 只做坐标复述，支柱细节以它为准 |
| `design/game-mechanics.md` | 系统实现与资产地图；本文 §2 的体量数据是它的结构视图 |
| `design/anti-ghost-reference.md` | 数据引用规范；本文 §4.7 的 `Resources.Load` 清单是它的违规项 |
| `design/immutable-constraints.md` | 位移三原则；本文 P3 提到 `ColumnManager` 语义稳定性与它相关 |
| `plan/main-menu-hub-refactor-plan-20261020.md` | 落地页/界面职责的功能计划；本文 §3 P2 的三处 bug 即来自该计划的阶段 F/G/H |
| `memory/work-loop-discipline.md` | 每批改造的收尾与验证纪律 |
