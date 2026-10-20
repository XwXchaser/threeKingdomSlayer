---
id: kd_25b1b2d9-4030-4f1c-b055-1d7c0bbbf47b
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: 架构改造交接（2026-10-20）：41 个提交落地——落地页/界面职责定稿、Hub 状态事件化、Locus.Data 程序集、18+消费者迁到角色接口、门面成员调用清零、Enemy 拆描边与冰系视觉、StageController 按角色拆类、Legacy 隔离；含量化现状、三档验收清单、未覆盖项与后续选项。
---

# 架构改造交接（2026-10-20）

> 接手入口。配套阅读：`design/architecture-decoupling-analysis.md`（问题分析与方案）、
> `plan/main-menu-hub-refactor-plan-20261020.md`（落地页/界面功能计划）。
> 测量基线：`770bba9`；本轮结束：`afbd41e`；**41 个提交**，全部已 push 到
> `feat/stage-select-talent-overlays`，工作区干净。

---

## 0. 一句话现状

功能面（落地页 + 四个界面）已跑通并由用户验收；结构面把「无边界、隐式全局、上帝类、三套路线驱动」四个结构性问题处理到可继续推进的程度——**门面成员调用清零、敌人/关卡门面瘦身后拆分、遗留实现隔离成孤岛**。

---

## 1. 本轮交付清单

### 1.1 落地页 / 界面职责（功能面）

| 提交 | 内容 |
|---|---|
| `fb5279b` | 落地页接存档点、通关推进下一关、选关立即生效（自举路径三个 bug） |
| `1210dc5` / `d3676c3` | 二级页面接管全屏、tab 栏改为**界面导航**（选关/天赋是界面，不是二级窗口） |
| `1b1c2e5` | 写死界面职责：四个 tab = 四个界面（`plan/...20261020.md` §4.3/§4.4） |

### 1.2 存量工作流落地（工作区从 92 个脏文件清零）

| 提交 | 内容 |
|---|---|
| `39ac81b` | 教学关导演重写 + 手势门槛 + 招式解锁与冰系天赋 |
| `e036ad2` | 可破坏道具系统（**同时修好 HEAD 缺 `BreakableProp` 类型导致的 clean clone 编译不过**） |
| `99ba074` / `65e1957` / `4eec32b` / `36b1a12` | 数值配置、卷轴试玩数据、1021 盾侧判定、天赋页关闭按钮 |

### 1.3 结构改造（批次 0 / 1）

| 提交 | 内容 |
|---|---|
| `f139212` | 删掉四条无人写入的跨场景启动入口 |
| `31f0332` | `HubStateSource` 单一状态源，替换三处每帧轮询 |
| `5b359d2` / `8a3586c` | 拆出 `Locus.Data` 程序集；纯枚举（`AttackType`/`DamageType`/`HitFeedback*`）提到数据层，8 个配置搬入 |
| `b2da9d2` | 路线层改用端口接口 `IRouteBattleHost` |
| `9f735e2` / `a0dccc9` / `853d022` / `d705596` | 拆出 `IStageFormation` / `IStageFlowControl` / `IStageBattleState`，28 个消费者迁移，**门面成员调用清零** |
| `eac2922` / `86c3fbd` | `Enemy` 拆出 `EnemyOutline`（描边）与 `EnemyFreezeVisual`（冰系视觉层） |
| `f906501` | `StageController` 按角色拆出 `StageFormation` / `StageRouteBattleHost` |
| `8224843` | 修：主场景重载后 overlay 栈过期，导致 hub 内容永久隐藏 |
| `55c216a` / `a38ee66` | 遗留路线实现收进 `Assets/Scripts/Legacy/`，现役代码不再引用任何 Legacy 类型 |

---

## 2. 当前结构快照（量化）

| 指标 | 改造前 | 现在 |
|---|---|---|
| `Enemy.cs` | 5224 行 | **4798 行** |
| `StageController.cs` | 852 行 | **708 行** |
| 门面（`StageController`）成员调用 | 大量 | **0**（`StageController.Instance` 仅剩 30 处惰性解析点） |
| 角色接口 | 0 | 4 个（`IRouteBattleHost` / `IStageFormation` / `IStageFlowControl` / `IStageBattleState`） |
| 数据层程序集 | 无 | `Locus.Data`：19 文件 / 1875 行 / 69 类型 |
| 路线驱动 | 3 套并存 + 运行期互禁 | 1 套现役 + 1 套 Legacy 孤岛（无互禁） |
| 遗留实现 | 散在 Core/Route/RouteV2 | `Assets/Scripts/Legacy/`（21 文件 / 2107 行）+ README |

---

## 3. 验收清单

### 3.1 脚本已自动验过（每次都跑过，可信）

| 项 | 证据 |
|---|---|
| 编译 | `unity_recompile` = compiled（多次） |
| 落地页主流程 | Ready → 点「开始关卡」→ `phase=Battle`、出怪 1、攻击掉血 20/次 |
| 四个界面态 | 战斗 `overlay=null`；选关 `overlay=StageSelect`；天赋 `overlay=TalentTree`；tab 栏常驻、当前 tab 高亮 |
| 界面内容让位 | 界面打开时开始按钮 alpha=0、头像/金币隐藏；关闭后全部还原 |
| 阵型数值 | 与改造前算法**逐位一致**（5 列 × 全排，差异 < 0.0001） |
| 战斗状态迁移 | 旅行态 `IsRouteCombatActive=false`、奖励等待 `IsRouteRewardWaiting=true`、重新开打 `true` |
| 冰系 | 冻结生成冰壳、呼吸 alpha 变化、破冰销毁覆盖层（子物体 2→1）、短冻结自然解冻 |
| 描边 | 穿刺高亮 0.71（白/宽 3）→ 调试霸体 1.00（橙/宽 8）→ 关高亮 0.00 |
| 遗留实验场景 | 4 个场景（Bisect / ScrollCombatTrial / ValleyCombatTrial / FakeRouteDataTrial）**缺失脚本 = 0** |
| Console | 全程 warn/error = 0 |

### 3.2 需要你人眼确认（脚本测不了）

1. **落地页观感**：头像/金币/开始关卡/底部 tab 的大小与位置、就绪态背景好不好看。
2. **界面叠放**：选关/天赋页与底部 tab 栏的视觉关系（tab 栏画在界面之上，order 50 vs 40/41）。
3. **天赋树面板与底栏是否重叠**（`TalentTreePanel` 是全屏铺满的，可能压到底栏区域）。
4. **两个界面的「返回」按钮**：现在它们与 tab 栏并存（点返回 = 回战斗界面）。若你倾向纯 tab 导航，可删掉这两个按钮。

### 3.3 明确未覆盖（别当成已验证）

- 真实手打到通关（结算用 `FinishRoute` 驱动，不是手打）。
- 战败重来（只做了代码路径确认，没实机点过「重来」）。
- 冻结→破冰后敌人停在第 1 排时 Slash 打不到：属既有「打前排」命中规则，**不是**本次改造引入。
- 包体：字体 36MB、`Enemy_1`/`Enemy_102`（~2.4MB）、敌人贴图 —— **按你的要求整体延后，尚未动手**。
- 小程序真机（微信开发者工具）未跑。

---

## 4. 已知遗留与风险

| 项 | 说明 |
|---|---|
| `StageController` 仍有 5 个只给 Legacy 用的入口 | 刻意不引用 Legacy 类型；Legacy 仍是孤岛 |
| Legacy 不能用 Editor-only asmdef 剔出包 | asmdef 程序集**不能引用 Assembly-CSharp**；要真剔除，前置是先把共享类型搬进独立程序集 |
| `Assets/Scripts/Prop/`（BreakableProp）在本轮才进仓库 | 之前 HEAD 是**坏树**（clean clone 编译不过），`e036ad2` 已修 |
| 工具通道偶发异常 | 本轮遇到 `unity_execute` 连续返回空、一次回显混入非脚本文本、两次 `edit` 回执未落盘。处置见 `memory/tool-result-verification.md`（已补两条纪律） |
| 截图工具不能做 UI 像素验证 | 仍以状态回读 + 人眼为准 |

---

## 5. 后续选项（按风险从低到高）

| 选项 | 内容 | 风险 | 备注 |
|---|---|---|---|
| A. 收尾小事 | 删两个界面的「返回」按钮；调天赋树面板底部留白 | 低 | 纯 UI |
| B. 包体 | 字体 36MB / `Enemy_1`+`Enemy_102`（2.4MB）/ 试玩场景剔除 | 低-中 | 阶段 E 已实测过数据；你之前要求延后 |
| C. `EnemyFreeze` 流程拆分 | 冰系**状态机**（剩 333 行）搬进 `EnemyFreeze` | 中高 | 敌人机制支柱，与状态机/伤害/挂起令牌交织 |
| D. `ColumnManager` 拆分 | 2130 行 → 阵型/位移/补齐/骑兵槽位 | 中高 | 承载「位移三原则」，越稳越好 |
| E. 共享类型独立成程序集 | 为「Legacy 剔出包」铺路 | 中 | 需先给 `Enemy`/`PlayerState` 等定程序集边界 |

---

## 6. 工作方式提醒

- 每批改造：**改码 → 编译 → 静态复查（`git diff`）→ 一次 Play Mode 跑完所有验证 → 提交**；
  不要反复进出 Play Mode（每次进出都会切碎一个回合，见 `memory/work-loop-discipline.md`）。
- 提交前必看 `plan/main-menu-hub-refactor-plan-20261020.md` §7 的 25 条坑（尤其 15 工作区脏时的
  hunk 拆分、17 untracked 引用、19 跨文件成组提交）。
- 改完必须回读目标文件确认落盘，**不要相信回执**。
