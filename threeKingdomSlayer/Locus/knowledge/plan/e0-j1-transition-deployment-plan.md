---
id: kd_8369a11b-da3c-4bab-9f99-08885b65b27c
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# E0→J1 门后直路部署方案

> **状态：已部署完成（2026-10-17）**。6 件稀疏结构物已停用并从 `scenery` 移除且删除；11 件新对象已建并登记。`scenery` **695 → 700**（−6 +11 = +5，与方案一致），空项 0、重复 0；已保存并关闭重载复核，Console 0/0。
>
> **实测结论：内缘约束全部通过（最差 3.83 ≥ 3.6），中央道路无侵入；190 m 的 J1 画面贡献 0 像素、未被改变。可见窗口为 130–163 m，165 m 起本组全部落到镜头后方（几何必然，见下方视图分析）。**

## 范围

只替换 `E0 to J1 - Beyond Gate Shoulder` 里现有的 6 件稀疏结构物。保留该组、4 丛草、路线长度和 J1 已验收路肩。新精灵只使用这四张，不用 `Previews/` 或 `References/`：

- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/E0/Transition/e0_j1_w1_left_wall_v1.png`
- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/E0/Transition/e0_j1_w2_right_wall_v1.png`
- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/E0/Transition/e0_j1_r1_rubble_v1.png`
- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/E0/Transition/e0_j1_c1_cart_v1.png`

四张均已按可见底边设置 Pivot，局部 Y 必须为 0。材质用 `YSampleScenery_E0Route`，草保持 `YGrass_E0Route`。Scale 三个分量都为正数；需要转向只用 `flipX`。Sorting 为 Default/0。

原始可见尺寸约：高墙 9.6×8.9 米、矮墙 9.6×6.0 米、车 10.6×7.7 米、石堆 6.0×3.2 米。因此不能用 Scale=1，否则内侧会进入 `|x|<2.45` 的道路。

## 停用

停用并移出 scenery，不留 null：

`E0J1_Logs_Left`、`E0J1_Stake_Right`、`E0J1_Palisade_Left`、`E0J1_Cheval_Right`、`E0J1_Earth_Left`、`E0J1_Timber_Right`。

四丛 `E0J1_Grass_*` 保留。部署前先回读 scenery 数量；上次磁盘记录是 695，若不是，先停止并报告。

## 新实例

坐标是组局部坐标，也是世界坐标。摆完后检查每件可见内缘，必须满足 `|x|>=3.6`。

| 对象 | Sprite | Position | Scale |
|---|---|---|---|
| E0J1_WallTall_A | W1 | (-6.8, 0, 142) | 0.56 |
| E0J1_WallTall_B | W1 | (-7.2, 0, 161) | 0.46 |
| E0J1_WallLow_A | W2 | (7.0, 0, 151) | 0.50 |
| E0J1_WallLow_B | W2 | (7.4, 0, 167) | 0.40 |
| E0J1_Cart_Loaded | C1 | (6.2, 0, 146) | 0.38 |
| E0J1_Rubble_A | R1 | (-5.0, 0, 146) | 0.30 |
| E0J1_Rubble_B | R1 | (5.5, 0, 155) | 0.26 |
| E0J1_Rubble_C | R1 | (-4.8, 0, 165) | 0.22 |
| E0J1_Rubble_D | R1 | (6.5, 0, 170) | 0.20 |

再复用两件旧素材：

- `J1_A04_BrokenLowFence_v1`，命名 `E0J1_Fence_Left`，(-5.4, 0, 153)，Scale 0.42。
- `N1_SmallBrokenFlag_v1`，命名 `E0J1_Flag_Left`，(-6.0, 0, 149)，Scale 0.34。它必须用路线材质，不做 N1 路灯旗，不抬高 Y。

右侧不镜像高墙。装货车默认轮子朝左，放在右侧时轮子朝向道路；若实际朝外，只改 `flipX`。

净增 11 个 Renderer。移除 6 个后，scenery 应增加 5，空项 0、重复 0。

## 验收

用 Battle Main Camera，路线根临时 Y=-1.8。主要看路径距离 145、155、165，因为投影下压会让 90 米处看不到这段，这不代表摆放失败。190 米的 J1 画面不应被这组改变。中央道路保持净空。

只保存 Y 场景。不改道路、门垣、J1、N1 旗帜、Shader 或材质资产。

## 执行结果（2026-10-17）

### 停用并删除的 6 件

`E0J1_Logs_Left`、`E0J1_Stake_Right`、`E0J1_Palisade_Left`、`E0J1_Cheval_Right`、`E0J1_Earth_Left`、`E0J1_Timber_Right` —— 已先置 `SetActive(false)`、从 `scenery` 数组中剔除（不留 null），再 `DestroyImmediate` 删除。磁盘回读确认已不存在。

### 新建的 11 件

全部为 `SpriteRenderer` + `YSampleScenery_E0Route`，局部 Y = 0，等比正缩放，Sorting = Default/0，`flipX` 全为 false（含装货车：实测原图车轮在左侧，放在右侧 x=+6.2 时车轮朝道路，无需翻转）。

| 对象 | Sprite | Position | Scale | 世界尺寸 | 内缘 x |
|---|---|---|---|---|---|
| E0J1_WallTall_A | W1 | (-6.8, 0, 142) | 0.56 | 5.73×5.33 | 3.94 |
| E0J1_WallTall_B | W1 | (-7.2, 0, 161) | 0.46 | 4.71×4.37 | 4.85 |
| E0J1_WallLow_A | W2 | (7.0, 0, 151) | 0.50 | 5.11×3.34 | 4.45 |
| E0J1_WallLow_B | W2 | (7.4, 0, 167) | 0.40 | 4.08×2.67 | 5.36 |
| E0J1_Cart_Loaded | C1 | (6.2, 0, 146) | 0.38 | 4.27×3.18 | 4.06 |
| E0J1_Rubble_A | R1 | (-5.0, 0, 146) | 0.30 | 1.99×1.17 | 4.00 |
| E0J1_Rubble_B | R1 | (5.5, 0, 155) | 0.26 | 1.73×1.01 | 4.64 |
| E0J1_Rubble_C | R1 | (-4.8, 0, 165) | 0.22 | 1.46×0.86 | 4.07 |
| E0J1_Rubble_D | R1 | (6.5, 0, 170) | 0.20 | 1.33×0.78 | 5.84 |
| E0J1_Fence_Left | J1_A04_BrokenLowFence_v1 | (-5.4, 0, 153) | 0.42 | 3.23×1.83 | 3.83 |
| E0J1_Flag_Left | N1_SmallBrokenFlag_v1 | (-6.0, 0, 149) | 0.34 | 0.88×1.47 | 5.27 |

四丛原有 `E0J1_Grass_*` 保留（`YGrass_E0Route`）。组共 15 个子节点（4 草 +11 新），全部登记在 `scenery`。

### 验证

**内缘约束**（方案要求 `|x| >= 3.6`，从实际 renderer bounds 测）：11 件全部通过，最差为 `E0J1_Fence_Left` 的 **3.83**；全部 > 2.45，**中央道路无侵入**。

**装饰尺寸**：方案预估高墙 9.6×8.9 m，实际按 0.56 缩放渲染为 **5.73×5.33 m**（贴图 1023×951 px / 100 ppu × 0.56 = 5.73×5.33）。这是因为方案里的 9.6×8.9 是**未缩放的贴图尺寸**（10.23×9.51 m）取整后的近似，不是预期渲染尺寸。若预期是屏幕上的 9.6 m 高墙，缩放应约为 1.0，但那会违反内缘约束。按方案给定的 Scale 数值执行。

**可见窗口**（组 OFF vs ON 像素差，阈值 ≥8）：

| 距离 | 可见像素 |
|---|---|
| 130 m | 23185 |
| 145 m | 15176 |
| 155 m | 4632 |
| 158 m | 2056 |
| 160 m | 796 |
| 162 m | 367 |
| **165 m** | **0** |
| 175 / 190 m | 0 |

中心带上（屏幕中间 20%）在所有距离均为 **0 像素**——本组没有侵入道路。

**为何 165 m 归零（几何必然，非摆放错误）**：物体 depth = 自身 Z − 行进 Z。到 165 m 时：

- z=142/146/149/151/153 的 6 件 depth 为 **−23 到 −12**，已在摄像机后方；
- 剩余 5 件（z=161/165/166/167/170）depth 为 −4 到 +5，已接近或越过相机，其中多数被近裁面切掉。

故本组的有效显示窗口就是驶过它之前的那段（130–163 m）。**若要 165 m 仍看到这组，必须把这组 Z 后移或右移物体密度，超出本方案授权。**

**190 m J1**：贡献 **0 像素**，已验收的 J1 构图完全未被改变。

### 其他确认

- 磁盘回读（关闭重载后复核）：15 个子节点、位置/缩放/材质/`flipX` 均与内存一致；
- 守卫项未变：门垣 z=132、J1 最小 z=173、N1 旗组 6 个、路线 `Length=266.117`；
- Console `0/0`；Battle 与 MainMenu 未保存。
