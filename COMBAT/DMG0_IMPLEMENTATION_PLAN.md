# DMG0 — Shared Typed Damage, Layer Affinities and Future Status Contracts

**Repository:** `ben0kay/godot-fortress-defense` · **Branch:** `main`  
**Status:** Planned — Codex implementation specification. This document does **not** mean source is implemented.  
**Companion:** [UNIFIED_DAMAGE_ROADMAP.md](UNIFIED_DAMAGE_ROADMAP.md).

## Task for Codex

Implement **DMG0 only**, using this specification and the current repository source as authority. Read `AGENTS.md`, `NOTES/CodePreferences.md`, root `README.md`, `COMBAT/README.md`, companion roadmap, and all relevant sources **at task start**. Do not modify any reference repository. Commit and push to **main** after minimal checks; label as **Source Implemented / Awaiting Owner Validation**, not Accepted.

### Reference implementations (read-only)

1. `ben0kay/godot-tower-defense` **dev**: `COMBAT/Damage/DamageInfo.cs`, `DamageType.cs`, `DamageResolver.cs`, `DamageResponseProfile.cs`, `COMBAT/Attribution/DamageSource.cs`, `COMBAT/AreaDamage/AreaDamage.cs`, `COMBAT/StatusEffects/{StatusEffectId,StatusEffectPayload,DamageOverTimeStats}.cs`, `ENEMIES/StatusEffects/`, `TOWERS/Shared/Weapons/Delivery/WeaponHitDelivery.cs`, shared projectiles/beams, Cryo/Stasis towers.
2. `ben0kay/spaceshooter_vector_2026` **main**: `scripts/sc_Enums/sc_Enums.gml`, `scripts/sc_damage/sc_damage.gml`, `scripts/sc_damage_effects/sc_damage_effects.gml`, `scripts/sc_enemy_damage_affinity/sc_enemy_damage_affinity.gml`, `scripts/sc_runtime_effects/sc_runtime_effects.gml` and damage `types` in `scripts/sc_config/sc_config.gml`.
3. `ben0kay/VECTOR_TD_2026` **main**: `scripts/scr_damage/scr_damage.gml`, `scripts/scr_beam_hitbox/scr_beam_hitbox.gml`, friendly/hostile projectile scripts.

Adapt concepts rather than copying TowerBase, player progression or GameMaker runtime logic.

## Current migration targets

Before editing, trace every `ApplyDamage(float)` call, public `ICombatTarget`, debug damage hook, and collision/attack path. Specifically inspect:
- `COMBAT/ICombatTarget.cs`, `COMBAT/CombatLayers.cs`, `COMBAT/CombatGeometry.cs`, `COMBAT/Projectiles/CombatProjectile.cs`, `COMBAT/Targeting/CombatTargeting.cs`.
- `ENEMIES/CORE/Enemy.cs`, `ENEMIES/ATTACKS/{EnemyMeleeAttack,EnemyRangedAttack,EnemyImpactAttack}.cs`, relevant enemy capabilities and projectile launch.
- `PLAYER/Player.cs`, `FORTRESS/BUILDINGS/SHARED/Building.cs`, `FORTRESS/BUILDINGS/SHARED/Combat/ProjectileTower.cs`, Citadel/level objective handling, level/world composition.
- Tower001 (ground) and Tower002 (air), Enemy001 melee, Enemy002 ranged, Enemy003 impact, debug manual damage, unfinished C1 construction sites.

`ICombatTarget` currently covers **Player and Building**, whereas `Enemy` is a separately owned receiver. Choose the smallest safe typed receiver abstraction (e.g., `IDamageReceiver` or direct typed overloads), not a forced broad rewrite of targeting contracts.

## 1 — Shared immutable hit contract

Add concise C# under `COMBAT/Damage/`:
- `DamageType.cs`: **Kinetic, Energy, Explosive, Electric, Thermal, Corrosive**, with stable numeric IDs documented; avoid later enum reordering.
- `DamageInfo.cs`: small immutable `readonly record struct` or equivalent; nonnegative finite base amount, valid type, optional lifetime-safe `DamageSource`, optional shield/hull/armour coefficient override as needed, shield penetration in [0,1], optional snapshotted context only if used. Zero is valid no-op. Don't use per-hit dictionaries or mutable Godot Resources.
- `DamageResult.cs`: immutable resulting layer values plus actual shield/armour/hull losses and optionally impact layer, usable for future feedback.
- `DamageResolver.cs`: single deterministic computation, no scene-tree actions, targeting, projectiles, visuals, status ticks or actor state mutation.

A projectile snapshots its hit payload **once on launch**, including type/source and final source-side values, regardless of later source destruction. Do not roll crits yet.

## 2 — Source identity

Use `COMBAT/Attribution/DamageSource.cs` or similarly bounded abstraction to distinguish Player, friendly Tower/Building, Enemy, Environment and Unknown/Debug. Preserve owner identity safely using weak references or equivalent (non-owning) semantics; no references to obsolete instances should be dereferenced. Avoid creating a new source object on every periodic tick. No XP/loot/kill attribution policy implementation in this pass; do not silently alter existing rewards.

## 3 — Layer effectiveness and deterministic overflow

Add `DamageResponseProfile` (Inspector resource) and neutral `DefaultDamageProfile.tres`:
- Global per-type multipliers for Shield / Armour / Hull, all **1.0 by default**.
- Validated nonnegative finite coefficients, no invalid enum values.
- Optional original Space Shooter coefficients **only as a separate clearly labeled reference preset**, not active by default. The reference matrix is in the companion roadmap.
- Use shield -> armour -> hull/health ordering and documented base-unit spillover. A zero-effective active layer **stops that layer's ordinary packet portion** instead of allowing a free passthrough. Explicit shield penetration bypasses shield under well-defined rules, but must still respect later armour/hull affinity and no duplicate damage.
- No negative resulting pools, double-application of resistance, NaN or runaway values; use stable arithmetic where warranted. No layer present => skip that layer.
- Keep a neutral fast path for existing unshielded/unarmoured entities.

**Do not create functional shield/armour values or regeneration on current enemies, player or buildings.** All current receivers remain health-only (existing values unchanged). The generalized resolver may accept test inputs for shield and armour.

## 4 — Authored affinity and immunity

Add compact `DamageAffinityProfile` (Inspector-editable) for per-receiver/type/layer coefficients:
- Broad all-layer per-type value, with optional Shield/Armour/Hull override for each type.
- Defaults 1; 0 means immune to a type **at the active layer**, (0,1) resistant, >1 vulnerable.
- Avoid large mutable per-instance tables and per-hit allocation; validate/configure once.
- Allow enemy definitions to specify an optional profile (inspect actual EnemyDefinition ownership), while default enemy types remain neutral.
- Provide optional support for player/building receiving profiles if appropriate without making all authors configure them.
- Test mixed layer override behavior and zero-affinity blocking, including a target with shields, with no shields, and with armour only.

**Separate the damage affinity from status immunity.** Zero direct HP damage must not accidentally be treated as mandatory rejection of every status effect.

## 5 — Typed receiver migration and existing attacks

Implement one typed `ApplyDamage(DamageInfo)` or equivalently narrow receiver path on **Enemy, Player and Building**. Keep scalar `ApplyDamage(float)` overload for existing debug/legacy tools, forwarding to neutral **Kinetic** typed hits, rather than maintaining a separate damage formula.

Receiver owns its health, events, hit visuals, death, building grid occupancy, target-loss notifications and other lifecycles. Resolver only calculates the result. Incomplete C1 buildings can still take damage. Do not change enemy HP, tower damage, projectile speeds, cooldowns, target rules, muzzle position, XP or credits.

Migrate delivery:
- Tower001 ground projectile, Tower002 air projectile: immutable launch `DamageInfo`; preserve FW1 fog acquisition; COMBAT1 friendly-ground wall overflight, first-hit ordering, swept collision.
- Enemy002 hostile ranged projectile: typed packet and exact current first-solid/wall behavior; intended-target overflight behavior where currently authored; no unintended friendly fire.
- Enemy001 melee, Enemy003 impact: emit typed hit and preserve attack/despawn one-shot semantics.
- Player and buildings receive typed damage; all existing debug damage works through compatibility overloads.

Check additional existing call paths; do not leave a second scalar-only production damage pipeline.

## 6 — Secondary-effect definitions, NOT effect runtime

Create distinct `COMBAT/StatusEffects/` **contracts only**:
- Stable `StatusEffectId` identities: Stagger, Burn, Corrosion, Disruption, CryoSlow, StasisFreeze. Document ID stability and effect-family mapping (movement control, damage over time, system impairment).
- Immutable validated `StatusEffectPayload` supporting identity, chance [0,1], duration seconds, strength, optional periodic tick interval when meaningful and safe source attribution. A hit can carry **zero or one** optional effect; don't require effects on existing attacks.
- Optional `StatusImmunityProfile` authored per receiver or definition; allow independent immunity to any effect identity, without conflating with damage immunity. A narrow API can report whether an effect **would be eligible**, but do **not** activate or tick effects yet.
- Distinguish **CryoSlow** (movement speed multiplier) from **StasisFreeze** (immobilization). Burn/Corrosion are future DoT, Disruption future system weakening, Stagger future short interruption.
- Status must not inherit effects automatically just because DamageType is Thermal or Corrosive; weapon definitions may opt in later. No default status scheduling.
- Clearly specify future status application policy: hit eligible -> resolver applies direct damage -> receiver survives -> independent immunity/eligibility check -> FX1 runtime apply. Whether zero direct damage prevents status must be configurable/explicit in FX1; do not hardwire damage immunity = status immunity.
- Do not create live timers, status stacking, movement modifiers, damage ticks, particles, or unimplemented general-purpose global managers.

## 7 — Boundaries for future delivery

Document, but do not implement:
- **FX1** shared active-only status scheduler (slow/freeze first, then stagger, burn, corrosion, disruption), seconds-based duration, stacking/refresh rules, pause/death cleanup.
- **DMG1** AoE/splash delivery + actual Splash Cannon, spatial query once, bounded radius, falloff in a copied scaled packet, each target once.
- **DMG2** sustained beam + actual Laser Tower, DPS x elapsed time/controlled ticks, expiry/LOS/eligibility, same typed resolver, no per-frame stat rereads.
- **DMG3** real shields/armour and defense behaviors.
- **DMG4** critical/rear hits, source-side once-only roll and geometry; no balance changes now.

DamageResolver must not become a weapon, effects or projectiles manager. Don't implement new weapons now.

## 8 — Performance / compatibility

Preserve:
- Existing WorldGrid physics sweep and projectile collision profiles;
- COMBAT1 wall-overflight for friendly ground towers but solid blocking for enemies;
- AIR0 air/ground eligibility;
- FW1 authoritative fog/visibility (including fog presentation toggle);
- FOG-PERF1A cosmetics, existing EnemyRegistry spatial buckets and staggered targeting;
- Player/Enemy/Building health and destruction; C1 construction lifecycle;
- Level001 127×127 grid, frame/camera behavior.

No per-hit resource lookup, reflection/boxing hot paths, per-hit manager allocations, hidden background physics, full-world scan, duplicated health storage or new game rules. Don't introduce a new damage-type-specific weapon system.

## 9 — Focused verification / owner checklist

Implement a small direct calculation smoke check if natural, and do only the lightweight checks required by CodePreferences:
1. 30 neutral Kinetic => 30 HP lost on unshielded enemy; all existing damage numbers unchanged.
2. Zero hit no-op; malformed amount/type/penetration/affinity rejected.
3. Global type x target affinity applied once, across shield/armour/hull with base-unit spillover and optional bypass; zero active-layer affinity blocks ordinary portion.
4. Enemy-only, Player-only, Building/Citadel receivers and C1 construction sites take current hits and die/detach once.
5. Tower001/002, Enemy001 melee, Enemy002 ranged, Enemy003 impact continue existing behavior.
6. Flying/ground targeting, fog, hostile wall collisions, friendly Wall001 overflight unchanged.
7. Projectile snapshots damage/source when fired and still resolves after attacker removal.
8. Direct immunity and independent status-effect immunity don't cross-activate; effect payload validations work, but no status runtime exists.
9. Scene reload/regeneration and damage-hit cleanup do not retain stale subscriptions.

No lengthy headless tests, repeated builds or benchmarks. Owner does actual Godot visual/FPS acceptance.

## 10 — Documentation, Git and handoff

Update `COMBAT/README.md` plus relevant receiver/projectile/enemy-attack docs, and reconcile this plan's status after actual implementation. Explain use of `DamageInfo`, response profiles, affinity authoring, typed receivers and deferred effects.

Commit and push **one logical implementation pass** to `main`. Return SHA, changed files, Inspector knobs, concise summary of migrated callers, lightweight checks actually performed, known limitations and owner acceptance steps.

**Stop at DMG0. No FX1, DMG1, DMG2, DMG3 or DMG4 without a separate request.**
