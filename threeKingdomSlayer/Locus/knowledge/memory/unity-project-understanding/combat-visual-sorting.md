---
id: kd_26d2a809-56c5-4eed-8154-ebff0fdbae91
injectMode: inherit
summary: 当前策略：仅敌方投射物提升至 EnemyProjectiles Sorting Layer；敌人与所有玩家视觉保持 Default/Z 深度排序。已修复 QTE stagger 箭在暂停前未启动却悬空可见，以及胜负终局冻结箭矢不清理的问题。
aiEditMode: inherit
---

## 敌方投射物优先策略（2026-07-09）

`ProjectSettings/TagManager.asset` 仅新增了 `EnemyProjectiles` Sorting Layer，位于 `Default` 之上。

### 目标与约束
- 敌方飞射物压过所有玩家战斗视觉，保证预警和可读性。
- 敌人本体与玩家武器/特效仍保持原有 `Default` 层和 world-Z 的 2.5D 深度关系。
- 用户确认：敌方箭矢和敌人本体重叠时，箭矢优先显示。
- 不可再次将敌人本体或所有玩家特效放入固定全局层级；此前方案会让敌人压住武器特效，已回退。

### 统一入口
- `Assets/Scripts/Core/EnemyProjectileVisualPriority.cs`
  - `Apply(GameObject)` 将根节点和所有子级 `Renderer` 设为 `EnemyProjectiles`。
  - 新的敌方飞射物 Prefab 可挂此组件；业务创建路径也应在生成后调用 `Apply`，不依赖仅 Prefab 配置。
- `Assets/Scripts/Enemy/EnemyProjectile.cs`
  - `Launch()` 在缓存子级 SpriteRenderer 后调用 `EnemyProjectileVisualPriority.Apply(gameObject)`。
- `Assets/Scripts/QTE/QTEController.cs`
  - 普通 QTE projectile 与 QTE 箭矢波生成后调用 `Apply`；其中 `EnemyProjectile.Launch()` 仍是二次保障。

### 生命周期修复
- 普通敌方飞射物保持 scaled time：玩家主动暂停时会静止，不能用 unscaled tween，否则会在菜单中继续命中玩家。
- QTE 箭矢波存在随机 stagger。现在 stagger 期间实例保持 inactive，DelayedCall 触发时才激活并 Launch，避免暂停期间出现未初始化的悬空箭。
- `StageController` 在 Victory/Defeat 后会立刻隐藏并销毁场景中的 `EnemyProjectile`，防止终局时 `Time.timeScale=0` 让 flight/fade/timeout 永久停住。
- 若 QTE 箭仍在 stagger 中被提前 Deflect，`EnemyProjectile.Deflect()` 会直接销毁 inactive 对象。

### StageController 热重载修复
- 在编辑器热重载/Domain Reload 后，场景内已有的 `StageController` 不会重新执行 `Awake()`，静态 `Instance` 会被清空。
- `StageController.OnEnable()` 现在会在 `Instance == null` 时恢复自身单例引用，避免运行时系统读到空实例。
- 一次调试曾通过 `unity_execute` 创建多个 `StageCleanupTest` StageController；它们已从当前 Battle 场景的编辑器运行状态移除，未保存到场景。

### 当前资产
- `Assets/Prefabs/arrow.prefab` 已挂 `EnemyProjectileVisualPriority`。
- `EnemyProjectileGlowOutline` 的红光参数不承担遮挡解决职责；其两个子 SpriteRenderer 会统一进入敌方投射物层。
