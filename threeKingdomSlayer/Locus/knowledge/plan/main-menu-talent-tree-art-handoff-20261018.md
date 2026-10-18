---
id: kd_8411e536-1f69-465a-8105-c33491cff791
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 主界面 + 天赋树 · 美术交接（2026-10-18）

> 这份文档是给美术的**可交付规格**，不是设计讨论。代码侧框架已经落地并可跑通（见 §7），
> 下面所有尺寸、字段名、导入设置都是照着现有代码写的，可以照单出图后直接拖进 Inspector。
>
> 画风基准：`design/art-style-guide.md`（§7 UI 与 HUD 为本次主依据）。本文不重新定义画风。

## 0. 一句话需求

把现在这套"程序默认按钮 + 纯色块"的主界面，换成新画风（墨色破边笔刷块 + 黄铜/金属边框 + 朱红点缀 + 金白高光），
并新增一个**天赋树**页面（树状节点 + 连线 + 三态）。竖屏，1080×1920 基准。

## 1. 现有实现（决定了素材怎么用）

| 项 | 现状 | 对美术的含义 |
|---|---|---|
| 画布 | Canvas `ScaleWithScreenSize`，参考 1080×1920，`matchWidthOrHeight = 0.5`，挂 `SafeAreaAdapter` | 按 1080×1920 出图；关键内容避开刘海/手势区 |
| 字体 | `方正粗黑宋简体 SDF`（TMP） | 标题可另出美术字，正文沿用该字体 |
| 主界面按钮 | 160×50，锚点居中，按钮 = `Image + Button + TMP 子文本` | **尺寸偏小，建议重排到 ≥ 88 高**；按钮底必须是九宫格 |
| 天赋页面 | `TalentTreePanel` **运行时按配置生成 UI**，不读 Resources | 图必须是可直接拖进 SerializedField 的 Sprite，不能靠命名约定 |

现有主界面元素与锚点（相对 Canvas 中心）：

| 元素 | anchoredPosition | size Delta |
|---|---|---|
| TalentButton（新增，天赋入口） | (0, 476) | 160×50 |
| NewGameButton | (0, 360.5) | 160×50 |
| ContinueButton | (0, 244.2) | 160×50 |
| QuitButton | (0, 126.3) | 160×50 |
| DeleteSaveButton | (-341.7, 55.1) | 160×50 |
| CoinDisplay | (-30, -30) | 250×50 |

> 这套位置是开发期随手摆的，**允许整体重排**。重排后把新的锚点回填到本表即可，代码不依赖坐标。

## 2. 主界面版式要求

竖向三段，从上到下：

1. **标题区**（约占顶部 22%）：游戏标 / 长坂坡字样。可压在战场背景上，但必须一眼读到，不与氛围杂物抢视线。
2. **主操作区**（中部 40%）：`继续 / 新游戏 / 天赋 / 退出`。主次分明——「继续」和「新游戏」是主按钮，「天赋」「退出」是次级。
   - 主按钮：墨色底 + 黄铜描边，高度 ≥ 88，宽度统一。
   - 次级按钮：可弱化为纯描边或更扁的牌条。
   - `删除存档` 是危险操作，建议做成小字/齿轮二级菜单里的东西，不要和主按钮排在一列。
3. **底部资源区**：铜钱条（图标 + 数字），固定落位、不随手势漂移。

背景：竖构图战场氛围（残旗、烟尘、断木），**必须给 UI 让出中上部**，不要在中部放高对比细节。

## 3. 天赋树页面版式要求

页面结构（全屏，覆盖在主界面之上）：

```
┌──────────────────────────────────────┐
│ [返回]            天  赋       ● 1200 │  ← 顶栏：关闭 / 标题 / 铜钱
│                                        │
│        ○ 指向突刺                      │
│         ╲                              │
│        ○ 枪突收尾        ○ 破阵         │  ← 树：节点 + 连线（当前单向链，允许 zigzag）
│         ╲                              │
│        ○ 追击            ○ 运筹         │
│                                        │
│              ○ 不屈                    │
├──────────────────────────────────────┤
│ 说明栏：节点名 + 效果说明 + 花费        │  ← 底部固定说明
└──────────────────────────────────────┘
```

代码里的实际排版参数（`TalentTreePanel` 序列化字段，可改）：

| 参数 | 当前值 | 说明 |
|---|---|---|
| `nodeSize` | 160×160 | 节点外框边长（设计像素） |
| `connectorThickness` | 10 | 连线粗细 |
| 节点位置 | 取 `TalentNodeConfig.treePosition` | 原点=面板中心，+Y 向上 |
| 说明栏 | 左右各留 80，底部 40 起，高 200 | |
| 关闭按钮 | 左上，140×88，距边 (56, 40) | |
| 铜钱行 | 右上，距边 (72, 56)，图标 56×56 | |

当前 6 个节点的坐标（可让美术照这个先出图，不满意再改，改坐标不用改代码）：

| 节点 | treePosition |
|---|---|
| 指向突刺（解锁 C2） | (0, 420) |
| 枪突收尾（解锁 C4） | (-150, 240) |
| 破阵 | (150, 60) |
| 追击 | (-150, -120) |
| 运筹 | (150, -300) |
| 不屈 | (0, -480) |

**节点三态必须一眼可分辨**（不看文字）：

| 状态 | 语义 | 当前占位色 | 建议画面语言 |
|---|---|---|---|
| 未解锁且前置未开 | 灰 | `(0.32, 0.30, 0.28)` | 暗铁色石/铜牌，无高光，轮廓锁死 |
| 可解锁（前置已开、铜钱够不够另说） | 金 | `(0.86, 0.70, 0.36)` | 黄铜亮边 + 微弱呼吸高光，让人想点 |
| 已解锁 | 亮 | `(0.93, 0.90, 0.82)` | 金白高光 + 内部点亮，与连线同为亮色 |

连线两态：未点亮 = 暗墨（`0.30,0.28,0.26`）；点亮 = 黄铜（`0.82,0.65,0.32`）。

## 4. 素材交付清单

### 4.1 主界面

目录：`Assets/Sprites/UI/MainMenu/`

| 文件名 | 用途 | 画布 | 九宫格边距 | 备注 |
|---|---|---|---|---|
| `MM_Background.png` | 主界面背景 | 1620×2880（1.5× 1080×1920） | 无 | 中上部留干净区 |
| `MM_Logo.png` | 游戏标 / 标题 | 宽 ≤ 1400，透明 | 无 | 可带笔刷飞白 |
| `MM_Btn_Primary.png` | 主按钮底 | 512×160 | 左/右 64，上/下 48 | 平态 |
| `MM_Btn_Primary_Pressed.png` | 主按钮按下 | 同上 | 同上 | 可整体压暗 + 内凹 |
| `MM_Btn_Secondary.png` | 次级按钮底 | 512×160 | 同上 | |
| `MM_Btn_Secondary_Pressed.png` | 次级按钮按下 | 同上 | 同上 | |
| `MM_Btn_Disabled.png` | 禁用态 | 512×160 | 同上 | 用于"继续"无存档时 |
| `MM_CoinBar.png` | 铜钱条底 | 512×128 | 左/右 48，上/下 40 | 可选，也可纯文字 |
| `MM_CoinIcon.png` | 铜钱图标 | 128×128 | 无 | 全项目复用 |

### 4.2 天赋树

目录：`Assets/Sprites/UI/Talent/`

| 文件名 | 用途 | 画布 | 九宫格边距 | 备注 |
|---|---|---|---|---|
| `TT_Panel.png` | 面板底板 | 1620×2880 或九宫格 512×512 | 左/右 96，上/下 96 | 墨色纸/木底，四角破边 |
| `TT_Node_Locked.png` | 节点·未解锁 | 256×256 | 无（整图） | 透明底，中心留符号位 |
| `TT_Node_Available.png` | 节点·可解锁 | 256×256 | 无 | |
| `TT_Node_Unlocked.png` | 节点·已解锁 | 256×256 | 无 | |
| `TT_Link.png` | 连线（可拉伸） | 128×32 | 左/右 48 | 中间留可平铺段，横向拉伸后不变形 |
| `TT_DescBar.png` | 底部说明栏底 | 512×256 | 左/右 48，上/下 48 | 可选 |
| `TT_Icon_UnlockC2.png` | 指向突刺符号 | 256×256 | 无 | 单焦点符号，不要插画 |
| `TT_Icon_UnlockC4.png` | 枪突收尾符号 | 256×256 | 无 | 与上一条必须一眼可分 |
| `TT_Icon_BreakFormation.png` | 破阵 | 256×256 | 无 | 占位内容，可能改 |
| `TT_Icon_Pursuit.png` | 追击 | 256×256 | 无 | 占位内容，可能改 |
| `TT_Icon_Strategy.png` | 运筹 | 256×256 | 无 | 占位内容，可能改 |
| `TT_Icon_Undying.png` | 不屈 | 256×256 | 无 | 占位内容，可能改 |

> 节点图标是"符号"不是插画（`art-style-guide.md` §7.3）：单一视觉焦点、轮廓优先、透明背景、无字无边框。

## 5. 导入设置（统一）

- Texture Type: `Sprite (2D and UI)`，Sprite Mode `Single`
- Pixels Per Unit: `100`
- Mesh Type: `Full Rect`（九宫格必须 Full Rect，否则拉伸丢边）
- Filter: `Bilinear`；Mip Maps: `Off`
- Compression: `None`（或 High Quality），Max Size `2048`
- sRGB: `On`，Alpha Is Transparency: `On`
- 九宫格：在 Sprite Editor 里按 §4 填 Border，并把 Image 的 `Type` 设为 `Sliced`

## 6. 出图后怎么接（不用改代码）

1. 图放到 §4 对应目录，按 §5 导入。
2. 主界面按钮：选中按钮的 `Image`，把 `Source Image` 换成对应图，`Type` 设 `Sliced`，按钮的 `Transition → Sprite Swap` 里填 Pressed / Disabled。
3. 天赋页面：选中 `Canvas/TalentTreePanel`，把图拖进这些字段——
   `dimBackground` / `panelBackground` / `nodeLocked` / `nodeAvailable` / `nodeUnlocked` / `connector` / `coinIcon`；
   节点自带图标拖进 `TalentTree_Main.asset` 里每个 `TalentNodeConfig.icon`。
4. 尺寸不合适就改 `TalentTreePanel.nodeSize` / `connectorThickness` / 各节点 `treePosition`，都是 Inspector 字段。

字段留空时程序会用程序化圆形 + 纯色占位，所以**先出一部分图也能接，不用等全套**。

## 7. 代码侧现状（供对齐，不需美术处理）

| 文件 | 作用 |
|---|---|
| `Assets/Scripts/Core/TalentTreeConfig.cs` | `TalentTreeConfig` + `TalentNodeConfig`（id/名称/说明/花费/解锁内容/坐标/图标） |
| `Assets/Scripts/Core/TalentTreeState.cs` | 解锁判定与扣铜钱：`IsUnlocked` / `IsAvailable` / `TryUnlock` / `IsFlagUnlocked` / `IsMoveUnlocked` |
| `Assets/Scripts/UI/TalentTreePanel.cs` | 运行时生成面板、节点、连线、三态刷新 |
| `Assets/Scripts/Core/SaveManager.cs` | 新增 `SaveData.unlockedTalents`（旧档自动补空列表） |
| `Assets/ScriptableObjects/Talents/TalentTree_Main.asset` | 当前 6 节点线性链（占位配置） |

已连通：主界面 `天赋` 按钮 → 面板打开 → 6 个节点按配置生成、三态正确。
**未验证**：真正点节点扣铜钱的那一步（会在验证时临时改存档，验完还原）。

## 8. 本期明确不做

- 天赋分叉（当前单链一路点到尾，用户口径）
- 每一级的花费数值、解锁内容定稿（等讨论，现在是 100/150/200/250/300/400 的占位值）
- 战前 Loadout、阶段词缀（`design/out-of-game-growth-system.md` 的 Layer 2）
- 面板进出动画、节点光效/粒子
- 主界面背景的 3D/视差层

## 9. 验收标准

- [ ] 1080×1920 与 720×1280 两档下，文字不糊、按钮不变形（九宫格拉伸无缺口）
- [ ] 关键元素全部落在安全区内（刘海、底部手势条不遮按钮）
- [ ] 节点三态并排看能立刻分出锁定/可点/已解锁
- [ ] 连线横向拉伸后没有断口、没有重复纹理接缝
- [ ] 铜钱数字压在底图上看清（对比度够）
- [ ] 与 `art-style-guide.md` 的两张概念图并排看，属于同一个世界（墨块/黄铜/朱红/金白高光）
- [ ] 游戏实际运行画面（不是设计稿）里主体仍然清楚
