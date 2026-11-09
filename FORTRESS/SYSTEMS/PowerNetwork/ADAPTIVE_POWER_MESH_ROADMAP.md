# Adaptive Power Mesh — Implementation Roadmap

**Repository:** `ben0kay/godot-fortress-defense` · **Branch:** `main`  
**Status:** **PWR0 Source Implemented / Awaiting Owner Validation.** PWR1–PWR5 remain planned. PWR0 specification: [PWR0_IMPLEMENTATION_PLAN.md](PWR0_IMPLEMENTATION_PLAN.md).

## Product direction

Fortress Defense uses a **distance-based electrical mesh**, **not terrain-conductivity islands** and not one global power pool. Relays are small metallic buildings with transmission range. Linked relays form connected components ("networks"). Two separate networks **merge automatically** when a new relay bridges them; destruction/removal of that relay may **split them**. Each component owns its own available generation, consumers and later battery capacity. Nodes relay, consumers/generators/batteries attach; ordinary consumers never extend the relay backbone.

The game might ultimately have vector/biological/cyber styling instead of spacecraft; this does not affect the power model. No daylight/solar multiplier required initially.

### Two concrete references (read-only)

- **`ben0kay/VECTOR_TD_2026` (`main`)**: `scripts/scr_energy/scr_energy.gml`, `objects/o_controller_energy/{Create_0,Step_0}.gml`. It implements node-only network traversal, closest-node attachment, range checks, private consumer buffers, per-network accounting, batteries and priorities. Improve it using cached topology, bounded membership searches and well-defined lifecycle; do not port its GameMaker controller/step loops wholesale.
- **`ben0kay/godot-tower-defense` (`dev`)**: `TOWERS/Economy/Power/Shared/PowerGridManager*.cs`, `TOWERS/Shared/Power/{PowerCapacitor,TowerPowerConsumer,TowerPowerConfig}.cs`, `WORLD/OBJECTIVES/Citadel/CitadelPowerConfig.cs`. Borrow lifecycle, energy accounting, capacitor and funding patterns only. **Do not reuse PowerIslandMap/conductive-terrain assumptions**.

## Canonical topology rules

1. **Relay backbone:** Every powered-capable Citadel is a node with authored relay radius; a constructed Power Relay will be another node. Nodes connect **bidirectionally** if their *centres* meet `distance <= min(radiusA, radiusB)`. Equivalently, each node must be able to reach the other. Merely overlapping two big drawn circles is **not** enough. No line-of-sight, terrain, grid navigation, wall blocking, or circular-footprint pathfinding.
2. **Non-relay endpoint:** A generator, battery or consumer connects to **exactly one** nearest eligible node, provided `distance <= min(endpointRadius, nodeRadius)`. Ties use stable deterministic ordering/IDs. Endpoints are **leaves**: they cannot bridge islands or daisy-chain consumers. Reattachment happens on topology change; a building attached to Network A does not automatically tap B even if its circle touches B. A standalone generator cannot create an island by itself.
3. **Networks = connected components of live relay nodes.** Use BFS/DFS over the finite graph and rebuild on relevant changes. Connected-component IDs are **runtime ephemeral**, not durable save IDs. Merge/split/reassign all endpoints atomically; never duplicate generators or stored energy across a split.
4. **Lifecycle:** Only alive, placed, **operational** nodes/endpoints participate. Unfinished C1 construction and destroyed/deleted structures do not; completed buildings join immediately. Support future disable/EMP and relocation signals. Citadel can act as a small generator and backbone node simultaneously, via independent capabilities.
5. **Network identity vs energy balance:** A connected component is a *membership graph*, not a globally stored infinity battery. Later generation is a finite energy/second budget shared among member consumers according to priority/limits; charge is stored **on capacitor/battery owners**, not as an unbounded network counter.
6. **No terrain islands:** A flat open battlefield, DeadCell terrain and distant procedural clusters never imply electrical connectivity. No physical wires or manual link clicks. Relay presence, operational state, position and transmission radii are the only PWR0 topology inputs.
7. **Range/visual clarity:** Selection can show the authored transmission radius and **actual valid links**. Avoid telling the player that two nodes connect just because two visible circles overlap; the connection condition is centre-to-centre distance. Link visuals are cosmetic and cannot own connectivity.

## Suggested architecture / ownership

```text
FORTRESS/SYSTEMS/PowerNetwork/
  PowerNetworkManager.cs         level-scoped authoritative graph owner
  PowerNetworkConfig.cs          Inspector-tunable topology settings
  PowerNetworkSnapshot.cs        lightweight query/result types as needed
  README.md
FORTRESS/BUILDINGS/Core/Citadel/
  [existing Citadel scene + opt-in future power producer/relay contract]
FORTRESS/BUILDINGS/Energy/
  PowerRelay/                   placeable building in PWR2, not PWR0
  Generator/                    in PWR3
  Battery/                      in PWR3
UI/...                          optional diagnostics & visuals later; never system owner
```

Use compact interfaces/components or an explicit registration API for node/leaf roles; **do not** put a large power manager on every building. WorldGrid remains authoritative for placement, not power topology. Avoid new root-level `POWER/` or `TOWERS/`.

## Incremental passes

| Pass | Status | Deliverable / boundary |
| --- | --- | --- |
| **PWR0** | **Source Implemented / Awaiting Owner Validation** | Range graph, deterministic relay connected components, nearest-leaf attachment, Citadel as initial node (topology only), lifecycle/merge/split, read-only diagnostics + focused checks. **No operational power costs or energy allocation yet.** |
| **PWR1** | Planned | Citadel small generation, Tower001 capacitor, idle/shot energy cost and no-power/recovery state, finite per-network energy-rate budget. Only Tower001 opts in first. Preserve DMG0 damage and C1 construction. |
| **PWR2** | Planned | Constructible metallic Power Relay (price, health, C1 drone/stage artwork), build-menu entry, automatic bridge/split, range preview and connection feedback. |
| **PWR3** | Planned | Placeable dedicated generator(s) and battery(s), per-network generation/storage, priority allocation; initial stable output (solar/wind variation later). |
| **PWR4** | Planned | Selected-only connected links/coverage view, local network Inspector totals, unmet demand, recharge and outage indicators. |
| **PWR5** | Planned | Advanced priorities, optional environmental generation factors, network weakness/articulation-point analysis and enemy infrastructure sabotage roles. |

PWR0 must expose enough query/registration seams that PWR1–PWR3 do **not** replace its graph or invent a competing electrical connectivity service.

### Initial tuning suggestions — not balance commitments

- Citadel relay radius ~320–480 world pixels, configurable.
- Future dedicated relay radius ~320 pixels and 1×1 footprint; independent cost/HP/construction tuning.
- Future consumer internal buffer / charging and generation numbers are **PWR1/PWR3** decisions. Existing Godot Tower Defense `BattlePower.tres` values and VECTOR TD defaults are *references*, not implicit Fortress Defense balancing.
- The Citadel's generation can be low enough to support a small starter fortress, then encourage dedicated generators.
- Relays should **not** consume electricity merely to create circular dependency in the first version; a later pass may add explicit upkeep if gameplay warrants it.

## Gameplay & attack ideas (deferred)

Enemy AI may later value a relay whose removal disconnects many consumers, but **PWR0 does not change enemy AI**. A later pass can use the network graph to find **articulation points** (e.g. Tarjan) and pass scored vulnerable relay targets to enemy strategy. Actual attacks still use existing damage, navigation, collision, fog and health logic. Destroyed generators reduce production; relay destruction changes topology. Backup generators and redundant relay loops mitigate failures.

## Design decisions / safety

- **Avoid accidental free power** from a consumer sitting in overlapping range of multiple networks. It chooses one; never duplicates generation.
- Explicit disabled/unfinished states unregister from graph; network rebuild is triggered by lifecycle, not each frame.
- No stale Node references after death, removal or reload. Stable registration key and deterministic tie-breaking; no serialized network numbers.
- Do not reassign/erase a tower's local stored charge during network merge/split.
- Keep a clear visible state for an unconnected building (PWR1 onward), and later add priority controls without new graph semantics.
- Do not block credit purchases due only to lack of power; a disconnected constructed tower can remain built and later connect.
- Graphics/fog are separate from the authoritative topology; no revealing unrevealed enemies.
- Maintain a small, event-driven graph; avoid full-map cell flooding, per-physics-frame pairwise scans and unnecessary allocations.

**Implementation entry:** [PWR0_IMPLEMENTATION_PLAN.md](PWR0_IMPLEMENTATION_PLAN.md). All later passes require their own authorization.
