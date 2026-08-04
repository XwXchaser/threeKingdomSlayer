---
id: kd_d16786ae-1273-4fe3-b121-e16b0cca4a77
type: memory
path: unity-project-understanding/stab-visual-reach-offset.md
title: stab-visual-reach-offset
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1784641685626
updatedAt: 1785834694089
---

# stab-visual-reach-offset

## Summary
Stab has a visual-only forward reach offset, independent of hit range and damage.

<!-- locus:body:start -->
`AttackSkillConfig.stabVisualReachOffset` controls only the Stab prefab's local forward placement during `StabSweepEffect.Create`.

- Unit: world distance.
- Positive values extend the visual weapon tip farther along the existing ray path.
- It does not alter `rangeRows`, ray target distance, hit timing, damage, or displacement.
- `Zhangfei_Stab.asset` currently uses `2f`; previous memory value `0.5f` was outdated.
- `AttackSystem.stabVisualStartXOffsets` is a five-element world-X visual-only offset array for columns left→right; current first pass `[-1, -0.5, 0, 0.5, 1]`.
- The X offset is applied after the visual's local placement, so the container path and hit calculations remain unchanged.
- Implementation: the StabRay container is oriented before the visual is instantiated; the visual local back offset is reduced by the reach field.

Relevant files: `Assets/Scripts/Core/AttackSkillConfig.cs`, `Assets/Scripts/Attack/StabSweepEffect.cs`, `Assets/Scripts/Player/AttackSystem.cs`, `Assets/Prefabs/UI/Skills/Zhangfei_Stab.asset`.
<!-- locus:body:end -->
