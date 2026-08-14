---
id: kd_39adfe80-4334-459c-92a3-0953f4b29c8c
injectMode: inherit
summary: 敌人波次染色的对象池残留和材质实例生命周期故障：根因、修复与预防规则。
aiMaintained: inherit
---

- 症状：后续染色波的敌人可能在对象池复用后保持错误颜色，或出现白图/崩坏显示。
- 根因：`Enemy.ApplyWaveScaling()` 对 `Color.white` 提前返回，未恢复上波写入的 `SpriteRenderer.color`；同时 `ResetEnemy()` 销毁 Renderer 仍持有的材质实例，后续依赖 Unity 隐式重建，存在材质引用失效风险。
- 修复：每次波次强化均显式写入 `_prefabColor * tint`（白色也写入）；敌人首次创建专属材质并显式绑定，池复用只恢复视觉状态，材质仅在 `OnDestroy` 销毁。
- 预防规则：对象池对象的每个视觉状态必须在 checkout 时显式恢复基准值；不要在回收时销毁 Renderer 当前绑定的材质并期待 Unity 自动恢复。
- 文件：`Assets/Scripts/Enemy/Enemy.cs`
