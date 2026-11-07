# Tower001 — basic Level 1 Battle Tower

**Status: Source Implemented / Awaiting Owner Validation.** `Tower001.tscn` inherits the canonical `SHARED/Building.tscn`; its root script derives from Building and retains shared placement, health, footprint, selection and destruction. This pass has no heat, ammunition, power, levels/upgrades, ranks, XP, construction, research, shields or detailed tower Inspector UI.

## Use

In **Level001 or DEBUG/test.tscn**, press **T** and click/drag empty buildable ground to place Tower001. **W** returns to wall placement. Click existing structures to select; selected towers show a range circle/HP and tint. Right-click/Delete removes a structure except CPU. F2 adds enemies through the reusable debug panel. All placement/terrain/player-marker/actor-overlap checks and dynamic navigation remain shared.

The fixed base and independent `Visuals/TurretPivot/GunSprite` use local exact copies of the reference Battle Tower's `Artwork/battle_base.svg` and `battle_gun.svg`. The muzzle remains `(0,-28.5)` in the original 64px registration. Visuals scale uniformly to the footprint's smaller pixel dimension: original size in Level001's 64px grid, half-size in DEBUG's 32px grid. A tiny passive polygon flash is shown briefly on firing; there is no recoil/effect framework.

## Inspector authoring

| File | Values |
| --- | --- |
| `Tower001Definition.tres` | Stable ID/category, footprint (1×1), health (1000) |
| `Tower001CombatConfig.tres` | Damage30, rate1.68/s, range323.4px, projectile speed2300px/s |
| Same combat config / aiming | Turn216°/s, aim tolerance12°, target search0.15s, launch spread±2.5° |
| Same combat config / presentation | Projectile radius1.5px, tracer length18px |

These approximate the **current Level 1 row in `godot-tower-defense/TOWERS/Offensive/BattleTower/BattleTowerLevels.cs`**, rather than its historical README balances. Combat config exports are editable in Godot; each tower snapshots them when placed. Change authored values then place new towers/restart to apply them; existing bullets keep immutable launch damage/speed/direction. Zero fire rate disables shooting. Searches are clamped to at least0.05s; extreme/nonfinite configuration is rejected before use. No upgrade row machinery is imported.

## Behavior and performance boundaries

Tower001 uses the reference's **Closest** policy, not its route-distance-based First system or advanced priority UI. Shared combat targeting queries nearby enemy buckets, tests range/clear line of sight, then caches its target between staggered acquisitions. Turning/fire timing run at physics cadence; only the gun pivot rotates. Firing requires alignment, cooldown and a clear muzzle line. No per-frame global enemy scans or path searches are added. Empty registries put tower physics to sleep, waking via registry events.

Shots follow actual barrel forward plus the single sampled spread, not target homing. Shared `COMBAT/Projectiles/CombatProjectile.cs` performs swept circle contact against current indexed enemy positions, selecting the earliest enemy before any solid obstruction. Enemy001's existing ApplyDamage/death/registry cleanup handles hits (30 HP defaults mean one hit kills). Misses can hit another enemy; they expire after acquisition range plus one grid cell. Dead terrain, CPU and other blocking buildings block shots; Wall001 and explicitly opted-out low barriers allow friendly rounds over them; only the firing tower's own footprint is ignored at barrel exit. No friendly structure damage or wall piercing.

`EnemySpatialIndex` belongs to the existing EnemyRegistry, updates only when an enemy crosses a128px bucket boundary and is shared by targeting/projectiles. Movement uses Enemy.SetMovementPosition so the index stays synchronized; future teleports must use the same method. Reused candidate lists avoid allocation per query. This is intended for20+ towers/100–200 enemies, **not a measured FPS guarantee**; no pooling or speculative combat manager was added.

A `BuildingPlacement.Placed` event lets LevelController bind the optional IBuildingCombat capability to the current grid/registry/RootCombat/Projectiles. Passive buildings remain unaffected. Removal/death detaches subscriptions and stops firing immediately; already-fired shots retain their snapshots. Full regeneration/reload frees projectiles with the scene. F2 → Clear clears enemies/pending spawns (C toggles camera); existing missed bullets expire shortly afterward.

## Reference provenance / owner checks

Local SVG copies originate in `ben0kay/godot-tower-defense/TOWERS/Offensive/BattleTower/Artwork/`; see Artwork/README.md. Also inspected BattleTower.tscn/.cs, BattleTowerLevels.cs, its config/README, shared TowerTargeting, TurretAiming and BasicProjectile. Only the basic art, numeric defaults and small aiming/acquisition/projectile patterns were adapted. The reference repository was not changed and there is no live dependency.

Minimal checks here: source/diff review, SVG copy hashes, scene/resource references. Godot/.NET are unavailable, so **no build/runtime or FPS result is claimed**. Owner: open both scenes, T-place several towers, F2-spawn enemies and inspect independent turret rotation, real tracers/hits/deaths, and unchanged walls/nav. Try blocking the firing line, moving targets across bucket boundaries, clearing while bullets fly, destroying/removing towers and same-seed regeneration. Confirm all collision and selection footprints remain aligned.

## AIR0 layer capability

CombatConfig.TargetLayers defaults **Ground** (flags: None/Ground/Air/Both). Existing Enemy001/Enemy002 remain eligible; Enemy003 is ignored. The shared acquisition and cached-validity helpers enforce the same mask without enemy-name checks, still using nearby enemy buckets at the existing0.15s staggered cadence. Each shot snapshots the selected enemy's plane: Ground shots retain terrain/building obstruction and cannot hit air; Air shots ignore ground geometry and cannot hit ground. Both permits selecting either, rather than making every shot indiscriminately cross layers. None disables weapon processing. AIR0 added capability support; AIR2 now provides Tower002 as a separate Air-only catalogue building. No progression was added. For optional owner AA-contract inspection, temporarily author Air/Both and place new towers, then restore Ground. Source Implemented / Awaiting Owner Validation.

AIR2 extracts the common implementation/config into `SHARED/Combat/ProjectileTower.cs` and ProjectileTowerConfig.cs. Tower001.cs and Tower001CombatConfig.cs remain at their original paths as thin subclasses, so existing Tower001 scenes/resources, Ground defaults, original artwork and balance are preserved. Tower002 uses the shared component directly with its own art/definition/config. No copied parallel firing loop.

## FW1 building vision

**Source Implemented / Awaiting Owner Validation.** This tower now reveals a static circle using its authored/snapshotted weapon range (default 323.4px). Building → VisionRadiusOverride defaults -1 (automatic); a nonnegative override supplies independent sight for future artillery/sensor designs. Shared acquisition and cached-target checks require current friendly sight from any source, even while F2 fog presentation is disabled. Loss of sight stops tracking/new shots at the next combat check; fired bullets continue. Layer masks and weapon values are unchanged. Registration/removal and overlapping sight use the existing fog queue/counts. See [FW1 checks](../../../../../WORLD/FOG/README.md#fw1-owner-acceptance).

COMBAT1 (**Source Implemented / Awaiting Owner Validation**) uses FriendlyTowerGround for acquisition, final pre-fire LOS and delivery, with the weapon's projectile radius. Only BuildingDefinition.BlocksFriendlyTowerProjectiles=false barriers are skipped. Ground enemy collisions, FW1 sight, range/cooldown/alignment and all movement/hostile-shot rules remain intact. Owner: compare walls versus CPU/towers/dead terrain between this tower and a visible Enemy001; inspect multiple segments and high-speed swept hits.

## C1 Level 1 construction

**Source Implemented / Awaiting Owner Validation.** A committed Tower001 placement immediately reserves its footprint and shows the foundation. The Citadel drone arrives and deploys its beam before the three-second work clock begins. Artwork advances at 35% and 70%. At completion the existing building keeps its HP and restores the finished layers, operational sight and staggered weapon acquisition. Until then it provides fallback building vision and can take damage. The drone returns and can take another site.

Tower001's Inspector exposes ConstructionEnabled and ConstructionSeconds (default 3). ConstructionSprite.Art references the authored stage resource. Select an unfinished tower to see Awaiting Drone or Constructing and its percentage in the existing level HUD. Free DEBUG placements use the same construction lifecycle. See the [drone service](../../../../SYSTEMS/ConstructionDrones/README.md) and [construction art](Artwork/Construction/README.md).
