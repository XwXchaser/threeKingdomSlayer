---
id: kd_e7e47c3b-0240-4a07-a41d-814631e003c6
injectMode: inherit
summary: 需要用 ReachAPI Seedance 2 生成视频（参考图/首尾帧/参考视频）并安全轮询下载时使用；含费用确认、限次轮询、断点续传与容器校验规则。
aiEditMode: inherit
skillEnabled: true
skillSurface: command
---

# ReachAPI Seedance 2 视频生成（安全与可恢复版）

## 目的

使用 ReachAPI 官方 Seedance 2 API 生成视频。默认模型为 `seedance-2-fast`，支持纯文本、参考图片、参考视频和参考音频。Key 只能从环境变量 `REACH_API_KEY` 读取，禁止写入脚本、项目、知识库、Git、命令参数或回复。

## 官方接口

- 创建任务：`POST https://direct.reachapi.ai/v1/vids/create`
- 查询任务：`GET https://direct.reachapi.ai/v1/tasks/{task_id}`
- 文件上传：`POST https://file.reachapi.ai/file/uploads`
- 认证：`Authorization: Bearer $REACH_API_KEY`

## 默认参数

```json
{
  "model": "seedance-2-fast",
  "duration_seconds": 6,
  "resolution": "720p",
  "aspect_ratio": "16:9",
  "generate_audio": false,
  "seed": -1,
  "watermark": false
}
```

`seedance-2-fast` 不支持 `1080p`；时长必须是 `4-15` 或 `-1`。比例支持 `21:9`、`16:9`、`4:3`、`1:1`、`3:4`、`9:16`、`adaptive`。

## 参考图上传

每张本地图必须单独上传，字段名必须是 `file`，请求必须使用 `multipart/form-data`：

```bash
curl --fail --silent --show-error \
  -X POST 'https://file.reachapi.ai/file/uploads' \
  -H "Authorization: Bearer $REACH_API_KEY" \
  -F 'file=@C:/absolute/path/reference.png;type=image/png'
```

从响应的 `data.url` 读取临时 HTTPS URL。上传成功后尽快创建视频任务；临时 URL 不是永久存储。

支持 PNG、JPEG/JPG、WebP、GIF，以及音频和视频文件；单文件最大约 50 MiB。

## 创建请求

必须使用新版 `input.content`，不能使用已废弃的 `input.prompt`、`input.image_url`、`input.image_urls`、`negative_prompt` 等字段。

普通参考图示例：

```json
{
  "model": "seedance-2-fast",
  "input": {
    "content": [
      {"type":"text","text":"<可执行的镜头描述>"},
      {"type":"image_url","role":"reference_image","image_url":{"url":"<上传返回的 data.url>"}}
    ],
    "duration_seconds": 6,
    "resolution": "720p",
    "aspect_ratio": "16:9",
    "generate_audio": false,
    "seed": -1,
    "watermark": false
  }
}
```

首尾帧工作流：使用 `first_frame` 和 `last_frame`，不要同时混入 `reference_image`、`reference_video` 或 `reference_audio`。

## 参考素材规则

- `input.content` 必须是非空数组；文字用 `{ "type": "text", "text": "..." }`。
- 参考素材按角色区分：`image_url` + `reference_image` / `first_frame` / `last_frame`；`video_url` + `reference_video`；`audio_url` + `reference_audio`。
- 数量上限：最多 9 张图、3 个视频、3 段音频。
- 音频不能单独使用；“文字 + 音频”还必须至少再含一个参考图片或参考视频。
- `first_frame` / `last_frame` 不能与 `reference_image`、`reference_video`、`reference_audio` 混用。
- 单文件最大约 50 MiB；图片 PNG/JPEG/WebP/GIF，音频 MP3/WAV/M4A/AAC/OGG/WebM，视频 MP4/WebM/MOV/MKV。

## 费用与确认

创建任务会产生费用。实际发起创建前，必须向用户复述：模型、时长、分辨率、比例、音频开关、参考素材及输出路径，并取得本次生成确认。只查询已有 task 不需要重新确认。

## 防止无限轮询

创建成功后保存 `task_id`。轮询规则：

- 首次立即查询；
- 间隔 5-10 秒；
- 默认最多 12 次、总等待不超过 120 秒；
- 每次 HTTP 请求超时 20 秒；
- `success`、`failed`、`error`、`cancelled`、`canceled` 立即停止；
- 超过次数或总时限必须停止并报告当前状态；禁止后台无限轮询；
- `success` 时只读取 `data[].url`，不根据 HTTP 200 推断视频已下载。

## 稳健下载

下载结果必须先写入同目录的 `.part` 文件：

```bash
curl --fail --location --continue-at - \
  --connect-timeout 20 --max-time 180 \
  '<data[0].url>' -o '<output>.mp4.part'
```

优先检查代理配置并短时测速；默认路径不一定经过代理，未配置代理时与直连是同一路线。当前已实测4路Range完整下载和生产下载成功：支持Range且测速有收益时，优先4路分段；不支持或没有收益时回退单连接续传。不要为了下载持续无限重试。

### Range并发下载（本轮已验证，仍需逐任务探测）

1. 查询已有task取得URL，不能创建新生成任务代替下载。用 `Range: bytes=0-0` 探测，要求HTTP 206和合法 `Content-Range`，取得总长度及ETag（若有）。不要只凭 `Accept-Ranges` 或HTTP 200判定分段可用。
2. 分为最多4个不重叠的连续区间，覆盖整个文件。每段写独立 `.part`；保存task、对象标识/ETag、总长度、起止偏移、已有长度、状态和耗时的manifest。不保存Key、临时签名URL。
3. 每段核对实际HTTP 206、精确起止/总长度、字节数和ETag；若返回整文件200或身份不一致，不得拼接。成功段保留，失败段只补剩余范围，不重下全部。当前试验脚本实现了分段首次下载，通用逐段续传仍需按此规则实现，不能声称已全面支持。
4. 同一任务返回新签名URL时仍需核对对象身份；已有下载可额外比较前64KiB。curl URL及认证通过stdin配置传入，不打印授权头或签名地址。
5. 为探测、下载、重试设统一单轮预算（例如180秒），单段超时不得超出剩余预算；并发耗时取墙钟时间，不累加各段时长。低速、失败或预算耗尽时保留数据并报告，不能无限重试。
6. 所有段验证通过后按偏移拼接到目标目录的 `.mp4.part`，验证完整长度、MP4盒结构并计算SHA256，再原子改名。用已知文件测速时还应比较整体SHA256；ETag不能泛化当作MD5。
7. 新host/网络变化重新短测。4路成功不代表越多越快，不默认扩展至8/16路；单个慢段可能决定总耗时。若回退单连接，只能从已验证连续前缀续传，不可把不连续分段当完整前缀。

### 计时与解码验证

- 保存UTC阶段时间及本地monotonic耗时：上传、创建请求、任务accepted、每次状态观察、下载开始/结束、解码校验。
- `accepted → success观察` 包含排队、服务端计算、查询间隔及本地等待，必须叫观察到完成的等待时间，不能冒称纯推理耗时。若有最后一次generating时间，可提供完成时间区间。
- 总耗时应明确起止；“上传开始→下载完成”不包含之后解码检查，也不包含之前提示词准备。提升传输不代表模型生成变快。
- 可用 PyAV 完整解码，Pillow 保存原尺寸尾帧/接触表；记录实际宽高、fps、帧数、容器时长、末帧PTS。请求4秒可能包含t=0和t=4的97帧（24fps），容器时长约4.042秒，两者不冲突。
- 技术成功、抽帧观察、无缝循环验收、用户美术验收分开报告；首尾同图、低整幅差值或容器通过都不能证明无缝循环。

下载完成后必须验证：

1. 文件存在且大于 1 KB；
2. 前 8 字节符合 MP4 `ftyp`；
3. MP4 顶层 box 长度不超过实际文件长度；
4. 如可用，使用 `ffprobe` 或播放器验证可解码；
5. 全部通过后原子改名为正式 `.mp4`；失败则保留或清理 `.part`，不得声称完成。

默认输出目录：`C:/Users/steam/Videos/doubaoVideo/`。若用户指定路径，使用用户路径。

## 报告要求

成功时报告：task ID、最终状态、真实本地文件完整路径、文件大小，以及 API 返回的 usage/cost（若有）。

失败或超时时报告：task ID、最后状态、错误 code/msg、失败阶段（上传/创建/轮询/下载/校验），绝不报告 Key，也不把服务端 `success` 当成本地视频完成。

## 安全禁令

- 仅为认证从环境变量读取 `REACH_API_KEY`；禁止打印其值或持久化；
- 不把 Key 写入 JSON、脚本、shell 历史、Unity 资产或 Git；
- 不未经确认创建收费任务；
- 不把未完整验证的 MP4 导入 Unity；
- 不对卡住的任务无限轮询；
- 不自动重新生成收费视频以替代下载失败。

## 本次实测与防错经验（N3→E3、E3→EW）

## 防错经验（来自实测，任务数据见 `memory/video-generation-case-log.md`）

- 不要把请求档位等同于实际像素尺寸（实测请求 480p，Unity 读到约 560×752）。
- 最终文件 `C:/Users/steam/Videos/doubaoVideo/N3toE3_village_supply_yard_480p_4s.mp4`，3,145,128 bytes。服务端 completion_tokens=39891，cost.spend=0.22339（按响应原值记录，不擅自推断币种）。用户已验收视频。
- 时长与分辨率同时改变时不能推导单一压缩比；新生成不是原视频转码，画面也不保证完全相同。
- 首轮12次查询仍为 generating，停止的是本地等待，不是服务端生成。后续单次查询返回success。不得将本地退出码判成服务端任务失败，也不得自动再次付费生成。
- 线路择优只能在当次测速后判断：不保证直连永远更快；curl 默认线路也不等于一定走 Windows 系统代理，需核对实际代理配置。
- 下载超时的成功关键是断点续传（保留 `.part` 并用 `--continue-at -`），而不是无限延长等待或反复重头下载。
- Range早期尝试只得到部分数据；后续4路完整文件测试已通过且SHA256一致，生产循环视频也成功一次性分段下载。按上方探测/校验流程条件性优先4路，具体实测数字见台账；仍不可保证所有任务或网络下更快。
- 续传必须确认task、对象路径、总长度等身份一致；若URL刷新，不代表内容必然相同。保留分段manifest，不能把新生成文件接到旧分段后面。
- 每轮有总截止时间（包含请求耗时及sleep），例如轮询最多120秒、单次HTTP最多20秒，下载单轮180秒；仅循环12次加每次20秒超时并不保证总等待120秒。耗尽预算就报告并停止，不自动开下一轮。
- 下载超时优先保留可续传数据；不要像第一次重试那样直接删掉全部进度。只有完整检查ftyp、顶层box边界、moov/mdat后才改名；容器检查不等于解码或美术验收。
- 官方文档提供callback_url以替代主动轮询，但需要已授权的公网HTTPS回调服务，不接受localhost/私网地址；当前无此部署，不擅自创建外部服务。官方两份文档未提供专用下载加速或刷新结果URL接口。
- 未保存完整 prompt 时不能声称逐字复用；完整 prompt、seed、规格、首尾帧路径与 task id 必须留档（不保存 Key 与临时签名 URL）。
- 上传/创建后台命令刚排队不等于服务端已接受；只有返回task_id才报告“生成任务已创建”。只上传成功也不算视频完成。
- 子进程认证推荐通过stdin配置传入curl，避免把展开后的Key放进进程命令参数；绝不输出授权头。
- 先收到服务端 `success` 再下载；下载中断后用相同 task 与 URL 续传完成，顶层 box 校验通过后才部署 Unity。
- 本次提示词视觉验收通过的核心是“短距离、单目标、首尾帧均为真实空间锚点”。不要把首尾帧写成需要crossfade或精确贴图的两张图片；要求模型重建连接两者的连续3D环境。
- 明确物体连续性比堆叠负面词更关键：写出村内现有的车、粮袋、围栏、墙体、碎石从两侧以视差掠过并退后；树林是远处原本存在、随接近而放大的环境。并明确禁止物体无中生有、消失、背景替换、瞬移、闪烁、硬切。
- “前两秒完全保持不动”会和真实前进冲突，容易原地踏步或后段跳跃。应改为开头短暂保持起始构图，然后全程稳定向前移动。镜头限制为forward dolly only，禁止转向、横移、roll、jump。
- 对跨度较大的路线，先插入中间节点/尾帧并拆段，例如村内`E3→EW`、树林`EW→J3`；单段优先4秒480p验证。首尾帧图的实际比例应一致，本项目竖屏背景用2:3，不能在请求中混入3:4。

## EW→J3 补充经验（案例见 `memory/video-generation-case-log.md`）

- 严格首尾帧适合短距离、空间差异小的路线；当尾帧要求新增道路或分岔时，模型可能强行重排景物以贴合尾帧。
- `first_frame` 与普通 `reference_image`/`reference_video`不能混用；请求前必须按接口规则选择工作流，不能向用户承诺不可执行的组合。
- 只使用首帧时，应把目标描述为“既有道路通过镜头前进和遮挡变化逐步显露”，并明确固定相机高度、近水平视线、禁止上飘；但仍需逐帧视觉验收。
- 当模型难以稳定遵守道路拓扑时，用户后期裁剪/拼接是可接受的最终制作路径（本案例成片见 `memory/video-generation-case-log.md`）。
- 任务成功和MP4容器校验只证明生成/下载完整，不证明道路运动正确；必须实际观看或逐帧抽查后才能部署。


- 任何涉及多条道路汇入/分叉的路线视频，先用Blender建立真实道路几何并固定多个2:3摄像机，再制作像素风关键帧。
- 不再把“尾帧包含道路”当作视频会自动理解道路来源的充分条件；必须提供连续中间关键帧和可见遮挡关系。
- 以上为降低风险的制作策略，不是成功保证。实际采用首尾帧和4秒480p的EW→J3仍出现侧路瞬移，用户已否决。不能把E3→EW的一次成功归因为单个关键词，也不能把所有失败归因为单一原因。
- 缓弯路段允许相机沿道路平缓实际转向；不要照搬直路的“禁止一切转向”。统一像素风关键帧后再提交，不混用灰模与像素图作为成片首尾帧。

## 官方来源

- https://reachapi.ai/docs/api-reference/video/seedance/seedance-2
- https://reachapi.ai/docs/api-reference/file/upload
