---
id: kd_f26871aa-8bd5-4407-9654-96a1adeaca55
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# Enemy 109 骑兵视觉设计交接文档

## 0. 当前交接状态

本轮工作已完成到“视觉素材验收 + 原始动作帧准备”，用户要求在其他对话继续后续工作。

已确认并可继续使用：

- `MountedIdle` 骑乘形象与 6 帧动画已通过用户验收，并已部署到项目；
- Windup + Charging 视频已通过用户验收；
- 已从该视频准备 6 帧 Windup 与 6 帧可循环 Charging，用户已验收选帧；
- 12 张新动作帧目前仍是原始绿幕 RGB PNG，尚未抠图、导入 Unity 或接入正式 Animator；
- 不重新生成已验收的图片或视频，不重复发起收费任务。

最新 Unity Editor 状态：`disconnected`。Editor Log：`H:/Project/threeKingdomSlayer/threeKingdomSlayer/Logs/Editor.log`。断开前最后已知 Active Scene 为 `Assets/Scenes/Battle.scene`；重新连接后必须重新确认 Editor 状态和 Active Scene，不能把旧状态当作当前状态。

本文件是跨对话恢复用的执行交接文档。它记录已验收事实、已落盘资产、当前接入边界和下一步顺序；不要用旧章节中的历史候选或旧占位结论覆盖本节。

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

### 3.2 当前 Unity 接入状态

当前 Prefab 仍保留旧占位节点，正式资源采用“根 Renderer 直接显示合成人马 Sprite”的过渡接线：

```text
Enemy_109
├─ 根 Enemy / CavalryEnemy / CavalryVisualController / Animator / SpriteRenderer
├─ MountVisual（旧 105 坐骑占位，MountedIdle 时隐藏）
├─ RiderAnchor（旧 101 骑手占位，MountedIdle 时隐藏）
└─ CavalryStateLabel（调试用，正式版待关闭）
```

已落盘并通过 MountedIdle 验证的资源：

- `Assets/Sprites/Enemy/Enemy109/Enemy_109_MountedIdle1.png` 至 `Enemy_109_MountedIdle6.png`：人马合一单层 Sprite；
- `Assets/Animations/Enemy_109_MountedIdle.anim`：6 帧循环 Idle；
- `Assets/Animations/Enemy_109.controller`：从 101 controller 复制并将 Idle motion 替换为 109 MountedIdle，其它未制作状态仍是占位 motion；
- `Assets/Resources/EnemyPrefabs/Enemy_109.prefab`：根 Animator 指向 `Enemy_109`，MountedIdle 时根 SpriteRenderer 显示正式合成图；
- `Assets/Scripts/Enemy/CavalryVisualController.cs`：按 `MountedCombinedIdle / MountedPlaceholder / Dismounted` 路由视觉。

当前真实接线边界：

- `MountedIdle` 已使用正式人马合成图并完成临时实例验证；
- Windup、Charging、Striking、Interrupted、Retreating 尚未接入正式合成帧；
- 非 MountedIdle 骑乘阶段仍会走旧 101 + 105 占位路径；
- 真正 `Dismounted` 后切换 `Assets/Animations/Enemy_1011.controller`，落马视觉仍是临时复用 1011；
- 不要把旧的 101 + 105 节点删除或重定向为正式资产，直到所有骑乘阶段和落马过渡完成验证。

### 3.3 落马视觉

本轮暂定：

- 落马后先暂时复用 `Assets/Animations/Enemy_1011.controller` 及其全套动画；
- 当前 1011 套件包括 `Idle / Walk / Attack / HitFlash / HitLeft / HitRight / Dead / Launched_Rise / Launched_Fall / Launched_Getup`；
- 将来再制作与骑兵身份连续的新步兵视觉替换；
- 替换时应保持 Enemy 通用 API 所需的状态名契约，不应把新步兵动画绑定写死到骑乘逻辑中。

---

## 4. 尺寸、锚点与已部署比例

MountedIdle 已按用户验收版本部署，但这只确认了当前视觉可用，不代表所有动作帧已经完成同样的几何归一化。

当前已知部署事实：

- 源动作视频/选帧：`960×960`；
- MountedIdle 正式 Sprite：统一补边到 `1108×1108`，源图不缩放；
- MountedIdle 导入设置：PPU `16`、Filter `Point`、Uncompressed、无 Mipmap、Alpha 启用、Pivot `(0.65, 0.55)`、Wrap `Repeat`、Max Size `2048`；
- MountedIdle 6 帧使用同一画布尺寸，已完成根 Renderer/Animator 的 Idle 播放验证；
- 当前 Prefab 根 scale、BoxCollider 与血条偏移已经按 MountedIdle 版本存在，不要在未检查 Prefab 现状前直接套用到新动作帧。

动作帧接入前必须重新做：

1. 对 12 张抠图后的帧统一画布、脚底/根位置和安全边距；
2. 确认枪尖、马蹄和红色飘带没有越界或被裁切；
3. 用同一 PPU、Point 过滤和 Pivot 导入；
4. 对 Windup/Charging 的包围盒变化区分“马腿抬起/落地的正常动作”与“根位置漂移”；
5. 只有统一后才能创建正式 Animator Clip。

此前 `k=1.5`、`k=1.6` 都不是独立的最终技术参数；当前应以已验收 MountedIdle 的实际部署比例为基准，不要重新推导或擅自放大。

---

## 5. 已验收视觉生产与动作帧记录

### 5.1 已验收的正式 MountedIdle

用户已验收骑乘形象：

```text
C:/Users/Administrator/Pictures/gptGen/cavalry_lancer_idle_v3_from_dismounted_v1.png
```

该版本确立的视觉身份：

- 人马合一的单层 Sprite，不采用运行时骑手/马合成作为正式美术；
- 短而宽的重装骑手；
- 深蓝灰/炭灰层叠重甲，暖金边与金属件；
- 棕色皮革；
- 红色颈巾、侧布与盔缨；
- 大型深栗棕成年战马；
- 长枪固定在画面左侧，对应骑手解剖学右手；
- 缰绳位于画面右侧，对应骑手解剖学左手。

视频首帧使用的绿幕锚点副本：

```text
C:/Users/Administrator/Pictures/gptGen/cavalry_lancer_idle_v3_from_dismounted_v1_green.png
```

该副本规格为 `1024×1024`、RGB 绿幕 `(0,177,64)`，只替换背景，不改变主体位置与比例。它只作为视频输入锚点，不是 Unity 正式透明素材。

### 5.2 MountedIdle Unity 部署

已部署并完成临时实例验证：

```text
Assets/Sprites/Enemy/Enemy109/Enemy_109_MountedIdle1.png
Assets/Sprites/Enemy/Enemy109/Enemy_109_MountedIdle2.png
Assets/Sprites/Enemy/Enemy109/Enemy_109_MountedIdle3.png
Assets/Sprites/Enemy/Enemy109/Enemy_109_MountedIdle4.png
Assets/Sprites/Enemy/Enemy109/Enemy_109_MountedIdle5.png
Assets/Sprites/Enemy/Enemy109/Enemy_109_MountedIdle6.png
Assets/Animations/Enemy_109_MountedIdle.anim
Assets/Animations/Enemy_109.controller
Assets/Resources/EnemyPrefabs/Enemy_109.prefab
Assets/Scripts/Enemy/CavalryVisualController.cs
```

已验证内容：

- MountedIdle 播放顺序 `1 → 2 → 3 → 4 → 5 → 6 → 1`；
- MountedIdle 时根 Renderer 显示合成人马图，旧 MountVisual/RiderAnchor 隐藏；
- 真实 `Dismount(false)` 后切换到 1011 Idle；
- 本轮没有修改 1011 正式资产；
- 完整真实战斗中的冲锋阶段切换尚未验收。

### 5.3 已验收 Windup + Charging 视频

视频任务只创建过一次，禁止重复创建：

```text
Task ID: task_b388d78b3a784172bcf8fc7dc3b04618
Model: seedance-2-fast
Duration: 4s
Requested resolution: 720p
Actual stream: 960×960, 24fps, approximately 4.04s
Audio: disabled
Watermark: disabled
Last frame: not used
```

视频文件：

```text
C:/Users/Administrator/Videos/doubaoVideo/cavalry_lancer_windup_charge_v1_720p_4s.mp4
```

文件验证：

- `2,782,816 bytes`；
- SHA-256：`a763b35a10096967625edb2583b830e297dfb8dba9e16ca1ef723eef7a237c06`；
- MP4 `ftyp`、`moov`、`mdat` 均存在；
- 97 帧完整解码通过；
- 用户已验收视频动作结果。

实际动作边界：

- f001–f013 基本保持 MountedIdle；
- f014 开始变化；
- f015–f020 是本轮选定的有效 Windup 段；
- 约 f030 后进入稳定 Charging；
- Charging 主要周期约 9 源帧，即约 `0.375s`；
- 视频末段没有回到 Idle，仍处于 Charging 状态。

任务记录与完整参数：

```text
Library/Locus/tmp/cavalry_concept/cavalry_windup_charge_video_v1.runtime.json
Library/Locus/tmp/cavalry_concept/prompt_cavalry_windup_charge_video_v1_combined.en.txt
Library/Locus/tmp/cavalry_concept/prompt_cavalry_windup_charge_video_v1.en.txt
Library/Locus/tmp/cavalry_concept/prompt_cavalry_windup_charge_situation_v1.en.txt
```

### 5.4 已验收的 12 张原始动作帧

用户已验收以下选帧方案。当前文件仍是原始绿幕 RGB PNG，未抠图：

Manifest：

```text
Library/Locus/tmp/cavalry_concept/cavalry_windup_charge_v1_decode/selected_frames_v1/selected_frames_manifest.json
```

Windup 源帧：

```text
f015, f016, f017, f018, f019, f020
```

对应目录：

```text
Library/Locus/tmp/cavalry_concept/cavalry_windup_charge_v1_decode/selected_frames_v1/windup/
```

Charging 源帧及建议循环顺序：

```text
f036 → f038 → f039 → f041 → f042 → f044 → f036
```

对应目录：

```text
Library/Locus/tmp/cavalry_concept/cavalry_windup_charge_v1_decode/selected_frames_v1/charging/
```

选帧验证：

- 共 12 张；
- 全部 `960×960 RGB`；
- 导出文件与解码源帧逐字节一致；
- manifest SHA-256 全部匹配；
- 未缩放、未改像素、未抠图；
- Charging 内部相邻前景差异约 `24.752–37.872`；
- `f044 → f036` 接缝差异 `26.302`，与内部动作变化同量级；
- 下一周期同相位 `f045` 与 `f036` 差异 `7.991`，支持 9 帧周期判断。

接触表：

```text
Library/Locus/tmp/cavalry_concept/cavalry_windup_charge_v1_decode/selected_frames_v1/windup_selected_raw.png
Library/Locus/tmp/cavalry_concept/cavalry_windup_charge_v1_decode/selected_frames_v1/charging_selected_raw.png
Library/Locus/tmp/cavalry_concept/cavalry_windup_charge_v1_decode/selected_frames_v1/windup_charging_selected_raw.png
```

`*_keyed_preview.png` 只是粗略键控预览，不是生产 Alpha，不得直接导入 Unity。

### 5.5 当前未完成边界

以下工作尚未执行：

- 12 张动作帧正式抠图；
- Alpha 边缘、绿边和主体残留检查；
- 抠图后的统一补边、脚底/根位置归一化；
- Windup/Charging 正式 Sprite 导入；
- Windup/Charging AnimationClip 与 Animator 状态接入；
- Striking、Interrupted、Retreating、骑乘死亡独立素材；
- 完整真实波次中的视觉切换、攻击时序和落马回归验收。

---

## 6. 下一次恢复的执行顺序

当前不需要重新设计、重写提示词或重新生成已经验收的视觉。下一次对话应从素材处理和 Unity 接入继续，按以下顺序执行：

### 6.1 先恢复工具与项目状态

1. 重新连接 Unity Editor；
2. 重新确认 Editor 状态、Active Scene、编译状态和 Console；
3. 检查当前工作树，保留用户已有改动，不清理未知的未跟踪文件；
4. 回读 `Assets/Resources/EnemyPrefabs/Enemy_109.prefab`、`Assets/Animations/Enemy_109.controller` 与 `Assets/Scripts/Enemy/CavalryVisualController.cs` 的当前状态；
5. 不把断开前的 Editor 状态或场景状态当作仍然有效。

### 6.2 处理已验收的 12 张动作帧

1. 以 `selected_frames_manifest.json` 为唯一源帧清单；
2. 对 Windup 6 帧和 Charging 6 帧执行正式抠图；
3. 检查透明 Alpha、绿边、半透明污染、枪尖、马蹄、红色飘带和马尾残留；
4. 不改动画内容，不重新抽帧，不使用 `*_keyed_preview.png` 作为生产 Alpha；
5. 统一画布、脚底/根位置、边距和像素尺寸；
6. 对正常抬蹄造成的包围盒变化与根位置漂移分别验收；
7. 记录每张最终 PNG 的尺寸、Alpha 范围、主体包围盒和 SHA-256。

### 6.3 接入 Windup / Charging

1. 将抠图后的帧导入项目合适的 `Assets/Sprites/Enemy/Enemy109/` 子目录；
2. 保持 MountedIdle 已验收的导入规范：Point、无 Mipmap、Alpha 启用、统一 PPU 与 Pivot；
3. 创建 `Enemy_109_Windup.anim`，使用 6 帧连续 Windup，按 `windupDuration = 0.3s` 调整播放速度；
4. 创建 `Enemy_109_Charging.anim`，使用 `f036 → f038 → f039 → f041 → f042 → f044`，循环播放；
5. 用 Animator speed 对齐玩法时长，不用动画长度反向改写 `CavalryEnemy` 的时序；
6. 将 `CavalryVisualController` 的正式路由从“MountedIdle 正式 + 其他骑乘阶段占位”逐步扩展到 Windup/Charging；
7. 保留旧 101 + 105 占位作为未完成阶段的回退，直到真实战斗验收通过。

### 6.4 后续动作与真实战斗验收

Windup/Charging 接入后，再按优先级处理：

1. Striking / Interrupted 的头帧、收招和 Retreat 衔接；
2. Retreating 不转身、不露背；
3. 骑乘中死亡表现；
4. 完整真实节点中的阶段切换、伤害提交、打断、返航、落马和对象池复用；
5. 正式版关闭调试标签，并确认受击、染色、透明度、描边和血条覆盖正确。

### 6.5 落马身份的后续设计

落马后暂时复用 `Assets/Animations/Enemy_1011.controller` 仍是当前接受的技术方案，不阻塞 Windup/Charging 接入。未来若要制作与骑乘身份连续的独立落马步兵，应另开设计任务，重新确认头盔、护颈、胸甲、短剑和阵营识别，不得在本次动作帧接入中擅自替换 1011 正式资源。

---

## 7. 跨对话恢复纪律

- 不重新创建 `task_b388d78b3a784172bcf8fc7dc3b04618`；该视频任务已经成功、下载、解码并由用户验收；
- 不重新生成已经验收的 MountedIdle 图、Windup/Charging 视频或已选帧；
- 不把原始绿幕帧或 `*_keyed_preview.png` 直接导入 Unity；
- 不把新的动作帧覆盖到已验收的 MountedIdle 资产；
- 不修改 `Enemy_1011` 正式资产；
- 不删除旧 MountVisual/RiderAnchor 占位，直到所有正式骑乘阶段和落马流程完成真实节点验证；
- 所有 Unity 结构化资产改动通过 Unity API/Editor 工具完成，不直接改 Unity YAML；
- 每次素材或脚本改动后先回读文件，再进行下一步；
- 外部收费任务、Alpha 处理和 Unity 部署分阶段验收，不要把工具成功回执当作用户视觉验收；
- 保留现有用户改动和未知未跟踪文件，不执行无授权清理。

当前推荐的交接起点是：

> 重新连接 Unity 后，先处理 `selected_frames_manifest.json` 中的 12 张原始绿幕帧；不要回到 Idle 设计或视频生成阶段。

---

## 8. 相关文件索引

状态优先级：本交接文档中的“当前交接状态”“当前 Unity 接入状态”“已验收视觉生产与动作帧记录”优先于其他历史交接或设计文档。`design/cavalry-enemy-animation.md` 与 `design/cavalry-enemy.md` 中仍可能保留 101 + 105 占位阶段的历史描述；它们分别继续作为动画需求矩阵和机制规则参考，但不能覆盖本文件记录的 MountedIdle 已部署、Windup/Charging 已验收以及 12 帧待抠图状态。

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
- `Locus/knowledge/design/art-style-guide.md`

### MountedIdle 正式部署资源

- `Assets/Sprites/Enemy/Enemy109/Enemy_109_MountedIdle1.png` 至 `Enemy_109_MountedIdle6.png`
- `Assets/Animations/Enemy_109_MountedIdle.anim`
- `Assets/Animations/Enemy_109.controller`
- `Assets/Resources/EnemyPrefabs/Enemy_109.prefab`
- `Assets/Scripts/Enemy/CavalryVisualController.cs`

### 已验收视频与动作帧

- `C:/Users/Administrator/Videos/doubaoVideo/cavalry_lancer_windup_charge_v1_720p_4s.mp4`
- `Library/Locus/tmp/cavalry_concept/cavalry_windup_charge_video_v1.runtime.json`
- `Library/Locus/tmp/cavalry_concept/cavalry_windup_charge_v1_decode/selected_frames_v1/selected_frames_manifest.json`
- `Library/Locus/tmp/cavalry_concept/cavalry_windup_charge_v1_decode/selected_frames_v1/windup/`
- `Library/Locus/tmp/cavalry_concept/cavalry_windup_charge_v1_decode/selected_frames_v1/charging/`
- `Library/Locus/tmp/cavalry_concept/cavalry_windup_charge_v1_decode/selected_frames_v1/windup_selected_raw.png`
- `Library/Locus/tmp/cavalry_concept/cavalry_windup_charge_v1_decode/selected_frames_v1/charging_selected_raw.png`

### 已作废或仅供历史追溯的候选

- `C:/Users/Administrator/Pictures/gptGen/cavalry_lancer_idle_v2.png` 仅为早期失败候选，不得作为新基准；
- 早期“没有正式 109 骑乘资源”“当前候选未验收”“暂停期间禁止制作动作帧”的表述已被本交接文档前文覆盖，不得按旧语义恢复。

---

## 9. 最终状态摘要

本轮已完成并验收：

- MountedIdle 正式人马合一 Sprite 6 帧及 Unity 临时接线；
- Windup + Charging 视频；
- 6 帧 Windup 与 6 帧可循环 Charging 的选帧方案。

当前仍待完成：

- 12 张原始绿幕帧的正式抠图与 Alpha 验收；
- 动作帧统一归一化、Unity 导入和 Windup/Charging Animator 接线；
- Striking、Interrupted、Retreating、骑乘死亡的正式素材与真实战斗验收。

> 下一次对话从 `selected_frames_manifest.json` 开始：先抠图并检查 Alpha，再接入 Windup/Charging；不要重新生成已经验收的 Idle、视频或选帧。
