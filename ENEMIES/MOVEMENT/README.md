# ENEMIES/MOVEMENT — ST1 safe cached movement

**Status: Source Implemented / Awaiting Owner Validation.**

`EnemyMovement.cs` follows world-space A* waypoints continuously, smoothly turns the arrow, and checks every actual movement segment against **current** WORLD occupancy. The swept test conservatively expands solid rectangles by the enemy radius, so a new wall cannot be crossed while queued work catches up. Work is capped at eight waypoints per physics tick; there is no per-frame search, steering or crowd collision.

`EnemyPathRequests.cs` is the minimal ST1 shared mechanism, not ST2's advanced scheduler:

- A linked FIFO plus per-enemy index coalesces duplicates and removes cancelled requests in O(1). Affected enemies pause while waiting; unaffected cached paths continue.
- One reusable `WORLD/Navigation/GroundReachability` workspace incrementally floods the origin's walkable component (512 visits/frame by default), testing legal CPU attack positions. Cardinal connectivity matches the corner-safe diagonal graph.
- Reachable CPU positions win. Only if the component is exhausted does it select an accessible building found at that component's boundary. The fallback score favors buildings closer to the original CPU with a small travel-distance preference; it is a heuristic, not an optimal demolition planner.
- A single shared `GroundNavigation.FindPath` finalizes the chosen approach. Default: one native A* per physics frame plus a 1.5-ms soft budget. Each job keeps its frozen origin/revision. Topology edits restart stale jobs at the FIFO tail; no result from an obsolete revision is accepted.
- No reachable target: enemy sleeps until a real topology change. There is no fixed-frequency failed-path retry. Destroying the strategic CPU cancels jobs and stops survivors.

`Enemy.OnTopologyChanged` compares edits with remaining segment bounds (including clearance halo); cells already passed are ignored. Breachers, missing targets and dormant failed routes reconsider after any topology edit. Openings do not force all valid CPU-bound enemies to seek shorter routes. Immediate segment safety is independent of invalidation accuracy. Pausing for replacements can create a short stop or small cell-center backtrack; safe-prefix/reconnection polish, proximity priorities, timing counters and fair priority refinements belong to ST2.

Breach candidates come from direct cell-to-building lookups beside visited cells; no full building registry scan per enemy. Terrain can never be a damageable breach target. The current cardinal boundary search is intentionally simple and can choose a merely useful reachable structure when complex terrain separates the CPU. Further opening decisions are made after each destruction/topology change.

Stats snapshot at spawn; radius must fit within half a grid cell. Placement rejects overlap with bodies and clearance cells, avoiding enemies trapped inside freshly claimed solids. No per-enemy physics-body avoidance is enabled: the grid sweep is authoritative for movement; enemies overlap each other. Future size classes/phasing must derive consistent views from WORLD rather than introducing competing terrain data.

Revised PL0: the same cooperative queue may resolve an explicitly chosen ICombatTarget mobile approach; no second grid/queue or extra A* budget is created. Native path/visit/time defaults remain unchanged. Unreachable mobile jobs fall back to CPU routing; rejected mobile cell/revision is cached until changed. Moving-goal reconsideration is bounded by TargetRoster, not physics-frame pathfinding. Pursuit may briefly pause while queued, as existing ST1 replacements do.

EN2 ranged jobs use the same cooperative queue/limits, evaluating full attack range and projectile-radius LOS at visited reachable cells rather than requiring an adjacent cell. The old CPU/melee profile is unchanged. Ranged units can stop/fire before an unnecessary final waypoint, cancelling queued work; lost sight queues a replacement. Failed static jobs sleep until topology/target changes. Strategic building retargeting never repaths an unchanged target.
