# UI 模块

## 模块名称
UI（用户界面与特效）

## 主要职责
HUD 显示（血量、复活、击杀、金币、波次、冷却指示器、胜利/失败面板），带模糊效果的场景转场动画，浮动伤害跳字，主菜单（选关、存档、新游戏/继续），精灵序列动画。

## 核心类

| 类 | 说明 |
|---|---|
| `BattleHUD` (MonoBehaviour) | 战斗 UI 绑定。订阅全部 `PlayerState` 事件。更新血条 Slider+Text、复活显示、击杀数、金币数、波次进度。每帧冷却 UI：Image 的 `fillAmount` 径向冷却 + 各技能图标独立的充能填充 Image。显示胜/负面板及金币统计。重开和主菜单按钮。`OnMainMenuButton()` 带 fallback——若 StageController.Instance 为 null 则直接 SceneManager.LoadScene。 |
| `CameraManager` (MonoBehaviour) | 挂载于相机的场景转场：背景 RectTransform 缩放 + 位置动画 + `OnRenderImage` 模糊效果（使用自定义 shader `Hidden/BlurEffect`）。`PlayDeparture()` 动画结束后加载下一场景。`PlayArrival()` 场景启动时动画。用静态 `IsArriving` 标志实现跨场景交接。 |
| `DamageNumber` (MonoBehaviour) | 浮动伤害跳字（TextMeshPro）。`Show(Vector3 worldPos, float damage)` 启动 DOTween 上浮 + 淡出。完成后调用 `OnReturnToPool` 回调返还 `DamageNumberManager`。配置：红色文字、黑色描边、粗体。 |
| `MainMenuUI` (MonoBehaviour) | 主菜单：4 个按钮预置于场景 Hierarchy（NewGame/Continue/DeleteSave/Quit），带持久化 onClick。选关网格根据 `StageConfigManager.stages` 自动生成横向排列关卡按钮，显示 [已通关]/[可挑战]/[未解锁] 状态。`RefreshUI()` 根据 `SaveManager.HasSave` 切换按钮可见性。调用 `StageController.PendingStageConfig` 传递关卡配置。 |
| `PingPongAnim` (MonoBehaviour) | 精灵序列动画器。两种模式：idle ping-pong（帧 [0,1] 间来回），随机触发的眨眼播放完整帧序列。同时支持 `SpriteRenderer` 和 UI `Image`。可配置 FPS 和眨眼概率。 |

## 公开接口

**BattleHUD**：
- `OnRestartButton()`, `OnMainMenuButton()` — 连线到 `StageController`
- `SetHealthBarColor(Color)`, `ResetHealthBarColor()` — 狂怒大招血量条变色
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
- `OnNewGame()` — 删除存档，从第一关开始
- `OnContinueGame()` — 从第一个未通关关卡继续
- `OnDeleteSave()` — 删除存档，重建选关网格
- `OnQuitGame()` — Editor `isPlaying=false` / 独立版 `Application.Quit()`

**PingPongAnim**：
- 序列化字段：`frames[]`, `fps`, `playOnStart`, `blinkChancePerSecond`

## 依赖模块

- **BattleHUD**：`PlayerState`, `WaveSpawner`, `StageController`, `AttackType`, `StageState`, `UnityEngine.UI`, `TMPro`
- **CameraManager**：Unity `SceneManager`, 自定义 shader `Hidden/BlurEffect`
- **DamageNumber**：`TMPro`, `DOTween`
- **MainMenuUI**：`StageConfigManager`, `SaveManager`, `StageController`, `CameraManager`, Unity `SceneManager`, `UnityEditor`（条件编译）
- **PingPongAnim**：`SpriteRenderer`, `UnityEngine.UI.Image`

## 重要规则

- **BattleHUD 冷却显示**：两层视觉 — `cooldownImage.fillAmount`（冷却进度 0->1，冷却中红色，就绪绿色）和 `chargeFill.fillAmount`（冷却进度 1->0，径向 360 填充）
- **CameraManager 跨场景**：使用 `static IsArriving` 通知下一场景的 CameraManager 在 Start 时播放 Arrival。Departure 场景在加载前设置 `IsArriving = true`
- **DamageNumber 池化**：由 `DamageNumberManager` 通过 `OnReturnToPool` 回调外部管理。池根节点是管理器的子对象
- **PingPongAnim 眨眼**：每帧随机触发（`Random.value < blinkChancePerSecond * deltaTime`），至少需要 4 帧
- **MainMenuUI 按钮可见性**：`RefreshUI()` 在 `Start()` 时调用，根据 `SaveManager.HasSave` 切换——有存档显示 Continue+Delete，无存档显示 NewGame。场景中所有按钮为 pre-placed（active=False 由运行时控制）
- **MainMenuUI 选关按钮**：运行时自动生成于 StageGrid 子对象，横向排列满行换行。解锁状态从 `SaveManager.GetNextAvailableStageId()` 判定
- **BattleHUD 返回主菜单**：`OnMainMenuButton()` 先尝试 `StageController.Instance.GoToMainMenu()`，若 Instance 为 null 则 fallback 直接 `SceneManager.LoadScene("MainMenu")`

## 扩展指南

- **新 HUD 元素**：在 `BattleHUD` 中添加序列化字段 + 更新方法，在 `Start()` 中订阅对应 `PlayerState` 事件
- **新场景转场效果**：修改 `CameraManager` 协程或添加新 shader pass。当前模糊使用 3-pass 降采样管线（临时 RenderTexture）
- **新伤害跳字样式**：配置 `DamageNumber` 序列化字段（颜色、大小、描边）或创建变体预制体
- **新菜单按钮**：在 MainMenu 场景 Canvas 下添加 Button + Text，在 `MainMenuUI` 中添加 `public Button` 字段并在 Inspector 赋值，在 `SetupButtons()` 中添加 onClick 绑定
