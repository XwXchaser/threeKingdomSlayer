# 开发日志

## 2026-06-07 — 海浪技能系统 + 道具池 + Wave BUG修复

### 概述
实现海浪（Wave）主动技能：逐排推进海浪特效，命中敌人造成伤害+击退。新增道具池（ItemPoolConfig）系统统一管理道具掉落权重。修复两个 Wave 核心 BUG。

### 新增内容

**海浪技能系统** (4 个文件)
- `WaveManager.cs` — 海浪编排器，`TriggerWave(startRow, endRow, damage)` 逐排错开生成海浪，海浪结束后触发逐排补齐
- `WaveEffectPlayer.cs` — 单排海浪动画播放器，5帧 wave1→2→3→2→1 序列，帧3判定伤害+击退
- `WaveEffect.prefab` — 海浪预制体（wave1/2/3 精灵子对象）
- `Wave.asset` — 海浪技能定义（UpgradeDefinition, gestureId=wave, effectType=wave, floatValue=伤害, intValue=起始排, secondaryIntValue=结束排）

**道具池系统** (3 个文件)
- `ItemPoolConfig.cs` — ScriptableObject，定义道具掉落权重列表（UpgradeDefinition + weight）
- `ItemPoolConfig.asset` — 默认道具池配置
- `ItemTestHelper.cs` — 调试工具，强制触发指定道具效果

**其他**
- `ArrowRainEffect.prefab` — 替换旧 `TimedArrowEffect.prefab`
- `icon_31item_wave.png` — 海浪道具图标

### Wave BUG 修复

| Bug | 修复 |
|---|---|
| **紧凑不维持阵型** | `WaveManager.WaveSequence()` 中将 `CompactAllColumns` 替换为 `RowBasedFillUp()`。逐排补齐只压缩跨所有列完全清空的排，保留排对齐 |
| **同排敌人伤害不一致** | `WaveEffectPlayer.DoHitCheck()` 中 `GetEnemyAt(col, row)` 改为遍历列中全部敌人按 `rowIndex` 筛选。修复 push 后同排多敌人时只命中第一个的漏检问题 |

### 核心代码改动

**Column.cs**
- `GetEnemyAtRow`: BUG FIX — 改用 `enemy.rowIndex` 遍历查找，而非列表索引（push/compact 后列表位置≠排号）
- `CompactByClearRows`: 新增 `pushedToRow` 参数，防止击退被补齐抵消
- `CompactColumn`: 新增 `rangeStart/rangeEnd` 分段紧凑（波区/后方独立紧凑，Boss墙壁）

**ColumnManager.cs**
- `RemoveEnemyFromColumn`: 新增 `skipChain` 参数，PerRow 模式下由 RowBasedFillUp 统一处理
- `RowBasedFillUp` / `PostDisplacementFillUp`: 新增 `pushedToRow` 参数
- `CompactAllColumns`: 新增 `rangeStart/rangeEnd` 参数

**Enemy.cs**
- 新增 `OnDeathAnimComplete` 事件（Boss 死亡锦囊触发时机）
- `TakeDamage`: 新增 `isParryInterrupt` 参数；Boss 阶段切换伤害预测修正（含 launched 倍率、hp=0 直接死亡）；Boss/非Boss 攻击打断逻辑拆分
- `Die`: QTE 演出中死亡时清理 QTE 状态

**UpgradeChoiceManager.cs** — 集成 `itemPoolConfig`，道具选择从道具池权重抽取
**BuffDisplayPanel.cs** — 新增 wave 手势点击处理
**StageController.cs** — 道具系统集成
**UpgradeDefinitionEditor.cs** — 新增 `triggerParam` 字段支持

### 平衡调整
| 变更 | 说明 |
|---|---|
| Zhangfei_Parry damage 5→1, poise 50→10 | 招架伤害和架势大幅下调 |
| Zhangfei_Sweep damage 8→5, range 2→5 | 横扫伤害下调，范围扩大至全屏 |
| Enemy_104 maxHealth 500→200 | Boss 血量下调 |
| testStage 新增 wave 1+2 | 增加测试波次 |

### 涉及文件
- `Assets/Scripts/Effect/WaveManager.cs` — 新增
- `Assets/Scripts/Effect/WaveEffectPlayer.cs` — 新增
- `Assets/Prefabs/Effects/WaveEffect.prefab` — 新增
- `Assets/ScriptableObjects/Upgrades/Definitions/Wave.asset` — 新增
- `Assets/Scripts/Core/ItemPoolConfig.cs` — 新增
- `Assets/ScriptableObjects/Upgrades/ItemPoolConfig.asset` — 新增
- `Assets/Scripts/DebugTools/ItemTestHelper.cs` — 新增
- `Assets/Prefabs/Effects/ArrowRainEffect.prefab` — 新增
- `Assets/Prefabs/Effects/TimedArrowEffect.prefab` — 删除
- `Assets/Scripts/Core/Column.cs` — GetEnemyAtRow/CompactByClearRows/CompactColumn 改进
- `Assets/Scripts/Core/ColumnManager.cs` — RowBasedFillUp/CompactAllColumns/RemoveEnemyFromColumn 改进
- `Assets/Scripts/Enemy/Enemy.cs` — TakeDamage/Die/OnDeathAnimComplete 改进
- `Assets/Scripts/Core/UpgradeChoiceManager.cs` — 道具池集成
- `Assets/Scripts/UI/BuffDisplayPanel.cs` — wave 手势
- `Assets/Scripts/Managers/StageController.cs` — 道具系统集成
- `Assets/Scripts/Managers/EnemyManager.cs` — 死亡回调
- `Assets/Scripts/Player/AttackSystem.cs` — 小改
- `Assets/Scripts/QTE/QTEController.cs` — 小改
- `Assets/Scripts/Wave/WaveSpawner.cs` — 小改
- `Assets/Scripts/Editor/UpgradeDefinitionEditor.cs` — triggerParam
- `Assets/Scenes/Battle.scene` — WaveManager 组件+道具池引用
- `Assets/Resources/StageConfigs/testStage.asset` — 测试波次扩展
- `Assets/Resources/EnemyPrefabs/Enemy_104.prefab` — Boss 血量下调
- `Assets/Prefabs/UI/Skills/Zhangfei_Parry.asset` — 平衡调整
- `Assets/Prefabs/UI/Skills/Zhangfei_Sweep.asset` — 平衡调整
- `Locus/knowledge/design/attack-interrupt-system.md` — 更新
- `Locus/knowledge/design/boss-mechanics.md` — 更新

---

## 2026-05-17 — BOSS QTE 攻击系统

### 概述
为 Enemy_104（Boss）实现 QTE 攻击系统：点击型和划动型两种 QTE 交互，支持单次攻击内多个 QTE 交错判定（TripleClick 0s/0.3s/0.6s 三连击），完整状态机驱动，玩家输入优先拦截（QTE 优先级高于普通攻击），成功扣 Boss 架势+充能大招，失败扣玩家血量。

### 新增内容

**QTE 配置层** (3 个 ScriptableObject)
- `QTEConfig` — 单个 QTE 行为配置：类型（Click/Swipe）、时机（预警窗口、判定窗口）、划动参数（方向/角度容差/最小速度）、效果数值（架势伤害/大招充能/失败伤害）、视觉（指示器 prefab、屏幕归一化坐标）
- `QTEAttackConfig` — 一次 QTE 攻击的完整配置：QTESlot 列表（config + 延迟秒数）、BOSS 动画参数（Trigger 名/前摇时间）、可选飞行物（prefab/飞行时间/目标 Z）、攻击后冷却
- `BossQTEData` — Boss QTE 数据根配置：QTE 攻击列表、循环开关、首次冷却、基础冷却

**QTE 运行时** (3 个 MonoBehaviour)
- `QTEController` — QTE 状态机：Idle → CoolingDown → WaitingForAttackFinish → PerformingQTEAttack → QTEJudging → QTECompleted。Phase-based 驱动（每个 slot 按 delay 独立 warning→judge→resolve），支持 `TryConsumeClick` / `TryQTESwipe` 输入接口，Click 用 `RectTransformUtility.RectangleContainsScreenPoint` 判定，Swipe 用角度+速度双阈值判定。成功扣 Boss 架势+充能大招，失败扣玩家血量
- `QTEDisplay` — Canvas UI 管理器：`SpawnIndicator`（DOTween scale pulse 预警动画）、`ShowQTEResult`（成功/失败特效 + 指示器缩小消失）、`ClearAllIndicators`
- `QTEProjectile` — DOTween 飞行物：`Initialize` 飞向目标坐标、`ContinuePassThrough`（失败时穿过摄像机）、`DestroyOnSuccess`（成功时销毁）

**改动现有文件**
- `InputManager.cs` — `ProcessGesture` 顶部新增 `TryConsumeQTEInput`：QTE 判定窗口内优先匹配 QTE 点击/划动，匹配成功则短路后续攻击逻辑
- `Enemy.cs` — 新增 `QTEAttacking` 枚举值；`EnterQTEAttack` / `ExitQTEAttack` 方法；`StartAttacking` 中 QTE 等待期间阻止新攻击

**ScriptableObject 资产** (`Assets/ScriptableObjects/QTE/`)
| 资产 | 说明 |
|---|---|
| `BossQTEData_104.asset` | TripleClick + Swipe 两轮攻击，loopAttacks=true，firstCooldown=5s |
| `QTEAttackConfig_TripleClick.asset` | 3 个 Click slot (0s/0.3s/0.6s)，animationLeadTime=0.3s |
| `QTEAttackConfig_Swipe.asset` | 1 个 Swipe slot (0s delay，方向=右，最小速度=500px/s) |
| `QTEConfig_Click_1/2/3.asset` | Click 配置（屏幕三位置，架势伤害 20，失败伤害 15） |
| `QTEConfig_Swipe.asset` | Swipe 配置（架势伤害 40，失败伤害 20，判定窗口 2s） |

**Canvas UI Prefab** (`Assets/Prefabs/QTE/`)
- `QTE_Click_Indicator_1/2/3.prefab` — Image + RectTransform (200×200，circle 1 精灵)
- `QTE_Swipe_Indicator.prefab` — Image + RectTransform (400×60，PoiseBar 精灵)

**场景/Prefab 连线**
- `Enemy_104.prefab` — 挂载 QTEController (qteData=BossQTEData_104, enemy 自动解析)
- `Battle.scene` — QTEDisplay 挂载到 Canvas (indicatorParent=QTEIndicators)

### 涉及文件
- `Assets/Scripts/QTE/QTEConfig.cs` — 新增
- `Assets/Scripts/QTE/QTEAttackConfig.cs` — 新增
- `Assets/Scripts/QTE/BossQTEData.cs` — 新增
- `Assets/Scripts/QTE/QTEController.cs` — 新增
- `Assets/Scripts/QTE/QTEDisplay.cs` — 新增
- `Assets/Scripts/QTE/QTEProjectile.cs` — 新增
- `Assets/Scripts/Player/InputManager.cs` — 新增 TryConsumeQTEInput 优先拦截
- `Assets/Scripts/Enemy/Enemy.cs` — 新增 QTEAttacking 状态
- `Assets/ScriptableObjects/QTE/BossQTEData_104.asset` — 新增
- `Assets/ScriptableObjects/QTE/QTEAttackConfig_TripleClick.asset` — 新增
- `Assets/ScriptableObjects/QTE/QTEAttackConfig_Swipe.asset` — 新增
- `Assets/ScriptableObjects/QTE/QTEConfig_Click_1.asset` — 新增
- `Assets/ScriptableObjects/QTE/QTEConfig_Click_2.asset` — 新增
- `Assets/ScriptableObjects/QTE/QTEConfig_Click_3.asset` — 新增
- `Assets/ScriptableObjects/QTE/QTEConfig_Swipe.asset` — 新增
- `Assets/Prefabs/QTE/QTE_Click_Indicator_1.prefab` — 新增
- `Assets/Prefabs/QTE/QTE_Click_Indicator_2.prefab` — 新增
- `Assets/Prefabs/QTE/QTE_Click_Indicator_3.prefab` — 新增
- `Assets/Prefabs/QTE/QTE_Swipe_Indicator.prefab` — 新增
- `Assets/Resources/EnemyPrefabs/Enemy_104.prefab` — 挂载 QTEController
- `Assets/Scenes/Battle.scene` — 挂载 QTEDisplay

---

## 2026-01-17 — 调试日志实例化 + 眩晕状态修正 + 招架平衡调整 + UI修复

### 概述
运行时调试日志全面升级（区分同预制体不同实例），修复眩晕状态机的两个严重 Bug，招架平衡性调整（非Boss敌人移除架势伤害），铜钱飘字裁剪修复，Stab 伤害下调。

### 新增内容

**Enemy 实例 ID 系统** (`Enemy.cs`)
- 新增 `instanceId`（`[System.NonSerialized]`，运行时自增分配）和 `DebugTag` 属性（格式 `#3(101)` = 实例#3 预制体ID 101）
- `Initialize()` 中分配 `instanceId`
- Enemy.cs / Column.cs / ColumnManager.cs 全部 ~80 处 `Debug.Log` 从 `enemyId={e.enemyId}` 改为 `{e.DebugTag}`
- 同屏出现 3 个 Enemy_101 实例时日志从 `enemyId=101...` 变为 `#1(101)...` / `#7(101)...` / `#12(101)...`，可区分不同实例

### Bug 修复

| Bug | 修复 |
|---|---|
| **眩晕冻结半空敌人** | `Stun()` 检测 `state == EnemyState.Launched`，击飞中仅重置 Poise，不进入眩晕。敌人被挑飞后再受招架不会卡在半空 |
| **眩晕后移动状态残留** | `Stun()` 进入时主动 `DOTween.Kill(transform)` + 清理 `isMovingToNextRow`/`isRushMove`/`moveProgress`，调用 `UpdateWorldPosition()` 锁定当前位置。眩晕结束后检查 `pendingRushMove`，若被标记则恢复 Rush 链 |
| **铜钱飘字被裁剪** | CoinCounterUI 新增 `floatTextRectSize`（默认 200x60），飘字 TMP 设置 `overflowMode = Overflow`，防止 RectTransform 过小时文字被裁剪 |

### 平衡调整

| 变更 | 说明 |
|---|---|
| **招架不再对普兵造成架势伤害** | `ExecuteParry()` 中非 Boss 敌人移除 `TakePoiseDamage` 调用，仅造成伤害。Boss 保留完整招架机制 |
| **Stab 伤害 100→10** | `Zhangfei_Stab.asset` damage 从 100 下调至 10 |

### 涉及文件
- `Assets/Scripts/Enemy/Enemy.cs` — 新增 instanceId/DebugTag；Stun 状态修正；全部日志升级
- `Assets/Scripts/Core/Column.cs` — 全部日志升级为 DebugTag
- `Assets/Scripts/Core/ColumnManager.cs` — 全部日志升级为 DebugTag
- `Assets/Scripts/Player/AttackSystem.cs` — ExecuteParry 非Boss移除TakePoiseDamage
- `Assets/Scripts/UI/CoinCounterUI.cs` — floatTextRectSize + overflowMode
- `Assets/ScriptableObjects/Skills/Zhangfei_Stab.asset` — damage 100→10
- `Assets/Prefabs/UI/HeroHUD_Zhangfei.prefab` — HP 纹理替换（压缩）+ 头像新增
- `Assets/Prefabs/UI/BossHealthBar.prefab` — Boss血条预制体调整
- `Assets/Prefabs/Slash.prefab` / `Stab.prefab` — 攻击波预制体调整
- `Assets/Resources/EnemyPrefabs/Enemy_104.prefab` — Boss预制体调整
- `Assets/Scenes/Battle.scene` — 场景层级调整
- `Assets/Sprites/zhangfei/` — HP纹理压缩（-40%体积）；新增 zhangfei_head.png / money.png

---

## 2025-12-24 — EnemyConfig 合并到 Enemy + 幽灵引用架构规范

### 概述
将 `EnemyConfig` ScriptableObject 消除，所有敌人属性直接序列化到 Enemy 预制体的 `Enemy` MonoBehaviour 组件上。这是「幽灵引用」清理的核心案例。

### 架构变更
- **Enemy.cs**：移除 `public EnemyConfig config` 字段，新增 ~25 个直接 `[SerializeField]` 字段（enemyName、enemyId、maxHealth、occupySlots、attackSpeed/Damage/Range/SpawnDuration/DrawDuration、moveSpeed、maxPoise、stunDuration、launchDuration/YHeight、launchedDamageTakenMultiplier/HitExtendDuration、coinReward、isBoss、6 种伤害倍率、parryStunThresholds）。`Initialize` 签名从 `(EnemyConfig cfg, int col, int row)` 改为 `(int col, int row)`
- **EnemyPool.cs**：新增 `GetEnemyOccupySlots(int enemyId)` 方法，从预制体读取占位数（无需实例化）
- **WaveSpawner.cs**：移除 `enemyConfigs` 列表、`enemyConfigCache` 静态字典、`GetEnemyConfig()` 方法。`SpawnRow()` 改用 `enemyPool.GetEnemyOccupySlots()` 和直接 `enemy.Initialize(col, row)`
- **Column.cs / ColumnManager.cs / EnemyManager.cs / StageController.cs / AttackSystem.cs**：所有 `enemy.config?.X` / `enemy.config != null ? enemy.config.X : default` 替换为 `enemy.X` 直接字段访问
- **ParryStunThreshold** struct 从 EnemyConfig.cs 移至 Enemy.cs
- **删除文件**：`EnemyConfig.cs` + `.meta`、3 个 `.asset` 文件（Enemy_Skeleton/Shilde/Boss）
- **预制体迁移**：Enemy_101/102/104.prefab 的 Enemy 组件已写入原 EnemyConfig 全部值

### 设计规范
- 新增 `design/anti-ghost-reference.md` — 架构规范：禁止 Resources.Load/静态缓存/字符串ID查找获取配置数据
- 新增 `skill/use-architecture-constraints.md` — 使用指南：新对话如何加载并遵守此规范

### 涉及文件
- `Assets/Scripts/Enemy/Enemy.cs` — 字段迁移 + `Initialize` 改签
- `Assets/Scripts/Enemy/EnemyPool.cs` — 新增 `GetEnemyOccupySlots()`
- `Assets/Scripts/Wave/WaveSpawner.cs` — 移除 config 体系
- `Assets/Scripts/Core/Column.cs` — `.config?.enemyId` → `.enemyId`
- `Assets/Scripts/Core/ColumnManager.cs` — 同上
- `Assets/Scripts/Managers/EnemyManager.cs` — `.config.attackDamage` → `.attackDamage`
- `Assets/Scripts/Managers/StageController.cs` — `.config.coinReward` → `.coinReward`
- `Assets/Scripts/Player/AttackSystem.cs` — `.config.maxPoise` → `.maxPoise`
- `Assets/Resources/EnemyPrefabs/Enemy_101.prefab` — 写入骨架兵值
- `Assets/Resources/EnemyPrefabs/Enemy_102.prefab` — 写入盾兵值
- `Assets/Resources/EnemyPrefabs/Enemy_104.prefab` — 写入Boss值
- `_summaries/` — 全量更新 Enemy/Core/Wave/Index/CHANGELOG

---

## 2026-05-03 — 技能配置编号字段

### 概述
为 `AttackSkillConfig` 和 `UltimateSkillConfig` 新增 `id` 字段，方便按编号管理和定位技能资产。

### 修改文件
- `Assets/Scripts/Core/AttackSkillConfig.cs` — 新增 `public int id` 字段（`[Header("基本信息")]` 首位）
- `Assets/Scripts/Core/UltimateSkillConfig.cs` — 新增 `public int id` 字段（`[Header("基础")]` 首位）

### 资产变更
| 资产 | id |
|---|---|
| `Zhangfei_Stab.asset` | 1 |
| `Zhangfei_Slash.asset` | 2 |
| `Zhangfei_Pierce.asset` | 3 |
| `Zhangfei_Sweep.asset` | 4 |
| `Zhangfei_Launch.asset` | 5 |
| `Zhangfei_Parry.asset` | 6 |
| `Zhangfei_BerserkUlt.asset` | 1 |

---

## 2025-12-23 — 铜钱计数器UI + 铜钱流转修正 + MainMenu总铜钱显示

### 概述
在 Battle 场景新增铜钱计数器UI（CoinCounterUI），修复铜钱数据流问题（本局铜钱 vs 总铜钱），并在 MainMenu 显示玩家持有的总铜钱数。

### 新增文件
- `Assets/Scripts/UI/CoinCounterUI.cs` — Battle 场景铜钱UI控制器。订阅 `PlayerState.OnCoinGained` 事件，DOTween 缩放跳动 + 飘字动画

### 场景变更
- `Battle.scene` — `BattleHUD(Canvas)/CoinCounter` 层级：
  - `CoinIcon` (Image, 40x40, 金色) — 获得铜钱时 DOPunchScale 跳动
  - `TotalText` (TMP, fontSize 40, 白色, 方正粗黑宋简体) — 显示本局铜钱数，获得时 DOPunchScale 跳动
  - `FloatAnchor` (空GameObject) — 控制飘字起始位置，拖动调整
- `MainMenu.scene` — `Canvas/CoinDisplay` 重构为图标+文字结构：
  - `CoinIcon` (Image, 40x40, 金色)
  - `CoinText` (TMP, fontSize 30, 金色, 方正粗黑宋简体) — 显示总铜钱数
  - 锚定右上角 (-30, -30)

### 修改文件
- `Assets/Scripts/UI/CoinCounterUI.cs` — 新增
- `Assets/Scripts/Player/PlayerState.cs` — 新增 `OnCoinGained(int amount, int total)` 事件；`coinCount` 仅记录本局铜钱，`ResetPlayer()` 归零
- `Assets/Scripts/Managers/StageController.cs` — `StartStage()` 移除存档铜钱恢复；`OnAllWavesCleared()` 通关时结算 `SaveManager.SetCoins(saved + sessionCoins)`
- `Assets/Scripts/UI/MainMenuUI.cs` — 新增 `coinText` 字段，`UpdateCoinDisplay()` 读取 `SaveManager.Load().coinCount` 显示总铜钱

### 铜钱数据流
```
杀敌 → PlayerState.AddCoins() → coinCount++ (本局) + 触发 OnCoinGained
通关 → StageController.OnAllWavesCleared() → SaveManager.SetCoins(存档总铜钱 + 本局铜钱)
新关卡 → PlayerState.ResetPlayer() → coinCount = 0 (从零开始)
MainMenu → MainMenuUI.UpdateCoinDisplay() → 显示 SaveManager.Load().coinCount (总持有)
```

### Bug 修复

| Bug | 修复 |
|---|---|
| **TotalText 从0跳到总金币** | `StartStage()` 中移除了从 SaveManager 恢复铜钱到 PlayerState.coinCount 的代码。本局铜钱从0开始 |
| **通关时铜钱已提前累加** | 金币仅在通关时结算：`OnAllWavesCleared()` 中 `SaveManager.SetCoins(saved + sessionCoins)` |
| **金色飘字位置不对** | 新增 `FloatAnchor` 空GameObject 控制飘字起始位置，Inspector 中拖动调整 |

---

## 2025-12-21 — 选关系统 + 存档系统 + Victory BUG修复

### 概述
新增选关界面、存档系统、修复击杀全部敌人不弹VictoryPanel的BUG。MainMenu重构为4按钮布局（新游戏/继续游戏/删除存档/退出），关卡按钮自动从StageConfigManager生成。

### 新增文件
- `Assets/Scripts/Core/StageConfigManager.cs` — MonoBehaviour，挂载于MainMenu场景。Inspector中拖入StageConfig资产并排序，列表顺序决定解锁顺序。关卡配置的唯一来源，不再自动扫描Resources
- `Assets/Scripts/Core/SaveManager.cs` — 静态存档管理器。PlayerPrefs + JsonUtility 存储 `clearedStageIds` + `coinCount`。`HasSave` / `MarkStageCleared()` / `Delete()` / `GetNextAvailableStageId()` / `IsStageCleared()`
- `Assets/Scripts/Core/StageRegistry.cs` — ScriptableObject 关卡注册表（创建菜单：`一夫当关/关卡注册表`）。保留为资产但运行时不被 StageConfigManager 加载
- `Assets/Resources/StageConfigs/` — 关卡配置资产目录
- `Assets/Resources/StageRegistry.asset` — 关卡注册表资产

### 修改文件
- `Assets/Scripts/UI/MainMenuUI.cs` — 全面重构。4个按钮预置场景中（newGameButton/continueButton/deleteSaveButton/quitButton），Run时通过RefreshUI控制显隐。选关网格从StageConfigManager自动生成（GridLayoutGroup，已通关/可挑战/未解锁三种状态）。OnNewGame清除存档并从第一关开始；OnContinueGame找第一个未通关关卡
- `Assets/Scripts/Managers/StageController.cs` — `SelectedStageId`(int) 替换为 `PendingStageConfig`(static StageConfig)。Awake中消费PendingStageConfig覆盖stageConfig。Victory时调用SaveManager.MarkStageCleared()/SetCoins()存档。StartStage中从SaveManager恢复铜钱
- `Assets/Scripts/UI/BattleHUD.cs` — `OnMainMenuButton()` 增加 fallback：`StageController.Instance` 为 null 时直接 `SceneManager.LoadScene("MainMenu")`。修复VictoryPanel/DefeatPanel的"返回主菜单"按钮点击无效BUG
- `Assets/Scripts/Wave/WaveSpawner.cs` — `Start()` 开头新增 `enemyConfigCache.Clear()`，修复场景重载后静态缓存残留导致新配置无法覆盖旧缓存的BUG

### 场景变更
- `MainMenu.scene` — 新增 StageConfigManager GameObject（含2个StageConfig）；Canvas新增 NewGameButton/ContinueButton/DeleteSaveButton/QuitButton，onClick 持久连线到 MainMenuUI 方法
- `Battle.scene` — Victory/Defeat 面板的 MainMenuButton onClick 接线到 BattleHUD.OnMainMenuButton

### Bug 修复

| Bug | 修复 |
|---|---|
| **击杀全部敌人不弹VictoryPanel** | WaveSpawner的`enemyConfigCache`为静态字典，场景重载后旧缓存残留。`ContainsKey`检查导致新StageConfig的EnemyConfig无法覆盖缓存，敌人用过期配置初始化失败，`IsAllEnemiesDead`永远为false。`Start()`中加`enemyConfigCache.Clear()` |
| **通关后"返回主菜单"按钮无功能** | Victory/Defeat面板按钮onClick丢失。BattleHUD.OnMainMenuButton增加fallback直接`SceneManager.LoadScene("MainMenu")`，Battle.scene中持久化连线 | 
| **无通关存档时显示"继续游戏"按钮** | MainMenuUI.RefreshUI() 根据 `SaveManager.HasSave` 控制显隐：无存档→仅显示"新游戏"，有存档→显示"继续游戏"+ "删除存档" |
| **继续游戏/选关按钮始终加载第一关** | StageController从`SelectedStageId`(int)改为`PendingStageConfig`(StageConfig静态变量)，MainMenu设置后跨场景传递，Battle Awake消费。确保正确关卡配置被加载 |

### 扩展指南
- **配置更多关卡**：在MainMenu场景中找到 StageConfigManager GameObject，在Inspector的Stages列表中拖入StageConfig资产并排序。列表顺序 = 解锁顺序
- **新增存档字段**：在`SaveData`类中添加字段，在`SaveManager`中添加对应getter/setter

---

## 2025-12-20 — Ult 独立配置体系重构（UltimateSkillConfig）

### 概述
将大招从 `AttackSkillConfig` 中完全剥离，建立独立的 `UltimateSkillConfig` ScriptableObject 体系。同时修复狂怒大招（Berserk）的三个运行时 Bug。

### 架构变更
- **新增 `UltimateSkillConfig`**：独立于普通攻击配置的 Ult 专属资产。公共字段：`cooldown`（秒，与普通技能统一单位）、`energyCost`、`damage`、`damageType`。Berserk 专用：`berserkDuration`、`berserkStabCooldown`、`berserkDamageMultiplier`。未来其他类型 Ult 可扩展各自字段
- **`AttackSkillConfig` 清理**：移除 `isUltimate`、`ultimateEnergyCost`、`berserkDuration`、`berserkStabCooldown`、`berserkDamageMultiplier`。仅保留 `ultimateEnergyGain`（普攻命中充能值）
- **`HeroConfig`**：新增 `ultimateSkillConfig` 字段，独立于 `skillConfigs` 列表
- **`PlayerState.GetCooldownDuration`**：Ult 路径改为读取 `heroConfig.ultimateSkillConfig.cooldown`
- **`UltimateSystem.EnergyCost`**：改为读取 `heroConfig.ultimateSkillConfig.energyCost`

### 新增文件
- `Assets/Scripts/Core/UltimateSkillConfig.cs` — 大招 ScriptableObject，菜单 `一夫当关/大招技能配置`
- `Assets/Scripts/Core/UltimateEffect_Berserk.cs` — 狂怒大招效果（无敌+自动Stab+禁技能输入）
- `Assets/Prefabs/UltimateBerserkEffect.prefab` — 挂载 UltimateEffect_Berserk 的预制体
- `Assets/ScriptableObjects/Skills/Zhangfei_BerserkUlt.asset` — 张飞狂怒 Ult（cooldown=10s, damage=100, berserkDuration=5s, stabCooldown=0.5s, mult=1.5x）

### 删除文件
- `Assets/ScriptableObjects/Skills/Zhangfei_Ultimate.asset` — 旧 Ult（AttackSkillConfig 类型，已废弃）

### 修改文件
- `Assets/Scripts/Core/AttackSkillConfig.cs` — 清理 Ult 字段
- `Assets/Scripts/Core/HeroConfig.cs` — 新增 `ultimateSkillConfig` 字段
- `Assets/Scripts/Core/UltimateSystem.cs` — `EnergyCost` 属性读 `ultimateSkillConfig.energyCost`；`ActivateUltimate` 减 `EnergyCost` 而非重置为 0
- `Assets/Scripts/Player/PlayerState.cs` — `GetCooldownDuration` Ult 路径读 `ultimateSkillConfig.cooldown`
- `Assets/Scripts/Player/AttackSystem.cs` — 新增 `ForceExecuteStab(int column, float damage)` 供 Ult 效果调用
- `Assets/Scripts/Player/InputManager.cs` — 新增 `skillInputEnabled` 字段，`ProcessGesture` 检查此开关
- `Assets/Scripts/UI/BattleHUD.cs` — 新增 `SetHealthBarColor()` / `ResetHealthBarColor()` 血条颜色控制
- `Assets/ScriptableObjects/Warrior/Hero_Zhangfei.asset` — `skillConfigs` 从 7→6（移除 Ult），`ultimateSkillConfig` 指向 Zhangfei_BerserkUlt
- `Assets/Scenes/Battle.scene` — UltimateSystem 的 `ultimateEffectPrefab` 指向 UltimateBerserkEffect

### Bug 修复

| Bug | 修复 |
|---|---|
| **Stab 只戳左/中列，漏掉右列** | `ExecuteAutoStab()` 每轮重新收集存活列，用 `stabRoundIndex` 轮转遍历，确保所有列依次被戳 |
| **Ult 结束后血条变白不恢复原色** | 不再依赖 `BattleHUD` 间接保存。`UltimateEffect_Berserk` 直接持有 `Image` 引用和原始 `Color?`，Cleanup 时直接写回 |
| **Ult cooldown 单位不一致** | 统一为秒。`UltimateSkillConfig.cooldown=10` = 每 10 秒发动 1 次，与 `AttackSkillConfig.cooldown` 同一标准 |

### 扩展指南
- **新 Ult 效果**：继承 `UltimateEffect`，在 `Execute()` 中实现逻辑，可读取 `PlayerState.Instance.heroConfig.ultimateSkillConfig` 获取配置
- **新 Ult 资产**：通过 Create 菜单 `一夫当关/大招技能配置` 创建 `.asset`，填入字段，拖入对应 `HeroConfig.ultimateSkillConfig`

---

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
