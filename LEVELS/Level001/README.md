# Level001 — WG0 procedural battlefield

**Status: Source Implemented / Awaiting Owner Validation.** Open `LEVELS/Level001/Level001.tscn` in Godot .NET, build C#, and run **F6**. F5 still runs the independent DEBUG sandbox.

## Initial world

`Level001.tscn` inherits `../SHARED/LevelBase.tscn` and supplies `Level001Config.tres`. Defaults: **63×63 cells ×64 pixels = 4032×4032 pixels**, target **22% dead terrain**, 12 connected cluster seeds, center CPU and an approximately 8-cell-radius ground clearing. Dead terrain is solid black, unbuildable and impassable. Ground is muted blue-gray. No per-cell scenes or processing scripts: one renderer and one static body with merged rectangular shapes cover the authoritative solid cells exactly.

The existing inherited Citadel and Wall001 keep shared health/placement/destruction. Enemy001 uses the existing runtime, queue and edge spawner, with level-local stats: 10px radius, 110px/s and **40px contact range** (the debug world's 24px range cannot reach a target from a 64px cell). Other defaults remain 30 HP, 8 damage / 1.2s, 0.8s spawn interval and 20 active maximum. No new enemy system or pressure modes.

`PlayerSpawn` is the spawn marker beside the CPU. Its cell stays reserved against construction and traversable as before. Player spawning defaults off for the benchmark; enable Config.SpawnPlayer to instantiate Config.PlayerScene there.

## Inspector settings

Select root **Config → Level001Config.tres**:

| Resource | Settings |
| --- | --- |
| Grid | Width/height in cells, cell size, diagonals, whole-cell clearance |
| Generation / Reproducibility | Original seed (112358), randomized vs sequential next seed, max random attempts (6) |
| Generation / Terrain Clusters | Coverage (0.22), count (12), size bounds (24–160), irregularity (0.65), elongation (up to 4), branching (0.35), smoothing (1), minimum fragment (12) |
| Generation / Playable Areas | CPU clearing radius (8 cells), protected edge band (2 cells), minimum reachable edge cells per side (4) |
| EnemyStats | Existing Enemy001 HP/radius/speed/turn/contact stats, scoped to this level |

The spawner and path-budget nodes remain Inspector-editable on the inherited scene. Terrain colors/classification are under `WORLD/Terrain/TerrainDefinitions/`. Grid dimensions in cells and cell size are authoritative; pixel extent is their product. Rectangular worlds are supported. Restart after authoring changes. Invalid/impossible clearing, edge or numeric inputs fail clearly instead of rerolling forever.

## Controls and regeneration

- **F1:** world details and **Regenerate World / Regenerate Same Seed** buttons. Seed, actual accepted coverage, attempts/accepted/rejected counts, derived seed, repair use, collision rectangle count and player cell are shown.
- **Regenerate World:** reload with a different random seed; if RandomizeSeedOnRegeneration is off, use current seed +1. Initial launch always uses the authored seed.
- **Regenerate Same Seed** or **R:** reload the original accepted layout for the current seed/settings, resetting all enemies, walls, CPU health, reservations, collision and path work.
- **Wheel / middle-drag:** zoom / pan to inspect the finite world. Starts framed to show the whole map; zoom in to inspect individual units.
- **Left-click/drag:** place walls; click existing structure to select. **Right-click/Delete:** remove wall. CPU is protected from accidental removal.
- **P:** toggle spawning. **F2 → Clear:** clear enemies/cancel queued spawns/pause automatic spawning. **C:** toggle free/follow camera; **Home:** frame full map. **B:** optional cell/route overlay; S/G set its demonstration endpoints. H/K remain developer damage/kill controls.

Regeneration passes a single scene-scoped seed through a clean reload. It does **not** edit/save the authored `.tres`, keep old actors alive, regenerate per frame or reuse obsolete paths. Same seed **and same generation settings/code version** reproduce the initial terrain; placements and elapsed combat are reset, not restored. Accepted count is 1; rejected counts are per regeneration. Up to 6 random candidates plus one repaired candidate are validated, so the maximum displayed attempt count is 7.

## Generation/validation contracts

Growth uses directional connected frontiers, randomized elongation and smooth lattice noise, followed by boundary cleanup and minimum-fragment filtering. Several clusters can merge; count/size settings control growth seeds/budgets, not a guarantee of exactly 12 separate components. Coverage is approximate after cleanup, protected space and optional repair.

CPU footprint/approaches and player cell are checked before any terrain is accepted. A startup flood treats the CPU footprint as blocked and uses the same clearance policy as shared navigation, proving routes from appropriate edge regions on all four sides. If bounded candidates fail, one repair carves clearance-wide cardinal connections, cleans fragments and revalidates; repair is reported and can reduce coverage/introduce straighter openings. Impossible settings still fail instead of silently loading an invalid world.

The existing spawner filters edge candidates through initial CPU connectivity and current walkability/occupancy. Player-built walls can subsequently seal the CPU; existing queued repath and accessible-wall breach behavior remains active. Failed route decisions sleep until topology changes, as in ST1.

## Brief owner checks / limitations

Build and run F6; inspect 4032px extent, black formations, central clearing, marker reservation and enemies reaching/damaging the CPU. Try wall placement on dead terrain (rejected), beside the CPU (allowed), and on the reserved player cell (rejected). Enclose CPU and inspect wall breach/recovery. F1 → same seed should restore identical initial terrain; new world should change shapes and reset counts/HP/obstacles without ghosts. Check rectangular dimensions and visible physics shapes against the black regions. Run DEBUG/test.tscn separately to confirm the fixed sandbox remains usable.

Only source/diff and scene/resource checks were possible here: Godot and dotnet are unavailable. No build, editor acceptance or FPS claim. Terrain is generated synchronously once at startup; there is no streaming. Appearance is intentionally grid-based black geometry. Existing ST1 limitations remain (overlapping enemies, pauses during queued replacements, heuristic breach selection). This is a level foundation, not a completed campaign: no player weapons, victory/progression, advanced towers, four pressure modes, water/lava, deposits, biomes, fog or construction animation.

## Shared debug scene and root containers

Level001 now inherits RootWorld / RootFortress / RootEnemies / RootCamera / RootUI / RootDebug from LevelBase. `RootDebug/Debug` instances `UI/DebugPanels/Debug.tscn`: persistent top-right FPS, live enemy count and queued count; **F2** opens manual Add10/50/100/200/custom, automatic-spawn toggle and clear controls. Edit the shared packed scene to apply UI changes across scenes. F1 keeps world generation controls. Manual requests use the existing spawner, at most2/frame by default, capped at200 active+pending; they raise the active cap and wait if valid edge space is unavailable. Clear cancels requests and pauses spawning. No advanced ST2 path priorities or performance results are introduced.

## Tower001 basic combat

Press **T** to place/paint Tower001, **W** to return to walls. Click an existing structure to select it; towers show range/HP and their base stays fixed while the gun rotates. F2-spawn enemies to inspect straight amber projectile hits through Enemy001's existing health/death handling. Right-click/Delete removes towers as well as walls. RootCombat/Projectiles owns fired rounds; clearing enemies leaves any already-fired rounds to expire, while reload/regeneration resets all combat nodes.

Tower001 uses local copies of the original Battle Tower base/gun and approximate Level1 values (30 damage,1.68/s,323.4px range,2300px/s,216°/s turn). Inspector config is under `FORTRESS/BUILDINGS/Defense/OffensiveTowers/Tower001/`. Art scales to the existing cell footprint, while combat ranges remain in pixels. Acquisition is closest-visible, staggered and spatially bounded; no heat/ammo/power/upgrades/ranks/XP/construction/research/shields were added. See the Tower001 README for values and owner checks. Source Implemented / Awaiting Owner Validation; no runtime/FPS result claimed.

## BM0 construction settings

**Source Implemented / Awaiting Owner Validation.** Config exposes Catalogue, StartingCredits (1,000), AllowedBuildingIds (`wall001`, `tower001`) and FreeDebugPlacement (false). W/T and drag placement use one validated purchase per new building: wall10, tower100, with credits shown in the status readout. Blocked/locked/unaffordable placement does not charge. Deleting/destroying buildings does not refund. Set FreeDebugPlacement=true for unlimited benchmark construction; the readout labels the bypass. Reset/regeneration creates a fresh wallet. DEBUG/test remains free. No kill rewards or income in BM0. BM1 adds the shared build menu. See [resource owner](../../FORTRESS/SYSTEMS/Resources/README.md).

## BM1 menu controls

**Source Implemented / Awaiting Owner Validation.** Start idle; press Ctrl, then Defense, then a building. The bottom tray and separate details panel are inherited from shared LevelBase. Green/red actual-footprint previews include actor overlap and affordability. Left-click/drag repeats purchases; Esc/RMB cancels without removing. Ctrl browsing pauses/resumes selection. W/T quick selection remains; after cancelling, click existing structures to select, RMB/Delete to remove. F1/F2 stay intact. Config.FreeDebugPlacement enables labelled free benchmark construction. See [shared menu docs](../../UI/BuildMenu/README.md).

## Revised PL0/CAM0 options

Config.SpawnPlayer=false preserves the no-player benchmark; PlayerScene is preassigned to PLAYER/Player.tscn. Enable spawning for a movable/damageable chassis (arrows). PlayerRequiredForDefeat=false and CitadelRequiredForDefeat=true configure the future defeat-request event, without results/mission-stop UI. EnemyStats exposes TargetRule and PlayerDetectionRange. RootEnemies/TargetRoster exposes bounded decision cadence/budget. RootCamera/Camera2D exposes zoom/follow/pan/bounds settings; starts free/full-map and C only follows a living actor. Home frames everything. Shared RootPlayer is empty when disabled. See PLAYER/README and CAMERA/README for owner checks and limitations.

EN2: F2 → enemy002 → Add spawns Building-Hunter Shooters using their separate definition, alongside any Enemy001s. Automatic Enemy001 pressure is unchanged. New buildings are found through staggered local spatial scans; local/global target counters are visible in expanded F2. Test with player disabled/enabled and verify Enemy002 only attacks buildings. See ../../ENEMIES/TYPES/Ranged/Enemy002/README.md for settings and7 owner scenarios; no FPS result claimed.

AIR0/AIR1: F2 includes enemy003 Basic Flyer. It flies over terrain/buildings and impacts only the CPU for15 damage once, then despawns. Tower001 defaults Ground-only and still attacks Enemy001/Enemy002. Mix all three in the existing200 combined-cap batches; optional player, C camera, Home and Ctrl menu are unchanged. See ../../ENEMIES/TYPES/Flying/Enemy003/README.md for settings and owner checks. AIR2 now adds Tower002; AIR3/gunship remains planned.

AIR2: Config.AllowedBuildingIds now includes tower002. Ctrl → Defense → Anti-Air Tower / Tower002 buys/places it for100 Credits. F2 enemy003 tests air combat; use Tower001 for enemy001/002. The common tower weapon component is shared; world generation, optional player/camera, existing spawning and navigation are unchanged.

## FW0 visibility

**Source Implemented / Awaiting Owner Validation.** See [fog ownership, config and limits](../../WORLD/FOG/README.md). Shared LevelBase/DEBUG bind CPU and optional player sources through the existing WorldGrid. F2's **Fog of War Enabled** bypasses presentation only; exploration keeps tracking. Enemy visuals, shots and destruction effects are clipped without changing simulation. Friendly bases remain known/dim; unseen HP/selection/turret feedback is suppressed. LevelConfig → Fog/FogEnabled selects settings/initial presentation; camera controls never reveal terrain. Debug diagnostics refresh at 4 Hz, and standalone Debug instances can receive an explicit Fog node reference. Runtime and FPS comparisons remain owner checks.

## C1 Tower001 construction

**Source Implemented / Awaiting Owner Validation.** Place Tower001 and select the site to see its status. Check that credits debit once, the footprint is solid immediately, and the work clock stays at zero until the drone deploys its beam. Watch the 35% and 70% art changes and confirm the gun activates at completion. Place more than four towers to exercise the queue. Remove or damage an unfinished tower, toggle F2 fog, and regenerate while drones are active. The default Citadel-loss fallback finishes paid sites locally if a builder cannot deploy.
