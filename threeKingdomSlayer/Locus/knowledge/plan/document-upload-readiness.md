---
id: kd_ce13d75a-8543-4f9e-b0a0-dd691b6eda64
injectMode: inherit
aiEditMode: inherit
---

# 文档上传准备与对齐检查

## Git 基线

- 当前分支：`route-fake-movement`。
- 本地 HEAD：`4c888dd4`（feat: complete fake route progression flow）。
- 远端实时查询：`origin/route-fake-movement` 为 `c017a25295f9dbe59762cd7599669c4b71247622`。
- 本地领先 1 个提交，该提交涉及 125 个文件，包含代码、场景、视频与文档；直接推送本分支不会是纯文档上传。
- 暂存区为空。本次检查未暂存、提交或推送，未修改既有知识文档及 Unity 数据。

## 文档工作树范围

检查时 `Locus/knowledge/` 有 51 个路径变更：11 修改、17 删除、23 新增（本清单不计入）。

主要变化：
- 开发计划、待办、包体优化移入 `plan/`。
- Android 状态、非战斗节点经验、路线失败复盘移入 `memory/`。
- 调试参数移入 `reference/`；HUD 实施记录移入 memory 子目录。
- UI 四篇文档合并为 `skill/unity-ui/patterns.md`。
- 视频生成流程升级为 v2，并新增角色动画、路线空间参考、部署等 workflow。
- 三选一旧短文删除，引用转向 V2 权威文档，UI 冷却约定补入 V2。

自动扫描现有 Markdown 中明确的 `design/`、`memory/`、`plan/`、`reference/`、`skill/` 文档路径，未发现缺失目标。不代表裸文件名、外部绝对路径或全部资产引用均有效。

## 对齐结果与上传前待办

### 优先修订

1. `design/route-fake-movement-architecture.md`：第 40、53 行仍写 N3→J3，第 49 行仍写正式选择 UI 未替换 OnGUI。当前 Editor 实际拓扑为 N3→E3→EW→J3，FakeRouteRuntime 已绑定 `Assets/Prefabs/UI/RouteChoicePanel_Test.prefab`；代码调用 RouteChoicePanel.Show。应更新现状，区分测试美术与已实现的 UI 技术路径。
2. `memory/video-generation-case-log.md`：第 117 行仍写 E3 出口 e3_to_j3、N3→E3→J3。实际资产为 e3_to_ew，随后 ew_to_j3；应改成当前状态，或明确标为历史阶段。
3. `plan/changbanpo-route-video-handoff.md`：最新状态区同时出现 12 个节点与 11 个路线节点。Editor 当前读取为 12 个，configurationVersion=9。11 个应注明属于 EW 加入前阶段。

### 建议澄清

- `design/route-stage-gameplay-spec.md` 开头仍以 RouteStageRoot、Head/Combat/Tail 为当前设计。假移动权威文档已有优先级覆盖声明，但旧文档自身缺少醒目的历史版本提示，独立阅读容易误用。
- `plan/audio-development-plan.md` 正文已注明 Wwise 阶段废弃，但 summary 仍表现为当前开发计划；建议同步摘要。
- `design/changbanpo-six-node-level-design.md` 已补 N3→E3→EW→J3；EW→J3 表现表仍侧重制作方案/被否决测试版本，可补充最终用户拼接版已部署的现状，避免把否决结论误用到当前视频。
- `memory/locus-knowledge-layer-mechanics.md` 属于历史工具实测，当前工具契约已描述 skill 检索支持；上传前应重新验证并限定其版本适用范围，不能将旧实测直接作为永久规则。

### 已对齐的抽样

- 新增 V2 冷却公式 `1 - timer / interval` 与 BuffDisplayPanel.cs 第 399 行一致；本次未逐项验证 Prefab 的全部填充参数。
- Stage/Node 脚本已拆为同名文件，与部署记录吻合。
- 当前 Editor 路线为 12 节点：N1→E1→J1，J1→EV/N2，EV→N3→E3→EW→J3，J3→N4/N5，N2→N4，N4/N5→N6。
- Skip 的 12 倍速表述与 FakeMovementPresenter.cs 第 316 行一致。

这是重点变更抽查，不是全知识库逐条实现审计，也未执行 Play Mode 黑盒验收。

## 上传范围与流程

1. 先确认本次是“当前分支整体同步”，还是“独立纯文档提交/分支”。后者不能直接推送当前分支来达到。
2. 修订上述现状冲突需获得对应 Design/Memory/Skill 文档的编辑批准；不因上传准备自动改写既有结论。
3. 逐项审阅合并删除：UI 四篇精简合并、Seedance v2 替代和三选一短文合并，避免把有意精简误报为文件丢失，也避免未经内容复核就声称无损迁移。
4. 仅纳入 `Locus/knowledge/` 目标文档（包括迁移的旧路径删除）；排除工作区现有 Assets 改动、`Locus/workspace-trees/`、`nul` 和临时文件。不要使用全仓库 git add。
5. 上传前做文档敏感信息检查：生成台账含本机绝对路径、任务 ID、费用及文件哈希；不得把这些当作已包含在仓库的源素材。API 凭据需单独扫描，本次未完成凭据专项审计。
6. 暂存后检查重命名/删除、新增清单与 diff --check，再生成提交说明。提交与远端推送前取得确认。

建议提交标题：`docs: reorganize knowledge and align fake-route handoff`。
