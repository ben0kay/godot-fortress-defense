# ENEMIES/CORE — ST1 lifecycle

**Status: Source Implemented / Awaiting Owner Validation.** `Enemy.cs` snapshots an EnemyDefinition and receives explicit grid, registry, request service and strategic Citadel references before AddChild. It owns health, active/strategic target state, lifecycle and topology reactions; focused helpers own movement and attack timing.

`EnemyRegistry.cs` is per-world, never static. It tracks actual live instances, provides click-time actor/clearance overlap checks and manual snapshot-based clearing. Death/despawn/ExitTree remove registry membership, topology subscriptions and queued/active requests. Unreachable enemies disable physics until an occupancy change; CPU loss stops all survivors. `ApplyDamage` is ready for later combat, with a debug K control in the sandbox; player weapons remain excluded; Tower001 projectiles use this health receiver.

## Shared combat spatial queries

Tower001/projectiles now use `EnemySpatialIndex.cs`, maintained by the existing registry. Each live enemy occupies one128px bucket; crossing a bucket updates it, same-bucket movement exits early, and removal/clear/ExitTree remove references. `Enemy.SetMovementPosition` is the authoritative movement/teleport boundary, used by the existing waypoint follower; future movement code must use it. Query fills caller-owned scratch lists, with exact range/collision filtering in COMBAT. MaxEnemyRadius is a conservative bound, reset when no enemies remain. No native transform-notification plus duplicate explicit update is added. Existing spawn, health, death, CPU/breach target and navigation semantics are unchanged.

Revised PL0 adds optional TargetRoster injection (default null), NavigationTarget/ActiveTarget through ICombatTarget, and bounded mobile-goal decisions. There is still one Enemy runtime, with movement/target-choice/attack helpers separate. No player means CPU/breach behavior is retained; death/despawn also unregisters from the roster.
