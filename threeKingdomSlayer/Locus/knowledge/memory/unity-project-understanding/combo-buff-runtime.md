---
id: kd_c3aaf3f5-cbf9-4354-a7ae-12c922b766c9
injectMode: inherit
summary: 连击Buff运行时：ComboManager断连清理、BuffManager同buffId叠加、UpgradeEffectManager属性应用，以及当前20连击伤害+10%配置。
aiEditMode: inherit
---

# Combo Buff Runtime

- `Assets/Scripts/Core/ComboManager.cs` 保持原连击计数、断连窗口和冻结规则；达到配置阈值后调用 `BuffManager.AddBuff(..., duration:0)`，并记录本轮触发的 buffId。`ResetCombo()` 会逐个移除这些 Buff。
- `Assets/Scripts/Core/BuffManager.cs` 对不同 buffId 分开存储；相同 buffId 会追加 modifiers 并逐项应用，因此不同阈值可叠加到同一 Buff。`RemoveBuff(buffId)` 会撤销其全部 modifiers。
- `Assets/Scripts/Core/UpgradeEffectManager.cs` 实现 `IStatModifierApplier`。当前支持 statId：`atk`、`attack_speed`、`move_speed`、`exp`；临时 Buff 加成与三选一永久升级加成在最终查询值中相加。
- `Assets/ScriptableObjects/Combo/ComboBuffConfig.asset` 当前唯一档位：20 连击，buffId=`combo_damage`，`atk` Multiply 0.10。
- 已部署头像 Buff UI：`HeroHUD_Zhangfei/HudCard/Health(Slider)/UltPortraitButton/ComboBuffStrip` 挂载 `ComboBuffHUD`。首次 `combo_damage` 生效时显示红色短剑与 `+10%`；断连时由 `ComboManager.OnComboUpdated(0)` 隐藏。
- `ComboBuffHUD.cs` 按 buffId `combo_damage` 汇总当前 `atk` modifiers，显示累计百分比；`SpriteNumberDisplay.ShowSignedPercent()` 支持 `+value%`。
- 素材：短剑导入 `Assets/Sprites/BatlleHUD/ComboBuff/combo_red_dagger_pixel.png`；红边白底数字在 `Assets/Sprites/BatlleHUD/ComboBuffNumbers/`（`combo_num_0`–`combo_num_9`、`combo_num_plus`、`combo_num_percent`）。均设为 Sprite、Point、Clamp、无 mipmap。
- `ComboManager.ComboResetProgress` 在冻结期间以 `_freezeStartedAt` 代替 `Time.time` 计算，因此 QTE Strict 生命周期内 `ComboDisplayUI` 的读条停在进入时的进度；`Resume()` 原有的 `_lastHitTime` 补偿使其结束后从该进度继续。编辑器验证：0.700 → Freeze 0.700 → 等待后 0.700 → Resume 0.700。
