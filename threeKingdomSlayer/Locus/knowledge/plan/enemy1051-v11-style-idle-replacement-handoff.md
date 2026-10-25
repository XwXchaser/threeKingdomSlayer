---
id: kd_a3425871-5399-4ff3-9e78-98023d6f3ae9
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: 105新画风制作入口，沿用1051美术编号。首轮以已验收1051 v3-A定蒙面轻装身份、比例和持弓姿势，以1011 v11定画法；1021 v8仅本地对照。两输入哈希、完整双语词、max参数、输出路径及比例修订规则已准备，尚未生成。后续射箭保留旧105 Attack三图实际姿态参考，新身份定稿后再更新动作方案。
---

# Enemy 105 新画风制作交接：1051 蒙面轻装弓手

## 0. 接续入口与本轮交付

**当前状态：2026-10-25，制作方案已准备，尚未发起生成。首轮只做一张持弓 Idle 身份图。** 用户要求“已验收，我需要一份交接文档发起105敌人的新画风制作，根据已有的1011和1021重塑经验准备这份文档”。这次授权的交付物是本文件，收费生成、视频、去背、补边和工程替换分别处理。

首轮采用最小改动：**以已验收的 1051 v3-A 决定身份、服饰、比例与持弓姿势，以已验收 1011 v11 决定绘制方式。** 1021 v8 用于本地对照和复用制作经验，不作为第三张上传图。先确认画法，再处理用户指出的具体造型问题，不在同一轮另换脸、换衣、缩头或改武器尺寸。

- 实际两张输入、顺序与哈希：§4。
- 美术保留范围：§5；接口字段：§6。
- 完整中文方案和唯一英文提交词：§7。
- 输出路径与回填：§8；验收及修订方法：§9。
- 定稿后的动作制作约束：§10；可复制给发起侧的指令：§11。

**本文件是 105 新画风制作入口。** 旧 1051 的身份、Idle 视频及失败射箭记录继续保存在 `plan/enemy1051-identity-generation-handoff.md` 等历史文档中；它们的“当前 v3／射箭 v2”不代表本轮已经生成，也不应直接替本轮发起旧身份任务。

## 1. 对象、编号与工程落点

105 是现有后排随机落点弓箭手。`1051` 沿用现有美术制作编号，表示 105 的新画风替身；本文件不新增运行时敌人编号，不建立或假定存在 `Enemy_1051.prefab`。

2026-10-25 已读取现有 Prefab 与 Controller：

| 范围 | 现有资源／观察 |
|---|---|
| Prefab | `Assets/Resources/EnemyPrefabs/Enemy_105.prefab`；`Enemy.enemyId=105`、`isRanged=true` |
| 精灵目录 | `Assets/Sprites/Enemy/Enemy5/`；Prefab 当前默认精灵为 `enemy5_idle.png` |
| Controller | `Assets/Animations/Enemy_105.controller` |
| Idle／Walk | `Assets/Animations/Enemy_105_Idle.anim`、`Assets/Animations/Enemy_105_Walk.anim` |
| Attack／HitFlash | `Assets/Animations/Enemy_105_Attack.anim`、`Assets/Animations/Enemy_105_HitFlash.anim` |
| 击飞／死亡 | `Assets/Animations/Enemy_105_Launched.anim`、`Assets/Animations/Enemy_105_Launched_Fall.anim`、`Assets/Animations/Enemy_105_Dead.anim` |
| 飞行物 | Prefab 直接引用 `Assets/Prefabs/arrow.prefab` |

这些是未来接入时的核对落点，本轮没有修改。造型制作不改变随机落点、攻击判定、发箭逻辑、箭矢资产、数值或阵列移动规则；不把另一兵种 106 纳入替换。

## 2. 可信基准与历史状态

| 基准 | 已确认的用途／边界 |
|---|---|
| 1011 刀兵 v11 | 2026-10-24 用户确认角色画风；新角色具体绘制语言以此校准。敞脸盔、红头巾、刀和甲是刀兵身份，不迁移到弓手。 |
| 1021 盾兵 v8 | 2026-10-25 已验收并定为该兵种身份图；不沿用其 v5 大头、v6 过小头或中间草稿作为比例基准。 |
| 1051 v3 阶段 A | 2026-10-18 已验收的蒙面轻装设计；本轮唯一身份输入。保留原图，不覆盖它。 |
| 1051 旧 Idle 视频／六帧 | 已验收，用户仍要求暂不去背。保留历史成果；本轮不重跑、不抠图，不把它们称为新画风动作。 |
| 1051 旧射箭 v1 | 因横弓、换手、反箭、偏离旧 105 姿态而不通过；不能作为动作生产源。 |
| 1051 射箭 v2 四参考方案 | 已准备、尚未提交，实际纳入旧 105 Attack1/2/3；新身份定稿后须替换身份输入并重新核对文本，不直接运行旧任务。 |

来源：`design/art-style-guide.md` §2.3、§3、§4.3–4.4；`plan/enemy1011-style-locked-dao-generation-handoff.md` §23、§25；`plan/enemy1021-v11-style-idle-replacement-handoff.md` §15–17；`plan/enemy1051-identity-generation-handoff.md` §2、§15–17。

本轮复读到 1051 v3-A 原图的胸前包布、护臂和腰布与早期文字的“薄面巾、小护片、短红布”仍有表现差异。**首轮以用户已验收原图的实际设计为准，不借重绘画风自行减布、收护腕或改装备。** 若用户另有明确造型反馈，再单列改动。

## 3. 从 1011、1021 继承的做法

| 已发生的问题／成功经验 | 本轮落实方式 |
|---|---|
| 1011 只加暖色、颗粒或多边形，没有得到目标画法；v11 按材质组织笔触才通过 | 用 v11 实图提供画法，布、皮、木、皮肤分别塑形，不以偏黄、色数或高频统计代替验收。 |
| 1011 v6／v8 变成规则大方格，v7 过于平滑，v10 全身统一碎面 | 保留完整大体积，硬边色簇大小和疏密变化；不要求固定像素格、两三阶色或全身同尺寸切片。 |
| 1021 反复用文字追头身，v6 同时缩头、扩盾后头过小且身体被改 | 首轮只重绘画法，保留 1051 已验收比例；有比例反馈时，先等高并排，必要时做可审阅的本地草稿。 |
| 1021 v7 用本地草稿明确头／盾大小，再单图修接缝；v8 只收盔壳、加遮耳护片后通过 | 具体造型修订每轮固定目标，保留已通过部分；草稿先给用户看，获准后才作下一次生成输入。 |
| 多张全身角色图会竞争身份、服饰和比例 | 首轮只传两图，1051 管身份，1011 管画法；1021、旧 105、脸裁图和场景图只作本地查验。 |
| 1021 实际通过图的盾位置与早期文字仍有差异 | 如实记录输出和用户验收，不把早期意图写成已实现；弓手的左右关系直接取本轮身份图，不套盾兵方向。 |

等高对照应看头、下巴、颈、肩、腰和腿的关系；武器长度、帽檐或前景包围盒宽度不能单独充当人体标尺。不规定未经验证的固定头数，不用统一身体尺寸抹掉兵种差异。

## 4. 实际参考图、职责和上传顺序

### 4.1 图 1：1051 已验收身份图

```text
C:\Users\steam\Pictures\gptGen\enemy1051_identity_v3\enemy1051_identity_stageA.png
1024x1024 RGB PNG, 1,088,256 bytes
SHA-256 553f30c66b2e91713670fdda8d69bf9d5869a2095a1c391fa3e694a2802288a0
```

**唯一决定身份、服饰、遮面、配色、比例、体量、弓／箭壶、姿势和构图。** 表面允许重绘，不锁定原有均匀碎纹或卡通明暗。原图不裁切、不缩放、不预先改色；本轮不改成旧视频使用的归一绿幕副本。

### 4.2 图 2：1011 v11 已验收画法

```text
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v11\enemy1011_style_locked_dao_v11.png
1024x1024 RGB PNG, 1,598,516 bytes
SHA-256 67da58389ed877ce7183b98ad06245c7ddc39eba79ce055fc3792d7e0cbb2c39
```

**只决定绘制语言**：深色结构边、完整体积、有方向且大小／疏密变化的硬边笔触、分材质塑形。不复制脸、头盔、红头巾、甲、刀、麻绳腰带或厚靴；不按它的头盔外宽重新缩放弓手。

### 4.3 本地对照，不上传：1021 v8

```text
C:\Users\steam\Pictures\gptGen\enemy1021_v11_style_idle_v8\enemy1021_v11_style_idle_v8.png
1024x1024 RGB PNG, 1,853,973 bytes
SHA-256 9b879ca2eddeebfe3e7b1ab56716df998f73bcf8cb6a39a12a550b3d4de99efe
```

用于查看第二个已通过兵种的体量、布／皮区分、装备厚边和与刀兵的差异化。它的盾、铁盔、蓝衣、脸和盔壳比例都不是弓手设计输入。

本机已完整解码并核验上述三图哈希。发起前复核**正好两个** `image[]`，图 1→图 2，不自动追加或交换顺序。源文件缺失或哈希不同则先定位同一素材，不临时换图；异机需传递同一 PNG，不能仅复制路径。

## 5. 首轮身份保留与重绘重点

- **轮廓与气质**：正面警戒站姿、宽肩、紧凑躯干、结实四肢和稳定双脚；保留当前头身。轻装来自装备少，不把身体拉细拉长，也不穿上刀兵／盾兵的重装。
- **遮面**：暗褐灰橄榄头巾与下半脸包布；只留窄暖橙眼光。鼻、嘴、下巴和脸颊仍遮住，不增加露脸五官，不借新画风换脸。
- **服饰**：灰褐无袖交领粗布短褂、露出的上臂／手部／膝及部分小腿、深棕短裤、脚踝绑带、低帮皮鞋；棕腰带、现有红腰布和一小段土黄结带。土黄不扩成大面积亮黄服装，身体不统一染橙黄。
- **弓与手**：一把木弓竖置于画面右侧，由画面右侧手握持（角色解剖学左手）；画面左侧手空置（角色右手）。弓弦连通，无搭箭、不拉弦、不射击、不换手、不镜像。
- **箭壶**：在画面左肩后，保持原图少量箭羽、背带和背负关系，不生成手持箭、第二把弓或第二武器。
- **布料**：用有方向的大褶皱解释包布、胸腹与衣摆，保留大明暗面；不铺统一细噪点、木纹或盔甲切片。
- **皮革与木**：带、护臂和鞋有厚边与接缝，反光收敛；木弓沿弧度和木材方向组织色簇，绑绳与握把清楚，弦不被笔触吞掉。
- **皮肤**：手臂和手用简洁暖色面表现体积，肘、拳、握弓处有明确结构暗部；不堆满金属磨损、石面切片、平滑塑料高光或写实毛孔。

本轮不另加胸甲、肩甲、铁盔、面具、刀、盾、披风或长靴。全身、弓梢、弦、双手、鞋和箭羽完整入画，绿底是制作背景，不属于角色色板，也不是透明素材。

## 6. 首轮接口字段与发起边界

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
  1. enemy1051_identity_stageA.png
  2. enemy1011_style_locked_dao_v11.png
prompt: section 7.2 English block only
read timeout: 900 seconds
automatic retries: 0
```

字段沿用 1011 v11／1021 新画风的已执行参数组合，本轮未调用接口或做在线探针。不单独请求透明、不加第二段语义去背、不附加独立负面词；`background` 字段省略，由英文词保留图 1 绿底。`quality=max` 与 `input_fidelity=high` 分别设置，不能把保真参数当作身份或比例锁定保证。

发起侧应展示／复核两图顺序与职责、完整中英文词、字段和输出目录。**本轮写文档不等于发起授权。** 用户已明确按本文件完整方案发起且关键条件未变时，沿用该授权；若换图、实质改词、改质量或构图，须先重新审阅。只提交一次，失败、超时、结果不明或不通过均保存记录并停止，不自动重试、降档、换模型或改词重发。

凭据只取环境变量 `MUSK_API_KEY`。不在文档、请求留档或回复中保存密钥、Authorization 头或完整签名 URL。

## 7. 首轮完整提示词

### 7.1 中文对照（供用户审阅，不提交）

按图 2 的游戏角色画法重绘图 1 的蒙面弓手。只有图 1 决定他的身份、服饰、头身关系、身体尺寸、装备、姿势与构图。图 2 只提供画法，不复制其脸、头盔、红头巾、护甲、刀、麻绳腰带或厚靴。

保留暗橄榄褐头巾和下半脸包布、窄橙眼、灰褐无袖短褂、裸露的结实手臂与部分小腿、棕色短裤、脚踝绑带、低帮皮鞋、小段黄结带与红腰布。脸仍遮住。保留画面右侧的木弓和弓弦、画面左侧的空手，以及左肩后的箭壶。没有搭箭、拉弓或射击姿势。保留宽肩、紧凑躯干、四肢和稳定站姿，不拉长、瘦化或重新缩放人物。

使用有方向、大小和疏密变化的硬边笔触、深色结构边及清楚的大体积。布料用大褶皱；皮革用收敛的反光与厚边；木材笔触沿木弓纹理和弧度；皮肤用简洁暖色面。细节集中于褶皱、关节、握持和材质交界。避免全身铺满细小多边形、规则方格、均匀颗粒或平滑卡通明暗。保留图 1 的低饱和配色与绿色背景，不整体染黄或染橙。只有一个完整弓手，弓梢、弦、双手、箭羽和双脚全部留在框内。

### 7.2 英文实际提交词（唯一）

```text
Repaint the masked archer in image 1 using the game-character drawing style of image 2. Image 1 alone defines his identity, costume, head-to-body relationship, body size, equipment, pose and framing. Image 2 supplies drawing style only; do not copy its face, helmet, red headscarf, armor, sword, rope belt or heavy boots.

Keep the dark olive-brown head wrap and lower-face covering, narrow orange eyes, gray-brown sleeveless tunic, bare muscular arms and lower legs, brown cropped trousers, ankle wraps, low leather shoes, small yellow cloth tie and red waist strip. Keep his face covered. Preserve the wooden bow and string on screen-right, the empty hand on screen-left, and the quiver behind the screen-left shoulder. No nocked arrow, drawing or shooting pose. Retain his broad shoulders, compact torso, limbs and planted stance without lengthening, slimming or rescaling him.

Use directional, hard-edged brush marks with varied size and density, dark structural edges and broad readable volumes. Describe cloth with broad folds, leather with restrained highlights and thick edges, wood along the bow's grain and curvature, and skin with simple warm planes. Concentrate detail at folds, joints, grips and material boundaries. Avoid an all-over pattern of tiny polygon facets, regular square blocks, uniform grain or smooth cartoon shading. Retain image 1's muted colors and green background without a yellow or orange wash. One complete archer only; keep the bow tips, string, hands, quiver feathers and both feet inside the frame.
```

正文和负面约束已合在这一个英文块内，**无独立负面提示词**。只提交代码块内容，LF 换行，无首尾空行、无结尾换行：**1,536 字符**，UTF-8 SHA-256 `cf5c18a010982872c2a6e288f999f7e65abfcd71eee58ffb6d841f755455e0f3`。中文、哈希、说明、1021 历史词和旧 1051 长服装改写词都不提交。

## 8. 输出、检查与回填

### 8.1 计划输出（尚未创建）

```text
C:\Users\steam\Pictures\gptGen\enemy1051_v11_style_idle_v1\enemy1051_v11_style_idle_v1.png
C:\Users\steam\Pictures\gptGen\enemy1051_v11_style_idle_v1\enemy1051_v11_style_idle_v1.request.json
C:\Users\steam\Pictures\gptGen\enemy1051_v11_style_idle_v1\enemy1051_v11_style_idle_v1.response.json
C:\Users\steam\Pictures\gptGen\enemy1051_v11_style_idle_v1\enemy1051_v11_style_idle_v1.status.json
```

本机准备时该目录不存在。发起前再次检查，已有结果先核对来源，不覆盖、不擅自改名重发。以上是当前主机的明确计划路径，异机按实际用户目录解析并在提交前列明。

留档记录实际输入顺序／完整哈希、实际英文词／哈希、字段、用户发起授权、提交时间、真实次数、HTTP／终态和输出尺寸／模式／字节数／哈希；费用只记服务实际返回。响应保留可审计内容，剔除凭据、完整签名 URL 和无须写入文档的内嵌图片数据。

下载后完整解码并从磁盘回读，再报告“已落盘”；实际附图交给用户。记录绿底色值与均匀度，不因 PNG 扩展名声称有透明通道，不因 HTTP 200 或工具附件成功声称用户已验收。

### 8.2 发起后回填

- 版本：1051 新画风 v1／1011 v11 画法重绘。
- 准备日期：2026-10-25。
- 发起授权／时间／真实提交次数：未提交。
- 输入与文本哈希：见 §4、§7.2，发起前复核。
- HTTP／终态、返图规格、输出哈希、费用：无返图。
- 用户画法／身份／比例／持弓关系验收：待生成后记录。
- 新画风视频、选帧、去背、补边、Clip／Prefab、实机验收：未执行。

## 9. 验收与下一版修订方法

先与 1051 v3-A、1011 v11、1021 v8 按**相同人物高度、脚底对齐**并排，再看缩小后的兵种剪影。对照图只供审阅，不上传。人物高度不以高出的弓梢、刀尖或箭羽代替；比较图的显示缩放不能回写原始返图。

1. **画法**：完整大体积、深色结构边和有方向的硬边色簇成立；布、皮、木、皮肤各有画法。不是原图仅变暖、颗粒增多、统一碎面或规则积木。
2. **身份**：仍是蒙面轻装弓手，头巾／面巾、露臂、短裤、低鞋与配色保持；没有搬来刀兵／盾兵的脸、盔甲或装备。
3. **比例**：头、肩、躯干、肘、腰、膝和脚仍有原图关系；没有突然大头／小头、长躯干、细胳膊或长腿。无需把弓手变成与盾兵同一轮廓。
4. **弓箭结构**：弓仍在画面右侧，握把、上下弓梢和弦清楚连续，左侧手空置，箭壶在左肩后；无搭箭、错误拉弓、换手、镜像或新增武器。
5. **完整性**：1024×1024 可完整解码，单人、全身及装备完整，绿色制作底接近均匀，无地面、投影、场景、文字或 UI。偏差如实报告，用户目视决定美术是否通过。

如果只剩比例／弓尺寸等局部问题，记录具体部位与相对位置，保留已通过画法；不要在同轮同时重做衣服、面巾、身体和武器。需要时先在 `Library/Locus/tmp/` 制作带明确来源的本地草稿，标注“比例草稿／非最终素材”，由用户确认调整幅度后，再准备单图修接缝方案。不得把分析蒙版或裁图冒充完成去背。

失败或不通过时停在本版记录，不自动提交 v2；用户静帧通过后，登记最终身份图路径／哈希和实际不可变项，再单独准备动作任务。

## 10. 新身份通过后，动作制作如何接续

以下是后续交接约束，本轮不执行，也没有准备可直接提交的视频词。

- **身份输入**：视频阶段以届时新验收的弓手原图为唯一身份基准；1011 v11、1021 v8 不继续作为视频身份参考，也不混投旧 1051 v3-A 让模型重新选画法和比例。
- **Idle**：沿用已验收“原地呼吸、双脚和骨盆稳、弓弦／箭羽不变、不取箭不拉弦”的动作语义。旧六帧和旧视频保留，不代表本轮已有新画风动作；视频、选帧、去背分步验收，既有暂缓去背要求仍有效。
- **Attack 必须使用实际姿态参考**：保留 `plan/enemy1051-attack-v2-pose-guided-video-preparation.md` 中旧 105 Attack1→2→3 的真实动作输入。新身份定稿后替换该方案图 1，更新其身份／画法／尺度文本，再审阅全部四图；不因为担心旧脸泄漏而自行删掉三张动作图，也不直接运行当前旧身份版本。
- **射箭几何**：正面竖弓，画面右侧手持弓、左侧手拉弦；箭尾／羽毛靠人物与弦，箭头朝玩家；明确满弓到一次快速松弦。先聚焦已搭箭→拉弓→释放→收势，不重复失败 v1 的横弓、反箭或完整取箭前置表演。
- **状态覆盖**：定稿后再按 §1 的 Idle、Walk、Attack、HitFlash、Launched_Rise／Fall、Dead 逐项核对需要制作的动作和现有程序表现；静帧过审不代表全套替换完成，不替用户安排本轮批量生成。
- **边界与接入**：源片→选帧→去背→尺寸／脚底→补边→Clip→Prefab／第一关分别交付；保留 GUID、`.meta` 和原件。不逐帧归一包围框或锚脚抹掉动作。接入后由用户开游戏验收，未在对话中明确要求自检时，不自动 Play、采样动画或对象池测试。

已核验存在、供后续新攻击方案沿用的三张旧姿态副本如下；**它们不是首轮静帧输入**，Library 不随 Git 迁移，异机需传递文件并复核：

```text
C:\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\enemy1051_attack_video_v2_10s\02_old105_attack1_pose_green_009F3E_1024.png
SHA-256 f7f14a4ceddb96859c93b233ce2c2818fd4e416aaa986942e5e5938d8bba4c96
C:\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\enemy1051_attack_video_v2_10s\03_old105_attack2_pose_green_009F3E_1024.png
SHA-256 c250042fcc4b0d52a17d6ae5d32f2a7f7beeb7bab371081ca6510ded41e7ccb7
C:\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\enemy1051_attack_video_v2_10s\04_old105_attack3_pose_green_009F3E_1024.png
SHA-256 99bc3f6052c4d266b481200457a63069132063af674a2b582f6610129b58fbad
```

三图均为 1024×1024 RGB 绿幕副本，源文件与制作方法见上述攻击交接 §3。旧脸、甲和红巾仍可见，新的动作方案须如实列出这项身份干扰风险，不能承诺文字分工会完全消除它。

## 11. 可复制给发起侧

> 请读取 `Locus/knowledge/plan/enemy1051-v11-style-idle-replacement-handoff.md`，从 §0 接续。现在只准备 105 弓手新画风 v1 的单张持弓 Idle 身份图：图 1 是 §4.1 已验收 1051 v3-A，唯一决定身份、蒙面轻装、比例、弓／箭壶和姿势；图 2 是 §4.2 已验收 1011 v11，只提供画法。正好两张 image[]，不上传 1021、旧 105 动作图或任何组图。使用 §6 全部参数与 §7.2 唯一英文正文，向用户展示两图职责、完整中英文词及输出路径，按已有明确发起授权执行，未获授权则等待。只提交一次，下载解码、磁盘回读、附图后交用户验收，按 §8 回填真实结果。若比例需改，按 §9 先明确调整幅度，不同时改头、身体和武器。不要运行旧射箭 v2，不做视频、去背、补边、Unity 替换或 Play 自检。
