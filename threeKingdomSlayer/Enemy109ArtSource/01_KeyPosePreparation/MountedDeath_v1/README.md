# 109骑乘死亡：关键帧生成准备包 v1（旧方向，仅供追溯）

> **2026-10-15 被新方向取代。** 用户不再让马死亡；改为“①骑手受击→脱鞍→摔落接地一次连续视频，马保持站立；②无骑手马向画面左侧跑离，游戏控制渐隐”。只在①的交接帧切分视觉，不需重做整套正常骑乘。本目录D1～D3及旧提示词／请求记录保留原样，均不作新动作首尾依据；之前“只重做D3”和三段视频方案已撤回。**本目录不是当前待生成包，也不再等待旧三帧整体验收。** 新方向素材与生成未开始，完整接续见 `Locus/knowledge/plan/enemy109-rider-fall-mount-escape-handoff-20261015.md`。以下是旧方案的生产与技术核验历史，不覆盖顶部更正。

## 旧方案状态与边界

- 用户要求：**准备生成关键帧动作**。
- 已准备：身份上传图、装备核对图、审阅参考板、完整中英文提示词、三次独立图片编辑规格。
- D1、D2、D3已按批准版本分别生成并保存，共3次收费／远程请求；没有自动重试。
- 旧候选没有整体美术验收通过记录；用户指出D3无法衔接D2，随后改为马存活逃跑。未制作视频、未抠图、未导入Unity、未修改代码／场景。素材生产流程未进入Play Mode，后续用户自行切换模式不属于本次自检。
- 本包仅定义**正面骑乘死亡**。侧身攻击或背向返航入口不能直接视为已覆盖；已实际落马的109不在本素材范围。

## 可直接粘贴到资源管理器的路径

准备包根目录：

`H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\01_KeyPosePreparation\MountedDeath_v1`

提示词：

- 中文完整包：`H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\01_KeyPosePreparation\MountedDeath_v1\prompt.zh.txt`
- 英文完整包：`H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\01_KeyPosePreparation\MountedDeath_v1\prompt.en.txt`
- 请求规格：`H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\01_KeyPosePreparation\MountedDeath_v1\request-spec.json`

旧候选素材目录（仅追溯，不再作为当前制作候选）：

`H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\01_KeyPosePreparation\MountedDeath_v1\generated`

- D1：`enemy109_mounted_death_d1_knee_collapse_v1.png`
- D2：`enemy109_mounted_death_d2_shoulder_contact_v1.png`
- D3：`enemy109_mounted_death_d3_settled_hold_v1.png`

审阅／追溯文件目录：

`H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\01_KeyPosePreparation\MountedDeath_v1\review`

其中包含三张候选拼接审阅板和技术核验JSON，不是Unity运行时素材。

## 参考职责

### 唯一计划上传图

`upload/charging1_identity_green_015962_1024.png`

来源为已验收Charging1/f036的既有1024生产底图。严格锁定109身份、马匹、甲胄、长枪、鞍具、配色、材质、光照、解剖尺寸及略俯视正面镜头，不锁定原冲锋关节姿态。D1／D2／D3均使用这一张身份图分别编辑，不用上一张新生成图递推，因此不会未经审阅自动变更参考职责。

复制SHA256：`cb2592e25fb00fba0598ed41d39539724aa77316ef622ec6aa9012ee46532998`。

### 仅供审阅与追溯，不上传

- `source/charging1_f036_960_rgba.png`：原透明身份来源。
- `source/mountedIdle1_f001_960_rgba.png`：核对马面甲、鞍具、马镫、长枪及四肢，不负责新姿势。
- `review/reference_context.png`：现有两张参考的拼接说明板，**不是死亡关键帧、不是生成候选，也不是上传分镜**。板上的缩小图仅供审阅，不定义生成尺寸或色彩。

## 三张动作及验收重点

| 编号 | 姿态职责 | 必须读得出的内容 | 不应出现 |
|---|---|---|---|
| D1 | 前腿跪塌、失去支撑 | 马前膝跪地、胸颈下沉；后腿短撑；骑手前伏并向右失衡，腰胯尚靠鞍；右手仍持枪 | 可恢复急停、重新坐稳、已完全侧卧 |
| D2 | 向右侧倒、马肩触地 | 马肩先接地，后躯仍稍高；骑手慢半拍向近侧空地滑落；靴子脱离马镫；枪开始松开 | 悬空翻滚、骑手压在马腹下、完全静止尾姿 |
| D3 | 人马倒地、最终收稳 | 人马均落地，骑手近侧可辨；长枪落在前外侧；四肢、红缨与马尾收稳 | 扶缰、抬头、准备起身、烘焙淡出 |

倒向统一为**画面右侧**；骑手离鞍只为死亡表现，不代表玩法上的“落马继续战斗”。允许自然遮挡，但身份轮廓和人与马的关系必须可读。长枪始终一根、直杆、不穿体；D1仍握、D2松开、D3落定。

## 完整提示词的使用规则

两个提示词文件包含相同的共同约束、三段独立动作和共同排除项。每次实际提交必须组装：

**共同提示词 + 当次唯一动作段 + 共同排除项**。

不把三个动作一起交给一条请求，不生成组图。计划提交英文，中文用于同步审阅。排除项已写入正文，不能额外杜撰接口`negative_prompt`字段。

## 计划生成参数

- API：`POST https://api.muskapis.com/v1/images/edits`，multipart单参考编辑。
- 模型：`gpt-image-2.5-sunburst`。
- 尺寸：`1024x1024`；质量：`max`；参考忠实度：`high`。
- 格式：PNG；每次1张；完整批准三张时共3次请求／3张图片。
- 不透明绿幕：目标`RGB(0,159,62)`／`#009F3E`，不请求原生Alpha；省略API的`background`参数。
- 图片没有时长参数；后续视频时长不在本次范围。
- 不自动付费重试，不自动修改提示词或参考后再调用。

## 本地核验

- 三张生成图均为1024×1024 RGB PNG，Alpha等效全不透明，尺寸核验通过。
- D1、D2、D3各自对应一次HTTP 200请求，生成记录已保存；总计3次，自动重试0次。
- 绿幕不是像素级完全统一的RGB(0,159,62)，四角存在通道偏差；当前未抠图或改底色，后续抠图须独立标定并验收，不自动认定必须重涂背景。
- D1视觉上读得出前腿跪塌、骑手前伏并仍持枪。
- D2视觉上读得出马肩触地及骑手近侧落下；仍需验收解剖、轮廓和接地。
- D3马已侧卧、长枪已落在外侧，但骑手仍偏坐靠／低头姿势，未完全达到失力躺倒的尾帧要求。
- D2→D3的马身、后躯和尾巴朝向有明显变化；连续性和尺寸未验证，不应直接视为已验收动画序列。
- `review/generated_candidates_contact_sheet.png`初版预览被裁切，现已修正为三张完整原图等比缩小；原候选PNG字节未改。
- 三张来源复制均与原文件SHA256一致。
- 上传图1024×1024 RGB；透明来源960×960 RGBA，Alpha仅0／255。
- 既有底图只在四边各补32像素纯绿；未缩放、裁切或改主体颜色。
- 原图可见192,075个主体像素全部保留，改色像素0。
- 参考落脚高度约y=929；这是约束提示而非落地绑定承诺，生成后仍需检查接地、边距、体积与连续性。
- `review/reference_context.png`已回读，确认只包含既有参考；完整提示词和请求规格已回读并检查。

## 后续授权边界

旧三张已按用户“发起生成”授权完成，共3次，不能据此追加调用。新方向不再推进D1→D2→D3倒马序列；需要另做“骑手落地＋马站立”的交接姿态与两段视频准备包，经新双语提示词、参考职责和参数审阅批准后才能生成。本轮仅更新交接文档，不自动准备或执行新素材任务。

生成图的身份一致、画风、方向、接地、轮廓完整度和D1→D2→D3连续性需另行验收，不因本地文件核验而视为美术通过。将来部署还需替换骑乘状态下通用死亡弹起旋转，保持即时击杀结算、死亡完成和对象池单次回收；本次没有实施这项工程改动。
