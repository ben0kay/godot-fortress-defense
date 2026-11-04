# DEBUG — BF0 + ST0 sandbox

**Status: Source Implemented / Awaiting Owner Validation.** `test.tscn` is now the default launch scene and manual foundation sandbox, separate from future production/campaign levels. Open using Godot .NET, build C#, then run F6 (scene) or F5 (project).

## Implemented

Finite configurable grid, deterministic optional natural barriers, one initial inherited 2×3 prototype building, atomic placement/selection/health/removal, physical footprint rectangles, shared ground A*, immediate dirty-rectangle navigation updates, and one route preview. No enemy or combat stress test yet.

| Control | Action |
| --- | --- |
| Left-click empty cell | Place a 2×3 test building with top-left anchor at cursor |
| Left-click occupied cell | Select the building occupying that cell |
| Right-click building | Remove it and release its complete footprint |
| Middle-click cell | Toggle terrain; rejected under a building |
| H | Deal 25 structural damage to selected building (100 HP default) |
| Delete | Remove selected building |
| S / G | Set route start / goal at mouse cell |

Green line/circle = route/start; orange circle = goal; gray = solid terrain; dark red = clearance-only blocked cell; cyan/red rectangle = valid/invalid 2×3 placement. A blocked/isolated endpoint explicitly displays UNREACHABLE. The small UI reports topology revision and actual registry count; it is **not** an FPS or AI profiler.

## Inspector knobs

- `WorldGrid.Config` → `WORLD/Grid/DefaultWorldGridConfig.tres`: width, height, cell size, corner-safe diagonals, whole-cell ground clearance. Restart to apply edits.
- Root `InitialObstacles`: deterministic barriers, no procedural random generator.
- Root `TestBuildingScene` → inherited `Foundation/TestBuilding.tscn` → Definition: ID, category, footprint and maximum health. The preview currently intentionally demonstrates 2×3; if changing that fixture's footprint, update the debug preview size too.

`FoundationSandbox.cs` only composes services, handles manual inputs and refreshes one demonstration path on edits. `GridDebugView.cs` redraws on input/events, not every frame. Prototype artwork is separate from the universal building root. Root `UI/DebugPanels/FoundationPanel.tscn` owns all panel content.

## Owner checks

1. Build/open scene; verify all blocked cells and the route detour around the initial building/barrier.
2. Place a 2×3 across the route; inspect rerouting and one revision increase. Try overlapping it and placing over map edges/terrain: no cells or revision should change.
3. Click each occupied cell to select the same building. Right-click or Delete; all six cells become available with one revision increase.
4. Place/select and press H four times. Confirm health decreases then building disappears, registry count drops and footprint/path recover. Re-place in the same cells.
5. Middle-click obstacles to isolate an endpoint and restore it. UNREACHABLE must clear when the route opens.
6. Restart with diagonals disabled and with clearance 1. Verify no corner squeezing, boundary margin and wider gaps required. Inspect visible physics collision shapes against terrain/building rectangles.

Only source/diff review was possible here: Godot and dotnet are unavailable. No runtime acceptance or FPS claim. Later BF1/ST1 brings Citadel/wall/enemy; ST2 brings F1 metrics, path scheduling and 10/50/100/200 spawns; ST3 brings the tower/projectiles. These remain planned.
