---
id: kd_86bb6e2b-c3a2-4a4a-8bfc-006c413f8f8a
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

> ## 方向否决（2026-10-16，以此为准）
>
> **用户已否决整杆漫画扫掠方向。** 三轮落地后实机观感仍读作「三道独立抓痕 / 一块丝绸」，不是参考图的一道连续弧。用户明确指示：放弃该方向，改为**细微残影**。
>
> 本文档转为**历史记录**：几何、着色、取证过程仍有参考价值，但**不要在其基础上继续调参或重新启用**。第 2 节列出的五条已证伪做法不要重复。
>
> 新工作入口：`plan/slash-weapon-afterimage-handoff.md`。
>
> 收口动作：`Assets/Scripts/Player/AttackSystem.cs::ExecuteSlash` 已摘掉 `enableMangaSweep: true`，正式斩击恢复原枪尖拖尾；`MeshWeaponMangaSweep` 组件（prefab 上 `m_Enabled: 0`）、`WeaponMangaSweep.shader`、`FantasySpear_MangaSweep.mat`、笔触遮罩与 `SweepEffect` 的选择参数全部保留且默认关闭，可随时回退或复用为残影开关。

> ## 交接摘要（跨对话继续入口 — 2026-10-16，历史）
>
> **当前状态：普通左右Slash的整杆漫画大弧，已于本轮按第10节完成“枪尖→枪尾连续衰减”重构；代码/Shader/材质/Prefab值均已落盘（相对Git HEAD为未提交工作区改动），尚未获得用户动态美术验收。** 最终编辑器现场：Edit Mode、Unity 2022.3.62t7、唯一加载场景 `Assets/Scenes/MainMenu.scene` 且未脏。
>
> **本轮（第10节）改动**：`MeshWeaponMangaSweep.RebuildMesh` 改为每个时间切片画一条跨越全杆的连续主弧 + 一条更细的副线（在主带**外侧**）；Shader 改为按 TEXCOORD2 自身宽度坐标着色（金白核心/暖金/朱红凸边），删除 `_OuterRange`/`_InnerRange`；材质 `_EdgeWidth` 0.040→0.220；Prefab 新字段落盘。
>
> **已完成验证**：正式编译/域重载；材质与Prefab回读（GUID `27d8262bc67b9864ea33c9576052febc` 未变）；尾/尖半宽比 0.180、alpha 沿杆长 14→174 单调递增；30/60fps 340顶点·254面、120fps 360顶点·268面；TEXCOORD0/1/2 通道正确；未启用入口不分配、重复 Begin/Clear 幂等；离屏扫描线确认 CORE/GOLD/RED 分层可见。
>
> **未验证**：真实敌群命中与卡肉衔接、HUD/透明排序遮挡、Android 真机、用户动态验收。
>
> **下一步**：用户在 `Assets/Scenes/Battle.scene` 左右挥动验收弧面连续性；按观感只调本组件/材质参数。

# 长枪整体挥击大弧：左右 Slash 原型实施方案

> ## 交接摘要（跨对话继续入口 — 2026-10-15）
>
> **当前状态：普通左右Slash的整杆漫画大弧原型已实施、可进入游戏内验收；尚未获得用户动态美术验收。** 代码/Shader/材质/笔触遮罩/Prefab引用均已落盘（相对Git HEAD为未提交的工作区改动）。最终编辑器现场：Edit Mode、Unity 2022.3.62t7、唯一加载场景 `Assets/Scenes/MainMenu.scene` 且未脏、运行时漫画Mesh数量0。
>
> **本轮改动清单**
>
> | 文件 | 改动 |
> |---|---|
> | `Assets/Scripts/Effects/MeshWeaponMangaSweep.cs` | 新增；默认disabled；时间×杆长动态扫掠Mesh、历史环形缓冲、暂停/清理 |
> | `Assets/Shaders/WeaponMangaSweep.shader` | 新增；单Pass透明预乘，金白主弧+内弧+朱红外缘，笔触/年龄尖收 |
> | `Assets/Models/Weapons/FantasySpear/Effects/FantasySpear_MangaSweep.mat` | 新增共享材质 |
> | `Assets/Models/Weapons/FantasySpear/Effects/Textures/FantasySpear_MangaSweepMask.png` | 新增128×32本地笔触遮罩 |
> | `Assets/Scripts/Attack/SweepEffect.cs` | `Create` 尾部新增 `enableMangaSweep=false`；接入Begin/SetProgress/SetPaused/EndEmission/Clear |
> | `Assets/Scripts/Player/AttackSystem.cs` | 仅 `ExecuteSlash` 传 `enableMangaSweep:true` |
> | `Assets/Prefabs/FantasySpear3D.prefab` | 根节点新增默认disabled组件及显式引用；Prefab GUID `27d8262bc67b9864ea33c9576052febc` 与尺寸未变 |
>
> **已保存的关键参数（Prefab回读值）**：`historySeconds=0.28`、`sampleInterval=1/60`、`maxSampleGap=0.1`、`emitStart=0.08`、`peakStart=0.22`、`peakEnd=0.74`、`emitEnd=0.92`、`maxAlpha=0.92`、`sortingLayer=Default`、`sortingOrder=4`、`teleportLengthRatio=0.8`；`shaftPoints=[WeaponTail, Shaft_00, Shaft_01, Shaft_02, WeaponTip]`；12个杆长采样点（含0.38/0.57内弧边界与0.70外弧边界）。材质：`_OuterRange=(0.70,1)`、`_InnerRange=(0.38,0.57)`、`_CoreColor`象牙白、`_GoldColor`暖金、`_RedColor`朱红、`_HistoryCurve=0.85`、`_BrushBreakup=0.72`、`_AgePower=1.18`、`_DebugGeometry=0`。
>
> **A/B与回退**：`renderMangaSweep`+`replaceTipTrail` 两个Inspector开关见第9节。完全回退第一步是在 `AttackSystem.ExecuteSlash` 移除 `enableMangaSweep:true`；组件默认disabled存在不会影响其他招式。
>
> **已验证**：正式编译/域重载；Shader导入无错误；Prefab/材质回读；真实 `ExecuteSlash` 左右空目标轨迹取证；30/60/120fps等效步进最大值216顶点/374三角面（预算512/1024）；全局暂停与局部卡肉冻结/恢复；非opt-in入口不分配历史与Mesh；正常结束与提前Kill后 SweepEffect/Mesh/容器为0。
>
> **未验证或未覆盖**：用户连续动态观感；真实敌群命中与伤害回归；背景/HUD/透明排序遮挡；Android GPU与真机帧率；正常速度录屏。取证场景没有敌人。
>
> **下一步**
> 1. 用户在真实Battle（`Assets/Scenes/Battle.scene`）左右挥动，验收弧面方向/面积、金白核心与朱红外缘、遮挡和命中卡肉衔接。
> 2. 按观感反馈只调本组件/材质参数；不改动作轨迹、时长、伤害、推退、命中判定。
> 3. 只有普通Slash验收通过后，才为C4淡扫掠层、幻影、Stab、Launch、Parry/QTE另开接线窗口。
>
> **本任务边界（继续保持）**：不改枪杆弯曲方向/幅度与红缨模拟；不改伤害/推退/QTE规则；不推广到其他招式；不迁移旧实验场景；不付费生图；不提交或推送Git。
>
> **复现验证的最小方式**：Edit Mode下以Additive加载 `Assets/Scenes/Battle.scene`，禁用 `StageController.autoStartStage`（并停用FakeRouteRuntime/RouteStageRuntimeV2避免抢流程），再用空目标列表调用 `SweepEffect.Create(wavePos, DamageType.Slash, 0f, new List<Enemy>(), true, 3f, 60f, 0.4f, prefab: FantasySpear3D prefab, useEnhancedSlashMotion: true, enableMangaSweep: true)`，只播视觉、不产生伤害。停止Play、不保存场景。截图与CSV证据路径见第9节。

## 1. 目标、范围与当前状态

以已验收的 `StabWeaponArtSource/17_MangaMotionConceptV02/stab_manga_sweep_concept_v02.png` 为美术目标：整把枪挥出一条宽阔金白主弧，朱红外沿和内层弧带属于同一次扫掠，枪杆形变与力量方向一致。

用户随后明确同意先为Slash实施。本轮已完成普通左右Slash原型：新增组件、Shader、材质、笔触遮罩和Prefab引用均已落盘；以临时Additive Battle Play现场取证，没有保存场景资产。当前为可游戏内验收的原型，未获用户动态美术验收；具体结果及边界见第9节。

首个可交付范围仅为**普通左右Slash**：

- 保留V02本体材质、模型/骨骼/权重、尺寸、原动作轨迹、时长和所有伤害/推退判定。
- 不做武器残影、BakeMesh、全屏模糊、杆身分散小短线；概念PNG不直接贴到场景充当特效。
- 保留已认可的大弧规模、金白核心和朱红外沿。游戏遮挡确需调节时再对照说明，不能预先以“太亮”为由退成细拖尾。
- C4扫掠层、构筑幻影和其他动作默认不启用；普通Slash通过用户游戏验收后再另行适配。
- 不修改当前暂缓的弯曲方向/幅度或红缨模拟。新弧跟随已有形变；若现有形变方向影响观感，单列证据讨论。

## 2. 已核实的关键接线与限制

### 2.1 必须显式区分共用消费者

`Assets/Scripts/Attack/SweepEffect.cs::Create` 同时用于：

| 消费方 | 当前行为 | 首原型 |
|---|---|---|
| `AttackSystem.ExecuteSlash` | `useEnhancedSlashMotion=true`，普通左右/斜向Slash | 明确开启 |
| `AttackSystem.PlaySweepPresentation` | C4的淡扫掠层，也使用enhanced路径 | 默认关闭 |
| `AttackSystem.ExecutePhantomAttack` 的Slash分支 | 透明幻影，非enhanced路径 | 默认关闭 |

不能只按 `useEnhancedSlashMotion`、伤害类型、alpha或“是3D枪”判断开启，否则会误影响C4/幻影。计划在 `SweepEffect.Create` 末尾追加可选参数 `enableMangaSweep=false`，仅普通 `ExecuteSlash` 传true；既有调用保持原行为。实施前再次搜索全部调用点和序列化绑定，新增参数不删除旧签名字段。

### 2.2 现有姿态与时间线

- 普通Slash配置：`Assets/Prefabs/UI/Skills/Zhangfei_Slash.asset`，actionDuration=0.5、halfWidth=3、fanAngle=60、视觉起点Y=-0.3/Z=-3.6。实际动作时长仍由 `GetAttackDuration` 决定。
- `SweepEffect` 的enhanced路径建立 `SlashMotion → SlashVisualPath → SlashVisual`。以0.5秒总时长为例：挥动约0.35秒、惯性0.06秒、渐隐0.09秒；同时有很短的视觉归位Tween。弧窗口按实际swingDuration归一化，不写死0.35秒。
- Sequence显式 `SetUpdate(UpdateType.Normal,false)`，主轨迹在Update执行；`MeshWeaponShaftBend` 在默认顺序LateUpdate更新杆骨，`WeaponTasselDynamics` 在LateUpdate(1000)运行。不是主轨迹与弯曲都在默认LateUpdate抢同一时机。
- 命中靠根节点X阈值与目标排序；不得改为新弧网格的Collider或面积判定。
- `HitStopRoutine`暂停Sequence和红缨，使用实时等待，不保证timeScale=0；新弧需显式暂停信号。

### 2.3 可直接使用的骨链

正式 `Assets/Prefabs/FantasySpear3D.prefab` 具有枪尾、Shaft_00/01/02和枪尖引用。各点在武器根局部空间的Y约为 -0.488656、-0.331656、-0.173656、-0.015656、0.488656，轴线存在共同X/Z偏移，不能用 `transform.position + up * length` 替代真实锚点。

`WeaponTip`在Shaft_02下，现有杆骨姿态可以直接作为形变后的折线控制点。无需重绑骨、读CPU网格或启用FBX Read/Write。红缨不决定弧形中心与发射开关，不为其每段布褶额外发特效。

## 3. 最小组件和资产改动

以下代码和资产已创建/接入。工作区原有大量未提交改动，写入前保留目标快照；Prefab回读和快照对照确认仅新增本轮扫掠组件。

| 文件/资产 | 工作 |
|---|---|
| 新 `Assets/Scripts/Effects/MeshWeaponMangaSweep.cs` | 骨链采样、历史环形缓冲、动态扫掠Mesh、阶段/卡肉/清理 |
| 新 `Assets/Shaders/WeaponMangaSweep.shader` | 金白主带、内层留白、朱红外沿、硬边断笔；独立透明路径 |
| 新 `Assets/Models/Weapons/FantasySpear/Effects/FantasySpear_MangaSweep.mat` | Inspector可追踪的共享材质 |
| 同目录 `Textures/FantasySpear_MangaSweepMask.png` | 本地制作小型笔触遮罩；不再收费生图，不从概念图抠一整片月牙 |
| `Assets/Prefabs/FantasySpear3D.prefab` | 新增默认休眠组件，显式拖入MeshWeaponVisual、骨链和材质；原数据/GUID保留 |
| `Assets/Scripts/Attack/SweepEffect.cs` | 默认false的选择参数、普通挥动阶段进度、卡肉/结束通知 |
| `Assets/Scripts/Player/AttackSystem.cs` | 仅ExecuteSlash入口传入true |

配置组件挂在武器Prefab根，便于所有骨骼/材质从Inspector追踪，默认disabled；Awake不创建Mesh/缓冲，只有普通Slash显式Begin后启用采样。SweepEffect缓存组件引用，调用Begin/SetProgress/SetPaused/End/Clear等少量API。运行时弧MeshRenderer挂在本次SweepEffect根下，历史世界点每次上传时转换为该根的局部坐标；不能作为枪体子级直接沿用历史局部坐标，造成双重变换。C4、幻影及其他非启用入口不Begin、不分配、不额外绘制。

不新增泛化特效框架或全局静态资产缓存，不改 `MeshWeaponVisual` 正常/渐隐材质，不重写 `MeshWeaponTrail`。A/B时仅对当前普通Slash实例禁用并清理旧枪尖Trail，测试结束恢复；不在正式Prefab上永久停用所有动作的枪尖Trail。

## 4. 整杆扫掠几何：时间 × 杆长

### 4.1 坐标与历史

沿实际骨链构造一条从枪尾到枪尖的连续采样曲线 `P(s,t)`：

- `s`为基于静止杆长的稳定归一化位置，沿骨链以保形分段插值生成8–12处采样，包含实际尾/尖，不按当前弯曲后距离每帧重新归一化UV。
- `t`为这次Slash的动作时间；每个时间切片使用最终杆骨姿态和完整父级变换。历史存世界坐标，渲染上传时统一转为Mesh持有者局部坐标，不被父级当帧移动再乘一次。
- “握持区”只控制内侧留白和宽度权重，不新增物理支点、不修改原Slash变换。实际路径允许平移，不能硬套世界固定圆心。

把 `P(s_i,t_j)` 与相邻杆长/时间样本连接成二维网格。外侧主弧、杆中段内弧、朱红边全部在**同一张扫掠表面**上通过区带控制产生，而不是多个互不关联的TrailRenderer。当前切片贴近真实枪体，为扫掠前缘；历史切片沿已走过的轨迹向后展开。

为外弧预留少量超出枪尖轮廓的视觉外沿，可由最后一段稳定切向延伸，范围有限、纯视觉；不改变枪尖、命中或整体枪长。不能用无限相机朝向扩宽来掩盖错误几何。

### 4.2 区带和尖收

- 第一版包含一条宽外弧、一条较窄内弧及少量朱红外沿；概念可选的第二条内弧待第一轮对照决定。
- 沿s设置相互分离的区带，区带之间是透明留白；主带权重在外端较高，握持/尾端连续收束。年代较老的切片再做宽度/alpha尖收，避免完整白扇面。
- 线条断裂来自稳定笔触遮罩和区带边界，不逐帧随机。纹理纵轴由累计扫过距离或固定发力进度锚定；样本年龄独立传递，避免UV随删旧样本不断拉伸、笔触爬动。
- 真实路径若自交或反向，切断当前历史段；不连接出蝴蝶结或闭合光环。

### 4.3 帧率、插值与预算

调试起点（不是已验收最终数值）：

- 实景迭代将历史窗口从0.14秒扩大为0.28秒：短历史窗在Battle原相机下只留下局部断带，扩大后覆盖大部分真实挥动窗口；没有改动作。
- 当前采样间隔为1/60秒，最多23个历史切片加1行真实前缘、每行12处杆长采样，理论上限288顶点/506三角面。30/60/120fps等效运行最大分别204/204/216顶点、352/352/374三角面，均在512顶点/1024三角面预算内。
- 高于目标频率时合并中间样本，仍保持当帧前缘；30fps等较低帧率时，在前/后**已知**姿态之间插入有限视觉切片。对根平移/旋转/缩放和局部骨链位置进行插值后重建曲线，不只把两端世界点直连成大折面。不回放/改写正在运行的Tween或骨骼。
- 每帧补点次数和总切片数量有上限，不追补卡顿期间所有历史。超过maxSampleGap（初值0.1秒）或瞬移阈值就清历史重新开始。
- 开始阶段没有历史时从真实轨迹生长，不虚构一整圈未来挥击；若真实屏幕轨迹撑不起概念面积，记录投影/扫过角证据，再调整视觉宽度/历史窗口，不能悄悄改动作。
- 新增Mesh/数组在Begin一次分配并复用，LateUpdate无LINQ/new集合/每帧材质实例。每个活动普通Slash目标1个MeshRenderer、1材质、1Pass；禁用旧枪尖Trail时不叠加其绘制。

## 5. 笔触Shader与遮挡

独立Unlit透明材质，首版用预乘alpha（`Blend One OneMinusSrcAlpha`且RGB显式乘alpha）、`ZTest LEqual`、`ZWrite Off`、`Cull Off`，无阴影/探针；不需要全屏后处理或GPU蒙皮。

- 米白/金白核心、暖金过渡、朱红外沿在同一Pass完成；首版不再加独立加法Pass或Bloom。保留概念的大弧强度，通过留白和核心形状保持实体枪与敌人可读。
- 小型纹理只负责硬边笔触和局部缺口；普通透明和很窄的边缘抗锯齿避免发灰/闪烁，不把大弧柔化成烟雾。初始颜色参考已验收概念，后续Inspector调节。
- 不能用 `WeaponColorUnlitFade` 深度预写材质：它会先占据半透明区域，挡住后绘制的弧/特效。
- 保留真实场景深度；当前 `sortingOrder=4`，置于枪体2、旧尾迹3之后，需在真实战斗中检查不透明枪体/场景深度与敌人透明排序。sortingOrder不是万能置顶或世界深度替代。
- 枪尾和历史切片可能穿近裁面：相机前方z有效时才计算屏幕速度/投影，跨近裁面不做无限投影宽度，也不把点钉在屏幕边缘。几何由相机正常裁切，退化面不上传。

## 6. 阶段门控、形变顺序与清理

### 阶段信号

普通Slash创建后Begin，但进入有效发力窗口才发弧。由原移动Tween的OnUpdate读取该Sequence的实际挥动进度，或用等价的只读阶段值传入；不得增加延迟或改变 `CheckHitThresholds` 的调用/回调顺序。

初调 `emitStart01≈0.08`、`emitEnd01≈0.90`，均相对于swingDuration；起收曲线连续，惯性段停止新增切片，已有弧在短尾窗内淡出，最晚与原对象销毁同步。这些值需轨迹记录后确认，不是固定设计。

### 采样顺序

动作Update → 既有默认LateUpdate杆骨形变 → 红缨LateUpdate(1000) → 新采样LateUpdate(1100)。采样不再写枪体或杆骨，不改变既有模拟。只读红缨不作为扫掠曲线控制点，避免自然甩动让大弧抖动。

### 生命周期约定

| 事件 | 新弧行为 |
|---|---|
| 局部命中卡肉 | SweepEffect显式SetPaused(true)，冻结当前弧和年龄、不采新点；恢复SetPaused(false)清空并从当前姿态重种历史，不跨冻结间隔连线 |
| 全局timeScale=0 | 冻结当前弧和年龄；恢复时若实时采样间隔超过maxSampleGap则rebase，不追补停顿轨迹 |
| 正常挥动结束 | 停止发射，现有弧短时淡出，跟随实体alpha收束 |
| 急停/反向/瞬移/超长帧 | 切段或清空，避免回拖及跨帧直线 |
| 禁用/中断/OnKill/OnDestroy | 清理Mesh内容、历史、运行时Renderer/资源；幂等，不影响其他特效 |
| Editor暂停/恢复 | 暂停时自然停帧；恢复检查实时采样间隔，超阈值重种，不追补编辑器停顿 |
| 非普通Slash或幻影 | 默认不Begin，不发弧，也不再次衍生新的尾迹 |

## 7. 实施检查点与验证

### A. 轨迹取证（已完成，见第9节）

在当前真实Battle相机下被动观察用户Slash；如需主动生成可控普通Slash，使用临时Play视觉实例/空目标入口，属于可回退运行验证，不称为“只读测试”。实施获授权后执行，保持原场景/暂停现场。

记录左右/斜向Slash每帧：主根矩阵、杆骨/尾尖点、屏幕投影、Sequence阶段、枪尖Trail状态、局部卡肉。先核实实际扫过方向、角度、面积和近裁面，不能只凭静态概念推定所有动作同心。

### B. 无笔触几何原型（已升级为笔触版，见第9节）

新建组件、材质和只带纯色区带的Mesh；默认休眠并仅由ExecuteSlash开启。目标是枪头外弧和杆中内弧来自同一表面，左右不翻折、斜向正确、近裁面不爆宽。此时不能以“已显示”宣布美术通过。

### C. 笔触、卡肉和可靠性（代码验证已完成，见第9节）

加入稳定遮罩、金白核心/朱红外沿/尖收；接入上述生命周期，三组A/B：旧枪尖Trail、新整体弧、两者合用。实现新序列化组件后完成编译/域重载并独立回读骨链、材质和enable开关。

代码/数值验证采用现有Unity连接的有界脚本或项目已有测试惯例，不为本任务引入新的测试包。检查：

- 30/60/120fps或等效有界步进，切片/顶点上限、不出现NaN/错误索引/历史越界。
- 正常与提前Kill销毁无运行时Mesh/容器残留，重复Begin/Clear/Disable安全。
- 左右Slash原伤害/回调次数、目标顺序和动作时长不变；真实敌人首击卡肉后不喷长弧。
- C4、幻影、Stab、Launch等入口仍不生成新弧；原枪尖特效不被全局停用。
- 纯镜头移动不触发新发射；方向测量将前后世界点投到同一当前相机，排除伪相机速度。门控依然以动作阶段为主。

### D. 用户观感检查点（待用户游戏内验收）

在 `Assets/Scenes/Battle.scene` 实际竖屏Game画面，交付左右各一套起手/发力/命中/收势关键帧及正常速度连续播放对照。已有捕获能力不等于具备录屏工具；能捕获连续片段时提供片段，否则明确截图边界并由用户实际挥动验收。

用户判断重点：**整把枪带出一条有面积、有方向、有力量的大弧；杆身与枪头不是分离特效；枪体弯曲和弧面同属一次发力。** 不以数值或单张截图代替动态美术验收。

### E. 性能与正式范围收口（移动端性能未测）

从首个可见版本开始记录CPU mesh更新、GC和额外绘制；最终检查实际敌群/已有幻影并发的透明覆盖。512顶点/单Pass是几何预算，不是移动端性能保证。用户接受原型并完成必要性能检查后，再保留普通Slash配置；其他招式另立接线窗口。

## 8. 调参与回退

Inspector首版按四组组织，不无条件扩大成通用配置体系：

1. 采样：骨链/visual/材质引用、历史秒数、采样间隔/上限。
2. 弧形：外主带/内带的杆长区域、宽度比例、握持收束、尖端外扩小倍率。
3. 时序：发力起止、开/收alpha、停止尾窗。
4. 颜色/笔触：金白核心、暖金过渡、朱红边、断裂强度、最大alpha。

回退以ExecuteSlash关闭新增选择参数为第一步，恢复原枪尖Trail的实例开关；通过Unity API只移除/禁用本轮新增组件与引用，不整份覆盖Prefab/场景，保留原GUID和并行工作。默认disabled组件存在不应影响未开启的其他招式。正式写入前先记录原Prefab与相关脚本当前差异，写入后另一次调用回读，核对只有本任务新增字段和接线。

范围外继续保持：红缨回收修复、枪杆弯曲方向/幅度重调、骨骼/UV重做、其他招式推广、旧实验场景迁移、伤害/QTE规则调整、付费素材生成、Git提交或推送。

## 10. 枪尖强度渐变优化方案（2026-10-16）

### 10.1 用户观测与目标

用户在真实战斗中观测：当前只有枪尖和枪尾两处明显，中间缺乏过渡。参考图 `StabWeaponArtSource/17_MangaMotionConceptV02/stab_manga_sweep_concept_v02.png` 要求主弧从枪尖一直连续到枪尾：枪尖最宽、最亮、最实，沿杆身向枪尾逐步变窄、变淡；不能只剩两个亮端。

### 10.2 已落地代码为什么没有解决

`MeshWeaponMangaSweep.RebuildMesh` 现在只生成两条独立飘带：

- 外弧固定采样 `outerCenter=0.9`，半宽 `outerHalfWidth=0.075`。
- 内弧固定采样 `innerCenter=0.5`，半宽 `innerHalfWidth=0.02`。
- `ShaftIntensity(center)` 只给整条飘带乘一个常数。外弧约为 0.97，内弧约为 0.54；同一条飘带的两个宽度顶点使用相同 alpha。

因此它只能表现“枪尖附近一条亮带 + 杆身中部一条暗带”，不能表现从枪尖到枪尾的连续衰减。`tailIntensity` 不能再靠调大或调小修正这个结构问题。

### 10.3 落地改法

保留现有时间历史、暂停、卡肉、清理和单个普通 Slash 开关。只改 `RebuildMesh` 的几何：

1. 把外弧改成一条跨越杆身的连续飘带。每个时间切片取 6–8 个稳定杆长样本，覆盖 `shaft=0`（枪尾）到 `shaft=1`（枪尖），而不是只取 0.9 和 0.5 两个中心。
2. 每个杆长样本仍用现有画面右向量做宽度偏移。偏移幅度随 `shaft` 增加：枪尖使用完整 `outerHalfWidth`，枪尾收到约 15%–20%。收窄方向必须沿飘带宽度，不能向镜头或枪尾世界坐标拉动。
3. 同一位置的顶点 alpha 也随 `shaft` 增加：枪尖为 1，枪尾为 `tailIntensity`（0.08），中间用 `SmoothStep` 连续插值。不要把一个常数应用到整条飘带。
4. 原来的窄内弧可保留为第二条更淡的细线，但它不能替代主弧的连续衰减。内弧强度约为同位置主弧的 35%–50%。
5. UV.x 继续表示时间年龄，UV.y 改为归一化杆长。着色器不再用固定 `_OuterRange/_InnerRange` 把两条带从一张曲面裁出来；金白核心和朱红外缘改为沿每条飘带自身宽度计算。
6. 完成后删除已失效的“单点带常数亮度”路径，避免两套强度逻辑同时生效。

### 10.4 验收口径

从 `Assets/Scenes/MainMenu.scene` 单独进入 Play，调用 `MainMenuUI.OnNewGame()`，等待唯一战斗场景和唯一启用相机。在真实左右挥击中截取中段帧。通过标准是主弧从枪尖连续走到枪尾，枪尖最强、中段可见、枪尾最淡；不再是两个分离亮点。不改动作、伤害、推退和判定。

### 10.5 实施结果（2026-10-16，代码与资产已落盘）

按 10.3 改法完成，工作区未提交。

| 文件/资产 | 改动 |
|---|---|
| `Assets/Scripts/Effects/MeshWeaponMangaSweep.cs` | `RebuildMesh` 重写：每个时间切片改画一条跨越 `shaft=0..1` 的主弧飘带；删除 `BandsPerSample`/`AddBand` 固定中心路径与 `GetSwingTangent`/`SampleStored` 旧辅助 |
| `Assets/Shaders/WeaponMangaSweep.shader` | 删除 `_OuterRange`/`_InnerRange`/`_HistoryCurve`；改由 TEXCOORD2（自身宽度坐标 −1..+1）计算金白核心与朱红外缘，新增 `_CoreWidth`、`_TipCoreBoost` |
| `Assets/Models/Weapons/FantasySpear/Effects/FantasySpear_MangaSweep.mat` | `_EdgeWidth` 0.040→0.220（语义从 UV 区间改为“沿自身宽度的比例”） |
| `Assets/Prefabs/FantasySpear3D.prefab` | 组件默认仍 disabled；新字段落盘（`outerShaftSamples` 6 点、`innerShaftSamples` 4 点、`tailWidthScale=0.18`、`innerHalfWidth=0.02`、`innerSideOffset=1.25`、`innerIntensityRatio=0.42`）；GUID 未变 |

新增/替换字段：`outerShaftSamples`、`tailWidthScale`、`innerShaftSamples`、`innerSideOffset`、`innerIntensityRatio`；删除 `outerCenter`、`innerCenter`。副作用线放在主带**外侧**（`innerSideOffset>0`），对应概念图的细外弧；主带自身凸边为朱红。

**已验证**（统一 Append 场景离屏渲染，非 Play；场景未保存，Active Scene 始终 `MainMenu` 且未脏）：

- 尾/尖半宽比 = 0.180，与 `tailWidthScale` 一致；顶点 alpha 沿杆长单调递增（14→174）。
- 17 行历史 / 340 顶点 / 252 三角面；30/60fps 等效 340 顶点·254 面，120fps 等效 360 顶点·268 面，仍在 512/1024 预算内。
- TEXCOORD0/1/2 均按期望通道写入（`uv3.x` 全顶点严格 ±1）。
- 未 Begin 的实例不分配 Mesh/历史；重复 Begin/Clear、暂停/恢复幂等无残留；`EndEmission` 后历史清空且组件自动 disabled。
- 扫描线证据：主带中心为 CORE（近白）、两侧过渡 GOLD、凸边 RED，四条带在 ARC 上均可见。

**未覆盖**：真实敌群命中/卡肉、HUD 与透明排序遮挡、Android GPU 与真机帧率、用户动态美术验收。截图见 `Library/Locus/tmp/manga-sweep-v02/`（`arc-final.png` 全局形态、`battle-framing-ltr/rtl.png` 竖屏取景）。

## 9. 已实施结果与下一入口（2026-10-15）

普通左右Slash原型已接入。`SweepEffect.Create` 新增默认false的 `enableMangaSweep`，只有 `AttackSystem.ExecuteSlash` 传true；C4、幻影和QTE等消费者保持默认关闭。Prefab上新增默认disabled的 `MeshWeaponMangaSweep`，显式绑定WeaponTail、Shaft_00/01/02、WeaponTip及共享材质；MeshWeaponVisual由创建入口传入本次实例，没有运行时字符串资产查找。

采样在LateUpdate(1100)运行，23行环形历史加真实前缘，历史存世界坐标；低帧率最多补3个有限切片，插值根位置/旋转/缩放及局部杆点，绝不回放Tween/骨骼。运行网格挂在外层SlashMotion，单Renderer/单材质/单Pass。Shader使用同一张表面的主弧、内弧与朱红外沿；旧切片向枪尾收束以改善近水平动作投影。笔触纹理按动作进度固定，年龄独立淡出/尖收。

已完成正式编译/域重载、Shader导入（无错误）、Prefab/材质回读、真实ExecuteSlash左右空目标取证、30/60/120fps等效步进、全局暂停及局部卡肉信号测试。暂停顶点/年龄冻结；恢复继续；默认关闭入口不分配历史或Mesh。自然结束后SweepEffect、运行时Mesh及其MeshFilter均为0。另验证了三个A/B组合、重复Begin/Clear和提前Kill：原枪尖模式无新缓冲，新弧模式仅关闭本次旧Trail，两者模式均开启；Kill后等待实际Unity帧号推进4帧，SweepEffect/Mesh/容器均为0。数值检查不等于Android性能验收。

截图证据（都在Library临时目录，可能被清理）：

- `Library/Locus/Screenshots/locus_game_20261015_113127_465.png`：早期左→右中段，0.14秒历史。
- `Library/Locus/Screenshots/locus_game_20261015_113329_666.png`：早期右→左中段，0.14秒历史。
- `Library/Locus/Screenshots/locus_game_20261015_115837_097.png`：采用的0.28秒历史/1÷60秒采样，主弧覆盖更完整。
- `Library/Locus/tmp/manga-sweep-v01/left-to-right-v02.csv` 与 `right-to-left-v02.csv`：早期左右轨迹/投影/预算记录，重复帧号为取证调度重复，不代表独立渲染帧。

下一步为用户进入真实战斗左右挥动验收连续动态：大弧方向/面积、金白核心与朱红外缘、敌人/HUD遮挡、命中卡肉后衔接。测试场景没有敌人，未覆盖真实敌群命中/伤害回归、背景/透明覆盖和Android GPU性能，也未交付正常速度录屏；不得称为最终美术验收。其他招式、红缨、弯曲方向/幅度继续保持原范围。

调参/A-B使用Prefab组件的 `renderMangaSweep` 与 `replaceTipTrail`；保留组件本身disabled（由Slash Begin启动）。`renderMangaSweep=false` 为原枪尖Trail，true且 `replaceTipTrail=true` 为新弧，true且 `replaceTipTrail=false` 为两者并用。当前默认新弧。持久资产参数已保存并回读；Play实验对象/状态不会持久保存。最终现场为Edit Mode、唯一加载场景 `Assets/Scenes/MainMenu.scene` 且isDirty=false；运行时漫画网格数量0。资产生成遗留的空 `Effects 1` 目录已用AssetDatabase删除，正式Effects目录保留。
