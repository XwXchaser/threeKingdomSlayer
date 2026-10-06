---
id: kd_5b281b52-0ed2-4f6a-a5f2-69ab545003da
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 敌人攻击预警十字星与可视化点位规范

## 目的

敌人即将出招前，在敌人预警挂点显示三帧预警图像，帮助玩家读懂出招时机。正式视觉来自预生成的透明 Sprite，不由 Unity 运行时绘制十字星、圆环或像素图案。

## 预警挂点与默认位置

每个敌人 Prefab 必须提供独立的 `AttackTelegraphAnchor` 子物体：

```text
Enemy
└── AttackTelegraphAnchor
    ├── SpriteRenderer
    └── EnemyAttackTelegraph
```

`AttackTelegraphAnchor` 的 Transform 局部位置、旋转和基础缩放就是该敌人预警的默认点位。点位属于敌人 Prefab 本身，不是全局统一坐标，也不是运行时由代码推算出的临时位置。

默认点位通常放在敌人上半身至头部附近，但必须根据每个敌人的精灵锚点、身高、姿态和攻击构图单独校准。不同敌人可以保存不同的局部坐标。

运行时禁止通过代码自动覆盖 `AttackTelegraphAnchor` 的位置、旋转或基础缩放。战斗逻辑只读取并使用 Prefab 中已保存的挂点 Transform。

## Edit Mode 可视化调整

敌人 Prefab 或其实例可以直接拖入场景进行调整。设计师应能在不进入 Play Mode 的情况下：

1. 在 Inspector 的 `EnemyAttackTelegraph` 上切换预览帧；
2. 在 Scene 视图观察十字星相对于敌人身体的位置；
3. 移动 `AttackTelegraphAnchor` 调整预警点位；
4. 必要时调整挂点基础缩放；
5. 保存 Prefab，使该位置成为该敌人的默认点位。

预览帧只改变 `SpriteRenderer` 的显示 Sprite，不改变挂点 Transform，不触发攻击逻辑，也不写入运行时动画状态。

## 正式三帧

使用已验收组图从左起前三张，按顺序表达“即将出招”：

```text
Frame 1：棱形点
Frame 2：展开的十字星，使用原图约 75% 尺寸
Frame 3：完整十字星与其外侧紧贴圆环
```

第四张不部署。Frame 2 的缩小已经在独立 PNG 素材中完成，不依赖运行时缩放来修正构图。

正式资源：

- `Assets/Sprites/Enemy/AttackTelegraph/attack_telegraph_frame_1.png`
- `Assets/Sprites/Enemy/AttackTelegraph/attack_telegraph_frame_2.png`
- `Assets/Sprites/Enemy/AttackTelegraph/attack_telegraph_frame_3.png`

## 运行时播放规则

`EnemyAttackTelegraph.BeginWarning(duration)` 只负责：

- 显示预警；
- 播放三帧 Sprite；
- 控制每帧持续时间、透明度和结束淡出；
- 触发预警音效；
- 维持既有排序层和父级 Alpha 处理。

推荐默认时间比例：

```text
Frame 1：duration × 0.25
Frame 2：duration × 0.30
Frame 3：duration × 0.45
```

运行时不负责绘制或重建十字星、圆环，也不自动寻找其他位置替代 `AttackTelegraphAnchor`。

## 数据与维护边界

- 预警图像是独立 Sprite 资产，通过 Inspector 直接引用。
- 所有敌人 Prefab 可共享三张正式 Sprite，但每个 Prefab 保留自己的 `AttackTelegraphAnchor` 局部 Transform。
- 修改某个敌人的预警位置，应修改对应敌人 Prefab 或其明确的场景实例，不应通过全局静态缓存、字符串查找或 `Resources.Load` 重定向引用。
- 新增敌人时必须创建 `AttackTelegraphAnchor`、绑定三帧资源，并在 Scene 视图完成默认点位校准。
