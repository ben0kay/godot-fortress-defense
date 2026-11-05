# FW0 + FW1 — Fog of War and Building Vision

**Source Implemented / Awaiting Owner Validation.** Reusable `FogOfWar.tscn` lives under `RootWorld` in LevelBase and DEBUG. `LevelController.Fog.cs` supplies the existing WorldGrid, registries, projectile container, CPU, placed buildings and optional player. Camera position/zoom/mode never grants sight. No minimap or AI culling is included.

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
| DefaultBuildingVisionRadius | 64 | Fallback for buildings without an explicit range |
| VisionBlocksOnTerrain | false | LOS is unsupported in FW0; true is rejected |
| VisibilityUpdateInterval | 0.15 s | Moving-source sampling cadence |
| MaximumVisionUpdatesPerFrame | 4 | Combined queued recalculation/moving-source decision budget |
| UpdateOnSourceCellChange | true | Moving sources recalculate only after changing grid cell |
| RendererUpdateMode | Batched | Coalesce mask uploads; Immediate also available |

Values must be finite, opacities 0–1, radii/softness nonnegative, interval at least .01 s and budget positive. Runtime edits to the master resource are not a live tuning contract: reload after Inspector changes.

## State and sources

`FogOfWar` owns a byte state and ushort overlap count per grid cell. A source records its sorted cell contribution; updating its bounded circular region merges old/new lists and only modifies differences. Removing one source preserves overlapping sight. Exploration survives source motion, disablement and loss; regenerating/reloading creates fresh level state. No session persistence.

Register explicitly with `RegisterSource(stableId, node, radius, moving)`. CPU/player use their instance IDs. `SetSourceRadius`, `SetSourceEnabled`, `RequestSourceUpdate` (relocated static sources) and `UnregisterSource` support future callers. Tree exit and ICombatTarget.TargetLost unregister automatically, idempotently. CPU detach/destruction and player death therefore remove sight. Future replacement actors must be explicitly registered by their composition owner. `BuildingVisionSources` now owns one-time startup registration and BuildingRegistry.Added/Removed subscriptions for **all friendly placed buildings**, including exactly one CPU source. CPU always uses CitadelVisionRadius. Other buildings use `Building.VisionRadius` when supplied, otherwise DefaultBuildingVisionRadius. Tower001/Tower002 provide their snapshotted authored weapon range by default (323.4px currently); Wall001 and passive structures use the 64px fallback. No tower-type-name checks exist in the vision-source bridge.

The universal building exposes **VisionRadiusOverride** in the Inspector: -1 = automatic, nonnegative = explicit sight radius (zero is intentional, never fallback). This lets radar/storage/artillery declare sight independently of any weapon. Custom building subclasses may override the nullable `VisionRadius` property; null requests fallback. Emit `NotifyVisionChanged()` after changing a custom provider's range. The Inspector override setter emits it automatically. The bridge queues SetSourceRadius/RequestSourceUpdate, coalescing repeated edits. Placed building roots remain stationary under the existing grid contract; a future supported relocation must first update occupancy/spatial ownership, then notify vision. Changing only a transform is not supported.

The bridge unregisters on registry removal and disconnects providers on teardown; FogOfWar's own TargetLost/TreeExiting hooks are idempotent backup cleanup. Deferred removal cannot leave a building contribution behind. Regeneration reconstructs the bridge and all level state. Weapon configs remain per-placement snapshots; edit authored combat range then place new towers/reload.

Static sources run only on explicit changes. Moving sources have phased deadlines and round-robin checks under the same frame budget as queued changes. Their circles use the actual world position when last sampled; with cell-change mode, movement within that cell can leave the circle up to roughly one cell behind. Radius zero reveals only a cell whose center exactly matches the source. Building registration bursts consume at most the configured source budget each frame (default 4); with a moving player, one slot is reserved for moving-source checks when the budget is greater than one. New-building vision may appear over subsequent frames while queued; no synchronous batch of every circle is calculated on placement. Budget 1 drains queued changes before moving checks, so use at least 2 for responsive player sight during bulk placement. Large-radius sources still cost more per decision; the budget bounds sources, not individual cells. Cached overlap counts avoid rebuilding the whole world. Source removal releases its cached local contribution immediately rather than waiting behind queued work. No terrain occlusion: walls and dead terrain do not block vision.

Read-only integration: `Dimensions`, `CellStates` (ReadOnlyMemory of Unexplored/Explored/Visible), `IsVisible`/`IsExplored` overloads for cell/world position, counts, and `VisibilityChanged(Rect2I)` batches. `ChangedCells` is transient and may only be consumed synchronously during that callback. Future minimaps should consume these same states rather than maintain another visibility authority. `MarkExplored` records known friendly construction without granting current sight.

## Rendering and entity presentation

`FogRenderer` retains one RGBA8 texel per world cell: R=current sight, G=ever explored. CPU writes only dirty texels; Godot's ImageTexture.Update uploads the whole **small mask** once per dirty batch (about 16 KB at 63², 65 KB at 127²), not a full-resolution battlefield image every frame. Batched uploads wake processing for one frame. No per-cell nodes, animated fog sprites or per-enemy CPU visibility loop.

The world overlay draws at z=4000, above current world visuals and below existing HUD/build/debug CanvasLayers. Soft filtering is cosmetic; logically unexplored cells remain exactly the configured black opacity. State remains cell-based, so sharp unknown-cell borders and some grid stepping are an FW0 visual limitation. Current interior is undimmed; filtered boundary pixels may darken.

A shared dynamic CanvasItem shader masks Enemy001/002/003's entire VectorVisual (including health/attack/hit drawing), shared projectiles and DestructionPulse effects. **Center-cell rule:** the visual's world origin must be currently visible; individual fragments must also fall in current sight. Flying enemies follow the identical rule. A tracer whose head leaves vision disappears entirely, even if part of its tail remains inside. Shader masking never removes an enemy from registries or changes processing, targeting, collisions, damage, paths or spawn budgets. FW1 additionally gates tower acquisition and each cached-target combat check on authoritative current sight (the enemy's center cell). Sight may come from any friendly source. Losing sight clears the cached target **before rotation or firing**; normal staggered acquisition resumes. Ground/Air masks, firing range, obstruction, aim tolerance and cooldown remain required. Already-fired shots continue unchanged, including hits on enemies that subsequently become hidden.

Friendly buildings become known on construction. Static bases remain dimmed scene artwork outside sight; this is **not** a last-seen snapshot. Unseen health bars, range/selection feedback, hit flashes and gun/muzzle artwork are suppressed. The selected-building HUD HP readout is also suppressed. Friendly destruction/removal can therefore change a remembered base outside sight; that limited friendly-state knowledge is intentional in FW0. Objective CPU status and aggregate debug counts remain available. No hidden enemy positions are shown through those readouts.

`FogPresentation` subscribes to explicit registry additions and the supplied projectile/level-root child events. It uses the existing building spatial index on dirty-region changes. New dynamic visual owners should call `RegisterDynamic(CanvasItem)` or add a focused binding here; arbitrary future particle/custom-material effects are not automatically discovered. Existing primitive visuals use this shared mask material; preserve/compose it when adding other visual shaders. The existing grid/world renderers assume an axis-aligned, unit-scale battlefield.

## F2 and lifecycle

F2 → **Fog of War Enabled** bypasses overlay and dynamic masking without erasing state. Disabled presentation skips mask writes/uploads and building visibility queries. Re-enable performs one small-mask resync and restores current presentation. Expanded F2 diagnostics refresh at the existing 4 Hz: sources, current/explored cells, last update duration and dirty count. Tower eligibility never reads PresentationEnabled or HasCurrentVision; disabling the overlay does not grant omniscient attacks. There is no combat-vision bypass. No FPS gains have been measured.

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

## FW1 owner acceptance

**Source Implemented / Awaiting Owner Validation.** Source/diff/scene checks only; no .NET toolchain available for a build.

- Start with CPU only and confirm its original radius/source count. Place Wall001 and each tower beyond its sight; check fallback versus authored weapon-radius reveals.
- Overlap multiple structures, remove/destroy them and regenerate. Remaining sources must retain their sight; lost unique areas stay explored/dim. Repeat with player enabled/disabled.
- To exercise weapon range exceeding personal sight, temporarily set a tower's VisionRadiusOverride to 64 on its scene and place a new one. An enemy farther away but inside weapon range must stay untargeted until another friendly source reveals its center cell. Remove that other source or move the enemy out of vision; aiming/new shots must stop at the next combat check. Test Ground/Enemy001 and Air/Enemy003 separately, then restore -1.
- Repeat the hidden/revealed checks with F2 presentation disabled. Already-fired bullets must keep travelling/resolving normally.
- Inspect many overlapping placements and range-override changes: bounded queue processing, no repeated static-circle updates and no source leaks on removal. FPS and gameplay acceptance remain owner checks.
