---
id: kd_9dfc6363-421b-400a-9ca0-727a1f6e55a1
type: skill
path: ui-visibility-patterns.md
title: ui-visibility-patterns
inheritInjectMode: true
summaryEnabled: true
commandEnabled: true
readOnly: false
inheritAiConfig: true
skillEnabled: true
skillSurface: command
createdAt: 1779437961778
updatedAt: 1779437961779
---

# ui-visibility-patterns

## Summary
Unity UI 显隐控制的最佳实践：避免 SetActive 自引用、优先使用 Image.color alpha、图片防拉伸、暂停兼容性。

## Content
# Unity UI 显示/隐藏模式

## 核心原则

**永远不要在 Awake/Start 中对自己的 GameObject 调用 SetActive(false)，然后用事件回调中的 SetActive(true) 来显示。** 这在 Play Mode 下有不可靠的边界行为。

## 推荐方案（按优先级排序）

### 1. Image.color Alpha（最安全）

- GameObjects 始终保持 active
- 初始将 Image.color.a 设为 0
- 显示时设为 1
- 只影响目标 Image，绝不影响其他 UI

```csharp
// Awake 中
var c = fillImage.color;
c.a = 0f;
fillImage.color = c;

// 显示
var c = fillImage.color;
c.a = 1f;
fillImage.color = c;
```

### 2. CanvasGroup Alpha（次选）

- 适用于控制一个子树的显隐
- 注意：CanvasGroup 只应加在目标节点上
- 不要在父 Canvas 上误加 CanvasGroup

### 3. GameObject.SetActive（最后手段）

- 仅用于非自身 GameObjects（如子节点、兄弟节点）
- 绝对不要 self.SetActive(false) + self.SetActive(true) 模式

## 常见陷阱

### SetActive 自引用

```csharp
// ❌ 错误：Awake 中关闭自己，回调中激活自己
void Awake() { gameObject.SetActive(false); }
void OnEvent() { gameObject.SetActive(true); }
// 结果：Play Mode 下 SetActive(true) 后 activeSelf 仍为 false
```

### CanvasGroup 泄漏

- 确认 CanvasGroup 挂载在正确的 GameObject 上
- 子节点的 CanvasGroup 理论上不影响父级/兄弟，但实际出现过异常

### 图片拉伸

- 始终设置 `Image.preserveAspect = true`
- RectTransform sizeDelta 应与 sprite 原始比例匹配

### 暂停兼容

- 需要随暂停冻结的系统：使用 `Time.time` / `Time.deltaTime`
- 需要无视暂停运行的系统：使用 `Time.unscaledTime` / `Time.unscaledDeltaTime`

## 场景清理

- 用 `GameObject.Find` + `transform.Find` 检查是否有同名重复节点
- 重复节点会导致事件分发混乱
