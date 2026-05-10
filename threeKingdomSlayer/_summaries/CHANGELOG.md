# 开发日志

## 2025-12-18 — 攻击技能可配置化重构（AttackSkillConfig）

### 概述
将攻击参数从 HeroConfig 平铺字段和 AttackSystem 硬编码字段中解耦，拆分为独立的 `AttackSkillConfig` ScriptableObject 资产。大招也纳入该配置系统。策划可在 Inspector 中拖拽不同技能资产来装配武将的攻击组合。

### 新增文件
- `Assets/Scripts/Core/AttackSkillConfig.cs` — ScriptableObject，含 attackType、damageType、damage、poiseDamage、rangeRows、cooldown、launchDuration、attackWavePrefab、isUltimate、ultimateEnergyCost、ultimateEnergyGain
- `Assets/ScriptableObjects/Warrior/Skills/Zhangfei_Stab.asset` — 戳击（damage=100, range=1, cooldown=1s）
- `Assets/ScriptableObjects/Warrior/Skills/Zhangfei_Slash.asset` — 斩击（damage=10, range=1, cooldown=1s）
- `Assets/ScriptableObjects/Warrior/Skills/Zhangfei_Pierce.asset` — 穿刺（damage=200, range=5, cooldown=5s）
- `Assets/ScriptableObjects/Warrior/Skills/Zhangfei_Sweep.asset` — 横扫（damage=100, range=2, cooldown=5s）
- `Assets/ScriptableObjects/Warrior/Skills/Zhangfei_Launch.asset` — 挑飞（damage=10, range=2, cooldown=5s, poise=50, duration=2s）
- `Assets/ScriptableObjects/Warrior/Skills/Zhangfei_Parry.asset` — 招架（damage=15, poise=50, range=1, cooldown=0.5s）
- `Assets/ScriptableObjects/Warrior/Skills/Zhangfei_Ultimate.asset` — 大招（damage=100, range=5, cooldown=10s, isUltimate=true, energyCost=100）

### 修改文件
- `Assets/Scripts/Core/HeroConfig.cs` — 删除所有平铺攻击字段（stabDamage~parryCooldown），替换为 `List<AttackSkillConfig> skillConfigs` + `GetSkillConfig(AttackType)` 查询方法
- `Assets/Scripts/Player/AttackSystem.cs` — 全面重构：删除 5 个 wavePrefab 字段 + 7 个 parry 参数字段；6 个 Execute* 方法全部改为 `heroConfig.GetSkillConfig(attackType)` 读取 damage/damageType/rangeRows/poiseDamage/launchDuration/attackWavePrefab
- `Assets/Scripts/Player/PlayerState.cs` — `AttackType` 枚举新增 `Ultimate`；6 个独立冷却计时器替换为 `Dictionary<AttackType, float> cooldownTimers`；`GetCooldownDuration()` 改为从 `heroConfig.GetSkillConfig()` 读取
- `Assets/Scripts/Core/UltimateSystem.cs` — 删除 `energyGainPerHit[]` 数组；`AddEnergyForAttack()` 改为 `heroConfig.GetSkillConfig(attackType).ultimateEnergyGain`
- `Assets/ScriptableObjects/Warrior/Hero_Zhangfei.asset` — 旧平铺字段清除，`skillConfigs` 列表含 7 个技能资产 GUID 引用

### 架构要点
- **输入与配置解耦**：InputManager 仍产生 AttackType + 手势参数，AttackSystem 根据 AttackType 查配置再执行
- **策划友好**：每个技能一个 .asset，Inspector 拖拽装配；不同武将可复用/替换技能配置
- **大招纳入统一体系**：`AttackType.Ultimate` 作为一个技能类型，有独立的 AttackSkillConfig，但执行路径仍走 UltimateSystem（UI 按钮直达）

---

## 2025-12-18 — 大招系统（Ultimate System）

### 概述
实现大招充能系统：攻击命中充能 → UI 按钮垂直填充 → 充满后可点击触发全敌伤害。

### 新增文件
- `Assets/Scripts/Core/UltimateSystem.cs` — 单例，充能管理（maxUltimateEnergy=100），按 AttackType 索引的 energyGainPerHit 数组，事件 OnEnergyChanged/OnUltimateReady/OnUltimateActivated
- `Assets/Scripts/Core/UltimateEffect.cs` — 抽象基类，Execute() + GetLifetime()
- `Assets/Scripts/Core/UltimateEffect_AllEnemyDamage.cs` — 示例效果：遍历 EnemyManager.GetAllAliveEnemies() 造成伤害
- `Assets/Scripts/UI/UltimateButtonUI.cs` — 按钮 UI：CanvasGroup.alpha 透明度、fillImage.fillAmount 垂直填充、TMP 数值显示、交互控制

### 集成点
- `AttackSystem.cs`：每次命中后调用 `UltimateSystem.Instance.AddEnergyForAttack(attackType)`
- `StageController.cs`：`StartStage()` 中调用 `UltimateSystem.Instance.ResetEnergy()`

### 场景配置
- Battle.scene：根级 UltimateSystem GameObject + BattleHUD/UltimateButton（Button + Fill Image + EnergyText）

### 涉及文件
- `Assets/Scripts/Core/UltimateSystem.cs` — 新增
- `Assets/Scripts/Core/UltimateEffect.cs` — 新增
- `Assets/Scripts/Core/UltimateEffect_AllEnemyDamage.cs` — 新增
- `Assets/Scripts/UI/UltimateButtonUI.cs` — 新增
- `Assets/Scripts/Player/AttackSystem.cs` — 集成 UltimateSystem.AddEnergyForAttack
- `Assets/Scripts/Managers/StageController.cs` — 集成 UltimateSystem.ResetEnergy
- `Assets/Scenes/Battle.scene` — UltimateSystem + UltimateButton UI 对象

---

## 2025-12-17 — 敌人血条显示修复（颜色 + Z 遮挡）

### 概述
修复非第一排敌人血条显示异常：纯白无颜色、被 Canvas 背景遮挡。

### Bug 1: 血条 Fill 颜色丢失（纯白）

**根因**：`EnemyHealthBar.Show()` 中通过 `fillRenderer.material` 获取材质实例并缓存为 `fillMaterialInstance`。当 `barRoot` 反复 `SetActive(false/true)` 后，Unity 内部可能重新创建 Renderer 的材质实例。旧代码通过 `fillMaterialInstance = fillRenderer.material` "认领"新实例，但未显式写回 Renderer，导致 `fillMaterialInstance.color` 设置到一个 Renderer 不再使用的材质上。显示为 Unlit/Color 默认白色。

**修复**：
- `EnsureCreated()`：改为显式 `new Material(_barMaterial)` + `fillRenderer.material = fillMaterialInstance`
- `Show()`：检测到 Renderer 材质与缓存不一致时，主动将缓存的实例**写回** `fillRenderer.material = fillMaterialInstance`，而非"认领" Renderer 的新实例

### Bug 2: 后排敌人血条被 Canvas 遮挡

**根因**：Canvas 为 Screen Space - Camera 模式，`planeDistance=10`。摄像机在世界 z=-10。Canvas 平面在 z≈-0.34。敌人 Z 范围：前排 z=-10 到后排 z=0。后排敌人血条（含头部偏移）在 Canvas 平面后方，Canvas 的 ZWrite 遮挡了后排血条。

**修复**：Canvas `planeDistance` 从 10 改为 15，Canvas 平面移到 z≈4.5（所有敌人后方）。

### 涉及文件
- `Assets/Scripts/UI/EnemyHealthBar.cs` — 材质实例显式创建与管理
- `Assets/Scenes/Battle.scene` — Canvas planeDistance: 10 → 15

---

## 2025-12-16 — 补齐链断裂修复 & 缩放残留修复

### 概述
修复 Launch（挑飞）攻击杀死敌人后补齐链中断，以及多敌人快速受击时缩放永久变形两个 Bug。

### Bug 1: 挑飞击杀后补齐链中断

**根因**：`AttackWave` 以 0.04s/排 stagger 命中敌人。前排敌人先死 → `RemoveEnemy()` 为后方敌人设 `targetRow`，但后方敌人尚未被命中。0.04s 后命中 → `Launch()` 无条件重置 `targetRow = -1`。落地时 `UpdateLaunch()` 查不到 targetRow，走自然移动而非补齐。

**修复**：`Launch()` 不再重置 `targetRow`。`RemoveEnemy()` 设置的值保留到落地。

### Bug 2: 多敌人快速受击时缩放永久变形

**根因**：`DOTween.Kill("punchScale")` 是全局 Kill，会误杀其他敌人正在运行的 punch tween。敌人 A 的 punch 被敌人 B 的受击 Kill 打断，scale 停在中间振动值无法恢复。`Launch()` 中同样存在全局 `DOTween.Kill("punchScale")` / `DOTween.Kill("rushBounce")` 误杀问题。

**修复**：
- `TakeDamage`：punch tween ID 改为 per-instance `$"punch_{GetInstanceID()}"`
- `Launch`：移除冗余的全局 `DOTween.Kill("punchScale")` / `DOTween.Kill("rushBounce")`（`transform.DOKill(false)` 已覆盖本对象）

### 涉及文件
- `Assets/Scripts/Enemy/Enemy.cs` — `Launch()` 不重置 targetRow；`TakeDamage()` per-instance punch ID；`Launch()` 移除全局 DOTween.Kill

### 概述
完善 Parry 攻击的触发、打断、架势判定、眩晕规则，重构敌人攻击与架势系统。

### 改动明细

**招架触发**
- `InputManager.ProcessGesture`：无充能快速滑动，方向与垂直轴夹角 < `verticalSwipeThreshold` 时映射为 Parry
- `HeroConfig` 新增 `parryRangeRows`（默认 1）、`parryCooldown`（默认 0.5s）
- `PlayerState.GetCooldownDuration(AttackType.Parry)` 读取 `heroConfig.parryCooldown`

**招架命中逻辑** (`AttackSystem.ExecuteParry`)
- 遍历 `parryRangeRows` 排内所有存活敌人，造成 `parryDamage`（DamageType.Stab）和 `parryPoiseDamage`
- 打断判定：`parryPoiseDamage >= enemy.config.maxPoise` 且敌人处于 AttackSpawn 阶段 → `CancelAttack()` 返回攻击冷却
- 不满足打断条件：仅造成伤害+架势伤害，不打断不眩晕
- 架势破碎不再眩晕：`TakePoiseDamage` 仅重置 `currentPoise`
- Boss 眩晕：`CheckParryStunThresholds` 仅在 `isBoss=true` 时按血量百分比阈值触发 `Stun()`
- 减伤代码已注释，留作日后角色专属技能

**敌人攻击三阶段** (`Enemy.cs`)
- AttackSpawn（前冲+翻转，`isAttackAnimating && !isAttackDrawPhase`）→ 可被招架打断
- AttackDraw（收招返回，`isAttackDrawPhase=true`）→ 不可打断
- 冷却 → 可被链式补齐中断
- `isAttackAnimating` / `isAttackDrawPhase` 改为 public，供 AttackSystem 读取

**受伤抖动隔离**
- `TakeDamage` 中 punch scale tween 使用 `DOTween.Kill("punchScale")` + `.SetId("punchScale")`
- 不再使用 `transform.DOKill(true)` 避免误杀攻击动画 tween

### 涉及文件
- `Assets/Scripts/Core/HeroConfig.cs` — 新增 `parryRangeRows`、`parryCooldown`
- `Assets/Scripts/Enemy/Enemy.cs` — 攻击三阶段、CancelAttack、TakePoiseDamage、CheckParryStunThresholds、punchScale 隔离
- `Assets/Scripts/Player/AttackSystem.cs` — ExecuteParry 重写
- `Assets/Scripts/Player/PlayerState.cs` — Parry 冷却读 HeroConfig
- `Assets/Scripts/Player/InputManager.cs` — Parry 手势分支（已存在）
- `_summaries/Enemy.summary.md` — 更新接口与规则
- `_summaries/Player.summary.md` — 更新接口与规则
- `_summaries/Core.summary.md` — 更新 HeroConfig 字段
