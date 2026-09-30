---
id: kd_622bcd8d-fa1a-4422-8c9e-0d12538575c4
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 场景实例值覆盖 C# 默认值：取证必须读实例

## 结论

`Assets/` 里组件的 **C# 字段默认值不等于运行时实际值**。场景 / Prefab 的序列化覆盖、以及 `Awake` 里对字段的二次计算，都会让实际值与代码默认值不同。做设计判断、写文档或评估手感时，必须从**场景实例**读取，不能引用代码默认值。

## 已验证的偏差（Battle 场景，2026 检查）

| 字段 | C# 默认 | 场景实际 | 偏差来源 |
|------|---------|----------|----------|
| `InputManager.minChargeTime` | 0.5 | **1.0** | 场景覆盖 |
| `InputManager.swipeThreshold` | 30 | **50** | `Awake` 按 DPI 缩放并钳制 30–150 |
| `AttackSystem.useActionBasedCooldown` | false | **true** | 场景覆盖（动作锁为实际运行模式） |
| `AttackSystem.parryProjectileRange` | 4 | 10 | 场景覆盖 |
| `AttackSystem.pierceTimeScale` | 3 | 1 | 设计观察用，已调回原速 |

同一次检查还确认：`InputManager.longPressDuration` = 0.3（与默认一致），`HeroConfig` = 张飞。

## 取证方式

- 读实例：`unity_execute` 里 `Object.FindObjectOfType<T>()` 后打印字段；注意 `Awake` 会改写字段（如 `swipeThreshold`），所以必须读**运行中的实例**而非资产。
- 读资产装配：`AssetDatabase.LoadAssetAtPath<T>` + `AssetDatabase.GetAssetPath` 打印引用链。
- 反向依赖（谁引用了某资产）：`AssetDatabase.GetDependencies` 遍历全部资产自建反向表。

## 工具现状

Locus 资产索引当时为空（`unity_asset_search` 返回 `total:0`），因此 `unity_ref_search` 不可用，只能用上面的 `unity_execute` + `AssetDatabase` 方案。若索引已重新扫描，可优先用索引工具。

## 教训

曾据 C# 默认值在文档里写下「蓄力门 0.5s」，实际是 **1.0s**，导致对连招节奏问题的严重性估计偏低，返工修正。
