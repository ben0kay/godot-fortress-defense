# Fortress Defense — Initial Navigation & 200-Enemy Stress-Test Plan (ST0–ST5)

**Status:** BF0/ST0 + BF1/ST1 **Source Implemented / Awaiting Owner Validation**; ST2–ST5 remain Planned. See `../DEBUG/README.md` for controls and checks.
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

### Enemy001 initial targeting and future extensibility

**For the first stress test, keep this deliberately simple:** `Enemy001` is a regular ground attacker whose **strategic/default target is the Citadel**. Assign/cache the Citadel reference at spawn (or resolve via a stable target registry); do not re-scan for a preferred building/player every frame. A missing/destroyed target, navigation topology change, blocked approach, or explicit order can **invalidate** the cached active route/target and trigger a bounded re-evaluation. A temporary wall/building **breach target** is different from the enemy's enduring strategic Citadel target: after an opening appears, it can return to the Citadel.

**The world can change while enemies are moving.** Player-built walls may seal an open corridor or be removed/destroyed. Footprint placement/removal modifies `WORLD` occupancy and increments a topology revision; enemies must not continue to follow stale paths through newly solid cells. Revalidate affected next steps and queue staggered repaths / reachability decisions rather than calculating all routes in one frame. A wall can be placed after enemies spawn; navigation and fallback behavior must still work.

**Future, not in this test:** support an explicit target assignment/order (e.g., attack the player, a tower, a mine, a specific building), and optional per-enemy **target-selection profiles** such as a weighted initial 80% Citadel / 20% nearby building choice. Treat percentages as an optional future *selection policy at an event or scheduled decision*, **not a per-frame lottery** and not a requirement for Enemy001. Don't implement percentages, commander orders, sophisticated building scoring, or player aggro until requested. Keep `strategic_target`, `active_target`, and `breach_target` concepts distinct so they can be introduced without rewriting the movement engine.

**Initial acceptance emphasis:** the useful benchmark is **enemies navigating dynamically changed walls while one or more towers shoot them**; measure movement, pathfinding, attack/projectile costs and frame spikes with 10/50/100/200 simultaneous enemies. Do not spend this pass on extra factions, advanced target switching, elaborate tower upgrades, full player AI aggro, or content breadth.

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

## Mandatory shared building scene foundation — BF0/BF1

**Status: BF0 shared scene/runtime, definitions, health, selection, registry and rectangular placement/collision are Source Implemented / Awaiting Owner Validation. DEBUG contains an inherited 2×3 test building. BF1 Citadel and Wall001 are Source Implemented / Awaiting Owner Validation; Tower001 is deferred to ST3. This foundation is a prerequisite for Citadel, Wall001 and Tower001 in ST1/ST3.** Implement one reusable **`FORTRESS/BUILDINGS/SHARED/Building.tscn`** as the Godot base scene that **all buildings inherit from** (Godot inherited scenes), regardless of category. Do **not** create independent unrelated roots for towers, generators, miners, storage, walls or Citadel.

```text
FORTRESS/BUILDINGS/
  SHARED/
    Building.tscn       # canonical scene for every structure
    Building.cs         # lightweight root Node2D and building lifecycle
    BuildingDefinition.cs   # data-driven identity, footprint, base health, category
    BuildingRegistry.cs     # centralized living building lookup / targeting
  Core/Citadel/Citadel.tscn                  # inherited from Building.tscn
  Defense/Walls/Wall001/Wall001.tscn        # inherited from Building.tscn
  Defense/OffensiveTowers/Tower001/Tower001.tscn  # inherited from Building.tscn
```

**Shared scene essentials:** `Building.tscn` has a lightweight `Node2D` root running `Building.cs`, a reusable `Visuals` anchor and footprint-aware collision/selection anchors. It owns building identity, definition, grid anchor, `Vector2I` footprint, health, destruction and change notifications, and communicates with `FORTRESS/SYSTEMS/BuildingPlacement` and `WORLD/Grid`. Do **not** force an empty sprite, turret, animation player, or construction effect onto every structure. Shared authored definitions stay separate from instance runtime state.

**Concrete inherited scenes:** `Citadel.tscn`, `Wall001.tscn`, `Tower001.tscn` inherit `Building.tscn`. The actual world instance is the particular inherited scene (e.g., Tower001), *not* an unrelated building root. Inherit the common base and add only the children that building uses.

**Building visuals / layering (authoring contract):**
- `BaseSprite` (optional `Sprite2D`): **static base/platform/body** that never rotates during combat. Tower foundations, chassis housing and floor mounts belong here.
- `RotatingParts` (optional `Node2D` with `Sprite2D` or other lightweight visual): turret head/barrel/aiming section rotates *independently* around an intentional pivot; its muzzle marker (when necessary) rotates with it. The base sprite and footprint collision **never rotate** just because a turret tracks a target.
- `AnimatedParts` (optional `Node2D`/`Sprite2D` with `AnimatedSprite2D`, `AnimationPlayer` or shader, as appropriate): small moving pieces, lights, fans, conveyor segments, etc. Prefer **pre-baked sprite/sprite-sheet layers** rather than numerous individual processing nodes. An animated overlay can be combined with a static base and rotating section.
- `ConstructionVisuals` (future, optional): separate build-progress visual/sprite layer or inherited construction-stage presentation, inspired by `godot-tower-defense`. Construction state can later drive staged sprites/animations without replacing the final artwork or creating a separate building hierarchy. **Do not implement construction effects, drones, scaffolding, or staged sprites during the first stress test.**
- These are **optional scene child conventions**, not four mandatory children for every wall or mine. The shared scene exposes a common `Visuals` attachment point; type-specific artwork and visual logic stay inside the concrete building scene, not in the building engine.
- Define stable 2D draw ordering (e.g., base below rotating/animated elements, effects above when appropriate) and local origins so baked sprite pieces align to the same placement/footprint. Graphics dimensions and navigation footprint are independent.

**Performance:** static sprites remain passive. Aim a turret only when necessary (active targeting or visible presentation), no per-building visual polling just to maintain an unchanging base. Avoid spinning up dozens of child nodes for a handful of baked animated pixels; animations run only while relevant. Cosmetic culling may skip offscreen animation, but gameplay logic, damage and tower fire must continue when a tower is offscreen.

**Avoid scene-inheritance traps:** validate Godot inherited-scene relationships in editor; do not replace a shared inherited root script with unrelated derivatives that bypass the lifecycle. Prefer typed reusable components, composition and virtual hooks sparingly. A scene-specific script can derive from `Building` if necessary. Root `UI/` owns selection/Inspector presentation, not the base scene.

**Performance and footprint contracts:** base building is **event-driven, with no default `_Process` or `_PhysicsProcess`**. Building registration/unregistration updates a shared registry without global scene scans. A multi-cell footprint (1×1, 2×3, 5×5 etc.) occupies/releases every cell in one atomic grid operation; collision and navigation match the same rectangle. Placing/removing a wall generates one navigation batch/topology change; prioritize impacted enemies' repath work rather than updating all enemies synchronously. The visuals may exceed the navigation footprint but must never change grid occupancy silently.

**BF0 acceptance:** one inherited test building shares base health, selection and footprint behavior; supports non-1×1 footprints and leaves no occupied cells after removal. **BF1 acceptance:** Citadel/Wall001/Tower001 all visibly inherit `Building.tscn`; Tower001 has a non-rotating base visual and optional independent rotating turret visual; only Tower001 fires, and placing/removing Wall001 changes ground navigation. Use these same scenes in ST1 and ST3—do not create disposable, incompatible placeholder roots.

## Placeholder encounter and stress harness

**Initial scene:**
- **`DEBUG/test.tscn`** is the dedicated world/navigation/combat stress-test map (BF0/ST0 interactive foundation sandbox; encounter content below remains planned). Keep campaign/scenario levels in `LEVELS/` separately.
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
**Status: Source Implemented / Awaiting Owner Validation**

Implemented: finite grid/config resource, atomic multi-cell occupancy, distinct terrain/building collision, one shared corner-safe ground A*, clearance policy, approach-cell candidates and incremental dirty-rectangle revision events. `DEBUG/test.tscn` is the launch scene with route preview and manual placement/removal/damage/terrain controls. SDK metadata added (Godot.NET.Sdk 4.7.2 / net8.0); no Godot or dotnet installed here, so source/diff review only. No stress/FPS result or editor acceptance claimed. Request queue/movement safety remain ST1/ST2; full procgen/fog/combat remain later.
- Confirm project boot state and available Godot 4 C# API.
- Establish small `WORLD/Grid/` occupancy/footprint data and world/cell transformations; configurable test cell size; deterministic simple obstacle test scene.
- Single ground `AStarGrid2D` path service + valid approach cells, clearance policy, diagonal/corner policy.
- Expose one navigation revision / dirty invalidation event for dynamic footprint edits.
- No full procgen, rendering framework or enemy AI yet.
- **Owner acceptance:** can view/debug blocked vs walkable cells, verify a route around a fixed obstacle, place a 2×3 (or larger) footprint without overlap, and remove/destroy it to restore **all** occupied cells.

### ST1 — Citadel, wall and pathing enemy
**Status: Source Implemented / Awaiting Owner Validation** · Depends ST0

BF1/ST1 now includes inherited Citadel/Wall001, Enemy001 definition/runtime/vector visual, world-scoped registry, fixed-objective contact combat, cooperative reachability + coalesced FIFO path work, per-step swept solid checks, gradual seeded edge spawning and a quiet battlefield presentation. Defaults: 40×26 ×32px; CPU 4×4/4000HP; wall 1×1/120HP; 20 enemies max at 0.8s intervals. Queue defaults: 512 flood visits/frame, 1 native A*/frame, 1.5ms soft cap. No-route enemies sleep until topology changes. CPU loss cancels routes/stops spawning. Bodies/clearance cells reject overlapping placement; CPU enclosure remains allowed.

Minimal ST1 queue is intentional groundwork for ST2, not its priority/profiler/preset implementation. Affected units currently pause for replacement; remaining-route bounding rectangles provide conservative invalidation, and grid sweeps independently prevent unsafe steps. Breach choice is a reachable-boundary heuristic, not an optimal demolition plan. No steering/crowd separation. See `../DEBUG/README.md` and `../ENEMIES/MOVEMENT/README.md` for controls, config, limitations and owner checks. Source/diff review only: no Godot/dotnet available, no runtime acceptance or FPS claim.

- Create BF0's shared `Building.tscn` foundation if it doesn't exist; the placeholder Citadel and wall **must inherit it**. Implement CPU footprint/health, basic vector ground enemy, edge-of-map spawn and waypoint movement.
- Central enemy registry with safe spawn/despawn, **Enemy001** with cached default Citadel target and a separate temporary wall/building breach target. No weighted target-choice implementation.
- Path when reachable; when enclosed, fallback to reachable wall/building target and attack it; release footprint on destruction and recover route.
- Keep core movement responsive even while tactical choices are throttled.
- **Owner acceptance:** blocked-but-open route works around multi-cell obstacles; fully enclosed CPU triggers non-phasing ground enemies to attack a wall, then resume Citadel pursuit after breach.

### ST2 — Shared scheduling and profiler overlay
**Status: Partially Source Implemented / Awaiting Owner Validation** · Depends ST1

Owner-authorized basic debug slice: linked `UI/DebugPanels/Debug.tscn` under RootDebug, real FPS, registry live count, pending count, F2 controls for bounded manual adds/custom counts and clear. Shared LevelBase/DEBUG now have grouped root containers. Manual adds reuse the edge spawner with frame/cap limits; no new enemy system. Advanced route priorities/validation budgets, visibility tiers and subsystem timing/profiling below remain planned; no rendered stress benchmark was run.

- F1 UI panels and DEBUG counters; configurable staggered AI and path query budgets.
- Camera/visibility-based decision tiers (default visible ~3–5/s, distant fog ~1/s) with independent reliable combat/movement.
- Cap synchronous path requests after wall placement, update revision and dirty queues. Add bounded 10/50/100/200 spawner with reset.
- Add cheap viewport cosmetic culling; no complete fog-of-war system yet.
- **Owner acceptance:** counter updates match actual entities and staggered workload is observable; 100/200 enemy scenes can be created for manual measurement. No benchmark success claimed by Codex.

### ST3 — Placeholder tower and combat load
**Status: Basic Tower001 Source Implemented / Awaiting Owner Validation** · Advanced ST2 scheduling remains incomplete

Owner-authorized basic Level1 Battle-style tower is now available with T placement/W walls in both scenes. Tower001 inherits Building, uses local base/gun SVGs,1000HP and row1 reference combat values (30 damage,1.68/s,323.4px,2300px/s,216°/s,12° aim,.15s search,±2.5° spread). Shared COMBAT closest targeting/aiming and straight swept projectiles use EnemyRegistry spatial buckets and Enemy001 health/death. Terrain/buildings block fire. RootCombat holds projectiles; no heat/ammo/power/upgrades/XP/research/construction/shields. Source/scene/asset review only; Godot/.NET unavailable, no rendered combat/FPS acceptance or stress profiling claimed. See Tower001/README.md.

- Implement Tower001 as an **inherited scene of `Building.tscn`**, with footprint, range, optional weapon node, simple fire/projectile/damage, shared COMBAT behavior and destruction cleanup.
- One or a few walls/buildings and active combat; same dynamic grid edit path as ST0. Basic Citadel default plus wall-breach fallback is enough; defer sophisticated/weighted target priorities.
- Stress conditions include tower shots, enemy attacks, AI switching and wall destruction, not merely silhouettes.
- **Owner acceptance:** 10/50/100/200 active simulated Enemy001 units and a tower operate together; placing/removing walls mid-fight causes correct staggered rerouting and/or breach behavior; owner reports representative FPS/spikes.

### ST4 — Procedural layout and fog foundation
**Status: Planned** · Depends ST3

**WG0 ahead-of-track note:** A separate authorized Level001 procedural world foundation is now Source Implemented / Awaiting Owner Validation. It reuses the ST1 services and leaves `DEBUG/test.tscn` fixed. Seeded clusters, CPU/player/edge validation, batched collision and new/same-seed reload controls are in place; **fog and the rest of ST4 remain planned**. See `../LEVELS/Level001/README.md`.

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
