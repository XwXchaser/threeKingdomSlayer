---
id: kd_bdc5392d-acfb-446b-bdef-c84dc8f99873
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 内容开发待办：明后两天（2026-10-25 / 10-26）

> 来源：用户 2026-10-24 提出的 7 项需求。本文的目的是**需求 → 会动哪些功能**的分析 + 两天排期。
> 依据：仓库实测（脚本 / 资产 / 场景）+ 已有知识文档。**文档与代码不一致时以代码为准。**
> 相关文档：设计规则见 `design/art-style-guide.md`、系统地图见 `memory/unity-project-understanding/README.md`、
> Y 场景管线见 `plan/scene-pipeline-handoff-20261023.md`、路线/天赋域见 `memory/unity-project-understanding/stage-select-and-routes.md` 与 `talent-system.md`。

> **2026-10-25 逐条实测复核（已修正 3 处过期）** ✅
> ① **D2-1 右官道段与 Q5 左石板路其实已经建好并部署**（原文写“用户明说先不搭了”已过期）：
>    右官道表 54 件（45 精灵 + 9 mesh）、左石板路表 51 件（44 精灵 + 7 mesh），
>    段 prefab 均已登记在 `YRouteSegments`（d=[192,262]），实测 SpriteRenderer 数量与表**逐件对齐**。
> ② **D1-2 教学关接口核对已有答案**：`StageController` 仍实现 `IStageBattleState`/`IRunFlow`
>    （`Assets/Scripts/Managers/StageController.cs:17-18`），导演也已走这两个接口
>    （`Assets/Scripts/Tutorial/TutorialStageDirector.cs:76-77`）→ **代码已就绪，只差挂载后跑通验证**。
> ③ **工具路径已变**：`Tools/Y 场景规格/` 与 `Tools/数据校验/` **在本 checkout 已不存在**，
>    工具已迁至 `Assets/Scripts/Editor/Experiments/`（`YSceneSpecTools` / `YScenePlacementImporter` /
>    `YSceneTableizeWindow` / `YSampleSceneTools`）与 `Assets/Scripts/Editor/`（`TalentTreeValidator` /
>    `RouteCatalogValidator` / `CombatBuffValidator` / `EnemyGroundAlignTool` / `AttackHitFrameValidator`）。
>    → 本文中所有 `Tools/…` 路径均需按此换算。

---

## 零、总览：7 项需求 → 动的功能域

| # | 需求 | 主要动的功能域 | 依赖新美术？ | 难度 |
| --- | --- | --- | --- | --- |
| 1 | 敌人形象重置 | 敌人 prefab 骨架 / 素材导入流水线 / 演出层（描边·血条·死亡） | **是**（素材未到位） | 中（技术准备即可） |
| 2 | 新手教程完善 | `TutorialStageDirector` 挂回 + 教学数据 + 手势提示 | 否 | 中（有明确缺口） |
| 3 | 天赋树外观改造 | `TalentTreePanel` 视觉层（数据已就绪） | 是（面板美术） | 低 |
| 4 | 选关界面搭建 | `StageSelectUI` 视觉层 + 选关规则数据化 | 是（参考图未到） | 低～中（规则要等参考图） |
| 5 | N1→N3 关卡流程 | 正式路线接线 / 节点对话与奖励 / Boss 入位 | 否 | 中 |
| 6 | 右路场景开发 + 敌人搭配 | Y 场景管线（J1 右官道 + N2 段）+ `Formal_N2_Battle` | 部分（素材可先补齐） | 中 |
| 7 | 104 boss 形象重塑 | 104 美术 + 27 个 clip 重导 + 入关接线 | **是**（形象） | 中高（接线面广） |

**好消息**：3 / 4 的**数据与逻辑底座已全部就绪**（`RouteCatalog.asset`、天赋 `prerequisiteIds`、解锁规则、校验菜单），
两天里这两项主要是「换皮 + 加表现」，不是重做逻辑。

---

## 一、逐项需求分析（会动哪些功能）

### 1. 敌人形象重置（美术未到位，先做技术准备）

**现状实测**
- 视觉体系**统一是 Animator**：`Resources/EnemyPrefabs/` 下 12 个 prefab 中 11 个带 `m_Controller`（`Enemy_101/1011/1021/103/104/105/106/107/108`），
  **只有 109 骑兵没有 Animator**（`RiderAnchor` + `MountVisual` + `CavalryStateLabel` 多物件 + `CavalryDeathSequence` + `MountEscapeVisual` 专属演出）。
- `EnemySpriteController`（精灵切换的临时方案）**代码在但没有任何 prefab 在用** —— 审计已定稿「暂不换」，不要基于它做设计。
- 敌人身上的表现层（换形象时容易被漏掉）：`EnemyOutline`（描边 / 破绽脉冲）、`EnemyHealthBar`（血条宽度由 `barWidth` 控制）、
  `PixelHitEffectManager`（打击像素特效）、`SpriteSequenceEffect`（逐帧特效，1021 胆怯死亡在用）、卡肉 / 击飞 / 死亡回池（`EnemyPool`）。
- 美术基准：`design/art-style-guide.md` —— 权威参考只有两张用户概念图 + 已验收的 1011 六帧 Idle；
  1011 技术基线：画布 1108×1108、PPU 16、Point、pivot 中心、Idle 0.6s/30fps/6 帧、prefabs 在 `Resources/EnemyPrefabs/`、clip 在 `Assets/Animations/`。

**会动的功能**
1. **形象装配入口统一**：把「根精灵 + AttackTelegraphAnchor + 描边 + 血条 + 脚底锚点 + Y 偏移」做成一个可复制的 prefab 骨架，
   新敌人照骨架搭，避免每次手搓（Y 偏移已有方案：`design/visual-yoffset-system.md`）。
2. **导入流水线**：素材到位后走 `memory/sprite-video-lab-matting-evaluation.md`（抠图工作流，已验收）+ 导入参数（PPU16/pivot/画布）+ 回读校验。
3. **演出层复核**：新形象帧数/节奏变了，动画 clip 与卡肉时长、死亡演出、击飞演出要对齐（**先不动逻辑，等素材**）。
4. **反幽灵引用**：`EnemyPool` 靠 `Resources.Load("EnemyPrefabs/<名字>")` 按名加载（违反 `design/anti-ghost-reference.md`）→ 换 prefab 名会静默走 Cube fallback。

**风险**
- Prefab 资产编辑会把未落盘的内存改动刷回磁盘值（`memory/project-mistake-note.md`）→ 改 prefab 前先备份、存盘后回读磁盘。
- 素材零引用审计必须包含 `.scene`（同上前）。

### 2. 新手教程完善（在新架构上跑通「原本的教程」）

**现状实测 —— 这是当前最大的功能缺口**
- 教学关资产**齐全**：`RouteData/Tutorial/` 下 `Tutorial_Route`(9100，节点 N1→E0)、`Tutorial_N1_Battle`(9001)、
  `Tutorial_E0_Battle`(9002)、`Tutorial_Battle`(9000)、`Tutorial_Choice_N1_E0`、`Formation_Tutorial`。
- **但 `TutorialStageDirector` 没挂在任何场景 / prefab 上**（`plan/project-audit-20261024.md` 实测：按脚本 GUID 扫全部 `.unity`/`.prefab` 零命中）
  → 教学期三件事**全都没在跑**：① 按波推台词 ② 教学期固定给冰雾 ③ 手势引导（`TutorialGestureHint` 同样没挂）。
- 导演需要的数据：`n1Battle` / `e0Battle`(`StageConfig`)、`n1Dialogues` / `e0Dialogues` / `choiceDialogue` / `openingDialogue`(`DialogueEventData`)、
  `tutorialIceSkill`(`UpgradeDefinition`)、`tutorialRouteConfig`(`FakeRouteStageConfig`)、`gestureHint`、`gestureSteps`(`TutorialGestureStep[]`)。
- 新架构下它依赖的接口还在：`IStageBattleState`（`Scripts/Data/IStageRoles.cs`）、`IRunFlow`（`Scripts/Data/IStageSessionRoles.cs`）、
  路线侧 `IRouteFlow`（`Scripts/Data/IRouteFlow.cs`）—— **但要核对实现方是否已换人**（架构十二批改造后 `StageController` / `StageRunFlow` 都拆过）。
  → **2026-10-25 实测：没换人** ✅ —— `StageController` 仍声明 `IStageBattleState` / `IRunFlow`，
  导演也是用 `StageController.Instance as IStageBattleState / IRunFlow` 拿的（`TutorialStageDirector.cs:76-77`）。
  **结论：D1-2 的代码侧核对已完成，剩余风险只在挂载后运行时验证。**
- 教学关战斗内容：1011（`f303`）+ 1021（`fd03`）+ 109 骑兵（`6d00`）。

**会动的功能**
1. 挂回导演（或做成 `TutorialDirector` prefab）+ 填上面 10 项数据 → 三件事恢复运行。
2. 核对接口接线（`IStageBattleState` / `IRunFlow` 实现方）；不通则改走 `IStageRoleContext` / `StageRouteBattleHost`。
3. 教学期手势提示会 `timeScale=0` 暂停 → 与本次新增的暂停门一致（游戏内容冻结、提示 UI 继续跑），**不用额外处理**。
4. 「原本的新手教程」的原始设计（台词 / 教学点 / 节奏）目前**不在仓库里**，需要用户提供原稿或确认沿用现有 `DialogueEventData` 词表。

**验收**：教学关 N1→E0 全流程跑通；按波台词、冰雾、手势提示三者都出现；Console 0 error。

### 3. 天赋树外观改造

**现状实测** —— `Assets/Scripts/UI/TalentTreePanel.cs`（444 行）**运行时用代码生成 UI**：
- 节点：圆形 Sprite（`node.icon` 为空时用 `GetCircleSprite()` 运行时生成）+ TMP 名称 + 铜钱数；位置用 `node.treePosition`（**已是数据驱动** ✅）。
- 连线：按「边」画（`prerequisiteIds` → 支持分叉 ✅，`fb6af69`）。
- 面板骨架：`dimBackground` / `panelBackground` / `connector` / `coinIcon` / 一组裸 `Color` 字段。
- 数据侧：`TalentTreeConfig` + `TalentNodeConfig`（`prerequisiteIds` / `icon` / `treePosition` / `coinCost`）+ 校验菜单 `Tools/数据校验/天赋树`。
- 场景：`Assets/Scenes/TalentTree.scene`（overlay，Canvas order 41）。

**会动的功能**
1. **美术资源**：面板底、节点框（三态：锁定 / 可解锁 / 已解锁）、图标、连线样式、铜钱图标 —— 素材未到位，先留引用位。
2. **表现层补强**：节点三态视觉（现在只有颜色区分）、连线按解锁状态变色（已有色字段，需按状态切）、选中态 / 悬停态 / 不可点反馈。
3. **大树的浏览**：节点变多后需要滚动 / 缩放（当前是固定 `nodeSize` + 居中锚点，无滚动）。
4. **可能需要的字段**：节点分组 / 层（树状层级视觉）、描述文本从 formatter 生成改成美术文案位。
5. 注意：`TalentTree.scene` 的 overlay 画布是 Overlay 41，tab 栏在 50 —— 改层级时别把 tab 栏盖住。

### 4. 选关界面搭建（等参考图定规则）

**现状实测** —— `Assets/Scripts/UI/StageSelectUI.cs`（229 行）**运行时用代码生成网格**：
- `GridLayoutGroup` 200×80 的方块按钮 + `LegacyRuntime.ttf` 文本；颜色全是脚本内裸 `Color`；无精灵（与「UI 一律配精灵」的项目规矩冲突）。
- 场景 `Assets/Scenes/StageSelect.scene` 只有 Canvas + 全屏底 + 标题 + 状态文字 + 关闭按钮。
- **规则已经数据化** ✅：`Assets/RouteData/RouteCatalog.asset`（`stages` + `alwaysUnlockedStageIds` + `prerequisiteStageIds`）、
  `FakeRouteStageConfig.prerequisiteStageIds`、解锁判定走 `catalogData.IsUnlocked`（旧的位置计数只作兜底）；校验菜单 `Tools/数据校验/关卡清单`。
- 当前只有 **2 条路线**（教学关·初见 9100 / 长坂坡·卷轴 1）。

**会动的功能**
1. **参考图规则 → 数据**：关卡卡片要显示什么（进度 / 难度 / 星级 / 解锁条件 / 奖励预览）、点选行为（现在 = 写档 + 立刻 `PrepareForEntry` + 关页）、
   是否需要二级详情页 —— 这些都要等参考图，先把可配的部分（卡片文本、图标、前置说明）留成字段。
2. **换皮**：网格 → 美术化列表/卡片；颜色 → 皮肤资产；字体 → TMP（现在还在用 Legacy Text）。
3. **`RouteCatalog` 仍是 MonoBehaviour**（关卡清单住在场景里）→ 数据模块化计划里要改成 SO 资产；改外观时顺手做掉，避免二次返工。

### 5. N1→N3 关卡流程

**现状实测**
- 正式路线资产齐：`RouteData/ChangbanpoScroll/` 下 Nodes `N1/E0/J1/EV/N2/N3`、Choices `N1_E0 / J1_EV / J1_N2 / EV_N3`、
  Travels 5 条、Battles `Formal_N1(1101) / Formal_E0(1102) / Formal_N2(1103) / Formal_N3(1104)` + `Test_Node_Battle(1199)`。
- **已知断点 / 缺口**：
  1. N1 的第 0 条战斗还是开发用 `Test_Node_Battle`（血量×0.25/伤害×0.15），正式战斗排在第 1 条 → **待拍板删不删**。
  2. **J1 / EV 节点没有战斗配置** → 纯过场节点（分叉点与村落接近）。
  3. **整条正式路线没有任何 Boss**：104 只出现在旧测试资产 `Resources/StageConfigs/{Stage_Trial, testStage, QTE_V2_TestStage}`。
  4. 所有节点 `entryDialogue` / `postBattleDialogue` 为 null、`rewards: []` → 无演出对话、无节点奖励（`design/gameplay-pillars.md` 记为「节点奖励未发放」）。
  5. 敌人配置：N1=1011；E0=1021；N2=1011+1021+109；N3=1011+1021。
- 运行时：`FakeRouteRuntime` 已拆 Prepare/Run；`DevSkipBattleButton` 可跳过本节点战斗（开发用）→ 跑流程时很有用。

**会动的功能**
1. 跑通 N1→E0→J1→EV→N3，逐节点标断点（对话 / 战斗 / 奖励 / 下一节点 / 结算）。
2. 补节点对话与奖励（对话走 `DialogueDatabase` / `DialogueEventData` 词表；奖励要确认发放链路）。
3. Boss 入位决策（见第 7 项）→ 决定放 N2 还是 N3 还是独立节点。
4. 跑流程时的工具：跳过战斗按钮 + `SaveManager` 存档备份还原（验证会污染 PlayerPrefs）。

### 6. 右路场景开发 + 敌人搭配

**现状实测**
- 右路 = J1 分叉后 `Choice_J1_N2`（displayName「右路：官道」）→ `Node_N2`「N2·官道断后（流程预览）」，`isFinalNode=1`，
  战斗 `Formal_N2_Battle`（1011 多数 + 1021 + 109 骑兵）。
- **Y 场景侧**：已有规格 `Right Official Road - Transport and Supplies`、`J1 Side Vegetation and Right Road Detail`、
  `Left Stone Lane - Fences and Low Vegetation`；**2026-10-25 复核：这两段（左石板路 / 右官道）已经建好并部署**
  （段 prefab 已登记在 `YRouteSegments`，实测与摆放表逐件对齐），**N2 段本身仍没有任何 YSceneSpec**。
- **工具已就绪**（不用重写）：摆放表 JSON → 规格 `.asset` → 段 prefab → 场景实例；
  工具在 `Assets/Scripts/Editor/Experiments/`（`YSceneSpecTools` / `YScenePlacementImporter` / `YSceneTableizeWindow` / `YSampleSceneTools`）；
  往返验证（逐件差异 0）+ 画面逐像素比对 + 三方对齐 + 表化工具。
- 敌人搭配：`Formal_N2_Battle` 的 waves 可重排（官道题材：骑军 109 + 步卒 1011 + 盾兵 1021 + Boss）。

**会动的功能**
1. ~~J1 右官道 55 件（+ 左石板路 52 件）~~ ✅ **已完成（2026-10-25 复核）**，本项无需再排。
2. N2 段新建（官道断后场景）→ 摆位表 → 规格 → 段 prefab → 接进 `YRouteSegments`。**未开始**。
3. `Formal_N2_Battle` 敌人搭配表 + wave 数调整。**现状：混编已在（1011+1021+109），但只有 2 波、大量空排**，waves 重排与波数扩充未做。
4. 段的美术素材缺口要盘点（`design/art-style-guide.md` §6 的三层空间要求）。

### 7. 104 boss 形象重塑（与第 1 项一起做）

**现状实测**
- `Assets/Animations/` 里已有 **27 个 104 clip**：Idle / Walk / Attack / CAttack / Dead / HitFlash / Launched_Rise|Fall /
  Phase2|3Transition / Stun 系列 / QTE_Sweep 系列 / QTE_TripleStab 系列 + `Boss_104.controller`；
  `Resources/EnemyPrefabs/Enemy_104.prefab` 已接 Animator + `AttackTelegraphAnchor`。
- **但 104 不在任何正式关卡里**（只出现在旧测试 StageConfig）。
- boss 专属功能面：`BossPhaseData` / 阶段推进与锁血转阶段、`BossHealthUI` 血条、`EnemyOutline` 描边、
  QTE 演出（`QTEController` + `Enemy.QTEAttack`）、`Enemy.Boss.cs`。

**会动的功能**
1. 美术换新（当前形象来自旧 `Sprites/Enemy/BOSS1/`，按画风指南属于「旧素材」）。
2. **动画资产重导**：新形象的帧数与节奏变了 → 27 个 clip 要跟着重导（这是形象重塑里最花时间的一项，不是画图本身）。
   现有 27 个 clip 在 `Assets/Animations/`（`Boss_104.controller` + Idle/Walk/Attack/CAttack/Dead/HitFlash/
   Launched_Rise|Fall/Phase2|3Transition/Stun/QTE_Sweep/QTE_TripleStab 系列）——**但没有重导流程与参数表**（D2-5 未做）。
3. **入关接线**：battle 配置 + boss 血条 + 阶段配置 + QTE + 描边 + 入场演出。
4. prefab 名与 `EnemyPool` 的 `Resources.Load` 名字要对上。

---

## 二、跨项共用：两天里要顺手固化的地基

| 地基 | 为什么 | 动作 |
| --- | --- | --- |
| **表现样式数据化** | 3 / 4 都要换皮；若颜色/尺寸还写死在代码里，美术来了要改代码 | 选关 + 天赋的样式抽成 ScriptableObject 皮肤；卡片/节点布局参数化 |
| **暂停语义统一** | 本轮已落地 `GameplayPause`（暂停时游戏内容冻结、UI 继续跑） | 教学手势提示 / 三选一 / 对话都走同一套，**不要再各写各的 timeScale=0** |
| **敌人素材流水线** | 第 1 / 7 项批量换素材时反复用 | 抠图 → 导入（PPU16/pivot）→ prefab 替换 → 回读 → 回归；登记进 `memory/` |
| **Y 场景管线** | 第 6 项直接用 | 摆位表 → 规格 → prefab → 场景；每步跑往返验证 + 逐像素比对。**工具路径已迁**：`Tools/Y 场景规格/` → `Assets/Scripts/Editor/Experiments/`（`YSceneSpecTools` / `YScenePlacementImporter` / `YSceneTableizeWindow`）；`Tools/数据校验/` → `Assets/Scripts/Editor/`（三个 Validator） |
| **存档兼容** | 选关解锁规则、天赋解锁、关卡进度都写 PlayerPrefs | 改结构前备份存档；天赋存 nodeId（安全）；选关解锁改成前置 id 后要迁老存档的 clearedCount 语义 |
| **验证纪律** | 本机 Editor Play Mode 帧率极低（2–5fps），`WaitFrames(10)` 只推进 2–3 帧 | 按帧数规划等待；进出 Play Mode 批处理（一次进、一次出跑完所有验证项） |

---

## 三、明天（10-25）待办 —— 不依赖新美术，先把「没在跑的」和「流程断点」清掉

| # | 任务 | 交付物 | 验收 |
| --- | --- | --- | --- |
| D1-1 | **教学关挂回**：`TutorialStageDirector` 挂到 Battle 场景或做成 prefab，填 `n1Battle/e0Battle/dialogues/gestureHint/gestureSteps/tutorialRouteConfig` 等数据 | 教学关可跑 | N1→E0 全流程通；按波台词 / 冰雾 / 手势提示三者都出现；Console 0 error |
| D1-2 | **教学关接口核对**：`IStageBattleState` / `IRunFlow` 在新架构下的实现方是否还对得上 | ✅ **已核对（2026-10-25）**：实现方未换人，代码侧就绪 | `StageController` 仍实现两接口；导演走接口拿而非 `FindObjectOfType` |
| D1-3 | **N1→N3 跑通 + 断点清单**：走一遍 N1→E0→J1→EV→N3 | 断点清单（带文件与行号） | 每个断点都有归属与修法 |
| D1-4 | **N1 第 0 条测试战斗**：删 / 保留（待拍板） | 配置改完 | 正式 N1 战斗是第 0 条 |
| D1-5 | **节点对话与奖励占位**：给 N1/E0/J1/EV/N2/N3 补 `entryDialogue` / `postBattleDialogue` / `rewards` | 资产改完 | 进节点有台词、出节点发奖励 |
| D1-6 | **外观改造地基**：选关/天赋的样式与布局参数抽成可配资产（先不改美术） | 皮肤/布局资产 + 代码接线 | 改一份配置就能换网格尺寸/卡片样式/节点三态视觉；不写代码 |
| D1-7 | **敌人形象装配清单**：写出「一个敌人形象需要哪些槽位」并落成 prefab 骨架模板 | 骨架 prefab + 清单文档 | 新敌人照骨架搭：根精灵 + 攻击预警锚点 + 描边 + 血条 + 脚底锚点 + Y 偏移 |

## 四、后天（10-26）待办 —— 右路落地 + 敌人/Boss 准备

| # | 任务 | 交付物 | 验收 |
| --- | --- | --- | --- |
| D2-1 | **J1 右官道段** ✅ **已完成（2026-10-25 复核）**：表 54 件（45 精灵 + 9 mesh）→ 段 prefab 已建并登记在 `YRouteSegments`（d=[192,262]），实测逐件对齐 | 段已进场景 | 往返逐件差异 0（表 54 = prefab 45 精灵 + 9 mesh）✅ |
| D2-2 | **N2 段新建**（官道断后）：素材盘点 → 摆位表 → 规格 → 段 prefab | N2 段 | 同上 |
| D2-3 | **右路敌人搭配**：`Formal_N2_Battle` 的 waves 重排（109 骑军 / 1011 步卒 / 1021 盾兵 / Boss？） | 战斗资产 | 通关节奏合理、不卡壳（跑一遍） |
| D2-4 | **104 入关接线**：battle 配置 + boss 血条 + 阶段 + QTE + 描边 + 入场演出 | 104 能在正式关卡出现 | 104 在关卡里完整跑一遍（阶段切换 / QTE / 死亡） |
| D2-5 | **104 形象重塑的技术准备**：把 27 个 clip 的重导流程写成可执行步骤 + 画布/PPU/锚点参数表 | 流程文档 + 参数表 | 拿到新素材后能按表批量重导，不用重新摸索 |
| D2-6 | **敌人素材流水线固化**：抠图 → 导入 → prefab 替换 → 回读 → 回归，写成脚本/菜单 | 工具或 SOP 文档 | 拿一个敌人走完整条流水线试跑 |

## 五、需要你拍板 / 提供的东西（会卡住对应任务）

| # | 需要什么 | 卡住谁 | 备注 |
| --- | --- | --- | --- |
| Q1 | **选关页参考图**（规则：卡片信息、点选行为、要不要二级页） | D1-6 的最终形态、明天之后的外观改造 | 规则先落数据，美术后补 |
| Q2 | **敌人新美术素材**（哪些先做：1011 / 1021 / 109 / 104？） | D2-5 / D2-6 的实际产出 | 形象与 104 一起定，优先级由你排 |
| Q3 | **104 放在哪**：N2（右路终点）/ N3（左路终点）/ 独立关卡 | D1-3、D2-4 | 影响 N1→N3 流程设计 |
| Q4 | **N1 第 0 条测试战斗删不删** | D1-4 | 一句话即可 |
| Q5 | **J1 左石板路要不要一起补**（52 件） | — | **2026-10-25 已解决**：左石板路也已建好并部署（表 51 件 = 44 精灵 + 7 mesh，段 prefab 已登记），无需再决策 |
| Q6 | **「原本的新手教程」原始设计**（台词 / 教学点 / 节奏）在哪 | D1-1 | 仓库里只有机制，没有文案；沿用现有对话词表也行 |
| Q7 | **局内补给要不要也改成「按生成表每局刷」** | 可破坏补给线 | 现在 E0 只有一只写死的箱子（`E0_Prop_Crate_01`）；改表驱动 = 局内也做一份「点位+轮次」计划（同局外那套 `SupplySpawnPlan`），每局刷出不同位置、坏掉的箱子下一局重置 |
| Q8 | **重打关卡时箱子要不要复原** | 可破坏补给线 / BUG-001 | 现状：**不复原**（局内箱子的“已破坏”写进路线快照 → 重打 E0 仍是残骸）。残骸保留是设计选择，但“越打越空”与 BUG-001 是同一件事 → 要么定“不复原”，要么在节点重进时清 |

---

## 六、风险与注意

1. **Prefab 编辑风险**：改 prefab 会把未落盘的内存改动刷回磁盘（`memory/project-mistake-note.md`）→ 先备份、存盘后回读磁盘。
2. **零引用审计**：删素材前必须连 `.scene` 一起查（历史误删）。
3. **`EnemyPool` 按名加载**：prefab 改名会静默走 Cube fallback，改名要同步 `Resources.Load` 路径。
4. **旧资产在场**：正式路线里 N1 的 `Test_Node_Battle`、节点对话全空、J1/EV 无战斗 —— 跑流程时先分清「设计如此」和「没做完」。
5. **验证污染存档**：`SaveManager` 直写 PlayerPrefs，验证前备份、结束后还原。
6. **本机 Editor 帧率极低**：Play Mode 验证按帧数规划等待，别用秒数估算；进出 Play Mode 批处理（一次进一次出跑完所有项）。
7. **工具路径已迁（2026-10-25）**：`Tools/Y 场景规格/` 与 `Tools/数据校验/` 在本 checkout 已不存在，
   实际位置见 §二「Y 场景管线」行与 §一.6 的工具清单；按旧路径找会以为工具丢了。
8. **可破坏补给线的剩余项**不在本文排期里，见 `plan/breakable-props-redesign-20261024.md` §7–§8
   （已完成 A/B/C 三批：点中本体命中、局外生成固定点位+倒计时、金币爆散表现、N1 移除→E0 部署）。
