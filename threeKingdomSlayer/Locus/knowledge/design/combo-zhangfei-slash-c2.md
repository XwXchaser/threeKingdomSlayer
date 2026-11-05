---
id: kd_539b6efa-f826-49bc-9d02-6fcfec986a5d
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_combo_zhangfei_slash_c2
injectMode: inherit
aiEditMode: inherit
---

# 连招文档 · 张飞·斩击 C2 链（SlashC2）

> 需求（2026-11-03）：「slash 移除左右位移效果，新增 slashC2，任意方向 slash 后蓄力松开发动，
> 释放一次与上次 slash 的强化 slash，带有位移效果，slashC2 需要解锁且需要设计蓄力动作和招式动作。」
>
> 本文按 `design/combo-registry.md` §3 的 checklist 写。**实现已落地并实机验证**；蓄力/招式动作表现待美术。

---

## 1. 输入模型

| 段 | 输入 | 说明 |
|---|---|---|
| ① | **任意方向斩击**（横滑 / 斜滑，未蓄力） | 横滑与斜滑都落到 Slash ✓（沿用现有默认映射：斜滑无论蓄力都是 Slash ✓） |
| ② | **按住蓄力**（长按不动，蓄力等级 ≥1） | 连段蓄力：枪体停住等蓄力 ✓（复用现有 `HasChargeContinuation` → `BeginComboChargeHold` 机制 ✓） |
| ③ | **松手** | 在 Slash 的接续窗口内（55%~100%）松手 → 释放 **SlashC2** |

**方向继承**：SlashC2 的左右方向 = **上一段 Slash 的方向**（蓄力松手本身没有方向 → 由状态机 `_entrySlashLeftToRight` 传递 ✓）。

## 2. 节点与边

```
MoveTable_Zhangfei.roots:
    Tap             (c0) → Zhangfei_Jab1
    SwipeHorizontal (c0) → Zhangfei_Slash          ← 未蓄力的横滑
    SwipeHorizontal (c1) → Zhangfei_Sweep         ← 蓄力的横滑（仍是横扫，不能被 Slash 抢走）
    SwipeDiagonal   (c0) → Zhangfei_Slash          ← 斜滑

Zhangfei_Slash.moveEdges:
    Hold (minChargeLevel=1) → Zhangfei_SlashC2   (overrideWindow, 窗口 0.55~1)
```

| 节点 | id | attackType | 伤害 | 架势 | 重攻击 | 覆盖 | 左右位移 | 窗口 |
|---|---|---|---|---|---|---|---|---|
| `Zhangfei_Slash` | 2 | Slash | 20 | 0 | 否 | 全列 1 排 | **0（已移除）** | 0.55–1（override） |
| `Zhangfei_SlashC2` | **30**（划线链十位段） | Slash | **35** | **30** | **是** | 全列 **2** 排 | **1 排** | 0.55–1 |

> 数值是**初值提议**（对齐 StabR2 的比例：伤害 20→35、架势 0→30、isHeavy、覆盖 +1 排）→ **全在资产里，随时可调** ✓。

## 3. 落点与位移

- **落点**：全列（N 列都打）✓，比母招多 1 排 ✓。
- **左右位移**：命中后把命中的敌人**向左/右推 1 排**（走既有 `ColumnManager.ApplyDirectionalPush` 通道 ✓，
  只改列号、不进击退回填 ✓）→ **普通 Slash 配 0 = 不再位移**（原实现其实一直是 debug 专用，正常游戏从未生效 ✓）。
- **打击反应方向**：`hitReactionDirection` 跟随斩击方向 ✓（母招同源 ✓）。

## 4. 表现（含与已有链的区分）

> **2026-11-04 更新**：已把「带位移」与「更重的打击反馈」两条**落地为代码 + 配置**（见下方 ✅ 两行）。
> 剩下的都是**素材侧**的事。

| 段 | 现有表现 | 状态 / 待补 |
|---|---|---|
| ① Slash | `SweepEffect` 扫掠（3D 枪模型 `FantasySpear3D` + 2 张旋转拖尾）✓ | — |
| ② 蓄力 | 跟手蓄力指示（`ChargeStabVisual`，**突刺风格**）⚠；**斩击后没有「枪体停住」**（那是 Stab 专属，`BeginComboChargeHold` 依赖 `_lastStabEffect`）⚠ | **SlashC2 的蓄力姿态**（收枪 + 蓄力弧）→ 美术，清单见 `plan/slash-c2-art-request-20261103.md`；「按招式切蓄力样式」的代码钩子一并等素材做（钩子先做没有素材可切） |
| ③ SlashC2 | ✅ **打击反馈 = Heavy**（顿帧 0.09→**0.14s** + 镜头震 heavy 档 0.11s/0.035 + 像素火花随 context）；拖尾仍复用 Slash 的两张全局贴图 ⚠ | **强化斩击的招式动作**（更宽更亮的拖尾）→ 美术，清单同上；填进 `Zhangfei_SlashC2.slashRotateSprite1/2` 即可，**不用改场景** ✓ |
| 位移 | ✅ **已带位移**：`Zhangfei_SlashC2.slashDirectionalPushRows = 1`（横向推开 1 排，走既有 `ColumnManager.ApplyDirectionalPush` 通道，只改列号不进击退回填）；普通 Slash 保持 0 | 敌人被推开时的**撞击反馈**（可后置） |

### 4.1 「更重的打击反馈」怎么实现的（2026-11-04）

| 通道 | 做法 | 参数 |
|---|---|---|
| 顿帧 | `HitFeedbackStrength.Standard → Heavy` | `GetHitStopDuration`：Standard **0.09s** → Heavy **0.14s** |
| 镜头震 | 同上（`CameraFeedbackController.heavyOnlyCameraShake = true` → **只有 Heavy 才震镜头**；普通 Slash 的 Standard 档不震，行为不变 ✓） | heavy：0.11s / 强度 0.035 |
| 像素火花 | `PixelHitEffectManager.RequestHit(context)` 随 context 走 | 自动跟随强度 |
| 音效 | Heavy → 播 `Slash_HeavyHit` 事件 → `AudioManager.PlayHeavyHit()`：**专用数组 `_heavyHitClips` 为空时回退到普通斩击音** → **素材没交时行为与现状完全一致（不静音、不回归）**；美术的「重击音」交来后直接填数组即生效，不用改代码 | `HEAVY_HIT_VOLUME = 1.35`（普通斩击 1.25） |

**代码改动（4 个文件）**

| 文件 | 改什么 |
|---|---|
| `Assets/Scripts/Data/AttackSkillConfig.cs` | 新增 `slashFeedbackStrength`（默认 `Standard` → **普通 Slash 零回归**） |
| `Assets/Scripts/Attack/SweepEffect.cs` | `Create` 加 `feedbackStrengthOverride` 参数；新增 `ResolveHitFeedbackStrength(isFirstHit)`：**只有首击吃满档**，后续命中降为 Light（避免连续命中全是重反馈） |
| `Assets/Scripts/Player/Combat/AttackSystem.Types.cs` | `ExecuteSlash` 传 `cfg.slashFeedbackStrength`；Heavy 时播 `Slash_HeavyHit` |
| `Assets/Scripts/Services/AudioManager.cs` | 新增 `_heavyHitClips` + `PlayHeavyHit()`（空数组回退普通斩击音）+ 事件 `Slash_HeavyHit` |

**配置改动（1 个资产）**：`Assets/ScriptableObjects/Moves/Zhangfei/Zhangfei_SlashC2.asset` → `slashFeedbackStrength: Standard → Heavy`（`slashDirectionalPushRows` 本来就是 1）

> 拖尾贴图已改成 **per-move**：`AttackSkillConfig.slashRotateSprite1/2`（2026-11-03 加，留空回退场景全局贴图）
> → SlashC2 的拖尾可以直接挂进资产，不用改场景 ✓。

> 前端侧：天赋页**不需要改代码**就能显示新节点（面板按 `moveConfig` 自动取图标与文案 ✓）；
> 面板按 `groupId` 画泳道（`groupId` 已填 `sweep` ✓）。

### 4.2 「解锁后首次进关」的引导（**与关卡节点无关**，2026-11-02）

> **口径**：玩家**首次解锁 slash 后、第一次进入战斗时**，弹一次「手势示范 + 一行文字」。
> 依据 `MoveGuideService.cs` 自己的规则注释（「解锁后第一次进关时，在波次开始前弹一次」）。
> → **教学关不需要 Slash 教学**。

**当前实现状态（活体实测）**

| 环节 | 状态 |
|---|---|
| 机制 | `MoveGuideService`（收集）+ `MoveGuidePresenter`（播）+ `TalentNodeConfig.moveGuideGesture/moveGuideDialogue`（数据位）+ `MoveGuideSaveSection`（存档）—— 全套都在 |
| 数据 | `talent_move_slash` = `SwipeHorizontal` + `guide_move_slash`（台词「手指横着划过去 —— 横扫一排敌人！」）—— 已配 |
| 触发 | `FakeRouteRuntime.Node.cs:118`（路线关卡，战斗开打前）+ `StageRunFlow.cs:66`（直连关卡） |
| **实际表现** | **黑屏 + 暂停 + 敌人没到位 + 没有教学的演出**（坏的） |

**根因（活体组件证据）**

```
MoveGuidePresenter: IsShowing=True  CompletedNormally=False  gestureHint.IsShowing=False
Dialogue: IsBusy=True      Time.timeScale=0
```

| 现象 | 根因 |
|---|---|
| 敌人还没到位 | `ShowPending()` 在**波次开始前** → 战斗还没开始（设计，不是 bug） |
| 黑幕 + 暂停 | 提示层 `ShowDim(0.5)` **叠加**对话层压暗；对话是「点击继续」式 → `IsBusy=true` → `timeScale=0` |
| **没有演出** | `MoveGuidePresenter` 用 `PlaySwipe()`（**播一次**）→ 1.1s 后 `Finish()` → `HideLayers()` **自己藏了**；而它等 `minGestureSeconds=1.2s` → 示范在玩家读台词期间就消失 |

**核心**：`MoveGuidePresenter` 用了「**演示一次**」（`Play*`），而教学用的是「**门槛循环演示**」（`TutorialGestureHint.Gate`）→ **应该复用 `Gate`，不自己播一次**。

**修法**：见 `plan/handoff-growth-slash-guide-20261102.md` §5-T2。

## 5. 解锁

- 节点：`TalentTree_Moves` → `talent_move_slashc2`（`moveConfig = Zhangfei_SlashC2`，`coinCost` 沿用占位 1）。
- 解锁判定：**零代码**（`PlayerMoveStateMachine.IsMoveLocked` 读 `hero.talentTree` = Moves 页 ✓）。
- 未解锁时：蓄力松手**不响应**（不回落成别的招式 ✓）。

## 6. 验收（2026-11-03 实机已过）

| # | 检查 | 结果 |
|---|---|---|
| 1 | 任意方向 Slash → 蓄力松手 → 打出 SlashC2 | ✔ 实机：`执行 Zhangfei_SlashC2` |
| 2 | SlashC2 伤害 = 35 | ✔ 实机：目标 HP 545 → 510（差 35 ✔） |
| 3 | 带左右位移、方向继承上一段 | ✔ 实机：列 3 → 2 → 1（向左，与上一段右→左一致 ✔） |
| 4 | 未解锁时打不出来 | ✔ 实机：`招式未解锁 Zhangfei_SlashC2（不响应）` |
| 5 | 普通 Slash 不再位移 | ✔ 实机：列 1 → 1 |
| 6 | 「横滑+蓄力 → Sweep」不被抢走 | ✔ 实机：`执行 Zhangfei_Sweep` —— ⚠️ **2026-11-05 已停用**，见 §8 |
| 7 | 校验器 | ✔ 天赋树 4 棵零问题 |

## 7. 已知副作用与待实机项

| # | 项 | 说明 |
|---|---|---|
| 1 | Sweep 期间再划一下 | 现在会**缓冲**并在窗口内重复 Sweep（之前是直接丢弃）→ Sweep 冷却 2s 会挡住 ✓ 实际无感 |
| 2 | 教学关 | 教学期 `restrictChargedToVertical` 会拦 Hold → `comboChargeActive` 放行 ✓（与 C2 同机制 ✓）；若要教 SlashC2，需对方补一段教学 |
| 3 | SlashC2 的数值 | 初值提议（伤害/架势/覆盖/位移排数）→ **需要你或数值同学定稿** |
| 4 | 美术 | 蓄力动作 + 招式动作 + 位移反馈 + 天赋页图标 → **清单已出**：`plan/slash-c2-art-request-20261103.md`（尺寸/命名/导入设置/挂点/验收口径） |

## 8. 2026-11-05：纯蓄力横扫停用 + 同类错误关联审计

> 用户反馈：「无前置的纯蓄力又回来了」。根因在**中立态入口**，不在代码。

### 8.1 根因

`MoveTable_Zhangfei.asset` 的 **中立态入口 roots** 里有一条：
`gesture=SwipeHorizontal(3) + minChargeLevel=1 → Zhangfei_Sweep`
→ `FindRoot` 按「蓄力要求最高且已满足」匹配 → **从中立态直接蓄力横滑就出横扫**（无前置）。

**这条入口是本文件对应的提交 `41909871`（2026-11-03）加的**，提交信息里写明了用途：
「MoveEntry 加 minChargeLevel …… **这是 Slash 进表的前提，否则「横滑+蓄力 → Sweep」会被 Slash 抢走**」
→ 即：它是为了把「蓄力横滑」从 Slash 手里抢回来给 Sweep，**但被挂在了中立态入口而不是连段边**。

**关键**：SlashC2 本身**不需要**这条入口 —— 它的连段边是
`Zhangfei_Slash --Hold(minChargeLevel=1)--> Zhangfei_SlashC2`。
所以这条 root 唯一的作用就是「让中立态的蓄力横滑变成横扫」。

### 8.2 为什么「总是」成立

| 条件 | 状态 |
|---|---|
| `InputManager.restrictChargedToVertical` | **恒为 false**（代码默认值 false + 场景值 0 + **全工程无任何写入者**）→ 站桩蓄力横滑这条路永远开着 |
| `Zhangfei_Sweep` 的解锁门控 | **没有对应天赋节点** → `IsMoveLocked` 永远为 false → 中立态可直接出 |

### 8.3 本次处置（暂时停用）

`MoveTable_Zhangfei.asset` 的 root[2]：`minChargeLevel` 由 `1` 改为 **`99`（不可达）** → `FindRoot` 永远跳过它
→ 蓄力横滑回落到 `Zhangfei_Slash`（且 Slash 受 `talent_move_slash` 天赋锁）。
**恢复 = 把 99 改回 1**。

实测：横滑+蓄力 0/1/2/3 全部解析为 `Zhangfei_Slash`；解锁横斩后实机 `执行 Zhangfei_Slash`（不再是 Sweep）。

### 8.4 同类错误关联审计（结论：无第二例，但有两个遗留口子）

审计范围：全工程只有 1 张招式表（`MoveTable_Zhangfei.asset`）；12 个 AttackSkillConfig；4 条连段边。

| 检查项 | 结果 |
|---|---|
| 既是「中立态入口」又是「连段后继」的招 | **无**（Sweep 不是任何招的边后继） |
| 中立态入口是否受天赋锁 | **受**（`PlayerMoveStateMachine` 的中立态段也查 `IsMoveLocked` —— 这是历史修过的坑） |
| 连段后继是否受天赋锁 | **受**（StabR2 / LaunchFinisher / SlashC2 都有对应天赋节点） |
| 唯一「无解锁门控的中立态终结技」 | **Sweep**（本次停用） |
| 同一提交加的另外两条 root（横滑→Slash、斜滑→Slash） | 等价于旧的默认回落，**无副作用**（顺带把横滑纳入天赋锁，是好事） |

**遗留口子 ①（已于 2026-11-05 第二轮一并堵上 —— 改在**输入层**）**：
第一轮只停了 `root[2]`，但**蓄力本身还是能从中立态起手** → 「站桩蓄力 + 横滑」照样成立
（用户反馈：「依然可以直接触发 swipe 或者 sweep 的前置蓄力」）。
真正的闸门不在招式表，而在**输入层**：中立态能不能蓄力。
→ 现已在 `InputManager` 加 **`IsChargeAllowed()`**（= `comboChargeActive`），
**蓄力只在「有前置招式」时存在**，并在**蓄力的源头**（`isLongPress` 的成立条件）上加闸：
中立态长按 → 不进蓄力态 → **没有蓄力等级、没有蓄力进度指示器、没有蓄力武器后拉表现**，
也不会靠蓄力打出蓄力招（蓄力横扫 / 蓄力横斩 / 蓄力上挑 / 站桩蓄力穿刺）。
→ 连带：`MoveGestureDefaults` 的默认兜底（`SwipeHorizontal+charged→Sweep`）也够不着了 → 纯蓄力无法从兜底路径复活。
→ 日志可查：`[InputManager] 滑向蓄力不成立：没有前置招式（comboChargeActive=false）→ 按未蓄力处理`。

判据 `comboChargeActive` 的语义：招式状态机在「**招式进行中再次按下**」或「按住期间招式节点切换」时置位
→ 即**玩家自己刚出过招**（= 有前置招式）；中立态长按是「直通」，不会置位 → 蓄力不成立 ✓。
未按住时该标记每帧清零 → 不会残留成「假前置」。

**遗留口子 ②（仍未关）**：`Hold → Pierce`（**站桩蓄力穿刺**）已随上面的改动一起失效
（长按不再进蓄力态 → 不会打出蓄力穿刺；中立态长按退化为普通点按）。
但 `Zhangfei_Pierce` / `Zhangfei_Launch`（中立态蓄力上挑）现在**都没有入口**了 → 变成无入口内容，
是否给它们天赋入口（像 `talent_move_slashc2` 那样）还是删，**待设计定**。

### 8.5 设计口径待对齐（本次不动代码）

| 文档 | 口径 |
|---|---|
| `design/combo-move-state-machine.md:201` | 终结技身份**由前置决定**（走图上的边） |
| `design/charge-build-three-options.md` | **蓄力线（Pierce/Sweep 蓄力爆发）是被明确设计的独立流派** |
| `design/gameplay-pillars.md:148` | 疾病流「**与纯蓄力流互斥**」 |

→ 「纯蓄力该不该存在」是**口径问题**，不是单纯的 bug；这也是它反复回来的原因。
