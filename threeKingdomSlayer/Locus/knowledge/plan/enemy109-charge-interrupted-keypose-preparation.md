---
id: kd_408efff5-0767-441c-85c6-46522aaf2af4
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# Enemy109 冲锋被打断 → 接返航：续接入口（2026-10-14）

> **当前状态（2026-10-15）**：A段透明01–11、十二帧v3（f003/005/007/011/056/058/062/066/070/074/078/087）T80透明版均已验收，**并已接入Unity**：新增`Enemy_109_ChargeInterrupted1..11`、`Enemy_109_InterruptedToRetreat1..12`共23张Sprite、两个Clip、`InterruptedToRetreat`状态与Prefab直接引用；打断路径已改为 `Interrupted →（实需返航时）InterruptedToRetreat → 背向保持`，前摇未离排时不强转。第一关预览波次已加109。未跑运行时自检，由用户开游戏验收。主体颜色连续性仍未修正。

> **2026-10-15 其他对话接续入口（骑乘死亡新方向）**：见 `plan/enemy109-rider-fall-mount-escape-handoff-20261015.md`。用户现要求骑手受击摔落，马存活向左跑离并渐隐；确定制作两段：①完整受击→脱鞍→摔落接地，②独立无骑手马逃跑。正常骑乘不用整套拆分，只在交接帧切开视觉。旧`MountedDeath_v1` D1～D3已生成但方向被取代，保留追溯；不再只修D3，不沿用三段视频提案。新素材包与生成尚未开始，旧三张授权已执行完，不能复用为新收费调用许可。

## 续接入口（先读这一节）

### 这条任务是什么

把「109 骑兵冲锋被打断」做成可接线的素材链：
`Charging → K1受击命中 → K2急停 → f063失衡尾态 → 转身桥 → 既有已验收背向返航Loop`

### 已完成并已验收

| 环节 | 状态 | 位置 |
|---|---|---|
| K1 命中关键帧（静图） | 动作职责已确认；锚点/配色/部署未验收 | `Enemy109ArtSource/01_KeyPosePreparation/ChargeInterrupted_v1/generated/enemy109_charge_interrupted_keypose_v1.png` |
| K2 急停尾帧（静图） | 用户已验收 | `Enemy109ArtSource/01_KeyPosePreparation/ChargeInterruptedTail_v1/generated/enemy109_charge_interrupted_brake_tail_v1.png` |
| A 段视频 K1→K2（4s） | 用户已验收 | `C:\Users\Administrator\Videos\doubaoVideo\enemy109_charge_interrupted_v1_720p_4s.mp4` |
| 12 帧选帧（RGB） | 帧序已验收 | `Enemy109ArtSource/03_SelectedFrames/ChargeInterrupted_v1/raw/` |
| 透明帧 01–11（RGBA） | 用户已验收 | `Enemy109ArtSource/03_SelectedFrames/ChargeInterrupted_v1/keyed_ui_t79/` |
| 透明帧 12 = f097 | **用户按色差拒收**（保留追溯、不得再入下一轮参考） | 同上目录的 `ChargeInterrupted12_f097_t4.000.png` |
| 既有背向返航 Loop | 之前已验收，本轮只复用、不重生 | `C:\Users\Administrator\Videos\doubaoVideo\enemy109_retreat_run_loop_v1_720p_4s.mp4` |

生成参考：A段尾帧采用 **f063**（`keyed_ui_t79/ChargeInterrupted11_f063_t2.583.png`），视频终点参考为既有返航真实首帧`Enemy109ArtSource/02_VideoPreparation/StrikeToRetreat_v1/source/retreat_video_f001_t79_rgba.png`。最新用户指定选帧则以转身桥源f003/t0.083起始、试剪源f087结束，两者未替换成参考图，跨段锚点/材质连续性仍需独立处理。

### 转身桥 v1 原片（保留追溯；采用剪接动作见下）

- 成片：`C:\Users\Administrator\Videos\doubaoVideo\enemy109_interrupted_to_retreat_f063_v1_720p_4s.mp4`
- task `task_9af5590e1ede438fa2ca54e6210aa74c`；2,833,549 bytes；SHA-256 `b1c5094dc2eb5503ee9ccd00c02a231eb9c6978e775c9094861b7830a05f8daa`
- 实际 960×960 / 24fps / 97帧 / 4.04s / 无音轨；MP4 结构与 FFmpeg 全量解码通过；usage 87300、cost.spend 0.48888（币种未返回）；create POST 仅 1 次、无重试
- 记录与审阅：`Enemy109ArtSource/02_VideoPreparation/InterruptedToRetreat_f063_v1/`（`README.md`、`request-spec.json`、`production_record.json`、`generated_review_4s/`、`preparation_verification.json`）
- 已对齐首尾副本（1024 RGB 同底）：`.../upload/first_frame_f063_aligned_green_015962_1024.png`、`.../upload/last_frame_retreat_f001_aligned_green_015962_1024.png`

### Unity接入已完成（2026-10-15）

- 用户授权：“已验收，请接入到项目中，更新文档报告我还有哪些动画没做”。
- 新增Sprite：`Assets/Sprites/Enemy/Enemy109/Enemy_109_ChargeInterrupted1..11.png`（急停收招，0.2秒）、`Enemy_109_InterruptedToRetreat1..12.png`（回正+左转至背面，0.4秒末端保持）。
- 新增Clip：`Assets/Animations/Enemy_109_ChargeInterrupted.anim`（11帧/0.2s/非Loop）、`Assets/Animations/Enemy_109_InterruptedToRetreat.anim`（12帧/0.4s/非Loop）；`Enemy_109.controller`新增`InterruptedToRetreat`状态。旧`Enemy_109_InterruptedPose.anim`保留但不再被`Interrupted`状态引用。
- 映射：源960→保留1024原生尺度→固定整段偏移置1108画布（A段(51,165)、B段(61,84)），终末支撑底行y1006与既有成功转身尾帧一致；未逐帧fit/钉脚/改色。PPU16/Point/Uncompressed/Clamp/1108画布/pivot(554,554)。
- 代码：`CavalryVisualController`新增`interruptedToRetreatAnimation`字段与共用转身计时；`PlayRetreatPose`在打断路由下保持本段末帧，不切回颜色不同的成功转身尾帧。`CavalryEnemy.RecoveryAndRetreat`改为先置`Interrupted`再广播，避免瞬间闪一次背向占位。玩法时长（0.2/0.22/0.18/1s、moveSpeed）未改。
- 第一关：`Assets/RouteData/Stage01/N1_ArtPreview_Battle.asset`第0排第4列加入109，保留既有1021/1011配置。
- 核验：完整编译+域重载通过；回读确认23/23 Sprite可加载且导入设置一致、2/2 Clip仅绑定根SpriteRenderer且无位移/事件、Prefab两个引用正确。**未进入Play Mode、未实例化Prefab、未做动画采样**；游戏内速度/尺度/衔接留用户验收。
- 记录：`Enemy109ArtSource/03_SelectedFrames/InterruptedToRetreat_12Frames_v3/unity_deployment_verification.json`、`.../interrupted_bridge_salvage_analysis_v1/deployment_v1/sprite_deployment_manifest.json`；回退快照`Library/Locus/tmp/backup_enemy109_interrupted_20261015-002321/`。

### 109 骑兵动画清单（2026-10-15 核点）

**当前接线条目（10 项：8项专用动画＋2项占位，不能全部算作完成的专用动画）**

| 动作 | Clip / 素材 | 状态 |
|---|---|---|
| MountedIdle | `Enemy_109_MountedIdle.anim`（6帧循环） | 已验收已接入 |
| Windup | `Enemy_109_Windup.anim`（6帧） | 已接入 |
| Charging | `Enemy_109_Charging.anim`（6帧循环） | 已接入 |
| Attack（成功刺击） | `Enemy_109_Attack.anim`（18帧） | 已接入 |
| StrikeToRetreat（成功刺击后转身） | `Enemy_109_StrikeToRetreat.anim`（11帧） | 已接入 |
| TurnToIdle（返航到位转正面） | `Enemy_109_TurnToIdle.anim`（24帧） | 已接入 |
| ChargeInterrupted（打断急停收招） | `Enemy_109_ChargeInterrupted.anim`（11帧） | **本轮新增接入** |
| InterruptedToRetreat（打断后左转返航） | `Enemy_109_InterruptedToRetreat.anim`（12帧） | **本轮新增接入** |
| RetreatPose（成功路径背向保持） | `Enemy_109_RetreatPose.anim`（静态） | 明确占位 |
| 骑乘受击 | 无专用Clip，沿用合成Idle姿势 | 占位 |

**素材已产出但尚未入库 / 接入（3 项）**

1. **背向返航奔跑循环**：视频已验收 `C:/Users/Administrator/Videos/doubaoVideo/enemy109_retreat_run_loop_v1_720p_4s.mp4`；**未选帧、未抠图、未建Clip**。目前成功路径用静态`RetreatPose`占位，打断路径用`InterruptedToRetreat`末帧冻结；这使返航看起来是滑行而非奔跑，是当前最明显的缺口。
2. **打断后返航保持姿态/循环**：若返航距离较长，`InterruptedToRetreat`播完0.4秒后停在末帧，同样需要接上项奔跑循环。
3. **TurnToIdle 透明版美术验收**：24帧已入库，但生成器主体色差未处理，仍待用户目检。

**尚未制作（4 项）**

1. **骑乘中死亡**：无正式专用视频／动画接入，仍走通用“弹起→旋转→下落”。旧人马倒地D1～D3静图已生成但新方向不再采用；最新为“骑手完整受击摔落＋无骑手马向左逃跑”两段视频，详见新交接入口。
2. **骑乘中受击反应**：无专用HitFlash帧，仅合成Idle姿势+闪白。
3. **存活落马过渡**：真正`Enemy.Launch()`后直接切1011，仍无专门过渡帧；不与最新骑乘致死演出混为一谈。当前死亡新方向不让马倒下。
4. **落马后步兵套**：仍复用1011全套，骑兵身份连续的新步兵素材未做（接口契约已满足，可直接替换）。

> 另外两个已知未收口项（非动画本体）：①主体颜色连续性（本线素材与旧已验收素材的主体RGB色差）仍未处理；②`EnemyGroundAligner`对109落马后新形态需调`Recapture()`。

### 历史：十二帧RGB已验收，T80透明版已完成待验收

- 用户最新反馈：“还是选12帧吧，6帧帧数太少流程度不大行”。沿用之前附图`InterruptedToRetreat02_f003_t0.083.png`的t0.083秒/源f003起点；不改为源f083。
- 当前候选目录：`Enemy109ArtSource/03_SelectedFrames/InterruptedToRetreat_12Frames_v3/raw/`，12张960×960 RGB PNG，编号01–12；源帧`3/5/7/11/56/58/62/66/70/74/78/87`。
- 保留六帧版全部地标f003/011/058/066/074/087，新增f005/007补抬枪回正，f056补正面左转入口，f062/070/078补侧身、后侧及背面过渡。实际衔接f011→f056，继续省略多余右转，尾帧仍为试剪f087。
- 全部从原MP4重解码保存RGB，无480预览像素、无缩放/裁切/平移/镜像/调色/插帧/抠图。独立FFmpeg再次解码97帧，12/12 PNG为960×960 RGB Color Type2，逐像素等于源帧；首帧与用户原附图一致。
- `review/selected_12frames_overview.png`为总览；`selected_12frames_order_review.webp`为960×960无损RGB、83ms/帧、约0.996秒，仅供帧序审阅，不设定游戏时长。总览、CSV和WebP顺序/像素/时长验证通过。
- 十二帧版README、CSV、manifest及`selection_verification.json`已落盘。17帧`InterruptedToRetreat_f063_v1/`与六帧`InterruptedToRetreat_6Frames_v2/`保留追溯，旧版所有文件及原视频哈希不变。
- 用户最新确认：“已验收，请按照80容差开始抠图”。12张RGB及帧序accepted；本轮按同一SVL实现（哈希与已验收A段一致，T79小样重现逐像素一致）完成T80，透明版仍pending。
- 透明候选目录：`Enemy109ArtSource/03_SelectedFrames/InterruptedToRetreat_12Frames_v3/keyed_ui_t80/`，12张960×960 RGBA，保持raw同名01–12；首`InterruptedToRetreat01_f003_t0.083.png`，尾`InterruptedToRetreat12_f087_t3.583.png`。
- 配方：逐帧auto-key / 容差80 / softness16 / halo1 / despill_strength0 / alpha-aware反混色 / 半透明转不透明；无平滑、额外描边替色、全图调色、裁切、缩放、移位或1108补边。
- 独立回读：12/12 PNG Color Type6、Alpha仅0/255、半透明0、透明隐藏RGB0、近源键背景残留0。1,074,099个保守核心像素次Alpha/RGB变化0；边缘12,192像素次RGB改变属于既定反混色，不宣称全体可见RGB不变。最低透明边距L/T/R/B=75/52/125/91px。
- `review/cutout_t80_12_white_overview.png`、`cutout_t80_12_neutral_overview.png`、`cutout_t80_12_dark_overview.png`及三个枪尖/缰绳/蹄部同坐标对照已回读；f066枪部诊断裁框已修正，透明PNG没有重处理。`cutout_t80_12_order_review.webp`为960×960中性底无损RGB、83ms/帧，仅审阅；全帧顺序和像素通过核验。
- 报告`matting_report_t80.json`、`matting_verification_t80.json`及补充裁片核验已落盘。白底可见gEx>8仍198–454像素/帧，按当前配方保留不自动追加压绿；源主体色差未统一。原RGB、RGB预览、原视频及既有已验收A段小样指纹保持。本轮在用户Play Mode下仅处理文件，未改Unity或切换模式。

### 待决项

1. **T80十二帧透明版**：RGB及帧序已验收，透明版待用户美术验收；六帧版因流畅度不足被要求替换，17帧及六帧仅供追溯。
2. **颜色连续性**：用户指出原片前后角色色差。局部材质配准显示提前截尾不能全面匹配返航首帧；源主体颜色差异按原样保留，选帧不等于解决。背景全帧R0/G143–152/B55–62相对键(0,159,62)仍偏低；颜色处理方案未授权，不自动改色或重生成。

### 未做 / 未授权

- T80抠图已完成；尚未1108补边、导入Unity或进行主体颜色匹配；未建Clip、未改Prefab/场景/Animator或玩法时长。
- 未重生既有返航Loop；未使用A段被拒收的f097。当前转身桥尾帧为源f087，不是任一f097。
- `CavalryVisualController`仍只在`previousStage == StrikeRecover`时播`strikeToRetreatAnimation`；**打断路径`Interrupted → Retreating`目前仍走静态背向姿势，尚未接转身**。接线需单独授权。

### 后续入口（等用户指定）

- 验收十二张T80透明帧；颜色问题单独裁定，不把动作或抠图技术核验升级为颜色通过。
- 颜色修正、1108部署布局及打断专用返航转身路由各按用户后续授权进行，本轮不自动推进。

### 环境与同步

- 仓库根是 `H:\Project\threeKingdomSlayer`（Unity 工程在其下的 `threeKingdomSlayer\`）；分支 `route-scroll-movement`，两台设备共用，禁止 force push。
- 本线的知识记录（本文件、`memory/video-generation-case-log.md`）当前**未提交**；要在另一台设备或另一对话读取，需先提交推送。
- 由本线触发的流程修正已提交并推送：`d94e4a0`（视频技能改为 G1/G2/G3 三级颜色门槛、受击动画流程验收清单同步）。上传包：`Library/Locus/tmp/upload_ready/video-generation-color-gates/`（Library 不随 Git 同步）。

## 历史目标与边界（逐轮记录）

- 用户已明确确认上一轮StrikeToRetreat部署，并纠正“接下来制作返航Loop”的建议：**返航视频已经制作且验收，下一段要制作109骑兵冲锋被打断动画。** 区分素材已生产与尚未选帧/接入，不能把后者误记为需要重生。
- K1由用户定位为“命中关键帧”，表示骑兵受到玩家攻击，不代表骑兵刺击成功。K1的动作职责已确认；画面映射、颜色、Unity部署不是同一验收项。
- K2、A段视频及原12张RGB帧序已验收，12张透明版已完成。用户最新逐帧判定透明01–11通过、透明12/f097因色差不采纳；采用f063为打断段尾帧和接返航转身的起点。当前仅讨论下一段内容，不创建视频、抠图、改色或修改Unity。
- 已验收的背向返航视频和成功StrikeToRetreat素材均保留复用；不要因“尚未接入某条路径”误判为缺少素材并重生。

## 准备产物

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\01_KeyPosePreparation\ChargeInterrupted_v1
```

- `README.md`、`request-spec.json`、`reference_manifest.json`和`prompt.zh.txt`/`prompt.en.txt`为可恢复的准备记录。
- 唯一拟上传图：`upload/charging1_identity_green_015962_1024.png`，基于现行Charging1/f036的960RGBA，每侧只补32像素，合成精确RGB(0,159,62)绿底，192,075可见像素0改RGB，未裁切/缩放/重绘。
- `source/`保留Charging1、Charging3、MountedIdle1的原RGBA字节副本，以及已验收返航真实f001 RGB；后三者只供目检追溯，不提交本次正面关键图请求。
- `review/charging_identity_anatomy_equipment.png`、`review/charging1_primary_reference_neutral_1024.png`、`review/charge_and_existing_retreat_context.png`均为已有参考对照，不是新打断姿态。
- 输入/来源哈希、主图四周纯底、原始像素及Prompt/spec回读通过。制作脚本`Library/Locus/tmp/cavalry_concept/prepare_charge_interrupted_keypose_v1.py`仅做Pillow副本/拼图，不调用API。

## 原K1姿态提案（已按批准内容生成）

保持Charging的正面略俯视镜头、同一体积/比例/配色：骑手肩线上提收紧、颈压缩、胸腹通过腰胯回仰、下颌略扬，不以左右倒斜或整幅放大表达后仰；臀部坐鞍、脚在镫内。解剖右手（画面左）握同一根直枪、肘收至腰侧、枪尖停在左前下方；解剖左手（画面右）收缰、连接马嘴。马后腿承重低幅刹停，前膝屈、前蹄稍提且可见，头略收、耳后转。无高人立/腾空/落马/掉枪/命中效果，无侧转，完整剪影64px留白目标。当前只锁定峰值，后续恢复转向返航另做。

## K1已批准并提交的规格

- `/v1/images/edits`，`gpt-image-2.5-sunburst` / `quality=max` / `input_fidelity=high` / 1024×1024 PNG / n=1。
- 使用完整英文Prompt（中文对照供用户审阅）；排除项在正文，无独立negative_prompt字段。
- 纯色绿幕RGB(0,159,62)/#009F3E，省略API background参数，不请求透明。生成后核主体与背景，文字色值不是锁色保证。
- 实际输出：`C:/Users/Administrator/Pictures/gptGen/enemy109_charge_interrupted_keypose_v1.png`及同名request/response记录，工程字节副本在本目录`generated/`，SHA核对一致。
- 本版已获“发起生成”批准并执行一次；不自动重试收费、不生成视频、不部署。

## 实际生成与回读（2026-10-13）

- POST1次、HTTP200、66.062秒，单张1024×1024 RGB PNG / Color Type2 / 1,071,876字节，SHA-256 `eed75c1aefabcd3d26699f1bcafc5398b9945d0ffc2d9a7d0a0e318fdbb8b46a`。已验原件、工程副本、批准参考和双语词哈希；未改色、缩放、裁切、移位或抠图。
- 已回读并附原图及`review/generated_vs_charging1.png`：骑手下颌抬起/回仰收缰、馬前膝屈/后蹄支撑，人仍坐鞍，单枪在身体外侧。技术成功和预审不代表用户验收。
- 诊断RGB轮廓边距165/148/297/173px，未触边且满足64px目标；输入蹄底exclusive929→输出851，上移约78px。姿态变形影响轮廓，不能只按bbox断言整体缩放/平移；后续视频需独立核对鞍部和落地锚点，当前对齐未通过验收。
- 背景角落64px块中位RGB(2,162,63)，std约0.6–0.9，不是请求的精确(0,159,62)。未重铺底或整图调色，主体材质未配准比较，不以底色归因所有主体差异。
- Usage input2227/output7024/total9251，cost未返回，不推断金额。路由host `r2.52image.xyz`；未请求透明，RGB属预期格式而非透明失败。
- 生产/恢复记录：本目录`production_record.json`与`Library/Locus/tmp/cavalry_concept/charge_interrupted_keypose_v1/runtime.json`；图像报告`generated_review_report.json`。K1在原生产记录中保留生成时的美术pending状态；后续用户确认其K1动作职责，但未将锚点/配色/部署升级为已验收。

## 机制上下文

前摇/前移/刺击伤害提交前可打断，提交后不能取消；提交前打断本次0伤害，0.2秒恢复（非刺击）或0.22秒收招（刺击）。骑乘期间免眩晕/击退，实际Launch才落马，所以打断峰值不能画成离座击飞。当前InterruptedPose是Charging1静态占位，相关接线以`Assets/Scripts/Enemy/CavalryEnemy.cs`与`Assets/Scripts/Enemy/CavalryVisualController.cs`为准。

已验收返航片仍是`C:/Users/Administrator/Videos/doubaoVideo/enemy109_retreat_run_loop_v1_720p_4s.mp4`；已接入的成功StrikeToRetreat是`Enemy109ArtSource/03_SelectedFrames/StrikeToRetreat_f010_v2/`，上一轮接线见`plan/enemy109-matting-unity-handoff-20261010.md` §12。旧分层/倒退方案只作历史，不定义本轮动作。

## K2急停尾帧：已生成并获用户验收（2026-10-13）

- 用户反馈：“这个是命中关键帧，后续应该是马急停角色措不及防的姿态。感觉还需要制作关键尾帧再衔接转身返航”；随后最新确认“已验收，思考冲锋被打断的视频内容”。K2关键姿态验收通过，K1→K2角色职责成立；该验收不扩展为锚点/调色/视频/部署通过或收费视频授权。
- 准备目录：`Enemy109ArtSource/01_KeyPosePreparation/ChargeInterruptedTail_v1/`，README、manifest、双语Prompt、request-spec、原图与报告已落盘。唯一生成输入`upload/k1_impact_original_1024_rgb.png`与K1原图逐字节/哈希一致；无裁切/缩放/移位/换底/抠图。Charging3及已验收返航f001仅供本地解剖/流程参照，不是K2生成上传图。
- K2动作职责：前蹄落地、马躯急停承重；骑手由K1回仰转成胸腹向马颈前扑、肩颈和头盔降低、肘屈缓冲，臀坐鞍/脚在镫，左手保持缰、右手保持单根直枪。生成回读观察到骑手前扑/低头、仍在鞍上、缰枪完整，四个脚部支撑轮廓可见；无地面线，技术检查未证明精确承重，但用户已验收该姿态。K2保持正面，尚未复原或转背；不是返航首帧。
- K2单图编辑已POST 1次：gpt-image-2.5-sunburst、max、input_fidelity=high、1024PNG、提示目标绿底(0,159,62)、不请求透明、无自动付费重试。原图`C:/Users/Administrator/Pictures/gptGen/enemy109_charge_interrupted_brake_tail_v1.png`，SHA `a8af5e01cfae6a606388a527d635a5fde2f69eaa6d08b4c2a8019586761da703`，原图/工程副本一致。
- K1/K2现图均未改色、裁切、缩放、移位、抠图或导入Unity。K1→K2素材锚点、同底参考副本和Charging→K1连续性需在未来视频素材准备阶段单独测量与验收；K1输入底色中位约(2,162,63)，K2约(1,164,66)，不得不检查就直接当成视频同底素材。
- `review/k1_k2_k3_action_plan.png`是K1现图、K2文字方案、返航入口的审阅拼图，不是动画成品；`review/k1_charge_retreat_reference_context.png`是参考上下文。生产/恢复状态均记录`user_art_verdict=accepted`、`video_generation_authorized=false`。

## 视频内容方案（A段已生成并验收，B段方案在此）

### 动作和玩法对应

- 打断发生在Windup、Charging或刺击伤害提交前；本次冲锋不结算伤害。Windup/Charging打断恢复`interruptRecover=0.20s`；刺击尚未提交时走`strikeRecover=0.22s`。这两个是游戏播放时长，不是视频服务的生成时长。
- **A段：K1受击接触 → K2急停尾态。** K1作为收到玩家打断攻击后的第一个可读接触姿态；一拍急促回仰后，马先把前蹄落稳、前膝屈曲承重、后肢撑住；骑手的反应略滞后，胸腹继续向前、头盔与肩线下压，肘部缓冲。动作要互相重叠而非逐部件机械排队。枪和缰始终握持、人仍坐鞍、脚仍在镫；逐渐收稳到K2，尾帧仍正面且惊慌未复原。
- A段不画骑兵成功刺击、玩家/敌人伤害命中特效、第二次撞击、马高抬/腾空/击飞、骑手落马、松缰掉枪、镜头运动或全体位移。动画画布内主体尺寸/基线保持稳定；游戏排位移动由玩法Transform单独负责。
- **B段：K2急停 → 恢复重心/收枪 → 人马共同转身 → 既有返航首帧。** 骑手从前扑收回重心并把枪收回身体外侧；马先稳脚，再与骑手协调转向，经侧身过渡到背向；最后一帧精确落在已验收返航视频的真实背向f001，随后直接复用原返航循环，不重生/重绘返航片。转身段不含长距离跑动、第二次攻击或镜头绕拍。仅当骑兵实际离开发起排、需要返航时播放B段。

### 片段边界建议

- 推荐A段首帧K1、尾帧K2；B段首帧K2、尾帧既有返航f001。A首设K1是本稿的播放策略建议：游戏收到打断攻击时直接进入可读的接触姿态，然后播放急停余势；K1不是重新发生一次撞击的前摇。Charging→K1入口的映射仍需独立对齐。若用户希望视频源片也包含完整的Charging→K1受击建立过程，可改用Charging/K1/K2三张普通`reference_image`作为动作地标；这不能与`first_frame`/`last_frame`混用，且不能保证精确锁定K1出现的时间。当前没有替用户批准任何一种生成规格。
- 采用首尾帧模式时，每段只设自己的首尾图。K2是A尾/B头的动作地标，两段均非Loop；已有返航Loop复用。生成后还须检查真实输出首尾帧的姿态、颜色与锚点连续性，参考图并不保证视频逐像素锁定。
- 生成视频的素材时长必须与玩法播放时长分开：若将来选用Seedance，4秒是可用最短源视频规格，不等于Unity播4秒。A段导出后从连续动作中选帧，再依玩法分别适配0.20/0.22秒；重要的是清楚覆盖K1接触、马前蹄落地、骑手惯性前扑及K2稳态，而非把4秒全量塞进短Clip。各Clip帧数和抽帧节奏本轮不定稿。
- A段源片节奏建议（若采用4秒素材，只作Prompt节奏目标）：0–0.3秒从K1接触姿态连续释放余势，不再受击；0.3–1.5秒前蹄下落、前膝吸收重量，骑手仍滞后保持回仰；1.0–2.7秒与马落蹄过程重叠，骑手胸肩和头盔前点、肘屈缓冲、枪尖沿短弧轻微下沉；2.7–4.0秒少量衰减收稳到K2，不反弹回Idle，不重复动作。红缨、马鬃与枪缨只作轻微滞后，不出现大幅飘带越框。具体数值和完整双语Prompt未来另交用户审阅。
- B段动作重叠：开始从K2抬回胸肩、右肘收枪并抬枪尖；左手保持缰绳牵引，马头/颈先提示转向，再用短小换脚带动躯干和后躯完成180°转向，骑手骨盆随鞍、上身略滞后，结束时右手枪在背向画面右侧且近竖直，匹配已有返航f001。不要让整个人马绕垂直轴像纸片原地旋转，也不要让枪横扫出框；主体阵位固定，转向允许必要的局部换脚。
- B段游戏播放时间不应额外延长返航移动；更适合与首个返航排移动重叠，由动画播放速率适配既有`moveSpeed`。若无法自然重叠，先调度设计，不通过加玩法等待补动画。

### 当前接线约束与后续准备门槛

- `CavalryVisualController`现有成功刺击的转身Clip仅在前一视觉阶段为`StrikeRecover`时进入；打断路径从`Interrupted`进入`Retreating`，当前会改用静态`retreatPoseAnimation`，没有K2→背向的打断专用转身路由。因此B段不是“生成后直接换静态图”：未来需另行批准Animator/Prefab接线，并正确处理打断期间有无实际返航距离。若打断发生在Windup、尚未离开发起排，`RetreatRoutine`会立即结束并回到MountedIdle，不应无条件转180°。
- 返航末端素材参照是`Enemy109ArtSource/02_VideoPreparation/StrikeToRetreat_v1/source/retreat_video_f001_t79_rgba.png`（真实f001透明帧）；既有用户验收MP4为`C:/Users/Administrator/Videos/doubaoVideo/enemy109_retreat_run_loop_v1_720p_4s.mp4`。B段需对齐前者并接后者，不得把成功StrikeToRetreat首帧误作K2入口。
- 生成前仍须单独检查K1/K2及返航f001的画布、鞍位、地面锚点、尺度、完整轮廓和背景；K1/K2背景虽近似绿色但统计值不同，须另制同色上传副本并让用户审阅。K1蹄底与Charging的既有约78px差异仍是跨段定位检查项。不得自动裁切/重心居中、逐帧缩放、整图调色或以Prompt代替量测。
- 本节保留原动作分析；后续用户生成请求与最终材料状态见下节。A/B内容讨论不等同于视频去背或Unity部署授权。

## A段视频生成（唯一任务已下载解码，用户视频验收通过）

- 用户确认完整双语Prompt、已准备的首尾同底副本及`seedance-2-fast / 4s / 720p / 1:1 / seed=-1 / 无音频无水印`。范围仅A段K1→K2，不含B转身或重生返航Loop。
- 唯一task `task_2db36264422f43af9add2480770f4e6c`：参考上传2次、视频create POST 1次，服务端success；usage completion/total=87300，cost spend=0.48888（API未回币种，不推断）。一次urllib轮询超时、一次SSL握手超时；改用curl查询原任务成功，没有重建任务。下载4个Range均返回/校验HTTP206，MP4结构与FFmpeg全量解码通过。
- 成片`C:/Users/Administrator/Videos/doubaoVideo/enemy109_charge_interrupted_v1_720p_4s.mp4`，2,114,011 bytes，SHA-256 `d3df3049161ade578d178c658972087d55e5c24a06310b9e961f5998a40d22d9`；实际960×960、24fps、97帧、4.04s、无音轨。技术状态complete，用户最新确认视频验收通过；选帧序列的验收仍单独记录。
- 输入副本维持1024RGB、source scale=1/offset(0,0)，无crop/resize/warp；269,673核心RGB不变、边缘重合成变化K1/K2分别414/457像素；空白同底(0,159,62)。原图、已有返航片未改。
- 审阅目录`Enemy109ArtSource/02_VideoPreparation/ChargeInterrupted_v1/generated_review_4s/`含12时序样本接触表、真实首尾对照、最后12连续帧、12张独立诊断帧及逐帧背景/边距报告。外4px无前景；最小L/T/R/B=120/86/242/154px。背景中位范围R0–2/G143–154/B56–60，偏离输入键并有轻微漂移；材质颜色未配准验证。
- 原尺寸抽帧的历史代理观察：开头约0.3–0.8秒有前躯/前蹄额外上抬后落地；视频随后已获用户验收，此观察不覆盖用户结论，选帧保留已验收原片的动作。

## 12张有效帧（原始RGB及帧序已验收）

- 目录`Enemy109ArtSource/03_SelectedFrames/ChargeInterrupted_v1/raw/`，真实源帧序号：f001/003/009/017/021/025/029/033/039/051/063/097。保留视频真实首尾，前段密集采样，尾段减少近似静止重复，覆盖回仰、前躯起伏、落蹄、前扑及收稳。
- 12张960×960 RGB PNG逐像素等于对应视频解码帧，没有裁切、缩放、移位、调色、插帧或去背。独立重解码全部97帧，所选12帧像素及PNG哈希、CSV时序、总览图和WebP顺序验证通过；原MP4指纹不变。
- 清单：`selected_frames_manifest.json`、`selected_frames.csv`；验证：`selection_verification.json`。预览：`review/selected_frames_overview.png`、`review/selected_frames_order_review.webp`。WebP每帧125ms仅为审阅顺序，不代表源片或游戏时长。
- 选帧边缘4px诊断前景均0，最小L/T/R/B=120/90/243/158px；不代表Alpha或全部解剖验收。原片底色偏移保留，主体颜色未配准验证；本轮不重生成或校色。
- 用户原视频和帧序verdict=accepted。原始`raw/`保持不变，后续透明版见下节。既有返航Loop和玩法参数保持原样。

## T79透明帧（2026-10-14，01–11已验收，f097色差不采纳）

- f029单帧小样已获用户“已验收，可以按照当前参数继续抠图”确认。本轮不再询问，按相同SVL Chroma / 逐帧auto-key / T79 / softness16 / halo1 / alpha-aware despill / 半透明转不透明处理其余11帧；f029原PNG直接逐字节复用，SHA`7349b5b99fb26827fbee556e9327089aa3d64679ec3c5d34d22bb8232c931573`不变。未加平滑、额外描边替色、全图调色或新收费API。
- 完整透明目录`Enemy109ArtSource/03_SelectedFrames/ChargeInterrupted_v1/keyed_ui_t79/`，共12张960×960 RGBA，同原选帧文件名与顺序。原RGB、原MP4、已验收小样和原RGB总览/WebP均保持。
- 独立回读12/12 PNG Color Type6、Alpha0/255、半透明0、透明隐藏RGB0、源键色附近背景残留0；990743个保护核心像素次Alpha/RGB变化0。可见边缘反混色RGB共10523像素次变化，不能宣称所有可见RGB不变。白底可见绿优势>0每帧801–1317、>8每帧216–569，按当前验收配方保留，不擅自追加压绿。
- 透明边距最低L/T/R/B=121/91/244/161px；没有裁切、缩放、移位、逐帧重锚或1108补边。白/中性/深底整套总览、原图/白底/深底枪尖/缰绳/蹄部同坐标对照已经回读；未见明显新增细部断裂，该段技术/代理观察保留；用户后续明确接受透明01–11并因色差拒绝f097。
- 报告`matting_report.json`、独立验证`matting_verification.json`。预览`review/cutout_12_white_overview.png`、`cutout_12_neutral_overview.png`、`cutout_12_dark_overview.png`和`cutout_12_order_review.webp`；WebP每帧125ms仅用于帧序审阅。三底总览各帧像素和WebP顺序回读通过。
- 验收边界：视频、帧序、f029及ChargeInterrupted01–11透明帧accepted；ChargeInterrupted12_f097因用户指出色差不采纳，仅保留追溯。恢复记录与规格已同步，未导入Unity、创建Clip、改Prefab或进入Play Mode。

## 从f063开始的返航动作方案（已据此生成转身桥 v1，结果见文首续接入口）

### 采用端点

- **采用起点**：`ChargeInterrupted11_f063_t2.583.png`，源帧63，已验收透明帧。骑手仍前倾、头盔较低，枪尖向画面左下，身体和马仍正面；它是“急停后的失衡尾态”，不是返航背向姿态。
- **采用终点**：`Enemy109ArtSource/02_VideoPreparation/StrikeToRetreat_v1/source/retreat_video_f001_t79_rgba.png`，现有已验收背向返航视频的真实首帧。它不是`f097`，也不是成功刺击转身的中间帧。
- `f097`透明帧因用户指出色差不采纳，不能作为终点、参考图或颜色基准；原RGB和透明文件都保留，不删除、不重抠、不校色。

### 动作分段建议

| 段落 | 建议源片时间 | 内容 | 必须保持 |
|---|---:|---|---|
| 1. 恢复重心 | 0–0.7s | 从f063前倾姿态少量抬回胸肩/头盔，让骑手重新控制身体；骨盆仍坐鞍，不先复原成完整Idle | 从首帧就有动作，不再受击、不重新后仰或抬马 |
| 2. 收枪牵缰 | 0.3–1.3s | 与恢复重心重叠：解剖右肘收近腰侧、枪杆沿身体外侧收回并抬尖；左手轻收缰，引马头朝画面右侧 | 全程同一右手同一根直枪；不横扫胸脸，不作新刺击 |
| 3. 人马转向 | 0.8–3.3s | 建议沿既有成功转身的画面右侧方向：马头/颈先转，前蹄短步调整承重，躯干与后躯跟随；骑手骨盆随鞍，上身略滞后，正面→右向侧面→后侧3/4 | 转向只有一次；脚有真实换步，不做整组纸片旋转/滑行 |
| 4. 接入背向 | 3.0–4.0s | 后侧3/4收至正背面，落在真实返航f001的支撑相，右手枪投影到画面右侧近竖直；轻微披挂/红缨余势收稳 | 短暂经过准确入口即可，随后复用已验收返航片，不新增长停留或一套奔跑 |

这些时间区间相互重叠，4秒只是可审阅的源视频节奏建议，模型/时长与完整双语Prompt尚未批准；不是Unity播放时长。整个桥段建议非Loop，首帧为采用的f063、尾帧为既有返航真实f001，之后复用原返航片。若实际返航只有一排，转身与既有移动时长的调度需后续单独确认，不因4秒源片增加玩法等待。

### 关键视觉原则

- **先恢复再转向**：不能从f063直接瞬间翻到背面；骑手先收回胸肩，马先稳住前蹄，再由马头牵引转向。
- **枪的运动是收回弧线，不是攻击弧线**：枪尖始终在身体外侧，最终到背向f001的右侧竖持位置；不产生新的刺击判定或攻击读感。
- **固定源画布但允许局部换脚**：视频镜头、960×960画布、整体绘制尺度和根锚点固定；转向只通过马腿短步、颈部、肩胯和骑手关节改变剪影。禁止逐帧bbox fit、自动居中和整个人马缩放。
- **保持人马合一**：鞍部、骑手骨盆和马躯相对位置连续；不落马、不漂浮、不让骑手独立旋转。
- **颜色/锚点分开验收**：f063透明底行788，真实返航f001底行891，二者相差103px；同为960×960不代表根/地面位置已对齐，bbox或蹄底差本身也不能证明整体缩放差。后续需核对人马绘制尺度、鞍位与支撑区，再另存明确的整体源空间映射副本并交用户审阅；不能让生成过程自行放大/下漂补差，不能逐帧按bbox fit。当前只是只读量测，未平移/缩放任一原件。
- **颜色连续性**：首帧须基于已验收f063，不能换成颜色异常的f097或旧静态K2；首尾上传副本未来可由可信Alpha合成到共同键色，原角色核心RGB保持。即使背景已统一，生成中主体RGB仍可能变化，需把头/中段/尾和接回返航素材的材质连续性单独审阅，不能用同绿底或技术解码通过替代。

### 玩法接线限制

- 当前`CavalryVisualController`只有在`previousStage == StrikeRecover`时才播放`strikeToRetreatAnimation`。打断路径是`Interrupted → Retreating`，因此从f063接返航需要单独的打断返航转身路由；不能把成功刺击的Clip直接当作当前已接线结果。
- `RecoveryAndRetreat()`先用`interruptRecover=0.20s`把视觉位置拉回当前物理排，再进入`RetreatRoutine()`；返航有实际排数才需要转身段。若打断发生在Windup且当前排未改变，`RetreatRoutine()`会直接完成，不应强制播放完整180度转身。
- 本节保留原动作提案；用户已同意该内容，并要求先对齐再准备生成。最新对齐副本与最终规格确认见下节；没有抠图、Animator/Prefab或玩法时长修改。

## f063→返航f001 转身桥（2026-10-14，已生成，视频待验收）

- 用户方向：“同意，先对齐尺寸和锚点位置，然后准备生成词和图发起生成”。只制作转身桥，既有返航Loop保留，f097拒收帧不使用。
- 准备目录`Enemy109ArtSource/02_VideoPreparation/InterruptedToRetreat_f063_v1/`，首尾source/upload、对齐总览、装备原像素尺度对照、完整中英文Prompt、request-spec、README、验证报告及production/runtime记录已落盘。用户确认“确认并生成”后已创建一次并完成下载解码。
- 尺度策略：保留两端原生1:1像素尺度，不根据枪-inclusive总bbox缩放。前倾正面与直立背面投影不同，本轮局部盔/靴对照不足以证明需要整体缩放；保留原像素，待用户目检最终对齐图。
- 支撑锚点：f063底行788、返航f001底行891，最后6行支撑外侧中点分别x481/x471。首帧相对尾帧整体平移(-10,+103)，再共同补32px边到1024：首帧原960画布位置(22,135)，尾帧(32,32)，最终支撑中点x503、底行y923一致。不是逐帧重锚/自动居中，原图未改。
- 独立验证首帧142775、尾帧149467可见像素均无删除或改RGB；两张1024RGB空背景同(0,159,62)/std0。上传首SHA`61e0124e5dde941fba86a5292fe158a25344aba432d65eefd96bd11d2e43b2b7`、尾SHA`fdb0ed4ae67c741397181f5628f38f7ea8ea61d06ac267d18102c25adb95f479`。原f063、返航f001及返航MP4指纹保持。
- 已用规格`seedance-2-fast / 4s / 720p / 1:1 / 无音频无水印 / seed=-1`，仅first_frame+last_frame，非Loop。完整双语词已贴于对话，EN SHA`99798fa58290e9be7f95f1d46965cebbc062eb66831ea831afdbf2b2cd390274`，ZH SHA`cb9c6fdd6ac4f1fff57baebbc7d6e2f01265159839f6e2663f81dc57df26ef0c`。
- 成片`C:/Users/Administrator/Videos/doubaoVideo/enemy109_interrupted_to_retreat_f063_v1_720p_4s.mp4`，task `task_9af5590e1ede438fa2ca54e6210aa74c`，2,833,549 bytes，SHA-256 `b1c5094dc2eb5503ee9ccd00c02a231eb9c6978e775c9094861b7830a05f8daa`；实际960×960/24fps/97帧/4.04s/无音轨，MP4结构与FFmpeg全量解码通过。参考上传2次、create POST仅1次；usage completion/total=87300，cost spend=0.48888（API未返回币种）。有限轮询第12次返回success，无查询重试。
- 检查依据：`head_tail_manifest.json`与`preparation_verification.json`。同底和可见RGB保留不保证视频模型恒色、姿态/尺寸或端点连续，成片仍需独立诊断和用户美术验收。
- 原尺寸抽帧的代理观察：f014早段已是直立持枪；f033/f052马朝画面右侧，f059/f065又朝画面左侧，f071转后侧、f084后正背面。**未形成预定的单向180度紧凑转身，中段更像原地小跑，末段有短暂背向直立停留。** 全97帧外4px无前景、无帧低于32px，最小边距43/40/88/90px；背景中位R0/G143–152/B55–62（相对批次基准键(0,159,62)偏低，非“无偏移”）。视频verdict=pending，不自动重生成。
- 本批触发的流程修正：视频技能§色彩一致性已改为 G1/G2/G3 三级门槛，并新增“偏差必须升级为待用户决策项”“参考帧来自上一代产物时先与身份基准比主体核心RGB”；`skill/workflows/character-hit-animation-video-workflow.md`同步。此修正直接来自本批审计，且与f063/f097色差属于同一根因。

## 去右转试剪获认可 → 17张原尺寸RGB选帧（2026-10-14）

- 用户提出去掉多余右转、采用受击回正后朝屏幕左侧的有效动作。诊断试剪 `f001–f014 → f056–f087` 的自然度随后获用户明确认可；用户要求按试剪选帧，但像素必须来自原视频。
- 输出：`Enemy109ArtSource/03_SelectedFrames/InterruptedToRetreat_f063_v1/`。17张源帧 `1/3/5/7/9/11/14/56/58/60/62/66/70/74/78/82/87`，文件编号01–17，首`InterruptedToRetreat01_f001_t0.000.png`，尾`InterruptedToRetreat17_f087_t3.583.png`。
- 原MP4完整解码97帧后保存原960×960 RGB；独立第二次FFmpeg解码验证PNG Color Type2、RGB像素、帧号/时间、CSV及预览通过；最小选帧边距L/T/R/B=74/51/119/90px，外4px无诊断前景。没有裁切、缩放、平移、调色、镜像、插帧、抠图或1108补边。
- 试剪点f014→f056原样保留；07/08对应两侧。试剪尾f087替代原MP4长停留末帧f097，不重绘返航入口。两份960×960无损WebP均逐帧等于对应源RGB，帧序回读核对通过。
- 新选帧`selection_user_verdict=pending`；试剪`user_motion_verdict=accepted`；颜色差异保留`unresolved`。原视频、首尾上传副本、已验收A段f063、返航f001及返航MP4前后指纹一致。
- 本轮未调用收费生成，未改Unity或玩法，未运行Play Mode。
