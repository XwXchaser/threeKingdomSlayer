# 最新待办清单

## 本周目标（进行中）

### 1. 角色与 Boss 演出补齐
- [ ] 将进度 UI 栏扩展为角色对话入口。
- [ ] 制作可双面展示的 3D 看板：正面显示进度节点，反面显示对话与 QTE。
- [ ] 对话触发时，看板以自身中心沿本地 X 轴翻转 180°；结束后恢复正面。
- [ ] 补齐角色演出与 Boss 演出，并在 Battle 实机流程验收。

### 2. 箭矢与飞射物表现优化
- [ ] 优化箭矢及其他飞射物的视觉、轨迹、命中反馈与预警表现。
- [x] Stab 已改为武器枪尖扫过实际敌人位置时结算伤害，支持无敌人时空挥；空挥消耗动作锁定/冷却，不产生能量或攻击被动。
- [ ] 在普通战斗、QTE 与 Boss 场景分别验收，避免影响现有伤害和时序逻辑。

### 3. Boss 设计、显示与难度
- [ ] 补全 Boss 设计内容及对应战斗演出。
- [ ] 优化 Boss 的 UI/场景显示。
- [ ] 调整 Boss 难度配置，并记录最终配置与实测结果。

### 4. 20–30 分钟测试关卡
- [ ] 制作一条目标时长 20–30 分钟的测试关卡。
- [ ] 按难度递进配置敌人、波次、Boss 与奖励节奏。
- [ ] 记录通关时长、玩家等级/技能成长、压力峰值和卡点，作为后续平衡依据。

### 5. “染色”敌人图片崩坏 Bug
- [x] 已定位并修复：对象池复用时，白色波次跳过写色导致上波 tint 残留；每次回收销毁 Renderer 正在使用的材质实例也可能使材质失效并出现白图/崩坏。
- [x] 修复：每波显式写入基准色×波次色（白色也恢复）；材质实例改为对象生命周期内稳定持有，仅在 `OnDestroy` 销毁。
- [x] 编辑器回归：`Enemy_1` 染红 → 回收 → 白色波复用，颜色恢复白色；脚本重新编译通过。
- [ ] 待 Battle/TestStage 实机覆盖：连续对象池复用、受击闪白、描边及 `Enemy_1/101/102/103/104/105`。

### 6. 技能组与美术素材
- [ ] 优化技能组配置与可选技能组合。
- [ ] 制作更多技能图标与技能表现美术素材，并完成导入和游戏内验证。

## 本周完成记录
- 每项完成后记录：修改文件/资产、最终参数、验证场景、实测结果、遗留问题。
- 状态标记：`[ ]` 未开始、`[-]` 进行中、`[x]` 已完成、`[!]` 阻塞。

## 历史待办

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
- [x] 已导入并配置 `Assets/SFX/enemy/enemy_hitted1.wav`、`Assets/SFX/enemy/enemy_hitted2.wav`。
- [x] 已接入 `AudioManager` 的全局 `Enemy_Hit` 事件：0.14 秒全局冷却、双音频防连续重复。
- [x] 已在普通敌人和 `SharedHealthGroup` 的有效扣血后触发；Boss 不播放、共享血量整组每次伤害仅一声、致死命中播放。
- [x] 已在 Battle 实机听感验收，高频攻击下效果可接受。
- 修改：`Assets/Scripts/Enemy/Enemy.cs`、`Assets/Scripts/Enemy/SharedHealthGroup.cs`、`Assets/Scripts/Managers/AudioManager.cs`、`Assets/Scenes/Battle.scene`。

## 4. 玩家升级经验与关卡难度节奏
- [ ] 先确定目标体验指标：每局目标等级/升级次数、每次升级的预期击杀或战斗时长、各波压力曲线、通关目标时长。
- [ ] 基于指标调整经验曲线、敌人基础经验、波次敌人组成与生命/攻速/伤害倍率；不要将当前数值视为已确认目标。
- [ ] 复核测试关与正式关的配置入口、`StageRegistry` 与 MainMenu 关卡目录不一致的问题，避免调试配置误进入正式流程。
- [ ] 验收升级 UI、多级连续升级、等级上限、各波敌人压力及奖励节奏；记录实测数据作为后续调优基准。
- 依据：`Assets/ScriptableObjects/ExpCurve/DefaultExpCurve.asset`、`Assets/Scripts/Player/PlayerState.cs`、`Assets/Resources/StageConfigs/`、`Assets/Scripts/Wave/WaveSpawner.cs`。

## 完成记录规范
- 每项完成后补充：修改文件/资产、最终参数、验证场景、实测结果与遗留问题。
- 所有运行时效果在 Battle 实机流程验收；所有场景、Prefab 与配置修改在 Unity 重新连接后复核。
