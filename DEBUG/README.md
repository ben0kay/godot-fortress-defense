# DEBUG — BF1 + ST1 battlefield

**Status: Source Implemented / Awaiting Owner Validation.** Open `test.tscn` in Godot .NET, build C#, and run F6/F5. The default scene is now a small encounter, not a grid demo.

## On Play

A 40×26-cell battlefield (32-pixel cells) has fixed natural cover and a centered **4×4 Citadel** with 4000 HP. Red arrow enemies enter from all four edges gradually (one per 0.8 seconds, maximum 20 active). They follow shared A* routes to legal contact positions, damage the CPU at 8 damage / 1.2 seconds, and flash on attacks. CPU/wall health bars and brief destruction rings provide feedback. CPU loss stops spawning and leaves survivors idle; R restarts the encounter.

Walls are inherited `Wall001.tscn` buildings: 1×1 cells, 120 HP by default. Enemies can route around them or attack an accessible temporary breach target when the CPU is unreachable. Orange arrows identify breachers. Tower001 basic shooting is now available via T; player weapons remain planned. Enemies deliberately overlap one another; crowd separation is deferred.

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

Source/diff and scene/resource reference review only in this environment: Godot/dotnet unavailable. No build, editor acceptance or FPS result claimed. BF1/ST1 is source implemented; Basic F2 stats/spawn controls and Tower001/projectiles are now source implemented; advanced ST2 priorities/profiling remain planned. The earlier `Foundation/TestBuilding.tscn` BF0 fixture remains available as an inherited-scene reference, but is no longer the default placed object.

## WG0 shared services

The sandbox remains the fixed 40×26 stress encounter and the F5 launch target. Proper procedural content is separate in `LEVELS/Level001/Level001.tscn` (run F6). WG0 extracts the existing setup/input into `LEVELS/SHARED/LevelController.cs`; `FoundationSandbox` only supplies its fixed obstacles. GridDebugView preserves its script/UID as a compatibility subclass of the shared terrain renderer. The existing GridDebugView node path is preserved. Terrain collision is merged into rectangular shapes; DEBUG terrain editing still works, and wall edits keep their incremental navigation updates. No campaign config or new pressure modes are imposed on this sandbox.

## Reusable FPS / enemy / spawn panel

`UI/DebugPanels/Debug.tscn` is instanced at `RootDebug/Debug` in the fixed sandbox and shared LevelBase, so Level001 inherits it. Top-right FPS/live-enemy/pending counts are always visible; **F2** expands Add10/50/100/200/custom, automatic-spawn toggle and clear controls. Requests are gradual (2/frame by default), share existing spawn validity checks, and cap active+pending at200. Clear or C cancels queued work and pauses automatic spawning. See `../UI/DebugPanels/README.md` for reuse/binding and cap semantics.

Scene organization is now RootWorld, RootFortress, RootEnemies, RootCamera, RootUI and RootDebug. These plain containers do not process. LevelController uses the corresponding grouped paths; gameplay owners remain unchanged. F1 is still Level001's world-generation panel, not this reusable F2 panel. Advanced ST2 priorities/profiling remain planned.

## Tower001 basic combat

Press **T** to place/paint Tower001, **W** to return to walls. Click an existing structure to select it; towers show range/HP and their base stays fixed while the gun rotates. F2-spawn enemies to inspect straight amber projectile hits through Enemy001's existing health/death handling. Right-click/Delete removes towers as well as walls. RootCombat/Projectiles owns fired rounds; clearing enemies leaves any already-fired rounds to expire, while reload/regeneration resets all combat nodes.

Tower001 uses local copies of the original Battle Tower base/gun and approximate Level1 values (30 damage,1.68/s,323.4px range,2300px/s,216°/s turn). Inspector config is under `FORTRESS/BUILDINGS/Defense/OffensiveTowers/Tower001/`. Art scales to the existing cell footprint, while combat ranges remain in pixels. Acquisition is closest-visible, staggered and spatially bounded; no heat/ammo/power/upgrades/ranks/XP/construction/research/shields were added. See the Tower001 README for values and owner checks. Source Implemented / Awaiting Owner Validation; no runtime/FPS result claimed.

BM0: this fixed sandbox explicitly uses TryPlaceFree, including W/T placement. Its credit diagnostic labels FREE DEBUG placement; no purchases or refunds occur. Level001 uses authored costs unless its Config.FreeDebugPlacement is enabled.

BM1: shared Ctrl → category → building menu is available here, labelled FREE DEBUG. The sandbox initially keeps wall placement armed. W/T select, LMB/drag builds repeatedly, Esc/RMB cancels. After cancellation, clicking selects and RMB/Delete removes. F1/F2 remain unchanged. [Shared build menu](../UI/BuildMenu/README.md).

Revised PL0/CAM0: this sandbox has no player by default and uses the shared free camera. C safely toggles follow only for a living player; Home frames the map, MMB pans and wheel zooms. B + MMB retains terrain editing. Clear enemies using F2 → Clear; C no longer clears. RootPlayer stays empty unless a configured level enables spawning.

EN2: F2 type selector supports enemy001/enemy002 batches, mixed under the same200 combined guard. Enemy002 is amber with a visible forward barrel and straight hostile rounds, with building-only targeting whether a player is present or absent. Normal automatic spawning stays Enemy001. See the Enemy002 README for tuning and owner scenarios.

AIR1: F2 → enemy003 spawns cyan winged impact flyers, independent of blocked/dead ground cells. They only damage the CPU; Tower001 remains Ground-only. Ground placement ignores airborne overlap, and B/MMB terrain edits beneath flyers do not trigger repaths. Mixed types share existing spawn limits. AIR2 now supplies Tower002; AIR3 remains planned.

AIR2: Ctrl → Defense → Anti-Air Tower / Tower002 is freely placeable using the shared menu, with Air-only combat. W/T retain their original wall/Tower001 quick selections.

## FW0 visibility

**Source Implemented / Awaiting Owner Validation.** See [fog ownership, config and limits](../WORLD/FOG/README.md). Shared LevelBase/DEBUG bind CPU and optional player sources through the existing WorldGrid. F2's **Fog of War Enabled** bypasses presentation only; exploration keeps tracking. Enemy visuals, shots and destruction effects are clipped without changing simulation. Friendly bases remain known/dim; unseen HP/selection/turret feedback is suppressed. LevelConfig → Fog/FogEnabled selects settings/initial presentation; camera controls never reveal terrain. Debug diagnostics refresh at 4 Hz, and standalone Debug instances can receive an explicit Fog node reference. Runtime and FPS comparisons remain owner checks.

PWR0 adds power network, relay and endpoint counts to the expanded F2 panel. The initial Citadel is one relay and one generator endpoint in one network. [PWR0 setup and checks](../FORTRESS/SYSTEMS/PowerNetwork/README.md).
