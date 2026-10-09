---
id: kd_f26871aa-8bd5-4407-9654-96a1adeaca55
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# Enemy 109 骑兵视觉设计交接文档

## 0. 当前交接状态（2026-10-09 更新）

**最新：v6边界控制版已生成并通过全帧边界检查，18张6+6+6原始关键帧已导出，等待用户动作帧验收。** task `task_1bcb08e0d51d4df8bd09c9388f4b7d4b`；全145帧最低边距48/98/70/60px，要求32px。核心伤害组保留f075/f076/f078/f080/f081/f082；v5及12/24帧保留为历史。详见§10.14。

| 项目 | 实际状态 | 下一会话边界 |
|---|---|---|
| MountedIdle | 6 张正式 Sprite + Idle Clip 已验收并部署 | 不重做、不覆盖 |
| Windup / Charging | 视频、选帧、12 张 GPT 去背结果均获用户验收；已补边并导入 Unity | 去背与素材导入完成，不再重复 |
| Windup / Charging 动画接线 | 未创建新 Clip，未改 Animator、Prefab、CavalryVisualController | 用户要求等下一段动画完成再部署 |
| Striking v1 视频 | 下载与解码成功，但用户否决：没有正朝玩家刺击 | 保留为失败参考，不去背、不部署 |
| 正面刺击尾帧 v2 | 生图成功；用户认为冲击力不足，未获最终验收；随后改变动作方向 | 不再作为当前视频的尾帧，不沿用旧 Prompt 自动生成 |
| 最新动作方向 | **马体向画面右侧侧身，骑手上身/面甲/视线面向玩家，长枪朝玩家纵深刺击**；随后收枪、完整回身并背向返航 | 单层人马 Sprite 不等于所有部位完全同向；回身返航不在当前视频范围 |
| 右侧身尾帧 v3 / v4 / v5 | v3_attempt2 朝向错误被否决；v4 整体正确但弯枪；v5 枪杆修正后已验收 | 当前 last_frame 只用 v5，不继续生成尾帧 |
| Striking video v2 | 已下载/解码但用户否决：举枪后指向玩家，不是刺击 | 保留为失败样本，不去背/部署 |
| Striking video v3 | task_711a9851c69a44a39ccd1a56162dd505 技术完成但用户否决：仍然指向而非刺击 | 保留失败样本，不重建/去背/部署 |
| Striking video v4 六秒视频 | 技术完成但用户否决：先转身再摆枪，不是同步刺击 | 保留失败样本，不重建/去背/部署 |
| Striking video v5 六秒视频 | task_4207390cad1d4fbc83e0a40607a8ea71 曾获一般视频验收；但刺击中段越界，不满足当前关键帧需求 | 原片保留作历史，不覆盖 |
| Striking v5 关键帧 | 旧12/24帧均未满足f075–f082六帧伤害段 | 不用于当前6+6+6交付 |
| Striking video v6 | task_1bcb08e0d51d4df8bd09c9388f4b7d4b 技术完成；全帧边界通过，18帧已按6+6+6导出 | 等待用户查看帧序；无去背/Unity接线 |


最新 Editor 公告为`editing`，Active Scene：`Assets/Experiments/CurvedScroll/E0OuterGatePreview.unity`。Editor Log：`H:/Project/threeKingdomSlayer/threeKingdomSlayer/Logs/Editor.log`。24帧扩展仅文件处理，未更改Editor状态、切场景或保存/修改预览场景；前轮Play Mode和Console观察为历史。

当前 Git 分支：`route-scroll-movement`。12 张新动作 PNG 及对应 `.meta` 尚为未跟踪文件；工作树还有用户的场景、Stage01、项目设置、APK、里程碑等改动与未知残留，全部保留，不做清理/覆盖/提交。

**恢复起点：查看v6边界通过的18帧6+6+6交付（§10.14）。** v6已只创建1次并完成技术验证；当前只等待用户动作帧验收，无去背/Unity接线。

状态优先级：本节、§9–10 优先；§2.1、§5.7–5.9、§6 保留前序阶段记录，遇到骑手/枪朝向或制作状态冲突以最新补充为准。旧 Design/runtime 的 `pending_cutout`、`pending_review`、禁止转身等历史描述不能覆盖用户确认。

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
6. **最新用户确认已覆盖旧规则**：人马先向画面右侧转至侧身刺击，再收枪并继续约 180° 回身，允许露背，背向玩家返航。

### 2.1 最新已确认的动作方向与未解决问题

用户认为正面尾帧 v2 冲击力不足，提出“敌人冲刺到目标位置，停下，向右侧身刺出一击；先完成一半转向，便于回身返回”。在明确“向右按画面右侧、人马一起转、是否允许露背”的选项后，用户选择了 **完整回身返航**。

当前目标链：

```text
正面 Charging
→ 到目标位置急停 / 人马原地转向画面右侧
→ 右侧身刺击到位（下一张图应是这一姿势的明确尾帧）
→ 收枪、继续同方向回身至背面
→ 背向玩家原路返航
→ 回到 MountedIdle（末端如何转回正面尚未设计）
```

- 转向的是骑手和马这一整个单位，不是只扭骑手上半身，也不是镜像现有正面 Sprite。
- 下一张尾帧仍应是**最大出枪、尚未收枪的侧身命中姿势**；不是背向返航姿势，也不是低枪戒备。
- 骑乘素材继续采用人马合一单层 Sprite；朝向变化不是恢复骑手/马分层架构的授权。
- 当前代码仍对同列正前方目标提交伤害。纯屏幕向右刺与逻辑目标轴线可能不一致，**尚未决定最终枪轴角度/目标投影**；准备关键词时明确这一点，不擅自改成攻击邻列或修改命中范围。
- 当前 0.18s 前探 / 0.22s 收招 / 0.2s 打断恢复未改。明显急停停顿、转身占用时长、前探位移是否调整，须在后续部署前确认，不能用生成视频时长代替玩法时长。
- 新方案需要回身过渡、背向返航循环；返航完成后转回正面 Idle、RetreatBlocked 等待姿态、被打断时避免播放成功刺击的路径仍未设计/制作。
- `Locus/knowledge/design/cavalry-enemy-animation.md` 仍有旧“倒退、不做 180° 转身”和分层占位建议；**本次转向批准已确认但尚未回写 Design、未实现代码或 Animator**。下一会话按本节理解新方向。

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
- Windup / Charging 的 12 张正式合成 Sprite 已导入，但尚未创建 Clip 或接线；Striking / Interrupted / 回身 / 背向返航尚无已验收动画素材；
- 非 MountedIdle 骑乘阶段仍会走旧 101 + 105 占位路径；
- 真正 `Dismounted` 后切换 `Assets/Animations/Enemy_1011.controller`，落马视觉仍是临时复用 1011；
- 不要把旧的 101 + 105 节点删除或重定向为正式资产，直到所有骑乘阶段和落马过渡完成验证。

- `Assets/Animations/` 当前 109 专用 Clip 只有 `Enemy_109_MountedIdle.anim`；`CavalryVisualController.GetVisualMode()` 仍仅在 MountedIdle 选择正式合成路线，其他骑乘阶段继续走占位。
- 本轮只完成 12 张 PNG 的补边与导入，没有改 `Enemy_109.controller`、`Enemy_109.prefab`、`CavalryVisualController.cs` 或玩法时序。素材导入不等于动画部署。

### 3.3 落马视觉

本轮暂定：

- 落马后先暂时复用 `Assets/Animations/Enemy_1011.controller` 及其全套动画；
- 当前 1011 套件包括 `Idle / Walk / Attack / HitFlash / HitLeft / HitRight / Dead / Launched_Rise / Launched_Fall / Launched_Getup`；
- 将来再制作与骑兵身份连续的新步兵视觉替换；
- 替换时应保持 Enemy 通用 API 所需的状态名契约，不应把新步兵动画绑定写死到骑乘逻辑中。

---

## 4. 尺寸、锚点与已部署比例

MountedIdle 与已验收 Windup/Charging Sprite 已采用同一尺寸/导入约定；Windup/Charging Clip、真实阶段切换和战斗播放仍未部署。

当前已知部署事实：

- 源动作视频/选帧：`960×960`；
- MountedIdle 正式 Sprite：统一补边到 `1108×1108`，源图不缩放；
- 实测导入设置：PPU `16`、Point、Uncompressed、无 Mipmap、Alpha Transparency 开启、Tight Mesh、NPOT None、Wrap Repeat、Max Size `2048`。
- **有效锚点纠正**：`.meta` 存储的 `spritePivot` 是 `(0.65,0.55)`，但 alignment 为 `Center (0)`；实际 Sprite Pivot 是 `(554,554)`，归一化 `(0.5,0.5)`。12 张新动作 Sprite 已复制 MountedIdle 完整设置并在 Unity 验证；不要改成 Custom `(0.65,0.55)`。
- MountedIdle 6 帧使用同一画布尺寸，已完成根 Renderer/Animator 的 Idle 播放验证；
- 当前 Prefab 根 scale、BoxCollider 与血条偏移已经按 MountedIdle 版本存在，不要在未检查 Prefab 现状前直接套用到新动作帧。

本轮已完成的尺寸处理：

1. 12 张已验收 GPT 透明帧全部为 960×960 RGBA。
2. 原图完整 1:1 粘贴在 1108×1108 透明画布 `(74,120)` 处；没有缩放、逐帧裁切或按包围框居中。
3. 使用无 mask 的 `Pillow.Image.paste` 保留原图全部 RGBA，包括 Alpha=0 下的 RGB；新增补边环全零。这与已验收 MountedIdle 六帧的实际处理逐像素一致。
4. 每张回读验证源窗口逐像素相等、PNG 哈希与报告匹配、对应 `.meta` 存在；Unity 中 12/12 可加载为 Sprite。
5. 马蹄随疾驰抬落的相位变化仍保留，不通过逐帧钉脚抹去。新侧身/背面姿态需单独检查画布容纳与比例，禁止仅按总包围框 fit 改变身体尺寸。

报告：`Library/Locus/tmp/cavalry_concept/windup_charge_deploy_1108_report.json`，已回读确认 `unity_import_done=true`、`animation_deployment_deferred=true`。

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
- 正面 Idle/Charging 参考中，长枪位于画面左侧、由解剖学右手持握；缰绳位于画面右侧、由解剖学左手持握；
- 这是正面参考的持握归属，不是新动作全程枪尖必须停在画面左侧的规则。侧身/背面投影允许变化，但仍保持右手持枪、左手握缰，不换手、不用镜像偷换。

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

用户已验收以下选帧方案。这些路径保留的是未经处理的原始绿幕 RGB PNG；对应的 GPT 去背结果已另存、验收并导入（见 §5.7），不能据此误判整个去背阶段仍未完成：

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

### 5.5 历史本地抠图候选（未通过，非当前生产路线）

前一阶段曾生成三个阶段的本地候选。它们都没有通过最终验收，不是现在已导入的 GPT 结果：

1. `cutout_batch_v1`（原始本地配方批处理，12 张 960×960 RGBA）：`cutout_batch_v1/cutout_batch_report.json`；
2. `cutout_batch_v2`（提高噪声底 + 幽灵清除，`GHOST_ALPHA=0.6 / GHOST_RADIUS=5`，12 张 960×960 RGBA）：`cutout_batch_v2/cutout_batch_v2_report.json`；
3. `normalized_1108_v1`（按 `(74,120)` 固定补边到 1108×1108，未缩放未裁剪）：`normalized_1108_v1/normalized_1108_report.json`。

已核实的机器结果：

- 12 张原始输入帧哈希未变；
- 三个阶段各自 12 张输出全部存在，且 SHA-256 与报告匹配；
- v1/v2 全部为 960×960 RGBA（PNG Color Type 6）；
- 归一化后全部为 1108×1108，透明区 RGB 全零，固定偏移全部为 `(74,120)`；
- v2 每帧清除了约 `994–8,731` 个远离主体的低 Alpha 压缩幽灵；
- 归一化没有改变 Alpha 或任何可见像素，差异只发生在原本已透明、但 RGB 非零的像素。

### 5.6 历史本地算法缺陷（未修复，但不再阻塞已验收成品）

本轮只读诊断确认了以下真实缺陷，`cutout_batch_v2` 与 `normalized_1108_v1` 因此不能作为最终素材：

1. **“封闭高光填充”没有生效。** `run_cavalry_local_cutout_calibration_v1.py` 的 `reach_border()` 使用指数跳步（`step = min(step * 2, 512)`），不是严格逐像素洪泛。只读合成测试：被实心边界包围的区域应有 144 个不可达像素，脚本判定为 0，且会跨越实心障碍。所以 v2 报告里的 `holes_filled_px = 0` 不能证明没有封闭高光区域。
2. **内部浅色/金属区域仍然半透明。** 重新测量 v2 输出：12 帧浅色深层区域（lum 120–215、chroma<45）平均 Alpha 仅约 `0.909–0.943`，大量像素低于 0.99；已验收的 `Enemy_109_MountedIdle1.png` 与 `Enemy_1011_idle1.png` 同口径平均约为 `0.992`。金属与高光区必须 α≈1。
3. **边缘邻接指标无效。** `metrics()` 中 `neighbor = ero(opaque, 1) & dil(ring, 1)` 经常是空集，`inner_neighbor_comp_gEx` 长期为 `null`，不能用作边缘质量证据。
4. **报告状态口径错误。** `cutout_batch_v2_report.json` 写了 `"production_cutout_complete": true`，而 review 状态仅且是 `batch_cutout_v2_ready_for_visual_review`；不能把“算法输出完成”当成“生产抠图完成”。

后续本对话已改用 GPT 语义去背并完成用户验收/导入，**不再把修本地脚本作为当前恢复起点**。不要导入本地 v1/v2/normalized 候选。

补充诊断：严格洪泛虽能找出真实封闭区域，但部分浅色内部沿半透明链与画外连通，只修 `reach_border()` 不保证内部不透明度合格。当前无需继续堆本地键控参数。

### 5.7 已验收 GPT 去背结果与 Unity 导入

- 先做 f015 单帧 GPT 探针并经用户验收，再批量其余 11 帧；成功候选的缩放离群帧也按用户批准重试，全部版本保留。
- 最终结果目录：`C:/Users/Administrator/Pictures/gptGen/cavalry_windup_charge_cutout_batch_v1/`。
- 逐帧采用清单：该目录 `batch_v1_candidate_selection.json`。后续必须读取 `selected_output_path`，不要猜文件名；部分采用文件含 `_attempt2` 或 `_attempt3`。

| 正式 Sprite | 源帧 | 采用尝试 |
|---|---|---|
| `Assets/Sprites/Enemy/Enemy109/Enemy_109_Windup1.png` | f015 | a1（探针复用） |
| `Assets/Sprites/Enemy/Enemy109/Enemy_109_Windup2.png` | f016 | a2 |
| `Assets/Sprites/Enemy/Enemy109/Enemy_109_Windup3.png` | f017 | a3 |
| `Assets/Sprites/Enemy/Enemy109/Enemy_109_Windup4.png` | f018 | a3 |
| `Assets/Sprites/Enemy/Enemy109/Enemy_109_Windup5.png` | f019 | a1 |
| `Assets/Sprites/Enemy/Enemy109/Enemy_109_Windup6.png` | f020 | a1 |
| `Assets/Sprites/Enemy/Enemy109/Enemy_109_Charging1.png` | f036 | a3 |
| `Assets/Sprites/Enemy/Enemy109/Enemy_109_Charging2.png` | f038 | a1 |
| `Assets/Sprites/Enemy/Enemy109/Enemy_109_Charging3.png` | f039 | a1 |
| `Assets/Sprites/Enemy/Enemy109/Enemy_109_Charging4.png` | f041 | a2 |
| `Assets/Sprites/Enemy/Enemy109/Enemy_109_Charging5.png` | f042 | a1 |
| `Assets/Sprites/Enemy/Enemy109/Enemy_109_Charging6.png` | f044 | a2 |

最终机器结果（用户已看图验收）：

- 全部 RGBA / PNG Color Type 6；深层内部 Alpha 约 0.9920–0.9926，与已验收 MountedIdle 约 0.9922 同级。
- 源高/输出高偏差最大 2.56%，中位 0.81%；无 >5% 离群帧。f038 的底部差约 −22px，仍由用户验收，不能在后续部署中擅自逐帧锚脚。
- 源图按固定 `(74,120)` 原像素补到 1108，并经 Unity API 导入/设置，不改 Unity YAML。
- Unity 现场验证 12/12 Sprite：1108×1108、PPU 16、有效 Pivot (554,554)；设置与 MountedIdle 一致。
- 先前中断的 `KeyError: source_frame` 已修复并实际重跑，所有 12 张导入完成；不是仅写出 Windup1 的旧状态。
- 只导入图片与 `.meta`，没有创建 Windup/Charging Clip、修改 Animator/Prefab/脚本；用户要求等后续动画制作完成再部署。

验证与审阅文件：

- `C:/Users/Administrator/Pictures/gptGen/cavalry_windup_charge_cutout_batch_v1/batch_progress_v1.json`
- `C:/Users/Administrator/Pictures/gptGen/cavalry_windup_charge_cutout_batch_v1/batch_v1_candidate_selection.json`
- `C:/Users/Administrator/Pictures/gptGen/cavalry_windup_charge_cutout_batch_v1/batch_v1_12frames_acceptance_report.json`
- `C:/Users/Administrator/Pictures/gptGen/cavalry_windup_charge_cutout_batch_v1/batch_v1_12frames_white.png`
- `C:/Users/Administrator/Pictures/gptGen/cavalry_windup_charge_cutout_batch_v1/batch_v1_12frames_dark.png`
- `C:/Users/Administrator/Pictures/gptGen/cavalry_windup_charge_cutout_batch_v1/review_pairs_white.png`
- `C:/Users/Administrator/Pictures/gptGen/cavalry_windup_charge_cutout_batch_v1/review_pairs_dark.png`
- `C:/Users/Administrator/Pictures/gptGen/cavalry_windup_charge_cutout_batch_v1/review_charging_strip_white.png`
- `Library/Locus/tmp/cavalry_concept/windup_charge_deploy_1108_report.json`
- `Library/Locus/tmp/cavalry_concept/deploy_cavalry_windup_charge_1108_v1.py`

路由/记录教训（不更改 Skill）：

- 本轮 `r2.52image.xyz` 多次返回真实 RGBA，与旧 Skill 的按 host 判失败结论不符；**只认实际 PNG/Alpha**。
- `asset.ai666.live` 曾返回 RGB、Alpha 全 255、画入背景，usage 的 image_tokens=0；失败时中止并经用户授权后重试，不应连续付费盲试。
- `cdn.jd23kjs.work` 也返回过真实 RGBA，但某次缩放仍超容差；路由技术成功不等于姿态/尺寸验收。
- 批量阶段记录 20 次 POST，另有 1 次请求读超时、服务端结果/计费不明；此前会话按最保守口径称共发出 21 次（不含先行 f015 探针）。不要把 token usage、HTTPError 或客户端超时直接当已计费金额。

### 5.8 Striking v1 视频（用户否决，保留失败参考）

- Task ID：`task_193cf338cbf846a783ad47296ba7f0ef`；只创建一次，已 success、下载、完整解码。
- 模型/规格：seedance-2-fast、4s、720p、1:1、无音频、无水印、seed −1；仅 Charging1 `first_frame`，没有 last_frame。
- 文件：`C:/Users/Administrator/Videos/doubaoVideo/cavalry_lancer_striking_v1_720p_4s.mp4`。
- 实测：2,297,494 bytes，960×960、24fps、97 帧、约 4.04s；SHA-256 `501513c4199cf87aa5450a88ceccddcd508d31b08c5ad64679ae4c560f127b4a`。
- API usage：completion_tokens / total_tokens 87300；cost.spend 原值 0.48888（不推断币种）。
- 用户明确“不通过，角色完全没有正朝玩家进行刺击”。抽帧约 f021–f038 可见长枪仍朝画面左下伸展，未形成纵深刺击。
- 旧 Prompt 过度要求“枪尖/枪杆保持画面左侧且完整可读”，缺乏明确命中姿态锚点；该失败原因只供复盘，不应再继续沿用此视频。
- **未选生产帧、未去背、未导入 Unity**；不要因 MP4/解码通过把它变成已验收素材。

相关记录：

- `Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v1.request.json`
- `Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v1.create_response.json`
- `Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v1.runtime.json`（已记录用户 rejected）
- `Library/Locus/tmp/cavalry_concept/prompt_cavalry_striking_v1.en.txt`
- `Library/Locus/tmp/cavalry_concept/prompt_cavalry_striking_v1.zh.txt`
- `Library/Locus/tmp/cavalry_concept/striking_v1_rejected_review/striking_v1_actual_motion_contact_sheet.png`

### 5.9 正面刺击尾帧 v2（冲击力不足，方向已被新方案取代）

- 生成目标曾是“Charging → 朝玩家刺击到位”的明确 `last_frame`，而不是收枪后的低位戒备。
- 文件：`C:/Users/Administrator/Pictures/gptGen/cavalry_striking_lastframe_v2.png`。
- 对照：`C:/Users/Administrator/Pictures/gptGen/cavalry_striking_lastframe_v2_vs_charging1.png`。
- 实测：960×960 RGB / PNG Color Type 2、不透明绿幕，1,000,003 bytes；SHA-256 `284c7a66c39bf594598239df040a387af7096ff3240a73175d1d171c12721a3e`。
- 一次 GPT 图像编辑：gpt-image-2.5-sunburst、high、input_fidelity high、960x960、PNG、n=1；不发送 background 参数。host 为 cdn.jd23kjs.work；usage in 1199 / out 4265 / total 5464，cost 未返回。
- 用户反馈“感觉不是很有冲击力”，随后选择右侧身刺击与完整回身返航。**未获验收，当前方案已改向，不将它用作新视频 last_frame，也不擅自当作新的首帧或背面参考。** 保留原文件供比较。
- 该 runtime 仍有 `candidate_downloaded_pending_review` / `user_art_review_pending=true`，是历史字段，本交接中的用户反馈与改向结论优先；本轮文档任务不修改其他记录。
- 没有用这张图生成新视频，没有导入 Unity；右侧身 v3 的关键词、尾帧、视频均未生成。

旧正面草稿及记录（只供历史追溯）：

- `Library/Locus/tmp/cavalry_concept/cavalry_striking_lastframe_v2.runtime.json`
- `Library/Locus/tmp/cavalry_concept/run_cavalry_striking_lastframe_v2.py`（有一次 POST 防重建标记，不重新执行）
- `Library/Locus/tmp/cavalry_concept/prompt_cavalry_striking_keypose_v2.en.txt`
- `Library/Locus/tmp/cavalry_concept/prompt_cavalry_striking_keypose_v2.zh.txt`
- `Library/Locus/tmp/cavalry_concept/prompt_cavalry_striking_lastframe_v2.en.txt`
- `Library/Locus/tmp/cavalry_concept/prompt_cavalry_striking_lastframe_v2.zh.txt`
- `Library/Locus/tmp/cavalry_concept/brief_cavalry_striking_lastframe_v2.md`

可继续复用的已验收首帧：

- `Library/Locus/tmp/cavalry_concept/cavalry_strike_first_frame_charging1_green_v1_960.png`：960×960 RGB；来自正式 Charging1 的原尺寸内容，不带 Unity 补边。
- SHA-256：`df498ba127c9094ae17270221c254d25eeaa4e8c68500fe08560993d39fb5d2c`。
- 另有 `cavalry_strike_first_frame_charging1_green_v1.png`，那张是 **1108×1108 的早期补边副本**，不是最后批准用于视频/生图的 960 版本，勿混用。

---

## 6. 下一次恢复的执行顺序

**本节 §6.1–6.2 的“新尾帧关键词与验收”步骤现已完成：v5 已验收，video v2 关键词已准备。最新恢复起点见 §10。** 下列条目保留前序流程；不能据此重复生成 v3/v4/v5，也不能把素材准备当视频生成授权。

### 6.1 恢复现场与授权边界

1. 读取本文件 §0、§2.1、§5.7–5.9、§9；确认当前 Editor 状态/场景。保持用户的场景 setup，不擅自切回 Battle。
2. 查看当前 Git 状态；保留未跟踪的 12 张 PNG + `.meta`、用户场景/项目设置/APK 和未知残留。
3. 有需要时直接回读已知 109 资产和报告，不把旧 runtime 状态当最新验收结论。
4. 本次只获批准新动作方向，**右侧身版本的中英文关键词尚未制作、未审阅，收费请求尚未批准**。先提交关键词/参数，不直接发起图像或视频调用。
5. 用户此前要求“先导入抠图且对齐尺寸的版本，部署工作等下一个动画制作完成再弄”；Windup/Charging 素材导入已完成，后续 Clip/Animator/Prefab 接线仍延期。

### 6.2 准备并审批新的右侧身刺击尾帧（建议版本 v3）

1. 使用 §5.9 的已验收 Charging1 960 绿幕副本作为身份/比例/起始构图参考，保留人马合一架构；不要使用失败视频或 v2 正面候选来锁定目标姿势。
2. 用中文和英文写清楚：到位急停、人马一起向**画面右侧**转至侧身、右手持枪完成一次明显出枪、左手仍握缰；这张图停在侧身刺击最大伸出，尚未收枪。
3. 侧身角度可提出约 90°作为起始方案，但精确角度/枪轴投影未被用户单独拍板，需结合 §2.1 的同列目标问题在方案中说明；不能偷偷把攻击改到邻列。
4. 不把“正面固定、不许 3/4、不许露背、枪尖朝镜头、只能留在左侧”等旧正面关键词拼进新方案；允许侧面/随后背面，但仍禁止画布内整体位移、镜头运动、随机尺寸漂移、换手、装备变化。
5. 检查成年战马侧面长躯干与长枪伸出是否能容纳于画布；以马头/胸甲/骑手等局部比例保持身份，不逐帧按总包围框 fit，亦不为了不超框把马变小。若需共同调整首尾参考画布，先向用户说明并审批副本，不改已验收原图。
6. 完整提交中英文 Prompt、禁止项、输入图职责、模型、size/quality/input_fidelity/background、输出路径与验收重点。
7. 获得明确生图批准后才生成 **1 张**侧身尾帧，保留请求/响应/哈希，不自动连续重试。用户先验收这张尾帧，再准备视频。

### 6.3 尾帧验收后，另行审批视频与素材处理

- 首帧：已验收 Charging1；尾帧：新侧身刺击到位图。使用 `first_frame` + `last_frame`，不混普通 reference_image/video/audio。
- 视频目标是“Charging 到位 → 急停 / 向右转至侧身 → 出枪命中”，结束在明确命中尾帧，不在同一片里先收枪再被强制拉回命中姿势。
- 仅生产原地 Sprite 动作，代码提供真实冲锋位移。后续收枪→继续回身至背面、背向返航循环须另有动作阶段和素材；怎样合并生成/分段提帧尚未确定，不能自行承诺已完成。
- 视频的模型、时长、分辨率、参考图及 Prompt 必须再次提交审批。创建后保存真实 task id，限时轮询；创建结果不明不重发收费请求；下载失败只续传同一结果。
- 技术校验、视频美术验收、选帧、去背、几何一致性、Unity 素材导入和运行时部署分别验收。动作方向尚未通过的视频不提前批量付费去背。
- 所有新动作去背源仍保留原始 RGB；若使用 GPT，先一帧探针，然后再申请批量，检查真实 Alpha 以及比例/位移，不按 CDN host 判合格。

### 6.4 后续部署与真实战斗待办（尚未授权立即执行）

1. 先解决 §2.1 中停顿/转身/收招时长、视觉枪轴与同列命中、命中末姿势与打断恢复的对应；当前 0.18/0.22/0.2 参数不自动改。
2. 动作素材验收后再建 Windup、Charging、Striking、收枪/回身、背向返航等 Clip，扩展正式路由。现有 Clip 只覆盖 MountedIdle。
3. 用真实节点检查伤害提交、打断不结算、返航受阻等待、到位转回正面 Idle、落马、骑乘死亡及对象池复用；保留旧 101+105 占位直到完整验证。
4. 受击、波次染色、排透明度、描边、血条及调试标签开关一并验证。**允许回身/露背是新的用户方向，不能再以旧规则否定方案。**
5. 原路逐排返航、不与其他单位重叠、既有位移/补齐规则继续有效；视觉转身不是改逻辑列/路径/目标的授权。

### 6.5 落马视觉边界

落马后暂时复用 `Assets/Animations/Enemy_1011.controller` 仍是当前接受方案。后续独立落马步兵身份另开设计任务，不能在此次骑乘动画里擅自替换 1011 正式资产。

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
- 不把 `cutout_batch_v1`、`cutout_batch_v2`、`normalized_1108_v1` 导入 Unity，它们未通过内部浅色 Alpha 与边缘验收；
- 不把历史本地算法维修作为当前任务，不在失败 v1/v2 参数上继续付费/本地堆叠试错；当前 Windup/Charging 去背与导入已完成；
- Striking v1 视频与正面尾帧 v2 未通过当前方向验收；保留但不用于生产，新方案先写右侧身尾帧 Prompt；
- “完整回身返航”已获确认；新图和新视频的 Prompt/参考素材/关键参数变更必须先审批，不复用旧正面请求的收费授权；
- 保留现有用户改动和未知未跟踪文件，不执行无授权清理。

当前推荐的交接起点是：

> 新对话先按“画面右侧身刺击到位 → 收枪继续完整回身 → 背向返航”准备新的尾帧关键词与规格，提交用户审批。不要重做已验收 Windup/Charging，不要直接运行旧正面生图/视频脚本，也不要提前部署。

---

## 8. 相关文件索引

状态优先级：本文件 §0、§2.1、§5.7–5.9、§6、§9 记录本次最新用户确认与实际完成状态；旧 Design/Memory/runtime 中保留的占位、不露背或待抠图历史描述不覆盖它们。机制规则继续参考 `Locus/knowledge/design/cavalry-enemy.md`，动作新方向已确认但尚未回写 `Locus/knowledge/design/cavalry-enemy-animation.md`；本次仅更新交接文档，没有实现这些新方向。

### 机制与动画设计

- `Locus/knowledge/design/cavalry-enemy.md`
- `Locus/knowledge/design/cavalry-enemy-animation.md`
- `Locus/knowledge/design/art-style-guide.md`
- `Locus/knowledge/design/pixel-character-and-enemy-animation-spec.md`

### 生成与验收流程

- `Locus/knowledge/skill/gpt-image-generation.md`
- `Locus/knowledge/skill/workflows/image-asset-generation.md`
- `Locus/knowledge/skill/workflows/character-hit-animation-video-workflow.md`
- `Locus/knowledge/skill/workflows/local-green-screen-cutout.md`（历史本地兜底流程，非本轮成品来源）
- `Locus/knowledge/skill/reachapi-seedance-video-generation-v2.md`
- `Locus/knowledge/memory/video-generation-case-log.md`（含 Striking v1 否决与技术记录）

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

### Windup/Charging 正式 Sprite 与生产记录

- 正式 12 张文件全路径及源帧/采用尝试见 §5.7。
- `Library/Locus/tmp/cavalry_concept/windup_charge_deploy_1108_report.json`
- `Library/Locus/tmp/cavalry_concept/deploy_cavalry_windup_charge_1108_v1.py`
- `C:/Users/Administrator/Pictures/gptGen/cavalry_windup_charge_cutout_batch_v1/batch_v1_candidate_selection.json`
- `C:/Users/Administrator/Pictures/gptGen/cavalry_windup_charge_cutout_batch_v1/batch_v1_12frames_acceptance_report.json`
- `Library/Locus/tmp/cavalry_concept/run_cavalry_windup_charge_cutout_batch_v1.py`（保存全部尝试，非恢复起点，不重复收费运行）
- `Library/Locus/tmp/cavalry_concept/import_scope_baseline.json`（导入阶段保存的保护范围哈希，仅当时验证参考）

### Striking 历史候选与可复用首帧

- Striking v1 视频、抽帧诊断、runtime/Prompt 路径见 §5.8。
- 正面刺击尾帧 v2、对照图、runtime/Prompt 路径见 §5.9。
- 新方案可复用 `Library/Locus/tmp/cavalry_concept/cavalry_strike_first_frame_charging1_green_v1_960.png`。
- 正面 v2 请求/响应在 `C:/Users/Administrator/Pictures/gptGen/cavalry_striking_lastframe_v2.request.json` 和 `C:/Users/Administrator/Pictures/gptGen/cavalry_striking_lastframe_v2.response.json`。
- **不存在已完成的右侧身 v3 Prompt、图像或视频**，需要新会话从关键词准备开始。

### 历史本地抠图中间产物（未通过，不得导入 Unity）

脚本：

- `Library/Locus/tmp/cavalry_concept/run_cavalry_local_cutout_calibration_v1.py`
- `Library/Locus/tmp/cavalry_concept/run_cavalry_local_cutout_batch_v1.py`
- `Library/Locus/tmp/cavalry_concept/run_cavalry_local_cutout_batch_v2.py`
- `Library/Locus/tmp/cavalry_concept/normalize_cavalry_cutout_batch_v2_to_1108.py`

产物目录（均在 `Library/Locus/tmp/cavalry_concept/cavalry_windup_charge_v1_decode/` 下）：

- `calibration_f015_v1/`（定标帧及报告）
- `cutout_batch_v1/`（v1 报告及接触表）
- `cutout_batch_v2/`（v2 报告、白底/深底/Alpha 接触表、4 张 8× 边缘）
- `normalized_1108_v1/`（1108×1108 候选与报告）

### 已作废或仅供历史追溯的候选

- `C:/Users/Administrator/Pictures/gptGen/cavalry_lancer_idle_v2.png` 仅为早期失败候选，不得作为新基准；
- 早期“没有正式 109 骑乘资源”“当前候选未验收”“暂停期间禁止制作动作帧”的表述已被本交接文档前文覆盖，不得按旧语义恢复；
- `cutout_batch_v1` 因低 Alpha 压缩幽灵被 v2 取代；v2 又因内部浅色 Alpha 与算法缺陷被本文件标记为未通过——两者都不得直接部署。

---

## 9. 最终状态摘要与可直接转交的新会话说明

**完成且已验收**：MountedIdle 正式素材/Idle 接线；Windup/Charging 视频、选帧、12 张 GPT 去背与 Unity Sprite 导入；v5 刺击尾帧。Charg­ing1 与 v5 已原字节复制到 video v2 staging 目录，不需要重新生成、去背或对齐。

**未通过/不继续生产**：历史本地 cutout 候选、Striking v1 视频、正面尾帧 v2、右侧身 v3_attempt2；v4 只获姿势认可，枪杆弯曲，以 v5 为本次视频终点。所有历史文件保留。

**最新朝向**：马体和马头向画面右侧侧身；骑手上身/面甲/视线以及右手长枪朝玩家/镜头，左手握缰。单层人马 Sprite 不要求马与骑手完全同向。

**已否决的成片**：Striking video v2（举枪后指向）和 v3（去掉举枪但仍只转枪指向）均技术完成、用户美术不通过，原视频保留，不作生产选帧/去背。

**v4 已否决**：用户确认实际动作仍是先转身、再将枪尖摆正，不是同步刺出；原视频和诊断保留，不作生产选帧/去背。

**下一步**：查看v6已导出的18帧6+6+6交付；v6全帧边界已通过，核心伤害段未遗漏f076–f081。去背/Unity接线未执行。

可复制给新对话：

> v5的f076–f081枪尖越界，用户确认重生成v6并要求控制边界。v6已只创建一次、下载解码通过；全部145帧最低边距48/98/70/60px。已按用户要求导出18帧：出枪前f055/f059/f063/f067/f071/f074，核心伤害f075/f076/f078/f080/f081/f082，收尾f083/f086/f089/f092/f097/f103。目录为Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v6/keyframes_6plus6plus6_v2/，等待帧序验收；Editor editing E0预览，不切场景/改Unity。

## 10. v5 验收与 Striking video v2 准备补充（2026-10-08）

### 10.1 尾帧生产状态

- 首次 v3 请求返回 HTTP 401 `token invalid`，未出图；API 修复后另行授权一次 v3_attempt2 请求。
- v3_attempt2：用户认可整体侧身，但骑手看向马头前方而非玩家，否决。随后明确选择“长枪也朝玩家”。
- v4：马体向右、骑手面向玩家、枪朝玩家的整体关系被认可；枪杆弯曲，用户要求单张修正。
- v5：使用 v4 作为唯一编辑输入，修直枪杆；用户已明确“已验收”。v5 枪尖略向左下移动的候选也包含在本次验收中。
- 当前唯一 accepted `last_frame`：`C:/Users/Administrator/Pictures/gptGen/cavalry_striking_lastframe_v5.png`。
- 规格：960×960 RGB，1,105,360 bytes；SHA-256 `fe14eeab03e7cce921c24457d190b1bfe6304125f779bd5f217174a7bc6ed71c`。
- Runtime：`Library/Locus/tmp/cavalry_concept/cavalry_striking_lastframe_v5.runtime.json`，状态 `user_accepted_as_strike_last_frame`；无视频、去背或 Unity 导入。

### 10.2 首尾帧与关键词

- `first_frame`：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v2/first_frame_charging1_960.png`；SHA-256 `df498ba127c9094ae17270221c254d25eeaa4e8c68500fe08560993d39fb5d2c`。
- `last_frame`：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v2/last_frame_striking_v5_960.png`；SHA-256 与 accepted v5 相同。
- 两份 staging 文件均与原图逐字节相同，960×960 RGB；未缩放、裁切、平移、补边或背景归一化。
- 对照图：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v2/first_last_frame_contact_sheet.png`，仅展示，不上传作参考图。
- Manifest：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v2/first_last_frame_manifest.json`。
- 英文 Prompt：`Library/Locus/tmp/cavalry_concept/prompt_cavalry_striking_video_v2.en.txt`。
- 中文 Prompt：`Library/Locus/tmp/cavalry_concept/prompt_cavalry_striking_video_v2.zh.txt`。
- 方案：`Library/Locus/tmp/cavalry_concept/brief_cavalry_striking_video_v2.md`。
- 请求规格：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v2.request-spec.json`。

### 10.3 拟定视频参数与边界

- `seedance-2-fast` / 4 秒 / 720p / 1:1 / generate_audio=false / watermark=false / seed=-1。
- 首尾帧模式，不混 ordinary reference_image/video/audio，不使用失败 v1 视频、v2 正面图或 v3_attempt2。
- 计划输出：`C:/Users/Administrator/Videos/doubaoVideo/cavalry_lancer_striking_v2_720p_4s.mp4`。
- 此项为 2026-10-08 准备状态：当时未上传/创建。2026-10-09 用户另行批准视频创建，当前结果见 §10.4；不要按历史未生成状态再次创建。
- 动作安排：0–0.25s Charging 起点；0.25–1.70s 急停与马体转向，骑手对向补偿且视线锁定玩家；1.70–3.15s 一次加速纵深直刺；3.15–4s v5 命中收稳。时间只是生成节奏建议，不是游戏玩法时长。
- 已知风险：尾帧绿幕不均匀；首尾最小画布余量约 61px / 70px，旋转中间帧需查超框。保留已验收原像素，不擅自逐图按包围框 fit。
- 后续收枪→完整回身→背向返航、到位回正面、打断路径和真实战斗验收仍另行制作/部署。

### 10.4 Striking video v2 已生成并验证（2026-10-09，用户否决）

- 只创建一次：`task_49dbbb7612ae42cda74dac3014a406e3`。用户批准后实际上传 Charging1 first_frame + v5 last_frame；无普通参考图。
- 参数：seedance-2-fast、4s、720p、1:1、无音频、无水印、seed=-1；Prompt SHA-256 `34740420950ca4d9d80698f7ebeddf2ec4ba91a5c558c07f4a268fe90e83d1d8`。
- task 已 success；usage completion_tokens/total_tokens 87300，cost 原值 `spend=0.48888`，不推断币种。
- 原视频：`C:/Users/Administrator/Videos/doubaoVideo/cavalry_lancer_striking_v2_720p_4s.mp4`，2,772,210 bytes；SHA-256 `54a72b49b4fcb5f1db960f83075f45655d8c31441f8b443ce7f66442cb7cddcf`。
- 下载：同任务结果，Range 探测后 4 个不重叠分段全部验证通过；MP4 `ftyp/uuid/free/mdat/moov` 结构完整。FFmpeg 7.1 全量解码退出码 0：960×960、24fps、97 帧、容器约 4.04 秒、无音频。
- 诊断接触表：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v2/diagnostic_review/motion_contact_sheet.png`。这 12 张诊断抽帧不是生产选帧。
- 用户验收**不通过**：完成转身后不是刺出一击，而是举枪后指向玩家，动作语义错误；约2.0–2.5s过头举枪、约1.0s尘土也在诊断中可见。
- Runtime：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v2.runtime.json`，状态 `user_rejected_action_semantics`。保留视频，不去背、不选生产帧、不部署；用户另行授权的是新 v3，不重复 v2。
- 创建脚本 `Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v2/run_striking_video_v2.py` 有持久化防重复门；不再次执行以创建新任务。下载出错时只续取原 task，不重新生成。

### 10.5 Striking video v3（2026-10-09，用户否决：仍为指向不是刺出）

- 用户在否决 v2 后明确“发起v3版生成”，只创建一次 task `task_711a9851c69a44a39ccd1a56162dd505`，服务端 success。
- 参数仍为 seedance-2-fast / 4s / 720p / 1:1 / 无音频无水印 / seed=-1；首尾帧仍为已验收 Charging1 + v5，不上传失败视频。
- 实际英文 Prompt：`Library/Locus/tmp/cavalry_concept/prompt_cavalry_striking_video_v3.en.txt`；SHA-256 `a37e2439dd4b13b184372cb894066c3295e1242ae230fd27186d35cd54a68c2d`。中文对照：`Library/Locus/tmp/cavalry_concept/prompt_cavalry_striking_video_v3.zh.txt`。
- 动作约束：0–0.20s Charging；0.20–1.25s马体完成向右转身，骑手持续面对玩家，枪留在头盔以下弯肘位；1.25–2.05s右肩/右肘驱动，枪沿轴朝玩家前移一次；2.05–4s到位保持。时间是生产建议，不改游戏玩法。
- 视频：`C:/Users/Administrator/Videos/doubaoVideo/cavalry_lancer_striking_v3_720p_4s.mp4`，2,650,016 bytes；SHA-256 `039bb42a90ca3a97bb397384f3496a6bdd5327bb98b48dcaff96b704c282ac13`。
- MP4容器完整；FFmpeg全量解码退出码0：960×960、24fps、97帧、约4.04s、无音频。API usage 87300，cost 原值 spend=0.48888，不推断币种。
- 诊断接触表：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v3/diagnostic_review/motion_contact_sheet.png`。
- 连续出枪段 f045–f053：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v3/diagnostic_review/thrust_consecutive_f045_f053.png`。仅诊断预览，不是生产选帧。
- 用户明确否决：依然完全没有解决枪不是刺出而是指向玩家的问题。无过头举枪不是通过证据；约1–1.9s横向枪、f048–f050枪向变化是失败路径。
- Runtime：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v3.runtime.json`，`user_rejected_pointing_not_thrust`；无生产选帧、去背或Unity导入，原视频不改。
- 创建脚本：`Library/Locus/tmp/cavalry_striking_video_v3/run_striking_video_v3.py`，实际脚本通过哈希和已存在runtime拒绝重复；不要再次调用。此前 request-spec runtime 路径笔误已更正到实际项目目录。

### 10.6 Striking video v4 六秒方案（历史方案，已生成并否决）

**此节的“转完→对准→拉回→停顿→再刺”和转身/刺击拆段建议均已被用户最新的同步要求取代，保留作失败复盘，不作为执行目标。见§10.10。**

**绿幕前置项现已完成：用户批准制作副本后，统一绿幕(0,159,62)首尾、保护主体RGB/位置、边缘预审及关键词修订见§10.8。当前Prompt不再使用旧(0,177,64)，但视频收费生成仍未批准。**

- 用户要求重新思考关键词并建议实际6秒；按API `duration_seconds=6`准备，不将旧视频补帧或拉长。
- 复盘：v3虽然已有轴向词句，仍缺少“枪已对准、手贴近身体且未推出”的可读中间状态；不得再将关键词更强当成功保证。
- 新动作：0–0.30s起始；0.30–2.20s制动转身并完成枪轴朝玩家；2.20–3.20s沿同轴拉回，肘弯/手到右肋；3.20–3.60s短暂可读预备；3.60–4.20s一次快速同轴推出至v5；4.20–6s终点保持。
- 阶段约束：允许转身期调整枪向；必须有刺击前拉回；正式刺击中不旋枪/横摆；接触后禁止收枪。允许局部手臂与武器透视变化，不允许镜头/人马整体缩放。
- 英文：`Library/Locus/tmp/cavalry_concept/prompt_cavalry_striking_video_v4.en.txt`；中文：`Library/Locus/tmp/cavalry_concept/prompt_cavalry_striking_video_v4.zh.txt`。
- 方案：`Library/Locus/tmp/cavalry_concept/brief_cavalry_striking_video_v4.md`；规格：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v4.request-spec.json`。
- 拟定参数：seedance-2-fast / **6s** / 720p / 1:1 / 无音频无水印 / seed=-1；仅Charging1 first_frame+v5 last_frame的新统一绿幕副本（§10.8），原图主体与姿势保留；不上传失败视频或第三张普通参考图。
- 计划输出：`C:/Users/Administrator/Videos/doubaoVideo/cavalry_lancer_striking_v4_720p_6s.mp4`。当前不存在任务/上传/runtime/输出，本轮无收费或Unity修改；需用户批准一次新创建。
- 首尾图不保证中间位。若仍失败，建议先制作并验收弯肘预备关键图，再拆成转身→预备、预备→同轴刺击两段；增加素材/任务需另批，本轮未执行。

### 10.7 首尾绿幕与生成主体色偏诊断（2026-10-09，仅检查）

- 用户指出首帧绿幕与生成视频不同，角色随之变色。已比较前序Charging源帧、首尾输入、v2/v3全194帧背景、配准主体材质、解码矩阵与原YUV平面。
- 四角空背景中位RGB：原Charging f036=(0,159,62)，上传首帧=(0,177,64)纯色，v5尾帧=(2,182,77)非纯色；v3 f001=(0,175,63)，f007/0.25s=(0,161,69)，f097=(0,175,74)。v2同样发生变化；视频YUV平面也随时间改变，不仅是播放器观感。
- 主体样本：配准后v3 0.25s蓝灰头盔中位(58,62,82)→(62,62,82)，红布(186,28,26)→(180,30,31)。马胸有较大形变、匹配可信度较低。说明部分主体RGB不恒定，不证明全身统一调色，也不证明绿幕差异是唯一原因；生成重绘、光照和编码仍可能参与。
- 当前首帧与正式Charging1深层主体161025像素平均RGB绝对差约(0.76,0.86,0.10)，小于1；首帧合成过程没有整体主体调色。视频f001也有小幅编码/解码差；不把后续同坐标位移差误当色差。
- 建议用户确认后另存**只改背景**的首尾副本到同一纯色（原Charging实测约(0,159,62)可作为候选），保留原主体/尺寸/锚点；首帧可用已有RGBA重新合成，尾帧需要背景分离及边缘检查。不能整图白平衡/亮度/饱和度来对齐绿色。
- 背景统一只是降低输入矛盾，不保证模型不改主体或输出RGB恒定；主体颜色和背景分别验收。当前未执行任何背景归一化/去背/生成/Unity改动，输入和原视频哈希保持。
- 诊断结论：`Library/Locus/tmp/cavalry_concept/cavalry_chroma_color_diagnostic_v1/findings.md`。
- 背景/材质对照：`Library/Locus/tmp/cavalry_concept/cavalry_chroma_color_diagnostic_v1/background_and_material_review.png`。
- 全帧背景CSV：`Library/Locus/tmp/cavalry_concept/cavalry_chroma_color_diagnostic_v1/background_timeline.csv`；报告：`Library/Locus/tmp/cavalry_concept/cavalry_chroma_color_diagnostic_v1/color_diagnostic_report.json`。

### 10.8 v4生成前准备完成（2026-10-09，历史准备阶段；随后已验收/生成）

- 用户明确同意“请先完成v4生成前工作”，允许只做背景统一副本和准备材料，不授权视频创建。
- 统一色：`(0,159,62)`。first/last都是960×960 RGB不透明PNG，未缩放、平移或补边。
- 新First：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v4/prep_green_015962_v1/first_frame_charging1_green_0_159_62.png`，401639 bytes；SHA-256 `36374bd36ddf11ff911429134e64d2004195d886b1bc5bf2a8d771934238ac85`。
- 新Last：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v4/prep_green_015962_v1/last_frame_striking_v5_green_0_159_62.png`，585612 bytes；SHA-256 `eb41bfcfbfb157b9a07ea26a570866826084f2660cc78cadd2916297d9b229c1`。
- First由正式Charging1 RGBA按已知(74,120)补边反取原960窗口合成；原绿重合成与旧First逐像素一致。新背景重合成后，Alpha>=245的194074个主体像素保留旧RGB，深层161025个像素改动数0。
- Last没有生产Alpha：保护非绿主体与18个内部偏绿高光，303246个保护像素RGB改动数0，深层249212像素最大差0；明确绿幕重铺，窄边缘混合像素7234个有变化，局部颜色线估计只用于静态RGB重铺，不是最终抠图/Alpha交付。
- 主体保护bbox前后不变；枪尖、盔缨、缰绳/马嘴、马鬃/尾和马蹄边缘前后已回读，未见明显新增缺损或边缘问题。技术预审通过，用户对新副本的目检仍待回应。
- 首尾对照：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v4/prep_green_015962_v1/first_last_frame_contact_sheet.png`；前后/边缘图同目录`before_after_review.png`和`last_frame_edge_review.png`。
- Manifest和报告同目录`first_last_frame_manifest.json`、`normalization_report.json`；均已回读，JSON重复键检查通过，源文件/旧视频哈希不变。
- 当前英文Prompt：`Library/Locus/tmp/cavalry_concept/prompt_cavalry_striking_video_v4.en.txt`，SHA-256 `49861f57fe6669836d1d4bb3e0bebb11936762fc089481cbfa4cc1bc73f5a01c`。中文对应`.zh.txt`，SHA-256 `99fe4ad7cd56bb136c1344725f58a184bae64d3d2e0bf3f0e4a4dc02200cdf09`。背景改为(0,159,62)，强调无绿光染色/主体调色；动作六秒方案不变。
- 请求规格`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v4.request-spec.json`已引用新副本和哈希，状态`prepared_common_green_ready_for_user_review_no_video_api_call`，用户视频创建授权false、create_post_attempt_count=0。
- 模型/时长/分辨率仍seedance-2-fast / 6s / 720p / 1:1，无音频无水印、seed=-1；预定输出`C:/Users/Administrator/Videos/doubaoVideo/cavalry_lancer_striking_v4_720p_6s.mp4`不存在，上传/请求/响应/runtime均未创建。
- 上述“未上传/创建”是准备阶段记录，随后用户已验收并批准生成；当前结果以§10.9为准。原始首尾图和本轮副本保留，统一输入不保证输出保色。

### 10.9 Striking video v4（2026-10-09，用户否决：先转身再摆枪）

- 用户明确“已验收，请发起生成”后只创建一次task `task_d666303dd1bb4a3488df82ac341b69ff`；上传两张统一(0,159,62)副本，seedance-2-fast / 6s / 720p / 1:1 / 无音频无水印 / seed=-1。
- 已成功下载：`C:/Users/Administrator/Videos/doubaoVideo/cavalry_lancer_striking_v4_720p_6s.mp4`，3,094,462 bytes；SHA-256 `cb69bd422af381307034cbc22ef678af65c73d8a54362beba03f3dc9f9fcae7b`。
- MP4容器有效，FFmpeg全量解码退出码0：960×960、24fps、145帧、约6.04s、无音频。API usage completion_tokens/total_tokens=130500；cost原值spend=0.7308，不推断币种。
- 全流程只创建一次；有限查询后success，同task四分段Range下载。英文Prompt哈希与已验收规格匹配，原视频和所有首尾输入哈希回读一致。
- 25帧诊断图：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v4/diagnostic_review/motion_contact_sheet.png`。
- f103–f114连续武器段：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v4/diagnostic_review/weapon_transition_consecutive_f103_f114.png`。这些仅验收预览，不是生产选帧。
- 用户明确否决：依然先转身、再将枪尖摆正，未真正刺出；用户确认**转身与刺出应该同时进行**。旧顺序Prompt结构是错误目标，不因技术解码通过而保留为生产候选。
- 预审颜色：输出四角背景中位数f001=(0,154,60)，f007/0.25s=(1,144,60)，f145=(0,150,60)；全视频G中位范围140–154，仍比输入159暗且时变。蓝灰头盔局部变化较小，但其他材质有形变和重绘，不能保证整体保色，也不证明背景是单一原因。
- 报告/全帧颜色CSV：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v4/diagnostic_review/decode_and_color_validation.json`、`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v4/diagnostic_review/background_timeline.csv`。
- Runtime：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v4.runtime.json`，状态`user_rejected_sequential_pointing_not_simultaneous_thrust`。创建脚本有持久化防重复门，不再执行。
- 原图、v2/v3、Unity资产均保留。本轮无生产选帧、去背或Unity接线修改；无自动再次抽卡。

### 10.10 用户修正与v5执行关键词：侧转前蓄力→转身与刺出同步（2026-10-09）

- 用户反馈：“转身和刺出应该是同时进行的”，v4验收不通过；随后明确“请将其加入关键词描述并发起生成”，授权把完整士兵蓄力/发力加入v5并新建一次任务。实际结果见§10.11，不再是待收费批准的准备稿。
- 侧转前紧凑蓄力：坐深鞍座、夹膝压镫、骨盆留在鞍上；腰背向右持枪侧拧紧、胸甲局部降低/收紧、右肩后压、右肘折叠、右手退近右肋，整枪随手沿原轴后退；左手低握缰、下巴略收、视线锁定玩家。不举枪/全身缩小。
- 释放时腰背→胸/右肩略领先，肘手自然重叠；马体首次侧转时手/肩已前送、肘打开、枪尖前探，中段持续；马侧身到位和最大出枪同一命中瞬间。盔缨/枪缨小幅滞后，不代替身体发力。
- 枪杆投影随旋身变化允许；手不动只转枪不算刺击。不能再执行“转完→固定枪轴→收枪/停顿→推出”，旧v3/v4拆段建议失效。
- 英文：`Library/Locus/tmp/cavalry_concept/prompt_cavalry_striking_video_v5.en.txt`，SHA-256 `6e4d96e7e6bad72fe0483f9004c898ad9b410d3ebe82f63da93fb55adc05316d`；中文：`Library/Locus/tmp/cavalry_concept/prompt_cavalry_striking_video_v5.zh.txt`，SHA-256 `78393eb6610b4d78fc571d54e4d79d38c064062bb28cb27f63b0820ced3a55fa`。
- brief和request-spec仍在同目录。请求节奏：0–0.30s Charging；约0.30–1.10s初始制动、侧转前紧凑蓄力；约1.10–2.40s释放并同步旋身刺击；约2.40–6s命中保持，无收枪/继续回身/返航。时间只是生成建议，不改玩法。
- 输入复用§10.8用户已验收的统一(0,159,62)Charging1/v5图片副本；960×960 RGB，首尾哈希保持；仅first_frame+last_frame，不混失败视频或第三普通参考。
- 已授权并实际执行：seedance-2-fast / 6s / 720p / 1:1 / 无音频无水印 / seed=-1。创建脚本持久化防重复，不再调用。
- 若用户否决，应先讨论“马仍在转向、臂已部分伸展”的同步中间姿势/运动参考，不拆为转完后的预备位；改变模式/新增素材与收费任务仍需另行授权。

### 10.11 Striking video v5 六秒（2026-10-09，已获用户验收）

- 只创建一次 task `task_4207390cad1d4fbc83e0a40607a8ea71`，服务端success；2次状态观察后成功，另1次GET仅为取得同task下载URL，无重复收费创建。
- 实际原视频：`C:/Users/Administrator/Videos/doubaoVideo/cavalry_lancer_striking_v5_720p_6s.mp4`，14,777,507 bytes；SHA-256 `7f65098ce2f0d51c33fe15dc3c188ab17eb4bd69803b16729b220f3c335b1602`。
- 同task四个Range区间ETag/长度均通过后拼接，MP4 `ftyp/free/mdat/moov`完整；正式改名前FFmpeg全量解码退出0：960×960、24fps、145帧、容器约6.04s、无音频。诊断曾把终端进度145误解析为fps，已按输入流24fps更正报告，145是帧数。
- usage completion_tokens/total_tokens=130500；cost原值spend=0.7308，不推断币种。
- 预审：约1.5–2.75s骑手低头/压胸、枪抬近肩、马仍正面；明确右肩后收/手退至肋/整枪沿轴后退未体现。约3.0–3.75s马转向和持枪臂伸展有重叠迹象，比v4更接近同步目标，但仍有明显画面左向摆枪，不能直接判为有力纵深刺击或精确同起同止。
- 约3.25s/f079枪尖切至画布左边；约2.4s命中建议未遵守，终点约4s后稳定接近尾图。以上为验收前代理诊断；随后用户明确“很好，已验收，请根据动作语言和敌人动画需要拆分动画关键帧”。视频已通过，原疑虑不覆盖用户验收；本轮抽帧避开实际出框段。
- 颜色：输出145帧四角64×64背景中位RGB范围R0–2/G144–153/B59–63；f001=(0,153,59)，f007=(1,147,59)，仍比输入159暗且时变。本轮仅新背景统计，未做全材质配准，不能将背景差直接归因成全身调色。
- 全时长诊断表：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v5/diagnostic_review/motion_contact_sheet.png`；转折区表同目录`transition_contact_sheet.png`。均只诊断，不是生产选帧。
- 同目录`decode_and_color_validation.json`、`background_timeline.csv`、`findings.md`记录技术和观察；下载manifest在`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v5/download_manifest.json`。
- Runtime：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v5.runtime.json`，最新状态`user_accepted_video_24_raw_keyframes_exported_frame_review_pending`。request-spec已同步视频验收和24帧结果，初版brief选帧记录保留为历史，不再为pending video review。
- 原图、历史视频、Unity资产不改；视频交付阶段未选帧，随后用户已授权并完成§10.12关键帧提取。仍无去背、Clip/Animator/Prefab/代码修改或场景保存，不自动再生成。

### 10.12 Striking v5 初版12帧（历史版；用户认为过少，当前用§10.13的24帧版）

- 用户已验收视频，并明确要求“根据动作语言和敌人动画需要拆分动画关键帧”；本轮执行本地解码、关键帧挑选与预览，无收费API。
- 145帧重新完整解码为960×960 RGB PNG，解码退出0；每张PNG回读与原始RGB像素相等。解码目录`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v5/keyframe_selection_v1/decoded_frames/`，同层`decode_manifest.json`记录源哈希/帧号/边缘和动作差异。
- 交付目录`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v5/keyframe_selection_v1/selected_frames_v1/`，原始单帧在其`striking/`目录，命名`striking_01_srcf001.png`至`striking_12_srcf145.png`。
- 动作语言分组：入口/急停蓄力 f001、f034、f040、f046；同步释放旋身/刺出 f070、f074、f083、f086；命中主帧 f092（第9张）；惯性余摆/收稳/终点 f103、f115、f145。不是等间隔抽样或把转身和出枪拆成先后两击。
- f002–f031的重复冲锋腿步、f047–f064的长蓄力静止被压缩。f076–f081实际枪尖触边/裁切，不作生产帧；f074→f083以完整姿态承接快动作，不修图或插值。12选帧边缘4px优势前景检测均0；最窄源边距f074左27px/f092右46px，目检未截断。
- 源/导出PNG字节和SHA-256逐张一致；全12张960×960 RGB，没有缩放、平移、裁切、重铺背景、调色、去背。原视频SHA仍`7f65098ce2f0d51c33fe15dc3c188ab17eb4bd69803b16729b220f3c335b1602`。
- 总览：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v5/keyframe_selection_v1/selected_frames_v1/striking_selected_raw.png`；同目录蓄力、释放命中、收稳分组表。
- 动图：同目录`striking_slow_review.webp`（慢放，每姿势可看）和`striking_timing_0p4s_preview.webp`（0.4s建议时序），均12帧完整可解码。重播前末帧额外0.8/0.6s审阅停留，不是游戏Clip时间。
- 时序建议CSV：同目录`striking_keyframe_timing.csv`。按既有Prefab `strikeWindup=0.18s` / `strikeRecover=0.22s`，建议非循环Clip全长0.4s，序号时间0/.025/.050/.075/.095/.115/.135/.155/**.180**/.240/.300/.370，末帧至.400。第9张f092对齐伤害提交，之后保持伸枪/惯性收稳，不倒放为收枪。时序未创建/实施或改玩法。
- 帧率检查：60fps简单采样能显示全部12序号；30fps此建议可能跳过04/07/12，命中帧首次显示约0.2s。接线时应实机检验短过渡与状态切换，不以拆帧结果宣称运行时已对齐。
- 后续首帧优先复用正式`Assets/Sprites/Enemy/Enemy109/Enemy_109_Charging1.png`，不重复去背；其余11张可作为新去背候选。视频f001和已验收PNG存在编码RGB差，复用是姿势/衔接建议，不是像素等同。
- 去背验收后沿用1108×1108统一画布，960源图1:1贴(74,120)，PPU16、Point、有效Center pivot=(0.5,0.5)；本轮未补边/导入。不得逐帧bbox fit或居中，完整保留马蹄相位。
- manifest、README和检查细节在交付目录`selected_frames_manifest.json`、`README.md`。视频已验收，本轮抽帧待用户查看；无新生图/去背、Clip/Animator/Prefab/C#修改或场景保存。
- 本视频只到最大出枪后的侧身收稳，收枪、继续回身、背向返航、Interrupted仍是后续独立素材/接线任务，不能反播命中段冒充。

### 10.13 Striking v5 24帧扩展（2026-10-09，当前候选）

- 用户“太少了，请拆除24帧”按上下文解释为拆出24帧；已本地扩展，无新生成请求。原12帧目录/PNG保持。
- 当前24个不同源帧：f001/f031/f034/f037/f040/f043/f046/f065/f068/f070/f072/f074/f075/f082/f083/f084/f086/f089/f092/f097/f103/f109/f115/f145。保留原12个节点，补12个中间姿势，不插帧或复制凑数。
- 分组：01–07入口/急停蓄力；08–18同步旋身前送；19=f092命中；20–24惯性收稳。f076–f081仍排除，新增f075/f082承接两侧完整姿势，24帧边缘4px检测均0，最窄源左边距14px。
- 当前交付目录`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v5/keyframe_selection_v1/selected_frames_v2_24/`，单帧在`striking/`；24张960×960 RGB与解码源PNG逐字节/哈希一致，源视频哈希不变，无缩放/移位/调色/去背。
- 总览`striking_24_selected_raw.png`、慢放`striking_24_slow_review.webp`、24fps审阅`striking_24_sequence_preview.webp`，manifest/README/`striking_24_frame_map.csv`同目录；两个WebP均24帧完整可解码。
- 预览仅审阅速度，不代表玩法。第19张f092仍是命中锚点，旧12帧的0.4s逐帧时间表不直接套用；现有0.18s前探/0.22s恢复不改，后续接线再验证采样与时序。
- 首帧可复用正式Charging1，其余23张等待后续去背；1108补边、Unity导入/Clip/Animator/Prefab/C#修改均未执行。当前Editor editing E0预览，未切场景/保存。

### 10.14 v6边界通过与6+6+6关键帧（2026-10-09，18帧已导出待验收）

- v6 task `task_1bcb08e0d51d4df8bd09c9388f4b7d4b`，只创建1次；视频`C:/Users/Administrator/Videos/doubaoVideo/cavalry_lancer_striking_v6_720p_6s.mp4`，3,252,977 bytes，SHA-256 `119df1a087a34856c14b104e4a50607fd60cc6967a6ee1c0fb0981dbfadf415b`。
- MP4完整，FFmpeg全量解码0：960×960、24fps、145帧、约6.04s、无音频；usage completion_tokens/total_tokens=130500，cost原值spend=0.7308，不推断币种。
- 全145帧边界检查通过：最低边距L/T/R/B为48/98/70/60px，要求32px；边缘4px前景像素全为0；bad frame count=0。报告：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v6/diagnostic_review/boundary_report.json`。
- 18帧交付目录：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v6/keyframes_6plus6plus6_v2/`。严格分组：出枪前6帧 `f055/f059/f063/f067/f071/f074`；核心伤害刺出6帧 `f075/f076/f078/f080/f081/f082`；f083之后收尾6帧 `f083/f086/f089/f092/f097/f103`。核心段没有跳过f076–f081。
- 总览：`keyframes_18_all.png`；核心段：`keyframes_6_core_damage_thrust.png`；审阅动图：`keyframes_18_review.webp`；清单：`selected_frames_manifest.json`；逐帧映射：`keyframes_6plus6plus6.csv`。18张均为960×960 RGB，源/输出PNG逐字节一致。
- 当前仍未去背、补边、导入Unity、创建Clip或修改玩法时序；仅完成视频技术验证和原始关键帧拆分，待用户验收。
