# UI 模块

## 模块名称
UI（用户界面与特效）

## 主要职责
HUD 显示（血量、复活、击杀、金币、波次、冷却指示器、胜利/失败面板），带模糊效果的场景转场动画，浮动伤害跳字，主菜单，精灵序列动画。

## 核心类

| 类 | 说明 |
|---|---|
| `BattleHUD` (MonoBehaviour) | 战斗 UI 绑定。订阅全部 `PlayerState` 事件。更新血条 Slider+Text、复活显示、击杀数、金币数、波次进度。每帧冷却 UI：Image 的 `fillAmount` 径向冷却 + 各技能图标独立的充能填充 Image。显示胜/负面板及金币统计。重开和主菜单按钮。`SetHealthBarColor(Color)` / `ResetHealthBarColor()` 供 Ult 效果控制血条颜色。 |
| `CameraManager` (MonoBehaviour) | 挂载于相机的场景转场：背景 RectTransform 缩放 + 位置动画 + `OnRenderImage` 模糊效果（使用自定义 shader `Hidden/BlurEffect`）。`PlayDeparture()` 动画结束后加载下一场景。`PlayArrival()` 场景启动时动画。用静态 `IsArriving` 标志实现跨场景交接。 |
| `DamageNumber` (MonoBehaviour) | 浮动伤害跳字（TextMeshPro）。`Show(Vector3 worldPos, float damage)` 启动 DOTween 上浮 + 淡出。完成后调用 `OnReturnToPool` 回调返还 `DamageNumberManager`。配置：红色文字、黑色描边、粗体。 |
| `CoinCounterUI` (MonoBehaviour) | Battle 场景铜钱UI。订阅 `PlayerState.OnCoinGained`。CoinIcon（Image，获得时 DOPunchScale 跳动）+ TotalText（TMP，显示本局铜钱数，获得时 DOPunchScale 跳动）+ FloatAnchor（空GameObject，控制飘字起始位置）。获得铜钱时在 FloatAnchor 位置生成金色 "+N" 飘字（TMP，overflowMode=Overflow 防裁剪），DOTween 上浮+淡出后销毁。Inspector 可配：iconPunchScale/Duration、totalPunchScale/Duration、floatDuration、floatTextColor/FontSize/floatTextRectSize/floatUpDistance。 |
| `MainMenuUI` (MonoBehaviour) | 主菜单：4个预置按钮（新游戏/继续游戏/删除存档/退出），根据 `SaveManager.HasSave` 控制显隐。选关网格从 `StageConfigManager` Inspector 列表自动生成（GridLayoutGroup 横向排列，溢出换行），每按钮显示关卡名+状态（已通关/可挑战/未解锁）。OnNewGame 删除存档从第一关开始；OnContinueGame 找第一个未通关关卡；OnDeleteSave 删除存档并重建网格。关卡选择通过 `StageController.PendingStageConfig` 跨场景传递。coinText 字段显示总铜钱（SaveManager.Load().coinCount）。 |
| `PingPongAnim` (MonoBehaviour) | 精灵序列动画器。两种模式：idle ping-pong（帧 [0,1] 间来回），随机触发的眨眼播放完整帧序列。同时支持 `SpriteRenderer` 和 UI `Image`。可配置 FPS 和眨眼概率。 |
| `UltimateButtonUI` (MonoBehaviour) | 大招按钮 UI 控制器。订阅 UltimateSystem 事件：OnEnergyChanged 驱动 fillImage.fillAmount（Vertical/Bottom 填充）和 EnergyText（TMP 数值），OnUltimateReady 高亮按钮并设为可交互，OnUltimateActivated 恢复半透明。未充满时 CanvasGroup.alpha 控制透明度。按钮点击调用 UltimateSystem.ActivateUltimate() |
| `QTEDisplay` (MonoBehaviour) | QTE 指示器管理器（挂载于 Canvas）。`SpawnIndicator(QTEConfig)` 实例化 QTE 指示器 prefab 并设置 anchor → DOTween Scale 脉冲预警动画。`ShowQTEResult(indicator, success)` 显示成功/失败特效 + 指示器缩小消失。`ClearAllIndicators()` 清理所有活跃指示器。indicatorParent（指示器父 RectTransform）、默认 prefab、结果特效 prefab 均可 Inspector 配置 |

## 公开接口

**BattleHUD**：
- `OnRestartButton()`, `OnMainMenuButton()` — 连线到 `StageController`
- 全部 UI 元素引用的序列化字段

**CameraManager**：
- `PlayDeparture()` — 开始推入 + 场景加载
- `PlayArrival()` — 开始拉出动画
- `SetBlur(float t)` — 直接控制模糊程度
- 静态：`IsArriving` (bool)

**DamageNumber**：
- `Show(Vector3 worldPos, float damage)`
- `ResetNumber()`
- `OnReturnToPool` (Action 回调)

**MainMenuUI**：
- `OnStartGame()`, `OnQuitGame()`
- `coinText` (TMP_Text) — 显示总铜钱

**CoinCounterUI**：
- 序列化字段：`coinIcon` (Image), `totalText` (TMP_Text), `floatTextAnchor` (Transform)
- Inspector 可配参数：`floatTextColor`, `floatTextFontSize`, `floatTextRectSize`, `floatUpDistance`, `floatDuration`, `iconPunchScale`, `iconPunchDuration`, `totalPunchScale`, `totalPunchDuration`

**PingPongAnim**：
- 序列化字段：`frames[]`, `fps`, `playOnStart`, `blinkChancePerSecond`

**ChargeIndicatorController**：
- 序列化字段：`indicatorRoot`, `chargeFillImage`, `chargeSpinImage`, `appearThreshold`, `spinSpeed`

**EnemyHealthBar**：
- `Show(float percent)` — 显示血量百分比条
- 序列化字段：`barWidth`, `barHeight`, `yOffset`, `displayDuration`, `highColor`, `lowColor`, `lowThreshold`

## 依赖模块

- **BattleHUD**：`PlayerState`, `WaveSpawner`, `StageController`, `AttackType`, `StageState`, `UnityEngine.UI`, `TMPro`
- **CameraManager**：Unity `SceneManager`, 自定义 shader `Hidden/BlurEffect`
- **DamageNumber**：`TMPro`, `DOTween`
- **MainMenuUI**：`StageConfigManager`（关卡列表）, `SaveManager`, `CameraManager`, Unity `SceneManager`, `UnityEditor`（条件编译）。4个按钮 public 字段：`newGameButton`/`continueButton`/`deleteSaveButton`/`quitButton`；coinText 字段
- **CoinCounterUI**：`PlayerState`（`OnCoinGained` 事件）, `DOTween`, `TMPro`
- **PingPongAnim**：`SpriteRenderer`, `UnityEngine.UI.Image`
- **ChargeIndicatorController**：`InputManager`（`OnChargeBegan/Updated/Ended` 事件）, `PlayerState`（`OnPlayerDied`）, `UnityEngine.UI`
- **EnemyHealthBar**：无外部代码依赖（纯程序化 Mesh + Shader）；由 `Enemy` 内部创建和管理
- **UltimateButtonUI**：依赖 `UltimateSystem` singleton 事件（OnEnergyChanged/OnUltimateReady/OnUltimateActivated），使用 CanvasGroup + Image.Filled/Vertical + TMP

## 重要规则

- **BattleHUD 冷却显示**：两层视觉 — `cooldownImage.fillAmount`（冷却进度 0->1，冷却中红色，就绪绿色）和 `chargeFill.fillAmount`（冷却进度 1->0，径向 360 填充）
- **CameraManager 跨场景**：使用 `static IsArriving` 通知下一场景的 CameraManager 在 Start 时播放 Arrival。Departure 场景在加载前设置 `IsArriving = true`
- **DamageNumber 池化**：由 `DamageNumberManager` 通过 `OnReturnToPool` 回调外部管理。池根节点是管理器的子对象
- **PingPongAnim 眨眼**：每帧随机触发（`Random.value < blinkChancePerSecond * deltaTime`），至少需要 4 帧
- **ChargeIndicatorController**：独立于 `BattleHUD`，直接订阅 `InputManager` 和 `PlayerState` 事件。蓄力进度映射：`fillAmount = (progress - appearThreshold) / (1 - appearThreshold)`
- **EnemyHealthBar**：由 `Enemy` 在首次 `Show()` 时通过 `GetComponent/AddComponent` 延迟创建。血条 GameObject 不挂载为敌人子物体，避免攻击翻转时继承缩放。每帧 `Update()` 跟随敌人世界位置。材质实例采用显式 `new Material()` + `fillRenderer.material = instance` 管理，不依赖 Unity 内部缓存（disable/enable 后可能重建实例）

- **CoinCounterUI 飘字**：每次获得铜钱生成临时 TMP GameObject（`overflowMode = Overflow` 防裁剪，`rectSize` 可配），用 DOTween.Sequence 做上浮+淡出，OnComplete 销毁。FloatAnchor 空 GameObject 控制飘字起始位置，Inspector 中拖动调整。
- **铜钱显示数据源**：Battle 中 CoinCounterUI 显示 `PlayerState.coinCount`（本局）；MainMenu 中 CoinDisplay 显示 `SaveManager.Load().coinCount`（总持有）。两者数据源不同，不可混用。

## 扩展指南

- **新 HUD 元素**：在 `BattleHUD` 中添加序列化字段 + 更新方法，在 `Start()` 中订阅对应 `PlayerState` 事件
- **新场景转场效果**：修改 `CameraManager` 协程或添加新 shader pass。当前模糊使用 3-pass 降采样管线（临时 RenderTexture）
- **新伤害跳字样式**：配置 `DamageNumber` 序列化字段（颜色、大小、描边）或创建变体预制体
- **新指示器样式**：修改 `ChargeIndicatorController` 的精灵图片或动画参数（`appearThreshold`, `spinSpeed`）
- **血条自定义**：修改 `EnemyHealthBar` 的颜色阈值（`lowThreshold`）、显示时长（`displayDuration`）或尺寸参数
- **大招按钮样式**：修改 `UltimateButtonUI` 的 `inactiveAlpha`、`readyColor`，或替换 Fill Image 的精灵/颜色
- **血条颜色控制**：`BattleHUD.SetHealthBarColor()` 保存原色后设置新色；`ResetHealthBarColor()` 恢复。狂怒 Ult 期间变橙，结束后自动恢复
