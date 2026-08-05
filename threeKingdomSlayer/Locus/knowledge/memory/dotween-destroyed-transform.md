---
id: kd_a2c9dc76-e50e-44a4-9726-80ae2ab41b7d
type: memory
path: dotween-destroyed-transform.md
title: dotween-destroyed-transform
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1785947499644
updatedAt: 1785947499645
---

# dotween-destroyed-transform

## Summary
攻击视觉 Tween 访问已销毁 Transform 的根因与清理规则。

<!-- locus:body:start -->
- 症状：攻击视觉对象被销毁后，DOTween 仍在下一帧访问已销毁 Transform，Unity 抛出 MissingReferenceException，可能导致游戏被强制暂停。
- 根因：Stab 的命中脉冲 Tween 独立于主 Sequence，销毁时未显式 Kill；Sweep 的独立 scaleIn/视觉路径 Tween 也未统一清理。SetTarget 不会让 Unity Destroy 后的 Transform 自动安全。
- 修复：Stab 在 OnDestroy、Sequence OnKill、OnComplete 清理视觉 Tween；Sweep 缓存视觉 Transform 与视觉路径 Transform，在 OnDestroy 清理；scaleIn 显式设置 target。
- 规则：任何独立 Tween 若访问 Transform，必须在对象销毁前显式 Kill，并覆盖 OnComplete、OnKill、OnDestroy 三条路径。
<!-- locus:body:end -->
