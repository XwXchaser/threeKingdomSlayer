---
id: kd_093966d7-530f-44c6-8e01-fd74ed65f7b0
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# E0→J1 门后直路参考图生成交接

## 授权状态

用户已要求按成功生成方案准备交接，并让后续对话直接发起这一张参考图。授权范围只有一次候选生成和交图，不包含自动重试、拆素材、导入 Unity 或部署场景。

## 唯一任务

生成一张“刚穿过 E0 残门、前方仍是一条直路”的环境参考图。它不是 E0 正面门垣，也不是 J1 分叉。

输出目录按当前用户解析：

`C:/Users/Administrator/Pictures/gptGen/e0_j1_gate_transition_reference_v1/`

必须保存：

- `e0_j1_gate_transition_v1.png`
- `e0_j1_gate_transition_v1.prompt.zh.txt`
- `e0_j1_gate_transition_v1.prompt.en.txt`
- `e0_j1_gate_transition_v1.request.json`
- `e0_j1_gate_transition_v1.response.json`
- `e0_j1_gate_transition_v1.status.json`
- `references/` 中三张实际输入副本及 `reference_manifest.json`

若目录已有同名成功结果，停止，不覆盖。

## 输入与顺序

按顺序重复 multipart `image[]`，实际上传文件，不把路径只写进 prompt：

1. `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/E0/References/E0_ApprovedSceneReference_v3.png`  
   已读，3216948 bytes。职责：蓝天、浅云、冷灰远山、暖黄土、残门石木材质和同一世界。
2. `C:/Users/Administrator/Pictures/gptGen/changbanpo_j1_reference_workflow/j1_after_e0_reference_v3.png`  
   已读，4200596 bytes。职责：门后土路、车辙、低篱、低残墙和木辎车语言。禁止继承左右分叉、中央路牌和绿石板路。
3. `C:/Users/Administrator/Downloads/微信图片_20260928101137_14_455.png`  
   已存在，2596005 bytes。职责：高清像素色簇、厚重土石木材和冷暖关系。禁止继承人物、武器、军旗符号和完整城市场景。

不上传 v1/v2/v4/v5/v6、几何草图、旧 E1 视频帧或当前稀疏部署截图。

## 请求

- `POST https://api.muskapis.com/v1/images/edits`
- `multipart/form-data`，字段名必须是重复的 `image[]`
- model=`gpt-image-2.5-sunburst`
- quality=`max`
- input_fidelity=`high`
- size=`1024x2048`
- output_format=`png`
- n=`1`
- 不传 `background`、`mask`，不请求透明
- Key 只读取环境变量 `MUSK_API_KEY`，不写入脚本、日志、文档或回复
- 只提交一次；失败就记录状态并停止，不自动改词或重试

实际提交的英文 prompt 必须与下方英文全文逐字一致，并把中英文全文分别落盘。

## 中文提示词

制作一张三国战场动作游戏连续卷轴的环境参考图，表现刚穿过外围残门后的直路过渡段。竖屏1:2，1024×2048，完整不透明PNG。镜头无人，低机位，轻微俯视，沿一条暖黄土路向前看；地平线约在画面上方35%，下半幅大部分是空地面。

参考职责：图1只提供蓝天、浅云、冷灰蓝远山、暖黄土、土石与破损木构的同一世界，不复制它的正面石拱门。图2只提供门后土路、车辙、低篱笆、低残墙和古代木辎车的材料语言，不复制它的左右分叉、中央路牌、绿色石板路或任何箭头。图3只提供高清像素化的大体块、锐利色簇、厚重土石木材和冷暖关系，忽略人物、武器、旗帜符号和完整城市。

画面只有一条从下方中央通向中央远方的直路，路幅宽、有压实车辙，不向左或向右分叉，不出现第二条路、路口或路牌。原残门已在镜头身后，只能在左右最外缘看到它延伸出来的矮墙根，前方绝不再出现石拱、门洞、门楼或第二座城门。

两侧是刚出城的过渡：近处有稍高的夯土石残墙、倒塌墙段、散石和断木，向远处逐渐变低。左侧路肩出现一段破木篱和低土石墙，作为村落方向的伏笔，但没有房屋、屋顶、炊烟、塔或绿色石板。右侧路肩侧停一辆朴素两轮木辎车，旁边只有少量木箱、麻布包囊和散落物，预告官道运输。路肩穿插枯草、碎石、断桩和最多两面素面破红旗；旗不高大，没有文字或徽记。

远景保持明亮日间蓝天、浅云和冷灰远山。中央下半幅保持可通行空地，所有大物体都在两侧路肩。高清像素化三国战场风格，边缘清楚，不是低分辨率复古像素、照片、光滑3D或萌系绘本。

只画静态环境：无人物、士兵、百姓、尸体、手、武器、敌阵、特效、HUD、文字、箭头、路牌、水印、logo和黑边。

负面约束：左右分叉、第二条路、中央路牌、箭头、绿色石板路、前方第二座城门、回头拍摄城门、门洞占据画面、房屋、屋顶、炊烟、塔楼、牌坊、横跨道路的车或拒马、高大路灯旗、阵营符号、文字、人物、武器、UI、黄昏、浓烟、森林、照片、光滑3D、萌系绘本、低分辨率像素、模糊、拼贴、黑边、水印。

## English prompt

Create one environmental reference for the straight-road transition immediately after passing through an outer ruined gate in a continuous-scrolling Three Kingdoms battlefield action game. Vertical 1:2, 1024x2048, fully opaque PNG. Use an unoccupied low camera with a mild downward angle, looking forward along one warm ochre dirt road. Place the horizon about 35 percent below the top edge and keep most of the lower half as open ground.

REFERENCE ROLES: Image 1 supplies only the shared world: blue daytime sky, pale clouds, cool gray-blue mountains, warm ochre soil, and damaged earth, stone, and timber. Do not copy its frontal stone arch. Image 2 supplies only post-gate dirt-road material, wagon ruts, low fences, low ruined walls, and an ancient wooden cart. Do not copy its left-right fork, central signpost, green stone paving, or arrows. Image 3 supplies only high-definition pixel clusters, substantial material masses, and warm-cool contrast. Ignore its people, weapons, heraldic symbols, and complete city.

Show exactly one broad dirt road running from the lower center toward the central distance, with compacted wagon ruts. It does not branch left or right. There is no second road, junction, or signpost. The original ruined gate is behind the camera; only very low continuations of its side walls may appear at the extreme left and right edges. No stone arch, gateway, gatehouse, or second gate appears ahead.

Both shoulders show a just-outside-the-gate transition. Near the camera are moderately high rammed-earth and stone wall remnants, collapsed masonry, loose stones, and broken timber; these become lower with distance. The left shoulder has one stretch of broken wooden fencing and a low earth-stone wall as a subtle hint toward a future village, but no house, roof, smoke, tower, or green stone road. The right shoulder has one plain two-wheeled wooden baggage cart parked beside the road, with only a few wooden crates, cloth bundles, and scattered supplies suggesting an official transport route. Add dry grass, rubble, broken posts, and no more than two small plain torn red flags. The flags are ordinary roadside flags, not tall lamp-like poles, and have no text or emblem.

Keep the bright daytime blue sky, pale clouds, and cool gray mountains. Leave the central lower half travelable and place every large object on the shoulders. Use high-definition pixelated Three Kingdoms battlefield art with clear edges, not low-resolution retro pixels, photography, glossy 3D, or cute storybook art.

Environment only: no people, soldiers, civilians, bodies, hands, weapons, enemy formations, effects, HUD, text, arrows, signposts, watermarks, logos, or black borders.

Negative constraints: left-right fork, second road, central signpost, arrows, green stone paving, second gate ahead, looking back at the gate, gateway occupying the frame, houses, roofs, smoke, towers, ceremonial arches, cart or barricade spanning the road, tall lamp-like flags, heraldry, text, people, weapons, UI, sunset, dense smoke, forest, photograph, glossy 3D, cute storybook, low-resolution pixels, blur, collage, black borders, watermark.

## 完成检查

用 Pillow 验证 PNG 为 1024×2048、RGB、不透明，记录字节数和 SHA-256。核对请求 JSON 中的三张输入顺序、英文 prompt、模型和参数。然后把原图附给用户，不宣布美术验收通过。

不修改 Battle、Y 场景、路线资产、Shader、材质或现有 `E0 to J1 - Beyond Gate Shoulder` 对象。
