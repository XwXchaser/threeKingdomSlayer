---
id: kd_ebebf1a0-79e5-49be-b326-ae9b750408bb
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
summary: J1 G2植被遮挡透明组图准备包：两棵中央树、两种高草和中高灌木，固定2048格位、双语提示词与透明验收已完成，尚未批准或提交。
---

# J1 G2 植被遮挡组图准备

## 状态

- **已按用户“发起生图”提交一次并生成**：HTTP 200，2048×2048 RGBA(Color Type 6)，7,158,283 bytes，耗时约122.36秒，host `r2.52image.xyz`，无重试。
- 原图 SHA-256：`25ce19cfae5c4937564b959b5bd715f1609b53b3f33cfb3e6f5923ee9d2e9473`；Alpha 0–254，四角透明有效。
- **偏差**：模型未遵守内部竖直格线，树对与下排三件在 x=1023/1024、639/640、1279/1280 两侧均有不透明像素；仅上下两排之间（y=1279/1280）完全透明。不能按原 x 坐标直接裁切。
- 当前状态 `generated_pending_user_visual_acceptance`，未裁切、未导入、未修改场景；总交接见 `plan/j1-art-reference-and-vegetation-handoff-20261012.md`。
- 用户要求根据 v6 构图补充树木和高草等前方遮挡物，让玩家理解需要向左右两侧走。
- 右侧推车、箱子、旗帜继续复用当前正式素材；A03/A04篱笆不重新生成，只另行修正实例尺度。
- G2准备包：`C:/Users/Administrator/Pictures/gptGen/j1_vegetation_blockers_g2_v2/`。
- 生成前为0次请求；本次为唯一一次授权请求，`submission_attempts=1`；未修改Unity。

## 组图内容

单张2048×2048透明组图，五格：

- T1 `(0,0)–(1024,1280)`：宽冠阔叶树，中央上层主遮挡。
- T2 `(1024,0)–(2048,1280)`：垂枝柳形树，非对称中央上层遮挡。
- G1 `(0,1280)–(640,2048)`：直立密集高草。
- G2 `(640,1280)–(1280,2048)`：横展非对称高草。
- S1 `(1280,1280)–(2048,2048)`：中高中密灌木，填补树根和内肩缝隙。

每格64px透明安全边：树格安全区域 `(64,64)–(960,1216)`、`(1088,64)–(1984,1216)`，根脚目标y=1215；下排安全区域 `(64,1344)–(576,1984)`、`(704,1344)–(1216,1984)`、`(1344,1344)–(1984,1984)`，根脚目标y=1983。坐标是排版目标，不能用裁断主体来满足。

统一低机位、正面略侧视和左上暖日光；树冠和草灌下部需要近乎不透明的实叶体量，不能只画透空枝叶骨架。素材之间独立透明，不带地面、石块、土台、路牌、篱笆或共享阴影。

## 参考与文件

上传5张实际参考：

1. `references/01_v6_broad_tree_detail.png`：宽冠树叶团、粗枝、暖灰绿材料。
2. `references/02_v6_willow_tree_detail.png`：垂枝树枝叶节奏。
3. `references/03_v6_tall_grass_detail.png`：高草叶片、干黄/灰绿和根部密度。
4. `references/04_v6_shrub_detail.png`：灌木团块和阴影。
5. `references/05_original_three_kingdoms_art_style.png`：三国高清像素化材料与大体块。

局部参考由 v6 画面裁取并3倍Nearest放大，只为查看形体，不是抠图或成品；旧背景偏重裁图移入 `review/rejected_intermediate/`，不上传。审阅图 `j1_vegetation_atlas_layout_review.png`、`j1_v6_blocking_roles_review.png`、`j1_vegetation_reference_contact_sheet.png` 不上传。

完整提示词：

- `j1_vegetation_atlas_g2_v1.prompt.zh.txt`
- `j1_vegetation_atlas_g2_v1.prompt.en.txt`

无密钥请求草稿：`j1_vegetation_atlas_g2_v1.request.prepared.json`。
状态：`j1_vegetation_atlas_g2_v1.status.json`。

## 拟请求参数

- `POST https://api.muskapis.com/v1/images/edits`
- multipart/form-data，字段 `image[]` 五张
- `gpt-image-2.5-sunburst`
- `quality=max`
- `input_fidelity=high`
- `size=2048x2048`
- `background=transparent`
- `output_format=png`
- `n=1`
- 只提交一次，不自动重试；透明能力、Color Type 6和格位服从必须以实际返回PNG验收，准备参数不构成成功保证。

## 生成后停止条件

- 若返回RGB、画入棋盘格、主体跨格、树根/草根裁断或混入完整场景，记录失败并停止，不自动重试。
- 机器检查PNG、RGBA、四角透明、空白格透明、五格无污染；白/深/赭土底检查边缘和主体透底。
- 视觉检查五件独立素材是否可辨、树冠/高草/灌木是否有足够实叶体量，以及是否保持三国像素战场风格。
- 即使机器检查通过，也要等待用户逐件验收；不自动裁切派生、不导入Unity、不修改篱笆或场景。
