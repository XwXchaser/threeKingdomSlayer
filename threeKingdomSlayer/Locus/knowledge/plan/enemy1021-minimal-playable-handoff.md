---
id: kd_7d503436-0e48-44b8-97e8-8a7fcd851ad3
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
summary: 1021机制接续入口：Idle/Walk/Attack/ShieldGuardHit已接入第一关，Walk/Attack/ShieldGuardHit游戏验收通过；剩余精确瞄准、独立破盾字段、ShieldBreak与独立掉盾接管等。后续目标由用户指定，默认不运行自检。
---

# Enemy 1021 最小可玩：实施记录与复测入口

> **机制与实现的权威记录已整合到 `design/enemy-1021-shield-guard.md`**（规则、参数、状态机、实现约束、验证记录、待验收项）。
> 本文件保留最小可玩版实施记录，并补充下一对话的机制接续入口；具体制作与验收进度见 `plan/enemy1021-art-production-handoff.md`。

## 0. 最新交接（2026-10-11）

- **动画基线**：Idle四帧、Walk六帧、Attack十二帧与ShieldGuardHit六帧已接入第一关，Walk/Attack/ShieldGuardHit均获用户游戏验收。Attack为2秒非循环、接触姿态键1秒；ShieldGuardHit为0.30秒非循环、首帧承力、末帧恢复并回Idle。接入未改攻击数值、命中帧声明或机制代码；已验收动作不重做，完整素材/Clip路径与留档见美术交接§0。
- **已实现机制**：四值方向分类、固定减伤、反侧重攻击破盾、`Normal → ShieldBroken → Coward`、胆怯不攻击与受伤倍率、池复位、第一关波次接入。下文测试记录属于此前最小可玩版，不能视为本次动画接入后的重新验证。
- **待继续机制**：跟手精确攻击与盾区判定、独立 `breaksGuard` / `isPreciseAim` 字段、独立盾掉落对象/脱手帧接管，以及破盾期打断、残盾、池复用等表现收尾。当前重攻击语义仍借用 `interruptsCavalryCharge`；后续字段解耦时保护骑兵行为与资产数据。
- **待继续美术**：正式 `ShieldBreak`、正面 `HitFlash`、反侧 `HitRight`、Dead/Launched/Getup、胆怯动作；独立格挡特效/音效为可选后续。正式 `ShieldGuardHit` 已完成并获游戏验收，动作素材保持无特效。Attack/ShieldGuardHit动作验收不包含剩余动画或机制的全量验收；没有确定下一步优先级，按用户在其他对话的目标推进。
- **执行边界**：本轮只更新交接，不继续实现。部署只接入第一关及对应动画，完成后通知用户自行开游戏验收；未明确要求不运行自检。第3、4节供用户或明确授权的检查参考，不自动执行。

## 1. 当时最小可玩版实施范围

- 当时做：四值方向判定（盾侧/反侧/正面/未声明）+ 固定数值减伤（下限1）+ 破盾 → 掉盾动画 → 胆怯（×1.5、不再攻击）+ 对象池复位 + 波次接入；当时美术全部复用102。后续Idle/Walk/Attack/ShieldGuardHit已替换，见§0。
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
4. 预期：Jab1/Jab2 被格挡（跳字颜色浅蓝灰、卡肉更轻、播 `ShieldGuardHit`）；Jab3 全额；**右→左划 + 蓄力**破盾 → 掉盾动画（0.46s）→ 胆怯，之后伤害 ×1.5。
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
