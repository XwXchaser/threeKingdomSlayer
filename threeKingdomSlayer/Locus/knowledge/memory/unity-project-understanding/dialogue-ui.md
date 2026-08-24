---
id: kd_20797491-db68-45ab-b536-6efcc6f7dca4
injectMode: inherit
summary: Dialogue coordinator, standalone Bubble overlay, art paths, and verified pause/flip isolation.
aiEditMode: inherit
---

# Dialogue UI

- Runtime coordinator: `Assets/Scripts/UI/DialogueManager.cs` subscribes to stage, wave, Boss and kill triggers, queues `DialogueEventData`, controls `DialogueDimOverlay`, and pauses with `Time.timeScale = 0` during an active event. `GetBossProfile(bossId)` resolves the portrait profile from configured dialogue events.
- Runtime presentation: `Assets/Scripts/UI/BattleDialogueView.cs` is on `Assets/Scenes/Battle.scene/BattleHUD(Canvas)/DialogueOverlay`; it owns tutorial/player/Boss bubbles, tails, Boss portrait/frame, advance button and skip button.
- The dialogue overlay is independent from `HeroHUDFlipCard`. Dialogue playback no longer changes QTE visibility or requests a card flip; Boss-combat state remains the only card-side driver.
- Layout: events containing no Boss line use the centered tutorial bubble; mixed events show one left-side player bubble or right-side Boss bubble per active line. Player portrait is intentionally omitted; Boss portrait uses `DialogueEventData.bossProfile`.
- Bubble assets: `Assets/Sprites/UI/Dialogue/dialogue_bubble_9slice.png` and `dialogue_tail.png`. The imported Sprite reports stale geometry despite importer values, so `BattleDialogueView.Awake()` creates the runtime 9-slice from its texture with 16 PPU and 12-pixel border before assigning all bubble/button backgrounds.
- Dialogue layer ordering: base `BattleHUD(Canvas)` and `HeroHUD/HudCard` are sorting order 21; `DialogueLayer` dim is 20; `DialogueOverlay` is 22. Thus regular HUD stays bright, the dialogue bubble stays above it, while `BuffDisplayPanel`, `KillMilestoneDisplay`, and `ChargeIndicator` each use independent order-19 canvases so skill/item and reward icons remain dimmed and blocked. Gameplay interaction is additionally guarded by `DialogueManager.IsInteractionBlocked` in item/ultimate entry points.
- Boss persistent portrait: scene object `BattleHUD(Canvas)/BossPortraitHUD` is configured by `BattleHUD`. On `EnemyManager.OnBossEngaged`, it resolves the matching `BossDialogueProfile`, displays the right-side portrait, and keeps it visible until `OnAnyEnemyDied` reports that same Boss. Portrait frame is optional.
- Validation 2026-07: compilation succeeded; Play Mode verified the runtime bubble has border 12 / PPU 16. Boss portrait show/hide paths were invoked against Boss 104 successfully.
