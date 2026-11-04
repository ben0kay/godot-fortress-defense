# Fortress Defense — Foundation & Performance Roadmap (FD0–FD5)

**Repository:** `ben0kay/godot-fortress-defense` · **Branch:** `main`  
**Status:** **Planned** — this document is an implementation brief for Codex, **not** evidence that its systems already exist.  
**Gameplay reference:** `ben0kay/VECTOR_TD_2026` (GameMaker/GML).  
**Architecture/style reference:** `ben0kay/godot-tower-defense` (Godot 4 C#).  
**Authoritative local guidance:** root `README.md`, `NOTES/CodePreferences.md`, `AGENTS.md` if present, and the actual owning source files.

## Intent and non-negotiables

Build a **separate**, expandable 2D vector-style fortress-defense/action/base-management game. The player controls a mobile combat chassis and defends a central **CPU**, placing towers, walls and eventually resource/production buildings on a **seed-generated open battlefield**. Different geometric enemies can independently attack the CPU, structures or visible player, with role-specific movement and combat. No fixed-path Classic TD model or copied Classic TD enemy roster.

**Performance is an architectural constraint from day one**, not a late polish task. Target a representative encounter with **100–200 active, visible, moving, targeting and attacking enemies** plus towers/projectiles/effects at **60 FPS (~16.7 ms/frame)** on a declared baseline PC. This is a **target**, not a current capability or guarantee. Measure both normal frame time and spikes. Performance budgets must account for *all* gameplay, rendering and GPU costs, not merely AI timings.

Do **not** turn the project into a speculative custom engine. Begin with simple Godot 4 C# facilities and optimize measured bottlenecks. Preserve distinct vector visuals: arrows/triangles, rotating circles, striking geometric silhouettes and lightweight effects. The old Vector TD project's enemy/navigation/culling/spawn patterns are **references**, not performance proof; do not reproduce GameMaker-specific event hierarchies blindly.

**Do not implement FD0–FD5 all at once.** Each pass requires owner authorization, focused diffs, appropriate documentation, one commit/push by default and **minimal AI testing** as required by `CodePreferences.md`.

## Proposed eventual folder structure

These are **ownership destinations, not folders to pre-create empty in FD0**. Create each umbrella as the corresponding feature starts; include a short `README.md` and a feature `*MasterConfig`/default `.tres` **when enough tunable settings justify it**. Names below are illustrative; inspect existing source before adding parallel owners.

```text
godot-fortress-defense/
├── project.godot
├── README.md
├── AGENTS.md                         # optional; create only if useful
├── NOTES/
│   ├── CodePreferences.md
│   └── FOUNDATION_PERFORMANCE_PLAN.md
│
├── SYSTEMS/
│   ├── README.md
│   ├── GameSession/                   # boot, game state, pause, transitions
│   └── Persistence/                   # future save/load contracts
├── DEBUG/
│   ├── README.md
│   ├── Performance/                   # F1 counters and samples
│   └── StressTools/                   # bounded manual test spawning
├── WORLD/
│   ├── README.md
│   ├── Generation/                    # seed, stages, generation settings
│   ├── Grid/                          # authoritative cells and occupancy
│   ├── Navigation/                    # navigation queries / dirty revisions
│   ├── Resources/                     # world deposits, not economy totals
│   └── Scenes/                        # battlefield scene and world visuals
├── LEVELS/
│   ├── README.md
│   └── Scenarios/                     # game-mode rules and wave content
├── PLAYER/
│   ├── README.md
│   ├── Chassis/
│   ├── Weapons/
│   └── Scenes/
├── CAMERA/
│   └── README.md
├── ENEMIES/
│   ├── README.md
│   ├── Data/                          # enemy definitions / inspector resources
│   ├── Simulation/                    # lifecycle and coordinated updates
│   ├── AI/                            # role, target selection and combat
│   ├── Movement/                      # flying / ground / steering behaviors
│   ├── Rendering/                     # vector silhouettes and VFX adapters
│   └── Spawning/
├── BUILDINGS/
│   ├── README.md
│   ├── Construction/
│   ├── CPU/                            # central objective
│   ├── Walls/
│   └── Infrastructure/
├── TOWERS/
│   ├── README.md
│   ├── Data/
│   ├── Targeting/
│   └── Weapons/
├── COMBAT/
│   ├── README.md
│   ├── Damage/
│   ├── Shields/
│   └── Projectiles/
├── ECONOMY/
│   ├── README.md
│   └── Resources/                     # currency/material ownership & transfer
├── POWER/
│   └── README.md
├── PRODUCTION/
│   ├── README.md
│   ├── Mining/
│   ├── Refining/
│   └── Fabrication/
├── LOGISTICS/
│   ├── README.md
│   └── CargoDrones/
├── FEEDBACK/
│   └── README.md
├── UI/
│   ├── README.md
│   ├── HUD/
│   └── Inspectors/
├── AUDIO/
│   └── README.md
└── TESTS/
    └── README.md
```

**Ownership rules:** WORLD owns terrain/grid/building occupancy and reusable navigation data. ENEMIES owns enemy behavior and calls WORLD services. BUILDINGS owns durability/placement of ordinary structures, TOWERS owns turret-specific behavior, COMBAT owns hit and damage resolution, and PLAYER owns the chassis. ECONOMY owns quantities/transfers; PRODUCTION converts materials; LOGISTICS moves physical goods; POWER distributes energy. DEBUG observes state; it must not become the game's authoritative gameplay manager. UI displays state and sends actions, and FEEDBACK owns cosmetics.

**Keep scene trees lean:** plain node containers for organization; scripts/process callbacks only when needed. Avoid one script per visual piece, needless autoloads and a huge all-purpose controller. **Don't build a generic ECS, multithreading scheduler or custom physics engine without profiler evidence.**

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

## Phased implementation contract

For each pass: inspect `README.md`, `NOTES/CodePreferences.md`, any `AGENTS.md`, relevant source and ownership docs; implement **only** the authorized pass; update feature READMEs and this plan's status; source-review and at most one practical build for substantive code; commit/push on the authorized branch unless told otherwise. Do not change the two reference repos. Report paths, commit SHA, controls/config knobs and owner checks.

### FD0 — Foundation and bootable project

**Status: Planned**

**Goal:** A small, bootable foundation with clean ownership and no speculative gameplay engine.

- Confirm Godot 4 C#/.NET project setup and launch path, current actual repo contents.
- Add only immediately necessary umbrellas: `SYSTEMS/`, `WORLD/`, `UI/` or `CAMERA/` as needed, plus succinct feature README(s). Existing `NOTES/` remains the plan authority.
- Add a minimal main scene/session/level handoff with a blank vector-styled battlefield and camera or viewport framing.
- Establish a single place for game/session state and a few Inspector-exported settings; do not create empty configs for every future subsystem.
- Set up logical entry points so later player/CPU/towers/world modules can be composed, not hard-coded into one controller.
- **Not in scope:** procedural terrain, enemy logic, buildings, production, complex save system, universal event bus or large asset pipelines.

**Acceptance:** Godot can launch a blank playable/test scene; there is a clearly owned entry point; no redundant controllers; documentation accurately distinguishes existing vs proposed systems.

### FD1 — Performance observability and debug controls

**Status: Planned** · **Depends on FD0**

**Goal:** Expose performance before gameplay complexity appears.

- Add `DEBUG/` with an **F1** debug panel; show FPS, frame milliseconds, live enemies, visible enemies and projectiles, plus optional tracked subsystem timings.
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

- Add lightweight vector player chassis, movement/camera controls and a central CPU/health state.
- Introduce a compact `COMBAT/` foundation for generic damage/health ownership; no full shield/resistance hierarchy yet.
- Ensure player and CPU lifecycle, death and future save identity are clearly separated.
- Keep drawing/lightweight player input distinct from future enemy coordination.

**Acceptance:** Player can navigate the generated battlefield, CPU can receive damage through a stable interface, camera follows as intended, and no tower/economy complexity is required.

### FD4 — High-count enemy foundation and stress harness

**Status: Planned** · **Depends on FD3**

**Goal:** Establish high-count enemy representation **before** introducing a large enemy catalog.

- Add `ENEMIES/` with data-driven definition for first geometric variant and extensible movement/attack/target role fields.
- Implement only enough behavior to test **target selection among CPU, player and buildings (when buildings exist)**. Until FD5, use lightweight target dummies where necessary.
- Shared/staggered target decisions, bounded path requests, cheap geometric rendering and robust spawn/despawn bookkeeping; ground vs flying only when justified for the test.
- Add bounded stress controls via F1 for **10 / 50 / 100 / 200** active enemies, cleanup/reset and seed consistency. Do not spawn unbounded bursts.
- Include active movement and target switching in the scenario; recognize that full combat stress validation is not possible until FD5.
- Inspect performance counters, but leave prolonged benchmarks to the owner.

**Acceptance:** Spawn/reset controls work, counts are accurate, enemies visibly move toward targets and retarget reasonably, and manual stress testing is possible. **No claim of 200-enemy production performance until FD5 battle scenario has been measured.**

### FD5 — First playable fortress defense loop

**Status: Planned** · **Depends on FD4**

**Goal:** A small actual game loop: **player + CPU + buildable tower/wall + attacking enemies**.

- Add `BUILDINGS/` placement/occupancy, durable wall/CPU and one reusable tower under `TOWERS/`.
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
