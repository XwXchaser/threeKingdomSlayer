---
id: kd_f5e4b738-a97e-4bb6-a5d4-f4b0ac578c37
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 1013 旗手 / 1051 弓手 · 美术接入 Unity 实施方案（2026-11-09）

> **✅ 已实施（2026-11-09）**。实施结果见 §0；下文 §1~§7 为方案原文（保留原始判断与踩坑记录）。
>
> **范围**：只做「接入 + 对齐」。素材已由用户验收，本方案**不评判素材质量、不修改素材**。
> **三条硬前提（用户 2026-11-09 拍板）**：
> 1. **1051 的原型 = `Enemy_106_Fixed.prefab`，行为完全对标 106。**
> 2. **1013 的对齐基点 = 人（靴底）**，旗杆不参与定尺、不参与地面基准。
> 3. **1051 进 `EnemyPool`；并替换 E0 波次里的 106 敌人**（不进池就加载不出来）。
>    1051 的屏幕高度取 **与新 1012 同族**（`rootScale 0.08316832`，人物身 3.144 世界单位），**不是** 106 的高度。
> **状态**：已落地 → 等待用户验收。

---

## 0. 实施结果（2026-11-09）

### 0.1 落位（1013 三组）

**发现并修正了素材台账的一个错**：walk 组的 `body_h` 列记的是「**旗顶→脚底**整幅内容高」（942），不是人物本体高；
直接拿它当缩放分母会把 walk 的人缩到只有 idle 的 57%。用户「以人为基点」的指示正好命中这一点。

用形态学开运算（NECK=45 宽，比旗杆~30px 宽、比人窄）把旗杆与旗面从人物身上剥离后实测：

| 组 | 台账 body_h | **实测人物本体高** | 落地 k | 落地人物身 |
|---|---|---|---|---|
| idle | 634 | **629** | 605/629 = **0.96184** | 605~606 ✓ |
| cast | 621 | **617**（取 idle 姿态帧 cast_01/cast_12） | 605/617 = **0.98055** | 605 ✓ |
| walk | 942 ✗ | **620** | 605/620 = **0.97581** | 595~623（源片自身起伏）✓ |

- 统一画布 **1440×1440**（README 写的 1108×1440 装不下 cast 横扫帧的 1131px 旗面）
- 统一靴底线 **1333**、人物本体高 **605**、身体中心 x = **720**（= pivot）
- idle/walk 用**组内统一偏移**（保留走路的水平摆动）；cast 用**逐帧对齐靴底**（旗杆高度在动作里变 300+px，裁切原点跟着变）
- 工具：`Tools/visual_brief/land_1013_frames.py`（含「量人物本体高」的形态学实现）

### 0.2 资产清单

| 类别 | 产物 |
|---|---|
| 精灵 | `Assets/Sprites/Enemy/Enemy1013/`（24 张，1440²）+ `Assets/Sprites/Enemy/Enemy1051/`（18 张，1108²） |
| 导入规格 | Single / Tight / **PPU 16** / 中心 pivot / alphaIsTransparency / Point / Uncompressed（照 1012 的 `.meta` 整段对齐） |
| clip | `Enemy_1013_Idle(1.8s) / _Walk(0.6s) / _Cast_Start(0.8s) / _Cast_Loop(0.3s) / _Cast_End(0.3s)`、`Enemy_1051_Idle(1.98s) / _Walk(0.6s) / _Attack(2.767s)` |
| controller | `Animator_1013.controller`（改造 104 副本，保留 GUID）、`Animator_1051.controller`（新建，照 `Enemy_105.controller`） |
| prefab | `Enemy_1013.prefab`（改造）、`Enemy_1051.prefab`（复制 106 新建） |
| 台账 | `Enemy1013ArtSource/00_Index/landing-1013.json`、`Enemy1051ArtSource/00_Index/deployed-assets.csv` |

### 0.3 最终参数（Unity 实测回读）

| Prefab | rootScale | visualYOffset | 剪影底(世界) | 人物身(世界) | 碰撞体(世界) |
|---|---|---|---|---|---|
| Enemy_1013 | 0.08316832 | **1.401980**（meshMinY −38.5） | **−1.8000** | 3.1448 | 高 1.2866 / 宽 0.5015 |
| Enemy_1051 | 0.08316832 | **−0.001485**（meshMinY −21.625，与 1012 同构） | **−1.8000** | 3.1448 | 高 1.2866 / 宽 0.5015 |

碰撞体按 1011/1012 的**世界尺寸**换算成 local 值（scale 变了必须跟着换，否则命中区会缩一半）。

### 0.4 接线

- `Battle.scene` 的 `EnemyPool.enemyPrefabList`：14 → **15**，末尾追加 `Enemy_1051`（场景保存前 `isDirty=False` → diff 干净）
- `Formal_E0_Battle.asset`：wave2 4 个 + wave3 3 个 + wave4 7 个 = **14 个 106 → 1051**（`enemyIds` 是 `int[]`，Unity 写成 4 字节小端 hex）
- `Formal_N3_Battle.asset` 的 11 个 106 **不动**（用户只点名 E0）→ `Enemy_106_Fixed` 保留在池里

### 0.5 运行时验收（Play Mode 实测）

| 验收项 | 结果 |
|---|---|
| 1013 脚底 | 剪影底 **−1.8000** ✓ |
| 1051 脚底 | 剪影底 **−1.8000** ✓ |
| 两者人物本体高 | 均 3.1448 世界单位（= 1012 同族）✓ |
| 1013 受击 | 状态进 `HitFlash`，**精灵保持 idle 姿势**；缩放反馈（0.083→0.100→0.083）与 1012 对照组**完全一致** ✓ |
| 1013 死亡 | `Dead` 状态 + **代码驱动的弹起→掉落**（Y: 2.94→3.92→−5.84）✓，姿势保持 idle ✓ |
| 1013 号令 | Idle→**Cast_Start**(cast1→2→4)→**Cast_Loop**(cast7)→**Cast_End**(cast11)→Idle ✓ 三段完整 |
| 1051 射箭 | `Enemy_1051_Attack` 逐帧换图 attack1→2→3→4→5→6 → 2.767s 后回 Idle ✓（与美术时序表一致） |
| 1051 投射物 | 继承 106 的 `arrow` + `Enemy_106_RowFlight` + 固定落点 `[-2,-1,0,1,2]` ✓ |

### 0.6 号令时序重配（2026-11-09 用户反馈「太快」后）

初版把 12 帧按「前摇/挥/收」切成 0.8 + 0.3 + 0.3 = **1.4s**，但三段帧率严重不均（6.25 / 16.7 / 6.7 fps）—— 最有戏的挥旗 5 帧只有 0.3s，一闪而过。
用户定：**0.8 + 0.8 + 0.8**。

| 项 | 改前 | 改后 |
|---|---|---|
| `EnemyCommander.castFollowDuration`（prefab） | 0.3 | **0.8** |
| `Enemy_1013_Cast_Start.anim` | 5 帧 / 0.8s / 6.25fps | 不变 |
| `Enemy_1013_Cast_Loop.anim` | 5 帧 / 0.3s / 16.7fps | **5 帧 / 0.8s / 6.25fps** |
| `Enemy_1013_Cast_End.anim` | 2 帧 / 0.3s | **2 帧 / 0.8s**（帧 11 收旗 0.4s + 帧 12 回 idle 0.4s） |
| 号令总时长（代码推算 = windup + follow×2） | 1.4s | **2.4s** |
| Play Mode 实测 | 1.43s | **2.37s**（0.73 / 0.83 / 0.81） |

### 0.7 遗留 / 提请注意

- **1013 的 `enemyName` 仍是「督战鼓手」**，但已验收美术是**曹军旗手**（执旗、无鼓）——名字与形象不符，**未擅自改**，请拍板。
- `Enemy_1013.prefab` 上你原有的未提交改动（`maxHealth 150→100`、`moveSpeed 0.3→0.5`）已保留 ✓
- 1051 的精灵沿用已验收落位（身体中心在 x=521，比 pivot 偏左 32px ≈ 0.166 世界单位），未重新落位

---

## 1. 对齐机制（已用 Unity 实测确认）

| 项 | 结论 |
|---|---|
| 地面基准线 | world Y = **-1.8**（全项目统一） |
| 偏移字段 | `Enemy.visualYOffset`（序列化），在 `UpdateWorldPosition()` 里与 `bounceYOffset` 相加 |
| 公式 | `visualYOffset = -1.8 - meshMinY × rootScaleY`（`meshMinY` = 导入后 Sprite 的 **tight mesh** 最低 local Y，px/16） |
| 校准 | 1011 `0.1389` / 1012 `0.001` / 104 `1.035` / 105 `0.2625` / 109 `0.7938` —— **5/5 与公式吻合，脚底全部精确落在 -1.8** |
| ⚠ `EnemyGroundAligner` | **死代码**：没挂在任何 prefab / 场景，代码里也没有任何 `AddComponent` → **运行时自动对齐从不生效**，必须手填 `visualYOffset` |
| 画布口径 | 1108² → `rootScale = 0.08316832`（1011 / 1012 / 109 同值）→ 人物本体 605px = **3.144 世界单位**（普通兵标准） |
| 导入设置 | Single / Tight / **PPU 16** / alphaIsTransparency / Point / maxTextureSize 2048 |

### 1.1 两个必须避开的坑

1. **`.meta` 里必须显式写 `spriteAlignment`**
   不写 → 默认 `spriteAlignment: 0`（Center），**`.meta` 里的 `spritePivot` 会被忽略**。
   实测：`Enemy_1012_idle1.png.meta` 写着 `spritePivot: {0.65392774, 0.5487343}`，但 `spriteAlignment` 缺失 → 实际生效 pivot = 中心 (0.5, 0.5)。
   （`memory/enemy1012-art-replacement-pipeline.md` §3 那句「pivot idle=0.6539」是**失效的旧记录**，以实测为准。）
2. **Unity 的 tight mesh 底部比可见底行低约 3px**
   实测 1012：可见底行 897 → `meshMinY = -21.625`（= -346px）。
   → 公式算出的 `visualYOffset` 约有 **±0.016 世界单位**误差，**导入后必须回读 `meshMinY` 校准**。

---

## 2. 1013 曹军旗手

### 2.1 落位（素材未落位，必须先做）

交付物实测与 README 声称的落位口径不一致 —— 三组画布/脚底线各不相同，**不能直接进 Unity**：

| 组 | 实际交付 | 脚底线（靴底） | 人物身 | 需要的 k |
|---|---|---|---|---|
| Idle 6 帧 | 834×1112 | 981（6 帧全同） | 634 | 605/**629** = **0.96184**（实测人物本体高，非台账的 636） |
| Cast 12 帧 | 1440×1440 | 1332（1331~1333） | 621 | 605/**617** = **0.98055**（取 idle 姿态帧） |
| Walk 6 帧 | 1440×1440 | 1331（1323~1331） | 942 ✗（是整幅内容高） | 605/**620** = **0.97581** |

**落位口径**：
- 统一画布 **1440×1440**（不是 README 的 1108×1440 —— Cast 横扫帧旗面宽 1131px，1108 装不下会切边）
- **靴底**统一对齐到 **1333**（以人为基点，旗杆不参与）
- 人物本体高统一 **605**（不含旗杆）→ 与普通兵同高
- pivot **中心 (0.5, 0.5)**、PPU 16
- 逐帧量「**人物身体中心 x**」再统一（**不能用剪影 bbox 中心** —— 旗面/旗杆会把中心带偏：实测三组的剪影中心分别约 373 / 719 / 782）

**以人为基点的实测依据**：逐帧分离靴底与旗杆底后，**靴底 = 剪影底**（idle 981、cast 1332、walk 1331/1323），仅 `walk_06` 差 4px（旗杆比靴底低 4px）。即旗杆没有低于靴子，地面基准用靴底即可。

### 2.2 prefab 要设的参数

```
rootScale       = 0.08316832
visualYOffset   = +1.402      ← 预测值，导入后回读 meshMinY 校准
pivot           = 中心 (0.5, 0.5)，.meta 显式写 spriteAlignment: 0
```

推算：1440² 中心 pivot → pivot 行 720；靴底 1333 → 在 pivot 下 613px，加 tight mesh 余量 3px = 616px = **-38.5 单位** → ×0.08316832 = **-3.20198** → `-1.8 - (-3.20198) = +1.402`。
→ 人物本体高 605px ×(1/16)×0.08316832 = **3.144 世界单位**（与 Enemy_1012 同高）。

### 2.3 动画（clip 时序沿用 1012 已部署口径）

| 动作 | 帧数 | clip 长度 | 备注 |
|---|---|---|---|
| Idle | 6 | **1.8s**（每帧 0.3533s） | 照 `Enemy_1012_Idle.anim`（m_StopTime 1.8、6 个 sprite key） |
| Walk | 6 | **0.6s** | 画面是**奔跑**；工程状态名仍叫 `Walk` |
| Cast | 12 | **0.8s = 15fps** | 拆三段由 `EnemyCommander` 驱动 |

### 2.4 Cast 三段与 EnemyCommander 的对应

`EnemyCommander` 现有时序：`castWindup = 0.8` / `castFollowDuration = 0.3` / `castInterval = 4` / `firstCastDelay = 2.5`；
流程：`BeginCast` 播 `castStartState` → t≥0.8 落加攻并播 `castLoopState` → t≥1.1 播 `castEndState` → t≥1.4 播 `Idle`。

| 新状态 | 帧 | 时长 | fps |
|---|---|---|---|
| `Cast_Start` | 01–05（起·沉·起举·举到顶·顶点呐喊） | 0.8s | 6.25 |
| `Cast_Loop` | 06–10（挥：起手·甩出·横扫·甩到右·回正） | 0.3s | 16.7 |
| `Cast_End` | 11–12（收·旗降 / 收回 idle） | 0.3s | 6.7 |

→ 需改 prefab 上 `EnemyCommander` 的 `castStartState` / `castLoopState` / `castEndState` 三个字段名。

### 2.5 Controller（新建，替换 104 副本）

新建 `Assets/Animations/Animator_1013.controller`，结构照 `Enemy_1012.controller`：
- 触发器参数：`Attack / Launch / Dead / Hit / Walk / ReRise`
- 真实状态：`Idle`(默认) / `Walk` / `Cast_Start` / `Cast_Loop` / `Cast_End`
- 过渡：Idle →(Walk/Dead/Hit/Launch 触发器)；Cast_Start→Cast_Loop→Cast_End→Idle（退出时间）；ANY→Dead；ANY→Launched_Rise

### 2.6 占位状态（motion 指向 Idle clip）

`Attack / HitFlash / HitLeft / HitRight / Dead / Launched_Rise / Launched_Fall / StunStart / StunLoop / StunHit / StunEnd`

**不建 `Launched_Getup`** —— 现行为如此（104 副本里没有该状态 → `BeginGetup()` 直接返回 false → 敌人保持 Idle）。建了反而会改变行为分支。

### 2.7 要清的 104 遗留物

| 遗留 | 处理 |
|---|---|
| SpriteRenderer 精灵 `Assets/Sprites/Enemy/BOSS1/BOSS_idle.png/BOSS_idle` | 换成 1013 Idle 首帧 |
| `Animator_1013.controller`（= `Boss_104.controller` 副本，23 状态全指向 `Boss_104_*.anim`） | 换成新建的 1013 controller |
| 子物体 `AttackTelegraphAnchor`（`attack_telegraph_frame_1` + `EnemyAttackTelegraph`） | **删除**（1013 不攻击：attackDamage=0、attackSequence 空） |
| `rootScale 0.18` / `visualYOffset 1.035` | 换成 §2.2 的值 |
| `EnemyCommander` 三个 cast 状态名 | 换成 `Cast_Start / Cast_Loop / Cast_End` |

⚠ prefab 上有用户未提交改动（`maxHealth 150→100`、`moveSpeed 0.3→0.5`）—— **实施时不得覆盖**。

---

## 3. 1051 曹军弓手

### 3.1 原型 = 106

`Enemy_106_Fixed.prefab`（enemyName「固定落点弓箭手」、enemyId 106、`isRanged 1`）。
**做法：复制该 prefab → 改 enemyId 1051 → 换美术与 controller → 重算对齐参数。**

106 的完整行为字段（1051 全部照搬）：

| 字段 | 值 |
|---|---|
| enemyName / enemyId | 弓箭手 / **1051**（名称待定） |
| isRanged | 1 |
| maxHealth / attackDamage / attackSpeed / attackRange | 100 / 5 / 0.3 / 4 |
| moveSpeed / occupySlots / expReward / coinReward | 0.5 / 1 / 50 / 10 |
| maxPoise | 40 |
| projectilePrefab | `arrow` |
| projectileArcHeight / projectileFlyDuration / projectileZTargetOffset | 3 / 1 / 5 |
| projectileLandingXSpread | 1.2 |
| **useFixedProjectileLandingX** | **1** |
| **fixedProjectileLandingXByColumn** | **[-2, -1, 0, 1, 2]** |
| **arrowConfig** | **`Enemy_106_RowFlight`** |
| attackSequence | 1 步：`isCAttack 0` / `animationTrigger 空` / `spawnDuration 2` / `drawDuration 1.5` / `useFlip 0` / `hitFrames 空` |
| 子物体 | `AttackTelegraphAnchor`（保留） |

### 3.2 美术已落位，无需重做

实测与 `1051_idle_landing.json` 吻合：画布 **1108²**、脚底 **897**（idle/attack 6 帧全同；walk 891~900）、人物身 **605**（顶 293）。

几何与 `Enemy_1012_idle1` **完全同构**（1108² / 中心 pivot / 脚底 897 / 身 605）→ `meshMinY` 同为 -21.625。

### 3.3 prefab 要设的参数

```
rootScale       = 0.08316832
visualYOffset   = 0          ← 公式值 -0.0015；1012 存 0.001，差 0.0015 世界单位
pivot           = 中心 (0.5, 0.5)，.meta 显式写 spriteAlignment: 0
```
→ 脚底精确 -1.8，人物身 **3.144 世界单位**（与 1012 同族）。

> 106 现值是 `rootScale 0.20` / `visualYOffset 0.2625`（配旧 512² 美术），换新美术必须重算。
> **已拍板（2026-11-09）：1051 取「与新 1012 同族」高度 → `rootScale 0.08316832`、人物身 3.144 世界单位**（不是 106 的 3.9375 剪影高）。

### 3.4 动画

| 动作 | 帧数 | clip 长度 | 30fps 时间轴 |
|---|---|---|---|
| Idle | 6 | **1.98s**（周期 48 源帧） | 0 / 0.33 / 0.66 / 0.99 / 1.32 / 1.65 |
| Walk | 6 | **0.6s** | 0 / 0.1 / 0.2 / 0.3 / 0.4 / 0.5 |
| Attack | 6 | **2.767s** | 0 / 0.35 / 0.70 / 1.05 / 1.50 / 2.50 |

Attack 的 2.767s **等于旧 105 `Enemy_105_Attack.anim` 的 StopTime** → 与 106 的 `spawnDuration 2` / `drawDuration 1.5` 时序可直接对上。

### 3.5 Controller（新建）

新建 `Assets/Animations/Animator_1051.controller`，结构照 `Assets/Animations/Enemy_105.controller`：
- 触发器参数：`Attack / Launch / Dead / Hit / Walk / ReRise`
- 状态：`Idle`(默认) / `Walk` / `Attack` / `HitFlash` / `Launched_Rise` / `Launched_Fall` / `Dead`
- 过渡：Idle →(Attack/Hit/Walk)；Attack→Idle(退出时间)；HitFlash→Idle；Launched_Rise→Launched_Fall；Launched_Fall→Idle(ReRise)；ANY→Dead；ANY→Launched_Rise
- **不建 `HitLeft` / `HitRight`**（105 controller 就没有 → `PlayHitVisual` 自动回退 `HitFlash`）
- **不建 `Launched_Getup`**（105 controller 就没有 → 保持 106 的行为分支）

### 3.6 占位状态（motion 指向 Idle clip）

`HitFlash / Dead / Launched_Rise / Launched_Fall`

### 3.7 注册与波次接线（已拍板）

**enemyId 来自文件名**：`EnemyPool.RegisterInspectorPrefabs()` 用 `ParseEnemyIdFromName` 从预制体文件名解析（`Enemy_{id}` 或 `Enemy_{id}_{后缀}`）→ 新预制体必须叫 **`Enemy_1051.prefab`**，才能注册成 enemyId **1051**。

**① 进池**：`Battle.scene` 的 `EnemyPool.enemyPrefabList` 追加 `Enemy_1051`（现 14 个，`Enemy_1013` 在 [13]，新项落在 [14]）。
**旧 `Enemy_106_Fixed` 必须保留在池里** —— `Formal_N3_Battle.asset` 还有 11 个 106 槽位在用。

**② 替换 E0 的 106**：`Assets/RouteData/ChangbanpoScroll/Battles/Formal_E0_Battle.asset`
- `enemyIds` 是**十六进制字节数组**，每个槽位 2 字节小端：`106 = 0x006A → "6a00"`，`1051 = 0x041B → "1b04"`
- 全文件 `6a00` 恰好 **14 处**，与解析出的 106 槽位数完全一致 → 可安全整体替换为 `1b04`
- 14 个槽位分布（wave / 排 r / 列 c）：

| wave | 位置 | 个数 |
|---|---|---|
| 2 | r5c2 / r9c1 / r9c2 / r9c3 | 4 |
| 3 | r5c1 / r5c2 / r5c3 | 3 |
| 4 | r2c2 / r3c1 / r3c3 / r7c2 / r9c1 / r9c2 / r9c3 | 7 |

- wave 0 / wave 1 没有 106，不动
- `Formal_N3_Battle.asset` 的 11 处 106 **不动**（用户只点名了 E0）；若要一并换型，需另行确认

---

## 4. 占位动作的统一设计

**规则：占位状态照常建出来，但 motion 指向 Idle clip。** 不建状态会让 `Animator.Play()` 每次受击刷一条 warning；而 `Dead` / `Launched_Rise` 是 ANY→X 过渡的目标，必须存在。

依据（已核实）——三个表现都不依赖动画：

| 表现 | 实际驱动 | 占位后 |
|---|---|---|
| 受击闪白 | **材质层**（`CreateFlashMaterials` / `ApplyHitFlashImmediate`） | 闪白 / 缩放 / 卡肉照常，姿势保持 idle ✅ |
| 死亡倒地 | **代码驱动**（`Enemy.Die()` → `DeathBounceAndFall()` 弹起+旋转+重力） | 尸体照常倒下 ✅ |
| 击飞抛物线 | **代码驱动**（`launchGravity=20` / `launchYHeightMin·Max` / `launchDuration=1.5`） | 照常被击飞 ✅ |

受击动画入口：`Enemy.FormSwitch.PlayHitVisual(direction)` → 播 `HitLeft` / `HitRight` / `HitFlash`，**目标状态不存在时回退 `HitFlash`**（`HasState` + `Base Layer.` 前缀）。

---

## 5. 实施步骤（待批准后执行）

1. **落位**：1013 三组按各 k 缩放 → 统一画布 1440×1440、靴底 1333、人身 605、逐帧人物中心 x 对齐；1051 已落位，跳过
2. **写精灵**：`Assets/Sprites/Enemy/Enemy1013/`（24 张）、`Assets/Sprites/Enemy/Enemy1051/`（18 张）
   —— 只覆盖 PNG 字节、保留 `.meta`；`.meta` 显式写 `spriteAlignment: 0` + `spritePivot (0.5,0.5)` + `spritePixelsToUnits: 16` + `spriteMeshType: 1` + `alphaIsTransparency: 1`；写完回读核对
3. **建 .anim**（30fps，照 1012 / 105 的现有 clip 组织方式）
4. **建 controller**（§2.5 / §3.5）
5. **改 prefab**：
   - 1013：换精灵 / rootScale / visualYOffset / controller，删 `AttackTelegraphAnchor`，改 `EnemyCommander` 三个 cast 状态名
   - 1051：复制 `Enemy_106_Fixed.prefab` → 改 enemyId 1051 → 换精灵 / rootScale / visualYOffset / controller
6. **回读校准** `visualYOffset`：按 `meshMinY` 反算，误差 ±0.02
7. **注册与接线**：
   - `Battle.scene` 的 `EnemyPool.enemyPrefabList` 追加 `Enemy_1051`（§3.7）；`Enemy_106_Fixed` 保留
   - `Formal_E0_Battle.asset` 的 14 个 106 槽位 → 1051（§3.7）；`Formal_N3_Battle.asset` 不动
8. **验收**

## 6. 验收口径

1. 静止脚底 = **-1.8**（读 prefab 的 `visualYOffset` 与导入后 `meshMinY` 反算）
2. 人物本体高 = **3.144 世界单位**（与 `Enemy_1012` 同高）
3. 受击：姿势保持 idle，**闪白 / 缩放 / 卡肉仍在**
4. 死亡：代码驱动的倒地照常
5. 击飞：代码驱动的抛物线照常；`Launched_Getup` 行为与接入前一致
6. 1013：号令 Cast 三段完整播放，与 104 的放箭 QTE 无关；`EnemyCommander` 的加攻落点仍在 t=0.8s
7. 1051：射箭投射物行为与 106 一致（固定落点按列 `-2/-1/0/1/2`、`Enemy_106_RowFlight`）
8. Console 0 error / 0 warning 新增

## 7. 待确认（只剩一条）

1. **1051 的 `enemyName`** 叫什么（106 原名「固定落点弓箭手」，105 原名「弓箭手」；1051 是曹军弓手，建议「曹军弓箭手」或沿用「弓箭手」）
