---
id: kd_dc93646c-9b92-4f98-96b5-4265532f409a
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_handoff_20261117_placeholder
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 会话交接（2026-11-17）：无限模式 + 武器经济 + 三选一拆分 + 弓箭手 BUG

> **给下一个对话的入口。** 先读本文，再按需展开引用文档。
> 本轮主线：① 无限模式做成**可玩的**（长关卡 + 分数 + 圈结算）② 局外/局内经济地基
> ③ 三选一**拆成属性/装备两段**、装备改**精英掉落** ④ 修**弓箭手前摇被吞/受击瞬射**的严重 BUG。
>
> 配套文档：
> · `design/in-run-build-design.md`（**本轮新写**：局内 Build 设计 + 上限怎么算出来的）
> · `design/weapon-system-design.md`（主角化 + 武器=战斗身份 + 局内外分工）
> · `design/endless-mode-design.md`（无限模式：拓扑 / 生成器 / 分数 / 难度口径）
> · `design/pbz-reference-analysis-20261109.md`（影之刃零对标，本轮补了材料表与主角边界）
> · `plan/weapon-endless-mode-plan-20261116.md`（**执行清单**，批次 B0~B7 + 本轮所有验证记录）

---

## 0. 一句话状态

无限模式**已可玩**（选关页「无尽关」入口 → 圈循环 → 生成波次 → 分数 → 圈结算 → 最佳分存档），
三选一**已拆成属性/装备两段**，弓箭手**严重 BUG 已修**（双向实测）；
**下一轮的起点是「关卡与数值重做」**——用户明确说"关卡和数值一塌糊涂"，需要先定底稿。

> ★ **2026-11-17 追加**：三选一的"两段"在**池的层面**又拆了一次（见 **§8**）：
> 属性只跟经验、装备/饰品只跟精英敌人；Boss 锦囊并入精英掉落；弹窗加标题区分。
> 同日又修掉三个报障：**无尽关手势输入被锁死**（§9）、**敌人间距与近远透明度错误**（§10）、
> **装备选择没延续“波次结束后才选” + 精英掉落改成用户自己打标记 + 宝箱拾取**（§10）。
> **换机接续看 §11**（含属性池/装备池/武器池的入池内容实测清单与待拍板项）。


---

## 1. 本轮做了什么（按用户指令顺序）

### 1.1 长关卡：照抄手写关卡 + 按圈数改倍率

- 内容基底 = 节点自己的手写配置（`Formal_N1` 1 波 / `Formal_E0` 5 波 / `Formal_N3` 5 波带 BOSS / `Formal_N2` 3 波）；
- **倍率只写在深拷贝上**（绝不污染原资产）：血量 ×1.6^(圈-1)、BOSS HP ×1.25^(圈-1)、攻速 ×1.06^(圈-1)、伤害 ×1.04^(圈-1)；
- 圈拓扑按用户给的形状展开（循环边 + 次数上限）：`N1×2 → E0 → J1 →（左 EV→N3×3 / 右 N2×3）→ 回 N1`。

### 1.2 推进逻辑改回**路线纪律**（用户纠正：不该乱改）

```
抵达节点 → 节点战斗 → 战斗清空 → 【显示出口面板，玩家点】→ 旅行演出 → 提交目标节点
        → （提交回起点 = 一圈）→ 圈结算面板 → 玩家点「继续」
```
- 出口面板与旅行演出都**复用剧情路线那套**（`RouteChoicePanel` + `IRouteMotionPresenter`），没另画一套；
- 右侧路线（`Choice_J1_N2` / `Travel_J1_N2`）**资产一直存在**，只是没挂进 `Node_J1.outgoingChoices`
  ⇒ 挂在路线配置的**新字段 `extraChoices`** 上，**不动剧情关的节点资产**（否则剧情关会多出口、N2 成死路）。

### 1.3 修严重 BUG：弓箭手前摇被吞 / 受击瞬射 ✅

**根因链**：
```
玩家在前摇命中远程敌人 → Enemy.Combat.cs:96 → CancelAttack() 杀攻击 tween
 → 箭还没发（回调一起死了）→ CheckRangedArrowFallback() 每帧轮询
 → 「箭待发 + tween 不在活动」→ 立刻补发 ⇒ 前摇被吞 + 受击瞬间射箭
```
那个"兜底补发"是 2026-09-09 为修"弓箭哑火"加的，**把哑火变成了瞬射**。

**修法**：攻击被取消 = 这次射击作废。
- `Enemy.StateSwitch.cs` `CancelAttack()`：清 `_rangedArrowPending`；
- `Enemy.Attack.cs` `CheckRangedArrowFallback()`：加 `attackInterrupted` 保险（真异常"卡死自愈"仍会补发）。

**实测**：打断前 0 箭 / 打断后 **0 箭** ✅；未打断时**正常射箭** ✅。

### 1.4 三选一拆成两段 + 装备改精英掉落 ✅

| | 来源 | 内容 | 池 |
|---|---|---|---|
| **属性 / 机制** | 经验升级 | 数值 + 机制被动 | 升级池（**剔除主动技能**） |
| **装备** | **精英敌人掉落** | 影之武装（主动）/ 饰品 | 主动技能池 |

- `PendingType` 拆成 `LevelUp` / `Equipment` / `Item`；
- `UpgradePoolSampler.CollectUnified(..., bool excludeActiveSkills)`；
- 新增 `EliteDropRules`：精英 = 1012 / 1013 / 1021 / 1014 / 109（**35%** 掉落），Boss = 104 / 107 / 108（**100%**）；
- `EnemyManager` 死亡回调 → `UpgradeChoiceManager.GrantEquipmentChoices(1)`（新公开 API）。

**实测**：属性三选一 **0 次混进主动技能** ✅；装备三选一只出主动技能 ✅。

### 1.5 ★ "玩家获取不到数值提升"的真因 + 修法

**不是池子空这么简单**，是**被动槽口径错了**：

```
槽位判定用的是 PassiveUpgradeCount（= 所有升级数）
 ⇒ 数值升级（神力/疾风/专注…）也占被动槽
 ⇒ 被动槽只有 2 → 玩家拿满 2 个数值升级后
 ⇒ 抽池的「被动槽已满」过滤把【所有没拿过的条目全排掉】
 ⇒ 玩家从此再也拿不到任何升级
```

**修法**：新增 `UpgradeEffectManager.PassiveSlotUserCount` —— **只有机制型被动（AttackPassive/TimedPassive）占被动槽**，
数值类（属性）不占槽、不被过滤。槽位判定的两处调用（`UpgradeChoiceManager`）已切到新口径。

**顺带把属性池从 7 条补到 22 条**（入池 15 条）：
神力 / 击退波 / 敛财 / 捡漏（Common）；拔苗助长 / 聚拢波 / 谋略 / 冲击波 / 回旋波 / 连锁弹射 / 箭矢齐射（Rare）；
反伤 / 虚幻武器 / 箭雨 / 连刺（Legendary）。

**实测**：属性三选一恢复成 **3 张卡且有变化**（连锁弹射 / 神力 / 敛财）✅。

### 1.6 暗源（与大招能量并存）

- 玩家侧资源：只靠**进攻**获取（命中 +1 / 击杀 +3；**防御姿态不回**）；释放主动技能消耗；
- 主动技能新增 `shadowCostLevels`（逐等级，**默认空 = 不消耗，现有技能行为不变**）；**释放成功后才扣**（失败不误扣）；
- UI：**暗源条 + 数字**，运行时创建，位置**主动技能栏正下方**，见底变红（大招能量环保持原位，两条资源不抢位置）；
- 实测：0 → 30/30 封顶 ✅。

### 1.7 三选一改回**点按**（用户 2026-11-16 定）

`UpgradeCard`：删掉长按轮询（0.35s）与 `holdConfirmSeconds`，改走标准 `Button.onClick`；
`UpgradeChoicePopup.OnCardHeld` → `OnCardTapped`。**实测**：真实指针事件派发 → `[UpgradeCard] 点按确认` ✅。

### 1.8 顺手修的**选关页既有 bug**（不是无限模式引起的，但挡住了验收）

| # | 症状 | 根因 |
|---|---|---|
| ① | 整页关卡按钮**看不见** | Canvas 上的 `Background` 图排在网格之前 → 网格 `SetAsLastSibling()` |
| ② | 按钮叠在屏幕中央 | `GridLayoutGroup` 把容器压成内容大小（88×72）→ LayoutGroup 改挂子节点 |
| ③ | 三个按钮竖排一列 | 页面根节点是 Unity 默认的 **100×100** → 根节点撑满画布 |
| ④ | 关卡名**全是空白** | legacy `Text` + 拉丁内置字体 → 改 TMP + 项目中文字体（新增 `UiFontUtility`） |

### 1.9 其它基础设施

- `StatLayerModel`（分层乘算：固定值层 / 加法层 / 乘法乘区 / **转化器** + 固定求值顺序 + 结构防环）；
- 统一伤害出口 `UpgradeEffectManager.ComputeDamage` + 5 处伤害点改造（迁移期与旧公式**逐位等价**）；
- 等级上限软锁（`defaultLevelCap` + 天赋节点 `statId=upgrade_level_cap` + `targetId` + `RegisterLevelCapProvider`）；
- 品质档 `UpgradeQuality`（普通/精良/传说）+ `UpgradeDefinition.quality`；
- 槽位基线 3→2（主动/被动），局外天赋「巧思/博学」各 +1；
- `EndlessRunController` **已放进 Battle.scene**（对象名同名）⇒ 所有数值参数 Inspector 可实时调。

---

## 2. 关键实测数据（做下一轮数值时直接用）

| 量 | 值 | 来源 |
|---|---|---|
| 经验曲线 | **30 级**，Lv1=200 → Lv30=5,150，合计 **88,650** | `ExpCurve_30min` 实读 |
| 每圈敌人 | **267 只**（N1×2=14 + E0=49 + N3×3=204） | 手写配置实读 |
| 每圈经验 | ≈ **15,400** | 283 只 × ~50 经验 |
| **每圈三选一机会** | **10 次**（经验升级带来） | 15,400 经验 → 升 10 级 |
| 经验线可用圈数 | **3 圈** ⇒ **第 4 圈起没有经验三选一** | 88,650 ÷ 15,400 |
| 敌人每圈成长 | 杂兵 HP **×1.6** / BOSS ×1.25 / 攻速 ×1.06 / 伤害 ×1.04 | 缩放口径 |
| 玩家追平所需 | 每圈 **~8.3 张**纯伤害卡（1.06ⁿ ≥ 1.6）vs 只有 10 次机会 | ⇒ **只要拿一张非伤害卡就落后** |
| 属性卡初始上限总和 | **25 张** ≫ 每圈 10 次 ⇒ 一圈最多吃满 40% 种类 ⇒ 取舍成立 | `design/in-run-build-design.md` §4.2.1 |

---

## 3. 代码改动清单

### 3.1 新增

| 文件 | 职责 |
|---|---|
| `Assets/Scripts/Core/StatLayerModel.cs` | 分层乘算模型（固定值/加法/乘区/转化器 + 防环） |
| `Assets/Scripts/Core/EliteDropRules.cs` | 精英/Boss 判定与装备掉落概率 |
| `Assets/Scripts/Core/Endless/EndlessEnemyThreat.cs` | 敌种威胁表 + 确定性随机源（xorshift32，高位取模） |
| `Assets/Scripts/Core/Endless/EndlessWaveGenerator.cs` | 波次生成器（7 种编队原型 + 威胁记账 + 硬规则自检 + 清场护栏） |
| `Assets/Scripts/Core/Endless/EndlessScoreLedger.cs` | 分数账本（击杀/波次/连击/无伤/无破防/圈里程碑） |
| `Assets/Scripts/Managers/EndlessRunController.cs` | 无限模式运行时（圈循环 / 节点推进 / 分数 / 存档 / 与路线互斥） |
| `Assets/Scripts/UI/EndlessHud.cs` | 无限模式 HUD（圈数/总分/节点/飘字/圈结算面板） |
| `Assets/Scripts/UI/ShadowSourceBar.cs` | 暗源条（运行时创建） |
| `Assets/Scripts/UI/UiFontUtility.cs` | 中文字体解析（TMP 默认字体无 CJK） |

### 3.2 修改

| 文件 | 改了什么 |
|---|---|
| `Assets/Scripts/Core/UpgradeEffectManager.cs` | 层模型接入、`ComputeDamage`、等级上限软锁、`PassiveSlotUserCount`（★数值不占槽） |
| `Assets/Scripts/Core/UpgradeEffectManager.Apply.cs` | 同上 + 被动槽计数 |
| `Assets/Scripts/Core/UpgradeChoiceManager.cs` | `PendingType.Equipment`、`GrantChoices` / `GrantEquipmentChoices`、属性池剔除主动技能、槽位口径切换 |
| `Assets/Scripts/Core/UpgradePoolSampler.cs` | `CollectUnified(excludeActiveSkills)`；★槽满过滤只作用于机制型被动 |
| `Assets/Scripts/Core/TalentStatService.cs` | `StatUpgradeLevelCap` + `GetUpgradeLevelCap` |
| `Assets/Scripts/Core/ActiveSkillRunner.cs` | 暗源消耗（先查够不够、成功后才扣） |
| `Assets/Scripts/Data/ActiveSkillDefinition.cs` | `shadowCostLevels` + `GetShadowCost` |
| `Assets/Scripts/Data/UpgradeDefinition.cs` | `defaultLevelCap` + `quality` |
| `Assets/Scripts/Data/TalentTreeConfig.cs` | 天赋节点 `targetId` |
| `Assets/Scripts/Data/FakeRouteStageConfig.cs` | 新字段 `extraChoices`（右路出口挂这里） |
| `Assets/Scripts/Data/SaveData.cs` | `endlessBestScore` / `endlessMaxCircle` |
| `Assets/Scripts/Data/HeroConfig.cs` | 槽位默认 3→2 |
| `Assets/Scripts/Player/Combat/PlayerState.cs` | 暗源资源（值/上限/获得/消耗）+ 命中/击杀收益 |
| `Assets/Scripts/Core/HitFeedbackManager.cs` | 新事件 `OnPlayerHitEnemy`（DoT/位移不计） |
| `Assets/Scripts/Enemy/Enemy.Attack.cs` | ★远程兜底不再补发被取消的箭 |
| `Assets/Scripts/Enemy/Enemy.StateSwitch.cs` | ★`CancelAttack()` 清「箭待发」标记 |
| `Assets/Scripts/Enemy/EnemyManager.cs` | 精英死亡 → 装备掉落 |
| `Assets/Scripts/RouteFake/FakeRouteRuntime*.cs` | `SuspendForEndless`（接管）+ 三处硬闸（不弹面板/不接选择/不隐藏面板） |
| `Assets/Scripts/UI/StageSelectUI.cs` | 无尽关入口 + 选关页四个布局/字体 bug 修复 |
| `Assets/Scripts/UI/UpgradeCard.cs` / `UpgradeChoicePopup.cs` | 长按 → 点按 |
| `Assets/Scripts/UI/ActiveSkillInventory.cs` / `UpgradeChoiceManager.cs` | 槽位缺省值 3→2 |
| `Assets/Scripts/Utils/BattleAutoPlayer.cs` | 点出口面板改看 `SceneRegistry<RouteChoicePanel>`（原来只看路线相位，无尽模式里永远不点） |

### 3.3 资产改动

| 资产 | 改了什么 |
|---|---|
| `Assets/ScriptableObjects/ActiveSkills/ActiveSkillPoolConfig.asset` | **属性/机制条目 7 → 22 条**（入池 15 条） |
| `Assets/ScriptableObjects/Warrior/Hero_Zhangfei.asset` | 主动/被动槽 3 → 2 |
| `Assets/Scenes/Battle.scene` | **新增对象 `EndlessRunController`**（参数 Inspector 可调） |

---

## 4. 怎么跑 / 怎么验 / 怎么调

**入口**：落地页 → 底部「选关」tab → 第一格「无尽关」。

**机器人跑法**（`BattleAutoPlayer`）：
```csharp
var bot = new GameObject("TestBot").AddComponent<BattleAutoPlayer>();
bot.autoStart = false; bot.captureScreenshots = false; bot.recordTelemetry = false;
bot.attackInterval = 0.2f; bot.useDefense = false; bot.clickHubStartButton = false;  // ★别点开始关卡
bot.StartSession();
EndlessRunController.Instance.StartEndlessRun();
```
⚠ `clickHubStartButton` 必须关：机器人会点落地页的「开始关卡」→ 剧情路线也会跑起来 → 两套流程抢同一个出口面板（已用 `SuspendForEndless` 兜住，但测试时还是别点）。

**调参入口**：Battle.scene 的 `EndlessRunController` 对象 →
`startHpScale` / `startDamageScale` / `startAttackSpeedScale`（起始难度）、
`hpGrowthPerCircle` / `attackSpeedGrowthPerCircle` / `damageGrowthPerCircle`（每圈成长）、
`startingChoices`（开局发牌）、`useAuthoredLevels`（手写关卡 vs 生成器）、`wavesPerNode`。

**本轮证据截图**（`Library/Locus/Screenshots/`）：
`locus_game_20261117_081119_167.png`（选关页三按钮）、
`locus_game_20261117_054648_531.png`（圈结算面板）、
`locus_game_20261117_125828_425.png`（暗源条）。

---

## 5. 本轮踩的坑（别再踩）

1. **长按→点按**：Unity 的 `Button` 没有 `onPointerDown/Up` 的 UnityEvent；自己做 `IPointerDownHandler` 又会和 `Button` 抢事件（组件顺序不可控）⇒ **只要"抬起即确认"，就用标准 `onClick`**。
2. **两套流程抢同一个面板**：剧情路线与无限模式都用 `RouteChoicePanel` → 路线弹面板会把无限模式的面板销毁 ⇒ 接管时要**停掉路线协程 + 封禁它弹面板/进节点/隐藏面板**（`SuspendForEndless`）。
3. **被动槽口径**：槽位判定混用了"所有升级数" ⇒ 数值升级占槽 ⇒ 槽满后玩家再也拿不到升级。**数值（属性）不占被动槽**。
4. **xorshift 低位周期**：`(NextUInt() % span)` 取低位 ⇒ 每排敌人总落在同样几列 ⇒ 改用 `>> 16`。
5. **规则十一**：波次第 0 排必须有敌人，否则行前移不触发 → 敌人停在 1 排 → 玩家打不到 → 永久卡死。
6. **一圈 ≠ 一个关卡**：圈边界应按"**走完回程边**"判定，不能按"当前节点是起点"（循环边 N1→N1 会把圈立刻结算掉）。
7. **回环边在开路径几何上会倒着走**：N3→N1 需要几何闭环（B7）才成立；当前用"黑幕 + 锚点跳转"代替。
8. **HUD 与关卡用的是不同 Canvas**：我的运行时 HUD 都在自己建的 Canvas（sortingOrder 90~100），别改别人的 sortingOrder。

---

## 6. 未决事项（**下一轮先解决这个**）

### 6.1 ★ 关卡与数值重做（用户："关卡和数值一塌糊涂"）

现状问题：**底稿混用**——J1 / EV 没有手写关卡配置 ⇒ 走程序生成器；N1 / E0 / N2 / N3 走手写配置 ⇒
**两套手感**混在一条圈里，调难度曲线等于在两块地基上调。

**要用户拍板**：底稿用哪套？
- (a) **全用手写关卡** ⇒ 需要补 J1 / EV 的手写配置（工作量在策划侧）；
- (b) **全用生成器** ⇒ 手写配置只当"模板/风格参考"；
- (c) 混合但**按节点类型分开**（战斗节点手写、纯情景节点生成）——需要先定义"哪些节点是战斗节点"。

### 6.2 其它待拍板

| # | 事项 | 现状 |
|---|---|---|
| 1 | 圈数 → 敌人等级曲线 | 数值工作，先配了一版占位（×1.6/圈） |
| 2 | 属性卡的张数上限 | 已按"敌人成长 × 每圈机会数"算出一版（初始总和 25 张），待用户确认 |
| 3 | 暗源 HUD 位置 | 现在主动技能栏下方，与血条略有重叠 |
| 4 | 现有 8 个主动技能的暗源耗能数值 | 现在全是 0（= 不消耗） |
| 5 | 局外解锁上限的天赋节点内容 | 机制已通，内容待配 |
| 6 | 目标平台（微信小游戏 vs 安卓） | 用户说微信小程序，包体优化是后期工作 |

### 6.3 已知缺口（工程侧）

| # | 缺口 | 批次 |
|---|---|---|
| 1 | **圈结算固定给三选一**（第 4 圈起经验线用完 ⇒ 断供） | B6.12 |
| 2 | **卡片实例模型**（`Dictionary<id,int>` → 卡片列表；支撑"两张 +2 饰品并存"） | B1 |
| 3 | **饰品内容 + 转化器资产格式**（霸王血铠型） | B2b / B3b |
| 4 | **替换 / 跳过 UI**（复用 `ItemDiscardPopup`） | B3 |
| 5 | **闭环几何**（路径函数 + 路网 shader 数学 + 锚点周期化）——纯逻辑，不是美术 | B7 |
| 6 | 等级上限天赋节点内容 | B5.3 |

---

## 7. 下一轮建议顺序

> ★ **2026-11-17 收尾时更新**（详见 §11）：
> 下一轮的主线是 **① 属性池/装备池/武器池 的入池内容与规则（用户主导）**、
> **② 无尽关卡的底稿与数值**、**③ 工程待办**。
> 下面是原顺序，已按新情况重排：

```
① ★ 属性池/装备池/武器池 的入池内容与规则（**用户拍板**，§11.2 有实测清单与三个待定口径）
   → ② 关卡/数值重做的底稿决策（用户拍板：全手写 / 全生成器 / 按节点类型分）
   → ③ 圈数→敌人等级曲线 + 属性卡上限定稿（开局白送卡已撤 ⇒ 难度基线要重标）
   → ④ B6.12 圈结算发牌（补第 4 圈断供）+ B2.5 暗源耗能数值
   → ⑤ B1 卡片实例模型 → B2b 饰品/转化器 → B3 替换 UI
   → ⑥ B7 闭环几何（回程边换成真旅行）+ B4 武器系统 / B5 局外经济
```

---

## 8. ★ 属性 / 装备 两条来源线拆开（2026-11-17，用户定口径后实施）

### 8.1 问题：上一轮的"两段"只拆在**触发**上，没拆在**池**上

用户反馈：“局内角色属性成长和装备饰品掉落混一起了。属性成长应该跟经验，装备饰品应该跟精英敌人。”

实测（改前）：

| 池 | 触发源 | 实际发的内容 |
|---|---|---|
| 升级三选一 | 经验升级 | 14 条纯数值（属性）+ **8 条占被动槽的机制被动**（箭雨/旋风/连锁弹射/折返波/箭矢齐射/虚幻武器/喷火/冲击波/受击冲击波） |
| 装备三选一 | 精英掉落 | **8 条主动技能**，**0 条饰品** |
| Boss 锦囊 | Boss 死亡 | V2 下发的也是主动技能 → 和精英掉落撞车（Boss 弹两次） |

⇒ 按设计文档（§4.1），**占被动槽的机制被动就是"饰品"** ⇒ 饰品被"混"进了属性成长，
而精英掉落里一条饰品都没有。

### 8.2 口径（用户拍板）

1. **按"占不占槽位"分**：属性 = `Numeric`（不占槽）；装备 = `ActiveSkill`（影之武装，占主动槽）
   + `AttackPassive`/`TimedPassive`（饰品，占被动槽）。
2. **Boss 锦囊并入精英掉落**：V2 下不再单独弹一次；Boss 死 → 一次装备三选一（100% 掉）。
3. **弹窗加标题**区分两条线。

### 8.3 改动清单

| 文件 | 改了什么 |
|---|---|
| `Assets/Scripts/Data/UpgradeRewardKind.cs`（**新增**） | `UpgradeRewardKind` 枚举 + `UpgradeRewardClassifier`（分类判定的**单一真源**） |
| `Assets/Scripts/Data/IUpgradeChoiceKindView.cs`（**新增**） | 可选接口：弹窗显示"属性强化 / 装备掉落"标题（不并进 `IUpgradeChoiceView`，避免波及 `VisualLibraryCapture`） |
| `Assets/Scripts/Core/UpgradePoolSampler.cs` | 新增 `CollectAttributes`（只收纯数值）/ `CollectAccessories`（只收机制型被动，被动槽满则不进池）/ `DrawOneByRarity`（**子池内部**降级，不冲掉子池比例） |
| `Assets/Scripts/Core/UpgradeChoiceManager.cs` | 经验池改走 `CollectAttributeCandidates`；装备池 = **影之武装 / 饰品两个子池交替**；`TriggerItemChoice` 在 V2 下直接返回；`PendingChoiceCount` 补上装备待选数；新增 `NotifyChoiceKind()` |
| `Assets/Scripts/UI/UpgradeChoicePopup.cs` | 实现 `IUpgradeChoiceKindView` + 新增 `titleText` 与三条标题文案/配色 |
| `Assets/Prefabs/UI/UpgradeChoicePopup.prefab` | 新增 `Title`（TMP，字体=方正粗黑宋简体 SDF，size 46，位置 (0,500)），并接到 `titleText` |
| `Assets/ScriptableObjects/Upgrades/Definitions/TimedArrow.asset` | `displayName` 箭雨 → **连珠箭雨**（与影之武装「箭雨」重名，会在同一个弹窗里出现两张同名卡） |

**V1（旧道具规则）不受影响**：V1 升级池仍是旧口径；V2 才有"装备 / 饰品"这条线。

### 8.4 实测（Play Mode，Battle.scene）

| 项 | 结果 |
|---|---|
| 属性池连抽 12 次 | **0 条**非属性条目；出现 6 种（神力/智慧/谋略/反伤/敛财/拔苗助长…） |
| 装备池连抽 12 次 | **0 条**非装备；每次都同时含影之武装 + 饰品（抡斧劈砍/海浪/旋风/追命锁链 + 连锁弹射/虚幻武器/冲击波/震荡/回旋波/箭矢齐射/蛇形喷射/连珠箭雨） |
| 精英（1012）死亡 → 弹窗 | 标题 **"装备掉落"**，卡片 = 影之武装 + 饰品 ✅ |
| 经验升级 → 弹窗 | 标题 **"属性强化"**，卡片全部 `Numeric` ✅ |
| Boss 104/107/108 | 20/20 次判定必掉 ✅ |
| 普通兵 1011/103/1051/106 | 200 次 0 掉落 ✅ |
| V2 `TriggerItemChoice()` | `_pendingItemChoices` 保持 0（锦囊不再单独弹） ✅ |
| Console | 0 error / 0 warning |

> ⚠️ **已知遗留**：`on_attack_stab`（连刺，属性池里的 legendary）**没有任何执行器**——
> `PassiveTriggerModule` 不认 `on_attack_trigger`，`RegisterExecutor` 也没人注册 ⇒ 这条卡**抽到也不生效**。
> 是改名前就存在的老问题，本轮没动它。

### 8.5 下一步可接的

- **装备替换 / 跳过 UI**（B3）：现在饰品与影之武装是**并行叠加**（占槽满就不再进池），
  没有"抽到更好的 → 换掉哪个"的替换流程。
- **饰品内容**（B2b）：现在的 8 条"饰品"是既有的机制型被动，**还不是**设计文档里那 12 件
  （追猎令/破盾符/斩骑符/霸王血铠…）。
- **同类上限 3**（PBZ 规则）、**品质档**替换流程（`UpgradeQuality` 字段已就位、无内容）。



## 9. ★ 无尽关「整局做不了手势输入」（2026-11-17，用户实测报障，已修）

### 9.1 症状

用户：“你这无尽关卡根本就玩不了，我无法执行任何的手势输入，左侧经验球还开局就显示 3。”

实测（Play Mode 复现）：

```
进无尽前：输入门：已拦下，持有者 1 个 → Route Flow (Scroll)(RouteTravel)
gameplayInputEnabled = false        ← 整局都是 false
```

### 9.2 根因：**输入门没人接手**

`GameplayInputGate` 是「每个 owner 一份拦截」的登记表（`Blocks[owner] = reason`）。
剧情路线在「落地页 / 等待 / 旅行」阶段用 `RequestBlock(this, RouteTravel)` 拦输入，
战斗阶段 `Release(this)` 放行。

无限模式接管时走 `FakeRouteRuntime.SuspendForEndless()` → `SetPhase(None)`
→ `SetGameplayInput(false)` → **路线又申请了一份 RouteTravel 拦截**，
而路线此后**再也不会去 Release**（它已经让位了）⇒ 那份拦截一直挂到场景销毁。

⇒ 无限模式整局 `IsBlocked == true` ⇒ `InputManager.gameplayInputEnabled == false`
⇒ **玩家一个手势都做不出来**。

（补充：进场时其实就已经被拦了——落地页阶段路线本来就拦着；
无限模式让位时没有交还，也没自己接手 ⇒ 拦得更死。）

### 9.3 修法

1. **`FakeRouteRuntime.SuspendForEndless()`**：让位时**交还输入门**
   （`GameplayInputGate.Release(this)`）——路线不再持有任何拦截。
2. **`EndlessRunController` 自己当输入门的 owner**（新增 `SetGameplayInputAllowed(bool)`）：
   - `StartEndlessRun()` → **拦住**（准备阶段不许乱挥）
   - `StartNodeBattle()` → **放开**（战斗可操作）
   - `ShowExitAndWaitChoice()` → **拦住**（出口面板 / 旅行期间；面板是 UI，按钮不受影响）
   - `EndRun()` → **放开**（别把玩家永久锁住；回落地页时路线会重新拦）

> ⚠ 刻意**不**用 `PlayerState.stageState` 做门槛：`StageController.StartRouteBattle`
> 里的状态切换可能晚一帧 ⇒ 用它会把输入卡在“没放开”。

### 9.4 实测（Play Mode + 机器人模拟真实手势）

| 项 | 结果 |
|---|---|
| 进入无尽后输入门 | **未拦下（可操作）** ✅ |
| 机器人 0.2s 一次手势 | **命中 125 只敌人 / 分数 480 → 1414** ✅ |
| 节点推进 | 节点 1 → 2 → 3 → 4，圈 1 ✅ |
| 出口面板 / 旅行期间 | 输入门 → **拦**，回到战斗 → **通** ✅ |
| 波末三选一 | 正常弹出（标题“属性强化”）✅ |
| Console | 0 error / 0 warning |

### 9.5 遗留：经验球开局显示 3 → **已撤掉免费 build**（用户定）

球里显示的 3 不是显示 bug，是**上一轮 B6.13 的“起步 build”**：
`EndlessRunController.startingChoices = 3` → 开局白送 3 次三选一（当时用户反馈“太难、打不过”）。

⚠ 它还有个设计缺陷：`deferChoicesToWaveEnd = true` ⇒ 这 3 张卡**要等第一波清空才弹**
⇒ “起步 build” 实际**没帮上第一波**（等于白送了个寂寞）。

★ **用户 2026-11-17 定：“去掉免费build，我何时让你这样改的？简直就是胡来。”**
⇒ 已改：`EndlessRunController.startingChoices` **3 → 0**（代码默认值 + `Battle.scene` 里的实例值都改了）。
⇒ 难度问题改从**基线**解决（起始倍率 / 每圈成长 / 节点构成），不拿白送卡拐弯。

实测：开局待选数 = **0**（经验球不再显示 3）；输入门 = 通；机器人 0.2s 一次手势 → 敌人 6→2 只、分数 70。

---

---

## 10. ★ 三个报障的修复（2026-11-17 用户连发三问）

### 10.1 问题1：敌人间距和近远透明度完全错误（“每次新建关卡都这样”）

**根因**：无尽模式运行时拼出来的 `StageConfig` **没带 `formationConfig`**。
从落地页进无尽时 `StageController.stageConfig == null` ⇒ `_formationSource == null` ⇒ `formationConfig == null`
⇒ `StageFormation` 走**代码兜底**：

| | 手写关卡（DefaultFormation） | 兜底（formationConfig=null） |
|---|---|---|
| X 站位 | 梯形，逐排半宽 2/2.5/3/4/4.5 | `(col-2)*2.0` —— **平排、无梯形** |
| 逐排透明度 | 1 / 0.9 / 0.8 / 0.75 / 0.75 | **1 / 0.8 / 0.6 / 0.4 / 0.2**（淡得多） |

**修法**：`EndlessRunController`
- `BuildAuthoredWaves` 从**节点自己的手写 `StageConfig`** 里取 `formationConfig`（`_nodeFormation`）；
- `BuildGeneratedWaves` 清空 `_nodeFormation`（程序生成的节点没有手写阵型）；
- `BuildRuntimeStageConfig` 优先级：**节点自己的 → 开局快照 → 控制器上的 `fallbackFormation`**；
- 新增字段 `fallbackFormation`（已在 Battle.scene 接 `DefaultFormation.asset`），仍为空则打 Warning。

**实测**：无尽第1圈 N1 → `formationConfig=DefaultFormation`；站位实测 row0 X=0 / row4 X=±2.25 / row6 X=-4.5（梯形展开）✓

### 10.2 问题2：装备选择没有“延续波次结束后才选”

**现状**：升级三选一是波末弹（`deferChoicesToWaveEnd`），但**装备选择是精英死亡那一瞬间当场弹**（上一轮遗留）。

**修法**：`UpgradeChoiceManager.GrantEquipmentChoices` 与升级三选一**同一队列、同一时机**
（`deferChoicesToWaveEnd` 时只入队，交给波末的 `FlushPendingChoices`）。
⇒ 精英死亡 → 掉宝箱 → 拾取 → 入队 → **波末和属性卡一起弹**。

**实测**：精英死亡瞬间弹窗=False、装备待选 +3 ✓；波末依次弹出（属性 + 装备 + 装备）✓

### 10.3 问题3：精英掉落（名单、概率、形态）

**用户口径**（2026-11-17）：
- 「精英敌人才掉**宝箱**给装备饰品」；
- 「**我自己打标记配**」——名单不许代码编；
- 「**精英和 boss 都是 100**」；
- 掉落形态 = **世界里掉宝箱再拾取**。

**修法**：
1. `Enemy` 组件新增 **`dropsEquipment`（精英标记）** 字段 → 策划在**敌人预制体**上自行勾选；
   代码里**删掉了原来的敌人 ID 名单**（那份名单把 N1 的主力杂兵 1012/1013 误算成精英，
   导致“N1 开局掉了两个”）。
2. `EliteDropRules` 重写：Boss 恒掉、精英（打了标记）按概率（**当前都 = 100%**），
   判定读**敌人身上的标记**（不按 ID 查）。
3. 新增**宝箱物件链**：`EquipmentChest`（屏幕空间 UI，与经验宝石同款）
   + `EquipmentChestManager`（在 Battle.scene 的 `Manager` 对象上，`chestPrefab` → `Assets/Prefabs/EquipmentChest.prefab`，
   `playerCollectPoint` → `Player`）。
   **链路**：敌人死亡 → 生成宝箱（敌人屏幕位置）→ 飞向玩家 → 拾取 → `GrantEquipmentChoices(1)` → 波末弹。
4. 美术：`Assets/Sprites/EquipmentChest_placeholder.png`（48×48 **占位**宝箱，Point 过滤、ppu=32）+ `Assets/Prefabs/EquipmentChest.prefab`。

**实测**：运行时给 7 只敌人打标记 → 7 个宝箱生成 → 全部飞抵拾取 → 装备待选 = 7 ✓；
装备三选一标题 = “装备掉落”✓、卡面 = 饰品/影之武装 ✓。

⚠ **所有敌人预制体的标记当前都是关的**（`dropsEquipment: 0`）——**勾选权在用户**，代码不代劳。



---

## 11. ★ 换机交接 + 下一轮主线（2026-11-17 收尾）

### 11.1 本轮改动的完整清单（已提交）

| 类别 | 改动 |
|---|---|
| **属性/装备两条来源线** | `Assets/Scripts/Data/UpgradeRewardKind.cs`（新增：`UpgradeRewardKind` + `UpgradeRewardClassifier` = 分类单一真源）· `Assets/Scripts/Data/IUpgradeChoiceKindView.cs`（新增：弹窗标题的**可选**接口，不改 `IUpgradeChoiceView` 签名以免波及 `VisualLibraryCapture`）· `UpgradePoolSampler` 新增 `CollectAttributes` / `CollectAccessories` / `DrawOneByRarity` · `UpgradeChoiceManager` 经验池只收属性、装备池 = 影之武装/饰品双子池交替 · `UpgradeChoicePopup` + prefab 新增 `Title`（TMP，标题“属性强化 / 装备掉落 / 战利品”）· `TimedArrow.displayName` 箭雨→连珠箭雨（与影之武装重名） |
| **Boss 锦囊并入精英掉落** | `UpgradeChoiceManager.TriggerItemChoice` 在 V2 下直接返回（装备唯一来源 = 精英敌人） |
| **无尽关手势输入被锁死** | `FakeRouteRuntime.SuspendForEndless` 交还输入门（`GameplayInputGate.Release(this)`）· `EndlessRunController` 新增 `SetGameplayInputAllowed`：准备拦 / 战斗放 / 出口面板与旅行拦 / 退出放 |
| **敌人间距与近远透明度错误** | `EndlessRunController` 阵型改从**节点自己的手写关卡**取（`_nodeFormation`）· 新增 `fallbackFormation`（已在 Battle.scene 接 `DefaultFormation.asset`） |
| **装备选择改为波末弹** | `UpgradeChoiceManager.GrantEquipmentChoices` 尊重 `deferChoicesToWaveEnd`（与升级三选一同一队列、同一时机） |
| **精英掉落改为用户打标记** | `Enemy.dropsEquipment`（新字段，**策划在预制体上自己勾**）· `EliteDropRules` 重写（**删掉了原来的敌人 ID 名单**）· 精英/Boss 都 100% |
| **宝箱掉落链** | `Assets/Scripts/Core/EquipmentChest.cs` + `EquipmentChestManager.cs`（新增）· `Assets/Prefabs/EquipmentChest.prefab` + `Assets/Sprites/EquipmentChest_placeholder.png`（**占位美术**）· `EnemyManager` 改为生成宝箱而非直接发卡 · `BattleHUD` 注入 `chestParent` |
| **撤掉未拍板的拐杖** | `EndlessRunController.startingChoices` 3 → **0**（代码默认值 + Battle.scene） |

### 11.2 ★ 下一轮主线①：属性池 / 装备池 / 武器池 的入池内容与规则（**用户主导**）

用户 2026-11-17 原话：“看你的描述属性池混了好多跟角色属性毫无关联的内容进去，后续我会继续梳理两者的入池内容和规则。”

**实测现状**（`ActiveSkillPoolConfig.asset`，共 22 条属性侧 + 8 条影之武装）：

| | 条数 | 条目 |
|---|---|---|
| **真·角色属性**（不占槽、可叠） | **8** | 神力 `damage_plus` · 疾风 `attack_speed` · 专注 `focus` · 回气 `cooldown_reduction` · 敛财加成 `on_kill_coin`(经济) · 捡漏 `item_drop_rate` · 道具伤害 `item_damage_bonus` · 射程 `attack_range_boost` · 蓄力减伤 `charge_damage_reduction` · 经验加成 `exp_boost`（池里的 14 条 Numeric 里只有 8 条 effectType 是真正的属性） |
| **混入属性池的机制条目（占被动槽 = 设计里的“饰品”）** | **8** | 震荡 `charge_hit_shockwave` · 蛇形喷射 `timed_fire_aoe` · 冲击波 `charge_shockwave` · 回旋波 `return_wave` · 连锁弹射 `chain_bounce` · 箭矢齐射 `arrow_volley` · 虚幻武器 `phantom_weapon` · 连珠箭雨 `timed_arrow_volley` |
| **混入属性池的机制条目（category=Numeric，**没占槽 ⇒ 上一轮按“占槽口径”没能把它们挑出去**）** | **6** | 地刺 `spike_trap` · 击退波 `push_wave` · 聚拢波 `convergence_wave` · 反伤 `charge_reflect_shield` · 敛财 `on_kill_coin` · 连刺 `on_attack_stab` |
| **影之武装**（占主动槽） | **8** | 火龙舌 · 染病 · 冰雾 · 箭雨 · 抡斧劈砍 · 追命锁链 · 旋风 · 海浪 |

⇒ **本轮采用的分类口径 = “占不占槽位”**（`UpgradeRewardClassifier`）：`Numeric`=属性、`ActiveSkill`+`AttackPassive`+`TimedPassive`=装备。
⇒ **这个口径的已知缺陷（就是用户要接着梳理的）**：
1. `category = Numeric` **不等于**“角色属性” ⇒ 上面那 6 条机制条目留在属性池；
2. `敛财`（经济）、`exp_boost`（经验）算不算“角色属性”，口径待定；
3. 「**连刺** `on_attack_stab`」**没有任何执行器**（`PassiveTriggerModule` 不认 `on_attack_trigger`，也没人 `RegisterExecutor`）⇒ **抽到也不生效**，要么补执行器要么从池里拿掉。

⇒ **建议的下一步（需用户拍板后我再动手）**：
- 给“属性”一个**真正的语义**（而不是用 `category` 兼）：要么新增一个 `UpgradeRewardKind` 字段写到每个 `UpgradeDefinition` 上（Inspector 可选），要么用一套独立的属性池资产（与装备池分开两个 `ActiveSkillPoolConfig`）。
- “武器池”目前**完全不存在**（`WeaponDefinition` / 武器台页 / 武器收藏存档都未做，见 §11.4）。

### 11.3 下一轮主线②：无尽关卡的底稿与数值

- **底稿仍是混的**：N1/E0/N2/N3 走手写配置、J1/EV 走程序生成器 ⇒ 两种手感在同一圈里。
- **难度基线未定**：起始倍率（血 ×0.55 / 伤害 ×0.5 / 攻速 ×0.8）+ 每圈成长（血 ×1.6 / BOSS 血 ×1.25 / 攻速 ×1.06 / 伤害 ×1.04）。
- **开局白送卡已撤**（`startingChoices = 0`）⇒ 开局就是真难度，需重新标定。
- 已知硬缺口：**第 4 圈起没有经验三选一**（经验线 30 级 ≈ 3 圈用完）⇒ 必须补圈结算发牌（B6.12）。

### 11.4 下一轮主线③：工程待办清单（按依赖排序）

| 批次 | 内容 | 依赖 |
|---|---|---|
| **B6.12** | 圈结算固定发 N 次三选一（补第 4 圈断供） | 无（**建议先做**） |
| **B2.5 / B2.6** | 8 个主动技能配暗源耗能（当前全 0 = 不消耗）+ “+N 暗源”飘字 | 无 |
| **B1** | 卡片实例模型（`Dictionary<id,int>` → 实例列表）—— B2b 饰品 / B3 替换 UI 的共同前置，**必须单列一批** | — |
| **B2b** | 12 件饰品资产（霸王血铠/追猎令/疾影仪/破盾符/斩骑符/铁壁护腕/稳息香盒/灵光腕/反击护腕/不屈指环/回魂铃/疾风符）+ 缺失的 4 个 statId（生命上限/暴击/吸血/移速）+ 转化器资产格式（B3b） | B1、B3b |
| **B3** | 装备替换 / 跳过 UI（复用 `ItemDiscardPopup`）+ 同类上限 3 + 属性卡软上限 | B1 |
| **B4 / B5** | 武器系统（`WeaponDefinition` / 武器台页 / 武器收藏存档）、局外经济（新货币 / 洗点券 / 等级上限天赋内容） | B1 |
| **B6.5 / B6.7 / B7** | 节点结算 UI、分数→材料接线、闭环几何（回程边仍是“黑幕 + 锚点跳转”） | — |
| 修 bug | 「连刺」补执行器或下架 | — |

### 11.5 git / 换机

- 分支：**`feat/stage-select-talent-overlays`**（两台都用这一条）。本轮提交前的本地 HEAD = `f13bb4df`（已领先远程 13 个提交）。
- 引擎：**Tuanjie 2022.3.62t7**（Unity 2022.3 系）——**两台必须完全一致**，否则全量重导 + 序列化漂移。
- **B 机（这台）推**：`git pull --rebase` → `git add -A` → `git commit` → `git push`（禁止 `--force`）。
- **A 机（另一台）同步**：`git fetch origin --prune` → `git merge --ff-only origin/feat/stage-select-talent-overlays` → 回 Unity 让它重新导入。
- **不要提交**：`Locus/workspace-trees/default.json`、`threeKingdomSlayer/Directory.Build.props`（Locus 机器本地状态）；`_art_process/` 等已在本机 `.git/info/exclude`。
- 已知小坑：Unity 重写序列化后可能出现“stat 脏但 diff 为空”，`git add <file>` 即可清掉。


