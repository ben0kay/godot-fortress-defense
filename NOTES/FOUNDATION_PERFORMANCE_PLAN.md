# Fortress Defense — Foundation & Performance Roadmap (FD0–FD5)

**Repository:** `ben0kay/godot-fortress-defense` · **Branch:** `main`  
**Status:** BF0/ST0 + BF1/ST1 **Source Implemented / Awaiting Owner Validation** in the focused stress track; FD0–FD5 remain a broader roadmap, not completed milestones. See `../DEBUG/README.md` for the current sandbox and owner checks.
**Gameplay reference:** `ben0kay/VECTOR_TD_2026` (GameMaker/GML).  
**Architecture/style reference:** `ben0kay/godot-tower-defense` (Godot 4 C#).  
**Authoritative local guidance:** root `README.md`, `NOTES/CodePreferences.md`, `AGENTS.md` if present, and the actual owning source files.

> **Initial stress sandbox:** For the concrete early implementation slices (grid/footprints, CPU enclosure fallback, staggered AI and path budgets, edge spawning, tower, fog-of-war and 10–200 enemy scenarios), follow [INITIAL_STRESS_NAVIGATION_PLAN.md](INITIAL_STRESS_NAVIGATION_PLAN.md). These ST0–ST5 phases refine this roadmap; they are not automatically authorized gameplay work.

> **Gameplay infrastructure after the initial combat benchmark:** Follow [GAMEPLAY_INFRASTRUCTURE_PLAN.md](GAMEPLAY_INFRASTRUCTURE_PLAN.md) for the separately authorized BM0 (catalogue/credits), BM1 (Ctrl category-first build menu), PL0 (player), CAM0 (follow/free camera) and FW0 (three-state fog with F2 debug bypass) passes. For now, preferentially match the local visual/UI patterns of `ben0kay/godot-tower-defense` while keeping Fortress Defense's own shared Building scene and gameplay ownership. BM0, BM1 and revised optional PL0/CAM0 are **Source Implemented / Awaiting Owner Validation**; FW0 remains **Planned**.

> **EN2:** Enemy002 Building-Hunter Shooter is **Source Implemented / Awaiting Owner Validation**; see [behavior/settings/owner checks](../ENEMIES/TYPES/Ranged/Enemy002/README.md). This adds fair bounded acquisition/routine targeting, stationary building buckets, shared hostile rounds and typed F2 batches without implementing later enemy roles or claiming measured FPS.

> **AIR0 + AIR1:** Shared movement/target/projectile layer capabilities and Enemy003 Basic Flyer are **Source Implemented / Awaiting Owner Validation**. See [contracts/settings/owner checks](../ENEMIES/TYPES/Flying/Enemy003/README.md). Flyers add no ground A* or terrain sweeps. The prior owner-observed200-ground-enemy60FPS scenario is a benchmark reference only; mixed-layer FPS remains unmeasured. **AIR2 Tower002 is now Source Implemented / Awaiting Owner Validation**; **AIR3 gunship remains Planned**.

> **AIR2:** [Tower002 Anti-Air](../FORTRESS/BUILDINGS/Defense/OffensiveTowers/Tower002/README.md) is **Source Implemented / Awaiting Owner Validation**. Air-only catalogue building, local AA art, shared projectile-tower component and existing spatial/airborne shot contracts. Tower001 keeps its stable resource/script paths and ground behavior. No AIR3 or advanced tower systems.

## Intent and non-negotiables

Build a **separate**, expandable 2D vector-style fortress-defense/action/base-management game. The player controls a mobile combat chassis and defends a central **CPU**, placing towers, walls and eventually resource/production buildings on a **seed-generated open battlefield**. Different geometric enemies can independently attack the CPU, structures or visible player, with role-specific movement and combat. No fixed-path Classic TD model or copied Classic TD enemy roster.

**Performance is an architectural constraint from day one**, not a late polish task. Target a representative encounter with **100–200 active, visible, moving, targeting and attacking enemies** plus towers/projectiles/effects at **60 FPS (~16.7 ms/frame)** on a declared baseline PC. This is a **target**, not a current capability or guarantee. Measure both normal frame time and spikes. Performance budgets must account for *all* gameplay, rendering and GPU costs, not merely AI timings.

Do **not** turn the project into a speculative custom engine. Begin with simple Godot 4 C# facilities and optimize measured bottlenecks. Preserve distinct vector visuals: arrows/triangles, rotating circles, striking geometric silhouettes and lightweight effects. The old Vector TD project's enemy/navigation/culling/spawn patterns are **references**, not performance proof; do not reproduce GameMaker-specific event hierarchies blindly.

**Do not implement FD0–FD5 all at once.** Each pass requires owner authorization, focused diffs, appropriate documentation, one commit/push by default and **minimal AI testing** as required by `CodePreferences.md`.

## Proposed eventual folder structure

**Structural rule:** `FORTRESS/BUILDINGS/` contains **all** constructible structures, including towers. Organize them by purpose; `FORTRESS/SYSTEMS/` holds the reusable mechanics powering those structures. Keep **all UI at repository root `UI/`**, including build menus, building inspectors and debug panels. Root `SYSTEMS/` (app lifecycle/saving) is intentionally distinct from `FORTRESS/SYSTEMS/` (building/base mechanics). Root `COMBAT/` remains shared by the player, enemies and fortress.

This is a **proposed eventual structure**, not a command to create empty folders in FD0. Add subfolders/READMEs/configs only when implementing the relevant features; use feature-level master config resources when justified.

```text
godot-fortress-defense/
├── project.godot
├── README.md
├── NOTES/
│   ├── CodePreferences.md
│   └── FOUNDATION_PERFORMANCE_PLAN.md
├── SYSTEMS/
│   ├── GameSession/                 # boot/pause/transitions
│   └── Persistence/                 # session save/load
├── DEBUG/
│   ├── Performance/                 # measurements/counters, not panel
│   └── StressTools/                 # manual enemy benchmarks
├── WORLD/
│   ├── Generation/                  # reproducible map seed/stages
│   ├── Grid/                        # authoritative occupancy
│   ├── Navigation/                  # world path services
│   ├── Resources/                   # physical ore nodes/deposits
│   └── Scenes/
├── LEVELS/
│   └── Scenarios/
├── PLAYER/
│   ├── Chassis/
│   ├── Weapons/
│   └── Scenes/
├── CAMERA/
├── ENEMIES/
│   ├── README.md
│   ├── CORE/                        # identity, runtime, lifecycle
│   ├── DATA/                        # editable enemy definitions / presets
│   ├── TYPES/                       # named enemy variants and families
│   ├── AI/                          # tactical decisions, targeting, orders
│   ├── MOVEMENT/                    # navigation integration and steering
│   ├── ATTACKS/                     # contact, shots, beams, siege, ram
│   ├── ABILITIES/                   # phasing, splitting, support, transport
│   ├── MODIFIERS/                   # shielded, stealthed, armored, elite
│   ├── SPAWNING/                    # waves, formation, queue and caps
│   ├── VISUALS/                     # vector geometry and animation
│   └── PERFORMANCE/                 # scheduling, indexing, metrics
├── FORTRESS/
│   ├── BUILDINGS/                   # Every buildable structure
│   │   ├── Core/                    # Citadel / CPU
│   │   ├── Defense/
│   │   │   ├── OffensiveTowers/
│   │   │   ├── RepairTowers/
│   │   │   ├── Shields/
│   │   │   └── Walls/
│   │   ├── Energy/
│   │   │   ├── Generators/
│   │   │   ├── SolarPanels/
│   │   │   ├── WindTurbines/
│   │   │   ├── Relays/
│   │   │   └── Batteries/
│   │   ├── Extraction/
│   │   │   ├── CopperMine/
│   │   │   ├── IronMine/
│   │   │   └── SiliconMine/
│   │   ├── Economy/
│   │   │   └── CreditGenerators/
│   │   ├── Industry/
│   │   │   ├── Refineries/
│   │   │   ├── Fabricators/
│   │   │   └── Ammunition/
│   │   └── Infrastructure/
│   │       ├── Storage/
│   │       ├── CargoHubs/
│   │       └── Construction/
│   └── SYSTEMS/                     # Shared mechanics, not building types
│       ├── BuildingPlacement/
│       ├── BuildingHealth/
│       ├── Resources/               # Credits / physical inventory
│       ├── PowerNetwork/            # Connected electrical supply
│       ├── Production/              # Extraction/conversion recipes
│       └── Logistics/               # Cargo jobs, reservations
├── COMBAT/
│   ├── Damage/
│   ├── Shields/
│   └── Projectiles/
├── FEEDBACK/                        # World VFX and non-UI effects
├── UI/                              # ALL user interfaces
│   ├── Menus/
│   ├── HUD/
│   ├── BuildMenu/
│   ├── BuildingInspector/
│   └── DebugPanels/
├── AUDIO/
└── TESTS/
```

Every major *implemented* feature folder should have a short `README.md` describing ownership, configs, dependencies and status. Avoid empty placeholder folders or individual per-building managers.

### Building categories and systems

| Player-facing building category | Example structures | Shared systems it may use |
| --- | --- | --- |
| Core | Citadel/CPU | Health, combat, power, resources |
| Defense | Offensive/repair towers, static shields, walls | Combat, power, targeting, repair |
| Energy | Generator, solar, wind, relay/pylon, battery | Power network, health |
| Extraction | Copper/iron/silicon mine | World deposits, power, production, physical inventory, logistics |
| Economy | Credit generator | Credit balance, optionally power |
| Industry | Refinery, fabricator, ammo plant | Production, power, inventory, logistics |
| Infrastructure | Storage, cargo hub, construction support | Inventory, logistics, building placement |

**These categories classify the buildings in code and the build menu, not their exclusive functions.** Buildings may opt into multiple systems. A repair tower is still a `Defense/` building; a solar panel is an `Energy/` building that participates in a network; a copper mine is an `Extraction/` building that consumes power and produces physical inventory. The Citadel remains under `Core/` even if initially not buildable. Store progression/unlock requirements in each building definition, not in category-wide hard-coded checks. One mining building per ore can gate progression without separate duplicated miner engines.

**Resource types:** (1) **Credits** are currency credited/spent through resource accounting, potentially generated per second by an Economy building; (2) **Power** is real-time connected generation/demand, with batteries/storage but not a cargo item; (3) **Physical ores/materials** (e.g. copper, iron, silicon) are mined, held in hoppers/storage, transferred by logistics and consumed by recipes. Distinguish world ore deposits (`WORLD/Resources/`) from owned resource quantities (`FORTRESS/SYSTEMS/Resources/`).

**Ownership and performance:** WORLD owns the grid/occupancy and navigation; BUILDINGS uses it for placement. ENEMIES owns target selection/AI. Root COMBAT owns shared damage rules. FORTRESS/SYSTEMS owns reusable base mechanics; no one giant fortress manager. Root UI owns presentation/interaction, not gameplay state. Root DEBUG owns metrics/stress tooling, while UI/DebugPanels owns F1 panels. Keep caches, bounded work, event-driven UI, staggered AI, power topology invalidation and storage/logistics reservations performance-conscious.


## Shared Building scene and artwork contract (BF0/BF1)

**BF0 Source Implemented / Awaiting Owner Validation:** shared Building scene/runtime, definition, registry and atomic placement exist, demonstrated by an inherited DEBUG test fixture. BF1 inherited Citadel/Wall001 are now source implemented; Basic Level1 Tower001/projectiles are now source implemented. Every structure, including Citadel, walls, offensive/repair towers, generators, miners, credit earners, factories and storage, inherits the same **`FORTRESS/BUILDINGS/SHARED/Building.tscn`** with the lightweight `Building.cs` foundation. This is the user's preferred naming, replacing earlier proposed `BuildingBase.tscn`/`BuildingBase.cs`. Individual placeable inherited scenes (e.g., Tower001.tscn) are instantiated into the world; they must preserve this shared lifecycle.

```text
FORTRESS/BUILDINGS/
  SHARED/
    Building.tscn             # canonical inherited building scene
    Building.cs               # event-driven root, health, footprint, selection
    BuildingDefinition.cs     # stable ID/category/scene/footprint
    BuildingRegistry.cs       # valid building references for target lookup
  Core/Citadel/Citadel.tscn
  Defense/Walls/Wall001/Wall001.tscn
  Defense/OffensiveTowers/Tower001/Tower001.tscn
```

**Visual authoring conventions:** A concrete building can have an optional non-moving `BaseSprite` (building body/foundation), independently rotating `RotatingParts` (turret, barrel, muzzle marker), and optional `AnimatedParts` made from **baked animation sprites/sprite sheets** for small details such as fans or moving machinery. These are **optional attachments under the shared scene's Visuals anchor**, not mandatory components or processing scripts for every building. Turret rotation must never rotate the static base or change the placement footprint. Preserve sensible draw order and aligned origins.

**Future construction presentation:** Keep room for a distinct optional `ConstructionVisuals` layer and staged construction sprites similar in spirit to `godot-tower-defense`, but explicitly defer their implementation until a construction pass. Building health, identity, multi-cell occupancy and placement must work without them.

No automatic per-building processing; passive static visuals incur minimal overhead. Different capabilities (tower weapons, power, storage, mining, repair, construction) should be opt-in without an enormous universal building controller. Follow the detailed acceptance and BF0/BF1 prerequisites in [INITIAL_STRESS_NAVIGATION_PLAN.md](INITIAL_STRESS_NAVIGATION_PLAN.md). Avoid separate tower-only scene roots.

## Enemy architecture — ST1 basic runtime, later modular expansion

**Status:** ST1 basic Enemy001, cached CPU/breach targeting, contact combat, continuous safe movement, bounded FIFO path requests and gradual edge spawner are Source Implemented / Awaiting Owner Validation. Advanced families and the broader FD4/FD5 scope remain planned. Read `ENEMIES/README.md` and individual ownership READMEs before implementing.

- **CORE:** Small enemy entity/runtime, identity, state, spawn and cleanup. Avoid a huge all-in-one Enemy controller and avoid a script/timer for every decoration.
- **DATA + TYPES:** `EnemyDefinition` (future C# Resource and .tres presets) expresses an enemy's base identity, visuals, vitals, targeting profile, movement, attack, rewards, modifiers and abilities. `TYPES/` contains concrete presets/scenes grouped by actual future family, *not* one duplicated behavior engine per class.
- **AI:** Separates target selection (CPU/player/building priorities, visibility, reachability), orders (e.g. focus a miner/tower/player) and tactical behaviors (standard, pursue, standoff, orbit, anchor beam, support). No full-scene scan for every enemy every frame.
- **MOVEMENT:** Ground/flying/underground movement, obstacle/phase rules, steering and standoff positions. `WORLD/` owns terrain, occupancy and reusable navigation services.
- **ATTACKS:** Contact, regular/triple projectile, sniper, continuous beam, rocket/siege and kamikaze ram/explosion. Actual shared damage, hit handling and generic projectile systems belong to root `COMBAT/`.
- **ABILITIES:** Phasing, splitting, transporting, regeneration, ally shield support, explosions and future special mechanics. Complex abilities have opt-in configuration/state; do not make a single huge flags switch.
- **MODIFIERS:** Optional Shielded/Stealthed/Armored/Elite traits; flags are useful for classification, while actual mutable health/shields, reveal states and cooldowns live in runtime components.
- **SPAWNING:** Staggered, capped enemy creation and contextual formations; wave definitions and scenario authoring may live in `LEVELS/`.
- **VISUALS + PERFORMANCE:** Cost-conscious vector drawing, cosmetic culling independent of simulation, bounded AI work, spatial lookups and accurate counters. Do not claim a 200-enemy benchmark based only on stationary shapes.

**Reference roster confirmed in GameMaker `VECTOR_TD_2026` enemy data:** weak seekers and MK2; building hunters and MK2; single/triple shooters and sniper; phaser; kamikaze; splitter/child; brute/berserker; flyer/heavy flyer/gunship; transporter MK1/MK2 and flying transporter; shield generator; siege beam/rocket; centipede head/child. Old enums define ground/flying/underground layers, contact/projectile/continuous beam attacks, abilities (phasing, explode, split, transport, regenerate, shield allies), modifiers (shielded, stealthed), tactical orders (player/miner/tower) and standoff/orbit/support behaviors. These are **references**, not proof that every old capability was finished.

**Composition examples:** Siege Beam = heavy preset + standoff/anchor AI + beam attack + optional shield modifier. Kamikaze Arrow = fast flying preset + structure target priority + ram/explosion. Phaser Hunter = hunter preset + phasing ability + optionally stealth. Keep behavior and targeting separate from art shape. Add unique boss-specific scripts only when necessary.

**Codex FD4 implementation order:** one runtime enemy, one definition, one vector visual, basic target/move/attack contracts, bounded spawning; then stress harness. Add categories and modules only as concrete enemy variants require them. **Do not create an ECS or 11 separate manager nodes just because the directories exist.**

## Performance architecture and measurable goals

| Area | Early design contract | What to instrument later |
| --- | --- | --- |
| Simulation | Separate gameplay state from drawing; avoid each entity owning many active scripts/timers; use fixed physics updates only for work that needs them | Alive/active/dormant enemy counts, simulation time |
| AI | Data-defined target priority (CPU / structures / player), detection and line of sight; stagger expensive decisions, respond to target destruction | Target scans, AI updates, retarget queue, AI cost |
| Navigation | World-owned occupancy and updates; ground/flying/phasing can differ; avoid per-frame path requests, cache and invalidate when geometry changes | Repath requests, executed/queued, navigation cost |
| Rendering | Minimal vector geometry, avoid redundant per-entity draw/canvas work; cull **cosmetics** when invisible without disabling necessary gameplay | Visible counts, effects/draw load, renderer time when available |
| Combat | Lightweight shared hit resolution; sensible collision layers/masks; avoid unbounded projectile spawn and per-hit allocations | Active projectiles, hit processing, projectiles/effects peaks |
| World generation | Reproducible seed, staged pipeline, valid CPU/buildable space, navigable objective approaches, separate world data and visuals | Generation duration, placement retries, navigation revisions |
| Spawning | Bounded batches, caps, gradual pressure; never do a large unbounded spawn burst on a single frame | Spawn count/queue, spawned per frame, alive cap |
| Lifecycle | Proper detach, cleanup, cancellation and idle disable for dead/offscreen/unused entities | Retained enemies/effects, active timers, allocations |

**Important:** Offscreen enemies may still need to damage buildings or approach the CPU. Treat **render culling** separately from **simulation throttling**; never change combat outcomes merely because an enemy is off camera. Measure cost before introducing exotic batching or pooling. Reuse built-in Godot systems where they are sufficient.

### Benchmark scenario, later in FD4

A reproducible **stress scenario** should include at least 200 active enemies with a mix of flying/ground and targeting behaviors, movement around meaningful obstacles, CPU/building/player target selection, tower fire, projectiles and some effects. Record map seed, enemy composition, tower/building counts, viewport/resolution, hardware, mean/low FPS or frame-time percentiles and spikes, build type, and profiler observations. Do **not** call 200 idle sprites a successful stress test. If target is missed, identify the measured bottleneck and make a small targeted improvement; do not prematurely rewrite the simulation.

**Minimal testing policy still applies:** Codex should implement the instrumentation and simple *manual* stress controls, but the owner runs prolonged FPS benchmarks and editor acceptance checks unless specifically requested otherwise.

## WG0 — Level001 procedural world foundation

**Status: Source Implemented / Awaiting Owner Validation.** WG0 is separately authorized ahead of full ST4 fog/benchmark work. `LEVELS/Level001/Level001.tscn` inherits shared LevelBase; LevelController now composes existing services for both production and DEBUG. Defaults: 63×63 cells at64px (4032px square), approximate22% black dead clusters,12 growth seeds,8-cell CPU clearing, protected edge band and traversable reserved player marker. Startup validates CPU footprint/approaches and edge reachability; at most6 random candidates plus1 deterministic repair are validated. F1 exposes seed/coverage/attempt counts and new/same-seed full-scene regeneration. Terrain collision/rendering uses merged exact solid rectangles, not per-cell scenes. Level-local Enemy001 range40px supports64px cells; existing spawning/path/breach code is reused.

See `../LEVELS/Level001/README.md`, `../WORLD/Generation/README.md` and `../WORLD/Terrain/README.md`. Source/diff and scene-reference review only; no Godot/dotnet available for a build/runtime check or FPS claim. No player controller, deposits, water/lava, biomes, fog, streaming, campaign progression, four pressure modes or towers were implemented. DEBUG remains the F5 stress sandbox; Level001 runs with F6. This does not mark full FD2/ST4 or a campaign accepted.

## Current focused-track implementation

BF1/ST1 has a real CPU-defense sandbox, inherited Citadel and Wall001, one arrow ground seeker, contact damage, accessible-boundary breach fallback and a minimal cooperative path queue. Root `DEBUG/README.md` lists exact defaults, controls, config paths and owner checks. Basic F2 stats/manual spawns and Level1 Tower001/projectiles are now source implemented; advanced ST2 priorities/profiling and benchmark acceptance remain planned. No build/runtime/FPS claim: Godot/dotnet unavailable; source/diff review only. The broader FD phases below are not marked accepted by implementing this focused slice.

## Phased implementation contract

For each pass: inspect `README.md`, `NOTES/CodePreferences.md`, any `AGENTS.md`, relevant source and ownership docs; implement **only** the authorized pass; update feature READMEs and this plan's status; source-review and at most one practical build for substantive code; commit/push on the authorized branch unless told otherwise. Do not change the two reference repos. Report paths, commit SHA, controls/config knobs and owner checks.

### FD0 — Foundation and bootable project

**Status: Planned**

**Goal:** A small, bootable foundation with clean ownership and no speculative gameplay engine.

- Confirm Godot 4 C#/.NET project setup and launch path, current actual repo contents.
- Add only immediately necessary umbrellas: `SYSTEMS/`, `WORLD/`, root `UI/` or `CAMERA/` as needed, plus succinct feature README(s). Existing `NOTES/` remains the plan authority.
- Add a minimal main scene/session/level handoff with a blank vector-styled battlefield and camera or viewport framing.
- Establish a single place for game/session state and a few Inspector-exported settings; do not create empty configs for every future subsystem.
- Set up logical entry points so later player/CPU/towers/world modules can be composed, not hard-coded into one controller.
- **Not in scope:** procedural terrain, enemy logic, buildings, production, complex save system, universal event bus or large asset pipelines.

**Acceptance:** Godot can launch a blank playable/test scene; there is a clearly owned entry point; no redundant controllers; documentation accurately distinguishes existing vs proposed systems.

### FD1 — Performance observability and debug controls

**Status: Planned** · **Depends on FD0**

**Goal:** Expose performance before gameplay complexity appears.

- Add `DEBUG/` for instrumentation and root `UI/DebugPanels/` for an **F1** debug panel; show FPS, frame milliseconds, live enemies, visible enemies and projectiles, plus optional tracked subsystem timings.
- Distinguish **Godot-reported frame timing** from any custom **measured code-section timing**; label missing counters as unavailable rather than fabricate values.
- Add cheap counters/hooks for future enemy/spawner/combat owners; no polling over the entire scene each frame solely to display counts.
- Keep debug work dormant when closed and use bounded/throttled text refreshes; a persistent tiny FPS readout is optional via config.
- Provide a foundation for later debug spawn controls but **do not invent a functioning 200-enemy spawner before enemies exist**.

**Acceptance:** F1 toggles panel, measurements are truthful/labelled, overlay itself has negligible idle cost, and no full battle benchmark is claimed.

### FD2 — Deterministic open battlefield

**Status: Planned** · **Depends on FD0; use FD1 metrics**

**Goal:** Finite, seed-based procedural top-down battlefield, separate from Classic TD corridor generation.

- Add `WORLD/Generation`, `Grid`, `Navigation`, `Scenes` and `LEVELS/` only as justified by real code.
- Implement seeded generation of traversable terrain, obstacle regions and clear CPU/buildable zones; **resource deposits may initially be visual markers or postponed**, not yet a full economy.
- Grid/occupancy is authoritative for construction and navigation; distinguish logical world cells from visuals.
- Expose seed/map size/obstacle density and key generation controls using an Inspector-friendly master config once helpful.
- Check CPU spawn clearance, useful buildable area and basic reachability of approach regions; avoid unbounded regeneration loops.
- Start with a finite single map and sensible generation budgets; **no streaming or infinite-world system in this pass**.

**Acceptance:** Same seed/settings reproduce layout, changing seed visibly changes map, objective/build space is sensible, and a base navigation query can use the world data.

### FD3 — Player chassis and central CPU

**Status: Planned** · **Depends on FD2**

**Goal:** Move freely in a world that has a defendable objective.

- Add lightweight vector player chassis, movement/camera controls and a central CPU/health state. The CPU's own scene/data belongs under `FORTRESS/BUILDINGS/Core/` (create only needed folders); shared damage logic stays in root `COMBAT/`.
- Introduce a compact `COMBAT/` foundation for generic damage/health ownership; no full shield/resistance hierarchy yet.
- Ensure player and CPU lifecycle, death and future save identity are clearly separated.
- Keep drawing/lightweight player input distinct from future enemy coordination.

**Acceptance:** Player can navigate the generated battlefield, CPU can receive damage through a stable interface, camera follows as intended, and no tower/economy complexity is required.

### FD4 — High-count enemy foundation and stress harness

**Status: Planned** · **Depends on FD3**

**Goal:** Establish high-count enemy representation **before** introducing a large enemy catalog.

- Use the already scaffolded `ENEMIES/CORE/`, `DATA/`, `TYPES/`, `AI/`, `MOVEMENT/`, `ATTACKS/`, `SPAWNING/`, `VISUALS/` and `PERFORMANCE/` ownership. Implement one data-driven geometric enemy with extensible movement/attack/target-role fields; opt into `ABILITIES/` and `MODIFIERS/` only when needed.
- Implement only enough behavior to test **target selection among CPU, player and buildings (when buildings exist)**. Until FD5, use lightweight target dummies where necessary.
- Shared/staggered target decisions, bounded path requests, cheap geometric rendering and robust spawn/despawn bookkeeping; ground vs flying only when justified for the test.
- Add bounded stress controls via F1 for **10 / 50 / 100 / 200** active enemies, cleanup/reset and seed consistency. Do not spawn unbounded bursts.
- Include active movement and target switching in the scenario; recognize that full combat stress validation is not possible until FD5.
- Inspect performance counters, but leave prolonged benchmarks to the owner.

**Acceptance:** Spawn/reset controls work, counts are accurate, enemies visibly move toward targets and retarget reasonably, and manual stress testing is possible. **No claim of 200-enemy production performance until FD5 battle scenario has been measured.**

### FD5 — First playable fortress defense loop

**Status: Planned** · **Depends on FD4**

**Goal:** A small actual game loop: **player + CPU + buildable tower/wall + attacking enemies**.

- Add the shared placement/health functionality under `FORTRESS/SYSTEMS/`, durable wall/CPU under `FORTRESS/BUILDINGS/`, and one tower under `FORTRESS/BUILDINGS/Defense/OffensiveTowers/`. Put its construction menu and inspector views in root `UI/`.
- Make enemies target CPU, structures and visible player according to configurable roles; add basic ranged and/or kamikaze attack as a small initial roster.
- Implement basic player/tower weapons, projectiles/hits and CPU loss condition; expose relevant Inspector settings.
- Make navigation react to building placement/destruction without per-frame global path rebuild.
- Expand FD4 stress harness into representative enemies + towers + projectiles + damage/effects, then record owner-tested results and targeted bottleneck fixes.
- **Not in scope:** full economy, refinery, all chassis, complex logistics, campaign, elaborate save/load or the full Vector TD 2026 roster.

**Acceptance:** Build tower/wall, enemies choose and damage valid targets, player can participate in defense, CPU loss ends the scenario, and a meaningful stress-test scene is available for owner profiling.

## Post-FD5 expansion (uncommitted roadmap, not authorized work)

- **Infrastructure:** Carbon, Silicon and Copper resource deposits, storage, local generators/batteries/relays and connected power.
- **Production:** miners, refineries, fabricators, ammunition inventory and cargo-drone delivery.
- **Combat content:** diverse geometric enemy behaviors (kamikaze flyers, ranged structure attackers, player hunters, support units), tower ranks/upgrades, shields, damage types and richer feedback.
- **Persistence/progression:** meaningful save/load, scenarios, player chassis variants, campaign/profile progress.

## Codex handoff checklist

1. Verify the requested **FD phase** and repository/branch.
2. Read this plan, local preferences, root README, any AGENTS instructions and real files. Follow ownership rather than copying old GML literally.
3. Implement **one phase**, with reusable defaults and proportional performance safeguards. No empty “future architecture” scaffolding.
4. Document new folders in their feature READMEs; update status of only completed work. Distinguish **Source Implemented** from **Awaiting Owner Validation** and **Accepted**.
5. Keep validation short per owner preference. For docs-only edits, diff review is sufficient. No extended stress tests unless expressly requested.
6. Commit and push authorized edits by default; list files/commit and what must be checked manually in Godot.

## Basic Tower001 combat slice — Source Implemented / Awaiting Owner Validation

The explicitly authorized Level1 Battle-style tower is now implemented on the shared Building scene, with local original base/gun SVG copies, an independent turret/muzzle, Inspector config, T/W placement and shared straight projectile/closest spatial targeting. Existing enemy health and cleanup are reused; the registry now indexes movement once per bucket transition. RootCombat/Projectiles is shared in LevelBase and DEBUG. This does not implement the excluded tower economy/heat/ammo/power/progression features, advanced ST2 scheduling or a measured 100–200-enemy benchmark. See Tower001 and COMBAT READMEs for source scope/owner checks; build/runtime unavailable here.
