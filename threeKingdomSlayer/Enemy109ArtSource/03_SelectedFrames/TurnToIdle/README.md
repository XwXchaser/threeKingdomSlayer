# Enemy 109 TurnToIdle：24帧透明PNG交付

用户已验收源视频及24张原始帧。本次按“请继续未完成的抠图工作”授权完成本地SVL抠图；**透明结果仍待用户目检验收**。

可直接复制到Windows资源管理器的最终透明PNG目录：

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\03_SelectedFrames\TurnToIdle\keyed_ui_t79\
```

该目录只含24张960×960 RGBA透明帧，名称为 `turnToIdle<序号>_f<视频帧号>.png`。按序号自然排序，勿使用纯文件名字典序（否则10在1之前）。完整顺序见 `frame_list.csv`。

## 配方与验证

- Sprite Video Lab原有Chroma：逐帧自动取色 / T79 / softness16 / halo1 / 半透明转不透明。
- 未用Real-ESRGAN、语义重绘、全图校色或旧环替换后处理；无新收费API。
- 对7张代表帧扫描T55/70/79后采用T79；同一脚本/配方对已有Idle1的回归结果与当前`keyed_ui_t79`逐像素一致。
- 原始PNG复制24/24哈希与已验收来源一致，原视频哈希不变。输出24/24可解码，960×960 RGBA，Alpha仅0/255，半透明0，隐藏透明RGB为0。
- 主体核心掩码共3,257,182个像素次，0删除、0改色；边缘反混合有约1,075–2,295个可见像素/帧RGB变化，属去溢色处理，不声称所有可见像素原色不变。
- 非透明剪影最小左/上/右/下余量37/36/43/67px；无画布裁切、缩放、平移、镜像或逐帧钉脚。
- 代理已读白底/深底总览和枪杆、缰绳、马尾、蹄部细节对照；细部连续，未观察到明显新增绿边或主体大片缺口。这不是用户对透明版的验收。

## 原有色差的处理边界

绿底已转为透明；上游生成器造成的主体轻微RGB变化仍保留。逐帧自动取键不等于全图调色，本次没有把转身末帧换成静态Idle，也没有调整主体颜色。已在同一灰底对照末两帧与对齐Idle1；最终视觉连续性留待用户审阅，不能将“背景已去掉”写成“主体色差已修好”。

## 审阅文件的完整磁盘地址

白底总览：

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\03_SelectedFrames\TurnToIdle\review\turnToIdle_24_white.png
```

深底总览：

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\03_SelectedFrames\TurnToIdle\review\turnToIdle_24_dark.png
```

中性底快速预览（24帧，2.68秒，仅审阅时序）：

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\03_SelectedFrames\TurnToIdle\review\turnToIdle_24_neutral_preview.webp
```

白底慢放（24帧，6.24秒，仅审阅时序）：

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\03_SelectedFrames\TurnToIdle\review\turnToIdle_24_white_slow.webp
```

细部源图/白底/深底对照及末帧接Idle对照：

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\03_SelectedFrames\TurnToIdle\review\fine_parts_source_white_dark.png
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\03_SelectedFrames\TurnToIdle\review\endpoint_to_existing_idle_neutral.png
```

## 来源与状态

- `raw/`：24张已验收源帧的原字节副本，未改色，不是透明成品。
- `keyed_ui_t79/`：24张透明PNG成品候选，用于本轮验收。
- `review/`：展示用合成底总览/动画，不能作为透明Sprite源。
- `raw_manifest.json`：原始来源、帧号、顺序、来源哈希。
- `matting_report.json`：透明输出哈希、配方、逐帧Key、颜色保护与边界数据。
- `frame_list.csv`：供排序/核对的24帧清单。

尚未导入Unity、未补到1108部署空间、未创建AnimationClip、未接Cavalry视觉路由、未修改玩法或保存场景。本次只处理已验收的转回Idle24帧；4秒返航奔跑视频尚无已确认选帧清单。所有预览的缩放、重复播放与首尾停留只服务审阅，不是游戏帧时序。
