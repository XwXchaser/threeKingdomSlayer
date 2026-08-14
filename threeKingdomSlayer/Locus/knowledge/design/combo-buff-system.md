---
id: kd_b6cf4b62-1523-4614-a917-e03794a2eb27
injectMode: inherit
summary: 连击 Buff：20连击伤害+10%的首个配置；支持Inspector自定义阈值、通用单属性修正、同buffId叠加、不同buffId独立显示，断连统一移除，图标显示在玩家头像左下方。
aiMaintained: inherit
---

# 连击 Buff 设计

## 当前配置
- 暂时只配置一个档位：20 连击时获得伤害 +10%。
- 其他连击阈值暂不配置。
- 不修改现有连击计数、计数来源、断连窗口和冻结恢复规则。

## Buff 规则
- 连击 Buff 持续到连击断开；断连时统一移除所有连击 Buff。
- 阈值和属性效果由 Inspector 配置，不固定阈值或固定属性。
- 每个连击 Buff 只配置一个属性修正；属性类型仍使用通用 `StatModifier`（如伤害、攻速等）。
- 不同阈值可以配置相同 `buffId`，相同 `buffId` 的修正需要叠加生效。
- 不同 `buffId` 即使修改相同属性，也独立生效、独立展示，不互相合并。

## UI 规则
- Buff 生效时，在玩家头像左下角显示对应图标。
- 不同 `buffId` 的图标从左到右排列。
- 相同 `buffId` 合并为一个图标，并显示该 Buff 的累计总加成值。
- 图标由每个连击档位直接配置。
- 连击 Buff UI 使用玩家头像专用挂点，不复用现有升级/道具 `BuffDisplayPanel`。

## 实现状态
- 已完成 Buff 数值与生命周期逻辑；头像图标 UI 暂缓到美术素材确定后实现。
- 当前 `Assets/ScriptableObjects/Combo/ComboBuffConfig.asset` 只配置：20 连击、`combo_damage`、`atk` Multiply +0.10。
- `BuffManager` 支持相同 `buffId` 追加并叠加修正；`ComboManager.ResetCombo()` 会移除本轮触发的所有连击 Buff。
- `UpgradeEffectManager` 已作为 `IStatModifierApplier` 接入实际伤害、攻速、移速与经验倍率查询，其中当前配置只使用 `atk`。

## 工程注意
- UI 实现时仍需按 `buffId` 聚合图标与总加成，不应按 `statId` 合并不同来源的 Buff。
- 连击 Buff 使用永久时长（`endTime=0`），只由断连/关卡重置的 `ResetCombo()` 移除。
