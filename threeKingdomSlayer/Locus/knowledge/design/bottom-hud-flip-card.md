---
id: kd_acd08d41-9288-45eb-860f-6483ef52584e
type: design
path: bottom-hud-flip-card.md
title: bottom-hud-flip-card
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1784129903567
updatedAt: 1784285649660
---

# bottom-hud-flip-card

## Summary
底部战斗 HUD 已复用现有素材改为 Overlay 双面翻牌：正面保留战斗 HUD，背面复用 QTEFrame。

## Content
# 底部战斗 HUD 双面看板方案

## 已实现
- `Assets/Prefabs/UI/HeroHUD_Zhangfei.prefab/HudCard` 保留血量、头像、大招、经验、技能与扩展 UI 为直属固定层，切换期间始终显示。
- 新增 `HudCard/FlipPanel`，仅容纳 `FrontFace` 与 `BackFace`，`HeroHUDFlipCard` 的旋转目标已绑定到该节点。
- `FlipPanel/FrontFace` 仅承载 `StageProgressBar`；`FlipPanel/BackFace` 承载 `QTEFrame`。
- `HeroHUDFlipCard` 以本地 X 轴完成 0°↔180°翻转，并在动画过半（90°）才切换两面 CanvasGroup，避免内容在动画起始瞬间消失。
- Boss 进入交战时翻到背面；Boss 死亡时翻回正面。对话将迁移至独立气泡层，不再参与翻牌。

## 已确认的重制方向
- 正反两面必须共用同款透明木框、铜角和位置；正面用羊皮沙盘内板，背面用暗红漆木QTE内板，形成同一实体看板的伪3D翻转。
- 正式素材拆分为：共用木框、正面羊皮内板、背面暗红内板、90°窄木侧边、战鼓待机图层、预警铜钉/鼓框图层、可交互鼓心/震波图层、成功/失败反馈图层、独立QTE指示器。
- QTE背面只承载单槽顺序QTE：Boss待机低亮 → QTE触发时铜钉依次点亮并轻震 → 指示器落入战鼓中心 → 落位瞬间开启判定 → 成功金白鼓波/失败暗红震颤 → 下一段或恢复待机。
- 显示时机与QTE数据绑定：`warningDuration` 驱动预警/指示器落位前时长；落位回调与判定窗口开始同步；`judgeWindow` 驱动可交互状态；每段Resolve驱动成功/失败图层；连续QTE按单槽队列逐段循环。
- 现有多slot配置（如TripleStab）将从并列屏幕位置改为中央单槽的顺序连段，`delay` 改为相邻段之间的等待节奏；不再使用半透明填充预警或`screenPosition`排布。

## 对话
- 对话由独立气泡层承担：播放时暂停与暗幕，但不切换看板正反面或QTE可见性。
