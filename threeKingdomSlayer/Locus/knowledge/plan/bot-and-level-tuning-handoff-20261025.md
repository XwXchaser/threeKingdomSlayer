---
id: kd_2f62116e-0a31-4d32-88a2-4d470001ec5b
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_4b9d7e21-8c53-4a16-9d0f-2e5c6b7a1d34
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 交接：跑关机器人 + 关卡难度调参（2026-10-25）

> **给下一个会话的开场白**：读本文 → 读 `plan/first-level-difficulty-20261025-v2.xlsx`（难度映射表 + 「无聊根因与解法」页）
> → 读 `plan/level-deployment-handoff-20261025.md`（关卡部署全景与两个现场问题）→ 开工。
> 本文**自包含**：不依赖本会话的任何上下文。

---

## 0. 现在在哪（2026-10-25 收盘状态）

| 项 | 值 |
| --- | --- |
| 分支 | `feat/stage-select-talent-overlays`（已与 `origin` 同步） |
| 最近提交 | `23ac6f0`（补 .meta）/ `6208aec`（卸 Pierce·Sweep + 机器人走手势 + 无聊分析）/ `f43d912`（清除存档按钮 + 工具链 + 交接文档）/ `33beadf`（难度表）/ `a868a8d`（敌人攻击锁死修复）/ `558dd16`（教学关挂回） |
| 未提交 | `Locus/workspace-trees/default.json`（Locus 索引缓存，**不要提交**）；两篇 hub UI 文档（`hub-ui-art-style-handoff-20261025.md`、`hub-ui-v4-delivery-20261025.md`，是**你**的活）；`~$first-level-difficulty-20261025.xlsx`（Excel 锁文件，**不要提交**） |
| 难度表 | `Locus/knowledge/plan/first-level-difficulty-20261025-v2.xlsx`（7 页）。**原名 `first-level-difficulty-20261025.xlsx` 被 Excel 占用**，关掉 Excel 后可覆盖回原名 |
| 生成脚本 | `Tools/build_first_level_difficulty_table.py`（数据全部来自资产实测，可重跑；被占用时自动写 `-v2`） |
| 埋点产物 | `threeKingdomSlayer/Library/Locus/telemetry/*.jsonl`（机器本地，不入库） |

---

## 1. 跑关机器人（`BattleAutoPlayer`）

**位置**：`Assets/Scripts/Utils/BattleAutoPlayer.cs`

**用法**：挂到场景任意对象 → Play 即自动开打（或按 `P` 键手动开打）。

**它会做什么**
- 只平A点最近目标（= 操作水平的**机械下限**）；`useHeavyAttacks=true` 时混入蓄力重击（竖滑上挑 / 横滑横扫），用来对比不同操作水平的耗时。
- **遇盾兵(1021)自动改用竖滑蓄力**（盾正面出手全被格挡，只吃上挑破盾）。
- **自动点 UI**：升级三选一（`UpgradeChoiceManager.ConfirmChoice`）+ 路线选择（`RouteChoiceButton.onClick`）。
  → ⚠️ **不点 UI 的话游戏会暂停在原地**，机器人和玩家都会卡死。这是机器人第一版跑不动的原因。
- 走**手势路径**（`InputManager.SubmitGestureForDebug`），所以**连招能正常接段**（早期版本直接调 `TryExecuteAttack` → 只会打第一记 Stab）。

**关键参数**：`attackInterval`（默认 0.35s）、`useHeavyAttacks` / `heavyChance`、`recordTelemetry`。

---

## 2. 埋点（`LevelTelemetry`）

**位置**：`Assets/Scripts/Utils/LevelTelemetry.cs` → 写 `Library/Locus/telemetry/run-*.jsonl`（JSONL，一行一条）。

| 事件 | 字段 |
| --- | --- |
| `battle_begin` | label(=战斗配置名)、hp、level、upgrades、time |
| `wave_begin` | battle、wave、enemies、composition（如 `1011x2 1021x1`）、hp、level、upgrades |
| `wave_end` | wave、duration、hpLost、alive、usedHeavy、level、upgrades |
| `battle_end` | victory、duration、waves、hpLost、deaths、level、upgrades、coins |

**坑**
- 波次边界必须接 `WaveSpawner.OnWaveStarted`（机器人已接）。用「存活数变化」推断会把**一波切成好几段**（第一版就是这样）。
- 出波事件触发时敌人还没实例完 → 统计要**延后一帧**（机器人已处理）。
- `hpLost` 目前按波次近似（每波开始时快照 HP），逐次掉血要走 `PlayerState.OnHealthChanged`（未接）。

---

## 3. 分析脚本

```bash
cd threeKingdomSlayer
python Tools/analyze_level_telemetry.py                 # 摘要 + 对照目标
python Tools/analyze_level_telemetry.py --csv out.csv   # 导出逐波明细
```

输出里每个波次会标 `✔/⚠`（对照：普通波 30~55s、Boss 波 45~90s）。

**怎么读这两组数**
- **机器人 = 机械下限**（只会平A、不躲）→ 它的清场时长是该波在最低操作水平下的耗时。
- **真人试玩 = 上限**（同格式埋点）。
- 两者夹住设计目标 → 判断 `healthMultiplier` 偏高还是偏低。
- ⚠️ 机器人**不是**"平均玩家"，它不会躲、不会用招架、不会走位，所以它的耗时会**偏乐观**；它更适合当"下限探针"，别当基准线。

---

## 4. 调参工作流（建议照这个顺序）

1. **先定成长缺口**（最大的杠杆，其余数值都跟着它）：
   当前敌人经验值（1011=80/只）→ 本关只到 **Lv28**；设计目标 **Lv40/42 次三选一**，差 **61%**。
   三个补法：① **加波次**（推荐：每波仍 3~4 只、节奏约 50s 一波）② 提敌人经验值（三选一会刷屏）③ 启用 `StageConfig.killMilestones`（现在全空）按里程碑补三选一。
2. **按「无聊根因与解法」页重排波次**：
   - 「少量对怪」≠「一排多只」→ 敌人**摊在纵深**（每排 1~2 只、纵深 2~3 排）→ Slash 才划算（Stab 66 DPS vs Slash×3排=93 DPS）；
   - **一波 = 1 个「门」+ 若干杂兵**（门 = 1021 格挡 / 105 后排远程 / 109 冲锋）；
   - 105 不要再一戳就死（8 血）→ 远程压力=0 → 招架没有使用场景。
3. **落配置**：改 `Assets/RouteData/ChangbanpoScroll/Battles/Formal_N*.asset`（waves / healthMultiplier / damageMultiplier）+ `Assets/RouteData/ChangbanpoScroll/Nodes/Node_*.asset`（battleEntries）。
   - N1 的第 0 条是**开发用** `Test_Node_Battle` → 删不删待你拍板（待办 D1-4）。
   - E0 的波次顺序要跟教学台词对上：台词 wave3=破盾(1021)、wave4=骑将(109) → 1021 放第 0 波、109 放第 1 波（现状错位）。
   - N3 的倍率全是 ×0.1（占位）→ 重算。
4. **量**：机器人跑一遍拿下限，真人跑一遍拿上限，对照 30~55s / 45~90s 拧 `healthMultiplier`。
5. **重跑难度表**：`python Tools/build_first_level_difficulty_table.py`（改完配置后重生成，Excel 里的建议值要跟着资产走）。

---

## 5. 改关卡时的硬规则（别踩）

| 规则 | 来源 | 说明 |
| --- | --- | --- |
| 路面半宽 **2.5m** | `memory/scene-build-procedure.md` | 路边物件**内缘** ≥ 2.5 → 中心 |x| ≥ 2.5 + 可见半宽 |
| 硬边构件退到路肩外（+1m） | 同上 | 墙/栅栏/拒马 |
| 街道近场留空 | 同上 | 前 ~8m 只有路 |
| 远程(105)必须后排 | 数值草案铁律十 | row ≥ 波次排数/2 |
| Boss(104)必须该波最后一排 | 铁律九 | |
| 每波经验按 per-enemy 实算 | 铁律三 | Σ(数量×经验) |
| 单波清场 30~55s（Boss 波 45~90s） | 铁律八 | |
| 敌人经验：1011=80 / 1021=50 / 109=100 / 105=50 / 104=400（**实测**，草案 §1.2 的谱系表已过期） | prefab 实测 | 1011 实际 200HP/3攻（草案写 16HP/10攻） |

---

## 6. 两个现场问题（详见 `level-deployment-handoff-20261025.md` §5）

1. **箱子层级**：Y 景物用 authored z 排序、玩法物件用投影后 z 排序 → 两套坐标系混在同一个深度排序里 → 箱子错误遮挡景物。
   推导出的硬约束：战斗相机可视半宽在玩家深度只有 ±2.89，而合规路边位 |x|≥3.5 → **战斗深度上看不见**；
   要「在屏内 + 合规」必须纵深 ≥ 8 → 那是 E0 景物最密的一段 → **要你定 x/z**。
2. **关卡无聊**：根因与解法在 Excel「无聊根因与解法」页（编排问题，不是数值问题）。

---

## 7. 已修但你要知道的行为（别重复排查）

- **敌人被连续命中后不再攻击**（提交 `a868a8d` + 后续补修）：受击闪白协程每挨一下重新计时 0.4s，而玩家出招间隔 < 0.4s → 协程续命 → `UpdateAttack` 早退 → 攻击永远启动不了。
  修法：连续受击硬直预算 `Enemy.MaxContinuousStunSeconds`（默认 0.6s）+ 攻击动画看门狗 + 池回收清理协程野引用。
  → 调难度时要知道：**玩家连续平A 仍能近乎无代价地打死敌人**（这是「难度失真」的来源之一，见 Excel 第 7 页第 5 条）。
- **Pierce / Sweep 已卸下**（`Hero_Zhangfei.asset` 的 skillConfigs）→ 「蓄力后直接松手」现在没招式可出（指向突刺链的 C1→C2 仍可用）。
- **落地页有「清除存档」按钮**（`HubDebugSaveButton`，HubCanvas 左下角）→ 删档后落地页自动回到 `Tutorial_Route(9100)`。**发版前删掉这个对象。**

---

## 8. 工作纪律（沿用项目规矩）

- 改数据后跑校验；改完配置**重跑难度表脚本**。
- 本机 Editor Play Mode 帧率极低（2~5fps）：按**帧数**规划等待，别用秒数；进出 Play Mode 批处理（一次进、一次出跑完所有验证项）。
- 验证会污染存档：`SaveManager` 直写 PlayerPrefs → 验前备份、验完还原（落地页那个按钮也能一键清）。
- Unity 资产编辑：**改 prefab 前先备份、存盘后回读磁盘**（`memory/project-mistake-note.md`）。
- 每批改动完成即提交推送；`Locus/workspace-trees/default.json` 与 `~$*.xlsx` **不提交**。
- 文档与代码不一致时**以代码与资产实测为准**；本文所有数字都标了来源。
