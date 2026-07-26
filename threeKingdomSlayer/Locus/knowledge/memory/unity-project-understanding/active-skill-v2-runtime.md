---
id: kd_65f993af-750c-4e93-8ef6-7377cd142e27
type: memory
path: unity-project-understanding/active-skill-v2-runtime.md
title: active-skill-v2-runtime
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1784887044900
updatedAt: 1784891923269
---

# active-skill-v2-runtime

## Summary
V1限次道具与V2主动技能在项目中的实现入口、资产路径、池路由和UI绑定。

<!-- locus:body:start -->
- V1/V2 版本选择由 `ActiveSkillInventory._ruleVersion` 控制，挂载在 `Assets/Scenes/Battle.scene/Manager`；仅开局前切换。当前场景默认配置为 `V2_ActiveSkill`。
- V1 保持 `ItemInventory`、`ItemPoolConfig`、`DropItemPoolConfig`、血包与弃置弹窗链路。
- V2 使用独立 `ActiveSkillInventory`、`ActiveSkillRunner`、`ActiveSkillPoolConfig` 和 `ActiveSkillDefinition`；普通敌人掉落及血包入口在 V2 关闭。
- V2 普通升级三选一合并主动技能候选；Boss 主动技能奖励只从 Rare/Legendary 池抽取。满槽时 `CanAcquire` 仅允许已有且未满级技能；全满级时无合法主动技能候选。
- 首批主动技能资产位于 `Assets/ScriptableObjects/ActiveSkills/`：主动喷火、主动箭雨、主动旋风。它们分别复用原被动技能的每级效果配置作为初始测试数值，但资产和运行状态完全独立。
- `BuffDisplayPanel` 与 `FrontItemBar` 根据版本绑定 V1 库存或 V2 主动技能；V2 角标显示冷却剩余秒数并使用冷却遮罩，不显示等级/次数。
- V2 冷却使用 `Time.deltaTime`，暂停时冻结；成功执行后才开始 CD。QTE 与对话锁定继续由现有 UI 输入守卫处理。
- `ShootFireEffect.Play` 始终播放喷火视觉；每个飞行火焰粒子在移动期间按配置列及可见行范围实时查询敌人。因此起手时无目标也会播特效，且喷射持续期内因补齐或位移进入范围的敌人可被后续粒子命中；同次喷射中每个敌人最多受伤一次。
<!-- locus:body:end -->
