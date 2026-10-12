# Enemy109 StrikeToRetreat：12帧原始有效帧

已验收原片按动作节点选出12张真实视频解码帧。文件位于 `raw/`，保持960×960 RGB原始源画布；逐帧像素与对应视频解码帧完全相同。没有裁切、缩放、平移、调色、插帧或抠图。

选帧覆盖刺击余势、收枪起步、侧身转向、背后四分之三转向、竖持长枪、背向支撑和真实视频末帧。源视频f013–025和f046–068一带枪尖裁切区间不选。全部所选帧4px边缘前景为0，四边最小留白满足32px筛选。

顺序、源帧号、时间、动作分组、边界与SHA-256见 `selected_frames_manifest.json` 和 `selected_frames.csv`。`review/selected_frames_overview.png`及`review/selected_frames_order_review.webp`仅供预览；WebP每帧125ms只是看帧顺序，不是原片时序或游戏动画时序。

**本目录保留初次12帧原始交付。** 后续用户指定第02张f010作为新转身头帧与Strike尾帧并授权抠图替换，已另建 `Enemy109ArtSource/03_SelectedFrames/StrikeToRetreat_f010_v2/`：11张T79透明帧、三底预览、部署与运行验证记录。原12张RGB、清单和预览不改写；f001仅从新部署序列排除，原件仍保留。正式透明帧与Unity接线以v2 README/manifest为准。
