# Unified Combat & Damage Roadmap — DMG0 onward

**Repository:** `ben0kay/godot-fortress-defense` · **Branch:** `main`  
**Status:** DMG0 Source Implemented / Awaiting Owner Validation. FX1 and DMG1 onward are Planned.

## Purpose and boundaries

All friendly towers, player weapons, enemies, environmental hazards and future devices should author the **same immutable typed damage packet**, use one defense/damage resolver, and dispatch hits through target-owned health/shield/armour lifecycles. Attack **delivery** (melee, projectile, instant, sustained beam, splash, shockwave or periodic status tick) and **target eligibility** (faction, ground/air, collision, line of sight, vision) are separate from damage calculation. Effect scheduling is separate again.

**Implementation order (one pass at a time):**

| Pass | Scope | Evidence / intended consumer |
| --- | --- | --- |
| **DMG0** | Immutable typed packets, source attribution, six types, neutral active response, shield/armour/hull resolver, per-target affinities and immunity contracts, effect payload definitions, migrate current attacks/receivers | Existing Tower001/002, Enemy001/002/003, Building/Citadel and Player |
| **FX1** | Actual status runtime, effect immunity and refresh/stack policies, finite timers, cryo slow and stasis freeze first; stagger, burn, corrosion and disruption as approved | First real status weapon / testing capability; no giant switch in damage resolver |
| **DMG1** | Shared splash/area delivery + a real splash weapon (Blaster/Splash Cannon); typed packets per target, radius falloff, faction/layer/occlusion decisions | Future friendly cannon then reused for hostile explosive attacks |
| **DMG2** | Shared sustained beam delivery + a real Laser Tower; damage-per-second with deterministic cadence/elapsed time and source snapshots | Future friendly laser then reused for hostile beams |
| **DMG3** | Functional shield/armour pools and regeneration/penetration UX, after an actor genuinely needs these | Defense-layer behaviors beyond the DMG0 resolver's mathematical support |
| **DMG4** | Crit/rear-hit/advanced modifiers, interception, additional defense rules as needed | Source-side once-per-attack rolls, positional data |
| **Later** | Expanding shockwaves, multi-effect hits, advanced DoT stacking, more exotic damage types | Only upon real weapon/gameplay requirements |

Naming is **DMG0** (not a separate DM0 feature); future pass labels above are planning identifiers, not implementation approval. See **[DMG0_IMPLEMENTATION_PLAN.md](DMG0_IMPLEMENTATION_PLAN.md)** for Codex's complete first-pass instructions.

## Original reference research

**GameMaker space shooter**: `ben0kay/spaceshooter_vector_2026` (`main`), especially:
- `scripts/sc_Enums/sc_Enums.gml`: `DamageType { KINETIC, ENERGY, EXPLOSIVE, ELECTRIC, THERMAL, CORROSIVE }`, `DamageEffect { NONE, DISRUPTION, BURN, CORROSION, STAGGER }`, defense layers NONE/SHIELD/ARMOUR/HULL.
- `scripts/sc_damage/sc_damage.gml`: immutable-at-launch packet intention, source/faction, critical metadata, knockback, layer-by-layer effective damage, source and enemy affinities.
- `scripts/sc_enemy_damage_affinity/sc_enemy_damage_affinity.gml`: generic per-type and overridden per-layer affinity (0=immune, 1=neutral).
- `scripts/sc_damage_effects/sc_damage_effects.gml`: effect chance, duration, strength, tick interval and optional system effects.
- `scripts/sc_runtime_effects/sc_runtime_effects.gml`: movement/rotation/weapon/time modifier concept.
- `scripts/sc_config/sc_config.gml`: original type coefficient matrix shown below.

**Godot Tower Defense**: `ben0kay/godot-tower-defense` (`dev`):
- `COMBAT/Damage/{DamageInfo,DamageType,DamageResolver,DamageResponseProfile}.cs`, `COMBAT/Attribution/DamageSource.cs`, `COMBAT/AreaDamage/AreaDamage.cs`.
- `COMBAT/StatusEffects/{StatusEffectId,StatusEffectPayload,DamageOverTimeStats}.cs` and `ENEMIES/StatusEffects/`.
- `TOWERS/Shared/Weapons/Delivery/WeaponHitDelivery.cs`, `TOWERS/Shared/Projectiles/`, `TOWERS/Shared/Weapons/Beams/`, CryoTower/StasisTower.
- Preserves separation of typed damage from Slow/Freeze/Corruption status effects.

**Earlier GameMaker tower defense**: `ben0kay/VECTOR_TD_2026` (`main`):
- `scripts/scr_damage/scr_damage.gml`, `scripts/scr_beam_hitbox/scr_beam_hitbox.gml`, friendly/hostile projectile scripts.

References are **read-only inspiration**. No live cross-repo dependencies, hard-coded Godot Tower Defense TowerBase or borrowed GameMaker behavior that conflicts with Fortress Defense. Codex must refresh current source/docs before each pass.

## Damage types versus effect identities

A **damage type** changes immediate interaction with defense layers; it does not automatically define a movement or damage-over-time mechanic. Proposed fixed initial enum:
- Kinetic (bullets/physical impacts)
- Energy (lasers/plasma)
- Explosive (blast)
- Electric (electrical attack)
- Thermal (heat)
- Corrosive (acid/corrosive attack)

**Effect identities**, separate from damage types:
- **CryoSlow** — sustained movement-speed reduction;
- **StasisFreeze** — temporary immobilization;
- **Stagger** — brief interruption;
- **Burn** — periodic damage;
- **Corrosion** — periodic damage; armour degradation is a later mechanic;
- **Disruption** — limited weapon/system performance.

Treat these as family groupings for reusable runtime rules: **movement control** (CryoSlow/StasisFreeze/Stagger), **damage over time** (Burn/Corrosion), **systems impairment** (Disruption). A thermal attack may carry Burn; an energy attack may carry CryoSlow or StasisFreeze; a corrosive hit may carry Corrosion. Effects may be overridden or absent per weapon. **Damage immunity does not imply effect immunity and vice versa**; effect application is an explicit policy (including on zero HP damage where valid).

**Original Space Shooter reference multipliers — optional reference preset ONLY, not default active Fortress Defense balance:**

| Type | Shield | Armour | Hull | Original default effect |
| --- | ---: | ---: | ---: | --- |
| Kinetic | 0.45 | 1.20 | 1.00 | None |
| Energy | 1.30 | 0.75 | 1.00 | None |
| Explosive | 0.75 | 1.00 | 1.40 | None |
| Electric | 1.20 | 0.40 | 0.75 | Disruption |
| Thermal | 0.40 | 1.10 | 1.25 | Burn |
| Corrosive | 0.50 | 1.50 | 1.20 | Corrosion |

The **live DMG0 profile is neutral** (all coefficients 1.0; existing direct damage values unchanged), and status effects are inactive until FX1. Original numbers must not silently change existing HP balance.

## Conceptual pipeline

```text
Weapon defines amount, type, attacker and optional effect
  -> source modifiers/critical rolls (future; once, before snapshot)
  -> immutable DamageInfo (+ optional independent effect payload)
  -> projectile / melee / beam / AoE delivery selects eligible recipients
  -> receiver DamageResolver (type x layer x target affinity; bypass/overflow)
  -> receiver commits shield/armour/HP and lifecycle/death exactly once
  -> eligible optional StatusEffectPayload dispatched to status runtime (FX1)
  -> feedback, XP, attribution and telemetry observe results
```

Delivery does **not** implement HP damage formula; DamageResolver does **not** aim weapons, choose targets, raycast, animate, or run status timers. Existing `WORLD` occupancy, projectile order and FW1 fog rules remain authoritative for eligibility and collisions.

### Layer affinity rules

Per-target authored multiplier of `0` means immunity on the selected active defense layer; `0.5` means 50% effectiveness; `1` neutral; `1.5` vulnerability. A broad "all layers" profile can be overridden by individual shield/armour/hull values. The resolver should apply source modifiers, global type coefficients, target affinities and the layer's own characteristics **once**. Preserve base-damage units across layer overflow; zero-effective active layer **stops the non-bypassing damage**, not leaks damage through. Shield bypass must be separately defined and tested. An actor without a layer bypasses that missing layer naturally.

Health, shield and armour **storage and death** remain actor-owned. DMG0 mathematically supports defense layers but **does not add functional shields or armour pools** to current Player/Enemy/Building actors. Per-enemy optional damage affinity authoring should be possible now without unintended balance changes.

## Future delivery specifics

- **DMG1 area:** spatially query candidates once per impact; apply deterministic distance falloff at packet level, constrain faction/ground-air/occlusion before dispatch; one hit per eligible recipient per impact, no duplicated visuals as damage authority.
- **DMG2 beams:** DPS is an authored rate, not per-physics-frame amount. Use elapsed simulation time or bounded periodic ticks; consistent results across frame rates; beam loses validity when out of target/range/obstruction; one current damage authority.
- **FX1 effects:** one active-only world scheduler or efficient receiver-owned equivalent, clear pause/death/despawn semantics, no per-entity always-on timers; stacking policies per effect, duration seconds (not frame counts), source lifetime-safe, periodic ticks as ordinary typed DamageInfo without recursively reapplying effects.
- **DMG3 advanced defense:** actual regenerative shields/armour, response profiles, penetration, on-hit HUD. Independent of effects.
- **DMG4 modifiers:** crit rolls once at launch/attack, directional/rear effects resolved once with impact/source location, never every projectile tick.

## Performance and scope contract

For each pass preserve spatial buckets, swept collision, cached shot stats, event-driven wake/sleep, no duplicate projectile registries or per-entity health managers, no allocations/resource loads per beam tick, no full-world scans per splash impact. Owner handles visual/FPS acceptance; Codex performs brief source review and one build when appropriate. No benchmarks or broad suites unless requested.

## Future acceptance principles

Test friendly and hostile delivery paths, player/building/Citadel/enemy receivers, ground/air masks, friendly shots over Wall001 only, hostile shots hitting walls, fog targeting with presentation disabled, damage after shooter removal, immunity per active layer, status immunity independence, many simultaneous attackers, teardown/reset. Avoid claiming future passes are completed just because their interfaces or notes exist.

**DMG0 awaits owner validation.** FX1/DMG1/DMG2/DMG3/DMG4 each require separate owner authorization.
