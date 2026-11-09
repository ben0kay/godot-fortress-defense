# PWR0 — Adaptive Power Mesh Topology Foundation (Codex Implementation Plan)

**Target:** `ben0kay/godot-fortress-defense`, branch **`main`**.  
**Status:** **Planned / not implemented**. Implement **PWR0 only**.  
**Context:** [ADAPTIVE_POWER_MESH_ROADMAP.md](ADAPTIVE_POWER_MESH_ROADMAP.md).

## Objective

Introduce a world-scoped, **range-based relay graph** for the future Fortress Defense power system. A built-in Citadel relay is the first anchor; future Power Relays connect automatically when in range, forming independent network components that can merge on bridge placement and split after node destruction. Generators, batteries and power consumers attach as leaves to their nearest valid node, and cannot themselves transmit power onward.

PWR0 establishes **topology and read-only inspection** only. It must be reusable without replacement when PWR1 adds actual Citadel generation/Tower001 capacitor consumption and PWR2 adds a placeable Power Relay. Keep the game fully playable and existing Tower001/Tower002/Wall001 behavior intact. **Do not create generator, battery or relay catalog buildings in PWR0.**

## A. Mandatory refresh / reference inspection

Before editing, refresh and read:
- `AGENTS.md`; `NOTES/CodePreferences.md`; root `README.md`.
- `FORTRESS/BUILDINGS/Energy/README.md`; [power roadmap](ADAPTIVE_POWER_MESH_ROADMAP.md).
- `FORTRESS/BUILDINGS/SHARED/Building.cs`, `BuildingDefinition.cs`, `BuildingRegistry.cs`; `FORTRESS/SYSTEMS/BuildingPlacement/BuildingPlacement.cs`; C1 construction work/operational lifecycle.
- `FORTRESS/BUILDINGS/Core/Citadel/Citadel.tscn`, definition/placement; `LEVELS/SHARED/LevelController*.cs`, `LevelBase.tscn`, `LEVELS/Level001/`, `DEBUG/test.tscn`; `WORLD/Grid/WorldGrid.cs`; relevant UI/debug owner.
- `COMBAT/README.md` and DMG0 architecture only as needed to ensure no combat rewrites.

Reference **read-only**:
- `ben0kay/VECTOR_TD_2026` (`main`): `scripts/scr_energy/scr_energy.gml`, `objects/o_controller_energy/Create_0.gml`, `Step_0.gml`, relevant node/generator/battery objects. Understand nodes-as-backbone, nearest-node endpoint attachment, separate network budgets, private storage and active-only registration. Improve design; do not copy global step loops.
- `ben0kay/godot-tower-defense` (`dev`): `TOWERS/Economy/Power/Shared/PowerGridManager.cs`, `PowerGridManager.Accounting.cs`, `TOWERS/Shared/Power/PowerCapacitor.cs` and `TowerPowerConsumer.cs`, `WORLD/OBJECTIVES/Citadel/CitadelPowerConfig.cs`. Borrow ownership and lifecycle patterns; **do not port terrain PowerIslandMap or conductivity**.

Source is the final authority for exact paths/signals; the file list is guidance. Do not touch reference repositories.

## B. Canonical topology contract — implement this, not a terrain island model

1. Register **relay nodes** with: stable registration identity, world position/footprint centre, finite positive transmission radius, operational eligibility, optional owner reference for lifecycle cleanup.
2. Two relay nodes link **only** if `distance_squared <= min(rangeA, rangeB)^2`. The rule is bidirectional, no terrain/world A* or physics raycast. No diagonal grid-neighbour exceptions. Range is in **world pixels**.
3. Register **endpoints** (consumer, generator, battery) with: stable identity, role, position, connection radius, operational eligibility. Attach each operational endpoint to **exactly one** nearest link-eligible relay; a deterministic stable-ID tiebreak resolves equal distances. It inherits that node's network membership. Endpoint-to-endpoint and endpoint-to-node links cannot create connectivity edges between networks.
4. Determine connected components via bounded BFS/DFS over live relay edges. A node with no other links is still a valid one-node network, including the initial Citadel.
5. **Two networks merge immediately on relay insertion** if the new relay links both; **split on removal/death/disable** if the removed relay was a bridge. Reattach leaves to their closest remaining eligible node. Do not depend on current map cell terrain, line of sight, building obstruction, movement path or camera position.
6. Inactive/under-construction structures must **not** participate until operational. Building death, removal, detachment and level reset remove membership exactly once. Future disable/EMP must use the same participation seam. An endpoint without valid relays reports **disconnected / network=-1**.
7. Connected-component IDs are **ephemeral** (do not save or treat them as durable building IDs). Return read-only snapshots/query results; never let HUD/consumers modify the live graph.
8. No power generation, global energy pool, storage, recharge allocation, demand or fighting disable behavior in PWR0. Nodes should be safe to model without a producer present.

## C. Integration strategy

Implement one authoritative, level-scoped `PowerNetworkManager` under `FORTRESS/SYSTEMS/PowerNetwork/`. Keep the graph algorithm pure or easily focused-testable, and provide a narrow registration/update/remove/query API.

Suggested files, only as useful:
- `PowerNetworkManager.cs` — runtime ownership, dirty revision and lifecycle binding.
- `PowerMeshGraph.cs` (optional) — compact deterministic component rebuild and nearest-node attachment.
- `PowerNetworkConfig.cs` + `DefaultPowerNetworkConfig.tres` — Inspector settings for initial Citadel radius and tuning if justified.
- `README.md` — roles, range rules, lifecycle, inspection, status.
- Focused `TESTS/Power/` smoke checks if feasible without broad harness.

**Citadel integration:** bind its **existing actual** building instance as a relay after placement/ready, with an Inspector-editable radius (e.g. provisional 400 world pixels). Do not replace/duplicate Citadel scene, health, placement or construction. It remains ordinary placed and vulnerable building, without additional power visuals or a new generator behavior yet.

**General building integration:** future concrete building types opt into an explicit capability/registration route. The base `Building` may gain a minimal optional event/interface if needed, but **do not mark every building as a relay or consumer automatically**. Avoid eagerly building unused generator/battery classes. Consider stable per-instance identity for deterministic tie-breaking without relying on volatile node enumeration order; no save system required in PWR0.

**Composition:** add the service to LevelBase/Level001 and the DEBUG sandbox using the existing level controller/service bind patterns (verify scene ownership); no autoload, no parallel power system in `WORLD/`. Leave `WorldGrid` terrain and occupancy untouched.

**Diagnostics:** provide O(1) or cached query access to NetworkCount, NodeCount, EndpointCount, NetworkFor(owner/ID), ConnectedNodes, and nearest relay information as appropriate. Minimal existing debug readout is optional; do not build a large new overlay or per-frame UI. A small debug-only registration/validation path is acceptable to demonstrate synthetic merge/split before PWR2 has a placeable relay.

## D. Event-driven rebuild & performance

Rebuild topology **only** when registrations, positions, radii, operational status, death/removal or level composition change. Batch related changes and rebuild lazily or once after a coherent mutation. A full O(relay²) pair check is acceptable for very small relays but avoid repeated full scans and establish an upgrade path to spatial buckets for hundreds of nodes. BFS handles deletion/splitting without error-prone union-find-only logic.

No `_PhysicsProcess` for graph topology, no terrain flood fill, no scene-tree scanning per tick, no duplicate owner subscriptions, no per-frame inspector refresh. Cache queries/revisions, avoid retaining invalid Godot objects and cleanly tear down on level reset. No performance claims without actual measurements.

## E. Rules to preserve

- DMG0 typed packets and existing health/death.
- C1 construction drones, pending/complete building state and cancellation.
- WorldGrid authoritative placement/nav/solid terrain.
- Credits/cost atomic placement, BuildingRegistry and existing Citadel ownership.
- Projectile collision and COMBAT1 friendly Wall001 overflight.
- FW1 fog and visibility; no enemy-target or construction changes.
- Tower001/Tower002 weapon behavior is unchanged and **not yet power-gated**.
- Enemy AI/targeting, player chassis, Level001 127×127 size, camera and debug scenes.
- No new persistent network ID/save format.

## F. Focused acceptance checks

1. Citadel is registered as one valid network node when level starts, with no electric output yet.
2. Isolated test relay forms its own network; two in-range relays merge; adding a bridge merges two originally separate networks.
3. Removing/destroying a bridge splits networks appropriately; redundant loop maintains connectivity after any single link is removed.
4. Exactly-at-boundary connection succeeds, just-outside fails; asymmetric ranges use the smaller value; diagonal terrain/dead clusters don't matter.
5. Consumer endpoint chooses nearest eligible relay, joins precisely one network, reattaches to the next when nearest disappears, and remains disconnected if none qualify. It must never itself bridge.
6. A non-operational/construction-queued prospective participant is absent, and only joins after operational completion.
7. Citadel destruction unregisters its node; repeated removals and reset/reload don't leave ghost links or leaked callbacks.
8. Deterministic ties and rebuild results are stable for the same authored inputs.
9. No tower damage/targeting/power costs/credits/occupancy/visuals change in this pass.
10. PWR1 can register the Citadel as producer and Tower001 as consumer without changing topology semantics.

Use direct small validation and source/diff review; one `dotnet build` when practical according to CodePreferences. No broad tests, FPS benchmarks or lengthy headless runs.

## G. Explicit exclusions

Do **not** implement:
- Any active Citadel electricity production or Battle Tower capacitor/drain/no-power (PWR1).
- Placeable Power Relay or art/construction/menu item (PWR2).
- Generators, solar, wind, batteries, fuel, environmental day/night (PWR3+).
- Battery discharge and priority allocation (PWR3+).
- Full selectable network overlay/animated links (PWR4).
- EMP, power sabotage AI, articulation-point scoring (PWR5).
- Power through conductive terrain or `WorldGrid` path regions.
- Imported Godot Tower Defense power island code, global energy cheat, manual wire-link UI.

## H. Documentation and Git handoff

Update `FORTRESS/BUILDINGS/Energy/README.md` and any actual composition docs. Mark source status accurately as **Source Implemented / Awaiting Owner Validation** after implementation; leave PWR1–PWR5 as Planned.

Commit and push **only PWR0** to `main`, respecting `NOTES/CodePreferences.md`. Report SHA, files, registration/API ownership, Inspector radius, what was source-checked/built, pending owner checks and any limitations (notably no active energy gameplay until PWR1).
