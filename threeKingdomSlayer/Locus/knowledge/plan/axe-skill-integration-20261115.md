---
id: kd_f62b8dce-3184-4d19-b3c2-943fa6455e82
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_9b41d7c8-0e26-4f5a-8d3c-1a7e6f2b4c05
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 抡斧接入「抡斧劈砍」技能（2026-11-15）

> 任务：用户验收了斧头贴图，要求「按照文档接入到项目对应技能中」，然后自己进游戏观察表现。
> 本文是接入记录。需求书：`design/weapon-3d-art-spec-axe-and-chain.md`；贴图修复过程见 `plan/axe-model-texture-fix-20261115.md`。

## 0. 会话交接（2026-11-16 更新，**下一轮先读这一节**）

> **地裂概念图最新入口：§6.15。** V1 六格返图和 V2 四格冲击语言返图都已收到；V1 冲击力不足，V2 偏离项目画风并生成了 3D 地形断层。下一步改为 3×2 扁平战斗特效叠加概念板，具体方案和生成审批状态见 §6.15；§6.12–§6.14a 保留为过程记录。

### 0.1 本轮做了什么（4 块）
1. **武器轴心修正**：斧头 Pivot 原先浮在柄尾**旁边 0.21 单位**（不是落在柄尾）⇒ Blender 原点 / FBX / Prefab 三层一起对齐到柄尾
   （`design/weapon-3d-art-spec-axe-and-chain.md` §4.3 “模型原点 = 握持点 = 握把末端”）。
2. **三帧位姿定稿**：美术摆的三帧**原样照搬**（**世界绝对坐标**，不锁敌人）＋ 五段时序重做（见 §0.2）；End 之后**原地淡出**、不再往下沉。
3. **命中感/力量感**：补了 PBZ 演出语法里缺的层（刀光拖尾、音效第二层、镜头低频“被顶开”）＋ 定位了两个真 bug（见 §0.4）。
4. **数值**：斧头杀伤力上调（见 §0.2 末行）。

### 0.2 当前参数速查（都是实测值，改动请同步改这里）
| 项 | 值 |
|---|---|
| 三帧握把（世界绝对） | Start (−0.10, −1.00, −5.47)／Apex (−0.60, −0.80, −5.97)／End (−0.04, −1.20, −5.84) |
| 三帧旋转（世界） | Start (3.0, 93.8, 99.7)／Apex (12.2, 91.8, 337.3)／End (8.3, 90.0, 78.6) |
| 运行时缩放 | **2**（美术摆位时的值） |
| 时序 | 蓄势 0.50s（ease-out 到位）→ 绷住 0.30s → 劈下 0.22s（位置 t²/旋转 t^1.5）→ 顿帧 0.12s → **原地淡出 0.45s** |
| 命中帧时刻 | t = 1.02s |
| **技能数值** | Lv1 伤害 **200**/削韧 **100**/2 排；Lv2 **300**/**100**/3 排；Lv3 **500**/**200**/4 排（三档都无视盾减伤） |
| 命中反馈 | 斧身高光 peak 0.35/0.06s ＋ 敌人闪白 ＋ 音效两层（金属瞬态 + 低频闷响）＋ 镜头推近 1.6°/0.22s ＋ 镜头顶开 0.07 单位/0.16s/滚转 0.25° ＋ 敌人 Heavy 卡肉 0.14s |
| 刀光 | `Axe3D` 挂 `MeshWeaponTrail`，锚点 Tip + BladeMid（局部 −0.20, 2.55, 0），材质 `BattleAxe_Trail`（`Custom/WeaponTrailAdditive`） |

### 0.2b 入池与图标（2026-11-16）

| 项 | 状态 |
|---|---|
| **已在技能池** | `ActiveSkillPoolConfig.rarePool` 含 `ActiveSkill_AxeSmash`（权重 100，**无天赋门槛、无前置**）。实测三选一候选表：抡斧劈砍在内 ✓（注：§5 里“它不在任何池里”的说法是 11-15 的过期记录，实际 11-14 已入池） |
| **开局默认赠送已撤** | Battle 场景 `ActiveSkillInventory`：`_grantDefaultSkills` 1→**0**、`_defaultSkills` [AxeSmash]→**[]** ⇒ 开局库存 0 条，只能从三选一拿。**教学不受影响**（教学三选一是强制池：旋风 / 海浪） |
| **图标** | 抡斧劈砍 → `icon_31_power.png`（暖橙·力量）；追命锁链 → `icon_31_huixuanbiao.png`（钢蓝·投掷钩）——**临时占位**，等正式美术图替换。顺手把**冰雾**从 `icon_31_timedFire.png`（与火线重图标）换到 `icon_31_larger.png`（蓝调）⇒ 池内 15 个候选图标**互不重复**（实测 dup=0） |

### 0.3 还没做的（下一轮/别处对话的候选）
| 类别 | 事项 |
|---|---|
| **防御相关 BUG（用户 2026-11-16 提出，未展开）** | 破防/格挡链还有 BUG 要调优 ⇒ 另一个对话做。相关背景：`design/guard-break-presentation-overhaul.md`（PBZ 演出语法 + 已否决方向） |
| 美术待出 | **地裂贴图**（需求：`design/axe-ground-crack-vfx-spec-20261115.md`）、**尘土**（512² 3~4 帧）、**碎屑**（256² 4~6 帧）、**冲击环**（512² 单帧）。清单与量级见 `design/axe-impact-presentation-spec-20261116.md` §4 |
| 玩法待拍板 | 敌人被击退（`Displacement` 源，改阵型）；技能改成 PBZ 式「按住蓄力、蓄满再放」；Lv1/Lv2 削韧都是 100 ⇒ 对普通兵/BOSS 都是一发破、**等级无梯度**（要梯度就给 60/120/220） |
| 结构上限 | 玩家无可见身体 ⇒ PBZ 的重量感只能靠镜头/拖尾/低频音/地面反应外借 |

### 0.4 本轮踩到并修掉的坑（**别重走**）
1. **运行时不能读斧 mesh 的顶点**：FBX 导入 `isReadable=false` ⇒ `mesh.vertices` 抛异常、**协程直接中断**（连带伤害结算不执行）。
   ⇒ 用编辑态量好的常量 `AxeSmashPose.AxeLowestVertexRootLocal`；换模型后用菜单 `Tools/测试/抡斧/重测斧刃入地点` 重测。
2. **表现层代码不能放在伤害结算之前**：裂纹生成已挪到伤害之后。
3. **贴图缺失会出白盘**：材质 `_MainTex` 为空 ⇒ Unity 绑默认白贴图 ⇒ 命中点一块白盘（读成“材质丢失变灰模”）。
   ⇒ `GroundCrackEffect.Play` 先判 `sprite == null` 就自毁；贴图到位要设**两处**：预制体 Sprite ＋ 材质 `_MainTex`。
4. **斧身闪白不能用 peak=1**：`lerp(c.rgb, white, 1)` ⇒ 整把斧变纯白剪影（读成材质丢失）⇒ 重武器用 peak 0.35。
5. **`alphaIsTransparency = true` 会毁掉这类 PNG 的 alpha**（GPU 采样里整体变 1）⇒ 用 `false` + `alphaSource = FromInput`。
6. **控制台刷屏会伪装成“卡死”**：`FillUpDiag.Enabled` 默认 true ⇒ 每次排阵重排逐行打完整堆栈（一次死亡 287 条 ≈ 4000 行）；Locus 的 `unity.connection.backgroundHook` 在 Tuanjie 分支找不到原生符号 ⇒ 每帧一条 warning（全文 8831 条）。
   ⇒ 已把 `FillUpDiag.Enabled` 默认改 false、`unity.connection.backgroundHook` 设为 false（**后者需 domain reload 生效**）。
7. **相机组件参数必须落盘进场景资产**：否则 domain reload 后回退默认值（`CameraFeedbackController` 在 Battle 场景里 `enabled=false`）。
8. **别重走的旧结论**：全局 timeScale 时停（没对比就没顿感）、大震屏（像地震）、只慢放武器（读成枪变慢）——见 `design/guard-break-presentation-overhaul.md` §1.4。

### 0.5 关键文件
| 用途 | 路径 |
|---|---|
| 表现参数与三帧（单一来源） | `Assets/Scripts/Core/AxeSmashPose.cs` |
| 技能执行 | `Assets/Scripts/Core/ActiveSkillRunner.cs`（`RunAxeSmash`） |
| 斧头 Prefab（含 Tip/Tail/BladeMid 锚点、拖尾） | `Assets/Prefabs/SkillWeapons/Axe3D.prefab` |
| 裂纹特效 Prefab / 脚本 / shader | `Assets/Prefabs/Effects/AxeGroundCrack.prefab`、`Assets/Scripts/Effects/GroundCrackEffect.cs`、`Assets/Shaders/GroundCrackReveal.shader` |
| 技能配置（数值/武器/特效） | `Assets/ScriptableObjects/ActiveSkills/ActiveSkill_AxeSmash.asset` |
| 调姿工具 | `Assets/Scripts/Editor/AxePoseTuningTool.cs`（菜单 `Tools/测试/抡斧/位姿对照图`、`动画分解图`、`重测斧刃入地点`） |
| 需求文档 | 武器 `design/weapon-3d-art-spec-axe-and-chain.md`；地裂 `design/axe-ground-crack-vfx-spec-20261115.md`；力量感 `design/axe-impact-presentation-spec-20261116.md` |

> ⚠ 场景遗留：`Battle.scene` 里有三把**已停用**的探针斧 `AxePose_Start/Apex/End`（美术摆位原值，留作记录）。
> **进包前删掉**（inactive 也会被打进包体）。靶场场景 `Assets/Scenes/Dev/AxePoseRange.unity` 已按用户指令删除。


## 1. 落地清单

| 资产 | 路径 | 说明 |
|---|---|---|
| 模型 | `Assets/Models/Weapons/BattleAxe/BattleAxe.fbx` | 来自 Blender 修复版；**不带材质**（需求书 §6）；导入设置：`globalScale=3.2112`（模型长 3.2）、`bakeAxisConversion=true`、`materialImportMode=None` |
| 贴图 | `Assets/Models/Weapons/BattleAxe/Textures/BattleAxe_BaseColor.png` | 2048² RGBA，sRGB；**alpha = UV 三角形覆盖遮罩**（淡出材质靠 `clip(tex.a * _Color.a)` 裁剪，没有 alpha 会淡出成一整块） |
| 材质 | `Assets/Models/Weapons/BattleAxe/BattleAxe.mat` | `Custom/WeaponColorUnlit` + `_MainTex`（`_FlashStrength` 通道由 shader 提供） |
| 淡出材质 | `Assets/Models/Weapons/BattleAxe/BattleAxe_Fade.mat` | `Custom/WeaponColorUnlitFade` + 同一张贴图 |
| Prefab | `Assets/Prefabs/SkillWeapons/Axe3D.prefab` | 需求书 §8 的结构 |
| 资产接线 | `Assets/ScriptableObjects/ActiveSkills/ActiveSkill_AxeSmash.asset` | `axeSmashLevels[0..2].weaponPrefab = Axe3D`（三个等级同一把，需求书 §1.1 说 L1/L2/L3 可以分别配） |

**Prefab 结构**（与需求书 §8 / FantasySpear3D 同构）

```
Axe3D                       ← Transform(1) + MeshWeaponVisual + WeaponTasselDynamics
 ├─ Model                   ← FBX 实例；localRotation(90,0,0)（Z-up→Y-up）、localScale 321.12
 ├─ Tip                     ← 刃尖锚点（(0.56, 2.72, -0.11)）
 └─ Tail                    ← 柄尾锚点（(0.21, 0.00, -0.01)）
```

- `MeshWeaponVisual`：renderers=[Model 的 MeshRenderer]、fadeMaterial=BattleAxe_Fade、modelRoot=Model、tipAnchor=Tip、tailAnchor=Tail、dynamics=本体。
- `WeaponTasselDynamics`：weaponTip/weaponTail 指向 Tip/Tail ⇒ `MeshWeaponVisual.Length` 有效（不加它 Length 会退化为 0）。
  bones 为空 ⇒ 求解器 `Initialize()` 直接返回 false ⇒ 不做任何红缨模拟，**安全**（斧头没有红缨）。
- 实测世界尺寸：全长 **3.20**、刃宽 **1.12**、厚 **0.36**；网格 3552 顶点 / 3730 三角面 / 单材质单 submesh
  （需求书 §6 预算：顶点 ≤4000、三角面 ≤8000、单 draw call ✅）。

## 2. 代码改动（一处，技能编排）

`Assets/Scripts/Core/ActiveSkillRunner.cs` → `RunAxeSmash`：

| 项 | 旧 | 新 | 原因 |
|---|---|---|---|
| 缩放 | `localScale = 0.45` | `localScale = 0.55` | 0.45 是「拿长枪当斧头」的占位值（长枪 10.8×0.45≈4.86）。实测玩家在 **z≈-5.4**，离主相机只有 4.6 单位 ⇒ 竖屏可视宽仅 ~2.6、可视高 ~5.3。需求书 §4.2 的「3.2 ≈ 屏高 28%」是按 z≈0 算的，在玩家深度会变成 **60% 屏高**、抡起时斧尖出画。0.55 ⇒ 屏上约 1.76（§4.2 也写了「宁可略小也不要大，过大会挡住战场读数」） |
| 抡起位 | `pivot+( -0.4, 2.6, -1.6)`，`Euler(35,0,0)` | `pivot+(-0.30, 1.45, -0.20)`，`Euler(18,0,-10)` | 旧位姿整把斧飞出画面顶部（只看得见柄尾倒钩） |
| 劈下位 | `pivot+(0.2,-1.2,1.4)`，`Euler(-70,0,0)` | `pivot+(-0.25, 1.30, 0.20)`，`Euler(0,0,-58)` | 绕 **X** 轴旋转＝把斧头甩进屏幕里（透视压缩成一条边，读不出来）。斧刃平面本来朝着相机，**绕 Z 轴（相机轴）在屏幕平面里扫**才读得出「劈下去」；左右扫都会出画（可视宽只有 2.6），所以选向右下扫并把握把左移 |
| 插值 | `Slerp(Euler(35,0,0), Euler(-70,0,0))` | `Slerp(raiseRot, smashRot)` | 跟随上面的位姿常量 |

> 位姿是**离屏实拍**挑的：主相机参数 `(0,3,-10)` / fov 60 / 竖屏 489×979，用 CommandBuffer 直接把网格画进主相机的 RenderTexture
> （不建场景对象，不弄脏用户的 Battle 场景）。对比图在 `Library/Locus/tmp/axe-unity-check/`（`g1_raise.png` / `g3_smash.png` 是最终采用的两张）。

## 3. 与需求书的偏差（已知、未擅自扩权）

| 需求书条款 | 现状 | 说明 |
|---|---|---|
| §6 贴图：1024×1024 PNG+Alpha、**一套 0..1 UV 覆盖整把武器、不要 UV 壳** | ❌ 未做 | 现在仍是 Tripo 2048² 图集（多岛布局）。alpha 已按 UV 覆盖生成 ⇒ 淡出正常。要满足 §6 需重做「单投影 + 烘焙」，是下一件事（会改动 UV 布局，需重新出图） |
| §6 法线图 | 暂未接 | 工程 shader `Custom/WeaponColorUnlit` 没有法线通道；法线图仍留在 `SkillWeaponsArtSource/04_Model/axe_normal_2048_steel.png` 备查 |
| §4.2 全长 3.2 | ✅ 模型本体是 3.2 | 但**屏上**按 0.55 缩放（见 §2 原因）。若希望屏上就是 3.2，把 `RunAxeSmash` 里的 0.55 改回 1 即可，但会挡战场读数 |
| §4.3 +Y＝刃口方向、原点在握持点 | ✅ | Model 子节点 rot(90,0,0) 完成 Z-up→Y-up；原点在柄尾（y=0），刃尖在 +Y |
| §4.2 刃宽 1.28 / 斧背 0.22 | ⚠ 1.12 / 0.36 | 模型的真实比例如此（刃略窄、略厚）。属建模侧问题，本轮只做接入未改形 |

## 4. 验证

- 六视角逐像素比对（Blender 侧）：除顶盖区域外与用户修复版原样 **0 像素差异**（见 `plan/axe-model-texture-fix-20261115.md` §8）。
- Unity 侧回读：Prefab 组件/字段全对、材质 shader 与贴图对、`_FlashStrength` 通道存在、网格 3552v/3730t/1 submesh、技能三个等级都指到 Axe3D。
- 离屏实拍：`g1_raise.png` / `g3_smash.png`（美术效果以用户进游戏观察为准）。
- 编译通过；Unity 控制台无 error（只有既有 warning）。

## 5. 测试装配（2026-11-15 追加）
抡斧是**主动技能**（`ActiveSkill_AxeSmash`），`gestureId` 为空 ⇒ 只能由 `ActiveSkillInventory.TryActivate` 触发
（技能栏点图标 / 三选一拿到后点）；而它**不在 `ActiveSkillPoolConfig` 的任何池里** ⇒ 正常流程拿不到。

**场景初始装配（已写入 Battle 场景并保存）**
Battle 场景的 `ActiveSkillInventory` 用了它自带的「默认技能（开局赠送，**调试用**）」字段：
`_grantDefaultSkills = true` + `_defaultSkills = [ActiveSkill_AxeSmash]`。
战斗开始（`StageState.InProgress`）时自动补上 ⇒ 每局开局就带抡斧劈砍 Lv1，技能栏出现图标，点它即可抡。
实测：`EnsureDefaultSkills()` 后库存 = 1 条（`active_axe_smash` Lv1）。

⚠ **副作用**：教学关那一步「释放技能」用的是 `inv.Entries[0]`（见 `TutorialFlowDriver`）⇒ 现在那一步会变成释放抡斧。
不想要就用菜单一键还原（见下）。

**编辑器测试工具**（`Assets/Scripts/Editor/AxeSkillTestRig.cs`，在 `Locus.EditorTools` 程序集 ⇒ 仅编辑器、不进包）

| 菜单 | 作用 |
|---|---|
| `Tools/测试/抡斧/授予到主动技能栏` | Play 中立刻塞进库存（不进初始装配） |
| `Tools/测试/抡斧/立即释放一次` | 不进库存，直接演一次抡斧（绕开冷却） |
| `Tools/测试/抡斧/写入场景初始装配` | 把初始装配写回当前场景 |
| `Tools/测试/抡斧/清除场景初始装配` | 还原（教学关当初就是关掉这个开关的） |

## 6. 等用户观察的点

### 6.1 劈砍动作重做（2026-11-15 第二轮，用户反馈后）

用户实际看到的效果有三个问题：**① 劈砍动作完全不正确；② 斧头不知道跑哪去了（要对第一排造成伤害）；③ 斧头比玩家的长枪还小**。
排查后确认是同一个根因链：上一版位姿是在**空场景**里离屏调的（没把玩家精灵与敌人摆进画面），
而且**玩家深度读错了**（误把长枪 Prefab 里存着的 z=-5.4 当成玩家位置，实际玩家在 z≈0）。

**实测基准**（相机 (0,3,-10) rot18 fov60，竖屏 489×979）

| 东西 | 世界坐标 | 屏幕坐标 |
|---|---|---|
| 玩家精灵（ThornArmorVisual） | (0, −1.54, −1.20) | (245, 628) |
| 第一排敌人 transform（rowIndex=0） | (0, 0.14, −1.00) | — |
| 第一排敌人胸口（落点） | (0, 0.94, −1.60) | **(244, 427)** |

**修法（四条）**

1. **去掉 0.55 的缩放**：模型本体已按需求书 §4.2 做成全长 3.2，在玩家深度上就是屏高 ≈28%（需求书自己的数）。
   之前那个 0.55 是基于错误的玩家深度加的，把斧头压成 1.76（屏高 15%）⇒ 比长枪小一大截。
2. **落点从数据算**：`落点 = 命中目标里 rowIndex 最小的那一排的 transform + 胸口高 0.8 − 往相机 0.6`。
   排位 z = `Enemies` 根节点 + (maxRows−1−row)×(−rowSpacing)，**不写死常量** ⇒ rangeRows / 阵型预设变化都跟着走。
3. **位姿改由 Prefab 锚点反解**（新文件 `Assets/Scripts/Core/AxeSmashPose.cs`，运行时与调姿工具共用）：
   - 命中时**刃尖锚点必须落在落点**：`握把 = 落点 − R·(tipLocal·缩放)`，`R` 由「刃尖局部方位角 → 期望砍击方位角」求出。
   - 模型朝向以后怎么改都不用重调参数。
   - ⚠ 同时修正了 Prefab 的锚点：`Tip` 原来放在“X 最大的顶点”（月牙最宽处，不是刃尖），现改为**长度轴远端** (−0.08, 3.20, −0.01)；`Tail` = 柄尾 (0.21, 0, −0.01)。
4. **节奏补齐**：抡起 0.16s（蓄力）→ 劈下 0.12s（ease-in，越接近命中越快）→ **命中帧结算伤害** → 命中停顿 0.06s → 收势 0.10s（顺着砍的方向再压下去）→ 销毁。
   旧版是命中后直接 `Destroy(axe)`（斧头凭空消失）。

**验证**（`Tools/测试/抡斧/位姿对照图`，红框=落点、绿十字=刃尖锚点）

| 位姿 | 刃尖世界坐标 | 刃尖屏幕坐标 | 刃口指向 |
|---|---|---|---|
| 抬斧 | (0.55, 2.39, −1.60) | (301, 281) | 竖直微右（举在右肩上方） |
| 落斧 | (0.00, 0.94, −1.60) | **(245, 427) = 落点** | 前下偏左（砸在目标胸口） |

对照图：`Library/Locus/tmp/axe-unity-check/pose_sheet.png`。

### 6.2 第二轮重做（用户反馈：没先抡起来 / 斧正面朝玩家 / 太小）

**先查根因**（对照 `SkillWeaponsArtSource/01_Reference/pbz/juxiongfu_axe.png` 官方立绘与逐帧实测）：

| 用户反馈 | 根因 |
|---|---|
| ① 应该先把斧头抡起来再砸下去 | 上一版的“抡起”姿态只是**斧头竖着站在目标左边**（逐帧图 1~3 帧），不是蓄力后拉；而且**全程只绕 Z 轴转**，读出来是“转了个方向”而不是“抡圆” |
| ② 斧正面朝玩家 | 同上：只用了绕 Z 的旋转 ⇒ 刃面永远垂直于相机，渲出来就是一张 2D 贴片。PBZ 官方立绘是 **3/4 侧角**（能看到斧背厚度、顶面与侧棱） |
| ③ 太小 | 斧刃宽 **1.12** 单位，而第一排敌人精灵高 **1.9** 单位 ⇒ 刃口只有敌人身高的 59%；长枪 10.8 单位（≈屏高 90%）。PBZ 立绘里月牙占整斧长度 44%（我们 35%） |

**另外查出一个自审方法上的错**：调姿工具把合成图**上下颠倒了**（RT 的 `ReadPixels` 第 0 行已是图底，我又多翻了一次），
所以前几轮“看着不对”的目视自审全部建立在倒图上 ⇒ 后面改成**以数值（刃尖屏幕坐标/落点坐标）+ 参照框**判读，图像只作辅助。

**改法（`Assets/Scripts/Core/AxeSmashPose.cs`）**

先算清这个镜头的硬约束（相机 (0,3,-10) 下俯 18°，竖屏 489×979；玩家精灵中心 screen (245,628)；第一排 (245,427)）：

- **1 单位 ≈ 96 px**（玩家深度可视高 10.16 单位）。
- **玩家到第一排只有 201 px**，而模型全长 3.2 单位（307 px）**比这段距离还长**
  ⇒ “玩家双手握斧、刃口正好落在第一排”在几何上**不可能**；除非缩到 ~0.75 倍（那时刃宽只有敌人的 44%，就是“太小了”）。
  ⇒ 结论：它只能是**目标排上方的武器表现**（握把可以出画，观众看刃口）。
- **刃宽预算**：敌人精灵高 1.9 单位（182 px）⇒ 刃宽取敌人的 70~80% = 1.3~1.5 单位 ⇒ **缩放 1.2~1.35**。

| 项 | 旧 | 新 |
|---|---|---|
| 缩放 | 1.75（刃宽 1.96 = 敌人 103%，荒谬） | **1.28**（全长 4.1、刃宽 **1.43 = 敌人 75%**） |
| 刃口指向（命中） | 二维（屏幕平面内） | 三维 `(−0.42,−0.62,−0.66)`：下 + 偏左 + **扎进屏幕纵深** |
| 刃口指向（抡起） | `(0.25,0.93,−0.27)`（几乎竖直） | `(−0.12,0.80,−0.59)`：**同样带足 Z 分量** |
| 偏航 yaw | 无 | 命中 **−30°** / 抡起 **+26°** ⇒ 3/4 侧角，能看见斧背厚度 |
| 抡起落点 | 刃尖贴着落点 | 后拉到目标**正上方略偏后**（screen ≈ (267,304)） |
| 时序 | 0.16 / 0.12 / 0.06 / 0.10 | **0.20 抡起 → 0.13 砸下（ease-in）→ 0.07 命中停顿 → 0.12 收势** |

★ **抡起也必须带 Z 分量**：上一版起手几乎竖直、砸下前下，两个朝向差近 150°，
4.1 单位长的刃尖绕握把扫出 400+ px 的大弧，中途跑到 x=−17 出画 ⇒ 看着像斧头自己飞出去。
改成整段“向前下方扎进敌人那一排”后，刃尖轨迹收紧在画面内。

**刃尖屏幕轨迹（最终）**：抡起 (267,304) → (214,290) → (103,313) → **命中 (244,427) = 第一排敌人胸口**（最大横向摆动 164 px，全程在画面内）。

验证图：`Library/Locus/tmp/axe-unity-check/anim_strip.png`（8 帧分解）、`pose_sheet.png`（抬/落两姿态 × 3 档缩放，
红框=落点、绿十字=刃尖锚点）。调姿工具：`Tools/测试/抡斧/位姿对照图`、`动画分解图`。

**过程教训（写给下一个接手的人）**

1. **对照图要先验证方向**：RT 的 `ReadPixels` 第 0 行已是图底，再翻一次就得倒图；我在倒图上做了三轮目视自审，全是错的。
   ⇒ 现在**以数值判读**（刃尖/落点的屏幕坐标），图像只作辅助。
2. **位姿必须先算再调**：先算“1 单位多少像素”“玩家到目标多远”“刃口该多大”，再照算出来的数写参数；
   不要靠试参数看图。本轮前几版是反过来做的，浪费了大量迭代。
3. **要截图动画帧**：只看两个端点姿态会漏掉“抡起其实只是竖着站”这种问题。

### 6.3 靶场场景（给美术摆位用）—— **已于 2026-11-15 删除，现按需重建**

- 原 `Assets/Scenes/Dev/AxePoseRange.unity` = **Battle 场景的真实副本**（Battle 本身就是 N1 关卡：`Formal_N1_Battle`），
  含全部环境/UI/管理器（BattleHUD、FrontItemBar、Route Flow、教程层、玩家、敌人池…）；副本里把
  `stageConfig` 直接指到 `Formal_N1_Battle`（Battle 里它是运行期由路线注入的，编辑态是空）。
- **现状（2026-11-15 用户指令「现在就删掉」）**：整个 `Assets/Scenes/Dev/` 目录（场景 + .meta，~885 KB）
  已用 `AssetDatabase.DeleteAsset` 删除；它无任何资产引用、从未进 git，删除时把编辑器活动场景切回
  `Assets/Scenes/Battle.scene`（未产生任何场景写入，Battle.scene 仍是 18:39 的版本）。
  ⇒ **交接里「读 AxePoseRange 文件反解三帧」的前提已失效**：文件不存在，三帧必须重新摆。
- **重建配方（要摆位时按这个复刻，~3 分钟）**
  1. Project 窗口复制 `Assets/Scenes/Battle.scene` → 拖到新建目录 `Assets/Scenes/Dev/` → 重命名 `AxePoseRange`。
  2. 把副本里 `StageController.stageConfig` 指到 `Assets/RouteData/ChangbanpoScroll/Battles/Formal_N1_Battle.asset`
     （Battle 副本里编辑态是空 `{fileID: 0}`；这是副本与 Battle 的**唯一**差异，实测确认）。
  3. 摆三把 `Axe3D.prefab` 实例：`AxePose_Start` / `AxePose_Apex` / `AxePose_End`（缩放 1.28）。
  4. 拖 → **Cmd+S 保存** → 下一个对话读该文件、按 Tip/Tail 锚点反解三帧、烘进 `AxeSmashPose`，
     然后**三把探针斧和整个场景文件一起删**（用户口径）。
- 附加参照（重建时可加）：`AxeRef_FirstRow` = z=−1 的红细线（第一排实测位置，玩家精灵本身就是参照）。
- ⚠ 之前做的**运行时键盘调姿器已删除**：编辑器脚本里 `Input.GetKeyDown` 读不到游戏输入（编辑器不转发输入），
  键盘方案不可用，别再走那条路。

### 6.4 其它待观察

1. 抡起/劈下的位姿是否合意（现在只是「能看清」）。
2. 屏上大小 0.55 是否合适（可 0.4~0.8 调）。
3. 扫的方向（现在向右下）是否符合手感预期。
4. 命中反馈（无视盾牌减伤 / 必定破盾 / 削韧）沿用原有逻辑，本次未改。

## 6.5 交接（2026-11-15 末，美术暂停、换对话继续）

**当前状态**

| 项 | 状态 |
|---|---|
| 斧头模型/贴图 | ✅ `SkillWeaponsArtSource/04_Model/`（钢质斧眼盖、UV 修复完毕、六视角逐像素一致） |
| Unity 资产 | ✅ `Assets/Models/Weapons/BattleAxe/`（FBX + 2048² BaseColor/RGBA）+ `Assets/Prefabs/SkillWeapons/Axe3D.prefab`（MeshWeaponVisual + Tip/Tail 锚点 + 淡出材质） |
| 技能接线 | ✅ `ActiveSkill_AxeSmash.asset` 的 `axeSmashLevels[0..2].weaponPrefab = Axe3D` |
| 开局装配 | ✅ Battle 场景 `ActiveSkillInventory._grantDefaultSkills=1` + `_defaultSkills=[AxeSmash]`（git diff 仅 3 行） |
| 劈砍动画 | ✅ **已定稿（2026-11-15）**：美术在 Battle 场景里摆的三帧**原样照搬**进 `AxeSmashPose`（缩放 2），见 §6.7 |
| 美术摆位 | ✅ 三帧已烘进代码；场景里保留三把**已停用**的探针（`AxePose_Start/Apex/End`，位置/旋转原值保留，勾回 active 即可对照） |

**下一步（美术摆完之后的流程）** —— **2026-11-15 已走完，见 §6.7；下面是当时的流程存档**

0. ⚠ **靶场场景已被删除**（用户 2026-11-15 指令）⇒ 摆位前先按 §6.3 的重建配方复刻一份
   `Assets/Scenes/Dev/AxePoseRange.unity`（Battle 副本 + `stageConfig → Formal_N1_Battle`）。
   ★ 实际执行时**没另建副本**：美术直接在 **Battle 场景**里摆（靶场已删），姿态已写回 `Battle.scene` 存盘。
1. 美术在 `AxePoseRange.unity` 里拖/转三把斧 → **Cmd+S 保存该场景**。
2. 开新对话的我：读 `Assets/Scenes/Dev/AxePoseRange.unity` 里三个对象的位置与旋转 →
   按 Tip/Tail 锚点反解每帧的“刃尖朝向 + 握把位置” → 烘进 `AxeSmashPose`（三帧 + 各段时长）→
   删掉场景里的三把斧 **并删掉整个场景文件** → 重出对照图给美术看。
3. 需要的工具：`Assets/Scripts/Editor/AxePoseTuningTool.cs`（菜单 `Tools/测试/抡斧/位姿对照图`、`动画分解图`，
   产出 `Library/Locus/tmp/axe-unity-check/pose_sheet.png` / `anim_strip.png`，红框=落点、绿十字=刃尖）。
4. `Assets/Scripts/Editor/AxeSkillTestRig.cs`（菜单 `Tools/测试/抡斧/…`）：授予技能到主动技能栏 / 立即释放一次 /
   写入或清除场景初始装配。游戏内点技能栏图标触发抡斧。

**不要重走的路**

- ❌ 编辑器脚本里用键盘调参：编辑器不转发输入，`Input.GetKeyDown` 根本不触发（已删该工具）。
- ❌ 自造简化靶场：美术要的是**真场景副本**（按 Battle(N1) 复制，UI/环境/配置都在；配方见 §6.3）。
- ❌ 用截图目视判姿态：对照图曾经是倒的；现在一律以**数值**（刃尖/落点屏幕坐标）判读，
  预算量见 `memory/unity-editor-scene-tweaking-lessons.md`。

**仍未做的需求书项**（`design/weapon-3d-art-spec-axe-and-chain.md`）

- §6 贴图规格：仍是 Tripo 2048² 图集（多岛），**未做** “1024×1024 PNG+Alpha + 一套 0..1 UV 覆盖整把武器 + 不要 UV 壳”。
- §4.2 尺寸表（刃宽 1.28 / 斧背 0.22）：当前模型实测 1.12 / 0.36，且游戏里用了 1.28 的运行时缩放。
- 斧头左侧（左/右不对称结构）未在建模层保证。

### 6.6 武器轴心修正（2026-11-15，用户报「轴心不在武器尾端」）

**定位靠实测，不靠目视。** 三层的原点位置（改前）：

| 层 | 原点实测 | 相对柄尾的关系 |
|---|---|---|
| Blender `.blend` | 对象原点 `(0.0002, 0.0012, 0)`，模型 z∈[0, 0.996]，原点下方 0 个顶点 | 在柄尾**平面**，但横向偏 0.065（柄轴不在 x=0） |
| FBX → Unity 网格 | 网格原点 z∈[−0.032, 0] | 同上 |
| **Prefab 根节点 `Axe3D`** | `(0,0,0)` | 在柄尾平面，**横向偏 0.21**（柄尾中心在 x=+0.21） |
| `Tail` 锚点 | `(0.20978, 0, −0.00939)` = 柄尾中心点 | ✓ 正确 |
| `Tip` 锚点 | `(−0.07828, 3.19994, −0.00939)` = 新月刃上角尖 | ✓ 正确 |

⇒ 场景里被拖/被旋转的那把斧，轴心是**浮在爪尖旁边 0.21 单位**（场景 scale 2 时约一个爪宽），
而不是落在柄尾点上；同时运行时真正的支点（`Tail` 锚点）**在**柄尾 ⇒ 两处不一致。
规格书 §4.3 要求「模型原点 = 握持点 = 握把末端（柄尾）」⇒ 按柄尾对齐。
（用户 2026-11-15 确认目标点＝**柄尾爪端**。）

**改法（三层一起改：源头对齐，不用再靠 Prefab 偏移补偿）**

1. **Blender 源文件** `SkillWeaponsArtSource/04_Model/battle_axe_3d_model_re.blend`
   把对象原点从「世界原点」移到**柄尾截面质心** `(0.069398, −0.001992, 0)`（柄尾平面局部 z<0.0187 的 23 个顶点的截面质心）：
   顶点数据 −C、对象位置 +C ⇒ **世界几何一动没动**（bbox 变化 3.7e-9，纯浮点噪声），只有原点挪了。
   改前原文件备份：`Library/Locus/tmp/axe-pivot-check/backup/`。
2. **FBX 重导**：`battle_axe_3d_model_re.fbx` → 同步到 `Assets/Models/Weapons/BattleAxe/BattleAxe.fbx`。
   导出参数按**实测复刻**：24 组候选在 Blender 里回环比对 + 在 Unity 里比对导入签名，
   最终选定 `axis_up='Y', axis_forward='-Z', global_scale=1.0, apply_unit_scale=True`（米制，只导 `_re` 这一个网格）。
   ⚠ 工程里那份 `Assets/.../BattleAxe.fbx` 与 `04_Model` 的源导出**本来就不是同一个文件**（导出参数不同、字节数不同）⇒ 两边都要换。
3. **Prefab** `Assets/Prefabs/SkillWeapons/Axe3D.prefab`（用 `PrefabUtility` 改，不手改 YAML）：

| 字段 | 改前 | 改后 |
|---|---|---|
| `Model.localPosition` | `(0, 0, 0)` | `(−0.00227, 0, −0.00003)` |
| `Tail.localPosition` | `(0.20978, 0, −0.00939)` | `(0, 0, 0)` |
| `Tip.localPosition` | `(−0.07828, 3.19994, −0.00939)` | `(−0.30340, 3.19994, −0.00303)` |

**结果（实测）**

- 柄尾截面质心 = `(0.00000, 0.00000, 0.00000)` ⇒ **轴心精确落在柄尾爪端**（改前离根原点 0.225）。
- `Tail` 锚点 == 根原点；`Tip` 锚点距刃尖顶点 1e-5。
- 预制局部 AABB：`min(−0.78558, 0, −0.17210) max(0.33534, 3.19994, 0.18484)`（全长 3.2、刃宽 1.121、厚 0.357）。
- 标记图（红=根原点、绿=Tail、蓝=Tip）：`Library/Locus/tmp/axe-pivot-check/final_side_XY.png`、`final_butt_zoom.png`（红与绿重合＝两者都压在爪尖上）。
- 运行时数学**不用改**：`AxeSmashPose` 从实例读 `vis.TipPosition`，锚点挪了它自动跟着走。

⚠ **场景现状（已变）**：靶场场景 `Assets/Scenes/Dev/AxePoseRange.unity` 已于 2026-11-15 按用户指令删除
（删除前磁盘上只剩一把 `AxePose_Start`，世界原点、scale 2、rot 0 —— 原先那三把我算的旧位姿在 19:17 被美术存盘覆盖）。
轴心改了 ⇒ 三帧本来就要重摆（美术自己摆，不代劳），重建配方见 §6.3。

### 6.7 三帧姿态定稿（2026-11-15，美术摆定 → 原样烘焙）

**来源**：美术在 **Battle 场景**里（靶场场景已被删）摆好三把 `Axe3D.prefab` 实例
`AxePose_Start` / `AxePose_Apex` / `AxePose_End`，在 **paused play mode** 里用 `unity_execute`
读出三者的根节点变换与 Tip/Tail 锚点世界坐标（**不退出 play mode**），随后退出 play 并把数值写回场景存盘。

| 帧 | 握把＝柄尾（世界） | 旋转（四元数） | 刃尖（世界） | 刃向 | 离落点 |
|---|---|---|---|---|---|
| Start | (−0.10, −1.00, −5.47) | (0.5697, 0.4570, 0.5095, 0.4550) | (0.22, −2.67, 0.73) | (0.05, −0.26, 0.96) | 4.30 |
| Apex | (−0.60, −0.80, −5.97) | (−0.0682, 0.7149, −0.2110, 0.6631) | (0.63, 5.20, −7.92) | (0.19, 0.93, −0.30) | 7.65 |
| End | (−0.04, −1.20, −5.84) | (0.4862, 0.5135, 0.4069, 0.5783) | (0.05, −0.53, 0.55) | (0.01, 0.10, 0.99) | 2.61 |

**用户决定（2026-11-15）**：位置**原样照搬**，运行时缩放用 **2**（美术摆位时的值）。

**烘焙方式**（`Assets/Scripts/Core/AxeSmashPose.cs` 重写）
- `RuntimeScale` 1.28 → **2**
- 旧的「算出来的」姿态数学（`RaiseAim/ImpactAim/ApexAim` + `RotationFor` + `RaiseGrip/ImpactGrip/ApexGrip`）**整段删除**；
  改为三帧常量：`StartGripPosition/ApexGripPosition/EndGripPosition` + `StartRotation/ApexRotation/EndRotation`。
- ★ 2026-11-16 修正：握把位置改为**美术摆定的世界绝对坐标**（Start (−0.10,−1.00,−5.47) / Apex (−0.60,−0.80,−5.97) / End (−0.04,−1.20,−5.84)）。
  本技能是**对排的范围伤害**，伤害按排结算（每个敌人用**自己**的位置算命中点），
  武器表现**不锁任何敌人的位置** ⇒ 不再挂到第一排胸口。
- 时序（五段，2026-11-15 按「蓄势 + 沉重」重做，**位置/旋转未动，只改节奏**）：
  | 段 | 时长 | 缓动 | 读感 |
  |---|---|---|---|
  | ① 蓄势 | **0.50s** | 手 smoothstep / **旋转慢 20%** | 慢起手，斧头拖在后面＝重 |
  | ② 绷住 | **0.30s** | 静止 | 最高点停一拍＝张力（“蓄势”的那一拍） |
  | ③ 劈下 | **0.50s** | 手 t²（峰值速度落在命中帧）/ **旋转 t^1.5** | 刃口甩在前面＝有劲 |
  | ④ 顿帧 | 0.22s | 静止 | ≈6.6 帧（PBZ 交击顿帧 2~6 帧的上沿） |
  | ⑤ 渐变消失 | **0.45s** | 原地 alpha 1→0（InQuad） | **End 就是最终位置**：不再往前/往下沉，斧头停住后淡出（`MeshWeaponVisual.SetAlpha` ⇒ 自动切到 `BattleAxe_Fade` 透明材质）。play 实测：SetAlpha(0.5) ⇒ 材质切淡出、renderQueue 3000、`_Color.a=0.50`；SetAlpha(0) ⇒ shader `clip(a*_Color.a)` 完全不可见 ✔ |

  ⚠ 2026-11-16 用户口径修正：之前那版“收势”会把握把沿刃向**再推 0.7 单位**并沿抡斧轴**多转 26°** ⇒ 斧头在 End 之后继续往地里钻（读成“往下沉”），
  而且结束时是 `Destroy` 硬切消失。⇒ 现在 End 之后**只淡出、不再有位移**，并删掉了 `FollowGrip/FollowRot/FollowDistance/FollowExtraDegrees`。
  ⇒ 命中帧 t=1.30s，总时长 1.97s（末版按用户指定定数：0.5 / 0.3 / 0.5）。收势沿刃向再推 1.2 单位、沿**抡斧自身旋转轴**再转 26°。

### 6.8 PBZ《影之刃零》巨凶斧调研（2026-11-15：整个劈砍要多久 / 在哪蓄势 / 怎么表现力度）

**来源**：B站影之刃WIKI「巨凶斧」词条（wiki.biligame.com/yingzhiren/巨凶斧）；
项目已有 `design/pbz-reference-analysis-20261109.md`（网络调研）、`design/guard-break-presentation-overhaul.md`（演出语法）。

**① 这把武器没有“轻点一下就劈”的招——它本来就是蓄力武器。**
- 巨凶斧＝影之武装（技能组），**破坏型**：伤害 309~1205、破防 208~811；官方描述“斧身厚重，一击可开山裂甲”。
- 三个升级形态全围绕蓄力：**强行蓄力**（连按两次重击 → 耗 1 暗源 → **直接满蓄力斩击**）/
  **厚甲护身**（蓄力中受伤减免 三级递进）/ **疾行斧**（闪避后、奔跑中也能蓄力）；铸魂**急速蓄力**＝蓄力速度 +30%。
- ⇒ 存在“跳过蓄力直达满蓄”的机制 ⇒ **蓄力本就有可观时间成本**；蓄力中会挨打 ⇒ 蓄力是**有风险的博弈态**。
- ⇒ **结论：整个劈砍动作的主体在蓄力段，释放段反而短**（慢蓄 + 狠砸，不是砍菜节奏）。

**② 蓄势在哪**：命中之前的**独立、可被打断的一段**；PBZ 的满蓄力 payoff 做成 **“凌空一劈”**（25 级“千钧之势”：奔跑中蓄满 → 释放一记空中重斧）。
本项目对应：第 ② 段“绷住 0.30s”（站定、绷住、蓄满）。

**③ 力度怎么表现**（项目自己从 PBZ 完美格挡/闪避拆出的演出语法）：
**判定点 → 顿帧 → 闪白 → 音效 → 镜头 → 奖励**
- 顿帧＝交击处**局部**定格 **2~6 帧**；PBZ 的完美闪避升级为**子弹时间**（整幅慢下来 + 去色 + 镜头推近）。
- 本项目已踩的坑：**顿帧落错对象**（落在自己枪上 ⇒ 读成“我的动作卡了一下”）＋**没有命中确认** ⇒ 画面上没有任何东西在说“打中了”。
- 对应：顿帧 0.22s≈6.6 帧（区间上沿，**不宜再加长**，重量主要落在敌人那侧）；命中确认已有
  （敌人局部 Hit Stop `feedbackStrength: Heavy` ＋ 闪白 ＋ 相机震动 `heavyOnlyCameraShake`）。

**④ 仍缺的（玩法层，不是动画层）**：PBZ 的蓄力是**“按住不放、松手才砍”**（玩家自己控制何时释放）；
我们是一次点击演完整套 ⇒ 命中要等 1.30s，手感偏“迟”。
若要对齐 PBZ，需把抡斧改成**蓄力态**（可被打断、蓄满再放）——属玩法改动，待用户拍板。

**代码改动**
- `Assets/Scripts/Core/AxeSmashPose.cs`：按上述重写（公开 API 全换：`RaiseGrip/ImpactGrip/FollowGrip(…,scale)` 等全部换名换参）。
- `Assets/Scripts/Core/ActiveSkillRunner.cs`（`RunAxeSmash`）：改为 Start→Apex→End 三段插值 + 新收势。
- `Assets/Scripts/Editor/AxePoseTuningTool.cs`：改用新 API；对照图改成**三帧 × 两档缩放（2 / 1.28）**；
  顺手修了旧 bug（RT 还挂在 `Camera.targetTexture` 上就 Release ⇒ 控制台刷 error）。

**验证**
- 运行时逐项复现美术的数值：Start grip (−0.10,−1.00,−5.47)、Apex (−0.60,−0.80,−5.97)、End (−0.04,−1.20,−5.84)，刃尖世界坐标全部一致 ✅
- 对照图 `Library/Locus/tmp/axe-unity-check/pose_sheet.png`（三帧×两档缩放）、`anim_strip.png`（8 帧分解）✅
- 编译通过，控制台 0 error ✅

⚠ **美术知情的取舍（2026-11-15 确认）**：这三帧**刃尖全程不碰敌人**（End 帧离落点 2.61 单位），
斧身在**相机前约 4.5 单位**处 ⇒ 一把斧就能盖满屏；Apex 帧刃尖出画（屏上 y = −1300 @489×979）。
这是“要斧在近景抡”的观感选择，伤害仍由技能逻辑（无视盾减伤/必定破盾/削韧）结算。
若之后要改成“刃口落在敌人胸口”：只改 `EndGripOffset` 一个向量（沿刃向平移 ≈ 2.6 单位）即可。

⚠ **场景遗留**：Battle 场景里保留三把**已停用**（active=false）的探针斧
`AxePose_Start/Apex/End`（位置/旋转原值，随时可勾回对照）；**进包前要删**。

### 6.9 力度优化（2026-11-15 三轮：蓄势震动→取消 / 下劈加速 / 命中感）

**用户三轮反馈**：
① 「蓄势没有震动，下劈不够狠」→ 做了抖动 + 换曲线；
② 「还是别蓄势了（不要抖动），做个**渐慢的到位**表达蓄力」→ **抖动全部删除**；
③ 「下劈还是不够快」＋「打击命中感很差，没有重武器命中的感觉」→ 劈下时长砍半 + 命中链补齐。

**最终形态（位置/旋转始终是美术摆的原值，未动）**

| 段 | 时长 | 缓动 | 说明 |
|---|---|---|---|
| ① 蓄势 | 0.50s | **ease-out**（1−(1−t)^2.5）/ 旋转滞后 18% | **到位前的后半段明显变慢**（用户指定：用减速到位表达蓄力，不抖动） |
| ② 绷住 | 0.30s | 静止 | 最高点停一拍＝张力 |
| ③ 劈下 | **0.22s**（原 0.50） | 位置 0.25t+0.75t³ / 旋转 0.15t+0.85t³ | 出手即有速度，末端猛加速；最后 20% 时间走完 42% 路程 |
| ④ 顿帧 | 0.12s | 静止 | ≈3.6 帧（重量主要落在敌人那侧：敌人 Heavy 卡肉 0.14s） |
| ⑤ 收势 | 0.45s | ease-out | 沿刃向推 1.2 单位、沿抡斧自身旋转轴再转 26° |

⇒ 命中帧 t=**1.02s**（原 1.30s），总时长 1.59s。
起手 ease-out 的实测进度：t=0.2→0.43、0.4→0.72、0.6→0.90、0.8→0.98（后半段明显变慢 ✓）。

**命中感补齐（PBZ 演出语法逐条落地）**

| 语法步骤 | 落地 |
|---|---|
| 判定点 | 命中帧 = 劈下段末帧 |
| **顿帧** | 斧头自身 0.12s（≈3.6 帧）；敌人局部卡肉 0.14s（HeavyHitStopDuration） |
| **闪白** | 命中帧 `axeVisual.PlayFlash(0.08f)`（斧身） |
| **音效** | **新增**：挥空音（金属风声 `enemy_swing_metal_whoosh.wav`）+ 命中音（`slash_hit1/2.wav`），走 `AxeSmashLevelConfig.swingClip/impactClip`（Inspector 可追踪，不靠 Resources） |
| **镜头** | **新增** `CameraFeedbackController.RequestImpactPush(1.6°, hold 0.22s)`（从格挡推近里抽出的通用方法，格挡那条改为调它） |
| 奖励 | 伤害/破盾/削韧沿用原逻辑 |

另修：伤害调用补上 `impactPosition: impactPoint`（敌人胸口）⇒ 像素特效/伤害数字不再落在敌人脚底。

⚠ **未解决的**：
1. **玩家本体没有抡斧动作** ⇒ 力度只能来自武器运动＋命中反馈（PBZ 的力度来自角色身体）——最大结构性差距。
2. ~~**没有刀光拖尾**~~ → **已做**（见 §6.11）。

### 6.10 力量感优化（2026-11-16，按项目自存的 PBZ 演出语法逐条对）

依据：`design/guard-break-presentation-overhaul.md` §1.2 的 PBZ「成功瞬间」**六层语法**
（①判定点 → ②顿帧 → ③闪白 → ④音效 → ⑤镜头 → ⑥奖励）与 §1.4「已试过并被否掉的方向」。

| 层 | 抡斧现状 |
|---|---|
| ① 判定点 | ✅ 命中帧 = 劈下段末帧（t=1.02s） |
| ② 顿帧 | ✅ 斧头停 0.12s（≈3.6 帧，PBZ 区间 2~6 帧）；**敌人那侧**另有 Heavy 卡肉 0.14s |
| ③ 闪白 | ✅ 敌人命中闪白 ＋ 斧身**轻微**高光（peak 0.35，不能用全白剪影） |
| ④ 音效 | ✅ **两层**：极短金属瞬态（`slash_hit1/2`）＋ 低频闷响（新增 `AudioManager.PlayHeavyThud()`，复用 Launch 命中音） |
| ⑤ 镜头 | ✅ 极短推近 `RequestImpactPush(1.6°, 0.22s)`（不是大震屏） |
| ⑥ 奖励 | ❌ 玩法层，未做 |

**新增：斧刃刀光拖尾**（视觉上最直接的一层）
- 预制体 `Axe3D` 挂 `MeshWeaponTrail`：锚点 = **Tip（刃尖）＋ BladeMid（刃根中段，局部 (−0.20, 2.55, 0)）** ⇒ 两条拖尾勾出斧刃弧线
- 材质 `Assets/Models/Weapons/BattleAxe/BattleAxe_Trail.mat`（`Custom/WeaponTrailAdditive` ＋ 软渐变贴图，暖白金）
- 参数：宽度系数 0.075（斧刃比枪宽）、trailSeconds 0.10、速度阈值 10~60（劈下段刃尖均速 ≈29 单位/s ⇒ 会出拖尾）
- play 实测：两条 TrailRenderer `emitting=True`、宽度 0.241、实机可见金色弧线 ✔

⚠ **没做的（要美术/玩法拍板）**：地裂贴图（等贴图）、命中尘土/碎屑精灵、敌人被击退（会改阵型，不擅自加）。

### 6.11 砸地裂纹特效（2026-11-16）

**需求**见 **`design/axe-ground-crack-vfx-spec-20261115.md`**（纯功能需求：触发/落点/行为/规格/契约/接线/验收）。
**工程侧已完成的脚手架**（贴图可替换）：

| 件 | 路径 | 状态 |
|---|---|---|
| shader | `Assets/Shaders/GroundCrackReveal.shader` | ✅ unlit + `_Reveal` 径向显现 + `_Fade` 淡出（径向显现已离屏验证 4 段） |
| 脚本 | `Assets/Scripts/Effects/GroundCrackEffect.cs` | ✅ `Play(worldPos, worldDiameter)` + DOTween 三段 + 自毁 + `GameplayPause.Bind` |
| 材质 | `Assets/Sprites/EffectSprites/axeCrack/AxeGroundCrack.mat` | ✅ |
| 预制体 | `Assets/Prefabs/Effects/AxeGroundCrack.prefab` | ✅ 根 + `Crack`(SpriteRenderer, sortingOrder 14) |
| 接线 | `ActiveSkill_AxeSmash.asset` | ✅ `effectPrefab` → 预制体（3 个等级） |
| 代码 | `ActiveSkillRunner.RunAxeSmash` 命中帧 | ✅ 生成；落点＝斧 mesh 最低点投影到 y=0（`AxeLowestRootLocal()` 缓存，O(1)，随姿态自动跟随） |

**生成记录（2026-11-16）**：曾用 `POST /v1/images/generations`（纯文生图、不带参考图）
生成过一张 1024×1024 PNG 并入库，**已按用户指示删除**（该图未携带项目画风约束，需重做）。
留档：`C:/Users/steam/Pictures/gptGen/axe_ground_crack_20261116/`。
提交脚本：`Tools/visual_brief/submit_axe_ground_crack_20261116.py`（`--prompt <路径>` 指定提示词文件；
**提示词文件已删，下一个执行者自写**）。
⇒ **不要再用本轮消耗过的额度反复试**；贴图由别的执行者按需求文档产出，落到
`Assets/Sprites/EffectSprites/axeCrack/AxeGroundCrack.png`，再赋给预制体的 `Crack` 子物体（pivot 不动）。

⚠ **Unity 导入坑（已修）**：`alphaIsTransparency = true`（RGB 膨胀）会让该类 PNG 的 alpha 在 GPU 采样里整体变成 1
（离屏实测：自定义 shader 与 `Sprites/Default` 均为 100% 不透明）。⇒ 本贴图必须 **`alphaIsTransparency = false`** +
`alphaSource = FromInput`；关掉后自定义 shader 30.6% 不透明 vs `Sprites/Default` 32.7% ✔ 一致。

⚠ **两个 2026-11-16 修掉的运行时 bug（都是本轮新增引入的）**
1. **不能运行时读斧 mesh 的顶点**：斧 FBX 的导入设置 `isReadable = false` ⇒ 运行时 `mesh.vertices` 抛
   “Not allowed to access vertices …”，**协程直接中断**（连带后面的伤害结算不执行、裂纹不生成、斧头永不销毁）。
   ⇒ 已改为用编辑态量好的常量 `AxeSmashPose.AxeLowestVertexRootLocal`（斧根局部空间 (−0.01534, 0, −0.00302)）；
   换/重导模型后用菜单 `Tools/测试/抡斧/重测斧刃入地点` 重新量并回填。
   ★ 另：**裂纹生成已挪到伤害结算之后**（表现层异常绝不能挡住伤害）。
2. **裂纹贴图未到位时不能出白盘**：材质 `_MainTex` 为空 ⇒ Unity 绑定默认白贴图 ⇒ `_Reveal` 升上去时
   命中点出现一块**白盘**（看着像“武器材质丢失变灰模”）。
   ⇒ `GroundCrackEffect.Play` 先判 `sprite == null` ⇒ 关渲染器 + 自毁（play 模式实测：对象已自毁 ✔）。
   ★ 贴图到位时要设**两处**：预制体 `Crack` 子物体的 Sprite ＋ 材质的 `_MainTex`。

### 6.12 先做动画概念分镜（2026-11-16）

**用户决定**：最终裂纹贴图生成前，先在别处生成一张动画表现概念图，用于确认裂纹形状、径向扩散和淡出读感；本阶段不生成最终透明 PNG，不修改 Unity 资产。

**概念图形式**：一张 1536×1024 的 3×2 六格分镜板。六格固定使用同一暖黄褐战场地面、同一固定斜俯视 2.5D 镜头、同一裂纹轮廓和同一尺度；只改变显现范围与整体透明度。阅读顺序为左到右、上到下：

| 格 | 时间参考 | 表现 |
|---|---:|---|
| 1 | 0.00s | 裂纹完全不可见 |
| 2 | 0.03s | 从中心快速向外显现，约到一半半径 |
| 3 | 0.16s | 裂纹完全显现 |
| 4 | 0.28s | 与第 3 格一致，短暂保持 |
| 5 | 0.42s | 同一裂纹整体淡出，但仍可辨认 |
| 6 | 0.50s | 完全消失 |

**视觉方向**：裂纹服务于“厚重冷蓝灰钢制单刃巨斧砸入暖黄褐战场土地”的物理冲击感，但概念板不显示斧头本体。中心最密、最深，约 6–8 条不规则粗裂纹向外分叉，少量短次级分支，外缘稀疏并渐隐；使用清晰的大色簇和受控阶梯状像素边缘。裂纹主体为深冷灰褐土石色，辅以克制的灰白碎石高光；不做魔法、火焰或能量效果。

**参考图职责**：

- `C:/Users/Administrator/Downloads/微信图片_20260928101136_13_455.png`、`C:/Users/Administrator/Downloads/微信图片_20260928101137_14_455.png`：只定义战斗世界、战场地面和特效气质；
- `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle1.png`：只定义高清像素色簇、硬边和细节组织；
- `SkillWeaponsArtSource/03_FourView/axe_four_view_plain.png`：只提供当前巨斧的厚重单刃形体印象，不复制武器本体或白底。

**生成约束**：`gpt-image-2.6-sunburst`、`quality=max`、`input_fidelity=high`、PNG、1536×1024。概念板可使用土面背景，不要求透明，且只能用于评审，不能直接作为最终贴图。负面约束包括：规则蜘蛛网、完整圆环、闪电、火焰、魔法符文、发光法阵、蓝色能量、软糊光晕、均匀噪点、尘云、火星、冲击波、镜头运动、角色、武器、文字、UI、白底和透明棋盘格。

**下一步**：在别处按本节方案提交一次概念板生成；先由用户验收动画表现，再决定是否进入最终 `Assets/Sprites/EffectSprites/axeCrack/AxeGroundCrack.png` 透明贴图生成。概念板生成前仍需遵守提示词和参数审批，不得把概念板直接导入 Unity。

### 6.13 概念板执行记录（2026-11-16）

**提交**：`Tools/visual_brief/submit_axe_crack_storyboard_20261116.py --approve`
（提示词唯一真源：`Tools/visual_brief/prompts/axe_crack_storyboard_v1.txt` / `.cn.txt` / `.negative.txt`；
参考图清单：`Tools/visual_brief/prompts/refs_axe_crack_storyboard_v1.json`）。

| 项 | 值 |
|---|---|
| 接口 / 模型 | `POST /v1/images/edits` / `gpt-image-2.6-sunburst` |
| 参数 | `size=1536x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1` / 不传 `background`（概念板用土面背景） |
| `image[]`（4 张，6.14 MB） | ① 实机截图 ② 实机截图 ③ 1011 已验收精灵 ④ 抡斧四视图；职责逐张写进提示词，并交代“实机截图带战斗界面，只取世界与地面” |
| 结果 | HTTP 200，182.22 s，1536×1024 **RGB**（color type 2，无 alpha），2,436,477 bytes，sha256 `8dfd3b6b…4b85`，usage total 8834 |
| 归档 | `SkillWeaponsArtSource/05_VfxConcept/axe_crack_storyboard_v1.*`（图 / 中英提示词 / negative / request / status / `.panels.json` 量测） |

**六格客观校验**（中心 64% 区域按亮度阈值统计，基线＝第 1 格干净地面）：

| 格 | 时间 | 设计意图 | 实测 |
|---|---|---|---|
| 1 | 0.00s | 裂纹不可见 | 暗部像素 18（= 基线）✔ |
| 2 | 0.03s | 显现到一半 | 暗部 1712 / 全显 3640 ⇒ **≈47% 半径**，不透明 ✔ |
| 3 | 0.16s | 完全显现 | 暗部 3640 ✔ |
| 4 | 0.28s | 保持 | 暗部 3690（= 第 3 格 +1.4%）✔ |
| 5 | 0.42s | 淡出但仍可辨 | 暗部归零（裂纹变亮），但 <130 像素 18246 vs 干净基线 9282 ⇒ **仍可辨认** ✔ |
| 6 | 0.50s | 完全消失 | 回到干净基线（<70=14 / <100=981）✔ |

其它：六格地面/岩石/木栅栏/光照/镜头完全一致（暗缝实测列 508–516、1020–128，行 507–516）；裂纹为冷灰褐土石色 + 少量灰白碎石高光；**无斧头、无角色、无文字/UI、无魔法火焰**；渲染为大色簇 + 阶梯状像素边缘，与工程画风一致。

**V1 状态（历史，已被 §6.14 后续反馈更新）**：概念板已出图；用户随后反馈冲击力不够，不应直接进入最终透明贴图。⚠ 概念板**不得导入 Unity**。

### 6.14 新一轮冲击语言对照方案（2026-11-16）

**用户反馈**：上一版返图“冲击力不大够”。复盘后问题不是单纯裂纹数量不足，而是视觉语义偏成“树根状星形裂纹”：中心没有明显受力破碎区，裂纹过细且均匀，缺少下压、崩裂、掀起的层次；六格主要展示显现范围变化，没有展示命中瞬间的视觉峰值。

**下一轮建议：不再直接生成六格显现板**。先生成一张 2×2 四格“命中峰值”对照板，每格都是独立的最终冲击瞬间，用来比较地面破坏语言；四格不负责展示时间变化，也不直接作为最终贴图。推荐画板构图：1536×1024、2×2，近距离地面裁切，冲击区域占每格约 55%–65%，背景简化，地面与镜头保持一致。此为待审方案，用户尚未确认四种方向中的最终选型。

| 方向 | 视觉核心 | 取舍 |
|---|---|---|
| A 压陷型（首选） | 中心 20%–25% 形成不规则破碎压陷坑，周围 4–6 块土石被压碎或略微掀起，粗裂纹从坑缘向外爆开 | 最符合当前巨斧的重量，最终也最容易在竖屏战斗画面中读懂 |
| B 斧刃劈裂型 | 中心形成短而深的单刃主劈口，裂纹明显不对称并沿劈砍方向延伸，两侧分叉长度不同 | 最能说明“这是斧头砸出的”，但要避免变成一道刀光或地面切线 |
| C 爆裂型 | 中心破碎核高对比爆开，6–8 条粗裂纹快速冲向外圈，中段有断裂错位，少量土石向外弹出 | 冲击最大，适合作为峰值参考；不应把所有碎屑都塞入最终单张裂纹贴图 |
| D 板块错裂型 | 中心地面被砸成 3–5 块厚重错开的土石板，断层边缘清楚，裂纹少但更粗 | 实体重量强，但需防止变成陨石坑、岩浆坑或魔法法阵 |

**推荐合成方向**：以 A 为主体，吸收 B 的一条明确主劈口，少量吸收 C 的中心碎屑；D 只作为对照，不直接作为最终形态。目标是“中心先被重物压碎，主劈口留下方向，粗裂纹随后向外爆开，边缘迅速衰减”，而不是“中心出现一颗均匀放射的黑色星形裂纹”。

**新一轮视觉约束**：

- 中心必须有可辨认的压陷/破碎核，不是几条细线的交点；
- 主裂纹约 5–7 条，粗细和长度有明显层级，避免平均放射；
- 主劈口应有轻微方向性和不对称性，体现单刃斧，不做规则六向星形；
- 可加入少量中心土块、碎石或断层亮边，表现实体受力；不加入大面积尘云、火星、冲击波或魔法光效；
- 地面背景简化，不能让场景石块、木桩和纹理抢过裂纹主体；
- 仍使用暖黄褐战场土、深冷灰褐裂口、少量灰白土石受光边缘和高清像素硬边大色簇；
- 禁止树根状星形裂纹、细线地面划痕、完整蜘蛛网、完整圆环、闪电、火焰、岩浆、符文、法阵、蓝色能量、柔光雾、均匀噪点、陨石坑、角色、斧头本体、文字和 UI。

**后续动画分镜（建议，不是现有实现）**：四格对照选定主方向后，再做第二张时间分镜；建议时间点为 `0.00s` 干净地面、`0.02s` 中心压陷/破碎核先出现、`0.06s` 主裂纹高速冲出、`0.16s` 达到峰值、`0.28s` 保持、`0.40s` 碎屑和边缘消散、`0.50s` 完全消失。当前 `Assets/Shaders/GroundCrackReveal.shader` 与 `Assets/Scripts/Effects/GroundCrackEffect.cs` 是**单张贴图的中心向外径向遮罩 + 统一淡出**：能够中心先显、外圈后显，但不能独立制作瞬时爆核、会动的碎屑或分别淡出。若用户最终选择这些表现，需另行讨论和批准地裂、尘土/碎屑、可选冲击环的分层及工程改动；这些仅是可能方案，并非已批准新增素材或承诺实现。功能规格仍以 `design/axe-ground-crack-vfx-spec-20261115.md` 为准。

**参考图职责和可追溯文件**：

- V1 返图 `SkillWeaponsArtSource/05_VfxConcept/axe_crack_storyboard_v1.png` 仅供复盘其不足，不是已验收的造型目标；原生成档在 `C:/Users/steam/Pictures/gptGen/axe_crack_storyboard_20261116/`。V1 参考清单在 `Tools/visual_brief/prompts/refs_axe_crack_storyboard_v1.json`。
- 两张项目概念图 `C:/Users/Administrator/Downloads/微信图片_20260928101136_13_455.png`、`C:/Users/Administrator/Downloads/微信图片_20260928101137_14_455.png` 负责新画风、战场和战斗氛围；已验收 `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle1.png` 负责像素色簇和边缘组织。旧实机地面截图只用于了解当前落位和对比度，不反向定义新画风。
- `Assets/Prefabs/SkillWeapons/Axe3D.prefab` 是当前使用的 3D 斧；`SkillWeaponsArtSource/03_FourView/axe_four_view_plain.png` 只供理解其厚重单刃形体，不画武器本体或复制白底。四视图并非实时截图。

**上一轮讨论中的中文提示词核心（供别处整理成完整稿后审批）**：

> 一张高清像素化三国战场动作游戏的地面重斧命中冲击概念对照板，2×2 四格，四格分别展示四种不同的地面破坏语言：A 压陷型，中心形成不规则破碎压陷坑并向外产生粗裂纹；B 斧刃劈裂型，中心形成短而深的单刃劈口，裂纹明显不对称并沿劈砍方向延伸；C 爆裂型，中心破碎核瞬间爆开，粗裂纹和少量土石向外冲出；D 板块错裂型，中心地面被砸成几块厚重错开的土石板，断面清楚可见。所有格子都表现同一暖黄褐战场地面、固定斜俯视 2.5D 镜头、近距离特效展示。强调厚重冷蓝灰钢制单刃巨斧造成的物理冲击，但画面中不出现斧头。高清像素色簇，硬边阶梯轮廓，中心高对比，深色裂口，少量灰白土石受光边缘，强烈重量感，清楚可读。

**负面词建议**：树根状星形裂纹，均匀六向放射，细线裂纹，普通地面划痕，完整蜘蛛网，完整圆环，魔法阵，符文，火焰，岩浆，雷电，蓝色能量，柔光，雾团，过多尘雾，过多碎片，陨石坑，爆炸火球，角色，武器，UI，文字，标签，完整战场远景，背景细节抢主体。

**生成前状态**：本节只记录方案，尚未生成。用户将在别处发起；提交前仍需展示完整中英文提示词、负面词、逐张参考图职责和参数，取得明确批准（已有的 V1 批准不自动适用于新版）。建议沿用 `gpt-image-2.6-sunburst / quality=max / input_fidelity=high / 1536×1024 / PNG / n=1`；这是带土面背景的概念对照板，不要求透明。新对照板仅供评审，不得导入 Unity；最终贴图仍须另行验收后才能落到 `Assets/Sprites/EffectSprites/axeCrack/AxeGroundCrack.png`。

### 6.15 回退到项目战斗特效语言（2026-11-16）

**V2 返图诊断**：`C:/Users/steam/Pictures/gptGen/axe_impact_language_board_20261116/axe_impact_language_board_v2.png` 已查看。虽然使用了像素块，但整体偏成“等距视角 3D 地形破坏概念图”：有厚重峡谷、竖直断层面、陨石坑式压陷、错开的岩板和过多地面碎石；四格像独立环境概念图，不像能叠在当前战斗地面上的 2.5D VFX。根因是 `crushed pit`、`thick slabs`、`broken fault surfaces`、`tilted earth and rock` 等描述把模型引向地质断层，而不是战斗特效贴图。

**方向结论**：下一轮不再做“地面被掀成几块厚岩板”的破坏语言对照，也不再让模型自由设计三维地形。应改为**项目战斗特效叠加概念板**：用当前战场地面作为底，只比较可平铺、可叠加的 2D/2.5D 扁平特效层。目标不是写实地面被挖开，而是“一枚扁平、硬边、像素化的重斧命中特效贴在战场地面上”。

**概念板形式**：一张 1536×1024、3 列×2 行六格板。六格使用同一块当前项目暖黄褐战场地面、同一固定近距离斜俯视 2.5D 镜头、同一特效中心和同一尺寸；不展示时间序列，而是比较六种战斗 VFX 叠加组合。每格特效占画面约 35%–50%，保持能放进实际约 3.0 世界单位的地面范围；背景地面简化但仍需保留项目实机的暖土色、像素纹理和明度关系。

| 格 | 叠加方向 | 目的 |
|---|---|---|
| 1 | 纯深色粗裂纹 | 确认扁平裂纹本体：5–7 条有层级的冷灰褐粗裂纹，轻微斧刃方向，不均匀放射，不是树根星形 |
| 2 | 裂纹 + 金白中心命中核 | 确认重击的瞬间焦点；中心短促、集中、硬边，不做圆形光球 |
| 3 | 裂纹 + 金白命中核 + 朱红短促边缘 | 对齐项目斩击/命中特效的金白与朱红战斗色关系，红色只作强调，不铺满 |
| 4 | 裂纹 + 少量碎土像素 | 增加实体反作用；碎土必须是少量扁平像素块，不做 3D 岩石和悬浮石板 |
| 5 | 裂纹 + 斧刃方向拖痕 | 确认单刃斧的攻击方向；拖痕是短、粗、硬边的地面擦裂，不是长刀光或完整圆弧 |
| 6 | 推荐综合版 | 以第 1 格裂纹为底，吸收第 2/3 格的中心命中核、第 4 格少量碎土和第 5 格轻微方向拖痕；作为最终视觉候选，但仍只是概念图 |

**六格共通硬约束**：

- 只画 2D/2.5D 地面叠加效果；不得出现任何竖直断层面、坑壁、峡谷、岩板、厚土块、地形切片或 3D 岩石结构；
- 不改变地面相机、地面纹理、光照、特效中心和特效尺寸；
- 中心必须是扁平的高对比命中区域，而不是立体坑洞；
- 裂纹是 5–7 条粗硬主裂纹加少量短分支，粗细/长度有层级，整体略有单刃方向，不平均放射；
- 金白色用于短促命中核心和少量受光边缘，朱红用于少量速度/冲击强调；不得使用整块白光、霓虹或魔法发光；
- 使用项目的高清像素化大色簇、硬边阶梯轮廓和清楚焦点；不要写实材质、柔和渐变、空气刷光晕、体积光或均匀噪点；
- 背景不得出现角色、斧头、敌人、UI、文字、标签、场景远景或抢主体的木桩石块；
- 概念板不是最终透明贴图，也不是六帧序列；仅用于决定最终扁平特效的视觉层级。

**参考图职责**：

- `C:/Users/Administrator/Downloads/微信图片_20260928101136_13_455.png`：只作为项目战斗特效、金白/朱红命中特效和竖屏战斗构图的最高优先参考；它含有 HUD 和角色，生成时必须明确“只取战斗特效色彩与像素化世界语言，不复制 HUD、人物或武器”。
- `C:/Users/Administrator/Downloads/微信图片_20260928101137_14_455.png`：只作为三国战场暖土、空间氛围和像素化世界的补充参考，不生成其中的建筑、军阵或远景。
- `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle1.png`：只定义高清像素色簇、硬边和局部细节组织，不复制角色。
- `SkillWeaponsArtSource/03_FourView/axe_four_view_plain.png`：只提示当前武器是厚重单刃斧，用于决定裂纹的轻微方向性；不画斧头，不使用白底，不把四视图当作画风参考。
- 当前实机地面截图若作为第五张参考，只定义实际地面颜色和对比度，不定义新画风；不能让生成模型自由重做地面。

**建议中英文提示词必须强调的核心句**：

> 这是当前项目战斗画面上的 2D/2.5D 地面叠加特效设计，不是环境概念图，不是地形破坏概念图，不是 3D 岩石断层。所有效果都必须像一张扁平 Sprite 贴在暖黄褐战场地面上，使用项目现有的高清像素化战斗特效语言：金白色短促命中核心、少量朱红强调、深冷灰褐粗裂纹、硬边大色簇和阶梯状轮廓。

English anchor sentence:

> This is a 2D/2.5D ground-overlay VFX design for the existing in-game combat screen, not an environment concept, not terrain destruction, and not a 3D geological fracture. Every effect must read as a flat sprite layered on the warm ochre battlefield ground, using the project's existing high-definition pixel combat language: a short gold-white impact core, restrained vermilion accents, thick cool gray-brown cracks, hard-edged colour clusters, and stepped silhouettes.

**新负面约束**：3D terrain destruction, geological fault, canyon, crater, deep pit, vertical cliff walls, raised slabs, tilted rock plates, thick broken ground chunks, isometric environment art, realistic rocks, photorealism, painterly concept art, volumetric lighting, soft glow, fog, dust cloud, smoke, fireball, lava, lightning, magic circle, runes, blue energy, tree-root star crack, evenly radial spokes, hairline scratches, complete ring, character, axe, weapon, enemy, UI, text, labels, background scenery, wide battlefield vista.

**工程边界**：当前 `Assets/Shaders/GroundCrackReveal.shader` 与 `Assets/Scripts/Effects/GroundCrackEffect.cs` 只支持单张裂纹 Sprite 的中心向外径向显现和整体淡出。六格中的金白核心、朱红边缘、碎土和拖痕只是为了评审视觉层级；若最终选择综合版，是否拆为多个 Sprite/材质层、是否新增冲击环或碎屑动画，必须在概念验收后另行拍板，不能把概念板直接当成实现承诺。

**生成前状态**：本节只记录新版方案，尚未生成。用户将在别处发起；已有 V1/V2 生成审批不自动适用于本方案。提交前需重新展示完整中文/英文提示词、负面词、参考图职责及参数，并取得明确批准。建议参数：`gpt-image-2.6-sunburst / quality=max / input_fidelity=high / 1536×1024 / PNG / n=1`；该概念板使用当前地面背景，不要求透明。概念板仅供评审，不得导入 Unity；最终贴图仍需另行验收后才能落到 `Assets/Sprites/EffectSprites/axeCrack/AxeGroundCrack.png`。

### 6.16 战斗特效叠加概念板 v3 执行记录（2026-11-16）

**提交**：`Tools/visual_brief/submit_axe_combat_vfx_overlay_board_20261116.py --approve`
（提示词唯一真源：`Tools/visual_brief/prompts/axe_combat_vfx_overlay_board_v3.txt` / `.cn.txt` / `.negative.txt`；
参考图清单：`Tools/visual_brief/prompts/refs_axe_combat_vfx_overlay_board_v3.json`）。

| 项 | 值 |
|---|---|
| 接口 / 模型 | `POST /v1/images/edits` / `gpt-image-2.6-sunburst` |
| 参数 | `size=1536x1024` / `quality=max` / `input_fidelity=high` / `output_format=png` / `n=1` / 不传 `background` |
| `image[]` | 5 张，1.86 MB（两张项目概念图缩小副本 + 1011 精灵 + 抡斧四视图缩小副本 + 实机地面裁切） |
| 结果 | **第 1 次提交即 HTTP 200**，112.26 s，1536×1024 RGB，2,510,663 bytes，sha256 `670adb56…eee8`，usage total 9896（无 503/504） |
| 归档 | `SkillWeaponsArtSource/05_VfxConcept/axe_combat_vfx_overlay_board_v3.*`（图 / 中英提示词 / negative / request / status / `.measure.json`） |

**六格局客观量测**（竖缝实测 x=510–513 / 1022–1024，横缝 y=511–513）：

| 格 | 效果覆盖 | 裂纹像素 | 金白核心像素 | 饱和朱红 | 12 扇区中的有效方向数 |
|---|---:|---:|---:|---:|---:|
| 1 纯粗裂纹 | 15.1% | 10.6% | 0.01% | 0.00% | 12（最强扇区 100%、最弱 21%） |
| 2 +金白命中核 | 15.3% | 9.4% | 1.02%（中心 2.1%） | 0.00% | 12 |
| 3 +朱红强调 | 17.0% | 10.8% | 0.97% | **2.03%** | 12 |
| 4 +碎土像素 | 19.4% | 12.9% | 1.01% | 1.09% | 12 |
| 5 +斧刃拖痕 | 20.8% | 14.2% | 1.01% | 0.93% | 12 |
| 6 综合版 | 21.3% | 14.9% | 1.03% | 1.17% | 12 |

- **六格地面中位色完全一致**（约 205/146/72）⇒ 同一地面、同一光照、同一纹理 ✔
- 效果覆盖从 15.1% 递增到 21.3%、裂纹像素从 10.6% 递增到 14.9% ⇒ **逐格叠加关系成立** ✔
- 金白核心只在第 2–6 格出现、占画面约 1%、中心区约 2% ⇒ **短促集中，不是大块白斑** ✔
- 饱和朱红第 1、2 格为 0、第 3 格 2.03% ⇒ **红只作强调** ✔（但第 4、5 格也带了 1% 左右，是小偏差）
- **V2 的失败模式已消除**：六格全是**平铺在地面上的扁平图层**，无竖直断面、坑壁、峡谷、岩板、3D 岩石 ✔
- **仍偏树根状**：12 个角度扇区全部有裂纹质量（虽然有一个主导方向，峰谷比 2.5–8×），即"不平均放射"只做到一半
- **其它偏差**：金白核心是**放射状星芒火花**（像本游戏的命中火花，芒线偏多偏亮）；第 4 格的碎土像带阴影的小石子（略偏 3D）；第 5 格的拖痕读成褐色泥抹痕而非硬边擦裂；特效占幅目测 45%–60%，略超需求的 35%–50%

**状态**：v3 概念板已出图，**等用户评审视觉层级**（是否采纳第 6 格综合版）。⚠ 本板子**只用于评审，不得导入 Unity**；最终透明贴图仍未生成。
⚠ 若最终采纳"金白核心 + 朱红强调 + 碎土 + 拖痕"多层叠加，现有 `GroundCrackReveal.shader` / `GroundCrackEffect.cs`（单张 Sprite 径向显现 + 统一淡出）**不支持分层**，需在概念验收后另行拍板是否拆多层 Sprite/材质。

### 6.14a 冲击语言对照板执行记录（历史，2026-11-16）

**提交**：`Tools/visual_brief/submit_axe_impact_language_board_20261116.py --approve`
（提示词唯一真源：`Tools/visual_brief/prompts/axe_impact_language_board_v2.txt` / `.cn.txt` / `.negative.txt`；
参考图清单：`Tools/visual_brief/prompts/refs_axe_impact_language_board_v2.json`）。

**五次尝试（提示词全程未改）**：

| 次 | 结果 | request id | 载荷 |
|---|---|---|---:|
| 1 | HTTP 503 upstream_unavailable | `20261116064647127370305wOO5KSYQ` | 6.61 MB |
| 2 | HTTP 504 upstream_timeout | `202611160650334442423328dxr2tJ4` | 6.61 MB |
| 3 | HTTP 503 upstream_unavailable | `202611160654344656332833FTGXbyt` | 1.86 MB（已降体积） |
| 4 | HTTP 503 upstream_unavailable | `20261116065830105525208gxH3gMxM` | 1.86 MB |
| 5 | ✅ **HTTP 200**，104.21 s | — | 1.86 MB |

⇒ **503/504 是上游路由故障，不是内容拦截**（同期免费探针 `GET /v1/models` 200，网关活着）；**降体积并不能解决**，第 3、4 次已是 1.86 MB 仍 503 ⇒ 有效做法是**原样重发 + 间隔等待**（第 4 次与第 5 次之间等 5 分钟即成功）。参考图缩小副本保存在 `SkillWeaponsArtSource/05_VfxConcept/refs/`（原图未改动）。

**产物**：`SkillWeaponsArtSource/05_VfxConcept/axe_impact_language_board_v2.png`（1536×1024 RGB，sha256 `277a4d45…3e99`，2,845,312 bytes）
+ 同名 `.prompt.txt` / `.prompt_cn.txt` / `.negative.txt` / `.request.json` / `.status.json` / `.panels.json`（四格量测）。

**四格量测**（暗缝实测：列 766–769、行 510–513）：

| 格 | 方向 | 中心破碎核暗像素占比 | 冲击区占比 | 地面中位色 |
|---|---|---:|---:|---|
| A | 压陷型 | 0.391 | 37% | (205,144,73) |
| B | 斧刃劈裂型 | 0.340 | 25% | (211,150,77) |
| C | 爆裂型 | 0.369 | 31% | (212,150,77) |
| D | 板块错裂型 | 0.439 | 36% | (209,148,78) |

- 四格地面色一致、外圈干净（面板 A 四角暗像素 0.9%–6.3%）✔
- 中心均为**实心破碎核**（0.34–0.44），不再是细线交点 ✔ ⇒ 相对 V1 的树根状星形裂纹，重量感明显提升
- 坑缘/断口有受光亮边；无魔法、火焰、尘雾、角色、武器、文字/UI ✔
- **偏差**：A、B 两格被画成**较深的沟槽/峡谷**（壁面很高），更像采石沟而非压陷坑；冲击区占比 25%–37%，低于需求书的 55%–65%（部分因抬起土板顶面接近地面色而被统计低估）

**状态（历史）**：四格对照板已出图，但用户反馈其整体偏离当前项目画风，生成成 3D 地形断层/沟槽/岩板，而不是可叠加在战斗地面上的扁平 2D/2.5D VFX；不得再按 A/B/C/D 选择方向，也不得作为最终贴图造型依据。新版方案以 §6.15 为准：先做 3×2 扁平战斗特效叠加概念板。
⚠ 本板子**只用于历史复盘，不得导入 Unity**；最终贴图仍未生成。


## 7. 相关文档

- 需求书：`design/weapon-3d-art-spec-axe-and-chain.md`
- 贴图修复过程：`plan/axe-model-texture-fix-20261115.md`
- 四视图（美术验收版）：`SkillWeaponsArtSource/03_FourView/`
- Blender 工作流与崩溃防线：`plan/blender-mcp-verified-workflow.md`
