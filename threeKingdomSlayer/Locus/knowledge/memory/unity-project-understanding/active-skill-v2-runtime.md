---
id: kd_65f993af-750c-4e93-8ef6-7377cd142e27
injectMode: inherit
summary: V1限次道具与V2主动技能在项目中的实现入口、资产路径、池路由和UI绑定。
aiEditMode: inherit
---

- V1/V2 版本选择由 `ActiveSkillInventory._ruleVersion` 控制，挂载在 `Assets/Scenes/Battle.scene/Manager`；仅开局前切换。当前场景默认配置为 `V2_ActiveSkill`。
- V1 保持 `ItemInventory`、`ItemPoolConfig`、`DropItemPoolConfig`、血包与弃置弹窗链路。
- V2 普通升级与Boss奖励统一读取 `Assets/ScriptableObjects/ActiveSkills/ActiveSkillPoolConfig.asset`；该资产概念上是 V2Pool。普通三选一按池层 0.60/0.35/0.05 抽取；技能资产自身 `rarity` 字段不参与抽取。
- 当前V2Pool：Common=专注、蛇形喷射、疾风、智慧、铁壁、地刺；Rare=震荡、拔苗助长、火龙舌、主动冲击波、海浪、箭雨、染病；Legendary=旋风。所有池内权重100。Boss奖励抽取规则另行讨论。
- 被动和主动共用V2Pool，但槽位独立过滤：最多6个不同被动、4个不同主动；槽满后仍可升级已有且未满级技能。
- V2使用 `ActiveSkillInventory`、`ActiveSkillRunner`、`ActiveSkillDefinition`；主动CD按 `Time.deltaTime` 递减，成功执行后才开始。专注在起冷却时统一应用 `基础CD × (1-累计缩减)`，不会追溯缩短已经开始的冷却。
- 火龙舌走 `ShootFireEffect.Play`：配置列内、当前可见排，每敌每次施放最多命中一次；目标处于灼烧时伤害直接×2。
- 蛇形喷射走 `ShootFireEffect.PlaySweep`；扇形往返扫射与单轮多段命中是正式核心机制。`TimedAoeLevelConfig.rangeRows` 已实现为逐级可调射程，当前 `TimedFireAOE.asset` 为1/1/2/2/3排。命中筛选和火焰视觉终点同步按射程限制，视觉飞行时长会按距离缩放以维持原飞行速度；同一敌人仍可按0.2秒间隔被同轮多次命中。蛇形专用起点由 `TimedPassiveModule.fireSweepStartZOffset` 配置；Battle当前为-4，叠加Prefab基础`fireStartZ=-2`后最终世界起点为Z=-6，终点不变。射程世界坐标必须加上实际敌人父节点Z；Battle中`EnemyPool.enemiesRoot`为空、活跃敌人实际挂在`poolRoot/Enemies`（Z=9），实现会优先从存活敌人父节点解析，回退到`enemiesRoot ?? poolRoot`。火龙舌继续使用独立的 `ShootFireEffect.Play` 路径，不受这些蛇形参数影响。
- 箭雨的配置 `damage` 当前是每波伤害预算：每波固定4支伤害箭，单箭伤害=`max(1, damage/4)`；`arrowCount`实际是波数。每支箭对落点半径内所有敌人分别结算，因此整次总伤害由波数、命中的伤害箭数、范围内敌人数和密度共同决定，不能写成固定的全场总伤。
- 海浪为0 HP伤害，普通敌人后推1排，未Stun Boss按最大Poise百分比削韧；旋风击飞范围内普通敌人、制造浮空增伤窗口，对未Stun Boss造成更高百分比削韧，并可击飞已Stun Boss。
- 震荡受击层与染病武装/附着层均正式允许无上限累积。强度分析不应将无上限本身视为实现错误，应通过基础伤害、触发/CD、传播和释放条件评估。
- 染病点击后武装下一次Stab；Lv1死亡向左/右/后相邻格传播，Lv2+优先选择最近同行敌人、否则最近其他排敌人，完整保留层数并重置持续时间。
- 数值设计基准（用户定义）：染病Lv1 = CD6s / 持续4s / 总伤10 作为1.0强度单位，裸DPS≈1.67；当前资产数值不具参照意义，后续数值重设计以此基准推导。
- 染病机制成长：仅「基础传播（死亡传染相邻格）」与「智能传播（优先最近同行敌）」两种传播形态；智能传播应自Lv3起出现，Lv1/Lv2为纯数值成长。禁止「传播范围+1格」「同时传播多个目标」等未设计机制。
- 火龙舌列数固定为中心一列，不可随等级成长或配置多列。
- 目标选取规则：主动 Cyclone 已改为区域型；按 `waveLevels.rangeRows` 每排生成一个位于该排 `col=2` 的大型旋风区域。区域生成不依赖敌人存在，生成时立即扫描该排已有敌人，持续期间继续扫描同排新出现/进入的敌人。
- 区域持续时间由 `ActiveWaveLevelConfig.cycloneDuration` 配置，当前 `ActiveSkill_Cyclone.asset` 五级均为 2 秒；它与敌人被吹飞时长分离。
- 同一敌人在同一个区域生命周期内只触发一次；不设置重新触发间隔。
- 区域视觉使用 `CycloneEffect.PlayZoneVisual`：按 `cyclone1→6` 展开，之后循环 `cyclone5↔6`，区域结束后淡出。
- 区域控制与敌人跟随视觉已解耦：主动区域不再为被击飞敌人实例化第二个 `CycloneEffect`，避免敌人脚下重复出现旋风；区域负责触发伤害、击飞和落地伤害，`CycloneEffect` 的目标跟随模式保留给其他旧路径。
- 区域实现：`Assets/Scripts/Effect/CycloneZone.cs`；区域组件挂载于 `Assets/Prefabs/Effects/CycloneEffect.prefab`。
