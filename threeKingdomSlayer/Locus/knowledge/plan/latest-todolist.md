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

