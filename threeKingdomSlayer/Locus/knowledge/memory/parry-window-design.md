---
id: kd_a1611e2a-5cae-4c64-9590-033dfb2ee8e5
injectMode: inherit
summary: parry 锁死问题的判定窗口收紧方案（设计讨论中，未实现）
aiEditMode: inherit
---

# Parry 判定窗口方案（开发中，2026）

## ⚠️ 当前问题（待继续，勿遗忘）
- **攻击动画与逻辑时序脱节**：敌人攻击动画由 Animator clip 驱动（如 Enemy_101_Attack.anim 时长 1.333s，精灵时序 attack1→attack2(挥击)→attack3），而伤害/停顿/窗口由 DOTween（spawnDuration=2s + parryWindow=1s）驱动，两套计时完全独立。
- 现象：敌人"举剑后立刻砍下"（动画 0.667s 就切 attack2），伤害却拖到 ~3s（收尾帧才触发）；parry 窗口与视觉挥击帧错位。
- EnemySpriteController 是死代码（所有 prefab 均未挂该组件，`GetComponent<EnemySpriteController>()` 返回 null），改它无效——敌人显示全由 Animator 驱动。
- 结论：**伤害帧和 parry 窗口必须与动画节奏对齐**，不能靠 DOTween 凭空插停顿。

## ✅ 已确定的设计方向（方案A1：attackSpeed 单一驱动，已实现 2026）
- 目标：调攻击快慢只改 `attackSpeed` 一个参数，动画/伤害帧/parry窗口/冷却自动同步。
- **已实现**（Enemy.cs）：
  1. 新增 `_attackHitTime` 字段 + `ParseAttackHitTime(clip)` 方法：解析 clip 伤害帧（规则：精灵名含 `hit` 取第一个出现时刻；否则取第2个不同精灵段末帧）。
  2. `PlayAttackAnimationTween` 近战分支重构：`_animator.speed = attackSpeed`；`hitTimeReal = hitT / attackSpeed`；前突移动时长 = hitTimeReal；伤害回调在 hitTimeReal 时刻触发；收招时长 = (clipLen - hitT)/attackSpeed。
  3. parry 窗口终点 = 伤害帧时刻：`_parryWindowStartTime = Time.time + hitTimeReal`。
  4. OnComplete / CancelAttack 恢复 `_animator.speed = 1f`。
- **已知限制**：`_attackClip` 在 Initialize 只缓存普通 `_Attack` clip；Boss 的 CAttack（phase 切换后 attackSequence 变化）的 hit 时间需另行解析（当前未处理）。远程分支（isRanged）仍用 spawnDuration 驱动，未切 A1。
- 待用户实测反馈后再决定是否扩展 CAttack/远程。

## 背景
- 问题：玩家只要一直 parry 就能无限打断霸体 Boss，Boss 永远进不了 AttackDraw/伤害帧（锁死）。
- 硬约束：不可加长玩家 parry 冷却。

## 已实现的方案（设计A：真实停顿窗口）
- 敌人攻击流程（近战分支）：蓄力(AttackSpawn，不可 parry 打断) → 悬停停顿(parryWindowDuration) → PerformAttack → AttackDraw。
- 窗口起点预算为 `_parryWindowStartTime = Time.time + spawnDuration`（蓄力完成时刻），`IsParryWindowActive` 判定区间 = [起点-grace, 起点+duration]。
- **所有敌人统一**：`TakeDamage` 中 `parryGatePassed = !isParryInterrupt || (!isRanged && IsParryWindowActive)`——远程 parry 完全不打断攻击动作（反弹飞行物在 AttackSystem 独立处理），近战仅窗口内打断；窗口外伤害/削韧照常。普通攻击打断路径（非霸体小怪）保持不变。
- 参数放 `AttackStep`（每敌人每步可调）：`parryWindowDuration` + `parryWindowGrace`。
- 清理：`_parryWindowStartTime=-1` 在所有打断/退出路径复位（ResetEnemy/Stun/Launch/CancelAttack/EnterQTEAttack/ExitQTEAttack/转阶段/攻击OnComplete/每次攻击开始）。

## 参数耦合关系
- 插入真实停顿 → 命中前总时长 = spawnDuration + parryWindowDuration，非互斥，线性叠加。
- 现阶段不做节奏补偿，总时长变长一个 window。

## 已写入资产的默认值（新敌人制作参考兜底值）
- `parryWindowDuration=0.3`, `parryWindowGrace=0.1`，已批量写入所有 Enemy prefab（9个）+ BossPhaseData_104 三阶段（12字段）。Enemy_108 两步攻击与 Phase1 已 YAML 复核。
- **新制作敌人时**：`AttackStep` 新增字段 `parryWindowDuration` / `parryWindowGrace` 的 C# 默认值为 0（= 无窗口，蓄力完立即命中、不可 parry 打断）。如希望敌人可被 parry 打断，需在 Inspector 手动填写兜底值：`parryWindowDuration=0.3`、`parryWindowGrace=0.1`，再按该敌人蓄力动画节奏微调。远程敌人可留 0（远程 parry 不打断，字段不生效）。

## 待办（非优先）
- 窗口期白色微光特效，提示玩家判定起点（用户确认后补）。

## 实现注意
- 插入停顿期间 Animator clip 会继续播，clip 长度 < spawn+window 会动画/判定错位，需逐个敌人确认蓄力帧是否自带 hold。
- 远程敌人(isRanged)攻击序列也写入了默认值，但门控 isRanged 使其不生效（无副作用）。

## 修复记录（2026，测试反馈两问题）
- 问题1：窗口外 parry 命中攻击中敌人 → 播放受击动画/闪白，但攻击未打断，视觉不同步。
- 问题2：AttackDraw 收尾帧期间 parry → Hit Trigger 污染 Animator，之后敌人不再攻击。
- 修复：`TakeDamage` 中 `suppressHitFeedback = isParryInterrupt && !parryGatePassed`；此标志同时应用到 sharedHealthGroup 的 triggerHitAnimation 参数与本地受击动画分支。窗口外/远程 parry 只保留伤害与削韧，不再播放受击表现。
