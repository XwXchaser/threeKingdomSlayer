---
id: kd_c2de3458-57af-4257-9475-96d4662f67f2
injectMode: inherit
summary: 战斗已知问题清单（QTE 被打断、特效残留、BOSS 墙壁、超范围攻击等）与已修复项；含 Locus 会话上下文撑爆/自动压缩死锁的工具侧排查；带调试步骤的待办见 plan/unresolved-issues.md。
aiEditMode: inherit
---

# 已知问题

> 现象与根因清单；带调试步骤的待办见 `plan/unresolved-issues.md`。

## 0. 战斗结束时的事件会触发两次（WaveSpawner）✅ 已绕开（2026-11-03）
- **现象**：`WaveSpawner` 在一场战斗结束时会把 `OnAllWavesCompleted` **触发两次**——
  `NotifyWaveCleared()` 一次，`SpawnNextWave()` 的溢出分支再一次（`currentWaveIndex >= waves.Count`）。
- **后果**：挂在该事件上的「一次性发奖 / 一次性结算」会**执行两次**
  （实测 2026-11-03：路线战斗节点的通关奖 `clearCoinReward` 被付了两遍，N1 +60 变成 +120）。
- **为什么没早暴露**：非路线分支在第一次调用里就把关卡状态切成 `Victory`，
  第二次调用被 `CurrentState != InProgress` 的状态检查拦下 → **侥幸躲过**；
  路线分支状态一直是 `InProgress` → **没有这道闸**。
- **正确做法**：「战斗结束付一次」的逻辑挂在**每个 BattleEntry 只走一次**的地方
  （本项目：`FakeRouteRuntime.RunBattleEntries` 里 `MarkBattleEntryCompleted` 旁边），
  不要挂在 `StageRunFlow.OnAllWavesCleared` 上。

## 1. QTE 动画期间受道具伤害播放 Hit 动画
- **现象**: BOSS 在 QTE 动画时若受到道具等其他伤害，会播放受击(hit)动画打断 QTE 动画
- **预期**: 应继续 QTE 动画（死亡等不可逆状态除外）
- **关联**: 问题 2 可能由此引起

## 2. BOSS 连续使用 QTE 且无对应动画
- **现象**: BOSS 连续触发 QTE 但不播放对应 QTE 动画
- **可能原因**: QTE 动画被其他伤害打断（见问题 1），导致状态机异常循环

## 3. 攻击特效/伤害残留
- **现象**: 攻击特效或伤害数字未消失时进入 BOSS 战，这些特效会残留在 BOSS 战中
- **重现**: 经常反复出现

## 4. BOSS 未变成"墙壁"导致敌人被击退到身后
- **现象**: 普通敌人被击退到 BOSS 身后，导致玩家因攻击不到普通敌人而卡关
- **预期**: BOSS 应作为墙壁阻挡敌人后退

## 5. 被击退超出攻击范围的敌人仍在超范围攻击
- **现象**: 如 101 敌人被击退到 row4 BOSS 身后时，直接在 row4 开始攻击且不会补齐
- **重现**: 经常反复出现

## 6. Cyclone / SpikeTrap 等特效遮挡前方敌人 ✅ 已修复
- **现象**: CycloneEffect 生效时会错误遮���敌人；最初误判为缺少高 sortingOrder
- **实际根因**: 战斗场景依赖透视相机 + Z 位置做 2.5D 深度排序。给 Cyclone 设置高 sortingOrder 会绕过 Z 深度，导致后排特效压住前排敌人
- **修复**: CycloneEffect 保持 `sortingOrder = 0`，生成时 `pos.z -= 0.2f`，与 SpikeTrap 的 Z 前移思路一致，让目标行内显示在敌人身前，同时保留跨排前后关系
- **规则**: 战斗内敌人/地面特效遮挡优先用 Z 偏移，不要用高 sortingOrder；高 sortingOrder 只用于 overlay/描边/UI 类视觉

## 7. Locus 会话上下文撑爆 → 自动压缩死锁（工具侧，不是游戏缺陷）
- **现象**：长会话跑到约 1088 条消息后，任何新消息都发不出去，聊天直接报错，且每轮重试 3 次结果完全一致：
  - `Custom Chat API error (400): invalid params, context window exceeds limit (2013)`（openai_chat 端点，如 MiniMax）
  - 或 `Responses API error (400): No tool output found for tool call call_function_xxx`（openai_responses 端点，如 DeepSeek）
  - 伴随 `preflight auto-compact failed`，会话 run 直接 failed。
- **根因是两段**，别只盯报错：
  1. 自动压缩在约 878k 上下文时连试三次都产出 `Checkpoint compaction returned an invalid anchored summary`，Locus 拒绝并**保留原文** → 上下文一点没变小。
  2. 之后压缩请求本身超预算：`Checkpoint summary request exceeds the endpoint context budget: estimated_tokens=968342, budget_tokens=967232, summary_output_tokens=32768`。预算 = `contextLength`（1,000,000）− 固定 32,768 摘要预留；历史 968,342 只差约 1.1k token 就超 → **压缩永远发不出去**，形成死锁，原文被原样保留，990k+ 的请求照发 → 上游 400。
- **第二个报错（`No tool output found`）的来历**：上游 Responses 校验发现载荷里有 `function_call` 却缺配对的 `function_call_output`。本例缺的正是整段历史里**第一个异步投递**的工具结果（`unity_execute` 异步片段编译失败）。该消息在 DB 里仍在（role=tool、tool_call_id 齐全、include_in_prompt=1），Locus 也把该会话登记进 `session_context_capture_gaps` → 是**载荷拼装缺项，不是消息丢失**。
- **取证位置**：`H:/locus/data/locus.db`（表 `sessions` / `messages` / `session_events` / `token_usage` / `session_context_capture_gaps`）＋ `C:/Users/Administrator/AppData/Roaming/locus/logs/locus.log`（关键字 `auto-compact`、`Checkpoint summary request exceeds`、`invalid anchored summary`、`No tool output found`、`iteration N, messages=`、`[Responses] POST`）。注意 `session_context_attempts` 与 `response_request_payloads` 在本机**不落原始请求体**，别指望从那里复原载荷。
- **恢复**：不要在原会话里反复重试。首选**换新会话 + 交接摘要**（范例见 `plan/scene-pipeline-handoff-20261023.md`）。原地硬救需退出 Locus 后直接改 `locus.db` 删早期历史，把上下文压到预算之下让压缩能跑，代价高、风险大，且旧会话仍带缺配对的 function_call。
- **预防**：
  - 上下文逼近上限前就换会话并写交接文档，别等自动压缩兜底。
  - `contextLength` 配置值要给摘要预留留余量：上游真实窗口小于配置值时，压缩请求会先于主请求超预算。
  - 同一会话别混用 openai_chat 与 openai_responses 端点：换端点会换掉报错形态（2013 → `No tool output found`），极易误判成新故障。
- **2026-10-29 根因修正（DeepSeek 端点实测）**：`No tool output found for tool call` **不只由压缩/裁剪引起**——全新小会话（3 条消息、无压缩）同样复现。根因是**端点协议不匹配**：该 DeepSeek 供应商在 `%APPDATA%/locus/custom_providers.json` 里配成 `apiFormat: openai_responses`（打 `https://api.deepseek.com`），而同一 id 在 `custom_endpoints.json` 里写的是 `openai_chat`；运行时以 providers 文件为准 ⇒ Locus 发 Responses 形状的 `function_call_output`，上游不认 ⇒ 400。
  - **排查要点**：失败请求 100% 是「带工具结果」的请求，纯文本请求（input=1）从不报此错；同一载荷换 `openai_chat` 端点即通过；坏项会留在历史里把会话毒死（每轮都引用同一个 call id）⇒ **原地重试无效**，必须换模型或开新会话。
  - 已知频率（2026-10-22→10-29 日志统计）：deepseek-flash 请求 89 次中失败 48 次，MiniMax 同一载荷无失败。

## 8. Locus 会话 413 Payload Too Large（图片附件累积撑爆请求体，工具侧）
- **现象**：会话跑到中途后，**连续两轮 run 直接 error**，报错是网关层的 `Custom Chat API error (413 Payload Too Large): <html>…nginx…</html>`（上游 `https://api.muskapis.com/v1/chat/completions`，nginx 拒收请求体）。该会话之后任何消息都发不出去 → 任务卡死无法继续。
- **与问题 7 的区别**：问题 7 是**上下文 token 预算**型（400 / 2013 / 压缩死锁）；本条是**请求体字节数**型（413），别按同一种故障排查。
- **根因（实测 2026-10-22 会话 `6938f3dc-20ac-4da1-87dc-5a198e9d8585`）**：会话 1038 条消息里只有 **26 条带图片附件，合计 104,510,310 B ≈ 99.7 MiB 的 base64 PNG**（`read` 工具读 PNG / 截图回传时把**全分辨率原图**内联进消息）。同期纯文本只有 content 0.61 MiB + tool_calls 0.61 MiB + thinking 0.93 MiB → **撑爆的是图片，不是文字**。单张最大三张 base64 分别 12.65 MB / 12.45 MB / 12.38 MB。
- **取证**：`H:/locus/data/locus.db`（只读打开即可）→
  `select count(*), sum(length(images)) from messages where session_id='<id>';`
  `select length(images), substr(images,1,80) from messages where session_id='<id>' and length(coalesce(images,''))>0 order by length(images) desc;`
  注意 `response_request_payloads` 在本机不落原始请求体，指望从那里复原载荷是没用的。
- **恢复**：同问题 7 —— 不要在原会话里反复重试；换新会话 + 交接摘要（范例 `plan/scene-pipeline-handoff-20261023.md`）。**未提交的工作区改动不会丢**，按 `git status` 接管即可。
- **预防**：
  - 长会话里**别把全分辨率 PNG 读进对话**：先用外部脚本裁剪/缩放后再 `read`；成组对比图改用拼图脚本合成一张。
  - 会话里读图次数控制在个位数；每读一张大图就估算累积体积，超过 ~20 MiB 就先写交接文档再换会话。
  - 截图验收优先走「渲染到文件 → 桌面文件夹」而不是反复回传图给模型。

## 10. 常态点击引导（手 + 光圈）与敌人对不齐 / 圈过大 / 无敌人处出现光圈 ✅ 已修复（2026-11-02）
- **现象 A**：正式关卡里，引导的**手指与光圈位置对不上**；光圈**明显偏大**。
- **现象 B**：某个位置**明明没有敌人**，却挂着常驻点击提示。
- **根因 A**：
  1. 上一轮改「pivot 改掌心 + 位置补偿」时，那批编辑**整批回滚**（工具报 `Edit 5 of 5 failed`）→ 实际只改了 pivot、**没加位置补偿** → 手整体压在目标点上、指尖偏左上 ≈200px；
  2. `TipUV/PalmUV` 是从 prefab pivot **反推**的，不是量图得来的（实测指尖 (0.273, 0.801)、掌心 (0.48, 0.48)）；
  3. 光圈尺寸用**固定设计像素**，而战斗 HUD 画布（1080×2160）与教学提示画布（1080×1920）缩放不同 → 正式关里大得离谱。
- **根因 B**：unit 持有**上一帧的敌人引用**；敌人死亡/被对象池回收后光圈停在原地。
- **修复**：尺寸改成**相对敌人屏幕高度**（圈 = 敌人高 × 0.3）；坐标常量换成像素实测值 + 位置补偿（指尖落在圈心，实测偏差 14.2px）；`CollectTargets`/`Refresh` 双重校验目标存活 → 敌人一死立刻撤圈（实测清空后 0.6s 内无残留）。
- **详见** `plan/tutorial-and-hint-overhaul-plan-20261101.md` §0.14（含截图路径与像素测量数据）。

## 11. 教学引导完成后不消失（光圈/手指残留）+ 霸体防御拍时机不对 ✅ 已修复（2026-11-02）
- **现象 A（用户第三轮反馈）**：「有些引导完成后不消失」——三选一选了【海浪】后光圈还在；开场点按教学同样收不掉；教学关的残留引导会跟着流进正式关。
  - **根因**：`TutorialGestureHint.Hide()` 只调 `HideLayers()`（`SetActive(false)`）**不停协程**；而循环示范（`PlayTapLoop` / `PlayPressHoldLoop`）每一轮都会 `ring.gameObject.SetActive(true)` → **下一帧又把光圈画回来**。
  - **修复**：`Hide()` = `StopRoutine()` + `HideLayers()`；`HideLayers()` 里顺手把手恢复静止姿态。
  - **验证**（Play Mode 全流程驱动）：第一拍结束 / 选完海浪 / 技能拍结束 / 全流程结束时，采样光圈与手指均为 False。
- **现象 B**：「不能一标红就开教学」——霸体防御拍原本在 `isCFrame` 刚成立（敌人刚变红）就冻结。
  - **实测**：1012 的 C 技攻击 tween 全长 3.13s，`isCFrame` 在剩 1.15s 时解除（= 出伤）。
  - **修复**：冻结判据改为 `Enemy.TimeToAttackEnd() <= defendPromptToEndSeconds`（默认 1.5s，可配）+ 仍要求 `isCFrame`；顺带修掉两个真问题：① 玩家抢在敌人抬手前把它点死 → 整拍被跳过（改为**拍一开始就禁出招**）；② 目标死亡直接丢拍（改为**换下一只**）。
  - **验证**：冻结瞬间敌人显示 `Enemy_1012_attack5`（举刀姿态，出伤帧 attack6 未出现）、攻击剩余 1.48s；玩家按住防御后这一击被**吸收 1 次**。
- **详见** `plan/tutorial-and-hint-overhaul-plan-20261101.md` §0.15。
- **仍待复测**：正式关内**常驻引导**（`AttackTapIndicator`）的对位 → 已于同日修完（见第 12 条），`plan/unresolved-issues.md` TODO-6 已关闭。
- **第二轮（2026-11-02 晚，用户仍报「残留最急」）：第一轮只堵住了一条路，另有 3 个洞**
  1. **循环示范没有主人概念**：`PlayTapLoop` / `PlayPressHoldLoop` / `PlayParry` 都是**无限协程**，只要启动它们的调用方没走到收尾，它们就一直画下去。上一轮只改了 `Hide()`，但**没堵住「根本没人来调 Hide()」的路**。
  2. **技能打断拍的两条提前退出**：`TutorialStageDirector.Beats.cs` 的 `SkillSlotInterruptRoutine` 在「舞台结束 / 目标死了」时走 `yield break` → **跳过了收尾** → 此时「循环点技能槽」的示范已经在跑 → 光圈和手永久残留。
  3. **三选一的那只手不属于任何拍**：`ShowChoiceHint()` 直接 `PlayTapLoop(卡片坐标)`，只有 `OnChoiceSelected` 会收它；弹窗以别的方式关掉（舞台结束/放弃）→ 无限协程留着 → **跟着流进正式关**。
  4. 附带：`keepRingVisible`（连点教学的光圈常驻）**从不复位** → 之后的任何循环示范每点一下都会把光圈留在场上。
- **修复（第二版）**：
  - `TutorialGestureHint` 每轮循环问宿主「还要不要我」（`HostAllowsDemo()` → 导演的 `BeatActive || ChoiceHintActive || RouteHintActive`）→ **主人没了就自己收摊**（`Finish()`），这一条把**所有**无限示范的残留一次性堵死；
  - `SkillSlotInterruptRoutine` 的两处 `yield break` 改成 `break` → 必定走到收尾（解冻 → `EndBeat()` → `Hide()`）；
  - 导演 `Update()` 兜底两条：三选一弹窗已关 → 收掉那只手；**不在教学路线上** → 一律收掉（正式关/落地页不该有教学手势）；
  - `Hide()` / `Cancel()` 里复位 `keepRingVisible`；
  - `AttackTapIndicator`：教学手势提示**只要还在屏上**就不挂常驻引导（之前只看 `BeatActive` → 三选一/路线箭头那只手出现时，常驻引导会同时在屏 → **两套手+光圈**）。
- **验证**（Play Mode 全流程驱动，420 次采样 / 85 秒）：提示层在屏 112 次，**无主残留 = 0**、**漏到教学路线外 = 0**、**两套提示同时在屏 = 0**；结束时提示层为空。
  另外单测：`PlayTapLoop` / `PlayPressHoldLoop` / `PlayParry` 在「无主人」时启动 → **立即自收**（`IsShowing=False`）。
- **已闭环**：~~按压动画绕掌心转、手身相对圈心摆 ≈48px~~ → 第四轮按「指尖=定位锚点 / 掌心=旋转支点」重做，
  **用户 2026-11-02 确认「问题1已修复」**（见下方「按压手势的分工」）。

### 按压手势的分工（2026-11-02 第四轮定稿）
- **指尖 = 定位锚点**：手的位置由指尖决定 → 静止时指尖精确落在圈心（实测 0.2px）。
- **掌心 = 旋转支点**：按压动画绕掌心转，**掌心在按压过程中完全不动**。
- **旧实现的坑**：pivot 本来就是掌心，但按压时还有一条**逐帧位置补偿**把指尖钉回圈心；
  「绕掌心转 + 钉死指尖」在数学上等价于「**绕指尖转**」→ 掌心被迫绕着指尖走，
  视觉上就是「整只手以指尖为圆心在转」，不像点击（用户：「实际是绕着手指在旋转，而不是掌心」）。
  实测旧值：掌心→圈心 192~204px（半径不变、切向摆 ≈30px）= 绕圈运动。
- **修法**：删掉逐帧补偿（`TutorialGestureStyle.RotatedTipOffset` 已删除）→ 按压只改 rotation/scale；
  角度默认 14° → **8°**（可配），方向改为正角度（指尖**下沉**，读作“按下去”，原来是负角度=指尖上抬）。
- **实测（新）**：旋转角 0°~8°；**掌心→圈心 124.8~124.8px（完全不动）**；指尖→圈心 0~18.5px（光圈半径的 15%，仍在圈内）。
- **代价**：按压那一瞬间指尖离开圈心 ≤18.5px（教学层）/ ≈30px（常驻引导，手更大，仍在光圈内）。
- **验收**：2026-11-02 用户确认「手和圈没有错位 / 问题1已修复」。实测（第四轮）：旋转角 0°~8°、
  掌心→圈心 124.8~124.8px（完全不动）、指尖→圈心 0~18.5px。
  **2026-11-02 第三轮已把分工理清**：**指尖 = 定位锚点**（静止时落在圈心）、**掌心 = 旋转支点**（按压时掌心不动）。
  代码里 pivot 确实 = 掌心，但**逐帧位置补偿**（`AttackTapIndicator.cs` / `TutorialGestureHint.cs` 里把指尖钉回圈心的那两行）
  把旋转支点偷换成了指尖 → 视觉上「整只手绕指尖转」。修法 = 删掉逐帧补偿（掌心固定，指尖走弧）。
  指尖离圈心最大距离 = 2×|掌心→指尖|·sin(θ/2)：θ=14°→44 单位（光圈半径的 24%）、θ=8°→25 单位（14%）。
  **待用户拍板**：θ 默认值 / 是否接受按压瞬间指尖离开圈心。

## 13. 常态点击引导的规则问题 ✅ 已修复并验收（2026-11-02 第三轮修，第四轮验收）
- **现象 A**：前排有 2~3 只敌人时，常驻引导**每只都挂一个**手+光圈 → 玩家不知道该点哪个、满屏都是手。
  - **根因**：`AttackTapIndicator.CollectTargets()` 收集了所有 `rowIndex == 0` 的敌人，`Refresh()` 对每个建一个单元。
  - **修复**：`PickTargets()` 只挑**随机 1 只**，且**保持稳定**（当前这只还在候选里就继续用它，它没了/前排换人才重抽 —— 否则每 0.1s 刷新重抽会乱跳）。
- **现象 B**：常驻引导**一直常驻**，被当成可点的按钮（用户要求改成闪烁）。
  - **根因**：`Animate()` 里手 alpha 恒为 `handAlpha`、光圈 alpha 恒在 0.63~0.9，全程不灭；唯一的动是 0.96~1.04 的呼吸（肉眼看不出）。
  - **修复**：新增可配闪烁（`blinkEnabled` / `blinkPeriod=1.8` / `blinkOnRatio=0.55` / `blinkFadeSeconds=0.22`），手+光圈+波纹一起淡入淡出。
- **附带**：教学手势提示在屏时，常驻引导**立刻**撤掉（不再等 0.1s 刷新）→ 消除「两套手+光圈同屏」的 ≤0.1s 重叠。

## 14. 「用技能打断霸体」教学整段缺失 ✅ 已修复并验收（2026-11-02 第三轮起，第九轮收尾）
- **现象**：三选一之后的「暂停 → 玩家点技能槽 → 技能打断敌人 C 技」这一段教学**根本没发生**。
- **根因（全部实测取证）**：
  1. **成功判据被卡肉误触发**：`Beats.cs` 的 `if (target.attackInterrupted || target.IsSuspended)` —— `IsSuspended` 包含 `SuspendSource.HitStop`，
     而**任何一次命中**都会 `ApplyHitStop` → `PushSuspend(HitStop)` → 判据立刻成立 → 拍瞬间收拍 → 玩家没机会点技能。
     实测旧日志：「打断=**False** 挂起=**True** → 收拍」= 根本没有技能打断。
  2. **时停期间根本放不出技能**：`ActiveSkillInventory.TryActivate` 有一道硬闸 `if (Time.timeScale <= 0f) return false;`
     → 拍冻结后玩家点技能槽**毫无反应**（实测：驱动每 2s 点一次，全部无效）。→ 新增 `GameplayInputGate.AllowSkillActivationWhileFrozen`（教学专用开关）。
  3. **台词在播时点击被吞**：`DialogueManager.IsInteractionBlocked`（含 nonBlocking 台词的 `_isPlaying`）→ `FrontItemBar.OnItemIconClicked` 直接 return。
     → 冻结那一刻先把台词 Skip 掉。
  4. **拍开始不拦出招**：`BeginBeat(BlockAll)` 原来只在冻结那一刻才调用 → 等待期间玩家照打 → 教学目标被打死 → 拍静默结束。
     → 改成拍一开始就 `BeginBeat`（与霸体防御拍一致）。
  5. **等技能槽 15 秒就跳过整拍**：三选一恰好在这一拍开始时才弹 → 旧逻辑必然超时 → 「等不到技能槽，跳过这一拍」。
     → 等待上限改为 `skillSlotWaitSeconds=60`，且**不再跳过教学**。
- **顺带对齐防御拍**：冻结时机从「C 技刚起手」改成「抬手做完」（`skillPromptToEndSeconds=1.5`，与 `defendPromptToEndSeconds` 同口径）；
  目标死了**换下一只**继续等；等 C 技的等待预算独立计算。
- **验证**：教学全流程驱动 → 技能拍在 39.74s 冻结（同帧驱动放技能）→ `attackInterrupted=True` → 拍正常收 → 教学通关；
  全程 4225 次采样：无主残留 0 / 漏到教学外 0 / 两套提示同屏 0。
- **第五轮（用户：「完全没有改善」+「点技能后回不到正常流程」）—— 2 条根因**：
  1. **拍的顺序错了：拍在波次开始就开，而三选一弹窗恰好也是这一刻弹出**
     → 实测 **22.38s 冻结 / 22.40s 弹窗** → 冻结画面和「点技能」手势整个被弹窗盖住，
     玩家根本看不到这一步；等弹窗关掉，拍已经快结束 → **教学等于没发生**。
     → 修：`SkillSlotInterruptRoutine` **先等玩家把三选一选完**（`ChosenSkill != null`，上限 `skillChoiceWaitSeconds=90`），
       这一段**不开拍、不拦出招**；选完之后才正式开拍（BlockAll）。
  2. **点完技能拍不收 → 出招闸门一直 BlockAll → 玩家只剩防御和技能能用**
     → 修：成功判据改成 **必须先 `skillUsed`（技能真的放出去了）**，且技能放出后最多再等
       `skillResultWaitSeconds=3`，**没打断也收拍**（教学不能把玩家卡住）。
- **第五轮实测（停驱动 = 模拟真人）**：
  21.66s 技能拍开始（等选卡，不开拍）→ 22.40s 玩家选卡 → 22.52s 正式开拍（闸门 BlockAll）
  → 25.36s 敌人抬手完成 → **冻结 + 画面压暗 + 手势落在技能槽（指尖偏差 0.2px）**
  → 真人点击技能槽 → 技能释放（CD=10）→ 0.05s 后：解冻、拍收、**闸门恢复 None（玩家能正常出招）** ✓
- **第六轮（2026-11-02 深夜，用户：「完全没有改善 / 下一次敌人攻击没有冻结」→ 查 Editor.log）**：
  - **日志证据**（**`%LOCALAPPDATA%/Tuanjie/Editor/Editor.log`**，不是项目内那个 `Logs/Editor.log`——那是 10 月的旧文件）：
    用户最近 3 次教学会话里这一拍**都跑通了**（`技能打断拍：敌人抬手完成（剩余 1.48~1.50s）` → `Activated 海浪` → `收拍（打断=True）`）；
    用户**最后一次**会话的日志正好停在「玩家选中技能：海浪」，之后敌人还没抡招就退出了 Play Mode。
  - **真正的隐患（已修）**：玩家选完卡**手痒先点了技能**（冷却中）→ 旧写法把这个提前释放当成「教学已完成」→
    `skillUsed=true` → **冻结分支被跳过** → 敌人再怎么抡招都不会冻结 → 这一段教学静默消失（正是「下一次敌人攻击没有冻结」）。
    → 修：① **冻结必须等技能冷却结束**（技能不可用就不冻结，否则玩家点了也没反应、教学卡死 30 秒）；
      ② `skillUsed` **只认冻结之后的释放**。
    → 实测：选完卡立刻放技能 → 等冷却（10s）→ 仍然正常冻结 → 放技能 → 收拍 ✓。
  - **加了阶段日志**（`[Tutorial][技能拍]` 前缀，进 Editor.log）：① 等玩家选完技能 → ② 开拍 → `WAIT-C`（每秒：敌人/抡招/C技/剩余秒数/技能是否可用）→ ③ 已冻结。
    下次测试只要看这四行，就能立刻知道卡在哪一步。
- **第七轮（真正的根因，日志取证）**：
  - **真凶**：1012 的攻击是**序列轮换**的 —— `Enemy.Attack.cs` 的 `PlayAttackAnimationTween()`：
    `var step = attackSequence[_currentAttackStep]; _currentAttackStep = (_currentAttackStep + 1) % attackSequence.Count;`
    然后 `isCFrame = isCAttack;` → **普通攻击和 C 技交替上**。
    而这一拍原来的冻结判据要求 `isCFrame`（= 霸体招）→ **敌人的「下一次攻击」如果是普通攻击，就永远等不到** →
    教学静默消失（用户那次会话 1012 连续两次普通攻击，一次都没冻上，日志里连一行「技能打断拍」都没有）。
    → 修：**冻结判据改成「敌人的下一次攻击」（不再要求 C 技）**，抬起到顶（剩余 ≤1.5s）就冻；
      日志里会标明这次是「C技」还是「普通招」。
  - **流程坑（也是「和修复前完全没变化」的原因之一）**：**我在改代码时反复 `unity_recompile`，
    域重载会把用户的 Play Mode 重置**（日志里能看到 `Start importing …StageController.cs` →
    `场景加载完成，自动开始关卡` → 教程被重开）。→ 约定：**改完代码先告诉用户、等用户测完再编译**。
- **第八轮（用户：「为什么会受击？为什么没有触发技能使用的流程？」+ 用户要求自己复述正确流程）**：
  - 用户那次会话的日志：拍开到了「② 开拍」+「等玩家选完技能 → 已选 active_wave」，
    然后**再也没有任何一行拍相关日志**（连「等敌人抡起霸体技」那一行都没有，也没有异常），
    而敌人一直在打玩家（受击）、玩家自己放了海浪。
  - **真凶（堆栈取证）**：`TutorialStageDirector.Update()` 里的**出口光标清理**调了 `StopHint()`。
    `StopHint()` 会 `StopCoroutine(_hintRoutine)` → **把当前正在跑的教学拍一起掐掉**；
    那段清理的判据是 `_routeHintActive`（路线光标标记，**从不复位**）→ 用户那次它刚好在拍开拍的同一帧触发。
    → 后果：① 冻结代码根本没执行（没有技能使用流程）；
      ② 出招被封（BeginBeat 的 BlockAll）+ 防御键被 `OnChoiceSelected` 锁住 → **只能站着挨打**。
    → **修**：那段清理**只收路线光标自己**（新增 `_routeHintRoutine` 单独持有那个协程），**不再调 `StopHint()`**。
  - 同时修的：
    - **找技能槽不再阻塞**：查一次 → 最多等 2 秒 → 查不到就**立刻**用「按槽位序号取位置」的兜底
      （旧写法等满 60 秒才跳过整拍 → 教学不发生、玩家挨打）。
    - **这一拍解锁防御键**（`OnChoiceSelected` 锁的）→ 等待抡招期间玩家能格挡，不白挨打。
    - 冻结判据 = 「敌人的**下一次攻击**」（不挑 C 技；1012 的普通招/C 技轮换，见第七轮）。
    - `StopHint()` / `OnDisable()` 都加了**堆栈日志** → 下次再看日志能直接知道是谁掐的。
  - **验证**：自动跑全流程 → 选卡 → 开拍 → 敌人抡招 → 冻结 → 放技能 → 收拍（打断=True）✓。
- **第九轮（用户：「终于能正常暂停，但是没有对应技能槽位点处出现手势指引和高亮提示」）**：
  - **根因**：场景里有**两个 `FrontItemBar`**（`HudCard` 下那个配了 4 个槽位；根节点上还有一个**空的**），
    而 `Instance` 是「先 Awake 的赢」→ 拿到的是**空的那个** → 所有技能槽查找都查空表
    → `TryGetActiveSkillSlotScreenPoint` 永远失败 → **手势指引出不来**（诊断日志：`entries=1 图标已注册=0 槽位数=0`）。
  - **修**：
    ① `FrontItemBar.Awake()`：**有槽位的那个优先当 Instance**；新增 `FrontItemBar.ResolveForSkills()`（遍历找带槽位的那个）；
    ② 教学拍查技能槽时**逐个 FrontItemBar 试**（不再只认 Instance）；
    ③ 冻结期间给**技能槽图标做脉冲缩放**（1.00↔1.12）作为高亮提示，收拍时复位。
  - **验证**（模拟真人：停驱动等冻结）：冻结中 ts=0、**手势在屏 ✓**、**指尖→技能槽偏差 0.6px ✓**、
    **高亮脉冲 scale 1.000~1.120 ✓**、技能释放 → 打断 → 收拍 ✓。
    截图：`Library/Locus/Screenshots/locus_game_20261102_075809_274.png`。
  - **✅ 验收**：用户 2026-11-02 确认「已验收」（暂停 / 技能槽手势指引 / 槽位高亮三项均正常）。
  - **遗留（非本条）**：常驻引导的「指尖→圈心」在按压时会离开圈心 ≤30px（第四轮定稿的分工：指尖=定位、掌心=旋转支点），已验收。


## 15. 跑关机器人卡死在「升级三选一」✅ 已修复（2026-11-04）
- **现象**：`BattleAutoPlayer` 跑到正式关 N1，敌人清完后停在「升级三选一」面板上不动：`IsChoosing=True`、`_pendingChoices` 有 3 个、`_running=True`，但机器人永远不选，`Time.timeScale=0`。
- **根因**：三选一面板会 **暂停游戏**（`UpgradeChoiceManager.cs:245` → `Time.timeScale = 0f`）→ **缩放时间 `Time.time` 冻结** → 机器人的 UI 节流计时器 `if (Time.time < _nextUiClickTime) return;` **永远成立** → `HandleBlockingUi()` 每帧都在第一行就 return，连三选一的分支都进不去 → **死锁**（机器人卡住 → 面板不关 → 时间一直冻着）。
- **实测取证**：`Time.time` 35.522 → 35.522（不动）而 `Time.unscaledTime` 152.912 → 153.027；`_nextUiClickTime=35.72 > Time.time=35.52` 恒成立。
- **修复**：机器人所有 **UI 节流计时器改用 `Time.unscaledTime`**（UI 点击节流本来就该用真实时间）；并把「点掉对话气泡」分支挪到「选三选一」**后面**（三选一更重要，且它会暂停游戏）。
- **规则**：**机器人里凡是「等一下再动作」的计时器，一律用 `Time.unscaledTime`** —— 缩放时间会在任何暂停 UI（三选一 / 对话 / 教学门槛）下冻结。
- **注**：交接文档里「机器人已经会点三选一」的说法只在**弹窗恰好没暂停**时成立，升级三选一这个场景以前必卡死。

## 16. 暂停恢复时 PixelHitEffectManager 抛 MissingReferenceException ✅ 已修复（2026-11-04）
- **现象**：Console 报 `MissingReferenceException: The object of type 'GameObject' has been destroyed`，
  栈是 `PixelHitEffectManager.ReturnToPool` → `GameplayPause.ResumeRegistered` → `Tween.Kill(false)` → `OnKill` 回调。
- **根因（两层）**：
  1. `PixelHitEffectManager.ReturnToPool` 直接访问 `instance.root.activeSelf`——池里的实例 GameObject 可能已被销毁
     （场景卸载 / `OnDestroy` 里杀 Tween）→ Unity 的「假 null」→ 抛异常。
  2. `GameplayPause.ResumeRegistered` 里 `t.Kill(false)` 会**触发该 Tween 的 OnKill 回调**→ 回调里抛异常 →
     **异常向上冒 → 整批恢复循环中断** → 后面的 Tween 永远停在暂停态（且 `s_pausedByGate` 不清空）。
- **修复**：① `ReturnToPool` 先判 `instance.root == null` 就直接丢弃；取实例改走 `TakeInstance()`（跳过已销毁的）；
  ② `ResumeRegistered` 每条 Tween 单独 `try/catch` → 一条坏的不影响其余。
- **规则**：`GameplayPause` 登记的 Tween 在「目标已销毁」时被 Kill 是**必然**发生的（暂停期间对象被销毁），
  所以「Kill → 回调 → 访问已销毁对象」这条链必须处处判空。

## 17. 教学手势「门槛机制」是没人用的旧机制 ⚠️ 别再当教学机制（2026-11-04 实测）
- **事实**：`TutorialGestureHint.Gate(...)`（暂停整个游戏 + `GameplayInputGate.RequestBlock(TutorialGate)` + 原始输入自判）
  **全工程无人调用**。教学拍（`TutorialStageDirector.Hints.cs` 的 `HintRoutine`）用的是完全另一套：
  **示范循环 + 招式真的打出去并命中**（`OnTutorialAttackExecuted`）+ 冻敌人 + 压暗场景 + 手势闸门限手势，**不暂停全局**。
- **代价**：交接文档 §3.2 曾写「Tap 教学用 Gate」→ 照它做的解锁引导与教学拍**不一致**（玩家划的那一下打不出去、
  抢同一个提示层导致教学拍手势收不掉）。2026-11-04 已改为「解锁引导 = 教学拍」。
- **规则**：要「教一个招式」→ 把它当**一拍拍出来**（`TutorialStageDirector`），不要另起一套表现。
- 附带坑：`TutorialGestureHint.Hide()` **不会停掉 Gate 的示范循环**（只停 `_routine`）→ 门槛期间被 `Hide()` 会让手势反复冒出来。

## 18. 教学关专属的教学行为漏进正式关 ✅ 已修复（2026-11-04）
- **现象**：正式关 N1 节点升级弹三选1 时，弹出「三选1 教学台词」（「听我的，选【海浪】…」）+ 一只指着推荐卡的手；而且**只能选推荐的那张**。
- **根因**：`TutorialStageDirector.OnChoicesReady` **没有任何路线门槛**。教学导演是**常驻组件**（挂在 Battle.scene），且每次切路线场景会**重建**（`Loaded scene 'Temp/__Backupscenes/0.backup'`）→ 新实例的「一次性标志位」复位 → 在正式关又教一遍。
- **修复**：加 `IsOnTutorialRoute()` 门槛（判当前路线是不是教学路线），正式关**不念台词 / 不指卡 / 不限制只能选推荐项**。
- **规则（P4）**：教学关专属的行为**必须自己判路线**，不能靠「只跑一次」的标志位——常驻组件 + 场景重建 = 标志位会复位。

## 19. 教学系统的五个隐形坑（2026-11-04 全链路实测踩出来的）✅ 已修

> 完整版见 `design/tutorial-guides-spec-20261104.md`（§4.1 全链路 + 5 个真 bug / §4.2 视觉 / §4.3 长枪）。

| # | 现象 | 根因 |
|---|---|---|
| 1 | **教学关永远结算不出来** | `TutorialFlowDriver` 的「技能拍期间不点按」判断**没带 `beatRunning`** → 拍结束了驱动还在等 → 最后一只敌人永远不死 |
| 2 | **天赋页教学不出现** | 天赋页是从 overlay **直接激活**的（不走 `Open()`）→ 教学没被启动 |
| 3 | **所有招式拍收不了** | 教学关的防御拍/技能拍把 `_beatEndsOnAttack` 关成 false，而**从来没人复位** |
| 4 | **派生招式明明打出来了，这一拍却不收** | 命中判定只靠 `OnAttackExecuted` 事件，而**连段/缓冲触发的执行不派发这个事件** → 改用**招式状态机当前节点**判定 |
| 5 | 机器人在教学拍里**连点三下**把连招打没了 | 调试注入**绕过教学闸门**（真实输入会守）→ `SubmitGestureForDebug` 已补闸门校验 |

**规则（避免再踩）**：
- 教学导演常驻 + 切场景重建 → 「只跑一次」的标志位会复位 → 教学行为必须**自己判路线**。
- 调试注入会绕过教学闸门 → 机器人跑教学拍前先确认注入路径也守闸门。
- TMP 文字要有**带中文字形的字体**（`TMP_Settings.defaultFontAsset` 是 LiberationSans → 中文全空白）。
- 引导锚点用 `rect.center`（视觉中心），不要用 `rectTransform.position`（那是 pivot，很多 UI 的 pivot 在角上）。
- 带旋转动画的手，位置必须**每帧反算**，否则指尖会甩离目标。
- `localScale` 是**倍率**不是尺寸 → 尺寸和缩放别同时乘（光圈会变成巨物）。

## 13. 教学关退出回主菜单必崩 + 教学状态漏到主菜单/正式关 ✅ 已修复（2026-11-05）
- **现象**：教学关里点「返回主菜单」（战败面板 / 暂停菜单那个按钮）→ 控制台抛
  `MissingReferenceException: The object of type 'TutorialGestureHint' has been destroyed`；
  异常顺着 `OnDisable` 往上冒 → **后半段清理全被跳过** → 教学状态漏到主菜单和正式关。
- **根因（两层）**：
  1. **`?.` 用在 `UnityEngine.Object` 上是错的**：`gestureHint?.Hide()` 走的是**引用 null** 判断，
     而 Unity 的「假 null」（对象已 Destroy、引用还在）它挡不住 → 场景重载时提示层先被销毁，
     这里照样调用 → 内部 `StopCoroutine` 抛异常。
  2. **清理是一条没有容错的直线**：`OnDisable` 里「退订 → 收拍 → 清白名单/三选一开关/输入闸门」，
     任何一步抛异常 → 后面全不跑。而漏掉的全是**活得比场景长**的状态（static）：
     `TutorialAttackGate` 白名单、`GameplayInputGate.AllowSkillActivationWhileBlocked/Frozen`、
     `DialogueManager.AllowDialogueWhilePaused`、`Time.timeScale`。
- **后果（能坏到什么程度）**：
  - `TutorialAttackGate` 白名单残留 → **正式关里玩家一个招式都放不出**（白名单 = 已教完的招式）。
  - `Time.timeScale` 残留 0 → **主菜单点不动**（三选一面板开着退出时最典型：面板把时间压成 0）。
  - `UpgradeChoiceManager.suppressChoices / forcedChoices` 残留 → 正式关的三选一被锁死在教学池。
- **修复（架构级）**：
  1. `TutorialGestureHint.StopRoutine / Cancel`：已销毁时不再调 `StopCoroutine`。
  2. 导演新增 **`ReleaseGlobalTutorialState()`** —— 教学占用过的**全局/静态状态唯一出口**（幂等）。
  3. `OnDisable` 重排为 **① 回收全局状态 → ② 退订事件 → ③ 收拍**，每步独立 `try/catch`
     （`SafeStep`）—— **任何一步抛异常都不许挡住后面的步骤**；全局状态排在最前，因为它最危险（static）。
  4. 导演里所有 `gestureHint?.X()` 改成 Unity 语义判空（`SafeHideGestureHint()` / `if (gestureHint != null)`）。
  5. `UpgradeChoiceManager` 记录 `_pausedByUs` → `OnDestroy` 时把**自己**暂停的时间还回去。
- **验证**：教学关跑到「白名单=Stab」→ 中途 `GoToMainMenu()` → 重载后：白名单随路线切换正确放开
  （切正式关 = `不限制`）、四个 static 开关全 False、timeScale=1；三选一面板开着退出也 timeScale=1；
  Console 0 error/0 warn。
- **通用规则（写给下一个改教学的人）**：
  - **`?.` 不能用在 `UnityEngine.Object` 字段上** —— 要用 `if (obj != null)`（Unity 语义，含假 null）。
  - **凡是 static / 单例上的状态，退出路径必须有一个「唯一回收口」**，且回收要**幂等 + 分步容错**。
  - **场景重载时对象销毁顺序不定** → 清理代码不能假设「我引用的对象还活着」。

## 12. 正式关里常驻引导的手/光圈错位（指尖偏 ~200px）✅ 已修复（2026-11-02）
- **现象**：正式关里，前排敌人身上的手和光圈对不上（教学关看不出）。
- **根因**：`AttackTapIndicator` 的**手是 TapHint 根节点的子物体**（根节点已摆在敌人屏幕点上），但代码把手的 `anchoredPosition` 写成了**画布绝对坐标**（`local + 偏移`）→ **位置算了两遍**，指尖落到 `2×目标点`。敌人离画面中心越远偏得越多（教学关敌人居中 → 被掩盖）。
- **实测**：正式关「长坂坡·卷轴」N1 → 指尖→圈心 **247px / 151px**（敌人偏 ±176 画布单位）。
- **修复**：① 手的 `anchoredPosition` 改为**相对根节点的偏移**；② 新增 `TutorialGestureStyle.RotatedTipOffset()`，按压动画（绕掌心转 + 缩放）每帧把位置反向补偿 → 指尖始终压在圈心（常驻引导 + 教学手势层都接上）。
- **验证**：正式关 6 秒 240 次采样指尖→圈心 **恒为 0.0px**；教学关常驻引导 100 次 / 教学手势层 30 次均为 **0.0px**。
- **详见** `plan/tutorial-and-hint-overhaul-plan-20261101.md` §0.15。

## 9. Locus 会话 422 new_sensitive（图片被上游内容审核判敏感 → 会话永久卡死，工具侧）
- **现象**：`Custom Chat API error (422 Unprocessable Entity): {"type":"error","error":{"type":"unprocessable_entity_error","message":"input new_sensitive, messages[48]'s content[1] image is sensitive, please check your input (1026)"}}`。首次出现后，该会话**任何新消息都在数秒内以同一条 422 结束，索引不变**，会话彻底废掉。
- **不是死循环也不是崩溃**：每次 run 只失败一次就结束，没有自动重试风暴；但重试必然复现——被判敏感的图**留在历史里**，Locus 每次请求都重发全部历史图片。
- **根因**：`read` 读 PNG / 截图回传把图片内联进消息后，上游（本例 MiniMax-M3 自定义端点，错误码 1026）的图片审核判定该图 sensitive。只要图在历史里，请求必被拒。
- **实锤（2026-10-23）**：
  - 会话 `6ec60f1e`（当时在做 `H:/Project/ChatRadio`，与游戏无关）：11:28:33 `read` 的截图通过，11:28:53 再 `read` 同一面板的第二个裁剪版（`Library/Locus/tmp/panel_merged.png`，211,429 B，sha1 `6262e602…`）→ 11:29:04 起连续 422 `messages[346]`。
  - 会话 `94a10895` 复现取证时，把同一张 PNG 重新 `read` 进自己的上下文（消息 46）→ 下一轮 11:35:49 报 422 `messages[48]`。**「把图读出来看看」这个排查动作本身就是中招动作**。
  - 全库 938 条带图消息里只有这 1 张图命中（2 条消息、2 个会话）。
- **取证**：`H:/locus/data/locus.db` → `messages.images`（JSON 数组，`data` 是 base64；用 `length(data)`+sha1 比对，别把图读进对话）；`session_events` 里只有 `event_type='error'` 的才是真失败，`userMessage` 载荷也会带上粘贴的报错文本，只按 LIKE 计数会被骗。`session_context_attempts` 本机为空，拿不到真实请求体。
- **恢复**：首选换新会话 + 交接摘要（同问题 7/8）。要把旧会话救回来，就只摘掉那条消息的附件：`UPDATE messages SET images=NULL WHERE id='<该条 tool 消息 id>'`（文本结果保留，tool_call/tool_result 配对不破；不要用 `include_in_prompt=0` 整条禁用，会打断配对）。改前先把 `images` 备份成文件。
- **预防**：读图前先掂量内容来源——聊天窗、真人照片、别人给的界面截图、含大量文字的界面最容易被判敏感；一旦某张图被 422 拒，**不要在同一会话重试**，立刻换会话并写交接。

## 20. 2026-11-05 手感与交互反馈 5 条（**已分析，已实装第一批，待实机验收**）
- **来源**：用户当面反馈（战斗/教学/三选一三条线）——① 需要动作游戏的**预输入**；② 教程**不应该点任意处都能继续**；③ 当前招式**不跟手、有延迟感**；④ 三选一应**长按选择**而不是双击；⑤ 三选一应**波次结束时弹出**而不是升级立刻弹出（避免体验断档）。
- **状态**：用户明确要求「先分析、不立刻改」→ 只写了分析文档，**没动任何代码**。逐条根因、改法方向、代价与验收口径见 **`plan/input-and-choice-ux-feedback-20261105.md`**。
- **三个已查实的关键事实（改之前先看）**：
  1. **「按下即出轻攻击」是死代码**：`AttackSystem.TryCancelLightAttackStartup()` 与 `StabSweepEffect.TryCancelBeforeHit()` 都在，但**全工程零调用点** → 招式跟手的现成脚手架已经在了。
  2. **「波末弹三选一」的架子也已经在了**：`StageRunFlow.OnWaveCleared` 已有「有待处理选择 → 等选完再出下一波」的逻辑，只是升级总是提前把弹窗弹了 → 改造成本比想象低。
  3. **三选一确实是两次点击**：`UpgradeChoicePopup.OnCardClicked` 第一次点只选中、第二次点才确认。
- **风险提示**：改 #5（三选一时机）与 #1/#3（预输入、窗口提前）都会**改变战斗节奏** → 之前标定的难度表/铁律八要重跑。
- **2026-11-05 晚：手感改造第一批已落地**（代码已合入，编译 0 error，**待实机验收**）：
  - **输入缓冲升级**（`PlayerMoveStateMachine`）：有效期 0.25s、跨招式兑现、无匹配边的手势也记、蓄力不进缓冲。
  - **命中取消**：打中后允许在“伤害窗口结束”处提前接下一招（`hitCancelEnabled`）；命中信号取自 Stab/Slash 的 `onFirstHit` 回调（**不是** `TryExecuteAttack` 的返回值）。
  - **卡肉对齐**：动作锁与招式时钟现在一起随卡肉冻结（修掉「动作锁先于视觉结束」的错位）。
  - **三选一改波末弹出** + **结算前先选完**；**三选一改长按选择**（按住 0.35s）。
  - **教程不再点任意处继续**（同时发现：气泡与「继续」按钮的 `raycastTarget` 原本是 false → 以前根本点不到，只能靠那条裸输入路径）。
  - **「按下即出」默认关**（`InputManager.pressTapDelay = 0`）：本项目“按住”本身就是蓄力手势，任何小于 0.3s 的按下即出窗口都会把「按住一下再划」拆成“一记轻攻击 + 划动被丢弃”。
  - 规范与开关表见 `design/action-game-feel-and-input-buffering.md` §0.5；落点与回归重点见 `design/combo-move-state-machine.md` §11.11。

## 21. 【已修 2026-11-05】未解锁 C2 时仍然进入 C2 的蓄力 / 0 前置的蓄力
- **现象**：天赋**没解锁** `talent_move_stabc2`（指向突刺）时，偶尔会进入 **C2 的蓄力**（枪停在回收段等蓄力 + 蓄力指示器出现，松手却没出手）；也会出现**没有前置招式的蓄力**（站桩蓄力复活）。
- **根因**：`PlayerMoveStateMachine` 里三处“能不能蓄力”的判定口径不一致：
  1. `BeginMove` 用 `HasChargeContinuation()`（**不看天赋锁**）→ 没解锁 C2 也进 C2 的蓄力；
  2. `BeginMove` / `UpdatePointerHold` 把 `comboChargeActive`（蓄力总闸）设成“只要有前置招式”→ 没有蓄力后继的节点（Jab3、斩击）也算有蓄力 → 0 前置的蓄力；
  3. 中立态的 `Hold` 手势回落到默认招式 = **穿刺** → 没有前置也能打出穿刺。
- **修法**：蓄力判定统一改用 `HasUsableChargeContinuation()`（有蓄力边 **且** 后继没被天赋锁住）；
  节点没有可用蓄力后继时**清掉** `comboChargeActive`；中立态**不执行 Hold 手势**。
- **实测**：Unity 内核实 `Zhangfei_StabR2` 确实被 `talent_move_stabc2` 锁住、`Zhangfei_Jab1.HasChargeContinuation()=true`（旧逻辑必然进蓄力）、`HasUsableChargeContinuation=false`（新逻辑不进）。
- **详见** `design/combo-move-state-machine.md` §18.1（第五条共享输入规则）。

## 22. 【已修 2026-11-05】教学需要出招的拍：防御键没提前锁 → 这一拍过不去
- **现象**：正式关的「招式解锁引导拍」（ComboCharge / 指向突刺）里，防御键在该锁的时候没锁 →
  玩家第一下点击错位 → 「点了没反应、无法继续」。
- **日志证据**（Editor.log）：`[Defend] 防御 开启` 出现在「教学条件满足」之后、「手势拍开始」之前。
- **根因**：① 防御键静音只在**开拍那一刻**生效 → 前面「等条件 + 台词 + 等敌人收招」是**没静音的空窗**；
  ② 静音态的防御键**仍然吃射线** → 玩家点在那一块 → 这一点被按钮吞掉 → 招式永远打不出来。
- **修法**：静音提前到「教学条件满足」那一刻（拍开不成/舞台结束时收回）；
  `DefenseButton.SetMuted(muted, swallowClicks)` —— 需要出招的拍**不吃射线**，三选一面板那段仍吃。
- **顺带**：`AttackSystem` 的两条静默丢弃路径（动作锁未结束 / 戳击视觉门未过）现在会打日志 → “我点了它没动”可查。
- **详见** `plan/bugs-and-requests.md` BUG-009。
- **深化方案（当日晚补）**：规范见 **`design/action-game-feel-and-input-buffering.md`**，实施工作包见 **`plan/feel-optimization-work-packages-20261105.md`**（W0~W8）。
- **第四个已查实的事实**：**当年撤回「按下即出轻攻击」的理由已经消失**。`design/combo-move-state-machine.md` §11.6 写的是「与起手即蓄力冲突」，而 2026-11-05 的 `2234ed95`（中立态彻底不许蓄力）已把那个冲突源删掉 → 轴 1 可以复活，且 `AttackSystem.TryCancelLightAttackStartup()` / `StabSweepEffect.TryCancelBeforeHit()` / `PlayerMoveStateMachine.NotifyExternalReset()` 三个预留接口**都还在、零调用点**。
