---
id: kd_93075e32-1734-41b2-841e-d892933f617e
injectMode: inherit
summary: Current Item inventory capacity, stacking, potion occupancy, UI slot, and cooldown rules.
aiMaintained: inherit
---

- `HeroConfig.itemSlotCount` defines the total in-run Item capacity for a hero. Zhang Fei is configured with 2.
- Health potions are no longer a locked dedicated UI slot; they occupy an `ItemInventory` entry and compete for capacity.
- Inventory stores slot-aware `ItemEntry` instances. Without stacking, each reward occupies one slot; `useCount` remains uses inside that entry.
- Same-type stacking is selected before a run through `ItemTestHelper.enableSameTypeStacking`; runtime switching is intentionally unsupported.
- Full Item inventory filters incompatible Boss Item-choice candidates via `ItemInventory.CanAdd`.
- Duplicate Cyclone entries share the type-level cooldown owned by `CycloneItemController`; the UI applies that cooldown to every Cyclone entry.
- Column-B displays exactly the hero capacity as persistent slots; empty slots remain visible and non-interactable.
