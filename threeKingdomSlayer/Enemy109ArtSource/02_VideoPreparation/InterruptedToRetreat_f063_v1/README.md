# Enemy109：f063 → 背向返航转身桥 v1（已生成，视频待验收）

本包用于准备一段新的非循环转身桥：从已采用的 `ChargeInterrupted11_f063_t2.583.png` 恢复重心、收枪牵缰、人马短步转向，最后接已有已验收返航视频的真实 f001。**已按确认规格仅创建一次视频任务并完整下载解码；成片现待用户美术验收。**

## 已对齐上传副本

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\02_VideoPreparation\InterruptedToRetreat_f063_v1\upload\first_frame_f063_aligned_green_015962_1024.png
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\02_VideoPreparation\InterruptedToRetreat_f063_v1\upload\last_frame_retreat_f001_aligned_green_015962_1024.png
```

- 两端均1024×1024 RGB，目标底色`(0,159,62)`，四角std=0。
- 保持原生960像素尺度，不裁切、不缩放、不扭曲、不重绘主体。
- f063仅在副本上整体平移`(-10,+103)`，再放入1024画布`(22,135)`；返航f001只加32px边放入`(32,32)`。
- 两端透明支撑底统一到y923，最后6行外侧支撑中点统一到x503；这是整体源空间对齐，不是逐帧bbox fit。
- f063原RGBA SHA-256：`878107393ca8607dff93632f1b3a6249450851a3a56105a678614fdade9204b2`。
- 返航f001原RGBA SHA-256：`9f66646ecb9c624343ee2272dfdc3e37f19e1b000b3e59dc2df3fce091080ab9`。
- 对齐副本的详细尺寸、锚点和保护源记录见`head_tail_manifest.json`；对照图在`review/`。

## 动作目标

1. `0–0.7s`：f063前倾状态恢复胸肩/头盔，骨盆和靴保持坐鞍，不回完整正面Idle。
2. `0.3–1.3s`：恢复重心同时收枪，右肘靠近腰侧，枪沿身体外侧抬尖；左手牵缰引马头向画面右侧。
3. `0.8–3.3s`：正面→画面右侧侧身→后侧3/4，马头/颈先转，前后蹄以短步完成重心转移，骑手骨盆随鞍、上身略滞后。
4. `3.0–4.0s`：后侧3/4收至正背面，匹配真实返航f001的背向支撑相和右侧竖枪，随后复用已有返航Loop。

## 规格

- `seedance-2-fast / 4秒 / 720p / 1:1 / seed=-1 / 无音频 / 无水印 / 非Loop`。
- 仅使用 `first_frame` + `last_frame`，不混入普通参考图、视频或音频。
- 不使用被拒收的 f097；不重新生成返航Loop。
- 不新增攻击、受击、落马、跑步长镜头、相机运动、整体缩放、根漂移或纸片式旋转。
- 对齐副本、完整双语词和规格已展示，用户明确“确认并生成”后仅创建一次；原件未改动。

## 实际视频（待用户美术验收）

```text
C:\Users\Administrator\Videos\doubaoVideo\enemy109_interrupted_to_retreat_f063_v1_720p_4s.mp4
```

task `task_9af5590e1ede438fa2ca54e6210aa74c`；create POST仅1次；2,833,549 bytes；SHA-256 `b1c5094dc2eb5503ee9ccd00c02a231eb9c6978e775c9094861b7830a05f8daa`。实际960×960、24fps、97帧、4.04秒、无音轨；MP4结构和FFmpeg全量解码通过。usage completion/total=87300，cost.spend=0.48888，API未返回币种。本任务有限轮询第12次返回success，后续GET用于下载；未出现查询超时，未重建任务。

## 生成后预览（只供审阅）

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\02_VideoPreparation\InterruptedToRetreat_f063_v1\generated_review_4s\motion_contact_sheet.png
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\02_VideoPreparation\InterruptedToRetreat_f063_v1\generated_review_4s\input_vs_actual_endpoints.png
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\02_VideoPreparation\InterruptedToRetreat_f063_v1\generated_review_4s\last_12_consecutive.png
```

原尺寸样本显示：f014早段直立持枪；f033/f052马朝画面右侧，f059/f065又朝画面左侧，f071转后侧、f084之后正背面。**未满足预定的单向180度紧凑转身，中段更像原地小跑，末段有短暂背向直立停留。** 这些是代理预审，用户视频verdict=pending，不自动重生成。

97帧外4px诊断无前景，最小边距L/T/R/B=43/40/88/90px。所有32px边距筛选通过，但部分帧低于Prompt64px目标；输入留白并不保证模型全程保持64px。背景中位范围R0–0/G143–152/B55–62，偏离输入键且随时间变化；主体颜色未姿态配准独立验证。技术解码/边界检查不等同美术通过。

`generated_review_4s/four_second_bridge_review.webp`为480×480、97帧的全片顺序预览，按约24fps审阅，循环只为预览；顺序与每帧缩小RGB经回读核对。`validation_and_color_report.json`记录全帧背景和边距，`diagnostic_frames/`仅审阅抽帧，不是生产帧清单。

原始f063、返航f001、已验收返航MP4与f097拒收素材均保留。没有新建返航循环、没有帧选、抠图或Unity部署；后续生成任务需重新确认。