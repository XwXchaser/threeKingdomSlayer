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
updatedAt: 1780169738871
---

# project-mistake-note

## Summary
更新至 2025-08-07 — 新增 Animator AnyState 转移错误打断动画规则 + 敌人动画状态机设计规范 + EnemySpriteController 空实现问题

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
<!-- locus:body:end -->
