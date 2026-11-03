---
id: kd_3d358295-73d2-4206-ac0e-f195e6754524
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 交接 · 局内成长 / 天赋数据层 / 关卡调参 + SlashC2（2026-11-03）

27 个提交、95 文件（+3124/−943），在 `feat/stage-select-talent-overlays` 上**未 push**。
计划里**不需要肉眼验收的代码 / 数据 / 配置全做完了**，剩下的是表现层（对方）、待定稿数值、需实机手感确认的项。
**2026-11-03 追加（C 类）**：铜钱闭环已修（战斗节点通关奖 + 节点奖励生效，含秘籍通道），
SlashC2 美术资源清单已出 → 详见 §2 的「本轮新增」。

## 1. 做了什么

| 系统 | 一句话 | 主落点 |
|---|---|---|
| 槽位 | `HeroConfig` 拆 `activeSkillSlotCount=3` / `passiveSlotCount=3` / `itemSlotCount=4`（老资产是 0，读取处兜底） | `HeroConfig.cs` |
| 被动排 | 显示全部占用槽位的条目（含 Numeric），与上限判定同口径 | `PassiveSkillRow.cs` |
| 满槽补偿 | 三选一无候选 → `30+5×等级` 铜钱，走 `CombatPresenters.InRunReward` | `UpgradeChoiceManager.cs` |
| 等级上限 | 有效上限 = `min(max(资产,天赋),数据)`，四处判定统一 | `UpgradeEffectManager.cs` |
| 能力天赋 | 11 个消费点（含连击精通 −5 阈值：20→15） | `TalentStatService.cs` |
| 天赋数据层 | `statId/statAdd` 通用载荷 + `groupId` + `col/row`；页1=13、页2=3、页3=9 节点 | `TalentTreeConfig.cs` + 4 棵资产 |
| 流派分线 | 9 技能绑 `requiredTalentIds`；通用（疾风/智慧/地刺/箭雨/冰雾）常驻；修掉「主动技能不过天赋条件」漏洞 | `UpgradePoolSampler.cs` |
| 秘籍 | 定义 / 存档分片 / 掉落 / 可见掉落物；破冰顶开从天赋节点迁入 | `ManualDropSystem.cs` |
| 关卡 | N1 1波 / E0 3波 / N2 3波(孤儿) / N3 5波；104 两阶段；血量倍率按铁律八反推 | `Formal_*.asset` |
| SlashC2 | Slash 去左右位移；新增「任意方向斩击→蓄力松手→同方向强化斩击(带位移)」，需天赋解锁 | `Zhangfei_SlashC2.asset` |

**验证**：编译 error/warn=0；四个校验器全过；整条路线实跑（104 两阶段切换成功，普通波 26.8–36.3s、Boss 57.5s）；满槽补偿 / 连击精通 / 流派过滤 / 秘籍掉落 / SlashC2 六项均实机确认；存档验前备份、验后逐字节还原。

## 2. 还有哪些工作

### A. 对方（面板 / 前端 / 美术）— 接口已备好

| 事项 | 我这边的准备 |
|---|---|
| 天赋页按 `groupId` 画分支泳道 + `TalentPaginator` group 感知切页 | `groupId` 已填满 |
| 天赋页「秘籍已获得」态 | 存档 `unlockedManuals` 已落 |
| `KillRewardUI` 挂进 Battle 场景 | 端口已就绪，挂上即生效 |
| 美术：SlashC2 蓄力动作 / 招式动作 / 位移反馈 / 图标 | 清单见 `design/combo-zhangfei-slash-c2.md` §4 |
| 美术：天赋页整体重做 | 数据层接口齐 |
| 教学关是否教 SlashC2 | 机制同源（`comboChargeActive` 放行），只差教学段 |

### B. 需要你拍板 / 定稿

1. ~~**正式关通关奖在路线流程里没被付**~~ → ✅ **2026-11-03 C-2 已修**（见下「本轮新增」）：战斗节点通关奖与节点奖励都生效，一条路线 ≈ **690**（杀钱 310 + 通关奖 380），落在 600–800 目标内 ✔
2. **XP 缺口 196%**：整条路线只到 Lv10（目标 Lv40）→ 加内容量或改经验曲线。
3. **满槽补偿当前不会触发**：一条路线 ~13 次升级 vs 槽位吃得下 30 次 → 要玩家看见得加内容或降槽位。
4. **N2 是孤儿节点**（J1 只有 `j1_to_ev` 一个出口）→ 补第二出口或下线。
5. SlashC2 数值定稿：伤害 35 / 架势 30 / 重攻击 / 2 排 / 位移 1 排 / 窗口 0.55–1（改资产即可）。
6. 天赋节点定价沿用占位（体魄等=1，聚财 120 / 巧思 180 / 博学 180 来自设计稿）。
7. 铁律八不达标：N1/E0 首波 ~27s（略低于 30）→ 首波倍率 5.8/5.9 提到 ~6.5。
8. 104 对「不操作玩家」容错为零（机器人 190 血掉到 -2）；机器人不会走位，不等于玩家打不过，但值得再调。
9. **路线级 `FakeRouteStageConfig.clearCoinReward` 仍是 0**：要不要再给「整关通关」一笔？（现在只有节点级与杀钱）

### C. 本轮新增（2026-11-03 C-2 / C-5，均已实测）

| # | 事项 | 结果 |
|---|---|---|
| 1 | **战斗节点通关奖生效**：每清完一场节点战斗付该战斗自己的 `StageConfig.clearCoinReward` | 实测 N1 **+60**、E0 **+120**，各只付一次 ✔（局内 90 → 290） |
| 2 | **节点 `rewards[]` 死字段激活**：节点内容跑完就发（铜钱 / 回血 / **秘籍**） | 实测 `Node_N3` 发出「重击解冻」→ 存档 `unlockedManuals=manual_ice_thaw` ✔ |
| 3 | **新增奖励类型 `KillRewardType.Manual`**（直引资产，不走字符串 id）+ 发放口径统一到 `KillRewardUtility` | 编译 0 error；四个校验器全过 ✔ |
| 4 | **SlashC2 美术资源清单** | `plan/slash-c2-art-request-20261103.md`（尺寸/命名/导入设置/挂点/验收口径） |
| 5 | **SlashC2 拖尾贴图改成 per-move**（`AttackSkillConfig.slashRotateSprite1/2`，留空回退全局） | 已加字段 + 已接线 ✔ |
| 6 | 难度表「待决与风险」页加了铜钱闭环行；`game-mechanics` / `stage-configuration` / `gameplay-pillars` 的旧数字已对齐 | 已更新 ✔ |

> ⚠️ **本轮踩到的坑（重要）**：`WaveSpawner` 一场战斗结束会把 `OnAllWavesCompleted` **触发两次**
> （`NotifyWaveCleared` 一次 + `SpawnNextWave` 溢出分支一次）。
> → **任何「战斗结束时付一次」的逻辑都不能挂在那个事件上**，必须挂在「每个 BattleEntry 只走一次」的地方
> （本轮改成挂在 `FakeRouteRuntime.RunBattleEntries`）。非路线分支靠「状态已切 Victory」侥幸躲过，路线分支没有这道闸。

## 3. 踩过的坑 / 注意事项

- 正式关**没有击杀里程碑**机制（`stage-configuration.md §6.3` 相关条目已标作废）；`talent-content-sample-3pages` 里的 `talent_skill_*` id 未采用（实际 `talent_pool_*`）；`numerical-balance-draft.md` 敌人表原为 Stage_Trial 老值（已重测）。
- 跑关机器人**必须先给天赋节点**，否则卡死在 E0 的 1012（C 技霸体只有重攻击能破）。
- `.meta` 里 guid 是 base64、资产引用里是 hex —— 同一 guid 两种写法，**不是悬空引用**。
- `clearCoinReward` 目前「配了但不生效」，别当它已发钱。
- 未 push；push 后另一侧要 rebase。

## 4. 文档索引

`plan/growth-stage-move-plan-20261102.md`（总表）· `plan/talent-scheme-alignment-20261102.md §0.5`（裁决执行状态）· `plan/slash-c2-requirement-analysis-20261103.md` · `plan/pending-visual-decisions-20261103.md`（三个待肉眼项）· `design/in-run-growth-and-unlocks.md §9` · `design/talent-tree-architecture.md` · `design/stage-configuration.md` · `design/combo-zhangfei-slash-c2.md` · `plan/first-level-difficulty-20261103.xlsx`
