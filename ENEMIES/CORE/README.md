# ENEMIES/CORE — ST1 lifecycle

**Status: Source Implemented / Awaiting Owner Validation.** `Enemy.cs` snapshots an EnemyDefinition and receives explicit grid, registry, request service and strategic Citadel references before AddChild. It owns health, active/strategic target state, lifecycle and topology reactions; focused helpers own movement and attack timing.

`EnemyRegistry.cs` is per-world, never static. It tracks actual live instances, provides click-time actor/clearance overlap checks and manual snapshot-based clearing. Death/despawn/ExitTree remove registry membership, topology subscriptions and queued/active requests. Unreachable enemies disable physics until an occupancy change; CPU loss stops all survivors. `ApplyDamage(DamageInfo)` resolves Tower001/002 hits with the enemy definition’s optional affinity. Enemy still owns health, feedback and death. The scalar overload forwards debug hits.

## Shared combat spatial queries

Tower001/projectiles now use `EnemySpatialIndex.cs`, maintained by the existing registry. Each live enemy occupies one128px bucket; crossing a bucket updates it, same-bucket movement exits early, and removal/clear/ExitTree remove references. `Enemy.SetMovementPosition` is the authoritative movement/teleport boundary, used by the existing waypoint follower; future movement code must use it. Query fills caller-owned scratch lists, with exact range/collision filtering in COMBAT. MaxEnemyRadius is a conservative bound, reset when no enemies remain. No native transform-notification plus duplicate explicit update is added. Existing spawn, health, death, CPU/breach target and navigation semantics are unchanged.

Revised PL0 adds optional TargetRoster injection (default null), NavigationTarget/ActiveTarget through ICombatTarget, and bounded mobile-goal decisions. There is still one Enemy runtime, with movement/target-choice/attack helpers separate. No player means CPU/breach behavior is retained; death/despawn also unregisters from the roster.

EN2: Enemy.BuildingHunter.cs adds the building-policy lock/loss hooks and focused ranged execution to the same Enemy root. Hunter initial acquisition waits for the shared scheduler instead of immediately using the CPU. Target replacement cancels/coalesces one route; loss clears cached attack/movement before scheduled recovery. Hunters remain independent of CPU/player lifecycle and keep hunting remaining structures after CPU loss. Enemy001 keeps its existing CPU-loss stop and optional-player rules.

AIR0/AIR1 adds Enemy.Flying.cs for optional direct-flight and impact hooks. Ground defaults retain existing navigation. Flyers snapshot MovementLayer, create only their own flight helper, watch CPU loss directly (clear strategic/navigation/active references), and never subscribe/request ground navigation. Shared health, registry, spatial movement updates, death and despawn cleanup are reused. New impact behavior is independent of movement/target policy.
