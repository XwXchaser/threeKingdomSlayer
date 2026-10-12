# TurnToIdle端点候选复核 v2（实际透明图证据）

**结论摘要**：简单把首帧从f001移到f002、末帧从f145移到f144，不能解决色差。视频首尾背景中位跨度仍约为`G +14 / B -3`；f144与现有Idle参考的背景差还比f145多1级G。f140/f141能避开f142后的视频内部变色段，并与已有f138背景/主体更近，但它们离现有Idle主体更远，因此只是端点取舍，不是全链路修复。候选均待用户选择，不自动替换已验收序列或已验收透明帧。

## 候选透明帧

这些都是原视频真实帧，按已验收SVL配方 `per-frame auto key / T79 / softness16 / halo1 / 半透明转不透明` 抠图。没有重绘、改色、缩放或平移。

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\03_SelectedFrames\TurnToIdle\endpoint_review_v2\rgba_candidates\turnToIdle_f002_T79.png
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\03_SelectedFrames\TurnToIdle\endpoint_review_v2\rgba_candidates\turnToIdle_f140_T79.png
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\03_SelectedFrames\TurnToIdle\endpoint_review_v2\rgba_candidates\turnToIdle_f141_T79.png
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\03_SelectedFrames\TurnToIdle\endpoint_review_v2\rgba_candidates\turnToIdle_f144_T79.png
```

逐帧透明验收值在 `verification_report.json`；核心主体颜色没有被改变，Alpha为二值。f002只是首帧后1/24秒，不改变动作头部语义；f144只比f145早1/24秒。

## 对照与预览

下列文件在同一960源画布、同一灰底、同一显示尺度下比较；复合背景来自实际RGBA alpha合成，不是原始绿底RGB帧：

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\03_SelectedFrames\TurnToIdle\endpoint_review_v2\review\head_comparison_neutral.png
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\03_SelectedFrames\TurnToIdle\endpoint_review_v2\review\tail_options_neutral.png
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\03_SelectedFrames\TurnToIdle\endpoint_review_v2\review\direct_f144_f145_idle_neutral.png
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\03_SelectedFrames\TurnToIdle\endpoint_review_v2\review\candidate_to_idle_comparison_neutral.png
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\03_SelectedFrames\TurnToIdle\endpoint_review_v2\review\material_closeups_neutral.png
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\03_SelectedFrames\TurnToIdle\endpoint_review_v2\review\tail_options_parallel.webp
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\03_SelectedFrames\TurnToIdle\endpoint_review_v2\review\head_f001_vs_f002_parallel.webp
```

WebP仅用于循环审阅，时长不是生产时序；端点切换预览不代表已经接入动画。

## 实测背景中位数

在同一视频解码输出的四角空背景区域测得：

| 帧 | 作用 | 空背景RGB中位数 |
|---|---|---|
| f001 | 当前序列首帧 | `(0,146,60)` |
| f002 | 首帧+1候选 | `(0,145,60)` |
| f138 | 当前第23张 | `(0,153,59)` |
| f140 | 末段稳定候选 | `(0,153,58)` |
| f141 | 末段稳定候选 | `(0,153,58)` |
| f142 | 末段漂移开始 | `(0,157,57)` |
| f144 | 当前末帧-1候选 | `(0,159,57)` |
| f145 | 当前序列末帧 | `(0,160,57)` |

视频起止输入差异原本存在：真实奔跑视频首尾解码帧约`(0,152,63)`，Idle尾图约`(0,168,60)`。使用现有视频内帧不能改变Seedance已输出的背景时序，也不能恢复静态Idle不同的底色。

## 主体接续的取舍

- f140/f141在视觉上与当前f138非常接近；局部配准NCC约0.99，适合避免“f138之后已经站稳，选帧却继续过渡/重复摆动”的问题。
- f144/f145处于末段背景色发生变化之后，配准甲胄/红布/马胸仍与f138略有差别；但它们与现有Idle材质比f140/f141更接近一些。
- 从f140/f141切到现有Idle虽有相同正面站姿方向，材质差距更明显；从f144/f145切Idle也仍有上游色差。候选不是对主体色差的修复。
- f002与f001在姿态、包围框几乎一致，背景中位反而向内不同色域移动约1级；从f002开始还跳过24fps序列的首帧/前约42ms，并没有显著改变后续最终色差。

## 保护状态与限制

- 当前已验收24张RGB序列与已验收24张T79透明版保持原哈希，未替换、未重排、未导入Unity。
- 当前实际视频哈希保持；仅新增四张候选RGBA和诊断文件。
- 该比较用于回答端点选择，不能把RGB角落统计等同完整材质配色验收；上游视频色彩变化仍需单独决定处理路线。
- v1目录里两份早期WebP曾错误地从不透明源视频RGB构建；它们**不作为已抠图端点证据，请勿引用**。v2预览已核验其每帧由RGBA经灰底合成，详见 `verification_report.json`。
- 还没有用户批准替换现有24帧序列。