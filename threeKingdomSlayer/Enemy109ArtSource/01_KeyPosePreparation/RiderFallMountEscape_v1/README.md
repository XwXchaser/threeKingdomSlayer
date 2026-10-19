# Enemy109 骑手摔落、马向左逃跑：交接关键姿态生成包 v1（验收不通过）

> 用户否决：“马匹没有惊慌感，角色倒地姿势应该后仰身而不是躺下，很难与受击倒地死亡联系上。”本候选仅保留追溯，不作视频端点或后续编辑输入。修订准备见`Enemy109ArtSource/01_KeyPosePreparation/RiderFallMountEscape_v2/`；v2已按新授权生成，待用户美术验收。

## 当前阶段

本目录对应用户已授权并完成的一次静态关键姿态图片生成。目标是为第一段“骑手受击→摔落接地”的连续视频准备交接尾姿候选：骑手已落地失力，马保持健康站立并朝画面左侧准备转向。已执行1次POST、HTTP200返回1张1024×1024 RGB PNG，用户美术验收不通过，原因见顶部记录。此图不是实际视频输出帧，也不是第二段无骑手马逃跑视频或Unity运行时素材。

- 生成范围：一次单图编辑，1张1024×1024 PNG。
- 唯一上传参考：`upload/mountedIdle1_identity_green_015962_1024.png`。
- `Charging1`只作为本地身份／装备核对，不上传，不承担动作锚点。
- 不自动重试，不自动生成视频，不抠图，不改Prefab、Animator、脚本或场景。
- 生成结果需要用户先审阅；通过后才准备第一段视频的首尾输入和第二段无骑手马视频方案。

## 参考职责

- `source/mountedIdle1_f001_960_rgba.png`：现有MountedIdle1透明源，作为首帧身份和稳定站立比例依据。
- `source/charging1_f036_960_rgba.png`：现有Charging1透明源，仅供装备、枪、马具和身份核对。
- `upload/mountedIdle1_identity_green_015962_1024.png`：MountedIdle1原960×960 RGBA每侧加32像素，原生像素尺度不变，合成到RGB(0,159,62)纯绿底；唯一计划上传图。
- `review/reference_context.png`：仅供审阅的三栏参考板，不上传。

## 目标姿态

1. 骑手已完成致命受击后的摔落，落在画面右侧／略偏前景地面，失力且与鞍座分离。
2. 马不死亡、不跪、不倒，四蹄保持完整；头颈和肩部做小幅朝画面左侧的准备转向。
3. 马鞍、马镫、缰辔和缰绳留在马上；骑手松手。
4. 长枪随骑手落地，保持一根直枪，落在骑手外侧，不穿过人马。
5. 人与马轮廓清楚分离，便于后续从真实交接帧拆出独立马逃跑素材。

## 文件

- `prompt.en.txt`：实际提交的英文提示词。
- `prompt.zh.txt`：同步审阅用中文提示词。
- `request-spec.json`：请求参数、来源哈希、授权边界和输出路径。
- `production_record.json`：提交前写入并在响应后更新的生产记录。
- `review/`：参考审阅图，不是生产素材。

## 实际产物与核验

- 项目副本：`generated/enemy109_rider_fall_mount_escape_handoff_v1.png`。
- 外部原图：`C:/Users/Administrator/Pictures/gptGen/enemy109_rider_fall_mount_escape_handoff_v1.png`；旁有请求及脱敏响应JSON。
- 1024×1024 RGB PNG，1,097,887字节；SHA256：`abc75d9438946eb61a62ffffaa011114673bf833a778ac4916c7eb6b9edaa51c`。原图与项目副本字节一致。
- 参数：`gpt-image-2.5-sunburst / quality=max / input_fidelity=high / n=1`。只提交1次，无自动重试；API未返回费用字段。
- 已回读图像：马站立并朝画面左侧，骑手位于右侧地面，两者轮廓分开；长枪落在外侧。身份、解剖、装备比例及后续视频可接性仍待用户审阅。
- 诊断边距L/T/R/B=114/225/51/143px，右边51px低于64px目标；绿幕角块中位RGB(2,164,65)，并非完全均匀的请求底色。核验仅用于记录，不代替美术验收或输出正式Alpha。
- 详见`generated_review_report.json`与`production_record.json`。

## 后续边界

本候选已否决，不用于视频端点或下一张编辑输入。v2改为马受惊＋骑手后仰失衡摔落中的关键姿态，原PNG、已提交双语词及外部请求响应保留。v2准备不等于新增生成授权；两段视频、抠图与Unity接入均未执行。第一段仍一次连续完成受击至落地，第二段从第一段实际验收的交接帧提取马后继续。