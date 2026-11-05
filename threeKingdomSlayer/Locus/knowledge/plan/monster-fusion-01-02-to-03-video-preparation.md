---
id: kd_620715a1-0a4e-4e2d-9163-ff81f154dcb8
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 怪兽合成进化片：01 + 02 → 03（10 秒，1:1 纯绿幕）制作方案

## 1. 状态与本轮授权

**状态：生成词已备好，待用户审阅后一次性发起。尚未提交任何付费生成，未改动任何项目资产，未做本地合成。**

用户 2026 的决策（本轮定调）：

| 决策项 | 选择 |
|---|---|
| 管线 | **AI 只做中段融合特效层 + 本地精确合成**（不采用 AI 一镜到底） |
| 画幅 / 底色 | **1:1，纯绿幕** |
| 生成授权 | 用户要求先审阅生成词，再决定是否发起 |

任务边界：**与本项目（三国割草）完全无关**，不产出 Unity 资产、不进引擎、不做透明精灵序列。交付物是给美术看的概念片。

## 2. 素材实测（本轮实测，非推测）

| | 画布 | 透明占比 | 色彩 | 边缘 | 主体(core) bbox | 宽高比 |
|---|---|---|---|---|---|---|
| 01 | 1200² | 66.7% | 99.2% 纯灰阶（近黑 16 / 近白 240） | 硬边，99.4% 可见像素 alpha=255 | x[66,1130] y[104,1097] 1065×994 | 1.071 |
| 02 | 1000² | 73.8% | 100% 纯灰阶，饱和度 0 | 硬边 | x[86,910] y[55,946] 825×892 | 0.925 |
| 03 | 2000² | 72.4% | 彩色（饱和度峰值 127，均值 27.4） | 软边 + 辉光，12.5% 半透明像素 | x[331,1699] y[228,1750] 1369×1523 | 0.899 |

关键事实：01/02 是同一套硬边双色调像素画；03 是软边彩色插画 + 霓虹辉光。**两者是不同渲染技术**，任何"形变式"生成都会导致身份丢失。

实测脚本与产物：`Library/Locus/tmp/monster_fusion_analysis/`（alpha 分布、配色直方图、core bbox、局部放大裁图）。

## 3. 成片结构（10.0s / 1:1 / 960×960 / 24fps / 241 帧）

| 时间 | 内容 | 由谁产出 |
|---|---|---|
| 0.0–2.0s | 01（左）与 02（右）同台，相向而立。主体等高 345px、脚底基线 y=819、左右对称，四边 12% 安全边（实测见 §3.1） | **本地**（原 PNG 精确合成） |
| 2.0–3.2s | 两个本体做像素崩解（按 alpha 噪声阈值溶解）：01 的碎屑保持硬边白刃状，02 的碎屑保持圆钝团块状，碎屑向中心流动 | **本地** |
| 3.2–7.2s | 融合特效层：左侧锐利白色碎裂光条 + 右侧圆润烟雾团对冲 → 交织成螺旋 → 收束成一点 → 爆闪扩散成光晕与火星 | **AI（4s 一段）** |
| 7.2–8.8s | 03 的纯黑剪影自爆闪中心浮现；彩色沿剪影自中心向外填充 | **本地**（用 03 原始 alpha） |
| 8.8–10.0s | 完整彩色 03 定格，辉光呼吸 + 极缓推近，末帧 = 03 原始立绘像素 | **本地** |

AI 段只占全片 40%，且**不包含任何角色**。角色的身份、比例、配色、末帧一致性全部由本地合成保证，不依赖模型守恒。

爆闪峰值帧由解码后实测确定，本地 03 显现时间轴以该峰值帧为锚点反向对齐（不预设 AI 的爆闪时刻）。

构图实测（960 画布，12% 允许区 [115,843]，已通过出框门）：

| 项 | 01 | 02 |
|---|---|---|
| 源 alpha>128 实体框 | 1069×993 | 830×890 |
| 缩放后实体高 | **345 px（画布 35.9%）** | **345 px（画布 35.9%）** |
| 可视 bbox | x[115,486] y[475,819] | x[521,842] y[475,819] |
| 脚底基线 | y=819 | y=819（差 0px） |
| 四边余量 | 左 12.0% | 右 12.2% |

并集 x[115,842] y[475,819]：上 49.5%、下 14.6%（下半幅留给"地面线"，上半幅留给融合特效）。

03 终帧（单角色，可更大；按 alpha>8 全包围盒含斜置琴身与辉光校验）：

| 03 高度 | 宽度 | 落位 | 四边余量 |
|---|---|---|---|
| 560 | 540 | x[210,749] y[285,845] | 21.9% |
| 620 | 598 | x[181,778] y[225,845] | 18.9% |
| 680 | 655 | x[152,806] y[165,845] | 15.8% |
| **700（选用）** | **675** | **x[142,816] y[145,845]** | **14.8%** |

### 3.1 原工作流约束继承表

初版方案只有 AI 段遵守"角色不出框"，本地合成段漏掉了同一条规则（01 左缘仅 3.1% 边距，违反项目硬规则）。下表为逐条审计结果。

| 原工作流硬约束（出处） | 适用 | 本任务如何落地 |
|---|---|---|
| 固定画布/机位/位置/比例，全程不变（§3） | ✅ | 960×960 单画布；无切镜、无缩放、无重构图 |
| 四边 ≥12% 安全边（Walk v3 / dead v4 硬规则） | ✅ | 允许区 x/y ∈ [115,843]；并集实测左 12.0% 右 12.2% 上 49.5% 下 14.6%；AI 特效层写死"四边 ≥12% 纯黑留边" |
| 脚底锚点固定、角色不平移（§3） | ✅ | 01/02 脚底同基线 y=819（差 0px）；本体不做位移，碎屑与火星属 VFX |
| 不靠逐帧缩放维持包围框（§3 / §12.3） | ✅ | 只在合成的第一步做一次全局缩放（0.348 / 0.388），此后逐帧不再缩放；不按 bbox 逐帧归一化 |
| 装备附件全程完整可见、不触边（§3） | ✅ | 01 最左元素是琴头，始终在允许区内且只向内移动；03 终帧含斜置琴身与辉光一并校验 |
| 身份保护：配色/比例不漂移（§7） | ✅ | 01/02 全程使用原 PNG 像素，不重绘、不调色、不镜像；03 末帧与原立绘逐像素一致 |
| 禁止镜头运动（§3） | ✅ | 全程无推拉摇移；末段原计划的"推近 1.00→1.03"已取消，改为辉光呼吸 + 残余火星 |
| 不得出现"充气"式整体放大（§6.A.8） | ✅ | 不做任何全身缩放动画 |
| G1 输入色键四角 median/std=0（reachapi skill） | ✅（改写） | 本次 AI 不上传参考图，无上传副本环节；改由本地合成底色 (0,159,62)，四角 median (0,159,62)、std=0（实测） |
| G2 输出片基逐帧数值 | ✅（降级） | 合成片逐帧量四角 ROI，报 ΔR/ΔG/ΔB 与最差帧；爆闪覆盖区本就不是绿底，单列说明 |
| G3 交付物颜色 | ✅ | 交付帧与原立绘同底合成比对，主体核心 RGB 不得被改色 |
| 不自动改色 / 不自动重生（G2.6） | ✅ | 色偏只报数值并列为待决项，不自动重生成 |
| 半透明羽化资产不硬化（§7） | ✅ | 03 含 12.5% 半透明像素（霓虹辉光），保留柔边不做阈值硬化 |
| Unity 侧（补边 1108 / PPU16 / clip / 控制器） | ❌ | 本任务不产出引擎资产，不涉及 |
| 逐帧 sprite 抽帧 / GPT 去背分支 | ❌ | 交付物是概念片，不需要透明精灵序列 |

**未决取舍**：1:1 画幅内并排两个等高角色，叠加 12% 边距后主体只能到 345px（35.9%）。要让角色更大只有两条路——① 改 16:9（同时容纳两个等高角色仍受宽度限制，实际改善有限）；② 两人前后错位叠化（破坏"大小比例几乎一致"）。默认按现状执行，如需更大需用户裁决。

## 4. AI 生成词（本轮唯一付费产出的输入）

### 4.1 参数

| 项 | 值 |
|---|---|
| 模型 | `seedance-2-fast` |
| 时长 | **4 秒**（仅中段特效层） |
| 分辨率 / 比例 | `720p` / `1:1` |
| 音频 / 水印 | `false` / `false` |
| seed | `-1` |
| 参考图 | **无**（纯文本生成为黑底特效层，避免角色污染；见 §6 风险） |
| 输出 | `C:/Users/steam/Videos/doubaoVideo/monster_fusion_vfx_v1_4s.mp4` |
| 留档 | `Library/Locus/tmp/monster_fusion_vfx_v1/` |
| 预计费用 | 约 **0.49**（实测：4s=0.48888，6s=0.7308，10s=1.21464） |

### 4.2 提交正文（英文，实际提交这一份）

```text
Create one continuous 4-second abstract VFX clip: the fusion of two energy streams, rendered as a clean 2D game effect layer on pure black. This clip contains NO characters, creatures, faces, bodies, limbs, hands, weapons or instruments of any kind. Only light, energy, particles, smoke and shockwaves appear.

SCENE AND CAUSE:
Two different energy streams arrive at the exact center of the frame at the same moment and collide. The LEFT stream is hard and angular: sharp white shard streaks, straight splinter-like slivers of light, crisp straight-edged slashes flying in horizontally from the left edge. The RIGHT stream is soft and round: billowing pale smoke puffs, soft round blobs of light drifting in horizontally from the right edge. They meet at the exact center and cannot pass through each other. The two streams then wind around each other into one tight rotating spiral, tighten and brighten, and finally collapse inward into a single small brilliant point of light at the exact center. That point flashes once and blows outward into a wide expanding halo: concentric shockwave rings, a spray of small sparks and drifting embers filling the central area. The halo breaks apart and the frame falls back to near-total black.

MOTION READ POINTS, in this order, once only:
0.0-0.6s: the two separate bright streaks first appear at the left and right frame edges, moving toward the center at the same speed, so both arrive together.
0.6-1.2s: collision at the exact center; angular shards and round puffs interpenetrate and begin orbiting each other.
1.2-2.4s: one clearly readable rotating spiral at the center, winding tighter, brighter and more compact.
2.4-3.0s: the spiral collapses into a single small brilliant point of light at the exact center.
3.0-3.5s: one sharp flash at that point, then it blows outward into the expanding halo with two or three concentric shockwave rings and a spray of sparks.
3.5-4.0s: the halo breaks apart into drifting sparks and embers; the frame returns to near-total black.

HARD CONSTRAINTS:
Pure absolute black background (#000000) in every frame. No gradient, no vignette, no fog, no floor, no horizon, no scenery, no texture, no light spill on the background away from the effects. One locked camera and the same square frame throughout: no cuts, no zoom, no pan, no track, no roll, no shake, no reframing. The two incoming streams travel strictly horizontally, never diagonally, and the collision point is the exact center of the frame. Keep at least 12 percent pure black margin at all four edges in every frame, so the expanding halo never reaches the corners and the background stays clean. Keep the effect graphic and hard-edged like a 2D game VFX sprite with crisp sharp edges and strong black-white contrast: not painterly, not volumetric, not photographic, not realistic smoke, not blurred.

NEGATIVE CONSTRAINTS: no characters, no monsters, no creatures, no animals, no humans, no faces, no eyes, no silhouettes of figures, no bodies, no limbs, no hands, no feet, no weapons, no guitar, no hammer, no musical notes, no logos, no text, no letters, no numbers, no UI, no border, no watermark, no signature, no scenery, no ground, no floor, no wall, no sky, no background image, no full-frame white flash, no fade to white, no fade to color, no rainbow, no colored background, no heavy bloom haze filling the frame, no lens dirt, no anamorphic streak across the frame, no film grain, no camera movement, no cut, no zoom, no pan, no roll, no shake, no reframing, no slow motion, no reverse, no loop, no realistic volumetric smoke, no motion blur, no double image, no duplicated streams, no secondary collision.
```

### 4.3 中文对照（审阅用，不提交）

```text
创作一支连续4秒的抽象特效片：两股能量融合的仪式，做成干净的黑底2D游戏特效层。本片不出现任何角色、生物、脸、身体、四肢、手、武器或乐器，只有光、能量、粒子、烟雾与冲击波。

场景与因果：
两股不同的能量同时抵达画面正中央并相撞。左股硬而锋利：锐利的白色碎裂光条、笔直的碎片状光屑、从左边缘水平飞入的清晰直边光刃。右股柔软而圆润：从右边缘水平飘入的翻滚淡色烟团、柔和的圆形光团。二者在正中央相遇且互不穿透，随后互相缠绕成一个紧密旋转的螺旋，越收越紧、越来越亮，最终向内塌缩成中央一个微小而炽烈的光点。该光点闪一次，向外炸开成大范围扩张的光晕：同心冲击波环、四散的小火星与飘散余烬，铺满中央区域。光晕碎裂后画面回到近乎全黑。

动作读点（按此顺序，只发生一次）：
0.0–0.6秒：两道分离的亮线分别从左右画面边缘出现，以相同速度向中心推进，因此同时抵达。
0.6–1.2秒：正中央相撞；锐利碎光与圆润烟团互相穿插，开始互相绕转。
1.2–2.4秒：中心形成清晰可读的旋转螺旋，收紧、增强、变紧凑。
2.4–3.0秒：螺旋塌缩为正中央一个微小炽亮光点。
3.0–3.5秒：该点一次锐闪，然后向外炸开成扩张光晕，带两三圈同心环与火星喷溅。
3.5–4.0秒：光晕碎成飘散火星，画面回到近乎全黑。

硬约束：
每帧都是纯绝对黑底（#000000）。无渐变、无暗角、无雾、无地板、无地平线、无场景、无纹理、无光溢出到背景。全程单一固定机位与同一正方形画面：不切镜、不缩放、不平移、不跟拍、不滚转、不震动、不重构图。两股来流严格水平飞行，绝不斜行；相撞点位于画面正中央。每帧四边至少保留12%纯黑留边，扩张光晕不得触及四角，背景保持干净。效果保持图形化、硬边，像2D游戏特效素材，边缘清晰锐利、黑白对比强烈：不要绘画感、不要体积渲染、不要摄影感、不要写实烟雾、不要模糊。

负面词：无角色、无怪物、无生物、无动物、无人类、无脸、无眼睛、无人物剪影、无身体、无四肢、无手、无脚、无武器、无吉他、无锤子、无音符、无logo、无文字、无字母、无数字、无UI、无边框、无水印、无签名、无场景、无地面、无地板、无墙、无天空、无背景图、无整屏白闪、无淡出为白、无淡出为彩色、无彩虹、无彩色背景、无充满画面的浓雾辉光、无镜头脏污、无横向变形光条、无胶片颗粒、无镜头运动、无切镜、无缩放、无平移、无滚转、无震动、无重构图、无慢动作、无倒放、无循环、无写实体积烟雾、无运动模糊、无重影、无重复来流、无第二次相撞。
```

## 5. 本地合成步骤（不产生费用）

1. **归一化与同台（已完成、已过门）**：先裁到 alpha>8 包围盒，预乘 alpha 后 LANCZOS 缩放（避免透明黑边产生灰边），并迭代校正使两者的 alpha>128 实体高**精确相等**（实测 345 / 345，差 0px）；脚底基线统一 y=819；合成 960×960 绿幕首帧（绿键 RGB 取项目基色 `(0,159,62)`，四角 median 一致、std=0）。产物 `step1_parents_green.png` / `step1_parents_green_margincheck.png`（带 12% 安全边标尺）。
2. **本体崩解**：按 alpha 乘噪声阈值做像素溶解，碎屑向中心流动。**不单独把吉他/锤头抠成飞行物**——二者在同一张 PNG 里与身体重叠，硬切会切穿角色；改为让 01 的碎屑保持硬边白刃状、02 的碎屑保持圆钝团块状，由 AI 特效层的左右两股形态呼应（"特征继承"由 03 终帧自带的吉他/锤形元素完成）。
3. **特效层合成**：解码 AI 成片 → 纯黑底做亮度 matte（保留辉光渐变，不硬化）→ screen 叠到绿底上；爆闪峰值帧实测后作为 03 显现的锚点。特效层受"四边 ≥12% 纯黑留边"约束，因此爆闪只覆盖中央 76% 区域。
5. **03 显现**：用 03 原始 alpha 生成纯黑剪影层，自爆闪中心浮现；再用 03 的彩色原图做由中心向外的遮罩填充；辉光层单独用 screen 叠加保留霓虹感。终帧落位 x[142,816] y[145,845]（高 700、四边余量 14.8%，按 alpha>8 全包围盒含斜置琴身与辉光校验）。
6. **收尾**：辉光呼吸（±3% 亮度）+ 残余火星飘落，**不做任何缩放或推镜**（原计划的 1.00→1.03 推近已取消：既违反"禁止镜头运动"，也会把背景边距从 12% 压到 9.6%）。末帧严格等于 03 原始立绘像素。
7. **输出**：PyAV 编码 H.264 mp4，960×960 / 24fps / 241 帧 / 10.0s；产出接触表与关键帧核对图。

## 6. 风险与退路

| 风险 | 依据 | 退路 |
|---|---|---|
| 纯文本生成的特效层出不来"干净的硬边黑白特效"，变成彩色/写实 | 本项目实测：纯文生图会漂移；色键类素材对硬边敏感 | 追加"白+淡金"限定；仍失败则改 6 秒版本（0.73）；再失败则本地程序化生成能量层（0 成本） |
| AI 层爆闪时刻不可控 | 实测模型节奏浮动（1021 案例起力比预期晚 1.3s） | 以实测峰值帧反向对齐本地时间轴，不预设 |
| 绿幕底被光晕污染，四角不再纯绿 | 已知：光效层必然叠到底色上 | 特效层限制在中央 76% 内、四边保留 12% 纯黑，成片四角仍为纯绿 |
| 03 原始 alpha 12.5% 半透明像素与绿底混合产生绿边 | 实测 03 含霓虹辉光 | 接受为辉光染色，或把 03 局部自发光区单独 screen 处理 |
| 观众/美术认为"AI 只占 4 秒不够天马行空" | — | 可选叠加：AI 额外生成 1 段"父母合体"（角色被强光遮蔽）的 1.5s 剪片作备选（+0.49） |

## 7. 提交纪律（沿用项目既有规则）

1. 生成前把中英文生成词贴给用户过目（已完成，见 §4）。
2. 用户明确"按此发起"后才创建任务；**创建只执行一次**，失败、超时或结果不明均不自动重建。
3. 提交正文只含 §4.2 英文（正文 + 负面词），不提交中文对照，不使用 `negative_prompt` 字段。
4. 轮询遵守 `skill/reachapi-seedance-video-generation-v2.md`：首次立即查、间隔 5–10 秒、最多 12 次、总等待 ≤120 秒、单请求 ≤20 秒。
5. 下载走 `.part` + Range 分段（≤4 路）+ 续传，校验 ftyp / 顶层 box 边界 / 全帧解码后原子改名。
6. 解码后必须实测并报告：实际尺寸、fps、帧数、容器时长、末帧 PTS、文件哈希、usage/cost。
7. 不记录 Key；不把服务端 success 当成本地成片完成；不做 Unity 接入。

## 8. 验收口径（给美术看片前自检）

- 0–3.2s：两个角色**像素级就是美术的原画**，比例一致（主体等高，实测 345/345 差 0px）、脚底同基线（y=819，差 0px）、四边安全边 ≥12%（实测左 12.0% 右 12.2%），无任何 AI 重绘痕迹。
- 3.2–7.2s：能量层为图形化硬边特效，四角始终纯绿，无角色、无文字、无杂色污染；特效不越出中央 76% 区域。
- 7.2s 起：03 从爆闪中浮现，色彩由 03 原画提供，落位四边余量 ≥14.8%，**末帧与 03 原始立绘逐像素一致**。
- 全片 10.0s / 24fps / 241 帧，无跳帧、无闪黑、无音轨。
- 色彩门（G2）：逐帧量四角 64×64 ROI，报 ΔR/ΔG/ΔB 与最差帧号；爆闪覆盖区单列，不与纯绿区混算。

## 9. 复跑入口

- 分析产物与实测脚本：`Library/Locus/tmp/monster_fusion_analysis/`
- 合成产物目录（执行后）：`Library/Locus/tmp/monster_fusion_composite_v1/`
- AI 成片（执行后）：`C:/Users/steam/Videos/doubaoVideo/monster_fusion_vfx_v1_4s.mp4`
