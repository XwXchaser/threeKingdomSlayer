---
id: kd_b526b9b9-4afa-4a30-9f86-06c005ae5ad1
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 冰系元素联动 · 交接（2026-10-18）

> 本文是**跨对话接手入口**。规则看 `design/ice-element-link.md`，美术需求看 `design/ice-element-art-brief.md` §10，实现细节看 `plan/ice-element-implementation.md`。
> 分支：`route-scroll-movement`。当前编辑器处于 `editing`，最近活动场景 `Assets/Scenes/MainMenu.scene`。

## 0. 一句话现状

冰雾 → 概率冻结 → 重攻击破冰（双倍 + 顶开 + 碰撞 + 碎冰 + 延迟回位）**整条链路已跑通并实机验收过**；冻结覆盖层 / 破冰碎裂 / 自然解冻融化三套美术已接入并由**一份共享配置**统一管理。剩余工作是回收调试参数、补 UI 细节、清理工作区。

## 1. 已验收的玩法行为

| 环节 | 行为 |
|---|---|
| 冰雾 | 复用 `ShootFireEffect.PlaySweep` 扇形喷流（火焰素材染冰蓝占位），对同一目标约每 0.2s 结算一次冰伤害 |
| 冻结概率 | 每次冰伤害叠加概率；目标处于**攻击前摇**时叠加 ×2；距上次命中 0.8s 后按 0.35/s 衰退；掷中即冻结并清零；冻结中不叠不掷 |
| 冻结 | 4 秒；暂停一切行动与计时（不是取消）；重复冻结只重置时长；**受击不解冻、不播 hitted** |
| 破冰触发 | 必须是 `isHeavy` 招式 + 实际命中冻结目标 + 满足破冰预备作用域（单列） |
| 破冰顺序 | ① 顶开（冰仍盖着，0.12s 滑行）→ ② 到位停住并双方各吃 `collisionDamage`(15) → ③ 命中伤害 ×2 → ④ 播放碎冰序列帧（hitted 同步）→ ⑤ 消费破冰预备 |
| 推动 | 最大 4 排，clamp 到可见排；撞到非命中者即停在其前一排；BOSS/骑乘骑兵/`freezePushImmune` 不推 |
| 回位 | 碎冰动画 + 1.2s 后按原槽走回；**不回位是绝不允许的**（曾出过 bug，见 §6） |
| 自然解冻 | 冻结计时到 → 播放 6 帧融化序列（往下塌）→ 停顿 0.5s（`freezeRecoveryDelay`，此期间不接补齐订单）→ 恢复正常 |
| 重攻击集合 | Pierce / Sweep / Launch / 枪突终结技 / **C2 指向突刺** |
| 破冰推动优先 | 命中触发破冰时，该目标**跳过招式自带的普通击退**（否则 4 排 + 1 排会叠加） |

## 2. 代码改动索引

| 文件 | 作用 |
|---|---|
| `Assets/Scripts/Core/ElementalBuffController.cs` | `PlayerElementalBuffs`（一次性 BUFF：授予/消费/清空 + `OnChanged` 事件）、`IceBreakRuntime`（攻击级会话、作用域判定、`IsBrokenThisAttack`） |
| `Assets/Scripts/Core/FreezePresentationConfig.cs` | **共享表现配置**（三个特效 Prefab + 冰块大小/位置），所有敌人共用 |
| `Assets/Scripts/Effect/SpriteSequenceEffect.cs` | 通用序列帧播放器（frames + fps + 播完自毁/淡出） |
| `Assets/Scripts/UI/ElementalBuffHUD.cs` | 破冰预备 HUD 图标（零场景接线，首次获得自动创建） |
| `Assets/Scripts/Enemy/Enemy.cs` | `Frozen`/`IceBreaking` 状态、`SuspendSource` 挂起令牌（卡肉+冻结共存）、冻结概率、`BreakIcePush`/`ApplyPendingBreakCollision`/`BeginIceBreaking`/`StartBreakSequence`、表现生成与等比适配、`RowGuard` 防越界 |
| `Assets/Scripts/Core/ColumnManager.cs` | `ApplyBreakIcePush`（逐排推进/撞停）、`RegisterBreakIceReturn`（延迟回位，`abortWaveMarch:false`）、`RegisterPushReturn` 支持自定义延时 |
| `Assets/Scripts/Player/AttackSystem.cs` | 每次出招 `IceBreakRuntime.BeginAttack`；`ApplyMovePushBack` 跳过已被破冰接管的目标 |
| `Assets/Scripts/Core/ActiveSkillRunner.cs` | `IceMist` 分支：起扇扫 + 每次命中 `ApplyIceHit` + 授予破冰预备 |
| `Assets/Scripts/Effect/ShootFireEffect.cs` | `onSweepHit` 回调、`particleTint`、`PlaySweep` 增加 `damageType` 参数 |
| `Assets/Scripts/Core/AttackSkillConfig.cs` | `ColumnMode`、`isHeavy`、`breakIcePushRows`（默认 4） |
| `Assets/Scripts/Core/StageConfig.cs` | `collisionDamage`（默认 15） |
| `Assets/Scripts/Core/ActiveSkillDefinition.cs` | `ActiveSkillEffectType.IceMist` |
| `Assets/Scripts/Core/UpgradeDefinition.cs` | `TimedAoeLevelConfig.iceFreezeChancePerHit` |
| `Assets/Scripts/Core/HitFeedbackManager.cs` | `HitFeedbackSource.Collision` |
| `Assets/Scripts/Player/PlayerState.cs` | 开局清空元素 BUFF 与会话 |

## 3. 资产索引

| 内容 | 路径 |
|---|---|
| 冰雾主动技能 | `Assets/ScriptableObjects/ActiveSkills/ActiveSkill_IceMist.asset`（Rare，5 级，CD 12→8s） |
| 表现共享配置 | `Assets/ScriptableObjects/Combat/FreezePresentation.asset`（12 个敌人预制体全部引用） |
| 三个特效 Prefab | `Assets/Prefabs/Effects/Ice_FreezeBlockFx.prefab`、`Ice_BreakFx.prefab`、`Ice_MeltFx.prefab` |
| 美术源 | `Assets/Sprites/EffectSprites/ice/`（14 张 256×256：冰块 1 + 破冰 7 帧 + 解冻 6 帧） |
| 招式标签 | 10 个招式资产上的 `columnMode` / `isHeavy` / `breakIcePushRows` |
| 调试注入 | ~~`Assets/Scenes/Battle.scene` 的 `DEBUG_IceTest_Bootstrap`~~（2026-10-18 已删除） |

## 4. 待办（按优先级）

1. **回收调试参数**（上线前必须）——2026-10-18 晚已做前两项，后两项归配平阶段
   - ✅ `ActiveSkillPoolConfig.asset`：冰雾已从 Common 池删除，Rare 池权重 100000 → **100**
   - ✅ 已删除 `Assets/Scenes/Battle.scene` 里的 `DEBUG_IceTest_Bootstrap`（冰雾现在只能靠三选一拿）
   - ⏸ `ActiveSkill_IceMist.asset`：每跳冻结概率 0.30→0.38 → 应回到 0.10 附近（**未改**，见 §9.2）
   - ⏸ `Enemy_1011.prefab`（以及其它敌人）：`freezeChanceMax` 0.9、`freezeChanceDecayDelay` 0.8、`freezeChanceDecayPerSecond` 0.35 → 0.75 / 0.6 / 0.5（**未改**，见 §9.2）
2. **冰雾正式美术**：目前喷流仍是火焰素材染冰蓝（实测会被硬编码暖黄基色乘成橄榄绿）。规格、交付清单与接入步骤见 §8。
3. **破冰预备 HUD 图标**：现在是程序化雪花，等 A8 后用 `ElementalBuffHUD.iconOverride` 替换
4. **局外解锁**：破冰预备作用域从"单列"放宽到"任意重攻击"依赖天赋树（未实现），可用调试开关模拟
5. **多元素容器**：冰是首个切片，后续火/雷/风等按 `design/ice-element-link.md` 的同一容器扩展

## 5. 实机验证方式

在 N1（`Assets/RouteData/Stage01/N1_ArtPreview_Battle.asset`，`fillUpRule = PerRow`，row0 有敌人、row1~3 空排、row4 有敌人）：

1. 进战斗 → 主动技能栏有冰雾（调试注入）
2. 点冰雾 → 喷流 → 连续命中 1011，概率累积后冻住（Console 有 `[Ice] ... roll=HIT`、`[Freeze] ... 进入冻结`）
3. 冻住期间：冰块覆盖 + 呼吸；打它**不会**解冻、不播 hitted
4. 蓄力松手出 Pierce（或 C2）→ 应看到：冰坨被顶开 → 撞到 row4 的敌人 → 双方跳 15 → 命中数字 ×2 → 碎冰序列 → 0.55s 后恢复 → 1.2s 后走回原槽
5. 不攻击，等 4 秒 → 融化序列（往下塌）→ 停顿 0.5s → 恢复行动

Console 关键字：`[ElementalBuff]`、`[Ice]`、`[Freeze]`、`[IceBreak]`、`[BreakIcePush]`、`[RowGuard]`（负数排告警，正常不应出现）。

## 6. 已修的关键坑（别改回去）

1. **破冰后被推的敌人不回位**：回位事务必须**在推完立即登记**（到期时间 = 0.12 顶开 + 0.55 碎冰 + 1.2 停留），不能等碎冰动画结束回调；且 `ApplyBreakIcePush` **不能**调 `ReleaseEnemyFromSchedulers`（会取消回位交易、摘掉行军链成员）。
2. **补齐规则被"破冰"打断**：`RegisterBreakIceReturn` 必须传 `abortWaveMarch:false`，否则每次破冰都会中止在途行军并回放旧步骤。
3. **冰块被拉伸**：只允许**等比缩放**（按高度算，宽度跟素材比例），早期用宽高分别缩放导致变形。
4. **冻结与卡肉互抢**：暂停必须走 `SuspendSource` 令牌，不能回到 `bool` 标记。
5. **碎冰一开始看不见**：碎片不能用半透明冰块贴图切分，必须是不透明亮色冰茬（现在用美术序列帧）。

以下四条是 2026-10-18 收尾补的，同样不要改回去：

6. **BOSS 转阶段必须解除冻结**：`EnterPhaseTransition` 里调 `ClearFreezeForSystemOverride()`。少这一步，`Frozen` 挂起令牌会残留，Animator 永久禁用、冻结计时不再递减、冰块不销毁（实测 BOSS 变“逻辑在动、画面不动”）。
7. **冻结中受击不播 hitted**：由 `IsFrozenHitReactionSuppressed` 统一判定，`TakeDamage` 与 `HitFlashRoutine` 两处都要用。**破冰序列（含 0.12s 顶开窗口）必须例外**，碎冰与受击要同步。
8. **`IceBreaking` 不可再冻、也不叠概率**：`CanBeFrozen` 与 `ApplyIceHit` 都要排除该状态，否则破冰那 0.55s 内一跳冰雾就能重新冻上、冰块在碎过的敌人身上复活。
9. **破冰只能由玩家攻击本体触发**：`TryResolve` 用 `feedbackSource == BasicAttack` 与 `_brokenThisAttack` 双重门；DoT / 被动 / 道具 / 碰撞不得触发，同一目标一次攻击只结算一次。
10. **冲锋位移中被冻结必须放弃本步**：`MoveVisualToRow` 返回后要立刻查 `_frozenDuringCharge` 并 `RecoveryAndRetreat`（2026-10-18 修）。少这一处，最后一步会把 `rowIndex` 落到 0，`while (rowIndex > 0)` 先退出循环，循环体内的冻结检查就永远跑不到 —— 实测表现为“冻住了、解冻后照样冲锋并打中玩家”。

## 7. 工作区风险（提交前必看）

- `Assets/Experiments/CurvedScroll/YJunctionSample.unity` 相对 HEAD 是 **+6370 / −112233 行**（净减约 10.6 万），与本次冰系工作无关，提交前需确认是本人有意修改，否则 `git checkout --` 恢复。
- `Assets/RouteData/Stage01/N1_ArtPreview_Battle.asset` 的 `rows` 被改过（row0 与 row4 各有一组敌人 + 中间三排空排），`fillUpRule` 已改为 `PerRow`——这是关卡配置调整，需与策划确认后一并提交。
- 若后续在 Play Mode 里改过资产再调 `AssetDatabase.SaveAssets()`，可能把运行时状态写回资产；改资产请在 Edit Mode 做。

## 8. 冰雾（喷冰）美术需求 A1 —— 可交付规格

> 对应 `design/ice-element-art-brief.md` §4 素材清单里的 A1。本节是从**当前扇形喷火代码的播放方式**反推出来的执行规格，可直接交给美术。

### 8.1 代码是怎么播的（需求的前提）

`ShootFireEffect.PlaySweep`（`Assets/Scripts/Effect/ShootFireEffect.cs`，prefab 为 `Assets/Prefabs/Effects/ShootFireEffect.prefab`）：

- 0.9s 内每 `sweepShotInterval = 0.02` 秒发射一股，**约 45 股**；喷口在 `sweepAngle = 35°` 内按 `sweepCycles = 3` 左右来回摆动。
- 每一股是**独立 `SpriteRenderer`**：从 `fireSprites` 4 张里随机取 1 张 → Z 轴旋转 `-当前扫射角 ± rotJitter 20°` → 缩放固定 `scaleNearMax 0.35 × globalScale 1` → 颜色 = **硬编码基色 `(1, 0.85, 0.2)`** × `particleTint` → 0.08s 淡入 / 保持 0.29s / 0.08s 淡出，沿扇形方向飞行 0.45s。
- 所以：**只要静态单帧精灵，不要序列帧、不要方向帧**。动感全部来自 45 股粒子的时间差、随机旋转和重叠。

### 8.2 硬性规格

| 项 | 要求 | 依据 |
|---|---|---|
| 画布 | **200×200 px**（与 `shootFire1-4` 一致） | 现有喷流素材 |
| 格式 | PNG RGBA32，直通 alpha | 同上 |
| PPU | 16 | `.meta` 实测 |
| Filter | Point | `.meta` `filterMode: 0` |
| Mesh | Tight | `.meta` `spriteMeshType: 1` |
| 张数 | 4 张（可 6 张，数组可扩） | 现有 4 槽 |
| 有效像素 | bbox 约 145–152 px，占画布 50–57% | 实测四张 `shootFire` |

若改出 256×256（与 `Assets/Sprites/EffectSprites/ice/` 其它素材同画布），世界尺寸会从约 4.4 单位变成 5.6（+28%），接入时需把 `scaleNearMax` / `scaleFar*` / `globalScale` 乘 0.78。两种都行，交付时说明即可。

### 8.3 画面要求

- **颜色**（核心→边缘）：近白青 `(225,245,255)` → 主色冰蓝 `(160,210,240)`（与 `Ice_FreezeBlock` 实测核心 `(160,206,240)` 同族）→ 边缘 alpha 0。**不要深蓝**：颜色会被乘算，深蓝叠完发灰发脏。
- **实心度**：沿用火焰的“实心团”手感（实测火焰每张约 1.2–1.3 万全不透明像素、仅约 800 半透明像素）。冰可以雾一点，但核心区仍需 ≥ 85% alpha，否则 45 股叠起来是一团糊。
- **形状差异**：4 张必须形不同——饱满冰团 / 斜拉冰絮 / 碎晶簇 / 薄雾。只靠旋转区分，会在高密度重叠里读成同一个疙瘩。
- **朝向**：精灵会被旋转到“顺着飞行方向”，所以**上密下散**（上方是锋头、下方留羽化尾）最贴；拿不准就做旋转对称的软团，最稳。
- **内部对比度要低**：0.02s 一股、45 股重叠，高对比内部细节会读成彩纸屑。
- **避开**：描边、1px 细线、高频噪点（Point 过滤 + 任意角度旋转会闪）、雪花图标化造型。

### 8.4 交付清单

- `Assets/Sprites/EffectSprites/ice/Ice_Spray_1.png` … `Ice_Spray_4.png`（可选 `_5`、`_6`）。
- 导入设置照 §8.2；不要动 `Assets/Sprites/EffectSprites/ice/` 里已有素材。
- 出 6 张时把引用都拖进数组即可，不需要改代码。

### 8.5 接入步骤（含一处必须改的代码）

1. 复制 `Assets/Prefabs/Effects/ShootFireEffect.prefab` → `Assets/Prefabs/Effects/IceMistEffect.prefab`；
2. 把 `fireSprites` 的 4 个引用换成冰素材；`particleTint` 设白（或 `(0.92,0.98,1)` 微冷）；
3. **必须改代码**：`SpawnSweepFire` 里 `Tinted(new Color(1f, 0.85f, 0.2f, 0f))` 是硬编码暖黄基色，会和新素材相乘（冰蓝 × 暖黄 ≈ 橄榄绿）。加一个 `baseSpriteColor` 字段（火焰保留暖黄、冰雾给白），或按 `damageType == Ice` 取白；
4. 场景里的 `ActiveSkillRunner._iceEffectPrefab` 指向新 prefab（`[SerializeField]`，需要在场景/预制体上赋值，代码不会自动找）；
5. 回 N1 用冰雾验收。

### 8.6 验收标准

- 整条喷流是青白色，没有黄/绿残留；
- 单股粒子看得出密度，不是一坨糊；0.9s 内三个来回的左右扫能看出来；
- 命中敌人附近不出现火焰色边缘；
- 与冻结冰块（`Ice_FreezeBlock`）同色系，不像两套素材。

### 8.7 本期不做

- 冰雾命中溅射（brief 的 A2）：目前复用命中火花；
- 喷流地面霜痕 / 寒气体积雾：归 P2。

### 8.8 已产出 v1（2026-10-18）

已按 §8.2 规格生成并入库四张：

| 文件 | 尺寸 | 形态 | 与火焰基准对比 |
|---|---|---|---|
| `Assets/Sprites/EffectSprites/ice/Ice_Spray_1.png` | 200×200 | 饱满冰团 | 主体 147px、核心 alpha 均值 211、密集像素 75% |
| `Assets/Sprites/EffectSprites/ice/Ice_Spray_2.png` | 200×200 | 斜拉冰絮 | 147×121、均值 198、密集 65% |
| `Assets/Sprites/EffectSprites/ice/Ice_Spray_3.png` | 200×200 | 带碎晶的雾团 | 147×129、均值 205、密集 69% |
| `Assets/Sprites/EffectSprites/ice/Ice_Spray_4.png` | 200×200 | 薄雾 | 113×150、均值 152、密集 40% |

对照基准（`shootFire1-4`）：主体 145–152px、核心 alpha 均值 247–248、密集像素 95–96%。**冰雾刻意比火焰软**，靠 45 股叠加吃出密度；若实机觉得太淡，只需把 `normalize` 的 alpha gamma 从 0.75–0.95 降到 0.65 重出一遍，不要改 Unity 侧的 tint（tint 只能压暗）。

- 生成参数：`gpt-image-2.5-sunburst`、`quality=max`、`size=1024x1024`、`background=transparent`、`output_format=png`；四张均返回原生 RGBA（ColorType 6，四角 alpha=0）。
- 原始 1024 图：`C:/Users/Administrator/Pictures/gptGen/ice_spray_v1_1..4.png`；对照图（深/浅底）：`C:/Users/Administrator/Pictures/gptGen/ice_spray_preview.png`。
- 归一化：裁 alpha 外框 → 预乘 alpha 缩放（最长边 147px）→ 轻度 alpha gamma 抬升 → 居中到 200×200。
- 导入设置已按 §8.2 设好（PPU 16 / Point / Tight / alphaIsTransparency / 不压缩 / 无 mipmap）。
- 还没接入预制体：等 §8.5 第 3 步的基础色字段改完再挂，否则会被乘成橄榄绿。

## 9. 本轮改动与验证状态（2026-10-18 收尾）

已改并**实测通过**（Play Mode，有数值证据）：

- 破冰动画（`IceBreaking`）期间不再被冻结、不叠冻结概率；
- 冻结中受击不播 hitted，破冰那次仍同步播（解冻瞬间实测播的是 `HitLeft`）；
- BOSS 冻结中跨阶段 → 挂起清空、Animator 恢复、冰块消失；
- 同一目标一次攻击只破一次：`hp 200→160→140`、`row 4→4`。

### 9.1 四条“只有代码没有实机”的项 —— 2026-10-18 晚已全部实机验证（Play Mode，N1 同级运行时配置）

验证方式：用运行时构造的 `StageConfig`（1011/109 + `FillUpRule.PerColumn`）在 `Battle.scene` 起同一套战斗系统，直接驱动 `Freeze()` / `IceBreakRuntime.BeginAttack()` / `Enemy.TakeDamage()`，读 `currentHealth`、`rowIndex`、`CavalryEnemy.Phase` 判定。

| 项 | 结果 | 实测证据 |
|---|---|---|
| ① DoT 打冻结目标不翻倍 | **通过** | 冻结后 `TakeDamage(20, feedbackSource: Dot)` → hp 200→180（未 ×2），state 仍 `Frozen`，`PlayerElementalBuffs.IceReady` 仍为 true（BUFF 未被消费）、`HasArmedSession=false` |
| ② 同列两个冻结各自 -40、不互吃 15 | **通过** | 同列 row0/row1 各冻一个，一记 C2 命中两者 → 各自 delta **40**（无 15 碰撞）；推为 row1→4、row0→3，`blockedBy=none`；1.87s 后各自回位到 row1 / row0 |
| ③ 补齐移动中被冻先落位 | **通过** | 杀掉前排触发同列补齐，第 2 帧捕获 `state=Moving row=1`，`Freeze()` 后立即 `row=0`、`state=Frozen`、`HasRushMoveOrder=false`（补齐链已走完，未挂住） |
| ④ 冲锋中的 109 被冻停住 + 解冻返航 | **修好后通过** | 冻结期间 `row 1→1`、`posZ 1.39` 不动；解冻后 `Phase` 直接进 `Retreating` → 回 `origin row2`，**玩家血量 100→100 未掉**；另测前摇中冻结同样不续播冲锋 |

**④ 第一次实测是失败的**：冻结发生在冲锋位移补间（`MoveVisualToRow`）期间时，补间结束后 `rowIndex` 先落到 0、`while (rowIndex > 0)` 条件随即退出，`_frozenDuringCharge` 的检查在循环体内根本没被求值 → 冲锋照打、玩家白挨一下。修复见 §6 第 10 条。

### 9.2 仍待处理（未在本轮动）

- 冻结概率仍是调试档：`ActiveSkill_IceMist.asset` 每跳 0.30/0.32/0.34/0.36/0.38（目标约 0.10）；12 个敌人预制体 `freezeChanceMax 0.9 / decayDelay 0.8 / decayPerSecond 0.35`（代码默认 0.75 / 0.6 / 0.5）。属于数值配平，等 `plan/loopable-demo-handoff-20261018.md` 的配平阶段一起定，不要单独改。
- 冰雾喷流仍是火焰素材染冰蓝（§8.8 的 `Ice_Spray_1..4` 已入库，但 §8.5 第 3 步的基础色字段改完后才接）。

### 工具坑（别再踩）

`unity_recompile` 曾回报 `compiled / domain_reload completed`，但 `Library/ScriptAssemblies/Assembly-CSharp.dll` 的 mtime 没变、Play Mode 仍跑旧程序集。核对方式：**看 DLL 的 mtime 与大小**（本轮 1026048 → 1028096）；必要时 `touch` 源文件再 `CompilationPipeline.RequestScriptCompilation()`。另：`unity_execute` 在 Play Mode 下偶发返回空且不执行，写文件请用**绝对路径**。
