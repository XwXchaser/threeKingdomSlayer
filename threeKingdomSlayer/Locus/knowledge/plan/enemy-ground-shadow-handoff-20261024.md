---
id: kd_3663244c-4e7f-4746-9e19-5c17edb45bee
injectMode: inherit
injectAgents:
- unity
aiEditMode: confirm
---

# 敌人脚下阴影交接（2026-10-24）

## 目标

给所有敌人单位脚下加一个贴地椭圆阴影。上一轮长会话因中转站连续 400 中断，阴影未验收。只在新会话继续，不要回旧会话，也不要查旧会话日志或会话数据。

## 已完成并验证

### 1. 移除敌人攻击前冲（已完成，与阴影无关）

- `Assets/Scripts/Enemy/Enemy.Attack.cs`：近战前摇/收招的两段 `DOLocalMoveZ` 改为 `AppendInterval`，时长保留，命中帧与预警时序不变，`useFlip` 镜像翻转保留。
- `Assets/Scripts/Enemy/CavalryEnemy.Charge.cs`：`StrikeRoutine` 删除前探与收招位移，只留计时与命中判定。
- `Assets/Scripts/Enemy/CavalryEnemy.cs`：删除 `strikeLungeDistance` 字段。`Enemy_109.prefab` 里残留的 `strikeLungeDistance: 0.5` 会被 Unity 忽略，下次保存 Prefab 时清理。
- `unity_recompile` 通过，Console 无 error。未做 Play 目视验收。

### 2. 阴影组件（未验收）

`Assets/Scripts/Enemy/EnemyGroundShadow.cs`，由 `Enemy.Init.cs` 的 `Awake` 运行时 `AddComponent`，不改任何 Prefab。

- 阴影是场景根上的独立对象（`HideFlags.DontSave`），不挂在敌人下，不继承弹跳与缩放。
- 贴图与材质全类静态共享，不为每个敌人新建材质。
- 绕 X 轴旋转 90° 平躺在 XZ 地面。
- 宽度按敌人剪影世界宽度 × `widthToSilhouette`（当前 0.66）推算，109 等长躯体会自动变大。
- 对象池回收时 `OnDisable` 隐藏，销毁时一并销毁。

## 本轮已改（2026-10-24，未做 Game 视图目视验收）

`Assets/Scripts/Enemy/EnemyGroundShadow.cs` 不再用网格顶点定脚底。

- 脚底改为不透明像素最低行（alpha > 16），按精灵缓存。贴图不可读时用临时 RenderTexture 读回，不改导入设置。
- 同一 clip 锁定第一次采到的像素脚底，换 clip 才重算。死区平滑已删除。
- 宽度同样改用该精灵的不透明像素跨度。
- 109 正式骑乘用根节点人马合一图，不是 `MountVisual`。根渲染器关闭时才改读 `mountRenderer`。`CavalryVisualController.mountRenderer` 改为 public。

### 实测（Play Mode，临时实例，已销毁）

| 单位 | 阴影 Y | 跨帧抖动 | 宽度 |
|---|---|---|---|
| 1011 idle / attack | -1.649 | 0.000 | 1.90 |
| 109 骑乘（根节点抬到 visualYOffset 0.794） | -1.790 | 0.000 | 1.52 |

- 1011 的 `visualYOffset` 把网格脚底抬到 -1.800。不透明像素脚底比网格高 0.151，所以阴影在 -1.649，不是贴地面线。这是贴视觉脚底，不是贴网格。
- 1011 idle 六帧像素脚底本地值 -21.50 ~ -21.56，世界差约 0.005，锁定后不再跳。
- 109 的 `MountVisual` 在正式 Prefab 上是关闭的。骑乘可见图是 `Enemy_109_MountedIdle`，马腿就在这张图里。
- 109 宽度 1.52 小于 1011 的 1.90。比例同为 0.66。用户要求 109 更大，这一项还没按目视加大。
- 没有真实波次入场。当前 Battle 的 `autoStartStage` 为 false，开战会重置玩家与路线，所以只用临时实例复现了 `visualYOffset`。

### 仍需用户看图

1. 1011 脚下空隙是否消失。数值上阴影比旧网格脚底高 0.15。
2. idle 是否还闪。数值上跨 8 帧抖动为 0。
3. 109 阴影是否还要再加大。当前按剪影比例，没有单独放大。

## 关键实测数据（Play Mode，Enemy_1011）

| 项 | 值 |
|---|---|
| 剪影世界宽度 | 2.56 ~ 4.14（随攻击帧变化） |
| 阴影世界宽度 | 2.13（`widthToSilhouette = 0.66`） |
| 阴影旋转 | (90, 0, 0) |
| 阴影纵深 | 0.53 ~ 0.71 |
| idle 六帧脚底世界偏移 | -0.1134 ~ -0.1329 |

用户要求阴影至少比当时的版本大 1.4 倍，0.66 是按这个倍数反推的，**视觉上是否够大未经用户确认**。

## 镜头

`Main Camera`：透视，FOV 60，位置 (0, 3, -10)，俯仰 18°。`Sample Camera` 禁用。阴影平躺后由透视自然压扁，不需要再手调高宽比。

## 相关但未改的文件

- `Assets/Scripts/Enemy/EnemyGroundAligner.cs`：运行时脚底对齐，`groundY = -1.8`。**没有任何敌人 Prefab 挂了这个组件**，对齐实际靠各 Prefab 手填的 `visualYOffset`。
- `Assets/Scripts/Editor/EnemyGroundAlignTool.cs`：菜单 `Tools/敌人/脚底对齐 (Ground Align)`，按剪影重算 `visualYOffset`。
- `Locus/knowledge/memory/unity-project-understanding/enemy-ground-alignment.md`：脚底对齐体系的权威记录，含各敌人的偏移表。

## 建议的下一步

1. 先验证死区修复是否消除 idle 闪烁（进 Play，看 idle 与攻击两段）。
2. 若仍闪，改为在同一 `AnimationClip` 内取脚底中位数并锁定，clip 切换时才重算。
3. 脚底定位改用不透明像素最低行，或按画布脚底锚点，解决「贴不贴脚」的空隙。这是用户反馈最多次的问题，优先于尺寸。
4. 在真实入场的 109 上测 `MountVisual` 世界脚底，再决定 109 的阴影宽度与 Y。
5. 阴影尺寸与贴脚都要用户在 Game 视图确认，代理侧 `unity_capture_viewport` 不可用，无法自行截图。

## 会话中断记录

- 上一轮是长会话，压缩后整包请求被中转站拒绝，与本文件内容无关。
- 不要回那个旧会话，不要按会话编号或请求 ID 去查日志、数据库或会话文件。
- 任务状态以本文为准，在新会话里继续。
