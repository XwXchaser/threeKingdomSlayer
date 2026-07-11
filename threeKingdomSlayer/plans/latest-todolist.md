# 最新待办清单

## 0. 图像生成能力
- [x] `gpt-image-generation` Skill 已部署并完成实际调用验证；通过 `curl` 可正常生成，测试图输出至 `C:/Users/Administrator/Downloads/gpt-image-2-test.png`。
- 使用前确认环境变量 `MUSK_API_KEY` 可用；测试图默认输出至 `C:/Users/steam/Pictures/gptGen/`，需导入项目的素材明确输出至 `Assets/...`。

## 1. TimedArrow（timearrow）实际生效验收
- [ ] 验证正常战斗流程能否抽到 `TimedArrow.asset`；当前它未配置进 `UpgradePoolConfig`，可能无法通过升级三选一获得。
- [ ] 获得后验收：Buff 图标与冷却显示、首次冷却、暂停时停止计时、每次触发的箭雨完整播放。
- [ ] 验收实际效果：前方 `rowCount` 排覆盖区域内发射 `4 x arrowCount` 支箭，0.28 秒内完成；空区域也应完整发射。
- [ ] 验收伤害：每箭原始穿刺伤害为 `max(1, damage / 4)`，按落点半径结算；确认阵型偏移、Boss 战斗状态及重叠落点叠伤均符合预期。
- 依据：`Assets/Scripts/Core/TimedPassiveModule.cs`、`Assets/Scripts/Effect/TimedArrowEffect.cs`、`Assets/ScriptableObjects/Upgrades/Definitions/TimedArrow.asset`。

## 2. 大招充能完成头像框火焰与 UltFill 就绪反馈
- [x] `ReadyFireEffect` 已完成位置、尺寸、透明度、循环帧率与抖动效果调整，并保持在头像框背景层。
- [x] `UltFill` 已完成就绪状态的底弱顶强渐变脉冲；大招释放后停止并恢复正常填充表现。
- [x] 已在 Battle 实机流程验收：充满时显示/闪烁，释放时隐藏/重置；多次充能循环尺寸稳定。
- 修改：`Assets/Scenes/Battle.scene`、`Assets/Prefabs/UI/HeroHUD_Zhangfei.prefab`、`Assets/Scripts/UI/UltimateButtonUI.cs`、`Assets/Scripts/UI/UIReadyFireEffect.cs`、`Assets/Scripts/UI/UIReadyVerticalPulse.cs`。
- 最终参数：ReadyFireEffect localOffset=(0,85)、sizeScale=1.1；UltFill pulseSpeed=2、bottomMinAlpha=0.8、topMinAlpha=0.25。

## 3. 敌人受击音效
- [ ] 确定并导入专用敌人受击音频资源；当前项目没有该资源或对应 `AudioManager` 事件。
- [ ] 在统一伤害流程添加一次性受击播放，覆盖普通敌人并处理 `SharedHealthGroup`，避免同次伤害重复播放或漏播。
- [ ] 明确并验收致死命中、群体伤害、高频多段伤害、Boss 与共享血量敌人的播放策略。
- 依据：`Assets/Scripts/Enemy/Enemy.cs`、`Assets/Scripts/Enemy/SharedHealthGroup.cs`、`Assets/Scripts/Managers/AudioManager.cs`、`Assets/Scenes/Battle.scene`。

## 4. 玩家升级经验与关卡难度节奏
- [ ] 先确定目标体验指标：每局目标等级/升级次数、每次升级的预期击杀或战斗时长、各波压力曲线、通关目标时长。
- [ ] 基于指标调整经验曲线、敌人基础经验、波次敌人组成与生命/攻速/伤害倍率；不要将当前数值视为已确认目标。
- [ ] 复核测试关与正式关的配置入口、`StageRegistry` 与 MainMenu 关卡目录不一致的问题，避免调试配置误进入正式流程。
- [ ] 验收升级 UI、多级连续升级、等级上限、各波敌人压力及奖励节奏；记录实测数据作为后续调优基准。
- 依据：`Assets/ScriptableObjects/ExpCurve/DefaultExpCurve.asset`、`Assets/Scripts/Player/PlayerState.cs`、`Assets/Resources/StageConfigs/`、`Assets/Scripts/Wave/WaveSpawner.cs`。

## 完成记录规范
- 每项完成后补充：修改文件/资产、最终参数、验证场景、实测结果与遗留问题。
- 所有运行时效果在 Battle 实机流程验收；所有场景、Prefab 与配置修改在 Unity 重新连接后复核。
