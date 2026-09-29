---
id: kd_a6e378bd-4fc7-452d-82fd-18f1967e019b
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 追加修正记录

- 第一次下山目标图 `N4_to_E11_downhill_target_v1.png` 不合格：模型生成城寨、桥梁、河流、瞭望塔和旗帜，越过了E11的山脚阶段。
- 第二次目标图 `N4_to_E11_downhill_target_v2.png` 通过基础内容检查：纯自然山脚道路、连续下坡、开阔山谷、无建筑和人物；尺寸1024×1536，SHA-256 `772deaa513e9a16b6cc7c8b774994a5497793e43943fc10f5e4bc50125258df3`。
- 该图仍是视频目标环境参考，不是必须逐像素对齐的last frame。用于视频时应与真实起始帧以普通reference_image上传，继续要求真实向前下坡和逐步显露，禁止尾帧闪现。
