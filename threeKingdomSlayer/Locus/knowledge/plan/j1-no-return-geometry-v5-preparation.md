---
id: kd_4a31970f-b81f-4f73-aefa-ff80567021af
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
summary: J1 v5构图获用户认可，景物偏西幻需修订；后续v6已按授权生成一次并交图，三国景物画风待用户验收，未部署Unity。
---

# J1 v5 不回正道路参考图准备

## 状态

- v4 `j1_geometry_locked_reference_v4` 已被用户否决：左右道路在转弯后错误回到远处，违背 Unity 路线固定 −45°/+45° 尾段。
- v4 原图、请求、响应和状态仍保留，但不得用于 J1 几何、拆素材、部署或 v5 几何输入。
- v5整体构图已获用户认可，用户指出景物偏西幻/中世纪。
- 用户随后明确授权“同意，请休整画风景物描述，生产新版参考图”；v6已按该授权生成一次，细节交接见 `plan/j1-three-kingdoms-scenery-v6-reference.md`。
- v5、v6各自都是一次请求、HTTP 200、无重试；Unity未修改或部署。

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

## v5候选与检查结果

- 原图：`C:/Users/Administrator/Pictures/gptGen/j1_no_return_geometry_v5/j1_scene_reference_v5.png`；请求/响应/状态与准备包同目录，完整路径见本机交付目录。
- HTTP 200，单张1024×2048 RGB PNG，完整解码通过；SHA-256=`f6d90bb0b9f01a6b6d44b89c64fac27b284616c7431312bd431fd06dd0d2e5d0`。
- 代理先前记录过道路投影疑虑；用户随后明确认可整体构图，并指出景物和建筑越来越偏向中世纪西幻。当前验收为“构图认可、景物需修订”，v5可作为后续构图参考，但景物不能照抄，也不据此声称与Unity路径逐像素一致。

## 验收门槛

生成后不能用 HTTP 成功、文件存在或高保真参数替代目视验收。重点检查：

1. 两条路在当前机位各只转一次，圆弧后继续按固定世界方向朝左右画幅外侧离开。
2. 任何道路都不能回到中央远方、地平线或远山；中央上方只能是陆地/植被/背景。
3. 道路外边界、分叉开口、共同来路和道路净空与参考图 1 一致。
4. 路牌为小型实体双箭头，不能再次成为巨大主体；左侧不能出现村庄建筑。
5. 右侧有运输侧带而非第二城门，景物不侵入道路净空。
6. 缩到约 319×632 后仍能读出左右出口方向；任一回正或长距离远路都直接不通过。

## 最新用户反馈与景物修改方向

用户反馈：“似乎越来越偏离三国风而是西幻了，建筑越来越偏向中世纪。整体构图没问题但是景物不对”。后续保留v5整体构图，仅修正景物造型与材料。用户已授权一次v6景物修订参考图并已生成；未授权Unity修改或进一步收费请求。详见 `plan/j1-three-kingdoms-scenery-v6-reference.md`。

对照观察：v5的块石矮墙、密铺石路、木车和桶箱、尖角残旗、装饰性草丛共同形成欧美奇幻乡间的印象；E0参考中的券拱和块石垛口也有相似倾向。单一物件不足以证明历史归属，问题在整体造型组合。v5提示词对道路非常具体，对器物只使用ancient wooden cart、crates、broken stone wall、torn red flags等泛化描述，题材形制约束不足。这是可能的偏移来源，不声称已证明模型内部因果。

拟调整：

| 对象 | 后续美术方向（待生成词审批） |
|---|---|
| 左石路 | 保留灰绿石路面积，铺面改为埋入土中的磨损石板与泥土缝，减少整齐厚重块石街道感 |
| 左篱笆与路肩 | 不齐木桩/竹条、绳扎横杆、低田埂与土石肩，保持乡间路线语义 |
| 右残墙 | 改为露出水平夯层的低夯土残埂，少量碎石/灰砖用于局部修补，不增加垛口或新城门 |
| 运输车与军需 | 明确为汉末战争语境的朴素木辎重板车、长车辕、木辐轮；载麻布粮袋、竹筐、捆束木料与少量朴素箱物，降低桶箱的主导地位 |
| 红旗 | 木杆/竹杆上的素面朱红布幡、简单横杆与绑绳，避免西式燕尾旗和徽章构造 |
| 植被组织 | 稀疏槐柳型树冠、野草与灌木依土坡成组，避免碎石墙加花园灌木的装饰性组合 |

下一版拟用v5做构图参考、原始用户概念图做题材/画风参考；不继续让E0整张券拱墙图承担器物形制指导。J1尚未到村庄，保持禁止民居/屋顶群/塔楼/门楼；不通过添加寺庙、牌坊、灯笼或夸张飞檐来替代汉末器物设计。画面仍为明亮、厚重的高清像素化三国战场。

v6已生成并交付，等待用户验收。当前不拆素材、不部署Unity；任何进一步收费请求均需另行明确批准。
