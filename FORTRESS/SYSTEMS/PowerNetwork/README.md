# Adaptive Power Mesh — PWR0

**Source Implemented / Awaiting Owner Validation.** LevelBase (including Level001) and DEBUG each own one `RootFortress/PowerNetwork`. LevelController registers the actual placed Citadel as a relay. Set **Citadel Relay Radius** on the manager in the scene Inspector before running the level. The default is **400 world pixels**.

## Registration and queries

- Call `BindBuilding` after placement to opt in as a relay, consumer, generator or battery. The same building can opt into multiple roles with one set of lifecycle subscriptions. A pending C1 site stays absent until `WorkCompleted`. Death, removal and scene exit clean up registrations and callbacks.
- `Register` is the explicit non-building API used by synthetic checks. `UpdateParticipant` accepts committed position, radius and enabled state changes. The caller should submit relocations after placement has committed them. An unfinished site stays absent even if enabled. `Remove` is idempotent.
- A relay links when the distance between centres is at most **both** radii. Endpoints choose a single closest relay under the same rule. Equal distances use the lowest stable registration ID. Terrain, obstacles, fog and navigation have no role.
- `NetworkFor` returns value-type membership by ID or by building and role. Disconnected results have network ID **−1** and relay ID **0**. `ConnectedNodes` returns a read-only component snapshot, and `AreRelaysLinked` checks a direct edge. IDs for registrations last for the service lifetime. Component IDs are ephemeral across relay edits and should never be saved.
- `NodeCount` counts active relays. `EndpointCount` counts operational endpoints, including disconnected ones. `NetworkCount` counts relay components, including isolated nodes. F2 reads these diagnostics from the existing debug refresh.

## Work and limits

Topology has no frame or physics callback of its own. It coalesces relay changes until the first component read and caches the result. A leaf edit invalidates only that leaf. Relay changes invalidate leaf attachments by revision, and each leaf reattaches when queried. Component traversal reuses scratch lists, removes discovered candidates from later scans, and stores no adjacency matrix. Relay rebuilds still have a quadratic worst case in sparse layouts. A spatial candidate index is the next optimization if relay counts warrant it.

PWR0 defines topology. PWR1 uses it for generation and consumption, and PWR2 adds a placeable relay. [Roadmap](ADAPTIVE_POWER_MESH_ROADMAP.md).

## Owner checks

1. Run `TESTS/Power/PowerMeshSmoke.tscn` with **F6**. The output should say `PWR0/PWR1 power mesh smoke passed.` It covers relay merge/split, redundancy, radius boundaries, nearest endpoint ties, C1 completion and Citadel destruction.
2. Run Level001 and DEBUG. Open **F2** and confirm **1 network, 1 relay, 1 endpoint**. Destroy the Citadel and confirm the counts drop to zero. Reload or regenerate and confirm the initial counts return.
3. Place Tower001 and let the C1 drone finish it, then spawn ground and air enemies. Check construction and combat as usual. Only Tower001 opts into PWR1 power.

## PWR1 — Citadel generation and Battle Tower power

**Source Implemented / Awaiting Owner Validation.** The Citadel also registers as a generator and supplies **30 energy/s** by default. Tune **Citadel Energy Per Second** on the same manager. Only Tower001 registers a capacitor. Tower002, walls, and construction drones keep their existing behavior.

| Tower001 PowerConfig | Default |
| --- | ---: |
| Connection radius | 400 px |
| Capacity / starting charge | 30 / 0 |
| Idle drain | 2 energy/s |
| Cost per shot | 4 energy |
| Max recharge | 12 energy/s |
| Restart threshold | 8 energy |

The first charge arrives after C1 completes. Every roughly 0.1 seconds, each network shares only the energy generated in that interval, in proportion to capped recharge requests. Unused generation expires. A disconnected tower keeps its remaining charge and can fire until it runs short. Idle drain applies without enemies. The weapon sleeps during an outage and resumes at the restart threshold. Its fog vision and occupancy remain active. Select a finished Battle Tower to see charge, readiness and whether it is out of network.

The allocator reuses scratch storage and sleeps when no capacitor needs recharge or idle service. Topology edits invalidate the partial supply interval, so no component receives energy from before its latest membership change. No storage or allocation state is saved on the network.

### PWR1 owner checks

1. Run `TESTS/Power/PowerMeshSmoke.tscn` with **F6**. Look for `PWR0/PWR1 power mesh smoke passed.` It includes finite sharing, idle drain, paid shots, and loss of the Citadel.
2. In Level001, place a Battle Tower within 400 px of the Citadel and another outside that range. Wait for C1 to finish. The close tower should charge and fire. Select the far tower and check **NO POWER** and **OUT OF NETWORK**. Check Tower002 with flyers as usual.
3. Build several Battle Towers in range and spawn a sustained ground group. Inspect their charge while idle and during firing. Destroy the Citadel and check that stored energy drains and the towers stop firing. Restart and confirm fresh empty capacitors.
