---
id: kd_aece36e4-c1d4-4acb-a0de-f035f5318780
injectMode: inherit
summary: Boss TripleStab QTE 箭矢的统一追踪与销毁规则。
aiMaintained: inherit
---

## Boss QTE 箭矢生命周期
- `QTEController` 以 `_arrowWaves` 统一拥有所有 TripleStab 箭矢；成功 Deflect 后不得立即从该集合移除，因为箭仍在坠落淡出，QTE 完成时需能统一强制销毁。
- stagger 发射的 `DOVirtual.DelayedCall` 必须保存 Tween 句柄；`ClearAllArrowWaves()` 先 Kill 并清空这些延迟，再销毁所有追踪箭矢。
- 所有退出边界都使用同一清理入口：新 QTE 开始、正常完成、Abort、切换 QTE 数据、控制器 OnDestroy。
