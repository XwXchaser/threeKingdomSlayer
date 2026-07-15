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
updatedAt: 1783787131044
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
### TimedArrow：只调整命中高度，无法修复“箭仍飞到脚底” ✅ 已修复（2026-03）
- 症状：增大提前命中值后，伤害可能已经提前结算，但箭矢视觉仍飞到敌人脚底才消失。
- 根因：伤害判定与飞行 Tween 生命周期相互独立；修改判定阈值不会自动停止或销毁视觉。
- 修复：命中时暂停飞行 Tween，播放短距离穿入和所有子 SpriteRenderer 的淡出，并在 Complete/Kill 两条路径统一销毁。
- 预防规则：**投射物的命中结算与命中视觉结束必须分阶段设计；调判定参数前先确认视觉 Tween 是否响应命中状态。**
- 文件：`Assets/Scripts/Effect/TimedArrowEffect.cs`

### DOTween 状态切换必须覆盖所有清理路径 ✅ 已修复（2026-03）
- 症状：命中箭偶发没有及时消失，或根 Sprite 已淡出但子 Sprite 仍可见。
- 根因：只处理根 SpriteRenderer，且原飞行 Tween、新命中序列和 GameObject 的清理所有权不明确。
- 修复：缓存所有子 SpriteRenderer；命中序列的 `OnComplete` 与 `OnKill` 均停止原 Tween 并销毁箭矢。
- 预防规则：**替换 Tween 状态时要明确旧 Tween、新 Tween、对象三者所有权；完成、Kill、外部销毁必须收敛到相同结果。**
- 文件：`Assets/Scripts/Effect/TimedArrowEffect.cs`

### 阵型技能的水平命中与垂直命中应采用不同坐标语义 ✅ 已修复（2026-03）
- 症状：敌人因攻击动画稍微位移就落空；敌人被击飞后，箭矢仍按地面高度触发。
- 根因：瞬时世界位置同时承担阵型覆盖和身体接触点，无法兼顾稳定格位与实时高度。
- 修复：XZ 使用稳定阵型格坐标并加入移动容差；Y 使用敌人实时世界高度、身体偏移和提前接触高度。
- 预防规则：**阵型范围技能应拆分坐标语义：XZ 保持格位稳定，Y 跟随实时 Transform。**
- 文件：`Assets/Scripts/Effect/TimedArrowEffect.cs`

### 起点与落点独立随机会造成箭矢横跨屏幕 ✅ 已修复（2026-03）
- 症状：从屏幕左上出现的箭矢可能飞向右侧，轨迹不像从上方落下。
- 根因：起点 X 和落点 X 独立采样，横向差值可能接近整个战场宽度；调整旋转无法修复轨迹本身。
- 修复：先生成目标 X，再围绕目标 X 施加有限的起点抖动，并约束斜视画面中的视觉朝向。
- 预防规则：**需要近似垂直下落时，随机端点必须相关；先定目标，再围绕目标生成起点。**
- 文件：`Assets/Scripts/Effect/TimedArrowEffect.cs`

### 高频受击缩放不能基于当前 Scale 相对叠加 ✅ 已修复（2026-03）
- 症状：敌人被箭雨连续命中后体型持续变大。
- 根因：新受击 Tween 从已放大的当前 Scale 继续计算，多个相对缩放反馈发生累积。
- 修复：每次反馈先停止旧 Tween、恢复缓存的原始 Scale，再播放固定目标倍率并回归原值；禁用、死亡、击飞和对象池回收时统一清理。
- 预防规则：**高频可重入反馈必须从缓存基准值重新开始，不能以当前 Transform 做相对叠加。**
- 文件：`Assets/Scripts/Enemy/Enemy.cs`

### C# 默认值不会迁移 Prefab 已序列化字段 ✅ 已处理（2026-03）
- 症状：发射字段改为 `volleyInterval` 后，只改代码默认值不能保证 Prefab 使用新参数。
- 根因：Unity 已序列化值不会因脚本字段默认值变化而自动更新；字段删除或替换还需要重新编译和保存资产。
- 修复：执行脚本重编译，通过 Unity API 明确写入 Prefab，再用 YAML 工具复核实际值。
- 预防规则：**序列化字段新增、删除或语义改变后，必须执行“重编译 → Unity API 写资产 → YAML/Inspector 复核”。**
- 文件：`Assets/Scripts/Effect/TimedArrowEffect.cs`、`Assets/Prefabs/Effects/ArrowRainEffect.prefab`

### 视觉箭数量与伤害载体数量必须解耦 ✅ 已处理（2026-03）
- 症状：直接把每波箭数从4增加到8，会令伤害和命中次数同步翻倍。
- 根因：视觉实例与伤害实例共用同一生成循环和命中逻辑。
- 修复：每波生成8支箭，其中固定4支参与伤害判定，其余4支只播放飞行、落地和淡出。
- 预防规则：**表现密度和数值载体应在生成入口显式区分；不要在结算后再补偿总伤害。**
- 文件：`Assets/Scripts/Effect/TimedArrowEffect.cs`

### Time 被动首次触发必须区分即时型与叠层型 ✅ 已处理（2026-03）
- 症状：若所有 Time 类型首次获得都直接释放，蓄力冲击波会破坏队列语义，甚至同时授层和释放。
- 根因：`TimedPassiveModule` 同时管理普通即时效果和 `charge_shockwave` 计时叠层效果。
- 修复：普通 Time 效果首次注册时立即调用现有 `SpawnEffect()`，然后开始完整周期；`charge_shockwave` 只立即授予1层。升级已有技能不免费触发，也不重置剩余时间。
- 预防规则：**修改统一计时框架前必须枚举各 effectType 的触发语义；即时型、叠层型和消费型不能仅因共用 timer 而采用相同策略。**
- 文件：`Assets/Scripts/Core/TimedPassiveModule.cs`

### UI新特效难以在目标位置验证时，先在屏幕中心做 debug target 独立验证渲染链路 ✅ 已验证（2025-01）
- 症状：给头像背后加火焰特效，多次调整后完全不可见，无法判断是渲染问题还是逻辑问题
- 根因：特效直接挂在目标位置（头像旁）时，可能被遮挡、alpha=0、CanvasGroup 隐藏、sibling order 错误等多重因素同时影响，无法逐一排查
- 修复：在屏幕中心放置一个独立的 debug target（ReadyFireEffect_Debug），先确认该 target 可见、粒子系统/帧动画正常工作，再回迁到目标位置
- 预防规则：**当 UI 新特效在目标位置反复不可见时，不要盲目调整参数。先在屏幕中心创建一个独立 debug target 验证渲染/逻辑链路完整，确认可见后再迁移到目标位置并调整参数。验证完成后务必移除 debug target**
- 文件：`Assets/Scripts/UI/UIReadyFireEffect.cs`, `Assets/Scripts/UI/UltimateButtonUI.cs`, `Assets/Scenes/Battle.scene`

### HUD 头像火焰特效挂在外部平级节点，运行时容易“逻辑已触发但看起来没生成” ✅ 已修复（2025-12）
- 症状：大招充能满后逻辑已触发，甚至中心调试 target 可见，但头像位置始终观测不到火焰，容易误判为特效逻辑失效
- 根因：`ReadyFireEffect` 初期挂在 `Health(Slider)` 下，和 `UltPortraitButton` 是平级关系，只能靠外部 offset 对齐头像；一旦头像布局、尺寸、层级发生变化，特效就可能偏离头像、被遮挡，或虽然存在但不在预期区域
- 修复：将火焰节点迁入 `UltPortraitButton` 内部，作为头像按钮的子物体，并放在 `UltBase` / `UltFill` / `Head` 之前渲染；位置和尺寸改为在头像局部空间内调整
- 预防规则：**所有“附着在某个 UI 元素上”的持续特效，都应优先挂在该 UI 元素内部，而不是挂在外部父节点后靠 anchoredPosition/offset 对齐。只要需求是“跟着某个 UI 元素走并稳定处于其前后层级”，就必须优先保证层级归属正确，再谈参数微调**
- 文件：`Assets/Scenes/Battle.scene`, `Assets/Scripts/UI/UltimateButtonUI.cs`, `Assets/Scripts/UI/UIReadyFireEffect.cs`
<!-- locus:body:end -->
