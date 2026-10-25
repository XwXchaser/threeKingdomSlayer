---
id: kd_307bcc17-5c3e-4bf9-ae45-0c41b7f6b1e8
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_c1529477-f63e-42db-87cf-3265d7b3d195
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 跑关机器人 + 第一关调参 · 第二轮（2026-10-25 续）

> 接 `plan/bot-and-level-tuning-handoff-20261025.md`（第一轮交接）。
> 本轮做完了：**机器人重做 + 第一关 22 波落配置 + 实测标定**。
> 配套：`Locus/knowledge/plan/first-level-difficulty-20261025-v2.xlsx`（7 页，含「已落hp× / 实测清场」两列）。

---

## 0. 一句话结论

机器人从「只能打一个节点」的半成品改成**能跑完整条路线、能当测量基准**的探针；
第一关 22 波全部落进资产并按**实测**标定。
**血量刻度最终按「设计意图」定：杂兵 1011 ≈ 3 段 jab 打死**（有效血 100~124）。

---

## 1. 机器人改造（`Assets/Scripts/Utils/BattleAutoPlayer.cs`）

### 1.1 修掉的缺陷（第一轮交接写的是「已做」，代码里其实没做）

| 缺陷 | 现象 | 修法 |
| --- | --- | --- |
| 没走手势路径 | 交接说「走 `SubmitGestureForDebug` 连招能接段」，**代码里其实还是直接 `TryExecuteAttack`**（`_input` 拿到却没用） | 改成 `InputManager.SubmitGestureForDebug`（Tap→戳击/连招、竖滑→上挑、横滑→斩击） |
| 重击打空 | 旧代码 `Random ? AttackType.Sweep : AttackType.Launch`，但 **Sweep 已随 Pierce 一起卸下** → 一半概率空招 | 重击改横划（斩击 20 伤）；上挑只留给破盾 |
| 盾兵卡死 | 破盾后仍用上挑（**上挑只有 2 伤**）→ E0 第一波实测 **162 秒** | 只在 `ShieldGuardEnemy.IsShieldActive` 时用上挑；破盾进胆怯（受伤×1.5）后改平A → 162s → 23s |
| 战斗一结束就死 | 节点间旅行会把关卡状态切走，机器人随即退出 | 战斗边界改由 `WaveSpawner` 事件驱动，机器人**跨节点续跑** |
| 胜负恒 false | `battle_end` 永远 `victory=false, coins=0` | 胜负取真实结果，铜钱取 `PlayerState.coinCount` |
| 逐波掉血是累计值 | `hpLost` 打的是整场累计 | 波开始快照 HP、波结束相减；并接 `PlayerState.OnHealthChanged` |
| 波次重复/缺失 | 换波那一帧 `WaveSpawner` 会**多发一次** `OnWaveCompleted`（带的是新波号） | 按波号过滤 + 「敌人确实死光」兜底 |
| 死字段 | `damageThreshold` 从没被读过 | 删掉 |

### 1.2 新增能力

- **`bossQTEMode`**：`Suppress`（默认，测量用）/ `Answer`（机器人代答 QTE）→ 配套 `QTEController.TryAnswerQTEForDebug()`。
- **`preferredRouteChoiceId`**：强制走某条支路（走运行时 `TrySelectChoice`，比点按钮可靠）。
- **`useAreaAttacks`**：前排 ≥2 只时改用横划（对比「会用范围技」的操作水平）。
- 敌人识别改用 `Enemy.enemyId`（旧代码靠对象名 `Contains("1021")`）。

### 1.3 埋点口径（`Assets/Scripts/Utils/LevelTelemetry.cs`）

- 一波 = 一个 `StageConfig` 里的一条 `WaveConfig`。
- `wave_end`：`duration`（本波用时）/ `hpLost`（**本波**掉血）/ `kills` / `usedHeavy`。
- `battle_end`：`victory` / `duration` / `waves` / `kills` / `hpLost`（整场）/ `deaths` / `coins`。
- ⚠️ **每 flush 一次新建一个文件** → `Tools/analyze_level_telemetry.py` 默认改成「读全部文件」（`--latest` 才只读最新）。

### 1.4 怎么用

```csharp
var go = new GameObject("BattleAutoPlayer(测量)");
var bot = go.AddComponent<BattleAutoPlayer>();
bot.autoStart = true; bot.useAreaAttacks = false;
bot.bossQTEMode = BattleAutoPlayer.BossQTEMode.Suppress;
bot.preferredRouteChoiceId = "";        // "" = 默认第一个出口
var route = AssetDatabase.LoadAssetAtPath<FakeRouteStageConfig>(
    "Assets/RouteData/ChangbanpoScroll/Changbanpo_ScrollRoute.asset");
FakeRouteRuntime.Instance.Begin(route);  // 直接开跑，不用点落地页
```

---

## 2. 资产改动（最终状态）

| 资产 | 改动 |
| --- | --- |
| `Formal_N1_Battle` | 1 波 → **5 波**（row0 两只 → row0 三只（范围技教学）→ 摊纵深 3 排），hp× **0.50/0.50/0.52/0.52/0.54** |
| `Formal_E0_Battle` | 3 波 → **5 波**（**1021 第0波、109 第1波**，对齐教学台词），hp× **0.55/0.55/0.56/0.57/0.58** |
| `Formal_N2_Battle` | 2 波 → **6 波**（105 后排 + 盾兵掩护 + 104 Boss），hp× 0.58~0.60，**Boss ×7.5（暂不开放，见 §4.1）** |
| `Formal_N3_Battle` | 1 波 10 排 ×0.1 占位 → **6 波**，hp× **0.60~0.62**，终局 104 **×9（4500）** |
| `Enemy_105.prefab` | 原始血 **8 → 40 → 200**（与 1011 同档：≈3 段 jab，不会被一戳秒） |
| `Node_N1` | 删掉第 0 条开发用 `Test_Node_Battle` |
| `Node_J1` | 曾接入右路 `j1_to_n2`，**用户决定暂不开放 → 已撤掉**（路线仍是单线 N1→E0→J1→EV→N3） |
| `Node_N2/N3` | 显示名去掉「（流程预览）」 |
| 三个新脚本的 `.meta` | GUID 非法（非 32 位十六进制）→ 改成 Unity 实际用的合法 GUID（否则换机克隆后 `HubDebugSaveButton` 场景引用会断） |

**编成规则落地**：每排 ≤2 只（N1 第2波「前排 3 只」是范围技教学，刻意保留）、
每波 1 个「门」（1021 格挡 / 105 后排远程 / 109 冲锋）、105 固定后排、Boss 在该波最后一排。

### 2.1 血量刻度：为什么是 0.5~0.62（而不是第一轮那个夸张值）

玩家的实际输出（实测 `Zhangfei_Jab1/2/3`）：**30 / 33 / 37 伤害，一段 Tap 连招 = 100 伤害**。
所以「杂兵 3 段 jab 打死」= **有效血 ≈ 100**；1011 原始血 200 → **hp× ≈ 0.5**。

| | 第一轮（按「每波 30~55s」反推） | 现在（按设计意图） |
| --- | --- | --- |
| N1 首波 1011 有效血 | 200 × 3.6 = **720**（≈22 段 jab） | 200 × 0.50 = **100**（≈3.3 段 jab） |
| 改之前的历史值 | 200 × 0.7 = 140（≈4.7 段 jab） | — |
| 单波清场 | 25~40s | **4~11s** |

**教训**：第一轮我把难度表里「单波 30~55s」当成硬指标去反推 hp×，
而那个指标本身来自拍脑袋的 DPS 模型 → 把杂兵血量推成了 22 段 jab。
**应以「几段 jab 打死」这种操作直觉为准，波长是结果不是目标。**

---

## 3. 实测标定（机器人平A 基线）

新刻度下的一次完整跑（左路）：

| 节点 | 波 | 实测清场(s) | 本波掉血 |
| --- | --- | --- | --- |
| N1 | 0~4 | 3.8 / 5.8 / 6.5 / 9.4 / 9.1 | 1.35（全节点） |
| E0 | 0~4 | 7.6 / 9.7 / 7.3 / 6.8 / 11.0 | 0 |
| N3 | 0~4 | 7.7 / 7.2 / 11.8 / **21.0** / 11.7 | 1.95 / 3.25 / 6.5 / **79.95** / 2.1 |
| N3 | 5（Boss） | 机器人**阵亡**（进 Boss 波时只剩 4.9 血） | — |

- 整关战斗时间：N1 34.6s + E0 42.5s + N3 ≈ 160s ≈ **2.6 分钟**（旧刻度是 11~13 分钟）。
- **难度墙回到 Boss**：机器人现在能一路打到 Boss 波才死 ✔（旧刻度死在 N2/N3 第4波）。
- **N3 第4波（109 骑兵 + 3 弓兵 + 2 剑兵）是伤害尖峰**：机器人一轮吃了 80 血（死一次）。
  主要是**骑兵**（10 伤 / 2s = 5 伤/秒，骑乘免控 → 机器人没法打断）。
  → 真人会招架的话没问题；不会招架/不躲的玩家会在这里死。

---

## 4. 本轮的新发现（要人拍板或要知道）

1. **N2 暂不开放**（用户 2026-10-25 决定）→ `Node_J1` 的右路出口已撤掉，
   路线回到单线 N1→E0→J1→EV→N3；N2 的配置保留但不可达（倍率已跟新刻度对齐）。
2. **正式路线原本就是单线的**：`J1` 只有 `j1_to_ev` 一条出口，**N2 挂在 nodes 里但没有任何入边** → 右路和它的 Boss 从来没被玩家看到过。
3. **难度墙卡在「弓兵波」而不是 Boss**（旧刻度）：机器人（不会招架）在 N2/N3 第4波被打死；
   玩家 100 血、**资产实测 `Hero_Zhangfei.reviveCount=0`**（文档写的 3 是错的）→ 不会招架的玩家第一关过不去。
   新刻度下机器人能打到 Boss 才死 ✔。
4. **Slash 的射程是 1 排**（`Zhangfei_Slash.rangeRows=1`），不是交接里写的「一次 3 排」——
   要跨排得吃 `attack_range_boost` 升级（+1 排/级，伤害 -30%/-25%，全库只有这一条）。
   → 「把敌人摊在纵深好让斩击划算」这条根因解法**前提不成立**。
5. **旧 DPS 模型是错的**：表里假设 `33 + 1.9×等级`，实测机器人平A的有效 DPS 只有 **57~70** 且几乎不随等级线性涨。
6. 三个新脚本的 `.meta` GUID 非法（第一轮提交进去的），换机克隆会让 `HubDebugSaveButton` 的场景引用断掉。

---

## 5. 未决 / 下一步

| # | 事项 | 状态 / 建议 |
| --- | --- | --- |
| 1 | **单波时长口径** | 旧铁律八「普通波 30~55s」已作废 → 改成「杂兵波 4~12s，Boss 波 45~90s」。已在难度表第 5 页改口径 |
| 2 | **成长缺口（Lv28 → Lv40）** | 新刻度下整关战斗只有 ~2.6 分钟 → 「一次循环 9~12min」和「42 次三选一」都靠**波次数**撑起来 → **方案 A（加波次）**从"可选"变成"必需"（建议 ~32 波，每波 3~5 只） |
| 3 | **N3 第4波的伤害尖峰** | 骑兵 5 伤/秒 → 机器人一轮掉 80 血。要么把 109 的攻击降到 6~7，要么给玩家 1~2 次复活，要么这波减到 2 弓兵 |
| 4 | N3 w3~w5 / N2 全波的倍率 | 只有 N3 w0~w4 实测到；N2 因未开放没测。倍率目前是按设计意图给的，等真人与开放后再拧 |
| 5 | 105 的 200 血是否够 | 现在 ≈3 段 jab，远程威胁成立（波次里它挨打期间一直在射） |

## 6. 纪律

- 改完资产要**回读磁盘**；改 prefab 前先备份（`Library/Locus/tmp/level-tuning-20261025/`）。
- 测量会污染存档（`SaveManager` 直写 PlayerPrefs）→ 验前备份、验完还原（已还原）。
- 本机 Editor Play Mode 实测 ~60fps，**一次跑完一条路线约 3~6 分钟**（别按秒数等，看埋点文件）。
- 埋点在 `Library/Locus/telemetry/*.jsonl`（机器本地，不入库）；分析用 `python Tools/analyze_level_telemetry.py`。
