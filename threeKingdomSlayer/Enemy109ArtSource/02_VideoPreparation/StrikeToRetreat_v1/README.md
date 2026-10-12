# Enemy109：刺击收尾 → 背向返航的转身视频准备 v1

用户随后明确“已确认，发起生成，时长为6秒”。已上传原确认头尾副本各一次，仅创建一次 `task_c67ba690deac46f2bb2cb17c31f0471f`；视频现已完整下载和解码。4秒是历史准备提案，最终使用6秒，动作分段相应调整。用户随后“已验收，请挑选12帧有效帧”，视频已验收；12张原始帧已导出到 `Enemy109ArtSource/03_SelectedFrames/StrikeToRetreat_v1/raw/`，原12帧作为历史原始交付保留。后续用户指定第02张f010为新的StrikeToRetreat头帧和Strike尾帧并授权抠图替换，已另建f010_v2的11张透明帧、Clip/Prefab接线并验证；详见 `Enemy109ArtSource/03_SelectedFrames/StrikeToRetreat_f010_v2/README.md`。玩法时长与伤害参数保持。

原片：`C:/Users/Administrator/Videos/doubaoVideo/enemy109_strike_to_retreat_v1_720p_6s.mp4`，3,788,350字节，SHA-256 `a207d2ce1310bb22e36045348bd9c6039f59bc0f784b88b28a38fa28f2cb3d96`。实际960×960、24fps、145帧、约6.04秒、无音轨，MP4容器和FFmpeg全量解码通过。API usage 130500 tokens，cost原值spend=0.7308，币种未返回。

## 成片预审与查看

原片已获用户验收。此前预审发现枪尖在中段越框和尾部站定，保留为历史观察，不覆盖用户验收。按用户要求挑选12帧时已避开裁切与低于32px留白的区间，所选为f001/010/027/035/039/043/072/075/079/087/104/145，保持全部原始RGB像素；不修改原片。

查看 `generated_review_6s/six_second_turn_review.webp`（480×480、145帧，循环仅用于审阅）、`motion_contact_sheet.png`、`endpoint_input_vs_video.png`和`findings.md`。其中`diagnostic_frames/`只含诊断样本，不是已验收生产选帧。原MP4与头尾原件保持哈希。

## 正式候选目录

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\02_VideoPreparation\StrikeToRetreat_v1\
```

生成时只使用 `upload/` 中两张PNG：

头帧：

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\02_VideoPreparation\StrikeToRetreat_v1\upload\first_frame_attack18_aligned_green_015962_960.png
```

尾帧：

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\02_VideoPreparation\StrikeToRetreat_v1\upload\last_frame_actual_retreat_f001_green_015962_960.png
```

完整中文、英文生成词：

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\02_VideoPreparation\StrikeToRetreat_v1\prompt.zh.txt
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\02_VideoPreparation\StrikeToRetreat_v1\prompt.en.txt
```

## 头尾帧职责与处理

- 头帧是当前已入库Attack18的源帧f103：马朝画面右侧、骑手仍面向玩家、长枪仍伸出。因此本段同时做轴向收枪和余下转向，没有新增独立“收枪完毕”图。
- 尾帧来自已验收4秒背向返航视频的真实第1帧（t=0.0），不是背面概念图，也不是转回Idle视频的头帧。
- 头帧采用已存在T79透明图，按已知1024→960制作尺度统一Nearest缩放0.9375倍为900×900，放在960画布 `(12,59)`；仅一次整体变换，不修改部件或按宽包围框fit。
- 尾帧保留真实960画布与位置，按SVL Chroma / auto_key / T79 / softness16 / halo1 / 半透明转不透明处理上传副本。100087个主体核心像素0删除、0改色；691个可见边缘像素做反混合RGB变化，不能称全图像素原样。
- 两张都合成到相同平底RGB `(0,159,62)`，960×960 RGB不透明PNG；四角共16384像素的背景std均0，深主体RGB未在合成时改色。没有全局调色、重绘、插帧或新增收费生图。
- 透明蹄底均为源画布左上坐标第891行；安全边距头106/184/116/68px、尾304/51/259/68px（左/上/右/下）。两端留白通过，不代表生成中间帧必定不越界。
- 头帧y偏移不同于此前Idle对齐方案。将来部署本段需要单独记录该单一源空间映射，不能直接套旧补边值宣称无切换跳变；身体比例、根位置与转向过程仍需目检。
- 原始PNG及已验收返航MP4保持哈希不变。`head_tail_manifest.json`记录原件/上传副本哈希、背景统计、处理配方与局限。

## 实际请求参数及动作

| 字段 | 值 |
|---|---|
| 模型 | seedance-2-fast |
| 视频源时长 | 6秒（用户指定，非游戏等待） |
| 档位 / 比例 | 720p / 1:1 |
| 输入 | first_frame + last_frame，两张实际上传副本 |
| 音频 / 水印 | 关闭 / 关闭 |
| seed | -1 |
| 动作类别 | 一次非循环过渡，无API loop字段 |
| 背景 / 透明度 | RGB(0,159,62)绿幕；视频不请求透明 |
| 质量 | 由720p档位指定，不新增未验证quality字段 |
| 负面词 | 已包含在两个提示词正文，不用旧API negative_prompt字段 |

动作路径为：右侧身 → 右后斜背 → 正背面；马只做剩余约90度，骑手取消朝玩家的扭身并回正。右手收枪、左手收缰、马换蹄转向同步重叠，枪沿身体外侧转到右手竖持。尾帧是接背向跑步的支撑相位，不延长站定，也不在本段增加完整奔跑循环。

## 仅供预览和追溯的目录

`review/` 的总览和细部图仅用于检查，不是上传输入：

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\02_VideoPreparation\StrikeToRetreat_v1\review\head_tail_green.png
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\02_VideoPreparation\StrikeToRetreat_v1\review\head_tail_neutral.png
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\02_VideoPreparation\StrikeToRetreat_v1\review\retreat_tail_matte_details.png
```

`source/` 保存原件字节副本与尾帧局部抠图：原Attack透明/RGB、实际返航f001 RGB、实际返航f001 T79 RGBA。这些是追溯或抠图检查图，不作为本次API输入。临时脚本在 `Library/Locus/tmp/cavalry_concept/strike_to_retreat_v1/`，不能作为跨主机唯一恢复入口。

## 验收重点与下一步

1. 人马朝已确认方向连续转动，无回正面、额外转圈、贴图硬旋、滑蹄。
2. 收枪与转向有时间重叠；同一右手、单根直枪，不穿身体、不换手。
3. 主体身份和比例持续一致，马用短步真实转向，尾帧准确接返航f001。
4. 原始首尾绿幕已统一，但生成后仍需独立测背景时间线、主体材质和实际尾帧，不把统一输入当模型锁色保证。
5. 视频已验收且用户已授权f010端点抠图替换部署，结果在 `Enemy109ArtSource/03_SelectedFrames/StrikeToRetreat_f010_v2/`。下一步目检该版透明序列和游戏速度；原片、诊断和初次12帧保留，未重生成。

实际视频结果位置：

```text
C:\Users\Administrator\Videos\doubaoVideo\enemy109_strike_to_retreat_v1_720p_6s.mp4
```
