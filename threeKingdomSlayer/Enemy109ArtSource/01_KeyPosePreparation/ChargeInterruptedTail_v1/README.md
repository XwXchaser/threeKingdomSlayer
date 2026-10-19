# Enemy109 K2：马急停、骑手措不及防的关键尾帧（用户已验收）

用户说明上一张生成图是**K1命中关键帧**，后续还需马急停/骑手失去平衡的K2关键尾帧，再衔接转身返航。这里的“命中”指骑兵受到玩家攻击，不是骑兵刺击成功。用户确认了K1的阶段职责，未明确宣称锚点、主体色彩或Unity部署验收通过；原K1保持未改。

本轮完成K2姿态提案、唯一输入副本、参考对照与完整中英文生成词/规格。用户随后明确“发起生成”，K2已提交1次并保存原图；最新反馈“已验收，思考冲锋被打断的视频内容”。**K2关键姿态美术已验收，未抠图、视频、Unity修改或Play自检。** 本次仅分析视频内容；原有返航动画复用，不重生，验收不等于新的视频生成授权。

## 准备资料目录

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\01_KeyPosePreparation\ChargeInterruptedTail_v1
```

- `prompt.zh.txt` / `prompt.en.txt`：完整双语词，排除项并入正文。
- `request-spec.json`：已提交参数、参考职责、K1→K2逻辑、版本化输出与最新验收边界。
- `reference_manifest.json`：已有参考的哈希和逐字节副本证据。

## 唯一实际上传图：K1命中图的原始字节副本

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\01_KeyPosePreparation\ChargeInterruptedTail_v1\upload\k1_impact_original_1024_rgb.png
```

源图：

```text
C:\Users\Administrator\Pictures\gptGen\enemy109_charge_interrupted_keypose_v1.png
```

副本1024×1024RGB，SHA-256 `eed75c1aefabcd3d26699f1bcafc5398b9945d0ffc2d9a7d0a0e318fdbb8b46a`。没有缩放、裁切、平移、抠图、换底或调色。用K1锁定同源身份、材质、配色、尺度和镜头，只编辑局部姿势，不回退使用Charging作主图。

## 姿态提案与分段

1. **K1受击命中（已有图）**：骑手受力回仰、下颌上抬，马前膝屈、前蹄提起。
2. **K2急停尾帧（已生成、用户已验收）**：马前蹄已经落地承重，前胸下降、前膝轻屈，后腿支撑稳定。骑手因惯性向马颈前扑，肩线和头盔降低、下颌收向胸口，肘弯缓冲；骨盆仍坐鞍、靴在镫内，左手保持缰绳、右手保持单根直枪在身体外侧，仍正面面向玩家。尾帧是短暂稳住的慌乱姿态，可以从这里恢复并转身，尚未回到镇定Idle。
3. **K2→转身返航（后续准备）**：从前扑姿态恢复重心，收枪转马，接既有背向返航入口。当前K2不提前转到侧面/背面，不直接套用成功StrikeToRetreat的侧身头帧；以后可借鉴其转向路径，具体连续动作另审阅。

正面投影中使用“肩线降低、头盔前点、胸甲靠近马鬃但留空隙、肘屈、前蹄接地”的读点，避免模糊的“措不及防”被画成又一次后仰、主动冲刺或滑稽表情。目标为前扑惯性，不是第二次攻击、落马、击飞或整组后退。

## 坐标与色彩边界

- K2保持K1的人马尺度、鞍部/马躯阵位，蹄部支撑接近K1当前源坐标y≈850。它只约束K1→K2素材连续性，不是新Unity落地规范。K1与原Charging底边约78px差异继续留为后续映射核对，不用逐帧放缩或自动拉回底线掩盖。
- 原K1背景不是严格统一RGB；本轮保留原RGB作为编辑参考，Prompt目标为纯绿(0,159,62)。当前是单图关键姿态编辑，不伪称已统一所有输入底色；后续视频参考若采用绿幕，须按其流程另做可信Alpha小样、同底副本并验收。
- 主体配色、背景平整、像素尺度和锚点分别检查；`input_fidelity=high`不是像素坐标/颜色硬保证。

## 已批准并实际提交的生成规格

| 参数 | 取值 |
|---|---|
| 接口 | `/v1/images/edits`，单图编辑 |
| 模型 | `gpt-image-2.5-sunburst` |
| 质量 / 参考保真 | `max` / `high` |
| 尺寸 / 数量 | 1024×1024 / 1张 |
| 格式 / 背景 | PNG，不请求Alpha；正文要求RGB(0,159,62)绿幕 |
| 上传图 | 只上传K1原始字节副本 |
| 排除项 | 在完整英文词中，无独立negative_prompt字段 |
| 重试 | 不自动重发收费请求 |

**实际生成产物：**

```text
C:\Users\Administrator\Pictures\gptGen\enemy109_charge_interrupted_brake_tail_v1.png
```

请求/脱敏响应已保存同目录同名`.request.json`/`.response.json`，工程副本在本目录`generated/`，原图与副本字节一致。K2本次获用户“发起生成”授权并只提交1次；不自动重试，不自动进入视频或部署。

## 仅供审阅与追溯

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\01_KeyPosePreparation\ChargeInterruptedTail_v1\review
```

- `k1_k2_k3_action_plan.png`：三拍动作示意，**中间只有文字方案，未画K2成品**。
- `k1_charge_retreat_reference_context.png`：K1命中图、Charging3马腿参考、已有返航首帧；后两者只供审阅，不上传K2生图。
- `source/`有K1原始副本、Charging3透明原图和返航实际f001 RGB副本；原来源哈希均保持。

## K2原验收重点与后续视频边界

- K2明显区别于K1：马前蹄落地、骑手由回仰变为前扑，读感是马停得比骑手快。
- 人仍坐鞍，缰与枪未松开，前扑幅度适合恢复转身，没有主动攻击或落马语义。
- 四蹄/两靴/双手与单根直枪完整，关节解剖和遮挡可信，64px完整轮廓安全带。
- 同镜头、尺度、材质和鞍部阵位，配色/底色/锚点独立核对，不擅自改变K1原图。
- K2获验收后已完成视频动作内容分析，详见恢复入口`Locus/knowledge/plan/enemy109-charge-interrupted-keypose-preparation.md`。尚未准备视频上传副本或锁定生成参数/帧数；不修改0.2/0.22秒玩法参数。

## 实际生成与回读（2026-10-13）

- K2原图：`C:\Users\Administrator\Pictures\gptGen\enemy109_charge_interrupted_brake_tail_v1.png`；工程副本：`generated/enemy109_charge_interrupted_brake_tail_v1.png`。1024×1024 RGB/Color Type2、1,094,988字节，SHA-256 `a8af5e01cfae6a606388a527d635a5fde2f69eaa6d08b4c2a8019586761da703`，原图/副本一致。
- POST1次、HTTP200、121.297秒、返回1张；input2416/output7024/total9440，cost未返回；没有自动重试。请求/响应/production_record/runtime记录已保存。
- 生成时的回读观察：骑手由K1回仰变为明显前扑、头盔降低，仍坐鞍，缰绳和单枪保持；四个脚部支撑轮廓可见，K2比K1更接近稳定尾态。无地面线，精确承重未由技术检查证明；后续用户已验收该关键姿态，保留上述观察但不把它扩展为像素对齐或部署验证。
- `review/k1_k2_generated_comparison.png`与`review/k1_k2_hoof_support_detail.png`为原图缩小/局部审阅，不是生产输出。K1、K2均未改色、裁切、缩放、抠图或Unity导入；K2当前`user_art_verdict=accepted`。
- 验收仅针对K2关键姿态，不自动升级K1锚点/配色、视频上传副本、视频生产或Unity部署。内容提案和恢复入口见`Locus/knowledge/plan/enemy109-charge-interrupted-keypose-preparation.md`的“视频内容方案”章节；尚未提交新收费请求。
