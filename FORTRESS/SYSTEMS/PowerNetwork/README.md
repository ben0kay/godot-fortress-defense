# Adaptive Power Mesh — PWR0

**Source Implemented / Awaiting Owner Validation.** LevelBase (including Level001) and DEBUG each own one `RootFortress/PowerNetwork`. LevelController registers the actual placed Citadel as a relay. Set **Citadel Relay Radius** on the manager in the scene Inspector before running the level. The default is **400 world pixels**.

## Registration and queries

- Call `BindBuilding` after placement to opt in as a relay, consumer, generator or battery. The same building can opt into multiple roles with one set of lifecycle subscriptions. A pending C1 site stays absent until `WorkCompleted`. Death, removal and scene exit clean up registrations and callbacks.
- `Register` is the explicit non-building API used by synthetic checks. `UpdateParticipant` accepts committed position, radius and enabled state changes. The caller should submit relocations after placement has committed them. An unfinished site stays absent even if enabled. `Remove` is idempotent.
- A relay links when the distance between centres is at most **both** radii. Endpoints choose a single closest relay under the same rule. Equal distances use the lowest stable registration ID. Terrain, obstacles, fog and navigation have no role.
- `NetworkFor` returns value-type membership by ID or by building and role. Disconnected results have network ID **−1** and relay ID **0**. `ConnectedNodes` returns a read-only component snapshot, and `AreRelaysLinked` checks a direct edge. IDs for registrations last for the service lifetime. Component IDs are ephemeral across relay edits and should never be saved.
- `NodeCount` counts active relays. `EndpointCount` counts operational endpoints, including disconnected ones. `NetworkCount` counts relay components, including isolated nodes. F2 reads these diagnostics from the existing debug refresh.

## Work and limits

The manager has no frame or physics callback. It coalesces relay changes until the first component read and caches the result. A leaf edit invalidates only that leaf. Relay changes invalidate leaf attachments by revision, and each leaf reattaches when queried. Component traversal reuses scratch lists, removes discovered candidates from later scans, and stores no adjacency matrix. Relay rebuilds still have a quadratic worst case in sparse layouts. A spatial candidate index is the next optimization if relay counts warrant it.

This pass is topology only. PWR1 adds energy generation and consumption, and PWR2 adds a placeable relay. [Roadmap](ADAPTIVE_POWER_MESH_ROADMAP.md).

## Owner checks

1. Run `TESTS/Power/PowerMeshSmoke.tscn` with **F6**. The output should say `PWR0 power mesh smoke passed.` It covers relay merge/split, redundancy, radius boundaries, nearest endpoint ties, C1 completion and Citadel destruction.
2. Run Level001 and DEBUG. Open **F2** and confirm **1 network, 1 relay, 0 endpoints**. Destroy the Citadel and confirm the counts drop to zero. Reload or regenerate and confirm the initial counts return.
3. Place Tower001 and let the C1 drone finish it, then spawn ground and air enemies. Check construction and combat as usual. Current towers do not register for power in normal gameplay.
