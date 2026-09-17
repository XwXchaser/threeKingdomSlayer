---
id: kd_94204135-afa7-4999-9188-935251d956ae
injectMode: inherit
summary: 需要把路线视频接入假移动路线节点并验收时序与尾帧表现时使用；含 ScriptableObject 绑定检查、背景覆盖根因与交付边界。
aiEditMode: inherit
skillEnabled: true
skillSurface: command
---

# 假移动路线节点与视频部署检查规程

仅规定技术部署与验收，不更改玩法设计。节点是 ScriptableObject，严禁为每个节点生成 Scene。遵循 `Locus/knowledge/design/route-fake-movement-architecture.md` 的路线规则及用户当次授权。

## 1. 检查当前状态，而不是直接照抄旧交接

- 先看 Git 改动，保留用户工作；读取目标节点、所有入边、表现资产及运行代码，再决定改动。
- 同一对节点之间可能存在两段方向相反的视频，且视频常挂在“入边的出口表现”上而不是节点本身，必须逐段确认归属。
- 节点语义（是否有战斗/奖励、出口数量、是否必须等待玩家点击）与“不为每个节点创建 Scene”的约束以当次设计文档为准。
- 既有部署状态、节点资产路径与验收记录见 `memory/video-generation-case-log.md`，不在本文重抄。

## 2. 防止资产型节点/配置的脚本绑定变成 None

- 磁盘 `m_Script` 为 `fileID=0` 时，内存对象仍可能暂时可读；**内存可读不能证明绑定能持久保存**，必须查磁盘。
- 一个脚本文件里放多个可序列化类型（例如节点类型与 Stage 类型共用同一 `.cs`）会放大该风险；每种资产型 ScriptableObject 应各自拥有独立同名脚本，并验证 `MonoScript.GetClass()` 是目标类型。
- 修复顺序：保留原 GUID 与 `.meta` 移动/拆分脚本 → 完整编译与域重载 → 若 Script 仍为 None，用 `SerializedObject` 设置 `m_Script` 后保存原资产 → 按原路径重新加载对象再 `SaveAssetIfDirty`（`ApplyModifiedPropertiesWithoutUndo` 可能让旧托管对象失效，不得继续对失效引用 `SetDirty`）。
- 不得用“删除重建”代替脚本修复；节点原 GUID、路径与战斗/对话/出口引用必须保持。
- 历史切片节点会被 Unity 补写现有类型的默认序列化字段，因此不能笼统断言所有文件只有一行差异。
- 涉及的数量与具体路径见 `memory/video-generation-case-log.md`。

部署前置检查：
1. 每个资产型ScriptableObject使用独立同名脚本，MonoScript.GetClass()必须是目标类型。
2. 磁盘/Editor出现None时暂停保存上层路线，先备份到 `Library/Locus/tmp/`，避免将失效引用固化为空。
3. 只用Unity API修改结构化资产，不手改YAML，不删除重建来替代脚本修复。
4. 创建新节点后检查m_Script非空且类型匹配；加入nodes并验证入边/出边。
5. 保存、强制重新导入、完成域重载，再确认起点、全部节点、出口目标仍有效。
6. `Tools/Fake Route/Validate Assets` 和 `Tools/Fake Route/Reimport And Validate Assets` 提供绑定、重复ID、目标注册及视频引用检查；它们是人工验收入口，不是自动拦截所有保存操作的保护器。

## 3. 视频尾帧被战斗背景覆盖的真实原因

当前 `FakeRouteRuntime.EnterNode()` 与 `RunBattleEntries()` 都会调用SetBattleBackground。Presenter的 `_heldFrameAsBackground` 仅对非空背景的第一次调用提供一次性保护，然后清标志；第二次战斗开始调用仍会套用静态图。

**规则**：只要目标节点配置了 `battleBackground` 或 `routeChoiceBackground`，即使视频表现 `holdLastFrameAsBackground=true`，进入战斗后尾帧仍会被静态图覆盖。不能只看“视频播完瞬间”或只核对 hold 标志就宣布通过；“目标节点背景覆盖检查”必须纳入视频接入清单。

- 采用“视频尾帧承担目标背景”时，节点需把 `battleBackground` 与 `routeChoiceBackground` 置 None，且只改配置、不改通用 Presenter 算法。
- 具体节点的配置与验收记录见 `memory/video-generation-case-log.md`，不在本文重抄。

注意：不是所有节点都能清空背景；采用“视频尾帧承担目标背景”是逐个节点确认的方案，不能笼统声称与其他节点逐字段一致。

当前Skip对非循环视频执行12倍速播放至结束，不是旧交接所述的立即丢弃尾帧；须按当前代码核查。尚未单独验收的跳过路径不得报告通过。

空背景不是全项目通用默认值：存档直接恢复、无前置视频进入、播放失败等路径可能没有可用尾帧。需要后续设计处理时单独确认，不擅自删除其他节点背景，也不掩盖一次性保护的现有限制。

## 4. 验收及交付

- 资产检查：节点可加载，GUID保持、脚本匹配、ID不重复、出口注册、视频引用正确、无额外Scene。
- 时序检查：点击出口→对应VideoPlayer.clip→播放完成→CurrentNode提交→对话→战斗→出口UI；尾帧不能在中间被静态图覆盖。
- 仅重载后引用有效，不等于 Play Mode 完整路线验收；未测试路径必须明确标记。
- 不直接改共享SO的startNode做测试；使用隔离配置或运行时状态，并检查退出后无测试污染。
- 事故记录：目标节点静态背景清空前曾被误报为“部署完成”；因此“目标节点背景覆盖检查”必须纳入视频接入清单。
- 更新交接记录时区分已生成、已完整下载、已导入、已配置、已重载验证、已用户验收，不能将其中一项等同全部完成。
