# ENEMIES/VISUALS — ST1 arrow

**Status: Source Implemented / Awaiting Owner Validation.** `EnemyVectorVisual.cs` draws one lightweight arrow from the snapshotted enemy radius. Red = CPU seeker; orange = breach target; a brief white highlight accompanies hit/attack/state notifications. A damaged health arc and root FEEDBACK destruction ring provide basic lifecycle feedback.

Facing follows movement/attack direction. Drawing is event-driven; only the brief highlight enables visual processing. No visibility-based simulation suppression, crowds, fancy materials or unused visual child nodes. Artwork can slightly exceed the logical radius but never defines navigation/collision.

## FW0 visibility

**Source Implemented / Awaiting Owner Validation.** See [fog ownership, config and limits](../../WORLD/FOG/README.md). Shared LevelBase/DEBUG bind CPU and optional player sources through the existing WorldGrid. F2's **Fog of War Enabled** bypasses presentation only; exploration keeps tracking. Enemy visuals, shots and destruction effects are clipped without changing simulation. Friendly bases remain known/dim; unseen HP/selection/turret feedback is suppressed. LevelConfig → Fog/FogEnabled selects settings/initial presentation; camera controls never reveal terrain. Debug diagnostics refresh at 4 Hz, and standalone Debug instances can receive an explicit Fog node reference. Runtime and FPS comparisons remain owner checks.

## FOG-PERF1A hidden cosmetics

**Source Implemented / Awaiting Owner Validation.** EnemyVisibilityCoordinator calls SetCosmeticDormant on the shared VectorVisual child using logical fog center-cell classification. Hidden optimized visuals stop hit/attack flash ticks, ignore cosmetic redraw notifications and skip newly requested drawing; authoritative HP/attack state still updates. Waking clears stale flash state and redraws current HP/breach/variant geometry once. Enemy roots, movement/physics, collision, targeting and damage are untouched. The shared fog shader remains the rendering authority.

F2 → Optimize Hidden Enemy Visuals is independent of Fog of War Enabled. Fog-off restores all cosmetics for inspection regardless of logical hidden counts; optimization-off preserves original event-driven flashes even under the mask. No per-enemy visibility timer or material copy. Future types using EnemyVectorVisual under VectorVisual share this behavior. [Settings, lifecycle, four-mode table and owner checks](../PERFORMANCE/README.md). Runtime/FPS acceptance remains with the owner.
