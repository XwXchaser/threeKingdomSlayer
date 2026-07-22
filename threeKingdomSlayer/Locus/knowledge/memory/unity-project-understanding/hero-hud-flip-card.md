---
id: kd_7d54a1b1-2197-4ca5-a669-343207f9d6d7
type: memory
path: unity-project-understanding/hero-hud-flip-card.md
title: hero-hud-flip-card
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1784131135849
updatedAt: 1784288939719
---

# hero-hud-flip-card

## Summary
底部 HeroHUD 双面翻牌的当前层级、挂点和事件接线。

<!-- locus:body:start -->
## 底部 HUD 双面看板
- `HeroHUDRoot` 是 Battle 场景常驻的 prefab 实例，来源 `Assets/Prefabs/UI/HeroHUD_Zhangfei.prefab`；结构性调整应在 Prefab Contents 中完成，再同步场景实例，直接改已连接场景实例不会生效。
- `HudCard` 固定于底部 `(0,170)`、尺寸 `(900,360)`，包含 `FrontFace` 与预旋转180°的 `BackFace`；正面承载原有战斗 HUD，背面承载 QTEFrame。
- `HeroHUDFlipCard` 订阅 QTE 开始/结束事件，并用 CanvasGroup 管理正反面可见和 Raycast；使用 `SetUpdate(true)` 避免 timeScale 暂停阻塞翻牌。
- 2026-07 截图基准：竖屏战斗画面以暖黄沙场、浅蓝天空和粗黑描边像素角色为主体；底部黑金三国纹样框占屏幕约四分之一。常驻HUD包括左上铜钱、右上暂停、左侧技能/道具圆形图标列、头像与血条；Boss战额外出现顶部Boss血条和中部道具栏。后续看板、节点与道具美术应优先采用黑金/木棕/暖黄，避免抢占中央敌我战斗区或与底部金属框产生重复厚框。
- 2026-07 看板素材已拆分并组装在 Prefab：`FlipPanel/FrontFace` 下为 `BoardInnerFront`（羊皮内板）、`StageProgressBar`、`BoardSharedFrame`；`BackFace` 下为 `BoardInnerBack`（暗红战鼓内板）、`QTEFrame`、`BoardSharedFrame`。两面各自持有一份同款外框以随伪3D翻转旋转，不能把外框作为 FlipPanel 的常驻子物体，否则翻面期间会穿帮。
- 当前正式导入素材位于 `Assets/Sprites/BatlleHUD/QTEBoard/`：`00_front_parchment_inner.png`、`01_shared_wood_frame.png`、`02_back_drum_idle.png`。Point filter、无 mipmap、Clamp；后续美术替换应保持这三个图层职责与近似3.5:1画幅。预警、就绪、成功、失败的特效图层尚未接入，等待后续美术修整和单槽顺序QTE改造。
- `BattleHUD.InstantiateHeroHUD()` 会向 `QTEDisplay` 注入 `QTEFrame` 与其 `IndicatorArea`；Play Mode 已验证引用正常注入，Boss/QTE翻向背面、退出后回到正面。
<!-- locus:body:end -->
