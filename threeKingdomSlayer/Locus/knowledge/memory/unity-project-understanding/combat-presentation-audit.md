---
id: kd_d02820b0-cd2f-4913-86fe-52b54e58fcea
type: memory
path: unity-project-understanding/combat-presentation-audit.md
title: combat-presentation-audit
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1785839961062
updatedAt: 1785840723522
---

# combat-presentation-audit

## Summary
Verified audit of current combat presentation: enemy feedback exists, but camera/audio/phase/charge/ultimate/wave/combo/hit-point layers are disconnected; prioritized no-new-art improvements.

<!-- locus:body:start -->
## Presentation audit without new animation assets
- Player-side combat feedback is partially implemented: Enemy hit flash, strength-scaled squash, local Animator hit stop, damage numbers, and AttackWave/Stab/Sweep first-hit pause exist. `HitFeedbackManager` is not connected to outgoing-hit camera response.
- `PlayerHitFeedback` already shakes `Camera.main` on player damage with `DOKill(true)`, so adding separate camera shake callers would conflict unless camera feedback is centralized. It also uses a full-screen white overlay.
- Built-in pipeline has reusable presentation assets: enemy state outline, projectile glow outline, blur shader/CameraManager, wave/cyclone/fire/spike effect sprites, QTE result sprites, victory stamp, existing charge/ready sprites. No dedicated combat camera feedback controller or active combat post-process volume was found.
- Enemy side is stronger than overall presentation: state outlines communicate C-frame/SuperArmor/QTE; Enemy supports hit flash, squash, launch/stun and local hit stop. Gaps include no procedural hit-point burst, no kill confirmation beat, no dedicated boss impact replacement audio, and shared-health linked members can visually react without all sharing hit-stop.
- Enemy hit-scale ownership is currently unsafe: the root Transform is shared by hit scaling, attack `DOScaleX` flip, movement bounce, state cleanup, launch/death and pooling. Several paths call broad `transform.DOKill` / `DOTween.Kill(transform)` or directly assign `originalScale`; an externally killed hit-scale sequence has no `OnKill` normalization. This matches the reported occasional enlarged enemy that never returns to baseline. Before adding more recipient feedback, isolate squash/stretch onto a visual child where possible; minimally add kill-completion restoration and use targeted tween ownership/IDs.
- Current missing or disconnected beats: outgoing-hit camera impulse, projectile launch/impact/deflect audio/VFX, enemy attack release audio, charge-ready cue, ultimate ready/activation/active/end presentation, boss phase visual/audio/camera beat, wave-clear beat, boss-bar threshold markers/trailing damage pulse, combo milestone/break feedback, victory sting, QTE optional success/failure effect hooks.
- Recommended order without new animation art: (1) centralize camera feedback with bounded strength/priority and world-only visual child; (2) connect existing audio and procedural UI/material pulses; (3) add procedural hit-point flash/ring using existing SpriteRenderer/quad and color; (4) repair ultimate-ready existing fire effect and give Berserk startup/end beats using existing sprites/blur/HUD; (5) boss bar phase markers and threshold pulses; (6) wave-clear/boss-entry breathing beats; (7) reduce debug logs after acceptance.
- Avoid global Time.timeScale changes, arbitrary camera random shaking, global sorting-layer overrides, and changing damage/position logic for visual improvements.
<!-- locus:body:end -->
