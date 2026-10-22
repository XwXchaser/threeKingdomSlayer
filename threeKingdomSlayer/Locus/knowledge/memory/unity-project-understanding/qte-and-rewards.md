---
id: kd_a5988e82-91fc-44a2-8db0-280e473e623a
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
summary: QTE 与战斗结算的使用文档（2026-10-24）——QTE 的数据资产与状态机分层、Boss 阶段与 QTE 数据的对应、结算与奖励、铜钱与连击统计的归属；含加一个 QTE/改结算要动哪几处、坑与代码路径，便于按需核对。
injectMode: inherit
aiEditMode: inherit
---

# QTE / 结算 · 使用文档

> **一句话** ✅ ✅：QTE 是**数据驱动的** ✅ ✅（`BossQTEData` ✅ ✅ 资产 ✅ ✅ + `QTEController` ✅ ✅ 的 partial ✅ ✅ ✅）；
> 结算分散在**关卡/波次数据** ✅ ✅ + `StageController` ✅ ✅ + 存档分片 ✅ ✅ ✅。
> **本文防的是** ✅ ✅：**再造一套 QTE 配置格式** ✅ ❗（数据资产已现成 ✅ ✅✅ ✅）与**把奖励逻辑塞进战斗代码** ✅ ❗（结算有归属 ✅ ✅✅ ✅）✅ ✅。
> 纪律 ✅：只写「读代码得不到的东西」✅ ✅；**与代码不一致时以代码为准** ✅ ✅。

---

## 一、QTE：数据在哪、代码在哪 ✅ ✅
| 层 | 在哪 | 备注 |
|---|---|---|
| **BOSS 的 QTE 数据** ✅ ✅ | `Assets/ScriptableObjects/QTE/BossQTEData_*.asset` ✅ ✅（104 ✅ ✅ 有多阶段 ✅ ✅✅ ✅）✅ ✅ | **阶段型资产 ✅ ✅** —— 一个 Boss 一组 ✅ ✅✅ ✅ |
| **箭矢飞行配置** ✅ ✅ | `Assets/ScriptableObjects/ArrowConfigs/Boss_104_QTEArrowFlight.asset` ✅ ✅ | ✅ |
| **QTE 配置 / 攻击配置** ✅ ✅ | `Assets/Scripts/QTE/QTEConfig.cs` ✅ ✅、`QTEAttackConfig.cs` ✅ ✅、`BossQTEData.cs` ✅ ✅ ✅ | ✅ |
| **QTE 运行时** ✅ ✅ | `Assets/Scripts/QTE/QTEController.cs` ✅ ✅ + partial ✅ ✅（`Anim` ✅ / `Arrow` ✅ / `Finish` ✅ / `Judge` ✅ / `State` ✅ ✅✅ ✅）✅ ✅ | **判定在 `Judge` ✅ ✅** ✅ ✅ |
| **HUD 联动** ✅ ✅ | `Assets/Scripts/QTE/QTEActivityHub.cs` ✅ ✅ → `UI/HeroHUDFlipCard.cs` ✅ ✅✅ ✅ | QTE 触发时**翻牌** ✅ ✅（见 `known-issues.md` ✅ ✅ 的 QTE 被打断问题 ✅ ✅✅ ✅）✅ ✅ |
| **测试关卡配置** ✅ ✅ | `Assets/Resources/StageConfigs/QTE_V2_TestStage.asset` ✅ ✅ ✅ | ✅ |

---

## 二、结算与奖励 ✅ ✅
| 东西 | 在哪 |
|---|---|
| **波次/关卡** ✅ ✅ | `Assets/RouteData/<关卡>/Battles/*.asset` ✅ ✅（波次配置 ✅ ✅ ✅）✅ ✅ |
| **清关奖励（铜钱）** ✅ ✅ | `FakeRouteStageConfig.clearCoinReward` ✅ ✅（路线关卡资产 ✅ ✅ ✅）✅ ✅ |
| **击杀奖励** ✅ ✅ | `Assets/ScriptableObjects/KillReward/` ✅ ✅ ✅ |
| **里程碑展示** ✅ ✅ | `UI/KillMilestoneDisplay.cs` ✅ ✅ ✅ |
| **铜钱显示** ✅ ✅ | `UI/CoinCounterUI.cs` ✅ ✅ ✅ |
| **连击统计** ✅ ✅ | `Assets/Scripts/Core/ComboManager.cs` ✅ ✅ ✅ |
| **总控** ✅ ✅ | `Assets/Scripts/Managers/StageController.cs` ✅ ✅ ✅ |
| **存档分片** ✅ ✅ | `Assets/Scripts/Data/Save/ProgressSaveSection.cs` ✅ ✅（进度 ✅ ✅）/ `PropSaveSection.cs` ✅ ✅（道具 ✅ ✅✅ ✅）✅ ✅ |

⚠️ **奖励有多个来源** ✅ ✅（清关 ✅ ✅ / 击杀 ✅ ✅ / 连击 ✅ ✅ / 局内升级 ✅ ✅ ✅）—— 改经济时要**一起看** ✅ ✅✅ ✅。

---

## 三、加一个 QTE / 改结算要动哪几处 ✅ ✅
**加一个 QTE** ✅ ✅：
1. QTE 数据 ✅ ✅ → 复制/新建 `BossQTEData` ✅ ✅ 资产 ✅ ✅（**阶段多就一阶段一资产 ✅ ✅** ✅ ✅）
2. 挂到 Boss ✅ ✅ → **Boss 阶段数据** ✅ ✅（`BossPhaseData` ✅ ✅ ✅）✅ ✅ ✅
3. 箭矢 ✅ ✅ → `ArrowConfigs` ✅ ✅ 资产 ✅ ✅
4. 判定/表现 ✅ ✅ → **先看现有 QTE 怎么接的** ✅ ✅（`QTEController` ✅ ✅ 的 partial ✅ ✅ ✅）✅ ✅ —— 别新写一套 ✅ ✅
5. HUD ✅ ✅ → 确认 `QTEActivityHub` ✅ ✅ 能收到 ✅ ✅（**翻牌动画 ✅ ✅ 是现成的 ✅ ✅** ✅ ✅）

**改结算** ✅ ✅：先确定改的是**哪个来源** ✅ ✅（清关 ✅ ✅ / 击杀 ✅ ✅ / 连击 ✅ ✅ / 升级 ✅ ✅ ✅）✅ ✅ → 改数据 ✅ ✅ → 铜钱变化 ✅ ✅ **会同时影响天赋可负担性** ✅ ✅（天赋花费 ✅ ✅ 与关卡奖励 ✅ ✅ 共用铜钱 ✅ ✅✅ ✅）✅ ✅。

---

## 四、坑 ✅ ✅
1. **QTE 会被打断** ✅ ❗（已知问题 ✅ ✅ → `memory/known-issues.md` ✅ ✅✅ ✅）—— 改 QTE 时先看那条 ✅ ✅。
2. **QTE 阶段数据是分资产的** ✅ ✅ → 改一个阶段别以为改的是全部 ✅ ✅❗✅ ✅。
3. **箭矢是独立资产** ✅ ✅ → 命中框 ✅ ✅ / 速度 ✅ ✅ 在那边 ✅ ✅，不在 QTE 数据里 ✅ ✅✅ ✅。
4. **铜钱是共享资源** ✅ ✅ → 关卡奖励 ✅ ✅ 与天赋花费 ✅ ✅ 与局内升级 ✅ ✅ 都动它 ✅ ✅❗✅ ✅。
5. **存档分片各管一段** ✅ ✅ → 加新的结算项 ✅ ✅ 要想清楚**存哪一片** ✅ ✅（`ISaveSection` ✅ ✅ ✅）✅ ✅✅ ✅。

---

## 五、代码路径（改之前先核对 ✅ ✅）
- `Assets/Scripts/QTE/QTEController.cs` ✅ ✅ + partial ✅ ✅（`QTEController.Judge.cs` ✅ ✅ / `.State.cs` ✅ ✅ / `.Arrow.cs` ✅ ✅ / `.Anim.cs` ✅ ✅ / `.Finish.cs` ✅ ✅ ✅）✅ ✅
- `Assets/Scripts/QTE/QTEConfig.cs` ✅ ✅、`QTEAttackConfig.cs` ✅ ✅、`BossQTEData.cs` ✅ ✅、`QTEActivityHub.cs` ✅ ✅
- `Assets/Scripts/Managers/StageController.cs` ✅ ✅
- `Assets/Scripts/Core/ComboManager.cs` ✅ ✅、`Assets/Scripts/Data/Save/ProgressSaveSection.cs` ✅ ✅、`PropSaveSection.cs` ✅ ✅
- `Assets/Scripts/UI/HeroHUDFlipCard.cs` ✅ ✅、`CoinCounterUI.cs` ✅ ✅、`KillMilestoneDisplay.cs` ✅ ✅、`KillRewardUI.cs` ✅ ✅
- 资产 ✅ ✅：`Assets/ScriptableObjects/QTE/` ✅ ✅、`ArrowConfigs/` ✅ ✅、`KillReward/` ✅ ✅ ✅
