# PERF1 — Shared Projectile Simulation and Rendering

**Repository:** `ben0kay/godot-fortress-defense` · **Branch:** `main`  
**Status: Planned — Codex implementation specification. No source implementation is implied.**

## Decision

**Replace the earlier suggestion to first pool individual CombatProjectile nodes.** The preferred first optimization is one **world-owned, array-backed projectile simulation and shared tracer renderer** for the **existing simple straight friendly and hostile shots**. Every active shot still has independent state and is resolved individually. Reuse vacant array slots after impact/expiry; do not create/free one Node2D per shot.

Make it ready for future visual glow and a strictly capped shared world-light lease, but **do not implement any new lighting or glow in PERF1**. Do not replace homing, rockets, splash, beams, hit effects or other specialized delivery in this pass. Future complex attacks may retain independent nodes/adapters.

## Read first

Refresh `AGENTS.md`, `NOTES/CodePreferences.md`, root `README.md`, `COMBAT/README.md`, this plan and current scenes/sources. Read:
- `COMBAT/Projectiles/CombatProjectile.cs`, `COMBAT/CombatLayers.cs`, `COMBAT/CombatGeometry.cs`, `COMBAT/Damage/DamageInfo.cs`.
- `FORTRESS/BUILDINGS/SHARED/Combat/ProjectileTower.cs`, Tower001/Tower002 configs/scenes and PWR1 shot-funding hooks.
- `ENEMIES/ATTACKS/EnemyRangedAttack.cs`, `ENEMIES/CORE/EnemyRegistry.cs`, `EnemySpatialIndex.cs`, `ENEMIES/AI/EnemyTargetRoster.cs`.
- `WORLD/Grid/WorldGrid.cs`; `LEVELS/LevelBase`, Level001 and DEBUG composition / RootCombat/Projectiles.
- **Reference only** `ben0kay/godot-tower-defense` branch `dev`: `TOWERS/Shared/Projectiles/FriendlyProjectileSystem.cs`, associated flight/drawing classes, `ENEMIES/Projectiles/EnemyBulletSystem.cs`, its renderer/data, `SYSTEMS/Lighting/Runtime/WorldLightingController.Projectiles.cs` and `SYSTEMS/Lighting/README.md`. Read without modifying the other repository.

## 1. Scope and owner

Implement one compact shared, level-scoped owner under `COMBAT/Projectiles/`. Suggested names `CombatProjectileSystem`, `CombatProjectileState` and optional `CombatProjectileDrawing`; adapt to real source. One authoritative physics loop and drawing surface for normal straight/tracer bullets is preferred over separate systems for two factions. If an actual hard technical reason requires separate friendly/hostile buffers, document and preserve one canonical shot API; do not duplicate gameplay rules.

A shot snapshot stores only necessary data: position, normalized direction, speed, distance/lifetime remaining, radius, tracer length/color/visual mode, immutable `DamageInfo`, friendly/hostile flag, layer mask and collision policy, optional non-owning source Building for source-footprint ignoring, and optional intended `ICombatTarget` for hostile intended-target profile. Make no duplicate damage formula; reuse existing `DamageResolver` via target-owned `ApplyDamage(DamageInfo)`.

Expose `LaunchFriendly(...)` and `LaunchHostile(...)` or equivalent with compatible arguments, and an actual-success result if launch may fail. Match all existing `Initialize` and `InitializeHostile` semantics. Adapt `ProjectileTower` and `EnemyRangedAttack` to call this owner after their original combat/power eligibility checks.

## 2. Simulate exactly the current projectile rules

- Each projectile continues straight independently, from its actual muzzle at its authored speed, per physics delta; no visible snapping, pooling respawn flash or direction changes.
- Retain current swept **circle vs enemy** nearest impact using `EnemyRegistry.Query` spatial buckets and `CircleEntry` equivalent; reuse one scratch candidate list between shots, clearing each use.
- Preserve `WorldGrid.FirstObstacleFraction`, boundary sweep and earliest collision ordering; ground friendly `FriendlyTowerGround` ignores only authored Wall001-style friendly-projectile pass-through, hostile `GroundObstructed` hits first solid, air ignores ground blockers but respects world edge.
- Preserve swept collision priority/tie behavior, including obstruction-versus-enemy ties, projectile radii, finite travel/range, one impact per shot, hostile first-solid building damage.
- Preserve `IntendedGroundTarget`: direct nonreplacement target, `TargetLost` invalidation and harmless miss on target loss; **unregister event subscriptions when shot completes, resets, is cleared or its target disappears**. Consider validity on reentrancy and mutations.
- After source death/removal, a shot keeps its snapshotted `DamageInfo` and travels/hits normally; reference source only for original footprint ignoring while valid.
- Projectiles continue without a visible or valid fog target after launch. No new fog or power checks in flight.
- PWR1 **pays exactly once only for a successfully committed shot**, with no double charge and no free shots. Existing cooldowns, visual flash and randomness stay unchanged. If capacity is exhausted, allow bounded growth/fallback **without silently dropping a paid/legitimate shot**. Never reject an attack merely because a fixed buffer is full.
- Handle frame reentrancy (impact callback might trigger scene teardown/new shots), pause, invalid targets, level reload/regeneration and manual clear safely. Stable hit/update ordering if multiple shots impact within one tick; no duplicate hits.

## 3. Reusable slots, NOT inactive projectile nodes

Keep a dense/growable array or reusable slots holding active shot states. On hit/expiry release target subscriptions and any future cosmetic leases, swap-remove or stable-compact with documented deterministic ordering. Retire all references to owners and targets, so pooled slots cannot pin destroyed Godot instances or leak events. Bounded initial capacity and configurable soft growth ceiling; an honest fallback if a ceiling is reached. Avoid allocating List/Resource/Node per active shot/per tick. Shared simulation sleeps when there are no shots. No speculative ECS.

If launch occurs inside a hit callback, stage it safely for a subsequent update so the active iteration is not invalidated. Preserve the shot's launch position/timing and avoid a changed attack rate.

## 4. Shared tracer drawing; exact baseline art

Use a single `Node2D._Draw` for all **onscreen** active simple tracers, with cached viewport bounds and padding for line tail. Reproduce existing `CombatProjectile._Draw` precisely:
- Friendly amber wide translucent streak + narrow bright core.
- Hostile orange-red core.
- Original tracer length, 4px wide soft backline and 1.5px core, alpha/tint and orientation.
- No new glow, gamma, lighting, blending, scaling or fading in PERF1.
- Drawing positions/orientations must be computed in world-to-renderer-local coordinates correctly, including nonidentity transforms. Reuse temporary objects; cull visually only, **never stop physics/collisions offscreen**.
- Shared `_Draw` can issue many Godot canvas drawing commands; **do not claim it is one GPU draw call** or guaranteed faster without measurement.

## 5. Future lighting-compatible hook, not active lights

Keep shot state/snapshots extensible for a **separate** optional visual profile, glow tint and light eligibility. No PointLight2D per bullet, no actual PointLight2D in PERF1.

Follow the reference world's existing **budgeted light lease** idea when implementing **LGT1**, not a separate uncontrolled PointLight2D for each shot:
- Tracer core and cheap fake halo can be drawn for many bullets in shared renderer (later PERF2).
- Real terrain-illuminating lights use a shared world pool, small configurable projectile budget, viewport culling and priority, with a global cap shared with Citadel/building lights. Normal bullets function identically even when no light slot exists.
- Visual-light admission/expiration is cosmetic; never affects collision, gameplay visibility/fog reveal or damage. Lease cleanup must survive slot swaps/removals and scene exit.
- Reference Godot Tower Defense documents 4 optional projectile lights, 34px radius, 0.34 energy, 24 total world lights. These are examples, **not Fortress Defense settings**.
- Do not assign lights just because every projectile looks luminous. Use explicit style opt-in and preference for important slow/plasma/rocket attacks in later LGT1.

## 6. Compatibility / risk boundaries

Preserve current Tower001/Tower002 and Enemy002 baseline, DMG0 typed Kinetic packet and independently authored affinity, friendly Wall001-overflight, hostile wall impact, air/ground masks, enemy spatial index, exact collision ordering, actual muzzle locations, projectile power payment and cooldown, projectile visuals, fog acquisition, PWR0/PWR1 and C1 construction.

Do not change enemy AI, Enemy001 melee, Enemy003 impact, navigation, WorldGrid obstruction, visual enemy rendering, camera, power budget or combat balance. Do not add new weapons, AoE/beam, status effect runtime, world lighting, decals or FX in this pass. Keep standalone `CombatProjectile` as an adapter only if still required by existing fixtures/direct callers; remove only after real callers are migrated and verified.

## 7. Focused checks and observability

Add very small debug counters (read-only, no polling UI): Active, Peak, Spawned, Hit, Expired, Capacity/Resizes and optional query time; avoid global profiler frameworks. Direct source/diff and one build when practical per CodePreferences; owner performs FPS/visual gameplay acceptance.

Owner checks with F2/Level001 or DEBUG:
1. Tower001 ground, Tower002 air and Enemy002 hostile projectiles show same colour, tracer length, direction and timing.
2. Friendly shot passes Wall001, other solids stop it; hostile shot hits Wall001; air shot over ground solids.
3. Fast swept shots hit first eligible enemy/building, miss on valid target loss, hit precisely once.
4. A projectile still hits after its shooter is sold/destroyed; paused gameplay stops it; regeneration/F2 Clear and reload leave no retained target subscriptions.
5. Starved/powered Battle Tower PWR1 consumes exactly once per shot; no free shots, missed launch or altered cooldown.
6. Stress high rates of concurrent friendly and hostile bullets; compare identical conditions against baseline using actual recorded numbers (owner-run). No artificial projectile cutoff.

## Follow-on passes (NOT included)

- **PERF2:** optional low-cost visual halo/glow in shared tracer renderer with authored colour/opacity, offscreen culling and batching; no native light requirement.
- **LGT1:** shared/budgeted/priority projectile world lighting as part of overall world light manager, accounting for tower/Citadel lights and future fog rules.
- **PERF3:** profile-driven changes only (e.g. MultiMesh for a repeated style, or spatial query refinements) if still beneficial; do not assume current shared draw already means a single GPU draw call.

## Delivery

Update `COMBAT/README.md` and any projectile-specific docs after implementation; mark **Source Implemented / Awaiting Owner Validation** only if implementation actually lands. Commit and push one focused PERF1 change to `main` and report SHA, API, migrated call paths, source/build checks performed, remaining issues and practical owner tests.

**STOP after PERF1.** No PERF2, LGT1 or PERF3 without separate authorization.
