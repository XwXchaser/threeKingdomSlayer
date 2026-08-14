---
id: kd_19d3b3c2-00e3-4913-911d-45d93cffa451
injectMode: inherit
summary: 蓄力 Stab 前端可视化 + Launch 衔接：指尖平面跟随/左右clamp+Zroll/上下Xpitch，Launch接管pose+三帧精灵序列charge2→charge1→stab
aiMaintained: inherit
---

# Charge Stab Visual System

## Overview
蓄力时在世界空间显示 Stab 武器精灵。视觉中心跟随指尖投射到玩家附近的相机平行平面；X 轴位置按 `halfWidth` clamp；左右移动驱动 Z 轴 roll；上下移动按出现后的相对位置驱动 X 轴 pitch。Launch 释放时读取并接管蓄力视觉的实时位置、旋转和缩放。

## Launch 视觉架构（已验收）
- `Assets/Scripts/Effects/LaunchVisualEffect.cs` 独立管理 Launch 视觉生命周期；`AttackSystem` 只提供三帧 Sprite、技能配置和命中回调。
- 层级为 `Root(握持支点位移) → Pivot(枪身旋转) → Weapon(Sprite 偏移)`；支点从枪尾向枪身中心回收 40%。
- 动作阶段为 Windup（后撤下旋）→ Thrust（分段贝塞尔弧线上挑）→ Hold（短暂停留）→ Retract（反向采样同一弧线快速回手并淡出）。不存在额外的命中后二次上挑。
- Thrust 在时间轴 48% 触发 impact 回调，`AttackSystem` 此时才创建透明 Launch AttackWave 并结算伤害/击飞，保证枪身与敌人起飞方向同步。
- 位移与旋转错峰：命中前位移先启动，旋转短暂滞后后追上；命中后自然减速到最高点。
- 左右蓄力位置通过 `sideRatio` 驱动轨迹侧移与侧倾；每次 Launch 额外产生约 8.25°–15° 的随机左右终态倾角，蓄势阶段只应用该随机量的 12%。
- Windup 使用 `Quaternion.SlerpUnclamped` 直接插值四元数，避免欧拉角跨 0°/360° 时偶发绕转或翻转。
- `WeaponMotionBlurController.UpdateMotionWorld` 用世界空间位移和 `Quaternion.Angle` 计算线速度/角速度；Launch 使用较高模糊响应与 56px 上限。
- 当前 `ObservationScale = 1.5f`，动作锁通过 `GetObservationDuration()` 至少覆盖完整 Launch 视觉时长。
- Launch 武器 `sortingOrder = 2`，命中特效为 `sortingOrder = 1`，枪身显示在特效中心亮核之上。

## Launch 命中特效
- `PixelHitEffectManager.BuildLaunchEffect` 复用 Slash 的三帧程序化像素爆裂主体，整体尺寸更大并带受控旋转。
- 不使用拉伸射线或十字延伸，避免连续细长纹理产生“面条”感；对象池中的所有 ray renderer 在 Launch 分支显式关闭。
- `AttackWave` 为 Launch 命中反馈传入相机上方向，特效朝向与挑飞方向一致。

## 行为流程
```
蓄力出现 → 跟随指尖位置/旋转
Launch 释放 → TryGetCurrentVisualPose 读取实时 pose
            → SuppressFadeAndDestroy 销毁旧蓄力视觉
            → LaunchVisualEffect 从该 pose 后撤下旋
            → 分段贝塞尔弧线上挑；48% 时结算伤害/击飞
            → 到达最高点后反向采样弧线快速收招并淡出
```

## ChargeStabVisual API
| 方法 | 说明 |
|------|------|
| `TryGetCurrentVisualPose(out pos, out rot, out scale)` | 读取当前蓄力视觉实例的世界位置/旋转/缩放，无实例返回 false |
| `SuppressFadeAndDestroy()` | 跳过渐隐直接销毁蓄力视觉，供 Launch 接管 |

## 关键文件与资产
- `Assets/Scripts/Effects/ChargeStabVisual.cs` — 蓄力世界空间视觉与实时 pose API
- `Assets/Scripts/Effects/LaunchVisualEffect.cs` — Launch 动作时间线、弧线、旋转、收招和模糊
- `Assets/Scripts/Effects/WeaponMotionBlurController.cs` — 武器方向性像素模糊
- `Assets/Scripts/Player/AttackSystem.cs` — Launch 执行入口与命中回调
- `Assets/Scripts/Core/PixelHitEffectManager.cs` — Launch 专属命中特效
- `Assets/Prefabs/UI/Skills/Zhangfei_Launch.asset` — `launchFlickDuration=0.25`、`launchWindupDuration=0.13`、`launchWindupDistance=0.42`、`launchSideTilt=12`、`launchAngleVariance=15`
- `Assets/Sprites/zhangfei/stab_charge1.png`、`stab_charge2.png`、`stab.png` — Launch 三帧 Sprite

## 注意
- 不要将蓄势目标四���数转成 Euler 后交给 `DOLocalRotate`；实时蓄力 pose 可能位于欧拉角奇异/环绕区间，必须直接进行四元数插值。
- 随机倾角应主要体现在上挑终态，不能在 Windup 阶段完整应用，否则会破坏既定蓄势语言。
- Launch 视觉、伤害与击飞通过 impact 回调同步，但视觉不得反向依赖敌人状态或 AttackWave 生命周期。
- 无蓄力视觉时仍保留固定玩家 offset 与自动缩放 fallback。
