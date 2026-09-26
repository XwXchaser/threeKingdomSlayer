---
id: kd_88f24d6e-f055-43ca-9efd-25c26dcf7568
injectMode: inherit
aiEditMode: inherit
---

# 长坂坡路线视频与节点扩展交接待办

> 本轮后续更新：以下“最新验收状态”优先于后面的历史记录；历史故障保留供追溯。

## 当前接续状态：E12桥前已接入，下一段原地回头

本节优先于下方旧记录，以及旧准备文档中“E12仅平原／N6必在桥上”的假设。当前为交接记录，不自动修改Design或其他路线。

### 已部署与验收边界

- 实际路线：`N3→E3→EW→J3→E4→N4→E11→E12→N6`；最后E12→N6仍为占位转场。N5仍直接到N6，未改为汇入E11。
- `Assets/RouteData/FakeStage01/FakeStage01.asset`：15节点，configurationVersion=12，起点仍N1。旧版本检查点可能失效。
- J3→E4、E4→N4、N4夜→晨素材已获用户验收。N4背景覆盖修复后用户明确验收。
- N4战斗/选择背景均为空；E4toN4与N4_NightToDawn均保留尾帧。N4实际流程：夜景尾帧→战斗→奖励等待→postBattleDialogue→postBattlePresentation夜转晨→保留晨景→显示出口。保留尾帧时不再调用RestoreNodeBackground。
- N4测试战斗 `Battle_FakeStage01_N4_1.asset`：1波，10个配置排，仅第0排 `[0,101,101,101,0]`，其余9排全零。不是只有1个逻辑排，空排可能影响推进测试。
- N4→E11与E11→E12素材已用户验收并部署；节点注册、脚本绑定、引用、哈希和尾帧设置验证通过。未执行这两段完整Play Mode黑盒、Skip及存档恢复验收。
- E11为“下山出口”、E12为“长坂桥前”：均无战斗/奖励、无静态背景，单出口等待点击。视频holdLastFrameAsBackground=true。不要再配置旧静态background覆盖视频环境。
- 保留的旧出口ID与目标不同：N4的`n4_to_n6`现在指E11；E11的`e11_to_n6`现在指E12。查实际targetNode，不按ID推断。E12出口`e12_to_n6`指N6，标签“回头迎敌（转身演出待接入）”，仍用RouteChoiceBlack。

### 正式视频与任务

| 段落 | 用户验收本地文件（C:/Users/steam/Videos/doubaoVideo/下） | task_id | 项目正式文件（Assets/RouteData/FakeStage01/Presentations/Videos/下） |
|---|---|---|---|
| E4→N4 | E4toN4_v2_480p_6s.mp4 | task_f1dcdf8bc0bf46fb86b394876542c68a | E4toN4.mp4 |
| N4夜→晨 | N4_night_to_dawn_v1_480p_6s.mp4 | task_9eff62a2b1e743958f286c671e58d299 | N4_night_to_dawn_v1.mp4 |
| N4→E11 | N4toE11_v3_480p_10s.mp4 | task_a58cf732d6e74b50906d9275c5b5faa0 | N4toE11.mp4 |
| E11→E12 | E11toE12_v1_480p_10s.mp4 | task_23c7b1c0e29a424f878294247165a5a8 | E11toE12.mp4 |

- 以上实际视频均560×752、24fps。6秒版145帧/6.041667秒；10秒版241帧/10.041667秒。
- N4toE11正式SHA256：`ad67a9c89caf125334d85a07c6e81e32a9a4f100ce3683c4d8b1bfb146eec3f1`。
- E11toE12正式SHA256：`c9f2b12bc668d2f1c7a971e424e62ce97a524cc077c23a4144bcc00098f18de2`。
- 最近两段采用单张first_frame锁起点，480p/adaptive/无音频/无水印。目标图只提炼为文字，未上传。费用各0.555022（API未注明币种）；不要再创建这些已有任务。
- 请求及下载临时记录：`Library/Locus/tmp/n4_to_e11_v3_10s/`、`Library/Locus/tmp/e11_to_e12_v1_10s/`及对应`*_download/`。完整Prompt在request_sanitized.json；临时目录不保证永久存在。

### 下一段：E12→N6，只回头，不移动位置

用户最终要求：E12停在平原土路上，长坂桥在前方，尚未登桥。随后原地回头，面向刚走过的平原来路（曹军方向），不画任何人物或军队。N6场景应由此空间关系反推，不能用旧N6桥上构图污染生成。

1. 从正式 `Assets/RouteData/FakeStage01/Presentations/Videos/E11toE12.mp4` 完整解码提取实际最后一帧，并阅读图像。这才是下段首帧；不能用桥前概念图替代。下载目录内last_frame.png可供核对，但应验证来源。
2. 先制作原地180°转身后的目标参考图，再让用户验收。机位原地、相对地面高度/焦距不变；桥应转到镜头后方，画面是来路，不应仍在脚下看到木桥。不预设新朝向画面或用左右翻图冒充回头。
3. 可准备左右90°中途参考确认河岸/桥头/道路连续几何，不能凭转身前单图声称背面空间已确定。
4. 本段是旋转镜头任务，不能照搬赶路视频“禁止旋转”或“必须前进”的要求；不新增人物/敌军。时长和旋转方向尚未确认，不自动沿用10秒。
5. 接入时检查N6现存 `battleBackground=N6_Battle`（旧桥上军阵/夕阳图），其与新桥前回头语义冲突，不能恢复成旧背景。N6还有N5入边，直接进入/存档/视频失败的无尾帧边界需说明，不能把N5路径当作已经改好。

### 图像参考与弃用

- 图片目录：`C:/Users/steam/Pictures/gptGen/`。
- `N4_dawn_to_E11_first_actual.png`：夜→晨真实尾帧，560×752。
- `N4_to_E11_downhill_target_v2.png`：仅下山中途参考，不是山脚终点。
- `N4_to_E11_mountain_foot_target_v4.png`：用户验收的低处道路目标概念图。
- `E11_actual_tail_for_E12.png`：N4toE11正式视频真实尾帧，560×752，SHA256 `7114149ee8a9fba2b13531d18308b921fb8e953dc2ea7c9c404fec5c208b5734`。
- `E12_plain_road_target_v1.png`：位置错误（仅平原，无桥），否决。
- `E12_changban_bridge_before_turn_v1.png`：位置错误（已上桥），否决，禁用旧N6参考。
- `E12_plain_before_changban_bridge_v2.png`：桥前空间参考，但画风偏写实，否决为最终美术。
- `E12_plain_before_changban_bridge_pixel_v3.png`：重新按原始项目像素画风制作的桥前目标图，1024×1536；仅用作E11toE12文字设计参考，不等于成片尾帧。像素画风应单独用原始美术约束，不能仅使用视频截图逐代生成。

## 历史验收状态与接续入口（以下节点数/版本号以顶部为准）

- 4秒480p版N3→E3视频已完整下载、用户验收、导入并接入路线。正式文件：`Assets/RouteData/FakeStage01/Presentations/Videos/N3toE3.mp4`；表现：`Assets/RouteData/FakeStage01/Presentations/N3toE3.asset`。
- `E3→EW` 4秒480p视频已完成生成、完整下载、用户验收并部署。正式文件：`Assets/RouteData/FakeStage01/Presentations/Videos/E3toEW.mp4`；表现：`Assets/RouteData/FakeStage01/Presentations/E3toEW.asset`；任务 `task_a553daccef564d35b66602bd9d4308ed`，Unity实际读取约4.04秒。
- 当前实际拓扑已扩展为 `N3→E3→EW→J3`。EW资产：`Assets/RouteData/FakeStage01/RouteNode_FakeStage01_EW.asset`，已加入总配置，总节点12个，configurationVersion保持9。EW无战斗/奖励，单出口等待点击；没有创建任何节点Scene。
- `EW` 背景已部署：`Assets/Sprites/changbanpo/Backgrounds/changbanpo_EW_outer_woods_dusk_v1.png`，表现资产：`Assets/RouteData/FakeStage01/Presentations/EW_Background.asset`。其视觉职责是村外树林、蓝调暮色和时间推进。
- E3出口已改为 `e3_to_ew → EW` 并绑定 `E3toEW.asset`；EW唯一出口 `ew_to_j3 → J3`。路线资产已保存、重新导入，FakeRoute验证器报告 `validation passed`。
- 本次 `E3→EW` 视频用户确认效果良好：未出现大幅度瞬移、场景闪现或明显硬切。该结果来自拆分路线、首尾帧约束和连续空间运动提示词的组合，不代表所有视频生成任务天然稳定。
- 当前仍未完成完整Play Mode黑盒验收：需实际点击E3出口确认视频播放、尾帧承接EW背景、EW内容结束后显示唯一出口，并继续点击进入J3。
- 当前制作阶段：Blender空间灰模已完成，已统一视频机位为2:3并输出EW接近、汇合、共同道路、J3抵达四张参考图。汇合点与J3分叉点已拉开为长共同道路，分叉位于缓弯之后。
- Blender工程：`C:/blenderFiles/Route_SpatialProof_Clean_2x3.blend`；空间参考经验已整理至 `skill/workflows/route-spatial-reference.md`。
- `EW→J3` 视频已由用户裁剪拼接完成并验收。用户文件 `C:/Users/steam/Videos/9月13日.mp4` 已部署为 `Assets/RouteData/FakeStage01/Presentations/Videos/EWtoJ3.mp4`；表现资产：`Assets/RouteData/FakeStage01/Presentations/EWtoJ3.asset`；Unity实际读取约7.40秒。EW出口 `ew_to_j3` 已绑定该视频并指向J3。
- 15个既有节点原m_Script为空的问题已原位修复；Stage/Node脚本拆为同名独立文件，保留原节点GUID，Stage原脚本GUID随文件移动。编译/域重载后全部11个路线节点及出口引用验证有效。
- N3部署后仍配置N3_Battle导致战斗开始覆盖尾帧。后续已清空N3的battleBackground，routeChoiceBackground仍为空；保存并重新导入验证，用户已明确验收该修复。不要再把N3_Battle设回N3背景。
- N3toE3设置holdLastFrameAsBackground=true；E3背景为空，沿用视频尾帧。J3仍使用旧静态背景，黄昏连续性重做仍待办。
- 旧Skip风险记录已过时：当前代码非循环视频Skip采用12倍速快进；本轮未单独做该路径验收。
- 新视频及N3尾帧配置已获用户验收，不扩大为所有路线、跳过、存档恢复及新E3完整Play Mode流程均已验收。
- 技术操作入口：`Locus/knowledge/skill/workflows/fake-route-node-video-deployment.md`；生成/下载入口：`Locus/knowledge/skill/reachapi-seedance-video-generation-v2.md`。不修改原玩法设计。

### 本次4秒任务的完整已确认Prompt

原10秒任务全文未保存；以下是本次经用户检查确认后实际提交的版本，非旧版本逐字复用。

> A continuous cinematic forward-moving shot transitioning from the N3 village rescue battlefield to the E3 village supply yard. All objects must exist in one continuous 3D space. The camera steadily moves forward along the village road; use natural parallax, near objects appearing larger, and foreground side occlusion to gradually reveal the supply yard. Preserve the warm orange sunset, distant mountains, smoke column, pixel-art game background style, and visual continuity between the two reference frames. No objects may suddenly appear, fade in, teleport, morph, or pop into existence. No cuts. No camera jump. End on the E3 supply-yard composition.

首尾帧沿用下文N3 v2与E3 v1候选图；使用first_frame/last_frame，不混普通参考；模型seedance-2-fast、4秒、480p、adaptive、无音频/水印。

## 历史记录（以下待办勾选未逐项回填，请以上述最新状态为准）

## 当前完成项

### N3 尾帧候选图

- 已实际生成并验证：`C:/Users/steam/Pictures/gptGen/changbanpo_N3_village_rescue_battle_background_v2_candidate.png`
- PNG，1024×1536，约 2.55 MB。
- 画面已通过初步视觉检查：延续 EV 的暖橙黄昏、远山和右侧烟柱；中央为宽阔战斗空地，建筑/火光留在两侧。
- 尚未导入 Unity，也尚未替换原始 N3 静态背景资源。

### EV → N3 路线视频接入

- 用户提供视频已复制并命名为：`Assets/RouteData/FakeStage01/Presentations/Videos/EVtoN3.mp4`
- 已创建表现资产：`Assets/RouteData/FakeStage01/Presentations/EVtoN3.asset`
  - `mode = Video`
  - `videoClip = EVtoN3.mp4`
  - `skipAllowed = true`
  - `holdLastFrameAsBackground = true`
- 已绑定 EV 唯一出口：
  - 节点：`Assets/RouteData/FakeStage01/RouteNode_FakeStage01_EV.asset`
  - 出口：`ev_to_n3`
  - 目标：N3
  - `presentation = EVtoN3.asset`
- 为避免 N3 重新覆盖视频尾帧，已将：
  - `Assets/RouteData/FakeStage01/RouteNode_FakeStage01_N3.asset#battleBackground = None`
  - `Assets/RouteData/FakeStage01/RouteNode_FakeStage01_N3.asset#routeChoiceBackground = None`
- 该逻辑依赖 `FakeMovementPresenter`：转场视频在非跳过完成时保留最后一帧；N3 不再设置静态背景，因此 N3 的对话、战斗和出口选择应沿用视频尾帧。
- 尚未完成 Play Mode 黑盒验收。必须验证：从 EV 点击 `ev_to_n3` → 视频播放 → 非跳过结束 → `CurrentNode == N3` → N3 战斗/出口期间仍显示视频尾帧。
- 已知边界：若用户跳过视频，既有 `FakeMovementPresenter` 逻辑不会保留尾帧。需决定是否接受，或修改跳过逻辑使其跳至视频末帧。

### E3 → EW 视频与节点部署（已完成）

- 输入首帧：`C:/Users/steam/Pictures/gptGen/changbanpo_E3_village_supply_yard_v1_candidate.png`
- 输入尾帧：`C:/Users/steam/Pictures/gptGen/changbanpo_village_outer_woods_dusk_tail_v1.png`
- 任务：`task_a553daccef564d35b66602bd9d4308ed`
- 参数：`seedance-2-fast`、4秒、480p、adaptive、无音频、无水印、seed=731842。
- 本地文件：`C:/Users/steam/Videos/doubaoVideo/E3toOuterWoods_480p_4s.mp4`，最终大小2,715,939 bytes；下载首次超时后保留`.part`，使用断点续传完成；MP4顶层box验证为 `ftyp / uuid / free / mdat / moov`。
- 项目文件：`Assets/RouteData/FakeStage01/Presentations/Videos/E3toEW.mp4`。
- 用户验收结论：调整后的提示词有效，视频保持了首帧空间起点和连续向前运动，未出现大幅度瞬移、场景闪现或明显硬切。
- 重要边界：这是视频内容的用户视觉验收结论；尚未完成完整Play Mode路线黑盒验收。

### 本次有效的视频生成经验

1. 不要一次跨越“村内→树林→夜间分岔”多个空间阶段；把路线拆成 `E3→EW`、`EW→J3` 等短段，每段只完成一个空间目标。
2. 尾帧要求必须使用真实 `last_frame` 输入，而不是只在文字中描述目标画面；否则模型容易自行重绘终点或硬切。
3. 首尾帧比例必须一致。项目竖屏背景使用2:3；不要同时在提示词或参数中要求3:4，以免首帧被裁切/重构。
4. 不要写“前两秒完全保持静止”这类与持续移动冲突的要求。改为“开头短暂保留起始构图，随后以稳定速度向前推进”。
5. 明确写出物件的生命周期：现有车、粮袋、围栏、墙体和碎石通过视差从镜头两侧掠过并退到身后；树林必须作为远处已存在的环境被逐渐接近，不能从空白处生成。
6. 强调“first_frame和last_frame是空间锚点，不是交叉淡化、变形或贴图替换目标”，并同时禁止hard cut、crossfade、morph、pop-in、teleport、background replacement。
7. 镜头动作尽量单一：只沿同一道路向前，禁止转向、横移、镜头跳跃和背景平移伪移动。
8. 先用4秒480p验证空间连续性，再考虑更高分辨率或更长时长；这次低成本验证已得到可部署结果。
9. 下载必须使用`.part`和断点续传；服务端success不等于本地视频完成，需通过`ftyp`和顶层box检查后才可导入Unity。

### E3 村中补给前院候选图

- 新增节点概念：N3 与 J3 之间增加无战斗情景节点 E3（奖励/补给功能尚未定义、未实现）。
- 已实际生成并验证：`C:/Users/steam/Pictures/gptGen/changbanpo_E3_village_supply_yard_v1_candidate.png`
- PNG，1024×1536，约 2.59 MB。
- 场景语义：同一暖橙黄昏村内更深处的半开放粮仓前院；中央道路持续通向单一村后出口，左右才放补给物资。
- 关键构成：
  - 右侧：开放粮仓、粮袋、木箱、竹筐、木柴、受损木车；
  - 左侧：陶制水瓮、草药篮、布卷、破院门、低土墙；
  - 只保留克制战乱痕迹；无人、无文字、无奖励光效。
- 初步视觉检查通过：道路未被阻断，物资在两侧，画风和 N3 候选图一致。
- 尚未导入 Unity、未创建 E3 资产、未绑定路线。

## 推荐目标拓扑

```text
N3 村落搜救（战斗）
→ E3 村中补给（无战斗、单出口情景）
→ EW 村外林道（无战斗、单出口情景）
→ J3 救援／合围选择
├─ N4 山道救援
└─ N5 曹军合围
```

当前实际拓扑已为：`N3 → E3 → EW → J3`。E3负责村内线索，EW负责离开村落、进入树林和蓝调入夜；不要把EW奖励、补给或战斗逻辑自行扩展。

## 当前待办（按顺序）

### P0：验收已接入的 EV → N3

- [ ] 在 Play Mode 从 N1 路径进入 EV。
- [ ] 点击 EV 的唯一出口 `ev_to_n3`。
- [ ] 确认播放的是 `EVtoN3.mp4`，不是 `RouteChoiceBlack.asset`。
- [ ] 不跳过视频，确认结束后 N3 持续显示视频最后一帧。
- [ ] 确认 N3 战斗可正常开始、对话正常、敌人显示正常。
- [ ] 确认 N3 完成后路线选择 UI 正常。
- [ ] 退出 Play Mode 后确认不会保存任何运行时临时修改。

### P1：E3与EW已接入（不实现奖励）

- [x] E3已作为无战斗节点接入；其背景继续由N3→E3视频尾帧承担。
- [x] EW已创建为无战斗、无奖励、单出口情景节点，背景为村外树林蓝调暮色图。
- [x] E3出口为 `e3_to_ew → EW`，播放`E3toEW.asset`；EW出口为 `ew_to_j3 → J3`。
- [x] EW已加入 `Assets/RouteData/FakeStage01/FakeStage01.asset#nodes`，资产验证通过。
- [x] 在Play Mode完成E3→EW→J3点击、视频尾帧/背景承接和出口UI验收。EW→J3最终使用用户裁剪拼接视频，已确认部署后路线可用。

### P2：N3 → E3 视频（已完成）

- [x] 已生成、完整下载、校验并接入；正式文件：`Assets/RouteData/FakeStage01/Presentations/Videos/N3toE3.mp4`。

### P3：EW → J3 视频与节点部署（已完成）

- [x] 用户提供最终裁剪拼接视频并完成视觉验收。
- [x] 已部署为 `Assets/RouteData/FakeStage01/Presentations/Videos/EWtoJ3.mp4`。
- [x] 已创建并绑定 `Assets/RouteData/FakeStage01/Presentations/EWtoJ3.asset`。
- [x] `RouteNode_FakeStage01_EW.asset#outgoingChoices/0` 的 `choiceId=ew_to_j3`、目标为J3、表现为 `EWtoJ3.asset`。
- [x] Unity实时读取视频约7.40秒；用户已验收路线表现。
- 生成阶段的多次Seedance候选不作为正式资源；正式资源以用户裁剪拼接后的文件为准。

## Seedance 生成故障与严格操作规则

### 已生成但未成功落盘的 N3 → E3 视频

- 新任务（真正重新上传两个首尾帧 + 新种子）：`task_8584ab6a91104aafb2dd2fa16664a86e`
- 状态：`success`
- 服务端结果：10 秒，服务端 `cost.spend = 1.22229`，`usage.completion_tokens = 218266`。
- 该任务的 URL 在最后检查时带有：
  - `X-Tos-Date=20260911T145622Z`
  - `X-Tos-Expires=86400`
- 本地文件 `C:/Users/steam/Videos/doubaoVideo/N3toE3_village_supply_yard_v2.mp4` 不可用：曾在下载过程中取消后台任务，文件约12 MB但容器被截断，播放器无法打开。
- 不要把该文件导入 Unity；可删除或移动到外部废弃目录，但删除须先取得用户授权。

### 第一个失败任务

- 任务：`task_c68303fa6b1c40989ce0a6f9bb4783f0`
- 任务状态也是 success，但服务端返回的对象存储 URL 在下载时已经过期；不要再用该任务。

### 防止再次卡住/损坏的下载步骤

1. 不要再进行长期后台轮询。生成创建后，最多轮询固定次数/固定总时间；每次轮询任务都应能独立取消。
2. 只有得到 `success` 后才发起一次下载。
3. 下载命令必须有明确连接与总时限（例如 curl `--connect-timeout 20 --max-time 180`）；没有进度或超时就停止并报告。
4. 下载至同目录 `.part` 文件，绝不直接写最终 `.mp4`。
5. 下载完成后校验：
   - 大小 > 1KB；
   - 文件开头是 ISO MP4 `ftyp`；
   - 顶级 MP4 box 结构完整，`mdat` 声明长度不可超过实际文件大小；
   - 如本机安装可用媒体工具，再用播放器或 `ffprobe` 实测解码。
6. 仅在全部验证通过后，原子改名为最终 `.mp4`。
7. 如果对象存储 URL 过期、下载超时或校验失败：不要声称视频完成，不要接入 Unity，不要自动重新付费生成；先向用户报告。

### API 接入经验

- ReachAPI 图片上传：本机 Python `urllib` 对 `https://file.reachapi.ai/file/uploads` 曾收到 HTTP403/error1010；已验证 `curl -F` 上传成功。
- 两张 N3/E3 图上传都曾返回 `code=200`、`file_kind=image`、`mime_type=image/png`。
- 创建视频使用 `curl -X POST https://direct.reachapi.ai/v1/vids/create` 已成功。
- 使用 `seed=-1` 的创建请求曾异常命中陈旧缓存，瞬间 success 并返回过期 URL；如重新生成，应重新上传首尾帧且设置一个新的固定 seed。
- API Key 仅从环境变量 `REACH_API_KEY` 读取；不得写入仓库、文档、日志或回复。

## 相关项目资产

- EV 背景：`Assets/Sprites/changbanpo/Backgrounds/changbanpo_EV_village_approach_v3.png`
- 旧 N3 背景：`Assets/Sprites/changbanpo/Backgrounds/changbanpo_N3_village_rescue_battle_background_v1.png`
- N3 节点：`Assets/RouteData/FakeStage01/RouteNode_FakeStage01_N3.asset`
- EV 节点：`Assets/RouteData/FakeStage01/RouteNode_FakeStage01_EV.asset`
- J3 节点：`Assets/RouteData/FakeStage01/RouteNode_FakeStage01_J3.asset`
- 路线总配置：`Assets/RouteData/FakeStage01/FakeStage01.asset`
- 路线表现脚本：`Assets/Scripts/RouteFake/FakeMovementPresenter.cs`
- 路线运行脚本：`Assets/Scripts/RouteFake/FakeRouteRuntime.cs`
- 关卡/视觉需求：`Locus/knowledge/design/changbanpo-six-node-level-design.md`
- 视频 Skill：`Locus/knowledge/skill/reachapi-seedance-video-generation-v2.md`（v1 已于 2026-09 合入 v2）
- 图片 Skill：`Locus/knowledge/skill/gpt-image-generation.md`
