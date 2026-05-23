---
id: kd_4a9116b1-c70a-4de3-8eeb-801deb71c4fe
type: memory
path: project-mistake-note.md
title: project-mistake-note
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1778764012219
updatedAt: 1779555009287
---

# project-mistake-note

## Summary
更新至 2025-07-16 — ExpBar DisabledColor + ExpGem 屏幕空间 baseSpeed 缩放

<!-- locus:body:start -->
### ExpBar Fill 亮度异常暗沉 ✅ 已修复（2025-07）
- 症状：ExpBar 中的 Fill Image（sprite=ex_fill）渲染亮度明显低于美术素材原图。Image.color=(1,1,1,1)、material=UI/Default、sprite 像素正确、无 Mask/RectMask2D、CanvasGroup alpha=1，一切参数名义上正确但 Fill 仍然偏暗且半透明
- 根因：ExpBar 的 Slider 组件 `m_Interactable: 0`（不可交互），Unity Selectable 机制自动将 ColorBlock 中的 `m_DisabledColor` 应用到 targetGraphic（Fill Image）。原 DisabledColor 为 `RGBA(0.784, 0.784, 0.784, 0.502)`（灰色 + 50% 透明度），导致 CanvasRenderer.color 被覆写为该值
- 排查关键：Image.color 仅设置组件的目标颜色，实际渲染由 CanvasRenderer.color 决定。Slider（Selectable 子类）在非 interactable 时会覆写 targetGraphic 关联的 CanvasRenderer.color。用 `CanvasRenderer.GetColor()` 而非 `Image.color` 才能看到真实渲染色
- 修复：将 Slider ColorBlock 的 `disabledColor` 设为 `(1,1,1,1)`（与 normalColor 一致），使 interactable 状态变化不影响渲染颜色
- 预防规则：所有纯展示型 Slider/Button/Toggle（不被玩家操作）若设为 `interactable=false`，必须将其 ColorBlock 的 disabledColor 设为与 normalColor 相同的值，否则 UI 渲染颜色会异常
- 文件：Assets/Scenes/Battle.scene (ExpBar Slider)

### 经验宝石飞行速度极慢 ✅ 已修复（2025-07-16）
- 症状：ExpGem 从世界空间 SpriteRenderer 切换到屏幕空间 UI Image 后，宝石飞行几乎不动
- 根因：世界空间与屏幕空间的坐标尺度完全不同。世界空间两物体间距通常 5~20 单位，baseSpeed=8 合适。切换到屏幕空间（Canvas 参考分辨率 1080×1920）后，宝石从敌人位置飞到 ExpBar 距离约 500~1500 像素，baseSpeed=8 意味着需要 60~180 秒
- 修复：将 `ExpGemManager.baseSpeed` 从 8 改为 800（屏幕空间像素/秒），同时更新场景中已序列化的值
- 预防规则：切换坐标系（世界↔屏幕）时必须检查速度/距离参数的量纲是否匹配。世界空间：1~20 单位/s；屏幕空间（参考分辨率）：500~1500 像素/s

### .meta 文件 GUID 不可直接读取 ✅ 已记录（2025-07-17）
- 症状：使用 bash `find` + `head` 直接读取 `.meta` 文件获取 GUID，然后通过 GUID 查找资产，结果不可靠。部分 Sprite 的 .meta 文件 GUID 与实际 AssetDatabase 中的 GUID 不一致
- 根因：Unity 可能在导入过程中内部重新映射精灵子资产的 GUID，.meta 文件中记录的 `guid:` 值与 `AssetDatabase.FindAssets` 返回的实际 GUID 不同，特别是 Texture 类型导入为多个 Sprite 子资产时
- 预防规则：获取 GUID 永远通过 `AssetDatabase.FindAssets("t:Sprite ...")` / `AssetDatabase.GUIDToAssetPath` 等 Editor API，禁止直接解析 .meta 文件获取 GUID
<!-- locus:body:end -->
