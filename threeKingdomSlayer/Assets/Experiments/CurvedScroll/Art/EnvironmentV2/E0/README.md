# E0 Outer Gate Art

This folder is the review and delivery area for the approved E0 outer-gate environment reference and architecture candidates.

## Approved reference

- `References/E0_ApprovedSceneReference_v3.png`
  - Approved E0 scene concept reference.
  - Defines the E0 composition, low stone arch, left flag context, right-side low connector wall, and surrounding spatial roles.
  - RGB scene reference only; not a deployable background and not a Sprite cutout.

## Reference boards

- `References/B01/B01_TargetScope_Review.png`
  - Yellow review annotation showing the proposed B01 right low connector-wall scope.
  - Review aid only; never upload this annotated image as a generation reference.
- `References/B01/B01_ShapeReference.png`
  - Unmarked local context crop for B01 shape planning.
- `References/B01/A03_StoneMaterial_Swatch.png`
  - Small material reference derived from the approved A03 architecture candidate.
- `References/B01/A03_EarthMaterial_Swatch.png`
  - Small earth-surface reference derived from the approved A03 architecture candidate.

## Approved architecture candidates

- `Architecture/e0_a01_stone_arch_front_v1.png`
- `Architecture/e0_a02_ruined_pier_left_v1.png`
- `Architecture/e0_a03_ruined_pier_right_v2.png`

These approved visual assets are now used in the independent composition-preview group in `Assets/Experiments/CurvedScroll/E0OuterGatePreview.unity`. Their PNG bytes and GUIDs are unchanged. A01/A02/A03 visible-footing pivots have been checked and corrected; residual low alpha pixels remain unedited. This is not formal route integration or a completed gate tunnel.

## B01 candidate

- `Candidates/e0_b01_low_wall_right_v1.png`
  - User-approved right-side low connector wall, now used on both sides of the independent E0 composition preview. Left reuse is a layout experiment, not a separately produced left-wall asset.
  - Original RGBA PNG preserved without cropping, padding, resizing or alpha cleanup.
  - Current visible width-to-height ratio is about 2.66, wider than the proposed 1.8 to 2.3.
  - The opaque subject has less than 32 pixels of side padding. Check this before placement.

## Production plan

- `Plans/B01_GenerationPlan.md`
  - Historical snapshot of the bilingual B01 prompts, reference roles, parameters, output records and limitations; its ending note links to the later user-approved composition preview.
  - Canonical execution document: `Locus/knowledge/plan/e0-b01-generation-prompts.md`.
- Raw generation PNG, request and response remain in `C:/Users/Administrator/Pictures/gptGen/e0_batch_b_from_approved_v3/`.

## Saved composition preview

- Scene: `Assets/Experiments/CurvedScroll/E0OuterGatePreview.unity`.
- New group: `E0 Outer Gate Preview/E0 Approved Gate - Composition v1`, enabled.
- Old group: `E0 Outer Gate Preview/E0 Outer Ruined Gate - Front Assembly`, preserved but disabled for comparison.
- Saved and reloaded; Play static and distance-12 approach samples checked. Exit state is Edit Mode with progress 0 and no unsaved scene changes.
- Handoff: `Locus/knowledge/plan/e0-approved-gate-composition-handoff.md`.
- The complete scene composition still awaits user review. Do not call this a completed gate passage or formal N1-to-E0 node.

## Production boundary

- B01 is a separate low horizontal connector-wall Sprite. It must not include the A03 tall pier, large rubble mound, stakes, flag, charred timber, road, sky, or grass.
- Inner gate walls, arch soffit, exit wall, rubble and timber remnants have separate responsibilities and are not implied by this folder's B01 candidate.
- The E0 concept image is a composition reference. Do not crop it directly into a final Sprite.
- Unity scene deployment is intentionally separate from reference archiving and candidate generation.
