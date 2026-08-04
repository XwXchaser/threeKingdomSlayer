---
id: kd_329ff503-f100-4792-8780-aef00b2b82e1
type: memory
path: unity-project-understanding/pierce-attack-runtime.md
title: pierce-attack-runtime
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1785839196682
updatedAt: 1785839196682
---

# pierce-attack-runtime

## Summary
Pierce currently slides static Stab prefab through one column via AttackWave; records deferred charge-release redesign constraints.

<!-- locus:body:start -->
## Current Pierce construction
- `AttackSystem.ExecutePierce` snapshots one selected column through `ColumnManager.GetEnemiesInRange`, releases charge-linked shockwaves, then creates an `AttackWave` with the Pierce config.
- `Zhangfei_Pierce.asset`: damage 20, rangeRows 5, cooldown 3, actionDuration 0.50, prefab `Assets/Prefabs/Stab.prefab`.
- `AttackWave.SetupTravel` moves the static `stab.png` prefab along world Z at fixed natural speed 8 toward the furthest target plus 3 world units, then pauses 0.05s and fades for 0.35s. The full sequence is time-scaled to the configured visual target duration.
- Targets are sorted by world Z and damaged when the wave root crosses each target Z threshold. First target gets Standard feedback and pauses the travel sequence; later targets get Light feedback.
- Current visual does not consume the charge pose or use `stab_charge1/2/ready`, has no dedicated release burst, per-target penetration pose, or distinct recovery. This makes the charged Pierce visually similar to a large static Stab sprite sliding through the column.

## Deferred redesign direction
- Keep the existing target snapshot, Z-threshold order, damage, and charge-shockwave pipeline authoritative.
- Add an opt-in Pierce-specific visual branch rather than globally changing shared `AttackWave` Travel behavior used by Sweep, return waves, and Phantom attacks.
- Proposed first phase: charge-ready/charge2 release pose -> charge1 transition -> accelerated `stab` penetration -> first-hit Standard stop and later Light feedback -> short over-penetration hold -> fast fade/recovery.
- A later phase may add visual-only afterimages/trail, per-row brightness or scale impulses, and a final penetration flash. Visual density must remain decoupled from damage count.
- If integrating `ChargeStabVisual`, transfer its pose deliberately and preserve cleanup ownership; do not let visual handoff alter Pierce hit timing.
<!-- locus:body:end -->
