---
id: kd_1e5bda6b-3133-419b-bb0d-44ddfdbe9366
type: memory
path: plan-a-hero-hud-implementation.md
title: plan-a-hero-hud-implementation
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1778769385905
updatedAt: 1783672643049
---

# plan-a-hero-hud-implementation

## Summary
Hero HUD 当前架构：Battle 场景常驻可视化 HeroHUDRoot + 每角色 HeroHUDSkin + 可选 extraUIPrefabs；旧 heroHUDPrefab 仅作兜底。

<!-- locus:body:start -->
## 2025-12 更新：场景常驻 HUD + Skin
- 推荐运行入口改为 `BattleHUD.sceneHeroHUD`，当前绑定 `Assets/Scenes/Battle.scene/BattleHUD(Canvas)/HeroHUDParent/HeroHUDRoot`，可在场景中直接可视化调整布局。
- `HeroConfig.hudSkin` 是每角色 HUD 视觉配置入口；`HeroConfig.heroHUDPrefab` 保留为旧方案/兜底。
- `HeroHUD.ApplySkin(HeroHUDSkin)` 替换血条、护盾、大招头像/底图/填充、技能图标，并把 `extraUIPrefabs` 实例化到 `HeroHUD.extraUIRoot`。
- `Assets/ScriptableObjects/Warrior/HeroHUDSkin_Zhangfei.asset` 已绑定张飞素材：`zhangfei_head`、`zhangfei_filler_base`、`zhangfei_filler_main` 等。
- `Assets/Prefabs/UI/HeroHUD_Zhangfei.prefab` 根节点已移除 Canvas/CanvasScaler/GraphicRaycaster 并重置 scale=1，避免 prefab 预览与运行时布局不一致或缩放为 0 不显示。
- 大招充满火焰特效的最终挂点应放在 `Assets/Scenes/Battle.scene/BattleHUD(Canvas)/HeroHUDParent/HeroHUDRoot/Health(Slider)/UltPortraitButton/ReadyFireEffect`，作为 `UltPortraitButton` 的子物体，并位于 `UltBase`、`UltFill`、`Head` 之前渲染，才能稳定显示在头像背后。
- `UltimateButtonUI` 只负责大招 ready 状态触发（`OnEnergyChanged` / `OnReady` / `OnActivated`），实际火焰视觉由 `UIReadyFireEffect` 驱动；角色差异继续通过 `HeroHUDSkin.readyFireStartSprite` / `readyFireLoopSprites` / `readyFireFps` 下发。
- 当前张飞 HUD 先复用 `HeroHUDSkin_Zhangfei.asset` 中已有 burn 火焰帧；后续若更换专用像素火焰素材，只需替换 skin 引用，不需要改代码或场景结构。
- `UIReadyFireEffect` 现已收敛为 HUD 版单团火焰：以单个 `Image` 做帧循环 + alpha/scale pulse + 轻微 jitter，并提供 `localOffset` / `sizeScale` 作为围绕头像微调的位置与尺寸入口。
<!-- locus:body:end -->
