---
id: kd_2c1daab6-34e3-490c-b6ba-63e652f50af6
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---
# 落地页按钮排布改造方案（参考「露米娅大陆」界面）

> **2026-10-24 补充：整张 UI 参考图生成交接见 `Locus/knowledge/plan/hub-ui-reference-generation-handoff-20261024.md`。** 用户已同意先做我们画风的完整竖屏落地页参考图，再考虑拆件与场景落地。重新读原图确认：关卡入口在右下、家园卡上方；主操作在下半部。本轮生成方向为“右下关卡入口＋其下天赋卡、左下任务条＋四宫格、下半部居中开始按钮”，只保留六项 UI，无头像、星级、体力或 tab。后文“24%—41% 关卡入口”等历史坐标不用于本轮生成。新文档含中英文提示词与验收口径；本轮仅写交接，未生成图片，未改场景，以下快照仍是历史工程状态。

> 参考图：`C:/Users/Administrator/Downloads/微信图片_20261019125149_30_455.jpg`（1116×2480）。
> 本文是排布 + 素材的定稿方案，**未改代码**。
> 相关：`plan/main-menu-hub-refactor-plan-20261020.md`（落地页自举与 overlay 体系）、
> `plan/content-development-todo-20261025.md`（需求 4 选关界面）、`design/art-style-guide.md`（画风权威）、`design/task-system-design.md`（任务系统）。

---

## 〇、用户已拍板（2026-10-24，实施必须按这版）

| # | 决策 |
| --- | --- |
| D1 | **底部 tab 栏下线**。主界面就是战斗界面，「战斗」tab 是多余动作（原地传送）。**不许新造 tab** —— 需要的地方只放**置灰占位** |
| D2 | 右侧关卡入口方向确认（缩略图 + 关卡名 + 进度），但形态改为**景色不规则 UI**（见 D7） |
| D3 | 左下那个是**四宫格菜单键**，**功能待定** → 只做置灰占位，**不命名、不编功能** |
| D4 | 顶部**体力位暂不放开**（后续会有）→ 本轮不放任何体力元素 |
| D5 | 左下任务条**不是小占位**：任务内容将来会非常丰富（参考手游 / 小游戏），**需要先规划任务内容与数据架构** → 见 `design/task-system-design.md` |
| D6 | **UI 排布回到最初参考图**（不再自行改造）：头像左上、金币右上、任务条左下、关卡入口右侧中部、开始关卡中下、天赋卡占右下「家园」位。**画面里不要武将** —— 我们游戏本来就没有武将角色 |
| D7 | **关卡入口 = 景色不规则形**：一块被裁下来的战场景色剪影（轮廓即按钮形状 + 金边），下方一条空墨色笔刷带；**关卡名用 TMP 叠加**（底图无字），星级用独立的金星小图 |

---

## 〇之二、当前快照（2026-10-24，交接用）

> 本节是**最新状态**，优先于后文各轮返工记录。后文第三、四、五轮的坐标与素材表是历史过程，仅供追溯。

### 场景搭建状态（`Assets/Scenes/Battle.scene`，已保存，Editor 内未脏）

```
HubCanvas(Canvas)
└── Hub/
    ├── Top/
    │   └── HubCoin                 金币图标 hub_coin + 数值文本「0」（脚本 HubCoinDisplay 待接玩家总铜钱）
    ├── Side/
    │   └── HubStageEntry           关卡入口：Base（hub_stage_entry 景色剪影）+ NameBand（hub_taskbar_brush）+ StageName「—」（占位，未绑数据）
    └── Bottom/
        ├── StartStageButton        「开始关卡」，MM_Btn_Primary
        ├── HubTabBar [停用]        四个 tab（战斗/选关/天赋/家园）；D1 已下线，代码与节点保留
        ├── HubTaskBar              Bar（hub_taskbar_brush）+ Content：Icon 无 sprite 已停用（防白块）/ TextBox「本关目标」「0/0」/ Reward（hub_coin）
        ├── HubMenuPlaceholder      Key（hub_fourgrid_key，置灰占位，功能未定）
BattleHUD(Canvas)  order 21（战斗 HUD，落地页隐藏）
```

- 已删除（第五轮）：落地页头像组、经验条、等级牌、关卡星级。场景中不再有这些节点。
- 工具：`Tools/落地页/` 下四个菜单（搭建落地页 UI / 重构 UI 分层 / 切换落地页 UI 显示 / 拆分落地页 UI 画布），均可重复执行。
- 脚本：`HubStageEntry`、`HubTaskBar`、`HubTalentEntry`、`HubMenuPlaceholder`、`HubCoinDisplay` 已存在于 `Assets/Scripts/UI/`（`.cs.meta` 齐全）。
- 控制台 error/warning：0（本次检查时）。
- 未处理：`BattleHUD` 下的 `BossPortrait` / `BossPortraitFrame`（第五轮 §五之二 #3）仍在场景中，待拍板删除或修正位置。
- 未提交：落地页相关文件与其他功能改动在同一工作区，`Battle.scene` 与 `HubCanvas` 相关脚本尚未单独 commit。

### 美术素材状态

| 文件 | 位置 | 用途 | 状态 |
| --- | --- | --- | --- |
| `hub_coin.png` | `Assets/Sprites/UI/Hub/` | 金币图标（HubCoin / HubTaskBar Reward） | 已导入、已挂载，待目视验收 |
| `hub_stage_entry.png` | 同上 | 关卡入口景色剪影（无字底） | 已导入、已挂载；**造型待定（R1）**，文件 16:27 最后修改 |
| `hub_taskbar_brush.png` | 同上 | 任务条笔刷底 / 关卡名字带 | 已导入、已挂载；Sliced 边框 (40,16,40,16) 已设（第三轮返工后的值，回读核实） |
| `hub_fourgrid_key.png` | 同上 | 四宫格键（置灰） | 已导入、已挂载，待验收 |
| `hub_talent_card.png` | 同上 | 天赋卡底板（深墨 + 金边） | 已导入、已挂载，待验收 |
| `hub_talent_tree.png` | 同上 | 天赋卡发光树图标 | 已导入、已挂载，待验收 |
| `hub_talent_card.png` 以外的完整素材 | `_art_process/hub_ui_v1/` | 原始产物与中间件 | 含头像、经验条、等级牌、星级、`_contact_sheet*.png`、`_new_bits.png` 等；**头像、星级、等级牌素材已不在 Assets 中，仅留在 `_art_process/` 作备份**。`coin_icon` / `talent_tree` / `fourgrid_key` / `taskbar_brush` 与 Assets 内版本同名，导入版以 Assets 为准；`hub_stage_entry` / `hub_talent_card` 在 `_art_process/hub_ui_v1/` 没有同名源文件（关卡入口源为 `stage_entry_notext.png`），来源需补记 |
| `Assets/Sprites/UI/Hub/*.png.meta` | 同上 | 导入设置 | 按第五轮记录设为 Sprite / PPU100 / Point / 无 mipmap / 原生尺寸（npotScale=None）；**本次未逐个回读确认** |

> 美术状态结论：Assets 内 6 张素材全部挂载，但**都还没有完成用户目视验收**；关卡入口造型未定；没有每关一张图（R3）。

### 尚未完成的数据与逻辑

| 项 | 现状 |
| --- | --- |
| 关卡名 | `HubStageEntry.StageName` 为占位「—」，未接 `RouteCatalog` / `SaveManager.GetActiveFakeRouteStageId()` |
| 金币数值 | `HubCoin/Amount` 为「0」，未接玩家总铜钱 |
| 任务条 | 图标已停用、标题「本关目标」为占位，未接任务系统（`design/task-system-design.md`） |
| 天赋卡 | 点击跳转 `SwitchOverlay("TalentTree")` 依赖运行时 Bind，**需 Play Mode 验证** |
| 四宫格键 | 置灰占位，功能未定（D3） |
| 验收 | 截图工具截不到 Overlay UI，须人眼在 Game 视图验收 |

### 待用户决定

R1 关卡入口造型 · R2 素材目视验收 · R3 每关一张图 · R4 任务条内容 · R5 金币左上是否留空 · `BossPortrait` 删除或修正（新增）

---

## 一、参考图解读（抄信息层级，不抄像素）

| 区域 | 参考图 | 作用 | 我们对应 |
| --- | --- | --- | --- |
| 顶部左 | 头像 + 等级 + 经验条 + 红点 | 身份 + 成长 | ✅ 已有（`UltPortraitButton` / `HeroHUD`） |
| 顶部右 | 体力 + 金币 | 资源 | 只有金币（体力位暂留空） |
| 左列 | 商城 / 首充礼包 | 商业化 | 无（不做） |
| 右侧中部 | **「火山」关卡卡**（景色 + 星级） | **当前关卡 + 选关入口** | ★ 新增：景色不规则形入口 |
| 中部 | 捕获率 60% | 核心数值 | 无 |
| 中下 | 「捕捉」大按钮 | **开始当前关卡** | ✅ 已有 `StartStageButton` |
| 左下 | 欢迎条 + **任务条** | 公告 + 当前任务 | ★ 任务条（公告位不做） |
| 底部行 | 3 小 tab + **右下大卡片「家园」** | 平级导航 + 强调入口 | tab 栏下线（D1）；**右下大卡片 = 天赋树入口** |

---

## 二、我们的现状（实测，`Assets/Scenes/Battle.scene`）

```
Canvas order 21  BattleHUD(Canvas)   1080×1920 ScaleWithScreenSize
├─ StartStageButton   anchor 底中  pos(0, 690)   479×172   「开始关卡」
├─ HubTabBar          anchor 底中  pos(0, 40)    960×120   → 自己 order 50 的 canvas（4 个 tab，D1 下线）
├─ CoinCounter        anchor 左上  pos(114,-84)   200×100   ← 参考图在右上，需挪
├─ HeroHUDParent → HeroHUDRoot → UltPortraitButton  anchor 左下 300×300   ← 头像（局内 = 必杀技按钮）
├─ ChargeIndicator    anchor 左下角 120×120   ← 蓄力指示器（局内）
└─ KillMilestoneDisplay / BuffDisplayPanel / BossBars / ComboDisplay / PauseLayer …
```

- 落地页显隐由 `HubHudModeSwitch` 两个维度管：`inRunOnly`（局内专用，就绪态隐藏）/ `hubOnly`（落地页专属，overlay 打开时隐藏）。
- overlay 切换：`SceneFlowManager.SwitchOverlay(场景名)`；选关页 = `StageSelect.scene`（order 40）、天赋页 = `TalentTree.scene`（order 41）；退出靠各界面自己的「返回」按钮 `UIOverlayCloseButton` → `RequestCloseOverlay()`。

---

## 三、定稿排布（就绪态 = 落地页，忠实于参考图）

```
┌──────────────────────────────────────────────┐
│ [头像+等级+经验条]                [金币]      │ ← 左上头像 / 右上金币（体力位暂留空）
│                                               │
│                              ┌──────┐         │
│                              │ 景色 │         │ ← ★右侧中部：关卡入口（景色不规则形 + 笔刷带 + TMP 名 + 星）
│                              │ 入口 │         │
│                              └──────┘         │
│                                               │
│              [ 开 始 关 卡 ]                  │ ← 现有 StartStageButton（底部中上 690）
│                                               │
│ [本关任务条]                                  │ ← ★左下任务条（+ 四宫格置灰键）
│                                   [天赋树]     │ ← ★右下大卡片（天赋入口，占参考图「家园」位）
└──────────────────────────────────────────────┘
```

**不画任何角色**（D6）：中间只有场景（Y 分叉路口实拍背景），不放武将、不放吉祥物。

### 建议坐标（1080×1920 基准）

| 元素 | 锚点 | 位置 | 尺寸 | 画布 |
| --- | --- | --- | --- | --- |
| 关卡入口（景色形） | 右上 | (-40, -360) | 340×300 | BattleHUD(21) |
| 天赋树大卡 | 右下 | (-40, 120) | 300×220 | BattleHUD(21) |
| 任务条 | 左下 | (60, 210) | 640×100 | BattleHUD(21) |
| 四宫格菜单键 | 左下 | (60, 80) | 96×96 | BattleHUD(21) |
| 金币 | 右上 | (-114, -84) | 200×100 | BattleHUD(21)（现锚在左上，需挪） |

---

## 四、会动哪些功能

### 新增组件（4 个）

| 组件 | 职责 | 依赖 |
| --- | --- | --- |
| `HubStageEntry` | 右侧景色形关卡入口：底图 + TMP 关卡名 + 星级 + 「选关」提示；点击 → `SceneFlowManager.SwitchOverlay("StageSelect")` | `RouteCatalog` / `SaveManager` / 星级数据 |
| `HubTalentEntry` | 右下天赋大卡：发光技能树图标 + 「天赋」；点击 → `SwitchOverlay("TalentTree")` | `SceneFlowManager` |
| `HubTaskBar` | 左下任务条：任务图标 + TMP 标题 + 进度 + 奖励；接任务系统 | `design/task-system-design.md` |
| `HubMenuPlaceholder` | 左下四宫格菜单键：**置灰占位**，功能待定 | 无 |

### 场景改动（`Assets/Scenes/Battle.scene`）—— ✅ 2026-10-24 已落地

- 新增 4 个 UI 节点（`HubStageEntry` / `HubTalentEntry` / `HubTaskBar` / `HubMenuPlaceholder`）；停用 `HubTabBar` 节点（D1，代码保留不删）。
- `CoinCounter` 从左上挪到右上（按参考图）。
- **头像位置完全没动**：实测改动前后画布位置都是 x[-499,-199] y[-630,-330]，层级也没动。
  - ⚠️ 但发现一个既有隐患：头像 `UltPortraitButton` 挂在 `Health(Slider)` 下，而血条在就绪态被隐藏 → **落地页根本看不到头像**。
  - 修法（不碰位置、不碰层级）：把 `Health` 从 `HubHudModeSwitch.inRunOnly` 拿掉 —— 血条的 Fill/Area/Background 本来就在隐藏名单里，就绪态只剩头像可见。
  - ⚠️ 头像在 **HeroHUD prefab 实例**里，`HeroHUDParent` 是它的**祖先** → **不能重挂父级**（Unity 会拒绝），坐标也会写歪。别再尝试重挂。
- `HubHudModeSwitch` 新增第三组 **`readyOnly`**（就绪态专属）：就绪态显示、关卡态隐藏、overlay 打开时让位。
  - 原来的两组：`inRunOnly`（局内专用，就绪态隐藏）/ `hubOnly`（落地页常驻，overlay 打开时隐藏）。
  - ⚠️ 新元素**不能只放 hubOnly**：那样关卡态不会隐藏（实测踩过）。
- 工具：`Tools/落地页/搭建落地页 UI`（幂等重建，菜单）→ `Assets/Scripts/Editor/HubSceneSetup.cs`。
- ⚠️ **onClick 监听必须在运行时接**（组件 `OnEnable` 里 `Bind()`）—— 编辑器里加的 UnityEvent 监听**不会被序列化进场景**，运行时是空的（实测踩过）。

### 数据改动

- `RouteCatalogData` / `FakeRouteStageConfig` 加 **关卡缩略图 / 入口图** 引用（Sprite，Inspector 拖拽，守 `design/anti-ghost-reference.md`）。
- 关卡入口底图目前是**通用一张**（长坂坡景观）；正式版应按关卡各出一张，字段按「每关一张图」设计。


### 手动调整位置与尺寸（**不用额外工具**）

2026-10-24 决定：**不做自定义布局工具**。理由与做法：

- **理由**：布局台想做的事，Unity 原生都能做 —— Scene 视图的 RectTransform 工具（W/E/R）直接拖；Inspector 直接填
  `anchoredPosition` / `sizeDelta` / `anchorMin/Max` / `pivot`；Play Mode 的 Game view 就是真实预览。
  自定义工具反而带来偏差（画布实际 960×1920 而非 1080×1920）、只支持非拉伸锚点、还要多维护一份代码。
  → **已删除** `HubLayoutStageWindow.cs` 与配套的 `HubLayoutHandle` 标记组件。
- **怎么调**：
  1. 在 `Battle.scene` 的 `BattleHUD(Canvas)` 下直接勾/取消那 4 个节点的 Active → 开关它们；
  2. 位置与尺寸：Inspector 填数值即可（推荐）；或编辑时把 `BattleHUD(Canvas)` 临时切成
     **Screen Space - Camera**，Scene 视图就能看见并直接拖（**Overlay 画布在 Scene 视图里是不可见的**），
     改完切回 Overlay（运行时渲染顺序不受影响）；
  3. 看效果：Play Mode 的 Game view（落地页 = 就绪态）。
- **保留的工具**：`Tools/落地页/搭建落地页 UI`（`HubSceneSetup.cs`）—— 幂等重建这 4 个节点，
  改坏了点一下就复原，**不是交互工具**。

### 实现方式（Unity 常规做法）

| 元素 | 做法 |
| --- | --- |
| 锚点 | 全部**角/边锚定**（关卡入口 anchor 1,0.5；天赋入口 1,0；任务条 0,0；四宫格 0,0；金币 1,1）→ 屏幕宽高变化自动贴边，不写死绝对坐标 |
| 任务条底图 | `Image.type = Sliced` + 笔刷条设了九宫格边框 (60,30,60,30) → 横向拉伸不变形 |
| 任务条内容 | `HorizontalLayoutGroup`（childControlWidth/Height = true）+ `LayoutElement` 定尺寸；文字用 `VerticalLayoutGroup` 竖排；文字块 `flexibleWidth = 1` 吸收剩余空间 |
| 文本 | TMP（`enableWordWrapping = false`），关卡名压在底图自带的墨色笔刷带上（`NameBand` 底锚定） |
| 按钮 | `Button.targetGraphic` 指向底图 Image；**onClick 监听在运行时 `OnEnable` 里接**（编辑器里加的 UnityEvent 监听不会被序列化进场景） |
| 关卡名数据 | `RouteCatalog` + `SaveManager.GetActiveFakeRouteStageId()` → 实测显示「长坂坡·卷轴」 |

---

## 五、素材产出（2026-10-24 已生成，**在 `Assets` 之外，待目视验收后导入**）

产物目录：`threeKingdomSlayer/_art_process/hub_ui_v1/`；原始大图在 `C:/Users/Administrator/Pictures/gptGen/`。
**2026-10-24 已导入进项目**：`Assets/Sprites/UI/Hub/`（`hub_*.png`），导入设置 = UI 精灵规范
（PPU 100 / Sprite / alphaIsTransparency / **Point**（像素风）/ 无 mipmap / Uncompressed / maxTextureSize 2048 / pivot 中心）。

| 文件 | 尺寸 | 用途 | 状态 |
| --- | --- | --- | --- |
| `taskbar_brush.png` | 507×222 | 左下任务条墨色笔刷底 | 待验收 |
| `avatar_frame.png` | 233×222 | 圆形头像金框 + 红点 | 待验收 |
| `level_plate.png` | 255×157 | 等级牌底 | 待验收 |
| `exp_bar.png` | 474×157 | 经验条 | 待验收 |
| `start_button.png` | 473×186 | 「开始关卡」朱红笔刷按钮底 | 待验收 |
| `fourgrid_key.png` | 191×186 | 四宫格菜单键（置灰） | 待验收 |
| `coin_icon.png` | 212×250 | 金币图标 | 待验收 |
| `talent_tree.png` | 249×250 | 发光技能树图标（天赋入口用） | 待验收 |
| `stage_entry_notext.png` | 958×805 | **关卡入口无字底**（景色剪影 + 金边 + 空笔刷带） | 待验收，**造型待定** |
| `stage_entry_scenery.png` | 856×716 | 关卡入口（带烘焙名字的旧版，仅参考） | 备用 |
| `star_0/1/2.png` | 288×286 | 空心星 / 半亮星 / 满星 | 待验收 |

**抠图方法**（可复用）：洋红底 → 混合度 `m = min(R,B)/255*(1-G/255)`；`alpha = clip(1-(m-0.18)/0.35)`；
**暗像素（max<70）判为不透明**；只对真混合像素做反预乘；alpha=0 处 RGB 清零；清最外 2px 残留。
直接用 `alpha = 1-m` + 全量反预乘会把墨色算成**绿色**（中心采样 (0,255,0)）——判据必须先分开「暗色不透明」与「真混合」。

**导入 Unity 时的约定**（按 `skill/workflows/image-asset-generation.md`）：不改导入设置（PPU / pivot / filter / compression 全项目统一），
密度差放单位 scale；这些是 UI 精灵，按项目既有 UI 精灵设置导入。

---

## 四之一、UI 全量遍历与分层重构（2026-10-24）

### 遍历结论（Battle 场景 UI 清单）

原来 31 个 UI 根节点平铺在 `BattleHUD(Canvas)` 下，显隐靠 `HubHudModeSwitch` 的**三个平面数组按名字匹配**
（`inRunOnly` / `hubOnly` / `readyOnly`），问题：

| 问题 | 具体 |
| --- | --- |
| 战斗 UI 漏到落地页 | `HeroHUDParent → HeroHUDRoot → HudCard`（血条 / 100-100 / 经验条 / 技能键 / QTE 板）**整条链不在任何显隐组** → 就绪态照常显示 |
| 名字匹配不可靠 | 父节点 `Health(Slider)` 与它的数值文本子节点**同名 "Health"** → 按名字分组会误伤 |
| 面板自管显隐 | `Victory` / `Defeat` / `HittedOverlay` / `DialogueLayer` 不在任何组，靠自身 active |
| 屏幕外孤儿 | `DialogueOverlay/BossPortrait` + `BossPortraitFrame` 停在**屏幕下方**（底部锚点 + 负 Y 偏移），且**无脚本引用**；`FlipPanel/BackFace` 里另有一套锚点规范的立绘 |
| 层级平铺难选中 | 30+ 个节点直接挂在画布下，没有功能分组 |

### 重构后的结构（`Tools/落地页/重构 UI 分层` 可重复执行）

```
BattleHUD(Canvas) order 21
└── Combat/
    ├── Hero/      HeroHUDParent（血条·经验条·技能键·QTE板·战斗头像）
    ├── Feedback/  ComboDisplay·KillCount·KillMilestoneDisplay·HittedOverlay·BuffDisplayPanel
    ├── Enemy/     BossBars·WaveText
    ├── Input/     ChargeIndicator
    └── Overlay/   PauseLayer·DialogueLayer·Victory·Defeat（面板型，自己管显隐）
HubCanvas(Canvas) order 25
└── Hub/
    ├── Top/       CoinCounter
    ├── Side/      HubStageEntry
    ├── Avatar/    落地页头像（金框 + 角色三图 + 等级牌 + 经验条）
    └── Bottom/    StartStageButton·HubTaskBar·HubMenuPlaceholder·HubTalentEntry·HubTabBar
```

显隐名单从 **14 个散名字**改成 **10 个组根/关键节点**：

| 名单 | 内容 | 语义 |
| --- | --- | --- |
| `inRunOnly` | `Combat/Hero` `Combat/Feedback` `Combat/Enemy` `Combat/Input` | 战斗专属 → 就绪态隐藏 |
| `readyOnly` | `Hub/Top` `Hub/Side` `Hub/Avatar` `Hub/Bottom` | 落地页专属 → 就绪态显示 |
| `hubOnly` | `Hub/Top`（金币）+ 战斗头像 | overlay 打开时隐藏 |

### 关键决定：落地页单独做一份头像

战斗头像 `UltPortraitButton` 在 **HeroHUD prefab 实例**里，父级 `Health(Slider)` 是它的祖先链 →
**不能重挂父级**（Unity 会拒绝）。因此落地页另做一份 `Hub/Avatar`（复用战斗头像的三张贴图
`zhangfei_filler_base` / `zhangfei_filler_main` / `zhangfei_head` + 新金框 + 等级牌 + 经验条），
**位置与战斗头像逐像素对齐**（实测两者画布坐标都是 x[-499,-199] y[-630,-330]），
锚在底部中心 → 画布宽度变化时与战斗头像同步漂移。
这样 `Combat/Hero` 就能整组隐藏，**血量条 / 100-100 / 技能板不再漏到落地页**。

### 翻车与修法（2026-10-24，用户报「无法进入游戏」）

| 现象 | 根因 | 修法 |
| --- | --- | --- |
| **进不去游戏**：`BattleDialogueView.Clear()` Awake 里 NullReferenceException | 我把 `DialogueOverlay` 下的 `BossPortrait` / `BossPortraitFrame` 当"无人引用的废件"删了 —— 但 `BattleDialogueView` 用**序列化字段** `_bossPortrait` / `_bossPortraitFrame` 引用它们 → 引用变 null → Awake 必崩。**我之前的 grep 搜的是 `BossPortrait`（大写开头），漏了 `_bossPortrait`（小写开头）** | 恢复两个节点并用 `SerializedObject` 重新接线；立绘位置本来就由代码运行时摆（`ApplyDialogueLayout` → `SetRectCenter`），只需把编辑器里的摆放摆正（右下角） |
| 落地页整组消失（`Top/Side/Bottom` 变 inactive 并被存进场景） | `HubHudModeSwitch` 在**编辑模式**也会执行显隐：Edit Mode 下 `HubStateSource.Instance` 为 null → 判定「非就绪态」→ 把 `readyOnly` 组关掉，并随场景保存 | `ApplyFromState()` 开头加 `if (!Application.isPlaying) return;` —— 编辑模式不应用显隐 |
| 改了显隐名单后显隐结果随机 | 换名单后 `_originalActive` 仍是**旧名单的下标** → 与新名单错位 | 新增 `HubHudModeSwitch.Recapture()`，替换名单后调用 |
| 每次重建都多一份节点（×3） | ① 清理只删了 `BattleHUD` 下的同名节点，重构后它们在 `HubCanvas` 下；② `Transform.Find` 只找**直接子节点**，分组后在子文件夹里找不到；③ 只删一份，积压的删不掉 | 清理改成**两个画布下递归查找 + 删全部同名** |
| `BattleHUD(Canvas)` 整个被关掉 | 编辑模式误应用显隐 + 名单错位 | 工具末尾显式 `combat.gameObject.SetActive(true)`；分组容器一律保持激活，**显示与否由运行时状态机决定，不写进场景** |

> 教训：**删/移场景节点前必须查序列化引用**（`unity_code_usages` 或按字段名全量扫，含下划线开头的私有字段）；
> **工具改场景时不要把运行时状态写进文件**（显隐由 `HubHudModeSwitch` 在 Play Mode 决定）。

### 第三轮：拉伸 / 白块 / Edit 模式（用户验收不通过后的返工）

| 现象 | 根因 | 修法 |
| --- | --- | --- |
| **很多 UI 严重拉伸** | ① 素材**带大块透明留白**（笔刷条 222 高里内容只有 113、经验条 157 高里内容 86）→ 塞进小框再 Sliced = 纵向压扁 + 横向拉长；② 元素尺寸没按素材比例（关卡入口 340×300 vs 素材 958×805；天赋图标 200×176 vs 249×250） | **先裁素材**（`hub_taskbar_brush` 503×113、`hub_exp_bar` 470×86），再**按素材自然比例**摆元素；九宫格只用在真能横向拉伸的笔刷条（border 40,16,40,16），窄于素材的标签带改 `Simple + preserveAspect` |
| **白块** | Unity 的 `Image` **没有 sprite 时会画一个白框**（任务条图标位还没接任务系统） | 该 Image 先 `enabled=false`（LayoutElement 保留占位），等任务系统给图标 |
| **落地页还显示战斗 UI** | 我上一轮把「编辑模式不应用显隐」当成结论 → 编辑器里战斗 UI 一直显示，压着落地页没法调 | 改成**编辑模式显示成「就绪态外观」**：战斗组隐藏、落地页显示、overlay 让位（`ApplyEditModeAppearance()`） |
| **落地页出现暂停键** | `PauseLayer` 被挪进 `Combat/Overlay` 组后不再受「局内专属」管 | `PauseLayer` 单独进 `inRunOnly` |
| **铜钱文字撑爆屏幕 / 是旧占位** | `TotalText` 的 rect 被压到 **18.5px 宽**，80 号字直接溢出；且场景里写死「铜钱: 0」 | 旧的 `CoinCounterUI`（局内"本局铜钱"）**移回战斗画布**；落地页**新做** `HubCoinDisplay`（图标 + 玩家**总铜钱**），落在 `Hub/Top` |
| 头像的经验条脱开头像 | 挂在头像根节点但锚在根节点外侧，且头像靠屏幕左缘 | 经验条改挂在**金框**下面，宽度与金框一致（200） |
| 任务条压住头像 | 画布 y 方向算反（y 越大越靠下） | 任务条落在 offset 150~263：头像下方、四宫格上方、「开始关卡」按钮下方 |

> 顺带确认：底部 tab 栏**已下线**（D1：主界面就是战斗界面，不需要「战斗」tab），
> 选关入口 = 右侧景色卡，天赋入口 = 右下大卡，都不在 tab 栏里。

### 第四轮：按参考图定稿排布（用户再次验收不通过后的返工）

**这次不再即兴摆位**：把参考图（露米娅原图 1116×2480）当唯一真源，先量出每个 UI 的归一化位置，再换算到 1080×1920。

| 元素 | 参考图归一化 | 我们（1080×1920） | 做法 |
| --- | --- | --- | --- |
| 头像+等级+经验条 | x 1-16%、y 1-9% | 顶左 170×210 | **一张合成件**（金框+武将+等级飘带+经验条一体）→ 不再拼装，角色下方不再出现无关框体与"血条" |
| 金币 | x 51-99%、y 1-5% | 顶右 220×80 | 新组件 `HubCoinDisplay`（玩家**总铜钱**）；旧的 `CoinCounterUI`（局内本局铜钱）移回战斗画布 |
| 关卡入口 | x 70-98%、y 24-41% | 右上 300×340 | 景色剪影（底图裁掉自带笔刷带）+ 笔刷条当名字带（TMP 写关卡名）+ 3 颗星 |
| 开始关卡 | x 30-72%、y 56-63% | 底中偏上 | 现有 `StartStageButton` 上移 |
| 任务条 | x 2-50%、y 73-78% | 左下 460×113 | 笔刷条（Sliced 横向拉伸） |
| 四宫格键 | x 2-10%、y 82-88% | 左下角 96×96 | 置灰占位 |
| 天赋入口 | x 58-99%、y 92-99% | 右下 260×408 | **卡底板**（深墨+金边）+ 发光树 + 底部亮带写「天赋」 |
| 底部 tab 栏 | y 89-99% | — | 已下线（D1：主界面即战斗界面） |

另修：
- **dev「跳过战斗」按钮**（运行时自建，不在场景里 → 场景里找不到）：位置改**顶部中间**（原来右上角和金币重叠），并且**落地页直接隐藏**（它是"跳过本节点战斗"的局内工具）。
- **天赋卡底板曾不显示**：旧逻辑把同一个 `Plate` 节点又写回笔刷条素材 → 整体重写 `FixTalentEntry`。

**教训**：UI 排版必须先有**可量的依据**（参考图坐标），不能再靠"看着差不多"摆；同一个节点被两段逻辑写同一个属性，是这类"看起来没生效"的常见原因。

### 第五轮：去蛇足（用户三条质疑）

| 质疑 | 事实 | 处理 |
| --- | --- | --- |
| 「左上角是什么东西？需要这个吗？」 | 是我**自作主张照搬参考图**加的落地页头像（金框+武将+等级12+经验条） | **删掉**：组、节点、素材文件全清。战斗里的头像/血条/经验条**一个字没动** |
| 「天赋后面的白色块是什么？」 | 唯一来源是**任务条图标位那个「无 sprite 的 Image」** —— Unity 的空 Image 会画成白块；已 `enabled=false`（等任务系统给图标） | 本轮抓 Game view 逐块扫过：落地页里**已无白块**（0 个启用中的空 Image） |
| 「有说关卡图需要星级评价吗？」 | **没有**这个需求，是我自己照参考图加的 | **删掉** 3 颗星（节点 + 素材文件） |

> 教训：参考图只用来定**位置关系**，不是功能清单 —— 参考图有的元素（头像/星级/商城/限时活动）不等于我们要有的。

### 落地页最终元素（6 个，都是需求里明确提过的）

金币（右上）· 关卡入口（右侧 24%-41%）· 开始关卡（中部偏下）· 任务条（左下 73%-78%）· 四宫格菜单键（左下角，功能待定）· 天赋卡（右下）。

### 其它修正

- 天赋入口补**墨色笔刷底板**（原来只有图标）+ 静态标题「天赋」。
- 删除对话层里停在屏幕外的 `BossPortrait` / `BossPortraitFrame`（无脚本引用；`FlipPanel/BackFace` 里有一套锚点规范的）。

---

## 四之二、画布拆分：落地页 UI 与战斗 UI 分开（2026-10-24）

用户反馈：「当前战斗部分的 UI 叠加在常态菜单的 UI 上，很难调试」。
→ Battle 场景现在有**两个根画布**，可以单独开关：

| 画布 | order | 内容 |
| --- | --- | --- |
| `HubCanvas(Canvas)` | **25**（在战斗 HUD 之上） | 关卡入口、天赋入口、任务条、四宫格菜单键、金币、「开始关卡」按钮、（已下线的底部 tab 栏） |
| `BattleHUD(Canvas)` | 21 | 战斗 HUD：血条/经验条/技能键/QTE 板（HeroHUDParent）、头像、连击、击杀里程碑、Boss 血条、蓄力指示、暂停层、对话层、命中遮罩、Buff 面板、结算/战败面板 |

调试用法：
- **只看战斗 UI** → 在 Hierarchy 里取消勾选 `HubCanvas(Canvas)`（或菜单 `Tools/落地页/切换落地页 UI 显示`）。
- **只看落地页** → 取消勾选 `BattleHUD(Canvas)`。
- `Tools/落地页/拆分落地页 UI 画布` 可重复执行（幂等，把落地页节点搬进 HubCanvas）。
- 落地页节点由 `HubHudModeSwitch.readyOnly` 控制显隐（就绪态显示 / 关卡态隐藏 / overlay 打开时让位），
  与画布开关互不冲突（`activeSelf` 不受父画布影响）。

---

## 五之二、2026-10-24 五处返工（用户验收发现）

| # | 问题 | 根因 | 修法 |
| --- | --- | --- | --- |
| 1 | 按钮文字乱码 | TMP 用了 `LiberationSans SDF`（**无中文字形**） | 改用场景里另一套 `方正粗黑宋简体 SDF`；脚本里加"必须 `HasCharacters("关卡天赋")`"的兜底判定 |
| 2 | 敌人间距错 | **`Test_Node_Battle` 引的是教学关的 `Formation_Tutorial`**（列距 2.6 / 排距 3.0），而正式关其它战斗用 `DefaultFormation`（列距 1.5 / 排距 2.5）→ 同一关卡两套间距 | `Test_Node_Battle.formationConfig` 改指 `DefaultFormation`；实测列距 2.60 → 1.38 |
| 3 | BossPortrait / BossPortraitFrame 跑到屏幕外 | 是**对话系统的 Boss 立绘**（旁边还有 PlayerPortrait/Frame），被放在**底部锚点 + 负 Y 偏移** = 屏幕下方；而且**没有任何脚本引用**（对话实际用气泡 `BossBubble`/`PlayerBubble`）→ 遗留废件 | 待你拍板：删掉这 4 个节点，或指定位置后修正偏移 |
| 4 | 屏幕尺寸变化导致相对位置错乱 | `BattleHUD` 画布**宽度是变的**（本机 Game 视图 960×1920，9:16 设备 1080×1920，CanvasScaler match=高度）→ 写死绝对坐标/宽度就会错位 | 全部改角/边锚定：关卡入口 (1,0.5)、天赋卡 (1,0)、四宫格 (0,0)、金币 (1,1)；**任务条改左右拉伸**（左端贴左 + 右端给右侧栏留 330） |
| 5 | 新 UI 全是白块 | 素材 `textureType=Default`（不是 Sprite）→ `LoadAssetAtPath<Sprite>` 返回 null → **Image 没图就是白块**；且纹理被按 POT 重采样（507×222 → 512×256） | 重新导入：`textureType=Sprite` + **`npotScale=None` 保持原生像素** + PPU100 + Point + alphaIsTransparency + 无 mipmap + Uncompressed；任务条另加九宫格边框 (60,30,60,30) 走 Sliced |

---

## 六、仍待定

| # | 问题 | 状态 |
| --- | --- | --- |
| R1 | 关卡入口的**具体造型**（用户明确「造型待定」） | 已出 v1 无字底，需按反馈再出 |
| R2 | 素材表 8 件 + 星星的**目视验收**与导入 | 待验收 |
| R3 | 关卡入口是否**每关一张图**（现在只有一张通用长坂坡景观） | 待定 |
| R4 | 任务条真实内容与数据架构 | 见 `design/task-system-design.md` |
| R5 | 金币从左上挪到右上后，左上是否还需要别的常驻信息 | 待定 |

---

## 七、验收口径

1. 就绪态：4 个新元素就位，**互不重叠**、不压「开始关卡」按钮、不越安全区。
2. 点关卡入口 → 打开选关界面；点天赋卡 → 打开天赋界面；两个入口**不重复出现**。
3. 关卡态（点开始关卡后）：4 个新元素全部隐藏，局内 HUD 恢复。
4. 选关 / 天赋界面打开时：4 个新元素隐藏，overlay 自己的「返回」按钮可正常退出。
5. 画面中**不出现任何角色**（D6）。
6. 竖屏 + 刘海屏安全区下不越界（`SafeAreaAdapter`）。
7. Console error / warning = 0。
8. 截图验收：本作环境 `unity_capture_viewport` 截不到 Overlay UI（见主菜单重构方案 §7.14）→ 用「组件状态回读 + 人眼」验收。
