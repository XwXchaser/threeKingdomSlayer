---
id: kd_9d5726f8-90ca-4b8e-805d-ab0b64e3a118
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 会话交接（2026-11-14）：破防演出 + 投壶兵 + 两个技能 + 美术规格

> **给下一个对话的入口。** 先读本文，再按需展开引用文档。
> 本轮主线：① 破防演出包（REQ-006）② 新兵种「投壶兵」+ 两个技能 ③ 两个武器模型的四视图需求与 3D 制作规范。
> 架构体检见 `plan/architecture-review-new-features-20261114.md`（**下一轮先做其中的 ①⑤⑥**）。

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
| 5 | ★ **主动技能放不出来** | `ActiveSkillInventory.UsesActiveSkills` 当前为 **false**（V1 道具规则）⇒ 机器人拿得到技能但 `TryActivate` 被门禁拦下。**需用户确认规则版本** |
| 6 | 数值校准 | 抡斧 90/130/180（rows 2/3/4，CD 8/7/6）；锁链 25/40/60（CD 10/9/8）；陶壶毒 2.5×4s |
| 7 | 正式图标 | 两个新技能现在用海浪图标占位 |
| 8 | 投壶兵的正式外观 | 现在是 106 的外观占位（美术按铁律 11：idle → 关键动作 → 剩余动作） |

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
| 总体状态与本周计划 | 本文；`plan/week-20261114-new-enemy-two-skills-parry-counter.md`（§2 兵种候选 / §3 防御衔接方案 / §4 已实施与机器人自测） |
| 架构欠账 | `plan/architecture-review-new-features-20261114.md` |
| 破防演出细节 | `plan/guard-break-handoff-20261112.md` §10~§12；`design/guard-break-presentation-overhaul.md` |
| 无双/影之刃的破防口径 | `design/guard-break-dynasty-warriors-reference.md` |
| 武器 3D 制作规范 | `design/weapon-3d-art-spec-axe-and-chain.md` |
| 美术工作流与铁律 | `memory/art-production-workflow-entry.md`（S0~S5 阶段闸门） |
| 既有系统地图 | `design/game-mechanics.md`；`design/gameplay-pillars.md` |

**建议下一轮的第一件事**：先做架构 ①（打落规则挪到攻击侧，纯重构、可机器人回归），
再做 §2 待办 1（防御→攻击衔接，方案已备好，只差 3 个拍板）。
