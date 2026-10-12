---
id: kd_454a66a5-a72e-4174-8360-f946ad437cc2
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# Enemy109：刺击后转身接返航的视频准备

用户已确认“收枪与回身同步，马从画面右侧身继续转到正背面”，随后明确“已确认，发起生成，时长为6秒”。唯一任务 `task_c67ba690deac46f2bb2cb17c31f0471f` 已success，成片完整下载/解码；用户随后“已验收，请挑选12帧有效帧”，视频已验收。12张原始有效帧已导出；随后用户指定第02张f010为新的StrikeToRetreat头帧及Strike尾帧，授权抠图替换部署。最新11帧透明版、Unity接线及运行验证已完成，见本文末尾f010_v2；原12帧与原片保留，历史预审不覆盖验收，玩法时长/伤害未改。

## 候选交付

候选素材根目录：

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\02_VideoPreparation\StrikeToRetreat_v1\
```

- `upload/first_frame_attack18_aligned_green_015962_960.png`：当前 Attack18/f103（侧身、枪仍伸出）作为本段起点。来自既有 T79 RGBA，整体 Nearest 缩放 0.9375 倍到 900×900，置于 960 画布 `(12,59)`；已知尺度依据为此前返航输入 1024→输出960，另以当前蹄底对齐一次整体位置。没有拆部件、逐帧 bbox fit 或重绘。
- `upload/last_frame_actual_retreat_f001_green_015962_960.png`：已验收背向返航视频的实际首帧 f001 / t=0.0，保持原 960 画布/位置。另存副本按现行 SVL T79 / softness16 / halo1 / 半透明转不透明局部抠图，再统一制作背景。不是背面概念图或 TurnToIdle 首帧。
- `prompt.zh.txt`、`prompt.en.txt`：完整双语生成词，包含动作时序、形象、武器/步态/根位置约束、背景与负面词。
- `request-spec.json`：实际请求 `seedance-2-fast / 6s / 720p / 1:1 / 无音频无水印 / seed=-1`。4秒是历史准备提案；用户指定6秒后仅调整源时长和对应分段，头尾帧不变。仅 first_frame + last_frame，不混 reference_image/video/audio；负面词放在正文，非循环不使用虚构 API loop 字段。
- `head_tail_manifest.json`：原件、来源视频、抠图源码与上传副本哈希、背景统计、处理记录。
- `review/head_tail_green.png`、`review/head_tail_neutral.png`、`review/retreat_tail_matte_details.png`：头尾总览与枪杆/枪缨/手缰/马尾/蹄部对照，仅供审阅，不能上传作输入。
- `source/`：Attack 原 RGBA/RGB 与返航实际首帧 RGB 字节副本、返航首帧 T79 RGBA；只供追溯，不改已验收原件。

## 验证

- 头尾上传副本都是 960×960 RGB 不透明 PNG，四角16384像素背景中位均 `(0,159,62)`、std均0。
- 头透明轮廓边距 L/T/R/B=106/184/116/68px；尾304/51/259/68px。两端透明蹄底均在第891行（源画布左上原点）。不能据此宣称生成中间帧无越界。
- 返航首帧重新抠图使用同一 SVL 源码：主体核心100087像素0删除、0改RGB；691个可见边缘像素反混合处理，单独记录，不能称全图原色不变。两张合成到统一背景时的原不透明主体颜色保持。
- 已目检上传原尺寸图、绿/灰底总览与尾帧枪/缰/尾/蹄细部，没有明显新增主体大片缺口或枪杆断裂；仍待用户美术审阅。
- 来源返航MP4哈希仍为 `9adf1f6be61a6c02bcbcf675f7d9e88981f2b1b2cb3d72aee768b0e0d5e343cd`。实际f001采用历史解码记录保存的PNG，本次没有重新解码视频；PNG与原件副本逐字节相同。
- 准备脚本在 `Library/Locus/tmp/cavalry_concept/strike_to_retreat_v1/prepare_head_tail.py`；该处是忽略的临时实现，跨主机恢复以候选目录、manifest、完整prompt/spec为入口。

## 动作内容

1. 0–1.0s：右臂沿原刺击轴开始收枪、左手收缰、马回收冲击重心与开始换蹄，同步启动。
2. 1.0–3.9s：继续余下约90度转向，右侧身→右后斜背；骑手取消面向玩家的扭身，枪沿身体外侧带到右侧并逐渐竖持。
3. 3.9–5.4s：人马完成正背面姿势、右手完成竖持枪、正常支撑调整。
4. 5.4–6s：短暂经过返航首帧的支撑相位，续接已有视频的下一帧；不额外长停或完整跑步循环。

本段只覆盖成功刺击后的返航入口；被打断从Charging姿势进入返航的入口不自动共用本段。严格保护单根直枪、同一右手、真实换步、同一阵位根位置和固定镜头，禁止镜像、硬切、整幅贴图硬旋、穿身体或额外攻击。

## 仍待审阅与生产边界

- 用户已确认头尾副本和生成词并指定6秒生产；仅上传两次、创建POST一次，无自动重新生成。首轮12次查询仍generating，后续同task返回success。
- 实际成片：`C:/Users/Administrator/Videos/doubaoVideo/enemy109_strike_to_retreat_v1_720p_6s.mp4`，3,788,350字节，SHA-256 `a207d2ce1310bb22e36045348bd9c6039f59bc0f784b88b28a38fa28f2cb3d96`；H.264、960×960、24fps、145帧、约6.04秒、无音轨，容器及FFmpeg全量解码通过。最后分段曾超时，保留前三段并续传最后一段，最终身份/长度一致；未重下已完成部分或重生成。
- API usage completion_tokens/total_tokens=130500；cost原值spend=0.7308，币种未返回。
- `generated_review_6s/`含20张时间线总览、转向细节、最后16连续帧、真实首尾对照和145帧WebP，以及全帧边界/色彩报告；均用于审阅，不是生产选帧。
- 历史成片预审：右侧身→斜背→正背方向符合；水平枪尖中段越框（原尺寸f054确认），36帧触及4px外围，57帧低于32px安全带，最小L/T/R/B=0/5/0/60px。約4.4秒后趋于背向站定。当时等待验收，之后用户已验收视频，并授权选帧、f010端点替换及抠图部署。越框区间未选，原片本身不宣称边界修复。
- 上传背景都为(0,159,62)，输出全145帧背景中位R0–3/G143–155/B59–63，首(0,155,59)、尾(0,150,61)；未完成主体局部配准保色验收，不把绿幕统计当主体染色因果。
- 原片与原确认输入哈希保持，user_art_verdict=accepted（用户“已验收，请挑选12帧有效帧”）；历史越框/停顿意见不覆盖用户验收。原片未改色、删帧、补生或重生成；6秒仅为制作源时长，玩法`strikeRecover=0.22s`未改，暂无Unity接入。

## 12帧有效原帧（已导出，帧序待用户查看）

- 目录：`Enemy109ArtSource/03_SelectedFrames/StrikeToRetreat_v1/raw/`；共12张960×960 RGB PNG。
- 源帧：f001/010/027/035/039/043/072/075/079/087/104/145。保留实际视频首尾，覆盖收枪、侧身回正、斜背转向、竖枪和背向支撑，压缩末段重复站姿，避开枪尖裁切/低于32px留白区间；其中f043→f072跳过裁切区段，连续播放观感仍需审阅。
- `selected_frames_manifest.json`与`selected_frames.csv`记录源帧号、时间、顺序、动作分组与哈希；`review/selected_frames_overview.png`和`review/selected_frames_order_review.webp`仅用于审阅，WebP每帧125ms不是原片/游戏时长。
- 独立重解码回读12/12原始像素完全一致、12个不同RGB帧、CSV/预览/总览顺序匹配；4px边缘前景全0，最小L/T/R/B=44/33/38/64px。原片哈希未变，没有裁切/缩放/平移/调色/插帧/抠图/Unity导入。
- 视频状态accepted；原12帧清单保存初次pending状态作历史。用户随后指定f010为新的转身头帧和Strike尾帧并授权抠图替换，实施结果见下节。

## f010_v2：已抠图替换部署与验证（2026-10-11）

- 最终目录：`Enemy109ArtSource/03_SelectedFrames/StrikeToRetreat_f010_v2/`，`keyed_ui_t79/`包含11张960×960RGBA，源序f010/027/035/039/043/072/075/079/087/104/145。原12帧、原f001与MP4哈希不变，不补帧或重生成。
- SVL逐帧auto/T79/softness16/halo1/半透明转不透明，7张代表帧与Idle同配方回归通过；批量Alpha0/255，幕布与隐藏RGB0，1,359,179核心像素次0删除/改色。14,321边缘像素次反混合单独记录。
- 1108部署整幅Nearest16/15到1024、左上(61,57)，依据原Strike头帧映射逆变换，不逐帧bbox fit。Attack18仅覆盖PNG保留meta/GUID，与新StrikeToRetreat1字节及像素一致，两端局部颜色/姿势匹配；不宣称全序列/Idle色差全部修复。
- 新Clip `Assets/Animations/Enemy_109_StrikeToRetreat.anim`（11键/30fps/0.366667s/非Loop），Controller状态和Prefab `CavalryVisualController.strikeToRetreatAnimation`直接引用。RetreatPose使用新f145末帧，0空Sprite/位移/事件。
- 成功StrikeRecover之后在原有返航时间（剩余排数×moveSpeed）内播放一次，受阻及HitStop暂停/续播，前置打断不共用成功动画；0.18s前探、0.22s收招、0.2s每排/冷却/伤害参数保持。专用背向奔跑和打断入口仍未制作部署。
- 完整编译/域重载、实际StrikeRoutine、阻塞续播、打断0伤害、HitStop、落马/池复用通过；临时独立列管理器实际CavalryCycle：row3→row1挡路者后一排→Attack12唯一32伤害→11帧转身只进入一次→row3→TurnToIdle→Idle/speed1通过。
- 首次完整循环探针被场景UI timeScale0干扰并超时，已清理且不作通过记录；隔离场景驱动后重测成功。停用场景根导致1条DamageNumber inactive无法StartCoroutine测试环境日志，不宣称Console0，未改跳字系统。最终Edit Mode、0临时探针、Battle/Y均clean，未保存场景或提交Git。
- 三底总览、帧序WebP与同画布首尾对照在`review/`；manifest和两份runtime报告、`unity_verification.json`已落盘。透明美术与实际速度仍待用户目检，未覆盖自然路线手势/多骑兵/全波次。
