---
id: kd_1c1fd8c8-6bd3-4f1e-84c3-abf354e5d7ac
injectMode: inherit
summary: 箭雨固定发射 `4 × arrowCount` 支箭覆盖前方区域；起点 X 关联落点并由 `xJitter`限制横移，视觉朝向锁定 Y/Z 平面，箭尖接触落点平面时提前判伤。
aiMaintained: inherit
---

# Timed Arrow Targeting

## Current Behavior
- `Assets/Scripts/Effect/TimedArrowEffect.cs`
- `Play(rowCount, arrowCount, damage)`：`rowCount` 是前方覆盖区域深度，`arrowCount` 是密度基数。
- 不预采样敌人；空区域也会完整生成箭雨。
- 固定生成 `4 × arrowCount` 支箭，单箭伤害为 `max(1, damage / 4)`。
- 落点使用战场阵列根世界偏移 + `StageController` 阵型局部公式。
- 先随机落点 X，再用 `startX = clamp(targetX ± xJitter)`生成起点，限制单箭横向位移。
- 箭矢按少量齐射批次从玩家后方纵深阵列发射；起点高度和飞行时长带轻微随机。
- 飞行由单一进度 Tween 计算抛物线位置；视觉朝向锁定 Y/Z 平面，横向位移不驱动 Sprite yaw，`rotJitter`只追加局部滚转。
- `arrowTipDistance`表示箭矢中心到箭尖的世界距离。下降过程中箭尖首次到达落点 Y 平面时，立即按原始 `targetPos`执行范围伤害；箭身继续完成原轨迹并淡出。
- 每箭使用独立标记保证只判伤一次；飞行完成回调仅作为异常轨迹兜底。
- Boss 仅在 `bossState == InCombat` 时可被命中。

## Caller Paths
- `Assets/Scripts/Core/TimedPassiveModule.cs` → `TimedArrowEffect.Play(cfg.rowCount, cfg.arrowCount, cfg.damage)`
- `Assets/Scripts/Core/PassiveTriggerModule.cs` → `TimedArrowEffect.Play(cfg.rowCount, cfg.arrowCount, cfg.damage)`

## Key Prefab Parameters
- `Assets/Prefabs/Effects/ArrowRainEffect.prefab`
- `xJitter`：起点相对落点的最大横向偏移。
- `spreadX` / `spreadZ`：落点覆盖区域扩展。
- `arrowTipDistance`：箭尖接触提前量；越大越早判伤。
- `impactRadius`：落点范围伤害半径。

## Previous Gotcha
- 预采样敌人会导致空区域取消箭雨、总箭数随敌人数波动。
- 用 `z >= targetZ`提前判伤在负 Z 战场会错误触发。
- 起点 X 与落点 X 独立全宽随机会产生跨屏横飞。
- 完整 3D yaw 在斜视相机和预旋转 Sprite 下会表现为左右歪斜。
- 等箭矢 Transform 中心到达落点才判伤，会让长且居中 Pivot 的 Sprite 看起来已经扎入敌人后才产生反馈；应使用箭尖接触平面判定。
