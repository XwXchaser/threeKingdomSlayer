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

1. **正式关通关奖在路线流程里没被付** → 一条路线到手 ≈410（杀 310 + 路线 100），低于目标 600–800。二选一：实现路线节点 `rewards[]` 消费（该字段现在是死的），或调数值。← 最影响经济闭环
2. **XP 缺口 196%**：整条路线只到 Lv10（目标 Lv40）→ 加内容量或改经验曲线。
3. **满槽补偿当前不会触发**：一条路线 ~13 次升级 vs 槽位吃得下 30 次 → 要玩家看见得加内容或降槽位。
4. **N2 是孤儿节点**（J1 只有 `j1_to_ev` 一个出口）→ 补第二出口或下线。
5. SlashC2 数值定稿：伤害 35 / 架势 30 / 重攻击 / 2 排 / 位移 1 排 / 窗口 0.55–1（改资产即可）。
6. 天赋节点定价沿用占位（体魄等=1，聚财 120 / 巧思 180 / 博学 180 来自设计稿）。
7. 铁律八不达标：N1/E0 首波 ~27s（略低于 30）→ 首波倍率 5.8/5.9 提到 ~6.5。
8. 104 对「不操作玩家」容错为零（机器人 190 血掉到 -2）；机器人不会走位，不等于玩家打不过，但值得再调。

### C. 我可以继续做（无需验收）

1. 给美术出 **SlashC2 资源清单**（尺寸 / 帧数 / 命名 / 挂点 / 拖尾参数）。
2. 实现路线节点 `rewards[]` 消费（**会改经济**，需先定 B-1 方向）。
3. 按你的新数值**跑第二轮整线验证**，出对照表。
4. 剩余文档实测数字对齐 + 难度表「待决与风险」页收尾。
5. `KillRewardType.Manual`（秘籍做里程碑奖励，确定性到手）——需你确认要不要这个通道。

## 3. 踩过的坑 / 注意事项

- 正式关**没有击杀里程碑**机制（`stage-configuration.md §6.3` 相关条目已标作废）；`talent-content-sample-3pages` 里的 `talent_skill_*` id 未采用（实际 `talent_pool_*`）；`numerical-balance-draft.md` 敌人表原为 Stage_Trial 老值（已重测）。
- 跑关机器人**必须先给天赋节点**，否则卡死在 E0 的 1012（C 技霸体只有重攻击能破）。
- `.meta` 里 guid 是 base64、资产引用里是 hex —— 同一 guid 两种写法，**不是悬空引用**。
- `clearCoinReward` 目前「配了但不生效」，别当它已发钱。
- 未 push；push 后另一侧要 rebase。

## 4. 文档索引

`plan/growth-stage-move-plan-20261102.md`（总表）· `plan/talent-scheme-alignment-20261102.md §0.5`（裁决执行状态）· `plan/slash-c2-requirement-analysis-20261103.md` · `plan/pending-visual-decisions-20261103.md`（三个待肉眼项）· `design/in-run-growth-and-unlocks.md §9` · `design/talent-tree-architecture.md` · `design/stage-configuration.md` · `design/combo-zhangfei-slash-c2.md` · `plan/first-level-difficulty-20261103.xlsx`
