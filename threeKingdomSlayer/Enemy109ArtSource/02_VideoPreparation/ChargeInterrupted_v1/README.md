# Enemy109 冲锋被打断视频 A 段 v1（透明01–11已验收，f097不采纳）

用户要求“发起视频生成”，完整双语Prompt、最终规格和同底首尾副本展示后另明确“确认并生成”。**已仅创建1次视频任务，下载/容器/全量解码完成；用户已验收原视频，并要求挑选有效帧。** 12帧序列已获独立验收；f029抠图小样也已验收，用户明确授权同参数继续，整套透明帧已完成。用户最新确认透明帧01–11验收通过；透明帧12/f097因色差不采纳并保留追溯，采用终态改为f063。无新收费调用。

## 范围与参数

- 单段 A：K1受击接触/回仰 → 马前蹄落地承重 → 骑手滞后惯性前扑 → K2急停尾态。
- `seedance-2-fast / 4s / 720p / 1:1 / seed=-1 / generate_audio=false / watermark=false`，非Loop（Prompt约束，不提交loop字段）。
- 首尾模式只上传两张1024×1024 RGB PNG，不混用普通参考图/视频。
- 零秒已经受击，不增加新受击/冲锋前导；全程正面、不转身、不返航、不复原Idle。
- 生成范围为A段。12张RGB帧已验收且完成本地T79抠图；B段、返航Loop重生、Unity部署和游戏时长设定均未执行。

## 实际上传文件（指纹保持）

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\02_VideoPreparation\ChargeInterrupted_v1\upload\first_frame_k1_impact_green_015962_1024.png
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\02_VideoPreparation\ChargeInterrupted_v1\upload\last_frame_k2_brake_tail_green_015962_1024.png
```

首图SHA `649ea33ac812b1988ab5628696bc8bde120f7a609cd7f6bca9a538d47265cc01`；尾图SHA `f7b290c504516de275b4d86c5fe8e8b418b8660c9952be48e11eb394aecfa244`。

- 原图/工程原件未改，输入不缩放、不裁切、不移动、不调主体颜色。
- 同底副本沿用SVL Chroma T79 / softness16 / halo1与边缘反混合，只为制备RGB视频输入；中间Alpha用于局部背景分离，不是Unity生产透明帧。
- 269,673核心像素RGB保持，核心Alpha删除0；可见边缘重合成RGB改变K1 414 / K2 457像素，分别留档，不能宣称全图未改。
- 空背景四角实测均RGB(0,159,62)，std=0。
- 配色和背景量测不能证明生成模型未来恒色，成片还要分别检查背景时序、主体材质与首尾姿态；Charging→K1的Unity映射不在本次范围。

## 审阅材料（不是视频输出）

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\02_VideoPreparation\ChargeInterrupted_v1\review\k1_k2_input_preparation_comparison.png
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\02_VideoPreparation\ChargeInterrupted_v1\review\key_details_raw_vs_white.png
```

`prompt.zh.txt` / `prompt.en.txt`包含完整已确认词和排除项；`request-spec.json`记录规格、角色与结果；`head_tail_manifest.json`记录首尾背景、原图指纹和局部RGB保护结果。`production_record.json`与`Library/Locus/tmp/cavalry_concept/charge_interrupted_video_v1/runtime.json`是同一唯一task的技术恢复记录。

## 原始视频（用户已验收）

```text
C:\Users\Administrator\Videos\doubaoVideo\enemy109_charge_interrupted_v1_720p_4s.mp4
```

- task：`task_2db36264422f43af9add2480770f4e6c`；服务端success，create POST仅1次，参考上传2次。
- 2,114,011字节；SHA `d3df3049161ade578d178c658972087d55e5c24a06310b9e961f5998a40d22d9`。实际960×960、24fps、97帧、约4.04秒、无音轨；MP4结构和FFmpeg全量解码通过。请求720p是服务档位，不等同输出宽高。
- usage completion_tokens/total_tokens=87300；cost.spend=0.48888，币种未返回，不推断。
- 既有task GET曾读超时/SSL握手超时，改curl有时限GET后得到success。没有再次创建。下载四段均HTTP206/范围/长度/ETag校验通过，原视频不改色、不裁切、不替换首尾参考图。

## 生成后审阅（只供预览/追溯）

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\02_VideoPreparation\ChargeInterrupted_v1\generated_review_4s\motion_contact_sheet.png
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\02_VideoPreparation\ChargeInterrupted_v1\generated_review_4s\input_vs_actual_endpoints.png
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\02_VideoPreparation\ChargeInterrupted_v1\generated_review_4s\last_12_consecutive.png
```

- 代理抽帧观察：K1→K2方向和末段收稳，始终正面骑乘；开头约0.3–0.8秒马前躯/前蹄有追加上抬，再落稳，与直接落蹄提案有差异。该观察保留供审阅；用户后来已验收原视频，不据此重生。
- 97帧诊断未发现外4px触边；最小L/T/R/B留白120/86/242/154px。前景掩码仅诊断，不能当生产Alpha或精确解剖证明。
- 全帧空背景中位范围R0–2/G143–154/B56–60，与输入(0,159,62)有偏离/时变。主体材质未按姿态配准独立验证，不能把背景色差当作主体变色原因或声称颜色已锁定。
- 96对相邻前景RGB差异：前段均值14.819、中段6.316、尾段0.703，支持尾段收稳观察；未配准，含姿态、重绘、编码差异，不是材质色稳定度。
- `generated_review_4s/diagnostic_frames/`为审阅抽帧，不是生产挑选序列。原视频及独立选帧已验收；本地透明帧交付在`Enemy109ArtSource/03_SelectedFrames/ChargeInterrupted_v1/keyed_ui_t79/`，透明帧01–11已验收，f097透明帧不采纳、仅保留追溯。

原图与已有返航视频均保持不变。最新用户“已验收，请挑选有效帧”：原视频状态accepted，已导出12张960×960 RGB候选帧到`Enemy109ArtSource/03_SelectedFrames/ChargeInterrupted_v1/raw/`，源号f001/003/009/017/021/025/029/033/039/051/063/097。独立重解码确认12/12像素与视频源帧完全相同、CSV与预览顺序通过，原片哈希不变。帧序已验收。f029单帧T79透明小样获验收后，另11张按同参数完成抠图，f029原PNG逐字节复用；整套12张960×960 RGBA和白/中性/深底总览、顺序WebP及细节对照在同动作目录。详见`matting_report.json`、`matting_verification.json`与README；原RGB与原MP4指纹保持，没有Unity部署或Play自检。最新采用透明帧01–11，f097因用户指出色差不采纳；从f063开始接返航的动画只做内容分析，不重生成、不校色或删素材。
