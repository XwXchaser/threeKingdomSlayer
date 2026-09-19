# Valley art experiment

Scene: `Assets/Experiments/CurvedScroll/ValleyCombatTrial.unity`.

Created by Save As from the user's unsaved ScrollCombatTrial scene state with approval. Original trial file and production Battle scene were not overwritten by this art integration.

Art is in `Assets/Experiments/CurvedScroll/Art/`: 12 slices in ValleyProps, four in ValleyShoulders, portrait 1024x1536 ValleyBackground, and separate 256px repeating ValleyRoad/ValleyRoadside. Original generation sources remain under Library/Locus/tmp/valley-art. Transparent atlas alpha was rescaled to reach 255; originals unchanged. Sprite crops are manually estimated from the generated layout and still require edge/foot inspection across every variant.

ValleyArtPresentation uses explicit Inspector references. It hides placeholder scenery/display enemies at runtime; actual combat enemies remain. The ground shader samples road/side textures at fixed world-unit density, with adjustable width and transition. Shoulder meshes follow the curved ground; upright props share travel distance. Background retains a 2:3 aspect with configurable vertical UV lift (default .4, requires restart) to expose distant mountains above the ground horizon. Other screen ratios use aspect-preserving crop, not stretch.

Validation: full compile passed, runtime Console errors zero; offscreen 800x1200 camera renders inspected, including a temporary environment-only capture with real enemy renderers restored immediately afterward. These captures are NOT full Game-view/HUD resolution acceptance. No claim of complete battle loop, long-run art memory profiling, all-slice edge verification, road-width extremes or seamless recycling acceptance.

Preview: `Library/Locus/tmp/valley-art/valley_environment_2x3_v2.png`.

Known tuning: shoulder silhouettes are narrow in perspective; ground-background horizon still needs art direction; foreground density and physical prop sizes are provisional. Generated meshes/material blocks are runtime-only. Stop Play Mode before changing persistent scene parameters. Exit Play Mode rather than using inherited production retry/menu actions during isolation testing.
