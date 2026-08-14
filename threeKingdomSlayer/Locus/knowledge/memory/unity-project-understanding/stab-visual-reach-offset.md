---
id: kd_d16786ae-1273-4fe3-b121-e16b0cca4a77
injectMode: inherit
summary: 'Stab has two visual-only offset mechanisms: (1) a forward reach offset that extends beyond hit range, and (2) a random target disk offset that adds visual variation to the stab endpoint. Both are independent of hit detection, damage, and range.'
aiMaintained: inherit
---

## Visual Reach Offset

`AttackSkillConfig.stabVisualReachOffset` controls only the Stab prefab's local forward placement during `StabSweepEffect.Create`.

- Unit: world distance.
- Positive values extend the visual weapon tip farther along the existing ray path.
- It does not alter `rangeRows`, ray target distance, hit timing, damage, or displacement.
- `Zhangfei_Stab.asset` currently uses `2f`; previous memory value `0.5f` was outdated.
- `AttackSystem.stabVisualStartXOffsets` is a five-element world-X visual-only offset array for columns left→right; current first pass `[-1, -0.5, 0, 0.5, 1]`.
- The X offset is applied after the visual's local placement, so the container path and hit calculations remain unchanged.
- Implementation: the StabRay container is oriented before the visual is instantiated; the visual local back offset is reduced by the reach field.

## Visual Target Random Offset (stabVisualTargetRandomRadius)

`AttackSkillConfig.stabVisualTargetRandomRadius` adds visual-only random offset to the stab's endpoint, making repeated stabs look slightly different without affecting hit detection.

- Field type: `float [Min(0f)]`, default `0.12f`
- The original target point serves as center of a disk on the camera-facing plane (using `Camera.main.transform.right` and `.up`)
- A random point within the disk is chosen using uniform disk distribution: angle = random 0-2π, distance = radius × sqrt(random 0-1)
- Radius scales with range via `radius = baseRadius × sqrt(currentRayLength / baseRayLength)`, clamped to [0.5×, 1.5×] of baseRadius
- `baseRayLength` is computed in `AttackSystem.ExecuteStab()` as `spacing × 2f` (the original base ray length before visual range extension)

### Implementation Architecture

A `VisualOffsetRoot` Transform layer sits between the StabRay container and the DeformRoot:

```
StabRay (transform)
  └─ VisualOffsetRoot (created at Initialize, DOLocalMove target during thrust)
       └─ DeformRoot (scale animation, motion blur)
            └─ Visual (SpriteRenderer)
```

- `VisualOffsetRoot` is created in `Initialize()`, parented to `transform` at local zero
- `DeformRoot` is parented under `VisualOffsetRoot` instead of directly under `transform`
- During thrust phase: `_visualOffsetRoot.DOLocalMove(_visualTargetOffsetLocal, thrustDuration).SetEase(Ease.OutCubic)`
- During retract phase: `_visualOffsetRoot.DOLocalMove(Vector3.zero, retractDuration).SetEase(Ease.OutCubic)`
- The offset is computed once at `Initialize()` time, not per-frame
- If `stabVisualTargetRandomRadius` is 0 or `Camera.main` is null, offset is `Vector3.zero` (graceful degradation)
- The world-space camera-plane vector is converted to local space via `transform.InverseTransformVector()` before storage

### Changed Signatures

`StabSweepEffect.Create()` and `StabSweepEffect.Initialize()` both gained two new parameters:
- `float visualTargetRandomRadius` — from `AttackSkillConfig.stabVisualTargetRandomRadius`
- `float baseRayLength` — the original base ray length for radius scaling

Relevant files: `Assets/Scripts/Core/AttackSkillConfig.cs`, `Assets/Scripts/Attack/StabSweepEffect.cs`, `Assets/Scripts/Player/AttackSystem.cs`, `Assets/Prefabs/UI/Skills/Zhangfei_Stab.asset`.
