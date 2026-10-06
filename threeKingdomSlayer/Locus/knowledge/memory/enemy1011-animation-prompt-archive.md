---
id: kd_868f2fe3-973e-45ce-8845-1829bf1be206
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 敌人 1011 动画完整 Prompt 档案

## 使用与维护

本附录保存任务记录内实际提交的英文视频 Prompt，供复现输入与对照姿态使用。摘要见 `memory/enemy1011-animation-pose-reference.md`。原词并非所有约束均已成功执行，状态和结果必须与原词分开解读。

原文按 JSON 解码后的字符串保留，不翻译、不改写拼写、方向词或时序。SHA-256 对该字符串的 UTF-8 字节计算（不是整个 JSON，也不包含 Markdown 围栏）。维护时新增版本，不用优化后的词覆盖已提交历史。临时来源可能被清理，因此正文是持久副本。

视频参数、收费授权与透明处理沿用 `skill/reachapi-seedance-video-generation-v2.md`、`skill/workflows/character-hit-animation-video-workflow.md` 和 `skill/workflows/image-asset-generation.md`；本文件不替代流程规范。

## 1. Idle

- 阶段：正式部署：6 帧循环；Idle speed 0.75。
- 记录来源：`Library/Locus/tmp/sword_enemy_v4_idle_video_v1.runtime.json`
- Task ID：`task_688460292bfc499f9f1f217b9a4c329c`
- Prompt 字符数：2581
- Prompt SHA-256：`71e487777ff360de77b95187d055a6c77d51a7bae05d53320a7329cebe701b07`

```text
Create a 4-second in-place combat-ready idle animation for a game sprite, not a cinematic video. The first and last images are the SAME prepared V4 character frame. Animate this exact character and return naturally to the same ready stance without a pose reset or a pause at the loop boundary.

Match the reference camera angle and framing exactly. Keep the camera locked, the square 1:1 canvas fixed, the character drawing scale unchanged, and both boot contacts and the pelvis anchored. Maintain the original bent-knee, asymmetric combat-ready stance. No camera movement, recentering, resizing, perspective change, walking, foot sliding, weight shifting, or whole-body bobbing.

Perform one gentle, continuous breathing cycle over four seconds. The chest expands slightly during inhalation and settles during exhalation. The shoulders and elbows follow with small overlapping movement. Keep the motion subtle but visibly alive, with the grounded weight of a heavily armored soldier, not exaggerated panting and not a frozen still image.

The anatomical right hand, on the image left, keeps a firm grip on the only sword. The elbow and wrist follow the breathing with very small movement. The sword moves as a rigid object with the hand; its blade, length, shape, and grip stay consistent. No sword swing, flourish, blade bending, trembling, or attack preparation. The anatomical left hand, on the image right, remains empty in its existing relaxed ready shape, with only slight arm follow-through.

Keep the helmet facing the same direction and the gaze alert and steady. The red helmet ribbon tip follows the breathing with a tiny delayed sway and settles naturally within the cycle. No strong wind, dramatic cloth motion, head turn, new gesture, attack, hit reaction, or shield.

Preserve V4's exact armor design, proportions, pixel-block rendering, red and gold colors, brightness, and existing metallic highlights. Do not darken, desaturate, make the armor matte, increase its gloss, or redesign its materials. Keep illumination stable, with no sweeping reflections, sparkling highlights, rim glow, bloom, or texture flicker.

Use the same uniform dark neutral background throughout, with no scenery or floor. No ground shadow, checkerboard, text, UI, particles, or additional objects. Keep the helmet ribbon, sword tip, both hands, both feet, and all armor fully inside the canvas in every frame. Use continuous local joint articulation, not whole-image scaling, morphing, crossfading, or rewinding. The video is a sprite-animation reference; do not create a cinematic shot.
```

## 2. 正面受击 HitFlash

- 阶段：正式部署：6 帧，0.9s 非循环，末帧复用 Idle。
- 记录来源：`Library/Locus/tmp/sword_enemy_hit_front_v2/sword_enemy_hit_front_v2.json`
- Task ID：`task_39f03a1065f5447bb302ab4e81bfe03b`
- Prompt 字符数：3605
- Prompt SHA-256：`6a01296667834351a944c28d51c831979686c7f81308701aaa4b6a73bb8f080b`

```text
This is a game sprite animation for a 2D battle game: a front-facing armored soldier holding a sword, standing in formation, seen straight on. He takes ONE light thrust to the middle of his chest from straight ahead - the attacker and the weapon stay outside the frame. He absorbs the blow, flinches reflexively for a moment while keeping his footing, and settles back into exactly the stance he started in.
    
    Feel and intent: this is a reflexive flinch from an impact, not a voluntary movement, not a pose change and not a stagger. He is a soldier in a mass battle being poked at: he takes the hit, stays standing in his slot, and recovers.
    
    Frame layout: the character stays centred on the same spot of the fixed 1:1 canvas the whole time, with the same drawing scale and the same ground anchor, and at least 15% empty margin on every side. The entire character, sword, hands, legs and boots stay fully inside the canvas in every frame and never touch the border.
    
    Camera: locked orthographic front-facing camera. Never zoom, pan, track, recenter, reframe, rescale, rotate or shake the camera.
    
    In-place animation: no whole-character translation horizontally, vertically, forward or backward; the midpoint between the boots stays fixed; both feet stay planted and never slide or lift.
    
    The first and last frames are the SAME provided idle standing pose: begin in that exact idle pose and end in that exact idle pose.
    
    Timing: the thrust lands within the first 0.05-0.15 seconds, the flinch peaks immediately and holds only very briefly, then the recovery is slower and settles with a small damped stop. Most of the clip is the settled idle stance.
    
    The flinch reads ONLY through joint articulation and part offsets, never through overall size:
    - the shoulder line rises vertically and squeezes inward;
    - the neck shortens so the helmet sinks a little toward the shoulders;
    - the pelvis drops slightly and the knees bend, so the whole figure becomes a little SHORTER than the idle;
    - the upper arms lag and the elbows curl slightly, while both hands keep exactly the same closed grip on the hilt as in the idle pose;
    - the sword angle jolts slightly; its tip moves only a little;
    - the helmet ribbon is flicked up and back.
    At the instant of impact only, the eyes inside the helmet narrow and the brows drop for a couple of frames; then the face returns to the same idle expression. The face geometry, feature positions, sizes and colours must not change at any point.
    
    ABSOLUTE SIZE RULE: at no moment may the character's overall size, volume or silhouette become larger than in the provided idle image. No inflation, no scale-up, no swelling.
    
    CRITICAL AXIS RULE: the vertical axis of the body stays vertical at all times. The character never leans, tilts or tips left or right, never twists and never turns: no three-quarter view, no side view, no mirroring. Only local compression and joint articulation change.
    
    Background: keep the same flat uniform chroma-green background (RGB 0,177,64) with no gradient, no vignette, no shadow, no floor, no scenery and no props; the background colour stays constant across every frame.
    
    Light-to-medium hit: no stagger, no step, no hop, no fall, no knockback of the whole body, no spin, no crouch, no second hit, no recovery flourish. No motion blur, no camera shake, no perspective change, no impact flash, no dust, no speed lines, no text, no UI. Keep the same thick dark pixel outline, hard block shading, colours and pixel density throughout.
```

## 3. Attack

- 阶段：正式部署：18 个对象引用键，2.9s 非循环；实际包含抬剑蓄势，命中键 2.000s。
- 记录来源：`Library/Locus/tmp/sword_enemy_attack_front_v1/record_v2.json`
- Task ID：`task_67c87dbe7385459cb37f16aa958bee7e`
- Prompt 字符数：6551
- Prompt SHA-256：`454b7125960b2e8e642e8aca2b22c34df6eb32182c2dd7b1f74985ec374dd599`

```text
This is a game sprite animation for a 2D battle game: a front-facing armored soldier holding a sword, standing in formation, seen straight on. He attacks on his own initiative: he pulls his sword back to his side, sweeps it forward across the front of his body in one heavy horizontal slash toward the enemy in front of him, and settles back into exactly the standing pose he started in.

    Situation and intent: he is one soldier inside a massed infantry formation pressing forward, and this is his one committed sword attack of the moment. The enemy he is striking stands in front of him, outside the frame, so no second character is visible. He is the attacker here: the movement is willed, heavy and aggressive, driven by his own body, with a fierce focused expression. It is NOT a hit reaction, NOT a flinch, NOT a stagger, and nothing external pushes him.

    Frame layout: the character stays centred on the same spot of the fixed 1:1 canvas the whole time, with the same drawing scale and the same ground anchor, and at least 15% empty margin on every side. The entire character, sword, hands, legs and boots stay fully inside the canvas in every frame and never touch the border.

    Camera: locked orthographic front-facing camera. Never zoom, pan, track, recenter, reframe, rescale, rotate or shake the camera.

    In-place animation: no whole-character translation horizontally, vertically, forward or backward; the midpoint between the boots stays fixed; both feet stay planted and never slide or lift. The attack's forward lunge is not part of this animation: the body does not travel toward the camera.

    The first and last frames are the SAME provided idle standing pose: begin in that exact idle pose and end in that exact idle pose.

    Timing: the draw-back takes the first half of the clip - a slow deliberate pull that ends with the blade held back and held there for a moment; the sweep is fast and happens in a single beat around the middle of the clip; the slash trail appears only during that sweep; the end of the sweep holds for only a couple of frames; the recovery back to the idle stance is slower and settles with a small damped stop. The last part of the clip is the settled idle stance.

    Draw-back (NOT overhead): from the idle stance the sword arm pulls the blade back to his sword side at about shoulder height, with the blade held roughly horizontal and the tip pointing back and slightly up. The hilt comes back beside his shoulder, the elbow bends and pulls behind him, both hands keep the same closed grip on the hilt as in the idle pose, the chest and hips coil away from the sweep, the shoulder line lifts on the sword side, the pelvis settles back and down, the knees load. The sword must NOT be raised above his head at any moment: the blade and its tip always stay at or below the top of his helmet. This is the anticipation of a sideways strike, not of a downward chop.

    Sweep (mostly sideways, toward the front): he drives the blade forward and across the front of his body in one wide sweep. The blade tip travels mostly sideways: its horizontal travel across the frame must be clearly larger than its vertical travel. The sweep ends with the blade in FRONT of the middle of his body: the tip finishes low, close to his centre line, within about 15% of his body width from that centre line, angled down and slightly toward the viewer, so the blade looks foreshortened and shorter than it did at the start of the sweep. The sword arm crosses in front of his chest at the end of the sweep and partly covers the chest armour; the hand ends on the opposite side of his body from where the blade started.

    Driving forward: the whole body commits toward the viewer at the end of the sweep - the torso pitches forward, both shoulders roll forward and drop toward the viewer, the pelvis pushes forward, the back leg straightens while the front knee bends, and the head keeps facing the camera and lowers slightly. This forward commitment is what makes the strike read as an attack toward the enemy in front, not as a chop at the ground.

    Slash trail: exactly one bright slash trail follows the arc of the blade, and only during the sweep. It is a thin, clean, hard-edged crescent that travels mostly sideways across the front of his body, drawn in the same bright highlight colours, the same hard block shading and the same thick dark pixel outline as the rest of the art, with no soft glow. Keep the trail compact: its horizontal span covers no more than about 60% of the canvas width and its vertical span no more than about 40% of the canvas height. The trail stays above the boots and never covers the boots, the head, the face or the helmet, and it never reaches the ground line. It is a crescent that follows the blade: never a screen-wide band, never a full-canvas smear, never covering the whole frame. In the first and last frames there is no trail at all.

    ABSOLUTE CONTAINMENT RULE: at every single frame, including the exact frame where the trail is widest, the whole composite - character, sword and slash trail together - stays completely inside the canvas with at least 15% empty margin on all four sides. Nothing is cropped, cut off, clipped, truncated or allowed to cross or touch any canvas border. No part of the sword, the trail or any effect may extend outside the frame or be cut by the edge of the image.

    BODY SCALE RULE: the head, torso, arms, legs, sword and armor keep exactly the same drawing size as in the provided idle image; only their angles and positions change. Never scale the character up or down, never inflate the silhouette.

    CRITICAL AXIS RULE: the vertical axis of the body stays vertical at all times. The character never leans, tilts or tips left or right, never twists away from the camera and never turns: no three-quarter view, no side view, no mirroring. Only joint articulation and the coiling described above change the pose.

    Background: keep the same flat uniform chroma-green background (RGB 0,177,64) with no gradient, no vignette, no shadow, no floor, no scenery and no props; the background colour stays constant across every frame. The slash trail is the only added effect: no impact flash, no sparks, no dust, no debris, no shockwave, no speed lines, no motion blur, no lens flare, no screen-wide brightness change.

    No second character, no second weapon, no text, no UI, no camera shake, no perspective change. Keep the same thick dark pixel outline, hard block shading, colours and pixel density throughout.
```

## 4. Dead

- 阶段：正式部署：6 帧，0.6s 非循环，末姿势保持。
- 记录来源：`Library/Locus/tmp/sword_enemy_dead_v1/record_v4.json`
- Task ID：`task_e3d84ebe3403436192b5c1d3697488aa`
- Prompt 字符数：5607
- Prompt SHA-256：`8d9da25cff15f4886b186045a702e49bdc6b6f9f6a50a5f37d43a0f4103508ca`

```text
This is a game sprite animation for a 2D battle game: a front-facing armored soldier holding a sword, standing in formation, seen straight on. A heavy blow from straight ahead hammers him off his feet: he is jolted upward, his limbs are flung by the impact, the sword is knocked out of his hands, and his body tumbles in the air as a limp corpse. He never comes to rest and never lies down.
    
        Situation and intent: he is one soldier inside a massed infantry formation and he has just been hit by a killing blow from straight ahead - the attacker and the weapon stay outside the frame. This is a violent, instantaneous death by force: the strike is heavy and fast and it knocks him off his feet. It is NOT his own attack, NOT a light flinch, NOT a stagger, NOT a slow sag, NOT a quiet collapse, and NOT a body lying down on a floor. The feeling is brutal impact: one blow ends his life and leaves him tumbling in the air.
    
        ABSOLUTE CONTAINMENT RULE - HIGHEST PRIORITY, OVERRIDES EVERY OTHER INSTRUCTION: in every single frame, including the instant of the fling and the widest moment of the tumble, the whole composite - boots, knees, hips, arms, hands, sword, helmet and ribbon - keeps at least 8 percent empty margin on all four sides of the fixed 1:1 canvas. Nothing may touch, cross, overhang or be cut off by any canvas edge, and nothing may leave the frame. If a pose would reach an edge, make that pose more compact instead - never crop it, never let a limb pass the border.
    
        STAY-CENTRED RULE: the figure's centre of mass stays near the middle of the canvas in every frame, in the same central safe area that the provided standing pose occupies. He never drifts up, left, right or forward away from the centre and he never slides. This is not a jump out of frame: the body stays in its own slot.
    
        SMALL LIFT RULE: the sprite itself only needs to show that the boots have left the ground, so the figure rises by no more than about 8 to 12 percent of its own height (roughly half a world unit) at the peak. Do NOT launch the body high into the air inside the sprite: the big flight and the spin are produced by the game engine afterwards, not by this animation.
    
        LIMB RULE: the arms and legs are flung out to the sides and slightly forward by the impact; they never reach above the top area that the standing helmet occupied, and the legs never kick straight up past the head.
    
        Camera: locked orthographic front-facing camera. Never zoom, pan, track, recenter, reframe, rescale, rotate or shake the camera.
    
        The first frame is the provided standing idle pose. The last frame is the provided airborne tumbling corpse pose, which the animation must arrive at exactly. The engine continues the flight and the spin afterwards, so the sprite must read as a body in the air, never as a body lying on the ground.
    
        Timing: the blow lands at once - the very first 0.15 seconds already contain the whole impact, not a build-up and not a slow sag; the peak of the blow (body arched, limbs flung, boots off the ground) is reached within the first 0.4-0.9 seconds; after that he tumbles and opens out into the provided airborne pose; the last part of the clip is the held airborne pose with only slight drift, and no movement that would read as landing.
    
        Impact reads - force, not limpness: the torso jack-knifes backward with the chest pushed up and the spine arched; both arms are thrown out and up by inertia with the hands opening; both legs are flung with the knees folding; the sword is knocked out of his grip, swings away from his body and never returns to his hand; the helmet ribbon is whipped.
    
        HEAD RULE - the most important read: the moment the blow lands his head is thrown back and to one side. From that instant he NEVER looks at the camera again: the neck bends, the chin points up and away, the helmet's face opening rotates away from the viewer so the face is no longer visible, and the helmet slides back against his shoulder. The head stays thrown back and to the side for the rest of the clip - it must never return to facing the viewer, and it must not stay upright and looking straight ahead while the body tumbles.
    
        Then the body reads as dead weight in the air: the limbs stop resisting and hang loose, the spine uncurls, the legs and arms trail, and the figure opens out to about 1.4 to 1.9 times wider than tall as it tumbles.
    
        BODY SCALE RULE: the head, torso, arms, legs and sword keep exactly the same drawing size as in the provided idle image; only their angles and positions change. Never scale the character up or down, never inflate the silhouette and never stretch the limbs.
    
        NO ROTATION RULE: do not rotate or tumble the figure as a whole inside the sprite and never change the camera: the hit, the fling and the tumbling pose all happen in the frame plane, seen straight on. No flipping of the whole figure, no spinning, no rolling of the whole body, no three-quarter view, no side view, no mirroring.
    
        Background: keep the same flat uniform chroma-green background, identical in every frame - no gradient, no vignette, no ground shadow, no floor, no scenery, no props. No blood, no wound spray, no sparks, no dust, no impact flash, no speed lines, no motion blur, no lens flare.
    
        No second character, no weapon flying in from off-frame, no text, no UI, no camera shake, no perspective change. Keep the same thick dark pixel outline, hard block shading, colours and pixel density throughout.
    
```

## 5. Walk（原地冲锋跑位）

- 阶段：正式部署：6 帧，0.6s 循环；语义是冲锋跑，不是慢走。
- 记录来源：`Library/Locus/tmp/sword_enemy_walk_v1/record_v4.json`
- Task ID：`task_18e87a74532b4f1f86161a1b00b318cf`
- Prompt 字符数：7279
- Prompt SHA-256：`45d4410e2024be689a228bc94cc5500931fb4de47dffe99738e9f4c369755234`

```text
This is a game sprite animation for a 2D battle game: a front-facing armored soldier holding a sword, standing in formation, seen straight on. He is running on the spot, driving forward at speed as part of the formation's charge: his boots pound the ground, his knees drive up, his bent arms pump, and his whole body is committed to moving forward.

Situation and intent: he is one soldier inside a massed infantry formation charging the enemy line, and this is his running stride, repeated over and over. He is NOT walking, NOT marching at ease, NOT strolling and NOT swaggering: this is a fast driving run, the last strides of a charge, with real effort in every step. He is not attacking, not receiving a hit, not flinching, not bracing and not dying, and no second character is involved. The forward travel does not happen inside the sprite - the game engine moves him forward - so his job here is only to keep running on the spot. It must read as running, never as a relaxed walk.

ABSOLUTE CONTAINMENT RULE - HIGHEST PRIORITY, OVERRIDES EVERY OTHER INSTRUCTION: in every single frame, including the widest and the highest moment of the run, the whole composite - helmet, head, shoulders, arms, hands, sword, torso, legs and boots - keeps at least 8 percent empty margin on all four sides of the fixed 1:1 canvas and stays inside the same area of the canvas that the provided pose occupies. Nothing may touch, cross, overhang or be cut off by any canvas edge, and nothing may leave the frame. If a pose would reach an edge, make that pose more compact instead - never crop it and never let a limb or the sword pass the border. The sword must never be carried anywhere near the top of the canvas.

Frame layout: the character stays centred on the same spot of the fixed 1:1 canvas the whole time, with the same drawing scale and the same ground anchor. The entire character, sword, hands, legs and boots stay fully inside the canvas in every frame and never touch the border.

Camera: locked orthographic front-facing camera. Never zoom, pan, track, recenter, reframe, rescale, rotate or shake the camera.

In-place animation: no whole-character translation horizontally, vertically, forward or backward; the midpoint between the boots stays fixed; the figure never drifts up, down, left or right and never slides across the canvas. Only local joint articulation changes.

HEIGHT AND GROUND RULE: the top of the helmet stays at about the same height in the canvas as in the provided pose in every frame, with only a small bob of a few percent; the lowest visible pixel of the boots also stays at the same height in the canvas as in the provided pose, within a few pixels, in every frame. The figure never grows taller, never stretches, never leaves its slot and never sinks into the ground. The pelvis may rise and fall with each stride, coming from the legs driving and bending rather than from the whole body being shifted as a block.

The first and last frames are the SAME provided idle standing pose: begin in that exact idle pose and end in that exact idle pose. That stance is the moment in the stride where the boots come together, so no wind-up, no settle and no pause is needed at either end: the run is already at full pace one frame after the start and is still at full pace one frame before the end.

Timing: a fast driving run: one complete stride cycle - one boot drives down, then the other boot drives down - takes about 0.4 to 0.5 seconds, and the clip contains at least eight identical cycles of the same length at a constant even pace. Never accelerate, never slow down, never stop, never hold a pose. Do not spend the first part of the clip standing still, and do not end with a slow settling walk.

Run read - the legs drive: as one boot drives down, its sole lands flat under the body and that leg straightens hard and takes the weight; the other leg drives its knee up and forward, the knee rising high - about 18 to 25 percent of the character's height - with the boot clearly leaving the ground and the lower leg angled back under it. The strides are long and quick, the boots never slide sideways along the ground, the boots never swap sides, and the two feet are almost never both planted flat at the same moment.

Arm pump - the strongest read that this is a run and not a walk: the elbows stay bent at roughly a right angle the whole time; they never straighten out into a loose hanging swing and never swing out wide to the sides like a stroll. The hands pump: with each stride one hand drives up to about chest height and slightly forward in front of the body, while the other hand drives back and down to about hip height behind the body; then they swap. The hands travel visibly up and down with every stride and never freeze in one position. The sword is carried in the same hand with that elbow bent: the blade is held back alongside the body, angled back and down, and it only follows the pumping arm by a small amount, about 15 to 25 degrees of angle change over a cycle. The blade must never rise above the shoulder line, never be raised overhead or behind the head, never point toward the camera, never sweep across the chest, and never reach further out to the sides than it does in the provided pose.

Body and shoulders - driving forward: the shoulders lift and come a little forward with the effort, the chest is driven forward into the pace and the neck compresses slightly, so the head sits a little lower and slightly forward as he drives on - reading as a man running into the fight, not as a man out for a stroll. The shoulders and the torso counter-rotate with the arm pump. The armour skirt and the helmet ribbon lift and flap with the pace. The head still faces the camera and the face does not change expression: he keeps the same fierce look as in the provided pose.

BODY SCALE RULE: the head, torso, arms, legs, sword and armour keep exactly the same drawing size as in the provided idle image, and the standing height of the figure never changes; only the angles and positions of the limbs change. Never scale the character up or down, never inflate the silhouette, never make him taller, shorter or wider than the provided pose, and never stretch the limbs.

CRITICAL AXIS RULE: the vertical axis of the body stays vertical at all times. The character never leans, tilts or tips left or right, never turns the whole body and never turns his feet: no three-quarter view, no side view, no profile, no mirroring and no turning to run in another direction. The shoulders may roll and counter-rotate and the torso may twist a little with the arm pump, but the body as a whole always faces the camera. Only joint articulation changes.

Background: keep the same flat uniform chroma-green background (RGB 0,177,64) with no gradient, no vignette, no shadow, no floor, no scenery and no props; the background colour stays constant across every frame. No effects of any kind: no dust, no ground impact, no footprints, no speed lines, no motion blur, no flash, no sparks, no debris, no lens flare and no screen-wide brightness change.

No second character, no second weapon, no text, no UI, no camera shake, no perspective change. Keep the same thick dark pixel outline, hard block shading, colours and pixel density throughout.
```

## 6. 左受击 HitLeft v3

- 阶段：视频/选帧/透明素材已验收，未部署。原视频峰值冻结约 1.21s；成功可用性来自剔除平台并重排选帧。
- 记录来源：`Library/Locus/tmp/sword_enemy_hit_left_v3/record.json`
- Task ID：`task_3992f2cba10041af902a0aff47a43e05`
- Prompt 字符数：6970
- Prompt SHA-256：`40646db783c6cde48e4d09172435d8c753d2c7fdba75d0922158710c10edfdf0`

```text
This is a 2D game sprite animation for a mass-battle scene, not a cinematic video. A front-facing armored soldier in a helmet and armour holds a sword and stands in his slot in formation, seen straight on. The same idle image is supplied as BOTH the first frame and the last frame: begin on that exact idle pose and end on that exact idle pose.

THE EVENT:
At the very start he is simply standing in his idle pose. Then, with no warning and no anticipation, one heavy invisible blow arrives from OUT OF FRAME AT THE LEFT EDGE OF THE SCREEN and lands on his screen-left shoulder and upper screen-left chest. The force travels from screen-left to screen-right. The attacker, the weapon and the impact flash are all outside the frame and never appear. There is exactly ONE hit in the whole clip: no second impact.

FEEL AND INTENT:
This is a sudden violent impact travelling through his body - not a voluntary movement, not a pose change, not a relaxed side-turn, not a guard and not a knockback. He is a soldier in a mass battle who is smashed from his screen-left flank and stays on his feet in his slot. The blow must feel heavy: the reaction has to read instantly even from a single peak frame, and anyone looking only at the peak frame must immediately understand that the force came from the LEFT of the screen and threw him toward the RIGHT of the screen.

DIRECTION READ (must be unmistakable):
- the screen-left shoulder is driven DOWN and BACKWARD by the blow;
- the screen-right shoulder is knocked UP and slightly forward, so the shoulder line tilts steeply - a height difference of about 40 to 55 pixels between the two shoulders;
- the upper torso is pushed toward screen-right and opens, arching backward and outward away from the blow, with the chest expanding and the ribs on the impact side compressed;
- the head does NOT snap with the torso: it lags by one or two frames, then is whipped toward screen-right and slightly backward, with the chin lifted a little and the visor briefly turned away from the camera; then the neck and head swing back to the exact front-facing idle head;
- the arms and the sword trail behind the torso with passive inertia;
- the helmet ribbon snaps after the body and settles last.

CAUSE AND EFFECT:
The parts move one after the other, overlapping: torso first, head a beat later, arms and sword a beat after that, ribbon last. Do not move every part at the same instant and do not move them as isolated robotic steps.

TIMING (fast hit, slower recovery, never a frozen pose):
- Opening: a very short moment of the untouched idle pose - roughly the first quarter of a second - with no wind-up, no brace and no guard.
- The blow then lands abruptly and the reaction explodes within about two to four frames.
- The impact extreme is reached early and is held for only about one to three frames. It must never be held for seconds.
- A fast rebound follows: the chest and shoulder block come back over the hips first, the head follows, then the arms.
- Then one clearly smaller damped rebound - the second oscillation is much smaller than the first.
- The motion changes continuously in every frame of the reaction: no newly invented pose is held flat, no long plateau, no freeze, no still frame inside the reaction.
- Afterwards he is back in the idle stance for the whole final part of the clip, at least the last third, and the very last frames are perfectly still and identical to the first frame.

QUANTITATIVE LIMITS (measured on the fixed canvas):
- the helmet must not drop more than about 25 pixels below its idle height: he is not ducking and not crouching;
- the head must not slide sideways more than about 60 pixels from its idle position;
- the whole silhouette must not grow: no part may extend more than about 40 pixels further left or further right than in the idle image;
- shoulder-line height difference at the peak: about 40 to 55 pixels;
- head turn at the peak: about 35 degrees, chin over the screen-right shoulder, visor clearly no longer facing the camera;
- the torso arch is a rotation plus a compression, not a slide: the hips stay above the boots.

HARD CONSTRAINTS:
- Fixed 1:1 canvas, locked orthographic front-facing camera, identical framing, identical drawing scale, identical ground anchor and identical pixel density from the first frame to the last. Never zoom, pan, track, recenter, reframe, rescale, rotate or shake the camera.
- Both boots stay planted on exactly the same pixel row and column for the entire clip: no stepping, sliding, hopping, lifting, kneeling or turning of the feet.
- No translation of the character across the canvas: he neither advances, retreats nor drifts sideways, and his silhouette stays on the same spot of the canvas.
- ABSOLUTE SIZE RULE: at no moment may his overall size, volume or silhouette become larger than in the idle image, and it must never pulse, inflate, swell, shrink, squash or stretch. The impact must never be expressed as a change of scale.
- The body never tips or leans as a whole: no whole-body left or right lean, no tilt of the vertical axis, no twist, no three-quarter view, no side view and no mirroring. The asymmetry lives only in joint angles and part offsets.
- Never turn the whole body away from the camera. Only the head has a brief local turn, and the shoulders and chest do not rotate with the head.
- SWORD RULE: the sword keeps its idle side, idle length, idle grip and idle general angle at all times. It never rises above the shoulder line, never sweeps, never crosses the body, never changes sides, never changes length and never becomes a guard or an attack. Only a small passive jolt of the blade is allowed.
- The face: at the moment of impact only, the eyes inside the helmet narrow for a few frames; the face geometry, feature positions, sizes and colours must not change at any point.
- The entire character - helmet, head, arms, hands, legs, boots, sword and ribbon - stays fully inside the canvas in every frame with at least 15 percent empty margin on every side, and nothing ever touches an edge.
- Background: keep the same flat uniform chroma-green background (RGB 0,177,64) in every frame, with no gradient, no vignette, no shadow, no floor, no scenery and no props.
- Keep the same thick dark pixel outline, hard block shading, limited colour steps, palette and pixel density throughout.

NEGATIVE LIST:
no second hit, no repeated blows, no attacker or weapon entering the frame, no impact flash, no blood, no dust, no smoke, no speed lines, no debris, no shockwave ring, no camera shake, no motion blur, no perspective change, no cuts, no transitions, no crossfade, no morph, no stagger step, no hop, no fall, no knockback, no slide, no spin, no crouch, no kneel, no ground contact, no weapon raise, no arm swing, no guard change, no combat stance, no casual side-turn or side glance, no looking at something, no dance move, no posing for the camera, no text, no UI, no watermark, no extra character.
```

## 7. 右受击 HitRight v1

- 阶段：视频已验收；6 帧候选待验收，未透明处理或部署。中段无长冻结平台，但剑侧外扩、飘带越界与 f018 闪帧不符合全部原词目标。
- 记录来源：`Library/Locus/tmp/sword_enemy_hit_right_v1/record.json`
- Task ID：`task_71ccbe537c0548d9af0b9a5c0dea6613`
- Prompt 字符数：8223
- Prompt SHA-256：`bd5493599439b53d7898ce11e6932f1baeab883a247c898087abf95d60e3cea3`

```text
This is a 2D game sprite animation for a mass-battle scene, not a cinematic video. A front-facing armored sword soldier stands in formation, seen straight on. The same accepted idle image is supplied as BOTH the first frame and the last frame. Start on that exact idle drawing and finish on that exact idle drawing. This is one continuous in-place hit reaction, not a cut between poses.

THE SINGLE IMPACT:
After a very short untouched idle opening, one sudden heavy invisible blunt hit arrives from OUT OF FRAME AT THE RIGHT EDGE OF THE SCREEN. It strikes the soldier's SCREEN-RIGHT shoulder and upper screen-right chest - the side without the sword. The force travels from screen-right to screen-left and throws the upper body toward screen-left and slightly away from the camera. The attacker, weapon and impact effect remain outside the frame. There is exactly one impact and no anticipation, wind-up or second hit.

READ THE CAUSE, NOT A POSE:
This must look like an external blow causing a brief loss of balance that is recovered in place. It is not a voluntary turn, a casual side glance, a guard, a dance, a stagger step or a newly chosen combat pose. The planted boots and pelvis are the anchor; the chest, shoulders, neck, head, arms and ribbon react around that anchor. The peak must immediately communicate: force from SCREEN-RIGHT, body thrown toward SCREEN-LEFT.

RIGHT-HIT BODY MECHANICS:
- The screen-right impact shoulder is compressed DOWN and slightly BACK by the blow. It drops and lags behind the chest instead of rising.
- The screen-left shoulder is kicked UP and slightly FORWARD. This creates a clear asymmetric shoulder line with about 40-55 pixels of height difference at the peak.
- The upper chest and shoulder block recoil toward screen-left and slightly backward around the fixed pelvis. The sternum opens and lifts from the impact; do not fold the whole torso forward, bow, duck or curl into the ribs.
- The belt buckle, pelvis center and midpoint between the boots remain nearly fixed. Allow local upper-torso articulation and a short leftward recoil, but do not translate the root or slide the entire character.
- The head follows one or two frames late. First the torso reacts; then the neck compresses and the helmet is whipped toward screen-left and slightly backward by inertia. The chin lifts slightly, the visor turns clearly away from the camera for a moment, and then the head swings back to the exact front-facing idle head. This is a delayed head whip, not a calm side look and not a low duck.
- The free screen-right arm trails after the torso with a small delayed elbow and wrist response; it does not deliberately guard, cross the chest or attack. The sword arm may follow the torso by a small passive amount so it does not freeze, but it must not swing.
- The ribbon reacts last with a short leftward snap and damped settling. Keep all parts connected to the same soldier.

SWORD-SIDE CONTINUITY - HIGHEST PRIORITY:
In the supplied idle image the sword and gripping arm are on the SCREEN-LEFT side. The blade already points up and outward toward screen-left, and its idle tip may already be above the shoulder line. Preserve this exact sword side, identity, length, grip relationship and general idle direction. Do not mirror the character or move the sword to screen-right.

The sword is allowed only a small passive jolt caused by the arm: its tip may move no more than about 30-40 source pixels from its idle tip position. It may settle a few pixels late with the wrist, but it must not make a new swing arc, rise farther than its idle envelope, cross the body, point at the camera, change length or become a guard/attack pose. Keep the sword mostly in its original screen-left lane while the non-sword shoulder and torso carry the main hit reaction.

OVERLAPPING ACTION PATH:
Use overlapping continuous motion, not isolated pose changes: torso and impact shoulder start first; head and neck follow one or two frames later; the free arm and sword arm follow with a smaller delay; sword and wrists settle after the arms; ribbon settles last. Do not move every part simultaneously. Do not jump directly from idle to a finished side pose. Do not hold any intermediate pose as a still illustration.

TIMING - FAST IMPACT, LONGER RECOVERY:
- 0.00-0.20s: exact idle, only a very short opening; no preparation.
- 0.20-0.38s: the hit arrives abruptly; torso and shoulders accelerate toward screen-left within 2-4 frames.
- 0.38-0.55s: pass through one strong peak; the peak exists for only 1-3 frames. Keep tiny head, arm, wrist and ribbon motion even around the peak.
- 0.55-1.20s: continuous recovery; chest and shoulders return first, head remains briefly delayed, then the arms and sword settle.
- 1.20-1.55s: one visibly smaller damped rebound, much smaller than the first; do not create a second pose plateau.
- 1.55-4.00s: exact idle construction restored and held. The final third is stable idle, and the last frames match the first frame.
- During the entire reaction, no newly invented pose may remain visually unchanged for more than about 3 frames. Every reaction phase must keep changing.

QUANTITATIVE READ POINTS ON THE 960x960 CANVAS:
- shoulder height difference at peak: 40-55 pixels, with screen-right lower and screen-left higher;
- upper-torso recoil toward screen-left: approximately 35-60 pixels relative to the idle chest, while the pelvis/belt/boot midpoint moves no more than about 10 pixels;
- helmet top stays within about 25 pixels below its idle height; no crouch or head drop;
- head horizontal displacement stays within about 60 pixels from idle;
- head turn at peak is about 30-35 degrees toward screen-left, chin slightly raised, visor no longer front-facing;
- sword tip displacement from its idle tip: no more than 30-40 pixels;
- no silhouette expansion beyond the idle envelope by more than about 40 pixels toward screen-left or 60 pixels toward screen-right;
- both boot contacts remain on the exact idle pixel row and in the exact idle columns.

HARD SPRITE CONSTRAINTS:
- Fixed 1:1 canvas, locked orthographic front-facing camera, identical framing, drawing scale, ground anchor and pixel density from first to last. Never zoom, pan, track, recenter, reframe, rescale, rotate or shake the camera.
- Keep the root, pelvis and both boot contacts in place. No whole-character horizontal or vertical translation, no approach, retreat, slide, hop, step, lift, kneel, fall or launch.
- Do not scale, inflate, swell, shrink, squash or stretch the drawing. Articulation changes the silhouette; camera or sprite scale never changes.
- Keep the soldier front-readable. The root and chest do not become a three-quarter or side-view character, and do not mirror the image. A local head turn and an asymmetric shoulder line are allowed and required.
- Preserve the helmet, armour, hands, boots, sword, ribbon, colours, hard pixel outline, block shading, highlights and pixel density. Do not redraw or add details.
- The face geometry, feature positions, sizes and colours stay fixed. Only a brief narrowed-eye impact expression is allowed.
- Keep every part fully inside the canvas with at least 15 percent empty margin. If the local recoil approaches an edge, make the articulation more compact; never solve it with a camera zoom.
- Keep the flat uniform chroma-green background RGB (0,177,64) unchanged in every frame: no gradient, floor, shadow, scenery or props.

NEGATIVE LIST:
no second hit, no repeated blow, no attacker, no entering weapon, no impact flash, no blood, no dust, no smoke, no debris, no speed lines, no shockwave, no camera shake, no motion blur, no perspective change, no cut, no crossfade, no morph, no jump-cut, no frozen intermediate pose, no long plateau, no whole-body knockback, no root translation, no step, no hop, no fall, no crouch, no kneel, no duck, no spin, no full-body lean, no three-quarter body turn, no side view, no mirroring, no casual side glance, no deliberate guard, no cross-body arm pose, no sword attack, no sword sweep, no sword raise beyond its idle envelope, no sword side switch, no weapon length change, no scale pulse, no inflation, no repainting, no text, no UI, no watermark, no extra character.
```

## 8. 左受击关键图 v6：仅调整头部

- 来源：`Library/Locus/tmp/sword_enemy_hit_left_keypose_v6/prompt_keypose_left_v6.txt`
- 类型：单帧编辑原词，不是视频 Prompt。
- 状态：v3 身体与 v5 转头方向分别获认可；v6 将两者结合，用户评价后仰幅度仍稍差一点，不应归类为完全理想峰值。
- 方向措辞保持历史原文，包括 his left/right；新 Prompt 应按主文档使用 SCREEN-LEFT / SCREEN-RIGHT 消除歧义。
- Prompt 字符数：2433
- Prompt SHA-256：`f3fa75bb1fd0433e2efe7b94d52c7ff700700761dbab6d6ca394d28e24b6df42`

```text
Take the provided game character sprite and change ONLY the orientation of his head. Everything else in the provided image is already approved and must stay EXACTLY as it is. This is a single still sprite frame, not a scene.

KEEP PIXEL-IDENTICAL - do not touch: the body pose and the lean of the chest, the shoulder line, both arms and hands and their exact positions, the sword (same length, same angle, same position), the legs, both boots on exactly the same pixel row and column, the overall scale of the figure, its position on the canvas, the canvas size, the framing, the flat chroma-green background, the colours, the pixel density and the thick dark outline.

THE ONLY CHANGE - the head: in the provided image the helmet has only started to turn; the head must now be turned MUCH further toward the RIGHT of the screen, as if a hard blow from his left had thrown his head toward his right shoulder. Turn the head by roughly 35 degrees, up to at most 40 degrees - clearly more than in the provided image - so that:

- the chin is over his right shoulder and the face plane points toward the right of the screen: the helmet is obviously NOT facing the camera any more, and the visor is seen at a clear angle;
- the far side of the helmet - his LEFT side, the side the blow comes from - turns away from the camera and becomes partly hidden behind the head, so the helmet silhouette is visibly narrower on that side;
- the head is also driven a little DOWN and AWAY into the shoulders, so the neck looks shorter and the helmet sits lower than in the provided image;
- the face keeps its own drawn size and shape: the eyes, brows, mouth and visor keep exactly their shape, size and colours and simply rotate together with the head; the head must not become bigger or smaller, and must not be redrawn in another style.

Rotate the head on the neck only. The neck stays where it is on the shoulders and the shoulders themselves must not move: no change of the torso, no change of the lean, no change of the shoulder line, no change of the arms, no change of the sword, no change of the legs or the feet, no change of the body silhouette, no change of the scale of the drawing, no shift of the figure on the canvas, no redraw of the armour and no change of the background. Do not add any expression, sweat, tear, grimace, impact flash, dust, speed line, text or UI. Keep the same flat chroma-green background and the same framing.

```

## 9–11. 新增击飞链 Prompt（Rise / Landing / Getup）

以下三段属于同一条击飞表现链，但按独立端点和运行时职责归档。三段原词均逐字保存；视频生成使用 4s 容器，不能把视频时长直接当作 Unity Clip 时长。三阶段 18 帧选帧、抠图和导入状态见主文档。

### 9. Launch Rise v1

- 阶段：已验收：视频、6 帧选帧和 6 张本地透明素材；未部署。
- 记录来源：`Library/Locus/tmp/sword_enemy_launch_v1/record_rise.json`
- Prompt 来源：`Library/Locus/tmp/sword_enemy_launch_v1/prompt_rise.txt`
- Task ID：`task_1c5d1f90fb0549f2b649110eda38208d`
- Prompt 字符数：5563
- Prompt SHA-256：`b1ffac236d8cb81cbba6ab4a3234502f71aba2a9e91784363240d0076f2306d9`

```text
This is a 2D game sprite animation for a mass-battle game, not a cinematic video. Animate the exact accepted 1011 front-facing armored sword soldier from the supplied identity and idle references. He stands in a formation slot with a silver-gray helmet, red helmet ribbon, dark gray metal armor, brown greaves, gold belt, red cloth and one sword held on the SCREEN-LEFT side. Preserve this exact character identity, proportions, pixel-art rendering, palette, hard block shading, thick dark outline and pixel density.

ACTION AND CAUSE:
A single powerful external launch hit from below and slightly in front suddenly lifts him off the ground. This is a passive knock-up, not a voluntary jump, not an attack, not a dodge and not a dramatic death. The force catches the pelvis and lower torso first, then the chest, head, arms, legs, sword and ribbon follow with inertia. He has just been struck by a launch attack and is losing support.

START AND END:
Start in the exact accepted idle standing pose for only a very short instant. Then the launch happens abruptly. End at the upper-air / early-apex airborne pose: both boots are clearly off the ground, the body is still rising or has just reached the end of the upward impulse, and the pose is ready to continue into the separate airborne Fall animation. Do not show falling to the ground, landing, crouching, standing back up or returning to Idle in this clip.

BODY READ - PASSIVE KNOCK-UP:
- The pelvis and lower torso are lifted first while the planted stance breaks. Both knees release and both boots leave the ground together; no walking step and no deliberate jump takeoff.
- The chest opens upward and slightly backward from the upward force. The abdomen stretches between the lifted pelvis and the thrown-back chest; do not fold forward into a crouch.
- The head follows the chest with a short delay, then the neck compresses and the helmet tips backward and slightly to one side. The visor is no longer calmly square to the camera; the face reads shocked and involuntarily thrown back.
- The arms are pulled outward and slightly upward by inertia. The free hand opens. The sword hand loosens and the sword begins to separate from the grip, but the sword remains a complete visible object close to the SCREEN-LEFT hand and inside the frame.
- The legs trail below and outward with bent knees and relaxed ankles. They are airborne, not planted, not running and not kicking straight above the head.
- The red ribbon whips after the head and settles into the airborne motion last.

PEAK POSE LIMITS ON THE 960x960 CANVAS:
- The sprite only needs to show that the boots have left the ground: the character's visible body rises no more than about 8-12 percent of his idle height inside the canvas. The game's root Y motion creates the large flight; do not move the sprite high up in the canvas.
- Keep the character's center near the same central safe area as the idle image. No whole-character drift toward any canvas edge.
- The head may move within a local airborne recoil of about 60 pixels from idle, but never leaves the safe area.
- Arms and legs may open by joint articulation, but the complete silhouette stays at least 15 percent away from every canvas edge.
- Keep the sword on the SCREEN-LEFT side. It may jolt and begin a short passive separation from the hand, but it never mirrors, changes side, bends, lengthens, disappears or exits the frame.

OVERLAPPING TIMING:
- 0.00-0.12s: exact idle, only a very short hold.
- 0.12-0.28s: abrupt external launch impulse; pelvis and torso lift first, boots release.
- 0.28-0.45s: chest and head are thrown back; arms, legs and sword follow with visible lag.
- 0.45-0.55s: reach one readable upper-air / early-apex pose for only a few frames. This is the terminal Rise pose, not a landing pose.
- Keep the reaction continuously changing during the launch. Do not freeze a new pose for seconds and do not insert a long platform.

HARD SPRITE CONSTRAINTS:
- Fixed 1:1 canvas, locked orthographic front-facing camera, identical framing, drawing scale, pixel density and character identity. Never zoom, pan, track, recenter, reframe, rotate or shake the camera.
- No floor, ground, scenery, shadow, buildings or lighting setup. Use the same uniform flat chroma-green background RGB (0,177,64) in every frame.
- No root translation inside the sprite, no scale pulse, no inflation, no shrinking, no squash-and-stretch of the whole drawing. Only local joint articulation expresses the airborne reaction.
- Keep the complete helmet, ribbon, armor, both hands, both boots, sword and all accessories inside the frame. If the pose approaches an edge, compact the limbs; never crop or solve it with a camera zoom.
- Preserve the exact 1011 equipment: sword on SCREEN-LEFT, same grip side at the beginning, same sword identity, no second weapon and no weapon entering from off-screen.
- No whole-body spin, no full 360-degree rotation, no side profile, no mirrored character, no three-quarter redesign and no horizontal flight across the canvas.

NEGATIVE LIST:
no voluntary jump, no attack, no sword swing, no guard pose, no second hit, no attacker, no impact flash, no blood, no dust, no smoke, no speed lines, no shockwave, no camera movement, no motion blur, no fall to the ground, no landing, no crouch, no kneel, no lying-down death pose, no dead corpse, no return to Idle, no standing recovery, no floor, no scenery, no shadow, no text, no UI, no checkerboard, no extra character, no extra weapon, no cropped limb, no cropped sword, no sprite leaving the frame.

```

### 10. Launch Landing v1

- 阶段：已验收：视频、6 帧选帧和 6 张本地透明素材；未部署。
- 记录来源：`Library/Locus/tmp/sword_enemy_launch_v1/record_landing.json`
- Prompt 来源：`Library/Locus/tmp/sword_enemy_launch_v1/prompt_landing_v1.txt`
- Task ID：`task_9f5c4081f24c4206afeae33c071e19c3`
- Prompt 字符数：4658
- Prompt SHA-256：`f1c68098abc2c35c4f40b7b30fec4dcf83303300ea8d477b2d56fc21d649347d`

```text
This is a 2D game sprite animation for the accepted 1011 armored sword soldier, not a cinematic video. The video is the SECOND HALF of a launch: it starts from the actual final airborne frame of the accepted Rise animation and ends in a stable grounded landing pose. It does NOT include the later stand-up recovery to Idle; that will be a separate animation after this landing tail is approved.

CHARACTER AND EQUIPMENT:
Preserve the exact 1011 identity: silver-gray helmet, red ribbon, dark gray metal armor, brown greaves, gold belt, red cloth, hard pixel-block shading, thick dark outline, original colors and pixel density. The sword belongs on the SCREEN-LEFT side. Keep the complete sword, helmet, ribbon, armor, both hands and both boots visible and consistent.

ENDPOINTS:
- FIRST FRAME: the supplied actual final frame of the accepted Rise video. It is an airborne passive-launch pose: torso and head tipped backward/upward, both hands open with their existing palm directions, both legs airborne, and the sword separated near the screen-left hand.
- LAST FRAME: the supplied grounded landing-tail reference. Use it only as a landing endpoint: the soldier has completed the fall, is lying in his own slot, and has stopped moving. Do not stand him up and do not return him to Idle in this video.
- The transition between endpoints must be continuous. Do not jump-cut from air to ground and do not make the head or hands snap to a new orientation.

ACTION:
Continue the existing downward flight from the airborne first frame. The game has brought the enemy down to ground level. Show one physical landing and the final grounded, motionless pose:
- the body descends in the same central slot;
- the lower legs and hips make contact first and absorb the impact;
- the torso follows down and settles into the grounded landing pose;
- the head and neck follow the torso continuously and remain naturally turned upward/away from the player, as in a body that has fallen backward. The face/visor must never suddenly turn front-facing toward the player;
- preserve the hand and wrist palm directions from the airborne first frame through contact. Hands may relax during the impact, but no palm flips or mirrored wrists;
- the sword lands on the same screen-left side and remains complete and visible; no new swing and no disappearance;
- the ribbon and loose cloth settle after the body and stop with the grounded pose.

TIMING:
- 0.00-0.20s: continue the airborne descent; no idle reset and no new jump.
- 0.20-0.45s: ground contact and one landing compression; hips, knees, chest, head and hands settle in overlapping order.
- 0.45-0.70s: small damped settling motion into the exact grounded tail pose.
- final part: hold the grounded landing pose completely still. No stand-up, no second bounce and no return to Idle.

LANDING READ:
This is a controlled game landing tail, not a death animation. The soldier is not being killed and must not receive a new killing blow. Do not add blood, damage effects, dust, flash, debris or a dramatic corpse collapse. The body may be sprawled or heavily reclined because of the launch, but the pose should read as the same living soldier completing a knock-up landing, not as a permanent Dead state.

HARD SPRITE CONSTRAINTS:
- Fixed 1:1 canvas, locked orthographic front-facing camera, identical framing, drawing scale, pixel density and character identity. Never zoom, pan, track, recenter, reframe, rotate or shake the camera.
- Keep the root slot and canvas position stable. The world-space flight is controlled by Unity; do not simulate the full vertical fall by translating the entire sprite across the canvas.
- No scale pulse, inflation, shrinking, whole-image squash, stretching or camera recentering. Only local landing articulation and the final grounded pose may change.
- Keep at least 15 percent empty margin on every side. Nothing is cropped, clipped or touches a canvas edge.
- Keep the sword on SCREEN-LEFT and preserve its length, identity and complete silhouette. Do not mirror or switch hands.
- Keep the flat uniform chroma-green background RGB(0,177,64) unchanged in every frame. No floor, shadow, scenery, text or UI.

NEGATIVE LIST:
no stand-up recovery, no return to Idle, no front-facing idle head, no head snap, no palm flip, no mirrored wrist, no second bounce, no second landing, no new hit, no attack, no guard, no death blow, no corpse death animation, no blood, no dust, no impact flash, no debris, no shockwave, no floor scenery, no camera movement, no zoom, no side view, no three-quarter redesign, no mirroring, no extra character, no extra weapon, no cropped limb, no cropped sword.

```

### 11. Launch Getup v1

- 阶段：已验收：视频、6 帧选帧和 6 张本地透明素材；未部署。
- 记录来源：`Library/Locus/tmp/sword_enemy_launch_v1/record_getup.json`
- Prompt 来源：`Library/Locus/tmp/sword_enemy_launch_v1/prompt_getup_v1.txt`
- Task ID：`task_b226b12b3dc24b9aae00e0ad63480260`
- Prompt 字符数：5600
- Prompt SHA-256：`a899d55b38cd3462790ef9cf1189dbd841c038ab884ba2a84b5534a90f313ac6`

```text
This is a 2D game sprite animation for the accepted 1011 armored sword soldier, not a cinematic video. The video is a GET-UP RECOVERY after the already completed and accepted launch landing. Preserve the exact 1011 identity: silver-gray helmet, red ribbon, dark gray metal armor, brown greaves, gold belt, red cloth, hard pixel-block shading, thick dark outline, original colors and pixel density. The sword belongs on the SCREEN-LEFT side.

ENDPOINTS:
- FIRST FRAME: the supplied actual final frame of the accepted landing-tail video. The soldier is already grounded in the accepted sprawled landing pose. Start from that exact grounded pose; do not repeat the fall or landing.
- LAST FRAME: the supplied accepted 1011 idle anchor. Finish in that exact upright idle pose and hold it steadily at the end.
- This is one continuous get-up action between those two endpoints. No cut, reset, pose snap or crossfade.

ACTION SEQUENCE:
1. Start grounded and still for only a very brief moment. The character is alive after a launch, not dead. The head and neck are naturally tipped upward and slightly away from the player as in the landing pose; do not begin with a front-facing idle head.
2. The screen-left forearm and the opposite arm begin to support and reposition the body. Hands press against the ground area only as an animation implication; do not draw a floor, shadow or contact effect. The palms keep the same orientation inherited from the landing pose while the wrists bend naturally.
3. The knees and hips draw inward first. The pelvis gathers underneath the body, the legs fold under the torso, and the torso rises from the sprawled position. This is a physical recovery, not a magical pop-up and not a death collapse.
4. The armored chest and shoulders lift next. The head remains delayed and still looks upward/away for a short part of the rise; the neck gradually follows the torso.
5. The sword remains on the SCREEN-LEFT side. It is recovered through the arm and returns continuously to the exact idle grip relationship and general idle angle. Do not make a new attack, guard or sword swing. Do not mirror or switch the sword side.
6. Near the end, the pelvis and boots regain the exact accepted idle standing construction. Only after the torso is upright does the head gradually return to the exact front-facing idle head. The hands close into the exact idle hand shapes and the ribbon settles last.
7. Finish in the exact idle pose and hold it without a second movement.

READ AS RECOVERY, NOT DEATH:
The soldier is alive and recovering from a launch. The motion must read as effort and re-stabilization: grounded sprawl -> push up -> knees under body -> torso upright -> head and hands align -> idle. Do not make him limp, vanish, die, crawl away, roll over, or lie permanently on the ground. Do not make him perform a combat roll, somersault, attack, dodge, kneel pose or dramatic hero pose.

TIMING:
- 0.00-0.20s: grounded landing pose settles; no repeat of the fall.
- 0.20-0.70s: arms/shoulders support and pelvis gathers; knees draw inward.
- 0.70-1.25s: torso rises through a clear crouched transition; the head remains delayed and angled upward/away.
- 1.25-1.75s: hips and boots regain the standing idle construction; torso becomes upright.
- 1.75-2.20s: head, hands, sword and ribbon finish their delayed return to the exact idle pose.
- 2.20-4.00s: exact idle pose held steadily. No new bounce, attack or idle redesign.
- Every body phase must change continuously. Do not freeze a new intermediate crouch or kneel pose for seconds.

HARD SPRITE CONSTRAINTS:
- Fixed 1:1 canvas, locked orthographic front-facing camera, identical framing, drawing scale, pixel density and root position from first to last. Never zoom, pan, track, recenter, reframe, rotate or shake the camera.
- The whole sprite stays in the same central canvas area. Do not translate, resize, inflate, shrink, squash or stretch the whole drawing. Use local joints and body articulation only.
- Keep at least 15 percent empty margin on every side. Helmet, ribbon, armor, both hands, both boots, sword and all accessories remain completely visible.
- Keep the sword on SCREEN-LEFT with the same identity, side, grip and length. No second weapon, no sword disappearance, no sword side switch.
- Preserve the exact 1011 helmet, armor, ribbon, colors, highlights and pixel outline.
- Do not draw a floor, shadow, ground plane, landing dust, scenery, text or UI. Keep the same flat uniform chroma-green background RGB(0,177,64) in every frame.

HEAD AND HAND CONTINUITY RULES:
- The head must not face the player while the body is still lying or crouched. It returns toward the player only continuously and only after the torso is nearly upright.
- The palms and wrists must not flip between frames. Inherit the landing pose hand orientation, then close and settle into the exact Idle hand shapes near the end.
- No standing head on a fallen body, no instant neck snap, no mirrored palms, no disconnected hands.

NEGATIVE LIST:
no fall replay, no second landing, no bounce, no attack, no sword swing, no guard, no dodge, no somersault, no combat roll, no crawl, no death, no corpse, no permanent lying pose, no floor, no shadow, no dust, no landing effect, no front-facing head before the torso rises, no head snap, no palm flip, no mirrored wrist, no sword side switch, no full-body translation, no camera movement, no zoom, no side view, no three-quarter redesign, no mirror, no scale pulse, no inflation, no stretching, no extra character, no extra weapon, no cropped limb, no cropped sword, no text, no UI.

```
