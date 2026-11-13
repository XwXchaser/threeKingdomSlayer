---
id: kd_9f8be317-c40b-438c-831a-b76a9a03c900
injectMode: inherit
injectAgents:
- '*'
summary: 技能按 4 条分支（位移流/范围伤害/通用流/基础技能）在天赋页解锁的落地记录：250 铜钱定价依据、已改资产清单（池出冲击波、3 个新门槛、TalentTree_Pools 重排为 3 列 9 节点、隐藏 3 个）、13 项最终池、模拟器验证与 4 条待办。
aiEditMode: inherit
---

# 技能解锁分支部署（2026-11-09，已落数据并出包）

交接入口：下一个对话要动这块，先读本文 + `design/in-run-build-archetypes.md`（已同步改成 4 分支版）。

## 1. 用户定的规则（原话要点）

1. 冲击波（`active_charge_attack_shockwave`）**出池**。
2. **位移流是严格链条**：地刺 → 染病 → 火龙舌（前一个没点，后面的点不了）。
3. 技能解锁**改成要一定量铜钱**，量级 ≈「打到 E0 wave3 能拿到的铜钱」，技能页其它提升同价。
4. 分类（没提到的技能不入池）：

| 分支 | 技能 |
|---|---|
| 位移流 | 地刺、染病、火龙舌 |
| 范围伤害 | 蛇形喷射、冰雾 |
| 通用流（含暂存的蓄力流） | 专注、疾风、铁壁、震荡 |
| 基础技能（不用解锁，开局在池） | 海浪、智慧、箭雨、旋风 |

## 2. 价格口径（250 铜钱）

- E0 波次敌人数实测（`Assets/RouteData/ChangbanpoScroll/Battles/Formal_E0_Battle.asset`，`enemyIds` 是 5 列 × 4 字节小端编码）：
  wave1 = 10、wave2 = 6、wave3 = 9 ⇒ 打到 wave3 共 **25 杀**。
- 每杀铜钱 = `Enemy.coinReward` 默认 10，本关敌人（1011/1012/1051）预制体均未覆盖 ⇒ 25 × 10 = **250**。
- E0 未配击杀里程碑（`killMilestones: []`），所以没有额外收入。
- ⇒ 技能页所有可见节点 `coinCost = 250`。

## 3. 改了哪些资产

| 资产 | 改动 |
|---|---|
| `Assets/ScriptableObjects/ActiveSkills/ActiveSkillPoolConfig.asset` | `rarePool` 5 → 4：移除 `active_charge_attack_shockwave`（冲击波） |
| `Assets/ScriptableObjects/Upgrades/Definitions/SpikeTrap.asset` | `requiredTalentIds += talent_pool_spiketrap` |
| `Assets/ScriptableObjects/ActiveSkills/ActiveSkill_IceMist.asset` | `requiredTalentIds += talent_pool_icemist` |
| `Assets/ScriptableObjects/Upgrades/Definitions/AttackSpeed.asset` | `requiredTalentIds += talent_pool_wind` |
| `Assets/ScriptableObjects/Talents/TalentTree_Pools.asset` | 节点 9 → 12（新增 3），隐藏 3，重排 3 条分支，全部 250 铜钱 |

天赋页技能页（`TalentTree_Pools`）最终布局：

| 列 | 分支 | 节点（nodeId / 坐标 / 前置） |
|---|---|---|
| col 0 | 位移流 | `talent_pool_spiketrap` (-300, 0) 无前置 → `talent_pool_disease` (-300, -200) 前置 spiketrap → `talent_pool_fireline` (-300, -400) 前置 disease |
| col 1 | 范围伤害 | `talent_pool_snake` (0, 0) 无前置 → `talent_pool_icemist` (0, -200) 前置 snake |
| col 2 | 通用流 | `talent_pool_focus` (222, 0)、`talent_pool_wind` (378, 0)、`talent_pool_ironwall` (222, -200)、`talent_pool_shockwave` (378, -200)，四者均无前置 |

隐藏（`hidden = true`，保留 id 以免旧存档解锁记录失效）：

- `talent_pool_chargewave`（冲击波，出池）
- `talent_pool_wave`（海浪，改基础技能，不再需要解锁）
- `talent_pool_cyclone`（旋风，同上）

分支牌：`Assets/Scenes/TalentTree.scene` 里 `TalentTreePanel.branchPlaques` 正好 3 个槽位，对应 col 0/1/2，UI 不用改。

## 4. 最终池（13 项）

| 桶 | 项 |
|---|---|
| common（7） | 专注🔒 / 智慧 / 铁壁🔒 / 地刺🔒 / 火龙舌🔒 / 染病🔒（权重 300）/ 冰雾🔒 |
| rare（4） | 震荡🔒 / 箭雨 / 蛇形喷射🔒 / 疾风🔒 |
| legendary（2） | 旋风 / 海浪 |

🔒 = 有 `requiredTalentIds` 门槛。无门槛的 4 个就是「基础技能」。

## 5. 机制（无新代码）

- 池项带 `requiredTalentIds` → `Assets/Scripts/Core/TalentUpgradeRequirementProvider.cs` 判定 → `UpgradeChoiceManager`（`CollectUnified`）在抽三选一时过滤 ⇒ **未解锁就不进候选**。
- 主动技能槽：`ActiveSkillInventory.Capacity = heroConfig.activeSkillSlotCount + TalentCapabilities.ExtraActiveSlot`（默认 3，天赋「巧思」+1）。
- 桶概率：`ActiveSkillPoolConfig.commonWeight/rareWeight/legendaryWeight = 0.60 / 0.35 / 0.05`，桶内按 `weight`。

## 6. 验证与产物

- 编辑器：数据核对（池 13 项、9 个可见节点、门槛与链条正确）。
- 构建：`Build/Android/threeKingdomSlayer_full_20261109_1354.apk`（errors=0，布局自检 ✅，1:29）。
- 模拟器：已 `install -r` 并启动到落地页；用户已验收天赋页/解锁表现。

## 7. 待办 / 待定（未做）

1. **图标是占位**：冰雾借用 `icon_31_timedFire`（火系图），疾风借用 `icon_31_longer`。要按 `design/skill-item-icon-art-guideline.md` 出正式图。
2. **染病权重仍是 300**（其它 100），是早期测试值；要不要归一到 100。
3. **火龙舌在链的第三层**：要 750 铜钱才摸到，而一条路线整关收入约 690 铜钱 —— 节奏是否符合预期待用户确认。
4. `Assets/Scenes/TalentTree.scene` 的 `TalentTreeOverlay` 上有一个 `m_Script: {fileID: 1230132438}`（只有 fileID 没有 guid）的空壳组件，运行时/打包报 `The referenced script ... is missing!`，建议清理（见 BUG-012）。
