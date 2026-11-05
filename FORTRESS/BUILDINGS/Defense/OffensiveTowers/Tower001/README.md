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

Shots follow actual barrel forward plus the single sampled spread, not target homing. Shared `COMBAT/Projectiles/CombatProjectile.cs` performs swept circle contact against current indexed enemy positions, selecting the earliest enemy before any solid obstruction. Enemy001's existing ApplyDamage/death/registry cleanup handles hits (30 HP defaults mean one hit kills). Misses can hit another enemy; they expire after acquisition range plus one grid cell. Dead terrain, CPU, walls and other buildings block shots; only the firing tower's own footprint is ignored at barrel exit. No friendly structure damage or wall piercing.

`EnemySpatialIndex` belongs to the existing EnemyRegistry, updates only when an enemy crosses a128px bucket boundary and is shared by targeting/projectiles. Movement uses Enemy.SetMovementPosition so the index stays synchronized; future teleports must use the same method. Reused candidate lists avoid allocation per query. This is intended for20+ towers/100–200 enemies, **not a measured FPS guarantee**; no pooling or speculative combat manager was added.

A `BuildingPlacement.Placed` event lets LevelController bind the optional IBuildingCombat capability to the current grid/registry/RootCombat/Projectiles. Passive buildings remain unaffected. Removal/death detaches subscriptions and stops firing immediately; already-fired shots retain their snapshots. Full regeneration/reload frees projectiles with the scene. C clears enemies/pending spawns; existing missed bullets expire shortly afterward.

## Reference provenance / owner checks

Local SVG copies originate in `ben0kay/godot-tower-defense/TOWERS/Offensive/BattleTower/Artwork/`; see Artwork/README.md. Also inspected BattleTower.tscn/.cs, BattleTowerLevels.cs, its config/README, shared TowerTargeting, TurretAiming and BasicProjectile. Only the basic art, numeric defaults and small aiming/acquisition/projectile patterns were adapted. The reference repository was not changed and there is no live dependency.

Minimal checks here: source/diff review, SVG copy hashes, scene/resource references. Godot/.NET are unavailable, so **no build/runtime or FPS result is claimed**. Owner: open both scenes, T-place several towers, F2-spawn enemies and inspect independent turret rotation, real tracers/hits/deaths, and unchanged walls/nav. Try blocking the firing line, moving targets across bucket boundaries, clearing while bullets fly, destroying/removing towers and same-seed regeneration. Confirm all collision and selection footprints remain aligned.
