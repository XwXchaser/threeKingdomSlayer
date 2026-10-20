---
id: kd_41d45e7b-c4a4-480f-9626-81a85e1382e3
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 可破坏补给（木箱）· EV 部署方案（2026-10-19）

> 范围：只准备部署方案，不含代码实现、场景改动和资产导入。本文不作为已批准设计。
> 依据：用户 2026-10-19 已验收木箱基图与六格组图 v2，并要求「常态未破损作为场景装饰物部署、攻击触发破坏并发放奖励」。

## 0. 一句话结论

第一阶段把木箱放到 **N1 战斗节点**：那里战斗期输入本来就是开的，不改任何门禁就能测通“点→ stab→破坏→奖励”全链；EV 作为第二阶段，因为行进期 `SetInput(false)`，需要额外开一个非强制交互窗口。素材侧唯一硬约束是基图与组图的像素密度差，已用导入 PPU 归一化（零运行时缩放、零重采样）。

## 1. 已核实的工程事实（带出处）

### 1.1 素材

| 项 | 值 |
|---|---|
| 常态基图 | `C:/Users/Administrator/Pictures/gptGen/breakable_props_base_v1/crate/Prop_Crate_base_v1.png`，1024×1024 RGBA，SHA-256 `c6757ef3…`，实心包围盒 `(53,112)-(974,956)`，Alpha 包围盒 `(50,104)-(986,959)` |
| 六格组图 | `C:/Users/Administrator/Pictures/gptGen/breakable_props_break_atlas_v2/crate/Prop_Crate_BreakAtlas_v2.png`，1536×1024，Color Type 6，SHA-256 `c89710c7a2ffe075d5b84ea296c06afa2c58c792eec875ca985973792b49e987` |
| 组图格内尺度 | 格 1 箱体 387×339 px（方案要求 240×219，模型放大约 1.6×）；六格最小格内留白 11px（方案要求 80px） |

**密度差是硬约束**：基图箱体宽 921 px，组图格 1 箱体宽 387 px，两者像素质感密度相差约 2.38 倍。直接混用会让破坏瞬间箱体尺寸跳变或像素粗细突变。

### 1.2 场景与摆放基准

- EV 的美术不在 `Battle.scene`，在附加加载的 `Assets/Experiments/CurvedScroll/YJunctionSample.unity`，其中已有 `EV `、`EV Approach `、`EV Village Outskirts ` 三个节点组。
- 现有装饰基准 `EVApproach_Cart`：`localPosition (6,0,-18)`、`localScale 0.34`、`sortingLayer 0`、`sortingOrder 0`、Sprite 世界尺寸 15.36×5.88（PPU 100）。
- 战斗侧 `Battle.scene` 的 `Battle Y Route Host`：`groundHeight = -1.8`、`enemyFootGroundY = -1.8`。
- 该场景现有排序值只有 `-100 / -90 / 0`，全部在 Default 排序层。

### 1.3 输入与攻击链路

- 列解析：`Assets/Scripts/Player/InputManager.cs:809 GetColumnFromScreenPosition` 只拿**存活最前排敌人**的屏幕 X 做最近匹配，超过半列宽（`Screen.width/10`）直接返回 -1；无敌人时走 `:848 FallbackGetColumn`（`screenX/width*5` 取整）。
- 出手判定：`Assets/Scripts/Player/AttackSystem.cs:258 TryExecuteAttack` 只有 `hitAny == true` 才返回成功并进入冷却；`Stab/Slash` 的结算挂在“首次实际命中”；`OnAttackPerformed`（`:354`）只在**非 Stab/Slash** 分支触发。
- 伤害投递：`Assets/Scripts/Attack/AttackWave.cs:636 HitTarget → enemy.TakeDamage`，目标集合来自列内敌人列表，**不是物理检测**，道具不会被现有波自动命中。
- **行进期输入关闭**：`Assets/Scripts/Core/BattleYRouteHost.cs` 在 Loading(96)、Opening travel(160)、战斗收尾(195)、节点 travel(240) 调用 `SetInput(false)`，只有进入战斗（193）才 `SetInput(true)`。EV 是纯行进节点，默认全程不可攻击。

### 1.4 奖励与存档

- `Assets/Scripts/Player/PlayerState.cs:424 AddCoins(int)`、`:259 Heal(float)`（`Heal` 在 Defeat/Victory 状态直接 return）。
- `Assets/Scripts/RouteFake/FakeRouteStageSaveSnapshot.cs` 只保存 `coinCount`、`currentHealth`、节点完成记录等，**没有“哪些道具已被破坏”这类字段**。

### 1.5 高亮描边

- `Assets/Shaders/EnemyOutline.shader` 暴露 `_OutlineColor`、`_OutlineWidth (0-10)`、`_OutlineEnabled`，`Enemy` 通过 `MaterialPropertyBlock` 设置（`Enemy.cs:4864-4866`），可直接复用到道具上做脉动描边，不需要另出图。

## 2. 已定与待定

### 2.1 已定（用户 2026-10-19）

1. 道具按**带血量的单位**实现，不显示血条；血量可配：酒缸 **1 血**、木箱 **1 血**。
2. **交互不是强制的**：只提供交互提示，玩家可以随时点前进/离开继续，不影响主流程。
3. 玩家点一下 → 对该目标打出一记 stab；目标被“击杀”后**不能再被 stab**（已破坏的道具必须从目标解析中移除，否则会空耗冷却）。
4. 高血量单位按**血量百分比**切换破损形态美术；本轮只有“完整 + 6 帧破碎”两态，多段美术只预留数据结构。
5. 酒缸奖励：**必杀能量直接充满**；木箱奖励：**随机道具，当前固定发 100 金币**。
6. **破坏段禁止任何缩放补偿**，六格组图内部尺度一致。
7. **本轮只部署木箱，酒缸不部署**（酒缸无可用破坏美术）。
8. 必须持久化**“已破坏”状态与已获取内容**（防反复刷）。
9. 首个部署/测试节点是 **N1**（战斗节点，战斗期输入本来就是开的），EV 后置。

### 2.2 仍待定

1. 命中粒度：按列命中（戳该列即连带砸箱，实现最简）还是必须点击箱体本体才生效（需额外做点击点判定）。木箱本轮实现默认按列命中。

## 3. 资产落地

### 3.1 目录

```text
Assets/Experiments/CurvedScroll/Art/EnvironmentV2/Props/EV/Prop_Crate_base_v1.png
Assets/Experiments/CurvedScroll/Art/EnvironmentV2/Props/EV/Prop_Crate_BreakAtlas_v2.png
```

理由：与 EV 场景美术同源，沿用现有 `EnvironmentV2/Props` 结构；本轮是 EV 装饰，不进 `Assets/Sprites`（角色/特效区）。

### 3.2 导入设置（与现有场景装饰一致）

- 基图：Sprite (Single)，PPU **100**，Filter **Point**，Compression **Uncompressed**，MeshType **FullRect**，Pivot **Custom = 底部中心**（1024² 下约 `(518, 959)`，即 Alpha 包围盒底边）。
- 组图：Sprite Mode **Multiple**，按 512×512 切 6 个 Sprite，命名 `Prop_Crate_Break_1..6`；Pivot **Custom = (256, 400)**（方案 §4 定义的接地参考点，六格共用）；PPU 100、Point、Uncompressed、FullRect。
- maxSize 需覆盖 1536×1024（否则会被压到 1024 导致切片错位）。

### 3.3 世界尺寸与密度归一化（关键，已按“禁止破坏段缩放”改写）

实测比例：

- 基图（`alpha>8`）包围盒 **936×855 px**。
- 组图格 1 箱体 **387×339 px**。
- 同 PPU、同 transform scale 下，破坏帧只有常态的约 **41%（宽）/ 40%（高）**，即破坏瞬间会“缩小”近六成，不可接受。

因为破坏段不允许缩放，密度差只能落在**导入 PPU**（不是运行时 scale，也不是重采样）：

| 方案 | 组图 Sprite PPU | 残留差异 |
|---|---|---|
| 按高度对齐（推荐） | 100 × 339 ÷ 855 ≈ **39.7** | 宽差约 4.3% |
| 按宽度对齐 | 100 × 387 ÷ 936 ≈ **41.3** | 高差约 4.0% |

- 基图保持 PPU 100、Pivot 底部中心（约 `(518,959)`）；组图六个 Sprite 保持 Pivot `(256,400)`。
- 根节点只有一个 scale（按目标世界高 1.6–1.8 反算，约 0.19–0.21），**子物体不再做任何缩放**。
- 残留 4% 宽高比差是因为模型重绘的箱体与基图比例不完全一致（936:855 vs 387:339）；部署后箱体在屏幕上约 120px 宽，4% ≈ 5px，建议按高度对齐、宽度差接受。
- 两边源图都远高于屏幕分辨率（基图 7.8×、组图 3.2× 过采样），PPU 差异在最终尺寸下不会产生可见的颗粒突变。

**不做**：不改基图像素、不重采样组图、不在破坏瞬间改 transform scale。

### 3.4 切片后的自检项

- 6 个 Sprite 的 pivot 全部为 (256,400)，世界接地点一致。
- 逐帧核对箱体可见高度：格 1 应等于常态箱高 × (339/855)，偏差 >5% 说明切片框取错。

## 4. 场景部署

### 4.1 第一阶段：N1（先跑通，便于测试）

- N1 是战斗节点（`Node_N1.asset` 有 `battleEntries`，`savePoint: 1`），战斗期 `BattleYRouteHost` 本来就 `SetInput(true)`，**不需要改输入门禁就能测**。
- 容器：在 `YJunctionSample.unity` 的 `N1 ` 组下新建 `N1_Props`，道具作为其子物体；位置写死，不做运行时随机。
- 摆放要求：箱体放在目标列的纵线上，使戳击射线正好穿过它；`y` 取战斗地面（`BattleYRouteHost.enemyFootGroundY = -1.8` 对应的高度，实施时以场景实测为准）。
- **必须做一次标定**：运行时打印 5 列的敌人世界 X 与箱体世界 X，确认箱体落在某一列的 X 上；不要靠目测。因为敌人列位置由 `RowFormation.GetColumnOffsetX` / `StageController.GetFormationOffset` 在 Battle.scene 算出，而箱体在附加的路线场景里。
- 排序：`sortingLayer 0`；`sortingOrder` 与相邻 N1 装饰一致（现有场景只出现 `-100 / -90 / 0`），若被草簇或路侧物遮挡则微调。
- 不遮挡主路中线、不与既有装饰穿插。

### 4.2 第二阶段：EV（后续）

- 容器改为 `EV Village Outskirts ` 下的 `EV_Props`；位置参照 `EVApproach_Cart`（`localPosition (6,0,-18)`、`scale 0.34`、`sortingOrder 0`）。
- EV 是纯行进节点且行进期 `SetInput(false)`，又因为交互非强制，只有两种可行做法（见 §5.1），必须先选其一再摆位。

## 5. 交互实现路线（核心难点）

### 5.1 前提：交互非强制，但必须有输入窗口

`BattleYRouteHost` 在节点 travel 阶段 `SetInput(false)`。既然交互非强制（玩家可点前进离开），可用形式只有两种：

- **N1（第一阶段，推荐）**：直接用战斗期已有的输入。战斗时 `SetInput(true)`，玩家戳击目标列时连带打碎箱体；战斗结束进 `Waiting rewards` / `Confirm exit` 时输入关闭，箱体就当景。第一阶段不改任何输入门禁。
- **EV（第二阶段）**：需要额外开窗口。可选：
  - (a) 在 EV 的 `Confirm exit`（`IsWaitingForExit = true`）阶段临时 `SetInput(true)`；玩家可以戳箱，也可以直接点前进离开。符合“非强制”。
  - (b) 行进期间对道具单独开一条不受 `gameplayInputEnabled` 管理的轻量输入（不播完整 stab 会更省事，但不满足“打出一记 stab”的表现要求）。

### 5.2 列解析必须用“道具自己的屏幕 X”

行进中道具会在屏幕上移动，`FallbackGetColumn` 按点击点分 5 段会把点击处和道具所在列判错。做法：`BreakableProp.GetColumnByScreenX()` 用自身 `WorldToScreenPoint().x` 走与 `GetColumnFromScreenPosition` 相同的分段规则，命中窗口内以道具列为准。

### 5.3 命中接入点

- 不改 `AttackWave` 的敌人路径。在 `AttackSystem.TryExecuteAttack` 的 `Stab`（必要时加 `Slash`）分支后补一条：若该列存在可破坏道具且在其攻击距离内，则视为命中（`hitAny = true`），播放同一套戳击表现，并把伤害交给道具。
- 道具侧提供 `TakeHit(int columnIndex, float damage, AttackType type)`；不在列内或无耐久则忽略。
- 注意 `Stab/Slash` 不走 `OnAttackPerformed`，不要试图用事件订阅绕开 `hitAny`，否则空挥也会被当成命中。

### 5.4 破坏表现

- 命中不致死：播放受击反馈（轻微缩放/闪白 + 描边闪一下），保留常态图。
- 致死：切到 `Break` 渲染器，按 **12 fps** 播放 6 帧（约 0.42s），末帧停留 1.0–1.5s 后淡出销毁；是否留残骸由用户决定。
- 破坏期间该道具从可交互集合移除，避免重复结算。

## 6. 高亮提示

- 复用 `EnemyOutline.shader`，通过 `MaterialPropertyBlock` 设置：`_OutlineEnabled = 1`、`_OutlineColor`（建议暖金或朱红，与敌人描边区分）、`_OutlineWidth` 在 1.0–3.0 之间按 0.8–1.2 Hz 脉动。
- 每个道具独立 PropertyBlock，不修改共享材质；禁用/销毁时把 `_OutlineEnabled` 归零（既有经验：对象池复用残留描边）。
- 触发条件：进入可交互窗口时开始脉动；已被破坏或窗口关闭即停止。

## 7. 奖励与存档

- 铜钱（木箱当前奖励）：`PlayerState.Instance.AddCoins(coinReward)`。
- 必杀充能（酒缸奖励）：`UltimateSystem.Instance` 已有 `AddEnergy(int)`（自动 clamp 到 `maxUltimateEnergy`）与 `SetEnergyForRouteRestore(int)`；充满等价于 `AddEnergy(maxUltimateEnergy)`，UI 通过 `OnEnergyChanged` 自动刷新。
- “随机道具”当前只落地金币：建议先做成一个最小掉落表（`List<RewardEntry>`，当前只有 Coin），避免以后接真道具时再改签名。
- 表现：复用现有 `OnCoinGained` 的 HUD 反馈，不新增 UI。
- **存档缺口（已定：要持久化）**：`FakeRouteStageSaveSnapshot` 目前没有道具记录。需要在快照里新增一个按道具 ID 的记录表，至少包含：
  - `propId`（场景里写死的唯一 ID，例如 `N1_Prop_Crate_01`）
  - `destroyed`（是否已破坏）
  - `grantedCoins` / `grantedUltimateEnergy`（已获取内容的审计值）
  - 可选：`hitsTaken`（部分血量单位恢复用）
- 恢复流程：进入节点时按快照重建道具状态（已破坏的直接用末帧残墓或不生成，取用户后续决定）；奖励只在首次破坏时结算，已记录的道具不再重复发放。
- 快照版本号需提升并写兼容迁移（没有该字段的旧档按“全部未破坏”处理）。

## 8. Inspector 可追踪字段（符合项目“禁止幽灵引用”规范）

`BreakableProp` 只从 Inspector 取引用，不做 Resources.Load / 字符串查找：

```text
baseSprite            Sprite     常态图
breakFrames           Sprite[6]  破坏帧（按顺序）
maxHits               int        1（木箱已定）
coinReward            int        100（木箱已定）
healAmount            float
columnOverride        int        -1 = 按屏幕 X 自动判定
outlineColor          Color
outlineWidthRange     Vector2    脉动区间
outlinePulseHz        float
breakFrameFps         float      建议 12
holdLastFrameSeconds  float
interactableWindow    bool       是否只在 EV 停顿窗口内可交互
```

## 9. 部署验收清单

- [ ] 两个 Sprite 导入后 PPU/Filter/Pivot 与文档一致，6 个切片 pivot 全部 (256,400)。
- [ ] 场景中箱体世界高 1.6–1.8，与 `EVApproach_Cart` 并排不显突兀；破坏前后世界尺寸无跳变。
- [ ] 不遮挡主路、不与既有装饰穿插；sorting 正确，不被篱笆/车遮挡也无法穿模。
- [ ] 进入 EV 停顿窗口后描边脉动可读；离开窗口即停。
- [ ] 按 1 或 2 击后播放 6 帧破坏，末帧按约定停留/销毁。
- [ ] 铜钱入账且 HUD 变化；回血受上限约束。
- [ ] N1/E0/N3 战斗与既有敌人命中、列解析无回归。
- [ ] 检查点恢复行为符合第 7 节决定。

## 10. 实施顺序（每步可单独回退）

1. 资产导入 + 切片 + pivot 校验（不动场景）。
2. `BreakableProp` 组件与高亮脉动（先在 EV 摆 1 个调试物件）。
3. EV 补给停顿窗口（`BattleYRouteHost` 的 EV 分支）+ 列解析。
4. 命中接入 `AttackSystem`（Stab 分支），验证空挥不误判。
5. 奖励结算 + 存档策略（按用户选择）。
6. 第二个摆位、整体验收。

## 11. 待用户决定

1. EV 行进期要不要“补给停顿”开输入（否则点道具打不出 stab）？
2. 木箱血量定几（酒缸已定 1）？
3. 木箱金币给多少？
4. 检查点恢复后是否保留“已破坏”状态（决定要不要扩 `FakeRouteStageSaveSnapshot`）？
5. 酒缸本轮是否也要部署？它目前**没有可用的破坏美术**（`breakable_props_break_v1/wine_jar/Prop_WineJar_Break_2..4.png` 已被判方向错误，且只有 3 帧、无 5/6）。

## 12. 未做的事

未改任何代码、未改场景、未导入资产、未建 Prefab。本文只是方案。
