---
id: kd_3a8110f3-a442-482f-b771-a872c976f637
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# N1 远景布景交接（2026-10-08）

> 本文是下一轮 N1 场景工作的可信入口。用户确认当前近景“好了不少”，并要求下一步完善远景内容。本轮仅制作交接文档，没有修改 Unity 场景。
>
> 编辑器已在交接前断开。下文中的 Unity 运行时状态均标记为“断开前最后一次实际回读”，新会话必须重新连接并核对，不能把旧状态当作当前 live 状态。

## 1. 断开前可信现场

- Git branch：`route-scroll-movement`
- HEAD：`f8cd9a24`（`docs(知识库): BUG/需求滚动登记入口、N1 与骑兵交接文档、里程碑权威化`）
- Unity 最近报告：`disconnected`
- 断开前最后一次 Unity 完整验证：Edit Mode；Active Scene=`Assets/Scenes/Battle.scene`；Additive Scene=`Assets/Experiments/CurvedScroll/YJunctionSample.unity`
- `Assets/Experiments/CurvedScroll/FakeRouteDataTrial.unity` 未加载；禁止打开、保存、回退或向其部署。
- 断开前 Battle/Y 均为 `dirty=false`；退出 Play Mode 后临时隐藏的 Battle 对象已通过从磁盘重载 Battle 恢复，Battle 未保存。
- 断开前 Console warning/error=0。
- Y scene `YScrollSample.scenery=74`；空引用=0、重复引用=0、74 个 Renderer 均 `sortingLayer=Default`、`sortingOrder=0`。
- `Assets/Experiments/CurvedScroll/YJunctionSample.unity` SHA-256=`e3a1edf3d5211aef3839e26005954eba596dc91cf9b82fbcc76047f9718b4e75`；与 HEAD 文件一致，场景无未提交差异。
- 当前 worktree 另有不相关用户/工具文件：`NUL`、`grep.exe.stackdump`、`pelican-bicycle.html`、`pelican-cycling-new.html`、`pelican_bicycle_animation.html`。不要处理或清理它们。

## 2. 本轮已经落盘的近景与中景

用户指出问题在比例和部署位置关系，不是缺少更多主体土堆。近景现已按此方向调整：

- 两辆破车继续是近景主体，保留用户确认的下沉值：
  - `N1_BrokenCart_Left_01` localPosition=`(-3.60, -0.70, 4.20)`，localScale=`(0.36, 0.36, 0.36)`。
  - `N1_BrokenCart_Right_01` localPosition=`(3.70, -0.50, 5.20)`，localScale=`(0.34, 0.34, 0.34)`。
- 已验收的 v4 土堆只用于左右近景低矮承托，不再扩展到远侧 SideBand：
  - Sprite：`Assets/Experiments/CurvedScroll/Art/EnvironmentV2/Props/N1/N1_LowEarthMound_ColorV4.png`
  - `N1_Mid_Mound_Left_Mid` localPosition=`(-4.35, 0.00, 6.15)`，localScale=`(0.20, 0.20, 0.20)`。
  - `N1_Mid_Mound_Right_Near` localPosition=`(4.35, 0.00, 6.95)`，localScale=`(0.19, 0.19, 0.19)`。
- 之前误将 5 个远侧旧土堆槽位替换成 v4 的做法已经撤回：`N1_SideBand_Left_03`、`N1_SideBand_Right_02`、`N1_Mid_Mound_Left_Far2`、`N1_SideBand_Left_06`、`N1_SideBand_Right_07` 均恢复旧 ReinforcedMound Sprite 和原始 Scale。不得重新把 v4 批量部署到 SideBand。
- 近中景已增加 20 个现有素材组合对象，按层次分为：
  - `N1_InnerEdge_*`：6 个，围绕破车内侧补充草、破盾、断木。
  - `N1_Transition_*`：8 个，约 z=8–15 的断木、草簇、木桩/拒马和破盾组合。
  - `N1_MidScatter_*`：6 个，约 z=17–25 的草簇、断木、木桩散点。
- 新草簇使用 `Assets/Experiments/CurvedScroll/Authoring/YGrass.mat`；普通道具使用 `Assets/Experiments/CurvedScroll/Authoring/YSampleScenery_v2.mat`。
- 断开前最后一张纯场景 Game 截图：`Library/Locus/Screenshots/locus_game_20261007_171546_795.png`（253×505）。该图显示近景车组仍是主视觉，草/碎片/木桩已连接到中景，中央道路保持开阔。

## 3. 当前下一步：只完善远景内容

用户最新方向：**近景已好了不少，下一步是完善远景内容。** 不要重新扩大近景土堆或继续往近景叠对象。

### 当前远/中远景现有内容

`Opening Roadside Props` 已有以下可复用对象族：

- 低矮路障与地标：`N1_PalisadeShort_Left_01/Right_01`、`N1_SmallBrokenFlag_Left_01/Right_01`。
- 中景设施：`N1_Mid_Rampart_*`、`N1_Mid_Timber_*`、`N1_Mid_Ruins_*`。
- 远侧连续分布组：`N1_SideBand_Left_01..08`、`N1_SideBand_Right_01..08`。
- 地面植被：`N1_GrassMedium_Left/Right_02..04`。
- 场景远景背景由 `Skybox Background - editable` 和 `N1_FarBackground_v1` 承担，不属于普通 Sprite scenery。

主要现有素材：

- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/Props/N1/N1_PalisadeShort_v1.png`
- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/Props/N1/N1_SmallBrokenFlag_v1.png`
- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/Props/N1/N1_MidgroundDistantRuins_v1_clean.png`
- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/Props/N1/N1_MidgroundEarthenRampart_v1_clean.png`
- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/Props/N1/N1_MidgroundTimberBarricade_v1_clean.png`
- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/Props/N1/N1_MidgroundReinforcedMound_v1_clean.png`

### 建议的远景处理方式

下一轮先重排和疏密调整现有中远景，而不是生成资产或批量增加对象：

1. 先用 Battle Main Camera 捕获当前纯场景基线，分析当前地平线侧带在屏幕上的实际占位；不要只用世界 Z 推断可见布局。
2. 重点检查约 z=28–70 的 `SideBand`、Rampart、Timber、Ruins、Palisade 和 Flag，找出同一屏幕深度上重复、相邻且形成水平连续墙的物件。
3. 通过左右错位、z 深度间隔和尺寸层级形成远景轮廓：稀疏断墙/木栅作为主形，少量旗帜/残垣作识别点；保留开阔地平线与中央道路。
4. 远景物件对比度和尺寸应低于近景破车与拒马；优先复用当前素材，不要让远景出现与近景同等体量的孤立大土堆。
5. 每轮只改明确的一组对象，Play Mode 用 Battle Main Camera 验收后，仅保存 Y scene；关闭/重载 Y 再回读对象、Sprite、Scale、scenery 和 dirty 状态。

YScrollScenery shader 对路线深度执行淡出，约在深度 70–80 之间逐步淡出。新远景对象应优先落在淡出区之前；如果希望物件承担真正的远背景职责，应使用独立背景层，不要盲目加入普通 `scenery`。

## 4. 不可变边界

后续 N1 远景美术任务不得：

- 修改 `Battle.scene`、Battle Main Camera、Player、Enemy、HUD、StageController 或战斗坐标；
- 修改 `YScrollSample.Evaluate()`、`Length`、junction、radius、turnAngle 或路线运动；
- 重新启用 `N1 Distant City Wall Landmark`、城墙背景或 `N1_FarBackground_WithCityWall_v1.png`；当前 N1 明确保持无城墙远景；
- 将远景背景加入普通 `YScrollSample.scenery`；
- 批量删除/禁用名称含 `Grass` 的对象；
- 将所有 `SideBand` 替换成 v4 土堆；
- 通过调整车的 Y、改 Pivot 或 Shader 遮盖用户确认的破车下沉；
- 整文件 Git restore、reset 或覆盖 Y scene；
- 把运行时临时试摆或截图当作已保存结果。

## 5. 重连后的验收流程

1. 确认 Unity 已连接并处于 Edit Mode；Active Scene 是 Battle，Additive Y scene 已加载，FakeRoute 未加载。
2. 重新读取 Y scene live state，确认 Battle/Y dirty；不要因本交接中的历史状态跳过检查。
3. Play Mode 使用 Battle Main Camera 捕获纯场景图；临时隐藏 HUD/Player/Enemies/Host 和运行时特效时，不保存 Battle，并在退出 Play Mode 后确认已恢复。
4. 远景改动完成后检查：Y scenery 引用无空项/重复、普通 Renderer 全部 `sortingLayer=Default` 与 `sortingOrder=0`、中央道路无遮挡、近景比例未改变。
5. 仅保存 `Assets/Experiments/CurvedScroll/YJunctionSample.unity`，关闭并重载它；Battle 不保存。
6. 最终记录 Game 截图路径、Y scene dirty=false、Battle dirty=false、Console warning/error 和对象层级结果。

## 6. 当前版本基线

- Y scene：`Assets/Experiments/CurvedScroll/YJunctionSample.unity`
- HEAD：`f8cd9a24`
- 当前 Y scene SHA-256：`e3a1edf3d5211aef3839e26005954eba596dc91cf9b82fbcc76047f9718b4e75`
- 当前场景对象名搜索确认：`N1_Transition_*` 8 个，`N1_MidScatter_*` 6 个，`N1_InnerEdge_*` 6 个。
- Git worktree 中 Y scene 无未提交差异；本交接文档是本次新建的知识库文档。
- Unity 当前已断开，不能声称重新连接后的状态已经验证。
