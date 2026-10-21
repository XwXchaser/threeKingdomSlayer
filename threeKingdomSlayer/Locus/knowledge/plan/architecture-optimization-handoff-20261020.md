---
id: kd_25b1b2d9-4030-4f1c-b055-1d7c0bbbf47b
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: 架构改造交接（2026-10-21 更新）：74 个提交落地——落地页/界面职责定稿、Hub 状态事件化、Locus.Data 程序集、门面成员调用清零、Enemy 拆描边与冰系视觉、StageController 按角色拆类、ColumnManager 六刀拆完（2149→662 行）、Legacy 隔离；含量化现状、脚本已自动验过的清单、**当前可验收清单（面向人眼/手操）**、明确未覆盖项与后续选项。
---

# 架构改造交接（2026-10-21 更新）

> 接手入口。配套阅读：`design/architecture-decoupling-analysis.md`（问题分析与方案）、
> `plan/main-menu-hub-refactor-plan-20261020.md`（落地页/界面功能计划）、
> `plan/columnmanager-split-plan-20261020.md`（ColumnManager 拆分操作手册与验收基线）。
> 测量基线：`770bba9`；当前 HEAD：`c762a90`；**74 个提交**，全部已 push 到
> `feat/stage-select-talent-overlays`，工作区干净。
> 最近更新：2026-10-21 —— 批次 2「ColumnManager 拆分」六刀全部完成（2149 → 662 行）。

---

## 0. 一句话现状

功能面（落地页 + 四个界面）已跑通并由用户验收；结构面把「无边界、隐式全局、上帝类、三套路线驱动」四个结构性问题处理到可继续推进的程度——**门面成员调用清零、敌人/关卡/列管理器三个上帝类已拆分、遗留实现隔离成孤岛**。

> 2026-10-21 追加：批次 2「ColumnManager 拆分」六刀全部完成（2149 → **662 行**），
> 新的可验收清单见 §4。

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
| `34b6bc5` | `Enemy` 拆出 `EnemyFreeze`（冰系状态与流程，4798 → 4496 行） |
| `5769fb0` / `dc23b6f` | PerColumn 退役：默认值反转 + 导入期报错；清理 119 个无引用文件（现役只剩教学关 + 正式 YJ） |
| `b3c382b` | 补齐豁免扩展点 `Enemy.ignoreFillUpOrders`（A/B 实测：豁免者不前移、同排他人照常前移、仍算占位） |
| `f906501` | `StageController` 按角色拆出 `StageFormation` / `StageRouteBattleHost` |
| `8224843` | 修：主场景重载后 overlay 栈过期，导致 hub 内容永久隐藏 |
| `55c216a` / `a38ee66` | 遗留路线实现收进 `Assets/Scripts/Legacy/`，现役代码不再引用任何 Legacy 类型 |

### 1.4 批次 2：ColumnManager 拆分（2149 → 662 行，六刀全完成）

| 提交 | 抽出 | 结果 |
|---|---|---|
| `facdbde` | 刀 1 `LogicalRowQueue`（逻辑排队列：活人排/空排占位符/999 门） | 2149 → 2026 |
| `415d4c0` | 刀 2 `ColumnQueries`（13 个只读查询） | → 1958 |
| `6add76a` | 刀 3 `SpawnEntryMover`（入场调度） | → 1836 |
| `c12d76a` + `b63af80` | 刀 4 `WaveMarchScheduler`（4a 状态+查询 / 4b 补齐流程） | → 1370 |
| `e5e997f` | 刀 5 `DisplacementMover`（击退回位/破冰顶开/击退/横推/聚拢） | → **662** |
| `86832d4` | 刀 6 退役 `FillUpRule`（补齐规则只剩一种） | — |
| `49d4e96` | 附带修掉：连击数字在物体被隐藏时启动协程报错 | — |

> 刀 4b 第一次尝试整段搬 region 编译不过、已回退；踩坑与正确做法记在
> `plan/columnmanager-split-plan-20261020.md` §0.8，刀 5 的做法记在同文档 §0.9。

---

## 2. 当前结构快照（量化）

| 指标 | 改造前 | 现在 |
|---|---|---|
| `Enemy.cs` | 5224 行 | **933 行**（主文件：字段/生命周期/初始化/转发）；另拆出 5 个 partial 分片：状态机 1495 / 战斗结算 828 / 表现层 413 / BOSS 推进 206 / 对象池 106；真类：`EnemyHitFeedback` / `EnemyBossPhases` / `EnemyMovement` / `EnemyPoise` |
| `StageController.cs` | 852 行 | **708 行** |
| `ColumnManager.cs` | 2149 行 | **662 行**（队列/查询/入场/补齐/位移五块全部拆出） |
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
| **入场合齐**（批次 2） | 注入 `Stage_Trial` 第 1 波：入场初期物理行 = 逻辑行 + 2（owner=SpawnEntry，state=Moving）；完成后落在逻辑行 0/1/3/4/5 |
| **普通补齐基线**（批次 2） | 杀 r0 → `col1/r0 · col1/r2 · col2/r3 · col3/r4`（与拆分前**逐位一致**） |
| **补齐豁免 A/B**（批次 2） | col1/r1 置 `ignoreFillUpOrders` 后杀 r0 → `col1/r1[豁免] · col1/r2 · col2/r3 · col3/r4` |
| **击退 + 精确回位**（批次 2） | 击退 `col2/r4 → col2/r5`，同列 `col2/r0` **不动**；~1.5s 后回到**精确原槽** |
| **横向位移**（批次 2） | `col1/r3 → col2/r3`（只改 column，row 不变），其他敌人不动 |
| **破冰顶开**（批次 2） | 冻结 `col1/r1` → 顶开到 `col1/r3`（moved=True），其他敌人不动；解冻 + 登记延迟回位后逐排走回 `col1/r1` |
| **蓄力长枪 3D 表现**（2026-10-21 追加） | 蓄力视觉实例创建 ✓；枪身沿轴后拉随进度递增（-0.52→-0.94）✓；满蓄 localScale 脉动 1.044 + 暖金染色 (1.55,1.25,0.5) ✓ |
| **蓄力刺命中**（2026-10-21 追加） | 蓄力 Stab 返回 True、命中掉血 20 ✓（与普通刺同伤害，蓄力增伤属升级流） |

### 3.2 需要你人眼确认（脚本测不了）

1. **落地页观感**：头像/金币/开始关卡/底部 tab 的大小与位置、就绪态背景好不好看。
2. **界面叠放**：选关/天赋页与底部 tab 栏的视觉关系（tab 栏画在界面之上，order 50 vs 40/41）。
3. **天赋树面板与底栏是否重叠**（`TalentTreePanel` 是全屏铺满的，可能压到底栏区域）。
4. **两个界面的「返回」按钮**：现在它们与 tab 栏并存（点返回 = 回战斗界面）。若你倾向纯 tab 导航，可删掉这两个按钮。

### 3.3 明确未覆盖（别当成已验证）

- **真实手打到通关**（结算用 `FinishRoute` 驱动，不是手打）——这是 §4 里最该验的一条。
- 战败重来（只做了代码路径确认，没实机点过「重来」）。
- 冻结→破冰后敌人停在第 1 排时 Slash 打不到：属既有「打前排」命中规则，**不是**本次改造引入。
- 包体：字体 36MB、`Enemy_1`/`Enemy_102`（~2.4MB）、敌人贴图 —— **按你的要求整体延后，尚未动手**。
- 小程序真机（微信开发者工具）未跑。
- 卷轴场景的**视觉连续过渡**：文档明写「不能宣称已完成」（`plan/latest-todolist.md`）。

---

## 4. 当前可验收清单（面向你：人眼 / 手操）

> 脚本能验的我都验过了（§3.1）。下面这些**要么是观感、要么必须手操**，按性价比排序。
> 每条都写了「在哪验 / 怎么验 / 看什么」。

### 4.1 第一梯队：刚搬完家的战斗内核（最该验）

> 批次 2 把「入场 / 普通补齐 / 击退回位 / 横向位移 / 破冰顶开」的实现从 `ColumnManager`
> 搬进了 4 个新类。脚本**逐位一致**（§3.1），但**手感与观感只能你玩**。
> 验收口径与脚本见 `plan/columnmanager-split-plan-20261020.md` §5。
>
> **在哪验**：正式关卡「**长坂坡·卷轴节点预览**」（stageId=1，6 个节点 N1/E0/J1/EV/N2/N3）。
> ⚠️ 选关里它**默认被锁**（解锁规则 = 已通关数+1，教学关未通关就锁死），
> 临时绕开见 `plan/bugs-and-requests.md` BUG-002。

| # | 验什么 | 怎么验 | 看什么 |
|---|---|---|---|
| 1 | 普通补齐 | 正式关卡 N1 节点，清光前排 | 后排是否整排**整齐**补到前排、不重叠、不卡在半路 |
| 2 | 击退 + 精确回位 | Stab 打到后排敌人 | 它后退后**自己走回原格**（不停在半路）；同列其他人不动 |
| 3 | 横向位移 | Slash 方向推 | 只有**被打中的人**横移；未打中的人不动；不引发额外补齐 |
| 4 | 破冰顶开 | 冰系天赋/冰伤打冻结敌人 | 冻结敌人被顶后 + **延迟回位**；旁边敌人不动 |
| 5 | 新波次入场 | 节点内出下一波 | 敌人从**后方两排**走上目标排，不是凭空出现 |
| 6 | BOSS 墙 | 打到有 BOSS 的关卡 | 普通敌人**不会被推到 BOSS 身后**（BOSS 排是墙） |

### 4.2 第二梯队：已实现、但一直没有验收记录

| # | 验什么 | 在哪 / 怎么验 | 备注 |
|---|---|---|---|
| 7 | 教学关：导演重写 + 手势门槛 + 招式解锁 + 冰系天赋 | 教学关从头打一遍 | 提交 `39ac81b`，**无验收记录** |
| 8 | 可破坏道具 | 关卡里的木桶/木箱等 | 提交 `e036ad2`；本轮才进仓库（之前 HEAD 是坏树） |
| 9 | 暂停菜单三档音量 | 暂停菜单 | 总音量/背景/音效独立调节 + **退出重进后设置保留** |
| 10 | 命中反馈节奏 | Stab / Slash / Pierce / Launch / Parry / 海浪 / 旋风 / 共享血量 / 连续箭雨 | 卡肉、闪白、伤害数字分级 |
| 11 | 灼烧 / 染病状态条 | 给敌人上 DoT | 普通敌人头顶条；Boss Poise 条下方同尺寸条；疾病紫条优先 |
| 12 | QTE 翻牌视觉与收尾时序 | BOSS 战 | 固定 0°↔180°；结束后**不异常补翻** |
| 13 | 1021 HitRight | 第一关打 1021 | 已接入，**待游戏验收** |
| 14 | 109 骑手落马 / 逃脱 | 第一关打 109 | 12 帧(T79) + 10 帧(T80) 已部署 |

### 4.3 第三梯队：素材 / 图（看图，不进游戏）

| # | 验什么 | 在哪 |
|---|---|---|
| 15 | J1 场景 v6 美术 | `plan/j1-three-kingdoms-scenery-v6-reference.md`（构图已认可，三国景物画风待确认；桶箱残留）；**未部署 Unity** |
| 16 | 1021 胆怯受击 v4 视频 | `plan/enemy1021-art-production-handoff.md`，待看片 |

### 4.4 先别验（已知缺陷，验了会误判）

- **正式关卡默认锁死**（已修 `a45e57b` 之后）：旧规则「已通关数+1」会让教学关未通关时正式关不可选；
  `StageSelectUI.ResolveUnlock()` 现保底放行前两条（教学关 + 第一个正式关）。详见 `plan/bugs-and-requests.md` BUG-002。
  落地页仍会从当前路线的存档检查点续跑（正常续玩语义，未改）。
- **QTE 反击无法触发**：✅ **2026-10-21 实测已不复现**（首次 `SelectBossAction()` 即进入 `PerformingQTEAttack`）。
  三个 Boss prefab 的 `QTEController` / `qteData` / `qteWeight` 均正常；原文的 `OnBossEngaged` 排查方向是错的。
  详见 `plan/unresolved-issues.md` TODO-1。
- **箱桶重挑战不重置**：`plan/bugs-and-requests.md` BUG-001，用户已示意先放放。
- **包体 / 字体缺字**：方案已有（`plan/package-size-optimization.md`、`plan/font-optimization-20261020.md`），**尚未实施**。
- **小程序真机**：未跑。
- **卷轴场景视觉连续过渡**：文档明确写着不能宣称已完成。

### 4.5 本轮追加的改动（2026-10-21，均已验收）

| 改动 | 内容 | 状态 |
|---|---|---|
| **蓄力长枪表现 3D 化** | `ChargeStabVisual` 的蓄力进度可视化从「换 2D 精灵帧」改成「3D 模型局部后拉 + 满蓄脉动 + 暖金染色」；新增 4 个 Inspector 参数（`meshChargePullback` / `meshChargedTint` / `meshReadyPulseScale` / `meshReadyPulseInterval`）。**2D 路径保留**：prefab 带 `SpriteRenderer` 时仍走换帧。残影按用户要求**先不接入** | ✅ 已验（见 §3.1） |
| **蓄力玩法默认开启** | `InputManager.restrictChargedToVertical` 由 true 改为 **false**（代码默认值 + `Battle.scene` 序列化值）。此前它恒为 true，会让**站桩蓄力（穿刺）/ 横滑蓄力（横扫）整条路径不成立**，蓄力指示器、穿刺瞄准指示器、蓄力武器视觉**全部被跳过** | ⚠️ 已开，但 Tooltip 标注的「蓄力 stab / 蓄力 slash 还没做完」现在暴露给玩家 → **需实机确认出招与命中手感** |
| **存档注入「冰雾」** | `active_ice_mist`（向身前喷射扇形冰流、叠加冻结概率）写进长坂坡检查点的 `activeSkills`，开局即带（V2 主动技能已启用，容量 4）。**仅本机存档**，不是代码改动 | ✅ 已验 |
| 附带清理 | Unity 保存场景时清掉了 3 个 **Legacy 悬空引用**（`routeStageConfig` / `routeStageConfigV2` / `routeStagesV2`，指向已删除资产，所属类均在 `Assets/Scripts/Legacy/` 且已隔离） | 无风险 |

---

## 5. 已知遗留与风险

| 项 | 说明 |
|---|---|
| `StageController` 仍有 5 个只给 Legacy 用的入口 | 刻意不引用 Legacy 类型；Legacy 仍是孤岛 |
| Legacy 不能用 Editor-only asmdef 剔出包 | asmdef 程序集**不能引用 Assembly-CSharp**；要真剔除，前置是先把共享类型搬进独立程序集 |
| `Assets/Scripts/Prop/`（BreakableProp）在本轮才进仓库 | 之前 HEAD 是**坏树**（clean clone 编译不过），`e036ad2` 已修 |
| `Enemy.cs` 仍有 4496 行 | 描边/冰系视觉/冰系流程已拆出；剩下的是状态机 + 战斗行为，**拆了收益下降、风险上升** |
| 工具通道偶发异常 | 本轮遇到 `unity_execute` 连续返回空、一次回显混入非脚本文本、两次 `edit` 回执未落盘。处置见 `memory/tool-result-verification.md`（已补两条纪律） |
| 截图工具不能做 UI 像素验证 | 仍以状态回读 + 人眼为准 |
| Play Mode 脚本里的游戏时间偏慢 | `ctx.WaitFrames` 推进的游戏内时间远慢于帧数（实测 180 帧 ≈ 0.4s）→ 测试要「等状态」而非「等帧数」 |
| 攻击命中是**延迟结算** | 命中不是调用瞬间生效，等固定帧数常读到「伤害 0」的假象（实为结算还没到）。判据：按「血量变化」等，而不是按帧数 |

---

## 6. 后续选项（按风险从低到高）

| 选项 | 内容 | 风险 | 备注 |
|---|---|---|---|
| A. 收尾小事 | 删两个界面的「返回」按钮；调天赋树面板底部留白 | 低 | 纯 UI |
| B. 包体 | 字体 35.4MB / `Enemy_1`+`Enemy_102`（2.4MB）/ 试玩场景剔除 | 低-中 | 阶段 E 已实测过数据；你之前要求延后 |
| C. `EnemyFreeze` 流程拆分 | 冰系**状态机**（已从 Enemy 拆出，✅ `34b6bc5`） | — | 已完成，无需再做 |
| D. `ColumnManager` 拆分 | 2149 → **662 行**；刀 1/2/3/4/5/6 **全部完成**（批次 2 收官） | 中高（已落地） | 承载「位移三原则」；做法与验收见 `plan/columnmanager-split-plan-20261020.md` §0.8/§0.9 |
| E. 共享类型独立成程序集 | 为「Legacy 剔出包」铺路 | 中 | 需先给 `Enemy`/`PlayerState` 等定程序集边界；也是 B 的前置 |
| F. 终态删 `FillUpRule` 枚举与字段 | ✅ 已完成（`86832d4`）：补齐规则只剩一种，AI 无从选错 | — | 已完成 |
| G. `Enemy.cs` 继续拆 | ✅ 部分完成：真类拆出 `EnemyHitFeedback` / `EnemyBossPhases` / `EnemyMovement` / `EnemyPoise`；主体 4177 → **933 行**（另 5 个 partial 分片，最大 1495）。**剩余**：只建议拆「护盾结算」→ `EnemyShield`（见 §14 的契约分析），`TakeDamage` 主体保留 | 中 | 刀 A–E1 的做法见 §10–§14 |

---

## 7. 包体现状（2026-10-20 重新实测，改造后）

> 口径：build settings 里启用的 4 个场景 + `Assets/Resources` 全部资产为根，
> `AssetDatabase.GetDependencies(roots, true)` 求并集，**按磁盘体积**统计（上界）。
> 真实包体取决于导入设置与平台转换，两者差异很大（见下）。

并集：**1219 个资产 / 270.5 MB**（磁盘上界）。按目录：

| 目录 | 数量 | 磁盘体积 | 会不会进包 |
|---|---|---|---|
| `Assets/Sprites/Enemy/` | 417 | **117.9 MB** | 会（405 张真在用） |
| `Assets/Experiments/`（YJunctionSample 为主） | 116 | 59.0 MB | 会（它是 build 场景） |
| `Assets/Sprites/` 其它 | 202 | 35.8 MB | 大部分会 |
| `Assets/Fonts/` | 1 | **35.4 MB** | 会 |
| `Assets/SFX/` | 18 | 14.7 MB | 会 |

### 7.1 三个可下手的点（按收益排序）

| # | 现状 | 根因 | 预估可省 | 动作 | 风险 |
|---|---|---|---|---|---|
| 1 | 敌人贴图 417 张 117.9MB，其中 **292 张 max2048 且导入设置为 Uncompressed**（85.7MB），另 123 张 max512 也未压缩 | PNG 未压缩 + 平台未转码，**包体里会变成原始像素**（2048²×4 = 16MB/张的运行时纹理） | **60-80MB**（包体口径） | 批量把 `Assets/Sprites/Enemy/` 的导入设置改为对应平台压缩格式（Sprite 建议 DXT5/ETC2/ASTC，按目标平台定） | 低（画质有损，需人眼看） |
| 2 | 字体 35.4MB = `方正粗黑宋简体 SDF.asset`：**4096×4096 静态图集 + 7229 字形**（内嵌 `image data` 16.7MB + mipmap + 字形表） | 静态图集一次性烘焙全部字形 | 20-30MB | 两个方向：(a) 按**运行时实际渲染**的字符集重新生成（需先做运行时字形普查）；(b) 改动态字体（运行时生成图集，需在微信小游戏上验性能） | 中（漏字会显示方块） |
| 3 | SFX 18 个 wav / 14.7MB | 未压缩音频 | 5-8MB | 导入设置改压缩（Vorbis/ADPCM），或转 `.ogg` | 低 |

### 7.2 顺带发现：现有字体可能缺字

扫 `Assets/` 下 595 个文本文件（.cs/.asset/.prefab/.unity/.json/.txt），项目里出现过的 CJK 字符
**10215 个**，而字体资产只有 **7229 个字形**——说明现有字体**覆盖不全**，
部分中文可能渲染为空白/方块。
（注：这个 10215 是**上限**，含注释、日志、文件名、资源描述；真实渲染字符集更小。）
→ 建议做一次**运行时字形普查**：TMP 对缺字会打日志，跑一遍全部流程收集缺字，
既能拿到准确的子集（决定第 2 项怎么做），也能把真实的缺字问题暴露出来。
> 详细记录与方案取舍见独立文档 `plan/font-optimization-20261020.md`
> （结论：**不建议做无守门的「按当前用字」静态子集化**——那是单向门，以后新增文案会静默缺字）。

### 7.3 不影响包体、但占仓库的

- 未进包且未被引用的 Assets 资产 357 个 / 104.2MB（几乎全是 `Assets/Experiments/` 下的美术源文件与 PSD）。
  它们**不占包体**，只占仓库与磁盘；要清就当仓库整理单独做。
- `Assets/Experiments/` 里有 212 个 / 78.2MB 资产当前没被任何 build 场景引用。

---

## 8. 性能实测（2026-10-21，结论：当前无 CPU 热点）

> 背景：架构重构后提出“继续优化性能”。按「先测量后优化」采了一次样，结论是
> **没有数据支撑的优化点**，不要盲改。

**采样条件**：编辑器 Play Mode · 长坂坡 N1 节点 · 4 个敌人 · 240 个样本（`WarmupFrames=30`）

| 指标 | 结果 |
|---|---|
| `main_thread_ms` | avg **6.12** · p95 6.65 · max **8.49**（预算 16.67ms / 60fps） |
| 超预算帧 | 0 |
| Console | 0 error / 0 warn |
| 产物 | `Library/Locus/RunStates/profiler-combat3-*-summary.json`（含 CSV） |

**静态测量（同一结论）**：全项目 `Update/FixedUpdate/LateUpdate` 方法体内的运行时查找
（`FindObjectOfType` / `GameObject.Find` / `GetComponent*`）只有 **5 处，分布在 3 个方法**
（其中 2 处在 `Assets/Scripts/Legacy/`）—— 不是热点。

**局限（别当成已验证）**：

- 只覆盖编辑器、轻负载（4 敌人）；**未覆盖** Boss 战多特效峰值、大批量刷怪、真机（Android/微信小游戏）。
- 未采集 GC 分配与 Draw Call（指标名需用 `DiscoverMetrics` 拿到的
  `Draw Calls Count` / `GC.*` 再采一轮；本轮 `gc_allocated_in_frame` 名字不存在）。
- 结论只说明「现在没必要为了性能重构」；若真机出现卡顿，应先在真机重采再动手。

---

## 14. 批次 3 刀 E：韧性拆出（`EnemyPoise`）+ 刀 E2 的契约分析

### 刀 E1（已完成）：`EnemyPoise`

> 新增 `Assets/Scripts/Enemy/EnemyPoise.cs`（117 行）；`Enemy.Combat.cs` 902 → **828 行**。
> 拆了什么：普通削韧 `TakePoiseDamage`、主动位移百分比削韧 `TakeActiveDisplacementPoiseDamage`、
> QTE 专用削韧 `TakeQTEPoiseDamage`（含破势延迟眩晕）。
> **特例**：韧性相关成员在 Enemy 里**本来就是 public**（`currentPoise` / `maxPoise` /
> `deferredStun` / `OnPoiseChanged` / `stunDuration`）→ 本刀**零 internal 化、零委托**，
> 只靠 owner 引用访问，是四刀里改动面最小的一刀。

**验证（Play Mode，实例化 Enemy_104）**：

| 项 | 实测 |
|---|---|
| 初始 | currentPoise=100（104 的 maxPoise=100） |
| 主动位移削韧 50% | 100 → **50**，`OnPoiseChanged` 事件 1 次 |
| QTE 削韧 10 | 50 → **40**，事件累计 2 次 |
| 韧性归零 | 返回 true、**deferredStun=true**、currentPoise 重置为 100（延迟眩晕语义） |
| 回归 | 主流程 phase=Battle/4 敌人；补齐基线 `col1/r0 · col1/r2 · col2/r3 · col3/r4` 逐位一致 |
| Console | 0 error / 0 warn |

### 刀 E2（**未做，动手前先读这段**）：`TakeDamage` 本体

> `TakeDamage`（~130 行）是战斗核心，**不建议整块搬**。下面是实测出来的依赖与自然缝。

**它实际依赖的东西**（跨 15+ 字段、10+ 方法）：

- 状态：`state` / `isBoss` / `bossState` / `isPhaseTransitioning` / `_healthLocked` /
  `currentHealth` / `maxHealth` / `formState` / `sharedHealthGroup` / `_deferredPhaseTransition` /
  `bossPhases` / `currentBossPhase` / `isAttackAnimating` / `isAttackDrawPhase` /
  `isSuperArmor` / `isCFrame` / `launchedDamageTakenMultiplier`
- 子系统：`_shieldGuard`（破盾）/ `IceBreakRuntime`（破冰翻倍）/ `HitFeedbackManager` /
  伤害数字 / `Die()`
- 方法：`GetDamageMultiplier` / `ResolveHitReactionDirection` / `EnterPhaseTransition` /
  `CancelAttack` / `Stun` / `Die` / `ApplyHitFlashImmediate` / `RestartHitScaleFeedback` /
  `TriggerHitFlash` / `OnDamageTaken` / `OnHealthChanged`

**按耦合度排的自然缝**（建议只拆第 3 条）：

| # | 段 | 耦合 | 建议 |
|---|---|---|---|
| 1 | 前置门控（无敌 / 锁血 / 转阶段阈值 / QTE 挂起） | 高（与 BOSS 阶段互调） | **留 Enemy** |
| 2 | 攻击打断裁决 | 高（与状态机互调） | **留 Enemy** |
| 3 | **护盾结算**（`_shieldGuard.ProcessHit` → finalDamage / blocked / broke） | 低（`_shieldGuard` 是独立组件） | **可拆 `EnemyShield`** |
| 4 | 共享血量组转交（一句 `sharedHealthGroup.TakeDamage(...)`） | 低 | 留 Enemy（一行转发） |
| 5 | 扣血 + 阈值钳制 + 死亡 | 高（与状态机/血条/Combo 纠缠） | **留 Enemy** |
| 6 | 受击反馈与伤害数字 | 已拆 | 已在 `EnemyHitFeedback` |

**结论**：刀 E2 = **只拆护盾结算**（约 30–60 行），`TakeDamage` 主体保留。
把 5 整块硬搬会造出 20+ 委托的壳，与“减 token”的目标相反。

---

## 13. 批次 3 刀 D：移动订单拆出（`EnemyMovement`）

> 2026-10-21 完成。新增 `Assets/Scripts/Enemy/EnemyMovement.cs`（232 行）；
> `Enemy.Visuals.cs` 602 → **413 行**。

**拆了什么**：RushMove 订单的派发/校验/取消/完成（owner + generation 语义）、
冻结前落位（`CompletePendingMovement`）、以及世界坐标换算（`UpdateWorldPosition`）。

**边界划分**：「什么时候发起移动」（`TryStartRushMove`）**留在 Enemy** ——
它同时牵扯 BOSS 应战分支与攻击状态机，拆开会同时牵动两块边界。
`GetRowZ` 也**留在 Enemy**（击飞静默补齐在用），改为 public 供 `EnemyMovement` 调用。

**手法**：同刀 A/B —— 状态字段留 `Enemy`（改 internal）+ owner 引用 + 同名薄转发（7 个）。

**验证（Play Mode）**：

| 项 | 实测 |
|---|---|
| 入场初期 | 物理行 = 逻辑行 + 2，owner = SpawnEntry（col1/r3 · col1/r5 · col2/r2 · col2/r6 · col3/r7） |
| 入场完成 | `col1/r1 · col1/r3 · col2/r0 · col2/r4 · col3/r5`（逐位一致） |
| 坐标换算 | col1/r1 → localPos (-1.25, 0.01, **-7.50**)，与 Z=(5-1-row)*(-2.5) 精确吻合 |
| 补齐基线 | 杀 r0 → `col1/r0 · col1/r2 · col2/r3 · col3/r4`（逐位一致）；补齐后 col1/r0 → Z **-10.14**（含 offsetZ） |
| Console | 0 error / 0 warn |

**下一步（批次 3 最后一块）**：`EnemyHealth`（韧性 / 护盾 / `TakeDamage`）——
**风险最高，放最后**：它是战斗核心，且与共享血量组、破盾、伤害数字、死亡判定纠缠。
建议先定契约（哪些字段真能搬走）再动手。

---

## 12. 批次 3 刀 C：`Enemy` 按机制 partial 分文件（**直接降 token 成本**）

> 2026-10-21 完成。动机：用户明确指出「减少上帝类，否则之后的工作都会大大消耗 token 和增添时长」。
> 真类拆分（刀 A/B）已把行为逻辑搬出；但 `Enemy.cs` 仍有 4177 行，**改任一块都得读全文**。
> 本刀用 `partial class` 把主体按机制分文件 —— **同类型、行为完全不变、零行为风险**。

| 文件 | 行数 | 内容 |
|---|---|---|
| `Enemy.cs` | 933 | 字段、生命周期、初始化、各子系统的薄转发 |
| `Enemy.State.cs` | 1495 | 状态机：状态切换 + 状态更新 |
| `Enemy.Combat.cs` | 902 | 命中帧、伤害系统、韧性/护盾/位移命中、冰系薄转发 |
| `Enemy.Visuals.cs` | 602 | 世界坐标与排位换算、透明度、描边 |
| `Enemy.Boss.cs` | 206 | BOSS 推进到应战排 / 暂停等待 / 恢复推进 |
| `Enemy.Pool.cs` | 106 | 对象池回收时的状态复位与资源清理 |

**效果**：改任一机制只需读对应文件（**最多 1495 行**，原来 4177 行）。
**做法**：`public class Enemy` → `public partial class Enemy`；按 `#region` 边界整块搬移，缩进不变；
各分片带 `using System.Collections.Generic; using UnityEngine; using DG.Tweening;` 与机制说明头。

**坑（我踩了一次）**：`#region` 可能**嵌套**（「伤害系统」内含「冻结（冰系）」），
用「找第一个 `#endregion`」配对会漏搬内层之后的内容并留下孤立 `#endregion` → 编译报错。
→ **必须用栈式配对**（深度计数）来定位 region 跨度。

**验证**：
- 静态：切分前后**方法集合逐名比对 147 = 147，零丢失零新增**；各文件 `#region`/`#endregion` 计数一致。
- Play Mode：主流程 `phase=Battle`/4 敌人；命中反馈 hitStop 0.090s、flash 0.150s、缩放 22 帧、伤害 20；
  补齐基线 `col1/r0 · col1/r2 · col2/r3 · col3/r4` 逐位一致；
  Boss 转阶段 `phase=1`、`isSuperArmor=True`、事件 1 次、状态回 Idle；Console 0 error / 0 warn。

**下一步（批次 3 剩余）**：真正**减类**的两块还剩 **韧性/护盾/伤害结算**（`EnemyHealth`，含 `TakeDamage`——
风险最高，放最后）与 **移动**（`EnemyMovement`，RushMove 订单 + 位置）。这两块继续用
刀 A/B 的「`internal` + owner 引用 + 同名薄转发」手法拆成独立类。

---

## 11. 批次 3 刀 B：BOSS 多阶段拆出（`EnemyBossPhases`）

> 2026-10-21 完成。`Enemy.cs` 4332 → **4177 行**（-155）；新增 `Assets/Scripts/Enemy/EnemyBossPhases.cs`（195 行）。
> 批次 3 刀 A+B 累计：`Enemy.cs` 4512 → **4177 行**（-335）。

**拆了什么**：当前阶段数据查询、BOSS Idle 行动加权调度（普通/C技/QTE）、转阶段的进入/演出/收尾（阶段参数套用、QTE 数据切换、`OnBossPhaseChanged` 广播）。
**没拆**：「BOSS 推进到应战排 / 暂停等待 / 恢复推进」属于行军与补齐范畴，留在 Enemy。

**同样用刀 A 的三件套**：`internal` 代替委托 + owner 引用 + 同名薄转发。
新 internal 化成员：`actionCooldownTimer` / `_healthLocked` / `_isStunExiting` / `_currentAttackStep` /
`_phaseTransitionRoutine` / `StopHitScaleFeedback()` / `UpdateOutlineState()` /
`ClearFreezeForSystemOverride()` / `BeginStunExit()`。

**验收（Play Mode，实例化 Enemy_104）**：

| 项 | 实测 |
|---|---|
| 转阶段触发 | `TakeDamage(175)` → hp 350（70%）→ `phase=1` |
| 阶段参数套用 | `isSuperArmor` false → **true**（phase1 配置生效）、`healthLocked=false`、`transitioning=false` |
| 阶段事件 | `OnBossPhaseChanged` 触发 **1** 次 |
| Idle 加权调度 | `SelectBossAction()` → `qteState=PerformingQTEAttack`、`attackIndex=0`、`enemyState=QTEAttacking` |
| Console | 0 error / 0 warn |

> 备注：脚本里直接实例化的 Boss `currentHealth` 初始为 0（`TakeDamage` 后才补齐），
> 真实流程由 `EnemyManager.RegisterEnemy` 赋血量 —— **既有行为，非本次改动引入**。

**下一步（批次 3 剩余）**：按同一手法拆 **韧性/护盾/伤害结算**（`EnemyHealth`，含 `TakeDamage`——
风险最高，放最后）、**位置与透明度**（`EnemyVisuals`）、**移动**（`EnemyMovement`）。

---

## 10. 批次 3 刀 A：打击反馈拆出（`EnemyHitFeedback`）

> 2026-10-21 完成。`Enemy.cs` 4512 → **4332 行**（-180）；新增 `Assets/Scripts/Enemy/EnemyHitFeedback.cs`（244 行）。

**拆了什么**：卡肉（HitStop）、受击缩放、闪白动画协程 + 闪白材质、挂起时的攻击位移暂停。

**关键做法（后续拆 Enemy 照抄）**：

- **状态留原地、方法搬走**：打击反馈的 15 个字段（`_hitStopRemaining` / `_hitScaleTween` / `flashMaterials` /
  `renderers` / `originalScale` / `_hitFlashRoutine` …）全部留在 `Enemy`，只把可见性 `private` → `internal`；
  这样 Enemy 内部上百处使用点**一行都不用改**。
- **`internal` 代替委托**：同程序集内 `internal` 可见 → 新类通过 `Enemy` 引用直接访问成员，
  **不需要把字段搬到新类、也不需要注入一堆委托**。这是拆 `Enemy` 的低成本路径。
- **owner 引用模式**（与 `EnemyFreeze` / `EnemyOutline` 一致）：新类持有 `Enemy _owner`，
  构造时传入 `this`。
- **同名薄转发**：Enemy 侧保留 `private void UpdateHitStop() => HitFeedback.UpdateHitStop();` 这类转发，
  现有调用点零改动。

**验收（Play Mode）**：命中后 `maxHitStop=0.074s`（卡肉）、`maxFlashTimer=0.134s`（闪白）、
受击缩放 27 帧内生效、伤害 20、收尾后 scale 与 hitStop 归位；打死敌人路径无异常；
补齐基线 `col1/r0 · col1/r2 · col2/r3 · col3/r4` 逐位一致；Console 0 error / 0 warn。

**下一步（批次 3 剩余）**：按同样手法依次拆 **韧性/护盾/伤害结算**（`EnemyHealth`）、
**位置与透明度**（`EnemyVisuals`）、**Boss 阶段**（`EnemyBossPhases`）、
**移动**（`EnemyMovement`）；每个都保持 `internal` + owner 引用 + 同名转发三件套。

---

## 9. 工作方式提醒

- 每批改造：**改码 → 编译 → 静态复查（`git diff`）→ 一次 Play Mode 跑完所有验证 → 提交**；
  不要反复进出 Play Mode（每次进出都会切碎一个回合，见 `memory/work-loop-discipline.md`）。
- 提交前必看 `plan/main-menu-hub-refactor-plan-20261020.md` §7 的 25 条坑（尤其 15 工作区脏时的
  hunk 拆分、17 untracked 引用、19 跨文件成组提交）。
- 改完必须回读目标文件确认落盘，**不要相信回执**。
