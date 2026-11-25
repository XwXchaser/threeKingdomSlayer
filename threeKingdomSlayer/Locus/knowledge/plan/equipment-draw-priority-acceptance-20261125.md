---
id: kd_11788bd1-2e13-4271-ab60-27dfb0f33a03
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 装备抽取 / 波末队列 / 换装验收方案（2026-11-25）

> **状态：已按用户确认规则实施；编译通过，命中判定 / 满槽切换 / 跳过 / 替换已做局部 Play Mode 验证，等待用户完整实机验收。**
> 关联：`plan/endless-demo-handoff-20261124.md` §0.9、`plan/endless-demo-equipment-p0-implementation-plan.md`、`memory/known-issues.md` §30–§34。

## 1. 本次确认的规则

1. **不刷出与当前已有装备完全一致的装备**：按 `upgradeId` 判断（同种类 + 同品质 + 同可见等级 + 同效果）；同种类的**其他**品质/等级仍可出现，用于换装。
2. **排除后某档没有候选 = 缺货**：不报配置错误、不整组作废；有几张出几张。
3. **所有装备选择排在所有属性升级之前**；同类型内部按到达顺序（装备 = 箱到玩家，属性 = 经验宝石落地）。
4. **波末只等敌人掉落的飞行装备箱**：战斗结束、箱子还在飞 → 等到位再弹；场景破坏物不阻塞波末。
5. **场景破坏物**：可选；打碎入队一次装备选择，在下一次弹出奖励窗口的时机弹出（下一波末）。
6. **跨圈**：按**掉落/打碎时**的内部圈数抽档，不用弹出时的当前圈数。
7. **Boss**：V2 100% 掉装备宝箱；不再有独立锦囊/道具三选一。
8. **满槽换装可切换**：下方旧装备名单打开时，上方候选卡仍可点；点另一张即切换“用哪件新装备替换”。
9. **满槽换装可跳过**：名单右上角「跳过」= 放弃本次装备奖励、保留原装备；不发放补偿，直接推进队列。

## 2. 实现落点

| 项 | 文件 | 说明 |
|---|---|---|
| 精确排除已装备变体 | `Assets/Scripts/Core/EquipmentLoadout.cs` | `IsEquippedExact(def)`：主动查 `FindByKind` 的 definition；被动查 `EquippedItems` 的 definition |
| 抽取过滤 / 缺货 | `Assets/Scripts/Core/EquipmentChoiceDrawer.cs` | 新增 `isEquippedExact` 回调；正权重档排除后无候选 → `report.isShortage` + warning，不报错；同次去重耗尽仍为配置错误 |
| 缺货报告 | `Assets/Scripts/Core/RewardDrawReport.cs` | `isShortage` |
| 装备优先队列 | `Assets/Scripts/Core/RewardChoiceQueue.cs`（新） | Equipment → LevelUp → Item；同类型保持入队顺序；装备项带 `circle` |
| 队列接线 / 圈数 | `Assets/Scripts/Core/UpgradeChoiceManager.cs` | 去掉 FIFO；`GrantEquipmentChoices(count, circle)`；`DrawEquipmentChoices(circle)`；缺货不报配置错误 |
| 掉落圈数盖章 | `Assets/Scripts/Core/EquipmentChest.cs`、`EquipmentChestManager.cs` | 箱子记录 `circleAtDrop`；到达时把圈数带入队列 |
| 波末等箱 | `Assets/Scripts/Managers/StageRunFlow.cs` | 只等 `HasFlyingChests`；10 秒仅作卡死兜底并 `LogError` |
| 箱清理 / 失效剔除 | `Assets/Scripts/Core/EquipmentChestManager.cs`、`EquipmentChest.cs` | `ForgetChest`、`PruneDestroyed`、`OnDestroy` 剔除；`ClearAll` 接进无尽重置 / 结束 |
| 场景破坏物装备 | `Assets/Scripts/Prop/BreakableProp.cs` | 新增 `grantEquipmentChoice`；打碎时 `GrantEquipmentChoices(1, 当前圈)` |
| 场景破坏物命中 | `Assets/Scripts/Prop/BreakableProp.cs` | 命中中心改用 `SpriteRenderer.bounds.center`；有效半径 = `max(hitRadius, bounds.extents.magnitude)` |
| 换装可切换 / 跳过 | `UpgradeChoiceManager.cs`、`UpgradeChoicePopup.cs`、`EquipmentReplaceListView.cs`、`Data/IEquipmentReplacePrompt.cs` | 名单打开时卡片不再禁用；点新卡清旧名单重解析；`ShowReplaceTargets(..., onSkip)`；右上角「跳过」→ `OnReplacementSkipped` |
| 测试掉宝 | `Assets/Scripts/Data/EquipmentDropTestMode.cs`、`Wave/WaveSpawner.cs`、`Enemy/EnemyPool.cs` | N1/E0 全掉宝箱（测试）；回池恢复预制体默认 |

## 3. 当前测试数据（临时）

- 场景 `EndlessRunController.debugForceEliteDropsFirstCircle = true`：第一圈 N1/E0 所有敌人 100% 掉箱。
- 测试变体：`Assets/ScriptableObjects/Equipment/TestVariants/`
  - `ActiveSkill_FireLine_Fine`（精良 Lv1）/ `ActiveSkill_FireLine_Legendary`（传说 Lv2）
  - `TimedFireAOE_Fine`（精良 Lv1）/ `TimedFireAOE_Legendary`（传说 Lv2）
- 装备池：原 `active_fire_line` / `timed_fire_aoe` 权重临时 0；4 个变体 tier1 / weight100。
- **场景破坏物**：当前没有已加载的 `BreakableProp` 实例；验收 Case 2 时需在路线场景里选一个可破坏物，勾上「装备奖励（V2）」。

## 4. 验收矩阵

### A. 精确排除

1. 进入无尽，获得并装备 `蛇形喷射·精良`（TimedFireAOE_Fine）。
2. 继续开装备箱，直到出现 `蛇形喷射·传说` / 其他同款变体。
3. 不应再出现 `蛇形喷射·精良` 这张卡；同款不同品质/等级**可以**出现。
4. 若某档只剩已装备变体：本次少出牌（0–2 张），Console 只有 warning，不出现「配置错误 / 本次奖励作废」。

### B. 装备优先

1. 在一波中同时制造 ≥2 个装备箱和 ≥1 次经验升级。
2. 波末选择窗口：**先连续弹完所有装备选择**，再弹属性升级。
3. 属性窗口正在弹时，若宝箱到达：当前窗口不打断，下一张优先装备，再回属性。
4. 同类型内部顺序 = 到达顺序（先到的先弹）。

### C. Case 1：战斗结束、箱子还在飞

1. 最后一杀掉箱（测试模式 N1/E0 所有敌人都会掉）。
2. 敌人清空后，箱子仍在飞时：**选择窗口不弹出**。
3. 箱子到位（加入队列）后才弹装备选择；Console 不应出现「波末等装备箱超过 10 秒」。
4. 若人为把箱子销毁/移出：等待列表剔除，不永久卡住（极端情况 10 秒兜底并 LogError）。

### D. Case 2：场景破坏物

0. **前置**：进入无尽后 `BreakableProp.HubState` 必须为 `false`（2026-11-25 修：`SuspendForEndless` / `StartEndlessRun` 切关卡态）。N1 时 E0 箱子 `within=False / sprite=False` 是路线距离/可见性正常现象，到 E0/范围内再验。
1. 在路线场景选一个 `BreakableProp`，勾「装备奖励（V2）」。
1.1. **命中范围**：站到箱子旁，分别点箱子的**中心 / 上半部 / 下半部**，都应打碎；不再出现“点上半部只出戳击、箱子不动”。
2. 战斗中选择性打碎：
   - 若在本波末选择**之前**打碎 → 本次波末窗口弹出装备选择。
   - 若在本波末选择**之后**打碎 → 下一次弹出奖励窗口时（下一波末）弹出装备选择；不立刻插在结算/旅行中。
3. 场景里存在但没打碎的箱子不阻塞波末，也不影响 Case 1 的等箱。

### E. 跨圈

1. 第 1 圈掉箱/打碎场景箱，但选择在第 2 圈才弹。
2. 抽档使用**第 1 圈**的档位概率（掉落时盖章），不是第 2 圈。

### F. Boss

1. V2 Boss 死亡 → 掉装备宝箱 → 波末装备三选一。
2. 不再额外弹「Boss 锦囊 / 道具三选一」。

### G. 换装回归（已修 §31）

1. 空槽直装。
2. 同款不同品质/等级 → 原槽替换，等级精确为新变体可见等级（不 +1）。
3. 满槽异款 → 下方旧装备列表，点旧装备替换。
4. 被动 `AttackPassive` 与 `TimedPassive` 可互相替换（都是饰品）。
5. 同一变体重复抽到 → 幂等成功，不再报错作废；但正常抽取应已被 A 排除，只有异常/旧队列才可能重复。
6. 满槽异款 → 名单打开后，点上方另一张卡：名单跟着换成新卡，旧卡高亮清除，不卡死。
7. 名单右上角「跳过」：窗口推进到下一项（或正常关闭）；原装备不丢，新变体不入包，不弹补偿。

### H. 回归

- 教学关：`forcedChoices` / `suppressChoices` 不受队列优先级影响；教学白名单 / 防御键正常。
- V1：Boss 锦囊（Item）仍按 V1 原行为；装备队列不生效。
- 路线检查点：装备按 `upgradeId + level` 恢复；换装不丢属性。

## 5. 复跑与日志

- 编译：Unity 完整编译 + Console 0 error。
- 纯内存已通过：
  - `RewardChoiceQueue`：Equipment→LevelUp→Item、迟到装备插队、同类型到达顺序。
  - `EquipmentChoiceDrawer`：精确排除、同款其他变体保留、档位缺货不报错、同次去重耗尽仍报错。
  - `EquipmentLoadout.IsEquippedExact`：已装备 Fine=true、同款 Legend=false。
  - 真实池 8 轮抽取：已装备 Fine 0 次出现，同款 Legend 正常出现。
- Play Mode 已通过：UCM 先弹装备（`GrantChoices(1)` + `GrantEquipmentChoices(2,1)` 时首窗为装备卡）。
- Play Mode 2026-11-25 命中实测（Hub crate，bounds 1.02 / hitRadius 0.95）：
  - 中心 / 上缘 / 下缘 / 左右缘全部 `TryHitAtScreenPoint=True`；修复前上缘为 `False`（只出戳击）。
- Play Mode 2026-11-25 换装 UI 实测（两件饰品占满槽，候选为异款饰品）：
  - 点卡 A → 名单出现且含「跳过」；点卡 B → `_pendingReplacementVariant` 从 A 切到 B，A 高亮关闭、B 高亮打开，名单仍可用；
  - 点「跳过」→ 名单销毁、队列收尾、原装备仍在、新变体未入包；
  - 点旧装备 chip → 替换成功（旧件移除，新件按可见等级 2 生效），名单关闭、队列收尾。
- 待用户验收：Case 1 等箱时序、Case 2 场景破坏物实际掉落、跨圈概率、完整实机 UI 流程。

## 6. 还原测试数据

- 取消 `EndlessRunController.debugForceEliteDropsFirstCircle`。
- 把 `active_fire_line` / `timed_fire_aoe` 权重改回 100（或正式值）。
- 删除 `TestVariants/` 与池内 4 行（或保留作测试资产）。
- 场景破坏物的「装备奖励（V2）」按需要取消。

## 7. 已知边界

- 波末等箱 10 秒是**异常兜底**；正常飞行 1–2 秒。超过 10 秒会 LogError 并继续，不会永久卡死。
- 迟到箱/场景箱若在本局结束前没有下一次奖励窗口，不会补弹；队列随本局结束清理。
- 档位缺货只影响本次出牌数量，不改写概率、不补卡、不报配置错误。
- `EquipmentChoiceDrawer` 仍会拒绝“同次同种类不同变体”重复；这是既有规则。
