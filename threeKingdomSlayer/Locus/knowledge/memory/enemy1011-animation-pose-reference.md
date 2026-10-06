---
id: kd_d6fc3ab9-0b2e-43fd-aa39-5272dbe39462
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 敌人 1011 动画姿态与 Prompt 复用参考

## 用途与维护边界

本文件整理持剑敌军 1011 已有动画案例，供后续新敌人描述动作、编写生成词和筛选关键帧时参考。用户已确认：1011 当前持有的动画基本属于所有敌人的通用动画类别，可作为后续敌人动作设计与姿态设计的基准参考。它是已验收案例总结，不要求后续敌人复制 1011 的具体像素剪影。

维护时必须分别记录视频验收、选帧验收、透明素材验收和 Unity 部署状态；新增结果应有任务记录或用户验收依据。不得把候选姿态、禁止项或量化目标直接写成最终结果。完整英文原词见 `memory/enemy1011-animation-prompt-archive.md`。

## 1. 案例等级与当前状态

| 动作 | 可复用程度 | 实际状态 | 正式资源或素材 |
|---|---|---|---|
| Idle | 正式成功案例 | 已部署，6 帧循环 | `Assets/Animations/Enemy_1011_Idle.anim` |
| 正面受击 HitFlash | 正式成功案例 | 已部署，6 帧非循环，末帧复用 Idle | `Assets/Animations/Enemy_1011_HitFlash.anim` |
| Attack | 正式成功案例 | 已部署，18 个对象引用键，非循环 | `Assets/Animations/Enemy_1011_Attack.anim` |
| Dead | 正式成功案例 | 已部署，6 帧非循环，末姿势保持 | `Assets/Animations/Enemy_1011_Dead.anim` |
| Walk | 正式成功案例 | 已部署；实际动作是原地冲锋跑位，不是慢走 | `Assets/Animations/Enemy_1011_Walk.anim` |
| Launched_Rise | 通用动作基线、已验收并部署 | 6 键、不循环；表达被动挑飞的离地上升与空中峰值 | `Assets/Animations/Enemy_1011_Launched_Rise.anim` |
| Launched_Fall | 通用动作基线、已验收并部署 | 使用 `landing1..6` 六键循环；表达下降/接地尾段，真实落地由代码判定 | `Assets/Animations/Enemy_1011_Launched_Fall.anim` |
| Launched_Getup | 通用动作基线、已验收并部署 | 6 键、不循环；落地后起身回普通行为 | `Assets/Animations/Enemy_1011_Launched_Getup.anim` |
| 左受击 HitLeft | 通用方向受击基线、已验收并部署 | 6 键、不循环；画面左来力、上身向右反冲 | `Assets/Animations/Enemy_1011_HitLeft.anim` |
| 右受击 HitRight | 通用方向受击基线、已验收并部署 | 6 键、不循环；画面右来力、上身向左反冲 | `Assets/Animations/Enemy_1011_HitRight.anim` |

正式 Controller 为 `Assets/Animations/Enemy_1011.controller`，Prefab 为 `Assets/Resources/EnemyPrefabs/Enemy_1011.prefab`，正式帧目录为 `Assets/Sprites/Enemy/Enemy1_1/`。当前已核对并接入 Idle、Attack、Dead、Walk、HitFlash、HitLeft、HitRight、Launched_Rise、Launched_Fall、Launched_Getup。没有独立 `Enemy_1011_Launched_Land.anim`；`landing1..6` 属于 Fall 段。方向受击和击飞流程已通过 Play Mode 验证。

旧记录中的“左受击未验收”“右受击未开始/未生成”“击飞关键词已准备、尚未生成”均为早期阶段快照，不代表当前工程状态。当前应以本文件 §1 表格和 `plan/enemy1011-animation-deployment-handoff.md` 的最终验收结论为准。

## 2. 共用的画面与动作约束

- 这是用于阵列战斗的 2D 游戏 Sprite 动画，不是电影镜头。固定正交正面视角，不推拉、不摇移、不绕人旋转。
- 固定 1:1 画布、角色比例、根部位置和脚底锚点，四周留安全边距。角色局部可以变形或摆动，不能靠整张角色平移、缩放来代替关节动作。
- 头盔、护甲、武器、手脚、飘带等身份特征连续一致，完整出现在画布内。固定装备在哪只手、画面哪一侧；禁止换手、换侧、凭空出现或消失。
- 复用已认可 Idle 作为外观和回位基准。对可回到待机的动作，首尾参考图可以使用同一 Idle；Dead 则不应回 Idle。
- 刚性身体先表达动作因果，武器、手臂、衣物再表现滞后。不要用粒子、血、镜头震动或夸张光效遮盖姿态问题。
- 1011 的像素位置数字只用于该参考画布的读点，不是新角色的通用绝对规格。换画布、身高、武器时应按比例重定目标，并重新检验剪影。

## 3. 各动作的姿态读法

### 3.1 Idle：稳定的站姿，局部呼吸

**意图：**士兵保持阵列中的警戒姿势，活着但不主动表演。

双脚和骨盆稳定，上身只有轻微呼吸、重心微调，头与武器不能显著游走。飘带可有小幅随动，但不能像独立旗帜大幅飘荡。帧间首先保持外观、比例和持剑关系，再追求细小动态。

复用关键词：`in-place idle loop`、`subtle breathing`、`stable planted boots`、`same silhouette and equipment`、`seamless return to the starting pose`。

正式配置：6 帧、Clip stopTime 0.6s、30fps、循环；Idle 状态 speed 为 0.75。30fps 是 Clip 的时间刻度，不表示 6 张不同图每张只播 1/30s；不能混淆帧数、键间隔与 Animator speed。

### 3.2 Attack：主动蓄势，明确命中，再收势

**意图：**角色自己发力攻击正前方目标，不是受到外力冲击。

动作应能拆成“起手/蓄力 → 快速命中 → 收势回 Idle”。蓄势阶段武器与肩臂建立力量方向，命中阶段形成清晰、快速的动作峰值，之后回收。保持根部和脚底稳定，不能靠冲出画布制造力度。

实际成功素材中存在抬剑蓄势。不能把原 Prompt 里的某条武器限制机械地当成最终姿态事实，也不能把成功 Attack 描述成“完全没有抬剑”。评价时应看最终可读姿态与命中时序。

复用关键词：`deliberate wind-up`、`active forward strike`、`fast readable hit beat`、`controlled recovery`、`return to the exact idle pose`。

避免：`recoil`、`flinch`、`thrown by an impact` 等受击词混入攻击主体；攻击者不应像被击退或踉跄。

正式配置：18 个对象引用键、30fps、非循环、stopTime 2.9s；已核对命中键为 2.000s。关键键不等于 18 张不同姿态，复制到新敌人时应重新核对伤害触发与命中图的时间关系。

### 3.3 正面受击：垂直压缩，不靠左右倾斜

**意图：**正面外力突然命中，上半身发生短促冲击反应后回位。

肩线向上耸并向内收，颈部被压短，胸腔收紧，膝盖轻微弯曲；手臂与武器被动滞后。因摄像机正对角色，所谓“后仰”不能仅靠整体变小或身体左右斜倒来表达。

复用关键词：`one sudden external frontal impact`、`shoulders jerk upward and inward`、`neck compression`、`chest tightens`、`small knee flexion`、`passive weapon lag`、`quick recovery`。

避免：主动举剑、换成新防御姿势、跳步、全身缩放、左右摆姿。先确认冲击因果，再评估幅度；更大的剪影并不自动意味着更强的受击感。

正式配置：6 帧、每键 0.15s、stopTime 0.9s、非循环，末帧复用 `Enemy_1011_idle1`。只保留冲击进入、峰值和连续回收，不复制视频中冗长等待。

### 3.4 左受击：画面左来力，上身向画面右反冲

本文左右一律指**画面左右**，避免与角色自己的解剖学左右混用。

力从画面左侧进入，击中画面左肩/上胸；受力侧肩被压低并后撤，对侧肩抬起，上身围绕固定骨盆向画面右后方折开。胸肩先动，头部随后转向画面右并压入肩部。强调“被外力打过去”，不是自然侧身、主动转头或选一个帅气 pose。

成功关键图探索中，v3 的身体和 v5 的头部读法分别获认可；v6 在已认可身体基础上只强化转头，用户评价为后仰幅度仍稍差一点。v6 可用作方向读点参考，不能宣称为毫无保留的理想峰值。

原词的量化目标包括：肩线高差约 40–55px、头盔下沉不超过约 25px、头横移不超过约 60px、转头约 35°。这些是 Prompt 目标，不是视频的全部实际测量。

左受击 v3 视频实际存在峰值冻结约 1.21s：f039–f067 为平台；取帧通过剔除平台重排节奏取得可用性。已验收选帧为 f024 / f033 / f069 / f070 / f071 / f075，6 张最终 RGBA 为 1108×1108，脚底和补边已对齐，文件名为 `Enemy_1011_hitLeft1.png` 至 `Enemy_1011_hitLeft6.png`。

复用关键词：`external blow from SCREEN-LEFT`、`upper body thrown toward SCREEN-RIGHT`、`impact shoulder down and back`、`opposite shoulder up`、`head follows the chest with a short delay`、`fixed pelvis and planted boots`。

### 3.5 右受击：镜像受力链，但不能直接镜像所有装备约束

力从画面右侧进入，受力右肩下压后撤，画面左肩上抬前送，上身向画面左后方反冲，头部延后转向画面左。核心仍是胸肩先动、头与手臂随后、飘带最后收。

1011 的剑在画面左侧，因此右受击把身体推向剑侧。不能直接镜像左受击图片或原词，导致武器换手、剑刃外甩、持剑臂越界。右词另设剑侧被动位移与非对称包络约束：左侧外扩目标 ≤40px、右侧 ≤60px，刃尖目标约 30–40px。Idle 剑尖本已高于肩线，不应使用“剑尖绝不能高过肩线”这种与锚图冲突的禁令。

右受击 v1 视频已获用户验收，但有明确偏差：剑侧最左像素约 x=20–24，相对 Idle 左缘 155 外扩约 135px，违反左扩目标；回收段飘带一度飞到头顶以上；f018 有单帧宽度异常。中段没有左 v3 那样的长冻结平台，是节奏方面的改善，不能因此声称空间约束全部成功。

候选 6 帧为 f019 / f030 / f051 / f054 / f057 / f075，已避开 f018；拟定 0.15s/键、0.9s 非循环只是播放提案，尚非已验收或已部署配置。

复用关键词：`external blow from SCREEN-RIGHT`、`upper body thrown toward SCREEN-LEFT`、`passive sword-side response`、`preserve the idle sword envelope`、`continuous recovery without a frozen plateau`。

### 3.6 Dead：失去支撑，倒下并保持终态

**意图：**死亡导致支撑丧失，角色倒下，不是一次会恢复站立的受击。

主要读点是站立结构瓦解、重心下降、身体逐步落地，头、躯干、手臂与装备保持连续身份。最终姿势必须明显死亡且留在画布中，不重新站起、不回 Idle、不消失。用相邻关键姿态描述落地过程，避免只在站姿与倒地终态之间变形。

复用关键词：`loss of support`、`continuous collapse`、`clear grounded final pose`、`remain down`、`no return to idle`。

正式配置：6 帧、30fps、stopTime 0.6s、非循环，末姿势保持。这里的末姿势保持是死亡动作需要，不应与受击视频里的无意峰值平台混为一谈。

### 3.7 Walk：原地冲锋跑位，不是慢走

这是后续普通敌人通用 Walk/推进动画的主要参考：动作重点是有支撑交替和明确的冲锋节奏，而不是慢速散步。

虽然资源名为 Walk，成功动画表达的是用于入阵/跑位的原地冲锋跑。腿部要有交替支撑、抬腿与迈步读点，上身、头和武器保持足够稳定，不能以整个人横移代替跑步。

“固定脚底锚点”在这里意味着不让整套循环漂移，不意味着两只脚逐帧都钉死地面；跑步允许抬脚与交替落脚。必须区分根部锚定与局部关节自由。

复用关键词：`in-place charging run cycle`、`alternating leg drive`、`stable root position`、`controlled upper-body motion`、`seamless loop`。

对后续敌人的通用参考重点：腿部交替支撑、根部稳定、上身/武器受控随动和首尾无缝循环；不直接复制 1011 的剑侧、身高或像素包围框。

正式配置：6 帧、stopTime 0.6s、30fps、循环，状态 speed 为 1。给新敌人命名时应明确动作语义，不要因沿用 Walk 文件名生成散步动画。

### 3.8 击飞链：Rise → Fall → Getup

这是所有敌人都可复用的三段通用动作语义：Rise 表达离地上升，Fall 表达空中下降到接地/倒地尾段，Getup 表达落地后恢复站立。当前 1011 的 `landing1..6` 已作为 Fall Clip 的循环帧使用，没有独立 Landing Clip；运行时根 Transform 的 Y 位移处理实际飞行，Sprite 只表达局部姿态，不能在画布中模拟完整世界位移。

**Rise（离地上升）：** 外力从下方及略偏前方命中，骨盆和下腹先被托起，胸腔向上并略向后打开，头部延迟后仰，双臂、双腿、剑和飘带按惯性依次甩开。双脚同时离地，剑仍在画面左侧，允许从手中被动短距离脱离但必须完整入框。末帧是可接空中段的 airborne peak，不是落地、死亡或回 Idle。用户确认的 6 帧为 `f049/f050/f052/f056/f070/f097`。

**Landing（落地尾段）：** 从 Rise 实际末帧继续下落，腿和髋先接地吸收冲击，躯干、头、手和飘带连续沉下并进入稳定倒地姿势。它是活着的击飞落地，不是 Dead；不站起、不回 Idle、不加入死亡特效。用户确认的 6 帧为 `f001/f006/f010/f015/f020/f097`。视频约 f020 起进入倒地终态并保持，因此“接触/压缩/过渡”和“稳定尾段”要分开理解。

**Getup（起身恢复）：** 从 Landing 的倒地终态开始，前臂和另一只手先提供支撑，膝和骨盆收拢到身体下方，躯干抬起，头部保持延迟并朝上/偏离玩家，接近站立后才连续回正，剑回到画面左侧的 Idle 握持关系，最后回到精确 Idle。它是受击后的恢复，不是死亡复活、翻滚、攻击或瞬移。用户确认的 6 帧为 `f001/f016/f031/f046/f061/f097`。

三阶段共 18 帧：先从 RGB 绿幕源中选出，随后本地绿幕抠图完成，白底绿像素为 0，并已复制到 `Assets/Sprites/Enemy/Enemy1_1/`。当前已创建并接入 `Enemy_1011_Launched_Rise.anim`、`Enemy_1011_Launched_Fall.anim`、`Enemy_1011_Launched_Getup.anim`，并由运行时代码接通真实落地和起身流程。

**复用边界：** Rise 的身体失控和剑脱手可以复用“被挑飞”的动作语义，但不能借用 Dead 的死亡含义；Fall 的倒地终态不能当作 Dead Clip；Getup 的末帧可以复用 Idle 锚点，但前半段不能提前出现正面 Idle 头部。后续敌人可复用这些动作因果和阶段职责，但必须按自己的体型、武器侧、脚底锚点和包围框重新设计。18 张导入帧的本地抠图报告为 `Library/Locus/tmp/sword_enemy_launch_v1/launch_local_key_report_v1.json`，总选帧为 `Library/Locus/tmp/sword_enemy_launch_v1/selected_launch_frames_v1.json`。

## 4. Prompt 的五层写法

1. **场景和用途**：2D game sprite、阵列战斗、固定正面镜头；说明不是 cinematic。
2. **动作因果**：谁主动发力，或外力从哪里打到哪里；单次还是循环；最终回 Idle 还是保持倒地。
3. **关节读点**：肩、胸、骨盆、头、手臂、武器、腿的运动方向与先后关系；先写足以识别动作的 2–3 个核心读点。
4. **画布硬约束**：比例、根部、脚底、装备、边距、外扩量；禁止摄像机运动、整体漂移、武器换侧。
5. **节奏与阶段**：进入、峰值、回收、终态；循环要求首尾连贯，受击要求快进快出，死亡要求终态保持。

可复用骨架（需按具体动作改写，不是已提交 Prompt）：

```text
This is a 2D game sprite animation, not a cinematic video.
Preserve the accepted character identity, fixed front-facing camera,
canvas, scale and root position.

ACTION AND CAUSE: [intent / one external impact / death / run loop].
BODY READ: [primary joint motion] -> [secondary lag] -> [recovery or final pose].
ANCHORS: [pelvis / root / applicable planted foot constraints].
EQUIPMENT: [weapon side, grip, passive or active motion, safe envelope].
TIMING: [entry / peak / recovery / terminal state].
DO NOT: [only the concrete failure modes relevant to this action].
```

使用 SCREEN-LEFT / SCREEN-RIGHT 定义方向；不要混入含糊的 his left/right。量化上限与 Idle 锚图必须相容。禁止项不是越多越好：“不许动手臂”“纵轴完全垂直”等绝对禁令可能堵住局部反应，迫使模型靠缩放表达。

## 5. 成功经验与失败边界

| 问题 | 案例证据 | 后续处理原则 |
|---|---|---|
| 受击像换姿势或主动摆 pose | 左 v1/v2 被否决 | 写清外力来源与被动因果；固定骨盆，让胸肩和头有先后关系 |
| 只靠体积变化表达后仰 | 过强垂直/不动限制会压掉方向动作 | 区分根部不动与局部上身反冲 |
| 峰值冻结 | 左 v1/v2/v3 分别约 2.3/2.4/1.21s | 提示词不能保证执行；选进入段和回收段，剔除平台 |
| 峰值不静止但装备越界 | 右 v1 节奏改善，剑与飘带仍超包络 | 分别评价节奏、姿态、空间和装备，不给单一“成功”标签 |
| 原词目标与实际结果不同 | Attack 抬剑、方向受击空间超限 | 原词用于复现输入，最终帧用于总结输出，两者分别归档 |
| 去背探针没有真正 Alpha | 左受击 GPT 探针落到不支持透明的后端 | 按既有流程停止无效批量尝试，使用本地绿幕兜底 |
| 视频压缩噪声扩大可见包围盒 | 左受击绿幕边缘发现淡品红幽灵 | 检查透明底、白底与轮廓；不能只靠肉眼判断 RGBA 完成度 |

左受击透明素材的成功是本地绿幕键、压缩幽灵清除、对齐与补边后的结果，不是原生透明生成成功。后续应沿用抠图流程文档，而非据此假定每个生成后端都支持透明。

## 6. 新敌人的复用检查清单

- 明确身份、持械侧、正面朝向和已认可 Idle，先确认动作语义。
- 为每个动作指定核心关节读点；方向受击首先写外力来源和上身被推动方向。
- 将 1011 的像素上限转换到新角色实际画布，不直接照抄 40–55px 等数值。
- 核对武器与头顶包络、衣物边距、脚底锚点；Death 与 Run 分别使用适当的锚定规则。
- 视频验收同时检查姿态因果、时序、外观连续性、越界、平台和闪帧。
- 关键帧按接触/峰值/回收或动作阶段选取，不等间隔抽帧代替判断。
- 透明处理后再检查边缘、包围盒、脚底与补边；素材验收之后才进入部署。
- Unity 接入单独核对 Clip 循环、键间隔、状态 speed、过渡和实际事件时间；保留素材/部署的独立状态。

## 7. 来源与流程链接

- 原始 Prompt 与任务索引：`memory/enemy1011-animation-prompt-archive.md`。
- 任务、版本与用户验收追加记录：`memory/video-generation-case-log.md`。
- 已有正面受击与五层 Prompt 经验：`memory/hit-animation-video-pipeline-lessons.md`。
- 1011 部署与后续任务：`plan/enemy1011-animation-handoff.md`。
- 视频与受击制作流程：`skill/workflows/character-hit-animation-video-workflow.md`。
- 图片、Alpha 与去背验收：`skill/workflows/image-asset-generation.md`。
- 视频 API 与参数：`skill/reachapi-seedance-video-generation-v2.md`。
- Sprite 画布与动作语义：`design/pixel-character-and-enemy-animation-spec.md`。
- 项目新画风：`design/art-style-guide.md`。

本次只整理 Memory；以上 Design 与 Skill 未修改。格式、接口与收费授权沿用其现行流程，不从案例总结反向改写流程规范。
