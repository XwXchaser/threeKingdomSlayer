---
id: kd_65412ee0-2f14-4b9d-a598-afdce4337b9a
injectMode: inherit
summary: 本周主要目标：音频系统改造（音效/背景音乐独立音量控制）；增强战斗视觉与动效（命中卡肉停顿与命中视觉效果）。其余待办保留。
aiEditMode: inherit
---

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

### 场景化路线 V2（当前进度）
- [x] Battle.scene 旧路线对象已清理；Player、Enemy、Camera、HUD 和战斗管理器保留。
- [x] 新建 `Assets/Scenes/RouteStageV2/Stage01_RouteV2.unity`，包含 A/B 节点、Head/Combat/Tail、A→B 连接。
- [x] 新建 V2 配置与场景绑定组件，RouteStage 场景一次加载、节点切换不加载/卸载单个节点。
- [x] MainMenu 只显示 V2 场景化关卡入口；旧路线入口配置已移除，节点战斗 StageConfig 作为战斗内容保留。
- [x] V2 核心链路已实测：MainMenu→Battle→RouteStage→Head→Combat→Tail→目标节点→终点结算。
- [x] 修复初始 Head 错误后退、战斗输入锁定、Victory/Defeat 面板交互、地板遮挡攻击表现。
- [x] V2 路径采样与 RouteStage 校验工具已建立，当前 Stage01 场景静态校验通过。
- [ ] P0：多 Tail 汇入同一 Head 的场景拓扑和运行验收。
- [ ] P1：多来源 Tail→同一 Head 的旋转角/支点/最终 Pose 验收。
- [ ] **待持续观测：计时被动跨节点首次触发/Head→Combat 时序**：当前保留 `[TimedPassiveDiag]`、`[RouteDiag]` 和 DoT 诊断日志；暂不判定已修复。重点观察 Head→Combat 是否出现 `TimerExpired`/`BurnTick`，`StartRouteBattle` 后首次触发是否成功，以及效果失败时是否错误进入冷却。

- [ ] **场景化路线 V2：P0 多 Tail 汇入同一 Head**：新增 C/D 节点并分别配置 C→B、D→B；通过独立测试起点分别验证 A→B、C→B、D→B，确认共享 B.Head、连接路径独立、B 可继续连接或作为终点。
- [ ] **场景化路线 V2：P1 Tail→Head 旋转对齐**：分别验证不同源 Tail 朝向、rotationPivot、先旋转再移动、最终 Head Pose 一致、Player/Camera 不移动。
- [ ] **场景化路线 V2：路径编辑器增强**：支持任意节点测试起点、路径预览、汇入路径可视化和运行时最终 Pose 误差校验。
- [ ] **节点胜利演出**：普通节点独立演出，等待 BattleEntry、经验/道具三选一和弃置全部完成后播放；不发放整关奖励、不标记通关、不结束路线。
- [ ] **路线存档恢复完整验收**：验证失败恢复先执行运行时 ResetAll，再从存档点节点 Head 重新进入；验证已保存的节点/BattleEntry 状态、经验/等级 UI、被动/主动技能列表、临时 DoT 清理和存档点状态不被死亡时运行态污染。MainMenu“继续游戏”不走该恢复路径，而是从最后未完成关卡的 startNode 开始。
- [ ] **计时被动状态机重构**：将获得、待 Combat 首次触发、效果成功、冷却和失败重试状态分离；当前诊断日志仅用于观测，不视为完成。
