# QTE V2 Test Stage

## Purpose
- Single-wave, short regression stage for strict-input QTE validation.
- Five front rows contain one Enemy_101 each; Enemy_104 is placed as the final sixth row.

## Deployment
- Asset: `Assets/Resources/StageConfigs/QTE_V2_TestStage.asset`
- Assigned to `StageController.stageConfig` in `Assets/Scenes/Battle.scene`.
- Enemy order is intentionally one unit per row in the center column: rows 0-4 = 101, row 5 = 104.

## Validation
- Clear five Enemy_101 units, then validate Boss 104 strict QTE behavior.
