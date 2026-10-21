---
id: kd_2b297c74-8d71-40ca-bc78-6e9575cc5d03
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# BUG 与新增小需求收集（唯一滚动入口）

> **用途**：用户不定时反馈的 BUG 与新增小需求登记在这里，之后开新对话逐条修复或实现。
> **权威进度表**：`plan/october-milestone-plan.md`（承诺节点、验收标准、决策点）。本文件只收集条目，不替代里程碑。
> **不要再往** `plan/latest-todolist.md` 追加新条目，该文件已停用为历史记录。

## 登记规则

- ID：`BUG-###` 按发现顺序递增；`REQ-###` 独立编号，两者不共用序号。
- **每条必须自包含**：新对话只读这一条就能开工，不依赖当时的口头描述或聊天上下文。
- 涉及文件/资产必须写真实路径（含 `.cs`、`.prefab`、`.asset`），便于直接定位与回读验证。
- 状态取值：`待处理` → `进行中` → `待验收` → `已关闭`。修复或实现完成后移到文末「已关闭」区，保留结论与改动文件，不删除。
- 优先级：`高`（阻塞演示或流程） / `中` / `低`（观感、打磨）。
- 涉及图片/视频生成的条目，状态先置 `待提示词审批`：按 `memory/user-preference.md`，必须先提交中英文提示词、负面提示词、参考图职责与关键参数并获得批准，之后才允许调用生成接口。

### BUG 条目模板

````markdown
### BUG-000 一句话现象
- 日期：
- 类型：BUG / 表现 | 逻辑 | 数值 | UI | 性能
- 现象：
- 复现：
- 期望：
- 涉及：`Assets/Scripts/...`
- 优先级：
- 状态：待处理
- 备注：
````

### 需求条目模板

````markdown
### REQ-000 一句话需求
- 日期：
- 类型：需求 / 战斗 | 表现 | UI | 美术 | 工具
- 动机：
- 期望行为：
- 验收标准：
- 相关：`design/...`、`plan/...`
- 状态：待处理
````

---

## 待处理 BUG

### BUG-001 可破坏道具（箱桶）在重挑战后不重置

- 日期：2026-10-21
- 类型：BUG / 逻辑
- 现象：箱桶一类可破坏道具被打破后，**重新挑战同一关卡**时箱桶仍然是破的状态（没有重建），一直保留在场景里。
- 复现：进入关卡 → 打碎一个箱/桶 → 退出（或战败重来 / 从选关重新进入同一关）→ 再次进入该关卡 → 箱桶仍是碎的。
- 期望：关卡重开时，可破坏道具应回到初始状态（未破坏），与其它场景物件的重建逻辑一致。
- 涉及：`Assets/Scripts/Prop/`（`BreakableProp` 与其生成/重置逻辑）、`Assets/Scripts/Core/ColumnManager.cs`（`ClearAllColumns` / 关卡重置入口）、关卡节点的重新进入路径（`Assets/Scripts/RouteFake/FakeRouteRuntime.cs`、`Assets/Scripts/Managers/StageController.cs`）。
- 优先级：中（观感与可玩性；不阻塞流程，但会让重复挑战的关卡“越打越空”）
- 状态：待处理（用户 2026-10-21 反馈，指示“先放放”，本轮不修）
- 备注：需先确认道具状态是「场景对象残留」还是「存档里存了破坏状态」——本轮存档里已见到 `FakeRouteStageSaveSnapshot.nodeStates`（含 `completedEntryIndices`）与 `RouteNodeBattleSaveState`，两者都可能是来源；重置时应与「节点重进」路径一起处理。

### BUG-002 落地页「开始关卡」从存档检查点起步，正式关卡在选关里被锁死

- 日期：2026-10-21
- 类型：BUG / 流程（设计待定）
- 现象（两个现象同源）：
  1. 落地页点「开始关卡」进入的是**教学关的后段节点**（实测存档 `tutorial_route` 的 checkpoint = `E0`），而不是该关卡的起点 `N1`——因为 `FakeRouteRuntime.ResolveStartNode()` 在落地页入口会去恢复存档检查点。
  2. 选关界面里**正式关卡（长坂坡·卷轴节点预览，stageId=1）被锁住**：解锁规则是“按列表顺序，已通关 N 条则前 N+1 条可选”，而教学关未通关 → 正式关永远不可选。玩家打不过教学关就**无法进入正式关卡**（死锁）。
- 复现：新存档（或教学关未通关的存档）→ 落地页 → 选关 → 正式关显示「[未解锁]」不可点；点「开始关卡」则从教学关的检查点节点（E0）继续。
- 期望：正式关卡应当有可达路径（要么教学关可完成、要么允许直接选正式关、要么提供调试直选）。
- 涉及：`Assets/Scripts/RouteFake/FakeRouteRuntime.cs`（`ResolveStartNode` / `IsHubEntry`）、`Assets/Scripts/UI/StageSelectUI.cs`（`ResolveUnlock` / `CreateRouteButton`）、`Assets/Scripts/Core/SaveManager.cs`（`MarkStageCleared` / `GetFakeRouteSnapshot` / `ClearFakeRouteSnapshot`）、`Assets/Scripts/Core/StageConfigManager.cs`（`GetSelectableRoutes`）。
- 优先级：高（阻塞正式关卡的验收与试玩）
- 状态：**待验收（2026-10-21 已修根因）** —— `StageSelectUI.ResolveUnlock()` 改为保底放行前两条（`Mathf.Max(cleared, 1)`），教学关未通关也能进第一个正式关；实测选关场景两个按钮均 `interactable=True`。之前用存档绕开的做法可继续保留，不影响。
- 备注：修前用存档临时绕开的做法（仅本机 Editor 存档）：`SaveManager.MarkStageCleared(9100)` + `SaveManager.SetActiveFakeRouteStage(1)`；回退 = 把 `clearedStageIds` 里的 9100 移除 + `SetActiveFakeRouteStage(9100)`。**现象 1（从检查点续跑）不改**：那是正常的续玩语义。待产品决策：是否继续放宽（比如全部关卡默认可选、教学关只作为引导入口）。

### BUG-003 选关界面导致崩溃退出

- 日期：2026-10-21
- 类型：BUG / 逻辑（崩溃）
- 现象：存档里**有一定节点记录**后，在主界面点「选关」会导致游戏崩溃退出。
- 复现：存档已走过一段路线（有节点/选择记录）→ 启动游戏 → 主界面点「选关」→ 崩溃。
- 期望：选关界面能正常打开（至少不崩溃）；锁定与解锁状态按 BUG-002 的新规则显示。
- 涉及：`Assets/Scripts/UI/StageSelectUI.cs`（`RefreshStageConfigs` / `CreateRouteButton` / `OnRouteSelected`）、
  `Assets/Scripts/RouteFake/FakeRouteRuntime.cs`（`PrepareForEntry` / `ResolveActiveRouteFromSave` / `RestoreCheckpoint`）、
  `Assets/Scripts/Core/SaveManager.cs`（`GetFakeRouteSnapshot` / `GetRouteStageSnapshot`）。
- 优先级：高（阻塞选关入口）
- 状态：待处理（用户 2026-10-21 反馈，明确要求**先记录、不修**）
- 备注：可疑方向（未验证）：选关点击后会立即 `PrepareForEntry` 换路线，而**有节点记录**时存档侧会走
  `RestoreCheckpoint` / `ValidateChoiceHistory` 分支（无记录时不走）——崩溃可能发生在换路线的恢复流程。
  定位需要设备侧堆栈（`adb logcat -s Tuanjie`）；编辑器里可用「先存一份有记录的存档 → 点选关」复现。

### BUG-004 打包版落地页背景图显示不正确（回归）

- 日期：2026-10-21
- 类型：BUG / 表现
- 现象：打包版（Android）**落地页背景图显示不正确**；**真机与模拟器都有**；此前打包（2026-10-15 的包）没有 → 属回归。
- 复现：装 `Build/Android/threeKingdomSlayer_full_20261021_1510.apk`（或 `_1526.apk`）→ 启动 → 落地页。
- 期望：与编辑器一致（**编辑器里已确认正常**：蓝天白云 + 远山 + 地面 + 战旗 + 底部 tab 栏）。
- 涉及：
  - `Assets/Experiments/CurvedScroll/Shaders/YSkyBackground.shader` —— 自定义 shader 里**手动翻转** `o.uv.y=1-o.uv.y`；D3D 与 GLES3 的纹理 Y 轴约定相反，这是「设备上上下颠倒」的头号嫌疑。
  - `Assets/Experiments/CurvedScroll/Authoring/YSampleScenerySkybox_v2.mat` —— `_MainTex=ValleySkybox_v2.png`（1024×1536）、`_ImageAspect=0.67`、`_VerticalOffset=-0.23`。
  - `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/N1/N1_FarBackground_v1.png` —— **SpriteRenderer 实际用的 sprite**（比例 1:2），与材质 `_MainTex` **不是同一张图**；两者比例不一致。
  - `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/ValleySkybox_v2.png` —— 导入设置 `type=Sprite` + `compression=Uncompressed` + `mipmap=False`（1024×1536, RGB24）。
- 优先级：高（首屏观感）
- 状态：待处理（**已排查到证据链，未定根因**；需要一张打包版截图才能收敛）
- 备注：**已排除**（均有实测证据）：
  ① 天空 quad 不在 `scenery[]` 数组（164 条不含它）→ 记忆里「天空被加进 scenery 读取投影全局量」那条坑**没复发**；
  ② `_VerticalOffset=-0.23` 与真机验收值一致、git 上从未被改；
  ③ 天空 quad 参数与旧场景完全一致（`scale=(22,33,1)`、`pos=(0,5,300)`、`active=1`）；
  ④ 材质 `_MainTex` 引用**未断裂**（能解析到 ValleySkybox_v2.png）；
  ⑤ `7198b85`（2026-10-17）把 `cover.y` 改成常量 `1` —— 该改动只在屏幕比例 > 0.667（平板/横屏）时才有差异，手机竖屏 0.45 下与旧值**完全等价** → 不是它。
  **待查**：a) SpriteRenderer 的 sprite 与材质 `_MainTex` 是两张图、两个比例，shader 到底采样哪一个；
  b) 贴图导入为 Sprite + Uncompressed + 无 mipmap 时，GLES3 上的采样/压缩行为与编辑器（D3D11）不同；
  c) shader 手动 `o.uv.y=1-o.uv.y` 在 GLES3 上方向相反（`memory/android-build-state.md` 2026-10-15 记过同一线索，当时以「真机已验收通过」结案）。
  **下一步需要**：一张打包版落地页截图（或描述是「缺失 / 上下颠倒 / 拉伸变形 / 位置错位」中的哪一种）→ 可直接判定是 a/b/c 哪一类。

### BUG-005 重新进入游戏从「检查点节点」开始，而不是关卡的初始节点

- 日期：2026-10-22
- 类型：BUG / 流程（语义待定）
- 现象：玩家**重新启动游戏**（落地页 → 开始关卡）时，不是从该关卡的**初始节点**开始，而是从存档里的**检查点节点**开始。
  实测：正式关「长坂坡·卷轴节点预览」的存档检查点为 `N3` → 重启后直接落在 N3（村落搜救），跳过了 N1/N2。
  Console 佐证：`[FakeRoute] 落地页自举：当前关卡 = 长坂坡·卷轴节点预览` → `[FakeRoute] restored checkpoint node=N3` → `[FakeRoute] 落地页就绪：… 起始节点=N3（等玩家点开始关卡）`。
- 复现：进正式关 → 走到 N3（或任意非初始节点）→ 退出游戏 → 重新启动 → 落地页点「开始关卡」→ 直接从 N3 开打。
- 期望：**重新进入游戏应从关卡初始节点开始**（N1），而不是从上次退出的节点续跑。
- 涉及：
  - `Assets/Scripts/RouteFake/FakeRouteRuntime.Entry.cs` —— `ResolveStartNode()` 第 144 行：
    `bool tryRestoreCheckpoint = !useTestStartNode && (FakeRouteLaunch.StartFromCheckpoint || IsHubEntry);`
    落地页自举（`IsHubEntry`）时**无条件**尝试恢复快照，于是 `startNode` 被换成 `restoredNode`；
    同一文件 `BeginRouteNextFrame()` 第 167 行是「老路径」（只看 `FakeRouteLaunch.StartFromCheckpoint`）。
  - `Assets/Scripts/RouteFake/FakeRouteRuntime.cs` —— `PrepareForEntry` / `RestoreCheckpoint`（`Assets/Scripts/RouteFake/FakeRouteRuntime.Checkpoint.cs`）。
  - `Assets/Scripts/Core/SaveManager.cs` —— `GetFakeRouteSnapshot` / 快照写入与清除。
  - `Assets/Scripts/UI/BattleStartStageButton.cs` —— 落地页「开始关卡」→ `FakeRouteRuntime.RunPrepared()`。
- 优先级：高（玩家每次重进都跳过前段内容；也是「关卡内容看不到」的根因之一）
- 状态：**已修（2026-10-22）** —— 根因：`FakeRouteRuntime.Entry.cs` 的 `ResolveStartNode()`
  里「落地页自举（`IsHubEntry`）**无条件**恢复检查点」✗，于是起始节点被换成上次退出的节点（实测 `N3` ✗）。
  修法：**只认显式的「续玩」标记**（`FakeRouteLaunch.StartFromCheckpoint` ✗）✅，
  落地页自举一律从 `routeConfig.startNode`（初始节点 ✅）开始 ✅。
  验证（Play Mode，落地页 → 点「开始关卡」）：当前节点 = **N1**（= 初始节点 ✅）✅、
  玩家血量 100/100 ✅、等级 0 ✅（干净开局 ✅）。
  **连带修的「血量不正确」**：检查点快照存的是**绝对血量** ✗，换英雄 / 改配置 / 被脏数据污染后就会失真 ✗
  （实测出现过「快照血量 9999 / 最大血量 100」✗）→ 快照新增 `maxHealthAtSave` 字段 ✅，
  恢复时**按比例**换算并钳到当前最大值 ✅（实测 9999 → 100/100 ✅）。
- 备注：
  - 想要「续玩」请走**显式入口**（设置 `FakeRouteLaunch.StartFromCheckpoint`）✅，不再作为落地页的默认行为 ✅。
  - **副作用**：重新进入游戏不再带回上次的血量 / 等级 / 局内升级 ✅（初始节点开局 = 状态干净 ✅）——
    这是本次要的结果 ✅，但如果之后要做「继续上次进度」按钮 ✅，得把检查点恢复做成**可选分支** ✅（引擎已就位 ✅）。

### BUG-006 N3 节点的敌人被染成全黑（黑影）

- 日期：2026-10-22
- 类型：BUG / 表现
- 现象：N3（村落搜救）节点的敌人**整个身体是纯黑的**，像一排黑影剪影，看不出是哪只兵种。
- 复现：进正式关 → 走到 N3 节点开打 → 敌人全黑（N1 / N2 / E0 节点正常）。
- 期望：N3 敌人与其它节点一样是正常贴图颜色（或与设计一致的淡红/其它染色）。
- 涉及（**根因已定位**）：
  - `Assets/RouteData/ChangbanpoScroll/Battles/Formal_N3_Battle.asset` 第 35 行：`waveTintColor: {r: 0, g: 0, b: 0, a: 0}`
    （其余节点同字段均为白色：`Formal_N1_Battle` / `Formal_N2_Battle` / `Formal_E0_Battle` 都是 `{r:1,g:1,b:1,a:1}`）。
  - 染色链路：`Assets/Scripts/Wave/WaveSpawner.cs` 第 303 行 `enemy.ApplyWaveScaling(currentWave.healthMultiplier, currentWave.waveTintColor, …)`
    → `Assets/Scripts/Enemy/Enemy.Init.cs` 第 244–249 行 `Color finalColor = _prefabColor * tint;` → `_spriteRenderer.color = finalColor;`
    → 乘上 `(0,0,0)` 即 RGB 全 0（alpha 被第 245 行改回 `_prefabColor.a`，所以不是透明，而是**纯黑**）。
  - 同一个资产的第 31–34 行还有 `healthMultiplier: 0.1` / `bossHealthMultiplier: 0` / `attackSpeedMultiplier: 0.1` / `damageMultiplier: 0.1`，
    而其它节点是 0.7~0.85 —— **疑似同一份占位配置整体没填实**（N3 敌人只有 10% 血、10% 伤害）。
  - 该资产由 `46e7c19`（补提交落地页默认路线引用的教学关/正式关数据）引入，**只提交过一次、此后未改**。
- 优先级：高（该节点完全不可读；且 0.1 倍率让该节点的战斗也失去意义）
- 状态：**待验收（染色部分已修，2026-10-22）** —— 已把 `Formal_N3_Battle.asset` 的 `waveTintColor` 改回白色
  （走 `AssetDatabase` / `SerializedObject` 改，不手改 YAML）；Play Mode 实测：N3 的 8 个敌人
  （1011 / 1021）`SpriteRenderer.color` 均为 `RGBA(1,1,1,1)`，不再是黑影；
  Trial 基线逐位一致、Console 0/0。
  **仍未决**：四个倍率（血 / 攻速 / 伤害 / BOSS 血）仍为 0.1 —— 实测 N3 敌人 maxHealth 只有 20 / 15
  （= prefab 值 × 0.1），需产品 / 数值给 N3 的口径后再改。
- 备注：
  - **修法（已执行）**：把 `Formal_N3_Battle.asset` 的 `waveTintColor` 改回白色（与其它节点一致）；若设计本意是某种染色，用设计给的颜色而不是黑。
  - **连带确认（未决）**：同一资产的四个倍率（血 / 攻速 / 伤害 / BOSS 血）是否也要按 N3 的设计重填（现在 0.1 与其它节点差一个量级）。需产品/数值给口径。

---

## 待实现需求

### REQ-001 张飞长枪换 3D 模型（含概念图）

- 日期：2026-10-07
- 类型：需求 / 美术 + 表现
- 动机：2D 精灵绕长轴自转会出现视觉变薄的副作用；换 3D 模型后同样的自转变成真正的绕轴旋转，表现更可信。
- 期望行为：长枪由 3D 模型承载，`stabRollDegrees` 的语义与取值沿用，无需改写招式参数。
- 当前美术进度：刚性枪体 V02 合图已获用户明确验收，记录在 `StabWeaponArtSource/04_BodyV02/user_acceptance.json`。红缨V02已按双参考图和批准参数单次生成、技术校验并归档 `StabWeaponArtSource/05_TasselV02/`，待最终美术验收；完整母图和V01保留为历史参考。没有找到已交付的长枪 3D 模型，`Assets/Prefabs/Stab.prefab` 仍使用原 `SpriteRenderer`。
- 素材：项目内副本 `StabWeaponArtSource/01_Reference/assembly_reference.png`、`StabWeaponArtSource/02_OrthographicV01/body/`、`StabWeaponArtSource/02_OrthographicV01/ribbon/`；原始成图在 `C:/Users/Administrator/Pictures/gptGen/stab_orthographic_refs_v01/`。枪体/飘带分别放大展示，装配比例以完整母图为准。
- 前置条件：
  - 用户验收枪体后批准制作红缨独立三视图，已生成一次。当前入口为 `plan/stab-tassel-three-view-generation-prompts.md` 和 `StabWeaponArtSource/00_Index/tassel_v02_specification.json`；检查记录披露正面右边距6px、背面左边距14px，未满足32px目标，但没有裁断。完整装配三视图仍为历史草案。
  - 红缨输入为完整新画风母图及V01红缨正面单图，职责分别为连接与比例、独立红缨轮廓；输出只保留红缨，未重新生成已验收枪体。
  - 接手时确认模型枪身长轴为 Unity 接入层本地 Y 轴，以便 `stabRollDegrees` 沿用；现代码仍依赖 Sprite 长度、换帧和模糊，需要单独适配，并非直接替换 FBX 即可运行。
- 验收标准：画面中始终只有一把可见武器；三段枪突与指向突刺的手感、衔接、命中时序不变；`stab_v13` 高速帧替换可退休，刺出速度感另找表达方式。
- 相关：`plan/stab-3d-three-view-production-handoff.md`（素材、真实进度与后续制作入口）、`plan/combo-progress-and-next.md` §6、`design/combo-zhangfei-stab3-launch.md`、`plan/stab-combo-video-reference-workflow.md`
- 当前补充任务：V5缺左侧，用户要求前／左／右／后单图及合图。V06单次補左侧并完成交付 `StabWeaponArtSource/12_AssembledFourViewV06/`：四张单图、无字合图、中文标注版和trace。前右后沿用V5原文件，左侧整体等比对齐，左／右枪尖枪尾高度差0px；原图与合图像素校验通过。左側红缨与遮挡仍待美术验收。入口 `plan/stab-assembled-four-view-v06.md`。
- 2026-10-21 追加：3D 枪已接入**蓄力视觉**（`Assets/Prefabs/FantasySpear3D.prefab` → `ChargeStabVisual.stabPrefab`）。蓄力进度可视化改为 3D 表现（枪身沿轴后拉 + 满蓄脉动 + 暖金染色，参数在 `ChargeStabVisual` 的「3D 蓄力表现」分组）；原 2D 精灵帧路径保留（prefab 带 `SpriteRenderer` 时仍走换帧）。**武器残影尚未接入**（用户同意先不做）。同时把 `InputManager.restrictChargedToVertical` 改为 false，蓄力玩法（站桩蓄力穿刺 / 横滑蓄力横扫）重新成立。
- 状态：进行中（刚性枪体V02已验收；V06前左右后四图已交付，新增左侧待验收；旧顶底候选和3D模型接入未完成。V06只提交一次，中断恢复未重新生成，没有Unity工程或模式改动。）

### REQ-002 多攻击动作敌人的预警挂点（每个攻击步骤一个）

- 日期：2026-10-07
- 类型：需求 / 战斗系统 + 配置
- 动机：当前每个敌人 Prefab 只有一个 `AttackTelegraphAnchor`（挂 `EnemyAttackTelegraph`），该敌人的所有攻击动作共用同一个预警点位，多种攻击动作时无法表达各自的出招点。
- 期望行为：
  - `AttackStep` 增加挂点引用（`EnemyAttackTelegraph telegraphAnchor`）；未指定时回退到该敌人的现有默认挂点。
  - Prefab 层级升级为「每攻击步一个挂点」，例如 `Enemy/AttackTelegraphAnchors/Attack1..N`；每个挂点独立保存 Position/Rotation/Scale，可各自绑定三帧 Sprite、颜色与播放节奏。
  - 运行时按当前攻击步骤使用对应挂点调用 `BeginWarning`，不再统一使用同一个实例。
  - 明确 Edit Mode 预览方式：Inspector 可指定「当前预览的攻击步骤」并显示该步骤对应挂点。
- 约束：禁止按 `spriteName`、攻击索引或字符串在运行时推断挂点位置（`design/anti-ghost-reference.md`）；现有已部署三帧 Sprite 的十个敌人 Prefab 不得被迁移破坏。
- 验收标准：多攻击动作敌人的每个攻击步骤可用不同挂点；Edit Mode 预览不修改挂点 Transform；战斗时序与伤害不变。
- 相关：`design/enemy-attack-telegraph.md`、`plan/attack-damage-frame-sync-plan.md` §9.3
- 状态：待处理

---

## 已关闭（归档）

暂无。
