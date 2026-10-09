---
id: kd_4de6b58d-723d-483d-88d1-fd6265a31a9f
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 当前卷轴化场景 TodoList：N1→E0→E1→J1

> 本清单根据本轮用户讨论整理，作为卷轴场景制作的专项执行入口。总里程碑仍以 `plan/october-milestone-plan.md` 为准；不向已停用的 `plan/latest-todolist.md` 追加。
> 最新进展：用户已指出 E0 不能作为独立展示场景，并明确授权按关卡设计实现 N1→E0→E1→J1。已将 E0 门垣与门前侧带整合到正式 `YJunctionSample.unity`，并将分叉后移至 J1；`BattleYRouteHost` 现在具有 E0/E1 单出口停点流程。阶段交接见 `plan/e0-approach-scene-refinement-handoff.md`。E1/J1专用美术、完整自然奖励/确认/旅行流程、门内深度和最终验收未完成。

## 1. 已确认的方向与现有证据

- [x] 节点顺序已由用户明确确认：**N1→E0→E1→J1**。本清单不采用旧白盒实验的 N0→J0→N1。
- [x] 用户提供的场景母图已读取：`C:/Users/Administrator/Downloads/n1_battlefield_reference_retry_20261007_011121.png`，1024×2048、RGB、1:2竖屏。以下称“场景母图”，不因之前口头提到N0而改节点ID。
- [x] 先推导完整场景参考图，再拆分制作所需素材；当前目标不是单独量产城墙。
- [x] 用户已亲自试摆并确认城墙在当前位置可以观测。保留其对象与布局，作为体量/可见性验证依据，不等于E0整体构图已验收。
- [x] 视觉行进距离可以为构图服务，**各段行进时间保持一致**。本轮正式 Host 的开场/故事停点/分支旅行字段均按3秒目标配置；完整自然流程仍待回归。
- [x] 已读取图片生成工作流与新画风规范；已向用户提交R1/R2完整中英文提示词、负面约束、参考图职责和参数，R1获明确批准并生成v2候选。上一版 `plan/n1-e0-wall-generation-prompts.md` 继续暂缓，不按六件城墙方案自动生成。
- [x] 用户确认E0为外围残门垣，左侧插旗；门垣与侧带已复制进正式 Y 路线层，正式 Y 重载回读 scenery=267、无展示敌人引用；独立预览仍保留作源与对照。

### 连续场景工作假设

```text
N1：开阔黄土战场
→ N1→E0：沿原道路接近城防影响范围
→ E0：残城门外的战后余烬停留点
→ E0→E1：穿过门垣，视野重新打开
→ E1：门后残军集结的城郊空地
→ E1→J1：出现官道和村落方向的环境线索
→ J1：官道 / 村落路线选择
```

用户已确认E0是外围残门垣、左侧插旗，并认可R1 v2候选的整体构图；v3已按反馈将正面木质门框改为石质，用户已验收通过，不重新质疑已认可的门垣距离和体量。文件命名仍沿用R1，不自行改为R2或据此勾选R2完成；独立E0试摆不作为正式构图依据，E1/J1与完整连续空间仍待评审。

## 2. P0：确定内容与空间连续性

**目标：先确定E0是什么，再决定生成哪些城墙素材。**

- [x] **P0-01 确认E0建筑语义。** 用户明确选择外围残门垣，左侧插有旗帜；无需再询问主城门/外围门垣。
- [x] **P0-02 定N1→E0的空间变化。** 正式 Y 路线保留 N1 尾段，E0门垣位于路线距离约90–132的远/中段，地面和相机连续；木构侧带、土垒、破车、散盾向石质门垣过渡。实际显露节奏仍待用户美术验收。
- [x] **P0-03 定E0停留构图。** E0门垣和侧带已进入正式连续层，E0 story stop锚点位于路线距离90，E1位于156，J1位于190；中央通道保持开放。E0门外画面已采样，最终审美仍待确认。
- [ ] **P0-04 定E0的叙事痕迹。** 评审破车、卸空辎重、粮袋/木箱、散盾断枪、墙根瓦砾、焦木、车辙、少量余烬和淡烟的取舍；避免简单把已有N1物件全部加密。
- [ ] **P0-05 定E1内容与门后承接。** 残军集结、辎重停靠、伤员/残兵报告和下一段信息分别由环境或独立角色/UI承担；参考图阶段不把人物烘进环境。
- [ ] **P0-06 定J1两条出口的环境语义。** 以现有长坂坡设计的“中央官道、左侧村落支路”为初稿，评审道路开口、村落/辎重线索与选择UI位置；不以图集顺序或配置数组顺序推断画面方向。
- [ ] **P0-07 记录旅行时间基线。** 在以后授权接线时读取实际生效的每段时长与进度曲线；当前测试Host默认值、旧视频长度和Y路线Length都不自动等于正式N1→J1基线。

**通过门槛：** 用户确认E0场景职责、门前/门后关系与连续路径；场景内容不会靠突然换光照、换镜头或大烟幕来掩盖空间跳跃。

## 3. P1：从场景母图逐张推导参考图

**顺序：母图→接近图→E0→E1→J1；上一张验收后再推进下一张。**

| 编号 | 参考图 | 需要解决的问题 | 当前状态 |
|---|---|---|---|
| R0 | 用户提供的N1场景母图 | 基准镜头、路面、远山、用色与两侧布局 | 已读取，作为起点 |
| R1 | N1→E0接近图 | 城墙如何从现有开阔战场自然出现 | v3石砌门框修订已生成、附图并获用户验收；作为批次A建筑拆分基准 |
| R2 | E0门外停留图 | 城门、墙体、战后残骸与中央通道的完整构图 | 待R1验收后制作 |
| R3 | E1门后集结图 | 同一路面穿门后如何进入开阔城郊 | 待R2验收后制作 |
| R4 | J1分岔图 | 官道与村落支路从环境上可读 | 待R3验收后制作 |

- [x] **P1-01 编制R1完整中英文提示词。** 已在当前对话提交负面词、参考图分工、1024×2048/high/input_fidelity high等参数；用户明确批准R1生成，并在修复API后再次授权提交。
- [x] **P1-02 获批准后生成R1并交用户看图。** v1返回401、未产图；v2仅提交一次，HTTP200，PNG/请求/响应已保存并回读，实际1024×2048 RGB。图片已附给用户；此项只标记生成与交图完成，不代表用户验收通过。
- [ ] **P1-03 逐张准备、批准、生成与验收R2/R3/R4。** 以上一张为主要空间参考，R0辅助统一用色与画风；不能分别自由文生图后强行拼接。
- [ ] **P1-04 制作相邻参考图对照。** 检查道路边界、地貌、门墙、远山、光向和残骸的接续；变化须对应向前接近或已讨论的转向。
- [ ] **P1-05 按需补门洞中段空间参考。** 如果R2/R3不能说明穿门深度，增加一个门内采样参考；不因此自动启动完整穿门系统。

### R1 v2 产物与验收边界

- 图片：`C:/Users/Administrator/Pictures/gptGen/changbanpo_e0_reference_workflow/r1_n1_to_e0_approach_v2.png`。
- 请求：`C:/Users/Administrator/Pictures/gptGen/changbanpo_e0_reference_workflow/r1_n1_to_e0_approach_v2.request.json`；响应同目录 `r1_n1_to_e0_approach_v2.response.json`。v1的401记录保留，未覆盖。
- 参数：`POST /v1/images/edits`，`gpt-image-2.5-sunburst`，quality=high，input_fidelity=high，size=1024x2048，PNG，n=1，不请求透明。按image[]先上传N1母图，再上传战场概念图14，后者只辅助画风。
- 原图3430843 bytes，SHA-256=`098b8d420830af36f1de40ee72a4c196ff4006e7e0d3b0ebadb5c795478d6128`；Pillow已加载验证，PNG Color Type=2。v2主prompt、参考图与生成参数与获批v1一致。
- 视觉回读：N1蓝天/远山/中央黄土路仍可辨认；中景出现左右残墙、中央通道和左侧残旗，无可见人物或HUD。用户认可整体构图，并要求将正面木质门框改为石质；v3修订已获用户验收。此前“门垣是否过大”的AI疑虑不作为未决用户意见。
- 本次没有导入Assets或修改Unity场景，没有生成R2。v3通过后，批次A生成词已独立整理；未经用户批准，不自动发起A01收费请求。

### R1 v3 石砌门框修订

- 图片：`C:/Users/Administrator/Pictures/gptGen/changbanpo_e0_reference_workflow/r1_n1_to_e0_approach_v3.png`。
- 请求：`C:/Users/Administrator/Pictures/gptGen/changbanpo_e0_reference_workflow/r1_n1_to_e0_approach_v3.request.json`；响应：`C:/Users/Administrator/Pictures/gptGen/changbanpo_e0_reference_workflow/r1_n1_to_e0_approach_v3.response.json`。v2原文件哈希已再次核对，未改变。
- 仅上传已认可的v2单图；提交一次 `/v1/images/edits`，模型/quality/input_fidelity/尺寸保持sunburst/high/high/1024x2048，PNG，n=1，不请求透明，无自动重试。完整中英文修订词保存在请求记录。
- HTTP200，1张结果；实际PNG 1024×2048 RGB、Color Type2，3216948 bytes，SHA-256=`44eb18f47a622e58f20e9c7fceafde20dad059e96ec5b5ad0cf1fc25ab1b923a`。请求、响应、图片都已重新回读验证，已通过read附加关键图。
- 视觉回读：中央木立柱/横梁已换为灰褐石砌门墩和浅石拱，开放通道保留；左旗、远山及两侧布局基本延续。路面纹理等有少量重绘，不声称其他像素完全不变。用户已验收v3，当前将其作为批次A建筑形体基准。
- 本轮未操作Unity、未导入Assets、未继续R2或物件生成。

### 参考图共同约束

- 沿用场景母图的1:2竖屏取景与高清像素化战场语言，不擅自换成2:3海报。
- 沿用蓝天、冷灰远山、暖黄土地和当前光照阶段，不突然切到黄昏或阴暗窄巷。
- 中央通道连续、开阔；大型建筑优先在中远层或两侧，E0不把下半幅挤满。
- 完整参考图为场景构图用途，可使用不透明RGB PNG；它不是最终透明Sprite，不直接从整张概念图裁切后部署。
- 不生成玩家身体、手臂、武器、敌阵、HUD、文字或后世阵营标识；叙事人物与动态烟火另层制作。
- 只在用户明确批准本次提示词、参考图职责与参数后调用生成接口。

**通过门槛：** R1/R2先获得用户目视验收；其余逐张推进。高分辨率细节多、HTTP成功或文件存在，都不能代替空间连续性验收。

## 4. P2：依据E0参考图拆素材，再制作

**依赖：E0整体参考图通过后再排产，旧六件城墙清单不是必做配额。**

- [ ] **P2-01 建立E0资产拆分表。** 每项记录用途、对应参考图位置、近/中/远层、尺寸类型、静态/动态、已有可复用源与缺口。
- [ ] **P2-02 评审建筑模块。** 按实际构图决定城门外立面、左右连接墙、破墙端部、墙墩/门楼的拆分数量；不预先固定六件全部重做。
- [ ] **P2-03 评审战后余烬模块。** 按需复用或补充瓦砾、塌梁、辎重、散落物、残旗；烟尘/余烬独立控制，不烘进墙图。
- [ ] **P2-04 分类制作规格。** 道路/地面为可平铺纹理，建筑/路侧物为独立透明Sprite；穿门内墙、拱顶内衬和出口是半3D空间素材，不能等同普通billboard。
- [ ] **P2-05 为实际缺口编制并审批生成词。** 以已验收E0参考图定义内容与用色，以 `design/art-style-guide.md` 定画风，以已部署环境规格定导入方式。
- [ ] **P2-06 分批生成、验收、裁切。** 保留原始图和响应；每个模块检查真实Alpha、浅/深/土色底、完整轮廓、透明留边、脚点和门洞孔洞。API回执不等于成品。

**通过门槛：** 素材覆盖E0构图职责、同一家族且可独立摆放；不接受带道路/天空的大卡片、伪透明、主体裁断或封死的门洞。

## 5. P3：卷轴部署与门前门后交接

在参考图与A01/A02/A03/B01单件验收后，用户已重新授权用现有素材试搭。当前保存的独立E0新组包含中央石拱、左右高低墙墩、两侧B01连接墙和左旗；旧门垣组隐藏但完整保留，9个旧路侧/草对象未改位置。新组已保存重载并检查静止/接近采样；正式接线、门内深度、完整自然接近段及用户整体构图验收仍未完成，不能把以下正式部署项目全部勾选。

- [x] **P3-01 明确E0/E1/J1的环境层归属。** Battle.scene仍是唯一战斗宿主，YJunctionSample是连续环境层；E0正式对象位于Y层，E1/J1使用路线停点和后续环境组，不再把E0作为独立展示入口。
- [x] **P3-02 导入新环境资源。** 本轮未改共享素材导入；正式 Y 使用新建的路线专用材质/Shader副本，现有Sprite继续沿用既有PPU100、Point、Uncompressed、Alpha透明、无mipmap基线。
- [x] **P3-03 保留用户试摆做可切换对照。** 独立 E0 源场景和旧门垣/旧路侧对象均保留；正式 Y 使用复制后的整合组，不覆盖或删除独立源。
- [x] **P3-04 搭建接近段与E0门外构图。** E0组已迁入Y层并按90处门外停点采样；接近段和门垣均使用正式路线投影。路线显露节奏、E1/J1环境密度仍待验收。
- [ ] **P3-05 单独验证穿门深度。** 检查门外接近、入门、门内、门后四个采样位置；用独立内墙/拱顶等形成真实可读深度，不用一张斜拍Sprite伪造穿门。
- [ ] **P3-06 搭建E1集结区与J1分岔。** 已建立 E1/J1 路线锚点并后移分支环境，但 E1 集结区专用美术、J1官道/村落语义和最终左右出口画面仍待制作/验收。
- [x] **P3-07 固定时长配平视觉距离。** `BattleYRouteHost`开场、E0、E1、J1分段目标均配置为3秒；E0/E1/J1距离由Y场景Inspector停点引用计算，不在代码重复维护世界坐标。自然奖励/确认暂停回归仍未完成。

**通过门槛：** 固定相机下真实Game可读，用户能够亲自检查保留的对象；没有瞬移、突然出现、悬空、穿地、透明倒序或门洞堵路。穿门验证没完成时不得称为穿门系统完成。

## 6. P4：验收、保存与当前场景收尾

- [ ] **P4-01 画面验收。** 已提供N1、E0门外、E1、J1的纯场景采样，但E1/J1仍为结构检查、非最终美术验收；正常战斗/HUD叠加和门内采样仍待完成。
- [ ] **P4-02 时间验收。** 比较改前基线与改后各段有效行进时长；测试暂停/恢复、到站各一次，如既有流程支持跳过则保留其语义。
- [ ] **P4-03 节点流程验收。** Host结构已改为 N1战斗→E0确认→E1确认→J1分支；Play已确认N1启动和奖励阻塞存在，但尚未完成自然奖励结算、点击继续、到达E0/E1/J1和分支战斗的完整回归。
- [ ] **P4-04 分支目标验收。** 实际点击J1官道与村落出口，检查空间方向、收到的choice、表现与落点，不仅检查配置引用。
- [ ] **P4-05 持久化验收。** 只保存授权环境资源/场景，回读对象、Transform、Sprite、材质、scenery空项/重复与dirty；重载不得丢失用户布局。临时隐藏HUD/敌人不保存进Battle。
- [ ] **P4-06 确认现有N1收尾状态。** 读取最新草风动/侧景保存状态、分支后段构图与自然流程回归；不把历史54草/151景物或后来80/81 scenery计数当最新事实，也不因本清单而重新铺一遍N1。
- [ ] **P4-07 同步专项结果与里程碑。** 只在有文件/Unity工具证据且用户验收后勾选相应项，保留失败候选与限制；commit/push等另按授权执行。

## 7. 保护边界

- 场景宿主：`Assets/Scenes/Battle.scene`；当前道路/景物层：`Assets/Experiments/CurvedScroll/YJunctionSample.unity`。
- 不移动Battle的Camera、Player、Enemy、StageController、HUD或战斗坐标来适配美术。
- 不修改 `Assets/Experiments/CurvedScroll/Scripts/YScrollSample.cs` 中的Evaluate、Length、junction、radius、turnAngle来掩盖布景问题。时间/显露方案确需工程改动时另行批准，不因“距离可调整”扩大授权。
- 不打开、保存、回退或向 `Assets/Experiments/CurvedScroll/FakeRouteDataTrial.unity` 部署。
- 不重新启用旧城墙天空叠图，不改共享Shader/材质来强行遮盖单个素材的问题。
- 保留用户场景和资源改动；不Save All、不整文件restore、不自动删除试摆或共享候选。

## 8. 当前优先项与状态

**当前优先：请用户目视验收正式 N1→E0 连续结构，随后再细化 E1/J1 和门内空间。**

用户已明确 E0 必须汇入 N1→E0，而不是单独展示。本轮已在 `Assets/Experiments/CurvedScroll/YJunctionSample.unity` 整合 E0 门垣与42件门前侧带，正式路线顺序为 N1尾段→E0门外→E1锚点→J1分岔；`BattleYRouteHost` 加入 E0/E1 单出口确认停点，分支后移到 `JunctionDistance=190`。路线保存重载后 `Length≈266.12`、`scenery=267`、无展示敌人引用、Y/Battle dirty=false、Console warning/error=0。正式 Y 使用路线专用材质/Shader副本，独立 E0 源场景仍保留且哈希未变。Play 已确认 N1 真实战斗启动、路线相机绑定与奖励阻塞存在，但未完成自然奖励结算后点击继续→E0→E1→J1→分支战斗全流程。纯场景截图见 `plan/e0-approach-scene-refinement-handoff.md`；E1/J1 仍为结构占位，尚待用户美术验收。

A03 v1原图与请求/响应保留：`C:/Users/Administrator/Pictures/gptGen/e0_batch_a_from_approved_v3/e0_a03_ruined_pier_right_v1.png`。技术格式为RGBA不等于造型通过；用户反馈已确认A02/A03呈同类高左缘→向右階梯下降墙片，未对应概念图中石拱旁的墙墩/外侧低墙/断墙端部关系。当前A03视为未通过候选，不能用此前材质相近或92%高度的检查替代造型核对。

B01已验收并用于新组合，路径仍为 `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/E0/Candidates/e0_b01_low_wall_right_v1.png`，不移动或重建GUID。PNG字节均未改，低Alpha残留未清理；A01/A02/A03/B01实际Pivot=(512,282)/(512,68)/(512,45)/(512,318)px。新组路径为 `Assets/Experiments/CurvedScroll/E0OuterGatePreview.unity/E0 Outer Gate Preview/E0 Approved Gate - Composition v1`；静止最终图 `Library/Locus/Screenshots/locus_game_20261009_064356_787.png`，distance12图 `Library/Locus/Screenshots/locus_game_20261009_065730_005.png`，均489×979。新组开、旧组关，旧对象/参数保留；路侧旧物件复用。共享材质、Shader、Battle/Y和路线脚本哈希未改，Console warn/error0。正式路线、门内深度和战斗/HUD验收未完成，没有后台任务。

### 相关资料

- `plan/e0-approved-gate-composition-handoff.md`（已验收门垣构图交接）
- `plan/e0-approach-scene-refinement-handoff.md`（门前过渡层交接与下一步）
- `plan/e0-batch-a-generation-prompts.md`（A01-A03 生成词）
- `plan/e0-a03-remake-prompts.md`（A03 重制与结果）
- `plan/e0-b01-generation-prompts.md`（B01 生成与结果）
- `memory/scene-art-production-pipeline.md`（参考图到组装的布景流程经验）
- `design/art-style-guide.md`
- `design/changbanpo-six-node-level-design.md`（节点职责与连续参考原则）
- `plan/n1-scene-art-production-handoff.md`（候选目录与半3D职责，历史计数需另行核对）
- `plan/n1-far-scenery-handoff-20261008.md`（N1远景历史检查点）
- `skill/workflows/image-asset-generation.md`
- `skill/gpt-image-generation.md`
