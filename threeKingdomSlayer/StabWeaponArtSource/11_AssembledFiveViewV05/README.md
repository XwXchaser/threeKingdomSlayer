# 带红缨长枪｜完整装配五视图候选 V05

绝对目录：`H:\Project\threeKingdomSlayer\threeKingdomSlayer\StabWeaponArtSource\11_AssembledFiveViewV05\`

本次已按用户批准提交一次生成，HTTP 200返回一张1536×1024白底RGB PNG。图中五个视角都显示同一把带单根红缨的长枪，保留枪刃、金绿枪套、红棕枪杆和金色尾鐏。生成图及中文解释版已交付，最终美术验收待确认。

**顶／底比例仍未通过建模几何核对，作为候选保留。** 正／侧／背的装配基本可读；侧／正面深宽比约0.35，顶视约0.72、底视约0.83，超出提示词的0.32–0.40约束。顶底红缨折面和遮挡也不严格对应，不能用它们直接确定模型厚度。

## 主交付

- `assembled_five_view_plain.png`：1536×1024无字原合图，与远端下载原文件逐字节一致。
- `assembled_five_view_annotated.png`：2304×1760中文说明板，标明视图、观察方向、红缨挂点和比例限制；图像区域按原尺寸排列，无缩放或变形。
- `views/front.png`、`views/right.png`、`views/back.png`：各384×1024，完整带红缨长枪正／侧／背。
- `views/top.png`、`views/bottom.png`：各448×384，完整装配端视候选。

生成没有完全服从预定图格，背面与顶视略跨格。导出使用实际留白边界，确保每件武器及红缨完整；未通过独立缩放、旋转或变形掩盖差异。实际裁切坐标见 `layout_transforms.json` 与 `trace/visual_review.json`。

## 检查与追溯

`delivery_verification.json` 记录PNG解码、五张裁切和中文板图像区域的逐像素一致性、无字原图哈希以及美术限制。主体完整；三张长幅图枪尖高度差约2px、枪尾差约1px。顶部留白约24–26px，低于提示词48px目标。

`trace/` 含本次提示词、批准规格快照、请求、状态及技术/视觉检查，仅供追溯。`source_manifest.json` 记录交付文件哈希。原始服务响应保留于外部输出目录，不重复复制进项目。

原始成图：`C:\Users\Administrator\Pictures\gptGen\stab_assembled_five_view_v05\stab_assembled_five_view_v05.png`

原图SHA-256：`15fddd686807898ae93547d580426d80e286ea9e876614a528d5ff05ee13c154`。

没有追加第二次生成，没有修改Unity资产、场景、Prefab或运行状态。已验收的枪体V02、尚待验收的红缨V02和此前评审版本均保留。工程一致的顶底投影仍需基于同一份确认几何生成。
