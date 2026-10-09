---
id: kd_3a8110f3-a442-482f-b771-a872c976f637
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# N1 远景布景交接（2026-10-08）

> 本文是 N1 远景工作的交接入口。第 1–6 节保留最初交接的历史基线；最新状态以第 7 节“远景第一轮调整已保存”为准。
>
> 用户已确认中、近景“好了不少”，后续重点为远景的比例、位置和层次关系。新会话必须重新核对 Editor live 状态；本轮保存事实不等于之后的实时状态。

## 1. 历史交接现场（远景调整前）

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

## 3. 远景调整前的任务方向

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

## 6. 历史版本基线（远景调整前）

- Y scene：`Assets/Experiments/CurvedScroll/YJunctionSample.unity`
- HEAD：`f8cd9a24`
- 当前 Y scene SHA-256：`e3a1edf3d5211aef3839e26005954eba596dc91cf9b82fbcc76047f9718b4e75`
- 当前场景对象名搜索确认：`N1_Transition_*` 8 个，`N1_MidScatter_*` 6 个，`N1_InnerEdge_*` 6 个。
- Git worktree 中 Y scene 无未提交差异；本交接文档是本次新建的知识库文档。
- 以上为远景调整前的断开状态，已被第 7 节的新 checkpoint 覆盖。

## 7. 最新 checkpoint：远景第一轮调整已保存（2026-10-08）

用户要求“对比参考图，继续丰富远景内容”。本轮复用现有资源，对 10 个中远景对象做位置/尺度调整，新增 6 个稀疏草簇；未生成新素材。近景破车、小土堆和前轮 20 个组合层对象未调整。全部永久修改通过 Edit Mode Unity API 应用，仅保存 Y scene。

### 已保存的对象参数

所有坐标、Scale 均为 `Opening Roadside Props` 下的 local 值；不是运行时 world 坐标。

| 对象 | Local Position | Local Scale |
|---|---|---|
| N1_Mid_Ruins_Left_Far | (-5.60, 0, 32) | (0.78, 0.64, 0.64) |
| N1_Mid_Ruins_Right_Far | (5.80, 0, 34) | (0.76, 0.62, 0.62) |
| N1_Mid_Ruins_Left_Far2 | (-6.80, 0, 44) | (0.62, 0.50, 0.50) |
| N1_Mid_Ruins_Right_Far2 | (6.60, 0, 46) | (0.64, 0.50, 0.50) |
| N1_Mid_Rampart_Left_Far | (-4.90, 0, 37) | (0.36, 0.36, 0.36) |
| N1_Mid_Timber_Right_Far | (4.90, 0, 40) | (0.38, 0.34, 0.34) |
| N1_PalisadeShort_Left_01 | (-5.80, 0, 38) | (0.44, 0.44, 0.44) |
| N1_PalisadeShort_Right_01 | (5.80, 0, 42) | (0.45, 0.45, 0.45) |
| N1_SmallBrokenFlag_Left_01 | (-5.70, 0, 34) | (0.44, 0.44, 0.44) |
| N1_SmallBrokenFlag_Right_01 | (5.80, 0, 38) | (0.40, 0.40, 0.40) |

上述对象继续使用各自原 Sprite 和 `Assets/Experiments/CurvedScroll/Authoring/YSampleScenery_v2.mat`。`SideBand` 的其他对象未改变，不要把本轮描述为已重构所有远景。

新增对象均在 `Assets/Experiments/CurvedScroll/YJunctionSample.unity/Y Junction - select for preview/Opening Roadside Props` 下，使用 `Assets/Experiments/CurvedScroll/Authoring/YGrass.mat`：

| 对象 | Sprite | Local Position | Local Scale |
|---|---|---|---|
| N1_FarScatter_GrassSmall_Left_01 | GrassSmall_v1 | (-5.25, 0, 32.5) | (0.22, 0.14, 0.14) |
| N1_FarScatter_GrassWide_Left_01 | GrassWide_v1 | (-4.85, 0, 39.5) | (0.22, 0.14, 0.14) |
| N1_FarScatter_GrassSmall_Left_02 | GrassSmall_v1 | (-5.85, 0, 48) | (0.18, 0.12, 0.12) |
| N1_FarScatter_GrassSmall_Right_01 | GrassSmall_v1 | (5.25, 0, 35) | (0.22, 0.14, 0.14) |
| N1_FarScatter_GrassWide_Right_01 | GrassWide_v1 | (4.85, 0, 42.5) | (0.21, 0.13, 0.13) |
| N1_FarScatter_GrassSmall_Right_02 | GrassSmall_v1 | (5.90, 0, 50) | (0.18, 0.12, 0.12) |

Sprite 完整路径分别为 `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/Grass/GrassSmall_v1.png` 和 `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/Grass/GrassWide_v1.png`。全部新增 Renderer 已加入 `YScrollSample.scenery`，Sorting Layer=Default，sortingOrder=0。

### 验证与截图边界

- 本轮基线 Game 截图：`Library/Locus/Screenshots/locus_game_20261008_001717_285.png`。
- 首次重排离屏图：`Library/Locus/tmp/n1-far-trial-01.png`（1012×2020）。这是首轮试摆的 camera RenderTexture 结果，不是最终保存后截图。
- 最终 Edit Mode 参数重新应用后、保存前 Play Mode Game 验收图：`Library/Locus/Screenshots/locus_game_20261008_003237_697.png`（253×505）。
- 保存并重载 Y、再重载 Battle 后的最终纯场景图：`Library/Locus/Screenshots/locus_game_20261008_004908_286.png`（253×505）。该图使用 Battle Main Camera，临时隐藏 Battle HUD/玩家/敌人/特效；临时状态未保存。
- 画面观察：两侧残垣、木栅、旗帜与草簇位置有错层，中央道路/地平线开口保留。本轮视觉变化较小，尚未获用户远景验收；不能声称已完整复现参考图。
- 退出 Play Mode 后仅保存并关闭/重载 Y。Battle 未保存，随后重载 Battle 恢复临时状态。
- 最后一次实际回读：Edit Mode；Active=`Assets/Scenes/Battle.scene`；Battle/Y loaded=true、dirty=false。
- `scenery=80`；空引用=0；重复引用=0；sortingOrder=0 的 Renderer=80；非零 order=0。
- `N1 Distant City Wall Landmark` inactive；天空 Sprite 不在 scenery；Edit Mode `viewCamera=None`。
- Console warning/error=0。未验证完整左右分支行进和战斗回归；本轮不作这些完成性声明。

### 最新磁盘基线与续作重点

- Y scene SHA-256：`596f9f5bc0af83eb5abdbf686c907a8473097d379947e28ea2fc4cb983154707`。保存后重载确认未变化。
- Y scene 本轮 diff：560 行增加、20 行删除（包含 6 个新对象和引用）。尚未由本轮提交。
- 当前对象组：Transition=8、MidScatter=6、InnerEdge=6、FarScatter=6。
- 最终保存后 Battle/Y：loaded=true、dirty=false；Active Scene=`Assets/Scenes/Battle.scene`；Console warning/error=0；`scenery=80`、nullRefs=0、duplicates=0、sorting0=80、sortingNonZero=0。
- 新会话先读取本节、当前 Git/live 状态和最终参考图；不要重放旧坐标覆盖新修改。
- 下一步首先由用户评估远景尺度与位置差异，再按真实 Game 构图调整小组。不要继续用增加物件数量代替比例与位置关系；不得扩大近景土堆、开启旧城墙或改变路线/Battle。
