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
updatedAt: 1785846924403
---

# pixel-hit-effect-runtime

## Summary
程序化像素命中特效的对象池、运行时 Sprite、Billboard、DOTween 生命周期、可见性修复与后续接触点/分技能造型约束。

<!-- locus:body:start -->
## 当前实现
- `Assets/Scripts/Core/PixelHitEffectManager.cs` 由 `HitFeedbackManager.Trigger` 统一调用，仅对 Standard/Heavy 且非 DoT 命中生成纯表现特效，不参与伤害、命中或位移判定。
- 管理器挂在 `Assets/Scenes/Battle.scene` 的 `Manager`，使用 12 个预创建实例的对象池；每个实例由 1 个中心 SpriteRenderer 和 4 个射线 SpriteRenderer 组成。
- Sprite 在运行时通过 2×2 纯白 Texture2D 创建，使用 Point Filter 和 2 PPU，避免最初 16 PPU 导致世界尺寸只有预期 1/8、实际仅数像素而不可见。
- 实例以 `Quaternion.LookRotation(-Camera.main.transform.forward, Camera.main.transform.up)` 面向相机，并沿相机 forward 偏移少量深度；渲染保持 Default Sorting Layer，sortingOrder=1。
- DOTween 使用 unscaled update，驱动中心菱形缩放收束和四向射线扩散；Sequence 的 OnComplete/OnKill 都通过 owner 校验收敛到统一回收，避免旧序列误回收复用实例。

## 已验证
- Play Mode 下单次请求会激活 5 个 SpriteRenderer，约 0.3 秒内恢复为 0；对象池数量保持 12→12。
- 当前 Standard 中心约 7 像素起步并扩张；用户已确认实战可观测。

## 调优约束
- 新特效不可反向驱动伤害时机或位移。
- 新视觉不可只依赖 `enemy.transform.position` 冒充武器接触点；需要由攻击执行路径提供接触位置与方向。
- Stab、Slash、Pierce 后续应采用不同形状，而不只是换色；保留同一对象池和程序化像素 Sprite 的实现方式。
<!-- locus:body:end -->
