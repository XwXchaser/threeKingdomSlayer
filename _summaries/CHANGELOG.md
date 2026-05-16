# 开发日志

## 2025-05-11 — 选关系统 + 存档系统 + BUG修复

### 概述
新增选关界面、存档/继续/删除功能、StageConfigManager Inspector 配置体系，修复胜利面板返回主菜单无响应、继续游戏按钮状态等 BUG。

### 改动明细

**选关系统 (MainMenuUI)**
- 4 个主菜单按钮预置于场景 Hierarchy（NewGameButton/ContinueButton/DeleteSaveButton/QuitButton），不再运行时生成
- 选关网格自动生成：按 StageConfigManager 配置的关卡列表横向排列，满行换行
- 关卡按钮根据存档状态显示 [已通关]/[可挑战]/[未解锁]，锁定关卡不可点击
- 所有按钮添加持久化 onClick（Editor 可见），运行时 RefreshUI 切换可见性

**存档系统 (SaveManager)**
- 新增 `SaveManager` 静态工具类，基于 `PlayerPrefs` + JSON 序列化
- 存储内容：已通关关卡 ID 列表、持有铜钱数
- `HasSave` / `Load()` / `Save()` / `Delete()` / `MarkStageCleared()` / `SetCoins()`
- 通关时自动保存：标记关卡已通关 + 保存铜钱
- 主菜单根据 `HasSave` 显示"新游戏"或"继续游戏"

**关卡配置统一 (StageConfigManager)**
- 新增 `StageConfigManager` MonoBehaviour，挂载在 MainMenu 场景的独立 GameObject 上
- Inspector 中拖入 StageConfig 资产并排序，列表顺序决定关卡解锁顺序
- 不再自动扫描 Resources 文件夹
- `StageController` 改为通过 `PendingStageConfig` 静态变量接收关卡配置（跨场景传递 SO 引用）

**BUG 修复**
- BUG1：`BattleHUD.OnMainMenuButton()` 添加 fallback — 若 `StageController.Instance` 为 null 则直接 `SceneManager.LoadScene`
- BUG2：`RefreshUI()` 在 `Start()` 时根据 `SaveManager.HasSave` 切换按钮显示状态
- BUG3：`OnContinueGame` 和选关按钮改用 `PendingStageConfig` 传递 StageConfig，不再依赖 stageId 间接查找
- 移除所有硬编码 Stage_1 引用

### 涉及文件
- `Assets/Scripts/Core/StageConfigManager.cs` — 新增 Inspector 关卡配置管理器
- `Assets/Scripts/Core/SaveManager.cs` — 新增存档管理器
- `Assets/Scripts/Core/StageRegistry.cs` — 新增关卡注册表 ScriptableObject
- `Assets/Scripts/UI/MainMenuUI.cs` — 选关网格、按钮可见性、onClick 持久化、PendingStageConfig
- `Assets/Scripts/Managers/StageController.cs` — SelectedStageId → PendingStageConfig
- `Assets/Scripts/UI/BattleHUD.cs` — OnMainMenuButton 添加 fallback
- `Assets/Scenes/MainMenu.scene` — StageConfigManager GO + 按钮 onClick 持久化
- `_summaries/Core.summary.md` — 更新核心类与接口
- `_summaries/UI.summary.md` — 更新 MainMenuUI/BattleHUD 接口
- `_summaries/Managers.summary.md` — 更新 StageController 接口

## 2025-05-09 — 招架（Parry）机制完善

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
