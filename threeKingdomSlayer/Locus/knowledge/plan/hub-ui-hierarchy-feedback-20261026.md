---
id: kd_f94e12b1-cfb4-4f6a-a136-652670e33e2e
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
summary: 落地页 + 战斗 HUD 的层级分类重构与交互反馈补齐（2026-10-26）——按用户三条反馈实施：资源栏底框拆成两段可独立挪移的框、左下四个占位键改为运行时灰化（着色+压暗层）、按钮加常态呼吸与按下回弹动效；新增可重跑的 Editor 工具 HubUiRestructure 与通用动效组件 UiButtonMotion。
---

# 落地页 / 战斗 HUD 层级重构与按钮反馈（2026-10-26）

> 状态：**已实施并自检通过**。场景已保存（未提交）。用户尚未在真机上验收动效手感。

## 1. 用户反馈与对应改动

| 反馈 | 改动 |
| --- | --- |
| ① 新部署的 UI 层级杂乱、父子关系混乱 | 落地页 `Hub` 与战斗 HUD `Combat` 两棵树都按功能分文件夹；父子关系重建（详见 §3） |
| ① 包子与铜钱共用同一张底框，要拆开、各自可挪移 | `hub_v9_resource_bar.png` 在中缝裁成两段 → `hub_v9_stamina_frame.png` / `hub_v9_coin_frame.png`，各自挂到独立组（`HubStamina` / `HubCoin`） |
| ② 左下四个按钮暂无功能，要「灰掉」 | `HubMenuPlaceholder` 扩展：键体+符号一起着色 `lockedTint` + 压暗层 `Dim`，`interactable=false` |
| ③ 按钮缺动效 | 新增通用组件 `UiButtonMotion`：常态呼吸缩放+上下微浮，按下压扁下沉、松手回弹；接到开始关卡（**常态呼吸**）；选关 / 天赋入口 **不呼吸**（只保留按下回弹，用户 2026-10-26 拍板） |

## 2. 新增 / 修改的文件

| 文件 | 说明 |
| --- | --- |
| `Assets/Scripts/UI/UiButtonMotion.cs`（新） | 通用按钮动效。DOTween 实现，常态 tween 与按下 tween **互斥**（按下 Kill 常态，回弹结束后再恢复），全部 `SetLink(gameObject)` |
| `Assets/Scripts/UI/HubMenuPlaceholder.cs`（改） | 新增 `symbolImage` / `dimOverlay` / `lockedTint`(0.62,0.64,0.68) / `lockedDim`(0.28)；`Apply()` 可在编辑态直接调用 |
| `Assets/Scripts/Editor/HubUiRestructure.cs`（新） | 菜单 `Tools/落地页/重构 UI 分类`，**幂等可重复跑**：做全部层级重排 + 资源条拆分 + 任务条对齐 + 四键灰化 + 动效挂载 + 显隐名单更新 |
| `Assets/Scripts/Editor/HubSceneSetup.cs`（改） | 重建清单加上 `MenuKey_1..4`，避免以后重跑该工具留下重复键 |
| `Assets/Sprites/UI/Hub/hub_v9_stamina_frame.png` / `hub_v9_coin_frame.png`（新） | 由 `hub_v9_resource_bar.png` 在中缝 x=666 处裁切（裁口像素连续，已核验） |
| `Assets/Scenes/Battle.scene`（改） | 重排结果已保存 |

## 3. 重排后的层级

```
HubCanvas(Canvas) order 25
└ Hub
    ├ Top/     HubStamina(Frame+BunIcon+StaminaText)   ← 体力组，底框独立
    │          HubCoin(Frame+CoinIcon+Amount)          ← 铜钱组，底框独立
    ├ Side/    HubStageEntry   HubTalentEntry          ← 天赋入口从 Bottom 挪来
    ├ Bottom/  StartStageButton  HubTaskBar  MenuKeys/(MenuKey_1..4)
    ├ Nav/     HubTabBar（已下线，保留不删）
    └ Debug/   HubDebugSaveButton（开发期清档，改为落地页专属）

BattleHUD(Canvas) order 21
└ Combat
    ├ Hero/       HeroHUDParent（prefab 实例，整块不动）
    ├ Feedback/   Combo / Counters / Status / Buffs / Currency
    ├ Enemy/      Boss / Wave
    ├ Input/      Charge
    └ Overlay/    Pause / Dialogue / Result
```

## 4. 顺手修掉的既有缺陷

1. **任务条错位**：原来 `Bar` 与 `Content` 都被挪了 (-20,-80)，节点矩形与画面差 80px，靶标压在笔刷左缘、铜钱悬在右缘外 → 改为底图铺满节点、内容按 16/8 内缩。
2. **开始关卡重复文案**：节点里还留着旧 TMP「开始关卡」（54 号字），和艺术字重复 → 已 `SetActive(false)`（保留对象与 `BattleStartStageButton.tmpLabel` 引用，不删除）。
3. **四宫格键**：原来 `HubMenuPlaceholder` / `_2` / `_3` / `_4` 平铺在 Bottom 下 → 收进 `MenuKeys/`，改名 `MenuKey_1..4`。
4. **资源条两个图标挤在一张框里** → 拆分（见 §2）。
5. **开始关卡按钮会闪出旧版按钮**：`Button` 原来用 **SpriteSwap**，`pressedSprite = MM_Btn_Primary_Pressed`、`disabledSprite = MM_Btn_Disabled`（遗留的 MM_ 系通用按钮图）。点下去的那一瞬间底图就被换成旧版通用按钮 → 看着像「旧版开始关卡按钮」。
   修法：改成 `ColorTint` + 把 `spriteState` 四个槽归零；`normalColor` 用纯白（避免给 V9 美术染色），按下只留 0.88 轻压，真正的反馈交给 `UiButtonMotion` 的缩放。

## 5. 踩过的坑（复用价值高）

1. **Unity 的 RectTransform 点锚点 + 零 sizeDelta = 零尺寸矩形（看不见）**。
   写「撑满父节点」必须用 `anchorMin=(0,0) / anchorMax=(1,1)` + `offsetMin/Max`，
   **不能**用「两点锚点相同 + sizeDelta=0」——那是最容易写错的一种。本轮底框、任务条底图、压暗层全部踩过一次。
2. **`Button` 的 ColorTint 只染 targetGraphic**：符号不是 target graphic → 锁定态必须**单独**给符号上色，否则键体灰了符号还是亮色，看着像能点。
3. **Button 的 disabledColor 会覆盖 targetGraphic 的 Image.color**：禁用态想控制颜色，走 `colors.disabledColor`，别只改 `Image.color`。
4. **无贴图的 Image 会画成实心块** → 正好拿来当压暗层（`sprite=null` + 黑色半透明）。
5. **重排脚本要用「从画布根开始找」**：只在目标组的父级里找，会漏掉画布根上的对象（清除存档按钮就是这么漏掉的）。
6. **prefab 实例内部不能拆**：`HeroHUD_Zhangfei` / `BuffIcon` 的子节点动了就产生脏 override → 只搬整块。
7. **编辑态预览隐藏是临时的**：`HubHudModeSwitch` 的预览靠 `DontSaveInEditor` 的 CanvasGroup，**域重载（重新编译）后会丢**，编辑模式又会露出战斗 HUD。补救：在编辑模式调一次 `sw.Recapture()`。

## 6. 验证

- 编译 0 错；Console 0 错 0 警。
- Game 视图截图（`Library/Locus/Screenshots/`）：
  - `locus_game_20261025_130917_134.png` 编辑态落地页（拆分后的资源条 + 任务条对齐）
  - `locus_game_20261025_131259_712.png` 运行态落地页（两段底框独立、铜钱 860、关卡名「长坂坡·卷轴」）
  - `locus_game_20261025_131813_063.png` 运行态临时点亮战斗 HUD（校验重排后战斗界面布局未变）
- 灰化效果用像素测过：四个符号亮度降到原来的约 1/3（暗青 / 暗红 / 灰白），第一版压到 0.4 偏狠，已调成 `lockedTint 0.62 + lockedDim 0.28`。
- 动效运行态实测：常态 scale 1.00↔1.03 呼吸 + 上下 ±3px；按下最小 scale 0.933（目标 0.93）+ 下沉 4px；松手 OutBack 回弹越过 1.0 后归位。
- 选关 / 天赋去呼吸后的复测：80 帧内 scale 恒为 1.000（开始关卡同窗口内 1.000~1.009 仍在呼吸）；选关入口按下仍到 0.943 → **只去掉常态呼吸，按下反馈保留**。
- 旧版按钮闪现修复后的复测：按下中 / 不可用态 / 恢复可用，底图全程都是 `hub_v9_start_brush`，不再切到 `MM_Btn_*`。

## 7. 遗留 / 待用户拍板

1. **动效手感需要用户真机验收**（呼吸幅度 3%、下沉 4px、回弹 0.2s 都在 `UiButtonMotion` 的 Inspector 字段里可调）。
2. **`Assets/Sprites/UI/Hub/hub_v9_resource_bar.png` 现在没有任何引用**（已切成两半）——保留作为重切素材源，也可删除。
3. **清除存档按钮仍在落地页左下角**（已归入 `Debug` 组，且改为落地页专属、战斗中不显示）；正式发版前建议删掉。
4. **体力没有数据源**，包子旁仍是占位 `—/—`（沿用上一轮结论）。
5. **V7 的「活动」旗（左上商城/首充礼包、右侧活动）仍未做**，落地页大片留白是美术缺口，不是层级问题。
6. `Tools/落地页/搭建落地页 UI`（旧工具）**不要跑**：它会重建节点、覆盖手工布局；新的重排请用 `Tools/落地页/重构 UI 分类`。
7. 本轮**未提交**；工作区另有他人并发的改动（Enemy 霸体削韧相关 + `StabSweepEffect.Hit.cs`），提交时不要混入。
