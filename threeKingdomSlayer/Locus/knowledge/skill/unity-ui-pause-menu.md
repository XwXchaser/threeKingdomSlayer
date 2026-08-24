---
id: kd_7627d603-3c45-49c8-ae3a-b8878cb2e6bb
injectMode: inherit
aiEditMode: inherit
skillEnabled: true
skillSurface: command
---

# unity-ui-pause-menu

## Summary
Unity UI 暂停菜单开发的常见陷阱和规范：组件挂载规则、点击穿透防护、raycastTarget 配置、SetActive 时序、EventSystem 重复检测

## Content
## 暂停菜单实现指南

### 核心模式

暂停通过 `Time.timeScale = 0f` 实现。所有需要响应暂停的 `Update()` 方法顶部需加：

```csharp
if (Time.timeScale == 0f) return;
```

### 组件挂载规则（血的教训）

**一个功能组件只挂一个 GameObject。** 不要将同一个 MonoBehaviour 挂在父子关系的两个不同 GameObject 上。

如果子节点有同名组件且引用自身为子节点（如 PauseMenuUI 挂在 PausePanel 上，且 pausePanel 字段指向自己），会导致：
- `Start()` 中 `pausePanel.SetActive(false)` 把自己 deactivate
- `OnPauseClicked()` 中 `pausePanel.SetActive(true)` 把自己 activate → `Start()` 再次运行 → 再次自我 deactivate
- 循环自毁，面板永远无法显示

**排查方法：** 在 Play Mode 用 `FindObjectsOfType<T>()` 检查是否存在意外重复组件。

### 点击穿透防护

游戏输入系统（InputManager）必须在处理鼠标/触摸前检查是否点击在 UI 上：

```csharp
using UnityEngine.EventSystems;

// 在 HandleMouseInput 的 GetMouseButtonDown(0) 内：
if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
    return;
```

### 全屏 UI 面板的 raycastTarget

全屏背景 Image 的 `raycastTarget` 应设为 `true`（阻止点击穿透到游戏层），但注意不要让它遮挡按钮。子按钮需有更高的 siblingIndex 以确保渲染和射线检测顺序正确。

### 暂停按钮自身防护

暂停按钮对应的逻辑（如大招按钮）也需检查 timeScale：

```csharp
private void OnButtonClick()
{
    if (Time.timeScale == 0f) return; // 暂停时不触发
    // ...
}
```

### SetActive 时序

先 `SetActive(true)` 显示面板，再设置 `Time.timeScale = 0`。反之则面板可能不渲染。

### 双 EventSystem 检查

场景中应只有一个 EventSystem。多个 EventSystem 会导致 UI 事件被处理两次。
