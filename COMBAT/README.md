# COMBAT — shared damage, projectiles and targeting

## DMG0 typed damage

**Source Implemented / Awaiting Owner Validation.** Both projectile towers and Enemy001 melee, Enemy002 ranged and Enemy003 impact use immutable Kinetic `DamageInfo` values at their existing damage amounts. Projectile launch copies the packet by value and hits still resolve if its emitter is removed. The emitter kind and original instance ID remain readable even after the weak owner expires. Enemy, Player and Building own HP, hit feedback, death and cleanup. Citadel and incomplete construction sites use the same Building receiver. Legacy scalar debug hits use neutral Kinetic response coefficients and still respect the receiver's authored Kinetic affinity.

- `Damage/DefaultDamageProfile.tres` is loaded once at actor initialization and defaults to 1 for every type/layer. Each type's X/Y/Z fields mean Shield/Armour/Hull. Optional immutable packet overrides replace the corresponding global coefficient.
- In EnemyDefinition, BuildingDefinition or Player, assign an optional **Damage Affinity** resource. For each type, X is the broad affinity and Y/Z/W override Shield/Armour/Hull respectively. Use -1 on an override to inherit X, 0 for immunity, 1 for neutral and values above 1 for vulnerability. Resource setters reject other negative or nonfinite values. Actors bind the shared profiles once at initialization.
- Damage is spent in base units. A layer consumes up to its remaining pool divided by its effective coefficient (response × affinity). An empty layer is skipped. An active layer with zero effectiveness stops the ordinary packet. Shield penetration splits the packet only when a shield is active. Bypass rejoins ordinary spillover before armour, so both obey armour/hull affinity exactly once. Only hull HP is live on current actors.
- **Status Immunity** is an optional independent Inspector flags resource on the same receivers. A packet may explicitly carry one validated effect, with no automatic mapping from damage type. CryoSlow multiplies movement speed and StasisFreeze immobilizes. Both are movement control, alongside Stagger. Burn and Corrosion are periodic damage contracts, and Disruption is a system impairment contract. IDs are stable. A future eligible hit resolves direct damage first, then checks receiver survival and independent effect eligibility before FX1 can roll/apply it. FX1 must explicitly decide whether zero direct damage allows a status.

[DMG0 plan](DMG0_IMPLEMENTATION_PLAN.md) · [Roadmap for FX1 and DMG1–DMG4](UNIFIED_DAMAGE_ROADMAP.md)

### Owner checks

1. Run `TESTS/Combat/DamageResolverSmoke.tscn` with F6. Look for “DMG0 calculation smoke passed.” It covers neutral and zero hits, layer overrides, base-unit spillover, bypass, immunity and invalid inputs.
2. Run Level001 with F6. F2-spawn Enemy001, Enemy002 and Enemy003. Verify Tower001 and Tower002 hit their respective ground/air targets for 30 HP. Check melee (8), hostile rounds (6) and flyer CPU impact (15), including optional Player melee and one-shot death/detachment.
3. Verify Tower001 shoots over Wall001 while Enemy002 hits solid walls. Test hidden ground targets with fog presentation off, and Tower002 over ground solids. Remove an attacker after launch and check its in-flight hit. Reload/regenerate while shots are active.
4. During C1 construction, damage or destroy the selected site with H or hostile fire and check HP, occupancy release and drone cancellation. For an affinity spot check, set an EnemyDefinition's Kinetic X to 0 and then restore it. Existing attacks carry no effects.

## Shared projectile and targeting foundation

**Status: Source Implemented / Awaiting Owner Validation.**

- `IBuildingCombat.cs`: optional capability receiving the current world's grid, EnemyRegistry projectile container and authoritative fog reference from placement composition. No weapon dependency is imposed on Building.
- `Targeting/CombatTargeting.cs`: nearby-bucket closest target search, current cached-target validity, clear grid line and shortest-arc tracking for up-facing gun artwork.
- `Projectiles/CombatProjectile.cs`: immutable launch stats, straight physical travel, amber tracer, finite travel distance and once-only swept enemy hits through existing Enemy.ApplyDamage. First terrain/blocking-building/map-boundary contact blocks a shot before any farther enemy; the firing footprint is ignored. No original target reference is held after launch.

Projectiles live at `RootCombat/Projectiles` and disappear with scene reload. Their broadphase uses EnemyRegistry's shared EnemySpatialIndex; their narrow phase is segment/circle entry, avoiding tunnelling at2300px/s. WORLD owns obstacle geometry and the matching earliest-contact fraction. Already fired bullets continue independently of tower removal; misses expire after range+one cell. There is no automatic bullet pooling, homing, live shields, active status effects, power, heat or research. Actors retain ownership of health and lifecycle.

Tower-specific config/art/flash logic stays under `FORTRESS/BUILDINGS/Defense/OffensiveTowers/Tower001/`. Runtime/visual acceptance and rendered stress measurement remain owner checks; only source and reference sanity checks were possible here without Godot/.NET.

EN2 extends CombatProjectile with InitializeHostile: same straight-travel/lifetime-distance loop and WORLD obstruction geometry, applying the launch packet to the first solid living building. The first-solid profile keeps no target reference and snapshots weak enemy attribution; terrain/boundaries consume shots and no player/enemy friendly fire is enabled. Tower launch/hit behavior remains the original path. No parallel enemy projectile manager, homing or pooling.

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
