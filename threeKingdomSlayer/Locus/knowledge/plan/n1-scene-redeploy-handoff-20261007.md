---
id: kd_55146d52-83cf-43cc-a52b-e082bd5ceb33
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# N1 场景重新部署交接（2026-10-07）

## 1. 任务状态

本轮 N1 场景重新部署任务已暂停。用户要求停止场景修改，后续由新对话继续。

本轮最后一次操作已经退出 Play Mode；没有保存最后的运行时试摆。当前没有继续调整 Transform、材质、Shader、Battle 场景或路线代码。

## 2. 正式场景结构

当前正式结构固定为：

```text
Assets/Scenes/Battle.scene
└─ Additive Assets/Experiments/CurvedScroll/YJunctionSample.unity
```

- Active Scene：`Assets/Scenes/Battle.scene`
- Additive 场景：`Assets/Experiments/CurvedScroll/YJunctionSample.unity`
- `FakeRouteDataTrial.unity` 未加载，不要打开、保存、回退或向其部署。
- Battle 场景负责 Player、Enemy、Camera、StageController、HUD 和真实战斗。
- Y 场景负责道路、天空、N1 环境 Sprite 和路线层景观。
- 不要修改 `YScrollSample.Evaluate()`、路线长度、转角、Battle Camera、Player、Enemy 或战斗坐标来适配美术。

## 3. 当前已恢复的 Y 场景内容

正式 `YJunctionSample.unity` 已从本地恢复快照写回并重载，当前回读结果：

- `YScrollSample` 存在；
- `YScrollSample.scenery = 52`；
- `Opening Roadside Props` 存在，active=true，子节点数=65；
- N1 对象共 51 个，包含近景、中景和直路尾段扩展对象；
- 草簇对象仍在场景快照中；按名称统计 `Grass*` 对象较多，其中一部分是风动草的辅助层；不要按名字批量删除；
- `Skybox Background - editable` 存在并启用；
- `N1 Far Landmarks` 存在，旧 `N1 Distant City Wall Landmark` 保持禁用；
- `Battle.scene` 和 Y 场景均 dirty=false；
- Console error/warning=0；
- Edit Mode 下 `YScrollSample.viewCamera=None` 是正常的，Play 时由 `BattleYRouteHost` 绑定 Battle Main Camera。

当前正式 Y 场景的本地 SHA-256：

```text
e42d270c36d30a767f2abfd286af07b8ffaacbf16f55117f4fd59284ee902ccd
```

它与本地恢复源相同：

```text
Library/Locus/tmp/YJunctionSample.rewrite-backup.unity
```

## 4. 恢复来源与历史快照

本轮恢复前 Git 中的 Y 场景已经回到较早基线，缺少 `Opening Roadside Props` 和 N1 专用侧带。通过本地临时快照找回了上一轮部署：

### 当前采用的快照

```text
Library/Locus/tmp/YJunctionSample.rewrite-backup.unity
```

特征：

- 约 62,223 行；
- `scenery` 52 项；
- N1 对象 51 个；
- 包含 `Opening Roadside Props`；
- 包含草簇层和直路尾段 `N1_SideBand_Left_05~08`、`N1_SideBand_Right_05~08`、`N1_Mid_Ruins_*_Tail`。

### 较早快照

```text
Library/Locus/tmp/n1-sideband-review/YJunctionSample.before-sideband.unity
```

特征：

- 约 61,323 行；
- `scenery` 42 项；
- N1 对象 41 个；
- 只有早期 N1 侧带，没有后续直路尾段 10 个扩展对象。

不要用较早快照覆盖当前正式场景，除非明确需要回退到 42 项版本。

## 5. 参考图和当前运行时证据

用户目标图：

```text
C:/Users/Administrator/Pictures/gptGen/n1_battlefield_reference_retry_20261007_011121.png
```

目标构图：

- 竖屏开阔黄土战场；
- 中央道路占据大面积画面并保持空旷；
- 左下近景有大型破车、木桩/拒马和草簇；
- 右下近景有另一辆破车、拒马、木桩和草簇；
- 地平线两侧有断旗、木栅、低矮残垣和少量烟尘；
- 远景为低对比山脉和天空；
- 近景、中景、远景有明显尺度差异；
- 侧景不能侵入中央战斗通道。

最近有效的纯场景运行时截图：

```text
Library/Locus/Screenshots/locus_game_20261007_033820_516.png
```

该截图是在 Play Mode 暂停后临时隐藏 Battle HUD、角色和战斗渲染得到的，未保存这些隐藏状态。它显示：

- 天空和道路方向基本正确；
- 当前两侧环境主体仍集中在地平线附近；
- 旧的暗色树/石侧景仍然抢视觉；
- N1 破车、拒马、木桩和草簇没有达到参考图下半部的近景占比；
- 中央道路仍然保持开放。

早期运行时基线截图：

```text
Library/Locus/Screenshots/locus_game_20261007_031453_820.png
```

## 6. 当前 N1 关键对象坐标（正式场景，未保存试摆前）

这些是退出 Play Mode 后从正式场景回读的值，后续新对话应以它们为当前基线：

```text
N1_BrokenCart_Left_01  localPosition=(-3.7, 0, 24)  localScale=(0.50, 0.50, 0.50)
N1_BrokenCart_Right_01 localPosition=(3.7, 0, 26)   localScale=(0.44, 0.44, 0.44)
```

其他近景对象：

```text
N1_StakeCluster_Left_01   (-2.8, 0, 11)  (0.65,0.65,0.65)
N1_StakeCluster_Right_01  (2.8, 0, 15)   (0.55,0.55,0.55)
N1_PalisadeShort_Right_01 (2.9, 0, 20)   (0.64,0.64,0.64)
N1_PalisadeShort_Left_01  (-2.9,0,34)   (0.58,0.58,0.58)
N1_LowCheval_Left_01      (-2.8,0,24)   (0.50,0.50,0.50)
N1_LowCheval_Right_01     (2.8,0,31)    (0.55,0.55,0.55)
N1_SmallBrokenFlag_Left_01  (-3,0,27)   (0.50,0.50,0.50)
N1_SmallBrokenFlag_Right_01 (3,0,34)   (0.46,0.46,0.46)
```

草簇当前正式场景基线：

```text
N1_GrassMedium_Left_01   (-5.0,0,8)   (0.45,0.24,0.48)
N1_GrassMedium_Right_01  (5.0,0,10)  (0.45,0.24,0.48)
N1_GrassMedium_Left_02   (-5.4,0,18)  (0.45,0.24,0.48)
N1_GrassMedium_Right_02  (5.4,0,20)   (0.45,0.24,0.48)
N1_GrassMedium_Left_03   (-4.8,0,24)  (0.42,0.23,0.46)
N1_GrassMedium_Right_03  (4.8,0,26)   (0.42,0.23,0.46)
N1_GrassMedium_Left_04   (-5.0,0,30)  (0.42,0.23,0.46)
N1_GrassMedium_Right_04  (5.0,0,32)   (0.42,0.23,0.46)
```

中景 N1 对象使用 `YSampleScenery_v2.mat` 和 `CurvedScroll/Y Sample Scenery`；草簇使用 `YGrass.mat` 和 `CurvedScroll/Y Grass`。中景排序主要为 -1，侧带部分为 -2，近景 SpriteRenderer 多为 0。

## 7. 最后一次未保存的运行时试摆

在 Play Mode 暂停状态曾临时设置：

```text
N1_BrokenCart_Left_01 localPosition=(-5.2,0,8)
N1_BrokenCart_Left_01 localScale=(0.52,0.52,0.52)
```

这只是运行时试摆，随后已经退出 Play Mode，未保存。正式场景当前仍为 `(-3.7,0,24)` / `0.50`。

试摆目的：证明 `z=24` 时破车投影落在地平线附近，不符合参考图；需要把破车推到更近的 z 区间并在真实 Game 画面中重新校准。不要直接把试摆值当作验收值，必须重新捕获运行时画面。

## 8. 已确认的投影问题

`YScrollScenery.shader` 在启用 `_YSEnabled` 后，会根据物件的 authored z 做下沉和曲率投影：

```text
if (anchor.z > 18)
    anchor.y -= .009 * d * d * smoothstep(...)
```

因此不能用普通世界坐标直觉判断远近。已实测：

- 左破车 `(-3.7,0,24)` 在运行时屏幕上只接近地平线，主体约 61x67 像素；
- 参考图要求破车成为左下/右下近景锚点；
- 下一步应该在 Play Mode 中用屏幕占比反推 z/x/scale，再将确认后的值写回 Y 场景。

## 9. 推荐的新对话执行顺序

1. 先读本交接文档和以下规则文档：
   - `Locus/knowledge/memory/y-junction-scene-authoring-workflow.md`
   - `Locus/knowledge/memory/project-mistake-note.md`
   - `Locus/knowledge/plan/curved-scroll-development-handoff.md` 第14节及 N1 相关段落
   - `Locus/knowledge/plan/n1-scene-art-production-handoff.md`
2. 确认 Active Scene 为 Battle，Additive Y 已加载，FakeRoute 未加载，Battle/Y dirty 状态和 Console。
3. 不要执行全仓库 `reset`、`restore`、`clean`，不要覆盖其他未提交文件。
4. 先进入 Play Mode，由 `BattleYRouteHost` 绑定 Battle Main Camera。
5. 暂时隐藏 Battle HUD/角色只用于纯场景截图，退出 Play Mode 后让临时状态自然丢弃；不要保存临时隐藏状态。
6. 先单独校准左破车，再对称校准右破车；目标是让两辆车分别成为左右下方的近景主体，同时保持道路中心无遮挡。
7. 再调整木桩、拒马、断旗和草簇，使其围绕破车形成参考图中的近景侧带。
8. 最后调整低矮残垣/土坡/木栅中景和远景密度，不要启用旧城墙地标。
9. 每轮只保存 `Assets/Experiments/CurvedScroll/YJunctionSample.unity`，保存后关闭/重载 Y 并回读：N1 对象、`scenery` 数量、材质、active、dirty、Console。
10. 最终必须重新进入 Play Mode，在 Battle Main Camera 下验收开局画面，并清理临时渲染隐藏状态。

## 10. 保护边界

禁止：

- 修改 `Assets/Scenes/Battle.scene` 来适配美术；
- 移动 Player、Enemy、Camera、StageController、HUD 或战斗坐标；
- 修改 `YScrollSample.Evaluate()`、`Length`、junction、radius、turnAngle 或路线运动；
- 批量用 `Enemy.visualYOffset` 掩盖景物脚点；
- 重新启用 `N1 Distant City Wall Landmark` 或城墙屏幕背景；
- 打开、保存、回退或向 `FakeRouteDataTrial.unity` 部署；
- 根据对象名称批量删除 `Grass*` 或旧对象；
- 用整文件 Git restore 覆盖当前正式 Y 场景；
- 将本地恢复快照误当作 Git 提交历史。

## 11. 当前 Git 注意事项

工作区存在其他用户/历史未提交内容，至少包括若干知识文档、`workspace-trees/default.json` 和未跟踪文件。当前与本任务直接相关的状态：

```text
M Assets/Experiments/CurvedScroll/YJunctionSample.unity
```

该场景相对 Git HEAD 的 diff 很大，原因是恢复快照保留了完整 N1/草簇部署并带有 Unity YAML 序列化重排；不要因为 diff 大就整文件回退。保存新局部变更前必须先保留当前工作树和恢复快照。

## 12. 停止点

- Unity 当前为 Edit Mode；
- Active Scene 为 `Assets/Scenes/Battle.scene`；
- Battle/Y 均 dirty=false；
- Console error/warning=0；
- 正式 Y 场景保留恢复后的 N1 部署；
- 最后的破车近景试摆未保存；
- 没有正在执行的场景修改任务。
