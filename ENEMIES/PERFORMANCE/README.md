# ENEMIES/PERFORMANCE — FOG-PERF1A

**Source Implemented / Awaiting Owner Validation.** This pass reduces hidden enemy CPU cosmetic work only. No measured FPS improvement is claimed. Adaptive AI scheduling, slower movement, map enlargement and minimap work remain excluded.

## Ownership and updates

`EnemyVisibilityCoordinator.cs` is composed under RootEnemies/EnemyVisibility in shared LevelBase and DEBUG. LevelController initializes it with the existing FogOfWar and EnemyRegistry. FogPresentation still applies the shared mask material; the coordinator does not replace rendering visibility, fog state, enemy registration or spatial indexing.

- Startup uses one registry snapshot. Registry Added/Removed events create/discard a small presentation cache entry (cell, visible flag, shared visual reference) per enemy and adjust counters.
- The existing Enemy.SetMovementPosition → EnemyRegistry.UpdatePosition boundary publishes PositionChanged **after** updating spatial buckets. The coordinator compares fog cells and queries IsVisible only on a cell change. No per-enemy timer, new physics loop or allocation on ordinary movement.
- FogOfWar.VisibilityChanged uses its dirty cell rectangle with the registry's existing spatial query; an exact center-cell region check filters returned bucket candidates. No full-registry enumeration for a small fog edit. A large dirty rectangle may legitimately touch all enemies.
- Classification is authoritative logical center-cell visibility for both ground and flying enemies. Counters continue updating even with fog presentation or optimization disabled. Only the cache is new; no independent fog grid, enemy registry or gameplay authority.
- VisibleEnemies changes on classification/lifecycle transitions; HiddenEnemies = TotalEnemies − VisibleEnemies. F2 reads these O(1) values at its existing 4 Hz, never scans enemies for diagnostics.

## Cosmetic sleep and configuration

FogMasterConfig → **Enemy Presentation Performance → OptimizeHiddenEnemyVisuals = true** supplies the initial default. The coordinator keeps a separate world-local OptimizationEnabled field for F2 overrides; it never modifies the shared .tres. Reload/regeneration resets the override to the authored default.

EnemyVectorVisual.SetCosmeticDormant clears the short flash timer and disables that **visual child's** process only. While dormant, hit/attack/state notifications neither start flash ticks nor queue redraws; an already queued draw returns before decorative calculations. On wake it queues one redraw of current enemy HP, breach/variant state and normal geometry, without replaying old flashes. The Enemy node's Visible flag, physics, collision and gameplay callbacks are never altered by this coordinator.

| Fog presentation | Optimization setting | Cosmetic behavior |
| --- | --- | --- |
| On | On | Logically hidden visuals dormant; visible visuals normal |
| On | Off | Original cosmetic behavior; shader still masks hidden enemies |
| Off | On | All cosmetics restored for inspection; logical counters remain accurate |
| Off | Off | Original cosmetic behavior across the battlefield |

Toggling either control applies state once to cached visual references. Shader materials remain shared; no per-enemy material or visibility detector is created. Entries without the shared EnemyVectorVisual still count correctly but have no cosmetic suppression. Future types using the existing `VectorVisual` EnemyVectorVisual child (or subclass) work automatically.

## Lifecycle and limits

Removal/death/F2 Clear discard cache entries and detach subscriptions through registry events. Coordinator exit, fog exit or registry exit disconnects all service events, restores surviving visual components and clears references/counters. Level reload/regeneration creates fresh scene-scoped state. Both player-enabled and player-disabled levels use the same composition.

Fog classifications refresh on movement-cell changes and dirty-fog callbacks; source scheduling retains FW0's bounded/batched cadence. No second scene loop or delayed visibility timer is added. Existing movement, target roster, path queues, attacks, projectile damage/collision, spawn caps, FW1 tower sight and COMBAT1 wall overflight remain unchanged. Destruction pulses/projectiles retain their existing lifetime processing and shader masking; this pass does not optimize those effects or remove GPU submissions for cached visual commands.

## Owner validation

Source/diff and scene/resource checks performed; .NET unavailable for a build. No runtime/benchmark acceptance claimed.

1. F2-spawn 100–200 mixed Enemy001/002/003. Visible + hidden must equal total and match the existing live registry count.
2. Move the optional player or add/remove building vision. Crossing enemies must wake/sleep correctly; no-player CPU/building vision must also work.
3. Damage hidden enemies, then reveal: current HP/style must show with no old flash replay. Hidden enemies must keep moving, attacking and taking damage normally.
4. Exercise all four checkbox combinations above; fog-off must reveal all artwork and normal new flashes. F2 checkbox changes must not persist into the authored master config.
5. F2 Clear, source death, player loss, restart and regeneration must leave accurate counts and no retained subscriptions.
6. Compare FPS at identical camera/enemy/combat conditions, recording actual measurements separately. No FPS gains are estimated here.
