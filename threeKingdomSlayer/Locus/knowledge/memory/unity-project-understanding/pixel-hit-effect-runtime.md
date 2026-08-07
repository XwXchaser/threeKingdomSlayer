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
updatedAt: 1786082148152
---

# pixel-hit-effect-runtime

## Summary
程序化像素命中特效的对象池、运行时 Sprite、Billboard、DOTween 生命周期、可见性修复与后续接触点/分技能造型约束。

<!-- locus:body:start -->
## 当前实现
- `Assets/Scripts/Core/PixelHitEffectManager.cs` 由 `HitFeedbackManager.Trigger` 统一调用，仅对 Standard/Heavy 且非 DoT 命中生成纯表现特效，不参与伤害、命中或位移判定。
- 管理器挂在 `Assets/Scenes/Battle.scene` 的 `Manager`，使用预创建实例对象池；每个实例由 1 个中心 SpriteRenderer 和 12 个射线 SpriteRenderer 组成。普通方向爆发和 Slash 仅启用所需的前 4 个/指定数量射线，避免对象池扩容后残留渲染。
- Sprite 在运行时通过 Texture2D 创建，使用 Point Filter 和 Clamp；Stab 命中特效使用 4 个变体、每个变体 3 帧的完整程序化像素 Sprite，而不是用独立色块拼装。形状以 `stab_hit_effect_concept_v3.png` 为参考：中心向四周爆发、错落长短尖刺、深棕轮廓、暗红/红色外缘、橙色中层、黄色内层和白色实心核心。
- Stab 每次命中轮换变体，并对局部尖刺角度/长度、整体缩放和旋转增加小范围受控随机；核心布局和中心位置保持稳定。爆发末帧额外包含少量红/棕碎屑。
- Stab 动画当前使用接触→展开→保持→衰减的慢速验收时序，普通约 0.45 秒、重击约 0.52 秒；程序化 Sprite 的 PPU 已从 44 调为 30，使视觉尺寸放大。
- 实例以 `Quaternion.LookRotation(-Camera.main.transform.forward, Camera.main.transform.up)` 面向相机，并沿相机 forward 偏移少量深度；渲染保持 Default Sorting Layer，sortingOrder=1。
- DOTween 使用 unscaled update，驱动中心程序化 Sprite 的阶段切换/缩放和碎屑扩散；Sequence 的 OnComplete/OnKill 都通过 owner 校验收敛到统一回收，避免旧序列误回收复用实例。

## 已验证
- `PixelHitEffectManager.cs` 无代码诊断错误/警告；Unity domain reload 编译成功。
- 已在编辑器中直接生成并读取慢速验收用 3 帧 Stab 程序化 Sprite 预览，文件位于 `Library/Locus/tmp/stab-refactor-v2-frame-0.png` 至 `stab-refactor-v2-frame-2.png`。预览确认中心已填充，不再是透明空心；尺寸和不规则放射轮廓已增强。

## 调优约束
- 新特效不可反向驱动伤害时机或位移。
- 新视觉不可只依赖 `enemy.transform.position` 冒充武器接触点；需要由攻击执行路径提供接触位置与方向。
- Stab、Slash、Pierce 后续应采用不同形状，而不只是换色；保留同一对象池和程序化像素 Sprite 的实现方式。
- 修改对象池射线数量后，所有使用射线的构建方法必须显式关闭未使用 Renderer，避免旧状态残留。
- 程序化形状调整必须以 `C:/Users/steam/Pictures/gptGen/stab_hit_effect_concept_v3.png` 为唯一造型参考，不能自行生成替代参考图。
<!-- locus:body:end -->
