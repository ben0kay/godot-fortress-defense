# Fortress Defense — Initial Navigation & 200-Enemy Stress-Test Plan (ST0–ST5)

**Status:** Planned (documentation only; no gameplay coded by this plan)  
**Owner repo / branch:** `ben0kay/godot-fortress-defense` / `main`  
**Read first:** `AGENTS.md`, root `README.md`, `NOTES/CodePreferences.md`, `NOTES/FOUNDATION_PERFORMANCE_PLAN.md`, `ENEMIES/README.md`.  
**References:** `ben0kay/VECTOR_TD_2026` `scripts/scr_navigation/scr_navigation.gml`, `scr_enemy_data`, `scr_enemy_behavior`, `scr_enemy_spawner` and world/grid scripts; Godot 4 `AStarGrid2D` documentation. Do **not** modify reference repositories.  
**Authority:** This is the focused first-battlefield/stress-test implementation plan. The broader FD0–FD5 roadmap still describes the full product; **ST phases are incremental implementation slices, not permission to implement all FD phases at once**.

## Objective: measure a real battle early

Create a finite top-down sandbox with:
1. A placeholder central **Citadel/CPU** with a defined building footprint and health.
2. One placeholder **enemy** (simple vector arrow/triangle) with health, collision radius, targeting and movement.
3. One placeholder **offensive tower** with building footprint, health, acquisition range, simple fire/damage and cheap visuals.
4. Configurable **edge-of-map spawning**, increasing through 10, 50, 100 and 200 live enemies.
5. **Static solid obstacles** and **placeable/removable solid walls** that cause actual navigation changes.
6. Ground enemies that seek the Citadel **only while reachable**, then choose and attack reachable buildings/walls if every Citadel approach is blocked; flying enemies later use a different traversal rule.
7. Deterministic map layouts and controlled, owner-run performance observation.
8. Foundation for **three fog-of-war states**: currently visible, previously explored but not visible, and never explored.

**Performance goal:** 100–200 genuinely active enemies moving around obstacles, selecting targets and participating in combat at target 60 FPS / ~16.7 ms total frame time on documented hardware/settings. Not guaranteed. A test of 200 stationary sprites does not count. Avoid broad optimization/benchmark sessions by Codex; the owner will run gameplay/FPS tests and report results.

**Initial scope discipline:** A temporary deterministic obstacle layout is fine; full procedural biome/ore generation, resource processing, full building menu, complete fog gameplay and player combat belong to later phases unless specifically authorized. The **navigation data contract should support future procedural generation** without redesign.

## Navigation decision: authoritative cell grid + A* paths + lightweight steering

### Why this choice

Old `VECTOR_TD_2026` used GameMaker `mp_grid` with ground, breach/phasing and flying navigation layers, and staggered repath timers. Preserve that **design intent**, not GameMaker APIs.

**Recommended baseline for Godot 4 C#:**
- `WORLD/Grid/`: one authoritative **integer-cell occupancy model**; `Vector2I` cell IDs, configurable cell size, world-to-cell and cell-to-world transforms, footprint origin/size/rotation rules.
- `WORLD/Navigation/`: `AStarGrid2D` driven by blocked ground cells and appropriate actor clearance; path requests are a **bounded service**, not per-enemy autonomous global searches.
- `ENEMIES/MOVEMENT/`: enemies move continuously along path waypoints in world coordinates using simple movement/turning/arrival smoothing; movement every necessary physics tick, strategic AI at lower frequencies.
- Steering/short-range separation can be introduced after movement works **if crowds visibly bunch or jitter**. Steering by itself cannot reliably determine Citadel reachability around walls, so don't use it as the sole routing method.
- A shared goal-distance/flow-field or connected-component reachability cache **may become beneficial** with hundreds of identical Citadel seekers. Consider after profiling; don't prematurely build an elaborate multi-grid engine.
- Do not use one `NavigationAgent2D` with avoidance enabled per enemy by default. Godot navmesh/agent avoidance is a valid alternative to compare if the chosen cell grid performs poorly or the level topology changes; it is not automatically the cheaper option.

**Godot API warning:** `AStarGrid2D.SetPointSolid` changes solidity without requiring `Update()`; `Update()` is for grid parameter changes and can clear point data. Verify exact C# names against the installed Godot version. `GetPointPath` is not documented as safe for concurrent access on the same grid; keep path queries on one owning thread until a safe measured alternative is justified. Avoid experimental threading in the first prototype.

### Grid / footprints contract

**One grid owns the truth:** generated terrain solids + placed-building footprints + explicit dynamic occupancy/reachability revisions. The visual TileMap, collisions and navigation **must agree**; a drawn obstacle without a blocked footprint is a bug.

Define an Inspector-exposed `WorldGridConfig` when implementation begins:
- `CellSize` (choose/tune only after inspecting camera scale, approximate building sizes and enemy collision radii; no ungrounded global fixed value).
- `MapWidthCells` / `MapHeightCells`.
- `AllowDiagonalMovement` and corner-cutting policy (prevent diagonal squeezing between touching solids).
- `GroundClearanceCells` or size class based on agent radius. A 1-cell path between walls is **not** traversable by an enemy wider than its clearance.
- `Seed`, prototype obstacle preset and initial CPU clearing dimensions.

**Required from the first implementation: structures are NOT restricted to 1×1 cells.** Every building definition specifies a configurable integer **WidthCells × HeightCells** footprint (e.g., 1×1 wall, 2×3 tower, 3×4 refinery, 5×5 CPU; sizes illustrative). A placement anchor/origin determines which grid cells are occupied; rectangular asymmetric footprints and future orientation/rotation rules must be considered without hard-coding a single size. A building may be rendered larger or smaller than its actual navigation footprint, but its logical occupied cells must remain accurate. Placement must validate **every** cell in the footprint against map bounds, existing terrain solids, other buildings, deposits and placement restrictions; reserve/release **all** cells atomically. Destruction/removal updates all previously occupied cells and increments one topology revision, not one global recalculation per cell. Keep a cell-to-building reference for selecting/damaging the actual structure when an enemy attacks a blocked perimeter. Building collisions and enemy clearance must match this multi-cell occupancy model. Do not silently treat a multi-cell structure as a center point or a 1×1 obstacle.

**Target approach cells:** CPU and towers usually occupy blocked cells. Never ask A* to route into the solid center of the CPU; select a reachable perimeter/attack position appropriate for the enemy's attack radius and footprint. The same holds for walls/obstacles chosen as fallback targets.

**Map changes:** New walls, demolished walls and procedural obstacle changes invalidate relevant cached reachability/path data. Queue expensive follow-up work with stagger/budget; avoid doing 200 full repaths synchronously during a build action. Immediate locally necessary state change is allowed, but queue costly searches.

### Dynamic nav and Citadel fallback behavior

Ground Citadel-seeker decision algorithm:
1. Attempt/reuse knowledge of whether a path to a legal CPU approach cell exists from the enemy's **current connected region**, accounting for footprint clearance and current topology revision.
2. **Reachable:** keep CPU as strategic target; follow cached path/next waypoint, update only on invalidation, meaningful target movement or bounded refresh.
3. **Unreachable:** choose a nearby **reachable and damageable blocking wall/building** (prefer strategic breach choices; nearest visually isn't necessarily best), path to an attack position and attack it.
4. When the wall/building is destroyed, topology revision changes; enemy may reconsider the CPU and repath.
5. If no reachable attackable target exists, wait/reposition or follow a defined fallback; **do not spin in endless zero-path retries**.
6. Enemy roles with other target priorities (miner hunters, tower attackers, player hunters, siege attackers) may use different target profiles. Not every enemy is a CPU seeker.
7. **Allow the player to seal the Citadel** for this prototype. Do not silently forbid wall placement just to ensure a CPU path; the intended fallback behavior must be tested.

**Important nuance:** A cached unreachable result is only valid for a particular topology revision **and relevant starting region/agent clearance**. When a moving enemy crosses regions or topology changes, revisit as necessary. Prefer a cheap shared connectivity/flood-fill cache for many CPU-bound ground units if measurements show per-enemy A* reachability checks are too expensive. The CPU's impassable footprint is distinct from an accessible perimeter.

**Future traversal classes and phasing requirement:** Ordinary Ground (terrain + built walls block), Flying (ignores ordinary ground blockers subject to later air restrictions), **Phasing Ground** (ignores **permitted building-occupied cells** such as walls/towers/structures while still respecting **natural solid terrain, map boundaries and any phase-proof blockers**), Underground (future). Phasing is a configurable enemy capability/traversal policy, **not** a separate duplicated enemy AI or an assumption that all ground obstacles disappear. Its optional ability flag changes path validity, actual physical collision/movement filtering and reachable target calculations consistently; a path that crosses a building must not cause physics collision to block the phasing enemy. Do **not** globally mark buildings walkable just to support one phasing enemy; ordinary ground enemies must remain blocked. Design traversal-specific nav queries/layers or derived grid views from the single authoritative WORLD occupancy data, updated on topology revisions; only materialize extra grids when the feature actually begins. A phaser should not automatically attack the surrounding walls when the CPU is enclosed if a legal phase-through path exists; ground non-phasers still breach walls. Never assume phasing bypasses terrain rocks or void cells.

## AI scheduler: strategic checks vs continuous movement

**Separate three classes of work:**

| Work | Scheduling | Principle |
| --- | --- | --- |
| Smooth movement, essential collision, fire cooldown when active | Normal necessary physics/simulation ticks | Do not make on-screen animation/movement visibly stutter |
| Target reconsideration, path validation, LOS, local threat scanning | Bounded and staggered low-rate decisions | No full map/scene scans per enemy every frame |
| Topology and target destruction reactions | Event marks agent(s) dirty; work goes to budgeted queue | Immediate correctness + controlled expensive recompute |

**Initial configurable decision-rate suggestions (not hard requirements):**

| Tier | Suggested strategic decision interval | Eligibility |
| --- | --- | --- |
| Visible & relevant | 0.20–0.33 s (3–5 decisions/sec) | In active camera and currently revealed, engaged or close enough to matter |
| Near offscreen / recently seen | 0.5–1.0 s | Just outside viewport or within near simulation band |
| Distant / concealed by fog | 1.0–2.0 s | Far away, no immediate combat event requiring response |
| Dormant/no valid work | Event/wake-based | Spawn pending, disabled effects, dead/despawned, absent targets |

**Corrections to keep explicit:** 5 updates/sec is *more frequent* than 3/sec; offscreen should **normally be less frequent**, not more. Screen visibility and fog exploration are **not equivalent to simulation relevance**. A hidden enemy currently attacking a building must keep dealing timely damage; don't allow reduced scheduling to change intended attack rate or AI outcomes. Give high-priority combat/threat changes an event-driven fast path. Tier parameters should be Inspector-tunable and testable.

- Stagger individual slots using stable spawn/instance IDs or assigned buckets, not one giant synchronous timer. Spread initial spawns and topology invalidations across frames using `MaxPathRequestsPerFrame`, `MaxTargetQueriesPerFrame` and request priority.
- No repeated `GetTree().GetNodesInGroup` or every-building scan for each enemy. Maintain registries/spatial search where needed; avoid a complex spatial index until actual bottlenecks justify it.
- Never let a low-frequency decision scheduler accidentally throttle movement speed, collision response or damage ticks.
- No busy-waiting and no allocation-heavy per-frame timers; keep relevant counters in `ENEMIES/PERFORMANCE/` with `DEBUG/` consuming them.

## Fog of war, exploration and rendering culling

**Three states per world cell** (future gameplay contract):
1. **Unexplored:** no terrain/building/enemy information drawn; black.
2. **Explored but not currently visible:** remembered terrain/structures in dim/desaturated gray; do not show current enemy positions or hidden changes through fog unless game rules explicitly allow memory markers.
3. **Currently visible:** full current terrain, structures, enemies, projectiles and effects.

Plan a root `WORLD/Visibility/` or `WORLD/FogOfWar/` owner only **when fog implementation is authorized**, with UI root HUD/minimap rendering its information. Track exploration separate from current visibility. Player/Citadel/sensors can be configurable sight sources later. Avoid one visibility raycast for every cell every frame; update from moving/revealing sources at bounded intervals/dirty areas.

**Rendering / simulation isolation:**
- For enemies outside viewport or completely hidden by fog, skip expensive vector drawing, decorative animations, particles and feedback where safe.
- Draw/visibility culling **must not delete or stop authoritative simulation** of enemy movement, targeting, damage, projectiles or wall damage.
- Offscreen collision/navigation remains correct. Enemy perceived visibility to player affects targeting/reveal rules separately from raw viewport position.
- Separate enemy-culling reason (outside camera / not revealed / far simulation band) from target line of sight and enemy stealth. Don't reveal stealth units solely because camera overlaps them.
- Dirty-update visual fog layers/geometry; avoid recoloring a huge map every frame.

**Scope:** ST prototype may initially implement only cheap viewport visibility / placeholder fog visualization. Complete explored vs current-visible state, sensors, minimap, memory of buildings and fog shader are later steps.

## Placeholder encounter and stress harness

**Initial scene** (naming suggestions, adjust to real repo ownership):
- `LEVELS/StressSandbox/` or `LEVELS/Scenarios/` entry/test scene.
- `WORLD/` temporary seed/preset or deterministic obstacle layout; grid-backed solids/clearings.
- `FORTRESS/BUILDINGS/Core/` simple static CPU with blocked footprint, health/damage receiver.
- `FORTRESS/BUILDINGS/Defense/OffensiveTowers/` one simple non-upgraded tower, same shared health/building model; optional 1-cell wall under `Defense/Walls/`.
- `ENEMIES/TYPES/Basic/` arrow-like ground seeker; `ENEMIES/CORE/` shared state; `ENEMIES/MOVEMENT/` waypoint movement; `ENEMIES/SPAWNING/` edge spawn.
- `COMBAT/` lightweight damage and projectile/hit resolution as needed (avoid full combat framework).
- `DEBUG/StressTools/` spawn controls/counters; `UI/DebugPanels/` F1 overlay.

**Edge spawner rules:**
- Configurable map-edge band(s), minimum separation from CPU/player, valid traversable spawn cells and per-step spawn cap.
- Sample/validate points with bounded retries and fallback search so spawn does not hang.
- Presets for 10 / 50 / 100 / 200 **simultaneously active** enemies; can gradually fill to target rather than spawn all in one frame.
- Toggle waves/continuous spawning, stop/reset, cleanup projectiles, elapsed/seed log and enemy role mix when later available.
- Track actual spawned/alive/visible/engaged, pending path/target requests, CPU health and wall breaches.

**Representative scenarios to manually check:**
A. Empty map: ground enemies reach CPU from multiple edges.  
B. Natural barrier with an opening: ground enemies route through opening; no diagonal clipping.  
C. Full wall enclosure: seeker recognizes CPU unreachable, approaches and destroys an accessible wall; resumes CPU pursuit when opening appears.  
D. Wall placed mid-fight: navigation invalidation and staggered repath with no mass spike.  
E. Tower defends CPU: 10 → 50 → 100 → 200 moving enemies with attack and effects.  
F. Offscreen/fog: hidden enemies remain dangerous but expensive visuals are culled; when revealed they appear correctly.  
G. Multiple enemy clearances/roles later: flying bypasses blockers; a **phasing ground enemy** may traverse permitted building footprints without crossing solid terrain, while ordinary ground enemies route or breach.\nH. Multi-cell construction: 1×1 wall, 2×3 tower and larger CPU block every logical cell; removal/destruction clears exactly the occupied cells; no partial clipping or false passable routes.

**Debug F1 / metrics** (real samples only): FPS, frame time, alive/visible/engaged enemy count, active projectile count, AI decision counts and duration, path requests pending/executed, navigation topology revision, enemy draw/cull counts, world seed, scenario and spawn cap. Clearly label unavailable timings or inferred totals; Godot engine frame time is not interchangeable with a custom timed C# section. Profiling should include frame-time spikes and GC allocations when meaningful.

## Codex execution phases

**These are sequential authorization slices**; each can be implemented without the full later economy/production systems. Keep existing FD0/FD1 foundation intent and reuse already completed work instead of replacing it.

### ST0 — Grid and navigation contract (no enemies yet)
**Status: Planned**
- Confirm project boot state and available Godot 4 C# API.
- Establish small `WORLD/Grid/` occupancy/footprint data and world/cell transformations; configurable test cell size; deterministic simple obstacle test scene.
- Single ground `AStarGrid2D` path service + valid approach cells, clearance policy, diagonal/corner policy.
- Expose one navigation revision / dirty invalidation event for dynamic footprint edits.
- No full procgen, rendering framework or enemy AI yet.
- **Owner acceptance:** can view/debug blocked vs walkable cells, verify a route around a fixed obstacle, place a 2×3 (or larger) footprint without overlap, and remove/destroy it to restore **all** occupied cells.

### ST1 — Citadel, wall and pathing enemy
**Status: Planned** · Depends ST0
- Placeholder CPU footprint and health, basic vector ground enemy, edge-of-map spawn, movement along waypoints.
- Central enemy registry with safe spawn/despawn, one target profile (CPU seeker).
- Path when reachable; when enclosed, fallback to reachable wall/building target and attack it; release footprint on destruction and recover route.
- Keep core movement responsive even while tactical choices are throttled.
- **Owner acceptance:** blocked-but-open route works around multi-cell obstacles; fully enclosed CPU triggers non-phasing ground enemies to attack a wall, then resume Citadel pursuit after breach.

### ST2 — Shared scheduling and profiler overlay
**Status: Planned** · Depends ST1
- F1 UI panels and DEBUG counters; configurable staggered AI and path query budgets.
- Camera/visibility-based decision tiers (default visible ~3–5/s, distant fog ~1/s) with independent reliable combat/movement.
- Cap synchronous path requests after wall placement, update revision and dirty queues. Add bounded 10/50/100/200 spawner with reset.
- Add cheap viewport cosmetic culling; no complete fog-of-war system yet.
- **Owner acceptance:** counter updates match actual entities and staggered workload is observable; 100/200 enemy scenes can be created for manual measurement. No benchmark success claimed by Codex.

### ST3 — Placeholder tower and combat load
**Status: Planned** · Depends ST2
- One tower with footprint, range, simple fire/projectile/damage; shared COMBAT behavior and destruction cleanup.
- One or a few walls/buildings, target priorities and active combat; same grid edit path as ST0.
- Stress conditions include tower shots, enemy attacks, AI switching and wall destruction, not merely silhouettes.
- **Owner acceptance:** 10/50/100/200 active simulated enemies and a tower operate together; owner reports representative FPS/spikes.

### ST4 — Procedural layout and fog foundation
**Status: Planned** · Depends ST3
- Seed-driven static terrain/obstacle patches and CPU clearing with valid spawn edges; no infinite map streaming.
- Introduce separate explored/currently-visible fog state and black/dim/full rendering; cull concealed enemy visuals without altering simulation.
- Avoid full-map per-frame raycasts or redraws. Give fog a dedicated config and DEBUG counters if useful.
- **Owner acceptance:** same seed yields same terrain; no obstacle/footprint disagreement; explored areas remain dim; unexplored areas black; unrevealed enemies hidden while simulations continue.

### ST5 — Baseline stress evaluation & targeted fixes
**Status: Planned** · Depends ST4
- Provide reproducible setup preset(s) including seed, map size, 200-enemy role mix where available, tower/building count, viewport and toggles for cosmetics/AI/navigation.
- Have owner measure 60 FPS / 16.7-ms goal, bad frame spikes, power/network/AI work only if those systems exist, and actual navigation/GC costs.
- Fix **identified** bottlenecks in focused later authorized changes: shared CPU reachability/flow field, narrower candidate indexes, vector batching, projectile pooling or simplified collision where measured beneficial.
- **Owner acceptance:** a repeatable benchmark and documented observed results or explicit unknowns, not a guarantee of target frame rate.

### Relation to existing FD0–FD5 roadmap

ST0–ST5 are the **concrete, early navigation/stress-sandbox track** spanning existing FD0–FD5 goals. Do not duplicate main-menu, camera, player, enemy or combat systems if already implemented in the FD track. In particular, the **full controllable player** from FD3 is **not required to begin stress testing**: leave a placeholder player target or omit it until authorized. This focused track may be performed ahead of complete production/gameplay features if the owner authorizes it.

## Development rules and reporting

- Inspect current source, `AGENTS.md`, `NOTES/CodePreferences.md` and this plan before every pass. No implementation beyond an authorized ST phase.
- Follow established folders: `WORLD/` owns grid/nav/fog; `ENEMIES/` owns behavior/spawn; `FORTRESS/BUILDINGS/` owns tower/CPU/wall scenes; `FORTRESS/SYSTEMS/` owns shared building placement/health; `COMBAT/` owns shared hit/damage; root `UI/` owns the F1 interface; `DEBUG/` owns measurements.
- Prefer Inspector-exported settings/master configs only for actually needed tuning controls. Don't add empty folders/managers or a speculative ECS.
- Documentation-only edits: read/diff sanity check, no build. Code changes: one basic build when practical; no extended validation unless the owner requests it.
- Update relevant README/status notes, make one focused commit and push by default unless told otherwise, then report exact file paths, SHA and what the owner must validate in Godot.

## Official API references (check exact installed Godot version)

- Godot `AStarGrid2D`: https://docs.godotengine.org/en/stable/classes/class_astargrid2d.html
- Godot `NavigationAgent2D`: https://docs.godotengine.org/en/stable/classes/class_navigationagent2d.html
- Godot navigation obstacles: https://docs.godotengine.org/en/stable/tutorials/navigation/navigation_using_navigationobstacles.html
