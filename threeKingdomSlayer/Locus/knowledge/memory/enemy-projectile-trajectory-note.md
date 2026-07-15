---
id: kd_aece36e4-c1d4-4acb-a0de-f035f5318780
type: memory
path: enemy-projectile-trajectory-note.md
title: enemy-projectile-trajectory-note
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1784088905696
updatedAt: 1784088905698
---

# enemy-projectile-trajectory-note

## Summary
Enemy arrow trajectory/facing and cleanup lifecycle rules.

<!-- locus:body:start -->
### Enemy projectile single-trajectory rule
- `EnemyProjectile` now drives flight with one scalar DOTween progress value and one trajectory evaluator: XZ interpolate from source to the existing randomized landing X/Z; Y uses the arc curve.
- Root rotation derives each update from the next trajectory position minus current position, so trajectory and facing share one source and the arrow cannot switch to a conflicting absolute rotation on descent.
- Existing `Enemy.SpawnProjectile()` source position and `projectileLandingXCenter ± projectileLandingXSpread` are retained, preserving per-archer lateral spread.
- Cleanup ownership is centralized in `DisposeProjectile()`: it stops flight/deflect tweens and timeout, fades all child SpriteRenderers once, then destroys the object from either fade completion or kill. Arrival and safety timeout use this path; Deflect kills flight then uses its own fall/fade before the same disposal path.
- Files: `Assets/Scripts/Enemy/EnemyProjectile.cs`, `Assets/Scripts/Enemy/Enemy.cs`.
<!-- locus:body:end -->
