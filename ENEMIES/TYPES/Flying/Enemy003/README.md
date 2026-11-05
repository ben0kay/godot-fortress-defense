# AIR0 + AIR1 — Enemy003 Basic Flyer

**Source Implemented / Awaiting Owner Validation.** F2 now offers enemy001, enemy002 and **enemy003**. Select enemy003 and use the existing10/50/100/200/custom Add buttons. Batches retain the chosen type; all types share the existing200 active+pending guard and gradual executor. Automatic spawning stays the authored Enemy001 scene by default. AIR2 now supplies Tower002; no gunship is implemented.

## Inspector settings

Edit `Enemy003Definition.tres`, then restart:

| Property | Default |
| --- | --- |
| EnemyId / DisplayName | enemy003 / Basic Flyer |
| MovementLayer | Flying |
| MaxHealth / Radius | 25HP / 12px |
| MovementSpeed / TurnSpeed | 145px/s / 10 |
| TargetRule | CitadelOnly |
| AttackBehavior / AttackDamage | Impact / 15HP |

AttackRange12 and AttackCooldown1 remain valid shared-definition fields, but impact contact uses the body radius and has no repeated cooldown attack. RangedAttack is absent. The pale-cyan winged vector silhouette, forward stripe, simple shadow and elevated draw order distinguish air from red/amber ground units. No hover-processing loop, physics body, detection Area2D or visual altitude simulation.

## Flight, contact and lifecycle

Enemy003 uses the existing Enemy root, HP/damage/death, EnemyRegistry and combat spatial index. `EnemyFlyingMovement` directly advances toward the cached objective, smoothly turns, and clamps the body within world bounds. Every movement uses SetMovementPosition, preserving spatial buckets. It does not inspect ground geometry, request A*, subscribe to ground topology changes or breach walls. Crossing unrelated buildings cannot trigger an impact.

`EnemyImpactAttack` sweeps each complete movement step against only the intended target's radius-expanded footprint. It marks itself resolved before damage callbacks, calls existing ICombatTarget.ApplyDamage once and guarantees despawn afterward. This handles large steps, prior overlaps, source death and target loss without repeated hits. The same movement-independent receiver can finish a future ground impact approach; no ground kamikaze content was added.

CPU-only flyers receive the existing Citadel reference once from spawning. Its TargetLost event clears strategic/active/navigation references and disables movement/attacks immediately on CPU death/removal; there is no replacement scan. Normal level defeat hooks/spawner CPU-loss behavior remain. Optional player presence has no effect on Enemy003's CitadelOnly policy.

Flying spawn validation uses finite map/body bounds, with an inward band only when radius requires it. It bypasses ground walkability, generated ground-connectivity filters, terrain and building occupancy, so dead/blocked edge cells are legal. Existing spawn body/player separation remains conservative. Building placement ignores airborne enemy overlap; construction beneath a flyer neither blocks nor repaths it. Ground actor placement safety is unchanged.

## AIR0 shared contracts

- `EnemyMovementLayer` is Ground/Flying and defaults Ground. Target policy and contact/impact/ranged capabilities are separate. Existing Enemy001/Enemy002 resources retain their IDs, references, ground stats and behavior.
- `EnemyTargetLayers` under COMBAT has None/Ground/Air/Both. Tower001 CombatConfig.TargetLayers defaults **Ground**, filters both spatial acquisition and cached eligibility, and never selects Enemy003 by name. Search cadence remains0.15s/staggered. No separate flying registry/index.
- Each Tower001 shot snapshots the selected target's layer and corresponding collision profile. A hybrid-capable tower can select either layer, but an individual shot stays on its launch plane. Direct API callers may explicitly request a Both mask.
- `GroundObstructed` retains existing tower/Enemy002 swept terrain/building obstruction. Friendly shots only damage eligible enemies; hostile ground shots damage the first solid building.
- `Airborne` friendly shots use the same swept enemy query but skip ground geometry, retain world-boundary/range expiry, and only hit the configured layer mask. AIR2 now uses this delivery in Tower002.
- `IntendedGroundTarget` hostile shots can pass over intervening ground geometry but sweep only their explicitly supplied ICombatTarget. They never damage an unrelated overlapping building. Target loss clears the reference; the straight miss expires without reacquisition. EnemyRangedConfig.CollisionProfile exposes this explicit option for later flying shooters; Enemy002 keeps GroundObstructed. No actual gunship/orbit/transport logic is implemented.

Flight movement creates no ground path workload. Only flying/impact entities allocate their small optional capability objects; ground movement retains its existing safety sweep/budgets. Targeting still uses one spatial enemy index and bounded shared decisions. A previously owner-observed60FPS ground benchmark is context, not proof of mixed-group performance.

## Brief owner acceptance checks

1. F2-spawn enemy003 and inspect direct movement over dead terrain, walls and buildings.
2. Watch it contact only the CPU, subtract15HP once and disappear. Kill one first with debug K; remove/destroy the CPU and confirm surviving flyers stop with no stale target or path work.
3. Place Tower001 and mix all three types: it must ignore flyers while shooting ground enemies. A ground shot passing through a flyer must not damage it.
4. Verify Enemy001/Enemy002 still route/breach/shoot around ground obstacles and preserve their different optional-player policies.
5. Spawn mixed batches near the200 combined cap, Clear, reset/regenerate and compare player-enabled/disabled scenes.
6. If checking the reusable AA profile, temporarily author Tower001.TargetLayers=Air or Both and place fresh towers: air shots should fly over ground obstacles and only hit air units. Restore Ground for normal Tower001. Tower002 now provides the normal Air-only test through Ctrl → Defense.
7. Inspect FPS/route stability with large mixed groups when convenient. No large automated benchmark, editor acceptance or FPS result was run here.

References inspected read-only: VECTOR TD scr_enemy_data (Basic Flyer, Heavy Flyer, Gunship, Flying Transporter), scr_enemy, scr_enemy_behavior, and scr_enemy_visual_flyer; Godot Tower Defense shared TowerTargeting and swept EnemyBulletSystem. This pass adapts direct-flight/vector/cached-eligibility ideas, not their grids, shields, rewards, transport or orbit systems.

Limits: 2D combat planes, conservative radius-expanded footprint contact, no altitude physics, collision/steering between moving enemies, explosions, gravity, homing, pooling or new building types. AIR2 Tower002 is now source implemented; AIR3 gunship remains planned.
