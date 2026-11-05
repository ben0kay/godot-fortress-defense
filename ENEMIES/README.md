# ENEMIES — BF1 / ST1 ground seeker

**Status: ST1 Source Implemented / Awaiting Owner Validation.**

Enemy001 is the initial vector ground attacker, with health, continuous movement, a cached Citadel objective, contact attacks, temporary wall-breach targeting and gradual edge spawning. No towers, weighted target selection, player targeting, advanced roles, steering, fog or ST2 stress presets are implemented.

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

WORLD owns terrain/occupancy, the single AStarGrid2D and cooperative reachability workspace. No per-enemy A* grids, scene-wide target searches or per-frame pathfinding. Enemies retain one strategic CPU reference for their whole life. A breach target never replaces that identity. UI lives at root `UI/`; cosmetic destruction feedback lives in root `FEEDBACK/`. ST1 contact attacks reuse existing scalar Building damage; a broader COMBAT damage/resistance framework is deferred.

Read [movement contracts](MOVEMENT/README.md) and [manual scene controls](../DEBUG/README.md). Target: eventually 100–200 active combatants at 60 FPS; this pass has a modest 20-active default and **no measured performance guarantee**. Enemy shapes can overlap each other; grid collision handles static obstacles, not crowd avoidance. Rendering visibility never disables gameplay damage/movement.
