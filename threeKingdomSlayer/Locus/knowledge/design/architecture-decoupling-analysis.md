---
id: kd_a6b3e958-ee14-46fb-956a-17f03b2ee980
injectMode: inherit
injectAgents:
- '*'
summary: 架构解耦与模块化分析：实测 226 脚本/0 asmdef/36 单例/96 处 Find*，指出六个结构性问题（无程序集边界、隐式全局网、上帝类、三套路线驱动、层间倒置、状态聚合），给出 asmdef 分层/三个会话/事件化/IRouteFlow/Enemy 分批拆/输入门/存档分片的方案与 0-4 批实施顺序，含复测命令与基线。
aiEditMode: inherit
---

# 架构解耦与模块化分析（2026-10-20）

> **本文只做分析与方案，不含任何落地改动。** 落地按 §5 的批次单独排期。
> 测量基线与复测命令见 §7；与功能计划的关系见 §9。
> 测量时的 HEAD：`1b1c2e5`（`feat/stage-select-talent-overlays`）。

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
| `Assets/Scripts/Core/UpgradeEffectManager.cs` | 1276 | 局内 Build 的万能执行器 |
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

落地顺序建议：**先只拆 `Locus.Data`**——它是叶子，几乎没有调用方需要改；拿到边界后，再逐层把 `Combat` / `Route` / `App` 拉出去。

### 4.2 三个"会话"边界替代全局门面

| 会话 | 拥有 | 生命周期 | 对外暴露 |
|---|---|---|---|
| `AppSession`（落地页/界面） | 当前关卡、四个界面的导航、Hub 可见性 | 进程级 | `ShowInterface(kind)`、`StateChanged` |
| `RunSession`（一局） | route 阶段、节点推进、checkpoint、奖励门控 | 一次运行 | `Begin(runConfig)`、`Phase`、`PhaseChanged` |
| `CombatDomain`（一场战斗） | 敌人培育 / 列阵 / 波次 / 伤害结算 | 节点内 | `StartBattle(stageConfig)`、`BattleCleared` |

`StageController` 的 50 个 public 成员按这四类拆成接口：`IRunFlow` / `ICombatStaging` / `IRewardGate` / `ISceneHost`。

> **手法**：先"只加接口、实现仍由 `StageController` 承担、行为一行不改"，让调用方逐步改成依赖接口。这一步零行为风险，但立刻让"谁在依赖什么"变成可读信息。

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

### 4.6 输入：从布尔旗标到"输入意图门"

```csharp
enum InputBlockReason { None, Dialogue, RouteTravel, Reward, HubInterface, Defeat }
GameplayInputGate.RequestBlock(owner, reason);   // 唯一所有者
GameplayInputGate.Release(owner);
bool GameplayInputGate.IsBlocked { get; }        // 可查询
```

把现有 8 处直接写 `gameplayInputEnabled` 改为申请/释放，附上原因。"为什么点不动"从"要读 5 个模块"变成一次查询。

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
| Hub 三处轮询合并为 `HubStateSource` | ✅ 代码已提交 | `31f0332` |
| `Locus.Data` asmdef | ⬜ 未开工（前置已解除） | — |
| Play Mode 回归 | ⚠️ **未执行**（`unity_execute` 通道异常，连 trivial probe 都返回空） | — |

**死路径清理的实际发现**：`StageController.Awake` 里四条 Pending* 分支（MainMenu 传 FakeRoute / 传 RouteV2 / 线性关卡 / 旧路线）**全部**无人写入，不只是文档里写的两条；两个 `public static` 字段也仅被自己置 null。已全部删除，Hub 自举成为唯一入口。

**Hub 事件化后的形态**：`HubStateSource`（挂在 `Battle.scene / BattleHUD(Canvas)`）是 `IsPrepared` 与 `Overlay` 的唯一计算源，变化时抛 `Changed`，首帧强制广播一次。三个组件改为 `OnEnable` 订阅 + `TrySubscribe()`（源可能晚 Awake，`Start` 再试一次），各自的 `Update` 与私有 `IsOverlayOpen()` 已删（`grep "void Update()"` 三文件均为空）。

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

**当前基线（2026-10-20，HEAD `1b1c2e5`）**：asmdef 0 / 脚本 226 / 单例 36 / `public static` 192 / `Find*` 96 / 每帧回调 75 / `Resources.Load` 8 / 输入旗标写入 8。

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
