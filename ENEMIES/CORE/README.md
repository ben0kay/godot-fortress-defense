# ENEMIES/CORE — ST1 lifecycle

**Status: Source Implemented / Awaiting Owner Validation.** `Enemy.cs` snapshots an EnemyDefinition and receives explicit grid, registry, request service and strategic Citadel references before AddChild. It owns health, active/strategic target state, lifecycle and topology reactions; focused helpers own movement and attack timing.

`EnemyRegistry.cs` is per-world, never static. It tracks actual live instances, provides click-time actor/clearance overlap checks and manual snapshot-based clearing. Death/despawn/ExitTree remove registry membership, topology subscriptions and queued/active requests. Unreachable enemies disable physics until an occupancy change; CPU loss stops all survivors. `ApplyDamage` is ready for later combat, with a debug K control in the sandbox; no player weapon or projectile exists yet.
