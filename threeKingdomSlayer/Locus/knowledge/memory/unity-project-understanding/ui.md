---
id: kd_a12c9d00-4f99-4eb6-98d7-82193dfa275c
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
summary: UI 域的使用文档（2026-10-24）——HUD/落地页/选关/天赋页/翻牌各系统的代码位置与职责、Hub 态与局内态的切换（HubHudModeSwitch）、竖屏分辨率适配与安全区、调试按钮；含加一个 UI 要动哪几处、坑与代码路径，便于按需核对。
injectMode: inherit
aiEditMode: inherit
---

# UI · 使用文档

> **一句话** ✅ ✅：UI 基本都是**代码生成** ✅ ✅（运行时按数据建 ✅ ✅）+ **少量预制体** ✅ ✅（HUD ✅ ✅ / 头像 ✅ ✅ / 面板 ✅ ✅ ✅）；
> 「落地页 ↔ 局内」✅ ✅ 两种态由 `HubHudModeSwitch` ✅ ✅ 统一切 ✅ ✅✅ ✅。
> **本文防的是** ✅ ✅：**再造一套 UI 框架** ✅ ❗（生成式 + 现成的态切换 ✅ ✅✅ ✅）与**在 UI 里写业务规则** ✅ ❗（规则在数据层 ✅ ✅✅ ✅）✅ ✅。
> 纪律 ✅：只写「读代码得不到的东西」✅ ✅；**与代码不一致时以代码为准** ✅ ✅。

---

## 一、按「界面」定位代码 ✅ ✅
| 界面 / 部件 | 代码 | 数据来源 |
|---|---|---|
| **局内 HUD 总成** ✅ ✅ | `UI/BattleHUD.cs` ✅ ✅ | 运行时注入 ✅ ✅（`StageConfig` ✅ ✅ / `WaveSpawner` ✅ ✅ ✅）✅ ✅ |
| **英雄头像 / 血条** ✅ ✅ | `UI/HeroHUD.cs` ✅ ✅ | 英雄 ✅ ✅ / 存档 ✅ ✅ ✅ |
| **铜钱计数** ✅ ✅ | `UI/CoinCounterUI.cs` ✅ ✅ | `SaveManager.GetCoins` ✅ ✅✅ ✅ |
| **连击显示** ✅ ✅ | `UI/ComboUI.cs` ✅ ✅、`ComboDisplayUI.cs` ✅ ✅、`ComboBuffHUD.cs` ✅ ✅ | `ComboManager` ✅ ✅✅ ✅ |
| **伤害数字** ✅ ✅ | `UI/DamageNumber.cs` ✅ ✅、`DamageNumberManager.cs` ✅ ✅ ✅ | ✅ |
| **敌人血条** ✅ ✅ | `UI/EnemyHealthBar.cs` ✅ ✅ + `EnemyHealthBarFactory.cs` ✅ ✅✅ ✅ | ✅ |
| **Boss 血条** ✅ ✅ | `UI/BossHealthUI.cs` ✅ ✅ | Boss 阶段 ✅ ✅✅ ✅ |
| **状态图标 HUD** ✅ ✅ | `UI/BuffDisplayPanel.cs` ✅ ✅、`BuffIcon.cs` ✅ ✅、`ElementalBuffHUD.cs` ✅ ✅ | 状态 ✅ ✅（两套 ✅ ✅ 见 `buff-and-status.md` ✅ ✅✅ ✅）✅ ✅ |
| **升级三选一弹窗** ✅ ✅ | `UI/UpgradeChoicePopup.cs` ✅ ✅、`UpgradeCard.cs` ✅ ✅ | `UpgradeChoiceManager` ✅ ✅✅ ✅ |
| **道具栏 / 丢弃** ✅ ✅ | `UI/FrontItemBar.cs` ✅ ✅、`ItemDiscardPopup.cs` ✅ ✅、`ItemDiscardPromptAdapter.cs` ✅ ✅ | `ItemInventory` ✅ ✅✅ ✅ |
| **大招按钮** ✅ ✅ | `UI/UltimateButtonUI.cs` ✅ ✅ | `HeroConfig.ultimateSkillConfig` ✅ ✅✅ ✅ |
| **蓄力指示** ✅ ✅ | `UI/ChargeIndicatorController.cs` ✅ ✅ | 蓄力状态 ✅ ✅✅ ✅ |
| **关卡进度条** ✅ ✅ | `UI/StageProgressBar.cs` ✅ ✅（+`.Flow.cs` ✅ ✅ / `.EditorPreview.cs` ✅ ✅✅ ✅）✅ ✅ | 波次 ✅ ✅✅ ✅ |
| **QTE 翻牌** ✅ ✅ | `UI/HeroHUDFlipCard.cs` ✅ ✅ | `QTEActivityHub` ✅ ✅✅ ✅ |
| **教学手势提示** ✅ ✅ | `UI/TutorialGestureHint.cs` ✅ ✅ | 教程 ✅ ✅✅ ✅ |
| **对话 / 气泡** ✅ ✅ | `UI/BattleDialogueView.cs` ✅ ✅、`DialogueBubbleLayout.cs` ✅ ✅、`DialogueManager.cs` ✅ ✅、`DialogueDatabase.cs` ✅ ✅ | 对话数据 ✅ ✅✅ ✅ |
| **落地页 / Tab 栏** ✅ ✅ | `UI/BattleHubTabBar.cs` ✅ ✅、`BattleStartStageButton.cs` ✅ ✅、`UIReadyFireEffect.cs` ✅ ✅、`UIReadyVerticalPulse.cs` ✅ ✅ | ✅ |
| **暂停菜单** ✅ ✅ | `UI/PauseMenuUI.cs` ✅ ✅ | ✅ |
| **选关页** ✅ ✅ | `UI/StageSelectUI.cs` ✅ ✅ | `RouteCatalogData` ✅ ✅（见 `stage-select-and-routes.md` ✅ ✅✅ ✅）✅ ✅ |
| **天赋页** ✅ ✅ | `UI/TalentTreePanel.cs` ✅ ✅ | `TalentTreeConfig` ✅ ✅（见 `talent-system.md` ✅ ✅✅ ✅）✅ ✅ |
| **里程碑 / 击杀奖励** ✅ ✅ | `UI/KillMilestoneDisplay.cs` ✅ ✅、`KillRewardUI.cs` ✅ ✅ | ✅ |
| **摄像机** ✅ ✅ | `UI/CameraManager.cs` ✅ ✅ | ✅ |

---

## 二、两种态：落地页 ↔ 局内 ✅ ✅
- `UI/HubHudModeSwitch.cs` ✅ ✅ + `UI/HubStateSource.cs` ✅ ✅ ✅ → **统一切「隐藏局内 HUD ✅ / 显示落地页 ✅」** ✅ ✅✅ ✅。
- → **加新界面时先问：它是落地页的东西还是局内的东西** ✅ ✅✅ ✅ → **别自己写一套显隐 ✅ ✅❗** ✅ ✅。

## 三、分辨率与安全区 ✅ ✅
- `UI/CanvasScalerAdapter.cs` ✅ ✅（竖屏适配 ✅ ✅）✅ ✅、`UI/SafeAreaAdapter.cs` ✅ ✅ ✅ ✅。
- → **新面板的尺寸按设计稿 1080×1920 口径填 ✅ ✅**（天赋页 `treePosition` ✅ ✅ 同一口径 ✅ ✅✅ ✅）✅ ✅。

---

## 四、加一个 UI 要动哪几处 ✅ ✅

> ⚠️ **已知缺口** ✅ ✅：**`TutorialStageDirector` ✅ ✅ 没挂在任何场景/预制体上** ✅ ❗（按 GUID 扫过 ✅ ✅ ✅ 零命中 ✅ ✅）
> → 教学期的 **手势引导 ✅ / 按波台词 ✅ / 固定给冰雾 ✅ ✅** 三件事**都没在跑** ✅ ❗❗
> （**不是重构造成的** ✅ ✅ —— 拆分前后都没挂 ✅ ✅✅ ✅）；✅ **要不要挂回去** ✅ ✅ 是内容决策 ✅ ✅ ✅ —— ✅ **不盲接** ✅ ✅❗✅ ✅
> 详见 `plan/project-audit-20261024.md` ✅ ✅ §七 ✅ ✅。
1. **能程序化生成** ✅ ✅ → 在生成代码里建 ✅ ✅（选关页 ✅ ✅ / 天赋页 ✅ ✅ 就是这么做的 ✅ ✅✅ ✅）✅ ✅ —— **别手摆一堆预制体 ✅ ✅❗** ✅ ✅
2. **需要美术面板** ✅ ✅ → 预制体 ✅ ✅ + 引用 ✅ ✅（HUD ✅ ✅ / 头像 ✅ ✅ ✅）✅ ✅
3. **数据来源** ✅ ✅ → 从**数据层**取 ✅ ✅（别在 UI 里写规则 ✅ ✅❗✅ ✅）✅ ✅
4. **显隐/态** ✅ ✅ → 走 `HubHudModeSwitch` ✅ ✅ ✅ ✅
5. **适配** ✅ ✅ → `CanvasScalerAdapter` ✅ ✅ + `SafeAreaAdapter` ✅ ✅ ✅ ✅
6. **手机可测** ✅ ✅ → `UI/DevSkipBattleButton.cs` ✅ ✅（跳过本节点战斗 ✅ ✅ —— **只在开发构建里 ✅ ✅**✅ ✅）✅ ✅

---

## 五、坑 ✅ ✅
1. **别在 UI 里写业务规则** ✅ ❗（解锁 ✅ ✅ / 可负担 ✅ ✅ / 可解锁 ✅ ✅ 全在数据层 ✅ ✅✅ ✅）✅ ✅
2. **显隐别各写各的** ✅ ❗ → 走 `HubHudModeSwitch` ✅ ✅✅ ✅ ✅ ✅
3. **面板尺寸按 1080×1920 口径** ✅ ✅（天赋页 ✅ ✅ 已是这个口径 ✅ ✅✅ ✅）✅ ✅
4. **QTE 翻牌会被打断** ✅ ❗（已知问题 ✅ ✅ → `memory/known-issues.md` ✅ ✅✅ ✅）✅ ✅
5. **开发按钮只在开发构建** ✅ ✅（`DevSkipBattleButton` ✅ ✅ ✅）✅ ✅ —— 别带进正式包 ✅ ✅
6. **UI 文件多（~55 个 ✅ ✅）** ✅ ✅ → 找界面先按**文件名 + 用途**找 ✅ ✅（`UI/` ✅ ✅ 下按界面分文件 ✅ ✅✅ ✅）✅ ✅

---

## 六、代码路径（改之前先核对 ✅ ✅）
- `Assets/Scripts/UI/` ✅ ✅（~55 个文件 ✅ ✅ —— 按界面分 ✅ ✅✅ ✅）✅ ✅
- `Assets/Scripts/UI/HubHudModeSwitch.cs` ✅ ✅、`HubStateSource.cs` ✅ ✅、`CanvasScalerAdapter.cs` ✅ ✅、`SafeAreaAdapter.cs` ✅ ✅、`DevSkipBattleButton.cs` ✅ ✅ ✅ ✅
- `Assets/Scripts/Managers/StageController.cs` ✅ ✅（局内总控 ✅ ✅✅ ✅）✅ ✅
- 相关 ✅ ✅：`stage-select-and-routes.md` ✅ ✅、`talent-system.md` ✅ ✅、`buff-and-status.md` ✅ ✅、`attacks-moves-and-heroes.md` ✅ ✅✅ ✅
