---
id: kd_04b72df3-abb3-4ca2-941d-83941c56fa62
type: memory
path: phase3-development-summary.md
title: phase3-development-summary
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1779520344306
updatedAt: 1780201356044
---

# phase3-development-summary

## Summary
第三期局内成长系统 + 击杀进度条 + 敌人动画状态机统一 + 通关印章效果，已完成。更新至 2025-08-08 — 新增 victory 通关印章（真三国无双风格）。

<!-- locus:body:start -->
### ✅ Enemy_105 远程弓箭手（2025-08-08）
- **攻击范围**：3格，远程单位
- **攻击类型**：非C技（`isCFrame=false`），可被任意攻击打断
- **远程飞行物**：`EnemyProjectile.cs` — DOTween 抛物线飞行
  - Z/X 线性插值 + Y 两段抛物线（OutQuad 上升 + InQuad 下降）
  - X轴俯仰旋转（-25°→+30°）模拟重力弧线
  - Z轴自旋（0→15°）模拟空气动力学
  - 飞行物独立于敌人状态（死亡/击飞不影响已射出箭矢）
- **Parry 格挡**：玩家只能用 Parry 格挡远程攻击
  - `AttackSystem.ExecuteParry()` 扫描范围内 `EnemyProjectile` 实例
  - `parryProjectileRange` 默认 4f
  - 格挡成功 → `Deflect()`：三轴随机旋转(rx:-300~300, ry:-200~200, rz:500~900) + 随机坠落(Y:-3~-6) + X漂移(-1~1)，1.5s
  - 未格挡 → 到达目标点后 `PlayerState.TakeDamage()`
- **DOTween-Animator 同步**（2025-08-08 打磨）：`_attackClip` 缓存 Attack AnimationClip，攻击 DOTween 时长跟随 clip.length 而非硬编码 drawDuration
- **动画**：单 AnimationClip 含3帧精灵关键帧（attack1@0s, attack2@1s, attack3@2s, stopTime=3s）
- **Animator**：`Assets/Animations/Enemy_105.controller` — 遵循统一规范（Idle/Attack/HitFlash/Launched/Dead，AnyState→Dead+Launched）
- **Prefab**：`Assets/Resources/EnemyPrefabs/Enemy_105.prefab`
  - `isRanged=true`, `attackRange=3`
  - `projectilePrefab` 指向 `Assets/Prefabs/arrow.prefab`
  - scale: 0.2（遵循 103 惯例）
- **素材**：`Assets/Sprites/Enemy/Enemy5/`
- **Enemy.cs 改动**：
  - 新增 Header "远程攻击" 字段：`isRanged`, `projectilePrefab`, `arcHeight`, `flyDuration`, `zTargetOffset`, `xOffset`
  - `SpawnProjectile()` 方法：在攻击动画 spawnDuration 结束时 Instantiate + Launch
  - `PlayAttackAnimationTween` 远程分支：跳过 move/flip DOTween，追加 interval+callback+interval
  - `_attackClip` 缓存：Start 中从 Animator controller 查找 Attack clip，用于 DOTween 时长同步
- **testStage 注册**：enemyId=105 (hex 0x69="69")
<!-- locus:body:end -->
