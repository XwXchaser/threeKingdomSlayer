# Curved Scroll Lab

## Real combat trial (iteration 3, partial verification)

`Assets/Experiments/CurvedScroll/ScrollCombatTrial.unity` is an isolated copy of Battle with the scroll environment and `TrialBattle.asset` (three 101 enemies). Original route runtime, tutorial DialogueManager (persistent tutorial writes), missing scripts and old tint probe were removed only from this copy. Stage auto-start is disabled in the copy. Source StageController gained a default-true autoStartStage switch and route-completion dispatch no longer requires a V2 runtime object.

Flow: 20-unit approach -> settle -> real battle -> wait for experience/upgrade/discard -> explicit Continue -> 20-unit departure. There is no trial victory settlement or checkpoint write. Original shared stage configs, enemy prefabs and Battle scene remain unchanged. Do not use the copied production restart/main-menu buttons for isolation testing; exit Play Mode instead. Lab keyboard controls remain available and can interfere with controlled travel; do not use Space/N/R in this trial.

Verified: compilation, travel stopping at 20, three actual enemies and InProgress state, zero Console errors at startup; defeat fallback displays a terminal trial state. NOT verified: successful clear/reward/Continue chain, persistent save diff, combat alignment screenshots, Boss/QTE/knockback regressions. Diagnostic high damage was attempted in an already failing run and is not evidence of a passed clear test. Current checkpoint leaves the Editor in Edit Mode for safe continuation.


Open `Assets/Experiments/CurvedScroll/CurvedScrollLab.unity` and enter Play Mode.

This is an isolated visual experiment, not a route node scene. It contains a camera and CurvedScrollLab; it does not instantiate Enemy behaviours, battle managers, colliders, or save systems. Shared sprites are read-only references. All materials and generated meshes belong to this experiment.

## Controls

- Flat / Curve / + Backdrop: compare the same scroll position.
- Space or Pause: pause simulation. N / Step: advance 1/30 second. R / Reset: reset distance and speed.
- Sliders: speed, curvature, flat zone, road width, normalized cycle position (seeking pauses).
- Travel + stop: travel the configured tripLength then stop. Loop: resume continuous travel.
- Inspector: transitionLength, viewDistance, spriteScale, roadsideGap, paperTilt, per-sprite normalized footAnchors, acceleration, brakingDistance.
- seed, displaySprites, footAnchors are initialization settings: edit before Play Mode or restart the scene to rebuild.
- Inspector changes during Play Mode are temporary. Stop Play Mode and edit to persist.

## Implementation boundaries

CPU-deformed subdivided road (240 rows, 5 bands), 40 pooled visual papers, static placeholder mountain silhouette. The lab root defines horizontal forward/local coordinates; do not pitch or scale the root. Camera uses the original 3-unit height, 18-degree pitch, 60-degree FOV for comparison. This is quadratic horizon curvature, not a complete physical cylinder.

Sprites are static test poses, not animated enemies. GUI is experimental IMGUI rather than production HUD. It is not added to Build Settings, and no production scene has been modified. Backdrop is a generated silhouette, not final background art. No route integration, turning, real combat, or save restoration is implemented.

## Depth layer comparison (iteration 2)

ScrollDepthLayers adds 16 flag slots, 64 alternating rock/bush slots, 80 road patch slots and a second static distant silhouette. Density selects a deterministic subset. All local scenery uses the same travel distance; only perspective changes apparent speed. No independent sliding layers or camera shake.

Bottom panel: Baseline / Layers / + Ground presets preserve current progress, camera, speed and curvature. Near/Middle/Far/Ground/Stripes can be toggled independently. Inspector exposes nearMargin, middleSpread, nearScale, middleScale, forestColor, forestHeight, forestOffset and seed (seed requires restart). Distant forest is shown only in + Backdrop view mode. Baseline restores the original striping; layered presets disable it.

Additional runtime check: all presets held distance at 30.08; simulated distance reached 1191.91 with 223 total lab subtree Transforms before/after, unchanged camera, zero Enemy components, zero Console errors. 1080x1920 screenshots inspected with controls hidden. These are placeholder polygons, not final art. Far-edge objects use a short height reveal and patches shrink at the loop boundary; extreme/flat-mode reveal quality still needs visual review. No performance budget or additional resolution acceptance is claimed.

## Checks performed

- Full Unity compilation/domain reload succeeded; Console had zero errors during initial runtime checks.
- Ten 60-unit trips ended at distance 600 with exactly ten arrivals.
- Simulated 1161.825 units (>10 cycles of the 94-unit range); 40 papers and 42 generated children remained constant.
- Flat mode drop and near-zone drop were zero.
- Pause and timeScale=0 both held distance across ten Editor frames.
- Runtime contained zero Enemy components and zero Colliders.
- Actual 1080x1920 Game screenshots inspected for flat, curved, and curved+backdrop modes.

Pending: continuous visual user acceptance, animated-pose foot calibration, additional aspect ratios/safe area, precise GC/frame-time/DrawCall profiling and target-device budget, extreme settings, travel controls at variable frame rates, all real battle/route regression cases. Object-count checks are not a complete performance test.
