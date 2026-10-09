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
2. Unity 资产冲突用**结构化合并**而非行级合并：本机可用 Locus 的 merge 能力（`Locus/skills/merge`）做字段/对象级选择；仓库根已新增 `.gitattributes`，把 `*.scene *.unity *.prefab *.asset *.controller *.anim *.mat *.meta` 指向 `tuanjieyamlmerge`（Tuanjie 版 SmartMerge），远距离改动可自动合并。**每台机器需一次性配置该驱动，且必须带 `--force`**（原因与命令见第 7 节）；未配置时属性会静默退回行级合并，仍可能产出"能合但语义错"的结果。
3. 接线（只有装配人动）：把新角色写进测试关卡/正式关卡的 `StageConfig.enemyIds`、按需在 `Enemy.cs` 加公共分支、更新 `StageRegistry.asset`。
4. 汇合后必须由装配人在**一台机器上打开工程跑 Play Mode**，不能靠 grep 或静态检查宣告完成。

### 双设备日常同步 SOP（单人双设备）

一次性：另一台机器按第 7 节「让另一台机器跟上」迁到重写后的历史，并配置 SmartMerge 驱动。此后每轮往返：

```bash
# ---- 在 B（提交并上传）----
cd <项目目录>                  # 建议等 Unity 导入完成，避免刚生成的 .meta 还在写
git status                     # 确认没有遗留冲突未处理
git pull --rebase              # 先对齐远程，避免 push 被拒
git add -A
git commit -m "feat(敌人1012): ..."
git push

# ---- 在 A（同步 B 的改动）----
git fetch origin --prune
git status -sb                 # 看是否 behind
git merge --ff-only origin/route-scroll-movement   # 或 git pull --rebase
# 回到 Unity，让它重新导入被改动的资产
```

规则：

- 两台设备固定用同一条开发分支（当前 `route-scroll-movement`），不要各自开长分支；单人双设备用 `pull --rebase` 保持线性历史最省事。
- **禁止 `push --force`**（历史已是共享的）；只有本机尚未推送的提交才能 rebase。
- 不要在两台机器之间手工拷贝 `Assets/` 目录：`.meta` / GUID 会带出"同名不同 GUID"或"同 GUID 两份资产"，一律走 git。
- 另一台机器如果还有**基于旧历史的未推送提交**，先导出补丁（`git format-patch`），迁移后再 `git am` 重放；详见第 7 节。
- 冲突处理：Unity 资产已配 SmartMerge，远距离改动会自动合并；真冲突会标 `UU` 并在输出里给出字段路径，此时用 Locus 的结构化合并或人工裁决。

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

- 所有 commit hash 都已改变：**已存在的克隆不能 `git pull`，只能重新克隆或按下方迁移步骤硬切**。
- 本机已是部分克隆（`remote.origin.promisor=true`、`partialclonefilter=blob:none`）；第二台机器建议 `git clone --filter=blob:none`，首次克隆与后续拉取只取当前需要的文件。

### 让另一台机器跟上（迁移步骤）

旧克隆自己不会知道历史被重写，直到它 fetch。判定标志：`git fetch` 输出里出现 `+ <旧sha>...<新sha> (forced update)`。此时**任何 `git merge` / `git pull` 都会把旧历史混回来**，只能 reset 或重新克隆。

方案 A（推荐，最干净）：先导出旧克隆里未推送的改动，再重新克隆

```bash
# 在旧克隆里保存未推送的工作
git format-patch origin/<branch> -o /path/to/patches   # 未推送的提交
git diff > /path/to/local-work.patch                   # 未提交的改动

# 重新克隆（可删掉旧目录）
git clone --filter=blob:none git@github.com:XwXchaser/threeKingdomSlayer.git threeKingdomSlayer
```

方案 B（原地切换，适用于旧克隆里有必须保留的工作）

```bash
# 1) 先抢救未推送的工作（旧历史上的提交不会被自动带过来）
git format-patch --stdout origin/<branch> > unpushed.patch
git diff > local-work.patch

# 2) fetch 会显示 forced update，这就是历史被重写的信号
git fetch origin --prune

# 3) 硬切到重写后的历史；不要在旧历史上提交，也不要强推旧历史
git checkout <branch>
git reset --hard origin/<branch>

# 4) 丢弃重写前的旧对象（旧克隆里约 500 MB）
git reflog expire --expire=now --all && git gc --prune=now

# 5) 需要时把第 1 步的补丁重放到新历史
git am unpushed.patch
```

**每台机器的一次性本地配置**（git 本地配置不随仓库同步）：

```bash
TOOL="C:/Program Files/Tuanjie/Hub/Editor/2022.3.62t7/Editor/Data/Tools/TuanjieYAMLMerge.exe"
git config merge.tuanjieyamlmerge.name   "Tuanjie SmartMerge"
git config merge.tuanjieyamlmerge.driver "\"$TOOL\" merge -p --force %O %B %A %A"
```

### 已完成：`.gitattributes`（提交 `bd7866a`）

- `* text=auto`：索引内统一 LF，两台机器的 `core.autocrlf` 不再影响入库内容。已核实索引里 0 个 CRLF 文件，因此**没有产生全量重规范化的改动**。
- 二进制标记（`binary` = `-diff -merge -text`）：`*.png *.psd *.jpg *.tga *.exr`、`*.wav *.mp3 *.ogg`、`*.mp4 *.mov`、`*.ttf *.otf`、`*.fbx *.obj *.blend`、`*.dll *.so *.pdb *.mdb`、`*.apk *.aab *.zip` 等。
- Unity 文本资产走 SmartMerge：`*.scene *.unity *.prefab *.asset *.controller *.overridecontroller *.anim *.mat *.mixer *.playable *.mask *.meta`。注意工程场景用的是 `.scene`（3 个）而不是默认的 `.unity`（6 个），两者都已覆盖。
- 实测属性解析：`Battle.scene` / `Enemy_1011.controller` / `Enemy_1011.prefab` → `merge: tuanjieyamlmerge`、`text: auto`；`BOSS.psd` / `battle_ev.wav` / 字体 ttf → `merge: unset`、`text: unset`。

### SmartMerge 实测结论（含一个必需的 `--force`）

用工程真实资产逐个验证了工具行为，结论如下：

- Tuanjie 的 SmartMerge 是 `TuanjieYAMLMerge.exe`（UnityYAMLMerge 的改名版），位置 `C:/Program Files/Tuanjie/Hub/Editor/2022.3.62t7/Editor/Data/Tools/`。
- 它**按文件扩展名派发处理器**。Git 传给 merge driver 的临时文件名形如 `<file>.prefab_BASE_<n>`，扩展名变成 `prefab_BASE_n`，工具直接报 `Couldn't locate merge tool to handle extension ...` 并返回 1。**这就是不加 `--force` 时驱动在 Git 里永远失败的原因**（直接调用同名文件反而成功，容易被误判为配置正确）。
- 加 `--force` 后不再依赖扩展名派发。远距离改动（两侧改不同字段）实测六类全部自动合并、返回 0、双方修改均保留：

  | 类型 | 直接调用 | 经 Git 驱动 |
  |---|---|---|
  | `.prefab` | 合并成功 | 合并成功 |
  | `.scene` | 合并成功 | 合并成功 |
  | `.asset` | 合并成功 | 合并成功 |
  | `.controller` | 合并成功 | 合并成功 |
  | `.anim` | 合并成功 | 未单独跑（同 YAML 格式） |
  | `.mat` | 合并成功 | 未单独跑（同 YAML 格式） |

- 真正同行冲突时返回 1：Git 将文件标为 `UU`，工具会在输出里列出冲突字段路径（例如 `Left  688460577474092011.GameObject.clashed`），此时改用结构化合并或人工裁决。
- 官方自带的 `mergespecfile.txt` 里的回退工具（PlasticSCM / Beyond Compare 等）本机都没装，所以未加 `--force` 时任何 Unity 资产合并都会失败；不需要安装这些工具。

### 决定：暂不启用 LFS

- 体积已可控（仓库 402 MB，PNG 历史 236 MB）；拉取成本已由部分克隆覆盖：不检出旧提交就不会下载旧图。
- GitHub 免费额度是 LFS 存储 1 GB + 每月流量 1 GB。把 552 个 PNG 与 PSD（约 300 MB）迁入 LFS 后，每次新克隆都要走一遍 LFS 流量，两台机器几次重克隆就可能打满，超限会**直接阻塞推送**。
- 迁移 LFS 等于再做一次历史重写 + 强制推送 + 两台机器重新克隆，不必紧接着再来一轮。
- 重新考虑的触发条件：PNG/PSD 历史超过约 1.5 GB，或克隆耗时成为日常阻碍。届时步骤：两台机器安装 git-lfs → `.gitattributes` 加 `*.png filter=lfs diff=lfs merge=lfs -text`（PSD 同理）→ `git lfs migrate import --include="*.png,*.psd" --everything` → 强制推送 → 两台机器重新克隆。

### 清理结果

- 已删除误建的 `nul` 文件（34 字节的 shell 重定向残留）。
- `Assets/Library.meta` 与 `Assets/Library/Locus/**` **未删除**：核查后确认那不是垃圾，而是 Locus 自己的知识索引缓存（`knowledge_index.db`、tantivy 索引、版本标记），正在被当前会话使用；其内部文件已被 `.gitignore` 的 `Library/` 规则忽略，仅文件夹 `.meta` 处于未跟踪。删除没有收益且可能破坏运行中的索引；彻底清理应把该缓存迁到 `<repo>/Library/Locus` 并重开工作区，属 Locus 侧配置调整。

## 8. 交接文档模板要求

每个角色的交接文档照 `plan/enemy1011-animation-deployment-handoff.md` 结构编写，至少包含：§0 当前结论（资产路径 + 验收状态）、精灵清单、Clip 参数表、Controller 状态表、代码播放的状态名字符串、独有机制与其组件、已知工程约束（scale/碰撞盒/血条/画布）、Play Mode 验证记录、关键文件索引。

## 9. 公共文件改动登记

任何人改动了 `Enemy.cs`、`Battle.scene`、`StageRegistry.asset`、`ProjectSettings/*` 或共用关卡资产，都在此登记：改动人、日期、文件、接口/字段、原因。

| 日期 | 改动人 | 文件 | 内容 |
|---|---|---|---|
| （待填） | | | |

## 10. 待确认项

1. 第 2 节的 ID 登记表（角色名 + ID + 制作人）：由用户自行登记。
2. 已完成：`.gitattributes`（`bd7866a`）、`*.apk`/`*.aab` 忽略规则、历史重写与强制推送。LFS 经评估暂不启用，重启条件见第 7 节。
3. 每台机器仍需执行一次 SmartMerge 驱动配置（命令见第 7 节）；第二台机器按第 7 节「让另一台机器跟上」重新克隆或原地切换。
