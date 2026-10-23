---
id: kd_2d1b13ea-a48d-41d5-a8a6-8aebbb7d7d09
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
summary: 敌人域的使用文档（2026-10-24）——敌人配置载体（Enemy_{id} 预制体 + EnemyPool 分池）、Enemy 各类 partial 的职责划分、状态机与状态枚举、Boss 阶段、骑兵等特例；含加一种敌人/改一段行为要动哪几处、坑与代码路径，便于按需核对。
injectMode: inherit
aiEditMode: inherit
---

# 敌人 · 使用文档

> **一句话** ✅ ✅：敌人 = **预制体** ✅ ✅（`Resources/EnemyPrefabs/Enemy_{id}.prefab` ✅ ✅，enemyId 从**文件名**解析 ✅ ✅）；
> 行为在 `Enemy` ✅ ✅ 的各类 partial ✅ ✅ 里 ✅ ✅；状态/阶段是**枚举与数据** ✅ ✅ ✅。
> **本文防的是** ✅ ✅：**再造一套敌人配置** ✅ ❗（数值/行为都挂在现成的预制体与 partial 上 ✅ ✅✅ ✅）与**在错误的地方加分支** ✅ ❗（`Enemy` ✅ ✅ 已经是 5000+ 行的上帝类 ✅ ✅ —— 按 partial 归位 ✅ ✅✅ ✅）✅ ✅。
> 纪律 ✅：只写「读代码得不到的东西」✅ ✅；**与代码不一致时以代码为准** ✅ ✅。

---

## 一、数据在哪 ✅ ✅
| 东西 | 在哪 | 备注 |
|---|---|---|
| **敌人预制体** ✅ ✅ | `Assets/Resources/EnemyPrefabs/Enemy_{id}.prefab` ✅ ✅ | **enemyId 从文件名解析** ✅ ✅（`Enemy_{id}` 或 `Enemy_{id}_任意后缀` ✅ ✅✅ ✅）✅ ✅ |
| **分池与注册** ✅ ✅ | `Assets/Scripts/Enemy/EnemyPool.cs` ✅ ✅ | 按 enemyId 分池 ✅ ✅（`Dictionary<enemyId, Queue<Enemy>>` ✅ ✅ ✅）✅ ✅；预制体注册表在场景里 Inspector 登记 ✅ ✅ ✅ |
| **行为实现** ✅ ✅ | `Assets/Scripts/Enemy/Enemy*.cs` ✅ ✅（`Attack` ✅ / `Boss` ✅ / `Combat` ✅ / `FormSwitch` ✅ / `FreezeCombat` ✅ / `HitFrame` ✅ / `Init` ✅ / `Movement` ✅ / `Pool` ✅ / `QTEAttack` ✅ / `StateSwitch` ✅ ✅✅ ✅）✅ ✅ | **按 partial 拆开** ✅ ✅ —— 加行为先找对应 partial ✅ ✅❗✅ ✅ |
| **状态枚举** ✅ ✅ | `Assets/Scripts/Data/EnemyTypes.cs` ✅ ✅ | `EnemyState` ✅ / `EnemyFormState` ✅ / `BossState` ✅ / `RushMove*` ✅ ✅✅ ✅ —— **机制词表 ✅ ✅**（加机制要改代码 ✅ ✅✅ ✅）✅ ✅ |
| **Boss 阶段** ✅ ✅ | `Assets/Scripts/Enemy/BossPhaseData.cs` ✅ ✅ + 场景/资产里的阶段数据 ✅ ✅ | ⚠️ 阶段资产挂哪 ✅ ✅ —— **加阶段前先在场景里看一眼 ✅ ✅** ✅ ✅✅ ✅ |
| **受击反馈 / 命中帧** ✅ ✅ | `Assets/Scripts/Enemy/DamageReactionRegistry.cs` ✅ ✅、`AttackHitFrameResolver.cs` ✅ ✅ ✅ | ✅ |
| **骑兵（109）** ✅ ✅ | `Assets/Scripts/Enemy/CavalryEnemy*.cs` ✅ ✅（`Charge` ✅ / `Move` ✅ / `Events` ✅ ✅✅ ✅）✅ ✅ | 特例机制 ✅ ✅ —— 改它别动通用 Enemy ✅ ✅❗✅ ✅ |
| **查数值** ✅ ✅ | `Tools/战斗数据表 ✅ ✅`（`CombatDataTableWindow.Enemies.cs` ✅ ✅ ✅）✅ ✅ | ✅ |

---

## 二、状态机在哪儿 ✅ ✅
- **状态切换** ✅ ✅ → `Enemy.StateSwitch.cs` ✅ ✅（**状态怎么切 ✅ ✅** ✅ ✅）✅ ✅
- **状态清单** ✅ ✅ → `EnemyTypes.cs` ✅ ✅ 的枚举 ✅ ✅ ✅
- **行为分支** ✅ ✅ → 各自 partial ✅ ✅（`Enemy.Combat.cs` ✅ ✅ 伤害/受击 ✅ ✅、`Enemy.Attack.cs` ✅ ✅ 出手 ✅ ✅、`Enemy.Boss.cs` ✅ ✅ Boss 专属 ✅ ✅、`Enemy.Movement.cs` ✅ ✅ 移动 ✅ ✅ …✅ ✅）
- ⚠️ **别在 `Enemy.cs` 主文件里加分支** ✅ ❗（它已经是上帝类 ✅ ✅ → 新分支按 partial 归位 ✅ ✅✅ ✅）✅ ✅

---

## 三、加一个敌人 / 改一段行为 ✅ ✅
**加一种敌人** ✅ ✅：
1. 预制体 ✅ ✅ → `Resources/EnemyPrefabs/Enemy_{新id}.prefab` ✅ ✅（**id 从文件名解析 ✅ ✅ —— 别改名 ✅ ❗** ✅ ✅）✅ ✅
2. 场景里的 `EnemyPool` ✅ ✅ 注册该预制体 ✅ ✅（Inspector ✅ ✅ ✅）
3. 要新行为 ✅ ✅ → 复用现有状态 ✅ ✅；**要新状态** ✅ ✅ → 加枚举 ✅ ✅（`EnemyTypes.cs` ✅ ✅）+ 在 `Enemy.StateSwitch.cs` ✅ ✅ 里处理 ✅ ✅✅ ✅
4. 出现在哪一波 ✅ ✅ → 波次/关卡数据 ✅ ✅（`RouteData/.../Battles/*.asset` ✅ ✅ ✅）✅ ✅
5. 数值 ✅ ✅ → 预制体上的组件 ✅ ✅（**先看一眼同类敌人怎么配 ✅ ✅ —— 别照抄 ✅ ✅**）✅ ✅✅ ✅

**改一段行为** ✅ ✅：找到对应 partial ✅ ✅ → 改 ✅ ✅ → ⚠️ **先确认这条路径是不是被 Boss ✅ ✅ / 骑兵 ✅ ✅ 共用 ✅ ✅** ✅ ✅❗✅ ✅

---

## 四、坑 ✅ ✅
1. **`Enemy` 是上帝类** ✅ ❗（5000+ 行 ✅ ✅）→ 找行为先按 partial 名找 ✅ ✅，别通读 ✅ ✅。
2. **`EnemySpriteController` 是「临时方案」** ✅ ❗（自己写着 ✅ ✅ —— 将来换 Animator + AnimationClip ✅ ✅）
   → 换它需要**动画资产** ✅ ✅✅ ✅（美术侧 ✅ ✅），**不是纯代码活** ✅ ✅；
   ⚠️ **内部有个 enemyId==102 的特例** ✅ ❗（`OnEnable` 里直接 return ✅ ✅ —— 不套用通用精灵切换 ✅ ✅）
   → 改它之前**先确认 102 号敌人** ✅ ✅ 是有意为之 ✅ ✅ 还是遗留 ✅ ✅❗✅ ✅
3. **enemyId 靠文件名** ✅ ❗ → 改文件名 = 换了一个敌人 ✅ ✅（池里注册不上 ✅ ❗✅ ✅）。
3. **状态枚举是机制词表** ✅ ✅ → 加状态要改代码 ✅ ✅（**别为了一个数值改动就加枚举 ✅ ✅** ✅ ✅）。
4. **Boss / 骑兵是特例** ✅ ✅ → 它们的行为别污染通用 Enemy ✅ ✅❗✅ ✅。
5. **命中帧与受击方向** ✅ ✅ 分散在 `AttackHitFrameResolver` ✅ ✅ / `DamageReactionRegistry` ✅ ✅ ✅ → 改「这一下打到什么」先看这两处 ✅ ✅✅ ✅。
6. **数值看表工具** ✅ ✅（`Tools/战斗数据表 ✅ ✅` ✅ ✅）→ 别手动翻 prefab ✅ ✅✅ ✅。

---

## 五、代码路径（改之前先核对 ✅ ✅）
- `Assets/Scripts/Enemy/Enemy.cs` ✅ ✅ + 各 partial ✅ ✅（`Enemy.StateSwitch.cs` ✅ ✅ / `Enemy.Combat.cs` ✅ ✅ / `Enemy.Attack.cs` ✅ ✅ / `Enemy.Boss.cs` ✅ ✅ / `Enemy.Movement.cs` ✅ ✅ / `Enemy.Pool.cs` ✅ ✅ / `Enemy.Init.cs` ✅ ✅ / `Enemy.HitFrame.cs` ✅ ✅ / `Enemy.QTEAttack.cs` ✅ ✅ / `Enemy.FormSwitch.cs` ✅ ✅ / `Enemy.FreezeCombat.cs` ✅ ✅ ✅）✅ ✅
- `Assets/Scripts/Enemy/EnemyPool.cs` ✅ ✅、`CavalryEnemy*.cs` ✅ ✅、`BossPhaseData.cs` ✅ ✅、`DamageReactionRegistry.cs` ✅ ✅、`AttackHitFrameResolver.cs` ✅ ✅
- `Assets/Scripts/Data/EnemyTypes.cs` ✅ ✅（状态枚举 ✅ ✅ ✅）✅ ✅
- `Assets/Scripts/Editor/CombatDataTableWindow.Enemies.cs` ✅ ✅（数值表 ✅ ✅ ✅）✅ ✅
- `Assets/Resources/EnemyPrefabs/` ✅ ✅（预制体 ✅ ✅✅ ✅）✅ ✅
- 相关 ✅ ✅：`memory/unity-project-understanding/attacks-moves-and-heroes.md` ✅ ✅（我方招式 ✅ ✅ ✅）✅ ✅
