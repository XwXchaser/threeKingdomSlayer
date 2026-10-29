---
id: kd_33f525f1-b34f-4c9d-ba85-9ab5977d9a57
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: 落地页 UI V9 拆件与场景拼接交接（2026-10-25，用户已签收）——8 件透明素材从 V7 整图拆出并接入 Battle 场景，按 V7 比例重排落地页，真实 Game 视图截图已逐处核验；含场景接线映射、6 个可复用坑（布局组覆盖手工 Rect、构建工具 disable 占位 Image、脚本 OnEnable 刷回精灵、RectTransform 轴心语义、就绪态可见性集合、截图工具现在能截 Overlay UI）与遗留项。
---

# 落地页 UI V9 拆件与场景拼接交接（2026-10-25）

> 状态：**用户已签收**。8 件拆件已生成并接入 `Assets/Scenes/Battle.scene`，按 V7 参考图比例重排落地页，已用真实 Game 视图截图逐处核验。场景已保存、**未提交**。
> 本文是落地页美术方向的**当前权威入口**。旧文档（`hub-ui-art-style-session-handoff-20261025.md`、`hub-ui-v4-delivery-20261025.md`、`hub-ui-deployment-handoff-20261024.md`、`hub-ui-remaining-assets-generation-handoff-20261024.md`）保留为演进记录，其结论以本文为准。

## 1. 结论一句话

V7 整图（1024×2048）是**用户认可的布局与画风基准**；本轮从 V7 局部拆出 8 件透明素材（右下空方框、任务栏底条、任务靶标、开始关卡红底、开始关卡艺术字、包子、金币、资源栏底框），再按 V7 比例把它们拼回 Battle 场景的落地页。左上角「商城 / 首充礼包 / 活动」那组仍未做。

## 2. 产物与路径

| 类别 | 路径 |
| --- | --- |
| 生成原图（8 件，勿改） | `_art_process/hub_ui_split_prompts_v1/generated/*.png` |
| 落地版（裁透明边、D3 重着色、四键符号提取） | `_art_process/hub_ui_split_prompts_v1/ready/*.png` |
| 双语提示词包（8 件逐件词 + 共同规则） | `_art_process/hub_ui_split_prompts_v1/PROMPTS.md` |
| 请求 / 响应 / 状态记录 | `_art_process/hub_ui_split_prompts_v1/generated/*.request.actual.json` / `*.response.json` / `*.status.json` |
| 拼接交付记录（本次） | `_art_process/hub_ui_split_prompts_v1/hub_assembly_delivery.md` |
| 工程内资产（14 个） | `Assets/Sprites/UI/Hub/hub_v9_*.png` |

工程内资产清单：`hub_v9_talent_frame`（方框，同时用于四宫格键体）、`hub_v9_task_strip`、`hub_v9_task_target`、`hub_v9_start_brush`、`hub_v9_start_title`、`hub_v9_bun`、`hub_v9_coin`、`hub_v9_resource_bar`、`hub_v9_talent_icon`（V9 生成的经脉武将）、`hub_v9_key_symbol_1..4`（从 V7 逐键提取）、`hub_v9_task_progress_fill`（程序生成 32×6 红条）。

导入参数照抄旧素材 `hub_task_plate_v3.png`：Sprite / Single / alphaIsTransparency=True / 无 mipmap / Point / maxSize 2048 / PPU 100 / 中心轴。

## 3. 场景接线映射（`Assets/Scenes/Battle.scene` → `HubCanvas(Canvas)`，order 25，设计空间 960×1920）

| 节点 | 变更 |
| --- | --- |
| `Hub/Bottom/StartStageButton` | 底图 → `hub_v9_start_brush`（620×259 @ 0,-814）；`TitleArt` → `hub_v9_start_title`（380×141） |
| `Hub/Bottom/HubTaskBar` | 底 → `hub_v9_task_strip`（560×99 @ 39,316）；`Icon` → `hub_v9_task_target`；`Reward` → `hub_v9_coin`；新增 `ProgressFill`；标题与计数同行，红条在标题下 |
| `Hub/Top/HubCoin` | 底 → `hub_v9_resource_bar`（369×67 @ 0,-28）；`CoinIcon` → `hub_v9_coin`；新增 `BunIcon` 与 `StaminaText`（`—/—` 占位） |
| `Hub/Bottom/HubTalentEntry` | 底 → `hub_v9_talent_frame`（200×204 @ -67,89）；`Icon` → `hub_v9_talent_icon`；`Label` 移到底部 |
| `Hub/Bottom/HubMenuPlaceholder` + `_2/_3/_4` | 键体 → `hub_v9_talent_frame`（108×110 @ 40/172/307/441, 100）；各配一枚 V7 提取符号 |
| `Hub/Side/HubStageEntry` | 仅按 V7 重定位（300×240 @ -60,400），美术未换 |

脚本字段同步更新（`HubTaskBar.barSprite/iconSprite/rewardSprite`、`HubCoinDisplay.iconSprite`、`HubTalentEntry.treeSprite`、`HubMenuPlaceholder.keySprite`×4）——**只改 Image 不改字段，运行时会刷回旧精灵**。

## 4. 这次踩到并修掉的坑（可复用）

1. **布局组会覆盖手工定位**：任务栏 `Content` 上有 `HorizontalLayoutGroup`、`TextBox` 上有 `VerticalLayoutGroup`（`HubSceneSetup` 搭 UI 时加的），每帧重算子节点 Rect，手工设的 anchoredPosition 会被冲掉。已移除这两个布局组与相关 `LayoutElement`，改为显式定位。V7 里标题与计数本来就在同一行，竖排布局组也对不上。
2. **构建工具把占位 Image 组件 disable 了**：`HubTaskBar/Content/Icon` 的 `Image.enabled=false`（当时还没接任务系统）。只换精灵不启用 → 靶标完全不显示。
3. **脚本 `OnEnable` 用自身字段覆盖精灵**：`HubTaskBar`/`HubCoinDisplay`/`HubTalentEntry`/`HubMenuPlaceholder` 都会在启用时把 Image 的 sprite 刷成自己的 public 字段。改素材必须同时改场景里的脚本字段。
4. **RectTransform 轴心语义**：`anchoredPosition` 相对的是「轴心点」不是「左边缘」。父节点轴心 0.5 时，子节点锚在父左边缘会偏出父框一半。标题被推出屏幕就是这个原因（`TextBox` 轴心改成左对齐后修正）。
5. **就绪态可见性集合**：`HubHudModeSwitch.inRunOnly = Hero / Feedback / Enemy / Input / PauseLayer`，就绪态整体隐藏。所以**局内头像和局内铜钱计数在落地页根本不出现**，新资源行不会与它们重复显示；任务条与四宫格也不存在被头像遮挡的问题。
6. **`unity_capture_viewport` 现在能截到 Overlay UI**（2026-10-25 实测：Game 视图 1080×2160，输出 640×1280，落地页 UI 完整入画）。**旧文档「截图工具截不到 Overlay UI、只能靠人眼验收」的结论已作废**，落地页改版可以直接用截图自检。
7. **V7 → 游戏设计空间映射**：V7 是 1024×2048；游戏画布参考 1080×1920 + match height，实际设计空间 960×1920。映射系数取 **0.9375**（纵向 2048→1920，横向对齐当前 960 设计宽度）。真机更窄比例下角落锚定元素尺寸不随宽度缩放，与原有布局同性质。

## 5. 本地确定性加工（未再调用生成接口）

- 全部素材裁掉透明边后导入。
- **D3 资源栏底框**：模型把参考里的蓝天一起吸收成饱和蓝笔刷。按亮度重映射为与任务栏同族的暖墨色，保留笔刷形状与中间分隔线（重映射后蓝差 B−R = 93.6，V7 原区域 93.8）。
- **四宫格符号**：从 V7 逐键提取——把暗蓝灰键面判为背景，闭运算 + 填洞 + 一像素暗描边，环 / 青玉双菱 / 朱红双方 / 银白层片四枚均无黑洞。
- 红色进度条为程序生成的 32×6 小像素条。

## 6. 验证方式与结论

- 场景文件前后对象集合比对：**无任何对象被删除**；新增 13 个对象（201→214 GameObject，257→266 MonoBehaviour），全部是本次新建。
- 真实 Game 视图截图逐处回读：红底+艺术字、任务条四元素、包子/金币行、天赋方框、四宫格均正确。截图 `Library/Locus/Screenshots/locus_game_20261025_113433_756.png`。
- 为截图临时隐藏了战斗 HUD 的 `Hero/Feedback/Enemy/Input/PauseLayer`（就绪态本来就会隐藏它们），截图后已恢复并保存。
- Console 0 错 0 警。

## 7. 仍需处理 / 待用户拍板

1. **商城 / 首充礼包 / 活动**（左上角那组）本批未做，素材未生成——这是落地页最后一块空白。
2. **体力没有数据源**，包子旁的数值是占位 `—/—`；接体力系统时要一并决定数据来源。
3. **关卡名字带**在编辑态是 `—`，运行时由 `HubStageEntry` 写入当前关卡名（长坂坡），尚未在游戏里验证过。
4. **旧的 `_v3` 落地页素材**（`hub_task_plate_v3` / `hub_talent_plate_v3` / `hub_start_plate_v3` / `hub_coin_bar_v3` / `hub_task_badge_v3` / `hub_menu_disabled_v3` / `hub_talent_nodes_v3`）现已无引用，可择机清理。
5. **提交**：`Assets/Sprites/UI/Hub/hub_v9_*.png`（14 个 + meta）与 `Assets/Scenes/Battle.scene` 尚未提交。`Assets/Sprites/UI/Hub/` 整个目录此前是 untracked，提交前先确认目录内所有被引用文件都要进版本库。
6. `Assets/Resources/EnemyPrefabs/Enemy_1011.prefab` 有一处 1 行的既有改动，**不是本轮工作产生**，未处理。
7. 本轮只改美术接线与布局，**未改任何脚本逻辑**。`Tools/落地页/搭建落地页 UI` 重新运行会重建节点并覆盖这次的手工布局。

## 8. 版本线（接手前必读）

| 版本 | 产物 | 用户判定 |
| --- | --- | --- |
| V1–V3 | 整图候选 | 偏复古 / 简洁但偏灰 / 灰壳过多 |
| V4 | 整图候选 | 灰壳已修；活动入口仍是写实战士 |
| V5–V6 | 整图候选 | 造型方向被否（方底、卡通脸等） |
| **V7** | 整图 1024×2048 | **布局与画风被认可**，是当前基准 |
| V8 | 双按钮图集 | 圆润金属牌、中文错成 ENE/KE，被否 |
| V9 | 双按钮图集 | 造型不成立，被否；**但其中的经脉武将图标被单独采纳**，现用于天赋方框 |

对应文件：V7 = `_art_process/hub_ui_independent_direction_v7/hub_ui_independent_direction_v7_generated.png`；V8 = `_art_process/hub_ui_independent_direction_v8/`；V9 = `_art_process/hub_ui_independent_direction_v9/`。
