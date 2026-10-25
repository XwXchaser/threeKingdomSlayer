---
id: kd_d6bac81e-8fb5-4366-9b2b-f020984513b9
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: 1021新画风替换：v8（收窄盔壳＋护片遮耳，脸与身体沿用v7/v5）已由用户于2026-10-25验收，定为1021 新画风身份定稿；v1–v7转为历史。§17 记录已验收图规格与键色（G 138–160）、身份不可变项及视频改造的输入规则与验收分步，供另一对话发起1021视频改造。未去背、未动画、未接入Unity。
---

# Enemy 1021 新画风替换：持盾 Idle 身份图生成交接

## 0. 接续入口与当前状态

**当前接续：§17 v8 已验收并定为 1021 新画风身份定稿；1021 视频改造由另一对话发起，本节只交接状态与不可变项。** 已验收图规格与实测键色见 §17.2，身份不可变项见 §17.3，视频阶段输入规则见 §17.4，分步验收与已知风险见 §17.5–§17.6；v8 的提交与量化记录见 §16.7。

任务起因：用户要求“写到新的交接文档中，这是1021敌人的替换”。v1–v8 的方案、提交与反馈在各历史章节保留；v8 于 2026-10-25 获用户验收后，v1–v7 转为历史版本。替换对象仍是现有 Enemy_1021，不新增敌人编号。

- 最新已验收画风参考：新 1011 刀兵 v11，见 `design/art-style-guide.md` §2.3、§3、§4.3–§4.4；1021 身份本身以 §17.2 的 v8 为准。
- 当前验收状态：v8 **已验收通过**（盔壳收窄、两侧护片遮耳；脸与身体沿用 v7／v5 的收拢轮廓）；v1–v7 均为历史，不再作身份、比例或画法输入。
- 下游视频改造在另一对话进行：身份参考只用 §17.2 的 v8 原图；旧 1021 素材仅用于核对机制与盾方向，不作形象输入。
- 1011 v11 仅作本地等高对照与笔触参考，不上传；v5 已修正的脸、盾兵盔形、服饰与站姿由 v8 继承。
- 待确认：v8 的盾顶仍是 v5 的侧方布局，未落实早期 §2.3／§14 所写的“盾顶低于下巴”；若需要该位置，先改静态图再确认（§17.6）。
- 发起侧先复述输入、参数、完整提交词与输出路径，取得当次明确“发起生成”后，只提交一次。
- 失败、超时、结果不明或用户不通过，保存记录并停下；不自动换图、改词、降档、换模型或重发。
- 身份静帧通过后才另行准备 Idle 动画及其余动作。生成、视频、选帧、去背、对齐、补边与接入分别处理，不能从“写交接”推定已获授权。

## 1. 替换对象与现有工程契约

本任务对应现有 `Enemy_1021` 盾兵。下列目标已在本机确认存在，仅登记为未来接入落点，本轮不修改：

- 精灵目录：`Assets/Sprites/Enemy/Enemy1021/`
- Idle：`Assets/Animations/Enemy_1021_Idle.anim`
- 控制器：`Assets/Animations/Enemy_1021.controller`
- Prefab：`Assets/Resources/EnemyPrefabs/Enemy_1021.prefab`

现有机制、状态与盾方向遵守：

- `design/enemy-1021-shield-guard.md`
- `design/enemy-1021-animation.md`，尤其持盾表现方案与方向规则。

**持盾时盾画在角色本体帧内。角色解剖学右手持盾，盾位于画面左侧、胸腹前方；右手、手指、前臂及握把藏在盾后。画面右侧是角色左手，保持空置。** 不按常见“左手盾、右手剑”的印象镜像，也不添加刀剑。

Idle 是已经举盾应战的状态，本轮不制作举盾、收盾或盾脱手动作。盾破后的独立掉盾接管与胆怯状态继续沿用现有工程逻辑，不在这张图中改机制。

旧美术与动画历史见 `plan/enemy1021-art-production-handoff.md`、`plan/enemy1021-minimal-playable-handoff.md`。历史已验收资产保持原样；本文件开启新的服饰与画法替换线，不把旧动画作废，也不重做历史任务。旧图只可本地核对盾的遮挡与方向，不作为本次生成的形象或画风输入。新身份获验收后，再逐项确定哪些动作需要更新。

## 2. 新 1021 设计：守阵盾兵，与新 1011 区分

### 2.1 共用画风，不共用整套服饰

继承 v11：短壮厚重、大头宽肩、紧凑躯干、短腿、厚手厚靴；有方向和疏密变化的硬边笔触、偏深结构边、完整形体与分材质塑形。头身和人物身高与 v11 属于同一尺度，不因盾兵身份单独拔高或缩腿。加盾后的整体包围盒自然会变化，不用含武器的总宽高比当作人体比例。

画风不是规则方格、低分辨率马赛克、乐高砖、平滑卡通或统一多边形碎面，也不是全身变暖黄。冷灰铁、蓝布、棕皮之间仍要能分出材质。

| 部位 | 当前新 1011 v11 | 新 1021 v1 初始方案（盔形见 §10，脸型修正见 §13.7／§14.1，待审比例方案见 §15） |
| --- | --- | --- |
| 头部 | 尖顶敞脸铁盔，红头巾和飘尾 | 低圆顶敞脸铁盔，无红头巾、无飘尾、无缨、无面罩 |
| 上身 | 麻色交领衣，小铁胸牌 | 暗靛蓝交领布衣，暖棕无袖皮质短甲，浅色内领可见 |
| 腰部 | 麻绳腰带，红布条 | 宽素皮腰带，小铁扣，不系粗麻绳 |
| 前臂 | 短铁护腕 | 厚皮短护腕，布袖与护腕边界清楚 |
| 下身 | 麻色衣摆，深棕裤，裹腿与厚靴 | 皮甲止于腰，腰带下是独立布衣下摆；深色宽裤、绑腿和稳固短靴 |
| 主要剪影 | 持刀向外展开，偏进攻 | 单面高木盾守住胸腹，衣甲轮廓方厚，偏防守 |

盾兵不靠兽首、龙纹、金边、大肩甲或满身札甲表现高级感。皮甲在腰带处结束，下面是单独的布衣下摆，不能重新画成胸甲、护裆连为一体的金属围裙。

### 2.2 材质处理

- 铁盔与盾边：连续金属曲面、可见边缘厚度、少量集中锐光，不用均匀碎亮片。
- 皮甲、护腕与腰带：厚实哑光、折转与压叠清楚，反光比铁弱，不画成塑料或木板。
- 蓝色衣袖与衣摆：用宽大的褶皱和有方向的笔触组织，不铺密集织纹。
- 木盾：竖向木板、朴素铁边、少量铆钉，木纹顺着木材，不加徽章、兽脸或复杂金饰。
- 脸与手：成年人结构，简洁暖色色面，眉眼与握拳体积清楚，不能复制木纹或金属碎面。

### 2.3 已举盾的战斗 Idle

只生成一张完整静帧，作为后续 Idle 动画的身份起点：

- 头、胸和身体正面朝向镜头；双脚分开、膝微屈、重心稳，呈守阵戒备。
- 压眉、集中注视、嘴紧闭；有战斗感，不能变成平静肖像、怒吼、挥盾或攻击蓄力。
- 解剖学右臂举起唯一一面高矩形木盾，盾位于画面左侧，守住胸腹；持盾的手、前臂、握把自然遮在盾后，不从盾面穿出来。
- 盾顶低于下巴，脸完整可见；盾底离地，双脚可见。盾不得挡住整张脸或变成拖地墙板。
- 画面右侧左手空置，在身侧自然戒备；不拿刀剑、长枪或第二面盾。
- 盾和人体作为一个完整角色画在同张图内；不在本轮拆盾、不做盾背面图。

## 3. v1 历史输入：已验收新 1011 v11

本节仅供首次生成追溯。当前 v2 的唯一输入为 §10.2 的新盾兵 v1 返图，不使用本节输入。

```text
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v11\enemy1011_style_locked_dao_v11.png
1024x1024 RGB PNG, Color Type 2, 1,598,516 bytes
SHA-256 67da58389ed877ce7183b98ad06245c7ddc39eba79ce055fc3792d7e0cbb2c39
```

已在本轮回读原图尺寸、模式、文件大小与哈希。`image[]` 只有这一张，不预先裁切、缩放、抠图或重绘。

参考职责：锁画法、头身与战斗重量；服饰、盾和持盾姿态由本文明确重新设计。**不得复制参考图的环首刀、红头巾、尖盔、小铁胸牌和麻绳腰带到新 1021 上。**

禁止追加旧 1021 持盾图：它虽然可用来本地核对姿态，但会混入旧面罩、红缨、重甲与不同笔触。也不追加 N1 破盾、J1 木篱、N3 场景或 v7；v11 已是用户确认的角色画风样本，不重新进行场景到角色的风格试验。

## 4. 生成参数与发起边界

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[]: ONLY enemy1011_style_locked_dao_v11.png
prompt: section 5 English prompt only
read timeout: 900 seconds
automatic retries: 0
```

以上沿用本项目已执行的图像编辑字段组；本轮没有在线验证接口、调用探针或提交任务。本阶段用不透明纯绿制作底，不请求透明，不把“Idle”理解为本次要发视频。

- 实际提交只取 §5 唯一英文代码块，不拼旧 1011 词、旧盾兵词或额外负面段。
- `quality=max` 与 `input_fidelity=high` 是不同字段，不相互替代；高保真不等于逐像素保留，实际仍需核对造型与比例。
- 凭据只从环境变量 `MUSK_API_KEY` 读取，不写入文档、请求记录、日志或回复。
- 发起前确认输入哈希和 §6 四个输出文件不存在，向用户复述输入、参数、词和输出位置。
- 取得当次明确发起后只提交一次。HTTP 错误、断连、超时或结果未知，记录后停止；不得在同一授权里自动重试、换模型、改图或降规格。
- 下载同一已生成结果不等于创建新任务；若下载中断，只处理同一结果，不重建收费请求。

## 5. 实际提交文本

中文供审阅，不提交：

生成一个完整、正面朝向镜头、已经举盾戒备的盾兵，画法和短壮头身与参考 v11 一致。服饰改成低圆顶敞脸铁盔、无红头巾，暗靛蓝交领布衣、暖棕皮质短甲、宽素皮腰带与小铁扣、短皮护腕。皮甲到腰停止，下面是单独的布衣下摆，配深色宽裤、绑腿和短靴。皮革厚实哑光，布料用大褶皱，铁的反光集中。

角色右臂在画面左侧举唯一一面高矩形木盾，挡住胸腹；持盾手与前臂藏在盾后。盾低于下巴、高于地面，脸和双脚完整可见。画面右侧左手空置。盾为竖木板、普通铁边、少量铆钉。微屈膝、稳住重心、压眉直视、嘴闭紧。不要刀剑、红缨、面罩或军官装饰。一个完整角色，均匀绿底 RGB(0,159,62)，四周留白。

英文沿用上轮交付的生成词，逐字提交：

```text
Create one full-body, front-facing shield infantryman in a guarded idle pose. Match the reference soldier's short stocky proportions, directional hard-edged brushwork, dark structural edges and distinct material treatment.

Give him a low rounded open-face iron helmet without the red headscarf, a dark indigo cross-collar tunic, a warm-brown leather cuirass ending at the waist, a broad plain leather belt with a small iron buckle, and short leather bracers. Below the belt, keep a separate cloth tunic hem, loose dark trousers, calf wraps and sturdy short boots. Keep the leather matte and thick, the cloth folds broad, and the iron highlights concentrated.

His anatomical right arm holds one tall rectangular wooden shield on screen-left, covering his chest and abdomen. Hide that hand and forearm behind the shield. Keep the shield below his chin and above the ground, with his full face and both feet visible. His screen-right hand is empty at his side. Use vertical timber boards, a plain iron rim and a few rivets.

Keep his knees slightly bent, weight braced, brows lowered and eyes fixed forward with a firmly closed mouth. No sword, plume, face mask or ornate officer armor. One complete soldier on a uniform green background RGB(0,159,62), with clear margins.
```

## 6. 新输出目录与记录

```text
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v1\enemy1021_v11_style_idle_v1.png
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v1\enemy1021_v11_style_idle_v1.request.json
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v1\enemy1021_v11_style_idle_v1.response.json
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v1\enemy1021_v11_style_idle_v1.status.json
```

准备时新目录不存在，尚未创建请求或生成结果。若发起前已有文件，先核对来源，不覆盖、改名或重复收费生成。

请求记录保存：输入路径及完整哈希、参数、实际 prompt 及其文本哈希、提交时间、提交次数与状态。响应与状态记录保存真实 HTTP、错误或终态、输出文件字节数／尺寸／模式／哈希；费用与 usage 只有服务返回才填，不猜测。记录不含 API key、Authorization 头或完整签名 URL。

结果必须下载并完整解码、从磁盘回读后才称为已落盘。原图保留，不经本地改盔、改脸或补盾后冒充原始返图。制作底允许 RGB PNG，或 Alpha 全不透明的 RGBA PNG；不把查看器棋盘格当透明证据。

## 7. 验收：画风、差异化、持盾关系分开检查

用户看原图，与 v11 在相近人物身高下并排：

- 画风：深色结构边、有方向且疏密变化的硬边笔触、完整金属曲面与集中反光；布、皮、铁、木可辨。没有积木方格、全身同尺寸切面或平滑塑料卡通。
- 比例：大头宽肩、躯干紧凑、短而有支撑的腿、厚手厚靴，与 v11 在同一角色体系内，不拉高或变成细长成人比例。
- 服饰差异：圆顶敞脸盔、蓝布衣、棕皮甲、宽素皮带、短皮护腕成立；不是给 1011 原服饰换一面盾，没残留红头巾、尖顶盔、小铁胸牌或麻绳腰带。
- 分层：皮甲在腰停止，腰带下是独立布下摆；不是连体金属围裙。衣甲不过度升级为精英将领。
- 持盾方向：解剖学右手持盾，盾在画面左侧；画面右侧左手空置。持盾手和前臂不穿盾，无第二武器。
- 可读性：脸在盾上方可见，两脚可见，盾底离地；已举盾守阵，有战斗神态，但不进入挥盾、迈步、怒吼或攻击动作。
- 完整性：单人单盾、1024x1024 PNG、接近均匀纯绿制作底、全身盾角完整并有留白；无地面、场景、文字、UI 或效果附着。

不通过则保存结果、具体问题和用户意见后停止，不自动改词重发。不用色数、高频量或总包围盒比值代替用户对画法和比例的判断。

## 8. 身份图通过后的替换流程（本轮不执行）

1. 用户先确认新 1021 身份图与新 1011 画法一致，且服饰差异、持盾关系成立。
2. 另行准备持盾 Idle 动画，锁定此身份、盾、画法与站姿；本文件只生成静帧，不包含视频任务。
3. 新旧持盾、破盾及无盾胆怯家族要逐项核对，保持圆盔、蓝布、棕皮甲等新身份一致；不能只换 Idle 后宣称 1021 全套美术已替换。
4. 生成源片、选帧、去背、尺寸与脚底对齐、补边、Clip 和部署各有单独验收。持盾与独立掉盾切换须维持现有单盾规则。
5. 接入前重新读现有资源、动画及 Prefab 引用，保留 GUID、`.meta`、用户改动和旧素材备份。具体参数以当时工程为准，不凭旧交接覆盖。
6. 如获后续部署授权，遵守用户既定边界：先接第一关和对应动画，通知用户开游戏验收；没有对话中的明确自检要求，不自动进 Play 或驱动动画。

本轮到此停在文档交接，不新建 Enemy 类型，不改数值、存档、格挡、破盾或胆怯机制。

## 9. 发起后回填

- 发起日期与授权指令：2026-10-24，用户指令“根据交接文档发起图片生成”。
- 提交词：§5 唯一英文代码块逐字提交，1,271 字符，SHA-256 `316f0e0c501489fe7af92af469b9f27605b7026acbb3947906979d88bce4de81`；无附加负面段。
- 输入：`enemy1011_style_locked_dao_v11.png`，1,598,516 B，SHA-256 `67da58389ed877ce7183b98ad06245c7ddc39eba79ce055fc3792d7e0cbb2c39`（与 §3 一致，仅 1 张 `image[]`）。
- 提交：`POST https://api.muskapis.com/v1/images/edits`，multipart body 1,600,725 B，本地时间 2026-10-24 23:03:41；真实提交次数 **1**，无重试、无改参。
- HTTP 与结果：200，耗时 86.1s（读超时 900s）。响应回显 `size=1024x1024`、`quality=max`、`background=auto`、`model=gpt-image-2.5-sunburst`；usage input 1,040 / output 21,072 / total 22,112 tokens。
- 原图：`enemy1021_v11_style_idle_v1.png`，1,580,737 B，1024×1024，PNG Color Type 2（RGB），SHA-256 `7d4b4bd3013c11c54b5365046827233df54614ceb9f2c9f521dd7f4706f9bff2`；已从磁盘回读并完整解码（24bpp RGB）。四角背景采样 ≈ RGB(2,152,69)，接近请求的纯绿制作底。
- 记录文件：`.request.json` / `.response.json`（6,231,938 B）/ `.status.json` 均在 §6 目录，未含 API key 或签名 URL。
- 用户反馈：头盔缺乏造型，虽然不是高级兵，但目前看起来像现代钢盔。**头盔项需修订，最终身份尚未通过验收。** 对其他项目不追加用户未作出的通过结论；v2 按限定范围保持其他内容。
- 动画、去背、补边、Unity 接入与实机验收：未执行。

## 10. v2：只换头盔结构，保留 v1 其余内容

### 10.1 反馈与修订目标

**历史状态：2026-10-24 已按用户指令提交一次并拿到 v2 返图。后续用户提出头身比例不一致、面相与 1011 重复，尚未通过整体身份验收。** v2 作为当前 §11 的盾兵设计输入，不重发本节头盔词。

用户指出 v1 的头盔缺少造型、像现代钢盔。造型诊断：光滑半球壳体加一圈连续外翻帽檐，缺少分片接合和护颈结构；上一版 `low rounded open-face iron helmet` 的描述不足以形成想要的三国步卒识别。

本轮采用朴素兜鍪的游戏美术设计，不声称考古复原。只修改头盔：

- **盔顶**：矮拱顶，由几块较大的铁片拼合；接缝清楚，中央略凸一条铁脊，不铺密集装饰。
- **额部**：去掉连续整圈外翻帽檐，改为贴合额头的窄铁箍与少量铆钉。中央铁脊到额箍停止，不向鼻梁延伸。
- **侧后方**：耳后增加两三片短叠片保护后颈，脸与颧骨保持完全露出；不将护颈延长成大肩甲或遮脸护颊。
- **等级**：朴素冷铁，无长缨、金边、兽首、宝石、面罩和鼻甲。通过结构获得辨识度，不靠将领装饰。
- **区别于刀兵**：保留盾兵低矮拱盔、无红头巾的方向，不换回新 1011 的尖顶盔和红布飘尾。

人物的脸、表情、头身、衣服、盾、持盾方向、手脚、姿势、画面占比、位置和绿色背景均保持 v1。原始盾兵依然是右手持盾、盾在画面左侧、画面右侧空手；不得因改盔翻转角色。

### 10.2 唯一输入：新 1021 v1 返图

```text
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v1\enemy1021_v11_style_idle_v1.png
1024x1024 RGB PNG, Color Type 2, 1,580,737 bytes
SHA-256 7d4b4bd3013c11c54b5365046827233df54614ceb9f2c9f521dd7f4706f9bff2
```

本轮已从磁盘完整解码并核对模式、尺寸、字节数与哈希。发起前再次复核。`image[]` 必须只有这张完整原图，不裁头、不放大、不缩小、不本地修改后重新命名。

不上传新 1011 v11、旧 1021 形象、旧 1011 帧、场景、盾牌单图或其他参考图；v1 已包含需要保留的画法、人物、服饰与盾，本轮不重开整个人物设计。

### 10.3 参数与发起规则

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[]: ONLY enemy1021_v11_style_idle_v1.png
prompt: section 10.4 English prompt only
read timeout: 900 seconds
automatic retries: 0
```

参数沿用 v1 已执行字段组，本轮未在线验证接口、未发请求或探针。实际提交仅用 §10.4 一个英文代码块，不拼接 §5 全身生成词，不附加负面段，不自行加入新的颜色、甲胄或表情设计。

发起侧按 §0 的规则，复述唯一输入、参数、完整词和输出位置，在取得当次明确“发起生成”后只提交一次。本次“更新交接文档”不是已执行生成。失败、超时、结果不明则保存并停止，不自动改词、换图、降保真或重发。

凭据只从环境变量 `MUSK_API_KEY` 读取，不出现在文档、脚本、日志、响应摘要或回复里。保真参数不保证仅头盔像素改变，仍需对照结果确认修改范围。

### 10.4 实际提交文本

沿用本轮对话已交付的生成词，逐字提交。中文供检阅，不提交：

仅将头盔换成朴素的三国步卒兜鍪。矮拱盔顶由几块较大的铁片拼合，中央略凸铁脊，额前是一条窄铆钉铁箍，替代连续外翻帽檐。铁脊在额箍处结束。耳后加两三片短叠片护颈，整张脸和颧骨完全露出。

头盔保持普通冷铁，延续人物现有硬边笔触和集中高光。无鼻甲、面罩、盔缨、金边或华丽装饰。

保留人物的脸、表情、比例、服饰、盾、姿势、构图和绿色背景。

```text
Replace only the helmet with a modest Three Kingdoms infantry doumou. Give it a low arched crown assembled from a few broad iron plates, a slightly raised central crown rib, and a narrow riveted brow band instead of the continuous flared brim. End the central rib at the brow band. Add two or three short overlapping neck-guard plates behind the ears, leaving the entire face and cheekbones exposed.

Keep the helmet plain cold iron, with the same hard-edged brushwork and concentrated highlights as the soldier. No nasal guard, face mask, plume, gold trim or ornate decoration.

Preserve the soldier's face, expression, proportions, clothing, shield, pose, framing and green background.
```

提交文本口径：LF 行尾、代码块内容本身、不含首尾空行或结尾换行，共 687 字符。

```text
prompt SHA-256: 3e398f79e4a698d6f08a4520c7fd2a56b64244f732fc31c061ccbe39aa1a6ca7
```

上面的哈希代码块是校验信息，不属于实际 prompt。只提取本节以 `Replace only the helmet` 开头的英文代码块；中文和哈希不提交。

### 10.5 新输出与记录

```text
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v2\enemy1021_v11_style_idle_v2.png
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v2\enemy1021_v11_style_idle_v2.request.json
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v2\enemy1021_v11_style_idle_v2.response.json
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v2\enemy1021_v11_style_idle_v2.status.json
```

本轮准备时 v2 目录不存在，未创建结果或请求记录。发起前检查四个输出路径，不覆盖 v1，不覆盖已有 v2 内容，不擅自换名重发。

记录要求沿用 §6：真实输入／文本哈希、字段组、授权和提交时间、次数、HTTP 或错误、输出文件尺寸／字节数／哈希。返回内容须落盘后完整解码回读；不能把工具回执当作落盘证明。实际返图原样保留，不本地改盔后冒充生成结果。

### 10.6 验收：先看盔形，再看保留范围

与 v1 并排，由用户目视判断：

- 低矮拱顶可辨，出现少量大铁片接缝、中央铁脊与窄额箍；原先现代钢盔式连续外翻帽檐消失。
- 铁脊在额箍结束，没有长到鼻梁上；耳后短叠片是护颈，不是遮脸护颊或新增肩甲。整张脸与颧骨仍露出。
- 仍是普通盾步卒的朴素冷铁盔，没有盔缨、金边、兽首、宝石或将领装饰，也没有换回刀兵的尖盔红头巾。
- 原人物的脸与神态、头身、蓝布衣、棕皮甲、护腕、腰带、盾牌、双脚和姿势与 v1 一致，画法仍与已验收新 1011 同属一套。
- 仍为单人单盾，盾在画面左侧、右侧空手；构图和制作绿底保持，全身与盾角完整，没有新增武器或场景。

未通过就记录具体原因并停下，不自动重发。保留其他内容是本轮修改要求，不代替用户对整个身份的最终验收。静帧通过后再另行准备 Idle 动画、其他动作和资源替换；本轮不去背、不补边、不进 Play、不改任何工程资产。

### 10.7 发起后回填

- 版本：v2／仅头盔结构修订。
- 发起日期与授权指令：2026-10-24，用户指令“已更新交接文档，请按文档内容发起生成”。
- 提交词：仅 §10.4 以 `Replace only the helmet` 开头的英文代码块逐字提交，687 字符，SHA-256 `3e398f79e4a698d6f08a4520c7fd2a56b64244f732fc31c061ccbe39aa1a6ca7`（与 §10.4 校验值一致）；未拼接 §5 生成词、无负面段。
- 输入：`enemy1021_v11_style_idle_v1.png`，1,580,737 B，SHA-256 `7d4b4bd3013c11c54b5365046827233df54614ceb9f2c9f521dd7f4706f9bff2`（与 §10.2 一致，仅 1 张 `image[]`，未裁切或改图）。
- 提交：`POST https://api.muskapis.com/v1/images/edits`，multipart body 1,582,359 B，本地时间 2026-10-24 23:42:49；真实提交次数 **1**，无重试、无改参。
- HTTP 与结果：200，耗时 39.48s（读超时 900s）。响应回显 `size=1024x1024`、`quality=max`、`background=auto`、`model=gpt-image-2.5-sunburst`；usage input 894 / output 7,024 / total 7,918 tokens。
- 原图：`enemy1021_v11_style_idle_v2.png`，1,631,416 B，1024×1024，PNG Color Type 2（RGB），SHA-256 `90a530d60f56c077139df59415daed809b95891443bc8c9b02a88af317c81d18`；已从磁盘回读并完整解码（24bpp RGB）。四角背景采样 ≈ RGB(1–2,148–152,65–68)，与 v1 制作绿底量级接近。
- 记录文件：`.request.json` / `.response.json`（2,175,546 B）/ `.status.json` 均在 §10.5 目录，未含 API key 或签名 URL。
- 用户后续反馈：头身比例似乎与已验收新 1011 v11 不一致；面相与 1011 一样，应采用已验收六脸 v1 的其他面孔避免重复。用户提供 `C:/Users/steam/Downloads/screenshot-20261025-000316.png`，对应六脸 v1 右上盾卫脸。本版整体身份未通过，盔形和服饰作为下一轮保留设计，不单独虚构通过结论。
- 动画、去背、补边、Clip、Prefab、实机验收：未执行。

## 11. v3：对齐 1011 v11 头身，采用六脸 v1 的盾卫面孔

### 11.1 反馈与修订范围

**状态：2026-10-25（本机本地日期）已准备方案和面孔输入裁图，同日按用户指令“根据交接文档发起生成”提交一次并拿到 v3 返图。** 日期依据本轮本机准备时间与用户提供的 2026-10-25 截图，不沿用前一日 v2 的提交日期。

用户提出两点：

1. 1021 新返图头身比例似乎与已验收的新 1011 v11 不一致。
2. 脸与 1011 重复，应采用已验收六脸 v1 的其他面孔设计。

本侧将 1011 v11 与 1021 v2 按相同总高度并排，目视发现盾兵的头脸占比偏小、肩到腰带的躯干偏长，整体更修长；不能仅把整张图缩小，也不能只放大盔壳。对照文件是 `Library/Locus/tmp/enemy1021_v2_proportion_review/1011_v11_vs_1021_v2_equal_height.png`，只供分析，不作为生成输入。该预览不是精确人体测量，不给未经验证的头数或百分比。

**比例目标**：匹配已验收 1011 v11 的头部占比、肩宽、躯干长度和四肢重量。脸、头与头盔一起适度放大，缩短肩部到腰带之间的躯干，保留宽肩厚臂、短而稳固的腿。蓝布衣、皮甲和腰带须随新人体关系自然适配，不把贴图机械拉伸；不要以加大盾牌掩盖未修正的人体。

**面孔目标**：采用已验收 `enemy_face_sheet_v1` 的右上盾卫脸，正是本轮用户截图所指。宽方脸、强方下颌、厚眉、较小且集中注视的眼、下巴中央一撮短髯；上唇和两颊干净。去掉当前与 1011 相同的连鬓络腮胡。不通过加黑肤色、换表情或随意加皱纹冒充换脸。

**画法保持**：新面孔只继承脸型、五官与胡须设计，重新用已验收 v11 的硬边笔触、深色结构边和分材质画法绘制。不能复制旧六脸图的红缨盔、护颊、金色额扣、颈部装备或旧像素画法。

**保留范围**：1021 v2 的分片拱盔、中央铁脊、窄额箍和短护颈的结构设计，暗靛蓝布衣、暖棕皮甲、皮腰带与皮护腕、竖木板矩形盾、微屈膝的守阵姿态。允许头盔随头部一起改变尺寸，禁止为了“保留”而重新锁住小头。盾仍在画面左侧，画面右侧空手，脸和双脚可见；不得生成刀剑或新肩甲。

### 11.2 三张实际输入，顺序固定

`image[]` 必须正好 3 张：

```text
1. C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v2\enemy1021_v11_style_idle_v2.png
   1024x1024 RGB PNG, 1,631,416 bytes
   SHA-256 90a530d60f56c077139df59415daed809b95891443bc8c9b02a88af317c81d18
   只管：盾兵盔形结构、蓝布棕皮甲、木盾、持盾方向和戒备姿态。
   不管：当前小头长躯干、当前与1011重复的脸与络腮胡。

2. C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v11\enemy1011_style_locked_dao_v11.png
   1024x1024 RGB PNG, 1,598,516 bytes
   SHA-256 67da58389ed877ce7183b98ad06245c7ddc39eba79ce055fc3792d7e0cbb2c39
   只管：已验收短壮头身、肩与四肢体量、具体画法和材质处理。
   不管：脸、胡须、尖顶盔、红头巾、刀、铁胸牌和麻绳腰带。

3. C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v3\shield_guard_face_v1_design_input.png
   250x228 RGB PNG, 89,468 bytes
   SHA-256 8e2c421f1824d3bdff54c589c7945c2dd71421513ef42ecab052b90d9e961c4e
   只管：已验收右上盾卫的宽方脸、强下颌、厚眉小眼、中央下巴短髯。
   不管：旧头盔和护颊碎边、红色服饰、旧绘制风格。
```

**图3 来源与本轮处理**：用户提供的 `C:/Users/steam/Downloads/screenshot-20261025-000316.png`，473x500 RGBA、337,714 bytes、SHA-256 `33980ccff399569fe0062f1c05073c1737f5b3cd1f8edf2154636cb0e91e5078`。按原图像素矩形 `(112,257)-(362,485)` 裁取，坐标原点左上、右下边界不包含；转 RGB，不缩放、不补画、不美化五官。已回读裁图与源图对应区域一致。边角仍有少量旧盔片和衣领，只供看脸，不复制这些装备。

对应已验收母图：`C:/Users/steam/Pictures/gptGen/enemy_face_sheet_v1/enemy_face_sheet_v1.png`，1536x1024 RGB、1,904,226 bytes、SHA-256 `ff5f23bfbfb80f46abe433cf0ac153cb11c9700ce4e5d6ae7114a4a60ce3b03d`，右上格为盾卫。本轮不上传整张母图，避免模型生成多个人或选错格。

裁图的分析副本是 `Library/Locus/tmp/enemy1021_v2_proportion_review/user_shield_guard_face_design_crop.png`，与图3字节哈希相同；实际上传用上述 v3 目录副本，不由发起侧临时重新裁。已准备的裁图是本地派生参考，不是 v3 生成结果。

发起前核对 3 张哈希和输入顺序。不得追加 v1、旧 1021 图、整张脸谱、场景图、原始 1011 Idle 或对照预览；不得只传两张而省略面孔输入。

### 11.3 参数与发起边界

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[] order:
  1. enemy1021_v11_style_idle_v2.png
  2. enemy1011_style_locked_dao_v11.png
  3. shield_guard_face_v1_design_input.png
prompt: section 11.4 English prompt only
read timeout: 900 seconds
automatic retries: 0
```

沿用本项目已执行字段组，本轮未在线核验接口、未发探针、未提交生成。三图能否同时保持指定分工由实际返图判断，不保证仅局部重绘，也不凭高保真参数认定尺寸和身份不会改变。

发起侧复述三张输入的顺序与职责、参数、完整英文词和输出路径，取得当次明确“发起生成”后只提交一次。本次“请更新交接文档”只落实准备方案，不等于发起生成。失败、超时、结果不明则记录并停止；不自动换图、降档、改词、重发或继续视频制作。凭据仅从环境变量 `MUSK_API_KEY` 读取，不写入文档或日志。

### 11.4 实际提交文本

英文沿用上轮对话交付的生成词，逐字提交。中文供检阅，不提交：

修改图1的盾兵。按图2的头身比例和画法，头、脸和头盔一起放大，躯干缩短，保留宽肩与厚重四肢。图2只管比例和笔触，不复制其脸和服装。

面孔换成图3盾卫的设计：宽方脸、强方下颌、厚眉、集中注视的小眼和下巴中央短髯；两颊、上唇干净。用图2的画法绘制这张新脸，不复制图3的头盔。

保留图1的头盔结构、靛蓝布衣、棕皮甲、木盾和守阵姿势。盾在画面左侧，画面右侧空手，脸和双脚可见，绿色背景保留。

```text
Revise the shield soldier in image 1. Match image 2's head-to-body proportions and drawing style: enlarge the head and face together with the helmet, shorten the torso, and retain broad shoulders and weighty limbs. Use image 2 only for proportions and brushwork, not its face or costume.

Replace the face with the shield guard's facial design in image 3: a broad square face, strong square jaw, heavy brows, small focused eyes and a short central chin beard. Keep the cheeks and upper lip clean. Render this face in image 2's drawing style without copying image 3's helmet.

Preserve image 1's helmet construction, indigo clothing, brown leather armor, wooden shield and guarded pose. Keep the shield on screen-left, the screen-right hand empty, the full face and both feet visible, and the green background.
```

**文本校验**：LF 行尾，代码块正文、不含首尾空行或末尾换行，共 809 字符，SHA-256 `9a3d1931d2ecd6a8ac2b7fdef43920113189c9ab8e2e69e83e6bec279ec50833`。只提取以 `Revise the shield soldier` 开头的唯一英文代码块；哈希、中文、参数和旧版词均不提交，不追加负面段。

### 11.5 输出、已有文件与留档

```text
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v3\enemy1021_v11_style_idle_v3.png
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v3\enemy1021_v11_style_idle_v3.request.json
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v3\enemy1021_v11_style_idle_v3.response.json
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v3\enemy1021_v11_style_idle_v3.status.json
```

本轮仅在该目录准备了 `shield_guard_face_v1_design_input.png`。以上四个结果／记录文件均尚不存在，目录存在不等于已提交或生成。发起前再次确认，不覆盖 v1、v2、原始 v11、用户截图、六脸母图或脸部副本；已有结果则核对来源并停下，不擅自另命名重发。

按 §6 留档真实输入顺序及完整哈希、实际 prompt 和哈希、参数、授权与提交时间、次数、HTTP／终态、输出字节数／尺寸／模式／哈希。保留原始返图，下载解码后从磁盘回读再报告。费用仅记录服务实际返回值，不猜测；不保存 API key、Authorization 头或完整签名 URL。

### 11.6 验收：人体比例、独立面孔与画风分开看

用户看原图，以相同人物总高度和近似显示大小对照 1011 v11、1021 v2 和盾卫脸裁图：

- **比例**：头脸与盔作为整体占比更接近 1011 v11，肩到腰带不再过长，宽肩、厚臂、短腿和厚靴属于同一体系；不是仅放大盔壳，亦不是整张图等比缩放。
- **独立面孔**：宽方下颌、厚眉小眼、下巴中央短髯可辨，上唇和两颊干净；与 1011 的连鬓络腮胡脸在正常观看尺寸下明显可区分。不能仅换个胡子而保留原来的全部脸型和五官。
- **画法**：新脸由 v11 的硬边笔触重新绘制，与身体材质统一；没有照搬脸谱母图的旧像素画法、红缨、金色额扣或护颊。
- **保留设计**：v2 的拱形分片盔、中央铁脊、窄额箍、短护颈、蓝衣棕皮甲、宽皮带与木盾还在；头盔可随新头部比例变化尺寸，结构不变，不长刀或新肩甲。
- **持盾关系**：盾仍在画面左侧，持盾手／前臂藏在后方；画面右侧左手空置，整张脸和双脚可见，盾底离地，仍是已举盾的静止戒备。
- **完整性**：单人单盾，1024x1024 PNG，接近均匀的绿色制作底，全身与盾角完整并留空边。

头身仍偏修长、脸仍与 1011 重复、或旧脸谱盔甲／画法被复制回来，均记为待修而不是自动通过。失败或用户不通过则记录后停止；不自动换词重发，不去背、不选帧、不补边、不改 Clip、Prefab、工程资产或机制。

### 11.7 发起后回填

- 版本：v3／头身比例与独立盾卫面孔。
- 准备与发起日期：2026-10-25；授权指令“根据交接文档发起生成”。
- 提交词：仅 §11.4 以 `Revise the shield soldier` 开头的英文块逐字提交，809 字符，SHA-256 `9a3d1931d2ecd6a8ac2b7fdef43920113189c9ab8e2e69e83e6bec279ec50833`（与 §11.4 校验值一致）；无中文、无哈希行、无负面段。
- 输入（顺序已校对，与 §11.2 一致）：
  1. `enemy1021_v11_style_idle_v2.png`，1,631,416 B，SHA-256 `90a530d60f56c077139df59415daed809b95891443bc8c9b02a88af317c81d18`
  2. `enemy1011_style_locked_dao_v11.png`，1,598,516 B，SHA-256 `67da58389ed877ce7183b98ad06245c7ddc39eba79ce055fc3792d7e0cbb2c39`
  3. `shield_guard_face_v1_design_input.png`，89,468 B，SHA-256 `8e2c421f1824d3bdff54c589c7945c2dd71421513ef42ecab052b90d9e961c4e`
- 提交：`POST https://api.muskapis.com/v1/images/edits`，multipart body 3,321,485 B，本地时间 2026-10-25 00:57:58；真实提交次数 **1**，无重试、无改参、未换图。
- HTTP 与结果：200，耗时 42.55s（读超时 900s）。响应回显 `size=1024x1024`、`quality=max`、`background=auto`、`model=gpt-image-2.5-sunburst`；usage input 2,045（image 1,680 / text 365）/ output 7,024 / total 9,069 tokens。
- 原图：`enemy1021_v11_style_idle_v3.png`，1,598,785 B，1024×1024，PNG Color Type 2（RGB），SHA-256 `c49135c58fb8f56533ecee515e4a231e99e30491e6d9c6701afb49fbc58aefd7`；已从磁盘回读并完整解码（24bpp RGB）。四角背景采样 ≈ RGB(1–2,151–154,65–66)，与前两版制作绿底同量级。
- 记录文件：`.request.json` / `.response.json`（2,132,040 B）/ `.status.json` 均在 §11.5 目录，未含 API key 或签名 URL；面孔输入副本原样保留。
- 用户比例、独立面孔、画法、保留设计与持盾关系验收：**待用户按 §11.6 目视判断**；本轮未作任何视觉通过结论。
- 动画、去背、补边、Clip、Prefab 与实机验收：未执行。

## 12. v4：只修脸，向参考盾卫脸对齐

### 12.1 反馈与修订范围

**状态：2026-10-25 已按用户指令提交 v4 一次（HTTP 200，41.74s）并落盘；用户判定“没变化”，脸仍太方，本版未通过。本轮仅修脸，其余保持 v3。**

用户判断：v3 的脸太方。本版只换脸，不重开人体比例、服饰、盔形、盾或姿态。

量测证据（面板而非判定依据）：盾卫脸裁图的脸部肤色框为 189×178，宽/高 1.06，最宽处在颧部，下巴宽约为颧宽的 0.97——**参考本身并不收下颌**，属于等宽带圆角的宽方脸。因此本轮将“太方”落实为去掉直角转折与平板块面，而不是收窄下颌或改成尖下巴；仅靠收窄会把脸做成参考之外的另一张脸。

v3 全图的肤色成分与皮甲、手部颜色相连（框 150×453），自动测量无法干净分出脸框，因此不给 v3 的脸宽数字，也不用错误测量替代用户目视。

生成词归属：以往分工为本侧不写词；本次用户明确指令“你自己想生成词”，故 §12.4 英文词由本侧起草，仍只作为待验收方案，不表示画风或身份已通过。

不得靠加黑肤色、换表情、加年龄皱纹或缩小脸冒充修脸。

**保留范围**：v3 的分片拱盔、中央铁脊、窄额箍、短护颈、头身比例、暗靛蓝布衣、暖棕皮甲、宽皮带与短皮护腕、竖木板矩形盾、微屈膝的守阵姿态、盾在画面左侧、右侧空手、脸部与双脚可见、构图与制作绿底。

### 12.2 两张输入，顺序固定

`image[]` 必须正好 2 张：

```text
1. C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v3\enemy1021_v11_style_idle_v3.png
   1024x1024 RGB PNG, 1,598,785 bytes
   SHA-256 c49135c58fb8f56533ecee515e4a231e99e30491e6d9c6701afb49fbc58aefd7
   只管：除脸以外的全部保留内容、人物尺寸与画法。
   不管：当前太方的脸。

2. C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v3\shield_guard_face_v1_design_input.png
   250x228 RGB PNG, 89,468 bytes
   SHA-256 8e2c421f1824d3bdff54c589c7945c2dd71421513ef42ecab052b90d9e961c4e
   只管：脸型、五官、眉、鼻与下巴中央短髯。
   不管：旧盔、护颊、红色服饰、金色额扣、旧像素画法。
```

不上传新 1011 v11：v3 已带有 v11 画法，追加只会抬高把 1011 面孔与髯须带回来的风险。不上传 v1/v2、整张脸谱、用户原始截图或对照预览；不裁头、不本地修脸后重新命名。

### 12.3 参数与发起边界

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[] order:
  1. enemy1021_v11_style_idle_v3.png
  2. shield_guard_face_v1_design_input.png
prompt: section 12.4 English prompt only
read timeout: 900 seconds
automatic retries: 0
```

沿用已执行字段组，未在线核验接口、未发探针。只提交 §12.4 一个英文块，不拼接前几轮生成词，不追加负面段。高保真不保证只改脸，需对照结果确认修改范围。凭据仅从环境变量 `MUSK_API_KEY` 读取。

### 12.4 实际提交文本

中文供检阅，不提交：

只修改图1盾兵的脸，其他全部不变：盔、头身、布衣、皮甲、腰带、护腕、木盾、姿势、构图和绿底。当前的脸像一个硬直角方块，改为按图2的脸型：下颌仍然宽厚、几乎与颧骨同宽，但转角圆而分段，从两颊流到下巴，不出现方盒。眉厚而直、眼小且集中、鼻宽低、下巴中央一撮短髯；两颊、上唇与下颌两侧干净，无边髯络腮胡。用图1的硬边笔触和暖肤色重画，只用几个大面，不铺小碎面网格；表情平静专注，不怒吼。图2只取脸结构，不复制其盔、护颊、红服饰、金色额扣或旧像素画法。

```text
Revise only the face of the shield soldier in image 1. Keep everything else unchanged: the same helmet construction, head size and body proportions, indigo clothing, brown leather armor, belt and bracers, wooden shield with vertical boards, guarded pose, shield on screen-left, empty screen-right hand, both feet visible, framing and the uniform green background.

The current face reads as a hard rectangular slab. Replace it with the facial design in image 2: a broad face whose jaw stays nearly as wide as the cheekbones, but with rounded, stepped corners instead of right angles, so the outline flows from cheek to a full chin without forming a square box. Give him heavy straight brows, small focused eyes, a broad low nose and one short beard tuft in the centre of the chin. Keep both cheeks, the upper lip and the sides of the jaw clean-shaven, with no sideburns, no full beard and no moustache.

Paint this face with image 1's hard-edged brushwork, dark structural edges and warm skin tone, using a few broad planes instead of a grid of small facets, and keep the same light direction as the rest of the figure. Keep his expression calm and focused, not angry and not shouting.

Take only the facial structure from image 2. Do not copy its helmet, cheek guards, red clothing, gold forehead ornament, old pixel rendering or any other equipment.
```

**文本校验**：LF 行尾，代码块正文、不含首尾空行或末尾换行，共 1,351 字符，SHA-256 `c12cf69dda9a4d5a9b8682c69cd8a52c982f152fa69f812d0b5e23acfa43ff26`。只提取以 `Revise only the face` 开头的唯一英文代码块；哈希、中文与参数均不提交，不追加负面段。

### 12.5 输出与记录

```text
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v4\enemy1021_v11_style_idle_v4.png
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v4\enemy1021_v11_style_idle_v4.request.json
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v4\enemy1021_v11_style_idle_v4.response.json
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v4\enemy1021_v11_style_idle_v4.status.json
```

不覆盖 v1、v2、v3、原始 v11、用户截图、六脸母图或脸部副本。记录要求沿用 §6：真实输入顺序与哈希、实际 prompt 与哈希、参数、授权与提交时间、次数、HTTP／终态、输出字节数／尺寸／模式／哈希；不保存 API key、Authorization 头或签名 URL。

### 12.6 验收：先看脸，再看保留范围

与 v3、盾卫脸裁图并排，由用户目视判断：

- **脸轮廓**：颧到下颌不再是一块直角平板，转折有圆角与分段；下巴仍然宽厚，没改成尖下巴或瘦长脸。
- **五官**：厚直眉、小而集中的眼、宽低鼻、下巴中央一撮短髯可辨；两颊、上唇与下颌两侧干净，没有边髯络腮胡或八字胡。
- **画法**：新脸用硬边笔触与暖肤色，光向与身体一致，没有铺满小碎面或平滑浆糊感。
- **保留范围**：脸以外的盔结构、头身比例、布衣皮甲、腰带护腕、木盾、姿势、构图与绿底与 v3 一致；盾仍在画面左侧、右侧空手。
- **参考隔离**：没有把脸谱的旧盔、护颊、红服饰、金色额扣或旧像素画法带回来；无刀剑、无新肩甲。
- **完整性**：单人单盾，1024x1024 PNG，接近均匀绿底，全身与盾角完整并留空边。

未通过则记录具体原因并停下，不自动重发、不去背、不选帧、不补边、不改工程资产。

### 12.7 发起后回填

- 版本：v4／只修脸（向参考盾卫脸对齐）。
- 发起授权与日期：2026-10-25，用户指令“把他的脸修正为参考的脸，发起修正生成，当前这个脸太方了”＋“你自己想生成词”；生成词由本侧起草并编码为 §12.4。
- 提交词：§12.4 以 `Revise only the face` 开头英文块逐字提交，1,351 字符，SHA-256 `c12cf69dda9a4d5a9b8682c69cd8a52c982f152fa69f812d0b5e23acfa43ff26`；无中文、无哈希行、无负面段。
- 输入（顺序已校对，与 §12.2 一致）：
  1. `enemy1021_v11_style_idle_v3.png`，1,598,785 B，SHA-256 `c49135c58fb8f56533ecee515e4a231e99e30491e6d9c6701afb49fbc58aefd7`
  2. `shield_guard_face_v1_design_input.png`，89,468 B，SHA-256 `8e2c421f1824d3bdff54c589c7945c2dd71421513ef42ecab052b90d9e961c4e`
- 提交：`POST https://api.muskapis.com/v1/images/edits`，multipart body 1,690,711 B，本地时间 2026-10-25 01:09:28；真实提交次数 **1**，无重试、无改参、未换图。
- HTTP 与结果：200，耗时 41.74s（读超时 900s）。响应回显 `size=1024x1024`、`quality=max`、`background=auto`、`model=gpt-image-2.5-sunburst`；usage input 1,620（image 1,120 / text 500）/ output 7,024 / total 8,644 tokens。
- 原图：`enemy1021_v11_style_idle_v4.png`，1,766,965 B，1024×1024，PNG Color Type 2（RGB），SHA-256 `145d32369b8ee875ad078c6125e7c6be50f879ab8b583920c2f4b95fcae8313b`；已从磁盘回读并完整解码（24bpp RGB）。四角背景采样 ≈ RGB(1–2,152–154,65–66)，与 v3 制作绿底同量级。
- 修改范围量化（仅作参考，不作验收结论）：与 v3 逐像素比较，阈值 |30| 下差异像素 31,170 / 1,048,576 = **2.97%**；差异集中在高度 20%–50% 带（头部区域，峰值行 y≈234–342），其余区域仅有少量散点差异，说明本轮改动以头部为主，但不是像素级只改脸。
- 用户反馈（2026-10-25）：“没变化”。目视结论为脸与 v3 无明显差别、仍然太方，本版按**未通过**记录；上面的 2.97% 头部差异没有形成可见的脸部改造。
- 当时停点：用户只要求记录现状，未授权该次继续重发。此处为 v4 历史记录；本轮用户要求更新新的生成方案，当前待发起内容改由 §13 v5 承接，不重发 §12。
- 动画、去背、补边、Clip、Prefab 与实机验收：未执行。

## 13. v5：单图收平脸颊与下颌，保留 v3 头身

### 13.1 反馈与修改范围

**历史状态：2026-10-25 已按用户指令“根据交接文档发起v5生成”提交一次（HTTP 200，32.58s）并落盘。后续用户确认脸型已修正，但指出头仍偏大、盾牌不够大，整体身份未通过。** v4 已有另一侧的提交记录，本方案因此使用新版本号 v5，不覆盖或改写 v4 历史；当前方案由 §15 v7 承接。

用户反馈：“脸太大太方了像是吃了槟榔一样”。v3 两颊鼓出、下颌转角外扩，脸从太阳穴到下巴几乎等宽。此前同时要求“放大头脸”与“宽方脸、强方下颌”，可能强化了这一效果。本轮仅修脸部轮廓：

- 两颊收平，减少嘴角外侧鼓出的肉感，保持成年兵卒的结实感，不做凹陷病瘦脸。
- 下颌从耳下向下巴自然内收，保留轻微棱角，去掉外扩方腮；下巴仍适度宽，不改成尖下巴或瘦长脸。
- 保留厚眉、小而集中的眼、紧闭的嘴和中央短髯；两颊及上唇干净，继续与 1011 的络腮胡区分。
- 保留 v3 的头身、头盔尺寸与结构、画法、衣甲、盾、四肢姿势、构图和绿底。收脸颊不等于缩小整颗头，不能只缩盔或拉长身材。

不再上传宽方盾卫脸裁图，也不再要求下颌几乎与颧骨同宽。§12 的“只改圆角、保留下颌等宽”路线已经无明显变化，本轮明确改变脸部外轮廓。§12 的平静表情要求不沿用，仍是严厉的成年战斗士兵，不靠加黑肤色、换胡须、加皱纹或怒吼冒充换脸。

### 13.2 唯一输入

```text
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v3\enemy1021_v11_style_idle_v3.png
1024x1024 RGB PNG, Color Type 2, 1,598,785 bytes
SHA-256 c49135c58fb8f56533ecee515e4a231e99e30491e6d9c6701afb49fbc58aefd7
```

本轮已从磁盘完整解码，复核尺寸、模式、字节数及哈希。发起前再次核对，`image[]` 正好 1 张。只上传 v3 完整原图，不裁头、不缩放、不本地改五官。

不上传 v4、宽方盾卫脸裁图、原始截图、整张六脸图、新旧 1011 或场景，也不换回 v1/v2。v3 是本轮对话已交付方案指定的底图，不继续累积 v4 修订。

### 13.3 参数与发起边界

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[]: ONLY enemy1021_v11_style_idle_v3.png
prompt: section 13.4 English prompt only
read timeout: 900 seconds
automatic retries: 0
```

沿用本项目已执行的字段组，本轮未在线核验接口、未发探针或生成请求。只提交 §13.4 唯一英文代码块，不拼接旧词，不追加负面段，不重加宽方下颌或下颌与颧骨等宽的指令。

发起侧复述唯一输入、参数、完整提交词和输出位置，取得当次明确“发起生成”后仅提交一次。本次更新交接只落实准备，不代表已授权生成。失败、超时或结果未知就留档停止，不自动改词、换图、降保真或重发。凭据只从环境变量 `MUSK_API_KEY` 读取，不写入记录或回复。保真参数不保证局部像素不变，仍需检查实际修改范围。

### 13.4 实际提交文本

沿用本轮对话已交付的英文词，逐字提交。中文供检阅，不提交：

只修盾兵的脸。减少两颊和下颌的宽度及饱满感，脸颊保持结实、较平，从耳下向适度宽的下巴自然收拢；保留轻微棱角，不要外扩方腮、鼓脸或尖下巴。

保留厚眉、集中注视的小眼、紧闭的嘴和中央短髯，两颊与上唇干净，维持成年士兵的严厉神态。保留当前头身、头盔尺寸和结构、画法、服饰、盾、姿势、构图和绿色背景。

```text
Refine only the shield soldier's face. Reduce the width and fullness of the cheeks and jaw. Keep the cheek planes firm and relatively flat, with the jaw tapering naturally from below the ears toward a moderately broad chin. Retain subtle angular definition without bulky jaw corners, swollen cheeks or a pointed chin.

Keep the heavy brows, small focused eyes, firmly closed mouth and short central chin beard. Keep the cheeks and upper lip clean, and preserve a stern adult soldier's expression.

Preserve the current head-to-body proportions, helmet size and construction, drawing style, clothing, shield, pose, framing and green background.
```

文本校验：LF 行尾、代码块正文、不含首尾空行及结尾换行，643 字符，SHA-256 `7c1e79dedc13bdf768d76d82a3d7a8a0f1645c89ee396b0032d16ebb6b5ece4c`。哈希、中文、参数和旧版章节不提交。

### 13.5 输出与留档

```text
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v5\enemy1021_v11_style_idle_v5.png
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v5\enemy1021_v11_style_idle_v5.request.json
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v5\enemy1021_v11_style_idle_v5.response.json
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v5\enemy1021_v11_style_idle_v5.status.json
```

准备时 v5 目录不存在，没有新请求或结果。发起前再次确认，不覆盖 v1–v4、源图、脸谱或任何已有结果；若存在文件，先核对来源，不擅自换名重发。

按 §6 记录真实输入／文本哈希、字段组、授权与提交时间、次数、HTTP／终态、输出路径／字节数／尺寸／模式／哈希。原始返图完整下载、解码和磁盘回读后再报告落盘，不本地修脸冒充生成结果。费用只填服务实际返回值；不保存 API key、Authorization 头或完整签名 URL。

### 13.6 验收

以相同显示大小与 v3 并排，由用户判断：

- 两颊不再鼓出，下颌从耳下向下巴有可见收拢；不是太阳穴到下巴等宽的方盒脸，也不只是磨圆直角。
- 保留成年、结实、严厉的士兵神态，不是瘦长脸、尖下巴、儿童脸或病瘦脸。
- 厚眉、小眼、闭口、中央短髯和干净两颊／上唇还在，与 1011 络腮胡有区别；不是仅换胡子或表情。
- 整组头部与身体比例未缩回 v2 的小头长身；头盔尺寸和结构、蓝衣棕皮甲、盾、四肢姿势、构图及绿底与 v3 保持。
- 画法仍在已验收 v11 体系内，没有新增规则方块、统一碎面或平滑卡通。盾在画面左侧，右侧空手，脸、双脚、盾角完整。

脸仍无可见变化、继续鼓腮宽方、或通过缩小整颗头改变比例，都不自动通过。保持其他内容是修改范围，不补写用户未作出的验收结论。失败或不通过就记录停止，不自动重发，不去背、不做动画、不补边、不进 Play、不改工程资源或机制。

### 13.7 发起后回填
- 版本：v5／单图收两颊与下颌。
- 准备与发起：准备日期 2026-10-25；授权指令“根据交接文档发起v5生成”。
- 提交词：§13.4 以 `Refine only the shield soldier` 开头英文块逐字提交，643 字符，SHA-256 `7c1e79dedc13bdf768d76d82a3d7a8a0f1645c89ee396b0032d16ebb6b5ece4c`（与 §13.4 校验值一致）；无旧词拼接、无负面段。
- 输入：`enemy1021_v11_style_idle_v3.png`，1,598,785 B，SHA-256 `c49135c58fb8f56533ecee515e4a231e99e30491e6d9c6701afb49fbc58aefd7`（与 §13.2 一致，仅 1 张 `image[]`，未裁头或本地改五官）。
- 提交：`POST https://api.muskapis.com/v1/images/edits`，multipart body 1,600,363 B，本地时间 2026-10-25 14:50:48；真实提交次数 **1**，无重试、无改参。
- HTTP 与结果：200，耗时 32.58s（读超时 900s）。响应回显 `size=1024x1024`、`quality=max`、`background=auto`、`model=gpt-image-2.5-sunburst`；usage input 883（image 560 / text 323）/ output 7,024 / total 7,907 tokens。
- 原图：`enemy1021_v11_style_idle_v5.png`，1,777,098 B，1024×1024，PNG Color Type 2（RGB），SHA-256 `1dede32afe60025f8912a146d41a1bbf899fea81c5cac3b131db699e0b00ad38`；已从磁盘回读并完整解码（24bpp RGB）。四角背景采样 ≈ RGB(1–2,160–168,63–66)，比 v3（≈152）略亮，仍在同一制作绿底量级。
- 修改范围量化（仅作参考，不作验收结论）：与 v3 逐像素比较，阈值 |30| 下差异像素 33,311 / 1,048,576 = **3.18%**；分布峰值在高度 20%–50% 带（头部区域），其余高度带只有少量散点，说明改动以头部为主，但不是像素级只改脸。
- 记录文件：`.request.json` / `.response.json` / `.status.json` 均在 §13.5 目录，未含 API key 或签名 URL。
- 用户后续反馈：**脸型已修正，但头部仍偏大、盾牌不够大**（反馈见 §14.1）。仅记录脸型修正，不把保留范围或整体身份补写为通过。
- 动画、去背、补边、Clip、Prefab 与实机验收：未执行。

## 14. v6：缩小头部并放大盾牌，保持 v5 设计

### 14.1 反馈与修改范围

**状态：2026-10-25 已按用户指令提交 v6 一次（HTTP 200，51.03s）并落盘；用户验收不通过。** 用户确认 v5 脸型已修正，但 v6 将头、脸和头盔缩小到诡异的小头比例；v6 不作为最终身份图。

本版只改两个剪影比例：

1. **头部**：用 1011 v11 作为比例与画法参考，把 v5 的头、脸和头盔一起缩小，直到相同人物总高度下的头身关系接近 1011。身体、肩、手臂、腿和靴子的重量保持 v5。不能只缩盔壳，不能整张图等比缩小，也不能通过拉长躯干补偿。
2. **盾牌**：在画面左侧同时加高、加宽 v5 的木盾。盾顶保持在下巴下方，盾底接近脚踝但离地，覆盖完整胸腹并向身体中线多延伸；脸、双脚和画面右侧空手继续可见。

保持：v5 已修正的面孔、矮拱分片盔、中央铁脊、窄额箍、短护颈、暗靛蓝布衣、棕皮甲、皮腰带、短皮护腕、守阵姿势、v11 角色画法、绿底。不要再次改脸，不加第二面盾，不加刀剑，不复制 1011 的脸、尖盔、红头巾、衣服或刀。

### 14.2 两张实际输入，顺序固定

`image[]` 必须正好 2 张：

```text
1. C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v5\enemy1021_v11_style_idle_v5.png
   1024x1024 RGB PNG, 1,777,098 bytes
   SHA-256 1dede32afe60025f8912a146d41a1bbf899fea81c5cac3b131db699e0b00ad38
   只管：1021 v5 已修正面孔、头盔结构、服饰、木盾、持盾关系、姿势与当前形象。

2. C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v11\enemy1011_style_locked_dao_v11.png
   1024x1024 RGB PNG, 1,598,516 bytes
   SHA-256 67da58389ed877ce7183b98ad06245c7ddc39eba79ce055fc3792d7e0cbb2c39
   只管：已验收的头身比例、体量关系与角色画法。
   不管：脸、盔、红头巾、衣服、刀和 1011 的装备。
```

发起前核对两个哈希和顺序。不得追加 v3、v4、六脸裁图、旧 1021、场景图或盾牌参考图；不得把 1011 的刀、脸、尖盔、红头巾和衣服带回结果。v5 是主要形象输入，1011 只作比例和画法对照。

### 14.3 参数与发起边界

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[] order:
  1. enemy1021_v11_style_idle_v5.png
  2. enemy1011_style_locked_dao_v11.png
prompt: section 14.4 English prompt only
read timeout: 900 seconds
automatic retries: 0
```

沿用已执行字段组，本轮未在线核验接口、未发探针或生成请求。只提交 §14.4 唯一英文代码块，不拼接旧版词，不追加负面段。高保真不保证只修改两项，返图必须逐项目视核对。

发起侧复述两张输入的职责与顺序、参数、完整提交词和输出路径，取得当次明确“发起生成”后仅提交一次。本次更新交接不等于生成授权。失败、超时或结果未知就留档停止，不自动改词、换图、降保真或重发。凭据只从环境变量 `MUSK_API_KEY` 读取，不写入记录或回复。

### 14.4 实际提交文本

```text
Revise image 1 in only two ways.

First, use image 2 only as a proportion and drawing-style reference. Reduce image 1's head and helmet together until the head-to-body relationship matches image 2 at the same total figure height. The current head is too large. Keep the torso, shoulders, arms, legs and boots at their current weight and size. Do not shrink the whole character, and do not change image 1's current facial design.

Second, enlarge image 1's wooden shield in both height and width. Keep its top edge just below the chin and its bottom edge just above the ground near ankle level. Make it cover the complete screen-left chest and abdomen and extend closer to the body's centerline. Keep the full face, both feet and the empty screen-right hand visible. The shield remains on screen-left.

Preserve image 1's corrected face, helmet construction, indigo clothing, brown leather armor, guarded pose, hard-edged brushwork, framing and green background. Use image 2 only for proportions and drawing style. Do not copy image 2's face, helmet, red headscarf, clothing or sword. No additional weapon.
```

文本校验：LF 行尾、代码块正文、不含首尾空行与结尾换行，1,105 字符，SHA-256 `8cac596b161903b802ef4880753c711d3231ae30234947b76684c271d35bd4ed`。中文、哈希、参数和旧版章节不提交。

### 14.5 输出与留档

```text
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v6\enemy1021_v11_style_idle_v6.png
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v6\enemy1021_v11_style_idle_v6.request.json
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v6\enemy1021_v11_style_idle_v6.response.json
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v6\enemy1021_v11_style_idle_v6.status.json
```

本轮准备时 v6 目录不存在，没有结果或请求记录。发起前再次确认，不覆盖 v1–v5、1011 v11 或任何参考文件；已有结果则先核对来源，不擅自换名重发。

按 §6 记录真实输入顺序与哈希、实际 prompt 与哈希、字段组、授权与提交时间、次数、HTTP／终态、输出尺寸／字节数／模式／哈希。完整下载、解码、磁盘回读后再报告落盘；费用只填服务实际返回值。不保存 API key、Authorization 头或完整签名 URL。

### 14.6 验收重点

与 v5、1011 v11 按相同人物总高度并排：

- **头身**：头、脸和盔一起缩小；头部占比接近 1011 v11。躯干没有被拉长，肩、臂、腿和靴子仍保持 v5 的重量。
- **盾牌**：盾同时加高、加宽，胸腹覆盖明显增强；盾顶在下巴下方，盾底接近脚踝但不拖地，向身体中线延伸；脸、双脚和画面右侧空手仍可见。
- **身份保持**：v5 面孔、分片拱盔、蓝布棕皮甲、护腕、守阵姿势与 v11 画法保持；不复制 1011 的脸、尖盔、红头巾、刀或衣服。
- **完整性**：单人单盾，盾仍在画面左侧，无第二武器、场景或特效，绿底接近均匀，全身和盾角完整。

若头仍过大、盾仍不够大、身体被等比缩小或拉长、脸／服饰／盔形被重做，则记录为未通过并停止；不自动重发，不去背、不做动画、不补边、不进 Play、不改工程资源或机制。

### 14.7 发起后回填
- 版本：v6／头身与盾牌尺寸修订。
- 准备与发起：准备日期 2026-10-25；授权指令“根据新的交接文档发起生成”。
- 提交词：§14.4 以 `Revise image 1 in only two ways` 开头英文块逐字提交，1,105 字符，SHA-256 `8cac596b161903b802ef4880753c711d3231ae30234947b76684c271d35bd4ed`（与 §14.4 校验值一致）；无旧词拼接、无负面段。
- 输入（顺序已校对，与 §14.2 一致）：
  1. `enemy1021_v11_style_idle_v5.png`，1,777,098 B，SHA-256 `1dede32afe60025f8912a146d41a1bbf899fea81c5cac3b131db699e0b00ad38`
  2. `enemy1011_style_locked_dao_v11.png`，1,598,516 B，SHA-256 `67da58389ed877ce7183b98ad06245c7ddc39eba79ce055fc3792d7e0cbb2c39`
- 提交：`POST https://api.muskapis.com/v1/images/edits`，multipart body 3,377,823 B，本地时间 2026-10-25 15:59:44；真实提交次数 **1**，无重试、无改参。
- HTTP 与结果：200，耗时 51.03s（读超时 900s）。响应回显 `size=1024x1024`、`quality=max`、`background=auto`、`model=gpt-image-2.5-sunburst`；usage input 1,559（image 1,120 / text 439）/ output 7,024 / total 8,583 tokens。
- 原图：`enemy1021_v11_style_idle_v6.png`，1,764,427 B，1024×1024，PNG Color Type 2（RGB），SHA-256 `d1b81421faeff6ca19c5ae4e9b9eef65b31e5cedb747dc6e75150fe6ce4ce740`；已从磁盘回读并完整解码（24bpp RGB）。四角背景采样 ≈ RGB(1–2,153–158,64–66)，比 v5（≈160–168）略低。
- 修改范围量化（仅作参考，不作验收结论）：与 v5 逐像素比较，阈值 |30| 下差异 375,504 / 1,048,576 = **35.81%**；十个高度带全部有 2.0 万–5.7 万像素改动，说明整张人物被重绘，不是局部两处微调，因此必须按 §14.6 逐项目视核对脸、盔形与服饰是否原样保留。
- 剪影测量（仅作参考）：主体外框高 951→930（底边 989→970、顶边 39→41，整高约缩 2.2%）；头盔带（高度 10% 行）宽度 225→171；盾区（高度 75% 行）左缘 197→144（向左扩 53 px）；主体最左点 135→144（右移 9 px）。这些是行宽代理值，不等于盾与头的真实尺寸。
- 记录文件：`.request.json` / `.response.json` / `.status.json` 均在 §14.5 目录，未含 API key 或签名 URL。
- 用户头身、盾牌、面孔与保留范围验收：**未通过**。用户反馈：最新验收版不合格，因为头部被缩小到诡异的尺寸。具体表现为 v6 的头、脸和头盔相对身体缩小过度，形成不自然的小头比例；不能作为最终身份图。
- 处理结论：v6 停止，不去背、不做动画、不补边、不改 Clip、Prefab、工程资产或机制；不自动改词、换图、降规格或重发。
- 当前参考边界：v5 的脸型修正记录保留，但 v5 也不能直接视为最终通过版；此前 v5 的头部仍偏大。新的 v7 分析与单图修缝方案见 §15，先以本地草稿固定比例，再由用户确认；不得重发 §14.4。
- 动画、去背、补边、Clip、Prefab 与实机验收：未执行。

## 15. v7：先用本地草稿固定比例，再以单图生成修复拼接

### 15.1 当前状态、六版复盘与方案变化

**状态：2026-10-25 已按用户指令“根据交接文档发起生成”提交 v7 一次（HTTP 200，35.03s）并落盘；等待用户按 §15.6 验收。** 不把文档更新、草稿存在或方案方向认同等同于草稿通过／收费生成授权。

本轮对话指令：

- 用户反馈 v6：“最新验收版不合格，因为头部被缩小到诡异的尺寸”。
- 用户要求：“思考下一版生成方案，已经兜兜转转6个版本了还没调好”。
- 用户随后要求：“更新到交接文档中”。

已复核 v1–v6 原图、1011 v11 和各轮提示词。复盘用于解释新的控制方法，不替用户补作验收：

| 阶段 | 已记录的问题／变化 | 本轮分析 |
| --- | --- | --- |
| v1→v2 | v1 盔像现代钢盔；v2 改盔结构后，头身与 1011 不一致且脸重复 | 盔形和服饰设计可保留，整体未通过 |
| v3 | 同时放大头脸、缩短躯干、换宽方脸；随后用户指出脸过大过方 | 多个变量叠在一次生成中，难以固定头部占比与脸部轮廓 |
| v4 | 用户反馈“没变化” | 仍要求下颌几乎与颧骨等宽，仅改转角，没有落实收窄轮廓 |
| v5 | 脸型已修正，但头部仍偏大、盾不够大 | 保留修正后的脸与当前身体，以此作为本地比例草稿的来源 |
| v6 | 用户拒收，头部缩小过度；腰线、衣摆和手臂位置也有变化 | 同时缩头、扩盾导致模型重新构图，提示词中的“其他保持不变”未约束住整体 |

本轮方法：**在本地明确摆出头部与盾牌的大小，先由用户看图判断，再把这张单图交给模型修接缝。** 不再仅用“头大一点／小一点／接近1011”让模型重新决定调整幅度。1011 的宽帽檐不能单独充当头部宽度标尺；等高对照应同时看脸、下巴、颈部、肩与腰的关系，不用帽檐宽度或主体包围盒代替人体比例。

### 15.2 已存在的本地草稿、对照文件与来源

**候选最终素材：尚无。** 下列均为分析派生文件，不是 v7 原始返图，不是完成的去背素材，也没有修改已有 Unity 资源。

**A. 待审单张绿底比例草稿，拟在用户确认后作为唯一生成输入：**

```text
C:\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\enemy1021_v7_proportion_study\v5_head90_shield115_GREEN_LOCAL_STUDY.png
1024x1024 RGB PNG, 761,593 bytes
SHA-256 cc5ca9b95277d18788b67d03b60c8e0cb96ba6b3115fd96ea34cb36d4f5eacd9
```

背景本地填为 RGB(0,159,62)，非透明。草稿仍有颈领绿缝、护颈残边等合成瑕疵，修复它们是拟生成目标；这些瑕疵不能当成新服饰设计。**比例仍待用户审阅，不能仅凭此路径存在就提交。**

**B. 等高四图对照，仅供审阅，绝不上传：**

```text
C:\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\enemy1021_v7_proportion_study\1011_v11_v5_local90_v6_EQUAL_HEIGHT_REVIEW.png
2240x876 RGB PNG, 1,472,261 bytes
SHA-256 fc0559a4486bcd5bfcfcfb32429345c93a4167de9b4baf62ab592999962da60c
```

从左至右：1011 v11／1021 v5／本地比例草稿／拒收 v6。按主体总高度等高展示、鞋底对齐，另附 96 px 高度缩略图；主体由粗略绿底分离得到，仅供视觉比较，不是精确人体测量或去背验收。

**C. 本地分层合成预览，仅供分析追溯，绝不上传：**

```text
C:\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\enemy1021_v7_proportion_study\v5_head90_shield115_LOCAL_MOCKUP.png
1024x1024 RGBA PNG, 1,444,424 bytes
SHA-256 f070200a18c1b45069d4c83c0e197dd7773f12b441efa114f863f43fcad3b968
```

该 RGBA 文件只是粗分离与合成中间预览，不是最终透明素材，不能交接给补边或接入环节。

草稿来源为 v5 原图，来源哈希与 §13.7 一致：

```text
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v5\enemy1021_v11_style_idle_v5.png
SHA-256 1dede32afe60025f8912a146d41a1bbf899fea81c5cac3b131db699e0b00ad38
```

本轮已完整解码并回读 A、B、C 的尺寸、模式、字节数和哈希。v5、v6 与 1011 v11 原图哈希仍与历史记录一致；未覆盖原图。上述 `Library/Locus/tmp/` 文件是本机草稿，不能假设另一设备已有；路径缺失或哈希不同须说明现状，不自动重做并当成同一已审输入。

### 15.3 本地调整幅度、参考职责与待确认项

| 部位 | 本地草稿实际操作 | 拟保持的边界 |
| --- | --- | --- |
| 头、脸、盔与护颈 | 以颈部附近为锚点，宽高缩到 v5 的约90% | 保留 v5 收拢脸颊与下颌后的独立面孔，不恢复宽方腮，不另换五官 |
| 木盾 | 宽高各增至 v5 的约115%，主要向左、向下扩展 | 保留木板、铁边、单盾和画面左侧持盾关系；不继续向上遮脸 |
| 身体、腰带、手脚 | 未做几何变换，沿用 v5 | 不拉长躯干，不移动腰带、拳头与双脚；局部接缝可能需要补画 |

90%／115% 仅描述当前本地草稿的实际线性变形，不是已通过的数值标准，也不是要求模型再执行一次缩放。原计划中的整体短壮比例仍由用户目视判断。

**盾牌位置待确认：** 当前草稿盾顶沿用了 v5 侧方布局，未落实 §2.3／§14 所写的“盾顶低于下巴”；它在脸的侧方且不遮脸，盾底也不按 §14 的脚踝位置强行对齐。这是待审草稿与旧文字目标的差异，不代表旧目标被修改或盾牌布局已通过。用户若仍要求旧位置，应先在本地草稿明确调整并重新展示，不能把相互矛盾的盾顶／盾底要求追加给模型自行解决。

参考图职责：

- **唯一拟上传图：§15.2 A**。同时提供已摆好的比例、盾兵身份、v5 修正面孔、服饰、站姿与现有画法。
- **v5 原图：仅本地来源与身份核对，不追加上传。** 防止旧的大头比例与已调整草稿同时竞争。
- **1011 v11：仅本地等高、画法和重量对照，不上传。** 不让模型重新解释帽檐宽度，也不把其脸、尖盔、红头巾、刀或衣服带回。
- **v6：仅失败对照，不上传。** 不继续继承其小头和身体重排。
- 不上传四图对照、RGBA 中间预览、六脸裁图、旧 1021、场景或盾牌单图。

### 15.4 拟用参数与发起边界

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[]: ONLY v5_head90_shield115_GREEN_LOCAL_STUDY.png (section 15.2 A, pending user confirmation)
prompt: section 15.5 English block only, including its final constraints paragraph
read timeout: 900 seconds
automatic retries: 0
```

沿用历史已执行字段组；本轮未验证接口、未发探针、未提交请求。`background` 不传，均匀绿底由词指定，不请求透明。`quality=max` 与 `input_fidelity=high` 保留；它们不保证只修接缝或逐像素保留。

发起前核对：用户已确认的具体草稿及哈希、盾牌位置、唯一输入、完整双语词、负面约束、参数和输出位置。取得明确发起授权后才提交一次；用户若同时明确批准所示草稿与发起，沿用该授权，不机械重复询问。

当前“更新到交接文档中”只授权本文件更新。若比例尚不合适，先本地调整并展示更新草稿，不发一次生成来试大小；本轮记录不自动执行这些后续调整。参考职责、草稿比例或提交词有实质变化时，先展示更新内容供用户确认。

失败、超时、结果未知或用户拒收就保存真实记录后停止，不自动重试、换图、改词、降档或换模型。凭据仅从环境变量 `MUSK_API_KEY` 读取，不写入文档、日志或回复。

### 15.5 拟提交完整中英文文本

沿用本轮对话的单图修缝词，并将当时正文列出的限制写成统一的最后一段，避免实际发起侧另行补词。本节是供审阅的 v7 草案，不是已批准或已提交的请求。

**中文供审阅，不提交：**

整理这张已经完成比例调整的盾兵合成图，使它成为一张完整连贯的游戏角色立绘。输入图中头部、头盔、身体和盾牌的相对大小与位置已经确定，请沿用这些比例和站姿。

修复颈部、内领、护颈与肩部的拼接，以及扩大后的盾牌边缘和遮挡关系。清除拼接造成的绿色缝隙、重复边缘和残留碎片，补全必要的局部结构，并使这些位置的光影与现有硬边笔触自然衔接。

保留当前收拢的脸颊与下颌、厚眉、集中注视的小眼、紧闭的嘴和中央短髯；两颊与上唇干净。保留分片拱盔、蓝布衣、棕皮甲、宽皮带、皮护腕、厚靴和木盾。盾在画面左侧，持盾手臂藏在盾后；画面右侧的手空置，脸与双脚完整可见。

保持现有有方向、疏密变化的硬边笔触和分材质塑形。单人单盾，均匀绿底 RGB(0,159,62)，全身完整并留有边距。

不要再次缩放头部、头盔或盾牌，不拉长躯干，不移动腰带、手与双脚，不恢复宽方腮，不增加武器，不加入刀兵的尖盔、红头巾或其他装备。不增加场景、文字、UI 或特效。

**唯一英文提交块（拟用，含负面约束）：**

```text
Clean up this proportion-adjusted composite into one coherent game-character illustration. The relative sizes and positions of the head, helmet, body and shield are already established in the input. Retain these proportions and the existing guarded stance.

Repair the compositing joins around the neck, inner collar, helmet neck guards and shoulders, and resolve the enlarged shield's edges and overlaps. Remove green gaps, duplicated cut edges and leftover fragments caused by compositing. Complete only the local structures needed to make these joins natural, with lighting and hard-edged brushwork consistent with the existing figure.

Preserve the current tapered cheeks and jaw, heavy brows, small focused eyes, firmly closed mouth and short central chin beard. Keep the cheeks and upper lip clean-shaven. Retain the segmented arched helmet, indigo clothing, brown leather armor, broad belt, leather bracers, heavy boots and wooden shield. Keep the shield on screen-left with the holding hand and forearm concealed behind it. Keep the screen-right hand empty, the full face visible and both feet visible.

Preserve the existing directional hard-edged brushwork with varied size and density and distinct material treatment. One complete soldier with one shield on a uniform green background RGB(0,159,62), with clear margins.

Do not resize the head, helmet or shield again, lengthen the torso, move the belt, hands or feet, restore the wide square jaw, add weapons, or introduce a pointed helmet, red headscarf or any other equipment from the sword soldier. Do not add a scene, text, UI or effects.
```

文本校验：LF 行尾、代码块正文、各行无额外缩进、不含首尾空行及结尾换行，1,604 字符，SHA-256 `9cf99bec3d156487930bd13c386945f2071e773ddbd4be1264c7d422a620f907`。负面约束已并入最后一段，不另传 `negative_prompt` 字段，不拼接 v1–v6 的词、不追加未经展示的段落。中文、哈希、参数和说明不提交。

### 15.6 验收顺序与修改范围

**先审本地草稿，不先发起生成：**

1. 用 §15.2 B 等高图及缩略图检查头、脸、颈肩与身体关系：避开 v5 的大头，也不能落回 v6 的小头；成年脸型、眉眼与中央短髯保留。
2. 单独判断盾的宽高和覆盖范围是否足够，明确 §15.3 的盾顶／盾底位置差异是否可接受；v6 盾大不代表盾已单独通过。
3. 确认身体、腰带、拳头、双脚与 v5 一致，当前头盔、蓝衣棕皮甲、护腕、靴子和守阵关系没有换设计。草稿接缝瑕疵属于待修项，不当作最终画面通过。

**如后续获授权生成，先查是否守住输入，再查修缝效果：**

1. 与获批草稿在同尺寸下对照或叠加，检查头盔顶、脸颊／下巴、腰带、拳头、双脚、盾四边的大小与位置；不因修缝又出现整体缩放、躯干拉长、腰线或手脚重排。整体比例再变则记为修改范围失控。
2. 颈部、内领、护颈与肩部连接完整，没有绿缝、重影、重复轮廓或残片；盾缘与人体的前后遮挡合理，持盾手臂不穿盾。
3. 脸保持 v5 修正后的收拢轮廓、厚眉小眼、闭口与中央短髯，不恢复鼓腮宽方脸，也不换回1011络腮胡；画法、光向和布／皮／铁／木的材质连续。
4. 单人单盾，盾在画面左侧、右侧空手，脸与双脚完整可见，盾底离地；1024x1024、不透明绿底、留边完整，无文字、UI、场景或效果。

单图能减少竞争参考，但不能保证模型只改局部，不把高保真参数、哈希、差异率或自动测量当成视觉通过证据。图片由用户验收；拒收后记录停止，不自动重发，也不进入动画、去背、补边、Clip、Prefab、Play 或工程接入。

### 15.7 拟输出目录、真实记录与当前停点

**下列为本次 v7 实际交付位置，结果已生成：**

```text
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v7\enemy1021_v11_style_idle_v7.png
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v7\enemy1021_v11_style_idle_v7.request.json
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v7\enemy1021_v11_style_idle_v7.response.json
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v7\enemy1021_v11_style_idle_v7.status.json
```

发起前已回读确认该目录不存在；提交后目录内只有本轮 v7 原图与三份记录，未覆盖 v1–v6、草稿、对照图或 1011 v11。

本次记录按 §6 留档：获批输入路径／哈希、完整 prompt／哈希、参数、授权、提交时间、次数、HTTP／终态、原始输出字节数／尺寸／模式／哈希均见下条。原始返图已下载、完整解码并磁盘回读后才报告落盘，未本地修改；usage 只写服务实际返回值，未保存凭据、Authorization 头或完整签名 URL。

- 发起授权与日期：2026-10-25，用户指令“根据交接文档发起生成”；按 §15.4“用户同时批准草稿与发起即沿用该授权”处理，未另发确认问询。
- 提交词：§15.5 英文块逐字提交，1,604 字符，SHA-256 `9cf99bec3d156487930bd13c386945f2071e773ddbd4be1264c7d422a620f907`（与 §15.5 校验值一致）；负面约束已在末段，未另传 `negative_prompt`。
- 输入：`Library/Locus/tmp/enemy1021_v7_proportion_study/v5_head90_shield115_GREEN_LOCAL_STUDY.png`，761,593 B，SHA-256 `cc5ca9b95277d18788b67d03b60c8e0cb96ba6b3115fd96ea34cb36d4f5eacd9`（与 §15.2 A 一致，仅 1 张 `image[]`）。
- 提交：`POST https://api.muskapis.com/v1/images/edits`，multipart body 764,142 B，本地时间 2026-10-25 17:22:48；真实提交次数 **1**，无重试、无改参。
- HTTP 与结果：200，耗时 35.03s（读超时 900s）。响应回显 `size=1024x1024`、`quality=max`、`background=auto`、`model=gpt-image-2.5-sunburst`；usage input 1,123（image 560 / text 563）/ output 7,024 / total 8,147 tokens。
- 原图：`enemy1021_v11_style_idle_v7.png`，1,664,446 B，1024×1024，PNG Color Type 2（RGB），SHA-256 `5593157f031f99bdb771df7ac59c0c0f5c31ebc92532dc311628ff33ddda4ba6`；已从磁盘回读并完整解码（24bpp RGB）。四角背景采样 ≈ RGB(1–3,154–159,60–64)。
- 比例保持量化（仅作参考，不作验收结论）：与草稿逐像素比较，阈值 |30| 下差异 225,936 / 1,048,576 = **21.55%**，十个高度带均有改动，说明修缝同时整幅重绘。剪影对比：主体高 914→913（保持）；整体上移约 9–10 px（顶边 76→67、底边 989→979）；头部带行宽 192→190（高度 8%）、216→213（12%）基本未再缩放；盾区行宽 766→756（60%）、684→664（75%）略收窄；主体最左点 94→101。以上为行宽代理值，不等于真实人体测量。
- 用户比例、修缝、面孔与保留范围验收：**待用户按 §15.6 目视判断**；本轮未作视觉结论。
- 若比例仍不通过：按 §15.4 先本地改草稿并展示，不另发一次生成试大小。
- 动画、去背、补边、Clip、Prefab 与实机验收：未执行。

## 16. v8：收窄头盔并遮蔽耳朵

### 16.1 反馈与修改范围

**状态：2026-10-25 v8 已提交一次（HTTP 200）并经用户验收通过；本图成为 1021 新画风身份定稿，后续视频改造见 §17。**

只改头盔，不动其他：

- **收窄**：盔壳整体宽度与两侧外扩收小，贴合颅顶；正面看不再是罩在脸两侧的宽锅形。
- **遮耳**：两侧护片向下延到完全盖住耳部，盔缘与下颌之间不露出耳廓或皮肤。护片窄而贴着头骨侧面，止于颧骨之前，**脸、颧骨与下颌仍完整露出**；不得延长成遮脸护颊，也不得变成面罩。
- **结构保留**：分片拱顶与板材接缝、中央铁脊（止于额箍）、窄铆钉额箍（不是外翻帽檐）、耳后短叠片护颈。
- 保持 v7 的收拢脸颊与下颌、厚眉小眼、闭口、中央短髯、头身比例、蓝布棕皮甲、腰带护腕、木盾、守阵姿势、盾在画面左侧与右侧空手、构图与绿底。
- 不再缩放头、身或盾；不加鼻甲、盔缨、金边、红头巾、刀剑或新装备。

### 16.2 唯一输入

```text
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v7\enemy1021_v11_style_idle_v7.png
1024x1024 RGB PNG, Color Type 2, 1,664,446 bytes
SHA-256 5593157f031f99bdb771df7ac59c0c0f5c31ebc92532dc311628ff33ddda4ba6
```

只上传 v7 完整原图，不裁头、不缩放、不本地改盔。不追加 v5／v6、本地草稿、1011 v11、六脸裁图或场景图：多图并存会让模型再动一次整体比例。

### 16.3 参数与发起边界

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[]: ONLY enemy1021_v11_style_idle_v7.png
prompt: section 16.4 English block only
read timeout: 900 seconds
automatic retries: 0
```

只提交 §16.4 一个英文块，不拼接旧词，不追加未经展示的负面段。高保真不保证只改头盔，返图须按 §16.6 逐项核对。

### 16.4 实际提交文本

中文供检阅，不提交：

把图1盾兵的头盔收窄，让它贴合头骨——减少盔壳总宽和两侧外扩，读起来是一顶紧凑的分片铁帽，而不是罩在脸两侧的宽锅。两侧护片向下延到完全盖住耳朵，盔缘与下颌之间不露耳廓也不露皮肤；护片要窄、贴着头骨侧面，停在颧骨之前，使整张脸、两侧颧骨与下颌仍然完整露出，不许变成遮脸护颊或面罩。保留分片拱顶与接缝、中央铁脊（止于额箍）、窄铆钉额箍（不是外翻帽檐）和耳后短叠片护颈。其余一律不变：收拢的脸颊与下颌、厚眉小眼、闭口、中央短髯、头身比例、靛蓝布衣、棕皮甲、腰带护腕、木盾、守阵姿势、盾在画面左侧、右侧空手、双脚可见、构图与均匀绿底。保持现有硬边笔触与分材质处理。不要重新缩放头、身体或盾，不加鼻甲、盔缨、红头巾、金边、遮脸护颊、武器或其他装备，不加场景、文字、UI 或特效。

```text
Narrow the helmet of the shield soldier in image 1 so it fits closely around the skull. Reduce the overall width of the shell and the flare at its two sides: it should read as a compact segmented iron cap sitting snug on the head, not a wide pot that overhangs the cheeks.

Extend the helmet's side plates down until they cover both ears completely. No ear and no strip of skin should remain visible between the helmet edge and the jaw. Keep these side plates narrow and structural, following the side of the skull. They must stop in front of the cheekbone so the whole face, both cheekbones and the jaw stay fully exposed. Do not turn them into full cheek guards and do not build a face mask.

Preserve the segmented arched crown with its broad plate seams, the slightly raised central rib that ends at the brow band, the narrow riveted brow band instead of a flared brim, and the short overlapping neck-guard plates behind the ears.

Keep everything else exactly as it is: the current tapered cheeks and jaw, heavy brows, small focused eyes, firmly closed mouth and short central chin beard, the current head size and body proportions, indigo clothing, brown leather armor, belt and bracers, the wooden shield, the guarded pose, the shield on screen-left, the empty screen-right hand, both feet visible, the framing and the uniform green background.

Preserve the existing directional hard-edged brushwork with varied density and the distinct material treatment. Do not rescale the head, body or shield. Do not add a nasal guard, plume, red headscarf, gold trim, cheek guards that hide the face, weapons or other equipment, and do not add a scene, text, UI or effects.
```

**文本校验**：LF 行尾、代码块正文、不含首尾空行及结尾换行，1670 字符，SHA-256 `696dabcf433dc40bc6e5ab98d961d0e7708d720da82d418713015cd1d1528ea6`。只提取以 `Narrow the helmet` 开头的唯一英文代码块；中文、哈希与参数不提交。

### 16.5 输出与记录

```text
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v8\enemy1021_v11_style_idle_v8.png
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v8\enemy1021_v11_style_idle_v8.request.json
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v8\enemy1021_v11_style_idle_v8.response.json
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v8\enemy1021_v11_style_idle_v8.status.json
```

起草时 v8 目录不存在。发起前再次确认，不覆盖 v1–v7、草稿、对照图与 1011 v11。记录沿用 §6：真实输入／文本哈希、参数、授权与提交时间、次数、HTTP／终态、输出字节数／尺寸／模式／哈希；不保存凭据与签名 URL。

### 16.6 验收

与 v7 同尺寸并排，由用户目视判断：

- **盔宽**：正面看盔壳不再明显宽出颅顶与脸侧，两侧外扩消失，读作紧凑分片铁帽。
- **遮耳**：两侧护片盖住耳部，盔缘到下颌之间看不到耳廓或肤色漏缝；遮耳没有变成遮脸护颊或面罩。
- **脸完整**：颧骨、下颌、两颊、口鼻与中央短髯仍露出，脸型仍是 v5 修正后的收拢轮廓。
- **结构**：分片拱顶与接缝、中央铁脊止于窄铆钉额箍、耳后短叠片护颈都在，没有回到外翻帽檐或现代钢盔轮廓。
- **保留**：头身比例、蓝布棕皮甲、腰带护腕、木盾四边、守阵姿势、构图与绿底与 v7 一致；无刀剑、无新肩甲、无第二面盾。
- **范围**：与 v7 的差异应集中在头部；若躯干、腰带、手脚或盾被重排，记为修改范围失控。
- **完整性**：单人单盾、1024x1024 PNG、绿底接近均匀、全身与盾角完整留边。

未通过则记录具体原因并停止，不自动改词重发；不去背、不选帧、不补边、不进 Play、不改工程资产。

### 16.7 发起后回填

- 版本：v8／收窄头盔并遮蔽耳朵。
- 起草与发起：起草 2026-10-25；授权指令“发起生成，试试效果”。
- 提交词：§16.4 英文块逐字提交，1,670 字符，SHA-256 `696dabcf433dc40bc6e5ab98d961d0e7708d720da82d418713015cd1d1528ea6`（与 §16.4 校验值一致）；无旧词拼接、无额外负面段。
- 输入：`enemy1021_v11_style_idle_v7.png`，1,664,446 B，SHA-256 `5593157f031f99bdb771df7ac59c0c0f5c31ebc92532dc311628ff33ddda4ba6`（与 §16.2 一致，仅 1 张 `image[]`）。
- 提交：`POST https://api.muskapis.com/v1/images/edits`，multipart body 1,667,051 B，本地时间 2026-10-25 18:02:32；真实提交次数 **1**，无重试、无改参。
- HTTP 与结果：200，耗时 34.82s（读超时 900s）。响应回显 `size=1024x1024`、`quality=max`、`background=auto`、`model=gpt-image-2.5-sunburst`；usage input 1,140（image 560 / text 580）/ output 7,024 / total 8,164 tokens。
- 原图：`enemy1021_v11_style_idle_v8.png`，1,853,973 B，1024×1024，PNG Color Type 2（RGB），SHA-256 `9b879ca2eddeebfe3e7b1ab56716df998f73bcf8cb6a39a12a550b3d4de99efe`；已从磁盘回读并完整解码（24bpp RGB）。四角背景采样 ≈ RGB(3–5,146–155,62–64)，比 v7（≈154–159）略暗。
- 修改范围量化（仅作参考，不作验收结论）：与 v7 逐像素比较，阈值 |30| 下差异 52,614 / 1,048,576 = **5.02%**；分布集中在高度 10%–40% 带（头部，峰值带 2 为 19,879 px），躯干与盾所在高度带仅 0.8–3.4 千像素；主体总高 913→914、底边 979→979 未变。
- 剪影对比（行宽代理值）：高度 8% 行 190→173、12% 行 213→193、16% 行 232→196、20% 行 262→212；30% 起各行为 626／714／755／665／554，与 v7 的 626／714／756／664／553 基本一致。即盔壳与头部剪影明显收窄，身体与盾几何未动。
- 用户盔形、遮耳与保留范围验收：**待用户按 §16.6 目视判断**；本轮未作视觉结论。
- 动画、去背、补边、Clip、Prefab 与实机验收：未执行。
- 用户验收：2026-10-25 用户明确“已验收”，v8 成为 1021 新画风身份定稿；下游视频改造交接见 §17。

## 17. v8 身份图验收与 1021 视频改造交接（2026-10-25）

### 17.1 验收结论

**用户于 2026-10-25 明确“已验收”v8。** 1021 的新画风身份静帧自此定稿为 v8；v1–v7 全部转为历史版本，不再作为身份、比例或画法输入。用户同时说明“即将在别的对话发起对 1021 的视频改造”，因此本节只交接状态、规格与不可变项，**不发起任何视频，不去背、不补边、不改工程资产**。

- 已验收：v8——盔壳收窄、两侧护片盖住耳朵，面部与身体沿用 v7（脸沿用 v5 修正后的收拢轮廓）。
- 未完成：没有任何视频源片、没有去背帧、没有补边、没有 Clip／Prefab 改动、没有实机验收。
- 本节不是视频任务的收费授权，只登记“下游可以拿什么、不能改什么”。

### 17.2 已验收身份图（下游唯一身份基准）

```text
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v8\enemy1021_v11_style_idle_v8.png
1024x1024 RGB PNG, Color Type 2, 1,853,973 bytes
SHA-256 9b879ca2eddeebfe3e7b1ab56716df998f73bcf8cb6a39a12a550b3d4de99efe
背景键色实测：四边 24px 内众数 RGB(5,156,65) 与 RGB(5,148,62)；边带 R≈5、B≈63、G 中位 151、范围 138–160
主体（非键色）约占画布 43%
```

未裁剪、未缩放、未本地改色；原始返图即验收版，提交与解码记录见 §16.7。

### 17.3 身份美术不可变项（视频改造必须继承）

- **头部**：低矮分片拱顶铁盔，板材接缝清楚，中央略凸铁脊止于窄铆钉额箍（无外翻帽檐、无盔缨、无金边、无面罩、无鼻甲）；两侧护片向下盖住耳朵，脸、颧骨与下颌完整露出。
- **面部**：收拢的脸颊与下颌（不是宽方腮）、厚直眉、小而集中的眼、紧闭的嘴、下巴中央一撮短髯；两颊与上唇干净，无边鬓络腮胡（与 1011 明确区分）。
- **服饰**：暗靛蓝交领布衣（浅色内领可见）、暖棕无袖皮质短甲止于腰、宽素皮腰带＋小铁扣、短皮护腕；腰带以下是独立布衣下摆、深色宽裤、绑腿、稳固短靴。皮革哑光厚实、布料用大褶皱、铁件高光集中。
- **盾**：单面高矩形木盾，竖木板、朴素铁边、少量铆钉；解剖学右手持盾、盾在画面左侧，持盾手与前臂藏在盾后；画面右侧左手空置。无刀剑、无第二面盾、无新肩甲。
- **画法**：v11 体系中有方向、疏密变化的硬边笔触、深色结构边与分材质塑形；不要平滑卡通、统一碎面或规则方格。
- **比例**：头身以 v8 为准；v5 的大头与 v6 的小头都不再作基准。

### 17.4 视频阶段输入规则（防止已定稿身份被再改一次）

1. 身份参考只用 §17.2 的 v8 原图（或等比缩小副本）。**不要同时投喂 v5–v7 或 1011 v11 当身份图**，多图竞争会让模型重新选比例。
2. 旧 1021 素材只能用于核对机制与盾的方向，不作为形象输入。
3. 动作语义、持盾表现与方向规则按 `design/enemy-1021-animation.md`、`design/enemy-1021-shield-guard.md`；持盾时盾画在角色本体帧内，破盾后的独立掉盾接管与胆怯家族沿用现有工程逻辑。
4. 视频阶段不得改脸、改盔形、改服饰、加刀剑、换盾数量或改站姿比例。

### 17.5 下游流程与验收分离（每步单独验收）

> **下游视频改造的活交接已另立文件**：`plan/enemy1021-v11-video-replacement-handoff.md`。该文件含 1011 形象替换案例复盘（重点是面部表情塑造手法）、1021 源片与工程落点实测、v8 归一化上传副本，以及第一条任务（持盾 Idle v8）的完整参数、参考职责、双语提交文本与验收闸门。**当前状态：Idle v1 成片已落盘但不进 `Assets/`；用户判定头体量与新版 1011 不一致（头小身大），修正方案与暂停点见该文件 §12（出图因服务商余额耗尽未执行，零成本本地草稿已备好，等余额恢复续接）。** 本节保留为规则总纲。

1. **视频源片**：只验收动作语义、镜头与站姿是否与 v8 一致。
2. **选帧**：先出图给用户看；未验收不得写入 `Assets/`。
3. **去背**：按每帧实测键色标定容差——v8 的 G 在 138–160 波动，不要沿用历史批次的固定容差。
4. **尺寸与脚底对齐**：v8 主体高 913–914 px（1024 画布内），脚底行 979；实际帧以当时测量为准，不逐帧按高度归一化。
5. **补边、Clip、Prefab、第一关接入**各自验收；接入前重读现有资源与引用，保留 GUID、`.meta` 与旧素材备份。
6. 用户既定边界：先接第一关并通知开游戏验收；没有对话中的明确自检要求，不自动进 Play 或驱动动画。

### 17.6 待确认与已知风险

- **盾顶位置**：v8 的盾顶沿用 v5 的侧方布局，未落实早期 §2.3／§14 所写的“盾顶低于下巴”。若下游仍要旧位置，应在静态图阶段本地改好再确认，不要交给模型在视频里自行解释。
- **遮耳护片**：转头、受击等动作里护片容易与下颌挤在一起或被画成护颊；出现挡脸或穿模应单独记录并修，不要顺手改盔结构。
- **背景不均匀**：v8 的 G 范围 138–160，比 v7（≈154–159）更宽，抠图容差与描边修复按实测标定。
- **头部偏小风险**：v8 比 v7 的头部行宽收窄约 9%–19%（高度 8%–20% 带）。若视频里头部读起来偏小，由用户判断后另立方案，不自行放回头部比例。

### 17.7 现有工程落点（本轮未改，供接入时核对）

- 精灵目录 `Assets/Sprites/Enemy/Enemy1021/`
- Idle `Assets/Animations/Enemy_1021_Idle.anim`
- 控制器 `Assets/Animations/Enemy_1021.controller`
- Prefab `Assets/Resources/EnemyPrefabs/Enemy_1021.prefab`

以上仍是旧美术，本节未改动任何一处；§8 的替换流程自本节起成为下游执行路径。新身份验收通过后，其余动作应逐项核对，不能只换 Idle 就宣称 1021 全套美术已替换。
