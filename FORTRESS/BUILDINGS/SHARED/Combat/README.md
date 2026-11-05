# Shared projectile-tower component

**Source Implemented / Awaiting Owner Validation (AIR2 extraction).** ProjectileTower : Building, IBuildingCombat owns the existing stationary-base/rotating-turret weapon loop formerly on Tower001. ProjectileTowerConfig supplies Inspector data and validation; each placed instance snapshots it. COMBAT owns spatial target acquisition, layer filtering, aiming helpers and actual projectiles. No passive building is forced to run a weapon.

Scene contract under inherited Building.Visuals: BaseSprite; TurretPivot/GunSprite; TurretPivot/Muzzle with optional simple polygon Flash (currently required by this focused component). Art uses original64px registration and scales uniformly to the smaller footprint dimension. Tower002 has its own local art/definition/config; Tower001 retains original paths through thin compatibility subclasses and unchanged `.tres` values.

Placement composition still injects world grid, EnemyRegistry and RootCombat/Projectiles through IBuildingCombat. There are no hard-coded level paths or tower-ID switches here. Empty registries/None targeting disable physics; registry changes wake live placed towers. Acquisitions remain spatial/staggered, cached references are layer-checked, shots retain launch stats and removal unbinds subscriptions. This extraction changes code ownership, not Tower001 balance or visuals.

Settings: damage/rate/range/speed, TargetLayers, rotation/tolerance/search/spread, projectile radius/tracer. No economy/power/upgrade/heat/ammo manager or separate tower hierarchy is introduced; both scenes inherit the universal Building scene. Owner build/gameplay validation remains outstanding when toolchain is absent.

## FW0 visibility

**Source Implemented / Awaiting Owner Validation.** See [fog ownership, config and limits](../../../../WORLD/FOG/README.md). Shared LevelBase/DEBUG bind CPU and optional player sources through the existing WorldGrid. F2's **Fog of War Enabled** bypasses presentation only; exploration keeps tracking. Enemy visuals, shots and destruction effects are clipped without changing simulation. Friendly bases remain known/dim; unseen HP/selection/turret feedback is suppressed. LevelConfig → Fog/FogEnabled selects settings/initial presentation; camera controls never reveal terrain. Debug diagnostics refresh at 4 Hz, and standalone Debug instances can receive an explicit Fog node reference. Runtime and FPS comparisons remain owner checks.
