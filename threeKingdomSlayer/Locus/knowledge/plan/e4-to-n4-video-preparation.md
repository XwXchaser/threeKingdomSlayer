---
id: kd_e9789320-477d-4f8d-81e7-54f094409bbe
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# E4 → N4 视频生成准备包

## 当前状态

- 用户已验收 J3→E4 部署。本轮只准备 E4→N4，不创建视频任务、不改 Unity 绑定。
- 实际出口：E4 的 `e4_to_n4` → N4 山道救援；目前使用 RouteChoiceBlack。
- 完整解码已验收的 J3toE4.mp4：560×752、24fps、145帧，末帧PTS=6.000秒。
- 原始首帧：`C:/Users/steam/Pictures/gptGen/E4toN4_first_actual.png`。
- 尾帧候选：`C:/Users/steam/Pictures/gptGen/E4toN4_tail_night_v1.png`，1024×1536，3,007,609 bytes；gpt-image-2.5-sunburst参考图编辑生成，未获用户验收。
- 裁切首帧候选：`C:/Users/steam/Pictures/gptGen/E4toN4_first_actual_2x3.png`。原图中心裁切框(29,0,530,752)，再缩放至1024×1536；约去掉左右合计10.5%宽度。501:752与2:3有一像素取整误差。它不是原视频逐像素无损首帧，不作为默认上传图。

## 场景判断与未通过项

起点是蓝调月夜的山道战痕：中央道路向左上方爬升；左侧岩壁和火把，右侧树木及谷地远火，近处有木轮、路牌、破布。运动必须沿左上既有道路，不直冲右侧谷底。

旧N4静态图为白天山谷，只可参考开阔战斗地面的用途。不能以昼夜变化、缩放旧图或闪现替代空间行进。

v1尾帧保持夜色、岩土材质及宽阔空地，但**还不能锁定为视频last_frame**：画面正对谷地且左侧仍出现上坡小路，有保留起点岔向/向谷底展开的歧义；未充分证明已沿左上道路越过岩肩。应先请用户评审，或修订为明确处于岩肩之后、来路退到镜头后方的山腰平台。不得为迁就这张图改成直接向山谷冲下去。

## 下一版尾帧参考要求

以上一段真实尾帧作为主要空间/材质/夜色参考。镜头沿中央道路朝左上推进、爬缓坡并平缓左转，越过两处火把与左侧岩肩；抵达岩肩之后附近的宽阔山腰鞍部空地，而不是远方谷底。旧木轮、路牌、破布和近处树木已在镜头后方；左岩肩只残留于侧后边缘。道路自然展宽，中央及下方约2/3保持连续可战斗地面。远山保持合理缓慢角度变化，右侧谷地可侧向远望，不占据主行进轴。单一向前出口、无新分叉。无人、无手臂武器、无文字旗号、无UI，无城门桥梁或突然新增营寨。保持固定焦距、近水平视线，不鸟瞰。

## 视频Prompt草案（尾帧空间验收后使用）

A single continuous six-second traveling shot through one physically connected mountain environment at blue moonlit night. Start from the exact approved E4 frame. The camera physically travels FORWARD along the existing central dirt path toward the UPPER LEFT of the starting image, climbing the gentle slope and making a gradual LEFT turn following the path around the existing left rock shoulder. Do not travel straight down into the valley on the right. Move beyond the starting location and arrive at a broad, nearby mountain-saddle clearing behind that shoulder, suitable for the N4 rescue battlefield.

0–2 seconds: begin genuine forward translation immediately, without a static opening hold. The foreground wheel and sign on the left and torn cloth and tree on the right grow with approach and pass out through their respective side edges, remaining behind the camera. Ground stones flow downward with perspective-dependent speed.
2–4 seconds: continue uphill along the same path and pass the existing roadside torches. Rotate gently left only as required by the road, while continuing to translate forward. The near rock shoulder slides laterally out of the sightline; the clearing beyond it is gradually revealed through changing partial occlusion. Keep part of the road and distant environment visible throughout. No full-screen occlusion used to hide a cut.
4–6 seconds: cross the mouth of the clearing and travel into its broad open ground, then gently decelerate at the new location. The road physically widens; nearby rocks and trees recede to the outer edges. Preserve blue night lighting, restrained warm torchlight, rocky soil and the mountain landscape. Leave the central and lower two-thirds empty for combat. No characters, visible body, hands, weapons, text or UI.

The two images are spatial anchors in the same world, not images to blend. Fixed camera height and focal length; real forward translation, depth-dependent parallax, foreground passage and slow distant-mountain displacement. No static camera, no in-place bobbing, no digital zoom as movement, no background scrolling, no cut, flash, dissolve, morph, teleport, object pop-in, instant clearing appearance, daylight change or late sudden replacement by the destination image. Existing objects remain in the world when they pass behind the camera. Target features must be revealed progressively by actual travel, never generated from empty space.

## 参数与工作流

1. 默认6秒，seedance-2-fast，480p低成本验证，无音频、无水印，aspect_ratio=adaptive。
2. 优先保留原始560×752首帧；尾帧应按同一比例生成/扩图并验收。2:3裁切首帧仅是可选候选，需接受接缝裁切后才能使用；不得直接拉伸原始图。
3. 尾帧须先通过“岩肩之后的新位置”检查，必要时生成中途越过岩肩的预演关键帧确认空间关系。它是制作参考，不自动新增逻辑节点。
4. 用户确认尾帧和比例后，使用first_frame + last_frame，不能混普通reference_image/video/audio。若选普通双参考，必须承认并非API级首帧锁定。
5. 视频收费创建前复述模型/6秒/480p/adaptive/无音频/两张确认图及输出路径并取得确认。建议输出 `C:/Users/steam/Videos/doubaoVideo/E4toN4_v1_480p_6s.mp4`。
6. 上传、创建、查询、下载独立执行；保存task_id及去敏请求参数。最多12次/120秒轮询；超时报告而非无限等待或重复收费生成。下载到.part，校验容器、长度和完整解码后再改名。
7. 每0.5秒抽帧并完整播放，检查至少两个近景物体从接近到掠过消失的连续轨迹；中景展宽循序发生；远山不能同步平移。0–2/2–4/4–6秒均应有真实推进，禁止最后一秒才换到目标。
8. 任何硬切、瞬移、突然展宽、原地抖动、纯缩放均判失败；不以提示词中写了禁止词作为通过证据。
9. 本次 E4→N4 v1 实测：原Prompt和两张普通 `reference_image` 保持不变，仅更换固定 seed 重新抽卡；v1整体移动正确，但末段停在高处台阶，说明随机落点/地形解释仍会改变终点。v2使用相同Prompt、参考图、6秒、480p、adaptive、无音频、无水印，仅seed=482917，用户验收通过。两张图作为起点/目标环境参考，不强制对齐last frame；必须强调全程实际向前、上坡、左转、深度视差和目标场景渐进显露，同时禁止静止平移、纯缩放、末帧闪现、硬切和最后一秒替换。遇到局部落点问题优先原样换seed重抽，不先破坏已正确的运动关键词。
10. 视频获用户验收后才部署 E4 出口。N4当前battleBackground=N4_Battle且为存档点，另有官道路入边：不能只清空背景来保尾帧而忽略直接恢复/其他入边。后续需独立确定新静态背景与实际尾帧衔接方案，本轮不改配置。

## 关键词

physical forward translation / uphill upper-left path / gradual left turn / pass existing torches / round rock shoulder / reveal through partial occlusion / depth-dependent parallax / foreground passes behind camera / nearby mountain saddle / broad empty combat ground / fixed focal length / continuous moonlit night

强禁：instant destination reveal / hard cut / crossfade / morph / teleport / pop-in / static shot / zoom-only / in-place walking / full-frame masking / day-night switch。
