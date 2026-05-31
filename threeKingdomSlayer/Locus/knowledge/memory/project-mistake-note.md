---
id: kd_4a9116b1-c70a-4de3-8eeb-801deb71c4fe
type: memory
path: project-mistake-note.md
title: project-mistake-note
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1778764012219
updatedAt: 1780242263649
---

# project-mistake-note

## Summary
更新至 2025-08-09 — 新增折返波混白透明 + LineRenderer BIRP遮挡 + 击退后攻击范围重检规则

<!-- locus:body:start -->
### Stab Wave 视觉旅行方向错误 ✅ 已修复（2025-07-18）
- 症状：戳击 wave 视觉上穿过空排飞到错误位置，而非走到目标敌人处。当 row0 有敌人、rows 1-2 为空时，wave 反向飞向 row3 方向
- 根因：`AttackWave.SetupTravel` 中 stab 用 `closestZ` 判断方向，`endTravelZ = closestZ ± 2.5` 的设计假设 wave 在目标前方足够远处生成。但实际 wave 固定生成在 Z=0.5（prefab Z + zOffset），而敌人可能处于负 Z。当 startZ(0.5) > targetZ(-1.0) 时 `closestZ + 2.5 = 1.5`，DOTween 从 0.5→1.5 正方向移动，与目标方向相反
- 修复：stab 改为向 **最远目标**（furthestZ）方向旅行，`endTravelZ = furthestZ`，wave 从 player 侧直走到范围内最远敌人处再收回。当 rangeRows 增大（Buff）时自然走到新范围内最远排。非 stab（Pierce/Sweep）逻辑不变
- 预防规则：Travel 型 wave 的 `startZ`（固定 spawn 点）和 `endTravelZ` 必须确保在空间同一侧，否则 DOTween 移动方向与视觉预期相反
- 文件：`Assets/Scripts/Attack/AttackWave.cs` (SetupTravel)

### 通过 unity_execute 创建 Prefab 后未串接 Prefab 引用 ✅ 已修复（2025-08-08）
- 症状：Enemy_105 播放攻击动画，但场景中看不到 arrow 飞行物。Console 无报错
- 根因：Enemy_105.prefab 通过 unity_execute 脚本创建时，`projectilePrefab` 字段未被赋值（保持 None）。`SpawnProjectile()` 中 `Instantiate(projectilePrefab)` 传入 null，Unity 静默返回 null（不抛异常），导致箭矢从未生成
- 修复：unity_execute 中显式 `LoadAssetAtPath<GameObject>("Assets/Prefabs/arrow.prefab")` 并赋值 `enemy.projectilePrefab = arrowPrefab`
- 预防规则：**通过 unity_execute 创建 Prefab 时，所有需要引用其他 Prefab/Asset 的字段必须在同一脚本中显式串接**。这与「代码创建GameObject未串接组件字段」同源，但特指跨 Prefab 的 Asset 引用
- 文件：`Assets/Resources/EnemyPrefabs/Enemy_105.prefab`、`Assets/Scripts/Enemy/Enemy.cs` (SpawnProjectile)

### Walk 动画只显示 walk1、不显示 walk2 ✅ 已修复（2025-08-08）
- 症状：敌人补齐移动时只看到迈左脚（walk1），从未看到迈右脚（walk2）
- 根因：Walk clip 时长 0.6s（walk1@0s, walk2@0.3s, loop），moveSpeed=0.2-0.3s。`moveProgress >= 1f` 时 `_animator.Play("Idle")` 强制切出，此时 walk2 尚未达到渲染时刻。0.6s 的 clip 全程只放了 0.2-0.3s
- 修复：`StartMoving` 中设置 `_animator.speed = max(1f, 0.6f / moveSpeed)` 加速 Walk 动画，确保在 moveSpeed 时间内至少完成一次完整循环（两帧都已渲染）
- 预防规则：**Sprite-swap AnimationClip 的时长必须 ≤ 驱动它的状态持续时间**。当 clip 时长 > 运动时长时，animator speed 必须按比例加速，否则后半帧永远不会被渲染
- 文件：`Assets/Scripts/Enemy/Enemy.cs` (StartMoving, UpdateMovement)

### 被击退敌人原地踏步播放 Walk ✅ 已修复（2025-08-08）
- 症状：Push 技能击退敌人后，被击退的敌人一直循环 Walk 动画，即使已到达攻击范围
- 根因：`_animator.SetTrigger("Walk")` 设置 trigger 后，`_animator.Play("Idle")` 强制切回 Idle，但 trigger 未被 Reset。Animator 的 trigger 在未通过转移消费时不会自动清除，后续可能在 Idle 状态下残留触发 Idle→Walk 转移
- 修复：移动完成时在 `Play("Idle")` 前先调用 `_animator.ResetTrigger("Walk")`；Stun 打断 rush 时同样 ResetTrigger
- 预防规则：**使用 `SetTrigger` + `Play()` 强制切状态时，必须在 `Play()` 前先 `ResetTrigger` 清理 trigger**。Animator trigger 依赖转移消费机制，`Play()` 不走转移故不会消费 trigger
- 文件：`Assets/Scripts/Enemy/Enemy.cs` (UpdateMovement, Stun)

### 折返波视觉不可见 — 颜色混白导致透明 ✅ 已修复（2025-08-09）
- 症状：折返波触发时只有伤害数字，完全看不到波本身
- 根因：`AttackWave.CreateInternal` 中非幻影路径执行 `Color.Lerp(color, Color.white, 0.5f)` 将颜色与白色50%混合，导致折返波几乎不可见。折返波未传 `materialOverride`，走了正常攻击波的混白逻辑
- 修复：`CreateInternal` 新增 `colorOverride` 参数 + `shouldReturnWave` 分支跳过混白，直接用传入颜色。`ExecuteReturnWave` 传入青蓝色 `(0.2, 0.7, 1.0)`
- 预防规则：**折返波/被动触发的特效波不应走正常攻击的颜色混白逻辑**。被动波需要独立的颜色控制路径
- 文件：`Assets/Scripts/Attack/AttackWave.cs` (CreateInternal, CreateReturnWave)、`Assets/Scripts/Player/AttackSystem.cs` (ExecuteReturnWave)

### LineRenderer 在 BIRP 下不可见 — 被 Sprite 遮挡 ✅ 已修复（2025-08-09）
- 症状：连锁弹射触发时无闪电连线，只有伤害数字
- 根因：`CreateChainVisual` 使用 `LineRenderer` + `Unlit/Color` 材质。在 BIRP 中 LineRenderer 是 3D 渲染器，默认 sortingOrder=0，会被同场景的 SpriteRenderer（敌人/波）遮挡。即便设了 sortingOrder，Unlit/Color shader 与 sprite 渲染管线配合不可靠
- 修复：废弃 LineRenderer，改用 `Chain.prefab`（16×16 链条 sprite）。实例化后紫色调 `(0.7, 0.3, 1.0)`、拉伸 X 匹配距离、旋转指向方向、sortingOrder=100、0.35s 渐隐
- 预防规则：**在 BIRP 的 2D 为主场景中，特效连线应优先使用 SpriteRenderer + 美术素材拉伸，而非 LineRenderer**。LineRenderer 的排序层与 sprite 渲染管线交互不可靠
- 文件：`Assets/Scripts/Player/AttackSystem.cs` (CreateChainVisual)、`Assets/Prefabs/Chain.prefab`

### 击退后敌人不重检攻击范围 ✅ 已修复（2025-08-09）
- 症状：敌人被击退后停留在原位不移动，也不攻击玩家
- 根因：`ExecutePush` 更新了 rowIndex 但未通知 Enemy 重检是否仍在攻击范围内
- 修复：`Enemy.RecheckAttackRange()` — 被推离攻击范围→取消攻击+补齐前进；仍在范围内→直接攻击。`ExecutePush` 末尾对每个被击退敌人调用此方法
- 文件：`Assets/Scripts/Core/ColumnManager.cs` (ExecutePush)、`Assets/Scripts/Enemy/Enemy.cs` (RecheckAttackRange)
<!-- locus:body:end -->
