---
id: kd_7d503436-0e48-44b8-97e8-8a7fcd851ad3
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
summary: 1021接续入口：Idle/Walk/Attack/ShieldGuardHit/HitFlash/Dead 已游戏验收；HitRight 与 ShieldBreak（含独立盾掉落）已接入待验收。不重做已验收动作。剩余：击飞 Rise/Fall、无盾胆怯家族（正式 CowardIdle/Hit/Launched/Dead）、起身与眩晕待拍板；精确攻击未实现。接入流程见 skill/workflows/character-action-pipeline-entry.md。不自动自检。
---

# Enemy 1021 最小可玩：实施记录与复测入口

> **机制与实现的权威记录已整合到 `design/enemy-1021-shield-guard.md`**（规则、参数、状态机、实现约束、验证记录、待验收项）。
> 本文件保留最小可玩版实施记录，并补充下一对话的机制接续入口；具体制作与验收进度见 `plan/enemy1021-art-production-handoff.md`。

## 0. 最新交接

### 2026-10-17：持盾死亡 `Enemy_1021_Dead` 已接入第一关并获用户游戏验收

- 用户要求“按交接内容接入 1021 敌人的 dead 动画”。只用用户已验收的 3 帧透明素材（`Library/Locus/tmp/enemy1021_dead_video_v2/dead_3frames_matted_v1/`：`dead01_f044` / `dead02_f046` / `dead03_f072`，960×960 RGBA）按固定偏移 `(74,91)` 零重采样补边到 1108。
- 新增资产：`Assets/Sprites/Enemy/Enemy1021/Enemy_1021_dead1..3.png`（导入设置逐项镜像 `Enemy_1021_hitFlash1`）、`Assets/Animations/Enemy_1021_Dead.anim`（30fps、0.6s、Clamp、键位 0/0.2/0.4＋保持 0.5667、`ShieldVisual` null 键）。
- 改动既有文件 1 个：`Assets/Animations/Enemy_1021.controller`（仅 `Dead` 状态的 motion 换为 `Enemy_1021_Dead`）；其余 14 个状态、18 条转场与 6 个 Trigger 不变。`Enemy.cs` 的 `PlayDeadVisual()` 未改（仍按名 `Play("Dead")`）。
- 静态核验通过：精灵引用完整、Clip 长度/键位/末帧保持正确、旧 `ShieldVisual` 已清空、Prefab/脚本/场景/关卡哈希未变（102 个保护文件只有控制器变化）；未进入 Play、未采样动画。核验记录 `Library/Locus/tmp/enemy1021_dead_deploy_v1/asset_verification.json`。
- 验证边界：按用户要求未进入 Play、未采样动画；无盾胆怯死亡（`CowardDead`）仍为 102 占位。用户于 2026-10-17 开游戏验收通过，`asset_verification.json` 已同步 `user_game_acceptance=accepted`。
- **动画后续状态（2026-10-17）**：胆怯家族的 `CowardIdle` 素材已验收；`CowardHit` 的视频 v1（动作太小）与 v2（两个语义不一致的受击动作、切换僵硬）均验收不通过，帧挑选补救也被否，**等另一侧改写提示词**；本侧不改词、不重发、未抠图未接入。详见 `plan/enemy1021-art-production-handoff.md` 最新停点与 `plan/enemy1021-coward-family-plan.md`。

### 历史停点：ShieldBreak 已接入第一关（含独立盾掉落），待用户游戏验收

- 透明帧与分件经用户“已验收，请按需求接入”后完成接入。范围只做到“接入＋配置＋静态核验”，**未进入 Play、未采样动画、未做对象池实测**。
- 新增资产：`Enemy_1021_shieldBreak1..6.png`、`Enemy_1021_shieldDrop.png`、`Enemy_1021_ShieldBreak.anim`（50fps/0.46s，0.12s 脱手事件＋0.46s 结束事件）、`Enemy_1021_CowardIdle_Hold.anim`（`CowardIdle` 静态兜底）、`Assets/Prefabs/Effects/Enemy_1021_ShieldDrop.prefab`、`Assets/Scripts/Effects/ShieldDropEffect.cs`。
- 改动既有文件 4 个：`Enemy.cs`、`ShieldGuardEnemy.cs`、`Enemy_1021.controller`、`Enemy_1021.prefab`。破盾期不抢 Walk 的逻辑**仅对 1021 生效**；池复位会清空旧 `ShieldVisual`。
- 静态核验通过（编译＋域重载、事件方法可解析、引用完整、关卡仍含 1021、场景未 dirty）；受保护哈希核对显示只有上述 4 个目标文件变化。
- 核验留档：`Library/Locus/tmp/enemy1021_shield_break_deploy_v1/asset_verification.json` 已补写（只输出原生数值，避开序列化报错）。未完成：无盾胆怯家族其余动作、持盾死亡、击飞仍为 102 占位；接入步骤见 `skill/workflows/action-to-unity-deployment.md`。
- 详细台账见 `memory/video-generation-case-log.md` 的 “Enemy 1021 ShieldBreak” 段落与 `plan/enemy1021-art-production-handoff.md` §0 最新停点。

### 历史停点（2026-10-14）：HitFlash 游戏验收通过、HitRight 接入；剩余事项由美术交接§0.2汇总

- 用户新指定按HitFlash v1姿态制作视频，审阅完整双语词/两张RGB(0,177,64)绿幕副本/4秒参数后明确“发起生成”。task `task_16d49a77f64446e693cf170b55106a32` 已创建一次，HTTP200；accepted UTC `2026-10-14T04:23:10.769696+00:00`。seedance-2-fast/4秒/720p/1:1/无音频/无水印/seed=-1。
- 首轮120秒6次查询均generating，后续查询同一task一次于UTC `2026-10-14T04:34:06.960219+00:00` 观察success；成片 `C:/Users/steam/Videos/doubaoVideo/enemy1021_hitflash_v1.mp4` 已下载，1,951,465 bytes，MP4容器及PyAV全帧解码通过。实测960×960/24fps/97帧/4.041667秒/H.264/无音轨；cost.spend=0.48888（币种未知）。用户已验收视频动作。
- 六帧零基 `f028/f034/f043/f046/f049/f052`、T80透明结果与HitFlash第一关游戏表现均获用户验收。六张透明Sprite按既有 `(74,91)` 固定偏移补边为1108×1108，创建 `Assets/Animations/Enemy_1021_HitFlash.anim` 并替换1021 Controller的HitFlash motion；静态导入、引用、源像素和保护文件核对通过。未运行代理自检；其余动画/机制清单见 `plan/enemy1021-art-production-handoff.md` §0.2。
- HitRight接入：用户自行对已验收六帧（`hitRight1_f018`…`hitRight6_f066`）完成抠图并明确“我已抠好那批图，你可以接入了”。六张按固定 `(74,91)` 补边为1108×1108、镜像Idle导入设置；新建 `Assets/Animations/Enemy_1021_HitRight.anim`（30fps、0.9秒、非循环、6帧+末姿态保持键、`ShieldVisual`清空键），Controller新增 `HitRight` 状态与 `HitRight → Idle`（exit 0.9、duration 0），其余状态motion、Prefab与第一关配置未改。补边仅做两项归一（半透明→不透明、全透明RGB→0），用户源图未动；HitRight游戏表现待用户验收。

### 历史交接（2026-10-13，素材制作暂停；以以上最新停点为准）

- **动画基线**：Idle四帧、Walk六帧、Attack十二帧与ShieldGuardHit六帧已接入第一关，Walk/Attack/ShieldGuardHit均获用户游戏验收。Attack为2秒非循环、接触姿态键1秒；ShieldGuardHit为0.30秒非循环、首帧承力、末帧恢复并回Idle。接入未改攻击数值、命中帧声明或机制代码；已验收动作不重做，完整素材/Clip路径与留档见美术交接§0。
- **已实现机制**：四值方向分类、固定减伤、反侧重攻击破盾、`Normal → ShieldBroken → Coward`、胆怯不攻击与受伤倍率、池复位、第一关波次接入。下文测试记录属于此前最小可玩版，不能视为本次动画接入后的重新验证。
- **待继续机制**：跟手精确攻击与盾区判定、独立 `breaksGuard` / `isPreciseAim` 字段，以及破盾期打断、残盾、池复用等表现收尾（独立盾掉落对象与脱手帧接管已于 2026-10-15 实现，见上）。当前重攻击语义仍借用 `interruptsCavalryCharge`；后续字段解耦时保护骑兵行为与资产数据。
- **历史受击停点（2026-10-13）**：`HitRight` v2姿态、视频动作和六帧选择已验收，补绿底后的SVL透明候选未获验收，当时暂不部署。`HitFlash`正面受击图v1–v4当时均未通过。2026-10-14用户重新指定v1后，HitFlash已完成视频/选帧/透明/游戏验收；本段仅作历史快照。
- **历史待继续美术（2026-10-13）**：当时剩余HitFlash姿态、HitRight透明收尾、正式ShieldBreak、Dead/Launched/Getup和无盾胆怯动作。**当前状态（2026-10-17）**：HitFlash与持盾Dead已完成并游戏验收；HitRight与ShieldBreak已接入待验收；Launched_Rise/Fall、无盾胆怯家族仍未完成，Getup/Stun待用户决定。完整清单见 `plan/enemy1021-art-production-handoff.md` §0.2。
- **执行边界**：本次收尾仅更新交接与验收留档，不继续制作或实施；当前暂不部署，不进入Play或运行自检。后续生成前先完整中英文词、实际绿幕副本和参数审批，不上传1011动作图片；任何新授权接入仍只做到第一关与对应动画，用户开游戏验收。第3、4节是最小可玩版历史复测入口，不自动执行。

## 1. 当时最小可玩版实施范围

- 当时做：四值方向判定（盾侧/反侧/正面/未声明）+ 固定数值减伤（下限1）+ 破盾 → 掉盾动画 → 胆怯（×1.5、不再攻击）+ 对象池复位 + 波次接入；当时美术全部复用102。后续Idle/Walk/Attack/ShieldGuardHit/HitFlash已替换，见§0。
- 当时未做：跟手精确攻击与盾区判定、`breaksGuard` / `isPreciseAim` 独立字段、盾与格挡的正式美术（含 `ShieldVisual` 的排序/锚点/复位问题）。这是最小可玩版历史范围；正式格挡动作现已完成，当前剩余项以§0和美术交接为准。

## 2. 改动清单

| 文件 | 类型 |
|---|---|
| `Assets/Scripts/Enemy/ShieldGuardEnemy.cs` | 新增（盾判定组件） |
| `Assets/Scripts/Enemy/Enemy.cs` | 修改 +107 / −5（形态、钩子、入口、守卫、池复位） |
| `Assets/Animations/Enemy_1021.controller` | 新增（复制 102 + `ShieldBreak`、`ShieldGuardHit`） |
| `Assets/Resources/EnemyPrefabs/Enemy_1021.prefab` | 新增（复制 102 + 改 id/名称/血量/关旧减伤/挂组件） |
| `Assets/RouteData/Stage01/N1_ArtPreview_Battle.asset` | 修改 1 行（第一排加入 1021；实验用，回退 = 把列 0 改回 0） |

`Assets/Resources/EnemyPrefabs/Enemy_102.prefab`、`Assets/Animations/Enemy_102.controller` 等 102 资产**未改动**。

## 3. 复测步骤

1. 打开 `Assets/Scenes/Battle.scene` → Play。当前走 `Stage01_RouteV2` → 节点 A → 战斗配置 `N1_ArtPreview_Battle`。
2. 第一排应出现：列 0 = `盾卫(1021)`，列 2 = `持剑杂兵_1011`。
3. 招式操作（`Zhangfei` 招式表）：中立 Tap → `Jab1`（正面），再 Tap → `Jab2`（盾侧），再 Tap → `Jab3`（反侧）；横划 → 斩击（左→右 = 盾侧，右→左 = 反侧）；蓄力横划 → 横扫。
4. 预期：Jab1/Jab2 被格挡（跳字颜色浅蓝灰、卡肉更轻、播 `ShieldGuardHit`）；Jab3 全额；**右→左划 + 蓄力**破盾 → 播 1021 正式 `ShieldBreak`（0.46s；0.12s 时本体去盾并由独立盾对象接手下落）→ 胆怯（接 `CowardIdle` 静态兜底），之后伤害 ×1.5。
5. 破盾表现期验证：期间被普通攻击不打断（闪白/掉血照常）；被挑飞则立即转胆怯并走击飞。
6. 连续两波/对象池复用：新实例应回到持盾态（`form=Normal`）。

## 4. 脚本化验证时的注意

- 命中卡肉会临时禁用 Animator，**不能**在命中后 1 帧读动画状态（会误判"动画没播"）；等 0.1~0.2s 再读。
- 破盾与胆怯两段之间必须复位血量，否则第二次受击会把敌人打死，出现"没进胆怯"的假失败。
- 直接调用入口：`pool.GetEnemy(1021)` → `enemy.Initialize(col, row)` → `enemyManager.RegisterEnemy(enemy)` → `columnManager.RegisterWaveEnemy(enemy, row)`。

## 5. 相关文档

- `plan/enemy1021-art-production-handoff.md`：最新动画/素材验收、部署基线、剩余任务与可复制的新对话入口。
- `memory/user-preference.md`：先选帧验收、第一关部署与不自动自检等用户边界。
- `design/enemy-1021-animation.md`：状态与动画矩阵、时长和接线契约。
- `design/enemy-1021-shield-guard.md`：1021权威文档（规则 / 参数 / 状态机 / 交互 / 历史验证 / 待完善项）。
- `design/cavalry-enemy.md`：同体例的另一个敌人专属文档（109）。
- `plan/enemy1011-animation-deployment-handoff.md` §4：`HitReactionDirection` 的既有语义来源。
