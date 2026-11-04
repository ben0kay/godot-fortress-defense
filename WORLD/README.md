# WORLD — BF0 / ST0 foundation

**Status: Source Implemented / Awaiting Owner Validation.**

`Grid/WorldGrid.cs` is the single authority for finite terrain and cell-to-building occupancy. `Navigation/GroundNavigation.cs` derives one shared ground A* view. DEBUG provides the demonstration, not gameplay ownership. No procedural generation, deposits, fog, enemies or path scheduler yet.

## Configuration and coordinates

`Grid/DefaultWorldGridConfig.tres` is assigned to `DEBUG/test.tscn`'s WorldGrid. Edit map width/height (4–512 cells), cell size (4–256 pixels), diagonal movement and ground clearance (0–8 cells) before starting. Defaults: 32×20, 32-pixel cells. This fits the initial viewport and gives the 2×3 prototype a 64×96-pixel footprint; tune alongside future enemy radius/camera scale. Runtime config mutation does not resize a live world. Create a new scene/world to apply changes.

Anchor = top-left occupied cell; building origin = footprint rectangle center. Rotation/quarter-turn placement is deferred: roots/containers must remain unrotated, unscaled, and placed buildings must not be manually moved. Translate the grid if needed; use `WorldToCell`, `CellCenter`, and `FootprintCenter` rather than assuming a zero world origin.

## Occupancy and collision

Terrain and building references are separate arrays. Out-of-map is blocked. Placement checks every cell before claiming any; removal releases the stored rectangle, even if authored definition data was subsequently changed. A footprint mutation increments `Revision` once and publishes one `TopologyChanged(Rect2I)` after the complete edit. Terrain rectangles also publish one event only if something changed. Rejected edits publish nothing.

Terrain has layer-1 static cell collision; placed buildings have one layer-1 static rectangle matching their exact footprint. Collision changes requested at removal are deferred safely; navigation changes immediately. No deposits, actor-overlap placement checks, costs or other restrictions exist yet. Future moving entities will need placement-overlap policy and immediate movement safety.

## Ground navigation

AStar is initialized once, with unit costs and matching Manhattan/cardinal or octile/diagonal heuristics. Diagonals require **both** side cells clear. `SetPointSolid` refreshes only changed cells plus the configured clearance halo; ordinary edits never call `Update()` or rebuild the full map. All calls stay on the main thread.

Clearance is deliberately conservative: `GroundClearanceCells=N` requires the square N-cell neighborhood to be empty, including boundaries. Zero means a cell-center path for an actor fitting within one cell (radius at most half the cell size); positive values reserve extra whole-cell clearance. It is not continuous swept-disc movement validation. Future movers must validate their next swept segment against current occupancy and retain corner rules while a queued replacement is pending.

`FindPath` returns world-space cell centers or an empty array for blocked/out-of-bounds/unreachable endpoints; it never returns a partial route. `GetApproachCells` returns walkable cardinal perimeter candidates outside a solid footprint with clearance separation, **not a guarantee of reachability**. Callers choose/search reachable candidates under future budgets. Do not query the blocked center of a building.

ST0 paths are explicit synchronous queries; DEBUG runs one route preview after edits. There is no automatic enemy repath or queue. Selective route dependencies, fair prioritized budgets, safe-prefix following and wall-breach decisions remain ST1/ST2 work. Additional flying/phasing views should derive from these same occupancy arrays when authorized.

## References reviewed

- Godot Tower Defense: `WORLD/NAVIGATION/README.md`, `PathRequestManager.cs`, `ENEMIES/ENEMYPERFORMANCE.md`, `enemypathing.md`, `TOWERS/Shared/TowerBase.cs`, `TowerDefinition.cs` and README.
- Vector TD 2026: `scr_building`, `scr_building_data`, `scr_foundation`, `scr_enemy`, `scr_enemy_data`, `scr_enemy_player_targeting`, `scr_navigation`.

The Godot performance notes identify optional corridor refinement as expensive route-assignment work; ST0 does not port it, weighted profiles, shared-route caches or per-entity A* grids. The GML atomic footprint reservations, separate terrain/building occupancy, cached target/approach design and staggered scheduling informed the boundaries here. Their benchmarks do not establish performance for this project.

API: [AStarGrid2D](https://docs.godotengine.org/en/stable/classes/class_astargrid2d.html). Build metadata uses Godot.NET.Sdk 4.7.2 / net8.0, matching the sister Godot project's SDK and this project's 4.7 feature tag. Neither Godot nor dotnet was available in the implementation environment; editor import, build and runtime remain owner checks.
