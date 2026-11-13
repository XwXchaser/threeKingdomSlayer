---
id: kd_92f6e2f1-e96a-48c9-a4de-3d29761fa392
summary: 1013 曹军旗手及兵种比例交接：v2 2.6 返图观感尚可，v3 比例修正不采用。四兵种并排比例参考图已按 §2.15 生成，用户反馈效果不错；§2.15.9 总结可复用成功经验与边界。成图仍只是比例研究参考，不替换现有资产。
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 1013 督战单位 · 美术任务书

> **当前状态：已确定曹军持旗方向；鼓＋短旗初案、持旗 v1 人物比例、v2 阶段 A（2.5）、v3 双图嫁接均已被否决或不采用。**
>
> **1013 候选状态**：`Enemy1013ArtSource/02_IdentityImages/1013_identity_v2_stageA_2_6.png` 与 1011 不完全相似，但用户认为观感尚可；后续 v3 比例修正「变怪」，不采用。候选仅供形象方向参考，尚未完成加旗或正式验收。
>
> **最新任务：四兵种并排比例参考图已生成（§2.15 / §2.15.8），用户反馈效果不错。** 以 1011 为起点，并排看 1011、1012 山贼、1013、105 弓手的体型是否同族；成图只是对照参考图，**不替换任何已有角色 / 动画、不裁切为精灵、不进 Unity**。此前 v2 单图与 v3 双图的提示词、输入都不同，不能据此证明单图必然正确或双图必然失败。
>
> **【交接文档：`plan/enemy1013-art-handoff-20261108.md`】**
>
> **最新任务（2026-11-08 续）：1013 idle 形象裁切与后续视频内容（§2.15.10）。** **1013 的形象、Idle、Cast、Walk（奔跑）四项均已验收**；已验收内容归集到 `Enemy1013ArtSource/`（该文件夹只放验收内容；被否候选与过程产物已移出到 `C:/Users/steam/Pictures/gptGen/enemy1013_rejected/`）。**暂不接入 Unity**（未导入 Assets、未接 Animator）。已验收帧：**Idle 6 帧**（`04_IdleFrames/`，循环 P=16 起相位 f003）、**Cast 12 帧**（`03_CastFrames/`，举→挥→收回到 idle，首尾同为 idle 姿态）、**Walk 6 帧**（`05_WalkFrames/`，奔跑；步态周期 P=20 起相位 f033；成片出框 0 帧、首末帧 IoU 0.9962、旗无横向漂移）。两组源片人物身高 636 / 632px → 像素密度一致，落位 k 分别 0.951 / 0.972 → 游戏内人物身同为 605。详见 §2.15.10.11–§2.15.10.12。下一步（待指示）：落位写入 Assets/Sprites/Enemy/Enemy1013/ 并接 Animator；其余动作（受击三支／Dead／击飞链）按同口径推进（每支 6 帧、杆布与人物整张一起动、不走视频替换流）。Walk 的过程与结论见 `plan/enemy1013-art-handoff-20261108.md` 与 `memory/enemy1012-art-replacement-pipeline.md` §13。
>
> **Cast（号令）方案已备妥但**未提交（§2.15.10.2）**：锚图 `Enemy1013ArtSource/02_IdentityImages/1013_cast_anchor_green_834x1112.png`（3:4、身体 560px、旗上方留白 153px）+ 中英提示词 `Tools/visual_brief/prompts/1013_cast_v1.txt` / `.cn.txt` 已存盘并 dry-run 校验；用户选定 3:4 构图，但选择**先不提交**（等其确认动作设计/提示词）。
>
> **既有旗手方向**：单人持长杆方旌旗、无鼓；暂沿用旗顶总高约普通兵 1.25× 的构图目标。旗杆外挂与旧动作复用均是**待验证方案**，不能假设动作可直接复用，见 §2.8。不处理动画、Unity 资产与波次。
>
> **剧情阵营纠正**：玩家是张飞、属于刘备军；长坂坡敌人是**曹军追兵**。本文旧版写的「1011 / 1012 同阵营（张飞军）」是错误描述，不得作为本单位身份依据。
>
> 素材包：`Enemy1013ArtSource/`（现有角色参考 / 动作母本 / 截图；包内旧 README 如与本文冲突，以本文为准）。
> 风格依据：`design/art-style-guide.md`（高清像素化三国战场，以 1011 为角色比例参考）；`design/pixel-character-and-enemy-animation-spec.md` 仅取动作语义、完整性、透明度与交付检查，旧色阶 / 描边数值不凌驾于现行画风和本次用户明确要求之上。
> 管线经验：`memory/enemy1012-art-replacement-pipeline.md`。工程侧机制、prefab 与波次见 **§5**；**其中 §5.0 是 2026-11-07 的落地实测记录（功能已实现）**，未落地的部分不代表已经实现。

---

## 1. 角色特征（设定 + 技能机制）

### 1.1 这是什么单位

| 项 | 内容 |
|---|---|
| id | `enemyId = 1013`；形象暂称「曹军旗手」（执旗传令者），正式显示名可另定 |
| 一句话 | **不攻击、给在场敌人叠加攻击力、死后给在场敌人永久减攻的敌方指挥单位** |
| 玩法定位 | 玩家第一个"**必须优先解决**"的目标（场上其它敌人都是"打哪个都行"） |
| 体量 | 一个**普通单位**：占 1 格 / 150 血 / 经验 150；**不是 BOSS**、不进 QTE、不做精英 |
| 所属阵营 | **曹操麾下的曹军追兵**；玩家是刘备军 / 张飞部的张飞。1011 和 1012 在本任务中是画风、题材及尺寸参考，旧版「张飞军」归属说法无效 |

### 1.2 技能机制（美术要能画出来的部分）

| 动作 | 触发 | 效果 | 美术要表达的事 |
|---|---|---|---|
| **常态** | — | **不攻击、不造成任何伤害**；正常行军 / 补齐 / 受击 / 被挑飞 / 被眩晕，与普通敌人无异 | ① 他**不是挥剑的兵**；② 常态里能看出"他在指挥" |
| **施放（号令 · 加攻）** | 周期 **4.0s**（首次有 **2.5s** 前置延迟）；施法前摇 **0.8s** | **在场**所有敌人攻击力 **+10%**，可叠加，**上限 5 层**；本波内不过期 | ③ **号令前摇要能预判**（0.8s，玩家要能看见"要来了"并决定打不打） |
| **死亡（减攻）** | 死亡瞬间 | **在场**所有敌人攻击力 **−25%**（一次性，不叠加）；本波内不过期 | ④ 死亡要有"**号令中断 / 败退**"的读感（和普通杂兵倒地不同） |

**净效果曲线**（旧版加法示意，玩家要能读懂早处理的收益）：

> 待机制设计确认：下表将加攻与减攻直接相加，和 §5.6 写的乘法公式不一致。例如 5 层时表内为 +25%，按 §5.6 的 `(1+5×10%)×(1−25%)` 则为 +12.5%。本次只更新美术生图方案，不擅自决定最终计算规则。

| 已号令次数 k | 0 | 1 | 2 | 3 | 4 | 5 |
|---|---|---|---|---|---|---|
| 全场累计加攻 | 0% | +10% | +20% | +30% | +40% | +50% |
| 死亡减攻 | −25% | −25% | −25% | −25% | −25% | −25% |
| **净效果** | **−25%** | **−15%** | **−5%** | **+5%** | **+15%** | **+25%** |

> **一句话教学口径**（画面要能帮玩家读懂这句）：*它每号令一次，全场敌人永久变强 10%；它一死，全场敌人永久变弱 25%。所以最好在它号令两次以内点掉它。*

### 1.3 "在场"的确切口径（决定号令覆盖谁）

| 计入 | 不计入 |
|---|---|
| 已生成进场的全部敌人 | BOSS |
| 正在入场行进的（已生成、已注册） | 它自己 |
| | 尚未生成的后续敌人 |

> 引擎侧事实：**一波敌人是一次性全部生成**的（只是错峰走进场）→ 所以第一次号令就覆盖本波全员；
> 死亡减攻只覆盖**死亡瞬间还活着**的那些。

### 1.4 号令会被打断（这条直接影响动作设计）

- 1013 **没有霸体** → 它的 0.8s 号令前摇**会被玩家任何直接伤害打断**（普通攻击 / 主动技能 / 大招）。
  灼烧、疾病、击退、挑飞、地刺等**不打断**。
- 打断后：本次号令作废（不加攻、层数不涨）、回到 Idle、**下一次重新计满 4.0s**。
- → **对美术的要求**：号令前摇必须是**一段连续、可被打断的动作**，玩家一眼能看出"这段能打断"；
  不能是瞬间切换的特效，也不能做成"一旦开始就必定完成"的仪式感动作。

### 1.5 它在战场上的行为约束

- 正常行军，**会补齐**（会随前排被打空一排排前移）→ 最终能进到前排被平 A 命中 → **不能画成"站着不动"**。
- 它活着 → 那一排永远算"没清空" → 后排不前移 → **它是一个必须被处理的硬门**（玩家不会无视它）。
- **不攻击** ⇒ 工程侧 `attackSequence` 清空、`attackDamage = 0` ⇒ **不需要 attack 动作**（省一支视频）。

---

## 2. 当前游戏画风与 1013 身份图生图方案

### 2.1 画风与参考职责

**本次用户明确口径：高清像素三国题材，1011 的比例正确，1012 更符合题材设定。** 因此 1013 采用 **1011 的体型 / 比例 + 1012 的汉末三国军人题材信息**，重新设计成曹军旗手；不是把 1012 换色，也不是复制 1011 的全身重甲。

| 项 | 要求 |
|---|---|
| 风格 | 按 `design/art-style-guide.md` 的高清像素化三国动作游戏风格，像素色簇与大体块表现厚重结构、金属和布革材质；轮廓干净，细节为缩小后的可读性服务。 |
| 比例 | 以 **1011** 正面 Idle 为准：约 2.5–3 头身、大头宽肩、躯干紧凑、短腿厚手脚、站姿稳；Q 化的是比例，气质仍严肃凶悍，不软萌。 |
| 题材 | 从 **1012 v11** 参考可见面容、胡须、交领布衣、皮带护腕、绑腿等中国古代军人语言，不复制其身材、宽檐盔、红头巾、剑或原样配色。 |
| 明暗材质 | 深色干净轮廓、清楚的受光面与暗部；冷铁、暖棕皮革、布料和木杆有厚度；**布旗有重量、有垂坠感，不是薄纸片**。**不以固定像素描边宽度或最多两阶阴影代替视觉验收**。 |
| 朝向 | 游戏敌人**正面朝玩家**：头、肩、躯干和双脚均正面；画面左 = 角色右，画面右 = 角色左。 |
| 禁止 | 欧美骑士 / 西幻法师、仙侠、现代军服、照片写实、3D 塑料、萌系卡通、侧身或 3/4 视角、低分辨率简笔像素、模糊、无意义噪点、背景场景、文字、UI、伪透明棋盘格。 |
| 动画资产 | 后续每帧独立单角色 PNG、真实透明 Alpha、装备完整不触边；同动作内画布统一、位置与比例稳定。身份图与正式透明动画帧不是同一交付阶段。 |

### 2.2 参考图与使用边界

| 文件 | 本次明确职责 |
|---|---|
| `Enemy1013ArtSource/02_IdentityImages/base_1011_idle1.png` | **比例和形体基准**：头身、肩宽、短腿、手脚和厚重感；不复制全覆面重甲与剑。 |
| `Enemy1013ArtSource/02_IdentityImages/base_1012_idle1.png` | 1012 v11 刀兵参考，用于人工比较汉末题材语言；不是 v3 输入。双参考图是否引起比例偏差不能由 v2 证明。该图不是工作区 1012 山贼版。 |
| `Enemy1013ArtSource/02_IdentityImages/1013_readability_compare_1011_vs_1012.png` | 并排检查 1013 的体量与兵种轮廓；不推导既有角色的剧情阵营。 |
| `Enemy1013ArtSource/02_IdentityImages/base_1012_walk1.png` | 后续步态参考，**不是**本轮身份设计的姿势模板。 |
| `Enemy1013ArtSource/02_IdentityImages/1013_identity_1012base_greenscreen_1024.png` | 这是 **1012 基底示例，不是 1013 最终身份图**；仅参考旧绿幕构图与规格，不能直接当新角色成品或定稿后视频身份图。 |

### 2.3 已确认的角色造型

**一句话**：曹操麾下随队推进的基层旗手；一名短壮、严厉的汉末军人，用一面长杆方旌旗发令号令曹军，而非独立作战的刀兵、法师或华丽大将。

| 部位 | 设计 |
|---|---|
| 脸与头盔 | 成熟、凶悍而克制的方脸，粗眉、短须或整齐络腮胡；**不戴盔**——束起的头发用一条深色布头巾加一条皮质护额勒紧，眉眼鼻口与胡须清楚可见。区别于 1011 的黑漆兜鍪与 1012 的宽檐斗笠；无高冠、大红缨或华丽将盔。 |
| 衣甲 | 暗青灰 / 灰米色交领布袍，外披深蓝灰短身轻甲**片**（只保留肩胸几块小甲片，不是全身重铠）；深棕皮革腰带、厚护腕，保留布与皮革结构。 |
| 下身 | 暗棕裤、灰白绑腿和厚底战靴；大头宽肩、粗壮手脚。 |
| 色彩 | 冷铁灰、蓝黑为主体，布袍为灰米 / 暗青灰，暖棕皮革；**暗赭红 / 朱褐的旗面是唯一的大面积彩色**，少量旧铜与银灰高光。暗红是本角色的视觉方案，不把某种颜色当成历史上曹军的绝对军服定制。 |
| 职业道具 | **长杆方旌旗**：深色木杆，杆顶挂一面**方形暗赭红布旗**，旗面**无字无图案**；旗布自杆顶顺风向**身后飘展**（不横向展开，见下方体量约束）。双手握杆，杆竖直高举过顶，旗在身后偏侧。旗、杆、双手、双脚完整可见，不遮脸。 |
| 姿态 | 正面、双脚分开站稳，双手握高举旗，身体微微前倾；像正在观察并组织军阵。**不是挥旗发令的瞬间**（那是 Cast 前摇），不是攻击动作。 |
| 明确不做 | **不做腰鼓、鼓槌、小三角令旗**（鼓手需求见 §2.7）；不拿刀、剑、枪或盾；不用巨大帅旗；不做重甲全覆面、夸张饰品或魔法法器。 |

**体量约束（2026-11-08 定）**：整体高度 ≈ 普通兵 **1.25×**（量到旗顶），**身宽与普通兵相当**。原因：五列阵型的列距只有 **2.6 世界单位**（`Assets/RouteData/Tutorial/Formation_Tutorial.asset` 的 `manualRowHalfWidths[0]=5.2` → 五列 X = −5.2 / −2.6 / 0 / 2.6 / 5.2），而普通兵屏高 5.76 世界单位、**身宽已接近列距** → 横向加宽会压到邻列，**"更大"只能走纵向**（旗顶更高、旗更大），旗布必须向后飘而不是向侧展开。

**画面可读性目标**：先看出一个 1011 比例的中国古代军人，再看出高举的长杆方旗；与 1011 重甲兵和 1012 刀兵并排时，一眼可区分兵种（无剑、有旗）。曹军归属来自长坂坡设定，不能保证仅靠无字服饰和颜色让玩家准确判断阵营。

### 2.4 历史方案：正面常态身份图（保留记录，不作为本轮执行稿）

> 本节是较早的单参考旗手提示词草案，v2 已失败；最新以 **§2.12** 为准。

**当时计划只做一张身份图**，不生成动作视频、精灵序列或 Unity 资源；生成结果需用户先验收。推荐使用 1:1 构图、目标 1024×1024（模型不支持时先取可用的方形原图并保留原图）。整身单人、固定正面、全装备入画，原图四周尽量留足约 15% 安全边距，不要贴边。

**历史参考图建议**：只给 `base_1011_idle1.png` 一张作为比例基准，题材用文字描述；此法与文字锚点都尚未验证能保证比例。双图融合导致比例偏差或带入剑、斗笠的因果未被本次结果证实，见 §2.10。

#### 中文正向提示词

```text
为三国题材动作游戏设计一张敌方角色的正面常态身份图：曹操麾下的基层旗手，随曹军追兵步行推进，用一面长杆方形军旗发令号令，不是武将、法师或近战攻击兵，也不带鼓。

高清像素化三国游戏角色，以参考图1011的头身比例和体量为基准：**全身约三个头高——头（含发髻）的高度约占全身的三分之一，肩宽约等于两个头宽**，大头、宽肩、短腿、厚实躯干、粗壮手脚，稳定有力量。只借鉴1011的比例，不复制全身重甲或武器。穿汉末中国军人的装束：可见人脸、交领布衣、皮革肩带、护腕和绑腿，不戴斗笠、不系红头巾、不佩剑。

成熟严厉的方脸，粗眉、短须或整齐络腮胡，神态凶悍克制；不戴头盔，束起的头发用一条深色布头巾和一条皮质护额勒紧，眉眼鼻口与胡须清楚可见。身穿暗青灰或灰米色交领布袍，外披深蓝灰短身轻甲片（只有肩胸几块小甲片，不是全身重铠），深棕皮带、厚护腕，暗棕裤、灰白绑腿、厚底战靴。金属有清晰银灰锐利高光，布料、皮革和木材有体积，少量旧铜点缀，整体冷铁灰与暖棕对比。

双手握一根深色长木旗杆，杆竖直高举、杆顶在人物头顶之上，**旗顶只比人物头顶高约四分之一身高**；杆顶是**平的素木杆顶，不带任何金属矛尖、枪头、矛头或装饰**。杆顶挂一面方形暗赭红布旗，旗面平整，完全空白，无文字、无图案、无徽记；旗布自杆顶向一侧展开并顺风飘，读起来是一片竖直的方形旗面，而不是横向展开的横幅，宽度不超过角色肩宽的 1.5 倍。旗面边长约为人物身高的四成。旗、杆、双手、双脚完整清楚，旗布不遮脸、不盖住整个躯干和双腿。无刀、剑、枪、盾，无鼓、无鼓槌、无小三角令旗。

一人、全身、完全正面朝向摄像机，头、肩、躯干与双脚都正对画面，双脚分开站稳，双手握高举旗，身体微微前倾，正在观察军阵，站立待命不迈步；不是挥旗发令的瞬间，不是攻击动作。人物是主体，形象是普通单位而非Boss。

1:1方形构图，单人居中，头顶、旗顶、旗面、双手和双脚完整入画，四周预留安全边距。纯均匀绿幕背景（目标RGB 0,159,62），无地面、阴影、场景或其他角色。游戏Sprite身份图，清楚深色轮廓、像素色簇、清晰形体和材质，严肃凶悍但不软萌；不加光效、声波、字、标志、UI或水印。
```

#### English positive prompt

```text
Design one front-facing idle identity image for an enemy in a Three Kingdoms action game: a junior Cao Cao army standard-bearer advancing on foot with the pursuing soldiers, signaling them with a long pole and a square cloth military flag. He is not a general, mage, or melee attacker, and he carries no drum.

High-definition pixel-art Three Kingdoms game character. Match reference 1011 for mass and build: **about three heads tall overall — the head including the topknot is roughly one third of his total height, and his shoulders are about two head-widths wide**; large head, broad shoulders, short legs, compact heavy torso, substantial hands and feet, a strong stable stance. Borrow 1011's proportions only, not its full-body heavy armor or sword. Dress him as a Han-era Chinese soldier with a visible human face, cross-collar cloth tunic, leather straps, bracers, and leg wraps; no wide-brim hat, no red headscarf, no sword.

Mature square face with thick eyebrows and short beard or neatly trimmed full beard, stern and battle-hardened rather than comical. He wears no helmet: his hair is tied up and bound with a dark cloth headband and a leather brow band, so eyes, nose, mouth, and beard remain visible. Wear a muted blue-gray or gray-beige cross-collar tunic with only a few small dark blue-gray light armor plates over the shoulders and chest (not a full suit of heavy armor), dark brown leather belt, sturdy bracers, dark brown trousers, gray-white leg wraps, and heavy boots. Clear silver-gray metal highlights, convincing cloth folds and leather volume, restrained aged bronze accents; cool iron against warm brown.

Both hands grip a tall dark wooden flagpole held vertically, with the pole top **only about one quarter of his height above the top of his head**. The very top of the pole is **plain cut wood — no metal spearhead, no spike, no finial, no ornament of any kind**. A square dark-vermilion cloth flag hangs from the top of the pole; the cloth surface is completely blank, with no text, no emblem, no symbols. The cloth streams to one side in the wind, so it reads as one upright square panel of cloth, never as a wide horizontal banner, and its width stays within about one and a half times his shoulder width. The flag's side length is about two fifths of the character's height. The flag, the pole, both hands and both feet are fully visible; the cloth does not cover the face, the torso, or the legs. No sword, blade, spear, shield; no drum, no drumstick, no small triangular pennant.

Exactly one full-body character, directly facing the camera: head, shoulders, torso, and both feet front-on. Feet planted apart, both hands gripping the raised pole, body slightly inclined forward, watching and organizing the formation. Standing still, not walking, not mid-signal, not attacking. The character is the main subject and reads as an ordinary unit rather than a boss.

Square 1:1 composition, centered, entire head, pole tip, flag cloth, hands and feet visible with generous safe margins. Flat uniform chroma-green background (target RGB 0,159,62), no ground, cast shadow, scenery or other characters. Game sprite identity art with clean dark contours, deliberate pixel clusters, strong readable forms and materials; serious and fierce, not cute. No magical effects, sound waves, text, emblems, UI, or watermark.
```

#### 负面提示词

```text
side view, three-quarter view, rear view, cropped body, extra people, extra limbs, extra fingers, covered face, missing hands, missing feet, sword, knife, spear, shield, drum, war drum, drumstick, round drum, small triangular pennant, metal spearhead on the pole tip, spike on the pole tip, ornate finial, round flag, horizontal banner stretching sideways, flag wider than one and a half times his shoulders, flag cloth covering the body, huge general banner, ornate general, mage, wizard hat, staff with magic, fantasy runes, spell circle, glow, sound wave, attacking, mid-swing signaling pose, dynamic action pose, cute, soft toy, anime moe, elongated realistic proportions, more than four heads tall, low-resolution simplistic pixel art, photorealism, 3D render, plastic, watercolor, oil painting, blur, checkerboard, textured background, scenery, ground, shadow, text, logo, emblem, UI, watermark
```

#### 身份图验收与后续加工

1. **先看角色，不看技术数字**：与 `base_1011_idle1.png` 对齐头身、肩宽和厚重比例；与 `base_1012_idle1.png` 对照，确认可见脸、汉末布衣 / 甲胄语言，而非西方骑士或仙侠人物。
2. **缩小与并排**：身份图缩到约 1/4，并与 1011、1012 并排；能分清头、身、四肢、**高举的长杆与方形旗面**，不误读为拿刀 / 持盾，也不显得像 Boss。曹军身份须结合设定确认，不以衣色单独证明。
3. **单图完整性**：正面、单人、**双手握杆**、杆从手到顶连续不断、旗面为方形且无字无图、旗不遮脸 / 双腿，旗顶与旗布不出框；无额外武器、文字或特效。原图绿幕纯净且与主体分离；生成模型未必精确遵循 RGB，须实际检查。
4. **先验收，后归档加工**：保留生成原图，确认角色后再单独抠图 / 合成纯绿幕。拟归档 `Enemy1013ArtSource/02_IdentityImages/1013_identity_final_1024.png`；可沿用旧管线的 1024 方图、`RGB(0,159,62)`、主体高 696 / 可见脚底 y=895 / 中心 x=474.5 **作为初始对齐目标而非保证值**。旗杆与旗面外轮廓会改变边距或中心，须按完整装备、脚位和画面对照实测调整；归档图脚底留白不必强套原图的 15% 建议。
5. **只在身份图通过后**再评估 Idle。旧动作母本持剑，旗手双手持杆；外挂旗只是 §2.8 的候选方案，须先验证角色换装、去剑、双手握杆和遮挡，不能假定母本帧可直接复用。Cast 是新动作，无母本。

### 2.5 号令与死亡的后续动作方向（不属于本轮生图）

- **Idle / Walk**：双手握高举旗待命，旗布随步轻晃；正常随队行军，旗始终清楚，不画成不能移动的场景旗台。
- **Cast 前摇 0.8s（2026-11-08 用户澄清后定稿）**：**挥旗**——旗手原地双手握杆，**在身前反复横扫**：肩带臂、手腕领杆，杆顶在头顶前方**划弧线来回甩**，一下接一下不停顿；旗布有惯性、**滞后甩在杆后**，每甩到弧线末端布面被拉直「炸开」成一整片再回落；身体随甩动拧转、重心在两脚间来回压；**最后一甩到达弧线末端的那一瞬张口呐喊**（顶点表情）。**注意：「挥」是一次性的向上「举」完全不同的动作**——举是到某个姿势停住，挥是反复划弧、停不下来。
- **Dead**：旗手失力倒地，**长杆与旗面倒地**——旗面低垂摊在地上、旗杆斜倒，不再飘动，表现号令中断；不做魔法爆炸。旗与杆的保留 / 脱离方式须随动画和后续接线单独确认。

### 2.6 后续 Unity 技术规格（旧管线参考，待新形象实测）

| 项 | 参考值与适用边界 |
|---|---|
| 精灵画布 | 候选 **1108×1108 RGBA**；先保证新角色视觉比例、脚底与旗杆旗面完整，再确定画布和落位。 |
| 导入 | 沿用 1012 候选设置：spriteMode 1（Rect）、PPU 16、Point、无压缩、alphaIsTransparency=1、maxTextureSize 2048；最终以 1013 实测为准。 |
| pivot / 落位 | 1012 idle pivot 为 `0.6539, 0.5487`，attack/dead 为 `0.5, 0.5`；旧 idle 落位 k=0.92366、脚底 897、中心 521。**仅作参考，不得假定新角色直接套用**。 |
| 屏幕尺寸 | **整体高度 ≈ 普通兵 1.25×（量到旗顶），身宽与普通兵相当**；交接记录中 1012 屏幕高约 5.76 世界单位，正式尺寸 / 偏移须实测。 |
| Animator 状态契约 | `Idle / Walk / HitFlash / HitLeft / HitRight / Dead / Launched_Rise / Launched_Fall / Launched_Getup`，另加 `Cast`；后续接线。 |

---

### 2.7 鼓手需求后置（2026-11-08 定）

- 1013 = **旗手，不含鼓**。鼓手（鼓吹鼓手）作为独立需求后置，另立 enemyId（例如 1014），与 1013 完全解耦，不共用 prefab / 动画 / 机制。
- 后置理由：用户已否决鼓＋旗的造型。鼓是贴身道具，动作中需要考虑与身体的贴合；旗杆 / 旗布可考虑外挂，但旧母本的剑、双手和运动姿态均须验证，**不能据此宣称动作可整套复用**（§2.8）。
- 若将来做鼓手：**不要**回到"腰挂小圆鼓 + 小三角令旗"（该组合是戏曲 / 评书符号，且制度上旗鼓分属两人）；方向是**军中大鼓 + 鼓架**，并重新评估体量与列距的冲突。

### 2.8 后续工程复用候选（尚未验证）

旗杆、旗布可以考虑作为独立 sprite，分别连接人物手部与杆顶；这只是候选，并非已经验证的部署方案。1012 母本持剑，剑与单手姿态绘在角色帧中；旗手是新造型、双手持杆，外挂旗并不会自动去剑、完成换装或修正握姿与遮挡。不同 Walk / 受击 / 击飞 / 死亡动作中的杆与双手贴合也各自需要验证。

**2026-11-08 用户定案（美术线）**：旗杆与旗面**与人物整张一起动**，**不做外挂 sprite**；且**不使用视频替换流**——每个动作按已验收成功方案的提示词结构改写后**新生成**（模板见 `Tools/visual_brief/prompts/1013_cast_v9|v10|wave_v2`），1012 母本只作动作语义参考。详见 `plan/enemy1013-art-handoff-20261108.md`。

**顺序**：先获得用户验收的人物身份图，再用持杆 Idle 做小样，检查角色比例、去剑、双手位置、前后遮挡和旗的连续性；只有样例可行后才评估其余动作复用、补帧及成本。Cast 无母本，需单独设计；死亡时旗杆与旗面倒地的表现也需单独验证。不要承诺「其余动作零次生成」或直接复用现有帧。

### 2.9 第一轮生成实测（2026-11-08）：v1 作废，形象方案移交隔壁

**结论先写**：旗手 v1 身份图**比例不合格（用户判定「比例完全就不正确」），已作废**。两次请求均有参考图但未得到目标比例，无法仅凭结果证明具体原因；文字锚点、单参考图和局部修图也都不能保证成功。后续以 §2.12 的 v3 准备方案为准。

#### 提交记录（可复现）

| 项 | 值 |
|---|---|
| 脚本 | `Tools/visual_brief/submit_1013_identity.py --version v1 --approve`（`--dry-run` 先看请求） |
| 提示词 | `Tools/visual_brief/prompts/1013_identity_v1.txt`（1249 字符，sha256 前缀 `e1d982f1`） |
| 参考图 | `Tools/visual_brief/prompts/refs_1013_identity_v1.json`（**双图**：1011 比例 + 1012 题材） |
| 接口 | `POST https://api.muskapis.com/v1/images/edits`（multipart，图片字段 `image[]` × 2） |
| 参数 | model `gpt-image-2.5-sunburst` / size 1024×1024 / quality `max` / input_fidelity `high` / output_format png / n=1 / **不传 background**（要绿幕不要透明） |
| 结果 | HTTP 200，**64.78s**，1,038,845 bytes，sha256 `9d75f809…574` |
| usage | prompt 313 / output 7024（image）/ total 7337 tokens；**接口未返回 cost 字段** |
| 请求留档 | `C:/Users/steam/Pictures/gptGen/enemy1013_identity/1013_identity_v1.{prompt.txt,request.json,response.json,status.json}` |

#### 产物与量测

| 文件 | 量测 | 状态 |
|---|---|---|
| `02_IdentityImages/1013_identity_v1_raw.png` | 1024²，主体 bbox 354×976（x 308–661 / y 4–979），绿幕中位 (2,158,69)、std<1 → 绿幕很干净 | **作废**（比例错），留作对照 |
| `02_IdentityImages/1013_identity_v1_greenscreen_total1250.png` | 1024²，主体 316×870，脚底 895，中心 473.5，绿 (0,159,62) | 加工件：总高 1.25× 口径，但角色身只有普通兵 **80%** |
| `02_IdentityImages/1013_identity_v1_greenscreen_body696.png` | 1024×1200，主体 394×1087，脚底 1160，中心 474.5 | 加工件：角色身高等于普通兵，整体 **1.56×** |
| `02_IdentityImages/1013_identity_v1_greenscreen_*.layout.json` | 量测明细 | 留档 |
| `00_Index/identity-images.csv` | 产物台账（含 sha256） | 入库 |

加工工具：`Tools/visual_brief/compose_identity_greenscreen.py`（抠图直接复用 `chroma_key.key_frame` 的 T=79 配方；参数 `--mode total|body`、`--body-top-y`、`--target-h`、`--canvas-h`、`--foot-y`）。

#### v1 实测偏差 —— 隔壁必须避开的坑

| # | 偏差 | 实测 | 根因 / 对策 |
|---|---|---|---|
| 1 | **人物比例错**（用户判定「比例完全就不正确」） | 人物头小、躯干 / 腿显长；历史记录目测约 **4 头身**，但头部边界和 1011 盔形不同，数字不作硬门槛 | 两张参考图与文字均未保证结果，具体原因未确定。返回的 `image_tokens=0` 不能证明参考图是否被接收；新方案以 1011 人物本体等高对照作为首要验收。 |
| 2 | 旗杆顶长出**金属矛尖** | 杆顶 x≈407–430 处有枪头状金属件 | 模型自发补「旗杆就该有尖」→ 必须明写「杆顶是**平的素木杆顶**，不带任何金属矛尖、枪头、矛头或装饰」 |
| 3 | **体量超标** | 旗顶高出头顶 **0.56 倍身高** → 整体 **1.56×** 普通兵（计划 1.25×） | 已拍板 1.25×（缩短旗杆）→ 锚点：「旗顶只比人物头顶高约**四分之一**身高」 |
| 4 | 旗面从杆顶**垂挂到右侧**、盖到头部右侧空间 | — | 建议改成「旗面挂在杆顶、整体偏向一侧展开，宽度不超过角色肩宽的 1.5 倍」（列距只有 2.6 世界单位）；同时避开 §2.4 负面词里「横向展开的横幅」这条禁止项的语义冲突 |
| 5 | 装备偏「将」（肩甲 + 胸甲全套） | — | 用户未要求改；若要更基层，写「只有肩胸几块小甲片」 |

#### 已拍板的口径（不要再改）

- 形象：**曹军旗手**——单人、无鼓、无鼓槌、无小三角令旗（鼓手需求后置，§2.7）
- 体量：**整体 1.25× 为目标**（缩短旗杆，角色身高与普通兵一致）；最终画布仍须实测，不能据此断言 1108×1108 一定足够
- 参考图：v2 曾尝试只给 1011 一张但未达目标；下一轮重新参考 1012 v6 的双图职责分离（§2.12）
- 叙事：**号令**；死亡 = 旗手倒地 + 旗杆旗面倒地 = 号令中断
- 工程：旗杆 / 旗布外挂与母本复用属于待验证候选（§2.8）

#### 下一轮入口

历史的「只需改文字锚点」判断不再作为保证。§2.10 是已失败的 v2 方案；v2 复盘见 §2.11，**最新 v3 准备方案见 §2.12**，比例未通过不得进入后续处理。

### 2.10 历史方案：先锁人物，再添加方旗（v2 阶段 A 已失败）

> 阶段 A 已生成 `Enemy1013ArtSource/02_IdentityImages/1013_identity_v2_raw.png`；用户已明确判定比例完全不正确，**作废，禁止进入阶段 B 或作为角色基准**。实际请求与失败分析见 §2.11。
> 历史提交内容：`Tools/visual_brief/prompts/1013_identity_v2.txt` + `Tools/visual_brief/prompts/refs_1013_identity_v2.json`；脚本 `Tools/visual_brief/submit_1013_identity.py --version v2`。
> 阶段 B 历史提示词 `Tools/visual_brief/prompts/1013_identity_v2_stageB_flag.txt` **不得对未通过的 v2 运行**。本轮不再提交生成。

#### 不变的设计结论

- 1013 是**曹军持长杆方旌旗的旗手**；鼓＋旗、小三角旗、鼓槌全部排除。
- 1011 负责唯一的**人物本体比例、体块和站姿基准**；1012 只做人工作品对照，不上传给本次生成。
- 保留汉末三国军人的可见人脸、交领布衣、皮革、护腕、绑腿等题材语言，但不复制 1012 的斗笠、红头巾、剑或身材。
- 旗顶总高暂以人物本体高度的约 **1.25×** 为目标；旗杆 / 旗面不能反过来拉长、压瘦或缩小人物本体。

#### 两阶段执行

1. **A：锁定人物本体和持杆姿态**。只上传 `base_1011_idle1.png`，以人物外轮廓和体块为结构模板改成曹军布甲旗手；只画素木杆，双手握杆，**先不画旗布**。人物需体现汉末题材，同时保留 1011 的大头、宽肩、紧凑躯干、短腿、厚手脚和扎实站姿。此阶段的单参考 / 编辑方法仍是未验证假设，不保证比例一定通过。
2. **A 验收后才进入 B**。以验收的 A 成图为唯一输入，请求只在原有木杆顶端添加暗赭红方旗；目标是保持人体、双手、杆的位置和尺寸。现有脚本没有指定局部 mask，不能保证其他位置不变。**B 必须重新对照 A**，若模型重画、拉长、缩小或移动身体则不合格。单张 A 的试验无旗布，绝不作为最终旗手交付。
3. **B 获用户认可后才归档与评估动画**。额外一次图像编辑与人物漂移风险是分阶段的代价；若 B 不能保持 A，应停止并另议局部编辑或手工合成，不以 A 结果冒充完成。

#### A 的第一道硬门槛：人物本体比例

- **先对齐后比较**：人工标记 1011 从**头盔顶部（不含红缨）到双脚脚底**、A 从**头部顶部（发髻单列检查）到双脚脚底**的本体范围；旗杆、旗布、飘带、剑不计入。只做**等比缩放**，将本体可见高度与脚底对齐，生成 1011 / A 并排对照；遮挡区域标为无法判断，不补画也不变形。头盔与真实人头不等价，数字只能辅助，不能因帽子 / 发髻大而判通过。
- **逐项目视**：头相对躯干是否有足够体量；肩宽和头肩距离；胸腹 / 腰胯是否紧凑；腰线、胯、膝与脚底的相对高度；腿长粗细；手、前臂、脚的视觉分量；整体是否像 1011 一样短壮稳重。不要只检查“三头身”。
- **再缩小复核**：并排图同时看原尺寸和约 1/4 尺寸；先交用户判断比例和三国题材。如果比例不合格，保留原图 / 请求记录并标记作废；不要用整体缩放、拉伸、加高画布或扩大头饰伪装成合格。
- **姿态 / 题材**：一个正面汉末军人，双手握同一根连续平木杆，没旗布、鼓、鼓槌、短旗、刀剑枪盾；面容、交领、布革语言与 1012 的题材气质可对照，但比例始终以 1011 为准。杆和双手不遮脸，四肢 / 木杆不出框。A 是结构试验，不以“无旗”为失败。

#### B 的验收：旗帜与人物同时成立

- 与 A **原尺寸叠图 / 闪切**检查头、肩、腰、膝、脚和双手位置；若身体比例或姿态发生明显变化，退回 B，不为旗牺牲 A。
- 一面素面无字无徽记的暗赭红方旗，挂于既有木杆顶端；旗帜不能遮脸、双手或主要身体部位，也不能压过人物主体。旗顶约为人物本体的 **1.25×**，人物本体仍与普通兵同高；宽度结合实际画面和列距检查，不能只靠压小人物达标。
- A 和 B 均由用户确认后才归档。旗杆外挂、动作母本和最终 Canvas 设置另行验证，不能根据静态图通过就当成已实现。

#### 本轮产物

- 阶段 A 提示词 / 参考：`Tools/visual_brief/prompts/1013_identity_v2.txt`、`Tools/visual_brief/prompts/refs_1013_identity_v2.json`
- 阶段 B 提示词：`Tools/visual_brief/prompts/1013_identity_v2_stageB_flag.txt`；输入图和参考清单待 A 生成且验收后确定。
- 阶段 A 已生成但否决：`Enemy1013ArtSource/02_IdentityImages/1013_identity_v2_raw.png`
- 本次检查未发现阶段 B 成图；不要将失败图加工或接入 Unity。

### 2.11 v2 比例失败复盘与下一轮方向

**用户反馈**：已收到 v2，但「比例完全不正确」。此评价是验收结论，v2 阶段 A 作废，**不要生成旗面 / 动画 / 接入**。

#### 生成记录与观测

- 图：`Enemy1013ArtSource/02_IdentityImages/1013_identity_v2_raw.png`；1024×1024 RGB，981642 bytes，SHA-256 `3f73bb31246767e83c97ebf8903441774a04b04f53fe4f0bc666d650493d0679`。
- 实际请求：`C:/Users/steam/Pictures/gptGen/enemy1013_identity/1013_identity_v2.request.json`；单张参考 `base_1011_idle1.png`，`gpt-image-2.5-sunburst` / 1024×1024 / quality=max / input_fidelity=high；HTTP 200。提示词为 `Tools/visual_brief/prompts/1013_identity_v2.txt`，SHA-256 前缀 `75db31ac74c4b3e4`。
- **等高目视对照（仅临时诊断）**：`Library/Locus/tmp/1013_ratio_v2/1011_v2_aligned.png`。1011 人工取 y=365–897（盔顶，不含红缨→靴底），v2 取 y=345–944（头巾顶，不含发髻→靴底）；只做等比缩放、脚底对齐。该路径在 Library 下，可能被清理；两种头饰不同、持杆改变肩臂姿态，标记仅供视觉分析，不是假定精确人体骨点或可直接用于自动判定。
- **图上所见**：v2 的头和肩部视觉体量更小、腰线更低、裤腿及站姿更窄，整体看起来比 1011 细长。不能只把原因归给旗杆或画布；不能只凭这张图和盔形判定精确头身比。
- 回执标示 `input_tokens_details.image_tokens=0`，但脚本构造的是 `image[]` multipart、请求清单记录了 1011；**单靠 token 字段不能确定图片是否被服务端接收或应用**。历史同类接口也曾报告非零 image token，路由 / 统计差异和本次失败之间是否有关尚未验证。

#### 原方案哪里需要修正

v2 虽然要求“保留 1011 的人物外轮廓”，仍在一次生成中要求模型同时移除头盔和重甲、重做裸脸与衣服、把单手持剑改成双手持杆，还要生成不同的构图和绿幕。实际输出说明“单张图 + high fidelity + 文字保持”**并没有成功约束住** 1011 的人体视觉结构；不能再把追加“不许改变比例”的文字或拆出旗面说成已证实的修复。

#### 当时建议（历史讨论；新 v3 已转为 §2.12 的双图嫁接方案）

1. **先做输入链核对**：在不提交收费请求的前提下核对本轮脚本真实 multipart 图像字段、图片尺寸 / Alpha 和本次调用留档；若依然无法确认服务器实际使用了参考图，要把它作为独立不确定项。不要把 `image_tokens=0` 当成已证实的原因，也不要为了试探反复付费。
2. **以 1011 原身为几何主图，逐部位修改，而不是一次重新画人**。优先考虑先只做脸 / 头部、再改服装 / 局部甲片、最后处理持杆姿态。每一步保留上一步结果，头、肩、腰、胯、膝、脚底等以 1011 等高对照，防止多次编辑累积漂移。每一步的可行性、是否保留用户认可的形象和实际成本都须单独评估；这是候选路径，不保证成功。
3. **先验证真实画面，不靠数字或工具回执宣布通过**。等高对齐时旗、红缨和武器不参与人体高度，服装、盔形和手臂姿态会改变外轮廓，必要时标记关键区域人工复核。若修改某一步引起明显比例变化，应保留证据并停止，而不是继续叠代或直接拉伸修图。
4. **比例通过后再处理持旗**。先确保人物确实属于 1011 的体型，再处理双手持杆及旗面；不能为了方便挂旗而牺牲身体比例。现有脚本不提供局部 mask，任何“只改某部位”目前只是请求意图，具体效果需实测。

> 本节只记 v2 的观察与当时候选方向；最新研究及准备的 v3 请求见 §2.12。

### 2.12 1011→1012 的真实比例嫁接经验与 1013 v3 新方案

> **状态：v3 阶段 A 已于 2026-11-08 生成（模型 `gpt-image-2.6-sunburst`），**结果不采用**——用户判定「加多个阶段A反而变怪了」。** 本节保留为历史研究记录：双图嫁接思路在本轮被实测证伪。实际提交记录见 **§2.13**，当前有效结论见 **§2.14**。

#### A. 1012 并不是从 1011 一次稳定生成的

依据本机归档的每版原图和 `.request.json` / `.status.json`，1012 v11 刀兵的主要分支如下：

| 阶段 | 实际图像输入与操作 | 结论与边界 |
|---|---|---|
| v1 | 1011 Idle 的**不透明 RGB 绿幕图**作为唯一输入，整体重画为汉末刀兵 | 已保留单手持刀 / 正面站姿语义，但 v1 不是最终造型。 |
| v2 → v5 | 以 v1 生 v2，再以 v2 生 v5，逐步改布衣、胸甲、脸、头盔和护腕；**v3/v4 是另一条候选分支**，不是 v2→v5 的中间环节 | v5 的汉末刀兵身份已经可见，但从原图目视比 1011 偏细长；不能说每一版比例从一开始就正确。 |
| **v6：关键嫁接** | 图 1 = 1011 绿幕图，**只管头身 / 宽肩短腿 / 像素轮廓**；图 2 = v5，**只管衣装 / 刀 / 脸**。实际提示词要求 `Enlarge the head and helmet, widen the shoulders, and shorten the torso and legs until the body weight matches image 1` | 两张图职责分开，主动纠正图 2 的瘦长比例，而非仅写“保持不变”。目视 v6 更短壮；该素材仍有粗颗粒问题，不能以此声称整个链路一次验收。 |
| v7 | 单图输入 v6，保留比例 / 造型，集中消除人物内部方块网格 | 保留 v6 形体结构，修饰内部表现；不主张每次图生图能保证零漂移。 |
| v11 | 图 1 = N1 破盾素材，只提供材质绘制；图 2 = v7，只提供人物身份、服装、武器、姿态和比例 | 按素材包 `Enemy1012ArtSource/README.md`，v11 是 1012 刀兵身份图基线；视频动作再使用该身份图与对应动作母本制作。 |

**证据索引**：
- 本轮等高目视演变对照：`Library/Locus/tmp/1013_1012_fusion_research/1011_1012_v5_v6_v7_v11.png`；手工按盔顶 / 脚底对齐，1011 不含红缨；盔形 / 姿态不同，仅供目视溯源，非人体数值量测。Library 临时文件可能被清理。
- v6 实际请求：`C:/Users/steam/Pictures/gptGen/enemy1011_style_locked_dao_v6/enemy1011_style_locked_dao_v6.request.json`；原图 `C:/Users/steam/Pictures/gptGen/enemy1011_style_locked_dao_v6/enemy1011_style_locked_dao_v6.png`。
- v5 形象：`C:/Users/steam/Pictures/gptGen/enemy1011_style_locked_dao_v5/enemy1011_style_locked_dao_v5.png`；v7：`C:/Users/steam/Pictures/gptGen/enemy1011_style_locked_dao_v7/enemy1011_style_locked_dao_v7.png`；v11：`C:/Users/steam/Pictures/gptGen/enemy1011_style_locked_dao_v11/enemy1011_style_locked_dao_v11.png`。
- 成功身份图用于 1012 动作替换的记录：`Enemy1012ArtSource/README.md`、`Enemy1012ArtSource/00_Index/identity-images.csv`。
- 历史 `plan/enemy1012-asset-pull-20261026.md` 的样例给出 Sprite 剪影世界高度：walk 1011 / 1012 为 **3.238 / 3.244**、命中帧 **3.670 / 3.763**，idle 为 **3.602 / 3.166**。这些是含姿态与武器的剪影 / 世界尺寸，不是头身或身体比例的严格量测，不能据此证明逐部位完全一致；本轮也未重新运行 Unity。

**关键限制**：v6 的 1012 是单手持刀，1013 是双手持杆；不同姿态 / 道具会影响身体与画面构图。v6 给出有价值的先例，但不保证 1013 同样一次成功，也不能保证模型完全按图像职责分离。

#### B. v3 实际准备：复用 v6 的双图职责，不复用被否决的比例

| 序 | 输入图 | 职责 / 处理 |
|---|---|---|
| 图 1 | `Enemy1013ArtSource/02_IdentityImages/1011_green_1012_v6_reference.png` | **1011 比例与体量**；本轮从历史 v6 的图 1 复制，1108×1108 RGB，SHA-256 `3f3c17a2be763ce80a054da333ef3bbc0b9190dfeba19d8402e38e2e38b6da56`，与历史文件字节相同。不能继承面罩、重甲、红缨或剑。 |
| 图 2 | `Enemy1013ArtSource/02_IdentityImages/1013_identity_v2_raw.png` | **仅提供衣着 / 可见脸 / 双手持杆构思**。用户已经否决其比例，且没有将其脸或服装作为已验收身份；不得继承小头、窄肩、细长躯干腿或以它定比例。 |

图 1 的历史绿幕副本与 `base_1011_idle1.png` 目视相同：按 RGB(0,159,62) 对 alpha 进行合成后逐像素一致；**这只是确认图片内容和格式，不证明历史请求成功的原因**。历史 v6 回执 image token 为 1120，而 1013 v2 回执是 0；统计和路由可能不同，不能从字段单独推出是否真正读取图片。当前 1013 脚本本地构造重复的 `image[]` 字段，但服务器如何处理仍要以输出和回执实测。

- 本轮完整提示词：`Tools/visual_brief/prompts/1013_identity_v3.txt`（英语，按 v6 的两图分工组织；不硬套 v6 的“最多两阶色阶”旧画风要求）。
- 顺序与引用：`Tools/visual_brief/prompts/refs_1013_identity_v3.json`；图 1 为 1011、图 2 为 1013 v2，**不得颠倒或使用鼓＋旗图**。
- 请求准备：`python Tools/visual_brief/submit_1013_identity.py --version v3 --dry-run`；脚本使用同款 `gpt-image-2.6-sunburst`（2026-11-08 由 2.5 切换）/ 1024×1024 / quality=max / input_fidelity=high / PNG / n=1、两张 multipart `image[]`，不指定 mask，自动重试为 0。**dry-run 仅检查本地加载，不发请求、不证明服务端接收。**
- 下一次如果用户批准，仅提交 **1 次 v3**；原图计划存 `Enemy1013ArtSource/02_IdentityImages/1013_identity_v3_raw.png`，不得覆盖 v1 / v2。失败不自动重试，先展示并排图和记录。

#### C. 闸门与后续旗面

1. **首要比例验收**：只按人物本体（1011 盔顶不含红缨到脚底；候选头顶 / 发髻分别核对到脚底，不含杆）等高、脚底对齐，和 1011 正常及约 1/4 尺寸并排。核对头肩、胸腹、腰胯、膝脚线、腿长、手脚分量与站姿，不靠数字、bbox 或红缨 / 发髻撑大的头高判通过；遮挡处注明无法判断。
2. **造型验收**：再让用户判断曹军旗手的面容、服装、双手持杆；v2 的身份只是一张草图，不能默认已被认可。本次 A 不画旗布是有意设计，不能当成最终持旗图。
3. **经用户认可后才加旗**：`Tools/visual_brief/prompts/1013_identity_v3_stageB_flag.txt` 为后续模板；引用必须换成被验收的 v3 成图并建立新清单。现有脚本没有 mask，“只改旗”只是意图；原尺寸闪切检查身体、手和脚是否被重画，比例漂移即退回，不得牺牲人物。
4. 本次仅备方案、未生成 v3、未发起付费请求，也未制作动画或接入游戏。（→ 2026-11-08 已提交 v3 阶段 A，见 §2.13；仍未加旗、未做动画、未接入游戏。）

### 2.13 2026-11-08 实际提交记录（模型切换 2.6 + v3 阶段 A）

**模型切换（同日，用户指令）**：探针确认 `gpt-image-2.6-sunburst` 可用（`GET /v1/models` 当天图像模型只剩 2.6；真实文生图探针 HTTP 200 / 87.66s / 1024² PNG）→ 工作流默认模型由 `gpt-image-2.5-sunburst` 改为 `gpt-image-2.6-sunburst`，**其余参数不变，质量仍用 `max`**。仓库内 125 个写死模型名的 Python 脚本已统一；`skill/gpt-image-generation.md` 与 `memory/gpt-image-workflow-check-20261022.md` 已同步。

#### 本日提交的三次生成

| 轮次 | 目的 | 模型 | 请求 | 结果 | 产物 |
|---|---|---|---|---|---|
| A-2 | 重跑 v2 阶段 A（单图 1011） | `gpt-image-2.6-sunburst` | `/v1/images/edits`、单张 `image[]`、`quality=max`、`input_fidelity=high`、1024²、png、n=1、不传 `background` | HTTP 200，113.6s，**Color Type 6（RGBA，原生透明）**，881,482 bytes，sha256 `6a2d44b3…cc7` | `02_IdentityImages/1013_identity_v2_stageA_2_6.png`（留档；**不覆盖** v2 路径）；留档 `C:/Users/steam/Pictures/gptGen/enemy1013_identity/1013_identity_v2_2_6.*` |
| **v3 阶段 A（本轮主线）** | §2.12 双图嫁接比例研究 | `gpt-image-2.6-sunburst` | `/v1/images/edits`、**两张 `image[]`**（图1 `1011_green_1012_v6_reference.png`、图2 `1013_identity_v2_raw.png`）、其余参数同上 | HTTP 200，116.38s，1024²，**Color Type 2（RGB 绿幕）**，1,034,697 bytes，sha256 `543a2f86…ee2`，usage in 614 / out 1756 / total 2370 | `02_IdentityImages/1013_identity_v3_raw.png`；留档 `C:/Users/steam/Pictures/gptGen/enemy1013_identity/1013_identity_v3.*` |

- 提示词 `Tools/visual_brief/prompts/1013_identity_v3.txt`（2453 字符，sha256 `5603f8b1…`）；参考顺序 `refs_1013_identity_v3.json`（图1=1011 比例、图2=v2 造型构思），**未颠倒**。
- **v2 原路径已恢复**：`02_IdentityImages/1013_identity_v2_raw.png` 仍为 §2.11 记录的 2.5 产物（sha256 `3f73bb31…0679`，与文档一致）；A-2 另存为 `1013_identity_v2_stageA_2_6.png`，文档中引用 v2 路径的地方不受影响。
- 回执 `input_tokens_details.image_tokens=0`（与 v2 相同）；**不能据此断定参考图未被接收**，仍以输出目视与并排对照为准。
- **背景差异**：A-2 拿到的是原生透明（alpha=0 占 82.55%），v3 拿到的是绿幕 RGB。→ v3 可直接进 `compose_identity_greenscreen.py` 流程；A-2 不适用该流程，只能当带 Alpha 的原图用。
- 状态：**v3 阶段 A 不采用**（实测把比例带偏，见 §2.14）；**A-2 单图版为当前最佳候选**；加旗、视频、精灵、Animator、Prefab 均未做。

### 2.14 2026-11-08 1013 单人候选记录（四兵种最新任务见 §2.15）

**用户判定**：A-2（`1013_identity_v2_stageA_2_6.png`）的**比例蛮好的**；而同一模型、加入第二张参考图（旧 v2）的 **v3 双图嫁接结果反而变怪，不采用**。

| 轮次 | 输入 | 模型 | 背景 | 用户结论 |
|---|---|---|---|---|
| v2 阶段 A（2.5） | 单图 1011 | 2.5 | 绿幕 | 「比例完全不正确」→ 作废 |
| **A-2 阶段 A（2.6）** | **单图 1011 + 文字** | **2.6** | 原生透明 RGBA | **「比例蛮好的」→ 当前最佳候选** |
| v3 阶段 A（2.6） | 1011 + 1013 v2 两图 | 2.6 | 绿幕 | 「变怪」→ 不采用 |

**当时的观察（不是可推广的因果定律）**
1. 此次单图 1011 + 文字的 2.6 结果得到用户正面评价；不能据一次样本保证后续比例一定正确。
2. 此次双图 v3 不采用，但它与单图返图的提示词和输入同时变化，且请求未记录每个输入文件的哈希，不能证明第二张图单独导致变怪，也不能证明所有双图嫁接都不可用。
3. 当时模型使用 2.6 / `quality=max`（见 §2.13）；当前四兵种提案以 §2.15 为准。

**A-2 已知待办（尚未做）**
- 背景是**原生透明**（alpha=0 占 82.55%，alpha_max=253），不是绿幕 → `compose_identity_greenscreen.py` 绿幕流程**不适用**；若后续需要统一绿幕底或对齐旧管线规格（1024² / 绿 (0,159,62) / 主体高 696 / 脚底 895 / 中心 x=474.5），需单独做一次透明→绿幕合成 + 落位实测。
- 造型是否最终认可（曹军旗手的面容、布甲、双手持杆）需用户再确认一次；比例已过，造型未表态。
- 比例闸门（§2.12 C 第 1 条）建议保留一次**等高目视对照**存档（1011 盔顶→脚底 对齐 A-2 头顶→脚底），作为后续加旗后“不因旗牺牲人物”的比对基准。
- 加旗：走 `Tools/visual_brief/prompts/1013_identity_v3_stageB_flag.txt` 的思路，但**参考图只放 A-2 一张**，且需新建对应 refs 清单（不得复用 A 的清单）。加旗后必须对照 A-2 检查人物是否被重绘/拉长/缩小。

**仍未做**：加旗、任何视频、精灵帧、Animator、Prefab、波次与工程接线（§5 仍为计划）。

### 2.15 四兵种并排比例参考图 · 生成与经验

> **任务状态**：**2026-11-08 已获批准并提交 1 次，成图已出（记录见 §2.15.8）**，待用户按 §2.15.7 判定；本节其余内容仍是方案稿。成图只是比例/兵种对照参考图，不是四个角色的身份定稿。
>
> **交付边界**：生成的整张并排图只用于判断四个兵种的**体型家族和职业差异**，不是四个最终角色的身份定稿；**不得直接裁切为精灵、覆盖原图、替换已验收动画、改 Prefab 或部署**。如果用户认可其中某个方向，再另行决定是否、以及如何制作单角色身份图和动画。

#### 2.15.1 生成目标与比例口径

在同一张图上，从左至右展示四名**完全正面、全身可见**的三国敌军角色：

1. **1011 重甲兵**：已验收的短壮体量与高清像素化画风基准；形象仅用于对照，**不是要求重新设计或替换现有 1011**。
2. **1012 山贼刀兵**：以已使用的山贼身份图为造型参考；保留可见面容、布衣皮革和刀兵读感。已有成果不因本参考图被自动推翻。
3. **1013 旗手**：以用户认为观感尚可的持杆图作方向参考；在并排草图中表现为**持长杆方旌旗的单人旗手**，无鼓。该图尚未完成加旗或角色定稿。
4. **105 弓手**：以历史 1051 弓手候选提示弓、箭袋及三国军人题材；**1051 是历史候选，不等于当前正式 105 的最终设计**，服装、面部和是否采用该候选需用户后续确认。

**要统一的是人物本体的形体语言**：大头相对身体的体量、宽肩、紧凑胸腹与腰胯、相对短而结实的腿、厚实手脚和稳定站姿。四人视觉上是同一套普通敌军体型，但**不要求脸型、动作、甲胄或剪影一模一样**：1011 的肩甲本来更厚，旗手的杆和弓手的弓也会改变外轮廓。

为便于美术比较，建议四人站在同一视觉基线、人物本体高大体相当；**红缨、旗杆、旗布、刀和弓不算作人物身高**。这只是对照图构图建议，不是游戏内精确高度、碰撞宽度或最终落位的既定数值。旗面 / 武器适当留空，不压住相邻人物。

#### 2.15.2 输入参考及职责（建议四张，顺序固定）

| 图序 | 实际文件 | 输入职责与限制 |
|---|---|---|
| 1 | `Enemy1013ArtSource/02_IdentityImages/1011_green_1012_v6_reference.png` | **唯一共同体型和画风基准**：1011 的头肩、躯干、腿与手脚体块；同时是第一位重甲兵的造型参考。RGB，1108×1108；SHA-256 `3f3c17a2be763ce80a054da333ef3bbc0b9190dfeba19d8402e38e2e38b6da56`。 |
| 2 | `Enemy1012ArtSource/02_IdentityImages/input_identity_v12_bandit_idle_v6_1024.png` | 第二位山贼刀兵的脸、汉末布衣、皮革、护腕、绑腿和刀兵身份参考；不以其为全部人物比例基准。该图是山贼 Idle 所用身份图，不是后出的 v5fix 候选。RGB，1024×1024；SHA-256 `a7d6ee5de716cf24617adb9e775abfab385c5e6bae8e9d6fbf9bf571ddd76f87`。 |
| 3 | `Enemy1013ArtSource/02_IdentityImages/1013_identity_v2_stageA_2_6.png` | 第三位旗手的候选面容、布甲与双手持杆姿态参考；**图中尚无旗布**，此图没有被用户认定为最终造型，不能把细节当既定定稿。RGBA，1024×1024；SHA-256 `6a2d44b31e591ebaee76bcf748e6ae9e030597711589139c72040a79b6d5fcc7`。 |
| 4 | `C:/Users/steam/Pictures/gptGen/enemy1051_redesign/T7_1051_identity_green_v1.png` | 第四位弓手的木弓、箭袋、汉末布革题材**候选参考**，不是当前正式 105 资产，也不要求按它定最终脸型或服装。RGB，1024×1024；SHA-256 `711757e81bea91c16ce2ddf6567f95c40a3a99362f14cd3abd0183010080e7d7`。这是本机绝对路径，发起前须确认可读。 |

参考图的职责是**建议，不是模型能够严格隔离的保证**。图 2–4 如将其他兵种脸、武器或体型串入，成图必须人工判读，不得以技术回执代替验收。四图输入能否完全依序识别、输出一致体型，尚未验证；一次尝试不能证明单图 / 多图或某个模型的普遍优劣。

#### 2.15.3 参数建议与生成前确认

| 项 | 本方案建议 / 边界 |
|---|---|
| 请求类型 | 图像编辑 / 多图参考生成（如使用现有链路为 `POST /v1/images/edits`、按顺序传四个 `image[]`）；**只有获明确批准后**才发送。 |
| 模型 | 建议 `gpt-image-2.6-sunburst`，因为 1013 的 2.6 单图返图曾获得用户正面评价；不是声称其一定优于其他模型。发起时核对当前服务模型可用性。 |
| 画幅 / 尺寸 | 建议横向 3:2，**1536×1024**，便于四人一排直接比较。此尺寸在别的历史生成任务中使用过，**本模型 / 当前接口是否支持尚未验证**；不支持时停止并让用户重新确认尺寸，不自动换成方图或重复提交。 |
| 质量 | `quality=max`；若接口支持编辑类字段，`input_fidelity=high`。 |
| 输出 | PNG，n=1。**纯色背景的视觉草图**，不请求或假定原生透明 Alpha；不绘制棋盘格。可省略 `background` 字段，实际返回格式以文件为准。 |
| 成本 / 重试 | 用户批准后最多提交一次；不自动重试、不覆盖历史图。服务未返回费用时不推断金额。 |
| 保存 | 建议在工程外的 `C:/Users/steam/Pictures/gptGen/enemy_proportion_lineup_v1/` 保存原图、实际参考顺序 / 哈希、完整提示词、请求与响应记录；**此目录尚未创建**。若使用其他生成环境，另定可追溯位置。 |

**禁止照搬现有 1013 脚本直接发起**：`Tools/visual_brief/submit_1013_identity.py` 当前是单角色固定规格链路，不能只替换提示词便假定支持四图横幅；发起侧须自行确认四张文件、顺序、输出尺寸和请求体，且避免覆盖已有产物。

#### 2.15.4 中文正向提示词

```text
为一款高清像素化三国战场动作游戏制作一张角色比例研究图。画面中只有四名曹军敌兵，分别是重甲兵、山贼刀兵、旗手和弓手。从左到右一人一个位置，四个人完整正面站立，排成同一行；这是用于比较体型的设计参考图，不是战斗场景，不是角色精灵图集。

参考图一是已验收的1011重甲兵，它是四名人物共同的身体比例和体量基准：头部相对身体有分量、肩膀宽、躯干紧凑、腿短而结实、手和脚厚实，站姿稳定。四个人都属于这一短壮、严肃、具有战斗重量的人体家族；保持共同的头身关系和身体重心，但不要把四人画成同一个脸、同一套衣服或同一个剪影。重甲、布衣、持旗和持弓可以造成合理的装备和姿态区别。

第一位：参考图一的重甲兵，保留其重甲、头盔、红色布料和持剑职业特征；他只作为比例和画风对照，不要重新设计成新武将。
第二位：参考图二的山贼刀兵，露出凶悍人脸，以汉末布衣、皮革、少量护甲、绑腿与刀表现兵种；人物比例与图一同族，保留轻装与重甲的区别。
第三位：参考图三的旗手候选，保留中国古代军人的可见面容、布甲和持杆概念；一个人双手持长木杆，杆顶有一面暗红色方形军旗，不带鼓和鼓槌。旗手的人物本体与图一同族，旗杆和旗布不是人体身高的一部分；图三只是候选，不把它尚未定稿的脸和服装当成必须原样复制的最终设计。
第四位：参考图四的弓手候选，以木弓、箭袋和三国布革军人装束识别远程兵种；这只是105弓手的方向参考，不把该候选脸部或服装当成已批准的最终造型。弓手的人体与图一同族，但装备轻于重甲兵。

四个人物本体的脚底位于同一视觉基线，人物本体高度大体相当；红缨、旗杆、旗布、刀、弓和箭袋不用于比较人物身高。人物之间留出空隙，四人的头肩、身体、双手和双脚都能单独看清，旗和武器不遮挡其他人。所有角色正面朝向玩家，不要透视远近造成的大小差，也不以整个人物拉伸或缩小来掩饰身体比例差异。

高清像素化三国动作游戏美术：清楚的深色轮廓、成簇的明暗色块，以及有体积的冷铁、布料和暖棕皮革；短壮但严肃凶悍，不做软萌卡通。横向画幅，单一中性纯色背景，只展示四个人物，不要环境、场景、战斗特效、地面透视、UI、文字、编号、水印或棋盘格。
```

#### 2.15.5 English positive prompt

```text
Create one character-proportion study for a high-definition pixel-art Three Kingdoms battlefield action game. Show exactly four Cao army enemy soldiers, full-body and directly front-facing, arranged in one horizontal row from left to right: an armored infantryman, a bandit swordsman, a standard-bearer, and an archer. This is a visual design comparison, not a battle scene and not a production sprite atlas.

Image 1 is the approved Enemy 1011 and controls the shared body-proportion and mass language for all four: a head with substantial visual weight, broad shoulders, compact torso and waist, short sturdy legs, substantial hands and feet, and a stable stance. All four belong to the same short, solid, serious combat-body family, with related head-to-body proportions and center of mass. Do not make them clones with identical faces, clothing or silhouettes: heavy armor, cloth, a flag and a bow can naturally give the four soldiers distinct equipment and poses.

First, use image 1 for the armored infantryman's established armor, helmet, red cloth and sword. He is the comparison baseline; do not redesign him as a new general.
Second, use image 2 for the bandit swordsman's visible fierce face, Han-era cloth tunic, leather, limited armor, leg wraps and sword-bearing role. Give his body the same family proportions as image 1 while retaining the difference between light clothing and heavy armor.
Third, use image 3 for the standard-bearer's candidate visible face, cloth-and-armor clothing and two-handed pole-holding concept. Show one man holding a long wooden pole with a dark-red square military flag; no drum or drumsticks. His body belongs to the same family as image 1; the pole and flag are not part of his body height. Image 3 is only a candidate, not a finalized face or costume that must be copied exactly.
Fourth, use image 4 as a candidate reference for the archer's wooden bow, quiver and Han-era cloth-and-leather military theme. This is a possible direction for Enemy 105, not an approved final face or costume. His body belongs to the same family as image 1, while his equipment remains lighter than the armored infantryman's.

Place the four pairs of feet on a shared visual baseline and make their body heights broadly comparable. Do not count the plume, flagpole, flag, sword, bow or quiver as body height. Leave visible space between figures so every head, shoulder, torso, pair of hands and pair of feet can be judged separately; flags and weapons must not cover another soldier. All face the player directly. Do not use perspective depth to make one soldier larger or smaller, and do not stretch or shrink an entire body to disguise incorrect internal proportions.

Use high-definition pixel-art Three Kingdoms action-game rendering: clean dark contours, clustered light and shadow planes, solid cool iron and warm cloth / leather volumes. Compact and fierce, not cute. Landscape composition, a single plain neutral-color background, only the four characters. No environment, battle effects, perspective ground, interface, text, labels, numbers, watermark or checkerboard.
```

#### 2.15.6 共用负面提示词

```text
fewer than four people, extra person, duplicate soldiers, overlapping characters, hidden face, cropped head, missing hands, missing feet, extra limbs, swapped weapons, archer without bow, flag-bearer with drum, giant boss, identical clone faces, identical armor on all characters, long-legged realistic anatomy, tiny heads, extreme toy-like chibi, side view, three-quarter view, depth perspective lineup, flag or weapon covering another character, text, labels, numbers, emblem text, logo, UI, watermark, checkerboard, scenery, ground plane, battle effects, spell circle, photorealism, 3D render, western fantasy knight, anime moe, watercolor, blur, random pixel noise
```

#### 2.15.7 验收顺序（由用户判断，非自动通过）

1. **先看人物体型家族**：以图一 1011 为参照，比较四人的头肩、胸腹、腰胯、腿、手脚和整体重心；不因头盔、红缨、旗杆、弓箭包围框改变而误判人体。每个角色不要求一模一样，但不应一人是正常写实长腿、另一人是极端玩具体型。
2. **再看职业辨识**：能区分重甲兵、山贼刀兵、旗手、弓手；旗手无鼓，刀与弓不互换，装备 / 身份不跨人混杂。尤其 1013 人物观感不可为了迎合比例被挤压成被否决的 v3 体型。
3. **再看画面与风格**：四人完全正面、身体不被他人遮挡，缩小后仍能读出兵种；像素色簇、冷暖关系和材质与 1011 属于同一世界，不能因统一而变成四个同脸模板。
4. **确认输出真实性**：记录成图尺寸、格式、哈希、四张实际输入哈希 / 顺序、完整提示词、模型与返回值；生图技术成功不等于用户认可。用户可分别判断“体型语言”和“具体造型”，也可以只认可其中一项。
5. **不通过**：保留原始返图和请求，说明是比例、造型、排版还是画风问题，先讨论再决定下一轮；不自动重试，也不动现有角色或 Unity。**通过**：图仍是参考图；是否重制 1012、是否沿用 1013 候选、105 使用哪种造型及后续单人素材，均需用户另行决定。

#### 2.15.8 2026-11-08 实际提交记录（已生成，用户反馈效果不错）

**提交链路**：新脚本 `Tools/visual_brief/submit_enemy_proportion_lineup.py`（**未沿用** `submit_1013_identity.py` 的单角色固定规格链路；支持 `--dry-run` / `--approve`，拒绕覆盖已有产物）。先 `--dry-run` 核对四图顺序与哈希，再 `--approve` 提交**一次**，无自动重试。

| 项 | 实际值 |
|---|---|
| 接口 | `POST https://api.muskapis.com/v1/images/edits`，multipart **四张 `image[]`**，顺序固定 |
| 模型 | `gpt-image-2.6-sunburst`（提交前 `GET /v1/models` 复核：当日图像模型只有它） |
| 参数 | `size=1536x1024`（**该尺寸实测被接口接受**）、`quality=max`、`input_fidelity=high`、`output_format=png`、`n=1`、**未传 `background`** |
| 结果 | HTTP 200，**100.84s**，1536×1024，**Color Type 6（RGBA）**，2,121,586 bytes，sha256 `e08eeb0e…c96fe`，usage in 950 / out 1372 / total 2322 |
| 原图 | `C:/Users/steam/Pictures/gptGen/enemy_proportion_lineup_v1/enemy_proportion_lineup_v1.png` |
| 留档 | 同目录 `enemy_proportion_lineup_v1.{prompt.txt,prompt_cn.txt,prompt_negative.txt,request.json,response.json,status.json}` |
| 实际提交提示词 | 英文正向词 + 英文负面词合并（3797 字符，sha256 `006d333f…9652`）；中文版全文单独存档未提交 |
| 四张输入 | 与 §2.15.2 表一致，sha256 逐一核对相同：图1 `3f3c17a2…`、图2 `a7d6ee5d…`、图3 `6a2d44b3…`、图4 `711757e8…` |

**图上所见（客观记录，不代替 §2.15.7 的用户判定）**
1. 四人同排、完全正面、彼此不遮挡，职业可区分：重甲兵（带面罩、剑、红缨）、刀兵、旗手（双手持杆、暗红方旗、**无鼓**）、弓手（木弓、箭袋）。
2. 体型家族一致性：四人均为短壮重心偏低，**未出现一个写实长腿、一个极端玩具体型**的分裂；弓手相对最轻装。
3. **与提示词不完全一致的细节**：① 背景不是“单一中性纯色”，而是透明区域附近带暗色 / 暖色半透明光晕；② 旗面自发增加金色回纹边框；③ 第二位刀兵虽露脸且保留带红缨头盔，但仍有细节重绘，不能假定与输入的 1012 山贼逐像素一致。用户反馈整图效果不错，尚未逐项确认这些细节是否作为最终角色设计。
4. 回执 `input_tokens_details.image_tokens=0` 与既往几次一致；**不能据此断定参考图是否被接收**，四张输入是否真按序被采用只能以成图目视判断。

**当前状态**：用户反馈“返图结果效果不错”，认可这次参考图的整体效果；未逐项确认每个造型或具体细节。成图仍只作比例研究参考，未做任何裁切、精灵化、Prefab 改动或 Unity 部署。

#### 2.15.9 并排比例参考图的成功经验与复用边界

用户对本次返图的反馈是“效果不错”。以下是根据实际请求与成图提炼的**可尝试方法**，不把单次成功归因为某个已证实的参数：

1. **先做体型研究图，再讨论单角色定稿**。四人同图的任务是回答“是否属于同一个短壮敌军家族”，不要求一次生成四个可部署角色。这让头肩、躯干、腰胯、腿与手脚的差异能直接并排讨论，而不必先改已有角色。
2. **一张共同体型基准 + 三张职业参考**。图 1 的 1011 负责共同体型 / 体量和画风，同时参考重甲兵形象；图 2 的 1012 山贼负责刀兵身份；图 3 的 1013 候选负责旗手持杆构思；图 4 的 1051 候选负责弓与箭袋。提示词按图序分别写职责；**模型是否严格分离职责没有得到验证**，仍要检查成图。
3. **四人在同一平面作比较**。同排、正面、脚底大体同基线、无明显透视远近、人物之间有空隙，让体型差异更容易目视讨论。比较人物本体时不将旗杆、旗布、刀、弓或红缨直接当作身高。
4. **先看体型家族与职业辨识，再讨论细节**。本图四人有共同的短壮视觉语言，也可辨认重甲、刀兵、旗手和弓手；不同装备仍保留了各自的剪影。不要把用户对整图效果的认可，扩展成对每个角色脸部 / 服装已最终验收。
5. **保留请求与参考图溯源**。实际使用 `Tools/visual_brief/submit_enemy_proportion_lineup.py`，四张 `image[]` 顺序与哈希见 §2.15.8；英文正向词 + 英文负面词为实际提交内容，中文版单独存档。一次提交、无自动重试；完整请求和结果在 `C:/Users/steam/Pictures/gptGen/enemy_proportion_lineup_v1/`。
6. **记录可复现条件，但不保证复现同样结果**。本次 `gpt-image-2.6-sunburst` / `quality=max` / `input_fidelity=high` / `1536×1024` / n=1，HTTP 200；此组合在本次链路被接受。输出回执的 `image_tokens=0` **不能证明图像未被接收，也不能证明四图被严格按职责使用**；单次成功亦不能证明多图或该模型普遍优于其他方案。
7. **成图偏差与交付边界**。图片确为 RGBA，但含半透明背景光晕；此外旗面出现装饰金边，角色细节有重绘。用户称效果不错，但并未逐项认可这些细节为新规则；这张图仍是比例 / 兵种研究参考，**不能直接裁切为 Sprite、覆盖原图或替换动画**。

后续如果制作 105 等单角色，可将本图用于人工复核体型家族，同时沿用各兵种自己的身份参考；是否生成、采用哪种脸 / 服装以及是否改动现有角色，均需单独获得用户确认。

#### 2.15.10 2026-11-08 1013 idle 形象裁切（研究参考）· 量测与后续视频方案

**本节状态**：裁切、量测、绿幕锚图、对照图与两份待批提示词草案已完成；**生图与视频均未提交**（等用户批准路线与细节）。

**产出（研究参考，不是游戏素材）**

| 文件 | 用途 |
|---|---|
| `Enemy1013ArtSource/02_IdentityImages/1013_lineup_crop_reference_rgba.png`（389×720 RGBA，sha `b24a3bc…`） | 并排图里 1013 旗手的独立裁切（含杆与旗），供目视复核与后续生图 / 视频当参考 |
| `Enemy1013ArtSource/02_IdentityImages/1013_lineup_anchor_green_1024.png`（sha `2f5e6fe…`） | 同一裁切的 1024² 绿幕锚图（绿 0,159,62、脚底 895、人体中心 474.5、杆顶 26、总高 870、人身 575），作视频首尾帧与生图编辑输入 |
| `Enemy1013ArtSource/02_IdentityImages/1013_vs_1011_proportion_sheet.png`（sha `f687806…`） | 1011 vs 1013 脚底对齐并排对照 |
| `Tools/visual_brief/crop_lineup_1013.py` | 裁切 + 量测脚本（可复现；`--baseline-head-top-y`、`--green-out`、`--green-total-h`） |
| `Tools/visual_brief/prompts/1013_identity_v4.txt` / `.en.txt` / `refs_1013_identity_v4.json` | 生图修正提示词草案（**未提交**） |
| `Tools/visual_brief/prompts/1013_idle_base_v1.txt` / `.cn.txt` | 视频 idle 基准提示词草案（**未提交**） |

外部归档：`C:/Users/steam/Pictures/gptGen/enemy1013_idle_ref_v1/`（裁切、量测 JSON、绿幕锚图、对照图、量测标尺图）。

**量测结论（并排图 → 游戏尺寸）**

| 项 | 实测 | 说明 |
|---|---|---|
| 1013 人物身（头顶 y=372 → 脚底 y=847） | 476 px | 与 1011 人物身 472 px 之比 **1.008** → 体型同族 ✓ |
| 1013 杆顶总高（y=128 → 847） | 720 px＝**1.53×** 普通兵身高 | §2.3 设计目标 1.25×，超出 |
| 折算到游戏（按 1012 idle 身 605 px，k=1.271） | 总高 915 px → 杆顶 y=**−18** | 1108² 精灵画布装不下，**旗顶会被裁掉** |
| 若按 1.25× | 总高 756 px → 杆顶 y=141 | 画布与列宽都放得下 ✓ |
| 旗面宽（裁切 337 px） | 折算 428 px | 列距 2.6 世界单位＝273 px → 旗面跨 **1.57 列**，会压住邻列 |

**另记录 4 处与 §2.3 定稿设定的偏差**：旗面自发多出金回纹边框（设定要求无字无图案）；杆顶有金属帽（设定 / 历史要求“平的素木杆顶”）；旗布横向铺开（设定要求顺风向后飘、不横向展开）；头巾为靛蓝（设定为深色布头巾，皮质护额本身符合）。

**结论**：并排图的人物本体与职业读感可用，但**直接裁切进游戏会带进两个硬伤**（画布溢出、旗宽压列）与两处装饰偏差。后续视频内容按两条路线备选：

- 路线 A（零生成）：直接用绿幕锚图当视频首尾帧 → idle 基准视频。代价是上述硬伤全留在游戏侧：要么改用更高画布并接受 1.53× 旗手，旗宽压列无解。
- 路线 B（推荐，1 生图 + 1 视频）：先用 `1013_identity_v4` 提示词对绿幕锚图做**单图编辑**（修杆高 / 旗宽 / 旗面 / 杆顶），与裁切逐项比对验收后，再用成图当视频首尾帧 → idle 基准视频。

**视频方案（idle 基准，路线 B）**：`seedance-2-fast` / 4s / 720p / 1:1 / `generate_audio=false` / `watermark=false` / `seed=-1`；`first_frame=last_frame=锚图`。构图数字写进提示词：人物身占画幅高 76%、头顶 21%、杆顶与旗顶 95%、脚底 97%（720p 下人物身≈547 px → 折算到游戏身 605、总高 756、杆顶 y=141，与 1.25× 口径自洽）。提示词只写形象层与呼吸幅度，不描述动作幅度（沿用替换视频工作流教训）。

##### 2.15.10.1 2026-11-08 实际提交：idle 基准视频（用户选定「直接用裁切做视频」）

用户选择**不做生图修图**，直接以并排图裁切为形象来源做视频；本轮只做 **idle 基准 1 支**。

| 项 | 实际值 |
|---|---|
| 参考图 | `Enemy1013ArtSource/02_IdentityImages/1013_lineup_anchor_green_1024.png`（sha `2f5e6fe…`）；`first_frame` = `last_frame` = 同一张 |
| 接口 / 模型 | `POST https://direct.reachapi.ai/v1/vids/create`，`seedance-2-fast` |
| 参数 | `duration_seconds=4`、`resolution=720p`、**`aspect_ratio=3:4`**、`generate_audio=false`、`watermark=false`、`seed=-1`；一次提交，无自动重试 |
| 画幅改动理由 | 1:1 装不下高杆角色：1:1 下旗顶要贴边、人物身只剩 ≈447 px、到游戏要放大 1.35 倍（像素会糊）；3:4 保住人物身 ≈571–642 px，放大 ≈1.06 倍，与 1012 同级锐度。`Tools/visual_brief/submit_seedance.py` 新增 `--resolution/--aspect`（默认仍为 720p/1:1，不影响既有链路） |
| 提示词 | `Tools/visual_brief/prompts/1013_idle_base_v1.txt`（英文，3811 字符；中文同义存档 `.cn.txt`）。只写形象层与呼吸幅度，**不描述动作**；另写一段锁道具比例（杆顶≈1.5×人物身、旗布尺寸不变），防模型擅自缩短旗杆 |
| 任务 | `task_7db8332ee373451980f8db435d406c73` → success |
| 成片 | `C:/Users/steam/Videos/doubaoVideo/1013_idle_base_v1.mp4`；已归档 `Enemy1013ArtSource/01_SourceVideos/1013_idle_base_v1.mp4`（1,833,560 bytes，sha `1f6d60a8…`），并登记到 `00_Index/videos.csv` 的 `idle_base` 行 |
| 画面 | **834×1112**（接口 720p+3:4 的实际输出）、97 帧 @24fps、4.04 s；绿幕四角中位 (0,153,54)（比基准 (0,159,62) 低约 6 G，属已知输出片基偏移） |

**成片量测（`Tools/visual_brief/chroma_key.py` 抠图后逐帧）**

| 项 | 实测 | 判定 |
|---|---|---|
| 人物身（头顶 340 → 脚底 981） | 642 px | 折算 k=605/642=0.942 → 游戏里人物身 605 ✓ |
| 杆顶总高（16 → 981） | 966 px＝**1.51×** 人物身 | 与并排图裁切的 1.51× 一致，模型没改道具比例 ✓；但仍是设计目标 1.25× 的约 1.2 倍 |
| 画内完整性 | 顶端余 16 px、底部余 131 px | 全身与旗顶都在画内，**未被裁切** ✓ |
| 循环 | 首末帧 IoU **0.9958** | 无缝循环 ✓ |
| 逐帧稳定 | 脚底 y=981、旗杆 x≈172、旗顶 y=16 全程不变 | 旗布在飘、人物只有极轻呼吸（本体 IoU 0.95–0.97）✓ |
| 形象 | 脸、头巾、络腮胡、布袍、轻甲片、腰带、绑腿靴、双手握杆均与裁切一致 | 身份锁定成功 ✓ |

**留作 Unity 阶段的两个待决项（本轮未动工程）**：
1. 杆顶总高折算后 910 px，脚底 897 → 杆顶 y=**−13**，1108² 精灵画布装不下；1013 可能需要更高的画布，或改成 1.25× 的道具比例（要重生成）。
2. 旗面宽折算约 428 px，列距 2.6 世界单位＝273 px → 旗会压到邻列。

##### 2.15.10.2 Cast（号令）动画生成方案（2026-11-08 备妥，**待用户批准**）

**动作语义（来自 §2.5，不自行扩充）**：连续收势 → 将旗杆向前上方一顿、旗面绷紧展开（0.8s 内玩家能看出“要发令了”）；被直接伤害打断时本次号令取消。**不做魔法爆炸、不加闪光**（§2.5 的“完成点可考虑敌人闪光”是后续表现提案，不在本轮）。

**路线**：Cast 无母本 → **首尾帧工作流**（`first_frame` = `last_frame` = 锚图）+ 可量化幅度的文字描述。已知风险是新动作幅度失控（1012 实测 4.5×），因此提示词必须写上限数字。

**锚图（已生成）**：`Enemy1013ArtSource/02_IdentityImages/1013_cast_anchor_green_834x1112.png`（sha `231b4d64…`），由 idle 基准视频 f000 抠帧后重贴而成（工具 `Tools/visual_brief/build_anchor_from_keyed_frame.py`）：

| 项 | 值 | 理由 |
|---|---|---|
| 画幅 | 834×1112（3:4，与 idle 同幅） | 沿用 idle 的比例工具链；横向余量大 |
| 人物身（头顶→脚底） | 560 px | 折算到游戏 k=605/560=1.08（idle 是 0.94），锐度接近 |
| 脚底线 / 旗顶 | y=1000 / y=153 | **旗上方留白 153px＝身高 27%**，给“向前上方一顿”留行程（idle 同构图只剩 16px，一动就出画） |
| 水平 / 底部 | 人物最左 20%、底部留白 112px | 与 idle 同一相对位置，避免落位偏移 |
| 绿幕 | (0,159,62) std=0 | G1 门槛自测通过 |

**提示词**：`Tools/visual_brief/prompts/1013_cast_v1.txt`（英文，**已被 v2 取代**）/ `1013_cast_v1.cn.txt`；**v2 = `1013_cast_v2.txt`（英文，5844 字符，提交用）/ `1013_cast_v2.cn.txt`（中文存档）**。

**v2 重写原因（2026-11-08 用户反馈：动作与表情缺乏张力）**：v1 把脸锁死（"Keep his face exactly as in image 1" + 一长串 "never grins / never opens his mouth / never shows his teeth / never speaks"）并把幅度上限收紧到 8%/5%/3% —— 这正是 1012 v3 的失败模式（负向词 = 告诉模型"脸别动"），必然出不来表情弧线与力量感。v2 改为：
- **脸是第二个主动作**，写成与四拍同调的**时间线弧线**（冷 → 绷 → 张口喝令的峰值 → 峰值咬住 → 收回），并明写"唯一峰值"；
- **负向词只留四条**（喝令不咧嘴/不露下齿/不友善/不疲惫），删掉所有锁脸的句子；
- **四拍**：沉（前 1/3，向后下压、拧肩 12°、下沉 4%）→ 发（0.15 身高内上抬并前送、前倾 8°、头抬起、旗面绷开＋布边波浪）→ **定（3–4 帧几乎不动的峰值保持）** → 收（沉重回落）；
- 幅度放宽：杆上升 8%→15%、下沉 8%、躯干前倾 5%→8%、头动 3%→6%；杆顶回落/横摆受限以保证旗不出框；**双脚不离地线、杆不换手、旗面与金边不变**的红线保留。

**提交参数**：`POST https://direct.reachapi.ai/v1/vids/create`、`seedance-2-fast`、`duration_seconds=4`、`resolution=720p`、`aspect_ratio=3:4`、`generate_audio=false`、`watermark=false`、`seed=-1`；一次提交，无自动重试；已 `--dry-run` 校验参数与哈希。

**成片验收计划**：① 用 `Tools/visual_brief/measure_expression_arc.py` 量表情弧线是否有趋势（峰值不在 f0）；② 目视六帧对照抓"沉/发/定"三个关键帧；③ 逐帧量测杆顶/脚底/画幅留白，确认没出框；④ 表情弧线不合格则按 1012 两阶段法补 E1 头部研究片再合成 E2。

**游戏侧衔接（待 Unity 阶段）**：Cast 状态取用片段里"沉→发→定"的 4–6 帧，映射到 0.8s 前摇；"定"的几帧作为峰值必须进动画；被打断时由状态机切回 Idle。**选帧与落位尚未做。**

> **⚠ 2026-11-08 用户重新判定：idle 基准视频 v1 验收不合格。** 理由：① **画幅用了 3:4 而不是项目标准 1:1**；② 人物旗杆总高 1.51×（设计 1.25×），导致旗顶贴边、1108² 画布溢出、旗宽压邻列；③ 动作与表情几乎静止，无表现力。v1 仅作为事实记录保留，**不作为 1013 的 idle 素材**。修正路线见 §2.15.10.4。

##### 2.15.10.3 Cast v2 实际提交：**失败样本**（2026-11-08）与修正方案

**提交记录**：`task_d70869afe43d4ef2a3f89e1dcb0331bb` → success；`seedance-2-fast` / 4s / 720p / 3:4；成片 834×1112、97 帧；归档 `Enemy1013ArtSource/01_SourceVideos/1013_cast_v2_FAILED.mp4`（sha `3c363cef…`），逐帧量测 `C:/Users/steam/Pictures/gptGen/enemy1013_idle_ref_v1/cast_v2_measure.json`。

**结论：不可用。** 文字描述的幅度全面失控（实测）：

| 项 | 意图 | 实测 |
|---|---|---|
| 旗杆运动 | 后压 → 前上方甩出，杆顶上升 ≤15% 身高 | **杆顶 y 在 88..513 之间波动 425px**（≈身高 1 倍）；旗杆在 f016–f044 **横甩成近水平**，旗出画，左边界贴 0 共 **29 帧** |
| 身体压缩 | 下沉 ≤4% 身高 | 主体高 867 → **最低 498（57%）**，f043 处呈深蹲 |
| 表情 / 发力 | 张口喝令峰值 | 未按描述发生（动作段脸被杆与蹲姿遮蔽，读不出峰值） |
| 地线 / 镜头 | 不动 | ✅ 脚底恒定 y=1010，帧幅与缩放未漂 |

**根因（三条，均有项目既有证据）**：
1. **纯文字描述新动作＝重新定义动作**，幅度会失控——1012 管线已实测（幅度放大 4.5×）。本次把同一失败重演了一遍（30 倍）。
2. **模型只认“位置/画幅”类约束，不认“角度/相对量”类约束**：地线（位置）与帧幅（画幅）守住了；“15% 身高 / 12 度 / 4%”全被忽略。“杆身以肩为轴转动”这类措辞反而暗示了大幅旋转。
3. **1012 的实测结论：锁住动作的是“路线”不是“词”**——首尾帧锚图路线同时锁住角色与动作（v6：145/145 帧角色一致 ✓）；纯文字路线只能锁大意。

**修正方案（待批准，Route：首尾帧锚图 + 文字只作补充）**：
1. **先用生图做一张「发」姿态末帧**（单图编辑，输入 = 现有 Cast 锚图；`gpt-image-2.6-sunburst` / `quality=max` / `input_fidelity=high` / 1024² / 绿幕）。用**位置类措辞**（“旗杆保持竖直、杆顶只比图 1 高一点点、仍远在顶边之下、身体略微前倾、旗面绷开、脸是张口喝令的峰值”），**且在花视频之前先用像素量测验收这张图**（杆的倾角、杆顶 y、人体高度比）——图片不合格就不烧视频。
2. **视频改成首尾帧**：`first_frame` = 现有 Cast 锚图（idle 姿态），`last_frame` = 「发」姿态图。幅度由两张图夹住，模型只负责中间过程；**表情峰值直接由末帧给出**（同时解决“脸不动”），起手表情自然等于 idle 长相。
3. **提示词只保留**：身份/装备锁定、节奏（前三段慢蓄力 → 一记快甩 → 短暂停住）、镜头/地线/不换手/旗面金边不变等位置类红线；**删掉角度与百分比幅度**（改由两张图承担）。
4. 参数沿用：`seedance-2-fast` / 4s / 720p / 3:4 / `generate_audio=false` / `watermark=false` / `seed=-1`。

**同时记住的教训**：带长道具的角色，动作幅度**不能靠文字数字控制**，必须靠帧（首/末帧图）控制；文字里的百分比与角度对模型基本无效。

##### 2.15.10.4 2026-11-08 用户纠正后的**修正路线**（待批准）

**用户指出的四个问题与结论**
1. **画幅必须是 1:1**（项目标准）。当时为了塞下高杆角色把视频改成 3:4 —— 那是"改参数绕过根因"，方向就错了。正确做法是**先修人物的旗杆比例**，让内容回到标准画幅。
2. **Cast 动作应是简单的挥舞旗帜**，不该旋转旗杆与旗面；v2 的横甩与深蹲正是"旋转"类描述带来的。
3. **idle 基准视频 v1 验收不合格**，须按 1:1 重出。
4. **"末帧姿态图"的前提是错的**：首尾帧工作流里 `first=last=idle` 时末帧就是 idle，不该再单独造一张"发"姿态图；该路线整体作废。

**唯一根因**：1013 的**旗杆总高 = 1.51× 人物身高**（设计 1.25×，见 §2.3）。它一个原因同时造成：720p 1:1 装不下（1.25× 要求人物身 ≤576px）、旗顶贴边、1108² 画布溢出（杆顶 y=−13）、旗宽折算 428px 跨 1.57 列。**比例不改，后面每一步都只是在为它打补丁。**

**修正后的标准参数（全部回到项目标准）**

| 项 | 值 |
|---|---|
| 视频画幅 | **1:1 / 720×720**（与 1012 管线一致；`submit_seedance.py --aspect 1:1`） |
| 人物身在帧内 | 约 **520px**（720 的 72%）；脚底 y=700；头顶 y=180 |
| 杆顶 | 人物身 1.25× = 650px → 杆顶 y=50（距顶边 50px） |
| 落位 | k=605/520=1.163 → 游戏内人物身 **605**（与普通兵同高）、总高 **756**（1.25×）、杆顶 y=141 → **1108² 画布放得下** ✓、旗宽不再跨列 ✓ |
| 代价 | 1013 的像素比 1012 放大 1.16×（1012 是 0.94 缩小），略软一点；这是"方画幅 + 1.25× 道具 + 与普通兵同身高"三者同时成立下的必然结果 |

**步骤（待批准后执行）**
1. **一次生图修比例**（单图编辑，输入 = 现有 1024² 绿幕锚图 `1013_lineup_anchor_green_1024.png`）：把**杆顶到脚底**改成**人物本体身高的 1.25 倍**，旗面随之下移并略缩（旗宽不超过肩宽 1.2 倍）；人物身份、装备、旗面金边、杆顶金属帽、站位与脚底位置全部不变。成图**先量测验收**（杆顶 y、人体高、旗宽、杆倾角），不合格不进入下一步。
2. 用 `Tools/visual_brief/build_anchor_from_keyed_frame.py` 把验收通过的成图重贴成 **720×720 绿幕锚图**（身体 520、脚底 700、杆顶 50）。
3. **重出 idle 基准**：1:1 / 720p / 4s / `first=last=锚图`。
4. **Cast 改为"挥舞旗帜"**（1:1）：旗杆**保持竖直不动**，旗布做一次明显的大幅甩动（自由边甩出波浪），身体只做小幅前倾与抬头，脸在甩动顶点张口喝令；幅度全部落在旗布自身区域，不可能出画。
5. 选帧 → 抠图 → 落位 → Unity 接线。

##### 2.15.10.5 Cast v3：**通过**（2026-11-08，用户定的“简单摇旗 + 拉大画布提分辨率”）

**用户口径（2026-11-08 纠正）**：不改旗杆比例、不生图；Cast 就是**简单的摇旗**（杆不动，只有旗布在动 + 顶点张口喝令）；画布不够就**拉大画幅/提分辨率**，而不是改管线参数绕过根因。v2 的“旋转旗杆/横甩”整段作废。

| 项 | 实际值 |
|---|---|
| 提交 | `task_a0598273dc3441c6bc4c7a3848ad9061` → success；`model=seedance-2`（`seedance-2-fast` 不支持 1080p）/ `duration=4` / **`resolution=1080p`** / **`aspect_ratio=1:1`** / `generate_audio=false` / `watermark=false` / `seed=-1`；一次提交无重试 |
| 提示词 | `Tools/visual_brief/prompts/1013_cast_v3.txt`（EN 3829 字符，提交用）/ `1013_cast_v3.cn.txt`（中文存档） |
| 首帧 | `Enemy1013ArtSource/02_IdentityImages/1013_cast_anchor_green_1080x1080.png`（1080² 绿幕 0,159,62 std=0；人物身 660、旗顶 y=43、脚底 1040；由并排图裁切重贴） |
| 成片 | `Enemy1013ArtSource/01_SourceVideos/1013_cast_v3.mp4`（2,359,648 bytes，sha `bc7b7b29…`），**实际帧幅 1440×1440**（接口的 “1080p/1:1” 实为 1440²）、97 帧 @24fps |

**逐帧量测（全部通过）**

| 项 | 实测 | 判定 |
|---|---|---|
| 旗杆 | 杆顶 y=59 **逐帧恒定** | 杆竖直不动 ✓（v2 的横甩已消失） |
| 地线 | 脚底 y=1385 恒定；主体高 1325 恒定 | 镜头/缩放锁定 ✓ |
| 出框 | 上/下/左/右均无出框 | ✓ |
| 旗布 | 自由边右端 947 → 1183 → 回落 947 | 大幅甩动 ✓ |
| 表情 | **f040 张口喝令**（下颌打开、露上齿、头抬起） | 峰值真的出来了 ✓ |
| 表情弧线闸门 | rise 2429 ≥ 60 **PASS**；fall Q3 5727 < 峰值×0.6 **PASS**；末帧≈首帧 **PASS** | 总体通过 ✓ |
| 循环 | 首末帧 IoU **0.9976** | 无缝 ✓ |

**落位（游戏侧）**：k=605/888=**0.681** → 游戏内人物身 **605**（与普通兵同高）、总高 **903**（1.49×）；1108² 画布把**脚底线放到 1000**（杆顶 y=97）→ 旗面落在邻列头顶以上的空白区，“旗压邻列”自然消解。

**残留事项**：① 旗布在甩动顶点被模型拉成几条飘带（风格化过强，可接受但需目视定夺）；② 1080p 的实际帧幅是 1440²，抠图/落位脚本要按真实帧幅处理；③ idle 基准也需用同一套 1:1 + 1080p 重出（现役 v1 是 3:4，已标不合格）。

##### 2.15.10.6 Cast v5：**通过**（2026-11-08）——动作/表情/不超框 三项同时成立

**为什么有 v5**：v3（杆不动、风吹旗布）虽然锁得死但**不是用户要的“摇旗”**；v4（“他自己挥旗两下”）模型把幅度放飞——杆顶 y 在 208–724 乱窜、主体高 665–1126 变.scale、**17 帧出画左边**。v5 只改一件事：**把幅度写成可校验的图像相对量**。

| 项 | 实际值 |
|---|---|
| 提交 | `task_55e05cd3a9b84482b613c6ec8a52108e` → success；`seedance-2` / 4s / 1080p / 1:1 / `generate_audio=false` / `watermark=false` / `seed=-1`；一次提交无重试 |
| 提示词 | `Tools/visual_brief/prompts/1013_cast_v5.txt`（EN，提交用）/ `1013_cast_v5.cn.txt` |
| 首帧 | `Enemy1013ArtSource/02_IdentityImages/1013_cast_anchor_green_1080x1080_v4.png`（1080² 绿幕，人物身 470＝画幅 43%，旗顶 y=290，人物偏左 12%） |
| 成片 | `Enemy1013ArtSource/01_SourceVideos/1013_cast_v5.mp4`（2,432,927 bytes，sha `daa8dfeb…`），实际帧幅 **1440×1440**、97 帧 @24fps |

**验收（三项优先全中）**

| 项 | 实测 | 判定 |
|---|---|---|
| **动作** | 杆在原地小幅摇动（杆顶横向总位移 **161px ≈ 0.9 个头宽**、竖向波动 43px），每摇一次旗布被甩开一次、自由边成飘带 | 他在摇旗 ✓ |
| **表情** | 峰值帧张口喝令；表情弧线闸门 rise 8188 / fall PASS / 末帧≈首帧 PASS | ✓ |
| **不超框** | **出框 0 帧**（v4 是 17 帧） | ✓ |
| 稳定性 | 脚底 y=1333 恒定、主体高 941–983 | ✓ |
| 落位 | k=605/627=**0.965** → 游戏内人物身 605、总高 912（1.51×）；1108² 画布脚底线 1000 → 杆顶 y=88 | ✓ |

**本轮沉淀的硬结论（写给以后所有生成任务）**：
1. **模型只服从“位置类”约束，不服从“模糊幅度词”。** v3 的“杆顶与画面 1 完全同高同位” → 逐帧 y=59 恒定；v4 的“只小幅摆动 / 只微微倾斜” → 幅度发飞、17 帧出画。**幅度必须写成可事后量出来的图像相对量**（如“横向移动不超过他自己一个头宽”“停在画面 1 里杆顶所在的那一小块区域”）。
2. **动作语义必须写“他做什么”，不能写“环境造成什么”。** v3 写的是“风吹旗布”→ 人像雕像；改成“他自己挥旗”才是用户要的。
3. **要给动作留行程，先把人物在画幅里缩小**：方形画幅里长杆角色占满时杆一动就出框；人物降到画幅 43% 后，杆有 ±一个头宽的摆动空间。
4. 表情要写成**与动作同拍的弧线**（顶点张口喝令），负向词只留四条（喝令不咧嘴/不露下齿/不友善/不疲惫），否则等于告诉模型“脸别动”。

##### 2.15.10.7 Cast v7：**高举战旗**（2026-11-08 用户改订语义后生成）

**语义改订**：§2.5 的 Cast 从「收势→向前上方一顿、旗面绷紧」改为**「高举战旗 + 呐喊」**（胜利式）——沉一下蓄力 → 猛地举到最高点（杆顶升约身高 1/4，身体挺直挺胸、下巴抬起）→ 旗面甩开绷直 → **定住 3–4 帧**；杆到顶那一瞬张口呐喊。

**锚点问题（本次同步修）**：
1. **提示词里的构图数字必须与锚图实际几何自洽**。v7 初稿写「杆顶举到画幅 8%」，但锚图（杆顶 27%、人物身 43%）按 1/4 身高推算应为 **16%**——两个数字互相矛盾，模型会各取一半。已改为 **27% → 16%**（16% × 1440 ≈ 230px，约等于一个头高，与「始终留一个头高」一致）。
2. **Cast 与 idle 必须共用同一张锚图**：v1 用的锚图与 v6 不同（人物高 660 vs 470、构图不同），落位后水平位置对不上，idle→cast 切换会跳。**idle 重出时必须用 `1013_cast_anchor_green_1080x1080_v6.png`（身体居中、人在画幅正中、旗向右展）**。
3. 居中原则：**人（身体中心）居中，旗子才有资格往一侧伸**；v5 之前把整张图（含旗）按最左像素放到 12%，导致人在画幅 24% 处、右半张空。

**提交**：`seedance-2` / 4s / 1080p / 1:1 / `generate_audio=false` / `watermark=false` / `seed=-1`；`first_frame=last_frame=` `Enemy1013ArtSource/02_IdentityImages/1013_cast_anchor_green_1080x1080_v6.png`；提示词 `Tools/visual_brief/prompts/1013_cast_v7.txt` / `.cn.txt`。


**v7 结果（已生成并验收通过）**：`task_640451d926e14eed9584012d6a3abf01` → success；`Enemy1013ArtSource/01_SourceVideos/1013_cast_v7.mp4`（2,046,672 bytes，sha `a3e514af…`），实际帧幅 1440×1440、97 帧 @24fps。

| 项 | 实测 | 判定 |
|---|---|---|
| 动作 | 杆顶从 y=388 举到 **y=74**（上升 322px，旗顶到画幅 5%）；**f032–f072 持续定住在最高点约 1.7 秒**；f000–f024 蓄势、f080–f096 回落，末帧＝首帧 | 「高举战旗」成立 ✓ |
| 表情 | 冷（f000 闭嘴）→ 绷（f024 眉锁）→ **张口呐喊**（f032–f072）→ 收（f088–f096）；表情弧线闸门 rise 3802 / fall PASS / 末帧≈首帧 PASS | ✓ |
| 不超框 | **出框 0 帧**；脚底 y=1333 恒定 | ✓ |
| 居中 | 身体中心约在画幅 46%，旗向右展开 | 居中规则生效 ✓ |
| 落位 | k=605/627=**0.965** → 游戏内人物身 605 | ✓ |

**由此带来的工程侧结论（Unity 阶段核对）**：杆顶最高点折算后 **1215px 高于脚底** → **1013 不能沿用 1108² 精灵画布**（静息态就已放不下：总高 914 > 897）。需要约 **1108×1440、脚底线 y=1350、pivot ≈ (0.654, 0.737)**（按 1012 idle pivot 相对脚底的 289px 偏移折算），Unity 阶段实测定稿。


**v9（2026-11-08，峰值不停留）**：用户指出 v7「高举只是挥舞了一下，之后完全没变化」。实测 v7/v8 的病根是：**模型把"定住"理解成长时间冻结**（v7 的杆顶在 y=74–99 停了约 1.7 秒、37 帧近似静止），而且**杆不动 → 旗布也不动**（模型的物理直觉）。v9 改法：① 把"定住"整拍删掉，改为「**旗杆直接穿过去、马上甩回来**」；② 明写「**杆可以停，布不许停**：旗面轮廓每一帧都不一样」；③ 上升量封顶 16%。

| 项 | v9 实测 | 判定 |
|---|---|---|
| 动作 | 杆顶 388 → **44**（上升 344px）；最高点期间杆顶在 44–94 间持续抖动（**不再是冻结**）；f078 后杆甩回；末帧 IoU **0.9954** | ✓ |
| 旗布 | 最高点每一帧轮廓都不同（旗布区帧间差中位 0.084） | ✓ |
| 表情 | 冷 → 绷 → **张口呐喊**（峰值 @f38）→ 收；弧线闸门 rise 2968 / fall PASS / 末帧≈首帧 PASS | ✓ |
| 不超框 | 出框 **0 帧**；脚底 y=1333 恒定 | ✓ |

成片：`Enemy1013ArtSource/01_SourceVideos/1013_cast_raise_v9.mp4`（sha `d66c3608…`，原名 `1013_cast_v9.mp4`）。

**沉淀**：模型会把"定住/保持"直接渲染成长时间静止，并且默认"杆停 → 布停"。**要峰值有张力，就写"穿过去"而不是"定住"，并显式写"杆可以停、布不许停、旗面每帧轮廓不同"。**


**v10（2026-11-08，「挥」的正确写法）**：用户追问"什么叫挥舞旗帜"——点破前面几轮做的是**举**（一次性上举到某个姿势停住），不是**挥**（反复横扫划弧）。v10 改法：把动作整段改成「**在身前反复横扫，杆顶划弧来回甩，一下接一下不停顿；布面滞后甩在杆后，末端炸开再回落**」，并明写「他没有把旗举到某个位置停住，而是一直在挥」；幅度限制改为「杆顶高度基本不变（27% 附近），只有横向弧线扫动，横向≤1.5 个头宽且不越过画面中线」。

| 项 | v9 | **v10** |
|---|---|---|
| 近似静止帧（IoU<0.01） | 37 帧 | **0 帧** ✓ |
| 帧间差中位 | 0.0426 | **0.1398**（3.3×） ✓ |
| 出框 | 0 帧 | **0 帧** ✓ |
| 旗/杆横向行程 | 几乎不横移 | 左边界 66→530、右边界 903→1339（**大幅横扫**） ✓ |
| 杆顶高度 | 44–388（大幅升降） | 313–416（**基本不变**，符合"只横挥"） ✓ |
| 表情 | 峰值 @f38 | 拧眉蓄力 → **最后一甩顶点张口呐喊**（f080–f088）→ 收 ✓ 闸门 rise 5940 / fall PASS / 末帧≈首帧 PASS |
| 循环 | IoU 0.9954 | IoU **0.9960** ✓ |

成片：`Enemy1013ArtSource/01_SourceVideos/1013_cast_v10.mp4`（sha `1be39793…`）。

**沉淀**：想要"挥舞"就不能写"举到最高点/定住"——模型会把"举 + 定"渲染成一次性上举加长时间冻结。**写"反复横扫划弧、一下接一下不停顿、布面滞后甩在杆后"，并用"旗面轮廓每一帧都不一样"强制布面持续变化。**


##### 2.15.10.10 Cast 改为**两段**（2026-11-08 用户要求：举起 → 挥舞）

**用户口径**：Cast 分两段——第一段举起，第二段挥舞；"当前已有举起，找个举起后的动作作为起始帧再开始摇旗"。

**为什么"从举到顶的姿态直接接挥舞"在几何上做不到**：1.51× 的长杆角色在 1440² 方画幅里，举到顶时**杆顶只剩 68px 余量、旗的右缘只剩 405px**，横向一扫必然出框。实测两版都以出框告终（wave v1：4 帧出画，杆被甩到腰高；wave v2：21 帧出画，两侧都贴边）。**"旗举到顶"和"旗能横扫"在方画幅里互斥**——要么挥舞发生在有余量的高度，要么画幅要加宽。

**落地方案（不新增生成，两段现有素材拼接）**：
- 段一 **举起** = `1013_cast_raise_v9.mp4` 的 f000–f090（举 → 顶点 → 张口呐喊 → 落回待命位）
- 段二 **挥舞** = `1013_cast_w10.mp4` 的 f000–f060（从待命位反复横扫划弧）
- **接缝天然连续**：v9 的 f090 与 v10 的 f000 是同一个锚图姿态，bbox 差 0/0/10/0 px
- 预览：`C:/Users/steam/Pictures/gptGen/enemy1013_idle_ref_v1/cast_two_segments_preview.mp4`（152 帧 / 6.33s @24fps）

**待决（选帧阶段）**：源片 6.33s 要压进 0.8s 前摇（约 8×），需要选帧时决定"举起"与"挥舞"各取几帧、哪几帧是发力帧。

**若要真正的"举起后直接接挥舞"**：需要重新出一支**终点更低的举起**（杆顶停在画幅约 20% 而不是 5%），给挥舞留出行程——注意模型会**放大**上升量（要求 16% 实际做到 5%），终点高度只能靠"位置类"措辞反复试。


##### 2.15.10.11 Cast 的 **12 帧**：举起 → 挥舞 → 收回到 idle（2026-11-08 用户定）

**用户要求**：Cast 动作要**完整走完「举 → 挥 → 收回到 idle」**，并且要检查取帧是否真的满足。

**关键事实**：wave_v1 / wave_v2 两支的**首尾都是"举起姿态"**（f000 与 f096 的杆顶同为 y=68），它们**没有"回到待命"这一段**；"落到待命高度"只在 v9（举起片）的下落段里。所以 12 帧必须**跨三段来源**拼：
- 举 = `1013_cast_raise_v9.mp4`（idle → 举起 → 顶点呐喊）
- 挥 = `1013_cast_wave_v1.mp4`（从最高处划弧甩下、甩过、绕回）
- 收回到 idle = `1013_cast_raise_v9.mp4` 的下落段（旗降 → 回到待命）

**取帧表（12 帧，全部未贴边，最小边距 62px）**

| # | 源 | 帧 | 动作含义 | 杆顶 y | 人物身高 |
|---|---|---|---|---|---|
| 01 | v9 | f000 | **idle·起**（＝末帧同姿态） | 388 | 621 |
| 02 | v9 | f014 | 沉·蓄力 | 388 | 589 |
| 03 | v9 | f026 | 起举 | 385 | 591 |
| 04 | v9 | f038 | 举到顶 | 95 | 635 |
| 05 | v9 | f056 | 顶点·呐喊 | 64 | 635 |
| 06 | wave_v1 | f008 | 挥·起手·高位 | 62 | 645 |
| 07 | wave_v1 | f016 | 挥·甩出·旗到左 | 230 | 624 |
| 08 | wave_v1 | f020 | 挥·横扫中 | 634 | 646 |
| 09 | wave_v1 | f024 | 挥·甩到右 | 208 | 659 |
| 10 | wave_v1 | f066 | 挥毕·回正·旗立 | 70 | 619 |
| 11 | v9 | f080 | 收·旗降 | 192 | 605 |
| 12 | v9 | f090 | **收回 idle**（＝首帧同姿态） | 388 | 621 |

**挥舞段的取帧原则（2026-11-08 用户纠正）**：wave_v1 的 **f026–f060 段旗自己掉头反挥**（f036 最明显），已弃用；挥舞只取 **f018–f024 的同向连续横扫**（中间帧 f020/f021）+ **f060 之后的回正帧**（f062–f070，杆重新立直、旗归位）。所以挥舞段 = f008（起手）→ f016 → f020 → f024（甩到右）→ f066（回正），**一个方向来回一次，没有反向乱挥**。

**落位口径**：人物身高 589–659，中位 622 → k = 605/622 = **0.972** → 游戏内人物身 605；**画布 1108×1440、脚底线 y=1350** → 顶点帧杆顶 y≈120（1108² 装不下，见 §2.15.10.7）。

**帧率**：按 §2.5 的 0.8s 前摇，12 帧 = **15fps**（由「0.8s + 12 帧」直接推出；若要放慢到 1.5s/8fps，需要同步改 §2.5 的 0.8s 口径）。

**产物（已验收，存于工程内）**：`Enemy1013ArtSource/03_CastFrames/`（12 张已抠图 PNG + `frame-provenance.csv` 逐帧来源 + `cast_12_frames_sheet.png` 序列图）。外部同份副本：`C:/Users/steam/Pictures/gptGen/enemy1013_idle_ref_v1/cast_12_frames/`。


##### 2.15.10.12 Idle 验收与 6 帧关键帧（2026-11-08 用户判定合格）

**用户判定**：`1013_idle_base_v1.mp4` **合格**（此前标"3:4 不合格"的判断作废；画幅差异在落位阶段归一，不影响验收）。源片已移回 `Enemy1013ArtSource/01_SourceVideos/1013_idle_base_v1.mp4`。

**取帧方法（第一次取错，第二次改对）**：idle 片的旗布是**连续模拟的飘动、不是可重复的周期** → ① 用「轮廓配对距离 + 固定周期等距取帧」会得到**接缝跳旗形**的 6 帧（首次取 f003/f006/f008/f011/f014/f016 即为此问题）；② 闸门也错在用**整幅剪影**算距离——不动的身体占了绝大部分，淹没了旗布形状差异，指标说「无缝」而实际会跳。

**正确取法**：① 闸门只算**运动区**（全片逐帧 XOR 的并集＝只有旗布在动）；② 先找**旗形最接近的一对首尾帧**，再在两者之间等距取 6 帧。本片最佳对 = **f040 ↔ f060**（运动区差异 0.100，随机配对约 0.38）。

| # | 源片 | 帧 | 人物身高 |
|---|---|---|---|
| 01 | idle | f040 | 633 |
| 02 | idle | f044 | — |
| 03 | idle | f048 | — |
| 04 | idle | f052 | — |
| 05 | idle | f056 | — |
| 06 | idle | f060 | 635 |

**像素密度一致性**：idle 源片人物身高 636px、Cast 源片 632px（两者虽分辨率不同，但主体尺度几乎一样）→ 落位 k 分别 605/636=**0.951**、605/622=**0.972**，两组到游戏内都是人物身 605，**不会出现 idle 比 cast 精细/粗糙的观感差**。

**产物**：`Enemy1013ArtSource/04_IdleFrames/`（6 张已抠图 PNG + `frame-provenance.csv` + `idle_6_frames_sheet.png`）。

## 3. 当前游戏实机截图

> 截图 = **游戏实机画面**（1080×2160，竖屏）。拍摄于 2026-11-07，
> 场景为教学关 N1（Battle.scene，Y 路线场景 + 程序化天空盒）。
> **画面里的 1012 是已入库的「v11 刀兵形象」**（拍摄时临时换回，工作区那批山贼版已原样还原）。

| 文件 | 内容 | 美术要从中读出什么 |
|---|---|---|
| `C:/Users/steam/Pictures/gptGen/enemy1013_rejected/03_InGameShots/01_battle_enemy_1012_committed.png` | **战斗中的 1012**（敌人 + 手势提示 + 教学字幕 + 完整 HUD） | 角色在**实机尺寸**下长什么样（缩小后还认不认得出兵种）、与地面/HUD 的比例关系、光照与接地阴影 |
| `C:/Users/steam/Pictures/gptGen/enemy1013_rejected/03_InGameShots/02_teaching_text_and_dim.png` | 教学拍（背景压暗 + 字幕气泡） | 压暗态下角色与文字的对比度；**背景被压暗时角色仍然是亮的** |
| `C:/Users/steam/Pictures/gptGen/enemy1013_rejected/03_InGameShots/03_battle_wide.png` | 战斗全景（无字幕） | 战场构图：地面、远景、道具（箱/旗/栅栏）、角色在画面中的位置与占比 |

**从截图里能直接读出的实机事实（给美术定尺寸与风格用）**：

- 相机：竖屏 1080×2160，**透视**；地面占下半屏，角色站在中下部；天空盒是程序化渐变 + 云。
- 角色在屏幕上**高度约 5.7 世界单位**，缩到 1/4 仍要能读出兵种 → **剪影与配色比细节重要**。
- 光照：方向光 + 接地阴影；角色与地面之间有软阴影。
- 战场元素：木质栅栏、破损木箱、红色旗杆、枯草、碎石；远景是山与云。
- HUD：底部有一条血条 + 左下角头像、右上角铜钱、左上角暂停；**中间大片区域是战斗画面**，角色不要指望靠 UI 区分。

---

## 4. 交付物与验收

### 4.1 要做的动作（**不需要 attack**）

| 动作 | 帧数参考 | 母本（`Enemy1013ArtSource/00_Index/videos.csv`） |
|---|---|---|
| Idle | 6 | 候选 1012 idle 源片；旧母本带剑 → **先出一支"持杆 idle 基准"**（§2.8），手部与杆的贴合要目视核对 |
| Walk | 周期 16 | 候选 1012 walk 源片；外挂仅为候选，先验证去剑、握杆 / 遮挡与相对位置稳定 |
| **号令 / 施法（Cast）** | 4–6 | **无母本** → 单独设计（只描述这一个动作）；**语义已按 §2.5 定为「高举战旗 + 呐喊」** |
| HitFlash / HitLeft / HitRight | 3 / 各 1–3 | 候选 1012 受击源片（1012 的 controller 没有 HitRight → 会回落 HitFlash）；是否能换形象并配合持旗需实测 |
| Dead | 6–8 | 候选 1012 dead 源片；须兼顾**旗面倒地**的拟定表现（旗布单独覆写几帧） |
| Launched_Rise / Fall / Getup | 击飞链 | 候选 1012 击飞三支源片；先核对旗杆 / 旗布的连续性（被挑飞时旗杆不得脱手乱飞） |
| ~~Attack~~ | — | **不做**（不攻击） |

### 4.2 制作方式

身份图获验收后，**可尝试**「一张图 + 一个已验收母本」的替换视频工作流：`reference_image` = 新身份图，`reference_video` = 动作母本，动作由母本继承。**Cast 无母本，不套用此法**。

- 1012 母本持剑，1013 双手握长杆；不要假定剑可直接变成旗杆（§2.8）。先试一支持杆 Idle，核对换装、去剑、手部与杆的贴合、杆的长度方向、旗面位置与身份比例；其他动作的复用须逐项验证，不能硬套或批量制作。
- 不把「首尾帧工作流 + 文字描述动作」误当成继承母本；那会重新定义动作（参见 1012 管线教训）。
- 母本里脸可见时可随母本继承表情；**新动作的起手表情必须等于实际验收的 idle 长相**。
- 1012 表情视频的负向词经验仅用于后续需要表情研究的动作视频，不是本轮身份图的唯一负向词限制。
- 避免在已生成结果上反复叠轮而损失锐度；先只做 Idle 并验收，再决定其他动作。

### 4.3 交付物

| # | 交付物 | 路径 / 命名 | 规格 |
|---|---|---|---|
| 1 | 精灵 | `Assets/Sprites/Enemy/Enemy1013/Enemy_1013_<action><n>.png` | 计划为 1108×1108 RGBA、PPU 16；pivot / 落位按 §2.6 实测，不直接继承 1012 |
| 2 | 身份图（归档） | `Enemy1013ArtSource/02_IdentityImages/1013_identity_final_1024.png` | 先保留生成原图；验收后再处理为 1024 绿幕。旧主体高度 / 脚底 / 中心仅作 §2.4 初始参考 |
| 3 | 源片与成片 | `Enemy1013ArtSource/01_SourceVideos/` | 母本 + 成片各一支 |
| 4 | 逐帧台账 | `Enemy1013ArtSource/00_Index/frame-provenance.csv/.md` | 每帧 → 源片帧号 + 落位参数 |
| 5 | Animator | `Assets/Animations/Animator_1013.controller` | 状态见 §2.6（含 `Cast`） |
| 6 | Prefab | `Assets/Resources/EnemyPrefabs/Enemy_1013.prefab` | 目标字段见 §5.5 |

### 4.4 验收清单

**1013 单人身份图**：`1013_identity_v2_stageA_2_6.png` 是用户认为观感尚可的候选，并非最终定稿；先结合 §2.15 的四兵种并排方向和用户反馈，确认造型、题材、体型与持旗识别，再讨论加工归档。

**后续单帧**：正式精灵 Alpha 干净无白边 / 棋盘格；装备完整不超框；同动作内位置 / 脚底锚点 / 尺寸稳定；深色轮廓和像素色簇清晰、有材质厚度，不以固定描边像素或色阶数代替视觉判断；表情符合动作语义，新动作起手表情 = 已验收 Idle 的实际长相。

**后续序列**：动作递进清楚；角色不漂移不缩放、镜头静止；**旗杆 / 旗布不无故增减、不变形、不脱手**（死亡落地若采用，需有明确动作设计和接线）；首尾能衔接（walk 周期 16 为参考）。

**后续游戏内**：与 1012 并排**分得开**、与 1011 并排**不混**；比例以 1011 为准、**高度 ≈ 普通兵 1.25×（量到旗顶）且不横向压到邻列**；号令前摇 0.8s 内能看出"在举旗发令"，被符合规则的直接伤害打断时动作真的停住；受击方向与击飞链都有动画，缺状态时有回落且不报错。

---

## 5. 工程侧附录（给实现者，美术可跳过）

> 2026-11-05 定的机制与工程方案，**不随形象改版而变**。

### 5.0 实施记录（2026-11-07，落地实测）

> 本文主线是美术任务书；本节是**工程侧已落地部分**的记录（合并自 2026-11-05 的《督战型增益单位制作方案》）。

**已落地**（提交 `92952ac0`）：

| 刀 | 内容 | 落点 |
|---|---|---|
| 逻辑层 | 出站攻击倍率状态（`atk_up` 可叠加 / `atk_down` 一次性） | `Assets/Scripts/Enemy/EnemyStatusAtkMod.cs` |
| 逻辑层 | 督战组件（号令计时 / 前摇 / 打断 / 全场加攻 / 死亡减攻） | `Assets/Scripts/Enemy/EnemyCommander.cs` |
| 逻辑层 | 三处伤害乘点 | `EnemyManager.OnEnemyAttackPlayer`（近战）/ `CavalryEnemy.ChargeDamage`（骑兵）/ `Enemy.Attack.SpawnProjectile`（远程，发射时快照） |
| 逻辑层 | "永不攻击"早退 | `Enemy.StateSwitch.StartAttacking()` |
| 逻辑层 | 带来源的受击事件 | `Enemy.OnDamagedWithSource`（`Enemy.Combat.TakeDamage` 内触发） |
| 逻辑层 | 状态 id | `EnemyStatusRegistry.AtkUp / AtkDown` |
| 显示层 | **按用户 2026-11-07 的决定改写**：被加成敌人的**左上角摆玩家的加攻图标 + 层数**（不做描边、不做血条第三根条） | `Assets/Scripts/UI/EnemyAtkBuffMarkerManager.cs`（图标由 `Battle.scene` 的 Inspector 直连 `icon_31_power`） |
| 资产 | `Enemy_1013.prefab`（复制 104 → 删 QTEController → 150 血 / 不攻击 / 经验 150 → 挂 EnemyCommander）+ `Animator_1013.controller`，已注册进 `Battle/EnemyPool.enemyPrefabList` | `Assets/Resources/EnemyPrefabs/`、`Assets/Animations/`、`Assets/Scenes/Battle.scene` |

**Play Mode 实测（2026-11-07）**：

| 验收项 | 结果 |
|---|---|
| 号令生效 | 每次号令全场敌人 +10% ✅（实测 3 层 → 出站倍率 1.30） |
| 可叠加 / 上限 | ✅（5 层封顶，Inspector 参数） |
| 死亡减攻 | ✅（实测 1.30 → **0.975** = 1.3×0.75） |
| 本体招式打断 | ✅（前摇中命中 → 取消本次施放 + 重置为完整周期 4.0s） |
| DoT 不打断 | ✅（灼烧类伤害 → 施放不中断） |
| 永不攻击 | ✅（全程 `Idle`，无攻击动画，`attackDamage=0`，`attackSequence` 空） |
| 跨波不泄漏 | ✅（下一波新生成敌人层数 0 —— 走既有 `StatusHost.ClearAll()`） |
| 标记显示 | ✅（被加成敌人左上角出现加攻图标）；**锚点用 sprite.textureRect 算出的「真正画出来那块」的左上角** —— ⚠️ 不能用 `sprite.bounds`：打包图集的多帧精灵，bounds 含整块图位（1012 实测：图位 1108×1108px = 69.25 世界单位，实际画面 530×609px 且贴在左下角）→ 用 bounds 会把标记甩到美术外面几十个世界单位 |

**未做（仍属别的线）**：

- **波次投放**：没有把 1013 放进任何波次（属关卡配置）——目前只能在 Play Mode 手动生成验证。
- **最终美术**：仍是 104 的临时形象；施法动作仍借放箭 QTE 三段（不放箭）。
- **描边/血条条**：按用户决定，本期用"左上加攻标记"代替。
- **头顶飘字「号令 ×N」**：未做（当前层数只体现在敌人左上的标记上）。

**2026-11-09 增补**：**号令与移动互斥（补齐优先）** —— 原实现里号令全程不碰 `Enemy` 状态机，敌人保持 `Idle` / `isAttackAnimating=false` → 补齐闸门 `Enemy.IsRushMoveReady` 恒开 → **边鼓舞边向前补齐**。修法见 §5.1 不变量 4 / §5.6 M10（只改 `EnemyCommander.cs`，不动 `Enemy` 与调度器）。

### 5.1 四条设计不变量（改代码/改图都不能破坏）

1. **只用既有动词**：威胁必须落在 **{选列 / 换招 / 择时 / 用技能}** 四个动词上 —— 玩家**没有位移动词**（`Assets/Scripts/Player/` 下无 `transform` 位置赋值），任何"需要走位才能解"的威胁都是无解。
2. **不新增移动语义**：正常行军（**不设** `ignoreFillUpOrders`），位移走 `ColumnManager` 既有通道（`design/immutable-constraints.md`）。
3. **不改既有平衡口径**：不加新系统、不改通用伤害公式；倍率只作用在"**出站伤害**"这一层，且只作用于本单位的加/减攻。
4. **号令与移动互斥（2026-11-09 定）**：号令（鼓舞）**不能与补齐移动并发**。
   - 前方出现补齐需求（已派单、待前移）→ **打断正在进行的号令** → 回 Idle → 走 Walk 补齐；
   - 移动中 / 有待执行的补齐订单 → **不开始号令**；
   - 打断口径与「本体伤害打断」一致：取消本次施放（**已落下的加攻不回滚**）+ 重置为**完整**周期；
   - ⚠️ 不许用 `ignoreFillUpOrders` 豁免补齐来实现（与不变量 2 冲突），也不许新增移动通道。

### 5.2 它补的三层缺口

1. **优先级缺口**：现役 4 种"门"（1011 C技霸体 / 1021 盾卫 / 105 远程 / 109 冲锋）都是"打哪个都行"。
2. **敌方增益缺口**：场上所有增益都来自玩家侧（灼烧 / 疾病 / 冻结 / 连击 Buff），敌方没有任何"让局面变糟"的能力。
3. **"何时杀"的决策缺口**：把"先打谁"变成**时机成本**（号令层数）vs **时间成本**（去后排点它）。

**它是"硬门"**：活着 → 所在逻辑排永不清空 → 后排不前移 → 波次也无法结束 ⇒ 玩家必然要处理它。

### 5.3 伤害链：出站倍率要乘在哪三处

| 伤害来源 | 结算路径 | 乘点 |
|---|---|---|
| 近战 | `Enemy.Attack.PerformAttack()` → `EnemyManager.OnEnemyAttackPlayer(enemy)` → `PlayerState.TakeDamage` | 在 `OnEnemyAttackPlayer` 里算 `enemy.attackDamage × 出站倍率` |
| 骑兵冲锋 | `CavalryEnemy.ChargeDamage` = `chargeDamage × 冲锋格数 × 波次倍率` | 在 `ChargeDamage` 属性里乘（**独立伤害源，不改则 109 完全不吃增益**） |
| 远程箭矢 | `Enemy.Attack.SpawnProjectile()` 把 `attackDamage` 作为**快照**传出 | 发射时快照一次出站倍率 |

> `Enemy.attackDamage` 在 `Enemy.Init.ApplyWaveScaling()` 里已乘过波次 `damageMultiplier` → 出站倍率必须乘在它**之后**。BOSS 走同一条近战链，但被排除在加/减攻之外。

**打断与霸体口径**：敌人前摇被打断的判定在 `Enemy.Combat.TakeDamage()`：
`if (!inArmorWindow) CancelAttack(); else if (canInterruptCFrame || isHeavyAttack) CancelAttack();`（`inArmorWindow = isCFrame || isSuperArmor`）。
1013 不设 `isCAttack`、不是 BOSS ⇒ **无霸体 ⇒ 任何直接伤害都能打断它的前摇**。
"只有玩家攻击本体算数"的样板：`if (poiseDamage > 0f && inArmorWindow && feedbackSource == HitFeedbackSource.BasicAttack)`（来源枚举见 `Assets/Scripts/Data/HitFeedbackEnums.cs`，共 10 种）。

### 5.4 临时形象方案（工程跑通用，正式形象见 §1–§4）

| 项 | 做法 |
|---|---|
| 形象 | 复用 `Enemy_104.prefab` 的 `BOSS_idle`（32×32）、`localScale=0.18`、`visualYOffset=1.035`（屏幕高 5.76 单位 ≈ 1012 的 5.76）。**仅临时占位**：正式形象（§2.3 旗手）高 ≈ 普通兵 1.25×，占位期间不体现体量差异 |
| 通用动作 | 复用 `Assets/Animations/Boss_104.controller`；建议复制为 `Animator_1013.controller` |
| 施法动作 | 复用放箭 QTE 三段 `QTE_TripleStab_Start`(0.50s) / `_Loop`(0.30s) / `_End`(0.30s)（`animator.Play(状态名)`，非 trigger） |
| 施法时序 | t=0 预警 0.3s → t=0.3 播 Start → **t=0.8 施放点**（施加加攻）播 Loop → t=1.1 播 End → t=1.4 回 Idle |

> **已核对**：这四个 clip 的 AnimationEvent 数均为 0 → 复用不会触发事件；箭矢由 `QTEController` 生成，1013 不挂它 → **不会放箭**。
> **两处已知缺口**：104 没有 `HitLeft` / `HitRight`（回落 HitFlash）、也没有 `Launched_Getup`（被挑飞后起身无动画，只报 Animator 警告）。

### 5.5 预制体字段（1013 目标值）

| 字段 | 目标 | 理由 |
|---|---|---|
| `enemyName` | 旗手（名可改） | 显示名 |
| `maxHealth` | 150 | 普通单位体量 |
| `attackDamage` | **0** | 配合"永不攻击"早退 |
| `attackSequence` | **清空** | 无攻击序列 |
| `isCAttack` | false | 无霸体 → 前摇可被打断 |
| `isBoss` | false | 不进 QTE、不吃加/减攻 |
| `ignoreFillUpOrders` | **不勾** | 必须正常行军 |
| `visualYOffset` / `localScale` | 按最终精灵实测重算 | 体型要与 1012 相当 |
| 组件 | 删 `QTEController`；加 `EnemyCommander`（X / 上限 / Y / 周期 / 首施前置 / 前摇） | |

**两个必做接线**：
1. `Assets/Scenes/Battle.scene` → `Battle/EnemyPool` → `enemyPrefabList`（**当前 13 项，Inspector 引用，不再扫目录**）→ 漏了运行时会 fallback 到临时 Cube。
2. 组件必须在**每次 `OnEnable`** 重新订阅 `enemy.OnDeath`（回池时 `ResetEnemy` 会把 `OnDeath = null`）。

### 5.6 技术改动清单（M1–M9）

| # | 文件 | 改动 |
|---|---|---|
| M1 | `Enemy.cs`（Init 段） | 缓存 `_commander` 组件引用（与 `_cavalry` / `_shieldGuard` 同模式） |
| M2 | `Enemy.StateSwitch.cs` → `StartAttacking()` | 开头早退 `state = EnemyState.Idle; return;`（复用骑兵分支写法；**已核对**行军完成分支独立调用 `CompleteRushMoveOrder()` → 不会卡住补齐队列） |
| M3 | `EnemyManager.cs` → `OnEnemyAttackPlayer()` | 伤害 = `enemy.attackDamage × 出站攻击倍率` |
| M4 | `CavalryEnemy.cs` → `ChargeDamage` | 同乘出站倍率 |
| M5 | `Enemy.Attack.cs` → `SpawnProjectile()` | 发射时快照一次出站倍率 |
| M6 | `EnemyStatusRegistry.cs` | 新增状态 id 常量 |
| M7 | `EnemyHealthBar.cs` + `Enemy.Visuals.cs` + `EnemyOutline.OutlineFacts` | ① 血条第三根状态条（加攻）+ 层数文本；② 新增 `AtkBuffed` 描边事实位 |
| M8 | `Enemy.cs` + `Enemy.Combat.cs` | 新增 `OnDamagedWithSource`（带 `HitFeedbackSource`）→ 区分"谁能打断号令" |
| M9 | `EnemyCommander.cs` | 订阅 M8：前摇被打断 → 取消施放、重置完整周期、回 Idle |
| M10 | `EnemyCommander.cs` | **补齐优先于号令**（2026-11-09）：有前移订单（普通补齐 / 新波次入场）→ 打断正在进行的号令 → 回 Idle → 走 Walk 补齐；移动中 / 有待执行订单 → 不开始号令。打断与「本体伤害打断」共用同一函数 `InterruptCast(reason)`（重置完整周期 + 回 Idle）；号令在 `Enemy` 状态机之外，所以由督战组件自己判、自己断，不改 `Enemy` / 调度器 |

**倍率存放（选 B）**：`EnemyStatusAtkMod : IStatusEffect` 挂在 `StatusHost`，自带层数，UI 走 `EnemyStatusRegistry` 查询；回池由**既有** `Enemy.Pool.cs:75` `_statusHost?.ClearAll()` 统一清理。
**公式**：`最终倍率 = (1 + 加攻层数 × X) × (1 − 减攻层数 × Y)`（减攻只有一层）。
⚠️ `Enemy.StatusHost` getter 每次访问会跑 `EnsureResidentStatusRegistered()`（只补登记"冻结"）→ 加攻/减攻必须**每次施放/结算时重新 Add**。

**"在场"枚举写法**：

```csharp
var list = EnemyManager.Instance.GetAllAliveEnemies();   // ⚠️ 返回内部列表引用
for (int i = 0; i < list.Count; i++)
{
    var e = list[i];
    if (e == null || e == self || e.isBoss || e.state == EnemyState.Dead) continue;
    // 施加加攻
}
```

> **遍历期间不得修改该列表**；频率每 4s 一次、开销可忽略。

### 5.7 对标参考

| 参考 | 可直接用到的点 |
|---|---|
| `design/blades-gameplay-benchmark.md` | ① 敌人能力 = **数据引用**（`_knownAbilities`）⇒ "号令"宜做成能力表条目而非写死组件；② **表现三段解耦**（施法/弹道/命中）⇒ 我们只有施法段；③ 重攻击/破防是两条显式成长线 ⇒ "用重攻击打断号令"这条对策才成立 |
| `reference/game-design-references.md` | L4D2 的"**特殊敌人专属音效提示**" ⇒ 1013 出场加一声号角（或举旗时一记旗展破空声）比再加 UI 省事；Sekiro 的"破防 → 处决" ⇒ 未来做优先目标收益时参考 |

### 5.8 波次编排

**不在本次范围**（属关卡配置工作）→ 做完机制与形象也不会自动出现在任何关卡里。
