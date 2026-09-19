# FakeRoute data trial

Open `Assets/Experiments/CurvedScroll/FakeRouteDataTrial.unity` and enter Play Mode.

## Topology

```text
A_Start
├─ Choice_0 → B_Left
└─ Choice_1 → C_Right
B_Left ─ Choice_2 → D_End
C_Right ─ Choice_3 → D_End
D_End: terminal, no outgoing choice
```

Data assets are in `Assets/Experiments/CurvedScroll/RouteTrialData/`.

## Test sequence

1. Start at A_Start. Its arrival writes a checkpoint.
2. Click `Complete current content`.
3. Choose Choice_0 or Choice_1. The associated travel presentation waits for its duration and starts the scroll travel.
4. At B_Left/C_Right, confirm the node and checkpoint. `Visited` must contain A and the selected branch.
5. Try to choose a route to an already visited node: the harness must reject it. The graph validator also rejects cycles before Play Mode.
6. Use `Load checkpoint` to restore the last logical node. Use `Restart` to clear the fake-route snapshot and return to A_Start.
7. Continue to D_End through the selected branch. D has no outgoing choices and is the terminal data case; the harness currently displays the terminal node but does not call the production victory settlement.

This harness tests data, one-time node visits, choice locking, travel callback timing and fake-route snapshot contents. It does not start real `StageConfig` battles and does not represent the final `FakeRouteRuntime`.

## Persistent-state boundary exercised

The snapshot stores route identity/version, checkpoint node, visited/completed nodes and choice history. The full production snapshot model also has fields for player health, revives, level, exp, upgrades, active skills and limited items. It intentionally excludes travel frames, current waves, enemies, projectiles, QTE/attack state and reward-popup intermediate state.

Snapshots use `fakeRouteSnapshots`, separate from old V2 `routeStageSnapshots`. Test assets and the test snapshot can be cleared with the panel's Restart button. Stop Play Mode after testing to avoid temporary runtime state confusion.
