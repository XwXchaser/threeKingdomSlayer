---
id: kd_27ef541b-f3c9-4b70-9c9d-0699fc1bcb0f
type: design
path: in-game-growth-system.md
title: in-game-growth-system
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1779350986798
updatedAt: 1779438372982
---

# in-game-growth-system

## Summary
三套局内奖励系统的分期实现计划：总击杀奖励 → 连击奖励 → 经验三选一

## Content
# 局内成长系统设计

## 总览

三套局内奖励系统，分期实现：
1. **总击杀奖励**（第一期，✅ 已完成）— 累计击杀里程碑 → 一次性铜钱/经验/回血/升级
2. **连击奖励**（第二期，✅ 已完成）— 连击阈值 → 限时BUFF
3. **经验三选一**（第三期，未开始）— 经验升级 → 暂停浮窗选本局永久加成

## 第二期 连击奖励 实现概要

### 架构
- `ComboManager` — 单例，监听敌人受击，积累连击数，到达阈值触发 Buff
- `BuffManager` — 单例，管理限时 Buff 的添加/移除/刷新，每帧更新并广播 `StatModifier`
- `ComboBuffConfig` — ScriptableObject，配置 resetDelay、hitIncrementMode、triggers 列表
- `StatModifier` — 属性修正数据类（statId, modifierType, value）
- `ComboDisplayUI` — 连击特效显示（FillImage + StaticImage + 缩放动画），使用 Image.color.a 控制显隐
- `ComboUI` — 连击数文字显示（预留）

### 关键设计决策
- 连击倒计时使用 `Time.time`（暂停时冻结）
- 缩放动画使用 `Time.deltaTime`（暂停时冻结）
- UI 显隐使用 `Image.color.a` 方案（避免 SetActive 自引用 Bug）
- 每帧同敌人只计一次（`HitIncrementMode.PerEnemy` 模式）
- 同类 Buff 刷新时长，异类共存

### 配置
- `Assets/ScriptableObjects/Combo/ComboBuffConfig.asset` — 10 连击 → combo_atk_10（5秒 50% ATK）

### Bug 经验
- SetActive 自引用在 Play Mode 下不可靠（见 `skill/ui-visibility-patterns.md`）
- CanvasGroup 在 Canvas 子节点上可能导致全 Canvas 消失
- Image 拉伸：`preserveAspect=true` + sizeDelta 匹配 sprite 比例

## 核心区别

| | 总击杀 | 连击 | 三选一 |
|---|---|---|---|
| 触发方式 | 累计击杀数 | 连续命中 | 经验满升级 |
| 奖励持续 | 即时一次性 | 限时BUFF | 本局永久 |
| 持久化 | 铜钱写存档 | 否 | 否 |
| 重置条件 | 本局不重置 | 断连归零 | 新对局重置 |
| 叠加规则 | 每里程碑一次 | 同类刷新/异类共存 | 可叠加 |

## 系统间关系

- 总击杀的 `GrantRandomUpgrade` 奖励类型依赖第三期的升级池
- 连击的 `StatModifier` 框架可被三选一的 `UpgradeEffect` 复用
- 三者共用 `PlayerState` 作为运行时数据载体
- 三者共用 `SaveManager` 作为持久化出口（仅总击杀）

## 策划配置

- `Assets/ScriptableObjects/Combo/ComboBuffConfig.asset`
- `Assets/ScriptableObjects/KillReward/TotalKillMilestoneConfig.asset`
- `Assets/ScriptableObjects/Upgrades/` (定义 + 池 + 经验曲线)

## 扩展接口

- 局外成长预留：`IGameStartModifier`、`OnBeforeKillRewardSettle`、`OnComboTrigger`、`OnUpgradeChosen` 等事件
- 效果系统：字符串 `statId` / `effectType` → 注册表模式，可插拔
