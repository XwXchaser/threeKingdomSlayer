---
id: kd_09197577-c608-4d1a-aac1-d052c85c4868
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 可破坏补给（BreakableProp）重构方案（2026-10-24）

> 状态：**批次 A / B / C 已完成并实测通过（2026-10-24）**；随后按用户要求做了部署调整（见 §8）。
> 已由用户拍板 4 条决策（见 §2）。本文是执行文档，不是已批准设计的全部历史。
> 起因：落地页/路线里正前方永远钉着一个木箱，打碎后残骸还跟着摄像头跑。

---

## 1. 根因（实测，非推测）

### 1.1 Y 场景是「着色器滚动」的世界

- 相机全程固定在 Battle 场景的 `(0,3,-10)`；`ScrollRoutePresenter` 明确不改相机坐标。
- 真正的滚动在顶点着色器里：`Assets/Experiments/CurvedScroll/Shaders/YScrollScenery.shader:11-19`
  用 `_YSPose`（旅行点）+ `_YSToLocal/_YSToWorld` 把物件沿路线重新投影，并按 `anchor.z` 做淡入淡出与远景下沉。
- 结果：**世界在动，相机不动**。任何不使用滚动 shader 的物件都会被钉在相机正前方。

### 1.2 木箱是唯一的「非滚动 + 永驻」物件

实测 `YRouteStreamingWindow.alwaysResidentRoots` 里的可见渲染器：

| 常驻组 | 渲染器 | 材质 | 结论 |
|---|---|---|---|
| `Terrain - one mesh…` | 1 | `J1_Ground_Branches` | 正常滚动 |
| `Route Nodes - N1 E0 J1…` | 57（45 可见） | `YSampleScenery_E0Route` | 正常滚动 |
| `Encounter Authoring` | **1（只有这只箱子）** | **`Sprites-Default`** | **不滚动、不被距离窗口裁掉 → 永久钉在屏幕正中 (540,829)** |

### 1.3 残骸「跟着镜头跑」的两层原因

1. `BreakableProp.PlayBreakFrames` 结束只把 `spriteRenderer.enabled=false`（`Assets/Scripts/Prop/BreakableProp.cs:240`），对象仍在原点；
2. `BreakableProp` 与 `YRouteStreamingWindow` **同时写同一个 `renderer.enabled`**（窗口按距离写、物件按破坏态写），谁后写谁赢 → 离开再进入窗口范围时会被窗口「复活」。

### 1.4 功能层面的既有缺陷（实测）

| 编号 | 问题 | 证据 |
|---|---|---|
| P1 | 命中只看列，不看距离/节点/可交互性 → 任何一场战斗中列空挥即可远程打碎 N1 的箱子并 +100 铜钱 | `AttackSystem.cs:373-374` → `BreakableProp.cs:161-172` |
| P2 | 提示是常驻金色 `_Color` 脉冲，不区分可交互；方案 §6 要求的描边没做 | `BreakableProp.cs:105-118`；prefab 材质是 `Sprites-Default` |
| P3 | 破坏序列接地点不齐：基图 pivot 在可见底边（65px），6 张破坏帧 pivot 在格底上方 112px 而内容底边在 44px → 破坏第一帧下沉 0.34 世界单位、末帧浮空 0.17 | 导入设置 + PNG alpha 包围盒实测 |
| P4 | 破坏后直接消失，末帧残骸从未被看到；方案 §5.4 写的是「淡出销毁」 | `BreakableProp.cs:240` |
| P5 | 存档只覆盖路线快照（`FakeRouteStageSaveSnapshot.propStates`），落地页破坏时 `NotifyPropDestroyed` 因 `routeConfig==null` 直接 return → 钱进账、状态没存 | `FakeRouteRuntime.Checkpoint.cs:63-64,74-81` |
| P6 | `columnOverride: 2` 写死，挪位后与实际列脱节 | prefab 字段 |
| P7 | 摆位压中线、材质与景物不一致、不在表驱动管线内 | 场景实测：其它 N1 摆件 `\|x\|≥2.65`，箱子 `x=0` |

---

## 2. 已定决策（用户 2026-10-24）

1. **局外生成**：先本地模拟，预留接口（`ISupplySpawnSchedule` + 本地实现 + 可插 HTTP）。
2. **Tap 点中物件**：照常出招、吃冷却，走完整 Stab 表现，只是伤害目标换成物件。
3. **滚动方式**：C# 跟随锚点（复刻滚动投影，transform 保持真实），不用滚动材质。
4. **局内外关系**：同一套物件定义（`SupplyPropDefinition`），靠「层」区分奖励与存档。

### 2.1 本文采用的默认值（未拍板但影响小，可随时改）

- 残骸：局内层**保留**（作为物件最终状态帧，随路线滚动、离开 105m 自动隐藏）；局外层**不保留**（直接消失）。
- 局外物件有效期：跟生成轮次绑定，过期消失。
- 局外奖励：存档金币（`SaveManager.AddCoins`），数量由定义配（可配随机区间）。
- 局内补给：本轮仍是 N1 固定一只，但位置与登记方式按表驱动规则走。
- 落地页（就绪态）只显示**局外层**物件；局内层物件属于关卡态。

---

## 3. 目标架构（三层模型）

```
层1 摆放层
  SupplyPropDefinition (ScriptableObject, Locus.Data)
      美术/破坏序列/耐久/掉落表/点击盒/外观尺寸/所属模式(局内|局外)
  SupplyPropInstance (MonoBehaviour, Locus.Combat)
      位置/朝向/实例 id/轮次/当前状态/所属层
  ISupplySpawnSchedule (Locus.Services) + LocalSupplySpawnSchedule
      生成计划：点位/物件定义/轮次/有效期；本地时钟推进轮次，可插 HTTP 实现
层2 表现层
  RoutePropFollower (Locus.Experiments)
      复刻滚动投影 → transform 真实；**独占 renderer.enabled**（远距隐藏）
  残骸/破坏序列/描边提示（EnemyOutline.shader）
层3 交互与结算层
  SupplyPropHitResolver  点中本体（Tap 落点 → 点击盒）
  SupplyPropRewardPolicy 局内( PlayerState 局内资源 ) / 局外( SaveManager 存档资源 )
  持久化：局内 → 路线快照 propStates；局外 → 存档分片（claimed spawnIds）
```

**职责边界（必须守住的规矩）**
- `renderer.enabled` **只有一个所有者**：带 follower 的物件由 follower 管；`YRouteStreamingWindow` 跳过这类渲染器。
- 物件的「破坏态」只改**显示哪张图**，不改 enabled。
- 滚动投影的数学**只有一处实现**（follower 的 C#），shader 版本保持原样；两者差异记录在本文件。

---

## 4. 分批实施

### 批次 A：画面与归属（纯表现，可独立回退，不动玩法语义）—— ✅ 已完成

| 编号 | 改动 | 文件 | 实测结果 |
|---|---|---|---|
| A1 | 新增 `RoutePropFollower`：复刻 shader 的锚点投影（`q = anchor.xz - pose.xz` → 按 heading 旋转 → `TransformPoint`），并按投影后的 `anchor.z` 决定远距隐藏 | 新建 `Assets/Experiments/CurvedScroll/Scripts/RoutePropFollower.cs` | ✅ progress 0 → 物件 worldZ=4.5；progress 0.05（+16m）→ projectedZ=-11.8（相机身后）；progress ≥0.1 → 自动隐藏 |
| A2 | `YRouteStreamingWindow` 跳过带 follower 的渲染器（消除双写 `renderer.enabled`） | `YRouteStreamingWindow.cs` | ✅ 实测窗口 tracked=573，`containsCrate=False`（不再抢开关） |
| A3 | `BreakableProp` 停止写 `renderer.enabled`；破坏后切残骸帧并保留 | `Assets/Scripts/Prop/BreakableProp.cs` | ✅ 打碎后残骸帧保留、随路线滚动；`hideWhenDestroyed` 开关留给局外 policy |
| A4 | 提示改描边（`Custom/EnemyOutline`），只在玩家靠近时脉动；去掉常驻金色 `_Color` 脉冲 | 同上 + 新建 `Authoring/YSamplePropOutline.mat` + prefab 材质 | ✅ 落地页可见金色描边；`hintVisibleDistance=16` 之外不脉动 |
| A5 | 箱子挪到 N1 右侧路边 | `YJunctionSample.unity` | ✅ 世界 `(3.0,-1.8,3.6)`、屏幕 x=907（路中间空了）；实测该位置与周围摆件无重叠 |
| A6 | 6 张破坏帧 pivot 改为各自内容底边 | atlas `.meta`（TextureImporter API） | ✅ 整段序列内容底边恒为 y=-1.800（原先第一帧下沉 0.343、末帧浮空 0.17） |
| A7 | 顺带止血：`TryHitColumn` 加距离门槛（`maxInteractDistance=20`） | `BreakableProp.cs` | ✅ 在别的节点空挥不再远程打碎 N1 的箱子 |

**A 批的两处设计修正（实测后改）**
- **材质不能用景物滚动材质**：既然选了 C# 跟随锚点（决策 3），物件位置已由 CPU 投影算好，再用 `YSampleScenery_E0Route` 会**叠加两次滚动**。所以物件用「描边材质（无滚动）」，滚动只由跟随器负责。
- **尺寸不用改**：原以为世界高 2.05 超标（那是 SpriteRenderer 的 quad 高，含透明边）；实测**可见**箱高 = 855px/100×0.2 = **1.71**，本就在 1.6~1.8 区间，scale 保持 0.2。
- 写 pivot 必须 `EditorUtility.SetDirty(importer)` 后再 `SaveAndReimport()`，否则不落盘（第一次改完运行时实测仍是旧值 0.21875）。

**验收口径（已全部实测）**
- ✅ 落地页截图里正前方没有木箱（截图：`Library/Locus/Screenshots/locus_game_20261024_161010_391.png`）
- ✅ 走到 E0/J1 时箱子在身后且不可见；返回时按距离窗口淡入淡出，不再永远钉在屏幕正中
- ✅ 打碎后残骸随路线一起走，**不跟镜头**；离开 105m 隐藏，返回时仍是残骸
- ✅ 破坏序列播放时箱体不跳、不陷、不浮
- ✅ Console 0 error / 0 warning


### 批次 B：交互改造（点中本体 + 全时点 + 分层奖励）—— ✅ 已完成

- B1 `GestureInput` 增加触点屏幕坐标（`screenPos`）与 `supplyTarget`（`ISupplyPropTarget`）。
- B2 `InputManager.ProcessTapGesture` 先做命中解析：`BreakableProp.TryHitAtScreenPoint` 把落点投影到与物件同深的平面上算世界距离（分辨率无关），命中则带上该物件。
- B3 删掉 `AttackSystem.cs:373-374` 的「按列连带打碎」钩子（`TryHitColumn` 标为 Obsolete）。
- B4 奖励分层：`SupplyLayer.InRun` → `PlayerState` 局内铜钱 + 路线快照；`SupplyLayer.OutOfRun` → `SaveManager` 存档金币，**不进路线快照**。
- B5 高亮：描边走 `Custom/EnemyOutline`（已换材质），只在可交互时脉动。
- B6 层的门禁：`BreakableProp.HubState`（由 `FakeRouteRuntime` 在「落地页就绪 / 开始关卡」时切换）——就绪态只可交互局外层，关卡态只可交互局内层。

**实测**
- 点在物件上 → 命中该物件（coins 750 → 900，+150 = 该点位的奖励）✅；点在空处 → 不命中 ✅。
- 层切换：就绪态 → 局外可交互 / 局内不可交互；关卡态 → 反之 ✅。

**两个实现期修正**
- **距离门槛必须以玩家为基准**（不能用相机）：战斗相机在玩家身后约 10m，用相机算会把路边的箱子（离玩家 10~12m）判成“太远”，根本点不到。已改为「到玩家的水平距离」（局内 16m / 提示 14m）。
- **落地页没有战斗状态**：`TryExecuteAttack` 在 `stageState != InProgress` 时直接返回 false → 局外物件在主菜单上点了没反应。已加兜底：**不在战斗态且招式没执行时，直接把这一下交给物件**；局内若被冷却挡住则不生效（保持“照常出招、吃冷却”）。

### 批次 C：局外生成（服务器倒计时，本地模拟）—— ✅ 已完成

- C1 `ISupplySpawnSchedule`（服务层契约）+ `LocalSupplySpawnSchedule`（本地模拟，墙钟当“服务器时间”）。
- C2 `SupplySpawnPlan`（数据层资产）：**固定点位**（位置 / 轮内偏移 / 存活时长 / 局外金币 / 预制体），避免与场景既有摆件撞车。
- C3 领取去重：`SupplySaveSection`（存档分片）记录已领取的实例 id（轮次+点位），同一实例不重复发。
- C4 局外流程：命中 → 存档金币 → 破坏帧 → **渐变 1s → 销毁**；不进路线快照。
- C5 `SupplySpawnManager`（实验程序集，与路线空间耦合）：每 1s 按时刻表核对 —— 该有的生成、不该有的销毁；玩家重新回到该页面时，下一次核对就等于「按倒计时重新查询并刷新」。

**实测**
- 落地页生成 2 个局外物件（左侧路肩 / 右侧远处），点中 → 金币入账 → 破坏帧 → 渐变（alpha 1.00 → 0.34）→ 销毁 ✅。
- 重复点击不再加钱（claimed 记录生效）✅。

**一个精度坑**
- Unix 秒在 **float32** 下分辨率只有 128 秒 → 用 float 算轮次会整整偏一两个轮次，实例 id 撞车（表现为“刚领过就再也不生成”）。时刻表内部一律用 `double`。

### 场景规则合规（依据 `memory/scene-build-procedure.md`）

| 规则 | 本次处理 |
|---|---|
| 规则 1：沿街件内边缘 ≥ 路面半宽（2.5m） | 箱子从 x=3.0（内缘 2.06，**侵了路面**）改到 **x=3.6（内缘 2.66）** ✅ |
| 规则 3：建筑退让 = 路面半宽 + 间隙 + 素材可见半宽 | 可见半宽 0.936（936px/100×0.2）✅ |
| 规则 6：街道近场留空（前 ~8m 只有路） | 箱子从 z=3.6 移到 **z=9.5**（第一处路肩摆件 z=3.0，箱子不再抢近场）✅ |
| “不遮挡其他物体” | 按“身后 2m 内无高度 ≥0.9m 的摆件”筛选，候选里 **x=3.6 / z=9.5** 遮挡分 0.23（最低一档）且屏幕完整可见 ✅ |
| 规则 7：只用本节点素材集 | ⚠️ **未合规**：箱子美术在 `Art/EnvironmentV2/Props/EV/`（EV 节点的素材集）。待办：把箱子素材拷到 `Props/N1/` 并重切图集（会改 internalID，需重绑 prefab） |
| 点位固定（用户要求） | 局外点位写死在计划资产里：`(-3.6, 11)` / `(4.5, 14.4)`，均避开路面与高件 ✅ |


---

## 5. 程序集边界（已核实）

- `Locus.Experiments` → 引用 `Locus.Data/Services/Combat/Flow/UI` ✅
- `Locus.Combat`（`Assets/Scripts/Prop/` 经 `Locus.Combat.asmref` 归入）→ 只引用 `Locus.Data/Services` ❌ 不能引用 `Locus.Experiments`
- 结论：**跟随器放 Experiments**（需要 `YScrollSample`）；`BreakableProp`（Combat）不得反向依赖它 → 交互层用接口/事件解耦（接口定义放 `Locus.Data`）。

---

## 6. 已知风险与坑

1. **投影数学双实现**：shader 与 follower 各一份 → 改滚动规则时两边都要改；follower 侧加一条「与 shader 差值 ≤ 1cm」的调试断言。
2. **窗口双写 `renderer.enabled`**：A2 必须同批落地，否则残骸复活问题照旧。
3. **pivot 改动**会让 6 张 sprite 的 `sprite.rect` 不变但锚点变化 → 通过 TextureImporter API 改并重导入，禁止手改 `.meta` YAML。
4. **存档兼容**：局外 `claimed spawnIds` 进存档分片，缺字段的旧档按「空集合」处理。
5. **工作区在飞改动**：实施时 `Battle.scene`、`AttackSystem.cs`、`InputManager.cs` 等 68 处有未提交改动 → 动手前先 `git status` 复核，避免覆盖。

---

## 8. 部署调整（2026-10-24 晚，用户确认后）

1. **N1 的箱子已移除**（场景实例 `N1_Prop_Crate_01` 已删）。
2. **E0 节点部署 1 个箱子**：`E0_Prop_Crate_01`，奖励 100 币，位置
   **路线局部 (3.6, 0, 100)** = E0 节点纵深 10m。
   - 内缘 2.66 ≥ 路面半宽 2.5 ✅；身后 2m 内无高 ≥0.9m 摆件 ✅；
   - **纵深 10m 是为了「在屏内」**：战斗相机在玩家身后 10m，玩家深度处的可视半宽只有 **±2.89**，
     合规的路边位置（|x|≥3.44）在这个深度**在屏幕外**；推到 10m 后半宽 5.8，箱子完整可见可点。
   - 放在敌人后阵（世界 z=9）之后 → 敌人向前推进、不会穿过它。
3. **奖励改为「金币爆散」**：点中 → **12 枚金币图标**从箱体位置散开、抛物线飞向金币计数器，
   **总时长 0.6s**（单枚 0.32s，错峰起飞），落进计数器时才入账。
   - 手感参照经验宝石（`ExpGem` / `ExpGemManager`）：屏幕空间 UI Image + 贝塞尔弧线。
   - 实现：`SupplyRewardPort`（数据层端口，内核不依赖 UI）+ `SupplyCoinBurst` / `SupplyCoinIcon`（UI 层）。
   - **不能吞奖励**：没有画布/目标/相机时端口立即回调；UI 层另有兜底计时器（超时也发）。
   - **状态记账立即做，只有金币数额等动画播完再入账**：
     否则「已碎 → 销毁 → 管理器重新生成」会开出一个重复物件（实测复现过）。

### 8.1 本轮修掉的一个真 bug（`Mathf.SmoothStep` ≠ GLSL `smoothstep`）

跟随器复刻滚动 shader 的远景下沉时用了 `Mathf.SmoothStep(0, 6, d)`：
Unity 的 `Mathf.SmoothStep(from,to,t)` 是「把 t 夹到 [0,1] 再在 from→to 之间插值」，
`d > to` 时会返回 **to(=6)**；而 GLSL 的 `smoothstep(0,6,d)` 返回 **1**。
结果远景下沉被放大 6 倍——E0 箱子（距旅行点 90m，d=72）直接掉到地下 280 米（完全不可见）。
现已改成手写 GLSL 语义（`t=Clamp01(d/6); t*t*(3-2t)`）。

> 教训：跨语言复刻 shader 数学时，**同名函数不保证同语义**；该处已加注释说明。

## 7. 未决项（等用户拍板）

> 2026-10-24 晚更新：1~5 已定（见 §8 与下文），仍待定的是「局内补给是否也改成按生成表每局刷」与「箱子美术仍用 EV 素材集」。
> 2026-10-25：另新增一条「重打关卡时箱子要不要复原」（对应 `plan/bugs-and-requests.md` 的 BUG-001 与
> `plan/content-development-todo-20261025.md` 的 Q8）。本节与那份权威待办表**保持同源**，改动请同步两处。

1. 局外物件的**有效期**与**生成点位** —— 已定：固定点位（写在计划资产里）；有效期按「轮内偏移 + 存活时长」，当前 60s 轮 / 点位1 整轮 / 点位2 后 40s。
2. 局外奖励除了存档金币给不给别的 —— 已定：**暂时只给金币**。
3. **局内补给是否也改成按生成表每局刷**（而不是固定一只）—— 待用户回答：现在 N1 只有一只固定箱子；如果改成表驱动，就要给局内也做一份「点位 + 轮次」计划（同一套 `SupplySpawnPlan` 可以复用，只是层不同）。
4. 残骸策略 —— 已定：局内保留；局外交互后渐变 1s 消失并销毁。
5. 「点中本体」与同列敌人的关系 —— 已定：**可以同时受击**（点物件时，该列敌人照常吃这一记戳）；物件本身不按列判定（同列概念对物件不成立）。
6. 箱子美术仍在 EV 的素材集里（规则 7 未合规）—— 要不要拷到 `Props/N1/`？
