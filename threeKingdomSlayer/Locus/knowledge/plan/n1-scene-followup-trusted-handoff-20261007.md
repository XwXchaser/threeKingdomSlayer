---
id: kd_0d24b0c9-1d8e-4188-999e-4633ee27a6eb
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# N1 场景后续可信交接（2026-10-07）

> 本文是当前 N1 场景工作的可信入口，覆盖并更新 `plan/n1-scene-redeploy-handoff-20261007.md` 中已经过时的坐标、试摆和生成任务描述。
>
> 本文只记录已由 Unity/file 工具实际回读的事实。未落盘的运行时试摆、未实际生成或未保存到本地的图片，不视为交付结果。

## 1. 当前可信现场

- Git branch：`route-scroll-movement`
- Active Scene：`Assets/Scenes/Battle.scene`
- Additive Scene：`Assets/Experiments/CurvedScroll/YJunctionSample.unity`
- `Assets/Experiments/CurvedScroll/FakeRouteDataTrial.unity`：未加载；禁止打开、保存、回退或向其部署。
- Unity 当前：Edit Mode
- Battle scene：loaded，`dirty=false`
- Y scene：loaded，`dirty=false`
- Console：error/warning=0
- `YScrollSample.scenery`：52 项
- scenery 空引用：0
- scenery 重复引用：0
- 52 个 scenery Renderer 当前全部 `sortingLayer=Default`、`sortingOrder=0`
- Edit Mode 下 `YScrollSample.viewCamera=None`：正常；Play Mode 由 `BattleYRouteHost` 绑定 Battle Main Camera。

当前正式 Y 场景 SHA-256：

```text
5392cef48c951078292e13711515083ad57773d495b15e637b96f2e981c7fdfb
```

工作区与 Git HEAD 的关系：

```text
M Assets/Experiments/CurvedScroll/YJunctionSample.unity
```

不要因为场景 diff 较大而整文件 `git restore`、`reset` 或覆盖。工作区存在其他历史/用户改动，后续只处理明确授权的 Y 场景局部修改。

## 2. 正式职责边界

### Battle.scene 负责

- Player、Enemy、EnemyPool、StageController；
- Battle Main Camera；
- HUD、QTE、投射物、真实战斗和奖励；
- `BattleYRouteHost`。

### YJunctionSample.unity 负责

- Y 形道路与三层地面；
- 天空和远景；
- N1 路侧 Sprite、中景、近景和草簇；
- Y 路线表现数据和 `YScrollSample.scenery`。

后续 N1 美术任务不得：

- 修改 Battle Camera、Player、Enemy、HUD、StageController 或战斗坐标；
- 修改 `YScrollSample.Evaluate()`、`Length`、junction、radius、turnAngle 或路线运动；
- 修改 `Battle.scene` 来适配景物；
- 重新启用旧城墙背景或 `N1 Distant City Wall Landmark`；
- 打开、保存、回退或向 `FakeRouteDataTrial.unity` 部署；
- 批量删除名称含 `Grass` 的对象；
- 使用 `Enemy.visualYOffset` 掩盖景物脚点；
- 用整文件 Git restore 覆盖当前 Y 场景。

## 3. 当前已保存的 N1 布局事实

### 3.1 用户确认保留的破车下沉

用户在最终场景中手动调整了两辆破车的 Y 值，后续必须保留：

```text
N1_BrokenCart_Left_01
localPosition=(-3.60, -0.70, 4.20)
localScale=(0.36, 0.36, 0.36)

N1_BrokenCart_Right_01
localPosition=(3.70, -0.50, 5.20)
localScale=(0.34, 0.34, 0.34)
```

对象锚点：

```text
Assets/Experiments/CurvedScroll/YJunctionSample.unity/Y Junction - select for preview/Opening Roadside Props/N1_BrokenCart_Left_01
Assets/Experiments/CurvedScroll/YJunctionSample.unity/Y Junction - select for preview/Opening Roadside Props/N1_BrokenCart_Right_01
```

这两个 Y 值用于表达车辆陷入黄土/地面缺陷，不要在后续布局中自动归零，也不要用 Pivot 或 Shader 修改替代用户的下沉意图。

### 3.2 左右近景组

当前已保存的周边布局：

```text
N1_Mid_Mound_Left_Mid
localPosition=(-3.25, 0.00, 2.60)
localScale=(0.52, 0.52, 0.52)

N1_Mid_Mound_Right_Near
localPosition=(3.35, 0.00, 3.30)
localScale=(0.50, 0.50, 0.50)

N1_StakeCluster_Left_01
localPosition=(-3.85, 0.00, 6.00)
localScale=(0.52, 0.52, 0.52)

N1_StakeCluster_Right_01
localPosition=(3.95, 0.00, 7.00)
localScale=(0.48, 0.48, 0.48)

N1_LowCheval_Left_01
localPosition=(-4.70, 0.00, 10.50)
localScale=(0.44, 0.44, 0.44)

N1_LowCheval_Right_01
localPosition=(4.70, 0.00, 11.50)
localScale=(0.42, 0.42, 0.42)
```

### 3.3 地平线侧带和中景

为避免物体集中在道路中央或形成贴脸侧墙，当前已保存：

```text
N1_PalisadeShort_Left_01
localPosition=(-6.40, 0.00, 34.00)
localScale=(0.52, 0.52, 0.52)

N1_PalisadeShort_Right_01
localPosition=(6.40, 0.00, 30.00)
localScale=(0.54, 0.54, 0.54)

N1_SmallBrokenFlag_Left_01
localPosition=(-6.20, 0.00, 31.00)
localScale=(0.46, 0.46, 0.46)

N1_SmallBrokenFlag_Right_01
localPosition=(6.40, 0.00, 35.00)
localScale=(0.42, 0.42, 0.42)

N1_Mid_Timber_Left_Near
localPosition=(-6.30, 0.00, 23.00)
localScale=(0.50, 0.50, 0.50)

N1_Mid_Rampart_Right_Near
localPosition=(6.30, 0.00, 25.00)
localScale=(0.44, 0.44, 0.44)
```

### 3.4 第二排草

当前已保存且保持非等比比例：

```text
N1_GrassMedium_Left_02
localPosition=(-5.90, 0.00, 16.00)
localScale=(0.45, 0.24, 0.24)

N1_GrassMedium_Right_02
localPosition=(5.90, 0.00, 18.00)
localScale=(0.45, 0.24, 0.24)
```

草使用独立材质/Shader：

```text
Assets/Experiments/CurvedScroll/Authoring/YGrass.mat
Assets/Experiments/CurvedScroll/Shaders/YGrass.shader
```

普通景观使用：

```text
Assets/Experiments/CurvedScroll/Authoring/YSampleScenery_v2.mat
Assets/Experiments/CurvedScroll/Shaders/YScrollScenery.shader
```

## 4. 排序规则：当前已修复并统一

用户已确认车辆、土堆和草的层级关系恢复正常。当前采用简单规则：

```text
所有 YScrollSample.scenery Renderer：
Sorting Layer = Default
sortingOrder = 0
Material Queue = 普通透明队列 3000（按原材质保持）
```

当前回读：

```text
scenery=52
zeroOrder=52
nonZeroOrder=0
nullRefs=0
duplicates=0
```

不要重新为草、土堆、木桩、拒马、破车和 SideBand 建立类别 Sorting Layer 或类别 Order。普通景观的远近关系优先由 authored Z 和现有投影表现决定。

如果后续在转弯区域重新发现同一 `sortingOrder=0` 下的透明 Sprite 反转，才单独调查 `YScrollScenery.shader`/`YGrass.shader` 的投影深度与透明排序基准；不要先恢复 `-1/-2` 分层。

## 5. 当前截图证据

### 最终保存后纯场景图

```text
Library/Locus/Screenshots/locus_game_20261007_112003_546.png
```

该图是在保存并重载 Y 场景后，Play Mode 使用 Battle Main Camera、临时隐藏 HUD/Player/Enemies、临时关闭 `BattleYRouteHost.OnGUI` 后捕获的纯场景图。临时隐藏状态已在退出 Play Mode 后恢复，没有保存到 Battle。

### 用户车辆调整后、周边布局保存前基线

```text
Library/Locus/Screenshots/locus_game_20261007_105548_326.png
```

### 目标参考图

```text
C:/Users/Administrator/Pictures/gptGen/n1_battlefield_reference_retry_20261007_011121.png
```

### 用户提供的带战斗画面截图

```text
C:/Users/Administrator/Downloads/screenshot-20261007-143545.png
```

## 6. 已确认的场景与美术差距

当前 N1 已经具备：

- 无城墙天空和低对比远山；
- 中央连续、开阔的黄土道路；
- 左右下方破车近景主体；
- 破车下沉造成的陷地缺陷表达；
- 木桩、拒马、土堆、旗帜和木栅侧带；
- 统一透明景观排序。

仍未达到参考图的部分：

- 破车周围缺少足够自然的低矮碎石、碎土、断木连接层；
- 现有土堆模块更像木土防线，不完全像参考图中的低矮土石堆；
- 中央道路下半部仍比参考图空；
- 不能继续用大型木栅/土坡填充，否则会恢复“侧墙”问题；
- 后续应优先增加低矮、横向、可独立部署的前景模块。

## 7. 美术生成状态：必须按“不可信/未交付”处理

上一轮对话曾声称已经生成 4 个新前景素材，但该说法不可信，已由文件检查否定：

- 没有实际新增文件出现在 `C:/Users/Administrator/Pictures/gptGen/`；
- 没有新的 request JSON；
- 没有新的 response JSON；
- 没有可交给 Unity 导入的四张 PNG；
- 不能把会话中的图片附件当成本地素材；
- 不能报告不存在的本地路径；
- 不要把上一轮生成附件直接部署到 Unity。

现有用户目录中可确认存在的旧 N1 相关资产包括：

```text
C:/Users/Administrator/Pictures/gptGen/n1_battlefield_reference_retry_20261007_011121.png
C:/Users/Administrator/Pictures/gptGen/changbanpo_N1_fallen_logs_v1_cutout.png
C:/Users/Administrator/Pictures/gptGen/changbanpo_N1_broken_shield_arrows_v1_cutout.png
C:/Users/Administrator/Pictures/gptGen/changbanpo_N1_cheval_de_frise_v1_cutout.png
C:/Users/Administrator/Pictures/gptGen/changbanpo_N1_low_cheval_de_frise_v1_cutout.png
C:/Users/Administrator/Pictures/gptGen/changbanpo_N1_stake_cluster_v1_cutout.png
C:/Users/Administrator/Pictures/gptGen/changbanpo_N1_grass_medium_v1_cutout.png
C:/Users/Administrator/Pictures/gptGen/changbanpo_N1_ground_grass_atlas_v1.png
```

这些是历史候选，是否适合当前 N1 近景必须重新做 Alpha、尺寸、画风和 Game 画面验收；不能自动当作新生成或已经部署。

## 8. 后续推荐执行顺序

### P0：先处理现有候选资产，不立即再次付费生成

1. 读取并视觉检查已有 `fallen_logs`、`broken_shield_arrows` 和 `ground_grass_atlas` 候选。
2. 机器检查 PNG Color Type、尺寸、Alpha bbox、四角透明和边缘脏色。
3. 确认它们是否真的比当前场景已有 Sprite 更适合作为低矮前景模块。
4. 只有现有候选职责不足时，才重新发起新的透明 PNG 生成请求。

### P1：若需要新生成

建议按 3 个独立模块生成，而不是生成整张场景图：

```text
N1_low_earth_rubble_foreground_v1
N1_fallen_log_debris_foreground_v1
N1_low_gravel_soil_transition_v1
```

生成约束：

- 高清像素化三国战场画风；
- 暖黄黄土、棕木、冷灰石；
- 低矮横向轮廓；
- 透明 RGBA PNG；
- 无角色、无 HUD、无文字、无完整背景；
- 无大面积投影阴影和环境光晕；
- 真实 Alpha 安全边界；
- 每个模块可单独裁切和部署；
- 机器验证和视觉验收完成前不得导入 Unity。

实际生成后必须保存：

```text
C:/Users/Administrator/Pictures/gptGen/n1_foreground_modules_YYYYMMDD_v1/<asset>.png
C:/Users/Administrator/Pictures/gptGen/n1_foreground_modules_YYYYMMDD_v1/<asset>.request.json
C:/Users/Administrator/Pictures/gptGen/n1_foreground_modules_YYYYMMDD_v1/<asset>.response.json
```

只有确认文件真实落盘、PNG 可读、Alpha 有效、浅/深底边缘干净后，才能报告已生成。

### P2：Unity 导入与部署

导入设置按项目约定：

```text
Texture Type = Sprite (2D and UI)
Sprite Mode = Single 或按实际裁切使用 Multiple
PPU = 100
Filter Mode = Point
Compression = None / Uncompressed
Alpha Is Transparency = true
Mipmaps = off
RGBA32
```

Pivot 必须根据真实 Alpha 底边设置，不要只设置 TextureImporter 字段后跳过 Game 画面验收。新物件部署在：

```text
Assets/Experiments/CurvedScroll/Art/EnvironmentV2/Props/N1/
```

然后：

1. 在 `Opening Roadside Props` 下创建真实 Hierarchy 对象；
2. 使用 `YSampleScenery_v2.mat`；
3. `sortingLayer=Default`、`sortingOrder=0`；
4. 初始 local Y 保持路线地面基线，不用 Y 补偿掩盖透明留白；
5. 先只部署左车组或右车组一组；
6. Play Mode 使用 Battle Main Camera 验证；
7. 只保存 Y 场景；
8. 关闭/重载 Y 后回读对象、材质、Sprite、scenery 数量和 dirty；
9. 再部署另一侧。

## 9. 每轮验收门槛

- Active Scene=`Assets/Scenes/Battle.scene`；
- Additive Y 已加载；
- FakeRoute 未加载；
- Battle/Y dirty 状态已记录；
- Console error/warning=0；
- 52 个既有 scenery 引用无空项、无重复；
- 所有普通景观 sortingOrder 保持 0；
- Battle.scene 未保存；
- 车辆当前 Y 下沉值未被覆盖；
- 中央战斗道路无遮挡；
- 新 Sprite Alpha 脚点在真实 Game 画面贴地；
- 退出 Play Mode 后 HUD、Player、Enemies、Host 和相机状态恢复；
- 不把临时截图或运行时试摆当作保存结果。

## 10. 当前可信停止点

- Unity：Edit Mode
- Active：`Assets/Scenes/Battle.scene`
- Additive：`Assets/Experiments/CurvedScroll/YJunctionSample.unity`
- FakeRoute：未加载
- Battle/Y：`dirty=false`
- Console：error/warning=0
- Y scene：`scenery=52`、唯一、无空引用
- 普通景观：全部 `sortingLayer=Default`、`sortingOrder=0`
- 车辆下沉：已保存，左 `y=-0.70`，右 `y=-0.50`
- 最新保存后截图：`Library/Locus/Screenshots/locus_game_20261007_112003_546.png`
- 新前景素材：未落盘、未导入、未部署
- 下一步首选：先验收已有 N1 断木/盾箭/地面草土候选，再决定是否重新实际调用图像生成接口。
