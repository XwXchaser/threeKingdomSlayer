---
id: kd_f26871aa-8bd5-4407-9654-96a1adeaca55
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# Enemy 109 骑兵视觉设计交接文档

## 0. 当前交接状态

本任务在本轮暂停。用户明确要求：

- 不再发起图片生成；
- 不继续制作视频、动画帧、Animator、Prefab 或 Unity 代码；
- 不执行下一步计划任务；
- 后续在新的对话中从本文件恢复讨论。

当前 Unity Editor 状态：`editing`，Active Scene 为 `Assets/Scenes/Battle.scene`。本轮没有修改 Unity 资产、Prefab、场景、Animator 或运行时代码。

本文件是设计交接记录，不代表骑兵视觉已经通过，也不代表后续方案已经拍板。

---

## 1. 任务目标

为敌人 109 制作一套符合项目新画风的重装长枪骑兵视觉：

- 骑乘状态：骑手持长枪，与马绘制成**一张人马合一的单层 Sprite**；
- 落马状态：暂时复用已部署的 1011 步兵动画与短剑形象；未来再制作与骑兵身份连续的新步兵套；
- 骑乘与落马是两套不同的动画/视觉单位，而不是把骑手和马分别生成后再运行时合成；
- 骑乘形象必须保留高清像素化三国战场风格，但不能成为 1011 的简单换武器、换坐骑版本。

核心画风依据：

- `Locus/knowledge/design/art-style-guide.md`
- `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle1.png` 至 `Enemy_1011_idle6.png`
- 用户提供并已保存的概念参考：`C:/Users/Administrator/Pictures/gptGen/sword_enemy_concept_v1_sunburst.png`

旧的 101、103、105 素材只能作为技术或兵种对照，不能定义新画风。

---

## 2. 已确认的 109 玩法与动画约束

权威机制文档：`Locus/knowledge/design/cavalry-enemy.md`。

骑乘期间的唯一主动行为是冲锋：

```text
MountedIdle → Windup / Charge → Striking 或 Interrupted → Retreating → MountedIdle
```

已确认规则：

- 骑乘期没有普通攻击；
- 可从任意 `row >= 1` 发起冲锋；
- 冲锋伤害按发起排计算；
- 冲锋路径遇到敌人时，在其后一排前探命中，然后返航；
- 只有标记为可打断骑兵冲锋的攻击可以打断；
- Parry 当前不打断骑兵冲锋；
- 打断发生在伤害提交前时，本次不结算伤害，收招后返航；
- 骑乘期免疫眩晕、击退和一般位移；
- 真正进入 `Enemy.Launch()` 后永久落马，恢复普通敌人受控逻辑；
- 落马后的死亡表现不改，沿用普通敌人的通用死亡流程；
- 骑乘中死亡需要独立的骑兵死亡表现，不沿用普通敌人“弹起、旋转、掉出屏幕”的通用表现。

当前 Prefab 参数中曾确认的玩法时长：

- `windupDuration = 0.3s`
- `moveSpeed = 0.2s/排`
- `strikeWindup = 0.18s`
- `strikeRecover = 0.22s`
- `interruptRecover = 0.2s`
- `chargeCooldown = 1s`

动画制作方面已确认的压缩方案：

1. Windup 与 Charge 使用同一个视频任务生成；视频前段切为 Windup，稳定循环段切为 Charge；
2. Interrupted 的头帧复用 Charge 的头帧；
3. Strike 的头帧复用 Charge 的头帧；
4. Interrupted 和 Strike 结束后直接切 Retreat，不要求回到 Charge 的原始姿态；
5. Strike 的收尾与 Retreat 的衔接可以使用额外的过渡处理解决，不要求为了衔接再制作独立完整动作；
6. Retreat 不做 180° 转身，不露背。

---

## 3. 当前视觉架构结论

### 3.1 不采用骑手/马分层合成

用户已经明确：骑手和马分别制作、再合并的方式困难，且两者实际上接近两套不同单位。因此不再把“骑手 Animator + 坐骑 SpriteRenderer”作为正式美术生产路线。

正式骑乘美术应当是：

```text
一张人马合一的骑乘 Sprite
一个骑乘 Animator / 一套骑乘 Clips
```

这张 Sprite 内部同时包含：

- 骑手；
- 战马；
- 鞍具与鞍毯；
- 长枪；
- 缰绳；
- 骑乘状态下的全部装备。

### 3.2 当前 Unity 仍是旧占位架构

当前 `Assets/Resources/EnemyPrefabs/Enemy_109.prefab` 仍然是旧占位：

```text
Enemy_109
├─ 根 Enemy / CavalryEnemy / CavalryVisualController
├─ 根 SpriteRenderer（当前关闭）
├─ MountVisual（105 占位 Sprite）
├─ RiderAnchor（101 占位 Sprite 镜像）
└─ CavalryStateLabel
```

当前仍使用 `Assets/Animations/Enemy_101.controller` 作为根 Animator 来源，并通过 `CavalryVisualController` 把骑手 Sprite 镜像到子节点。该结构没有被本轮改造，也不能视为最终接线。

当前项目中没有正式的 `Enemy_109_*` 骑乘动画资源。

### 3.3 落马视觉

本轮暂定：

- 落马后先暂时复用 `Assets/Animations/Enemy_1011.controller` 及其全套动画；
- 当前 1011 套件包括 `Idle / Walk / Attack / HitFlash / HitLeft / HitRight / Dead / Launched_Rise / Launched_Fall / Launched_Getup`；
- 将来再制作与骑兵身份连续的新步兵视觉替换；
- 替换时应保持 Enemy 通用 API 所需的状态名契约，不应把新步兵动画绑定写死到骑乘逻辑中。

---

## 4. 尺寸与比例决策状态

曾经提出并暂定过骑兵视觉高度倍率 `k = 1.5`，即骑乘人马主体在游戏中约为普通步兵可见高度的 1.5 倍。

但在看到当前候选后，用户提出：

> 马看起来太矮，似乎把比例改为 1.6 会更好。

因此当前状态是：

- `1.5`：此前的工作假设，不是最终确认；
- `1.6`：用户提出的新候选方向，尚未确认，也没有据此重算素材或修改 Prefab；
- 不能在下一次对话中直接把 `1.6` 当作已经拍板的技术参数。

重要原则：先确定形象比例，再决定画布、素材像素高度、Unity scale、碰撞体和血条位置。不要先用技术尺寸反推一个尚未通过的骑兵形象。

此前用于说明的 1108×1108、蹄线约 19%、骑乘可见高度约 60–65% 等，只是部署参考，不是当前候选必须遵守的最终美术规格。

---

## 5. 已生成候选与真实验收结果

### 5.1 生成文件

当前最近一次真实生成候选：

```text
C:/Users/Administrator/Pictures/gptGen/cavalry_lancer_idle_v2.png
C:/Users/Administrator/Pictures/gptGen/cavalry_lancer_idle_v2.response.json
```

生成参数：

```text
model: gpt-image-2.5-sunburst
endpoint: /v1/images/edits
quality: high
input_fidelity: high
size: 1024x1024
background: transparent
output_format: png
reference_count: 2
```

使用的参考图：

```text
C:/Users/Administrator/Pictures/gptGen/sword_enemy_concept_v1_sunburst.png
Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle1.png
```

原始请求提示词与中文对照：

```text
Library/Locus/tmp/cavalry_concept/prompt_cavalry_idle_v2.en.txt
Library/Locus/tmp/cavalry_concept/prompt_cavalry_idle_v2.zh.md
```

### 5.2 机器验收

已确认：

- HTTP 200；
- PNG 1024×1024；
- PNG Color Type 6；
- RGBA；
- 四角 Alpha 均为 0；
- 完全透明像素 771,890；
- 半透明像素 37,158；
- 不透明像素 239,528；
- 主体包围盒约为 `x=49..1010, y=32..996`。

不合格项：

- 左右、上下留白远低于动画安全要求；
- 主体几乎贴近画布边缘；
- 背景存在明显红黑环境光/光晕污染；
- 半透明边缘中包含较多背景污染；
- 不能直接作为正式 Unity Sprite 或后续视频的稳定基准帧。

因此当前候选只能标记为：

> 已生成候选图；Alpha 一级通过；二级干净透明与动画构图验收不通过；未获得用户认可；不得作为正式美术基准。

### 5.3 用户反馈

用户明确否决当前候选的主要问题：

1. **骑兵整体造型与 1011 太相似。** 当前看起来像“1011 重甲步兵骑到马上、换成骑枪”的变体，缺乏独立兵种识别度；
2. **马太矮。** 当前视觉关系中骑手显得过高，马的身体存在感不足；用户认为把整体比例改为约 `1.6` 可能更合适；
3. 用户进一步提出：重新设计前，可能应该先确定骑兵落马后的步兵状态形象，以便先确立骑兵本人的身份、装备、盔甲和武器连续性。

当前候选中的“马面甲、骑手蓝灰甲、红色布料、长枪”等元素不能自动视为已被接受的正式设计。

---

## 6. 当前真正需要解决的设计问题

下一次继续时，不应直接重写提示词或继续生成。先回答以下问题：

### 6.1 是否先做落马步兵形象

这是目前最重要的顺序问题。

推荐先设计一张“落马后的短剑步兵 Idle 形象”，原因：

- 当前落马后暂时复用 1011，只是临时技术方案；
- 骑兵未来必须能解释为“同一个人落马后仍然成立”；
- 如果先设计骑乘形象，很容易再次做成“1011 穿甲骑马”；
- 先确定步兵形象，可以明确：头盔、护颈、肩甲、胸甲、腰带、红布、皮革、脸部/面甲、短剑持握侧和阵营识别色；
- 再把这些身份元素扩展成骑乘形态，同时增加骑枪、马具和骑乘姿态；
- 落马后由长枪切换到短剑的武器变化也更容易设计成有意的身份转换，而不是随机换武器。

### 6.2 骑兵与 1011 的区分必须来自结构，不只是换武器

后续骑兵设计至少需要在以下结构中与 1011 形成明显差异：

- 头盔轮廓；
- 面甲/护颈形制；
- 肩甲层数与外轮廓；
- 胸甲与腰甲布局；
- 红布的形态和位置；
- 长枪/短剑的持握关系；
- 马具和骑乘姿态；
- 骑手身体重心与坐骑比例。

不能只改：

- 1011 的剑为枪；
- 1011 的腿下加一匹马；
- 1011 的红色围巾换成红鞍毯。

### 6.3 马的比例必须先从形象设计确定

当前问题不是单纯把图片整体放大。需要重新设计：

- 马头相对骑手头部的大小；
- 马胸宽度与厚度；
- 马颈高度；
- 鞍座位置；
- 骑手骨盆与马背的接触关系；
- 马蹄线到骑手盔顶的总高度；
- 马在正面剪影中占据的视觉面积。

`k=1.6` 可以作为候选部署比例，但不能替代马与骑手本身的结构设计。

---

## 7. 后续恢复时必须遵守的生产纪律

本交接暂停期间不要执行以下动作：

- 不重新生成骑兵图片；
- 不重试相同提示词；
- 不把当前候选复制进 `Assets/`；
- 不制作骑乘视频；
- 不制作动画帧；
- 不修改 `Enemy_109.prefab`；
- 不创建 `Enemy_109.controller`；
- 不更新正式 Sprite 或 Animator 引用。

恢复后必须先完成：

1. 选择“先做落马步兵形象”或“直接重做骑乘形象”的顺序；
2. 确定骑兵与 1011 的结构性差异；
3. 确定马与骑手的高度、宽度和坐骑接触关系；
4. 重新写一版只描述形象方向的 Idle 提示词；
5. 用户确认提示词和方向后，才考虑发起下一次候选生成；
6. 生成候选后先做用户视觉验收，再进入动画视频流程。

---

## 8. 相关文件索引

### 机制与动画设计

- `Locus/knowledge/design/cavalry-enemy.md`
- `Locus/knowledge/design/cavalry-enemy-animation.md`
- `Locus/knowledge/design/art-style-guide.md`
- `Locus/knowledge/design/pixel-character-and-enemy-animation-spec.md`

### 生成与验收流程

- `Locus/knowledge/skill/gpt-image-generation.md`
- `Locus/knowledge/skill/workflows/image-asset-generation.md`
- `Locus/knowledge/skill/workflows/character-hit-animation-video-workflow.md`
- `Locus/knowledge/skill/workflows/local-green-screen-cutout.md`

### 已确认的新画风基准

- `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle1.png`
- `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle2.png`
- `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle3.png`
- `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle4.png`
- `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle5.png`
- `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle6.png`
- `Assets/Animations/Enemy_1011.controller`

### 当前候选与提示词

- `C:/Users/Administrator/Pictures/gptGen/cavalry_lancer_idle_v2.png`
- `C:/Users/Administrator/Pictures/gptGen/cavalry_lancer_idle_v2.response.json`
- `Library/Locus/tmp/cavalry_concept/prompt_cavalry_idle_v2.en.txt`
- `Library/Locus/tmp/cavalry_concept/prompt_cavalry_idle_v2.zh.md`

---

## 9. 最终状态摘要

当前没有通过验收的 109 骑兵正式形象。

当前唯一有效结论是：

> 109 应当采用 1011 同世界的新画风，但必须重新设计身份结构；骑乘形态应为人马合一的单层 Sprite；当前候选与 1011 区分度不足，马体比例不足，不能继续作为生产基准。下一次对话应先解决“是否先确立落马步兵形象”的设计顺序，再决定新的骑乘形象方向。
