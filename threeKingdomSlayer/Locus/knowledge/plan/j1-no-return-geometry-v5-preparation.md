---
id: kd_4a31970f-b81f-4f73-aefa-ff80567021af
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
summary: J1 v5道路不回正参考图准备包：以真实投影道路带和俯视拓扑锁定一次圆弧后固定±45°侧向出画，v4已被用户否决，v5仅准备未提交。
---

# J1 v5 不回正道路参考图准备

## 状态

- v4 `j1_geometry_locked_reference_v4` 已被用户否决：左右道路在转弯后错误回到远处，违背 Unity 路线固定 −45°/+45° 尾段。
- v4 原图、请求、响应和状态仍保留，但不得用于 J1 几何、拆素材、部署或 v5 几何输入。
- v5 只完成本地数学投影底稿、审阅图、中英文提示词、参考职责、请求规格和状态记录。
- v5 远程/付费请求数：0；`submission_attempts=0`；`approved=false`；未修改或部署 Unity。

准备包：`C:/Users/Administrator/Pictures/gptGen/j1_no_return_geometry_v5/`

## 真实几何证据

来源为 `Assets/Experiments/CurvedScroll/Scripts/YScrollSample.cs` 当前 `Evaluate()` 和当前 Battle 相机矩阵。机位底稿采用解析投影与道路半宽生成，未执行实际地面网格栅格化，因此不是新Unity截图或像素级帧复刻：

- J1 分叉距离 190 米。
- 圆弧半径 28.1595573，左右转角 −45°/+45°。
- 圆弧结束约 212.11647 米。
- 道路中心线约在 212.9 米离开画面左右边界；完整道路带约在 217.3 米离开，采样间隔 0.1 米。
- 尾段始终保持左世界方向 −45°、右世界方向 +45°，不会回到 0°。
- 当前道路半宽 2.45；路线显示高度 −1.8。
- Battle Camera position=(0,3,-10)、pitch=18°、FOV=60；准备输出 1024×2048、1:2。
- 俯视图只验证世界拓扑，机位图才是最终视图几何约束。

验证文件：`C:/Users/Administrator/Pictures/gptGen/j1_no_return_geometry_v5/review/geometry-verification.json`、`review/exact-projection-and-exits.json`。左右对称、尾段角度和出画检测通过；与 v4 已导出中心线最大单轴差异约 0.523px。这个数值只验证投影计算一致，不代表生成模型能逐像素锁定。

## v5参考图职责

1. `references/01_j1_projected_road_footprint.png`：最高优先级当前相机道路带和硬外轮廓；纯色可被自然材质替换，道路与陆地外边界、宽度、分叉和侧向出画不变。
2. `references/02_j1_world_route_topology.png`：世界拓扑图；一次圆弧后固定 −45°/+45°直向左右外侧，不是最终相机。
3. `references/03_j1_small_sign_and_massing.png`：原透明路牌Sprite按准备尺度合成，辅以中性体块；只参考小路牌及路外景物占位。
4. `references/04_e0_light_and_materials.png`：E0已认可的蓝天、远山、暖土、木石材质和日照。
5. `references/05_original_art_style.png`：高清像素化三国战场画风和材质体量。

参考图 1 中共同来路/左石路入口的纯色接缝是区域标签，生成时可以自然做土石材质过渡；不可改变道路外轮廓、道路宽度、中央陆地和左右出口。含文字/色线的 `review/` 图不上传；`j1_road_footprint_mask.png` 仅为本地比较，不声称为 API 像素锁定蒙版。

## 提示词与拟请求

完整中文正向词/负面词：[中文提示词](C:/Users/Administrator/Pictures/gptGen/j1_no_return_geometry_v5/j1_no_return_geometry_v5.prompt.zh.txt)。

完整英文正向词/负面词：[English prompt](C:/Users/Administrator/Pictures/gptGen/j1_no_return_geometry_v5/j1_no_return_geometry_v5.prompt.en.txt)。

无密钥请求草稿：[request.prepared.json](C:/Users/Administrator/Pictures/gptGen/j1_no_return_geometry_v5/j1_scene_reference_v5.request.prepared.json)。

计划参数：POST `https://api.muskapis.com/v1/images/edits`，multipart `image[]` 五张，`gpt-image-2.5-sunburst`，`quality=max`，`input_fidelity=high`，`1024x2048`，PNG，`n=1`，不透明，不设置 transparent；只允许一次候选，不自动重试，不覆盖 v4。必须在提示词、输入职责或参数发生实质变化后重新获得用户明确批准。

## 审阅图

- `review/j1_camera_road_edges_review.png`：机位道路边界与左右出画说明。
- `review/j1_world_heading_review.png`：一次圆弧后保持固定世界角度。
- `review/j1_input_geometry_before_after.png`：旧颜色底稿与新硬轮廓底稿对照。
- `review/j1_sign_scale_comparison.png`：路牌尺度板，不上传。
- `review/j1_camera_road_edges_320x640.png`：用户小画幅检查。

## 验收门槛

生成后不能用 HTTP 成功、文件存在或高保真参数替代目视验收。重点检查：

1. 两条路在当前机位各只转一次，圆弧后继续按固定世界方向朝左右画幅外侧离开。
2. 任何道路都不能回到中央远方、地平线或远山；中央上方只能是陆地/植被/背景。
3. 道路外边界、分叉开口、共同来路和道路净空与参考图 1 一致。
4. 路牌为小型实体双箭头，不能再次成为巨大主体；左侧不能出现村庄建筑。
5. 右侧有运输侧带而非第二城门，景物不侵入道路净空。
6. 缩到约 319×632 后仍能读出左右出口方向；任一回正或长距离远路都直接不通过。

当前不进行成图、拆素材、Unity 部署或场景修改。
