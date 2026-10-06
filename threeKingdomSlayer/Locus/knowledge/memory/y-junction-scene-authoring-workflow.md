---
id: kd_df013b4e-c25d-4bc8-941f-882ab1417ab0
summary: Y分叉卷轴场景搭建：固定Battle宿主与Additive Y路线层，保护移动和用户布局，按实际Game截图验证背景、地面、草簇与保存结果。
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# Y 分叉卷轴场景布置工作流

## 适用范围

新对话先看 `plan/curved-scroll-development-handoff.md` 第14节的最新状态；本文记录可复用操作规则，不把历史实验或备选方案当成必须执行的任务。美术修改优先复用已有素材，先拍Game截图再决定是否需要生图。

本流程适用于当前两分支 Y 形卷轴路线的美术场景搭建、环境材质部署、插片布置和跨对话交接。正式结构只有：

```text
Assets/Scenes/Battle.scene
└─ Battle Y Route Host
   └─ Additive Assets/Experiments/CurvedScroll/YJunctionSample.unity
```

`Battle.scene` 是唯一战斗宿主；`YJunctionSample.unity` 是当前唯一分叉路线美术/路线层。旧的 `Assets/Experiments/CurvedScroll/FakeRouteDataTrial.unity` 是旧单路线/连续距离实验入口，除非用户单独授权，不打开、不保存、不回退、不导入其材质。

## 场景职责

### Battle.scene

负责：

- Player、Enemy、StageController、对象池、HUD、Battle Camera；
- 真实敌人生成、战斗、QTE、投射物和奖励流程；
- `BattleYRouteHost` 路线流程和 Battle 主相机绑定。

美术部署阶段不得移动或替换这些对象来适配路线视觉。

### YJunctionSample

负责：

- Y 形 Terrain/道路视觉；
- Left/Right 分支路径与 Progress 预览；
- 树、岩壁、栅栏、旗帜、火盆、建筑等真实可保存 Hierarchy 对象；
- `Encounter Authoring`、Battle Anchor、Player Entry 和敌人预览标记；
- 独立的远景背景层。

路线层不得放真实 Enemy、对象池、StageController 或第二套战斗管理器。

## 移动系统保护边界

美术部署前冻结并只读检查：

- `YScrollSample.Evaluate()`、`progress`、`right`、`turnAngle`、`Length`；
- Left/Right 路径和转角规则；
- `BattleYRouteHost`；
- Battle.scene 的 Player、Enemy、Camera、StageController、QTE 和 Projectile 坐标；
- Y 场景的路线层级和已验收移动逻辑。

任何地面、背景或插片问题不得直接通过重写移动算法、修改路径点、旋转 Battle 对象或批量改 `Enemy.visualYOffset` 解决。

## 标准部署流程

### Phase 0：现场确认与移动基线

每次新对话或新一轮部署开始前，先读取并记录：

- Active Scene 必须是 `Assets/Scenes/Battle.scene`；
- `YJunctionSample` 已 Additive 加载；
- `FakeRouteDataTrial` 未加载；
- Battle.scene 和 Y 场景 dirty 状态；
- `git status --short --branch`；
- Console error/warning；
- Hierarchy 中不存在 `Generated Route Road`、`Generated Route Shoulder` 或临时预览树石。

- 核对 `YScrollSample.viewCamera` 是实际输出Game画面的相机，避免两个相机同时接管画面。跨场景Main Camera引用不能靠保存Y场景可靠持久化；重开后为空不等于路线丢失。已加载Battle+Y时先做会话绑定；Play由 `BattleYRouteHost` 重新绑定。
- `Tools/Curved Scroll/Open Battle Host` 会先Single重开Battle，菜单只检查Active Scene dirty，未检查所有加载场景；**Y有未保存工作时不要调用**。先检查所有loaded scenes，不为绑定相机而重开用户正在编辑的场景。
- 预览检查Progress必然临时改运行/编辑状态，应记录原值并在 `finally` 恢复progress/right/camera/targetTexture及临时显隐；这类调用标记为写操作，不能伪装read-only。

在不修改持久布局、材质或代码的情况下，临时预览并记录：

```text
Left  Progress 0 / 0.25 / 0.5 / 0.75 / 1
Right Progress 0 / 0.25 / 0.5 / 0.75 / 1
```

每个点检查：位置、路线角度、道路是否连续、景物是否贴地、中央战斗区是否无遮挡。基线失败时停止，不进入美术部署。

### Phase 1：背景

背景是天空盒式远景，不是场景建筑板，也不是普通路线景物。

职责只包含：

- 天空；
- 云；
- 远山；
- 低对比大气雾。

不得包含：城门、城墙、瞭望塔、旗帜、帐篷、道路、前景地面、敌人或 HUD。

部署规则：

- 使用独立背景材质/Shader；
- 不加入 `YScrollSample.scenery`；
- 不读取 `_YSEnabled`、`_YSPose` 等路线投影全局参数；
- 不参与普通景物的距离淡出和曲率变形；
- 不覆盖道路和侧景；
- 用 Battle Main Camera 验证，不使用 Y 样例相机作为正式运行相机。

单张竖版概念图只能作为色彩/山体参考，不能未经职责检查直接作为最终天空盒。

### Phase 2：三层地面

地面不能只用“中央道路 + 外侧地面”两层高差贴图硬切。目标职责为：

```text
中央 Battle Road
→ 中间 Shoulder / Road Transition
→ 外侧 Outer Ground
```

#### 中央道路

- 明亮、干净、低对比暖黄；
- 少量细小砂砾；
- 中央战斗区可读；
- 不出现大块岩石、密集枯草或边框。

#### 过渡肩部

- 颜色比道路略深；
- 仍保持压实土质；
- 少量碎石、压痕和细小干草；
- 不出现外侧地面的大石块和高对比草簇；
- 负责消除道路与外侧地面的突兀接缝。

#### 外侧地面

- 更深的棕黄/灰褐；
- 碎石、枯草、粗糙斑块密度更高；
- 可以承载树、岩石、木栅和营地装饰；
- 不向中央战斗区侵入。

先验证纹理无明显接缝，再导入 Unity。道路材质、过渡肩材质和外侧材质必须在 Y 场景中明确可追踪。

### Phase 3：分支道路覆盖

当前单一 Y Terrain + Shader 只能作为基线，替换 `_Road/_Side` 纹理不等于左右连接道路已经完成。

如果左右转弯看起来出现地面缺口：

1. **先隔离遮挡**：同机位开/关背景和可疑透明层，真实读取两张图。若关闭背景后道路出现，修背景Shader/Queue/深度测试，不修改地面或路径。
2. 只在遮挡排除后，读取Terrain bounds、顶点/三角范围、分支采样范围以及地面Shader裁剪。截图与计算一起判断，不能从物体悬空就推断网格不存在。
3. 本轮天空开/关对照已证明原来的空带是 `YScrollScenery` 天空大卡片盖住地面；共享曲率与裁剪实验未奏效，已恢复原样。当前仍用已有单一 `YSampleTerrain`，不是已证实覆盖不足。
4. 只有另一次诊断确证当前网格无法覆盖且用户同意时，才设计一次性持久化Road/Shoulder Mesh。这是备用方案，不是后续默认必须改造。
5. 未部署的 `YRouteSurfaceAuthoring.cs/YRouteSurface.shader` 草稿不可直接使用；当前组件包含OnEnable重建和运行程序集中的UnityEditor调用，需要专门整改才可能作为生产方案。

任何持久面片必须拥有明确的Mesh资产、材质与路径引用；不能在加载或Play时重建、更不能自动覆盖用户布局。当前美术任务优先保留道路和移动，处理侧景。

### Phase 4：侧景与插片

侧景必须是真实 Hierarchy 对象，可选中、移动、缩放、换 Sprite、保存和重开。

推荐层级：

```text
Y Junction - select for preview
├─ Opening Roadside
├─ Left Branch Content
├─ Right Branch Content
├─ Left Valley Encounter
├─ Right Camp Encounter
└─ Background - sky only
```

按照路线采样点和横向偏移布置，不使用任意世界坐标撒点：

```text
景物脚点 = 路径采样位置 + 道路横向偏移 + 层级深度调整
```

密度采用近/中/远三层：

- 开局直路：木栅、火盆、军旗、拒马和箱体形成节奏；
- 左路山谷：近景岩石/灌木，中景栅栏/旗帜/火盆，远景岩壁/树群/瞭望塔；
- 右路营地：入口栅栏/旗帜/火盆，中景拒马/门楼，远景主帐篷/瞭望塔；
- 中央战斗区域保持可读，不用大型建筑或高草挡满；**用户允许少量低矮草进入道路边缘，不存在“草绝不能侵入道路”的要求**。
- 草密度、尺寸和分布以真实Game截图决定，不凭统一缩放范围或Transform数值猜画面。近处透视占比、转弯未选分支的草、后段空档都要检查。
- 小范围试摆后必须持续推进到有意义的一组，并明确“已保存/未保存”；不能只摆6株就宣称密度需求完成，也不能因画面不合格把全部草删除当作最终交付。
- 先调整已有四种草图，不因部署失败自动再生新图；用户认可的密度要保留。

#### 植物Y旋转与高度
- 普通 `YScrollScenery.shader` 将景物顶点强制朝路线视图，可能忽略Transform自身旋转。先验证视觉效果，不以Inspector角度非零作为完成依据。
- 当前草用独立 `YGrass.shader` + `YGrass.mat`，只为草处理相机朝向和作者Y偏转；不要修改共享树/建筑Shader。
- 缩矮草优先减少Y scale、保留X宽度和数量；基于已有尺度计算一次，不反复乘倍率导致越来越小。先记录原值，使用同机位before/after和yaw=0/非零对照验证。
- 历史草高度/朝向检查：形状高度降低、非零Y偏转生效、草Shader无编译消息；这种验证不是完整Play路线测试。后续用户已确认草摇曳实现，仍不等于所有路线构图均已验收。

#### 草风动维护规则
- 当前渲染不是原始4顶点Sprite，而是保留并禁用原SpriteRenderer，由每株的 `Wind Mesh` 子节点实际渲染。网格及三份材质在 `Assets/Experiments/CurvedScroll/Authoring/GrassWindMeshes/`，每种网格21顶点/24三角形。
- 原SpriteRenderer作为布局源保留；维护时检查54株与54个唯一风动引用，禁止将原Renderer和子Mesh一起追加到scenery。整组景物当前基线为151项，后续合法新增应重新建立数量基线。
- 实际风动材质为上述目录的 `YGrass_GrassWide_v1.mat`、`YGrass_GrassSmall_v1.mat`、`YGrass_GrassMedium_v1.mat`；修改旧 `Authoring/YGrass.mat` 不代表实际Mesh材质已改变。当前强度0.28、速度2.1，自动时间必须为 `_WindTimeOverride=-1`；非负值只用于固定时间试验并在结束恢复。
- 风动位于Shader的 `_YSEnabled` 路线投影分支内；Edit预览无相机绑定时不可靠，不能因此判定已确认效果失效。维护以实际Game连续画面为准，用户效果确认与资产保存/重载确认分别记录。

### Phase 5：Sprite 导入和贴地

每个插片导入后都必须回读：

- `TextureImporter` 的类型、尺寸、Filter、Compression；
- `Sprite.pivot`；
- `Sprite.bounds`；
- `Renderer.bounds.min.y`；
- 运行时/预览时的实际脚点。

底部Pivot必须按实际不透明脚点设定。通过 `TextureImporterSettings.spriteAlignment=Custom` 配合 `spritePivot`，或合适的BottomCenter设置，再重导入回读；只设置 `ti.spritePivot` 而Alignment仍是Center不会得到预期结果。

带透明padding时，`Renderer.bounds.min.y=0`只证明矩形底在地面，不证明可见草根贴地。要另外检查alpha底边留白/PPU、Sprite实际脚点和Game画面。Shader投影后的可见位置也不能完全用未投影Renderer.bounds代替。

新Sprite若要裁切既有合图，先按真实主体连通范围检查，不按固定四等分切片：现有GrassSmall带邻株残段、GrassWide被截边的迹象需要后续关注，但不因此先发新生图请求。

### Phase 6：保存和回归

每次只做一个变更：背景、地面层、单组侧景、单个 Pivot 不混在同一次保存中。

保存前：

- Active Scene=`Battle.scene`；
- Y 场景 Additive 已加载；
- `FakeRouteDataTrial` 未加载；
- 无运行时生成道路/路肩；
- 无 DontSave 临时对象；
- Console 新错误为 0；
- Y 场景 dirty 内容仅是本次预期对象。

只保存明确目标 `YJunctionSample.unity`。禁止进入旧场景触发导入后再保存。

## 验收矩阵

### 静态

- Hierarchy 中能选中所有新插片；
- 左右分支对象父节点正确；
- 中央战斗区无大型物件；
- 背景不含建筑，不参与路线景物投影；
- 道路→肩部→外侧地面色彩、颗粒和尺度自然过渡；
- Sprite 脚点与地面一致。

### Edit Mode

- Progress 0/0.25/0.5/0.75/1；
- Left/Right 各五点；
- Game 画面道路连续，景物不悬空；
- Scene 平铺 Transform 不被预览改变；
- 重开场景引用保持。

### Play Mode

- Battle 启动→开局移动→战斗→奖励；
- 自然点击 Left，观察真实转向、抵达和下一战；
- 重新启动并自然点击 Right，重复验证；
- 暂停、恢复、退出 Play、重复打开 Battle Host；
- Player/Enemy/QTE/Projectile 坐标与命中不变；
- Console 无新 error/warning、MissingReference、DOTween 或数组越界。

## 绝对禁止

- 修改或保存 `FakeRouteDataTrial.unity`；
- 对场景执行整文件 `git restore` 或覆盖；
- 在未记录 diff/对象清单时保存场景；
- Play/OnEnable 中生成并保存道路、路肩、树石或临时预览；
- 用全局 Shader 接管 Battle 对象；
- 把天空背景加入普通 `scenery`；
- 把概念图整张直接当作最终天空盒；
- 用 `visualYOffset` 批量掩盖地面坐标错误；
- 未验证移动基线就继续增加插片；
- 把“材质替换成功”“编译通过”“一次 Progress 调用成功”表述为路线完成。

- 矩形 bounds.min.y 不能代替alpha脚点；仍需Game截图。
- Scene中的草数量/密度以已保存54株为起点，不要因名称中包含Trial删除它们。

## 跨对话入口

新对话先读取 `plan/curved-scroll-development-handoff.md` **第14节（2026-10-01当前交接）**、本文、`plan/latest-todolist.md` 当前优先清单及 `memory/project-mistake-note.md`。确认Git与Editor现场，计划中的“道路Mesh备用方案”不得覆盖已经证实的背景遮挡结论；然后只在 `Assets/Scenes/Battle.scene` + Additive `Assets/Experiments/CurvedScroll/YJunctionSample.unity` 工作。
