---
id: kd_b6e5ab2f-d5b8-42c9-8008-1581eb503440
type: memory
path: unity-project-understanding/slash-attack-runtime.md
title: slash-attack-runtime
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1785836640662
updatedAt: 1785838512135
---

# slash-attack-runtime

## Summary
Slash runtime: Stab prefab linear-X/Z-rotation sweep, frame timing, root-X hit thresholds, hit-stop, directional push, and shared QTE/Phantom blast radius.

<!-- locus:body:start -->
## Current Slash construction
- Input: a short non-vertical swipe invokes `AttackSystem.TryExecuteAttack(AttackType.Slash, ..., slashLeftToRight)`; charged diagonal swipe can also fall back to Slash.
- Main entry: `AttackSystem.ExecuteSlash` snapshots all enemies in effective range through `ColumnManager.GetAllEnemiesInRange`; InCombat Bosses use the existing range exception.
- Main visual uses `Assets/Prefabs/Stab.prefab`, not `slash.png`. `SweepEffect` moves the logic root across world X while rotating around Z, flips the visual for one direction, and swaps `stab -> stab_rotate1 -> stab_rotate2` at 0%/10%/30% of the active swing.
- Current `Zhangfei_Slash.asset`: damage 8, rangeRows 1, cooldown 0.65, actionDuration 0.50, halfWidth 3, sweepAngle 60, sweepDuration 0.40, spawn Y 0, spawn Z -3.60.
- Normal Slash enables an isolated enhanced-motion mode. The logic root keeps the established start/end positions and root-X hit threshold; the prefab becomes a visual child for local anticipation/inertia only. At actionDuration 0.50s the timeline is 0.04s anticipation, 0.02s return, 0.29s active swing, 0.06s follow-through, and 0.09s fade. The active root movement/rotation uses `Ease.InOutCubic`.
- Hit timing sorts target snapshots by world X and damages only while the logic root moves, when its X crosses each target X. The anticipation and follow-through child motion do not run hit scanning.
- First actual hit is Standard feedback and pauses the whole attack sequence for 90ms realtime; later enemies receive Light feedback without additional attack-visual pauses.
- First hit grants energy and emits `OnAttackPerformed`; after all target thresholds are processed, Slash directional push is applied to collected hit targets.
- `SweepEffect` is shared by main Slash, Phantom Slash, and QTE swipe input visuals. Enhanced motion is opt-in only for normal Slash; Phantom and QTE retain the legacy path.
- `Assets/Prefabs/sweep.prefab` uses `Assets/Sprites/zhangfei/slash.png`, but that prefab belongs to the separate charged Sweep/attack-wave path, not the normal Slash weapon animation.

## Optimization constraints
- Preserve the existing root-X hit timing, range, damage, directional push, Boss exception, and first-hit resource timing while optimizing presentation.
- Do not use `SpriteRenderer.bounds.min/max.x` as the weapon contact point: it is an axis-aligned bounding box of the whole rotated silhouette, including the long shaft, afterimage/smear pixels, sprite swaps, and flip. In the current art it can extend more than five world units ahead of the root and causes premature damage/displacement.
- Keep presentation-only anticipation/follow-through on the visual child; only the logic root may drive hit thresholds.
- Any added trail/afterimage should be visual-only; do not couple visual instance count to damage count.
<!-- locus:body:end -->
