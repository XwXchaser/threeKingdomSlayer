---
id: kd_ba079271-c302-4d4a-9e7b-4af1a2395764
injectMode: inherit
aiEditMode: inherit
---

# 弯曲卷轴旅行实验：执行与交接

## 当前状态

- 用户已授权创建第三分支并开始独立实验；当前分支 `experiment/curved-scroll-travel`，未提交或推送。
- 第一版隔离原型已实现：`Assets/Experiments/CurvedScroll/CurvedScrollLab.unity`，脚本、Shader、材质均在同一实验目录。正式 Battle 场景、共享敌人 Prefab 与材质不修改。
- 三模式：平直、曲率、曲率+占位远景；40张静态敌人展示纸片、细分道路、暂停/单步/重置/进度拖动、定距离到站。当前是二次下弯，不是完整物理圆柱。
- 已完成全编译，运行 Console 无错误。T01/T02/T03/T14 仅初步9:16截图观察；T06对象数量检查通过（1161.825单位超过10轮，40纸片/42子对象恒定）；T07仅零曲率/平直区检查；T08暂停与timeScale=0各10帧冻结；T09模拟10次60单位到站，累计600、通知计数10。
- 未完成：动态连续视觉验收、动画脚底、多分辨率/安全区、完整极值测试、Profiler与目标设备性能、真实战斗/路线测试。不得把局部检查写成T01–T18全部通过。
- 操作说明与边界：`Assets/Experiments/CurvedScroll/README.md`。当前使用独立IMGUI和占位远景，未接正式HUD；sprite/anchor/seed在初始化读取，修改后需重新开始Play Mode。

- 当前进度与后续交接总文档：`Locus/knowledge/plan/curved-scroll-development-handoff.md`。本文件替代此前分散的实验进度描述，进入新对话先阅读它并重新核对工具现场。
- 当前代码基线仍以旧 V2 路线为主；FakeRoute 运行器尚未存在，本轮未伪装成完整路线运行已完成。
- 新增逻辑资产契约：`Assets/Scripts/RouteFake/FakeRouteStageConfig.cs`，包含 FakeRouteStageConfig、FakeRouteNodeConfig、FakeRouteBattleEntry、FakeRouteChoiceConfig、FakeRouteTravelPresentation。
- 新增存档模型：`Assets/Scripts/RouteFake/FakeRouteStageSaveSnapshot.cs`。新增 SaveData.fakeRouteSnapshots 及 SaveManager 的按 routeId/stageId 读写/清理 API。旧 `routeStageSnapshots` 保留，两个架构不互读。
- 新增 `FakeRouteGraphValidator`：检查稳定 ID、起点、出口目标、旅行表现、终点无出口、自环和从起点可达回环；目标是保证同一局不返回同一节点（DAG）。
- 存档安全边界：只在新节点提交后、目标战斗启动前或节点内容/奖励/选项完成后保存。保存玩家生命、复活、等级、经验、被动升级、主动技能等级、限次道具、checkpointNodeId、visited/completed 节点、选择历史、BattleEntry 完成索引、routeId、stageId、configurationVersion、snapshotVersion、架构 ID。
- 明确不保存：卷轴/旅行中间帧、未提交选择、当前波次、场上敌人、投射物、攻击/QTE、奖励弹窗中间态、临时战斗效果。失败恢复从最近安全 checkpoint 重建。
- 现阶段验证：完整编译通过；FakeRoute 类型可编译；验证器能拒绝终点作为起点；独立快照 Save/Get/Clear 通过；测试快照已立即清理，未污染持久存档。
- 未完成：FakeRouteRuntime、节点一次性访问运行时、实际分支 UI、卷轴 Presenter 回调接入、玩家主动技能/道具快照恢复、版本不匹配处理和完整 Play Mode 路线验收。

- 新增独立测试场景：`Assets/Experiments/CurvedScroll/FakeRouteDataTrial.unity`，使用 `RouteTrialData/ValleyBranchTrial.asset`，拓扑为 `A_Start → B_Left/C_Right → D_End`。
- 测试面板支持：完成当前节点、选择分支、手动保存、读取存档、重启；路线旅行使用不同 duration/distance 的旅行表现资产，并驱动山谷卷轴实验距离。
- 测试案例：A完成后可选B或C；选择后到达目标节点并自动存档；再次选择已访问目标会被拒绝；存档记录 checkpoint、visited、completed 和 choice history；D为无出口终点。
- 已修复测试资产发现的序列化问题：FakeRoute资产类型拆成独立同名脚本，避免多类型同文件导致 `m_Script=None` 和出口引用丢失。测试资产已重建，图验证通过。
- Play Mode验证通过：A→B旅行完成，`checkpoint=B_Left`、`visited=2`；从B尝试回访A/B被拒绝；保存快照存在。编译通过、Console无错误。
- 当前测试只验证路线数据、旅行表现回调和存档边界，不启动真实BattleEntry战斗；FakeRouteRuntime正式接入仍未完成。

1. 评审需求：确认独立实验、曲率方向、第一阶段视觉标准，以及是否最终接入旧 V2 或 FakeRoute。FakeRoute 的表现空间运算例外必须另行批准，不能静默改现行约束。
2. 获实现授权后建立隔离试验：固定远景、细分道路、现有敌人展示纸片、最小调试 UI；三模式对比并记录参数。
3. 执行需求文档第一阶段用例，提交实际截图/录屏和性能数据，等待视觉评审。
4. 获集成授权后验证真实战斗衔接、暂停、奖励、Boss、QTE、预览交接及退出清理。
5. 最后决定是否做循环走廊、出口转向、背景切换和正式节点集成。

## 文档同步记录

- 分支：`route-scene-v2-baseline`；本地 HEAD 保持 `2846123`，没有 pull/merge/reset。
- 执行 `git fetch origin route-scene-v2-baseline route-fake-movement` 成功。
- 已验证场景远端包含 `c61104f3`，视频远端包含 `c31868a2`；当次 tip 即这两个提交。
- 两个远端的 knowledge tree 均为 `432b7671c58f0f4f3e782882b9d8d7671283e5a5`。
- 按用户最新要求只恢复 `Locus/knowledge/` 工作树，未合并代码、场景、资源，未改暂存区。
- 首次恢复遇 partial clone 对象读取失败及自动维护 pack unlink 警告；仅对重试调用禁用自动维护后恢复成功，没有改全局 Git 配置或清理 pack。
- 同步后逐文件 Git blob 校验：远端107个文件全部匹配。相对本地 HEAD，远端删除/迁出的16个旧路径全部不存在。
- 唯一本机未提交文档 `Locus/knowledge/design/route-scene-blender-production-and-deployment.md` 不在远端树，未覆盖、未删除；与远端无同路径冲突，但内容属于旧 V2，不能作为 FakeRoute 现行约束。
- 备份：`Library/Locus/tmp/knowledge-sync-backup-202609/route-scene-blender-production-and-deployment.md`；原件与备份 Git blob hash 均为 `f3d08e11a53bb2ece1b95d515c01c9054e62e570`。
- `ProjectSettings/ProjectSettings.asset`、`Locus/workspace-trees/` 原有改动保留。本轮新需求/本交接是远端基线之外的授权新增文档。

## 网络诊断记录

首次 fetch 失败原因为 SSH 解析 github.com 失败，不能据此推断凭据或代理错误。后续实测：
- origin：`git@github.com:XwXchaser/threeKingdomSlayer.git`。
- DNS 返回 `20.205.243.166`；HTTPS 经 `127.0.0.1:7897` 代理返回200。
- HTTP_PROXY / HTTPS_PROXY 指向该本地代理；未观察到 ALL_PROXY，Git代理配置查询无条目。
- SSH目标为 github.com:22，无输出 ProxyCommand/ProxyJump，身份认证成功，分支 fetch 成功。
- 未修改系统 DNS、全局代理或 SSH 配置。GitHub不提供shell导致 `ssh -T` 非零退出不能当作认证失败。

## 目录职责

Design 放需求、设计方向和约束；Memory 放经验、状态与长期上下文；Plan 放执行计划、待办、交接和进度；Reference 放参考资料；Skill 放可复用操作规范。`skill/workflows/` 是 Skill 的多步骤流程子目录，不是顶级知识类型；不创建 `knowledge/workflow/`。

## 当前主机 Skill 发现实测

已列出实际目录，并读取分析纪律、架构约束、UI模式、Seedance v2、图片生成流程、角色动画流程、路线视频部署及空间参考相关正文/frontmatter。扫描全部13篇项目 Skill：summary均非空、skillEnabled均为true、skillSurface均为command；别名为 `/anti-loop`、`/gpt-image`，其余按文件名。

### 命令注册：通过清单验证

加载当前 `skill_list` 工具并执行 `source=project`，返回13/13篇，包括：
- `skill/unity-ui/patterns.md` → `/patterns`
- `skill/reachapi-seedance-video-generation-v2.md` → 同名命令
- `skill/workflows/image-asset-generation.md`
- `skill/workflows/character-hit-animation-video-workflow.md`
- `skill/workflows/fake-route-node-video-deployment.md`
- `skill/workflows/route-spatial-reference.md`
- 其余分析、架构、图片API、反循环、Android、像素特效及UI配置Skill。

这证明清单发现和命令映射注册，不等于逐个点击客户端斜杠菜单或实际执行外部生成工作流；本次没有收费生成或资源部署。

### 知识检索：项目 Skill 未命中

当前工具契约允许 skill/，但当次查询实际结果为：

| lexicalQuery | pathPrefix | 结果 |
|---|---|---|
| 程序化 UI | skill/ | 仅返回App工具文档，未命中项目 `skill/unity-ui/patterns.md` |
| patterns | skill/unity-ui/ | No results |
| Blender | skill/workflows/ | No results，未命中正文含该词的 `route-spatial-reference.md` |
| MUSK_API_KEY | skill/ | No results，未命中图片技术/流程文档 |
| 幽灵引用 | design/ | 正常命中 `design/anti-ghost-reference.md`（对照） |

因此不能报告“检索/命令全部正常”，也不能把历史文档推广为 Skill 永久不可检索。当前证据是项目 Skill 检索与命令注册表现不一致，具体原因未定位；未擅自改写 Skill 配置或 `memory/locus-knowledge-layer-mechanics.md`，需单独排查主机检索索引/范围。

## 交付检查

- [x] 远端分支及同步提交验证。
- [x] 本机文档备份与远端内容逐文件校验。
- [x] 旧路径删除校验，未恢复旧副本。
- [x] Skill frontmatter与相关正文阅读、检索与注册分别实测。
- [x] 新增需求方向、参数清单、18项测试用例，区分两路线方案边界。
- [ ] 用户评审需求。
- [ ] 工程实施授权与测试执行。
