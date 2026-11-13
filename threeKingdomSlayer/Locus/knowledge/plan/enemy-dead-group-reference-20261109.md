---
id: kd_982cc7f3-66a7-4505-b048-e4fd90213f71
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# Dead / 受击通用动作 · 并排组图路线（2026-11-09）

> **状态**：**已于 2026-11-09 16:17:35 按 §4 方案提交并出图**（一次提交、无重试、HTTP 200、125s），结果、量测与偏差见 **§10**；成图**待用户目视验收**。本文 §4 的提示词与参数**不要再重复提交**。
>
> **本轮新结论**：1013 与 1051 的 Dead 连续不合格，根因是**新角色的姿态靠文字重建**，与工程内已部署的 Dead 家族脱节；改走 **「已部署 Dead 帧 + 新角色并排组图」**，让差异在同一张图里直接可见（沿用 Idle 并排组图的成功经验）。

相关文档：
- `plan/enemy1013-art-handoff-20261108.md`（1013 美术线交接，Dead 轮作废记录）
- `plan/enemy1051-remaining-animation-art-plan.md`（1051 剩余动作方案，§4.7 Dead 全套与 §4.7.6 四版作废）
- `memory/enemy1012-art-replacement-pipeline.md`（管线级经验）、`memory/enemy-proportion-lineup-reference.md`（并排组图成功经验与坑）
- `memory/user-preference.md`（用户铁律）、`memory/art-production-workflow-entry.md`（S0–S5 流程与成本控制）

---

## 0. 当前状况（截至 2026-11-09 15:48，实测）

| 角色 | 已验收 | 未做 | 被否产物 |
|---|---|---|---|
| **1013 旗手** | 形象 / Idle 6 帧 / Cast 12 帧 / Walk 6 帧（`Enemy1013ArtSource/`），**未接入 Unity** | Dead、受击三套、击飞三支 | `C:/Users/steam/Pictures/gptGen/enemy1013_rejected/03_DeadPose_v1/`（末帧原图 + 绿幕合成图）；提示词 `Tools/visual_brief/prompts/1013_identity_deadpose1_REJECTED.*`、`1013_dead_v1_REJECTED.*` |
| **1051 弓手** | 形象 / idle 6 帧 / walk 6 帧 / attack 6 帧（`Enemy1051ArtSource/`），**未接入 Unity** | Dead、受击三套、击飞三支 | `C:/Users/steam/Pictures/gptGen/enemy1051_rejected/03_DeadPose_P3/`（v1–v4 原图 + 绿幕合成图）；提示词 `Tools/visual_brief/prompts/1051_pose_deadp3*.txt` |

**关键事实（避免下个会话误判）**

1. **1013 没有生成过 Dead 视频**：磁盘 `C:/Users/steam/Videos/doubaoVideo/` 里只有 `1013_cast_*`、`1013_idle_base_v1`、`1013_walk_v1`；1013 只有**一张被否的末帧图**和一份作废的视频词。
2. **1051 也没有生成过 Dead 视频**：被否的只有四张 **P3 姿势图**；`1051_dead_keyframes_6s.mp4` 母片从未创建，视频步骤从未执行。
3. 因此前几轮的全部问题都发生在**静态姿势图**阶段；视频路线尚未被验证过。
4. Unity Editor 当前状态为 **crashed**（`Logs/Editor.log`）；本轮是文档与方案工作，不涉及 Unity。
5. **2026-11-09 16:17:35 已按 §4 方案提交并出图**（一次提交、无重试、HTTP 200、125s）→ 成图与量测见 **§10**；成图待用户目视验收，**不要重复提交同一请求**。

---

## 1. 前几轮为什么不合格（分析结论）

### 1.1 1013（一张末帧图被否）

- 提交的词写的是「**仰面平躺**、双手空、**旗杆脱手平躺在身侧**、**旗布摊平在地上**、整个人低低躺在画面下方」——**目标本身就画错了**：工程内 1011/1012 的 Dead 即使到末帧也保持后仰、屈腿、四肢张开、武器离手的剪影，不是工整贴地平躺。
- **道具关系被改坏**：已验收资产里旗是**系在杆顶、垂挂在杆的右侧**；被否图里杆躺在身侧下方、旗布铺在地上成了「身体旁边另一块平铺的布」。
- **提交前自身就不一致**：`Tools/visual_brief/prompts/refs_1013_identity_deadpose1_REJECTED.json` 标题写「**杆不脱手**」，而实际提交词写「双手空、杆已脱手」。这属于提交前目标冲突，不能归咎于模型。
- **没先看工程内已部署帧**：照母任务书 §2.5 的字面写；§2.5 该条已于 2026-11-09 作废。

### 1.2 1051（四张 P3 姿势图全否）

| 版 | 用户反馈 | 实际错在哪 |
|---|---|---|
| v1 | 非正面、读成「躺卧」、没有后倒感；毫无痛苦表情 | 身体一条水平直线 → 读成躺卧；脸照抄身份图怒视 |
| v2 | 完全没后倒感 | 改成斜立但仍是直线、脚没甩起 |
| （中途） | 「去找姿态参考」 | 外网检索对本项目基本无效（1013 两次检索全返杂志封面/广告） |
| v3 | 双手像两个大张开的蜘蛛爪朝镜头伸；构图要修 | 提示词本身要求张手、手指展开；身体斜穿画布对角线、边距太紧 |
| v4 | 构图不对，和 1013 一样的问题 | 身体被拉成平板、浮空过高、弓仍在身体右下 |

### 1.3 共同根因（两条）

1. **姿态没有锚定**：1051 四版**只上传身份图**，动作参考只由制作方「读过再翻译成文字」；模型看到的是站立弓手，被击飞姿态完全靠文字重建 → 每版都在姿势、构图、道具位置之间换来换去。
2. **道具与人物的相对关系没有锁死**：家族里脱手的武器飞向**自己那一侧的上方**（1011/1012 实测 dx ≈ −330、dy ≈ −150~−220），1051 四版的弓全在身体右下；1013 的旗与杆直接脱钩。
3. **补充根因**：v3/v4 的提示词里混入了「几乎水平，只略微倾斜」这类**写死的极端措辞**，直接造成「平板 + 浮空」；同时构图百分比是拍脑袋估的（「头顶 1/4、鞋底 3/4」），没先量工程内已部署帧。

---

## 2. 交接文档里需要纠错的数字（2026-11-09 复测）

复测对象：1051 四张被否原图（1024²，透明底）、1011/1012 已部署 dead3/dead6（1108²）。口径：**alpha ≥ 128 二值化 → 4 邻接 BFS 连通块 → 身体块 = 去掉武器块后的最大块；质心按像素加权**。

| 项 | 文档记载 | 本次复测 | 说明 |
|---|---|---|---|
| 1051 v4 宽高比 | 1.995 | **1.652**（身体块）/ 1.995（含弓的整体包围框） | 文档把「人物 + 弓」当成身体块了。**1.65 仍然偏扁**，但不能按 1.995 判「平板」 |
| 1051 四版弓的位置 | 「全落在身体右下方（dy 为正）」 | **v1 dy +201、v3 +224、v4 +44（右下）；v2 dy −84（右上）** | v2 的弓其实在右上，仍然被否 → **弓的位置对了也不足以救动作** |
| R3 离地 | 1108² 下 17–54px；姿势图 1024² 下「底边离地线约 20–60px」 | 同源换算：17–54px（1108²）→ 约 **16–51px**（1024²） | 数值本身可用，但**必须先声明画布尺寸与地线**，否则换算会错 |
| 修正规格 P3 | 身体块底 ~778（76%）、地线 895 | 差 **117px**，与同节「离地 20–60px」自相矛盾 | 该表**暂不可直接当验收线** |
| R1 身体块顶 | 27–31% | 同表列出的 1012 dead6 身体块顶实为 **38%** | 不同阶段不可套同一范围；1012 dead6 属后段躺平帧 |
| 已部署帧实测复核 | 1011 dead3 身体块 1.339 / 顶 27% 底 76% / 离地 54px | **1.339 / 302–841 → 顶 27.3% 底 75.9% / 离地 54px** | ✅ 与文档一致 |
| 已部署帧实测复核 | 1012 dead6 身体块 1.550 / 顶 38% 底 79% / 离地 17px | **1.550 / 417–878 → 顶 37.6% 底 79.2% / 离地 17px** | ✅ 与文档一致 |

> **结论**：已部署帧那张表（1011 dead3/dead6、1012 dead3/dead6）是可信的；**问题出在 1051 的修正规格表**（P3/P4/P5 那几行）与「四版弓都在右下」这句概括。

---

## 3. 决策：改走「并排组图」路线（用户 2026-11-09 同意）

### 3.1 为什么组图更合理

- Idle 并排组图已被验证有效（用户「效果不错」）：**把差异摆进同一张图，偏差一眼可见**，不必靠文字互相猜。
- 本次有比 Idle 更好的条件：**已有角色的 Dead 帧就是现成的动作母本**（1011/1012 各 6 帧已部署），不需要外部检索。
- 1013/1051 的失败本质是「姿态靠文字重建」。组图让模型**直接看到被打横的姿态**，把姿态锚定从「生图阶段靠词」前移成「有图可照」。

### 3.2 这一张只画一个动作节点：**腾空被打横**（对应 `dead3`）

理由：分歧最大的就是这一拍——1013 被否成「平躺」，1051 被否成「没被打横」。落地/终态那一拍（`dead6`）等这张过了再单独出图，不混在一张里。

### 3.3 布局：2×2 四格，单一动作节点

| 格 | 角色 | 提供的参考 | 在本图中的角色 |
|---|---|---|---|
| 左上 | 1011 重甲刀兵 | `Enemy_1011_dead3.png` | 动作与画风家族基准（尽量照原样重画） |
| 右上 | 1012 山贼刀兵 | `Enemy_1012_dead3.png` | 同一动作的第二例，确认家族一致性 |
| 左下 | **1013 旗手** | `1013_cast_anchor_green_1080x1080_v6.png` | 新角色：只换姿势，形象/杆/旗照锚图 |
| 右下 | **1051 弓手** | `1051_idle_identity_greenscreen_1024.png` | 新角色：只换姿势，形象/弓/箭袋照身份图 |

> 2×2 而不是 1×4：四人横躺 + 1013 长旗杆，一排排会让每个角色太小、姿势看不清。1536×1024 下每格约 768×512，够看剪影、道具与头脚方向。

---

## 4. 生成方案（可直接照做）

### 4.1 请求参数

| 项 | 值 |
|---|---|
| 接口 | `POST https://api.muskapis.com/v1/images/edits`，multipart **四张 `image[]`**，顺序固定（见 4.2） |
| 模型 | `gpt-image-2.6-sunburst`（**提交前先 `GET /v1/models` 复核当日可用图像模型**） |
| 尺寸 | `1536×1024`（该尺寸在本链路实测被接受过） |
| 质量 | `quality=max` + `input_fidelity=high`（编辑类） |
| 其他 | `output_format=png`、`n=1`、**不传 `background`** |
| 超时 | 显式放宽（≥600s）。五图组图请求曾读超时 901s 与 HTTP 504 各一次；**同一重请求连续失败两次就换轻量路线，不要第三次重发**（图像接口同步、丢回执＝丢产物，无 resume） |
| 留档 | `C:/Users/steam/Pictures/gptGen/enemy_dead_group_v1/`（原图 + `prompt.txt` / `prompt_cn.txt` / `prompt_negative.txt` / `request.json` / `response.json` / `status.json`） |

### 4.2 参考图清单与顺序

| 序 | 文件（相对工程根） | SHA-256 | 职责 |
|---|---|---|---|
| 1 | `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_dead3.png`（1108²，透明底，298,645 B） | `1da28cd9c745e86d…f58966da40` | 动作 + 画风基准：身体被打横、腾空、剑脱手飞向画面左上 |
| 2 | `Assets/Sprites/Enemy/Enemy1012/Enemy_1012_dead3.png`（1108²，透明底，382,115 B） | `c647a5a734b4a19c…34c58a881f` | 同一动作的第二例，确认家族一致 |
| 3 | `Enemy1013ArtSource/02_IdentityImages/1013_cast_anchor_green_1080x1080_v6.png`（1080² 绿幕，317,542 B） | `fed405d071357f20…2e607d34b` | 1013 形象唯一真源：脸、头巾护额、络腮胡、布袍甲片、皮带、护腕、绑腿战靴、素木杆顶金属帽、暗赭红方旗 + 金回纹边框、比例 |
| 4 | `Enemy1051ArtSource/02_IdentityImages/1051_idle_identity_greenscreen_1024.png`（1024² 绿幕，470,600 B） | `f40bd12e53b3ac77…28cee006ca4` | 1051 形象唯一真源：发髻、灰头带、蒙面布巾、怒视眉眼、麻衣、皮背心、护腕、带扣、暗红布条、裤、绑腿、绑脚鞋、右肩箭袋三支白羽箭、左手木弓 |

四张合计 **1,468,902 B ≈ 1.4 MiB**（比之前失败的 2.2 MiB 五图请求轻）。

### 4.3 English positive prompt（实际提交内容建议＝本段）

```text
Create one pose-comparison sheet for a high-definition pixel-art Three Kingdoms action game. Four separate full-body game sprites are drawn in a 2x2 arrangement on one flat chroma-green background (RGB 0,159,62), one sprite per quadrant, with clear empty green space between the four of them and none of them cropped.

ALL FOUR SPRITES SHOW THE SAME SINGLE MOMENT: the instant after a fatal hit, when the body has been knocked sideways through the air, already turned almost horizontal, feet off the ground, limbs thrown open by the force. This is one beat, not a sequence, not a storyboard, not four different stages.

Top left: the armored soldier of image 1. Reproduce him as he is in image 1: the same armor, the same helmet, the same red cloth, the same sword already separated from his hand and flying away at the upper LEFT of the frame with a clear gap between the blade and his body. His body lies almost horizontal, head toward the right side of the frame, feet toward the left, chest still turned toward the viewer, and the lowest point of his body is lifted clear of the ground.

Top right: the bandit swordsman of image 2, in that same beat and with the same read as image 1: body almost horizontal, head to the right, feet to the left, chest toward the viewer, body lifted clear of the ground, his sword already separated from his hand and flying away at the upper LEFT of the frame with a clear gap.

Bottom left: the standard-bearer of image 3 in that same beat. Keep him exactly as image 3: the same face, the same dark cloth headband and studded leather brow band, the same tied hair knot, the same square jaw and full beard, the same cream cross-collar cloth robe, the same few dark blue-grey shoulder and chest armour plates, the same dark brown leather belt with its square buckle, the same thick bracers, the same dark brown trousers, the same pale leg wraps, the same heavy boots, the same plain wooden pole with its small metal cap, the same dark vermilion square flag with its gold meander border, the same pixel-art rendering, the same colors and the same body proportions. Only his pose changes. His body is knocked sideways through the air and lies almost horizontal, head toward the right side of the frame, feet toward the left, chest still toward the viewer, his lowest point lifted clear of the ground, and both arms thrown open by the force.

His pole has left his hands and is tumbling through the air at the upper LEFT of the frame, the same way the two swords fly above him. The square flag is still tied to the top end of that same pole and travels with it: the cloth is never detached from the pole, never a separate flat piece lying on the ground, and it keeps the same side of the pole as in image 3.

Bottom right: the archer of image 4 in that same beat. Keep him exactly as image 4: the same topknot, the same grey cloth headband, the same grey scarf over the lower half of his face, the same angry narrowed eyes, the same coarse grey tunic with the torn hem, the same worn dark brown leather vest, the same leather bracers, the same square belt buckle, the same short dagger at his belt, the same red cloth strip, the same dark brown cropped trousers, the same pale leg wraps, the same strapped sandals, the same quiver with three white fletched arrows over his right shoulder, the same wooden bow with its wrappings and its red band on the grip, the same pixel-art rendering, the same colors and the same proportions. Only his pose changes. His body is knocked sideways through the air and lies almost horizontal, head toward the LEFT side of the frame, feet toward the right, chest still toward the viewer, his lowest point lifted clear of the ground.

His bow has just left his left hand and is tumbling through the air at the upper RIGHT of the frame, on the same side as the bow in image 4, with a clear gap between the bow and his body. There is no arrow in either hand and no loose fletching anywhere in the frame; the quiver on his shoulder still holds exactly the same three white fletched arrows.

All four bodies are drawn in the same silhouette language: the back arched into a curve rather than a straight line, the chest opened toward the viewer, the head thrown back with the chin up, the legs bent and lifted so the boots are clearly higher than the hips, the arms thrown open and asymmetric, one up and one down, and the hands relaxed, small and half closed, never splayed like claws and never reaching out toward the viewer.

Framing for all four: each figure fits inside its own quadrant with a clear green margin on every side, the whole body plus its weapon fully visible, nothing cropped and nothing touching the edge of the picture. Each body occupies the middle band of its quadrant, with the top of the head at about 30% of the canvas height and the lowest point of the body at about 75% of the canvas height, so the four bodies sit at the same height and can be compared directly. The four characters are drawn at a comparable body size so that the silhouettes can be compared.

Rendering: the same high-definition pixel-art rendering as the reference images, clean dark contours, clustered light and shadow planes, solid volumes in cloth, leather and wood. One flat chroma-green background (RGB 0,159,62) behind all four, no floor, no ground line, no shadow, no scenery, no text, no labels, no numbers, no UI, no watermark, no checkerboard.
```

### 4.4 中文正向提示词（存档用，便于审阅）

```text
为高清像素化三国动作游戏制作一张姿态对照图。四个独立的全身游戏精灵以 2×2 排列画在同一张纯绿幕底（RGB 0,159,62）上，每格一个精灵，四者之间留出明显的绿色空隙，任何一个都不得出画。

四个精灵画的是同一个瞬间：致命一击刚打中、身体被横向打飞、已经几乎被打横、双脚离地、四肢被力量甩开的这一拍。这是一拍，不是一组连续动作，不是分镜，也不是四个不同阶段。

左上：图 1 的重甲兵。尽量照图 1 原样重画：同样的重甲、头盔、红布，他的剑已经脱手，飞在画面左上角，刀身与身体之间有明显空隙。他的身体几乎横躺，头在画面右侧、双脚在左侧，胸口仍朝向观众，身体最低点离开地面。

右上：图 2 的山贼刀兵，与图 1 同一拍、同一种读法：身体几乎横躺，头右脚左，胸口朝观众，身体离地，剑已脱手飞向画面左上角，与身体之间有明显空隙。

左下：图 3 的旗手，同一拍。人物完全照图 3：同一张脸、同一条深色布头巾与带铆钉皮质护额、同一束发髻、同一副方下颌与络腮胡、同一件米色交领布袍、同样那几块深蓝灰肩胸甲片、同一条带方形带扣的深棕皮带、同样厚实的护腕、同样的深棕裤、同样的浅色绑腿与厚底战靴、同一根杆顶带小金属帽的素木杆、同一面暗赭红方旗与金回纹边框、同样的像素画风、配色与身体比例。只有姿势改变：身体被横向打飞、几乎横躺，头在画面右侧、双脚在左侧，胸口仍朝观众，身体最低点离开地面，双臂被力量甩开。

旗杆已经脱手，在画面左上方翻飞，与上方两把剑的飞法一致。方旗仍然系在**同一根杆的杆顶**、随杆一起飞：旗布绝不与杆分离，绝不是单独平铺在地上的一块布，仍然在杆的同一侧。

右下：图 4 的弓手，同一拍。人物完全照图 4：同一发髻、灰头带、遮住下半脸的灰布巾、同一双怒视细眼、同一件粗麻短衣与破旧深棕皮背心、皮革护腕、方形带扣、腰间短刀、暗红布条、深棕束裤、浅色绑腿、绑脚鞋、右肩箭袋与三支白羽箭、同一把木弓与弓柄红带、同样的像素画风、配色与比例。只有姿势改变：身体被横向打飞、几乎横躺，头在画面**左侧**、双脚在右侧，胸口仍朝观众，身体最低点离开地面。

弓刚从他左手脱落，在画面**右上角**翻飞，与图 4 中弓所在的一侧相同，与身体之间有明显空隙。两只手里都没有箭，画面上也没有散落的箭羽；肩后箭袋仍是同样三支白羽箭。

四个身体用同一套剪影语言：背部弓成弧线而不是一条直线，胸口打开朝向观众，头后甩、下巴朝上，双腿屈起、鞋底明显高于臀部，双臂不对称地甩开（一上一下），手放松、半握、保持与身份图相同的小尺寸，绝不成张开的爪子，绝不朝镜头伸。

四个格子的取景一致：人物完整在自己的格子里，四周留明显绿色空隙，身体与武器全部可见，不出画、不贴边。身体都落在画布中间高度带：头顶约在画布 30% 高度、身体最低点约在 75% 高度，四个身体高度一致以便直接比较，四个角色画成大体相同的体量以便比较剪影。

渲染：与参考图一致的高清像素画风，干净的深色轮廓，成簇的明暗色块，布、皮革、木头有实打实的体积感。四个精灵共用一张纯绿幕底（RGB 0,159,62），没有地面、没有地平线、没有投影、没有场景、没有文字、没有编号、没有 UI、没有水印、没有棋盘格。
```

### 4.5 负面提示词

```text
fewer than four sprites, more than four sprites, extra person, duplicate character, two standard-bearers, two archers, second bow, second sword, second pole, second flag, flag detached from the pole, flag lying flat on the ground as a separate cloth, pole standing upright, spearhead on the pole tip, drum, shield on the archer, arrow in the hands, loose fletching, empty quiver, cropped head, cropped feet, missing hands, missing boots, extra limbs, extra fingers, splayed claw hands, hands reaching toward the viewer, straight horizontal body, body lying flat on the ground, standing pose, kneeling pose, crouching pose, feet touching the ground, body much smaller than the others, body much larger than the others, side view, three-quarter view, back view, perspective depth, different sizes per quadrant, text, labels, numbers, watermark, checkerboard, scenery, ground plane, cast shadow, motion blur, glow, spark, dust, blood, photorealism, 3D render, anime, cartoon, chibi
```

> 负面词长度沿用**并排组图**的做法（图生图用长列表）；「只留四条」那条规则适用于**表情 / 动作弧线类视频词**，不适用于本图。

### 4.6 提交前的可选预处理

1. **裁掉大片空白**：1011/1012 两张是 1108² 里躺着一个角色，空白很大。裁到内容 bbox（四周留 40–80px）可减小载荷、也更容易让模型看清姿态。脚本口径与 `Tools/visual_brief/make_contact_sheet.py` 一致即可。
2. **先 `--dry-run` 再提交**：沿用 `Tools/visual_brief/submit_enemy_proportion_lineup.py` 的 `--dry-run` / `--approve` 习惯（该脚本本身是并排图专用规格，**不要直接改它当 Dead 组图用**，照抄它的「顺序 + 哈希 + 留档 + 一次提交不重试」流程即可）。
3. **提交前复核 `GET /v1/models`**：确认当日图像模型只有 `gpt-image-2.6-sunburst` 还是新增了别的。

### 4.7 保存与留档

- 原图与请求记录存 `C:/Users/steam/Pictures/gptGen/enemy_dead_group_v1/`（**工程外**，不进版本库）。
- 记录：实际尺寸、格式、输出 SHA-256、四张输入的 SHA-256 与顺序、完整中英提示词、模型、usage、HTTP 状态与耗时。
- 回执里的 `image_tokens=0` **不能**用来判断参考图是否被接收；四张图是否真按职责被采用，只能靠成图目视判断。

---

## 5. 两个待用户拍板的决定（默认已写进 4.3 / 4.4 的词里，改口径就要同步改词）

| # | 决定 | 默认（本方案采用） | 备选 |
|---|---|---|---|
| 1 | **1051 被打横的方向** | 头在画面**左**、脚在右、弓飞向画面**右上**（因为弓在画面右 → 身体往另一侧倒，弓不跨过身体；与「武器在哪一侧，身体往另一侧倒」一致） | 头右脚左 → 则弓必须跑到画面左侧，与「弓在左手」矛盾，等于把 1013 的错再犯一次。**建议不要** |
| 2 | **1013 的旗杆是否脱手** | **脱手**：杆在画面左上翻飞，**旗仍系在杆顶并随杆一起飞**（既满足家族「武器飞向自己一侧上方」，又满足「旗与杆相对关系不可改」） | 不脱手：双手仍握杆、杆随人一起倒下、旗仍系在杆顶。也可，但与家族武器飞出的读法不同 |

> 这两条在 `plan/enemy1051-remaining-animation-art-plan.md` §4.7.6 里被记为「需用户拍板」，至今未定。本图按默认写。

---

## 6. 出图后的量测与验收

### 6.1 本地量测（先做，再交用户）

用 4 邻接 BFS + alpha ≥ 128 分连通块，量四个格子里的：身体块 bbox / 宽高比 / 顶底占画布百分比 / 离地量，以及道具块相对身体块质心的 dx、dy。

- **对照基准 = 工程内原始帧**（1011/1012 dead3：身体块 1.30–1.34、顶 27–30%、底 76–79%、离地 17–54px；武器 dx ≈ −330、dy ≈ −150~−220）。
- ⚠ **图里的 1011/1012 会被模型重绘**，其数值只作参考；判定「是否同族」必须对照 `Assets/Sprites/Enemy/.../Enemy_1011_dead3.png` 等原始文件。
- 量测时必须写清**画布尺寸、地线、是否排除武器块**，避免重演第 2 节那种口径混淆。

### 6.2 用户目视验收（顺序）

1. **是否同族**：四个身体的后仰弧线、头脚方向、屈腿、手臂甩法是不是同一套动作语言。
2. **1013**：旗是否仍系在杆顶？杆旗与身体的空间关系是否与已验收资产一致（不再是「旁边一块平铺的布」）？
3. **1051**：弓是否在**自己那一侧的上方**、与身体有明显间隙？手里与画面上有没有多余的箭？蒙面布巾与怒视眉眼是否保持？
4. **画面与风格**：四人是否都被画成同一套像素语言（单张渲染容易把脸磨平，参见 `memory/enemy-proportion-lineup-reference.md` 教训三）？
5. **出框与边距**：四个人是否都在各自格子里、四周留白、不贴边？

### 6.3 边界（务必说清）

- 这张图是**姿态研究图**，不是精灵、不是动画、不是最终素材；**不能代表 1013/1051 的 Dead 动画已经成立**。
- 组图里的 1011/1012 只能辅助比较，**不得裁下来当游戏素材**（并排组图裁切进游戏有实测硬伤：旗杆 1.53×、画布装不下、旗宽 1.57 列，见 `memory/enemy-proportion-lineup-reference.md`）。

---

## 7. 组图通过之后的落地路径（顺序，供下个会话接）

1. **裁出 1013 / 1051 面板** → 合成 1024² 底件（`Tools/visual_brief/compose_identity_from_alpha.py` 或 `compose_identity_greenscreen.py`，绿幕口径 1024² / 绿 (0,159,62) / 主体高 696 / 脚底 895 / 中心 474.5）。
2. **单图编辑精修成 P3 姿势图**：底件取自**组图裁切件**（`memory/enemy-proportion-lineup-reference.md` 教训五：拿并排图裁切件当底，才不会画风漂移）。沿用 `Tools/visual_brief/submit_1051_pose.py` 的路线，只上传身份图 + 需要的参考。
3. **母片五帧**（P1 站立 → P2 挨打后仰 → **P3 腾空被打横** → P4 落地 → P5 躺平终态），`Tools/visual_brief/make_keyframe_clip.py`，每拍 ≥1.0s、总 6.0s。
4. **seedance 视频**（`Tools/visual_brief/submit_seedance.py --workflow replace`），词用 `plan/enemy1051-remaining-animation-art-plan.md` §4.7.1 的已备稿（**动作 landmark 三句 + 错时因果链 + 尾段阻尼**），但把「纯文字描述姿态」的部分改成「照 P1–P5 母片」。
5. 抠图（`Tools/visual_brief/chroma_key.py`，T=79）→ 选帧（按动作相位）→ 落位（固定 k + 统一偏移）→ 写精灵（只覆盖 PNG 字节、保留 .meta）→ 回读核对。
6. **1013 特有问题**：旗杆与旗面「整张一起动」（用户 2026-11-08 定），所以每支动作都是一次完整生成，母本帧不能直接复用。

---

## 8. 可直接粘贴到新对话的开场语

```text
这是「三国杀戮」Unity 工程的 Dead 动作美术线交接。请先读这几份再动手：
- Locus/knowledge/plan/enemy-dead-group-reference-20261109.md（主文档：现状、前几轮失败根因、纠错后的实测数据、并排组图生成方案与完整中英提示词）
- Locus/knowledge/plan/enemy1051-remaining-animation-art-plan.md（§4.7 Dead 执行卡、§4.7.6 四版作废记录）
- Locus/knowledge/plan/enemy1013-art-handoff-20261108.md（1013 已验收资产与 Dead 轮作废记录）
- Locus/knowledge/memory/user-preference.md（用户铁律）与 memory/art-production-workflow-entry.md（S0–S5 流程）

现状：1013 的形象/Idle/Cast/Walk、1051 的形象/idle/walk/attack 已验收，均未接入 Unity；两人的 Dead、受击、击飞链都还没做。
**1013 与 1051 的 Dead 之前连续不合格**：1013 是一张被否的末帧图，1051 是四张被否的 P3 姿势图；磁盘上两人都没有生成过 Dead 视频。

用户 2026-11-09 已定的新路线：**不再用文字描述新角色的死亡姿态，改用「工程内已部署的 Dead 帧 + 新角色」并排组图对照生成**（沿用 Idle 并排组图的成功经验）。主文档第 4 节已备好：四张参考图的顺序与职责、请求参数、完整英文/中文提示词、负面词、留档路径、量测与验收清单。

组图**已于 2026-11-09 出图**（路径与哈希见 §10），你这边**不要重复提交同一请求**。接下来请：① 读 §10 的量测表与偏差清单；② 把成图交给用户目视判定（同族性 / 1013 旗是否仍系杆顶 / 1051 弓是否在自己一侧上方 / 画风是否被磨平 / 背景是否可接受）；③ 若用户认可，按 §7 走裁切 → 单图精修 → 母片 → 视频 → 抠图落位；若要出 v2，**先改词并重新报批**（绿幕底与像素画风这两条偏差必须写进新词）。
组图只是姿态研究图，不能当素材，也不能代表 Dead 动画已成立（§6.3 边界不变）。
一切生图/视频请求仍须先报批（完整中英提示词 + 参考图职责 + 参数），并遵守 memory/user-preference.md 的全部铁律。
```

---

## 9. 相关文件索引

| 用途 | 路径 |
|---|---|
| 主文档 | `Locus/knowledge/plan/enemy-dead-group-reference-20261109.md` |
| 1013 交接 / 母任务书 | `plan/enemy1013-art-handoff-20261108.md`、`plan/enemy-commander-buff-unit-plan.md` |
| 1051 方案 / 形象交接 | `plan/enemy1051-remaining-animation-art-plan.md`、`plan/enemy1051-bandit-archer-redesign-handoff.md` |
| 已部署 Dead 实帧汇总 | `Library/Locus/tmp/dead_ref/all_dead.png`（可由 `Assets/Sprites/Enemy/*/Enemy_*_dead*.png` 重建） |
| 1011/1012 Dead 原始帧 | `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_dead{1..6}.png`、`Assets/Sprites/Enemy/Enemy1012/Enemy_1012_dead{1..6}.png` |
| 1013 形象 / 锚图 | `Enemy1013ArtSource/02_IdentityImages/1013_cast_anchor_green_1080x1080_v6.png`、`1013_lineup_crop_reference_rgba.png` |
| 1051 形象 / 锚图 | `Enemy1051ArtSource/02_IdentityImages/1051_idle_identity_greenscreen_1024.png`、`1051_cast_anchor_green_1080x1080.png` |
| 被否产物归档 | `C:/Users/steam/Pictures/gptGen/enemy1013_rejected/03_DeadPose_v1/`、`C:/Users/steam/Pictures/gptGen/enemy1051_rejected/03_DeadPose_P3/` |
| 作废提示词 | `Tools/visual_brief/prompts/1013_identity_deadpose1_REJECTED.*`、`1013_dead_v1_REJECTED.*`、`1051_pose_deadp3*.txt` |
| 工具 | `Tools/visual_brief/`：`submit_enemy_proportion_lineup.py`（组图流程范本，勿直接改用）、`submit_1051_pose.py`、`make_keyframe_clip.py`、`submit_seedance.py`、`chroma_key.py`、`compose_identity_greenscreen.py`、`compose_identity_from_alpha.py`、`measure_clip.py`、`make_contact_sheet.py` |
| 本轮新增工具 | `Tools/visual_brief/submit_enemy_dead_group.py`（组图提交脚本，§4 提示词已内嵌）、`Tools/visual_brief/measure_dead_group.py`（成图量测：4 邻接 BFS + 深色底梯度泛洪分背景，可测 alpha 帧做对照） |
| 本轮成图与留档 | `C:/Users/steam/Pictures/gptGen/enemy_dead_group_v1/`（工程外）：`enemy_dead_group_v1.png`、`enemy_dead_group_v1.prompt.txt` / `.prompt_cn.txt` / `.prompt_negative.txt` / `.request.json` / `.response.json` / `.status.json`、`enemy_dead_group_v1_measure.json`、`enemy_dead_group_v1_annotated.png` |

---

## 10. 本轮实际提交与成图结果（2026-11-09 16:17 提交，16:19 落盘）

### 10.1 请求与产物

| 项 | 值 |
|---|---|
| 提交时间 | 2026-11-09 16:17:35（本地） |
| 接口 / 模型 | `POST https://api.muskapis.com/v1/images/edits` / `gpt-image-2.6-sunburst`（当日 `GET /v1/models` 复核：图像模型只有它） |
| 参数 | `size=1536x1024`、`quality=max`、`input_fidelity=high`、`output_format=png`、`n=1`、不传 `background` |
| 提交文本 | 英文正向词 §4.3 + `Negative constraints: ` + §4.5 负面词（**负面词按 Idle 组图的既有做法并入正文**，因为该接口没有独立 negative 字段）；共 6413 字符，sha256 `d1f95bb0dd2d5f6c…`（英文正向词单独 sha256 `19e6e771fe06b46f…`） |
| 参考图 | 四张，顺序与 §4.2 一致，哈希逐张核对通过（`1da28cd9…` / `c647a5a7…` / `fed405d0…` / `f40bd12e…`） |
| 结果 | **HTTP 200**，耗时 124.95s，1536×1024 PNG，2,000,722 B，sha256 `ef3ae05203cfc08fc0fd06d99f8b280951790bc30f24ed005b62a281469b26a4` |
| 留档 | `C:/Users/steam/Pictures/gptGen/enemy_dead_group_v1/`（工程外，不进版本库） |

> `usage.image_tokens=0` 依旧不能用来判断参考图是否被采用；四张图是否真按职责被采用，只靠成图目视（结论见 §10.3）。

### 10.2 量测（口径同 §2：4 邻接 BFS；身体块 = 最大连通块；武器块 = 次大连通块；顶/底 = 占**整张画布**高度 %；画布 1536×1024，2×2 每格 768×512；**组图没有地面，地线取所在格底边**，与已部署帧的「地线 y=895」不是同一种距离）

| 角色 | 位置 | 身体块尺寸 | 宽高比 | 顶% | 底% | 武器 dx / dy（相对身体质心） |
|---|---|---|---|---|---|---|
| 1011 | 左上 | 482×267 | **1.805** | 15.1 | 41.2 | −263 / −138 |
| 1012 | 右上 | 356×246 | 1.447 | 9.3 | 33.3 | −317 / −110 |
| 1013 | 左下 | 389×262 | 1.485 | 10.4 | 35.9 | −236 / −180 |
| 1051 | 右下 | 340×259 | 1.313 | 10.8 | 36.1 | **+281 / −135** |

对照（工程内已部署帧，同一脚本同一口径复算，复现了 §2 的数值）：

| 帧 | 身体块尺寸 | 宽高比 | 顶% | 底% | 离地（地线 895） | 武器 dx / dy |
|---|---|---|---|---|---|---|
| 1011 dead3 | 722×539 | 1.340 | 27.3 | 75.9 | 54px | −353 / −167 |
| 1012 dead3 | 698×536 | 1.302 | 29.8 | 78.2 | 29px | −360 / −158 |
| 1011 dead6 | 739×548 | 1.349 | 27.4 | 76.9 | 43px | −350 / −193 |
| 1012 dead6 | 715×461 | 1.551 | 37.6 | 79.2 | 17px | −341 / −212 |

**量测结论**

1. **武器相对位置全部落在家族区间**：三人脱手武器 dx −236…−317、dy −110…−180（已部署帧 −341…−360 / −158…−212）；1051 的弓 dx **+281**（在自己那一侧）、dy −135 → §5 决定 #1 的默认口径成立。
2. **宽高比**：1051 = 1.313 落在「被打横那一拍 1.30–1.39」窄带内；1012 = 1.447、1013 = 1.485 在家族总区间（1.06–1.55）内但高于窄带；**1011 = 1.805 超出 1.6 的「平板」红线**（原始帧是 1.34，成图里四肢张得更开）。
3. **高度带没对齐**：成图身体底在 33–41%，提示词要求约 75%；四格不在同一高度，1011 还明显比其他三个大一圈（身体块宽 482 vs 340–389）。
4. **离地无法按已部署帧口径衡量**：组图没有地面，用格底边代算为 1011 90px / 1012 171px，仅作参考。

### 10.3 目视检查（我方初判，用户终判）

**达成（相对 §6.2 的验收清单）**

1. **同族性成立**：四个身体读起来确实是同一拍「腾空被打横」——背部弓成弧、头后仰、双腿屈起、双臂不对称甩开；头脚方向与方案一致（1011 / 1012 / 1013 头右脚左，1051 头左脚右）。
2. **1013 旗仍系在杆顶**，杆与旗一起脱手、飞在身体**自己那一侧的左上**，不再是「旁边一块平铺的布」。
3. **1051 弓飞在自己那一侧的右上**，与身体有明显间隙；两手无箭、画面无散落箭羽、箭袋仍是三支白羽箭；蒙面布巾与怒视眉眼保持。
4. **身份保持良好**：1013 的络腮胡／头巾护额／米色交领袍／蓝灰甲片／方扣皮带／绑腿战靴／暗赭红方旗金回纹、1051 的发髻／灰头带／蒙面布巾／皮背心／方扣腰带／腰间短刀／暗红布条／右肩箭袋／木弓红缠带，均与身份锚图一致；1012 的头盔红头带／敞口／米白短衣／胸甲／绑腿也保持。
5. **四人各在自己格子里**，不出画、不贴边，武器完整可见。

**偏差（与 §4 提示词不符）**

1. **背景不是绿幕**：模型给的是**深棕黑渐变 + 人物周围发光**（角落均值 RGB 8,8,8），不是要求的纯色绿幕 RGB (0,159,62)。后果：不能直接 chroma key；§7 第 1 步「裁切后合成 1024² 绿底件」要改成**分割去背**，而且发光晕会留黑边。
2. **画风漂移**：四个都被重绘成平滑厚涂，不是本项目的像素画语言（与 `memory/enemy-proportion-lineup-reference.md` 教训三同源，这次更明显）。
3. **1011 丢了头盔**：原始 `Enemy_1011_dead3.png` 有深色头盔，成图里是露头加深色布带。
4. **1051 的弓弦被画成一条伸出弓外的细白线**（渲染 artifact）。
5. **1011 身体块过扁**（1.805 > 1.6 红线），且比其余三个大一圈、四格不在同一高度带。

### 10.4 下一步（等用户拍板，不要自行推进）

- **若用户认可本图**：按 §7 走「裁出 1013 / 1051 面板 → 合成 1024² 底件 → 单图精修 → 母片 P1–P5 → 视频 → 抠图落位」。**注意第 1 步的底件合成要换掉绿幕键色做法**（本图没有绿幕）。
- **若用户要 v2**：必须先把两条偏差写进新词并**重新报批**（绿幕底、像素画风），再提交；提交纪律仍是「一次提交、不自动重试、不覆盖已有产物」。
- **本图仍是姿态研究图**：不是精灵、不是动画、不得裁下来当游戏素材，也不能代表 1013 / 1051 的 Dead 动画已成立（§6.3 边界不变）。
