---
id: kd_04b72df3-abb3-4ca2-941d-83941c56fa62
type: memory
path: phase3-development-summary.md
title: phase3-development-summary
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
aiMaintained: true
explicitMaintenanceRules: true
createdAt: 1779520344306
updatedAt: 1780242202994
---

# phase3-development-summary

## Summary
第三期局内成长系统（经验三选一）开发状态 + 击杀进度条 & 击杀里程碑闪现 + 位移效果三选一系统（击退波/聚拢波/回旋波/连锁弹射）（2025-08-09）

<!-- locus:maintain-rules:start -->
Keep only durable and reusable project memory
Consolidate duplicates or conflicts into the latest conclusion
Remove temporary context, one-off tasks, and unsupported guesses
<!-- locus:maintain-rules:end -->

<!-- locus:body:start -->
### ✅ 左侧 Buff 图标面板 BuffDisplayPanel（2025-08-06）

**设计原则**：
- 槽位制：Inspector 中 `_columnASlots` / `_columnBSlots` 列表预置 BuffIcon 实例，代码按序分配
- 手动布局：与 UpgradePopup 设计一致，无 VerticalLayoutGroup，用户手动摆放图标位置
- 代码不创建/销毁实例，只 SetActive 切换显隐

**两根柱子**：

| 列 | 存放内容 | 生命周期 | 显示逻辑 |
|----|---------|---------|---------|
| ColumnA | 数值型(Numeric) + 被动型(Passive) | 持久，永不删除 | 再次获得仅更新角标（Lv.N / 阈值数） |
| ColumnB | 道具型(Item) | 可消耗，消耗后槽位前移补位 | 点击图标触发效果 → TryConsume → 图标消失+CompactColumnB |

**双列联动**：同一 UpgradeDefinition 可能同时出现在 ColumnA（被动注册）和 ColumnB（道具库存），由 `PassiveTriggerModule.OnPassiveRegistered` 和 `ItemInventory.OnItemChanged` 分别驱动。

**关键脚本**：

**BuffDisplayPanel.cs** (~212行)
- `_columnASlots` / `_columnBSlots` — `List<BuffIcon>`，Inspector 中增删
- `_upgradeIcons` — upgradeId → BuffIcon 映射（ColumnA）
- `_itemIcons` — gestureId → BuffIcon 映射（ColumnB）
- 监听三个事件：`UpgradeEffectManager.OnUpgradeApplied`、`ItemInventory.OnItemChanged`、`PassiveTriggerModule.OnPassiveRegistered`
- 首次收到升级时淡入面板（CanvasGroup alpha 0→1）
- CompactColumnB：被消耗图标之后的槽位依次前移补位

**BuffIcon.cs** (~69行)
- `_iconImage` / `_badgeText` / `_button` — 80×80 Image + Badge TMP + Button
- `Setup()` — 设置图标、角标、按钮状态（Item 型可点击，其他型不可交互）
- `ResetSlot()` — 清空所有数据并 SetActive(false)
- `OnClicked` — 道具点击回调（仅 Item 型绑定）

**道具点击分发流程**（BuffDisplayPanel.OnItemIconClicked）：
```
点击 BuffIcon → TryConsume(gestureId)
  → 成功 → 按 gestureId 分发：
    "circle"         → WhirlwindController.Activate(def)    // 大旋风自动运转
    "long_press_swipe_down" → InputManager.ExecuteLightning(def)  // 落雷
    "damage_boost"   → UpgradeEffectManager.AddDamageBonus(def.floatValue)  // 伤害加成
  → TryConsume 触发 OnItemChanged → BuffDisplayPanel 移除图标 + CompactColumnB
```

**ItemInventory 挂载修复**：
- ItemInventory 组件未在场景任何 GameObject 上，导致道具存储/消耗链路完全断裂
- 已添加到 Manager GameObject（与 UpgradeEffectManager 同级）

**WhirlwindController 重构**：
- 从画圈手势驱动改为点击 BuffIcon 激活 → 自动运转
- 移除：画圈检测（~80行）、手指追踪角度累积、TickActive
- 新增：`autoDuration`（默认5s）、`autoSpinSpeed`（默认180°/s）
- Update() 自动倒计时 → 持续伤害 → 定时击飞

**InputManager 简化**：
- 移除：画圈检测代码（~35行，Update 中的每帧检测+MouseUp/TouchEnd 中的 Deactivate 分支）
- 移除：`TryConsumeItemGesture`（长按下滑检测，~35行）
- `ExecuteLightning` 改为 public（供 BuffDisplayPanel 直接调用）

**BuffIcon 等级底框（2025-08-07）**：
- `BuffIcon.prefab` 重构：根节点 Image 移除，新增 Frame/Icon 两个子 Image
  - 渲染顺序：Frame（底框）→ Icon（技能图标）→ Badge（角标）
- `BuffIcon.SetFrame(Sprite)` — 设置底框精灵
- `BuffDisplayPanel._levelFrames[5]` — Lv.1~5 对应 31_ReFrame_1~5
- `BuffDisplayPanel._skillFrame` — 道具型统一使用 31_ReFrame_skill
- `GetLevelFrame(int level)` — level 超出 5 时 clamp 到 Lv.5
- 素材路径：`Assets/Sprites/31Reward/31_ReFrame_1~5.png`、`31_ReFrame_skill.png`

**场景结构**：
```
BattleHUD(Canvas)/BuffDisplayPanel [CanvasGroup, BuffDisplayPanel]
├─ ColumnA (6 BuffIcon slots: A_0~A_5, 默认disabled)
└─ ColumnB (4 BuffIcon slots: B_0~B_4, 默认disabled)
```

**新增文件**：
- `Assets/Prefabs/UI/BuffIcon.prefab` — 80×80 Image+Button+Badge
- `Assets/Scripts/UI/BuffDisplayPanel.cs`
- `Assets/Scripts/UI/BuffIcon.cs`
- `Assets/ScriptableObjects/Upgrades/Definitions/TestDamageBoost.asset` — 测试道具（gestureId=dmg_boost, floatValue=1.0）

---

### ✅ 敌人补齐行走动画 Walk（2025-08-08）

**需求**：敌人在向前补齐移动（rush）时播放两帧行走动画（walk1/walk2 交替），模拟跑步动作。不包含空中被击飞的自动移动补齐。

**三敌人覆盖**：

| 敌人 | Walk clip | 帧结构 | 素材 |
|------|----------|--------|------|
| Enemy_101 | `Enemy_101_Walk.anim` | walk1@0s, walk2@0.3s, 0.6s loop | `Enemy_1_walk1.png`, `Enemy_1_walk2.png` |
| Enemy_103 | `Enemy_103_Walk.anim` | walk1@0s, walk2@0.3s, 0.6s loop | `Enemy_3_walk1.png`, `Enemy_3_walk2.png` |
| Enemy_105 | `Enemy_105_Walk.anim` | walk1@0s, walk2@0.3s, 0.6s loop | `Enemy_5_walk1.png`, `Enemy_5_walk2.png` |

**Animator 配置**：
- 三个 controller（`Enemy_101/103/105.controller`）均已添加 `Walk` Trigger 参数
- 均已添加 `Walk` 状态（motion 指向对应 `Enemy_XXX_Walk.anim`）
- 均已添加 `Idle → Walk` 转移（条件：Walk trigger，Has Exit Time=false，Transition Duration=0）
- Walk 状态无退出转移（由代码 `_animator.Play("Idle")` 强制切回）

**代码改动**（`Assets/Scripts/Enemy/Enemy.cs`）：
- `StartMoving(isRush: true)` → `_animator.SetTrigger("Walk")` + `_animator.speed = max(1, 0.6/moveSpeed)` 加速动画
- `UpdateMovement()` 移动完成 → `_animator.ResetTrigger("Walk")` + `_animator.speed = 1f` + `_animator.Play("Idle")`
- `Stun()` 打断 rush → 同清理 animator speed + ResetTrigger

**Bug 修复记录**：
1. **只显示 walk1**：Walk 片段 0.6s，walk2@0.3s，但 moveSpeed=0.2-0.3s 导致运动在 walk2 渲染前完成。修复：按 `0.6/moveSpeed` 计算 animator speed
2. **被击退敌人原地踏步**：`SetTrigger("Walk")` 后 force-switch 回 Idle 时残留 trigger 未被 Reset。修复：移动完成时 `ResetTrigger("Walk")`

**新增文件**：
- `Assets/Animations/Enemy_101_Walk.anim`
- `Assets/Animations/Enemy_103_Walk.anim`
- `Assets/Animations/Enemy_105_Walk.anim`
- `Assets/Sprites/Enemy/Enemy1/Enemy_1_walk1.png`, `Enemy_1_walk2.png`
- `Assets/Sprites/Enemy/Enemy3/Enemy_3_walk1.png`, `Enemy_3_walk2.png`
- `Assets/Sprites/Enemy/Enemy5/Enemy_5_walk1.png`, `Enemy_5_walk2.png`

**修改文件**：
- `Assets/Animations/Enemy_101.controller`
- `Assets/Animations/Enemy_103.controller`
- `Assets/Animations/Enemy_105.controller`
- `Assets/Scripts/Enemy/Enemy.cs`

---

### ✅ 位移效果三选一系统 — 击退波/聚拢波/回旋波/连锁弹射（2025-08-09）

**设计目标**：为局内三选一添加四种改变敌人阵型/增加攻击覆盖的升级选项，形成构筑方向。

**四个新升级**：

| 升级 | effectType | 类别 | 效果 | 配置资产 |
|------|-----------|------|------|---------|
| 击退波 | `push_wave` | Numeric | 攻击命中将敌人击退N排（BOSS免疫），栈式阻塞 | `PushWave.asset` (intValue=1) |
| 聚拢波 | `convergence_wave` | Numeric | 攻击命中将敌人向col=2聚拢N步，冲突时%HP伤害+重分配col=1/3 | `ConvergenceWave.asset` (intValue=1, floatValue=0.10) |
| 回旋波 | `passive_return_wave` | Passive | 每N次攻击触发折返波，到达终点后折返再次命中50%伤害 | `ReturnWave.asset` (intValue=4, floatValue=0.50) |
| 连锁弹射 | `passive_chain_bounce` | Passive | 每N次攻击触发弹射，Pierce命中后弹射至同行最近敌人，最多M次，每次保留X%伤害 | `ChainBounce.asset` (intValue=6, secondaryIntValue=3, floatValue=0.80) |

**ColumnManager 位移 API**（`Assets/Scripts/Core/ColumnManager.cs`）：
- `ApplyPushWave(hitEnemies, pushAmount)` — 逐列栈式阻塞检测 → 执行击退，返回bool
- `ApplyConvergenceWave(hitEnemies, step, damagePercent)` — 向col=2聚拢 + 冲突裁决 + 聚拢伤害
- `CanPushColumn(col, pushAmount, hitSet)` — 击退阻塞检测：后方有非hit敌人则整列阻塞
- `ExecutePush(col, pushAmount, columnHitEnemies)` — 单列击退执行 + RecheckAttackRange
- `MoveEnemyToColumnAtRow(enemy, targetCol, targetRow)` — 敌人跨列移动，无冲突时使用
- `MoveEnemyToColumnEnd(enemy, targetCol)` — 追加到目标列末尾
- `ResolveConvergenceConflicts(conflicted, damagePercent)` — 冲突伤害(BOSS免疫) + 交替分配col=1/3

**不可变设计约束**（`design/immutable-constraints.md`）：
- 位移绝不导致敌人重叠
- 击退栈式阻塞：后方有非击退敌人→整列不动，BOSS免疫
- 聚拢向col=2移动，冲突时聚拢伤害+重分配到col=1/3
- BOSS不承受聚拢伤害但参与位移
- 运行时不再验证排列是否符合stage配置

**PassiveTriggerModule 扩展**（`Assets/Scripts/Core/PassiveTriggerModule.cs`）：
- `PassiveKind` 枚举新增 `ReturnWave` / `ChainBounce`
- `Register()` 支持 `passive_return_wave` / `passive_chain_bounce` effectType
- `OnAttackPerformed` 回调中根据 kind 分发 → `ExecuteReturnWave` / `ExecuteChainBounce`
- 回旋波/弹射仅对兼容攻击类型生效（Pierce/Sweep），否则退化为幻影攻击

**AttackWave 折返支持**（`Assets/Scripts/Attack/AttackWave.cs`）：
- `CreateReturnWave()` — 创建折返波，`shouldReturnWave=true`
- `SetupTravel()` 折返阶段：到达终点 → 反转targets → `DOMoveZ` 回到起点外 → `DORotate(0,360,0)` 回旋镖旋转
- `HitTarget()` 中 `_isReturning` 时伤害 × `_returnDamageMultiplier`

**AttackSystem 执行器**（`Assets/Scripts/Player/AttackSystem.cs`）：
- `ApplyDisplacementEffects()` — 攻击命中后调用：先击退，成功则跳过聚拢（互斥优先级）
- `ExecuteReturnWave()` — 创建青蓝色折返波，colorOverride跳过混白
- `ExecuteChainBounce()` — Pierce命中后遍历初始目标，逐次弹射FindNearestSameRowEnemy
- `CreateChainVisual()` — 使用Chain.prefab紫色拉伸+旋转，0.35s渐隐
- `_chainBouncePrefab` — SerializeField引用Chain预制体

**UpgradeEffectManager 数值路由**（`Assets/Scripts/Core/UpgradeEffectManager.cs`）：
- `GetPushWaveDistance()` / `GetConvergenceStep()` / `GetConvergenceDamagePercent()`
- `ApplyNumericEffect()` 中 push_wave 累积 intValue，convergence_wave 累积 intValue + floatValue
- `GetDescription()` 特殊分支：push/convergence 用 `{0}`=intValue×level, `{1}`=floatValue×100%

**Enemy 击退后攻击范围重检**（`Assets/Scripts/Enemy/Enemy.cs`）：
- `RecheckAttackRange()` — 被推离攻击范围→取消攻击→补齐前进；仍在范围内→直接攻击

**升级池配置**（`Assets/ScriptableObjects/Upgrades/UpgradePoolConfig.asset`）：
- 13个定义（含4个新位移升级），0个broken引用

**新增文件**：
- `Assets/ScriptableObjects/Upgrades/Definitions/PushWave.asset`
- `Assets/ScriptableObjects/Upgrades/Definitions/ConvergenceWave.asset`
- `Assets/ScriptableObjects/Upgrades/Definitions/ReturnWave.asset`
- `Assets/ScriptableObjects/Upgrades/Definitions/ChainBounce.asset`
- `Assets/Materials/PhantomWave.mat` — 紫色透明材质(0.5,0.2,0.8,0.6)
- `Locus/knowledge/design/immutable-constraints.md` — 位移系统不可变约束

**修改文件**：
- `Assets/Scripts/Core/ColumnManager.cs`
- `Assets/Scripts/Core/PassiveTriggerModule.cs`
- `Assets/Scripts/Core/UpgradeEffectManager.cs`
- `Assets/Scripts/Player/AttackSystem.cs`
- `Assets/Scripts/Attack/AttackWave.cs`
- `Assets/Scripts/Enemy/Enemy.cs`
- `Assets/Scenes/Battle.scene`

**本会话 Bug 修复**：
1. **折返波不可见**：`CreateInternal` 正常路径将颜色与白色50%混合 → 近乎透明。修复：折返波跳过混白，直接用colorOverride青蓝色
2. **连锁弹射不可见**：LineRenderer + Unlit/Color 在BIRP下被sprite遮挡。修复：改用Chain.prefab美术素材，紫色调拉伸旋转
3. **击退+聚拢冲突**：已通过互斥优先级解决（击退成功→return跳过聚拢）
<!-- locus:body:end -->
