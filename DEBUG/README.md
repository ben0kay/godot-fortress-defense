# DEBUG — BF1 + ST1 battlefield

**Status: Source Implemented / Awaiting Owner Validation.** Open `test.tscn` in Godot .NET, build C#, and run F6/F5. The default scene is now a small encounter, not a grid demo.

## On Play

A 40×26-cell battlefield (32-pixel cells) has fixed natural cover and a centered **4×4 Citadel** with 4000 HP. Red arrow enemies enter from all four edges gradually (one per 0.8 seconds, maximum 20 active). They follow shared A* routes to legal contact positions, damage the CPU at 8 damage / 1.2 seconds, and flash on attacks. CPU/wall health bars and brief destruction rings provide feedback. CPU loss stops spawning and leaves survivors idle; R restarts the encounter.

Walls are inherited `Wall001.tscn` buildings: 1×1 cells, 120 HP by default. Enemies can route around them or attack an accessible temporary breach target when the CPU is unreachable. Orange arrows identify breachers. There are no towers or player weapons yet. Enemies deliberately overlap one another; crowd separation is deferred.

## Controls

| Control | Action |
| --- | --- |
| Left-click / drag empty cells | Place / paint Wall001 |
| Left-click building | Select and show its HP |
| Right-click / Delete | Remove hovered / selected wall (CPU protected from accidental removal) |
| P | Toggle gradual spawning |
| C | Clear enemies **and pause spawning**; P resumes |
| R | Reload/reset entire encounter, including CPU |
| B | Optional grid + demonstration route overlay (off on startup) |
| S / G with overlay on | Move demonstration start / goal to cursor |
| Middle-click with overlay on | Toggle natural solid; reject changes under buildings/enemies |
| H | Developer-only: 25 damage to selected building |
| K | Developer-only: kill an enemy under the cursor to inspect death/cleanup |

Walls cannot be placed over enemy bodies or within their configured clearance-cell reservation. Sealing the CPU is allowed; connectivity is **not** a placement restriction. The cursor outlines the proposed footprint; click-time actor checks may reject an apparently terrain-valid preview.

## Inspector settings

- `WorldGrid.Config` → `WORLD/Grid/DefaultWorldGridConfig.tres`: dimensions, cell size, diagonals and clearance; restart after edits.
- Root `InitialObstacles`, `CitadelScene`, `WallScene`: encounter composition. Fixed cover is a simple deterministic layout, not procedural generation. Enlarging CPU footprints far beyond defaults can overlap cover; disable InitialObstacles or adjust the layout.
- `Citadel/CitadelDefinition.tres`: footprint and HP; `Wall001/Wall001Definition.tres`: footprint and HP. Preview/visuals/collision derive from definitions, not hard-coded sprite sizes.
- `Enemy001/Enemy001Definition.tres`: HP, radius, speed, turning, attack damage, center-to-footprint range and cooldown. Radius must be below half a cell. Attack range must reach a legal approach cell (at least `(ClearanceCells + 0.5) × CellSize` for cardinal approaches).
- `EdgeEnemySpawner`: enable, interval, maximum active count, attempt cap, CPU minimum separation and local seed. Failed/blocked edge attempts wait until the next interval; no creation burst or infinite retry loop.
- `EnemyPathRequests`: max visited cells/frame (512), native A* paths/frame (1), soft time budget (1.5 ms). Jobs coalesce and run FIFO. Native A* itself is indivisible; these are workload limits, not guaranteed frame time.

The small status UI is under root `UI/DebugPanels/`. It refreshes HP/counts at 4 Hz and is **not** ST2 profiling. Background/grid drawing updates only on input/topology changes. Optional demonstration routes are explicit debug queries outside the enemy request budget; keep B off when judging normal gameplay.

## Brief owner checks

1. Run: CPU visible in center, edge enemies visibly move, turn, avoid natural cover and chip CPU health. Wait for CPU loss and R-restart.
2. Paint a wall across an approaching route. Affected enemies pause briefly for queued replacement; others continue. Nobody crosses the new wall or diagonal blocked corners. Placement directly on enemies must be rejected.
3. Press C, surround the CPU with a complete wall ring, then P. Enemies should turn orange, approach an **accessible** wall and destroy it, then return to CPU pursuit through the opening. Repeat with a second wall layer. Existing enemies inside an enclosure can still attack the CPU.
4. Destroy/remove a wall while an enemy approaches it: occupancy/collision recover, obsolete requests disappear and enemies reconsider the CPU. No ghost blockers or endless failed searches when only unbreakable terrain isolates a region.
5. Try H on selected wall and K on an enemy; inspect brief rings and count/selection cleanup. C/P/R should not leave enemy callbacks, reservations or pending work behind.

Source/diff and scene/resource reference review only in this environment: Godot/dotnet unavailable. No build, editor acceptance or FPS result claimed. BF1/ST1 is source implemented; ST2 priorities/metrics/10–200 presets and ST3 tower/projectiles remain planned. The earlier `Foundation/TestBuilding.tscn` BF0 fixture remains available as an inherited-scene reference, but is no longer the default placed object.

## WG0 shared services

The sandbox remains the fixed 40×26 stress encounter and the F5 launch target. Proper procedural content is separate in `LEVELS/Level001/Level001.tscn` (run F6). WG0 extracts the existing setup/input into `LEVELS/SHARED/LevelController.cs`; `FoundationSandbox` only supplies its fixed obstacles. GridDebugView preserves its script/UID as a compatibility subclass of the shared terrain renderer. The existing GridDebugView node path is preserved. Terrain collision is merged into rectangular shapes; DEBUG terrain editing still works, and wall edits keep their incremental navigation updates. No campaign config or new pressure modes are imposed on this sandbox.
