---
id: kd_a79937d2-69ae-4dd5-81d6-136c5896082e
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_1012_bandit_face_v2_2026_10_30
summary: 1012 敌方面相 v2：从「凶狠武将」改为「明显山贼感」的完整方案与执行记录。含 v1 被否诊断、六次历史失败规律、用户已定的三项方向（山贼强度/脸+姿态/四件套全开）、Stage A2 大脸研究与 Stage B2 全身回贴的完整中英文词、参数、输出路径、SHA 与验收闸门。产物在 C:\Users\steam\Pictures\gptGen\enemy1012_bandit_face_v2\。未接 Unity。
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 1012 敌对面相 v2：从「凶狠」到「山贼感」

> **状态：A2、B2 各提交一次并落盘，等待用户对 B2 全身图的目视验收。**
>
> 旧档 `plan/enemy1012-hostile-face-generation-handoff.md` 在本轮中途被外部同步从 plan/ 目录移除（磁盘上已不存在，git 无历史）。本文件承接其全部有效内容并作为当前唯一入口；v1（Stage A/B）产物保留在 `C:\Users\steam\Pictures\gptGen\enemy1012_hostile_face_v1\`，未被覆盖。

---

## 1. v1 结论与被否原因

### 1.1 v1 已执行内容

| 阶段 | 输入 | 输出 | 结果 |
| --- | --- | --- | --- |
| Stage A（面相研究图） | v11 头肩裁切 `stageA_face_study_input_v1.png` | `stageA_face_study_v1.png`，1024×1024 RGB，1,215,864 bytes，SHA `29dca7c87bc16589c2159f2acf5c7718f035299787ffdfc894cf44fb6674967e`，HTTP 200 / 71.08s | 已生成 |
| Stage B（全身回贴） | image1 = v11 全身，image2 = Stage A | `stageB_enemy1012_hostile_face_v1.png`，1024×1024 RGB，1,304,871 bytes，SHA `a0cbb0124069356d732286065f6feeda5ecf2a5e3ca5c4fc2a1a2d2ddce347f5`，HTTP 200 / 126.06s | 已生成 |

### 1.2 用户验收结论（v1）

**不通过**：「只是让这个角色的面相变凶狠了，但依然没有『贼配军』等令人厌恶、想攻击的感觉。」

### 1.3 诊断

v1 目标是 hostile / fierce，模型自然映射到「重眉骨 + 正视 + 咬紧方下颌 + 闭嘴」，这本身就是**忠勇武将骨相**：宽方脸、颊厚、下颌方正、颈粗、粗眉左右对称、双眼正视、鼻梁端正、嘴宽唇线平、皮肤干净暖亮；全身又保留双脚对称、刀举身侧、胸甲端正的英雄式剪影。缩小到游戏尺寸时玩家先读到「正派武将立绘」，脸再阴险也会被剪影抵消。

三条障碍按优先级：**① 骨相没换；② 眼神写的是正视对峙，贼眼是斜的、半垂的、游移的；③ 全身剪影没动。**

### 1.4 历史失败规律（来自 `plan/enemy1011-face-identity-generation-handoff.md`）

| 记录 | 做法 | 结果 | 教训 |
| --- | --- | --- | --- |
| §22 | 「眉脊更重、暗色磨损」 | 整体变黑显老 | 敌意不能用程度副词表达 |
| §23 | 全身小脸改五官形状 | 几乎无变化 | 脸必须在大尺寸研究图上改 |
| §24 | 只写「一侧嘴角下压」 | 全变成歪嘴 | 单点嘴角变化会被执行成表情 |
| §25 | 无参考文生图 | 画风变写实 | 必须带 v11 参考图 |
| §26 | 「替换旧脸」 | 只改头骨轮廓 | 必须逐项点名眉/眼/鼻/嘴（§27 有效） |
| §27 | 显式逐项指定眉/眼/鼻/嘴 | 画风保住、五官确实改变 | 本轮沿用此写法 |

另有通用教训（`memory/gpt-image-edit-redraw-detection.md`）：`input_fidelity=high` 锁不住像素与几何，「保持不变」的否定词无效，必须用数值锚点。

---

## 2. 用户已定的方向（2026-10-30）

- 强度：**明显山贼感**（窄长脸、颧高颊凹、斜瞄眼神、挎刀驼背）
- 范围：**脸 + 姿态剪影**（装备种类、配色、画风、比例、刀型、头盔、红头巾、画布位置、脚底全锁死）
- 四件套**全开**：蜡黄油光皮肤 / 稀疏歪小胡子+杂胡茬 / 眼白略露的斜瞄 / 单侧嘴角上挑刻薄感

---

## 3. Stage A2：大尺寸山贼面相研究图（已完成，用户已通过）

### 3.1 输入

```text
C:\Users\steam\Pictures\gptGen\enemy1012_bandit_face_v2\stageA2_bandit_face_input_v1.png
```

由 v11 源图（SHA-256 `67da58389ed877ce7183b98ad06245c7ddc39eba79ce055fc3792d7e0cbb2c39`）裁切 `left=330, top=100, right=690, bottom=460`（360×360），最近邻放大 2.5× 到 900×900，居中置于 1024×1024 绿幕画布（填充 RGB 8,145,73）。输出 1024×1024 RGB PNG，329,143 bytes，SHA-256 `026921a74eeca41b454e11945663e3fd468936072ef1155d1a8f1224deae389e`。

**不含 v1 的凶狠脸**，避免叠加；不上传工程精灵 `Enemy_1012_idle1.png`。

### 3.2 参数

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit（字段不传）
image[]: ONLY stageA2_bandit_face_input_v1.png
read timeout: 900 seconds
automatic retries: 0
```

### 3.3 英文正向词（实际提交）

```text
Redesign the face and bone structure of the man in image 1 into a sly, cheap, thieving bandit soldier, and keep everything else in image 1 exactly as it is. Preserve the same v11 pixel-art rendering, the same iron helmet, the same red headscarf, the same neck, the same collar and shoulder area, the same head scale, the same head direction, the same overall skin brightness and the same uniform green background. Do not change the helmet, the scarf, the clothing or the framing.

Replace the original broad square soldier face. Give him a narrow long face: high cheekbones, hollow sunken cheeks, a narrowed jaw, a slightly pointed and slightly forward chin, a longer upper face and a slightly thinner neck, while the head stays broad and heavy so he is still the same stocky soldier and not a thin scholar. He must read as a sly, dirty-minded bandit, not as a brave officer.

Replace the eyebrows with thin, long, uneven brows whose outer ends tilt upward, the left one a little higher and thinner than the right. Replace the eyes with narrow triangular slits under heavy upper lids, the inner corners pressed down, the outer corners slightly lifted, the eyes set a little closer together, and one eye catching a thin sliver of white as he glances sideways at the viewer instead of staring straight ahead. Replace the nose with a broad flattened tip, slightly flared nostrils and a bridge that drifts a little off center. Replace the mouth with thin pursed closed lips, a slightly thinner upper lip, the lip line reading straight and shut, with only one corner sitting a fraction higher than the other so the closed mouth reads as a mean contemptuous curl rather than a snarl. Keep the lips closed at all times. Replace the heavy short beard with a sparse, crooked, uneven wispy mustache sitting crookedly above the lip and a few stray stubble hairs on the chin, so the facial hair looks neglected rather than groomed.

Keep the skin at the same brightness as the source image, but give it a sallow yellow-olive undertone with a faint greasy sheen, deep-set eye sockets and a slight shadow under the eyes, so the face looks oily, sly and unpleasant to look at. He should look like a thief who would sell you out, not a scarred hero, not a monster and not a comic character.

The whole effect must still be readable when the head is shrunk to a small game sprite size.
```

### 3.4 英文负向词（实际提交）

```text
Avoid a broad square jaw; a wide friendly face; full round cheeks; a thick straight neck; a handsome heroic face; a strong symmetric stare straight into the camera; direct eye contact; angry shouting brows; a scowl; a snarl; a grotesque twisted or lopsided mouth; an open mouth; visible teeth; a grin; smiling; a bushy beard; a full beard; a large dark beard; a clean shaven face; darker skin; blackened skin; dirty skin; grime; blood; wounds; scars; wrinkles; aging; an old man; a gaunt starved look; a scholar's face; a monster; a demon; a cartoon; realistic painterly rendering; smooth shading; low-resolution chunky pixels; a new helmet; a changed scarf; changed clothing; changed head size; changed framing; a second person; extra equipment; scenery; floor; shadow; text; UI; checkerboard; transparency; and any canvas other than 1024x1024 RGB PNG.
```

### 3.5 执行结果

| 项 | 值 |
| --- | --- |
| 提交 | 第 1 次即成功，HTTP 200，141.32s |
| 字段 | `gpt-image-2.5-sunburst` / `1024x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1`，只省 `background` |
| 输入 | 只有 `stageA2_bandit_face_input_v1.png`（SHA `026921a7…ae389e`） |
| 产物 | 1024×1024，Color Type 2，1,223,099 bytes，SHA-256 `b3f747c5ca3fa05eeeb6d2b2ee6d73d061c2c0f4d19c9f112b30f689cc51bf6f` |
| usage | input 1528（image 560 / text 968），output 7024，total 8552 |

```text
C:\Users\steam\Pictures\gptGen\enemy1012_bandit_face_v2\stageA2_bandit_face_v1.png
```

发起侧目视：宽方脸 → 窄长脸、下颌收窄、颧骨与颊部转折明显；眉细长外端上挑左右不等；眼窄三角、上眼睑重压、一线眼白斜瞄；鼻头宽扁鼻梁略偏；嘴闭合薄唇、唇线直、单侧略高；上唇胡稀疏歪斜 + 下巴杂胡茬；肤色偏黄蜡质带油光。头盔、红头巾、衣领、画法、绿底未变。**用户验收通过。**

遗留观察：两只眼都留了可见眼白，斜瞄成立但白眼球面积偏大，可能被读成「懒眼／斗鸡眼」。用户已知悉并接受。

---

## 4. Stage B2：全身回贴 + 姿态剪影（已提交，等待验收）

### 4.1 输入顺序与职责

| 序 | 文件 | 职责 | SHA-256 |
| --- | --- | --- | --- |
| image 1 | `C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v11\enemy1011_style_locked_dao_v11.png` | 唯一权威：全身设计、装备、配色、画风、比例、人物高度、脚底线、画布位置 | `67da5838…cbb2c39` |
| image 2 | `C:\Users\steam\Pictures\gptGen\enemy1012_bandit_face_v2\stageA2_bandit_face_v1.png` | 只提供已通过的山贼骨相与眼神 | `b3f747c5…cc51bf6f` |

不上传 `Enemy_1012_idle1.png`、v1 的 Stage A/B 产物、旧 v5、旧脸组图、外部参考图。

### 4.2 参数

与 A2 相同：`gpt-image-2.5-sunburst` / `1024x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1` / 不传 `background` / 读超时 900s / 自动重试 0。

### 4.3 英文正向词（实际提交）

```text
Repaint the soldier in image 1 as a sly bandit soldier, using image 2 only as the source of the new face and bone structure.

Keep from image 1: the same iron helmet with the same red headscarf, the same hemp tunic, the same chest plate, the same rope belt, the same bracers, the same trousers, the same leg wraps, the same heavy boots, the same single ring-pommel dao held in the right hand, the same hard-edged high-definition pixel-art rendering, the same cool iron and warm cloth palette, the same body proportions, the same overall body height, the same foot line and the same position inside the 1024x1024 canvas. The figure must keep exactly the same vertical extent as in image 1, from the top of the helmet to the soles of the boots.

Change the exposed face to match image 2: the narrow long face with high cheekbones and hollow cheeks, the slightly pointed forward chin, the thin uneven brows with lifted outer ends, the narrow triangular slit eyes with heavy upper lids and one eye showing a thin sliver of white as he glances sideways, the broad flattened nose tip, the thin pursed closed lips with a faint one-sided lift, the sparse crooked wispy mustache and stray chin stubble, and the sallow yellow-olive oily skin with deep-set sockets and a slight shadow under the eyes.

Also change the stance so it no longer reads as a heroic officer, while keeping the same equipment and the same body. Let him slouch: shoulders pushed forward and slightly hunched, the head tilted a little to one side, the weight shifted onto one leg so the feet are no longer planted symmetrically, the dao arm hanging relaxed with the blade tip angled down toward the ground instead of raised, and the free hand tucked at the belt or gathered at his chest instead of hanging straight down. Keep the right hand on the dao and keep exactly one dao.

He must read as a cheap, sly, dangerous bandit soldier: greasy and unpleasant to look at, the kind of enemy the player wants to hit. The hostility must come from the bone structure, the sideways greedy gaze and the slouching asymmetry, not from darker skin, dirt, blood, wounds, scars or an exaggerated monster face.

Deliver one complete full-body soldier on the same uniform green background, with the entire helmet, red headscarf, dao, hands, boots and feet inside the frame.
```

数值锚点说明：`The figure must keep exactly the same vertical extent as in image 1, from the top of the helmet to the soles of the boots.` 是相对早期版本新增的一句，用于对抗 `input_fidelity=high` 下的几何漂移（改姿态时人高/整体缩放最容易跑）。

### 4.4 英文负向词（实际提交）

```text
Avoid a heroic upright stance; a broad square jaw; a friendly handsome face; direct eye contact; a symmetrical planted stance; a raised brandished blade; a clean shaven face; a bushy beard; a full beard; a large dark beard; darker skin; blackened skin; dirty skin; grime; blood; wounds; scars; wrinkles; aging; an old man; a gaunt starved scholar; a monster; a demon; a cartoon; a changed helmet; a new helmet; a changed headscarf; changed clothing; changed armor; changed weapon; a second weapon; a shield; a spear; changed body proportions; a different body height; cropped feet; cropped blade; an extra person; realistic painterly rendering; smooth cartoon shading; low-resolution chunky pixels; scenery; floor; contact shadow; text; UI; checkerboard; transparency; and any canvas other than 1024x1024 RGB PNG.
```

### 4.5 执行结果

| 项 | 值 |
| --- | --- |
| 提交 | 第 1 次即成功，HTTP 200，76.06s |
| 输入 | image1 = v11 全身，image2 = A2 通过图（两张按序） |
| 产物 | 1024×1024，Color Type 2，1,235,496 bytes，SHA-256 `56fb65d7d4dd8dd2e10eee0923aad8925ec3736e17aab337b167966b7f2500fe` |
| usage | input 2761（image 2048 / text 713），output 7024，total 9785 |

```text
C:\Users\steam\Pictures\gptGen\enemy1012_bandit_face_v2\stageB2_enemy1012_bandit_v1.png
```

同目录伴生记录：`stageB2_enemy1012_bandit_v1.request.json` / `.response.json` / `.status.json` / `.prompt.txt`。

### 4.6 发起侧自检（**未经用户验收**）

- 面相：窄长脸、颧高颊凹、细长上挑眉、窄三角眼含一线眼白斜瞄、宽扁鼻头、抿嘴单侧略高、稀疏歪胡须、蜡黄油光 —— 已从 A2 传到全身；
- 姿态六项：肩前扣驼背 ✓、头微歪 ✓、重心偏一条腿且双脚不对称 ✓、刀放松垂下且刀尖斜向地面 ✓、空手插在麻绳腰带处 ✓；
- 装备与身份：兜鍪、红头巾、麻衣、胸牌、麻绳腰带、护腕、裤子、绑腿、厚靴、右手单把环首刀、冷铁暖布配色、像素画法均在；
- 几何：人物竖向范围 83→968，与 v11 的 83→967 一致（数值锚点生效），人物高度未漂移；横向变窄是因为刀改为斜垂；
- 差异点：刀身比 v11 更宽更方、靴子与绑腿细节有重绘、盔檐形状略有差异，属于编辑接口的重绘抖动，需用户判定是否可接受。

---

## 5. 验收闸门（B2）

1. 缩小看第一眼是阴险山贼，不是正派武将；
2. 面相来源清楚：窄长脸、颧高颊凹、斜瞄一线眼白、抿嘴略偏、小胡子、蜡黄油光都能看到；
3. 不是只靠单点 gimmick（不是只歪嘴、不是只黑脸、不是只张嘴露齿、不是只血疤、不是只大胡子）；
4. 姿态六项逐项确认；
5. v11 装备、配色、画风、比例、人物高度、脚底线、画布位置无漂移；
6. 技术完整：1024×1024 RGB PNG、绿幕均匀、无场景、阴影、文字、UI、棋盘格、透明误判；
7. 与 `Assets/Sprites/Enemy/Enemy1012/Enemy_1012_idle1.png` 按人物高度和脚底对齐并排，缩小后「贼」感仍可读。

不通过时：保留 A2 与 B2 记录，不自动重发、不改 Unity。若要改，必须另立版本并重新走「展示输入 + 完整词 + 取得批准」。

---

## 6. 仍未授权（与 v1 一致）

- 覆盖 `Assets/Sprites/Enemy/Enemy1012/` 任何精灵；
- 去背、选帧、缩放、脚底对齐、补边、Clip、Animator、Prefab、Unity 接入；
- 重做 1012 的 Idle / Walk / Attack 等动作；
- 修改 1011 原角色；
- 把未验收候选写成 `design/art-style-guide.md` 的正式风格样本。

静帧通过后可另立下一份动作替换交接；静帧通过不等于 70 张 1012 动画精灵需要立即替换。

---

## 7. v3：换汉末兜鍪 + 完全正视 + 蓄势型交战 idle（用户反馈后新增，2026-10-30）

> **状态：B3 已由用户目视验收通过，定稿为新的 1012 idle 静帧候选（2026-10-30）。**
>
> 定稿产物：`C:\Users\steam\Pictures\gptGen\enemy1012_bandit_face_v3\stageB3_enemy1012_bandit_idle_v1.png`，1024×1024 RGB PNG，1,273,294 bytes，SHA-256 `677cfb33ccf7349395d958cc0f9a5f55ca6da695e805bd7c7c7f7801aa34f4ba`。头肩基准图：`stageA4_head_refine_v1.png`（SHA `123fae19…f277687`）。
>
> **尚未做**：去背、选帧、缩放、脚底对齐、补边、Clip、Animator、Prefab、Unity 接入，以及 70 张 1012 动作帧的重做。
>
> 触发原因：用户对 B2 的验收结论——面相对了，但**角色没有面朝前方正视前方**；idle 姿态**不像交战状态**（比原英雄站姿更贴气质但仍需调）；头顶钢盔**形制不像三国**。

### 7.1 三条反馈的诊断

**① 朝向**：v2 的词把「头歪 + 斜瞄 + 重心偏腿」捆成「游荡打量」姿态，模型把身体也跟着转出正面轴，剪影先读成侧身回头的人。这是设计转向而非单纯姿态 bug：v1/v2 的「贼」靠**偷看**，用户要的是**面朝玩家的奸相**。因此把「贼」从偷看迁移到对峙表达：去掉斜瞄与一线眼白，改用「上睑重压、下睑略抬、瞳孔收小、眼神发黏没对焦感」；骨相、蜡黄肤质、稀疏胡须、发际线沿用 A2 已通过的脸。

**② idle 不像交战**：B2 是「歇脚」而非「蓄势」——驼背、刀垂到地、空手插腰带、双脚松散，没有可启动性。交战 idle 需要蓄势：正面朝向、肩线等宽、含胸微前倾、重心压在前脚掌、膝微屈站姿更低、刀斜举在体侧偏前（护手朝外、刀尖后下）、空手半握拳护腹前、肘内收。含胸+微屈+半起手+握拳 = 贼兵式蓄势，仍低、黏、不正派，但随时能出手。

**③ 头盔形制**：放大确认 v11 头饰为宽平檐 + 高锥顶 + 顶钮 + 檐下细红带两侧布尾，属笠帽/风帽形制（宋明风），非汉末形制；汉末正规军为**半球形兜鍪 + 护颈顿项 + 短红缨 + 额前护额**。宽檐钢盔同时是「正统军」符号，会把气质往武将方向拉，与「贼兵」目标冲突。

### 7.2 用户已定的三项（2026-10-30）

| 项 | 决定 |
| --- | --- |
| 头饰 | **换成汉末兜鍪**：低矮半球形盔体、顶部短红缨、额前护额、后颈护颈顿项、皮革系带松垮、盔体有凹痕磨损 |
| 视线 | **完全正视**：脸与身体正对玩家，眼球正对，去掉斜瞄与一线眼白；贼意靠上睑重压、眼神发黏无对焦感、骨相、肤质、胡须 |
| 姿态 | **蓄势型交战 idle**：正面朝向、含胸微前倾、重心在前脚、膝微屈、刀斜举体侧偏前（护手朝外、刀尖后下）、空手半握拳护腹前肘内收、头正对 |

### 7.3 连带影响（未授权，仅提示）

工程 `Assets/Sprites/Enemy/Enemy1012/` 现有 70 张精灵全部带旧宽檐钢盔。换盔会使头盔轮廓与所有动作帧不一致。本轮**只产出新的 idle 静帧基准图**，不碰现有 70 帧；动作重做另立交接。

### 7.4 Stage A3：头肩研究图（换盔 + 正视）

输入（复制自已通过的 A2，SHA 不变）：

```text
C:\Users\steam\Pictures\gptGen\enemy1012_bandit_face_v3\stageA3_input_from_A2.png
SHA-256 b3f747c5ca3fa05eeeb6d2b2ee6d73d061c2c0f4d19c9f112b30f689cc51bf6f
```

职责：**只提供已通过的脸**（窄长脸、颧高颊凹、尖下巴、细上挑眉、窄三角眼、宽扁鼻、抿嘴、稀疏歪胡须、蜡黄油光）与 v11 画法。不作 B3 全身回贴的 image 2 以外用途。

参数与 v2 相同：`POST /v1/images/edits` / multipart / `gpt-image-2.5-sunburst` / `1024x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1` / 不传 `background` / 读超时 900s / 自动重试 0。

输出：

```text
C:\Users\steam\Pictures\gptGen\enemy1012_bandit_face_v3\
├─ stageA3_head_study_v1.png
├─ stageA3_head_study_v1.request.json
├─ stageA3_head_study_v1.response.json
├─ stageA3_head_study_v1.status.json
└─ stageA3_head_study_v1.prompt.txt
```

英文正向词（实际提交）：

```text
Keep the man's face in image 1 exactly as it is, and change two things only: the eyes now look straight at the viewer, and the wide-brimmed iron hat is replaced by a late-Han Chinese soldier helmet.

Keep his narrow long bandit face, high cheekbones, hollow cheeks, slightly pointed forward chin, thin uneven brows with lifted outer ends, the narrow triangular eyelids, the broad flattened nose tip, the thin pursed closed lips with a faint one-sided lift, the sparse crooked wispy mustache, the stray chin stubble, the sallow yellow-olive oily skin with deep-set sockets and the slight shadow under the eyes, and the same skin brightness. Keep the same head size, the same head direction straight toward the viewer, the same neck, the same collar and shoulder area, the same fine high-definition pixel-art rendering and the same uniform green background.

Change the eyes: both eyes now look straight out at the viewer, centred in their narrow triangular openings, with heavy upper lids pressing down and a dull, unfocused, greedy look, as if he is staring at a victim while calculating. Show no white of the eye on the inner corners any more, and do not angle the gaze sideways.

Replace the wide flat brim hat with a late-Han Chinese soldier helmet: a low hemispherical iron dome that sits close on the head, a short red horsehair plume at the very top, a narrow forehead band across the brow, and a rounded neck guard flaring down over the back of the neck. The helmet is battered and dented, the iron is dark and dull, and a loose leather chin strap hangs slack. No wide brim, no tall cone, no knob finial and no hat over the helmet.

He must still read as a sly, cheap, dangerous bandit soldier facing the player head on, with a greasy unpleasant face and an unfocused greedy stare. Do not turn him into a clean handsome officer, and do not turn him into a monster or a cartoon.
```

英文负向词（实际提交）：

```text
Avoid the wide-brimmed hat; a tall conical hat; a knob finial on top of the hat; a straw rain hat; cloth hat tails at the sides of the head; a sideways glance; eyes looking away from the viewer; visible white of the eye; a tilted head; a changed face shape; a changed jaw; a changed nose; a changed moustache; a changed skin tone; a handsome heroic face; a smile; an open mouth; visible teeth; darker skin; dirty skin; grime; blood; wounds; scars; wrinkles; aging; a monster; a demon; a cartoon; realistic painterly rendering; smooth shading; low-resolution chunky pixels; changed clothing; changed shoulders; changed framing; a second person; extra equipment; scenery; floor; shadow; text; UI; checkerboard; transparency; and any canvas other than 1024x1024 RGB PNG.
```

A3 验收闸门：兜鍪形制对（半球+短红缨+护额+顿项，无宽檐无顶钮）；双眼正视玩家且无眼白偏侧；脸骨相/肤质/胡须与 A2 一致未回退；不是变黑变老变脏；嘴闭唇直；头盔与脸之间比例正常（不遮眉眼）；1024×1024 RGB 绿幕无场景文字。

#### A3 执行结果（2026-10-30，1 次提交成功）

| 项 | 值 |
| --- | --- |
| 提交 | 第 1 次即成功，HTTP 200，135.97s |
| 字段 | `gpt-image-2.5-sunburst` / `1024x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1`，只省 `background` |
| 输入 | 只有 `stageA3_input_from_A2.png`（= A2 通过图，SHA `b3f747c5…cc51bf6f`） |
| 产物 | 1024×1024，Color Type 2，1,283,790 bytes，SHA-256 `667ebd2caf96031ff83dbab09e3ebf582995c22172706467e893cf00a08b8a61` |
| usage | input 664（text 664），output 7024，total 7688 |

```text
C:\Users\steam\Pictures\gptGen\enemy1012_bandit_face_v3\stageA3_head_study_v1.png
```

发起侧目视（**未经用户验收**）：

- 头饰已换成汉式铁兜鍪：贴头半球形盔体、带铆钉、额前护额、顶部红缨、两侧下垂护耳（内衬红）、松垮皮革系带；宽檐与顶钮消失；
- 双眼已正视玩家，内眼角不再露眼白；
- 脸骨相、稀疏胡须、蜡黄肤质保留。

两点需要用户判定的观察：

1. **贼感部分回退**：正视 + 粗重下压的眉 + 双眼白较明显 = 读成「怒视的凶将」而不是「贼」。A2 的偷瞄消失后，缺少黏、钝、不对焦的眼神。
2. **盔的细节偏向**：红缨偏长偏飘，两侧护耳下垂且红衬明显，视觉上略接近日式/戏曲兜鍪，不一定是用户想要的汉末正统感。

若要修（可选 A4，只改眉眼局部的 A3 输入）：上睑再压低盖住虹膜上半、只留一线眼白；眉外端上挑变细、左右一高一低；眼角略松；保留嘴角单侧略高与蜡黄肤质。

#### A4 执行结果（2026-10-30，1 次提交成功，用户已批准执行）

| 项 | 值 |
| --- | --- |
| 提交 | 第 1 次即成功，HTTP 200，69.88s |
| 字段 | `gpt-image-2.5-sunburst` / `1024x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1`，只省 `background` |
| 输入 | 只有 `stageA3_head_study_v1.png`（SHA `667ebd2c…8b8a61`） |
| 产物 | 1024×1024，Color Type 2，1,223,971 bytes，SHA-256 `123fae194d4661ac5aff4ecc8e6ffeacfe50a40431467b134936cb429f277687` |
| usage | input 1685（image 1024 / text 661），output 7024，total 8709 |

```text
C:\Users\steam\Pictures\gptGen\enemy1012_bandit_face_v3\stageA4_head_refine_v1.png
```

发起侧目视（**未经用户验收**）：红缨收短、护耳改为贴头铆钉片且去红衬，盔形更汉末；双眼正视且眼型变窄、上睑下压、眼白减少；脸骨相、稀疏胡须、蜡黄肤质、嘴型保留。**残留观察**：眉仍偏粗偏深、外端上挑不明显，怒意未完全清掉；双眼白仍在，属于半睑钝感而非瞪眼。

A4 是 B3 的 image 2 候选；B3 需等用户对 A4 的目视结论。

### 7.5 Stage B3：全身回贴 + 蓄势型交战 idle

只有 A3 获用户目视通过后才准备。`image[]` 顺序：image 1 = v11 全身（唯一权威：装备、配色、画风、比例、人物高度、脚底线、画布位置），image 2 = A3 通过图（只提供兜鍪与脸）。参数同 A3。

英文正向词（实际提交）：

```text
Repaint the soldier in image 1 as a sly bandit soldier standing in a battle-ready idle stance, using image 2 as the source of his head: the late-Han hemispherical iron helmet with its short red plume, forehead band and neck guard, and the narrow long bandit face with the sallow oily skin, the thin pursed lips, the sparse crooked mustache and the heavy-lidded eyes now looking straight at the viewer.

Keep from image 1: the same hemp tunic, the same chest plate, the same rope belt, the same bracers, the same trousers, the same leg wraps, the same heavy boots, the same single ring-pommel dao, the same cool iron and warm cloth palette, the same hard-edged high-definition pixel-art rendering, the same body proportions, the same overall body height, the same foot line and the same position inside the 1024x1024 canvas. The figure must keep exactly the same vertical extent as in image 1, from the top of the helmet to the soles of the boots.

Remove the wide flat brim hat and the red cloth tails beside the head, and put on the late-Han helmet from image 2.

Build a battle-ready idle stance that faces the player head on: the body axis square to the viewer, both shoulders level and almost even, the chest slightly rounded with the upper body leaning a little forward, the weight settled onto the front foot so he can move at any moment, both knees slightly bent so he stands lower and compact, the feet set apart but not symmetrically planted. His right hand holds the ring-pommel dao angled up beside his body at about thirty to forty-five degrees, the guard turned forward and outward and the blade tip pointing down and back, ready to swing but not brandished overhead and not hanging limp. His left hand is half closed into a fist and held low in front of his belly with the elbow tucked in. His head is level and turned straight to the player, and his dull heavy-lidded eyes look straight out of the picture.

He must read as a cheap, sly, dangerous bandit soldier waiting to fight: greasy and unpleasant, low and coiled, not a proud officer, not a tired lounger, not a monster. The menace must come from the bone structure, the unfocused stare, the battered helmet and the coiled stance, not from darker skin, dirt, blood, wounds, scars or an exaggerated monster face.

Deliver one complete full-body soldier on the same uniform green background, with the entire helmet, plume, dao, hands, boots and feet inside the frame.
```

英文负向词（实际提交）：

```text
Avoid the wide-brimmed hat; a tall conical hat; a knob finial; a straw rain hat; red cloth tails beside the head; a sideways glance; a tilted head; a proud upright heroic stance; a wide symmetrical planted stance; a raised brandished blade overhead; a dangling limp dao arm; a hand inside the belt; a slouched tired stance; straight stiff legs; a handsome heroic face; a smile; an open mouth; visible teeth; a bushy beard; a full beard; a large dark beard; darker skin; blackened skin; dirty skin; grime; blood; wounds; scars; wrinkles; aging; an old man; a gaunt starved scholar; a monster; a demon; a cartoon; changed clothing; changed armor; a changed weapon; a second weapon; a shield; a spear; changed body proportions; a different body height; cropped feet; cropped blade; an extra person; realistic painterly rendering; smooth cartoon shading; low-resolution chunky pixels; scenery; floor; contact shadow; text; UI; checkerboard; transparency; and any canvas other than 1024x1024 RGB PNG.
```

B3 验收闸门：兜鍪正确且与 A3 一致；脸正视玩家且骨相未回退；姿态六项逐项成立（正面朝向、含胸前倾、重心前脚、膝微屈、刀斜举半起手、空手握拳护腹前）；v11 装备配色画风比例人物高度脚底线画布位置无漂移；技术完整；与 `Enemy_1012_idle1.png` 按人物高度与脚底并排缩小后仍读得出「挡路的贼兵」。

#### B3 执行结果（2026-10-30，1 次提交成功）

| 项 | 值 |
| --- | --- |
| 提交 | 第 1 次即成功，HTTP 200，70.49s |
| 字段 | `gpt-image-2.5-sunburst` / `1024x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1`，只省 `background` |
| 输入 | image1 = v11 全身（SHA `67da5838…cbb2c39`），image2 = A4 通过图（SHA `123fae19…f277687`） |
| 产物 | 1024×1024，Color Type 2，1,273,294 bytes，SHA-256 `677cfb33ccf7349395d958cc0f9a5f55ca6da695e805bd7c7c7f7801aa34f4ba` |
| usage | input 2837（image 2048 / text 789），output 7024，total 9861 |

```text
C:\Users\steam\Pictures\gptGen\enemy1012_bandit_face_v3\stageB3_enemy1012_bandit_idle_v1.png
```

发起侧目视（**未经用户验收**）：

- 头饰：汉末兜鍪到位——半球盔体、短红缨、额前护额、铆钉、贴头护耳、松垮皮带；宽檐与红布尾已消除；
- 面相：双眼正视玩家、眼型窄、上睑下压、稀疏胡须与蜡黄肤质保留；
- 姿态六项逐项成立：正面朝向、肩线等宽、含胸微前倾、重心前脚、双膝微屈低身位、双脚不对称站定；右手刀斜举体侧偏前、护手朝外、刀尖朝后下；左手半握拳护在腹前偏低、肘内收；
- 装备：麻衣、胸牌、麻绳腰带（带红布）、护腕、裤子、绑腿、厚靴、右手单把环首刀、冷铁暖布配色、像素画法均在；
- 几何：人物竖向范围 70→959（高 889），v11 为 83→967（高 884），差 5px，数值锚点生效。

两点需用户判定：

1. **头身比变了**：兜鍪比旧宽檐盔小，加上躯干被重绘得更宽，角色整体显矮壮、头相对变小；与 v11 的「头略大」不完全一致。
2. **贼感仍偏「恶面怒视」**：正视 + 眉压 + 窄眼 = 恶面。这是「完全正视」路线的结构性代价；若要更强的贼感，只能再放宽正视（例如允许轻微侧脸）或靠姿态/装备不对称补。

### 7.6 v3 执行顺序

```text
展示 A3 输入副本 + 完整中英文词 + 参数 + 输出路径 → 取得 A3 批准
→ 提交 A3 一次 → 磁盘回读 → 附图验收
→ A3 通过后展示 B3 两图顺序与完整词 → 取得 B3 批准
→ 提交 B3 一次 → 磁盘回读 → 附图验收
→ 通过后登记为新的 1012 idle 静帧候选（仍不动 Unity）
```

---

## 8. 定稿与后续决策点（2026-10-30）

用户验收：**B3 通过，定稿**。定为新的 1012 idle 静帧候选，**不覆盖任何工程资源**。

定稿文件：

```text
C:\Users\steam\Pictures\gptGen\enemy1012_bandit_face_v3\stageB3_enemy1012_bandit_idle_v1.png
```

1024×1024 RGB PNG，1,273,294 bytes，SHA-256 `677cfb33ccf7349395d958cc0f9a5f55ca6da695e805bd7c7c7f7801aa34f4ba`。

头肩基准图（后续动作重做时作头部参考）：`stageA4_head_refine_v1.png`，SHA-256 `123fae194d4661ac5aff4ecc8e6ffeacfe50a40431467b134936cb429f277687`。

定稿特征（供后续动作重做与接入时对照）：

- 汉末兜鍪：半球盔体 + 短红缨 + 额前护额 + 铆钉 + 贴头护耳 + 松垮皮带；**无宽檐、无顶钮、无红布尾**；
- 面相：窄长脸、颧高颊凹、略尖前伸下巴、细长外端上挑眉、窄三角半睑正视眼、宽扁鼻头、抿唇单侧略高、稀疏歪胡须 + 下巴杂胡茬、偏黄蜡质油光皮肤与眼下淡影；
- 姿态：正面朝向、肩线等宽、含胸微前倾、重心前脚、双膝微屈低身位、双脚不对称站定、右手刀斜举体侧偏前（护手朝外、刀尖后下）、左手半握拳护腹前肘内收；
- 保留 v11：麻衣、胸牌、麻绳腰带带红布、护腕、裤子、绑腿、厚靴、单把环首刀、冷铁暖布配色、细密高清像素画法、人物高度（889px vs v11 884px）。

已知差异（定稿时用户已知悉并接受）：头相对变小、躯干略宽；正视+眉压使读感偏「恶面怒视」而非强贼相。

### 8.1 后续决策点（均未授权）

1. **70 张动作帧**：工程 `Assets/Sprites/Enemy/Enemy1012/` 现有 70 张精灵全部带旧宽檐钢盔。新静帧换盔后头部轮廓与全部动作帧不一致，idle 动画会出现头盔跳变。必须决定：只替换 idle 静帧 / 先重做 idle 循环帧 / 整体重做 70 帧。
2. **接入链**：去背（绿幕底）→ 缩放 → 脚底对齐 → 补边 → Clip → Animator → Prefab → 场景接线。当前 Unity Editor 状态为 `crashed`，接入前需先处理。
3. **同类敌人**：1011 及其他刀兵/盾卫/枪兵/弓手/重甲兵若要同一「贼兵」气质，需另立分组方案（历史 `plan/enemy1011-face-identity-generation-handoff.md` §20–§28 有六兵种结构表草案）。

---

## 9. 记录与执行纪律

- 每阶段一次请求、`quality=max`、`input_fidelity=high`、不传 `background`、不请求透明；
- 无自动重试、不换模型、不降档、不叠轮编辑；
- 用磁盘实际文件（目录、尺寸、Color Type、SHA-256）确认「已生成」，不以工具回显为准；
- 凭据只从环境变量 `MUSK_API_KEY` 读取，不写入任何脚本、记录、文档或回复；
- 提交脚本存档于 `Library/Locus/tmp/`：`enemy1012_stageA2_submit.py`、`enemy1012_stageB2_submit.py`、`enemy1012_stageA3_submit.py`、`enemy1012_stageA4_submit.py`、`enemy1012_stageB3_submit.py`（以及 v1 的 `enemy1012_stageA_submit.py`、`enemy1012_stageB_submit.py`）。
