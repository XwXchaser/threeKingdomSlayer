---
id: kd_e42a0a48-0246-4517-838f-19022757a6ac
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 卷轴场景按需加载优化实施计划

## 目标与边界

主菜单进入长坂坡的加载体验，以及行进期间的每帧开销。核心手段是"按需"：看不见的不提交、还没走到的不装在内存里。

本轮不改：

- 路线长度、曲率、节点米数、EV/N3/N2 布局。
- 任何布景物体的世界坐标与美术分组语义。
- 战斗数值、节点流程、存档结构。
- 包体大小不做承诺（本轮不测量也不优化）。

**不要求节点等距。** 加载参数全部由实际节点米数推导（见 §四），当前 90/100/76/45 的不等距现状是输入而不是待修问题；是否等距属于节奏设计决策，与加载无关，将来改了也不需要改加载代码。

## 一、权威数据

### 节点米数不等距（本计划的基准，禁止按节点编号或等距推算）

数据源：`Assets/Experiments/CurvedScroll/YJunctionSample.unity` 中 6 个 `ScrollRouteNodePresentation.worldDistance`，以及 `Assets/RouteData/ChangbanpoScroll/Travels/Travel_*.asset` 的 `duration`。

| 节点 | 沿路米数 | 与上一点间距 | 移动时长 | 平均速度 |
|---|---|---|---|---|
| N1 | 0 | — | — | — |
| E0 | 90 | 90 | 3 s | 30.0 m/s |
| J1 | 190 | 100 | 3 s | 33.3 m/s（最快） |
| N2（右路） | 266.116 | 76.1 | 3 s | 25.4 m/s |
| EV（左路） | 280 | 90 | 3 s | 30.0 m/s |
| N3（左路终点） | 325 | 45 | 3 s | 15.0 m/s（最慢） |

- `YScrollSample.Length = 190 + 28.1596×45° + 113 ≈ 325.1`（`JunctionDistance` / `CurveRadius` / `ExitTailDistance` 均为常量）。
- 转弯之后世界 Z 与沿路米数不再一致（`BattleYRouteHost.TryGetNodeProgress` 的注释已说明分支世界 Z 短于行进距离）。因此**所有段边界、窗口、阈值只能用沿路米数，不能用世界 Z 手填**。

### 可视窗口由着色器定义，不归加载系统决定

`Assets/Experiments/CurvedScroll/Shaders/YScrollScenery.shader`：

```hlsl
fade = saturate((anchor.z + 14) / 3) * saturate((80 - anchor.z) / 10);
```

- 身后 −14 m 开始渐隐，−11 m 已全隐。
- 前方 70 m 完全淡入，80 m 淡到 0。
- 即真实可视带约为 **[pos − 14, pos + 80]**，而整条路 325 m。

### 三条直接推论

1. **最长间距 100 m > 可视 80 m**：站在任一节点时，下一段的近端已经在可视窗口边缘甚至窗内。加载不能等"移动开始后再做"。
2. **最快一段 33.3 m/s，可视带 2.4 秒走完**：任何在 3 秒移动期间开始的实例化，都会变成玩家看得见的卡顿。
3. 因此预载只能发生在**节点停留期**（战斗、奖励选择、出口确认、选路、死亡读档），这正是"到节点预载下一段"的合理依据；但参数用米，不用节点序号。

## 二、现状测量（`YJunctionSample.unity` 实测）

| 项 | 改动前 | Step 1 之后 |
|---|---|---|
| GameObject 总数 | 1483（单根 `Y Junction - select for preview` 下 1482） | 856 |
| SpriteRenderer + MeshRenderer | 1162 + 198 = 1360 | 543 + 198 = 741 |
| 其中 `m_Enabled = 0`（作者态关闭的精灵） | 129 | 129 |
| 真实提交量（`enabled ∩ activeInHierarchy`） | 578 | 578 |
| 唯一材质 | 19（共享良好，不是材质爆炸） | 19 |
| `YScrollSample.scenery` 数组 | 790 项，渲染前把 `localBounds` 撑到 10000 → 视锥剔除失效 | 665 |
| Y 系着色器 | `YScrollScene` / `YGrass` / `YSceneryWind` / `YOrientedSceneryE0Route` 均声明 `"DisableBatching"="True"` | 同 |

> 早期统计里的“820 激活物体 / 707 激活 renderer”按 `activeSelf` 计算，高估了提交量：**129 个 SpriteRenderer 序列化为 `m_Enabled = 0`**（作者关掉的内侧灌木与草），从来不会提交。以 **578** 作为窗口优化的基线。

由此得出两条测量结论：

- **每帧浪费 2~4 倍**：578 个 renderer 每帧全部提交，而着色器本身只在 `[−14, +80]` 内可见（实测可视带内 60~210 个）。且因禁用合批，**draw call 量级 ≈ renderer 数量**。
- **加载浪费集中在一次性**：Step 1 前有 663 个未激活物体随场景反序列化，其中 627 个已搬出归档（见 Step 1）。

### 美术分组按主题组织，不按距离组织（影响段切分）

| 组 | 物体 | 激活 | renderer | 直接子节点 | 世界 Z 跨度 |
|---|---|---|---|---|---|
| J1 Integrated - Official Road and Stone Lane @ 190 | 399 | 399 | 392 | 6 | 173.0 – 254.2 |
| Opening Roadside Props | 100 | 81 | 79 | 92 | 3.0 – 82.0 |
| N3 Village Rescue @ 266 | 85 | 85 | 76 | 4 | 293.7 – 340.2 |
| Route Nodes - N1 E0 J1 and Branches | 73 | 64 | 49 | 6 | —（逻辑锚点 + 敌人展示标记） |
| E0 Integrated - ... - Approach | 48 | 48 | 42 | 5 | 92.0 – 126.8 |
| N1 to E0 - Shoulder Detail v1 | 34 | 34 | 30 | 3 | 52.0 – 130.0 |
| E0 to J1 - Beyond Gate Shoulder | 17 | 17 | 16 | 15 | 142.0 – 170.0 |
| E0 Integrated - ... - Roadside / 主组 | 10 + 9 | 8 + 9 | 7 + 7 | 9 / 1 | 94.5 – 132.3 |
| J1 to EV - Village Approach | 8 | 8 | 7 | 7 | 215.3 – 252.4 |
| Route Connections | 49 | 49 | 0 | 5 | —（纯逻辑） |
| Encounter Authoring | 13 | 13 | 0 | 4 | —（战斗锚点） |
| Approach - Valley Road | 483 | **0** | **0** | 482 | —（废弃） |
| 5 个分支组（左/右地标、坑谷、营垒、岩石屏障） | 144 | **0** | **0** | 21~32 | —（引用已置空） |

`J1 Integrated` 内部已有可用切分：

- `J1 Side Vegetation and Right Road Detail`：198 物体 / 118 直接子节点（z 176–254，跨整段，是最粗的一批）
- `Left Stone Lane - Fences and Low Vegetation`：52（z 193.6–242.9）
- `Right Official Road - Transport and Supplies`：55（z 193.6–242.9）
- `J1 Approach - Low Shoulder Detail`：49（z 173–190.5）、`Central Signpost and Divider`：15、`J1 Vegetation - Accepted Trees`：29

注意左右两路内容的 z 跨度完全重叠（都在 193.6–242.9），说明**分岔后 3.6 m 就开始出现两路各自的近端布景** —— 这是分支点必须双常驻的直接证据。

## 三、加载模型

两层，顺序不可颠倒。

### 第 1 层：距离窗口激活（每帧收益，不改资产）

对场景内对象按"沿路米数"做窗口激活，双半径：

```
residentHorizon = 120 m   // 激活：objectDistance ≤ pos + 120
residentBehind  =  30 m   // 释放：objectDistance <  pos - 30
```

- 120 来自"最大节点间距 100 m + 20 m 缓冲"，同时远超 80 m 淡出线；保证任何一次节点间移动中都不会露白。
- 30 来自淡出 −14 m 再加 16 m 安全边。
- 语义等价于着色器已有的淡出契约，只是把"淡成透明仍在画"换成"不提交"。

### 第 2 层：段 prefab 按需实例化（一次性收益）

首屏场景只保留常驻 + S0；其余段做成 prefab，在节点停留期按窗口实例化，离开窗口后销毁。

- 段登记为 **Inspector 直连的 prefab 引用数组**（`prefab / startDistance / endDistance / preloadLead / alwaysResident`），不使用 `Resources.Load`、字符串 ID、Addressables —— 与 `design/anti-ghost-reference.md` 一致。
- 用 prefab 而不是 Additive 子场景：微信/WebGL 无线程（`webGLThreadsSupport: 0`），`LoadSceneAsync` 的实例化是一次性主线程落完，等于一整根尖峰；prefab 可以每帧 1~2 个根摊平，且不引入跨场景引用。Addressables 不在当前 `Packages/manifest.json`，引入等于新增包体与构建管线成本。
- 只有进入 §七 的 Phase 5 条件（单段大到必须整块释放，或需要热更下载）才考虑子场景/Addressables。

### 段表

| 段 | 米数范围 | 内容 | renderer | 直接子节点 | 策略 |
|---|---|---|---|---|---|
| S0 开场 | 0 – 95 | Opening Roadside Props（z<95 部分）、N1→E0 Shoulder（z<95 部分） | ~110 | ~100 | 首屏装载 |
| S1 E0 | 95 – 160 | E0 Approach、E0 主组、E0 Roadside、N1→E0 Shoulder（z≥95 部分） | ~60 | ~20 | 到达 N1 后于战斗中实例化 |
| S2 J1 路口 | 160 – 250 | J1 Approach、Central Signpost、J1 Vegetation、J1 Side Vegetation（拆两块，见下） | ~290 | ≤60 | **preloadLead = 160 m**，在 E0 战斗中实例化 |
| S3L 左路 | 190 – 340 | Left Stone Lane、J1 to EV Village Approach、EV→N3 Village Interior、N3 Village | ~145 | ≤60 | 到达 J1 时与 S3R 同时常驻 |
| S3R 右路 | 190 – 300 | Right Official Road、右侧营地内容 | ~55 | ≤45 | 同上；选择后延迟释放另一侧 |

- S2 的 `J1 Side Vegetation and Right Road Detail`（198 物体 / 118 直接子节点）**必须二次切分**为"z<210"和"z≥210"两个 prefab，否则单批实例化无法摊平。
- 段 prefab 直接子节点上限 60；实例化预算 2 根/帧 → 最多 30 帧（约 0.5 s）。
- 段边界是作者数据（`startDistance` / `endDistance`），按当前实测米数落地；节点增删或间距改变时只重新标段，不改代码，因为窗口参数是推导出来的（§四 6）。

### 常驻清单（永不随窗口卸载）

- `Terrain - one mesh, fork painted in terrain`（覆盖全程，1 个 renderer）。
- `Skybox Background - editable`。
- `Route Nodes - N1 E0 J1 and Branches`（6 个节点锚点 + 敌人展示标记）、`Route Connections`（49）、`Encounter Authoring`（13）—— 纯逻辑/锚点载体，共 135 物体。
- `N1 Far Landmarks`。

注意：`Route Nodes` 的 49 个 renderer 是 `Display Enemies - no combat` 展示标记，运行时由 `BattleYRouteHost` 按名字关闭；常驻不等于可见。

## 四、窗口与预载规则

1. **每帧从 `YScrollSample.progress × Length` 重算窗口，幂等，不做事件链**。现有流程有三处会破坏事件顺序：`MoveTo` 在 `Blocking()`（升级 UI、对话、经验收集）时暂停推进；`StageController.RestartStage()` 的检查点恢复会直接把 progressed 跳到任意米数；`BattleYRouteHost.Choose()` 会切换 `route.right`。
2. **激活判据是"段起点米数 − pos ≤ preloadLead"**，不是"第 N+1 个节点"。因为间距 45/76/90/100 不等，按节点序号会在 EV→N3 过载、在 E0→J1 露白。
3. **分支双常驻**：到达 J1 时 S3L 与 S3R 必须都已就绪（两路近端在 193.6 m 就开始出现，且选完立即开始 3 秒移动，中间没有加载窗口）。选择确定后不得立即释放未选侧，等 `pos > 190 + 120` 再释放。
4. **禁止在移动期实例化**：3 秒移动内速度 15–33 m/s，可视带 2.4–5.3 秒走完，任何一帧的实例化峰值都会被看见。移动开始前必须完成。
5. **检查点恢复**是唯一的一次性大量激活场景，必须放在死亡读档的停顿里完成。
6. **窗口参数从数据推导，不写死常量**：

   ```
   fadeEnd = 80                                  // 唯一事实源：YScrollScene 着色器淡出距离
   maxGap  = max(相邻节点米数差)                  // 从 routeConfig.nodes + nodePresentations 计算
   residentHorizon = max(fadeEnd + 40, maxGap + 20)   // 当前配置下 = max(120, 120) = 120
   residentBehind  = 30
   ```

   这样任何节点间距（等距或不等距、增删节点、改关卡）都自适应，不需要改代码只重新标段；同一公式同时保护了"着色器看得见"和"跨一个节点移动"两个约束。
7. **段表按实测米数落地，不按索引推算**：`preloadLead` 每段单独可覆盖（J1 段 160 m），其余默认等于 `residentHorizon`。

## 五、硬约束（不变量）

1. **不改任何布景物体的世界坐标，不 reparent 到会移动的父节点**。根 transform 是 identity，着色器靠 `_YSToLocal` / `_YSToWorld` 换算；改变层级或位移会让整段布景错位。
2. **段根 `SetActive` 不刷子物体**。场景里有 649 个作者态 `active=false` 的物体（隐蔽灌木、展示敌人、已置空分支组）。根节点开关不会改子物体的 `activeSelf`，prefab 方案天然安全；若以后做逐 renderer 剔除，**必须记录并恢复作者态 active**，不能一律刷成 true。
3. **不调用 `Resources.UnloadUnusedAssets`**：WebGL 上是同步重活，会卡在最不该卡的段切换点。
4. **段边界只用沿路米数**，由 Editor 工具把段的 renderer 包围盒投影到路径自动算出（`ScrollRouteNodePresentation.SyncTransformFromDistance` 是现成的反向实现，投影函数补在旁边），不手填世界 Z。
5. **段 prefab 直接子节点 ≤ 60，实例化 ≤ 2 根/帧**。
6. **不引入 `Resources.Load` / 字符串 ID / Addressables**，段用 Inspector 直连引用。
7. **不改 `YScrollSample.Evaluate/Length/turnAngle`**，不改移动时序配置。

## 六、分步施工

### Step 0：基线（先量后改）

- 录制：从主菜单进关的总耗时、离场动画期间最差帧、第二次由 `BattleYRouteHost.Start()` 触发的加载时长。
- 录制：N1 / E0@90 / J1@190 / 左 EV / 右 N2 五个机位的 draw call、SetPass、提交 renderer 数、最差帧。
- 对每个节点 dump 全部 `TryGetNodeProgress` 结果，作为后续视觉回归的基线。
- 验证方式：Frame Debugger / Profiler + 运行时 HUD 计数器（提交 renderer 数、当前窗口内段数）；本阶段只观察。

### Step 1：搬出为归档 prefab（不删除）— ✅ 已执行 2026-10-17

原则：**只搬出、不删除**。搬出前实测：`Approach - Valley Road` 根 `activeSelf=true` 但 482 个子物体全为 `false`；5 个分支组根 `activeSelf=false`（`Left - Valley Encounter` / `Right - Camp Encounter` 各有 1 个 `activeSelf=true` 的子物体，但父级关闭）。即 **619 个 renderer 对渲染的贡献为零**，只是随场景反序列化、占内存。

| 归档组 | 物体 | renderer | 世界 Z 跨度 | 内容摘要 |
|---|---|---|---|---|
| `Approach - Valley Road` | 483 | 482 | 0 – 139.9 | `GrassPatch_Opening` 154、`Roadside_0..5` 各 44（共 264）、`GrassOpeningTrial_04` 11 —— 旧开场路边/草散布，已被 `Opening Roadside Props` + `N1 to E0 - Shoulder Detail v1` 取代 |
| `Junction Rock Barrier and Roadside Density` | 33 | 32 | 124.8 – 250.1 | 岔路口岩壁、左右路侧簇、远景树、巨石 |
| `Left branch landmarks` | 31 | 30 | 194.2 – 252.0 | `GrassPatch_Left` 17、瞭望塔、松树、石组 |
| `Right branch landmarks` | 31 | 30 | 195.2 – 250.8 | `GrassPatch_Right` 17、松树、石组 |
| `Left - Valley Encounter` | 28 | 26 | 167.7 – 252.2 | `Enemy101_Row1/Row2` 各 3、谷壁、侧翼标记（旧左路战斗展示） |
| `Right - Camp Encounter` | 21 | 19 | 167.7 – 254.4 | `Enemy101_Row1/Row2` 各 3、营门地标、帐篷、旗、火盆（旧右路战斗展示） |
| **合计** | **627** | **619** | — | — |

- 依据：`plan/j1-scene-deployment-handoff-20261011.md` 记录 5 个分支组的引用已按"避免旧山谷/营地组在新 J1 布景下被 Host 重新启用"置空；`Approach - Valley Road` 已核实**不在 `YScrollSample.scenery` 数组的 790 项内**（逐 fileID 反查）。
- 归档位置：`Assets/Experiments/CurvedScroll/Authoring/Archive/`（与路线场景同目录层级），命名保持原组名。
- 同步剔除 `scenery` 数组里属于这 6 个组的条目（实测 **125 项**，全部属于 5 个分支组，索引 133–257 连续；`Approach - Valley Road` 无条目）。
- **搬出时必须生成清单**：`archive-inventory.csv`（组名、物体名、世界坐标、activeSelf），并记录搬出前后的场景物体总数与 renderer 总数，作为"没少东西"的凭证。
- 前置：零引用审计必须覆盖 `.scene`（`memory/project-mistake-note.md` 的教训）。
- 验证：`TryGetNodeProgress` 全节点 dump 无变化；五个机位截图无差异；搬出前后物体数差 = 627（不多不少）；Console 0/0。
- 回退：把归档 prefab 拖回原父节点即可复原，坐标在 prefab 内已固化。

**执行结果（2026-10-17）**

| 指标 | 前 | 后 | 差 |
|---|---|---|---|
| 场景 GameObject | 1483 | 856 | −627 |
| Renderer | 1360 | 741 | −619 |
| `YScrollSample.scenery` | 790 | 665 | −125 |
| 场景文件 | 3451 KB | 1884 KB | −45% |

- 6 个 prefab 全部保存成功（`success=True`，合计 ~1.5 MB），`archive-inventory.csv` 627 行 + 表头，`README.md` 已写明复原步骤。
- `TryGetNodeProgress` 六个锚点前后完全一致：N1 `0.000000` / E0 `0.276824` / J1 `0.584406` / EV `0.861230` / N2 `0.818527` / N3 `0.999642`。
- `scenery` 665 项无空引用；场景根下剩 16 个组，渲器数合计 741 与实测一致；Console 0 warning / 0 error。
- 执行时场景带未保存改动（dirty），已随本次保存一并落盘；`AssetDatabase.SaveAssets()` 也可能同时写入了其它待保存资产。

### Step 2：距离窗口激活（不改资产，先拿每帧收益）

- 在路线层加一个流送控制器：读取 `progress`，对段内的对象做 `[pos−30, pos+120]` 激活。
- 顺带把 `scenery` 数组"全部撑到 10000"收敛为"仅撑跨弯道的大件"，让段内真实 bounds 恢复剔除能力。`CurveRadius = 28.1596`、45° 时最大横向位移约 8 m，尾段叠加，余量按这个量级给。
- 验证：提交 renderer 数从 707 降到视窗内 100–200；五个机位 draw call 与最差帧改善；截图与基线一致；确认 `Skybox` 与远景地标是否走 Y 着色器（走的话要进常驻清单而不是被窗口吞掉）。
- 回退：窗口控制器单独开关，关闭即恢复原行为。

### Step 3：段 prefab 化（装载收益）

- 先做 S2（最重、收益最大）：把 `J1 Integrated` 按 §三 拆成 ≤60 直接子节点的 prefab，包括对 `J1 Side Vegetation` 的二次切分。
- 再做 S1、S3L、S3R、S0 剩余部分；首屏只装 S0 + 常驻。
- 预载时机挂在节点停留期：N1 战斗中装 S1，E0 战斗中装 S2，J1 出口确认时同时装 S3L/S3R。
- 验证：首屏 GameObject 数由 1483 降到 ~250 + 常驻；移动期间零实例化（用日志断言"移动相位内无实例化请求"）；五个机位截图与基线一致。

**执行记录（2026-10-17，部分完成）**

已实现 `Assets/Experiments/CurvedScroll/Scripts/YRouteSegments.cs`（段表 + 停留期预载 + 离开后卸载）与 `YRouteStreamingWindow` 的动态登记（`RegisterSegment` / `UnregisterSegment`，同步维护 `scenery` bounds 表）。

已搬出的 13 个段（prefab 在 `Assets/Experiments/CurvedScroll/Authoring/Segments/`）：

| 段 | 距离 | renderer |
|---|---|---|
| E0 Integrated - Gate and Aftermath @ 90 | 130–135 | 7 |
| E0 Integrated - Gate and Aftermath @ 90 - Approach | 90–129 | 42 |
| E0 Integrated - Gate and Aftermath @ 90 - Roadside | 94–117 | 9 |
| E0 to J1 - Beyond Gate Shoulder | 140–172 | 16 |
| J1 Approach - Low Shoulder Detail | 171–193 | 48 |
| J1 Side Vegetation and Right Road Detail | 174–271 | 197 |
| Left Stone Lane - Fences and Low Vegetation | 192–262 | 51 |
| Right Official Road - Transport and Supplies | 192–262 | 54 |
| Central Signpost and Divider | 203–209 | 14 |
| J1 Vegetation - Accepted Trees and Tall Grass | 202–247 | 28 |
| J1 to EV - Village Approach | 223–276 | 7 |
| EV to N3 - Village Interior | 234–258 | 4 |
| N3 Village Rescue - Accepted Art @ 266 | 323–328 | 76 |

结果：场景 GameObject 846 → **275**；场景内 renderer 188；`scenery` 164。首屏停留后装回 10/13 段。

实测（Play Mode，逐帧推过四条腿）：**移动期间装载数 = 0**（N1→E0 / E0→J1 / J1→N2 / J1→EV 全部为 0）；段重复装/卸两轮后 `scenery` 不增长、无空引用；可见性校验（按 prefab 内容逐件核对）缺失 0 件。

重要发现（与计划的预期不同）：

1. **预载提前量必须 ≥ 最大节点间距 + 渲染窗口**（当前 100 + 105 + 10 = 215 m），否则移动期间仍会临时装载。已改为从 `nodePresentations` 推导，不再手填。
2. **本关太短，按需的收益主要在“场景装载”而不在内存**：可视窗 105 m、关卡 325 m，第一个节点停留就会把 10/13 段装回。真正受益的是以后更长的路线/更多关卡。
3. `J1 Side Vegetation`（197 renderer）已按路口米数二次切分为 A（50 renderer，174–211）与 B（147 renderer，209–271），最大的单次 Instantiate 从 197 降到 147。旧 prefab 保留在 Segments 目录里作备份，已不参与装载。
4. 因为预载提前量必须 ≥ 215 m，**第一个节点停留就会把 J1 整块也装回来**（11/14 段）。也就是说本关的“按需”收益在初始装载与分摊，不在峰值内存；要真正看到内存效果，需要关卡变长。
5. 移动期间仍会有少量**卸载**（E0→J1 卸 3 段、J1→N2 卸 4 段），目前允许；若真机在此处卡顿，再把卸载也推到停留期。


### Step 4：场景架构（原 Additive 方案降级为可选项）

`plan/wechat-additive-loading-implementation-plan.md` 的 Bootstrap + 全 Additive 放在最后，价值转为"以后养成/商店多界面层 + 跨关卡预加载"。内容按需之后，Battle 的 155 个物体与菜单已不是瓶颈。

若实施，必须补上该文档缺的两项：MainMenu 与 Battle 各有 1 个 AudioListener 与 1 个 EventSystem，共存会产生 "2 audio listeners" / "2 event systems" 警告；以及开发态直接开业务场景按 Play 的自举兜底。另：`FakeRouteStageConfig` 目前没有路线场景字段（`BattleYRouteHost.routeSceneName` 与 `ScrollRoutePresenter.routeSceneName` 各自硬编码路径），要"按关卡预加载路线层"必须先补字段（建议直接序列化 `SceneAsset`）。

## 七、验收

### 装载

- 首屏 GameObject 数、首屏耗时、离场动画期间最差单帧。
- 移动结束进入下一节点时，不存在第二次加载尖峰。
- 移动相位内实例化请求数 = 0（日志断言）。

### 每帧

- 五个机位的提交 renderer 数、draw call、SetPass、最差帧。
- 目标：提交数从 707 起降到视窗内 100–200 区间（按 Step 0 基线定具体阈值）。

### 视觉不变

- N1 / E0@90 / J1@190 / 左 EV / 右 N2 五机位前后截图逐段比对。
- 所有节点 `TryGetNodeProgress` 结果与 Step 0 基线完全一致。
- Console 无新增 warning/error。

## 八、风险与待决

1. **分支视觉引用当前为空**：`YScrollSample.leftBranchVisuals / rightBranchVisuals / leftBranchEncounter / rightBranchEncounter / junctionVisuals` 均序列化为 `{fileID: 0}`，`BattleYRouteHost.SetBranchGroup` 是空操作。这不阻塞本计划（这些组本来就不显示），但将来恢复分支视觉切换时，要保证"组显隐"与"段装卸"是两个独立维度，不互相覆盖。
2. **`J1 Side Vegetation` 的二次切分**会改动美术分组语义。需要确认按 z<210 / z≥210 切开后，跨切片的视觉衔接（草簇、路边密度）在机位截图里无差异。
3. **`skybox` 与远景地标是否走 Y 着色器**未验证，Step 2 必须实测确认，否则会被窗口误吞。
4. **`Approach - Valley Road` 的外部引用**未遍历完（仅确认不在 `scenery` 数组）。已按"搬出为归档 prefab、不删除"执行，审计只影响是否需要保留在场景外，不再影响可逆性。
5. **Addressables / Additive 子场景**明确不在本轮，触发条件见 §三。
6. 旧文档 `plan/wechat-additive-loading-implementation-plan.md` 的"首轮全 Additive"与"每帧只激活有限数量场景根对象"两项主张由本计划降级/替代；其"禁止 Single Load 打断动画""预热可中断""不做菜单期全关卡预加载"三项仍然有效。
