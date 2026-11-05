# COMBAT — basic shared projectile/targeting foundation

**Status: Source Implemented / Awaiting Owner Validation.**

- `IBuildingCombat.cs`: optional capability receiving the current world's grid, EnemyRegistry and projectile container from placement composition. No weapon dependency is imposed on Building.
- `Targeting/CombatTargeting.cs`: nearby-bucket closest target search, current cached-target validity, clear grid line and shortest-arc tracking for up-facing gun artwork.
- `Projectiles/CombatProjectile.cs`: immutable launch stats, straight physical travel, amber tracer, finite travel distance and once-only swept enemy hits through existing Enemy.ApplyDamage. First terrain/building/map-boundary contact blocks a shot before any farther enemy; the firing footprint is ignored. No original target reference is held after launch.

Projectiles live at `RootCombat/Projectiles` and disappear with scene reload. Their broadphase uses EnemyRegistry's shared EnemySpatialIndex; their narrow phase is segment/circle entry, avoiding tunnelling at2300px/s. WORLD owns obstacle geometry and the matching earliest-contact fraction. Already fired bullets continue independently of tower removal; misses expire after range+one cell. There is no automatic bullet pooling, homing, shields, typed damage pipeline, status ammo, power, heat or research. Scalar building/enemy health receivers remain the existing owners rather than introducing a redundant health manager.

Tower-specific config/art/flash logic stays under `FORTRESS/BUILDINGS/Defense/OffensiveTowers/Tower001/`. Runtime/visual acceptance and rendered stress measurement remain owner checks; only source and reference sanity checks were possible here without Godot/.NET.
