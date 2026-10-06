---
id: kd_33c753e0-5c7c-45af-838c-a6ebe8d30ec4
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 1011（持剑敌军）动画部署与播放规则交接

> **用途**：新对话可直接以本文件开启 1011 动画部署、Animator 接线、受击方向播放规则和 Launch 落地/起身接线；不需要重新回读历史生成过程。完整任务/费用/版本记录见 `memory/video-generation-case-log.md`。
> **历史快照**：本文件记录早期素材生产与选帧阶段；当时的“未接入”状态已被后续部署完成。当前工程状态以 `Locus/knowledge/plan/enemy1011-animation-deployment-handoff.md` 为准。
> **旧历史文件**：`plan/enemy1011-animation-handoff.md` 保留早期生成/选帧/抠图记录；本文件是当前部署与播放规则入口。

## 0. 历史快照说明

本文件保留早期生成、选帧、抠图和部署前检查记录，仅用于追溯素材来源与生产过程。不要依据本文件判断当前 Unity 接入状态；当前权威交接见 `Locus/knowledge/plan/enemy1011-animation-deployment-handoff.md`。

## 0. 历史结论（不再作为当前状态）

1011（enemyId **1011**）已有并已接入的正式动画：

- Idle
- 正面 HitFlash
- Attack
- Dead
- Walk（实际语义为原地冲锋跑位）

以下是部署前的**历史快照**，当前 Unity 接入已由后续部署完成：

- 左受击 HitLeft：6 帧透明素材已暂存，未复制到 Assets
- 右受击 HitRight：6 帧 GPT 原始透明输出已完成但几何未对齐，未复制到 Assets
- Launch Rise：6 帧已导入 Assets
- Launch Landing tail：6 帧已导入 Assets
- Launch Getup：6 帧已导入 Assets

本交接范围是下一次对话的工程部署，不是继续生成视频。

## 1. 当前资产清单与真实部署状态

### 1.1 已接入 Unity 的正式动画


| 状态 | 101 的 clip | 1011 现状 |
|---|---|---|
| Idle | `Enemy_101_Idle.anim`（2 帧 / 0.6s） | **已替换** → `Enemy_1011_Idle.anim`（6 帧、键距 0.1s；state speed **0.75**）；素材 `Enemy_1011_idle1..6.png` |
| HitFlash（受击） | `Enemy_101_HitFlash.anim`（2 帧 / 每帧 0.15s / 共 0.3s / 不循环） | **已替换** → `Enemy_1011_HitFlash.anim`（6 帧 / 0.9s / 不循环 = 正面受击）；素材 `Enemy_1011_hit1..5.png` + 末帧引用 `idle1` |
| Attack | `Enemy_101_Attack.anim`（2.533s） | **已替换** → `Enemy_1011_Attack.anim`（18 键 / 30fps / Loop 关 / stopTime 2.9 / **命中键 2.000s**）；素材 `Enemy_1011_attack1..16.png` |
| Dead | `Enemy_101_Dead.anim`（0.633s） | **已替换** → `Enemy_1011_Dead.anim`（6 键 / stop 0.6 / 末姿势保持）；素材 `Enemy_1011_dead1..6.png` |
| Walk | `Enemy_101_Walk.anim`（2 帧 / 0.6s） | **已替换** → `Enemy_1011_Walk.anim`（6 键 @0.1s / 30fps / **Loop 开 / stopTime 0.6**）；素材 `Enemy_1011_walk1..6.png`（来自 v4 冲锋跑帧 f023/f025/f027/f029/f030/f032） |
| Launched_Rise | `Enemy_101_Launched_Rise.anim`（0.583s） | 历史快照：当时仍用 101，后续已替换为 1011 Clip |
| Launched_Fall | `Enemy_101_Launched_Fall.anim`（0.483s） | 历史快照：当时仍用 101，后续已替换为 1011 Clip |
| Launched_Land / Getup | 无 | 历史快照：当前无独立 Landing Clip；Getup 已接入，landing 帧归入 Fall |

### 1.2 当前 Controller 绑定（历史快照；不代表当前工程状态）

历史检查点中，`Assets/Animations/Enemy_1011.controller` 的实际 motion 如下；这是部署前快照，不是当前 motion：

- Idle → `Enemy_1011_Idle.anim`
- Attack → `Enemy_1011_Attack.anim`
- HitFlash → `Enemy_1011_HitFlash.anim`
- Dead → `Enemy_1011_Dead.anim`
- Walk → `Enemy_1011_Walk.anim`
- Launched_Rise → `Enemy_101_Launched_Rise.anim`（旧 101）
- Launched_Fall → `Enemy_101_Launched_Fall.anim`（旧 101）
- 没有 `Enemy_1011_Launched_Rise/Fall/Land/Getup.anim`

### 控制器换 motion 的历史位置（路径 = `Base Layer/...`，只换 motion）

| 控制器内路径 | 当前 motion |
|---|---|
| `Idle` | `Enemy_1011_Idle.anim` ✓ |
| `Idle/AnimatorStateTransition/Attack` | `Enemy_1011_Attack.anim` ✓ |
| `Idle/AnimatorStateTransition[2]/HitFlash` | `Enemy_1011_HitFlash.anim` ✓ |
| `Idle/AnimatorStateTransition[2]/Dead` | `Enemy_1011_Dead.anim` ✓ |
| `Idle/AnimatorStateTransition[4]/Walk` | `Enemy_1011_Walk.anim` ✓ |
| `Idle/AnimatorStateTransition[5]/Launched_Rise` | `Enemy_101_Launched_Rise.anim` ← 待做 |
| `.../Launched_Rise/AnimatorStateTransition/Launched_Fall` | `Enemy_101_Launched_Fall.anim` ← 待做 |

### 1.3 状态名不可改（历史代码快照）（代码按状态名/Trigger 驱动，`Assets/Scripts/Enemy/Enemy.cs`）

```csharp
private void PlayIdleVisual()   => _animator?.Play(IsCoward ? "CowardIdle"     : "Idle", 0, 0f);        // 808
private void PlayLaunchVisual() => _animator?.Play(IsCoward ? "CowardLaunched" : "Launched_Rise", 0, 0f); // 809
private void PlayHitVisual()    => _animator?.Play(IsCoward ? "CowardHit"      : "HitFlash", 0, 0f);   // 810
private void PlayDeadVisual()   => _animator?.Play(IsCoward ? "CowardDead"     : "Dead", 0, 0f);       // 811
// Attack / Walk 走 Trigger：753 行 SetTrigger("Walk")、859 行 SetTrigger("Attack")
```

关键时序（都已在代码里核实）：
- **Walk**：仅**补齐移动（rush）**触发；`_animator.speed = Mathf.Max(1f, 0.6f / moveSpeed)`（moveSpeed 0.3 → **2×**）；推进 `moveDuration = moveSpeed = 0.3s`；推进结束 `_animator.speed = 1f` + `ResetTrigger("Walk")` + `PlayIdleVisual()`（每排从第 0 帧重放）→ **clip 基长保持 0.6s**，否则循环与弹跳错拍。
- **Attack**：`attackSequence[0]` 的 `spawnDuration = 2.0s`、`drawDuration = 1.0s`（`isRanged=false`、`useFlip=false`）→ 0–2.0s 前冲（DOTween）→ **2.0s 处出伤害**（同时结束霸体）→ 2.0–3.0s 回位；**命中帧必须落在 ≥2.0s**。
- **Dead**：`Play("Dead")` + 闪白 → 腾起 → 旋转下落 → 复位回收（总约 1.1s，clip 长度不敏感）。
- **Launched_Rise/Fall**：前者代码播放，后者由控制器过渡触发；与 prefab 的 `launchDuration = 0.8s`、`launchedHitExtendDuration = 1.0s` 配套，替换后必须实测击飞落地时机。

## 2. 标准工序（每套动画照抄）

### 2.1 视频生成
- 参数（已验证四次，输出恒为 **960×960 / 24fps / 97 帧 / 4.04s**，请求的 720p 不等于实际尺寸）：
  `model=seedance-2-fast`、`duration=4`、`resolution=720p`、`aspect_ratio=1:1`、`generate_audio=false`、`watermark=false`、`seed=-1`；content = text + first_frame + last_frame。
- 首尾帧都用同一张**已验收的绿幕站姿**：`Library/Locus/tmp/sword_enemy_hit_front_v2/anchor_green.png`（960²；绿键前景框 x[155,697] y[187,776]；最小留白 16.1%；背景中位 RGB(0,177,64)）。
- 运行脚本（复制改路径/提示词即可，支持 `create|poll|status|adopt` 与 `--prompt/--out/--record`，poll 带断点续传与 ftyp/box 校验）：
  `Library/Locus/tmp/sword_enemy_walk_v1/run_walk_video.py`（原始版 `.../sword_enemy_attack_front_v1/run_attack_video.py`）。
- 纪律：**提示词先给中文对照**、**收费前取得用户确认**、**不自行重试**；单次 4s 的 `cost.spend ≈ 0.48888`（币种未推断）。
- 提示词必须写满 5 层（情景/动作语义/量化读点/硬约束/节奏）；动作量大时**防超框段要一起写**（`ABSOLUTE CONTAINMENT RULE - HIGHEST PRIORITY`，≥8% 空白、越界就把姿势做紧凑）；**行进类动作的手臂摆动必须写成主读点并给度数区间，不能锁剑的角度**。

### 2.2 取帧（v4 的教训最完整）
1. 先解码成帧（`ffmpeg` 二进制在 `H:/locus/data/managed-python/windows-x64/python-3.13/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe`）。
2. **先找静止段**：逐帧前景像素变化 <400 px 的即静止（v4 是 `f001–f010` 与 `f091–f097`）。**把静止帧当相位是必错**（我踩过）。
3. 在运动区里**用前景像素的局部极小点定跨步分界**（v4：f012/f023/f034/f044/f054/f060/f070/f080，间隔 10–11 帧 ≈ 0.44s）。**不要用靴底簇信号**（会被高抬膝与剑尖污染，导出伪周期 P=12/18）。
4. 取**一个**跨步内的 N 个相位（本项目 N=4 或 6），评分用**相邻相位轮廓 xor 的相对离散度**（越小越顺滑），并做门：两两不重复、左右各一次抬腿、s=L−R 出现两种符号。
5. 脚本可参考 `Library/Locus/tmp/sword_enemy_walk_v1/` 下的 `pick6_walk_v4_final.py` 与 `analyze_walk_v1.py`。

### 2.3 抠图（**路线结论见 §3**）
- **首选 GPT 语义去背**（1 帧探针 → 用户认可 → 其余帧）：脚本 `Library/Locus/tmp/sword_enemy_walk_v1/run_walk_cutout_probe.py`（源自攻击帧同款 `cutout_gpt.py`，同角色已验证）：`MUSK_API_KEY`、`https://api.muskapis.com/v1/images/edits`、`gpt-image-2.5-sunburst`、`quality=high`、`input_fidelity=high`、`size=960x960`、`background=transparent`、`output_format=png`。
- 本地绿幕键（`Library/Locus/tmp/matting_green_screen_v2_CANONICAL.py`，6 步配方）保留作为**慢速/静止源**的免费路线。

### 2.4 对齐
- 逐帧比较抠图结果与源帧的**高度归一化 shapeIoU**（对缩放不变）与**所需回缩倍率**；**轴间倍率必须一致**（GPT 会随机整幅放大，见 §3）。
- 只对 outlier 帧做整幅缩放；平移用**单一全局偏移**；**禁止逐帧锚脚**（会把跑步的腾空相位抹掉）、**禁止逐帧按包围盒归一化**。

### 2.5 补边与导入
- 补边：**源画布居中补边 960→1108 = x+74 / y+120**，**必须无 mask 原始拷贝**（`canvas.paste(im, pos)`；带 mask 的 `paste(im,pos,im)` 会把半透明像素 RGB 乘 alpha）。脚本参考 `.../sword_enemy_attack_front_v1/deploy_pad.py`。
- 导入设置**逐项复制已验收素材**（如 `Enemy_1011_idle1.png`）：PPU 16、Center (554,554)、Point、Uncompressed、Tight、alphaIsTransparency；**`TextureImporterSettings` 不含压缩项，`textureCompression` 必须单独赋值**。复制后读回校验。

### 2.6 clip 与控制器
- 最稳做法：`AssetDatabase.CopyAsset(".../Enemy_1011_Idle.anim", ".../Enemy_1011_<动作>.anim")`（继承 6 键 @0.1s / 30fps / Loop 的骨架）→ `AnimationUtility.SetObjectReferenceCurve` 逐个替换 sprite → `EditorUtility.SetDirty` + `SaveAssets`。
- **`stopTime` 必须显式设**：复制来的值是 **0.5333**（idle 的实际值），会让第 6 姿势只显示 0.033s；本项目动作 clip 用 **0.6**（N 键 × 0.1s 均匀）。注意 `clip.length` 显示值会滞后于 stopTime。
- 控制器换 motion：定位 `Walk`/`Attack` 等状态（`ctrl.layers[0]` 递归 `stateMachines`）改 `state.motion`。**state speed 也影响手感**（Idle 0.75、其余 1）。

### 2.7 Play Mode 实测
- 找实例必须**筛激活中的**：`FindObjectsOfType<Enemy>(true)` 会先返回**对象池里未激活的克隆**（`activeInHierarchy=False` 时 `Animator.layerCount = 0`、`Play()` 完全无效 → 极易误判为接线错）。**我在 Walk 上就踩过**。
- 采样方式：`e.enabled = false`（临时停掉代码驱动）→ `anim.Play(状态名, 0, 0f)` → `await ctx.WaitSeconds(...)` 逐点读 `sr.sprite.name`；对照 `anim.GetCurrentAnimatorStateInfo(0).IsName(...)` 与 `GetCurrentAnimatorClipInfo(0)`。
- 必须验证：序列顺序、循环回环（norm 越过 1.0 后 sprite 回到第 1 帧）、实际速度（Walk 在 speed=2 时应 0.3s 一圈）、切回 Idle 不跳姿势、角色位置不变。

## 3. 抠图路线结论（本次最重要，用户 2026-10-04 定调）

**当前 GPT 语义去背依然是最可靠的抠图路线**。量化依据（同一套判据）：

| 判据 | 本地绿幕键（v4 跑动源） | 已验收参照 | 结论 |
|---|---|---|---|
| 过渡带(d 20–80)/实体 | **0.104–0.118** | dead 0.073 / attack 0.077 / idle 锚点 0.016 | 源本身更软 |
| 边缘相邻实体环 gEx | **−30** | idle −54 / attack −66.6 | 源描边偏绿 |
| 同 d 的 alpha 映射 | 与参照一致（甚至更紧） | — | **不是参数问题** |
| 可见像素 | 274k →（噪声底修正后）146k | 155k | 修掉了压缩幽灵残影 |

- 即：**快速动作源**（跑动等）的边更软、描边偏绿，本地键控**再怎么调参数都到不了验收量级**（噪声底抬高 ✓务必保留、外扩环半径自适应 ✗反而更灰、α<12 归零 ≈无效果），继续拧只会得到文档 §4 记录的另一种坏边（硬边+弱须）。
- **GPT 语义去背的必备纪律**（本次 11 次调用实测）：
  1. **先发 1 帧探针**，读路由指纹（`echoed` 为空 + `mode=RGBA` + `alpha_max=254` = 原生 alpha；坏路由返回 RGB + 画格子）→ 用户目检认可（**浅色/白底是主验收面**）后才处理其余帧。
  2. **模型不重画姿势，但会把整幅画随机放大**：shapeIoU 0.946–0.976（同姿势）而所需回缩倍率 **0.740–1.000**（同一帧不同次调用：f027 = 0.820/0.917/0.958、f032 = 0.791/0.933/0.740）→ **单帧视觉通过 ≠ 动画可用**，必须做**帧间倍率一致性择优**（本次 f027 取 v3×0.958、f032 取 v2×0.933，其余 4 帧 ≤±1% 零重采样）；不达标就**换时间/新路由重试**（保留 v1/v2/v3 全部候选），不要堆提示词。
  3. 产物全部版本化：`C:/Users/Administrator/Pictures/gptGen/sword_enemy_walk_gpt_cutout/`（PNG + request/response json）。

## 4. 资产与数值速查

**1011 资产**
- 精灵：`Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_{idle1..6, hit1..5, attack1..16, dead1..6, walk1..6}.png`
- clip：`Assets/Animations/Enemy_1011_{Idle,HitFlash,Attack,Dead,Walk}.anim`
- 控制器：`Assets/Animations/Enemy_1011.controller`
- 预制体：`Assets/Resources/EnemyPrefabs/Enemy_1011.prefab`（**文件名必须是 `Enemy_{id}.prefab`**，EnemyPool 按文件名解析 enemyId）

**部署约定（与 101 逐项一致）**
- 画布 **1108×1108**、**PPU 16**、**Center pivot (554,554)**、Point、Uncompressed、Tight
- 单位 `transform.localScale = 0.083168`；碰撞盒 local size/center **×2.1643**（世界碰撞盒 0.502×1.287×0.036、中心 y 0.0246）
- 血条 `EnemyHealthBar.yOffset = 35.28`；**脚底落在自上第 897 行**（= 101 的地面线；Walk 的落地帧实测 906 行，比它低 9px = 0.047 世界单位，可忽略）
- 补边 = `x+74 / y+120` 居中补边，**禁止横向重锚定**（踩过：按“脚心对齐画布中心”会把整幅画左推 0.74 世界单位，看起来差一列并与左邻重叠）
- 验收硬指标（与 101 逐项比对）：精灵世界框 **5.76**、脚底世界 y **−1.789**、碰撞盒世界 **0.502×1.287×0.036**、血条世界高 **2.934**、角色 alpha 质量中心相对变换原点偏移与 101 同量级（基准 101 = −0.04 单位）

**测试战斗配置（改这里才生效）**
- `Assets/Experiments/CurvedScroll/RouteTrialData/SmallBattle.asset`（stageId −9101）；`Battle.scene` 的 `Battle Y Route Host` 三个槽位都指向它
- 第0排 `enemyIds` 当前 `[101, 101, 1011, 101, 101]`；索引 0..4 = 列 0..4，**世界 x = 索引 − 2（列距 1.0）**；`0` = 空槽、`999` = 节奏门
- YAML 编码：小端 int32 十六进制（101 = `65000000`、1011 = `f3030000`、空 = `00000000`）
- `Assets/Resources/StageConfigs/*` **未被这条流程使用**（改它们不生效）

**攻击命中判定（排查“打不到”看这条）**
- `InputManager.GetColumnFromScreenPosition`：只考虑“有前排敌人的列”，取与输入屏幕 x 最近者；**距离超过半列宽（Screen.width/10）直接阻断**（返回 −1）。空列多时命中窗口明显变窄。

## 5. 待办

### P0 Launched_Rise + Launched_Fall（峰值关键图已生成，待用户验收）
- [ ] 两段**成对**制作与替换（`PlayLaunchVisual()` 播放 Rise；Fall 由控制器过渡触发；空中再次受击通过 `ReRise` 回 Rise），并在 Animator 里核对过渡的 Exit Time / 条件是否为 1.0
- [ ] 已确认 1011 Prefab 实际参数：`launchDuration = 0.8s`、`launchGravity = 20`、`launchYHeightMin/Max = 2.8/3.2`、`launchReboundVelocity = 8`、`launchedHitExtendDuration = 1.0s`；Rise/Fall 只是 Sprite 局部姿态，世界 Y 飞行由 `Enemy.UpdateLaunch()` 控制
- [ ] 当前 `UpdateLaunch()` 落地分支直接 `PlayIdleVisual()`，没有独立 Landing 状态；落地尾段已生成并验收，起身回 Idle 视频已生成待用户验收。后续仍需 `Launched_Land`/起身短过渡接入 Animator/代码来避免 Fall → Idle 硬跳；关键词：`prompt_landing.txt` / `prompt_landing_v1.txt` / `prompt_getup_v1.txt`，尚未改 Animator 或代码
- [ ] 旧 101 clip 接线目标：Rise `30fps / stopTime 0.55 / Loop off`；Fall `30fps / stopTime 0.45 / Loop on`。本次落地尾段先按视频验收；起身段验收后再单独制作并决定最终 clip/状态接线
- [ ] 关键词与制作说明：`Library/Locus/tmp/sword_enemy_launch_v1/keywords_design.md`；Rise `prompt_rise.txt`；Fall `prompt_fall.txt`；落地尾段 `prompt_landing_v1.txt`；起身 `prompt_getup_v1.txt`
- [ ] **当前关键图候选未采用为 Rise 末帧**：`C:/Users/Administrator/Pictures/gptGen/sword_enemy_launch_v1/enemy1011_launch_peak_v1.png` 更像早期离地中间帧；未提交新的关键图生成。
- [ ] **Dead v4 空中帧已找到并作为参考**：`Library/Locus/tmp/sword_enemy_dead_v1/clean_raw_v4/f050.png`（lift 9.0%，bbox `[44,180,883,723]`）与 `f052.png`（lift 12.8%，bbox `[60,164,886,701]`）更接近 Rise 高点；已抠图版本为 `matted_dead/dead_f050_final.png` / `dead_f052_final.png`。`f071/f078` 作为后续 Fall 空中失控参考；v1/v2 的 `dead_lastframe*` 作为倒地终态参考。
- [ ] 复用 Dead 帧只借用离地高度、身体展开、后仰和剑脱手关系；不得直接继承死亡语义、尸体摊平或翻滚，正式 Rise 末帧需保留“活着的被动挑飞”状态。
- [ ] **落地尾段 v1 已生成并已验收**：`task_9f5c4081f24c4206afeae33c071e19c3`，`cost.spend=0.48888`，文件 `C:/Users/Administrator/Videos/doubaoVideo/enemy1011_launch_landing_v1.mp4`（2,107,363 bytes；容器已校验）。首帧为 Rise 实际末帧 f097，尾帧为 Dead v2 倒地终态参考；视频不含起身。解码产物：`contact_sheet_landing_v1.png`、`landing_phase_strip_v1.png`、`metrics_landing_v1.json`。
- [ ] **起身 v1 已生成，待用户验收**：`task_b226b12b3dc24b9aae00e0ad63480260`，`cost.spend=0.48888`，文件 `C:/Users/Administrator/Videos/doubaoVideo/enemy1011_launch_getup_v1.mp4`（2,256,652 bytes；容器已校验）。首帧为落地尾段实际末帧 f097，末帧回到 Idle；解码产物：`contact_sheet_getup_v1.png`、`getup_phase_strip_v1.png`、`metrics_getup_v1.json`。视频内容待用户验收，尚未接入 Unity。
- [x] **三阶段各6帧候选已由用户验收（共18帧）**：Rise `f049/f050/f052/f056/f070/f097`；Landing `f001/f006/f010/f015/f020/f097`；Getup `f001/f016/f031/f046/f061/f097`。总览：`Library/Locus/tmp/sword_enemy_launch_v1/all_18_launch_frames_overview.png`；分段表与0.15s预览分别在 `select6_rise_v1/`、`select6_landing_v1/`、`select6_getup_v1/`；总清单 `selected_launch_frames_v1.json`。
- [ ] **抠图状态**：GPT 探针 `rise4_from_f056` 返回原生 RGBA / Color Type 6；批量至 `getup4_from_f046` 时切换 B 路由（RGB / Color Type 2 / 近白背景），按 SOP 停止，未继续消耗。GPT 输出保留在 `C:/Users/Administrator/Pictures/gptGen/sword_enemy_launch_cutout_v1/`。
- [x] **18帧本地抠图已完成**：`local_key_rise_v1/`、`local_key_landing_v1/`、`local_key_getup_v1/`；白底绿像素全部 0；聚合报告 `launch_local_key_report_v1.json`。
- [x] **18帧已复制并导入 Unity**：`Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_rise1..6.png`、`Enemy_1011_landing1..6.png`、`Enemy_1011_getup1..6.png`；均为 1108×1108、Sprite Single、PPU16、Point、Uncompressed、Alpha、Tight，并复制 `Enemy_1011_idle1.png` 的实际 pivot。
- [ ] **尚未创建动画 Clip/接入 Controller**：当前仍没有 `Enemy_1011_Launched_Rise.anim`、`Enemy_1011_Launched_Fall.anim` 或 `Enemy_1011_Launched_Land.anim`；`Enemy_1011.controller` 和运行时代码本轮未修改。
- [ ] 提示词的语义读点：被动受力（不是主动跳）→ 骨盆先离地、胸头后仰、四肢被甩、剑从画面左侧握持关系中被动脱离但保持入框；**不要落地姿势、不要死亡躺地、不要大幅画布内上移**

### P1（进行中）左 / 右方向受击

**已确认的设计结论（2026-10-04 与用户逐版裁定得出）**
- 方向语义：「从左受击」= **力来自画面左、把人推向画面右**
- **允许小幅侧倾**（用户裁定；原「纵轴必须完全垂直」只适用于正面受击）。方向读点采用模型实际能实现的约定：**受力侧肩被压下并后撤 + 肩线陡倾 + 躯干折向被推方向 + 头被甩向被推方向并压进肩里**。实测：写“受力侧肩**抬高**”会被模型做成相反方向（v2/v3 关键图均为受力侧肩下沉）
- 幅度门槛（960 画布）：肩线倾斜 Δ(R−L) 约 **40–55px** 才读得出受击；低于 ~26px 会读成“自然侧身”
- 头部必须明显转头（约 **35°**、下巴朝右肩、面罩不再正对镜头），否则读成“侧目”
- 每帧 0.15s（6 帧 = 0.9s）；clip 命名 `Enemy_1011_HitLeft/HitRight.anim`；末帧沿用 `Enemy_1011_idle1.png`
- **方向选择规则**仍未定（候选：`impactDirection.x`，|x|<0.5 → 正面 HitFlash；x>0 → HitLeft）。数据源已核实：`SweepEffect`（横扫方向）、`StabSweepEffect`（射线，|x|≈0.2）、`AttackWave`（挑飞=竖向），其余伤害源不传方向 → 正面回退。共享血组 `SharedHealthGroup.cs:105` 调 `ApplyDamageFeedback` 不带方向，需补参数才能分方向

**用户已认可的姿势（关键图）**
- `C:/Users/Administrator/Pictures/gptGen/sword_enemy_hit_left_keypose_v6.png` = **v3 身体 + v5 强度的转头**，用户裁定「v6 差不多，后仰幅度稍稍差一点点」。唯一小遗憾：后仰略小

**已完成/已废的版本（细节见各自目录的 verdict*.md）**
| 版本 | 产物 | 裁定 |
|---|---|---|
| 视频 v1 | `sword_enemy_hit_left_v1.mp4` | 废：抬剑换姿势 + 2.3s 保持，无受击感 |
| 关键图 v1/v2/v3/v4/v5 | `sword_enemy_hit_left_keypose_v1..v5.png` | v1 整幅压扁废；v2 方向弱；**v3 身体被认可**；v4 转头不足；**v5 头被认可**（整幅 +12%、脚底 +31px） |
| 视频 v2 | `sword_enemy_hit_left_v2.mp4` | 废：模型自创姿势（头右移 184px、顶行 +60、宽 +149px），用户判「像向左倾斜、没有后仰、像摆 pose」 |
| 视频 v3 | `sword_enemy_hit_left_v3.mp4`（`task_3992f2cba10041af902a0aff47a43e05`，0.48888） | **待你裁定**：首尾已回到同一张 idle，起跳/回位各 0.42s、97 帧最低行恒为 775；但变形姿势仍被**冻结 1.21s**（第三次），且峰值几何远比 v6 夸张（宽 +200px、上体右移 +71px、左带剑区 −12.4k、右带 +11.7k）。细节：`Library/Locus/tmp/sword_enemy_hit_left_v3/verdict_v3.md` |

**已作废的方案**：曾把已认可 v6 极值当**首帧**（`Library/Locus/tmp/sword_enemy_hit_left_locked_v1/`，未提交、未计费），会被判成「从 idle 突然切到姿势再返回」。**首尾必须都是 idle**，正确结构是 `idle → 左侧重击 → 连续受力与回弹 → idle`（v3 按此提交）。

**视频 v3 已被用户验收（2026-10-04）**；已选出 6 键（v2 修订版：6 键全部取自视频、同密度），待用户验收选帧结果。

**Animator 约束（决定末键）**：`Enemy_1011.controller` 的 `HitFlash` 状态有一条**出口过渡回 `Idle`**（`m_ExitTime = 0.9`、`m_TransitionDuration = 0`、无条件）→ clip 在 0.9s 的 90%（0.81s）被**瞬时**切回 Idle，所以**末键必须视觉上≈idle**，但不必是已部署的 idle1。

**选帧 v2（用户否决 v1 的「f072 + idle1」后重选，依据 `Library/Locus/tmp/sword_enemy_hit_left_v3/select6_v2/selected_frames_v2.json`）**
- 命中：**f024（56%，接触）/ f033（100%，极值）/ f069（86%，回收：躯干回位 + 剑臂向左反甩）/ f070（65%，回收：躯干到位、头仍滞后 55px）/ f071（41%，回收：头继续回正）/ f075（6.5%，收到位）**。
- v1 被否的原因：f072（27%）及其后的帧在视觉上已经回到 idle 图，回收信息已耗尽。回收段改用真正有读点的 f069/f070/f071；末键换成**同源视频帧 f075**（bbox `[154,186,695,775]` 与 idle `[156,188,695,775]` 只差 2px），既满足瞬时出口不跳，又让 6 键**同一像素密度**（不再依赖内容框宽 +118px 的 `Enemy_1011_idle1.png`）。
- 变形量序列：56 → 100 → 86 → 65 → 41 → 6.5%（快进慢出）；相邻帧像素差 144k–207k（远高于 10k 防重复闸门）；6 帧脚底行恒为 775。
- 源帧副本与目检产物：`Library/Locus/tmp/sword_enemy_hit_left_v3/select6_v2/hitLeft1..6_from_f0XX.png`、`pick6_v3_v2_sheet.png`、`pick6_v3_v2_preview_0p15s.webp`（6 键 × 0.15s = 0.9s）。
- **部署前必须处理**：① 左缘 0/26/0/4/64/154 px —— f024、f069 贴着画布左缘、f070 距 4px，补边 (74,120) 后为 74/100/74/78/138/228 px，不裁切但**低于 15% 安全边距**；② 最宽的是 **f069 的 754 px ≈ 3.93 世界单位**（f033 740、idle 542），列距仅 1.0 → 与邻兵交叠会大于 idle。

**下一步（用户 2026-10-04 定：先暂存、之后再部署）**：按 `Library/Locus/tmp/sword_enemy_hit_left_v3/for_deploy/DEPLOY.md` 导入 → 建 `Enemy_1011_HitLeft.anim` → 接控制器状态 → Play Mode 实测。

**抠图（2026-10-04，进行中）**
- **GPT 语义去背不可用**：探针发了 2 次（间隔 4 分钟）**都落在 B 类后端**（`echoed` 无 `background`、`output_tokens = 6732`、PNG Color Type **2**、给近白噪声底 240–255），按 SOP **立即停手未批量**；输出留在 `C:/Users/Administrator/Pictures/gptGen/sword_enemy_hit_left_gpt_cutout/` 作失败候选（v1/v2）。
- **本地绿幕键（SOP 兜底路线）已跑完**：`Library/Locus/tmp/sword_enemy_hit_left_v3/local_key_v2_final/`（6 帧）；脚本 `run_hitleft_local_key.py` = `matting_green_screen_v2_CANONICAL.py` 逐行算法 + 一步可测量的幽灵清除（`GHOST_ALPHA = 0.6`、`GHOST_RADIUS = 5`：仅删“离实心剪影 >5px 且 alpha<0.6”的散射像素）。
- 指标（6 帧）：白底绿像素 **0**；bandRatio **0.066–0.077**（与 attack 0.077 / dead 0.073 同量级，不是 walk 的快速动作源 0.104–0.118）；羽化剖面 226–228 / 209–214 / 164–176 / 134–153 / 84–115（≈文档「合格形状」224/207/157/127/87）；ring gEx −19.2…−20.7（优于已验收 hit3 的 −15.0）；outline_ratio 0.857–0.931；可视 bbox 与源帧差 ≤5px（f024 `[0,184,710,779]` vs 源 `[0,188,705,775]`）。
- **幽灵清除的实测效果**：可视像素 199k→168k（f071）、淡像素带 1.35–3.97% → 0.50–0.65%（已验收基线 0.09–0.16%）；未清除时可见 bbox 会虚胖到 `[0,98,875,779]`，且散射淡像素均值 RGB ≈ **[150,50,178]（品红）** = 反混合在低 alpha 处把色度噪声放大 ~13× 的指纹（h264 压缩幽灵）。
- 目检产物：`local_key_preview_v2/sheet_hitleft_white.png`、`sheet_hitleft_dark.png`、`zoom8x_edge_{white,dark}_hitLeft2_from_f033.png`（与已验收 `Enemy_1011_hit3.png` 同框 8× 边缘对照）。
- **用户已验收（2026-10-04）**：素材已按既有约定补边到 1108 并暂存到 `Library/Locus/tmp/sword_enemy_hit_left_v3/for_deploy/`（`Enemy_1011_hitLeft1..6.png`，实心脚底 895，与已部署正面受击批同基准）；部署步骤与验收硬指标写在同目录 `DEPLOY.md`。**Unity 侧尚未导入**（暂存在 Library 下，避免 Unity 用默认导入设置污染工程）。

**右侧受击（HitRight）关键词设计（2026-10-04，只有文案、未提交生成）**
- 现行词：`Library/Locus/tmp/sword_enemy_hit_right_v1/prompt.txt`；设计依据与「不可镜像项」：同目录 `keywords_design.md`；runner 已就绪 `run_hit_right_video.py`（输出 `C:/Users/Administrator/Videos/doubaoVideo/sword_enemy_hit_right_v1.mp4`；目录无 `record.json` = 从未提交）。
- 旧 `prompt_v1_flinch_style.txt`（15:54）镜像的是**已被否决的左侧 v1 风格**，已留档不再使用。
- 三处**不能**镜像的地方：① **剑在画面左侧**，而右侧受击把人推向画面左 = 推向剑那一侧 → 新增独立的 SWORD SIDE 段（刃尖被动位移 ≤40px、不外甩、不越肩线、不换侧）；② 外扩方向相反且余量不同（idle 左留白 155 / 右留白 263）→ 改成**非对称上限**（向左 ≤40px、向右 ≤60px）；③ 不能沿用左侧 v1「纵轴必须垂直」当唯一红线（那条会让模型只靠体积变化表达）。
- 其余逐字沿用 v3：首尾同一张 idle 锚点、5 层结构、量化上限（头盔下沉 ≤25px / 头横移 ≤60px / 肩线 Δ40–55px / 转头≈35°）、时序、HARD CONSTRAINTS 与 NEGATIVE LIST；参数 `seedance-2-fast` / 4s / 720p / 1:1 / 无音频 / 无水印 / seed −1。
- 预期（左侧三次复现）：模型仍会把变形姿势冻结 1.2–2.4s → 取帧照样只取上升段 + 回落段。
- **视频 v1 已生成（2026-10-04，用户批准）**：`task_71ccbe537c0548d9af0b9a5c0dea6613`，`cost.spend 0.48888`，`C:/Users/Administrator/Videos/doubaoVideo/sword_enemy_hit_right_v1.mp4`（2,160,235 bytes；sha256 `7daca7fe…`；960×960 / 24fps / 97 帧）。
- **客观判读**（`Library/Locus/tmp/sword_enemy_hit_right_v1/metrics_right_v1.json`）：
  - ✓ **方向读点成立**：上身质点 444→**413**（左移 31px）、头部带左缘 330→**300**、画面左肩带顶 287→**144**（上升 ~98px）、画面右肩带顶 216→**325**（下沉 ~109px）；97 帧最低行**恒为 775**；首尾均为 idle（f001–f017 / f079–f097）。
  - ✓ **中段无冻结平台**：静止帧只出现在 f4–f17 与 f79–f97，中段仅 f44/f45 → 优化词里的「任何姿势不得静止超过 ~3 帧」生效（左侧 v3 曾冻结 1.21s）。
  - ✗ **剑臂被甩出画面左**：最左像素到 **x=20–24**（idle 155，外扩 ~135px ≈ 0.7 世界单位）；左带 15,741→**25,186**（+9,445）；宽度峰值 **688**（idle 540，+148），f018 另有 1 帧异常宽 804 → 违反本词「向左 ≤40px」的上限。
  - ✗ **飘带飞到头顶上方**：f060–f069 出现红色 mass 至 **y=126**（高出 idle 头盔顶 62px），且发生在回收段而不是受击瞬间。
  - ✗ f018 单帧异常（宽 804、右带 6,860px），疑似过渡/闪帧，取帧时必须避开。
  - 目检产物：`contact_sheet_right_v1.png`、`preview_right_v1.webp`；脚本 `analyze_right_v1.py`。
- 取帧规则（沿用左侧）：只取受击上升段 + 回落段，丢弃任何静止段；6 键 = 接触 / 极值 / 回收 3 帧 / 收到位，末键必须视觉≈idle（出口过渡 `m_ExitTime 0.9`、`m_TransitionDuration 0` 瞬时切回）。
- **视频 v1 已被用户验收（2026-10-04）**；6 帧候选已生成并获用户验收：**f019（接触）/ f030（极值）/ f051（回收1）/ f054（回收2）/ f057（回收3）/ f075（收到位）**。排除异常 f018；候选全部同源 960²、脚底行 775；f075 变形量 12%，用于避免末端硬跳。产物：`Library/Locus/tmp/sword_enemy_hit_right_v1/select6_right_v1/`。
- **GPT 抠图已完成（用户已批准批量）**：探针 f030 + 其余 5 帧均返回原生 RGBA / PNG Color Type 6，尺寸 960²，Alpha max 254，角落无棋盘格；输出目录 `C:/Users/Administrator/Pictures/gptGen/sword_enemy_hit_right_gpt_cutout/`，总览 `right_cutout_v1_sheet_white.png` / `right_cutout_v1_sheet_dark.png` / `right_source_vs_gpt_v1_sheet.png`，聚合报告 `gpt_cutout_report_right_v1.json`。
- **重要边界**：GPT 视觉抠图通过，但逐帧几何未通过，不能直接部署。输出 bbox 宽/高分别为 `642/588, 797/728, 787/707, 747/723, 634/680, 727/781`（f019/f030/f051/f054/f057/f075），底行 `779,872,864,874,839,907`；bbox shift 最大 `[-68,-61]`。这是 GPT 已知逐帧随机放大/移位行为，下一步必须做单一全局对齐与必要的 outlier 整幅缩放，再补边到 1108；原始 GPT 输出不进入 `Assets/`。

**本方向已花成本**：视频 3 次（1.46664）、关键图 6 次（≈45,129 tokens）。

**尚未做**：HitLeft 的 Unity 部署（按 `for_deploy/DEPLOY.md`）；HitRight 视频生成（待用户批准提交）；**左右受击判定/播放规则未实现**（没有规则就不会 `Play("HitLeft"/"HitRight")`；候选 `impactDirection.x`，符号语义需按实际代码核实，`SharedHealthGroup.cs:105` 需补方向参数）。

### P2 可能微调
- [ ] 手感：受击 0.9s 是否偏长、idle 的 state speed 0.75 是否合适 —— 改法都是**一处数值**（clip 键距或 state speed）
- [ ] 剑横向伸出约 **1.7 世界单位**（相对身体中心），列距仅 1.0 → 与邻兵美术交叠；对齐无法消除，只能改持剑姿势或加大列距

## 6. 已知的坑（避免重复踩）

**提示词层**
- 缺“内容情景”层 → 僵硬平均解；写 `slightly wider` → 被实现成整体放大（“充气”）
- 正面锁定视角下“后仰/前倾”不可投影 → 模型只会左右倾斜；改用正面可见读点（肩上耸、脖压缩、膝屈、手臂惯性）
- **把剑的角度锁死（如“只能跟随 ≤10°”）会连手臂一起冻结** → 走动读成散步。要锁的是**方向红线**（不高过肩线/不指镜头/不横扫/横向伸出不超给定姿势）
- 动作量大时缺**防超框段** → 剑挥过头顶出框（v2 实测 23 帧贴边、最小边距 0px）
- 走 vs 跑是**语义差异**，不是力度差异：跑＝屈肘约 90°、手部泵动、膝抬 18–25% 身高、肩前顶；走路写法会得到“大摇大摆”

**取帧层**
- 把**静止保持帧**当相位（v4 的 f001–f010）→ 循环里混入站立帧
- 用**靴底簇信号**定周期 → 高抬膝/剑尖污染 → 伪周期（P=12、P=18 两次踩过）
- 只在整除数上找周期 → 反而错过真实周期；正确做法是先自相关/局部极小点定分界，再在一个跨步内分相位

**抠图层**
- 本地键控的**噪声底必须盖住实测背景噪声**（v4 的压缩残影落在 gEx 88–91，规范值 6% 会把 13% 画布变成薄雾）
- 外扩环半径**不是越大越好**（自适应 8px 反而让带更偏灰）
- GPT 抠图**会随机整幅放大**（见 §3）→ 必须做帧间倍率一致性择优；单帧通过不等于动画可用
- GPT 图像接口是**多后端路由**，失败时**永远 HTTP 200 出图不报错** → 必须读回 `mode/alpha` 与回显指纹，不能只看 HTTP
- 补边**必须无 mask 原始拷贝**；`TextureImporterSettings` 不含压缩项

**Unity 层**
- `FindObjectsOfType<Enemy>(true)` 先命中**未激活的池克隆**（`layerCount=0`，Play 无效）
- clip 的 `length` 显示值滞后于 `stopTime`；复制 idle clip 后**必须显式设 stopTime**
- 状态名/Trigger 名不许改（代码按名字播放）
- Animator 的 `Play(name, layer, normalizedTime)` 需要**对象已激活**才有效

## 7. 知识索引

| 文档 / 脚本 | 用途 |
|---|---|
| `skill/workflows/character-hit-animation-video-workflow.md` | **主流程**：参考图分工、已验证提示词模板、执行步骤、美术验收、部署；§6.A 第 5–8 条（顺序纪律/中文预览/受击提示词陷阱/抠图路线 SOP）、**§12**（提示词 5 层、关键帧筛选、动画帧对齐） |
| `skill/workflows/image-asset-generation.md` | GPT Image 生成/编辑/**语义去背**（含已验证参数与执行规则）、透明素材部署进 Unity 的尺寸与密度约定 |
| `skill/workflows/local-green-screen-cutout.md` | **本地绿幕键配方**（6 步）、七项验收清单、失败模式、已验收基线；**路线选择看它的开头说明** |
| `skill/gpt-image-generation.md` | 图像接口能力：质量档位、**多后端路由指纹** |
| `skill/reachapi-seedance-video-generation-v2.md` | 视频生成 API：参数、上传、创建、限次轮询、断点续传、容器校验 |
| `memory/video-generation-case-log.md` | **台账**：每个版本的文件/参数/费用/验收结论（1011 全部版本、Walk 的本地键控边界与 GPT 倍率乱象、部署记录） |
| `memory/hit-animation-video-pipeline-lessons.md` | 提示词 5 层、正面视角铁律、节奏、帧筛选、绿幕键与代价、指标陷阱、几何规则 |
| `design/pixel-character-and-enemy-animation-spec.md` | 像素角色动画硬性规范（画布/居中统一锚点/不触边/≥15% 安全边距/禁止镜头运动） |
| 可复用脚本（`Library/Locus/tmp/`） | 视频：`sword_enemy_walk_v1/run_walk_video.py`；取帧：`pick6_walk_v4_final.py`、`analyze_walk_v1.py`；本地抠图：`matting_green_screen_v2_CANONICAL.py`；GPT 抠图：`sword_enemy_walk_v1/run_walk_cutout_probe.py`；补边：`sword_enemy_attack_front_v1/deploy_pad.py` |

## 8. 必须遵守的纪律

1. 关键姿态参考图/探针**先给用户目检**，再复述规格取得确认，之后才批量执行
2. **不自行重试收费任务**；`failed` 且 `cost=0` 只证明未计费，不等于已授权
3. 交付必须贴**完整路径**（含对照图），不用省略号缩写
4. 生成类提示词提交前贴**中文对照版**（提交仍用英文原文）
5. 抠图**先发 1 帧探针**判路由；坏路由**立刻停手**，换时间重试而不是连发
6. 改任何 Unity 资产后**回读校验**；工具回执与磁盘不一致时以磁盘 `exists/size/mtime` 为准
7. 同一时刻只保留一个候选目录，产生新版本时明确宣告旧版作废
