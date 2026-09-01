---
id: kd_4a9116b1-c70a-4de3-8eeb-801deb71c4fe
injectMode: inherit
summary: 更新至 2026-03 — 新增 TimedArrow 命中与视觉生命周期、随机轨迹、DOTween 清理、受击缩放、序列化迁移、视觉/伤害解耦及 Time 被动类型区分经验
aiEditMode: auto
maintenanceRules: |-
  - Keep only durable and reusable project memory
  - Consolidate duplicates or conflicts into the latest conclusion
  - Remove temporary context, one-off tasks, and unsupported guesses
---

### DOTween 特效生命周期与战斗结束边界（2026-03）
- 典型故障：动态火焰/箭雨等子对象被父特效或节点切换销毁后，未终止的 `DOMove` / `DOFade` 仍访问已销毁的 Transform/SpriteRenderer，抛 `MissingReferenceException`；`OnKill` 再次 `Destroy` 还会导致 DOTween 内部回收重入和 `IndexOutOfRangeException`。
- Tween target 必须可追踪：创建时明确 `SetTarget(dynamicGameObject)` 或 `SetTarget(dynamicTransform)`；销毁时必须以**完全相同的 target**调用 `DOTween.Kill`。`transform.DOKill()` 不能清理 target 设为 GameObject 的 Tween。
- 动态子对象由父特效管理时，父 `OnDestroy` 必须停止协程并枚举子对象，逐一 Kill 对应 target；自然播放结束仅在 `OnComplete` 销毁，禁止 `OnKill -> Destroy`。
- 时序边界：三选一仅暂停游戏，所有已开始表现（含 ULT）冻结后恢复；ULT 只在真正结束战斗时取消。路线奖励等待是软结束：停止新刷怪/新战斗逻辑，已开始的死亡动画与普通特效自然结束。只有玩家确认切换节点、重开或回菜单才硬清理残留表现。
- 新技能验收：必须覆盖“播放中进入三选一”“最后一击进入奖励等待”“玩家确认离开节点”三个场景，并检查 Console 无 MissingReference/DOTween 回收异常。


- 用户确认：存档点在可存档节点 **Head 到达时立即保存**，不是 Tail。
- 失败恢复从该节点 Head 开始，重新执行 Head→Combat，并重新执行该节点本次应执行的 BattleEntry；不能因为保存时刻位于 Head 就把当前节点战斗标记为已完成。
- 例如：A→C，抵达 C Head 保存；C 战斗中失败后，重开从 C Head 开始，而不是 C Tail 或 A。
- 当前实现曾错误地在 Tail 保存，并在恢复后清空完成条目；后续实现必须以 `route-stage-gameplay-spec.md` 的 Head 存档规则为准。

- 路线结构：`A→B`、`A→C`、`A→D`、`C→B`、`D→B`；B 的 Head 是多个入边共享的唯一目标姿态，每条入边独立拥有 Pivot 和路径。
- 症状：A→C 报 `target node entry missing: C`，而 A→B、A→D、D→B 正常；移动阶段曾出现先后退再前进。
- A→C 根因：`CombatNode_C` GameObject 保留，但其 `RouteCombatNodeEntryV2` 组件变成 Missing Script，`sceneEntry.nodes[2]` 同时指向已销毁组件。不要只检查 GameObject 名称或场景 YAML 中是否有 C；必须检查组件类型、有效引用和 `TryGetNode` 结果。最终通过用完整 A 节点结构重建 C，再绑定 C 配置、Head/Combat/Tail、内部路径和 A→C/C→B 连接修复。
- 移动倒退根因：旋转 `RouteStageRoot` 后又读取已随根节点移动的路径点，并用 `root.position - path[0]` 额外叠加 Delta，造成路径先偏离最终目标再返回。路线根节点最终 Pose 只能由目标锚点对齐计算，不能再叠加路径首尾 Delta。
- 移动修复：旋转阶段保持 `rotationPivot` 世界位置；旋转完成后直接将根节点移动到目标锚点计算出的最终 Pose，并在结束时一次性校正。路径点当前只作为编辑/校验数据，不能在该实现阶段继续作为独立世界路径源。
- 场景生命周期经验：Additive RouteStage 场景重入前必须处理同名旧场景，否则旧实例、旧组件引用可能污染运行时。失败回调也必须恢复 `_choosing`，避免输入被锁死后表现为暂停。
- 预防规则：**场景化路线问题优先对比“正常节点”和“异常节点”的组件实例、数组引用、Script GUID、Asset GUID 和 `TryGetNode`；不要只对比逻辑配置。任何节点重建、脚本丢失或 GUID 变化后，都必须重新保存 SceneEntry 的节点/连接引用并重新加载验证。**
- 相关文件：`Assets/Scripts/RouteV2/RouteStageRuntimeV2.cs`、`Assets/Scripts/RouteV2/RouteStageSceneEntryV2.cs`、`Assets/Scripts/RouteV2/Editor/RouteStageV2Validator.cs`、`Assets/Scenes/RouteStageV2/Stage01_RouteV2.unity`

- 症状：连续 Parry 普通敌人多次后，敌人架势从 50 逐步降到 0，进入 `Stunned`；眩晕结束后普通敌人回到 `Idle`，位于 row=0 时没有移动事件重新调用 `StartAttacking()`，之后不再攻击。
- 根因：`Enemy.TakePoiseDamage()` 只检查 `state == Attacking` 和 `isAttackDrawPhase`，没有限制 `isBoss`，导致已经停用的"普通敌人架势/眩晕"机制重新生效；同时没有要求 `isAttackAnimating`，所以普通敌人在攻击冷却阶段也能被 Parry 持续削架势。
- 修复：
  1. `TakePoiseDamage()` 首先拒绝非 Boss；
  2. 仅在 Boss `InCombat`、`state == Attacking`、`isAttackAnimating == true` 且不处于 `AttackDraw` 时削架势；
  3. 普通敌人保留原有 Parry 攻击打断，但不再因 Parry 累计架势或进入 Stunned。
- 预防规则：**架势/眩晕是 Boss 专属机制，任何通用 Enemy 方法都必须明确区分 `isBoss`；Parry 架势伤害必须要求实际攻击动画前摇状态，不能只依赖 `state == Attacking`，因为该状态覆盖攻击冷却阶段。**
- 文件：`Assets/Scripts/Enemy/Enemy.cs`

### PlayLaunchVisual 变量声明顺序错误导致编译失败 ✅ 已修复
- 症状：`windupDistance`、`sideRatio`、`riseDistance` 在声明前被使用（CS0841），导致 `PlayLaunchVisual` 无法编译。
- 根因：重构枪尾支点模型时，将变量计算行放在 camera 向量行之后，但 windupPos/apexPos 计算仍未迁移，留在声明前引用。
- 修复：将 `windupDistance`/`sideRatio`/`riseDistance` 声明移到 camera 向量和轨迹计算之前。
- 文件：`Assets/Scripts/Player/AttackSystem.cs`

### Launch 蓄势偶发诡异绕转 ✅ 已修复并验收
- 症状：Launch 发动时，接管蓄力武器 pose 后，Windup 阶段低概率出现不符合既定动作的绕远或翻转；连续测试后修复版本暂未复现。
- 根因：目标姿态先通过 `.eulerAngles` 拆成欧拉角，再交给 `DOLocalRotate(..., RotateMode.Fast)` 插值。实时蓄力 pose 可能接近欧拉角环绕或非唯一表示区间，同一四元数会被拆成差异很大的欧拉角组合；随机倾角完整参与 Windup 又放大了异常。
- 修复：
  1. Windup 改为自定义 DOTween 进度，由 `Quaternion.SlerpUnclamped(startRotation, windupRotation, t)` 直接插值；
  2. Windup 位移与旋转由同一进度同步驱动；
  3. 随机倾角在 Windup 仅应用 12%，完整随机倾角延后到上挑终态。
- 预防规则：**从实时世界 pose 接管的武器动画不得将目标 Quaternion 转为 Euler 后做 Tween；跨对象/跨坐标系旋转衔接优先使用 Quaternion Slerp，并限制随机姿态在过渡前段的参与量。**
- 文件：`Assets/Scripts/Effects/LaunchVisualEffect.cs`

### 蓄力 Pierce 枪尾偏移改造（枪尖锚点）✅ 已完成
- 需求：蓄力枪尾随射出角产生 X 偏移、枪尖跟手、枪身以射出角射出；入场就位时角度和偏移与跟手一致，且 Pierce 衔接不受影响。
- 根因：参考的 Stab“枪尾偏移”是**枪尖锚点**结构——枪尖对齐目标列（ray 前方）、枪尾沿枪轴反向延伸（ray 后方），`stabVisualStartXOffsets` 只是额外整体 X 平移。蓄力原本是“绕中心旋转”（枪身中心对齐跟手点、枪尖枪尾对称）。反复误做三种错误，一直没落地：
  1. 整体平移（`position.x += offset`）→ 枪尖也被平移，枪体整体偏移、离开屏幕；
  2. 绕中心旋转（原版 `Euler(90,0,-zRot)`）→ 枪尖枪尾对称，像“钻头”；
  3. 枪身绕枪尖偏转（枪尾额外偏移）→ 偏移量一大枪身就 360° 翻转。
- 修复：改为枪尖锚点 `position = tip - axis * halfLength`（`halfLength = _weaponLength * 0.5`，`_weaponLength = _sr.bounds.size.y` 缓存世界枪长）。枪尖严格跟手，枪尾偏移 = `-axis.x * 枪长`，完全由射出角（`maxAngle → zRot → axis.x`）决定，不做额外平移；`maxAngle` 作为可调旋转角，减小即让枪尾偏移变小、枪体留在屏幕内。
- 入场：`_entryAxis = 枪轴方向`，枪尖沿射出角从后方刺入；枪尾沿枪轴反向自然前进。所谓“自我修正”（枪尾 X 随 entryDistance 变化）其实是“沿射出角前进”的自然表现，不是 bug；之前误把它当 bug 去改入场方向（改成纵深 Z），反而变成“整体平移”。
- 预防规则：
  1. **复刻已有视觉功能前先读懂它的锚点结构**（枪尖/枪尾谁对齐目标、谁反向延伸、哪些是额外平移项），不要凭空在“平移/旋转/锚点”里猜。
  2. **偏移量按“射出角”算，不要按“屏幕位置”算**：用 `axis.x`（枪轴 X 分量），而不是 `normalizedX`（手指位置）。
  3. **单 Sprite 下枪尾是 Sprite 的一部分**（枪尾 = 枪身 - axis×半枪长），无法独立偏移；要“枪尖跟手 + 枪尾偏移”只能枪尖锚点，偏移由旋转角决定。
  4. 旋转表示等价性可用 `unity_execute` 验证：`Quaternion.LookRotation(axis, Vector3.up) * Quaternion.Euler(90,0,0)` 与 `Quaternion.Euler(90,0,-z)` 完全等价（angleDelta=0），可放心互换，避免“打竖/打横”。
### 本轮路线存档与奖励/技能时序经验（2026-03）

- 路线 BattleEntry 清空不能直接推进 Tail：敌人清空、经验宝石飞行、三选一、道具选择和弃置是不同完成条件。必须等待经验宝石全部收集及阻塞 UI 完成后再结束 BattleEntry。
- 经验宝石在敌人死亡后异步飞向经验条；若提前把 `PlayerState.stageState` 改为 `Starting`，宝石到达时 `PlayerState.AddExp()` 会直接返回，导致升级事件被吞。奖励等待应使用独立门控，不能用非 InProgress 状态阻止经验结算。
- 快照恢复必须是 replace 语义：先清空 `PlayerState.acquiredUpgrades`、`UpgradeEffectManager`、`ActiveSkillInventory`、道具库存、被动注册表和被动协程，再按快照升级列表重建。被动 UI 还拥有独立 `_upgradeIcons`/槽位缓存，必须通过 reset 事件清空后再重建。
- 快照中 `currentLevel/currentExp` 已正确保存/恢复，但直接赋值不会刷新 HUD；恢复后应发送仅用于显示的经验/等级同步事件，不能发送 `OnLevelUp`，否则会重复弹三选一。
- 计时被动在奖励阶段获得时，不能立即触发，也不能开始消耗 timer；进入正式 Combat 后才允许首次触发并进入冷却。当前实现曾通过 pending 集合、`IsRouteCombatActive` 和效果清理进行修补，但日志显示仍可能出现 Head→Combat 残留效果或“无实际效果却进入冷却”，因此该问题只能标记为待观测/待重构。
- 诊断日志本身会改变 Unity Editor 的帧时序和协程相对顺序；出现“加日志后问题消失”时不能视为修复。应优先使用关键状态变化日志，避免每帧输出造成观测扰动。
- 效果触发必须区分“调用 SpawnEffect”和“效果实际成功创建”：配置、Prefab、组件或 ColumnManager 缺失时不能提交冷却；失败应保留待触发状态并输出失败原因。
### 已修复：SpawnEntry/地刺同步死亡导致补齐请求丢失
- 症状：FakeStage01 N3/N4 中，敌人正在初始入场或后续补齐时被地刺击杀，后方敌人停止补齐；纯 0 排会放大现象，但不是规则根因。
- 根因：SpawnEntry pending 期间发生整排死亡时，逻辑重排请求可能被提前消费；补齐移动完成后的地刺同步死亡还会继续执行死亡对象的移动收尾，破坏调度状态。
- 修复：SpawnEntry 准备或 pending 期间延迟逻辑重排，最后一个 SpawnEntry 完成/移除后重试 dirty 请求；地刺检测后若敌人已死亡则立即结束当前移动收尾。
- 验收：用户已完成 FakeStage01 主路线战斗节点验收。
- 文件：`Assets/Scripts/Core/ColumnManager.cs`、`Assets/Scripts/Enemy/Enemy.cs`

### FakeRoute 假移动路线阶段总结（2026-03）

- 新方案不是旧 RouteStage 空间移动的简化版，而是独立的纯逻辑路线层：固定 Battle.scene，节点是 ScriptableObject 逻辑关卡，路线选项直接引用目标节点，背景表现与节点提交解耦。
- 已实现并验收 `A → B/C → D` 拓扑、多个来源汇入同一目标节点、节点差异化普通敌人阵列、BattleEntry 清场后的奖励等待、假移动占位表现和终点结算。
- 已实现并验收 FakeRoute 独立快照：存档节点、已访问节点、BattleEntry 完成状态、路线选择历史、玩家生命/复活/等级/经验、击杀数、局内铜钱、被动和主动技能持有/等级、UT 能量。主动技能/普通攻击冷却、计时被动剩余时间、敌人、投射物、连击、QTE、DoT、临时效果和假移动进度不保存。
- 快照恢复采用 replace 语义：先清理运行态，再按快照重建玩家和 Build；恢复从对应存档节点重新进入，不从死亡位置或新节点继续。MainMenu Continue 与失败恢复分离，Continue 从最后未完成路线关卡的 startNode 重新开始，不读取失败快照。
- FakeRoute 快照与旧 V2 快照隔离，并通过架构标识、快照版本、routeId、stageId、configurationVersion 校验，避免同名节点被静默误恢复。
- 当前仍未完成：正式路线选择 Canvas UI、真实背景动画/音效/转场、条件系统、剧情状态、更复杂节点阶段/重访规则，以及旧 Route/RouteV2 代码和资产的最终清理。
- 预防规则：空间技能的伤害逻辑与视觉生命周期必须独立验证；路线转场只清理运行时视觉实例，不得清除局内升级等级。重新进入战斗时若视觉依赖敌人生成后的父节点，不能在刷怪前立即创建，应等待敌人容器可用后再重建，否则会出现“伤害仍生效但地刺不可见”。
- 文件：`Assets/Scripts/Core/SpikeTrapController.cs`、`Assets/Scripts/Managers/StageController.cs`

### 地刺路线转场视觉生命周期坑点（2026-03）
- 症状：地刺升级在上一战斗生效；路线移动期间地刺应消失，但下一战斗节点只剩伤害效果，地刺基础视觉没有显示。
- 根因：地刺视觉重建发生在敌人生成之前，`SpawnVisual()` 找不到敌人的阵型父节点；伤害检测不依赖视觉对象的正确挂载，因此形成“有伤害、无视觉”。
- 修复：将地刺配置/等级与视觉实例分离；非战斗期间调用 `DeactivateVisual()`，只销毁视觉和停止协程，不清空 `_appliedUpgrades` 或 `PlayerState.acquiredUpgrades`。进入战斗后由 `RequestVisual()` 协程等待敌人容器可用，再挂到敌人阵型父节点生成视觉。
- 预防规则：**场地技能必须分别验收“逻辑触发”和“视觉显示”；依赖动态敌人层级的视觉不能假设敌人已生成，战斗开始应等待合法父节点后重建。路线转场清理不得复用会清除局内升级的总重置逻辑。**
- 文件：`Assets/Scripts/Core/SpikeTrapController.cs`、`Assets/Scripts/Managers/StageController.cs`

### FakeRoute 假移动路线阶段总结（2026-03）