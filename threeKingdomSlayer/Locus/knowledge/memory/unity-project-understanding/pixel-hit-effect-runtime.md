---
id: kd_dbe058ac-9f56-40da-beef-9250628b3aac
type: memory
path: unity-project-understanding/pixel-hit-effect-runtime.md
title: pixel-hit-effect-runtime
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1785846924403
updatedAt: 1786525618288
---

# pixel-hit-effect-runtime

## Summary
程序化像素命中特效的对象池、运行时 Sprite、Billboard、DOTween 生命周期、可见性修复与后续接触点/分技能造型约束。

<!-- locus:body:start -->
## 当前实现
- `Assets/Scripts/Core/PixelHitEffectManager.cs` 由 `HitFeedbackManager.Trigger` 统一调用，仅对 Standard/Heavy 且非 DoT 命中生成纯表现特效，不参与伤害、命中或位移判定。
- 管理器挂在 `Assets/Scenes/Battle.scene` 的 `Manager`，使用预创建实例对象池；每个实例由 1 个中心 SpriteRenderer 和 12 个射线 SpriteRenderer 组成。所有构建分支必须显式关闭未使用 Renderer，避免对象池复用残留。
- Sprite 在运行时通过 Texture2D 创建，使用 Point Filter 和 Clamp。Stab、Slash 使用多变体三帧程序化像素 Sprite，而非连续缩放的纯色 Quad。
- 实例面向相机并沿 camera forward 偏移少量深度；命中特效保持 Default Sorting Layer、`sortingOrder=1`。
- DOTween 使用 unscaled update；Sequence 的 OnComplete/OnKill 通过 owner 校验统一回收到对象池。

## Launch 专属命中特效
- `DamageType.Launch` 在 `Play()` 中进入 `BuildLaunchEffect`，不再使用通用四向爆裂。
- Launch 复用 Slash 三帧程序化像素爆裂主体，整体尺寸约 4.8 基准并带约 ±32° 的短时受控旋转。
- 最终验收版本关闭所有额外 ray renderer，仅保留三帧像素主体；此前拉伸十字射线会产生无像素块感的连续“面条”纹理，已移除。
- `AttackWave.HitTarget` 在 Launch 时向 `Enemy.TakeDamage` 传入相机上方向，供命中特效确定挑飞朝向。
- Launch 武器自身使用 `sortingOrder=2`，因此武器位于命中特效亮核之上，避免中心爆裂遮住枪身。

## 已验证
- `PixelHitEffectManager.cs` 与 Launch 相关代码无编译错误，Unity domain reload 成功。
- Launch 专属命中特效完成多轮视觉调整：移除黄色外围装饰、暗红十字延伸以及最终全部拉伸射线，仅保留放大的 Slash 像素主体。

## 调优约束
- 新特效不可反向驱动伤害时机或位移。
- 新视觉不可只依赖 `enemy.transform.position` 冒充武器接触点；攻击路径应提供接触位置与方向。当前 Launch 已提供方向，接触位置仍使用 Enemy 默认 body offset。
- Stab、Slash、Launch 应采用可辨识的不同轮廓与节奏，而不只是换色；保留同一对象池和程序化像素 Sprite 的实现方式。
- 修改对象池射线数量或使用方式后，所有构建方法必须显式关闭未使用 Renderer。
- 不要通过极窄 SpriteRenderer 长条的连续缩放模拟像素爆裂；高速移动时会呈现平滑“面条”而不是离散像素块。优先使用完整程序化 Sprite 帧或短促块状碎片。
- 程序化 Stab 形状调整仍以 `C:/Users/steam/Pictures/gptGen/stab_hit_effect_concept_v3.png` 为参考，不能自行生成替代参考图。
<!-- locus:body:end -->
