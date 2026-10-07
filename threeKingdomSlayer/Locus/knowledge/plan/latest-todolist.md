---
id: kd_65412ee0-2f14-4b9d-a598-afdce4337b9a
injectMode: inherit
summary: Y分叉最新待办：54株草摇曳获用户确认，接线与自动风动参数已回读；当前Battle/Y均dirty，保存重载收尾、Encounter接入和自然路线战斗回归待完成。
aiEditMode: inherit
---

## 当前场景搭建交接入口（2026-10-01）

- **2026-10-07 起本文件停用为待办总表**：新增 BUG 与新增需求登记到 `plan/bugs-and-requests.md`；进度以 `plan/october-milestone-plan.md` 为准。本文件仅保留历史记录，不再追加条目。
- 最新状态先读 `plan/curved-scroll-development-handoff.md` 第14节；工作流为 `memory/y-junction-scene-authoring-workflow.md`，错误复盘为 `memory/project-mistake-note.md`。本文件历史段落不是当前自动执行清单。
- 当前分支 `port/combat-layer-from-video-branch`（HEAD `07970e52`），场景搭建改动未commit/push；保持Battle宿主 + Additive Y路线层，不覆盖其他战斗层/Enemy/MoveSystem/HTML/Workspace未提交文件。
- 既有保存基线：三层候选地面、独立天空背景、54株草、建筑脚点及战斗节点镜像布景。草摇曳效果现已由用户确认实现；当前只读核对为54个 `Wind Mesh`、151项scenery、无空项/重复、三份材质自动风动（强度0.28、速度2.1、时间-1），Console error/warning=0。
- 当前现场覆盖旧dirty=false结论：Battle和Y均dirty=true；Y根位置(0,-1.8,0)、right=false、progress=0.260352、viewCamera=None。本轮仅更新文档，未保存、恢复预览或重载场景；草风动持久化收尾仍需确认，禁止Save All。
- **当前优先待办**：
  - [ ] 新会话先绑定编辑器预览相机；当前重开后 `viewCamera=None`，不可靠跨场景保存。不要为绑定而调用会丢Y未保存工作的重开菜单。
  - [x] 用户确认当前草摇曳效果已实现；保持54株及现有布局，不重新生成或重复部署。
  - [ ] 草风动保存/重载收尾：先区分用户编辑与临时预览，明确恢复值，只保存授权Y层和草资源；不保存Battle临时dirty状态。
  - [ ] 截图复查分支后段密度、近景草过亮/过大与草图切片缺陷，按实际Game微调。
  - [x] 两座瞭望塔、门楼、主帐篷按实际Alpha脚点、路线终点投影和真实Battle视野重新布置；保存重载及Play暂停截图已复看。旧重复 `CommandTent - beside road` 已禁用但保留，最终主观观感待用户验收。
  - [x] 复用已验收木栅/火盆/军旗，补齐右营地及开局战斗点的左右镜像组；正确裁切的小帐篷/箱子及复用道具位于 `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/Props/`。旧共享图集未修改。
  - [ ] 提高Encounter编辑可操作性：目前三份预览引用不驱动Runtime；Host的Opening/Left/Right依旧同为SmallBattle。
  - [ ] 在最新战斗移植基线上完成自然Left/Right战斗→奖励→旅行回归、暂停/退出/重入与10次生命周期检查。
- 暂不做：改移动采样/曲率、追加新道路Mesh、正式存档/合流/三分支。此前道路“空带”已证实是背景遮挡，持久道路Mesh只保留为未来有新证据时的备选。

## 敌人攻击预警：多攻击动作挂点（2026-10-07 已迁出）

该需求已迁至 `plan/bugs-and-requests.md` 的 `REQ-002`，本文件不再维护。

## 本周主要目标（新增）

### P0：Stab / Slash 高速运动表现（用户确认，待实现）
- [ ] **对象级方向性像素动态模糊 Shader（替代残影方案）**：仅为 Stab / 普通 Slash 的武器 SpriteRenderer 使用专用 BIRP Sprite Shader；沿瞬时速度方向对当前 Sprite 做 3–5 次离散 UV 采样，形成当前轮廓的方向性拖伸，而不是复制多个清晰残影。
  - Shader 保持透明 Sprite、SpriteAtlas、Renderer Color、翻转与原始 alpha；使用 Point 风格离散采样，不做高斯软化。采样偏移按纹理 texel 量化，使边缘保持街机像素硬度。
  - 参数建议：`_MotionDirectionUV`、`_MotionStrengthPixels`、`_MotionWeight`；用 `MaterialPropertyBlock` 每帧驱动，避免实例化材质和 GC。原材质/属性必须在 Complete、Kill、OnDestroy 统一恢复。
  - Stab：在 `StabSweepEffect` 的加速刺入阶段，根据 StabRay 当前/上一帧世界位置计算速度；投影到武器 Sprite 的局部/UV 方向，仅刺入和短穿入阶段启用，蓄势、回收与 Hit Stop 时衰减至 0。不得改变枪尖接触点、射线或命中时序。
  - Slash：仅普通 Slash 的 enhanced-motion 路径启用；根据逻辑根平移速度 + 旋转角速度估算屏幕切线方向和强度。需要避免把整根长枪沿错误 UV 轴糊成色块，强度设上限并在首击 Hit Stop 时固定当前值或快速收束。
  - 建议首版固定 5 taps，中心权重最高，尾向偏移多于前向偏移；最大拖伸控制在约 3–6 个源纹理像素。若移动设备开销明显，可退为 3 taps，但不切换回残影方案。
  - 不使用 `Assets/Shaders/BlurEffect.shader`：该 Shader 是屏幕/RenderTexture 高斯模糊，不支持 SpriteAtlas、每对象速度方向，也会产生软糊观感。
- [ ] **Slash 跟手斜率**：将输入手势的归一化斜率传入 `AttackSystem` / `SweepEffect`，在不改变 Slash 左右方向、X 阈值命中和范围的前提下，仅调整视觉路径的 `movementTilt` / 角度偏移。
  - 快速非蓄力 Slash 当前只传左右布尔值，应额外传递 `swipeDirection.y / abs(swipeDirection.x)` 或屏幕角度，并限制在约 `±15°`，使用死区和 Clamp，避免轻微手抖造成角度跳变。
  - 蓄力手势中的水平划动仍属于 Sweep；只有判定为 Slash 的斜划手势消费斜率。
  - 视觉根可倾斜，命中仍由既有逻辑根 X 穿越目标阈值驱动；不得使用旋转后 Sprite bounds 决定命中。
- 合理性：现有 Stab / Slash 已有独立视觉 Transform、DOTween OnUpdate 和阶段时序，能提供速度数据；对象级 Shader 不影响背景/UI，且比残影更准确表达“当前武器高速运动中的动态模糊”。

### 1. 音频系统改造
- [x] 将音效与背景音乐分离，支持分别调整音量。
- [x] 已升级为 AudioMixer 三档控制：总音量 / 背景音乐 / 音效；暂停菜单接入三个 Slider，并完成运行时路由、存档恢复与编译验证。
- [x] 修复 Mixer 快照首帧覆盖存档音量：延后一帧重应用；敌人受击音效播放倍率调整为 80%。
- 修改：`Assets/Audio/ThreeKingdomSlayerAudioMixer.mixer`、`Assets/Scripts/Managers/AudioManager.cs`、`Assets/Scripts/UI/PauseMenuUI.cs`、`Assets/Scenes/Battle.scene`。
- 待用户验收：暂停菜单三档音量独立调节、退出重进后的设置恢复。

### 2. 视觉与动效表现增强
- [-] 首批命中反馈已实现：统一命中来源/强度上下文；局部 Animator 卡肉；现有闪白、受击缩放与伤害数字按强度分级；DoT 不触发卡肉。
- [ ] Battle 实机验收：Stab、Slash、Pierce、Launch、Parry、海浪/旋风、共享血量与连续箭雨/火焰的反馈节奏。
- [ ] 命中特效（火花/斩痕等）暂缓，等待明确美术素材；接入前先在屏幕中心 Debug Target 验证可见性，再迁移到敌人命中位置。
- [ ] 卡肉调试日志当前开启：`[HitFeedback] Trigger/Freeze/Resume`；确认用户验收后再关闭或改为可配置开关。

## 已修复、待持续验收的 Bug

### [-] QTE 翻牌视觉与收尾时序
- 修复：翻牌改为固定 `0° ↔ 180°` 绝对目标并按 Tween 50% 时间切换正反面；`CompleteQTEAttack()` 增加一次性门闩，避免动画事件与时间兜底重复结束 QTE 活动。
- 待观察：Boss 在海浪/旋风等主动位移效果命中后进入 QTE；正常 QTE 成功与失败；QTE结束返回正面。确认不会出现正面遮挡QTE图案、技能栏无法交互或结束时异常补翻。
- 重点文件：`Assets/Scripts/UI/HeroHUDFlipCard.cs`、`Assets/Scripts/QTE/QTEController.cs`、`Assets/Scripts/QTE/QTEActivityHub.cs`。

### [-] 灼烧/染病状态条显示
- 已实现：普通敌人头顶状态条；Boss Poise 条下方同尺寸状态条；疾病紫条优先、灼烧红条紧贴下方；仅灼烧时自动上移；疾病层数文本；Boss QTE/转阶段暂停 DoT。
- 待观察：普通敌人与Boss的单DoT/双DoT显示、条长与层数位置、DoT持续期间血条保持显示、Boss暂停DoT时的进度冻结、对象池复用后显示是否清理。
- 重点文件：`Assets/Scripts/UI/EnemyHealthBar.cs`、`Assets/Scripts/UI/BossHealthUI.cs`、`Assets/Scripts/Core/UpgradeEffectManager.cs`、`Assets/Prefabs/UI/BossHealthBar.prefab`。

### [-] 补齐链并发卡死风险（降级观察）
- 历史现象：死亡、方向位移和攻击状态交叠后，部分列可能不再补齐。
- 当前状态：已修复攻击范围过滤合法 WaveMarch 订单；近期回归未复现。
- 待观察：死亡、击退/击飞回位、攻击动画与整排清空并发时的补齐；若复现，按日志核对 WaveMarch、击退回位与死亡释放的订单所有权。
- 重点文件：`Assets/Scripts/Core/Column.cs`、`Assets/Scripts/Core/ColumnManager.cs`、`Assets/Scripts/Enemy/Enemy.cs`。

## P0：当前严重 BUG（修复前必须重新核对现状）

### [-] BUG1：补齐链并发卡死风险（降级观察）
- 历史现象：死亡、方向位移和攻击状态交叠后，部分列可能不再补齐。
- 当前状态：本轮已修复“攻击范围过滤合法WaveMarch订单”问题；用户回归暂未复现补齐链卡死。
- 后续策略：降为观察项，不阻塞三选一优化；若复现，再按当时日志核对WaveMarch、击退回位与死亡释放的订单所有权。
- 重点文件：`Assets/Scripts/Core/Column.cs`、`Assets/Scripts/Core/ColumnManager.cs`、`Assets/Scripts/Enemy/Enemy.cs`。

### [x] BUG2：弃选 UI 排布、叠加表达与道具点击穿透（已验收）
- 已完成：弃选弹窗使用独立 Prefab；支持最多 5 个已持有道具 + 1 个新获得道具；列表从固定顶部 Y 向下紧密排列，新获得道具置顶。
- 已完成：第一次点击选中高亮，第二次点击同一卡确认；叠加道具显示持有数量并按稳定 `entryId` 整组丢弃。
- 已完成：标题静态放入 Prefab 并绑定项目中文字体；移除运行时创建标题和居中扩张布局。
- 已完成：为独立 `BuffDisplayPanel` Canvas 添加 `GraphicRaycaster`，修复道具点击穿透为 Stab。
- 验收：用户已确认修复通过。美术素材后续可独立替换，不影响当前结构与交互。
- 修改范围：`Assets/Prefabs/UI/ItemDiscardPopup.prefab`、`Assets/Scenes/Battle.scene`、`Assets/Scripts/UI/ItemDiscardPopup.cs`、`Assets/Scripts/Core/ItemInventory.cs`、`Assets/Scripts/Core/UpgradeEffectManager.cs`、`Assets/Scripts/Managers/EnemyManager.cs`。

## 本周目标（进行中）

### P0：战斗手感、QTE 与数值
- [x] **Stab 视觉射程与穿刺层级校准**：新增 `stabVisualReachOffset`（当前 0.5）与五列 `stabVisualStartXOffsets`（当前 [-1, -0.5, 0, 0.5, 1]）纯视觉校准；Stab 改为 Default / order 0，与敌人按世界 Z 产生穿刺遮挡。已验收。
- [x] **QTE 双版本规则**：
  - V1 保留原有战斗输入穿透规则；V2 Strict 在完整 QTE 生命周期内吞掉战斗输入，提前做出当前手势判失败，并冻结连击、锁定道具栏和大招。
  - 已修复提前输入后的索引回退卡死、对象池/数据切换清理、旧回调跨轮污染；三联 QTE 提前失败不会跳段，仍按原时序发射箭矢。
  - Boss 104 已配置 Strict；TripleStab 三个点击指示器统一使用正式 `QTE_Stab` 素材；用户已完成实机验收。
- [ ] **QTE 开始视觉预告**：老虎机/图案直接出现前增加可辨识的进入提示，与 QTE 状态和时机严格同步；后续结合 QTE 看板素材与音效验收。
- [ ] **数值平衡**：在不改基础招式、连击和击飞既定数值的前提下，基于现有三选一、道具、敌人波次倍率、Boss 两套已制作 QTE，实测并调整 20–30 分钟测试关的成长、压力曲线与 Build 成型节奏。

### 1. 角色与 Boss 演出补齐
- [ ] 将进度 UI 栏扩展为角色对话入口。
- [ ] 制作可双面展示的 3D 看板：正面显示进度节点，反面显示对话与 QTE。
- [ ] 对话触发时，看板以自身中心沿本地 X 轴翻转 180°；结束后恢复正面。
- [ ] 补齐角色演出与 Boss 演出，并在 Battle 实机流程验收。
- [ ] 整理底部 HUD、双面看板、道具栏与血条之间的显示层级，修复遮挡/排序问题。

### 1.1 阻塞式台词系统
- [ ] 配置资产：区分教学台词/对白台词；支持事件ID、逐句说话者（玩家/Boss）、点击推进、Boss头像/头像框与自动触发条件（开局、波次、Boss入场/阶段/死亡、击杀数）。
- [ ] 播放协调器：FIFO 队列；QTE、三选一、弃置等现有阻塞交互结束后播放；台词期间暗遮罩、暂停战斗与战斗输入。
- [ ] 教学进度：全局存档；触发后只标记“本局已播放待结算”，仅该关胜利时写入永久完成；失败/重开/退出均再次触发；不可跳过。
- [ ] 对白规则：本局每个事件仅一次；可用“跳过”按钮跳过整段事件。
- [ ] 背板布局：左侧玩家头像与框，Boss战显示右侧Boss头像与框；玩家独白占整块文本区，双方对话采用左上玩家/右下Boss对称文本区；QTE独占背板。
- [ ] 接线与验收：自动触发器、`DialogueManager.Trigger(eventId)` 手动教学入口、结算提交、暂停恢复与冲突队列完整实测。

### 2. 箭矢与飞射物表现优化
- [-] 敌方箭、Boss QTE 箭、定时箭雨与攻击计数箭雨已完成统一轨迹/朝向/清理改造。
- [ ] 在普通战斗、QTE 与 Boss 场景分别进行完整验收，避免影响现有伤害和时序逻辑。

### 3. Boss 设计、显示与难度
- [ ] 补全 Boss 设计内容及对应战斗演出。
- [ ] 优化 Boss 的 UI/场景显示。
- [ ] 调整 Boss 难度配置，并记录最终配置与实测结果。

### 4. 20–30 分钟测试关卡
- [ ] 制作一条目标时长 20–30 分钟的测试关卡。
- [ ] 按难度递进配置敌人、波次、Boss 与奖励节奏。
- [ ] 记录通关时长、玩家等级/技能成长、压力峰值和卡点，作为后续平衡依据。

### 5. “染色”敌人图片崩坏 Bug
- [x] 对象池复用时已显式恢复波次染色与材质绑定，修复白图/崩坏显示。
- [x] 已覆盖染色波、普通波与对象池复用路径；后续仅在新增材质/渲染改动后回归验证。

### 6. 数值配置总表（Editor）
- [ ] 制作 Unity EditorWindow「战斗数值总表」，集中查看和编辑敌人、技能等战斗数据。
- [ ] 首期：敌人页（Prefab）与技能页（AttackSkillConfig / UltimateSkillConfig），支持搜索、排序、单元格编辑、定位原资产。
- [ ] 复杂嵌套数据（攻击序列、Boss 阶段、QTE 槽位）仅提供“详情”打开原 Inspector，不做表格化批量编辑。

- [ ] **三选一升级 UI 重制与二次确认交互**：
  - 美术：以 `upgrade_choice_ui_concept_v1.png` 为方向，制作固定尺寸的深靛木铜框大底板、独立技能名外框、独立效果文本框、独立金色选中发光层；技能图标外框复用当前已有素材，不重制；不采用 9-slice。不得使用整张“内卡框”包住全部内容，避免文本与图标定位依赖识别图。三选一表达“战术奖励/Build 选择”，须与弃置 UI 的红蓝交换/警示语义明显区分。
  - 布局：大底板约 900×980；三组内容横排、间距约 28；每组由既有图标框 + 名称框 + 效果文本框组成，均以独立 RectTransform 精确部署；标题由 TMP 独立承载，不烙入素材。
  - 状态（未完成，当前不可验收）：现有拆分素材（底衬、名称框、效果框、金框）因透明边距、原图比例与布局关系不匹配，实际效果框面积与文本安全区不足，当前排版不可接受。后续必须先在独立 1080×1920 静态预览中，以真实中文名称/长说明验收“图标→名称→大效果文本区”的视觉比例与整体卡组居中，再接入运行时；禁止继续以硬编码坐标或运行时调整 RectTransform 叠补丁。
- [ ] **制作并替换池内技能图标**：当前需制作4张独立图标，均须符合 `design/skill-item-icon-art-guideline.md`：
  - 专注（缺失）：主动技能冷却缩减的专注/计时意象。
  - 拔苗助长（错误复用疾风）：攻击距离扩展且带伤害代价的武器延展意象。
  - 染病（错误复用主动冲击波）：紫色疾病/传染意象。
  - 主动冲击波（错误复用被动冲击波）：手动施放并为下一次蓄力攻击附加冲击波的“蓄力武器 + 预装能量”意象；被动冲击波保留现有 `icon_31_charge_shockwave.png`，两者不得共用。
  - 每张图导入、绑定前保留现有引用；替换后在三选一、主动栏/被动栏与冷却遮罩下验收。
- [ ] 战斗 UI 美术套件：双面看板（进度节点/路线/对话/QTE）、Boss 血条、道具栏槽框/角标、三选一与弃置卡牌、暂停/结算面板、连击/蓄力/击杀反馈；按“看板→Boss血条→道具栏→三选一→暂停结算→反馈”推进。

### 8. 道具流玩法（新增 — 当前焦点）

**设计决策**：
- 分池：3选1升级池 / 3选1道具池 / 击杀掉落池，三者独立
- 道具（消耗品）≠ 道具类奖励（数值升级如捡漏、谋略）
- 道具栏满时弹出弃置/替换 UI
- 局外数值暂缓

**实现步骤**：

- [x] **8.1 修复 ItemInventory 初始化**：ItemInventory 组件当前未挂载到任何场景，整个道具系统静默空转。将 ItemInventory 挂到 Battle.scene。
- [x] **8.2 道具栏满弃置 UI**：道具栏满时弹出面板让玩家选择丢弃/替换哪个道具，而非静默排除。
- [x] **8.3 击杀掉落框架**：Enemy 死亡时按基础概率判定（固定值，不随等级成长），从掉落道具池随机抽取，塞进 ItemInventory。
- [x] **8.4 掉落道具池配置**：新增 ScriptableObject（DropItemPoolConfig），管理击杀掉落道具表及各自权重。
- [x] **8.5 新消耗道具实现**：
  - 万箭齐发：呼叫弓箭支援，对 N 排敌人射出多波箭矢造成伤害
  - 火蛇机关：向前方喷出火焰，对 N 排敌人造成伤害
  - 虚幻武器：有持续时间，召唤幻影（文案复用但规则独立）
- [x] **8.6 新 3 选 1 升级（道具类奖励）**：
  - 捡漏：增加 X% 道具掉落概率（加入 UpgradePoolConfig）
  - 谋略：增加 X% 道具造成的伤害（加入 UpgradePoolConfig）
- [x] **8.6a ItemEffectRunner 伤害动态读取谋略**：万箭齐发/火蛇机关在激活时读取 UpgradeEffectManager.GetItemDamageBonus()，实时计算最终伤害。
- [x] **8.6b 虚幻武器实际效果**：激活后每 phantomInterval 秒对随机有敌人的列执行一次幻影 Stab 攻击，持续 phantomDuration。伤害受谋略加成。
- [ ] **8.7 道具栏 UI 显示与使用**：确认 BuffDisplayPanel 正确显示道具图标、点击使用正常。
- [ ] **8.8 道具流与现有流派协同测试**：连击位移聚怪+道具清场、蓄力间隔+道具补输出。

### BattleHUD Scale 归一化与布局修复（分批验收）
- [x] **首批低风险 UI**：Combo 静态/填充图、Charge SpinImage、PauseButton、Defeat Text 已将视觉 Scale 等价烘焙进 RectTransform，层级与美术素材不变；修改前后屏幕四角一致。
- [x] **连击文字与数字**：`ComboDisplayUI._referenceGap` 从 41 调整为 8；DigitParent `Scale 1.2 → 1`，DigitSlot 真实尺寸/布局间距已等价转换。曾出现数字 Y 偏移，已恢复 Y=639 并验收。
- [x] **CoinCounter**：根节点 `Scale 0.5 → 1`；图标、文本、浮字锚点和浮字参数已同步等价转换，用户已验收。
- [x] **BuffDisplayPanel ColumnB**：四个道具槽 `Scale 1.2 → 1`；V3 隐藏、V1/V2 左侧栏显示与点击已验收。
- [x] **旧技能图标**：六个 inactive 技能图标 `Scale 1.5 → 1`；内部冷却 Fill 覆盖范围已等价转换并验收。
- [x] **HeroHUD 血条组**：Background_bottom、Fill Area、Fill、Background_frame、Handle 已归一化；血量 0/50/100%、护盾与头像关联验收通过。
- [x] **HeroHUD ExpBar**：`Scale 2.22 → 1`；经验 Fill、等级文字与经验宝石飞行位置已验收。
- [x] **HudCard 容器**：Prefab 与场景实例 `Scale 1.1 → 1`；正反面、V1/V2、V3、QTE 翻面均已验收。
- [ ] **保留项（非批量修复对象）**：Health 血量文字保留非等比 `Scale (1, 0.833, 0.833)`，用于维持字形压缩观感；CanvasScaler 运行缩放、BossTail 的 X=-1 镜像、SpriteNumberDisplay 的动态 Scale=0 均为功能性状态，不调整。
- [ ] **后续仅按需求处理**：独立检查 Charge Fill / 场景中非 HUD 的非单位 Scale；不可按本轮规则批量归一化。

### 后续新增待办
- [x] **制作 QTE 成功与失败符号**：已接入 `QTE_SUCCESS.png` 与 `QTE_FAIL.png`；每个 slot 结算即时显示于图案右侧，0.42 秒自动消失，不阻塞下一段输入或时序。
- [ ] **添加连击数加成**：设计连击数对应的收益类型、成长曲线、断连规则及 UI 表达后实施。
- [ ] **道具流平衡**：围绕掉落率、持有上限、道具伤害、谋略加成、使用频率及与其他流派协同进行实机调优。

## V2 三选一优化（当前焦点）

### 1. 获得与升级信息表达
- [ ] 选项卡增加结果提示：未持有时显示“新获得”；已持有且可升级时显示“Lv.当前等级 → Lv.下一等级”；满级状态显示“Lv.Max”。
- [ ] 结果提示位于效果文本下方并居中；同步调整选项卡框高度与对应美术安全区，由美术修整后再精确接入。
- [ ] 在效果文本下方居中展示“被动”或“主动”分类美术图案；主动技能暂复用底部主动技能栏同款外框。
- [ ] 修正技能名称相对名称框未居中的问题；以名称框自身中心为准，不依赖卡片整体坐标。

### 2. 技能等级与稀有度重分级
- [ ] 以 V2 主动技能和永久升级为基准，逐项审视最大等级、单级提升、稀有度与候选权重。
- [ ] 不适合 10 级长线成长的技能应缩短等级段，并提高单级改变幅度与稀有度；例如将原本平缓的 3→6→10 类关键节点改为更少但更明显的阶段性成长。
- [ ] 分级调整必须同时更新效果描述、池归属、权重与 30 分钟关成长曲线，避免只改 maxLevel 导致候选耗尽或数值断层。

### 3. V2 与 30 分钟测试关联调
- [ ] 在完成信息表达和分级方案后，基于 V2 三选一/主动技能配置重调 20–30 分钟关的升级频率、稀有奖励时机、敌人压力与 Boss 奖励节奏。
- [ ] 记录每局获得技能数、各技能等级、关键升级时点、Boss奖励与通关/失败压力点，作为数值调整依据。

## 本周完成记录
- [x] **V2 主动技能基础与冲击波**：已建立 ActiveSkillDefinition / Inventory / Runner / Pool 结构，Battle 使用 V2 主动技能规则。火龙舌、被动蛇形烈焰喷射与主动冲击波已可用；主动冲击波点击后可积攒层数，并在下一次蓄力穿刺/横扫时逐层释放。当前为高频测试值：Rare 60%、池内权重1000、CD 2/1.8/1.6/1.4/1.2 秒、伤害 3/4/5/6/7。
- [x] **火系验收修正**：被动烈焰喷射改为固定五列、三排的蛇形火焰效果；灼烧跳红字且不打断敌方攻击/受击动画；火龙舌与烈焰喷射图标已对调并完成验收。
- [-] **V2 技能完整化**：主动海浪已实现并进入 Rare 池高频测试（权重2000，CD 2/1.8/1.6 秒，Lv1-3 覆盖前1/2/3排、伤害均为2、固定击退1排）；单次海浪已保证同一敌人只命中/位移一次，并隔离并发施放的追踪状态。待实现/转换：虚幻武器主动版；已有但未进入正确池：疾风、智慧、铁壁、地刺、延长、波长、主动箭雨、主动旋风。需清理：被动旋风、主动烈焰喷射。完成技能池整理后再进入30分钟数值阶段。

## 历史待办

## P0：TestStage 最终 BOSS 无法补齐交战
- [x] Boss `Approaching` 的前两排阻塞判断改为扫描存活敌人的实际 `rowIndex <= 1`，不再误用 `Column.enemies` 列表下标。
- [x] Battle/TestStage 实机验收完成：清空前两排后，row=2 的 Boss 能恢复推进至 row=1 并进入战斗；本轮未再遇到波次卡死。
- 修改：`Assets/Scripts/Enemy/Enemy.cs`。

## P1：Enemy_105 箭矢落点与预警可见性
- [x] 普通远程箭保留原 Z 落点，新增世界 X 落点中心和随机半宽；QTE 箭雨保持独立规则。
- [x] `Enemy_105` 已配置落点中心 0、半宽 0.75。
- [x] Battle/TestStage 实机验收完成：col=0/4 与 row=2 的箭能斜向进入镜头中心区域，伤害时序正常；本轮未发现新的卡死。
- 修改：`Assets/Scripts/Enemy/Enemy.cs`、`Assets/Resources/EnemyPrefabs/Enemy_105.prefab`。

## P2：敌人生成、可视窗口与对象池优化
- [x] 对象池预热改为按 enemyId 去重，每类仅预热一次，消除每个配置出现都额外创建 `defaultPoolSize` 个对象的问题。
- [ ] 保持当前波次内敌人全量实例化，实测并记录优化前后初始对象数、内存与后续波次表现。
- [ ] 第二阶段“逻辑槽位 + 可视化窗口”需单独评审：后方敌人保留逻辑占位、只物化前 5 排及后备行；不得将未物化槽位误判为空位。
- [ ] 第二阶段须覆盖列阵补齐、Boss 等待、共享血量、全体伤害大招和波次完成，这些当前依赖活跃 Enemy ��象。
- 修改：`Assets/Scripts/Managers/StageController.cs`。

## 延后：铁壁·震荡图标
- [ ] 图标替换暂停，保留当前占位资源；后续统一处理技能图标美术和导入规格。

### 假移动路线（当前进度）
- [x] 新建纯逻辑 FakeRoute 运行层，Battle.scene 作为唯一战斗场景，不加载路线 Unity Scene。
- [x] 节点战斗垂直切片：节点配置、BattleEntry、路线选择、占位假移动、目标节点战斗和终点结算。
- [x] 测试拓扑 `A → B/C → D`，D 为唯一终点；B/C 汇入 D 已实际验收。
- [x] A/B/C/D 使用不同普通敌人阵列，已验证节点战斗配置随节点切换生效。
- [x] B/C 存档点和 FakeRoute 独立快照。
- [x] 快照恢复已验收：节点/BattleEntry、路线选择历史、玩家状态、被动/主动技能、UT 能量、击杀数和局内铜钱均正确恢复。
- [x] 主动技能冷却、普通攻击冷却、计时被动计时、敌人、投射物、连击、QTE、临时效果和占位动画进度不保存，恢复时按规则重置。
- [x] 快照架构、路线、关卡和配置版本校验；旧 V2 快照不会静默混用。
- [x] MainMenu 继续游戏按当前未完成路线关卡启动，并从该关卡 startNode 开始，不读取失败恢复快照。
- [x] 节点战斗结束后直接显示当前节点路线选择 UI，并保持当前战斗背景及正在播放的图片、视频和音频；不切换旧的 `routeChoiceBackground`，不播放旧的 `routeChoiceTransition`。
- [x] 玩家选择路线后播放所选 `choice.presentation` 位移过场，表现完成后提交目标节点并进入其战斗。
- [x] `FakeStage01` 已加入纯非战斗分叉节点 `Junction`，并通过 B 存档点支持节点提交后的快照保存与失败恢复验收。
- [x] 统一 FakeRouteNodeConfig：不拆分 CombatNode/JunctionNode 配置资产；节点通过 battleEntries、outgoingChoices 和终点属性组合表达行为。
  - 每次进入节点最多挑战一个未完成 BattleEntry；完成后离开当前节点，不自动开始下一条。
  - 再次进入节点时跳过已完成 BattleEntry，继续挑战第一个未完成条目。
  - 所有 BattleEntry 完成或 battleEntries 为空后进入统一出口流程。
  - 任意节点允许零、一个或多个出口；单出口也必须点击确认，多出口显示路线选择。
  - 空 battleEntries 的节点自然承担 Junction/非战斗节点语义，不建立独立 JunctionNode 运行器。
  - 终点节点无出口，唯一 BattleEntry 完成后直接进入终点结算。
- [x] 统一节点模型验收：已验证 Combat → Combat、Combat 多出口、空战斗节点分支及复合节点拓扑；已验证 Entry 不自动连续推进、不重复战斗和奖励；表现完成后才提交目标节点、存档和启动战斗。
- [x] 单出口节点交互验收：战斗或非战斗节点内容完成后进入 `ChoosingRoute`，单出口也必须点击确认，不会自动移动到目标节点；终点节点除外。
- [ ] 条件系统及其快照状态。
- [ ] 剧情/剧情选项数据和快照状态。
- [ ] 更复杂的节点阶段和重访规则。
- [ ] 评估清理旧 Route/RouteV2 脚本、旧路线场景和旧校验工具。
- [ ] 计时被动状态机重构：获得、待 Combat 首次触发、效果成功、冷却和失败重试状态分离。

### 连续2.5D卷轴场景重构（当前任务）

#### 当前已验收基线（2026-10-02）
- 用户已确认当前80单位节点间距、3秒移动时长的高速转场观感良好，作为后续卷轴化场景调参基线。
- 路线采样：`YScrollSample.Length=160`；开场节点到战斗节点距离=`80`；战斗节点到分支节点距离=`80`；左右分支等长。
- 移动时序：`BattleYRouteHost.openingDuration=3`、`branchDuration=3`；当前等距80单位版本的观感已由用户实机确认；此前2.994秒/2.997秒工具采样仍使用旧414.823长度，不能作为80单位版本的计时证明。
- 当前路线采样参数：`junction=83.12884`、`radius=28.1595573`、45°时尾段约`54.75470`，转角保持`45°`；`Length`当前固定为`160`用于等距节点测试。
- 当前旅行曲线由用户调过并保留，关键点为：`(0,0)`、`(0.241670221,0.437265038)`、`(0.4583298,0.770281851)`、`(0.675,0.911040068)`、`(1,1)`。曲线表达“前段快速推进、后段长减速、终点精准停靠”；后续调整必须先保留此基线副本。
- 地面Shader同步使用当前路线几何：直路中心延伸到`83.12884`，分叉半径`28.1595573`，出口延伸长度`600`；道路/地形覆盖已按当前测试路线保存。
- 当前景观引用数量=`633`，无空引用/重复引用；这些对象仍是高速移动的临时重复景观测试集，正式拓扑主题化部署待后续完成。
- 回读保存状态：`Assets/Experiments/CurvedScroll/YJunctionSample.unity`与`Assets/Scenes/Battle.scene`均`dirty=false`；Y根节点恢复`(0,0,0)`，`progress=0.5`、`right=true`、`viewCamera=None`。

#### 本轮用户目标：高速、长距离、剧情化转场
- [ ] **路线长度扩展为当前约3倍**：当前 Y 样例 `YScrollSample.Length` 约69.1世界单位；目标规格约207世界单位，最终数值以路线几何、道路覆盖和镜头可见范围实测为准。
  - 不得只修改 `Length` 返回值；直路段、转弯半径、分支尾段、地面道路覆盖、侧景布局、远景背景和终点入口必须同步扩展。
  - 不修改 `YScrollSample.Evaluate()` 的既有坐标语义，除非先建立新的可配置路线参数并完成采样/战斗回归；当前 `junction=20`、`radius=18`、尾段35是受保护的既有行为基线。
  - 三倍长度的目标是让节点之间有足够的“移动叙事空间”，不是把同一批树石稀疏拉开。
- [ ] **移动时间统一缩短为3秒**：开局移动、分支移动和后续正式路线出口转场均以3秒为目标；不要通过降低美术数量或跳过环境段来实现。
  - 当前 `BattleYRouteHost.openingDuration=8`、`branchDuration=8`，需要迁移为配置化的3秒演出时长；`YScrollSample.duration` 仅保留为孤立样例预览参数，不能成为正式路线唯一时序来源。
  - 速度提升后必须验证近景掠过速度、对象插值、道路投影、目标节点抵达和战斗启动没有错位；禁止让战斗对象、Battle Camera、Player 或敌人跟随道路移动。
- [ ] **转场必须有明显的高速移动感**：通过近景大物体快速掠过、中景连续替换/渐变、远景缓慢变化、道路曲率和镜头轻微惯性共同表达，不依赖单纯提高 progress。
- [ ] **概念先行**：每条正式拓扑边在部署前先制作一张可评审概念图/构图草图，至少包含道路方向、中央安全区、近中远景层、节点语义和转场起止画面；概念图未确认前不批量部署场景对象。
- [ ] **素材生产闭环**：概念图 → 拆分背景/地面/侧景/前景/事件道具 → 导入并验证透明边界与脚点 → 按距离和层级部署 → Game Camera截图 → 3秒高速播放验收。
- [ ] **每条边必须有独立环境主题，不得复用同一套树石填满全程**：至少区分黄土战场、战后余烬、官道辎重、燃烧村落、山脚、山道、曹军营垒、桥头等状态。

#### 正式拓扑对应的转场内容清单
- [ ] **N1 当阳乱军 → E0 战后余烬**：开阔黄土战场逐渐进入战后烟尘；残旗、断枪、翻倒木车、散落盾牌、火星和撤退人流从近景快速掠过；远景保留低矮山脉/残墙。
- [ ] **E0 → E1 残军集结**：战场边缘出现聚拢的残兵、辎车和伤兵；用短暂开阔区和烟尘降低速度压迫，传达“重新整队”而不是瞬移换图。
- [ ] **E1 → J1 官道/村落选择**：分岔前必须形成两种可读环境：官道一侧有辎重、宽路和曹军远旗；村落一侧有燃烧民居、破篱笆、倒墙和火光。中央出口保持开放。
- [ ] **J1 → N2 官道断后**：进入较宽的官道；两侧部署辎车、木箱、低矮断墙、烽火和远景曹军旗阵；不侵入五列五排战斗空间。
- [ ] **J1 → EV/村落外围 → N3 村落搜救**：实际转入村落侧翼；近景按篱笆、破门、民居边角、火盆、泥路和求救痕迹排列；右侧断石墙作为空间锚点，避免硬切。
- [ ] **N2 → E2 辎重受阻 → E3 陷车改道**：表现陷泥车轮、散落粮袋、民夫推车和断裂车轴；道路环境由官道逐步过渡到山脚，不直接换背景。
- [ ] **E3 → E4 山脚急行 → N4 山道救援**：山体/岩壁从远景进入左右中景，色调由黄土转灰蓝；近景树干、岩石和断箭高速掠过；中央保持开阔谷地。
- [ ] **N3 → E5 安置幸存者 → E6 民居线索 → E7 斥候急报**：村落密度逐渐下降，转为村外空地和树林；保留火光、幸存者聚集点、指向山道的残墙/路标，表达情报推进。
- [ ] **J2 → E8 战痕引路 → E9 山隘传令 → N4**：马蹄印、血迹、断箭、敌军残骸和赵云撤离痕迹成为连续线索；山壁作为两侧远景，不压入中央战斗区。
- [ ] **J2 → N5 曹军合围**：环境转入曹军侧翼军阵；远景营垒、暗红军旗、火把、战鼓台和密集军阵横向铺开；近景旗杆必须位于道路边缘，不插入道路中心。
- [ ] **N4/N5 → E11 撤军讯号 → E12 奔赴桥头 → N6 长坂桥断后**：出现远处撤军旗语、桥头烟尘和撤退人流；道路逐步转换为桥面，桥栏沿左右边缘，水面只在两侧窄条露出，最后进入长坂桥终局战。

#### 每条转场边的制作交付物
- [ ] 边配置表：`sourceNode`、`targetNode`、出口方向、演出时长（3秒）、目标环境段、起止画面、音乐/环境声、是否允许跳过。
- [ ] 概念图：起点构图、0.5秒高速状态、中段环境变化、2.5秒目标入口、3秒抵达帧；标出中央安全通道和近中远景层。
- [ ] 背景素材：远景山体/营垒/村落/桥头等，保证可平铺或可按距离渐变。
- [ ] 地面素材：道路、泥地、山道、桥面和水面边缘，保证路线长度扩展后无空白接缝。
- [ ] 中景素材：树群、岩壁、篱笆、营垒、辎车、民居、断墙等，按左右手工布局。
- [ ] 近景素材：树干、旗杆、碎石、木箱、火盆、断箭、车轮等，用于高速掠过和速度参照。
- [ ] 事件素材：撤退人流、尘烟、火星、旗语、马蹄/血迹线索、远处赵云/阿斗剪影等；不与战斗对象绑定。
- [ ] 部署清单：每个对象记录起始距离、结束距离、横向偏移、缩放、排序层、翻转、透明淡入淡出和是否循环。
- [ ] Game截图与播放录像：至少验证起点、1秒、中段、2秒、终点五个采样帧；确认快速移动不是“整屏瞬换”。

#### 关键保护边界
- [ ] 不移动 Player、Enemy、Battle Camera、StageController、QTE、投射物和战斗阵型。
- [ ] 不用道路障碍物解释战斗区缩窄；所有环境阻挡只放在侧边或远景。
- [ ] 不通过节点瞬间 `ApplyProfile` 切换整套场景；视觉环境必须由 Travel/World Sequence 按距离连续推进。
- [ ] 不打开、保存或部署 `Assets/Experiments/CurvedScroll/FakeRouteDataTrial.unity`。
- [ ] 不把新转场美术一次性堆进 `YJunctionSample.unity`；正式拓扑扩展后应迁移到可编辑的 World Sequence/Travel Presentation 数据。

#### 核心理解
- 卷轴场景是一条连续的、按 presentation distance 推进的 2.5D 面片化环境，不是按节点切换 Unity Scene 或瞬时替换背景。
- 节点只负责战斗、对话、奖励和路线出口；节点进入不得直接 ApplyProfile 改变场景。
- 旅行连接负责距离、固定 3 秒演出和目标环境序列；玩家在移动过程中实时看到山谷、营地入口、营地内部等环境渐变。
- 战斗坐标、敌人阵型、攻击范围、QTE 和投射物目标始终独立于卷轴视觉宽度和环境材质。
- 这是 2.5D 场景搭建：背景、地面、侧景、前景和过渡面片通过距离、层级、缩放、遮挡和滚动形成空间感。

#### 需求清单
- [ ] 建立 `ScrollWorldSequence` / 环境区段数据：每段有起始距离、长度、Profile 和过渡参数。
- [ ] 将视觉目标从 Node 移到 Travel/World Sequence，移除 `Enter(node) -> ApplyProfile` 硬切换。
- [ ] 将 `ScrollRouteVisualProfile` 从随机 Sprite 数组扩展为可序列化手工布局：位置、距离、缩放、排序层、翻转和是否循环。
- [ ] 重做山谷侧景：大型山体/岩壁/树群固定左右布局，放大并形成包围感，中央保留战斗区。
- [ ] 重做营地入口/内部：帐篷、栅栏、火盆、箱子等按手工布局布置，禁止按数组索引随机交替排列。
- [ ] 营地背景只承担远景；营地地面单独使用地面纹理；前景装饰不替代背景职责。
- [ ] 实现旅行中的连续过渡：背景/地面/道路宽度/侧景/前景按距离渐变或分段进入退出，而非节点到达瞬切。
- [ ] 实现整关卡卷轴预览：路线全貌、节点标记、环境区段边界、距离滑块和自动播放；不启动战斗。

#### 测试计划
- [ ] 静态资产验证：Profile、环境区段、Travel 引用完整；没有节点视觉硬绑定作为唯一来源。
- [ ] 编译验证：代码编译通过，无 MissingReference、数组越界和空 Profile 中断。
- [ ] 连续距离验证：拖动/自动推进 distance 时环境连续变化，不出现瞬时全屏替换。
- [ ] 布局视觉验证：山谷两侧形成大型包围；营地物件有明确左右/远近关系；中央战斗区无遮挡。
- [ ] 场景职责验证：背景、地面、侧景、前景分层正确；营地背景不是地面纹理。
- [ ] 战斗隔离验证：道路宽度和环境切换不改变玩家/敌人世界坐标、阵型、攻击命中和 QTE。
- [ ] 路线流程验证：A→B/C→D完整战斗/奖励仍待回归；旅行读取资产duration（当前8秒），节点距离20/75/145，预览不得推进节点。
- [ ] 整关预览验证：能查看完整路线卷轴、节点和区段边界，并可回到正常战斗流程。

#### 最新问题交接
- [x] BUG-Scroll-01：用户已验收首次前进不再突然闪现。后续平铺场景重构的整段旅行仍需视觉回归。
- [ ] BUG-Scroll-02：敌人受击时整个场景会震动；需隔离 `CameraFeedbackController` 的全局相机反馈与 `worldBackground` 视差位移，确认是否应限制为局部受击反馈。
- [ ] 当前未验收：连续环境实际过渡、山谷大型侧景、营地固定布局、营地地面材质、F8整关预览、重复Play Mode的DOTween错误清理。
- [ ] 当前不能宣称卷轴场景连续过渡已完成；分段映射和编译通过不等于视觉跳跃已修复。
- [ ] 详细交接入口：`Locus/knowledge/plan/curved-scroll-development-handoff.md`第12节“可编辑场景重构”。第11节保留历史证据，不代表全部最新状态。

#### 可编辑卷轴场景（2026-09-26，取代F8/临时观察窗口）
- [x] 真实布局位于 `Scroll World - Editable Layout/Layout - edit objects here`，分山谷/入口/内部，148个可保存的侧景、地面和背景组件。
- [x] Scene整段平铺可移动/缩放/复制/删除；专用材质只在Game相机渲染时做距离偏移与曲率投影，不移动作者Transform。
- [x] 根节点ScrollWorldAuthoring Inspector提供Game预览距离滑条0–175和聚焦整段Scene按钮；旧F8关闭，旧预览菜单转向可编辑根节点。
- [x] 绑定旅行演出，保存重开保留148对象；Play从0开始，不继承编辑滑条；退出后恢复编辑距离，没有重复临时树石。
- [x] 验证真实物件移动改变Game画面；距离滑条和旅行演出不改布局Transform；20→75与75→145隔离演出各8秒。
- [ ] 用户验收实际拖动物件、场景构图和Game显示的一致性。
#### 真实道路分支重构（方案待实现，2026-09-26）

目标：将当前单一 Z 轴卷轴改为可编辑的真实道路连接系统。玩家选择左/直/右路线后，环境沿对应道路路径连续平移和旋转；玩家、敌人、阵型、攻击、QTE 和投射物继续保持战斗坐标独立。

设计边界：
- 方向角由全局规则统一配置，不按出口数组顺序推断；默认 `Left=-45°`、`Forward=0°`、`Right=+45°`，角度可在一个路线世界规则组件中统一填写调整。所有节点的同名方向读取同一组角度，不允许某个连接单独改成30°或25°。
- 首个样例采用现有 `A_Start → B_Left/C_Right → D_End`；B/C 最终在 D 前合流。
- 每条 `FakeRouteChoiceConfig` 绑定一个真实连接：方向类型、路径点、道路宽度、路肩、侧景、入口/合流点和目标节点朝向。连接不保存私有转角字段。
- Scene 中保存真实 `Connections` 层级；路径点、道路网格、树石、旗帜和背景都可直接编辑。
- Game 预览使用“连接选择 + 路径进度”而不是全局距离；Scene 保持整条路线平铺。
- 分支入口保留未选道路从侧后方离开，选中道路连续转入屏幕中心；合流点必须位置和朝向同时匹配，禁止瞬切。
- 不移动战斗对象；到达节点后锁定道路朝向，再开始节点战斗。

技术方案：
- 新增 `ScrollWorldRouteRules`，保存全局 `leftAngle`、`forwardAngle`、`rightAngle`、默认转弯半径、默认道路宽度和合流容差。
- 新增 `ScrollRouteConnection`，保存 source/target/choice、Direction enum、path points、道路/路肩/装饰引用和起止朝向约束，不保存连接私有 angle。
- 路径采样按弧长输出位置和切线；路径末端朝向必须等于起点朝向加全局方向角。
- 同方向连接可以有不同路径长度，但最终朝向必须一致；验证器应检查并报告偏差。
- `ScrollWorldAuthoring` 预览选择“连接 + progress”；编辑器中仍显示完整平铺路线。
- `ScrollNodeFlowTrial` 只在旅行阶段读取连接；旅行完成后锁定目标朝向，再进入战斗。

Phase 1 交付边界（只做数据和编辑器结构，不接入运行时转向）：
- [ ] 新增全局路线角度规则组件，Inspector 可统一调整 Left/Forward/Right。
- [ ] 新增 Direction enum 和连接数据，不添加连接私有角度字段。
- [ ] 建立 A→B、A→C、B→D、C→D 的真实 Hierarchy 连接层级和路径点。
- [ ] Scene 绘制路径线、方向箭头、连接名、节点/战斗标记和目标朝向。
- [ ] 校验同方向统一角度、路径起终点、合流位置/朝向和目标引用。
- [ ] 暂不改变现有战斗流程和 Travel 运行时。

新增测试用例：
- [ ] 修改全局 Left 角度后，所有 Left 连接同步更新；不存在单连接覆盖值。
- [ ] 修改 Forward/Right 角度后，所有同方向连接同步更新。
- [ ] 三叉路方向映射：Left/Forward/Right 使用全局三组角度。
- [ ] 两出口和单出口只减少连接数量，不改变全局方向规则。
- [ ] 同方向不同连接长度可不同，但最终朝向必须相同。
- [ ] 合流 B→D/C→D 位置与朝向误差在容差内才通过校验。
- [ ] 移动路径点、调整道路宽度和装饰后保存重开，连接引用和全局规则保持。
- [ ] Phase 1 预览不推进路线状态、不启动战斗、不生成临时对象。

#### Phase3 回退后的真实分支道路重构方案（重新设计，未实现）

上一版 Phase3 的全局 Shader 投影已回退，原因：`previewConnectionInGame` 在 Play Mode 接管了旧距离卷轴；全局 shader 参数作用于所有对象，且 Phase2 临时道路材质产生白色面片。后续不得沿用“全局 shader 直接投影全部场景”的方案。

推荐安全架构：
- 保留当前 `Scroll World - Editable Layout` 的真实平铺对象和旧直线卷轴作为基线。
- 每条连接的真实物件放在独立 `ConnectionContent` 父节点下：道路、路肩、背景、树石和旗帜仍是 Hierarchy 可编辑对象。
- 新增 `ScrollRoutePresentationRig`，只在指定 Game Camera 的 `onPreCull` 阶段临时计算连接采样位置/朝向，并移动该连接的 PresentationRoot；`onPostRender` 立即恢复原 Transform。Scene 窗口和作者布局始终保持平铺，不使用全局 shader。
- 编辑器 Game 预览和 Play Mode 使用同一套 PresentationRig；编辑器滑条只改变临时显示状态，不标脏场景。
- 未选择的分支内容不删除、不重建；由连接内容的显隐/渲染层控制。道路材质必须是明确的可见材质，不能使用未配置的白色临时材质。
- 玩家、敌人、QTE、投射物和战斗根节点不属于 PresentationRoot，永远不随道路转向。
- 旧直线卷轴继续作为无连接/回退路径；连接未绑定或校验失败时不接管运行时，保持旧功能。

路径坐标约定：
- Connection path points 使用世界平铺坐标。
- 采样返回 `position` 和 `tangent`。
- PresentationRoot 的临时姿态为采样点的逆平移/逆旋转，使选中道路切线对齐 Game 前方。
- 到达节点时锁定连接末端姿态；战斗开始后不再更新 PresentationRoot。
- B/C→D 合流必须由两个连接共享同一 merge marker 和目标朝向；误差超规则容差时连接不允许运行。

重新分阶段：
- [ ] Phase3A：仅实现 PathSampler + PresentationRig + 编辑器 Game 预览；旧 Play 路线默认关闭 Rig。
- [ ] Phase3B：给每条连接绑定真实 ContentRoot，验证道路、背景和侧景随父节点整体转向。
- [ ] Phase3C：验证编辑器预览和 Play 的 Transform 恢复，不污染 Scene，不改变战斗坐标。
- [ ] Phase4：在完整通过 Phase3A-C 后，才把 `ScrollNodeFlowTrial.Choose()` 接入连接旅行。

必须先通过的回归：
- [ ] 未选择连接时，原有距离20→75→145移动与此前一致。
- [ ] 关闭/删除连接道路对象不影响旧卷轴显示。
- [ ] 编辑器预览关闭时，Game 与旧版本一致。
- [ ] 编辑器预览打开时，只有 Game Camera 临时转向，Scene 平铺 Transform 不变。
- [ ] Play 进入 A 战斗仍为距离20、敌人生成正常、旧 Lab 不重复生成。
- [ ] 连接转向只改变环境，不改变 Player/Enemy/QTE/Projectile 世界坐标。
- [ ] 进入/退出 Play 和编辑器预览10次，无白色面片、重复道路、残留 Transform、材质污染或 Console 错误。
- [ ] 所有连接道路材质明确引用有效资产；未配置材质的道路默认禁用，而不是显示白色 Mesh。

## 13. 分叉卷轴正式游戏接入（未完成，下一阶段最高优先级）

### 用户确认的最终目标
- 分叉卷轴必须真正接入现有 Battle 游戏流程，不是独立视觉样例或临时 Additive 验证器。
- 当前案例只保留两条分支：Left / Right；暂不开发三分支。
- 必须保留 Edit Mode 的 Scene 平铺观察、Game 窗口预览、Progress/路径预览和节点位置可调整能力。
- Scene 中调整节点、路径点、战斗锚点和场景物件后，Game 预览与 Play 使用同一套保存数据。
- 玩家选择 Left/Right 后，游戏内实际沿选中道路移动/转向；抵达后停下并进入真实 Battle 战斗。
- 玩家/敌人/QTE/投射物战斗坐标不随视觉道路旋转；战斗地面高度必须正确，不能出现人物下沉。

### 当前事实与失败边界
- Y样例的视觉转向已单独验收，但它不是正式路线系统。
- `BattleYRouteHost` 曾验证 Battle.scene Additive加载 Y层和 `StartRouteBattle()`，但不等于正式路线选择/奖励/节点流程已接入。
- 之前没有完成正式 Left/Right UI选择→旅行→抵达→战斗→奖励的完整实机测试，不能再把样例测试当作游戏验收。
- `YJunctionSample`与 Battle.scene 当前是两套坐标/地面体系，玩家下沉说明没有完成视觉层与战斗层的高度/相机对齐。
- 未完成前不得扩展三分支、正式存档或更多素材。

### 目标运行架构
```text
Battle.scene（唯一战斗宿主）
├─ Player / Enemy / Manager / StageController / Camera / UI
├─ RouteBranchRuntime（正式路线流程所有者）
└─ Additive route visual layer
   ├─ RouteWorldAuthoring（真实可编辑节点/路径/场景物件）
   ├─ Left connection content
   └─ Right connection content
```

- `RouteBranchRuntime`负责正式路线状态、选择UI、旅行token、暂停/恢复、抵达回调和节点战斗提交。
- 视觉层只负责选中连接的路径投影和场景表现，不移动 Battle.scene 的玩家、敌人和战斗管理器。
- `Battle.scene`进入 Play 后加载路线层；Edit Mode 使用同一条路线层以 Additive 方式打开，Scene 和 Game 都可观察。
- 运行时未加载路线层或连接校验失败时，安全回退旧直线卷轴，不影响原有 Battle。
- 连接方向只使用全局 Left/Right 角度规则，不允许单连接私有角度。

### 编辑器与预览要求
- `Tools/Curved Scroll/Open Battle Host`：打开 Battle.scene 并 Additive加载路线层，绑定 Battle Main Camera；Scene中同时可见 Battle宿主和完整平铺路线。
- 根节点 Inspector 提供：当前连接、Left/Right、Progress、自动播放、显示路径、显示敌人生成标记、显示 Battle Anchor。
- Scene 中可直接移动：节点锚点、路径点、分支入口、合流点、战斗锚点、道路/背景/树石/营地物件。
- Game 窗口使用 Battle Main Camera 显示当前 Progress 的实际转向结果；预览不得启动战斗、奖励、visited 或保存。
- 编辑器预览关闭时，Game必须恢复旧直线卷轴显示；关闭窗口/重开场景不得残留临时Transform、材质或白色占位面片。
- 修改节点位置后，必须保存并重开验证引用、战斗锚点和 Game 预览仍一致。

### 正式流程实现顺序
- [ ] 从 Battle.scene 启动正式 `RouteBranchRuntime`，移除 Y样例反向加载 Battle 的临时职责。
- [ ] 正式路线选择 UI：Left / Right；单出口也等待确认；双击只提交一次。
- [ ] 选择后锁定连接，按路径位置/切线驱动视觉层，暂停战斗输入和路线按钮。
- [ ] 抵达节点后锁定视觉朝向，校准 Battle Camera/地面高度，隐藏展示敌人，启动真实 `StageController.StartRouteBattle()`。
- [ ] 战斗完成→奖励/三选一/经验收集→下一个正式节点状态；不能直接跳过阻塞交互。
- [ ] 两条分支分别绑定战斗配置，B/C抵达位置与战斗背景/敌人生成标记可在 Scene 调整。
- [ ] 暂停、恢复、失败、退出Play、重新进入、路线token和旧回调清理。
- [ ] 最终再删除/停用 `BattleYRouteHost`临时验证器和独立Y样例启动逻辑。

### 必测用例（必须真实操作，不只调用方法）
- [ ] Edit Mode打开 Battle Host：Scene显示平铺分支、节点、敌人阵型标记；Game显示Battle相机下预览。
- [ ] 调整A/B/C节点、路径点和战斗锚点，保存重开，Scene/Game/Play引用一致。
- [ ] Play启动→打开正式路线选择→选择Left→实际移动转向→抵达B→真实敌人生成→完成战斗/奖励。
- [ ] 重新开始→选择Right→实际移动转向→抵达C→真实敌人生成→完成战斗/奖励。
- [ ] 左右选择的目标节点、背景、敌人阵列和战斗配置不串线。
- [ ] 旅行中暂停/恢复；双击路线按钮；退出Play；旧协程/回调不会再次进入节点。
- [ ] 玩家、敌人、QTE、投射物世界坐标和战斗命中不随视觉转向改变。
- [ ] 玩家脚底与 Battle 地面一致，左右分支抵达都不下沉、不漂浮。
- [ ] Game预览拖动Progress不会启动战斗、奖励、visited、存档或改变场景Transform。
- [ ] 关闭预览/重开场景/重复Play 10次：无重复Additive场景、白色面片、残留对象、材质污染、DOTween错误。
- [ ] Console 0 error / 0 warning；记录每步截图和节点距离/路径/战斗状态。

### 当前状态
- [ ] 正式 `RouteBranchRuntime`、正式存档和三分支仍未完成。
- [ ] B/C→D真实合流仍未完成。
- [ ] Battle视觉地面与战斗坐标高度仍需最终统一；不要再通过增加 `Enemy.visualYOffset` 修复，当前已撤销错误抬高逻辑。

### 本轮已验收：两分支卷轴样例与Battle宿主接入（2026-09-27）
- [x] `YJunctionSample.unity`：单一连续地板上的Y形Left/Right道路；Scene可编辑，Game Progress可观察连续转向。
- [x] Y样例使用Battle Main Camera时，左右路径分别可达-45°/+45°；树石、营地和展示敌人随路线视觉显示；展示敌人无Enemy战斗组件。
- [x] Battle.scene为唯一战斗宿主；Y样例作为Build Settings中的Additive路线层加载；编辑器菜单 `Tools/Curved Scroll/Open Battle Host` 可联合打开Battle和Y路线层。
- [x] Battle Host禁用旧RouteStageRuntimeV2与Y样例反向YSampleBattleHost，避免重复管理器。
- [x] 实际流程已验证：Battle启动→Y路线层→开局移动→真实Battle→奖励/三选一阻塞→Left/Right选择→Progress实际移动→抵达后再次StartRouteBattle生成真实敌人。
- [x] `Time.timeScale=0`奖励状态下路线仍可用unscaled时间推进；Progress可从0到1；左右共用SmallBattle测试资产；Console 0 error/0 warning。
- [x] 编辑器场景与布局保存重开正常；Battle Host Additive打开后Scene/Game可联合观察。
- [x] 撤销错误Enemy抬高逻辑：`applyEnemyVisualOffset=false`，Enemy rootY/visualYOffset恢复原始值。
- [ ] 人物/敌人脚底与Battle视觉地面最终高度仍待后续单独校准；本轮只确认未再抬高敌人。

### 后续正式化任务
- [ ] 用正式RouteBranchRuntime替代BattleYRouteHost测试Host，保留节点、奖励、路线token和回调清理。
- [ ] 接入正式路线选择UI，完成Left/Right完整自然操作回归。
- [ ] 绑定不同分支战斗配置、战斗背景和Scene敌人生成标记。
- [ ] 完成B/C→D合流和正式存档，不扩展三分支前不改变两分支规则。

### 场景搭建可操作性重构：路线、战斗节点与敌人出现位置统一（2026-09-27）

#### 明确需求
- [ ] `YJunctionSample` 不再只是路线视觉样例；它必须成为可编辑的路线与战斗节点 authoring 场景。
- [ ] 美术人员可以在 Scene 中直接选择、移动和保存：路线节点、分支路径点、战斗区域、玩家进入点、敌人阵型预览点以及环境插片。
- [ ] 每个可进入战斗的路线节点必须有唯一的 `BattleEncounterAuthoring` 配置，并明确绑定一个或多个 `StageConfig`/BattleEntry。
- [ ] Scene 中显示的敌人预览必须来自绑定的真实战斗配置/FormationConfig；不能继续使用与真实战斗无关的手工 `Display Enemies - no combat` 作为唯一依据。
- [ ] 敌人预览只负责编辑显示，不挂 `Enemy`、不接管对象池、不参与伤害；真实敌人仍由 `Battle.scene` 的 `StageController` 生成。
- [ ] 到达路线节点后，运行时必须读取该节点的战斗配置、Battle Anchor、玩家进入方向和战斗区域参数，再启动真实战斗；不能只调用固定 `StartRouteBattle()` 而忽略场景配置。
- [ ] 战斗区域、道路环境与战斗坐标职责必须清晰：环境可以随路线视觉转向，Player/Enemy/QTE/Projectile 仍由 Battle 宿主控制，不能被美术层随意移动。
- [ ] 左路与右路必须分别可配置、可预览、可验证，不能共享同一套无法区分的敌人出现位置或战斗配置。
- [ ] 任何场景保存都不得把运行时生成的道路、路肩、临时投影对象或预览对象写入 `.unity`；编辑数据必须是 Hierarchy 中真实、可追踪、可保存的对象或明确的 ScriptableObject 引用。

#### 目标场景层级
```text
Y Junction - Authoring Root
├─ Route Visual
│  ├─ Terrain / Road
│  ├─ Left Branch Content
│  └─ Right Branch Content
├─ Encounter Authoring
│  ├─ Opening Encounter
│  │  ├─ Battle Anchor
│  │  ├─ Player Entry Marker
│  │  └─ Enemy Spawn Preview
│  ├─ Left Valley Encounter
│  │  ├─ Battle Anchor
│  │  ├─ Player Entry Marker
│  │  └─ Enemy Spawn Preview
│  └─ Right Camp Encounter
│     ├─ Battle Anchor
│     ├─ Player Entry Marker
│     └─ Enemy Spawn Preview
└─ Route Markers
   ├─ Opening Arrival
   ├─ Left Arrival
   └─ Right Arrival
```

#### 实现方案
- [x] 新增可序列化的 `BattleEncounterAuthoring` 组件/数据结构，已包含：`nodeId`、`displayName`、`worldDistance`、`StageConfig[] battleConfigs`、`battleAnchor`、`playerEntry`、`battleAreaSize`、预览颜色、波次/标签显示开关和启用开关；暂未接入正式运行时战斗流程。
- [x] 新增统一的 `BattleEncounterAuthoringEditor`，Inspector 已提供 StageConfig 引用、战斗区域尺寸、玩家进入点、显示波次、预览颜色和定位/聚焦操作；禁止要求美术人员手工填写隐含 fileID 或字符串路径。
- [ ] 重构 `ScrollBattleSpawnMarker`：从对应 `StageConfig.formationConfig` 和 Wave 数据绘制真实敌人生成预览，支持按波次开关、敌人 ID/行列标签、Battle Anchor、玩家进入点和区域边界显示。
- [x] 已在 `YJunctionSample` 部署 `Encounter Authoring/Opening Encounter`、`Left Valley Encounter`、`Right Camp Encounter` 三个真实层级；分别绑定 `NarrowRoadBattle_01/02/03`，并创建可移动的 Battle Anchor 与 Player Entry。
- [ ] 在 `YJunctionSample` 中将现有 `Display Enemies - no combat` 降级为临时兼容展示，不能继续作为正式配置来源。
- [ ] 扩展 `BattleYRouteHost` 或后续 `RouteBranchRuntime`，按当前路线节点查找对应 Encounter；到达后同步当前 `StageConfig`、Battle Anchor/玩家进入方向和地面契约，再调用 `StageController.StartRouteBattle()`。
- [ ] 为左路、右路和开局分别绑定可区分的测试战斗资产，至少能在 Scene 中看出阵列差异；禁止继续让左右路线默认共用同一个 `SmallBattle` 作为最终实现。
- [ ] 明确坐标契约：Encounter 的位置使用路线层世界坐标；Battle Anchor 只作为环境/相机对齐参考；真实战斗对象的位置由 Battle 宿主决定；不得通过运行时批量修改 Enemy `visualYOffset` 掩盖地面高度错误。
- [ ] 将“编辑器预览”和“Play 运行”共用同一份保存数据，但预览只做临时渲染姿态；在 `onPreCull/onPostRender` 或等价生命周期中恢复 Transform、材质和 shader 状态，禁止预览污染场景。
- [ ] 补充菜单入口：打开 Battle Host 后自动加载路线层并选中 Authoring Root；提供“显示/隐藏战斗区域”“显示/隐藏敌人预览”“校验所有 Encounter”命令。
- [ ] 增加验证器：检查节点 ID 唯一、StageConfig 不为空、Battle Anchor/Player Entry 存在、左右分支引用不串线、战斗区域有效、预览敌人数与 Wave 数据一致、路径终点与 Encounter 对齐。

#### 实现边界与暂不做事项
- [ ] 本阶段只实现两分支 Left/Right，不扩展三分支。
- [ ] 不把真实 Enemy 预制体放入路线场景，不在路线场景中创建第二套对象池、StageController 或玩家。
- [ ] 不通过移动 Battle.scene 的 Player、Enemy、Camera Transform 来适配每个美术节点；必须通过明确的 Anchor/坐标适配层解决。
- [ ] 不继续新增营地/山谷插片，直到战斗区域和敌人生成预览可操作性验收通过。
- [x] 初始部署：环境V2资源、三层地面候选、独立天空背景和54株草簇已存在；不是整体视觉或完整Play验收。后续优先用Game截图迭代已有布局，不再盲目生成同类草素材。

#### 检测与验收方案
- [ ] 静态场景检查：打开 `YJunctionSample`，Hierarchy 中能找到 Opening/Left/Right 三个 Encounter；每个 Encounter 可选中、可移动、可保存。
- [ ] Inspector 检查：每个 Encounter 可直接看到并修改 StageConfig、Battle Anchor、Player Entry、战斗区域尺寸、波次显示和预览开关；不依赖代码改值。
- [ ] 预览检查：切换每个 Encounter 后，Scene Gizmo 显示战斗区域、玩家进入点、Battle Anchor 和按真实 Wave/FormationConfig 生成的敌人位置标签。
- [ ] 数据一致性检查：修改 FormationConfig 或 StageConfig 后重新打开场景，预览阵列数量、行列、敌人 ID 与实际配置一致；空 Wave/空行不生成假标记。
- [ ] 左右隔离检查：左路与右路绑定不同测试 StageConfig，预览敌人数、敌人 ID、背景和 Encounter 名称均不串线。
- [ ] 保存重开检查：移动 Battle Anchor、Player Entry、路径点和一处建筑，保存、关闭、重开后位置和引用保持；场景文件不出现新增 `Generated Route Road/Shoulder` 或临时预览对象。
- [ ] 运行检查：Battle Host 启动后进入开局 Encounter；完成奖励并选择 Left/Right 后，沿选中道路抵达对应 Encounter，真实战斗使用该 Encounter 的 StageConfig，而非固定配置。
- [ ] 生成检查：运行时路线场景中没有 `Enemy` 组件的展示对象；真实敌人数量、行列和敌人 ID 与当前 Encounter 配置一致，战斗仍由 `StageController`/对象池管理。
- [ ] 坐标检查：路线转向过程中 Player/Enemy/QTE/Projectile 的战斗坐标和命中逻辑不因环境旋转改变；抵达左路/右路后人物脚底与 Battle 地面一致，不下沉、不漂浮。
- [ ] 生命周期检查：Edit Mode 预览、Play、暂停、恢复、退出 Play、重复打开 Battle Host 各 10 次；无重复 Additive 场景、残留 Transform、材质污染、白色面片、MissingReference、DOTween 或数组越界错误。

### 卷轴场景部署错误复盘与安全实施协议（2026-09-28，必须优先遵守）

#### 已发生且已确认的错误
- [ ] **错误目标场景**：多次把旧的 `Assets/Experiments/CurvedScroll/FakeRouteDataTrial.unity` 当作当前关卡部署入口；该场景是旧单路线/连续距离实验，不是当前正式分叉宿主。后续不得打开、保存、回退或继续部署它。
- **本轮已确认的操作错误**：在未确认目标和未保存工作前反复打开/保存旧FakeRoute，出现大量Generated Route Mesh序列化重排；并把真实布局 `Small_13_RockRound` 当残留对象误删。大diff或Mesh名称不能直接证明不移动根因，也不能据此批量删除场景GameObject。
- **错误整场景回退**：曾执行实验场景 `git restore --source=HEAD`，未保护Editor内存及用户未提交状态；随后用户看到错误的旧实验入口。不能仅凭Git状态断言具体未提交分叉内容已丢失；后续应按明确差异和对象身份局部恢复，不整场景回退。
- [ ] **错误混淆正式宿主**：分叉道路正式结构是 `Assets/Scenes/Battle.scene` + Additive `Assets/Experiments/CurvedScroll/YJunctionSample.unity`；`BattleYRouteHost` 位于 Battle.scene。不能通过切回 `FakeRouteDataTrial` 判断分叉道路是否丢失。
- [ ] **错误部署环境材质**：曾把 v2 道路/背景材质部署到 `FakeRouteDataTrial`，不是当前 Y 分叉路线层；之后才在 `YJunctionSample` 的 `Terrain - one mesh, fork painted in terrain` 上建立 `YSampleGround_v2`。
- [ ] **错误声明 Pivot 已修复**：大型插片实际读取到的 Sprite Pivot 仍接近中心，例如 `Watchtower_v1` 的 pivot 为 `(401,743)`；仅设置导入器字段、没有回读 `Sprite.pivot`、`Sprite.bounds` 和 `Renderer.bounds.min.y`，不能宣称已贴地。
- [ ] **错误固定坐标布景**：大型插片按任意世界 X/Z 直接放置，没有按开局直路、左/右转弯路径和道路横向偏移布置，造成转弯后道路两侧空、景物偏离或看起来悬空。
- [ ] **错误背景职责**：天空盒式背景被加入 `YScrollSample.scenery`，与普通树石/建筑一起参与路线投影；背景应是独立远景层，不能被道路曲率、近景裁剪和普通景物淡出接管。
- [ ] **错误把纹理替换当成分支道路完成**：仅替换 `YSampleGround` 的 `_Road/_Side` 纹理不能证明左右转弯已有连续可见道路；当前 Y 场景是单一网格配合 `YScrollGround.shader` 绘制分叉遮罩，必须先检查网格覆盖、Shader 分叉坐标和相机裁剪，再决定最小修复，不得盲目生成运行时道路。
- [ ] **错误在未验证移动前继续堆插片**：开局、左转、右转和终点没有逐段截图/运行基线，就继续增加建筑，导致美术问题掩盖了地面投影与移动问题。

- 错误原因更正：本节早期把Generated Route序列化重排、裁剪距离或网格不足直接断言为“不移动/悬空”原因，缺少验证；最新同机位背景开/关证据在下方执行结论与交接第14节，必须优先采用。

#### 当前正式场景边界
- [ ] 唯一正式运行宿主：`Assets/Scenes/Battle.scene`。
- [ ] 唯一当前分叉美术/路线层：`Assets/Experiments/CurvedScroll/YJunctionSample.unity`，以 Additive 方式加载。
- [ ] 当前允许修改：Y 场景中的真实 Hierarchy、Y 场景专用材质、环境素材和与 Y 场景绑定的编辑器数据。
- [ ] 当前禁止修改：`FakeRouteDataTrial.unity`；除非用户单独授权，不打开、不保存、不回退、不导入其材质。
- [ ] 当前移动核心冻结：`YScrollSample.Evaluate()`、`YScrollSample` 的路线状态/Progress、Y 路径点与转角、`BattleYRouteHost`、Battle.scene 玩家/敌人/相机/战斗管理器、QTE/投射物坐标。
- [ ] 不使用 `RouteStageRuntimeV2`；不让 Y 样例反向加载 Battle；不把真实 `Enemy`、对象池或 `StageController` 放进路线层。

#### 完善后的分阶段实施方案
- [ ] **Phase 0：建立移动基线**。保持 `Battle.scene` 为 Active、Y 场景 Additive 加载；不改任何 Transform/材质/脚本。记录 Progress `0/0.25/0.5/0.75/1` 的 Game 截图、道路覆盖、侧景脚点、当前路线角度和 Console 状态。若基线移动失败，先停在诊断，不部署美术。
- [ ] **Phase 1：验证分支地面覆盖**。只读检查 Y Terrain 网格 bounds、顶点范围、左右路径采样范围、`YScrollGround.shader` 的分叉距离计算、`_YSEnabled` 投影和相机裁剪。确认左右转弯地面缺失的真实原因后，优先修现有持久化 Y 场景网格/材质；不得在 Play 或 `OnEnable` 中生成并保存道路/路肩。
- [ ] **Phase 2：持久化道路备用方案**。仅当遮挡、相机绑定、Shader及网格对照确证当前Terrain无法满足并重新确认范围时，才设计持久化Road/Shoulder Mesh。当前道路连续且本轮不需要新道路；不要部署未使用的 `YRouteSurfaceAuthoring` 草稿（含OnEnable重建和UnityEditor依赖）。
- [ ] **Phase 3：独立天空盒背景**。从 `YScrollSample.scenery` 移除天空背景 Renderer；使用独立背景层/专用背景材质，只承担天空、云、远山和大气雾。背景不参与道路曲率、近景裁剪或景物淡出；Progress/Left/Right 截图确认它稳定且不遮挡战斗。
- [ ] **Phase 4：路径化两侧景观**。为每条连接建立可编辑的 `ConnectionContent`/景观分组；景物位置由路径采样点、道路横向偏移、层级距离和物体脚点共同确定。开局直路、左路、右路分别布置近景/中景/远景，保留中央战斗通道；不以随机数组索引或任意世界坐标批量撒物件。
- [ ] **Phase 5：插片贴地验收**。每个新 Sprite 导入后必须回读实际 `Sprite.pivot`、`Sprite.bounds`、`Renderer.bounds.min.y`；底部不透明像素与对应地面高度对齐后，才允许加入路线景观列表。大型建筑、栅栏、旗帜和火盆分别检查脚点、遮挡层和转弯后的姿态。
- [ ] **Phase 6：分支自然操作回归**。完成开局战斗/奖励后真实选择 Left 和 Right，分别观察旅行、转向、抵达、战斗和奖励；确认道路连续、景物不悬空、中央区域无遮挡，并确认 Battle.scene 战斗坐标未改变。

#### 保存与回退安全协议
- [ ] 每次改动前记录 Active Scene、Loaded Scenes、Scene dirty 状态、Git status 和目标文件列表；不以“当前看起来是 Battle”替代实际目标确认。
- [ ] 保存前必须满足：Active Scene=`Battle.scene`；Y 场景已 Additive 加载；`FakeRouteDataTrial` 未加载；Hierarchy 中无运行时生成道路/路肩；无临时预览树石；无 `DontSave` 伪持久化对象。
- [ ] 任何场景写入只允许明确写入 `YJunctionSample.unity`；素材/材质写入项目 Authoring/Art 目录；禁止通过打开旧场景触发导入或保存。
- [ ] 禁止对场景使用 `git restore`、整文件覆盖或“保存后再看差异”式试错；需要回退时先保存 `git diff`/对象清单，确认用户未提交改动边界，再做局部恢复。
- [ ] 每次只做一个可验证变更：地面、背景、单组景观、单个 Pivot 不得混在一次保存中；失败立即停止，不继续堆叠后续美术。

#### 必测检查矩阵
- [ ] Edit Mode：Progress `0/0.25/0.5/0.75/1` 下道路连续、左右分支覆盖、天空背景稳定、景物脚点贴地。
- [ ] Play Mode：Battle 启动→开局战斗→奖励阻塞→Left；记录移动时长、Progress、路线角度、道路覆盖和景物位置。
- [ ] Play Mode：重新启动→开局战斗→奖励阻塞→Right；执行同样记录，不能只调用 `ChooseBranch()` 代替自然操作。
- [ ] 坐标隔离：旅行前后 Player/Enemy/QTE/Projectile 世界坐标、Battle Camera 和 StageController 坐标不因美术投影变化。
- [ ] 场景持久化：移动一件景物、保存 Y 场景、关闭重开并重新打开 Battle Host；Transform、Sprite、材质、路径引用保持，Y 场景无运行时生成物。
- [ ] 生命周期：Edit Mode 预览、Play、暂停、恢复、退出 Play、重复打开 Battle Host 10 次；无单路线场景切入、无重复 Additive、无道路/路肩重建、无材质污染、无白色面片、无 MissingReference/DOTween/数组越界。
- [ ] 完成标准：以上矩阵全部通过并有截图/日志后，才允许继续增加大型建筑或新的环境素材；“编译通过”“材质已替换”“一次 Progress 调用成功”均不等于路线完成。

#### 当前执行结论（以2026-10-01交接为准）
- [x] 此前已回读Left/Right五点移动数值，近期草调整未改移动采样；本次交接复核Active Battle + Additive Y、FakeRoute未加载、双方dirty=false、Console error/warning=0。数值基线不是新战斗层下自然Play验收。
- [x] 已用同机位天空开/关验证：此前地面空带来自透明天空卡片遮挡，而非已证实的道路网格缺失。共享地面/景物Shader的未奏效裁剪/曲率实验已恢复到当前HEAD，无diff；Y Terrain继续使用YSampleTerrain。
- [x] 已保存三层地面候选 `Assets/Experiments/CurvedScroll/Authoring/YSampleGround_ThreeLayer_Candidate.mat` + `Assets/Experiments/CurvedScroll/Shaders/YGroundThreeLayerCandidate.shader`；Road/Shoulder/Outer=BattleRoad_v2/BattleShoulder_v3_candidate/BattleRoadside_v2。开局和左右候选截图已查看，硬切改善，最终美术观感仍待用户验收，不是实际高度差地面。
- [x] 独立 `YSkyBackground.shader` 和天空材质已应用；天空不在scenery，1024×1536尺寸保持、UV方向修正，不再覆盖道路。只是2D远背景而不是真cubemap；新增最终天空素材暂不需要。
- [x] 当前保存54株草（开局20/Left17/Right17），使用独立 `YGrass.mat/YGrass.shader`；降低Y高度、保留X宽度，添加相机朝向及作者Y偏转。保存重开后54草、54列表引用、非零Y偏转54株；高度约0.4836–0.993、绝对偏转7–25°，yaw开/关图差10906像素。
- [x] 历史草高度/朝向截图已记录；后续建筑布景及风动接入后，当前scenery为151项（天空不计入），54个唯一风动引用，无空项/重复。旧138项不再作为当前计数。
- [x] 草摇曳效果已由用户确认实现；54株各有一个21顶点/24三角形细分网格，原SpriteRenderer保留但禁用。用户的风动确认不等于全部Progress构图、切片缺陷和自然路线流程已验收。
- [x] 大建筑脚点、旧帐篷切片与战斗节点镜像布景已在第14节记录修正和保存验证；此处旧“建筑仍待修”结论已过时。整体主观构图仍以用户后续反馈为准。
- [ ] Edit Mode相机绑定未持久化：交接实查viewCamera=None，Sample Camera disabled。先安全会话绑定，避免将未投影画面错认成不移动或回退。
- [ ] 新战斗层分支上的自然Left/Right流程、Boss/QTE、奖励和重复Play测试尚未完成。
- [ ] 场景/新环境资源仍未commit/push；不操作受保护FakeRouteDataTrial和并发战斗移植未提交文件。

