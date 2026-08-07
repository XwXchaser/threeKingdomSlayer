---
id: kd_4a9116b1-c70a-4de3-8eeb-801deb71c4fe
type: memory
path: project-mistake-note.md
title: project-mistake-note
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
aiMaintained: true
explicitMaintenanceRules: true
createdAt: 1778764012219
updatedAt: 1786103290906
---

# project-mistake-note

## Summary
更新至 2026-03 — 新增 TimedArrow 命中与视觉生命周期、随机轨迹、DOTween 清理、受击缩放、序列化迁移、视觉/伤害解耦及 Time 被动类型区分经验

<!-- locus:maintain-rules:start -->
- Keep only durable and reusable project memory
- Consolidate duplicates or conflicts into the latest conclusion
- Remove temporary context, one-off tasks, and unsupported guesses
<!-- locus:maintain-rules:end -->

<!-- locus:body:start -->
### Slash命中特效左右镜像与视觉偏移必须分离坐标语义 ✅ 已修复
- 症状：Slash 左右方向的火星移动方向可以镜像，但火星造型仍保持同一朝向；尝试把命中特效根节点移动到视觉起点后，偏移效果不明显，甚至容易影响命中表现。
- 根因：
  1. 命中特效根节点同时承担世界位置、相机朝向和子节点局部坐标。相机采用 `Quaternion.LookRotation(-Camera.main.transform.forward, Camera.main.transform.up)` 后，根节点局部 X 与世界 X 可能相反，不能直接把世界左右符号与局部旋转/`flipX` 混用。
  2. 火星的位移、旋转、分叉角度和 Sprite 图案镜像分别使用了不同方向基准，导致“运动镜像但造型不镜像”。
  3. 将根节点从起点移动到命中点会让所有子特效一起移动；如果同时再让子火星从命中点反向起步，两个动画会互相抵消，视觉上看不出偏移。
- 修复：
  1. 逻辑命中根节点固定在真实 `impactPosition`，新增 `VisualRoot` 作为纯视觉内容父节点；只有 `VisualRoot` 从视觉起点飞向命中点，不改变伤害、命中时机、位移或卡肉。
  2. Slash 方向先转换为命中特效根节点的局部方向，再由同一个 `travelSign` 同时驱动旋转、移动、分叉角度和 SpriteRenderer.flipX；不要分别从世界 X、相机屏幕 X 和局部 X 推导不同符号。
  3. 任何不对称程序化 Sprite 都必须显式验证左右两侧的图案朝向，不能只验证位置轨迹或旋转数值。
- 预防规则：**特效方向至少拆成“世界方向、相机屏幕方向、特效局部方向”三种语义；确定一个渲染坐标系后，运动、旋转、分叉和图案镜像必须共用同一方向基准。需要从起点飞到命中点时，根节点固定在命中点，移动独立的视觉子树，避免视觉动画与逻辑对象互相争夺 Transform。实现后必须通过运行时左右对照检查 Sprite 图案，而不能仅凭代码认为已镜像。**
- 文件：`Assets/Scripts/Attack/SweepEffect.cs`、`Assets/Scripts/Core/HitFeedbackManager.cs`、`Assets/Scripts/Core/PixelHitEffectManager.cs`、`Assets/Scripts/Enemy/Enemy.cs`、`Assets/Scripts/Enemy/SharedHealthGroup.cs`

### Slash 旋转角与左右镜像必须分离
- 错误做法：判断左向后直接将局部方向取反，再用取反后的向量计算 `Atan2`；左右 Slash 会得到相同旋转角。
- 正确做法：保留局部方向的 X 符号，用 `Atan2(y, Abs(x))` 计算斜向倾角，再将左右符号独立用于 `SpriteRenderer.flipX`。这样旋转负责“斜角”，镜像负责“左右造型”。
- 经验：任何方向性命中特效都要把“角度”和“镜像”作为两个独立输出，先做数学验证，再做左右运行时对照验收。

### 程序化 Sprite 内嵌碎片与运行时碎片必须先区分来源
- 症状：关闭 `BuildStabEffect()` 的 `instance.rays` 后，Stab 画面上的外围碎片仍存在。
- 根因：Stab 的碎片是 `CreateStabFrameSprite()` 最终帧调用 `DrawStabDebris()` 直接写入 Texture2D 的像素，不属于运行时 `SpriteRenderer` 碎片。
- 规则：修改“图片内部”的视觉元素前，先追踪它是 Texture2D 像素生成、运行时 SpriteRenderer，还是 Prefab 子物体；只修改其真实来源。对于当前 Stab，短时长下保留内嵌碎片的观感已验收。
<!-- locus:body:end -->
