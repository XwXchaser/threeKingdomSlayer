---
id: kd_ff9d3453-e475-47f8-9c47-5d1f9284e701
injectMode: inherit
aiEditMode: inherit
skillEnabled: true
skillSurface: command
---

# ReachAPI Seedance 2 视频生成

使用 ReachAPI 的 Seedance 2 异步视频接口生成视频。默认模型为 `seedance-2-fast`；本 Skill 支持纯文本、参考图、参考视频和参考音频工作流。

## 已验证的接入状态（2026-09-08）

当前工作站已完成 ReachAPI 接入验证，可在其他对话直接按本 Skill 执行：

- `REACH_API_KEY` 已通过 Windows 用户环境变量配置；调用时仅检查其存在，绝不输出密钥。
- 已成功上传项目参考图 `Assets/Sprites/changbanpo/Backgrounds/changbanpo_EV_village_approach_v3.png`：服务返回 `code=200`、`file_kind=image`、`mime_type=image/png`。
- 已成功创建并轮询 `seedance-2-fast` 图生视频任务，状态从 `queued` / `generating` 到 `success`；服务端返回结果视频 URL、token 用量和费用。
- 后续成功视频的默认本地下载目录为 `C:/Users/steam/Videos/doubaoVideo/`；该目录不属于 Unity 项目或 Git 工作树。
- 生成任务为异步任务：创建后使用返回的 `task_id` 每 5–10 秒轮询 `GET /v1/tasks/{task_id}`，只在 `success` 后下载 `data[].url`。

## 安全与前提

- **绝不**把 API Key 写入项目文件、Git、命令历史、日志或回复正文。
- 仅从环境变量 `REACH_API_KEY` 读取凭据；若不存在，先要求用户在其终端安全设置。示例（Git Bash）：`export REACH_API_KEY='...'`。
- 用户在对话中提供过明文 Key；提醒其立即在 ReachAPI 控制台撤销并重新生成该 Key，然后只在环境变量中保存新 Key。
- 本服务会产生费用；在发起请求前，向用户复述模型、时长、分辨率、比例以及参考素材，并取得其对该次生成的确认。
- 上传文件仅为临时 URL；输出 URL 也应视服务端临时资源，及时下载到用户指定位置。

## 默认参数

| 参数 | 默认值 | 限制 |
|---|---:|---|
| 模型 | `seedance-2-fast` | 不支持 `1080p` |
| 时长 | `6` 秒 | `4`–`15`，或 `-1` 自动 |
| 分辨率 | `720p` | `480p` / `720p` |
| 比例 | `16:9` | `21:9`、`16:9`、`4:3`、`1:1`、`3:4`、`9:16`、`adaptive` |
| 随机种子 | `-1` | `-1`–`4294967295` |
| 音频 | `false` | 需要时才启用 |

用户明确要求时，可改用 `seedance-2`，该模型额外支持 `1080p`。

## 工作流

### 1. 收集生成规格

确认：提示词、时长、分辨率、比例、是否生成音频、是否要最后一帧，以及每个本地参考文件及其用途。

将提示词写成可执行镜头说明：主体与外观、动作、场景、镜头运动/构图、光线、时间节奏、风格。不要声称模型支持负面提示词；该字段已不被 API 支持。

### 2. 上传本地参考文件（如有）

对每一个本地图、音频或视频单独上传。仅接受单文件、最大 50 MiB，且类型必须是：

- 图片：PNG、JPEG/JPG、WebP、GIF
- 音频：MP3、WAV、M4A/AAC、OGG、WebM
- 视频：MP4、WebM、MOV、MKV

```bash
curl -sS -X POST 'https://file.reachapi.ai/file/uploads' \
  -H "Authorization: Bearer $REACH_API_KEY" \
  -F 'file=@/absolute/path/to/reference.png;type=image/png'
```

从成功 JSON 响应中提取 `data.url`，并在创建视频任务时使用该临时 HTTPS URL。已有目标模型可访问的公开 HTTPS URL 时不必上传。

### 3. 组装 `input.content`

`input.content` 必须是非空数组。文字使用：

```json
{ "type": "text", "text": "<prompt>" }
```

参考素材使用对应类型、角色和 URL：

```json
{ "type": "image_url", "role": "reference_image", "image_url": { "url": "<uploaded-or-public-url>" } }
{ "type": "image_url", "role": "first_frame", "image_url": { "url": "<uploaded-or-public-url>" } }
{ "type": "image_url", "role": "last_frame", "image_url": { "url": "<uploaded-or-public-url>" } }
{ "type": "video_url", "role": "reference_video", "video_url": { "url": "<uploaded-or-public-url>" } }
{ "type": "audio_url", "role": "reference_audio", "audio_url": { "url": "<uploaded-or-public-url>" } }
```

约束：最多 9 张图、3 个视频、3 段音频；音频不能单独使用，且文字加音频必须至少再含一个参考图片或视频；`first_frame`/`last_frame` 不能与 `reference_image`、`reference_video`、`reference_audio` 混用。

### 4. 创建任务

先将请求 JSON 保存至安全的临时文件（不要包含密钥）。纯文本示例：

```bash
cat > /tmp/seedance-request.json <<'JSON'
{
  "model": "seedance-2-fast",
  "input": {
    "content": [
      { "type": "text", "text": "A cinematic low-angle shot of a lone warrior walking through a misty bamboo forest at dawn; wind moves the cloak and bamboo leaves; slow forward dolly; detailed stylized game cinematic." }
    ],
    "duration_seconds": 6,
    "resolution": "720p",
    "aspect_ratio": "16:9",
    "generate_audio": false,
    "seed": -1,
    "watermark": false
  }
}
JSON

curl -sS -X POST 'https://direct.reachapi.ai/v1/vids/create' \
  -H "Authorization: Bearer $REACH_API_KEY" \
  -H 'Content-Type: application/json' \
  --data-binary @/tmp/seedance-request.json
```

成功后记录返回的 `task_id`。不要把未文档化的旧字段（如 `input.prompt`、`negative_prompt`、`image_urls`）发送给 API。创建任务会产生费用；必须先得到用户对本次模型、时长、分辨率、比例与参考素材的确认。

### 5. 轮询并下载结果

轮询任务直至终态；避免过于频繁，建议 5–10 秒一次：

```bash
curl -sS "https://direct.reachapi.ai/v1/tasks/<task_id>" \
  -H "Authorization: Bearer $REACH_API_KEY"
```

当 `status` 为 `success` 时，从 `data[].url` 获取结果视频 URL，并下载到默认本地输出目录 `C:/Users/steam/Videos/doubaoVideo/`（除非用户为本次任务明确指定其他目录）：

```bash
mkdir -p 'C:/Users/steam/Videos/doubaoVideo'
curl -L '<data[0].url>' -o 'C:/Users/steam/Videos/doubaoVideo/<descriptive-name>.mp4'
```

报告任务 ID、最终状态、输出文件路径、`usage` 与 `cost`（如果响应提供）。如果状态失败，完整报告 `code` 和 `msg`，但不得泄露授权头或密钥。

## 最小参考图请求

将上传所得 URL 填入 `reference_image`：

```json
{
  "model": "seedance-2-fast",
  "input": {
    "content": [
      { "type": "text", "text": "Animate the supplied character reference into a confident idle stance, subtle breathing and cloth motion, clean studio lighting, locked medium shot." },
      { "type": "image_url", "role": "reference_image", "image_url": { "url": "<UPLOAD_DATA_URL>" } }
    ],
    "duration_seconds": 6,
    "resolution": "720p",
    "aspect_ratio": "16:9",
    "seed": -1
  }
}
```

## 来源

- Seedance 2 API：<https://reachapi.ai/docs/api-reference/video/seedance/seedance-2>
- 文件上传 API：<https://reachapi.ai/docs/api-reference/file/upload>
