---
id: kd_ff4efaa3-1820-41ca-a10c-3650275656d5
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 双角色并行制作与版本汇合契约

> **用途**：两名成员在不同主机上各制作一个新敌人角色（像素精灵管线），并各自配好独有动画机状态与独有机制，最后汇合到同一个版本。本文件是开工前的共同依据：ID 分配、目录命名、参数冻结、状态契约、分支与汇合流程、验收清单。
>
> **边界**：本文件规定"怎么协作"，不规定具体角色的动作设计。角色自身的交付内容以各自的动画部署交接文档为准（模板见 `plan/enemy1011-animation-deployment-handoff.md`）。凡本文件出现的实测结论（ID、导入参数、代码行为）均已核对工程；未核对的推测已明确标注。

## 1. 为什么这个工程适合并行

- **角色注册是扫描式的，没有中心登记表**：`EnemyPool.Awake()` 执行 `Resources.LoadAll("EnemyPrefabs")`，再从文件名解析 ID（`Assets/Scripts/Enemy/EnemyPool.cs:70-135`）。新增角色 = 纯新增文件，不需要改任何公共注册代码，也不需要在别人文件里插一行。
- **编辑器工具同样扫目录**：战斗数值总表 `Assets/Scripts/Editor/CombatDataTableWindow.cs`、攻击命中帧校验 `Assets/Scripts/Editor/AttackHitFrameValidator.cs` 都按 `Assets/Resources/EnemyPrefabs` 目录遍历。新角色自动出现在表里。
- **控制器是按角色独立的**：每个敌人有独立 `.controller`（如 `Assets/Animations/Enemy_1011.controller`），不存在所有人共用一个状态机的结构性冲突。
- **结论**：只要守住"目录隔离 + 只在一个地方改公共文件"，两人分支的合并几乎是纯新增，Git 层面基本不会冲突；真正的风险是**共用文件被同时修改**和**视觉参数各自发挥**。

## 2. 已占用敌人 ID（实测）

| ID | Prefab / 证据 | 说明 |
|---|---|---|
| 1 | `Enemy_1.prefab` | 早期单位 |
| 101 | `Enemy_101.prefab`，`Stage_1.asset` 使用 | 通用骷髅兵基准 |
| 102 | `Enemy_102.prefab`，`Enemy_102.controller` 含 `CowardIdle` | 惧战兵；`Enemy.cs` 里有专属状态集（`IsCoward`） |
| 103 / 104 / 105 / 106 | `Enemy_103/104/105/106_Fixed.prefab` | 104 有 `Boss_104.controller`；106 有 `Enemy_106_RowFlight.asset` |
| 107 / 108 | `Enemy_107/108.prefab` | BOSS |
| 109 | `Enemy_109.prefab` + `CavalryEnemy.cs` | 骑兵，专属机制组件 |
| 1011 | `Enemy_1011.prefab` | 101 的变体（持剑） |

**ID 分配规则（开工前必须在此登记）**

- 基础角色使用三位数段：建议 `110`、`120`（避开 101–109）。
- 该角色的后续变体沿用既有惯例，以基础 ID 为前缀追加一位：`101 → 1011`，因此 `110 → 1101/1102`。
- Prefab 文件名必须为 `Enemy_<id>.prefab`；解析逻辑是"`Enemy_` 之后读取连续数字直到非数字字符"，所以 `Enemy_110_V2.prefab`、`Enemy_106_Fixed.prefab`（既有先例）都能正确解析为 110、106。
- 同一个 ID 不能指向两个 Prefab：`RegisterPrefab` 遇到重复 ID 会**静默保留先注册的那个**（`EnemyPool.cs:159-164`），文件名撞号会出现"改了没生效"的假象。

| 角色 | 制作人 | enemyId | 状态 |
|---|---|---|---|
| （待填） | A | 110 | 待登记 |
| （待填） | B | 120 | 待登记 |

## 3. 每个角色的产物清单与目录契约

| 产物 | 路径与命名 | 归属 |
|---|---|---|
| 精灵帧 | `Assets/Sprites/Enemy/<角色名>/` | 各自新增目录，互不触碰 |
| 动画 Clip | `Assets/Animations/Enemy_<id>_<Action>.anim` | 各自新增文件 |
| 动画控制器 | `Assets/Animations/Enemy_<id>.controller` | 各自新增文件 |
| Prefab | `Assets/Resources/EnemyPrefabs/Enemy_<id>.prefab` | 各自新增文件 |
| 角色专属脚本 | `Assets/Scripts/Enemy/<角色名><功能>.cs` | 各自新增文件（照 `CavalryEnemy.cs` / `CavalryVisualController.cs` 模式） |
| 自己的测试入口 | `Assets/Scenes/<角色名>Test.scene`、`Assets/Resources/StageConfigs/Stage_<角色名>_Test.asset` | 各自新增文件，不共用 |
| 交接文档 | `Locus/knowledge/plan/<角色名>-animation-deployment-handoff.md` | 各自新增文件 |

Prefab 必须包含的组件（对齐 `Enemy_1011.prefab` 实测结构）：`Enemy`、`SpriteRenderer`、`BoxCollider`、`EnemyHealthBar`、`Animator`、攻击预警锚点（`EnemyAttackTelegraph`）。

## 4. 视觉与导入参数契约

**跨角色统一（全项目固定）** —— 已实测 `Enemy_2.png.meta` 与 `Enemy_1011_idle1.png.meta` 一致：

```text
spritePixelsToUnits (PPU) = 16
filterMode = 0 (Point)
textureType = 8 (Sprite) / spriteMode = 1 (Single)
alphaIsTransparency = 1，Mipmaps 关闭
压缩 = Uncompressed
```

**同角色内统一、跨角色不统一**：

- 画布尺寸按角色自定（实测：`Enemy2` 系列 512×512；`Enemy_1011` 系列 1108×1108）。
- pivot 按角色自定（实测：`Enemy2` 为 `0.5/0.5`；`Enemy_1011_idle1` 为 `0.65392774/0.5487343`，因为剑向画面左侧横伸需要偏移）。
- 规则：**先确定该角色 idle 首帧的画布与 pivot，随后该角色所有动作帧复制它**；禁止逐帧按包围框 trim 或缩放（会把姿态本身抹掉，见 `design/pixel-character-and-enemy-animation-spec.md` 第 6 节与 `skill/workflows/image-asset-generation.md` 的尺寸密度约定）。
- `.meta` 与 GUID 由 Unity 生成，不手写、不跨机拷贝资产文件夹。

**动画节奏基线**（可偏离但需在交接文档写明）：30fps、每动作 6 帧、stopTime 0.6/0.9、Idle 与 Fall 循环、Attack/Dead/Hit 系列不循环。

## 5. Animator 与状态契约（本次重点）

因为两个角色有**各自的动画机状态设计**，必须区分"公共状态"和"角色独有状态"。

### 5.1 代码实测事实

- `Enemy.cs` 用**字符串**播放状态，公共集合为：`Idle`、`Attack`、`HitFlash`、`HitLeft`、`HitRight`、`Dead`、`Walk`、`Launched_Rise`、`Launched_Fall`、`Launched_Getup`（另有 `ReRise` 用于空中二次受击）。
- **缺失可选状态会优雅降级**：`PlayHitVisual` 在控制器里找不到 `HitLeft/HitRight` 时自动回退到 `HitFlash`（`Enemy.cs:876-895`）。
- **击飞链是硬依赖**：`HasGetupAnimatorState()` 检查 `Launched_Getup` 是否存在，直接决定落地后是否播起身（`Enemy.cs:902-917`）；`Launched_Rise`、`Launched_Fall` 同理属于全局机制。新角色必须实现这三段，否则击飞表现退化。
- 角色可以拥有**自己的状态集**，已有两个工程先例：
  - `IsCoward`（102 惧战兵）：`CowardIdle / CowardHit / CowardGetup / CowardDead` 由 `Enemy.cs` 内部分支播放；
  - `CavalryVisualController`（109 骑兵）：用**多个 `RuntimeAnimatorController` 序列化字段**做视觉路由，按自己的机制阶段切换控制器，完全不动 `Enemy.cs`。

### 5.2 规则

1. **不得改名或挪用公共状态名**去做别的语义。状态名是跨机器的硬契约，改名的失败表现是"动画部署了但播不出来"。
2. **角色独有状态用角色前缀命名**（如 `RaiderIdle`、`RaiderWindup`），放在自己的 `.controller` 里；共用控制器不需要、也不应该包含它们。
3. **独有状态由角色专属脚本驱动**，优先照 `CavalryVisualController` 模式：把需要的控制器作为 `[SerializeField] RuntimeAnimatorController` 挂在角色新组件上，自己按机制阶段切换。这条路线**不需要改 `Enemy.cs`**，是并行开发的首选。
4. **公共状态集不够用时才改 `Enemy.cs`**，且必须：集中由一个人做、单次提交、在本文件第 9 节登记改动点。禁止两人各自在 `Enemy.cs` 里加自己的分支。
5. 独有机制同样优先做成新组件（照 `CavalryEnemy.cs`），挂在 Prefab 上并只读 `Enemy` 的公开接口；跨角色都要用的机制改动由装配人统一实施。

## 6. 分支、隔离与汇合流程

### 阶段 0：开工前（一次性）

1. 在本文件填好第 2 节的 ID 登记表。
2. 双方从**同一个 integration 基线 commit** 切分支，不要从各自的历史分叉点开始。
3. 确认本机引擎版本完全一致：当前 `2022.3.62t7`（Tuanjie 1.8.5）。版本不一致会触发全量重导和序列化漂移。

### 阶段 1：各自开发

- 分支：`feat/enemy-<id>-<角色名>`。
- **允许触碰**：第 3 节表格中自己名下的新增文件。
- **禁止触碰**：`Assets/Scenes/Battle.scene`、`Assets/Scripts/Enemy/Enemy.cs`（除非走第 5.2 节第 4 条）、`ProjectSettings/*`、`Assets/Resources/StageRegistry.asset`、他人名下的关卡与资产目录、共用测试关卡 `Assets/Experiments/CurvedScroll/RouteTrialData/SmallBattle.asset`。
- 提交粒度：按动作或按阶段提交（idle 一个、attack 一个、hit 一个…），不要把整套几十 MB 精灵塞进一个 commit。

### 阶段 2：汇合（装配人执行）

1. 在 integration 分支合入两人的分支。新增文件基本是净增，Git 不会报冲突；若共用文件被两边都改，按"装配人裁决"处理。
2. Unity 资产冲突用**结构化合并**而非行级合并：本机可用 Locus 的 merge 能力（`Locus/skills/merge`）做字段/对象级选择；若走原生 Git，需为 `*.unity *.prefab *.asset *.controller *.anim *.mat` 配置 UnityYAMLMerge（当前 git 配置里**没有任何 merge driver**，行级合 YAML 会产出"能合但语义错"的结果）。
3. 接线（只有装配人动）：把新角色写进测试关卡/正式关卡的 `StageConfig.enemyIds`、按需在 `Enemy.cs` 加公共分支、更新 `StageRegistry.asset`。
4. 汇合后必须由装配人在**一台机器上打开工程跑 Play Mode**，不能靠 grep 或静态检查宣告完成。

### 阶段 3：验收

每个角色与整体各跑一遍：

- [ ] 导入设置回读：PPU 16、Point、Uncompressed、Sprite、alphaIsTransparency、pivot 与同角色 idle 首帧一致。
- [ ] Clip：帧数、fps、stopTime、Loop 与交接文档一致。
- [ ] Controller：公共状态接线齐全，`Launched_Getup` 存在（否则击飞落地退化）。
- [ ] 运行时：出场、正面/左/右受击、攻击命中帧、死亡、击飞 Rise→Fall→落地→Getup→回普通行为。
- [ ] 独有状态与独有机制的触发条件、打断恢复、对象池回收。
- [ ] 脚底锚点、碰撞盒、血条位置、与相邻列的重叠。

## 7. 仓库体积与拉取速度

**重写前实测**（`git rev-list --objects --all` + `git cat-file --batch-check`，唯一 blob 字节数）：

| 类别 | 体积 | 当时状态 |
|---|---|---|
| 历史总唯一 blob | 1891 MB | 打包后 `.git` 932 MB（13 个 pack） |
| `Assets/Wwise/**` | 1266 MB | 已在 59bca96e 从工作区删除，但永久留在历史里（`.pdb`/`.dSYM`/`.so` 单文件可达 79 MB） |
| 插件二进制（pdb/so/bundle/dll） | 895 MB | 主要来自 Wwise |
| PNG | 236 MB | `Assets/Sprites` 占 226 MB |
| `BOSS.psd` 等 PSD | 47 MB | 单文件 42.9 MB，仍是源美术，保留 |
| `threeKingdomSlayer.apk` | 40 MB | 曾跟踪在 HEAD |

**已执行（2026-10-08）：历史重写，剔除 Wwise 与 apk**

- 工具：`git filter-repo 2.47.0`（`--invert-paths`）。
- 剔除路径：`Assets/Wwise/**`、`Assets/Wwise.meta`、`threeKingdomSlayer_WwiseProject/**`（遗留 Wwise 工程 23.6 MB）、`Assets/Scripts/Managers/WwiseAudioManager.cs(.meta)`、`threeKingdomSlayer.apk`。
- 结果：`.git` 从 932 MB 降到 402 MB；13 个本地分支全部重写，11 个远程分支强制推送成功；重写后的主分支内容与远程原内容逐文件一致（仅少了 apk 与 Wwise）。推送时远程有两个来自另一台机器的提交（`68f7843b`、`a08bc487`）被重放到重写后的历史上。
- `.gitignore` 修正：原规则写的是 `.apk`（只匹配名为 `.apk` 的文件，实际不生效），已改为 `*.apk` / `*.aab`。apk 文件仍保留在磁盘但被忽略。
- 备份：`C:/threeKingdomSlayer-history-backup-20261008.git`（硬链接镜像，含重写前全部 68 个引用，必要时可回溯）。

**对两台机器的后果（重要）**

- 所有 commit hash 都已改变：**任何已存在的克隆必须重新克隆**，不能只 `git pull`。第二台机器建议 `git clone --filter=blob:none`（部分克隆），首次克隆与后续拉取只取当前需要的文件。
- 本机已是部分克隆（`remote.origin.promisor=true`、`partialclonefilter=blob:none`）。
- 未启用 LFS：LFS 不缩小单次下载体积，但能让大图不参与 pack。若后续启用，应一次性协调完成。
- 仍未处理（需一次性协调提交）：`.gitattributes` 把 `*.png *.psd *.wav *.mp4 *.ttf` 标为 binary、为 `*.unity *.prefab *.asset *.controller *.anim *.mat` 配置 UnityYAMLMerge、统一 `text=auto eol=lf`（本机 `core.autocrlf=true`，与第二台机器不一致会产生整文件假 diff）。
- 顺带清理：工作区仍有未跟踪的 `nul`、`Assets/Library.meta`。

## 8. 交接文档模板要求

每个角色的交接文档照 `plan/enemy1011-animation-deployment-handoff.md` 结构编写，至少包含：§0 当前结论（资产路径 + 验收状态）、精灵清单、Clip 参数表、Controller 状态表、代码播放的状态名字符串、独有机制与其组件、已知工程约束（scale/碰撞盒/血条/画布）、Play Mode 验证记录、关键文件索引。

## 9. 公共文件改动登记

任何人改动了 `Enemy.cs`、`Battle.scene`、`StageRegistry.asset`、`ProjectSettings/*` 或共用关卡资产，都在此登记：改动人、日期、文件、接口/字段、原因。

| 日期 | 改动人 | 文件 | 内容 |
|---|---|---|---|
| （待填） | | | |

## 10. 待确认项

1. 第 2 节的 ID 登记表（角色名 + ID + 制作人）。
2. 是否引入第 7 节末尾的 `.gitattributes` 与 LFS。
3. 历史重写已于 2026-10-08 完成并强制推送，第二台机器需重新克隆（见第 7 节）。
