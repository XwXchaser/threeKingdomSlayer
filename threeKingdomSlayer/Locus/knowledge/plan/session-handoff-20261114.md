---
id: kd_9d5726f8-90ca-4b8e-805d-ab0b64e3a118
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 会话交接（2026-11-14）：破防演出 + 投壶兵 + 两个技能 + 美术规格

> ⚠ **本文已被取代** —— 当前交接入口见 **`plan/session-handoff-20261117.md`**
> （无限模式可玩 + 局外/局内经济地基 + 三选一拆分 + 弓箭手 BUG 修复）。
> 本篇保留作历史记录；里面的“下一轮先做 ①⑤⑥”已在新交接里重新排序。

> **给下一个对话的入口。** 先读本文，再按需展开引用文档。
> 本轮主线：① 破防演出包（REQ-006）② 新兵种「投壶兵」+ 两个技能 ③ 两个武器模型的四视图需求与 3D 制作规范。
> 架构体检见 `plan/architecture-review-new-features-20261114.md`（**下一轮先做其中的 ①⑤⑥**）。
>
> ★ **2026-11-14 续轮（拉取 `a9fbac88` 后）已完成架构 ①**，并修正了本文 §2 待办 5 的一处**勘误**。
> 续轮记录见 **§6**；架构侧见 `plan/architecture-review-new-features-20261114.md` §5。
>
> ★ **1014 角色与组图：v1 返图已被用户否决；v2/v3 已出图，v3 组图已于 2026-11-16 验收通过（见 §7.9 / §7.13）。** v1 仅保留为历史证据，不得作为形象参考。
> ★★ **下一项任务：修 1014 的 idle 姿态 —— 完整任务书见 §7.14（用户已把出方案交给别的 AI）。**
> ★ **2026-11-16 更新：idle「轻抛小水壶把玩」概念稿 v1 已出（§7.14.9）**，待用户验收；6 帧循环与本体高口径仍未定。阶段 B/C 未开始，未改 Unity 资产。

---

## 1. 本轮做完了什么（都已实测）

### 1.1 破防演出包（REQ-006）

| 项 | 状态 | 验证 |
|---|---|---|
| 枪的脱手动作 | **时长 0.26 → 2.0 秒**、**匀速曲线**（`k*(1.12-0.12k)` 改为 `k`） | 角速度 1.25°/帧量级，实测记录在 `Library/Locus/tmp/guardbreak-*.txt` |
| 破防瞬间 | 脆冲击（后坐 1× 0.33s）→ 缓慢脱手（2s） | 同上 |
| 命中确认 | 枪闪白 0.35s（`_FlashStrength` 通道）、接触点火花、屏幕红边框 0.40s、破防专属音（降调）、镜头推近（进 0.08 / **保持 0.55** / 回 0.3） | — |
| 攻击者 | 冻结 0.12s + 局部慢放 0.25× | 敌人 `Animator.speed` 每帧重申（见 §4 坑⑤） |
| 敌人侧 | `Enemy.projectilePoison*` 等新字段 | — |
| ❌ 已删除 | 屏幕模糊/冻结后处理（`GuardBreakTimeFx` + 着色器）与**全部全局 `timeScale` 操作** | 用户要求「完全去除拖影」 |

### 1.2 投壶兵（新远程，id **1014**）

- 新增玩家侧状态 `Status/PlayerPoisonStatus.cs`：**毒直接扣血** ⇒ 防御吸收不了、反射盾弹不走；
- `EnemyProjectile`：命中时**毒先于伤害**结算 ⇒ **防御只免直接伤害、毒照扣**；新增 `ShootDown()`；
- 玩家挥枪时身边的投射物**可被打落**（不伤害、不留毒）⇒ 远程成为高优先级目标；
- 新建 `Assets/Resources/EnemyPrefabs/Enemy_1014.prefab`（**占位：暂用 106 的外观**），已注册进 Battle 的 EnemyPool；
- **106 也临时带毒**（用户指定当载体）⇒ 想还原把 `Enemy_106_Fixed.prefab` 的 `projectilePoisonDamage/Duration` 清 0；
- ⚠ `projectileCanBeShotDown` **默认 false**（只有 1014 / 106 为 true）⇒ 弓手的箭不被顺手打掉。

**实测**：防御位命中陶壶 ⇒ HP 100→100（直接伤害被吸收）**但 poisoned=True**，毒持续掉血；
玩家挥枪 ⇒ 投射物被清除、**无伤害无毒**。

### 1.3 两个技能（已进 `ActiveSkillPoolConfig.rarePool`，weight=100）

| 技能 | 机制 | 实测 |
|---|---|---|
| **抡斧劈砍** `ActiveSkill_AxeSmash` | `Enemy.TakeDamage(ignoreShieldBlock)` ⇒ `ShieldGuardEnemy.ProcessHit(forceBreak)`：**正面全额 + 必定破盾** | 对照：普通攻击 10 打在盾卫正面只掉 **1** 血；抡斧 **90** 血 + 破盾（形态 `ShieldBroken`） |
| **追命锁链** `ActiveSkill_ChainPull` | 锁定**最远（rowIndex 最大）且最弱（当前血量最低）**的敌人 → 落到**本列最前空格**（不跨列 ⇒ 不重叠、不走补齐通道） | 远弱(row5,hp15) → **row0**；近强不动 |

**表现占位**（用户指示「斧头和锁链暂时用这杆长枪代替」）：
- 斧 = 另起一个长枪实例，**抡起 0.18s → 劈下 0.12s**，命中帧结算；
- 锁镰 = 长枪实例飞出 + `Effects/ChainLinkRig.cs`（**运行时生成的弹簧骨链**，9 节）+ 2D 链环贴图（`Enemy2/chain.png`）；
- ⚠ 3D 模型在透视相机下**会被 2D 精灵挡住**（sortingOrder 压不过深度）⇒ 正式模型建议带 2D 拖影/特效。

### 1.4 美术规格（★ 用户要的那份）

**`design/weapon-3d-art-spec-axe-and-chain.md`**：统一观察坐标、四视图交付物（单图 384×1024 / 合图 1536×1024 /
中文标注版 1856×1440）、**3D 制作规范**（尺度：斧 3.2 / 锁镰 0.9 / 链环外径 0.26；+Y=刃口方向、
原点=握持点；单材质 `Custom/WeaponColorUnlit` + `_FlashStrength`；`tipAnchor/tailAnchor` 硬要求；
Prefab 结构；面数 ≤8k）、两个武器形态要求、中英提示词与负面词、验收清单、6 个继承坑。
**代码侧已预留插拔位**：`axeSmashLevels[].weaponPrefab`、`chainPullLevels[].weaponPrefab`、`chainPullLevels[].chainLinkSprite`
（资产里已显式写出，当前为空）⇒ **模型到位只改资产、不改代码**。

---

## 2. 待办（按优先级）

| # | 事项 | 说明 |
|---|---|---|
| 1 | **防御姿态直接衔接攻击**（用户指示最后做） | 方案已出：`plan/week-20261114-new-enemy-two-skills-parry-counter.md` §3（互斥改单向 / 武器交接 A·B·C / 姿态插值 / 受击处理判定 / 收尾接回）；**待拍板 3 点** |
| 2 | **两个武器的 3D 模型** | 四视图需求已给（§1.4）；拿到图后做「图 → 模型」尺寸/锚点对照与 Prefab 接线 |
| 3 | **架构优化 ①⑤⑥** | `plan/architecture-review-new-features-20261114.md` §4 |
| 4 | **链链读感偏弱** | 链环偏小、链身偏短 ⇒ 调 `ChainLinkRig.linkScale / segmentLength / segmentCount` 或等正式链节美术 |
| 5 | ★ **主动技能放不出来** | ⚠ **勘误：实际不是问题。** `ActiveSkillInventory.UsesActiveSkills` 在 **Battle 场景里实测为 `true`**（规则版本 = `V2_ActiveSkill`）。代码里的默认值 `V1_LimitedItem` 只是**新挂组件时的缺省值**，Battle 场景的实例已被显式写成 V2 ⇒ **机器人拿得到、也放得出**。若哪天又「放不出」，先查这个场景实例的 `_ruleVersion`，不是查代码默认值。详见 §6.2 |
| 6 | 数值校准 | 抡斧 90/130/180（rows 2/3/4，CD 8/7/6）；锁链 25/40/60（CD 10/9/8）；陶壶毒 2.5×4s |
| 7 | 正式图标 | 两个新技能现在用海浪图标占位 |
| 8 | 投壶兵的正式外观 | 现在是 106 的外观占位。★ 美术规格：`design/enemy-1014-art-brief.md`；最新外部生成准备方案、提示词与验收闸门见本文 §7。用户将在别处发起生成；当前无生成结果、无 Unity 修改 |

---

## 3. 怎么验证（可复跑的配方）

**机器人跑关 + 拿技能 + 战斗内验收**（用户 2026-11-14 认可的手法）：
1. 运行时把新技能权重临时拉高（`ActiveSkillPoolConfig`：`rareWeight=1`、`common/legendary=0`、新技能 `weight=5000`）
   ⚠ **跑完必须还原**（`commonWeight=0.6 / rare=0.35 / legendary=0.05`，新技能 `weight=100`）；
2. 运行时挂 `BattleAutoPlayer`（`autoStart=true`、`clickHubStartButton=true`、`captureScreenshots=true`）
   ⇒ 它**自己点「开始关卡」**；三选一随机选卡（权重拉高后 2.6 秒就能拿到新技能）；
3. 在战斗里触发技能 + `ScreenCapture.CaptureScreenshot`（证据落在 `Library/Locus/tmp/bot/`）；
4. ⚠ 主动技能被 `UsesActiveSkills` 门禁拦住时，用反射调 `ActiveSkillRunner` 的私有 `Activate*` 绕过（仅测试用）。

**其它配方**：
- 破防时序采样：`Library/Locus/tmp/guardbreak-slow-departure.txt`（脚本见 `plan/guard-break-handoff-20261112.md` §10.3 的采样写法）；
- 投壶兵：`Library/Locus/tmp/pot-smoke*.txt`（防御位 + 毒 + 打落三段）。

---

## 4. 本轮踩到的坑（**别再踩**）

1. **本工程的程序集按 `.asmref` 分配**（目录里放 `Locus.Combat.asmref` 即并入该程序集）⇒
   **只看 asmdef 会判错归属**（例：`Assets/Scripts/Enemy/` 没有 asmdef，但它属于 **Locus.Combat**）。
2. **`.meta` 里的 GUID 是 base64 串，而 Unity 与场景 YAML 用 32 位 hex**
   ⇒ 拿 `.meta` 的串去 grep 场景**一定搜不到**（会误判「组件没落盘」）。
   正确查法：`AssetDatabase.AssetPathToGUID(path)` 拿 hex 再 grep，或直接读活场景。
3. **Unity 域重载会保留 ScriptableObject / 场景实例的 managed 内存值**
   ⇒ 只改 C# 默认值**运行时不生效**；新字段必须写进 `.asset` 并 `SaveAssets()`。
4. **`AddComponent` 会立刻触发 `Awake()`**
   ⇒ 组件里的初始化若依赖「之后才赋值的字段」，会在赋值前就把自己初始化成空
   （本轮链子不渲染的根因）。⇒ 初始化改由工厂方法在赋值后触发。
5. **`Sprite.Create` 需要纹理可读（Read/Write Enabled）**
   ⇒ 没开就抛异常，且会把后续初始化整段跳过（本轮又中一次）。裁剪改用「原图 + 放大」或先改导入设置。
6. **链环贴图按节长拉伸会变成一条黑杠** ⇒ 链环要**固定尺寸**，靠间距读出链节。
7. **2D 精灵与 3D 模型抢排序**：`sortingOrder` 只管 2D；3D 靠深度 ⇒ 3D 占位会被人群挡住。
8. **`ActiveSkillPoolConfig` 是真实资产**：临时改权重后**务必还原并 `SaveAssets()`**（本轮误存过一次，已还原）。
9. **机器人不点主动技能**（只处理三选一/对话/路线），且技能受 `UsesActiveSkills` 规则版本门禁。

---

## 5. 下一轮入口

| 目的 | 读 |
|---|---|
| ★ **1014 下一项任务（idle 姿态）** | **本文 §7.14**（完整任务书：已定项 / 三种读法 / 工程实测约束 / 可用资产 / 交付物 / 坑 / 待拍板） |
| ★ 1014 形象基准件（★ 后续一切工作的起点） | `Enemy1014ArtSource/02_IdentityImages/1014_v3_lineup_greenscreen_1024.png`（1024² 纯绿，主体高 696 / 脚底 895 / 中心 474.0） |
| 1014 形象需求书（唯一需求源） | `design/enemy-1014-art-brief.md`（§5.2 逐部位 / §5.5 脸谱 / §6 陶壶 / §8.2 精灵规格 / §10 验收清单） |
| 1014 组图产出记录（本轮全流程） | 本文 §7.9（v2 组图）/ §7.10（形象验收 + 裁切）/ §7.11（脸谱方案 C）/ §7.12（脸型 + 视线）/ §7.13（v3 组图，**已验收**） |
| 总体状态与本周计划 | 本文；`plan/week-20261114-new-enemy-two-skills-parry-counter.md`（§2 兵种候选 / §3 防御衔接方案 / §4 已实施与机器人自测） |
| 架构欠账 | `plan/architecture-review-new-features-20261114.md` |
| 破防演出细节 | `plan/guard-break-handoff-20261112.md` §10~§12；`design/guard-break-presentation-overhaul.md` |
| 无双/影之刃的破防口径 | `design/guard-break-dynasty-warriors-reference.md` |
| 武器 3D 制作规范 | `design/weapon-3d-art-spec-axe-and-chain.md` |
| 美术工作流与铁律 | `memory/art-production-workflow-entry.md`（S0~S5 阶段闸门） |
| 既有系统地图 | `design/game-mechanics.md`；`design/gameplay-pillars.md` |

**建议下一轮的第一件事**：★ **1014 的 idle 姿态**（本文 §7.14，用户已交由另一个 AI 出方案）；
工程侧则先做架构 ①（打落规则挪到攻击侧，纯重构、可机器人回归），再做 §2 待办 1（防御→攻击衔接，方案已备好，只差 3 个拍板）。

---

## 6. 续轮（2026-11-14，git 拉取 `a9fbac88` 后）

### 6.1 拉取到的内容

`git pull` 取到 1 个提交：

```
a9fbac88 feat(combat): 破防演出重做 + 投壶兵(1014) + 两个主动技能（抡斧劈砍 / 追命锁链）
```

**拉取后必须做的一步**：Unity 弹出「场景在磁盘上被外部修改」模态框 → **选 Reload**，
否则编辑器拿的是旧场景实例（表现为 `EnemyPool` 里没有 1014、技能池里没有两个新技能）。
选完 Reload 后编辑器会重新导入，**但仍可能用内存里的旧 Prefab 覆盖磁盘**（见 §6.3）。

### 6.2 勘误：主动技能其实放得出来

| 断言 | 实测 |
|---|---|
| 旧记录：「`UsesActiveSkills` 当前为 false（V1 道具规则）⇒ 机器人拿得到技能但 `TryActivate` 被门禁拦下」 | **不成立。** Battle 场景里的 `ActiveSkillInventory` 实例：`_ruleVersion = V2_ActiveSkill`、`UsesActiveSkills = True`、`Entries = 0`（未领技能而已） |

**根因**：`ActiveSkillInventory._ruleVersion` 的**代码默认值**是 `V1_LimitedItem`，
但 Battle 场景的实例已显式写成 `V2_ActiveSkill` ⇒ 读代码默认值会误判。
⇒ **以后查这条规则一定读场景实例，不要读代码默认值。**

顺带核实（拉取后实测）：

- `ActiveSkillPoolConfig.rarePool` = 6 项，**含** `ActiveSkill_AxeSmash`（effect=8）与 `ActiveSkill_ChainPull`（effect=9），权重均为 100；
  三个池权重已还原为 `common 0.6 / rare 0.35 / legendary 0.05`（**没有被留在临时的权重拉高状态**）✔
- `EnemyPool.enemyPrefabList` = **16** 项，**含 `Enemy_1014`** ✔（运行时日志 `注册敌人预制体: Enemy_1014 → enemyId=1014` 也印证）

### 6.3 一个必须知道的坑：拉取后 Prefab 会被内存旧值刷回磁盘

**现象**：`git pull` 后 Unity 重新导入，`Assets/Resources/EnemyPrefabs/Enemy_1014.prefab`
的 `projectilePoisonDamage/Duration` 从磁盘上的 `2.5 / 4` 变成 `0 / 0`，
`projectileCanBeShotDown` 从 `1` 变 `0` ⇒ **投壶兵既不中毒、也不可被打落**。

**根因**：Editor 内存里还是上一版的 Prefab 实例，导入时把内存版重新序列化回了磁盘。

**修法（本轮已用）**：不要 `git checkout` 去抢磁盘，而是**改内存实例再显式写盘**：

```csharp
var p  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/EnemyPrefabs/Enemy_1014.prefab");
var so = new SerializedObject(p.GetComponent<Enemy>());
so.FindProperty("projectilePoisonDamage").floatValue    = 2.5f;
so.FindProperty("projectilePoisonDuration").floatValue   = 4f;
so.FindProperty("projectileCanBeShotDown").boolValue     = true;
so.ApplyModifiedPropertiesWithoutUndo();
EditorUtility.SetDirty(p.GetComponent<Enemy>());
AssetDatabase.SaveAssets();   // ← 少了这一行，下次刷新又会被刷回去
```

**验收**：写入后 `git diff --numstat` 无差异、`git hash-object --path=...` 与 HEAD blob 哈希一致
（`9bb7d465…`）⇒ 内容已完全对齐；`git status` 仍可能显示 `M`，那只是 `core.autocrlf=true`
造成的换行符伪差异，不是内容变更。

### 6.4 架构 ① 已完成：「投射物打落」规则挪到攻击侧

**改动（3 个文件，+42 / −20）**：

| 文件 | 改动 |
|---|---|
| `Assets/Scripts/Enemy/EnemyProjectile.cs` | 删掉 `TryShootDown()` 及其在 `UpdateFlight()` 里的调用；`ShootDown()` 保留但降为**被动**方法（补了注释说明「何时打落」由攻击侧决定） |
| `Assets/Scripts/Player/Combat/AttackSystem.Types.cs` | 新增 `ShootDownProjectilesNear(Vector3, float)`，**紧邻**既有的 `DeflectProjectilesNear`（反弹通道）⇒ 两条同类规则同源同位置 |
| `Assets/Scripts/Player/Combat/AttackSystem.cs` | `Update()` 里在**动作锁未结束**时调用打落扫描 |

**口径与旧实现对齐的两点（都是刻意保持行为不变）**：

1. **触发窗口 = 整个动作播放期**（动作锁 `_actionLockTimer > 0`），**不是**只在命中帧。
   （架构体检原文建议「在攻击命中帧调用」，但那会**缩短**打落窗口 ⇒ 不是纯重构 ⇒ 未采纳。）
2. **世界停走时不打落**（`Time.timeScale > 0`）。
   旧实现挂在投射物的飞行 tick 上，而 `timeScale=0`（教学时停 / 暂停菜单 / 三选一面板）时那个 tick 不跑
   ⇒ 旧行为就是「停走时不打落」。**不写这条会让教学拍被远程偷伤。**

**实测（Play Mode，三态对照，机器人驱动进战斗）**：

| 场景 | 投射物 | 结果 | 玩家 HP | 中毒 |
|---|---|---|---|---|
| 出招中 | `canBeShotDown=true` | **被打落** ✔ | 不变 | 无 ✔ |
| 出招中 | `canBeShotDown=false`（弓手口径） | **仍在飞** ✔（未被顺手打掉） | — | — |
| **不出招** | `canBeShotDown=true` | **仍在飞** ✔（没有动作锁就不打落） | — | — |

运行时调用链（Console 取证）：

```
[EnemyProjectile] 被击落 id=-45590 pos=(0.00, 0.81, 2.49)
  EnemyProjectile:ShootDown ()                        (EnemyProjectile.cs:176)
  AttackSystem:ShootDownProjectilesNear (Vector3,single) (AttackSystem.Types.cs:612)
  AttackSystem:Update ()                              (AttackSystem.cs:390)
```

对照组投射物随后正常落地（`EnemyProjectile:OnArrival` → `PlayerState.TakeDamage`）⇒ 未被打落 ✔

**副作用**：`EnemyProjectile` 不再引用 `AttackSystem` / `PlayerState` ⇒ 敌人层不再反向依赖玩家层。

### 6.5 架构 ⑤+⑥ 已完成（2026-11-14 续轮）

**新增** `Assets/Scripts/Effects/SpringBoneChain.cs`：**共用的弹簧骨链求解器**（普通类，不是 MonoBehaviour 基类）。
- 共用部分：速度积分（弹簧 + 重力）、指数阻尼、径向速度剔除、速度上限、120Hz 子步进。
- 不共用（各自实现）：红缨的**静止朝向 / 瞬移重置 / 枪杆避让 / 角度上限**；铁链的**距离约束 / 链尾钉手 / 链头外部驱动**。
- ★ **为什么用组合而不是继承**：红缨挂在玩家的长枪预制体上、字段（bones/tip/weaponTip/weaponTail）是**序列化引用** ⇒ 改继承关系 / 搬字段有丢引用风险 ⇒ 两个组件的**序列化字段一个都没动**。

**红缨的物理表现**逐帧数值**完全没变**（见 §6.6 的回归证明）。

**⑥ 魔法数字进资产**：`ChainPullLevelConfig` 新增 8 个字段，**已显式写进** `ActiveSkill_ChainPull.asset`（结构体新字段在旧资产里会读成 0，**必须写盘**）：
`chainSegmentCount 9` / `chainSegmentLength 0.95` / `chainLinkScale 3.4` / `chainStiffness 900` / `chainDamping 40` / `chainGravityScale 0.22` / `chainThrowSeconds 0.25` / `chainReturnSeconds 0.2`。
`ActiveSkillRunner` 不再出现 `9` / `0.95f` / `0.25f` / `0.2f` 这类裸数字。

#### ★★ 顺手定位并修掉一个**真 BUG**：链身一直像「几块碎片」

上一轮记录里那句「链环偏小、链身偏短（读感像几块碎片而不是一条链）」，**根因不是尺寸，是约束**：

| | 旧链（重构前） | 新链（重构后） |
|---|---|---|
| 甩出末帧链环 z（甩到 z=10） | `0.0 0.0 0.0 0.0 0.0 0.0 0.0 0.5 5.5` | `0.4 1.1 1.5 1.8 1.9 2.0 2.2 4.1 8.0` |
| 绷紧末帧 | 全挤在 0（**根本没伸出去**） | `0.5 1.4 2.3 3.2 3.9 4.5 5.0 7.5 10.0` |
| 末节→链头距离 | 4.5（**断开**） | 0.04（**接上了**） |

**根因**：旧约束用「**当前朝向**」算下一节的位置（follow-the-leader），而链一上来就**塌在一点**
（`Setup(hand, hand)`：钩子还在手上）⇒ 当前朝向退化 ⇒ 永远拉不开。
**修法**：中间节的目标方向改取「**当前节 → 链头**」（全局方向，永远有定义）⇒ 链自己会展开。

**硬度是实测出来的，不是拍的**：k=140 时链追不上链头（绷紧后仍差 **2.38** 单位）⇒ k=900 / d=40（ζ=40/(2·√900)≈0.67，略欠阻尼留一点甩劲）才能拉直。
⇒ 这也正是把参数搬进资产的价值：**现在调硬度不用改代码**。

### 6.6 回归证明（怎么确认没改坏）

**红缨逐帧数值夹具**（`Library/Locus/tmp/tassel-*.txt`，说明见 `tassel-regression-harness.md`）：
建一条 6 节骨骼链 + 枪杆，固定步长 `Simulate(1/60)` × 180 帧，每帧导出 6 节的 `localRotation`（F6）。
- 重构**前**：`tassel-before.txt`
- 重构**后**：`tassel-after.txt`
- ★ 结论：**两份文件逐字节完全一致**（`before == after` ⇒ True）⇒ 红缨的物理表现一个像素都没变。

**其它回归**：
- `EnemyProjectile` 的成员里已**不含任何** `AttackSystem` 引用 ✔（架构 ① 的反向依赖已断开）
- `SpringBoneChain` 的子类只有两个：`ChainLinkRig+ChainSolver` / `WeaponTasselDynamics+TasselSolver` ⇒ 确实共用，不是复制粘贴 ✔
- 链的确定性扫描（Edit Mode，无头）：无 NaN；相邻链环长度偏差稳定；绷紧阶段末节→链头 = 0.04 ✔
- Play Mode 实跑技能（领 `ActiveSkill_ChainPull` → `TryActivate`）：链环从玩家手**连续铺开**到钩子、绷紧时拉直、回收时卷回 ✔

### 6.7 续轮之后，下一个该做什么

| 优先 | 事项 | 状态 |
|---|---|---|
| 1 | **REQ-010 防御→攻击衔接** | ★ **用户已拍板（2026-11-14）**：武器交接 = **方案 A（同一把枪连续运动）**；**不允许边防御边输出**（出招即脱离防御态）⇒ 可以开工了 |
| 2 | 架构 ②：`Enemy.TakeDamage` 分组参数（**只给新代码**引入 `HitOptions`，旧调用点逐步迁移） | 可做，收益一般 |
| 3 | 待办 2/4/6/7/8：3D 模型（等用户出图）、链链读感（**现在可从资产里调了**）、两个技能数值、正式图标、投壶兵正式外观 | 等用户 |
| 4 | 架构 ③④⑧：`ActiveSkillRunner` 上帝类拆分 | **攒够 2~3 个新技能再做**，别和新功能混批 |

## 7. 1014 正式外观生成方案（2026-11-16，v2 修订，待外部生成）

> 本节是下一处对话/外部图像生成入口。用户将在别处发起生成；本项目当前只保存方案，不执行生成、不改 Unity 资产。
> **当前优先级**：本文早先 §7.2–§7.4 的“大陶壶、双手抱壶、偏制式轻甲”方向已被用户否决；最新方案见 §7.8，覆盖所有冲突的角色气质、服装、水壶尺寸、idle 姿态和 attack 方向。
> **口径变更说明**：`design/enemy-1014-art-brief.md` §1.2 / §7.1 早先写“按人工制作、不走生成接口”；本节记录用户之后明确要求准备外部生成方案的最新指令。美术需求书尚未改写，执行时以用户在外部发起生成时的指令为准。
> 机制真源：`design/enemy-1014-javelin-thrower-mechanics.md`；美术规格真源：`design/enemy-1014-art-brief.md`。

### 7.1 方案总则：先组图锁家族比例，再做身份图

1014 采用已验证的“组图锁比例、单图锁身份”方法：先把新角色放入现有兵种并排图，确认共同体型、脚底基线、画风和阵列宽度，再做单角色身份图；组图只用于比例/画风研究，不能直接裁切为 Unity Sprite。

生成顺序固定：

```text
A. 六人并排组图
B. 验收组图后，制作 1014 单角色身份图
C. 验收身份图后，制作独立陶壶透明道具图
D. 以上通过后才进入投掷 attack 动作
```

本轮不做 attack 视频、不做精灵序列、不替换 Prefab。

### 7.2 阶段 A：六人并排组图

**成员与顺序固定**：

`1011 重甲刀兵 / 1012 兜鍪贼兵 / 1013 督战旗手 / 1051 弓手 / 1021 轻装楯兵 / 1014 投壶兵`

**规格**：横向 `1536×1024` 或更大、比例 3:2；恰好六人、全部正面、同一身体方向、脚底同一视觉基线；无透视远近、场景、地面透视、战斗特效、文字、编号、UI、Logo、水印或棋盘格；人与人之间留出间隙，道具不得压到邻人。

**参考图职责**：

| 输入 | 只负责 |
|---|---|
| `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle1.png` | 共同体型、体量与画风基准 |
| `Assets/Sprites/Enemy/Enemy1012/Enemy_1012_idle1.png` | 山贼脸、布衣与皮革语言 |
| `Assets/Sprites/Enemy/Enemy1013/Enemy_1013_idle1.png` | 旗手服装与旗杆/旗面关系 |
| `Assets/Sprites/Enemy/Enemy1051/Enemy_1051_idle1.png` | 弓手裹巾、木弓与箭袋 |
| `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_idle1.png` | 轻装楯兵老卒脸、布袍、小甲片与盾 |

1011 只作为全体风格/体型基准；前五个角色的身份细节保持已有形象；不使用历史生成图，不使用 1014 当前弓箭手占位图作为造型参考。

**1014 设定**：露脸、闭嘴硬线、压眉直视；束发髻和窄额布带；暗青灰交领布袍、少量深蓝灰肩胸甲片、暖棕厚护腕、暗棕裤、灰白绑腿、厚底战靴；轻装投掷兵，不持刀、剑、枪、弓、箭袋、旗、旗杆、盾或其它武器。身体微前倾但双脚站稳。

陶壶放在**画面左侧、角色右手侧**，由双手在胸腹前抱住。形态为圆鼓腹、收束短颈、短流口、两侧小环耳的素陶粗壶；暖土黄/灰褐，只有轻微烧制色斑、粗砂感和一两道暗裂纹；可有暖棕麻绳/皮绳及一小段暗红布条。禁止釉、彩绘、花纹、金边、符文、绿色发光、毒液外溢、葫芦、花瓶、酒壶、双耳大罐或炸弹造型。

组图中 1014 人物本体高必须为 1011 的 `0.94–0.99` 倍；按 1108² 游戏精灵口径折算，目标为 `648–682 px`（不要把这组绝对像素值直接套到 1536×1024 组图画布）。陶壶、武器、旗杆、旗面和盾不计入人物身高；总宽不横向压过邻列。

**中文提示词基线**：

```text
制作一张横向三国战争题材高清像素化动作游戏角色比例研究组图。
画面中必须恰好有六名不同的曹军敌兵，从左到右严格排列为：1011重甲刀兵、1012山贼刀兵、1013督战旗手、1051弓手、1021轻装楯兵、1014投壶兵。
六人全部正面朝向观察者，身体方向一致，双脚站在同一条视觉基线上，不能有透视远近变化，不能通过前后站位制造大小差异。六人的人物本体高度属于同一个短壮、厚重、紧凑的三国敌军家族，头肩、胸腹、腰胯、腿部、手脚比例统一。
整体画风严格匹配1011已验收精灵：高清像素化三国战场动作美术，成簇明暗色块、阶梯状轮廓、清楚的金属体积和锐利高光；使用冷蓝灰钢、暖棕皮革、暗色布料和少量暗红识别色。不要低分辨率像素画、平滑厚涂、照片写实或马赛克滤镜。
保持1011重甲刀兵、1012山贼刀兵、1013督战旗手、1051弓手和1021轻装楯兵的既有身份与道具。1014设计为新的曹军基层投壶兵：露脸、闭嘴硬线、压眉直视，束发髻和窄额带，暗青灰交领布袍，少量深蓝灰肩胸甲片，暖棕厚护腕，暗棕裤、灰白绑腿、厚底战靴；不持刀、剑、枪、弓、箭袋、旗、旗杆、盾或其它武器。
1014唯一主要识别物是一只抱在身体正前方的粗陶壶。陶壶位于画面左侧、角色右手侧，由双手在胸腹前稳定抱住；陶壶圆鼓腹、短颈、短流口、两侧小环耳，素陶暖土黄或灰褐，有轻微烧制色斑和粗砂质感，无釉、无花纹、无金边、无符文、无绿色发光，不横向压到邻人。
1014人物本体高度约为1011的0.94至0.99倍；陶壶、弓、盾、刀、箭袋、旗面和旗杆不计入人物本体高度。六人之间必须有清楚间隙，每个人的头、肩、躯干、双手和双脚都能单独辨认。背景为单一中性纯色，无场景、地面透视、战斗特效、文字、编号、UI、水印或棋盘格。输出只用于比例和画风研究，不是最终游戏精灵。
```

**负面词基线**：

```text
seven characters, fewer than six characters, duplicated character, missing character, wrong order, side view, three-quarter view, turned body, inconsistent baseline, perspective depth, oversized 1014, tiny 1014, oversized pottery jar, jar covering the torso, prop overlapping neighboring character, weapon overlap, cropped feet, cropped head, extra sword, extra knife, extra spear, extra bow, extra quiver, extra shield, extra flag, 1014 wearing helmet, 1014 wearing wide-brimmed hat, 1014 wearing face mask, 1014 wearing heavy full plate armor, 1014 as a mage, glowing green poison jar, magic staff, runes, painted ceramic pattern, gold trim, glazed pottery, vase, gourd, wine bottle, large amphora, bomb, modern grenade, fantasy armor, European knight, cute chibi, toy-like, glossy 3D plastic, smooth rendering, airbrushed shading, painterly rendering, soft gradients, random pixel noise, blurry edges, gray halo, white halo, ground shadow, scenery, text, labels, numbers, logo, watermark, UI, checkerboard
```

**英文提示词基线**：

```text
Create a horizontal character proportion lineup for a high-definition pixelized Three Kingdoms action game. Show exactly six Cao Wei enemy soldiers in this order from left to right: 1011 heavy armored swordsman, 1012 bandit swordsman, 1013 standard bearer, 1051 archer, 1021 light shield soldier, and 1014 pottery-throwing soldier. All face directly forward with identical body orientation and feet on one shared baseline. Use no perspective depth or near/far size change. Keep the human bodies compact, short and sturdy, with consistent head, shoulders, torso, hips, legs, hands and feet. Match the approved 1011 pixel-art style: clustered color masses, stepped silhouettes, readable cold blue-gray steel, warm brown leather, dark cloth and restrained dark red accents. Do not use smooth painted rendering or a mosaic filter. Keep the first five soldiers recognizable with their existing equipment. Design 1014 as an exposed-face, closed-mouth, lowered-brow Cao Wei pottery thrower with tied hair, narrow forehead band, dark blue-gray crossed-collar robe, a few small armor plates, thick brown bracers, dark trousers, pale leg wraps and heavy boots. He carries no sword, knife, spear, bow, quiver, flag or shield. His defining prop is one unglazed rough ceramic jar held with both hands in front of the chest and abdomen, on the left side of the image, corresponding to the character's right hand. The jar has a round belly, short neck, short spout and two small loop handles, with muted earthenware colors and no pattern, glaze, gold trim, runes or green glow. Keep 1014's human body at 0.94 to 0.99 of 1011; props do not count as body height. Use clear gaps, a single neutral solid background, and no scene, ground perspective, effects, text, UI, logo or watermark. This is a proportion study, not a final game sprite.
```

### 7.3 阶段 B：1014 单角色身份图

仅在阶段 A 组图通过后执行。参考图使用：通过验收的六人组图；组图中 1014 的裁切件（只负责体型、服装结构、陶壶位置与手性）；`Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle1.png`（只负责像素笔法与光影密度）。不要让其它兵种重新决定 1014 的身份。

**提示词基线**：

```text
基于已确认的六人比例组图，制作一张单独的1014投壶兵身份图。只保留1014，保持组图中已确认的人物比例、服装结构、陶壶大小、陶壶位置、手性和高清像素化画风，不要变成更平滑或更细腻的另一种风格。
1014是露脸的曹军基层投掷兵：束发髻，额头窄布带，闭嘴硬线，压眉直视；暗青灰交领布袍，少量深蓝灰肩胸甲片，暖棕厚护腕，暗棕裤，灰白绑腿，厚底战靴。双脚分开站稳，身体轻微前倾但不夸张。
陶壶位于画面左侧、角色右手侧，由双手在胸腹前抱住。壶为圆鼓腹、短颈、短流口、两侧小环耳的素陶粗壶，暖土黄或灰褐，有少量烧制色斑和粗砂感；禁止釉面、花纹、金边、符文、绿色发光和毒液外溢。
正面全身，头顶、双手、陶壶和双脚完整入画。不要刀、剑、枪、弓、箭袋、旗、旗杆、盾、兜鍪、斗笠、面甲或魔法道具。表情中等凶狠，不龇牙、不张嘴、不瞪大眼。不要场景、地面、投影、文字、UI、水印或其它角色。
```

**单图规格**：`1024×1024 PNG`；纯绿幕 `RGB(0,159,62)`；主体高 `696 px`；可见脚底线 `y=895`；身体中心 `x=474.5`；四周至少 15% 安全边距；封闭孔 0。外部工具若无法稳定输出精确绿幕/落位，先保留干净主体，再本地合成绿幕和校准；不接受白边、灰边或半透明光晕。

### 7.4 阶段 C：陶壶独立道具图

仅在阶段 B 身份图通过后执行。独立做单个粗陶投掷壶，不从身份图直接裁切；后续飞行、旋转和命中特效需要独立控制。

- 单个陶壶，正面略带三分之四角度，完整显示鼓腹、短颈、短流口和两侧小环耳。
- 素面粗陶，暖土黄至灰褐，少量烧制色斑、粗砂感和一两道暗裂纹；颈部可有暖棕麻绳/皮绳及小段暗红布条。
- 透明背景，轮廓干净，无白边/灰边、角色、手、地面、阴影、文字或 UI。
- 禁止釉面、彩绘、花纹、金边、符文、绿色液体/发光、毒雾、葫芦、花瓶、高足酒壶、双耳大罐、炸弹或现代手雷。
- 目标尺寸：壶身直径 `115–135 px`；含颈与流口高度 `145–165 px`；PNG + Alpha。

**陶壶独立图提示词基线**：

```text
制作一张汉末民用粗陶投掷壶的独立游戏道具图。只显示一个陶壶，正面略带三分之四角度，完整显示圆鼓腹、收束短颈、短流口和两侧小环状系绳耳。壶身为素面粗陶，暖土黄至灰褐色，带少量自然烧制色斑、粗砂质感和一两道暗色细裂纹。颈部有一圈暖棕麻绳或皮绳，绳上可系一小段暗红布条。透明背景，轮廓干净，边缘无白边和灰边。禁止釉面、彩绘、花纹、金边、符文、绿色液体、绿色发光、毒雾、葫芦、细颈花瓶、高足酒壶、双耳大罐、炸弹、现代手雷、角色、手、地面、阴影、文字或 UI。
```

**英文提示词基线**：

```text
Create a single Han-period rough ceramic throwing jar as an isolated game prop. Show one jar in a near-front three-quarter view, fully readable with a round belly, short neck, short pouring spout and two small loop handles. Use unglazed warm ochre to gray-brown earthenware with subtle firing marks, coarse ceramic texture and one or two dark hairline cracks. Add one warm brown rope or leather cord around the neck, optionally with a small dark red cloth strip. Use a transparent background with clean edges and no white or gray fringe. No glaze, painted pattern, gold trim, runes, green liquid, green glow, poison mist, gourd, narrow-neck vase, tall wine vessel, large amphora, bomb, modern grenade, character, hand, ground, shadow, text or UI.
```

### 7.5 外部生成后的验收闸门

```text
1. 六人并排组图
2. 验收比例、画风、1014身份、陶壶位置与手性
3. 1014单角色身份图
4. 验收尺寸、绿幕、Alpha、落位和细节
5. 陶壶独立道具图
6. 验收道具尺寸、Alpha、边缘和素陶材质
7. 通过后才进入投掷 attack 动作
```

**组图验收**：恰好六人且顺序正确；全正面、同基线、无透视；比较人物本体（排除帽缨、旗杆、旗面、武器与道具）后，1014 高度为 1011 的 `0.94–0.99` 倍；陶壶不遮邻人；缩小至约 25% 仍读成“抱壶投掷兵”；1014 不比前五人更平滑；无额外武器。将比例折算到 1108² 精灵规格后再核对 `648–682 px`，不要把这个值直接当组图画布像素验收。

**身份图验收**：1024²、纯绿 `(0,159,62)`、主体高 `696`、脚底 `895`、中心 `474.5`、封闭孔 0；露脸闭嘴；陶壶为素陶且仍能认出流口/鼓腹；无其它兵种装备。

手性必须人工目视核对，不能只相信提示词；组图不能直接裁切为 Unity Sprite；用户验收身份图前不做动作视频；本轮不改 Unity 资产。

### 7.6 交付路径与当前状态

- 组图归档：`Enemy1014ArtSource/03_Lineup/`
- 身份图归档：`Enemy1014ArtSource/02_IdentityImages/`
- 陶壶贴图目标：`Assets/Sprites/Enemy/Enemy1014/Enemy_1014_pot.png`
- 当前状态：**阶段 A 组图 v1 已出图但用户已明确否决；不得继续使用 v1 形象或从 v1 裁切身份图。** v2 生成方案见 §7.8，阶段 B、C 未开始；无 Unity 接线。详见 §7.7。

### 7.7 阶段 A 执行记录（2026-11-16）

**提交**：`Tools/visual_brief/submit_enemy_proportion_lineup_1014.py --approve`（提示词与参考图清单外置在 `Tools/visual_brief/prompts/enemy_proportion_lineup_1014.*` / `refs_enemy_proportion_lineup_1014.json`，为唯一真源）。

| 项 | 值 |
|---|---|
| 接口 / 模型 | `POST /v1/images/edits` / `gpt-image-2.6-sunburst` |
| 参数 | `size=1536x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1` / 不传 `background` |
| `image[]`（5 张，1.90 MB） | 五张**已部署 idle 精灵帧**（1011/1012/1013/1051/1021），职责逐张写进提示词；1014 无输入图 |
| 结果 | HTTP 200，用时 117.89s，1536×1024 PNG color type 6，1,578,920 bytes，sha256 `8b246a2b…06dd`，usage total 9794（image 5488 / text 1506 / input 4306） |
| 归档 | `Enemy1014ArtSource/03_Lineup/enemy_proportion_lineup_1014_v1.*`（含 prompt / request / status / measure / annotated）+ `00_Index/lineup-images.csv` + 包 README |

**量测（`Tools/visual_brief/measure_lineup_figures.py`，alpha≥128 + 4 邻接 BFS）**：恰好 6 人、顺序正确、脚底同基线 `y=700`；本体高 1011=341 / 1012=328 / 1051=316 / 1021=312 / **1014=311**（1013 被旗杆污染）；**1014/1011 = 0.912**。

**达标**：1014 露脸闭嘴、无武器、双手抱素陶粗壶（鼓腹+短流口可辨、无釉无金边无绿光）；陶壶在**画面左侧**（躯干带质心 1379.9 < 头带质心 1395.5）⇒ 角色右手侧。

**未达标（需用户拍板）**：
1. **画风**：六人整体渲染成**平滑厚涂**，不是工程的成簇色块像素画 —— 与 `memory/enemy-proportion-lineup-reference.md`「教训八」同一现象（五张输入是像素精灵帧 ⇒ 模型当形体参考重绘）。
2. **背景**：透明底 + 人物周围暖色光晕（alpha 17–127 占 4.74%），非单一中性纯色。
3. 1013 旗面又自发长出**金色回纹边框**（跨轮复现的已知偏差）。
4. 1014 身高比 0.912：落在本图实际家族带（0.912–0.962）内，但低于需求书的 0.94–0.99。

**若要出 v2**：把 `image[]` 换成**像素风锚图/身份锚图**（v4 组图那套输入：1011 绿幕锚图 + 1012/1013/1051/1021 锚图件），正文写死「成簇色块 / 阶梯轮廓 / 纯中性色块背景 / 禁止平滑渐变」；**不得降级输入图**（见 `skill/gpt-image-generation.md` 的 400 排查纪律）。

相关经验：`memory/enemy-proportion-lineup-reference.md` 记录了 1013/1021 组图中比例测量、道具包围框、手性反转、单图画风磨平和组图不能直接裁切的实际教训；`memory/art-production-workflow-entry.md` 规定了 S0-S5 阶段闸门与“idle → 关键动作 → 通用动作”的顺序。

### 7.8 v2 修订方案：山贼式民兵 + 小型单手水壶（当前真源）

**用户反馈（2026-11-16）**：v1 造型和气质不对；不要重甲/制式轻甲风格，要接近 1012 山贼气质的基层民兵着装；不要酒缸大小的壶，要普通随身水壶，后续动作像扔手榴弹一样单手投掷。

**v1 处置**：

- 阶段 A v1 组图保留为历史证据，不再作为形象参考。
- v1 的 1014 不得裁切、不得用于身份图、不得用于动作图、不得继续修补成正式造型。
- v1 的“大陶壶、双手抱壶、偏制式轻甲”方向全部废弃。
- v2 重新出组图；v2 组图通过后才制作身份图，不能跳过比例闸门。

**角色方向**：1014 是与 1012 同源的山贼式基层民兵，不是陶工、重甲兵、法师或专门的制式投壶兵。

- 气质：粗粝、实用、非正规军、基层民兵；表情直接凶悍但克制，露脸、压眉、闭嘴。
- 服装：旧灰米色/暗褐色交领粗布衣、暗棕皮革肩带、腰带和小布包、暗色裤子、灰白绑腿、旧布鞋或厚底鞋。
- 头部：窄头巾、束发或旧布帽；可参考 1012 的民兵气质，但不要复制 1012 的完整造型。
- 装备：轻便、不整齐、无全身甲；删除之前的肩胸甲片和重型护腕感，只保留普通布护腕或窄皮腕带。
- 识别差异：1012 是斗笠+红头巾+刀；1014 是民兵粗布衣+小水壶+单手投掷预备，不带刀剑弓箭袋旗盾。

**水壶方向**：

- 普通随身粗陶水壶，像民兵携带的饮水壶，不是酒缸、酒坛、大陶罐或炸弹。
- 一只手可以握住；壶身略扁圆腹、短颈、小壶嘴、一个小把手或提绳。
- 身份图中的目标高度约为 `85–100 px`，约为人物本体的 `1/8–1/10`；壶身直径约 `55–75 px`。
- 壶可以挂在腰侧或由角色右手自然握住；不能遮住胸腹，不能成为身体的第二个主体块。
- 素陶暖棕/灰褐，少量烧制色斑即可；无釉、无彩绘、无金边、无符文、无绿色发光、无毒液外溢。

**v2 身份姿态（历史方案；当前 Idle 姿态以 §7.14 为准）**：

- 正面站立，双脚稳，接近 1012 的民兵站姿。
- idle 不双手抱壶、不展示大壶、不做搬运酒缸姿势。
- 水壶挂在画面左侧、角色右腰附近，或由角色右手自然握住；另一只手自然下垂或用于平衡。
- 只在后续 attack 中表达投掷，不在身份图中做夸张投掷瞬间。

**v2 attack 方向（下一阶段，不在本轮生成）**：

```text
右手取壶 -> 手臂后摆 -> 肩膀和腰部蓄力 -> 单手向前上方甩出
-> 水壶脱手 -> 投掷臂前甩 -> 身体前倾收势 -> 回到 idle
```

动作语义是“像扔手榴弹一样的单手投掷”，不是抱壶前推、掷矛、投石、拉弓或法术施放。投出帧必须手中无壶，空中出现独立小水壶。

**v2 中文提示词基线**：

```text
重新设计1014投壶兵，使其成为与1012山贼兵同源的基层民兵，而不是重甲士兵、陶工或抱着大酒缸的角色。
整体气质参考1012：粗粝、实用、非正规军、山贼式民兵。角色穿旧的灰米色或暗褐色交领粗布衣，搭配暗棕皮革肩带、腰带和小布包，暗色裤子，灰白绑腿，旧布鞋或厚底鞋。装备简陋、不整齐、没有制式全身甲。角色露脸，压眉直视，嘴巴闭合，表情凶悍但克制。头部使用简易窄头巾、束发或旧布帽，不戴兜鍪，不戴面甲，不蒙面。
1014不携带刀、剑、枪、弓、箭袋、旗或盾。他的唯一战斗道具是一只普通大小的随身粗陶水壶，像民兵随身携带的饮水壶，而不是酒缸、大陶罐或大型陶器。水壶必须能被一只手握住，约为角色身高的八分之一到十分之一。水壶略扁圆腹、短颈、小壶嘴，一个小把手或提绳，素陶暖棕或灰褐色，没有花纹、釉面、金边、绿色发光或毒液。
身份姿态为正面稳定站立，水壶挂在画面左侧、角色右腰附近，或由角色右手自然握住，另一只手自然下垂或用于平衡。不要双手抱住水壶，不要展示大壶，不要做搬运酒缸的姿势。角色应能支持后续像扔手榴弹一样的单手投掷动作，但本张只做身份姿态。
保持1012与其他已验收敌人的短壮比例、高清像素化笔法、粗布与皮革材质、清晰色簇和厚重轮廓。1014人物本体高度与1012、1011属于同一家族，不能因为水壶变大而缩小人物。
```

**v2 英文提示词基线**：

```text
Redesign enemy 1014 as a low-ranking militia pottery thrower from the same rough, bandit-like background as enemy 1012, not as a heavily equipped soldier, pot maker, or character carrying a large wine jar.
Match the practical, rough and irregular militia mood of 1012: worn gray-beige or dark brown crossed-collar cloth clothing, dark brown leather shoulder strap, belt and small pouch, dark trousers, pale leg wraps, and worn cloth or heavy shoes. The equipment is light, improvised and non-uniform, with no full armor. Show an exposed face with lowered brows, a closed mouth and a restrained hostile expression. Use a simple narrow head cloth, tied hair or worn cloth cap. No helmet, face guard or face covering.
1014 carries no sword, knife, spear, bow, quiver, flag or shield. His only combat prop is one ordinary personal ceramic water bottle, similar to a rough militia canteen, not a wine jar, barrel or large pottery vessel. The bottle must be small enough to hold in one hand, approximately one eighth to one tenth of the character's body height. Give it a slightly flattened round body, short neck, small spout, and one small handle or carrying cord. Use plain warm brown or gray-brown unglazed pottery with no pattern, glaze, gold trim, green glow or visible poison.
For the identity pose, keep the character facing forward in a stable stance. Place the small bottle at the left side of the image, corresponding to the character's right waist, either hanging from the belt or naturally held in the right hand; the other hand hangs naturally or helps balance. Do not make him hug the bottle, display a large jar, or look like he is carrying a wine container. The design should support a future one-handed grenade-like throwing motion, but this image is an identity pose, not the throwing moment.
Match the compact proportions, high-definition pixelized rendering, clustered color shapes, rough cloth and leather materials, and heavy readable silhouettes of enemy 1012 and the approved enemy family. Do not shrink the body to compensate for an oversized bottle.
```

**v2 负面词增量**：

```text
large jar, wine jar, wine barrel, large amphora, oversized pottery, heavy ceramic vessel, two-handed jar hug, carrying a large container, pot maker, ceramic worker, heavy armor, uniform armor, plated shoulders, oversized bracers, helmet, face guard, face mask, sword, knife, spear, bow, quiver, flag, shield, magic bottle, green glowing poison, grenade-shaped bottle, modern grenade, bottle covering torso, bottle larger than one hand, smooth painted rendering, glossy fantasy costume, noble officer, general, elite soldier
```

**v2 组图验收**：先看 1014 是否与 1012 同源的基层民兵气质，再看比例和画风；水壶必须一手可握、不能遮胸腹、不能出现酒缸感；1014 不得比前五人更重甲、更制式或更精致；人物本体仍按 1011 的 `0.94–0.99` 相对比例检查，水壶不计入身高。组图只做比例研究，不直接裁切入 Unity。

**v2 后续顺序**：

```text
1. 重新生成六人并排组图 v2
2. 用户验收 1012 同源民兵气质、人物比例和小水壶尺寸
3. 通过后生成 1014 单角色身份图 v2
4. 通过后单独生成小水壶透明道具图
5. 最后才制作单手手榴弹式投掷 attack
```

当前状态：**v1 形象已否决；v2 组图已于 2026-11-16 生成完毕（§7.9），等用户验收。阶段 B/C 和 Unity 接线均未开始。**

---

### 7.9 v2 组图执行记录（2026-11-16）

**提交**：`Tools/visual_brief/submit_enemy_proportion_lineup_1014_v2.py --approve`（提示词与参考图清单外置在 `Tools/visual_brief/prompts/enemy_proportion_lineup_1014_v2.*` / `refs_enemy_proportion_lineup_1014_v2.json`，为唯一真源）。

**两处相对 v1 的改动**（依据 §7.7 的「若要出 v2」与 §7.8 的角色方向）：

1. **输入图换成像素风锚图**（v4 组图那套）：`Enemy1011ArtSource/.../anchor_green.png` + `Enemy1012ArtSource/.../input_identity_v12_bandit_idle_v6_1024.png` + `Enemy1013ArtSource/.../1013_lineup_crop_reference_rgba.png` + `Enemy1051ArtSource/.../1051_idle_identity_greenscreen_1024.png` + `Enemy1021ArtSource/.../1021_idle_identity_greenscreen_1024.png`（前三张与 v4 输入**逐字节一致**，后两张是 v4 那两个已随本机存档清理的文件的本地等价件）。**不再用已部署精灵帧**。
2. **1014 改成 §7.8 的方向**：与 1012 同源的基层民兵 + 单手小水壶（不是重甲抱大陶壶）。

| 项 | 值 |
|---|---|
| 接口 / 模型 | `POST /v1/images/edits` / `gpt-image-2.6-sunburst` |
| 参数 | `size=1536x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1` / 不传 `background` |
| `image[]`（5 张，2.10 MB） | 像素风锚图 / 身份锚图件；1014 无输入图 |
| 结果 | HTTP 200，用时 212.66s，1536×1024 PNG color type 6，1,804,423 bytes，sha256 `216e1f8d…0f65`，usage total 10371（image 5488 / text 2083 / input 4883） |
| 归档 | `Enemy1014ArtSource/03_Lineup/enemy_proportion_lineup_1014_v2.*`（png / prompt / request / response / status / measure / annotated）+ `00_Index/lineup-images.csv` + 包 README |

**量测（`Tools/visual_brief/measure_lineup_figures.py`，alpha≥128 + 4 邻接 BFS）**：恰好 6 人、顺序正确、脚底同基线 `y=718–719`；本体高 1011=376 / 1012=362 / 1051=351 / 1021=361 / **1014=346**（1013=507 被旗杆污染）⇒ **1014/1011 = 0.920**（目标带 0.94–0.99；本图实际家族带 0.920–0.963）。

**达标（造型方向）**：1014 = 露脸闭嘴的基层民兵（灰米交领粗布衣 + 暗棕皮革肩带/腰带 + 小布包 + 暗棕裤 + 灰白绑腿 + 旧布鞋，**无甲片、无兜鍪、无武器**），水壶在**画面左＝人物右手**、单手可握、不遮胸腹；1021 的盾仍在画面左未被镜像。

**未达标**：

1. **画风**：仍是**平滑厚涂插画** —— ★ **换像素风锚图作 `image[]` 也没解决** ⇒ 判定为该链路的**结构性表现**（整排全身人物一律被渲染成插画），不是输入选错；继续加负面词无效。
2. **背景**：透明底 + 人物周围暖色光晕/渐变（alpha=0 占 72.60%、alpha 1–127 占 5.54%），仍非单一中性纯色。
3. 1014 身高比 **0.920**（偏低但在家族带内）；水壶高度约为人物本体的 **1/5**（设定 1/8–1/10，偏大）。
4. 1013 旗面又自发长出**金色回纹边框**（跨轮复现的已知偏差）。

**口径冲突（需用户拍板）**：`design/enemy-1014-art-brief.md` §0/§7.1/§11.6 写的是「**本任务按人工制作执行，不再走生成接口**，历史生成样本全部废除」；本轮 v2 是按 §7.8 的指示发起的生成。⇒ **v2 只能当比例与设计研究图**，1014 的正式形象按需求书口径仍应人工制作（把 v2 的造型方向 + 比例数据交给制作方）。

**下一步**：用户验收 v2 造型方向 → 若通过，把「1014 本体高 0.920×1011 ⇒ 1108² 约 628–661 px（取家族带中值 ≈ 645 px）」+ 水壶尺寸口径写进需求书，交人工制作阶段 B/C。

### 7.10 形象验收 + 裁切件 + 脸谱修正项（2026-11-16 续）

**① 验收**：用户确认 **v2 的 1014 形象通过**（基层民兵 + 单手小水壶的造型方向成立）。

**② 裁切件已出**：`Enemy1014ArtSource/02_IdentityImages/1014_lineup_crop_reference_rgba.png`（214×364 RGBA；按 v2 量测 bbox `[1295,370,1493,718]` 裁出、四周 +8 px 边距）——**当前的形象基准**。⚠ 它是**插画风**（成图限制），只作造型/画风研究参考，**不是 Unity Sprite**。

**③ 脸谱对照表已出**：`Enemy1014ArtSource/02_IdentityImages/1014_face_reference_chart.png`（1011 兜鍪 / 1012 山贼 / 1013 旗手 / 1051 弓手 / 1021 楯兵 / 1014 新形象 并排）。

**④ ★ 用户反馈：当前脸读不出山贼的奸诈感**（压眉 + 闭嘴 + 方脸 ⇒ 与 1021 的「风霜老卒」撞档）。核对六个兵种的脸谱后得到的结论与候选方案已写入 **需求书 `design/enemy-1014-art-brief.md` §5.5**：

- **技术结论**：「奸诈感」的技术核心是**不对称**，不是张嘴与否 ⇒ **闭嘴单侧挑唇 + 视线略斜 + 眉内端下压** 即可拿到，不靠露齿。
- 候选：**A 闭嘴单侧挑唇（推荐）** / **B 阴鸷眯缝斜视** / **C 1012 式咧嘴**（与 1012 撞脸，不推荐）。
- 执行路线二选一：**① 人工制作**（需求书 §0 口径）**② 生成接口出概念稿**（单图编辑：底件 = 裁切件放大重贴 1024² 绿幕底件，提示词只写「改脸、其余不动」）。
- ⇒ **待用户拍板**：① 选 A/B/C 哪个脸；② 走人工还是走生成概念稿。**拍板前不提交任何付费请求。**

### 7.11 脸谱方案 C 概念稿（2026-11-16 续，已出图）

**用户拍板**：脸谱 = **方案 C（1012 山贼式的“笑着的凶”· 咧嘴露齿）**；路线 = **生成接口出概念稿**（付费，已授权）。

**底件**：`Enemy1014ArtSource/02_IdentityImages/1014_v2_base_greenscreen_1024.png`
（= v2 裁切件经 `Tools/visual_brief/compose_identity_from_alpha.py` 放大重贴：1024² 纯绿 RGB(0,159,62)、**主体高 696 / 脚底 895 / 中心 474.0**、四角 std=0）。

**提交**：`Tools/visual_brief/submit_1014_face_grin.py --approve`（提示词真源 `Tools/visual_brief/prompts/1014_face_grin_v1.*`）。
**参数**：gpt-image-2.6-sunburst / `POST /v1/images/edits`（**单张 `image[]`**）/ 1024×1024 / quality=max / input_fidelity=high / output_format=png / n=1 / 不传 background。
**结果**：HTTP 200，用时 59.13s，1024×1024 RGB，952,956 bytes，sha256 `6518306d…6505`，usage total 8260。
**归档**：`Enemy1014ArtSource/02_IdentityImages/1014_face_grin_v1.*`（png / prompt / request / response / status）+ `1014_face_before_after.png`（改脸前后对照）。

**保真度（“只改脸、其余不动”的量化）**：**剪影一致率 IoU = 0.9867**；落位不变（主体高 696、脚底 895、中心 474→475）。

**达标**：眯缝眼 + 眉外端上挑内端下压 + **咧嘴露齿** + **嘴角单侧上挑（不对称）** + 视线斜视 ⇒ 拿到 1012 那种“笑着的凶”的奸诈感。

**⚠ 附带偏差**：模型顺手加了**一圈黑色胡茬**（下颌/颧下/两颊，底件原本没有）⇒ 与 1013（络腮胡）、1021（浅灰胡茬）有混淆风险。
**待用户拍板**：① 保留；② 再发一次编辑去胡茬；③ 改成 1021 式**浅灰短茬**。

**★ 两条可复用经验**

1. **“只改一处”的单图编辑路线成立且保真度很高**：底件 = 同源裁切件按标准口径重贴成 1024² 绿幕件，提示词只写“改脸 + 其余保持原样”，**单张 `image[]`** ⇒ 身体/衣服/道具/姿势**几乎不变**（IoU 98.7%）。这比多图融合安全（多图会把角色重新拼装）。
2. **但“只改表情”会被模型顺手扩到“脸上的一切”**：它给底件凭空加了胡茬 ⇒ **提示词要把“脸部其它特征保持原样”写清楚**（本次已写：脸型/下颌/耳朵/肤色/发色/笔触保持），仍没能完全阻止 ⇒ **验收必须逐项对比底件**，不能只看表情。

需求书侧已同步：`design/enemy-1014-art-brief.md` §0（状态）、§5.2（脸行改成方案 C）、**§5.5（方案 C 已拍板）**、**§5.6（本次产出记录 + 胡茬待定）**。

### 7.12 脸谱第二轮：改脸型 + 视线回正（2026-11-16）

**用户反馈**：**脸型没有发生改变**、**角色没有正视前方**。⇒ 第二轮只改这两处（保留咧嘴笑与胡茬）。

| 项 | 值 |
|---|---|
| 底件 | `Enemy1014ArtSource/02_IdentityImages/1014_face_grin_v1.png` |
| 提交 | `Tools/visual_brief/submit_1014_face_shape_v2.py --approve`（提示词真源 `Tools/visual_brief/prompts/1014_face_shape_v2.*`） |
| 参数 | gpt-image-2.6-sunburst / edits / **单张 image[]** / 1024×1024 / quality=max / input_fidelity=high / png / n=1 |
| 结果 | ⚠ **首次提交 HTTP 503 `upstream_unavailable`（服务端上游不可用，与提示词/图片无关，非内容拦截）** ⇒ 75s 后重试一次 → **HTTP 200，用时 201.75s**，1,159,920 bytes，sha256 `44feb7f8…2c90` |
| 产出 | `02_IdentityImages/1014_face_shape_v2.png`（原始）+ `1014_face_shape_v2_greenscreen_1024.png`（**归一化后**）+ `1014_face_three_way.png`（三代对照） |

**⚠ 必须本地归一化**：接口原图把脚底画到 **884**（口径是 895）、且整个人被重渲染了一轮（剪影 IoU 从 0.9867 降到 **0.9001**）⇒ **不能直接拿接口原图当身份图**。
新工具 **`Tools/visual_brief/normalize_greenscreen_layout.py`**（吃 RGB 绿幕图，补齐 `compose_identity_from_alpha.py` 只吃 alpha 的缺口）→ 归一化结果：**主体高 696 / 脚底 895 / 中心 474.0 / 四角绿幕 (0,159,62) 偏差 0**，满足需求书 §8.1。

**达标**：脸型收窄（颊内收、颧骨成棱、下颌收成略尖下巴、额头与鼻梁收窄）⇒ 从宽厚方脸变精瘦锋利；**双眼正视前方**（瞳孔居中、鼻尖正对、下巴端平略抬、头不歪）；咧嘴奸笑与胡茬保留；身体/衣服/水壶/头巾未变。

**★ 可复用经验**

1. **503 `upstream_unavailable` 是上游抖动，不是内容拦截** ⇒ 可在 60–120s 后重试一次（与 400 的处置完全不同：400 要改措辞，503 等一等就行）。
2. **连续两轮 edits 的漂移会累积**：第一轮剪影 IoU 0.9867（几乎不动），第二轮降到 0.9001 ⇒ 连续微调时应**每轮都做落位归一化**，并把“只改脸”写成**逐项列举**（脸型/视线/保留项）。
3. **落位归一化工具补齐**：`normalize_greenscreen_layout.py`（RGB 绿幕 → 标准落位，含 de-spill 去绿边）。

需求书侧已同步：`design/enemy-1014-art-brief.md` §5.2（脸行）、**§5.7（第二轮记录）**。

---

### 7.13 组图 v3：改回组图方案，锁家族比例（2026-11-16）**【用户已验收通过】**

> ★ **2026-11-16 用户验收：v3 组图通过**（「可以，但是需要修 idle 姿态」）。
> ⇒ 形象 / 脸 / 比例**就此锁定**；后续唯一美术未决项 = **idle 姿态**，任务书见 **§7.14**。

**触发**：用户反馈「**比例不对了**，建议变回出组图方案」。
**量化确认**：单图连改两轮把身形拖歪 —— **肩线从体高 15.1% 掉到 30.5%**、头高占比 0.095 → 0.105 ⇒ 角色变得溜肩、显瘦长，不再是家族身材。

**v3 方案（三处改动）**

1. **回到六人并排组图**：同排、同基线 ⇒ 比例由家族（1011 锚图）统一锁死，不再靠单图编辑去“修比例”。
2. **输入加第 6 张** = 已验收的 1014 身份底件（`1014_face_shape_v2_greenscreen_1024.png`），锁 1014 的造型与脸（沿用 v3 组图给 1021 的做法：新角色定稿图当第 N 张输入）；并在正文里写明**图6 只提供造型与脸，身材跟图1家族走**。
3. **正文追加两条硬约束**：①「第六位与前五位是同一种身材……**不许瘦长、溜肩、窄肩**」；②「**六人都直视画面外的玩家**」。负面词同步加 `lanky 1014 / narrow shoulders on 1014 / long legs on 1014 / looking sideways / cross-eyed`。

**提交与过程**（真源：`Tools/visual_brief/prompts/enemy_proportion_lineup_1014_v3.*` / `refs_..._v3.json`，脚本 `submit_enemy_proportion_lineup_1014_v3.py`）

| 项 | 值 |
|---|---|
| 提交 | `POST /v1/images/edits` / gpt-image-2.6-sunburst / 1536×1024 / quality=max / input_fidelity=high / png / n=1 |
| 参考图 | **六张**（前五张 = v2 原锚图；第 6 张 = 1014 身份底件） |
| ⚠ 过程 | 首次提交 **503 `upstream_unavailable`** ⇒ 等 100s 重试**仍 503** ⇒ 按项目经验「重请求连续失败两次就换轻量路线」，把六张参考图**等比降到长边 640**（内容不变、顺序不变，载荷 **2.46 MB → 0.34 MB**）⇒ **一次成功**：HTTP 200，289.78s，1,897,796 bytes，sha256 `31eaaa6d…aba6`，usage total 11201 |
| 产出 | `Enemy1014ArtSource/03_Lineup/enemy_proportion_lineup_1014_v3.*`（png / prompt / request / response / status / measure / annotated）+ `02_IdentityImages/1014_v3_lineup_greenscreen_1024.png`（从组图裁出并归一化） |

**量测（`measure_lineup_figures.py`，本次给它加了**实色背景**分人支持）**：恰好 **6 人**、顺序正确、脚底同基线 **y=744–745**；本体高 1011=388 / 1012=370 / 1051=354 / 1021=355 / **1014=363**（1013=528 被旗杆污染）⇒ **1014/1011 = 0.936**（v2 是 0.912；本图实际家族带 0.912–0.954）。

**改善项**

- **背景第一次是单一平匀中性灰 (113,114,115)**（v1/v2 都是透明底 + 人物周围暖色光晕）⇒ 需求书 §7.2 的“单一中性纯色背景”这条**首次达成**。
- **1014 体型回到家族短壮**（肩线不再被拖到体高 30%），不再溜肩。
- 1014 = **瘦脸**（颊内收、颧骨成棱、下巴略尖）+ **咧嘴不对称奸笑** + **双眼正视前方** + 短黑胡茬 + 右手小水壶（画面左）。

**★ 可复用经验**

1. **503 `upstream_unavailable` 要区别于 400**：400 改措辞；503 等一等（同一天单图编辑 75s 后就成功），若重请求连挂两次就**减载荷**（参考图降分辨率，六张 2.46 MB → 0.34 MB 后一次过）。
2. **“改比例”不要靠单图编辑**：edits 会连身体一起重渲染并悄悄改掉肩线/头身比；**比例必须回到同排组图里做**。
3. **量测工具要支持实色底**：新版 `measure_lineup_figures.py` 对不透明图改用“与背景色差 > 30”分人；`normalize_greenscreen_layout.py` 新增 `--bg-rgb`（渐变底/裁切件必传，否则会抠出灰底方块）。

## 7.14 ★ 当前任务：1014 的 idle 姿态——轻抛小水壶把玩（2026-11-16）

> **本节是接手者的当前任务书。** 用户最新要求：1014 的 idle 要有“像在抛东西抛着玩”的感觉；这不是朝玩家发动攻击。先调整待机姿态方案，**本次只记录方案，尚未生成或修改 Unity**。
> 前置状态：**v3 组图已由用户验收通过**（见 §7.13）。形象 / 脸 / 比例**已锁死**，本任务**不要重画形象**。

### 7.14.1 当前状态（已定，不要再动）

| 项 | 已定内容 |
|---|---|
| 形象 | 基层民兵：灰米交领粗布衣（毛边）+ 暗棕皮革斜肩带 + 腰带与**暗红小布包** + 暗棕裤 + 灰白绑腿 + 旧布鞋；**束发髻 + 窄幅头巾（右侧垂布）**；**无甲片、无兜鍪、无武器** |
| 脸 | **瘦脸**（颊内收、颧骨成棱、下巴略尖）+ **咧嘴不对称奸笑**（嘴角一侧上挑、露齿）+ **双眼正视前方** + 下颌/两颊**短黑胡茬** |
| 道具 | **一只普通随身粗陶小水壶**（略扁圆腹、短颈、小壶嘴、单手可握，约人物本体高 1/5），**画面左 = 人物右手**；素陶暖棕/灰褐，无釉无彩绘无金边无符文；**毒是机制不是造型**（毒交给特效） |
| 比例 | v3 组图实测 **1014/1011 = 0.936**（家族带 0.912–0.954；需求书目标带 0.94–0.99） |
| 身份 | 与 1012 同源的**基层民兵**（不是制式兵、不是陶工、不是法师）；靠**头饰**与 1012 区分（1012 斗笠+红头巾 / 1014 束发+窄头巾） |

**权威需求源**：`design/enemy-1014-art-brief.md`（§0 状态 / §5.2 逐部位 / §5.5 脸谱 / §5.6、§5.7 产出记录 / §6 陶壶 / §7 组图 / §8 技术规格 / §9 红线 / §10 验收清单）。

### 7.14.2 待办：idle 像轻抛物件把玩，而不是军姿

| 方面 | 当前问题 | 本次方向 |
|---|---|---|
| **静态姿态** | 当前双脚并立、身体正、小壶低垂在右手，读作站军姿 | 保持正面、双脚站稳；右手（画面左）在身体前侧做轻松的抛接手势，小水壶位于手边或手上方，静帧就能读出“轻抛着玩”。姿态只是建议，具体手位、壶与手的距离待画面确认；不改已验收的脸、服装及比例。**★ 已出概念稿 v1（§7.14.9），待用户验收** |
| **循环表现** | 文档记录目前没有 1014 的正式 idle 帧序列 | 如后续获授权制作循环，可让小水壶轻抛、下落、被接住再回到起点；细节和帧数先作为方案，不等于动画已制作或已验收。 |
| **与攻击区分** | 正式 attack 是把水壶掷向玩家 | idle 是近身小范围把玩，不应出现朝玩家抛出的攻击轨迹、发射、伤害或毒效果。后续 attack 单独制作，动作衔接在制作时验证。 |

> **姿态设计建议，非新增玩法**：沿用已验收的小水壶和民兵形象；可以先尝试右手轻抛、同一只手接住，另一手自然放松。抛接方式不是用户已拍板的硬约束，亦可提供其它清楚读作“把玩”的姿态供用户选择。静态造型与循环动作必须区分：当前只整理姿态方向，并未要求立即生成动作。
>
> ⚠ 若要**直接改姿态**，**不要用生成接口的 edits 去改一张已验收图**（本轮实测：连续两轮 edits 把肩线从体高 15.1% 拖到 30.5%、头高占比 +10%，角色变瘦长；比例问题只能回同排组图里做，见 §7.13）。

### 7.14.3 工程侧硬约束（2026-11-16 实测，别照抄旧数）

**画布与导入**：1108×1108 PNG ｜ PPU 16 ｜ Pivot = 画布中心 **(554, 554)** ｜ Point / Uncompressed / MipMap off / maxTextureSize 2048 / **Alpha Is Transparency = 1**
（⚠ **pivot 不要默认沿用**：idle 与 attack/dead 的 pivot 常常不同。落地位置 `Assets/Sprites/Enemy/Enemy1014/`）

**家族已部署 idle 实测**（alpha≥128 bbox）：

| 敌 | 帧数 | 画布 | 本体高 px | 脚底 y | 中心 x | 帧间高差 |
|---|---|---|---|---|---|---|
| 1011 | 6 | 1108² | 598–618 | 896–897 | ~505 | 20 |
| 1012 | 6 | 1108² | 596–613 | **897** | ~521 | 17 |
| 1013 | 6 | 1440²（带旗） | 928 | 1333 | ~777 | 0 |
| 1051 | 6 | 1108² | 619 | 897 | ~533 | 0 |
| 1021 | 4 | 1108² | 549–569 | 870 | ~549 | 20 |

> ⚠ **口径差异（必须先定）**：需求书 §4.1 写的 1011 = **689 px** 是 **alpha≥1** 口径（含半透明边缘）；alpha≥128 只有 598–618。
> ⇒ **1014 的目标本体高取决于用哪个口径**：alpha≥1 口径下需求书给的是 **648–682 px**；alpha≥128 口径下 0.94–0.99×618 ≈ **581–612 px**。**开稿前必须与用户确认口径。**

**动作语义（`design/pixel-character-and-enemy-animation-spec.md` §7）**：Idle = 双脚站稳、正面朝玩家、武器自然持握；**可有轻微呼吸或头巾变化，但不能迈步、攻击或产生尘土**。

**家族 idle 的实际做法**：6 帧不是行走循环，而是短循环待机。1014 可以比 1012 多一个“轻抛—接住”的小动作，但幅度必须克制，保持民兵在原地把玩物件的感觉：不迈步、不攻击、不扬尘、不做正式投掷。参考图 `Enemy1014ArtSource/00_Reference/idle_loop_reference_1012_1051_1021.png` 仍用于呼吸、重心和帧间节拍，不要求 1014 完全静止。

### 7.14.4 可用资产（全部已入库，别再翻仓库）

| 资产 | 路径 | 用途 |
|---|---|---|
| ★ **当前形象基准** | `Enemy1014ArtSource/02_IdentityImages/1014_v3_lineup_greenscreen_1024.png` | 1024² 纯绿 (0,159,62)、主体高 **696** / 脚底 **895** / 中心 **474.0**；从 v3 组图裁出并归一化 |
| 家族组图 + 量测 | `Enemy1014ArtSource/03_Lineup/enemy_proportion_lineup_1014_v3.png` / `.measure.json` / `.annotated.png` | 比例家族带与同排同基线基准 |
| ★ **家族 idle 循环参考** | `Enemy1014ArtSource/00_Reference/idle_loop_reference_1012_1051_1021.png` | 1012(6) / 1051(6) / 1021(4) 的 idle 逐帧对照 |
| 脸谱对照 | `Enemy1014ArtSource/02_IdentityImages/1014_face_reference_chart.png` | 六兵种头部对照（脸已定，仅备查） |
| 已部署 idle 帧 | `Assets/Sprites/Enemy/Enemy1012/Enemy_1012_idle1..6.png`、`Enemy1051/...`、`Enemy1021/...` | 落位与循环节拍的第一手参考 |
| 1014 目标目录 | `Assets/Sprites/Enemy/Enemy1014/`（**当前是 106 的占位**） | 后续落地位置 |

**工具**（都在 `Tools/visual_brief/`）：`compose_identity_from_alpha.py`（带 alpha 的原图 → 标准绿幕底件）、`normalize_greenscreen_layout.py`（RGB 绿幕/实色底 → 标准落位，支持 `--bg-rgb`）、`measure_lineup_figures.py`（组图量测，已支持实色底分人）、`chroma_key.py`（视频抠图）、`pick_cycle_frames.py` / `pick_idle_frames.py`（选帧）。

### 7.14.5 交付物（本任务产出）

1. **idle 方案**（先出方案再出图）：待机姿势描述，并说明如何一眼读成“轻抛着玩”、如何区别于朝玩家投掷。若用户后续授权制作循环，再按选择的抛接方式拟定逐帧变化与正式攻击的衔接。
2. **idle 帧序列**（若获授权出图）：`Assets/Sprites/Enemy/Enemy1014/` 下 6 张 PNG，1108²，**只覆盖 PNG 字节、保留 `.meta`**（GUID 与导入设置不变）。
3. **落位量测 JSON**：每帧的本体高、脚底可见 y、中心 x（口径写清楚）。
4. **验收对照图**：6 帧并排 + 与家族 idle 的同基线对照。

### 7.14.6 已知坑（别再踩）

- **比例不要用单图 edits 修**（本轮实测会把肩线拖到体高 30%）；比例必须回同排组图里做。
- **组图不能直接裁切进游戏**（1013 裁切实测：旗杆总高 1.53×、折算后超出画布）。
- **生成接口出的是插画风**，不是工程像素画风 ⇒ 正式像素素材按需求书 §0 走**人工制作**；生成图只当概念稿。
- **接口报错要分型**：400 = 措辞/参考图（改措辞，**不要降级输入图**）；**503 `upstream_unavailable` = 上游抖动**（等 60–120s 重试；重请求连挂两次就减载荷，见 §7.13）。
- **写精灵必须保留 alpha**；`convert("RGB")` 会把透明区变黑底；PIL bbox 的右/下是 exclusive，缩放后最底行可能透明 ⇒ 落位按**可见底行**对齐。
- **动画部署只接第一关**，交用户开游戏验收，不自动进 Play Mode 自检（`memory/user-preference.md`）。
- **不要自行追加需求**；需求没写的只能列为建议让用户拍板。

### 7.14.7 待用户拍板（建议接手者先问这三条）

1. **轻抛姿态的具体手位与抛接方式**：上面的右手轻抛、同手接住只是建议，等姿态方案给用户看后确认；无需再问用户是否要“抛着玩”，该方向已明确。
2. **本体高口径**：alpha≥1（需求书的 648–682 px）还是 alpha≥128（≈581–612 px）？
3. **走人工制作还是生成接口出概念稿？**（需求书 §0 口径是人工；生成路线只出插画风概念稿）

### 7.14.8 相关文档

- 需求书：`design/enemy-1014-art-brief.md`（唯一需求源）
- 机制：`design/enemy-1014-javelin-thrower-mechanics.md`
- 像素与动画规格：`design/pixel-character-and-enemy-animation-spec.md`（§5 帧要求 / §6 位置与比例 / §7 动作语义）
- 画风：`design/art-style-guide.md`
- 工作流与铁律：`memory/art-production-workflow-entry.md`（S0–S5 阶段闸门）
- 组图方法与教训：`memory/enemy-proportion-lineup-reference.md`
- 落位经验：`memory/enemy1012-art-replacement-pipeline.md`（§3 落位口径 / §9 工具）
- 本轮全部产出记录：**本文 §7.9 / §7.10 / §7.11 / §7.12 / §7.13**

---

### 7.14.9 ★ 已产出：idle「轻抛小水壶把玩」概念稿 v1（2026-11-16）

**按 §7.14.2 的方向出了第一张 idle 姿态概念稿**（用户授权「按新文档内容生成」）。

| 项 | 值 |
|---|---|
| 参考图 | ★ **已验收的形象基准件** `02_IdentityImages/1014_v3_lineup_greenscreen_1024.png`（**单张 `image[]`**，不是 edits 改图） |
| 提交 | `Tools/visual_brief/submit_1014_idle_toss.py --approve`（提示词真源 `Tools/visual_brief/prompts/1014_idle_toss_v1.*`） |
| 参数 | gpt-image-2.6-sunburst / `POST /v1/images/edits` / 1024×1024 / quality=max / input_fidelity=high / output_format=png / n=1 / 不传 background |
| 结果 | **HTTP 200，一次成功**，用时 163.88s，1024×1024 RGBA，1,041,981 bytes，sha256 `dcda9ca2…af81`，usage total 见 request 留档 |
| 产出 | `02_IdentityImages/1014_idle_toss_v1.png`（原始）+ **`1014_idle_toss_v1_greenscreen_1024.png`（归一化落位）** + **`1014_idle_pose_compare.png`（军姿 vs 新 idle 对照）** |

**姿态方案（已落图）**

- 正面朝观众、**双脚站在地上**，重心主要压左腿、右膝放松；肩膀放松但仍正对观众，胸口打开、背脊直。
- **左臂自然下垂**，手掌放松张开。
- **右臂（画面左侧）屈肘抬到胸前，掌心朝上摊开，手指放松微屈**；**小陶壶悬在摊开的掌心上方一点**（刚抛起、还没落回，壶身略倾）。
- 头微歪看壶，**眼睛仍直视画面外观众**，保持那个歪斜的露齿笑。
- **与 attack 的区分**：手臂**没有后拉**、肩膀**没有蓄力**、身体**没有前倾**，壶不朝观众方向 ⇒ 静帧一眼读成"随手抛弄小物件"，不读成投掷；画面里**没有**弹道、动作线、扬尘、特效、发光、毒雾。

**比例校验（对比基准件，alpha≥128 / 绿幕键，同一量法）**

| 指标 | 基准 v3（军姿） | idle 轻抛 v1 | 结论 |
|---|---|---|---|
| 本体高 px | 696 | 687 | −1.3%，可忽略 |
| 高宽比 | 1.794 | 1.832 | +2.1%，可忽略 |
| **肩线占比**（体高百分比） | 0.178 | **0.164** | 肩线**更高**，家族短壮身材保住了 |
| 头高占比 | 0.112 | 0.131 | +17% 相对值，偏大一点；生成图的粗略量法，仅提示留意 |
| 脚底 y / 中心 x | 895 / 473.5 | 881 / 476.0 | 归一化后回到口径 |

⇒ **对比 §7.13 的教训**：坏掉的 edits 是肩线 0.151 → 0.297（翻倍）；本次肩线 0.178 → 0.164（方向相反、幅度很小）⇒ **"用基准件做参考 + 明确写死身材" 这条路没有把比例改坏**。

**归一化落位**（`Tools/visual_brief/compose_identity_from_alpha.py`）：**主体高 695 / 脚底 y 894 / 中心 x 474.0 / 四角绿幕 (0,159,62) std=0** ⇒ 满足需求书 §8.1 身份图口径（与基准件同一套落位，便于做 idle 帧时的对位）。
**质心判定**：躯干带质心 484.7 < 头带质心 509.1 ⇒ 质量偏**画面左** ⇒ **壶与抬起的右手都在人物右手侧** ✔ 符合"画面左 = 人物右手"。

**仍待用户拍板 / 下一手**

1. **这张 idle 姿态是否通过**（手位、壶的高度、头的歪度都可调）。
2. **6 帧循环怎么做**（交付物 2）：按这张静帧的构图，建议 **f1 壶在掌心上方 → f2 更高 → f3 最高 → f4 回落 → f5 接近掌心 → f6 接住（壶在掌心）**，幅度控制在**壶上下 ≤ 1/4 身高**、身体只做轻微呼吸与重心微移（家族节拍参考 `00_Reference/idle_loop_reference_1012_1051_1021.png`）；**不迈步、不扬尘、不做正式投掷**。
3. **本体高口径**（§7.14.7 第 2 条仍未定）：alpha≥1（648–682 px）还是 alpha≥128（≈581–612 px）—— 做帧前必须定，否则 6 帧之间会缩放不一。
4. **是否走人工制作**（需求书 §0 口径）—— 本图是**插画风概念稿**，正式像素帧仍需人工制作。
