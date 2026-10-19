# Enemy109 冲锋被打断：峰值关键帧 v1（已生成，待用户验收）

## 当前状态与范围

用户纠正下一段目标：**背向返航视频已经制作并验收，不需要重新制作；当前要补的是109骑兵冲锋被打断的动画。** 最初完成“准备制作关键打断帧参考图”的参考副本、构图提案、完整中英文Prompt、负面约束及单图请求规格。随后用户明确“发起生成”，已按批准内容只提交一次单图编辑，HTTP200返回1张1024×1024 RGB PNG，原件和工程副本均保存并回读。**关键姿态待用户验收，未生成视频、抠图、Unity接入或运行自检。**

已确认的上一轮StrikeToRetreat部署保留。本次关键图已生成并附图交付；随后用户将它定位为K1“命中关键帧”，要求另补马急停/骑手措不及防的K2关键尾帧，再衔接转身返航。K1不是急停动作的终态；K2准备在 `Enemy109ArtSource/01_KeyPosePreparation/ChargeInterruptedTail_v1/`，未生成。阶段定位确认不等于锚点、色彩或部署验收，后续生产仍须批准。

## 准备目录

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\01_KeyPosePreparation\ChargeInterrupted_v1
```

- `prompt.zh.txt`：完整中文生成词，含排除项。
- `prompt.en.txt`：完整英文生成词，含排除项；批准后提交此版本。
- `request-spec.json`：拟用参数、唯一上传图、参考职责、输出地址及授权边界。
- `reference_manifest.json`：来源、SHA-256、画布映射、源像素保护和未生成状态。

## 唯一拟上传图（已有冲锋身份参考，不是新打断图）

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\01_KeyPosePreparation\ChargeInterrupted_v1\upload\charging1_identity_green_015962_1024.png
```

- 源为现行 `Enemy109ArtSource/03_SelectedFrames/Charging/keyed_ui_t79/charging1_f036.png`。原960×960 RGBA每侧补32像素到1024×1024，用可信0/255 Alpha合成纯底RGB(0,159,62)。未裁切、缩放、翻转或重绘角色；鞍部/蹄底坐标只加固定32像素偏移。
- 192,075个可见主体像素逐像素RGB保持，原件/副本哈希回读一致。四周32像素环为精确同色绿底；参考剪影留白L/T/R/B=145/189/294/95px。输出生成图仍需独立验证，不能以参考图检查代替产物验收。
- 主参考负责骑手、马、铠甲、马具、长枪、配色、尺度和正面略俯视镜头，生成器仅改变局部动作以表现打断峰值。无需把旧101/105素材当形象基准。

## 仅供查看和追溯（不上传）

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\01_KeyPosePreparation\ChargeInterrupted_v1\review
```

- `charging_identity_anatomy_equipment.png`：Charging1主参考、Charging3马腿解剖、MountedIdle1装备对照。它们全部为已有姿势，**不是生成的打断候选**。
- `charging1_primary_reference_neutral_1024.png`：主参考中性底回读。
- `charge_and_existing_retreat_context.png`：已有正面冲锋/已验收背向返航首帧的流程对照。返航图不上传本次单图请求，防止模型提前转背面。
- `source/`保存三张960原RGBA的字节副本，以及已验收返航视频真实f001的原RGB字节副本。原来源未覆盖。

## 关键姿态提案

制作**重击截停冲锋的峰值瞬间**，不是成功刺击、返航末帧、普通Idle或落马：

1. 正面略俯视保持：肩线上提收紧、颈部缩短、胸腹通过腰胯回仰、下颌稍扬；不以左右倒斜或整体放大伪造后仰。
2. 骑手臀部留在鞍上、膝夹马身、双脚在镫内。解剖右手（画面左）继续握单根直枪，右肘回收；解剖左手（画面右）收缰，缰绳连续。
3. 马低幅刹停，后腿承重、前膝屈起、前蹄小幅离地但完整可见。马头稍收、耳向后，红缨小幅扬起。禁止高人立、全身跳跃、击飞、倒地或落马。
4. 枪尖停在画面左前下方、枪杆在身体外侧，不击中目标、不换手、不穿身、不掉枪。这个姿态供后续“冲锋→打断峰值→收枪转向→已有背向返航”分段制作，当前单图不直接承担整段动画。

## 已批准并实际提交的生成参数

- GPT Image `/v1/images/edits` 单图编辑，`gpt-image-2.5-sunburst`。
- `quality=max`、`input_fidelity=high`、`size=1024x1024`、PNG、n=1。
- 不请求Alpha，不传API `background=transparent`；正文要求均匀RGB(0,159,62)/#009F3E绿幕。模型输出实际底色和主体材质分别检查，不自动视为锁色保证。
- 不使用独立negative_prompt字段，完整排除项并入英文正文。
- 收费生成失败或结果未知时不自动重发；先保留响应及原图、报告，再由用户决定。

**实际生成原图：**

```text
C:\Users\Administrator\Pictures\gptGen\enemy109_charge_interrupted_keypose_v1.png
```

同名 `.request.json`/`.response.json`已保存完整提交字段和脱敏响应；本目录`generated/enemy109_charge_interrupted_keypose_v1.png`是原图的字节副本，SHA-256一致，未改色、缩放、移动或抠图。

## 生成后的验收重点（后续步骤）

- 读感明确为“冲锋失败，被迫收招”，而非命中成功或单纯换待机。
- 正面、身份、体积、材质和武器与Charging一致；不重构头盔、马头或护甲。
- 骑手仍在马上、低幅马匹急停；不违背骑乘免击退/眩晕与实际Launch才落马的机制边界。
- 四马腿/两靴/手缰/直枪连续完整，64px整剪影安全留白为本图目标；不缩放、改镜头或整体漂移。
- 纯底及主体色彩分开检查。绿幕只是后续抠图底，PNG扩展名不代表透明成品。

## 当前机制依据与未实施边界

`Assets/Scripts/Enemy/CavalryEnemy.cs` 的前摇、冲锋前移和刺击提交前可打断，本次0伤害。非刺击打断恢复0.2秒、刺击提交前打断收招0.22秒；当前`Assets/Animations/Enemy_109_InterruptedPose.anim`仍是Charging1静态占位。骑乘免眩晕/击退，真正Launch才落马。

后续如何让打断收招转到已有背向返航，需要在关键姿态验收后单独准备；本轮不改这些时长、移动、控制器或Prefab，不运行Play Mode。旧动画设计表中的分层骑手/坐骑、倒退不转身等历史提案不能覆盖现行人马合一和背向返航决定。

## 实际产物、回读和限制（2026-10-13）

- 原图1024×1024、RGB PNG / Color Type2、1,071,876字节；SHA-256 `eed75c1aefabcd3d26699f1bcafc5398b9945d0ffc2d9a7d0a0e318fdbb8b46a`。无Alpha，符合本次绿底参考图请求，不是透明精灵。
- POST仅1次、66.062秒、HTTP200、返回1张；input2227/output7024/total9251 tokens，接口未返回cost，不推断金额或币种。原始英文词和输入哈希未变，生产记录`production_record.json`与临时`runtime.json`一致。
- 已实际回读原尺寸图和`review/generated_vs_charging1.png`，能观察到骑手回仰、下颌上提、收缰，馬前膝屈起/后蹄支撑，单根枪保持画面左侧且人仍在鞍上；这只是预审观察，不代替用户验收。
- RGB绿幕诊断轮廓留白L/T/R/B=165/148/297/173px，未触边，满足64px目标。原Charging输入底边exclusive929、输出851，约上移78px；姿态改变使剪影变化，不能据此直接断言整体缩放/位移，但后续动画需单独核对鞍部/落地锚点，当前不宣称对齐通过。
- 背景角落64px块中位RGB(2,162,63)，std约0.6–0.9，不是请求的精确RGB(0,159,62)。原输出不重铺底或全图调色；背景一致与主体保色分别检查，尚未进行主体材质配准比较。
- `generated_review_report.json`记录机器回读、边界、背景和限制；该绿色诊断掩码不是Alpha，不输出抠图成品。`review/generated_vs_charging1.png`仅作同画布原图对照，展示时Nearest缩小但生成原件未改。
- user_art_verdict保留pending，补记user_defined_frame_role=K1_hit_contact_keyframe。用户定位后已按新的尾帧职责准备K2（马前蹄落地/骑手惯性前扑），0新增生成调用；不将K1重做为尾帧，也无视频创建、抠图、Unity修改或运行自检。
