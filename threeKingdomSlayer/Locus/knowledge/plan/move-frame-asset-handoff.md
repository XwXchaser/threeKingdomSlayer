---
id: kd_3780175d-6e61-41bf-ae31-0031e70c0903
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 招式动作帧素材交接（给美术生成 AI）

本文档用于交接「招式动作帧」的美术生产与接入配置。执行方需要能生成/编辑像素素材，并按第 6 节完成 Unity 配置。
范围：**先做张飞三段枪突（Jab1 / Jab2 / Jab3）的动作帧区分**；同一套规则可复用到其它招式。

---

## 0. 先读什么 + 系统速览

### 0.1 必读顺序

1. **`design/combo-move-state-machine.md`（权威设计与实现状态）**。至少读：§1 术语、§2 基线、§3 招式转移图、§5 输入模型、§6 数据与配置、§11.5～11.8 实现状态。本文档只写素材与接入，不重复系统语义。
2. **本文档**：本次任务的素材规格、生成约束、接入步骤与验收清单。
3. 需要时：`memory/stab-vfx-generation.md`（素材生成经验与失败原因）、`memory/stab-programmatic-animation.md`（换帧实现与导入规则）。

### 0.2 系统速览（足够看懂本任务，细节以设计文档为准）

**组成**：输入（手势 + 蓄力等级）→ `PlayerMoveStateMachine` → 招式资产（`AttackSkillConfig`）组成的图 → `AttackSystem` 执行。

**现行输入语义**（分叉轴是「是否蓄力」，不是方向）：

| 输入 | 结果 |
|------|------|
| 点击 | 戳击 Stab |
| 长按不移动（≥ 0.3s） | 穿刺 Pierce |
| 竖滑 + 蓄力 | 挑飞 Launch |
| 竖滑 + 未蓄力 | 招架 Parry |
| 横滑 + 蓄力 | 横扫 Sweep |
| 横滑 + 未蓄力 | 斩击 Slash |
| 斜滑（不分蓄力） | 斩击 Slash |

**蓄力分级**：`InputManager.chargeLevelTimes` = 0.3 / 0.6 / 1.0 秒 → 一级 / 二级 / 三级。一级门槛与蓄力视觉出现时刻对齐。等级会传给招式，可用 `chargeLevelDamageMultipliers` 按级缩放伤害（默认空 = 不改数值）。

**招式资产字段**（一层结构，窗口与接续边直接写在招式上）：

- 原有：伤害 / 范围 / 时长 / 表现（含 `attackWavePrefab`）
- `repeatSelf`：是否允许用同一手势重复本招（关闭即为串尾）
- `overrideWindow` + `windowStart01` / `windowEnd01`：接续窗口（按该招序列的归一化比例）
- `moveEdges[]`（`AttackMoveEdge`）：手势 + 窗口覆盖 + 后继招式资产

**招式表**：每武将一份 `MoveTableConfig`，`roots` 配「中立态手势 → 入口招式」；未配置的手势回落到默认映射。张飞的表是 `MoveTable_Zhangfei`，已挂在 `Hero_Zhangfei.moveTable`。

**运行时行为**：招式进行中只在窗口内接受输入，接上边就取消当前收招直接出下一段；窗口未开且该输入有出路时进输入缓冲，窗口打开自动执行；**没有出路的输入立即不响应**（第三段就是这种情况）；阶段时钟与动作锁同步，命中卡肉时冻结；收尾走完回中立。

**内容现状**：只有张飞三段枪突建了招式资产与接续边；其它招式仍用 `HeroConfig.skillConfigs` 里的默认配置，不参与连段。

---

## 1. 交付物

| # | 交付物 | 说明 |
|---|--------|------|
| 1 | 三段枪突各自的武器精灵（透明 PNG） | 三段要能看出是同一把枪的三种刺出形态，不能只是同一张图 |
| 2 | 每段对应的 Prefab（若走 A 方案） | 每个 Prefab 内置该段自己的 SpriteRenderer 精灵 |
| 3 | 接入后的 Unity 状态 | 三份招式资产分别指向各自的素材，实机可见三段不同 |
| 4 | 一份填写完成的验收记录 | 见第 7 节清单 |

不改代码即可完成的部分：**A 方案（每段一份 Prefab）**。
如果需要「每段各自的高速中间帧」，需要先改代码（见 6.3），请先回报，不要擅自改代码。

---

## 2. 招式系统如何吃素材（现状）

一个招式 = 一份 `AttackSkillConfig` 资产。三段枪突现在是：

| 资产 | 路径 | 伤害 | actionDuration |
|------|------|------|----------------|
| Jab1 | `Assets/ScriptableObjects/Moves/Zhangfei/Zhangfei_Jab1.asset` | 20 | 0.45 |
| Jab2 | `Assets/ScriptableObjects/Moves/Zhangfei/Zhangfei_Jab2.asset` | 26 | 0.40 |
| Jab3 | `Assets/ScriptableObjects/Moves/Zhangfei/Zhangfei_Jab3.asset` | 34 | 0.60 |

**问题**：三份资产的 `attackWavePrefab` 都指向同一个 `Assets/Prefabs/Stab.prefab`，所以三段动作视觉完全相同。

### 2.1 每个招式自己能指定的素材字段

| 字段 | 作用 |
|------|------|
| `attackWavePrefab` | **每招独立**。刺出时实例化这个 Prefab，取它子节点里的 `SpriteRenderer.sprite` 作为武器图 |
| `stabVisualReachOffset` | 视觉沿攻击方向额外前伸的世界距离（不影响命中） |
| `stabVisualTargetRandomRadius` | 枪头终点在面向相机的圆盘内随机偏移半径 |
| `stabSpawnYOffset` / `stabSpawnZOffset` | 生成位置偏移 |

### 2.2 目前挂在 AttackSystem 组件上的共享帧（**不是每招独立**）

| 字段 | 当前素材 | 用途 |
|------|----------|------|
| `_stabSpeedSprite` | `Assets/Sprites/zhangfei/stab_v13.png` | 普通 Stab 的高速中间帧 |
| `_stabRotate1Sprite` / `_stabRotate2Sprite` | `stab_rotate1/2.png` | 横扫类弯曲帧，**普通直刺不要用** |
| `_launchSprite1/2/3` | `stab_charge2 / stab_charge1 / stab.png` | 挑飞专用 |

因为高速中间帧是共享的，所以**三段枪突现在共用同一张起始帧和同一张高速帧**。要区分三段，见第 6 节的两个方案。

### 2.3 一把枪的动画时序（决定帧该画成什么姿态）

刺出序列按固定比例切分（`StabSweepEffect` 内常量）：

```
起手 12%  →  刺出 28%  →  穿入 8%  →  回收 52%
```

- 「起手」是后撤蓄势，仍显示起始帧。
- 「刺出」是武器本体高速延展阶段；高速中间帧只在这一段内替换显示。
- 「穿入」是枪头到位后轻微再推进，形态接近刺出末态。
- 「回收」拉回起始帧。

素材只需提供「姿态」，不需要提供时间——时序由代码控制。

---

## 3. 本次需要的三段造型（设计意图）

三段必须读作**同一把长枪**（银灰枪尖、红穗、绿饰、红棕枪身的配色与结构不能变），差别在刺出的延伸幅度与速度语言：

| 段 | 定位 | 素材诉求 |
|----|------|----------|
| Jab1 | 起立直刺，基准形态 | 可直接沿用现有 `stab.png`（作为基准帧，不必重做） |
| Jab2 | 连贯加长的中段刺 | 枪身明显更长，尾端延伸更远；速度面比 Jab1 更强调，但仍以「武器本体变形」为主 |
| Jab3 | 收招重刺，最强一击 | 最长延伸、最明显的高速面；枪尖仍必须清晰可辨。**不要做成必杀技或全屏特效规模**，仍是普通攻击的尺度 |

三段放的顺序是 Jab1 → Jab2 → Jab3，所以视觉强度应递增。

---

## 4. 素材规格与导入设置

| 项 | 要求 |
|----|------|
| 格式 | 透明 PNG（RGBA） |
| 导入设置 | Point 过滤、无 Mipmap、无压缩、RGBA32 |
| 画布尺寸 | 允许与 `stab.png` 不同；但必须以**同一视觉锚点对齐**（枪尖位置），靠透明留白 / PPU / Pivot 在素材侧解决 |
| 方向 | 与现有 `stab.png` 一致（枪指向画面内/前方），不要自带透视翻转 |
| 命名 | 建议 `stab_jab2.png` / `stab_jab3.png` 放在 `Assets/Sprites/zhangfei/` |
| 参考素材 | 身份参考用 `Assets/Sprites/zhangfei/stab.png`；速度形变参考用已验收的 `Assets/Sprites/zhangfei/stab_v13.png` |

**硬规则**：运行时只做 `SpriteRenderer.sprite` 替换，不会为不同尺寸的帧补偿位移或缩放。因此**帧与帧之间的可见位置必须靠素材本身对齐**，否则会出现帧一换武器就跳位或飞出画面。

---

## 5. 生成提示词要求与已知失败模式

以下经验来自本项目已完成的 Stab 素材生产，**必须遵守**：

必须做的：
- 明确这是「武器本体的高速形变」，不是独立特效图。
- 原始武器图负责颜色、枪尖、枪杆、装饰的身份；动作参考只负责透视、冲击方向与速度语言。
- 一次只生成单张姿态，不要一次出多帧概念图。
- 保留可识别的枪尖作为远端锚点——可以变小，但不能删除或模糊到认不出。
- 主体描述为「长枪身核心 + 长距离近似平行的白色高速面」。
- 红穗、绿饰只作局部、不连续的速度结构。

已知会失败的做法（不要重复）：
- 反复强调「近端宽、远端窄、远端小点」→ 会生成等腰三角形 / 圣诞树 / 旗帜轮廓。
- 让红绿装饰铺满两侧 → 边缘变树枝状，破坏武器比例。
- 把枪尖描述成「几乎消失」→ 丢失识别锚点。
- 一次生成多帧 → 帧与帧不像同一物体。
- 把重绘武器、运动残影、碰撞闪光、独立 VFX 混在一个 prompt 里 → 会变成角色、爆炸、光束或泛化长枪。
- 过度夸张「极限速度、巨大穿刺、全屏通道」→ 会把普通攻击推成必杀技规模。
- 用生图模型对已验收素材做「局部编辑」→ 模型仍会重绘整图，改变构图与比例。
- 对素材做矩形切片、错位、重叠的程序加工 → 出现横向接缝，像拼接而非连续形变。
- 直接画几何色块替代武器 → 丢失原始像素纹理，变成硬质长条。

---

## 6. 接入配置

### 6.1 方案 A（推荐，无需改代码）：每段一份 Prefab

1. 复制 `Assets/Prefabs/Stab.prefab` 为 `Stab_Jab2.prefab`、`Stab_Jab3.prefab`（放在 `Assets/Prefabs/Moves/` 或就地）。
2. 在每个新 Prefab 里，把子节点上的 `SpriteRenderer.sprite` 换成该段的新素材。
3. 确认 Prefab 里没有多余组件被删改（射线、碰撞、粒子都不需要动；`StabSweepEffect` 只取 `SpriteRenderer`）。
4. 打开对应招式资产，把 `attackWavePrefab` 指到新的 Prefab：
   - `Zhangfei_Jab1.asset` → `Stab.prefab`（不变）
   - `Zhangfei_Jab2.asset` → `Stab_Jab2.prefab`
   - `Zhangfei_Jab3.asset` → `Stab_Jab3.prefab`
5. 保存资产与场景，进 Play Mode 实机确认。

注意：这样三段各有自己的**起始帧**，但「高速中间帧」仍是共享的 `stab_v13.png`。如果只关心起手与末态的区别，方案 A 足够。

### 6.2 方案 B：每段各有起始帧与高速帧

需要把 AttackSystem 上共享的 `_stabSpeedSprite` 搬到 `AttackSkillConfig` 上，变成每招独立字段，然后 `StabSweepEffect` 按传入值换帧。
**这属于代码改动，请先回报，不要自行修改 C#。**

### 6.3 相关代码位置（仅供理解，不要改）

| 文件 | 职责 |
|------|------|
| `Assets/Scripts/Attack/StabSweepEffect.cs` | 刺出序列、换帧时机、命中判定 |
| `Assets/Scripts/Player/AttackSystem.cs` | 创建刺出特效、把 `_stabSpeedSprite` 传进去 |
| `Assets/Scripts/Core/AttackSkillConfig.cs` | 招式资产字段（含 `attackWavePrefab`、窗口、接续边） |

---

## 7. 验收清单

交付前逐条自检：

- [ ] 三段素材放在 `Assets/Sprites/zhangfei/`，命名与第 4 节一致
- [ ] 透明背景干净，无残留白边或半透明底
- [ ] Point / 无 Mipmap / 无压缩 / RGBA32
- [ ] 枪尖位置在三段之间视觉一致（换帧不跳位）
- [ ] 三段配色与结构读起来是同一把枪
- [ ] 强度按 Jab1 < Jab2 < Jab3 递增，且没有超出普通攻击的尺度
- [ ] 对应 Prefab 已创建且只替换了精灵
- [ ] 三份招式资产的 `attackWavePrefab` 指向正确
- [ ] Play Mode 实机确认：点击三段连打（点一下、收招后段再点、再点）时能看到三种形态
- [ ] 未改动任何 C# 代码；未改动命中、伤害、动作时长、接续窗口

实机验证方法：进入真正的战斗节点（路由转场期间输入是关闭的），点击出第一段，在收招后段（约 0.29s 之后）再点出第二段，再点出第三段。接续窗口当前设在序列的 `0.65~0.95`。

---

## 8. 明令不要做的事

- 不要修改 `actionDuration`、`cooldown`、`damage`、`rangeRows`、接续窗口、`moveEdges`。
- 不要修改 `Assets/Prefabs/Stab.prefab` 本体（它是 Jab1 与其它系统的共用基准）。
- 不要复用 `stab_rotate1/2.png` 给普通直刺（那是横扫弯曲帧）。
- 不要在运行时用 `sprite.bounds` 反推缩放或位移（含大量透明边缘，会算错并导致武器飞出画面）。
- 不要一次重做整套武器美术；本次只做三段刺出形态的区分。

---

## 9. 相关文件索引

| 文件 | 用途 |
|------|------|
| `Assets/Sprites/zhangfei/stab.png` | 武器身份与基准姿态参考 |
| `Assets/Sprites/zhangfei/stab_v13.png` | 已验收的高速中间帧（速度形变参考） |
| `Assets/Prefabs/Stab.prefab` | 刺出特效 Prefab（三段现在共用） |
| `Assets/ScriptableObjects/Moves/Zhangfei/Zhangfei_Jab*.asset` | 三段招式资产 |
| `Assets/ScriptableObjects/Moves/MoveTable_Zhangfei.asset` | 张飞招式表（入口：点击 → Jab1） |
| `Locus/knowledge/memory/stab-vfx-generation.md` | 本素材方向的完整生成经验与失败原因 |
| `Locus/knowledge/memory/stab-programmatic-animation.md` | 换帧实现约束与素材导入规则 |
| `Locus/knowledge/design/combo-move-state-machine.md` | 招式状态机与接续窗口设计（必读；11.8 节为三段枪突现状） |
