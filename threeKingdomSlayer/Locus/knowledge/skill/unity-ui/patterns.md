---
id: kd_b8708ea3-19ef-4786-8856-8ffafa2c0b38
injectMode: inherit
summary: 在本项目实现或修改 Unity UI 时使用：显隐控制、暂停与模态面板、世界空间头顶血条、程序化 UI 布局四类模式与反模式。
aiEditMode: inherit
skillEnabled: true
skillSurface: command
---

# Unity UI 实现模式（显隐 · 暂停 · 世界空间血条 · 程序化布局）

适用：在本项目写 UI 显隐逻辑、暂停/模态面板、世界空间血条，或用代码生成 UI 时。原先四篇分散文档（显隐模式、暂停菜单、头顶血条、程序化 UI 原则）已合并到本文。

## 1. 显隐控制

- 绝不在 `Awake`/`Start` 里对自己调用 `SetActive(false)`，再用事件回调 `SetActive(true)` 显示——Play Mode 下会出现 `activeSelf` 仍为 false 的边界行为。
- 优先级：① `Image.color.a`（只影响目标 Image，最安全）② `CanvasGroup.alpha`（控制子树；只挂在目标节点，不要误加到父 Canvas）③ `GameObject.SetActive`（仅用于非自身对象）。
- 图片不拉伸：`Image.preserveAspect = true`，`RectTransform.sizeDelta` 与 sprite 原始比例一致。
- 暂停兼容：随暂停冻结的系统用 `Time.time` / `Time.deltaTime`；无视暂停的系统用 `Time.unscaledTime` / `Time.unscaledDeltaTime`。
- 场景里不要留同名重复节点（用 `GameObject.Find` + `transform.Find` 检查），重复节点会导致事件分发混乱。

## 2. 暂停与模态面板

- 暂停用 `Time.timeScale = 0f`；所有需响应暂停的 `Update()` 顶部加 `if (Time.timeScale == 0f) return;`。
- 一个功能组件只挂一个 GameObject：父子节点各挂一份、且字段指向自己，会造成 "`Start()` 自我 deactivate → 点击后 activate → `Start()` 再自我 deactivate" 的自毁循环，面板永远显示不出来。排查用 Play Mode 下 `FindObjectsOfType<T>()` 检查重复组件。
- 点击穿透防护：游戏输入在处理鼠标/触摸前先判断 `EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()` 并直接 return。
- 全屏背景 Image 的 `raycastTarget` 设为 `true` 防穿透，但子按钮需要更高的 siblingIndex 才能正常响应。
- 暂停期间需要屏蔽的逻辑（例如大招按钮）同样加 `Time.timeScale == 0` 检查。
- 时序：先 `SetActive(true)` 显示面板，再设置 `Time.timeScale = 0`；顺序反了面板可能不渲染。
- 场景中只保留一个 EventSystem，多个会导致 UI 事件被处理两次。

## 3. 世界空间头顶血条

- 适用场景：3D/2D 战场血条、不跟随角色翻转、需兼容对象池、BIRP。
- 推荐方案：程序化 Quad Mesh + `Shader.Find("Unlit/Color")`（或 `Sprites/Default`）+ **根级独立定位**——血条根对象不挂为角色子物体，每帧手动设置 `position` 跟随，因此不受敌人 scale/rotation，也不受 DOTween `DOScaleX` 翻转影响。
- 不推荐：`SpriteRenderer`（与 Canvas 排序/层级冲突，易被 UI 背景遮挡）；每个敌人一个 World Space Canvas（开销大，且仍可能被 ScreenSpaceCamera Canvas 覆盖）；子物体反转 scale（与 DOTween 竞争，逐帧符号判断不可靠）。
- Inspector 可配参数：`barWidth` / `barHeight`、`yOffset`（0 = 按 `SpriteRenderer.sprite.bounds` 自动）、`displayDuration`、`highColor` / `lowColor` + `lowThreshold`。
- 对象池兼容：`Awake()` 创建共享静态材质；`EnsureCreated()` 延迟创建子对象避免复用重复创建；`OnDisable()` 隐藏；`OnDestroy()` 销毁血条根对象。
- 隐藏计时用手动 `hideTimer -= Time.deltaTime`（零分配），不必上 DOTween。
- 已知限制：Quad 不面向透视相机时需额外 billboard 处理；MeshRenderer 的 layer / cullingMask 不能被摄像机裁掉。

## 4. 程序化 UI 布局原则

核心问题：代码动态调整 UI 时，容易覆写设计师在 prefab 中手动调好的布局。根因是没有区分"用户设置的静态布局"和"代码驱动的动态值"。

1. **静态布局归 prefab，动态数据归代码**：位置/大小/锚点由设计师在 prefab 调整，代码只读不写；滚动偏移、动态数量、颜色变化以 prefab 值为基准做相对计算。反例：`_contentRect.anchoredPosition = new Vector2(-windowStart * dotSpacing, y)` 用绝对值覆写了设计师设的 X。
2. **必须控制位置时用"基准偏移"**：首次读取 `_originX = rect.anchoredPosition.x`，运行时 `rect.anchoredPosition = new Vector2(_originX - scrollOffset, rect.anchoredPosition.y)`。不要用 `Vector2.zero` 或硬编码绝对值做初始位置。
3. **Edit Mode 预览只重建叶子**：结构节点（Frame、Content 容器）首次创建后序列化进 prefab，后续 `BuildVisuals` 用 `transform.Find()` 复用且不改 RectTransform；叶子预览元素（Line、Node、PlayerDot）标 `HideFlags.DontSave | HideFlags.NotEditable`，重建时销毁；RectMask2D 同样给 `PreviewFlags`；预览触发用 `[ExecuteAlways]` + Update 脏标记兜底（`OnEnable` 在 Prefab Stage 打开时可能不触发）。

反模式（应避免）：`MainMenuUI` 硬编码 anchor 构建 StageGrid；`KillRewardUI` 实例化 prefab 后立即覆写 anchor/position；`QTEDisplay` 的 Ghost 子节点固定全拉伸、没有设计师调整入口。
