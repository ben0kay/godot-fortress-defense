# WORLD — grid, navigation and WG0 generation

**Status: Source Implemented / Awaiting Owner Validation.**

`Grid/WorldGrid.cs` is the single authority for finite terrain and cell-to-building occupancy. `Navigation/GroundNavigation.cs` derives one shared ground A* view. DEBUG provides the demonstration, not gameplay ownership. ST1 adds `GroundReachability.cs` and swept segment checks consumed by the bounded enemy request queue. WG0 adds reusable seeded cluster generation/validation and terrain rendering under `Generation/` and `Terrain/`, used by `LEVELS/Level001`. Deposits and fog remain planned.

## Configuration and coordinates

`Grid/DefaultWorldGridConfig.tres` is assigned to `DEBUG/test.tscn`'s WorldGrid. Edit map width/height (4–512 cells), cell size (4–256 pixels), diagonal movement and ground clearance (0–8 cells) before starting. Defaults: 40×26, 32-pixel cells. The 4×4 CPU is 128×128 pixels and the 1×1 wall is 32×32; Enemy001 radius is 7 pixels. Runtime config mutation does not resize a live world. Create a new scene/world to apply changes.

Anchor = top-left occupied cell; building origin = footprint rectangle center. Rotation/quarter-turn placement is deferred: roots/containers must remain unrotated, unscaled, and placed buildings must not be manually moved. Translate the grid if needed; use `WorldToCell`, `CellCenter`, and `FootprintCenter` rather than assuming a zero world origin.

## Occupancy and collision

Terrain and building references are separate arrays. Out-of-map is blocked. Placement checks every cell before claiming any; removal releases the stored rectangle, even if authored definition data was subsequently changed. A footprint mutation increments `Revision` once and publishes one `TopologyChanged(Rect2I)` after the complete edit. Terrain rectangles also publish one event only if something changed. Rejected edits publish nothing.

Terrain has layer-1 collision merged from contiguous solid row runs on one static body; placed buildings have one layer-1 static rectangle matching their exact footprint. Collision changes requested at removal are deferred safely; navigation changes immediately. The placement service additionally rejects live enemy body/clearance-cell overlap through EnemyRegistry. DEBUG applies the same check to solid terrain additions. WG0 additionally reserves the traversable player-marker cell against building/solid-terrain placement. Deposits and costs remain future restrictions.

## Ground navigation

AStar is initialized once, with unit costs and matching Manhattan/cardinal or octile/diagonal heuristics. Diagonals require **both** side cells clear. `SetPointSolid` refreshes only changed cells plus the configured clearance halo; ordinary edits never call `Update()` or rebuild the full map. All calls stay on the main thread.

Clearance is deliberately conservative: `GroundClearanceCells=N` requires the square N-cell neighborhood to be empty, including boundaries. Zero means a cell-center path for an actor fitting within one cell (radius at most half the cell size); positive values reserve extra whole-cell clearance. `WorldGrid.SegmentClear` separately checks actual movement against conservative radius-expanded solid rectangles and map bounds, including new walls. ST1 movers stop before unsafe steps.

`FindPath` returns world-space cell centers or an empty array for blocked/out-of-bounds/unreachable endpoints; it never returns a partial route. `GetApproachCells` returns walkable cardinal perimeter candidates outside a solid footprint with clearance separation, **not a guarantee of reachability**. Callers choose/search reachable candidates under future budgets. Do not query the blocked center of a building.

ST1 `GroundReachability` provides one reusable stamped-array flood workspace, visited cooperatively under `ENEMIES/MOVEMENT/EnemyPathRequests` budgets. It proves reachable CPU contact cells or finds accessible breach-building boundaries before one shared A* query. No per-enemy grids or full-scene target searches. Remaining-route bounds selectively queue edits; affected units pause and every movement step checks current solids. Advanced priorities, safe-prefix handoffs and instrumentation remain ST2. See `../ENEMIES/MOVEMENT/README.md`. Additional flying/phasing views should derive from these same occupancy arrays when authorized.

## WG0 proper level

`LEVELS/Level001/Level001.tscn` uses its own 63×63 ×64px config, approximate 22% black dead terrain, an 8-cell CPU clearing and reserved player marker. See [generation](Generation/README.md), [terrain/collision](Terrain/README.md) and [Level001 controls/settings](../LEVELS/Level001/README.md). Bulk terrain is accepted once before A* initialization; shared `LevelController` composes existing services in both proper levels and DEBUG. Neither the debug grid config nor the default project launch path is replaced.

## References reviewed

- Godot Tower Defense: `WORLD/NAVIGATION/README.md`, `PathRequestManager.cs`, `ENEMIES/ENEMYPERFORMANCE.md`, `enemypathing.md`, `TOWERS/Shared/TowerBase.cs`, `TowerDefinition.cs` and README.
- Vector TD 2026: `scr_building`, `scr_building_data`, `scr_foundation`, `scr_enemy`, `scr_enemy_data`, `scr_enemy_player_targeting`, `scr_navigation`.

The Godot performance notes identify optional corridor refinement as expensive route-assignment work; ST0 does not port it, weighted profiles, shared-route caches or per-entity A* grids. The GML atomic footprint reservations, separate terrain/building occupancy, cached target/approach design and staggered scheduling informed the boundaries here. Their benchmarks do not establish performance for this project.

API: [AStarGrid2D](https://docs.godotengine.org/en/stable/classes/class_astargrid2d.html). Build metadata uses Godot.NET.Sdk 4.7.2 / net8.0, matching the sister Godot project's SDK and this project's 4.7 feature tag. Neither Godot nor dotnet was available in the implementation environment; editor import, build and runtime remain owner checks.

COMBAT projectiles use WorldGrid.FirstObstacleFraction for earliest solid/map-boundary contact, with the same cell data and radius-expanded rectangles used by movement. This does not change terrain generation, building reservations or ground A* rules. Building shots can ignore only their own source footprint during barrel exit.

EN2: FirstObstacleFraction also has an out Building overload reporting the earliest struck footprint, using exactly the same swept solid-cell loop. Hostile shared projectiles damage that building; terrain/bounds win ties and never forward damage to a farther structure. Ranged approach search remains in the existing bounded ENEMIES queue. Terrain generation/grid ownership are unchanged.
