# COMBAT — basic shared projectile/targeting foundation

**Status: Source Implemented / Awaiting Owner Validation.**

- `IBuildingCombat.cs`: optional capability receiving the current world's grid, EnemyRegistry projectile container and authoritative fog reference from placement composition. No weapon dependency is imposed on Building.
- `Targeting/CombatTargeting.cs`: nearby-bucket closest target search, current cached-target validity, clear grid line and shortest-arc tracking for up-facing gun artwork.
- `Projectiles/CombatProjectile.cs`: immutable launch stats, straight physical travel, amber tracer, finite travel distance and once-only swept enemy hits through existing Enemy.ApplyDamage. First terrain/blocking-building/map-boundary contact blocks a shot before any farther enemy; the firing footprint is ignored. No original target reference is held after launch.

Projectiles live at `RootCombat/Projectiles` and disappear with scene reload. Their broadphase uses EnemyRegistry's shared EnemySpatialIndex; their narrow phase is segment/circle entry, avoiding tunnelling at2300px/s. WORLD owns obstacle geometry and the matching earliest-contact fraction. Already fired bullets continue independently of tower removal; misses expire after range+one cell. There is no automatic bullet pooling, homing, shields, typed damage pipeline, status ammo, power, heat or research. Scalar building/enemy health receivers remain the existing owners rather than introducing a redundant health manager.

Tower-specific config/art/flash logic stays under `FORTRESS/BUILDINGS/Defense/OffensiveTowers/Tower001/`. Runtime/visual acceptance and rendered stress measurement remain owner checks; only source and reference sanity checks were possible here without Godot/.NET.

EN2 extends CombatProjectile with InitializeHostile: same straight-travel/lifetime-distance loop and WORLD obstruction geometry, now applying scalar damage to the first solid living building. It has no held target or source enemy, so destruction invalidates nothing in flight; terrain/boundaries consume shots and no player/enemy friendly fire is enabled. Tower launch/hit behavior remains the original path. No parallel enemy projectile manager, homing or pooling.

## AIR0 explicit combat planes

CombatLayers.cs defines EnemyTargetLayers None/Ground/Air/Both and ProjectileCollisionProfile GroundObstructed/Airborne/IntendedGroundTarget. Reusable target queries and cached validity filter the layer mask before scoring; no enemy-type-name checks. Tower001 defaults Ground and snapshots a per-shot launch layer; air shots skip ground obstruction while ground shots retain it. Both-capable acquisition can select either plane; direct projectile callers may explicitly use a Both hit mask.

CombatProjectile.Initialize retains its existing parameters with optional targetLayers/profile arguments. GroundObstructed is the compatibility default. Airborne uses the same indexed swept circle contacts with map-boundary/range expiry and no ground-solid query. InitializeHostile defaults to first-solid GroundObstructed (Enemy002 unchanged); explicit IntendedGroundTarget takes an ICombatTarget, ignores intervening footprints/terrain, sweeps only that current target rectangle and detaches its TargetLost event on loss/expiry. Loss creates a straight harmless miss, never a replacement search. Profiles are validated to avoid silently mixing hostile/friendly semantics. CombatGeometry.SegmentRectEntry is shared with intended-contact impacts. No duplicate projectile registry, altitude physics or pooling; AIR2 now provides Tower002 using these contracts; AIR3 remains planned.

AIR2: Tower001/Tower002 share BUILDINGS/SHARED/Combat/ProjectileTower rather than duplicate weapon loops. COMBAT targeting, layer eligibility and projectile geometry are unchanged by the second tower; Ground and Air defaults reside in their own authored configs.

## FW1 offensive-tower visibility

**Source Implemented / Awaiting Owner Validation.** CombatTargeting.Valid/Closest now require the world's FogOfWar and current center-cell sight after alive/layer/range filtering. Closest retains spatial buckets, deterministic closest ties and clear-line checks. ProjectileTower checks cached eligibility before tracking and new shots; hidden enemies are cleared without changing staggered acquisition. Presentation bypass cannot bypass these checks. Vision and weapon range remain independent building capabilities, with towers defaulting sight to weapon range. Fired projectile collision/damage does not query fog and remains unchanged.

## COMBAT1 — friendly tower rounds over low barriers

**Source Implemented / Awaiting Owner Validation.** `BuildingDefinition.BlocksFriendlyTowerProjectiles` defaults **true** and is snapshotted on Building initialization. Wall001 explicitly sets it **false**. Future low barriers may opt out in their own definitions; no building-ID switches, separate grid, detector areas or physics bodies are involved. Author changes then place new instances/reload.

`ProjectileCollisionProfile.FriendlyTowerGround` is appended to preserve existing serialized profile values. CombatLayers.ShotProfile selects it for friendly ground-targeting towers; Airborne remains the air policy. General CombatProjectile.Initialize still defaults to GroundObstructed for compatibility. Both EnemyRangedConfig and InitializeHostile reject the friendly-only profile, preserving Enemy002's solid-wall collision and the existing intended-target overflight contract.

Acquisition, final muzzle checks and actual friendly ground travel use the same WorldGrid.FirstObstacleFraction sweep with WorldProjectileFilter.FriendlyTower. Acquisition now receives the authored projectile radius too. The filter skips only buildings whose placed obstruction snapshot is false (plus the existing firing-source exception). All other footprints, dead terrain and finite world bounds remain blockers. Existing radius-expanded slabs, earliest-obstacle ordering, solids winning enemy-contact ties and indexed swept enemy contacts are retained. Newly placed blockers are read from current occupancy during travel. Already-fired rounds retain their selected collision profile; losing a target or vision never rewrites their launch rules.

Generic SegmentClear, default FirstObstacleFraction, navigation and physical collision remain all-solid. No registry enumeration, fog changes, new reveal or enemy AI changes. FW1 still requires logical current friendly sight/layer/range eligibility even when F2 presentation is off. AA rounds continue ignoring terrain/buildings with normal bounds/air-enemy swept hits.

Owner checks: visible Enemy001 behind one/multiple Wall001 segments can be shot by Tower001; substitute another tower, CPU or dead terrain and shots must stop. Ground movement still routes around walls; Enemy002 shots still hit/damage walls; Tower002 still hits flyers across ground solids. Repeat hidden/revealed targeting with F2 off, and inspect high-speed shots near building corners/bounds. Source/diff/resource checks only here (.NET unavailable); build/gameplay/FPS acceptance remains with the owner.
