---
id: kd_0ef77297-5d40-4b44-94c4-84b69f799122
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# E0→J1 门后直路道具组图 G1 生成交接

## 授权

用户要求按既有成功组图经验准备交接，并由后续对话发起这一张透明素材组图。授权仅限本地裁参考、校验真实请求、提交一次和交图。不含自动重试、切片、导入 Unity 或改场景。

## 四件与格位

画布 2048×2048，真实透明 PNG。原点左上，右/下边界不包含。每格 1024×1024，格内再留 80px，件与件之间保持 160px 透明隔离：

| 格 | 范围 | 编号 | 内容 |
|---|---|---|---|
| 左上 | (0,0)-(1024,1024) | W1 | 左侧较高残墙：灰褐方石砌体，左高右塌，墙根带少量散石 |
| 右上 | (1024,0)-(2048,1024) | W2 | 右侧较矮残墙：同一石材和受光，破口不同，禁止镜像 W1 |
| 左下 | (0,1024)-(1024,2048) | R1 | 独立散石堆：三到五块落石，可重复摆在墙脚 |
| 右下 | (1024,1024)-(2048,2048) | C1 | 装货侧视板车：木板车厢、一只清楚辐条大轮、麻袋和木箱；无长车辕、无马 |

墙是路侧残段，不是城门、拱、院墙或长条夯土墙。车比两段墙矮。所有脚点完整，不裁断，不共享地面或底座。

## 参考图

先从已生成参考图裁局部，不上传整图。源图：

`C:/Users/Administrator/Pictures/gptGen/e0_j1_gate_transition_reference_v1/e0_j1_gate_transition_v1.png`

先核对其实际尺寸，再按相对范围裁切并用 Nearest 放大，保存到输出目录 `references/`：

1. `01_left_wall_shape.png`：源图左侧残墙，约 x=0–0.30、y=0.30–0.50。只取方石残墙形状。
2. `02_right_cart_wall_shape.png`：源图右侧板车和矮墙，约 x=0.70–1、y=0.32–0.52。只取板车装货形态和较矮石墙。
3. `03_art_style.png`：原样复制 `C:/Users/Administrator/Downloads/微信图片_20260928101137_14_455.png`，只取像素色簇和材质。

忽略裁图中的道路、天空、远山、地面和其他重复小物件。不上传 E0 夯土墙、N3 卵石院墙、J1 空车、整张过渡参考图或任何带框线的分析图。

## 请求

```text
POST https://api.muskapis.com/v1/images/edits
multipart image[]: 01, 02, 03
model: gpt-image-2.5-sunburst
size: 2048x2048
quality: max
input_fidelity: high
background: transparent
output_format: png
n: 1
retries: 0
```

提交前解析实际 multipart，确认 `background=transparent`、`output_format=png` 和三张图顺序都在请求体中。Key 只读 `MUSK_API_KEY`，不写入文件或回复。失败即停止，不改白底、不自动重试。

输出目录：

`C:/Users/Administrator/Pictures/gptGen/e0_j1_transition_props_g1_v1/`

保存 `e0_j1_transition_props_g1_v1.png`、中英文 prompt、request、response、status、三张参考副本和 manifest。同名成功结果已存在时停止。

实际英文 prompt 必须与下方英文全文逐字一致。

## 中文提示词

制作一张 2048×2048、真正 RGBA 透明的高清像素化三国战场环境道具组图。严格分成四格，每格只有一件完整、可独立放置的道具，禁止跨格。

格位（左上原点，右/下边界不包含）：
- 左上 (0,0)-(1024,1024)：一段左侧较高的残墙。灰褐方石砌体，左侧高、向右塌落，墙根只有少量附着散石。它是路侧残段，不是门、拱或完整城墙。
- 右上 (1024,0)-(2048,1024)：一段右侧较矮残墙。使用与左墙相同的方石、砂浆和日间受光，但高度、破口和轮廓都不同。禁止把左墙镜像后放入此格。
- 左下 (0,1024)-(1024,2048)：一堆独立散石，只含三到五块大小不同的落石，可单独重复摆放。
- 右下 (1024,1024)-(2048,2048)：一辆古代木板运输车的清晰侧视。画全木板车厢、一只大而清楚的辐条轮、车轴和车底；车厢内有麻袋与少量木箱。不要长车辕，不要马和人。车体高度明显低于两段残墙。

参考职责：图1只提供左侧方石残墙的砌法和塌落轮廓，忽略道路、天空、山和地面。图2只提供右侧装货板车和较矮石墙的形体，忽略道路、远景和其他小物件。图3只提供高清像素色簇、厚重石木材质和冷暖受光，不复制其中人物、武器、军旗或城市。

统一日间光照，亮面暖、暗面偏冷。轮廓清楚，缩小后仍能辨认。每件居中，距离格边界至少 80px，相邻格之间保持约 160px 真实透明隔离。画布其余区域全部透明。不画格线、编号、文字、边框、地面、共享底座、投影、天空或棋盘格。

禁止：城门、拱门、门楼、院墙、长条夯土墙、镜像复制、房屋、屋顶、树、草、路、远山、人物、马、旗帜、武器、文字、第二辆车、长车辕、白底、灰底、绘入棋盘格、光晕、照片、光滑3D、萌系绘本、低分辨率噪点。

## English prompt

Create one genuinely transparent RGBA environment-prop atlas, 2048x2048, for a high-definition pixel-cluster Three Kingdoms battlefield. Use exactly four cells. Each cell contains one complete, independently placeable prop. Nothing may cross a cell boundary.

CELLS (origin at upper-left; right and bottom edges are exclusive):
- TOP-LEFT (0,0)-(1024,1024): one taller ruined roadside wall. Gray-brown rectangular stone masonry, high at its left end and collapsing toward the right, with only a few loose stones attached at its base. It is a ruined wall segment, not a gate, arch, or complete city wall.
- TOP-RIGHT (1024,0)-(2048,1024): one lower ruined roadside wall. Use the same rectangular stone, mortar, and daylight lighting, but change its height, breaches, and silhouette. Do not mirror the top-left wall.
- BOTTOM-LEFT (0,1024)-(1024,2048): one standalone rubble cluster containing only three to five loose stones of varied sizes. It must be repeatable by itself.
- BOTTOM-RIGHT (1024,1024)-(2048,2048): one ancient wooden baggage cart in a clear side view. Include the plank cargo bed, one large readable spoked wheel, axle, and underside. Load the bed with sacks and a few wooden crates. No long shafts, horse, or person. The cart is clearly shorter than either wall.

REFERENCE ROLES: Image 1 supplies only the left rectangular-stone ruined wall and its collapse. Ignore its road, sky, mountains, and ground. Image 2 supplies only the loaded side cart and lower stone wall. Ignore its road, distance, and incidental props. Image 3 supplies only high-definition pixel clusters, heavy stone and timber, and warm-cool daylight. Do not copy its people, weapons, flags, or city.

Use consistent daylight, with warm lit faces and cooler shadows. Keep silhouettes readable when reduced. Center each prop with at least 80 px of internal transparent margin and about 160 px of real transparent separation between cells. Everything else is transparent. Do not draw cell lines, labels, text, borders, ground, a shared base, cast shadows, sky, or a checkerboard.

Forbidden: gate, arch, gatehouse, courtyard wall, long rammed-earth wall, mirrored duplicate, house, roof, tree, grass, road, mountains, person, horse, flag, weapon, text, second cart, long shafts, white or gray background, painted checkerboard, halo, photograph, glossy 3D, cute storybook, low-resolution noise.

## 验收

用 Pillow 核验 2048×2048、8-bit RGBA，并记录透明像素数、SHA-256 和四角 Alpha。输出白、深灰、土色三底预览给用户。

格位只是生成约束。若四件轮廓完整但没有落在固定切线内，按实际 Alpha 连通区域记录范围，不按 1024 硬切。若两件相连、贴边、缺脚或透明失败，停止，不自动修图或再次付费。不把生成成功写成美术验收通过。
