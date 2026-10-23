---
id: kd_c2de3458-57af-4257-9475-96d4662f67f2
injectMode: inherit
summary: 战斗已知问题清单（QTE 被打断、特效残留、BOSS 墙壁、超范围攻击等）与已修复项；含 Locus 会话上下文撑爆/自动压缩死锁的工具侧排查；带调试步骤的待办见 plan/unresolved-issues.md。
aiEditMode: inherit
---

# 已知问题

> 现象与根因清单；带调试步骤的待办见 `plan/unresolved-issues.md`。

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
