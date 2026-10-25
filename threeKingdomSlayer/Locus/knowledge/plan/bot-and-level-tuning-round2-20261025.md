---
id: kd_c1529477-f63e-42db-87cf-3265d7b3d195
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_7b3c9d21-4f6a-4e58-9c17-2b8f5a6d30e4
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 跑关机器人 + 第一关调参 · 第二轮（2026-10-25 续）

> 接 `plan/bot-and-level-tuning-handoff-20261025.md`（第一轮交接）。
> 本轮做完了：**机器人重做 + 第一关波次/数值落配置 + 四次实跑标定**。
> 配套：`Locus/knowledge/plan/first-level-difficulty-20261025-v2.xlsx`（7 页，已含实测列）。

---

## 0. 一句话结论

机器人从「只能打一个节点」的半成品改成**能跑完整条路线、能当测量基准**的探针；
第一关 22 波全部落进资产并按**实测**标定（不再用拍脑袋的 DPS 模型）；
量出来的结论是：**难度墙确实存在，但卡点在「弓兵波」而不是 Boss**。

---

## 1. 机器人改造（`Assets/Scripts/Utils/BattleAutoPlayer.cs`）

### 1.1 修掉的缺陷（第一轮交接里写的是「已做」，但代码里其实没做）

| 缺陷 | 现象 | 修法 |
| --- | --- | --- |
| 没走手势路径 | 交接说「走 `SubmitGestureForDebug` 连招能接段」，**代码里其实还是直接 `TryExecuteAttack`**（`_input` 字段拿到却没用） | 改成 `InputManager.SubmitGestureForDebug`（Tap→戳击/连招、SwipeVertical→上挑、SwipeHorizontal→斩击） |
| 重击分支打空 | 旧代码 `Random ? AttackType.Sweep : AttackType.Launch`，但 **Sweep 已随 Pierce 一起从张飞配置卸下** → 一半概率空招 | 重击改用横划（斩击，20 伤）；上挑只留给破盾 |
| 盾兵卡死 | 破盾后仍用上挑（**上挑只有 2 伤**）→ E0 第一波实测 **162 秒** | 只在 `ShieldGuardEnemy.IsShieldActive`（还在举盾）时用上挑；破盾进胆怯（受伤×1.5）后改平A → 162s → 23s |
| 战斗一结束就死 | 节点间旅行会把关卡状态切走，机器人随即退出，后面节点全打不了 | 战斗边界改由 `WaveSpawner` 事件驱动，机器人**跨节点续跑**直到整关结束或按 P |
| 胜负恒 false | `battle_end` 永远 `victory=false, coins=0` | 胜负取真实结果（全部波次清空=胜、玩家阵亡=败），铜钱取 `PlayerState.coinCount` |
| 逐波掉血是累计值 | `hpLost` 打的是「整场累计」而不是「本波」 | 波开始时快照 HP，波结束相减；并接上 `PlayerState.OnHealthChanged` → 整场掉血/死亡次数 |
| 波次重复/缺失 | 换波那一帧 `WaveSpawner` 会**多发一次** `OnWaveCompleted`（带的是新波号）→ 上一波被记两遍、本波提前结算 | 按波号过滤 `waveIndex == _waveIndex`，并加「敌人确实死光」的兜底结算 |
| 死字段 | `damageThreshold` 从没被读过；`waveGap` 没被用过 | 删掉死字段，`waveGap` 改为波间空窗 |

### 1.2 新增能力

- **`bossQTEMode`**：`Suppress`（默认，测量用：把 BOSS 的 QTE 数据清空，量到干净 DPS）/ `Answer`（机器人按 QTE 配置自动作答）。
  → 配套给 `QTEController` 加了 `TryAnswerQTEForDebug()`（点型点指示器中心、划动型按配置方向划过指示器）。
- **`preferredRouteChoiceId`**：强制走某条支路（`j1_to_n2` / `j1_to_ev`），用运行时的 `TrySelectChoice`（比点按钮可靠）。
- **`useAreaAttacks`**：前排 ≥2 只时改用横划（斩击打整排）——用来对比「会用范围技」的操作水平。
- 敌人识别改用 `Enemy.enemyId`（旧代码靠对象名 `Contains("1021")`）。

### 1.3 埋点口径（`Assets/Scripts/Utils/LevelTelemetry.cs`）

- 一波 = 一个 `StageConfig` 里的一个 `WaveConfig`（按出波事件切）。
- `wave_end`：`duration`（本波用时）/ `hpLost`（**本波**掉血）/ `kills`（本波击杀）/ `usedHeavy` / `level` / `upgrades`。
- `battle_end`：`victory` / `duration` / `waves` / `kills` / `hpLost`（整场）/ `deaths` / `coins`。
- ⚠️ **每 flush 一次就新建一个文件**（`run-<时间戳>.jsonl`），一场战斗会散在多个文件里 →
  `Tools/analyze_level_telemetry.py` 的默认行为已改成「读全部文件」（`--latest` 才只读最新的）。

### 1.4 怎么用

```csharp
var go = new GameObject("BattleAutoPlayer(测量)");
var bot = go.AddComponent<BattleAutoPlayer>();
bot.autoStart = true;                 // 平A 基线
bot.useAreaAttacks = false;           // 要量「会用范围技」就 true
bot.bossQTEMode = BattleAutoPlayer.BossQTEMode.Suppress;
bot.preferredRouteChoiceId = "j1_to_n2";
bot.recordTelemetry = true;

var route = AssetDatabase.LoadAssetAtPath<FakeRouteStageConfig>(
    "Assets/RouteData/ChangbanpoScroll/Changbanpo_ScrollRoute.asset");
FakeRouteRuntime.Instance.Begin(route);   // 直接开跑，不用点落地页
```

---

## 2. 资产改动

| 资产 | 改动 |
| --- | --- |
| `Formal_N1_Battle` | 1 波 → **5 波**（row0 两只 → row0 三只（范围技教学）→ 摊纵深 3 排），hp× 3.6/2.7/3.0/1.9/2.0 |
| `Formal_E0_Battle` | 3 波 → **5 波**（**1021 第0波、109 第1波**，与教学台词顺序对上），hp× 3.4/2.4/3.2/3.7/2.8 |
| `Formal_N2_Battle` | 2 波 → **6 波**（105 后排远程 + 盾兵掩护 + 104 Boss ×7.5 在最后一排），hp× 2.6/4.1/3.2/3.1/2.1/1.4 |
| `Formal_N3_Battle` | 1 波 10 排 ×0.1 占位 → **6 波**（倍率重算），终局 104 ×11.5，hp× 3.3/5.0/3.4/2.7/4.0/1.0 |
| `Enemy_105.prefab` | 血量 **8 → 40**（远程威胁才成立） |
| `Node_N1` | 删掉第 0 条开发用 `Test_Node_Battle` |
| `Node_J1` | **接入右路出口 `j1_to_n2`**（此前 N2 无入边 → 右路和它的 Boss 完全不可达） |
| `Node_N2/N3` | 显示名去掉「（流程预览）」 |
| 三个新脚本的 `.meta` | GUID 非法（非 32 位十六进制）→ 改成 Unity 实际用的合法 GUID（否则换机克隆后 `HubDebugSaveButton` 的场景引用会断） |

**编成规则落地**：每排 ≤2 只（N1 第2波「前排 3 只」是范围技教学，刻意保留）、
每波 1 个「门」（1021 格挡 / 105 后排远程 / 109 冲锋）、105 固定后排、Boss 在该波最后一排。

---

## 3. 实测标定（四次跑，平A 基线）

| 节点 | 波 | 实测清场(s) | 目标 | 结论 |
| --- | --- | --- | --- | --- |
| N1 | 0~4 | 25.4 / 30.3 / 34.8 / 30.1 / 36.4 | 26/28/30/32/34 | ✔ 达标 |
| E0 | 0~4 | 32.0 / 38.9 / 32.7 / 34.2 / 37.5 | 30/34/32/34/36 | ✔ 达标 |
| N2 | 0~2 | 36.1 / 34.8 / 34.2 | 32/34/36 | ✔ 达标（w3+ 机器人被打死，倍率按同节点 DPS 外推） |
| N3 | 0~2 | 37.8 / 39.1 / 40.4 | 34/36/38 | ✔ 达标（w3+ 同上） |

标定公式：**hp×_新 = hp×_旧 × (目标秒数 ÷ 实测秒数)**，跑了 3~4 次取平均
（机器人随机选三选一 → 同波不同次有 **±30%** 浮动）。

**旧 DPS 模型是错的**：表里假设 `33 + 1.9×等级`（Lv1 34.9 → Lv28 86.2），
实测机器人平A的**有效 DPS 只有 50~72 且几乎不随等级线性涨** → 表格脚本已换成实测拟合的 `58 + 0.4×等级`。

---

## 4. 本轮的新发现（都要人拍板或知道）

1. **正式路线原本是单线的**：`J1` 只有一条出口 `j1_to_ev` → `EV` → `N3`，**N2 完全不可达**
   （它挂在 `nodes` 里但没有任何入边）。已把 `j1_to_n2` 接进 `Node_J1`，路线校验通过。
2. **难度墙卡在「弓兵波」而不是 Boss**：机器人（不会招架）在 **N2 第4波 / N3 第4波**就被打死
   （弓兵一波打掉 19~28 血）。玩家 100 血、**0 次复活**（资产实测 `Hero_Zhangfei.reviveCount=0`，
   文档写的 3 是错的）→ **不会招架的玩家第一关过不去**。
   设计上这是「远程压力 → 逼你招架/绕后」的门，但要真人试玩确认这堵墙是「合理」还是「太早」。
3. **Slash 的射程是 1 排**（`Zhangfei_Slash.rangeRows=1`），不是交接里写的「一次 3 排」——
   要跨排得吃 `attack_range_boost` 升级（+1 排/级，伤害 -30%/-25%，全库只有这一条）。
   → 「把敌人摊在纵深好让斩击划算」这条根因解法**前提不成立**，已写回难度表第 7 页。
4. **`reviveCount=0`**、玩家 100 血：难度墙的「100 血 + 3 次复活 ≈ 能撑 30~40s」那段结论要重写。
5. 三个新脚本的 `.meta` GUID 非法（第一轮提交进去的），换机克隆会让 `HubDebugSaveButton` 的场景引用断掉。

---

## 5. 未决 / 下一步

| # | 事项 | 状态 |
| --- | --- | --- |
| 1 | **成长缺口（Lv28 → Lv40）** | 用户 2026-10-25 定：先按方案 A 落表、量完再定。实测已给出现状：**22 波清场合计约 11~13 分钟**（加旅行 ≈ 14~16 分钟），方案 A 的 ~35 波会到 22 分钟以上 → 建议改用 **B（提敌人经验）** 或 **C（里程碑补三选一）** |
| 2 | 弓兵波的伤害预算 | 机器人一波掉 19~28 血；要么把 105 的攻击/频率降一点，要么给玩家 1~2 次复活，要么把弓兵从 3 只减到 2 只 |
| 3 | N2 w3/w4/w5、N3 w3/w4/w5 的倍率 | 没实测到（机器人中途阵亡）→ 现在是外推值，真人试玩后再拧 |
| 4 | 105 的 40 血是否够 | 一波弓兵波要打 35~40s；再高会让「绕后/招架」之外没有解 |
| 5 | 波次表里的「每排 ≤2 只」与「范围技教学」 | N1 第2波保留前排 3 只，其余每排 ≤2 |

## 6. 纪律

- 改完资产要**回读磁盘**；改 prefab 前先备份（`Library/Locus/tmp/level-tuning-20261025/`）。
- 测量会污染存档（`SaveManager` 直写 PlayerPrefs）→ 验前备份、验完还原（已还原）。
- 本机 Editor Play Mode 实测 ~60fps，**一次跑完一条路线约 6~9 分钟**（不要按秒数等，按帧/文件看）。
- 埋点在 `Library/Locus/telemetry/*.jsonl`（机器本地，不入库）；分析用 `python Tools/analyze_level_telemetry.py`。
