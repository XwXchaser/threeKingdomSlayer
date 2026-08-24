---
id: kd_c59a496c-fce5-40ee-87a7-4ebaa34ded0e
injectMode: inherit
summary: 'Independent charge-hit shockwave upgrade: actual damage taken while charging increases its next charged-release damage.'
aiEditMode: inherit
---

- New independent three-choice upgrade asset: `Assets/ScriptableObjects/Upgrades/Definitions/ChargeHitShockwave.asset` (`upgradeId`/`effectType`: `charge_hit_shockwave`, category AttackPassive).
- Per-level data lives in `UpgradeDefinition.chargeHitShockwaveLevels`: `shockwaveCount`, `baseDamage`, `rangeRows`, `damageBonusPerHit`.
- `UpgradeEffectManager` owns acquired level and current hit count. `RegisterChargeHitShockwaveHit()` increments only after `PlayerState.TakeDamage` reaches actual health loss while charging.
- Invulnerability and fully absorbed reflect-shield hits return before incrementing. Reduced but nonzero damage counts once.
- Charged Pierce, Sweep, and Launch call `AttackSystem.ReleaseChargeHitShockwave()` before their normal attack effect. It consumes/reset the accumulated hit count, queries front rows, and creates Slash-type `AttackWave` instances using the Slash attack prefab.
- `BuffDisplayPanel` polls `GetChargeHitShockwaveBonusPercent()` and displays the live accumulated percent in the icon upper-right.
- This system is independent from existing timed/queued `charge_shockwave`; both may release on the same charged attack.
