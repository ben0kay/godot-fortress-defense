# ENEMIES — BF1 / ST1 ground seeker

**Status: ST1 Source Implemented / Awaiting Owner Validation.**

Enemy001 is the initial vector ground attacker, with health, continuous movement, a cached Citadel objective, contact attacks, temporary wall-breach targeting and gradual edge spawning. Tower001 now uses the shared health/death receiver and spatial registry. Optional-player targeting and basic F2 stress batches are source implemented; weighted targeting, advanced roles, steering, fog and advanced ST2 scheduling/profiling remain planned.

| Owner | Implemented files / responsibility |
| --- | --- |
| `CORE/` | `Enemy.cs`: runtime and lifecycle; `EnemyRegistry.cs`: live set and click-time overlap checks |
| `DATA/` | `EnemyDefinition.cs`: Inspector-authored vitals, movement and contact-attack stats |
| `AI/` | `EnemyTargeting.cs`: target validity, footprint-distance/line checks and local breach-boundary lookup |
| `MOVEMENT/` | `EnemyMovement.cs`: continuous safe waypoints; `EnemyPathRequests.cs`: bounded cooperative request queue |
| `ATTACKS/` | `EnemyMeleeAttack.cs`: independent cooldown and scalar Building damage |
| `SPAWNING/` | `EdgeEnemySpawner.cs`: seeded valid-edge attempts, gradual creation, cap and CPU-loss shutdown |
| `TYPES/Basic/Enemy001/` | Concrete scene and default `.tres` definition |
| `VISUALS/` | `EnemyVectorVisual.cs`: red arrow, orange breacher, brief hit/attack highlight and damaged health arc |
| `ABILITIES/`, `MODIFIERS/`, `PERFORMANCE/` | Reserved for future passes; no gameplay implementation |

WORLD owns terrain/occupancy, the single AStarGrid2D and cooperative reachability workspace. No per-enemy A* grids, scene-wide target searches or per-frame pathfinding. Enemy001 retains its strategic CPU; EN2 hunters may lock and replace building objectives. A breach target never replaces that identity. UI lives at root `UI/`; cosmetic destruction feedback lives in root `FEEDBACK/`. ST1 contact attacks reuse existing scalar Building damage; a broader COMBAT damage/resistance framework is deferred.

Read [movement contracts](MOVEMENT/README.md) and [manual scene controls](../DEBUG/README.md). Target: eventually 100–200 active combatants at 60 FPS; this pass has a modest 20-active default and **no measured performance guarantee**. Enemy shapes can overlap each other; grid collision handles static obstacles, not crowd avoidance. Rendering visibility never disables gameplay damage/movement.

## EN2 additions

[Enemy002](TYPES/Ranged/Enemy002/README.md) is the authorized building-hunting shooter. EnemyDefinition exposes optional BuildingHunting and RangedAttack resources; Enemy.BuildingHunter.cs integrates these capabilities without a second AI/health controller. EnemyTargetRoster handles bounded priority acquisition/recovery and fair routine building/player decisions. BuildingHunterTargeting uses BuildingRegistry's stationary spatial queries. EnemyRangedAttack launches shared COMBAT projectiles; all navigation still uses EnemyPathRequests. F2 manual batches choose either type, while automatic composition remains unchanged. Source Implemented / Awaiting Owner Validation.

## AIR0 + AIR1

[Enemy003](TYPES/Flying/Enemy003/README.md) adds a configured Flying/CitadelOnly/Impact combination to the same Enemy root. MovementLayer defaults Ground for existing definitions and is independent of target policy/attack capability. Enemy.Flying.cs composes direct EnemyFlyingMovement and once-only EnemyImpactAttack. Shared registry/index/health remain unchanged; ground placement safety now excludes airborne bodies. Enemy003 has no ground topology subscription or path requests; CPU target loss clears references and sleeps. F2 now offers all three types. AIR2/AIR3 remain planned; no new aircraft types/towers or FPS guarantee.
