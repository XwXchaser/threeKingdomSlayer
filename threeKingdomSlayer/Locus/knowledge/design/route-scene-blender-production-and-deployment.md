---
id: kd_47eaa9f8-1f3b-45d4-a363-10752239b1e5
injectMode: inherit
aiEditMode: inherit
---

# Route Scene V2：Blender 场景制作与 Unity 部署规范

## 1. 文档目的

本文面向负责 Blender 建模、场景搭建和资产交付的 AI/美术工具，说明当前 `route-scene-v2-baseline` 分支的场景构造方式、空间契约、导出要求和 Unity 部署接口。

已有运行时架构参考：`Locus/knowledge/design/route-scene-architecture-v2.md`。

本文不是普通美术风格说明，而是一个可执行的场景制作交付规范。Blender 输出的场景必须能够被放入 Unity 的 RouteStage Scene，并通过 Inspector 引用接入路线系统。

## 2. 当前运行方式

本分支不是“玩家节点向前移动”方案，也不是播放视频伪装移动。

核心规则：

```text
Player 固定在 Battle.scene 的战斗坐标
Enemy / Battle Camera / HUD 固定
RouteStage Scene 作为附加场景加载
通过移动、旋转 RouteStageRoot 表达玩家沿路线前进
```

运行时涉及两类场景：

```text
Battle.scene
├─ Player
├─ Enemy / Wave / Combat systems
├─ Battle Camera
├─ Battle HUD
└─ RouteStageTargetsV2

RouteStageV2/Stage01_RouteV2.unity
└─ RouteStageRoot
   ├─ CombatNode_A
   ├─ CombatNode_B
   ├─ CombatNode_C
   └─ ...
```

RouteStage Scene 以 Additive 方式加载。路线节点不会逐个加载或卸载；整张路线场景一次加载，节点切换只改变 `RouteStageRoot` 的运行时位置和旋转。

## 3. Blender 必须制作的空间结构

每个 CombatNode 必须包含以下三个空间锚点：

```text
CombatNode_A
├─ HeadJunction
├─ CombatArea
│  └─ 节点环境、美术、道路、装饰
└─ TailJunction
```

### 3.1 HeadJunction

用途：节点进入时的入口锚点。

要求：

- 位于进入该节点的道路/路线起点；
- Transform 的位置代表玩家抵达该节点时的空间位置；
- Transform 的朝向代表玩家进入该节点后的最终朝向；
- 必须能够作为一个整体被 RouteStageRoot 移动、旋转；
- 不要把 HeadJunction 固定绑定到 Battle Camera 或 Player；
- 不要假设所有节点入口方向相同。

### 3.2 CombatArea

用途：该节点的战斗场景中心/战斗环境。

要求：

- 包含本节点的地面、道路、背景、装饰、可见环境；
- 其 Transform 是整个 CombatArea 对齐 Battle 固定战斗区域的参考点；
- CombatArea 对齐完成后，Battle 中固定的 Player、Enemy 和 Camera 应能正确显示该节点环境；
- 战斗区域应避免依赖运行时重新摆放 Player；
- 不要在 CombatArea 中创建会接管 Battle 战斗逻辑的 Player、Enemy、WaveSpawner 或摄像机。

### 3.3 TailJunction

用途：节点战斗完成后的离开锚点。

要求：

- 位于节点出口道路/路线末端；
- 位置和朝向代表玩家离开该节点时的姿态；
- 必须能够连接到一个或多个其他节点的 HeadJunction；
- 多个出口必须分别有独立的场景路径；
- 不要用代码假设 Tail 到 Head 是直线或固定距离。

## 4. 路径点与路线连接

每个节点内部有两段路径：

```text
HeadJunction → CombatArea
CombatArea   → TailJunction
```

每条路线连接还有一段路径：

```text
当前节点 TailJunction → 目标节点 HeadJunction
```

在 Unity 中对应字段：

```text
RouteCombatNodeEntryV2
├─ headJunction
├─ combatArea
├─ tailJunction
├─ headToCombatPath[]
└─ combatToTailPath[]

RouteConnectionSceneBindingV2
├─ sourceTail
├─ targetHead
├─ rotationPivot
└─ travelPath[]
```

### Blender 路径制作规则

- 路径点按移动顺序命名并保持顺序：`P00`, `P01`, `P02` ...；
- 路径首点应靠近/对应源锚点；
- 路径末点应靠近/对应目标锚点；
- 路径点必须是明确的 Empty 或可转换为 Unity Transform 的对象；
- 不要只交付 Blender Curve 而不提供可导出的控制点；
- 不要把路径依赖成不可追踪的约束、驱动器或脚本；
- 转弯处必须增加足够控制点，避免 Unity 运行时只用首尾点导致穿墙或跳转；
- 每条连接必须有独立路径，即使多个连接共用同一个目标 Head；
- 多条连接可以共享同一个目标 Head，但不能共享互相冲突的目标朝向。

建议结构：

```text
RouteStageRoot
└─ CombatNode_A
   ├─ HeadJunction
   ├─ CombatArea
   ├─ TailJunction
   ├─ HeadToCombatPath
   │  ├─ P00
   │  ├─ P01
   │  └─ P02
   └─ CombatToTailPath
      ├─ P00
      ├─ P01
      └─ P02

Connections
└─ A_to_B
   ├─ SourceTailReference
   ├─ TargetHeadReference
   ├─ RotationPivot
   └─ TravelPath
      ├─ P00
      ├─ P01
      └─ P02
```

## 5. 坐标、朝向和尺度契约

### 5.1 坐标系统

- 使用 Blender 导出到 Unity 后的统一坐标约定；
- 不要在模型内部额外创建“玩家补偿根节点”；
- 场景的真实路线关系必须由 Transform 位置和旋转表达；
- 不要将路线坐标写入战斗脚本或使用额外硬编码偏移；
- 不要要求 Unity 运行时把 Player 移到 Blender 场景中的实际位置。

### 5.2 朝向

- 每个 HeadJunction、CombatArea、TailJunction 都必须有明确旋转；
- 朝向表示玩家/路线进入该阶段时的前进方向；
- 入口方向可以不同，不能默认都是世界坐标的固定方向；
- 不要假设所有转弯都是固定 90 度；
- RouteStageRoot 的旋转由运行时根据锚点和目标姿态计算。

### 5.3 尺度

- Blender 使用真实且一致的单位比例；
- Unity 导入后应保持道路、角色、战斗区域的比例关系；
- 建模前先用 Unity Battle 固定战斗区域作为尺度参考；
- CombatArea 的可战斗空间必须覆盖 Battle 中固定的敌人生成区域和攻击表现范围；
- 避免极端小尺度或超大尺度，以免产生摄像机裁剪、排序和物理问题。

## 6. 美术内容边界

Blender 负责：

- 地面、道路、桥梁、建筑、山体、树木和背景结构；
- 节点之间的连接道路与转弯形态；
- CombatArea 的环境主题；
- 环境材质、贴图、静态装饰和可见遮挡；
- 必要的碰撞体或碰撞标记。

Blender 不负责：

- Player；
- Enemy；
- WaveSpawner；
- StageController；
- Battle Camera；
- HUD；
- QTE；
- 伤害、攻击、路线选择或奖励逻辑；
- 依赖 Blender 脚本才能在 Unity 中运行的路线推进逻辑。

如果需要环境交互，应交付明确的 Unity 可识别对象，并由 Unity 脚本接管行为。

## 7. Blender 交付物建议

每个路线关卡建议交付：

```text
route_stage_v2.blend
route_stage_v2.fbx 或 glTF
textures/
materials/
README.md
```

README 至少记录：

- Blender 版本；
- 单位和导出轴向；
- 模型原点；
- RouteStageRoot 对应对象；
- 每个 CombatNode 的名称；
- 每个 HeadJunction / CombatArea / TailJunction 的名称；
- 每条连接的路径点名称和顺序；
- 使用的材质和贴图；
- 是否包含碰撞体；
- 是否有透明、双面、动画或特殊 Shader 要求。

建议导出结构保持对象名称稳定。Unity 侧需要通过 Inspector 直接引用 Transform，因此名称变化必须同步说明。

## 8. Unity 部署流程

1. 将模型、材质和贴图导入 `Assets/` 合适目录；
2. 在 Unity 中创建或更新对应 RouteStage Scene；
3. 创建 `RouteStageRoot`；
4. 将 Blender 环境放到对应 CombatNode 下；
5. 创建并配置 `RouteStageSceneEntryV2`；
6. 为每个节点配置 `RouteCombatNodeEntryV2`；
7. 直接拖拽配置：
   - `nodeConfig`
   - `headJunction`
   - `combatArea`
   - `tailJunction`
   - `headToCombatPath`
   - `combatToTailPath`
8. 为每条连接配置 `RouteConnectionSceneBindingV2`；
9. 直接拖拽配置：
   - `sourceNode`
   - `targetNode`
   - `sourceTail`
   - `targetHead`
   - `rotationPivot`
   - `travelPath`
10. 在 `RouteStageTargetsV2` 中确认 Battle 固定目标：
    - `initialHeadTarget`
    - `combatTarget`
    - `tailTarget`
11. 使用编辑器预览或 Play Mode 验证三段移动；
12. 确认玩家、敌人和 Battle Camera 未被路线场景移动；
13. 确认路线完成后可以正常进入胜利结算。

运行时入口脚本为 `RouteStageRuntimeV2`。它会：

- Additive 加载路线场景；
- 找到 `RouteStageSceneEntryV2`；
- 将起始 Head 对齐到 Battle 的 `initialHeadTarget`；
- 移动 RouteStageRoot，使 CombatArea 对齐 `combatTarget`；
- 战斗完成后移动 RouteStageRoot，使 Tail 对齐 `tailTarget`；
- 路线选择后沿连接路径将目标 Head 对齐到 `initialHeadTarget`；
- 再执行目标节点的 Head→Combat；
- 全程不移动 Player。

## 9. 验收标准

### 结构验收

- RouteStage Scene 中存在唯一 `RouteStageSceneEntryV2`；
- `routeStageRoot` 已配置；
- 每个节点都有唯一 Head、CombatArea、Tail；
- 每个节点都绑定正确的 `RouteNodeConfigV2`；
- 每条连接都绑定 source/target 节点；
- 每条连接都有 source Tail、target Head 和独立路径；
- 不存在只靠名称或字符串查找才能工作的引用。

### 运行验收

- 首节点 Head 能正确对齐 Battle 入口目标；
- Head→Combat 后 CombatArea 能覆盖固定战斗区域；
- 战斗期间 Player、Enemy、Battle Camera 坐标保持固定；
- 战斗结束后 Combat→Tail 正常完成；
- Tail 到目标 Head 的转向和移动符合道路形状；
- 目标节点能继续执行 Head→Combat；
- 多个入口指向同一 Head 时最终姿态一致；
- 终点节点能正常结算；
- 暂停、失败、重开和路线场景卸载不残留旧场景对象。

### 美术验收

- 移动过程中道路没有明显断裂、穿模或跳跃；
- 节点转场不会暴露场景外空白区域；
- CombatArea 在 Battle Camera 下具有足够的前景、中景和背景层次；
- 材质、贴图、光照和透明效果在 Unity Built-in Render Pipeline 下正常；
- 场景模型不包含错误的 Battle 系统对象；
- 移动速度 1.5 秒级别的测试演出下，路线视觉连贯。

## 10. 禁止事项

- 不要制作“玩家移动后再把玩家拉回固定点”的混合方案；
- 不要把路线移动写成固定 `Vector3.back` 或固定距离；
- 不要假设所有节点共线；
- 不要用视频替代真实路线场景；
- 不要将每个节点制作成必须单独加载的 Unity Scene；
- 不要把 Head、CombatArea、Tail 的坐标写死在 C#；
- 不要让 Blender 文件中的不可导出约束成为运行时必需条件；
- 不要修改 Battle.scene 的 Player、Enemy 或 Camera 位置来适配单个节点；
- 不要用 `Resources.Load`、字符串 ID 或静态缓存寻找场景对象；
- 不要在没有验证碰撞、材质和路径点的情况下直接交付模型。

## 11. 给 Blender AI 的最小执行指令

请制作一张可导入 Unity 的路线场景，遵循以下结构：

```text
RouteStageRoot
├─ CombatNode_A
│  ├─ HeadJunction
│  ├─ CombatArea
│  ├─ TailJunction
│  ├─ HeadToCombatPath/P00...
│  └─ CombatToTailPath/P00...
├─ CombatNode_B
│  ├─ HeadJunction
│  ├─ CombatArea
│  ├─ TailJunction
│  ├─ HeadToCombatPath/P00...
│  └─ CombatToTailPath/P00...
└─ Connections
   └─ A_to_B/TravelPath/P00...
```

场景必须适配“RouteStageRoot 移动、Player 固定”的运行方式。请把路线锚点、路径控制点和环境模型作为可追踪的独立对象交付，不要创建 Player、Enemy 或摄像机，不要用视频，不要依赖不可导出的 Blender 逻辑。交付时同时提供对象清单和每条路径的首尾对应关系。
