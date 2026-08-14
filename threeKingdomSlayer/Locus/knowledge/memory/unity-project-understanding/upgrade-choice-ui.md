---
id: kd_d80bbf76-091c-443c-87be-4d853fc77e32
injectMode: inherit
summary: UpgradePopup now uses independent three-choice art components, order 100 overlay, and click-select then click-confirm interaction.
aiMaintained: inherit
---

Three-choice upgrade popup was rebuilt around independent fixed-size art components rather than 9-slicing:

- Art paths: `Assets/Resources/UI/UpgradeChoice/upgrade_choice_outer_frame_v2.png`, `upgrade_choice_card_backing_v1.png`, `upgrade_choice_name_plate_v1.png`, `upgrade_choice_description_box_v1.png`, `upgrade_choice_card_selected_glow_v1.png`.
- `UpgradePopup.prefab` uses overlay Canvas sorting order 100 so it sits above dialogue/pause-tier Battle HUD visuals while active. Its card Image remains the only interactive graphic; art/text subcomponents have raycast disabled.
- Three `UpgradeCard`s are fixed horizontal cards under Content: backing at 250×650, x=-270/0/270. Existing icon frame is retained; NamePlate and DescriptionBox are generated independent Image children so TMP placement remains independent of art.
- `UpgradeCard` stores original anchored position, controls a SelectedGlow child and applies 16px upward selection lift.
- `UpgradeChoicePopup`: first click selects/raises a card; clicking the same selected card calls `UpgradeChoiceManager.ConfirmChoice`. Selecting another card moves the visual selection without applying.
- `ItemDiscardPopup` reuses UpgradePopup but hides `NamePlate`, `DescriptionBox`, and `SelectedGlow`, then applies its independent red/blue card assets and explicit discard layout. This preserves its exchange decision semantics.
- AddTitle no longer depends on the removed Content VerticalLayoutGroup.



### 2026-07 repair: popup prefab isolation
- `UpgradeChoiceManager` now has separate `popupPrefab` (three-choice) and `discardPopupPrefab` fields.
- `Assets/Prefabs/UI/UpgradeChoicePopup.prefab` owns the deep-indigo three-card vertical composition and the click-select/click-confirm flow.
- `Assets/Prefabs/UI/ItemDiscardPopup.prefab` is isolated for the accepted red/blue 1+2 discard composition; it no longer receives three-choice-only NamePlate, DescriptionBox, or SelectedGlow children.
- Both overlay canvases use order 100; only the card root Image/Button accepts raycasts.
- Do not mutate static layout at runtime between these modes. `ItemDiscardPopup` only swaps its own visual/data state within its dedicated prefab.
