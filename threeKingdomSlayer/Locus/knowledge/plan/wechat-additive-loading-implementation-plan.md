---
id: kd_56a9bbe7-1093-44a9-9a6b-aa2652259744
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 微信小游戏加载优化实施交接

> **2026-10-17 修订：** 本文件的「首轮全 Additive + Bootstrap」已被 `plan/scroll-on-demand-loading-plan.md` 降级为最后一步（Step 4，可选项）。当前优先级是按距离窗口激活 → 段 prefab 按需实例化 → 才轮到场景架构。
> 仍然有效的部分：禁止 Single Load 打断动画、离场动画固定时长、预热可中断、不做菜单期全关卡预加载、本轮不承诺包体。
> 若日后实施，须补两项该文档未覆盖的内容：MainMenu 与 Battle 各有 1 个 AudioListener / EventSystem 需显式停用；`FakeRouteStageConfig` 缺路线场景字段，需先补 `SceneAsset` 引用。

## 目标与边界

主菜单进入长坂坡时，离场动画固定播完，加载完成也不缩短。动画期间保持可连续渲染。动画结束时如果目标层已准备好就进入；没准备好就停在模糊末帧，不能中途切场景。

本轮只改加载架构，不改路线长度、节点距离、布景、战斗数值或包体资源。

当前证据：

- `CameraManager.DepartureRoutine()` 播完后调用 `SceneManager.LoadSceneAsync(nextScene)`，默认是 Single。
- 菜单场景序列化值为 `duration=1.5`、`nextScene=Battle`。
- `MainMenuUI.StartBattle()` 在没有 CameraManager 时使用同步 `LoadScene("Battle")`。
- `StageController.GoToBattleScene()` 和回菜单也使用同步 Single Load。
- `BattleYRouteHost.Start()` 在 Battle 激活后才 Additive 加载 `Assets/Experiments/CurvedScroll/YJunctionSample.unity`。
- 三个场景都已在 Build Settings：MainMenu、Battle、YJunctionSample。

## 场景架构

新增 `Assets/Scenes/Bootstrap.scene`，设为 Build Settings 第一项。它只放一个 `GameFlowLoader` 和常驻基础对象，不放 UI、敌人或路线景物。

所有业务场景都改为 Additive：

- Bootstrap：永不卸载。
- MainMenu、以后的养成/商店：界面层，同时只激活一个。
- Battle：战斗壳层。
- YJunctionSample：当前唯一路线层。

禁止再用 `LoadScene` 或默认 Single 的 `LoadSceneAsync` 切换 MainMenu 与 Battle。Single 会卸掉已经预加载的 Additive 层，微信小游戏上会造成重复加载。

## 加载状态

`GameFlowLoader` 使用 `DontDestroyOnLoad`，记录：

- 当前界面层。
- 当前选中的 `FakeRouteStageConfig`。
- Battle AsyncOperation。
- 当前路线场景 AsyncOperation。
- 是否已经完成本关首屏预热。

玩家只是浏览菜单时不加载关卡。只有 `OnNewGame` 或 `OnContinueGame` 已确定关卡后才开始预加载。更换关卡时取消并卸载上一关，不保留多个路线场景。

## 首轮时序

1. Bootstrap 加载 MainMenu Additive，并激活 MainMenu。
2. 玩家确定关卡后，后台开始加载 Battle 和 Y 路线场景，两者都先 `allowSceneActivation=false`。
3. 玩家点击开始后，现有离场动画固定运行 1.5 秒。动画代码不得读取加载进度来提前结束。
4. 在动画运行期间，加载操作可以继续；完成激活和首屏预热也在这段时间进行。每帧只处理有限数量的场景根对象，避免一次激活 704 个 Renderer。
5. 动画自然结束后：
   - 已准备好：卸载 MainMenu，启用 Battle 相机，开始现有路线流程。
   - 未准备好：背景停在动画最后一帧和最大模糊，继续等待。准备好后执行同一切换。
6. Battle 到达动画只在切换完成后播放，不替代离场动画。

## 具体代码边界

- `CameraManager` 不再直接调用场景加载。它只负责固定时长动画，并提供“播放完成”事件。
- `MainMenuUI.StartBattle()` 改为通知 `GameFlowLoader`，删除同步 `LoadScene("Battle")` 后备路径。
- `StageController.GoToBattleScene()` 改为走同一个 Loader。
- `GoToMainMenu()` 和 `BattleHUD` 的回菜单改为 Additive 加载菜单、卸载 Battle 和路线层。
- `BattleYRouteHost` 保留路线绑定逻辑，但删除自己的 `LoadSceneAsync`。它等待 Loader 提供已经加载的路线场景。
- `routeSceneName` 继续从关卡配置读取，不在 Host 内写死未来关卡。

## 首屏预热

不要调用 `Shader.WarmupAllShaders`。只在隐藏的小 RenderTexture 上用 Battle 相机渲染一次当前路线起点，以编译当前卷轴 Shader。

临时提高 `QualitySettings.asyncUploadTimeSlice` 与 `asyncUploadBufferSize`，切换完成后恢复原值。

预热必须可中断。如果玩家在离场动画中返回或切换关卡，停止激活和预热，不进入 Battle。

## 微信小游戏限制

微信小游戏没有可用的 Unity 后台加载线程，AsyncOperation 也不能保证动画期间不卡。因此：

- 不在菜单打开时预加载全部关卡。
- 不把未来养成、商店和完整路线同时留在内存。
- 加载停滞时保持模糊末帧，不允许 Single 切换打断动画。
- 首轮仍加载整张当前路线场景，因为还没有按距离拆分。

## 后续扩展，不在首轮实现

当路线继续变大或多关卡正式接入时，把 Y 场景按距离拆成 Additive 小层：开场、E0、J1、左村、右路。首屏只激活当前段和下一段，行进时提前加载，离开后卸载。

养成、背包、商店也必须作为 Bootstrap 下的 Additive 界面层。打开它们时卸载菜单可视内容，但保留当前关卡加载状态；只有更换关卡才卸载路线层。

## 验收

一次从主菜单进入当前长坂坡：

- 离场动画时长仍为 1.5 秒，加载提前完成也不缩短。
- 动画帧不完全冻结；记录最差帧，不得用总等待时间掩盖。
- 动画结束后不存在第二次由 `BattleYRouteHost` 引发的长加载。
- 返回菜单使用 Additive 流程，不再调用 Single Load。
- 路线进度、节点距离、EV/N3 布局和战斗对象坐标与改动前一致。
- Console 无新增 warning/error。

本轮不测量或承诺包体缩小。
