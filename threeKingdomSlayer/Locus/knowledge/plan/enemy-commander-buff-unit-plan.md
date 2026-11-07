---
id: kd_92f6e2f1-e96a-48c9-a4de-3d29761fa392
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
summary: 1013 督战单位（enemyId 1013）的**美术任务书（策划给美术）**：① 角色特征（设定 + 技能机制的事实：身份、不攻击、全场加攻、死亡减攻、0.8s 可打断前摇、在场口径、净效果曲线）；② 当前游戏画风（像素规范、实测配色与导入规格、已入库的 1012「v11 刀兵形象」为基点）；③ 当前游戏实机截图（战斗中的 1012、教学拍压暗、战斗全景）；④ 交付物与验收口径；⑤ 工程侧附录（机制与改动清单 M1–M9/预制体字段/临时替身）。形象怎么设计由美术决定，本文只给事实与约束。
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 1013 督战单位 · 美术任务书

> **策划给美术的输入文档**（2026-11-07）。
> **形象怎么设计由美术决定**；本文只提供三样东西：**① 角色特征（设定 + 技能机制）② 当前游戏画风 ③ 当前游戏实机截图**，
> 外加交付物清单与验收口径（不做就交不回来）。
>
> **工程侧**（机制实现、prefab 接线、波次编排）在 §6 附录，**美术可跳过**。
>
> 素材包：`Enemy1013ArtSource/`（身份图 / 母本 / 台账 / 实机截图）。
> 硬规范：`design/pixel-character-and-enemy-animation-spec.md`（像素硬规范，**必读**）。
> 管线经验：`memory/enemy1012-art-replacement-pipeline.md`（1012 那条线踩过的坑，**动手前必读**）。

---

## 1. 角色特征（设定 + 技能机制）

### 1.1 这是什么单位

| 项 | 内容 |
|---|---|
| id | `enemyId = 1013`（代号「督战鼓手」为临时名，可改） |
| 一句话 | **不攻击、给在场敌人叠加攻击力、死后给在场敌人永久减攻的敌方指挥单位** |
| 玩法定位 | 玩家第一个"**必须优先解决**"的目标（场上其它敌人都是"打哪个都行"） |
| 体量 | 一个**普通单位**：占 1 格 / 150 血 / 经验 150；**不是 BOSS**、不进 QTE、不做精英 |
| 所属阵营 | 与 1011 / 1012 同阵营（张飞军）→ 形象要与之同族 |

### 1.2 技能机制（美术要能画出来的部分）

| 动作 | 触发 | 效果 | 美术要表达的事 |
|---|---|---|---|
| **常态** | — | **不攻击、不造成任何伤害**；正常行军 / 补齐 / 受击 / 被挑飞 / 被眩晕，与普通敌人无异 | ① 他**不是挥剑的兵**；② 常态里能看出"他在指挥" |
| **施放（号令 · 加攻）** | 周期 **4.0s**（首次有 **2.5s** 前置延迟）；施法前摇 **0.8s** | **在场**所有敌人攻击力 **+10%**，可叠加，**上限 5 层**；本波内不过期 | ③ **号令前摇要能预判**（0.8s，玩家要能看见"要来了"并决定打不打） |
| **死亡（减攻）** | 死亡瞬间 | **在场**所有敌人攻击力 **−25%**（一次性，不叠加）；本波内不过期 | ④ 死亡要有"**号令中断 / 败退**"的读感（和普通杂兵倒地不同） |

**净效果曲线**（首版数值，玩家要能读懂这条）：

| 已号令次数 k | 0 | 1 | 2 | 3 | 4 | 5 |
|---|---|---|---|---|---|---|
| 全场累计加攻 | 0% | +10% | +20% | +30% | +40% | +50% |
| 死亡减攻 | −25% | −25% | −25% | −25% | −25% | −25% |
| **净效果** | **−25%** | **−15%** | **−5%** | **+5%** | **+15%** | **+25%** |

> **一句话教学口径**（画面要能帮玩家读懂这句）：*它每号令一次，全场敌人永久变强 10%；它一死，全场敌人永久变弱 25%。所以最好在它号令两次以内点掉它。*

### 1.3 "在场"的确切口径（决定号令覆盖谁）

| 计入 | 不计入 |
|---|---|
| 已生成进场的全部敌人 | BOSS |
| 正在入场行进的（已生成、已注册） | 它自己 |
| | 尚未生成的后续敌人 |

> 引擎侧事实：**一波敌人是一次性全部生成**的（只是错峰走进场）→ 所以第一次号令就覆盖本波全员；
> 死亡减攻只覆盖**死亡瞬间还活着**的那些。

### 1.4 号令会被打断（这条直接影响动作设计）

- 1013 **没有霸体** → 它的 0.8s 号令前摇**会被玩家任何直接伤害打断**（普通攻击 / 主动技能 / 大招）。
  灼烧、疾病、击退、挑飞、地刺等**不打断**。
- 打断后：本次号令作废（不加攻、层数不涨）、回到 Idle、**下一次重新计满 4.0s**。
- → **对美术的要求**：号令前摇必须是**一段连续、可被打断的动作**，玩家一眼能看出"这段能打断"；
  不能是瞬间切换的特效，也不能做成"一旦开始就必定完成"的仪式感动作。

### 1.5 它在战场上的行为约束

- 正常行军，**会补齐**（会随前排被打空一排排前移）→ 最终能进到前排被平 A 命中 → **不能画成"站着不动"**。
- 它活着 → 那一排永远算"没清空" → 后排不前移 → **它是一个必须被处理的硬门**（玩家不会无视它）。
- **不攻击** ⇒ 工程侧 `attackSequence` 清空、`attackDamage = 0` ⇒ **不需要 attack 动作**（省一支视频）。

---

## 2. 当前游戏画风

### 2.1 画风规范（硬性，必读）

完整规范见 `design/pixel-character-and-enemy-animation-spec.md`，要点：

| 项 | 规则 |
|---|---|
| 风格 | 高清 16-bit 像素街机风；东方三国武将感 + 幻想 RPG 厚重感；参考街机 / SFC / NeoGeo / GBA 后期高质量像素 |
| 比例 | **Q 版 2.5–3 头身**：大头、宽肩、短腿、厚重 |
| 轮廓 | 粗黑或深色像素描边 **1–3px**；轮廓优先于内部细节 |
| 明暗 | 硬边色块，**最多两阶阴影**；**禁止平滑渐变** |
| 材质 | 金属锐利高光 / 皮革偏暖棕 / 布料对比稍低 / 宝石高饱和但受控 |
| 朝向 | **所有敌人正面朝玩家**：不得侧身、3/4、横版；画面左 = 角色右 |
| 禁止 | 油画、厚涂、柔光、模糊、AI 插画感、过度抗锯齿、二次元比例、写实人体、欧美骑士、西幻游侠、低质量简笔像素、场景背景、文字、UI、伪透明棋盘格 |
| 通用动画帧 | 每帧独立单角色 PNG、透明 Alpha（不是白底/灰底/棋盘格）、装备完整在画布内、轮廓不触碰边界、同一动作画布尺寸统一、角色不逐帧漂移 |

### 2.2 本单位的形象基点：**已入库的 1012「v11 刀兵形象」**

> ⚠️ **不要用工作区里正在改的 1012「山贼版」**（圆顶盔 + 红缨那批，未入库）。基点是下面这版。

参考图（已备好）：

| 文件 | 用途 |
|---|---|
| `Enemy1013ArtSource/02_IdentityImages/base_1012_idle1.png` | **基点**：1012 正面站姿（1108×1108） |
| `Enemy1013ArtSource/02_IdentityImages/base_1012_walk1.png` | 步态参考 |
| `Enemy1013ArtSource/02_IdentityImages/base_1011_idle1.png` | 同阵营的 1011 重甲（**对比用**：1013 不能读成重甲兵） |
| `Enemy1013ArtSource/02_IdentityImages/1013_identity_1012base_greenscreen_1024.png` | **喂给视频模型的输入图**（绿幕 `RGB(0,159,62)`，主体高 696 / 脚底 895 / 中心 474.5） |
| `Enemy1013ArtSource/02_IdentityImages/1013_readability_compare_1011_vs_1012.png` | 1011 vs 1012 可读性对照（统一主体高 520、脚底对齐） |

**基点的装备语言（1013 要与之同族）**：

| 部位 | 1012（基点） |
|---|---|
| 头 | 深钢色**斗笠盔**（宽檐圆锥盔）+ **红色头巾/发髻**从盔下露出；黑色络腮胡、浓眉、怒目 |
| 上身 | **米白布衣**（交领）+ 棕色皮革肩带/胸带 + **深钢胸甲片**（护胸）+ 前臂金属护腕 |
| 腰 | 麻绳腰带 + **红布腰巾**（垂下） |
| 下身 | 棕色长裤 + **米白绑腿** |
| 足 | 木屐 / 木底鞋 |
| 武器 | 右手（画面左侧）持**直剑**：银亮剑身 + 金色护手 + 圆首 |

**实测配色**（从基点 PNG 取样的色簇，供对齐色族）：

| 用途 | 取样色 |
|---|---|
| 布衣 / 绑腿 | `#C0A878`（米白偏暖，占比最大） |
| 钢盔 / 胸甲 / 护腕 | `#484860` / `#303048`（冷调钢灰蓝） |
| 皮革 / 裤 / 靴 | `#483018` / `#603030`（暖棕） |
| 头巾 / 腰巾 | 暗红（阵营统一色） |
| 外轮廓 | `#181818`（近黑） |

### 2.3 技术规格（出图必须照这个来）

| 项 | 值 | 依据 |
|---|---|---|
| 画布 | **1108×1108**（与 1012 同族同规格） | 1012 idle1 实测 |
| 导入 | spriteMode 1（Rect）、**PPU 16**、filterMode **Point**、**无压缩**、alphaIsTransparency=1、maxTextureSize 2048 | 1012 `.meta` 实测 |
| pivot | idle 系 **0.6539, 0.5487**；attack/dead 系 **0.5, 0.5** | 1012 `.meta` 实测 |
| 落位口径 | idle：k=0.92366、脚底 897、中心 521 | 1012 管线实测 |
| 屏幕尺寸 | 与 1012 相当（1012 屏幕高 ≈ 5.76 世界单位） | prefab 实测 |
| Animator 状态契约 | `Idle / Walk / HitFlash / HitLeft / HitRight / Dead / Launched_Rise / Launched_Fall / Launched_Getup` | 双设备契约 §5 |

---

## 3. 当前游戏实机截图

> 截图 = **游戏实机画面**（1080×2160，竖屏）。拍摄于 2026-11-07，
> 场景为教学关 N1（Battle.scene，Y 路线场景 + 程序化天空盒）。
> **画面里的 1012 是已入库的「v11 刀兵形象」**（拍摄时临时换回，工作区那批山贼版已原样还原）。

| 文件 | 内容 | 美术要从中读出什么 |
|---|---|---|
| `Enemy1013ArtSource/03_InGameShots/01_battle_enemy_1012_committed.png` | **战斗中的 1012**（敌人 + 手势提示 + 教学字幕 + 完整 HUD） | 角色在**实机尺寸**下长什么样（缩小后还认不认得出兵种）、与地面/HUD 的比例关系、光照与接地阴影 |
| `Enemy1013ArtSource/03_InGameShots/02_teaching_text_and_dim.png` | 教学拍（背景压暗 + 字幕气泡） | 压暗态下角色与文字的对比度；**背景被压暗时角色仍然是亮的** |
| `Enemy1013ArtSource/03_InGameShots/03_battle_wide.png` | 战斗全景（无字幕） | 战场构图：地面、远景、道具（箱/旗/栅栏）、角色在画面中的位置与占比 |

**从截图里能直接读出的实机事实（给美术定尺寸与风格用）**：

- 相机：竖屏 1080×2160，**透视**；地面占下半屏，角色站在中下部；天空盒是程序化渐变 + 云。
- 角色在屏幕上**高度约 5.7 世界单位**，缩到 1/4 仍要能读出兵种 → **剪影与配色比细节重要**。
- 光照：方向光 + 接地阴影；角色与地面之间有软阴影。
- 战场元素：木质栅栏、破损木箱、红色旗杆、枯草、碎石；远景是山与云。
- HUD：底部有一条血条 + 左下角头像、右上角铜钱、左上角暂停；**中间大片区域是战斗画面**，角色不要指望靠 UI 区分。

---

## 4. 交付物与验收

### 4.1 要做的动作（**不需要 attack**）

| 动作 | 帧数参考 | 母本（`Enemy1013ArtSource/00_Index/videos.csv`） |
|---|---|---|
| Idle | 6 | 1012 idle 源片 |
| Walk | 周期 16 | 1012 walk 源片 |
| **号令 / 施法（Cast）** | 4–6 | **无母本** → 需单独生成（只描述这一个动作） |
| HitFlash / HitLeft / HitRight | 3 / 各 1–3 | 1012 受击源片（1012 的 controller 没有 HitRight → 会回落 HitFlash） |
| Dead | 6–8 | 1012 dead 源片 |
| Launched_Rise / Fall / Getup | 击飞链 | 1012 击飞三支源片 |
| ~~Attack~~ | — | **不做**（不攻击） |

### 4.2 制作方式

**一张图 + 一个母本**（替换视频工作流）：`reference_image` = 新身份图，`reference_video` = 动作母本 → **动作从母本继承**。

- ❌ 不用「首尾帧工作流 + 文字描述动作」→ 那等于把动作重新定义一遍（1012 那轮 4 支全飘的根因）。
- 母本里脸可见时表情随母本继承；**新动作的起手表情必须等于游戏内 idle 的实际长相**。
- 负向词只留四条不可违反的：嘴不张开 / 不露下齿 / 不变友善笑 / 不疲惫困倦。
- 一次动作只生成一次（反复叠轮边缘锐度会逐轮下降）；**先只做 idle 一支**，过了再批量。

### 4.3 交付物

| # | 交付物 | 路径 / 命名 | 规格 |
|---|---|---|---|
| 1 | 精灵 | `Assets/Sprites/Enemy/Enemy1013/Enemy_1013_<action><n>.png` | 1108×1108 RGBA、PPU 16、pivot 按 §2.3 |
| 2 | 身份图（归档） | `Enemy1013ArtSource/02_IdentityImages/1013_identity_final_1024.png` | 1024 绿幕（主体高 696 / 脚底 895 / 中心 474.5） |
| 3 | 源片与成片 | `Enemy1013ArtSource/01_SourceVideos/` | 母本 + 成片各一支 |
| 4 | 逐帧台账 | `Enemy1013ArtSource/00_Index/frame-provenance.csv/.md` | 每帧 → 源片帧号 + 落位参数 |
| 5 | Animator | `Assets/Animations/Animator_1013.controller` | 状态见 §2.3（+ `Cast`） |
| 6 | Prefab | `Assets/Resources/EnemyPrefabs/Enemy_1013.prefab` | 字段见 §6.4 |

### 4.4 验收清单

**单帧**：Alpha 干净无白边/棋盘格；装备完整不超框（四周 ≥15% 安全边距）；同动作内位置/脚底锚点/尺寸一致；描边 1–3px、最多两阶阴影、与 1012 基点同一画风；表情符合动作语义；新动作起手表情 = idle 的实际长相。

**序列**：动作递进清楚；角色不漂移不缩放、镜头静止；装备不增减（旗/鼓/剑不忽然出现消失）；首尾能衔接（walk 周期 16）。

**游戏内**：与 1012 并排**分得开**、与 1011 并排**不混**；体型与 1012 相当；号令前摇 0.8s 内能看出"在发令"，被打断时动作真的停住；受击方向与击飞链都有动画，缺状态时有回落且不报错。

---

## 5. 工程侧附录（给实现者，美术可跳过）

> 2026-11-05 定的机制与工程方案，**不随形象改版而变**。

### 5.1 三条设计不变量（改代码/改图都不能破坏）

1. **只用既有动词**：威胁必须落在 **{选列 / 换招 / 择时 / 用技能}** 四个动词上 —— 玩家**没有位移动词**（`Assets/Scripts/Player/` 下无 `transform` 位置赋值），任何"需要走位才能解"的威胁都是无解。
2. **不新增移动语义**：正常行军（**不设** `ignoreFillUpOrders`），位移走 `ColumnManager` 既有通道（`design/immutable-constraints.md`）。
3. **不改既有平衡口径**：不加新系统、不改通用伤害公式；倍率只作用在"**出站伤害**"这一层，且只作用于本单位的加/减攻。

### 5.2 它补的三层缺口

1. **优先级缺口**：现役 4 种"门"（1011 C技霸体 / 1021 盾卫 / 105 远程 / 109 冲锋）都是"打哪个都行"。
2. **敌方增益缺口**：场上所有增益都来自玩家侧（灼烧 / 疾病 / 冻结 / 连击 Buff），敌方没有任何"让局面变糟"的能力。
3. **"何时杀"的决策缺口**：把"先打谁"变成**时机成本**（号令层数）vs **时间成本**（去后排点它）。

**它是"硬门"**：活着 → 所在逻辑排永不清空 → 后排不前移 → 波次也无法结束 ⇒ 玩家必然要处理它。

### 5.3 伤害链：出站倍率要乘在哪三处

| 伤害来源 | 结算路径 | 乘点 |
|---|---|---|
| 近战 | `Enemy.Attack.PerformAttack()` → `EnemyManager.OnEnemyAttackPlayer(enemy)` → `PlayerState.TakeDamage` | 在 `OnEnemyAttackPlayer` 里算 `enemy.attackDamage × 出站倍率` |
| 骑兵冲锋 | `CavalryEnemy.ChargeDamage` = `chargeDamage × 冲锋格数 × 波次倍率` | 在 `ChargeDamage` 属性里乘（**独立伤害源，不改则 109 完全不吃增益**） |
| 远程箭矢 | `Enemy.Attack.SpawnProjectile()` 把 `attackDamage` 作为**快照**传出 | 发射时快照一次出站倍率 |

> `Enemy.attackDamage` 在 `Enemy.Init.ApplyWaveScaling()` 里已乘过波次 `damageMultiplier` → 出站倍率必须乘在它**之后**。BOSS 走同一条近战链，但被排除在加/减攻之外。

**打断与霸体口径**：敌人前摇被打断的判定在 `Enemy.Combat.TakeDamage()`：
`if (!inArmorWindow) CancelAttack(); else if (canInterruptCFrame || isHeavyAttack) CancelAttack();`（`inArmorWindow = isCFrame || isSuperArmor`）。
1013 不设 `isCAttack`、不是 BOSS ⇒ **无霸体 ⇒ 任何直接伤害都能打断它的前摇**。
"只有玩家攻击本体算数"的样板：`if (poiseDamage > 0f && inArmorWindow && feedbackSource == HitFeedbackSource.BasicAttack)`（来源枚举见 `Assets/Scripts/Data/HitFeedbackEnums.cs`，共 10 种）。

### 5.4 临时形象方案（工程跑通用，正式形象见 §1–§4）

| 项 | 做法 |
|---|---|
| 形象 | 复用 `Enemy_104.prefab` 的 `BOSS_idle`（32×32）、`localScale=0.18`、`visualYOffset=1.035`（屏幕高 5.76 单位 ≈ 1012 的 5.76） |
| 通用动作 | 复用 `Assets/Animations/Boss_104.controller`；建议复制为 `Animator_1013.controller` |
| 施法动作 | 复用放箭 QTE 三段 `QTE_TripleStab_Start`(0.50s) / `_Loop`(0.30s) / `_End`(0.30s)（`animator.Play(状态名)`，非 trigger） |
| 施法时序 | t=0 预警 0.3s → t=0.3 播 Start → **t=0.8 施放点**（施加加攻）播 Loop → t=1.1 播 End → t=1.4 回 Idle |

> **已核对**：这四个 clip 的 AnimationEvent 数均为 0 → 复用不会触发事件；箭矢由 `QTEController` 生成，1013 不挂它 → **不会放箭**。
> **两处已知缺口**：104 没有 `HitLeft` / `HitRight`（回落 HitFlash）、也没有 `Launched_Getup`（被挑飞后起身无动画，只报 Animator 警告）。

### 5.5 预制体字段（1013 目标值）

| 字段 | 目标 | 理由 |
|---|---|---|
| `enemyName` | 督战官（名可改） | 显示名 |
| `maxHealth` | 150 | 普通单位体量 |
| `attackDamage` | **0** | 配合"永不攻击"早退 |
| `attackSequence` | **清空** | 无攻击序列 |
| `isCAttack` | false | 无霸体 → 前摇可被打断 |
| `isBoss` | false | 不进 QTE、不吃加/减攻 |
| `ignoreFillUpOrders` | **不勾** | 必须正常行军 |
| `visualYOffset` / `localScale` | 按最终精灵实测重算 | 体型要与 1012 相当 |
| 组件 | 删 `QTEController`；加 `EnemyCommander`（X / 上限 / Y / 周期 / 首施前置 / 前摇） | |

**两个必做接线**：
1. `Assets/Scenes/Battle.scene` → `Battle/EnemyPool` → `enemyPrefabList`（**当前 13 项，Inspector 引用，不再扫目录**）→ 漏了运行时会 fallback 到临时 Cube。
2. 组件必须在**每次 `OnEnable`** 重新订阅 `enemy.OnDeath`（回池时 `ResetEnemy` 会把 `OnDeath = null`）。

### 5.6 技术改动清单（M1–M9）

| # | 文件 | 改动 |
|---|---|---|
| M1 | `Enemy.cs`（Init 段） | 缓存 `_commander` 组件引用（与 `_cavalry` / `_shieldGuard` 同模式） |
| M2 | `Enemy.StateSwitch.cs` → `StartAttacking()` | 开头早退 `state = EnemyState.Idle; return;`（复用骑兵分支写法；**已核对**行军完成分支独立调用 `CompleteRushMoveOrder()` → 不会卡住补齐队列） |
| M3 | `EnemyManager.cs` → `OnEnemyAttackPlayer()` | 伤害 = `enemy.attackDamage × 出站攻击倍率` |
| M4 | `CavalryEnemy.cs` → `ChargeDamage` | 同乘出站倍率 |
| M5 | `Enemy.Attack.cs` → `SpawnProjectile()` | 发射时快照一次出站倍率 |
| M6 | `EnemyStatusRegistry.cs` | 新增状态 id 常量 |
| M7 | `EnemyHealthBar.cs` + `Enemy.Visuals.cs` + `EnemyOutline.OutlineFacts` | ① 血条第三根状态条（加攻）+ 层数文本；② 新增 `AtkBuffed` 描边事实位 |
| M8 | `Enemy.cs` + `Enemy.Combat.cs` | 新增 `OnDamagedWithSource`（带 `HitFeedbackSource`）→ 区分"谁能打断号令" |
| M9 | `EnemyCommander.cs` | 订阅 M8：前摇被打断 → 取消施放、重置完整周期、回 Idle |

**倍率存放（选 B）**：`EnemyStatusAtkMod : IStatusEffect` 挂在 `StatusHost`，自带层数，UI 走 `EnemyStatusRegistry` 查询；回池由**既有** `Enemy.Pool.cs:75` `_statusHost?.ClearAll()` 统一清理。
**公式**：`最终倍率 = (1 + 加攻层数 × X) × (1 − 减攻层数 × Y)`（减攻只有一层）。
⚠️ `Enemy.StatusHost` getter 每次访问会跑 `EnsureResidentStatusRegistered()`（只补登记"冻结"）→ 加攻/减攻必须**每次施放/结算时重新 Add**。

**"在场"枚举写法**：

```csharp
var list = EnemyManager.Instance.GetAllAliveEnemies();   // ⚠️ 返回内部列表引用
for (int i = 0; i < list.Count; i++)
{
    var e = list[i];
    if (e == null || e == self || e.isBoss || e.state == EnemyState.Dead) continue;
    // 施加加攻
}
```

> **遍历期间不得修改该列表**；频率每 4s 一次、开销可忽略。

### 5.7 对标参考

| 参考 | 可直接用到的点 |
|---|---|
| `design/blades-gameplay-benchmark.md` | ① 敌人能力 = **数据引用**（`_knownAbilities`）⇒ "号令"宜做成能力表条目而非写死组件；② **表现三段解耦**（施法/弹道/命中）⇒ 我们只有施法段；③ 重攻击/破防是两条显式成长线 ⇒ "用重攻击打断号令"这条对策才成立 |
| `reference/game-design-references.md` | L4D2 的"**特殊敌人专属音效提示**" ⇒ 1013 出场加一声鼓声/号角比再加 UI 省事；Sekiro 的"破防 → 处决" ⇒ 未来做优先目标收益时参考 |

### 5.8 波次编排

**不在本次范围**（属关卡配置工作）→ 做完机制与形象也不会自动出现在任何关卡里。
