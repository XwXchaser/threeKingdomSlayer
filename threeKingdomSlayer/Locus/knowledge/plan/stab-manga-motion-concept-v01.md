---
id: kd_ad24e0ca-24b3-4f44-acea-c9880ff19a39
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 长枪漫画挥动方向线：概念参考 V01

## 目标与当前状态

用户要求「先生成参考图，根据当前已有的长枪造型生成概念参考图」。本轮按该指令准备并提交一次图生图，生成方向为单幅横挥概念，尚不实施Unity特效。

当前状态：一次请求HTTP200成功，PNG已下载、解码、尺寸核验、原图副本逐字节验证并实际read附图；用户反馈已明确：枪头大弧方向正确且应保留，但杆身分段短线无参考意义。实际提交1次、自动重试0。V01降级为局部效果参考，不作为整体方向线定稿。V02画风已获用户验收。下一版构图准备见 `plan/stab-manga-sweep-concept-v02-preparation.md`。

## 造型与画风参考

参考输入顺序：

1. `StabWeaponArtSource/16_MangaMotionConceptV01/Inputs/current_v02_spear_slash_pose.png`，1536×1024：当前正式V02长枪在隔离预览场景中的斜置渲染，用于锁定造型、材质、比例和概念构图。
2. `StabWeaponArtSource/16_MangaMotionConceptV01/Inputs/current_v02_spear_front.png`，768×1536：同一长枪完整竖直渲染，用于补充枪刃、金绿枪套、短银套、木杆、金色尾鐏与朱红缨布构造。
3. `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle1.png`，1108×1108：已验收画风依据，仅控制像素色簇、材质折面和冷钢/暖金/朱红关系，不加入人物。

前两张直接读取正式 `Assets/Prefabs/FantasySpear3D.prefab` 及其V02材质，没有使用旧面片或历史母图冒充当前造型。额外三分之四预览仅归档，不上传。

预览在独立PreviewScene中生成，无Play、无FBX/UV/材质改动。开工时Battle dirty=false、YJunctionSample dirty=true；预览结束后两者dirty状态保持原样，PreviewScene已清理。用户现有Y场景未保存工作不被覆盖。

## 单幅概念规格

- 单把完整长枪，枪尾左下、枪尖朝右上，保持当前V02造型和清楚材质。
- 以杆下部隐含握持区为支点，描述顺时针向右下方扫出的切向运动；不是沿枪杆向右上刺出。
- V01原规格曾要求约6–9条分段短笔触；该部分已被用户否定。下一版不再沿用分散短线，改为单一共同挥击关系下的外侧主弧与内层弧带。
- 枪头线条较长，杆身较短；强端贴近当前轮廓，旧轨迹向此前左上方尖收；不加箭头和支点标记。
- 米白/浅暖金主线、极窄深暖灰边，至多1–2笔朱红辅线。
- 暖灰不透明纯底，单幅无字，不画人物/手/场景/HUD，不画武器残影/大光环/巨大扇面/爆炸。

提示词：`StabWeaponArtSource/16_MangaMotionConceptV01/prompt_en.txt`（实际提交）与 `prompt_zh.txt`（阅读对照）。输入路径、职责与SHA-256见同目录 `request.prepared.json`。

## 请求与失败边界

- Endpoint：`POST https://api.muskapis.com/v1/images/edits`，multipart三张同名 `image[]`。
- 模型：`gpt-image-2.5-sunburst`；质量max、参考保真high、1536×1024、opaque、PNG、n=1。
- 当前用户目录输出：`C:/Users/Administrator/Pictures/gptGen/stab_manga_motion_concept_v01/stab_manga_sweep_concept_v01.png`。
- 项目归档：`StabWeaponArtSource/16_MangaMotionConceptV01/stab_manga_sweep_concept_v01.png`，已确认存在并与当前用户目录输出逐字节一致。
- 一次提交、0自动重试；超时/结果未知先保留回执，不继续重复收费提交。
- 原始响应保存在当前用户输出目录，项目保留移除签名URL/大base64的元数据与请求/验证记录。

## 成图验收

先核对实际HTTP、PNG签名/解码/尺寸/文件校验，然后实际read附图，并检查：同一把枪造型是否保留、方向线是否覆盖杆身而非仅枪尖、是否一致描述右下扫动、是否误画巨大光环/扇面/残影、线条与当前画风是否协调。

生成结果只作概念方向参考，不直接导入Unity；用户验收后再决定程序化原型的线条形状、颜色、密度和具体动作窗口。

## 实际交付与视觉评审

- 实际HTTP200，耗时约67.76秒，1536×1024 RGB PNG，1,364,806 bytes。
- SHA-256：`e115a518bb6db1d5ac3599adf1094982f61977701296e96ceefe77a69b3c5eae`。
- 完整图片、请求与状态在当前用户输出目录；项目归档保留提示词、输入渲染、request、去签名URL/大base64的response metadata、verification和visual_review。
- 主体为一把完整长枪，银刃、金绿枪套、红棕杆和金尾身份大体保持；红缨形状被简化并沿杆拉开，不能作为新的模型造型依据。
- 杆身确实加入多处彼此分离的米白/朱红短线，覆盖目标基本成立。
- 用户反馈已明确：枪头外侧大弧正是目标效果，应保留其规模、方向和力量感；问题不是弧太大，而是弧线没有与整把枪、杆身形变组织成同一次挥击。V01杆身短线不再作为参考。
- 各组笔触的共同支点/切向运动仍需设计与程序化验证，生成静态图不能证明真实动态方向一致。
- 已通过read实际附图，等待用户反馈；无追加收费生成，无Unity部署。
