---
id: kd_fb351cd8-3e22-4ce9-902e-7b6b93cee3dc
injectMode: inherit
summary: 旋风(Cyclone) TimedPassive 升级功能：周期性随机击飞敌人，配合 cyclone 精灵动画，高级解锁落地伤害。
aiEditMode: inherit
---

# 旋风 (Cyclone) — 主动区域型旋风

## 概述
- **类型**：V2 ActiveSkill，`activeEffectType=Cyclone`
- **稀有度**：Legendary
- **最高等级**：5
- **触发机制**：主动释放后，按覆盖排数生成区域旋风；生成与敌人是否存在解耦。

## 效果
| 等级 | 冷却(s) | 覆盖排数 | 旋风持续(s) | Boss架势削减 | 落地伤害 |
|------|---------|----------|-------------|--------------|----------|
| Lv.1 | 10 | 1 | 2 | 12% | 0 |
| Lv.2 | 9 | 1 | 2 | 14% | 0 |
| Lv.3 | 8 | 2 | 2 | 16% | 8 |
| Lv.4 | 7 | 2 | 2 | 18% | 12 |
| Lv.5 | 6 | 3 | 2 | 20% | 16 |

### 区域触发规则
- 每个覆盖排生成一个大型旋风，位置固定为该排 `col=2` 的中心位置，视觉上覆盖整排五列。
- 旋风生成时立即检测该排已有敌人并触发；持续期间继续检测同排敌人，因此后续进入/出现的敌人也会触发。
- 同一敌人在同一个旋风区域生命周期内只触发一次；没有重新触发间隔。
- 普通敌人被主动击飞；未眩晕 Boss 不击飞，执行主动受击打断和架势削减。
- 当前区域控制直接执行击飞伤害、`Launch` 和落地伤害监听，不再为被击飞敌人额外生成脚下 Cyclone 视觉。

## 视觉生命周期
- 区域视觉：`cyclone1→cyclone6` 展开，随后 `cyclone5↔cyclone6` 循环，区域结束后淡出。
- 被击飞敌人的跟随视觉是另一种旧路径，不属于主动区域的必要组成。

## 代码架构
- `Assets/Scripts/Core/ActiveSkillRunner.cs`：主动入口，按 `rangeRows` 创建每排区域。
- `Assets/Scripts/Effect/CycloneZone.cs`：区域位置、生命周期、同排扫描、单次触发记录、Boss处理、落地伤害监听。
- `Assets/Scripts/Effect/CycloneEffect.cs`：区域视觉模式及旧的敌人跟随/地面视觉模式。
- `Assets/Prefabs/Effects/CycloneEffect.prefab`：包含 `CycloneEffect` 与 `CycloneZone`。
- `Assets/ScriptableObjects/ActiveSkills/ActiveSkill_Cyclone.asset`：五级主动配置，`cycloneDuration=2`。

## 经验与约束
- 区域技能必须由区域对象负责生成/消失，不能以“为当前敌人生成特效”作为唯一入口；否则空场无法生成，后续进入者也无法触发。
- 区域视觉与敌人受击后的跟随视觉必须分离；复用同一 Prefab 时必须禁止区域控制器再次实例化自身作为敌人视觉，否则会出现脚下重复特效。
- 区域生命周期时长与目标控制时长是两个不同概念，应使用独立配置字段。
