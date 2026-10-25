---
id: kd_c50ca659-fe39-43fe-b198-c1b9244e4c87
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_parry_guard_rework_handoff_20261026
summary: 2026-10-26 霸体反制重定 + 防御姿态/破防 + 招架动作重做的自包含交接：规则、代码落点、全部数值、验收方式、待办与坑。
---

# 交接：霸体反制 + 防御姿态 + 招架动作（2026-10-26）

> 自包含：新会话只读这一份 + `design/attack-interrupt-system.md`（§3 打断表 / §4.5–§4.8 破势·招架·防御）即可继续。
> 相关设计：`design/combo-move-state-machine.md` §19「武器动作准则：进入与离开」（本轮立规）。

## 0. 本轮产出（8 个提交）

| 提交 | 内容 |
|---|---|
| `ac5e1a1` | 数值收口：终结技 1000→60、冰雾每跳冻结概率 0.30–0.38→0.10–0.14、池权重 100000→100 |
| `707ba3d` | **霸体反制重定**：重攻击（`isHeavy`）可打断霸体并削韧；削韧只在霸体窗口内生效；归零→破势眩晕 |
| `6378639` | 文档：打断矩阵 / 出伤窗口 / 削韧跟判定成功 |
| `ad1683d` | 文档：立「武器动作准则：进入与离开」+ 纠正预输入语义 |
| `e0b7be4` / `8cb3226` / `3e6004e` / `f9968f9` | 常驻持枪实体 `PlayerWeaponStance`、破防条真衰减 + 血条上方显示、招架动作重做（挥入/过冲回弹定格/收枪出画） |
| `f734020` / `c60c190` / `d6670a8` / `d910977` | 调参窗口（几何读数 + 按目标屏幕位反解）、尺度铁律（scale 不是可调项）、取景数值 |
| `b2be79c` / `eb6d18a` / `a2cb192` / `8551efa` | 起手语义改「枪尖从下往上挑」→ 改「上抬+旋转」→ 改「竖直上提为主」→ 改「**沿枪轴推出**」 |

## 1. 最终规则（用户口径）

### 1.1 打断矩阵（`Enemy.Combat.cs`）

| 攻击者 | 前摇 | 出伤窗口 | 收招 |
|---|---|---|---|
| 轻攻击 | 打断（普通窗口；霸体期无效） | 打断 | 不可 |
| **重攻击**（`isHeavy`） | 打断（霸体期也能）→ 专职"拆前摇" | 打断 | 不可 |
| **招架** | **不断** | **格开**（取消攻击 → 出伤不结算）→ 专职"格出伤" | 不可 |

- **出伤窗口**：出伤瞬间前 `Enemy.parryStrikeWindow`（默认 **0.15s**）。出伤时刻 = 本次攻击前摇结束时刻（已按命中帧修正；远程=发射瞬间）。
  实现用攻击 tween 的已播比例换算（卡肉/冻结暂停时判定不错位）。
- **只有招架能在出伤窗口格开**；招架在前摇打不断。
- **削韧跟判定成功**：只有真的格开了一次才磨架势（旧行为是"对范围内每个敌人无条件磨"，已废除）。`Zhangfei_Parry.poiseDamage` = **30**（原 10）。
- 破防值满 → 破势眩晕 → 眩晕期间不可打断（`isAttackDrawPhase` 语义），破势把敌人打出 Stunned。
- 招架的"弹刀反馈"（`PlayClankEffect`）**至今未实现**（设计文档写了、代码里从来没有）——仍是缺口。

### 1.2 防御姿态（`PlayerGuard` + `PlayerWeaponStance`）

- 入口：**招架后按住**；预输入只是"提前按住"，**本质仍先播完 parry 再回防御位**（"挥入中一按住就切防御位"是 BUG，已修：挥入阶段只记住按住）。
- 防御期间**免伤**，只累积破防值；破防值**每帧固定速率回落**（真衰减条，无"停手 N 秒才开始降"）→ 阈值/速率 = 自然窗口（默认 120 / 30 每秒 ≈ 4 秒）。
- 破防值满 → 破防：`GameplayInputGate` 申请 `GuardBroken` → 不能行动 / 不能放技能 / 不能做手势输入，持续 `parryGuardBreakDuration`（1.5s）。
- 防御态期间**不允许出招**（手势被吞）；想攻击必须先松手收枪（与旧版"parry 时不能攻击"一致）。
- 破防条显示：`HeroHUD_Zhangfei.prefab` 的 `Health(Slider)` 上方新增 `GuardMeter`（Filled/Horizontal，复用 `zhangfei_HP_filler`）+ `HeroHUD.SetGuardMeter(ratio, visible)` + `BattleHUD` 每帧轮询。

### 1.3 武器动作准则（`combo-move-state-machine.md` §19，本轮立规）

- **除非特殊要求，武器必须"进入画面 / 离开画面"**：进入从屏外挥入，离开要挥出画面后才销毁；**不允许在画面里原地出现或原地消失**；招式交接点必须画面内连续。
- 理由：本作没有可见人物与手，武器是"手里有东西在动"的唯一证据；原地生灭会读成"凭空"。

## 2. 招架动作的最终参数（`Assets/Prefabs/UI/Skills/Zhangfei_Parry.asset`）

| 字段 | 值 | 含义 |
|---|---|---|
| `parryStanceTipDegrees` | 42 | 斜举角（= 防御位角度，防御位只用位置差） |
| `parryStanceHeadAnchor` | (2.121, 3.222, −2.5) | **矛头**落点 → 屏幕「右 104% / 上 62%」（枪尖滑出右上边缘） |
| `parryStanceEntryAngleDegrees` | 8 | 起手指向 |
| `parryStanceEntryRootOffset` | (−1.386, −2.769, 0) | 起手时枪尾（=手）沿枪轴**回退 2.2** + 落低 1.3 |
| `parryStanceExitAngleDegrees` | −100 | 收枪指向 |
| `parryStanceExitRootOffset` | (−1.2, −1.5, 0) | 收枪时枪尾推出画面 |
| `parryStanceSwingInSeconds` | 0.18 | 挥入 |
| `parryStanceOvershootDegrees` | 8 | 过冲 |
| `parryStanceSettleSeconds` | 0.08 | 回弹 |
| `parryStanceHoldSeconds` | 0.10 | 定格（格住的那一下） |
| `parryStanceExitFastSeconds` / `ExitNormalSeconds` | 0.12 / 0.54 | 收枪（**成功格挡加速 / 未格挡正常**，沿用旧版招架规则） |
| `parryStanceDefendAnchorDrop` | 0.18 | 防御位比斜举位低多少（**只用位置差** → 枪尖上扬角每次读得一样） |
| `parryStanceTasselDamping` / `MaxBend` | 26 / 14 | 持枪实例级红缨物理收敛（枪头附近抖动变小） |
| `actionDuration` / `parryWhiffDuration` | 0.48 / 0.9 | 动作锁必须覆盖"武器在画面上的全长"，否则玩家能在枪还没出画时出招（两把枪） |
| `poiseDamage` | 30 | 招架削韧（只在格开成功时） |

**动作链**：招架 → 挥入（枪尖从下往上挑，**整杆沿枪轴被推出**）→ 过冲 → 回弹 → 定格 → （按住）接防御位 → （松手）收枪**整杆出画后**销毁。

## 3. 代码落点

| 文件 | 内容 |
|---|---|
| `Assets/Scripts/Player/Combat/PlayerWeaponStance.cs` | 常驻持枪实体：姿态（枪尖指向 + 矛头锚点）、挥入/过冲/回弹/定格/收枪、预输入只记不切、受击后退 |
| `Assets/Scripts/Player/Combat/PlayerGuard.cs` | 破防值（真衰减条）、破防硬直 + 输入门、削韧判定入口 |
| `Assets/Scripts/Player/Combat/PlayerState.cs` | 持有 Guard/WeaponStance；TakeDamage 走防御吸收；Update 推进两者 |
| `Assets/Scripts/Player/InputManager.cs` / `.Gesture.cs` | `PointerHeld`（物理按下）、`IsGuarding`、持枪姿态进行中吞掉手势 |
| `Assets/Scripts/Player/Combat/AttackSystem.Types.cs` | 招架：格开成功判定 → 传给 stance（决定收尾加速）+ 镜头震动；`parryPoiseDamage` 透传 |
| `Assets/Scripts/Enemy/Enemy.cs` / `.Attack.cs` / `.Combat.cs` / `EnemyPoise.cs` | `parryStrikeWindow` + `IsInStrikeWindow()`、前摇时长记录、被打断块重写、破势削韧 |
| `Assets/Scripts/UI/HeroHUD.cs` / `BattleHUD.cs` | `SetGuardMeter` + 每帧轮询 |
| `Assets/Scripts/Editor/ParryStanceTuningWindow.cs` | **调参窗口**（Tools ▸ 三国杀戮 ▸ 招架姿态调参） |
| `Assets/Scripts/Data/AttackSkillConfig.cs` / `GameplayInputGate.cs` / `AttackSkillConfig` | 姿态与破防字段；`InputBlockReason.GuardBroken` |

## 4. 调参工具（重要）

`Tools ▸ 三国杀戮 ▸ 招架姿态调参`
- 滑杆：矛头锚点 xyz / 斜举角 / 起手指向 / 起手枪尾偏移 / 防御下沉 / 挥入 / 过冲 / 回弹 / 定格 / 收枪×2
- **读数**（相机几何解算，0%=中心 100%=边缘）：矛头与枪尾（斜举位、起手位）、杆身左缘穿出点与占比、**起手是否整杆出画**、深度处可见半宽/半高
- **按目标屏幕位反解矛头锚点**：填「矛头屏幕 X% / Y% / 深度 z」→ 直接算世界锚点并应用（相机空间射线 `(sx·tanH, sy·tanV, 1)` 走到深度平面）

## 5. 本轮踩过的坑（别再踩）

1. **scale 不是可调项**：武器大小是**武器的属性**，slash/stab 一致 → 持枪必须用 prefab 原尺寸（`parryStanceScale` 已删）。slash/stab 的枪巨大是因为它**从镜头前扫过**（一次性特效、离相机 4.5 单位），持枪不能类比。
2. **整杆枪在任何前景深度都必然出画**：相机 aspect 0.5、可见半宽 `0.2887·(z+10)`，枪长 10.8 → 构图只能靠解算"矛头在哪、杆身从哪穿出"，不能靠截图目测。
3. **纯旋转会读成"绕一个点甩"**：手的位移在画面外 → 观众只看枪尖的净路径。第一人称双手持矛的正解是**手提 + 顺枪轴推出**（枪尖净位移近似直线，旋转只留 34°）。
4. **收枪必须整杆出画后再销毁**（准则），否则就是"原地消失"。
5. **一批多编辑里只要有一条"新旧内容相同"，整批不应用**（工具只提示那一条）→ 每次改完必须回读文件确认（曾因此漏掉 `_entryRoot`，表现成"武器凭空出现"）。
6. **资产值只在重进 Play Mode 时重读** → 改完必须退出 Play 再进，否则看到旧值。
7. 截图有秒级延迟，抓不到 0.18s 的动画中间帧 → 验收靠**慢速 + 数值采样**（手动 `Tick` 打印枪尖/枪尾轨迹）。

## 6. 待办

| # | 事项 | 状态 |
|---|---|---|
| 1 | 招架动作的实机手感（用户已说"大体对了但仍有调整空间"，这版是「沿枪轴推出」版，**等他再进 Play Mode 看**） | 待用户看 |
| 2 | **防反动作（defend_attack）**：用户说"规则之后再说"，只**预留了竖直终态**（参考图 defend_attack3 = 竖直 90°，由斜举 42° 上撩） | 未实现 |
| 3 | **弹刀反馈**（`PlayClankEffect`）：霸体期命中的橙红闪+横向抖动，设计有、代码无 | 未实现 |
| 4 | 防御位的"位置差 0.18"是否够（角度一致 → 枪尖上扬角每次一样，但可能看起来不够"稍低于 idle"） | 待用户看 |
| 5 | 3D 加分项（用户明确欢迎）：挥入时枪尖**前探**（z 曲线）、绕枪身轴的**翻滚 roll**、收枪的**顿点** | 未实现，可选 |

## 7. 同批未收尾的事（别的线，别误以为已做）

- 招架仍是**局外天赋解锁项**（`talent_unlock_parry`，100 铜钱，独立旁支）——2026-10-25 修过一次：旧规则"空列表=取列表上一项"把它挂在 1850 铜钱单链末端，导致根本买不到。现已修（`design/out-of-game-growth-system.md` 有记）。
- 斩击链（`design/combo-registry.md`）：只有登记与结论文档，**没有资产**；`StabR2` 的 `poiseDamage` 现为 30（C2 是单列 heavy → 可破冰也可削韧）。
