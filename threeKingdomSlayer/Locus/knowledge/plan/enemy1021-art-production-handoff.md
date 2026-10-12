---
id: kd_190da719-cb35-4537-9560-44d26329b07d
injectMode: inherit
injectAgents:
- unity
summary: 1021美术交接：Idle已部署，Walk/Attack/ShieldGuardHit已接入第一关并获用户游戏验收；格挡六帧、0.30秒非循环、播完回Idle。后续在其他对话指定未完成动画或机制目标，保留素材与接线记录；未运行自检。
aiEditMode: inherit
---

# Enemy 1021 动画与机制继续任务交接（ShieldGuardHit 游戏验收通过）

> **用途**：在新对话里直接读本文件即可开工，产出的目标是 **1021 盾兵的正式美术**（先定形象与 Idle，再按批次做动作）。
> **前提**：1021 的机制与代码已在最小可玩版落地（见 `design/enemy-1021-shield-guard.md`）；Idle、Walk、Attack 与 ShieldGuardHit 已换为正式素材，其余动作仍复用 102 占位美术。
> **本文件不定义机制**；机制、状态机、动画需求以 `design/enemy-1021-shield-guard.md` 与 `design/enemy-1021-animation.md` 为准。
> **当前进度（2026-10-11 ShieldGuardHit 游戏验收通过）**：Idle 四帧、Walk 六帧、Attack 十二帧与 ShieldGuardHit 六帧均已接入第一关；Walk、Attack 与 ShieldGuardHit 已获用户游戏验收。格挡使用已验收透明v3，`Assets/Animations/Enemy_1021_ShieldGuardHit.anim` 为30fps、0.30秒非循环，首帧承力、末帧恢复，经 `Assets/Animations/Enemy_1021.controller` 原exit time 1转场回Idle。第一关 `Assets/RouteData/Stage01/N1_ArtPreview_Battle.asset` 已含1021，Prefab scale仍为0.090161。Hit/ShieldBreak等其他动作仍为102占位；后续目标由用户在其他对话指定。本次只同步交接与验收留档，未修改Unity资产或运行自检。
> **后续部署边界（用户明确要求）**：只做到角色接入第一关并配置对应动画；部署完成后通知用户，由用户开游戏检查和验收。除非用户在对话中明确要求自检动画部署情况，否则不进入运行自检、不自动驱动动画或追加画面检查。该要求优先于下文历史检查流程，见 `memory/user-preference.md`。

---

## 0. 下一对话从这里继续

### 最新 ShieldGuardHit 六秒视频 v1（2026-10-11，透明六帧v3已接入，用户游戏验收通过）

- 用户认可K1 v4（“很好，根据这个v4参考图思考视频生成词”），审阅完整双语视频词后明确“时间改为6秒，发起生成”。已上传Idle定妆图和K1 v4，以两张普通 `reference_image` 创建一次视频任务，未混用首尾帧。
- **Task ID**：`task_e6757d35965a46c389e06bfd86a6d4ae`，创建HTTP200，accepted UTC `2026-10-11T13:25:25.591157+00:00`。规格 `seedance-2-fast` / 6秒 / 720p / 1:1 / 无音频 / 无水印 / seed=-1。
- 动作目标：约0.5秒接住来自画面左侧的攻击，盾向画面右侧压入，缩颈/耸肩、膝屈和空手稍晚跟随；经过v4承力姿态后盾向左回弹，颈肩释放、回Idle，保持到六秒结束。无特效、固定双脚/根位置/镜头。提示词保留此前0.25–0.35秒受力与恢复目标，用户只修改源视频总时长。
- 首轮12次查询全部 `generating`，随后查询现有任务于UTC `2026-10-11T13:38:48.417507+00:00` 返回 `success`；接口 usage `completion_tokens=130500`，cost `spend=0.7308`（币种未注明）。下载未创建新任务。
- 成片：`C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_guard_hit_v1_6s.mp4`，2,310,345 bytes，SHA-256 `cefcc521c18651cadc0777a29f44f23b5fb44ccd97ad76290eba2d78692d7e1f`。实测960×960、24fps、145帧、容器时长6.041667秒、末帧PTS 6.0秒、H.264、无音频；MP4 `ftyp/moov/mdat` 结构和全部帧解码通过。
- 下载报告：`C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_guard_hit_v1_6s.download.json`；四段Range均HTTP206/ETag和字节范围核验通过，下载耗时89.724秒。用户随后明确“已验收，请截取有效帧”，要求六帧；六帧选帧也已验收，用户明确要求“开始抠图”；本地抠图v3完成后，用户明确“已验收，请接入”，透明结果已验收并完成静态接入；用户随后明确“已验收，更新交接文档准备在其他对话继续任务”，ShieldGuardHit游戏表现验收通过。
- 六帧已截取：零基 **f023/f024/f026/f090/f093/f101**，源时间0.958/1.000/1.083/3.750/3.875/4.208秒。首帧已是承力姿态，顺序为初次接触→压入→缩颈峰值→回弹→颈肩释放→恢复Idle；跳过f027..f089的长时间近似保持和前后待机。
- 已验收源帧目录 `Library/Locus/tmp/enemy1021_shield_guard_video_v1_6s/frames/selected_v1/`，文件 `guard1_f023.png` 至 `guard6_f101.png`；六图均960×960 RGB原始画布，与解码源帧字节哈希及重新解码像素一致，原始文件未改动。选择记录 `frames/selected_frames_v1.json` 已记录 `selected_frames_accepted_by_user=true`，145张原始帧保留于 `frames/clean_raw/`。
- 原选帧预览 `Library/Locus/tmp/enemy1021_shield_guard_video_v1_6s/frames/GUARD_SELECTED_6_REVIEW_v1.png` 与同目录 `GUARD_SELECTED_6_PREVIEW_v1.gif` 保留。**当前透明交付为v3**：`Library/Locus/tmp/enemy1021_shield_guard_video_v1_6s/frames/keyed_gray_v3/`，六张与源帧同名的960×960 RGBA PNG；未裁切、缩放、平移、对齐或重绘。
- **抠图配方与核验**：现有本地SVL Chroma自动取底色，threshold14/softness16/halo1/半透明转不透明；灰底与甲胄近色，补充底色距离26的8连通外部背景与逐帧确认的真实空隙清理，保护内部金属高光。未使用生成式平滑、旧描边修复或远程生成。六图源/输出哈希回读一致，Alpha仅0/255、透明RGB全0、已识别背景及空隙泄漏0、距可见轮廓4px的内部RGB改动0；用户随后明确验收透明结果。核验报告 `Library/Locus/tmp/enemy1021_shield_guard_video_v1_6s/frames/guard_matting_verification_gray_v3.json`。
- **透明结果预览**：白底/棋盘格 `Library/Locus/tmp/enemy1021_shield_guard_video_v1_6s/frames/GUARD_MATTED_6_WHITE_CHECKER_v3.png`，原图/白底/深底细节 `Library/Locus/tmp/enemy1021_shield_guard_video_v1_6s/frames/GUARD_MATTED_SOURCE_WHITE_DARK_DETAILS_v3.png`，GIF `Library/Locus/tmp/enemy1021_shield_guard_video_v1_6s/frames/GUARD_MATTED_6_PREVIEW_v3.gif` 均已回读。预览使用固定ROI，GIF每帧80ms仅评审姿态顺序，不定义正式Clip。用户已验收透明结果并授权接入，按既有固定偏移补边部署，未做逐帧缩放或脚底拉齐。早期 `keyed_ui_t14_connectivity_v1/`、`keyed_gray_t14_v2/` 保留为试验，不作最终交付。
- 完整双语词、六秒参数、参考图职责/哈希和task ID在 `C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_guard_hit_v1_6s.request.json`，已回读核验；实际提交Prompt SHA-256 `fc83097a561aee4862ea902d516bb9ee94998d97f06d9213388e4057af87ddf4`。`generate_video.py`和`query_and_download_existing.py`均已执行，不要重跑或重新创建任务。六帧选帧、透明v3及Unity游戏表现均已获用户验收；不要重做已验收动作。
- **ShieldGuardHit部署**：透明六帧以Idle/Walk/Attack相同固定偏移 `(74,91)` 零重采样补边到1108×1108，保存为 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_shieldGuardHit1.png` 至 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_shieldGuardHit6.png`。导入设置与Idle一致：Single/PPU16/Point/无压缩/无Mipmaps/Tight/Center；Unity实际导入1024×1024、Sprite PPU14.7870035，与现有动作导入几何一致。原960透明帧像素和动作位置保留，无逐帧缩放、裁切或拉齐。
- **Clip与接线**：新建 `Assets/Animations/Enemy_1021_ShieldGuardHit.anim`，30fps、0.30秒非循环，六帧键位0/0.033333/0.066667/0.133333/0.2/0.266667秒。包含 `ShieldVisual` sprite null键，无Transform曲线或AnimationEvent；仅替换1021控制器的 `ShieldGuardHit` motion，保留exit time 1、duration 0回Idle转场。Prefab已引用该Controller和状态名，第一关已含1021，无需新增入口配置。
- **保存证据与验收边界**：`Library/Locus/tmp/enemy1021_shield_guard_deploy_v1/preparation_report.json` 与 `Library/Locus/tmp/enemy1021_shield_guard_deploy_v1/asset_verification.json` 已回读；六张补边PNG像素一致、六个Sprite引用完整，Clip/Controller重新导入核对保存成功。85个受保护既有文件哈希保持一致，既有资产仅修改1021控制器；未改Prefab、关卡、场景、脚本或其他动画。代理未进入Play Mode、未驱动动画采样或执行运行/画面自检；用户自行开游戏后已明确验收通过，部署留档已同步 `user_game_acceptance=accepted`、`game_acceptance_pending=false`。

### ShieldGuardHit 关键帧参考图制作历史（2026-10-11，K1 v4获认可；最终游戏验收见上节）

- 用户明确的格挡读感：角色右手持盾（画面左侧），小幅接住来自画面左侧的攻击，盾牌小幅震荡，角色不后仰；动作素材不允许特效。v1两图因角色姿态没有变化、仅增加盾牌特效而被用户否决。
- 按用户随后批准的修订动作语言重新生成v2：K1 承力压入、K2 释放回弹。唯一参考为 `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3.png`；`gpt-image-2.5-sunburst`、`quality=max`、`input_fidelity=high`、1024×1024、PNG、浅灰底，每个姿态仅提交一次。
- K1 v2：`C:/Users/steam/Pictures/gptGen/enemy1021_shield_guard_contact_v2.png`，1,111,759 bytes，SHA-256 `6dd5400f33134dfbcdfcd0fc7f75fec670560a6ced628a41a24cac3da34e7772`。HTTP 200，usage input/output `1167/8596`，cost未返回；用户反馈盾牌位移有效、肢体变化不足，未通过。
- K2 v2：`C:/Users/steam/Pictures/gptGen/enemy1021_shield_guard_rebound_v2.png`，1,020,121 bytes，SHA-256 `49df3772aa0649236534f176c6f47a87415f8dfb2affda1c1b6b416308343b4e`。HTTP 200，usage input/output `831/1084`，cost未返回；随v2方案一并不作为验收动作依据。
- K1 v3：`C:/Users/steam/Pictures/gptGen/enemy1021_shield_guard_contact_v3.png`，965,738 bytes，SHA-256 `ec3613fc53e63c38f9ae10b5eabd33054fcbd73529726f9d90eed81ac31b1031`。使用Idle定妆图和K1 v2两张参考，HTTP 200，`quality=max`、`input_fidelity=high`，usage input/output `2662/7024`，cost未返回；每个姿态只提交一次。
- 用户反馈：K1 v2盾牌位移有效，但角色整体肢体姿态变化不足，至少须有朝画面右侧倾斜的承力反应，v2未通过。用户随后批准K1 v3双参考编辑Prompt：以骨盆为支点，上身向画面右侧侧倾约8°、头肩胸相对骨盆右移约25–35px，保持双脚和根位置固定、正面朝向、不后仰、无特效。8°和25–35px是获批请求目标，不能当作成图实测结果。
- K1 v3已回读，代理初检曾认为右倾读感偏弱；**用户随后明确反馈“其实已经很接近了，建议稍稍让其缩脖子表达受到冲击即可”**。当时保留v3姿态，不再加大右倾或重做盾位/四肢；该阶段K1尚未最终验收、K2暂缓。后续K1 v4已认可并用于已验收视频，最终游戏验收见上节。
- 完整双语Prompt与请求/响应/核验位于输出旁的 `.request.json`、`.response.json`、`.result.json`；v2生成脚本和配置位于 `Library/Locus/tmp/enemy1021_shield_guard_keyframes_v2/`。费用字段为空，不推断费用。
- K1 v3阶段仅有静态候选，当时尚未生成K2 v3、格挡视频、透明帧或Unity资产；后续完成情况见上节。Idle/v2/v3同画布对照为 `Library/Locus/tmp/enemy1021_shield_guard_keyframes_v3/K1_IDLE_V2_V3_REVIEW.png`：三图统一缩至50%预览，未分别裁切或对齐。v3完整请求、脱敏响应、核验在输出旁 `.request.json`、`.response.json`、`.result.json`；脚本和配置在 `Library/Locus/tmp/enemy1021_shield_guard_keyframes_v3/`，脚本已执行，不要重新运行。
- **K1 v4已生成**：以 `C:/Users/steam/Pictures/gptGen/enemy1021_shield_guard_contact_v3.png` 为唯一输入，仅请求颈肩微调；目标输出 `C:/Users/steam/Pictures/gptGen/enemy1021_shield_guard_contact_v4.png`，994,135 bytes，SHA-256 `fe6d53654ee1f985f440f835a9e7d0bef35b264f8cbc5d42e0ea399f40ad875d`。HTTP 200，`quality=max`、`input_fidelity=high`，usage input/output `653/1023`，cost未返回；只提交一次。
- v4回读未见火花、木屑、震动线等特效，缩颈读感比v3明显；同时模型带动了头部、肩甲和盾牌/构图，变化比“只做颈肩局部微调”的目标更大。用户随后反馈“很好”，并要求以v4推导视频词，现已作为六秒视频的承力参考；这不是视频动作、透明帧或部署验收。K2未另行生成。
- v4原图及v3对照：`Library/Locus/tmp/enemy1021_shield_guard_keyframes_v4/K1_V3_V4_REVIEW.png`，上排为相同完整画布50%预览，下排为相同颈肩区域裁剪的100%对照；未做姿态对齐或像素修改。完整Prompt、请求、脱敏响应、核验位于输出旁 `.request.json`、`.response.json`、`.result.json`；配置与脚本位于 `Library/Locus/tmp/enemy1021_shield_guard_keyframes_v4/`，脚本已执行，不要重跑。

### 最新 Attack 任务（2026-10-11，已接入第一关并获用户验收）

- 用户审阅“稍稍侧身用盾牌撞击前方”的中英文提示词后明确“发起生成”；已提交一次 `seedance-2-fast` 4秒/720p/1:1、无音频无水印、seed=-1 视频任务。
- Task ID：`task_941e179a2fbd4ee080d0e7c66022469b`，accepted UTC `2026-10-11T06:29:01.914241+00:00`；首轮12次查询仍 `generating` 后停止。用户随后要求下载，本轮UTC `2026-10-11T06:43:56.565016+00:00` 观察到success，已下载同一任务，未重新生成；不要再运行创建脚本。
- 参考为已验收 `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3_green_v1.png`，同一图一次上传后用于首尾帧；允许躯干小幅侧转，镜头与脚底锚点固定。完整请求 `C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_attack_v1.request.json`；提交提示词SHA-256 `fdcb8bff319094b28bc169ea62c3e86fce30b15b28e0af8fb94f9107a014828b`。
- 运行记录：`Library/Locus/tmp/enemy1021_attack_video_v1/runtime.json`；下载报告 `C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_attack_v1.download.json`；分段与续传清单 `Library/Locus/tmp/enemy1021_attack_video_v1/download_manifest.json`。首次查询TLS超时后换curl成功；末段超时后只补缺失615,900 bytes完成下载，已验证远程对象/ETag/精确Range。
- 实际成片 `C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_attack_v1.mp4` 已落盘，2,476,908 bytes，SHA-256 `ea41b8a47b01b4d9ddea455222d5653f5d93184a4512b5c9ec99e97e0838b404`；960×960/24fps/97帧、4.041667秒、无音频，MP4容器及完整解码通过。下载完成UTC `2026-10-11T06:48:25.843527+00:00`；原任务 `cost.spend=0.48888`（币种未知）。检查接触表只是文件诊断，不是生产选帧。
- 用户已确认12帧候选并授权抠图和对齐；顺序为先选有效帧供用户验收，再抠图，最后统一尺寸/锚点。选中零基idx：000/024/032/036/040/048/056/064/072/080/088/096，原始97帧、12帧RGB候选与未对齐SVL透明版均保留。
- **当前交付使用v2**：12张960×960 RGBA在 `Library/Locus/tmp/enemy1021_attack_video_v1/frames/aligned_v2_960/`。以可见头盔细节的实际匹配尺寸估算整体倍率，按双靴底锚点对齐到 `(484.25,780)`；待机参考帧不缩放，顶撞阶段整帧缩放约0.814–0.822，不按轮廓总高fit。v1为已被替代的中间版，后续不要从v1部署。
- **验收素材与证据**：接触表 `Library/Locus/tmp/enemy1021_attack_video_v1/frames/ATTACK_ALIGNED_12_WHITE_REVIEW_v2.png`，处理前后对照 `Library/Locus/tmp/enemy1021_attack_video_v1/frames/ATTACK_ALIGNMENT_BEFORE_AFTER_v2.png`，GIF `Library/Locus/tmp/enemy1021_attack_video_v1/frames/ATTACK_ALIGNMENT_BEFORE_AFTER_v2_preview.gif`（0.12秒/帧，仅评审节奏）。报告 `Library/Locus/tmp/enemy1021_attack_video_v1/frames/alignment_report_v2.json` 和 `Library/Locus/tmp/enemy1021_attack_video_v1/frames/verification_report_v2.json`；12帧底边均780，重新测得双脚中点最大X误差1.75px，透明区RGB全0，Alpha0/255，边界完整。
- **素材验收与接入**：用户先明确“已验收，请接入然后等我验收”，已授权使用对齐v2；接入第一关后用户再次确认 Attack 已验收。十二帧按Idle相同固定偏移 `(74,91)` 零重采样补到1108×1108，部署为 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_attack1.png` 至 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_attack12.png`。新建 `Assets/Animations/Enemy_1021_Attack.anim`，替换 `Assets/Animations/Enemy_1021.controller` 的Attack motion；已有第一关配置 `Assets/RouteData/Stage01/N1_ArtPreview_Battle.asset` 包含1021，Prefab引用该Controller。
- **播放与接线记录**：Clip为30fps、2秒非循环，十二帧键位0/0.5/0.6667/0.75/0.8333/1/1.1667/1.3333/1.5/1.6667/1.8333/1.9667秒，`attack6` 接触姿态在1秒。额外包含 `ShieldVisual` 清空键；原Attack→Idle与受击转场保留。没有修改Prefab、攻击数值、脚本、命中帧声明或场景。
- **落盘证据与验收边界**：`Library/Locus/tmp/enemy1021_attack_deploy_v1/preparation_report.json` 与 `Library/Locus/tmp/enemy1021_attack_deploy_v1/asset_verification.json` 已回读，十二个root Sprite引用完整、源像素补边后保持一致、Clip/Controller保存完毕。用户已验收 Attack；本轮未进入Play Mode、未执行运行或画面自检。后续在其他对话继续1021敌人动画与机制完善。

### 0.0 最新 Walk 进度（2026-10-11，优先于下文早期Idle阶段快照）

- 用户已明确验收Walk视频与六帧，并要求“已验收，请开始抠图”；任务 `task_2d7741be5ad14839b33f12f061c6b606`，视频 `C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_walk_v1.mp4`，960×960/24fps/97帧，SHA-256 `2d20f36dc068c73c0710971b69c8f1ea8c650e809f787921eee2baef636fbc58`。
- 已解码97张原始RGB帧至 `Library/Locus/tmp/enemy1021_walk_v1_frames/clean_raw/`，编号f001..f097为1基。下半身固定ROI在约11帧间隔重复，选中段完整周期六帧：**f040/f042/f044/f046/f048/f050**，排除首尾Idle与重复姿态。
- 候选目录 `Library/Locus/tmp/enemy1021_walk_v1_frames/selected_v1/`；选择/哈希清单 `Library/Locus/tmp/enemy1021_walk_v1_frames/selected_frames_v1.json`；编号接触表 `Library/Locus/tmp/enemy1021_walk_v1_frames/WALK_CANDIDATES_v1_6frames.png`；循环GIF `Library/Locus/tmp/enemy1021_walk_v1_frames/WALK_CANDIDATES_v1_loop_0p6s.gif`。
- 原始候选960×960RGB、未裁切缩放去背，哈希与对应解码帧一致；接触表/GIF仅采用同一固定预览ROI。GIF每帧0.1s/循环0.6s，只是评审候选节奏。
- **透明六帧已完成并部署，用户已确认验收**：现有SVL本地Chroma自动取背景色，容差79/softness16/halo1/半透明转不透明；透明源文件在 `Library/Locus/tmp/enemy1021_walk_v1_frames/keyed_ui_t79/`，与源帧同名，960×960 RGBA。半透明/幕布残留/封闭孔均0；保留原跑步起伏，没有逐帧缩放、裁切或拉齐脚底。没有收费生成或新安装。
- 白底+棋盘格图 `Library/Locus/tmp/enemy1021_walk_v1_frames/WALK_MATTED_v1_6frames_white_alpha.png`；循环GIF `Library/Locus/tmp/enemy1021_walk_v1_frames/WALK_MATTED_v1_loop_0p6s.gif`；SVL指标报告 `Library/Locus/tmp/enemy1021_walk_v1_frames/svl_matte_walk_v1_report.json`；完整核验 `Library/Locus/tmp/enemy1021_walk_v1_frames/walk_matting_verification_v1.json`。
- **Walk 部署完成**：透明源帧按 Idle 相同固定偏移 `(74,91)` 零重采样补边至1108×1108，部署为 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_walk1.png` 至 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_walk6.png`。`Assets/Animations/Enemy_1021_Walk.anim` 为30fps、0.6秒循环、六个唯一精灵，含 `ShieldVisual` 清空键；Controller 的 `Walk` motion 已替换，Prefab 与 Idle Clip 保持部署前字节一致。
- **留档**：`Library/Locus/tmp/enemy1021_walk_deploy_v1/preparation_report.json`、`Library/Locus/tmp/enemy1021_walk_deploy_v1/asset_verification.json`、`Library/Locus/tmp/enemy1021_walk_deploy_v1/runtime_verification.json`。用户提出新边界前已完成受控对象池实例测试（六帧、两次前进一排、回 Idle、同实例复用）；这不是自然波次调度回归。后续画面对照已停止，不追加自检。

### 0.1 当前任务与完成边界

- **继续目标**：为敌人 1021 制作正式美术，先完成定妆与 Idle，再分批制作其他动作；每套动作分别完成视频、选帧、透明处理、对齐与 Unity 部署，各阶段单独验收。
- **已完成**：用户明确说“idle形象已验收”；`C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3.png` 是当前唯一获验收的 1021 身份与持盾姿态锚点。不要重新设计体型、头盔、护甲或盾牌来贴合早期提示词数字。
- **已接入动作**：Idle四帧（0.96秒循环）、Walk六帧（0.6秒循环）、Attack十二帧（2秒非循环、1秒接触姿态）与ShieldGuardHit六帧（0.30秒非循环、播完回Idle）。Walk、Attack与ShieldGuardHit均已获用户游戏验收；正式HitFlash/HitRight、ShieldBreak、死亡/击飞/胆怯动作及独立掉盾接管留待后续。
- **最新用户请求与授权**：ShieldGuardHit视频、六帧选帧与透明v3逐阶段验收后已完成补边、导入、Clip与Controller接线；用户现已确认游戏验收通过，并要求“更新交接文档准备在其他对话继续任务”。本次范围仅为文档与验收留档，不启动新制作、工程改动或自检。
- **最新工作环境快照**：工作目录 `C:/threeKingdomSlayer/threeKingdomSlayer`，分支 `route-scroll-movement`；最近 Unity 公告为 Edit Mode，Active Scene `Assets/Scenes/Battle.scene`。新对话先读取最新公告和 `git status`，不要把快照当作持续有效状态。

### 0.2 其他对话继续时的未完成项

本轮只更新交接，用户将在其他对话指定后续动画或机制目标。以下是已有方案中的剩余工作，不是本轮自动实施清单，也没有确定下一套动作的制作优先级。

| 方向 | 未完成内容 | 继续时的约束与入口 |
|---|---|---|
| 格挡附加反馈（可选） | 独立格挡特效/音效，尚未制作 | 正式 `ShieldGuardHit` 已接入并获游戏验收，不再列为未完成；动作素材保持无特效，独立反馈另按用户后续请求确定 |
| 破盾与掉盾 | 正式 `ShieldBreak`、独立盾精灵/掉落对象、脱手帧接管及生命周期 | 盾在本体帧内直到脱手，引擎随后接管；当前 `breakAnimationDuration=0.46s`，终态接无盾 `CowardIdle`；防止双盾和残盾 |
| 受击/死亡/击飞 | `HitFlash`、反侧 `HitRight`、正式 `Dead`、`Launched_Rise/Fall/Getup` 及胆怯家族 | 持盾侧 `HitLeft` 按既定规则并入格挡，不另做；起身/眩晕是否补充仍待用户决定 |
| 精确攻击 | 跟手瞄准、枪尖与命中点同源、盾区参数及可视化 | 设计已定但未实现；链路和边界见 `design/enemy-1021-shield-guard.md` §4.4 |
| 数据声明 | 独立 `breaksGuard` / `isPreciseAim` 字段 | 当前重攻击语义借用 `interruptsCavalryCharge`；后续解耦时保留骑兵行为和原资产数据，不维护招式白名单 |
| 机制完善与验收 | 减伤/破盾/胆怯手感、破盾期死亡或击飞、池复用、同列多1021等交互 | 见 `plan/enemy1021-minimal-playable-handoff.md` 与机制Design §12；Attack与ShieldGuardHit动作验收不代表这些机制全量验收 |

继续前先读本节、`memory/user-preference.md` 及相关Design，再核对实际代码/资产。后续动画顺序固定为：有效帧先交用户验收 → 正常SVL抠图 → 尺寸/锚点对齐 → 按明确请求接入第一关和对应动画 → 通知用户开游戏验收。用户未明确要求时不进入运行自检。

### 0.3 本机素材与留档

| 文件 | 用途与当前状态 |
|---|---|
| `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3.png` | 已验收定妆原图；1024×1024、RGB、983,338 bytes；SHA-256 `446d6d558baa7d4a5d6e6fd9bf712c07b99c63a1c4f2e711cc0b28a7564427d0` |
| `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3.request.json` | 已提交英文原词、参数、两图来源/处理/哈希；4,924 bytes；不是视频请求 |
| `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3.response.json` | 原始 HTTP 成功响应、图片 URL 与 usage；403 bytes，未返回费用或 task id |
| `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3_green_v1.png` | 获批视频参考副本；1024×1024 RGB，344,540 bytes；SHA-256 `d398a1eb7cd436f8082c6cf7dd7901e59d4f888e757f48b1ac01823f4adf40de`；同名 `.processing.json` 记录本地处理 |
| `C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_idle_v1.request.json` | 已提交视频词、参数、参考图哈希与 task ID；5,976 bytes；已回读核验 |
| `Library/Locus/tmp/enemy1021_idle_video_v1/runtime.json` | 运行与限次查询记录；创建脚本在 `Library/Locus/tmp/create_enemy1021_idle_video_v1.py`，已执行，勿再次运行 |
| `C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_idle_v1.mp4` | 已下载；2,000,007 bytes；SHA-256 `808593f9cbc5b9132ba7992f5ad62159f77be0ea6e4eb5c2feaa04b0c07fce2f`；960×960/24fps/97帧 |
| `Library/Locus/tmp/enemy1021_idle_v1_frames/keyed_ui_t79/` | 其他AI已完成的四帧透明素材；f001/f025/f049/f073；RGBA、半透明0、幕布残留0 |
| `Assets/Sprites/Enemy/Enemy1021/` | 已部署 `Enemy_1021_idle1..4.png`；1108画布固定偏移补边，保持现有1021之外资产不变 |
| `Assets/Animations/Enemy_1021_Idle.anim` | 已创建并接入Idle状态；4帧、30fps、0.96s循环，含旧ShieldVisual null键 |
| `Library/Locus/tmp/enemy1021_idle_deploy_v1/deploy_report.json` | 本次部署参数、哈希、Prefab与运行验证记录 |
| `Library/Locus/tmp/enemy1021_idle_deploy_v1/runtime_verification.json` | 自然生成、循环、旧盾清空和对象池复用的运行验证记录 |
| `C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_idle_v1.download.json` | 下载与容器/完整解码报告；同任务，usage/cost已记录 |
| `Library/Locus/tmp/enemy1021_idle_video_v1/contact_sheet.png` | 0/24/48/72/96检查帧接触表，已回读；不是正式选帧或循环验收 |
| `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle1.png` | 1011 主体画风与人物基底；后续动作以 1021 v3 为直接身份锚点 |
| `C:/Users/steam/Downloads/微信图片_20260928101134_12_223.png` | 用户盾牌构造/材质来源，仅用于追溯定妆，不作为新动画的姿态锚点 |
| `Library/Locus/tmp/enemy1021_shield_hold_v3_inputs/` | 本次两张实际上传副本；当前存在且哈希匹配，可能被清理，重建方法见 §3.2/请求 JSON |

交接前已再次检查：上述 PNG 和两份 JSON 可读，原图与已记录哈希匹配；请求英文原词 SHA-256 为 `246e7766ee2b916fdc2070a91d62b9b9a691cdfc43c7f3e8e433ae5b67eec4be`。图片和 JSON 在本机用户目录，**未复制进项目、未成为 Unity 资产，也不随 Git 自动迁移到另一台机器**；换机器时先确认文件可用，不能因本机路径缺失自行重新收费生成。

### 0.4 已确认约束与本轮教训

- **人物**：1011 本人改剑为盾，共享体型、头盔和甲胄；保持 Q 化比例、严肃战斗感、冷铁灰/朱红/暖棕/暖金关系。不能把 1011 降为泛泛“风格参考”，再另画一个盾兵；不再上传 109 重装骑兵或早期失败候选定义本角色。
- **姿态**：角色右手对应画面左侧，盾已举在胸腹前方偏左，手/手指/前臂/握把藏在盾后；画面右侧左手保持空垂，头盔/面罩/眼光可读。“固定持盾”指不做举起/收回切换，不是禁止举盾应战。
- **盾与画风**：沿用已验收 v3 的厚木板、暗铁件、铆钉、硬边体块与阶梯轮廓，无参考图红黑纹章。不要为了显示握持而重新露出手指，不写固定两三色、2～3 像素铆钉或红色占比 3% 等无依据限制。
- **比例判断**：先与原图并排看头/肩/躯干/腿的关系；画布占比不等于头身比，低 Alpha 外围不能当主体尺寸。前面“4.5 头身”“原图 669px 所以输出 540px 比例错了”等推断已撤回，不作为制作要求。
- **生成纪律**：所有 GPT Image 请求固定 `quality=max`，编辑/融合另用 `input_fidelity=high`；参数不保证姿态或像素锁定。先呈现中英文词供审，实际提交可只用获批英文，禁止“逐像素不变”的未经验证承诺。
- **盾生命周期**：持盾画在本体帧内；ShieldBreak 表现脱手后由引擎驱动独立盾对象。正式脱手帧、盾精灵接管、死亡/击飞打断与池回收仍待实现，不恢复旧分离持盾层路线。
- **范围保护**：机制、数值、现有状态名契约和 102 的资产保持既定方案。Idle已部署，Walk、Attack与ShieldGuardHit已接入并由用户游戏验收；后续在其他对话继续未完成动画与机制，不重复制作已验收动作，不额外改全局画风文档或 Skill。

### 0.5 工作树与后续未决项

- 当前已有改动需保留：`Assets/Scenes/Battle.scene`、`Assets/RouteData/Stage01/N1_ArtPreview_Battle.asset`、`Assets/Scripts/Enemy/Enemy.cs`，以及新增的 `Assets/Scripts/Enemy/ShieldGuardEnemy.cs`、`Assets/Resources/EnemyPrefabs/Enemy_1021.prefab`、`Assets/Animations/Enemy_1021.controller` 与相关 `.meta`。1021 两份 Design 和本交接文件也尚未 Git 跟踪；不要因清理任务而丢弃这些既有工作。
- Walk六张、Attack十二张、ShieldGuardHit六张Sprite及对应Clip已导入，Controller中各自motion已替换。ShieldGuardHit部署路径与留档见§0首节。以上接入没有提交Git、保存场景或修改Prefab/攻击脚本/数值；相关1021资产目前多为未跟踪状态。新对话重新检查实际工作树，保护并行改动。
- Idle四帧、Walk六帧、Attack十二帧与ShieldGuardHit六帧均已部署，Walk/Attack/ShieldGuardHit游戏验收通过。其他正式动作及独立掉盾对象接管等待后续对话；默认不运行自检。
- 更多动作与机制契约分别见 `design/enemy-1021-animation.md`、`design/enemy-1021-shield-guard.md`；详细生成台账见 `memory/video-generation-case-log.md` 的“Enemy 1021 持盾 Idle 定妆图 v3”。

可复制到新对话的开场指令：

> 请读取 `plan/enemy1021-art-production-handoff.md`、`plan/enemy1021-minimal-playable-handoff.md` 和 `memory/user-preference.md`，继续1021敌人动画与机制完善。Idle已部署，Walk、Attack和ShieldGuardHit均已接入第一关并由我游戏验收；Attack为12帧、2秒非循环、接触姿态在1秒，ShieldGuardHit为6帧、0.30秒非循环、播完回Idle。结合两份1021 Design与实际工程核对剩余目标，Design中旧进度以本交接最新验收状态为准，已验收动作不要重做。下一套动作或机制由我指定；生成前先提交完整中英文词与参考/参数供批准，有效帧先交我验收，再抠图及按需对齐；部署只接入第一关与对应动画，完成后通知我开游戏检查，未明确要求不要运行自检。

---

## 1. 本次要产出什么

按优先级交付，每步都需用户目检验收后才进入下一步：

| 阶段 | 交付物 | 说明 |
|---|---|---|
| ① 形象确定 | **1021 的 Idle 定妆图（单帧）** | **已验收**：`enemy1021_shield_hold_v3.png`；作为后续动作身份锚点 |
| ② Idle 动画 | 已部署4帧透明PNG与Idle Clip | 0.96秒循环；沿用现有素材，不按早期6帧建议重复制作 |
| ③ 其他动作 | 按 `design/enemy-1021-animation.md` §2/§3.2 的清单分批 | **Walk/Attack/ShieldGuardHit已完成并获游戏验收**；后续HitFlash/HitRight、**ShieldBreak**、Dead / 击飞三阶段与胆怯家族（可先复用102） |
| ④ 可选 | 眩晕、起身（`Launched_Getup`/`CowardGetup`） | 主文档 §10 待拍板项 |

**不在本次范围**：机制/数值改动、控制器接线逻辑改动（状态名是契约，换 clip 不需要改代码）、102 的任何资产。

---

## 2. 开工前必读（按顺序）

| 文档 | 作用 |
|---|---|
| `design/enemy-1021-shield-guard.md` | **机制权威**：方向分类、固定减伤、破盾与胆怯、形态状态机 |
| `design/enemy-1021-animation.md` | **动画需求**：逐状态 × 动画矩阵、需新做的清单与规格、Animator 待补状态、时间对齐规则、盾的表现方案、验收清单 |
| `design/pixel-character-and-enemy-animation-spec.md` | 硬性规范：正面像素角色、完整在画布内、脚底锚点、动作语义（**注意 §4 的盾位与本敌人相反，见 §6.1**） |
| `design/art-style-guide.md` | **画风基准**：两张概念图 + 已验收 1011 六帧 Idle（§2.2、§3.1 体型与剪影、§3.2 材质） |
| `skill/workflows/character-hit-animation-video-workflow.md` | 视频生产 SOP（提示词 5 层结构、参考图分工、防超框、验收、部署） |
| `skill/workflows/image-asset-generation.md` | 图像生成/编辑/去背 SOP，含**尺寸与密度约定**与两级验收标准 |
| `skill/gpt-image-generation.md` | GPT Image 接口能力与**路由指纹**（决定能否拿到原生透明） |
| `skill/reachapi-seedance-video-generation-v2.md` | Seedance 视频接口与下载方法 |
| `skill/workflows/local-green-screen-cutout.md` | 原生透明不可用时的本地绿幕抠图兜底 SOP |
| `memory/hit-animation-video-pipeline-lessons.md` | **9 条实测教训**（提示词 5 层、正面视角三条铁律、收费纪律等） |
| `memory/video-generation-case-log.md` | 任务台账：每次的参数/费用/文件/结论（追加新任务用） |
| `memory/enemy1011-animation-prompt-archive.md`、`memory/enemy1011-animation-pose-reference.md` | 本机可直接读取的 1011 实际视频原词与姿态经验；Idle 参考主读档案 §1，改掉持剑/无盾等不适用条款，不直接整段照搬 |
| `plan/enemy1011-animation-deployment-handoff.md` | 已验收的**接入约定与基线**（画布、导入设置、锚点、命名、脚底/血条数值） |

用户流程纪律（来自教训文档 §9，必须遵守）：关键姿态参考图先给用户目检；提交前把提示词的**中文对照版**贴进对话；收费任务**不自行重试**；交付一律贴**完整路径**。

---

## 3. 形象确定的历史记录（当前部署状态以 §0 为准）

### 3.1 必须锁定的设计点

1. **盾在画面左侧**（敌人**右手**持盾）——这是机制的前提（`Left` = 力来自画面左 = 盾侧）。**注意这与 102 的占位美术相反**（102 的盾在画面右侧、即角色左手），详见 §6.1。
2. **一眼能与 102 区分**：1021 是正式盾兵、102 是实验品，二者会在同一波次并存。当前以已验收 v3 的盾位、木盾、1011 人物基底与持法区分；不再为区分兵种另换头盔、加重护甲或添加盾面纹章。
3. **用户已确认盾的制作方案**：持盾时盾画在角色本体帧内；ShieldBreak 动画表现角色脱手，之后由引擎驱动独立盾牌掉落。不制作 Animator 驱动的分离持盾层；Idle 保持已经举盾应战的姿势，不制作举起/收回盾的状态切换。
4. **人物基底已定**：直接以 1011 为基底，将持剑改为持盾；共享体型、头盔和甲胄设计，保留大头盔、宽肩、紧凑躯干、短腿及冷铁灰、朱红、暖金的绘制语言，不另行设计更重或更轻的装备。
5. **已验收 Idle 语义**：双脚站稳、正面应战；盾位于胸腹前方偏画面左侧，持盾手、手指、前臂和握把藏在盾后，头盔、面罩和眼光保持可读；另一只手（角色左手、画面右侧）保持原图**空垂**。允许轻微呼吸/缨带随动，不迈步、攻击、切换盾位或起尘。具体外观与盾形以已验收 v3 成图为准。

### 3.2 已验收定妆图与实际生成来源

- **已验收文件**：`C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3.png`；1024×1024、RGB PNG、983,338 bytes、浅灰背景。用户已验收 Idle 形象，尚未去背、对齐或导入 Unity。
- **人物主参考**：`Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle1.png`，负责同一角色身份、头身关系、装备、镜头角度、受光和像素体块；不是泛化的风格参考。实际上传副本合成到 `#E6E6E6`，保留原 1108×1108 画布，未去剑或缩放。
- **盾牌局部参考**：用户提供的 `C:/Users/steam/Downloads/微信图片_20260928101134_12_223.png`，裁剪区域 `(685,575,1024,1120)`（左、上、右、下），339×545；仅借用木板、铁件、铆钉的构造与材质，不继承红黑纹章、人物、握持姿态或场景。局部副本保留原图案，由提示词明确排除。
- **实际上传副本**：`Library/Locus/tmp/enemy1021_shield_hold_v3_inputs/01_1011_idle1_lightgray.png` 与 `Library/Locus/tmp/enemy1021_shield_hold_v3_inputs/02_shield_material_detail.png`；临时目录可能被清理，来源与处理方式同时保存在请求 JSON。
- **生成记录**：`POST /v1/images/edits`、`gpt-image-2.5-sunburst`、`quality=max`、`input_fidelity=high`、`size=1024x1024`、PNG、`n=1`；只提交一次获批英文提示词，HTTP 200，无自动重试。完整原词和参数在同名 `.request.json`，原始响应在 `.response.json`；用量与哈希见 `memory/video-generation-case-log.md`。
- **当前外观锚点**：后续动作沿用 v3 的体型、头盔、甲胄、红缨/围巾、木盾和举盾遮挡关系；副手保持空垂。具体盾高、底部收窄和材质以用户验收成图为准，不为追求早期比例数字自动修图或再生成。
- **早期方案已被取代**：1011+109 的两图参考和 `enemy1021_idle_keypose_v1/v2.png`、`enemy1021_shield_hold_v1/v2.png` 均属于探索版本，未成为身份锚点。后续不继续上传 109 或失败候选来定义 1021。
- **验收边界**：本次仅确认单帧形象；Idle 视频、循环连贯性、透明帧、锚点对齐、导入参数与运行时表现分别验收。不得将 HTTP 200、PNG 可读或单帧通过写成动画/部署完成。

### 3.3 定妆图验收要点（同时满足才算过）

- 正面朝向、无侧身/3/4 视角；盾在**画面左侧**且持握关系清楚。
- 与 1011 同级的厚重度与像素密度；材质按 `art-style-guide.md` §3.2（金属冷灰蓝灰 + 锐利反光、红布大色块、木盾有板缝与铁件）。
- 缩小到游戏尺寸仍能读出"盾兵"；剪影不被细节塞满。
- 装备、配色、比例在后续动作里必须保持（动作参考图只改姿态，不重设计角色）。

---

## 4. Idle 视频与早期规格记录（实际已部署4帧，见 §0）

### 4.1 视频

- 参考/锚点：已验收v3原图仍是身份与姿态基准；首尾使用获批的独立绿幕副本，未覆盖原图。五层结构双语词已批准并提交，v1 task现为success，视频已下载并完整解码；成片待用户验收，不重新创建收费任务。
- **本次实际规格**：获批请求 `seedance-2-fast` / 4s / 720p / 1:1 / 无音频无水印 / seed=-1；1021 v1实测960×960、24fps、97帧、容器时长4.041667秒，末帧PTS=4.000秒，音频流0；API返回cost.spend=0.48888。这些有本次响应/解码证据，不是沿用1011历史值。
- **背景已确认**：已验收 v3 浅灰原图保留；本地另存 `#00B140` 绿幕副本作为 v1 的首尾参考，不涉及收费生图。背景纯度、画布和保留像素已检查，细浅灰边限制已告知用户；视频绿幕稳定性仍需本次成片检验。

### 4.2 取帧与抠图

- 取帧：先读取本次视频的实际尺寸、帧率与时长，再用 `ffmpeg` 解出 `clean_raw/f000.png…`；保留源视频的实际画布和背景，不标注、不裁切、不缩放，不预设输出一定是 960×960 或绿底。
- 选 6 帧：先确认视频中的完整、稳定呼吸周期，检查脚底、盾位、尺寸和首尾接缝；周期内可均匀取 6 个代表姿态，不能对整段视频机械等距取帧后假定无缝。1011 的 6 键/每键 0.1s/循环 0.6s 仅为接入参考，1021 最终节奏需按实际选帧和用户验收确定。
- 抠图：先发 **1 次探针**（`background=transparent` + `output_format=png`），用**路由指纹**判断能否拿到原生 alpha（G 类 = 原生 RGBA；B 类 = RGB 无 alpha，立即停手）；不可用则转本地绿幕键（用户已验收 A2 组合：反混合去污染 + 2px 环颜色替换）。
- 对齐：保留源视频帧的原始几何关系，禁止按各帧包围框或高度分别归一化。若 GPT 去背引入缩放/位移漂移，先测量与对应源帧的差异，再按 SOP 采用**单一全局缩放 + 逐帧脚底锚点对齐**并检查残差；不能用逐帧伸缩抹掉呼吸/关节变化或掩盖源视频漂移。

### 4.3 补边与导入（沿用 1011 已验收约定）

| 项 | 值 |
|---|---|
| 画布 | 补边到 **1108×1108**（角色保持原生像素、零重采样） |
| 脚底 | 自底 **210px**；脚心 x ≈ **549** |
| 补边方式 | **源画布居中补边**（不要"脚心锚到画布中心"，1011 因此整幅画偏移约 0.74 世界单位、看起来差一列） |
| 导入 | PPU **16**、Sprite Single、Center pivot **(554,554)**、Filter **Point**、Compression **Uncompressed**、Alpha Is Transparency、Mipmaps off、Mesh Tight |
| 命名/路径 | `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_idle1..6.png` |
| Clip | `Assets/Animations/Enemy_1021_Idle.anim`（6 键 @0.1s、30fps、Loop 为 1011 基线，1021 键间隔与状态 speed 待验收确定）；**替换 controller 里 `Idle` 状态的 motion，不改代码** |

**Prefab 数值必须同步**（当前 1021 预制体是 102 的拷贝：scale 0.2、血条 yOffset 0、碰撞盒 local 2.787×7.149）：

- 若角色画到与 1011 同级高度（约 606px / 1108 画布）：`scale ≈ 3.15×16÷身高px ≈ 0.0832`；碰撞盒与血条可直接沿用 1011 的验收值（碰撞盒世界 0.502×1.287×0.036、中心 y 0.067；血条 `yOffset = 35.28`、世界高 2.934）。公式与细节见 `plan/enemy1011-animation-deployment-handoff.md` §7 与 `skill/workflows/image-asset-generation.md` 的尺寸密度约定。
- 这些数值**必须在新素材到位后实测复核**，不能照抄；改分辨率而不改 scale/碰撞盒/血条会出现"看起来变了一个兵"的错误。

---

## 5. 第三步：后续动作分批

每个动作按用户确认的顺序推进：**写五层中英文提示词 → 用户批准生成 → 视频交用户验收 → 选有效帧交用户验收 → 正常SVL抠图 → 尺寸/锚点对齐交用户验收 → 按明确请求补边并接入第一关 → 通知用户自行开游戏验收**。若另需生成关键姿态参考图，先单独交用户验收。未明确要求时不运行自检。

| 批次 | 动作 | 提示词/流程要点 |
|---|---|---|
| A | `Walk`、`Attack`（已验收）、`HitFlash`、`HitRight`（待制作） | Walk 要"向玩家前进"、**手臂摆动是主读点**（不能锁死手臂，1011 Walk v1 因此被否决）；Attack 按"起手→蓄力→命中→收势"，命中帧放中部；HitRight 是从画面右被打（全额侧） |
| A+ | **`ShieldGuardHit`（游戏验收通过）**、**`ShieldBreak`（待制作）** | 格挡沿用当前已验收六帧；掉盾见下方专项 |
| B | `Launched_Rise` / `Launched_Fall` / `Launched_Getup` | 击飞语义见规范 §7.KnockUp；Rise 非循环、Fall 循环、Getup 非循环 |
| C | 胆怯家族 `CowardIdle/Hit/Dead/Launched` | 可先沿用 102 的现有帧（已能读），有余力再重做；注意现有 `CowardLaunched` 用的是死亡帧，读感差 |
| D | 可选：眩晕、起身 | 非 Boss 目前全无眩晕表现，属通用缺口 |

### 5.1 `ShieldGuardHit`（格挡）专项

- **当前状态：六帧正式动作已接入并获用户游戏验收，不再重新设计或生成。** 角色右手持盾（画面左侧），接住左侧来力，盾向画面右侧小幅压入，上身轻微右倾、缩颈耸肩后回弹，双脚与根位置固定、无后仰；动作素材无特效。该动作同时服务盾侧与正面；不得用早期“禁止任何侧倾”描述推翻已验收姿态。
- 卡肉会禁用 Animator（格挡走 Light = 45ms），**首帧必须已经是"盾面接触"**，不能是渐变起手。
- 已部署时长0.30s，六帧、30fps非循环，末帧恢复站姿，出边exit time 1 → Idle；路径与键位见§0。
- 不需要左右两个变体（盾侧受击已并入该动作，理由见 `design/enemy-1021-animation.md` §7）。

### 5.2 `ShieldBreak`（站立掉盾）专项

- **长度必须等于 `breakAnimationDuration`（当前 0.46s）**；首帧要能从 Idle / 受击姿态接上，**末帧必须是"无盾踉跄/空手"站姿**（播完代码立刻切 `CowardIdle`）。
- 表现内容：盾脱手/被打飞 + 本体踉跄，**不要**用"被击飞"的动作语言（102 的 `CowardTransition` 就是被击飞语言，仅作占位）。
- 期间普通受击不播受击动画（代码抑制），所以这段**不需要受击变体帧**；但死亡/击飞会被 AnyState 切走，关键姿态不要放尾部。
- 用户已选择“帧内绘制盾 + 引擎驱动独立掉落”方案：视频中盾随本体动作直至脱手，脱手后正式本体帧不继续绘制飞落盾；掉落由引擎接管。本体帧与独立盾对象必须在脱手帧衔接，避免双盾、提前消失或重新出现。具体脱手帧和代码接管时机留待 ShieldBreak 制作/部署时确认，本轮未实现。

---

## 6. 已知风险与坑（都踩过，逐条照做可避免）

### 6.1 盾位：与 102 相反（最容易做错的一条）

- 机制要求 **盾在画面左侧（敌人右手）**；而 `pixel-character-and-enemy-animation-spec.md` §4「Enemy2 盾兵基准」描述的 102 是"盾位于角色左侧，即**画面右侧**"，102 的掉盾 clip 里盾的位移也是朝 +x。
- 结论：**规范 §4 描述的是 102 的旧盾位，不能照抄**；1021 要刻意镜像到画面左侧。新对话若直接用 102 的盾帧只会得到反向的盾。
- 连带：盾会向画面左侧伸出，**横向占位可能超过列距 1.0 世界单位**（1011 的剑就有这个问题），这是美术固有事实；要么接受跨列读感，要么在美术上收窄盾的横向占位。

### 6.2 生成与抠图

- GPT Image 的 `background=transparent` **随后端路由变化**：同参数提交可能拿到 RGB + 画入的底 → 必须"先探针、按指纹判、不可用立刻停手"。
- GPT 去背可能逐帧改变角色缩放与位置，须与对应源帧比较；Idle 和受击均不按每帧高度分别归一化。采用单一全局缩放与逐帧脚底对齐后仍需量化残差，保留原始动作几何，不以处理后稳定代替源视频验收。
- 绿幕视频的本地抠图：边软/压缩幽灵会让包围盒虚胖；按 `local-green-screen-cutout.md` 的 SOP 走，并**与已验收基线比**（1px 环亮度比 idle 原生 alpha 基线约 0.47–0.61），不要用绝对值下判。
- 交付必须保留"对齐前/对齐后"两版候选与对照表，便于回溯。

### 6.3 视频提示词

- 必须分 5 层写（情景/动作语义/量化读点/硬约束/节奏），缺一层就会得到僵硬平均解。
- **正面视角下"后仰"不可投影**：要用正面可见读点（双肩上耸、脖子压缩、胸腔压扁、膝盖受力微屈、武器惯性滞后），并显式禁止身体纵轴左右倾斜。
- **不要写"变宽/变厚"**：会被实现成整体放大（"充气"）。写死"任何时刻整体尺寸不得大于 idle"。
- 动作量大的动作（Walk/击飞/掉盾）**必须带"绝对防超框"段**（四边 ≥8% 空白、碰边就做紧凑），否则"动作到位"会以出框为代价。
- 行进/位移类动作**手臂摆动是主读点**并给度数区间；武器只给方向红线，不锁角度。
- 节奏：受力/发力只占 0.05–0.15s，其余是回弹——静止关键图读不出冲击感是正常的，别指望它解决节奏。

### 6.4 部署与验证

以下运行检查注意事项仅在用户明确要求自检时使用；普通部署完成后通知用户自行验收，不自动执行。

- `EnemyPool` 按**文件名**解析 id，预制体必须叫 `Enemy_1021.prefab`（`Enemy_102_1.prefab` 会被解析成 102 撞号）。
- Play Mode 验证时必须筛**激活中**的实例（`FindObjectsOfType<Enemy>(true)` 会先命中池里未激活的克隆，`Animator.layerCount=0`、`Play` 完全无效，极易误判成接线错）。
- 命中卡肉会临时禁用 Animator，**不能在命中后 1 帧读动画状态**（会误判"动画没播"）。
- 当前 1021 Prefab 仍保留从 102 复制来的 `ShieldVisual` 占位层；正式方案已选帧内绘盾、脱手后引擎驱动独立盾对象。部署时须确认旧分离层和旧 clip 不会显示残盾，并实测独立掉落对象生命周期、敌人池复用及死亡/击飞打断。是否清理/复位旧节点按接线现状评估，不因旧债务而恢复分离持盾层生产。

---

## 7. 收费与交付纪律（复用既有约定）

- 任何收费任务**不自行重试**；服务端 `failed` 且 `cost=0` 只说明未计费，不等于已获授权。
- 提交前把提示词的**中文对照版**贴进对话；关键姿态参考图先给用户目检。
- 每个任务完成后把 **task id / 文件完整路径 / 字节数 / 费用 / 结论**追加到 `memory/video-generation-case-log.md` 的"任务台账"（一次性数据），流程性结论写进 `skill/workflows/` 对应 SOP。
- 中间产物放 `Library/Locus/tmp/enemy1021_*/`（可清理，不作为正式资产）；正式素材进 `Assets/Sprites/Enemy/Enemy1021/`。

---

## 8. 开工顺序建议（新对话照此推进）

1. **先读 §0 继续任务摘要与实际 v3 原图**，再按 §2 查当前阶段所需文档；起草 Idle 视频重点读画风、1011 实际 Idle 原词与视频 SOP，实施机制/部署时再核对对应契约和当前 Unity 引用。
2. **形象定妆已完成验收**：身份锚点为 `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3.png`。人物以 1011 为基底改剑为盾，已举盾应战、持盾手藏于盾后、副手空垂；帧内绘盾与脱手后引擎掉落方案不变。生成来源见 §3.2，不再回到 1011+109 或早期失败候选。
3. **完成基线**：Idle四帧、Walk六帧、Attack十二帧、ShieldGuardHit六帧已接入；Walk/Attack/ShieldGuardHit已由用户游戏验收。不要沿用早期阶段快照重做已完成动作。
4. 后续按用户在新对话指定的动画或机制目标推进，先核对§0.2与实际代码/资产。动画先选有效帧交用户验收，再抠图和对齐；部署只接入第一关，未明确要求不运行自检。
5. 角色身份继续使用已验收v3，盾在画面左侧、帧内绘制；独立掉盾接管尚未完成，不能将Attack或ShieldGuardHit动作验收解释为破盾/胆怯等机制全量验收。
6. 每批完成后同步本交接与 `memory/video-generation-case-log.md`；Design文档中的规则继续作为约束，旧资产进度快照以本交接和当前工程为准。

---

## 9. 变更日志

- 初版：为 1021 形象与动画素材生产建立开工交接。汇总了必读文档清单、三步产出路线（形象定妆 → Idle 六帧 → 分批动作）、1011 已验收的接入约定与数值、1021 专属要点（盾位镜像、格挡/破盾两个专属动作的画法与时长契约）、六类已踩过的风险与收费纪律。
- 早期用户确认（参考输入已被本轮验收版取代）：A 型宽矩形木盾、空手副手、固定持盾姿势；盾在视频/本体帧内，脱手后由引擎驱动掉落。当时参考为 1011 与 109 Idle 原帧，未生图/视频；所有 GPT Image 请求统一 `quality=max`、编辑/融合 `input_fidelity=high`。
- 本轮 Idle 定妆验收：用户明确确认“idle形象已验收”；`enemy1021_shield_hold_v3.png` 成为身份与持盾姿态锚点。同步实际 1011 主参考 + 用户盾牌局部参考、已验收文件与阶段边界；Idle 动画、去背、对齐、Unity 部署和引擎掉盾尚未完成，本轮仅更新记录。
- 跨对话交接整理：新增 §0 的当前目标、已完成/未开始、授权边界、素材核验、1011 原词入口、下一步视频方案和工作树保护；纠正逐帧高度归一化矛盾，把背景与历史视频参数明确标为待审批/参考。当时未创建新生成任务、复制素材进 Assets、提交 Git 或修改 Unity 资产。
- 2026-10-10 Idle视频v1创建：本地背景染绿，用户批准参考图、双语词及参数后创建一次任务 `task_e169c149759b4216b75c879672389448`。首尾同图，4s/720p/1:1；初轮12次generating后停止自动轮询，当时无本地成片/usage/cost；保存请求与台账，未改Unity资产或去背。
- 2026-10-10 用户要求下载后：现有task success，4路Range下载83.808秒，2,000,007 bytes；容器及全部97帧解码通过，实际960×960/24fps/4.041667秒，cost.spend=0.48888。视频落盘哈希已回读核对，接触表保存0/24/48/72/96检查帧；当时待用户动画验收，未制作生产透明帧或部署。
- 2026-10-11 Attack交接收尾：Idle四帧、Walk六帧和Attack十二帧均已部署；用户确认Walk与Attack验收通过。Attack使用已验收对齐v2，2秒非循环、1秒接触姿态键。当时仅同步文档和验收留档，未改变Unity资产或启动运行自检。
- 2026-10-11 ShieldGuardHit交接收尾：用户逐阶段验收六秒视频、六帧选帧、透明v3后授权接入；六帧按现有固定偏移补边，创建0.30秒非循环Clip并接入1021控制器。用户随后明确游戏验收通过，要求在其他对话继续；已同步主交接、机制接续入口、素材台账与部署留档，移除正式ShieldGuardHit待办，保留其他未完成目标。本次仅更新文档/验收记录，未修改Unity资产、启动生成或执行自检。
