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
updatedAt: 1786528929781
---

# pierce-attack-runtime

## Summary
Pierce currently slides static Stab prefab through one column via AttackWave; records deferred charge-release redesign constraints.

<!-- locus:body:start -->
## Current Pierce construction
- `AttackSystem.ExecutePierce` snapshots one selected column through `ColumnManager.GetEnemiesInRange`, releases charge-linked shockwaves, then schedules a release callback through `Assets/Scripts/Effects/AttackReleaseTimeline.cs`.
- The release timeline uses `actionDuration` (scaled by attack speed) and invokes Pierce release once at about 42% of the player action. The callback filters dead targets before creating the `AttackWave`.
- `Zhangfei_Pierce.asset`: damage 20, rangeRows 5, cooldown 3, actionDuration 0.50, prefab `Assets/Prefabs/Stab.prefab`.
- `AttackWave.SetupTravel` moves the static `stab.png` prefab along world Z at fixed natural speed 8 toward the furthest target plus 3 world units, then pauses 0.05s and fades for 0.35s. Pierce now receives no cooldown-derived `targetDuration`, so this lifecycle remains independent of the player action lock.
- Targets are sorted by world Z and damaged when the wave root crosses each target Z threshold. First target gets Standard feedback and pauses the travel sequence; later targets get Light feedback.
- Pierce still has no dedicated weapon出手 visual; the timeline currently supplies the release boundary only. A dedicated Pierce release visual should replace the timing-only timeline without changing the target snapshot or AttackWave authority.

## Current Sweep construction
- `AttackSystem.ExecuteSweep` snapshots all targets within the effective rows, releases charge-linked shockwaves, then schedules a release callback through `AttackReleaseTimeline` at about 48% of `actionDuration`.
- The callback filters dead targets before creating the configured Sweep `AttackWave`; the wave is no longer stretched to `cooldown`.
- `Zhangfei_Sweep.asset`: damage 5, rangeRows 3, cooldown 2, actionDuration 0.55, prefab `Assets/Prefabs/sweep.prefab`.
- Sweep still has no dedicated player weapon出手 visual; the timeline currently supplies the release boundary only. A dedicated Sweep release visual should replace the timing-only timeline without changing the target snapshot or AttackWave authority.

## Deferred visual redesign direction
- Keep the existing target snapshot, damage, row order, hit feedback and charge-shockwave pipeline authoritative.
- Add opt-in Pierce/Sweep release visual branches rather than globally changing shared `AttackWave` Travel behavior used by Stab, return waves, and Phantom attacks.
- Pierce target language: charge-ready pose -> small rearward alignment -> explosive straight thrust -> release the piercing body -> fast recovery.
- Sweep target language: side/rear windup -> broad horizontal weapon sweep -> release the wide wave -> inertia and return.
- The future visual component owns presentation and release callback only; the detached AttackWave owns flight, per-row hit timing, fade and destruction.
- Visual density must remain decoupled from damage count. If `ChargeStabVisual` is integrated, transfer its pose deliberately and preserve cleanup ownership.
<!-- locus:body:end -->
