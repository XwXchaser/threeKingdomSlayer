---
id: kd_f7c02667-fb9c-4106-9ad4-0b10f6c79a3d
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 盾兵敌人（1021）动画制作交接：状态 × 动画矩阵

本文件是 1021 盾兵的**动画制作输入**。机制规则与状态机以 `design/enemy-1021-shield-guard.md` 为准；本文件只回答"每个状态需要什么动画、什么时候播、时长对不齐时谁让谁、素材放哪、怎么接线"。

> **开工入口**：当前进度、素材来源、后续未完成项与用户制作流程见 `plan/enemy1021-art-production-handoff.md`；机制接续见 `plan/enemy1021-minimal-playable-handoff.md`。
> **最新状态（2026-10-17）**：Idle四帧、Walk六帧、Attack十二帧、ShieldGuardHit六帧、HitFlash六帧与**持盾死亡 `Enemy_1021_Dead`（3帧、30fps、0.6秒非循环、末帧保持）**均已接入第一关并获用户游戏验收；HitRight六帧与ShieldBreak（含独立掉盾）已接入、待游戏验收。Attack为30fps、2秒非循环，`attack6` 接触姿态键在1秒；ShieldGuardHit为0.30秒非循环；HitFlash为30fps、0.3667秒非循环；HitRight为30fps、0.9秒非循环。身份锚点仍为 `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3.png`。击飞三阶段与无盾胆怯家族仍为102占位，详见 `plan/enemy1021-art-production-handoff.md` §0。已验收动作不重复制作。
> **用户验收边界**：先选有效帧交用户验收，再正常SVL抠图和尺寸/锚点对齐；部署只接入第一关及对应动画，完成后通知用户开游戏检查。未在对话中明确要求时不运行自检；本次Attack接入与交接未进入Play Mode，用户随后自行开游戏并确认通过。

制作规范遵循 `design/pixel-character-and-enemy-animation-spec.md`（统一画布与脚底锚点、禁止整体平移、四周留边距、动作按"起手 → 蓄力 → 命中 → 收势"）。命名与导入参考 `plan/enemy1011-animation-deployment-handoff.md`。

## 1. 驱动的状态机

1021 的形态机（`EnemyFormState`，见主文档 §5）：

```
Normal（持盾）
  └破盾→ ShieldBroken（掉盾表现期，0.46s）
            └→ Coward（胆怯）
```

正交的通用敌人状态：`Dead` / `Launched` / `HitFlash` / 眩晕（`Stunned`）/ 补齐移动（`Moving`）/ 攻击（`Attacking`）。

- 全部位移由代码驱动（DOTween 插值根 `transform.localPosition`）；动画**不允许**带位移或 root motion（`Animator.m_ApplyRootMotion` 为 false）——当"固定画布上的原地 Sprite 动画"做。
- 动画状态由**代码按状态名 `Play`** 或**控制器 Trigger** 两种方式进入，字符串名是硬契约（见 §8）。

## 2. 状态 × 动画矩阵（主表）

| # | 形态 / 状态 | 进入条件（代码入口） | 玩法时长 | 播什么 | 循环 | 素材来源 |
|---|---|---|---|---|---|---|
| 1 | 持盾 Idle | 生成（`Initialize`）、移动结束、攻击收招、起身结束 | — | 持盾待机 | 是 | **已接入并验收**：`Assets/Animations/Enemy_1021_Idle.anim`；4帧，0.96s循环 |
| 2 | 持盾 Walk | 开始逐排补齐/前移（`SetTrigger("Walk")`） | 每排 `moveSpeed`（0.3s/排） | 持盾行进 | 是 | **已接入并获用户验收**：`Assets/Animations/Enemy_1021_Walk.anim`，6帧，0.6s循环 |
| 3 | 持盾 Attack | 攻击序列开始（`Play("Idle")` + `SetTrigger("Attack")`） | `spawnDuration 1.0` + `drawDuration 1.0`（当前无命中帧声明） | 稍侧身盾撞 | 否 | **已接入并获用户验收**：`Assets/Animations/Enemy_1021_Attack.anim`，12帧，2s非循环，1s接触姿态 |
| 4 | 受击（正面 / 未声明方向） | `TriggerHitFlash` → `PlayHitVisual(Front)` | clip 长 0.3667s | 正面挨打 | 否 | **已接入并获用户游戏验收**：`Assets/Animations/Enemy_1021_HitFlash.anim`，6帧、30fps、非循环；命中前会被卡肉延迟几帧 |
| 5 | ~~受击（盾侧 = Left）~~ | — | — | **并入 #7**：盾侧攻击在当前规则下必然被格挡，`HitLeft` 在持盾形态不可达（见 §7） | — | 不做 |
| 6 | 受击（反侧 = Right） | `PlayHitVisual(Right)` | 0.9s | 右侧挨打（无盾侧，全额） | 否 | **已接入、待用户游戏验收**：`Assets/Animations/Enemy_1021_HitRight.anim`，6帧、30fps、非循环，`HitRight → Idle` exit 0.9（与1011方向受击约定一致）；末帧仍接近持盾站姿 |
| 7 | **格挡命中**（盾侧 / 正面的普通与重攻击） | `ShieldGuardEnemy.PlayBlockVisual()` 覆盖普通受击动画 | 0.30s | 盾臂局部承力 + 盾牌小幅震荡；上身相对骨盆向画面右侧轻微倾斜，双脚/根位置固定；无后仰、无特效 | 否 | **已接入并获用户游戏验收**：`Assets/Animations/Enemy_1021_ShieldGuardHit.anim`，6帧、30fps、非循环 |
| 8 | 击飞 Rise | `Launch()` → `PlayLaunchVisual()` | 到落地为止 | 被挑飞起手 | 否 | 现复用 `Enemy_102_Launched`（1 帧，借 105 的帧） |
| 9 | 击飞 Fall | Rise 播完自动转移（`Launched_Rise → Launched_Fall`） | 循环至落地 | 空中下坠 | 是 | 现复用 `Enemy_102_Launched_Fall`（1 帧） |
| 10 | 起身 Getup | 落地后 `BeginGetup()`（**当前控制器无该状态 → 直接回 Idle**） | 代码兜底 0.6s | 爬起 | 否 | **缺失** |
| 11 | 眩晕 | `Stun()`（架势破防；1021 非 Boss） | `stunDuration = 1.5s` | 当前无专属动画（非 Boss 通用缺口） | — | 可选新增 |
| 12 | 持盾死亡 | HP 归零（AnyState → `Dead`，`Enemy.PlayDeadVisual()` 按名 `Play`） | 视觉段由 `DeathBounceAndFall()` 协程（0.3s 弹起＋0.8s 掉落）决定；clip 0.6s 非循环、末帧保持 | 持盾倒地 | 否 | **已接入并获用户游戏验收（2026-10-17）**：`Assets/Animations/Enemy_1021_Dead.anim`，3帧（f044/f046/f072）、30fps、0.6s、Clamp；源帧为已验收的 `dead_3frames_matted_v1/` |
| 13 | **破盾表现期** | 破盾瞬间 `Enemy.EnterShieldBrokenForm()` → `Play("ShieldBreak")` | **`breakAnimationDuration = 0.46s`** | 盾被击飞/脱手 + 本体踉跄（站立版） | 否 | **缺失**（现复用 `Enemy_102_CowardTransition`） |
| 14 | 胆怯 Idle | 掉盾动画播完（`CompleteShieldBrokenToCoward`）；胆怯形态移动结束 | — | 无盾缩身待机 | 是 | **已接入第一关、待游戏验收（2026-10-18）**：`Assets/Animations/Enemy_1021_CowardIdle.anim`，6 帧、30fps、0.96s 循环；素材为已验收的 `coward_idle_6frames_matted_v1/`。注意胆怯无 Walk 动画 |
| 15 | 胆怯 受击 | 胆怯形态受击（`PlayHitVisual` → `CowardHit`） | `CowardHit → CowardIdle` exit 1.0，**clip 长即表现时长** | 无盾受击（压缩→张开峰值→回待机） | 否 | **已接入第一关、待游戏验收（2026-10-18）**：`Assets/Animations/Enemy_1021_CowardHit.anim`，30fps、0.2666s、4 姿势键（待机 0 / 压缩 0.0667 / 张开 0.1333 / 待机 0.2）＋末键 0.2333；素材为待机帧＋已认可的 v6/v8 生成图 |
| 16 | 胆怯 击飞 | `IsCoward` 时 `PlayLaunchVisual()` → `CowardLaunched` | 到落地为止（`launchDuration = 2.0`） | 无盾被挑飞 | 是 | 现复用 `Enemy_102_CowardLaunched`（复用了死亡帧）。已排期：默认做 **2.0s Clamp 长保持 clip**；其 `→ CowardIdle` exit 1.0 出边会提前切走，见 `plan/enemy1021-coward-family-plan.md` §4.2 |
| 17 | 胆怯 死亡 | `IsCoward` 时 `Die()` → `CowardDead` | 末帧保持 | 无盾倒地 | 否 | 现复用 `Enemy_102_CowardDead`；已排期，目标 0.6s 非循环、末帧保持，不直接复用持盾 Dead 的帧 |

补充说明：

- **#7 与 #4/#6 是互斥的**（#5 已并入 #7）：`PlayBlockVisual()` 在受击动画分支**之后**执行，所以"格挡"会覆盖本帧的普通受击表现；两者不要做成同一套帧。
- **#5/#6 的意义**是本机制的核心可读性：方向决定"挡不挡"，动作就必须看得出"从哪边打来的"。
- **#13 的时长是接口**：`breakAnimationDuration` 决定代码什么时候切 `CowardIdle`；clip 长度必须与它一致（或改字段），否则会出现"动画没播完就被切走"或"杵在原地不动"。
- **#13 期间普通受击不播受击动画**（代码抑制），因此这段 clip 里**不需要**受击变体帧；但被击飞/死亡仍会被 AnyState 切走（见 §5 第 6 条）。
- **#10 是现状缺口**：`Enemy.PlayGetupVisual()` 会尝试 `Launched_Getup`（非胆怯）/ `CowardGetup`（胆怯），两个状态在 1021 控制器里都不存在；`HasGetupAnimatorState()` 因此为假，落地直接回 Idle/胆怯 Idle。
- **#16 目前复用死亡帧**（`CowardLaunched` 用的是 `Enemy2_Coward_Dead_01..03`），读感上"被打飞"和"已阵亡"难以区分。

## 3. 素材清单

### 3.1 102 占位素材（Idle/Walk/Attack已替换，其余仍复用）

| 用途 | 资产 | 实际内容 |
|---|---|---|
| 旧持盾 Idle/Walk/Attack | `Enemy_102_Idle.anim`、`Enemy_102_Walk.anim`、`Enemy_102_Attack.anim` | 1021这三个状态已改接正式1021素材；102原资产保留不变 |
| 持盾受击 / 死亡 | `Enemy_102_HitFlash.anim`（0.33s，实质1张 `enemy2_hitted`）、`Enemy_102_Dead.anim`（0.10s，1帧） | 1021的受击与持盾死亡已改接正式 `Enemy_1021_HitFlash.anim` / `Enemy_1021_Dead.anim`；102原资产保留不变 |
| 击飞 | `Enemy_102_Launched.anim` / `_Launched_Fall.anim` | 各 1 帧，借用 `enemy5_launch` |
| 破盾（掉盾） | `Enemy_102_CowardTransition.anim` | **1021 已改接正式 `Enemy_1021_ShieldBreak.anim`（50fps/0.46s，本体6帧＋末帧保持；盾改由独立对象接管）**；本行是 102 原资产，保留不变 |
| 胆怯 | `Enemy_102_CowardIdle.anim`（0.42s 循环）、`_CowardHit.anim`（0.26s）、`_CowardDead.anim`（0.30s）、`_CowardLaunched.anim`（0.30s） | 胆怯 Idle/Hit 各 3 键 + `ShieldVisual` 的 **null 键**（显式清掉盾）；分帧精灵在 `Assets/Sprites/Enemy/Enemy2/Coward/` |

### 3.2 建议新增（命名 `Enemy_1021_*`）

| 用途 | 建议命名 | 建议规格 |
|---|---|---|
| 持盾待机 | `Enemy_1021_Idle` | **已完成：4帧循环，0.96s，30fps，键位0/0.24/0.48/0.72s，末帧0.9267s短暂保持**；盾的位置在循环内保持稳定；来源为f001/f025/f049/f073的SVL透明帧 |
| 持盾行进 | `Enemy_1021_Walk` | **已完成并验收：6帧、0.6s循环、30fps**；沿用moveSpeed调制，不重复制作 |
| 持盾攻击 | `Enemy_1021_Attack` | **已完成并验收：12帧、2s非循环、30fps，接触姿态键1s**；沿用前摇1s/收招1s，无命中帧声明 |
| 正面受击 | `Enemy_1021_HitFlash` | **已完成并验收：6帧、30fps、0.3667s非循环**；末帧接近持盾站姿，含 `ShieldVisual` 清空键 |
| 右受击 | `Enemy_1021_HitRight` | **已接入、待游戏验收：6帧、30fps、0.9s非循环**；用户自行抠图后按固定偏移补边部署，含 `ShieldVisual` 清空键 |
| **格挡命中** | `Enemy_1021_ShieldGuardHit` | **已完成并验收：6帧、0.30s非循环**；首帧即“盾面接触”（卡肉只有 45ms），末帧回站姿（出边 exit time 1 → Idle） |
| **破盾（站立版）** | `Enemy_1021_ShieldBreak` | **已完成并接入、待游戏验收：50fps、0.46s 非循环**；六帧键位 0/0.06/0.12/0.22/0.32/0.40＋末帧保持 0.44；`ShieldVisual` null 键；0.12s 事件生成独立盾、0.46s 事件收尾进胆怯。长度 == `breakAnimationDuration` |
| 击飞三阶段 | `Enemy_1021_Launched_Rise` / `_Launched_Fall` / `_Launched_Getup` | Rise ≈0.5–0.6s 非循环、Fall 循环（≈0.45–0.6s）、Getup ≈0.6s 非循环 |
| 持盾死亡 | `Enemy_1021_Dead` | **已接入并获用户游戏验收：3帧、30fps、0.6s 非循环（末帧保持）**；键位 0/0.2/0.4＋保持键 0.5667，`ShieldVisual` null 键；源帧 960→固定偏移 `(74,91)` 补边 1108 |
| 盾掉落（独立对象） | `Assets/Prefabs/Effects/Enemy_1021_ShieldDrop.prefab` | **已实现并接入、待游戏验收**：由 0.12s 事件在脱手帧生成，读取敌人姿态一次后不跟随移动/形变，0.85s 后自毁；脚本 `Assets/Scripts/Effects/ShieldDropEffect.cs` |
| （可选）眩晕 | `Enemy_1021_Stun` | 非 Boss 目前无眩晕表现，属通用缺口 |

美术规范要点（同规范文档）：同一动作画布尺寸统一、脚底锚点固定、最大后仰/展开时不越框、像素密度与描边一致、禁止镜头运动与整体平移。

**已部署导入参数**：源/部署 PNG 为 1108×1108 画布，透明帧由960×960原帧以固定偏移 `(74,91)` 零重采样补边；`Texture Type = Sprite`、`Sprite Mode = Single`、`PPU = 16`、`Filter = Point`、`Compression = Uncompressed`、`Alpha Is Transparency = true`、`Mipmaps = false`、`Mesh = Tight`、`Pivot = Center`。Prefab scale 为 `0.090161`，使平均可见高约3.15世界单位；碰撞盒和血条按世界尺寸补偿。

## 4. Animator 结构

现状：`Assets/Animations/Enemy_1021.controller`（复制自102）已有15个状态、6个Trigger参数；Idle、Walk、Attack、Dead 分别引用 `Enemy_1021_Idle.anim`、`Enemy_1021_Walk.anim`、`Enemy_1021_Attack.anim`、`Enemy_1021_Dead.anim`（其余正式动作为 `Enemy_1021_HitFlash/HitRight/ShieldGuardHit/ShieldBreak`）。每次接入只替换目标状态的 motion，原转场保留。

参数：`Attack`、`Launch`、`Dead`、`Hit`、`ReRise`、`Walk`（全 Trigger）。默认状态 `Idle`。

现有转移（实测）：

```
Idle   -> Attack          [Attack]        + -> Launched_Rise [Launch, exit 0.9]
                                          + -> Dead [Dead, exit 0.9] + -> HitFlash [Hit] + -> Walk [Walk, exit 0.9]
Attack -> Idle            (exit 1.0)      + -> HitFlash [Hit]
HitFlash -> Idle          (exit 0.9)
Launched_Rise -> Launched_Fall (exit 1.0)
Launched_Fall -> Idle     [ReRise]
CowardTransition -> CowardLaunched (exit 1.0)
CowardHit -> CowardIdle   (exit 1.0)
CowardLaunched -> CowardIdle (exit 1.0)
ShieldGuardHit -> Idle    (exit 1.0)      ← 本次新增
ShieldBreak               (无出边，代码计时后切 CowardIdle) ← 本次新增
AnyState -> Dead [Dead]、AnyState -> Launched_Rise [Launch]
无出边的状态：Walk、CowardIdle、Dead（末帧保持）
```

需要补的状态与边（与 §3.2 一一对应）：

1. ~~`HitRight`~~ **已接入**（由 `Enemy.PlayHitVisual` 按名 `Play`）：`Enemy_1021_HitRight.anim` 0.9s、非循环，出边 `HitRight → Idle` hasExitTime、exit 0.9、duration 0；**`HitLeft` 不做**（盾侧表现并入 `ShieldGuardHit`，见 §7）。
2. `Launched_Getup`（`HasGetupAnimatorState()` 依赖状态名 `Base Layer.Launched_Getup`；补上后才会有起身表现；`CowardGetup` 可选，若不补则胆怯落地直接回 `CowardIdle`）。
3. `ShieldGuardHit`、`ShieldBreak` 已就位，把 motion 换成正式 clip 即可（不用改代码）。
4. Trigger `Walk` 的进入边位于 `Idle`；`ShieldBreak` / `ShieldGuardHit` 没有入边，**只能由代码 `Play`**——这是刻意的（破盾与格挡都不该被其它 Trigger 抢走表现）。

## 5. 时间对齐规则（重要）

1. **玩法时长权威**：`breakAnimationDuration`（0.46s）决定"掉盾动画播到什么时候切胆怯"；要延长动画就同步改这个字段（Inspector），或把默认值改成 clip 的实际长度。
2. **命中卡肉会禁用 Animator**：`Enemy.ApplyHitStop` 在命中瞬间把 `_animator.enabled = false`（Standard 0.09s / Heavy 0.14s / **格挡走 Light 0.045s**），所以任何"命中那一帧被 `Play` 的状态"都要等卡肉结束才出现在画面上。推论：**格挡动画的首帧必须是"盾面已经接触"**，否则读不出"当"的那一下。
3. **攻击动画长度会覆盖收招时长**：若在攻击步里声明了命中帧（`AttackStep.hitFrames`），`Enemy` 会用"clip 长度 − 起手时长"作为 `drawDuration`，并让出伤时刻落在命中帧精灵出现的时刻；当前 1021 的配置**没有**声明命中帧（沿用 `spawnDuration/drawDuration`），所以新攻击 clip 的长度不会影响出伤——若要"刀到人才掉血"，需要同时声明命中帧。
4. **Walk 的播放速率被 `moveSpeed` 调制**（`_animator.speed = max(1, 0.6/moveSpeed)`）：不要把 clip 长度当作"每排耗时"，每排耗时是 `moveSpeed = 0.3`。
5. **所有状态都可能被外部切走**：死亡与击飞是 AnyState 转移，随时会打断当前表现 → 关键姿态（盾面接触、盾脱手）不要放在 clip 尾部。唯一例外是 `ShieldBreak`：它的尾部本身就是接口（"无盾站姿"），用来衔接 `CowardIdle`。
6. **破盾表现期内可能发生补齐位移**：那段 0.46s 里若前排被清空，敌人会平移补齐，画面上会看到"掉盾姿态平移"。属可接受的边缘情况；若要在意，需要在计时内推迟补齐（不必为最小版加复杂度）。

## 6. 盾的表现方案（方案 A 已落地：Idle/Walk/Attack/HitFlash/ShieldBreak 已接入，独立掉盾接管已实现）

用户已确认正式制作方案：**持盾时盾画在角色本体帧内；破盾动画表现脱手，之后由引擎驱动独立盾对象掉落。初版保持已举盾应战的 Idle，不制作举起/收回盾的状态切换。** 人物以 1011 为基底改剑为盾，具体体型、头盔、装备、盾形与位置以已验收 `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3.png` 为准。盾位于画面左侧、胸腹前方，角色右手/手指/前臂/握把藏在盾后；画面右侧左手保持空垂，头盔、面罩与眼光可读。脱手帧、本体中盾的消失与独立对象的接管必须协调，避免双盾、闪没或重新出现；具体时间点留待制作/部署时确认。

**方案 A 已落地（2026-10-15）**：持盾与脱手前仍把盾**画在本体帧内**（Idle/Walk/Attack/HitFlash/Dead 只绑 root）；`Enemy_1021_ShieldBreak` 从第 3 帧起本体无盾，0.12s 事件生成独立世界对象接手下落，末帧为无盾姿态。旧 `ShieldVisual` 分离层不再作为持盾/掉盾路线：Prefab 上该子渲染器已清空（sprite 为空、transform 复位），并在池复位中显式清空。

方案对照（A 已选，B 仅保留为历史比较）：

| 方案 | 做法 | 代价 |
|---|---|---|
| **A. 帧内绘制（已选）** | 持盾及破盾脱手前盾画在本体帧里；脱手后由引擎驱动**独立世界对象**掉落 | 每个持盾动作都要画盾；脱手接管与独立掉落对象生命周期须验证，避免旧分离持盾层的排序、锚点和池复位债务 |
| B. 分离层 | `ShieldVisual` 子物体由所有 clip 驱动 | 每个 clip 都要给盾打 sprite 键 + 位置/旋转对齐；还要处理排序与复位（见下） |

旧分离层方案的池复位债务：`ShieldVisual` 只有胆怯家族 clip 会写 null 键，且当前代码没有复位；若沿用旧 `CowardTransition` 位置曲线，破盾期死亡或回池可能留下悬空盾。正式制作已选择方案 A，不把 Animator 驱动的 `ShieldVisual` 分离层作为持盾/掉盾表现路线。实现方案 A 时仍须核对占位节点与旧 clip 是否会残留或重新显示，并验证独立掉落对象的销毁/回收和敌人对象池复用。

已知的 B 方案债务（仅在继续使用分离层时才需处理）：`ShieldVisual` 的 `sortingOrder = 1` 是固定值，而 `Enemy` 没有任何排序逻辑（敌人本体靠世界 Z 排序）→ 盾会跨排压过其他敌人本体；分离帧是紧裁剪（115×214 等）与 512×512 画布不同锚点体系，全靠位置曲线对齐。

## 7. 方向与变体规则

| 变体 | 数量 | 选择逻辑 |
|---|---|---|
| 受击（全额） | 2（`HitFlash`：未声明来源；`HitRight`：反侧） | `Enemy.PlayHitVisual(HitReactionDirection)`；缺失时回落 `HitFlash`。DoT / Launch / 破盾表现期不播受击 |
| 格挡（减伤） | 1（`ShieldGuardHit`） | 盾侧与正面共用；**盾侧的受击表现并入这里**（理由见下） |
| 其余状态 | 无方向变体 | Idle / Walk / Attack / Dead / Launched* / Coward* / ShieldBreak |

方向语义见主文档 §4.1（`Left` = 力来自画面左 = 盾侧）。

**为什么盾侧受击与格挡命中可以同一动画（已定）**：在当前规则的 `Normal` 形态里，来源侧为 `Left` 的攻击一律被格挡（普通与重攻击都不破盾），且 `PlayBlockVisual()` 会覆盖当帧的普通受击动画；盾消失后走胆怯表现（`CowardHit`）。因此 `HitLeft` 在持盾形态**不可达**，不存在"同一动画对应两种结果"的歧义。

**什么时候需要重新拆开**：若以后放宽规则，让左侧攻击出现"不被挡却全额"的情况（例：`heavyFromShieldSideBreaks = true` 的左侧破盾、或左侧传入无方向/被忽略的伤害），则必须补一个与格挡明显不同的左侧受击动画，否则玩家会把"全额"读成"被挡"。

**画面语言约束（2026-10-11用户修正）**：角色右手持盾（画面左侧），接住来自画面左侧的攻击，盾牌产生小幅位移，身体不后仰。K1承力极值须清楚表现上身从骨盆向画面右侧轻微倾斜：头盔与肩部相对骨盆向右偏移，持盾侧肩上提内收、肘部折叠，双膝承力弯曲，双脚和根位置固定；最终以画面上能读出上身相对骨盆倾斜、且非整体平移为准。躯干仍正面朝向，不后仰。2026-10-11用户认为K1 v3已接近目标，并提出只增加轻微缩颈表现受冲击；下一版以v3为唯一姿态基准，仅将头部/头盔沿颈轴向领口稍下收、肩甲轻微耸起，形成颈肩压缩，保留v3已有躯干倾斜、盾位和四肢，不再扩大倾斜或改变其他动作。K2只在K1通过后再设计为盾臂释放、盾牌向画面左侧回位、上身由右倾向直立恢复但保留少量右倾，膝盖部分伸展、空手轻微滞后。关键帧姿态必须与Idle肉眼可辨地不同。动作素材禁止火花、闪光、木屑、裂纹、震动线、残影或任何其他特效。仍共用盾侧与正面的格挡状态，不画成被打飞或失去平衡。v1因身体姿态未改变且有特效被否决；v2虽表现盾位变化，但肢体变化仍未达要求，不能作为验收帧。

## 8. 接线落点与约束

- **字符串状态名是硬契约**（代码按名 `Play`，缺失才回落）：`Idle`、`Walk`、`Attack`、`HitFlash`、`HitRight`、`ShieldGuardHit`、`ShieldBreak`、`Launched_Rise`、`Launched_Fall`、`Launched_Getup`、`Dead`、`CowardIdle`、`CowardHit`、`CowardDead`、`CowardLaunched`、（可选）`CowardGetup`。
- `Enemy.PlayHitVisual` 对非胆怯形态会先查 `Base Layer.HitLeft/HitRight` 是否存在，不存在才回落 `HitFlash` → **新增状态不需要改代码**（本次只补 `HitRight`；`HitLeft` 虽在代码查表里，但持盾形态不可达，见 §7）。
- `Enemy.EnterShieldBrokenForm` 与 `ShieldGuardEnemy.PlayBlockVisual` 都先查状态是否存在再 `Play` → 换正式 clip 同样不需要改代码。
- `HasGetupAnimatorState()` 只认 `Base Layer.Launched_Getup`；补 `Launched_Getup` 后起身流程自动生效（`GettingUp` 状态、0.6s 兜底）。
- 刷新状态名/新增状态都在 `Assets/Animations/Enemy_1021.controller` 里做；**不要修改** `Enemy_102.controller` 与 `Enemy_102_*.anim`（102 是实验品，保持不动）。
- 素材引用全部用 Inspector/Animator 引用，不加 `Resources.Load` 或字符串资源查找。
- 盾（若走分离层）只能有一个 sprite 写入者；不能同时由 Animator 与代码写。

## 9. 验收参考（路线节点 A / `N1_ArtPreview_Battle`）

默认由用户开游戏验收；以下清单不是自动自检授权。只有用户在对话中明确要求时，代理才可运行对应检查。

| 看什么 | 期望 |
|---|---|
| 出场与待机 | 持盾 Idle 循环，盾的位置稳定；脚底与血条不跳 |
| 行进 | Walk 循环与每排 0.3s 的位移同步，不滑步 |
| 攻击 | Attack 播完回到 Idle；若声明了命中帧，出伤与命中帧同帧 |
| 正面/盾侧被打 | 播"格挡"动作（盾面接住），伤害被固定减伤 |
| 反侧被打 | 播对应方向受击（`HitRight`），全额伤害 |
| 破盾 | `ShieldBreak` 从 Idle/受击姿态无缝接上；盾脱手清楚可读；结束瞬间切到胆怯 Idle，不闪帧 |
| 破盾中被打 / 被挑飞 / 死亡 | 普通受击不打断掉盾动画；被击飞或死亡立刻切走（AnyState），无残留姿态 |
| 胆怯 | Idle/Hit/Dead/Launched 之间切换无"盾又出现"的闪帧（`ShieldVisual` 必须保持清空） |
| 连续两波 / 对象池复用 | 新实例从头持盾；**没有半空的盾残留** |
| 击飞落地 | 若有 `Launched_Getup`：落地后起身再回 Idle；没有则直接回 Idle（现状） |

## 10. 待确认（需要拍板再开工）

1. **盾的方案已落地**：盾画在本体帧内，破盾动画脱手后由引擎驱动独立对象掉落（§6）；已于 2026-10-15 随 `ShieldBreak` 接入实现，待游戏验收。
2. **持盾姿态已验收**：初版为 v3 成图的已举盾应战 Idle（持盾手藏于盾后、副手空垂），不制作举起/收回盾的状态切换；没有据此新增命中部位判定或玩法状态。
3. **胆怯是否需要起身**：补 `Launched_Getup`（非胆怯形态落地起身）与 `CowardGetup`（胆怯形态）？
4. ~~是否需要方向受击~~ **已定**：要 `HitRight`（反侧 = 全额，必须看得出没被挡）；`HitLeft` 不做（当前规则下不可达，盾侧表现并入 `ShieldGuardHit`，见 §7）。
5. **眩晕表现**：非 Boss 目前无眩晕动画（属全敌人通用缺口），1021 是否单独补？
6. **盾掉落已实现**：由引擎驱动独立对象（`Enemy_1021_ShieldDrop.prefab`），不继续用本体 clip 的盾位置曲线；接管帧为 `ShieldBreak` 的 0.12s 事件，生命周期 0.85s 自毁；层级与轨迹仍待游戏内确认。
7. **形象单帧已验收**：以 1011 人物为基底改剑为盾；实际生成参考为 1011 Idle1 浅灰副本 + 用户盾牌局部构造/材质参考，不再使用 109。后续动作统一沿用 `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3.png`，不按早期头身比或盾形数字另行重设计。实际输入、参数和阶段边界见 `plan/enemy1021-art-production-handoff.md`。

## 11. 变更日志

- 初版：按已实现的最小可玩机制（`Normal / ShieldBroken / Coward` + 四值方向判定）产出状态 × 动画矩阵、素材清单（现复用项与建议新增项）、Animator 现状与待补状态、时间对齐规则、盾的表现方案、方向变体规则、接线约束与验收清单；第 10 节 6 项待用户拍板。
- 早期用户确认：A 型宽矩形木盾、空手副手、固定持盾姿势；视频/本体帧内绘盾，脱手后由引擎驱动独立掉落对象。当时只更新设计记录，未生图/视频或修改代码/资产。
- 本轮 Idle 定妆验收：用户确认 `enemy1021_shield_hold_v3.png` 的 Idle 形象通过，作为后续动作身份锚点；同步以 1011 改剑为盾、持盾手遮挡和副手空垂的实际方案。仅单帧形象通过，正式 Idle 动画、透明帧、对齐与部署仍待制作；本轮未改代码/Unity 资产。
- 2026-10-10 Idle 素材部署：找到其他AI完成的 `Library/Locus/tmp/enemy1021_idle_v1_frames/keyed_ui_t79/` 四帧，依据 `svl_matte_batch_report.json` 核对 RGBA、半透明0、幕布残留0。四帧固定偏移 `(74,91)` 零重采样补到1108，写入 `Assets/Sprites/Enemy/Enemy1021/`；创建 `Assets/Animations/Enemy_1021_Idle.anim`，接入 `Enemy_1021.controller` 的 Idle 状态。Prefab scale改为0.090161，碰撞盒/血条世界尺寸匹配101基准；Idle clip含 `ShieldVisual` null键清旧分离盾。Play Mode已验证自然生成、四阶段循环、旧盾清空、对象池复用，Console错误0。详细报告见 `Library/Locus/tmp/enemy1021_idle_deploy_v1/deploy_report.json`。
- 2026-10-11 Walk与Attack交接：Walk六帧、0.6秒循环已接入并验收；Attack十二帧先选帧验收，再经SVL抠图与整帧尺度/脚底对齐v2验收，按 `(74,91)` 补边后接入第一关。Attack为2秒非循环、1秒接触姿态键，用户已验收游戏表现。本次仅同步文档，不改机制/数值或工程资产，不启动运行自检；后续在其他对话继续未完成动画与机制。
- 2026-10-14 进度同步（ShieldGuardHit/HitFlash完成）：ShieldGuardHit六帧与HitFlash六帧先后接入第一关并获用户游戏验收；§2矩阵、§3素材清单与顶部状态行同步为已完成，HitRight标为未接入/透明候选待验收，ShieldBreak与死亡/击飞/胆怯仍为102占位。仅同步事实状态，未改代码、控制器或数值；剩余清单见 `plan/enemy1021-art-production-handoff.md` §0.2。
- 2026-10-14 HitRight接入：用户自行对已验收六帧完成抠图并授权接入。新增 `Assets/Animations/Enemy_1021_HitRight.anim`（30fps、0.9秒、非循环、6帧+末姿态保持键）与控制器 `HitRight` 状态（`HitRight → Idle` exit 0.9、duration 0），与1011已验收的方向受击约定一致；§2第6行与§3.2同步为已接入、待游戏验收。本次未改其他状态、规则或数值。
- 2026-10-17 持盾死亡接入：用户已验收的 3 帧透明素材（`dead01_f044` / `dead02_f046` / `dead03_f072`，960×960 RGBA，T=80）按固定偏移 `(74,91)` 零重采样补边到 1108，新增 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_dead1..3.png`（导入设置与 `Enemy_1021_hitFlash1` 逐项一致）与 `Assets/Animations/Enemy_1021_Dead.anim`（30fps、0.6s、Clamp、键位 0/0.2/0.4＋保持 0.5667、`ShieldVisual` null 键），控制器仅替换 `Dead` 状态的 motion，18 条转场与其余 14 个状态 motion 未变。未进入 Play、未采样动画；用户随后开游戏确认持盾死亡表现通过（2026-10-17）。核验记录 `Library/Locus/tmp/enemy1021_dead_deploy_v1/asset_verification.json`。
- 2026-10-15 ShieldBreak 接入：新建 `Enemy_1021_ShieldBreak.anim`（50fps/0.46s，0.12s 脱手事件＋0.46s 结束事件）、`Enemy_1021_CowardIdle_Hold.anim`（`CowardIdle` 静态兜底）、独立盾 Prefab 与 `Assets/Scripts/Effects/ShieldDropEffect.cs`；`Enemy.cs`/`ShieldGuardEnemy.cs` 补脱手、打断与池复位；旧 `ShieldVisual` 清空。素材按单一全局缩放 1.20＋脚底对齐部署（源六帧比 Idle 小约 20%，**非精确标定**）。静态核验通过，未进入 Play、未采样动画，待用户游戏验收。接入流程见 `skill/workflows/action-to-unity-deployment.md`。
