---
id: kd_41b35b08-46a1-4917-b863-4d07b837439e
injectMode: inherit
summary: 需要制作像素角色受击、失盾及状态转换视频时使用；含参考图对齐、固定坐标、防超框、连续过渡、收费确认与美术验收。
aiEditMode: inherit
skillEnabled: true
skillSurface: command
---

# 游戏角色受击动画视频制作流程

## 1. 适用范围与验收边界

本流程用于为 Unity 像素角色制作受击与状态转换视频，供后续提取透明 PNG 动画帧。原「站姿 → 受击 → 恢复站姿」案例保留；「持盾 → 盾牌脱手淡出 → 后仰胆怯」三参考图流程见第10节。不可把原模板中的恢复 Idle、全程持盾、痛苦表情限制照搬到不同终态任务。

边界：任务创建成功、下载完成与 MP4 容器校验通过，只证明生成链路可用，不代表模型能保证像素级位置锁定，也不代表已完成游戏动画资产。逐帧定位测量、透明帧提取与 Unity 部署必须单独验收；历史任务 id、文件与费用见 `memory/video-generation-case-log.md`。

## 2. 开始前必须读取

### 统一美术与动画规范

1. `Locus/knowledge/design/pixel-character-and-enemy-animation-spec.md`：已归纳原始 TXT 的画风、朝向、兵种、动作语义、完整性、透明帧和质检规则，并区分通用规则与本次 Enemy2 原地受击要求。后续任务直接读取本项目文档，不再依赖桌面 TXT。

### 项目工具与补充文档

2. `Locus/knowledge/skill/reachapi-seedance-video-generation-v2.md`：视频 API 的实际参数、参考图角色、费用确认、有限轮询、续传和校验要求。
3. `Locus/knowledge/skill/gpt-image-generation.md`：需要准备关键图或图像处理时阅读，具体执行同时遵循当前会话工具规则。
4. `Locus/knowledge/design/skill-item-icon-art-guideline.md`：补充理解项目像素街机视觉语言；它是图标规范，不能将其中的构图、Pivot 等直接套用到角色。

### 必须实际查看的资源

- `Assets/Sprites/Enemy/Enemy2/Enemy_2.png`：通常形象、身份、装备和站姿。
- `Assets/Sprites/Enemy/Enemy2/enemy2_hitted.png`：受击姿势参考。
- 更换角色或动作时，读取对应实际资源，而非只看文件名或依赖文字记忆。

注意：`sprite-animation-spec.txt` 主要针对 Enemy1，含 `main.lua`、`backgroundFit="contain"` 和外部目录示例。其制作原则可迁移，运行时实现、画布尺寸、帧数和部署路径不可未经核查照搬到 Unity。

## 3. 核心理解：制作 Sprite 素材，不是电影镜头

- 全程固定画布、镜头、角色整体位置、绘制比例和脚底锚点。
- 只允许局部身体关节、躯干、手臂、头部及表情变化。
- 受击后仰可以改变轮廓形状，但不能通过逐帧缩放来维持包围框尺寸。
- 禁止角色整体平移、前进、后退、靠近镜头或远离镜头。
- 禁止镜头推拉、平移、跟随、旋转、震动、切镜与重新构图。
- 头盔、四肢、盾牌与所有配件必须全程完整可见，不能触边或超框。
- 安全空间应按最大动作展开范围预留；不能在动作进行中拉远镜头来补救。
- 本次要求双脚原地固定，不迈步、不滑动、不跳跃、不击飞、不倒地。

固定位置与比例是后续 Sprite 切帧、统一 Pivot 和游戏内稳定播放的基础，不是可有可无的视觉偏好。

## 4. 原受击恢复案例的参考图分工

本次成功使用两张普通 `reference_image`：

1. 图1 `Enemy_2.png`：严格约束角色身份、通常站姿、服饰、盾牌、配色及比例。
2. 图2 `enemy2_hitted.png`：约束受击姿势；不复制其背景，不借此重设计角色。

第一版只提供通常图，不能称为“按现有受击图制作”。必须实际上传动作参考图，并在提示词中明确两张图的职责。

采用 `input.content` 中两个 `image_url` 项，`role` 均为 `reference_image`。不要与 `first_frame` / `last_frame` 混用。本案例不是首尾帧工作流，也未额外上传第三张构图参考图。

## 5. 已验证的提示词模板（受击 → 恢复）

以下为已验证通过的英文提示词（角色为 Enemy2）；复用时替换角色描述、参考图职责与方向，并保留“固定画布、无镜头运动、不超框、原地受击”全部约束：

```text
Generate a 4-second game sprite animation, not a cinematic video. Use a locked orthographic front-facing camera and fixed 1:1 canvas. Camera, canvas, framing, character position, character scale, pixel density, and ground-anchor coordinates remain identical first to last. The entire character is fully visible every frame: helmet, head, both arms, hands, legs, boots, shield, accessories. Reserve at least 15% empty safety margin on every side. Never zoom, pan, track, recenter, reframe, rescale, or move the camera. This is an in-place sprite animation: no whole-character translation horizontally, vertically, forward, or backward; no approach or retreat; midpoint between feet fixed; both feet planted. Only local body articulation changes. Image 1 is the exact character identity and idle pose. Image 2 is only the exact hit-reaction pose reference; do not copy its background or redesign the character. Start in exact idle pose, receive one frontal invisible chest hit, match Image 2 with torso and shoulders recoiling backward and painful expression, hold briefly, recover through local articulation only, end in exact idle pose. No walking, stepping, hopping, sliding, falling, launching, attacking, or second hit. Preserve exact silver-gray helmet, red ribbon, brown armor, metal chest plates, wooden rectangular shield on character left (image right), proportions, equipment, colors, thick dark pixel outline, hard block shading, pixel density. No background, floor, platform, scenery, extra objects, text, UI, side view, three-quarter view, rotation, camera shake, perspective change, cuts, transitions, motion blur, painterly rendering, gradients, anti-aliased edges.
```

提示词表达的是约束目标，不是生成器的硬性保证。两张参考图与加强约束共同用于第二版，不能仅凭一次成功断言某一个关键词是唯一原因。

## 6. 实际执行步骤

### A. 准备与确认

1. 读取上述规范和实际图像，区分身份参考与动作参考。
2. 确认图片存在且可读取；检查 `REACH_API_KEY` 是否存在，但绝不打印、记录或提交 Key。
3. 向用户说明并确认本次收费生成的模型、时长、分辨率、比例、音频、参考图和输出路径。已有针对同一任务的明确确认应复用，不重复询问。
4. 采用版本化输出，保留失败版用于比较，不覆盖用户资源。

本次规格：

```text
model: seedance-2-fast
duration_seconds: 4
resolution: 720p
aspect_ratio: 1:1
generate_audio: false
watermark: false
seed: -1
output: C:/Users/steam/Videos/doubaoVideo/enemy2_hitted_return_v2.mp4
```

### B. 上传与创建

1. 每张图分别向 `https://file.reachapi.ai/file/uploads` 发出 multipart/form-data 请求，字段名 `file`。
2. 认证从环境变量读取；避免把 Key 展开到命令行参数中。使用 curl 时按视频 Skill 通过 stdin 配置传入认证。
3. Python urllib 上传在本次两轮均出现默认请求 403，而加入 `User-Agent: curl/8.0` 后成功。后续复用成功的请求头；此观察不能证明所有 403 都由 User-Agent 引起。
4. 若失败，先读取响应状态、具体错误内容及网络路径，脱敏后诊断。不能仅凭 403 宣称 Key 无效或权限不足。
5. 从成功响应读取 `data.url`，尽快创建任务，不把临时 URL 当永久资产。
6. 创建接口为 `https://direct.reachapi.ai/v1/vids/create`。使用新版 `input.content`，禁止旧版 `input.prompt`、`input.image_url`、`negative_prompt` 等字段。
7. 原案例按“完整文字提示词 → 图1 → 图2”提交，均使用 `reference_image`；三姿态流程增加图3并明确其尾态职责，不与 first_frame/last_frame 混用。
8. 仅在返回真实 `task_id` 后报告任务创建成功。创建请求超时而结果不明时，不盲目重发收费请求。
9. 保存 task ID、完整提示词、参数、参考图路径、输出路径；不保存 Key 或临时签名 URL。项目临时脚本和记录放在 `Library/Locus/tmp/`，不要像本次早期操作那样放系统临时目录。

### C. 查询与下载

严格遵循视频 Skill，而非照抄本次早期脚本中的不足：

- 首次立即查询；后续间隔5–10秒，最多12次，并用总截止时间约束不超过120秒，单请求最多20秒。
- 服务端 `success` 才从 `data[].url` 读取视频地址。超时或下载失败不自动重新创建收费任务。
- 下载写入同目录 `.part`，设置单轮总时限，支持断点续传；不在失败时清空已下载内容。
- 确认文件大于1KB、MP4 文件头、顶层 box 边界，以及 `moov` / `mdat` 等结构后，再原子改为正式 `.mp4`。
- 有 ffprobe 时补充媒体信息/解码相关检查；本次环境未找到 ffprobe，实际完成的是容器校验，不能声称完成了解码验证。
- 报告任务状态、真实完整本地路径、大小和 API usage/cost。费用不擅自推断币种。

## 7. 美术验收与后续部署

### 视频验收

- [ ] 外观与通常图一致，动作符合受击图，不是任意即兴动作。
- [ ] 全程无角色整体漂移、缩放、透视变化和镜头运动。
- [ ] 脚底锚点稳定，只有身体姿态变化。
- [ ] 最大后仰时所有轮廓仍完整，盾牌、头盔、手脚不触边或超框。
- [ ] 起始和结束姿势衔接，无额外攻击、迈步或第二次受击。
- [ ] 装備形状与像素密度稳定，痛苦表情不变成惊讶、滑稽或卖萌。

文件下载和容器校验通过，不等于美术合格。必须实际观看或抽帧检查后再称为可部署。

### 透明帧与 Unity 部署（独立步骤，需单独授权）

Seedance 视频 Skill 没有承诺 Alpha 透明视频，也没有提供透明视频参数。`no background` 只是画面要求，不能证明文件透明。

经用户认可后，再按单独授权范围进行：

1. 保存原视频并提取原始帧，选择清晰的受击极端姿态和恢复节点。
2. 逐帧去背、检查 Alpha 与装备完整性；不能把棋盘格当透明。
3. 统一画布、角色绘制比例和脚底锚点。禁止对每帧独立 trim 后按包围框缩放，造成动画忽大忽小；不要以逐帧中心居中替代固定脚底锚点。
4. 在 Unity 中读取实际 Sprite 导入设置、Animator、AnimationClip、Prefab 和伤害时序，保留 GUID 与引用，通过 Unity API 部署和验证。
5. 动画帧数与播放节奏以当前项目实际接线为准，不照搬 Enemy1 文档或本视频4秒时长作为游戏受击时长。

## 8. 结果记录

任务 id、文件、大小、费用以及版本否决/认可记录统一登记在 `memory/video-generation-case-log.md`；本文只保留可复用的流程与验收规则。使用本流程后，新的验收结论写回该记录，不要写回本文。

## 9. 复用时最重要的纪律

先读文档与图像 → 明确游戏素材约束 → 确认收费规格 → 实际上传对应参考图 → 创建并记录任务 → 有限查询和可靠下载 → 区分容器验证与美术验收 → 验收后才制作透明帧和部署。

不把读 Skill 当作已执行，不把上传成功当作任务成功，不把服务端成功当作下载完成，不把下载完成当作美术可用。

## 10. 三关键姿态驱动的状态转换（失盾案例经验）

### 参考图先验收，再准备上传副本

1. 先确认起始状态、中间受力状态、最终待机状态，分开验收形象与动作。失盾后的终态是后仰胆怯，不是拳击防御或前蜷。
2. 有已验收中间帧和尾帧时必须实际上传，不能只上传站姿并声称使用了三图。图片是 motion landmarks，不是要求视频硬切替换的静态画面。
3. 普通参考图模式可承载三图；它并不保证指定时刻精确锁帧。接口不允许 first_frame/last_frame 与普通参考图混合时，不能虚构中间帧锁定功能。
4. 对齐准备副本而不覆盖已验收原图。先统一原图分辨率和人物绘制尺度，再用脚底基线/中点放置；对整个参考组共同调整安全画布。禁止按每图包含盾牌的总包围框独立 trim/fit，这会造成角色忽大忽小。
5. 准备完成后查看对比图；检查两只脚、头部及装备尺度。脚底最低点相同仅是近似对齐，不代表左右脚坐标、骨盆和解剖比例完全一致。
6. 预留最高盾位、手臂展开和后仰所需空间。原中间图顶部很窄时，不能原样上传再指望模型不出框。静态副本可统一缩放准备；视频过程中仍严格禁止缩放。
7. 原图含武器而目标无武器时，先清理副本可见武器，保留手部并检查局部；不靠文字与强身份参考互相对抗。选择现成图时实际查看特效，不把带星芒/烟尘图无条件作为动作参考。

### 动作语义及经验边界

- 被动挑飞：盾先上移、手臂被牵引、手指随后松开，另一侧肩臂与膝盖产生补偿。手向上跟随，不为了避免主动抛掷而硬写回收胸前或整个人向另一侧倾斜。
- 必须点明非持盾侧肩、肘、手的变化；仅写“全身受力”容易得到身体仍 Idle、只有一只手变化。
- 失盾后：从痛苦反应过渡为胆怯，胸肩头向后撤、双掌前挡；不是握拳准备迎战，也不是胸口抱缩低头前弓。
- 根位置/双脚固定不等于全身冻结。肩、肘、腕、膝、躯干可以局部关节运动，轮廓变化不应触发重定中心或缩放。
- 单帧不能证明受力先后，静态姿态要与视频因果关系共同约束；不要把某一个关键词当成必然成功原因。

### 三图之间跳切时：先改过渡，不必重做参考图

本次用户认可保留三图，仅修改视频 Prompt。将图称为 motion landmarks；要求连续中间关节姿态、错时重叠反应、手臂弧线路径、腕指跟随、到位后小幅衰减收稳。避免所有部位同步跳变或逐个关节机械排队。不要用慢速预举盾来补时间，它容易被读成主动举盾。

可复用的6秒节奏（时间是提示，不是API硬约束）：

| 时间 | 动作 |
|---|---|
| 0–0.6s | 短暂持盾警戒，不作抛掷前摇 |
| 0.6–1.4s | 突然受力后连续演进：盾 → 手臂牵引 → 松手 → 另一侧补偿 |
| 1.4–1.7s | 中间姿态短暂可读，保留余势，不冻结 |
| 1.7–2.7s | 盾短距垂直减速上升、均匀淡出；手臂开始松动 |
| 2.7–4.8s | 延长收势：双臂沿弧线转换，睁眼变惊惧，胸肩后仰，动作互相重叠 |
| 4.8–6s | 小幅收稳后胆怯待机；不是完整首尾闭环的循环视频 |

过渡核心提示模板：

```text
The three reference images are motion landmarks, NOT three still pictures to display in sequence.
Use visible continuous in-between joint articulation. No pose snapping, hard cuts, crossfades or morphing.
Start the external upward shield impact abruptly, without a preparatory shield lift or throw wind-up.
The shield pulls the gripping arm upward; fingers release after the pull.
The free shoulder and arm respond slightly later and the knees cushion the force.
These responses overlap naturally, not all at once and not as robotic isolated steps.
Let the intermediate pose remain readable through moving follow-through, not a freeze frame.
During the extended recovery, lower the raised arm along a curved path through shoulder and elbow rotation.
The free arm bends and sweeps forward into an open-palm protective gesture.
Wrists and fingers follow with a small natural delay.
As the arms move, eyes open and pain becomes apprehension; chest, shoulders and head lean backward.
Arrive at the final fearful pose with a tiny damped settling motion of local joints, not root bobbing.
Keep drawing scale, camera and both boot contacts fixed. Do not recenter or resize as the silhouette changes.
```

### 盾渐隐与透明帧

本案例仅允许盾在脱手后短距上升、保持画内并均匀降低不透明度；人物保持完全不透明。禁止缩小、碎裂、烟雾、发光、飞出边缘或盾重新出现。物体“全程完整”指其仍可见时形体完整入框，不排斥经明确授权的淡出。

视频视觉淡出不等于Alpha视频。后续去背须保留盾牌渐隐的半透明边缘；静态不透明角色用的白底硬阈值删除不能直接用于这些渐隐帧，否则会突然跳没或形成白边。

### 验收、复用及交付边界

- 用户认可成片、容器校验、解码校验、逐帧位置测量、Unity部署是不同完成项，分别记录。
- 不新增参考图的重生成，应复用同一批文件并记录哈希，避免同时改图片和Prompt导致无法比较。
- 下载超时保留 `.part`；续传核对同任务对象与本地前缀，HTTP 206/Content-Range验证长度；完整容器验证后才改正式文件名。下载失败不是重新创建收费任务的理由。
- 优先使用现成图像库（如 Pillow）做图片准备；先检查工具身份。Windows `System32/convert.exe` 是磁盘工具，不是 ImageMagick；不要反复重试错误工具或手写PNG解码器。
- 胆怯循环须后续单独制作或提帧验证闭环；6秒转态视频末尾有待机不等于已经交付独立无缝循环。
- task ID、费用、验收图和成片路径只写台账 `memory/video-generation-case-log.md`，不把临时运行记录混入本流程。

## 11. 从已验收视频尾帧接续待机循环

### 提取真实尾帧，不回用原始尾姿态参考图

1. 对已验收的视频原文件核对路径/哈希，用PyAV等解码至最后有效画面；保存未裁切、未缩放的PNG，记录零基帧索引、PTS时间、实际尺寸与fps。不能用视频生成前的静态尾图代替实际视频尾帧。
2. 同时抽查末尾约半秒的连续画面，确认手、肩、表情和脚底状态，判断是否仍有收势运动；只取最后一张不能证明接续自然。
3. 保留原画布留白、背景色和角色像素位置。请求720p不代表实测尺寸一定720×720；读取实际输出后再准备输入。

### 循环生成分支（流程可用，结果仍须验收）

- 将同一张真实尾帧作为 `first_frame` 和 `last_frame`；可只上传一次并复用URL。不混入普通 `reference_image`。此分支是起止状态约束，不是中间全程位置或无缝保证。
- 最短4秒生成候选，围绕稳定姿态做一次温和呼吸周期与细微腕指颤动，不安排新攻击、再次受击、后退或回到持盾状态。
- 后仰、双掌前挡与胆怯神态以实际尾帧为准；不重新摆姿势、不放大填满盾牌消失后的留白。
- 根位置、脚底和绘制比例固定，但允许局部呼吸和关节变化。禁止全身上下跳动、夸张晃动和把胆怯变成主动拳击。

可复用提示核心：

```text
The first and last images are the SAME actual final frame of the preceding approved animation.
Continue directly from this exact pose, without resetting, recentering or rescaling.
Keep the backward-leaning fearful stance, planted boots and open defensive palms.
Perform one gentle nervous breathing cycle with tiny wrist and fingertip tremors.
Only local articulation changes. No whole-body bobbing, foot sliding, new flinch or gesture.
Return naturally to the starting pose with continuous movement direction and speed across the loop boundary.
No freeze-frame animation, rewind, crossfade, visible reset, weapon, shield or effects.
Preserve the source background and lighting without flicker.
```

### 循环与耗时验收

- 按技术Skill选择限时4路Range下载，分别记录上传、创建请求、观察完成等待、下载墙钟与总耗时；不靠延长单连接超时掩盖慢速。
- 完整解码后记录实际帧数/时长并输出接触表，检查起、中、尾的人物位置、比例、边界和装备。
- 首尾差值只是诊断：大面积白底会稀释全画布MAE，应补查主体ROI、两只脚坐标以及相邻帧运动方向。相同首尾输入也可能输出不同画面。
- 顺序检查“原视频末尾→循环开头”接缝和“循环末尾→循环开头”接缝，两处都需视觉与运动连续性验收。
- 不自动删除末帧；先检查是否重复端点导致停顿，再决定截取周期或处理接缝。不能用逐帧缩放修正漂移。
- **用户肯定速度/生成水平不等于接受循环成片。** 明确标记待验收，不用技术成功升级美术状态。当前此分支仅有候选生成、完整下载/解码和抽帧观察证据；无缝循环及部署须另验收。


