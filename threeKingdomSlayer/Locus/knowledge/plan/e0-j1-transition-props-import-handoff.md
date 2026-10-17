---
id: kd_92788a41-d0be-45e4-8602-dfaa897d69c2
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# E0→J1 过渡段道具组图：切割、抠图与 Unity 导入交接

## 状态

用户已验收 G1 组图（`plan/e0-j1-transition-props-g1-handoff.md` 记录的那张），并要求切割、抠图、导入 Unity 成为场景素材。**已完成上述三步，未部署到任何场景**，等待部署指令。

生成侧的完整记录（prompt、请求、响应、原始 2048×2048 图）在 `C:/Users/Administrator/Pictures/gptGen/e0_j1_transition_props_g1_v1/`。

## 交付物

目录 `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/E0/Transition/`：

| 文件 | 画布 | 内容尺寸 | 说明 |
|---|---|---|---|
| `e0_j1_w1_left_wall_v1.png` | 1023×951 | 959×887 | 左侧较高残墙 |
| `e0_j1_w2_right_wall_v1.png` | 1021×667 | 957×603 | 右侧较矮残墙 |
| `e0_j1_r1_rubble_v1.png` | 664×390 | 600×326 | 独立散石堆 |
| `e0_j1_c1_cart_v1.png` | 1124×837 | 1060×773 | 装货侧视板车 |

另有 `Previews/`（每件白/深灰/土色三底 + `contact_dark.png` 四件合览）与 `References/`（原始图集 `source_atlas_g1_v1.png` 与清理后 `source_atlas_g1_v1_cleaned.png`）。

## 切割方式

未按 1024 硬切线切，改为按 Alpha 连通区域实测：

1. 先清理噪点：源图有 121,051 个 `alpha≤16` 的散点（其中约 4 万个远离主体、密度 2%），按 `alpha≤16 且距核心 >3px` 归零，再清掉 `alpha<8` 的残留；共移除 93,356 + 21,879 像素，另丢弃 92 个小于 1000px 的碎片。
2. 清理后正好得到 4 个实体连通域，与格位一一对应，互不连通。
3. 每件按自身 bbox 裁切，四周补 32px 纯透明安全边（与 G1 移交要求的 80px 内边距一致方向，但按实际轮廓而非固定切线）。

板车左缘 x=953 跨过 x=1024 竖线 71px，因此固定切线会切断车轮；按实际连通域裁切后保持完整。

## 抠图处理

源图是真正的透明 PNG，但按网络图像缩放后的 Alpha 分布有两个问题，已处理：

- **无完全不透明像素**：最大值 254，核心区集中在 250–254。处理为 `alpha≥220` 一律置 255，得到干净的实心主体；边缘保留原始软过渡用于抗锯齿。
- **去色边**：半透明边缘的 RGB 会被背景色污染。用核心区颜色向软边环做最近邻填充（`distance_transform_edt` 索引回填），消除白边/暗边光晕。

处理后每件的 255/中间/0 像素分布：

| 文件 | alpha=255 | 0<alpha<255 | alpha=0 |
|---|---|---|---|
| w1_left_wall | 508,520 | 8,161 | 456,192 |
| w2_right_wall | 355,755 | 7,206 | 318,046 |
| r1_rubble | 129,254 | 3,195 | 126,511 |
| c1_cart | 564,358 | 9,285 | 367,145 |

中间 Alpha 只占 1–1.6%，均是 1–2px 抗锯齿边。

## Unity 导入设置

四张按项目既有像素美术约定配置（与 `E0/Architecture/` 一致）：

- `textureType = Sprite`，`spriteImportMode = Single`，`spritePixelsPerUnit = 100`
- `alphaIsTransparency = true`，`mipmapEnabled = false`，`filterMode = Point`，`wrapMode = Clamp`
- `maxTextureSize = 2048`，`npotScale = None`，`textureCompression = Uncompressed`（DefaultTexturePlatform）
- `spriteMeshType = FullRect`，关闭 fallback physics shape
- **pivot = 内容脚点上方 32px**：四张分别为 y = 0.033649 / 0.047976 / 0.082051 / 0.038232（归一化），实测 `Sprite.pivot` 均为 `(w/2, 32.0)` 像素

同时修正了归档图的导入：`References/` 两张源集集与 `Previews/` 十三张预览原被导入为 Default + mipmap + ASTC（还会剥掉 Alpha），已统一改为 Default + 无 mipmap + 不压缩，源集集恢复 RGBA32。

GUID（供部署时引用）：

| 文件 | GUID |
|---|---|
| `e0_j1_w1_left_wall_v1.png` | `d0c21e59e2a8a674c9d9d9ec35dd7b87` |
| `e0_j1_w2_right_wall_v1.png` | `ae8f7dea8dfb98f44a7d72045e01f338` |
| `e0_j1_r1_rubble_v1.png` | `b2cd55be6414d214d936411173afebc7` |
| `e0_j1_c1_cart_v1.png` | `c382a96e8e0368646ac5ec4f6d6a1913` |

## 验证

- 四张均为 8-bit RGBA PNG，Unity 侧 `Texture2D` 格式 RGBA32。
- 通过 `Graphics.Blit` + `ReadPixels` 回读 Unity 实际像素，Alpha 分布与磁盘文件逐项一致（如 w1: 508520 / 8161 / 456192），无压缩损失。
- Console 零 error、零 warning。
- 每件内容完整入画，四周 32px 透明，未裁断。
- 三底预览目视确认无白边、无灰底、无棋盘格、无投影。

## 尺寸关系（部署时需注意）

三件的轮廓高度实测：W1 887px、C1 车 773px、W2 603px。G1 规格要求"车明显矮于两段墙"，实际车比 W1 矮但比 W2 高 170px。同框摆放时若要让车显得低于右墙，需要靠 Transform 缩放或分层错位处理，单纯等比例并排会读成车高于右墙。

## 边界

- 未修改 Battle、Y、E0 预览场景，未创建任何 GameObject，未改动 Shader、材质或现有 `E0 to J1 - Beyond Gate Shoulder` 对象。
- 工作区存在大量与本次无关的未提交改动（场景、Prefab、脚本等），本任务只新增了 `Transition/` 目录，未触碰它们。
- 未提交 Git。
