# FW0 — Fog of War

**Source Implemented / Awaiting Owner Validation.** Reusable `FogOfWar.tscn` lives under `RootWorld` in LevelBase and DEBUG. `LevelController.Fog.cs` supplies the existing WorldGrid, registries, projectile container, CPU and optional player. Camera position/zoom/mode never grants sight. No minimap or AI culling is included.

## Configuration

Edit `FogMasterConfig.tres`, or assign another master resource through LevelConfig → Fog. LevelConfig → FogEnabled can start a particular level with presentation bypassed. Both switches affect presentation only; logical visibility continues tracking. No scene-specific singleton or copied grid is involved.

| Inspector property | Default | Meaning |
| --- | --- | --- |
| EnabledByDefault | true | Initial presentation, also subject to level override |
| UnexploredOpacity | 1.0 | Black unknown terrain |
| ExploredOpacity | 0.70 | Dimming outside current sight |
| EdgeSoftnessPixels | 48 | Cosmetic mask filtering in world pixels |
| CitadelVisionRadius | 450 | Static CPU circle |
| PlayerVisionRadius | 350 | Optional moving player circle |
| VisionBlocksOnTerrain | false | LOS is unsupported in FW0; true is rejected |
| VisibilityUpdateInterval | 0.15 s | Moving-source sampling cadence |
| MaximumVisionUpdatesPerFrame | 4 | Combined queued recalculation/moving-source decision budget |
| UpdateOnSourceCellChange | true | Moving sources recalculate only after changing grid cell |
| RendererUpdateMode | Batched | Coalesce mask uploads; Immediate also available |

Values must be finite, opacities 0–1, radii/softness nonnegative, interval at least .01 s and budget positive. Runtime edits to the master resource are not a live tuning contract: reload after Inspector changes.

## State and sources

`FogOfWar` owns a byte state and ushort overlap count per grid cell. A source records its sorted cell contribution; updating its bounded circular region merges old/new lists and only modifies differences. Removing one source preserves overlapping sight. Exploration survives source motion, disablement and loss; regenerating/reloading creates fresh level state. No session persistence.

Register explicitly with `RegisterSource(stableId, node, radius, moving)`. CPU/player use their instance IDs. `SetSourceRadius`, `SetSourceEnabled`, `RequestSourceUpdate` (relocated static sources) and `UnregisterSource` support future callers. Tree exit and ICombatTarget.TargetLost unregister automatically, idempotently. CPU detach/destruction and player death therefore remove sight. Future replacement actors must be explicitly registered by their composition owner. Other buildings do **not** become sources automatically.

Static sources run only on explicit changes. Moving sources have phased deadlines and round-robin checks under the same frame budget as queued changes. Their circles use the actual world position when last sampled; with cell-change mode, movement within that cell can leave the circle up to roughly one cell behind. Radius zero reveals only a cell whose center exactly matches the source. Source removal releases its cached local contribution immediately rather than waiting behind queued work. No terrain occlusion: walls and dead terrain do not block vision.

Read-only integration: `Dimensions`, `CellStates` (ReadOnlyMemory of Unexplored/Explored/Visible), `IsVisible`/`IsExplored` overloads for cell/world position, counts, and `VisibilityChanged(Rect2I)` batches. `ChangedCells` is transient and may only be consumed synchronously during that callback. Future minimaps should consume these same states rather than maintain another visibility authority. `MarkExplored` records known friendly construction without granting current sight.

## Rendering and entity presentation

`FogRenderer` retains one RGBA8 texel per world cell: R=current sight, G=ever explored. CPU writes only dirty texels; Godot's ImageTexture.Update uploads the whole **small mask** once per dirty batch (about 16 KB at 63², 65 KB at 127²), not a full-resolution battlefield image every frame. Batched uploads wake processing for one frame. No per-cell nodes, animated fog sprites or per-enemy CPU visibility loop.

The world overlay draws at z=4000, above current world visuals and below existing HUD/build/debug CanvasLayers. Soft filtering is cosmetic; logically unexplored cells remain exactly the configured black opacity. State remains cell-based, so sharp unknown-cell borders and some grid stepping are an FW0 visual limitation. Current interior is undimmed; filtered boundary pixels may darken.

A shared dynamic CanvasItem shader masks Enemy001/002/003's entire VectorVisual (including health/attack/hit drawing), shared projectiles and DestructionPulse effects. **Center-cell rule:** the visual's world origin must be currently visible; individual fragments must also fall in current sight. Flying enemies follow the identical rule. A tracer whose head leaves vision disappears entirely, even if part of its tail remains inside. Shader masking never removes an enemy from registries or changes processing, targeting, collisions, damage, paths or spawn budgets. Towers can still shoot unseen enemies.

Friendly buildings become known on construction. Static bases remain dimmed scene artwork outside sight; this is **not** a last-seen snapshot. Unseen health bars, range/selection feedback, hit flashes and gun/muzzle artwork are suppressed. The selected-building HUD HP readout is also suppressed. Friendly destruction/removal can therefore change a remembered base outside sight; that limited friendly-state knowledge is intentional in FW0. Objective CPU status and aggregate debug counts remain available. No hidden enemy positions are shown through those readouts.

`FogPresentation` subscribes to explicit registry additions and the supplied projectile/level-root child events. It uses the existing building spatial index on dirty-region changes. New dynamic visual owners should call `RegisterDynamic(CanvasItem)` or add a focused binding here; arbitrary future particle/custom-material effects are not automatically discovered. Existing primitive visuals use this shared mask material; preserve/compose it when adding other visual shaders. The existing grid/world renderers assume an axis-aligned, unit-scale battlefield.

## F2 and lifecycle

F2 → **Fog of War Enabled** bypasses overlay and dynamic masking without erasing state. Disabled presentation skips mask writes/uploads and building visibility queries. Re-enable performs one small-mask resync and restores current presentation. Expanded F2 diagnostics refresh at the existing 4 Hz: sources, current/explored cells, last update duration and dirty count. No FPS gains have been measured.

Scene teardown unregisters sources/events and releases image, texture, material and cached contributions. Enemies, tower combat and effects continue normal simulation under fog and while the toggle is off. Existing F1/F2 spawning, Ctrl menu, C camera, W/T and debug navigation controls are preserved.

## Owner acceptance

Source/diff/resource-path checks only in this environment; no installed .NET build toolchain; Godot runtime was not launched. Please check:

1. Run Level001 without a player; CPU reveals a circle and the rest stays black.
2. Enable SpawnPlayer; walk out, then return. Visited terrain becomes dim and CPU/player overlap remains stable.
3. Pan/zoom free camera and press C; camera changes must not reveal cells.
4. F2-spawn all three enemy types; confirm movement, building hunting, flight and attacks continue while hidden, without health/effect leaks.
5. Toggle fog off/on while moving and fighting; full simulation is inspectable and exploration survives.
6. Kill/remove CPU/player sources, restart and regenerate; overlap is preserved on loss and new worlds reset exploration.
7. Check both DEBUG and Level001, player on/off, walls/towers and unseen building feedback.
8. Compare FPS on/off at the same camera/enemy count; 127×127 scaling and visual/shader acceptance remain owner checks.
