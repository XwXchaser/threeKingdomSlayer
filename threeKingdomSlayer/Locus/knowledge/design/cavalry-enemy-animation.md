---
id: kd_8789bec9-537e-4a51-a14f-c0bbf33d1fee
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 骑兵敌人（109）动画制作交接：状态 × 动画矩阵

本文件是 109 骑兵的**动画制作输入**。规则与状态机以 `design/cavalry-enemy.md` 为准，本文件只回答"每个状态需要什么动画、时长对不齐时谁让谁、素材放哪、怎么接线"。

制作规范遵循 `design/pixel-character-and-enemy-animation-spec.md`（统一画布与脚底锚点、禁止整体平移、四周预留 15% 边距、动作按"起手 → 蓄力 → 命中 → 收势"）。

## 1. 驱动的状态机

- 骑兵阶段：`CavalryPhase = MountedIdle / Windup / Charging / Striking / Retreating / RetreatBlocked / Dismounted`（`Assets/Scripts/Enemy/CavalryEnemy.cs`）。
- 正交的通用敌人状态：`Dead / Launched / HitFlash`（来自 `Enemy`）；**骑乘期间不会进入 `Stunned`**（免控），落马后才恢复。
- 位移全部由代码驱动（协程 lerp 根 `transform.localPosition`）；动画**不允许**带位移或 root motion（`Animator.m_ApplyRootMotion` 已为 false）。请把它当成"固定画布上的原地 Sprite 动画"。

## 2. 状态 × 动画矩阵（主表）

| # | 阶段 / 状态 | 进入条件 | 玩法时长（当前参数） | 骑手（马上的 101） | 坐骑（105） | 循环 | 素材来源 |
|---|---|---|---|---|---|---|---|
| 1 | MountedIdle（待机 + 冷却等待） | 初始化完成、返航结束、冷却中 | `chargeCooldown = 1s` | 101_Idle | 105 idle（轻微起伏） | 是 | 复用 |
| 2 | 入场前移（SpawnEntry，属 `Enemy` 的 rush 阶段） | 波次生成后列管理器派单 | 每排 `Enemy.moveSpeed`（0.2s/排） | 101_Walk + 骑乘偏移 | 疾驰 loop | 是 | 复用 + 新增 |
| 3 | Windup（预备，可被打断） | 判定通过、冷却结束 | `windupDuration = 0.3s` | 举枪前倾蓄势（新） | 低头刨地 / 前蹄蓄势（新） | 否 | 新增 |
| 4 | Charging（前移，可被打断） | 前摇结束 | 每排 0.2s，共 1–4 排（0.2–0.8s） | 冲锋姿态（前倾持枪） | 疾驰 loop（≥4 帧） | 是 | 新增 |
| 5 | Striking（命中前探 → 结算 → 收招） | 前方有敌人或抵达 row=0 | `strikeWindup 0.18s` + `strikeRecover 0.22s` | 命中帧落在最前端，收招回骑乘姿 | 前冲急停（新） | 否 | 新增 |
| 6 | StrikeInterrupted（命中前被打断的收招） | 可打断窗口内被标记攻击命中 | `interruptRecover = 0.2s` | 收招（复用 Striking 后半段） | 急停 / 踉跄（新） | 否 | 新增 |
| 7 | Retreating（返航） | 命中结算完成或被打断 | 每排 0.2s，最多 4 排 | 后退姿态（**不做 180° 转身**） | 倒退走 loop（2–4 帧） | 是 | 新增 |
| 8 | RetreatBlocked（返航受阻等待） | 返航目标排被占 | 不定（每帧重试） | 101_Idle | idle 或小幅踱步 | 是 | 复用 |
| 9 | 落马过渡（`Dismount` 执行中） | 真正进入 `Enemy.Launch()` | 与击飞同步 | 离座 + 101_Launched_Rise → Fall | 侧移淡出后隐藏（0.2–0.35s） | 否 | 复用 + 新增 |
| 10 | Dismounted（≈101 杂兵） | 落马结束 | 普通敌人逻辑 | 101 全套（Idle/Walk/Attack/HitFlash/Dead/Launched） | 无（已隐藏） | 视动作 | 复用 |
| 11 | Dead（骑乘中死亡） | HP 归零 | 一次性 | 101_Dead | 待定（见第 6 节） | 否 | 复用（坐骑待定） |
| 12 | HitFlash（受击反应） | 任意受击（非致命） | `HIT_FLASH_DURATION = 0.15s` | 101_HitFlash | 105 hitted（复用 `enemy5_hitted`） | 否 | 复用 |

补充说明：

- 第 2 行的"入场前移"用的是普通补齐的 101 动作；骑兵还没进入自己的循环（`IsCavalryReadyForCombat` 为假），入场结束后还有 `spawnEntrySettleDuration = 0.35s` 才允许判定。
- 第 3/4 行的可打断窗口包含"前摇 + 前移"，被标记攻击命中会立刻跳第 6 行，**本次不结算任何伤害**。
- 第 5 行的伤害在**前探结束那一刻**提交，提交后不可再取消；请把命中帧放在前探末端。
- 第 7/8 行合成一个读感："往回跑；被堵就停住等"。
- 105 的三张攻击帧（`enemy5_attack1/2/3`）**不要**用：坐骑不做攻击，也不引入 105 的远程逻辑。

## 3. 素材清单

可复用（现成）：

| 用途 | 资产 |
|---|---|
| 骑手动作 | `Assets/Animations/Enemy_101.controller`、`Enemy_101_Idle/Walk/Attack/HitFlash/Dead/Launched/Launched_Rise/Launched_Fall.anim` |
| 坐骑静帧与受击 | `Assets/Sprites/Enemy/Enemy5/enemy5_idle.png`、`Enemy_5_walk1.png`、`Enemy_5_walk2.png`、`enemy5_hitted.png`、`enemy5_dead.png` |
| 坐骑参考 | `Assets/Animations/Enemy_105.controller`（仅作参考，不要把它的远程逻辑接到坐骑） |

建议新增（命名按项目既有风格，`Enemy_109_*`）：

| 用途 | 建议命名 | 建议规格 |
|---|---|---|
| 蓄势 | `Enemy_109_Rider_Windup.anim` | 3–4 帧，原地 |
| 冲锋循环 | `Enemy_109_Rider_Charge.anim` | 4–6 帧循环，起伏明显 |
| 命中 + 收招 | `Enemy_109_Rider_Strike.anim` | 5–6 帧，命中帧在 45% 处 |
| 后退循环 | `Enemy_109_Rider_Retreat.anim` | 3–4 帧循环 |
| 坐骑疾驰循环 | `Enemy_109_Mount_Gallop.anim`（或 sprite 序列 4–6 张） | 4–6 帧循环，四蹄动作 |
| 坐骑蓄势 | `Enemy_109_Mount_Windup.anim` | 3 帧，原地低头/刨地 |
| 坐骑急停 | `Enemy_109_Mount_Brake.anim` | 3 帧，原地 |
| 坐骑倒退 | `Enemy_109_Mount_Back.anim`（或 sprite 2–4 张） | 2–4 帧循环 |
| 坐骑落马淡出 | 可直接用 `enemy5_dead` 或单独一张 | 1–3 帧 + 代码淡出 |

美术规范要点（同规范文档）：同一动作画布尺寸统一、脚底锚点固定、最大后仰/展开时不越框、像素密度与描边一致、禁止镜头运动与整体平移。

## 4. Animator 结构建议

骑手（推荐）：

- 新建 `Assets/Animations/Enemy_109.controller`，从 101 复制后重定向绑定，追加上表第 3–7 行的 4 个状态；**不改动** 101/102 的 controller 与 clip。
- 保持"根 Animator 是精灵来源、`CavalryVisualController` 把根 `SpriteRenderer` 的 sprite/颜色/材质镜像到 `riderRenderer`"的现有结构（根 Renderer 关闭）。
- 参数建议：int `Phase`（0=MountedIdle, 1=Windup, 2=Charging, 3=Striking, 4=Retreating, 5=RetreatBlocked）或 bool `IsCharging/IsRetreating`；trigger `Strike`、`Interrupted`、`Dismount`。
- Transition：关闭 `Has Exit Time`，交叉淡入 0.05–0.1s；不要用动画长度反向决定阶段时长。

坐骑（二选一，不要并行）：

- A. 继续纯 sprite 驱动（当前方案）：扩展 `CavalryVisualController` 里现有的 `mountIdle/mountWalk1/mountWalk2 + walkFrameDuration` 翻帧方式，按阶段取帧序列。改动最小、无 Animator 开销。
- B. 给 `MountVisual` 挂一个**纯视觉** Animator（只有 SpriteRenderer，不含 Enemy/碰撞/血条/发箭），由 `CavalryVisualController` 用 trigger/state 驱动。动画表现更强，但需要新增字段并在落马时确保它同步停止。

## 5. 时间对齐规则（重要）

1. **玩法时长是权威**：`windupDuration`、`strikeWindup`、`strikeRecover`、`interruptRecover`、`chargeCooldown`、`Enemy.moveSpeed` 都是 Inspector 参数；动画不得改变它们。
2. 动画比时长长/短时，用 **`Animator.speed` 调制**而不是改玩法时长。项目已有先例：`Enemy` 的 Walk 用 `_animator.speed = max(1, 0.6/moveSpeed)`；QTE Sweep 也用 speed 调制同步判定窗口（见 `design/qte-sweep-design.md`）。
3. `Striking` 的命中帧必须落在 `strikeWindup`（0.18s）处——伤害在该时刻提交且之后不可取消。
4. 前移/返航每排耗时 = `moveSpeed`（0.2s/排），疾驰与倒退循环的播放速率按此换算，保证"一格 ≈ 一个循环步"的读感。
5. 所有阶段都可能被外部订单或落马打断，clip 必须能在任意帧被切走：不要依赖"播完才有意义"的尾部姿态，重要姿态（如命中帧）要落在 clip 中部。

## 6. 待确认（需要拍板再开工）

1. **骑乘中死亡**：坐骑与骑手一起倒地，还是坐骑原地淡出、骑手单独播放死亡？（当前占位是"骑乘则一起淡出"）
2. **坐骑方案**：走 A（sprite 翻帧，改动小）还是 B（独立纯视觉 Animator，表现强）？
3. **冲锋表现层**：是否需要尘土/速度线/镜头轻微反馈？这些属于特效层，需要时我按项目现有特效体系单独出方案。
4. **调试标签**：`CavalryVisualController.showDebugState` 是否在正式版关闭（当前开启，便于验收）。

## 7. 接线落点与约束

现有字段（`Assets/Resources/EnemyPrefabs/Enemy_109.prefab` 的 `CavalryVisualController`）：`mountAnchor`、`mountRenderer`、`mountIdle`、`mountWalk1`、`mountWalk2`、`riderAnchor`、`riderRenderer`、`sourceRenderer`、`debugLabel`、`showDebugState`、`mountedRiderOffset`、`dismountedRiderOffset`、`walkFrameDuration`。

可能需要新增的字段（按第 6 节拍板后确定）：`mountWindup`、`mountCharge`、`mountBrake`、`mountBack`、`mountHit`、`mountFadeDuration`（落马淡出），或 A/B 方案二选一所需的 `mountAnimator`。

硬约束：

- 105 节点只允许含 `SpriteRenderer`（+ 可选独立纯视觉 Animator）：不得含 `Enemy`、碰撞体、血条、掉落、对象池注册或发箭逻辑。
- 骑手的 sprite 只能有一个写入者（现为根 Animator → 镜像到 `riderRenderer`），不能让 `EnemySpriteController` 与 Animator 同时写。
- 骑手/坐骑要接受一致的排透明度、波次染色、闪白与描边；材质实例化不能污染共享材质。
- 素材引用全部用 Inspector 引用，不加 `Resources.Load` 或字符串查找。

## 8. 验收清单（真实节点 N1）

用 `Assets/RouteData/FakeStage01/Battle_FakeStage01_N1_1.asset` 的完整波次逐项确认，并对照 Console 的 `[CavalryDiag]` 日志（标签含义见 `reference/debug-parameters-reference.md` 第 13 节）：

| 看什么 | 期望 |
|---|---|
| 入场 | 骑手用 Walk 前移、坐骑疾驰循环，到逻辑排后停下，无"提前冲锋" |
| 蓄势 → 冲锋 | 前摇原地不动，冲锋位移与疾驰循环同步，不出现滑步/原地空转 |
| 命中玩家 | 命中帧与玩家掉血同时发生；玩家掉血值 = `chargeDamage × (发起排+1)` |
| 命中敌人 | 在敌人后一排前探命中，不与敌人重叠 |
| 被打断 | 收招姿态清楚可读（不要看起来像"命中成功"）；随后转身返航 |
| 返航 | 逐排后退，被堵时停在原地等，不穿插他人 |
| 落马 | 骑手离座 + 击飞；坐骑淡出；落地后是完整 101 表现 |
| 死亡（骑乘中） | 按第 6 节拍板的结果 |
| 受击反馈 | 闪白与坐骑受击帧同步，不打断阶段逻辑 |

## 9. 变更日志

- 初版：按已实现的 `CavalryPhase` 状态机产出状态 × 动画矩阵、素材清单、Animator 结构建议、时间对齐规则与验收清单；第 6 节的 4 项待用户拍板。
